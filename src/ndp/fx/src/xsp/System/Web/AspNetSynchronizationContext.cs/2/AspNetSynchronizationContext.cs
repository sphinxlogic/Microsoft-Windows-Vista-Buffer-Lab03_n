//------------------------------------------------------------------------------ 
// <copyright file="AspNetSynchronizationContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

using System; 
using System.ComponentModel; 
using System.Security.Permissions;
using System.Threading; 
using System.Web;
using System.Web.Util;

namespace System.Web { 

    internal class AspNetSynchronizationContext : SynchronizationContext { 
        private HttpApplication _application; 
        private bool _disabled;
        private bool _syncCaller; 
        private bool _invalidOperationEncountered;
        private int _pendingCount;
        private Exception _error;
        private WaitCallback _lastCompletionWorkItemCallback; 

        internal AspNetSynchronizationContext(HttpApplication app) { 
            _application = app; 
        }
 
        private void CallCallback(SendOrPostCallback callback, Object state) {
            // don't take app lock for sync caller to avoid deadlocks in case they poll for result
            if (_syncCaller) {
                CallCallbackPossiblyUnderLock(callback, state); 
            }
            else { 
                lock (_application) { 
                    CallCallbackPossiblyUnderLock(callback, state);
                } 
            }
        }

        private void CallCallbackPossiblyUnderLock(SendOrPostCallback callback, Object state) { 
            HttpApplication.ThreadContext threadContext = null;
            try { 
                threadContext = _application.OnThreadEnter(); 
                try {
                    callback(state); 
                }
                catch (Exception e) {
                    _error = e;
                } 
            }
            finally { 
                if (threadContext != null) { 
                    threadContext.Leave();
                } 
            }
        }

        internal int PendingOperationsCount { 
            get { return _pendingCount; }
        } 
 
        internal Exception Error {
            get { return _error; } 
        }

        internal void ClearError() {
            _error = null; 
        }
 
        internal void SetLastCompletionWorkItem(WaitCallback callback) { 
            Debug.Assert(_lastCompletionWorkItemCallback == null); // only one at a time
            _lastCompletionWorkItemCallback = callback; 
        }

        public override void Send(SendOrPostCallback callback, Object state) {
#if DBG 
            Debug.Trace("Async", "Send");
            Debug.Trace("AsyncStack", "Send from:\r\n" + System.Environment.StackTrace); 
#endif 
            CallCallback(callback, state);
        } 

        public override void Post(SendOrPostCallback callback, Object state) {
#if DBG
            Debug.Trace("Async", "Post"); 
            Debug.Trace("AsyncStack", "Post from:\r\n" + System.Environment.StackTrace);
#endif 
            CallCallback(callback, state); 
        }
 
#if DBG
        [EnvironmentPermission(SecurityAction.Assert, Unrestricted=true)]
        private void CreateCopyDumpStack() {
            Debug.Trace("Async", "CreateCopy"); 
            Debug.Trace("AsyncStack", "CreateCopy from:\r\n" + System.Environment.StackTrace);
        } 
#endif 

        public override SynchronizationContext CreateCopy() { 
#if DBG
            CreateCopyDumpStack();
#endif
            AspNetSynchronizationContext context = new AspNetSynchronizationContext(_application); 
            context._disabled = _disabled;
            context._syncCaller = _syncCaller; 
            return context; 
        }
 
        public override void OperationStarted() {
            if (_invalidOperationEncountered || (_disabled && _pendingCount == 0)) {
                _invalidOperationEncountered = true;
                throw new InvalidOperationException(SR.GetString(SR.Async_operation_disabled)); 
            }
 
            Interlocked.Increment(ref _pendingCount); 
#if DBG
            Debug.Trace("Async", "OperationStarted(count=" + _pendingCount + ")"); 
            Debug.Trace("AsyncStack", "OperationStarted(count=" + _pendingCount + ") from:\r\n" + System.Environment.StackTrace);
#endif
        }
 
        public override void OperationCompleted() {
            if (_invalidOperationEncountered || (_disabled && _pendingCount == 0)) { 
                // throw from operation started could cause extra operation completed 
                return;
            } 

            bool lastOperationCompleted = (Interlocked.Decrement(ref _pendingCount) == 0);

#if DBG 
            Debug.Trace("Async", "OperationCompleted(count=" + _pendingCount + ")");
            Debug.Trace("AsyncStack", "OperationCompleted(count=" + _pendingCount + ") from:\r\n" + System.Environment.StackTrace); 
#endif 

            if (lastOperationCompleted && _lastCompletionWorkItemCallback != null) { 
                // notify (once) about the last completion to resume the async work
                WaitCallback cb = _lastCompletionWorkItemCallback;
                _lastCompletionWorkItemCallback = null;
                Debug.Trace("Async", "Queueing LastCompletionWorkItemCallback"); 
                ThreadPool.QueueUserWorkItem(cb);
            } 
        } 

        internal bool Enabled { 
            get { return !_disabled; }
        }

        internal void Enable() { 
            _disabled = false;
        } 
 
        internal void Disable() {
            _disabled = true; 
        }

        internal void SetSyncCaller() {
            _syncCaller = true; 
        }
 
        internal void ResetSyncCaller() { 
            _syncCaller = false;
        } 
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="AspNetSynchronizationContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

using System; 
using System.ComponentModel; 
using System.Security.Permissions;
using System.Threading; 
using System.Web;
using System.Web.Util;

namespace System.Web { 

    internal class AspNetSynchronizationContext : SynchronizationContext { 
        private HttpApplication _application; 
        private bool _disabled;
        private bool _syncCaller; 
        private bool _invalidOperationEncountered;
        private int _pendingCount;
        private Exception _error;
        private WaitCallback _lastCompletionWorkItemCallback; 

        internal AspNetSynchronizationContext(HttpApplication app) { 
            _application = app; 
        }
 
        private void CallCallback(SendOrPostCallback callback, Object state) {
            // don't take app lock for sync caller to avoid deadlocks in case they poll for result
            if (_syncCaller) {
                CallCallbackPossiblyUnderLock(callback, state); 
            }
            else { 
                lock (_application) { 
                    CallCallbackPossiblyUnderLock(callback, state);
                } 
            }
        }

        private void CallCallbackPossiblyUnderLock(SendOrPostCallback callback, Object state) { 
            HttpApplication.ThreadContext threadContext = null;
            try { 
                threadContext = _application.OnThreadEnter(); 
                try {
                    callback(state); 
                }
                catch (Exception e) {
                    _error = e;
                } 
            }
            finally { 
                if (threadContext != null) { 
                    threadContext.Leave();
                } 
            }
        }

        internal int PendingOperationsCount { 
            get { return _pendingCount; }
        } 
 
        internal Exception Error {
            get { return _error; } 
        }

        internal void ClearError() {
            _error = null; 
        }
 
        internal void SetLastCompletionWorkItem(WaitCallback callback) { 
            Debug.Assert(_lastCompletionWorkItemCallback == null); // only one at a time
            _lastCompletionWorkItemCallback = callback; 
        }

        public override void Send(SendOrPostCallback callback, Object state) {
#if DBG 
            Debug.Trace("Async", "Send");
            Debug.Trace("AsyncStack", "Send from:\r\n" + System.Environment.StackTrace); 
#endif 
            CallCallback(callback, state);
        } 

        public override void Post(SendOrPostCallback callback, Object state) {
#if DBG
            Debug.Trace("Async", "Post"); 
            Debug.Trace("AsyncStack", "Post from:\r\n" + System.Environment.StackTrace);
#endif 
            CallCallback(callback, state); 
        }
 
#if DBG
        [EnvironmentPermission(SecurityAction.Assert, Unrestricted=true)]
        private void CreateCopyDumpStack() {
            Debug.Trace("Async", "CreateCopy"); 
            Debug.Trace("AsyncStack", "CreateCopy from:\r\n" + System.Environment.StackTrace);
        } 
#endif 

        public override SynchronizationContext CreateCopy() { 
#if DBG
            CreateCopyDumpStack();
#endif
            AspNetSynchronizationContext context = new AspNetSynchronizationContext(_application); 
            context._disabled = _disabled;
            context._syncCaller = _syncCaller; 
            return context; 
        }
 
        public override void OperationStarted() {
            if (_invalidOperationEncountered || (_disabled && _pendingCount == 0)) {
                _invalidOperationEncountered = true;
                throw new InvalidOperationException(SR.GetString(SR.Async_operation_disabled)); 
            }
 
            Interlocked.Increment(ref _pendingCount); 
#if DBG
            Debug.Trace("Async", "OperationStarted(count=" + _pendingCount + ")"); 
            Debug.Trace("AsyncStack", "OperationStarted(count=" + _pendingCount + ") from:\r\n" + System.Environment.StackTrace);
#endif
        }
 
        public override void OperationCompleted() {
            if (_invalidOperationEncountered || (_disabled && _pendingCount == 0)) { 
                // throw from operation started could cause extra operation completed 
                return;
            } 

            bool lastOperationCompleted = (Interlocked.Decrement(ref _pendingCount) == 0);

#if DBG 
            Debug.Trace("Async", "OperationCompleted(count=" + _pendingCount + ")");
            Debug.Trace("AsyncStack", "OperationCompleted(count=" + _pendingCount + ") from:\r\n" + System.Environment.StackTrace); 
#endif 

            if (lastOperationCompleted && _lastCompletionWorkItemCallback != null) { 
                // notify (once) about the last completion to resume the async work
                WaitCallback cb = _lastCompletionWorkItemCallback;
                _lastCompletionWorkItemCallback = null;
                Debug.Trace("Async", "Queueing LastCompletionWorkItemCallback"); 
                ThreadPool.QueueUserWorkItem(cb);
            } 
        } 

        internal bool Enabled { 
            get { return !_disabled; }
        }

        internal void Enable() { 
            _disabled = false;
        } 
 
        internal void Disable() {
            _disabled = true; 
        }

        internal void SetSyncCaller() {
            _syncCaller = true; 
        }
 
        internal void ResetSyncCaller() { 
            _syncCaller = false;
        } 
    }
}
