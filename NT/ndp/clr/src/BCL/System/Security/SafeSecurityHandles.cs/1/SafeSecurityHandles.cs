namespace Microsoft.Win32.SafeHandles { 
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution; 
    using System.Security;
 
    internal sealed class SafeLocalAllocHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeLocalAllocHandle () : base(true) {}
 
        // 0 is an Invalid Handle
        internal SafeLocalAllocHandle (IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeLocalAllocHandle InvalidHandle { 
            get { return new SafeLocalAllocHandle(IntPtr.Zero); } 
        }
 
        override protected bool ReleaseHandle()
        {
            return Win32Native.LocalFree(handle) == IntPtr.Zero;
        } 
    }
 
    internal sealed class SafeLsaLogonProcessHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeLsaLogonProcessHandle() : base (true) {}
 
        // 0 is an Invalid Handle
        internal SafeLsaLogonProcessHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeLsaLogonProcessHandle InvalidHandle { 
            get { return new SafeLsaLogonProcessHandle(IntPtr.Zero); } 
        }
 
        override protected bool ReleaseHandle()
        {
            // LsaDeregisterLogonProcess returns an NTSTATUS
            return Win32Native.LsaDeregisterLogonProcess(handle) >= 0; 
        }
    } 
 
    internal sealed class SafeLsaMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeLsaMemoryHandle() : base(true) {} 

        // 0 is an Invalid Handle
        internal SafeLsaMemoryHandle(IntPtr handle) : base (true) {
            SetHandle(handle); 
        }
 
        internal static SafeLsaMemoryHandle InvalidHandle { 
            get { return new SafeLsaMemoryHandle( IntPtr.Zero ); }
        } 

        override protected bool ReleaseHandle() {
            return Win32Native.LsaFreeMemory(handle) == 0;
        } 
    }
 
    internal sealed class SafeLsaPolicyHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeLsaPolicyHandle() : base(true) {}
 
        // 0 is an Invalid Handle
        internal SafeLsaPolicyHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeLsaPolicyHandle InvalidHandle { 
            get { return new SafeLsaPolicyHandle( IntPtr.Zero ); } 
        }
 
        override protected bool ReleaseHandle() {
            return Win32Native.LsaClose(handle) == 0;
        }
    } 

    internal sealed class SafeLsaReturnBufferHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeLsaReturnBufferHandle() : base (true) {} 

        // 0 is an Invalid Handle 
        internal SafeLsaReturnBufferHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }
 
        internal static SafeLsaReturnBufferHandle InvalidHandle {
            get { return new SafeLsaReturnBufferHandle(IntPtr.Zero); } 
        } 

        override protected bool ReleaseHandle() 
        {
            // LsaFreeReturnBuffer returns an NTSTATUS
            return Win32Native.LsaFreeReturnBuffer(handle) >= 0;
        } 
    }
 
    internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeProcessHandle() : base (true) {}
 
        // 0 is an Invalid Handle
        internal SafeProcessHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeProcessHandle InvalidHandle { 
            get { return new SafeProcessHandle(IntPtr.Zero); } 
        }
 
        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        } 
    }
 
    internal sealed class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeThreadHandle() : base (true) {}
 
        // 0 is an Invalid Handle
        internal SafeThreadHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        override protected bool ReleaseHandle() 
        { 
            return Win32Native.CloseHandle(handle);
        } 
    }

    internal sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeTokenHandle() : base (true) {} 

        // 0 is an Invalid Handle 
        internal SafeTokenHandle(IntPtr handle) : base (true) { 
            SetHandle(handle);
        } 

        internal static SafeTokenHandle InvalidHandle {
            get { return new SafeTokenHandle(IntPtr.Zero); }
        } 

        override protected bool ReleaseHandle() 
        { 
            return Win32Native.CloseHandle(handle);
        } 
    }
}
namespace Microsoft.Win32.SafeHandles { 
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution; 
    using System.Security;
 
    internal sealed class SafeLocalAllocHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeLocalAllocHandle () : base(true) {}
 
        // 0 is an Invalid Handle
        internal SafeLocalAllocHandle (IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeLocalAllocHandle InvalidHandle { 
            get { return new SafeLocalAllocHandle(IntPtr.Zero); } 
        }
 
        override protected bool ReleaseHandle()
        {
            return Win32Native.LocalFree(handle) == IntPtr.Zero;
        } 
    }
 
    internal sealed class SafeLsaLogonProcessHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeLsaLogonProcessHandle() : base (true) {}
 
        // 0 is an Invalid Handle
        internal SafeLsaLogonProcessHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeLsaLogonProcessHandle InvalidHandle { 
            get { return new SafeLsaLogonProcessHandle(IntPtr.Zero); } 
        }
 
        override protected bool ReleaseHandle()
        {
            // LsaDeregisterLogonProcess returns an NTSTATUS
            return Win32Native.LsaDeregisterLogonProcess(handle) >= 0; 
        }
    } 
 
    internal sealed class SafeLsaMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeLsaMemoryHandle() : base(true) {} 

        // 0 is an Invalid Handle
        internal SafeLsaMemoryHandle(IntPtr handle) : base (true) {
            SetHandle(handle); 
        }
 
        internal static SafeLsaMemoryHandle InvalidHandle { 
            get { return new SafeLsaMemoryHandle( IntPtr.Zero ); }
        } 

        override protected bool ReleaseHandle() {
            return Win32Native.LsaFreeMemory(handle) == 0;
        } 
    }
 
    internal sealed class SafeLsaPolicyHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeLsaPolicyHandle() : base(true) {}
 
        // 0 is an Invalid Handle
        internal SafeLsaPolicyHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeLsaPolicyHandle InvalidHandle { 
            get { return new SafeLsaPolicyHandle( IntPtr.Zero ); } 
        }
 
        override protected bool ReleaseHandle() {
            return Win32Native.LsaClose(handle) == 0;
        }
    } 

    internal sealed class SafeLsaReturnBufferHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeLsaReturnBufferHandle() : base (true) {} 

        // 0 is an Invalid Handle 
        internal SafeLsaReturnBufferHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }
 
        internal static SafeLsaReturnBufferHandle InvalidHandle {
            get { return new SafeLsaReturnBufferHandle(IntPtr.Zero); } 
        } 

        override protected bool ReleaseHandle() 
        {
            // LsaFreeReturnBuffer returns an NTSTATUS
            return Win32Native.LsaFreeReturnBuffer(handle) >= 0;
        } 
    }
 
    internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeProcessHandle() : base (true) {}
 
        // 0 is an Invalid Handle
        internal SafeProcessHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeProcessHandle InvalidHandle { 
            get { return new SafeProcessHandle(IntPtr.Zero); } 
        }
 
        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        } 
    }
 
    internal sealed class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        private SafeThreadHandle() : base (true) {}
 
        // 0 is an Invalid Handle
        internal SafeThreadHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        override protected bool ReleaseHandle() 
        { 
            return Win32Native.CloseHandle(handle);
        } 
    }

    internal sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeTokenHandle() : base (true) {} 

        // 0 is an Invalid Handle 
        internal SafeTokenHandle(IntPtr handle) : base (true) { 
            SetHandle(handle);
        } 

        internal static SafeTokenHandle InvalidHandle {
            get { return new SafeTokenHandle(IntPtr.Zero); }
        } 

        override protected bool ReleaseHandle() 
        { 
            return Win32Native.CloseHandle(handle);
        } 
    }
}
