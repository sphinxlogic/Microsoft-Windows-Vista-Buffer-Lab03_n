//------------------------------------------------------------------------------ 
// <copyright file="UnsafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Drawing.Design { 
    using System.Runtime.InteropServices; 
    using System;
    using System.Security.Permissions; 
    using System.Collections;
    using System.IO;
    using System.Text;
 
    [
    System.Security.SuppressUnmanagedCodeSecurityAttribute() 
    ] 
    internal class UnsafeNativeMethods {
        private UnsafeNativeMethods() {} 

        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern int ClientToScreen(HandleRef hWnd, [In, Out] NativeMethods.POINT pt);
 
        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern int ScreenToClient(HandleRef hWnd, [In, Out] NativeMethods.POINT pt); 
 
        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern IntPtr SetFocus(HandleRef hWnd); 
        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern IntPtr GetFocus();

        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)] 
        public static extern void NotifyWinEvent(int winEvent, HandleRef hwnd, int objType, int objID);
 
        public const int OBJID_CLIENT = unchecked(unchecked((int)0xFFFFFFFC)); 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="UnsafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Drawing.Design { 
    using System.Runtime.InteropServices; 
    using System;
    using System.Security.Permissions; 
    using System.Collections;
    using System.IO;
    using System.Text;
 
    [
    System.Security.SuppressUnmanagedCodeSecurityAttribute() 
    ] 
    internal class UnsafeNativeMethods {
        private UnsafeNativeMethods() {} 

        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern int ClientToScreen(HandleRef hWnd, [In, Out] NativeMethods.POINT pt);
 
        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern int ScreenToClient(HandleRef hWnd, [In, Out] NativeMethods.POINT pt); 
 
        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern IntPtr SetFocus(HandleRef hWnd); 
        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern IntPtr GetFocus();

        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)] 
        public static extern void NotifyWinEvent(int winEvent, HandleRef hwnd, int objType, int objID);
 
        public const int OBJID_CLIENT = unchecked(unchecked((int)0xFFFFFFFC)); 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
