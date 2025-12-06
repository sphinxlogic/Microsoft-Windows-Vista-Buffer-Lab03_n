// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SafeLocalMemHandle 
**
** <EMAIL>Author: David Gutierrez ([....]) </EMAIL> 
**
** A wrapper for handle to local memory
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
    internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeLocalMemHandle() : base(true) {} 

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)] 
        internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle) { 
            SetHandle(existingHandle);
        } 


        [DllImport(ExternDll.Advapi32, CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern unsafe bool ConvertStringSecurityDescriptorToSecurityDescriptor(string StringSecurityDescriptor, int StringSDRevision, out SafeLocalMemHandle pSecurityDescriptor, IntPtr SecurityDescriptorSize); 

        [DllImport(ExternDll.Kernel32)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static extern IntPtr LocalFree(IntPtr hMem);
 
        override protected bool ReleaseHandle()
        {
            return LocalFree(handle) == IntPtr.Zero;
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
** Class:  SafeLocalMemHandle 
**
** <EMAIL>Author: David Gutierrez ([....]) </EMAIL> 
**
** A wrapper for handle to local memory
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
    internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeLocalMemHandle() : base(true) {} 

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)] 
        internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle) { 
            SetHandle(existingHandle);
        } 


        [DllImport(ExternDll.Advapi32, CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern unsafe bool ConvertStringSecurityDescriptorToSecurityDescriptor(string StringSecurityDescriptor, int StringSDRevision, out SafeLocalMemHandle pSecurityDescriptor, IntPtr SecurityDescriptorSize); 

        [DllImport(ExternDll.Kernel32)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static extern IntPtr LocalFree(IntPtr hMem);
 
        override protected bool ReleaseHandle()
        {
            return LocalFree(handle) == IntPtr.Zero;
        } 

    } 
} 

 



