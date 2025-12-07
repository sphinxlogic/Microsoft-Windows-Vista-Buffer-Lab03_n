// ------------------------------------------------------------------------------ 
// <copyright file="_HttpRequestStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Threading;
    using System.Collections;
    using System.Security.Permissions;
 
    unsafe class HttpRequestStream : Stream {
        private HttpListenerContext m_HttpContext; 
        private uint m_DataChunkOffset; 
        private int m_DataChunkIndex;
        private bool m_Closed; 
        private const int MaxReadSize = 0x20000; //http.sys recommends we limit reads to 128k

        internal HttpRequestStream(HttpListenerContext httpContext) {
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::.ctor() HttpListenerContext#" + ValidationHelper.HashString(httpContext)); 
            m_HttpContext = httpContext;
        } 
 
        public override bool CanSeek {
            get { 
                return false;
            }
        }
 
        public override bool CanWrite {
            get { 
                return false; 
            }
        } 

        public override bool CanRead {
            get {
                return true; 
            }
        } 
 
        /*
        // Consider removing. 
        public bool DataAvailable {
            get {
                return !m_Closed;
            } 
        }
        */ 
 
        public override void Flush() {
        } 

        public override long Length {
            get {
                throw new NotSupportedException(SR.GetString(SR.net_noseek)); 
            }
 
        } 

        public override long Position { 
            get {
                throw new NotSupportedException(SR.GetString(SR.net_noseek));
            }
            set { 
                throw new NotSupportedException(SR.GetString(SR.net_noseek));
            } 
        } 

        public override long Seek(long offset, SeekOrigin origin) { 
            throw new NotSupportedException(SR.GetString(SR.net_noseek));
        }

        public override void SetLength(long value) { 
            throw new NotSupportedException(SR.GetString(SR.net_noseek));
        } 
 
        public override int Read([In, Out] byte[] buffer, int offset, int size) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Read", ""); 
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() size:" + size + " offset:" + offset);
            if (buffer==null) {
                throw new ArgumentNullException("buffer");
            } 
            if (offset<0 || offset>buffer.Length) {
                throw new ArgumentOutOfRangeException("offset"); 
            } 
            if (size<0 || size>buffer.Length-offset) {
                throw new ArgumentOutOfRangeException("size"); 
            }
            if (size==0 || m_Closed) {
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Read", "dataRead:0");
                return 0; 
            }
 
            uint dataRead = 0; 

            if (m_DataChunkIndex != -1) { 
                dataRead = UnsafeNclNativeMethods.HttpApi.GetChunks(m_HttpContext.Request.RequestBuffer, m_HttpContext.Request.OriginalBlobAddress, ref m_DataChunkIndex, ref m_DataChunkOffset, buffer, offset, size);
            }

            if(m_DataChunkIndex == -1 && dataRead < size){ 
                GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() size:" + size + " offset:" + offset);
                uint statusCode = 0; 
                uint extraDataRead = 0; 
                offset+= (int) dataRead;
                size-=(int)dataRead; 

                //the http.sys team recommends that we limit the size to 128kb
                if(size > MaxReadSize){
                    size = MaxReadSize; 
                }
 
                fixed (byte *pBuffer = buffer) { 
                    // issue unmanaged blocking call
                    GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() calling UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody"); 

                    statusCode =
                        UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody(
                            m_HttpContext.RequestQueueHandle, 
                            m_HttpContext.RequestId,
                            (uint)UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY, 
                            (void*)(pBuffer + offset), 
                            (uint)size,
                            &extraDataRead, 
                            null);

                    dataRead+=extraDataRead;
                    GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() call to UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody returned:" + statusCode + " dataRead:" + dataRead); 
                }
                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) { 
                    Exception exception = new HttpListenerException((int)statusCode); 
                    if(Logging.On)Logging.Exception(Logging.HttpListener, this, "Read", exception);
                    throw exception; 
                }
                UpdateAfterRead(statusCode, dataRead);
            }
            if(Logging.On)Logging.Dump(Logging.HttpListener, this, "Read", buffer, offset, (int)dataRead); 
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() returning dataRead:" + dataRead);
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Read", "dataRead:" + dataRead); 
            return (int)dataRead; 
        }
 
        void UpdateAfterRead(uint statusCode, uint dataRead) {
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::UpdateAfterRead() statusCode:" + statusCode + " m_Closed:" + m_Closed);
            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF || dataRead == 0) {
                Close(); 
            }
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::UpdateAfterRead() statusCode:" + statusCode + " m_Closed:" + m_Closed); 
        } 

 
        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "BeginRead", "");
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::BeginRead() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset); 
            if (buffer==null) {
                throw new ArgumentNullException("buffer"); 
            } 
            if (offset<0 || offset>buffer.Length) {
                throw new ArgumentOutOfRangeException("offset"); 
            }
            if (size<0 || size>buffer.Length-offset) {
                throw new ArgumentOutOfRangeException("size");
            } 
            if (size==0 || m_Closed) {
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "BeginRead", ""); 
                HttpRequestStreamAsyncResult result = new HttpRequestStreamAsyncResult(this, state, callback); 
                result.InvokeCallback((uint) 0);
                return result; 
            }

            HttpRequestStreamAsyncResult asyncResult = null;
 
            uint dataRead = 0;
            if (m_DataChunkIndex != -1) { 
                dataRead = UnsafeNclNativeMethods.HttpApi.GetChunks(m_HttpContext.Request.RequestBuffer, m_HttpContext.Request.OriginalBlobAddress, ref m_DataChunkIndex, ref m_DataChunkOffset, buffer, offset, size); 
                if (m_DataChunkIndex != -1 && dataRead == size) {
                    asyncResult = new HttpRequestStreamAsyncResult(this, state, callback, buffer, offset, (uint)size, 0); 
                    asyncResult.InvokeCallback(dataRead);
                }
            }
 
            if(m_DataChunkIndex == -1 && dataRead < size){
                GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::BeginRead() size:" + size + " offset:" + offset); 
                uint statusCode = 0; 
                offset += (int)dataRead;
                size -= (int)dataRead; 

                //the http.sys team recommends that we limit the size to 128kb
                if(size > MaxReadSize){
                    size = MaxReadSize; 
                }
 
                asyncResult = new HttpRequestStreamAsyncResult(this, state, callback, buffer, offset, (uint)size, dataRead); 

                fixed (byte *pBuffer = buffer) { 
                    // issue unmanaged blocking call
                    GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::BeginRead() calling UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody");

                    m_HttpContext.EnsureBoundHandle(); 
                    GlobalLog.Assert(asyncResult.m_pOverlapped->InternalLow != (IntPtr) 0x0103, "HttpRequestStream#{0}::BeginRead()|Overlapped structure STATUS_PENDING before use.  0x{1:x}", ValidationHelper.HashString(this), (IntPtr) asyncResult.m_pOverlapped);
                    statusCode = 
                        UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody( 
                            m_HttpContext.RequestQueueHandle,
                            m_HttpContext.RequestId, 
                            (uint)UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
                            asyncResult.m_pPinnedBuffer,
                            (uint)size,
                            null, 
                            asyncResult.m_pOverlapped);
 
                    GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::BeginRead() call to UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody returned:" + statusCode + " dataRead:" + dataRead); 
                }
                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) { 
                    Exception exception = new HttpListenerException((int)statusCode);
                    if(Logging.On)Logging.Exception(Logging.HttpListener, this, "BeginRead", exception);
                    throw exception;
                } 
                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) {
                    // WORKAROUND: http.sys HttpReceiveRequestEntityBody has a bug where InternalLow gets set to STATUS_PENDING when it shouldn't. 
                    // This causes an MDA to fire in the CLR (as well as the assert in NclUtilities.OverlappedFree). 
                    // Explicitly reset InternalLow as if it hadn't been touched.
                    asyncResult.m_pOverlapped->InternalLow = IntPtr.Zero; 

                    asyncResult.InternalCleanup();
                    asyncResult = new HttpRequestStreamAsyncResult(this, state, callback);
                    asyncResult.InvokeCallback((uint)0); 
                }
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "BeginRead", ""); 
            return asyncResult;
        } 

        public override int EndRead(IAsyncResult asyncResult) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "EndRead", "");
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::EndRead() asyncResult#" + ValidationHelper.HashString(asyncResult)); 
            if (asyncResult==null) {
                throw new ArgumentNullException("asyncResult"); 
            } 
            HttpRequestStreamAsyncResult castedAsyncResult = asyncResult as HttpRequestStreamAsyncResult;
            if (castedAsyncResult==null || castedAsyncResult.AsyncObject!=this) { 
                throw new ArgumentException(SR.GetString(SR.net_io_invalidasyncresult), "asyncResult");
            }
            if (castedAsyncResult.EndCalled) {
                throw new InvalidOperationException(SR.GetString(SR.net_io_invalidendcall, "EndRead")); 
            }
            castedAsyncResult.EndCalled = true; 
            // wait & then check for errors 
            object returnValue = castedAsyncResult.InternalWaitForCompletion();
            Exception exception = returnValue as Exception; 
            if (exception!=null) {
                GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::EndRead() rethrowing exception:" + exception);
                if(Logging.On)Logging.Exception(Logging.HttpListener, this, "EndRead", exception);
                throw exception; 
            }
            // 
 

            uint dataRead = (uint)returnValue; 
            UpdateAfterRead((uint)castedAsyncResult.ErrorCode, dataRead);
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::EndRead() returning returnValue:" + ValidationHelper.ToString(returnValue));
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "EndRead", "");
            return (int)dataRead + (int)castedAsyncResult.m_dataAlreadyRead; 
        }
 
        public override void Write(byte[] buffer, int offset, int size) { 
            throw new InvalidOperationException(SR.GetString(SR.net_readonlystream));
        } 


        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state) { 
            throw new InvalidOperationException(SR.GetString(SR.net_readonlystream));
        } 
 
        public override void EndWrite(IAsyncResult asyncResult) {
            throw new InvalidOperationException(SR.GetString(SR.net_readonlystream)); 
        }

        protected override void Dispose(bool disposing) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Dispose", ""); 
            try {
                GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Dispose(bool) m_Closed:" + m_Closed); 
                m_Closed = true; 
            }
            finally { 
                base.Dispose(disposing);
            }
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Dispose", "");
        } 

        unsafe class HttpRequestStreamAsyncResult : LazyAsyncResult { 
            internal NativeOverlapped* m_pOverlapped; 
            internal void* m_pPinnedBuffer;
            internal uint m_dataAlreadyRead = 0; 

            private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(Callback);

            internal HttpRequestStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback) : base(asyncObject, userState, callback) { 
            }
 
            internal HttpRequestStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback, byte[] buffer, int offset, uint size, uint dataAlreadyRead): base(asyncObject, userState, callback) { 
                m_dataAlreadyRead = dataAlreadyRead;
                Overlapped overlapped = new Overlapped(); 
                overlapped.AsyncResult = this;
                m_pOverlapped = overlapped.Pack(s_IOCallback, buffer);
                m_pPinnedBuffer = (void*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
            } 

            private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped) { 
                Overlapped callbackOverlapped = Overlapped.Unpack(nativeOverlapped); 
                HttpRequestStreamAsyncResult asyncResult = callbackOverlapped.AsyncResult as HttpRequestStreamAsyncResult;
                GlobalLog.Print("HttpRequestStreamAsyncResult#" + ValidationHelper.HashString(asyncResult) + "::Callback() errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes + " nativeOverlapped:0x" + ((IntPtr)nativeOverlapped).ToString("x8")); 
                object result = null;
                try {
                    if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) {
                        asyncResult.ErrorCode = (int)errorCode; 
                        result = new HttpListenerException((int)errorCode);
                    } 
                    else { 
                        result = numBytes;
                        if(Logging.On)Logging.Dump(Logging.HttpListener, asyncResult, "Callback", (IntPtr)asyncResult.m_pPinnedBuffer, (int)numBytes); 
                    }
                    GlobalLog.Print("HttpRequestStreamAsyncResult#" + ValidationHelper.HashString(asyncResult) + "::Callback() calling Complete()");
                }
                catch (Exception e) { 
                    result = e;
                } 
                asyncResult.InvokeCallback(result); 
            }
 
            // Will be called from the base class upon InvokeCallback()
            protected override void Cleanup() {
                base.Cleanup();
                if (m_pOverlapped != null) { 
                    Overlapped.Free(m_pOverlapped);
                } 
            } 
        }
    } 
}
// ------------------------------------------------------------------------------ 
// <copyright file="_HttpRequestStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Threading;
    using System.Collections;
    using System.Security.Permissions;
 
    unsafe class HttpRequestStream : Stream {
        private HttpListenerContext m_HttpContext; 
        private uint m_DataChunkOffset; 
        private int m_DataChunkIndex;
        private bool m_Closed; 
        private const int MaxReadSize = 0x20000; //http.sys recommends we limit reads to 128k

        internal HttpRequestStream(HttpListenerContext httpContext) {
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::.ctor() HttpListenerContext#" + ValidationHelper.HashString(httpContext)); 
            m_HttpContext = httpContext;
        } 
 
        public override bool CanSeek {
            get { 
                return false;
            }
        }
 
        public override bool CanWrite {
            get { 
                return false; 
            }
        } 

        public override bool CanRead {
            get {
                return true; 
            }
        } 
 
        /*
        // Consider removing. 
        public bool DataAvailable {
            get {
                return !m_Closed;
            } 
        }
        */ 
 
        public override void Flush() {
        } 

        public override long Length {
            get {
                throw new NotSupportedException(SR.GetString(SR.net_noseek)); 
            }
 
        } 

        public override long Position { 
            get {
                throw new NotSupportedException(SR.GetString(SR.net_noseek));
            }
            set { 
                throw new NotSupportedException(SR.GetString(SR.net_noseek));
            } 
        } 

        public override long Seek(long offset, SeekOrigin origin) { 
            throw new NotSupportedException(SR.GetString(SR.net_noseek));
        }

        public override void SetLength(long value) { 
            throw new NotSupportedException(SR.GetString(SR.net_noseek));
        } 
 
        public override int Read([In, Out] byte[] buffer, int offset, int size) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Read", ""); 
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() size:" + size + " offset:" + offset);
            if (buffer==null) {
                throw new ArgumentNullException("buffer");
            } 
            if (offset<0 || offset>buffer.Length) {
                throw new ArgumentOutOfRangeException("offset"); 
            } 
            if (size<0 || size>buffer.Length-offset) {
                throw new ArgumentOutOfRangeException("size"); 
            }
            if (size==0 || m_Closed) {
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Read", "dataRead:0");
                return 0; 
            }
 
            uint dataRead = 0; 

            if (m_DataChunkIndex != -1) { 
                dataRead = UnsafeNclNativeMethods.HttpApi.GetChunks(m_HttpContext.Request.RequestBuffer, m_HttpContext.Request.OriginalBlobAddress, ref m_DataChunkIndex, ref m_DataChunkOffset, buffer, offset, size);
            }

            if(m_DataChunkIndex == -1 && dataRead < size){ 
                GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() size:" + size + " offset:" + offset);
                uint statusCode = 0; 
                uint extraDataRead = 0; 
                offset+= (int) dataRead;
                size-=(int)dataRead; 

                //the http.sys team recommends that we limit the size to 128kb
                if(size > MaxReadSize){
                    size = MaxReadSize; 
                }
 
                fixed (byte *pBuffer = buffer) { 
                    // issue unmanaged blocking call
                    GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() calling UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody"); 

                    statusCode =
                        UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody(
                            m_HttpContext.RequestQueueHandle, 
                            m_HttpContext.RequestId,
                            (uint)UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY, 
                            (void*)(pBuffer + offset), 
                            (uint)size,
                            &extraDataRead, 
                            null);

                    dataRead+=extraDataRead;
                    GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() call to UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody returned:" + statusCode + " dataRead:" + dataRead); 
                }
                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) { 
                    Exception exception = new HttpListenerException((int)statusCode); 
                    if(Logging.On)Logging.Exception(Logging.HttpListener, this, "Read", exception);
                    throw exception; 
                }
                UpdateAfterRead(statusCode, dataRead);
            }
            if(Logging.On)Logging.Dump(Logging.HttpListener, this, "Read", buffer, offset, (int)dataRead); 
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Read() returning dataRead:" + dataRead);
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Read", "dataRead:" + dataRead); 
            return (int)dataRead; 
        }
 
        void UpdateAfterRead(uint statusCode, uint dataRead) {
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::UpdateAfterRead() statusCode:" + statusCode + " m_Closed:" + m_Closed);
            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF || dataRead == 0) {
                Close(); 
            }
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::UpdateAfterRead() statusCode:" + statusCode + " m_Closed:" + m_Closed); 
        } 

 
        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "BeginRead", "");
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::BeginRead() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset); 
            if (buffer==null) {
                throw new ArgumentNullException("buffer"); 
            } 
            if (offset<0 || offset>buffer.Length) {
                throw new ArgumentOutOfRangeException("offset"); 
            }
            if (size<0 || size>buffer.Length-offset) {
                throw new ArgumentOutOfRangeException("size");
            } 
            if (size==0 || m_Closed) {
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "BeginRead", ""); 
                HttpRequestStreamAsyncResult result = new HttpRequestStreamAsyncResult(this, state, callback); 
                result.InvokeCallback((uint) 0);
                return result; 
            }

            HttpRequestStreamAsyncResult asyncResult = null;
 
            uint dataRead = 0;
            if (m_DataChunkIndex != -1) { 
                dataRead = UnsafeNclNativeMethods.HttpApi.GetChunks(m_HttpContext.Request.RequestBuffer, m_HttpContext.Request.OriginalBlobAddress, ref m_DataChunkIndex, ref m_DataChunkOffset, buffer, offset, size); 
                if (m_DataChunkIndex != -1 && dataRead == size) {
                    asyncResult = new HttpRequestStreamAsyncResult(this, state, callback, buffer, offset, (uint)size, 0); 
                    asyncResult.InvokeCallback(dataRead);
                }
            }
 
            if(m_DataChunkIndex == -1 && dataRead < size){
                GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::BeginRead() size:" + size + " offset:" + offset); 
                uint statusCode = 0; 
                offset += (int)dataRead;
                size -= (int)dataRead; 

                //the http.sys team recommends that we limit the size to 128kb
                if(size > MaxReadSize){
                    size = MaxReadSize; 
                }
 
                asyncResult = new HttpRequestStreamAsyncResult(this, state, callback, buffer, offset, (uint)size, dataRead); 

                fixed (byte *pBuffer = buffer) { 
                    // issue unmanaged blocking call
                    GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::BeginRead() calling UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody");

                    m_HttpContext.EnsureBoundHandle(); 
                    GlobalLog.Assert(asyncResult.m_pOverlapped->InternalLow != (IntPtr) 0x0103, "HttpRequestStream#{0}::BeginRead()|Overlapped structure STATUS_PENDING before use.  0x{1:x}", ValidationHelper.HashString(this), (IntPtr) asyncResult.m_pOverlapped);
                    statusCode = 
                        UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody( 
                            m_HttpContext.RequestQueueHandle,
                            m_HttpContext.RequestId, 
                            (uint)UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
                            asyncResult.m_pPinnedBuffer,
                            (uint)size,
                            null, 
                            asyncResult.m_pOverlapped);
 
                    GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::BeginRead() call to UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody returned:" + statusCode + " dataRead:" + dataRead); 
                }
                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) { 
                    Exception exception = new HttpListenerException((int)statusCode);
                    if(Logging.On)Logging.Exception(Logging.HttpListener, this, "BeginRead", exception);
                    throw exception;
                } 
                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) {
                    // WORKAROUND: http.sys HttpReceiveRequestEntityBody has a bug where InternalLow gets set to STATUS_PENDING when it shouldn't. 
                    // This causes an MDA to fire in the CLR (as well as the assert in NclUtilities.OverlappedFree). 
                    // Explicitly reset InternalLow as if it hadn't been touched.
                    asyncResult.m_pOverlapped->InternalLow = IntPtr.Zero; 

                    asyncResult.InternalCleanup();
                    asyncResult = new HttpRequestStreamAsyncResult(this, state, callback);
                    asyncResult.InvokeCallback((uint)0); 
                }
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "BeginRead", ""); 
            return asyncResult;
        } 

        public override int EndRead(IAsyncResult asyncResult) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "EndRead", "");
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::EndRead() asyncResult#" + ValidationHelper.HashString(asyncResult)); 
            if (asyncResult==null) {
                throw new ArgumentNullException("asyncResult"); 
            } 
            HttpRequestStreamAsyncResult castedAsyncResult = asyncResult as HttpRequestStreamAsyncResult;
            if (castedAsyncResult==null || castedAsyncResult.AsyncObject!=this) { 
                throw new ArgumentException(SR.GetString(SR.net_io_invalidasyncresult), "asyncResult");
            }
            if (castedAsyncResult.EndCalled) {
                throw new InvalidOperationException(SR.GetString(SR.net_io_invalidendcall, "EndRead")); 
            }
            castedAsyncResult.EndCalled = true; 
            // wait & then check for errors 
            object returnValue = castedAsyncResult.InternalWaitForCompletion();
            Exception exception = returnValue as Exception; 
            if (exception!=null) {
                GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::EndRead() rethrowing exception:" + exception);
                if(Logging.On)Logging.Exception(Logging.HttpListener, this, "EndRead", exception);
                throw exception; 
            }
            // 
 

            uint dataRead = (uint)returnValue; 
            UpdateAfterRead((uint)castedAsyncResult.ErrorCode, dataRead);
            GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::EndRead() returning returnValue:" + ValidationHelper.ToString(returnValue));
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "EndRead", "");
            return (int)dataRead + (int)castedAsyncResult.m_dataAlreadyRead; 
        }
 
        public override void Write(byte[] buffer, int offset, int size) { 
            throw new InvalidOperationException(SR.GetString(SR.net_readonlystream));
        } 


        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state) { 
            throw new InvalidOperationException(SR.GetString(SR.net_readonlystream));
        } 
 
        public override void EndWrite(IAsyncResult asyncResult) {
            throw new InvalidOperationException(SR.GetString(SR.net_readonlystream)); 
        }

        protected override void Dispose(bool disposing) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Dispose", ""); 
            try {
                GlobalLog.Print("HttpRequestStream#" + ValidationHelper.HashString(this) + "::Dispose(bool) m_Closed:" + m_Closed); 
                m_Closed = true; 
            }
            finally { 
                base.Dispose(disposing);
            }
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Dispose", "");
        } 

        unsafe class HttpRequestStreamAsyncResult : LazyAsyncResult { 
            internal NativeOverlapped* m_pOverlapped; 
            internal void* m_pPinnedBuffer;
            internal uint m_dataAlreadyRead = 0; 

            private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(Callback);

            internal HttpRequestStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback) : base(asyncObject, userState, callback) { 
            }
 
            internal HttpRequestStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback, byte[] buffer, int offset, uint size, uint dataAlreadyRead): base(asyncObject, userState, callback) { 
                m_dataAlreadyRead = dataAlreadyRead;
                Overlapped overlapped = new Overlapped(); 
                overlapped.AsyncResult = this;
                m_pOverlapped = overlapped.Pack(s_IOCallback, buffer);
                m_pPinnedBuffer = (void*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
            } 

            private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped) { 
                Overlapped callbackOverlapped = Overlapped.Unpack(nativeOverlapped); 
                HttpRequestStreamAsyncResult asyncResult = callbackOverlapped.AsyncResult as HttpRequestStreamAsyncResult;
                GlobalLog.Print("HttpRequestStreamAsyncResult#" + ValidationHelper.HashString(asyncResult) + "::Callback() errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes + " nativeOverlapped:0x" + ((IntPtr)nativeOverlapped).ToString("x8")); 
                object result = null;
                try {
                    if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) {
                        asyncResult.ErrorCode = (int)errorCode; 
                        result = new HttpListenerException((int)errorCode);
                    } 
                    else { 
                        result = numBytes;
                        if(Logging.On)Logging.Dump(Logging.HttpListener, asyncResult, "Callback", (IntPtr)asyncResult.m_pPinnedBuffer, (int)numBytes); 
                    }
                    GlobalLog.Print("HttpRequestStreamAsyncResult#" + ValidationHelper.HashString(asyncResult) + "::Callback() calling Complete()");
                }
                catch (Exception e) { 
                    result = e;
                } 
                asyncResult.InvokeCallback(result); 
            }
 
            // Will be called from the base class upon InvokeCallback()
            protected override void Cleanup() {
                base.Cleanup();
                if (m_pOverlapped != null) { 
                    Overlapped.Free(m_pOverlapped);
                } 
            } 
        }
    } 
}
