// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// WindowsIdentity.cs 
//
// Representation of a process/thread token. 
//

namespace System.Security.Principal
{ 
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles; 
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices; 
    using System.Runtime.Serialization;
    using System.Security.AccessControl;
    using System.Security.Permissions;
    using System.Text; 
    using System.Threading;
    using System.Runtime.Versioning; 
 
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)] 
    public enum WindowsAccountType {
        Normal      = 0,
        Guest       = 1,
        System      = 2, 
        Anonymous   = 3
    } 
 
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)] 
    public enum TokenImpersonationLevel {
        None            = 0,
        Anonymous       = 1,
        Identification  = 2, 
        Impersonation   = 3,
        Delegation      = 4 
    } 

    [Serializable, Flags] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum TokenAccessLevels {
        AssignPrimary       = 0x00000001,
        Duplicate           = 0x00000002, 
        Impersonate         = 0x00000004,
        Query               = 0x00000008, 
        QuerySource         = 0x00000010, 
        AdjustPrivileges    = 0x00000020,
        AdjustGroups        = 0x00000040, 
        AdjustDefault       = 0x00000080,
        AdjustSessionId     = 0x00000100,

        Read                = 0x00020000 | Query, 

        Write               = 0x00020000 | AdjustPrivileges | AdjustGroups | AdjustDefault, 
 
        AllAccess           = 0x000F0000       |
                              AssignPrimary    | 
                              Duplicate        |
                              Impersonate      |
                              Query            |
                              QuerySource      | 
                              AdjustPrivileges |
                              AdjustGroups     | 
                              AdjustDefault    | 
                              AdjustSessionId,
 
        MaximumAllowed      = 0x02000000
    }

    // Keep in sync with vm\comprincipal.h 
    internal enum WinSecurityContext {
        Thread = 1, // OpenAsSelf = false 
        Process = 2, // OpenAsSelf = true 
        Both = 3 // OpenAsSelf = true, then OpenAsSelf = false
    } 

    [Serializable()]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class WindowsIdentity : IIdentity, ISerializable, IDeserializationCallback, IDisposable { 
        private string m_name = null;
        private SecurityIdentifier m_owner = null; 
        private SecurityIdentifier m_user = null; 
        private object m_groups = null;
        private SafeTokenHandle m_safeTokenHandle = SafeTokenHandle.InvalidHandle; 
        private string m_authType = null;
        private int m_isAuthenticated = -1;

        // 
        // Constructors.
        // 
 
        private WindowsIdentity () {}
        internal WindowsIdentity (SafeTokenHandle safeTokenHandle) : this (safeTokenHandle.DangerousGetHandle()) { 
            GC.KeepAlive(safeTokenHandle);
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)] 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public WindowsIdentity (IntPtr userToken) : this (userToken, null, -1) {} 
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public WindowsIdentity (IntPtr userToken, string type) : this (userToken, type, -1) {}

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public WindowsIdentity (IntPtr userToken, string type, WindowsAccountType acctType) : this (userToken, type, -1) {}
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)] 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public WindowsIdentity (IntPtr userToken, string type, WindowsAccountType acctType, bool isAuthenticated) 
            : this (userToken, type, isAuthenticated ? 1 : 0) {}

        private WindowsIdentity (IntPtr userToken, string authType, int isAuthenticated) {
            CreateFromToken(userToken); 
            m_authType = authType;
            m_isAuthenticated = isAuthenticated; 
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        private void CreateFromToken (IntPtr userToken) {
            if (userToken == IntPtr.Zero)
                throw new ArgumentException(Environment.GetResourceString("Argument_TokenZero")); 

            // Find out if the specified token is a valid. 
            uint dwLength = (uint) Marshal.SizeOf(typeof(uint)); 
            bool result = Win32Native.GetTokenInformation(userToken, (uint) TokenInformationClass.TokenType,
                                                          SafeLocalAllocHandle.InvalidHandle, 0, out dwLength); 
            if (Marshal.GetLastWin32Error() == Win32Native.ERROR_INVALID_HANDLE)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidImpersonationToken"));

            if (!Win32Native.DuplicateHandle(Win32Native.GetCurrentProcess(), 
                                             userToken,
                                             Win32Native.GetCurrentProcess(), 
                                             ref m_safeTokenHandle, 
                                             0,
                                             true, 
                                             Win32Native.DUPLICATE_SAME_ACCESS))
                throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
        }
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public WindowsIdentity (string sUserPrincipalName) : this (sUserPrincipalName, null) {} 
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public WindowsIdentity (string sUserPrincipalName, string type) { 
            m_safeTokenHandle = KerbS4ULogon(sUserPrincipalName);
        }

        // 
        // We cannot make sure the token will stay alive
        // until it is being deserialized in another AppDomain. We do not have a way to capture 
        // the state of a token (just a pointer to kernel memory) and re-construct it later 
        // and even if we did (via calling NtQueryInformationToken and relying on the token undocumented
        // format), constructing a token requires TCB privilege. We need to address the "serializable" 
        // nature of WindowsIdentity since it is not obvious that can be achieved at all.
        //

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)] 
        public WindowsIdentity (SerializationInfo info, StreamingContext context) : this(info)
        { 
        } 

        // This is a copy of the serialization constructor above but without the 
        // security demand that's slow and breaks partial trust scenarios
        // without an expensive assert in place in the remoting code. Instead we
        // special case this class and call the private constructor directly
        // (changing the demand above is considered a breaking change, even 
        // though nobody else should have been using a serialization constructor
        // directly). 
        private WindowsIdentity(SerializationInfo info) { 
            IntPtr userToken = (IntPtr) info.GetValue("m_userToken", typeof(IntPtr));
            if (userToken != IntPtr.Zero) 
                CreateFromToken(userToken);
        }

        /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) { 
            info.AddValue("m_userToken", m_safeTokenHandle.DangerousGetHandle()); 
        }
 
        /// <internalonly/>
        void IDeserializationCallback.OnDeserialization (Object sender) {}

        // 
        // Factory methods.
        // 
 

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)] 
        public static WindowsIdentity GetCurrent () {
           return GetCurrentInternal(TokenAccessLevels.MaximumAllowed, false);
        }
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public static WindowsIdentity GetCurrent (bool ifImpersonating) { 
           return GetCurrentInternal(TokenAccessLevels.MaximumAllowed, ifImpersonating); 
        }
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public static WindowsIdentity GetCurrent (TokenAccessLevels desiredAccess) {
           return GetCurrentInternal(desiredAccess, false);
        } 

        // GetAnonymous() is used heavily in ASP.NET requests as a dummy identity to indicate 
        // the request is anonymous. It does not represent a real process or thread token so 
        // it cannot impersonate or do anything useful. Note this identity does not represent the
        // usual concept of an anonymous token, and the name is simply misleading but we cannot change it now. 

        public static WindowsIdentity GetAnonymous () {
            return new WindowsIdentity();
        } 

        // 
        // Properties. 
        //
 
        public string AuthenticationType {
            get {
                // If this is an anonymous identity, return an empty string
                if (m_safeTokenHandle.IsInvalid) 
                    return String.Empty;
 
                if (m_authType == null) { 
                    Win32Native.LUID authId = GetLogonAuthId(m_safeTokenHandle);
                    if (authId.LowPart == Win32Native.ANONYMOUS_LOGON_LUID) 
                        return String.Empty; // no authentication, just return an empty string

                    SafeLsaReturnBufferHandle ppLogonSessionData = SafeLsaReturnBufferHandle.InvalidHandle;
                    int status = Win32Native.LsaGetLogonSessionData(ref authId, ref ppLogonSessionData); 
                    if (status < 0) // non-negative numbers indicate success
                        throw GetExceptionFromNtStatus(status); 
 
                    Win32Native.SECURITY_LOGON_SESSION_DATA logonSessionData = (Win32Native.SECURITY_LOGON_SESSION_DATA) Marshal.PtrToStructure(ppLogonSessionData.DangerousGetHandle(), typeof(Win32Native.SECURITY_LOGON_SESSION_DATA));
                    string authPackage = Marshal.PtrToStringUni(logonSessionData.AuthenticationPackage.Buffer); 
                    ppLogonSessionData.Dispose();
                    return authPackage;
                }
 
                return m_authType;
            } 
        } 

 
        [ComVisible(false)]
        public TokenImpersonationLevel ImpersonationLevel {
            get {
                // If this is an anonymous identity 
                if (m_safeTokenHandle.IsInvalid)
                    return TokenImpersonationLevel.Anonymous; 
 
                uint cbSize = 0;
                SafeLocalAllocHandle pTokenType = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenType, out cbSize); 
                int level = Marshal.ReadInt32(pTokenType.DangerousGetHandle());
                if (level == (int) TokenType.TokenPrimary)
                    return TokenImpersonationLevel.None; // primary token
 
                // This is an impersonation token, get the impersonation level
                SafeLocalAllocHandle pImpersonationLevel = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenImpersonationLevel, out cbSize); 
                level = Marshal.ReadInt32(pImpersonationLevel.DangerousGetHandle()); 

                pTokenType.Dispose(); 
                pImpersonationLevel.Dispose();

                return (TokenImpersonationLevel) level + 1;
            } 
        }
 
        public virtual bool IsAuthenticated { 
            get {
                // make this a no-op on Win98 so calling code does not have to special case down-level platforms. 
                if (!RunningOnWin2K)
                    return false;

                if (m_isAuthenticated == -1) { 
                    // There is a known bug where this approach will not work correctly for domain guests (will return false
                    // instead of true). But this is a corner-case that is not very interesting. 
                    WindowsPrincipal wp = new WindowsPrincipal(this); 
                    SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                                    new int[] {Win32Native.SECURITY_AUTHENTICATED_USER_RID}); 
                    m_isAuthenticated = wp.IsInRole(sid) ? 1 : 0;
                }

                return m_isAuthenticated == 1; 
            }
        } 
 
        //
        // IsGuest, IsSystem and IsAnonymous are maintained for compatibility reasons. It is always 
        // possible to extract this same information from the User SID property and the new
        // (and more general) methods defined in the SID class (IsWellKnown, etc...).
        //
 
        public virtual bool IsGuest {
            get { 
                // special case the anonymous identity. 
                if (m_safeTokenHandle.IsInvalid)
                    return false; 
                SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                                new int[] {Win32Native.SECURITY_BUILTIN_DOMAIN_RID, Win32Native.DOMAIN_USER_RID_GUEST});
                return (this.User == sid);
            } 
        }
 
        public virtual bool IsSystem { 
            get {
                // special case the anonymous identity. 
                if (m_safeTokenHandle.IsInvalid)
                    return false;
                SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                                new int[] {Win32Native.SECURITY_LOCAL_SYSTEM_RID}); 
                return (this.User == sid);
            } 
        } 

        public virtual bool IsAnonymous { 
            get {
                // special case the anonymous identity.
                if (m_safeTokenHandle.IsInvalid)
                    return true; 
                SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                                new int[] {Win32Native.SECURITY_ANONYMOUS_LOGON_RID}); 
                return (this.User == sid); 
            }
        } 


        public virtual string Name {
            get { 
                return GetName();
            } 
        } 

        [DynamicSecurityMethodAttribute()] 
        internal String GetName()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            // special case the anonymous identity. 
            if (m_safeTokenHandle.IsInvalid)
                return String.Empty; 
 
            if (m_name == null)
            { 
                // revert thread impersonation for the duration of the call to get the name.
                using (SafeImpersonate(SafeTokenHandle.InvalidHandle, null, ref stackMark))
                {
                    NTAccount ntAccount = this.User.Translate(typeof(NTAccount)) as NTAccount; 
                    m_name = ntAccount.ToString();
                } 
            } 

            return m_name; 
        }

        [ComVisible(false)]
        public SecurityIdentifier Owner { 
            get {
                // special case the anonymous identity. 
                if (m_safeTokenHandle.IsInvalid) 
                    return null;
 
                if (m_owner == null) {
                    uint cbSize = 0;
                    SafeLocalAllocHandle pOwnerToken = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenOwner, out cbSize);
                    m_owner = new SecurityIdentifier(Marshal.ReadIntPtr(pOwnerToken.DangerousGetHandle()), true ); 
                    pOwnerToken.Dispose();
                } 
 
                return m_owner;
            } 
        }

        [ComVisible(false)]
        public SecurityIdentifier User { 
            get {
                // special case the anonymous identity. 
                if (m_safeTokenHandle.IsInvalid) 
                    return null;
 
                if (m_user == null) {
                    uint cbSize = 0;
                    SafeLocalAllocHandle pUserToken = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenUser, out cbSize);
                    m_user = new SecurityIdentifier(Marshal.ReadIntPtr(pUserToken.DangerousGetHandle()), true ); 
                    pUserToken.Dispose();
                } 
 
                return m_user;
            } 
        }

        public IdentityReferenceCollection Groups {
            get { 
                // special case the anonymous identity.
                if (m_safeTokenHandle.IsInvalid) 
                    return null; 

                if (m_groups == null) { 
                    IdentityReferenceCollection groups = new IdentityReferenceCollection();
                    uint cbSize = 0;
                    using (SafeLocalAllocHandle pGroups = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenGroups, out cbSize)) {
                        int count = Marshal.ReadInt32(pGroups.DangerousGetHandle()); 
                        IntPtr pSidAndAttributes = new IntPtr((long) pGroups.DangerousGetHandle() + (long) Marshal.OffsetOf(typeof(Win32Native.TOKEN_GROUPS), "Groups"));
                        for (int i = 0; i < count; ++i) { 
                            Win32Native.SID_AND_ATTRIBUTES group = (Win32Native.SID_AND_ATTRIBUTES) Marshal.PtrToStructure(pSidAndAttributes, typeof(Win32Native.SID_AND_ATTRIBUTES)); 
                            // Ignore disabled, logon ID, and deny-only groups.
                            uint mask = Win32Native.SE_GROUP_ENABLED | Win32Native.SE_GROUP_LOGON_ID | Win32Native.SE_GROUP_USE_FOR_DENY_ONLY; 
                            if ((group.Attributes & mask) == Win32Native.SE_GROUP_ENABLED) {
                                groups.Add(new SecurityIdentifier(group.Sid, true ));
                            }
                            pSidAndAttributes = new IntPtr((long) pSidAndAttributes + (long) Marshal.SizeOf(typeof(Win32Native.SID_AND_ATTRIBUTES))); 
                        }
                    } 
                    Interlocked.CompareExchange(ref m_groups, groups, null); 
                }
 
                return m_groups as IdentityReferenceCollection;
            }
        }
 
        //
        // Note this property does not duplicate the token. This is also the same as V1/Everett behaviour. 
        // 

        public virtual IntPtr Token { 
            [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            get {
                return m_safeTokenHandle.DangerousGetHandle();
            } 
        }
 
        // 
        // Public methods.
        // 
        [DynamicSecurityMethodAttribute()]
        public virtual WindowsImpersonationContext Impersonate ()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
            return Impersonate(ref stackMark);
        } 
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        [DynamicSecurityMethodAttribute()] 
        public static WindowsImpersonationContext Impersonate (IntPtr userToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            if (userToken == IntPtr.Zero) 
                return SafeImpersonate(SafeTokenHandle.InvalidHandle, null, ref stackMark);
 
            WindowsIdentity wi = new WindowsIdentity(userToken); 
            return wi.Impersonate(ref stackMark);
        } 

        internal WindowsImpersonationContext Impersonate (ref StackCrawlMark stackMark) {
            // make this a no-op on Win98 so calling code does not have to special case down-level platforms.
            if (!RunningOnWin2K) 
                return new WindowsImpersonationContext(SafeTokenHandle.InvalidHandle, GetCurrentThreadWI(), false, null);
 
            if (m_safeTokenHandle.IsInvalid) 
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AnonymousCannotImpersonate"));
 
            return SafeImpersonate(m_safeTokenHandle, this, ref stackMark);
        }

        [ComVisible(false)] 
        protected virtual void Dispose(bool disposing) {
            if (disposing) { 
                if (m_safeTokenHandle != null && !m_safeTokenHandle.IsClosed) 
                    m_safeTokenHandle.Dispose();
            } 
            m_name = null;
            m_owner = null;
            m_user = null;
        } 

        [ComVisible(false)] 
        public void Dispose() { 
            Dispose(true);
        } 

        //
        // internal.
        // 

        internal SafeTokenHandle TokenHandle { 
            get { 
                return m_safeTokenHandle;
            } 
        }

        [ResourceExposure(ResourceScope.Process)]
        [ResourceConsumption(ResourceScope.Process)] 
        internal static WindowsImpersonationContext SafeImpersonate (SafeTokenHandle userToken, WindowsIdentity wi, ref StackCrawlMark stackMark)
        { 
            // make this a no-op on Win98 so calling code does not have to special case down-level platforms. 
            if (!RunningOnWin2K)
                return new WindowsImpersonationContext(SafeTokenHandle.InvalidHandle, GetCurrentThreadWI(), false, null); 

            bool isImpersonating;
            int hr = 0;
            SafeTokenHandle safeTokenHandle = GetCurrentToken(TokenAccessLevels.MaximumAllowed, false, out isImpersonating, out hr); 
            if (safeTokenHandle == null || safeTokenHandle.IsInvalid)
                throw new SecurityException(Win32Native.GetMessage(hr)); 
 
            // Set the SafeTokenHandle on the FSD:
            FrameSecurityDescriptor secObj = SecurityRuntime.GetSecurityObjectForFrame(ref stackMark, true); 
            if (secObj == null)
            {
                if (SecurityManager._IsSecurityOn())
                    // Security: REQ_SQ flag is missing. Bad compiler ? 
                    // This can happen when you create delegates over functions that need the REQ_SQ
                    throw new SecurityException(Environment.GetResourceString( "ExecutionEngine_MissingSecurityDescriptor" ) ); 
            } 

            WindowsImpersonationContext context = new WindowsImpersonationContext(safeTokenHandle, GetCurrentThreadWI(), isImpersonating, secObj); 

            if (userToken.IsInvalid) { // impersonating a zero token means clear the token on the thread
                hr = Win32.RevertToSelf();
                if (hr < 0) 
                    throw new SecurityException(Win32Native.GetMessage(hr));
                // update identity on the thread 
                UpdateThreadWI(wi); 
                secObj.SetTokenHandles(safeTokenHandle, (wi == null?null:wi.TokenHandle));
            } else { 
                hr = Win32.RevertToSelf();
                if (hr < 0)
                    throw new SecurityException(Win32Native.GetMessage(hr));
                hr = Win32.ImpersonateLoggedOnUser(userToken); 
                if (hr < 0) {
                    context.Undo(); 
                    throw new SecurityException(Environment.GetResourceString("Argument_ImpersonateUser")); 
                }
                UpdateThreadWI(wi); 
                secObj.SetTokenHandles(safeTokenHandle, (wi == null?null:wi.TokenHandle));
            }

            return context; 
        }
 
        internal static WindowsIdentity GetCurrentThreadWI() 
        {
            return SecurityContext.GetCurrentWI(Thread.CurrentThread.GetExecutionContextNoCreate()); 
        }

        internal static void UpdateThreadWI(WindowsIdentity wi)
        { 
            // Set WI on Thread.CurrentThread.ExecutionContext.SecurityContext
            SecurityContext sc = SecurityContext.GetCurrentSecurityContextNoCreate(); 
            if (wi != null && sc == null) { 
                // create a new security context on the thread
                sc = new SecurityContext(); 
                Thread.CurrentThread.ExecutionContext.SecurityContext = sc;
            }

            if (sc != null) // null-check needed here since we will not create an sc if wi is null 
            {
                sc.WindowsIdentity = wi; 
            } 
        }
 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static WindowsIdentity GetCurrentInternal (TokenAccessLevels desiredAccess, bool threadOnly) { 
            WindowsIdentity wi = null;
            if (!RunningOnWin2K) { 
                if (!threadOnly) { 
                    // Force create an empty WI
                    wi = new WindowsIdentity(); 
                    wi.m_name = String.Empty;
                }
                return wi; // We only do this for backward compatibility. Ideally, we would like to
                           // throw a NotSupportedException on Win9X platforms. 
            }
 
            int hr = 0; 
            bool isImpersonating;
            SafeTokenHandle safeTokenHandle = GetCurrentToken(desiredAccess, threadOnly, out isImpersonating, out hr); 
            if (safeTokenHandle == null || safeTokenHandle.IsInvalid) {
                // either we wanted only ThreadToken - return null
                if (threadOnly && !isImpersonating)
                    return wi; 
                // or there was an error
                throw new SecurityException(Win32Native.GetMessage(hr)); 
            } 
            wi = new WindowsIdentity();
            wi.m_safeTokenHandle = safeTokenHandle; 
            return wi;
        }

        // 
        // private.
        // 
 
        private static int s_runningOnWin2K = -1;
        internal static bool RunningOnWin2K { 
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get {
                if (s_runningOnWin2K == -1) {
                    Win32Native.OSVERSIONINFO osvi = new Win32Native.OSVERSIONINFO(); 
                    bool r = Win32Native.GetVersionEx(osvi);
                    s_runningOnWin2K = (r && (osvi.PlatformId == Win32Native.VER_PLATFORM_WIN32_NT) && (osvi.MajorVersion >= 5)) ? 1 : 0; 
                } 
                return s_runningOnWin2K == 1;
            } 
        }

        private static int GetHRForWin32Error (int dwLastError) {
            if ((dwLastError & 0x80000000) == 0x80000000) 
                return dwLastError;
            else 
                return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000); 
        }
 
        private static Exception GetExceptionFromNtStatus (int status) {
            if ((uint) status == Win32Native.STATUS_ACCESS_DENIED)
                return new UnauthorizedAccessException();
 
            if ((uint) status == Win32Native.STATUS_INSUFFICIENT_RESOURCES || (uint) status == Win32Native.STATUS_NO_MEMORY)
                return new OutOfMemoryException(); 
 
            int win32ErrorCode = Win32Native.LsaNtStatusToWinError(status);
            return new SecurityException(Win32Native.GetMessage(win32ErrorCode)); 
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        private static SafeTokenHandle GetCurrentToken(TokenAccessLevels desiredAccess, bool threadOnly, out bool isImpersonating, out int hr) {
            BCLDebug.Assert(RunningOnWin2K, "Don't call WindowsIdentity.GetCurrentToken on Win9X"); 
 
            isImpersonating = true;
            SafeTokenHandle safeTokenHandle = GetCurrentThreadToken(desiredAccess, out hr); 
            if (safeTokenHandle == null && hr == GetHRForWin32Error(Win32Native.ERROR_NO_TOKEN)) {
                // No impersonation
                isImpersonating = false;
                if (!threadOnly) 
                    safeTokenHandle = GetCurrentProcessToken(desiredAccess, out hr);
            } 
            return safeTokenHandle; 
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static SafeTokenHandle GetCurrentProcessToken (TokenAccessLevels desiredAccess, out int hr) {
            hr = 0; 
            SafeTokenHandle safeTokenHandle = SafeTokenHandle.InvalidHandle;
            if (!Win32Native.OpenProcessToken(Win32Native.GetCurrentProcess(), desiredAccess, ref safeTokenHandle)) 
                hr = GetHRForWin32Error(Marshal.GetLastWin32Error()); 
            return safeTokenHandle;
        } 

        internal static SafeTokenHandle GetCurrentThreadToken(TokenAccessLevels desiredAccess, out int hr) {
            SafeTokenHandle safeTokenHandle;
            hr = Win32.OpenThreadToken(desiredAccess, WinSecurityContext.Both, out safeTokenHandle); 
            return safeTokenHandle;
        } 
 
        private static Win32Native.LUID GetLogonAuthId (SafeTokenHandle safeTokenHandle) {
            uint cbSize = 0; 
            SafeLocalAllocHandle pStatistics = GetTokenInformation(safeTokenHandle, TokenInformationClass.TokenStatistics, out cbSize);
            Win32Native.TOKEN_STATISTICS statistics = (Win32Native.TOKEN_STATISTICS) Marshal.PtrToStructure(pStatistics.DangerousGetHandle(), typeof(Win32Native.TOKEN_STATISTICS));
            pStatistics.Dispose();
            return statistics.AuthenticationId; 
        }
 
        private static SafeLocalAllocHandle GetTokenInformation (SafeTokenHandle tokenHandle, TokenInformationClass tokenInformationClass, out uint dwLength) { 
            SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
            dwLength = (uint) Marshal.SizeOf(typeof(uint)); 
            bool result = Win32Native.GetTokenInformation(tokenHandle,
                                                          (uint) tokenInformationClass,
                                                          safeLocalAllocHandle,
                                                          0, 
                                                          out dwLength);
            int dwErrorCode = Marshal.GetLastWin32Error(); 
            switch (dwErrorCode) { 
            case Win32Native.ERROR_BAD_LENGTH:
                // special case for TokenSessionId. Falling through 
            case Win32Native.ERROR_INSUFFICIENT_BUFFER:
                // ptrLength is an [In] param to LocalAlloc
                IntPtr ptrLength = new IntPtr(dwLength);
                safeLocalAllocHandle = Win32Native.LocalAlloc(Win32Native.LMEM_FIXED, ptrLength); 
                if (safeLocalAllocHandle == null || safeLocalAllocHandle.IsInvalid)
                    throw new OutOfMemoryException(); 
 
                result = Win32Native.GetTokenInformation(tokenHandle,
                                                         (uint) tokenInformationClass, 
                                                         safeLocalAllocHandle,
                                                         dwLength,
                                                         out dwLength);
                if (!result) 
                    throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
                break; 
            case Win32Native.ERROR_INVALID_HANDLE: 
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidImpersonationToken"));
            default: 
                throw new SecurityException(Win32Native.GetMessage(dwErrorCode));
            }
            return safeLocalAllocHandle;
        } 

        private unsafe static SafeTokenHandle KerbS4ULogon (string upn) { 
            // source name 
            byte[] sourceName = new byte[] {(byte) 'C', (byte) 'L', (byte) 'R'}; // we set the source name to "CLR".
 
            // ptrLength is an [In] param to LocalAlloc
            IntPtr ptrLength = new IntPtr((uint) (sourceName.Length + 1));

            SafeLocalAllocHandle pSourceName = Win32Native.LocalAlloc(Win32Native.LPTR, ptrLength); 
            Marshal.Copy(sourceName, 0, pSourceName.DangerousGetHandle(), sourceName.Length);
            Win32Native.UNICODE_INTPTR_STRING Name = new Win32Native.UNICODE_INTPTR_STRING (sourceName.Length, sourceName.Length + 1, pSourceName.DangerousGetHandle()); 
 
            int status;
            SafeLsaLogonProcessHandle logonHandle = SafeLsaLogonProcessHandle.InvalidHandle; 

            Privilege privilege = null;

            RuntimeHelpers.PrepareConstrainedRegions(); 
            // Try to get an impersonation token.
            try { 
                // Try to enable the TCB privilege if possible 
                try {
                    privilege = new Privilege("SeTcbPrivilege"); 
                    privilege.Enable();
                } catch (PrivilegeNotHeldException) {}

                IntPtr dummy = IntPtr.Zero; 
                status = Win32Native.LsaRegisterLogonProcess(ref Name, ref logonHandle, ref dummy);
                if (Win32Native.ERROR_ACCESS_DENIED == Win32Native.LsaNtStatusToWinError(status)) { 
                    // We don't have the Tcb privilege. The best we can hope for is to get an Identification token. 
                    status = Win32Native.LsaConnectUntrusted(ref logonHandle);
                } 
            }
            catch {
                // protect against exception filter-based luring attacks
                if (privilege != null) 
                    privilege.Revert();
                throw; 
            } 
            finally {
                if (privilege != null) 
                    privilege.Revert();
            }
            if (status < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(status); 

            // package name ("Kerberos") 
            byte[] arrayPackageName = new byte[Win32Native.MICROSOFT_KERBEROS_NAME.Length + 1]; 
            Encoding.ASCII.GetBytes(Win32Native.MICROSOFT_KERBEROS_NAME, 0, Win32Native.MICROSOFT_KERBEROS_NAME.Length, arrayPackageName, 0);
 
            // ptrLength is an [In] param to LocalAlloc
            ptrLength = new IntPtr((uint) arrayPackageName.Length);
            SafeLocalAllocHandle pPackageName = Win32Native.LocalAlloc(Win32Native.LMEM_FIXED, ptrLength);
            if (pPackageName == null || pPackageName.IsInvalid) 
                throw new OutOfMemoryException();
            Marshal.Copy(arrayPackageName, 0, pPackageName.DangerousGetHandle(), arrayPackageName.Length); 
            Win32Native.UNICODE_INTPTR_STRING PackageName = new Win32Native.UNICODE_INTPTR_STRING (Win32Native.MICROSOFT_KERBEROS_NAME.Length, Win32Native.MICROSOFT_KERBEROS_NAME.Length + 1, pPackageName.DangerousGetHandle()); 

            uint packageId = 0; 
            status = Win32Native.LsaLookupAuthenticationPackage(logonHandle, ref PackageName, ref packageId);
            if (status < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(status);
 
            // source context
            Win32Native.TOKEN_SOURCE sourceContext = new Win32Native.TOKEN_SOURCE(); 
            if (!Win32Native.AllocateLocallyUniqueId(ref sourceContext.SourceIdentifier)) 
                throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
            sourceContext.Name = new char[8]; 
            sourceContext.Name[0] = 'C'; sourceContext.Name[1] = 'L'; sourceContext.Name[2] = 'R';

            uint profileSize = 0;
            SafeLsaReturnBufferHandle profile = SafeLsaReturnBufferHandle.InvalidHandle; 
            Win32Native.LUID logonId = new Win32Native.LUID();
            Win32Native.QUOTA_LIMITS quotas = new Win32Native.QUOTA_LIMITS(); 
            int subStatus = 0; 

            SafeTokenHandle safeTokenHandle = SafeTokenHandle.InvalidHandle; 

            // logon info
            int logonInfoSize = Marshal.SizeOf(typeof(Win32Native.KERB_S4U_LOGON)) + 2 * (upn.Length + 1);
            byte[] logonInfo = new byte[logonInfoSize]; 
            // We need to protect the byte array so we can pack the KERB_S4U_LOGON safely since
            // LsaLogonUser expects the structure to be serialized. 
            fixed (byte* ptr = logonInfo) { 
                byte[] arrayUpnName = new byte[2 * (upn.Length + 1)];
                Encoding.Unicode.GetBytes(upn, 0, upn.Length, arrayUpnName, 0); 
                Buffer.BlockCopy(arrayUpnName, 0, logonInfo, Marshal.SizeOf(typeof(Win32Native.KERB_S4U_LOGON)), arrayUpnName.Length);
                Win32Native.KERB_S4U_LOGON * pLogonInfo = (Win32Native.KERB_S4U_LOGON *) ptr;
                pLogonInfo->MessageType = (uint) KerbLogonSubmitType.KerbS4ULogon;
                pLogonInfo->Flags = 0; 
                pLogonInfo->ClientUpn.Length = (ushort) (2 * upn.Length);
                pLogonInfo->ClientUpn.MaxLength = (ushort) (2 * (upn.Length + 1)); 
                pLogonInfo->ClientUpn.Buffer = new IntPtr((byte*) (pLogonInfo + 1)); 
                // logon user
                status = Win32Native.LsaLogonUser(logonHandle, 
                                                  ref Name,
                                                  (uint) SecurityLogonType.Network,
                                                  packageId,
                                                  new IntPtr(ptr), 
                                                  (uint) logonInfo.Length,
                                                  IntPtr.Zero, 
                                                  ref sourceContext, 
                                                  ref profile,
                                                  ref profileSize, 
                                                  ref logonId,
                                                  ref safeTokenHandle,
                                                  ref quotas,
                                                  ref subStatus); 
            }
 
            // If both status and substatus are < 0, substatus is preferred. 
            if (status == Win32Native.STATUS_ACCOUNT_RESTRICTION && subStatus < 0)
                status = subStatus; 
            if (status < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(status);
            if (subStatus < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(subStatus); 

            profile.Dispose(); 
            pSourceName.Dispose(); 
            pPackageName.Dispose();
            logonHandle.Dispose(); 

            return safeTokenHandle;
        }
    } 

    [Serializable] 
    internal enum KerbLogonSubmitType : int { 
        KerbInteractiveLogon = 2,
        KerbSmartCardLogon = 6, 
        KerbWorkstationUnlockLogon = 7,
        KerbSmartCardUnlockLogon = 8,
        KerbProxyLogon = 9,
        KerbTicketLogon = 10, 
        KerbTicketUnlockLogon = 11,
        KerbS4ULogon = 12 
    } 

    [Serializable] 
    internal enum SecurityLogonType : int {
        Interactive = 2,
        Network,
        Batch, 
        Service,
        Proxy, 
        Unlock 
    }
 
    [Serializable]
    internal enum TokenType : int {
        TokenPrimary = 1,
        TokenImpersonation 
    }
 
    [Serializable] 
    internal enum TokenInformationClass : int {
        TokenUser = 1, 
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup, 
        TokenDefaultDacl,
        TokenSource, 
        TokenType, 
        TokenImpersonationLevel,
        TokenStatistics, 
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference, 
        TokenSandBoxInert
    } 
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// WindowsIdentity.cs 
//
// Representation of a process/thread token. 
//

namespace System.Security.Principal
{ 
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles; 
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices; 
    using System.Runtime.Serialization;
    using System.Security.AccessControl;
    using System.Security.Permissions;
    using System.Text; 
    using System.Threading;
    using System.Runtime.Versioning; 
 
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)] 
    public enum WindowsAccountType {
        Normal      = 0,
        Guest       = 1,
        System      = 2, 
        Anonymous   = 3
    } 
 
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)] 
    public enum TokenImpersonationLevel {
        None            = 0,
        Anonymous       = 1,
        Identification  = 2, 
        Impersonation   = 3,
        Delegation      = 4 
    } 

    [Serializable, Flags] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum TokenAccessLevels {
        AssignPrimary       = 0x00000001,
        Duplicate           = 0x00000002, 
        Impersonate         = 0x00000004,
        Query               = 0x00000008, 
        QuerySource         = 0x00000010, 
        AdjustPrivileges    = 0x00000020,
        AdjustGroups        = 0x00000040, 
        AdjustDefault       = 0x00000080,
        AdjustSessionId     = 0x00000100,

        Read                = 0x00020000 | Query, 

        Write               = 0x00020000 | AdjustPrivileges | AdjustGroups | AdjustDefault, 
 
        AllAccess           = 0x000F0000       |
                              AssignPrimary    | 
                              Duplicate        |
                              Impersonate      |
                              Query            |
                              QuerySource      | 
                              AdjustPrivileges |
                              AdjustGroups     | 
                              AdjustDefault    | 
                              AdjustSessionId,
 
        MaximumAllowed      = 0x02000000
    }

    // Keep in sync with vm\comprincipal.h 
    internal enum WinSecurityContext {
        Thread = 1, // OpenAsSelf = false 
        Process = 2, // OpenAsSelf = true 
        Both = 3 // OpenAsSelf = true, then OpenAsSelf = false
    } 

    [Serializable()]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class WindowsIdentity : IIdentity, ISerializable, IDeserializationCallback, IDisposable { 
        private string m_name = null;
        private SecurityIdentifier m_owner = null; 
        private SecurityIdentifier m_user = null; 
        private object m_groups = null;
        private SafeTokenHandle m_safeTokenHandle = SafeTokenHandle.InvalidHandle; 
        private string m_authType = null;
        private int m_isAuthenticated = -1;

        // 
        // Constructors.
        // 
 
        private WindowsIdentity () {}
        internal WindowsIdentity (SafeTokenHandle safeTokenHandle) : this (safeTokenHandle.DangerousGetHandle()) { 
            GC.KeepAlive(safeTokenHandle);
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)] 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public WindowsIdentity (IntPtr userToken) : this (userToken, null, -1) {} 
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public WindowsIdentity (IntPtr userToken, string type) : this (userToken, type, -1) {}

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public WindowsIdentity (IntPtr userToken, string type, WindowsAccountType acctType) : this (userToken, type, -1) {}
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)] 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public WindowsIdentity (IntPtr userToken, string type, WindowsAccountType acctType, bool isAuthenticated) 
            : this (userToken, type, isAuthenticated ? 1 : 0) {}

        private WindowsIdentity (IntPtr userToken, string authType, int isAuthenticated) {
            CreateFromToken(userToken); 
            m_authType = authType;
            m_isAuthenticated = isAuthenticated; 
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        private void CreateFromToken (IntPtr userToken) {
            if (userToken == IntPtr.Zero)
                throw new ArgumentException(Environment.GetResourceString("Argument_TokenZero")); 

            // Find out if the specified token is a valid. 
            uint dwLength = (uint) Marshal.SizeOf(typeof(uint)); 
            bool result = Win32Native.GetTokenInformation(userToken, (uint) TokenInformationClass.TokenType,
                                                          SafeLocalAllocHandle.InvalidHandle, 0, out dwLength); 
            if (Marshal.GetLastWin32Error() == Win32Native.ERROR_INVALID_HANDLE)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidImpersonationToken"));

            if (!Win32Native.DuplicateHandle(Win32Native.GetCurrentProcess(), 
                                             userToken,
                                             Win32Native.GetCurrentProcess(), 
                                             ref m_safeTokenHandle, 
                                             0,
                                             true, 
                                             Win32Native.DUPLICATE_SAME_ACCESS))
                throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
        }
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public WindowsIdentity (string sUserPrincipalName) : this (sUserPrincipalName, null) {} 
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public WindowsIdentity (string sUserPrincipalName, string type) { 
            m_safeTokenHandle = KerbS4ULogon(sUserPrincipalName);
        }

        // 
        // We cannot make sure the token will stay alive
        // until it is being deserialized in another AppDomain. We do not have a way to capture 
        // the state of a token (just a pointer to kernel memory) and re-construct it later 
        // and even if we did (via calling NtQueryInformationToken and relying on the token undocumented
        // format), constructing a token requires TCB privilege. We need to address the "serializable" 
        // nature of WindowsIdentity since it is not obvious that can be achieved at all.
        //

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)] 
        public WindowsIdentity (SerializationInfo info, StreamingContext context) : this(info)
        { 
        } 

        // This is a copy of the serialization constructor above but without the 
        // security demand that's slow and breaks partial trust scenarios
        // without an expensive assert in place in the remoting code. Instead we
        // special case this class and call the private constructor directly
        // (changing the demand above is considered a breaking change, even 
        // though nobody else should have been using a serialization constructor
        // directly). 
        private WindowsIdentity(SerializationInfo info) { 
            IntPtr userToken = (IntPtr) info.GetValue("m_userToken", typeof(IntPtr));
            if (userToken != IntPtr.Zero) 
                CreateFromToken(userToken);
        }

        /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) { 
            info.AddValue("m_userToken", m_safeTokenHandle.DangerousGetHandle()); 
        }
 
        /// <internalonly/>
        void IDeserializationCallback.OnDeserialization (Object sender) {}

        // 
        // Factory methods.
        // 
 

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)] 
        public static WindowsIdentity GetCurrent () {
           return GetCurrentInternal(TokenAccessLevels.MaximumAllowed, false);
        }
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public static WindowsIdentity GetCurrent (bool ifImpersonating) { 
           return GetCurrentInternal(TokenAccessLevels.MaximumAllowed, ifImpersonating); 
        }
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public static WindowsIdentity GetCurrent (TokenAccessLevels desiredAccess) {
           return GetCurrentInternal(desiredAccess, false);
        } 

        // GetAnonymous() is used heavily in ASP.NET requests as a dummy identity to indicate 
        // the request is anonymous. It does not represent a real process or thread token so 
        // it cannot impersonate or do anything useful. Note this identity does not represent the
        // usual concept of an anonymous token, and the name is simply misleading but we cannot change it now. 

        public static WindowsIdentity GetAnonymous () {
            return new WindowsIdentity();
        } 

        // 
        // Properties. 
        //
 
        public string AuthenticationType {
            get {
                // If this is an anonymous identity, return an empty string
                if (m_safeTokenHandle.IsInvalid) 
                    return String.Empty;
 
                if (m_authType == null) { 
                    Win32Native.LUID authId = GetLogonAuthId(m_safeTokenHandle);
                    if (authId.LowPart == Win32Native.ANONYMOUS_LOGON_LUID) 
                        return String.Empty; // no authentication, just return an empty string

                    SafeLsaReturnBufferHandle ppLogonSessionData = SafeLsaReturnBufferHandle.InvalidHandle;
                    int status = Win32Native.LsaGetLogonSessionData(ref authId, ref ppLogonSessionData); 
                    if (status < 0) // non-negative numbers indicate success
                        throw GetExceptionFromNtStatus(status); 
 
                    Win32Native.SECURITY_LOGON_SESSION_DATA logonSessionData = (Win32Native.SECURITY_LOGON_SESSION_DATA) Marshal.PtrToStructure(ppLogonSessionData.DangerousGetHandle(), typeof(Win32Native.SECURITY_LOGON_SESSION_DATA));
                    string authPackage = Marshal.PtrToStringUni(logonSessionData.AuthenticationPackage.Buffer); 
                    ppLogonSessionData.Dispose();
                    return authPackage;
                }
 
                return m_authType;
            } 
        } 

 
        [ComVisible(false)]
        public TokenImpersonationLevel ImpersonationLevel {
            get {
                // If this is an anonymous identity 
                if (m_safeTokenHandle.IsInvalid)
                    return TokenImpersonationLevel.Anonymous; 
 
                uint cbSize = 0;
                SafeLocalAllocHandle pTokenType = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenType, out cbSize); 
                int level = Marshal.ReadInt32(pTokenType.DangerousGetHandle());
                if (level == (int) TokenType.TokenPrimary)
                    return TokenImpersonationLevel.None; // primary token
 
                // This is an impersonation token, get the impersonation level
                SafeLocalAllocHandle pImpersonationLevel = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenImpersonationLevel, out cbSize); 
                level = Marshal.ReadInt32(pImpersonationLevel.DangerousGetHandle()); 

                pTokenType.Dispose(); 
                pImpersonationLevel.Dispose();

                return (TokenImpersonationLevel) level + 1;
            } 
        }
 
        public virtual bool IsAuthenticated { 
            get {
                // make this a no-op on Win98 so calling code does not have to special case down-level platforms. 
                if (!RunningOnWin2K)
                    return false;

                if (m_isAuthenticated == -1) { 
                    // There is a known bug where this approach will not work correctly for domain guests (will return false
                    // instead of true). But this is a corner-case that is not very interesting. 
                    WindowsPrincipal wp = new WindowsPrincipal(this); 
                    SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                                    new int[] {Win32Native.SECURITY_AUTHENTICATED_USER_RID}); 
                    m_isAuthenticated = wp.IsInRole(sid) ? 1 : 0;
                }

                return m_isAuthenticated == 1; 
            }
        } 
 
        //
        // IsGuest, IsSystem and IsAnonymous are maintained for compatibility reasons. It is always 
        // possible to extract this same information from the User SID property and the new
        // (and more general) methods defined in the SID class (IsWellKnown, etc...).
        //
 
        public virtual bool IsGuest {
            get { 
                // special case the anonymous identity. 
                if (m_safeTokenHandle.IsInvalid)
                    return false; 
                SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                                new int[] {Win32Native.SECURITY_BUILTIN_DOMAIN_RID, Win32Native.DOMAIN_USER_RID_GUEST});
                return (this.User == sid);
            } 
        }
 
        public virtual bool IsSystem { 
            get {
                // special case the anonymous identity. 
                if (m_safeTokenHandle.IsInvalid)
                    return false;
                SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                                new int[] {Win32Native.SECURITY_LOCAL_SYSTEM_RID}); 
                return (this.User == sid);
            } 
        } 

        public virtual bool IsAnonymous { 
            get {
                // special case the anonymous identity.
                if (m_safeTokenHandle.IsInvalid)
                    return true; 
                SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                                new int[] {Win32Native.SECURITY_ANONYMOUS_LOGON_RID}); 
                return (this.User == sid); 
            }
        } 


        public virtual string Name {
            get { 
                return GetName();
            } 
        } 

        [DynamicSecurityMethodAttribute()] 
        internal String GetName()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            // special case the anonymous identity. 
            if (m_safeTokenHandle.IsInvalid)
                return String.Empty; 
 
            if (m_name == null)
            { 
                // revert thread impersonation for the duration of the call to get the name.
                using (SafeImpersonate(SafeTokenHandle.InvalidHandle, null, ref stackMark))
                {
                    NTAccount ntAccount = this.User.Translate(typeof(NTAccount)) as NTAccount; 
                    m_name = ntAccount.ToString();
                } 
            } 

            return m_name; 
        }

        [ComVisible(false)]
        public SecurityIdentifier Owner { 
            get {
                // special case the anonymous identity. 
                if (m_safeTokenHandle.IsInvalid) 
                    return null;
 
                if (m_owner == null) {
                    uint cbSize = 0;
                    SafeLocalAllocHandle pOwnerToken = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenOwner, out cbSize);
                    m_owner = new SecurityIdentifier(Marshal.ReadIntPtr(pOwnerToken.DangerousGetHandle()), true ); 
                    pOwnerToken.Dispose();
                } 
 
                return m_owner;
            } 
        }

        [ComVisible(false)]
        public SecurityIdentifier User { 
            get {
                // special case the anonymous identity. 
                if (m_safeTokenHandle.IsInvalid) 
                    return null;
 
                if (m_user == null) {
                    uint cbSize = 0;
                    SafeLocalAllocHandle pUserToken = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenUser, out cbSize);
                    m_user = new SecurityIdentifier(Marshal.ReadIntPtr(pUserToken.DangerousGetHandle()), true ); 
                    pUserToken.Dispose();
                } 
 
                return m_user;
            } 
        }

        public IdentityReferenceCollection Groups {
            get { 
                // special case the anonymous identity.
                if (m_safeTokenHandle.IsInvalid) 
                    return null; 

                if (m_groups == null) { 
                    IdentityReferenceCollection groups = new IdentityReferenceCollection();
                    uint cbSize = 0;
                    using (SafeLocalAllocHandle pGroups = GetTokenInformation(m_safeTokenHandle, TokenInformationClass.TokenGroups, out cbSize)) {
                        int count = Marshal.ReadInt32(pGroups.DangerousGetHandle()); 
                        IntPtr pSidAndAttributes = new IntPtr((long) pGroups.DangerousGetHandle() + (long) Marshal.OffsetOf(typeof(Win32Native.TOKEN_GROUPS), "Groups"));
                        for (int i = 0; i < count; ++i) { 
                            Win32Native.SID_AND_ATTRIBUTES group = (Win32Native.SID_AND_ATTRIBUTES) Marshal.PtrToStructure(pSidAndAttributes, typeof(Win32Native.SID_AND_ATTRIBUTES)); 
                            // Ignore disabled, logon ID, and deny-only groups.
                            uint mask = Win32Native.SE_GROUP_ENABLED | Win32Native.SE_GROUP_LOGON_ID | Win32Native.SE_GROUP_USE_FOR_DENY_ONLY; 
                            if ((group.Attributes & mask) == Win32Native.SE_GROUP_ENABLED) {
                                groups.Add(new SecurityIdentifier(group.Sid, true ));
                            }
                            pSidAndAttributes = new IntPtr((long) pSidAndAttributes + (long) Marshal.SizeOf(typeof(Win32Native.SID_AND_ATTRIBUTES))); 
                        }
                    } 
                    Interlocked.CompareExchange(ref m_groups, groups, null); 
                }
 
                return m_groups as IdentityReferenceCollection;
            }
        }
 
        //
        // Note this property does not duplicate the token. This is also the same as V1/Everett behaviour. 
        // 

        public virtual IntPtr Token { 
            [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            get {
                return m_safeTokenHandle.DangerousGetHandle();
            } 
        }
 
        // 
        // Public methods.
        // 
        [DynamicSecurityMethodAttribute()]
        public virtual WindowsImpersonationContext Impersonate ()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
            return Impersonate(ref stackMark);
        } 
 
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        [DynamicSecurityMethodAttribute()] 
        public static WindowsImpersonationContext Impersonate (IntPtr userToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            if (userToken == IntPtr.Zero) 
                return SafeImpersonate(SafeTokenHandle.InvalidHandle, null, ref stackMark);
 
            WindowsIdentity wi = new WindowsIdentity(userToken); 
            return wi.Impersonate(ref stackMark);
        } 

        internal WindowsImpersonationContext Impersonate (ref StackCrawlMark stackMark) {
            // make this a no-op on Win98 so calling code does not have to special case down-level platforms.
            if (!RunningOnWin2K) 
                return new WindowsImpersonationContext(SafeTokenHandle.InvalidHandle, GetCurrentThreadWI(), false, null);
 
            if (m_safeTokenHandle.IsInvalid) 
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AnonymousCannotImpersonate"));
 
            return SafeImpersonate(m_safeTokenHandle, this, ref stackMark);
        }

        [ComVisible(false)] 
        protected virtual void Dispose(bool disposing) {
            if (disposing) { 
                if (m_safeTokenHandle != null && !m_safeTokenHandle.IsClosed) 
                    m_safeTokenHandle.Dispose();
            } 
            m_name = null;
            m_owner = null;
            m_user = null;
        } 

        [ComVisible(false)] 
        public void Dispose() { 
            Dispose(true);
        } 

        //
        // internal.
        // 

        internal SafeTokenHandle TokenHandle { 
            get { 
                return m_safeTokenHandle;
            } 
        }

        [ResourceExposure(ResourceScope.Process)]
        [ResourceConsumption(ResourceScope.Process)] 
        internal static WindowsImpersonationContext SafeImpersonate (SafeTokenHandle userToken, WindowsIdentity wi, ref StackCrawlMark stackMark)
        { 
            // make this a no-op on Win98 so calling code does not have to special case down-level platforms. 
            if (!RunningOnWin2K)
                return new WindowsImpersonationContext(SafeTokenHandle.InvalidHandle, GetCurrentThreadWI(), false, null); 

            bool isImpersonating;
            int hr = 0;
            SafeTokenHandle safeTokenHandle = GetCurrentToken(TokenAccessLevels.MaximumAllowed, false, out isImpersonating, out hr); 
            if (safeTokenHandle == null || safeTokenHandle.IsInvalid)
                throw new SecurityException(Win32Native.GetMessage(hr)); 
 
            // Set the SafeTokenHandle on the FSD:
            FrameSecurityDescriptor secObj = SecurityRuntime.GetSecurityObjectForFrame(ref stackMark, true); 
            if (secObj == null)
            {
                if (SecurityManager._IsSecurityOn())
                    // Security: REQ_SQ flag is missing. Bad compiler ? 
                    // This can happen when you create delegates over functions that need the REQ_SQ
                    throw new SecurityException(Environment.GetResourceString( "ExecutionEngine_MissingSecurityDescriptor" ) ); 
            } 

            WindowsImpersonationContext context = new WindowsImpersonationContext(safeTokenHandle, GetCurrentThreadWI(), isImpersonating, secObj); 

            if (userToken.IsInvalid) { // impersonating a zero token means clear the token on the thread
                hr = Win32.RevertToSelf();
                if (hr < 0) 
                    throw new SecurityException(Win32Native.GetMessage(hr));
                // update identity on the thread 
                UpdateThreadWI(wi); 
                secObj.SetTokenHandles(safeTokenHandle, (wi == null?null:wi.TokenHandle));
            } else { 
                hr = Win32.RevertToSelf();
                if (hr < 0)
                    throw new SecurityException(Win32Native.GetMessage(hr));
                hr = Win32.ImpersonateLoggedOnUser(userToken); 
                if (hr < 0) {
                    context.Undo(); 
                    throw new SecurityException(Environment.GetResourceString("Argument_ImpersonateUser")); 
                }
                UpdateThreadWI(wi); 
                secObj.SetTokenHandles(safeTokenHandle, (wi == null?null:wi.TokenHandle));
            }

            return context; 
        }
 
        internal static WindowsIdentity GetCurrentThreadWI() 
        {
            return SecurityContext.GetCurrentWI(Thread.CurrentThread.GetExecutionContextNoCreate()); 
        }

        internal static void UpdateThreadWI(WindowsIdentity wi)
        { 
            // Set WI on Thread.CurrentThread.ExecutionContext.SecurityContext
            SecurityContext sc = SecurityContext.GetCurrentSecurityContextNoCreate(); 
            if (wi != null && sc == null) { 
                // create a new security context on the thread
                sc = new SecurityContext(); 
                Thread.CurrentThread.ExecutionContext.SecurityContext = sc;
            }

            if (sc != null) // null-check needed here since we will not create an sc if wi is null 
            {
                sc.WindowsIdentity = wi; 
            } 
        }
 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static WindowsIdentity GetCurrentInternal (TokenAccessLevels desiredAccess, bool threadOnly) { 
            WindowsIdentity wi = null;
            if (!RunningOnWin2K) { 
                if (!threadOnly) { 
                    // Force create an empty WI
                    wi = new WindowsIdentity(); 
                    wi.m_name = String.Empty;
                }
                return wi; // We only do this for backward compatibility. Ideally, we would like to
                           // throw a NotSupportedException on Win9X platforms. 
            }
 
            int hr = 0; 
            bool isImpersonating;
            SafeTokenHandle safeTokenHandle = GetCurrentToken(desiredAccess, threadOnly, out isImpersonating, out hr); 
            if (safeTokenHandle == null || safeTokenHandle.IsInvalid) {
                // either we wanted only ThreadToken - return null
                if (threadOnly && !isImpersonating)
                    return wi; 
                // or there was an error
                throw new SecurityException(Win32Native.GetMessage(hr)); 
            } 
            wi = new WindowsIdentity();
            wi.m_safeTokenHandle = safeTokenHandle; 
            return wi;
        }

        // 
        // private.
        // 
 
        private static int s_runningOnWin2K = -1;
        internal static bool RunningOnWin2K { 
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get {
                if (s_runningOnWin2K == -1) {
                    Win32Native.OSVERSIONINFO osvi = new Win32Native.OSVERSIONINFO(); 
                    bool r = Win32Native.GetVersionEx(osvi);
                    s_runningOnWin2K = (r && (osvi.PlatformId == Win32Native.VER_PLATFORM_WIN32_NT) && (osvi.MajorVersion >= 5)) ? 1 : 0; 
                } 
                return s_runningOnWin2K == 1;
            } 
        }

        private static int GetHRForWin32Error (int dwLastError) {
            if ((dwLastError & 0x80000000) == 0x80000000) 
                return dwLastError;
            else 
                return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000); 
        }
 
        private static Exception GetExceptionFromNtStatus (int status) {
            if ((uint) status == Win32Native.STATUS_ACCESS_DENIED)
                return new UnauthorizedAccessException();
 
            if ((uint) status == Win32Native.STATUS_INSUFFICIENT_RESOURCES || (uint) status == Win32Native.STATUS_NO_MEMORY)
                return new OutOfMemoryException(); 
 
            int win32ErrorCode = Win32Native.LsaNtStatusToWinError(status);
            return new SecurityException(Win32Native.GetMessage(win32ErrorCode)); 
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        private static SafeTokenHandle GetCurrentToken(TokenAccessLevels desiredAccess, bool threadOnly, out bool isImpersonating, out int hr) {
            BCLDebug.Assert(RunningOnWin2K, "Don't call WindowsIdentity.GetCurrentToken on Win9X"); 
 
            isImpersonating = true;
            SafeTokenHandle safeTokenHandle = GetCurrentThreadToken(desiredAccess, out hr); 
            if (safeTokenHandle == null && hr == GetHRForWin32Error(Win32Native.ERROR_NO_TOKEN)) {
                // No impersonation
                isImpersonating = false;
                if (!threadOnly) 
                    safeTokenHandle = GetCurrentProcessToken(desiredAccess, out hr);
            } 
            return safeTokenHandle; 
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static SafeTokenHandle GetCurrentProcessToken (TokenAccessLevels desiredAccess, out int hr) {
            hr = 0; 
            SafeTokenHandle safeTokenHandle = SafeTokenHandle.InvalidHandle;
            if (!Win32Native.OpenProcessToken(Win32Native.GetCurrentProcess(), desiredAccess, ref safeTokenHandle)) 
                hr = GetHRForWin32Error(Marshal.GetLastWin32Error()); 
            return safeTokenHandle;
        } 

        internal static SafeTokenHandle GetCurrentThreadToken(TokenAccessLevels desiredAccess, out int hr) {
            SafeTokenHandle safeTokenHandle;
            hr = Win32.OpenThreadToken(desiredAccess, WinSecurityContext.Both, out safeTokenHandle); 
            return safeTokenHandle;
        } 
 
        private static Win32Native.LUID GetLogonAuthId (SafeTokenHandle safeTokenHandle) {
            uint cbSize = 0; 
            SafeLocalAllocHandle pStatistics = GetTokenInformation(safeTokenHandle, TokenInformationClass.TokenStatistics, out cbSize);
            Win32Native.TOKEN_STATISTICS statistics = (Win32Native.TOKEN_STATISTICS) Marshal.PtrToStructure(pStatistics.DangerousGetHandle(), typeof(Win32Native.TOKEN_STATISTICS));
            pStatistics.Dispose();
            return statistics.AuthenticationId; 
        }
 
        private static SafeLocalAllocHandle GetTokenInformation (SafeTokenHandle tokenHandle, TokenInformationClass tokenInformationClass, out uint dwLength) { 
            SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
            dwLength = (uint) Marshal.SizeOf(typeof(uint)); 
            bool result = Win32Native.GetTokenInformation(tokenHandle,
                                                          (uint) tokenInformationClass,
                                                          safeLocalAllocHandle,
                                                          0, 
                                                          out dwLength);
            int dwErrorCode = Marshal.GetLastWin32Error(); 
            switch (dwErrorCode) { 
            case Win32Native.ERROR_BAD_LENGTH:
                // special case for TokenSessionId. Falling through 
            case Win32Native.ERROR_INSUFFICIENT_BUFFER:
                // ptrLength is an [In] param to LocalAlloc
                IntPtr ptrLength = new IntPtr(dwLength);
                safeLocalAllocHandle = Win32Native.LocalAlloc(Win32Native.LMEM_FIXED, ptrLength); 
                if (safeLocalAllocHandle == null || safeLocalAllocHandle.IsInvalid)
                    throw new OutOfMemoryException(); 
 
                result = Win32Native.GetTokenInformation(tokenHandle,
                                                         (uint) tokenInformationClass, 
                                                         safeLocalAllocHandle,
                                                         dwLength,
                                                         out dwLength);
                if (!result) 
                    throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
                break; 
            case Win32Native.ERROR_INVALID_HANDLE: 
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidImpersonationToken"));
            default: 
                throw new SecurityException(Win32Native.GetMessage(dwErrorCode));
            }
            return safeLocalAllocHandle;
        } 

        private unsafe static SafeTokenHandle KerbS4ULogon (string upn) { 
            // source name 
            byte[] sourceName = new byte[] {(byte) 'C', (byte) 'L', (byte) 'R'}; // we set the source name to "CLR".
 
            // ptrLength is an [In] param to LocalAlloc
            IntPtr ptrLength = new IntPtr((uint) (sourceName.Length + 1));

            SafeLocalAllocHandle pSourceName = Win32Native.LocalAlloc(Win32Native.LPTR, ptrLength); 
            Marshal.Copy(sourceName, 0, pSourceName.DangerousGetHandle(), sourceName.Length);
            Win32Native.UNICODE_INTPTR_STRING Name = new Win32Native.UNICODE_INTPTR_STRING (sourceName.Length, sourceName.Length + 1, pSourceName.DangerousGetHandle()); 
 
            int status;
            SafeLsaLogonProcessHandle logonHandle = SafeLsaLogonProcessHandle.InvalidHandle; 

            Privilege privilege = null;

            RuntimeHelpers.PrepareConstrainedRegions(); 
            // Try to get an impersonation token.
            try { 
                // Try to enable the TCB privilege if possible 
                try {
                    privilege = new Privilege("SeTcbPrivilege"); 
                    privilege.Enable();
                } catch (PrivilegeNotHeldException) {}

                IntPtr dummy = IntPtr.Zero; 
                status = Win32Native.LsaRegisterLogonProcess(ref Name, ref logonHandle, ref dummy);
                if (Win32Native.ERROR_ACCESS_DENIED == Win32Native.LsaNtStatusToWinError(status)) { 
                    // We don't have the Tcb privilege. The best we can hope for is to get an Identification token. 
                    status = Win32Native.LsaConnectUntrusted(ref logonHandle);
                } 
            }
            catch {
                // protect against exception filter-based luring attacks
                if (privilege != null) 
                    privilege.Revert();
                throw; 
            } 
            finally {
                if (privilege != null) 
                    privilege.Revert();
            }
            if (status < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(status); 

            // package name ("Kerberos") 
            byte[] arrayPackageName = new byte[Win32Native.MICROSOFT_KERBEROS_NAME.Length + 1]; 
            Encoding.ASCII.GetBytes(Win32Native.MICROSOFT_KERBEROS_NAME, 0, Win32Native.MICROSOFT_KERBEROS_NAME.Length, arrayPackageName, 0);
 
            // ptrLength is an [In] param to LocalAlloc
            ptrLength = new IntPtr((uint) arrayPackageName.Length);
            SafeLocalAllocHandle pPackageName = Win32Native.LocalAlloc(Win32Native.LMEM_FIXED, ptrLength);
            if (pPackageName == null || pPackageName.IsInvalid) 
                throw new OutOfMemoryException();
            Marshal.Copy(arrayPackageName, 0, pPackageName.DangerousGetHandle(), arrayPackageName.Length); 
            Win32Native.UNICODE_INTPTR_STRING PackageName = new Win32Native.UNICODE_INTPTR_STRING (Win32Native.MICROSOFT_KERBEROS_NAME.Length, Win32Native.MICROSOFT_KERBEROS_NAME.Length + 1, pPackageName.DangerousGetHandle()); 

            uint packageId = 0; 
            status = Win32Native.LsaLookupAuthenticationPackage(logonHandle, ref PackageName, ref packageId);
            if (status < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(status);
 
            // source context
            Win32Native.TOKEN_SOURCE sourceContext = new Win32Native.TOKEN_SOURCE(); 
            if (!Win32Native.AllocateLocallyUniqueId(ref sourceContext.SourceIdentifier)) 
                throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
            sourceContext.Name = new char[8]; 
            sourceContext.Name[0] = 'C'; sourceContext.Name[1] = 'L'; sourceContext.Name[2] = 'R';

            uint profileSize = 0;
            SafeLsaReturnBufferHandle profile = SafeLsaReturnBufferHandle.InvalidHandle; 
            Win32Native.LUID logonId = new Win32Native.LUID();
            Win32Native.QUOTA_LIMITS quotas = new Win32Native.QUOTA_LIMITS(); 
            int subStatus = 0; 

            SafeTokenHandle safeTokenHandle = SafeTokenHandle.InvalidHandle; 

            // logon info
            int logonInfoSize = Marshal.SizeOf(typeof(Win32Native.KERB_S4U_LOGON)) + 2 * (upn.Length + 1);
            byte[] logonInfo = new byte[logonInfoSize]; 
            // We need to protect the byte array so we can pack the KERB_S4U_LOGON safely since
            // LsaLogonUser expects the structure to be serialized. 
            fixed (byte* ptr = logonInfo) { 
                byte[] arrayUpnName = new byte[2 * (upn.Length + 1)];
                Encoding.Unicode.GetBytes(upn, 0, upn.Length, arrayUpnName, 0); 
                Buffer.BlockCopy(arrayUpnName, 0, logonInfo, Marshal.SizeOf(typeof(Win32Native.KERB_S4U_LOGON)), arrayUpnName.Length);
                Win32Native.KERB_S4U_LOGON * pLogonInfo = (Win32Native.KERB_S4U_LOGON *) ptr;
                pLogonInfo->MessageType = (uint) KerbLogonSubmitType.KerbS4ULogon;
                pLogonInfo->Flags = 0; 
                pLogonInfo->ClientUpn.Length = (ushort) (2 * upn.Length);
                pLogonInfo->ClientUpn.MaxLength = (ushort) (2 * (upn.Length + 1)); 
                pLogonInfo->ClientUpn.Buffer = new IntPtr((byte*) (pLogonInfo + 1)); 
                // logon user
                status = Win32Native.LsaLogonUser(logonHandle, 
                                                  ref Name,
                                                  (uint) SecurityLogonType.Network,
                                                  packageId,
                                                  new IntPtr(ptr), 
                                                  (uint) logonInfo.Length,
                                                  IntPtr.Zero, 
                                                  ref sourceContext, 
                                                  ref profile,
                                                  ref profileSize, 
                                                  ref logonId,
                                                  ref safeTokenHandle,
                                                  ref quotas,
                                                  ref subStatus); 
            }
 
            // If both status and substatus are < 0, substatus is preferred. 
            if (status == Win32Native.STATUS_ACCOUNT_RESTRICTION && subStatus < 0)
                status = subStatus; 
            if (status < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(status);
            if (subStatus < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(subStatus); 

            profile.Dispose(); 
            pSourceName.Dispose(); 
            pPackageName.Dispose();
            logonHandle.Dispose(); 

            return safeTokenHandle;
        }
    } 

    [Serializable] 
    internal enum KerbLogonSubmitType : int { 
        KerbInteractiveLogon = 2,
        KerbSmartCardLogon = 6, 
        KerbWorkstationUnlockLogon = 7,
        KerbSmartCardUnlockLogon = 8,
        KerbProxyLogon = 9,
        KerbTicketLogon = 10, 
        KerbTicketUnlockLogon = 11,
        KerbS4ULogon = 12 
    } 

    [Serializable] 
    internal enum SecurityLogonType : int {
        Interactive = 2,
        Network,
        Batch, 
        Service,
        Proxy, 
        Unlock 
    }
 
    [Serializable]
    internal enum TokenType : int {
        TokenPrimary = 1,
        TokenImpersonation 
    }
 
    [Serializable] 
    internal enum TokenInformationClass : int {
        TokenUser = 1, 
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup, 
        TokenDefaultDacl,
        TokenSource, 
        TokenType, 
        TokenImpersonationLevel,
        TokenStatistics, 
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference, 
        TokenSandBoxInert
    } 
} 
