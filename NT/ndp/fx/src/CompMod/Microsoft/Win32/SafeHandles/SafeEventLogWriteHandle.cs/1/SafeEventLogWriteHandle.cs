// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SafeEventLogWriteHandle 
**
** <EMAIL>Author: David Gutierrez ([....]) </EMAIL> 
**
** A wrapper for event log handles
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
    internal sealed class SafeEventLogWriteHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Note: RegisterEventSource returns 0 on failure 

        internal SafeEventLogWriteHandle () : base(true) {} 
 
        [DllImport(ExternDll.Advapi32, CharSet=System.Runtime.InteropServices.CharSet.Unicode, SetLastError=true)]
        internal static extern SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName); 

        [DllImport(ExternDll.Advapi32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern bool DeregisterEventSource(IntPtr hEventLog); 

        override protected bool ReleaseHandle() 
        { 
            return DeregisterEventSource(handle);
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
** Class:  SafeEventLogWriteHandle 
**
** <EMAIL>Author: David Gutierrez ([....]) </EMAIL> 
**
** A wrapper for event log handles
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
    internal sealed class SafeEventLogWriteHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Note: RegisterEventSource returns 0 on failure 

        internal SafeEventLogWriteHandle () : base(true) {} 
 
        [DllImport(ExternDll.Advapi32, CharSet=System.Runtime.InteropServices.CharSet.Unicode, SetLastError=true)]
        internal static extern SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName); 

        [DllImport(ExternDll.Advapi32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern bool DeregisterEventSource(IntPtr hEventLog); 

        override protected bool ReleaseHandle() 
        { 
            return DeregisterEventSource(handle);
        } 
    }
}

 

