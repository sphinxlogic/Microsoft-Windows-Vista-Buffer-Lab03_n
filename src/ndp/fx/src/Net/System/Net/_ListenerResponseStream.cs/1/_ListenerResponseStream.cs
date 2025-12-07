// ------------------------------------------------------------------------------ 
// <copyright file="_HttpResponseStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Threading;
    using System.Collections;
    using System.Globalization;
    using System.Security.Permissions; 

    unsafe class HttpResponseStream : Stream { 
        private HttpListenerContext m_HttpContext; 
        private long m_LeftToWrite = long.MinValue;
        private bool m_Closed; 

        internal HttpResponseStream(HttpListenerContext httpContext) {
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::.ctor() HttpListenerContext##" + ValidationHelper.HashString(httpContext));
            m_HttpContext = httpContext; 
        }
 
        internal UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS ComputeLeftToWrite() { 
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::ComputeLeftToWrite() on entry m_LeftToWrite:" + m_LeftToWrite);
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE; 
            if (!m_HttpContext.Response.ComputedHeaders) {
                flags = m_HttpContext.Response.ComputeHeaders();
            }
            if (m_LeftToWrite==long.MinValue) { 
                UnsafeNclNativeMethods.HttpApi.HTTP_VERB method = m_HttpContext.GetKnownMethod();
                m_LeftToWrite = method != UnsafeNclNativeMethods.HttpApi.HTTP_VERB.HttpVerbHEAD ? m_HttpContext.Response.ContentLength64 : 0; 
                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::ComputeLeftToWrite() computed m_LeftToWrite:" + m_LeftToWrite); 
                if (m_LeftToWrite==0) {
                    Close(); 
                }
                else if (method==UnsafeNclNativeMethods.HttpApi.HTTP_VERB.HttpVerbOPTIONS && m_LeftToWrite > 0) {
                    throw new ProtocolViolationException(SR.GetString(SR.net_nouploadonget));
                } 
            }
            return flags; 
        } 

        public override bool CanSeek { 
            get {
                return false;
            }
        } 

        public override bool CanWrite { 
            get { 
                return true;
            } 
        }

        public override bool CanRead {
            get { 
                return false;
            } 
        } 

        /* 
        public bool DataAvailable {
            get {
                throw new InvalidOperationException(SR.GetString(SR.net_writeonlystream));
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
            throw new InvalidOperationException(SR.GetString(SR.net_writeonlystream)); 
        }

        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state) { 
            throw new InvalidOperationException(SR.GetString(SR.net_writeonlystream));
        } 
 
        public override int EndRead(IAsyncResult asyncResult) {
            throw new InvalidOperationException(SR.GetString(SR.net_writeonlystream)); 
        }

        public override void Write(byte[] buffer, int offset, int size) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Write", ""); 
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Write() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset);
            if (buffer==null) { 
                throw new ArgumentNullException("buffer"); 
            }
            if (offset<0 || offset>buffer.Length) { 
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size<0 || size>buffer.Length-offset) {
                throw new ArgumentOutOfRangeException("size"); 
            }
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite(); 
            if (size==0 || m_Closed) { 
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Write", "");
                return; 
            }
            if (m_LeftToWrite>0 && size>m_LeftToWrite) {
                throw new ProtocolViolationException(SR.GetString(SR.net_entitytoobig));
            } 

            uint statusCode; 
            uint dataToWrite = (uint)size; 
            SafeLocalFree bufferAsIntPtr = null;
            IntPtr pBufferAsIntPtr = IntPtr.Zero; 
            bool sentHeaders = m_HttpContext.Response.SentHeaders;
            try {
                fixed (byte *pDataBuffer = buffer) {
                    byte *pBuffer = pDataBuffer; 
                    if (m_HttpContext.Response.BoundaryType==BoundaryType.Chunked) {
                        // 
 

                        string chunkHeader = size.ToString("x", CultureInfo.InvariantCulture); 
                        dataToWrite = dataToWrite + (uint)(chunkHeader.Length + 4);
                        bufferAsIntPtr = SafeLocalFree.LocalAlloc((int)dataToWrite);
                        pBufferAsIntPtr = bufferAsIntPtr.DangerousGetHandle();
                        for (int i=0; i<chunkHeader.Length; i++) { 
                            Marshal.WriteByte(pBufferAsIntPtr, i, (byte)chunkHeader[i]);
                        } 
                        Marshal.WriteInt16(pBufferAsIntPtr, chunkHeader.Length, 0x0A0D); 
                        Marshal.Copy(buffer, offset, IntPtrHelper.Add(pBufferAsIntPtr, chunkHeader.Length + 2), size);
                        Marshal.WriteInt16(pBufferAsIntPtr, (int)(dataToWrite - 2), 0x0A0D); 
                        pBuffer = (byte*)pBufferAsIntPtr;
                        offset = 0;
                    }
                    UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK dataChunk = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK(); 
                    dataChunk.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    dataChunk.pBuffer = (byte*)(pBuffer + offset); 
                    dataChunk.BufferLength = dataToWrite; 

                    flags |= m_LeftToWrite==size ? UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE : UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA; 
                    if (!sentHeaders) {
                        statusCode = m_HttpContext.Response.SendHeaders(&dataChunk, null, flags);
                    }
                    else { 
                        GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Write() calling UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody");
 
                        statusCode = 
                            UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                                m_HttpContext.RequestQueueHandle, 
                                m_HttpContext.RequestId,
                                (uint)flags,
                                1,
                                &dataChunk, 
                                null,
                                SafeLocalFree.Zero, 
                                0, 
                                null,
                                null); 

                        GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Write() call to UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody returned:" + statusCode);
                        if (m_HttpContext.Listener.IgnoreWriteExceptions) {
                            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Write() suppressing error"); 
                            statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;
                        } 
                    } 
                }
            } 
            finally {
                if (bufferAsIntPtr != null) {
                    // free unmanaged buffer
                    bufferAsIntPtr.Close(); 
                }
            } 
 
            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) {
                Exception exception = new HttpListenerException((int)statusCode); 
                if(Logging.On)Logging.Exception(Logging.HttpListener, this, "Write", exception);
                m_HttpContext.Abort();
                throw exception;
            } 
            UpdateAfterWrite(dataToWrite);
            if(Logging.On)Logging.Dump(Logging.HttpListener, this, "Write", buffer, offset, (int)dataToWrite); 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Write", ""); 
        }
 

        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state) {
           GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::BeginWrite() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset); 
            if (buffer==null) {
                throw new ArgumentNullException("buffer"); 
            } 
            if (offset<0 || offset>buffer.Length) {
                throw new ArgumentOutOfRangeException("offset"); 
            }
            if (size<0 || size>buffer.Length-offset) {
                throw new ArgumentOutOfRangeException("size");
            } 
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            if (size==0 || m_Closed) { 
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "BeginWrite", ""); 
                HttpResponseStreamAsyncResult result = new HttpResponseStreamAsyncResult(this, state, callback);
                result.InvokeCallback((uint) 0); 
                return result;
            }
            if (m_LeftToWrite>0 && size>m_LeftToWrite) {
                throw new ProtocolViolationException(SR.GetString(SR.net_entitytoobig)); 
            }
 
            uint statusCode; 
            flags |= m_LeftToWrite==size ? UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE : UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
            bool sentHeaders = m_HttpContext.Response.SentHeaders; 
            HttpResponseStreamAsyncResult asyncResult = new HttpResponseStreamAsyncResult(this, state, callback, buffer, offset, size, m_HttpContext.Response.BoundaryType==BoundaryType.Chunked, sentHeaders);
            if (!sentHeaders) {
                statusCode = m_HttpContext.Response.SendHeaders(null, asyncResult, flags);
            } 
            else {
                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::BeginWrite() calling UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody"); 
 
                m_HttpContext.EnsureBoundHandle();
                statusCode = 
                    UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                        m_HttpContext.RequestQueueHandle,
                        m_HttpContext.RequestId,
                        (uint)flags, 
                        asyncResult.dataChunkCount,
                        asyncResult.pDataChunks, 
                        null, 
                        SafeLocalFree.Zero,
                        0, 
                        asyncResult.m_pOverlapped,
                        null);

                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::BeginWrite() call to UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody returned:" + statusCode); 
                if (m_HttpContext.Listener.IgnoreWriteExceptions) {
                    GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::BeginWrite() suppressing error"); 
                    statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS; 
                }
            } 
            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING) {
                Exception exception = new HttpListenerException((int)statusCode);
                if(Logging.On)Logging.Exception(Logging.HttpListener, this, "BeginWrite", exception);
                m_HttpContext.Abort(); 
                throw exception;
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "BeginWrite", ""); 
            return asyncResult;
        } 

        public override void EndWrite(IAsyncResult asyncResult) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "EndWrite", "");
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::EndWrite() asyncResult#" + ValidationHelper.HashString(asyncResult)); 
            if (asyncResult==null) {
                throw new ArgumentNullException("asyncResult"); 
            } 
            HttpResponseStreamAsyncResult castedAsyncResult = asyncResult as HttpResponseStreamAsyncResult;
            if (castedAsyncResult==null || castedAsyncResult.AsyncObject!=this) { 
                throw new ArgumentException(SR.GetString(SR.net_io_invalidasyncresult), "asyncResult");
            }
            if (castedAsyncResult.EndCalled) {
                throw new InvalidOperationException(SR.GetString(SR.net_io_invalidendcall, "EndWrite")); 
            }
            castedAsyncResult.EndCalled = true; 
            // wait & then check for errors 
            object returnValue = castedAsyncResult.InternalWaitForCompletion();
 
            Exception exception = returnValue as Exception;
            if (exception!=null) {
                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::EndWrite() rethrowing exception:" + exception);
                if(Logging.On)Logging.Exception(Logging.HttpListener, this, "EndWrite", exception); 
                m_HttpContext.Abort();
                throw exception; 
            } 
            //
 

            UpdateAfterWrite((uint)returnValue);
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::EndWrite()");
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "EndWrite", ""); 
        }
 
        void UpdateAfterWrite(uint dataWritten) { 
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::UpdateAfterWrite() dataWritten:" + dataWritten + " m_LeftToWrite:" + m_LeftToWrite + " m_Closed:" + m_Closed);
            if (m_LeftToWrite>0) { 
                // keep track of the data transferred
                m_LeftToWrite -= dataWritten;
            }
            if (m_LeftToWrite==0) { 
                // in this case we already passed 0 as the flag, so we don't need to call HttpSendResponseEntityBody() when we Close()
                m_Closed = true; 
            } 
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::UpdateAfterWrite() dataWritten:" + dataWritten + " m_LeftToWrite:" + m_LeftToWrite + " m_Closed:" + m_Closed);
        } 

        protected override void Dispose(bool disposing) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Close", "");
 
            try {
                if(disposing){ 
                    GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Close() m_Closed:" + m_Closed); 
                    if (m_Closed) {
                        if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Close", ""); 
                        return;
                    }
                    m_Closed = true;
                    UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite(); 
                    if (m_LeftToWrite>0) {
                        throw new InvalidOperationException(SR.GetString(SR.net_io_notenoughbyteswritten)); 
                    } 
                    bool sentHeaders = m_HttpContext.Response.SentHeaders;
                    if (sentHeaders && m_LeftToWrite==0) { 
                        if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Close", "");
                        return;
                    }
 
                    uint statusCode = 0;
                    if ((m_HttpContext.Response.BoundaryType==BoundaryType.Chunked || m_HttpContext.Response.BoundaryType==BoundaryType.None) && (String.Compare(m_HttpContext.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase)!=0)) { 
                        if (m_HttpContext.Response.BoundaryType==BoundaryType.None) { 
                            flags |= UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
                        } 
                        fixed (void* pBuffer = NclConstants.ChunkTerminator)
                        {
                            UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK* pDataChunk = null;
                            if (m_HttpContext.Response.BoundaryType==BoundaryType.Chunked) { 
                                UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK dataChunk = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                                dataChunk.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory; 
                                dataChunk.pBuffer = (byte *)pBuffer; 
                                dataChunk.BufferLength = (uint) NclConstants.ChunkTerminator.Length;
                                pDataChunk = &dataChunk; 
                            }
                            if (!sentHeaders) {
                                statusCode = m_HttpContext.Response.SendHeaders(pDataChunk, null, flags);
                            } 
                            else {
                                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Close() calling UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody"); 
 
                                statusCode =
                                    UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody( 
                                        m_HttpContext.RequestQueueHandle,
                                        m_HttpContext.RequestId,
                                        (uint)flags,
                                        pDataChunk!=null ? (ushort)1 : (ushort)0, 
                                        pDataChunk,
                                        null, 
                                        SafeLocalFree.Zero, 
                                        0,
                                        null, 
                                        null);

                                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Close() call to UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody returned:" + statusCode);
                                if (m_HttpContext.Listener.IgnoreWriteExceptions) { 
                                    GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Close() suppressing error");
                                    statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS; 
                                } 
                            }
                        } 
                    }
                    else {
                        if (!sentHeaders) {
                            statusCode = m_HttpContext.Response.SendHeaders(null, null, flags); 
                        }
                    } 
                    if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) { 
                        Exception exception = new HttpListenerException((int)statusCode);
                        if(Logging.On)Logging.Exception(Logging.HttpListener, this, "Close", exception); 
                        m_HttpContext.Abort();
                        throw exception;
                    }
                    m_LeftToWrite = 0; 
                }
            } 
            finally { 
                base.Dispose(disposing);
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Dispose", "");
        }
    }
 
    unsafe class HttpResponseStreamAsyncResult : LazyAsyncResult {
        internal NativeOverlapped* m_pOverlapped; 
        private UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK[] m_DataChunks; 
        internal bool m_SentHeaders;
 
        private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(Callback);

        internal ushort dataChunkCount {
            get { 
                return (ushort)m_DataChunks.Length;
            } 
        } 
        internal UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK* pDataChunks {
            get { 
                return (UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK*)(Marshal.UnsafeAddrOfPinnedArrayElement(m_DataChunks, 0));
            }
        }
 
        internal HttpResponseStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback) : base(asyncObject, userState, callback) {
        } 
 

        internal HttpResponseStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback, byte[] buffer, int offset, int size, bool chunked, bool sentHeaders): base(asyncObject, userState, callback){ 
            m_SentHeaders = sentHeaders;
            Overlapped overlapped = new Overlapped();
            overlapped.AsyncResult = this;
 
            m_DataChunks = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK[chunked ? 3 : 1];
 
            GlobalLog.Print("HttpResponseStreamAsyncResult#" + ValidationHelper.HashString(this) + "::.ctor() m_pOverlapped:0x" + ((IntPtr)m_pOverlapped).ToString("x8")); 

            object[] objectsToPin = new object[1 + m_DataChunks.Length]; 
            objectsToPin[m_DataChunks.Length] = m_DataChunks;


            int chunkHeaderOffset = 0; 
            byte[] chunkHeaderBuffer = null;
            if (chunked) { 
                chunkHeaderBuffer = ConnectStream.GetChunkHeader(size, out chunkHeaderOffset); 

                m_DataChunks[0] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK(); 
                m_DataChunks[0].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                m_DataChunks[0].BufferLength = (uint)(chunkHeaderBuffer.Length - chunkHeaderOffset);

                objectsToPin[0] = chunkHeaderBuffer; 

                m_DataChunks[1] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK(); 
                m_DataChunks[1].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory; 
                m_DataChunks[1].BufferLength = (uint)size;
 
                objectsToPin[1] = buffer;

                m_DataChunks[2] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                m_DataChunks[2].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory; 
                m_DataChunks[2].BufferLength = (uint) NclConstants.CRLF.Length;
 
                objectsToPin[2] = NclConstants.CRLF; 

            } 
            else {
                m_DataChunks[0] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                m_DataChunks[0].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                m_DataChunks[0].BufferLength = (uint)size; 

                objectsToPin[0] = buffer; 
            } 

            // This call will pin needed memory 
            m_pOverlapped = overlapped.Pack(s_IOCallback, objectsToPin);

            if (chunked)
            { 
                m_DataChunks[0].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer, chunkHeaderOffset));
                m_DataChunks[1].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset)); 
                m_DataChunks[2].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(NclConstants.CRLF, 0)); 
            }
            else 
            {
                m_DataChunks[0].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
            }
 
        }
 
        private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped) { 
            Overlapped callbackOverlapped = Overlapped.Unpack(nativeOverlapped);
            HttpResponseStreamAsyncResult asyncResult = callbackOverlapped.AsyncResult as HttpResponseStreamAsyncResult; 
            GlobalLog.Print("HttpResponseStreamAsyncResult#" + ValidationHelper.HashString(asyncResult) + "::Callback() errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes + " nativeOverlapped:0x" + ((IntPtr)nativeOverlapped).ToString("x8"));
            object result = null;
            try {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) { 
                    asyncResult.ErrorCode = (int)errorCode;
                    result = new HttpListenerException((int)errorCode); 
                } 
                else {
                    // if we sent headers and body together, numBytes will be the total, but we need to only account for the data 
                    // result = numBytes;
                    result = asyncResult.m_DataChunks.Length==1 ? asyncResult.m_DataChunks[0].BufferLength : 0;
                    if(Logging.On){for (int i=0; i<asyncResult.m_DataChunks.Length; i++){Logging.Dump(Logging.HttpListener, asyncResult, "Callback",(IntPtr)asyncResult.m_DataChunks[0].pBuffer, (int)asyncResult.m_DataChunks[0].BufferLength);}}
                } 
                GlobalLog.Print("HttpResponseStreamAsyncResult#" + ValidationHelper.HashString(asyncResult) + "::Callback() calling Complete()");
            } 
            catch (Exception e) { 
                result = e;
            } 
            catch {
                result = new Exception(SR.GetString(SR.net_nonClsCompliantException));
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
// ------------------------------------------------------------------------------ 
// <copyright file="_HttpResponseStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Threading;
    using System.Collections;
    using System.Globalization;
    using System.Security.Permissions; 

    unsafe class HttpResponseStream : Stream { 
        private HttpListenerContext m_HttpContext; 
        private long m_LeftToWrite = long.MinValue;
        private bool m_Closed; 

        internal HttpResponseStream(HttpListenerContext httpContext) {
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::.ctor() HttpListenerContext##" + ValidationHelper.HashString(httpContext));
            m_HttpContext = httpContext; 
        }
 
        internal UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS ComputeLeftToWrite() { 
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::ComputeLeftToWrite() on entry m_LeftToWrite:" + m_LeftToWrite);
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE; 
            if (!m_HttpContext.Response.ComputedHeaders) {
                flags = m_HttpContext.Response.ComputeHeaders();
            }
            if (m_LeftToWrite==long.MinValue) { 
                UnsafeNclNativeMethods.HttpApi.HTTP_VERB method = m_HttpContext.GetKnownMethod();
                m_LeftToWrite = method != UnsafeNclNativeMethods.HttpApi.HTTP_VERB.HttpVerbHEAD ? m_HttpContext.Response.ContentLength64 : 0; 
                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::ComputeLeftToWrite() computed m_LeftToWrite:" + m_LeftToWrite); 
                if (m_LeftToWrite==0) {
                    Close(); 
                }
                else if (method==UnsafeNclNativeMethods.HttpApi.HTTP_VERB.HttpVerbOPTIONS && m_LeftToWrite > 0) {
                    throw new ProtocolViolationException(SR.GetString(SR.net_nouploadonget));
                } 
            }
            return flags; 
        } 

        public override bool CanSeek { 
            get {
                return false;
            }
        } 

        public override bool CanWrite { 
            get { 
                return true;
            } 
        }

        public override bool CanRead {
            get { 
                return false;
            } 
        } 

        /* 
        public bool DataAvailable {
            get {
                throw new InvalidOperationException(SR.GetString(SR.net_writeonlystream));
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
            throw new InvalidOperationException(SR.GetString(SR.net_writeonlystream)); 
        }

        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state) { 
            throw new InvalidOperationException(SR.GetString(SR.net_writeonlystream));
        } 
 
        public override int EndRead(IAsyncResult asyncResult) {
            throw new InvalidOperationException(SR.GetString(SR.net_writeonlystream)); 
        }

        public override void Write(byte[] buffer, int offset, int size) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Write", ""); 
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Write() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset);
            if (buffer==null) { 
                throw new ArgumentNullException("buffer"); 
            }
            if (offset<0 || offset>buffer.Length) { 
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size<0 || size>buffer.Length-offset) {
                throw new ArgumentOutOfRangeException("size"); 
            }
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite(); 
            if (size==0 || m_Closed) { 
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Write", "");
                return; 
            }
            if (m_LeftToWrite>0 && size>m_LeftToWrite) {
                throw new ProtocolViolationException(SR.GetString(SR.net_entitytoobig));
            } 

            uint statusCode; 
            uint dataToWrite = (uint)size; 
            SafeLocalFree bufferAsIntPtr = null;
            IntPtr pBufferAsIntPtr = IntPtr.Zero; 
            bool sentHeaders = m_HttpContext.Response.SentHeaders;
            try {
                fixed (byte *pDataBuffer = buffer) {
                    byte *pBuffer = pDataBuffer; 
                    if (m_HttpContext.Response.BoundaryType==BoundaryType.Chunked) {
                        // 
 

                        string chunkHeader = size.ToString("x", CultureInfo.InvariantCulture); 
                        dataToWrite = dataToWrite + (uint)(chunkHeader.Length + 4);
                        bufferAsIntPtr = SafeLocalFree.LocalAlloc((int)dataToWrite);
                        pBufferAsIntPtr = bufferAsIntPtr.DangerousGetHandle();
                        for (int i=0; i<chunkHeader.Length; i++) { 
                            Marshal.WriteByte(pBufferAsIntPtr, i, (byte)chunkHeader[i]);
                        } 
                        Marshal.WriteInt16(pBufferAsIntPtr, chunkHeader.Length, 0x0A0D); 
                        Marshal.Copy(buffer, offset, IntPtrHelper.Add(pBufferAsIntPtr, chunkHeader.Length + 2), size);
                        Marshal.WriteInt16(pBufferAsIntPtr, (int)(dataToWrite - 2), 0x0A0D); 
                        pBuffer = (byte*)pBufferAsIntPtr;
                        offset = 0;
                    }
                    UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK dataChunk = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK(); 
                    dataChunk.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    dataChunk.pBuffer = (byte*)(pBuffer + offset); 
                    dataChunk.BufferLength = dataToWrite; 

                    flags |= m_LeftToWrite==size ? UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE : UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA; 
                    if (!sentHeaders) {
                        statusCode = m_HttpContext.Response.SendHeaders(&dataChunk, null, flags);
                    }
                    else { 
                        GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Write() calling UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody");
 
                        statusCode = 
                            UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                                m_HttpContext.RequestQueueHandle, 
                                m_HttpContext.RequestId,
                                (uint)flags,
                                1,
                                &dataChunk, 
                                null,
                                SafeLocalFree.Zero, 
                                0, 
                                null,
                                null); 

                        GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Write() call to UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody returned:" + statusCode);
                        if (m_HttpContext.Listener.IgnoreWriteExceptions) {
                            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Write() suppressing error"); 
                            statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;
                        } 
                    } 
                }
            } 
            finally {
                if (bufferAsIntPtr != null) {
                    // free unmanaged buffer
                    bufferAsIntPtr.Close(); 
                }
            } 
 
            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) {
                Exception exception = new HttpListenerException((int)statusCode); 
                if(Logging.On)Logging.Exception(Logging.HttpListener, this, "Write", exception);
                m_HttpContext.Abort();
                throw exception;
            } 
            UpdateAfterWrite(dataToWrite);
            if(Logging.On)Logging.Dump(Logging.HttpListener, this, "Write", buffer, offset, (int)dataToWrite); 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Write", ""); 
        }
 

        [HostProtection(ExternalThreading=true)]
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state) {
           GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::BeginWrite() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset); 
            if (buffer==null) {
                throw new ArgumentNullException("buffer"); 
            } 
            if (offset<0 || offset>buffer.Length) {
                throw new ArgumentOutOfRangeException("offset"); 
            }
            if (size<0 || size>buffer.Length-offset) {
                throw new ArgumentOutOfRangeException("size");
            } 
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            if (size==0 || m_Closed) { 
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "BeginWrite", ""); 
                HttpResponseStreamAsyncResult result = new HttpResponseStreamAsyncResult(this, state, callback);
                result.InvokeCallback((uint) 0); 
                return result;
            }
            if (m_LeftToWrite>0 && size>m_LeftToWrite) {
                throw new ProtocolViolationException(SR.GetString(SR.net_entitytoobig)); 
            }
 
            uint statusCode; 
            flags |= m_LeftToWrite==size ? UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE : UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
            bool sentHeaders = m_HttpContext.Response.SentHeaders; 
            HttpResponseStreamAsyncResult asyncResult = new HttpResponseStreamAsyncResult(this, state, callback, buffer, offset, size, m_HttpContext.Response.BoundaryType==BoundaryType.Chunked, sentHeaders);
            if (!sentHeaders) {
                statusCode = m_HttpContext.Response.SendHeaders(null, asyncResult, flags);
            } 
            else {
                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::BeginWrite() calling UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody"); 
 
                m_HttpContext.EnsureBoundHandle();
                statusCode = 
                    UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                        m_HttpContext.RequestQueueHandle,
                        m_HttpContext.RequestId,
                        (uint)flags, 
                        asyncResult.dataChunkCount,
                        asyncResult.pDataChunks, 
                        null, 
                        SafeLocalFree.Zero,
                        0, 
                        asyncResult.m_pOverlapped,
                        null);

                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::BeginWrite() call to UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody returned:" + statusCode); 
                if (m_HttpContext.Listener.IgnoreWriteExceptions) {
                    GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::BeginWrite() suppressing error"); 
                    statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS; 
                }
            } 
            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING) {
                Exception exception = new HttpListenerException((int)statusCode);
                if(Logging.On)Logging.Exception(Logging.HttpListener, this, "BeginWrite", exception);
                m_HttpContext.Abort(); 
                throw exception;
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "BeginWrite", ""); 
            return asyncResult;
        } 

        public override void EndWrite(IAsyncResult asyncResult) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "EndWrite", "");
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::EndWrite() asyncResult#" + ValidationHelper.HashString(asyncResult)); 
            if (asyncResult==null) {
                throw new ArgumentNullException("asyncResult"); 
            } 
            HttpResponseStreamAsyncResult castedAsyncResult = asyncResult as HttpResponseStreamAsyncResult;
            if (castedAsyncResult==null || castedAsyncResult.AsyncObject!=this) { 
                throw new ArgumentException(SR.GetString(SR.net_io_invalidasyncresult), "asyncResult");
            }
            if (castedAsyncResult.EndCalled) {
                throw new InvalidOperationException(SR.GetString(SR.net_io_invalidendcall, "EndWrite")); 
            }
            castedAsyncResult.EndCalled = true; 
            // wait & then check for errors 
            object returnValue = castedAsyncResult.InternalWaitForCompletion();
 
            Exception exception = returnValue as Exception;
            if (exception!=null) {
                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::EndWrite() rethrowing exception:" + exception);
                if(Logging.On)Logging.Exception(Logging.HttpListener, this, "EndWrite", exception); 
                m_HttpContext.Abort();
                throw exception; 
            } 
            //
 

            UpdateAfterWrite((uint)returnValue);
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::EndWrite()");
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "EndWrite", ""); 
        }
 
        void UpdateAfterWrite(uint dataWritten) { 
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::UpdateAfterWrite() dataWritten:" + dataWritten + " m_LeftToWrite:" + m_LeftToWrite + " m_Closed:" + m_Closed);
            if (m_LeftToWrite>0) { 
                // keep track of the data transferred
                m_LeftToWrite -= dataWritten;
            }
            if (m_LeftToWrite==0) { 
                // in this case we already passed 0 as the flag, so we don't need to call HttpSendResponseEntityBody() when we Close()
                m_Closed = true; 
            } 
            GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::UpdateAfterWrite() dataWritten:" + dataWritten + " m_LeftToWrite:" + m_LeftToWrite + " m_Closed:" + m_Closed);
        } 

        protected override void Dispose(bool disposing) {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Close", "");
 
            try {
                if(disposing){ 
                    GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Close() m_Closed:" + m_Closed); 
                    if (m_Closed) {
                        if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Close", ""); 
                        return;
                    }
                    m_Closed = true;
                    UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite(); 
                    if (m_LeftToWrite>0) {
                        throw new InvalidOperationException(SR.GetString(SR.net_io_notenoughbyteswritten)); 
                    } 
                    bool sentHeaders = m_HttpContext.Response.SentHeaders;
                    if (sentHeaders && m_LeftToWrite==0) { 
                        if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Close", "");
                        return;
                    }
 
                    uint statusCode = 0;
                    if ((m_HttpContext.Response.BoundaryType==BoundaryType.Chunked || m_HttpContext.Response.BoundaryType==BoundaryType.None) && (String.Compare(m_HttpContext.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase)!=0)) { 
                        if (m_HttpContext.Response.BoundaryType==BoundaryType.None) { 
                            flags |= UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
                        } 
                        fixed (void* pBuffer = NclConstants.ChunkTerminator)
                        {
                            UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK* pDataChunk = null;
                            if (m_HttpContext.Response.BoundaryType==BoundaryType.Chunked) { 
                                UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK dataChunk = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                                dataChunk.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory; 
                                dataChunk.pBuffer = (byte *)pBuffer; 
                                dataChunk.BufferLength = (uint) NclConstants.ChunkTerminator.Length;
                                pDataChunk = &dataChunk; 
                            }
                            if (!sentHeaders) {
                                statusCode = m_HttpContext.Response.SendHeaders(pDataChunk, null, flags);
                            } 
                            else {
                                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Close() calling UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody"); 
 
                                statusCode =
                                    UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody( 
                                        m_HttpContext.RequestQueueHandle,
                                        m_HttpContext.RequestId,
                                        (uint)flags,
                                        pDataChunk!=null ? (ushort)1 : (ushort)0, 
                                        pDataChunk,
                                        null, 
                                        SafeLocalFree.Zero, 
                                        0,
                                        null, 
                                        null);

                                GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Close() call to UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody returned:" + statusCode);
                                if (m_HttpContext.Listener.IgnoreWriteExceptions) { 
                                    GlobalLog.Print("HttpResponseStream#" + ValidationHelper.HashString(this) + "::Close() suppressing error");
                                    statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS; 
                                } 
                            }
                        } 
                    }
                    else {
                        if (!sentHeaders) {
                            statusCode = m_HttpContext.Response.SendHeaders(null, null, flags); 
                        }
                    } 
                    if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) { 
                        Exception exception = new HttpListenerException((int)statusCode);
                        if(Logging.On)Logging.Exception(Logging.HttpListener, this, "Close", exception); 
                        m_HttpContext.Abort();
                        throw exception;
                    }
                    m_LeftToWrite = 0; 
                }
            } 
            finally { 
                base.Dispose(disposing);
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Dispose", "");
        }
    }
 
    unsafe class HttpResponseStreamAsyncResult : LazyAsyncResult {
        internal NativeOverlapped* m_pOverlapped; 
        private UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK[] m_DataChunks; 
        internal bool m_SentHeaders;
 
        private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(Callback);

        internal ushort dataChunkCount {
            get { 
                return (ushort)m_DataChunks.Length;
            } 
        } 
        internal UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK* pDataChunks {
            get { 
                return (UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK*)(Marshal.UnsafeAddrOfPinnedArrayElement(m_DataChunks, 0));
            }
        }
 
        internal HttpResponseStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback) : base(asyncObject, userState, callback) {
        } 
 

        internal HttpResponseStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback, byte[] buffer, int offset, int size, bool chunked, bool sentHeaders): base(asyncObject, userState, callback){ 
            m_SentHeaders = sentHeaders;
            Overlapped overlapped = new Overlapped();
            overlapped.AsyncResult = this;
 
            m_DataChunks = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK[chunked ? 3 : 1];
 
            GlobalLog.Print("HttpResponseStreamAsyncResult#" + ValidationHelper.HashString(this) + "::.ctor() m_pOverlapped:0x" + ((IntPtr)m_pOverlapped).ToString("x8")); 

            object[] objectsToPin = new object[1 + m_DataChunks.Length]; 
            objectsToPin[m_DataChunks.Length] = m_DataChunks;


            int chunkHeaderOffset = 0; 
            byte[] chunkHeaderBuffer = null;
            if (chunked) { 
                chunkHeaderBuffer = ConnectStream.GetChunkHeader(size, out chunkHeaderOffset); 

                m_DataChunks[0] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK(); 
                m_DataChunks[0].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                m_DataChunks[0].BufferLength = (uint)(chunkHeaderBuffer.Length - chunkHeaderOffset);

                objectsToPin[0] = chunkHeaderBuffer; 

                m_DataChunks[1] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK(); 
                m_DataChunks[1].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory; 
                m_DataChunks[1].BufferLength = (uint)size;
 
                objectsToPin[1] = buffer;

                m_DataChunks[2] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                m_DataChunks[2].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory; 
                m_DataChunks[2].BufferLength = (uint) NclConstants.CRLF.Length;
 
                objectsToPin[2] = NclConstants.CRLF; 

            } 
            else {
                m_DataChunks[0] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                m_DataChunks[0].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                m_DataChunks[0].BufferLength = (uint)size; 

                objectsToPin[0] = buffer; 
            } 

            // This call will pin needed memory 
            m_pOverlapped = overlapped.Pack(s_IOCallback, objectsToPin);

            if (chunked)
            { 
                m_DataChunks[0].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer, chunkHeaderOffset));
                m_DataChunks[1].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset)); 
                m_DataChunks[2].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(NclConstants.CRLF, 0)); 
            }
            else 
            {
                m_DataChunks[0].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
            }
 
        }
 
        private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped) { 
            Overlapped callbackOverlapped = Overlapped.Unpack(nativeOverlapped);
            HttpResponseStreamAsyncResult asyncResult = callbackOverlapped.AsyncResult as HttpResponseStreamAsyncResult; 
            GlobalLog.Print("HttpResponseStreamAsyncResult#" + ValidationHelper.HashString(asyncResult) + "::Callback() errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes + " nativeOverlapped:0x" + ((IntPtr)nativeOverlapped).ToString("x8"));
            object result = null;
            try {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF) { 
                    asyncResult.ErrorCode = (int)errorCode;
                    result = new HttpListenerException((int)errorCode); 
                } 
                else {
                    // if we sent headers and body together, numBytes will be the total, but we need to only account for the data 
                    // result = numBytes;
                    result = asyncResult.m_DataChunks.Length==1 ? asyncResult.m_DataChunks[0].BufferLength : 0;
                    if(Logging.On){for (int i=0; i<asyncResult.m_DataChunks.Length; i++){Logging.Dump(Logging.HttpListener, asyncResult, "Callback",(IntPtr)asyncResult.m_DataChunks[0].pBuffer, (int)asyncResult.m_DataChunks[0].BufferLength);}}
                } 
                GlobalLog.Print("HttpResponseStreamAsyncResult#" + ValidationHelper.HashString(asyncResult) + "::Callback() calling Complete()");
            } 
            catch (Exception e) { 
                result = e;
            } 
            catch {
                result = new Exception(SR.GetString(SR.net_nonClsCompliantException));
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
