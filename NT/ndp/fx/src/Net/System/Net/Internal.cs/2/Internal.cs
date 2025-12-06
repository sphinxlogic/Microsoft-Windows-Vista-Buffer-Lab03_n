//------------------------------------------------------------------------------ 
// <copyright file="Internal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.IO; 
    using System.Reflection;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Net.Sockets; 
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning; 
    using System.Security.Cryptography.X509Certificates; 
    using System.Text;
    using System.Text.RegularExpressions; 
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Threading;
    using System.Security.Principal; 
    using System.Security;
    using System.Net.Security; 
    using System.Net.NetworkInformation; 
    using System.Runtime.Serialization;
 
    internal static class IntPtrHelper {
        /*
        // Consider removing.
        internal static IntPtr Add(IntPtr a, IntPtr b) { 
            return (IntPtr) ((long)a + (long)b);
        } 
        */ 
        internal static IntPtr Add(IntPtr a, int b) {
            return (IntPtr) ((long)a + (long)b); 
        }
    }

    internal class InternalException : SystemException 
    {
        internal InternalException() 
        { 
            GlobalLog.Assert("InternalException thrown.");
        } 

        internal InternalException(SerializationInfo serializationInfo, StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        { } 
    }
 
    internal static class NclUtilities 
    {
        /// <devdoc> 
        ///    <para>
        ///       Indicates true if the threadpool is low on threads,
        ///       in this case we need to refuse to start new requests,
        ///       and avoid blocking. 
        ///    </para>
        /// </devdoc> 
        internal static bool IsThreadPoolLow() 
        {
#if !FEATURE_PAL 
            if (ComNetOS.IsAspNetServer)
            {
                return false;
            } 
#endif //!FEATURE_PAL
 
            int workerThreads, completionPortThreads; 
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
 
#if !FEATURE_PAL
            return workerThreads < 2 || (ComNetOS.IsWinNt && completionPortThreads < 2);
#else
            GlobalLog.Assert(completionPortThreads == 0, "completionPortThreads should be zero on the PAL"); 
            return workerThreads < 2;
#endif //!FEATURE_PAL 
        } 

 
        internal static bool HasShutdownStarted
        {
            get
            { 
                return Environment.HasShutdownStarted || AppDomain.CurrentDomain.IsFinalizingForUnload();
            } 
        } 

        // This only works for context-destroying errors. 
        internal static bool IsCredentialFailure(SecurityStatus error)
        {
            return error == SecurityStatus.LogonDenied ||
                error == SecurityStatus.UnknownCredentials || 
                error == SecurityStatus.NoImpersonation ||
                error == SecurityStatus.NoAuthenticatingAuthority || 
                error == SecurityStatus.UntrustedRoot || 
                error == SecurityStatus.CertExpired ||
                error == SecurityStatus.SmartcardLogonRequired; 
        }

        // This only works for context-destroying errors.
        internal static bool IsClientFault(SecurityStatus error) 
        {
            return error == SecurityStatus.InvalidToken || 
                error == SecurityStatus.CannotPack || 
                error == SecurityStatus.QopNotSupported ||
                error == SecurityStatus.NoCredentials || 
                error == SecurityStatus.MessageAltered ||
                error == SecurityStatus.OutOfSequence ||
                error == SecurityStatus.IncompleteMessage ||
                error == SecurityStatus.IncompleteCredentials || 
                error == SecurityStatus.WrongPrincipal ||
                error == SecurityStatus.TimeSkew || 
                error == SecurityStatus.IllegalMessage || 
                error == SecurityStatus.CertUnknown ||
                error == SecurityStatus.AlgorithmMismatch || 
                error == SecurityStatus.SecurityQosFailed ||
                error == SecurityStatus.UnsupportedPreauth;
        }
 

        // ContextRelativeDemand 
        // Allows easily demanding a permission against a given ExecutionContext. 
        // Have requested the CLR to provide this method on ExecutionContext.
        private static ContextCallback s_ContextRelativeDemandCallback; 

        internal static ContextCallback ContextRelativeDemandCallback
        {
            get 
            {
                if (s_ContextRelativeDemandCallback == null) 
                    s_ContextRelativeDemandCallback = new ContextCallback(DemandCallback); 
                return s_ContextRelativeDemandCallback;
            } 
        }

        private static void DemandCallback(object state)
        { 
            ((CodeAccessPermission) state).Demand();
        } 
 
        // This is for checking if a hostname probably refers to this machine without going to DNS.
        internal static bool GuessWhetherHostIsLoopback(string host) 
        {
            string hostLower = host.ToLowerInvariant();
            if (hostLower == "localhost" || hostLower == "loopback")
            { 
                return true;
            } 
 
#if !FEATURE_PAL
            IPGlobalProperties ip = IPGlobalProperties.InternalGetIPGlobalProperties(); 
            string hostnameLower = ip.HostName.ToLowerInvariant();
            return hostLower == hostnameLower || hostLower == hostnameLower + "." + ip.DomainName.ToLowerInvariant();
#else
            return false; 
#endif
        } 
 
        internal static bool IsFatal(Exception exception)
        { 
            return exception != null && (exception is OutOfMemoryException || exception is StackOverflowException || exception is ThreadAbortException);
        }

        // Need a fast cached list of local addresses for internal use. 
        private static IPAddress[] _LocalAddresses;
        private static object _LocalAddressesLock; 
        private static NetworkAddressChangePolled s_AddressChange; 

#if !FEATURE_PAL 
        internal static IPAddress[] LocalAddresses
        {
            get
            { 
                if (s_AddressChange != null && s_AddressChange.CheckAndReset())
                { 
                    return (_LocalAddresses = GetLocalAddresses()); 
                }
 
                if (_LocalAddresses != null)
                {
                    return _LocalAddresses;
                } 

                lock (LocalAddressesLock) 
                { 
                    if (_LocalAddresses != null)
                    { 
                        return _LocalAddresses;
                    }

                    s_AddressChange = new NetworkAddressChangePolled(); 

                    return (_LocalAddresses = GetLocalAddresses()); 
                } 
            }
        } 

        private static IPAddress[] GetLocalAddresses()
        {
            IPAddress[] local; 

            // Different discovery for pre or post XP 
            if (ComNetOS.IsPostWin2K) 
            {
                ArrayList collections = new ArrayList(16); 
                int total = 0;

                SafeLocalFree buffer = null;
                GetAdaptersAddressesFlags gaaFlags = GetAdaptersAddressesFlags.SkipAnycast | GetAdaptersAddressesFlags.SkipMulticast | 
                    GetAdaptersAddressesFlags.SkipFriendlyName | GetAdaptersAddressesFlags.SkipDnsServer;
                uint size = 0; 
                uint result = UnsafeNetInfoNativeMethods.GetAdaptersAddresses(AddressFamily.Unspecified, (uint)gaaFlags, IntPtr.Zero, SafeLocalFree.Zero, ref size); 
                while (result == IpHelperErrors.ErrorBufferOverflow)
                { 
                    try
                    {
                        buffer = SafeLocalFree.LocalAlloc((int)size);
                        result = UnsafeNetInfoNativeMethods.GetAdaptersAddresses(AddressFamily.Unspecified, (uint)gaaFlags, IntPtr.Zero, buffer, ref size); 

                        if (result == IpHelperErrors.Success) 
                        { 
                            IpAdapterAddresses adapterAddresses = (IpAdapterAddresses)Marshal.PtrToStructure(buffer.DangerousGetHandle(), typeof(IpAdapterAddresses));
 
                            while (true)
                            {
                                if (adapterAddresses.FirstUnicastAddress != IntPtr.Zero)
                                { 
                                    UnicastIPAddressInformationCollection coll = SystemUnicastIPAddressInformation.ToAddressInformationCollection(adapterAddresses.FirstUnicastAddress);
                                    total += coll.Count; 
                                    collections.Add(coll); 
                                }
 
                                if (adapterAddresses.next == IntPtr.Zero)
                                {
                                    break;
                                } 

                                adapterAddresses = (IpAdapterAddresses)Marshal.PtrToStructure(adapterAddresses.next, typeof(IpAdapterAddresses)); 
                            } 
                        }
                    } 
                    finally
                    {
                        if (buffer != null)
                            buffer.Close(); 
                        buffer = null;
                    } 
                } 

                if (result != IpHelperErrors.Success && result != IpHelperErrors.ErrorNoData) 
                {
                    throw new NetworkInformationException((int)result);
                }
 
                local = new IPAddress[total];
                uint i = 0; 
                foreach (UnicastIPAddressInformationCollection coll in collections) 
                {
                    foreach (IPAddressInformation info in coll) 
                    {
                        local[i++] = info.Address;
                    }
                } 
            }
            else 
            { 
                ArrayList collections = new ArrayList(16);
                int total = 0; 

                SafeLocalFree buffer = null;
                uint size = 0;
                uint result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(SafeLocalFree.Zero, ref size); 
                while (result == IpHelperErrors.ErrorBufferOverflow)
                { 
                    try 
                    {
                        buffer = SafeLocalFree.LocalAlloc((int)size); 
                        result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(buffer, ref size);

                        if (result == IpHelperErrors.Success)
                        { 
                            IpAdapterInfo adapterInfo = (IpAdapterInfo)Marshal.PtrToStructure(buffer.DangerousGetHandle(), typeof(IpAdapterInfo));
 
                            while (true) 
                            {
                                IPAddressCollection coll = adapterInfo.ipAddressList.ToIPAddressCollection(); 
                                total += coll.Count;
                                collections.Add(coll);

                                if (adapterInfo.Next == IntPtr.Zero) 
                                {
                                    break; 
                                } 

                                adapterInfo = (IpAdapterInfo)Marshal.PtrToStructure(adapterInfo.Next, typeof(IpAdapterInfo)); 
                            }
                        }
                    }
                    finally 
                    {
                        if (buffer != null) 
                            buffer.Close(); 
                    }
                } 


                if (result != IpHelperErrors.Success && result != IpHelperErrors.ErrorNoData)
                { 
                    throw new NetworkInformationException((int)result);
                } 
 
                local = new IPAddress[total];
                uint i = 0; 
                foreach (IPAddressCollection coll in collections)
                {
                    foreach (IPAddress info in coll)
                    { 
                        local[i++] = info;
                    } 
                } 
            }
            return local; 
        }

        internal static bool IsAddressLocal(IPAddress ipAddress) {
            IPAddress[] localAddresses = NclUtilities.LocalAddresses; 
            for (int i = 0; i < localAddresses.Length; i++)
            { 
                if (ipAddress.Equals(localAddresses[i], false)) 
                {
                    return true; 
                }
            }
            return false;
        } 

#else // !FEATURE_PAL 
        private const int HostNameBufferLength = 256; 
        internal static string _LocalDomainName;
 
        // Copied from the old version of DNS.cs
        // Returns a list of our local addresses by calling gethostbyname with null.
        //
        private static IPHostEntry GetLocalHost() 
        {
            // 
            // IPv6 Changes: If IPv6 is enabled, we can't simply use the 
            //               old IPv4 gethostbyname(null). Instead we need
            //               to do a more complete lookup. 
            //
            if (Socket.SupportsIPv6)
            {
                // 
                // IPv6 enabled: use getaddrinfo() of the local host name
                // to obtain this information. Need to get the machines 
                // name as well - do that here so that we don't need to 
                // Assert DNS permissions.
                // 
                StringBuilder hostname = new StringBuilder(HostNameBufferLength);
                SocketError errorCode =
                    UnsafeNclNativeMethods.OSSOCK.gethostname(
                    hostname, 
                    HostNameBufferLength);
 
                if (errorCode != SocketError.Success) 
                {
                    throw new SocketException(); 
                }

                return Dns.GetHostByName(hostname.ToString());
            } 
            else
            { 
                // 
                // IPv6 disabled: use gethostbyname() to obtain information.
                // 
                IntPtr nativePointer =
                    UnsafeNclNativeMethods.OSSOCK.gethostbyname(
                    null);
 
                if (nativePointer == IntPtr.Zero)
                { 
                    throw new SocketException(); 
                }
 
                return Dns.NativeToHostEntry(nativePointer);
            }

        } // GetLocalHost 

        internal static IPAddress[] LocalAddresses 
        { 
            get
            { 
                IPAddress[] local = _LocalAddresses;
                if (local != null)
                {
                    return local; 
                }
 
                lock (LocalAddressesLock) 
                {
                    local = _LocalAddresses; 
                    if (local != null)
                    {
                        return local;
                    } 

                    List<IPAddress> localList = new List<IPAddress>(); 
 
                        try
                        { 
                            IPHostEntry hostEntry = GetLocalHost();
                            if (hostEntry != null)
                            {
                                if (hostEntry.HostName != null) 
                                {
                                    int dot = hostEntry.HostName.IndexOf('.'); 
                                    if (dot != -1) 
                                    {
                                        _LocalDomainName = hostEntry.HostName.Substring(dot); 
                                    }
                                }

                                IPAddress[] ipAddresses = hostEntry.AddressList; 
                                if (ipAddresses != null)
                                { 
                                    foreach (IPAddress ipAddress in ipAddresses) 
                                    {
                                        localList.Add(ipAddress); 
                                    }
                                }
                            }
                        } 
                        catch
                        { 
                        } 

                    local = new IPAddress[localList.Count]; 
                    int index = 0;
                    foreach (IPAddress ipAddress in localList)
                    {
                        local[index] = ipAddress; 
                        index++;
                    } 
                    _LocalAddresses = local; 

                    return local; 
                }
            }
        }
#endif // !FEATURE_PAL 

        private static object LocalAddressesLock 
        { 
            get
            { 
                if (_LocalAddressesLock == null)
                {
                    Interlocked.CompareExchange(ref _LocalAddressesLock, new object(), null);
                } 
                return _LocalAddressesLock;
            } 
        } 
    }
 
    internal static class NclConstants
    {
        internal static readonly object Sentinel = new object();
        internal static readonly object[] EmptyObjectArray = new object[0]; 
        internal static readonly Uri[] EmptyUriArray = new Uri[0];
 
        internal static readonly byte[] CRLF = new byte[] {(byte) '\r', (byte) '\n'}; 
        internal static readonly byte[] ChunkTerminator = new byte[] {(byte) '0', (byte) '\r', (byte) '\n', (byte) '\r', (byte) '\n'};
    } 

    //
    // A simple sync point, useful for deferring work.  Just an int value with helper methods.
    // 
    // The two events being synchronized are the "Triggering" event and the "Completing" event.  The Triggering event
    // marks the gate as being active; the first subsequent Completing event handles the action. 
    // 
    // First, a thread calls Trigger() to set the trigger on the gate.  This means it needs some work to be done, but only
    // after another operation (the Completing event) finishes.  If Trigger() returns false, the Completing event already 
    // happened.  When the Completing event occurs, that thread calls Complete().  It returns true if the gate has been
    // previously triggered and the caller is the first one to complete it.  The caller should then handle the pending work item.
    //
    // StartTrigger()/FinishTrigger() can be used instead of Trigger() if the triggering thread needs to set up some state 
    // (e.g. the pending work item).  It will block Complete() in a spin-lock.
    // 
    internal struct InterlockedGate 
    {
        private int m_State; 

        internal const int Open = 0;        // Initial state of gate.
        internal const int Held = 1;        // Gate is being actively held by a thread - indeterminate state.
        internal const int Triggered = 2;   // The triggering event has occurred. 
        internal const int Closed = 3;      // The gated event is done.
 
#if DEBUG 
        /* Consider removing
        internal int State 
        {
            get
            {
                return m_State; 
            }
        } 
        */ 
#endif
 
        // Only call when all threads are guaranteed to be done with the gate.
        internal void Reset()
        {
            m_State = Open; 
        }
 
        // Returns false if the gate is already closed or triggered.  If exclusive is true, throws if the gate is already 
        // triggered.
        internal bool Trigger(bool exclusive) 
        {
            int gate = Interlocked.CompareExchange(ref m_State, Triggered, Open);
            if (exclusive && (gate == Held || gate == Triggered))
            { 
                GlobalLog.Assert("InterlockedGate::Trigger", "Gate already triggered.");
                throw new InternalException(); 
            } 
            return gate == Open;
        } 

        // Use StartTrigger() and FinishTrigger() to trigger the gate as a two step operation.  This is useful to set up an invariant
        // that must be ready by the time another thread closes the gate.  Do not block between StartTrigger() and FinishTrigger(), just
        // set up your state to be consistent.  If this method returns true, FinishTrigger() *must* be called to avoid deadlock - do 
        // it in a finally.
        // 
        // Returns false if the gate is already closed or triggered.  If exclusive is true, throws if the gate is already 
        // triggered.
        internal bool StartTrigger(bool exclusive) 
        {
            int gate = Interlocked.CompareExchange(ref m_State, Held, Open);
            if (exclusive && (gate == Held || gate == Triggered))
            { 
                GlobalLog.Assert("InterlockedGate::StartTrigger", "Gate already triggered.");
                throw new InternalException(); 
            } 
            return gate == Open;
        } 

        // Gate must be held by StartTrigger().
        internal void FinishTrigger()
        { 
            int gate = Interlocked.CompareExchange(ref m_State, Triggered, Held);
            if (gate != Held) 
            { 
                GlobalLog.Assert("InterlockedGate::FinishTrigger", "Gate not held.");
                throw new InternalException(); 
            }
        }

        // Returns false if the gate had never been triggered or is already closed. 
        internal bool Complete()
        { 
            int gate; 

            // Spin while the gate is being held, allowing the other thread to set invariants up. 
            while ((gate = Interlocked.CompareExchange(ref m_State, Closed, Triggered)) != Triggered)
            {
                if (gate == Closed)
                { 
                    return false;
                } 
 
                if (gate == Open)
                { 
                    if (Interlocked.CompareExchange(ref m_State, Closed, Open) == Open)
                    {
                        return false;
                    } 

                    continue; 
                } 

                // gate == Held 
                Thread.SpinWait(1);
            }

            return true; 
        }
    } 
 
#if !FEATURE_PAL
    // 
    // A polling implementation of NetworkAddressChange.
    //
    internal class NetworkAddressChangePolled : IDisposable
    { 
        private bool disposed;
        private SafeCloseSocketAndEvent ipv4Socket = null; 
        private SafeCloseSocketAndEvent ipv6Socket = null; 

 
        internal unsafe NetworkAddressChangePolled()
        {
            Socket.InitializeSockets();
            int blocking; 
            if (Socket.SupportsIPv4)
            { 
                blocking = -1; 
                ipv4Socket = SafeCloseSocketAndEvent.CreateWSASocketWithEvent(AddressFamily.InterNetwork, SocketType.Dgram, (ProtocolType)0, true, false);
                UnsafeNclNativeMethods.OSSOCK.ioctlsocket(ipv4Socket, IoctlSocketConstants.FIONBIO, ref blocking); 
            }

            if(Socket.OSSupportsIPv6){
                blocking = -1; 
                ipv6Socket = SafeCloseSocketAndEvent.CreateWSASocketWithEvent(AddressFamily.InterNetworkV6, SocketType.Dgram, (ProtocolType)0, true, false);
                UnsafeNclNativeMethods.OSSOCK.ioctlsocket(ipv6Socket,IoctlSocketConstants.FIONBIO,ref blocking); 
            } 
            Setup(StartIPOptions.Both);
        } 

        private unsafe void Setup(StartIPOptions startIPOptions)
        {
            int length; 
            SocketError errorCode;
 
 
            if (Socket.SupportsIPv4 && (startIPOptions & StartIPOptions.StartIPv4) != 0){
                errorCode = (SocketError)UnsafeNclNativeMethods.OSSOCK.WSAIoctl_Blocking( 
                    ipv4Socket.DangerousGetHandle(),
                    (int) IOControlCode.AddressListChange,
                    null, 0, null, 0,
                    out length, 
                    IntPtr.Zero, IntPtr.Zero);
 
                if (errorCode != SocketError.Success) { 
                    NetworkInformationException exception = new NetworkInformationException();
                    if (exception.ErrorCode != (uint)SocketError.WouldBlock) { 
                        Dispose();
                        return;
                    }
                } 

                errorCode = (SocketError)UnsafeNclNativeMethods.OSSOCK.WSAEventSelect(ipv4Socket, ipv4Socket.GetEventHandle().SafeWaitHandle, AsyncEventBits.FdAddressListChange); 
                if (errorCode != SocketError.Success) { 
                    Dispose();
                    return; 
                }
            }

            if(Socket.OSSupportsIPv6 && (startIPOptions & StartIPOptions.StartIPv6) !=0){ 
                errorCode = (SocketError) UnsafeNclNativeMethods.OSSOCK.WSAIoctl_Blocking(
                    ipv6Socket.DangerousGetHandle(), 
                    (int) IOControlCode.AddressListChange, 
                    null, 0, null, 0,
                    out length, 
                    IntPtr.Zero, IntPtr.Zero);

                if (errorCode != SocketError.Success) {
                    NetworkInformationException exception = new NetworkInformationException(); 
                    if (exception.ErrorCode != (uint)SocketError.WouldBlock) {
                        Dispose(); 
                        return; 
                    }
                } 

                errorCode = (SocketError)UnsafeNclNativeMethods.OSSOCK.WSAEventSelect(ipv6Socket, ipv6Socket.GetEventHandle().SafeWaitHandle, AsyncEventBits.FdAddressListChange);
                if (errorCode != SocketError.Success) {
                    Dispose(); 
                    return;
                } 
            } 
        }
 
        internal bool CheckAndReset()
        {
            if(!disposed){
                lock (this){ 
                    if (!disposed){
                        StartIPOptions options = StartIPOptions.None; 
 
                        if (ipv4Socket != null && ipv4Socket.GetEventHandle().WaitOne(0, false)){
                            options|= StartIPOptions.StartIPv4; 
                        }
                        if (ipv6Socket != null && ipv6Socket.GetEventHandle().WaitOne(0, false))
                        {
                            options|= StartIPOptions.StartIPv6; 
                        }
 
                        if(options != StartIPOptions.None){ 
                            Setup(options);
                            return true; 
                        }
                    }
                }
            } 
            return false;
        } 
 
        public void Dispose()
        { 
            if(!disposed){
                lock (this){
                    if (!disposed){
                        if(ipv6Socket != null){ 
                            ipv6Socket.Close();
                            ipv6Socket = null; 
                        } 
                        if(ipv4Socket != null){
                            ipv4Socket.Close(); 
                            ipv6Socket = null;
                        }
                        disposed = true;
                    } 
                }
            } 
        } 
    }
#endif // FEATURE_PAL 



#if !FEATURE_PAL 
    internal static class ComNetOS
    { 
        internal static readonly bool IsWin9x; 
        internal static readonly bool IsWinNt;
        internal static readonly bool IsWin2K; 
        internal static readonly bool IsPostWin2K;    // ie: XP or later but not Win2K
        internal static readonly bool IsAspNetServer; // ie: running under ASP+
        internal static readonly bool IsWinHttp51;    // Is WinHttp 5.1 available.
        internal static readonly bool IsWin2k3;       // Is Windows Server 2003 or later. 
        internal static readonly bool IsXpSp2;        // Is Windows XP sp2 or later
        internal static readonly bool IsWin2k3Sp1;    // Is Windows 2003 sp1 or later 
        internal static readonly bool IsVista;        // Is Windows Vista or later 

        // We use it safe so assert 
        [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.AppDomain, ResourceScope.AppDomain)]
        static ComNetOS() 
        {
            OperatingSystem operatingSystem = Environment.OSVersion; 
 
            GlobalLog.Print("ComNetOS::.ctor(): " + operatingSystem.ToString());
 
            if (operatingSystem.Platform == PlatformID.Win32Windows)
            {
                IsWin9x = true;
                return; 
            }
 
            // 
            // Detect ASP+ as a platform running under NT
            // 

            try
            {
                IsAspNetServer = (Thread.GetDomain().GetData(".appDomain") != null); 
            }
            catch { } 
 
            //
            // Platform is Windows NT or later 
            // NT4: 4.0
            // 2000: 5.0
            // XP: 5.1
            // Win2k3: 5.2 
            //
            IsWinNt = true; 
 
            //
            // Platform is Windows NT 2K or later 
            // operatingSystem.Version.Major>=5
            //
            IsWin2K = true;
 
            if (operatingSystem.Version.Major == 5 && operatingSystem.Version.Minor == 0)
            { 
                IsWinHttp51 = operatingSystem.Version.MajorRevision >= 3;    // Win2k SP3 
                return;
            } 

            IsPostWin2K = true;

            if ((operatingSystem.Version.Major == 5 
                && operatingSystem.Version.Minor == 1
                && operatingSystem.Version.MajorRevision >=2) 
                ||(operatingSystem.Version.Major >= 6)) 
            {
                IsXpSp2 = true;        // WinXP SP2 
            }

            if (operatingSystem.Version.Major == 5 && operatingSystem.Version.Minor == 1)
            { 
                IsWinHttp51 = operatingSystem.Version.MajorRevision >= 1;    // WinXP SP1
 
                return; 
            }
 
            IsWinHttp51 = true;
            IsWin2k3 = true;

            if ((operatingSystem.Version.Major == 5 
                && operatingSystem.Version.Minor == 2
                && operatingSystem.Version.MajorRevision >= 1) 
                || (operatingSystem.Version.Major >= 6)) 
            {
                IsWin2k3Sp1 = true;    // Win2k3 SP1 
            }

            if (operatingSystem.Version.Major >= 6)
            { 
                IsVista = true;
            } 
        } 
    }
#endif 


    //
    // support class for Validation related stuff. 
    //
    internal static class ValidationHelper { 
 
        public static string [] EmptyArray = new string[0];
 
        internal static readonly char[]  InvalidMethodChars =
                new char[]{
                ' ',
                '\r', 
                '\n',
                '\t' 
                }; 

        // invalid characters that cannot be found in a valid method-verb or http header 
        internal static readonly char[]  InvalidParamChars =
                new char[]{
                '(',
                ')', 
                '<',
                '>', 
                '@', 
                ',',
                ';', 
                ':',
                '\\',
                '"',
                '\'', 
                '/',
                '[', 
                ']', 
                '?',
                '=', 
                '{',
                '}',
                ' ',
                '\t', 
                '\r',
                '\n'}; 
 
        public static string [] MakeEmptyArrayNull(string [] stringArray) {
            if ( stringArray == null || stringArray.Length == 0 ) { 
                return null;
            } else {
                return stringArray;
            } 
        }
 
        public static string MakeStringNull(string stringValue) { 
            if ( stringValue == null || stringValue.Length == 0) {
                return null; 
            } else {
                return stringValue;
            }
        } 

        /* 
        // Consider removing. 
        public static string MakeStringEmpty(string stringValue) {
            if ( stringValue == null || stringValue.Length == 0) { 
                return String.Empty;
            } else {
                return stringValue;
            } 
        }
        */ 
 
#if TRAVE
        /* 
        // Consider removing.
        public static int HashCode(object objectValue) {
            if (objectValue == null) {
                return -1; 
            } else {
                return objectValue.GetHashCode(); 
            } 
        }
        */ 
#endif

        public static string ExceptionMessage(Exception exception) {
            if (exception==null) { 
                return string.Empty;
            } 
            if (exception.InnerException==null) { 
                return exception.Message;
            } 
            return exception.Message + " (" + ExceptionMessage(exception.InnerException) + ")";
        }

        public static string ToString(object objectValue) { 
            if (objectValue == null) {
                return "(null)"; 
            } else if (objectValue is string && ((string)objectValue).Length==0) { 
                return "(string.empty)";
            } else if (objectValue is Exception) { 
                return ExceptionMessage(objectValue as Exception);
            } else if (objectValue is IntPtr) {
                return "0x" + ((IntPtr)objectValue).ToString("x");
            } else { 
                return objectValue.ToString();
            } 
        } 
        public static string HashString(object objectValue) {
            if (objectValue == null) { 
                return "(null)";
            } else if (objectValue is string && ((string)objectValue).Length==0) {
                return "(string.empty)";
            } else { 
                return objectValue.GetHashCode().ToString(NumberFormatInfo.InvariantInfo);
            } 
        } 

        public static bool IsInvalidHttpString(string stringValue) { 
            return stringValue.IndexOfAny(InvalidParamChars)!=-1;
        }

        public static bool IsBlankString(string stringValue) { 
            return stringValue==null || stringValue.Length==0;
        } 
 
        /*
        // Consider removing. 
        public static bool ValidateUInt32(long address) {
            // on false, API should throw new ArgumentOutOfRangeException("address");
            return address>=0x00000000 && address<=0xFFFFFFFF;
        } 
        */
 
        public static bool ValidateTcpPort(int port) { 
            // on false, API should throw new ArgumentOutOfRangeException("port");
            return port>=IPEndPoint.MinPort && port<=IPEndPoint.MaxPort; 
        }

        public static bool ValidateRange(int actual, int fromAllowed, int toAllowed) {
            // on false, API should throw new ArgumentOutOfRangeException("argument"); 
            return actual>=fromAllowed && actual<=toAllowed;
        } 
 
        /*
        // Consider removing. 
        public static bool ValidateRange(long actual, long fromAllowed, long toAllowed) {
            // on false, API should throw new ArgumentOutOfRangeException("argument");
            return actual>=fromAllowed && actual<=toAllowed;
        } 
        */
    } 
 
    internal static class ExceptionHelper
    { 
        internal static readonly KeyContainerPermission KeyContainerPermissionOpen = new KeyContainerPermission(KeyContainerPermissionFlags.Open);
        internal static readonly WebPermission WebPermissionUnrestricted = new WebPermission(NetworkAccess.Connect);
        internal static readonly SecurityPermission UnmanagedPermission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
        internal static readonly SocketPermission UnrestrictedSocketPermission = new SocketPermission(PermissionState.Unrestricted); 
        internal static readonly SecurityPermission InfrastructurePermission = new SecurityPermission(SecurityPermissionFlag.Infrastructure);
        internal static readonly SecurityPermission ControlPolicyPermission = new SecurityPermission(SecurityPermissionFlag.ControlPolicy); 
        internal static readonly SecurityPermission ControlPrincipalPermission = new SecurityPermission(SecurityPermissionFlag.ControlPrincipal); 

        internal static NotImplementedException MethodNotImplementedException { 
            get {
                return new NotImplementedException(SR.GetString(SR.net_MethodNotImplementedException));
            }
        } 

        internal static NotImplementedException PropertyNotImplementedException { 
            get { 
                return new NotImplementedException(SR.GetString(SR.net_PropertyNotImplementedException));
            } 
        }

        internal static NotSupportedException MethodNotSupportedException {
            get { 
                return new NotSupportedException(SR.GetString(SR.net_MethodNotSupportedException));
            } 
        } 

        internal static NotSupportedException PropertyNotSupportedException { 
            get {
                return new NotSupportedException(SR.GetString(SR.net_PropertyNotSupportedException));
            }
        } 

        internal static WebException IsolatedException { 
            get { 
                return new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.KeepAliveFailure),WebExceptionStatus.KeepAliveFailure, WebExceptionInternalStatus.Isolated, null);
            } 
        }

        internal static WebException RequestAbortedException {
            get { 
                return new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.RequestCanceled), WebExceptionStatus.RequestCanceled);
            } 
        } 

        internal static UriFormatException BadSchemeException { 
            get {
                return new UriFormatException(SR.GetString(SR.net_uri_BadScheme));
            }
        } 

        internal static UriFormatException BadAuthorityException { 
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_BadAuthority));
            } 
        }

        internal static UriFormatException EmptyUriException {
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_EmptyUri));
            } 
        } 

        internal static UriFormatException SchemeLimitException { 
            get {
                return new UriFormatException(SR.GetString(SR.net_uri_SchemeLimit));
            }
        } 

        internal static UriFormatException SizeLimitException { 
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_SizeLimit));
            } 
        }

        internal static UriFormatException MustRootedPathException {
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_MustRootedPath));
            } 
        } 

        internal static UriFormatException BadHostNameException { 
            get {
                return new UriFormatException(SR.GetString(SR.net_uri_BadHostName));
            }
        } 

        internal static UriFormatException BadPortException { 
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_BadPort));
            } 
        }

        internal static UriFormatException BadAuthorityTerminatorException {
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_BadAuthorityTerminator));
            } 
        } 

        internal static UriFormatException BadFormatException { 
            get {
                return new UriFormatException(SR.GetString(SR.net_uri_BadFormat));
            }
        } 

        internal static UriFormatException CannotCreateRelativeException { 
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_CannotCreateRelative));
            } 
        }

        internal static WebException CacheEntryNotFoundException {
            get { 
                return new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.CacheEntryNotFound), WebExceptionStatus.CacheEntryNotFound);
            } 
        } 

        internal static WebException RequestProhibitedByCachePolicyException { 
            get {
                return new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.RequestProhibitedByCachePolicy), WebExceptionStatus.RequestProhibitedByCachePolicy);
            }
        } 
    }
 
#if !FEATURE_PAL 
    internal enum SecurityStatus
    { 
        // Success / Informational
        OK                          =   0x00000000,
        ContinueNeeded              =   unchecked((int)0x00090312),
        CompleteNeeded              =   unchecked((int)0x00090313), 
        CompAndContinue             =   unchecked((int)0x00090314),
        ContextExpired              =   unchecked((int)0x00090317), 
        CredentialsNeeded           =   unchecked((int)0x00090320), 
        Renegotiate                 =   unchecked((int)0x00090321),
 
        // Errors
        OutOfMemory                 =   unchecked((int)0x80090300),
        InvalidHandle               =   unchecked((int)0x80090301),
        Unsupported                 =   unchecked((int)0x80090302), 
        TargetUnknown               =   unchecked((int)0x80090303),
        InternalError               =   unchecked((int)0x80090304), 
        PackageNotFound             =   unchecked((int)0x80090305), 
        NotOwner                    =   unchecked((int)0x80090306),
        CannotInstall               =   unchecked((int)0x80090307), 
        InvalidToken                =   unchecked((int)0x80090308),
        CannotPack                  =   unchecked((int)0x80090309),
        QopNotSupported             =   unchecked((int)0x8009030A),
        NoImpersonation             =   unchecked((int)0x8009030B), 
        LogonDenied                 =   unchecked((int)0x8009030C),
        UnknownCredentials          =   unchecked((int)0x8009030D), 
        NoCredentials               =   unchecked((int)0x8009030E), 
        MessageAltered              =   unchecked((int)0x8009030F),
        OutOfSequence               =   unchecked((int)0x80090310), 
        NoAuthenticatingAuthority   =   unchecked((int)0x80090311),
        IncompleteMessage           =   unchecked((int)0x80090318),
        IncompleteCredentials       =   unchecked((int)0x80090320),
        BufferNotEnough             =   unchecked((int)0x80090321), 
        WrongPrincipal              =   unchecked((int)0x80090322),
        TimeSkew                    =   unchecked((int)0x80090324), 
        UntrustedRoot               =   unchecked((int)0x80090325), 
        IllegalMessage              =   unchecked((int)0x80090326),
        CertUnknown                 =   unchecked((int)0x80090327), 
        CertExpired                 =   unchecked((int)0x80090328),
        AlgorithmMismatch           =   unchecked((int)0x80090331),
        SecurityQosFailed           =   unchecked((int)0x80090332),
        SmartcardLogonRequired      =   unchecked((int)0x8009033E), 
        UnsupportedPreauth          =   unchecked((int)0x80090343),
    } 
 
    internal enum   ContentTypeValues {
        ChangeCipherSpec    = 0x14, 
        Alert               = 0x15,
        HandShake           = 0x16,
        AppData             = 0x17,
        Unrecognized        = 0xFF, 
    }
 
    internal enum ContextAttribute { 
        //
        // look into <sspi.h> and <schannel.h> 
        //
        Sizes               = 0x00,
        Names               = 0x01,
        Lifespan            = 0x02, 
        DceInfo             = 0x03,
        StreamSizes         = 0x04, 
        //KeyInfo             = 0x05, must not be used, see ConnectionInfo instead 
        Authority           = 0x06,
        // SECPKG_ATTR_PROTO_INFO          = 7, 
        // SECPKG_ATTR_PASSWORD_EXPIRY     = 8,
        // SECPKG_ATTR_SESSION_KEY         = 9,
        PackageInfo         = 0x0A,
        // SECPKG_ATTR_USER_FLAGS          = 11, 
        NegotiationInfo    = 0x0C,
        // SECPKG_ATTR_NATIVE_NAMES        = 13, 
        // SECPKG_ATTR_FLAGS               = 14, 
        // SECPKG_ATTR_USE_VALIDATED       = 15,
        // SECPKG_ATTR_CREDENTIAL_NAME     = 16, 
        // SECPKG_ATTR_TARGET_INFORMATION  = 17,
        // SECPKG_ATTR_ACCESS_TOKEN        = 18,
        // SECPKG_ATTR_TARGET              = 19,
        // SECPKG_ATTR_AUTHENTICATION_ID   = 20, 
        RemoteCertificate   = 0x53,
        LocalCertificate    = 0x54, 
        RootStore           = 0x55, 
        IssuerListInfoEx    = 0x59,
        ConnectionInfo      = 0x5A, 
    }

    internal enum Endianness {
        Network             = 0x00, 
        Native              = 0x10,
    } 
 
    internal enum CredentialUse {
        Inbound             = 0x1, 
        Outbound            = 0x2,
        Both                = 0x3,
    }
 
    internal enum BufferType {
        Empty               = 0x00, 
        Data                = 0x01, 
        Token               = 0x02,
        Parameters          = 0x03, 
        Missing             = 0x04,
        Extra               = 0x05,
        Trailer             = 0x06,
        Header              = 0x07, 
        Padding             = 0x09,    // non-data padding
        Stream              = 0x0A, 
        ReadOnlyFlag        = unchecked((int)0x80000000), 
        ReadOnlyWithChecksum= 0x10000000
    } 

    internal enum ChainPolicyType {
        Base                = 1,
        Authenticode        = 2, 
        Authenticode_TS     = 3,
        SSL                 = 4, 
        BasicConstraints    = 5, 
        NtAuth              = 6,
    } 

    internal enum IgnoreCertProblem {
        not_time_valid              = 0x00000001,
        ctl_not_time_valid          = 0x00000002, 
        not_time_nested             = 0x00000004,
        invalid_basic_constraints   = 0x00000008, 
 
        all_not_time_valid          =
            not_time_valid          | 
            ctl_not_time_valid      |
            not_time_nested,

        allow_unknown_ca            = 0x00000010, 
        wrong_usage                 = 0x00000020,
        invalid_name                = 0x00000040, 
        invalid_policy              = 0x00000080, 
        end_rev_unknown             = 0x00000100,
        ctl_signer_rev_unknown      = 0x00000200, 
        ca_rev_unknown              = 0x00000400,
        root_rev_unknown            = 0x00000800,

        all_rev_unknown             = 
            end_rev_unknown         |
            ctl_signer_rev_unknown  | 
            ca_rev_unknown          | 
            root_rev_unknown,
        none = 
            not_time_valid |
            ctl_not_time_valid |
            not_time_nested |
            invalid_basic_constraints | 
            allow_unknown_ca |
            wrong_usage | 
            invalid_name | 
            invalid_policy |
            end_rev_unknown | 
            ctl_signer_rev_unknown |
            ca_rev_unknown |
            root_rev_unknown
    } 

    internal enum CertUsage { 
        MatchTypeAnd    = 0x00, 
        MatchTypeOr     = 0x01,
    } 

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ChainPolicyParameter {
        public uint cbSize; 
        public uint dwFlags;
        public SSL_EXTRA_CERT_CHAIN_POLICY_PARA* pvExtraPolicyPara; 
 
        public static readonly uint StructSize = (uint) Marshal.SizeOf(typeof(ChainPolicyParameter));
    } 

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SSL_EXTRA_CERT_CHAIN_POLICY_PARA {
 
        [StructLayout(LayoutKind.Explicit)]
        internal struct U { 
              [FieldOffset(0)] internal uint cbStruct;  //DWORD 
              [FieldOffset(0)] internal uint cbSize;    //DWORD
        }; 
        internal U u;
        internal int    dwAuthType;  //DWORD
        internal uint   fdwChecks;   //DWORD
        internal char*  pwszServerName; //WCHAR* // used to check against CN=xxxx 

        internal SSL_EXTRA_CERT_CHAIN_POLICY_PARA(bool amIServer) 
        { 
            u.cbStruct = StructSize;
            u.cbSize   = StructSize; 
            //#      define      AUTHTYPE_CLIENT         1
            //#      define      AUTHTYPE_SERVER         2
            dwAuthType = amIServer? 1: 2;
            fdwChecks = 0; 
            pwszServerName = null;
        } 
        static readonly uint StructSize = (uint) Marshal.SizeOf(typeof(SSL_EXTRA_CERT_CHAIN_POLICY_PARA)); 
    }
 
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ChainPolicyStatus {
        public uint   cbSize;
        public uint   dwError; 
        public uint   lChainIndex;
        public uint   lElementIndex; 
        public void*  pvExtraPolicyStatus; 

        public static readonly uint StructSize = (uint) Marshal.SizeOf(typeof(ChainPolicyStatus)); 
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CertEnhKeyUse { 

        public uint   cUsageIdentifier; 
        public void*  rgpszUsageIdentifier; 

#if TRAVE 
        public override string ToString() {
            return "cUsageIdentifier="+cUsageIdentifier.ToString()+ " rgpszUsageIdentifier=" + new IntPtr(rgpszUsageIdentifier).ToString("x");
        }
#endif 
    };
 
    [StructLayout(LayoutKind.Sequential)] 
    internal struct CertUsageMatch {
        public CertUsage     dwType; 
        public CertEnhKeyUse Usage;
#if TRAVE
        public override string ToString() {
            return "dwType="+dwType.ToString()+" "+Usage.ToString(); 
        }
#endif 
    }; 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct ChainParameters {
        public uint cbSize;
        public CertUsageMatch RequestedUsage;
        public CertUsageMatch RequestedIssuancePolicy; 
        public uint           UrlRetrievalTimeout;
        public int            BoolCheckRevocationFreshnessTime; 
        public uint           RevocationFreshnessTime; 

 
        public static readonly uint StructSize = (uint) Marshal.SizeOf(typeof(ChainParameters));
#if TRAVE
        public override string ToString() {
            return "cbSize="+cbSize.ToString()+" "+RequestedUsage.ToString(); 
        }
#endif 
    }; 

    [StructLayout(LayoutKind.Sequential)] 
    struct _CERT_CHAIN_ELEMENT
    {
        public uint cbSize;
        public IntPtr pCertContext; 
        // Since this structure is allocated by unmanaged code, we can
        // omit the fileds below since we don't need to access them 
        // CERT_TRUST_STATUS   TrustStatus; 
        // IntPtr                pRevocationInfo;
        // IntPtr                pIssuanceUsage; 
        // IntPtr                pApplicationUsage;
    }

    // CRYPTOAPI_BLOB 
    //[StructLayout(LayoutKind.Sequential)]
    //unsafe struct CryptoBlob { 
    //    // public uint cbData; 
    //    // public byte* pbData;
    //    public uint dataSize; 
    //    public byte* dataBlob;
    //}

    // SecPkgContext_IssuerListInfoEx 
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct IssuerListInfoEx { 
        public SafeHandle aIssuers; 
        public uint cIssuers;
 
        public unsafe IssuerListInfoEx(SafeHandle handle, byte[] nativeBuffer) {
            aIssuers = handle;
            fixed(byte* voidPtr = nativeBuffer) {
                // if this breaks on 64 bit, do the sizeof(IntPtr) trick 
                cIssuers = *((uint*)(voidPtr + IntPtr.Size));
            } 
        } 
    }
 

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecureCredential {
 
/*
typedef struct _SCHANNEL_CRED 
{ 
    DWORD           dwVersion;      // always SCHANNEL_CRED_VERSION
    DWORD           cCreds; 
    PCCERT_CONTEXT *paCred;
    HCERTSTORE      hRootStore;

    DWORD           cMappers; 
    struct _HMAPPER **aphMappers;
 
    DWORD           cSupportedAlgs; 
    ALG_ID *        palgSupportedAlgs;
 
    DWORD           grbitEnabledProtocols;
    DWORD           dwMinimumCipherStrength;
    DWORD           dwMaximumCipherStrength;
    DWORD           dwSessionLifespan; 
    DWORD           dwFlags;
    DWORD           reserved; 
} SCHANNEL_CRED, *PSCHANNEL_CRED; 
*/
 
        public const int CurrentVersion = 0x4;

        public int version;
        public int cCreds; 

        // ptr to an array of pointers 
        // There is a hack done with this field.  AcquireCredentialsHandle requires an array of 
        // certificate handles; we only ever use one.  In order to avoid pinning a one element array,
        // we copy this value onto the stack, create a pointer on the stack to the copied value, 
        // and replace this field with the pointer, during the call to AcquireCredentialsHandle.
        // Then we fix it up afterwards.  Fine as long as all the SSPI credentials are not
        // supposed to be threadsafe.
        public IntPtr certContextArray; 

        private readonly IntPtr rootStore;               // == always null, OTHERWISE NOT RELIABLE 
        public int cMappers; 
        private readonly IntPtr phMappers;               // == always null, OTHERWISE NOT RELIABLE
        public int cSupportedAlgs; 
        private readonly IntPtr palgSupportedAlgs;       // == always null, OTHERWISE NOT RELIABLE
        public SchProtocols grbitEnabledProtocols;
        public int dwMinimumCipherStrength;
        public int dwMaximumCipherStrength; 
        public int dwSessionLifespan;
        public SecureCredential.Flags dwFlags; 
        public int reserved; 

        [Flags] 
        public enum Flags {
            Zero            = 0,
            NoSystemMapper  = 0x02,
            NoNameCheck     = 0x04, 
            ValidateManual  = 0x08,
            NoDefaultCred   = 0x10, 
            ValidateAuto    = 0x20 
        }
 
        public SecureCredential(int version, X509Certificate certificate, SecureCredential.Flags flags, SchProtocols protocols) {
            // default values required for a struct
            rootStore = phMappers = palgSupportedAlgs = certContextArray = IntPtr.Zero;
            cCreds = cMappers = cSupportedAlgs = 0; 
            dwMinimumCipherStrength = dwMaximumCipherStrength = 0;
            dwSessionLifespan = reserved = 0; 
            this.version = version; 
            dwFlags = flags;
            grbitEnabledProtocols = protocols; 
            if (certificate != null) {
                certContextArray = certificate.Handle;
                cCreds = 1;
            } 
        }
 
        [System.Diagnostics.Conditional("TRAVE")] 
        internal void DebugDump() {
            GlobalLog.Print("SecureCredential #"+GetHashCode()); 
            GlobalLog.Print("    version                 = " + version);
            GlobalLog.Print("    cCreds                  = " + cCreds);
            GlobalLog.Print("    certContextArray        = " + String.Format("0x{0:x}", certContextArray));
            GlobalLog.Print("    rootStore               = " + String.Format("0x{0:x}", rootStore)); 
            GlobalLog.Print("    cMappers                = " + cMappers);
            GlobalLog.Print("    phMappers               = " + String.Format("0x{0:x}", phMappers)); 
            GlobalLog.Print("    cSupportedAlgs          = " + cSupportedAlgs); 
            GlobalLog.Print("    palgSupportedAlgs       = " + String.Format("0x{0:x}", palgSupportedAlgs));
            GlobalLog.Print("    grbitEnabledProtocols   = " + String.Format("0x{0:x}", grbitEnabledProtocols)); 
            GlobalLog.Print("    dwMinimumCipherStrength = " + dwMinimumCipherStrength);
            GlobalLog.Print("    dwMaximumCipherStrength = " + dwMaximumCipherStrength);
            GlobalLog.Print("    dwSessionLifespan       = " + String.Format("0x{0:x}", dwSessionLifespan));
            GlobalLog.Print("    dwFlags                 = " + String.Format("0x{0:x}", dwFlags)); 
            GlobalLog.Print("    reserved                = " + String.Format("0x{0:x}", reserved));
        } 
 
    } // SecureCredential
 

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SecurityBufferStruct {
        public int          count; 
        public BufferType   type;
        public IntPtr       token; 
 
        public static readonly int Size = sizeof(SecurityBufferStruct);
    } 

    internal class SecurityBuffer {
        public int size;
        public BufferType type; 
        public byte[] token;
        public int offset; 
 
        public SecurityBuffer(byte[] data, int offset, int size, BufferType tokentype) {
            GlobalLog.Assert(offset >= 0 && offset <= (data == null ? 0 : data.Length), "SecurityBuffer::.ctor", "'offset' out of range.  [" + offset + "]"); 
            GlobalLog.Assert(size >= 0 && size <= (data == null ? 0 : data.Length - offset), "SecurityBuffer::.ctor", "'size' out of range.  [" + size + "]");
 			
            this.offset = data == null || offset < 0 ? 0 : Math.Min(offset, data.Length);
            this.size   = data == null || size < 0 ? 0 : Math.Min(size, data.Length - this.offset); 
            this.type   = tokentype;
            this.token  = size == 0 ? null : data; 
        } 

        public SecurityBuffer(byte[] data, BufferType tokentype) { 
            this.size   = data == null ? 0 : data.Length;
            this.type   = tokentype;
            this.token  = size == 0 ? null : data;
        } 

        public SecurityBuffer(int size, BufferType tokentype) { 
            GlobalLog.Assert(size >= 0, "SecurityBuffer::.ctor", "'size' out of range.  [" + size.ToString(NumberFormatInfo.InvariantInfo) + "]"); 

            this.size   = size; 
            this.type   = tokentype;
            this.token  = size == 0 ? null : new byte[size];
        }
    } 

    [StructLayout(LayoutKind.Sequential)] 
    internal unsafe class SecurityBufferDescriptor { 
    /*
    typedef struct _SecBufferDesc { 
        ULONG        ulVersion;
        ULONG        cBuffers;
        PSecBuffer   pBuffers;
    } SecBufferDesc, * PSecBufferDesc; 
    */
            public  readonly    int     Version; 
            public  readonly    int     Count; 
            public  void*       UnmanagedPointer;
 
        public SecurityBufferDescriptor(int count) {
            Version = 0;
            Count = count;
            UnmanagedPointer = null; 
        }
 
        [System.Diagnostics.Conditional("TRAVE")] 
        internal void DebugDump() {
            GlobalLog.Print("SecurityBufferDescriptor #" + ValidationHelper.HashString(this)); 
            GlobalLog.Print("    version             = " + Version);
            GlobalLog.Print("    count               = " + Count);
            GlobalLog.Print("    securityBufferArray = 0x" + (new IntPtr(UnmanagedPointer)).ToString("x"));
        } 
    } // SecurityBufferDescriptor
 
    internal  enum    CertificateEncoding { 
        Zero                     = 0,
        X509AsnEncoding          = unchecked((int)0x00000001), 
        X509NdrEncoding          = unchecked((int)0x00000002),
        Pkcs7AsnEncoding         = unchecked((int)0x00010000),
        Pkcs7NdrEncoding         = unchecked((int)0x00020000),
        AnyAsnEncoding           = X509AsnEncoding|Pkcs7AsnEncoding 
    }
 
    internal  enum    CertificateProblem { 
        OK                          =   0x00000000,
        TrustNOSIGNATURE            = unchecked((int)0x800B0100), 
        CertEXPIRED                 = unchecked((int)0x800B0101),
        CertVALIDITYPERIODNESTING   = unchecked((int)0x800B0102),
        CertROLE                    = unchecked((int)0x800B0103),
        CertPATHLENCONST            = unchecked((int)0x800B0104), 
        CertCRITICAL                = unchecked((int)0x800B0105),
        CertPURPOSE                 = unchecked((int)0x800B0106), 
        CertISSUERCHAINING          = unchecked((int)0x800B0107), 
        CertMALFORMED               = unchecked((int)0x800B0108),
        CertUNTRUSTEDROOT           = unchecked((int)0x800B0109), 
        CertCHAINING                = unchecked((int)0x800B010A),
        CertREVOKED                 = unchecked((int)0x800B010C),
        CertUNTRUSTEDTESTROOT       = unchecked((int)0x800B010D),
        CertREVOCATION_FAILURE      = unchecked((int)0x800B010E), 
        CertCN_NO_MATCH             = unchecked((int)0x800B010F),
        CertWRONG_USAGE             = unchecked((int)0x800B0110), 
        TrustEXPLICITDISTRUST       = unchecked((int)0x800B0111), 
        CertUNTRUSTEDCA             = unchecked((int)0x800B0112),
        CertINVALIDPOLICY           = unchecked((int)0x800B0113), 
        CertINVALIDNAME             = unchecked((int)0x800B0114),

        CryptNOREVOCATIONCHECK       = unchecked((int)0x80092012),
        CryptREVOCATIONOFFLINE       = unchecked((int)0x80092013), 

        TrustSYSTEMERROR            = unchecked((int)0x80096001), 
        TrustNOSIGNERCERT           = unchecked((int)0x80096002), 
        TrustCOUNTERSIGNER          = unchecked((int)0x80096003),
        TrustCERTSIGNATURE          = unchecked((int)0x80096004), 
        TrustTIMESTAMP              = unchecked((int)0x80096005),
        TrustBADDIGEST              = unchecked((int)0x80096010),
        TrustBASICCONSTRAINTS       = unchecked((int)0x80096019),
        TrustFINANCIALCRITERIA      = unchecked((int)0x8009601E), 
    }
#endif // !FEATURE_PAL 
 
    //
    // WebRequestPrefixElement 
    //
    // This is an element of the prefix list. It contains the prefix and the
    // interface to be called to create a request for that prefix.
    // 

    /// <devdoc> 
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    // internal class WebRequestPrefixElement { 
    internal class WebRequestPrefixElement  {

        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public    string              Prefix; 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        internal    IWebRequestCreate   creator;
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        internal    Type   creatorType;
 
        public IWebRequestCreate Creator { 
            get {
                if (creator == null && creatorType != null) { 
                    lock(this) {
                        if (creator == null) {
                            creator = (IWebRequestCreate)Activator.CreateInstance(
                                                        creatorType, 
                                                        BindingFlags.CreateInstance
                                                        | BindingFlags.Instance 
                                                        | BindingFlags.NonPublic 
                                                        | BindingFlags.Public,
                                                        null,          // Binder 
                                                        new object[0], // no arguments
                                                        CultureInfo.InvariantCulture
                                                        );
                        } 
                    }
                } 
 
                return creator;
            } 

            set {
                creator = value;
            } 
        }
 
        public WebRequestPrefixElement(string P, Type creatorType) { 
            // verify that its of the proper type of IWebRequestCreate
            if (!typeof(IWebRequestCreate).IsAssignableFrom(creatorType)) 
            {
                throw new InvalidCastException(SR.GetString(SR.net_invalid_cast,
                                                                creatorType.AssemblyQualifiedName,
                                                                "IWebRequestCreate")); 
            }
 
            Prefix = P; 
            this.creatorType = creatorType;
        } 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public WebRequestPrefixElement(string P, IWebRequestCreate C) {
            Prefix = P; 
            Creator = C; 
        }
 
    } // class PrefixListElement


    // 
    // HttpRequestCreator.
    // 
    // This is the class that we use to create HTTP and HTTPS requests. 
    //
 
    internal class HttpRequestCreator : IWebRequestCreate {

        /*++
 
         Create - Create an HttpWebRequest.
 
            This is our method to create an HttpWebRequest. We register 
            for HTTP and HTTPS Uris, and this method is called when a request
            needs to be created for one of those. 


            Input:
                    Uri             - Uri for request being created. 

            Returns: 
                    The newly created HttpWebRequest. 

         --*/ 

        public WebRequest Create( Uri Uri ) {
            //
            // Note, DNS permissions check will not happen on WebRequest 
            //
            return new HttpWebRequest(Uri, null); 
        } 

    } // class HttpRequestCreator 

    //
    //  CoreResponseData - Used to store result of HTTP header parsing and
    //      response parsing.  Also Contains new stream to use, and 
    //      is used as core of new Response
    // 
    internal class CoreResponseData { 

        // Status Line Response Values 
        public HttpStatusCode m_StatusCode;
        public string m_StatusDescription;
        public bool m_IsVersionHttp11;
 
        // Content Length needed for semantics, -1 if chunked
        public long m_ContentLength; 
 
        // Response Headers
        public WebHeaderCollection m_ResponseHeaders; 

        // ConnectStream - for reading actual data
        public Stream m_ConnectStream;
 
        internal CoreResponseData Clone() {
            CoreResponseData cloneResponseData = new CoreResponseData(); 
            cloneResponseData.m_StatusCode        = m_StatusCode; 
            cloneResponseData.m_StatusDescription = m_StatusDescription;
            cloneResponseData.m_IsVersionHttp11   = m_IsVersionHttp11; 
            cloneResponseData.m_ContentLength     = m_ContentLength;
            cloneResponseData.m_ResponseHeaders   = m_ResponseHeaders;
            cloneResponseData.m_ConnectStream     = m_ConnectStream;
            return cloneResponseData; 
        }
 
    } 

 
    /*++

    StreamChunkBytes - A class to read a chunk stream from a ConnectStream.
 
    A simple little value class that implements the IReadChunkBytes
    interface. 
 
    --*/
    internal class StreamChunkBytes : IReadChunkBytes { 

        public  ConnectStream   ChunkStream;
        public  int             BytesRead = 0;
        public  int             TotalBytesRead = 0; 
        private byte            PushByte;
        private bool            HavePush; 
 
        public StreamChunkBytes(ConnectStream connectStream) {
            ChunkStream = connectStream; 
            return;
        }

        public int NextByte { 
            get {
                if (HavePush) { 
                    HavePush = false; 
                    return PushByte;
                } 

                return ChunkStream.ReadSingleByte();
            }
            set { 
                PushByte = (byte)value;
                HavePush = true; 
            } 
        }
 
    } // class StreamChunkBytes


    internal delegate bool HttpAbortDelegate(HttpWebRequest request, WebException webException); 

    // 
    // this class contains known header names 
    //
 
    internal static class HttpKnownHeaderNames {

        public const string CacheControl = "Cache-Control";
        public const string Connection = "Connection"; 
        public const string Date = "Date";
        public const string KeepAlive = "Keep-Alive"; 
        public const string Pragma = "Pragma"; 
        public const string ProxyConnection = "Proxy-Connection";
        public const string Trailer = "Trailer"; 
        public const string TransferEncoding = "Transfer-Encoding";
        public const string Upgrade = "Upgrade";
        public const string Via = "Via";
        public const string Warning = "Warning"; 
        public const string ContentLength = "Content-Length";
        public const string ContentType = "Content-Type"; 
        public const string ContentEncoding = "Content-Encoding"; 
        public const string ContentLanguage = "Content-Language";
        public const string ContentLocation = "Content-Location"; 
        public const string ContentRange = "Content-Range";
        public const string Expires = "Expires";
        public const string LastModified = "Last-Modified";
        public const string Age = "Age"; 
        public const string Location = "Location";
        public const string ProxyAuthenticate = "Proxy-Authenticate"; 
        public const string RetryAfter = "Retry-After"; 
        public const string Server = "Server";
        public const string SetCookie = "Set-Cookie"; 
        public const string SetCookie2 = "Set-Cookie2";
        public const string Vary = "Vary";
        public const string WWWAuthenticate = "WWW-Authenticate";
        public const string Accept = "Accept"; 
        public const string AcceptCharset = "Accept-Charset";
        public const string AcceptEncoding = "Accept-Encoding"; 
        public const string AcceptLanguage = "Accept-Language"; 
        public const string Authorization = "Authorization";
        public const string Cookie = "Cookie"; 
        public const string Cookie2 = "Cookie2";
        public const string Expect = "Expect";
        public const string From = "From";
        public const string Host = "Host"; 
        public const string IfMatch = "If-Match";
        public const string IfModifiedSince = "If-Modified-Since"; 
        public const string IfNoneMatch = "If-None-Match"; 
        public const string IfRange = "If-Range";
        public const string IfUnmodifiedSince = "If-Unmodified-Since"; 
        public const string MaxForwards = "Max-Forwards";
        public const string ProxyAuthorization = "Proxy-Authorization";
        public const string Referer = "Referer";
        public const string Range = "Range"; 
        public const string UserAgent = "User-Agent";
        public const string ContentMD5 = "Content-MD5"; 
        public const string ETag = "ETag"; 
        public const string TE = "TE";
        public const string Allow = "Allow"; 
        public const string AcceptRanges = "Accept-Ranges";
        public const string P3P = "P3P";
        public const string XPoweredBy = "X-Powered-By";
        public const string XAspNetVersion = "X-AspNet-Version"; 
    }
 
    /// <devdoc> 
    ///    <para>
    ///       Represents the method that will notify callers when a continue has been 
    ///       received by the client.
    ///    </para>
    /// </devdoc>
    // Delegate type for us to notify callers when we receive a continue 
    public delegate void HttpContinueDelegate(int StatusCode, WebHeaderCollection httpHeaders);
 
    // 
    // HttpWriteMode - used to control the way in which an entity Body is posted.
    // 
    enum HttpWriteMode {
        Unknown         = 0,
        ContentLength   = 1,
        Chunked         = 2, 
        Buffer          = 3,
        None            = 4, 
    } 

    // Used by Request to notify Connection that we are no longer holding the Connection (for NTLM connection sharing) 
    delegate void UnlockConnectionDelegate();

    enum HttpBehaviour : byte {
        Unknown                     = 0, 
        HTTP10                      = 1,
        HTTP11PartiallyCompliant    = 2, 
        HTTP11                      = 3, 
    }
 
    internal enum HttpProcessingResult {
        Continue  = 0,
        ReadWait  = 1,
        WriteWait = 2, 
    }
 
    // 
    // HttpVerb - used to define various per Verb Properties
    // 

    //
    // Note - this is a place holder for Verb properties,
    //  the following two bools can most likely be combined into 
    //  a single Enum type.  And the Verb can be incorporated.
    // 
    class KnownHttpVerb { 
        internal string Name; // verb name
 
        internal bool RequireContentBody; // require content body to be sent
        internal bool ContentBodyNotAllowed; // not allowed to send content body
        internal bool ConnectRequest; // special semantics for a connect request
        internal bool ExpectNoContentResponse; // response will not have content body 

        internal KnownHttpVerb(string name, bool requireContentBody, bool contentBodyNotAllowed, bool connectRequest, bool expectNoContentResponse) { 
            Name = name; 
            RequireContentBody = requireContentBody;
            ContentBodyNotAllowed = contentBodyNotAllowed; 
            ConnectRequest = connectRequest;
            ExpectNoContentResponse = expectNoContentResponse;
        }
 
        // Force an an init, before we use them
        private static ListDictionary NamedHeaders; 
 
        // known verbs
        internal static KnownHttpVerb Get; 
        internal static KnownHttpVerb Connect;
        internal static KnownHttpVerb Head;
        internal static KnownHttpVerb Put;
        internal static KnownHttpVerb Post; 
        internal static KnownHttpVerb MkCol;
 
        // 
        // InitializeKnownVerbs - Does basic init for this object,
        //  such as creating defaultings and filling them 
        //
        static KnownHttpVerb() {
            NamedHeaders = new ListDictionary(CaseInsensitiveAscii.StaticInstance);
            Get = new KnownHttpVerb("GET", false, true, false, false); 
            Connect = new KnownHttpVerb("CONNECT", false, true, true, false);
            Head = new KnownHttpVerb("HEAD", false, true, false, true); 
            Put = new KnownHttpVerb("PUT", true, false, false, false); 
            Post = new KnownHttpVerb("POST", true, false, false, false);
            MkCol = new KnownHttpVerb("MKCOL",false,false,false,false); 
            NamedHeaders[Get.Name] = Get;
            NamedHeaders[Connect.Name] = Connect;
            NamedHeaders[Head.Name] = Head;
            NamedHeaders[Put.Name] = Put; 
            NamedHeaders[Post.Name] = Post;
            NamedHeaders[MkCol.Name] = MkCol; 
        } 

        public bool Equals(KnownHttpVerb verb) { 
            return this==verb || string.Compare(Name, verb.Name, StringComparison.OrdinalIgnoreCase)==0;
        }

        public static KnownHttpVerb Parse(string name) { 
            KnownHttpVerb knownHttpVerb = NamedHeaders[name] as KnownHttpVerb;
            if (knownHttpVerb==null) { 
                // unknown verb, default behaviour 
                knownHttpVerb = new KnownHttpVerb(name, false, false, false, false);
            } 
            return knownHttpVerb;
        }
    }
 

    // 
    // HttpProtocolUtils - A collection of utility functions for HTTP usage. 
    //
 
    internal class HttpProtocolUtils {

        private HttpProtocolUtils() {
        } 

        // 
        // extra buffers for build/parsing, recv/send HTTP data, 
        //  at some point we should consolidate
        // 


        // parse String to DateTime format.
        internal static DateTime string2date(String S) { 
            DateTime dtOut;
            if (HttpDateParse.ParseHttpDate(S,out dtOut)) { 
                return dtOut; 
            }
            else { 
                throw new ProtocolViolationException(SR.GetString(SR.net_baddate));
            }

        } 

        // convert Date to String using RFC 1123 pattern 
        internal static string date2string(DateTime D) { 
            DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();
            return D.ToUniversalTime().ToString("R", dateFormat); 
        }
    }

#if !FEATURE_PAL 
    // Proxy class for linking between ICertificatePolicy <--> ICertificateDecider
    internal class  PolicyWrapper { 
        private const uint          IgnoreUnmatchedCN       = 0x00001000; 
        private ICertificatePolicy  fwdPolicy;
        private ServicePoint        srvPoint; 
        private WebRequest          request;

        internal PolicyWrapper(ICertificatePolicy policy, ServicePoint sp, WebRequest wr) {
            this.fwdPolicy = policy; 
            srvPoint = sp;
            request = wr; 
        } 

        public bool Accept(X509Certificate Certificate, int CertificateProblem) { 
            return fwdPolicy.CheckValidationResult(srvPoint, Certificate, request, CertificateProblem);
        }

        internal static uint VerifyChainPolicy(SafeFreeCertChain chainContext, ref ChainPolicyParameter cpp) { 
            GlobalLog.Enter("PolicyWrapper::VerifyChainPolicy", "chainContext="+ chainContext + ", options="+String.Format("0x{0:x}", cpp.dwFlags));
            ChainPolicyStatus status = new ChainPolicyStatus(); 
            status.cbSize = ChainPolicyStatus.StructSize; 
            int errorCode =
                UnsafeNclNativeMethods.NativePKI.CertVerifyCertificateChainPolicy( 
                    (IntPtr) ChainPolicyType.SSL,
                    chainContext,
                    ref cpp,
                    ref status); 

            GlobalLog.Print("PolicyWrapper::VerifyChainPolicy() CertVerifyCertificateChainPolicy returned: " + errorCode); 
#if TRAVE 
            GlobalLog.Print("PolicyWrapper::VerifyChainPolicy() error code: " + status.dwError+String.Format(" [0x{0:x8}", status.dwError) + " " + SecureChannel.MapSecurityStatus(status.dwError) + "]");
#endif 
            GlobalLog.Leave("PolicyWrapper::VerifyChainPolicy", status.dwError.ToString());
            return status.dwError;
        }
 
        private static IgnoreCertProblem MapErrorCode(uint errorCode) {
            switch ((CertificateProblem) errorCode) { 
 
                case CertificateProblem.CertINVALIDNAME :
                case CertificateProblem.CertCN_NO_MATCH : 
                    return IgnoreCertProblem.invalid_name;

                case CertificateProblem.CertINVALIDPOLICY :
                case CertificateProblem.CertPURPOSE : 
                    return IgnoreCertProblem.invalid_policy;
 
                case CertificateProblem.CertEXPIRED : 
                    return IgnoreCertProblem.not_time_valid | IgnoreCertProblem.ctl_not_time_valid;
 
                case CertificateProblem.CertVALIDITYPERIODNESTING :
                    return IgnoreCertProblem.not_time_nested;

                case CertificateProblem.CertCHAINING : 
                case CertificateProblem.CertUNTRUSTEDCA :
                case CertificateProblem.CertUNTRUSTEDROOT : 
                    return IgnoreCertProblem.allow_unknown_ca; 

                case CertificateProblem.CertREVOKED : 
                case CertificateProblem.CertREVOCATION_FAILURE :
                case CertificateProblem.CryptNOREVOCATIONCHECK:
                case CertificateProblem.CryptREVOCATIONOFFLINE:
                    return IgnoreCertProblem.all_rev_unknown; 

                case CertificateProblem.CertROLE: 
                case CertificateProblem.TrustBASICCONSTRAINTS: 
                    return IgnoreCertProblem.invalid_basic_constraints;
 
                case CertificateProblem.CertWRONG_USAGE :
                    return IgnoreCertProblem.wrong_usage;

                default: 
                    return 0;
            } 
        } 

 
        private uint[] GetChainErrors(string hostName, X509Chain chain, ref bool fatalError)
        {
            fatalError = false;
            SafeFreeCertChain chainContext= new SafeFreeCertChain(chain.ChainContext); 
            ArrayList certificateProblems = new ArrayList();
            unsafe { 
                uint status = 0; 
                ChainPolicyParameter cppStruct = new ChainPolicyParameter();
                cppStruct.cbSize  = ChainPolicyParameter.StructSize; 
                cppStruct.dwFlags = 0;

                SSL_EXTRA_CERT_CHAIN_POLICY_PARA eppStruct = new SSL_EXTRA_CERT_CHAIN_POLICY_PARA(false);
                cppStruct.pvExtraPolicyPara = &eppStruct; 

                fixed (char* namePtr = hostName) { 
                    if (ServicePointManager.CheckCertificateName){ 
                        eppStruct.pwszServerName = namePtr;
                    } 

                    while (true) {
                        status = VerifyChainPolicy(chainContext, ref cppStruct);
                        uint ignoreErrorMask = (uint)MapErrorCode(status); 

                        certificateProblems.Add(status); 
 
                        if (status == 0) {  // No more problems with the certificate?
                            break;          // Then break out of the callback loop 
                        }

                        if (ignoreErrorMask == 0) {  // Unrecognized error encountered
                            fatalError = true; 
                            break;
                        } 
                        else { 
                            cppStruct.dwFlags |= ignoreErrorMask;
                            if ((CertificateProblem)status == CertificateProblem.CertCN_NO_MATCH && ServicePointManager.CheckCertificateName) { 
                                eppStruct.fdwChecks = IgnoreUnmatchedCN;
                            }
                        }
                    } 
                }
            } 
 
            return (uint[]) certificateProblems.ToArray(typeof(uint));
        } 

        internal bool CheckErrors(string hostName, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == 0) 
                return Accept(certificate, 0);
 
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0) 
                return Accept(certificate, (int) CertificateProblem.CertCRITICAL); // ToDO, Define an appropriate enum entry
            else { 
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 ||
                    (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                {
                    bool fatalError = false; 
                    uint[] certificateProblems = GetChainErrors(hostName, chain, ref fatalError);
 
                    if (fatalError) { 
                        // By today design we cannot allow decider to ignore a fatal error.
                        // This error is fatal. 
                        Accept(certificate, (int) SecurityStatus.InternalError);
                        return false;
                    }
 

                    if (certificateProblems.Length == 0) 
                        return Accept(certificate, (int) CertificateProblem.OK); 

                    // Run each error through Accept(). 
                    foreach (uint error in certificateProblems)
                        if (!Accept(certificate, (int) error))
                            return false;
                } 
                return true;
            } 
        } 

/* 



 

 
 

 



 

 
 

 



 

 
 

 



*/ 
    }
    // Class implementing default certificate policy 
    internal class DefaultCertPolicy : ICertificatePolicy { 
        public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest request, int problem) {
            return problem == (int)CertificateProblem.OK; 
        }
    }
#endif // !FEATURE_PAL
 
    internal enum TriState {
        Unspecified = -1, 
        False = 0, 
        True = 1
    } 

    internal enum DefaultPorts {
        DEFAULT_FTP_PORT = 21,
        DEFAULT_GOPHER_PORT = 70, 
        DEFAULT_HTTP_PORT = 80,
        DEFAULT_HTTPS_PORT = 443, 
        DEFAULT_NNTP_PORT = 119, 
        DEFAULT_SMTP_PORT = 25,
        DEFAULT_TELNET_PORT = 23 
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    internal struct hostent { 
        public IntPtr   h_name;
        public IntPtr   h_aliases; 
        public short    h_addrtype; 
        public short    h_length;
        public IntPtr   h_addr_list; 
    }


    [StructLayout(LayoutKind.Sequential)] 
    internal struct Blob {
        public int cbSize; 
        public int pBlobData; 
    }
 

    // This is only for internal code path i.e. TLS stream.
    // See comments on GetNextBuffer() method below.
    // 
    internal class SplitWritesState
    { 
        private const int c_SplitEncryptedBuffersSize = 64*1024; 
        private BufferOffsetSize[] _UserBuffers;
        private int _Index; 
        private int _LastBufferConsumed;
        private BufferOffsetSize[] _RealBuffers;

        // 
        internal SplitWritesState(BufferOffsetSize[] buffers)
        { 
            _UserBuffers    = buffers; 
            _LastBufferConsumed = 0;
            _Index          = 0; 
            _RealBuffers = null;
        }
        //
        // Everything was handled 
        //
        internal bool IsDone { 
            get { 
                if (_LastBufferConsumed != 0)
                    return false; 

                for (int index = _Index ;index < _UserBuffers.Length; ++index)
                    if (_UserBuffers[index].Size != 0)
                        return false; 

                return true; 
            } 
        }
        // Encryption takes CPU and if the input is large (like 10 mb) then a delay may 
        // be 30 sec or so. Hence split the ecnrypt and write operations in smaller chunks
        // up to c_SplitEncryptedBuffersSize total.
        // Note that upon return from here EncryptBuffers() may additonally split the input
        // into chunks each <= chkSecureChannel.MaxDataSize (~16k) yet it will complete them all as a single IO. 
        //
        //  Returns null if done, returns the _buffers reference if everything is handled in one shot (also done) 
        // 
        //  Otheriwse returns subsequent BufferOffsetSize[] to encrypt and pass to base IO method
        // 
        internal BufferOffsetSize[] GetNextBuffers()
        {
            int curIndex = _Index;
            int currentTotalSize = 0; 
            int lastChunkSize = 0;
 
            int  firstBufferConsumed = _LastBufferConsumed; 

            for ( ;_Index < _UserBuffers.Length; ++_Index) 
            {
                lastChunkSize = _UserBuffers[_Index].Size-_LastBufferConsumed;

                currentTotalSize += lastChunkSize; 

                if (currentTotalSize > c_SplitEncryptedBuffersSize) 
                { 
                    lastChunkSize -= (currentTotalSize - c_SplitEncryptedBuffersSize);
                    currentTotalSize = c_SplitEncryptedBuffersSize; 
                    break;
                }

                lastChunkSize = 0; 
                _LastBufferConsumed = 0;
            } 
 
            // Are we done done?
            if (currentTotalSize == 0) 
                return null;

             // Do all buffers fit the limit?
            if (firstBufferConsumed == 0 && curIndex == 0 && _Index == _UserBuffers.Length) 
                return _UserBuffers;
 
            // We do have something to split and send out 
            int buffersCount = lastChunkSize == 0? _Index-curIndex: _Index-curIndex+1;
 
            if (_RealBuffers == null || _RealBuffers.Length != buffersCount)
                _RealBuffers = new BufferOffsetSize[buffersCount];

            int j = 0; 
            for (; curIndex < _Index; ++curIndex)
            { 
                _RealBuffers[j++] = new BufferOffsetSize(_UserBuffers[curIndex].Buffer, _UserBuffers[curIndex].Offset + firstBufferConsumed, _UserBuffers[curIndex].Size-firstBufferConsumed, false); 
                firstBufferConsumed = 0;
            } 

            if (lastChunkSize != 0)
            {
                _RealBuffers[j] = new BufferOffsetSize(_UserBuffers[curIndex].Buffer, _UserBuffers[curIndex].Offset + _LastBufferConsumed, lastChunkSize, false); 
                if ((_LastBufferConsumed += lastChunkSize) == _UserBuffers[_Index].Size)
                { 
                    ++_Index; 
                    _LastBufferConsumed = 0;
                } 
            }

            return _RealBuffers;
 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="Internal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.IO; 
    using System.Reflection;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Net.Sockets; 
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning; 
    using System.Security.Cryptography.X509Certificates; 
    using System.Text;
    using System.Text.RegularExpressions; 
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Threading;
    using System.Security.Principal; 
    using System.Security;
    using System.Net.Security; 
    using System.Net.NetworkInformation; 
    using System.Runtime.Serialization;
 
    internal static class IntPtrHelper {
        /*
        // Consider removing.
        internal static IntPtr Add(IntPtr a, IntPtr b) { 
            return (IntPtr) ((long)a + (long)b);
        } 
        */ 
        internal static IntPtr Add(IntPtr a, int b) {
            return (IntPtr) ((long)a + (long)b); 
        }
    }

    internal class InternalException : SystemException 
    {
        internal InternalException() 
        { 
            GlobalLog.Assert("InternalException thrown.");
        } 

        internal InternalException(SerializationInfo serializationInfo, StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        { } 
    }
 
    internal static class NclUtilities 
    {
        /// <devdoc> 
        ///    <para>
        ///       Indicates true if the threadpool is low on threads,
        ///       in this case we need to refuse to start new requests,
        ///       and avoid blocking. 
        ///    </para>
        /// </devdoc> 
        internal static bool IsThreadPoolLow() 
        {
#if !FEATURE_PAL 
            if (ComNetOS.IsAspNetServer)
            {
                return false;
            } 
#endif //!FEATURE_PAL
 
            int workerThreads, completionPortThreads; 
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
 
#if !FEATURE_PAL
            return workerThreads < 2 || (ComNetOS.IsWinNt && completionPortThreads < 2);
#else
            GlobalLog.Assert(completionPortThreads == 0, "completionPortThreads should be zero on the PAL"); 
            return workerThreads < 2;
#endif //!FEATURE_PAL 
        } 

 
        internal static bool HasShutdownStarted
        {
            get
            { 
                return Environment.HasShutdownStarted || AppDomain.CurrentDomain.IsFinalizingForUnload();
            } 
        } 

        // This only works for context-destroying errors. 
        internal static bool IsCredentialFailure(SecurityStatus error)
        {
            return error == SecurityStatus.LogonDenied ||
                error == SecurityStatus.UnknownCredentials || 
                error == SecurityStatus.NoImpersonation ||
                error == SecurityStatus.NoAuthenticatingAuthority || 
                error == SecurityStatus.UntrustedRoot || 
                error == SecurityStatus.CertExpired ||
                error == SecurityStatus.SmartcardLogonRequired; 
        }

        // This only works for context-destroying errors.
        internal static bool IsClientFault(SecurityStatus error) 
        {
            return error == SecurityStatus.InvalidToken || 
                error == SecurityStatus.CannotPack || 
                error == SecurityStatus.QopNotSupported ||
                error == SecurityStatus.NoCredentials || 
                error == SecurityStatus.MessageAltered ||
                error == SecurityStatus.OutOfSequence ||
                error == SecurityStatus.IncompleteMessage ||
                error == SecurityStatus.IncompleteCredentials || 
                error == SecurityStatus.WrongPrincipal ||
                error == SecurityStatus.TimeSkew || 
                error == SecurityStatus.IllegalMessage || 
                error == SecurityStatus.CertUnknown ||
                error == SecurityStatus.AlgorithmMismatch || 
                error == SecurityStatus.SecurityQosFailed ||
                error == SecurityStatus.UnsupportedPreauth;
        }
 

        // ContextRelativeDemand 
        // Allows easily demanding a permission against a given ExecutionContext. 
        // Have requested the CLR to provide this method on ExecutionContext.
        private static ContextCallback s_ContextRelativeDemandCallback; 

        internal static ContextCallback ContextRelativeDemandCallback
        {
            get 
            {
                if (s_ContextRelativeDemandCallback == null) 
                    s_ContextRelativeDemandCallback = new ContextCallback(DemandCallback); 
                return s_ContextRelativeDemandCallback;
            } 
        }

        private static void DemandCallback(object state)
        { 
            ((CodeAccessPermission) state).Demand();
        } 
 
        // This is for checking if a hostname probably refers to this machine without going to DNS.
        internal static bool GuessWhetherHostIsLoopback(string host) 
        {
            string hostLower = host.ToLowerInvariant();
            if (hostLower == "localhost" || hostLower == "loopback")
            { 
                return true;
            } 
 
#if !FEATURE_PAL
            IPGlobalProperties ip = IPGlobalProperties.InternalGetIPGlobalProperties(); 
            string hostnameLower = ip.HostName.ToLowerInvariant();
            return hostLower == hostnameLower || hostLower == hostnameLower + "." + ip.DomainName.ToLowerInvariant();
#else
            return false; 
#endif
        } 
 
        internal static bool IsFatal(Exception exception)
        { 
            return exception != null && (exception is OutOfMemoryException || exception is StackOverflowException || exception is ThreadAbortException);
        }

        // Need a fast cached list of local addresses for internal use. 
        private static IPAddress[] _LocalAddresses;
        private static object _LocalAddressesLock; 
        private static NetworkAddressChangePolled s_AddressChange; 

#if !FEATURE_PAL 
        internal static IPAddress[] LocalAddresses
        {
            get
            { 
                if (s_AddressChange != null && s_AddressChange.CheckAndReset())
                { 
                    return (_LocalAddresses = GetLocalAddresses()); 
                }
 
                if (_LocalAddresses != null)
                {
                    return _LocalAddresses;
                } 

                lock (LocalAddressesLock) 
                { 
                    if (_LocalAddresses != null)
                    { 
                        return _LocalAddresses;
                    }

                    s_AddressChange = new NetworkAddressChangePolled(); 

                    return (_LocalAddresses = GetLocalAddresses()); 
                } 
            }
        } 

        private static IPAddress[] GetLocalAddresses()
        {
            IPAddress[] local; 

            // Different discovery for pre or post XP 
            if (ComNetOS.IsPostWin2K) 
            {
                ArrayList collections = new ArrayList(16); 
                int total = 0;

                SafeLocalFree buffer = null;
                GetAdaptersAddressesFlags gaaFlags = GetAdaptersAddressesFlags.SkipAnycast | GetAdaptersAddressesFlags.SkipMulticast | 
                    GetAdaptersAddressesFlags.SkipFriendlyName | GetAdaptersAddressesFlags.SkipDnsServer;
                uint size = 0; 
                uint result = UnsafeNetInfoNativeMethods.GetAdaptersAddresses(AddressFamily.Unspecified, (uint)gaaFlags, IntPtr.Zero, SafeLocalFree.Zero, ref size); 
                while (result == IpHelperErrors.ErrorBufferOverflow)
                { 
                    try
                    {
                        buffer = SafeLocalFree.LocalAlloc((int)size);
                        result = UnsafeNetInfoNativeMethods.GetAdaptersAddresses(AddressFamily.Unspecified, (uint)gaaFlags, IntPtr.Zero, buffer, ref size); 

                        if (result == IpHelperErrors.Success) 
                        { 
                            IpAdapterAddresses adapterAddresses = (IpAdapterAddresses)Marshal.PtrToStructure(buffer.DangerousGetHandle(), typeof(IpAdapterAddresses));
 
                            while (true)
                            {
                                if (adapterAddresses.FirstUnicastAddress != IntPtr.Zero)
                                { 
                                    UnicastIPAddressInformationCollection coll = SystemUnicastIPAddressInformation.ToAddressInformationCollection(adapterAddresses.FirstUnicastAddress);
                                    total += coll.Count; 
                                    collections.Add(coll); 
                                }
 
                                if (adapterAddresses.next == IntPtr.Zero)
                                {
                                    break;
                                } 

                                adapterAddresses = (IpAdapterAddresses)Marshal.PtrToStructure(adapterAddresses.next, typeof(IpAdapterAddresses)); 
                            } 
                        }
                    } 
                    finally
                    {
                        if (buffer != null)
                            buffer.Close(); 
                        buffer = null;
                    } 
                } 

                if (result != IpHelperErrors.Success && result != IpHelperErrors.ErrorNoData) 
                {
                    throw new NetworkInformationException((int)result);
                }
 
                local = new IPAddress[total];
                uint i = 0; 
                foreach (UnicastIPAddressInformationCollection coll in collections) 
                {
                    foreach (IPAddressInformation info in coll) 
                    {
                        local[i++] = info.Address;
                    }
                } 
            }
            else 
            { 
                ArrayList collections = new ArrayList(16);
                int total = 0; 

                SafeLocalFree buffer = null;
                uint size = 0;
                uint result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(SafeLocalFree.Zero, ref size); 
                while (result == IpHelperErrors.ErrorBufferOverflow)
                { 
                    try 
                    {
                        buffer = SafeLocalFree.LocalAlloc((int)size); 
                        result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(buffer, ref size);

                        if (result == IpHelperErrors.Success)
                        { 
                            IpAdapterInfo adapterInfo = (IpAdapterInfo)Marshal.PtrToStructure(buffer.DangerousGetHandle(), typeof(IpAdapterInfo));
 
                            while (true) 
                            {
                                IPAddressCollection coll = adapterInfo.ipAddressList.ToIPAddressCollection(); 
                                total += coll.Count;
                                collections.Add(coll);

                                if (adapterInfo.Next == IntPtr.Zero) 
                                {
                                    break; 
                                } 

                                adapterInfo = (IpAdapterInfo)Marshal.PtrToStructure(adapterInfo.Next, typeof(IpAdapterInfo)); 
                            }
                        }
                    }
                    finally 
                    {
                        if (buffer != null) 
                            buffer.Close(); 
                    }
                } 


                if (result != IpHelperErrors.Success && result != IpHelperErrors.ErrorNoData)
                { 
                    throw new NetworkInformationException((int)result);
                } 
 
                local = new IPAddress[total];
                uint i = 0; 
                foreach (IPAddressCollection coll in collections)
                {
                    foreach (IPAddress info in coll)
                    { 
                        local[i++] = info;
                    } 
                } 
            }
            return local; 
        }

        internal static bool IsAddressLocal(IPAddress ipAddress) {
            IPAddress[] localAddresses = NclUtilities.LocalAddresses; 
            for (int i = 0; i < localAddresses.Length; i++)
            { 
                if (ipAddress.Equals(localAddresses[i], false)) 
                {
                    return true; 
                }
            }
            return false;
        } 

#else // !FEATURE_PAL 
        private const int HostNameBufferLength = 256; 
        internal static string _LocalDomainName;
 
        // Copied from the old version of DNS.cs
        // Returns a list of our local addresses by calling gethostbyname with null.
        //
        private static IPHostEntry GetLocalHost() 
        {
            // 
            // IPv6 Changes: If IPv6 is enabled, we can't simply use the 
            //               old IPv4 gethostbyname(null). Instead we need
            //               to do a more complete lookup. 
            //
            if (Socket.SupportsIPv6)
            {
                // 
                // IPv6 enabled: use getaddrinfo() of the local host name
                // to obtain this information. Need to get the machines 
                // name as well - do that here so that we don't need to 
                // Assert DNS permissions.
                // 
                StringBuilder hostname = new StringBuilder(HostNameBufferLength);
                SocketError errorCode =
                    UnsafeNclNativeMethods.OSSOCK.gethostname(
                    hostname, 
                    HostNameBufferLength);
 
                if (errorCode != SocketError.Success) 
                {
                    throw new SocketException(); 
                }

                return Dns.GetHostByName(hostname.ToString());
            } 
            else
            { 
                // 
                // IPv6 disabled: use gethostbyname() to obtain information.
                // 
                IntPtr nativePointer =
                    UnsafeNclNativeMethods.OSSOCK.gethostbyname(
                    null);
 
                if (nativePointer == IntPtr.Zero)
                { 
                    throw new SocketException(); 
                }
 
                return Dns.NativeToHostEntry(nativePointer);
            }

        } // GetLocalHost 

        internal static IPAddress[] LocalAddresses 
        { 
            get
            { 
                IPAddress[] local = _LocalAddresses;
                if (local != null)
                {
                    return local; 
                }
 
                lock (LocalAddressesLock) 
                {
                    local = _LocalAddresses; 
                    if (local != null)
                    {
                        return local;
                    } 

                    List<IPAddress> localList = new List<IPAddress>(); 
 
                        try
                        { 
                            IPHostEntry hostEntry = GetLocalHost();
                            if (hostEntry != null)
                            {
                                if (hostEntry.HostName != null) 
                                {
                                    int dot = hostEntry.HostName.IndexOf('.'); 
                                    if (dot != -1) 
                                    {
                                        _LocalDomainName = hostEntry.HostName.Substring(dot); 
                                    }
                                }

                                IPAddress[] ipAddresses = hostEntry.AddressList; 
                                if (ipAddresses != null)
                                { 
                                    foreach (IPAddress ipAddress in ipAddresses) 
                                    {
                                        localList.Add(ipAddress); 
                                    }
                                }
                            }
                        } 
                        catch
                        { 
                        } 

                    local = new IPAddress[localList.Count]; 
                    int index = 0;
                    foreach (IPAddress ipAddress in localList)
                    {
                        local[index] = ipAddress; 
                        index++;
                    } 
                    _LocalAddresses = local; 

                    return local; 
                }
            }
        }
#endif // !FEATURE_PAL 

        private static object LocalAddressesLock 
        { 
            get
            { 
                if (_LocalAddressesLock == null)
                {
                    Interlocked.CompareExchange(ref _LocalAddressesLock, new object(), null);
                } 
                return _LocalAddressesLock;
            } 
        } 
    }
 
    internal static class NclConstants
    {
        internal static readonly object Sentinel = new object();
        internal static readonly object[] EmptyObjectArray = new object[0]; 
        internal static readonly Uri[] EmptyUriArray = new Uri[0];
 
        internal static readonly byte[] CRLF = new byte[] {(byte) '\r', (byte) '\n'}; 
        internal static readonly byte[] ChunkTerminator = new byte[] {(byte) '0', (byte) '\r', (byte) '\n', (byte) '\r', (byte) '\n'};
    } 

    //
    // A simple sync point, useful for deferring work.  Just an int value with helper methods.
    // 
    // The two events being synchronized are the "Triggering" event and the "Completing" event.  The Triggering event
    // marks the gate as being active; the first subsequent Completing event handles the action. 
    // 
    // First, a thread calls Trigger() to set the trigger on the gate.  This means it needs some work to be done, but only
    // after another operation (the Completing event) finishes.  If Trigger() returns false, the Completing event already 
    // happened.  When the Completing event occurs, that thread calls Complete().  It returns true if the gate has been
    // previously triggered and the caller is the first one to complete it.  The caller should then handle the pending work item.
    //
    // StartTrigger()/FinishTrigger() can be used instead of Trigger() if the triggering thread needs to set up some state 
    // (e.g. the pending work item).  It will block Complete() in a spin-lock.
    // 
    internal struct InterlockedGate 
    {
        private int m_State; 

        internal const int Open = 0;        // Initial state of gate.
        internal const int Held = 1;        // Gate is being actively held by a thread - indeterminate state.
        internal const int Triggered = 2;   // The triggering event has occurred. 
        internal const int Closed = 3;      // The gated event is done.
 
#if DEBUG 
        /* Consider removing
        internal int State 
        {
            get
            {
                return m_State; 
            }
        } 
        */ 
#endif
 
        // Only call when all threads are guaranteed to be done with the gate.
        internal void Reset()
        {
            m_State = Open; 
        }
 
        // Returns false if the gate is already closed or triggered.  If exclusive is true, throws if the gate is already 
        // triggered.
        internal bool Trigger(bool exclusive) 
        {
            int gate = Interlocked.CompareExchange(ref m_State, Triggered, Open);
            if (exclusive && (gate == Held || gate == Triggered))
            { 
                GlobalLog.Assert("InterlockedGate::Trigger", "Gate already triggered.");
                throw new InternalException(); 
            } 
            return gate == Open;
        } 

        // Use StartTrigger() and FinishTrigger() to trigger the gate as a two step operation.  This is useful to set up an invariant
        // that must be ready by the time another thread closes the gate.  Do not block between StartTrigger() and FinishTrigger(), just
        // set up your state to be consistent.  If this method returns true, FinishTrigger() *must* be called to avoid deadlock - do 
        // it in a finally.
        // 
        // Returns false if the gate is already closed or triggered.  If exclusive is true, throws if the gate is already 
        // triggered.
        internal bool StartTrigger(bool exclusive) 
        {
            int gate = Interlocked.CompareExchange(ref m_State, Held, Open);
            if (exclusive && (gate == Held || gate == Triggered))
            { 
                GlobalLog.Assert("InterlockedGate::StartTrigger", "Gate already triggered.");
                throw new InternalException(); 
            } 
            return gate == Open;
        } 

        // Gate must be held by StartTrigger().
        internal void FinishTrigger()
        { 
            int gate = Interlocked.CompareExchange(ref m_State, Triggered, Held);
            if (gate != Held) 
            { 
                GlobalLog.Assert("InterlockedGate::FinishTrigger", "Gate not held.");
                throw new InternalException(); 
            }
        }

        // Returns false if the gate had never been triggered or is already closed. 
        internal bool Complete()
        { 
            int gate; 

            // Spin while the gate is being held, allowing the other thread to set invariants up. 
            while ((gate = Interlocked.CompareExchange(ref m_State, Closed, Triggered)) != Triggered)
            {
                if (gate == Closed)
                { 
                    return false;
                } 
 
                if (gate == Open)
                { 
                    if (Interlocked.CompareExchange(ref m_State, Closed, Open) == Open)
                    {
                        return false;
                    } 

                    continue; 
                } 

                // gate == Held 
                Thread.SpinWait(1);
            }

            return true; 
        }
    } 
 
#if !FEATURE_PAL
    // 
    // A polling implementation of NetworkAddressChange.
    //
    internal class NetworkAddressChangePolled : IDisposable
    { 
        private bool disposed;
        private SafeCloseSocketAndEvent ipv4Socket = null; 
        private SafeCloseSocketAndEvent ipv6Socket = null; 

 
        internal unsafe NetworkAddressChangePolled()
        {
            Socket.InitializeSockets();
            int blocking; 
            if (Socket.SupportsIPv4)
            { 
                blocking = -1; 
                ipv4Socket = SafeCloseSocketAndEvent.CreateWSASocketWithEvent(AddressFamily.InterNetwork, SocketType.Dgram, (ProtocolType)0, true, false);
                UnsafeNclNativeMethods.OSSOCK.ioctlsocket(ipv4Socket, IoctlSocketConstants.FIONBIO, ref blocking); 
            }

            if(Socket.OSSupportsIPv6){
                blocking = -1; 
                ipv6Socket = SafeCloseSocketAndEvent.CreateWSASocketWithEvent(AddressFamily.InterNetworkV6, SocketType.Dgram, (ProtocolType)0, true, false);
                UnsafeNclNativeMethods.OSSOCK.ioctlsocket(ipv6Socket,IoctlSocketConstants.FIONBIO,ref blocking); 
            } 
            Setup(StartIPOptions.Both);
        } 

        private unsafe void Setup(StartIPOptions startIPOptions)
        {
            int length; 
            SocketError errorCode;
 
 
            if (Socket.SupportsIPv4 && (startIPOptions & StartIPOptions.StartIPv4) != 0){
                errorCode = (SocketError)UnsafeNclNativeMethods.OSSOCK.WSAIoctl_Blocking( 
                    ipv4Socket.DangerousGetHandle(),
                    (int) IOControlCode.AddressListChange,
                    null, 0, null, 0,
                    out length, 
                    IntPtr.Zero, IntPtr.Zero);
 
                if (errorCode != SocketError.Success) { 
                    NetworkInformationException exception = new NetworkInformationException();
                    if (exception.ErrorCode != (uint)SocketError.WouldBlock) { 
                        Dispose();
                        return;
                    }
                } 

                errorCode = (SocketError)UnsafeNclNativeMethods.OSSOCK.WSAEventSelect(ipv4Socket, ipv4Socket.GetEventHandle().SafeWaitHandle, AsyncEventBits.FdAddressListChange); 
                if (errorCode != SocketError.Success) { 
                    Dispose();
                    return; 
                }
            }

            if(Socket.OSSupportsIPv6 && (startIPOptions & StartIPOptions.StartIPv6) !=0){ 
                errorCode = (SocketError) UnsafeNclNativeMethods.OSSOCK.WSAIoctl_Blocking(
                    ipv6Socket.DangerousGetHandle(), 
                    (int) IOControlCode.AddressListChange, 
                    null, 0, null, 0,
                    out length, 
                    IntPtr.Zero, IntPtr.Zero);

                if (errorCode != SocketError.Success) {
                    NetworkInformationException exception = new NetworkInformationException(); 
                    if (exception.ErrorCode != (uint)SocketError.WouldBlock) {
                        Dispose(); 
                        return; 
                    }
                } 

                errorCode = (SocketError)UnsafeNclNativeMethods.OSSOCK.WSAEventSelect(ipv6Socket, ipv6Socket.GetEventHandle().SafeWaitHandle, AsyncEventBits.FdAddressListChange);
                if (errorCode != SocketError.Success) {
                    Dispose(); 
                    return;
                } 
            } 
        }
 
        internal bool CheckAndReset()
        {
            if(!disposed){
                lock (this){ 
                    if (!disposed){
                        StartIPOptions options = StartIPOptions.None; 
 
                        if (ipv4Socket != null && ipv4Socket.GetEventHandle().WaitOne(0, false)){
                            options|= StartIPOptions.StartIPv4; 
                        }
                        if (ipv6Socket != null && ipv6Socket.GetEventHandle().WaitOne(0, false))
                        {
                            options|= StartIPOptions.StartIPv6; 
                        }
 
                        if(options != StartIPOptions.None){ 
                            Setup(options);
                            return true; 
                        }
                    }
                }
            } 
            return false;
        } 
 
        public void Dispose()
        { 
            if(!disposed){
                lock (this){
                    if (!disposed){
                        if(ipv6Socket != null){ 
                            ipv6Socket.Close();
                            ipv6Socket = null; 
                        } 
                        if(ipv4Socket != null){
                            ipv4Socket.Close(); 
                            ipv6Socket = null;
                        }
                        disposed = true;
                    } 
                }
            } 
        } 
    }
#endif // FEATURE_PAL 



#if !FEATURE_PAL 
    internal static class ComNetOS
    { 
        internal static readonly bool IsWin9x; 
        internal static readonly bool IsWinNt;
        internal static readonly bool IsWin2K; 
        internal static readonly bool IsPostWin2K;    // ie: XP or later but not Win2K
        internal static readonly bool IsAspNetServer; // ie: running under ASP+
        internal static readonly bool IsWinHttp51;    // Is WinHttp 5.1 available.
        internal static readonly bool IsWin2k3;       // Is Windows Server 2003 or later. 
        internal static readonly bool IsXpSp2;        // Is Windows XP sp2 or later
        internal static readonly bool IsWin2k3Sp1;    // Is Windows 2003 sp1 or later 
        internal static readonly bool IsVista;        // Is Windows Vista or later 

        // We use it safe so assert 
        [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.AppDomain, ResourceScope.AppDomain)]
        static ComNetOS() 
        {
            OperatingSystem operatingSystem = Environment.OSVersion; 
 
            GlobalLog.Print("ComNetOS::.ctor(): " + operatingSystem.ToString());
 
            if (operatingSystem.Platform == PlatformID.Win32Windows)
            {
                IsWin9x = true;
                return; 
            }
 
            // 
            // Detect ASP+ as a platform running under NT
            // 

            try
            {
                IsAspNetServer = (Thread.GetDomain().GetData(".appDomain") != null); 
            }
            catch { } 
 
            //
            // Platform is Windows NT or later 
            // NT4: 4.0
            // 2000: 5.0
            // XP: 5.1
            // Win2k3: 5.2 
            //
            IsWinNt = true; 
 
            //
            // Platform is Windows NT 2K or later 
            // operatingSystem.Version.Major>=5
            //
            IsWin2K = true;
 
            if (operatingSystem.Version.Major == 5 && operatingSystem.Version.Minor == 0)
            { 
                IsWinHttp51 = operatingSystem.Version.MajorRevision >= 3;    // Win2k SP3 
                return;
            } 

            IsPostWin2K = true;

            if ((operatingSystem.Version.Major == 5 
                && operatingSystem.Version.Minor == 1
                && operatingSystem.Version.MajorRevision >=2) 
                ||(operatingSystem.Version.Major >= 6)) 
            {
                IsXpSp2 = true;        // WinXP SP2 
            }

            if (operatingSystem.Version.Major == 5 && operatingSystem.Version.Minor == 1)
            { 
                IsWinHttp51 = operatingSystem.Version.MajorRevision >= 1;    // WinXP SP1
 
                return; 
            }
 
            IsWinHttp51 = true;
            IsWin2k3 = true;

            if ((operatingSystem.Version.Major == 5 
                && operatingSystem.Version.Minor == 2
                && operatingSystem.Version.MajorRevision >= 1) 
                || (operatingSystem.Version.Major >= 6)) 
            {
                IsWin2k3Sp1 = true;    // Win2k3 SP1 
            }

            if (operatingSystem.Version.Major >= 6)
            { 
                IsVista = true;
            } 
        } 
    }
#endif 


    //
    // support class for Validation related stuff. 
    //
    internal static class ValidationHelper { 
 
        public static string [] EmptyArray = new string[0];
 
        internal static readonly char[]  InvalidMethodChars =
                new char[]{
                ' ',
                '\r', 
                '\n',
                '\t' 
                }; 

        // invalid characters that cannot be found in a valid method-verb or http header 
        internal static readonly char[]  InvalidParamChars =
                new char[]{
                '(',
                ')', 
                '<',
                '>', 
                '@', 
                ',',
                ';', 
                ':',
                '\\',
                '"',
                '\'', 
                '/',
                '[', 
                ']', 
                '?',
                '=', 
                '{',
                '}',
                ' ',
                '\t', 
                '\r',
                '\n'}; 
 
        public static string [] MakeEmptyArrayNull(string [] stringArray) {
            if ( stringArray == null || stringArray.Length == 0 ) { 
                return null;
            } else {
                return stringArray;
            } 
        }
 
        public static string MakeStringNull(string stringValue) { 
            if ( stringValue == null || stringValue.Length == 0) {
                return null; 
            } else {
                return stringValue;
            }
        } 

        /* 
        // Consider removing. 
        public static string MakeStringEmpty(string stringValue) {
            if ( stringValue == null || stringValue.Length == 0) { 
                return String.Empty;
            } else {
                return stringValue;
            } 
        }
        */ 
 
#if TRAVE
        /* 
        // Consider removing.
        public static int HashCode(object objectValue) {
            if (objectValue == null) {
                return -1; 
            } else {
                return objectValue.GetHashCode(); 
            } 
        }
        */ 
#endif

        public static string ExceptionMessage(Exception exception) {
            if (exception==null) { 
                return string.Empty;
            } 
            if (exception.InnerException==null) { 
                return exception.Message;
            } 
            return exception.Message + " (" + ExceptionMessage(exception.InnerException) + ")";
        }

        public static string ToString(object objectValue) { 
            if (objectValue == null) {
                return "(null)"; 
            } else if (objectValue is string && ((string)objectValue).Length==0) { 
                return "(string.empty)";
            } else if (objectValue is Exception) { 
                return ExceptionMessage(objectValue as Exception);
            } else if (objectValue is IntPtr) {
                return "0x" + ((IntPtr)objectValue).ToString("x");
            } else { 
                return objectValue.ToString();
            } 
        } 
        public static string HashString(object objectValue) {
            if (objectValue == null) { 
                return "(null)";
            } else if (objectValue is string && ((string)objectValue).Length==0) {
                return "(string.empty)";
            } else { 
                return objectValue.GetHashCode().ToString(NumberFormatInfo.InvariantInfo);
            } 
        } 

        public static bool IsInvalidHttpString(string stringValue) { 
            return stringValue.IndexOfAny(InvalidParamChars)!=-1;
        }

        public static bool IsBlankString(string stringValue) { 
            return stringValue==null || stringValue.Length==0;
        } 
 
        /*
        // Consider removing. 
        public static bool ValidateUInt32(long address) {
            // on false, API should throw new ArgumentOutOfRangeException("address");
            return address>=0x00000000 && address<=0xFFFFFFFF;
        } 
        */
 
        public static bool ValidateTcpPort(int port) { 
            // on false, API should throw new ArgumentOutOfRangeException("port");
            return port>=IPEndPoint.MinPort && port<=IPEndPoint.MaxPort; 
        }

        public static bool ValidateRange(int actual, int fromAllowed, int toAllowed) {
            // on false, API should throw new ArgumentOutOfRangeException("argument"); 
            return actual>=fromAllowed && actual<=toAllowed;
        } 
 
        /*
        // Consider removing. 
        public static bool ValidateRange(long actual, long fromAllowed, long toAllowed) {
            // on false, API should throw new ArgumentOutOfRangeException("argument");
            return actual>=fromAllowed && actual<=toAllowed;
        } 
        */
    } 
 
    internal static class ExceptionHelper
    { 
        internal static readonly KeyContainerPermission KeyContainerPermissionOpen = new KeyContainerPermission(KeyContainerPermissionFlags.Open);
        internal static readonly WebPermission WebPermissionUnrestricted = new WebPermission(NetworkAccess.Connect);
        internal static readonly SecurityPermission UnmanagedPermission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
        internal static readonly SocketPermission UnrestrictedSocketPermission = new SocketPermission(PermissionState.Unrestricted); 
        internal static readonly SecurityPermission InfrastructurePermission = new SecurityPermission(SecurityPermissionFlag.Infrastructure);
        internal static readonly SecurityPermission ControlPolicyPermission = new SecurityPermission(SecurityPermissionFlag.ControlPolicy); 
        internal static readonly SecurityPermission ControlPrincipalPermission = new SecurityPermission(SecurityPermissionFlag.ControlPrincipal); 

        internal static NotImplementedException MethodNotImplementedException { 
            get {
                return new NotImplementedException(SR.GetString(SR.net_MethodNotImplementedException));
            }
        } 

        internal static NotImplementedException PropertyNotImplementedException { 
            get { 
                return new NotImplementedException(SR.GetString(SR.net_PropertyNotImplementedException));
            } 
        }

        internal static NotSupportedException MethodNotSupportedException {
            get { 
                return new NotSupportedException(SR.GetString(SR.net_MethodNotSupportedException));
            } 
        } 

        internal static NotSupportedException PropertyNotSupportedException { 
            get {
                return new NotSupportedException(SR.GetString(SR.net_PropertyNotSupportedException));
            }
        } 

        internal static WebException IsolatedException { 
            get { 
                return new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.KeepAliveFailure),WebExceptionStatus.KeepAliveFailure, WebExceptionInternalStatus.Isolated, null);
            } 
        }

        internal static WebException RequestAbortedException {
            get { 
                return new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.RequestCanceled), WebExceptionStatus.RequestCanceled);
            } 
        } 

        internal static UriFormatException BadSchemeException { 
            get {
                return new UriFormatException(SR.GetString(SR.net_uri_BadScheme));
            }
        } 

        internal static UriFormatException BadAuthorityException { 
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_BadAuthority));
            } 
        }

        internal static UriFormatException EmptyUriException {
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_EmptyUri));
            } 
        } 

        internal static UriFormatException SchemeLimitException { 
            get {
                return new UriFormatException(SR.GetString(SR.net_uri_SchemeLimit));
            }
        } 

        internal static UriFormatException SizeLimitException { 
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_SizeLimit));
            } 
        }

        internal static UriFormatException MustRootedPathException {
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_MustRootedPath));
            } 
        } 

        internal static UriFormatException BadHostNameException { 
            get {
                return new UriFormatException(SR.GetString(SR.net_uri_BadHostName));
            }
        } 

        internal static UriFormatException BadPortException { 
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_BadPort));
            } 
        }

        internal static UriFormatException BadAuthorityTerminatorException {
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_BadAuthorityTerminator));
            } 
        } 

        internal static UriFormatException BadFormatException { 
            get {
                return new UriFormatException(SR.GetString(SR.net_uri_BadFormat));
            }
        } 

        internal static UriFormatException CannotCreateRelativeException { 
            get { 
                return new UriFormatException(SR.GetString(SR.net_uri_CannotCreateRelative));
            } 
        }

        internal static WebException CacheEntryNotFoundException {
            get { 
                return new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.CacheEntryNotFound), WebExceptionStatus.CacheEntryNotFound);
            } 
        } 

        internal static WebException RequestProhibitedByCachePolicyException { 
            get {
                return new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.RequestProhibitedByCachePolicy), WebExceptionStatus.RequestProhibitedByCachePolicy);
            }
        } 
    }
 
#if !FEATURE_PAL 
    internal enum SecurityStatus
    { 
        // Success / Informational
        OK                          =   0x00000000,
        ContinueNeeded              =   unchecked((int)0x00090312),
        CompleteNeeded              =   unchecked((int)0x00090313), 
        CompAndContinue             =   unchecked((int)0x00090314),
        ContextExpired              =   unchecked((int)0x00090317), 
        CredentialsNeeded           =   unchecked((int)0x00090320), 
        Renegotiate                 =   unchecked((int)0x00090321),
 
        // Errors
        OutOfMemory                 =   unchecked((int)0x80090300),
        InvalidHandle               =   unchecked((int)0x80090301),
        Unsupported                 =   unchecked((int)0x80090302), 
        TargetUnknown               =   unchecked((int)0x80090303),
        InternalError               =   unchecked((int)0x80090304), 
        PackageNotFound             =   unchecked((int)0x80090305), 
        NotOwner                    =   unchecked((int)0x80090306),
        CannotInstall               =   unchecked((int)0x80090307), 
        InvalidToken                =   unchecked((int)0x80090308),
        CannotPack                  =   unchecked((int)0x80090309),
        QopNotSupported             =   unchecked((int)0x8009030A),
        NoImpersonation             =   unchecked((int)0x8009030B), 
        LogonDenied                 =   unchecked((int)0x8009030C),
        UnknownCredentials          =   unchecked((int)0x8009030D), 
        NoCredentials               =   unchecked((int)0x8009030E), 
        MessageAltered              =   unchecked((int)0x8009030F),
        OutOfSequence               =   unchecked((int)0x80090310), 
        NoAuthenticatingAuthority   =   unchecked((int)0x80090311),
        IncompleteMessage           =   unchecked((int)0x80090318),
        IncompleteCredentials       =   unchecked((int)0x80090320),
        BufferNotEnough             =   unchecked((int)0x80090321), 
        WrongPrincipal              =   unchecked((int)0x80090322),
        TimeSkew                    =   unchecked((int)0x80090324), 
        UntrustedRoot               =   unchecked((int)0x80090325), 
        IllegalMessage              =   unchecked((int)0x80090326),
        CertUnknown                 =   unchecked((int)0x80090327), 
        CertExpired                 =   unchecked((int)0x80090328),
        AlgorithmMismatch           =   unchecked((int)0x80090331),
        SecurityQosFailed           =   unchecked((int)0x80090332),
        SmartcardLogonRequired      =   unchecked((int)0x8009033E), 
        UnsupportedPreauth          =   unchecked((int)0x80090343),
    } 
 
    internal enum   ContentTypeValues {
        ChangeCipherSpec    = 0x14, 
        Alert               = 0x15,
        HandShake           = 0x16,
        AppData             = 0x17,
        Unrecognized        = 0xFF, 
    }
 
    internal enum ContextAttribute { 
        //
        // look into <sspi.h> and <schannel.h> 
        //
        Sizes               = 0x00,
        Names               = 0x01,
        Lifespan            = 0x02, 
        DceInfo             = 0x03,
        StreamSizes         = 0x04, 
        //KeyInfo             = 0x05, must not be used, see ConnectionInfo instead 
        Authority           = 0x06,
        // SECPKG_ATTR_PROTO_INFO          = 7, 
        // SECPKG_ATTR_PASSWORD_EXPIRY     = 8,
        // SECPKG_ATTR_SESSION_KEY         = 9,
        PackageInfo         = 0x0A,
        // SECPKG_ATTR_USER_FLAGS          = 11, 
        NegotiationInfo    = 0x0C,
        // SECPKG_ATTR_NATIVE_NAMES        = 13, 
        // SECPKG_ATTR_FLAGS               = 14, 
        // SECPKG_ATTR_USE_VALIDATED       = 15,
        // SECPKG_ATTR_CREDENTIAL_NAME     = 16, 
        // SECPKG_ATTR_TARGET_INFORMATION  = 17,
        // SECPKG_ATTR_ACCESS_TOKEN        = 18,
        // SECPKG_ATTR_TARGET              = 19,
        // SECPKG_ATTR_AUTHENTICATION_ID   = 20, 
        RemoteCertificate   = 0x53,
        LocalCertificate    = 0x54, 
        RootStore           = 0x55, 
        IssuerListInfoEx    = 0x59,
        ConnectionInfo      = 0x5A, 
    }

    internal enum Endianness {
        Network             = 0x00, 
        Native              = 0x10,
    } 
 
    internal enum CredentialUse {
        Inbound             = 0x1, 
        Outbound            = 0x2,
        Both                = 0x3,
    }
 
    internal enum BufferType {
        Empty               = 0x00, 
        Data                = 0x01, 
        Token               = 0x02,
        Parameters          = 0x03, 
        Missing             = 0x04,
        Extra               = 0x05,
        Trailer             = 0x06,
        Header              = 0x07, 
        Padding             = 0x09,    // non-data padding
        Stream              = 0x0A, 
        ReadOnlyFlag        = unchecked((int)0x80000000), 
        ReadOnlyWithChecksum= 0x10000000
    } 

    internal enum ChainPolicyType {
        Base                = 1,
        Authenticode        = 2, 
        Authenticode_TS     = 3,
        SSL                 = 4, 
        BasicConstraints    = 5, 
        NtAuth              = 6,
    } 

    internal enum IgnoreCertProblem {
        not_time_valid              = 0x00000001,
        ctl_not_time_valid          = 0x00000002, 
        not_time_nested             = 0x00000004,
        invalid_basic_constraints   = 0x00000008, 
 
        all_not_time_valid          =
            not_time_valid          | 
            ctl_not_time_valid      |
            not_time_nested,

        allow_unknown_ca            = 0x00000010, 
        wrong_usage                 = 0x00000020,
        invalid_name                = 0x00000040, 
        invalid_policy              = 0x00000080, 
        end_rev_unknown             = 0x00000100,
        ctl_signer_rev_unknown      = 0x00000200, 
        ca_rev_unknown              = 0x00000400,
        root_rev_unknown            = 0x00000800,

        all_rev_unknown             = 
            end_rev_unknown         |
            ctl_signer_rev_unknown  | 
            ca_rev_unknown          | 
            root_rev_unknown,
        none = 
            not_time_valid |
            ctl_not_time_valid |
            not_time_nested |
            invalid_basic_constraints | 
            allow_unknown_ca |
            wrong_usage | 
            invalid_name | 
            invalid_policy |
            end_rev_unknown | 
            ctl_signer_rev_unknown |
            ca_rev_unknown |
            root_rev_unknown
    } 

    internal enum CertUsage { 
        MatchTypeAnd    = 0x00, 
        MatchTypeOr     = 0x01,
    } 

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ChainPolicyParameter {
        public uint cbSize; 
        public uint dwFlags;
        public SSL_EXTRA_CERT_CHAIN_POLICY_PARA* pvExtraPolicyPara; 
 
        public static readonly uint StructSize = (uint) Marshal.SizeOf(typeof(ChainPolicyParameter));
    } 

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SSL_EXTRA_CERT_CHAIN_POLICY_PARA {
 
        [StructLayout(LayoutKind.Explicit)]
        internal struct U { 
              [FieldOffset(0)] internal uint cbStruct;  //DWORD 
              [FieldOffset(0)] internal uint cbSize;    //DWORD
        }; 
        internal U u;
        internal int    dwAuthType;  //DWORD
        internal uint   fdwChecks;   //DWORD
        internal char*  pwszServerName; //WCHAR* // used to check against CN=xxxx 

        internal SSL_EXTRA_CERT_CHAIN_POLICY_PARA(bool amIServer) 
        { 
            u.cbStruct = StructSize;
            u.cbSize   = StructSize; 
            //#      define      AUTHTYPE_CLIENT         1
            //#      define      AUTHTYPE_SERVER         2
            dwAuthType = amIServer? 1: 2;
            fdwChecks = 0; 
            pwszServerName = null;
        } 
        static readonly uint StructSize = (uint) Marshal.SizeOf(typeof(SSL_EXTRA_CERT_CHAIN_POLICY_PARA)); 
    }
 
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ChainPolicyStatus {
        public uint   cbSize;
        public uint   dwError; 
        public uint   lChainIndex;
        public uint   lElementIndex; 
        public void*  pvExtraPolicyStatus; 

        public static readonly uint StructSize = (uint) Marshal.SizeOf(typeof(ChainPolicyStatus)); 
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CertEnhKeyUse { 

        public uint   cUsageIdentifier; 
        public void*  rgpszUsageIdentifier; 

#if TRAVE 
        public override string ToString() {
            return "cUsageIdentifier="+cUsageIdentifier.ToString()+ " rgpszUsageIdentifier=" + new IntPtr(rgpszUsageIdentifier).ToString("x");
        }
#endif 
    };
 
    [StructLayout(LayoutKind.Sequential)] 
    internal struct CertUsageMatch {
        public CertUsage     dwType; 
        public CertEnhKeyUse Usage;
#if TRAVE
        public override string ToString() {
            return "dwType="+dwType.ToString()+" "+Usage.ToString(); 
        }
#endif 
    }; 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct ChainParameters {
        public uint cbSize;
        public CertUsageMatch RequestedUsage;
        public CertUsageMatch RequestedIssuancePolicy; 
        public uint           UrlRetrievalTimeout;
        public int            BoolCheckRevocationFreshnessTime; 
        public uint           RevocationFreshnessTime; 

 
        public static readonly uint StructSize = (uint) Marshal.SizeOf(typeof(ChainParameters));
#if TRAVE
        public override string ToString() {
            return "cbSize="+cbSize.ToString()+" "+RequestedUsage.ToString(); 
        }
#endif 
    }; 

    [StructLayout(LayoutKind.Sequential)] 
    struct _CERT_CHAIN_ELEMENT
    {
        public uint cbSize;
        public IntPtr pCertContext; 
        // Since this structure is allocated by unmanaged code, we can
        // omit the fileds below since we don't need to access them 
        // CERT_TRUST_STATUS   TrustStatus; 
        // IntPtr                pRevocationInfo;
        // IntPtr                pIssuanceUsage; 
        // IntPtr                pApplicationUsage;
    }

    // CRYPTOAPI_BLOB 
    //[StructLayout(LayoutKind.Sequential)]
    //unsafe struct CryptoBlob { 
    //    // public uint cbData; 
    //    // public byte* pbData;
    //    public uint dataSize; 
    //    public byte* dataBlob;
    //}

    // SecPkgContext_IssuerListInfoEx 
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct IssuerListInfoEx { 
        public SafeHandle aIssuers; 
        public uint cIssuers;
 
        public unsafe IssuerListInfoEx(SafeHandle handle, byte[] nativeBuffer) {
            aIssuers = handle;
            fixed(byte* voidPtr = nativeBuffer) {
                // if this breaks on 64 bit, do the sizeof(IntPtr) trick 
                cIssuers = *((uint*)(voidPtr + IntPtr.Size));
            } 
        } 
    }
 

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecureCredential {
 
/*
typedef struct _SCHANNEL_CRED 
{ 
    DWORD           dwVersion;      // always SCHANNEL_CRED_VERSION
    DWORD           cCreds; 
    PCCERT_CONTEXT *paCred;
    HCERTSTORE      hRootStore;

    DWORD           cMappers; 
    struct _HMAPPER **aphMappers;
 
    DWORD           cSupportedAlgs; 
    ALG_ID *        palgSupportedAlgs;
 
    DWORD           grbitEnabledProtocols;
    DWORD           dwMinimumCipherStrength;
    DWORD           dwMaximumCipherStrength;
    DWORD           dwSessionLifespan; 
    DWORD           dwFlags;
    DWORD           reserved; 
} SCHANNEL_CRED, *PSCHANNEL_CRED; 
*/
 
        public const int CurrentVersion = 0x4;

        public int version;
        public int cCreds; 

        // ptr to an array of pointers 
        // There is a hack done with this field.  AcquireCredentialsHandle requires an array of 
        // certificate handles; we only ever use one.  In order to avoid pinning a one element array,
        // we copy this value onto the stack, create a pointer on the stack to the copied value, 
        // and replace this field with the pointer, during the call to AcquireCredentialsHandle.
        // Then we fix it up afterwards.  Fine as long as all the SSPI credentials are not
        // supposed to be threadsafe.
        public IntPtr certContextArray; 

        private readonly IntPtr rootStore;               // == always null, OTHERWISE NOT RELIABLE 
        public int cMappers; 
        private readonly IntPtr phMappers;               // == always null, OTHERWISE NOT RELIABLE
        public int cSupportedAlgs; 
        private readonly IntPtr palgSupportedAlgs;       // == always null, OTHERWISE NOT RELIABLE
        public SchProtocols grbitEnabledProtocols;
        public int dwMinimumCipherStrength;
        public int dwMaximumCipherStrength; 
        public int dwSessionLifespan;
        public SecureCredential.Flags dwFlags; 
        public int reserved; 

        [Flags] 
        public enum Flags {
            Zero            = 0,
            NoSystemMapper  = 0x02,
            NoNameCheck     = 0x04, 
            ValidateManual  = 0x08,
            NoDefaultCred   = 0x10, 
            ValidateAuto    = 0x20 
        }
 
        public SecureCredential(int version, X509Certificate certificate, SecureCredential.Flags flags, SchProtocols protocols) {
            // default values required for a struct
            rootStore = phMappers = palgSupportedAlgs = certContextArray = IntPtr.Zero;
            cCreds = cMappers = cSupportedAlgs = 0; 
            dwMinimumCipherStrength = dwMaximumCipherStrength = 0;
            dwSessionLifespan = reserved = 0; 
            this.version = version; 
            dwFlags = flags;
            grbitEnabledProtocols = protocols; 
            if (certificate != null) {
                certContextArray = certificate.Handle;
                cCreds = 1;
            } 
        }
 
        [System.Diagnostics.Conditional("TRAVE")] 
        internal void DebugDump() {
            GlobalLog.Print("SecureCredential #"+GetHashCode()); 
            GlobalLog.Print("    version                 = " + version);
            GlobalLog.Print("    cCreds                  = " + cCreds);
            GlobalLog.Print("    certContextArray        = " + String.Format("0x{0:x}", certContextArray));
            GlobalLog.Print("    rootStore               = " + String.Format("0x{0:x}", rootStore)); 
            GlobalLog.Print("    cMappers                = " + cMappers);
            GlobalLog.Print("    phMappers               = " + String.Format("0x{0:x}", phMappers)); 
            GlobalLog.Print("    cSupportedAlgs          = " + cSupportedAlgs); 
            GlobalLog.Print("    palgSupportedAlgs       = " + String.Format("0x{0:x}", palgSupportedAlgs));
            GlobalLog.Print("    grbitEnabledProtocols   = " + String.Format("0x{0:x}", grbitEnabledProtocols)); 
            GlobalLog.Print("    dwMinimumCipherStrength = " + dwMinimumCipherStrength);
            GlobalLog.Print("    dwMaximumCipherStrength = " + dwMaximumCipherStrength);
            GlobalLog.Print("    dwSessionLifespan       = " + String.Format("0x{0:x}", dwSessionLifespan));
            GlobalLog.Print("    dwFlags                 = " + String.Format("0x{0:x}", dwFlags)); 
            GlobalLog.Print("    reserved                = " + String.Format("0x{0:x}", reserved));
        } 
 
    } // SecureCredential
 

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SecurityBufferStruct {
        public int          count; 
        public BufferType   type;
        public IntPtr       token; 
 
        public static readonly int Size = sizeof(SecurityBufferStruct);
    } 

    internal class SecurityBuffer {
        public int size;
        public BufferType type; 
        public byte[] token;
        public int offset; 
 
        public SecurityBuffer(byte[] data, int offset, int size, BufferType tokentype) {
            GlobalLog.Assert(offset >= 0 && offset <= (data == null ? 0 : data.Length), "SecurityBuffer::.ctor", "'offset' out of range.  [" + offset + "]"); 
            GlobalLog.Assert(size >= 0 && size <= (data == null ? 0 : data.Length - offset), "SecurityBuffer::.ctor", "'size' out of range.  [" + size + "]");
 			
            this.offset = data == null || offset < 0 ? 0 : Math.Min(offset, data.Length);
            this.size   = data == null || size < 0 ? 0 : Math.Min(size, data.Length - this.offset); 
            this.type   = tokentype;
            this.token  = size == 0 ? null : data; 
        } 

        public SecurityBuffer(byte[] data, BufferType tokentype) { 
            this.size   = data == null ? 0 : data.Length;
            this.type   = tokentype;
            this.token  = size == 0 ? null : data;
        } 

        public SecurityBuffer(int size, BufferType tokentype) { 
            GlobalLog.Assert(size >= 0, "SecurityBuffer::.ctor", "'size' out of range.  [" + size.ToString(NumberFormatInfo.InvariantInfo) + "]"); 

            this.size   = size; 
            this.type   = tokentype;
            this.token  = size == 0 ? null : new byte[size];
        }
    } 

    [StructLayout(LayoutKind.Sequential)] 
    internal unsafe class SecurityBufferDescriptor { 
    /*
    typedef struct _SecBufferDesc { 
        ULONG        ulVersion;
        ULONG        cBuffers;
        PSecBuffer   pBuffers;
    } SecBufferDesc, * PSecBufferDesc; 
    */
            public  readonly    int     Version; 
            public  readonly    int     Count; 
            public  void*       UnmanagedPointer;
 
        public SecurityBufferDescriptor(int count) {
            Version = 0;
            Count = count;
            UnmanagedPointer = null; 
        }
 
        [System.Diagnostics.Conditional("TRAVE")] 
        internal void DebugDump() {
            GlobalLog.Print("SecurityBufferDescriptor #" + ValidationHelper.HashString(this)); 
            GlobalLog.Print("    version             = " + Version);
            GlobalLog.Print("    count               = " + Count);
            GlobalLog.Print("    securityBufferArray = 0x" + (new IntPtr(UnmanagedPointer)).ToString("x"));
        } 
    } // SecurityBufferDescriptor
 
    internal  enum    CertificateEncoding { 
        Zero                     = 0,
        X509AsnEncoding          = unchecked((int)0x00000001), 
        X509NdrEncoding          = unchecked((int)0x00000002),
        Pkcs7AsnEncoding         = unchecked((int)0x00010000),
        Pkcs7NdrEncoding         = unchecked((int)0x00020000),
        AnyAsnEncoding           = X509AsnEncoding|Pkcs7AsnEncoding 
    }
 
    internal  enum    CertificateProblem { 
        OK                          =   0x00000000,
        TrustNOSIGNATURE            = unchecked((int)0x800B0100), 
        CertEXPIRED                 = unchecked((int)0x800B0101),
        CertVALIDITYPERIODNESTING   = unchecked((int)0x800B0102),
        CertROLE                    = unchecked((int)0x800B0103),
        CertPATHLENCONST            = unchecked((int)0x800B0104), 
        CertCRITICAL                = unchecked((int)0x800B0105),
        CertPURPOSE                 = unchecked((int)0x800B0106), 
        CertISSUERCHAINING          = unchecked((int)0x800B0107), 
        CertMALFORMED               = unchecked((int)0x800B0108),
        CertUNTRUSTEDROOT           = unchecked((int)0x800B0109), 
        CertCHAINING                = unchecked((int)0x800B010A),
        CertREVOKED                 = unchecked((int)0x800B010C),
        CertUNTRUSTEDTESTROOT       = unchecked((int)0x800B010D),
        CertREVOCATION_FAILURE      = unchecked((int)0x800B010E), 
        CertCN_NO_MATCH             = unchecked((int)0x800B010F),
        CertWRONG_USAGE             = unchecked((int)0x800B0110), 
        TrustEXPLICITDISTRUST       = unchecked((int)0x800B0111), 
        CertUNTRUSTEDCA             = unchecked((int)0x800B0112),
        CertINVALIDPOLICY           = unchecked((int)0x800B0113), 
        CertINVALIDNAME             = unchecked((int)0x800B0114),

        CryptNOREVOCATIONCHECK       = unchecked((int)0x80092012),
        CryptREVOCATIONOFFLINE       = unchecked((int)0x80092013), 

        TrustSYSTEMERROR            = unchecked((int)0x80096001), 
        TrustNOSIGNERCERT           = unchecked((int)0x80096002), 
        TrustCOUNTERSIGNER          = unchecked((int)0x80096003),
        TrustCERTSIGNATURE          = unchecked((int)0x80096004), 
        TrustTIMESTAMP              = unchecked((int)0x80096005),
        TrustBADDIGEST              = unchecked((int)0x80096010),
        TrustBASICCONSTRAINTS       = unchecked((int)0x80096019),
        TrustFINANCIALCRITERIA      = unchecked((int)0x8009601E), 
    }
#endif // !FEATURE_PAL 
 
    //
    // WebRequestPrefixElement 
    //
    // This is an element of the prefix list. It contains the prefix and the
    // interface to be called to create a request for that prefix.
    // 

    /// <devdoc> 
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    // internal class WebRequestPrefixElement { 
    internal class WebRequestPrefixElement  {

        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public    string              Prefix; 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        internal    IWebRequestCreate   creator;
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        internal    Type   creatorType;
 
        public IWebRequestCreate Creator { 
            get {
                if (creator == null && creatorType != null) { 
                    lock(this) {
                        if (creator == null) {
                            creator = (IWebRequestCreate)Activator.CreateInstance(
                                                        creatorType, 
                                                        BindingFlags.CreateInstance
                                                        | BindingFlags.Instance 
                                                        | BindingFlags.NonPublic 
                                                        | BindingFlags.Public,
                                                        null,          // Binder 
                                                        new object[0], // no arguments
                                                        CultureInfo.InvariantCulture
                                                        );
                        } 
                    }
                } 
 
                return creator;
            } 

            set {
                creator = value;
            } 
        }
 
        public WebRequestPrefixElement(string P, Type creatorType) { 
            // verify that its of the proper type of IWebRequestCreate
            if (!typeof(IWebRequestCreate).IsAssignableFrom(creatorType)) 
            {
                throw new InvalidCastException(SR.GetString(SR.net_invalid_cast,
                                                                creatorType.AssemblyQualifiedName,
                                                                "IWebRequestCreate")); 
            }
 
            Prefix = P; 
            this.creatorType = creatorType;
        } 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public WebRequestPrefixElement(string P, IWebRequestCreate C) {
            Prefix = P; 
            Creator = C; 
        }
 
    } // class PrefixListElement


    // 
    // HttpRequestCreator.
    // 
    // This is the class that we use to create HTTP and HTTPS requests. 
    //
 
    internal class HttpRequestCreator : IWebRequestCreate {

        /*++
 
         Create - Create an HttpWebRequest.
 
            This is our method to create an HttpWebRequest. We register 
            for HTTP and HTTPS Uris, and this method is called when a request
            needs to be created for one of those. 


            Input:
                    Uri             - Uri for request being created. 

            Returns: 
                    The newly created HttpWebRequest. 

         --*/ 

        public WebRequest Create( Uri Uri ) {
            //
            // Note, DNS permissions check will not happen on WebRequest 
            //
            return new HttpWebRequest(Uri, null); 
        } 

    } // class HttpRequestCreator 

    //
    //  CoreResponseData - Used to store result of HTTP header parsing and
    //      response parsing.  Also Contains new stream to use, and 
    //      is used as core of new Response
    // 
    internal class CoreResponseData { 

        // Status Line Response Values 
        public HttpStatusCode m_StatusCode;
        public string m_StatusDescription;
        public bool m_IsVersionHttp11;
 
        // Content Length needed for semantics, -1 if chunked
        public long m_ContentLength; 
 
        // Response Headers
        public WebHeaderCollection m_ResponseHeaders; 

        // ConnectStream - for reading actual data
        public Stream m_ConnectStream;
 
        internal CoreResponseData Clone() {
            CoreResponseData cloneResponseData = new CoreResponseData(); 
            cloneResponseData.m_StatusCode        = m_StatusCode; 
            cloneResponseData.m_StatusDescription = m_StatusDescription;
            cloneResponseData.m_IsVersionHttp11   = m_IsVersionHttp11; 
            cloneResponseData.m_ContentLength     = m_ContentLength;
            cloneResponseData.m_ResponseHeaders   = m_ResponseHeaders;
            cloneResponseData.m_ConnectStream     = m_ConnectStream;
            return cloneResponseData; 
        }
 
    } 

 
    /*++

    StreamChunkBytes - A class to read a chunk stream from a ConnectStream.
 
    A simple little value class that implements the IReadChunkBytes
    interface. 
 
    --*/
    internal class StreamChunkBytes : IReadChunkBytes { 

        public  ConnectStream   ChunkStream;
        public  int             BytesRead = 0;
        public  int             TotalBytesRead = 0; 
        private byte            PushByte;
        private bool            HavePush; 
 
        public StreamChunkBytes(ConnectStream connectStream) {
            ChunkStream = connectStream; 
            return;
        }

        public int NextByte { 
            get {
                if (HavePush) { 
                    HavePush = false; 
                    return PushByte;
                } 

                return ChunkStream.ReadSingleByte();
            }
            set { 
                PushByte = (byte)value;
                HavePush = true; 
            } 
        }
 
    } // class StreamChunkBytes


    internal delegate bool HttpAbortDelegate(HttpWebRequest request, WebException webException); 

    // 
    // this class contains known header names 
    //
 
    internal static class HttpKnownHeaderNames {

        public const string CacheControl = "Cache-Control";
        public const string Connection = "Connection"; 
        public const string Date = "Date";
        public const string KeepAlive = "Keep-Alive"; 
        public const string Pragma = "Pragma"; 
        public const string ProxyConnection = "Proxy-Connection";
        public const string Trailer = "Trailer"; 
        public const string TransferEncoding = "Transfer-Encoding";
        public const string Upgrade = "Upgrade";
        public const string Via = "Via";
        public const string Warning = "Warning"; 
        public const string ContentLength = "Content-Length";
        public const string ContentType = "Content-Type"; 
        public const string ContentEncoding = "Content-Encoding"; 
        public const string ContentLanguage = "Content-Language";
        public const string ContentLocation = "Content-Location"; 
        public const string ContentRange = "Content-Range";
        public const string Expires = "Expires";
        public const string LastModified = "Last-Modified";
        public const string Age = "Age"; 
        public const string Location = "Location";
        public const string ProxyAuthenticate = "Proxy-Authenticate"; 
        public const string RetryAfter = "Retry-After"; 
        public const string Server = "Server";
        public const string SetCookie = "Set-Cookie"; 
        public const string SetCookie2 = "Set-Cookie2";
        public const string Vary = "Vary";
        public const string WWWAuthenticate = "WWW-Authenticate";
        public const string Accept = "Accept"; 
        public const string AcceptCharset = "Accept-Charset";
        public const string AcceptEncoding = "Accept-Encoding"; 
        public const string AcceptLanguage = "Accept-Language"; 
        public const string Authorization = "Authorization";
        public const string Cookie = "Cookie"; 
        public const string Cookie2 = "Cookie2";
        public const string Expect = "Expect";
        public const string From = "From";
        public const string Host = "Host"; 
        public const string IfMatch = "If-Match";
        public const string IfModifiedSince = "If-Modified-Since"; 
        public const string IfNoneMatch = "If-None-Match"; 
        public const string IfRange = "If-Range";
        public const string IfUnmodifiedSince = "If-Unmodified-Since"; 
        public const string MaxForwards = "Max-Forwards";
        public const string ProxyAuthorization = "Proxy-Authorization";
        public const string Referer = "Referer";
        public const string Range = "Range"; 
        public const string UserAgent = "User-Agent";
        public const string ContentMD5 = "Content-MD5"; 
        public const string ETag = "ETag"; 
        public const string TE = "TE";
        public const string Allow = "Allow"; 
        public const string AcceptRanges = "Accept-Ranges";
        public const string P3P = "P3P";
        public const string XPoweredBy = "X-Powered-By";
        public const string XAspNetVersion = "X-AspNet-Version"; 
    }
 
    /// <devdoc> 
    ///    <para>
    ///       Represents the method that will notify callers when a continue has been 
    ///       received by the client.
    ///    </para>
    /// </devdoc>
    // Delegate type for us to notify callers when we receive a continue 
    public delegate void HttpContinueDelegate(int StatusCode, WebHeaderCollection httpHeaders);
 
    // 
    // HttpWriteMode - used to control the way in which an entity Body is posted.
    // 
    enum HttpWriteMode {
        Unknown         = 0,
        ContentLength   = 1,
        Chunked         = 2, 
        Buffer          = 3,
        None            = 4, 
    } 

    // Used by Request to notify Connection that we are no longer holding the Connection (for NTLM connection sharing) 
    delegate void UnlockConnectionDelegate();

    enum HttpBehaviour : byte {
        Unknown                     = 0, 
        HTTP10                      = 1,
        HTTP11PartiallyCompliant    = 2, 
        HTTP11                      = 3, 
    }
 
    internal enum HttpProcessingResult {
        Continue  = 0,
        ReadWait  = 1,
        WriteWait = 2, 
    }
 
    // 
    // HttpVerb - used to define various per Verb Properties
    // 

    //
    // Note - this is a place holder for Verb properties,
    //  the following two bools can most likely be combined into 
    //  a single Enum type.  And the Verb can be incorporated.
    // 
    class KnownHttpVerb { 
        internal string Name; // verb name
 
        internal bool RequireContentBody; // require content body to be sent
        internal bool ContentBodyNotAllowed; // not allowed to send content body
        internal bool ConnectRequest; // special semantics for a connect request
        internal bool ExpectNoContentResponse; // response will not have content body 

        internal KnownHttpVerb(string name, bool requireContentBody, bool contentBodyNotAllowed, bool connectRequest, bool expectNoContentResponse) { 
            Name = name; 
            RequireContentBody = requireContentBody;
            ContentBodyNotAllowed = contentBodyNotAllowed; 
            ConnectRequest = connectRequest;
            ExpectNoContentResponse = expectNoContentResponse;
        }
 
        // Force an an init, before we use them
        private static ListDictionary NamedHeaders; 
 
        // known verbs
        internal static KnownHttpVerb Get; 
        internal static KnownHttpVerb Connect;
        internal static KnownHttpVerb Head;
        internal static KnownHttpVerb Put;
        internal static KnownHttpVerb Post; 
        internal static KnownHttpVerb MkCol;
 
        // 
        // InitializeKnownVerbs - Does basic init for this object,
        //  such as creating defaultings and filling them 
        //
        static KnownHttpVerb() {
            NamedHeaders = new ListDictionary(CaseInsensitiveAscii.StaticInstance);
            Get = new KnownHttpVerb("GET", false, true, false, false); 
            Connect = new KnownHttpVerb("CONNECT", false, true, true, false);
            Head = new KnownHttpVerb("HEAD", false, true, false, true); 
            Put = new KnownHttpVerb("PUT", true, false, false, false); 
            Post = new KnownHttpVerb("POST", true, false, false, false);
            MkCol = new KnownHttpVerb("MKCOL",false,false,false,false); 
            NamedHeaders[Get.Name] = Get;
            NamedHeaders[Connect.Name] = Connect;
            NamedHeaders[Head.Name] = Head;
            NamedHeaders[Put.Name] = Put; 
            NamedHeaders[Post.Name] = Post;
            NamedHeaders[MkCol.Name] = MkCol; 
        } 

        public bool Equals(KnownHttpVerb verb) { 
            return this==verb || string.Compare(Name, verb.Name, StringComparison.OrdinalIgnoreCase)==0;
        }

        public static KnownHttpVerb Parse(string name) { 
            KnownHttpVerb knownHttpVerb = NamedHeaders[name] as KnownHttpVerb;
            if (knownHttpVerb==null) { 
                // unknown verb, default behaviour 
                knownHttpVerb = new KnownHttpVerb(name, false, false, false, false);
            } 
            return knownHttpVerb;
        }
    }
 

    // 
    // HttpProtocolUtils - A collection of utility functions for HTTP usage. 
    //
 
    internal class HttpProtocolUtils {

        private HttpProtocolUtils() {
        } 

        // 
        // extra buffers for build/parsing, recv/send HTTP data, 
        //  at some point we should consolidate
        // 


        // parse String to DateTime format.
        internal static DateTime string2date(String S) { 
            DateTime dtOut;
            if (HttpDateParse.ParseHttpDate(S,out dtOut)) { 
                return dtOut; 
            }
            else { 
                throw new ProtocolViolationException(SR.GetString(SR.net_baddate));
            }

        } 

        // convert Date to String using RFC 1123 pattern 
        internal static string date2string(DateTime D) { 
            DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();
            return D.ToUniversalTime().ToString("R", dateFormat); 
        }
    }

#if !FEATURE_PAL 
    // Proxy class for linking between ICertificatePolicy <--> ICertificateDecider
    internal class  PolicyWrapper { 
        private const uint          IgnoreUnmatchedCN       = 0x00001000; 
        private ICertificatePolicy  fwdPolicy;
        private ServicePoint        srvPoint; 
        private WebRequest          request;

        internal PolicyWrapper(ICertificatePolicy policy, ServicePoint sp, WebRequest wr) {
            this.fwdPolicy = policy; 
            srvPoint = sp;
            request = wr; 
        } 

        public bool Accept(X509Certificate Certificate, int CertificateProblem) { 
            return fwdPolicy.CheckValidationResult(srvPoint, Certificate, request, CertificateProblem);
        }

        internal static uint VerifyChainPolicy(SafeFreeCertChain chainContext, ref ChainPolicyParameter cpp) { 
            GlobalLog.Enter("PolicyWrapper::VerifyChainPolicy", "chainContext="+ chainContext + ", options="+String.Format("0x{0:x}", cpp.dwFlags));
            ChainPolicyStatus status = new ChainPolicyStatus(); 
            status.cbSize = ChainPolicyStatus.StructSize; 
            int errorCode =
                UnsafeNclNativeMethods.NativePKI.CertVerifyCertificateChainPolicy( 
                    (IntPtr) ChainPolicyType.SSL,
                    chainContext,
                    ref cpp,
                    ref status); 

            GlobalLog.Print("PolicyWrapper::VerifyChainPolicy() CertVerifyCertificateChainPolicy returned: " + errorCode); 
#if TRAVE 
            GlobalLog.Print("PolicyWrapper::VerifyChainPolicy() error code: " + status.dwError+String.Format(" [0x{0:x8}", status.dwError) + " " + SecureChannel.MapSecurityStatus(status.dwError) + "]");
#endif 
            GlobalLog.Leave("PolicyWrapper::VerifyChainPolicy", status.dwError.ToString());
            return status.dwError;
        }
 
        private static IgnoreCertProblem MapErrorCode(uint errorCode) {
            switch ((CertificateProblem) errorCode) { 
 
                case CertificateProblem.CertINVALIDNAME :
                case CertificateProblem.CertCN_NO_MATCH : 
                    return IgnoreCertProblem.invalid_name;

                case CertificateProblem.CertINVALIDPOLICY :
                case CertificateProblem.CertPURPOSE : 
                    return IgnoreCertProblem.invalid_policy;
 
                case CertificateProblem.CertEXPIRED : 
                    return IgnoreCertProblem.not_time_valid | IgnoreCertProblem.ctl_not_time_valid;
 
                case CertificateProblem.CertVALIDITYPERIODNESTING :
                    return IgnoreCertProblem.not_time_nested;

                case CertificateProblem.CertCHAINING : 
                case CertificateProblem.CertUNTRUSTEDCA :
                case CertificateProblem.CertUNTRUSTEDROOT : 
                    return IgnoreCertProblem.allow_unknown_ca; 

                case CertificateProblem.CertREVOKED : 
                case CertificateProblem.CertREVOCATION_FAILURE :
                case CertificateProblem.CryptNOREVOCATIONCHECK:
                case CertificateProblem.CryptREVOCATIONOFFLINE:
                    return IgnoreCertProblem.all_rev_unknown; 

                case CertificateProblem.CertROLE: 
                case CertificateProblem.TrustBASICCONSTRAINTS: 
                    return IgnoreCertProblem.invalid_basic_constraints;
 
                case CertificateProblem.CertWRONG_USAGE :
                    return IgnoreCertProblem.wrong_usage;

                default: 
                    return 0;
            } 
        } 

 
        private uint[] GetChainErrors(string hostName, X509Chain chain, ref bool fatalError)
        {
            fatalError = false;
            SafeFreeCertChain chainContext= new SafeFreeCertChain(chain.ChainContext); 
            ArrayList certificateProblems = new ArrayList();
            unsafe { 
                uint status = 0; 
                ChainPolicyParameter cppStruct = new ChainPolicyParameter();
                cppStruct.cbSize  = ChainPolicyParameter.StructSize; 
                cppStruct.dwFlags = 0;

                SSL_EXTRA_CERT_CHAIN_POLICY_PARA eppStruct = new SSL_EXTRA_CERT_CHAIN_POLICY_PARA(false);
                cppStruct.pvExtraPolicyPara = &eppStruct; 

                fixed (char* namePtr = hostName) { 
                    if (ServicePointManager.CheckCertificateName){ 
                        eppStruct.pwszServerName = namePtr;
                    } 

                    while (true) {
                        status = VerifyChainPolicy(chainContext, ref cppStruct);
                        uint ignoreErrorMask = (uint)MapErrorCode(status); 

                        certificateProblems.Add(status); 
 
                        if (status == 0) {  // No more problems with the certificate?
                            break;          // Then break out of the callback loop 
                        }

                        if (ignoreErrorMask == 0) {  // Unrecognized error encountered
                            fatalError = true; 
                            break;
                        } 
                        else { 
                            cppStruct.dwFlags |= ignoreErrorMask;
                            if ((CertificateProblem)status == CertificateProblem.CertCN_NO_MATCH && ServicePointManager.CheckCertificateName) { 
                                eppStruct.fdwChecks = IgnoreUnmatchedCN;
                            }
                        }
                    } 
                }
            } 
 
            return (uint[]) certificateProblems.ToArray(typeof(uint));
        } 

        internal bool CheckErrors(string hostName, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == 0) 
                return Accept(certificate, 0);
 
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0) 
                return Accept(certificate, (int) CertificateProblem.CertCRITICAL); // ToDO, Define an appropriate enum entry
            else { 
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 ||
                    (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                {
                    bool fatalError = false; 
                    uint[] certificateProblems = GetChainErrors(hostName, chain, ref fatalError);
 
                    if (fatalError) { 
                        // By today design we cannot allow decider to ignore a fatal error.
                        // This error is fatal. 
                        Accept(certificate, (int) SecurityStatus.InternalError);
                        return false;
                    }
 

                    if (certificateProblems.Length == 0) 
                        return Accept(certificate, (int) CertificateProblem.OK); 

                    // Run each error through Accept(). 
                    foreach (uint error in certificateProblems)
                        if (!Accept(certificate, (int) error))
                            return false;
                } 
                return true;
            } 
        } 

/* 



 

 
 

 



 

 
 

 



 

 
 

 



*/ 
    }
    // Class implementing default certificate policy 
    internal class DefaultCertPolicy : ICertificatePolicy { 
        public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest request, int problem) {
            return problem == (int)CertificateProblem.OK; 
        }
    }
#endif // !FEATURE_PAL
 
    internal enum TriState {
        Unspecified = -1, 
        False = 0, 
        True = 1
    } 

    internal enum DefaultPorts {
        DEFAULT_FTP_PORT = 21,
        DEFAULT_GOPHER_PORT = 70, 
        DEFAULT_HTTP_PORT = 80,
        DEFAULT_HTTPS_PORT = 443, 
        DEFAULT_NNTP_PORT = 119, 
        DEFAULT_SMTP_PORT = 25,
        DEFAULT_TELNET_PORT = 23 
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    internal struct hostent { 
        public IntPtr   h_name;
        public IntPtr   h_aliases; 
        public short    h_addrtype; 
        public short    h_length;
        public IntPtr   h_addr_list; 
    }


    [StructLayout(LayoutKind.Sequential)] 
    internal struct Blob {
        public int cbSize; 
        public int pBlobData; 
    }
 

    // This is only for internal code path i.e. TLS stream.
    // See comments on GetNextBuffer() method below.
    // 
    internal class SplitWritesState
    { 
        private const int c_SplitEncryptedBuffersSize = 64*1024; 
        private BufferOffsetSize[] _UserBuffers;
        private int _Index; 
        private int _LastBufferConsumed;
        private BufferOffsetSize[] _RealBuffers;

        // 
        internal SplitWritesState(BufferOffsetSize[] buffers)
        { 
            _UserBuffers    = buffers; 
            _LastBufferConsumed = 0;
            _Index          = 0; 
            _RealBuffers = null;
        }
        //
        // Everything was handled 
        //
        internal bool IsDone { 
            get { 
                if (_LastBufferConsumed != 0)
                    return false; 

                for (int index = _Index ;index < _UserBuffers.Length; ++index)
                    if (_UserBuffers[index].Size != 0)
                        return false; 

                return true; 
            } 
        }
        // Encryption takes CPU and if the input is large (like 10 mb) then a delay may 
        // be 30 sec or so. Hence split the ecnrypt and write operations in smaller chunks
        // up to c_SplitEncryptedBuffersSize total.
        // Note that upon return from here EncryptBuffers() may additonally split the input
        // into chunks each <= chkSecureChannel.MaxDataSize (~16k) yet it will complete them all as a single IO. 
        //
        //  Returns null if done, returns the _buffers reference if everything is handled in one shot (also done) 
        // 
        //  Otheriwse returns subsequent BufferOffsetSize[] to encrypt and pass to base IO method
        // 
        internal BufferOffsetSize[] GetNextBuffers()
        {
            int curIndex = _Index;
            int currentTotalSize = 0; 
            int lastChunkSize = 0;
 
            int  firstBufferConsumed = _LastBufferConsumed; 

            for ( ;_Index < _UserBuffers.Length; ++_Index) 
            {
                lastChunkSize = _UserBuffers[_Index].Size-_LastBufferConsumed;

                currentTotalSize += lastChunkSize; 

                if (currentTotalSize > c_SplitEncryptedBuffersSize) 
                { 
                    lastChunkSize -= (currentTotalSize - c_SplitEncryptedBuffersSize);
                    currentTotalSize = c_SplitEncryptedBuffersSize; 
                    break;
                }

                lastChunkSize = 0; 
                _LastBufferConsumed = 0;
            } 
 
            // Are we done done?
            if (currentTotalSize == 0) 
                return null;

             // Do all buffers fit the limit?
            if (firstBufferConsumed == 0 && curIndex == 0 && _Index == _UserBuffers.Length) 
                return _UserBuffers;
 
            // We do have something to split and send out 
            int buffersCount = lastChunkSize == 0? _Index-curIndex: _Index-curIndex+1;
 
            if (_RealBuffers == null || _RealBuffers.Length != buffersCount)
                _RealBuffers = new BufferOffsetSize[buffersCount];

            int j = 0; 
            for (; curIndex < _Index; ++curIndex)
            { 
                _RealBuffers[j++] = new BufferOffsetSize(_UserBuffers[curIndex].Buffer, _UserBuffers[curIndex].Offset + firstBufferConsumed, _UserBuffers[curIndex].Size-firstBufferConsumed, false); 
                firstBufferConsumed = 0;
            } 

            if (lastChunkSize != 0)
            {
                _RealBuffers[j] = new BufferOffsetSize(_UserBuffers[curIndex].Buffer, _UserBuffers[curIndex].Offset + _LastBufferConsumed, lastChunkSize, false); 
                if ((_LastBufferConsumed += lastChunkSize) == _UserBuffers[_Index].Size)
                { 
                    ++_Index; 
                    _LastBufferConsumed = 0;
                } 
            }

            return _RealBuffers;
 
        }
    } 
} 
