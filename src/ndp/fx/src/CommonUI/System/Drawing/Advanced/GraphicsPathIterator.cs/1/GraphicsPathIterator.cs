//------------------------------------------------------------------------------ 
// <copyright file="GraphicsPathIterator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/*************************************************************************\ 
* 
* Copyright (c) 1998-2000, Microsoft Corp.  All Rights Reserved.
* 
* Module Name:
*
*   GraphicsPathIterator.cs
* 
* Abstract:
* 
*   COM+ wrapper for GDI+ path iterator objects 
*
* Revision History: 
*
*    3/15/2000 [....]
*       Created it.
* 
\**************************************************************************/
 
namespace System.Drawing.Drawing2D { 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using System.Drawing; 
    using System.ComponentModel;
    using System.Drawing.Internal; 
    using System.Globalization; 

    /** 
     * Represent a Path Iterator object
     */
    /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides helper functions for the <see cref='System.Drawing.Drawing2D.GraphicsPath'/> class. 
    ///    </para> 
    /// </devdoc>
    public sealed class GraphicsPathIterator : MarshalByRefObject, IDisposable { 
        /**
         * Create a new path iterator object
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.GraphicsPathIterator"]/*' /> 
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/> class with the specified <see cref='System.Drawing.Drawing2D.GraphicsPath'/>. 
        /// </devdoc> 
        public GraphicsPathIterator(GraphicsPath path)
        { 
            IntPtr nativeIter = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreatePathIter(out nativeIter, new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath));
 
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status); 
 
            this.nativeIter = nativeIter;
        } 

        /**
         * Dispose of resources associated with the
         */ 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Dispose"]/*' />
        /// <devdoc> 
        ///    Cleans up Windows resources for this 
        /// <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/>.
        /// </devdoc> 
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        } 

        void Dispose(bool disposing) { 
            if (nativeIter != IntPtr.Zero) { 
                try{
#if DEBUG 
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeletePathIter(new HandleRef(this, nativeIter));
#if DEBUG 
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif 
                } 
                catch( Exception ex ){
                    if( ClientUtils.IsSecurityOrCriticalException( ex ) ) { 
                        throw;
                    }

                    Debug.Fail( "Exception thrown during Dispose: " + ex.ToString() ); 
                }
                finally{ 
                    nativeIter = IntPtr.Zero; 
                }
            } 
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Finalize"]/*' />
        /// <devdoc> 
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/>. 
        /// </devdoc> 
        ~GraphicsPathIterator() {
            Dispose(false); 
        }

        /**
         * Next subpath in path 
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextSubpath"]/*' /> 
        /// <devdoc> 
        ///    Returns the number of subpaths in the
        /// <see cref='System.Drawing.Drawing2D.GraphicsPath'/>. The start index and end index of the 
        ///    next subpath are contained in out parameters.
        /// </devdoc>
        public int NextSubpath(out int startIndex, out int endIndex, out bool isClosed) {
            int resultCount = 0; 
            int tempStart = 0;
            int tempEnd = 0; 
            int status = SafeNativeMethods.Gdip.GdipPathIterNextSubpath(new HandleRef(this, nativeIter), out resultCount, 
                                    out tempStart, out tempEnd, out isClosed);
 
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
            else {
                startIndex = tempStart; 
                endIndex = tempEnd;
            } 
 
            return resultCount;
        } 

        /**
         * Next subpath in path
         */ 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextSubpath1"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int NextSubpath(GraphicsPath path, out bool isClosed) { 
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextSubpathPath(new HandleRef(this, nativeIter), out resultCount,
                                    new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath), out isClosed);
 
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status); 
 
            return resultCount;
        } 

        /**
         * Next type in subpath
         */ 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextPathType"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int NextPathType(out byte pathType, out int startIndex, out int endIndex) 
        {
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextPathType(new HandleRef(this, nativeIter), out resultCount,
                                    out pathType, out startIndex, out endIndex); 

            if (status != SafeNativeMethods.Gdip.Ok) 
                throw SafeNativeMethods.Gdip.StatusException(status); 

            return resultCount; 
        }

        /**
         * Next marker in subpath 
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextMarker"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public int NextMarker(out int startIndex, out int endIndex)
        {
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextMarker(new HandleRef(this, nativeIter), out resultCount, 
                                    out startIndex, out endIndex);
 
            if (status != SafeNativeMethods.Gdip.Ok) 
                throw SafeNativeMethods.Gdip.StatusException(status);
 
            return resultCount;
        }

        /** 
         * Next marker in subpath
         */ 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextMarker1"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int NextMarker(GraphicsPath path)
        {
            int resultCount = 0; 
            int status = SafeNativeMethods.Gdip.GdipPathIterNextMarkerPath(new HandleRef(this, nativeIter), out resultCount,
                                    new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath)); 
 
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status); 

            return resultCount;
        }
 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Count"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int Count 
        {
            get {
                int resultCount = 0;
                int status = SafeNativeMethods.Gdip.GdipPathIterGetCount(new HandleRef(this, nativeIter), out resultCount); 

                if (status != SafeNativeMethods.Gdip.Ok) 
                    throw SafeNativeMethods.Gdip.StatusException(status); 

                return resultCount; 
            }
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.SubpathCount"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public int SubpathCount
        { 
            get {
                int resultCount = 0;
                int status = SafeNativeMethods.Gdip.GdipPathIterGetSubpathCount(new HandleRef(this, nativeIter), out resultCount);
 
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status); 
 
                return resultCount;
            } 
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.HasCurve"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public bool HasCurve() 
        {
            bool hasCurve = false; 

            int status = SafeNativeMethods.Gdip.GdipPathIterHasCurve(new HandleRef(this, nativeIter), out hasCurve);

            if (status != SafeNativeMethods.Gdip.Ok) 
                throw SafeNativeMethods.Gdip.StatusException(status);
 
            return hasCurve; 
        }
 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Rewind"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public void Rewind()
        { 
            int status = SafeNativeMethods.Gdip.GdipPathIterRewind(new HandleRef(this, nativeIter)); 

            if (status != SafeNativeMethods.Gdip.Ok) 
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Enumerate"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public int Enumerate(ref PointF[] points, ref byte[] types)
        { 
            if (points.Length != types.Length)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            int resultCount = 0; 
            GPPOINTF pt = new GPPOINTF();
 
            int size =  (int) Marshal.SizeOf(pt.GetType()); 
            int count = points.Length;
 
            IntPtr memoryPts =  Marshal.AllocHGlobal(count*size);
            try {
                int status = SafeNativeMethods.Gdip.GdipPathIterEnumerate(new HandleRef(this, nativeIter), out resultCount,
                                memoryPts, types, points.Length); 

                if (status != SafeNativeMethods.Gdip.Ok) { 
                    throw SafeNativeMethods.Gdip.StatusException(status); 
                }
 
                points = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(memoryPts, points.Length);
            } finally {
                Marshal.FreeHGlobal(memoryPts);
            } 

            return resultCount; 
        } 

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.CopyData"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")] 
        public int CopyData(ref PointF[] points, ref byte[] types, int startIndex, int endIndex)
        { 
            if (points.Length != types.Length) 
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);
 
            int resultCount = 0;
            GPPOINTF pt = new GPPOINTF();

            int size =  (int)Marshal.SizeOf(pt.GetType()); 
            int count = points.Length;
 
            IntPtr memoryPts =  Marshal.AllocHGlobal(count*size); 
            try {
                int status = SafeNativeMethods.Gdip.GdipPathIterCopyData(new HandleRef(this, nativeIter), out resultCount, 
                                memoryPts, types, startIndex, endIndex);

                if (status != SafeNativeMethods.Gdip.Ok) {
                    throw SafeNativeMethods.Gdip.StatusException(status); 
                }
 
                points = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(memoryPts, points.Length); 
            } finally {
                Marshal.FreeHGlobal(memoryPts); 
            }

            return resultCount;
        } 

        /* 
         * handle to native path iterator object 
         */
        internal IntPtr nativeIter; 
    }

}
 
// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved. 
//------------------------------------------------------------------------------ 
// <copyright file="GraphicsPathIterator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/*************************************************************************\ 
* 
* Copyright (c) 1998-2000, Microsoft Corp.  All Rights Reserved.
* 
* Module Name:
*
*   GraphicsPathIterator.cs
* 
* Abstract:
* 
*   COM+ wrapper for GDI+ path iterator objects 
*
* Revision History: 
*
*    3/15/2000 [....]
*       Created it.
* 
\**************************************************************************/
 
namespace System.Drawing.Drawing2D { 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using System.Drawing; 
    using System.ComponentModel;
    using System.Drawing.Internal; 
    using System.Globalization; 

    /** 
     * Represent a Path Iterator object
     */
    /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides helper functions for the <see cref='System.Drawing.Drawing2D.GraphicsPath'/> class. 
    ///    </para> 
    /// </devdoc>
    public sealed class GraphicsPathIterator : MarshalByRefObject, IDisposable { 
        /**
         * Create a new path iterator object
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.GraphicsPathIterator"]/*' /> 
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/> class with the specified <see cref='System.Drawing.Drawing2D.GraphicsPath'/>. 
        /// </devdoc> 
        public GraphicsPathIterator(GraphicsPath path)
        { 
            IntPtr nativeIter = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreatePathIter(out nativeIter, new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath));
 
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status); 
 
            this.nativeIter = nativeIter;
        } 

        /**
         * Dispose of resources associated with the
         */ 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Dispose"]/*' />
        /// <devdoc> 
        ///    Cleans up Windows resources for this 
        /// <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/>.
        /// </devdoc> 
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        } 

        void Dispose(bool disposing) { 
            if (nativeIter != IntPtr.Zero) { 
                try{
#if DEBUG 
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeletePathIter(new HandleRef(this, nativeIter));
#if DEBUG 
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif 
                } 
                catch( Exception ex ){
                    if( ClientUtils.IsSecurityOrCriticalException( ex ) ) { 
                        throw;
                    }

                    Debug.Fail( "Exception thrown during Dispose: " + ex.ToString() ); 
                }
                finally{ 
                    nativeIter = IntPtr.Zero; 
                }
            } 
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Finalize"]/*' />
        /// <devdoc> 
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/>. 
        /// </devdoc> 
        ~GraphicsPathIterator() {
            Dispose(false); 
        }

        /**
         * Next subpath in path 
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextSubpath"]/*' /> 
        /// <devdoc> 
        ///    Returns the number of subpaths in the
        /// <see cref='System.Drawing.Drawing2D.GraphicsPath'/>. The start index and end index of the 
        ///    next subpath are contained in out parameters.
        /// </devdoc>
        public int NextSubpath(out int startIndex, out int endIndex, out bool isClosed) {
            int resultCount = 0; 
            int tempStart = 0;
            int tempEnd = 0; 
            int status = SafeNativeMethods.Gdip.GdipPathIterNextSubpath(new HandleRef(this, nativeIter), out resultCount, 
                                    out tempStart, out tempEnd, out isClosed);
 
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
            else {
                startIndex = tempStart; 
                endIndex = tempEnd;
            } 
 
            return resultCount;
        } 

        /**
         * Next subpath in path
         */ 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextSubpath1"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int NextSubpath(GraphicsPath path, out bool isClosed) { 
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextSubpathPath(new HandleRef(this, nativeIter), out resultCount,
                                    new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath), out isClosed);
 
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status); 
 
            return resultCount;
        } 

        /**
         * Next type in subpath
         */ 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextPathType"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int NextPathType(out byte pathType, out int startIndex, out int endIndex) 
        {
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextPathType(new HandleRef(this, nativeIter), out resultCount,
                                    out pathType, out startIndex, out endIndex); 

            if (status != SafeNativeMethods.Gdip.Ok) 
                throw SafeNativeMethods.Gdip.StatusException(status); 

            return resultCount; 
        }

        /**
         * Next marker in subpath 
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextMarker"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public int NextMarker(out int startIndex, out int endIndex)
        {
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextMarker(new HandleRef(this, nativeIter), out resultCount, 
                                    out startIndex, out endIndex);
 
            if (status != SafeNativeMethods.Gdip.Ok) 
                throw SafeNativeMethods.Gdip.StatusException(status);
 
            return resultCount;
        }

        /** 
         * Next marker in subpath
         */ 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextMarker1"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int NextMarker(GraphicsPath path)
        {
            int resultCount = 0; 
            int status = SafeNativeMethods.Gdip.GdipPathIterNextMarkerPath(new HandleRef(this, nativeIter), out resultCount,
                                    new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath)); 
 
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status); 

            return resultCount;
        }
 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Count"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int Count 
        {
            get {
                int resultCount = 0;
                int status = SafeNativeMethods.Gdip.GdipPathIterGetCount(new HandleRef(this, nativeIter), out resultCount); 

                if (status != SafeNativeMethods.Gdip.Ok) 
                    throw SafeNativeMethods.Gdip.StatusException(status); 

                return resultCount; 
            }
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.SubpathCount"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public int SubpathCount
        { 
            get {
                int resultCount = 0;
                int status = SafeNativeMethods.Gdip.GdipPathIterGetSubpathCount(new HandleRef(this, nativeIter), out resultCount);
 
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status); 
 
                return resultCount;
            } 
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.HasCurve"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public bool HasCurve() 
        {
            bool hasCurve = false; 

            int status = SafeNativeMethods.Gdip.GdipPathIterHasCurve(new HandleRef(this, nativeIter), out hasCurve);

            if (status != SafeNativeMethods.Gdip.Ok) 
                throw SafeNativeMethods.Gdip.StatusException(status);
 
            return hasCurve; 
        }
 
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Rewind"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public void Rewind()
        { 
            int status = SafeNativeMethods.Gdip.GdipPathIterRewind(new HandleRef(this, nativeIter)); 

            if (status != SafeNativeMethods.Gdip.Ok) 
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Enumerate"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public int Enumerate(ref PointF[] points, ref byte[] types)
        { 
            if (points.Length != types.Length)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            int resultCount = 0; 
            GPPOINTF pt = new GPPOINTF();
 
            int size =  (int) Marshal.SizeOf(pt.GetType()); 
            int count = points.Length;
 
            IntPtr memoryPts =  Marshal.AllocHGlobal(count*size);
            try {
                int status = SafeNativeMethods.Gdip.GdipPathIterEnumerate(new HandleRef(this, nativeIter), out resultCount,
                                memoryPts, types, points.Length); 

                if (status != SafeNativeMethods.Gdip.Ok) { 
                    throw SafeNativeMethods.Gdip.StatusException(status); 
                }
 
                points = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(memoryPts, points.Length);
            } finally {
                Marshal.FreeHGlobal(memoryPts);
            } 

            return resultCount; 
        } 

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.CopyData"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")] 
        public int CopyData(ref PointF[] points, ref byte[] types, int startIndex, int endIndex)
        { 
            if (points.Length != types.Length) 
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);
 
            int resultCount = 0;
            GPPOINTF pt = new GPPOINTF();

            int size =  (int)Marshal.SizeOf(pt.GetType()); 
            int count = points.Length;
 
            IntPtr memoryPts =  Marshal.AllocHGlobal(count*size); 
            try {
                int status = SafeNativeMethods.Gdip.GdipPathIterCopyData(new HandleRef(this, nativeIter), out resultCount, 
                                memoryPts, types, startIndex, endIndex);

                if (status != SafeNativeMethods.Gdip.Ok) {
                    throw SafeNativeMethods.Gdip.StatusException(status); 
                }
 
                points = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(memoryPts, points.Length); 
            } finally {
                Marshal.FreeHGlobal(memoryPts); 
            }

            return resultCount;
        } 

        /* 
         * handle to native path iterator object 
         */
        internal IntPtr nativeIter; 
    }

}
 
// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved. 
