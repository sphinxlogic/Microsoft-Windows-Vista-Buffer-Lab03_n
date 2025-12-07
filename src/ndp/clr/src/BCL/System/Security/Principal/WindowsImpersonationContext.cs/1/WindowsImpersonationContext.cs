// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// WindowsImpersonationContext.cs 
//
// Representation of an impersonation context. 
//

namespace System.Security.Principal
{ 
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles; 
    using System.Runtime.InteropServices; 
    using System.Security.Permissions;
    using System.Runtime.ConstrainedExecution; 

    [System.Runtime.InteropServices.ComVisible(true)]
    public class WindowsImpersonationContext : IDisposable {
        private SafeTokenHandle m_safeTokenHandle = SafeTokenHandle.InvalidHandle; 
        private WindowsIdentity m_wi;
        private FrameSecurityDescriptor m_fsd; 
 
        private WindowsImpersonationContext () {}
        internal WindowsImpersonationContext (SafeTokenHandle safeTokenHandle, WindowsIdentity wi, bool isImpersonating, FrameSecurityDescriptor fsd) { 
            // make this a no-op on Win98 so calling code does not have to special case down-level platforms.
            if (WindowsIdentity.RunningOnWin2K) {
                if (safeTokenHandle.IsInvalid)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidImpersonationToken")); 

                if (isImpersonating) { 
                    if (!Win32Native.DuplicateHandle(Win32Native.GetCurrentProcess(), 
                                                     safeTokenHandle,
                                                     Win32Native.GetCurrentProcess(), 
                                                     ref m_safeTokenHandle,
                                                     0,
                                                     true,
                                                     Win32Native.DUPLICATE_SAME_ACCESS)) 
                        throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
                    m_wi = wi; 
                } 
                m_fsd = fsd;
            } 
        }

        // Revert to previous impersonation (the only public method).
        public void Undo () { 
            // make this a no-op on Win98 so calling code does not have to special case down-level platforms.
            if (!WindowsIdentity.RunningOnWin2K) 
                return; 

            int hr = 0; 
            if (m_safeTokenHandle.IsInvalid) { // the thread was not initially impersonating
                hr = Win32.RevertToSelf();
                if (hr < 0)
                    throw new SecurityException(Win32Native.GetMessage(hr)); 
            } else {
                hr = Win32.RevertToSelf(); 
                if (hr < 0) 
                    throw new SecurityException(Win32Native.GetMessage(hr));
                hr = Win32.ImpersonateLoggedOnUser(m_safeTokenHandle); 
                if (hr < 0)
                    throw new SecurityException(Win32Native.GetMessage(hr));
            }
            WindowsIdentity.UpdateThreadWI(m_wi); 
            if (m_fsd != null)
                m_fsd.SetTokenHandles(null, null); 
        } 

        // Non-throwing version that does not new any exception objects. To be called when reliability matters 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal bool UndoNoThrow()
        {
            bool bRet = false; 
            try{
                // make this a no-op on Win98 so calling code does not have to special case down-level platforms. 
                if (!WindowsIdentity.RunningOnWin2K) 
                    return true;
 
                int hr = 0;
                if (m_safeTokenHandle.IsInvalid)
                { // the thread was not initially impersonating
                    hr = Win32.RevertToSelf(); 
                }
                else 
                { 
                    hr = Win32.RevertToSelf();
                    if (hr >= 0) 
                        hr = Win32.ImpersonateLoggedOnUser(m_safeTokenHandle);
                }
                bRet = (hr >= 0);
                if (m_fsd != null) 
                    m_fsd.SetTokenHandles(null,null);
            } 
            catch 
            {
                bRet = false; 
            }
            return bRet;
        }
 
        //
        // IDisposable interface. 
        // 

        [ComVisible(false)] 
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (m_safeTokenHandle != null && !m_safeTokenHandle.IsClosed) {
                    Undo(); 
                    m_safeTokenHandle.Dispose();
                } 
            } 
        }
 
        [ComVisible(false)]
        public void Dispose () {
            Dispose(true);
        } 
    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// WindowsImpersonationContext.cs 
//
// Representation of an impersonation context. 
//

namespace System.Security.Principal
{ 
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles; 
    using System.Runtime.InteropServices; 
    using System.Security.Permissions;
    using System.Runtime.ConstrainedExecution; 

    [System.Runtime.InteropServices.ComVisible(true)]
    public class WindowsImpersonationContext : IDisposable {
        private SafeTokenHandle m_safeTokenHandle = SafeTokenHandle.InvalidHandle; 
        private WindowsIdentity m_wi;
        private FrameSecurityDescriptor m_fsd; 
 
        private WindowsImpersonationContext () {}
        internal WindowsImpersonationContext (SafeTokenHandle safeTokenHandle, WindowsIdentity wi, bool isImpersonating, FrameSecurityDescriptor fsd) { 
            // make this a no-op on Win98 so calling code does not have to special case down-level platforms.
            if (WindowsIdentity.RunningOnWin2K) {
                if (safeTokenHandle.IsInvalid)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidImpersonationToken")); 

                if (isImpersonating) { 
                    if (!Win32Native.DuplicateHandle(Win32Native.GetCurrentProcess(), 
                                                     safeTokenHandle,
                                                     Win32Native.GetCurrentProcess(), 
                                                     ref m_safeTokenHandle,
                                                     0,
                                                     true,
                                                     Win32Native.DUPLICATE_SAME_ACCESS)) 
                        throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
                    m_wi = wi; 
                } 
                m_fsd = fsd;
            } 
        }

        // Revert to previous impersonation (the only public method).
        public void Undo () { 
            // make this a no-op on Win98 so calling code does not have to special case down-level platforms.
            if (!WindowsIdentity.RunningOnWin2K) 
                return; 

            int hr = 0; 
            if (m_safeTokenHandle.IsInvalid) { // the thread was not initially impersonating
                hr = Win32.RevertToSelf();
                if (hr < 0)
                    throw new SecurityException(Win32Native.GetMessage(hr)); 
            } else {
                hr = Win32.RevertToSelf(); 
                if (hr < 0) 
                    throw new SecurityException(Win32Native.GetMessage(hr));
                hr = Win32.ImpersonateLoggedOnUser(m_safeTokenHandle); 
                if (hr < 0)
                    throw new SecurityException(Win32Native.GetMessage(hr));
            }
            WindowsIdentity.UpdateThreadWI(m_wi); 
            if (m_fsd != null)
                m_fsd.SetTokenHandles(null, null); 
        } 

        // Non-throwing version that does not new any exception objects. To be called when reliability matters 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal bool UndoNoThrow()
        {
            bool bRet = false; 
            try{
                // make this a no-op on Win98 so calling code does not have to special case down-level platforms. 
                if (!WindowsIdentity.RunningOnWin2K) 
                    return true;
 
                int hr = 0;
                if (m_safeTokenHandle.IsInvalid)
                { // the thread was not initially impersonating
                    hr = Win32.RevertToSelf(); 
                }
                else 
                { 
                    hr = Win32.RevertToSelf();
                    if (hr >= 0) 
                        hr = Win32.ImpersonateLoggedOnUser(m_safeTokenHandle);
                }
                bRet = (hr >= 0);
                if (m_fsd != null) 
                    m_fsd.SetTokenHandles(null,null);
            } 
            catch 
            {
                bRet = false; 
            }
            return bRet;
        }
 
        //
        // IDisposable interface. 
        // 

        [ComVisible(false)] 
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (m_safeTokenHandle != null && !m_safeTokenHandle.IsClosed) {
                    Undo(); 
                    m_safeTokenHandle.Dispose();
                } 
            } 
        }
 
        [ComVisible(false)]
        public void Dispose () {
            Dispose(true);
        } 
    }
} 
