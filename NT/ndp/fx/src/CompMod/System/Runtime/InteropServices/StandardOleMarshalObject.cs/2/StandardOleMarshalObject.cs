//------------------------------------------------------------------------------ 
// <copyright file="COM2AboutBoxPropertyDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

    namespace System.Runtime.InteropServices { 
    using System.Diagnostics; 
    using System;
    using Microsoft.Win32; 
    using System.Security;
    using System.Security.Permissions;

    /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject"]/*' /> 
    /// <internalonly/>
    /// <devdoc> 
    /// Replaces the standard CLR free-threaded marshaler with the standard OLE STA one.  This prevents the calls made into 
    /// our hosting object by OLE from coming in on threads other than the UI thread.
    /// 
    /// </devdoc>
    [ComVisible(true)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1403:AutoLayoutTypesShouldNotBeComVisible")]
    public class StandardOleMarshalObject : MarshalByRefObject, UnsafeNativeMethods.IMarshal { 

        protected StandardOleMarshalObject() { 
        } 

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
        private IntPtr GetStdMarshaller(ref Guid riid, int dwDestContext, int mshlflags) {
            IntPtr pStdMarshal = IntPtr.Zero;
            IntPtr pUnk = Marshal.GetIUnknownForObject(this);
            if (pUnk != IntPtr.Zero) { 
                try {
                    if (NativeMethods.S_OK == UnsafeNativeMethods.CoGetStandardMarshal(ref riid, pUnk, dwDestContext, IntPtr.Zero, mshlflags, out pStdMarshal)) { 
                        Debug.Assert(pStdMarshal != null, "Failed to get marshaller for interface '" +  riid.ToString() + "', CoGetStandardMarshal returned S_OK"); 
                        return pStdMarshal;
                    } 
                }
                finally {
                    Marshal.Release(pUnk);
                } 
            }
            throw new InvalidOperationException(SR.GetString(SR.StandardOleMarshalObjectGetMarshalerFailed, riid.ToString())); 
        } 

 

        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.GetUnmarshalClass"]/*' />
        /// <internalonly/>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
        int UnsafeNativeMethods.IMarshal.GetUnmarshalClass(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out Guid pCid){
 
            pCid = typeof(UnsafeNativeMethods.IStdMarshal).GUID; 
            return NativeMethods.S_OK;
        } 

        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.GetMarshalSizeMax"]/*' />
        /// <internalonly/>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
        int UnsafeNativeMethods.IMarshal.GetMarshalSizeMax(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize) {
 
            // 98830 - GUID marshaling in StandardOleMarshalObject AVs on 64-bit 
            Guid riid_copy = riid;
            IntPtr pStandardMarshal = GetStdMarshaller(ref riid_copy, dwDestContext, mshlflags); 

            try {
                return UnsafeNativeMethods.CoGetMarshalSizeMax(out pSize, ref riid_copy, pStandardMarshal, dwDestContext, pvDestContext, mshlflags);
            } 
            finally {
                Marshal.Release(pStandardMarshal); 
            } 
        }
 
        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.MarshalInterface"]/*' />
        /// <internalonly/>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        int UnsafeNativeMethods.IMarshal.MarshalInterface(object pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags) { 

            IntPtr pStandardMarshal = GetStdMarshaller(ref riid, dwDestContext, mshlflags); 
 
            try {
                return UnsafeNativeMethods.CoMarshalInterface(pStm, ref riid, pStandardMarshal, dwDestContext, pvDestContext, mshlflags); 
            }
            finally {
                Marshal.Release(pStandardMarshal);
                if (pStm != null) { 
                    Marshal.ReleaseComObject(pStm);
                } 
            } 
        }
 
        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.UnmarshalInterface"]/*' />
        /// <internalonly/>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        int UnsafeNativeMethods.IMarshal.UnmarshalInterface(object pStm, ref Guid riid, out IntPtr ppv) { 
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::UnmarshalInterface should not be called."); 
            ppv = IntPtr.Zero; 
            if (pStm != null) {
                Marshal.ReleaseComObject(pStm); 
            }
            return NativeMethods.E_NOTIMPL;
        }
 
        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.ReleaseMarshalData"]/*' />
        /// <internalonly/> 
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
        int UnsafeNativeMethods.IMarshal.ReleaseMarshalData(object pStm) {
            // this should never be called on this interface, but on the standard one handed back by the previous calls. 
            Debug.Fail("IMarshal::ReleaseMarshalData should not be called.");
            if (pStm != null) {
                Marshal.ReleaseComObject(pStm);
            } 
            return NativeMethods.E_NOTIMPL;
        } 
 
        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.DisconnectObject"]/*' />
        /// <internalonly/> 
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        int UnsafeNativeMethods.IMarshal.DisconnectObject(int dwReserved) {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::DisconnectObject should not be called."); 
            return NativeMethods.E_NOTIMPL;
        } 
    } 
}
 
//------------------------------------------------------------------------------ 
// <copyright file="COM2AboutBoxPropertyDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

    namespace System.Runtime.InteropServices { 
    using System.Diagnostics; 
    using System;
    using Microsoft.Win32; 
    using System.Security;
    using System.Security.Permissions;

    /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject"]/*' /> 
    /// <internalonly/>
    /// <devdoc> 
    /// Replaces the standard CLR free-threaded marshaler with the standard OLE STA one.  This prevents the calls made into 
    /// our hosting object by OLE from coming in on threads other than the UI thread.
    /// 
    /// </devdoc>
    [ComVisible(true)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1403:AutoLayoutTypesShouldNotBeComVisible")]
    public class StandardOleMarshalObject : MarshalByRefObject, UnsafeNativeMethods.IMarshal { 

        protected StandardOleMarshalObject() { 
        } 

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
        private IntPtr GetStdMarshaller(ref Guid riid, int dwDestContext, int mshlflags) {
            IntPtr pStdMarshal = IntPtr.Zero;
            IntPtr pUnk = Marshal.GetIUnknownForObject(this);
            if (pUnk != IntPtr.Zero) { 
                try {
                    if (NativeMethods.S_OK == UnsafeNativeMethods.CoGetStandardMarshal(ref riid, pUnk, dwDestContext, IntPtr.Zero, mshlflags, out pStdMarshal)) { 
                        Debug.Assert(pStdMarshal != null, "Failed to get marshaller for interface '" +  riid.ToString() + "', CoGetStandardMarshal returned S_OK"); 
                        return pStdMarshal;
                    } 
                }
                finally {
                    Marshal.Release(pUnk);
                } 
            }
            throw new InvalidOperationException(SR.GetString(SR.StandardOleMarshalObjectGetMarshalerFailed, riid.ToString())); 
        } 

 

        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.GetUnmarshalClass"]/*' />
        /// <internalonly/>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
        int UnsafeNativeMethods.IMarshal.GetUnmarshalClass(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out Guid pCid){
 
            pCid = typeof(UnsafeNativeMethods.IStdMarshal).GUID; 
            return NativeMethods.S_OK;
        } 

        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.GetMarshalSizeMax"]/*' />
        /// <internalonly/>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
        int UnsafeNativeMethods.IMarshal.GetMarshalSizeMax(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize) {
 
            // 98830 - GUID marshaling in StandardOleMarshalObject AVs on 64-bit 
            Guid riid_copy = riid;
            IntPtr pStandardMarshal = GetStdMarshaller(ref riid_copy, dwDestContext, mshlflags); 

            try {
                return UnsafeNativeMethods.CoGetMarshalSizeMax(out pSize, ref riid_copy, pStandardMarshal, dwDestContext, pvDestContext, mshlflags);
            } 
            finally {
                Marshal.Release(pStandardMarshal); 
            } 
        }
 
        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.MarshalInterface"]/*' />
        /// <internalonly/>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        int UnsafeNativeMethods.IMarshal.MarshalInterface(object pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags) { 

            IntPtr pStandardMarshal = GetStdMarshaller(ref riid, dwDestContext, mshlflags); 
 
            try {
                return UnsafeNativeMethods.CoMarshalInterface(pStm, ref riid, pStandardMarshal, dwDestContext, pvDestContext, mshlflags); 
            }
            finally {
                Marshal.Release(pStandardMarshal);
                if (pStm != null) { 
                    Marshal.ReleaseComObject(pStm);
                } 
            } 
        }
 
        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.UnmarshalInterface"]/*' />
        /// <internalonly/>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        int UnsafeNativeMethods.IMarshal.UnmarshalInterface(object pStm, ref Guid riid, out IntPtr ppv) { 
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::UnmarshalInterface should not be called."); 
            ppv = IntPtr.Zero; 
            if (pStm != null) {
                Marshal.ReleaseComObject(pStm); 
            }
            return NativeMethods.E_NOTIMPL;
        }
 
        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.ReleaseMarshalData"]/*' />
        /// <internalonly/> 
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
        int UnsafeNativeMethods.IMarshal.ReleaseMarshalData(object pStm) {
            // this should never be called on this interface, but on the standard one handed back by the previous calls. 
            Debug.Fail("IMarshal::ReleaseMarshalData should not be called.");
            if (pStm != null) {
                Marshal.ReleaseComObject(pStm);
            } 
            return NativeMethods.E_NOTIMPL;
        } 
 
        /// <include file='doc\StandardOleMarshalObject.uex' path='docs/doc[@for="StandardOleMarshalObject.UnsafeNativeMethods.IMarshal.DisconnectObject"]/*' />
        /// <internalonly/> 
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        int UnsafeNativeMethods.IMarshal.DisconnectObject(int dwReserved) {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::DisconnectObject should not be called."); 
            return NativeMethods.E_NOTIMPL;
        } 
    } 
}
 
