//------------------------------------------------------------------------------ 
// <copyright file="HttpAsyncResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * ASP.NET simple internal implementation of IAsyncResult 
 *
 * Copyright (c) 2000 Microsoft Corporation 
 */

namespace System.Web {
 
    using System;
    using System.Threading; 
 
    internal class HttpAsyncResult : IAsyncResult {
        private AsyncCallback _callback; 
        private Object        _asyncState;

        private bool          _completed;
        private bool          _completedSynchronously; 

        private Object        _result; 
        private Exception     _error; 

        // pipeline support 
        private RequestNotificationStatus
                              _status;

        /* 
         * Constructor with pending result
         */ 
        internal HttpAsyncResult(AsyncCallback cb, Object state) { 
            _callback    = cb;
            _asyncState  = state; 
            _status      = RequestNotificationStatus.Continue;
        }

        /* 
         * Constructor with known result
         */ 
        internal HttpAsyncResult(AsyncCallback cb, Object state, 
                                 bool completed, Object result, Exception error) {
            _callback    = cb; 
            _asyncState  = state;

            _completed = completed;
            _completedSynchronously = completed; 

            _result = result; 
            _error = error; 
            _status = RequestNotificationStatus.Continue;
 
            if (_completed && _callback != null)
                _callback(this);
        }
 
        internal void SetComplete() {
            _completed = true; 
        } 

        /* 
         * Helper method to process completions
         */
        internal void Complete(bool synchronous, Object result, Exception error, RequestNotificationStatus status) {
            _completed              = true; 
            _completedSynchronously = synchronous;
            _result                 = result; 
            _error                  = error; 
            _status                 = status;
 
            if (_callback != null)
                _callback(this);
        }
 
        internal void Complete(bool synchronous, Object result, Exception error) {
            Complete(synchronous, result, error, RequestNotificationStatus.Continue); 
        } 

 
        /*
         * Helper method to implement End call to async method
         */
        internal Object End() { 
            if (_error != null)
                throw new HttpException(null, _error); 
 
            return _result;
        } 

        //
        // Properties that are not part of IAsyncResult
        // 

        internal Exception   Error { get { return _error;}} 
 
        internal RequestNotificationStatus Status {
            get { 
                return _status;
            }
        }
 
        //
        // IAsyncResult implementation 
        // 

        public bool         IsCompleted { get { return _completed;}} 
        public bool         CompletedSynchronously { get { return _completedSynchronously;}}
        public Object       AsyncState { get { return _asyncState;}}
        public WaitHandle   AsyncWaitHandle { get { return null;}} // wait not supported
    } 

} 
//------------------------------------------------------------------------------ 
// <copyright file="HttpAsyncResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * ASP.NET simple internal implementation of IAsyncResult 
 *
 * Copyright (c) 2000 Microsoft Corporation 
 */

namespace System.Web {
 
    using System;
    using System.Threading; 
 
    internal class HttpAsyncResult : IAsyncResult {
        private AsyncCallback _callback; 
        private Object        _asyncState;

        private bool          _completed;
        private bool          _completedSynchronously; 

        private Object        _result; 
        private Exception     _error; 

        // pipeline support 
        private RequestNotificationStatus
                              _status;

        /* 
         * Constructor with pending result
         */ 
        internal HttpAsyncResult(AsyncCallback cb, Object state) { 
            _callback    = cb;
            _asyncState  = state; 
            _status      = RequestNotificationStatus.Continue;
        }

        /* 
         * Constructor with known result
         */ 
        internal HttpAsyncResult(AsyncCallback cb, Object state, 
                                 bool completed, Object result, Exception error) {
            _callback    = cb; 
            _asyncState  = state;

            _completed = completed;
            _completedSynchronously = completed; 

            _result = result; 
            _error = error; 
            _status = RequestNotificationStatus.Continue;
 
            if (_completed && _callback != null)
                _callback(this);
        }
 
        internal void SetComplete() {
            _completed = true; 
        } 

        /* 
         * Helper method to process completions
         */
        internal void Complete(bool synchronous, Object result, Exception error, RequestNotificationStatus status) {
            _completed              = true; 
            _completedSynchronously = synchronous;
            _result                 = result; 
            _error                  = error; 
            _status                 = status;
 
            if (_callback != null)
                _callback(this);
        }
 
        internal void Complete(bool synchronous, Object result, Exception error) {
            Complete(synchronous, result, error, RequestNotificationStatus.Continue); 
        } 

 
        /*
         * Helper method to implement End call to async method
         */
        internal Object End() { 
            if (_error != null)
                throw new HttpException(null, _error); 
 
            return _result;
        } 

        //
        // Properties that are not part of IAsyncResult
        // 

        internal Exception   Error { get { return _error;}} 
 
        internal RequestNotificationStatus Status {
            get { 
                return _status;
            }
        }
 
        //
        // IAsyncResult implementation 
        // 

        public bool         IsCompleted { get { return _completed;}} 
        public bool         CompletedSynchronously { get { return _completedSynchronously;}}
        public Object       AsyncState { get { return _asyncState;}}
        public WaitHandle   AsyncWaitHandle { get { return null;}} // wait not supported
    } 

} 
