//------------------------------------------------------------------------------ 
// <copyright file="DesignerAutoFormatCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.Drawing; 

    /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection"]/*' />
    public sealed class DesignerAutoFormatCollection : IList {
 
        private ArrayList _autoFormats = new ArrayList();
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Count"]/*' /> 
        public int Count {
            get { 
                return _autoFormats.Count;
            }
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.PreviewSize"]/*' />
        public Size PreviewSize { 
            get { 
                int height = 200;
                int width = 200; 
                foreach (DesignerAutoFormat f in _autoFormats) {
                    int heightValue = (int)f.Style.Height.Value;
                    if (heightValue > height) {
                        height = heightValue; 
                    }
                    int widthValue = (int)f.Style.Width.Value; 
                    if (widthValue > width) { 
                        width = widthValue;
                    } 
                }
                return new Size(width, height);
            }
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.SyncRoot"]/*' /> 
        public Object SyncRoot { 
            get {
                return this; 
            }
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.this"]/*' /> 
        public DesignerAutoFormat this[int index] {
            get { 
                return (DesignerAutoFormat)_autoFormats[index]; 
            }
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Add"]/*' />
        public int Add(DesignerAutoFormat format) {
            return _autoFormats.Add(format); 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Clear"]/*' /> 
        public void Clear() {
            _autoFormats.Clear(); 
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Contains"]/*' />
        public bool Contains(DesignerAutoFormat format) { 
            return _autoFormats.Contains(format);
        } 
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IndexOf"]/*' />
        public int IndexOf(DesignerAutoFormat format) { 
            return _autoFormats.IndexOf(format);
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Insert"]/*' /> 
        public void Insert(int index, DesignerAutoFormat format) {
            _autoFormats.Insert(index, format); 
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Remove"]/*' /> 
        public void Remove(DesignerAutoFormat format) {
            _autoFormats.Remove(format);
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.RemoveAt"]/*' />
        public void RemoveAt(int index) { 
            _autoFormats.RemoveAt(index); 
        }
 
        #region IList implementation
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.ICollection.Count"]/*' />
        /// <internalonly/>
        int ICollection.Count { 
            get {
                return Count; 
            } 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.IsFixedSize"]/*' />
        /// <internalonly/>
        bool IList.IsFixedSize {
            get { 
                return false;
            } 
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.IsReadOnly"]/*' /> 
        /// <internalonly/>
        bool IList.IsReadOnly {
            get {
                return false; 
            }
        } 
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.ICollection.IsSynchronized"]/*' />
        /// <internalonly/> 
        bool ICollection.IsSynchronized {
            get {
                return false;
            } 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.this"]/*' /> 
        /// <internalonly/>
        object IList.this[int index] { 
            get {
                return _autoFormats[index];
            }
            set { 
                if (value is DesignerAutoFormat) {
                    _autoFormats[index] = value; 
                } 
            }
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.Add"]/*' />
        /// <internalonly/>
        int IList.Add(object value) { 
            if (value is DesignerAutoFormat) {
                return Add((DesignerAutoFormat)value); 
            } 

            return -1; 
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.Contains"]/*' />
        /// <internalonly/> 
        bool IList.Contains(object value) {
            if (value is DesignerAutoFormat) { 
                return Contains((DesignerAutoFormat)value); 
            }
 
            return false;
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.ICollection.CopyTo"]/*' /> 
        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index) { 
            _autoFormats.CopyTo(array, index); 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IEnumerable.GetEnumerator"]/*' />
        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator() {
            return _autoFormats.GetEnumerator(); 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.IndexOf"]/*' /> 
        /// <internalonly/>
        int IList.IndexOf(object value) { 
            return IndexOf((DesignerAutoFormat)value);
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.Insert"]/*' /> 
        /// <internalonly/>
        void IList.Insert(int index, object value) { 
            if (value is DesignerAutoFormat) { 
                Insert(index, (DesignerAutoFormat)value);
            } 
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.RemoveAt"]/*' />
        /// <internalonly/> 
        void IList.RemoveAt(int index) {
            RemoveAt(index); 
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.Remove"]/*' /> 
        /// <internalonly/>
        void IList.Remove(object value) {
            if (value is DesignerAutoFormat) {
                Remove((DesignerAutoFormat)value); 
            }
        } 
        #endregion 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerAutoFormatCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.Drawing; 

    /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection"]/*' />
    public sealed class DesignerAutoFormatCollection : IList {
 
        private ArrayList _autoFormats = new ArrayList();
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Count"]/*' /> 
        public int Count {
            get { 
                return _autoFormats.Count;
            }
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.PreviewSize"]/*' />
        public Size PreviewSize { 
            get { 
                int height = 200;
                int width = 200; 
                foreach (DesignerAutoFormat f in _autoFormats) {
                    int heightValue = (int)f.Style.Height.Value;
                    if (heightValue > height) {
                        height = heightValue; 
                    }
                    int widthValue = (int)f.Style.Width.Value; 
                    if (widthValue > width) { 
                        width = widthValue;
                    } 
                }
                return new Size(width, height);
            }
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.SyncRoot"]/*' /> 
        public Object SyncRoot { 
            get {
                return this; 
            }
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.this"]/*' /> 
        public DesignerAutoFormat this[int index] {
            get { 
                return (DesignerAutoFormat)_autoFormats[index]; 
            }
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Add"]/*' />
        public int Add(DesignerAutoFormat format) {
            return _autoFormats.Add(format); 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Clear"]/*' /> 
        public void Clear() {
            _autoFormats.Clear(); 
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Contains"]/*' />
        public bool Contains(DesignerAutoFormat format) { 
            return _autoFormats.Contains(format);
        } 
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IndexOf"]/*' />
        public int IndexOf(DesignerAutoFormat format) { 
            return _autoFormats.IndexOf(format);
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Insert"]/*' /> 
        public void Insert(int index, DesignerAutoFormat format) {
            _autoFormats.Insert(index, format); 
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.Remove"]/*' /> 
        public void Remove(DesignerAutoFormat format) {
            _autoFormats.Remove(format);
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.RemoveAt"]/*' />
        public void RemoveAt(int index) { 
            _autoFormats.RemoveAt(index); 
        }
 
        #region IList implementation
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.ICollection.Count"]/*' />
        /// <internalonly/>
        int ICollection.Count { 
            get {
                return Count; 
            } 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.IsFixedSize"]/*' />
        /// <internalonly/>
        bool IList.IsFixedSize {
            get { 
                return false;
            } 
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.IsReadOnly"]/*' /> 
        /// <internalonly/>
        bool IList.IsReadOnly {
            get {
                return false; 
            }
        } 
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.ICollection.IsSynchronized"]/*' />
        /// <internalonly/> 
        bool ICollection.IsSynchronized {
            get {
                return false;
            } 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.this"]/*' /> 
        /// <internalonly/>
        object IList.this[int index] { 
            get {
                return _autoFormats[index];
            }
            set { 
                if (value is DesignerAutoFormat) {
                    _autoFormats[index] = value; 
                } 
            }
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.Add"]/*' />
        /// <internalonly/>
        int IList.Add(object value) { 
            if (value is DesignerAutoFormat) {
                return Add((DesignerAutoFormat)value); 
            } 

            return -1; 
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.Contains"]/*' />
        /// <internalonly/> 
        bool IList.Contains(object value) {
            if (value is DesignerAutoFormat) { 
                return Contains((DesignerAutoFormat)value); 
            }
 
            return false;
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.ICollection.CopyTo"]/*' /> 
        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index) { 
            _autoFormats.CopyTo(array, index); 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IEnumerable.GetEnumerator"]/*' />
        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator() {
            return _autoFormats.GetEnumerator(); 
        }
 
        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.IndexOf"]/*' /> 
        /// <internalonly/>
        int IList.IndexOf(object value) { 
            return IndexOf((DesignerAutoFormat)value);
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.Insert"]/*' /> 
        /// <internalonly/>
        void IList.Insert(int index, object value) { 
            if (value is DesignerAutoFormat) { 
                Insert(index, (DesignerAutoFormat)value);
            } 
        }

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.RemoveAt"]/*' />
        /// <internalonly/> 
        void IList.RemoveAt(int index) {
            RemoveAt(index); 
        } 

        /// <include file='doc\DesignerAutoFormatCollection.uex' path='docs/doc[@for="DesignerAutoFormatCollection.IList.Remove"]/*' /> 
        /// <internalonly/>
        void IList.Remove(object value) {
            if (value is DesignerAutoFormat) {
                Remove((DesignerAutoFormat)value); 
            }
        } 
        #endregion 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
