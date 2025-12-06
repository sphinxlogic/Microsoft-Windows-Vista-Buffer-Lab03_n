//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionPoolGroup.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.Data.Common;
#if ORACLE 
    using System.Data.OracleClient;
#endif 
    using System.Diagnostics; 
    using System.Threading;
 
    // set_ConnectionString calls DbConnectionFactory.GetConnectionPoolGroup
    // when not found a new pool entry is created and potentially added
    // DbConnectionPoolGroup starts in the Active state
 
    // Open calls DbConnectionFactory.GetConnectionPool
    // if the existing pool entry is Disabled, GetConnectionPoolGroup is called for a new entry 
    // DbConnectionFactory.GetConnectionPool calls DbConnectionPoolGroup.GetConnectionPool 

    // DbConnectionPoolGroup.GetConnectionPool will return pool for the current identity 
    // or null if identity is restricted or pooling is disabled or state is disabled at time of add
    // state changes are Active->Active, Idle->Active

    // DbConnectionFactory.PruneConnectionPoolGroups calls Prune 
    // which will QueuePoolForRelease on all empty pools
    // and once no pools remain, change state from Active->Idle->Disabled 
    // Once Disabled, factory can remove its reference to the pool entry 

    sealed internal class DbConnectionPoolGroup { 
        private readonly DbConnectionOptions               _connectionOptions;
        private readonly DbConnectionPoolGroupOptions      _poolGroupOptions;
        private HybridDictionary                           _poolCollection;
 
        private          int                               _poolCount;      // number of pools
        private          int                               _state;          // see PoolGroupState* below 
 
        private          DbConnectionPoolGroupProviderInfo _providerInfo;
        private          DbMetaDataFactory                 _metaDataFactory; 

        private static int _objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);
 
        // always lock this before changing _state, we don't want to move out of `the disabled state
        // PoolGroupStateUninitialized = 0; 
        private const int PoolGroupStateActive   = 1; // initial state, GetPoolGroup from cache, connection Open 
        private const int PoolGroupStateIdle     = 2; // all pools are pruned via Clear
        private const int PoolGroupStateDisabled = 4; // factory pool entry prunning method 

        internal DbConnectionPoolGroup (DbConnectionOptions connectionOptions, DbConnectionPoolGroupOptions poolGroupOptions) {
            Debug.Assert(null != connectionOptions, "null connection options");
            Debug.Assert(null == poolGroupOptions || ADP.IsWindowsNT, "should not have pooling options on Win9x"); 

            _connectionOptions = connectionOptions; 
            _poolGroupOptions = poolGroupOptions; 

            // always lock this object before changing state 
            // HybridDictionary does not create any sub-objects until add
            // so it is safe to use for non-pooled connection as long as
            // we check _poolGroupOptions first
            _poolCollection = new HybridDictionary(1, false); 
            _state = PoolGroupStateActive; // VSWhidbey 112102
        } 
 
        internal DbConnectionOptions ConnectionOptions {
            get { 
                return _connectionOptions;
            }
        }
 
        internal int Count {
            get { 
                // NOTE: the use of this property does not indicate activity, 
                // on the pool entry because it only it is only used to identify
                // empty pool entries when we're pruning them. 
                return _poolCount;
            }
        }
 
        internal DbConnectionPoolGroupProviderInfo ProviderInfo {
            get { 
                return _providerInfo; 
            }
            set { 
                _providerInfo = value;
                if(null!=value) {
                    _providerInfo.PoolGroup = this;
                } 
            }
        } 
 
        internal bool IsDisabled {
            get { 
                return (PoolGroupStateDisabled == _state);
            }
        }
 
        internal int ObjectID {
            get { 
                return _objectID; 
            }
        } 

        internal DbConnectionPoolGroupOptions PoolGroupOptions {
            get {
                return _poolGroupOptions; 
            }
        } 
 
        internal DbMetaDataFactory MetaDataFactory{
            get { 
                return  _metaDataFactory;
                }

            set { 
                _metaDataFactory = value;
            } 
        } 

        internal void Clear() { 
            ClearInternal(true);
        }

        private bool ClearInternal(bool clearing) { 
            // must be multi-thread safe with competing calls by Clear and Prune via background thread
            // will return true for Prune on if the pool entry is Disabled or not 
 
            lock (this) {
                HybridDictionary poolCollection = _poolCollection; 
                if (0 < poolCollection.Count) {
                    HybridDictionary newPoolCollection = new HybridDictionary(poolCollection.Count, false);

                    foreach (DictionaryEntry entry in poolCollection) { 
                        if (null != entry.Value) {
                            DbConnectionPool pool = (DbConnectionPool)entry.Value; 
 
                            //
 



 

 
                            // Actually prune the pool if the user requested it (clearing == true) 
                            // or if there are no connections in the pool and no errors occurred.
                            // Empty pool during pruning indicates zero or low activity, but 
                            //  an error state indicates the pool needs to stay around to
                            //  throttle new connection attempts.
                            if (clearing || (!pool.ErrorOccurred && 0 == pool.Count)) {
 
                                // Order is important here.  First we remove the pool
                                // from the collection of pools so no one will try 
                                // to use it while we're processing and finally we put the 
                                // pool into a list of pools to be released when they
                                // are completely empty. 
                                DbConnectionFactory connectionFactory = pool.ConnectionFactory;

                                connectionFactory.PerformanceCounters.NumberOfActiveConnectionPools.Decrement();
                                connectionFactory.QueuePoolForRelease(pool, clearing); 
                            }
                            else { 
                                newPoolCollection.Add(entry.Key, entry.Value); 
                            }
                        } 
                    }
                    _poolCollection = newPoolCollection;
                    _poolCount = newPoolCollection.Count;
                } 

                // must be pruning thread to change state and no connections 
                // otherwise pruning thread risks making entry disabled soon after user calls ClearPool 
                if (!clearing && (0 == _poolCount)) {
                    if (PoolGroupStateActive == _state) { 
                        _state = PoolGroupStateIdle;
                        Bid.Trace("<prov.DbConnectionPoolGroup.ClearInternal|RES|INFO|CPOOL> %d#, Idle\n", ObjectID);
                    }
                    else if (PoolGroupStateIdle == _state) { 
                        _state = PoolGroupStateDisabled;
                        Bid.Trace("<prov.DbConnectionPoolGroup.ReadyToRemove|RES|INFO|CPOOL> %d#, Disabled\n", ObjectID); 
                    } 
                }
                return (PoolGroupStateDisabled == _state); 
            }
        }

        internal DbConnectionPool GetConnectionPool(DbConnectionFactory connectionFactory) { 
            // When this method returns null it indicates that the connection
            // factory should not use pooling. 
 
            // We don't support connection pooling on Win9x; it lacks too
            // many of the APIs we require. 
            // PoolGroupOptions will only be null when we're not supposed to pool
            // connections.
            object pool = null;
            if (null != _poolGroupOptions) { 
                Debug.Assert(ADP.IsWindowsNT, "should not be pooling on Win9x");
 
                DbConnectionPoolIdentity currentIdentity = DbConnectionPoolIdentity.NoIdentity; 
                if (_poolGroupOptions.PoolByIdentity) {
                    // if we're pooling by identity (because integrated security is 
                    // being used for these connections) then we need to go out and
                    // search for the connectionPool that matches the current identity.

                    currentIdentity = DbConnectionPoolIdentity.GetCurrent(); 

                    // If the current token is restricted in some way, then we must 
                    // not attempt to pool these connections. 
                    if (currentIdentity.IsRestricted) {
                        currentIdentity = null; 
                    }
                }
                if (null != currentIdentity) {
                    HybridDictionary poolCollection = _poolCollection; 
                    pool = poolCollection[currentIdentity]; // find the pool
                    if (null == pool) { 
 
                        DbConnectionPoolProviderInfo connectionPoolProviderInfo = connectionFactory.CreateConnectionPoolProviderInfo(this.ConnectionOptions);
 
                        // optimistically create pool, but its callbacks are delayed until after actual add
                        DbConnectionPool newPool = new DbConnectionPool(connectionFactory, this, currentIdentity, connectionPoolProviderInfo);

                        lock (this) { 
                            // Did someone already add it to the list?
                            poolCollection = _poolCollection; 
                            pool = poolCollection[currentIdentity]; // find the pool 

                            if (null == pool) { 
                                if (MarkPoolGroupAsActive()) {
                                    // If we get here, we know for certain that we there isn't
                                    // a pool that matches the current identity, so we have to
                                    // add the optimistically created one 
                                    newPool.Startup(); // must start pool before usage
 
                                    HybridDictionary newPoolCollection = new HybridDictionary(1+poolCollection.Count, false); 
                                    foreach(DictionaryEntry entry in poolCollection) {
                                        newPoolCollection.Add(entry.Key, entry.Value); 
                                    }
                                    newPoolCollection.Add(currentIdentity, newPool);
                                    connectionFactory.PerformanceCounters.NumberOfActiveConnectionPools.Increment();
                                    _poolCollection = newPoolCollection; 
                                    _poolCount = newPoolCollection.Count;
                                    pool = newPool; 
                                    newPool = null; 
                                }
                                else { 
                                    // else pool entry has been disabled so don't create new pools
                                    Debug.Assert(PoolGroupStateDisabled == _state, "state should be disabled");
                                }
                            } 
                            else {
                                // else found an existing pool to use instead 
                                Debug.Assert(PoolGroupStateActive == _state, "state should be active since a pool exists and lock holds"); 
                            }
                        } 

                        if (null != newPool) {
                            // don't need to call connectionFactory.QueuePoolForRelease(newPool) because
                            // pool callbacks were delayed and no risk of connections being created 
                            newPool.Shutdown();
                        } 
                    } 
                    // the found pool could be in any state
                } 
            }

            if (null == pool) {
                lock(this) { 
                    // keep the pool entry state active when not pooling
                    MarkPoolGroupAsActive(); 
                } 
            }
            return (DbConnectionPool)pool; 
        }

        private bool MarkPoolGroupAsActive() {
            // when getting a connection, make the entry active if it was idle (but not disabled) 
            // must always lock this before calling
 
            if (PoolGroupStateIdle == _state) { 
                _state = PoolGroupStateActive;
                Bid.Trace("<prov.DbConnectionPoolGroup.ClearInternal|RES|INFO|CPOOL> %d#, Active\n", ObjectID); 
            }
            return (PoolGroupStateActive == _state);
        }
 
        internal bool Prune() {
            // must only call from DbConnectionFactory.PruneConnectionPoolGroups on background timer thread 
            // must lock(DbConnectionFactory._connectionPoolGroups.SyncRoot) before calling ReadyToRemove 
            //     to avoid conflict with DbConnectionFactory.CreateConnectionPoolGroup replacing pool entry
 
            return ClearInternal(false);
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionPoolGroup.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.Data.Common;
#if ORACLE 
    using System.Data.OracleClient;
#endif 
    using System.Diagnostics; 
    using System.Threading;
 
    // set_ConnectionString calls DbConnectionFactory.GetConnectionPoolGroup
    // when not found a new pool entry is created and potentially added
    // DbConnectionPoolGroup starts in the Active state
 
    // Open calls DbConnectionFactory.GetConnectionPool
    // if the existing pool entry is Disabled, GetConnectionPoolGroup is called for a new entry 
    // DbConnectionFactory.GetConnectionPool calls DbConnectionPoolGroup.GetConnectionPool 

    // DbConnectionPoolGroup.GetConnectionPool will return pool for the current identity 
    // or null if identity is restricted or pooling is disabled or state is disabled at time of add
    // state changes are Active->Active, Idle->Active

    // DbConnectionFactory.PruneConnectionPoolGroups calls Prune 
    // which will QueuePoolForRelease on all empty pools
    // and once no pools remain, change state from Active->Idle->Disabled 
    // Once Disabled, factory can remove its reference to the pool entry 

    sealed internal class DbConnectionPoolGroup { 
        private readonly DbConnectionOptions               _connectionOptions;
        private readonly DbConnectionPoolGroupOptions      _poolGroupOptions;
        private HybridDictionary                           _poolCollection;
 
        private          int                               _poolCount;      // number of pools
        private          int                               _state;          // see PoolGroupState* below 
 
        private          DbConnectionPoolGroupProviderInfo _providerInfo;
        private          DbMetaDataFactory                 _metaDataFactory; 

        private static int _objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);
 
        // always lock this before changing _state, we don't want to move out of `the disabled state
        // PoolGroupStateUninitialized = 0; 
        private const int PoolGroupStateActive   = 1; // initial state, GetPoolGroup from cache, connection Open 
        private const int PoolGroupStateIdle     = 2; // all pools are pruned via Clear
        private const int PoolGroupStateDisabled = 4; // factory pool entry prunning method 

        internal DbConnectionPoolGroup (DbConnectionOptions connectionOptions, DbConnectionPoolGroupOptions poolGroupOptions) {
            Debug.Assert(null != connectionOptions, "null connection options");
            Debug.Assert(null == poolGroupOptions || ADP.IsWindowsNT, "should not have pooling options on Win9x"); 

            _connectionOptions = connectionOptions; 
            _poolGroupOptions = poolGroupOptions; 

            // always lock this object before changing state 
            // HybridDictionary does not create any sub-objects until add
            // so it is safe to use for non-pooled connection as long as
            // we check _poolGroupOptions first
            _poolCollection = new HybridDictionary(1, false); 
            _state = PoolGroupStateActive; // VSWhidbey 112102
        } 
 
        internal DbConnectionOptions ConnectionOptions {
            get { 
                return _connectionOptions;
            }
        }
 
        internal int Count {
            get { 
                // NOTE: the use of this property does not indicate activity, 
                // on the pool entry because it only it is only used to identify
                // empty pool entries when we're pruning them. 
                return _poolCount;
            }
        }
 
        internal DbConnectionPoolGroupProviderInfo ProviderInfo {
            get { 
                return _providerInfo; 
            }
            set { 
                _providerInfo = value;
                if(null!=value) {
                    _providerInfo.PoolGroup = this;
                } 
            }
        } 
 
        internal bool IsDisabled {
            get { 
                return (PoolGroupStateDisabled == _state);
            }
        }
 
        internal int ObjectID {
            get { 
                return _objectID; 
            }
        } 

        internal DbConnectionPoolGroupOptions PoolGroupOptions {
            get {
                return _poolGroupOptions; 
            }
        } 
 
        internal DbMetaDataFactory MetaDataFactory{
            get { 
                return  _metaDataFactory;
                }

            set { 
                _metaDataFactory = value;
            } 
        } 

        internal void Clear() { 
            ClearInternal(true);
        }

        private bool ClearInternal(bool clearing) { 
            // must be multi-thread safe with competing calls by Clear and Prune via background thread
            // will return true for Prune on if the pool entry is Disabled or not 
 
            lock (this) {
                HybridDictionary poolCollection = _poolCollection; 
                if (0 < poolCollection.Count) {
                    HybridDictionary newPoolCollection = new HybridDictionary(poolCollection.Count, false);

                    foreach (DictionaryEntry entry in poolCollection) { 
                        if (null != entry.Value) {
                            DbConnectionPool pool = (DbConnectionPool)entry.Value; 
 
                            //
 



 

 
                            // Actually prune the pool if the user requested it (clearing == true) 
                            // or if there are no connections in the pool and no errors occurred.
                            // Empty pool during pruning indicates zero or low activity, but 
                            //  an error state indicates the pool needs to stay around to
                            //  throttle new connection attempts.
                            if (clearing || (!pool.ErrorOccurred && 0 == pool.Count)) {
 
                                // Order is important here.  First we remove the pool
                                // from the collection of pools so no one will try 
                                // to use it while we're processing and finally we put the 
                                // pool into a list of pools to be released when they
                                // are completely empty. 
                                DbConnectionFactory connectionFactory = pool.ConnectionFactory;

                                connectionFactory.PerformanceCounters.NumberOfActiveConnectionPools.Decrement();
                                connectionFactory.QueuePoolForRelease(pool, clearing); 
                            }
                            else { 
                                newPoolCollection.Add(entry.Key, entry.Value); 
                            }
                        } 
                    }
                    _poolCollection = newPoolCollection;
                    _poolCount = newPoolCollection.Count;
                } 

                // must be pruning thread to change state and no connections 
                // otherwise pruning thread risks making entry disabled soon after user calls ClearPool 
                if (!clearing && (0 == _poolCount)) {
                    if (PoolGroupStateActive == _state) { 
                        _state = PoolGroupStateIdle;
                        Bid.Trace("<prov.DbConnectionPoolGroup.ClearInternal|RES|INFO|CPOOL> %d#, Idle\n", ObjectID);
                    }
                    else if (PoolGroupStateIdle == _state) { 
                        _state = PoolGroupStateDisabled;
                        Bid.Trace("<prov.DbConnectionPoolGroup.ReadyToRemove|RES|INFO|CPOOL> %d#, Disabled\n", ObjectID); 
                    } 
                }
                return (PoolGroupStateDisabled == _state); 
            }
        }

        internal DbConnectionPool GetConnectionPool(DbConnectionFactory connectionFactory) { 
            // When this method returns null it indicates that the connection
            // factory should not use pooling. 
 
            // We don't support connection pooling on Win9x; it lacks too
            // many of the APIs we require. 
            // PoolGroupOptions will only be null when we're not supposed to pool
            // connections.
            object pool = null;
            if (null != _poolGroupOptions) { 
                Debug.Assert(ADP.IsWindowsNT, "should not be pooling on Win9x");
 
                DbConnectionPoolIdentity currentIdentity = DbConnectionPoolIdentity.NoIdentity; 
                if (_poolGroupOptions.PoolByIdentity) {
                    // if we're pooling by identity (because integrated security is 
                    // being used for these connections) then we need to go out and
                    // search for the connectionPool that matches the current identity.

                    currentIdentity = DbConnectionPoolIdentity.GetCurrent(); 

                    // If the current token is restricted in some way, then we must 
                    // not attempt to pool these connections. 
                    if (currentIdentity.IsRestricted) {
                        currentIdentity = null; 
                    }
                }
                if (null != currentIdentity) {
                    HybridDictionary poolCollection = _poolCollection; 
                    pool = poolCollection[currentIdentity]; // find the pool
                    if (null == pool) { 
 
                        DbConnectionPoolProviderInfo connectionPoolProviderInfo = connectionFactory.CreateConnectionPoolProviderInfo(this.ConnectionOptions);
 
                        // optimistically create pool, but its callbacks are delayed until after actual add
                        DbConnectionPool newPool = new DbConnectionPool(connectionFactory, this, currentIdentity, connectionPoolProviderInfo);

                        lock (this) { 
                            // Did someone already add it to the list?
                            poolCollection = _poolCollection; 
                            pool = poolCollection[currentIdentity]; // find the pool 

                            if (null == pool) { 
                                if (MarkPoolGroupAsActive()) {
                                    // If we get here, we know for certain that we there isn't
                                    // a pool that matches the current identity, so we have to
                                    // add the optimistically created one 
                                    newPool.Startup(); // must start pool before usage
 
                                    HybridDictionary newPoolCollection = new HybridDictionary(1+poolCollection.Count, false); 
                                    foreach(DictionaryEntry entry in poolCollection) {
                                        newPoolCollection.Add(entry.Key, entry.Value); 
                                    }
                                    newPoolCollection.Add(currentIdentity, newPool);
                                    connectionFactory.PerformanceCounters.NumberOfActiveConnectionPools.Increment();
                                    _poolCollection = newPoolCollection; 
                                    _poolCount = newPoolCollection.Count;
                                    pool = newPool; 
                                    newPool = null; 
                                }
                                else { 
                                    // else pool entry has been disabled so don't create new pools
                                    Debug.Assert(PoolGroupStateDisabled == _state, "state should be disabled");
                                }
                            } 
                            else {
                                // else found an existing pool to use instead 
                                Debug.Assert(PoolGroupStateActive == _state, "state should be active since a pool exists and lock holds"); 
                            }
                        } 

                        if (null != newPool) {
                            // don't need to call connectionFactory.QueuePoolForRelease(newPool) because
                            // pool callbacks were delayed and no risk of connections being created 
                            newPool.Shutdown();
                        } 
                    } 
                    // the found pool could be in any state
                } 
            }

            if (null == pool) {
                lock(this) { 
                    // keep the pool entry state active when not pooling
                    MarkPoolGroupAsActive(); 
                } 
            }
            return (DbConnectionPool)pool; 
        }

        private bool MarkPoolGroupAsActive() {
            // when getting a connection, make the entry active if it was idle (but not disabled) 
            // must always lock this before calling
 
            if (PoolGroupStateIdle == _state) { 
                _state = PoolGroupStateActive;
                Bid.Trace("<prov.DbConnectionPoolGroup.ClearInternal|RES|INFO|CPOOL> %d#, Active\n", ObjectID); 
            }
            return (PoolGroupStateActive == _state);
        }
 
        internal bool Prune() {
            // must only call from DbConnectionFactory.PruneConnectionPoolGroups on background timer thread 
            // must lock(DbConnectionFactory._connectionPoolGroups.SyncRoot) before calling ReadyToRemove 
            //     to avoid conflict with DbConnectionFactory.CreateConnectionPoolGroup replacing pool entry
 
            return ClearInternal(false);
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
