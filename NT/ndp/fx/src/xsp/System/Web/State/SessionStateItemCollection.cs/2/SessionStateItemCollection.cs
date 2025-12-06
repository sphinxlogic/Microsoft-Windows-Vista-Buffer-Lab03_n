//------------------------------------------------------------------------------ 
// <copyright file="SessionStateItemCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * SessionStateItemCollection 
 *
 * Copyright (c) 1998-1999, Microsoft Corporation 
 *
 */

namespace System.Web.SessionState { 

    using System.IO; 
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Web.Util; 
    using System.Security;
    using System.Security.Permissions;

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface ISessionStateItemCollection : ICollection { 
 
        Object this[String name]
        { 
            get;
            set;
        }
 
        Object this[int index]
        { 
            get; 
            set;
        } 

        void Remove(String name);

        void RemoveAt(int index); 

        void Clear(); 
 
        NameObjectCollectionBase.KeysCollection Keys {
            get; 
        }

        bool Dirty {
            get; 
            set;
        } 
    } 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class SessionStateItemCollection : NameObjectCollectionBase, ISessionStateItemCollection {

        class KeyedCollection : NameObjectCollectionBase {
 
            internal KeyedCollection(int count) : base(count, Misc.CaseInsensitiveInvariantKeyComparer) {
            } 
 
            internal Object this[String name]
            { 
                get {
                    return BaseGet(name);
                }
 
                set {
                    Object oldValue = BaseGet(name); 
                    if (oldValue == null && value == null) 
                        return;
 
                    BaseSet(name, value);
                }
            }
 
            internal Object this[int index]
            { 
                get { 
                    return BaseGet(index);
                } 

                set {
                    Object oldValue = BaseGet(index);
 
                    // We don't expect null value
                    Debug.Assert(value != null); 
 
                    BaseSet(index, value);
                } 
            }

            internal void Remove(String name) {
                BaseRemove(name); 
            }
 
            internal void RemoveAt(int index) { 
                BaseRemoveAt(index);
            } 

            internal void Clear() {
                BaseClear();
            } 

            internal string GetKey(  int index) { 
                return BaseGetKey(index); 
            }
 
            internal bool ContainsKey(string name) {
                // Please note that we don't expect null value to be inserted.
                return (BaseGet(name) != null);
            } 
        }
 
        static Hashtable s_immutableTypes; 
        const int       NO_NULL_KEY = -1;
        const int       SIZE_OF_INT32 = 4; 
        bool            _dirty;
        KeyedCollection _serializedItems;
        Stream          _stream;
        int             _iLastOffset; 
        object          _serializedItemsLock = new object();
 
        public SessionStateItemCollection() : base(Misc.CaseInsensitiveInvariantKeyComparer) { 
        }
 
        static SessionStateItemCollection() {
            Type t;
            s_immutableTypes = new Hashtable(19);
 
            t=typeof(String);
            s_immutableTypes.Add(t, t); 
            t=typeof(Int32); 
            s_immutableTypes.Add(t, t);
            t=typeof(Boolean); 
            s_immutableTypes.Add(t, t);
            t=typeof(DateTime);
            s_immutableTypes.Add(t, t);
            t=typeof(Decimal); 
            s_immutableTypes.Add(t, t);
            t=typeof(Byte); 
            s_immutableTypes.Add(t, t); 
            t=typeof(Char);
            s_immutableTypes.Add(t, t); 
            t=typeof(Single);
            s_immutableTypes.Add(t, t);
            t=typeof(Double);
            s_immutableTypes.Add(t, t); 
            t=typeof(SByte);
            s_immutableTypes.Add(t, t); 
            t=typeof(Int16); 
            s_immutableTypes.Add(t, t);
            t=typeof(Int64); 
            s_immutableTypes.Add(t, t);
            t=typeof(UInt16);
            s_immutableTypes.Add(t, t);
            t=typeof(UInt32); 
            s_immutableTypes.Add(t, t);
            t=typeof(UInt64); 
            s_immutableTypes.Add(t, t); 
            t=typeof(TimeSpan);
            s_immutableTypes.Add(t, t); 
            t=typeof(Guid);
            s_immutableTypes.Add(t, t);
            t=typeof(IntPtr);
            s_immutableTypes.Add(t, t); 
            t=typeof(UIntPtr);
            s_immutableTypes.Add(t, t); 
        } 

        static internal bool IsImmutable(Object o) { 
            return s_immutableTypes[o.GetType()] != null;
        }

        internal void DeserializeAllItems() { 
            if (_serializedItems == null) {
                return; 
            } 

            lock (_serializedItemsLock) { 
                for (int i = 0; i < _serializedItems.Count; i++) {
                    DeserializeItem(_serializedItems.GetKey(i), false);
                }
            } 
        }
 
        void DeserializeItem(int index) { 
            // No-op if SessionStateItemCollection is not deserialized from a persistent storage.
            if (_serializedItems == null) { 
                return;
            }

#if DBG 
            // The keys in _serializedItems should match the beginning part of
            // the list in NameObjectCollectionBase 
            for (int i=0; i < _serializedItems.Count; i++) { 
                Debug.Assert(_serializedItems.GetKey(i) == BaseGetKey(i));
            } 
#endif

            lock (_serializedItemsLock) {
                // No-op if the item isn't serialized. 
                if (index >= _serializedItems.Count) {
                    return; 
                } 

                DeserializeItem(_serializedItems.GetKey(index), false); 
            }
        }

        [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)] 
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.SerializationFormatter)]
        private object ReadValueFromStreamWithAssert() { 
            return AltSerialization.ReadValueFromStream(new BinaryReader(_stream)); 
        }
 
        void DeserializeItem(String name, bool check) {
            int             offset;
            object          val;
 
            lock (_serializedItemsLock) {
                if (check) { 
                    // No-op if SessionStateItemCollection is not deserialized from a persistent storage, 
                    if (_serializedItems == null) {
                        return; 
                    }

                    // User is asking for an item we don't have.
                    if (!_serializedItems.ContainsKey(name)) { 
                        return;
                    } 
                } 

                Debug.Assert(_serializedItems != null); 
                Debug.Assert(_stream != null);

                offset = (int)_serializedItems[name];
                if (offset < 0) { 
                    // It has been deserialized already.
                    return; 
                } 

                // Position the stream to the place where the item is stored. 
                _stream.Seek(offset, SeekOrigin.Begin);

                // Set the value
                Debug.Trace("SessionStateItemCollection", "Deserialized an item: keyname=" + name); 

                // VSWhidbey 427316: Sandbox Serialization in non full trust cases 
                if (HttpRuntime.NamedPermissionSet != null && HttpRuntime.ProcessRequestInApplicationTrust) { 
                    HttpRuntime.NamedPermissionSet.PermitOnly();
                } 

                // This deserialization work used to be done in AcquireRequestState event when
                // there is no user code on the stack.
                // In whidbey we added this on-demand deserialization for performance reason.  However, 
                // in medium and low trust cases the page doesn't have permission to do it.
                // So we have to assert the permission. 
                // (See VSWhidbey 275003) 
                val = ReadValueFromStreamWithAssert();
 
                BaseSet(name, val);

                // At the end, mark the item as deserialized by making it -ve.
                // If the offset is zero, we will use -ve Int32.MaxValue to mark it 
                // as deserialized.
                if (offset == 0) { 
                    _serializedItems[name] = Int32.MaxValue * -1; 
                }
                else if (offset > 0) { 
                    _serializedItems[name] = offset * -1;
                }
                else {
                    Debug.Fail("offset should never be zero within locked deserializeItem!"); 
                }
            } 
 
        }
 
        void MarkItemDeserialized(String name) {
            // No-op if SessionStateItemCollection is not deserialized from a persistent storage,
            if (_serializedItems == null) {
                return; 
            }
 
            lock (_serializedItemsLock) { 
                // If the serialized collection contains this key,
                // set it as deserialized by making it -ve. 
                if (_serializedItems.ContainsKey(name)) {
                    int offset = (int)_serializedItems[name];

                    // Mark the item as deserialized by making it -ve. 
                    // If the offset is zero, we will use -ve Int32.MaxValue to mark it
                    // as deserialized. 
 
                    if (offset == 0) {
                        _serializedItems[name] = Int32.MaxValue * -1; 
                    }
                    else if (offset > 0) {
                        _serializedItems[name] = offset * -1;
                    } 
                }
            } 
        } 

        void MarkItemDeserialized(int index) { 
            // No-op if SessionStateItemCollection is not deserialized from a persistent storage,
            if (_serializedItems == null) {
                return;
            } 

#if DBG 
            // The keys in _serializedItems should match the beginning part of 
            // the list in NameObjectCollectionBase
            for (int i=0; i < _serializedItems.Count; i++) { 
                Debug.Assert(_serializedItems.GetKey(i) == BaseGetKey(i));
            }
#endif
 
            // No-op if the item isn't serialized.
            if (index >= _serializedItems.Count) { 
                return; 
            }
 
            lock (_serializedItemsLock) {
                int offset = (int)_serializedItems[index];

                // Mark the item as deserialized by making it -ve. 
                // If the offset is zero, we will use -ve Int32.MaxValue to mark it
                // as deserialized. 
 
                if (offset == 0) {
                    _serializedItems[index] = Int32.MaxValue * -1; 
                }
                else if (offset > 0) {
                    _serializedItems[index] = offset * -1;
                } 
            }
        } 
 
        public bool Dirty {
            get {return _dirty;} 
            set {_dirty = value;}
        }

        public Object this[String name] 
        {
            get { 
                DeserializeItem(name, true); 

                Object obj = BaseGet(name); 
                if (obj != null) {
                    if (!IsImmutable(obj)) {
                        // If the item is immutable (e.g. an array), then the caller has the ability to change
                        // its content without calling our setter.  So we have to mark the collection 
                        // as dirty.
                        Debug.Trace("SessionStateItemCollection", "Setting _dirty to true in get"); 
                        _dirty = true; 
                    }
                } 

                return obj;
            }
 
            set {
                MarkItemDeserialized(name); 
                Debug.Trace("SessionStateItemCollection", "Setting _dirty to true in set"); 
                BaseSet(name, value);
                _dirty = true; 
            }
        }

        public Object this[int index] 
        {
            get { 
                DeserializeItem(index); 

                Object obj = BaseGet(index); 
                if (obj != null) {
                    if (!IsImmutable(obj)) {
                        Debug.Trace("SessionStateItemCollection", "Setting _dirty to true in get");
                        _dirty = true; 
                    }
                } 
 
                return obj;
            } 

            set {
                MarkItemDeserialized(index);
                Debug.Trace("SessionStateItemCollection", "Setting _dirty to true in set"); 
                BaseSet(index, value);
                _dirty = true; 
            } 
        }
 

        public void Remove(String name) {
            lock (_serializedItemsLock) {
                if (_serializedItems != null) { 
                    _serializedItems.Remove(name);
                } 
 
                BaseRemove(name);
                _dirty = true; 
            }
        }

        public void RemoveAt(int index) { 
            lock (_serializedItemsLock) {
                if (_serializedItems != null && index < _serializedItems.Count) { 
                    _serializedItems.RemoveAt(index); 
                }
 
                BaseRemoveAt(index);
                _dirty = true;
            }
        } 

        public void Clear() { 
            lock (_serializedItemsLock) { 
                if (_serializedItems != null) {
                    _serializedItems.Clear(); 
                }
                BaseClear();
                _dirty = true;
            } 
        }
 
        public override IEnumerator GetEnumerator() { 
            // Have to deserialize all items; otherwise the enumerator won't
            // work because we'll keep on changing the collection during 
            // individual item deserialization
            DeserializeAllItems();

            return base.GetEnumerator(); 
        }
 
        public override NameObjectCollectionBase.KeysCollection Keys { 
            get {
                // Unfortunately, we have to deserialize all items first, because 
                // Keys.GetEnumerator might be called and we have the same problem
                // as in GetEnumerator() above.
                DeserializeAllItems();
 
                return base.Keys;
            } 
        } 

        [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)] 
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.SerializationFormatter)]
        private void WriteValueToStreamWithAssert(object value, BinaryWriter writer) {
            AltSerialization.WriteValueToStream(value, writer);
        } 

        public void Serialize(BinaryWriter writer) { 
            int     count; 
            int     i;
            long    iOffsetStart; 
            long    iValueStart;
            string  key;
            object  value;
            long    curPos; 
            byte[]  buffer = null;
            Stream  baseStream = writer.BaseStream; 
 
            // VSWhidbey 427316: Sandbox Serialization in non full trust cases
            if (HttpRuntime.NamedPermissionSet != null && HttpRuntime.ProcessRequestInApplicationTrust) { 
                HttpRuntime.NamedPermissionSet.PermitOnly();
            }

            lock (_serializedItemsLock) { 
                count = Count;
                writer.Write(count); 
 
                if (count > 0) {
                    if (BaseGet(null) != null) { 
                        // We have a value with a null key.  Find its index.
                        for (i = 0; i < count; i++) {
                            key = BaseGetKey(i);
                            if (key == null) { 
                                writer.Write(i);
                                break; 
                            } 
                        }
 
                        Debug.Assert(i != count);
                    }
                    else {
                        writer.Write(NO_NULL_KEY); 
                    }
 
                    // Write out all the keys. 
                    for (i = 0; i < count; i++) {
                        key = BaseGetKey(i); 
                        if (key != null) {
                            writer.Write(key);
                        }
                    } 

                    // Next, allocate space to store the offset: 
                    // - We won't store the offset of first item because it's always zero. 
                    // - The offset of an item is counted from the beginning of serialized values
                    // - But we will store the offset of the first byte off the last item because 
                    //   we need that to calculate the size of the last item.
                    iOffsetStart = baseStream.Position;
                    baseStream.Seek(SIZE_OF_INT32 * count, SeekOrigin.Current);
 
                    iValueStart = baseStream.Position;
 
                    for (i = 0; i < count; i++) { 
                        // See if that item has not be deserialized yet.
                        if (_serializedItems != null && 
                            i < _serializedItems.Count &&
                            (int)_serializedItems[i] >= 0) {

                            int dataLength; 
                            int offset1, offset2;
 
                            Debug.Assert(_stream != null); 

                            // The item is read as serialized data from a store, and it's still 
                            // serialized, meaning no one has referenced it.  Just copy
                            // the bytes over.

                            // The length of the item is the difference between its offset 
                            // and next item offset, which are stored at position i and i+1 respectively,
                            // except for the last items. 
                            offset1 = (int)_serializedItems[i]; 

                            if (i == _serializedItems.Count - 1) { 
                                offset2 = _iLastOffset;
                            }
                            else {
                                offset2 = Math.Abs((int)_serializedItems[i + 1]); 
                            }
 
                            dataLength = offset2 - offset1; 

                            // Move the stream to the serialized data and copy it over to writer 
                            _stream.Seek(offset1, SeekOrigin.Begin);

                            if (buffer == null || buffer.Length < dataLength) {
                                buffer = new Byte[dataLength]; 
                            }
 
#if DBG 
                            int read =
#endif 
                            _stream.Read(buffer, 0, dataLength);
#if DBG
                            Debug.Assert(read == dataLength);
#endif 

                            baseStream.Write(buffer, 0, dataLength); 
                        } 
                        else {
                            value = BaseGet(i); 
                            WriteValueToStreamWithAssert(value, writer);
                        }

                        curPos = baseStream.Position; 

                        // Write the offset 
                        baseStream.Seek(i * SIZE_OF_INT32 + iOffsetStart, SeekOrigin.Begin); 
                        writer.Write((int)(curPos - iValueStart));
 
                        // Move back to current position
                        baseStream.Seek(curPos, SeekOrigin.Begin);

                        Debug.Trace("SessionStateItemCollection", 
                            "Serialize: curPost=" + curPos + ", offset= " + (int)(curPos - iValueStart));
                    } 
                } 
#if DBG
                writer.Write((byte)0xff); 
#endif
            }
        }
 
        public static SessionStateItemCollection Deserialize(BinaryReader reader) {
            SessionStateItemCollection   d = new SessionStateItemCollection(); 
            int                 count; 
            int                 nullKey;
            String              key; 
            int                 i;
            byte[]              buffer;

            count = reader.ReadInt32(); 

            if (count > 0) { 
                nullKey = reader.ReadInt32(); 

                d._serializedItems = new KeyedCollection(count); 

                // First, deserialize all the keys
                for (i = 0; i < count; i++) {
                    if (i == nullKey) { 
                        key = null;
                    } 
                    else { 
                        key = reader.ReadString();
                    } 

                    // Need to set them with null value first, so that
                    // the order of them items is correct.
                    d.BaseSet(key, null); 
                }
 
                // Next, deserialize all the offsets 
                for (i = 0; i < count; i++) {
                    if (i == 0) { 
                        d._serializedItems[d.BaseGetKey(i)] = 0;
                    }
                    else {
                        d._serializedItems[d.BaseGetKey(i)] = reader.ReadInt32(); 
                    }
                } 
 
                //
                d._iLastOffset = reader.ReadInt32(); 

                Debug.Trace("SessionStateItemCollection",
                    "Deserialize: _iLastOffset= " + d._iLastOffset);
 
                // _iLastOffset is the first byte past the last item, which equals
                // the total length of all serialized data 
                buffer = new byte[d._iLastOffset]; 
                int bytesRead = reader.BaseStream.Read(buffer, 0, d._iLastOffset);
                if (bytesRead != d._iLastOffset) { 
                    throw new HttpException(SR.GetString(SR.Invalid_session_state));
                }
                d._stream = new MemoryStream(buffer);
            } 

    #if DBG 
            Debug.Assert(reader.ReadByte() == 0xff); 
    #endif
 
            d._dirty = false;

            return d;
        } 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="SessionStateItemCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * SessionStateItemCollection 
 *
 * Copyright (c) 1998-1999, Microsoft Corporation 
 *
 */

namespace System.Web.SessionState { 

    using System.IO; 
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Web.Util; 
    using System.Security;
    using System.Security.Permissions;

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface ISessionStateItemCollection : ICollection { 
 
        Object this[String name]
        { 
            get;
            set;
        }
 
        Object this[int index]
        { 
            get; 
            set;
        } 

        void Remove(String name);

        void RemoveAt(int index); 

        void Clear(); 
 
        NameObjectCollectionBase.KeysCollection Keys {
            get; 
        }

        bool Dirty {
            get; 
            set;
        } 
    } 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class SessionStateItemCollection : NameObjectCollectionBase, ISessionStateItemCollection {

        class KeyedCollection : NameObjectCollectionBase {
 
            internal KeyedCollection(int count) : base(count, Misc.CaseInsensitiveInvariantKeyComparer) {
            } 
 
            internal Object this[String name]
            { 
                get {
                    return BaseGet(name);
                }
 
                set {
                    Object oldValue = BaseGet(name); 
                    if (oldValue == null && value == null) 
                        return;
 
                    BaseSet(name, value);
                }
            }
 
            internal Object this[int index]
            { 
                get { 
                    return BaseGet(index);
                } 

                set {
                    Object oldValue = BaseGet(index);
 
                    // We don't expect null value
                    Debug.Assert(value != null); 
 
                    BaseSet(index, value);
                } 
            }

            internal void Remove(String name) {
                BaseRemove(name); 
            }
 
            internal void RemoveAt(int index) { 
                BaseRemoveAt(index);
            } 

            internal void Clear() {
                BaseClear();
            } 

            internal string GetKey(  int index) { 
                return BaseGetKey(index); 
            }
 
            internal bool ContainsKey(string name) {
                // Please note that we don't expect null value to be inserted.
                return (BaseGet(name) != null);
            } 
        }
 
        static Hashtable s_immutableTypes; 
        const int       NO_NULL_KEY = -1;
        const int       SIZE_OF_INT32 = 4; 
        bool            _dirty;
        KeyedCollection _serializedItems;
        Stream          _stream;
        int             _iLastOffset; 
        object          _serializedItemsLock = new object();
 
        public SessionStateItemCollection() : base(Misc.CaseInsensitiveInvariantKeyComparer) { 
        }
 
        static SessionStateItemCollection() {
            Type t;
            s_immutableTypes = new Hashtable(19);
 
            t=typeof(String);
            s_immutableTypes.Add(t, t); 
            t=typeof(Int32); 
            s_immutableTypes.Add(t, t);
            t=typeof(Boolean); 
            s_immutableTypes.Add(t, t);
            t=typeof(DateTime);
            s_immutableTypes.Add(t, t);
            t=typeof(Decimal); 
            s_immutableTypes.Add(t, t);
            t=typeof(Byte); 
            s_immutableTypes.Add(t, t); 
            t=typeof(Char);
            s_immutableTypes.Add(t, t); 
            t=typeof(Single);
            s_immutableTypes.Add(t, t);
            t=typeof(Double);
            s_immutableTypes.Add(t, t); 
            t=typeof(SByte);
            s_immutableTypes.Add(t, t); 
            t=typeof(Int16); 
            s_immutableTypes.Add(t, t);
            t=typeof(Int64); 
            s_immutableTypes.Add(t, t);
            t=typeof(UInt16);
            s_immutableTypes.Add(t, t);
            t=typeof(UInt32); 
            s_immutableTypes.Add(t, t);
            t=typeof(UInt64); 
            s_immutableTypes.Add(t, t); 
            t=typeof(TimeSpan);
            s_immutableTypes.Add(t, t); 
            t=typeof(Guid);
            s_immutableTypes.Add(t, t);
            t=typeof(IntPtr);
            s_immutableTypes.Add(t, t); 
            t=typeof(UIntPtr);
            s_immutableTypes.Add(t, t); 
        } 

        static internal bool IsImmutable(Object o) { 
            return s_immutableTypes[o.GetType()] != null;
        }

        internal void DeserializeAllItems() { 
            if (_serializedItems == null) {
                return; 
            } 

            lock (_serializedItemsLock) { 
                for (int i = 0; i < _serializedItems.Count; i++) {
                    DeserializeItem(_serializedItems.GetKey(i), false);
                }
            } 
        }
 
        void DeserializeItem(int index) { 
            // No-op if SessionStateItemCollection is not deserialized from a persistent storage.
            if (_serializedItems == null) { 
                return;
            }

#if DBG 
            // The keys in _serializedItems should match the beginning part of
            // the list in NameObjectCollectionBase 
            for (int i=0; i < _serializedItems.Count; i++) { 
                Debug.Assert(_serializedItems.GetKey(i) == BaseGetKey(i));
            } 
#endif

            lock (_serializedItemsLock) {
                // No-op if the item isn't serialized. 
                if (index >= _serializedItems.Count) {
                    return; 
                } 

                DeserializeItem(_serializedItems.GetKey(index), false); 
            }
        }

        [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)] 
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.SerializationFormatter)]
        private object ReadValueFromStreamWithAssert() { 
            return AltSerialization.ReadValueFromStream(new BinaryReader(_stream)); 
        }
 
        void DeserializeItem(String name, bool check) {
            int             offset;
            object          val;
 
            lock (_serializedItemsLock) {
                if (check) { 
                    // No-op if SessionStateItemCollection is not deserialized from a persistent storage, 
                    if (_serializedItems == null) {
                        return; 
                    }

                    // User is asking for an item we don't have.
                    if (!_serializedItems.ContainsKey(name)) { 
                        return;
                    } 
                } 

                Debug.Assert(_serializedItems != null); 
                Debug.Assert(_stream != null);

                offset = (int)_serializedItems[name];
                if (offset < 0) { 
                    // It has been deserialized already.
                    return; 
                } 

                // Position the stream to the place where the item is stored. 
                _stream.Seek(offset, SeekOrigin.Begin);

                // Set the value
                Debug.Trace("SessionStateItemCollection", "Deserialized an item: keyname=" + name); 

                // VSWhidbey 427316: Sandbox Serialization in non full trust cases 
                if (HttpRuntime.NamedPermissionSet != null && HttpRuntime.ProcessRequestInApplicationTrust) { 
                    HttpRuntime.NamedPermissionSet.PermitOnly();
                } 

                // This deserialization work used to be done in AcquireRequestState event when
                // there is no user code on the stack.
                // In whidbey we added this on-demand deserialization for performance reason.  However, 
                // in medium and low trust cases the page doesn't have permission to do it.
                // So we have to assert the permission. 
                // (See VSWhidbey 275003) 
                val = ReadValueFromStreamWithAssert();
 
                BaseSet(name, val);

                // At the end, mark the item as deserialized by making it -ve.
                // If the offset is zero, we will use -ve Int32.MaxValue to mark it 
                // as deserialized.
                if (offset == 0) { 
                    _serializedItems[name] = Int32.MaxValue * -1; 
                }
                else if (offset > 0) { 
                    _serializedItems[name] = offset * -1;
                }
                else {
                    Debug.Fail("offset should never be zero within locked deserializeItem!"); 
                }
            } 
 
        }
 
        void MarkItemDeserialized(String name) {
            // No-op if SessionStateItemCollection is not deserialized from a persistent storage,
            if (_serializedItems == null) {
                return; 
            }
 
            lock (_serializedItemsLock) { 
                // If the serialized collection contains this key,
                // set it as deserialized by making it -ve. 
                if (_serializedItems.ContainsKey(name)) {
                    int offset = (int)_serializedItems[name];

                    // Mark the item as deserialized by making it -ve. 
                    // If the offset is zero, we will use -ve Int32.MaxValue to mark it
                    // as deserialized. 
 
                    if (offset == 0) {
                        _serializedItems[name] = Int32.MaxValue * -1; 
                    }
                    else if (offset > 0) {
                        _serializedItems[name] = offset * -1;
                    } 
                }
            } 
        } 

        void MarkItemDeserialized(int index) { 
            // No-op if SessionStateItemCollection is not deserialized from a persistent storage,
            if (_serializedItems == null) {
                return;
            } 

#if DBG 
            // The keys in _serializedItems should match the beginning part of 
            // the list in NameObjectCollectionBase
            for (int i=0; i < _serializedItems.Count; i++) { 
                Debug.Assert(_serializedItems.GetKey(i) == BaseGetKey(i));
            }
#endif
 
            // No-op if the item isn't serialized.
            if (index >= _serializedItems.Count) { 
                return; 
            }
 
            lock (_serializedItemsLock) {
                int offset = (int)_serializedItems[index];

                // Mark the item as deserialized by making it -ve. 
                // If the offset is zero, we will use -ve Int32.MaxValue to mark it
                // as deserialized. 
 
                if (offset == 0) {
                    _serializedItems[index] = Int32.MaxValue * -1; 
                }
                else if (offset > 0) {
                    _serializedItems[index] = offset * -1;
                } 
            }
        } 
 
        public bool Dirty {
            get {return _dirty;} 
            set {_dirty = value;}
        }

        public Object this[String name] 
        {
            get { 
                DeserializeItem(name, true); 

                Object obj = BaseGet(name); 
                if (obj != null) {
                    if (!IsImmutable(obj)) {
                        // If the item is immutable (e.g. an array), then the caller has the ability to change
                        // its content without calling our setter.  So we have to mark the collection 
                        // as dirty.
                        Debug.Trace("SessionStateItemCollection", "Setting _dirty to true in get"); 
                        _dirty = true; 
                    }
                } 

                return obj;
            }
 
            set {
                MarkItemDeserialized(name); 
                Debug.Trace("SessionStateItemCollection", "Setting _dirty to true in set"); 
                BaseSet(name, value);
                _dirty = true; 
            }
        }

        public Object this[int index] 
        {
            get { 
                DeserializeItem(index); 

                Object obj = BaseGet(index); 
                if (obj != null) {
                    if (!IsImmutable(obj)) {
                        Debug.Trace("SessionStateItemCollection", "Setting _dirty to true in get");
                        _dirty = true; 
                    }
                } 
 
                return obj;
            } 

            set {
                MarkItemDeserialized(index);
                Debug.Trace("SessionStateItemCollection", "Setting _dirty to true in set"); 
                BaseSet(index, value);
                _dirty = true; 
            } 
        }
 

        public void Remove(String name) {
            lock (_serializedItemsLock) {
                if (_serializedItems != null) { 
                    _serializedItems.Remove(name);
                } 
 
                BaseRemove(name);
                _dirty = true; 
            }
        }

        public void RemoveAt(int index) { 
            lock (_serializedItemsLock) {
                if (_serializedItems != null && index < _serializedItems.Count) { 
                    _serializedItems.RemoveAt(index); 
                }
 
                BaseRemoveAt(index);
                _dirty = true;
            }
        } 

        public void Clear() { 
            lock (_serializedItemsLock) { 
                if (_serializedItems != null) {
                    _serializedItems.Clear(); 
                }
                BaseClear();
                _dirty = true;
            } 
        }
 
        public override IEnumerator GetEnumerator() { 
            // Have to deserialize all items; otherwise the enumerator won't
            // work because we'll keep on changing the collection during 
            // individual item deserialization
            DeserializeAllItems();

            return base.GetEnumerator(); 
        }
 
        public override NameObjectCollectionBase.KeysCollection Keys { 
            get {
                // Unfortunately, we have to deserialize all items first, because 
                // Keys.GetEnumerator might be called and we have the same problem
                // as in GetEnumerator() above.
                DeserializeAllItems();
 
                return base.Keys;
            } 
        } 

        [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)] 
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.SerializationFormatter)]
        private void WriteValueToStreamWithAssert(object value, BinaryWriter writer) {
            AltSerialization.WriteValueToStream(value, writer);
        } 

        public void Serialize(BinaryWriter writer) { 
            int     count; 
            int     i;
            long    iOffsetStart; 
            long    iValueStart;
            string  key;
            object  value;
            long    curPos; 
            byte[]  buffer = null;
            Stream  baseStream = writer.BaseStream; 
 
            // VSWhidbey 427316: Sandbox Serialization in non full trust cases
            if (HttpRuntime.NamedPermissionSet != null && HttpRuntime.ProcessRequestInApplicationTrust) { 
                HttpRuntime.NamedPermissionSet.PermitOnly();
            }

            lock (_serializedItemsLock) { 
                count = Count;
                writer.Write(count); 
 
                if (count > 0) {
                    if (BaseGet(null) != null) { 
                        // We have a value with a null key.  Find its index.
                        for (i = 0; i < count; i++) {
                            key = BaseGetKey(i);
                            if (key == null) { 
                                writer.Write(i);
                                break; 
                            } 
                        }
 
                        Debug.Assert(i != count);
                    }
                    else {
                        writer.Write(NO_NULL_KEY); 
                    }
 
                    // Write out all the keys. 
                    for (i = 0; i < count; i++) {
                        key = BaseGetKey(i); 
                        if (key != null) {
                            writer.Write(key);
                        }
                    } 

                    // Next, allocate space to store the offset: 
                    // - We won't store the offset of first item because it's always zero. 
                    // - The offset of an item is counted from the beginning of serialized values
                    // - But we will store the offset of the first byte off the last item because 
                    //   we need that to calculate the size of the last item.
                    iOffsetStart = baseStream.Position;
                    baseStream.Seek(SIZE_OF_INT32 * count, SeekOrigin.Current);
 
                    iValueStart = baseStream.Position;
 
                    for (i = 0; i < count; i++) { 
                        // See if that item has not be deserialized yet.
                        if (_serializedItems != null && 
                            i < _serializedItems.Count &&
                            (int)_serializedItems[i] >= 0) {

                            int dataLength; 
                            int offset1, offset2;
 
                            Debug.Assert(_stream != null); 

                            // The item is read as serialized data from a store, and it's still 
                            // serialized, meaning no one has referenced it.  Just copy
                            // the bytes over.

                            // The length of the item is the difference between its offset 
                            // and next item offset, which are stored at position i and i+1 respectively,
                            // except for the last items. 
                            offset1 = (int)_serializedItems[i]; 

                            if (i == _serializedItems.Count - 1) { 
                                offset2 = _iLastOffset;
                            }
                            else {
                                offset2 = Math.Abs((int)_serializedItems[i + 1]); 
                            }
 
                            dataLength = offset2 - offset1; 

                            // Move the stream to the serialized data and copy it over to writer 
                            _stream.Seek(offset1, SeekOrigin.Begin);

                            if (buffer == null || buffer.Length < dataLength) {
                                buffer = new Byte[dataLength]; 
                            }
 
#if DBG 
                            int read =
#endif 
                            _stream.Read(buffer, 0, dataLength);
#if DBG
                            Debug.Assert(read == dataLength);
#endif 

                            baseStream.Write(buffer, 0, dataLength); 
                        } 
                        else {
                            value = BaseGet(i); 
                            WriteValueToStreamWithAssert(value, writer);
                        }

                        curPos = baseStream.Position; 

                        // Write the offset 
                        baseStream.Seek(i * SIZE_OF_INT32 + iOffsetStart, SeekOrigin.Begin); 
                        writer.Write((int)(curPos - iValueStart));
 
                        // Move back to current position
                        baseStream.Seek(curPos, SeekOrigin.Begin);

                        Debug.Trace("SessionStateItemCollection", 
                            "Serialize: curPost=" + curPos + ", offset= " + (int)(curPos - iValueStart));
                    } 
                } 
#if DBG
                writer.Write((byte)0xff); 
#endif
            }
        }
 
        public static SessionStateItemCollection Deserialize(BinaryReader reader) {
            SessionStateItemCollection   d = new SessionStateItemCollection(); 
            int                 count; 
            int                 nullKey;
            String              key; 
            int                 i;
            byte[]              buffer;

            count = reader.ReadInt32(); 

            if (count > 0) { 
                nullKey = reader.ReadInt32(); 

                d._serializedItems = new KeyedCollection(count); 

                // First, deserialize all the keys
                for (i = 0; i < count; i++) {
                    if (i == nullKey) { 
                        key = null;
                    } 
                    else { 
                        key = reader.ReadString();
                    } 

                    // Need to set them with null value first, so that
                    // the order of them items is correct.
                    d.BaseSet(key, null); 
                }
 
                // Next, deserialize all the offsets 
                for (i = 0; i < count; i++) {
                    if (i == 0) { 
                        d._serializedItems[d.BaseGetKey(i)] = 0;
                    }
                    else {
                        d._serializedItems[d.BaseGetKey(i)] = reader.ReadInt32(); 
                    }
                } 
 
                //
                d._iLastOffset = reader.ReadInt32(); 

                Debug.Trace("SessionStateItemCollection",
                    "Deserialize: _iLastOffset= " + d._iLastOffset);
 
                // _iLastOffset is the first byte past the last item, which equals
                // the total length of all serialized data 
                buffer = new byte[d._iLastOffset]; 
                int bytesRead = reader.BaseStream.Read(buffer, 0, d._iLastOffset);
                if (bytesRead != d._iLastOffset) { 
                    throw new HttpException(SR.GetString(SR.Invalid_session_state));
                }
                d._stream = new MemoryStream(buffer);
            } 

    #if DBG 
            Debug.Assert(reader.ReadByte() == 0xff); 
    #endif
 
            d._dirty = false;

            return d;
        } 
    }
} 
