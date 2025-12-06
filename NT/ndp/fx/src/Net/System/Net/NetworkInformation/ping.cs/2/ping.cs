 
namespace System.Net.NetworkInformation {

    using System.Net;
    using System.Net.Sockets; 
    using System;
    using System.Runtime.InteropServices; 
    using System.Threading; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Security.Permissions;

    public delegate void PingCompletedEventHandler (object sender, PingCompletedEventArgs e);
 

    public class PingCompletedEventArgs: System.ComponentModel.AsyncCompletedEventArgs { 
        PingReply reply; 

        internal PingCompletedEventArgs (PingReply reply, Exception error, bool cancelled, object userToken):base(error,cancelled,userToken) { 
            this.reply = reply;
        }
        public PingReply Reply{get {return reply;}}
    } 

    public class Ping:Component,IDisposable 
    { 
        const int MaxUdpPacket = 0xFFFF + 256; // Marshal.SizeOf(typeof(Icmp6EchoReply)) * 2 + ip header info;
        const int MaxBufferSize = 65500; //artificial constraint due to win32 api limitations. 
        const int DefaultTimeout = 5000; //5 seconds same as ping.exe
        const int DefaultSendBufferSize = 32;  //same as ping.exe

        byte[] defaultSendBuffer = null; 
        bool ipv6 = false;
        bool inAsyncCall = false; 
        bool cancelled = false; 
        bool disposed = false;
 
        //used for icmpsendecho apis
        internal ManualResetEvent pingEvent = null;
        private RegisteredWaitHandle registeredWait = null;
        SafeLocalFree requestBuffer = null; 
        SafeLocalFree replyBuffer = null;
        int sendSize = 0;  //needed to determine what reply size is for ipv6 in callback 
 
        //used for downlevel calls
        const int TimeoutErrorCode = 0x274c; 
        const int PacketTooBigErrorCode = 0x2738;
        Socket pingSocket = null;
        byte[] downlevelReplyBuffer = null;
        SafeCloseIcmpHandle handlePingV4 = null; 
        SafeCloseIcmpHandle handlePingV6 = null;
        int startTime = 0; 
        IcmpPacket packet = null; 
        int llTimeout = 0;
 
        //new async event support
        AsyncOperation asyncOp = null;
        SendOrPostCallback onPingCompletedDelegate;
        public event PingCompletedEventHandler PingCompleted; 

 
        protected void OnPingCompleted(PingCompletedEventArgs e) 
        {
            if (PingCompleted != null) { 
                PingCompleted (this,e);
            }
        }
 
        void PingCompletedWaitCallback (object operationState) {
            OnPingCompleted((PingCompletedEventArgs)operationState); 
        } 

        public Ping () { 
            onPingCompletedDelegate = new SendOrPostCallback (PingCompletedWaitCallback);
        }

        //cancel pending async requests, close the handles 
        private void InternalDispose () {
            if (disposed) { 
                return; 
            }
 
            disposed = true;

            if (inAsyncCall) {
                SendAsyncCancel (); 
            }
 
            if (pingSocket != null) 
            {
                pingSocket.Close (); 
                pingSocket = null;
            }

            if (handlePingV4 != null) { 
                handlePingV4.Close ();
                handlePingV4 = null; 
            } 

            if (handlePingV6 != null) { 
                handlePingV6.Close ();
                handlePingV6 = null;
            }
 
            if (registeredWait != null)
            { 
                registeredWait.Unregister(null); 
            }
 
            if (pingEvent != null) {
                pingEvent.Close();
            }
 
            if (replyBuffer != null) {
                replyBuffer.Close(); 
            } 
        }
 

        /// <internalonly/>
        void IDisposable.Dispose () {
            InternalDispose (); 
        }
 
 
        //cancels pending async calls
        public void SendAsyncCancel() { 
            lock (this) {
                if (!inAsyncCall) {
                    return;
                } 

                cancelled = true; 
 
                if (handlePingV4 != null) {
                    handlePingV4.Close (); 
                    handlePingV4 = null;
                }

                if (handlePingV6 != null) { 
                    handlePingV6.Close ();
                    handlePingV6 = null; 
                } 

                if (pingSocket != null) { 
                    pingSocket.Close ();
                    pingSocket = null;
                }
            } 

            if (pingEvent != null) { 
                pingEvent.WaitOne(); 
            }
        } 


        //private callback invoked when icmpsendecho apis succeed
        private static void PingCallback (object state, bool signaled) 
        {
            Ping ping = (Ping)state; 
            PingCompletedEventArgs eventArgs = null; 
            bool cancelled = false;
            AsyncOperation asyncOp = null; 
            SendOrPostCallback onPingCompletedDelegate = null;

            try
            { 
                lock(ping) {
                    cancelled = ping.cancelled; 
                    asyncOp = ping.asyncOp; 
                    onPingCompletedDelegate = ping.onPingCompletedDelegate;
 
                    if (!cancelled) {
                        //parse reply buffer
                        SafeLocalFree buffer = ping.replyBuffer;
 
                        int error = 0;
                        if (ping.ipv6) { 
                            error = (int)UnsafeNetInfoNativeMethods.Icmp6ParseReplies (buffer.DangerousGetHandle (), (uint)MaxUdpPacket); 
                        }
                        else { 
                            if (ComNetOS.IsPostWin2K) {
                                error = (int)UnsafeNetInfoNativeMethods.IcmpParseReplies (buffer.DangerousGetHandle (), (uint)MaxUdpPacket);
                            }
                            else{ 
                                error = (int)UnsafeIcmpNativeMethods.IcmpParseReplies (buffer.DangerousGetHandle (), (uint)MaxUdpPacket);
                            } 
                        } 

                        //looks like we get a lot of false failures here 
                        //if (error == 0){
                        //  error = Marshal.GetLastWin32Error();
                        //if (error != 0) {
                        //   throw new PingException(error);    looks like we get false failures here 
                        // if the status code isn't success.
                        //seems to be timing related 
                        // } 
                        //}
 
                        //marshals and constructs new reply
                        PingReply reply;

                        if (ping.ipv6) 
                        {
                            Icmp6EchoReply icmp6Reply = (Icmp6EchoReply)Marshal.PtrToStructure (buffer.DangerousGetHandle (), typeof(Icmp6EchoReply)); 
                            reply = new PingReply (icmp6Reply,buffer.DangerousGetHandle(),ping.sendSize); 
                        }
                        else { 
                            IcmpEchoReply icmpReply = (IcmpEchoReply)Marshal.PtrToStructure (buffer.DangerousGetHandle (), typeof(IcmpEchoReply));
                            reply = new PingReply (icmpReply);
                        }
 
                        eventArgs = new PingCompletedEventArgs (reply, null, false, asyncOp.UserSuppliedState);
                    } 
                } 
            }
            // in case of failure, create a failed event arg 
            catch (Exception e) {
                PingException pe = new PingException(SR.GetString(SR.net_ping), e);
                eventArgs = new PingCompletedEventArgs (null,pe, false, asyncOp.UserSuppliedState);
            } 
            catch {
                PingException pe = new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                eventArgs = new PingCompletedEventArgs (null,pe, false, asyncOp.UserSuppliedState); 
            }
            finally { 
                ping.FreeUnmanagedStructures ();
                ping.inAsyncCall = false;
            }
 
            if (cancelled) {
                eventArgs = new PingCompletedEventArgs (null, null, true, asyncOp.UserSuppliedState); 
            } 
            asyncOp.PostOperationCompleted (onPingCompletedDelegate, eventArgs);
        } 


        //private callback invoked when downlevel send succeeds
        private static void PingSendCallback (IAsyncResult result) { 
            Ping ping = (Ping)(result.AsyncState);
            PingCompletedEventArgs eventArgs = null; 
 
            try {
                ping.pingSocket.EndSendTo (result); 
                PingReply reply = null;

                if (!ping.cancelled) {
                    // call blocking receive so we timeout normally. 
                    EndPoint endPoint = new IPEndPoint (0, 0);
 
                    int bytesReceived = 0; 

                    while (true) { 
                        bytesReceived = ping.pingSocket.ReceiveFrom (ping.downlevelReplyBuffer, ref endPoint);
                        if (CorrectPacket(ping.downlevelReplyBuffer,ping.packet)) {
                            break;
                        } 
                        if ((System.Environment.TickCount - ping.startTime) > ping.llTimeout) {
                            reply = new PingReply(IPStatus.TimedOut); 
                            break; 
                        }
                    } 
                    int rttTime = System.Environment.TickCount - ping.startTime; // stop timing
                    if (reply == null) {
                        reply = new PingReply (ping.downlevelReplyBuffer, bytesReceived, ((IPEndPoint)endPoint).Address, rttTime);
                    } 

                    //construct the reply 
                    eventArgs = new PingCompletedEventArgs (reply, null, false, ping.asyncOp.UserSuppliedState); 
                }
            } 

            // in case of failure, create a failed event arg
            catch (Exception e) {
                PingReply reply = null; 
                PingException pe = null;
                SocketException e2 = e as SocketException; 
 
                if (e2 != null) {
                    //timed out 
                    if (e2.ErrorCode == TimeoutErrorCode) {
                        reply = new PingReply(IPStatus.TimedOut);
                    }
                    //buffer is too big 
                    else if (e2.ErrorCode == PacketTooBigErrorCode) {
                        reply = new PingReply(IPStatus.PacketTooBig); 
                    } 
                }
                if (reply == null) { 
                    pe = new PingException(SR.GetString(SR.net_ping), e);
                }
                eventArgs = new PingCompletedEventArgs (reply, pe, false, ping.asyncOp.UserSuppliedState);
            } 
            catch {
                PingException pe = new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                eventArgs = new PingCompletedEventArgs (null, pe, false, ping.asyncOp.UserSuppliedState); 
            }
 
            //otherwise, create real one
            try {
                if (ping.cancelled) {
                    eventArgs = new PingCompletedEventArgs (null, null, true, ping.asyncOp.UserSuppliedState); 
                }
                ping.asyncOp.PostOperationCompleted (ping.onPingCompletedDelegate, eventArgs); 
            } 
            finally {
                ping.inAsyncCall = false; 
            }
        }

        /*   private static void PingRecvCallback (IAsyncResult result) 
           {
               Ping ping = (Ping)result.AsyncState; 
               EndPoint endPoint = new IPEndPoint (0, 0); 
               int bytesReceived = ping.pingSocket.EndReceiveFrom (result, ref endPoint);
               PingReply reply = new PingReply (ping.downlevelReplyBuffer, bytesReceived, ((IPEndPoint)endPoint).Address, 0); 
               ping.inAsyncCall = false;
           }
   */
        public PingReply Send (string hostNameOrAddress) { 
            return Send (hostNameOrAddress, DefaultTimeout, DefaultSendBuffer, null);
        } 
 

        public PingReply Send (string hostNameOrAddress, int timeout) { 
            return Send (hostNameOrAddress, timeout, DefaultSendBuffer, null);
        }

 
        public PingReply Send (IPAddress address) {
            return Send (address, DefaultTimeout, DefaultSendBuffer, null); 
        } 

        public PingReply Send (IPAddress address, int timeout) { 
            return Send (address, timeout, DefaultSendBuffer, null);
        }

        public PingReply Send (string hostNameOrAddress, int timeout, byte[] buffer) { 
            return Send (hostNameOrAddress, timeout, buffer, null);
        } 
 
        public PingReply Send (IPAddress address, int timeout, byte[] buffer) {
            return Send (address, timeout, buffer, null); 
        }

        public PingReply Send (string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options) {
            if (ValidationHelper.IsBlankString(hostNameOrAddress)) { 
                throw new ArgumentNullException ("hostNameOrAddress");
            } 
 
            IPAddress address;
            try { 
                address = Dns.GetHostAddresses(hostNameOrAddress)[0];
            }
            catch (ArgumentException)
            { 
                throw;
            } 
            catch (Exception ex) { 
                throw new PingException(SR.GetString(SR.net_ping), ex);
            } 
            return Send(address, timeout, buffer, options);
        }

 
        public PingReply Send (IPAddress address, int timeout, byte[] buffer, PingOptions options) {
            if (buffer == null) { 
                throw new ArgumentNullException ("buffer"); 
            }
 
            if (buffer.Length > MaxBufferSize ) {
                throw new ArgumentException(SR.GetString(SR.net_invalidPingBufferSize), "buffer");
            }
 
            if (timeout < 0) {
                throw new ArgumentOutOfRangeException ("timeout"); 
            } 

            if (address == null) { 
                throw new ArgumentNullException ("address");
            }

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) 
            {
                throw new ArgumentException(SR.GetString(SR.net_invalid_ip_addr), "address"); 
            } 

            if (inAsyncCall == true) { 
                throw new InvalidOperationException(SR.GetString(SR.net_inasync));
            }

            if (disposed) { 
                throw new ObjectDisposedException (this.GetType ().FullName);
            } 
 
            //
            // FxCop: need to snapshot the address here, so we're sure that it's not changed between the permission 
            // and the operation, and to be sure that IPAddress.ToString() is called and not some override that
            // always returns "localhost" or something.
            //
            IPAddress addressSnapshot; 
            if (address.AddressFamily == AddressFamily.InterNetwork)
            { 
                addressSnapshot = new IPAddress(address.GetAddressBytes()); 
            }
            else 
            {
                addressSnapshot = new IPAddress(address.GetAddressBytes(), address.ScopeId);
            }
 
            (new NetworkInformationPermission(NetworkInformationAccess.Ping)).Demand();
 
            try { 
                return InternalSend (addressSnapshot, buffer, timeout, options, false);
            } 
            catch (Exception e) {
                throw new PingException(SR.GetString(SR.net_ping), e);
            }
            catch { 
                throw new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException)));
            } 
        } 

 

        [HostProtection(ExternalThreading=true)]
        public void SendAsync (string hostNameOrAddress, object userToken) {
            SendAsync (hostNameOrAddress, DefaultTimeout, DefaultSendBuffer, userToken); 
        }
 
 
        [HostProtection(ExternalThreading=true)]
        public void SendAsync (string hostNameOrAddress, int timeout, object userToken) { 
            SendAsync (hostNameOrAddress, timeout, DefaultSendBuffer, userToken);
        }

 
        [HostProtection(ExternalThreading=true)]
        public void SendAsync (IPAddress address, object userToken) { 
            SendAsync (address, DefaultTimeout, DefaultSendBuffer, userToken); 
        }
 

        [HostProtection(ExternalThreading=true)]
        public void SendAsync (IPAddress address, int timeout, object userToken) {
            SendAsync (address, timeout, DefaultSendBuffer, userToken); 
        }
 
 
        [HostProtection(ExternalThreading=true)]
        public void SendAsync (string hostNameOrAddress, int timeout, byte[] buffer, object userToken) { 
            SendAsync (hostNameOrAddress, timeout, buffer, null, userToken);
        }

 
        [HostProtection(ExternalThreading=true)]
        public void SendAsync (IPAddress address, int timeout, byte[] buffer, object userToken) { 
            SendAsync (address, timeout, buffer, null, userToken); 
        }
 

        [HostProtection(ExternalThreading=true)]
        public void SendAsync (string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options, object userToken) {
            if (ValidationHelper.IsBlankString(hostNameOrAddress)) { 
                throw new ArgumentNullException ("hostNameOrAddress");
            } 
 
            if (buffer == null) {
                throw new ArgumentNullException ("buffer"); 
            }

            if (buffer.Length > MaxBufferSize ) {
                throw new ArgumentException(SR.GetString(SR.net_invalidPingBufferSize), "buffer"); 
            }
 
            if (timeout < 0) { 
                throw new ArgumentOutOfRangeException ("timeout");
            } 

            if (inAsyncCall == true) {
                throw new InvalidOperationException(SR.GetString(SR.net_inasync));
            } 

            if (disposed) { 
                throw new ObjectDisposedException (this.GetType ().FullName); 
            }
 
            IPAddress address;
            if (IPAddress.TryParse(hostNameOrAddress, out address))
            {
                SendAsync(address, timeout, buffer, options, userToken); 
                return;
            } 
 
            try {
                inAsyncCall = true; 
                asyncOp = AsyncOperationManager.CreateOperation (userToken);
                AsyncStateObject state = new AsyncStateObject(hostNameOrAddress,buffer,timeout,options,userToken);
                ThreadPool.QueueUserWorkItem(new WaitCallback(ContinueAsyncSend), state);
            } 
            catch (Exception e) {
                inAsyncCall = false; 
                throw new PingException(SR.GetString(SR.net_ping), e); 
            }
        } 



        [HostProtection(ExternalThreading=true)] 
        public void SendAsync (IPAddress address, int timeout, byte[] buffer, PingOptions options, object userToken) {
            if (buffer == null) { 
                throw new ArgumentNullException ("buffer"); 
            }
 
            if (buffer.Length > MaxBufferSize ) {
                throw new ArgumentException(SR.GetString(SR.net_invalidPingBufferSize), "buffer");
            }
 
            if (timeout < 0) {
                throw new ArgumentOutOfRangeException ("timeout"); 
            } 

            if (address == null) { 
                throw new ArgumentNullException ("address");
            }

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) 
            {
                throw new ArgumentException(SR.GetString(SR.net_invalid_ip_addr), "address"); 
            } 

            if (inAsyncCall == true) { 
                throw new InvalidOperationException(SR.GetString(SR.net_inasync));
            }

            if (disposed) { 
                throw new ObjectDisposedException (this.GetType ().FullName);
            } 
 
            //
            // FxCop: need to snapshot the address here, so we're sure that it's not changed between the permission 
            // and the operation, and to be sure that IPAddress.ToString() is called and not some override that
            // always returns "localhost" or something.
            //
 
            IPAddress addressSnapshot;
            if (address.AddressFamily == AddressFamily.InterNetwork) 
            { 
                addressSnapshot = new IPAddress(address.GetAddressBytes());
            } 
            else
            {
                addressSnapshot = new IPAddress(address.GetAddressBytes(), address.ScopeId);
            } 

            (new NetworkInformationPermission(NetworkInformationAccess.Ping)).Demand(); 
 
            try{
                inAsyncCall = true; 
                asyncOp = AsyncOperationManager.CreateOperation (userToken);
                InternalSend (addressSnapshot, buffer, timeout, options, true);
            }
 
            catch(Exception e){
                inAsyncCall = false; 
                throw new PingException(SR.GetString(SR.net_ping), e); 
            }
            catch { 
                inAsyncCall = false;
                throw new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException)));
            }
        } 

 
        internal class AsyncStateObject{ 
            internal AsyncStateObject(string hostName, byte[] buffer, int timeout, PingOptions options, object userToken)
            { 
                this.hostName = hostName;
                this.buffer = buffer;
                this.timeout = timeout;
                this.options = options; 
                this.userToken = userToken;
            } 
 
            internal byte[] buffer;
            internal string hostName; 
            internal int timeout;
            internal PingOptions options;
            internal object userToken;
        } 

        private void ContinueAsyncSend(object state) { 
            // 
            // FxCop: need to snapshot the address here, so we're sure that it's not changed between the permission
            // and the operation, and to be sure that IPAddress.ToString() is called and not some override that 
            // always returns "localhost" or something.
            //

            Debug.Assert(asyncOp != null, "Null AsyncOp?"); 

            AsyncStateObject stateObject = (AsyncStateObject) state; 
 
            try {
                IPAddress addressSnapshot = Dns.GetHostAddresses(stateObject.hostName)[0]; 

                (new NetworkInformationPermission(NetworkInformationAccess.Ping)).Demand();
                InternalSend (addressSnapshot, stateObject.buffer, stateObject.timeout, stateObject.options, true);
            } 

            catch(Exception e){ 
                PingException pe = new PingException(SR.GetString(SR.net_ping), e); 
                PingCompletedEventArgs eventArgs = new PingCompletedEventArgs (null, pe, false, asyncOp.UserSuppliedState);
                inAsyncCall = false; 
                asyncOp.PostOperationCompleted(onPingCompletedDelegate, eventArgs);
            }
            catch {
                PingException pe = new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                PingCompletedEventArgs eventArgs = new PingCompletedEventArgs (null, pe, false, asyncOp.UserSuppliedState);
                inAsyncCall = false; 
                asyncOp.PostOperationCompleted(onPingCompletedDelegate, eventArgs); 
            }
        } 



        // internal method responsible for sending echo request on win2k and higher 

        private PingReply InternalSend (IPAddress address, byte[] buffer, int timeout, PingOptions options, bool async) { 
            inAsyncCall = async; 
            cancelled = false;
 
            //for <win2k
            if (address.AddressFamily == AddressFamily.InterNetworkV6 && !ComNetOS.IsPostWin2K) {
                throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));
            } 

            if (!ComNetOS.IsWin2K ) { 
                return InternalDownLevelSend (address, buffer, timeout, options, async); 
            }
 
            ipv6 =  (address.AddressFamily == AddressFamily.InterNetworkV6)?true:false;
            sendSize = buffer.Length;

            //get and cache correct handle 
            if (!ipv6 && handlePingV4 == null) {
                if (ComNetOS.IsPostWin2K) { 
                    handlePingV4 = UnsafeNetInfoNativeMethods.IcmpCreateFile (); 
                }
                else{ 
                    handlePingV4 = UnsafeIcmpNativeMethods.IcmpCreateFile ();
                }
            }
            else if (ipv6 && handlePingV6 == null) { 
                handlePingV6 = UnsafeNetInfoNativeMethods.Icmp6CreateFile ();
            } 
 

            //setup the options 
            IPOptions ipOptions = new IPOptions (options);

            //setup the reply buffer
            if (replyBuffer == null) { 
                replyBuffer = SafeLocalFree.LocalAlloc (MaxUdpPacket);
            } 
 
            //queue the event
            int error; 

            if (registeredWait != null)
            {
                registeredWait.Unregister(null); 
                registeredWait = null;
            } 
            try 
            {
                if (async) { 
                    if (pingEvent == null)
                        pingEvent = new ManualResetEvent (false);
                    else
                        pingEvent.Reset(); 

                    registeredWait = ThreadPool.RegisterWaitForSingleObject (pingEvent, new WaitOrTimerCallback (PingCallback), this, -1, true); 
                } 

                //Copy user dfata into the native world 
                SetUnmanagedStructures (buffer);

                if (!ipv6) {
                    if (ComNetOS.IsPostWin2K) { 
                        if (async) {
                            error = (int)UnsafeNetInfoNativeMethods.IcmpSendEcho2 (handlePingV4, pingEvent.SafeWaitHandle, IntPtr.Zero, IntPtr.Zero, (uint)address.m_Address, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout); 
                        } 
                        else{
                            error = (int)UnsafeNetInfoNativeMethods.IcmpSendEcho2 (handlePingV4, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)address.m_Address, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout); 
                        }
                    }
                    else{
                        if(async){ 
                            error = (int)UnsafeIcmpNativeMethods.IcmpSendEcho2 (handlePingV4, pingEvent.SafeWaitHandle, IntPtr.Zero, IntPtr.Zero, (uint)address.m_Address, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout);
                        } 
                        else{ 
                            error = (int)UnsafeIcmpNativeMethods.IcmpSendEcho2 (handlePingV4, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)address.m_Address, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout);
                        } 
                    }
                }
                else {
                    IPEndPoint ep = new IPEndPoint (address, 0); 
                    SocketAddress remoteAddr = ep.Serialize ();
                    byte[] sourceAddr = new byte[28]; 
                    if(async){ 
                        error = (int)UnsafeNetInfoNativeMethods.Icmp6SendEcho2 (handlePingV6, pingEvent.SafeWaitHandle, IntPtr.Zero, IntPtr.Zero, sourceAddr, remoteAddr.m_Buffer, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout);
                    } 
                    else{
                        error = (int)UnsafeNetInfoNativeMethods.Icmp6SendEcho2 (handlePingV6, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, sourceAddr, remoteAddr.m_Buffer, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout);
                    }
                } 
            }
            catch 
            { 
                if (registeredWait != null)
                { 
                    registeredWait.Unregister(null);
                }
                throw;
            } 

 
            //need this if something is bogus. 
            if (error == 0) {
                error = (int)Marshal.GetLastWin32Error();//UnsafeNclNativeMethods.OSSOCK.GetLastError(); 
                if (error != 0) {
                   FreeUnmanagedStructures ();
                   return new PingReply((IPStatus)error);
                } 
            }
 
            if (async) { 
                return null;
            } 


            FreeUnmanagedStructures ();
 
            //return the reply
            if (ipv6) { 
                Icmp6EchoReply icmp6Reply = (Icmp6EchoReply)Marshal.PtrToStructure (replyBuffer.DangerousGetHandle (), typeof(Icmp6EchoReply)); 
                return new PingReply (icmp6Reply,replyBuffer.DangerousGetHandle(),sendSize);
            } 

            IcmpEchoReply icmpReply = (IcmpEchoReply)Marshal.PtrToStructure (replyBuffer.DangerousGetHandle (), typeof(IcmpEchoReply));
            return new PingReply (icmpReply);
        } 

 
        // for echo requests < win2k 
        // ipv6 is not supported on these platforms
        // although we can set the ttl and fragment headers, we won't be able to return these 
        // in the reply, therefore the options instance will always be null in this case.

        private PingReply InternalDownLevelSend (IPAddress address, byte[] buffer, int timeout, PingOptions options, bool async) {
 
            try {
                //set the options and reply buffer 
                if (options == null) { 
                    options = new PingOptions ();
                } 
                if (downlevelReplyBuffer == null) {
                    downlevelReplyBuffer = new byte[64000];
                }
 
                llTimeout = timeout;
 
                //create a new icmppacket and get the actual icmp send buffer 
                packet = new IcmpPacket (buffer);
                byte[] sendBuffer = packet.GetBytes (); 

                //setup the send and receive endpoints
                IPEndPoint epServer = new IPEndPoint (address, 0);
                EndPoint endPoint = (EndPoint)new IPEndPoint (IPAddress.Any, 0); 

 
                // get and save a new socket 
                if (pingSocket == null) {
                    pingSocket = new Socket (AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp); 
                }

                //Set the socket timeouts and icmp options
                pingSocket.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, (int)timeout); 
                pingSocket.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.SendTimeout, (int)timeout);
                pingSocket.SetSocketOption (SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, options.Ttl); 
                pingSocket.SetSocketOption (SocketOptionLevel.IP, SocketOptionName.DontFragment, options.DontFragment); 

 
                //start the ping and start timing
                int bytesReceived = 0;
                int rttTime = 0;
 
                startTime = System.Environment.TickCount;
 
                if (async) { 
                    pingSocket.BeginSendTo (sendBuffer, 0, sendBuffer.Length, SocketFlags.None, epServer, new AsyncCallback (PingSendCallback), this);
                    return null; 
                }
                pingSocket.SendTo (sendBuffer, sendBuffer.Length, SocketFlags.None, epServer);

                while (true) { 
                    bytesReceived = pingSocket.ReceiveFrom (downlevelReplyBuffer, ref endPoint);
                    if (CorrectPacket(downlevelReplyBuffer,packet)) { 
                        break; 
                    }
                    if ((System.Environment.TickCount - startTime) > llTimeout) { 
                        return new PingReply(IPStatus.TimedOut);
                    }
                }
 
                //construct and return the reply
                rttTime = System.Environment.TickCount - startTime; // stop timing 
                return new PingReply (downlevelReplyBuffer, bytesReceived, ((IPEndPoint)endPoint).Address, rttTime); 
            }
 
            catch (SocketException e)
            {
                //timed out, sync is implied
                if (e.ErrorCode == TimeoutErrorCode) { 
                    return new PingReply(IPStatus.TimedOut);
                } 
                //buffer too big 
                else if (e.ErrorCode == PacketTooBigErrorCode) {
                    PingReply reply = new PingReply(IPStatus.PacketTooBig); 
                    if (!async) {
                        return reply;
                    }
                    PingCompletedEventArgs eventArgs = new PingCompletedEventArgs(reply,null, false, asyncOp.UserSuppliedState); 
                    asyncOp.PostOperationCompleted (onPingCompletedDelegate, eventArgs);
                    return null; 
 
                }
                else 
                    throw e;
            }
        }
        // copies sendbuffer into unmanaged memory for async icmpsendecho apis 
        private unsafe void SetUnmanagedStructures (byte[] buffer) {
            requestBuffer = SafeLocalFree.LocalAlloc(buffer.Length); 
            byte* dst = (byte*)requestBuffer.DangerousGetHandle(); 
            for (int i = 0; i < buffer.Length; ++i)
            { 
                dst[i] = buffer[i];
            }
        }
 
        // release the unmanaged memory after ping completion
        void FreeUnmanagedStructures () { 
            if (requestBuffer != null) { 
                requestBuffer.Close();
                requestBuffer = null; 
            }
        }

 
        // creates a default send buffer if a buffer wasn't specified.  This follows the
        // ping.exe model 
        private byte[] DefaultSendBuffer { 
            get {
                if (defaultSendBuffer == null) { 
                    defaultSendBuffer = new byte[DefaultSendBufferSize];
                    for (int i=0;i<DefaultSendBufferSize;i++)
                        defaultSendBuffer[i] = (byte)((int)'a'+ i % 23);
                } 
                return defaultSendBuffer;
            } 
        } 

 
        internal static bool CorrectPacket(byte[] buffer, IcmpPacket packet) {
            //if no error
            if (buffer[20] == 0 && buffer[21] == 0) {
                //id and sequence 
                if ((((buffer[25] << 8) | buffer[24]) == packet.Identifier) &&
                    (((buffer[27] << 8) | buffer[26]) == packet.sequenceNumber)) { 
                    return true; 
                }
            } 
            else {
                //id and sequence for icmp error
                if ((((buffer[53] << 8) | buffer[52]) == packet.Identifier) &&
                    (((buffer[55] << 8) | buffer[54]) == packet.sequenceNumber)) { 
                    return true;
 
                } 
            }
            return false; 
        }
    }

 
    // downlevel representation of an icmp packet
    internal class IcmpPacket 
    { 
        static ushort staticSequenceNumber = 0;
 
        internal byte type;             // type of message
        internal byte subCode = 0;          // type of sub code
        internal ushort checkSum;         // ones complement checksum of struct
        static internal ushort identifier = 0;      // identifier 
        internal ushort sequenceNumber = 0;   // sequence number
        internal byte[] buffer;             // byte array of buffer 
 
        //creates the icmppacket from a buffer
        internal IcmpPacket (byte[] buffer) { 
            type = 8; // echo request
            //identifier = 0x2d;
            this.buffer = buffer;
            sequenceNumber = staticSequenceNumber ++; 
            checkSum = (ushort)GetCheckSum ();
        } 
 
        internal ushort Identifier{
            get{ 
                if (identifier == 0) {
                    identifier = (ushort)Process.GetCurrentProcess().Id;
                }
                return identifier; 
            }
        } 
 

        //performs a checksum operation on the entire structure 
        private uint GetCheckSum () {
            uint total = (uint)type + Identifier + sequenceNumber;
            for (int i = 0; i < buffer.Length; i++) {
                total += (uint)buffer[i] + (uint)(buffer[++i] << 8); 
            }
            total = (total >> 16) + (total & 0xffff); 
            total += (total >> 16); 
            return(~total);
        } 

        //converts the structure into a byte array
        internal byte[] GetBytes() {
            byte[] bytes = new byte[buffer.Length + 8]; 

            byte[] b_cksum = BitConverter.GetBytes (checkSum); 
            byte[] b_id = BitConverter.GetBytes (Identifier); 
            byte[] b_seq = BitConverter.GetBytes (sequenceNumber);
            bytes[0] = type ; 
            bytes[1] = subCode ;
            Array.Copy ( b_cksum , 0 , bytes , 2 , 2 ) ;
            Array.Copy ( b_id , 0 , bytes , 4 , 2 ) ;
            Array.Copy (b_seq, 0, bytes, 6, 2); 
            Array.Copy ( buffer , 0 , bytes , 8 , buffer . Length ) ;
            return bytes; 
        } 
    } // class IcmpPacket
} 


 
namespace System.Net.NetworkInformation {

    using System.Net;
    using System.Net.Sockets; 
    using System;
    using System.Runtime.InteropServices; 
    using System.Threading; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Security.Permissions;

    public delegate void PingCompletedEventHandler (object sender, PingCompletedEventArgs e);
 

    public class PingCompletedEventArgs: System.ComponentModel.AsyncCompletedEventArgs { 
        PingReply reply; 

        internal PingCompletedEventArgs (PingReply reply, Exception error, bool cancelled, object userToken):base(error,cancelled,userToken) { 
            this.reply = reply;
        }
        public PingReply Reply{get {return reply;}}
    } 

    public class Ping:Component,IDisposable 
    { 
        const int MaxUdpPacket = 0xFFFF + 256; // Marshal.SizeOf(typeof(Icmp6EchoReply)) * 2 + ip header info;
        const int MaxBufferSize = 65500; //artificial constraint due to win32 api limitations. 
        const int DefaultTimeout = 5000; //5 seconds same as ping.exe
        const int DefaultSendBufferSize = 32;  //same as ping.exe

        byte[] defaultSendBuffer = null; 
        bool ipv6 = false;
        bool inAsyncCall = false; 
        bool cancelled = false; 
        bool disposed = false;
 
        //used for icmpsendecho apis
        internal ManualResetEvent pingEvent = null;
        private RegisteredWaitHandle registeredWait = null;
        SafeLocalFree requestBuffer = null; 
        SafeLocalFree replyBuffer = null;
        int sendSize = 0;  //needed to determine what reply size is for ipv6 in callback 
 
        //used for downlevel calls
        const int TimeoutErrorCode = 0x274c; 
        const int PacketTooBigErrorCode = 0x2738;
        Socket pingSocket = null;
        byte[] downlevelReplyBuffer = null;
        SafeCloseIcmpHandle handlePingV4 = null; 
        SafeCloseIcmpHandle handlePingV6 = null;
        int startTime = 0; 
        IcmpPacket packet = null; 
        int llTimeout = 0;
 
        //new async event support
        AsyncOperation asyncOp = null;
        SendOrPostCallback onPingCompletedDelegate;
        public event PingCompletedEventHandler PingCompleted; 

 
        protected void OnPingCompleted(PingCompletedEventArgs e) 
        {
            if (PingCompleted != null) { 
                PingCompleted (this,e);
            }
        }
 
        void PingCompletedWaitCallback (object operationState) {
            OnPingCompleted((PingCompletedEventArgs)operationState); 
        } 

        public Ping () { 
            onPingCompletedDelegate = new SendOrPostCallback (PingCompletedWaitCallback);
        }

        //cancel pending async requests, close the handles 
        private void InternalDispose () {
            if (disposed) { 
                return; 
            }
 
            disposed = true;

            if (inAsyncCall) {
                SendAsyncCancel (); 
            }
 
            if (pingSocket != null) 
            {
                pingSocket.Close (); 
                pingSocket = null;
            }

            if (handlePingV4 != null) { 
                handlePingV4.Close ();
                handlePingV4 = null; 
            } 

            if (handlePingV6 != null) { 
                handlePingV6.Close ();
                handlePingV6 = null;
            }
 
            if (registeredWait != null)
            { 
                registeredWait.Unregister(null); 
            }
 
            if (pingEvent != null) {
                pingEvent.Close();
            }
 
            if (replyBuffer != null) {
                replyBuffer.Close(); 
            } 
        }
 

        /// <internalonly/>
        void IDisposable.Dispose () {
            InternalDispose (); 
        }
 
 
        //cancels pending async calls
        public void SendAsyncCancel() { 
            lock (this) {
                if (!inAsyncCall) {
                    return;
                } 

                cancelled = true; 
 
                if (handlePingV4 != null) {
                    handlePingV4.Close (); 
                    handlePingV4 = null;
                }

                if (handlePingV6 != null) { 
                    handlePingV6.Close ();
                    handlePingV6 = null; 
                } 

                if (pingSocket != null) { 
                    pingSocket.Close ();
                    pingSocket = null;
                }
            } 

            if (pingEvent != null) { 
                pingEvent.WaitOne(); 
            }
        } 


        //private callback invoked when icmpsendecho apis succeed
        private static void PingCallback (object state, bool signaled) 
        {
            Ping ping = (Ping)state; 
            PingCompletedEventArgs eventArgs = null; 
            bool cancelled = false;
            AsyncOperation asyncOp = null; 
            SendOrPostCallback onPingCompletedDelegate = null;

            try
            { 
                lock(ping) {
                    cancelled = ping.cancelled; 
                    asyncOp = ping.asyncOp; 
                    onPingCompletedDelegate = ping.onPingCompletedDelegate;
 
                    if (!cancelled) {
                        //parse reply buffer
                        SafeLocalFree buffer = ping.replyBuffer;
 
                        int error = 0;
                        if (ping.ipv6) { 
                            error = (int)UnsafeNetInfoNativeMethods.Icmp6ParseReplies (buffer.DangerousGetHandle (), (uint)MaxUdpPacket); 
                        }
                        else { 
                            if (ComNetOS.IsPostWin2K) {
                                error = (int)UnsafeNetInfoNativeMethods.IcmpParseReplies (buffer.DangerousGetHandle (), (uint)MaxUdpPacket);
                            }
                            else{ 
                                error = (int)UnsafeIcmpNativeMethods.IcmpParseReplies (buffer.DangerousGetHandle (), (uint)MaxUdpPacket);
                            } 
                        } 

                        //looks like we get a lot of false failures here 
                        //if (error == 0){
                        //  error = Marshal.GetLastWin32Error();
                        //if (error != 0) {
                        //   throw new PingException(error);    looks like we get false failures here 
                        // if the status code isn't success.
                        //seems to be timing related 
                        // } 
                        //}
 
                        //marshals and constructs new reply
                        PingReply reply;

                        if (ping.ipv6) 
                        {
                            Icmp6EchoReply icmp6Reply = (Icmp6EchoReply)Marshal.PtrToStructure (buffer.DangerousGetHandle (), typeof(Icmp6EchoReply)); 
                            reply = new PingReply (icmp6Reply,buffer.DangerousGetHandle(),ping.sendSize); 
                        }
                        else { 
                            IcmpEchoReply icmpReply = (IcmpEchoReply)Marshal.PtrToStructure (buffer.DangerousGetHandle (), typeof(IcmpEchoReply));
                            reply = new PingReply (icmpReply);
                        }
 
                        eventArgs = new PingCompletedEventArgs (reply, null, false, asyncOp.UserSuppliedState);
                    } 
                } 
            }
            // in case of failure, create a failed event arg 
            catch (Exception e) {
                PingException pe = new PingException(SR.GetString(SR.net_ping), e);
                eventArgs = new PingCompletedEventArgs (null,pe, false, asyncOp.UserSuppliedState);
            } 
            catch {
                PingException pe = new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                eventArgs = new PingCompletedEventArgs (null,pe, false, asyncOp.UserSuppliedState); 
            }
            finally { 
                ping.FreeUnmanagedStructures ();
                ping.inAsyncCall = false;
            }
 
            if (cancelled) {
                eventArgs = new PingCompletedEventArgs (null, null, true, asyncOp.UserSuppliedState); 
            } 
            asyncOp.PostOperationCompleted (onPingCompletedDelegate, eventArgs);
        } 


        //private callback invoked when downlevel send succeeds
        private static void PingSendCallback (IAsyncResult result) { 
            Ping ping = (Ping)(result.AsyncState);
            PingCompletedEventArgs eventArgs = null; 
 
            try {
                ping.pingSocket.EndSendTo (result); 
                PingReply reply = null;

                if (!ping.cancelled) {
                    // call blocking receive so we timeout normally. 
                    EndPoint endPoint = new IPEndPoint (0, 0);
 
                    int bytesReceived = 0; 

                    while (true) { 
                        bytesReceived = ping.pingSocket.ReceiveFrom (ping.downlevelReplyBuffer, ref endPoint);
                        if (CorrectPacket(ping.downlevelReplyBuffer,ping.packet)) {
                            break;
                        } 
                        if ((System.Environment.TickCount - ping.startTime) > ping.llTimeout) {
                            reply = new PingReply(IPStatus.TimedOut); 
                            break; 
                        }
                    } 
                    int rttTime = System.Environment.TickCount - ping.startTime; // stop timing
                    if (reply == null) {
                        reply = new PingReply (ping.downlevelReplyBuffer, bytesReceived, ((IPEndPoint)endPoint).Address, rttTime);
                    } 

                    //construct the reply 
                    eventArgs = new PingCompletedEventArgs (reply, null, false, ping.asyncOp.UserSuppliedState); 
                }
            } 

            // in case of failure, create a failed event arg
            catch (Exception e) {
                PingReply reply = null; 
                PingException pe = null;
                SocketException e2 = e as SocketException; 
 
                if (e2 != null) {
                    //timed out 
                    if (e2.ErrorCode == TimeoutErrorCode) {
                        reply = new PingReply(IPStatus.TimedOut);
                    }
                    //buffer is too big 
                    else if (e2.ErrorCode == PacketTooBigErrorCode) {
                        reply = new PingReply(IPStatus.PacketTooBig); 
                    } 
                }
                if (reply == null) { 
                    pe = new PingException(SR.GetString(SR.net_ping), e);
                }
                eventArgs = new PingCompletedEventArgs (reply, pe, false, ping.asyncOp.UserSuppliedState);
            } 
            catch {
                PingException pe = new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                eventArgs = new PingCompletedEventArgs (null, pe, false, ping.asyncOp.UserSuppliedState); 
            }
 
            //otherwise, create real one
            try {
                if (ping.cancelled) {
                    eventArgs = new PingCompletedEventArgs (null, null, true, ping.asyncOp.UserSuppliedState); 
                }
                ping.asyncOp.PostOperationCompleted (ping.onPingCompletedDelegate, eventArgs); 
            } 
            finally {
                ping.inAsyncCall = false; 
            }
        }

        /*   private static void PingRecvCallback (IAsyncResult result) 
           {
               Ping ping = (Ping)result.AsyncState; 
               EndPoint endPoint = new IPEndPoint (0, 0); 
               int bytesReceived = ping.pingSocket.EndReceiveFrom (result, ref endPoint);
               PingReply reply = new PingReply (ping.downlevelReplyBuffer, bytesReceived, ((IPEndPoint)endPoint).Address, 0); 
               ping.inAsyncCall = false;
           }
   */
        public PingReply Send (string hostNameOrAddress) { 
            return Send (hostNameOrAddress, DefaultTimeout, DefaultSendBuffer, null);
        } 
 

        public PingReply Send (string hostNameOrAddress, int timeout) { 
            return Send (hostNameOrAddress, timeout, DefaultSendBuffer, null);
        }

 
        public PingReply Send (IPAddress address) {
            return Send (address, DefaultTimeout, DefaultSendBuffer, null); 
        } 

        public PingReply Send (IPAddress address, int timeout) { 
            return Send (address, timeout, DefaultSendBuffer, null);
        }

        public PingReply Send (string hostNameOrAddress, int timeout, byte[] buffer) { 
            return Send (hostNameOrAddress, timeout, buffer, null);
        } 
 
        public PingReply Send (IPAddress address, int timeout, byte[] buffer) {
            return Send (address, timeout, buffer, null); 
        }

        public PingReply Send (string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options) {
            if (ValidationHelper.IsBlankString(hostNameOrAddress)) { 
                throw new ArgumentNullException ("hostNameOrAddress");
            } 
 
            IPAddress address;
            try { 
                address = Dns.GetHostAddresses(hostNameOrAddress)[0];
            }
            catch (ArgumentException)
            { 
                throw;
            } 
            catch (Exception ex) { 
                throw new PingException(SR.GetString(SR.net_ping), ex);
            } 
            return Send(address, timeout, buffer, options);
        }

 
        public PingReply Send (IPAddress address, int timeout, byte[] buffer, PingOptions options) {
            if (buffer == null) { 
                throw new ArgumentNullException ("buffer"); 
            }
 
            if (buffer.Length > MaxBufferSize ) {
                throw new ArgumentException(SR.GetString(SR.net_invalidPingBufferSize), "buffer");
            }
 
            if (timeout < 0) {
                throw new ArgumentOutOfRangeException ("timeout"); 
            } 

            if (address == null) { 
                throw new ArgumentNullException ("address");
            }

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) 
            {
                throw new ArgumentException(SR.GetString(SR.net_invalid_ip_addr), "address"); 
            } 

            if (inAsyncCall == true) { 
                throw new InvalidOperationException(SR.GetString(SR.net_inasync));
            }

            if (disposed) { 
                throw new ObjectDisposedException (this.GetType ().FullName);
            } 
 
            //
            // FxCop: need to snapshot the address here, so we're sure that it's not changed between the permission 
            // and the operation, and to be sure that IPAddress.ToString() is called and not some override that
            // always returns "localhost" or something.
            //
            IPAddress addressSnapshot; 
            if (address.AddressFamily == AddressFamily.InterNetwork)
            { 
                addressSnapshot = new IPAddress(address.GetAddressBytes()); 
            }
            else 
            {
                addressSnapshot = new IPAddress(address.GetAddressBytes(), address.ScopeId);
            }
 
            (new NetworkInformationPermission(NetworkInformationAccess.Ping)).Demand();
 
            try { 
                return InternalSend (addressSnapshot, buffer, timeout, options, false);
            } 
            catch (Exception e) {
                throw new PingException(SR.GetString(SR.net_ping), e);
            }
            catch { 
                throw new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException)));
            } 
        } 

 

        [HostProtection(ExternalThreading=true)]
        public void SendAsync (string hostNameOrAddress, object userToken) {
            SendAsync (hostNameOrAddress, DefaultTimeout, DefaultSendBuffer, userToken); 
        }
 
 
        [HostProtection(ExternalThreading=true)]
        public void SendAsync (string hostNameOrAddress, int timeout, object userToken) { 
            SendAsync (hostNameOrAddress, timeout, DefaultSendBuffer, userToken);
        }

 
        [HostProtection(ExternalThreading=true)]
        public void SendAsync (IPAddress address, object userToken) { 
            SendAsync (address, DefaultTimeout, DefaultSendBuffer, userToken); 
        }
 

        [HostProtection(ExternalThreading=true)]
        public void SendAsync (IPAddress address, int timeout, object userToken) {
            SendAsync (address, timeout, DefaultSendBuffer, userToken); 
        }
 
 
        [HostProtection(ExternalThreading=true)]
        public void SendAsync (string hostNameOrAddress, int timeout, byte[] buffer, object userToken) { 
            SendAsync (hostNameOrAddress, timeout, buffer, null, userToken);
        }

 
        [HostProtection(ExternalThreading=true)]
        public void SendAsync (IPAddress address, int timeout, byte[] buffer, object userToken) { 
            SendAsync (address, timeout, buffer, null, userToken); 
        }
 

        [HostProtection(ExternalThreading=true)]
        public void SendAsync (string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options, object userToken) {
            if (ValidationHelper.IsBlankString(hostNameOrAddress)) { 
                throw new ArgumentNullException ("hostNameOrAddress");
            } 
 
            if (buffer == null) {
                throw new ArgumentNullException ("buffer"); 
            }

            if (buffer.Length > MaxBufferSize ) {
                throw new ArgumentException(SR.GetString(SR.net_invalidPingBufferSize), "buffer"); 
            }
 
            if (timeout < 0) { 
                throw new ArgumentOutOfRangeException ("timeout");
            } 

            if (inAsyncCall == true) {
                throw new InvalidOperationException(SR.GetString(SR.net_inasync));
            } 

            if (disposed) { 
                throw new ObjectDisposedException (this.GetType ().FullName); 
            }
 
            IPAddress address;
            if (IPAddress.TryParse(hostNameOrAddress, out address))
            {
                SendAsync(address, timeout, buffer, options, userToken); 
                return;
            } 
 
            try {
                inAsyncCall = true; 
                asyncOp = AsyncOperationManager.CreateOperation (userToken);
                AsyncStateObject state = new AsyncStateObject(hostNameOrAddress,buffer,timeout,options,userToken);
                ThreadPool.QueueUserWorkItem(new WaitCallback(ContinueAsyncSend), state);
            } 
            catch (Exception e) {
                inAsyncCall = false; 
                throw new PingException(SR.GetString(SR.net_ping), e); 
            }
        } 



        [HostProtection(ExternalThreading=true)] 
        public void SendAsync (IPAddress address, int timeout, byte[] buffer, PingOptions options, object userToken) {
            if (buffer == null) { 
                throw new ArgumentNullException ("buffer"); 
            }
 
            if (buffer.Length > MaxBufferSize ) {
                throw new ArgumentException(SR.GetString(SR.net_invalidPingBufferSize), "buffer");
            }
 
            if (timeout < 0) {
                throw new ArgumentOutOfRangeException ("timeout"); 
            } 

            if (address == null) { 
                throw new ArgumentNullException ("address");
            }

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) 
            {
                throw new ArgumentException(SR.GetString(SR.net_invalid_ip_addr), "address"); 
            } 

            if (inAsyncCall == true) { 
                throw new InvalidOperationException(SR.GetString(SR.net_inasync));
            }

            if (disposed) { 
                throw new ObjectDisposedException (this.GetType ().FullName);
            } 
 
            //
            // FxCop: need to snapshot the address here, so we're sure that it's not changed between the permission 
            // and the operation, and to be sure that IPAddress.ToString() is called and not some override that
            // always returns "localhost" or something.
            //
 
            IPAddress addressSnapshot;
            if (address.AddressFamily == AddressFamily.InterNetwork) 
            { 
                addressSnapshot = new IPAddress(address.GetAddressBytes());
            } 
            else
            {
                addressSnapshot = new IPAddress(address.GetAddressBytes(), address.ScopeId);
            } 

            (new NetworkInformationPermission(NetworkInformationAccess.Ping)).Demand(); 
 
            try{
                inAsyncCall = true; 
                asyncOp = AsyncOperationManager.CreateOperation (userToken);
                InternalSend (addressSnapshot, buffer, timeout, options, true);
            }
 
            catch(Exception e){
                inAsyncCall = false; 
                throw new PingException(SR.GetString(SR.net_ping), e); 
            }
            catch { 
                inAsyncCall = false;
                throw new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException)));
            }
        } 

 
        internal class AsyncStateObject{ 
            internal AsyncStateObject(string hostName, byte[] buffer, int timeout, PingOptions options, object userToken)
            { 
                this.hostName = hostName;
                this.buffer = buffer;
                this.timeout = timeout;
                this.options = options; 
                this.userToken = userToken;
            } 
 
            internal byte[] buffer;
            internal string hostName; 
            internal int timeout;
            internal PingOptions options;
            internal object userToken;
        } 

        private void ContinueAsyncSend(object state) { 
            // 
            // FxCop: need to snapshot the address here, so we're sure that it's not changed between the permission
            // and the operation, and to be sure that IPAddress.ToString() is called and not some override that 
            // always returns "localhost" or something.
            //

            Debug.Assert(asyncOp != null, "Null AsyncOp?"); 

            AsyncStateObject stateObject = (AsyncStateObject) state; 
 
            try {
                IPAddress addressSnapshot = Dns.GetHostAddresses(stateObject.hostName)[0]; 

                (new NetworkInformationPermission(NetworkInformationAccess.Ping)).Demand();
                InternalSend (addressSnapshot, stateObject.buffer, stateObject.timeout, stateObject.options, true);
            } 

            catch(Exception e){ 
                PingException pe = new PingException(SR.GetString(SR.net_ping), e); 
                PingCompletedEventArgs eventArgs = new PingCompletedEventArgs (null, pe, false, asyncOp.UserSuppliedState);
                inAsyncCall = false; 
                asyncOp.PostOperationCompleted(onPingCompletedDelegate, eventArgs);
            }
            catch {
                PingException pe = new PingException(SR.GetString(SR.net_ping), new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                PingCompletedEventArgs eventArgs = new PingCompletedEventArgs (null, pe, false, asyncOp.UserSuppliedState);
                inAsyncCall = false; 
                asyncOp.PostOperationCompleted(onPingCompletedDelegate, eventArgs); 
            }
        } 



        // internal method responsible for sending echo request on win2k and higher 

        private PingReply InternalSend (IPAddress address, byte[] buffer, int timeout, PingOptions options, bool async) { 
            inAsyncCall = async; 
            cancelled = false;
 
            //for <win2k
            if (address.AddressFamily == AddressFamily.InterNetworkV6 && !ComNetOS.IsPostWin2K) {
                throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));
            } 

            if (!ComNetOS.IsWin2K ) { 
                return InternalDownLevelSend (address, buffer, timeout, options, async); 
            }
 
            ipv6 =  (address.AddressFamily == AddressFamily.InterNetworkV6)?true:false;
            sendSize = buffer.Length;

            //get and cache correct handle 
            if (!ipv6 && handlePingV4 == null) {
                if (ComNetOS.IsPostWin2K) { 
                    handlePingV4 = UnsafeNetInfoNativeMethods.IcmpCreateFile (); 
                }
                else{ 
                    handlePingV4 = UnsafeIcmpNativeMethods.IcmpCreateFile ();
                }
            }
            else if (ipv6 && handlePingV6 == null) { 
                handlePingV6 = UnsafeNetInfoNativeMethods.Icmp6CreateFile ();
            } 
 

            //setup the options 
            IPOptions ipOptions = new IPOptions (options);

            //setup the reply buffer
            if (replyBuffer == null) { 
                replyBuffer = SafeLocalFree.LocalAlloc (MaxUdpPacket);
            } 
 
            //queue the event
            int error; 

            if (registeredWait != null)
            {
                registeredWait.Unregister(null); 
                registeredWait = null;
            } 
            try 
            {
                if (async) { 
                    if (pingEvent == null)
                        pingEvent = new ManualResetEvent (false);
                    else
                        pingEvent.Reset(); 

                    registeredWait = ThreadPool.RegisterWaitForSingleObject (pingEvent, new WaitOrTimerCallback (PingCallback), this, -1, true); 
                } 

                //Copy user dfata into the native world 
                SetUnmanagedStructures (buffer);

                if (!ipv6) {
                    if (ComNetOS.IsPostWin2K) { 
                        if (async) {
                            error = (int)UnsafeNetInfoNativeMethods.IcmpSendEcho2 (handlePingV4, pingEvent.SafeWaitHandle, IntPtr.Zero, IntPtr.Zero, (uint)address.m_Address, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout); 
                        } 
                        else{
                            error = (int)UnsafeNetInfoNativeMethods.IcmpSendEcho2 (handlePingV4, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)address.m_Address, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout); 
                        }
                    }
                    else{
                        if(async){ 
                            error = (int)UnsafeIcmpNativeMethods.IcmpSendEcho2 (handlePingV4, pingEvent.SafeWaitHandle, IntPtr.Zero, IntPtr.Zero, (uint)address.m_Address, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout);
                        } 
                        else{ 
                            error = (int)UnsafeIcmpNativeMethods.IcmpSendEcho2 (handlePingV4, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)address.m_Address, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout);
                        } 
                    }
                }
                else {
                    IPEndPoint ep = new IPEndPoint (address, 0); 
                    SocketAddress remoteAddr = ep.Serialize ();
                    byte[] sourceAddr = new byte[28]; 
                    if(async){ 
                        error = (int)UnsafeNetInfoNativeMethods.Icmp6SendEcho2 (handlePingV6, pingEvent.SafeWaitHandle, IntPtr.Zero, IntPtr.Zero, sourceAddr, remoteAddr.m_Buffer, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout);
                    } 
                    else{
                        error = (int)UnsafeNetInfoNativeMethods.Icmp6SendEcho2 (handlePingV6, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, sourceAddr, remoteAddr.m_Buffer, requestBuffer, (ushort)buffer.Length, ref ipOptions, replyBuffer, MaxUdpPacket, (uint)timeout);
                    }
                } 
            }
            catch 
            { 
                if (registeredWait != null)
                { 
                    registeredWait.Unregister(null);
                }
                throw;
            } 

 
            //need this if something is bogus. 
            if (error == 0) {
                error = (int)Marshal.GetLastWin32Error();//UnsafeNclNativeMethods.OSSOCK.GetLastError(); 
                if (error != 0) {
                   FreeUnmanagedStructures ();
                   return new PingReply((IPStatus)error);
                } 
            }
 
            if (async) { 
                return null;
            } 


            FreeUnmanagedStructures ();
 
            //return the reply
            if (ipv6) { 
                Icmp6EchoReply icmp6Reply = (Icmp6EchoReply)Marshal.PtrToStructure (replyBuffer.DangerousGetHandle (), typeof(Icmp6EchoReply)); 
                return new PingReply (icmp6Reply,replyBuffer.DangerousGetHandle(),sendSize);
            } 

            IcmpEchoReply icmpReply = (IcmpEchoReply)Marshal.PtrToStructure (replyBuffer.DangerousGetHandle (), typeof(IcmpEchoReply));
            return new PingReply (icmpReply);
        } 

 
        // for echo requests < win2k 
        // ipv6 is not supported on these platforms
        // although we can set the ttl and fragment headers, we won't be able to return these 
        // in the reply, therefore the options instance will always be null in this case.

        private PingReply InternalDownLevelSend (IPAddress address, byte[] buffer, int timeout, PingOptions options, bool async) {
 
            try {
                //set the options and reply buffer 
                if (options == null) { 
                    options = new PingOptions ();
                } 
                if (downlevelReplyBuffer == null) {
                    downlevelReplyBuffer = new byte[64000];
                }
 
                llTimeout = timeout;
 
                //create a new icmppacket and get the actual icmp send buffer 
                packet = new IcmpPacket (buffer);
                byte[] sendBuffer = packet.GetBytes (); 

                //setup the send and receive endpoints
                IPEndPoint epServer = new IPEndPoint (address, 0);
                EndPoint endPoint = (EndPoint)new IPEndPoint (IPAddress.Any, 0); 

 
                // get and save a new socket 
                if (pingSocket == null) {
                    pingSocket = new Socket (AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp); 
                }

                //Set the socket timeouts and icmp options
                pingSocket.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, (int)timeout); 
                pingSocket.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.SendTimeout, (int)timeout);
                pingSocket.SetSocketOption (SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, options.Ttl); 
                pingSocket.SetSocketOption (SocketOptionLevel.IP, SocketOptionName.DontFragment, options.DontFragment); 

 
                //start the ping and start timing
                int bytesReceived = 0;
                int rttTime = 0;
 
                startTime = System.Environment.TickCount;
 
                if (async) { 
                    pingSocket.BeginSendTo (sendBuffer, 0, sendBuffer.Length, SocketFlags.None, epServer, new AsyncCallback (PingSendCallback), this);
                    return null; 
                }
                pingSocket.SendTo (sendBuffer, sendBuffer.Length, SocketFlags.None, epServer);

                while (true) { 
                    bytesReceived = pingSocket.ReceiveFrom (downlevelReplyBuffer, ref endPoint);
                    if (CorrectPacket(downlevelReplyBuffer,packet)) { 
                        break; 
                    }
                    if ((System.Environment.TickCount - startTime) > llTimeout) { 
                        return new PingReply(IPStatus.TimedOut);
                    }
                }
 
                //construct and return the reply
                rttTime = System.Environment.TickCount - startTime; // stop timing 
                return new PingReply (downlevelReplyBuffer, bytesReceived, ((IPEndPoint)endPoint).Address, rttTime); 
            }
 
            catch (SocketException e)
            {
                //timed out, sync is implied
                if (e.ErrorCode == TimeoutErrorCode) { 
                    return new PingReply(IPStatus.TimedOut);
                } 
                //buffer too big 
                else if (e.ErrorCode == PacketTooBigErrorCode) {
                    PingReply reply = new PingReply(IPStatus.PacketTooBig); 
                    if (!async) {
                        return reply;
                    }
                    PingCompletedEventArgs eventArgs = new PingCompletedEventArgs(reply,null, false, asyncOp.UserSuppliedState); 
                    asyncOp.PostOperationCompleted (onPingCompletedDelegate, eventArgs);
                    return null; 
 
                }
                else 
                    throw e;
            }
        }
        // copies sendbuffer into unmanaged memory for async icmpsendecho apis 
        private unsafe void SetUnmanagedStructures (byte[] buffer) {
            requestBuffer = SafeLocalFree.LocalAlloc(buffer.Length); 
            byte* dst = (byte*)requestBuffer.DangerousGetHandle(); 
            for (int i = 0; i < buffer.Length; ++i)
            { 
                dst[i] = buffer[i];
            }
        }
 
        // release the unmanaged memory after ping completion
        void FreeUnmanagedStructures () { 
            if (requestBuffer != null) { 
                requestBuffer.Close();
                requestBuffer = null; 
            }
        }

 
        // creates a default send buffer if a buffer wasn't specified.  This follows the
        // ping.exe model 
        private byte[] DefaultSendBuffer { 
            get {
                if (defaultSendBuffer == null) { 
                    defaultSendBuffer = new byte[DefaultSendBufferSize];
                    for (int i=0;i<DefaultSendBufferSize;i++)
                        defaultSendBuffer[i] = (byte)((int)'a'+ i % 23);
                } 
                return defaultSendBuffer;
            } 
        } 

 
        internal static bool CorrectPacket(byte[] buffer, IcmpPacket packet) {
            //if no error
            if (buffer[20] == 0 && buffer[21] == 0) {
                //id and sequence 
                if ((((buffer[25] << 8) | buffer[24]) == packet.Identifier) &&
                    (((buffer[27] << 8) | buffer[26]) == packet.sequenceNumber)) { 
                    return true; 
                }
            } 
            else {
                //id and sequence for icmp error
                if ((((buffer[53] << 8) | buffer[52]) == packet.Identifier) &&
                    (((buffer[55] << 8) | buffer[54]) == packet.sequenceNumber)) { 
                    return true;
 
                } 
            }
            return false; 
        }
    }

 
    // downlevel representation of an icmp packet
    internal class IcmpPacket 
    { 
        static ushort staticSequenceNumber = 0;
 
        internal byte type;             // type of message
        internal byte subCode = 0;          // type of sub code
        internal ushort checkSum;         // ones complement checksum of struct
        static internal ushort identifier = 0;      // identifier 
        internal ushort sequenceNumber = 0;   // sequence number
        internal byte[] buffer;             // byte array of buffer 
 
        //creates the icmppacket from a buffer
        internal IcmpPacket (byte[] buffer) { 
            type = 8; // echo request
            //identifier = 0x2d;
            this.buffer = buffer;
            sequenceNumber = staticSequenceNumber ++; 
            checkSum = (ushort)GetCheckSum ();
        } 
 
        internal ushort Identifier{
            get{ 
                if (identifier == 0) {
                    identifier = (ushort)Process.GetCurrentProcess().Id;
                }
                return identifier; 
            }
        } 
 

        //performs a checksum operation on the entire structure 
        private uint GetCheckSum () {
            uint total = (uint)type + Identifier + sequenceNumber;
            for (int i = 0; i < buffer.Length; i++) {
                total += (uint)buffer[i] + (uint)(buffer[++i] << 8); 
            }
            total = (total >> 16) + (total & 0xffff); 
            total += (total >> 16); 
            return(~total);
        } 

        //converts the structure into a byte array
        internal byte[] GetBytes() {
            byte[] bytes = new byte[buffer.Length + 8]; 

            byte[] b_cksum = BitConverter.GetBytes (checkSum); 
            byte[] b_id = BitConverter.GetBytes (Identifier); 
            byte[] b_seq = BitConverter.GetBytes (sequenceNumber);
            bytes[0] = type ; 
            bytes[1] = subCode ;
            Array.Copy ( b_cksum , 0 , bytes , 2 , 2 ) ;
            Array.Copy ( b_id , 0 , bytes , 4 , 2 ) ;
            Array.Copy (b_seq, 0, bytes, 6, 2); 
            Array.Copy ( buffer , 0 , bytes , 8 , buffer . Length ) ;
            return bytes; 
        } 
    } // class IcmpPacket
} 


