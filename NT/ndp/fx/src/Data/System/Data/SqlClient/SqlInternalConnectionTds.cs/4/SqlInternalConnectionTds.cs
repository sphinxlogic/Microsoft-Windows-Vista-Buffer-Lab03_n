//------------------------------------------------------------------------------ 
// <copyright file="SqlInternalConnectionTds.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient
{ 
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common; 
    using System.Data.ProviderBase;
    using System.Diagnostics; 
    using System.Globalization; 
    using System.Reflection;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions; 
    using System.Text;
    using System.Threading; 
    using SysTx = System.Transactions; 

    sealed internal class SqlInternalConnectionTds : SqlInternalConnection, IDisposable { 
        // CONNECTION AND STATE VARIABLES
        private readonly SqlConnectionPoolGroupProviderInfo _poolGroupProviderInfo; // will only be null when called for ChangePassword, or creating SSE User Instance
        private TdsParser                _parser;
        private SqlLoginAck              _loginAck; 

        // FOR POOLING 
        private bool                     _fConnectionOpen = false; 

        // FOR CONNECTION RESET MANAGEMENT 
        private bool                     _fResetConnection;
        private string                   _originalDatabase;
        private string                   _currentFailoverPartner;                     // only set by ENV change from server
        private string                   _originalLanguage; 
        private string                   _currentLanguage;
        private int                      _currentPacketSize; 
        private int                      _asyncCommandCount; // number of async Begins minus number of async Ends. 

        // FOR SSE 
        private string                   _instanceName = String.Empty;

        // FOR NOTIFICATIONS
        private DbConnectionPoolIdentity _identity; // Used to lookup info for notification matching Start(). 

        // OTHER STATE VARIABLES AND REFERENCES 
 
        // don't use a SqlCommands collection because this is an internal tracking list.  That is, we don't want
        // the command to "know" it's in a collection. 
        private List<WeakReference>      _preparedCommands; //

#if WINFSFunctionality
        //Hash table of UDT caches. Created in CreateAssemblyCache 
        private static Dictionary<string,AssemblyCache> _assemblyCacheTable = new Dictionary<string,AssemblyCache>();
        private static object _assemblyCacheLock = new object(); 
#endif 

        // although the new password is generally not used it must be passed to the c'tor 
        // the new Login7 packet will always write out the new password (or a length of zero and no bytes if not present)
        //
        internal SqlInternalConnectionTds(DbConnectionPoolIdentity identity, SqlConnectionString connectionOptions, object providerInfo, string newPassword, SqlConnection owningObject, bool redirectedUserInstance) : base(connectionOptions) {
#if DEBUG 
            try { // use this to help validate this object is only created after the following permission has been previously demanded in the current codepath
                if (null != owningObject) { 
                    owningObject.UserConnectionOptions.DemandPermission(); 
                }
                else { 
                    connectionOptions.DemandPermission();
                }
            }
            catch(System.Security.SecurityException) { 
                System.Diagnostics.Debug.Assert(false, "unexpected SecurityException for current codepath");
                throw; 
            } 
#endif
            if (connectionOptions.UserInstance && InOutOfProcHelper.InProc) { 
                throw SQL.UserInstanceNotAvailableInProc();
            }

            _identity = identity; 
            Debug.Assert(null!=newPassword, "newPassword argument must not be null");
            _poolGroupProviderInfo = (SqlConnectionPoolGroupProviderInfo)providerInfo; 
            _fResetConnection = connectionOptions.ConnectionReset; 
            if (_fResetConnection) {
                _originalDatabase = connectionOptions.InitialCatalog; 
                _originalLanguage = connectionOptions.CurrentLanguage;
            }

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                    OpenLoginEnlist(owningObject, connectionOptions, newPassword, redirectedUserInstance); 
#if DEBUG
                } 
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            }
            catch (System.OutOfMemoryException) {
                DoomThisConnection(); 
                throw;
            } 
            catch (System.StackOverflowException) { 
                DoomThisConnection();
                throw; 
            }
            catch (System.Threading.ThreadAbortException) {
                DoomThisConnection();
                throw; 
            }
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnectionTds.ctor|ADV> %d#, constructed new TDS internal connection\n", ObjectID); 
            }
        } 

#if WINFSFunctionality
        internal AssemblyCache AssemblyCache{
            get{ 
                return CreateAssemblyCache();
            } 
        } 
#endif
 
        override internal SqlInternalTransaction CurrentTransaction {
            get {
                return _parser.CurrentTransaction;
            } 
        }
 
        override internal SqlInternalTransaction AvailableInternalTransaction { 
            get {
                return _parser._fResetConnection ? null : CurrentTransaction; 
            }
        }

 
        override internal SqlInternalTransaction PendingTransaction {
            get { 
                return _parser.PendingTransaction; 
            }
        } 

        internal DbConnectionPoolIdentity Identity {
            get {
                return _identity; 
            }
        } 
 
        internal string InstanceName {
            get { 
                return _instanceName;
            }
        }
 
        override internal bool IsLockedForBulkCopy {
            get { 
                return (!Parser.MARSOn && Parser._physicalStateObj.BcpLock); 
            }
        } 

        override protected internal bool IsNonPoolableTransactionRoot {
            get {
                return IsTransactionRoot && (!IsKatmaiOrNewer || null == Pool); 
            }
        } 
 
        override internal bool IsShiloh {
            get { 
                return _loginAck.isVersion8;
            }
        }
 
        override internal bool IsYukonOrNewer {
            get { 
                return _parser.IsYukonOrNewer; 
            }
        } 

        override internal bool IsKatmaiOrNewer {
            get {
                return _parser.IsKatmaiOrNewer; 
            }
        } 
 
#if WINFSFunctionality
        override internal bool IsWinFS { 
            get {
                return _parser.IsWinFS;
            }
        } 
#endif
 
        internal int PacketSize { 
            get {
                return _currentPacketSize; 
            }
        }

        internal TdsParser Parser { 
            get {
                return _parser; 
            } 
        }
 
        internal string ServerProvidedFailOverPartner {
            get {
                return  _currentFailoverPartner;
            } 
        }
 
        internal SqlConnectionPoolGroupProviderInfo PoolGroupProviderInfo { 
            get {
                return _poolGroupProviderInfo; 
            }
        }

        override protected bool ReadyToPrepareTransaction { 
            get {
                // 
                bool result = (null == FindLiveReader(null)); // can't prepare with a live data reader... 
                return result;
            } 
        }

        override public string ServerVersion {
            get { 
                return(String.Format((IFormatProvider)null, "{0:00}.{1:00}.{2:0000}", _loginAck.majorVersion,
                       (short) _loginAck.minorVersion, _loginAck.buildNum)); 
            } 
        }
 

        ////////////////////////////////////////////////////////////////////////////////////////
        // GENERAL METHODS
        //////////////////////////////////////////////////////////////////////////////////////// 

        override protected void ChangeDatabaseInternal(string database) { 
            // MDAC 73598 - add brackets around database 
            database = SqlConnection.FixupDatabaseTransactionName(database);
            _parser.TdsExecuteSQLBatch("use " + database, ConnectionOptions.ConnectTimeout, null, _parser._physicalStateObj); 
            _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj);
        }

        override public void Dispose() { 
            if (Bid.AdvancedOn) {
                Bid.Trace("<sc.SqlInternalConnectionTds.Dispose|ADV> %d# disposing\n", base.ObjectID); 
            } 
            try {
                TdsParser parser = Interlocked.Exchange(ref _parser, null);  // guard against multiple concurrent dispose calls -- Delegated Transactions might cause this. 

                Debug.Assert(parser != null && _fConnectionOpen || parser == null && !_fConnectionOpen, "Unexpected state on dispose");
                if (null != parser) {
                    parser.Disconnect(); 
                }
            } 
            finally { // 
                // close will always close, even if exception is thrown
                // remember to null out any object references 
                _loginAck          = null;
                _fConnectionOpen   = false; // mark internal connection as closed
            }
            base.Dispose(); 
        }
 
        override internal void ValidateConnectionForExecute(SqlCommand command) { 
            SqlDataReader reader = null;
            if (Parser.MARSOn) { 
                if (null != command) { // command can't have datareader already associated with it
                    reader = FindLiveReader(command);
                }
            } 
            else { // single datareader per connection
                reader = FindLiveReader(null); 
            } 
            if (null != reader) {
                // if MARS is on, then a datareader associated with the command exists 
                // or if MARS is off, then a datareader exists
                throw ADP.OpenReaderExists(); // MDAC 66411
            }
            else if (!Parser.MARSOn && Parser._physicalStateObj._pendingData) { 
                Parser._physicalStateObj.CleanWire();
            } 
            Debug.Assert(!Parser._physicalStateObj._pendingData, "Should not have a busy physicalStateObject at this point!"); 

            Parser.RollbackOrphanedAPITransactions(); 
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // POOLING METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        override protected void Activate(SysTx.Transaction transaction) { 
            FailoverPermissionDemand(); // Demand for unspecified failover pooled connections
 
            // When we're required to automatically enlist in transactions and
            // there is one we enlist in it. On the other hand, if there isn't a
            // transaction and we are currently enlisted in one, then we
            // unenlist from it. 
            //
            // Regardless of whether we're required to automatically enlist, 
            // when there is not a current transaction, we cannot leave the 
            // connection enlisted in a transaction.
            if (null != transaction){ 
                if (ConnectionOptions.Enlist) {
                   Enlist(transaction);
                }
            } 
            else {
                Enlist(null); 
            } 
        }
 
        override protected void InternalDeactivate() {
            // When we're deactivated, the user must have called End on all
            // the async commands, or we don't know that we're in a state that
            // we can recover from.  We doom the connection in this case, to 
            // prevent odd cases when we go to the wire.
            if (0 != _asyncCommandCount) { 
                DoomThisConnection(); 
            }
 
            // If we're deactivating with a delegated transaction, we
            // should not be cleaning up the parser just yet, that will
            // cause our transaction to be rolled back and the connection
            // to be reset.  We'll get called again once the delegated 
            // transaction is completed and we can do it all then.
            if (!IsNonPoolableTransactionRoot) { 
                Debug.Assert(null != _parser, "Deactivating a disposed connection?"); 
                _parser.Deactivate(IsConnectionDoomed);
 
                if (!IsConnectionDoomed) {
                    ResetConnection();
                }
            } 
        }
 
        private void ResetConnection() { 
            // For implicit pooled connections, if connection reset behavior is specified,
            // reset the database and language properties back to default.  It is important 
            // to do this on activate so that the hashtable is correct before SqlConnection
            // obtains a clone.

            Debug.Assert(!HasLocalTransactionFromAPI, "Upon ResetConnection SqlInternalConnectionTds has a currently ongoing local transaction."); 
            Debug.Assert(!_parser._physicalStateObj._pendingData, "Upon ResetConnection SqlInternalConnectionTds has pending data.");
 
            if (_fResetConnection) { 
                // Ensure we are either going against shiloh, or we are not enlisted in a
                // distributed transaction - otherwise don't reset! 
                if (IsShiloh) {
                    // Prepare the parser for the connection reset - the next time a trip
                    // to the server is made.
                    _parser.PrepareResetConnection(IsTransactionRoot && !IsNonPoolableTransactionRoot); 
                }
                else if (!IsEnlistedInTransaction) { 
                    // If not Shiloh, we are going against Sphinx.  On Sphinx, we 
                    // may only reset if not enlisted in a distributed transaction.
                    try { 
                        // execute sp
                        _parser.TdsExecuteSQLBatch("sp_reset_connection", 30, null, _parser._physicalStateObj);
                        _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj);
                    } 
                    catch (Exception e) {
                        // 
                        if (!ADP.IsCatchableExceptionType(e)) { 
                            throw;
                        } 

                        DoomThisConnection();
                        ADP.TraceExceptionWithoutRethrow(e);
                    } 
                }
 
                // Reset hashtable values, since calling reset will not send us env_changes. 
                CurrentDatabase = _originalDatabase;
                _currentLanguage = _originalLanguage; 
            }
        }

        internal void DecrementAsyncCount() { 
            Interlocked.Decrement(ref _asyncCommandCount);
        } 
 
        internal void IncrementAsyncCount() {
            Interlocked.Increment(ref _asyncCommandCount); 
        }


        //////////////////////////////////////////////////////////////////////////////////////// 
        // LOCAL TRANSACTION METHODS
        //////////////////////////////////////////////////////////////////////////////////////// 
 
        override internal void DisconnectTransaction(SqlInternalTransaction internalTransaction) {
            TdsParser parser = Parser; 

            if (null != parser) {
                parser.DisconnectTransaction(internalTransaction);
            } 
        }
 
        internal void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso) { 
            ExecuteTransaction(transactionRequest, name, iso, null, false);
        } 

        override internal void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso, SqlInternalTransaction internalTransaction, bool isDelegateControlRequest) {
            if (IsConnectionDoomed) {  // doomed means we can't do anything else...
                if (transactionRequest == TransactionRequest.Rollback 
                 || transactionRequest == TransactionRequest.IfRollback) {
                    return; 
                } 
                throw SQL.ConnectionDoomed();
            } 

            if (transactionRequest == TransactionRequest.Commit
             || transactionRequest == TransactionRequest.Rollback
             || transactionRequest == TransactionRequest.IfRollback) { 
                if (!Parser.MARSOn && Parser._physicalStateObj.BcpLock) {
                    throw SQL.ConnectionLockedForBcpEvent(); 
                } 
            }
 
            string transactionName = (null == name) ? String.Empty : name;

            if (!_parser.IsYukonOrNewer) {
                ExecuteTransactionPreYukon(transactionRequest, transactionName, iso, internalTransaction); 
            }
            else { 
                ExecuteTransactionYukon(transactionRequest, transactionName, iso, internalTransaction, isDelegateControlRequest); 
            }
        } 

        internal void ExecuteTransactionPreYukon(TransactionRequest transactionRequest, string transactionName, IsolationLevel iso, SqlInternalTransaction internalTransaction) {
            StringBuilder sqlBatch = new StringBuilder();
 
            switch (iso) {
                case IsolationLevel.Unspecified: 
                    break; 
                case IsolationLevel.ReadCommitted:
                    sqlBatch.Append(TdsEnums.TRANS_READ_COMMITTED); 
                    sqlBatch.Append(";");
                    break;
                case IsolationLevel.ReadUncommitted:
                    sqlBatch.Append(TdsEnums.TRANS_READ_UNCOMMITTED); 
                    sqlBatch.Append(";");
                    break; 
                case IsolationLevel.RepeatableRead: 
                    sqlBatch.Append(TdsEnums.TRANS_REPEATABLE_READ);
                    sqlBatch.Append(";"); 
                    break;
                case IsolationLevel.Serializable:
                    sqlBatch.Append(TdsEnums.TRANS_SERIALIZABLE);
                    sqlBatch.Append(";"); 
                    break;
                case IsolationLevel.Snapshot: 
                    throw SQL.SnapshotNotSupported(IsolationLevel.Snapshot); 

                case IsolationLevel.Chaos: 
                    throw SQL.NotSupportedIsolationLevel(iso);

                default:
                    throw ADP.InvalidIsolationLevel(iso); 
            }
 
            if (!ADP.IsEmpty(transactionName)) { 
                transactionName = " " + SqlConnection.FixupDatabaseTransactionName(transactionName);
            } 

            switch (transactionRequest) {
                case TransactionRequest.Begin:
                    sqlBatch.Append(TdsEnums.TRANS_BEGIN); 
                    sqlBatch.Append(transactionName);
                    break; 
                case TransactionRequest.Promote: 
                    Debug.Assert(false, "Promote called with transaction name or on pre-Yukon!");
                    break; 
                case TransactionRequest.Commit:
                    sqlBatch.Append(TdsEnums.TRANS_COMMIT);
                    sqlBatch.Append(transactionName);
                    break; 
                case TransactionRequest.Rollback:
                    sqlBatch.Append(TdsEnums.TRANS_ROLLBACK); 
                    sqlBatch.Append(transactionName); 
                    break;
                case TransactionRequest.IfRollback: 
                    sqlBatch.Append(TdsEnums.TRANS_IF_ROLLBACK);
                    sqlBatch.Append(transactionName);
                    break;
                case TransactionRequest.Save: 
                    sqlBatch.Append(TdsEnums.TRANS_SAVE);
                    sqlBatch.Append(transactionName); 
                    break; 
                default:
                    Debug.Assert(false, "Unknown transaction type"); 
                    break;
            }

            _parser.TdsExecuteSQLBatch(sqlBatch.ToString(), ConnectionOptions.ConnectTimeout, null, _parser._physicalStateObj); 
            _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj);
 
            // Prior to Yukon, we didn't have any transaction tokens to manage, 
            // or any feedback to know when one was created, so we just presume
            // that successful execution of the request caused the transaction 
            // to be created, and we set that on the parser.
            if (TransactionRequest.Begin == transactionRequest) {
                Debug.Assert(null != internalTransaction, "Begin Transaction request without internal transaction");
                _parser.CurrentTransaction = internalTransaction; 
            }
        } 
 
        internal void ExecuteTransactionYukon(TransactionRequest transactionRequest, string transactionName, IsolationLevel iso, SqlInternalTransaction internalTransaction, bool isDelegateControlRequest) {
 
            TdsEnums.TransactionManagerRequestType    requestType = TdsEnums.TransactionManagerRequestType.Begin;
            TdsEnums.TransactionManagerIsolationLevel isoLevel    = TdsEnums.TransactionManagerIsolationLevel.ReadCommitted;

            switch (iso) { 
                case IsolationLevel.Unspecified:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Unspecified; 
                    break; 
                case IsolationLevel.ReadCommitted:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.ReadCommitted; 
                    break;
                case IsolationLevel.ReadUncommitted:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.ReadUncommitted;
                    break; 
                case IsolationLevel.RepeatableRead:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.RepeatableRead; 
                    break; 
                case IsolationLevel.Serializable:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Serializable; 
                    break;
                case IsolationLevel.Snapshot:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Snapshot;
                    break; 
                case IsolationLevel.Chaos:
                    throw SQL.NotSupportedIsolationLevel(iso); 
                default: 
                    throw ADP.InvalidIsolationLevel(iso);
            } 

            TdsParserStateObject stateObj = _parser._physicalStateObj;
            TdsParser parser = _parser;
            bool mustPutSession = false; 
            bool mustRelease = false;
            try { 
                switch (transactionRequest) { 
                    case TransactionRequest.Begin:
                        requestType = TdsEnums.TransactionManagerRequestType.Begin; 
                        break;
                    case TransactionRequest.Promote:
                        requestType = TdsEnums.TransactionManagerRequestType.Promote;
                        break; 
                    case TransactionRequest.Commit:
                        requestType = TdsEnums.TransactionManagerRequestType.Commit; 
                        break; 
                    case TransactionRequest.IfRollback:
                        // Map IfRollback to Rollback since with Yukon and beyond we should never need 
                        // the if since the server will inform us when transactions have completed
                        // as a result of an error on the server.
                    case TransactionRequest.Rollback:
                        requestType = TdsEnums.TransactionManagerRequestType.Rollback; 
                        break;
                    case TransactionRequest.Save: 
                        requestType = TdsEnums.TransactionManagerRequestType.Save; 
                        break;
                    default: 
                        Debug.Assert(false, "Unknown transaction type");
                        break;
                }
 

                // 
 

 



 

 
 

 



                if (null != internalTransaction && internalTransaction.IsDelegated) { 
                    if (_parser.MARSOn) {
                        stateObj = _parser.GetSession(this); 
                        mustPutSession = true; 
                    }
                    else if (internalTransaction.OpenResultsCount == 0) { 
                        Monitor.Enter(stateObj);
                        mustRelease = true;

                        if (internalTransaction.OpenResultsCount != 0) { 
                            throw SQL.CannotCompleteDelegatedTransactionWithOpenResults();
                        } 
                    } 
                    else {
                        throw SQL.CannotCompleteDelegatedTransactionWithOpenResults(); 
                    }
                }

                // 

                _parser.TdsExecuteTransactionManagerRequest(null, requestType, transactionName, isoLevel, 
                    ConnectionOptions.ConnectTimeout, internalTransaction, stateObj, isDelegateControlRequest); 
            }
            finally { 
                if (mustPutSession) {
                    parser.PutSession(stateObj);
                }
                if (mustRelease) { 
                    Monitor.Exit(stateObj);
                } 
            } 
        }
 
        ////////////////////////////////////////////////////////////////////////////////////////
        // DISTRIBUTED TRANSACTION METHODS
        ////////////////////////////////////////////////////////////////////////////////////////
 
        override internal void DelegatedTransactionEnded() {
            // 
            base.DelegatedTransactionEnded(); 
        }
 
        override protected byte[] GetDTCAddress() {
            byte[] dtcAddress = _parser.GetDTCAddress(ConnectionOptions.ConnectTimeout, _parser._physicalStateObj);
            Debug.Assert(null != dtcAddress, "null dtcAddress?");
            return dtcAddress; 
        }
 
        override protected void PropagateTransactionCookie(byte[] cookie) { 
            _parser.PropagateDistributedTransaction(cookie, ConnectionOptions.ConnectTimeout, _parser._physicalStateObj);
        } 

        ////////////////////////////////////////////////////////////////////////////////////////
        // LOGIN-RELATED METHODS
        //////////////////////////////////////////////////////////////////////////////////////// 

        private void CompleteLogin(bool enlistOK) { 
            _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj); 

            Debug.Assert(SniContext.Snix_Login == Parser._physicalStateObj.SniContext, String.Format((IFormatProvider)null, "SniContext should be Snix_Login; actual Value: {0}", Parser._physicalStateObj.SniContext)); 
            _parser._physicalStateObj.SniContext = SniContext.Snix_EnableMars;
            _parser.EnableMars(ConnectionOptions.DataSource);

            _fConnectionOpen = true; // mark connection as open 

            // for non-pooled connections, enlist in a distributed transaction 
            // if present - and user specified to enlist 
            if(enlistOK && ConnectionOptions.Enlist) {
                _parser._physicalStateObj.SniContext = SniContext.Snix_AutoEnlist; 
                SysTx.Transaction tx = ADP.GetCurrentTransaction();
                Enlist(tx);
            }
            _parser._physicalStateObj.SniContext=SniContext.Snix_Login; 
        }
 
        private void Login(long timerExpire, string newPassword) { 
            // create a new login record
            SqlLogin login = new SqlLogin(); 

            // gather all the settings the user set in the connection string or
            // properties and do the login
            CurrentDatabase   = ConnectionOptions.InitialCatalog; 
            _currentPacketSize = ConnectionOptions.PacketSize;
            _currentLanguage   = ConnectionOptions.CurrentLanguage; 
 
            int timeout = 0;
 
            // If a timeout tick value is specified, compute the timeout based
            // upon the amount of time left.
            if (Int64.MaxValue != timerExpire) {
                long t = ADP.TimerRemainingSeconds(timerExpire); 

                if ((long)Int32.MaxValue > t) { 
                    timeout = (int)t; 
                }
            } 

            login.timeout          = timeout;

            login.userInstance     = ConnectionOptions.UserInstance; 
            login.hostName         = ConnectionOptions.ObtainWorkstationId();
            login.userName         = ConnectionOptions.UserID; 
            login.password         = ConnectionOptions.Password; 
            login.applicationName  = ConnectionOptions.ApplicationName;
 
            login.language         = _currentLanguage;
            if (!login.userInstance) { // Do not send attachdbfilename or database to SSE primary instance
                login.database         = CurrentDatabase;;
                login.attachDBFilename = ConnectionOptions.AttachDBFilename; 
            }
            login.serverName       = ConnectionOptions.DataSource; 
            login.useReplication   = ConnectionOptions.Replication; 
            login.useSSPI          = ConnectionOptions.IntegratedSecurity;
            login.packetSize       = _currentPacketSize; 
            login.newPassword      = newPassword;

            _parser.TdsLogin(login);
        } 

        private void LoginFailure() { 
            Bid.Trace("<sc.SqlInternalConnectionTds.LoginFailure|RES|CPOOL> %d#\n", ObjectID); 

            // If the parser was allocated and we failed, then we must have failed on 
            // either the Connect or Login, either way we should call Disconnect.
            // Disconnect can be called if the connection is already closed - becomes
            // no-op, so no issues there.
            if (_parser != null) { 

                _parser.Disconnect(); 
            } 
            //
        } 

        private void OpenLoginEnlist(SqlConnection owningObject, SqlConnectionString connectionOptions, string newPassword, bool redirectedUserInstance) {
            long timerStart = ADP.TimerCurrent();
            bool useFailoverPartner; // should we use primary or secondary first 
            string dataSource = ConnectionOptions.DataSource;
            string failoverPartner; 
 
            if (null != PoolGroupProviderInfo) {
                useFailoverPartner = PoolGroupProviderInfo.UseFailoverPartner; 
                failoverPartner = PoolGroupProviderInfo.FailoverPartner;
            }
            else {
                // Only ChangePassword or SSE User Instance comes through this code path. 
                useFailoverPartner = false;
                failoverPartner = ConnectionOptions.FailoverPartner; 
            } 

            bool hasFailoverPartner = !ADP.IsEmpty(failoverPartner); 

            // Open the connection and Login
            try {
                if (hasFailoverPartner) { 
                    LoginWithFailover(
                                useFailoverPartner, 
                                dataSource, 
                                failoverPartner,
                                newPassword, 
                                redirectedUserInstance,
                                owningObject,
                                connectionOptions,
                                timerStart); 
                }
                else { 
                    LoginNoFailover(dataSource, newPassword, redirectedUserInstance, 
                            owningObject, connectionOptions, timerStart);
                } 
            }
            catch (Exception e) {
                //
                if (ADP.IsCatchableExceptionType(e)) { 
                    LoginFailure();
                } 
                throw; 
            }
#if DEBUG 
            _parser._physicalStateObj.InvalidateDebugOnlyCopyOfSniContext();
#endif
        }
 
    // Attempt to login to a host that does not have a failover partner
    // 
    //  Will repeatedly attempt to connect, but back off between each attempt so as not to clog the network. 
    //  Back off period increases for first few failures: 100ms, 200ms, 400ms, 800ms, then 1000ms for subsequent attempts
    // 
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    //  DEVNOTE: The logic in this method is paralleled by the logic in LoginWithFailover.
    //           Changes to either one should be examined to see if they need to be reflected in the other
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 
    private void LoginNoFailover(string host, string newPassword, bool redirectedUserInstance,
                SqlConnection owningObject, SqlConnectionString connectionOptions, long timerStart) { 
        if (Bid.AdvancedOn) { 
            Bid.Trace("<sc.SqlInternalConnectionTds.LoginNoFailover|ADV> %d#, host=%s\n", ObjectID, host);
        } 
        int  timeout    = ConnectionOptions.ConnectTimeout;
        long timerExpire;
        int  sleepInterval = 100;  //milliseconds to sleep (back off) between attempts.
 
        ServerInfo serverInfo = new ServerInfo(ConnectionOptions.NetworkLibrary, host);
        ResolveExtendedServerName(serverInfo, !redirectedUserInstance, owningObject); 
 
        // Timeout of 0 should map to maximum (MDAC 90672). Netlib doesn't do that, so we have to
        if (0 == timeout) { 
            timerExpire = Int64.MaxValue;
        }
        else {
            timerExpire = checked(timerStart + ADP.TimerFromSeconds(timeout)); 
        }
 
        // Only three ways out of this loop: 
        //  1) Successfully connected
        //  2) Parser threw exception while main timer was expired 
        //  3) Parser threw logon failure-related exception
        //  4) Parser threw exception in post-initial connect code,
        //      such as pre-login handshake or during actual logon. (parser state != Closed)
        // 
        //  Of these methods, only #1 exits normally. This preserves the call stack on the exception
        //  back into the parser for the error cases. 
        while(true) { 
            // Re-allocate parser each time to make sure state is known
            _parser = new TdsParser(ConnectionOptions.MARS, ConnectionOptions.Asynchronous); 
            Debug.Assert(SniContext.Undefined== Parser._physicalStateObj.SniContext, String.Format((IFormatProvider)null, "SniContext should be Undefined; actual Value: {0}", Parser._physicalStateObj.SniContext));

            try {
                // 

 
                AttemptOneLogin(    serverInfo, 
                                    newPassword,
                                    true,           // ignore timeout for SniOpen call 
                                    timerExpire,
                                    owningObject);
                break; // leave the while loop -- we've successfully connected
            } 
            catch (SqlException sqlex) {
                if (null == _parser 
                        || TdsParserState.Closed != _parser.State 
                        || (TdsEnums.LOGON_FAILED == sqlex.Number) // actual logon failed, i.e. bad password
                        || (TdsEnums.PASSWORD_EXPIRED == sqlex.Number) // actual logon failed, i.e. password isExpired 
                        || ADP.TimerHasExpired(timerExpire)) {       // no more time to try again
                    throw;  // Caller will call LoginFailure()
                }
 
                // Check sleep interval to make sure we won't exceed the timeout
                //  Do this in the catch block so we can re-throw the current exception 
                long remainingMilliseconds = ADP.TimerRemainingMilliseconds(timerExpire); 
                if (remainingMilliseconds <= sleepInterval) {
                    throw; 
                }

                //
            } 

            // We only get here when we failed to connect, but are going to re-try 
 
            // Switch to failover logic if the server provided a partner
            if (null != ServerProvidedFailOverPartner) { 
                LoginWithFailover(
                            true,   // start by using failover partner, since we already failed to connect to the primary
                            host,
                            ServerProvidedFailOverPartner, 
                            newPassword,
                            redirectedUserInstance, 
                            owningObject, 
                            connectionOptions,
                            timerStart); 
                return; // LoginWithFailover successfully connected and handled entire connection setup
            }

            // Sleep for a bit to prevent clogging the network with requests, 
            //  then update sleep interval for next iteration (max 1 second interval)
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnectionTds.LoginNoFailover|ADV> %d#, sleeping %d{milisec}\n", ObjectID, sleepInterval); 
            }
            Thread.Sleep(sleepInterval); 
            sleepInterval = (sleepInterval < 500) ? sleepInterval * 2 : 1000;
        }

        if (null != PoolGroupProviderInfo) { 
            // We must wait for CompleteLogin to finish for to have the
            // env change from the server to know its designated failover 
            // partner; save this information in _currentFailoverPartner. 
            PoolGroupProviderInfo.FailoverCheck(this, false, connectionOptions, ServerProvidedFailOverPartner);
        } 
        CurrentDataSource = host;
    }

    // Attempt to login to a host that has a failover partner 
    //
    // Connection & timeout sequence is 
    //      First target, timeout = interval * 1 
    //      second target, timeout = interval * 1
    //      sleep for 100ms 
    //      First target, timeout = interval * 2
    //      Second target, timeout = interval * 2
    //      sleep for 200ms
    //      First Target, timeout = interval * 3 
    //      etc.
    // 
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 
    //  DEVNOTE: The logic in this method is paralleled by the logic in LoginNoFailover.
    //           Changes to either one should be examined to see if they need to be reflected in the other 
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    private void LoginWithFailover(
            bool                useFailoverHost,
            string              primaryHost, 
            string              failoverHost,
            string              newPassword, 
            bool                redirectedUserInstance, 
            SqlConnection       owningObject,
            SqlConnectionString connectionOptions, 
            long                timerStart
        ) {
        if (Bid.AdvancedOn) {
            Bid.Trace("<sc.SqlInternalConnectionTds.LoginWithFailover|ADV> %d#, useFailover=%d{bool}, primary=", ObjectID, useFailoverHost); 
            Bid.PutStr(primaryHost);
            Bid.PutStr(", failover="); 
            Bid.PutStr(failoverHost); 
            Bid.PutStr("\n");
        } 
        int  timeout    = ConnectionOptions.ConnectTimeout;
        long timerExpire;
        int  sleepInterval = 100;  //milliseconds to sleep (back off) between attempts.
        long timeoutUnitInterval; 

        string     protocol = ConnectionOptions.NetworkLibrary; 
        ServerInfo primaryServerInfo = new ServerInfo(protocol, primaryHost); 
        ServerInfo failoverServerInfo = new ServerInfo(protocol, failoverHost);
 
        ResolveExtendedServerName(primaryServerInfo, !redirectedUserInstance, owningObject);
        if (null == ServerProvidedFailOverPartner) {// No point in resolving the failover partner when we're going to override it below
            // Don't resolve aliases if failover == primary //
            ResolveExtendedServerName(failoverServerInfo, !redirectedUserInstance && failoverHost != primaryHost, owningObject); 
        }
 
        // Timeout of 0 should map to maximum (MDAC 90672). Netlib doesn't do that, so we have to 
        if (0 == timeout) {
            timerExpire = Int64.MaxValue; 
            timeoutUnitInterval = checked((long) ADP.FailoverTimeoutStep * ADP.TimerFromSeconds(ADP.DefaultConnectionTimeout));
        }
        else {
            long timerTimeout = ADP.TimerFromSeconds(timeout);   // ConnectTimeout is in seconds, we need timer ticks 
            timerExpire = checked(timerStart + timerTimeout);
            timeoutUnitInterval = checked((long) (ADP.FailoverTimeoutStep * timerTimeout)); 
        } 

        // Initialize loop variables 
        bool failoverDemandDone = false; // have we demanded for partner information yet (as necessary)?
        long intervalExpire = checked(timerStart + timeoutUnitInterval);
        int attemptNumber = 0;
 
        // Only three ways out of this loop:
        //  1) Successfully connected 
        //  2) Parser threw exception while main timer was expired 
        //  3) Parser threw logon failure-related exception (LOGON_FAILED, PASSWORD_EXPIRED, etc)
        // 
        //  Of these methods, only #1 exits normally. This preserves the call stack on the exception
        //  back into the parser for the error cases.
        while (true) {
            // Re-allocate parser each time to make sure state is known 
            _parser = new TdsParser(ConnectionOptions.MARS, ConnectionOptions.Asynchronous);
            Debug.Assert(SniContext.Undefined== Parser._physicalStateObj.SniContext, String.Format((IFormatProvider)null, "SniContext should be Undefined; actual Value: {0}", Parser._physicalStateObj.SniContext)); 
 
            ServerInfo currentServerInfo;
            if (useFailoverHost) { 
                if (!failoverDemandDone) {
                    FailoverPermissionDemand();
                    failoverDemandDone = true;
                } 

                // Primary server may give us a different failover partner than the connection string indicates.  Update it 
                if (null != ServerProvidedFailOverPartner && failoverServerInfo.ResolvedServerName != ServerProvidedFailOverPartner) { 
                    if (Bid.AdvancedOn) {
                        Bid.Trace("<sc.SqlInternalConnectionTds.LoginWithFailover|ADV> %d#, new failover partner=%s\n", ObjectID, ServerProvidedFailOverPartner); 
                    }
                    failoverServerInfo.SetDerivedNames(protocol, ServerProvidedFailOverPartner);
                }
                currentServerInfo = failoverServerInfo; 
            }
            else { 
                currentServerInfo = primaryServerInfo; 
            }
 
            try {
                // Attempt login.  Use timerInterval for attempt timeout unless infinite timeout was requested.
                AttemptOneLogin(
                        currentServerInfo, 
                        newPassword,
                        false,          // Use timeout in SniOpen 
                        (0 == timeout) ? timerExpire : intervalExpire, 
                        owningObject);
                break; // leave the while loop -- we've successfully connected 
            }
            catch (SqlException sqlex) {
                if ((TdsEnums.LOGON_FAILED == sqlex.Number) // actual logon failed, i.e. bad password
                        || (TdsEnums.PASSWORD_EXPIRED == sqlex.Number) // actual logon failed, i.e. password isExpired 
                        || ADP.TimerHasExpired(timerExpire)) {       // no more time to try again
                    throw;  // Caller will call LoginFailure() 
                } 

                if (1 == attemptNumber % 2) { 
                    // Check sleep interval to make sure we won't exceed the timeout
                    //  Do this in the catch block so we can re-throw the current exception
                    long remainingMilliseconds = ADP.TimerRemainingMilliseconds(timerExpire);
                    if (remainingMilliseconds <= sleepInterval) { 
                        throw;
                    } 
                } 

                // 
            }

            // We only get here when we failed to connect, but are going to re-try
 
            // After trying to connect to both servers fails, sleep for a bit to prevent clogging
            //  the network with requests, then update sleep interval for next iteration (max 1 second interval) 
            if (1 == attemptNumber % 2) { 
                if (Bid.AdvancedOn) {
                    Bid.Trace("<sc.SqlInternalConnectionTds.LoginWithFailover|ADV> %d#, sleeping %d{milisec}\n", ObjectID, sleepInterval); 
                }
                Thread.Sleep(sleepInterval);
                sleepInterval = (sleepInterval < 500) ? sleepInterval * 2 : 1000;
            } 

            // Update timeout interval (but no more than the point where we're supposed to fail: timerExpire) 
            attemptNumber++; 
            intervalExpire = checked(ADP.TimerCurrent() + (timeoutUnitInterval * ((attemptNumber / 2) + 1)));
            if (intervalExpire > timerExpire) { 
                intervalExpire = timerExpire;
            }

            // try again, this time swapping primary/secondary servers 
            useFailoverHost = !useFailoverHost;
        } 
 
        // If we get here, connection/login succeeded!  Just a few more checks & record-keeping
 
        // if connected to failover host, but said host doesn't have DbMirroring set up, throw an error
        if (useFailoverHost && null == ServerProvidedFailOverPartner) {
            throw SQL.InvalidPartnerConfiguration(failoverHost, CurrentDatabase);
        } 

        if (null != PoolGroupProviderInfo) { 
            // We must wait for CompleteLogin to finish for to have the 
            // env change from the server to know its designated failover
            // partner; save this information in _currentFailoverPartner. 
            PoolGroupProviderInfo.FailoverCheck(this, useFailoverHost, connectionOptions, ServerProvidedFailOverPartner);
        }
        CurrentDataSource = (useFailoverHost ? failoverHost : primaryHost);
    } 

    private void ResolveExtendedServerName(ServerInfo serverInfo, bool aliasLookup, SqlConnection owningObject) { 
        if (serverInfo.ExtendedServerName == null) { 
            string host = serverInfo.UserServerName;
            string protocol = serverInfo.UserProtocol; 

            if (aliasLookup) { // We skip this for UserInstances...
                // Perform registry lookup to see if host is an alias.  It will appropriately set host and protocol, if an Alias.
                TdsParserStaticMethods.AliasRegistryLookup(ref host, ref protocol); 

                // 
                if ((null != owningObject) && ((SqlConnectionString)owningObject.UserConnectionOptions).EnforceLocalHost) { 
                    // verify LocalHost for |DataDirectory| usage
                    SqlConnectionString.VerifyLocalHostAndFixup(ref host, true, true /*fix-up to "."*/); 
                }
                // else if (null == owningObject) && EnforceLocalHost, then its a PoolCreateRequest and safe to create
            }
 
            serverInfo.SetDerivedNames(protocol, host);
        } 
    } 

    // Common code path for making one attempt to establish a connection and log in to server. 
    private void AttemptOneLogin(ServerInfo serverInfo, string newPassword, bool ignoreSniOpenTimeout,
                long timerExpire, SqlConnection owningObject) {
        if (Bid.AdvancedOn) {
            Bid.Trace("<sc.SqlInternalConnectionTds.AttemptOneLogin|ADV> %d#, timout=%d{ticks}, server=", ObjectID, timerExpire - ADP.TimerCurrent()); 
            Bid.PutStr(serverInfo.ExtendedServerName);
            Bid.Trace("\n"); 
        } 

        _parser._physicalStateObj.SniContext = SniContext.Snix_Connect; 

        _parser.Connect(serverInfo,
                        this,
                        ignoreSniOpenTimeout, 
                        timerExpire,
                        ConnectionOptions.Encrypt, 
                        ConnectionOptions.TrustServerCertificate, 
                        ConnectionOptions.IntegratedSecurity,
                        owningObject); 

        _parser._physicalStateObj.SniContext = SniContext.Snix_Login;
        this.Login(timerExpire, newPassword);
 
        CompleteLogin(!ConnectionOptions.Pooling);
    } 
 

    internal void FailoverPermissionDemand() { 
        if (null != PoolGroupProviderInfo) {
            PoolGroupProviderInfo.FailoverPermissionDemand();
        }
    } 

#if WINFSFunctionality 
        //////////////////////////////////////////////////////////////////////////////////////// 
        // UDT METHODS
        //////////////////////////////////////////////////////////////////////////////////////// 

        //helper to create the cache object.
        //called from the property accessor, as well as internal functions
        private AssemblyCache CreateAssemblyCache() { 
            AssemblyCache cache;
            Debug.Assert(ConnectionOptions != null, "ConnectionOptions"); 
            Debug.Assert(!ADP.IsEmpty(CurrentDataSource),  "CurrentDataSource"); 
            if (!_assemblyCacheTable.TryGetValue(CurrentDataSource, out cache)) {
                lock(_assemblyCacheLock) { 
                    if (!_assemblyCacheTable.TryGetValue(CurrentDataSource, out cache)) {
                        cache = new AssemblyCache();
                        _assemblyCacheTable.Add(CurrentDataSource,cache);
                    } 
                }
            } 
            Debug.Assert(cache != null, "Internal Error! AssemblyCache could not be created"); 
            return cache;
        } 
#endif

        ////////////////////////////////////////////////////////////////////////////////////////
        // PREPARED COMMAND METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        override internal void AddPreparedCommand(SqlCommand cmd) { 
            if (_preparedCommands == null)
                _preparedCommands = new List<WeakReference>(5); 

            for (int i = 0; i < _preparedCommands.Count; ++i) {
                if (!_preparedCommands[i].IsAlive) {    // reuse the dead weakreference
                    _preparedCommands[i].Target = cmd; 
                    return;
                } 
            } 
            _preparedCommands.Add(new WeakReference(cmd));
        } 

        override internal void ClearPreparedCommands() {
            //
            // be sure to unprepare all prepared commands 
            //
            if (null != _preparedCommands) { 
                // note that unpreparing a command will cause the command object to call RemovePreparedCommand 
                // on this connection.
                for (int i = 0; i < _preparedCommands.Count; ++i) { 
                    SqlCommand cmd = _preparedCommands[i].Target as SqlCommand;
                    if (null != cmd) {
                        cmd.Unprepare(true);
                        _preparedCommands[i].Target = null; 
                    }
                } 
 
                _preparedCommands = null;
            } 
        }

        override internal void RemovePreparedCommand(SqlCommand cmd) {
            if (_preparedCommands == null || _preparedCommands.Count == 0) 
                return;
 
            for (int i = 0; i < _preparedCommands.Count; i++) 
                if (_preparedCommands[i].Target == cmd) {
                    _preparedCommands[i].Target = null;    // don't shrink the list, just keep the reference for reuse 
                    break;
                }
        }
 
        ////////////////////////////////////////////////////////////////////////////////////////
        // PARSER CALLBACKS 
        //////////////////////////////////////////////////////////////////////////////////////// 

        internal void BreakConnection() { 
            Bid.Trace("<sc.SqlInternalConnectionTds.BreakConnection|RES|CPOOL> %d#, Breaking connection.\n", ObjectID);
            DoomThisConnection();   // Mark connection as unusable, so it will be destroyed
            if (null != Connection) {
                Connection.Close(); 
            }
        } 
 
        internal void OnEnvChange(SqlEnvChange rec) {
            switch (rec.type) { 
                case TdsEnums.ENV_DATABASE:
                    // If connection is not open, store the server value as the original.
                    if (!_fConnectionOpen)
                        _originalDatabase = rec.newValue; 

                    CurrentDatabase = rec.newValue; 
                    break; 

                case TdsEnums.ENV_LANG: 
                    // If connection is not open, store the server value as the original.
                    if (!_fConnectionOpen)
                        _originalLanguage = rec.newValue;
 
                    _currentLanguage = rec.newValue; //
                    break; 
 
                case TdsEnums.ENV_PACKETSIZE:
                    _currentPacketSize = Int32.Parse(rec.newValue, CultureInfo.InvariantCulture); 
                    break;

                case TdsEnums.ENV_CHARSET:
                case TdsEnums.ENV_LOCALEID: 
                case TdsEnums.ENV_COMPFLAGS:
                case TdsEnums.ENV_COLLATION: 
                case TdsEnums.ENV_BEGINTRAN: 
                case TdsEnums.ENV_COMMITTRAN:
                case TdsEnums.ENV_ROLLBACKTRAN: 
                case TdsEnums.ENV_ENLISTDTC:
                case TdsEnums.ENV_DEFECTDTC:
                    // only used on parser
                    break; 

                case TdsEnums.ENV_LOGSHIPNODE: 
                    _currentFailoverPartner = rec.newValue; 
                    break;
 
                case TdsEnums.ENV_PROMOTETRANSACTION:
                    PromotedDTCToken = rec.newBinValue;
                    break;
 
                case TdsEnums.ENV_TRANSACTIONENDED:
                    break; 
 
                case TdsEnums.ENV_TRANSACTIONMANAGERADDRESS:
                case TdsEnums.ENV_SPRESETCONNECTIONACK: 
                    // For now we skip these Yukon only env change notifications
                    break;

                case TdsEnums.ENV_USERINSTANCE: 
                    _instanceName = rec.newValue;
                    break; 
 
                default:
                    Debug.Assert(false, "Missed token in EnvChange!"); 
                    break;
            }
        }
 
        internal void OnLoginAck(SqlLoginAck rec) {
            _loginAck = rec; 
            // 
        }
    } 

    internal sealed class ServerInfo {
        private string _extendedServerName;     // the resolved servername with protocol
        private string _resolvedServerName;     // the resolved servername only 
        private string _userProtocol;           // the user specified protocol
        private string _userServerName;         // the user specified servername 
 
        internal ServerInfo (string userProtocol, string userServerName) {
            _userProtocol = userProtocol; 
            _userServerName = userServerName;
        }
        internal string ExtendedServerName {
            get { 
                return _extendedServerName;
            } 
            // setter will go away 
            set {
                _extendedServerName = value; 
            }
        }

        internal string ResolvedServerName { 
            get {
                return _resolvedServerName; 
            } 
            // setter will go away
            set { 
                _resolvedServerName = value;
            }
        }
 
        internal string UserProtocol {
            get { 
                return _userProtocol; 
            }
        } 

        internal string UserServerName {
            get {
                return _userServerName; 
            }
        } 
 
        internal void SetDerivedNames(string protocol, string serverName) {
            // The following concatenates the specified netlib network protocol to the host string, if netlib is not null 
            // and the flag is on.  This allows the user to specify the network protocol for the connection - but only
            // when using the Dbnetlib dll.  If the protocol is not specified, the netlib will
            // try all protocols in the order listed in the Client Network Utility.  Connect will
            // then fail if all protocols fail. 
            if (!ADP.IsEmpty(protocol)) {
                ExtendedServerName = protocol + ":" + serverName; 
            } 
            else {
                ExtendedServerName = serverName; 
            }
            ResolvedServerName = serverName;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlInternalConnectionTds.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient
{ 
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common; 
    using System.Data.ProviderBase;
    using System.Diagnostics; 
    using System.Globalization; 
    using System.Reflection;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions; 
    using System.Text;
    using System.Threading; 
    using SysTx = System.Transactions; 

    sealed internal class SqlInternalConnectionTds : SqlInternalConnection, IDisposable { 
        // CONNECTION AND STATE VARIABLES
        private readonly SqlConnectionPoolGroupProviderInfo _poolGroupProviderInfo; // will only be null when called for ChangePassword, or creating SSE User Instance
        private TdsParser                _parser;
        private SqlLoginAck              _loginAck; 

        // FOR POOLING 
        private bool                     _fConnectionOpen = false; 

        // FOR CONNECTION RESET MANAGEMENT 
        private bool                     _fResetConnection;
        private string                   _originalDatabase;
        private string                   _currentFailoverPartner;                     // only set by ENV change from server
        private string                   _originalLanguage; 
        private string                   _currentLanguage;
        private int                      _currentPacketSize; 
        private int                      _asyncCommandCount; // number of async Begins minus number of async Ends. 

        // FOR SSE 
        private string                   _instanceName = String.Empty;

        // FOR NOTIFICATIONS
        private DbConnectionPoolIdentity _identity; // Used to lookup info for notification matching Start(). 

        // OTHER STATE VARIABLES AND REFERENCES 
 
        // don't use a SqlCommands collection because this is an internal tracking list.  That is, we don't want
        // the command to "know" it's in a collection. 
        private List<WeakReference>      _preparedCommands; //

#if WINFSFunctionality
        //Hash table of UDT caches. Created in CreateAssemblyCache 
        private static Dictionary<string,AssemblyCache> _assemblyCacheTable = new Dictionary<string,AssemblyCache>();
        private static object _assemblyCacheLock = new object(); 
#endif 

        // although the new password is generally not used it must be passed to the c'tor 
        // the new Login7 packet will always write out the new password (or a length of zero and no bytes if not present)
        //
        internal SqlInternalConnectionTds(DbConnectionPoolIdentity identity, SqlConnectionString connectionOptions, object providerInfo, string newPassword, SqlConnection owningObject, bool redirectedUserInstance) : base(connectionOptions) {
#if DEBUG 
            try { // use this to help validate this object is only created after the following permission has been previously demanded in the current codepath
                if (null != owningObject) { 
                    owningObject.UserConnectionOptions.DemandPermission(); 
                }
                else { 
                    connectionOptions.DemandPermission();
                }
            }
            catch(System.Security.SecurityException) { 
                System.Diagnostics.Debug.Assert(false, "unexpected SecurityException for current codepath");
                throw; 
            } 
#endif
            if (connectionOptions.UserInstance && InOutOfProcHelper.InProc) { 
                throw SQL.UserInstanceNotAvailableInProc();
            }

            _identity = identity; 
            Debug.Assert(null!=newPassword, "newPassword argument must not be null");
            _poolGroupProviderInfo = (SqlConnectionPoolGroupProviderInfo)providerInfo; 
            _fResetConnection = connectionOptions.ConnectionReset; 
            if (_fResetConnection) {
                _originalDatabase = connectionOptions.InitialCatalog; 
                _originalLanguage = connectionOptions.CurrentLanguage;
            }

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                    OpenLoginEnlist(owningObject, connectionOptions, newPassword, redirectedUserInstance); 
#if DEBUG
                } 
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            }
            catch (System.OutOfMemoryException) {
                DoomThisConnection(); 
                throw;
            } 
            catch (System.StackOverflowException) { 
                DoomThisConnection();
                throw; 
            }
            catch (System.Threading.ThreadAbortException) {
                DoomThisConnection();
                throw; 
            }
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnectionTds.ctor|ADV> %d#, constructed new TDS internal connection\n", ObjectID); 
            }
        } 

#if WINFSFunctionality
        internal AssemblyCache AssemblyCache{
            get{ 
                return CreateAssemblyCache();
            } 
        } 
#endif
 
        override internal SqlInternalTransaction CurrentTransaction {
            get {
                return _parser.CurrentTransaction;
            } 
        }
 
        override internal SqlInternalTransaction AvailableInternalTransaction { 
            get {
                return _parser._fResetConnection ? null : CurrentTransaction; 
            }
        }

 
        override internal SqlInternalTransaction PendingTransaction {
            get { 
                return _parser.PendingTransaction; 
            }
        } 

        internal DbConnectionPoolIdentity Identity {
            get {
                return _identity; 
            }
        } 
 
        internal string InstanceName {
            get { 
                return _instanceName;
            }
        }
 
        override internal bool IsLockedForBulkCopy {
            get { 
                return (!Parser.MARSOn && Parser._physicalStateObj.BcpLock); 
            }
        } 

        override protected internal bool IsNonPoolableTransactionRoot {
            get {
                return IsTransactionRoot && (!IsKatmaiOrNewer || null == Pool); 
            }
        } 
 
        override internal bool IsShiloh {
            get { 
                return _loginAck.isVersion8;
            }
        }
 
        override internal bool IsYukonOrNewer {
            get { 
                return _parser.IsYukonOrNewer; 
            }
        } 

        override internal bool IsKatmaiOrNewer {
            get {
                return _parser.IsKatmaiOrNewer; 
            }
        } 
 
#if WINFSFunctionality
        override internal bool IsWinFS { 
            get {
                return _parser.IsWinFS;
            }
        } 
#endif
 
        internal int PacketSize { 
            get {
                return _currentPacketSize; 
            }
        }

        internal TdsParser Parser { 
            get {
                return _parser; 
            } 
        }
 
        internal string ServerProvidedFailOverPartner {
            get {
                return  _currentFailoverPartner;
            } 
        }
 
        internal SqlConnectionPoolGroupProviderInfo PoolGroupProviderInfo { 
            get {
                return _poolGroupProviderInfo; 
            }
        }

        override protected bool ReadyToPrepareTransaction { 
            get {
                // 
                bool result = (null == FindLiveReader(null)); // can't prepare with a live data reader... 
                return result;
            } 
        }

        override public string ServerVersion {
            get { 
                return(String.Format((IFormatProvider)null, "{0:00}.{1:00}.{2:0000}", _loginAck.majorVersion,
                       (short) _loginAck.minorVersion, _loginAck.buildNum)); 
            } 
        }
 

        ////////////////////////////////////////////////////////////////////////////////////////
        // GENERAL METHODS
        //////////////////////////////////////////////////////////////////////////////////////// 

        override protected void ChangeDatabaseInternal(string database) { 
            // MDAC 73598 - add brackets around database 
            database = SqlConnection.FixupDatabaseTransactionName(database);
            _parser.TdsExecuteSQLBatch("use " + database, ConnectionOptions.ConnectTimeout, null, _parser._physicalStateObj); 
            _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj);
        }

        override public void Dispose() { 
            if (Bid.AdvancedOn) {
                Bid.Trace("<sc.SqlInternalConnectionTds.Dispose|ADV> %d# disposing\n", base.ObjectID); 
            } 
            try {
                TdsParser parser = Interlocked.Exchange(ref _parser, null);  // guard against multiple concurrent dispose calls -- Delegated Transactions might cause this. 

                Debug.Assert(parser != null && _fConnectionOpen || parser == null && !_fConnectionOpen, "Unexpected state on dispose");
                if (null != parser) {
                    parser.Disconnect(); 
                }
            } 
            finally { // 
                // close will always close, even if exception is thrown
                // remember to null out any object references 
                _loginAck          = null;
                _fConnectionOpen   = false; // mark internal connection as closed
            }
            base.Dispose(); 
        }
 
        override internal void ValidateConnectionForExecute(SqlCommand command) { 
            SqlDataReader reader = null;
            if (Parser.MARSOn) { 
                if (null != command) { // command can't have datareader already associated with it
                    reader = FindLiveReader(command);
                }
            } 
            else { // single datareader per connection
                reader = FindLiveReader(null); 
            } 
            if (null != reader) {
                // if MARS is on, then a datareader associated with the command exists 
                // or if MARS is off, then a datareader exists
                throw ADP.OpenReaderExists(); // MDAC 66411
            }
            else if (!Parser.MARSOn && Parser._physicalStateObj._pendingData) { 
                Parser._physicalStateObj.CleanWire();
            } 
            Debug.Assert(!Parser._physicalStateObj._pendingData, "Should not have a busy physicalStateObject at this point!"); 

            Parser.RollbackOrphanedAPITransactions(); 
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // POOLING METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        override protected void Activate(SysTx.Transaction transaction) { 
            FailoverPermissionDemand(); // Demand for unspecified failover pooled connections
 
            // When we're required to automatically enlist in transactions and
            // there is one we enlist in it. On the other hand, if there isn't a
            // transaction and we are currently enlisted in one, then we
            // unenlist from it. 
            //
            // Regardless of whether we're required to automatically enlist, 
            // when there is not a current transaction, we cannot leave the 
            // connection enlisted in a transaction.
            if (null != transaction){ 
                if (ConnectionOptions.Enlist) {
                   Enlist(transaction);
                }
            } 
            else {
                Enlist(null); 
            } 
        }
 
        override protected void InternalDeactivate() {
            // When we're deactivated, the user must have called End on all
            // the async commands, or we don't know that we're in a state that
            // we can recover from.  We doom the connection in this case, to 
            // prevent odd cases when we go to the wire.
            if (0 != _asyncCommandCount) { 
                DoomThisConnection(); 
            }
 
            // If we're deactivating with a delegated transaction, we
            // should not be cleaning up the parser just yet, that will
            // cause our transaction to be rolled back and the connection
            // to be reset.  We'll get called again once the delegated 
            // transaction is completed and we can do it all then.
            if (!IsNonPoolableTransactionRoot) { 
                Debug.Assert(null != _parser, "Deactivating a disposed connection?"); 
                _parser.Deactivate(IsConnectionDoomed);
 
                if (!IsConnectionDoomed) {
                    ResetConnection();
                }
            } 
        }
 
        private void ResetConnection() { 
            // For implicit pooled connections, if connection reset behavior is specified,
            // reset the database and language properties back to default.  It is important 
            // to do this on activate so that the hashtable is correct before SqlConnection
            // obtains a clone.

            Debug.Assert(!HasLocalTransactionFromAPI, "Upon ResetConnection SqlInternalConnectionTds has a currently ongoing local transaction."); 
            Debug.Assert(!_parser._physicalStateObj._pendingData, "Upon ResetConnection SqlInternalConnectionTds has pending data.");
 
            if (_fResetConnection) { 
                // Ensure we are either going against shiloh, or we are not enlisted in a
                // distributed transaction - otherwise don't reset! 
                if (IsShiloh) {
                    // Prepare the parser for the connection reset - the next time a trip
                    // to the server is made.
                    _parser.PrepareResetConnection(IsTransactionRoot && !IsNonPoolableTransactionRoot); 
                }
                else if (!IsEnlistedInTransaction) { 
                    // If not Shiloh, we are going against Sphinx.  On Sphinx, we 
                    // may only reset if not enlisted in a distributed transaction.
                    try { 
                        // execute sp
                        _parser.TdsExecuteSQLBatch("sp_reset_connection", 30, null, _parser._physicalStateObj);
                        _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj);
                    } 
                    catch (Exception e) {
                        // 
                        if (!ADP.IsCatchableExceptionType(e)) { 
                            throw;
                        } 

                        DoomThisConnection();
                        ADP.TraceExceptionWithoutRethrow(e);
                    } 
                }
 
                // Reset hashtable values, since calling reset will not send us env_changes. 
                CurrentDatabase = _originalDatabase;
                _currentLanguage = _originalLanguage; 
            }
        }

        internal void DecrementAsyncCount() { 
            Interlocked.Decrement(ref _asyncCommandCount);
        } 
 
        internal void IncrementAsyncCount() {
            Interlocked.Increment(ref _asyncCommandCount); 
        }


        //////////////////////////////////////////////////////////////////////////////////////// 
        // LOCAL TRANSACTION METHODS
        //////////////////////////////////////////////////////////////////////////////////////// 
 
        override internal void DisconnectTransaction(SqlInternalTransaction internalTransaction) {
            TdsParser parser = Parser; 

            if (null != parser) {
                parser.DisconnectTransaction(internalTransaction);
            } 
        }
 
        internal void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso) { 
            ExecuteTransaction(transactionRequest, name, iso, null, false);
        } 

        override internal void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso, SqlInternalTransaction internalTransaction, bool isDelegateControlRequest) {
            if (IsConnectionDoomed) {  // doomed means we can't do anything else...
                if (transactionRequest == TransactionRequest.Rollback 
                 || transactionRequest == TransactionRequest.IfRollback) {
                    return; 
                } 
                throw SQL.ConnectionDoomed();
            } 

            if (transactionRequest == TransactionRequest.Commit
             || transactionRequest == TransactionRequest.Rollback
             || transactionRequest == TransactionRequest.IfRollback) { 
                if (!Parser.MARSOn && Parser._physicalStateObj.BcpLock) {
                    throw SQL.ConnectionLockedForBcpEvent(); 
                } 
            }
 
            string transactionName = (null == name) ? String.Empty : name;

            if (!_parser.IsYukonOrNewer) {
                ExecuteTransactionPreYukon(transactionRequest, transactionName, iso, internalTransaction); 
            }
            else { 
                ExecuteTransactionYukon(transactionRequest, transactionName, iso, internalTransaction, isDelegateControlRequest); 
            }
        } 

        internal void ExecuteTransactionPreYukon(TransactionRequest transactionRequest, string transactionName, IsolationLevel iso, SqlInternalTransaction internalTransaction) {
            StringBuilder sqlBatch = new StringBuilder();
 
            switch (iso) {
                case IsolationLevel.Unspecified: 
                    break; 
                case IsolationLevel.ReadCommitted:
                    sqlBatch.Append(TdsEnums.TRANS_READ_COMMITTED); 
                    sqlBatch.Append(";");
                    break;
                case IsolationLevel.ReadUncommitted:
                    sqlBatch.Append(TdsEnums.TRANS_READ_UNCOMMITTED); 
                    sqlBatch.Append(";");
                    break; 
                case IsolationLevel.RepeatableRead: 
                    sqlBatch.Append(TdsEnums.TRANS_REPEATABLE_READ);
                    sqlBatch.Append(";"); 
                    break;
                case IsolationLevel.Serializable:
                    sqlBatch.Append(TdsEnums.TRANS_SERIALIZABLE);
                    sqlBatch.Append(";"); 
                    break;
                case IsolationLevel.Snapshot: 
                    throw SQL.SnapshotNotSupported(IsolationLevel.Snapshot); 

                case IsolationLevel.Chaos: 
                    throw SQL.NotSupportedIsolationLevel(iso);

                default:
                    throw ADP.InvalidIsolationLevel(iso); 
            }
 
            if (!ADP.IsEmpty(transactionName)) { 
                transactionName = " " + SqlConnection.FixupDatabaseTransactionName(transactionName);
            } 

            switch (transactionRequest) {
                case TransactionRequest.Begin:
                    sqlBatch.Append(TdsEnums.TRANS_BEGIN); 
                    sqlBatch.Append(transactionName);
                    break; 
                case TransactionRequest.Promote: 
                    Debug.Assert(false, "Promote called with transaction name or on pre-Yukon!");
                    break; 
                case TransactionRequest.Commit:
                    sqlBatch.Append(TdsEnums.TRANS_COMMIT);
                    sqlBatch.Append(transactionName);
                    break; 
                case TransactionRequest.Rollback:
                    sqlBatch.Append(TdsEnums.TRANS_ROLLBACK); 
                    sqlBatch.Append(transactionName); 
                    break;
                case TransactionRequest.IfRollback: 
                    sqlBatch.Append(TdsEnums.TRANS_IF_ROLLBACK);
                    sqlBatch.Append(transactionName);
                    break;
                case TransactionRequest.Save: 
                    sqlBatch.Append(TdsEnums.TRANS_SAVE);
                    sqlBatch.Append(transactionName); 
                    break; 
                default:
                    Debug.Assert(false, "Unknown transaction type"); 
                    break;
            }

            _parser.TdsExecuteSQLBatch(sqlBatch.ToString(), ConnectionOptions.ConnectTimeout, null, _parser._physicalStateObj); 
            _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj);
 
            // Prior to Yukon, we didn't have any transaction tokens to manage, 
            // or any feedback to know when one was created, so we just presume
            // that successful execution of the request caused the transaction 
            // to be created, and we set that on the parser.
            if (TransactionRequest.Begin == transactionRequest) {
                Debug.Assert(null != internalTransaction, "Begin Transaction request without internal transaction");
                _parser.CurrentTransaction = internalTransaction; 
            }
        } 
 
        internal void ExecuteTransactionYukon(TransactionRequest transactionRequest, string transactionName, IsolationLevel iso, SqlInternalTransaction internalTransaction, bool isDelegateControlRequest) {
 
            TdsEnums.TransactionManagerRequestType    requestType = TdsEnums.TransactionManagerRequestType.Begin;
            TdsEnums.TransactionManagerIsolationLevel isoLevel    = TdsEnums.TransactionManagerIsolationLevel.ReadCommitted;

            switch (iso) { 
                case IsolationLevel.Unspecified:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Unspecified; 
                    break; 
                case IsolationLevel.ReadCommitted:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.ReadCommitted; 
                    break;
                case IsolationLevel.ReadUncommitted:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.ReadUncommitted;
                    break; 
                case IsolationLevel.RepeatableRead:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.RepeatableRead; 
                    break; 
                case IsolationLevel.Serializable:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Serializable; 
                    break;
                case IsolationLevel.Snapshot:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Snapshot;
                    break; 
                case IsolationLevel.Chaos:
                    throw SQL.NotSupportedIsolationLevel(iso); 
                default: 
                    throw ADP.InvalidIsolationLevel(iso);
            } 

            TdsParserStateObject stateObj = _parser._physicalStateObj;
            TdsParser parser = _parser;
            bool mustPutSession = false; 
            bool mustRelease = false;
            try { 
                switch (transactionRequest) { 
                    case TransactionRequest.Begin:
                        requestType = TdsEnums.TransactionManagerRequestType.Begin; 
                        break;
                    case TransactionRequest.Promote:
                        requestType = TdsEnums.TransactionManagerRequestType.Promote;
                        break; 
                    case TransactionRequest.Commit:
                        requestType = TdsEnums.TransactionManagerRequestType.Commit; 
                        break; 
                    case TransactionRequest.IfRollback:
                        // Map IfRollback to Rollback since with Yukon and beyond we should never need 
                        // the if since the server will inform us when transactions have completed
                        // as a result of an error on the server.
                    case TransactionRequest.Rollback:
                        requestType = TdsEnums.TransactionManagerRequestType.Rollback; 
                        break;
                    case TransactionRequest.Save: 
                        requestType = TdsEnums.TransactionManagerRequestType.Save; 
                        break;
                    default: 
                        Debug.Assert(false, "Unknown transaction type");
                        break;
                }
 

                // 
 

 



 

 
 

 



                if (null != internalTransaction && internalTransaction.IsDelegated) { 
                    if (_parser.MARSOn) {
                        stateObj = _parser.GetSession(this); 
                        mustPutSession = true; 
                    }
                    else if (internalTransaction.OpenResultsCount == 0) { 
                        Monitor.Enter(stateObj);
                        mustRelease = true;

                        if (internalTransaction.OpenResultsCount != 0) { 
                            throw SQL.CannotCompleteDelegatedTransactionWithOpenResults();
                        } 
                    } 
                    else {
                        throw SQL.CannotCompleteDelegatedTransactionWithOpenResults(); 
                    }
                }

                // 

                _parser.TdsExecuteTransactionManagerRequest(null, requestType, transactionName, isoLevel, 
                    ConnectionOptions.ConnectTimeout, internalTransaction, stateObj, isDelegateControlRequest); 
            }
            finally { 
                if (mustPutSession) {
                    parser.PutSession(stateObj);
                }
                if (mustRelease) { 
                    Monitor.Exit(stateObj);
                } 
            } 
        }
 
        ////////////////////////////////////////////////////////////////////////////////////////
        // DISTRIBUTED TRANSACTION METHODS
        ////////////////////////////////////////////////////////////////////////////////////////
 
        override internal void DelegatedTransactionEnded() {
            // 
            base.DelegatedTransactionEnded(); 
        }
 
        override protected byte[] GetDTCAddress() {
            byte[] dtcAddress = _parser.GetDTCAddress(ConnectionOptions.ConnectTimeout, _parser._physicalStateObj);
            Debug.Assert(null != dtcAddress, "null dtcAddress?");
            return dtcAddress; 
        }
 
        override protected void PropagateTransactionCookie(byte[] cookie) { 
            _parser.PropagateDistributedTransaction(cookie, ConnectionOptions.ConnectTimeout, _parser._physicalStateObj);
        } 

        ////////////////////////////////////////////////////////////////////////////////////////
        // LOGIN-RELATED METHODS
        //////////////////////////////////////////////////////////////////////////////////////// 

        private void CompleteLogin(bool enlistOK) { 
            _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj); 

            Debug.Assert(SniContext.Snix_Login == Parser._physicalStateObj.SniContext, String.Format((IFormatProvider)null, "SniContext should be Snix_Login; actual Value: {0}", Parser._physicalStateObj.SniContext)); 
            _parser._physicalStateObj.SniContext = SniContext.Snix_EnableMars;
            _parser.EnableMars(ConnectionOptions.DataSource);

            _fConnectionOpen = true; // mark connection as open 

            // for non-pooled connections, enlist in a distributed transaction 
            // if present - and user specified to enlist 
            if(enlistOK && ConnectionOptions.Enlist) {
                _parser._physicalStateObj.SniContext = SniContext.Snix_AutoEnlist; 
                SysTx.Transaction tx = ADP.GetCurrentTransaction();
                Enlist(tx);
            }
            _parser._physicalStateObj.SniContext=SniContext.Snix_Login; 
        }
 
        private void Login(long timerExpire, string newPassword) { 
            // create a new login record
            SqlLogin login = new SqlLogin(); 

            // gather all the settings the user set in the connection string or
            // properties and do the login
            CurrentDatabase   = ConnectionOptions.InitialCatalog; 
            _currentPacketSize = ConnectionOptions.PacketSize;
            _currentLanguage   = ConnectionOptions.CurrentLanguage; 
 
            int timeout = 0;
 
            // If a timeout tick value is specified, compute the timeout based
            // upon the amount of time left.
            if (Int64.MaxValue != timerExpire) {
                long t = ADP.TimerRemainingSeconds(timerExpire); 

                if ((long)Int32.MaxValue > t) { 
                    timeout = (int)t; 
                }
            } 

            login.timeout          = timeout;

            login.userInstance     = ConnectionOptions.UserInstance; 
            login.hostName         = ConnectionOptions.ObtainWorkstationId();
            login.userName         = ConnectionOptions.UserID; 
            login.password         = ConnectionOptions.Password; 
            login.applicationName  = ConnectionOptions.ApplicationName;
 
            login.language         = _currentLanguage;
            if (!login.userInstance) { // Do not send attachdbfilename or database to SSE primary instance
                login.database         = CurrentDatabase;;
                login.attachDBFilename = ConnectionOptions.AttachDBFilename; 
            }
            login.serverName       = ConnectionOptions.DataSource; 
            login.useReplication   = ConnectionOptions.Replication; 
            login.useSSPI          = ConnectionOptions.IntegratedSecurity;
            login.packetSize       = _currentPacketSize; 
            login.newPassword      = newPassword;

            _parser.TdsLogin(login);
        } 

        private void LoginFailure() { 
            Bid.Trace("<sc.SqlInternalConnectionTds.LoginFailure|RES|CPOOL> %d#\n", ObjectID); 

            // If the parser was allocated and we failed, then we must have failed on 
            // either the Connect or Login, either way we should call Disconnect.
            // Disconnect can be called if the connection is already closed - becomes
            // no-op, so no issues there.
            if (_parser != null) { 

                _parser.Disconnect(); 
            } 
            //
        } 

        private void OpenLoginEnlist(SqlConnection owningObject, SqlConnectionString connectionOptions, string newPassword, bool redirectedUserInstance) {
            long timerStart = ADP.TimerCurrent();
            bool useFailoverPartner; // should we use primary or secondary first 
            string dataSource = ConnectionOptions.DataSource;
            string failoverPartner; 
 
            if (null != PoolGroupProviderInfo) {
                useFailoverPartner = PoolGroupProviderInfo.UseFailoverPartner; 
                failoverPartner = PoolGroupProviderInfo.FailoverPartner;
            }
            else {
                // Only ChangePassword or SSE User Instance comes through this code path. 
                useFailoverPartner = false;
                failoverPartner = ConnectionOptions.FailoverPartner; 
            } 

            bool hasFailoverPartner = !ADP.IsEmpty(failoverPartner); 

            // Open the connection and Login
            try {
                if (hasFailoverPartner) { 
                    LoginWithFailover(
                                useFailoverPartner, 
                                dataSource, 
                                failoverPartner,
                                newPassword, 
                                redirectedUserInstance,
                                owningObject,
                                connectionOptions,
                                timerStart); 
                }
                else { 
                    LoginNoFailover(dataSource, newPassword, redirectedUserInstance, 
                            owningObject, connectionOptions, timerStart);
                } 
            }
            catch (Exception e) {
                //
                if (ADP.IsCatchableExceptionType(e)) { 
                    LoginFailure();
                } 
                throw; 
            }
#if DEBUG 
            _parser._physicalStateObj.InvalidateDebugOnlyCopyOfSniContext();
#endif
        }
 
    // Attempt to login to a host that does not have a failover partner
    // 
    //  Will repeatedly attempt to connect, but back off between each attempt so as not to clog the network. 
    //  Back off period increases for first few failures: 100ms, 200ms, 400ms, 800ms, then 1000ms for subsequent attempts
    // 
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    //  DEVNOTE: The logic in this method is paralleled by the logic in LoginWithFailover.
    //           Changes to either one should be examined to see if they need to be reflected in the other
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 
    private void LoginNoFailover(string host, string newPassword, bool redirectedUserInstance,
                SqlConnection owningObject, SqlConnectionString connectionOptions, long timerStart) { 
        if (Bid.AdvancedOn) { 
            Bid.Trace("<sc.SqlInternalConnectionTds.LoginNoFailover|ADV> %d#, host=%s\n", ObjectID, host);
        } 
        int  timeout    = ConnectionOptions.ConnectTimeout;
        long timerExpire;
        int  sleepInterval = 100;  //milliseconds to sleep (back off) between attempts.
 
        ServerInfo serverInfo = new ServerInfo(ConnectionOptions.NetworkLibrary, host);
        ResolveExtendedServerName(serverInfo, !redirectedUserInstance, owningObject); 
 
        // Timeout of 0 should map to maximum (MDAC 90672). Netlib doesn't do that, so we have to
        if (0 == timeout) { 
            timerExpire = Int64.MaxValue;
        }
        else {
            timerExpire = checked(timerStart + ADP.TimerFromSeconds(timeout)); 
        }
 
        // Only three ways out of this loop: 
        //  1) Successfully connected
        //  2) Parser threw exception while main timer was expired 
        //  3) Parser threw logon failure-related exception
        //  4) Parser threw exception in post-initial connect code,
        //      such as pre-login handshake or during actual logon. (parser state != Closed)
        // 
        //  Of these methods, only #1 exits normally. This preserves the call stack on the exception
        //  back into the parser for the error cases. 
        while(true) { 
            // Re-allocate parser each time to make sure state is known
            _parser = new TdsParser(ConnectionOptions.MARS, ConnectionOptions.Asynchronous); 
            Debug.Assert(SniContext.Undefined== Parser._physicalStateObj.SniContext, String.Format((IFormatProvider)null, "SniContext should be Undefined; actual Value: {0}", Parser._physicalStateObj.SniContext));

            try {
                // 

 
                AttemptOneLogin(    serverInfo, 
                                    newPassword,
                                    true,           // ignore timeout for SniOpen call 
                                    timerExpire,
                                    owningObject);
                break; // leave the while loop -- we've successfully connected
            } 
            catch (SqlException sqlex) {
                if (null == _parser 
                        || TdsParserState.Closed != _parser.State 
                        || (TdsEnums.LOGON_FAILED == sqlex.Number) // actual logon failed, i.e. bad password
                        || (TdsEnums.PASSWORD_EXPIRED == sqlex.Number) // actual logon failed, i.e. password isExpired 
                        || ADP.TimerHasExpired(timerExpire)) {       // no more time to try again
                    throw;  // Caller will call LoginFailure()
                }
 
                // Check sleep interval to make sure we won't exceed the timeout
                //  Do this in the catch block so we can re-throw the current exception 
                long remainingMilliseconds = ADP.TimerRemainingMilliseconds(timerExpire); 
                if (remainingMilliseconds <= sleepInterval) {
                    throw; 
                }

                //
            } 

            // We only get here when we failed to connect, but are going to re-try 
 
            // Switch to failover logic if the server provided a partner
            if (null != ServerProvidedFailOverPartner) { 
                LoginWithFailover(
                            true,   // start by using failover partner, since we already failed to connect to the primary
                            host,
                            ServerProvidedFailOverPartner, 
                            newPassword,
                            redirectedUserInstance, 
                            owningObject, 
                            connectionOptions,
                            timerStart); 
                return; // LoginWithFailover successfully connected and handled entire connection setup
            }

            // Sleep for a bit to prevent clogging the network with requests, 
            //  then update sleep interval for next iteration (max 1 second interval)
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnectionTds.LoginNoFailover|ADV> %d#, sleeping %d{milisec}\n", ObjectID, sleepInterval); 
            }
            Thread.Sleep(sleepInterval); 
            sleepInterval = (sleepInterval < 500) ? sleepInterval * 2 : 1000;
        }

        if (null != PoolGroupProviderInfo) { 
            // We must wait for CompleteLogin to finish for to have the
            // env change from the server to know its designated failover 
            // partner; save this information in _currentFailoverPartner. 
            PoolGroupProviderInfo.FailoverCheck(this, false, connectionOptions, ServerProvidedFailOverPartner);
        } 
        CurrentDataSource = host;
    }

    // Attempt to login to a host that has a failover partner 
    //
    // Connection & timeout sequence is 
    //      First target, timeout = interval * 1 
    //      second target, timeout = interval * 1
    //      sleep for 100ms 
    //      First target, timeout = interval * 2
    //      Second target, timeout = interval * 2
    //      sleep for 200ms
    //      First Target, timeout = interval * 3 
    //      etc.
    // 
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 
    //  DEVNOTE: The logic in this method is paralleled by the logic in LoginNoFailover.
    //           Changes to either one should be examined to see if they need to be reflected in the other 
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    private void LoginWithFailover(
            bool                useFailoverHost,
            string              primaryHost, 
            string              failoverHost,
            string              newPassword, 
            bool                redirectedUserInstance, 
            SqlConnection       owningObject,
            SqlConnectionString connectionOptions, 
            long                timerStart
        ) {
        if (Bid.AdvancedOn) {
            Bid.Trace("<sc.SqlInternalConnectionTds.LoginWithFailover|ADV> %d#, useFailover=%d{bool}, primary=", ObjectID, useFailoverHost); 
            Bid.PutStr(primaryHost);
            Bid.PutStr(", failover="); 
            Bid.PutStr(failoverHost); 
            Bid.PutStr("\n");
        } 
        int  timeout    = ConnectionOptions.ConnectTimeout;
        long timerExpire;
        int  sleepInterval = 100;  //milliseconds to sleep (back off) between attempts.
        long timeoutUnitInterval; 

        string     protocol = ConnectionOptions.NetworkLibrary; 
        ServerInfo primaryServerInfo = new ServerInfo(protocol, primaryHost); 
        ServerInfo failoverServerInfo = new ServerInfo(protocol, failoverHost);
 
        ResolveExtendedServerName(primaryServerInfo, !redirectedUserInstance, owningObject);
        if (null == ServerProvidedFailOverPartner) {// No point in resolving the failover partner when we're going to override it below
            // Don't resolve aliases if failover == primary //
            ResolveExtendedServerName(failoverServerInfo, !redirectedUserInstance && failoverHost != primaryHost, owningObject); 
        }
 
        // Timeout of 0 should map to maximum (MDAC 90672). Netlib doesn't do that, so we have to 
        if (0 == timeout) {
            timerExpire = Int64.MaxValue; 
            timeoutUnitInterval = checked((long) ADP.FailoverTimeoutStep * ADP.TimerFromSeconds(ADP.DefaultConnectionTimeout));
        }
        else {
            long timerTimeout = ADP.TimerFromSeconds(timeout);   // ConnectTimeout is in seconds, we need timer ticks 
            timerExpire = checked(timerStart + timerTimeout);
            timeoutUnitInterval = checked((long) (ADP.FailoverTimeoutStep * timerTimeout)); 
        } 

        // Initialize loop variables 
        bool failoverDemandDone = false; // have we demanded for partner information yet (as necessary)?
        long intervalExpire = checked(timerStart + timeoutUnitInterval);
        int attemptNumber = 0;
 
        // Only three ways out of this loop:
        //  1) Successfully connected 
        //  2) Parser threw exception while main timer was expired 
        //  3) Parser threw logon failure-related exception (LOGON_FAILED, PASSWORD_EXPIRED, etc)
        // 
        //  Of these methods, only #1 exits normally. This preserves the call stack on the exception
        //  back into the parser for the error cases.
        while (true) {
            // Re-allocate parser each time to make sure state is known 
            _parser = new TdsParser(ConnectionOptions.MARS, ConnectionOptions.Asynchronous);
            Debug.Assert(SniContext.Undefined== Parser._physicalStateObj.SniContext, String.Format((IFormatProvider)null, "SniContext should be Undefined; actual Value: {0}", Parser._physicalStateObj.SniContext)); 
 
            ServerInfo currentServerInfo;
            if (useFailoverHost) { 
                if (!failoverDemandDone) {
                    FailoverPermissionDemand();
                    failoverDemandDone = true;
                } 

                // Primary server may give us a different failover partner than the connection string indicates.  Update it 
                if (null != ServerProvidedFailOverPartner && failoverServerInfo.ResolvedServerName != ServerProvidedFailOverPartner) { 
                    if (Bid.AdvancedOn) {
                        Bid.Trace("<sc.SqlInternalConnectionTds.LoginWithFailover|ADV> %d#, new failover partner=%s\n", ObjectID, ServerProvidedFailOverPartner); 
                    }
                    failoverServerInfo.SetDerivedNames(protocol, ServerProvidedFailOverPartner);
                }
                currentServerInfo = failoverServerInfo; 
            }
            else { 
                currentServerInfo = primaryServerInfo; 
            }
 
            try {
                // Attempt login.  Use timerInterval for attempt timeout unless infinite timeout was requested.
                AttemptOneLogin(
                        currentServerInfo, 
                        newPassword,
                        false,          // Use timeout in SniOpen 
                        (0 == timeout) ? timerExpire : intervalExpire, 
                        owningObject);
                break; // leave the while loop -- we've successfully connected 
            }
            catch (SqlException sqlex) {
                if ((TdsEnums.LOGON_FAILED == sqlex.Number) // actual logon failed, i.e. bad password
                        || (TdsEnums.PASSWORD_EXPIRED == sqlex.Number) // actual logon failed, i.e. password isExpired 
                        || ADP.TimerHasExpired(timerExpire)) {       // no more time to try again
                    throw;  // Caller will call LoginFailure() 
                } 

                if (1 == attemptNumber % 2) { 
                    // Check sleep interval to make sure we won't exceed the timeout
                    //  Do this in the catch block so we can re-throw the current exception
                    long remainingMilliseconds = ADP.TimerRemainingMilliseconds(timerExpire);
                    if (remainingMilliseconds <= sleepInterval) { 
                        throw;
                    } 
                } 

                // 
            }

            // We only get here when we failed to connect, but are going to re-try
 
            // After trying to connect to both servers fails, sleep for a bit to prevent clogging
            //  the network with requests, then update sleep interval for next iteration (max 1 second interval) 
            if (1 == attemptNumber % 2) { 
                if (Bid.AdvancedOn) {
                    Bid.Trace("<sc.SqlInternalConnectionTds.LoginWithFailover|ADV> %d#, sleeping %d{milisec}\n", ObjectID, sleepInterval); 
                }
                Thread.Sleep(sleepInterval);
                sleepInterval = (sleepInterval < 500) ? sleepInterval * 2 : 1000;
            } 

            // Update timeout interval (but no more than the point where we're supposed to fail: timerExpire) 
            attemptNumber++; 
            intervalExpire = checked(ADP.TimerCurrent() + (timeoutUnitInterval * ((attemptNumber / 2) + 1)));
            if (intervalExpire > timerExpire) { 
                intervalExpire = timerExpire;
            }

            // try again, this time swapping primary/secondary servers 
            useFailoverHost = !useFailoverHost;
        } 
 
        // If we get here, connection/login succeeded!  Just a few more checks & record-keeping
 
        // if connected to failover host, but said host doesn't have DbMirroring set up, throw an error
        if (useFailoverHost && null == ServerProvidedFailOverPartner) {
            throw SQL.InvalidPartnerConfiguration(failoverHost, CurrentDatabase);
        } 

        if (null != PoolGroupProviderInfo) { 
            // We must wait for CompleteLogin to finish for to have the 
            // env change from the server to know its designated failover
            // partner; save this information in _currentFailoverPartner. 
            PoolGroupProviderInfo.FailoverCheck(this, useFailoverHost, connectionOptions, ServerProvidedFailOverPartner);
        }
        CurrentDataSource = (useFailoverHost ? failoverHost : primaryHost);
    } 

    private void ResolveExtendedServerName(ServerInfo serverInfo, bool aliasLookup, SqlConnection owningObject) { 
        if (serverInfo.ExtendedServerName == null) { 
            string host = serverInfo.UserServerName;
            string protocol = serverInfo.UserProtocol; 

            if (aliasLookup) { // We skip this for UserInstances...
                // Perform registry lookup to see if host is an alias.  It will appropriately set host and protocol, if an Alias.
                TdsParserStaticMethods.AliasRegistryLookup(ref host, ref protocol); 

                // 
                if ((null != owningObject) && ((SqlConnectionString)owningObject.UserConnectionOptions).EnforceLocalHost) { 
                    // verify LocalHost for |DataDirectory| usage
                    SqlConnectionString.VerifyLocalHostAndFixup(ref host, true, true /*fix-up to "."*/); 
                }
                // else if (null == owningObject) && EnforceLocalHost, then its a PoolCreateRequest and safe to create
            }
 
            serverInfo.SetDerivedNames(protocol, host);
        } 
    } 

    // Common code path for making one attempt to establish a connection and log in to server. 
    private void AttemptOneLogin(ServerInfo serverInfo, string newPassword, bool ignoreSniOpenTimeout,
                long timerExpire, SqlConnection owningObject) {
        if (Bid.AdvancedOn) {
            Bid.Trace("<sc.SqlInternalConnectionTds.AttemptOneLogin|ADV> %d#, timout=%d{ticks}, server=", ObjectID, timerExpire - ADP.TimerCurrent()); 
            Bid.PutStr(serverInfo.ExtendedServerName);
            Bid.Trace("\n"); 
        } 

        _parser._physicalStateObj.SniContext = SniContext.Snix_Connect; 

        _parser.Connect(serverInfo,
                        this,
                        ignoreSniOpenTimeout, 
                        timerExpire,
                        ConnectionOptions.Encrypt, 
                        ConnectionOptions.TrustServerCertificate, 
                        ConnectionOptions.IntegratedSecurity,
                        owningObject); 

        _parser._physicalStateObj.SniContext = SniContext.Snix_Login;
        this.Login(timerExpire, newPassword);
 
        CompleteLogin(!ConnectionOptions.Pooling);
    } 
 

    internal void FailoverPermissionDemand() { 
        if (null != PoolGroupProviderInfo) {
            PoolGroupProviderInfo.FailoverPermissionDemand();
        }
    } 

#if WINFSFunctionality 
        //////////////////////////////////////////////////////////////////////////////////////// 
        // UDT METHODS
        //////////////////////////////////////////////////////////////////////////////////////// 

        //helper to create the cache object.
        //called from the property accessor, as well as internal functions
        private AssemblyCache CreateAssemblyCache() { 
            AssemblyCache cache;
            Debug.Assert(ConnectionOptions != null, "ConnectionOptions"); 
            Debug.Assert(!ADP.IsEmpty(CurrentDataSource),  "CurrentDataSource"); 
            if (!_assemblyCacheTable.TryGetValue(CurrentDataSource, out cache)) {
                lock(_assemblyCacheLock) { 
                    if (!_assemblyCacheTable.TryGetValue(CurrentDataSource, out cache)) {
                        cache = new AssemblyCache();
                        _assemblyCacheTable.Add(CurrentDataSource,cache);
                    } 
                }
            } 
            Debug.Assert(cache != null, "Internal Error! AssemblyCache could not be created"); 
            return cache;
        } 
#endif

        ////////////////////////////////////////////////////////////////////////////////////////
        // PREPARED COMMAND METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        override internal void AddPreparedCommand(SqlCommand cmd) { 
            if (_preparedCommands == null)
                _preparedCommands = new List<WeakReference>(5); 

            for (int i = 0; i < _preparedCommands.Count; ++i) {
                if (!_preparedCommands[i].IsAlive) {    // reuse the dead weakreference
                    _preparedCommands[i].Target = cmd; 
                    return;
                } 
            } 
            _preparedCommands.Add(new WeakReference(cmd));
        } 

        override internal void ClearPreparedCommands() {
            //
            // be sure to unprepare all prepared commands 
            //
            if (null != _preparedCommands) { 
                // note that unpreparing a command will cause the command object to call RemovePreparedCommand 
                // on this connection.
                for (int i = 0; i < _preparedCommands.Count; ++i) { 
                    SqlCommand cmd = _preparedCommands[i].Target as SqlCommand;
                    if (null != cmd) {
                        cmd.Unprepare(true);
                        _preparedCommands[i].Target = null; 
                    }
                } 
 
                _preparedCommands = null;
            } 
        }

        override internal void RemovePreparedCommand(SqlCommand cmd) {
            if (_preparedCommands == null || _preparedCommands.Count == 0) 
                return;
 
            for (int i = 0; i < _preparedCommands.Count; i++) 
                if (_preparedCommands[i].Target == cmd) {
                    _preparedCommands[i].Target = null;    // don't shrink the list, just keep the reference for reuse 
                    break;
                }
        }
 
        ////////////////////////////////////////////////////////////////////////////////////////
        // PARSER CALLBACKS 
        //////////////////////////////////////////////////////////////////////////////////////// 

        internal void BreakConnection() { 
            Bid.Trace("<sc.SqlInternalConnectionTds.BreakConnection|RES|CPOOL> %d#, Breaking connection.\n", ObjectID);
            DoomThisConnection();   // Mark connection as unusable, so it will be destroyed
            if (null != Connection) {
                Connection.Close(); 
            }
        } 
 
        internal void OnEnvChange(SqlEnvChange rec) {
            switch (rec.type) { 
                case TdsEnums.ENV_DATABASE:
                    // If connection is not open, store the server value as the original.
                    if (!_fConnectionOpen)
                        _originalDatabase = rec.newValue; 

                    CurrentDatabase = rec.newValue; 
                    break; 

                case TdsEnums.ENV_LANG: 
                    // If connection is not open, store the server value as the original.
                    if (!_fConnectionOpen)
                        _originalLanguage = rec.newValue;
 
                    _currentLanguage = rec.newValue; //
                    break; 
 
                case TdsEnums.ENV_PACKETSIZE:
                    _currentPacketSize = Int32.Parse(rec.newValue, CultureInfo.InvariantCulture); 
                    break;

                case TdsEnums.ENV_CHARSET:
                case TdsEnums.ENV_LOCALEID: 
                case TdsEnums.ENV_COMPFLAGS:
                case TdsEnums.ENV_COLLATION: 
                case TdsEnums.ENV_BEGINTRAN: 
                case TdsEnums.ENV_COMMITTRAN:
                case TdsEnums.ENV_ROLLBACKTRAN: 
                case TdsEnums.ENV_ENLISTDTC:
                case TdsEnums.ENV_DEFECTDTC:
                    // only used on parser
                    break; 

                case TdsEnums.ENV_LOGSHIPNODE: 
                    _currentFailoverPartner = rec.newValue; 
                    break;
 
                case TdsEnums.ENV_PROMOTETRANSACTION:
                    PromotedDTCToken = rec.newBinValue;
                    break;
 
                case TdsEnums.ENV_TRANSACTIONENDED:
                    break; 
 
                case TdsEnums.ENV_TRANSACTIONMANAGERADDRESS:
                case TdsEnums.ENV_SPRESETCONNECTIONACK: 
                    // For now we skip these Yukon only env change notifications
                    break;

                case TdsEnums.ENV_USERINSTANCE: 
                    _instanceName = rec.newValue;
                    break; 
 
                default:
                    Debug.Assert(false, "Missed token in EnvChange!"); 
                    break;
            }
        }
 
        internal void OnLoginAck(SqlLoginAck rec) {
            _loginAck = rec; 
            // 
        }
    } 

    internal sealed class ServerInfo {
        private string _extendedServerName;     // the resolved servername with protocol
        private string _resolvedServerName;     // the resolved servername only 
        private string _userProtocol;           // the user specified protocol
        private string _userServerName;         // the user specified servername 
 
        internal ServerInfo (string userProtocol, string userServerName) {
            _userProtocol = userProtocol; 
            _userServerName = userServerName;
        }
        internal string ExtendedServerName {
            get { 
                return _extendedServerName;
            } 
            // setter will go away 
            set {
                _extendedServerName = value; 
            }
        }

        internal string ResolvedServerName { 
            get {
                return _resolvedServerName; 
            } 
            // setter will go away
            set { 
                _resolvedServerName = value;
            }
        }
 
        internal string UserProtocol {
            get { 
                return _userProtocol; 
            }
        } 

        internal string UserServerName {
            get {
                return _userServerName; 
            }
        } 
 
        internal void SetDerivedNames(string protocol, string serverName) {
            // The following concatenates the specified netlib network protocol to the host string, if netlib is not null 
            // and the flag is on.  This allows the user to specify the network protocol for the connection - but only
            // when using the Dbnetlib dll.  If the protocol is not specified, the netlib will
            // try all protocols in the order listed in the Client Network Utility.  Connect will
            // then fail if all protocols fail. 
            if (!ADP.IsEmpty(protocol)) {
                ExtendedServerName = protocol + ":" + serverName; 
            } 
            else {
                ExtendedServerName = serverName; 
            }
            ResolvedServerName = serverName;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
