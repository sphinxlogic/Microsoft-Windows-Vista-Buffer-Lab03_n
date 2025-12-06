//------------------------------------------------------------------------------ 
// <copyright file="TdsParserSessionPool.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
 
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.ProviderBase; 
    using System.Diagnostics;
    using System.Runtime.CompilerServices; 
    using System.Runtime.InteropServices; 
    using System.Threading;
 
    internal class TdsParserSessionPool {
        // NOTE: This is a very simplistic, lightweight pooler.  It wasn't
        //       intended to handle huge number of items, just to keep track
        //       of the session objects to ensure that they're cleaned up in 
        //       a timely manner, to avoid holding on to an unacceptible
        //       amount of server-side resources in the event that consumers 
        //       let their data readers be GC'd, instead of explicitly 
        //       closing or disposing of them
 
        private const int MaxInactiveCount = 10; // pick something, preferably small...

        private static int              _objectTypeCount; // Bid counter
        private readonly int            _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount); 

        private readonly TdsParser                  _parser;       // parser that owns us 
        private readonly List<TdsParserStateObject> _cache;        // collection of all known sessions 
        private int                                 _cachedCount;  // lock-free _cache.Count
        private TdsParserStateObjectListStack       _freeStack;    // collection of all sessions available for reuse 

        internal TdsParserSessionPool(TdsParser parser) {
            _parser = parser;
            _cache  = new List<TdsParserStateObject>(); 
            _freeStack = new TdsParserStateObjectListStack();
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.TdsParserSessionPool.ctor|ADV> %d# created session pool for parser %d\n", ObjectID, parser.ObjectID); 
            }
        } 

        private bool IsDisposed {
            get {
                return (null == _freeStack); 
            }
        } 
 
        internal int ObjectID {
            get { 
                return _objectID;
            }
        }
 
        internal TdsParserStateObject CreateSession() {
            // NOTE: In the event of a thread abort, we may lose track of 
            //       the session we create here, but since the SNI handle 
            //       contained in it is a SafeHandle, it will eventually
            //       get reclaimed.  I see no reason to go through 
            //       herculean effort to make this section of code a CER,
            //       since the lock below will force an AppDomain unload.

            TdsParserStateObject session = _parser.CreateSession(); 

            // 
 
            lock (_cache) {
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.TdsParserSessionPool.CreateSession|ADV> %d# adding session %d to pool\n", ObjectID, session.ObjectID);
                }
                _cache.Add(session);
                _cachedCount = _cache.Count; 
            }
            return session; 
        } 

        internal void Deactivate() { 
            // When being deactivated, we check all the sessions in the
            // cache to make sure they're cleaned up and then we dispose of
            // sessions that are past what we want to keep around.
 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.TdsParserSessionPool.Deactivate|ADV> %d# deactivating cachedCount=%d\n", ObjectID, _cachedCount); 
 
            try {
                lock(_cache) { 
                    // NOTE: The PutSession call below may choose to remove the
                    //       session from the cache, which will throw off our
                    //       enumerator.  We avoid that by simply indexing backward
                    //       through the array. 

                    for (int i = _cache.Count - 1; i >= 0 ; i--) { 
                        TdsParserStateObject session = _cache[i]; 

                        if (null != session) { 
                            if (session.IsOrphaned) {
                                //

                                if (Bid.AdvancedOn) { 
                                    Bid.Trace("<sc.TdsParserSessionPool.Deactivate|ADV> %d# reclaiming session %d\n", ObjectID, session.ObjectID);
                                } 
 
                                PutSession(session);
                            } 
                        }
                    }
                    //
 
                }
            } 
            finally { 
                Bid.ScopeLeave(ref hscp);
            } 
        }

        internal void Dispose() {
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.TdsParserSessionPool.Dispose|ADV> %d# disposing cachedCount=%d\n", ObjectID, _cachedCount);
            } 
 
            _freeStack = null;  // shutting down, dude
 
            lock(_cache) {
                for (int i = 0; i < _cache.Count; i++) {
                    TdsParserStateObject session = _cache[i];
 
                    if (null != session) {
                        session.Dispose(); 
                    } 
                }
                _cache.Clear(); 
            }
        }

        internal TdsParserStateObject GetSession(object owner) { 
            TdsParserStateObject session = _freeStack.SynchronizedPop();
 
            if (null == session) { 
                session = CreateSession();
            } 
            session.Activate(owner);

            if (Bid.AdvancedOn) {
                Bid.Trace("<sc.TdsParserSessionPool.GetSession|ADV> %d# using session %d\n", ObjectID, session.ObjectID); 
            }
 
            return session; 
        }
 
        internal void PutSession(TdsParserStateObject session) {
            Debug.Assert (null != session, "null session?");
            //Debug.Assert(null != session.Owner, "session without owner?");
 
            bool okToReuse = session.Deactivate();
 
            if (!IsDisposed) { 
                if (okToReuse && _cachedCount < MaxInactiveCount) {
                    if (Bid.AdvancedOn) { 
                        Bid.Trace("<sc.TdsParserSessionPool.PutSession|ADV> %d# keeping session %d cachedCount=%d\n", ObjectID, session.ObjectID, _cachedCount);
                    }
                    Debug.Assert(!session._pendingData, "pending data on a pooled session?");
                    _freeStack.SynchronizedPush(session); 
                }
                else { 
                    if (Bid.AdvancedOn) { 
                        Bid.Trace("<sc.TdsParserSessionPool.PutSession|ADV> %d# disposing session %d cachedCount=%d\n", ObjectID, session.ObjectID, _cachedCount);
                    } 

                    lock (_cache) {
                        bool removed = _cache.Remove(session);
                        Debug.Assert(removed, "session not in pool?"); 
                        _cachedCount = _cache.Count;
                    } 
                    session.Dispose(); 
                }
            } 
        }

        internal string TraceString() {
            return String.Format(/*IFormatProvider*/ null, 
                        "(ObjID={0}, free={1}, cached={2}, total={3})",
                        _objectID, 
                        null == _freeStack ? "(null)" : _freeStack.CountDebugOnly.ToString((IFormatProvider) null), 
                        _cachedCount,
                        _cache.Count); 
        }

        private class TdsParserStateObjectListStack {
            private TdsParserStateObject _stack; 
#if DEBUG
            private int _version; 
            private int _count; 
#endif
 
            internal int CountDebugOnly {
                get {
#if DEBUG
                    return _count; 
#else
                    return -1; 
#endif 
                }
            } 

            internal TdsParserStateObjectListStack() {
            }
 
            internal TdsParserStateObject SynchronizedPop() {
                TdsParserStateObject value; 
                lock(this) { 
                    value = _stack;
                    if (null != value) { 
                        _stack = value.NextPooledObject;
                        value.NextPooledObject = null;
#if DEBUG
                        _version++; 
                        _count--;
#endif 
                    } 
#if DEBUG
                    Debug.Assert((null != value || 0 == _count) && (0 <= _count), "broken SynchronizedPop"); 
#endif
                }
                return value;
            } 

            internal void SynchronizedPush(TdsParserStateObject value) { 
                Debug.Assert(null != value, "pushing null value"); 
                lock(this) {
#if DEBUG 
                    Debug.Assert(null == value.NextPooledObject, "pushing value with non-null NextPooledObject");
                    int index = 0;
                    for(TdsParserStateObject x = _stack; null != x; x = x.NextPooledObject, ++index) {
                        Debug.Assert(x != value, "double push: object already in stack"); 
                    }
                    Debug.Assert(_count == index, "SynchronizedPush count is corrupt"); 
#endif 
                    value.NextPooledObject = _stack;
                    _stack = value; 
#if DEBUG
                    _version++;
                    _count++;
#endif 
                }
            } 
        } 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TdsParserSessionPool.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
 
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.ProviderBase; 
    using System.Diagnostics;
    using System.Runtime.CompilerServices; 
    using System.Runtime.InteropServices; 
    using System.Threading;
 
    internal class TdsParserSessionPool {
        // NOTE: This is a very simplistic, lightweight pooler.  It wasn't
        //       intended to handle huge number of items, just to keep track
        //       of the session objects to ensure that they're cleaned up in 
        //       a timely manner, to avoid holding on to an unacceptible
        //       amount of server-side resources in the event that consumers 
        //       let their data readers be GC'd, instead of explicitly 
        //       closing or disposing of them
 
        private const int MaxInactiveCount = 10; // pick something, preferably small...

        private static int              _objectTypeCount; // Bid counter
        private readonly int            _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount); 

        private readonly TdsParser                  _parser;       // parser that owns us 
        private readonly List<TdsParserStateObject> _cache;        // collection of all known sessions 
        private int                                 _cachedCount;  // lock-free _cache.Count
        private TdsParserStateObjectListStack       _freeStack;    // collection of all sessions available for reuse 

        internal TdsParserSessionPool(TdsParser parser) {
            _parser = parser;
            _cache  = new List<TdsParserStateObject>(); 
            _freeStack = new TdsParserStateObjectListStack();
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.TdsParserSessionPool.ctor|ADV> %d# created session pool for parser %d\n", ObjectID, parser.ObjectID); 
            }
        } 

        private bool IsDisposed {
            get {
                return (null == _freeStack); 
            }
        } 
 
        internal int ObjectID {
            get { 
                return _objectID;
            }
        }
 
        internal TdsParserStateObject CreateSession() {
            // NOTE: In the event of a thread abort, we may lose track of 
            //       the session we create here, but since the SNI handle 
            //       contained in it is a SafeHandle, it will eventually
            //       get reclaimed.  I see no reason to go through 
            //       herculean effort to make this section of code a CER,
            //       since the lock below will force an AppDomain unload.

            TdsParserStateObject session = _parser.CreateSession(); 

            // 
 
            lock (_cache) {
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.TdsParserSessionPool.CreateSession|ADV> %d# adding session %d to pool\n", ObjectID, session.ObjectID);
                }
                _cache.Add(session);
                _cachedCount = _cache.Count; 
            }
            return session; 
        } 

        internal void Deactivate() { 
            // When being deactivated, we check all the sessions in the
            // cache to make sure they're cleaned up and then we dispose of
            // sessions that are past what we want to keep around.
 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.TdsParserSessionPool.Deactivate|ADV> %d# deactivating cachedCount=%d\n", ObjectID, _cachedCount); 
 
            try {
                lock(_cache) { 
                    // NOTE: The PutSession call below may choose to remove the
                    //       session from the cache, which will throw off our
                    //       enumerator.  We avoid that by simply indexing backward
                    //       through the array. 

                    for (int i = _cache.Count - 1; i >= 0 ; i--) { 
                        TdsParserStateObject session = _cache[i]; 

                        if (null != session) { 
                            if (session.IsOrphaned) {
                                //

                                if (Bid.AdvancedOn) { 
                                    Bid.Trace("<sc.TdsParserSessionPool.Deactivate|ADV> %d# reclaiming session %d\n", ObjectID, session.ObjectID);
                                } 
 
                                PutSession(session);
                            } 
                        }
                    }
                    //
 
                }
            } 
            finally { 
                Bid.ScopeLeave(ref hscp);
            } 
        }

        internal void Dispose() {
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.TdsParserSessionPool.Dispose|ADV> %d# disposing cachedCount=%d\n", ObjectID, _cachedCount);
            } 
 
            _freeStack = null;  // shutting down, dude
 
            lock(_cache) {
                for (int i = 0; i < _cache.Count; i++) {
                    TdsParserStateObject session = _cache[i];
 
                    if (null != session) {
                        session.Dispose(); 
                    } 
                }
                _cache.Clear(); 
            }
        }

        internal TdsParserStateObject GetSession(object owner) { 
            TdsParserStateObject session = _freeStack.SynchronizedPop();
 
            if (null == session) { 
                session = CreateSession();
            } 
            session.Activate(owner);

            if (Bid.AdvancedOn) {
                Bid.Trace("<sc.TdsParserSessionPool.GetSession|ADV> %d# using session %d\n", ObjectID, session.ObjectID); 
            }
 
            return session; 
        }
 
        internal void PutSession(TdsParserStateObject session) {
            Debug.Assert (null != session, "null session?");
            //Debug.Assert(null != session.Owner, "session without owner?");
 
            bool okToReuse = session.Deactivate();
 
            if (!IsDisposed) { 
                if (okToReuse && _cachedCount < MaxInactiveCount) {
                    if (Bid.AdvancedOn) { 
                        Bid.Trace("<sc.TdsParserSessionPool.PutSession|ADV> %d# keeping session %d cachedCount=%d\n", ObjectID, session.ObjectID, _cachedCount);
                    }
                    Debug.Assert(!session._pendingData, "pending data on a pooled session?");
                    _freeStack.SynchronizedPush(session); 
                }
                else { 
                    if (Bid.AdvancedOn) { 
                        Bid.Trace("<sc.TdsParserSessionPool.PutSession|ADV> %d# disposing session %d cachedCount=%d\n", ObjectID, session.ObjectID, _cachedCount);
                    } 

                    lock (_cache) {
                        bool removed = _cache.Remove(session);
                        Debug.Assert(removed, "session not in pool?"); 
                        _cachedCount = _cache.Count;
                    } 
                    session.Dispose(); 
                }
            } 
        }

        internal string TraceString() {
            return String.Format(/*IFormatProvider*/ null, 
                        "(ObjID={0}, free={1}, cached={2}, total={3})",
                        _objectID, 
                        null == _freeStack ? "(null)" : _freeStack.CountDebugOnly.ToString((IFormatProvider) null), 
                        _cachedCount,
                        _cache.Count); 
        }

        private class TdsParserStateObjectListStack {
            private TdsParserStateObject _stack; 
#if DEBUG
            private int _version; 
            private int _count; 
#endif
 
            internal int CountDebugOnly {
                get {
#if DEBUG
                    return _count; 
#else
                    return -1; 
#endif 
                }
            } 

            internal TdsParserStateObjectListStack() {
            }
 
            internal TdsParserStateObject SynchronizedPop() {
                TdsParserStateObject value; 
                lock(this) { 
                    value = _stack;
                    if (null != value) { 
                        _stack = value.NextPooledObject;
                        value.NextPooledObject = null;
#if DEBUG
                        _version++; 
                        _count--;
#endif 
                    } 
#if DEBUG
                    Debug.Assert((null != value || 0 == _count) && (0 <= _count), "broken SynchronizedPop"); 
#endif
                }
                return value;
            } 

            internal void SynchronizedPush(TdsParserStateObject value) { 
                Debug.Assert(null != value, "pushing null value"); 
                lock(this) {
#if DEBUG 
                    Debug.Assert(null == value.NextPooledObject, "pushing value with non-null NextPooledObject");
                    int index = 0;
                    for(TdsParserStateObject x = _stack; null != x; x = x.NextPooledObject, ++index) {
                        Debug.Assert(x != value, "double push: object already in stack"); 
                    }
                    Debug.Assert(_count == index, "SynchronizedPush count is corrupt"); 
#endif 
                    value.NextPooledObject = _stack;
                    _stack = value; 
#if DEBUG
                    _version++;
                    _count++;
#endif 
                }
            } 
        } 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
