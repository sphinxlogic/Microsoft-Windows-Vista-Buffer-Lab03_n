//------------------------------------------------------------------------------ 
// <copyright file="SqlInternalConnection.cs" company="Microsoft">
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

    abstract internal class SqlInternalConnection : DbConnectionInternal { 
        private readonly SqlConnectionString _connectionOptions;
        private string                       _currentDatabase;
        private string                       _currentDataSource;
        private bool                         _isEnlistedInTransaction; // is the server-side connection enlisted? true while we're enlisted, reset only after we send a null... 
        private byte[]                       _promotedDTCToken;        // token returned by the server when we promote transaction
        private SqlDelegatedTransaction      _delegatedTransaction;    // the delegated (or promoted) transaction we're responsible for. 
        private byte[]                       _whereAbouts;             // cache the whereabouts (DTC Address) for exporting 

        internal enum TransactionRequest { 
            Begin,
            Promote,
            Commit,
            Rollback, 
            IfRollback,
            Save 
        }; 

        internal SqlInternalConnection(SqlConnectionString connectionOptions) : base() { 
            Debug.Assert(null != connectionOptions, "null connectionOptions?");
            _connectionOptions = connectionOptions;
        }
 
        internal SqlConnection Connection {
            get { 
                return (SqlConnection)Owner; 
            }
        } 

        internal SqlConnectionString ConnectionOptions {
            get {
                return _connectionOptions; 
            }
        } 
 
        internal string CurrentDatabase {
            get { 
                return _currentDatabase;
            }
            set {
                _currentDatabase = value; 
            }
        } 
 
        internal string CurrentDataSource {
            get { 
                return _currentDataSource;
            }
            set {
                _currentDataSource = value; 
            }
        } 
 
        internal SqlDelegatedTransaction DelegatedTransaction {
            get { 
                return _delegatedTransaction;
            }
            set {
                _delegatedTransaction = value; 
            }
        } 
 
        abstract internal SqlInternalTransaction CurrentTransaction {
            get; 
        }

        //
 

 
        virtual internal SqlInternalTransaction AvailableInternalTransaction { 
            get {
                return CurrentTransaction; 
            }
        }

        abstract internal SqlInternalTransaction PendingTransaction { 
            get;
        } 
 
        override internal bool RequireExplicitTransactionUnbind {
            get { 
                return _connectionOptions.TransactionBinding == SqlConnectionString.TransactionBindingEnum.ExplicitUnbind;
            }
        }
 
        override protected internal bool IsNonPoolableTransactionRoot {
            get { 
                return IsTransactionRoot;  // default behavior is that root transactions are NOT poolable.  Subclasses may override. 
            }
        } 

        override internal bool IsTransactionRoot {
            get {
                if (null == _delegatedTransaction) { 
                    return false;
                } 
 
                lock (this) {
                    return (null != _delegatedTransaction && _delegatedTransaction.IsActive); 
                }
            }
        }
 
        internal bool HasLocalTransaction {
            get { 
                SqlInternalTransaction currentTransaction = CurrentTransaction; 
                bool result = (null != currentTransaction && currentTransaction.IsLocal);
                return result; 
            }
        }

        internal bool HasLocalTransactionFromAPI { 
            get {
                SqlInternalTransaction currentTransaction = CurrentTransaction; 
                bool result = (null != currentTransaction && currentTransaction.HasParentTransaction); 
                return result;
            } 
        }

        internal bool IsEnlistedInTransaction {
            get { 
                return _isEnlistedInTransaction;
            } 
        } 

        // Workaround to access context transaction without rewriting connection pool & internalconnections properly. 
        // Context transactions SHOULD be considered enlisted.
        //   This works for now only because we can't unenlist from the context transaction
        // DON'T START USING THIS ANYWHERE EXCEPT IN InternalTransaction and in InternalConnectionSmi!!!
        private SysTx.Transaction _contextTransaction; 
        protected internal SysTx.Transaction ContextTransaction {
            get { 
                return _contextTransaction; 
            }
            set { 
                _contextTransaction = value;
            }
        }
 
        internal SysTx.Transaction InternalEnlistedTransaction {
            get { 
                // Workaround to access context transaction without rewriting connection pool & internalconnections properly. 
                // This SHOULD be a simple wrapper around EnlistedTransaction.
                //   This works for now only because we can't unenlist from the context transaction 
                SysTx.Transaction tx = EnlistedTransaction;

                if (null == tx) {
                    tx = ContextTransaction; 
                }
 
                return tx; 
            }
        } 

        abstract internal bool IsLockedForBulkCopy {
            get;
        } 

        abstract internal bool IsShiloh { 
            get; 
        }
 
        abstract internal bool IsYukonOrNewer {
            get;
        }
 
        abstract internal bool IsKatmaiOrNewer {
            get; 
        } 

#if WINFSFunctionality 
        abstract internal bool IsWinFS {
            get;
        }
#endif 

        internal byte[] PromotedDTCToken { 
            get { 
                return _promotedDTCToken;
            } 
            set {
                _promotedDTCToken = value;
            }
        } 

        abstract internal void AddPreparedCommand(SqlCommand cmd); 
 
        override public DbTransaction BeginTransaction(IsolationLevel iso) {
            return BeginSqlTransaction(iso, null); 
        }

        virtual internal SqlTransaction BeginSqlTransaction(IsolationLevel iso, string transactionName) {
            SqlStatistics statistics = null; 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Connection.Statistics);
 
                    SqlConnection.ExecutePermission.Demand(); // MDAC 81476 

                    ValidateConnectionForExecute(null); 

                    if (HasLocalTransactionFromAPI)
                        throw ADP.ParallelTransactionsNotSupported(Connection);
 
                    if (iso == IsolationLevel.Unspecified) {
                        iso = IsolationLevel.ReadCommitted; // Default to ReadCommitted if unspecified. 
                    } 

                    SqlTransaction transaction = new SqlTransaction(this, Connection, iso, AvailableInternalTransaction); 
                    ExecuteTransaction(TransactionRequest.Begin, transactionName, iso, transaction.InternalTransaction, false);
                    return transaction;
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                Connection.Abort(e);
                throw;
            } 
            catch (System.StackOverflowException e) {
                Connection.Abort(e); 
                throw; 
            }
            catch (System.Threading.ThreadAbortException e) { 
                Connection.Abort(e);
                throw;
            }
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        } 

        override public void ChangeDatabase(string database) { 
            SqlConnection.ExecutePermission.Demand(); // MDAC 80961

            if (ADP.IsEmpty(database)) {
                throw ADP.EmptyDatabaseName(); 
            }
 
            ValidateConnectionForExecute(null); // 

            ChangeDatabaseInternal(database);  // do the real work... 
        }

        abstract protected void ChangeDatabaseInternal(string database);
 
        abstract internal void ClearPreparedCommands();
 
        // 

 

        internal override void CloseConnection(DbConnection owningObject, DbConnectionFactory connectionFactory) {
            if (!IsConnectionDoomed) {
                ClearPreparedCommands(); 
            }
 
            base.CloseConnection(owningObject, connectionFactory); 
        }
 
        override protected void CleanupTransactionOnCompletion(SysTx.Transaction transaction) {
            // Note: unlocked, potentially multi-threaded code, so pull delegate to local to
            //  ensure it doesn't change between test and call.
            SqlDelegatedTransaction delegatedTransaction = DelegatedTransaction; 
            if (null != delegatedTransaction) {
                delegatedTransaction.TransactionEnded(transaction); 
            } 
        }
 
        override protected DbReferenceCollection CreateReferenceCollection() {
            return new SqlReferenceCollection();
        }
 
        override protected void Deactivate() {
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnection.Deactivate|ADV> %d# deactivating\n", base.ObjectID); 
            }
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {

#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    SqlReferenceCollection referenceCollection = (SqlReferenceCollection)ReferenceCollection;
                    if (null != referenceCollection) {
                        referenceCollection.Deactivate();
                    } 

                    // Invoke subclass-specific deactivation logic 
                    InternalDeactivate(); 
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
            catch (Exception e) { 
                //
                if (!ADP.IsCatchableExceptionType(e)) {
                    throw;
                } 

                // if an exception occurred, the inner connection will be 
                // marked as unusable and destroyed upon returning to the 
                // pool
                DoomThisConnection(); 

                ADP.TraceExceptionWithoutRethrow(e);
            }
        } 

        abstract internal void DisconnectTransaction(SqlInternalTransaction internalTransaction); 
 
        override public void Dispose() {
            _whereAbouts = null; 
            base.Dispose();
        }

        protected void Enlist(SysTx.Transaction tx) { 
            // We should never be calling this method once we've created a
            // delegated transaction, because the Manual enlistment should 
            // have caught this and thrown an exception, and the Auto enlistment 
            // isn't possible because Sys.Tx keeps the connection alive until
            // the transaction is completed. 
            Debug.Assert (!IsNonPoolableTransactionRoot, "cannot defect a delegated transaction!");  // potential race condition, but it's an assert

            if (null == tx) {
                 if (IsEnlistedInTransaction) { 
                    EnlistNull();
                } 
            } 
            // Only enlist if it's different...
            else if (!tx.Equals(EnlistedTransaction)) { // WebData 20000024 - Must use Equals, not != 
                EnlistNonNull(tx);
            }
        }
 
        private void EnlistNonNull(SysTx.Transaction tx) {
            Debug.Assert(null != tx, "null transaction?"); 
 
            if (Bid.AdvancedOn) {
                Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, transaction %d#.\n", base.ObjectID, tx.GetHashCode()); 
            }

            bool hasDelegatedTransaction = false;
 
            if (IsYukonOrNewer) {
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, attempting to delegate\n", base.ObjectID); 
                }
 
                // Promotable transactions are only supported on Yukon
                // servers or newer.
                SqlDelegatedTransaction delegatedTransaction = new SqlDelegatedTransaction(this, tx);
 
                try {
                    // NOTE: System.Transactions claims to resolve all 
                    // potential race conditions between multiple delegate 
                    // requests of the same transaction to different
                    // connections in their code, such that only one 
                    // attempt to delegate will succeed.

                    // NOTE: PromotableSinglePhaseEnlist will eventually
                    // make a round trip to the server; doing this inside 
                    // a lock is not the best choice.  We presume that you
                    // aren't trying to enlist concurrently on two threads 
                    // and leave it at that -- We don't claim any thread 
                    // safety with regard to multiple concurrent requests
                    // to enlist the same connection in different 
                    // transactions, which is good, because we don't have
                    // it anyway.

                    // PromotableSinglePhaseEnlist may not actually promote 
                    // the transaction when it is already delegated (this is
                    // the way they resolve the race condition when two 
                    // threads attempt to delegate the same Lightweight 
                    // Transaction)  In that case, we can safely ignore
                    // our delegated transaction, and proceed to enlist 
                    // in the promoted one.

                    if (tx.EnlistPromotableSinglePhase(delegatedTransaction)) {
                        hasDelegatedTransaction = true; 

                        _delegatedTransaction  = delegatedTransaction; 
 
                        if (Bid.AdvancedOn) {
                            long transactionId = SqlInternalTransaction.NullTransactionId; 
                            int transactionObjectID = 0;
                            if (null != CurrentTransaction) {
                                transactionId = CurrentTransaction.TransactionId;
                                transactionObjectID = CurrentTransaction.ObjectID; 
                            }
                            Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, delegated to transaction %d# with transactionId=0x%I64x\n", base.ObjectID, transactionObjectID, transactionId); 
                        } 
                    }
                } 
                catch (SqlException e) {
                    // we do not want to eat the error if it is a fatal one
                    if (e.Class >= TdsEnums.FATAL_ERROR_CLASS) {
                        throw; 
                    }
 
                    // if the parser is not openloggedin, the connection is no longer good 
                    SqlInternalConnectionTds tdsConnection = this as SqlInternalConnectionTds;
                    if (tdsConnection != null) { 
                        if (tdsConnection.Parser.State != TdsParserState.OpenLoggedIn) {
                            throw;
                        }
                    } 

                    ADP.TraceExceptionWithoutRethrow(e); 
 
                    // In this case, SqlDelegatedTransaction.Initialize
                    // failed and we don't necessarily want to reject 
                    // things -- there may have been a legitimate reason
                    // for the failure.
                }
            } 

            if (!hasDelegatedTransaction) { 
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, delegation not possible, enlisting.\n", base.ObjectID);
                } 

                byte[] cookie = null;

                if (null == _whereAbouts) { 
                     byte[] dtcAddress = GetDTCAddress();
 
                     if (null == dtcAddress) { 
                        throw SQL.CannotGetDTCAddress();
                     } 
                     _whereAbouts = dtcAddress;
                }

                cookie = GetTransactionCookie(tx, _whereAbouts); 

                // send cookie to server to finish enlistment 
                PropagateTransactionCookie(cookie); 

                _isEnlistedInTransaction = true; 

                if (Bid.AdvancedOn) {
                    long transactionId = SqlInternalTransaction.NullTransactionId;
                    int transactionObjectID = 0; 
                    if (null != CurrentTransaction) {
                        transactionId = CurrentTransaction.TransactionId; 
                        transactionObjectID = CurrentTransaction.ObjectID; 
                    }
                    Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, enlisted with transaction %d# with transactionId=0x%I64x\n", base.ObjectID, transactionObjectID, transactionId); 
                }
            }

            EnlistedTransaction = tx; // Tell the base class about our enlistment 

 
            // If we're on a Yukon or newer server, and we we delegate the 
            // transaction successfully, we will have done a begin transaction,
            // which produces a transaction id that we should execute all requests 
            // on.  The TdsParser or SmiEventSink will store this information as
            // the current transaction.
            //
            // Likewise, propagating a transaction to a Yukon or newer server will 
            // produce a transaction id that The TdsParser or SmiEventSink will
            // store as the current transaction. 
            // 
            // In either case, when we're working with a Yukon or newer server
            // we better have a current transaction by now. 

            Debug.Assert(!IsYukonOrNewer || null != CurrentTransaction, "delegated/enlisted transaction with null current transaction?");
        }
 
        internal void EnlistNull() {
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnection.EnlistNull|ADV> %d#, unenlisting.\n", base.ObjectID); 
            }
 
            // We were in a transaction, but now we are not - so send
            // message to server with empty transaction - confirmed proper
            // behavior from Sameet Agarwal
            // 
            // The connection pooler maintains separate pools for enlisted
            // transactions, and only when that transaction is committed or 
            // rolled back will those connections be taken from that 
            // separate pool and returned to the general pool of connections
            // that are not affiliated with any transactions.  When this 
            // occurs, we will have a new transaction of null and we are
            // required to send an empty transaction payload to the server.

            PropagateTransactionCookie(null); 

            _isEnlistedInTransaction = false; 
            EnlistedTransaction = null; // Tell the base class about our enlistment 

            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnection.EnlistNull|ADV> %d#, unenlisted.\n", base.ObjectID);
            }

            // The EnlistTransaction above will return an TransactionEnded event, 
            // which causes the TdsParser or SmiEventSink should to clear the
            // current transaction. 
            // 
            // In either case, when we're working with a Yukon or newer server
            // we better not have a current transaction at this point. 

            Debug.Assert(!IsYukonOrNewer || null == CurrentTransaction, "unenlisted transaction with non-null current transaction?");   // verify it!
        }
 
        override public void EnlistTransaction(SysTx.Transaction transaction) {
            SqlConnection.VerifyExecutePermission(); 
 
            ValidateConnectionForExecute(null);
 
            // If a connection has a local transaction outstanding and you try
            // to enlist in a DTC transaction, SQL Server will rollback the
            // local transaction and then do the enlist (7.0 and 2000).  So, if
            // the user tries to do this, throw. 
            if (HasLocalTransaction) {
                throw ADP.LocalTransactionPresent(); 
            } 

            if (null != transaction && transaction.Equals(EnlistedTransaction)) { 
                // No-op if this is the current transaction
                return;
            }
 
            // If a connection is already enlisted in a DTC transaction and you
            // try to enlist in another one, in 7.0 the existing DTC transaction 
            // would roll back and then the connection would enlist in the new 
            // one. In SQL 2000 & Yukon, when you enlist in a DTC transaction
            // while the connection is already enlisted in a DTC transaction, 
            // the connection simply switches enlistments.  Regardless, simply
            // enlist in the user specified distributed transaction.  This
            // behavior matches OLEDB and ODBC.
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    Enlist(transaction);
#if DEBUG 
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG
            }
            catch (System.OutOfMemoryException e) { 
                Connection.Abort(e);
                throw; 
            } 
            catch (System.StackOverflowException e) {
                Connection.Abort(e); 
                throw;
            }
            catch (System.Threading.ThreadAbortException e) {
                Connection.Abort(e); 
                throw;
            } 
        } 

        abstract internal void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso, SqlInternalTransaction internalTransaction, bool isDelegateControlRequest); 

        internal SqlDataReader FindLiveReader(SqlCommand command) {
            SqlDataReader reader = null;
            SqlReferenceCollection referenceCollection = (SqlReferenceCollection)ReferenceCollection; 
            if (null != referenceCollection) {
                reader =  referenceCollection.FindLiveReader(command); 
            } 
            return reader;
        } 

        abstract protected byte[] GetDTCAddress();

        static private byte[] GetTransactionCookie(SysTx.Transaction transaction, byte[] whereAbouts) { 
            byte[] transactionCookie = null;
            if (null != transaction) { 
                transactionCookie = SysTx.TransactionInterop.GetExportCookie(transaction, whereAbouts); 
            }
            return transactionCookie; 
        }

        virtual protected void InternalDeactivate() {
        } 

        internal void OnError(SqlException exception, bool breakConnection) { 
            if (breakConnection) 
                DoomThisConnection();
 
            if (null != Connection) {
                Connection.OnError(exception, breakConnection);
            }
            else if (exception.Class >= TdsEnums.MIN_ERROR_CLASS) { 
                // It is an error, and should be thrown.  Class of TdsEnums.MIN_ERROR_CLASS
                // or above is an error, below TdsEnums.MIN_ERROR_CLASS denotes an info message. 
                throw exception; 
            }
        } 

        abstract protected void PropagateTransactionCookie(byte[] transactionCookie);

        abstract internal void RemovePreparedCommand(SqlCommand cmd); 

        abstract internal void ValidateConnectionForExecute(SqlCommand command); 
 
        // This validation step MUST be done after locking the connection to guarantee we don't
        //  accidentally execute after the transaction has completed on a different thread, causing us to unwittingly 
        //  execute in auto-commit mode.
        internal void ValidateTransaction() {
            if (RequireExplicitTransactionUnbind) {
 
                // If we have a system transaction, check that transaction is active and is the current transaction
                if (null != InternalEnlistedTransaction) { 
                    SysTx.Transaction currentTran = SysTx.Transaction.Current; 
                    if (SysTx.TransactionStatus.Active != InternalEnlistedTransaction.TransactionInformation.Status ||
                            null == currentTran || 
                            !InternalEnlistedTransaction.Equals(currentTran)) {
                        throw ADP.TransactionConnectionMismatch();
                    }
                } 
            }
        } 
 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlInternalConnection.cs" company="Microsoft">
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

    abstract internal class SqlInternalConnection : DbConnectionInternal { 
        private readonly SqlConnectionString _connectionOptions;
        private string                       _currentDatabase;
        private string                       _currentDataSource;
        private bool                         _isEnlistedInTransaction; // is the server-side connection enlisted? true while we're enlisted, reset only after we send a null... 
        private byte[]                       _promotedDTCToken;        // token returned by the server when we promote transaction
        private SqlDelegatedTransaction      _delegatedTransaction;    // the delegated (or promoted) transaction we're responsible for. 
        private byte[]                       _whereAbouts;             // cache the whereabouts (DTC Address) for exporting 

        internal enum TransactionRequest { 
            Begin,
            Promote,
            Commit,
            Rollback, 
            IfRollback,
            Save 
        }; 

        internal SqlInternalConnection(SqlConnectionString connectionOptions) : base() { 
            Debug.Assert(null != connectionOptions, "null connectionOptions?");
            _connectionOptions = connectionOptions;
        }
 
        internal SqlConnection Connection {
            get { 
                return (SqlConnection)Owner; 
            }
        } 

        internal SqlConnectionString ConnectionOptions {
            get {
                return _connectionOptions; 
            }
        } 
 
        internal string CurrentDatabase {
            get { 
                return _currentDatabase;
            }
            set {
                _currentDatabase = value; 
            }
        } 
 
        internal string CurrentDataSource {
            get { 
                return _currentDataSource;
            }
            set {
                _currentDataSource = value; 
            }
        } 
 
        internal SqlDelegatedTransaction DelegatedTransaction {
            get { 
                return _delegatedTransaction;
            }
            set {
                _delegatedTransaction = value; 
            }
        } 
 
        abstract internal SqlInternalTransaction CurrentTransaction {
            get; 
        }

        //
 

 
        virtual internal SqlInternalTransaction AvailableInternalTransaction { 
            get {
                return CurrentTransaction; 
            }
        }

        abstract internal SqlInternalTransaction PendingTransaction { 
            get;
        } 
 
        override internal bool RequireExplicitTransactionUnbind {
            get { 
                return _connectionOptions.TransactionBinding == SqlConnectionString.TransactionBindingEnum.ExplicitUnbind;
            }
        }
 
        override protected internal bool IsNonPoolableTransactionRoot {
            get { 
                return IsTransactionRoot;  // default behavior is that root transactions are NOT poolable.  Subclasses may override. 
            }
        } 

        override internal bool IsTransactionRoot {
            get {
                if (null == _delegatedTransaction) { 
                    return false;
                } 
 
                lock (this) {
                    return (null != _delegatedTransaction && _delegatedTransaction.IsActive); 
                }
            }
        }
 
        internal bool HasLocalTransaction {
            get { 
                SqlInternalTransaction currentTransaction = CurrentTransaction; 
                bool result = (null != currentTransaction && currentTransaction.IsLocal);
                return result; 
            }
        }

        internal bool HasLocalTransactionFromAPI { 
            get {
                SqlInternalTransaction currentTransaction = CurrentTransaction; 
                bool result = (null != currentTransaction && currentTransaction.HasParentTransaction); 
                return result;
            } 
        }

        internal bool IsEnlistedInTransaction {
            get { 
                return _isEnlistedInTransaction;
            } 
        } 

        // Workaround to access context transaction without rewriting connection pool & internalconnections properly. 
        // Context transactions SHOULD be considered enlisted.
        //   This works for now only because we can't unenlist from the context transaction
        // DON'T START USING THIS ANYWHERE EXCEPT IN InternalTransaction and in InternalConnectionSmi!!!
        private SysTx.Transaction _contextTransaction; 
        protected internal SysTx.Transaction ContextTransaction {
            get { 
                return _contextTransaction; 
            }
            set { 
                _contextTransaction = value;
            }
        }
 
        internal SysTx.Transaction InternalEnlistedTransaction {
            get { 
                // Workaround to access context transaction without rewriting connection pool & internalconnections properly. 
                // This SHOULD be a simple wrapper around EnlistedTransaction.
                //   This works for now only because we can't unenlist from the context transaction 
                SysTx.Transaction tx = EnlistedTransaction;

                if (null == tx) {
                    tx = ContextTransaction; 
                }
 
                return tx; 
            }
        } 

        abstract internal bool IsLockedForBulkCopy {
            get;
        } 

        abstract internal bool IsShiloh { 
            get; 
        }
 
        abstract internal bool IsYukonOrNewer {
            get;
        }
 
        abstract internal bool IsKatmaiOrNewer {
            get; 
        } 

#if WINFSFunctionality 
        abstract internal bool IsWinFS {
            get;
        }
#endif 

        internal byte[] PromotedDTCToken { 
            get { 
                return _promotedDTCToken;
            } 
            set {
                _promotedDTCToken = value;
            }
        } 

        abstract internal void AddPreparedCommand(SqlCommand cmd); 
 
        override public DbTransaction BeginTransaction(IsolationLevel iso) {
            return BeginSqlTransaction(iso, null); 
        }

        virtual internal SqlTransaction BeginSqlTransaction(IsolationLevel iso, string transactionName) {
            SqlStatistics statistics = null; 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Connection.Statistics);
 
                    SqlConnection.ExecutePermission.Demand(); // MDAC 81476 

                    ValidateConnectionForExecute(null); 

                    if (HasLocalTransactionFromAPI)
                        throw ADP.ParallelTransactionsNotSupported(Connection);
 
                    if (iso == IsolationLevel.Unspecified) {
                        iso = IsolationLevel.ReadCommitted; // Default to ReadCommitted if unspecified. 
                    } 

                    SqlTransaction transaction = new SqlTransaction(this, Connection, iso, AvailableInternalTransaction); 
                    ExecuteTransaction(TransactionRequest.Begin, transactionName, iso, transaction.InternalTransaction, false);
                    return transaction;
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                Connection.Abort(e);
                throw;
            } 
            catch (System.StackOverflowException e) {
                Connection.Abort(e); 
                throw; 
            }
            catch (System.Threading.ThreadAbortException e) { 
                Connection.Abort(e);
                throw;
            }
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        } 

        override public void ChangeDatabase(string database) { 
            SqlConnection.ExecutePermission.Demand(); // MDAC 80961

            if (ADP.IsEmpty(database)) {
                throw ADP.EmptyDatabaseName(); 
            }
 
            ValidateConnectionForExecute(null); // 

            ChangeDatabaseInternal(database);  // do the real work... 
        }

        abstract protected void ChangeDatabaseInternal(string database);
 
        abstract internal void ClearPreparedCommands();
 
        // 

 

        internal override void CloseConnection(DbConnection owningObject, DbConnectionFactory connectionFactory) {
            if (!IsConnectionDoomed) {
                ClearPreparedCommands(); 
            }
 
            base.CloseConnection(owningObject, connectionFactory); 
        }
 
        override protected void CleanupTransactionOnCompletion(SysTx.Transaction transaction) {
            // Note: unlocked, potentially multi-threaded code, so pull delegate to local to
            //  ensure it doesn't change between test and call.
            SqlDelegatedTransaction delegatedTransaction = DelegatedTransaction; 
            if (null != delegatedTransaction) {
                delegatedTransaction.TransactionEnded(transaction); 
            } 
        }
 
        override protected DbReferenceCollection CreateReferenceCollection() {
            return new SqlReferenceCollection();
        }
 
        override protected void Deactivate() {
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnection.Deactivate|ADV> %d# deactivating\n", base.ObjectID); 
            }
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {

#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    SqlReferenceCollection referenceCollection = (SqlReferenceCollection)ReferenceCollection;
                    if (null != referenceCollection) {
                        referenceCollection.Deactivate();
                    } 

                    // Invoke subclass-specific deactivation logic 
                    InternalDeactivate(); 
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
            catch (Exception e) { 
                //
                if (!ADP.IsCatchableExceptionType(e)) {
                    throw;
                } 

                // if an exception occurred, the inner connection will be 
                // marked as unusable and destroyed upon returning to the 
                // pool
                DoomThisConnection(); 

                ADP.TraceExceptionWithoutRethrow(e);
            }
        } 

        abstract internal void DisconnectTransaction(SqlInternalTransaction internalTransaction); 
 
        override public void Dispose() {
            _whereAbouts = null; 
            base.Dispose();
        }

        protected void Enlist(SysTx.Transaction tx) { 
            // We should never be calling this method once we've created a
            // delegated transaction, because the Manual enlistment should 
            // have caught this and thrown an exception, and the Auto enlistment 
            // isn't possible because Sys.Tx keeps the connection alive until
            // the transaction is completed. 
            Debug.Assert (!IsNonPoolableTransactionRoot, "cannot defect a delegated transaction!");  // potential race condition, but it's an assert

            if (null == tx) {
                 if (IsEnlistedInTransaction) { 
                    EnlistNull();
                } 
            } 
            // Only enlist if it's different...
            else if (!tx.Equals(EnlistedTransaction)) { // WebData 20000024 - Must use Equals, not != 
                EnlistNonNull(tx);
            }
        }
 
        private void EnlistNonNull(SysTx.Transaction tx) {
            Debug.Assert(null != tx, "null transaction?"); 
 
            if (Bid.AdvancedOn) {
                Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, transaction %d#.\n", base.ObjectID, tx.GetHashCode()); 
            }

            bool hasDelegatedTransaction = false;
 
            if (IsYukonOrNewer) {
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, attempting to delegate\n", base.ObjectID); 
                }
 
                // Promotable transactions are only supported on Yukon
                // servers or newer.
                SqlDelegatedTransaction delegatedTransaction = new SqlDelegatedTransaction(this, tx);
 
                try {
                    // NOTE: System.Transactions claims to resolve all 
                    // potential race conditions between multiple delegate 
                    // requests of the same transaction to different
                    // connections in their code, such that only one 
                    // attempt to delegate will succeed.

                    // NOTE: PromotableSinglePhaseEnlist will eventually
                    // make a round trip to the server; doing this inside 
                    // a lock is not the best choice.  We presume that you
                    // aren't trying to enlist concurrently on two threads 
                    // and leave it at that -- We don't claim any thread 
                    // safety with regard to multiple concurrent requests
                    // to enlist the same connection in different 
                    // transactions, which is good, because we don't have
                    // it anyway.

                    // PromotableSinglePhaseEnlist may not actually promote 
                    // the transaction when it is already delegated (this is
                    // the way they resolve the race condition when two 
                    // threads attempt to delegate the same Lightweight 
                    // Transaction)  In that case, we can safely ignore
                    // our delegated transaction, and proceed to enlist 
                    // in the promoted one.

                    if (tx.EnlistPromotableSinglePhase(delegatedTransaction)) {
                        hasDelegatedTransaction = true; 

                        _delegatedTransaction  = delegatedTransaction; 
 
                        if (Bid.AdvancedOn) {
                            long transactionId = SqlInternalTransaction.NullTransactionId; 
                            int transactionObjectID = 0;
                            if (null != CurrentTransaction) {
                                transactionId = CurrentTransaction.TransactionId;
                                transactionObjectID = CurrentTransaction.ObjectID; 
                            }
                            Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, delegated to transaction %d# with transactionId=0x%I64x\n", base.ObjectID, transactionObjectID, transactionId); 
                        } 
                    }
                } 
                catch (SqlException e) {
                    // we do not want to eat the error if it is a fatal one
                    if (e.Class >= TdsEnums.FATAL_ERROR_CLASS) {
                        throw; 
                    }
 
                    // if the parser is not openloggedin, the connection is no longer good 
                    SqlInternalConnectionTds tdsConnection = this as SqlInternalConnectionTds;
                    if (tdsConnection != null) { 
                        if (tdsConnection.Parser.State != TdsParserState.OpenLoggedIn) {
                            throw;
                        }
                    } 

                    ADP.TraceExceptionWithoutRethrow(e); 
 
                    // In this case, SqlDelegatedTransaction.Initialize
                    // failed and we don't necessarily want to reject 
                    // things -- there may have been a legitimate reason
                    // for the failure.
                }
            } 

            if (!hasDelegatedTransaction) { 
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, delegation not possible, enlisting.\n", base.ObjectID);
                } 

                byte[] cookie = null;

                if (null == _whereAbouts) { 
                     byte[] dtcAddress = GetDTCAddress();
 
                     if (null == dtcAddress) { 
                        throw SQL.CannotGetDTCAddress();
                     } 
                     _whereAbouts = dtcAddress;
                }

                cookie = GetTransactionCookie(tx, _whereAbouts); 

                // send cookie to server to finish enlistment 
                PropagateTransactionCookie(cookie); 

                _isEnlistedInTransaction = true; 

                if (Bid.AdvancedOn) {
                    long transactionId = SqlInternalTransaction.NullTransactionId;
                    int transactionObjectID = 0; 
                    if (null != CurrentTransaction) {
                        transactionId = CurrentTransaction.TransactionId; 
                        transactionObjectID = CurrentTransaction.ObjectID; 
                    }
                    Bid.Trace("<sc.SqlInternalConnection.EnlistNonNull|ADV> %d#, enlisted with transaction %d# with transactionId=0x%I64x\n", base.ObjectID, transactionObjectID, transactionId); 
                }
            }

            EnlistedTransaction = tx; // Tell the base class about our enlistment 

 
            // If we're on a Yukon or newer server, and we we delegate the 
            // transaction successfully, we will have done a begin transaction,
            // which produces a transaction id that we should execute all requests 
            // on.  The TdsParser or SmiEventSink will store this information as
            // the current transaction.
            //
            // Likewise, propagating a transaction to a Yukon or newer server will 
            // produce a transaction id that The TdsParser or SmiEventSink will
            // store as the current transaction. 
            // 
            // In either case, when we're working with a Yukon or newer server
            // we better have a current transaction by now. 

            Debug.Assert(!IsYukonOrNewer || null != CurrentTransaction, "delegated/enlisted transaction with null current transaction?");
        }
 
        internal void EnlistNull() {
            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnection.EnlistNull|ADV> %d#, unenlisting.\n", base.ObjectID); 
            }
 
            // We were in a transaction, but now we are not - so send
            // message to server with empty transaction - confirmed proper
            // behavior from Sameet Agarwal
            // 
            // The connection pooler maintains separate pools for enlisted
            // transactions, and only when that transaction is committed or 
            // rolled back will those connections be taken from that 
            // separate pool and returned to the general pool of connections
            // that are not affiliated with any transactions.  When this 
            // occurs, we will have a new transaction of null and we are
            // required to send an empty transaction payload to the server.

            PropagateTransactionCookie(null); 

            _isEnlistedInTransaction = false; 
            EnlistedTransaction = null; // Tell the base class about our enlistment 

            if (Bid.AdvancedOn) { 
                Bid.Trace("<sc.SqlInternalConnection.EnlistNull|ADV> %d#, unenlisted.\n", base.ObjectID);
            }

            // The EnlistTransaction above will return an TransactionEnded event, 
            // which causes the TdsParser or SmiEventSink should to clear the
            // current transaction. 
            // 
            // In either case, when we're working with a Yukon or newer server
            // we better not have a current transaction at this point. 

            Debug.Assert(!IsYukonOrNewer || null == CurrentTransaction, "unenlisted transaction with non-null current transaction?");   // verify it!
        }
 
        override public void EnlistTransaction(SysTx.Transaction transaction) {
            SqlConnection.VerifyExecutePermission(); 
 
            ValidateConnectionForExecute(null);
 
            // If a connection has a local transaction outstanding and you try
            // to enlist in a DTC transaction, SQL Server will rollback the
            // local transaction and then do the enlist (7.0 and 2000).  So, if
            // the user tries to do this, throw. 
            if (HasLocalTransaction) {
                throw ADP.LocalTransactionPresent(); 
            } 

            if (null != transaction && transaction.Equals(EnlistedTransaction)) { 
                // No-op if this is the current transaction
                return;
            }
 
            // If a connection is already enlisted in a DTC transaction and you
            // try to enlist in another one, in 7.0 the existing DTC transaction 
            // would roll back and then the connection would enlist in the new 
            // one. In SQL 2000 & Yukon, when you enlist in a DTC transaction
            // while the connection is already enlisted in a DTC transaction, 
            // the connection simply switches enlistments.  Regardless, simply
            // enlist in the user specified distributed transaction.  This
            // behavior matches OLEDB and ODBC.
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    Enlist(transaction);
#if DEBUG 
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG
            }
            catch (System.OutOfMemoryException e) { 
                Connection.Abort(e);
                throw; 
            } 
            catch (System.StackOverflowException e) {
                Connection.Abort(e); 
                throw;
            }
            catch (System.Threading.ThreadAbortException e) {
                Connection.Abort(e); 
                throw;
            } 
        } 

        abstract internal void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso, SqlInternalTransaction internalTransaction, bool isDelegateControlRequest); 

        internal SqlDataReader FindLiveReader(SqlCommand command) {
            SqlDataReader reader = null;
            SqlReferenceCollection referenceCollection = (SqlReferenceCollection)ReferenceCollection; 
            if (null != referenceCollection) {
                reader =  referenceCollection.FindLiveReader(command); 
            } 
            return reader;
        } 

        abstract protected byte[] GetDTCAddress();

        static private byte[] GetTransactionCookie(SysTx.Transaction transaction, byte[] whereAbouts) { 
            byte[] transactionCookie = null;
            if (null != transaction) { 
                transactionCookie = SysTx.TransactionInterop.GetExportCookie(transaction, whereAbouts); 
            }
            return transactionCookie; 
        }

        virtual protected void InternalDeactivate() {
        } 

        internal void OnError(SqlException exception, bool breakConnection) { 
            if (breakConnection) 
                DoomThisConnection();
 
            if (null != Connection) {
                Connection.OnError(exception, breakConnection);
            }
            else if (exception.Class >= TdsEnums.MIN_ERROR_CLASS) { 
                // It is an error, and should be thrown.  Class of TdsEnums.MIN_ERROR_CLASS
                // or above is an error, below TdsEnums.MIN_ERROR_CLASS denotes an info message. 
                throw exception; 
            }
        } 

        abstract protected void PropagateTransactionCookie(byte[] transactionCookie);

        abstract internal void RemovePreparedCommand(SqlCommand cmd); 

        abstract internal void ValidateConnectionForExecute(SqlCommand command); 
 
        // This validation step MUST be done after locking the connection to guarantee we don't
        //  accidentally execute after the transaction has completed on a different thread, causing us to unwittingly 
        //  execute in auto-commit mode.
        internal void ValidateTransaction() {
            if (RequireExplicitTransactionUnbind) {
 
                // If we have a system transaction, check that transaction is active and is the current transaction
                if (null != InternalEnlistedTransaction) { 
                    SysTx.Transaction currentTran = SysTx.Transaction.Current; 
                    if (SysTx.TransactionStatus.Active != InternalEnlistedTransaction.TransactionInformation.Status ||
                            null == currentTran || 
                            !InternalEnlistedTransaction.Equals(currentTran)) {
                        throw ADP.TransactionConnectionMismatch();
                    }
                } 
            }
        } 
 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
