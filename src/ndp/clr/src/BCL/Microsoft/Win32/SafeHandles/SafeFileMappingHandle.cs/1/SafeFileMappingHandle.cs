// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SafeViewOfFileHandle 
**
** 
** A wrapper for file handles
**
**
===========================================================*/ 
using System;
using System.Security; 
using System.Security.Permissions; 
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices; 
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;

namespace Microsoft.Win32.SafeHandles 
{
    internal sealed class SafeFileMappingHandle : SafeHandleZeroOrMinusOneIsInvalid 
    { 
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeFileMappingHandle() : base(true) {} 

        // 0 is an Invalid Handle
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeFileMappingHandle(IntPtr handle, bool ownsHandle) : base (ownsHandle) 
        {
            SetHandle(handle); 
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle); 
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
** Class:  SafeViewOfFileHandle 
**
** 
** A wrapper for file handles
**
**
===========================================================*/ 
using System;
using System.Security; 
using System.Security.Permissions; 
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices; 
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;

namespace Microsoft.Win32.SafeHandles 
{
    internal sealed class SafeFileMappingHandle : SafeHandleZeroOrMinusOneIsInvalid 
    { 
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeFileMappingHandle() : base(true) {} 

        // 0 is an Invalid Handle
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeFileMappingHandle(IntPtr handle, bool ownsHandle) : base (ownsHandle) 
        {
            SetHandle(handle); 
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle); 
        }
    } 
} 

