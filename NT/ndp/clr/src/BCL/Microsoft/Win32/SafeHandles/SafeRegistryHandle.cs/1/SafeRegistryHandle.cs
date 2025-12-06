// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SafeRegistryHandle 
**
** 
** A wrapper for registry handles
**
**
===========================================================*/ 

using System; 
using System.Security; 
using System.Security.Permissions;
using System.Runtime.InteropServices; 
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace Microsoft.Win32.SafeHandles { 

    internal sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid { 
 
        // Note: Officially -1 is the recommended invalid handle value for
        // registry keys, but we'll also get back 0 as an invalid handle from 
        // RegOpenKeyEx.

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeRegistryHandle() : base(true) {} 

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)] 
        internal SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) { 
            SetHandle(preexistingHandle);
        } 

        [DllImport(Win32Native.ADVAPI32),
         SuppressUnmanagedCodeSecurity,
         ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static extern int RegCloseKey(IntPtr hKey);
 
        override protected bool ReleaseHandle() 
        {
            // Returns a Win32 error code, 0 for success 
            int r = RegCloseKey(handle);
            return r == 0;
        }
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SafeRegistryHandle 
**
** 
** A wrapper for registry handles
**
**
===========================================================*/ 

using System; 
using System.Security; 
using System.Security.Permissions;
using System.Runtime.InteropServices; 
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace Microsoft.Win32.SafeHandles { 

    internal sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid { 
 
        // Note: Officially -1 is the recommended invalid handle value for
        // registry keys, but we'll also get back 0 as an invalid handle from 
        // RegOpenKeyEx.

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeRegistryHandle() : base(true) {} 

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)] 
        internal SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) { 
            SetHandle(preexistingHandle);
        } 

        [DllImport(Win32Native.ADVAPI32),
         SuppressUnmanagedCodeSecurity,
         ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static extern int RegCloseKey(IntPtr hKey);
 
        override protected bool ReleaseHandle() 
        {
            // Returns a Win32 error code, 0 for success 
            int r = RegCloseKey(handle);
            return r == 0;
        }
    } 
}
