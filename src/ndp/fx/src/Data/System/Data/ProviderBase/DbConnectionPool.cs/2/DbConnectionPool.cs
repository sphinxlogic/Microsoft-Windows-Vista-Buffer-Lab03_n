//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionPool.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.InteropServices;
    using System.Security; 
    using System.Security.Permissions;
    using System.Security.Principal;
    using System.Threading;
    using SysTx = System.Transactions; 

    sealed internal class DbConnectionPool { 
        private enum State { 
            Initializing,
            Running, 
            ShuttingDown,
        }

        internal const Bid.ApiGroup PoolerTracePoints = (Bid.ApiGroup)0x1000; 

        sealed private class TransactedConnectionPool : Hashtable { 
            DbConnectionPool _pool; 

            private static int _objectTypeCount; // Bid counter 
            internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);

            internal TransactedConnectionPool(DbConnectionPool pool) : base() {
                Debug.Assert(null != pool, "null pool?"); 

                _pool = pool; 
                Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactedConnectionPool|RES|CPOOL> %d#, Constructed for connection pool %d#\n", ObjectID, _pool.ObjectID); 
            }
 
            internal int ObjectID {
                get {
                    return _objectID;
                } 
            }
 
            internal DbConnectionPool Pool { 
                get {
                    return _pool; 
                }
            }

            internal DbConnectionInternal GetTransactedObject(SysTx.Transaction transaction) { 
                Debug.Assert(null != transaction, "null transaction?");
 
                DbConnectionInternal transactedObject = null; 

                List<DbConnectionInternal> connections = (List<DbConnectionInternal>)this[transaction]; 

                if (null != connections) {
                    lock (connections) {
                        int i = connections.Count - 1; 
                        if (0 <= i) {
                            transactedObject = connections[i]; 
                            connections.RemoveAt(i); 
                        }
                    } 
                }

                if (null != transactedObject) {
                    Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.GetTransactedObject|RES|CPOOL> %d#, Transaction %d#, Connection %d#, Popped.\n", ObjectID, transaction.GetHashCode(), transactedObject.ObjectID); 
                }
                return transactedObject; 
            } 

            internal void PutTransactedObject(SysTx.Transaction transaction, DbConnectionInternal transactedObject) { 
                Debug.Assert(null != transaction, "null transaction?");
                Debug.Assert(null != transactedObject, "null transactedObject?");

                Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.PutTransactedObject|RES|CPOOL> %d#, Transaction %d#, Connection %d#, Pushing.\n", ObjectID, transaction.GetHashCode(), transactedObject.ObjectID); 

                List<DbConnectionInternal> connections = (List<DbConnectionInternal>)this[transaction]; 
 
                // NOTE: it is possible that the connecton was put on the
                //       deactivate queue, and while it was on the queue, the 
                //       transaction ended, causing the list for the transaction
                //       to have been removed.  In that case, we can't expect
                //       the list to be here.
                if (null != connections) { 
                    lock (connections) {
                        Debug.Assert(0 > connections.IndexOf(transactedObject), "adding to pool a second time?"); 
                        connections.Add(transactedObject); 
                        Pool.PerformanceCounters.NumberOfFreeConnections.Increment();
                    } 
                }
            }

            internal void TransactionBegin(SysTx.Transaction transaction) { 
                Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionBegin|RES|CPOOL> %d#, Transaction %d#, Begin.\n", ObjectID, transaction.GetHashCode());
 
                List<DbConnectionInternal> connections = (List<DbConnectionInternal>)this[transaction]; 

                if (null == connections) { 
                    List<DbConnectionInternal> newConnections= new List<DbConnectionInternal>(2); // start with only two connections in the list; most times we won't need that many.
                    SysTx.Transaction transactionClone = null;
                    try {
                        transactionClone = transaction.Clone(); 

                        Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionBegin|RES|CPOOL> %d#, Transaction %d#, Adding List to transacted pool.\n", ObjectID, transaction.GetHashCode()); 
                        lock (this) { 
                            connections = (List<DbConnectionInternal>)this[transaction];
 
                            if (null == connections) {
                                connections = newConnections;
                                this.Add(transactionClone, connections);
                                transactionClone = null; // we've used it -- don't throw it away. 
                            }
                        } 
                    } 
                    finally {
                        if (null != transactionClone) { 
                            transactionClone.Dispose();
                        }
                    }
                    newConnections = null; 
                    Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionBegin|RES|CPOOL> %d#, Transaction %d#, Added.\n", ObjectID, transaction.GetHashCode());
                } 
            } 

            internal void TransactionEnded(SysTx.Transaction transaction, DbConnectionInternal transactedObject) { 
                Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionEnded|RES|CPOOL> %d#, Transaction %d#, Connection %d#, Transaction Completed\n", ObjectID, transaction.GetHashCode(), transactedObject.ObjectID);

                List<DbConnectionInternal> connections = (List<DbConnectionInternal>)this[transaction];
                int entry = -1; 

                // NOTE: we may be ending a transaction for a connection that is 
                //       currently not in the pool, and therefore it may not have 
                //       a list for it, because it may have been removed already.
                if (null != connections) { 
                    lock (connections) {
                        entry = connections.IndexOf(transactedObject);

                        if (entry >= 0) { 
                            connections.RemoveAt(entry);
                        } 
 
                        // Once we've completed all the ended notifications, we can
                        // safely remove the list from the transacted pool. 
                        if (0 >= connections.Count) {
                            Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionEnded|RES|CPOOL> %d#, Transaction %d#, Removing List from transacted pool.\n", ObjectID, transaction.GetHashCode());
                            lock (this) {
                                Remove(transaction); 
                            }
                            Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionEnded|RES|CPOOL> %d#, Transaction %d#, Removed.\n", ObjectID, transaction.GetHashCode()); 
 
                            // we really need to dispose our clone; it may have
                            // native resources and GC may not happen soon enough. 
                            transaction.Dispose();
                        }
                    }
                } 

                // If (and only if) we found the connection in the list of 
                // connections, we'll put it back... 
                if (0 <= entry)  {
                    Pool.PerformanceCounters.NumberOfFreeConnections.Decrement(); 
                    Pool.PutObjectFromTransactedPool(transactedObject);
                }
            }
        } 

        private sealed class PoolWaitHandles : DbBuffer { 
 
            private readonly Semaphore _poolSemaphore;
            private readonly ManualResetEvent _errorEvent; 

            // Using a Mutex requires ThreadAffinity because SQL CLR can swap
            // the underlying Win32 thread associated with a managed thread in preemptive mode.
            // Using an AutoResetEvent does not have that complication. 
            private readonly Semaphore _creationSemaphore;
 
            private readonly SafeHandle _poolHandle; 
            private readonly SafeHandle _errorHandle;
            private readonly SafeHandle _creationHandle; 

            private readonly int _releaseFlags;

            internal PoolWaitHandles(Semaphore poolSemaphore, ManualResetEvent errorEvent, Semaphore creationSemaphore) : base(3*IntPtr.Size) { 
                bool mustRelease1 = false, mustRelease2 = false, mustRelease3 = false;
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try { 
                    _poolSemaphore     = poolSemaphore;
                    _errorEvent        = errorEvent; 
                    _creationSemaphore = creationSemaphore;

                    // because SafeWaitHandle doesn't have reliability contract
                    _poolHandle     = poolSemaphore.SafeWaitHandle; 
                    _errorHandle    = errorEvent.SafeWaitHandle;
                    _creationHandle = creationSemaphore.SafeWaitHandle; 
 
                    _poolHandle.DangerousAddRef(ref mustRelease1);
                    _errorHandle.DangerousAddRef(ref mustRelease2); 
                    _creationHandle.DangerousAddRef(ref mustRelease3);

                    Debug.Assert(0 == SEMAPHORE_HANDLE, "SEMAPHORE_HANDLE");
                    Debug.Assert(1 == ERROR_HANDLE, "ERROR_HANDLE"); 
                    Debug.Assert(2 == CREATION_HANDLE, "CREATION_HANDLE");
 
                    WriteIntPtr(SEMAPHORE_HANDLE*IntPtr.Size, _poolHandle.DangerousGetHandle()); 
                    WriteIntPtr(ERROR_HANDLE*IntPtr.Size,     _errorHandle.DangerousGetHandle());
                    WriteIntPtr(CREATION_HANDLE*IntPtr.Size,  _creationHandle.DangerousGetHandle()); 
                }
                finally {
                    if (mustRelease1) {
                        _releaseFlags |= 1; 
                    }
                    if (mustRelease2) { 
                        _releaseFlags |= 2; 
                    }
                    if (mustRelease3) { 
                        _releaseFlags |= 4;
                    }
                }
            } 

            internal SafeHandle CreationHandle { 
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
                get { return _creationHandle; }
            } 

            internal Semaphore CreationSemaphore {
                get { return _creationSemaphore; }
            } 

            internal ManualResetEvent ErrorEvent { 
                get { return _errorEvent; } 
            }
 
            internal Semaphore PoolSemaphore {
                get { return _poolSemaphore; }
            }
 
            protected override bool ReleaseHandle() {
                // NOTE: The SafeHandle class guarantees this will be called exactly once. 
                // we know we can touch these other managed objects because of our original DangerousAddRef 
                if (0 != (1 & _releaseFlags)) {
                    _poolHandle.DangerousRelease(); 
                }
                if (0 != (2 & _releaseFlags)) {
                    _errorHandle.DangerousRelease();
                } 
                if (0 != (4 & _releaseFlags)) {
                    _creationHandle.DangerousRelease(); 
                } 
                return base.ReleaseHandle();
            } 
        }

        private const int MAX_Q_SIZE    = (int)0x00100000;
 
        // The order of these is important; we want the WaitAny call to be signaled
        // for a free object before a creation signal.  Only the index first signaled 
        // object is returned from the WaitAny call. 
        private const int SEMAPHORE_HANDLE = (int)0x0;
        private const int ERROR_HANDLE     = (int)0x1; 
        private const int CREATION_HANDLE  = (int)0x2;
        private const int BOGUS_HANDLE     = (int)0x3;

        private const int WAIT_OBJECT_0 = 0; 
        private const int WAIT_TIMEOUT   = (int)0x102;
        private const int WAIT_ABANDONED = (int)0x80; 
        private const int WAIT_FAILED    = -1; 

        private const int ERROR_WAIT_DEFAULT = 5 * 1000; // 5 seconds 

        // we do want a testable, repeatable set of generated random numbers
        private static readonly Random _random = new Random(5101977); // Value obtained from Dave Driver
 
        private readonly int              _cleanupWait;
        private readonly DbConnectionPoolIdentity _identity; 
 
        private readonly DbConnectionFactory          _connectionFactory;
        private readonly DbConnectionPoolGroup        _connectionPoolGroup; 
        private readonly DbConnectionPoolGroupOptions _connectionPoolGroupOptions;
        private          DbConnectionPoolProviderInfo _connectionPoolProviderInfo;

        private State                     _state; 

        private readonly DbConnectionInternalListStack _stackOld = new DbConnectionInternalListStack(); 
        private readonly DbConnectionInternalListStack _stackNew = new DbConnectionInternalListStack(); 

        private readonly WaitCallback     _poolCreateRequest; 

        private readonly Queue            _deactivateQueue;
        private readonly WaitCallback     _deactivateCallback;
 
        private int                       _waitCount;
        private readonly PoolWaitHandles  _waitHandles; 
 
        private Exception                 _resError;
        private volatile bool             _errorOccurred; 

        private int                       _errorWait;
        private Timer                     _errorTimer;
 
        private Timer                     _cleanupTimer;
 
        private readonly TransactedConnectionPool _transactedConnectionPool; 

        private readonly List<DbConnectionInternal> _objectList; 
        private int                       _totalObjects;

        private static int _objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount); 

        // only created by DbConnectionPoolGroup.GetConnectionPool 
        internal DbConnectionPool( 
                            DbConnectionFactory connectionFactory,
                            DbConnectionPoolGroup connectionPoolGroup, 
                            DbConnectionPoolIdentity identity,
                            DbConnectionPoolProviderInfo connectionPoolProviderInfo ) {
            Debug.Assert(ADP.IsWindowsNT, "Attempting to construct a connection pool on Win9x?");
            Debug.Assert(null != connectionPoolGroup, "null connectionPoolGroup"); 

            if ((null != identity) && identity.IsRestricted) { 
                throw ADP.InternalError(ADP.InternalErrorCode.AttemptingToPoolOnRestrictedToken); 
            }
 
            _state= State.Initializing;

            lock(_random) { // Random.Next is not thread-safe
                _cleanupWait = _random.Next(12, 24)*10*1000; // 2-4 minutes in 10 sec intervals, WebData 103603 
            }
 
            _connectionFactory = connectionFactory; 
            _connectionPoolGroup = connectionPoolGroup;
            _connectionPoolGroupOptions = connectionPoolGroup.PoolGroupOptions; 
            _connectionPoolProviderInfo = connectionPoolProviderInfo;
            _identity = identity;

            if (UseDeactivateQueue) { 
                _deactivateQueue = new Queue();
                _deactivateCallback = new WaitCallback(ProcessDeactivateQueue); 
            } 

            _waitHandles = new PoolWaitHandles( 
                                new Semaphore(0, MAX_Q_SIZE),
                                new ManualResetEvent(false),
                                new Semaphore(1, 1));
 
            _errorWait      = ERROR_WAIT_DEFAULT;
            _errorTimer     = null;  // No error yet. 
 
            _objectList     = new List<DbConnectionInternal>(MaxPoolSize);
 
            if(ADP.IsPlatformNT5) {
                _transactedConnectionPool = new TransactedConnectionPool(this);
            }
 
            _poolCreateRequest = new WaitCallback(PoolCreateRequest); // used by CleanupCallback
            _state = State.Running; 
 
            Bid.PoolerTrace("<prov.DbConnectionPool.DbConnectionPool|RES|CPOOL> %d#, Constructed.\n", ObjectID);
 
            //_cleanupTimer & QueuePoolCreateRequest is delayed until DbConnectionPoolGroup calls
            // StartBackgroundCallbacks after pool is actually in the collection
        }
 
        private int CreationTimeout {
            get { return PoolGroupOptions.CreationTimeout; } 
        } 

        internal int Count { 
            get { return _totalObjects; }
        }

        internal DbConnectionFactory ConnectionFactory { 
            get { return _connectionFactory; }
        } 
 
        internal bool ErrorOccurred {
            get { return _errorOccurred; } 
        }

        private bool HasTransactionAffinity {
            get { return PoolGroupOptions.HasTransactionAffinity; } 
        }
 
        internal TimeSpan LoadBalanceTimeout { 
            get { return PoolGroupOptions.LoadBalanceTimeout; }
        } 

        private bool NeedToReplenish {
            get {
                if (State.Running != _state) // SQL BU DT 364595 - don't allow connection create when not running. 
                    return false;
 
                int totalObjects = Count; 

                if (totalObjects >= MaxPoolSize) 
                    return false;

                if (totalObjects < MinPoolSize)
                    return true; 

                int freeObjects        = (_stackNew.Count + _stackOld.Count); 
                int waitingRequests    = _waitCount; 
                bool needToReplenish = (freeObjects < waitingRequests) || ((freeObjects == waitingRequests) && (totalObjects > 1));
 
                return needToReplenish;
            }
        }
 
        internal DbConnectionPoolIdentity Identity {
            get { return _identity; } 
        } 

        private int MaxPoolSize { 
            get { return PoolGroupOptions.MaxPoolSize; }
        }

        private int MinPoolSize { 
            get { return PoolGroupOptions.MinPoolSize; }
        } 
 
        internal int ObjectID {
            get { 
                return _objectID;
            }
        }
 
        internal DbConnectionPoolCounters PerformanceCounters {
            get { return _connectionFactory.PerformanceCounters; } 
        } 

        internal DbConnectionPoolGroup PoolGroup { 
            get { return _connectionPoolGroup; }
        }

        internal DbConnectionPoolGroupOptions PoolGroupOptions { 
            get { return _connectionPoolGroupOptions; }
        } 
 
        internal DbConnectionPoolProviderInfo ProviderInfo {
            get { return _connectionPoolProviderInfo; } 
        }

        private bool UseDeactivateQueue {
            get { return PoolGroupOptions.UseDeactivateQueue; } 
        }
 
        internal bool UseLoadBalancing { 
            get { return PoolGroupOptions.UseLoadBalancing; }
        } 

        private bool UsingIntegrateSecurity {
            get { return (null != _identity && DbConnectionPoolIdentity.NoIdentity != _identity); }
        } 

        private void CleanupCallback(Object state) { 
            // Called when the cleanup-timer ticks over. 

            // This is the automatic prunning method.  Every period, we will 
            // perform a two-step process:
            //
            // First, for each free object above MinPoolSize, we will obtain a
            // semaphore representing one object and destroy one from old stack. 
            // We will continue this until we either reach MinPoolSize, we are
            // unable to obtain a free object, or we have exhausted all the 
            // objects on the old stack. 
            //
            // Second we move all free objects on the new stack to the old stack. 
            // So, every period the objects on the old stack are destroyed and
            // the objects on the new stack are pushed to the old stack.  All
            // objects that are currently out and in use are not on either stack.
            // 
            // With this logic, objects are pruned from the pool if unused for
            // at least one period but not more than two periods. 
 
            Bid.PoolerTrace("<prov.DbConnectionPool.CleanupCallback|RES|INFO|CPOOL> %d#\n", ObjectID);
 
            // Destroy free objects that put us above MinPoolSize from old stack.
            while(Count > MinPoolSize) { // While above MinPoolSize...

                if (_waitHandles.PoolSemaphore.WaitOne(0, false) /* != WAIT_TIMEOUT */) { 
                    // We obtained a objects from the semaphore.
                    DbConnectionInternal obj = _stackOld.SynchronizedPop(); 
 
                    if (null != obj) {
                        // If we obtained one from the old stack, destroy it. 
                        PerformanceCounters.NumberOfFreeConnections.Decrement();

                        // Transaction roots must survive even aging out (TxEnd event will clean them up).
                        bool shouldDestroy = true; 
                        lock (obj) {    // Lock to prevent race window between IsTransactionRoot and shouldDestroy assignment
                            if (obj.IsTransactionRoot) { 
                                shouldDestroy = false; 
                            }
                        } 

                        // !!!!!!!!!! WARNING !!!!!!!!!!!!!
                        //   ONLY touch obj after lock release if shouldDestroy is false!!!  Otherwise, it may be destroyed
                        //   by transaction-end thread! 

                        // Note that there is a minor race condition between this task and the transaction end event, if the latter runs 
                        //  between the lock above and the SetInStasis call below. The reslult is that the stasis counter may be 
                        //  incremented without a corresponding decrement (the transaction end task is normally expected
                        //  to decrement, but will only do so if the stasis flag is set when it runs). I've minimized the size 
                        //  of the window, but we aren't totally eliminating it due to SetInStasis needing to do bid tracing, which
                        //  we don't want to do under this lock, if possible. It should be possible to eliminate this race with
                        //  more substantial re-architecture of the pool, but we don't have the time to do that work for the current release.
 
                        if (shouldDestroy) {
                            DestroyObject(obj); 
                        } 
                        else {
                            obj.SetInStasis(); 
                        }
                    }
                    else {
                        // Else we exhausted the old stack (the object the 
                        // semaphore represents is on the new stack), so break.
                        _waitHandles.PoolSemaphore.Release(1); 
                        break; 
                    }
                } 
                else {
                    break;
                }
            } 

            // Push to the old-stack.  For each free object, move object from 
            // new stack to old stack. 
            if(_waitHandles.PoolSemaphore.WaitOne(0, false) /* != WAIT_TIMEOUT */) {
                for(;;) { 
                    DbConnectionInternal obj = _stackNew.SynchronizedPop();

                    if (null == obj)
                        break; 

                    Bid.PoolerTrace("<prov.DbConnectionPool.CleanupCallback|RES|INFO|CPOOL> %d#, ChangeStacks=%d#\n", ObjectID, obj.ObjectID); 
 
                    Debug.Assert(!obj.IsEmancipated, "pooled object not in pool");
                    Debug.Assert(obj.CanBePooled,     "pooled object is not poolable"); 

                    _stackOld.SynchronizedPush(obj);
                }
                _waitHandles.PoolSemaphore.Release(1); 
            }
 
            // Queue up a request to bring us up to MinPoolSize 
            QueuePoolCreateRequest();
        } 

        internal void Clear() {
            Bid.PoolerTrace("<prov.DbConnectionPool.Clear|RES|CPOOL> %d#, Clearing.\n", ObjectID);
 
            DbConnectionInternal obj;
 
            // First, quickly doom everything. 
            lock(_objectList) {
                int count = _objectList.Count; 

                for (int i = 0; i < count; ++i) {
                    obj = _objectList[i];
 
                    if (null != obj) {
                        obj.DoNotPoolThisConnection(); 
                    } 
                }
            } 

            // Second, dispose of all the free connections.
            while (null != (obj = _stackNew.SynchronizedPop())) {
                PerformanceCounters.NumberOfFreeConnections.Decrement(); 
                DestroyObject(obj);
            } 
            while (null != (obj = _stackOld.SynchronizedPop())) { 
                PerformanceCounters.NumberOfFreeConnections.Decrement();
                DestroyObject(obj); 
            }

            // Finally, reclaim everything that's emancipated (which, because
            // it's been doomed, will cause it to be disposed of as well) 
            ReclaimEmancipatedObjects();
 
            Bid.PoolerTrace("<prov.DbConnectionPool.Clear|RES|CPOOL> %d#, Cleared.\n", ObjectID); 
        }
 
        private Timer CreateCleanupTimer() {
            return (new Timer(new TimerCallback(this.CleanupCallback), null, _cleanupWait, _cleanupWait));
        }
 
        private DbConnectionInternal CreateObject(DbConnection owningObject) {
            DbConnectionInternal newObj = null; 
 
            try {
                newObj = _connectionFactory.CreatePooledConnection(owningObject, this, _connectionPoolGroup.ConnectionOptions); 
                if (null == newObj) {
                    throw ADP.InternalError(ADP.InternalErrorCode.CreateObjectReturnedNull);    // CreateObject succeeded, but null object
                }
                if (!newObj.CanBePooled) { 
                    throw ADP.InternalError(ADP.InternalErrorCode.NewObjectCannotBePooled);        // CreateObject succeeded, but non-poolable object
                } 
                newObj.PrePush(null); 

                lock (_objectList) { 
                    _objectList.Add(newObj);
                    _totalObjects = _objectList.Count;
                    PerformanceCounters.NumberOfPooledConnections.Increment();   //
                } 
                Bid.PoolerTrace("<prov.DbConnectionPool.CreateObject|RES|CPOOL> %d#, Connection %d#, Added to pool.\n", ObjectID, newObj.ObjectID);
 
                // Reset the error wait: 
                _errorWait = ERROR_WAIT_DEFAULT;
            } 
            catch(Exception e)  {
                //
                if (!ADP.IsCatchableExceptionType(e)) {
                    throw; 
                }
 
                ADP.TraceExceptionForCapture(e); 

                newObj = null; // set to null, so we do not return bad new object 
                // Failed to create instance
                _resError = e;
                _waitHandles.ErrorEvent.Set();
                   _errorOccurred = true; 
                _errorTimer = new Timer(new TimerCallback(this.ErrorCallback), null, _errorWait, _errorWait);
 
                if (30000 < _errorWait) { 
                    _errorWait = 60000;
                } 
                else {
                    _errorWait *= 2;
                }
                throw; 
            }
            return newObj; 
        } 

        private void DeactivateObject(DbConnectionInternal obj) { 
            Bid.PoolerTrace("<prov.DbConnectionPool.DeactivateObject|RES|CPOOL> %d#, Connection %d#, Deactivating.\n", ObjectID, obj.ObjectID);

            obj.DeactivateConnection(); // we presume this operation is safe outside of a lock...
 
            if ((State.Running == _state) && !obj.IsConnectionDoomed) {
                bool returnToGeneralPool = true; 
 
                lock (obj) {
                    // A connection with a delegated transaction cannot currently 
                    // be returned to a different customer until the transaction
                    // actually completes, so we send it into Stasis -- the SysTx
                    // transaction object will ensure that it is owned (not lost),
                    // and it will be certain to put it back into the pool. 
                    if (obj.IsNonPoolableTransactionRoot) {
                        obj.SetInStasis(); 
                        returnToGeneralPool = false; 
                    }
                    else { 
                        // We must put this connection into the transacted pool
                        // while inside a lock to prevent a race condition with
                        // the transaction asyncronously completing on a second
                        // thread. 
                        SysTx.Transaction transaction = obj.EnlistedTransaction;
                        if (null != transaction) { 
                            TransactionBegin(transaction);  // Delayed creation of transacted pool 
                            _transactedConnectionPool.PutTransactedObject(transaction, obj);
                            returnToGeneralPool = false; 
                        }
                    }
                }
 
                // Only push the connection into the general pool if we didn't
                // already push it onto the transacted pool. 
                if (returnToGeneralPool) { 
                    PutNewObject(obj);
                } 
            }
            else {
                // the object is not fit for reuse -- just dispose of it.
                DestroyObject(obj); 
                QueuePoolCreateRequest();
            } 
        } 

        private void DestroyObject(DbConnectionInternal obj) { 
            // A connection with a delegated transaction cannot be disposed of
            // until the delegated transaction has actually completed.  Instead,
            // we simply leave it alone; when the transaction completes, it will
            // come back through PutObjectFromTransactedPool, which will call us 
            // again.
            if (obj.IsTxRootWaitingForTxEnd) { 
                Bid.PoolerTrace("<prov.DbConnectionPool.DestroyObject|RES|CPOOL> %d#, Connection %d#, Has Delegated Transaction, waiting to Dispose.\n", ObjectID, obj.ObjectID); 
            }
            else { 
                Bid.PoolerTrace("<prov.DbConnectionPool.DestroyObject|RES|CPOOL> %d#, Connection %d#, Removing from pool.\n", ObjectID, obj.ObjectID);

                bool removed = false;
                lock (_objectList) { 
                    removed = _objectList.Remove(obj);
                    Debug.Assert(removed, "attempt to DestroyObject not in list"); 
                    _totalObjects = _objectList.Count; 
                }
 
                if (removed) {
                    Bid.PoolerTrace("<prov.DbConnectionPool.DestroyObject|RES|CPOOL> %d#, Connection %d#, Removed from pool.\n", ObjectID, obj.ObjectID);
                    PerformanceCounters.NumberOfPooledConnections.Decrement();
                } 
                obj.Dispose();
 
                Bid.PoolerTrace("<prov.DbConnectionPool.DestroyObject|RES|CPOOL> %d#, Connection %d#, Disposed.\n", ObjectID, obj.ObjectID); 
                PerformanceCounters.HardDisconnectsPerSecond.Increment();
            } 
        }

        private void ErrorCallback(Object state) {
            Bid.PoolerTrace("<prov.DbConnectionPool.ErrorCallback|RES|CPOOL> %d#, Resetting Error handling.\n", ObjectID); 

            _errorOccurred = false; 
            _waitHandles.ErrorEvent.Reset(); 
            Timer t     = _errorTimer;
            _errorTimer = null; 
            if (t != null) {
                t.Dispose(); // Cancel timer request.
            }
        } 

        internal DbConnectionInternal GetConnection(DbConnection owningObject) { 
            DbConnectionInternal obj = null; 
            SysTx.Transaction transaction = null;
 
            PerformanceCounters.SoftConnectsPerSecond.Increment();

            if(_state != State.Running) {
                Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, DbConnectionInternal State != Running.\n", ObjectID); 
                return null;
            } 
 
            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Getting connection.\n", ObjectID);
            // If automatic transaction enlistment is required, then we try to 
            // get the connection from the transacted connection pool first.
            if (HasTransactionAffinity) {
                obj = GetFromTransactedPool(out transaction);
            } 

            if (null == obj) { 
                Interlocked.Increment(ref _waitCount); 
                uint waitHandleCount = 3;
                uint timeout = (uint)CreationTimeout; 

                do {
                    int waitResult = BOGUS_HANDLE;
                    int releaseSemaphoreResult = 0; 

                    bool mustRelease = false; 
                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
                        _waitHandles.DangerousAddRef(ref mustRelease); 

                        // We absolutely must have the value of waitResult set,
                        // or we may leak the mutex in async abort cases.
                        RuntimeHelpers.PrepareConstrainedRegions(); 
                        try {
                            Debug.Assert(2 == waitHandleCount || 3 == waitHandleCount, "unexpected waithandle count"); 
                        } 
                        finally {
                            waitResult = SafeNativeMethods.WaitForMultipleObjectsEx(waitHandleCount, _waitHandles.DangerousGetHandle(), false, timeout, false); 
                        }

                        // From the WaitAny docs: "If more than one object became signaled during
                        // the call, this is the array index of the signaled object with the 
                        // smallest index value of all the signaled objects."  This is important
                        // so that the free object signal will be returned before a creation 
                        // signal. 

                        switch (waitResult) { 
                        case WAIT_TIMEOUT:
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Wait timed out.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount);
                            return null; 

                        case ERROR_HANDLE: 
                            // Throw the error that PoolCreateRequest stashed. 
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Errors are set.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount); 
                            throw _resError;

                        case CREATION_HANDLE:
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Creating new connection.\n", ObjectID); 

                            try { 
                                obj = UserCreateRequest(owningObject); 
                            }
                            catch { 
                                if (null == obj) {
                                    Interlocked.Decrement(ref _waitCount);
                                }
                                throw; 
                            }
                            finally { 
                                // 

                                if (null != obj) { 
                                    Interlocked.Decrement(ref _waitCount);
                                }
                            }
 
                            if (null == obj) {
                                // If we were not able to create an object, check to see if 
                                // we reached MaxPoolSize.  If so, we will no longer wait on 
                                // the CreationHandle, but instead wait for a free object or
                                // the timeout. 

                                //

                                if (Count >= MaxPoolSize && 0 != MaxPoolSize) { 
                                    if (!ReclaimEmancipatedObjects()) {
                                        // modify handle array not to wait on creation mutex anymore 
                                        Debug.Assert(2 == CREATION_HANDLE, "creation handle changed value"); 
                                        waitHandleCount = 2;
                                    } 
                                }
                            }
                            break;
 
                        case SEMAPHORE_HANDLE:
                            // 
                            //    guaranteed available inventory 
                            //
                            Interlocked.Decrement(ref _waitCount); 
                            obj = GetFromGeneralPool();
                            break;

                        case WAIT_FAILED: 
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Wait failed.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount); 
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error()); 
                            goto default; // if ThrowExceptionForHR didn't throw for some reason
                        case (WAIT_ABANDONED+SEMAPHORE_HANDLE): 
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Semaphore handle abandonded.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount);
                            throw new AbandonedMutexException(SEMAPHORE_HANDLE,_waitHandles.PoolSemaphore);
                        case (WAIT_ABANDONED+ERROR_HANDLE): 
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Error handle abandonded.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount); 
                            throw new AbandonedMutexException(ERROR_HANDLE,_waitHandles.ErrorEvent); 
                        case (WAIT_ABANDONED+CREATION_HANDLE):
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Creation handle abandoned.\n", ObjectID); 
                            Interlocked.Decrement(ref _waitCount);
                            throw new AbandonedMutexException(CREATION_HANDLE,_waitHandles.CreationSemaphore);
                        default:
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, WaitForMultipleObjects=%d\n", ObjectID, waitResult); 
                            Interlocked.Decrement(ref _waitCount);
                            throw ADP.InternalError(ADP.InternalErrorCode.UnexpectedWaitAnyResult); 
                        } 
                    }
                    finally { 
                        if (CREATION_HANDLE == waitResult) {
                            int result = SafeNativeMethods.ReleaseSemaphore(_waitHandles.CreationHandle.DangerousGetHandle(), 1, IntPtr.Zero);
                            if (0 == result) { // failure case
                                releaseSemaphoreResult = Marshal.GetHRForLastWin32Error(); 
                            }
                        } 
                        if (mustRelease) { 
                            _waitHandles.DangerousRelease();
                        } 
                    }
                    if (0 != releaseSemaphoreResult) {
                        Marshal.ThrowExceptionForHR(releaseSemaphoreResult); // will only throw if (hresult < 0)
                    } 
                } while (null == obj);
            } 
 
            if (null != obj) {
                lock (obj) {   // Protect against Clear and ReclaimEmancipatedObjects, which call IsEmancipated, which is affected by PrePush and PostPop 
                    obj.PostPop(owningObject);
                }
                try {
                    obj.ActivateConnection(transaction); 
                }
                catch(SecurityException) { 
                    // if Activate throws an exception 
                    // put it back in the pool or have it properly disposed of
                    this.PutObject(obj, owningObject); 
                    throw;
                }
            }
            return(obj); 
        }
 
        private DbConnectionInternal GetFromGeneralPool() { 
            DbConnectionInternal obj = null;
 
            obj = _stackNew.SynchronizedPop();
            if (null == obj) {
                obj = _stackOld.SynchronizedPop();
            } 

            // 
 

 


            if (null != obj) {
                Bid.PoolerTrace("<prov.DbConnectionPool.GetFromGeneralPool|RES|CPOOL> %d#, Connection %d#, Popped from general pool.\n", ObjectID, obj.ObjectID); 
                PerformanceCounters.NumberOfFreeConnections.Decrement();
            } 
            return(obj); 
        }
 
        private DbConnectionInternal GetFromTransactedPool(out SysTx.Transaction transaction) {
            transaction = ADP.GetCurrentTransaction();
            DbConnectionInternal obj = null;
 
            if (null != transaction && null != _transactedConnectionPool) {
                obj = _transactedConnectionPool.GetTransactedObject(transaction); 
 
                if (null != obj) {
                    Bid.PoolerTrace("<prov.DbConnectionPool.GetFromTransactedPool|RES|CPOOL> %d#, Connection %d#, Popped from transacted pool.\n", ObjectID, obj.ObjectID); 
                    PerformanceCounters.NumberOfFreeConnections.Decrement();
                }
            }
            return obj; 
        }
 
        private void PoolCreateRequest(object state) { 
            // called by pooler to ensure pool requests are currently being satisfied -
            // creation mutex has not been obtained 

            IntPtr hscp;

            Bid.PoolerScopeEnter(out hscp, "<prov.DbConnectionPool.PoolCreateRequest|RES|INFO|CPOOL> %d#\n", ObjectID); 

            try { 
                if (State.Running == _state) { 
                    // Before creating any new objects, reclaim any released objects that were
                    // not closed. 
                    ReclaimEmancipatedObjects();

                    if (!ErrorOccurred) {
                        if (NeedToReplenish) { 
                            // Check to see if pool was created using integrated security and if so, make
                            // sure the identity of current user matches that of user that created pool. 
                            // If it doesn't match, do not create any objects on the ThreadPool thread, 
                            // since either Open will fail or we will open a object for this pool that does
                            // not belong in this pool.  The side effect of this is that if using integrated 
                            // security min pool size cannot be guaranteed.
                            if (UsingIntegrateSecurity && !_identity.Equals(DbConnectionPoolIdentity.GetCurrent())) {
                                return;
                            } 
                            bool mustRelease = false;
                            int waitResult = BOGUS_HANDLE; 
                            uint timeout = (uint)CreationTimeout; 
                            RuntimeHelpers.PrepareConstrainedRegions();
                            try { 
                                _waitHandles.DangerousAddRef(ref mustRelease);

                                // Obtain creation mutex so we're the only one creating objects
                                // and we must have the wait result 
                                RuntimeHelpers.PrepareConstrainedRegions();
                                try { } finally { 
                                    waitResult = SafeNativeMethods.WaitForSingleObjectEx(_waitHandles.CreationHandle.DangerousGetHandle(), timeout, false); 
                                }
                                if (WAIT_OBJECT_0 == waitResult) { 
                                    DbConnectionInternal newObj;

                                    // Check ErrorOccurred again after obtaining mutex
                                    if (!ErrorOccurred) { 
                                        while (NeedToReplenish) {
                                            newObj = CreateObject((DbConnection)null); 
 
                                            // We do not need to check error flag here, since we know if
                                            // CreateObject returned null, we are in error case. 
                                            if (null != newObj) {
                                                PutNewObject(newObj);
                                            }
                                            else { 
                                                break;
                                            } 
                                        } 
                                    }
                                } 
                                else if (WAIT_TIMEOUT == waitResult) {
                                    // do not wait forever and potential block this worker thread
                                    // instead wait for a period of time and just requeue to try again
                                    QueuePoolCreateRequest(); 
                                }
                                else { 
                                    // trace waitResult and ignore the failure 
                                    Bid.PoolerTrace("<prov.DbConnectionPool.PoolCreateRequest|RES|CPOOL> %d#, PoolCreateRequest called WaitForSingleObject failed %d", ObjectID, waitResult);
                                } 
                            }
                            catch (Exception e) {
                                //
                                if (!ADP.IsCatchableExceptionType(e)) { 
                                    throw;
                                } 
 
                                // Now that CreateObject can throw, we need to catch the exception and `swallow it.
                                // There is no further action we can take beyond tracing.  The error will be 
                                // thrown to the user the next time they request a connection.
                                Bid.PoolerTrace("<prov.DbConnectionPool.PoolCreateRequest|RES|CPOOL> %d#, PoolCreateRequest called CreateConnection which threw an exception: " + e.ToString(), ObjectID);
                            }
                            finally { 
                                if (WAIT_OBJECT_0 == waitResult) {
                                    // reuse waitResult and ignore its value 
                                    waitResult = SafeNativeMethods.ReleaseSemaphore(_waitHandles.CreationHandle.DangerousGetHandle(), 1, IntPtr.Zero); 
                                }
                                if (mustRelease) { 
                                    _waitHandles.DangerousRelease();
                                }
                            }
                        } 
                    }
                } 
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        }

        private void ProcessDeactivateQueue(object state) { 
            IntPtr hscp;
 
            Bid.PoolerScopeEnter(out hscp, "<prov.DbConnectionPool.ProcessDeactivateQueue|RES|INFO|CPOOL> %d#\n", ObjectID); 

            try { 
                object[] deactivateQueue;
                lock (_deactivateQueue.SyncRoot) {
                    deactivateQueue = _deactivateQueue.ToArray();
                    _deactivateQueue.Clear(); 
                }
 
                foreach (DbConnectionInternal obj in deactivateQueue) { 
                    PerformanceCounters.NumberOfStasisConnections.Decrement();
                    DeactivateObject(obj); 
                }
            }
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        internal void PutNewObject(DbConnectionInternal obj) {
            Debug.Assert(null != obj,        "why are we adding a null object to the pool?"); 
            Debug.Assert(obj.CanBePooled,    "non-poolable object in pool");

            Bid.PoolerTrace("<prov.DbConnectionPool.PutNewObject|RES|CPOOL> %d#, Connection %d#, Pushing to general pool.\n", ObjectID, obj.ObjectID);
 
            _stackNew.SynchronizedPush(obj);
            _waitHandles.PoolSemaphore.Release(1); 
            PerformanceCounters.NumberOfFreeConnections.Increment(); 

        } 

        internal void PutObject(DbConnectionInternal obj, object owningObject) {
            Debug.Assert(null != obj, "null obj?");
 
            PerformanceCounters.SoftDisconnectsPerSecond.Increment();
 
            // Once a connection is closing (which is the state that we're in at 
            // this point in time) you cannot delegate a transaction to or enlist
            // a transaction in it, so we can correctly presume that if there was 
            // not a delegated or enlisted transaction to start with, that there
            // will not be a delegated or enlisted transaction once we leave the
            // lock.
 
            lock (obj) {
                // Calling PrePush prevents the object from being reclaimed 
                // once we leave the lock, because it sets _pooledCount such 
                // that it won't appear to be out of the pool.  What that
                // means, is that we're now responsible for this connection: 
                // it won't get reclaimed if we drop the ball somewhere.
                obj.PrePush(owningObject);

                // 
            }
 
            if (UseDeactivateQueue) { 
                // If we're using the DeactivateQueue, we'll just queue it up and
                // be done; all the hard work will be done on the despooler thread. 

                bool needToQueueWorkItem;

                Bid.PoolerTrace("<prov.DbConnectionPool.PutObject|RES|CPOOL> %d#, Connection %d#, Queueing for deactivation.\n", ObjectID, obj.ObjectID); 
                PerformanceCounters.NumberOfStasisConnections.Increment();
 
                lock (_deactivateQueue.SyncRoot) { 
                    needToQueueWorkItem = (0 == _deactivateQueue.Count);
                    _deactivateQueue.Enqueue(obj); 
                }
                if (needToQueueWorkItem) {
                    // Make sure we actually get around to deactivating the object
                    // and making it available again. 
                    ThreadPool.QueueUserWorkItem(_deactivateCallback, null);
                } 
            } 
            else {
                // no deactivate queue -- do the work right now. 
                DeactivateObject(obj);
            }
        }
 
        internal void PutObjectFromTransactedPool(DbConnectionInternal obj) {
            Debug.Assert(null != obj, "null pooledObject?"); 
            Debug.Assert(!obj.HasEnlistedTransaction, "pooledObject is still enlisted?"); 

            // called by the transacted connection pool , once it's removed the 
            // connection from it's list.  We put the connection back in general
            // circulation.

            // NOTE: there is no locking required here because if we're in this 
            // method, we can safely presume that the caller is the only person
            // that is using the connection, and that all pre-push logic has been 
            // done and all transactions are ended. 

            Bid.PoolerTrace("<prov.DbConnectionPool.PutObjectFromTransactedPool|RES|CPOOL> %d#, Connection %d#, Transaction has ended.\n", ObjectID, obj.ObjectID); 

            if (_state == State.Running && obj.CanBePooled) {
                PutNewObject(obj);
            } 
            else {
                DestroyObject(obj); 
                QueuePoolCreateRequest(); 
            }
        } 

        private void QueuePoolCreateRequest() {
            if (State.Running == _state) {
                // Make sure we're at quota by posting a callback to the threadpool. 
                ThreadPool.QueueUserWorkItem(_poolCreateRequest);
            } 
        } 

        private bool ReclaimEmancipatedObjects() { 
            bool emancipatedObjectFound = false;

            Bid.PoolerTrace("<prov.DbConnectionPool.ReclaimEmancipatedObjects|RES|CPOOL> %d#\n", ObjectID);
 
            List<DbConnectionInternal> reclaimedObjects = new List<DbConnectionInternal>();
            int count; 
 
            lock(_objectList) {
                count = _objectList.Count; 

                for (int i = 0; i < count; ++i) {
                    DbConnectionInternal obj = _objectList[i];
 
                    if (null != obj) {
                        bool locked = false; 
 
                        try {
                            locked = Monitor.TryEnter(obj); 

                            if (locked) { // avoid race condition with PrePush/PostPop and IsEmancipated
                                if (obj.IsEmancipated) {
                                    // Inside the lock, we want to do as little 
                                    // as possible, so we simply mark the object
                                    // as being in the pool, but hand it off to 
                                    // an out of pool list to be deactivated, 
                                    // etc.
                                    obj.PrePush(null); 
                                    reclaimedObjects.Add(obj);
                                }
                            }
                        } 
                        finally {
                            if (locked) 
                                Monitor.Exit(obj); 
                        }
                    } 
                }
            }

            // NOTE: we don't want to call DeactivateObject while we're locked, 
            // because it can make roundtrips to the server and this will block
            // object creation in the pooler.  Instead, we queue things we need 
            // to do up, and process them outside the lock. 
            count = reclaimedObjects.Count;
 
            for (int i = 0; i < count; ++i) {
                DbConnectionInternal obj = reclaimedObjects[i];

                Bid.PoolerTrace("<prov.DbConnectionPool.ReclaimEmancipatedObjects|RES|CPOOL> %d#, Connection %d#, Reclaiming.\n", ObjectID, obj.ObjectID); 
                PerformanceCounters.NumberOfReclaimedConnections.Increment();
 
                emancipatedObjectFound = true; 

                // NOTE: it is not possible for us to have a connection that has 
                // a delegated transaction at this point, because IsEmancipated
                // would not have returned true if it did, and when a connection
                // is emancipated, you can't enlist in a transaction (because you
                // can't get to it to make the call...) 
                DeactivateObject(obj);
            } 
            return emancipatedObjectFound; 
        }
 
        internal void Startup() {
            Bid.PoolerTrace("<prov.DbConnectionPool.Startup|RES|INFO|CPOOL> %d#, CleanupWait=%d\n", ObjectID, _cleanupWait);

            _cleanupTimer = CreateCleanupTimer(); 
            if (NeedToReplenish) {
                QueuePoolCreateRequest(); 
            } 
        }
 
        internal void Shutdown() {
            Bid.PoolerTrace("<prov.DbConnectionPool.Shutdown|RES|INFO|CPOOL> %d#\n", ObjectID);

            _state = State.ShuttingDown; 

            Timer t; // deactivate timer callbacks 
 
            t = _cleanupTimer;
            _cleanupTimer = null; 
            if (null != t) {
                t.Dispose();
            }
 
            t = _errorTimer;
            _errorTimer = null; 
            if (null != t) { 
                t.Dispose();
            } 
        }

        internal void TransactionBegin(SysTx.Transaction transaction) {
            TransactedConnectionPool transactedConnectionPool = _transactedConnectionPool; 
            if (null != transactedConnectionPool) {
                transactedConnectionPool.TransactionBegin(transaction); 
            } 
        }
 
        internal void TransactionEnded(SysTx.Transaction transaction, DbConnectionInternal transactedObject) {
            Debug.Assert(null != transaction, "null transaction?");
            Debug.Assert(null != transactedObject, "null transactedObject?");
            // Note: connection may still be associated with transaction due to Explicit Unbinding requirement. 

            Bid.PoolerTrace("<prov.DbConnectionPool.TransactionEnded|RES|CPOOL> %d#, Transaction %d#, Connection %d#, Transaction Completed\n", ObjectID, transaction.GetHashCode(), transactedObject.ObjectID); 
 
            // called by the internal connection when it get's told that the
            // transaction is completed.  We tell the transacted pool to remove 
            // the connection from it's list, then we put the connection back in
            // general circulation.

            TransactedConnectionPool transactedConnectionPool = _transactedConnectionPool; 
            if (null != transactedConnectionPool) {
                transactedConnectionPool.TransactionEnded(transaction, transactedObject); 
            } 
        }
 
        private DbConnectionInternal UserCreateRequest(DbConnection owningObject) {
            // called by user when they were not able to obtain a free object but
            // instead obtained creation mutex
 
            DbConnectionInternal obj = null;
            if (ErrorOccurred) { 
               throw _resError; 
            }
            else { 
                 if ((Count < MaxPoolSize) || (0 == MaxPoolSize)) {
                    // If we have an odd number of total objects, reclaim any dead objects.
                    // If we did not find any objects to reclaim, create a new one.
 
                    //
                     if ((Count & 0x1) == 0x1 || !ReclaimEmancipatedObjects()) 
                        obj = CreateObject(owningObject); 
                }
                return obj; 
            }
        }

        private class DbConnectionInternalListStack { 
            private DbConnectionInternal _stack;
#if DEBUG 
            private int _version; 
            private int _count;
#endif 
            internal DbConnectionInternalListStack() {
            }

            internal int Count { 
                get {
                    int count = 0; 
                    lock(this) { 
                        for(DbConnectionInternal x = _stack; null != x; x = x.NextPooledObject) {
                            ++count; 
                        }
                    }
#if DEBUG
                    Debug.Assert(count == _count, "count is corrupt"); 
#endif
                    return count; 
                } 
            }
 
            internal DbConnectionInternal SynchronizedPop() {
                DbConnectionInternal value;
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

            internal void SynchronizedPush(DbConnectionInternal value) {
                Debug.Assert(null != value, "pushing null value");
                lock(this) { 
#if DEBUG
                    Debug.Assert(null == value.NextPooledObject, "pushing value with non-null NextPooledObject"); 
                    int index = 0; 
                    for(DbConnectionInternal x = _stack; null != x; x = x.NextPooledObject, ++index) {
                        Debug.Assert(x != value, "double push: connection already in stack"); 
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
// <copyright file="DbConnectionPool.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.InteropServices;
    using System.Security; 
    using System.Security.Permissions;
    using System.Security.Principal;
    using System.Threading;
    using SysTx = System.Transactions; 

    sealed internal class DbConnectionPool { 
        private enum State { 
            Initializing,
            Running, 
            ShuttingDown,
        }

        internal const Bid.ApiGroup PoolerTracePoints = (Bid.ApiGroup)0x1000; 

        sealed private class TransactedConnectionPool : Hashtable { 
            DbConnectionPool _pool; 

            private static int _objectTypeCount; // Bid counter 
            internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);

            internal TransactedConnectionPool(DbConnectionPool pool) : base() {
                Debug.Assert(null != pool, "null pool?"); 

                _pool = pool; 
                Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactedConnectionPool|RES|CPOOL> %d#, Constructed for connection pool %d#\n", ObjectID, _pool.ObjectID); 
            }
 
            internal int ObjectID {
                get {
                    return _objectID;
                } 
            }
 
            internal DbConnectionPool Pool { 
                get {
                    return _pool; 
                }
            }

            internal DbConnectionInternal GetTransactedObject(SysTx.Transaction transaction) { 
                Debug.Assert(null != transaction, "null transaction?");
 
                DbConnectionInternal transactedObject = null; 

                List<DbConnectionInternal> connections = (List<DbConnectionInternal>)this[transaction]; 

                if (null != connections) {
                    lock (connections) {
                        int i = connections.Count - 1; 
                        if (0 <= i) {
                            transactedObject = connections[i]; 
                            connections.RemoveAt(i); 
                        }
                    } 
                }

                if (null != transactedObject) {
                    Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.GetTransactedObject|RES|CPOOL> %d#, Transaction %d#, Connection %d#, Popped.\n", ObjectID, transaction.GetHashCode(), transactedObject.ObjectID); 
                }
                return transactedObject; 
            } 

            internal void PutTransactedObject(SysTx.Transaction transaction, DbConnectionInternal transactedObject) { 
                Debug.Assert(null != transaction, "null transaction?");
                Debug.Assert(null != transactedObject, "null transactedObject?");

                Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.PutTransactedObject|RES|CPOOL> %d#, Transaction %d#, Connection %d#, Pushing.\n", ObjectID, transaction.GetHashCode(), transactedObject.ObjectID); 

                List<DbConnectionInternal> connections = (List<DbConnectionInternal>)this[transaction]; 
 
                // NOTE: it is possible that the connecton was put on the
                //       deactivate queue, and while it was on the queue, the 
                //       transaction ended, causing the list for the transaction
                //       to have been removed.  In that case, we can't expect
                //       the list to be here.
                if (null != connections) { 
                    lock (connections) {
                        Debug.Assert(0 > connections.IndexOf(transactedObject), "adding to pool a second time?"); 
                        connections.Add(transactedObject); 
                        Pool.PerformanceCounters.NumberOfFreeConnections.Increment();
                    } 
                }
            }

            internal void TransactionBegin(SysTx.Transaction transaction) { 
                Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionBegin|RES|CPOOL> %d#, Transaction %d#, Begin.\n", ObjectID, transaction.GetHashCode());
 
                List<DbConnectionInternal> connections = (List<DbConnectionInternal>)this[transaction]; 

                if (null == connections) { 
                    List<DbConnectionInternal> newConnections= new List<DbConnectionInternal>(2); // start with only two connections in the list; most times we won't need that many.
                    SysTx.Transaction transactionClone = null;
                    try {
                        transactionClone = transaction.Clone(); 

                        Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionBegin|RES|CPOOL> %d#, Transaction %d#, Adding List to transacted pool.\n", ObjectID, transaction.GetHashCode()); 
                        lock (this) { 
                            connections = (List<DbConnectionInternal>)this[transaction];
 
                            if (null == connections) {
                                connections = newConnections;
                                this.Add(transactionClone, connections);
                                transactionClone = null; // we've used it -- don't throw it away. 
                            }
                        } 
                    } 
                    finally {
                        if (null != transactionClone) { 
                            transactionClone.Dispose();
                        }
                    }
                    newConnections = null; 
                    Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionBegin|RES|CPOOL> %d#, Transaction %d#, Added.\n", ObjectID, transaction.GetHashCode());
                } 
            } 

            internal void TransactionEnded(SysTx.Transaction transaction, DbConnectionInternal transactedObject) { 
                Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionEnded|RES|CPOOL> %d#, Transaction %d#, Connection %d#, Transaction Completed\n", ObjectID, transaction.GetHashCode(), transactedObject.ObjectID);

                List<DbConnectionInternal> connections = (List<DbConnectionInternal>)this[transaction];
                int entry = -1; 

                // NOTE: we may be ending a transaction for a connection that is 
                //       currently not in the pool, and therefore it may not have 
                //       a list for it, because it may have been removed already.
                if (null != connections) { 
                    lock (connections) {
                        entry = connections.IndexOf(transactedObject);

                        if (entry >= 0) { 
                            connections.RemoveAt(entry);
                        } 
 
                        // Once we've completed all the ended notifications, we can
                        // safely remove the list from the transacted pool. 
                        if (0 >= connections.Count) {
                            Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionEnded|RES|CPOOL> %d#, Transaction %d#, Removing List from transacted pool.\n", ObjectID, transaction.GetHashCode());
                            lock (this) {
                                Remove(transaction); 
                            }
                            Bid.PoolerTrace("<prov.DbConnectionPool.TransactedConnectionPool.TransactionEnded|RES|CPOOL> %d#, Transaction %d#, Removed.\n", ObjectID, transaction.GetHashCode()); 
 
                            // we really need to dispose our clone; it may have
                            // native resources and GC may not happen soon enough. 
                            transaction.Dispose();
                        }
                    }
                } 

                // If (and only if) we found the connection in the list of 
                // connections, we'll put it back... 
                if (0 <= entry)  {
                    Pool.PerformanceCounters.NumberOfFreeConnections.Decrement(); 
                    Pool.PutObjectFromTransactedPool(transactedObject);
                }
            }
        } 

        private sealed class PoolWaitHandles : DbBuffer { 
 
            private readonly Semaphore _poolSemaphore;
            private readonly ManualResetEvent _errorEvent; 

            // Using a Mutex requires ThreadAffinity because SQL CLR can swap
            // the underlying Win32 thread associated with a managed thread in preemptive mode.
            // Using an AutoResetEvent does not have that complication. 
            private readonly Semaphore _creationSemaphore;
 
            private readonly SafeHandle _poolHandle; 
            private readonly SafeHandle _errorHandle;
            private readonly SafeHandle _creationHandle; 

            private readonly int _releaseFlags;

            internal PoolWaitHandles(Semaphore poolSemaphore, ManualResetEvent errorEvent, Semaphore creationSemaphore) : base(3*IntPtr.Size) { 
                bool mustRelease1 = false, mustRelease2 = false, mustRelease3 = false;
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try { 
                    _poolSemaphore     = poolSemaphore;
                    _errorEvent        = errorEvent; 
                    _creationSemaphore = creationSemaphore;

                    // because SafeWaitHandle doesn't have reliability contract
                    _poolHandle     = poolSemaphore.SafeWaitHandle; 
                    _errorHandle    = errorEvent.SafeWaitHandle;
                    _creationHandle = creationSemaphore.SafeWaitHandle; 
 
                    _poolHandle.DangerousAddRef(ref mustRelease1);
                    _errorHandle.DangerousAddRef(ref mustRelease2); 
                    _creationHandle.DangerousAddRef(ref mustRelease3);

                    Debug.Assert(0 == SEMAPHORE_HANDLE, "SEMAPHORE_HANDLE");
                    Debug.Assert(1 == ERROR_HANDLE, "ERROR_HANDLE"); 
                    Debug.Assert(2 == CREATION_HANDLE, "CREATION_HANDLE");
 
                    WriteIntPtr(SEMAPHORE_HANDLE*IntPtr.Size, _poolHandle.DangerousGetHandle()); 
                    WriteIntPtr(ERROR_HANDLE*IntPtr.Size,     _errorHandle.DangerousGetHandle());
                    WriteIntPtr(CREATION_HANDLE*IntPtr.Size,  _creationHandle.DangerousGetHandle()); 
                }
                finally {
                    if (mustRelease1) {
                        _releaseFlags |= 1; 
                    }
                    if (mustRelease2) { 
                        _releaseFlags |= 2; 
                    }
                    if (mustRelease3) { 
                        _releaseFlags |= 4;
                    }
                }
            } 

            internal SafeHandle CreationHandle { 
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
                get { return _creationHandle; }
            } 

            internal Semaphore CreationSemaphore {
                get { return _creationSemaphore; }
            } 

            internal ManualResetEvent ErrorEvent { 
                get { return _errorEvent; } 
            }
 
            internal Semaphore PoolSemaphore {
                get { return _poolSemaphore; }
            }
 
            protected override bool ReleaseHandle() {
                // NOTE: The SafeHandle class guarantees this will be called exactly once. 
                // we know we can touch these other managed objects because of our original DangerousAddRef 
                if (0 != (1 & _releaseFlags)) {
                    _poolHandle.DangerousRelease(); 
                }
                if (0 != (2 & _releaseFlags)) {
                    _errorHandle.DangerousRelease();
                } 
                if (0 != (4 & _releaseFlags)) {
                    _creationHandle.DangerousRelease(); 
                } 
                return base.ReleaseHandle();
            } 
        }

        private const int MAX_Q_SIZE    = (int)0x00100000;
 
        // The order of these is important; we want the WaitAny call to be signaled
        // for a free object before a creation signal.  Only the index first signaled 
        // object is returned from the WaitAny call. 
        private const int SEMAPHORE_HANDLE = (int)0x0;
        private const int ERROR_HANDLE     = (int)0x1; 
        private const int CREATION_HANDLE  = (int)0x2;
        private const int BOGUS_HANDLE     = (int)0x3;

        private const int WAIT_OBJECT_0 = 0; 
        private const int WAIT_TIMEOUT   = (int)0x102;
        private const int WAIT_ABANDONED = (int)0x80; 
        private const int WAIT_FAILED    = -1; 

        private const int ERROR_WAIT_DEFAULT = 5 * 1000; // 5 seconds 

        // we do want a testable, repeatable set of generated random numbers
        private static readonly Random _random = new Random(5101977); // Value obtained from Dave Driver
 
        private readonly int              _cleanupWait;
        private readonly DbConnectionPoolIdentity _identity; 
 
        private readonly DbConnectionFactory          _connectionFactory;
        private readonly DbConnectionPoolGroup        _connectionPoolGroup; 
        private readonly DbConnectionPoolGroupOptions _connectionPoolGroupOptions;
        private          DbConnectionPoolProviderInfo _connectionPoolProviderInfo;

        private State                     _state; 

        private readonly DbConnectionInternalListStack _stackOld = new DbConnectionInternalListStack(); 
        private readonly DbConnectionInternalListStack _stackNew = new DbConnectionInternalListStack(); 

        private readonly WaitCallback     _poolCreateRequest; 

        private readonly Queue            _deactivateQueue;
        private readonly WaitCallback     _deactivateCallback;
 
        private int                       _waitCount;
        private readonly PoolWaitHandles  _waitHandles; 
 
        private Exception                 _resError;
        private volatile bool             _errorOccurred; 

        private int                       _errorWait;
        private Timer                     _errorTimer;
 
        private Timer                     _cleanupTimer;
 
        private readonly TransactedConnectionPool _transactedConnectionPool; 

        private readonly List<DbConnectionInternal> _objectList; 
        private int                       _totalObjects;

        private static int _objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount); 

        // only created by DbConnectionPoolGroup.GetConnectionPool 
        internal DbConnectionPool( 
                            DbConnectionFactory connectionFactory,
                            DbConnectionPoolGroup connectionPoolGroup, 
                            DbConnectionPoolIdentity identity,
                            DbConnectionPoolProviderInfo connectionPoolProviderInfo ) {
            Debug.Assert(ADP.IsWindowsNT, "Attempting to construct a connection pool on Win9x?");
            Debug.Assert(null != connectionPoolGroup, "null connectionPoolGroup"); 

            if ((null != identity) && identity.IsRestricted) { 
                throw ADP.InternalError(ADP.InternalErrorCode.AttemptingToPoolOnRestrictedToken); 
            }
 
            _state= State.Initializing;

            lock(_random) { // Random.Next is not thread-safe
                _cleanupWait = _random.Next(12, 24)*10*1000; // 2-4 minutes in 10 sec intervals, WebData 103603 
            }
 
            _connectionFactory = connectionFactory; 
            _connectionPoolGroup = connectionPoolGroup;
            _connectionPoolGroupOptions = connectionPoolGroup.PoolGroupOptions; 
            _connectionPoolProviderInfo = connectionPoolProviderInfo;
            _identity = identity;

            if (UseDeactivateQueue) { 
                _deactivateQueue = new Queue();
                _deactivateCallback = new WaitCallback(ProcessDeactivateQueue); 
            } 

            _waitHandles = new PoolWaitHandles( 
                                new Semaphore(0, MAX_Q_SIZE),
                                new ManualResetEvent(false),
                                new Semaphore(1, 1));
 
            _errorWait      = ERROR_WAIT_DEFAULT;
            _errorTimer     = null;  // No error yet. 
 
            _objectList     = new List<DbConnectionInternal>(MaxPoolSize);
 
            if(ADP.IsPlatformNT5) {
                _transactedConnectionPool = new TransactedConnectionPool(this);
            }
 
            _poolCreateRequest = new WaitCallback(PoolCreateRequest); // used by CleanupCallback
            _state = State.Running; 
 
            Bid.PoolerTrace("<prov.DbConnectionPool.DbConnectionPool|RES|CPOOL> %d#, Constructed.\n", ObjectID);
 
            //_cleanupTimer & QueuePoolCreateRequest is delayed until DbConnectionPoolGroup calls
            // StartBackgroundCallbacks after pool is actually in the collection
        }
 
        private int CreationTimeout {
            get { return PoolGroupOptions.CreationTimeout; } 
        } 

        internal int Count { 
            get { return _totalObjects; }
        }

        internal DbConnectionFactory ConnectionFactory { 
            get { return _connectionFactory; }
        } 
 
        internal bool ErrorOccurred {
            get { return _errorOccurred; } 
        }

        private bool HasTransactionAffinity {
            get { return PoolGroupOptions.HasTransactionAffinity; } 
        }
 
        internal TimeSpan LoadBalanceTimeout { 
            get { return PoolGroupOptions.LoadBalanceTimeout; }
        } 

        private bool NeedToReplenish {
            get {
                if (State.Running != _state) // SQL BU DT 364595 - don't allow connection create when not running. 
                    return false;
 
                int totalObjects = Count; 

                if (totalObjects >= MaxPoolSize) 
                    return false;

                if (totalObjects < MinPoolSize)
                    return true; 

                int freeObjects        = (_stackNew.Count + _stackOld.Count); 
                int waitingRequests    = _waitCount; 
                bool needToReplenish = (freeObjects < waitingRequests) || ((freeObjects == waitingRequests) && (totalObjects > 1));
 
                return needToReplenish;
            }
        }
 
        internal DbConnectionPoolIdentity Identity {
            get { return _identity; } 
        } 

        private int MaxPoolSize { 
            get { return PoolGroupOptions.MaxPoolSize; }
        }

        private int MinPoolSize { 
            get { return PoolGroupOptions.MinPoolSize; }
        } 
 
        internal int ObjectID {
            get { 
                return _objectID;
            }
        }
 
        internal DbConnectionPoolCounters PerformanceCounters {
            get { return _connectionFactory.PerformanceCounters; } 
        } 

        internal DbConnectionPoolGroup PoolGroup { 
            get { return _connectionPoolGroup; }
        }

        internal DbConnectionPoolGroupOptions PoolGroupOptions { 
            get { return _connectionPoolGroupOptions; }
        } 
 
        internal DbConnectionPoolProviderInfo ProviderInfo {
            get { return _connectionPoolProviderInfo; } 
        }

        private bool UseDeactivateQueue {
            get { return PoolGroupOptions.UseDeactivateQueue; } 
        }
 
        internal bool UseLoadBalancing { 
            get { return PoolGroupOptions.UseLoadBalancing; }
        } 

        private bool UsingIntegrateSecurity {
            get { return (null != _identity && DbConnectionPoolIdentity.NoIdentity != _identity); }
        } 

        private void CleanupCallback(Object state) { 
            // Called when the cleanup-timer ticks over. 

            // This is the automatic prunning method.  Every period, we will 
            // perform a two-step process:
            //
            // First, for each free object above MinPoolSize, we will obtain a
            // semaphore representing one object and destroy one from old stack. 
            // We will continue this until we either reach MinPoolSize, we are
            // unable to obtain a free object, or we have exhausted all the 
            // objects on the old stack. 
            //
            // Second we move all free objects on the new stack to the old stack. 
            // So, every period the objects on the old stack are destroyed and
            // the objects on the new stack are pushed to the old stack.  All
            // objects that are currently out and in use are not on either stack.
            // 
            // With this logic, objects are pruned from the pool if unused for
            // at least one period but not more than two periods. 
 
            Bid.PoolerTrace("<prov.DbConnectionPool.CleanupCallback|RES|INFO|CPOOL> %d#\n", ObjectID);
 
            // Destroy free objects that put us above MinPoolSize from old stack.
            while(Count > MinPoolSize) { // While above MinPoolSize...

                if (_waitHandles.PoolSemaphore.WaitOne(0, false) /* != WAIT_TIMEOUT */) { 
                    // We obtained a objects from the semaphore.
                    DbConnectionInternal obj = _stackOld.SynchronizedPop(); 
 
                    if (null != obj) {
                        // If we obtained one from the old stack, destroy it. 
                        PerformanceCounters.NumberOfFreeConnections.Decrement();

                        // Transaction roots must survive even aging out (TxEnd event will clean them up).
                        bool shouldDestroy = true; 
                        lock (obj) {    // Lock to prevent race window between IsTransactionRoot and shouldDestroy assignment
                            if (obj.IsTransactionRoot) { 
                                shouldDestroy = false; 
                            }
                        } 

                        // !!!!!!!!!! WARNING !!!!!!!!!!!!!
                        //   ONLY touch obj after lock release if shouldDestroy is false!!!  Otherwise, it may be destroyed
                        //   by transaction-end thread! 

                        // Note that there is a minor race condition between this task and the transaction end event, if the latter runs 
                        //  between the lock above and the SetInStasis call below. The reslult is that the stasis counter may be 
                        //  incremented without a corresponding decrement (the transaction end task is normally expected
                        //  to decrement, but will only do so if the stasis flag is set when it runs). I've minimized the size 
                        //  of the window, but we aren't totally eliminating it due to SetInStasis needing to do bid tracing, which
                        //  we don't want to do under this lock, if possible. It should be possible to eliminate this race with
                        //  more substantial re-architecture of the pool, but we don't have the time to do that work for the current release.
 
                        if (shouldDestroy) {
                            DestroyObject(obj); 
                        } 
                        else {
                            obj.SetInStasis(); 
                        }
                    }
                    else {
                        // Else we exhausted the old stack (the object the 
                        // semaphore represents is on the new stack), so break.
                        _waitHandles.PoolSemaphore.Release(1); 
                        break; 
                    }
                } 
                else {
                    break;
                }
            } 

            // Push to the old-stack.  For each free object, move object from 
            // new stack to old stack. 
            if(_waitHandles.PoolSemaphore.WaitOne(0, false) /* != WAIT_TIMEOUT */) {
                for(;;) { 
                    DbConnectionInternal obj = _stackNew.SynchronizedPop();

                    if (null == obj)
                        break; 

                    Bid.PoolerTrace("<prov.DbConnectionPool.CleanupCallback|RES|INFO|CPOOL> %d#, ChangeStacks=%d#\n", ObjectID, obj.ObjectID); 
 
                    Debug.Assert(!obj.IsEmancipated, "pooled object not in pool");
                    Debug.Assert(obj.CanBePooled,     "pooled object is not poolable"); 

                    _stackOld.SynchronizedPush(obj);
                }
                _waitHandles.PoolSemaphore.Release(1); 
            }
 
            // Queue up a request to bring us up to MinPoolSize 
            QueuePoolCreateRequest();
        } 

        internal void Clear() {
            Bid.PoolerTrace("<prov.DbConnectionPool.Clear|RES|CPOOL> %d#, Clearing.\n", ObjectID);
 
            DbConnectionInternal obj;
 
            // First, quickly doom everything. 
            lock(_objectList) {
                int count = _objectList.Count; 

                for (int i = 0; i < count; ++i) {
                    obj = _objectList[i];
 
                    if (null != obj) {
                        obj.DoNotPoolThisConnection(); 
                    } 
                }
            } 

            // Second, dispose of all the free connections.
            while (null != (obj = _stackNew.SynchronizedPop())) {
                PerformanceCounters.NumberOfFreeConnections.Decrement(); 
                DestroyObject(obj);
            } 
            while (null != (obj = _stackOld.SynchronizedPop())) { 
                PerformanceCounters.NumberOfFreeConnections.Decrement();
                DestroyObject(obj); 
            }

            // Finally, reclaim everything that's emancipated (which, because
            // it's been doomed, will cause it to be disposed of as well) 
            ReclaimEmancipatedObjects();
 
            Bid.PoolerTrace("<prov.DbConnectionPool.Clear|RES|CPOOL> %d#, Cleared.\n", ObjectID); 
        }
 
        private Timer CreateCleanupTimer() {
            return (new Timer(new TimerCallback(this.CleanupCallback), null, _cleanupWait, _cleanupWait));
        }
 
        private DbConnectionInternal CreateObject(DbConnection owningObject) {
            DbConnectionInternal newObj = null; 
 
            try {
                newObj = _connectionFactory.CreatePooledConnection(owningObject, this, _connectionPoolGroup.ConnectionOptions); 
                if (null == newObj) {
                    throw ADP.InternalError(ADP.InternalErrorCode.CreateObjectReturnedNull);    // CreateObject succeeded, but null object
                }
                if (!newObj.CanBePooled) { 
                    throw ADP.InternalError(ADP.InternalErrorCode.NewObjectCannotBePooled);        // CreateObject succeeded, but non-poolable object
                } 
                newObj.PrePush(null); 

                lock (_objectList) { 
                    _objectList.Add(newObj);
                    _totalObjects = _objectList.Count;
                    PerformanceCounters.NumberOfPooledConnections.Increment();   //
                } 
                Bid.PoolerTrace("<prov.DbConnectionPool.CreateObject|RES|CPOOL> %d#, Connection %d#, Added to pool.\n", ObjectID, newObj.ObjectID);
 
                // Reset the error wait: 
                _errorWait = ERROR_WAIT_DEFAULT;
            } 
            catch(Exception e)  {
                //
                if (!ADP.IsCatchableExceptionType(e)) {
                    throw; 
                }
 
                ADP.TraceExceptionForCapture(e); 

                newObj = null; // set to null, so we do not return bad new object 
                // Failed to create instance
                _resError = e;
                _waitHandles.ErrorEvent.Set();
                   _errorOccurred = true; 
                _errorTimer = new Timer(new TimerCallback(this.ErrorCallback), null, _errorWait, _errorWait);
 
                if (30000 < _errorWait) { 
                    _errorWait = 60000;
                } 
                else {
                    _errorWait *= 2;
                }
                throw; 
            }
            return newObj; 
        } 

        private void DeactivateObject(DbConnectionInternal obj) { 
            Bid.PoolerTrace("<prov.DbConnectionPool.DeactivateObject|RES|CPOOL> %d#, Connection %d#, Deactivating.\n", ObjectID, obj.ObjectID);

            obj.DeactivateConnection(); // we presume this operation is safe outside of a lock...
 
            if ((State.Running == _state) && !obj.IsConnectionDoomed) {
                bool returnToGeneralPool = true; 
 
                lock (obj) {
                    // A connection with a delegated transaction cannot currently 
                    // be returned to a different customer until the transaction
                    // actually completes, so we send it into Stasis -- the SysTx
                    // transaction object will ensure that it is owned (not lost),
                    // and it will be certain to put it back into the pool. 
                    if (obj.IsNonPoolableTransactionRoot) {
                        obj.SetInStasis(); 
                        returnToGeneralPool = false; 
                    }
                    else { 
                        // We must put this connection into the transacted pool
                        // while inside a lock to prevent a race condition with
                        // the transaction asyncronously completing on a second
                        // thread. 
                        SysTx.Transaction transaction = obj.EnlistedTransaction;
                        if (null != transaction) { 
                            TransactionBegin(transaction);  // Delayed creation of transacted pool 
                            _transactedConnectionPool.PutTransactedObject(transaction, obj);
                            returnToGeneralPool = false; 
                        }
                    }
                }
 
                // Only push the connection into the general pool if we didn't
                // already push it onto the transacted pool. 
                if (returnToGeneralPool) { 
                    PutNewObject(obj);
                } 
            }
            else {
                // the object is not fit for reuse -- just dispose of it.
                DestroyObject(obj); 
                QueuePoolCreateRequest();
            } 
        } 

        private void DestroyObject(DbConnectionInternal obj) { 
            // A connection with a delegated transaction cannot be disposed of
            // until the delegated transaction has actually completed.  Instead,
            // we simply leave it alone; when the transaction completes, it will
            // come back through PutObjectFromTransactedPool, which will call us 
            // again.
            if (obj.IsTxRootWaitingForTxEnd) { 
                Bid.PoolerTrace("<prov.DbConnectionPool.DestroyObject|RES|CPOOL> %d#, Connection %d#, Has Delegated Transaction, waiting to Dispose.\n", ObjectID, obj.ObjectID); 
            }
            else { 
                Bid.PoolerTrace("<prov.DbConnectionPool.DestroyObject|RES|CPOOL> %d#, Connection %d#, Removing from pool.\n", ObjectID, obj.ObjectID);

                bool removed = false;
                lock (_objectList) { 
                    removed = _objectList.Remove(obj);
                    Debug.Assert(removed, "attempt to DestroyObject not in list"); 
                    _totalObjects = _objectList.Count; 
                }
 
                if (removed) {
                    Bid.PoolerTrace("<prov.DbConnectionPool.DestroyObject|RES|CPOOL> %d#, Connection %d#, Removed from pool.\n", ObjectID, obj.ObjectID);
                    PerformanceCounters.NumberOfPooledConnections.Decrement();
                } 
                obj.Dispose();
 
                Bid.PoolerTrace("<prov.DbConnectionPool.DestroyObject|RES|CPOOL> %d#, Connection %d#, Disposed.\n", ObjectID, obj.ObjectID); 
                PerformanceCounters.HardDisconnectsPerSecond.Increment();
            } 
        }

        private void ErrorCallback(Object state) {
            Bid.PoolerTrace("<prov.DbConnectionPool.ErrorCallback|RES|CPOOL> %d#, Resetting Error handling.\n", ObjectID); 

            _errorOccurred = false; 
            _waitHandles.ErrorEvent.Reset(); 
            Timer t     = _errorTimer;
            _errorTimer = null; 
            if (t != null) {
                t.Dispose(); // Cancel timer request.
            }
        } 

        internal DbConnectionInternal GetConnection(DbConnection owningObject) { 
            DbConnectionInternal obj = null; 
            SysTx.Transaction transaction = null;
 
            PerformanceCounters.SoftConnectsPerSecond.Increment();

            if(_state != State.Running) {
                Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, DbConnectionInternal State != Running.\n", ObjectID); 
                return null;
            } 
 
            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Getting connection.\n", ObjectID);
            // If automatic transaction enlistment is required, then we try to 
            // get the connection from the transacted connection pool first.
            if (HasTransactionAffinity) {
                obj = GetFromTransactedPool(out transaction);
            } 

            if (null == obj) { 
                Interlocked.Increment(ref _waitCount); 
                uint waitHandleCount = 3;
                uint timeout = (uint)CreationTimeout; 

                do {
                    int waitResult = BOGUS_HANDLE;
                    int releaseSemaphoreResult = 0; 

                    bool mustRelease = false; 
                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
                        _waitHandles.DangerousAddRef(ref mustRelease); 

                        // We absolutely must have the value of waitResult set,
                        // or we may leak the mutex in async abort cases.
                        RuntimeHelpers.PrepareConstrainedRegions(); 
                        try {
                            Debug.Assert(2 == waitHandleCount || 3 == waitHandleCount, "unexpected waithandle count"); 
                        } 
                        finally {
                            waitResult = SafeNativeMethods.WaitForMultipleObjectsEx(waitHandleCount, _waitHandles.DangerousGetHandle(), false, timeout, false); 
                        }

                        // From the WaitAny docs: "If more than one object became signaled during
                        // the call, this is the array index of the signaled object with the 
                        // smallest index value of all the signaled objects."  This is important
                        // so that the free object signal will be returned before a creation 
                        // signal. 

                        switch (waitResult) { 
                        case WAIT_TIMEOUT:
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Wait timed out.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount);
                            return null; 

                        case ERROR_HANDLE: 
                            // Throw the error that PoolCreateRequest stashed. 
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Errors are set.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount); 
                            throw _resError;

                        case CREATION_HANDLE:
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Creating new connection.\n", ObjectID); 

                            try { 
                                obj = UserCreateRequest(owningObject); 
                            }
                            catch { 
                                if (null == obj) {
                                    Interlocked.Decrement(ref _waitCount);
                                }
                                throw; 
                            }
                            finally { 
                                // 

                                if (null != obj) { 
                                    Interlocked.Decrement(ref _waitCount);
                                }
                            }
 
                            if (null == obj) {
                                // If we were not able to create an object, check to see if 
                                // we reached MaxPoolSize.  If so, we will no longer wait on 
                                // the CreationHandle, but instead wait for a free object or
                                // the timeout. 

                                //

                                if (Count >= MaxPoolSize && 0 != MaxPoolSize) { 
                                    if (!ReclaimEmancipatedObjects()) {
                                        // modify handle array not to wait on creation mutex anymore 
                                        Debug.Assert(2 == CREATION_HANDLE, "creation handle changed value"); 
                                        waitHandleCount = 2;
                                    } 
                                }
                            }
                            break;
 
                        case SEMAPHORE_HANDLE:
                            // 
                            //    guaranteed available inventory 
                            //
                            Interlocked.Decrement(ref _waitCount); 
                            obj = GetFromGeneralPool();
                            break;

                        case WAIT_FAILED: 
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Wait failed.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount); 
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error()); 
                            goto default; // if ThrowExceptionForHR didn't throw for some reason
                        case (WAIT_ABANDONED+SEMAPHORE_HANDLE): 
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Semaphore handle abandonded.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount);
                            throw new AbandonedMutexException(SEMAPHORE_HANDLE,_waitHandles.PoolSemaphore);
                        case (WAIT_ABANDONED+ERROR_HANDLE): 
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Error handle abandonded.\n", ObjectID);
                            Interlocked.Decrement(ref _waitCount); 
                            throw new AbandonedMutexException(ERROR_HANDLE,_waitHandles.ErrorEvent); 
                        case (WAIT_ABANDONED+CREATION_HANDLE):
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, Creation handle abandoned.\n", ObjectID); 
                            Interlocked.Decrement(ref _waitCount);
                            throw new AbandonedMutexException(CREATION_HANDLE,_waitHandles.CreationSemaphore);
                        default:
                            Bid.PoolerTrace("<prov.DbConnectionPool.GetConnection|RES|CPOOL> %d#, WaitForMultipleObjects=%d\n", ObjectID, waitResult); 
                            Interlocked.Decrement(ref _waitCount);
                            throw ADP.InternalError(ADP.InternalErrorCode.UnexpectedWaitAnyResult); 
                        } 
                    }
                    finally { 
                        if (CREATION_HANDLE == waitResult) {
                            int result = SafeNativeMethods.ReleaseSemaphore(_waitHandles.CreationHandle.DangerousGetHandle(), 1, IntPtr.Zero);
                            if (0 == result) { // failure case
                                releaseSemaphoreResult = Marshal.GetHRForLastWin32Error(); 
                            }
                        } 
                        if (mustRelease) { 
                            _waitHandles.DangerousRelease();
                        } 
                    }
                    if (0 != releaseSemaphoreResult) {
                        Marshal.ThrowExceptionForHR(releaseSemaphoreResult); // will only throw if (hresult < 0)
                    } 
                } while (null == obj);
            } 
 
            if (null != obj) {
                lock (obj) {   // Protect against Clear and ReclaimEmancipatedObjects, which call IsEmancipated, which is affected by PrePush and PostPop 
                    obj.PostPop(owningObject);
                }
                try {
                    obj.ActivateConnection(transaction); 
                }
                catch(SecurityException) { 
                    // if Activate throws an exception 
                    // put it back in the pool or have it properly disposed of
                    this.PutObject(obj, owningObject); 
                    throw;
                }
            }
            return(obj); 
        }
 
        private DbConnectionInternal GetFromGeneralPool() { 
            DbConnectionInternal obj = null;
 
            obj = _stackNew.SynchronizedPop();
            if (null == obj) {
                obj = _stackOld.SynchronizedPop();
            } 

            // 
 

 


            if (null != obj) {
                Bid.PoolerTrace("<prov.DbConnectionPool.GetFromGeneralPool|RES|CPOOL> %d#, Connection %d#, Popped from general pool.\n", ObjectID, obj.ObjectID); 
                PerformanceCounters.NumberOfFreeConnections.Decrement();
            } 
            return(obj); 
        }
 
        private DbConnectionInternal GetFromTransactedPool(out SysTx.Transaction transaction) {
            transaction = ADP.GetCurrentTransaction();
            DbConnectionInternal obj = null;
 
            if (null != transaction && null != _transactedConnectionPool) {
                obj = _transactedConnectionPool.GetTransactedObject(transaction); 
 
                if (null != obj) {
                    Bid.PoolerTrace("<prov.DbConnectionPool.GetFromTransactedPool|RES|CPOOL> %d#, Connection %d#, Popped from transacted pool.\n", ObjectID, obj.ObjectID); 
                    PerformanceCounters.NumberOfFreeConnections.Decrement();
                }
            }
            return obj; 
        }
 
        private void PoolCreateRequest(object state) { 
            // called by pooler to ensure pool requests are currently being satisfied -
            // creation mutex has not been obtained 

            IntPtr hscp;

            Bid.PoolerScopeEnter(out hscp, "<prov.DbConnectionPool.PoolCreateRequest|RES|INFO|CPOOL> %d#\n", ObjectID); 

            try { 
                if (State.Running == _state) { 
                    // Before creating any new objects, reclaim any released objects that were
                    // not closed. 
                    ReclaimEmancipatedObjects();

                    if (!ErrorOccurred) {
                        if (NeedToReplenish) { 
                            // Check to see if pool was created using integrated security and if so, make
                            // sure the identity of current user matches that of user that created pool. 
                            // If it doesn't match, do not create any objects on the ThreadPool thread, 
                            // since either Open will fail or we will open a object for this pool that does
                            // not belong in this pool.  The side effect of this is that if using integrated 
                            // security min pool size cannot be guaranteed.
                            if (UsingIntegrateSecurity && !_identity.Equals(DbConnectionPoolIdentity.GetCurrent())) {
                                return;
                            } 
                            bool mustRelease = false;
                            int waitResult = BOGUS_HANDLE; 
                            uint timeout = (uint)CreationTimeout; 
                            RuntimeHelpers.PrepareConstrainedRegions();
                            try { 
                                _waitHandles.DangerousAddRef(ref mustRelease);

                                // Obtain creation mutex so we're the only one creating objects
                                // and we must have the wait result 
                                RuntimeHelpers.PrepareConstrainedRegions();
                                try { } finally { 
                                    waitResult = SafeNativeMethods.WaitForSingleObjectEx(_waitHandles.CreationHandle.DangerousGetHandle(), timeout, false); 
                                }
                                if (WAIT_OBJECT_0 == waitResult) { 
                                    DbConnectionInternal newObj;

                                    // Check ErrorOccurred again after obtaining mutex
                                    if (!ErrorOccurred) { 
                                        while (NeedToReplenish) {
                                            newObj = CreateObject((DbConnection)null); 
 
                                            // We do not need to check error flag here, since we know if
                                            // CreateObject returned null, we are in error case. 
                                            if (null != newObj) {
                                                PutNewObject(newObj);
                                            }
                                            else { 
                                                break;
                                            } 
                                        } 
                                    }
                                } 
                                else if (WAIT_TIMEOUT == waitResult) {
                                    // do not wait forever and potential block this worker thread
                                    // instead wait for a period of time and just requeue to try again
                                    QueuePoolCreateRequest(); 
                                }
                                else { 
                                    // trace waitResult and ignore the failure 
                                    Bid.PoolerTrace("<prov.DbConnectionPool.PoolCreateRequest|RES|CPOOL> %d#, PoolCreateRequest called WaitForSingleObject failed %d", ObjectID, waitResult);
                                } 
                            }
                            catch (Exception e) {
                                //
                                if (!ADP.IsCatchableExceptionType(e)) { 
                                    throw;
                                } 
 
                                // Now that CreateObject can throw, we need to catch the exception and `swallow it.
                                // There is no further action we can take beyond tracing.  The error will be 
                                // thrown to the user the next time they request a connection.
                                Bid.PoolerTrace("<prov.DbConnectionPool.PoolCreateRequest|RES|CPOOL> %d#, PoolCreateRequest called CreateConnection which threw an exception: " + e.ToString(), ObjectID);
                            }
                            finally { 
                                if (WAIT_OBJECT_0 == waitResult) {
                                    // reuse waitResult and ignore its value 
                                    waitResult = SafeNativeMethods.ReleaseSemaphore(_waitHandles.CreationHandle.DangerousGetHandle(), 1, IntPtr.Zero); 
                                }
                                if (mustRelease) { 
                                    _waitHandles.DangerousRelease();
                                }
                            }
                        } 
                    }
                } 
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        }

        private void ProcessDeactivateQueue(object state) { 
            IntPtr hscp;
 
            Bid.PoolerScopeEnter(out hscp, "<prov.DbConnectionPool.ProcessDeactivateQueue|RES|INFO|CPOOL> %d#\n", ObjectID); 

            try { 
                object[] deactivateQueue;
                lock (_deactivateQueue.SyncRoot) {
                    deactivateQueue = _deactivateQueue.ToArray();
                    _deactivateQueue.Clear(); 
                }
 
                foreach (DbConnectionInternal obj in deactivateQueue) { 
                    PerformanceCounters.NumberOfStasisConnections.Decrement();
                    DeactivateObject(obj); 
                }
            }
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        internal void PutNewObject(DbConnectionInternal obj) {
            Debug.Assert(null != obj,        "why are we adding a null object to the pool?"); 
            Debug.Assert(obj.CanBePooled,    "non-poolable object in pool");

            Bid.PoolerTrace("<prov.DbConnectionPool.PutNewObject|RES|CPOOL> %d#, Connection %d#, Pushing to general pool.\n", ObjectID, obj.ObjectID);
 
            _stackNew.SynchronizedPush(obj);
            _waitHandles.PoolSemaphore.Release(1); 
            PerformanceCounters.NumberOfFreeConnections.Increment(); 

        } 

        internal void PutObject(DbConnectionInternal obj, object owningObject) {
            Debug.Assert(null != obj, "null obj?");
 
            PerformanceCounters.SoftDisconnectsPerSecond.Increment();
 
            // Once a connection is closing (which is the state that we're in at 
            // this point in time) you cannot delegate a transaction to or enlist
            // a transaction in it, so we can correctly presume that if there was 
            // not a delegated or enlisted transaction to start with, that there
            // will not be a delegated or enlisted transaction once we leave the
            // lock.
 
            lock (obj) {
                // Calling PrePush prevents the object from being reclaimed 
                // once we leave the lock, because it sets _pooledCount such 
                // that it won't appear to be out of the pool.  What that
                // means, is that we're now responsible for this connection: 
                // it won't get reclaimed if we drop the ball somewhere.
                obj.PrePush(owningObject);

                // 
            }
 
            if (UseDeactivateQueue) { 
                // If we're using the DeactivateQueue, we'll just queue it up and
                // be done; all the hard work will be done on the despooler thread. 

                bool needToQueueWorkItem;

                Bid.PoolerTrace("<prov.DbConnectionPool.PutObject|RES|CPOOL> %d#, Connection %d#, Queueing for deactivation.\n", ObjectID, obj.ObjectID); 
                PerformanceCounters.NumberOfStasisConnections.Increment();
 
                lock (_deactivateQueue.SyncRoot) { 
                    needToQueueWorkItem = (0 == _deactivateQueue.Count);
                    _deactivateQueue.Enqueue(obj); 
                }
                if (needToQueueWorkItem) {
                    // Make sure we actually get around to deactivating the object
                    // and making it available again. 
                    ThreadPool.QueueUserWorkItem(_deactivateCallback, null);
                } 
            } 
            else {
                // no deactivate queue -- do the work right now. 
                DeactivateObject(obj);
            }
        }
 
        internal void PutObjectFromTransactedPool(DbConnectionInternal obj) {
            Debug.Assert(null != obj, "null pooledObject?"); 
            Debug.Assert(!obj.HasEnlistedTransaction, "pooledObject is still enlisted?"); 

            // called by the transacted connection pool , once it's removed the 
            // connection from it's list.  We put the connection back in general
            // circulation.

            // NOTE: there is no locking required here because if we're in this 
            // method, we can safely presume that the caller is the only person
            // that is using the connection, and that all pre-push logic has been 
            // done and all transactions are ended. 

            Bid.PoolerTrace("<prov.DbConnectionPool.PutObjectFromTransactedPool|RES|CPOOL> %d#, Connection %d#, Transaction has ended.\n", ObjectID, obj.ObjectID); 

            if (_state == State.Running && obj.CanBePooled) {
                PutNewObject(obj);
            } 
            else {
                DestroyObject(obj); 
                QueuePoolCreateRequest(); 
            }
        } 

        private void QueuePoolCreateRequest() {
            if (State.Running == _state) {
                // Make sure we're at quota by posting a callback to the threadpool. 
                ThreadPool.QueueUserWorkItem(_poolCreateRequest);
            } 
        } 

        private bool ReclaimEmancipatedObjects() { 
            bool emancipatedObjectFound = false;

            Bid.PoolerTrace("<prov.DbConnectionPool.ReclaimEmancipatedObjects|RES|CPOOL> %d#\n", ObjectID);
 
            List<DbConnectionInternal> reclaimedObjects = new List<DbConnectionInternal>();
            int count; 
 
            lock(_objectList) {
                count = _objectList.Count; 

                for (int i = 0; i < count; ++i) {
                    DbConnectionInternal obj = _objectList[i];
 
                    if (null != obj) {
                        bool locked = false; 
 
                        try {
                            locked = Monitor.TryEnter(obj); 

                            if (locked) { // avoid race condition with PrePush/PostPop and IsEmancipated
                                if (obj.IsEmancipated) {
                                    // Inside the lock, we want to do as little 
                                    // as possible, so we simply mark the object
                                    // as being in the pool, but hand it off to 
                                    // an out of pool list to be deactivated, 
                                    // etc.
                                    obj.PrePush(null); 
                                    reclaimedObjects.Add(obj);
                                }
                            }
                        } 
                        finally {
                            if (locked) 
                                Monitor.Exit(obj); 
                        }
                    } 
                }
            }

            // NOTE: we don't want to call DeactivateObject while we're locked, 
            // because it can make roundtrips to the server and this will block
            // object creation in the pooler.  Instead, we queue things we need 
            // to do up, and process them outside the lock. 
            count = reclaimedObjects.Count;
 
            for (int i = 0; i < count; ++i) {
                DbConnectionInternal obj = reclaimedObjects[i];

                Bid.PoolerTrace("<prov.DbConnectionPool.ReclaimEmancipatedObjects|RES|CPOOL> %d#, Connection %d#, Reclaiming.\n", ObjectID, obj.ObjectID); 
                PerformanceCounters.NumberOfReclaimedConnections.Increment();
 
                emancipatedObjectFound = true; 

                // NOTE: it is not possible for us to have a connection that has 
                // a delegated transaction at this point, because IsEmancipated
                // would not have returned true if it did, and when a connection
                // is emancipated, you can't enlist in a transaction (because you
                // can't get to it to make the call...) 
                DeactivateObject(obj);
            } 
            return emancipatedObjectFound; 
        }
 
        internal void Startup() {
            Bid.PoolerTrace("<prov.DbConnectionPool.Startup|RES|INFO|CPOOL> %d#, CleanupWait=%d\n", ObjectID, _cleanupWait);

            _cleanupTimer = CreateCleanupTimer(); 
            if (NeedToReplenish) {
                QueuePoolCreateRequest(); 
            } 
        }
 
        internal void Shutdown() {
            Bid.PoolerTrace("<prov.DbConnectionPool.Shutdown|RES|INFO|CPOOL> %d#\n", ObjectID);

            _state = State.ShuttingDown; 

            Timer t; // deactivate timer callbacks 
 
            t = _cleanupTimer;
            _cleanupTimer = null; 
            if (null != t) {
                t.Dispose();
            }
 
            t = _errorTimer;
            _errorTimer = null; 
            if (null != t) { 
                t.Dispose();
            } 
        }

        internal void TransactionBegin(SysTx.Transaction transaction) {
            TransactedConnectionPool transactedConnectionPool = _transactedConnectionPool; 
            if (null != transactedConnectionPool) {
                transactedConnectionPool.TransactionBegin(transaction); 
            } 
        }
 
        internal void TransactionEnded(SysTx.Transaction transaction, DbConnectionInternal transactedObject) {
            Debug.Assert(null != transaction, "null transaction?");
            Debug.Assert(null != transactedObject, "null transactedObject?");
            // Note: connection may still be associated with transaction due to Explicit Unbinding requirement. 

            Bid.PoolerTrace("<prov.DbConnectionPool.TransactionEnded|RES|CPOOL> %d#, Transaction %d#, Connection %d#, Transaction Completed\n", ObjectID, transaction.GetHashCode(), transactedObject.ObjectID); 
 
            // called by the internal connection when it get's told that the
            // transaction is completed.  We tell the transacted pool to remove 
            // the connection from it's list, then we put the connection back in
            // general circulation.

            TransactedConnectionPool transactedConnectionPool = _transactedConnectionPool; 
            if (null != transactedConnectionPool) {
                transactedConnectionPool.TransactionEnded(transaction, transactedObject); 
            } 
        }
 
        private DbConnectionInternal UserCreateRequest(DbConnection owningObject) {
            // called by user when they were not able to obtain a free object but
            // instead obtained creation mutex
 
            DbConnectionInternal obj = null;
            if (ErrorOccurred) { 
               throw _resError; 
            }
            else { 
                 if ((Count < MaxPoolSize) || (0 == MaxPoolSize)) {
                    // If we have an odd number of total objects, reclaim any dead objects.
                    // If we did not find any objects to reclaim, create a new one.
 
                    //
                     if ((Count & 0x1) == 0x1 || !ReclaimEmancipatedObjects()) 
                        obj = CreateObject(owningObject); 
                }
                return obj; 
            }
        }

        private class DbConnectionInternalListStack { 
            private DbConnectionInternal _stack;
#if DEBUG 
            private int _version; 
            private int _count;
#endif 
            internal DbConnectionInternalListStack() {
            }

            internal int Count { 
                get {
                    int count = 0; 
                    lock(this) { 
                        for(DbConnectionInternal x = _stack; null != x; x = x.NextPooledObject) {
                            ++count; 
                        }
                    }
#if DEBUG
                    Debug.Assert(count == _count, "count is corrupt"); 
#endif
                    return count; 
                } 
            }
 
            internal DbConnectionInternal SynchronizedPop() {
                DbConnectionInternal value;
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

            internal void SynchronizedPush(DbConnectionInternal value) {
                Debug.Assert(null != value, "pushing null value");
                lock(this) { 
#if DEBUG
                    Debug.Assert(null == value.NextPooledObject, "pushing value with non-null NextPooledObject"); 
                    int index = 0; 
                    for(DbConnectionInternal x = _stack; null != x; x = x.NextPooledObject, ++index) {
                        Debug.Assert(x != value, "double push: connection already in stack"); 
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
