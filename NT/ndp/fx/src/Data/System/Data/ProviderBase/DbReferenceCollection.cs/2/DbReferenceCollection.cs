//------------------------------------------------------------------------------ 
// <copyright file="DbReferenceCollection.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.ProviderBase {
 
    using System;
    using System.Collections;
    using System.Diagnostics;
 
    internal abstract class DbReferenceCollection {
 
#if ORACLE 
        abstract public void Add(object value, int tag);
        abstract public void Notify(int message); 
        abstract public void Remove(object value);
#else

        private struct CollectionEntry { 

            private int _tag; // information about the reference 
            private WeakReference _weak; // the reference itself. 

            public bool HasTarget { 
                get {
                    Debug.Assert((0 != _tag && null != _weak) || (null == _weak || !_weak.IsAlive), "0 tag with Target");
                    return ((0 != _tag) && null != _weak && _weak.IsAlive);
                } 
            }
 
            public bool InUse { 
                get {
                    return (null != _weak); 
                }
            }

            public int Tag { 
                get {
                     return _tag; 
                } 
                set {
                     _tag = value; 
                }
            }

            public object Target { 
                get {
                    if (0 != _tag) { 
                        return _weak.Target; 
                    }
                    Debug.Assert(!_weak.IsAlive, "0 tag with Target"); 
                    return null;
                }
                set {
                    if (null == _weak) { 
                        _weak = new WeakReference(value, false);
                    } 
                    else { 
                        _weak.Target = value;
                    } 
                }
            }
        }
 
        private CollectionEntry[] _items;
 
        protected DbReferenceCollection() { 
            _items = new CollectionEntry[5];
        } 

        abstract public void Add(object value, int tag);

        protected void AddItem(object value, int tag) { 
            Debug.Assert(null != value && 0 != tag, "AddItem with null value or 0 tag");
            CollectionEntry[] items = _items; 
            for (int i = 0; i < items.Length; ++i) { 
                if (!items[i].HasTarget) {
                    items[i].Target = value; 
                    items[i].Tag = tag;
                    Debug.Assert(items[i].HasTarget, "missing expected target");
                    return;
                } 
            }
            int newlength = ((5 == items.Length) ? 15 : (items.Length + 15)); 
            CollectionEntry[] jtems = new CollectionEntry[newlength]; 
            for (int i = 0; i < items.Length; ++i) {
                jtems[i] = items[i]; 
            }
            jtems[items.Length].Target = value;
            jtems[items.Length].Tag = tag;
            Debug.Assert(jtems[items.Length].HasTarget, "missing expected target"); 
            _items = jtems;
        } 
 
        internal IEnumerable Filter(int tag) {
            return new DbFilteredReferenceCollection (_items, tag); 
        }

        public void Notify(int message) {
            CollectionEntry[] items = _items; 
            for (int index = 0; index < items.Length; ++index) {
                if (items[index].InUse) { 
                    object value = items[index].Target; // checks tag & gets target 
                    if (null != value) {
                        Debug.Assert(items[index].HasTarget, "missing expected target"); 
                        if (!NotifyItem(message, items[index].Tag, value)) {
                            items[index].Tag = 0;
                            items[index].Target = null;
                            Debug.Assert(!items[index].HasTarget, "has unexpected target"); 
                        }
                    } 
                    // else (0 == Tag) or !IsAlive 
                }
                else { 
                    Debug.Assert(!items[index].HasTarget, "has unexpected target");
                    break;  // we're done as soon as we find the first null in the list
                }
            } 
        }
 
        abstract protected bool NotifyItem(int message, int tag, object value); 

        public void Purge() { 
            CollectionEntry[] items = _items;
#if DEBUG
            for(int i = 0; i < items.Length; ++i) {
                Debug.Assert(!items[i].HasTarget, "unexpected target during purge"); 
            }
#endif 
            if (100 < items.Length) { 
                _items = new CollectionEntry[5];    // revert back to 5 items if we're really big...
            } 
        }

        abstract public void Remove(object value);
 
        protected void RemoveItem(object value) {
            Debug.Assert(null != value, "RemoveItem with null"); 
            CollectionEntry[] items = _items; 

            for (int index = 0; index < items.Length; ++index) { 
                if (items[index].InUse) {
                    if (value == items[index].Target) { // checks tag & gets target
                        items[index].Tag = 0;
                        items[index].Target = null; 
                        Debug.Assert(!items[index].HasTarget, "has unexpected target");
                        break; 
                    } 
                    // else (0 == Tag) or !IsAlive or (value != Target)
                } 
                else {
                    break;  // we're done as soon as we find the first null in the list
                }
            } 
        }
 
        private struct DbFilteredReferenceCollection : IEnumerable { 
            private readonly DbReferenceCollection.CollectionEntry[] _items;
            private readonly int _filterTag; 

            internal DbFilteredReferenceCollection(DbReferenceCollection.CollectionEntry[] items, int filterTag) {
                _items = items;
                _filterTag = filterTag; 
            }
 
            IEnumerator IEnumerable.GetEnumerator() { 
                return new DbFilteredReferenceCollectionedEnumerator(_items, _filterTag);
            } 

            private struct DbFilteredReferenceCollectionedEnumerator : IEnumerator {
                private readonly DbReferenceCollection.CollectionEntry[] _items;
                private readonly int _filterTag; 
                private int _current;
 
                internal DbFilteredReferenceCollectionedEnumerator(DbReferenceCollection.CollectionEntry[] items, int filterTag) { 
                    _items = items;
                    _filterTag = filterTag; 
                    _current = -1;
                }

                object IEnumerator.Current { 
                    get {
                        return _items[_current].Target; 
                    } 
                }
 
                bool IEnumerator.MoveNext() {
                    while (++_current < _items.Length) {
                        if (_items[_current].InUse) {
                            if (_items[_current].Tag == _filterTag) { 
                                return true;
                            } 
                        } 
                        else {
                            break; 
                        }
                    }
                    return false;
                } 

                void IEnumerator.Reset() { 
                    _current = -1; 
                }
            } 
        }
#endif
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbReferenceCollection.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.ProviderBase {
 
    using System;
    using System.Collections;
    using System.Diagnostics;
 
    internal abstract class DbReferenceCollection {
 
#if ORACLE 
        abstract public void Add(object value, int tag);
        abstract public void Notify(int message); 
        abstract public void Remove(object value);
#else

        private struct CollectionEntry { 

            private int _tag; // information about the reference 
            private WeakReference _weak; // the reference itself. 

            public bool HasTarget { 
                get {
                    Debug.Assert((0 != _tag && null != _weak) || (null == _weak || !_weak.IsAlive), "0 tag with Target");
                    return ((0 != _tag) && null != _weak && _weak.IsAlive);
                } 
            }
 
            public bool InUse { 
                get {
                    return (null != _weak); 
                }
            }

            public int Tag { 
                get {
                     return _tag; 
                } 
                set {
                     _tag = value; 
                }
            }

            public object Target { 
                get {
                    if (0 != _tag) { 
                        return _weak.Target; 
                    }
                    Debug.Assert(!_weak.IsAlive, "0 tag with Target"); 
                    return null;
                }
                set {
                    if (null == _weak) { 
                        _weak = new WeakReference(value, false);
                    } 
                    else { 
                        _weak.Target = value;
                    } 
                }
            }
        }
 
        private CollectionEntry[] _items;
 
        protected DbReferenceCollection() { 
            _items = new CollectionEntry[5];
        } 

        abstract public void Add(object value, int tag);

        protected void AddItem(object value, int tag) { 
            Debug.Assert(null != value && 0 != tag, "AddItem with null value or 0 tag");
            CollectionEntry[] items = _items; 
            for (int i = 0; i < items.Length; ++i) { 
                if (!items[i].HasTarget) {
                    items[i].Target = value; 
                    items[i].Tag = tag;
                    Debug.Assert(items[i].HasTarget, "missing expected target");
                    return;
                } 
            }
            int newlength = ((5 == items.Length) ? 15 : (items.Length + 15)); 
            CollectionEntry[] jtems = new CollectionEntry[newlength]; 
            for (int i = 0; i < items.Length; ++i) {
                jtems[i] = items[i]; 
            }
            jtems[items.Length].Target = value;
            jtems[items.Length].Tag = tag;
            Debug.Assert(jtems[items.Length].HasTarget, "missing expected target"); 
            _items = jtems;
        } 
 
        internal IEnumerable Filter(int tag) {
            return new DbFilteredReferenceCollection (_items, tag); 
        }

        public void Notify(int message) {
            CollectionEntry[] items = _items; 
            for (int index = 0; index < items.Length; ++index) {
                if (items[index].InUse) { 
                    object value = items[index].Target; // checks tag & gets target 
                    if (null != value) {
                        Debug.Assert(items[index].HasTarget, "missing expected target"); 
                        if (!NotifyItem(message, items[index].Tag, value)) {
                            items[index].Tag = 0;
                            items[index].Target = null;
                            Debug.Assert(!items[index].HasTarget, "has unexpected target"); 
                        }
                    } 
                    // else (0 == Tag) or !IsAlive 
                }
                else { 
                    Debug.Assert(!items[index].HasTarget, "has unexpected target");
                    break;  // we're done as soon as we find the first null in the list
                }
            } 
        }
 
        abstract protected bool NotifyItem(int message, int tag, object value); 

        public void Purge() { 
            CollectionEntry[] items = _items;
#if DEBUG
            for(int i = 0; i < items.Length; ++i) {
                Debug.Assert(!items[i].HasTarget, "unexpected target during purge"); 
            }
#endif 
            if (100 < items.Length) { 
                _items = new CollectionEntry[5];    // revert back to 5 items if we're really big...
            } 
        }

        abstract public void Remove(object value);
 
        protected void RemoveItem(object value) {
            Debug.Assert(null != value, "RemoveItem with null"); 
            CollectionEntry[] items = _items; 

            for (int index = 0; index < items.Length; ++index) { 
                if (items[index].InUse) {
                    if (value == items[index].Target) { // checks tag & gets target
                        items[index].Tag = 0;
                        items[index].Target = null; 
                        Debug.Assert(!items[index].HasTarget, "has unexpected target");
                        break; 
                    } 
                    // else (0 == Tag) or !IsAlive or (value != Target)
                } 
                else {
                    break;  // we're done as soon as we find the first null in the list
                }
            } 
        }
 
        private struct DbFilteredReferenceCollection : IEnumerable { 
            private readonly DbReferenceCollection.CollectionEntry[] _items;
            private readonly int _filterTag; 

            internal DbFilteredReferenceCollection(DbReferenceCollection.CollectionEntry[] items, int filterTag) {
                _items = items;
                _filterTag = filterTag; 
            }
 
            IEnumerator IEnumerable.GetEnumerator() { 
                return new DbFilteredReferenceCollectionedEnumerator(_items, _filterTag);
            } 

            private struct DbFilteredReferenceCollectionedEnumerator : IEnumerator {
                private readonly DbReferenceCollection.CollectionEntry[] _items;
                private readonly int _filterTag; 
                private int _current;
 
                internal DbFilteredReferenceCollectionedEnumerator(DbReferenceCollection.CollectionEntry[] items, int filterTag) { 
                    _items = items;
                    _filterTag = filterTag; 
                    _current = -1;
                }

                object IEnumerator.Current { 
                    get {
                        return _items[_current].Target; 
                    } 
                }
 
                bool IEnumerator.MoveNext() {
                    while (++_current < _items.Length) {
                        if (_items[_current].InUse) {
                            if (_items[_current].Tag == _filterTag) { 
                                return true;
                            } 
                        } 
                        else {
                            break; 
                        }
                    }
                    return false;
                } 

                void IEnumerator.Reset() { 
                    _current = -1; 
                }
            } 
        }
#endif
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
