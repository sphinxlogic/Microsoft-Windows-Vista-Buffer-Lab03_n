//------------------------------------------------------------------------------ 
// <copyright file="SqlConnection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="true" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

#if WINFSFunctionality 
    // UDTExtensions is a friend assembly - for UDT requirements
    // At some later time, would be convenient to have all assembly level attributes in one place.
    [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UdtExtensions, PublicKey=0024000004800000940000000602000000240000525341310004000001000100272736ad6e5f9586bac2d531eabc3acc666c2f8ec879fa94f8f7b0327d2ff2ed523448f83c3d5c5dd2dfc7bc99c5286b2c125117bf5cbe242b9d41750732b2bdffe649c6efb8e5526d526fdd130095ecdb7bf210809c6cdad8824faa9ac0310ac3cba2aa0523567b2dfa7fe250b30facbd62d4ec99b94ac47c7d3b28f1f6e4c8")] //
#endif 
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Data.DataSetExtensions, PublicKey="+AssemblyRef.EcmaPublicKeyFull)] //
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Data.Entity, PublicKey="+AssemblyRef.EcmaPublicKeyFull)] // SQLPT 300000492 
 
namespace System.Data.SqlClient
{ 
    using System;
    using System.Collections;
    using System.Configuration.Assemblies;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Common; 
    using System.Data.ProviderBase; 
    using System.Data.Sql;
    using System.Data.SqlTypes; 
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices; 
    using System.Runtime.Remoting; 
    using System.Runtime.Serialization.Formatters;
    using System.Text; 
    using System.Threading;
    using System.Security;
    using System.Security.Permissions;
    using System.Reflection; 

    using Microsoft.SqlServer.Server; 
 
    [DefaultEvent("InfoMessage")]
#if WINFSInternalOnly 
    internal
#else
    public
#endif 
    sealed partial class SqlConnection: DbConnection, ICloneable {
 
        static private readonly object EventInfoMessage = new object(); 

        private SqlDebugContext _sdc;   // SQL Debugging support 

        private bool    _AsycCommandInProgress;

        // SQLStatistics support 
        internal SqlStatistics _statistics;
        private bool _collectstats; 
 
        private bool _fireInfoMessageEventOnUserErrors; // False by default
 
#if WINFSFunctionality
        //this is the id of the current database. This is not perfect.
        //we will just cache the last active database that called GetUdtInfo()
        internal int _dbId; // making it internal so we can access this from SqlSerializationContext 
        internal string _catalog;
        IUdtSerializationContext _ctx; 
#endif 

        public SqlConnection(string connectionString) : this() { 
            ConnectionString = connectionString;
        }

        private SqlConnection(SqlConnection connection) { // Clone 
            GC.SuppressFinalize(this);
            CopyFrom(connection); 
#if WINFSFunctionality 
            _dbId = connection._dbId;
            _catalog = connection.Database; 
#endif
        }

        // 
        // PUBLIC PROPERTIES
        // 
 
        // used to start/stop collection of statistics data and do verify the current state
        // 
        // devnote: start/stop should not performed using a property since it requires execution of code
        //
        // start statistics
        //  set the internal flag (_statisticsEnabled) to true. 
        //  Create a new SqlStatistics object if not already there.
        //  connect the parser to the object. 
        //  if there is no parser at this time we need to connect it after creation. 
        //
 
        [
        DefaultValue(false),
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.SqlConnection_StatisticsEnabled), 
        ]
        public bool StatisticsEnabled { 
            get { 
                return (_collectstats);
            } 
            set {
                if (IsContextConnection) {
                    if (value) {
                        throw SQL.NotAvailableOnContextConnection(); 
                    }
                } 
                else { 
                    if (value) {
                        // start 
                        if (ConnectionState.Open == State) {
                            if (null == _statistics) {
                                _statistics = new SqlStatistics();
                                ADP.TimerCurrent(out _statistics._openTimestamp); 
                            }
                            // set statistics on the parser 
                            // update timestamp; 
                            Debug.Assert(Parser != null, "Where's the parser?");
                            Parser.Statistics = _statistics; 
                        }
                    }
                    else {
                        // stop 
                        if (null != _statistics) {
                            if (ConnectionState.Open == State) { 
                                // remove statistics from parser 
                                // update timestamp;
                                TdsParser parser = Parser; 
                                Debug.Assert(parser != null, "Where's the parser?");
                                parser.Statistics = null;
                                ADP.TimerCurrent(out _statistics._closeTimestamp);
                            } 
                        }
                    } 
                    this._collectstats = value; 
                }
            } 
        }

        internal bool AsycCommandInProgress  {
            get { 
                return (_AsycCommandInProgress);
            } 
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
            set {
                _AsycCommandInProgress = value; 
            }
        }

        internal bool IsContextConnection { 
            get {
                SqlConnectionString opt = (SqlConnectionString)ConnectionOptions; 
                bool result = false; 
                if (null != opt) {
                    result = (opt.ContextConnection); 
                }
                return result;
            }
        } 

        internal SqlConnectionString.TransactionBindingEnum TransactionBinding { 
            get { 
                return ((SqlConnectionString)ConnectionOptions).TransactionBinding;
            } 
        }

        internal SqlConnectionString.TypeSystem TypeSystem {
            get { 
                return ((SqlConnectionString)ConnectionOptions).TypeSystemVersion;
            } 
        } 

        override protected DbProviderFactory DbProviderFactory { 
            get {
                return SqlClientFactory.Instance;
            }
        } 

        [ 
        DefaultValue(""), 
        RecommendedAsConfigurable(true),
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        Editor("Microsoft.VSDesigner.Data.SQL.Design.SqlConnectionStringEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing),
        ResDescriptionAttribute(Res.SqlConnection_ConnectionString),
        ] 
        override public string ConnectionString {
            get { 
                return ConnectionString_Get(); 
            }
            set { 
                ConnectionString_Set(value);
            }
        }
 
        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResDescriptionAttribute(Res.SqlConnection_ConnectionTimeout), 
        ]
        override public int ConnectionTimeout { 
            get {
                SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                return ((null != constr) ? constr.ConnectTimeout : SqlConnectionString.DEFAULT.Connect_Timeout);
            } 
        }
 
        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.SqlConnection_Database), 
        ]
        override public string Database {
            // if the connection is open, we need to ask the inner connection what it's
            // current catalog is because it may have gotten changed, otherwise we can 
            // just return what the connection string had.
            get { 
                SqlInternalConnection innerConnection = (InnerConnection as SqlInternalConnection); 
                string result;
 
                if (null != innerConnection) {
                    result = innerConnection.CurrentDatabase;
                }
                else { 
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                    result = ((null != constr) ? constr.InitialCatalog : SqlConnectionString.DEFAULT.Initial_Catalog); 
                } 
                return result;
            } 
        }

        [
        Browsable(true), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.SqlConnection_DataSource), 
        ] 
        override public string DataSource {
            get { 
                SqlInternalConnection innerConnection = (InnerConnection as SqlInternalConnection);
                string result;

                if (null != innerConnection) { 
                    result = innerConnection.CurrentDataSource;
                } 
                else { 
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                    result = ((null != constr) ? constr.DataSource : SqlConnectionString.DEFAULT.Data_Source); 
                }
                return result;
            }
        } 

        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.SqlConnection_PacketSize), 
        ]
        public int PacketSize {
            // if the connection is open, we need to ask the inner connection what it's
            // current packet size is because it may have gotten changed, otherwise we 
            // can just return what the connection string had.
            get { 
                if (IsContextConnection) { 
                    throw SQL.NotAvailableOnContextConnection();
                } 

                SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds);
                int result;
 
                if (null != innerConnection) {
                    result = innerConnection.PacketSize; 
                } 
                else {
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions; 
                    result = ((null != constr) ? constr.PacketSize : SqlConnectionString.DEFAULT.Packet_Size);
                }
                return result;
            } 
        }
 
        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResDescriptionAttribute(Res.SqlConnection_ServerVersion),
        ]
        override public string ServerVersion {
            get { 
                return GetOpenConnection().ServerVersion;
            } 
        } 

        internal SqlStatistics Statistics { 
            get {
                return _statistics;
            }
        } 

        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.SqlConnection_WorkstationId), 
        ]
        public string WorkstationId {
            get {
                if (IsContextConnection) { 
                    throw SQL.NotAvailableOnContextConnection();
                } 
 
                // If not supplied by the user, the default value is the MachineName
                // Note: In Longhorn you'll be able to rename a machine without 
                // rebooting.  Therefore, don't cache this machine name.
                SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                string result = ((null != constr) ? constr.WorkstationId : null);
                if (null == result) { 
                    // getting machine name requires Environment.Permission
                    // user must have that permission in order to retrieve this 
                    result = Environment.MachineName; 
                }
                return result; 
            }
        }

        // 
        // PUBLIC EVENTS
        // 
 
        [
        ResCategoryAttribute(Res.DataCategory_InfoMessage), 
        ResDescriptionAttribute(Res.DbConnection_InfoMessage),
        ]
        public event SqlInfoMessageEventHandler InfoMessage {
            add { 
                Events.AddHandler(EventInfoMessage, value);
            } 
            remove { 
                Events.RemoveHandler(EventInfoMessage, value);
            } 
        }

        public bool FireInfoMessageEventOnUserErrors {
            get { 
                return _fireInfoMessageEventOnUserErrors;
            } 
            set { 
                _fireInfoMessageEventOnUserErrors = value;
            } 
        }

        //
        // PUBLIC METHODS 
        //
 
        new public SqlTransaction BeginTransaction() { 
            // this is just a delegate. The actual method tracks executiontime
            return BeginTransaction(IsolationLevel.Unspecified, null); 
        }

        new public SqlTransaction BeginTransaction(IsolationLevel iso) {
            // this is just a delegate. The actual method tracks executiontime 
            return BeginTransaction(iso, null);
        } 
 
        public SqlTransaction BeginTransaction(string transactionName) {
                // Use transaction names only on the outermost pair of nested 
                // BEGIN...COMMIT or BEGIN...ROLLBACK statements.  Transaction names
                // are ignored for nested BEGIN's.  The only way to rollback a nested
                // transaction is to have a save point from a SAVE TRANSACTION call.
                return BeginTransaction(IsolationLevel.Unspecified, transactionName); 
        }
 
        public SqlTransaction BeginTransaction(IsolationLevel iso, string transactionName) { 
            SqlStatistics statistics = null;
            IntPtr hscp; 
            string xactName =  ADP.IsEmpty(transactionName)? "None" : transactionName;
            Bid.ScopeEnter(out hscp, "<sc.SqlConnection.BeginTransaction|API> %d#, iso=%d{ds.IsolationLevel}, transactionName='%ls'\n", ObjectID, (int)iso,
                        xactName);
 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
 
                // NOTE: we used to throw an exception if the transaction name was empty
                // (see MDAC 50292) but we have a BeginTransaction method that doesn't 
                // have a transactionName argument.
                SqlTransaction transaction = GetOpenConnection().BeginSqlTransaction(iso, transactionName);

                // 

 
                GC.KeepAlive(this); 

                return transaction; 
            }
            finally {
                Bid.ScopeLeave(ref hscp);
                SqlStatistics.StopTimer(statistics); 
            }
        } 
 
        override public void ChangeDatabase(string database) {
            SqlStatistics statistics = null; 

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    statistics = SqlStatistics.StartTimer(Statistics);
                    InnerConnection.ChangeDatabase(database);
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG 
            }
            catch (System.OutOfMemoryException e) {
                Abort(e);
                throw; 
            }
            catch (System.StackOverflowException e) { 
                Abort(e); 
                throw;
            } 
            catch (System.Threading.ThreadAbortException e) {
                Abort(e);
                throw;
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            } 
        }
 
        static public void ClearAllPools() {
            (new SqlClientPermission(PermissionState.Unrestricted)).Demand();
            SqlConnectionFactory.SingletonInstance.ClearAllPools();
        } 

        static public void ClearPool(SqlConnection connection) { 
            ADP.CheckArgumentNull(connection, "connection"); 

            DbConnectionOptions connectionOptions = connection.UserConnectionOptions; 
            if (null != connectionOptions) {
                connectionOptions.DemandPermission();
                if (connection.IsContextConnection) {
                    throw SQL.NotAvailableOnContextConnection(); 
                }
                SqlConnectionFactory.SingletonInstance.ClearPool(connection); 
            } 
        }
 
        object ICloneable.Clone() {
            SqlConnection clone = new SqlConnection(this);
            Bid.Trace("<sc.SqlConnection.Clone|API> %d#, clone=%d#\n", ObjectID, clone.ObjectID);
            return clone; 
        }
 
        override public void Close() { 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlConnection.Close|API> %d#" , ObjectID); 
            try {
                SqlStatistics statistics = null;

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
#if DEBUG 
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                        statistics = SqlStatistics.StartTimer(Statistics); 

                        // The lock here is to protect against the command.cancel / connection.close race condition 
                        // The SqlInternalConnectionTds is set to OpenBusy during close, once this happens the cast below will fail and 
                        // the command will no longer be cancelable.  It might be desirable to be able to cancel the close opperation, but this is
                        // outside of the scope of Whidbey RTM.  See (SqlCommand::Cancel) for other lock. 
                        lock (InnerConnection) {
                            InnerConnection.CloseConnection(this, ConnectionFactory);
                        }
                        // does not require GC.KeepAlive(this) because of OnStateChange 

                        if (null != Statistics) { 
                            ADP.TimerCurrent(out _statistics._closeTimestamp); 
                        }
 #if DEBUG 
                    }
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                    } 
#endif //DEBUG
                } 
                catch (System.OutOfMemoryException e) { 
                    Abort(e);
                    throw; 
                }
                catch (System.StackOverflowException e) {
                    Abort(e);
                    throw; 
                }
                catch (System.Threading.ThreadAbortException e) { 
                    Abort(e); 
                    throw;
                } 
                finally {
                    SqlStatistics.StopTimer(statistics);
                }
            } 
            finally {
                SqlDebugContext  sdc = _sdc; 
                _sdc = null; 
                Bid.ScopeLeave(ref hscp);
                if (sdc != null) { 
                   sdc.Dispose();
                }
            }
        } 

        new public SqlCommand CreateCommand() { 
            return new SqlCommand(null, this); 
        }
 
        private void DisposeMe(bool disposing) { // MDAC 65459
        }

        public void EnlistDistributedTransaction(System.EnterpriseServices.ITransaction transaction) { 
            if (IsContextConnection) {
                throw SQL.NotAvailableOnContextConnection(); 
            } 

            EnlistDistributedTransactionHelper(transaction); 
        }

        override public void Open() {
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlConnection.Open|API> %d#", ObjectID) ;
            try { 
                if (StatisticsEnabled) { 
                    if (null == _statistics) {
                        _statistics = new SqlStatistics(); 
                    }
                    else {
                        _statistics.ContinueOnNewConnection();
                    } 
                }
 
                SqlStatistics statistics = null; 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
#if DEBUG
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
                        Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG 
                        statistics = SqlStatistics.StartTimer(Statistics);
 
                        InnerConnection.OpenConnection(this, ConnectionFactory);
                        // does not require GC.KeepAlive(this) because of OnStateChange

                        SqlInternalConnectionSmi innerConnection = (InnerConnection as SqlInternalConnectionSmi); 
                        if (null != innerConnection) {
                            innerConnection.AutomaticEnlistment(); 
                        } 
                        else {
                            Debug.Assert(Parser != null, "Where's the parser?"); 

                            if (StatisticsEnabled) {
                                ADP.TimerCurrent(out _statistics._openTimestamp);
                                Parser.Statistics = _statistics; 
                            }
                            else { 
                                Parser.Statistics = null; 
                                _statistics = null; // in case of previous Open/Close/reset_CollectStats sequence
                            } 
                            CompleteOpen();
                        }
#if DEBUG
                    } 
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                    } 
#endif //DEBUG
                } 
                catch (System.OutOfMemoryException e) {
                    Abort(e);
                    throw;
                } 
                catch (System.StackOverflowException e) {
                    Abort(e); 
                    throw; 
                }
                catch (System.Threading.ThreadAbortException e) { 
                    Abort(e);
                    throw;
                }
                finally { 
                    SqlStatistics.StopTimer(statistics);
                } 
            } 
            finally {
                Bid.ScopeLeave(ref hscp) ; 
            }
        }

 
        //
        // INTERNAL PROPERTIES 
        // 

#if WINFSFunctionality 
        internal AssemblyCache AssemblyCache {
            get {
                return GetOpenTdsConnection().AssemblyCache;
            } 
        }
#endif 
 
        internal bool HasLocalTransaction {
            get { 
                return GetOpenConnection().HasLocalTransaction;
            }
        }
 
        internal bool HasLocalTransactionFromAPI {
            get { 
                return GetOpenConnection().HasLocalTransactionFromAPI; 
            }
        } 

        internal bool IsShiloh {
            get {
                return GetOpenConnection().IsShiloh; 
            }
        } 
 
        internal bool IsYukonOrNewer {
            get { 
                return GetOpenConnection().IsYukonOrNewer;
            }
        }
 
        internal bool IsKatmaiOrNewer {
            get { 
                return GetOpenConnection().IsKatmaiOrNewer; 
            }
        } 

        internal TdsParser Parser {
            get {
                SqlInternalConnectionTds tdsConnection = (GetOpenConnection() as SqlInternalConnectionTds); 
                if (null == tdsConnection) {
                    throw SQL.NotAvailableOnContextConnection(); 
                } 
                return tdsConnection.Parser;
            } 
        }

        internal bool Asynchronous {
            get { 
                SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                return ((null != constr) ? constr.Asynchronous : SqlConnectionString.DEFAULT.Asynchronous); 
            } 
        }
 
        //
        // INTERNAL METHODS
        //
 
        internal void AddPreparedCommand(SqlCommand cmd) {
            GetOpenConnection().AddPreparedCommand(cmd); 
        } 

        internal void ValidateConnectionForExecute(string method, SqlCommand command) { 
            SqlInternalConnection innerConnection = GetOpenConnection(method);
            innerConnection.ValidateConnectionForExecute(command);
        }
 
        // Surround name in brackets and then escape any end bracket to protect against SQL Injection.
        // NOTE: if the user escapes it themselves it will not work, but this was the case in V1 as well 
        // as native OleDb and Odbc. 
        static internal string FixupDatabaseTransactionName(string name) {
            if (!ADP.IsEmpty(name)) { 
                return "[" + name.Replace("]", "]]") + "]";
            }
            else {
                return name; 
            }
        } 
 
        internal void OnError(SqlException exception, bool breakConnection) {
            Debug.Assert(exception != null && exception.Errors.Count != 0, "SqlConnection: OnError called with null or empty exception!"); 

            // Bug fix - MDAC 49022 - connection open after failure...  Problem was parser was passing
            // Open as a state - because the parser's connection to the netlib was open.  We would
            // then set the connection state to the parser's state - which is not correct.  The only 
            // time the connection state should change to what is passed in to this function is if
            // the parser is broken, then we should be closed.  Changed to passing in 
            // TdsParserState, not ConnectionState. 
            // fixed by [....]
 
            if (breakConnection && (ConnectionState.Open == State)) {
                Bid.Trace("<sc.SqlConnection.OnError|INFO> %d#, Connection broken.\n", ObjectID) ;
                this.Close();
            } 

            if (exception.Class >= TdsEnums.MIN_ERROR_CLASS) { 
                // It is an error, and should be thrown.  Class of TdsEnums.MIN_ERROR_CLASS or above is an error, 
                // below TdsEnums.MIN_ERROR_CLASS denotes an info message.
                throw exception; 
            }
            else {
                // If it is a class < TdsEnums.MIN_ERROR_CLASS, it is a warning collection - so pass to handler
                this.OnInfoMessage(new SqlInfoMessageEventArgs(exception)); 
            }
        } 
 
        internal void RemovePreparedCommand(SqlCommand cmd) {
            GetOpenConnection().RemovePreparedCommand(cmd); 
        }

        //
        // PRIVATE METHODS 
        //
 
        private void CompleteOpen() { 
            Debug.Assert(ConnectionState.Open == State, "CompleteOpen not open");
            // be sure to mark as open so SqlDebugCheck can issue Query 

            // check to see if we need to hook up sql-debugging if a debugger is attached
            // We only need this check for Shiloh and earlier servers.
            if (!GetOpenConnection().IsYukonOrNewer && 
                    System.Diagnostics.Debugger.IsAttached) {
                bool debugCheck = false; 
                try { 
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand(); // MDAC 66682, 69017
                    debugCheck = true; 
                }
                catch (SecurityException e) {
                    ADP.TraceExceptionWithoutRethrow(e);
                } 

                if (debugCheck) { 
                    // if we don't have Unmanaged code permission, don't check for debugging 
                    // but let the connection be opened while under the debugger
                    CheckSQLDebugOnConnect(); 
                }
            }
        }
 
        internal SqlInternalConnection GetOpenConnection() {
            SqlInternalConnection innerConnection = (InnerConnection as SqlInternalConnection); 
            if (null == innerConnection) { 
                throw ADP.ClosedConnectionError();
            } 
            return innerConnection;
        }

        internal SqlInternalConnection GetOpenConnection(string method) { 
            DbConnectionInternal innerConnection = InnerConnection;
            SqlInternalConnection innerSqlConnection = (innerConnection as SqlInternalConnection); 
            if (null == innerSqlConnection) { 
                throw ADP.OpenConnectionRequired(method, innerConnection.State);
            } 
            return innerSqlConnection;
        }

        internal SqlInternalConnectionTds GetOpenTdsConnection() { 
            SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds);
            if (null == innerConnection) { 
                throw ADP.ClosedConnectionError(); 
            }
            return innerConnection; 
        }

        internal SqlInternalConnectionTds GetOpenTdsConnection(string method) {
            SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds); 
            if (null == innerConnection) {
                throw ADP.OpenConnectionRequired(method, innerConnection.State); 
            } 
            return innerConnection;
        } 

        internal void OnInfoMessage(SqlInfoMessageEventArgs imevent) {
            if (Bid.TraceOn) {
                Debug.Assert(null != imevent, "null SqlInfoMessageEventArgs"); 
                Bid.Trace("<sc.SqlConnection.OnInfoMessage|API|INFO> %d#, Message='%ls'\n", ObjectID, ((null != imevent) ? imevent.Message : ""));
            } 
            SqlInfoMessageEventHandler handler = (SqlInfoMessageEventHandler)Events[EventInfoMessage]; 
            if (null != handler) {
                try { 
                    handler(this, imevent);
                }
                catch (Exception e) { // MDAC 53175
                    if (!ADP.IsCatchableOrSecurityExceptionType(e)) { 
                        throw;
                    } 
 
                    ADP.TraceExceptionWithoutRethrow(e);
                } 
            }
        }

        // 
        // SQL DEBUGGING SUPPORT
        // 
 
        // this only happens once per connection
        private void CheckSQLDebugOnConnect() { 
            IntPtr hFileMap;
            uint pid = (uint)SafeNativeMethods.GetCurrentProcessId();

            string mapFileName; 

            // If Win2k or later, prepend "Global\\" to enable this to work through TerminalServices. 
            if (ADP.IsPlatformNT5) { 
                mapFileName = "Global\\" + TdsEnums.SDCI_MAPFILENAME;
            } 
            else {
                mapFileName = TdsEnums.SDCI_MAPFILENAME;
            }
 
            mapFileName = mapFileName + pid.ToString(CultureInfo.InvariantCulture);
 
            hFileMap = NativeMethods.OpenFileMappingA(0x4/*FILE_MAP_READ*/, false, mapFileName); 

            if (ADP.PtrZero != hFileMap) { 
                IntPtr pMemMap = NativeMethods.MapViewOfFile(hFileMap, 0x4/*FILE_MAP_READ*/, 0, 0, IntPtr.Zero);
                if (ADP.PtrZero != pMemMap) {
                    SqlDebugContext sdc = new SqlDebugContext();
                    sdc.hMemMap = hFileMap; 
                    sdc.pMemMap = pMemMap;
                    sdc.pid = pid; 
 
                    // optimization: if we only have to refresh memory-mapped data at connection open time
                    // optimization: then call here instead of in CheckSQLDebug() which gets called 
                    // optimization: at command execution time
                    // RefreshMemoryMappedData(sdc);

                    // delaying setting out global state until after we issue this first SQLDebug command so that 
                    // we don't reentrantly call into CheckSQLDebug
                    CheckSQLDebug(sdc); 
                    // now set our global state 
                    _sdc = sdc;
                } 
            }
        }

        // This overload is called by the Command object when executing stored procedures.  Note that 
        // if SQLDebug has never been called, it is a noop.
        internal void CheckSQLDebug() { 
            if (null != _sdc) 
                CheckSQLDebug(_sdc);
        } 

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)] // MDAC 66682, 69017
        private void CheckSQLDebug(SqlDebugContext sdc) {
            // check to see if debugging has been activated 
            Debug.Assert(null != sdc, "SQL Debug: invalid null debugging context!");
 
#pragma warning disable 618 
            uint tid = (uint)AppDomain.GetCurrentThreadId();    // Sql Debugging doesn't need fiber support;
#pragma warning restore 618 
            RefreshMemoryMappedData(sdc);

            //
 

 
            // If we get here, the debugger must be hooked up. 
            if (!sdc.active) {
                if (sdc.fOption/*TdsEnums.SQLDEBUG_ON*/) { 
                    // turn on
                    sdc.active = true;
                    sdc.tid = tid;
                    try { 
                        IssueSQLDebug(TdsEnums.SQLDEBUG_ON, sdc.machineName, sdc.pid, sdc.dbgpid, sdc.sdiDllName, sdc.data);
                        sdc.tid = 0; // reset so that the first successful time through, we notify the server of the context switch 
                    } 
                    catch {
                        sdc.active = false; 
                        throw;
                    }
                }
            } 

            // be sure to pick up thread context switch, especially the first time through 
            if (sdc.active) { 
                if (!sdc.fOption/*TdsEnums.SQLDEBUG_OFF*/) {
                    // turn off and free the memory 
                    sdc.Dispose();
                    // okay if we throw out here, no state to clean up
                    IssueSQLDebug(TdsEnums.SQLDEBUG_OFF, null, 0, 0, null, null);
                } 
                else {
                    // notify server of context change 
                    if (sdc.tid != tid) { 
                        sdc.tid = tid;
                        try { 
                            IssueSQLDebug(TdsEnums.SQLDEBUG_CONTEXT, null, sdc.pid, sdc.tid, null, null);
                        }
                        catch {
                            sdc.tid = 0; 
                            throw;
                        } 
                    } 
                }
            } 
        }

        private void IssueSQLDebug(uint option, string machineName, uint pid, uint id, string sdiDllName, byte[] data) {
 
            if (GetOpenConnection().IsYukonOrNewer) {
                // 
                return; 
            }
 
            //

            SqlCommand c = new SqlCommand(TdsEnums.SP_SDIDEBUG, this);
            c.CommandType = CommandType.StoredProcedure; 

            // context param 
            SqlParameter p = new SqlParameter(null, SqlDbType.VarChar, TdsEnums.SQLDEBUG_MODE_NAMES[option].Length); 
            p.Value = TdsEnums.SQLDEBUG_MODE_NAMES[option];
            c.Parameters.Add(p); 

            if (option == TdsEnums.SQLDEBUG_ON) {
                // debug dll name
                p = new SqlParameter(null, SqlDbType.VarChar, sdiDllName.Length); 
                p.Value = sdiDllName;
                c.Parameters.Add(p); 
                // debug machine name 
                p = new SqlParameter(null, SqlDbType.VarChar, machineName.Length);
                p.Value = machineName; 
                c.Parameters.Add(p);
            }

            if (option != TdsEnums.SQLDEBUG_OFF) { 
                // client pid
                p = new SqlParameter(null, SqlDbType.Int); 
                p.Value = pid; 
                c.Parameters.Add(p);
                // dbgpid or tid 
                p = new SqlParameter(null, SqlDbType.Int);
                p.Value = id;
                c.Parameters.Add(p);
            } 

            if (option == TdsEnums.SQLDEBUG_ON) { 
                // debug data 
                p = new SqlParameter(null, SqlDbType.VarBinary, (null != data) ? data.Length : 0);
                p.Value = data; 
                c.Parameters.Add(p);
            }

            c.ExecuteNonQuery(); 
        }
 
        public static void ChangePassword(string connectionString, string newPassword) { 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlConnection.ChangePassword|API>") ; 
            try {
                if (ADP.IsEmpty(connectionString)) {
                    throw SQL.ChangePasswordArgumentMissing("connectionString");
                } 
                if (ADP.IsEmpty(newPassword)) {
                    throw SQL.ChangePasswordArgumentMissing("newPassword"); 
                } 
                if (TdsEnums.MAXLEN_NEWPASSWORD < newPassword.Length) {
                    throw ADP.InvalidArgumentLength("newPassword", TdsEnums.MAXLEN_NEWPASSWORD); 
                }

                SqlConnectionString connectionOptions = SqlConnectionFactory.FindSqlConnectionOptions(connectionString);
                if (connectionOptions.IntegratedSecurity) { 
                    throw SQL.ChangePasswordConflictsWithSSPI();
                } 
                if (! ADP.IsEmpty(connectionOptions.AttachDBFilename)) { 
                    throw SQL.ChangePasswordUseOfUnallowedKey(SqlConnectionString.KEY.AttachDBFilename);
                } 
                if (connectionOptions.ContextConnection) {
                    throw SQL.ChangePasswordUseOfUnallowedKey(SqlConnectionString.KEY.Context_Connection);
                }
 
                System.Security.PermissionSet permissionSet = connectionOptions.CreatePermissionSet();
                permissionSet.Demand(); 
 
                // note: This is the only case where we directly construt the internal connection, passing in the new password.
                // Normally we would simply create a regular connectoin and open it but there is no other way to pass the 
                // new password down to the constructor. Also it would have an unwanted impact on the connection pool
                //
                using (SqlInternalConnectionTds con = new SqlInternalConnectionTds(null, connectionOptions, null, newPassword, (SqlConnection)null, false)) {
                    if (!con.IsYukonOrNewer) { 
                        throw SQL.ChangePasswordRequiresYukon();
                    } 
                } 
                SqlConnectionFactory.SingletonInstance.ClearPool(connectionString);
            } 
            finally {
                Bid.ScopeLeave(ref hscp) ;
            }
        } 

#if WINFSFunctionality 
        static private volatile bool     _searched              = false; 
        static private          Assembly _udtExtensionsAssembly = null;
        static private          object   _lockobj               = new object(); 
        static private          Type     _serializationHelper   = null;

        static private void CheckLoadUDTExtensions() {
            if (!_searched) { 
                lock (_lockobj) {
                    if (!_searched) { 
                        try { 
                            AssemblyName assemblyName = new AssemblyName();
                            assemblyName.Name = "udtextensions"; 
                            assemblyName.Version = new Version("9.0.242.0");
                            assemblyName.SetPublicKeyToken(new byte[]{0x89,0x84,0x5d,0xcd,0x80,0x80,0xcc,0x91});
                            assemblyName.CultureInfo = CultureInfo.InvariantCulture;
 
                            _udtExtensionsAssembly = Assembly.Load(assemblyName);
 
                            _serializationHelper = _udtExtensionsAssembly.GetType("System.Data.Sql.SerializationHelper"); 
                        }
                        catch (FileNotFoundException) { 
                            _udtExtensionsAssembly = null; // reset to null in case GetType fails
                            // OPEN WIDE!!!
                        }
                        finally { 
                            _searched = true;
                        } 
                    } 
                }
            } 
        }

        static internal object Deserialize(Stream stream, Type type, IUdtSerializationContext context) {
            CheckLoadUDTExtensions(); 

            object obj = null; 
 
            Debug.Assert(null != _serializationHelper, "Why are we calling Deserialize with no assembly?");
 
            if (null != _serializationHelper) {
                obj = _serializationHelper.InvokeMember("Deserialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                                                        (Binder)null, (object)null, new object[] {stream, type, context}, (CultureInfo)null);
            } 

            return obj; 
        } 

        static internal object Serialize(Stream stream, object instance, IUdtSerializationContext context) { 
            CheckLoadUDTExtensions();

            object obj = null;
 
            Debug.Assert(null != _serializationHelper, "Why are we calling Serialize with no assembly?");
 
            if (null != _serializationHelper) { 
                obj = _serializationHelper.InvokeMember("Serialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                                                        (Binder)null, (object)null, new object[] {stream, instance, context}, (CultureInfo)null); 
            }

            return obj;
        } 

        static internal int SizeInBytes(object instance) { 
            CheckLoadUDTExtensions(); 

            int size = 0; 

            Debug.Assert(null != _serializationHelper, "Why are we calling Deserialize with no assembly?");

            if (null != _serializationHelper) { 
                size = (int) _serializationHelper.InvokeMember("SizeInBytes", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                                                         (Binder)null, (object)null, new object[] {instance}, (CultureInfo)null); 
            } 

            return size; 
        }
#endif

        // updates our context with any changes made to the memory-mapped data by an external process 
        static private void RefreshMemoryMappedData(SqlDebugContext sdc) {
            Debug.Assert(ADP.PtrZero != sdc.pMemMap, "SQL Debug: invalid null value for pMemMap!"); 
            // copy memory mapped file contents into managed types 
            MEMMAP memMap = (MEMMAP)Marshal.PtrToStructure(sdc.pMemMap, typeof(MEMMAP));
            sdc.dbgpid = memMap.dbgpid; 
            sdc.fOption = (memMap.fOption == 1) ? true : false;
            // xlate ansi byte[] -> managed strings
            Encoding cp = System.Text.Encoding.GetEncoding(TdsEnums.DEFAULT_ENGLISH_CODE_PAGE_VALUE);
            sdc.machineName = cp.GetString(memMap.rgbMachineName, 0, memMap.rgbMachineName.Length); 
            sdc.sdiDllName = cp.GetString(memMap.rgbDllName, 0, memMap.rgbDllName.Length);
            // just get data reference 
            sdc.data = memMap.rgbData; 
        }
 
        public void ResetStatistics() {
            if (IsContextConnection) {
                throw SQL.NotAvailableOnContextConnection();
            } 

            if (null != Statistics) { 
                Statistics.Reset(); 
                if (ConnectionState.Open == State) {
                    // update timestamp; 
                    ADP.TimerCurrent(out _statistics._openTimestamp);
                }
            }
        } 

        public IDictionary RetrieveStatistics() { 
            if (IsContextConnection) { 
                throw SQL.NotAvailableOnContextConnection();
            } 

            if (null != Statistics) {
                UpdateStatistics();
                return Statistics.GetHashtable(); 
            }
            else { 
                return new SqlStatistics().GetHashtable(); 
            }
        } 

        private void UpdateStatistics() {
            if (ConnectionState.Open == State) {
                // update timestamp 
                ADP.TimerCurrent(out _statistics._closeTimestamp);
            } 
            // delegate the rest of the work to the SqlStatistics class 
            Statistics.UpdateStatistics();
        } 

        //
        // UDT SUPPORT
        // 

        // 
        internal static void CheckGetExtendedUDTInfo(SqlMetaDataPriv metaData, bool fThrow) { 
            if (metaData.udtType == null) { // If null, we have not obtained extended info.
                Debug.Assert(!ADP.IsEmpty(metaData.udtAssemblyQualifiedName), "Unexpected state on GetUDTInfo"); 
                // 2nd argument determines whether exception from Assembly.Load is thrown.
    			metaData.udtType = Type.GetType(metaData.udtAssemblyQualifiedName, fThrow);
                if (fThrow && metaData.udtType == null) {
                    // 
                    throw SQL.UDTUnexpectedResult(metaData.udtAssemblyQualifiedName);
                } 
            } 
        }
 
#if WINFSFunctionality
        private bool GetUdtInfo(ref int dbId, int typeId) {
            int aId = 0;
            String typeName = null; 
            String className = null;
            SqlCommand cmd; 
            SqlParameter p; 
            bool result = true;
            bool isInstantiated = false; 
            bool foundInstantiated = false;
            Int32 genericId = 0;
            int index = 0;
            int typeIdNew = 0; 
            int instantiatedTypeId = 0;	// instanced id of the generic type
            int[] list = new int[1]; 
 
            Debug.Assert(this.AssemblyCache != null, "Cache object is NULL");
            _catalog = this.Database; 

            SqlConnection  con   = null;
            SqlTransaction trans = null; // Pass to connection if using MARS.
 
            try {
                if (this.Parser.MARSOn) { 
                    con   = this; 
                    // NOTE - the following line of code will need to be modified if SqlClient is going
                    // to support parallel transactions.  In that case we will be required to pass the 
                    // transaction all the way down.
                    // NOTE - currently due to the server implementation of UDT materialization, if the
                    // client creates the type and tries to materialize the type through a select in the
                    // same transaction - the user will not be able to if MARS is off.  In that case 
                    // we would have to open a new connection and since we would not be running under the
                    // same transaction at that point, we would not be able to select the UDT info. 
                    // 
                    SqlInternalTransaction currentTransaction = this.Parser.CurrentTransaction;
                    if (null != currentTransaction) { 
                        trans = (SqlTransaction)(currentTransaction.Parent);
                    }
                }
                else { 
                    con = (SqlConnection) ((ICloneable) this).Clone();
                    con.Open(); 
                } 

                Debug.Assert(con.GetOpenConnection().IsWinFS, "Should not enter this code path against non-WinFS server!"); 

                // Call the sp with fully qualified name. This is to avoid generating dynamic sql on the server
                // Also, cloned connections default to the login's database.
                // Also - modified sproc call to obtain DbId since we no longer receive in TDS.  This value is also 
                // returned and cached on the client.
                cmd = new SqlCommand("set @typeDbId=db_id();exec [" + _catalog + "].sys." + TdsEnums.SP_UDTINFO_WINFS + " @dbid=@typeDbId, @id=@typeId", con, trans); 
                cmd.CommandType = CommandType.Text; 

                p = cmd.Parameters.Add("@typeDbId", SqlDbType.Int); 
                p.Value = dbId;
                p = cmd.Parameters.Add("@typeId", SqlDbType.Int);
                p.Value = typeId;
 
                using (SqlDataReader r = cmd.ExecuteReader()) {
                    //Read info regarding UDT. 
                    //see if we are dealing with collection valued UDTs or simple UDTs 
                    while (r.Read()) {
                        AssemblyName aName = new AssemblyName(); 

                        dbId = r.GetInt32(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_DB_ID));
                        _dbId = dbId; // Since we no longer have DbId from TDS, obtain from server and cache.
 
                        typeIdNew = r.GetInt32(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_TYPE_ID));
                        isInstantiated = r.GetBoolean(r.GetOrdinal(UDTINFOWinFS_Text.UDT_IS_INSTANTIATED_TYPE)); 
 
                        if (!isInstantiated) {
                            // isInstantiated == true means it's an istantiation of generic UDT 
                            aId = r.GetInt32(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_ASSEMBLY_ID));
                        }
                        else {
                            aId = 0; 
                            Debug.Assert(instantiatedTypeId == 0, "Attempted to assign instantiated type id twice");
                            instantiatedTypeId = typeIdNew; 
                        } // do we need typeIdNew in this case? 

                        //skip maxlen,  for now 
                        typeName = r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_CATALOG_NAME)) + "." + r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_SCHEMA_NAME)) + "." + r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_NAME));

                        if (!isInstantiated) { // again it's not instantiated means its a regular UDT
                            className = r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_BOUND_CLASS)); 
                            aName = GetAssemblyName(r, con);
                        } 
                        else { 
                            Debug.Assert(!foundInstantiated, "MultiValued Type appeared twice in the result set!");
                            foundInstantiated = true; 
                            genericId = r.GetInt32(r.GetOrdinal(UDTINFOWinFS_Text.UDT_GENERIC_TYPE));
                            aId = 0;
                            className = String.Empty;
                        } 

                        if (foundInstantiated) 
                            index = r.GetInt32(r.GetOrdinal(UDTINFOWinFS_Text.UDT_PARAMETER_ORDINAL));//index of this type in the generic parameter list 

                        // index=0 means it's the instantiated type, index>0 is types of typeparameter 
                        // instantiated types dont have assemblies.
                        if (!isInstantiated && false == this.AssemblyCache.AddAssemblyToCache(dbId, aId, aName, 1))
                            throw SQL.UDTUnexpectedResult(aName.ToString());
 
                        //register the type in all cases.
                        if (false == this.AssemblyCache.AddTypeRefToCache(dbId, typeIdNew, typeName, className, aId, isInstantiated)) 
                            throw SQL.UDTUnexpectedResult(aName.ToString()); 

                        if (foundInstantiated && (index != 0)) { 
                            //make sure this type if present before we exit this function
                            //we could do a defered download but it may complicate things.
                            //Keep it simple for now. Download all parameter type assemblies
                            EnsureAssembly(dbId, aId, aName); 
                            Debug.Assert(index <= 1, "Not supposed to have more than 1 generic parameter!");
                            if (index > 1) 
                                throw SQL.UDTUnexpectedResult(aName.ToString()); 
                            list[index - 1] = typeIdNew;
                        } 

                        if (!foundInstantiated)
                            break; //for normal types, there is only one row
                    } // end of while loop! 

                    //shouldn't have anymore rows 
                    if (true == r.Read()) 
                        throw SQL.UDTUnexpectedResult(typeName);
 
                    if (foundInstantiated) {
                        //add the type ids to the generic type
                        TypeInfo info = this.AssemblyCache.GetTypeInfo(dbId, instantiatedTypeId);
                        Debug.Assert(info != null, "BAD STATE!!"); 
                        Debug.Assert(info.isInstantiated, "Adding Generic parameters to non-generic type!!");
                        info.genericType = genericId; 
                        info.parameters = new int[index]; 
                        for (int i = 0; i < index; i++) {
                            info.parameters[i] = list[i]; 
                        }
                    }
                }
            } 
            finally {
                if (!this.Parser.MARSOn && con != null) 
                    con.Close(); 
            }
 
            return result;
        }

        internal AssemblyName GetAssemblyName(SqlDataReader r, SqlConnection con) { 
            object key = null;
            AssemblyName aName = new AssemblyName(); 
            SqlString version;; 

            //skip prog_id, is_fixed_len, is_binary_ordered 
            aName.Name = r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.ASSEMBLY_NAME));
            version = r.GetSqlString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.ASSEMBLY_VERSION));

            if (version.IsNull == false) 
                aName.Version = new Version(version.Value);
 
            key = r.GetValue(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.ASSEMBLY_PUBLICKEY)); 

            if (key != System.DBNull.Value) 
                aName.SetPublicKeyToken((byte[])key);

            SqlString cult;
 
            cult = r.GetSqlString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.ASSEMBLY_CULTUREINFO));
 
            if (cult.IsNull == false) 
                aName.CultureInfo = new CultureInfo(cult.Value);
            else 
                aName.CultureInfo = CultureInfo.InvariantCulture;

            //
 
            //skip permissions
            SqlBinary blob; 
 
            blob = r.GetSqlBinary(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_METADATA));
 
            return aName;
        }

        void EnsureAssembly(int dbId, int aId, AssemblyName aName) { 
            if (false == this.AssemblyCache.FindAndLoadAssembly(dbId, aId, false)) {
                // Auto assembly download is disabled for Whidbey. 
                // AssemblyDownloadHelper(dbId, aId); 
                throw SQL.UDTCantLoadAssembly(aName.ToString());
            } 
        }

        //return the type instance based on dbid and typeid.
        //if the assembly is not available on the client, it will be downloaded. 
        internal Type GetUdtType(int dbId, int typeId) {
            TypeInfo info = null; 
            AssemblyInfo aInfo = null; 
            int aId = 0;
 
            //see if it is available in the cache
            info = this.AssemblyCache.GetTypeInfo(dbId, typeId);
            if (null == info) {
                //query the UDT info from the server. This method should NOT fail. 
                if (false == GetUdtInfo(ref dbId, typeId))
                    throw SQL.UDTInvalidDbId(dbId, typeId); 
 
                //above method will add it to the cache
                info = this.AssemblyCache.GetTypeInfo(dbId, typeId); 
                Debug.Assert(info != null, "SqlClient is in inconsistent state Inconsistent state!!. AssemblyLoad has failed");

                if (info == null)
                    throw SQL.UDTInvalidDbId(dbId, typeId); // not the correct exception, but prevents later AV for now... 
            }
 
            //branch out for generic collection udts 
            if (info.isInstantiated)
                return GetUdtTypeInstantiated(dbId, info); 
            aId = info.assemblyId;
            //get assembly info
            aInfo = this.AssemblyCache.GetAssemblyInfo(dbId, aId);
            Debug.Assert(aInfo != null, "INVALID!!"); 

            if (aInfo == null) 
                throw SQL.UDTUnexpectedResult(info.typeName); 

            //if its not already loaded, and could not be located on the machine... 
            if ((aInfo.assemblyState != AssemblyState.Loaded) && (false == this.AssemblyCache.FindAndLoadAssembly(dbId, aId, false))) {
                throw SQL.UDTCantLoadAssembly(aInfo.assemblyName.ToString());
            }
 
            //add the type to the cache with a reference to the assembly implementation.
            if (false == this.AssemblyCache.AddTypeRefToCache(dbId, typeId, info.typeName, info.className, aId, false)) 
                throw SQL.UDTUnexpectedResult(info.typeName); 

            //now retrieve the Type() instance 
            return this.AssemblyCache.GetTypeFromId(dbId, typeId);
        }

        internal static Type GetMultiSetType() { 
            // For winFS, this is always MultiSet.
            Type type = null; 
            CheckLoadUDTExtensions(); 

            if (null != _udtExtensionsAssembly) { 
                // Retry logic due to CLR breaking change of generics generated names to
                // enable overloading on arity.
                //
                type = _udtExtensionsAssembly.GetType("System.Data.Sql.MultiSet`1"); 
                if (type == null) {
                    type = _udtExtensionsAssembly.GetType("System.Data.Sql.MultiSet!1"); 
 
                    if (type == null) {
                        type = _udtExtensionsAssembly.GetType("System.Data.Sql.MultiSet"); 
                    }
                }
                Debug.Assert(type != null, "null type is not expected!");
            } 

            return type; 
        } 

 
        //if this is a generic udt, all the dependencies must have been downloaded during GetUdtInfo.
        internal Type GetUdtTypeInstantiated(int dbId, TypeInfo info) {
            //Make sure the type is a generic type
            Debug.Assert(info != null, "Invalid TypeInfo"); 
            Debug.Assert(info.isInstantiated, "GetUdtTypeInstantiated called on non generic type!!!");
            if (info.typeRef != null) 
                return info.typeRef; 

            Debug.Assert(info.genericType == (int) TdsEnums.GenericType.MultiSet, "invalid state"); 
            Type generic = GetMultiSetType();
            Debug.Assert(generic.IsGenericType, "Bad Generic TypeInfo!!!");
            Debug.Assert(info.parameters != null && info.parameters.Length == 1, "Bad Generic TypeInfo!!!");
            Type[] paramTypes = new Type[1]; 
            paramTypes[0] = GetUdtType(dbId, info.parameters[0]);
            Type target = generic.MakeGenericType(paramTypes); 
            info.typeRef = target; 
            return target;
 
        }

        internal IUdtSerializationContext GetCurrentContext() {
            // Only to be called from the parameter case... 
            // called if the caller has no context info for extracting the context object.
            if (null == _ctx) { 
                _ctx = new SqlSerializationContext(this, _dbId); 
            }
            return _ctx; 
        }
#endif

        internal object GetUdtValue(object value, SqlMetaDataPriv metaData, bool returnDBNull) { 
            if (returnDBNull && ADP.IsNull(value)) {
                return DBNull.Value; 
            } 

            object o = null; 

            // Since the serializer doesn't handle nulls...
            if (ADP.IsNull(value)) {
                Type t = metaData.udtType; 
                Debug.Assert(t != null, "Unexpected null of udtType on GetUdtValue!");
                o = t.InvokeMember("Null", BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Static, null, null, new Object[]{}, CultureInfo.InvariantCulture); 
                Debug.Assert(o != null); 
                return o;
            } 
            else {
#if WINFSFunctionality
                IUdtSerializationContext ctx = null;
                if (this.GetOpenConnection().IsWinFS) { 
                    ctx = this.GetCurrentContext();
                } 
#endif 

                MemoryStream stm = new MemoryStream((byte[]) value); 

#if WINFSFunctionality
                if (ctx != null) { // If WinFS, then ctx is non-null.
                    o = SqlConnection.Deserialize(stm, metaData.udtType, ctx); 
                }
                else { 
#endif 
                    o = SerializationHelperSql9.Deserialize(stm, metaData.udtType);
#if WINFSFunctionality 
                }
#endif

                Debug.Assert(o != null, "object could NOT be created"); 
                return o;
            } 
        } 

        internal byte[] GetBytes(object o) { 
            Microsoft.SqlServer.Server.Format format  = Microsoft.SqlServer.Server.Format.Native;
            int    maxSize = 0;
            return GetBytes(o, out format, out maxSize);
        } 

        internal byte[] GetBytes(object o, out Microsoft.SqlServer.Server.Format format, out int maxSize) { 
            SqlUdtInfo attr = AssemblyCache.GetInfoFromType(o.GetType()); 
            maxSize = attr.MaxByteSize;
            format  = attr.SerializationFormat; 

            if (maxSize < -1 || maxSize >= UInt16.MaxValue) { // Do we need this?  Is this the right place?
                throw new InvalidOperationException(o.GetType() + ": invalid Size");
            } 

            byte[] retval; 
 
            using (MemoryStream stm = new MemoryStream(maxSize < 0 ? 0 : maxSize)) {
#if WINFSFunctionality 
                IUdtSerializationContext ctx = null;
                if (this.GetOpenConnection().IsWinFS) {
                    ctx = this.GetCurrentContext();
                } 

                if (null == ctx) { 
#endif 
                    SerializationHelperSql9.Serialize(stm, o);
#if WINFSFunctionality 
                }
                else {
                    SqlConnection.Serialize(stm, o, ctx);
                } 
#endif
                retval = stm.ToArray(); 
            } 
            return retval;
        } 
    } // SqlConnection

    //
 

 
 

    [ 
    ComVisible(true),
    ClassInterface(ClassInterfaceType.None),
    Guid("afef65ad-4577-447a-a148-83acadd3d4b9"),
    ] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name = "FullTrust")]
#if WINFSInternalOnly 
    internal 
#else
    public 
#endif
    sealed class SQLDebugging: ISQLDebug {

        // Security stuff 
        const int STANDARD_RIGHTS_REQUIRED = (0x000F0000);
        const int DELETE = (0x00010000); 
        const int READ_CONTROL = (0x00020000); 
        const int WRITE_DAC = (0x00040000);
        const int WRITE_OWNER = (0x00080000); 
        const int SYNCHRONIZE = (0x00100000);
        const int FILE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x000001FF);
        const uint GENERIC_READ = (0x80000000);
        const uint GENERIC_WRITE = (0x40000000); 
        const uint GENERIC_EXECUTE = (0x20000000);
        const uint GENERIC_ALL = (0x10000000); 
 
        const int SECURITY_DESCRIPTOR_REVISION = (1);
        const int ACL_REVISION = (2); 

        const int SECURITY_AUTHENTICATED_USER_RID = (0x0000000B);
        const int SECURITY_LOCAL_SYSTEM_RID = (0x00000012);
        const int SECURITY_BUILTIN_DOMAIN_RID = (0x00000020); 
        const int SECURITY_WORLD_RID = (0x00000000);
        const byte SECURITY_NT_AUTHORITY = 5; 
        const int DOMAIN_GROUP_RID_ADMINS = (0x00000200); 
        const int DOMAIN_ALIAS_RID_ADMINS = (0x00000220);
 
        const int sizeofSECURITY_ATTRIBUTES = 12; // sizeof(SECURITY_ATTRIBUTES);
        const int sizeofSECURITY_DESCRIPTOR = 20; // sizeof(SECURITY_DESCRIPTOR);
        const int sizeofACCESS_ALLOWED_ACE = 12; // sizeof(ACCESS_ALLOWED_ACE);
        const int sizeofACCESS_DENIED_ACE = 12; // sizeof(ACCESS_DENIED_ACE); 
        const int sizeofSID_IDENTIFIER_AUTHORITY = 6; // sizeof(SID_IDENTIFIER_AUTHORITY)
        const int sizeofACL = 8; // sizeof(ACL); 
 
        private IntPtr CreateSD(ref IntPtr pDacl) {
            IntPtr pSecurityDescriptor = IntPtr.Zero; 
            IntPtr pUserSid = IntPtr.Zero;
            IntPtr pAdminSid = IntPtr.Zero;
            IntPtr pNtAuthority = IntPtr.Zero;
            int cbAcl = 0; 
            bool status = false;
 
            pNtAuthority = Marshal.AllocHGlobal(sizeofSID_IDENTIFIER_AUTHORITY); 
            if (pNtAuthority == IntPtr.Zero)
                goto cleanup; 
            Marshal.WriteInt32(pNtAuthority, 0, 0);
            Marshal.WriteByte(pNtAuthority, 4, 0);
            Marshal.WriteByte(pNtAuthority, 5, SECURITY_NT_AUTHORITY);
 
            status =
            NativeMethods.AllocateAndInitializeSid( 
            pNtAuthority, 
            (byte)1,
            SECURITY_AUTHENTICATED_USER_RID, 
            0,
            0,
            0,
            0, 
            0,
            0, 
            0, 
            ref pUserSid);
 
            if (!status || pUserSid == IntPtr.Zero) {
                goto cleanup;
            }
            status = 
            NativeMethods.AllocateAndInitializeSid(
            pNtAuthority, 
            (byte)2, 
            SECURITY_BUILTIN_DOMAIN_RID,
            DOMAIN_ALIAS_RID_ADMINS, 
            0,
            0,
            0,
            0, 
            0,
            0, 
            ref pAdminSid); 

            if (!status || pAdminSid == IntPtr.Zero) { 
                goto cleanup;
            }
            status = false;
            pSecurityDescriptor = Marshal.AllocHGlobal(sizeofSECURITY_DESCRIPTOR); 
            if (pSecurityDescriptor == IntPtr.Zero) {
                goto cleanup; 
            } 
            for (int i = 0; i < sizeofSECURITY_DESCRIPTOR; i++)
                Marshal.WriteByte(pSecurityDescriptor, i, (byte)0); 
            cbAcl = sizeofACL
            + (2 * (sizeofACCESS_ALLOWED_ACE))
            + sizeofACCESS_DENIED_ACE
            + NativeMethods.GetLengthSid(pUserSid) 
            + NativeMethods.GetLengthSid(pAdminSid);
 
            pDacl = Marshal.AllocHGlobal(cbAcl); 
            if (pDacl == IntPtr.Zero) {
                goto cleanup; 
            }
            // rights must be added in a certain order.  Namely, deny access first, then add access
            if (NativeMethods.InitializeAcl(pDacl, cbAcl, ACL_REVISION))
                if (NativeMethods.AddAccessDeniedAce(pDacl, ACL_REVISION, WRITE_DAC, pUserSid)) 
                    if (NativeMethods.AddAccessAllowedAce(pDacl, ACL_REVISION, GENERIC_READ, pUserSid))
                        if (NativeMethods.AddAccessAllowedAce(pDacl, ACL_REVISION, GENERIC_ALL, pAdminSid)) 
                            if (NativeMethods.InitializeSecurityDescriptor(pSecurityDescriptor, SECURITY_DESCRIPTOR_REVISION)) 
                                if (NativeMethods.SetSecurityDescriptorDacl(pSecurityDescriptor, true, pDacl, false)) {
                                    status = true; 
                                }

            cleanup :
            if (pNtAuthority != IntPtr.Zero) { 
                Marshal.FreeHGlobal(pNtAuthority);
            } 
            if (pAdminSid != IntPtr.Zero) 
                NativeMethods.FreeSid(pAdminSid);
            if (pUserSid != IntPtr.Zero) 
                NativeMethods.FreeSid(pUserSid);
            if (status)
                return pSecurityDescriptor;
            else { 
                if (pSecurityDescriptor != IntPtr.Zero) {
                    Marshal.FreeHGlobal(pSecurityDescriptor); 
                } 
            }
            return IntPtr.Zero; 
        }

        bool ISQLDebug.SQLDebug(int dwpidDebugger, int dwpidDebuggee, [MarshalAs(UnmanagedType.LPStr)] string pszMachineName,
        [MarshalAs(UnmanagedType.LPStr)] string pszSDIDLLName, int dwOption, int cbData, byte[] rgbData) { 
            bool result = false;
            IntPtr hFileMap = IntPtr.Zero; 
            IntPtr pMemMap = IntPtr.Zero; 
            IntPtr pSecurityDescriptor = IntPtr.Zero;
            IntPtr pSecurityAttributes = IntPtr.Zero; 
            IntPtr pDacl = IntPtr.Zero;

            // validate the structure
            if (null == pszMachineName || null == pszSDIDLLName) 
                return false;
 
            if (pszMachineName.Length > TdsEnums.SDCI_MAX_MACHINENAME || 
            pszSDIDLLName.Length > TdsEnums.SDCI_MAX_DLLNAME)
                return false; 

            // note that these are ansi strings
            Encoding cp = System.Text.Encoding.GetEncoding(TdsEnums.DEFAULT_ENGLISH_CODE_PAGE_VALUE);
            byte[] rgbMachineName = cp.GetBytes(pszMachineName); 
            byte[] rgbSDIDLLName = cp.GetBytes(pszSDIDLLName);
 
            if (null != rgbData && cbData > TdsEnums.SDCI_MAX_DATA) 
                return false;
 
            string mapFileName;

            // If Win2k or later, prepend "Global\\" to enable this to work through TerminalServices.
            if (ADP.IsPlatformNT5) { 
                mapFileName = "Global\\" + TdsEnums.SDCI_MAPFILENAME;
            } 
            else { 
                mapFileName = TdsEnums.SDCI_MAPFILENAME;
            } 

            mapFileName = mapFileName + dwpidDebuggee.ToString(CultureInfo.InvariantCulture);

            // Create Security Descriptor 
            pSecurityDescriptor = CreateSD(ref pDacl);
            pSecurityAttributes = Marshal.AllocHGlobal(sizeofSECURITY_ATTRIBUTES); 
            if ((pSecurityDescriptor == IntPtr.Zero) || (pSecurityAttributes == IntPtr.Zero)) 
                return false;
 
            Marshal.WriteInt32(pSecurityAttributes, 0, sizeofSECURITY_ATTRIBUTES); // nLength = sizeof(SECURITY_ATTRIBUTES)
            Marshal.WriteIntPtr(pSecurityAttributes, 4, pSecurityDescriptor); // lpSecurityDescriptor = pSecurityDescriptor
            Marshal.WriteInt32(pSecurityAttributes, 8, 0); // bInheritHandle = FALSE
            hFileMap = NativeMethods.CreateFileMappingA( 
            ADP.InvalidPtr/*INVALID_HANDLE_VALUE*/,
            pSecurityAttributes, 
            0x4/*PAGE_READWRITE*/, 
            0,
            Marshal.SizeOf(typeof(MEMMAP)), 
            mapFileName);

            if (IntPtr.Zero == hFileMap) {
                goto cleanup; 
            }
 
 
            pMemMap = NativeMethods.MapViewOfFile(hFileMap, 0x6/*FILE_MAP_READ|FILE_MAP_WRITE*/, 0, 0, IntPtr.Zero);
 
            if (IntPtr.Zero == pMemMap) {
                goto cleanup;
            }
 
            // copy data to memory-mapped file
            // layout of MEMMAP structure is: 
            // uint dbgpid 
            // uint fOption
            // byte[32] machineName 
            // byte[16] sdiDllName
            // uint dbData
            // byte[255] vData
            int offset = 0; 
            Marshal.WriteInt32(pMemMap, offset, (int)dwpidDebugger);
            offset += 4; 
            Marshal.WriteInt32(pMemMap, offset, (int)dwOption); 
            offset += 4;
            Marshal.Copy(rgbMachineName, 0, ADP.IntPtrOffset(pMemMap, offset), rgbMachineName.Length); 
            offset += TdsEnums.SDCI_MAX_MACHINENAME;
            Marshal.Copy(rgbSDIDLLName, 0, ADP.IntPtrOffset(pMemMap, offset), rgbSDIDLLName.Length);
            offset += TdsEnums.SDCI_MAX_DLLNAME;
            Marshal.WriteInt32(pMemMap, offset, (int)cbData); 
            offset += 4;
            if (null != rgbData) { 
                Marshal.Copy(rgbData, 0, ADP.IntPtrOffset(pMemMap, offset), (int)cbData); 
            }
            NativeMethods.UnmapViewOfFile(pMemMap); 
            result = true;
        cleanup :
            if (result == false) {
                if (hFileMap != IntPtr.Zero) 
                    NativeMethods.CloseHandle(hFileMap);
            } 
            if (pSecurityAttributes != IntPtr.Zero) 
                Marshal.FreeHGlobal(pSecurityAttributes);
            if (pSecurityDescriptor != IntPtr.Zero) 
                Marshal.FreeHGlobal(pSecurityDescriptor);
            if (pDacl != IntPtr.Zero)
                Marshal.FreeHGlobal(pDacl);
            return result; 
        }
    } 
 
    // this is a private interface to com+ users
    // do not change this guid 
    [
    ComImport,
    ComVisible(true),
    Guid("6cb925bf-c3c0-45b3-9f44-5dd67c7b7fe8"), 
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    BestFitMapping(false, ThrowOnUnmappableChar = true), 
    ] 
    interface ISQLDebug {
 
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name = "FullTrust")]
        bool SQLDebug(
        int dwpidDebugger,
        int dwpidDebuggee, 
        [MarshalAs(UnmanagedType.LPStr)] string pszMachineName,
        [MarshalAs(UnmanagedType.LPStr)] string pszSDIDLLName, 
        int dwOption, 
        int cbData,
        byte[] rgbData); 
    }

    sealed class SqlDebugContext: IDisposable {
        // context data 
        internal uint pid = 0;
        internal uint tid = 0; 
        internal bool active = false; 
        // memory-mapped data
        internal IntPtr pMemMap = ADP.PtrZero; 
        internal IntPtr hMemMap = ADP.PtrZero;
        internal uint dbgpid = 0;
        internal bool fOption = false;
        internal string machineName = null; 
        internal string sdiDllName = null;
        internal byte[] data = null; 
 
        public void Dispose() {
            Dispose(true); 
            GC.SuppressFinalize(this);
        }
        private void Dispose (bool disposing) {
            if (disposing) { 
                // Nothing to do here
                ; 
            } 
            if (pMemMap != IntPtr.Zero) {
                NativeMethods.UnmapViewOfFile(pMemMap); 
                pMemMap = IntPtr.Zero;
            }
            if (hMemMap != IntPtr.Zero) {
                NativeMethods.CloseHandle(hMemMap); 
                hMemMap = IntPtr.Zero;
            } 
            active = false; 
        }
 
        ~SqlDebugContext() {
                Dispose(false);
        }
 
    }
 
    // native interop memory mapped structure for sdi debugging 
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    internal struct MEMMAP { 
        [MarshalAs(UnmanagedType.U4)]
        internal uint dbgpid; // id of debugger
        [MarshalAs(UnmanagedType.U4)]
        internal uint fOption; // 1 - start debugging, 0 - stop debugging 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal byte[] rgbMachineName; 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] 
        internal byte[] rgbDllName;
        [MarshalAs(UnmanagedType.U4)] 
        internal uint cbData;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        internal byte[] rgbData;
    } 
} // System.Data.SqlClient namespace
 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlConnection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="true" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

#if WINFSFunctionality 
    // UDTExtensions is a friend assembly - for UDT requirements
    // At some later time, would be convenient to have all assembly level attributes in one place.
    [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UdtExtensions, PublicKey=0024000004800000940000000602000000240000525341310004000001000100272736ad6e5f9586bac2d531eabc3acc666c2f8ec879fa94f8f7b0327d2ff2ed523448f83c3d5c5dd2dfc7bc99c5286b2c125117bf5cbe242b9d41750732b2bdffe649c6efb8e5526d526fdd130095ecdb7bf210809c6cdad8824faa9ac0310ac3cba2aa0523567b2dfa7fe250b30facbd62d4ec99b94ac47c7d3b28f1f6e4c8")] //
#endif 
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Data.DataSetExtensions, PublicKey="+AssemblyRef.EcmaPublicKeyFull)] //
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Data.Entity, PublicKey="+AssemblyRef.EcmaPublicKeyFull)] // SQLPT 300000492 
 
namespace System.Data.SqlClient
{ 
    using System;
    using System.Collections;
    using System.Configuration.Assemblies;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Common; 
    using System.Data.ProviderBase; 
    using System.Data.Sql;
    using System.Data.SqlTypes; 
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices; 
    using System.Runtime.Remoting; 
    using System.Runtime.Serialization.Formatters;
    using System.Text; 
    using System.Threading;
    using System.Security;
    using System.Security.Permissions;
    using System.Reflection; 

    using Microsoft.SqlServer.Server; 
 
    [DefaultEvent("InfoMessage")]
#if WINFSInternalOnly 
    internal
#else
    public
#endif 
    sealed partial class SqlConnection: DbConnection, ICloneable {
 
        static private readonly object EventInfoMessage = new object(); 

        private SqlDebugContext _sdc;   // SQL Debugging support 

        private bool    _AsycCommandInProgress;

        // SQLStatistics support 
        internal SqlStatistics _statistics;
        private bool _collectstats; 
 
        private bool _fireInfoMessageEventOnUserErrors; // False by default
 
#if WINFSFunctionality
        //this is the id of the current database. This is not perfect.
        //we will just cache the last active database that called GetUdtInfo()
        internal int _dbId; // making it internal so we can access this from SqlSerializationContext 
        internal string _catalog;
        IUdtSerializationContext _ctx; 
#endif 

        public SqlConnection(string connectionString) : this() { 
            ConnectionString = connectionString;
        }

        private SqlConnection(SqlConnection connection) { // Clone 
            GC.SuppressFinalize(this);
            CopyFrom(connection); 
#if WINFSFunctionality 
            _dbId = connection._dbId;
            _catalog = connection.Database; 
#endif
        }

        // 
        // PUBLIC PROPERTIES
        // 
 
        // used to start/stop collection of statistics data and do verify the current state
        // 
        // devnote: start/stop should not performed using a property since it requires execution of code
        //
        // start statistics
        //  set the internal flag (_statisticsEnabled) to true. 
        //  Create a new SqlStatistics object if not already there.
        //  connect the parser to the object. 
        //  if there is no parser at this time we need to connect it after creation. 
        //
 
        [
        DefaultValue(false),
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.SqlConnection_StatisticsEnabled), 
        ]
        public bool StatisticsEnabled { 
            get { 
                return (_collectstats);
            } 
            set {
                if (IsContextConnection) {
                    if (value) {
                        throw SQL.NotAvailableOnContextConnection(); 
                    }
                } 
                else { 
                    if (value) {
                        // start 
                        if (ConnectionState.Open == State) {
                            if (null == _statistics) {
                                _statistics = new SqlStatistics();
                                ADP.TimerCurrent(out _statistics._openTimestamp); 
                            }
                            // set statistics on the parser 
                            // update timestamp; 
                            Debug.Assert(Parser != null, "Where's the parser?");
                            Parser.Statistics = _statistics; 
                        }
                    }
                    else {
                        // stop 
                        if (null != _statistics) {
                            if (ConnectionState.Open == State) { 
                                // remove statistics from parser 
                                // update timestamp;
                                TdsParser parser = Parser; 
                                Debug.Assert(parser != null, "Where's the parser?");
                                parser.Statistics = null;
                                ADP.TimerCurrent(out _statistics._closeTimestamp);
                            } 
                        }
                    } 
                    this._collectstats = value; 
                }
            } 
        }

        internal bool AsycCommandInProgress  {
            get { 
                return (_AsycCommandInProgress);
            } 
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
            set {
                _AsycCommandInProgress = value; 
            }
        }

        internal bool IsContextConnection { 
            get {
                SqlConnectionString opt = (SqlConnectionString)ConnectionOptions; 
                bool result = false; 
                if (null != opt) {
                    result = (opt.ContextConnection); 
                }
                return result;
            }
        } 

        internal SqlConnectionString.TransactionBindingEnum TransactionBinding { 
            get { 
                return ((SqlConnectionString)ConnectionOptions).TransactionBinding;
            } 
        }

        internal SqlConnectionString.TypeSystem TypeSystem {
            get { 
                return ((SqlConnectionString)ConnectionOptions).TypeSystemVersion;
            } 
        } 

        override protected DbProviderFactory DbProviderFactory { 
            get {
                return SqlClientFactory.Instance;
            }
        } 

        [ 
        DefaultValue(""), 
        RecommendedAsConfigurable(true),
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        Editor("Microsoft.VSDesigner.Data.SQL.Design.SqlConnectionStringEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing),
        ResDescriptionAttribute(Res.SqlConnection_ConnectionString),
        ] 
        override public string ConnectionString {
            get { 
                return ConnectionString_Get(); 
            }
            set { 
                ConnectionString_Set(value);
            }
        }
 
        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResDescriptionAttribute(Res.SqlConnection_ConnectionTimeout), 
        ]
        override public int ConnectionTimeout { 
            get {
                SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                return ((null != constr) ? constr.ConnectTimeout : SqlConnectionString.DEFAULT.Connect_Timeout);
            } 
        }
 
        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.SqlConnection_Database), 
        ]
        override public string Database {
            // if the connection is open, we need to ask the inner connection what it's
            // current catalog is because it may have gotten changed, otherwise we can 
            // just return what the connection string had.
            get { 
                SqlInternalConnection innerConnection = (InnerConnection as SqlInternalConnection); 
                string result;
 
                if (null != innerConnection) {
                    result = innerConnection.CurrentDatabase;
                }
                else { 
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                    result = ((null != constr) ? constr.InitialCatalog : SqlConnectionString.DEFAULT.Initial_Catalog); 
                } 
                return result;
            } 
        }

        [
        Browsable(true), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.SqlConnection_DataSource), 
        ] 
        override public string DataSource {
            get { 
                SqlInternalConnection innerConnection = (InnerConnection as SqlInternalConnection);
                string result;

                if (null != innerConnection) { 
                    result = innerConnection.CurrentDataSource;
                } 
                else { 
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                    result = ((null != constr) ? constr.DataSource : SqlConnectionString.DEFAULT.Data_Source); 
                }
                return result;
            }
        } 

        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.SqlConnection_PacketSize), 
        ]
        public int PacketSize {
            // if the connection is open, we need to ask the inner connection what it's
            // current packet size is because it may have gotten changed, otherwise we 
            // can just return what the connection string had.
            get { 
                if (IsContextConnection) { 
                    throw SQL.NotAvailableOnContextConnection();
                } 

                SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds);
                int result;
 
                if (null != innerConnection) {
                    result = innerConnection.PacketSize; 
                } 
                else {
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions; 
                    result = ((null != constr) ? constr.PacketSize : SqlConnectionString.DEFAULT.Packet_Size);
                }
                return result;
            } 
        }
 
        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResDescriptionAttribute(Res.SqlConnection_ServerVersion),
        ]
        override public string ServerVersion {
            get { 
                return GetOpenConnection().ServerVersion;
            } 
        } 

        internal SqlStatistics Statistics { 
            get {
                return _statistics;
            }
        } 

        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.SqlConnection_WorkstationId), 
        ]
        public string WorkstationId {
            get {
                if (IsContextConnection) { 
                    throw SQL.NotAvailableOnContextConnection();
                } 
 
                // If not supplied by the user, the default value is the MachineName
                // Note: In Longhorn you'll be able to rename a machine without 
                // rebooting.  Therefore, don't cache this machine name.
                SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                string result = ((null != constr) ? constr.WorkstationId : null);
                if (null == result) { 
                    // getting machine name requires Environment.Permission
                    // user must have that permission in order to retrieve this 
                    result = Environment.MachineName; 
                }
                return result; 
            }
        }

        // 
        // PUBLIC EVENTS
        // 
 
        [
        ResCategoryAttribute(Res.DataCategory_InfoMessage), 
        ResDescriptionAttribute(Res.DbConnection_InfoMessage),
        ]
        public event SqlInfoMessageEventHandler InfoMessage {
            add { 
                Events.AddHandler(EventInfoMessage, value);
            } 
            remove { 
                Events.RemoveHandler(EventInfoMessage, value);
            } 
        }

        public bool FireInfoMessageEventOnUserErrors {
            get { 
                return _fireInfoMessageEventOnUserErrors;
            } 
            set { 
                _fireInfoMessageEventOnUserErrors = value;
            } 
        }

        //
        // PUBLIC METHODS 
        //
 
        new public SqlTransaction BeginTransaction() { 
            // this is just a delegate. The actual method tracks executiontime
            return BeginTransaction(IsolationLevel.Unspecified, null); 
        }

        new public SqlTransaction BeginTransaction(IsolationLevel iso) {
            // this is just a delegate. The actual method tracks executiontime 
            return BeginTransaction(iso, null);
        } 
 
        public SqlTransaction BeginTransaction(string transactionName) {
                // Use transaction names only on the outermost pair of nested 
                // BEGIN...COMMIT or BEGIN...ROLLBACK statements.  Transaction names
                // are ignored for nested BEGIN's.  The only way to rollback a nested
                // transaction is to have a save point from a SAVE TRANSACTION call.
                return BeginTransaction(IsolationLevel.Unspecified, transactionName); 
        }
 
        public SqlTransaction BeginTransaction(IsolationLevel iso, string transactionName) { 
            SqlStatistics statistics = null;
            IntPtr hscp; 
            string xactName =  ADP.IsEmpty(transactionName)? "None" : transactionName;
            Bid.ScopeEnter(out hscp, "<sc.SqlConnection.BeginTransaction|API> %d#, iso=%d{ds.IsolationLevel}, transactionName='%ls'\n", ObjectID, (int)iso,
                        xactName);
 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
 
                // NOTE: we used to throw an exception if the transaction name was empty
                // (see MDAC 50292) but we have a BeginTransaction method that doesn't 
                // have a transactionName argument.
                SqlTransaction transaction = GetOpenConnection().BeginSqlTransaction(iso, transactionName);

                // 

 
                GC.KeepAlive(this); 

                return transaction; 
            }
            finally {
                Bid.ScopeLeave(ref hscp);
                SqlStatistics.StopTimer(statistics); 
            }
        } 
 
        override public void ChangeDatabase(string database) {
            SqlStatistics statistics = null; 

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    statistics = SqlStatistics.StartTimer(Statistics);
                    InnerConnection.ChangeDatabase(database);
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG 
            }
            catch (System.OutOfMemoryException e) {
                Abort(e);
                throw; 
            }
            catch (System.StackOverflowException e) { 
                Abort(e); 
                throw;
            } 
            catch (System.Threading.ThreadAbortException e) {
                Abort(e);
                throw;
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            } 
        }
 
        static public void ClearAllPools() {
            (new SqlClientPermission(PermissionState.Unrestricted)).Demand();
            SqlConnectionFactory.SingletonInstance.ClearAllPools();
        } 

        static public void ClearPool(SqlConnection connection) { 
            ADP.CheckArgumentNull(connection, "connection"); 

            DbConnectionOptions connectionOptions = connection.UserConnectionOptions; 
            if (null != connectionOptions) {
                connectionOptions.DemandPermission();
                if (connection.IsContextConnection) {
                    throw SQL.NotAvailableOnContextConnection(); 
                }
                SqlConnectionFactory.SingletonInstance.ClearPool(connection); 
            } 
        }
 
        object ICloneable.Clone() {
            SqlConnection clone = new SqlConnection(this);
            Bid.Trace("<sc.SqlConnection.Clone|API> %d#, clone=%d#\n", ObjectID, clone.ObjectID);
            return clone; 
        }
 
        override public void Close() { 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlConnection.Close|API> %d#" , ObjectID); 
            try {
                SqlStatistics statistics = null;

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
#if DEBUG 
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                        statistics = SqlStatistics.StartTimer(Statistics); 

                        // The lock here is to protect against the command.cancel / connection.close race condition 
                        // The SqlInternalConnectionTds is set to OpenBusy during close, once this happens the cast below will fail and 
                        // the command will no longer be cancelable.  It might be desirable to be able to cancel the close opperation, but this is
                        // outside of the scope of Whidbey RTM.  See (SqlCommand::Cancel) for other lock. 
                        lock (InnerConnection) {
                            InnerConnection.CloseConnection(this, ConnectionFactory);
                        }
                        // does not require GC.KeepAlive(this) because of OnStateChange 

                        if (null != Statistics) { 
                            ADP.TimerCurrent(out _statistics._closeTimestamp); 
                        }
 #if DEBUG 
                    }
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                    } 
#endif //DEBUG
                } 
                catch (System.OutOfMemoryException e) { 
                    Abort(e);
                    throw; 
                }
                catch (System.StackOverflowException e) {
                    Abort(e);
                    throw; 
                }
                catch (System.Threading.ThreadAbortException e) { 
                    Abort(e); 
                    throw;
                } 
                finally {
                    SqlStatistics.StopTimer(statistics);
                }
            } 
            finally {
                SqlDebugContext  sdc = _sdc; 
                _sdc = null; 
                Bid.ScopeLeave(ref hscp);
                if (sdc != null) { 
                   sdc.Dispose();
                }
            }
        } 

        new public SqlCommand CreateCommand() { 
            return new SqlCommand(null, this); 
        }
 
        private void DisposeMe(bool disposing) { // MDAC 65459
        }

        public void EnlistDistributedTransaction(System.EnterpriseServices.ITransaction transaction) { 
            if (IsContextConnection) {
                throw SQL.NotAvailableOnContextConnection(); 
            } 

            EnlistDistributedTransactionHelper(transaction); 
        }

        override public void Open() {
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlConnection.Open|API> %d#", ObjectID) ;
            try { 
                if (StatisticsEnabled) { 
                    if (null == _statistics) {
                        _statistics = new SqlStatistics(); 
                    }
                    else {
                        _statistics.ContinueOnNewConnection();
                    } 
                }
 
                SqlStatistics statistics = null; 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
#if DEBUG
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
                        Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG 
                        statistics = SqlStatistics.StartTimer(Statistics);
 
                        InnerConnection.OpenConnection(this, ConnectionFactory);
                        // does not require GC.KeepAlive(this) because of OnStateChange

                        SqlInternalConnectionSmi innerConnection = (InnerConnection as SqlInternalConnectionSmi); 
                        if (null != innerConnection) {
                            innerConnection.AutomaticEnlistment(); 
                        } 
                        else {
                            Debug.Assert(Parser != null, "Where's the parser?"); 

                            if (StatisticsEnabled) {
                                ADP.TimerCurrent(out _statistics._openTimestamp);
                                Parser.Statistics = _statistics; 
                            }
                            else { 
                                Parser.Statistics = null; 
                                _statistics = null; // in case of previous Open/Close/reset_CollectStats sequence
                            } 
                            CompleteOpen();
                        }
#if DEBUG
                    } 
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                    } 
#endif //DEBUG
                } 
                catch (System.OutOfMemoryException e) {
                    Abort(e);
                    throw;
                } 
                catch (System.StackOverflowException e) {
                    Abort(e); 
                    throw; 
                }
                catch (System.Threading.ThreadAbortException e) { 
                    Abort(e);
                    throw;
                }
                finally { 
                    SqlStatistics.StopTimer(statistics);
                } 
            } 
            finally {
                Bid.ScopeLeave(ref hscp) ; 
            }
        }

 
        //
        // INTERNAL PROPERTIES 
        // 

#if WINFSFunctionality 
        internal AssemblyCache AssemblyCache {
            get {
                return GetOpenTdsConnection().AssemblyCache;
            } 
        }
#endif 
 
        internal bool HasLocalTransaction {
            get { 
                return GetOpenConnection().HasLocalTransaction;
            }
        }
 
        internal bool HasLocalTransactionFromAPI {
            get { 
                return GetOpenConnection().HasLocalTransactionFromAPI; 
            }
        } 

        internal bool IsShiloh {
            get {
                return GetOpenConnection().IsShiloh; 
            }
        } 
 
        internal bool IsYukonOrNewer {
            get { 
                return GetOpenConnection().IsYukonOrNewer;
            }
        }
 
        internal bool IsKatmaiOrNewer {
            get { 
                return GetOpenConnection().IsKatmaiOrNewer; 
            }
        } 

        internal TdsParser Parser {
            get {
                SqlInternalConnectionTds tdsConnection = (GetOpenConnection() as SqlInternalConnectionTds); 
                if (null == tdsConnection) {
                    throw SQL.NotAvailableOnContextConnection(); 
                } 
                return tdsConnection.Parser;
            } 
        }

        internal bool Asynchronous {
            get { 
                SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                return ((null != constr) ? constr.Asynchronous : SqlConnectionString.DEFAULT.Asynchronous); 
            } 
        }
 
        //
        // INTERNAL METHODS
        //
 
        internal void AddPreparedCommand(SqlCommand cmd) {
            GetOpenConnection().AddPreparedCommand(cmd); 
        } 

        internal void ValidateConnectionForExecute(string method, SqlCommand command) { 
            SqlInternalConnection innerConnection = GetOpenConnection(method);
            innerConnection.ValidateConnectionForExecute(command);
        }
 
        // Surround name in brackets and then escape any end bracket to protect against SQL Injection.
        // NOTE: if the user escapes it themselves it will not work, but this was the case in V1 as well 
        // as native OleDb and Odbc. 
        static internal string FixupDatabaseTransactionName(string name) {
            if (!ADP.IsEmpty(name)) { 
                return "[" + name.Replace("]", "]]") + "]";
            }
            else {
                return name; 
            }
        } 
 
        internal void OnError(SqlException exception, bool breakConnection) {
            Debug.Assert(exception != null && exception.Errors.Count != 0, "SqlConnection: OnError called with null or empty exception!"); 

            // Bug fix - MDAC 49022 - connection open after failure...  Problem was parser was passing
            // Open as a state - because the parser's connection to the netlib was open.  We would
            // then set the connection state to the parser's state - which is not correct.  The only 
            // time the connection state should change to what is passed in to this function is if
            // the parser is broken, then we should be closed.  Changed to passing in 
            // TdsParserState, not ConnectionState. 
            // fixed by [....]
 
            if (breakConnection && (ConnectionState.Open == State)) {
                Bid.Trace("<sc.SqlConnection.OnError|INFO> %d#, Connection broken.\n", ObjectID) ;
                this.Close();
            } 

            if (exception.Class >= TdsEnums.MIN_ERROR_CLASS) { 
                // It is an error, and should be thrown.  Class of TdsEnums.MIN_ERROR_CLASS or above is an error, 
                // below TdsEnums.MIN_ERROR_CLASS denotes an info message.
                throw exception; 
            }
            else {
                // If it is a class < TdsEnums.MIN_ERROR_CLASS, it is a warning collection - so pass to handler
                this.OnInfoMessage(new SqlInfoMessageEventArgs(exception)); 
            }
        } 
 
        internal void RemovePreparedCommand(SqlCommand cmd) {
            GetOpenConnection().RemovePreparedCommand(cmd); 
        }

        //
        // PRIVATE METHODS 
        //
 
        private void CompleteOpen() { 
            Debug.Assert(ConnectionState.Open == State, "CompleteOpen not open");
            // be sure to mark as open so SqlDebugCheck can issue Query 

            // check to see if we need to hook up sql-debugging if a debugger is attached
            // We only need this check for Shiloh and earlier servers.
            if (!GetOpenConnection().IsYukonOrNewer && 
                    System.Diagnostics.Debugger.IsAttached) {
                bool debugCheck = false; 
                try { 
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand(); // MDAC 66682, 69017
                    debugCheck = true; 
                }
                catch (SecurityException e) {
                    ADP.TraceExceptionWithoutRethrow(e);
                } 

                if (debugCheck) { 
                    // if we don't have Unmanaged code permission, don't check for debugging 
                    // but let the connection be opened while under the debugger
                    CheckSQLDebugOnConnect(); 
                }
            }
        }
 
        internal SqlInternalConnection GetOpenConnection() {
            SqlInternalConnection innerConnection = (InnerConnection as SqlInternalConnection); 
            if (null == innerConnection) { 
                throw ADP.ClosedConnectionError();
            } 
            return innerConnection;
        }

        internal SqlInternalConnection GetOpenConnection(string method) { 
            DbConnectionInternal innerConnection = InnerConnection;
            SqlInternalConnection innerSqlConnection = (innerConnection as SqlInternalConnection); 
            if (null == innerSqlConnection) { 
                throw ADP.OpenConnectionRequired(method, innerConnection.State);
            } 
            return innerSqlConnection;
        }

        internal SqlInternalConnectionTds GetOpenTdsConnection() { 
            SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds);
            if (null == innerConnection) { 
                throw ADP.ClosedConnectionError(); 
            }
            return innerConnection; 
        }

        internal SqlInternalConnectionTds GetOpenTdsConnection(string method) {
            SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds); 
            if (null == innerConnection) {
                throw ADP.OpenConnectionRequired(method, innerConnection.State); 
            } 
            return innerConnection;
        } 

        internal void OnInfoMessage(SqlInfoMessageEventArgs imevent) {
            if (Bid.TraceOn) {
                Debug.Assert(null != imevent, "null SqlInfoMessageEventArgs"); 
                Bid.Trace("<sc.SqlConnection.OnInfoMessage|API|INFO> %d#, Message='%ls'\n", ObjectID, ((null != imevent) ? imevent.Message : ""));
            } 
            SqlInfoMessageEventHandler handler = (SqlInfoMessageEventHandler)Events[EventInfoMessage]; 
            if (null != handler) {
                try { 
                    handler(this, imevent);
                }
                catch (Exception e) { // MDAC 53175
                    if (!ADP.IsCatchableOrSecurityExceptionType(e)) { 
                        throw;
                    } 
 
                    ADP.TraceExceptionWithoutRethrow(e);
                } 
            }
        }

        // 
        // SQL DEBUGGING SUPPORT
        // 
 
        // this only happens once per connection
        private void CheckSQLDebugOnConnect() { 
            IntPtr hFileMap;
            uint pid = (uint)SafeNativeMethods.GetCurrentProcessId();

            string mapFileName; 

            // If Win2k or later, prepend "Global\\" to enable this to work through TerminalServices. 
            if (ADP.IsPlatformNT5) { 
                mapFileName = "Global\\" + TdsEnums.SDCI_MAPFILENAME;
            } 
            else {
                mapFileName = TdsEnums.SDCI_MAPFILENAME;
            }
 
            mapFileName = mapFileName + pid.ToString(CultureInfo.InvariantCulture);
 
            hFileMap = NativeMethods.OpenFileMappingA(0x4/*FILE_MAP_READ*/, false, mapFileName); 

            if (ADP.PtrZero != hFileMap) { 
                IntPtr pMemMap = NativeMethods.MapViewOfFile(hFileMap, 0x4/*FILE_MAP_READ*/, 0, 0, IntPtr.Zero);
                if (ADP.PtrZero != pMemMap) {
                    SqlDebugContext sdc = new SqlDebugContext();
                    sdc.hMemMap = hFileMap; 
                    sdc.pMemMap = pMemMap;
                    sdc.pid = pid; 
 
                    // optimization: if we only have to refresh memory-mapped data at connection open time
                    // optimization: then call here instead of in CheckSQLDebug() which gets called 
                    // optimization: at command execution time
                    // RefreshMemoryMappedData(sdc);

                    // delaying setting out global state until after we issue this first SQLDebug command so that 
                    // we don't reentrantly call into CheckSQLDebug
                    CheckSQLDebug(sdc); 
                    // now set our global state 
                    _sdc = sdc;
                } 
            }
        }

        // This overload is called by the Command object when executing stored procedures.  Note that 
        // if SQLDebug has never been called, it is a noop.
        internal void CheckSQLDebug() { 
            if (null != _sdc) 
                CheckSQLDebug(_sdc);
        } 

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)] // MDAC 66682, 69017
        private void CheckSQLDebug(SqlDebugContext sdc) {
            // check to see if debugging has been activated 
            Debug.Assert(null != sdc, "SQL Debug: invalid null debugging context!");
 
#pragma warning disable 618 
            uint tid = (uint)AppDomain.GetCurrentThreadId();    // Sql Debugging doesn't need fiber support;
#pragma warning restore 618 
            RefreshMemoryMappedData(sdc);

            //
 

 
            // If we get here, the debugger must be hooked up. 
            if (!sdc.active) {
                if (sdc.fOption/*TdsEnums.SQLDEBUG_ON*/) { 
                    // turn on
                    sdc.active = true;
                    sdc.tid = tid;
                    try { 
                        IssueSQLDebug(TdsEnums.SQLDEBUG_ON, sdc.machineName, sdc.pid, sdc.dbgpid, sdc.sdiDllName, sdc.data);
                        sdc.tid = 0; // reset so that the first successful time through, we notify the server of the context switch 
                    } 
                    catch {
                        sdc.active = false; 
                        throw;
                    }
                }
            } 

            // be sure to pick up thread context switch, especially the first time through 
            if (sdc.active) { 
                if (!sdc.fOption/*TdsEnums.SQLDEBUG_OFF*/) {
                    // turn off and free the memory 
                    sdc.Dispose();
                    // okay if we throw out here, no state to clean up
                    IssueSQLDebug(TdsEnums.SQLDEBUG_OFF, null, 0, 0, null, null);
                } 
                else {
                    // notify server of context change 
                    if (sdc.tid != tid) { 
                        sdc.tid = tid;
                        try { 
                            IssueSQLDebug(TdsEnums.SQLDEBUG_CONTEXT, null, sdc.pid, sdc.tid, null, null);
                        }
                        catch {
                            sdc.tid = 0; 
                            throw;
                        } 
                    } 
                }
            } 
        }

        private void IssueSQLDebug(uint option, string machineName, uint pid, uint id, string sdiDllName, byte[] data) {
 
            if (GetOpenConnection().IsYukonOrNewer) {
                // 
                return; 
            }
 
            //

            SqlCommand c = new SqlCommand(TdsEnums.SP_SDIDEBUG, this);
            c.CommandType = CommandType.StoredProcedure; 

            // context param 
            SqlParameter p = new SqlParameter(null, SqlDbType.VarChar, TdsEnums.SQLDEBUG_MODE_NAMES[option].Length); 
            p.Value = TdsEnums.SQLDEBUG_MODE_NAMES[option];
            c.Parameters.Add(p); 

            if (option == TdsEnums.SQLDEBUG_ON) {
                // debug dll name
                p = new SqlParameter(null, SqlDbType.VarChar, sdiDllName.Length); 
                p.Value = sdiDllName;
                c.Parameters.Add(p); 
                // debug machine name 
                p = new SqlParameter(null, SqlDbType.VarChar, machineName.Length);
                p.Value = machineName; 
                c.Parameters.Add(p);
            }

            if (option != TdsEnums.SQLDEBUG_OFF) { 
                // client pid
                p = new SqlParameter(null, SqlDbType.Int); 
                p.Value = pid; 
                c.Parameters.Add(p);
                // dbgpid or tid 
                p = new SqlParameter(null, SqlDbType.Int);
                p.Value = id;
                c.Parameters.Add(p);
            } 

            if (option == TdsEnums.SQLDEBUG_ON) { 
                // debug data 
                p = new SqlParameter(null, SqlDbType.VarBinary, (null != data) ? data.Length : 0);
                p.Value = data; 
                c.Parameters.Add(p);
            }

            c.ExecuteNonQuery(); 
        }
 
        public static void ChangePassword(string connectionString, string newPassword) { 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlConnection.ChangePassword|API>") ; 
            try {
                if (ADP.IsEmpty(connectionString)) {
                    throw SQL.ChangePasswordArgumentMissing("connectionString");
                } 
                if (ADP.IsEmpty(newPassword)) {
                    throw SQL.ChangePasswordArgumentMissing("newPassword"); 
                } 
                if (TdsEnums.MAXLEN_NEWPASSWORD < newPassword.Length) {
                    throw ADP.InvalidArgumentLength("newPassword", TdsEnums.MAXLEN_NEWPASSWORD); 
                }

                SqlConnectionString connectionOptions = SqlConnectionFactory.FindSqlConnectionOptions(connectionString);
                if (connectionOptions.IntegratedSecurity) { 
                    throw SQL.ChangePasswordConflictsWithSSPI();
                } 
                if (! ADP.IsEmpty(connectionOptions.AttachDBFilename)) { 
                    throw SQL.ChangePasswordUseOfUnallowedKey(SqlConnectionString.KEY.AttachDBFilename);
                } 
                if (connectionOptions.ContextConnection) {
                    throw SQL.ChangePasswordUseOfUnallowedKey(SqlConnectionString.KEY.Context_Connection);
                }
 
                System.Security.PermissionSet permissionSet = connectionOptions.CreatePermissionSet();
                permissionSet.Demand(); 
 
                // note: This is the only case where we directly construt the internal connection, passing in the new password.
                // Normally we would simply create a regular connectoin and open it but there is no other way to pass the 
                // new password down to the constructor. Also it would have an unwanted impact on the connection pool
                //
                using (SqlInternalConnectionTds con = new SqlInternalConnectionTds(null, connectionOptions, null, newPassword, (SqlConnection)null, false)) {
                    if (!con.IsYukonOrNewer) { 
                        throw SQL.ChangePasswordRequiresYukon();
                    } 
                } 
                SqlConnectionFactory.SingletonInstance.ClearPool(connectionString);
            } 
            finally {
                Bid.ScopeLeave(ref hscp) ;
            }
        } 

#if WINFSFunctionality 
        static private volatile bool     _searched              = false; 
        static private          Assembly _udtExtensionsAssembly = null;
        static private          object   _lockobj               = new object(); 
        static private          Type     _serializationHelper   = null;

        static private void CheckLoadUDTExtensions() {
            if (!_searched) { 
                lock (_lockobj) {
                    if (!_searched) { 
                        try { 
                            AssemblyName assemblyName = new AssemblyName();
                            assemblyName.Name = "udtextensions"; 
                            assemblyName.Version = new Version("9.0.242.0");
                            assemblyName.SetPublicKeyToken(new byte[]{0x89,0x84,0x5d,0xcd,0x80,0x80,0xcc,0x91});
                            assemblyName.CultureInfo = CultureInfo.InvariantCulture;
 
                            _udtExtensionsAssembly = Assembly.Load(assemblyName);
 
                            _serializationHelper = _udtExtensionsAssembly.GetType("System.Data.Sql.SerializationHelper"); 
                        }
                        catch (FileNotFoundException) { 
                            _udtExtensionsAssembly = null; // reset to null in case GetType fails
                            // OPEN WIDE!!!
                        }
                        finally { 
                            _searched = true;
                        } 
                    } 
                }
            } 
        }

        static internal object Deserialize(Stream stream, Type type, IUdtSerializationContext context) {
            CheckLoadUDTExtensions(); 

            object obj = null; 
 
            Debug.Assert(null != _serializationHelper, "Why are we calling Deserialize with no assembly?");
 
            if (null != _serializationHelper) {
                obj = _serializationHelper.InvokeMember("Deserialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                                                        (Binder)null, (object)null, new object[] {stream, type, context}, (CultureInfo)null);
            } 

            return obj; 
        } 

        static internal object Serialize(Stream stream, object instance, IUdtSerializationContext context) { 
            CheckLoadUDTExtensions();

            object obj = null;
 
            Debug.Assert(null != _serializationHelper, "Why are we calling Serialize with no assembly?");
 
            if (null != _serializationHelper) { 
                obj = _serializationHelper.InvokeMember("Serialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                                                        (Binder)null, (object)null, new object[] {stream, instance, context}, (CultureInfo)null); 
            }

            return obj;
        } 

        static internal int SizeInBytes(object instance) { 
            CheckLoadUDTExtensions(); 

            int size = 0; 

            Debug.Assert(null != _serializationHelper, "Why are we calling Deserialize with no assembly?");

            if (null != _serializationHelper) { 
                size = (int) _serializationHelper.InvokeMember("SizeInBytes", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                                                         (Binder)null, (object)null, new object[] {instance}, (CultureInfo)null); 
            } 

            return size; 
        }
#endif

        // updates our context with any changes made to the memory-mapped data by an external process 
        static private void RefreshMemoryMappedData(SqlDebugContext sdc) {
            Debug.Assert(ADP.PtrZero != sdc.pMemMap, "SQL Debug: invalid null value for pMemMap!"); 
            // copy memory mapped file contents into managed types 
            MEMMAP memMap = (MEMMAP)Marshal.PtrToStructure(sdc.pMemMap, typeof(MEMMAP));
            sdc.dbgpid = memMap.dbgpid; 
            sdc.fOption = (memMap.fOption == 1) ? true : false;
            // xlate ansi byte[] -> managed strings
            Encoding cp = System.Text.Encoding.GetEncoding(TdsEnums.DEFAULT_ENGLISH_CODE_PAGE_VALUE);
            sdc.machineName = cp.GetString(memMap.rgbMachineName, 0, memMap.rgbMachineName.Length); 
            sdc.sdiDllName = cp.GetString(memMap.rgbDllName, 0, memMap.rgbDllName.Length);
            // just get data reference 
            sdc.data = memMap.rgbData; 
        }
 
        public void ResetStatistics() {
            if (IsContextConnection) {
                throw SQL.NotAvailableOnContextConnection();
            } 

            if (null != Statistics) { 
                Statistics.Reset(); 
                if (ConnectionState.Open == State) {
                    // update timestamp; 
                    ADP.TimerCurrent(out _statistics._openTimestamp);
                }
            }
        } 

        public IDictionary RetrieveStatistics() { 
            if (IsContextConnection) { 
                throw SQL.NotAvailableOnContextConnection();
            } 

            if (null != Statistics) {
                UpdateStatistics();
                return Statistics.GetHashtable(); 
            }
            else { 
                return new SqlStatistics().GetHashtable(); 
            }
        } 

        private void UpdateStatistics() {
            if (ConnectionState.Open == State) {
                // update timestamp 
                ADP.TimerCurrent(out _statistics._closeTimestamp);
            } 
            // delegate the rest of the work to the SqlStatistics class 
            Statistics.UpdateStatistics();
        } 

        //
        // UDT SUPPORT
        // 

        // 
        internal static void CheckGetExtendedUDTInfo(SqlMetaDataPriv metaData, bool fThrow) { 
            if (metaData.udtType == null) { // If null, we have not obtained extended info.
                Debug.Assert(!ADP.IsEmpty(metaData.udtAssemblyQualifiedName), "Unexpected state on GetUDTInfo"); 
                // 2nd argument determines whether exception from Assembly.Load is thrown.
    			metaData.udtType = Type.GetType(metaData.udtAssemblyQualifiedName, fThrow);
                if (fThrow && metaData.udtType == null) {
                    // 
                    throw SQL.UDTUnexpectedResult(metaData.udtAssemblyQualifiedName);
                } 
            } 
        }
 
#if WINFSFunctionality
        private bool GetUdtInfo(ref int dbId, int typeId) {
            int aId = 0;
            String typeName = null; 
            String className = null;
            SqlCommand cmd; 
            SqlParameter p; 
            bool result = true;
            bool isInstantiated = false; 
            bool foundInstantiated = false;
            Int32 genericId = 0;
            int index = 0;
            int typeIdNew = 0; 
            int instantiatedTypeId = 0;	// instanced id of the generic type
            int[] list = new int[1]; 
 
            Debug.Assert(this.AssemblyCache != null, "Cache object is NULL");
            _catalog = this.Database; 

            SqlConnection  con   = null;
            SqlTransaction trans = null; // Pass to connection if using MARS.
 
            try {
                if (this.Parser.MARSOn) { 
                    con   = this; 
                    // NOTE - the following line of code will need to be modified if SqlClient is going
                    // to support parallel transactions.  In that case we will be required to pass the 
                    // transaction all the way down.
                    // NOTE - currently due to the server implementation of UDT materialization, if the
                    // client creates the type and tries to materialize the type through a select in the
                    // same transaction - the user will not be able to if MARS is off.  In that case 
                    // we would have to open a new connection and since we would not be running under the
                    // same transaction at that point, we would not be able to select the UDT info. 
                    // 
                    SqlInternalTransaction currentTransaction = this.Parser.CurrentTransaction;
                    if (null != currentTransaction) { 
                        trans = (SqlTransaction)(currentTransaction.Parent);
                    }
                }
                else { 
                    con = (SqlConnection) ((ICloneable) this).Clone();
                    con.Open(); 
                } 

                Debug.Assert(con.GetOpenConnection().IsWinFS, "Should not enter this code path against non-WinFS server!"); 

                // Call the sp with fully qualified name. This is to avoid generating dynamic sql on the server
                // Also, cloned connections default to the login's database.
                // Also - modified sproc call to obtain DbId since we no longer receive in TDS.  This value is also 
                // returned and cached on the client.
                cmd = new SqlCommand("set @typeDbId=db_id();exec [" + _catalog + "].sys." + TdsEnums.SP_UDTINFO_WINFS + " @dbid=@typeDbId, @id=@typeId", con, trans); 
                cmd.CommandType = CommandType.Text; 

                p = cmd.Parameters.Add("@typeDbId", SqlDbType.Int); 
                p.Value = dbId;
                p = cmd.Parameters.Add("@typeId", SqlDbType.Int);
                p.Value = typeId;
 
                using (SqlDataReader r = cmd.ExecuteReader()) {
                    //Read info regarding UDT. 
                    //see if we are dealing with collection valued UDTs or simple UDTs 
                    while (r.Read()) {
                        AssemblyName aName = new AssemblyName(); 

                        dbId = r.GetInt32(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_DB_ID));
                        _dbId = dbId; // Since we no longer have DbId from TDS, obtain from server and cache.
 
                        typeIdNew = r.GetInt32(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_TYPE_ID));
                        isInstantiated = r.GetBoolean(r.GetOrdinal(UDTINFOWinFS_Text.UDT_IS_INSTANTIATED_TYPE)); 
 
                        if (!isInstantiated) {
                            // isInstantiated == true means it's an istantiation of generic UDT 
                            aId = r.GetInt32(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_ASSEMBLY_ID));
                        }
                        else {
                            aId = 0; 
                            Debug.Assert(instantiatedTypeId == 0, "Attempted to assign instantiated type id twice");
                            instantiatedTypeId = typeIdNew; 
                        } // do we need typeIdNew in this case? 

                        //skip maxlen,  for now 
                        typeName = r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_CATALOG_NAME)) + "." + r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_SCHEMA_NAME)) + "." + r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_NAME));

                        if (!isInstantiated) { // again it's not instantiated means its a regular UDT
                            className = r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_BOUND_CLASS)); 
                            aName = GetAssemblyName(r, con);
                        } 
                        else { 
                            Debug.Assert(!foundInstantiated, "MultiValued Type appeared twice in the result set!");
                            foundInstantiated = true; 
                            genericId = r.GetInt32(r.GetOrdinal(UDTINFOWinFS_Text.UDT_GENERIC_TYPE));
                            aId = 0;
                            className = String.Empty;
                        } 

                        if (foundInstantiated) 
                            index = r.GetInt32(r.GetOrdinal(UDTINFOWinFS_Text.UDT_PARAMETER_ORDINAL));//index of this type in the generic parameter list 

                        // index=0 means it's the instantiated type, index>0 is types of typeparameter 
                        // instantiated types dont have assemblies.
                        if (!isInstantiated && false == this.AssemblyCache.AddAssemblyToCache(dbId, aId, aName, 1))
                            throw SQL.UDTUnexpectedResult(aName.ToString());
 
                        //register the type in all cases.
                        if (false == this.AssemblyCache.AddTypeRefToCache(dbId, typeIdNew, typeName, className, aId, isInstantiated)) 
                            throw SQL.UDTUnexpectedResult(aName.ToString()); 

                        if (foundInstantiated && (index != 0)) { 
                            //make sure this type if present before we exit this function
                            //we could do a defered download but it may complicate things.
                            //Keep it simple for now. Download all parameter type assemblies
                            EnsureAssembly(dbId, aId, aName); 
                            Debug.Assert(index <= 1, "Not supposed to have more than 1 generic parameter!");
                            if (index > 1) 
                                throw SQL.UDTUnexpectedResult(aName.ToString()); 
                            list[index - 1] = typeIdNew;
                        } 

                        if (!foundInstantiated)
                            break; //for normal types, there is only one row
                    } // end of while loop! 

                    //shouldn't have anymore rows 
                    if (true == r.Read()) 
                        throw SQL.UDTUnexpectedResult(typeName);
 
                    if (foundInstantiated) {
                        //add the type ids to the generic type
                        TypeInfo info = this.AssemblyCache.GetTypeInfo(dbId, instantiatedTypeId);
                        Debug.Assert(info != null, "BAD STATE!!"); 
                        Debug.Assert(info.isInstantiated, "Adding Generic parameters to non-generic type!!");
                        info.genericType = genericId; 
                        info.parameters = new int[index]; 
                        for (int i = 0; i < index; i++) {
                            info.parameters[i] = list[i]; 
                        }
                    }
                }
            } 
            finally {
                if (!this.Parser.MARSOn && con != null) 
                    con.Close(); 
            }
 
            return result;
        }

        internal AssemblyName GetAssemblyName(SqlDataReader r, SqlConnection con) { 
            object key = null;
            AssemblyName aName = new AssemblyName(); 
            SqlString version;; 

            //skip prog_id, is_fixed_len, is_binary_ordered 
            aName.Name = r.GetString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.ASSEMBLY_NAME));
            version = r.GetSqlString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.ASSEMBLY_VERSION));

            if (version.IsNull == false) 
                aName.Version = new Version(version.Value);
 
            key = r.GetValue(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.ASSEMBLY_PUBLICKEY)); 

            if (key != System.DBNull.Value) 
                aName.SetPublicKeyToken((byte[])key);

            SqlString cult;
 
            cult = r.GetSqlString(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.ASSEMBLY_CULTUREINFO));
 
            if (cult.IsNull == false) 
                aName.CultureInfo = new CultureInfo(cult.Value);
            else 
                aName.CultureInfo = CultureInfo.InvariantCulture;

            //
 
            //skip permissions
            SqlBinary blob; 
 
            blob = r.GetSqlBinary(r.GetOrdinal(UDTINFOYukonOrWinFS_Text.UDT_METADATA));
 
            return aName;
        }

        void EnsureAssembly(int dbId, int aId, AssemblyName aName) { 
            if (false == this.AssemblyCache.FindAndLoadAssembly(dbId, aId, false)) {
                // Auto assembly download is disabled for Whidbey. 
                // AssemblyDownloadHelper(dbId, aId); 
                throw SQL.UDTCantLoadAssembly(aName.ToString());
            } 
        }

        //return the type instance based on dbid and typeid.
        //if the assembly is not available on the client, it will be downloaded. 
        internal Type GetUdtType(int dbId, int typeId) {
            TypeInfo info = null; 
            AssemblyInfo aInfo = null; 
            int aId = 0;
 
            //see if it is available in the cache
            info = this.AssemblyCache.GetTypeInfo(dbId, typeId);
            if (null == info) {
                //query the UDT info from the server. This method should NOT fail. 
                if (false == GetUdtInfo(ref dbId, typeId))
                    throw SQL.UDTInvalidDbId(dbId, typeId); 
 
                //above method will add it to the cache
                info = this.AssemblyCache.GetTypeInfo(dbId, typeId); 
                Debug.Assert(info != null, "SqlClient is in inconsistent state Inconsistent state!!. AssemblyLoad has failed");

                if (info == null)
                    throw SQL.UDTInvalidDbId(dbId, typeId); // not the correct exception, but prevents later AV for now... 
            }
 
            //branch out for generic collection udts 
            if (info.isInstantiated)
                return GetUdtTypeInstantiated(dbId, info); 
            aId = info.assemblyId;
            //get assembly info
            aInfo = this.AssemblyCache.GetAssemblyInfo(dbId, aId);
            Debug.Assert(aInfo != null, "INVALID!!"); 

            if (aInfo == null) 
                throw SQL.UDTUnexpectedResult(info.typeName); 

            //if its not already loaded, and could not be located on the machine... 
            if ((aInfo.assemblyState != AssemblyState.Loaded) && (false == this.AssemblyCache.FindAndLoadAssembly(dbId, aId, false))) {
                throw SQL.UDTCantLoadAssembly(aInfo.assemblyName.ToString());
            }
 
            //add the type to the cache with a reference to the assembly implementation.
            if (false == this.AssemblyCache.AddTypeRefToCache(dbId, typeId, info.typeName, info.className, aId, false)) 
                throw SQL.UDTUnexpectedResult(info.typeName); 

            //now retrieve the Type() instance 
            return this.AssemblyCache.GetTypeFromId(dbId, typeId);
        }

        internal static Type GetMultiSetType() { 
            // For winFS, this is always MultiSet.
            Type type = null; 
            CheckLoadUDTExtensions(); 

            if (null != _udtExtensionsAssembly) { 
                // Retry logic due to CLR breaking change of generics generated names to
                // enable overloading on arity.
                //
                type = _udtExtensionsAssembly.GetType("System.Data.Sql.MultiSet`1"); 
                if (type == null) {
                    type = _udtExtensionsAssembly.GetType("System.Data.Sql.MultiSet!1"); 
 
                    if (type == null) {
                        type = _udtExtensionsAssembly.GetType("System.Data.Sql.MultiSet"); 
                    }
                }
                Debug.Assert(type != null, "null type is not expected!");
            } 

            return type; 
        } 

 
        //if this is a generic udt, all the dependencies must have been downloaded during GetUdtInfo.
        internal Type GetUdtTypeInstantiated(int dbId, TypeInfo info) {
            //Make sure the type is a generic type
            Debug.Assert(info != null, "Invalid TypeInfo"); 
            Debug.Assert(info.isInstantiated, "GetUdtTypeInstantiated called on non generic type!!!");
            if (info.typeRef != null) 
                return info.typeRef; 

            Debug.Assert(info.genericType == (int) TdsEnums.GenericType.MultiSet, "invalid state"); 
            Type generic = GetMultiSetType();
            Debug.Assert(generic.IsGenericType, "Bad Generic TypeInfo!!!");
            Debug.Assert(info.parameters != null && info.parameters.Length == 1, "Bad Generic TypeInfo!!!");
            Type[] paramTypes = new Type[1]; 
            paramTypes[0] = GetUdtType(dbId, info.parameters[0]);
            Type target = generic.MakeGenericType(paramTypes); 
            info.typeRef = target; 
            return target;
 
        }

        internal IUdtSerializationContext GetCurrentContext() {
            // Only to be called from the parameter case... 
            // called if the caller has no context info for extracting the context object.
            if (null == _ctx) { 
                _ctx = new SqlSerializationContext(this, _dbId); 
            }
            return _ctx; 
        }
#endif

        internal object GetUdtValue(object value, SqlMetaDataPriv metaData, bool returnDBNull) { 
            if (returnDBNull && ADP.IsNull(value)) {
                return DBNull.Value; 
            } 

            object o = null; 

            // Since the serializer doesn't handle nulls...
            if (ADP.IsNull(value)) {
                Type t = metaData.udtType; 
                Debug.Assert(t != null, "Unexpected null of udtType on GetUdtValue!");
                o = t.InvokeMember("Null", BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Static, null, null, new Object[]{}, CultureInfo.InvariantCulture); 
                Debug.Assert(o != null); 
                return o;
            } 
            else {
#if WINFSFunctionality
                IUdtSerializationContext ctx = null;
                if (this.GetOpenConnection().IsWinFS) { 
                    ctx = this.GetCurrentContext();
                } 
#endif 

                MemoryStream stm = new MemoryStream((byte[]) value); 

#if WINFSFunctionality
                if (ctx != null) { // If WinFS, then ctx is non-null.
                    o = SqlConnection.Deserialize(stm, metaData.udtType, ctx); 
                }
                else { 
#endif 
                    o = SerializationHelperSql9.Deserialize(stm, metaData.udtType);
#if WINFSFunctionality 
                }
#endif

                Debug.Assert(o != null, "object could NOT be created"); 
                return o;
            } 
        } 

        internal byte[] GetBytes(object o) { 
            Microsoft.SqlServer.Server.Format format  = Microsoft.SqlServer.Server.Format.Native;
            int    maxSize = 0;
            return GetBytes(o, out format, out maxSize);
        } 

        internal byte[] GetBytes(object o, out Microsoft.SqlServer.Server.Format format, out int maxSize) { 
            SqlUdtInfo attr = AssemblyCache.GetInfoFromType(o.GetType()); 
            maxSize = attr.MaxByteSize;
            format  = attr.SerializationFormat; 

            if (maxSize < -1 || maxSize >= UInt16.MaxValue) { // Do we need this?  Is this the right place?
                throw new InvalidOperationException(o.GetType() + ": invalid Size");
            } 

            byte[] retval; 
 
            using (MemoryStream stm = new MemoryStream(maxSize < 0 ? 0 : maxSize)) {
#if WINFSFunctionality 
                IUdtSerializationContext ctx = null;
                if (this.GetOpenConnection().IsWinFS) {
                    ctx = this.GetCurrentContext();
                } 

                if (null == ctx) { 
#endif 
                    SerializationHelperSql9.Serialize(stm, o);
#if WINFSFunctionality 
                }
                else {
                    SqlConnection.Serialize(stm, o, ctx);
                } 
#endif
                retval = stm.ToArray(); 
            } 
            return retval;
        } 
    } // SqlConnection

    //
 

 
 

    [ 
    ComVisible(true),
    ClassInterface(ClassInterfaceType.None),
    Guid("afef65ad-4577-447a-a148-83acadd3d4b9"),
    ] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name = "FullTrust")]
#if WINFSInternalOnly 
    internal 
#else
    public 
#endif
    sealed class SQLDebugging: ISQLDebug {

        // Security stuff 
        const int STANDARD_RIGHTS_REQUIRED = (0x000F0000);
        const int DELETE = (0x00010000); 
        const int READ_CONTROL = (0x00020000); 
        const int WRITE_DAC = (0x00040000);
        const int WRITE_OWNER = (0x00080000); 
        const int SYNCHRONIZE = (0x00100000);
        const int FILE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x000001FF);
        const uint GENERIC_READ = (0x80000000);
        const uint GENERIC_WRITE = (0x40000000); 
        const uint GENERIC_EXECUTE = (0x20000000);
        const uint GENERIC_ALL = (0x10000000); 
 
        const int SECURITY_DESCRIPTOR_REVISION = (1);
        const int ACL_REVISION = (2); 

        const int SECURITY_AUTHENTICATED_USER_RID = (0x0000000B);
        const int SECURITY_LOCAL_SYSTEM_RID = (0x00000012);
        const int SECURITY_BUILTIN_DOMAIN_RID = (0x00000020); 
        const int SECURITY_WORLD_RID = (0x00000000);
        const byte SECURITY_NT_AUTHORITY = 5; 
        const int DOMAIN_GROUP_RID_ADMINS = (0x00000200); 
        const int DOMAIN_ALIAS_RID_ADMINS = (0x00000220);
 
        const int sizeofSECURITY_ATTRIBUTES = 12; // sizeof(SECURITY_ATTRIBUTES);
        const int sizeofSECURITY_DESCRIPTOR = 20; // sizeof(SECURITY_DESCRIPTOR);
        const int sizeofACCESS_ALLOWED_ACE = 12; // sizeof(ACCESS_ALLOWED_ACE);
        const int sizeofACCESS_DENIED_ACE = 12; // sizeof(ACCESS_DENIED_ACE); 
        const int sizeofSID_IDENTIFIER_AUTHORITY = 6; // sizeof(SID_IDENTIFIER_AUTHORITY)
        const int sizeofACL = 8; // sizeof(ACL); 
 
        private IntPtr CreateSD(ref IntPtr pDacl) {
            IntPtr pSecurityDescriptor = IntPtr.Zero; 
            IntPtr pUserSid = IntPtr.Zero;
            IntPtr pAdminSid = IntPtr.Zero;
            IntPtr pNtAuthority = IntPtr.Zero;
            int cbAcl = 0; 
            bool status = false;
 
            pNtAuthority = Marshal.AllocHGlobal(sizeofSID_IDENTIFIER_AUTHORITY); 
            if (pNtAuthority == IntPtr.Zero)
                goto cleanup; 
            Marshal.WriteInt32(pNtAuthority, 0, 0);
            Marshal.WriteByte(pNtAuthority, 4, 0);
            Marshal.WriteByte(pNtAuthority, 5, SECURITY_NT_AUTHORITY);
 
            status =
            NativeMethods.AllocateAndInitializeSid( 
            pNtAuthority, 
            (byte)1,
            SECURITY_AUTHENTICATED_USER_RID, 
            0,
            0,
            0,
            0, 
            0,
            0, 
            0, 
            ref pUserSid);
 
            if (!status || pUserSid == IntPtr.Zero) {
                goto cleanup;
            }
            status = 
            NativeMethods.AllocateAndInitializeSid(
            pNtAuthority, 
            (byte)2, 
            SECURITY_BUILTIN_DOMAIN_RID,
            DOMAIN_ALIAS_RID_ADMINS, 
            0,
            0,
            0,
            0, 
            0,
            0, 
            ref pAdminSid); 

            if (!status || pAdminSid == IntPtr.Zero) { 
                goto cleanup;
            }
            status = false;
            pSecurityDescriptor = Marshal.AllocHGlobal(sizeofSECURITY_DESCRIPTOR); 
            if (pSecurityDescriptor == IntPtr.Zero) {
                goto cleanup; 
            } 
            for (int i = 0; i < sizeofSECURITY_DESCRIPTOR; i++)
                Marshal.WriteByte(pSecurityDescriptor, i, (byte)0); 
            cbAcl = sizeofACL
            + (2 * (sizeofACCESS_ALLOWED_ACE))
            + sizeofACCESS_DENIED_ACE
            + NativeMethods.GetLengthSid(pUserSid) 
            + NativeMethods.GetLengthSid(pAdminSid);
 
            pDacl = Marshal.AllocHGlobal(cbAcl); 
            if (pDacl == IntPtr.Zero) {
                goto cleanup; 
            }
            // rights must be added in a certain order.  Namely, deny access first, then add access
            if (NativeMethods.InitializeAcl(pDacl, cbAcl, ACL_REVISION))
                if (NativeMethods.AddAccessDeniedAce(pDacl, ACL_REVISION, WRITE_DAC, pUserSid)) 
                    if (NativeMethods.AddAccessAllowedAce(pDacl, ACL_REVISION, GENERIC_READ, pUserSid))
                        if (NativeMethods.AddAccessAllowedAce(pDacl, ACL_REVISION, GENERIC_ALL, pAdminSid)) 
                            if (NativeMethods.InitializeSecurityDescriptor(pSecurityDescriptor, SECURITY_DESCRIPTOR_REVISION)) 
                                if (NativeMethods.SetSecurityDescriptorDacl(pSecurityDescriptor, true, pDacl, false)) {
                                    status = true; 
                                }

            cleanup :
            if (pNtAuthority != IntPtr.Zero) { 
                Marshal.FreeHGlobal(pNtAuthority);
            } 
            if (pAdminSid != IntPtr.Zero) 
                NativeMethods.FreeSid(pAdminSid);
            if (pUserSid != IntPtr.Zero) 
                NativeMethods.FreeSid(pUserSid);
            if (status)
                return pSecurityDescriptor;
            else { 
                if (pSecurityDescriptor != IntPtr.Zero) {
                    Marshal.FreeHGlobal(pSecurityDescriptor); 
                } 
            }
            return IntPtr.Zero; 
        }

        bool ISQLDebug.SQLDebug(int dwpidDebugger, int dwpidDebuggee, [MarshalAs(UnmanagedType.LPStr)] string pszMachineName,
        [MarshalAs(UnmanagedType.LPStr)] string pszSDIDLLName, int dwOption, int cbData, byte[] rgbData) { 
            bool result = false;
            IntPtr hFileMap = IntPtr.Zero; 
            IntPtr pMemMap = IntPtr.Zero; 
            IntPtr pSecurityDescriptor = IntPtr.Zero;
            IntPtr pSecurityAttributes = IntPtr.Zero; 
            IntPtr pDacl = IntPtr.Zero;

            // validate the structure
            if (null == pszMachineName || null == pszSDIDLLName) 
                return false;
 
            if (pszMachineName.Length > TdsEnums.SDCI_MAX_MACHINENAME || 
            pszSDIDLLName.Length > TdsEnums.SDCI_MAX_DLLNAME)
                return false; 

            // note that these are ansi strings
            Encoding cp = System.Text.Encoding.GetEncoding(TdsEnums.DEFAULT_ENGLISH_CODE_PAGE_VALUE);
            byte[] rgbMachineName = cp.GetBytes(pszMachineName); 
            byte[] rgbSDIDLLName = cp.GetBytes(pszSDIDLLName);
 
            if (null != rgbData && cbData > TdsEnums.SDCI_MAX_DATA) 
                return false;
 
            string mapFileName;

            // If Win2k or later, prepend "Global\\" to enable this to work through TerminalServices.
            if (ADP.IsPlatformNT5) { 
                mapFileName = "Global\\" + TdsEnums.SDCI_MAPFILENAME;
            } 
            else { 
                mapFileName = TdsEnums.SDCI_MAPFILENAME;
            } 

            mapFileName = mapFileName + dwpidDebuggee.ToString(CultureInfo.InvariantCulture);

            // Create Security Descriptor 
            pSecurityDescriptor = CreateSD(ref pDacl);
            pSecurityAttributes = Marshal.AllocHGlobal(sizeofSECURITY_ATTRIBUTES); 
            if ((pSecurityDescriptor == IntPtr.Zero) || (pSecurityAttributes == IntPtr.Zero)) 
                return false;
 
            Marshal.WriteInt32(pSecurityAttributes, 0, sizeofSECURITY_ATTRIBUTES); // nLength = sizeof(SECURITY_ATTRIBUTES)
            Marshal.WriteIntPtr(pSecurityAttributes, 4, pSecurityDescriptor); // lpSecurityDescriptor = pSecurityDescriptor
            Marshal.WriteInt32(pSecurityAttributes, 8, 0); // bInheritHandle = FALSE
            hFileMap = NativeMethods.CreateFileMappingA( 
            ADP.InvalidPtr/*INVALID_HANDLE_VALUE*/,
            pSecurityAttributes, 
            0x4/*PAGE_READWRITE*/, 
            0,
            Marshal.SizeOf(typeof(MEMMAP)), 
            mapFileName);

            if (IntPtr.Zero == hFileMap) {
                goto cleanup; 
            }
 
 
            pMemMap = NativeMethods.MapViewOfFile(hFileMap, 0x6/*FILE_MAP_READ|FILE_MAP_WRITE*/, 0, 0, IntPtr.Zero);
 
            if (IntPtr.Zero == pMemMap) {
                goto cleanup;
            }
 
            // copy data to memory-mapped file
            // layout of MEMMAP structure is: 
            // uint dbgpid 
            // uint fOption
            // byte[32] machineName 
            // byte[16] sdiDllName
            // uint dbData
            // byte[255] vData
            int offset = 0; 
            Marshal.WriteInt32(pMemMap, offset, (int)dwpidDebugger);
            offset += 4; 
            Marshal.WriteInt32(pMemMap, offset, (int)dwOption); 
            offset += 4;
            Marshal.Copy(rgbMachineName, 0, ADP.IntPtrOffset(pMemMap, offset), rgbMachineName.Length); 
            offset += TdsEnums.SDCI_MAX_MACHINENAME;
            Marshal.Copy(rgbSDIDLLName, 0, ADP.IntPtrOffset(pMemMap, offset), rgbSDIDLLName.Length);
            offset += TdsEnums.SDCI_MAX_DLLNAME;
            Marshal.WriteInt32(pMemMap, offset, (int)cbData); 
            offset += 4;
            if (null != rgbData) { 
                Marshal.Copy(rgbData, 0, ADP.IntPtrOffset(pMemMap, offset), (int)cbData); 
            }
            NativeMethods.UnmapViewOfFile(pMemMap); 
            result = true;
        cleanup :
            if (result == false) {
                if (hFileMap != IntPtr.Zero) 
                    NativeMethods.CloseHandle(hFileMap);
            } 
            if (pSecurityAttributes != IntPtr.Zero) 
                Marshal.FreeHGlobal(pSecurityAttributes);
            if (pSecurityDescriptor != IntPtr.Zero) 
                Marshal.FreeHGlobal(pSecurityDescriptor);
            if (pDacl != IntPtr.Zero)
                Marshal.FreeHGlobal(pDacl);
            return result; 
        }
    } 
 
    // this is a private interface to com+ users
    // do not change this guid 
    [
    ComImport,
    ComVisible(true),
    Guid("6cb925bf-c3c0-45b3-9f44-5dd67c7b7fe8"), 
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    BestFitMapping(false, ThrowOnUnmappableChar = true), 
    ] 
    interface ISQLDebug {
 
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name = "FullTrust")]
        bool SQLDebug(
        int dwpidDebugger,
        int dwpidDebuggee, 
        [MarshalAs(UnmanagedType.LPStr)] string pszMachineName,
        [MarshalAs(UnmanagedType.LPStr)] string pszSDIDLLName, 
        int dwOption, 
        int cbData,
        byte[] rgbData); 
    }

    sealed class SqlDebugContext: IDisposable {
        // context data 
        internal uint pid = 0;
        internal uint tid = 0; 
        internal bool active = false; 
        // memory-mapped data
        internal IntPtr pMemMap = ADP.PtrZero; 
        internal IntPtr hMemMap = ADP.PtrZero;
        internal uint dbgpid = 0;
        internal bool fOption = false;
        internal string machineName = null; 
        internal string sdiDllName = null;
        internal byte[] data = null; 
 
        public void Dispose() {
            Dispose(true); 
            GC.SuppressFinalize(this);
        }
        private void Dispose (bool disposing) {
            if (disposing) { 
                // Nothing to do here
                ; 
            } 
            if (pMemMap != IntPtr.Zero) {
                NativeMethods.UnmapViewOfFile(pMemMap); 
                pMemMap = IntPtr.Zero;
            }
            if (hMemMap != IntPtr.Zero) {
                NativeMethods.CloseHandle(hMemMap); 
                hMemMap = IntPtr.Zero;
            } 
            active = false; 
        }
 
        ~SqlDebugContext() {
                Dispose(false);
        }
 
    }
 
    // native interop memory mapped structure for sdi debugging 
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    internal struct MEMMAP { 
        [MarshalAs(UnmanagedType.U4)]
        internal uint dbgpid; // id of debugger
        [MarshalAs(UnmanagedType.U4)]
        internal uint fOption; // 1 - start debugging, 0 - stop debugging 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal byte[] rgbMachineName; 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] 
        internal byte[] rgbDllName;
        [MarshalAs(UnmanagedType.U4)] 
        internal uint cbData;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        internal byte[] rgbData;
    } 
} // System.Data.SqlClient namespace
 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
