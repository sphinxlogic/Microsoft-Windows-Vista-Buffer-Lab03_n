//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionListCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System; 
    using System.Collections;
    using System.Diagnostics; 

    /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(true)] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public class DesignerActionListCollection : CollectionBase { 

        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.DesignerActionListCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public DesignerActionListCollection() { 
        } 

        internal DesignerActionListCollection(DesignerActionList actionList) { 
            this.Add(actionList);
        }

        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.DesignerActionListCollection1"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public DesignerActionListCollection(DesignerActionList[] value) {
            AddRange(value); 
        }

        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.this"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public DesignerActionList this[int index] { 
            get {
                return (DesignerActionList)(List[index]); 
            }
            set {
                List[index] = value;
            } 
        }
 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.Add"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int Add(DesignerActionList value) {
            return List.Add(value);
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.AddRange"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public void AddRange(DesignerActionList[] value) { 
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            for (int i = 0; ((i) < (value.Length)); i = ((i) + (1))) { 
                this.Add(value[i]);
            } 
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.AddRange1"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void AddRange(DesignerActionListCollection value) {
            if (value == null) { 
                throw new ArgumentNullException("value");
            } 
            int currentCount = value.Count; 
            for (int i = 0; i < currentCount; i = ((i) + (1))) {
                this.Add(value[i]); 
            }
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.Insert"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public void Insert(int index, DesignerActionList value) { 
            List.Insert(index, value);
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public int IndexOf(DesignerActionList value) {
            return List.IndexOf(value); 
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.Contains"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(DesignerActionList value) {
            return List.Contains(value); 
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.Remove"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public void Remove(DesignerActionList value) {
            List.Remove(value);
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.CopyTo"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public void CopyTo(DesignerActionList[] array, int index) {
            List.CopyTo(array, index); 
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnSet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        protected override void OnSet(int index, object oldValue, object newValue) { 
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnInsert"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnInsert(int index, object value) {
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnClear"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        protected override void OnClear() { 
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnRemove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        protected override void OnRemove(int index, object value) { 
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnValidate"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnValidate(object value) {
            Debug.Assert(value != null, "Don't add null actionlist!"); 
        }
 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionListCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System; 
    using System.Collections;
    using System.Diagnostics; 

    /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(true)] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public class DesignerActionListCollection : CollectionBase { 

        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.DesignerActionListCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public DesignerActionListCollection() { 
        } 

        internal DesignerActionListCollection(DesignerActionList actionList) { 
            this.Add(actionList);
        }

        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.DesignerActionListCollection1"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public DesignerActionListCollection(DesignerActionList[] value) {
            AddRange(value); 
        }

        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.this"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public DesignerActionList this[int index] { 
            get {
                return (DesignerActionList)(List[index]); 
            }
            set {
                List[index] = value;
            } 
        }
 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.Add"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int Add(DesignerActionList value) {
            return List.Add(value);
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.AddRange"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public void AddRange(DesignerActionList[] value) { 
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            for (int i = 0; ((i) < (value.Length)); i = ((i) + (1))) { 
                this.Add(value[i]);
            } 
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.AddRange1"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void AddRange(DesignerActionListCollection value) {
            if (value == null) { 
                throw new ArgumentNullException("value");
            } 
            int currentCount = value.Count; 
            for (int i = 0; i < currentCount; i = ((i) + (1))) {
                this.Add(value[i]); 
            }
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.Insert"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public void Insert(int index, DesignerActionList value) { 
            List.Insert(index, value);
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public int IndexOf(DesignerActionList value) {
            return List.IndexOf(value); 
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.Contains"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(DesignerActionList value) {
            return List.Contains(value); 
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.Remove"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public void Remove(DesignerActionList value) {
            List.Remove(value);
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.CopyTo"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public void CopyTo(DesignerActionList[] array, int index) {
            List.CopyTo(array, index); 
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnSet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        protected override void OnSet(int index, object oldValue, object newValue) { 
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnInsert"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnInsert(int index, object value) {
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnClear"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        protected override void OnClear() { 
        }
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnRemove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        protected override void OnRemove(int index, object value) { 
        } 
        /// <include file='doc\DesignerActionListCollection.uex' path='docs/doc[@for="DesignerActionListCollection.OnValidate"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnValidate(object value) {
            Debug.Assert(value != null, "Don't add null actionlist!"); 
        }
 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
