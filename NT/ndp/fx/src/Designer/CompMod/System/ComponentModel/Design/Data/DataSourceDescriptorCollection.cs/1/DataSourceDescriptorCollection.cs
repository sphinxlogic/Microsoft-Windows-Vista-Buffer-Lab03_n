//------------------------------------------------------------------------------ 
// <copyright file="DataSourceDescriptorCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data 
{ 
    using System;
    using System.Collections; 

    /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection"]/*' />
    /// <devdoc>
    ///     Type safe collection of DataSourceDescriptor objects. 
    /// </devdoc>
    public class DataSourceDescriptorCollection : CollectionBase 
    { 
        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.DataSourceDescriptorCollection"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public DataSourceDescriptorCollection() : base() {
        }
 
        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.Add"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public int Add(DataSourceDescriptor value) {
            return List.Add(value); 
        }

        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.IndexOf"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public int IndexOf(DataSourceDescriptor value) { 
            return List.IndexOf(value); 
        }
 
        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.Insert"]/*' />
        /// <devdoc>
        /// </devdoc>
        public void Insert(int index, DataSourceDescriptor value) { 
            List.Insert(index, value);
        } 
 
        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.Contains"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public bool Contains(DataSourceDescriptor value) {
            return List.Contains(value);
        } 

        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.CopyTo"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public void CopyTo(DataSourceDescriptor[] array, int index) { 
            List.CopyTo(array, index);
        }

        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.Remove"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public void Remove(DataSourceDescriptor value) { 
            List.Remove(value);
        } 

        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.this"]/*' />
        /// <devdoc>
        /// </devdoc> 
        public DataSourceDescriptor this[int index] {
            get { 
                return (DataSourceDescriptor) List[index]; 
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
// <copyright file="DataSourceDescriptorCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data 
{ 
    using System;
    using System.Collections; 

    /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection"]/*' />
    /// <devdoc>
    ///     Type safe collection of DataSourceDescriptor objects. 
    /// </devdoc>
    public class DataSourceDescriptorCollection : CollectionBase 
    { 
        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.DataSourceDescriptorCollection"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public DataSourceDescriptorCollection() : base() {
        }
 
        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.Add"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public int Add(DataSourceDescriptor value) {
            return List.Add(value); 
        }

        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.IndexOf"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public int IndexOf(DataSourceDescriptor value) { 
            return List.IndexOf(value); 
        }
 
        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.Insert"]/*' />
        /// <devdoc>
        /// </devdoc>
        public void Insert(int index, DataSourceDescriptor value) { 
            List.Insert(index, value);
        } 
 
        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.Contains"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public bool Contains(DataSourceDescriptor value) {
            return List.Contains(value);
        } 

        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.CopyTo"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public void CopyTo(DataSourceDescriptor[] array, int index) { 
            List.CopyTo(array, index);
        }

        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.Remove"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public void Remove(DataSourceDescriptor value) { 
            List.Remove(value);
        } 

        /// <include file='doc\DataSourceDescriptorCollection.uex' path='docs/doc[@for="DataSourceDescriptorCollection.this"]/*' />
        /// <devdoc>
        /// </devdoc> 
        public DataSourceDescriptor this[int index] {
            get { 
                return (DataSourceDescriptor) List[index]; 
            }
 
            set {
                List[index] = value;
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
