//------------------------------------------------------------------------------ 
// <copyright file="ImageCodecInfoPrivate.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/*************************************************************************\ 
* 
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
* 
* Module Name:
*
*   ImageCodecInfo.cs
* 
* Abstract:
* 
*   Native GDI+ ImageCodecInfo structure. 
*
* Revision History: 
*
*   1/26/2k [....]
*       Created it.
* 
\**************************************************************************/
 
namespace System.Drawing.Imaging { 
    using System.Runtime.InteropServices;
    using System.Diagnostics; 
    using System;
    using System.Drawing;

    // sdkinc\imaging.h 
    [StructLayout(LayoutKind.Sequential, Pack=8)]
    internal class ImageCodecInfoPrivate { 
        [MarshalAs(UnmanagedType.Struct)] 
        public Guid Clsid;
        [MarshalAs(UnmanagedType.Struct)] 
        public Guid FormatID;

        public IntPtr CodecName = IntPtr.Zero;
        public IntPtr DllName = IntPtr.Zero; 
        public IntPtr FormatDescription = IntPtr.Zero;
        public IntPtr FilenameExtension = IntPtr.Zero; 
        public IntPtr MimeType = IntPtr.Zero; 

        public int Flags = 0; 
        public int Version = 0;
        public int SigCount = 0;
        public int SigSize = 0;
 
        public IntPtr SigPattern = IntPtr.Zero;
        public IntPtr SigMask = IntPtr.Zero; 
    } 
}
 
// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ImageCodecInfoPrivate.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/*************************************************************************\ 
* 
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
* 
* Module Name:
*
*   ImageCodecInfo.cs
* 
* Abstract:
* 
*   Native GDI+ ImageCodecInfo structure. 
*
* Revision History: 
*
*   1/26/2k [....]
*       Created it.
* 
\**************************************************************************/
 
namespace System.Drawing.Imaging { 
    using System.Runtime.InteropServices;
    using System.Diagnostics; 
    using System;
    using System.Drawing;

    // sdkinc\imaging.h 
    [StructLayout(LayoutKind.Sequential, Pack=8)]
    internal class ImageCodecInfoPrivate { 
        [MarshalAs(UnmanagedType.Struct)] 
        public Guid Clsid;
        [MarshalAs(UnmanagedType.Struct)] 
        public Guid FormatID;

        public IntPtr CodecName = IntPtr.Zero;
        public IntPtr DllName = IntPtr.Zero; 
        public IntPtr FormatDescription = IntPtr.Zero;
        public IntPtr FilenameExtension = IntPtr.Zero; 
        public IntPtr MimeType = IntPtr.Zero; 

        public int Flags = 0; 
        public int Version = 0;
        public int SigCount = 0;
        public int SigSize = 0;
 
        public IntPtr SigPattern = IntPtr.Zero;
        public IntPtr SigMask = IntPtr.Zero; 
    } 
}
 
// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
