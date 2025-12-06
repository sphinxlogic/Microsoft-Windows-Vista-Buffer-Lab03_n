//------------------------------------------------------------------------------ 
// <copyright file="SqlTransaction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
    using System.Data; 
    using System.Data.Common;
    using System.Data.ProviderBase;
    using System.Data.Sql;
    using System.Data.SqlTypes; 
    using System.Diagnostics;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Threading;
 
#if WINFSInternalOnly
    internal
#else
    public 
#endif
    sealed class SqlTransaction : DbTransaction { 
        private  static   int            _objectTypeCount; // Bid counter 
        internal readonly int            _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);
        internal readonly IsolationLevel _isolationLevel = IsolationLevel.ReadCommitted; 

        private  SqlInternalTransaction  _internalTransaction;
        private  SqlConnection           _connection;
 
        private  bool                    _isFromAPI;
 
        internal SqlTransaction(SqlInternalConnection internalConnection, SqlConnection con, 
                                IsolationLevel iso, SqlInternalTransaction internalTransaction) {
            SqlConnection.VerifyExecutePermission(); 

            _isolationLevel = iso;
            _connection = con;
 
            if (internalTransaction == null) {
                _internalTransaction = new SqlInternalTransaction(internalConnection, TransactionType.LocalFromAPI, this); 
            } 
            else {
                Debug.Assert(internalConnection.CurrentTransaction == internalTransaction, "Unexpected Parser.CurrentTransaction state!"); 
                _internalTransaction = internalTransaction;
                _internalTransaction.InitParent(this);
            }
        } 

        //////////////////////////////////////////////////////////////////////////////////////// 
        // PROPERTIES 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        new public SqlConnection Connection { // MDAC 66655
            get {
                if (IsZombied) {
                    return null; 
                }
                else { 
                    return _connection; 
                }
            } 
        }

        override protected DbConnection DbConnection {
            get { 
                return Connection;
            } 
        } 

        internal SqlInternalTransaction InternalTransaction { 
            get {
                return _internalTransaction;
            }
        } 

        override public IsolationLevel IsolationLevel { 
            get { 
                ZombieCheck();
                return _isolationLevel; 
            }
        }

        private bool IsYukonPartialZombie { 
            get {
                return (null != _internalTransaction && _internalTransaction.IsCompleted); 
            } 
        }
 
        internal bool IsZombied {
            get {
                return (null == _internalTransaction || _internalTransaction.IsCompleted);
            } 
        }
 
        internal int ObjectID { 
            get {
                return _objectID; 
            }
        }

        internal SqlStatistics Statistics { 
            get {
                if (null != _connection) { 
                    if (_connection.StatisticsEnabled) { 
                        return _connection.Statistics;
                    } 
                }
                return null;
            }
        } 

        //////////////////////////////////////////////////////////////////////////////////////// 
        // PUBLIC METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        override public void Commit() {
            SqlConnection.ExecutePermission.Demand(); // MDAC 81476

            ZombieCheck(); 

            SqlStatistics statistics = null; 
            IntPtr hscp; 

            Bid.ScopeEnter(out hscp, "<sc.SqlTransaction.Commit|API> %d#", ObjectID); 

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    statistics = SqlStatistics.StartTimer(Statistics);

                    _isFromAPI = true; 

                    _internalTransaction.Commit(); 
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _connection.Abort(e); 
                throw; 
            }
            catch (System.StackOverflowException e) { 
                _connection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _connection.Abort(e);
                throw; 
            } 
            finally {
                _isFromAPI = false; 

                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
#if DEBUG
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { 
                        Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                        if (!IsZombied && !IsYukonPartialZombie) { 
                            _internalTransaction.Dispose();
                        }
#if DEBUG
                    } 
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                    } 
#endif //DEBUG
                } 
                catch (System.OutOfMemoryException e) {
                    _connection.Abort(e);
                    throw;
                } 
                catch (System.StackOverflowException e) {
                    _connection.Abort(e); 
                    throw; 
                }
                catch (System.Threading.ThreadAbortException e)  { 
                    _connection.Abort(e);
                    throw;
                }
            } 
            base.Dispose(disposing);
        } 
 
        override public void Rollback() {
            if (IsYukonPartialZombie) { 
                // Put something in the trace in case a customer has an issue
                if (Bid.AdvancedOn) {
                    Bid.Trace("<sc.SqlTransaction.Rollback|ADV> %d# partial zombie no rollback required\n", ObjectID);
                } 
                _internalTransaction = null; // yukon zombification
            } 
            else { 
                ZombieCheck();
 
                SqlStatistics statistics = null;
                IntPtr hscp;
                Bid.ScopeEnter(out hscp, "<sc.SqlTransaction.Rollback|API> %d#", ObjectID);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
#if DEBUG 
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try {
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                        statistics = SqlStatistics.StartTimer(Statistics);
 
                        _isFromAPI = true; 

                        _internalTransaction.Rollback(); 
#if DEBUG
                    }
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                    }
#endif //DEBUG 
                } 
                catch (System.OutOfMemoryException e) {
                    _connection.Abort(e); 
                    throw;
                }
                catch (System.StackOverflowException e) {
                    _connection.Abort(e); 
                    throw;
                } 
                catch (System.Threading.ThreadAbortException e)  { 
                    _connection.Abort(e);
                    throw; 
                }
                finally {
                    _isFromAPI = false;
 
                    SqlStatistics.StopTimer(statistics);
                    Bid.ScopeLeave(ref hscp); 
                } 
            }
        } 

        public void Rollback(string transactionName) {
            SqlConnection.ExecutePermission.Demand(); // MDAC 81476
 
            ZombieCheck();
 
            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlTransaction.Rollback|API> %d# transactionName='%ls'", ObjectID, transactionName); 

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    statistics = SqlStatistics.StartTimer(Statistics);

                    _isFromAPI = true; 

                    _internalTransaction.Rollback(transactionName); 
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _connection.Abort(e); 
                throw; 
            }
            catch (System.StackOverflowException e) { 
                _connection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _connection.Abort(e);
                throw; 
            } 
            finally {
                _isFromAPI = false; 

                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        public void Save(string savePointName) { 
            SqlConnection.ExecutePermission.Demand(); // MDAC 81476
 
            ZombieCheck();

            SqlStatistics statistics = null;
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlTransaction.Save|API> %d# savePointName='%ls'", ObjectID, savePointName);
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Statistics); 

                    _internalTransaction.Save(savePointName); 
#if DEBUG
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG 
            } 
            catch (System.OutOfMemoryException e) {
                _connection.Abort(e); 
                throw;
            }
            catch (System.StackOverflowException e) {
                _connection.Abort(e); 
                throw;
            } 
            catch (System.Threading.ThreadAbortException e)  { 
                _connection.Abort(e);
                throw; 
            }
            finally {
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        ////////////////////////////////////////////////////////////////////////////////////////
        // INTERNAL METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////

        internal void Zombie() {
            // 

 
 

            SqlInternalConnection internalConnection = (_connection.InnerConnection as SqlInternalConnection); 
            if (null != internalConnection && internalConnection.IsYukonOrNewer && !_isFromAPI) {
                if (Bid.AdvancedOn) {
                    Bid.Trace("<sc.SqlTransaction.Zombie|ADV> %d# yukon deferred zombie\n", ObjectID);
                } 
            }
            else { 
                _internalTransaction = null; // pre-yukon zombification 
            }
 
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // PRIVATE METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        private void ZombieCheck() { 
            // If this transaction has been completed, throw exception since it is unusable.
            if (IsZombied) { 

                if (IsYukonPartialZombie) {
                    _internalTransaction = null; // yukon zombification
                } 

                throw ADP.TransactionZombied(this); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlTransaction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
    using System.Data; 
    using System.Data.Common;
    using System.Data.ProviderBase;
    using System.Data.Sql;
    using System.Data.SqlTypes; 
    using System.Diagnostics;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Threading;
 
#if WINFSInternalOnly
    internal
#else
    public 
#endif
    sealed class SqlTransaction : DbTransaction { 
        private  static   int            _objectTypeCount; // Bid counter 
        internal readonly int            _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);
        internal readonly IsolationLevel _isolationLevel = IsolationLevel.ReadCommitted; 

        private  SqlInternalTransaction  _internalTransaction;
        private  SqlConnection           _connection;
 
        private  bool                    _isFromAPI;
 
        internal SqlTransaction(SqlInternalConnection internalConnection, SqlConnection con, 
                                IsolationLevel iso, SqlInternalTransaction internalTransaction) {
            SqlConnection.VerifyExecutePermission(); 

            _isolationLevel = iso;
            _connection = con;
 
            if (internalTransaction == null) {
                _internalTransaction = new SqlInternalTransaction(internalConnection, TransactionType.LocalFromAPI, this); 
            } 
            else {
                Debug.Assert(internalConnection.CurrentTransaction == internalTransaction, "Unexpected Parser.CurrentTransaction state!"); 
                _internalTransaction = internalTransaction;
                _internalTransaction.InitParent(this);
            }
        } 

        //////////////////////////////////////////////////////////////////////////////////////// 
        // PROPERTIES 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        new public SqlConnection Connection { // MDAC 66655
            get {
                if (IsZombied) {
                    return null; 
                }
                else { 
                    return _connection; 
                }
            } 
        }

        override protected DbConnection DbConnection {
            get { 
                return Connection;
            } 
        } 

        internal SqlInternalTransaction InternalTransaction { 
            get {
                return _internalTransaction;
            }
        } 

        override public IsolationLevel IsolationLevel { 
            get { 
                ZombieCheck();
                return _isolationLevel; 
            }
        }

        private bool IsYukonPartialZombie { 
            get {
                return (null != _internalTransaction && _internalTransaction.IsCompleted); 
            } 
        }
 
        internal bool IsZombied {
            get {
                return (null == _internalTransaction || _internalTransaction.IsCompleted);
            } 
        }
 
        internal int ObjectID { 
            get {
                return _objectID; 
            }
        }

        internal SqlStatistics Statistics { 
            get {
                if (null != _connection) { 
                    if (_connection.StatisticsEnabled) { 
                        return _connection.Statistics;
                    } 
                }
                return null;
            }
        } 

        //////////////////////////////////////////////////////////////////////////////////////// 
        // PUBLIC METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        override public void Commit() {
            SqlConnection.ExecutePermission.Demand(); // MDAC 81476

            ZombieCheck(); 

            SqlStatistics statistics = null; 
            IntPtr hscp; 

            Bid.ScopeEnter(out hscp, "<sc.SqlTransaction.Commit|API> %d#", ObjectID); 

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    statistics = SqlStatistics.StartTimer(Statistics);

                    _isFromAPI = true; 

                    _internalTransaction.Commit(); 
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _connection.Abort(e); 
                throw; 
            }
            catch (System.StackOverflowException e) { 
                _connection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _connection.Abort(e);
                throw; 
            } 
            finally {
                _isFromAPI = false; 

                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
#if DEBUG
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { 
                        Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                        if (!IsZombied && !IsYukonPartialZombie) { 
                            _internalTransaction.Dispose();
                        }
#if DEBUG
                    } 
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                    } 
#endif //DEBUG
                } 
                catch (System.OutOfMemoryException e) {
                    _connection.Abort(e);
                    throw;
                } 
                catch (System.StackOverflowException e) {
                    _connection.Abort(e); 
                    throw; 
                }
                catch (System.Threading.ThreadAbortException e)  { 
                    _connection.Abort(e);
                    throw;
                }
            } 
            base.Dispose(disposing);
        } 
 
        override public void Rollback() {
            if (IsYukonPartialZombie) { 
                // Put something in the trace in case a customer has an issue
                if (Bid.AdvancedOn) {
                    Bid.Trace("<sc.SqlTransaction.Rollback|ADV> %d# partial zombie no rollback required\n", ObjectID);
                } 
                _internalTransaction = null; // yukon zombification
            } 
            else { 
                ZombieCheck();
 
                SqlStatistics statistics = null;
                IntPtr hscp;
                Bid.ScopeEnter(out hscp, "<sc.SqlTransaction.Rollback|API> %d#", ObjectID);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
#if DEBUG 
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try {
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                        statistics = SqlStatistics.StartTimer(Statistics);
 
                        _isFromAPI = true; 

                        _internalTransaction.Rollback(); 
#if DEBUG
                    }
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                    }
#endif //DEBUG 
                } 
                catch (System.OutOfMemoryException e) {
                    _connection.Abort(e); 
                    throw;
                }
                catch (System.StackOverflowException e) {
                    _connection.Abort(e); 
                    throw;
                } 
                catch (System.Threading.ThreadAbortException e)  { 
                    _connection.Abort(e);
                    throw; 
                }
                finally {
                    _isFromAPI = false;
 
                    SqlStatistics.StopTimer(statistics);
                    Bid.ScopeLeave(ref hscp); 
                } 
            }
        } 

        public void Rollback(string transactionName) {
            SqlConnection.ExecutePermission.Demand(); // MDAC 81476
 
            ZombieCheck();
 
            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlTransaction.Rollback|API> %d# transactionName='%ls'", ObjectID, transactionName); 

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    statistics = SqlStatistics.StartTimer(Statistics);

                    _isFromAPI = true; 

                    _internalTransaction.Rollback(transactionName); 
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _connection.Abort(e); 
                throw; 
            }
            catch (System.StackOverflowException e) { 
                _connection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _connection.Abort(e);
                throw; 
            } 
            finally {
                _isFromAPI = false; 

                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        public void Save(string savePointName) { 
            SqlConnection.ExecutePermission.Demand(); // MDAC 81476
 
            ZombieCheck();

            SqlStatistics statistics = null;
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlTransaction.Save|API> %d# savePointName='%ls'", ObjectID, savePointName);
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Statistics); 

                    _internalTransaction.Save(savePointName); 
#if DEBUG
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG 
            } 
            catch (System.OutOfMemoryException e) {
                _connection.Abort(e); 
                throw;
            }
            catch (System.StackOverflowException e) {
                _connection.Abort(e); 
                throw;
            } 
            catch (System.Threading.ThreadAbortException e)  { 
                _connection.Abort(e);
                throw; 
            }
            finally {
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        ////////////////////////////////////////////////////////////////////////////////////////
        // INTERNAL METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////

        internal void Zombie() {
            // 

 
 

            SqlInternalConnection internalConnection = (_connection.InnerConnection as SqlInternalConnection); 
            if (null != internalConnection && internalConnection.IsYukonOrNewer && !_isFromAPI) {
                if (Bid.AdvancedOn) {
                    Bid.Trace("<sc.SqlTransaction.Zombie|ADV> %d# yukon deferred zombie\n", ObjectID);
                } 
            }
            else { 
                _internalTransaction = null; // pre-yukon zombification 
            }
 
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // PRIVATE METHODS 
        ////////////////////////////////////////////////////////////////////////////////////////
 
        private void ZombieCheck() { 
            // If this transaction has been completed, throw exception since it is unusable.
            if (IsZombied) { 

                if (IsYukonPartialZombie) {
                    _internalTransaction = null; // yukon zombification
                } 

                throw ADP.TransactionZombied(this); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
