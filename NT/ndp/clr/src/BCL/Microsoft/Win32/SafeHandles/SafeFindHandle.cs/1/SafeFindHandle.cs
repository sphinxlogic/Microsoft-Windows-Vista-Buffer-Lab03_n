// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SafeFindHandle 
**
** 
** A wrapper for find handles
**
**
===========================================================*/ 

using System; 
using System.Security; 
using System.Security.Permissions;
using System.Runtime.InteropServices; 
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32;
 
namespace Microsoft.Win32.SafeHandles {
    internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid 
    { 
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeFindHandle() : base(true) {} 

        override protected bool ReleaseHandle()
        {
            return Win32Native.FindClose(handle); 
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
** Class:  SafeFindHandle 
**
** 
** A wrapper for find handles
**
**
===========================================================*/ 

using System; 
using System.Security; 
using System.Security.Permissions;
using System.Runtime.InteropServices; 
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32;
 
namespace Microsoft.Win32.SafeHandles {
    internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid 
    { 
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeFindHandle() : base(true) {} 

        override protected bool ReleaseHandle()
        {
            return Win32Native.FindClose(handle); 
        }
    } 
} 
