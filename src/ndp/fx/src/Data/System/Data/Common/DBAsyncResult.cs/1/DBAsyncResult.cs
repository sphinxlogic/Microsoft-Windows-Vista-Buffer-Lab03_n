//------------------------------------------------------------------------------ 
// <copyright file="DBAsyncResult.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
    using System; 
    using System.Data.ProviderBase;
    using System.Diagnostics;
    using System.Threading;
 
    internal sealed class DbAsyncResult : IAsyncResult {
        private readonly AsyncCallback     _callback                = null; 
        private bool                       _fCompleted              = false; 
        private bool                       _fCompletedSynchronously = false;
        private readonly ManualResetEvent  _manualResetEvent        = null; 
        private object                     _owner                   = null;
        private readonly object            _stateObject             = null;
        private readonly string            _endMethodName;
        private ExecutionContext           _execContext             = null; 
        static  private ContextCallback    _contextCallback         = new ContextCallback(AsyncCallback_Context);
 
        // Used for SqlClient Open async 
        private DbConnectionInternal       _connectionInternal = null;
 
        internal DbAsyncResult(object owner, string endMethodName, AsyncCallback callback, object stateObject, ExecutionContext execContext) {
            _owner            = owner;
            _endMethodName    = endMethodName;
            _callback         = callback; 
            _stateObject      = stateObject;
            _manualResetEvent = new ManualResetEvent(false); 
            _execContext      = execContext; 
        }
 
        object IAsyncResult.AsyncState {
            get {
                return _stateObject;
            } 
        }
 
        WaitHandle IAsyncResult.AsyncWaitHandle { 
            get {
                return _manualResetEvent; 
            }
        }

        bool IAsyncResult.CompletedSynchronously { 
            get {
                return _fCompletedSynchronously; 
            } 
        }
 
        internal DbConnectionInternal ConnectionInternal {
            get {
                return _connectionInternal;
            } 
            set {
                _connectionInternal = value; 
            } 
        }
 
        bool IAsyncResult.IsCompleted {
            get {
                return _fCompleted;
            } 
        }
 
        internal string EndMethodName { 
            get {
                return _endMethodName; 
            }
        }

        internal void CompareExchangeOwner(object owner, string method) { 
            object prior = Interlocked.CompareExchange(ref _owner, null, owner);
            if (prior != owner) { 
                if (null != prior) { 
                    throw ADP.IncorrectAsyncResult();
                } 
                throw ADP.MethodCalledTwice(method);
            }
        }
 
        internal void Reset() {
            _fCompleted = false; 
            _fCompletedSynchronously = false; 
            _manualResetEvent.Reset();
        } 

        internal void SetCompleted() {
            _fCompleted = true;
            _manualResetEvent.Set(); 

            if (_callback != null) { 
                // QueueUserWorkItem only accepts WaitCallback - which requires a signature of Foo(object state). 
                // Must call function on this object with that signature - and then call user AsyncCallback.
                // AsyncCallback signature is Foo(IAsyncResult result). 
                ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteCallback), this);
            }
        }
 
        internal void SetCompletedSynchronously() {
            _fCompletedSynchronously = true; 
        } 

 
        static private void AsyncCallback_Context(Object state)
        {
            DbAsyncResult result = (DbAsyncResult) state;
            if (result._callback != null) { 
                result._callback(result);
            } 
        } 

        private void ExecuteCallback(object asyncResult) { 
            DbAsyncResult result = (DbAsyncResult) asyncResult;
            if (null != result._callback) {
                if (result._execContext != null) {
                    ExecutionContext.Run(result._execContext, DbAsyncResult._contextCallback, result); 
                } else {
                    result._callback(this); 
                } 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DBAsyncResult.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
    using System; 
    using System.Data.ProviderBase;
    using System.Diagnostics;
    using System.Threading;
 
    internal sealed class DbAsyncResult : IAsyncResult {
        private readonly AsyncCallback     _callback                = null; 
        private bool                       _fCompleted              = false; 
        private bool                       _fCompletedSynchronously = false;
        private readonly ManualResetEvent  _manualResetEvent        = null; 
        private object                     _owner                   = null;
        private readonly object            _stateObject             = null;
        private readonly string            _endMethodName;
        private ExecutionContext           _execContext             = null; 
        static  private ContextCallback    _contextCallback         = new ContextCallback(AsyncCallback_Context);
 
        // Used for SqlClient Open async 
        private DbConnectionInternal       _connectionInternal = null;
 
        internal DbAsyncResult(object owner, string endMethodName, AsyncCallback callback, object stateObject, ExecutionContext execContext) {
            _owner            = owner;
            _endMethodName    = endMethodName;
            _callback         = callback; 
            _stateObject      = stateObject;
            _manualResetEvent = new ManualResetEvent(false); 
            _execContext      = execContext; 
        }
 
        object IAsyncResult.AsyncState {
            get {
                return _stateObject;
            } 
        }
 
        WaitHandle IAsyncResult.AsyncWaitHandle { 
            get {
                return _manualResetEvent; 
            }
        }

        bool IAsyncResult.CompletedSynchronously { 
            get {
                return _fCompletedSynchronously; 
            } 
        }
 
        internal DbConnectionInternal ConnectionInternal {
            get {
                return _connectionInternal;
            } 
            set {
                _connectionInternal = value; 
            } 
        }
 
        bool IAsyncResult.IsCompleted {
            get {
                return _fCompleted;
            } 
        }
 
        internal string EndMethodName { 
            get {
                return _endMethodName; 
            }
        }

        internal void CompareExchangeOwner(object owner, string method) { 
            object prior = Interlocked.CompareExchange(ref _owner, null, owner);
            if (prior != owner) { 
                if (null != prior) { 
                    throw ADP.IncorrectAsyncResult();
                } 
                throw ADP.MethodCalledTwice(method);
            }
        }
 
        internal void Reset() {
            _fCompleted = false; 
            _fCompletedSynchronously = false; 
            _manualResetEvent.Reset();
        } 

        internal void SetCompleted() {
            _fCompleted = true;
            _manualResetEvent.Set(); 

            if (_callback != null) { 
                // QueueUserWorkItem only accepts WaitCallback - which requires a signature of Foo(object state). 
                // Must call function on this object with that signature - and then call user AsyncCallback.
                // AsyncCallback signature is Foo(IAsyncResult result). 
                ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteCallback), this);
            }
        }
 
        internal void SetCompletedSynchronously() {
            _fCompletedSynchronously = true; 
        } 

 
        static private void AsyncCallback_Context(Object state)
        {
            DbAsyncResult result = (DbAsyncResult) state;
            if (result._callback != null) { 
                result._callback(result);
            } 
        } 

        private void ExecuteCallback(object asyncResult) { 
            DbAsyncResult result = (DbAsyncResult) asyncResult;
            if (null != result._callback) {
                if (result._execContext != null) {
                    ExecutionContext.Run(result._execContext, DbAsyncResult._contextCallback, result); 
                } else {
                    result._callback(this); 
                } 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
