//------------------------------------------------------------------------------ 
// <copyright file="SqlConnectionFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient
{ 
    using System;
    using System.Data.Common;
    using System.Data.ProviderBase;
    using System.Collections.Specialized; 
    using System.Configuration;
    using System.Diagnostics; 
    using System.IO; 

    using Microsoft.SqlServer.Server; 


    sealed internal class SqlConnectionFactory : DbConnectionFactory {
        private SqlConnectionFactory() : base(SqlPerformanceCounters.SingletonInstance) {} 

        public static readonly SqlConnectionFactory SingletonInstance = new SqlConnectionFactory(); 
        private const string _metaDataXml          = "MetaDataXml"; 

        override public DbProviderFactory ProviderFactory { 
            get {
                return SqlClientFactory.Instance;
            }
        } 

        override protected DbConnectionInternal CreateConnection(DbConnectionOptions options, object poolGroupProviderInfo, DbConnectionPool pool, DbConnection owningConnection) { 
            SqlConnectionString opt = (SqlConnectionString)options; 
            SqlInternalConnection result = null;
 
            if (opt.ContextConnection) {
                result = GetContextConnection(opt, poolGroupProviderInfo, owningConnection);
            }
            else { 
                bool redirectedUserInstance       = false;
                DbConnectionPoolIdentity identity = null; 
 
                // Pass DbConnectionPoolIdentity to SqlInternalConnectionTds if using integrated security.
                // Used by notifications. 
                if (opt.IntegratedSecurity) {
                    if (pool != null) {
                        identity = pool.Identity;
                    } 
                    else {
                        identity = DbConnectionPoolIdentity.GetCurrent(); 
                    } 
                }
 
                // FOLLOWING IF BLOCK IS ENTIRELY FOR SSE USER INSTANCES
                // If "user instance=true" is in the connection string, we're using SSE user instances
                if (opt.UserInstance) {
                    redirectedUserInstance = true; 
                    string instanceName;
 
                    if ( (null == pool) || 
                         (null != pool && pool.Count <= 0) ) { // Non-pooled or pooled and no connections in the pool.
 
                        SqlInternalConnectionTds sseConnection = null;
                        try {
                            // What about a failure - throw?  YES!
                            sseConnection = new SqlInternalConnectionTds(identity, opt, null, "", null, false); 
                            instanceName = sseConnection.InstanceName;
 
                            if (!instanceName.StartsWith("\\\\.\\", StringComparison.Ordinal)) { 
                                throw SQL.NonLocalSSEInstance();
                            } 

                            if (null != pool) { // Pooled connection - cache result
                                SqlConnectionPoolProviderInfo providerInfo = (SqlConnectionPoolProviderInfo) pool.ProviderInfo;
                                // No lock since we are already in creation mutex 
                                providerInfo.InstanceName = instanceName;
                            } 
                        } 
                        finally {
                            if (null != sseConnection) { 
                                sseConnection.Dispose();
                            }
                        }
                    } 
                    else { // Cached info from pool.
                        SqlConnectionPoolProviderInfo providerInfo = (SqlConnectionPoolProviderInfo) pool.ProviderInfo; 
                        // No lock since we are already in creation mutex 
                        instanceName = providerInfo.InstanceName;
                    } 

                    // options immutable - stored in global hash - don't modify
                    opt = new SqlConnectionString(opt, false, instanceName);
                    poolGroupProviderInfo = null; // null so we do not pass to constructor below... 
                }
                result = new SqlInternalConnectionTds(identity, opt, poolGroupProviderInfo, "", (SqlConnection)owningConnection, redirectedUserInstance); 
            } 
            return result;
        } 

        protected override DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions previous) {
            Debug.Assert(!ADP.IsEmpty(connectionString), "empty connectionString");
            SqlConnectionString result = new SqlConnectionString(connectionString); 
            return result;
        } 
 
        override internal DbConnectionPoolProviderInfo CreateConnectionPoolProviderInfo(DbConnectionOptions connectionOptions){
            DbConnectionPoolProviderInfo providerInfo = null; 

            if (((SqlConnectionString) connectionOptions).UserInstance) {
                providerInfo = new SqlConnectionPoolProviderInfo();
            } 

            return providerInfo; 
        } 

        override protected DbConnectionPoolGroupOptions CreateConnectionPoolGroupOptions( DbConnectionOptions connectionOptions ) { 
            SqlConnectionString opt = (SqlConnectionString)connectionOptions;

            DbConnectionPoolGroupOptions poolingOptions = null;
 
            if (!opt.ContextConnection && opt.Pooling) {    // never pool context connections.
                int connectionTimeout = opt.ConnectTimeout; 
 
                if ((0 < connectionTimeout) && (connectionTimeout < Int32.MaxValue/1000))
                    connectionTimeout *= 1000; 
                else if (connectionTimeout >= Int32.MaxValue/1000)
                    connectionTimeout = Int32.MaxValue;

                // NOTE: we use the deactivate queue because we may have to make 
                // a number of server round trips to unprepare statements.
                poolingOptions = new DbConnectionPoolGroupOptions( 
                                                    opt.IntegratedSecurity, 
                                                    opt.MinPoolSize,
                                                    opt.MaxPoolSize, 
                                                    connectionTimeout,
                                                    opt.LoadBalanceTimeout,
                                                    opt.Enlist,
                                                    false);  // useDeactivateQueue 
            }
            return poolingOptions; 
        } 

        override protected DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection, out bool cacheMetaDataFactory){ 
            Debug.Assert (internalConnection != null, "internalConnection may not be null.");
            cacheMetaDataFactory = false;

            if (internalConnection is SqlInternalConnectionSmi) { 
                throw SQL.NotAvailableOnContextConnection();
            } 
 
            NameValueCollection settings = (NameValueCollection)PrivilegedConfigurationManager.GetSection("system.data.sqlclient");
            Stream XMLStream =null; 
            if (settings != null){
                string [] values = settings.GetValues(_metaDataXml);
                if (values != null) {
                    XMLStream = ADP.GetXmlStreamFromValues(values, _metaDataXml); 
                }
            } 
 
            // if the xml was not obtained from machine.config use the embedded XML resource
            if (XMLStream == null){ 
                XMLStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("System.Data.SqlClient.SqlMetaData.xml");
                cacheMetaDataFactory = true;
            }
            Debug.Assert (XMLStream != null,"XMLstream may not be null."); 

            return new SqlMetaDataFactory (XMLStream, 
                                          internalConnection.ServerVersion, 
                                          internalConnection.ServerVersion); //internalConnection.ServerVersionNormalized);
 
        }

        override internal DbConnectionPoolGroupProviderInfo CreateConnectionPoolGroupProviderInfo (DbConnectionOptions connectionOptions) {
            return new SqlConnectionPoolGroupProviderInfo((SqlConnectionString)connectionOptions); 
        }
 
 
        internal static SqlConnectionString FindSqlConnectionOptions(string connectionString) {
            SqlConnectionString connectionOptions = (SqlConnectionString)SingletonInstance.FindConnectionOptions(connectionString); 
            if (null == connectionOptions) {
                connectionOptions = new SqlConnectionString(connectionString);
            }
            if (connectionOptions.IsEmpty) { 
                throw ADP.NoConnectionString();
            } 
            return connectionOptions; 
        }
 
        private SqlInternalConnectionSmi GetContextConnection(SqlConnectionString options, object providerInfo, DbConnection owningConnection) {
            SmiContext smiContext = SmiContextFactory.Instance.GetCurrentContext();

            SqlInternalConnectionSmi result = (SqlInternalConnectionSmi)smiContext.GetContextValue((int)SmiContextFactory.ContextKey.Connection); 

            // context connections are automatically re-useable if they exist unless they've been doomed. 
            if (null == result || result.IsConnectionDoomed) { 
                if (null != result) {
                    result.Dispose();   // A doomed connection is a messy thing.  Dispose of it promptly in nearest receptacle. 
                }

                result = new SqlInternalConnectionSmi(options, smiContext);
                smiContext.SetContextValue((int)SmiContextFactory.ContextKey.Connection, result); 
            }
 
            result.Activate(); 

            return result; 
        }

        override internal DbConnectionPoolGroup GetConnectionPoolGroup(DbConnection connection) {
            SqlConnection c = (connection as SqlConnection); 
            if (null != c) {
                return c.PoolGroup; 
            } 
            return null;
        } 

        override internal DbConnectionInternal GetInnerConnection(DbConnection connection) {
            SqlConnection c = (connection as SqlConnection);
            if (null != c) { 
                return c.InnerConnection;
            } 
            return null; 
        }
 
        override protected int GetObjectId(DbConnection connection) {
            SqlConnection c = (connection as SqlConnection);
            if (null != c) {
                return c.ObjectID; 
            }
            return 0; 
        } 

        override internal void PermissionDemand(DbConnection outerConnection) { 
            SqlConnection c = (outerConnection as SqlConnection);
            if (null != c) {
                c.PermissionDemand();
            } 
        }
 
        override internal void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup) { 
            SqlConnection c = (outerConnection as SqlConnection);
            if (null != c) { 
                c.PoolGroup = poolGroup;
            }
        }
 
        override internal void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to) {
            SqlConnection c = (owningObject as SqlConnection); 
            if (null != c) { 
                c.SetInnerConnectionEvent(to);
            } 
        }

        override internal bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from) {
            SqlConnection c = (owningObject as SqlConnection); 
            if (null != c) {
                return c.SetInnerConnectionFrom(to, from); 
            } 
            return false;
        } 

        override internal void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to) {
            SqlConnection c = (owningObject as SqlConnection);
            if (null != c) { 
                c.SetInnerConnectionTo(to);
            } 
        } 

    } 

    sealed internal class SqlPerformanceCounters : DbConnectionPoolCounters {
        private const string CategoryName = ".NET Data Provider for SqlServer";
        private const string CategoryHelp = "Counters for System.Data.SqlClient"; 

        public static readonly SqlPerformanceCounters SingletonInstance = new SqlPerformanceCounters(); 
 
        [System.Diagnostics.PerformanceCounterPermissionAttribute(System.Security.Permissions.SecurityAction.Assert, PermissionAccess=PerformanceCounterPermissionAccess.Write, MachineName=".", CategoryName=CategoryName)]
        private SqlPerformanceCounters() : base (CategoryName, CategoryHelp) { 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlConnectionFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient
{ 
    using System;
    using System.Data.Common;
    using System.Data.ProviderBase;
    using System.Collections.Specialized; 
    using System.Configuration;
    using System.Diagnostics; 
    using System.IO; 

    using Microsoft.SqlServer.Server; 


    sealed internal class SqlConnectionFactory : DbConnectionFactory {
        private SqlConnectionFactory() : base(SqlPerformanceCounters.SingletonInstance) {} 

        public static readonly SqlConnectionFactory SingletonInstance = new SqlConnectionFactory(); 
        private const string _metaDataXml          = "MetaDataXml"; 

        override public DbProviderFactory ProviderFactory { 
            get {
                return SqlClientFactory.Instance;
            }
        } 

        override protected DbConnectionInternal CreateConnection(DbConnectionOptions options, object poolGroupProviderInfo, DbConnectionPool pool, DbConnection owningConnection) { 
            SqlConnectionString opt = (SqlConnectionString)options; 
            SqlInternalConnection result = null;
 
            if (opt.ContextConnection) {
                result = GetContextConnection(opt, poolGroupProviderInfo, owningConnection);
            }
            else { 
                bool redirectedUserInstance       = false;
                DbConnectionPoolIdentity identity = null; 
 
                // Pass DbConnectionPoolIdentity to SqlInternalConnectionTds if using integrated security.
                // Used by notifications. 
                if (opt.IntegratedSecurity) {
                    if (pool != null) {
                        identity = pool.Identity;
                    } 
                    else {
                        identity = DbConnectionPoolIdentity.GetCurrent(); 
                    } 
                }
 
                // FOLLOWING IF BLOCK IS ENTIRELY FOR SSE USER INSTANCES
                // If "user instance=true" is in the connection string, we're using SSE user instances
                if (opt.UserInstance) {
                    redirectedUserInstance = true; 
                    string instanceName;
 
                    if ( (null == pool) || 
                         (null != pool && pool.Count <= 0) ) { // Non-pooled or pooled and no connections in the pool.
 
                        SqlInternalConnectionTds sseConnection = null;
                        try {
                            // What about a failure - throw?  YES!
                            sseConnection = new SqlInternalConnectionTds(identity, opt, null, "", null, false); 
                            instanceName = sseConnection.InstanceName;
 
                            if (!instanceName.StartsWith("\\\\.\\", StringComparison.Ordinal)) { 
                                throw SQL.NonLocalSSEInstance();
                            } 

                            if (null != pool) { // Pooled connection - cache result
                                SqlConnectionPoolProviderInfo providerInfo = (SqlConnectionPoolProviderInfo) pool.ProviderInfo;
                                // No lock since we are already in creation mutex 
                                providerInfo.InstanceName = instanceName;
                            } 
                        } 
                        finally {
                            if (null != sseConnection) { 
                                sseConnection.Dispose();
                            }
                        }
                    } 
                    else { // Cached info from pool.
                        SqlConnectionPoolProviderInfo providerInfo = (SqlConnectionPoolProviderInfo) pool.ProviderInfo; 
                        // No lock since we are already in creation mutex 
                        instanceName = providerInfo.InstanceName;
                    } 

                    // options immutable - stored in global hash - don't modify
                    opt = new SqlConnectionString(opt, false, instanceName);
                    poolGroupProviderInfo = null; // null so we do not pass to constructor below... 
                }
                result = new SqlInternalConnectionTds(identity, opt, poolGroupProviderInfo, "", (SqlConnection)owningConnection, redirectedUserInstance); 
            } 
            return result;
        } 

        protected override DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions previous) {
            Debug.Assert(!ADP.IsEmpty(connectionString), "empty connectionString");
            SqlConnectionString result = new SqlConnectionString(connectionString); 
            return result;
        } 
 
        override internal DbConnectionPoolProviderInfo CreateConnectionPoolProviderInfo(DbConnectionOptions connectionOptions){
            DbConnectionPoolProviderInfo providerInfo = null; 

            if (((SqlConnectionString) connectionOptions).UserInstance) {
                providerInfo = new SqlConnectionPoolProviderInfo();
            } 

            return providerInfo; 
        } 

        override protected DbConnectionPoolGroupOptions CreateConnectionPoolGroupOptions( DbConnectionOptions connectionOptions ) { 
            SqlConnectionString opt = (SqlConnectionString)connectionOptions;

            DbConnectionPoolGroupOptions poolingOptions = null;
 
            if (!opt.ContextConnection && opt.Pooling) {    // never pool context connections.
                int connectionTimeout = opt.ConnectTimeout; 
 
                if ((0 < connectionTimeout) && (connectionTimeout < Int32.MaxValue/1000))
                    connectionTimeout *= 1000; 
                else if (connectionTimeout >= Int32.MaxValue/1000)
                    connectionTimeout = Int32.MaxValue;

                // NOTE: we use the deactivate queue because we may have to make 
                // a number of server round trips to unprepare statements.
                poolingOptions = new DbConnectionPoolGroupOptions( 
                                                    opt.IntegratedSecurity, 
                                                    opt.MinPoolSize,
                                                    opt.MaxPoolSize, 
                                                    connectionTimeout,
                                                    opt.LoadBalanceTimeout,
                                                    opt.Enlist,
                                                    false);  // useDeactivateQueue 
            }
            return poolingOptions; 
        } 

        override protected DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection, out bool cacheMetaDataFactory){ 
            Debug.Assert (internalConnection != null, "internalConnection may not be null.");
            cacheMetaDataFactory = false;

            if (internalConnection is SqlInternalConnectionSmi) { 
                throw SQL.NotAvailableOnContextConnection();
            } 
 
            NameValueCollection settings = (NameValueCollection)PrivilegedConfigurationManager.GetSection("system.data.sqlclient");
            Stream XMLStream =null; 
            if (settings != null){
                string [] values = settings.GetValues(_metaDataXml);
                if (values != null) {
                    XMLStream = ADP.GetXmlStreamFromValues(values, _metaDataXml); 
                }
            } 
 
            // if the xml was not obtained from machine.config use the embedded XML resource
            if (XMLStream == null){ 
                XMLStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("System.Data.SqlClient.SqlMetaData.xml");
                cacheMetaDataFactory = true;
            }
            Debug.Assert (XMLStream != null,"XMLstream may not be null."); 

            return new SqlMetaDataFactory (XMLStream, 
                                          internalConnection.ServerVersion, 
                                          internalConnection.ServerVersion); //internalConnection.ServerVersionNormalized);
 
        }

        override internal DbConnectionPoolGroupProviderInfo CreateConnectionPoolGroupProviderInfo (DbConnectionOptions connectionOptions) {
            return new SqlConnectionPoolGroupProviderInfo((SqlConnectionString)connectionOptions); 
        }
 
 
        internal static SqlConnectionString FindSqlConnectionOptions(string connectionString) {
            SqlConnectionString connectionOptions = (SqlConnectionString)SingletonInstance.FindConnectionOptions(connectionString); 
            if (null == connectionOptions) {
                connectionOptions = new SqlConnectionString(connectionString);
            }
            if (connectionOptions.IsEmpty) { 
                throw ADP.NoConnectionString();
            } 
            return connectionOptions; 
        }
 
        private SqlInternalConnectionSmi GetContextConnection(SqlConnectionString options, object providerInfo, DbConnection owningConnection) {
            SmiContext smiContext = SmiContextFactory.Instance.GetCurrentContext();

            SqlInternalConnectionSmi result = (SqlInternalConnectionSmi)smiContext.GetContextValue((int)SmiContextFactory.ContextKey.Connection); 

            // context connections are automatically re-useable if they exist unless they've been doomed. 
            if (null == result || result.IsConnectionDoomed) { 
                if (null != result) {
                    result.Dispose();   // A doomed connection is a messy thing.  Dispose of it promptly in nearest receptacle. 
                }

                result = new SqlInternalConnectionSmi(options, smiContext);
                smiContext.SetContextValue((int)SmiContextFactory.ContextKey.Connection, result); 
            }
 
            result.Activate(); 

            return result; 
        }

        override internal DbConnectionPoolGroup GetConnectionPoolGroup(DbConnection connection) {
            SqlConnection c = (connection as SqlConnection); 
            if (null != c) {
                return c.PoolGroup; 
            } 
            return null;
        } 

        override internal DbConnectionInternal GetInnerConnection(DbConnection connection) {
            SqlConnection c = (connection as SqlConnection);
            if (null != c) { 
                return c.InnerConnection;
            } 
            return null; 
        }
 
        override protected int GetObjectId(DbConnection connection) {
            SqlConnection c = (connection as SqlConnection);
            if (null != c) {
                return c.ObjectID; 
            }
            return 0; 
        } 

        override internal void PermissionDemand(DbConnection outerConnection) { 
            SqlConnection c = (outerConnection as SqlConnection);
            if (null != c) {
                c.PermissionDemand();
            } 
        }
 
        override internal void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup) { 
            SqlConnection c = (outerConnection as SqlConnection);
            if (null != c) { 
                c.PoolGroup = poolGroup;
            }
        }
 
        override internal void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to) {
            SqlConnection c = (owningObject as SqlConnection); 
            if (null != c) { 
                c.SetInnerConnectionEvent(to);
            } 
        }

        override internal bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from) {
            SqlConnection c = (owningObject as SqlConnection); 
            if (null != c) {
                return c.SetInnerConnectionFrom(to, from); 
            } 
            return false;
        } 

        override internal void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to) {
            SqlConnection c = (owningObject as SqlConnection);
            if (null != c) { 
                c.SetInnerConnectionTo(to);
            } 
        } 

    } 

    sealed internal class SqlPerformanceCounters : DbConnectionPoolCounters {
        private const string CategoryName = ".NET Data Provider for SqlServer";
        private const string CategoryHelp = "Counters for System.Data.SqlClient"; 

        public static readonly SqlPerformanceCounters SingletonInstance = new SqlPerformanceCounters(); 
 
        [System.Diagnostics.PerformanceCounterPermissionAttribute(System.Security.Permissions.SecurityAction.Assert, PermissionAccess=PerformanceCounterPermissionAccess.Write, MachineName=".", CategoryName=CategoryName)]
        private SqlPerformanceCounters() : base (CategoryName, CategoryHelp) { 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
