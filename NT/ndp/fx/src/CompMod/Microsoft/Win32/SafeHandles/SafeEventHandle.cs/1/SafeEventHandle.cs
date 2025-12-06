// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SafeEventHandle 
**
** <EMAIL>Author: David Gutierrez ([....]) </EMAIL> 
**
** A wrapper for a win32 event handles
**
** Date:  July 8, 2002 
**
===========================================================*/ 
 
using System;
using System.Security; 
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32; 
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution; 
 
namespace Microsoft.Win32.SafeHandles {
    [HostProtectionAttribute(MayLeakOnAbort = true)] 
    [SuppressUnmanagedCodeSecurityAttribute]
    internal sealed class SafeEventHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Note that CreateEvent returns 0 on failure 

        internal SafeEventHandle() : base(true) {} 
 
        [DllImport(ExternDll.Kernel32, CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern SafeEventHandle CreateEvent(HandleRef lpEventAttributes, bool bManualReset, 
                                               bool bInitialState, string name);

        [DllImport(ExternDll.Kernel32, ExactSpelling=true, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static extern bool CloseHandle(IntPtr handle);
 
        override protected bool ReleaseHandle() 
        {
            return CloseHandle(handle); 
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
** Class:  SafeEventHandle 
**
** <EMAIL>Author: David Gutierrez ([....]) </EMAIL> 
**
** A wrapper for a win32 event handles
**
** Date:  July 8, 2002 
**
===========================================================*/ 
 
using System;
using System.Security; 
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32; 
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution; 
 
namespace Microsoft.Win32.SafeHandles {
    [HostProtectionAttribute(MayLeakOnAbort = true)] 
    [SuppressUnmanagedCodeSecurityAttribute]
    internal sealed class SafeEventHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Note that CreateEvent returns 0 on failure 

        internal SafeEventHandle() : base(true) {} 
 
        [DllImport(ExternDll.Kernel32, CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern SafeEventHandle CreateEvent(HandleRef lpEventAttributes, bool bManualReset, 
                                               bool bInitialState, string name);

        [DllImport(ExternDll.Kernel32, ExactSpelling=true, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static extern bool CloseHandle(IntPtr handle);
 
        override protected bool ReleaseHandle() 
        {
            return CloseHandle(handle); 
        }
    }
}
