//------------------------------------------------------------------------------ 
// <copyright file="DataSourceGroupCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data 
{ 
    using System;
    using System.Collections; 

    /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection"]/*' />
    /// <devdoc>
    ///     Type safe collection of DataSourceGroup objects. 
    /// </devdoc>
    public class DataSourceGroupCollection : CollectionBase 
    { 
        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.DataSourceGroupCollection"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public DataSourceGroupCollection() : base() {
        }
 
        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.Add"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public int Add(DataSourceGroup value) {
            return List.Add(value); 
        }

        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.IndexOf"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public int IndexOf(DataSourceGroup value) { 
            return List.IndexOf(value); 
        }
 
        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.Insert"]/*' />
        /// <devdoc>
        /// </devdoc>
        public void Insert(int index, DataSourceGroup value) { 
            List.Insert(index, value);
        } 
 
        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.Contains"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public bool Contains(DataSourceGroup value) {
            return List.Contains(value);
        } 

        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.CopyTo"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public void CopyTo(DataSourceGroup[] array, int index) { 
            List.CopyTo(array, index);
        }

        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.Remove"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public void Remove(DataSourceGroup value) { 
            List.Remove(value);
        } 

        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.this"]/*' />
        /// <devdoc>
        /// </devdoc> 
        public DataSourceGroup this[int index] {
            get { 
                return (DataSourceGroup) List[index]; 
            }
 
            set {
                List[index] = value;
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataSourceGroupCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data 
{ 
    using System;
    using System.Collections; 

    /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection"]/*' />
    /// <devdoc>
    ///     Type safe collection of DataSourceGroup objects. 
    /// </devdoc>
    public class DataSourceGroupCollection : CollectionBase 
    { 
        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.DataSourceGroupCollection"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public DataSourceGroupCollection() : base() {
        }
 
        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.Add"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public int Add(DataSourceGroup value) {
            return List.Add(value); 
        }

        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.IndexOf"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public int IndexOf(DataSourceGroup value) { 
            return List.IndexOf(value); 
        }
 
        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.Insert"]/*' />
        /// <devdoc>
        /// </devdoc>
        public void Insert(int index, DataSourceGroup value) { 
            List.Insert(index, value);
        } 
 
        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.Contains"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public bool Contains(DataSourceGroup value) {
            return List.Contains(value);
        } 

        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.CopyTo"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public void CopyTo(DataSourceGroup[] array, int index) { 
            List.CopyTo(array, index);
        }

        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.Remove"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public void Remove(DataSourceGroup value) { 
            List.Remove(value);
        } 

        /// <include file='doc\DataSourceGroupCollection.uex' path='docs/doc[@for="DataSourceGroupCollection.this"]/*' />
        /// <devdoc>
        /// </devdoc> 
        public DataSourceGroup this[int index] {
            get { 
                return (DataSourceGroup) List[index]; 
            }
 
            set {
                List[index] = value;
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
