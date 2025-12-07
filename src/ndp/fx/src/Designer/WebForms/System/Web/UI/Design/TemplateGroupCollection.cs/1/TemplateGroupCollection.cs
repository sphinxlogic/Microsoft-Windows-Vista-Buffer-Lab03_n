//------------------------------------------------------------------------------ 
// <copyright file="TemplateGroupCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Design; 
    using System.Globalization;

    /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection"]/*' />
    /// <devdoc> 
    /// Provides the necessary functionality for a template editing verb collection
    /// </devdoc> 
    public sealed class TemplateGroupCollection : IList { 
        private ArrayList _list;
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.TemplateGroupCollection"]/*' />
        public TemplateGroupCollection() {
        }
 
        internal TemplateGroupCollection(TemplateGroup[] verbs) {
            for (int i = 0; i < verbs.Length; i++) { 
                Add(verbs[i]); 
            }
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Count"]/*' />
        public int Count {
            get { 
                return InternalList.Count;
            } 
        } 

        private ArrayList InternalList { 
            get {
                if (_list == null) {
                    _list = new ArrayList();
                } 
                return _list;
            } 
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.this"]/*' /> 
        public TemplateGroup this[int index] {
            get {
                return (TemplateGroup)InternalList[index];
            } 
            set {
                InternalList[index] = value; 
            } 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Add"]/*' />
        public int Add(TemplateGroup group) {
            return InternalList.Add(group);
        } 

        public void AddRange(TemplateGroupCollection groups) { 
            InternalList.AddRange(groups); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Clear"]/*' />
        public void Clear() {
            InternalList.Clear();
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Contains"]/*' /> 
        public bool Contains(TemplateGroup group) { 
            return InternalList.Contains(group);
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.CopyTo"]/*' />
        public void CopyTo(TemplateGroup[] array, int index) {
            InternalList.CopyTo(array, index); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IndexOf"]/*' /> 
        public int IndexOf(TemplateGroup group) {
            return InternalList.IndexOf(group); 
        }

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Insert"]/*' />
        public void Insert(int index, TemplateGroup group) { 
            InternalList.Insert(index, group);
        } 
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Remove"]/*' />
        public void Remove(TemplateGroup group) { 
            InternalList.Remove(group);
        }

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.RemoveAt"]/*' /> 
        public void RemoveAt(int index) {
            InternalList.RemoveAt(index); 
        } 

        #region IList implementation 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.ICollection.Count"]/*' />
        /// <internalonly/>
        int ICollection.Count {
            get { 
                return Count;
            } 
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.IsFixedSize"]/*' /> 
        /// <internalonly/>
        bool IList.IsFixedSize {
            get {
                return InternalList.IsFixedSize; 
            }
        } 
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.IsReadOnly"]/*' />
        /// <internalonly/> 
        bool IList.IsReadOnly {
            get {
                return InternalList.IsReadOnly;
            } 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.ICollection.IsSynchronized"]/*' /> 
        /// <internalonly/>
        bool ICollection.IsSynchronized { 
            get {
                return InternalList.IsSynchronized;
            }
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.ICollection.SyncRoot"]/*' /> 
        /// <internalonly/> 
        object ICollection.SyncRoot {
            get { 
                return InternalList.SyncRoot;
            }
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.this"]/*' />
        /// <internalonly/> 
        object IList.this[int index] { 
            get {
                return this[index]; 
            }
            set {
                if (!(value is TemplateGroup)) {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "value"); 
                }
 
                this[index] = (TemplateGroup)value; 
            }
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Add"]/*' />
        /// <internalonly/>
        int IList.Add(object o) { 
            if (!(o is TemplateGroup)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o"); 
            } 

            return Add((TemplateGroup)o); 
        }

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Clear"]/*' />
        /// <internalonly/> 
        void IList.Clear() {
            Clear(); 
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Contains"]/*' /> 
        /// <internalonly/>
        bool IList.Contains(object o) {
            if (!(o is TemplateGroup)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o"); 
            }
 
            return Contains((TemplateGroup)o); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.ICollection.CopyTo"]/*' />
        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index) {
            InternalList.CopyTo(array, index); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IEnumerable.GetEnumerator"]/*' /> 
        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator() { 
            return InternalList.GetEnumerator();
        }

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.IndexOf"]/*' /> 
        /// <internalonly/>
        int IList.IndexOf(object o) { 
            if (!(o is TemplateGroup)) { 
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o");
            } 

            return IndexOf((TemplateGroup)o);
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Insert"]/*' />
        /// <internalonly/> 
        void IList.Insert(int index, object o) { 
            if (!(o is TemplateGroup)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o"); 
            }

            Insert(index, (TemplateGroup)o);
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Remove"]/*' /> 
        /// <internalonly/> 
        void IList.Remove(object o) {
            if (!(o is TemplateGroup)) { 
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o");
            }

            Remove((TemplateGroup)o); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.RemoveAt"]/*' /> 
        /// <internalonly/>
        void IList.RemoveAt(int index) { 
            RemoveAt(index);
        }

        #endregion 
    }
} 
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TemplateGroupCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Design; 
    using System.Globalization;

    /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection"]/*' />
    /// <devdoc> 
    /// Provides the necessary functionality for a template editing verb collection
    /// </devdoc> 
    public sealed class TemplateGroupCollection : IList { 
        private ArrayList _list;
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.TemplateGroupCollection"]/*' />
        public TemplateGroupCollection() {
        }
 
        internal TemplateGroupCollection(TemplateGroup[] verbs) {
            for (int i = 0; i < verbs.Length; i++) { 
                Add(verbs[i]); 
            }
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Count"]/*' />
        public int Count {
            get { 
                return InternalList.Count;
            } 
        } 

        private ArrayList InternalList { 
            get {
                if (_list == null) {
                    _list = new ArrayList();
                } 
                return _list;
            } 
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.this"]/*' /> 
        public TemplateGroup this[int index] {
            get {
                return (TemplateGroup)InternalList[index];
            } 
            set {
                InternalList[index] = value; 
            } 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Add"]/*' />
        public int Add(TemplateGroup group) {
            return InternalList.Add(group);
        } 

        public void AddRange(TemplateGroupCollection groups) { 
            InternalList.AddRange(groups); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Clear"]/*' />
        public void Clear() {
            InternalList.Clear();
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Contains"]/*' /> 
        public bool Contains(TemplateGroup group) { 
            return InternalList.Contains(group);
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.CopyTo"]/*' />
        public void CopyTo(TemplateGroup[] array, int index) {
            InternalList.CopyTo(array, index); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IndexOf"]/*' /> 
        public int IndexOf(TemplateGroup group) {
            return InternalList.IndexOf(group); 
        }

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Insert"]/*' />
        public void Insert(int index, TemplateGroup group) { 
            InternalList.Insert(index, group);
        } 
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.Remove"]/*' />
        public void Remove(TemplateGroup group) { 
            InternalList.Remove(group);
        }

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.RemoveAt"]/*' /> 
        public void RemoveAt(int index) {
            InternalList.RemoveAt(index); 
        } 

        #region IList implementation 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.ICollection.Count"]/*' />
        /// <internalonly/>
        int ICollection.Count {
            get { 
                return Count;
            } 
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.IsFixedSize"]/*' /> 
        /// <internalonly/>
        bool IList.IsFixedSize {
            get {
                return InternalList.IsFixedSize; 
            }
        } 
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.IsReadOnly"]/*' />
        /// <internalonly/> 
        bool IList.IsReadOnly {
            get {
                return InternalList.IsReadOnly;
            } 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.ICollection.IsSynchronized"]/*' /> 
        /// <internalonly/>
        bool ICollection.IsSynchronized { 
            get {
                return InternalList.IsSynchronized;
            }
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.ICollection.SyncRoot"]/*' /> 
        /// <internalonly/> 
        object ICollection.SyncRoot {
            get { 
                return InternalList.SyncRoot;
            }
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.this"]/*' />
        /// <internalonly/> 
        object IList.this[int index] { 
            get {
                return this[index]; 
            }
            set {
                if (!(value is TemplateGroup)) {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "value"); 
                }
 
                this[index] = (TemplateGroup)value; 
            }
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Add"]/*' />
        /// <internalonly/>
        int IList.Add(object o) { 
            if (!(o is TemplateGroup)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o"); 
            } 

            return Add((TemplateGroup)o); 
        }

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Clear"]/*' />
        /// <internalonly/> 
        void IList.Clear() {
            Clear(); 
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Contains"]/*' /> 
        /// <internalonly/>
        bool IList.Contains(object o) {
            if (!(o is TemplateGroup)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o"); 
            }
 
            return Contains((TemplateGroup)o); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.ICollection.CopyTo"]/*' />
        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index) {
            InternalList.CopyTo(array, index); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IEnumerable.GetEnumerator"]/*' /> 
        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator() { 
            return InternalList.GetEnumerator();
        }

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.IndexOf"]/*' /> 
        /// <internalonly/>
        int IList.IndexOf(object o) { 
            if (!(o is TemplateGroup)) { 
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o");
            } 

            return IndexOf((TemplateGroup)o);
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Insert"]/*' />
        /// <internalonly/> 
        void IList.Insert(int index, object o) { 
            if (!(o is TemplateGroup)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o"); 
            }

            Insert(index, (TemplateGroup)o);
        } 

        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.Remove"]/*' /> 
        /// <internalonly/> 
        void IList.Remove(object o) {
            if (!(o is TemplateGroup)) { 
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "TemplateGroup"), "o");
            }

            Remove((TemplateGroup)o); 
        }
 
        /// <include file='doc\TemplateGroupCollection.uex' path='docs/doc[@for="TemplateGroupCollection.IList.RemoveAt"]/*' /> 
        /// <internalonly/>
        void IList.RemoveAt(int index) { 
            RemoveAt(index);
        }

        #endregion 
    }
} 
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
