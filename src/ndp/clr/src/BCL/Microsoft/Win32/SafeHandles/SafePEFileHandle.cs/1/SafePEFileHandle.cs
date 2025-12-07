// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SafePEFileHandle 
**
** 
** A wrapper for pefile pointers
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
 
namespace Microsoft.Win32.SafeHandles
{ 
    internal sealed class SafePEFileHandle: SafeHandleZeroOrMinusOneIsInvalid 
    {
        // 0 is an Invalid Handle 
        private SafePEFileHandle(IntPtr handle) : base (true)
        {
            SetHandle(handle);
        } 

        internal static SafePEFileHandle InvalidHandle 
        { 
            get { return new SafePEFileHandle(IntPtr.Zero); }
        } 

        override protected bool ReleaseHandle()
        {
#if !FEATURE_PAL 
            System.Security.Policy.Hash._ReleasePEFile(handle);
#endif 
            return true; 
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
** Class:  SafePEFileHandle 
**
** 
** A wrapper for pefile pointers
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
 
namespace Microsoft.Win32.SafeHandles
{ 
    internal sealed class SafePEFileHandle: SafeHandleZeroOrMinusOneIsInvalid 
    {
        // 0 is an Invalid Handle 
        private SafePEFileHandle(IntPtr handle) : base (true)
        {
            SetHandle(handle);
        } 

        internal static SafePEFileHandle InvalidHandle 
        { 
            get { return new SafePEFileHandle(IntPtr.Zero); }
        } 

        override protected bool ReleaseHandle()
        {
#if !FEATURE_PAL 
            System.Security.Policy.Hash._ReleasePEFile(handle);
#endif 
            return true; 
        }
    } 
}

