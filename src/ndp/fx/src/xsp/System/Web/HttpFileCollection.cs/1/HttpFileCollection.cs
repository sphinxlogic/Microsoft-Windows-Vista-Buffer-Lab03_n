//------------------------------------------------------------------------------ 
// <copyright file="HttpFileCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Collection of posted files for the request intrinsic 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

namespace System.Web {
 
    using System.Runtime.InteropServices;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.Security.Permissions;
    using System.Web.Util; 


    /// <devdoc>
    ///    <para> 
    ///       Accesses incoming files uploaded by a client (using
    ///       multipart MIME and the Http Content-Type of multipart/formdata). 
    ///    </para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class HttpFileCollection : NameObjectCollectionBase {
        // cached All[] arrays
        private HttpPostedFile[] _all;
        private String[] _allKeys; 

        internal HttpFileCollection() : base(Misc.CaseInsensitiveInvariantKeyComparer) { 
        } 

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(Array dest, int index) { 
            if (_all == null) {
                int n = Count; 
                _all = new HttpPostedFile[n]; 
                for (int i = 0; i < n; i++)
                    _all[i] = Get(i); 
            }

            if (_all != null) {
                _all.CopyTo(dest, index); 
            }
        } 
 
        internal void AddFile(String key, HttpPostedFile file) {
            _all = null; 
            _allKeys = null;

            BaseAdd(key, file);
        } 

#if UNUSED 
        internal void Reset() { 
            _all = null;
            _allKeys = null; 

            BaseClear();
        }
#endif 

        // 
        //  Access by name 
        //
 

        /// <devdoc>
        ///    <para>
        ///       Returns a file from 
        ///       the collection by file name.
        ///    </para> 
        /// </devdoc> 
        public HttpPostedFile Get(String name) {
            return(HttpPostedFile)BaseGet(name); 
        }


        /// <devdoc> 
        ///    <para>Returns item value from collection.</para>
        /// </devdoc> 
        public HttpPostedFile this[String name] 
        {
            get { return Get(name);} 
        }

        //
        // Indexed access 
        //
 
 
        /// <devdoc>
        ///    <para> 
        ///       Returns a file from
        ///       the file collection by index.
        ///    </para>
        /// </devdoc> 
        public HttpPostedFile Get(int index) {
            return(HttpPostedFile)BaseGet(index); 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       Returns key name from collection.
        ///    </para> 
        /// </devdoc>
        public String GetKey(int index) { 
            return BaseGetKey(index); 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Returns an 
        ///       item from the collection.
        ///    </para> 
        /// </devdoc> 
        public HttpPostedFile this[int index]
        { 
            get { return Get(index);}
        }

        // 
        // Access to keys and values as arrays
        // 
 

        /// <devdoc> 
        ///    <para>
        ///       Creates an
        ///       array of keys in the collection.
        ///    </para> 
        /// </devdoc>
        public String[] AllKeys { 
            get { 
                if (_allKeys == null)
                    _allKeys = BaseGetAllKeys(); 

                return _allKeys;
            }
        } 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="HttpFileCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Collection of posted files for the request intrinsic 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

namespace System.Web {
 
    using System.Runtime.InteropServices;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.Security.Permissions;
    using System.Web.Util; 


    /// <devdoc>
    ///    <para> 
    ///       Accesses incoming files uploaded by a client (using
    ///       multipart MIME and the Http Content-Type of multipart/formdata). 
    ///    </para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class HttpFileCollection : NameObjectCollectionBase {
        // cached All[] arrays
        private HttpPostedFile[] _all;
        private String[] _allKeys; 

        internal HttpFileCollection() : base(Misc.CaseInsensitiveInvariantKeyComparer) { 
        } 

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(Array dest, int index) { 
            if (_all == null) {
                int n = Count; 
                _all = new HttpPostedFile[n]; 
                for (int i = 0; i < n; i++)
                    _all[i] = Get(i); 
            }

            if (_all != null) {
                _all.CopyTo(dest, index); 
            }
        } 
 
        internal void AddFile(String key, HttpPostedFile file) {
            _all = null; 
            _allKeys = null;

            BaseAdd(key, file);
        } 

#if UNUSED 
        internal void Reset() { 
            _all = null;
            _allKeys = null; 

            BaseClear();
        }
#endif 

        // 
        //  Access by name 
        //
 

        /// <devdoc>
        ///    <para>
        ///       Returns a file from 
        ///       the collection by file name.
        ///    </para> 
        /// </devdoc> 
        public HttpPostedFile Get(String name) {
            return(HttpPostedFile)BaseGet(name); 
        }


        /// <devdoc> 
        ///    <para>Returns item value from collection.</para>
        /// </devdoc> 
        public HttpPostedFile this[String name] 
        {
            get { return Get(name);} 
        }

        //
        // Indexed access 
        //
 
 
        /// <devdoc>
        ///    <para> 
        ///       Returns a file from
        ///       the file collection by index.
        ///    </para>
        /// </devdoc> 
        public HttpPostedFile Get(int index) {
            return(HttpPostedFile)BaseGet(index); 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       Returns key name from collection.
        ///    </para> 
        /// </devdoc>
        public String GetKey(int index) { 
            return BaseGetKey(index); 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Returns an 
        ///       item from the collection.
        ///    </para> 
        /// </devdoc> 
        public HttpPostedFile this[int index]
        { 
            get { return Get(index);}
        }

        // 
        // Access to keys and values as arrays
        // 
 

        /// <devdoc> 
        ///    <para>
        ///       Creates an
        ///       array of keys in the collection.
        ///    </para> 
        /// </devdoc>
        public String[] AllKeys { 
            get { 
                if (_allKeys == null)
                    _allKeys = BaseGetAllKeys(); 

                return _allKeys;
            }
        } 
    }
} 
