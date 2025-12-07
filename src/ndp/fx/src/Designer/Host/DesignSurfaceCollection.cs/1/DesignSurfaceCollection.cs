//------------------------------------------------------------------------------ 
// <copyright file="DesignSurfaceCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 

 
    /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection"]/*' /> 
    /// <devdoc>
    ///     Provides a read-only collection of design surfaces. 
    /// </devdoc>
    public sealed class DesignSurfaceCollection : ICollection {

        private DesignerCollection _designers; 

        /// <devdoc> 
        ///     Initializes a new instance of the DesignSurfaceCollection class 
        /// </devdoc>
        internal DesignSurfaceCollection(DesignerCollection designers) { 
            _designers = designers;

            if (_designers == null) {
                _designers = new DesignerCollection(null); 
            }
        } 
 
        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.Count"]/*' />
        /// <devdoc> 
        ///    Gets number of design surfaces in the collection.
        /// </devdoc>
        public int Count {
            get { 
                return _designers.Count;
            } 
        } 

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.this"]/*' /> 
        /// <devdoc>
        ///     Gets or sets the document at the specified index.
        /// </devdoc>
        public DesignSurface this[int index] { 
            get {
                IDesignerHost host = _designers[index]; 
                DesignSurface surface = host.GetService(typeof(DesignSurface)) as DesignSurface; 
                if (surface == null) {
                    throw new NotSupportedException(); 
                }
                return surface;
            }
        } 

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.GetEnumerator"]/*' /> 
        /// <devdoc> 
        ///     Creates and retrieves a new enumerator for this collection.
        /// </devdoc> 
        public IEnumerator GetEnumerator() {
            return new DesignSurfaceEnumerator(_designers.GetEnumerator());
        }
 
        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.ICollection.Count"]/*' />
        /// <internalonly/> 
        int ICollection.Count { 
            get {
                return Count; 
            }
        }

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.ICollection.IsSynchronized"]/*' /> 
        /// <internalonly/>
        bool ICollection.IsSynchronized { 
            get { 
                return false;
            } 
        }

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.ICollection.SyncRoot"]/*' />
        /// <internalonly/> 
        object ICollection.SyncRoot {
            get { 
                return null; 
            }
        } 

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.ICollection.CopyTo"]/*' />
        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index) { 
            foreach(DesignSurface surface in this) {
                array.SetValue(surface, index++); 
            } 
        }
 
        public void CopyTo(DesignSurface[] array, int index) {
            ((ICollection)this).CopyTo(array, index);
        }
 
        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.IEnumerable.GetEnumerator"]/*' />
        /// <internalonly/> 
        IEnumerator IEnumerable.GetEnumerator() { 
            return GetEnumerator();
        } 

        /// <devdoc>
        ///     Enumerator that performs the conversion from designer host
        ///     to design surface. 
        /// </devdoc>
        private class DesignSurfaceEnumerator : IEnumerator { 
 
            private IEnumerator _designerEnumerator;
 
            internal DesignSurfaceEnumerator(IEnumerator designerEnumerator) {
                _designerEnumerator = designerEnumerator;
            }
 
            public object Current {
                get { 
                    IDesignerHost host = (IDesignerHost)_designerEnumerator.Current; 

                    DesignSurface surface = host.GetService(typeof(DesignSurface)) as DesignSurface; 
                    if (surface == null) {
                        throw new NotSupportedException();
                    }
                    return surface; 
                }
            } 
 
            public bool MoveNext() {
                return _designerEnumerator.MoveNext(); 
            }

            public void Reset() {
                _designerEnumerator.Reset(); 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignSurfaceCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 

 
    /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection"]/*' /> 
    /// <devdoc>
    ///     Provides a read-only collection of design surfaces. 
    /// </devdoc>
    public sealed class DesignSurfaceCollection : ICollection {

        private DesignerCollection _designers; 

        /// <devdoc> 
        ///     Initializes a new instance of the DesignSurfaceCollection class 
        /// </devdoc>
        internal DesignSurfaceCollection(DesignerCollection designers) { 
            _designers = designers;

            if (_designers == null) {
                _designers = new DesignerCollection(null); 
            }
        } 
 
        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.Count"]/*' />
        /// <devdoc> 
        ///    Gets number of design surfaces in the collection.
        /// </devdoc>
        public int Count {
            get { 
                return _designers.Count;
            } 
        } 

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.this"]/*' /> 
        /// <devdoc>
        ///     Gets or sets the document at the specified index.
        /// </devdoc>
        public DesignSurface this[int index] { 
            get {
                IDesignerHost host = _designers[index]; 
                DesignSurface surface = host.GetService(typeof(DesignSurface)) as DesignSurface; 
                if (surface == null) {
                    throw new NotSupportedException(); 
                }
                return surface;
            }
        } 

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.GetEnumerator"]/*' /> 
        /// <devdoc> 
        ///     Creates and retrieves a new enumerator for this collection.
        /// </devdoc> 
        public IEnumerator GetEnumerator() {
            return new DesignSurfaceEnumerator(_designers.GetEnumerator());
        }
 
        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.ICollection.Count"]/*' />
        /// <internalonly/> 
        int ICollection.Count { 
            get {
                return Count; 
            }
        }

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.ICollection.IsSynchronized"]/*' /> 
        /// <internalonly/>
        bool ICollection.IsSynchronized { 
            get { 
                return false;
            } 
        }

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.ICollection.SyncRoot"]/*' />
        /// <internalonly/> 
        object ICollection.SyncRoot {
            get { 
                return null; 
            }
        } 

        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.ICollection.CopyTo"]/*' />
        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index) { 
            foreach(DesignSurface surface in this) {
                array.SetValue(surface, index++); 
            } 
        }
 
        public void CopyTo(DesignSurface[] array, int index) {
            ((ICollection)this).CopyTo(array, index);
        }
 
        /// <include file='doc\DesignSurfaceCollection.uex' path='docs/doc[@for="DesignSurfaceCollection.DesignSurfaceCollection.IEnumerable.GetEnumerator"]/*' />
        /// <internalonly/> 
        IEnumerator IEnumerable.GetEnumerator() { 
            return GetEnumerator();
        } 

        /// <devdoc>
        ///     Enumerator that performs the conversion from designer host
        ///     to design surface. 
        /// </devdoc>
        private class DesignSurfaceEnumerator : IEnumerator { 
 
            private IEnumerator _designerEnumerator;
 
            internal DesignSurfaceEnumerator(IEnumerator designerEnumerator) {
                _designerEnumerator = designerEnumerator;
            }
 
            public object Current {
                get { 
                    IDesignerHost host = (IDesignerHost)_designerEnumerator.Current; 

                    DesignSurface surface = host.GetService(typeof(DesignSurface)) as DesignSurface; 
                    if (surface == null) {
                        throw new NotSupportedException();
                    }
                    return surface; 
                }
            } 
 
            public bool MoveNext() {
                return _designerEnumerator.MoveNext(); 
            }

            public void Reset() {
                _designerEnumerator.Reset(); 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
