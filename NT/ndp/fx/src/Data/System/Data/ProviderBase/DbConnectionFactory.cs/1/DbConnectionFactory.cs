//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionFactory.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Data.Common;
    using System.Threading; 
#if ORACLE
    using System.Data.OracleClient; 
#endif 

    internal abstract class DbConnectionFactory { 
        private Dictionary<string,DbConnectionPoolGroup>  _connectionPoolGroups;
        private readonly List<DbConnectionPool> _poolsToRelease;
        private readonly List<DbConnectionPoolGroup> _poolGroupsToRelease;
        private readonly DbConnectionPoolCounters _performanceCounters; 
        private readonly Timer _pruningTimer;
 
        private const int PruningDueTime =4*60*1000;           // 4 minutes 
        private const int PruningPeriod  =  30*1000;           // thirty seconds
 
        private static int _objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);

        protected DbConnectionFactory() : this (DbConnectionPoolCountersNoCounters.SingletonInstance) { } 

        protected DbConnectionFactory(DbConnectionPoolCounters performanceCounters) { 
            _performanceCounters = performanceCounters; 
            _connectionPoolGroups = new Dictionary<string,DbConnectionPoolGroup>();
            _poolsToRelease = new List<DbConnectionPool>(); 
            _poolGroupsToRelease = new List<DbConnectionPoolGroup>();
            _pruningTimer = CreatePruningTimer();
        }
 
        internal DbConnectionPoolCounters PerformanceCounters {
            get { return _performanceCounters; } 
        } 

        abstract public DbProviderFactory ProviderFactory { 
            get;
        }

        internal int ObjectID { 
            get {
                return _objectID; 
            } 
        }
 
        public void ClearAllPools() {
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<prov.DbConnectionFactory.ClearAllPools|API> ");
            try { 
                Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
                foreach (KeyValuePair<string, DbConnectionPoolGroup> entry in connectionPoolGroups) { 
                    DbConnectionPoolGroup poolGroup = entry.Value; 
                    if (null != poolGroup) {
                        Debug.Assert(!poolGroup.IsDisabled, "Disabled pool entry discovered"); 
                        poolGroup.Clear();
                    }
                }
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            } 
        }
 
        public void ClearPool(DbConnection connection) {
            ADP.CheckArgumentNull(connection, "connection");

            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<prov.DbConnectionFactory.ClearPool|API> %d#" , GetObjectId(connection));
            try { 
                DbConnectionPoolGroup poolGroup = GetConnectionPoolGroup(connection); 
                if (null != poolGroup) {
                    poolGroup.Clear(); 
                }
            }
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
#if !ORACLE
        public void ClearPool(string connectionString) { 
            ADP.CheckArgumentNull(connectionString, "connectionString");

            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<prov.DbConnectionFactory.ClearPool|API> connectionString"); 
            try {
                DbConnectionPoolGroup poolGroup; 
                Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups; 
                if (connectionPoolGroups.TryGetValue(connectionString, out poolGroup)) {
                    poolGroup.Clear(); 
                }
            }
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        } 
#endif 

        internal virtual DbConnectionPoolProviderInfo CreateConnectionPoolProviderInfo(DbConnectionOptions connectionOptions){ 
            return null;
        }

        virtual protected DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection, out bool cacheMetaDataFactory) { 
            // providers that support GetSchema must override this with a method that creates a meta data
            // factory appropriate for them. 
            cacheMetaDataFactory = false; 
            throw ADP.NotSupported();
        } 

        internal DbConnectionInternal CreateNonPooledConnection(DbConnection owningConnection, DbConnectionPoolGroup poolGroup) {
            Debug.Assert(null != owningConnection, "null owningConnection?");
            Debug.Assert(null != poolGroup, "null poolGroup?"); 

            DbConnectionOptions connectionOptions = poolGroup.ConnectionOptions; 
            DbConnectionPoolGroupProviderInfo poolGroupProviderInfo = poolGroup.ProviderInfo; 

            DbConnectionInternal newConnection = CreateConnection(connectionOptions, poolGroupProviderInfo, null, owningConnection); 
            if (null != newConnection) {
                PerformanceCounters.HardConnectsPerSecond.Increment();
                newConnection.MakeNonPooledObject(owningConnection, PerformanceCounters);
            } 
            Bid.Trace("<prov.DbConnectionFactory.CreateNonPooledConnection|RES|CPOOL> %d#, Non-pooled database connection created.\n", ObjectID);
            return newConnection; 
        } 

        internal DbConnectionInternal CreatePooledConnection(DbConnection owningConnection, DbConnectionPool pool, DbConnectionOptions options) { 
            Debug.Assert(null != pool, "null pool?");
            DbConnectionPoolGroupProviderInfo poolGroupProviderInfo = pool.PoolGroup.ProviderInfo;

            DbConnectionInternal newConnection = CreateConnection(options, poolGroupProviderInfo, pool, owningConnection); 
            if (null != newConnection) {
                PerformanceCounters.HardConnectsPerSecond.Increment(); 
                newConnection.MakePooledConnection(pool); 
            }
            Bid.Trace("<prov.DbConnectionFactory.CreatePooledConnection|RES|CPOOL> %d#, Pooled database connection created.\n", ObjectID); 
            return newConnection;
        }

        virtual internal DbConnectionPoolGroupProviderInfo CreateConnectionPoolGroupProviderInfo (DbConnectionOptions connectionOptions) { 
            return null;
        } 
 
        private Timer CreatePruningTimer() {
            TimerCallback callback = new TimerCallback(PruneConnectionPoolGroups); 
            return new Timer(callback, null, PruningDueTime, PruningPeriod);
        }

#if !ORACLE 
        protected DbConnectionOptions FindConnectionOptions(string connectionString) {
            if (!ADP.IsEmpty(connectionString)) { 
                DbConnectionPoolGroup connectionPoolGroup; 
                Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
                if (connectionPoolGroups.TryGetValue(connectionString, out connectionPoolGroup)) { 
                    return connectionPoolGroup.ConnectionOptions;
                }
            }
            return null; 
        }
#endif 
 
        internal DbConnectionInternal GetConnection(DbConnection owningConnection) {
            Debug.Assert(null != owningConnection, "null owningConnection?"); 

            DbConnectionPoolGroup poolGroup = GetConnectionPoolGroup(owningConnection);
            DbConnectionPool connectionPool = GetConnectionPool(owningConnection, poolGroup);
            DbConnectionInternal connection; 

            if (null == connectionPool) { 
                // If GetConnectionPool returns null, we can be certain that 
                // this connection should not be pooled via DbConnectionPool
                // or have a disabled pool entry. 
                poolGroup = GetConnectionPoolGroup(owningConnection); // previous entry have been disabled
                connection = CreateNonPooledConnection(owningConnection, poolGroup);
                PerformanceCounters.NumberOfNonPooledConnections.Increment();
            } 
            else {
                connection = connectionPool.GetConnection(owningConnection); 
 
                // If GetConnection failed the pool timeout occurred.
                if (null == connection) { 
                    Bid.Trace("<prov.DbConnectionFactory.GetConnection|RES|CPOOL> %d#, GetConnection failed because a pool timeout occurred.\n", ObjectID);
                    throw ADP.PooledOpenTimeout();
                }
            } 
            return connection;
        } 
 
        private DbConnectionPool GetConnectionPool(DbConnection owningObject, DbConnectionPoolGroup connectionPoolGroup) {
            // if poolgroup is disabled, it will be replaced with a new entry 

            Debug.Assert(null != owningObject, "null owningObject?");
            Debug.Assert(null != connectionPoolGroup, "null connectionPoolGroup?");
 
            // It is possible that while the outer connection object has
            // been sitting around in a closed and unused state in some long 
            // running app, the pruner may have come along and remove this 
            // the pool entry from the master list.  If we were to use a
            // pool entry in this state, we would create "unmanaged" pools, 
            // which would be bad.  To avoid this problem, we automagically
            // re-create the pool entry whenever it's disabled.

            // however, don't rebuild connectionOptions if no pooling is involved - let new connections do that work 
            if (connectionPoolGroup.IsDisabled && (null != connectionPoolGroup.PoolGroupOptions)) {
                Bid.Trace("<prov.DbConnectionFactory.GetConnectionPool|RES|INFO|CPOOL> %d#, DisabledPoolGroup=%d#\n", ObjectID, connectionPoolGroup.ObjectID); 
 
                // reusing existing pool option in case user originally used SetConnectionPoolOptions
                DbConnectionPoolGroupOptions poolOptions = connectionPoolGroup.PoolGroupOptions; 

                // get the string to hash on again
                DbConnectionOptions connectionOptions = connectionPoolGroup.ConnectionOptions;
                string connectionString = connectionOptions.UsersConnectionString(false); 

                Debug.Assert(null != connectionOptions, "prevent expansion of connectionString"); 
                connectionPoolGroup = GetConnectionPoolGroup(connectionString, poolOptions, ref connectionOptions); 
                Debug.Assert(null != connectionPoolGroup, "null connectionPoolGroup?");
                SetConnectionPoolGroup(owningObject, connectionPoolGroup); 
            }
            DbConnectionPool connectionPool = connectionPoolGroup.GetConnectionPool(this);
            return connectionPool;
        } 

        internal DbConnectionPoolGroup GetConnectionPoolGroup(string connectionString,  DbConnectionPoolGroupOptions poolOptions, ref DbConnectionOptions userConnectionOptions) { 
            if (ADP.IsEmpty(connectionString)) { 
                return (DbConnectionPoolGroup)null;
            } 

            DbConnectionPoolGroup connectionPoolGroup;
            Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
            if (!connectionPoolGroups.TryGetValue(connectionString, out connectionPoolGroup) || (connectionPoolGroup.IsDisabled && (null != connectionPoolGroup.PoolGroupOptions))) { 
                // If we can't find an entry for the connection string in
                // our collection of pool entries, then we need to create a 
                // new pool entry and add it to our collection. 

                DbConnectionOptions connectionOptions = CreateConnectionOptions(connectionString, userConnectionOptions); 
                if (null == connectionOptions) {
                    throw ADP.InternalConnectionError(ADP.ConnectionError.ConnectionOptionsMissing);
                }
 
                string expandedConnectionString = connectionString;
                if (null == userConnectionOptions) { // we only allow one expansion on the connection string 
 
                    userConnectionOptions = connectionOptions;
                    expandedConnectionString = connectionOptions.Expand(); 

                    // if the expanded string is same instance (default implementation), the use the already created options
                    if ((object)expandedConnectionString != (object)connectionString) {
                        // 
                        return GetConnectionPoolGroup(expandedConnectionString, null, ref userConnectionOptions);
                    } 
                } 

                // We don't support connection pooling on Win9x; it lacks too many of the APIs we require. 
                if ((null == poolOptions) && ADP.IsWindowsNT) {
                    if (null != connectionPoolGroup) {
                        // reusing existing pool option in case user originally used SetConnectionPoolOptions
                        poolOptions = connectionPoolGroup.PoolGroupOptions; 
                    }
                    else { 
                        // Note: may return null for non-pooled connections 
                        poolOptions = CreateConnectionPoolGroupOptions(connectionOptions);
                    } 
                }


                DbConnectionPoolGroup newConnectionPoolGroup = new DbConnectionPoolGroup(connectionOptions, poolOptions); 
                newConnectionPoolGroup.ProviderInfo = CreateConnectionPoolGroupProviderInfo(connectionOptions);
 
                lock (this) { 
                    connectionPoolGroups = _connectionPoolGroups;
                    if (!connectionPoolGroups.TryGetValue(expandedConnectionString, out connectionPoolGroup)) { 
                        // build new dictionary with space for new connection string
                        Dictionary<string,DbConnectionPoolGroup> newConnectionPoolGroups = new Dictionary<string,DbConnectionPoolGroup>(1+connectionPoolGroups.Count);
                        foreach (KeyValuePair<string, DbConnectionPoolGroup> entry in connectionPoolGroups) {
                            newConnectionPoolGroups.Add(entry.Key, entry.Value); 
                        }
 
                        // lock prevents race condition with PruneConnectionPoolGroups 
                        newConnectionPoolGroups.Add(expandedConnectionString, newConnectionPoolGroup);
                        PerformanceCounters.NumberOfActiveConnectionPoolGroups.Increment(); 
                        connectionPoolGroup = newConnectionPoolGroup;
                        _connectionPoolGroups = newConnectionPoolGroups;
                    }
                    else { 
                        Debug.Assert(!connectionPoolGroup.IsDisabled, "Disabled pool entry discovered");
                    } 
                } 
                Debug.Assert(null != connectionPoolGroup, "how did we not create a pool entry?");
                Debug.Assert(null != userConnectionOptions, "how did we not have user connection options?"); 
            }
            else if (null == userConnectionOptions) {
                userConnectionOptions = connectionPoolGroup.ConnectionOptions;
            } 
            return connectionPoolGroup;
        } 
 
        internal DbMetaDataFactory GetMetaDataFactory(DbConnectionPoolGroup connectionPoolGroup,DbConnectionInternal internalConnection){
            Debug.Assert (connectionPoolGroup != null, "connectionPoolGroup may not be null."); 

            // get the matadatafactory from the pool entry. If it does not already have one
            // create one and save it on the pool entry
            DbMetaDataFactory metaDataFactory = connectionPoolGroup.MetaDataFactory; 

            // consider serializing this so we don't construct multiple metadata factories 
            // if two threads happen to hit this at the same time.  One will be GC'd 
            if (metaDataFactory == null){
                bool allowCache = false; 
                metaDataFactory = CreateMetaDataFactory(internalConnection, out allowCache);
                if (allowCache) {
                    connectionPoolGroup.MetaDataFactory = metaDataFactory;
                } 
            }
            return metaDataFactory; 
        } 

        private void PruneConnectionPoolGroups(object state) { 
            // when debugging this method, expect multiple threads at the same time
            if (Bid.AdvancedOn) {
                Bid.Trace("<prov.DbConnectionFactory.PruneConnectionPoolGroups|RES|INFO|CPOOL> %d#\n", ObjectID);
            } 

            // First, walk the pool release list and attempt to clear each 
            // pool, when the pool is finally empty, we dispose of it.  If the 
            // pool isn't empty, it's because there are active connections or
            // distributed transactions that need it. 
            lock (_poolsToRelease) {
                if (0 != _poolsToRelease.Count) {
                    DbConnectionPool[] poolsToRelease = _poolsToRelease.ToArray();
                    foreach (DbConnectionPool pool in poolsToRelease) { 
                        if (null != pool) {
                            pool.Clear(); 
 
                            if (0 == pool.Count) {
                                _poolsToRelease.Remove(pool); 
                                if (Bid.AdvancedOn) {
                                    Bid.Trace("<prov.DbConnectionFactory.PruneConnectionPoolGroups|RES|INFO|CPOOL> %d#, ReleasePool=%d#\n", ObjectID, pool.ObjectID);
                                }
                                PerformanceCounters.NumberOfInactiveConnectionPools.Decrement(); 
                            }
                        } 
                    } 
                }
            } 

            // Next, walk the pool entry release list and dispose of each
            // pool entry when it is finally empty.  If the pool entry isn't
            // empty, it's because there are active pools that need it. 
            lock (_poolGroupsToRelease) {
                if (0 != _poolGroupsToRelease.Count) { 
                    DbConnectionPoolGroup[] poolGroupsToRelease = _poolGroupsToRelease.ToArray(); 
                    foreach (DbConnectionPoolGroup poolGroup in poolGroupsToRelease) {
                        if (null != poolGroup) { 
                            poolGroup.Clear(); // may add entries to _poolsToRelease

                            if (0 == poolGroup.Count) {
                                _poolGroupsToRelease.Remove(poolGroup); 
                                if (Bid.AdvancedOn) {
                                    Bid.Trace("<prov.DbConnectionFactory.PruneConnectionPoolGroups|RES|INFO|CPOOL> %d#, ReleasePoolGroup=%d#\n", ObjectID, poolGroup.ObjectID); 
                                } 
                                PerformanceCounters.NumberOfInactiveConnectionPoolGroups.Decrement();
                            } 
                        }
                    }
                }
            } 

            // Finally, we walk through the collection of connection pool entries 
            // and prune each one.  This will cause any empty pools to be put 
            // into the release list.
            lock (this) { 
                Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
                Dictionary<string,DbConnectionPoolGroup> newConnectionPoolGroups = new Dictionary<string,DbConnectionPoolGroup>(connectionPoolGroups.Count);

                foreach (KeyValuePair<string, DbConnectionPoolGroup> entry in connectionPoolGroups) { 
                    if (null != entry.Value) {
                        Debug.Assert(!entry.Value.IsDisabled, "Disabled pool entry discovered"); 
 
                        // entries start active and go idle during prune if all pools are gone
                        // move idle entries from last prune pass to a queue for pending release 
                        // otherwise process entry which may move it from active to idle
                        if (entry.Value.Prune()) { // may add entries to _poolsToRelease
                            PerformanceCounters.NumberOfActiveConnectionPoolGroups.Decrement();
                            QueuePoolGroupForRelease(entry.Value); 
                        }
                        else { 
                            newConnectionPoolGroups.Add(entry.Key, entry.Value); 
                        }
                    } 
                }
                _connectionPoolGroups = newConnectionPoolGroups;
            }
        } 

        internal void QueuePoolForRelease(DbConnectionPool pool, bool clearing) { 
            // Queue the pool up for release -- we'll clear it out and dispose 
            // of it as the last part of the pruning timer callback so we don't
            // do it with the pool entry or the pool collection locked. 
            Debug.Assert (null != pool, "null pool?");

            // set the pool to the shutdown state to force all active
            // connections to be automatically disposed when they 
            // are returned to the pool
            pool.Shutdown(); 
 
            lock (_poolsToRelease) {
                if (clearing) { 
                    pool.Clear();
                }
                _poolsToRelease.Add(pool);
            } 
            PerformanceCounters.NumberOfInactiveConnectionPools.Increment();
        } 
 
        internal void QueuePoolGroupForRelease(DbConnectionPoolGroup poolGroup) {
            Debug.Assert (null != poolGroup, "null poolGroup?"); 
            Bid.Trace("<prov.DbConnectionFactory.QueuePoolGroupForRelease|RES|INFO|CPOOL> %d#, poolGroup=%d#\n", ObjectID, poolGroup.ObjectID);

            lock (_poolGroupsToRelease) {
                _poolGroupsToRelease.Add(poolGroup); 
            }
            PerformanceCounters.NumberOfInactiveConnectionPoolGroups.Increment(); 
        } 

        abstract protected DbConnectionInternal CreateConnection(DbConnectionOptions options, object poolGroupProviderInfo, DbConnectionPool pool, DbConnection owningConnection); 

        abstract protected DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions previous);

        abstract protected DbConnectionPoolGroupOptions CreateConnectionPoolGroupOptions(DbConnectionOptions options); 

        abstract internal DbConnectionPoolGroup GetConnectionPoolGroup(DbConnection connection); 
 
        abstract internal DbConnectionInternal GetInnerConnection(DbConnection connection);
 
        abstract protected int GetObjectId(DbConnection connection);

        abstract internal void PermissionDemand(DbConnection outerConnection);
 
        abstract internal void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup);
 
        abstract internal void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to); 

        abstract internal bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from) ; 

        abstract internal void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to);
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionFactory.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Data.Common;
    using System.Threading; 
#if ORACLE
    using System.Data.OracleClient; 
#endif 

    internal abstract class DbConnectionFactory { 
        private Dictionary<string,DbConnectionPoolGroup>  _connectionPoolGroups;
        private readonly List<DbConnectionPool> _poolsToRelease;
        private readonly List<DbConnectionPoolGroup> _poolGroupsToRelease;
        private readonly DbConnectionPoolCounters _performanceCounters; 
        private readonly Timer _pruningTimer;
 
        private const int PruningDueTime =4*60*1000;           // 4 minutes 
        private const int PruningPeriod  =  30*1000;           // thirty seconds
 
        private static int _objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);

        protected DbConnectionFactory() : this (DbConnectionPoolCountersNoCounters.SingletonInstance) { } 

        protected DbConnectionFactory(DbConnectionPoolCounters performanceCounters) { 
            _performanceCounters = performanceCounters; 
            _connectionPoolGroups = new Dictionary<string,DbConnectionPoolGroup>();
            _poolsToRelease = new List<DbConnectionPool>(); 
            _poolGroupsToRelease = new List<DbConnectionPoolGroup>();
            _pruningTimer = CreatePruningTimer();
        }
 
        internal DbConnectionPoolCounters PerformanceCounters {
            get { return _performanceCounters; } 
        } 

        abstract public DbProviderFactory ProviderFactory { 
            get;
        }

        internal int ObjectID { 
            get {
                return _objectID; 
            } 
        }
 
        public void ClearAllPools() {
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<prov.DbConnectionFactory.ClearAllPools|API> ");
            try { 
                Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
                foreach (KeyValuePair<string, DbConnectionPoolGroup> entry in connectionPoolGroups) { 
                    DbConnectionPoolGroup poolGroup = entry.Value; 
                    if (null != poolGroup) {
                        Debug.Assert(!poolGroup.IsDisabled, "Disabled pool entry discovered"); 
                        poolGroup.Clear();
                    }
                }
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            } 
        }
 
        public void ClearPool(DbConnection connection) {
            ADP.CheckArgumentNull(connection, "connection");

            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<prov.DbConnectionFactory.ClearPool|API> %d#" , GetObjectId(connection));
            try { 
                DbConnectionPoolGroup poolGroup = GetConnectionPoolGroup(connection); 
                if (null != poolGroup) {
                    poolGroup.Clear(); 
                }
            }
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
#if !ORACLE
        public void ClearPool(string connectionString) { 
            ADP.CheckArgumentNull(connectionString, "connectionString");

            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<prov.DbConnectionFactory.ClearPool|API> connectionString"); 
            try {
                DbConnectionPoolGroup poolGroup; 
                Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups; 
                if (connectionPoolGroups.TryGetValue(connectionString, out poolGroup)) {
                    poolGroup.Clear(); 
                }
            }
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        } 
#endif 

        internal virtual DbConnectionPoolProviderInfo CreateConnectionPoolProviderInfo(DbConnectionOptions connectionOptions){ 
            return null;
        }

        virtual protected DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection, out bool cacheMetaDataFactory) { 
            // providers that support GetSchema must override this with a method that creates a meta data
            // factory appropriate for them. 
            cacheMetaDataFactory = false; 
            throw ADP.NotSupported();
        } 

        internal DbConnectionInternal CreateNonPooledConnection(DbConnection owningConnection, DbConnectionPoolGroup poolGroup) {
            Debug.Assert(null != owningConnection, "null owningConnection?");
            Debug.Assert(null != poolGroup, "null poolGroup?"); 

            DbConnectionOptions connectionOptions = poolGroup.ConnectionOptions; 
            DbConnectionPoolGroupProviderInfo poolGroupProviderInfo = poolGroup.ProviderInfo; 

            DbConnectionInternal newConnection = CreateConnection(connectionOptions, poolGroupProviderInfo, null, owningConnection); 
            if (null != newConnection) {
                PerformanceCounters.HardConnectsPerSecond.Increment();
                newConnection.MakeNonPooledObject(owningConnection, PerformanceCounters);
            } 
            Bid.Trace("<prov.DbConnectionFactory.CreateNonPooledConnection|RES|CPOOL> %d#, Non-pooled database connection created.\n", ObjectID);
            return newConnection; 
        } 

        internal DbConnectionInternal CreatePooledConnection(DbConnection owningConnection, DbConnectionPool pool, DbConnectionOptions options) { 
            Debug.Assert(null != pool, "null pool?");
            DbConnectionPoolGroupProviderInfo poolGroupProviderInfo = pool.PoolGroup.ProviderInfo;

            DbConnectionInternal newConnection = CreateConnection(options, poolGroupProviderInfo, pool, owningConnection); 
            if (null != newConnection) {
                PerformanceCounters.HardConnectsPerSecond.Increment(); 
                newConnection.MakePooledConnection(pool); 
            }
            Bid.Trace("<prov.DbConnectionFactory.CreatePooledConnection|RES|CPOOL> %d#, Pooled database connection created.\n", ObjectID); 
            return newConnection;
        }

        virtual internal DbConnectionPoolGroupProviderInfo CreateConnectionPoolGroupProviderInfo (DbConnectionOptions connectionOptions) { 
            return null;
        } 
 
        private Timer CreatePruningTimer() {
            TimerCallback callback = new TimerCallback(PruneConnectionPoolGroups); 
            return new Timer(callback, null, PruningDueTime, PruningPeriod);
        }

#if !ORACLE 
        protected DbConnectionOptions FindConnectionOptions(string connectionString) {
            if (!ADP.IsEmpty(connectionString)) { 
                DbConnectionPoolGroup connectionPoolGroup; 
                Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
                if (connectionPoolGroups.TryGetValue(connectionString, out connectionPoolGroup)) { 
                    return connectionPoolGroup.ConnectionOptions;
                }
            }
            return null; 
        }
#endif 
 
        internal DbConnectionInternal GetConnection(DbConnection owningConnection) {
            Debug.Assert(null != owningConnection, "null owningConnection?"); 

            DbConnectionPoolGroup poolGroup = GetConnectionPoolGroup(owningConnection);
            DbConnectionPool connectionPool = GetConnectionPool(owningConnection, poolGroup);
            DbConnectionInternal connection; 

            if (null == connectionPool) { 
                // If GetConnectionPool returns null, we can be certain that 
                // this connection should not be pooled via DbConnectionPool
                // or have a disabled pool entry. 
                poolGroup = GetConnectionPoolGroup(owningConnection); // previous entry have been disabled
                connection = CreateNonPooledConnection(owningConnection, poolGroup);
                PerformanceCounters.NumberOfNonPooledConnections.Increment();
            } 
            else {
                connection = connectionPool.GetConnection(owningConnection); 
 
                // If GetConnection failed the pool timeout occurred.
                if (null == connection) { 
                    Bid.Trace("<prov.DbConnectionFactory.GetConnection|RES|CPOOL> %d#, GetConnection failed because a pool timeout occurred.\n", ObjectID);
                    throw ADP.PooledOpenTimeout();
                }
            } 
            return connection;
        } 
 
        private DbConnectionPool GetConnectionPool(DbConnection owningObject, DbConnectionPoolGroup connectionPoolGroup) {
            // if poolgroup is disabled, it will be replaced with a new entry 

            Debug.Assert(null != owningObject, "null owningObject?");
            Debug.Assert(null != connectionPoolGroup, "null connectionPoolGroup?");
 
            // It is possible that while the outer connection object has
            // been sitting around in a closed and unused state in some long 
            // running app, the pruner may have come along and remove this 
            // the pool entry from the master list.  If we were to use a
            // pool entry in this state, we would create "unmanaged" pools, 
            // which would be bad.  To avoid this problem, we automagically
            // re-create the pool entry whenever it's disabled.

            // however, don't rebuild connectionOptions if no pooling is involved - let new connections do that work 
            if (connectionPoolGroup.IsDisabled && (null != connectionPoolGroup.PoolGroupOptions)) {
                Bid.Trace("<prov.DbConnectionFactory.GetConnectionPool|RES|INFO|CPOOL> %d#, DisabledPoolGroup=%d#\n", ObjectID, connectionPoolGroup.ObjectID); 
 
                // reusing existing pool option in case user originally used SetConnectionPoolOptions
                DbConnectionPoolGroupOptions poolOptions = connectionPoolGroup.PoolGroupOptions; 

                // get the string to hash on again
                DbConnectionOptions connectionOptions = connectionPoolGroup.ConnectionOptions;
                string connectionString = connectionOptions.UsersConnectionString(false); 

                Debug.Assert(null != connectionOptions, "prevent expansion of connectionString"); 
                connectionPoolGroup = GetConnectionPoolGroup(connectionString, poolOptions, ref connectionOptions); 
                Debug.Assert(null != connectionPoolGroup, "null connectionPoolGroup?");
                SetConnectionPoolGroup(owningObject, connectionPoolGroup); 
            }
            DbConnectionPool connectionPool = connectionPoolGroup.GetConnectionPool(this);
            return connectionPool;
        } 

        internal DbConnectionPoolGroup GetConnectionPoolGroup(string connectionString,  DbConnectionPoolGroupOptions poolOptions, ref DbConnectionOptions userConnectionOptions) { 
            if (ADP.IsEmpty(connectionString)) { 
                return (DbConnectionPoolGroup)null;
            } 

            DbConnectionPoolGroup connectionPoolGroup;
            Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
            if (!connectionPoolGroups.TryGetValue(connectionString, out connectionPoolGroup) || (connectionPoolGroup.IsDisabled && (null != connectionPoolGroup.PoolGroupOptions))) { 
                // If we can't find an entry for the connection string in
                // our collection of pool entries, then we need to create a 
                // new pool entry and add it to our collection. 

                DbConnectionOptions connectionOptions = CreateConnectionOptions(connectionString, userConnectionOptions); 
                if (null == connectionOptions) {
                    throw ADP.InternalConnectionError(ADP.ConnectionError.ConnectionOptionsMissing);
                }
 
                string expandedConnectionString = connectionString;
                if (null == userConnectionOptions) { // we only allow one expansion on the connection string 
 
                    userConnectionOptions = connectionOptions;
                    expandedConnectionString = connectionOptions.Expand(); 

                    // if the expanded string is same instance (default implementation), the use the already created options
                    if ((object)expandedConnectionString != (object)connectionString) {
                        // 
                        return GetConnectionPoolGroup(expandedConnectionString, null, ref userConnectionOptions);
                    } 
                } 

                // We don't support connection pooling on Win9x; it lacks too many of the APIs we require. 
                if ((null == poolOptions) && ADP.IsWindowsNT) {
                    if (null != connectionPoolGroup) {
                        // reusing existing pool option in case user originally used SetConnectionPoolOptions
                        poolOptions = connectionPoolGroup.PoolGroupOptions; 
                    }
                    else { 
                        // Note: may return null for non-pooled connections 
                        poolOptions = CreateConnectionPoolGroupOptions(connectionOptions);
                    } 
                }


                DbConnectionPoolGroup newConnectionPoolGroup = new DbConnectionPoolGroup(connectionOptions, poolOptions); 
                newConnectionPoolGroup.ProviderInfo = CreateConnectionPoolGroupProviderInfo(connectionOptions);
 
                lock (this) { 
                    connectionPoolGroups = _connectionPoolGroups;
                    if (!connectionPoolGroups.TryGetValue(expandedConnectionString, out connectionPoolGroup)) { 
                        // build new dictionary with space for new connection string
                        Dictionary<string,DbConnectionPoolGroup> newConnectionPoolGroups = new Dictionary<string,DbConnectionPoolGroup>(1+connectionPoolGroups.Count);
                        foreach (KeyValuePair<string, DbConnectionPoolGroup> entry in connectionPoolGroups) {
                            newConnectionPoolGroups.Add(entry.Key, entry.Value); 
                        }
 
                        // lock prevents race condition with PruneConnectionPoolGroups 
                        newConnectionPoolGroups.Add(expandedConnectionString, newConnectionPoolGroup);
                        PerformanceCounters.NumberOfActiveConnectionPoolGroups.Increment(); 
                        connectionPoolGroup = newConnectionPoolGroup;
                        _connectionPoolGroups = newConnectionPoolGroups;
                    }
                    else { 
                        Debug.Assert(!connectionPoolGroup.IsDisabled, "Disabled pool entry discovered");
                    } 
                } 
                Debug.Assert(null != connectionPoolGroup, "how did we not create a pool entry?");
                Debug.Assert(null != userConnectionOptions, "how did we not have user connection options?"); 
            }
            else if (null == userConnectionOptions) {
                userConnectionOptions = connectionPoolGroup.ConnectionOptions;
            } 
            return connectionPoolGroup;
        } 
 
        internal DbMetaDataFactory GetMetaDataFactory(DbConnectionPoolGroup connectionPoolGroup,DbConnectionInternal internalConnection){
            Debug.Assert (connectionPoolGroup != null, "connectionPoolGroup may not be null."); 

            // get the matadatafactory from the pool entry. If it does not already have one
            // create one and save it on the pool entry
            DbMetaDataFactory metaDataFactory = connectionPoolGroup.MetaDataFactory; 

            // consider serializing this so we don't construct multiple metadata factories 
            // if two threads happen to hit this at the same time.  One will be GC'd 
            if (metaDataFactory == null){
                bool allowCache = false; 
                metaDataFactory = CreateMetaDataFactory(internalConnection, out allowCache);
                if (allowCache) {
                    connectionPoolGroup.MetaDataFactory = metaDataFactory;
                } 
            }
            return metaDataFactory; 
        } 

        private void PruneConnectionPoolGroups(object state) { 
            // when debugging this method, expect multiple threads at the same time
            if (Bid.AdvancedOn) {
                Bid.Trace("<prov.DbConnectionFactory.PruneConnectionPoolGroups|RES|INFO|CPOOL> %d#\n", ObjectID);
            } 

            // First, walk the pool release list and attempt to clear each 
            // pool, when the pool is finally empty, we dispose of it.  If the 
            // pool isn't empty, it's because there are active connections or
            // distributed transactions that need it. 
            lock (_poolsToRelease) {
                if (0 != _poolsToRelease.Count) {
                    DbConnectionPool[] poolsToRelease = _poolsToRelease.ToArray();
                    foreach (DbConnectionPool pool in poolsToRelease) { 
                        if (null != pool) {
                            pool.Clear(); 
 
                            if (0 == pool.Count) {
                                _poolsToRelease.Remove(pool); 
                                if (Bid.AdvancedOn) {
                                    Bid.Trace("<prov.DbConnectionFactory.PruneConnectionPoolGroups|RES|INFO|CPOOL> %d#, ReleasePool=%d#\n", ObjectID, pool.ObjectID);
                                }
                                PerformanceCounters.NumberOfInactiveConnectionPools.Decrement(); 
                            }
                        } 
                    } 
                }
            } 

            // Next, walk the pool entry release list and dispose of each
            // pool entry when it is finally empty.  If the pool entry isn't
            // empty, it's because there are active pools that need it. 
            lock (_poolGroupsToRelease) {
                if (0 != _poolGroupsToRelease.Count) { 
                    DbConnectionPoolGroup[] poolGroupsToRelease = _poolGroupsToRelease.ToArray(); 
                    foreach (DbConnectionPoolGroup poolGroup in poolGroupsToRelease) {
                        if (null != poolGroup) { 
                            poolGroup.Clear(); // may add entries to _poolsToRelease

                            if (0 == poolGroup.Count) {
                                _poolGroupsToRelease.Remove(poolGroup); 
                                if (Bid.AdvancedOn) {
                                    Bid.Trace("<prov.DbConnectionFactory.PruneConnectionPoolGroups|RES|INFO|CPOOL> %d#, ReleasePoolGroup=%d#\n", ObjectID, poolGroup.ObjectID); 
                                } 
                                PerformanceCounters.NumberOfInactiveConnectionPoolGroups.Decrement();
                            } 
                        }
                    }
                }
            } 

            // Finally, we walk through the collection of connection pool entries 
            // and prune each one.  This will cause any empty pools to be put 
            // into the release list.
            lock (this) { 
                Dictionary<string,DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
                Dictionary<string,DbConnectionPoolGroup> newConnectionPoolGroups = new Dictionary<string,DbConnectionPoolGroup>(connectionPoolGroups.Count);

                foreach (KeyValuePair<string, DbConnectionPoolGroup> entry in connectionPoolGroups) { 
                    if (null != entry.Value) {
                        Debug.Assert(!entry.Value.IsDisabled, "Disabled pool entry discovered"); 
 
                        // entries start active and go idle during prune if all pools are gone
                        // move idle entries from last prune pass to a queue for pending release 
                        // otherwise process entry which may move it from active to idle
                        if (entry.Value.Prune()) { // may add entries to _poolsToRelease
                            PerformanceCounters.NumberOfActiveConnectionPoolGroups.Decrement();
                            QueuePoolGroupForRelease(entry.Value); 
                        }
                        else { 
                            newConnectionPoolGroups.Add(entry.Key, entry.Value); 
                        }
                    } 
                }
                _connectionPoolGroups = newConnectionPoolGroups;
            }
        } 

        internal void QueuePoolForRelease(DbConnectionPool pool, bool clearing) { 
            // Queue the pool up for release -- we'll clear it out and dispose 
            // of it as the last part of the pruning timer callback so we don't
            // do it with the pool entry or the pool collection locked. 
            Debug.Assert (null != pool, "null pool?");

            // set the pool to the shutdown state to force all active
            // connections to be automatically disposed when they 
            // are returned to the pool
            pool.Shutdown(); 
 
            lock (_poolsToRelease) {
                if (clearing) { 
                    pool.Clear();
                }
                _poolsToRelease.Add(pool);
            } 
            PerformanceCounters.NumberOfInactiveConnectionPools.Increment();
        } 
 
        internal void QueuePoolGroupForRelease(DbConnectionPoolGroup poolGroup) {
            Debug.Assert (null != poolGroup, "null poolGroup?"); 
            Bid.Trace("<prov.DbConnectionFactory.QueuePoolGroupForRelease|RES|INFO|CPOOL> %d#, poolGroup=%d#\n", ObjectID, poolGroup.ObjectID);

            lock (_poolGroupsToRelease) {
                _poolGroupsToRelease.Add(poolGroup); 
            }
            PerformanceCounters.NumberOfInactiveConnectionPoolGroups.Increment(); 
        } 

        abstract protected DbConnectionInternal CreateConnection(DbConnectionOptions options, object poolGroupProviderInfo, DbConnectionPool pool, DbConnection owningConnection); 

        abstract protected DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions previous);

        abstract protected DbConnectionPoolGroupOptions CreateConnectionPoolGroupOptions(DbConnectionOptions options); 

        abstract internal DbConnectionPoolGroup GetConnectionPoolGroup(DbConnection connection); 
 
        abstract internal DbConnectionInternal GetInnerConnection(DbConnection connection);
 
        abstract protected int GetObjectId(DbConnection connection);

        abstract internal void PermissionDemand(DbConnection outerConnection);
 
        abstract internal void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup);
 
        abstract internal void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to); 

        abstract internal bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from) ; 

        abstract internal void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to);
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
