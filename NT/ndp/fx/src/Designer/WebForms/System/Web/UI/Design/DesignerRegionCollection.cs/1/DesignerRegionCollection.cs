//------------------------------------------------------------------------------ 
// <copyright file="DesignerRegionCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Design; 
    using System.Globalization;

    /// <include file='doc\DesignerRegionCollection.uex' path='docs/doc[@for="DesignerRegionCollection"]/*' />
    /// <devdoc> 
    ///   Provides the necessary functionality for a designer region collection
    /// </devdoc> 
    public class DesignerRegionCollection : IList{ 
        private ArrayList _list;
        private ControlDesigner _owner; 

        public DesignerRegionCollection() {
        }
 
        public DesignerRegionCollection(ControlDesigner owner) {
            _owner = owner; 
        } 

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

        public bool IsFixedSize { 
            get { 
                return InternalList.IsFixedSize;
            } 
        }

        public bool IsReadOnly {
            get { 
                return InternalList.IsReadOnly;
            } 
        } 

        public bool IsSynchronized { 
            get {
                return InternalList.IsSynchronized;
            }
        } 

        public ControlDesigner Owner { 
            get { 
                return _owner;
            } 
        }

        public object SyncRoot {
            get { 
                return InternalList.SyncRoot;
            } 
        } 

        public DesignerRegion this[int index] { 
            get {
                return (DesignerRegion)InternalList[index];
            }
            set { 
                InternalList[index] = value;
            } 
        } 

        public int Add(DesignerRegion region) { 
            return InternalList.Add(region);
        }

        public void Clear() { 
            InternalList.Clear();
        } 
 
        public void CopyTo(Array array, int index) {
            InternalList.CopyTo(array, index); 
        }

        public IEnumerator GetEnumerator() {
            return InternalList.GetEnumerator(); 
        }
 
        public bool Contains(DesignerRegion region) { 
            return InternalList.Contains(region);
        } 

        public int IndexOf(DesignerRegion region) {
            return InternalList.IndexOf(region);
        } 

        public void Insert(int index, DesignerRegion region) { 
            InternalList.Insert(index, region); 
        }
 
        public void Remove(DesignerRegion region) {
            InternalList.Remove(region);
        }
 
        public void RemoveAt(int index) {
            InternalList.RemoveAt(index); 
        } 

        #region IList implementation 
        int ICollection.Count {
            get {
                return Count;
            } 
        }
 
        bool IList.IsFixedSize { 
            get {
                return IsFixedSize; 
            }
        }

        bool IList.IsReadOnly { 
            get {
                return IsReadOnly; 
            } 
        }
 
        bool ICollection.IsSynchronized {
            get {
                return IsSynchronized;
            } 
        }
 
        object ICollection.SyncRoot { 
            get {
                return SyncRoot; 
            }
        }

        object IList.this[int index] { 
            get {
                return this[index]; 
            } 
            set {
                if (!(value is DesignerRegion)) { 
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "value");
                }

                this[index] = (DesignerRegion)value; 
            }
        } 
 
        int IList.Add(object o) {
            if (!(o is DesignerRegion)) { 
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o");
            }

            return Add((DesignerRegion)o); 
        }
 
        void IList.Clear() { 
            Clear();
        } 

        bool IList.Contains(object o) {
            if (!(o is DesignerRegion)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o"); 
            }
 
            return Contains((DesignerRegion)o); 
        }
 
        void ICollection.CopyTo(Array array, int index) {
            CopyTo(array, index);
        }
 
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator(); 
        } 

        int IList.IndexOf(object o) { 
            if (!(o is DesignerRegion)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o");
            }
 
            return IndexOf((DesignerRegion)o);
        } 
 
        void IList.Insert(int index, object o) {
            if (!(o is DesignerRegion)) { 
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o");
            }

            Insert(index, (DesignerRegion)o); 
        }
 
        void IList.Remove(object o) { 
            if (!(o is DesignerRegion)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o"); 
            }

            Remove((DesignerRegion)o);
        } 

        void IList.RemoveAt(int index) { 
            RemoveAt(index); 
        }
 
        #endregion
    }
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerRegionCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Design; 
    using System.Globalization;

    /// <include file='doc\DesignerRegionCollection.uex' path='docs/doc[@for="DesignerRegionCollection"]/*' />
    /// <devdoc> 
    ///   Provides the necessary functionality for a designer region collection
    /// </devdoc> 
    public class DesignerRegionCollection : IList{ 
        private ArrayList _list;
        private ControlDesigner _owner; 

        public DesignerRegionCollection() {
        }
 
        public DesignerRegionCollection(ControlDesigner owner) {
            _owner = owner; 
        } 

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

        public bool IsFixedSize { 
            get { 
                return InternalList.IsFixedSize;
            } 
        }

        public bool IsReadOnly {
            get { 
                return InternalList.IsReadOnly;
            } 
        } 

        public bool IsSynchronized { 
            get {
                return InternalList.IsSynchronized;
            }
        } 

        public ControlDesigner Owner { 
            get { 
                return _owner;
            } 
        }

        public object SyncRoot {
            get { 
                return InternalList.SyncRoot;
            } 
        } 

        public DesignerRegion this[int index] { 
            get {
                return (DesignerRegion)InternalList[index];
            }
            set { 
                InternalList[index] = value;
            } 
        } 

        public int Add(DesignerRegion region) { 
            return InternalList.Add(region);
        }

        public void Clear() { 
            InternalList.Clear();
        } 
 
        public void CopyTo(Array array, int index) {
            InternalList.CopyTo(array, index); 
        }

        public IEnumerator GetEnumerator() {
            return InternalList.GetEnumerator(); 
        }
 
        public bool Contains(DesignerRegion region) { 
            return InternalList.Contains(region);
        } 

        public int IndexOf(DesignerRegion region) {
            return InternalList.IndexOf(region);
        } 

        public void Insert(int index, DesignerRegion region) { 
            InternalList.Insert(index, region); 
        }
 
        public void Remove(DesignerRegion region) {
            InternalList.Remove(region);
        }
 
        public void RemoveAt(int index) {
            InternalList.RemoveAt(index); 
        } 

        #region IList implementation 
        int ICollection.Count {
            get {
                return Count;
            } 
        }
 
        bool IList.IsFixedSize { 
            get {
                return IsFixedSize; 
            }
        }

        bool IList.IsReadOnly { 
            get {
                return IsReadOnly; 
            } 
        }
 
        bool ICollection.IsSynchronized {
            get {
                return IsSynchronized;
            } 
        }
 
        object ICollection.SyncRoot { 
            get {
                return SyncRoot; 
            }
        }

        object IList.this[int index] { 
            get {
                return this[index]; 
            } 
            set {
                if (!(value is DesignerRegion)) { 
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "value");
                }

                this[index] = (DesignerRegion)value; 
            }
        } 
 
        int IList.Add(object o) {
            if (!(o is DesignerRegion)) { 
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o");
            }

            return Add((DesignerRegion)o); 
        }
 
        void IList.Clear() { 
            Clear();
        } 

        bool IList.Contains(object o) {
            if (!(o is DesignerRegion)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o"); 
            }
 
            return Contains((DesignerRegion)o); 
        }
 
        void ICollection.CopyTo(Array array, int index) {
            CopyTo(array, index);
        }
 
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator(); 
        } 

        int IList.IndexOf(object o) { 
            if (!(o is DesignerRegion)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o");
            }
 
            return IndexOf((DesignerRegion)o);
        } 
 
        void IList.Insert(int index, object o) {
            if (!(o is DesignerRegion)) { 
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o");
            }

            Insert(index, (DesignerRegion)o); 
        }
 
        void IList.Remove(object o) { 
            if (!(o is DesignerRegion)) {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.WrongType), "DesignerRegion"), "o"); 
            }

            Remove((DesignerRegion)o);
        } 

        void IList.RemoveAt(int index) { 
            RemoveAt(index); 
        }
 
        #endregion
    }
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
