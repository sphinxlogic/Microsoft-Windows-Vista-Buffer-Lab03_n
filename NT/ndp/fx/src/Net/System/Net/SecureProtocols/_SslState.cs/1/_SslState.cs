/*++ 
Copyright (c) 2000 Microsoft Corporation

Module Name:
 
    _SslState.cs
 
Abstract: 

 
Author:

    Mauro Ottaviani   07-Nov-2001
    Arthur Bierer     16-Nov-2001 
    Alexei Vopilov    26-Jun-2002
 
Revision History: 
    22-Aug-2003     Adopted for new Ssl feature design.
    15-Sept-2003    Implemented concurent rehanshake 

--*/

namespace System.Net.Security { 
    using System;
    using System.IO; 
    using System.Threading; 
    using System.Security.Cryptography.X509Certificates;
    using System.Collections; 
    using System.Runtime.InteropServices;
    using System.Globalization;
    using System.Net.Sockets;
    using System.Security.Authentication; 
    using System.ComponentModel;
 
    internal class SslState { 

        static int UniqueNameInteger = 123; 
        static AsyncProtocolCallback _PartialFrameCallback  = new AsyncProtocolCallback(PartialFrameCallback);
        static AsyncProtocolCallback _ReadFrameCallback     = new AsyncProtocolCallback(ReadFrameCallback);
        static AsyncCallback         _WriteCallback         = new AsyncCallback(WriteCallback);
 

        private RemoteCertValidationCallback    _CertValidationDelegate; 
        private LocalCertSelectionCallback      _CertSelectionDelegate; 

        private bool                            _CanRetryAuthentication;    // for now it's always false. 

        private Stream          _InnerStream;

        private _SslStream      _SecureStream; 

        private FixedSizeReader _Reader; 
 
        private int             _NestedAuth;
        private SecureChannel   _Context; 

        private bool            _HandshakeCompleted;
        private bool            _CertValidationFailed;
        private SecurityStatus  _SecurityStatus; 
        private Exception       _Exception;
 
        enum CachedSessionStatus: byte 
        {
            Unknown         = 0, 
            IsNotCached     = 1,
            IsCached        = 2,
            Renegotiated    = 3
        } 
        private CachedSessionStatus _CachedSession;
 
        // This block is used by rehandshake code to buffer data decryptred with the old key 
        private byte[]  _QueuedReadData;
        private int     _QueuedReadCount; 
        private bool    _PendingReHandshake;
        private const int _ConstMaxQueuedReadBytes = 1024 * 128;

        // 
        // This block is used to rule the >>re-handshakes<< that are concurent with read/write io requests
        // 
        private const int LockNone          = 0; 
        private const int LockWrite         = 1;
        private const int LockHandshake     = 2; 
        private const int LockPendingWrite  = 3;
        private const int LockRead          = 4;
        private const int LockPendingRead   = 6;
 
        private int     _LockWriteState;
        private object  _QueuedWriteStateRequest; 
 
        private object  _UserRequestedHandshake;
 
        private int     _LockReadState;
        private object  _QueuedReadStateRequest;

        // This is a perf trick ONLY for HTTPS TlsStream. 
        //
 
 
        private bool    _ForceBufferingLastHandshakePayload;
        private byte[]  _LastPayload; 

        //
        // This .ctor is only for internal TlsStream class, used only from client side ssl and it also has some special requirements.
        // 
        internal SslState(Stream innerStream, bool isHTTP): this(innerStream, null, null)
        { 
            _ForceBufferingLastHandshakePayload = isHTTP; 
        }
        // 
        //  The public Client and Server classes enforce the parameters rules  before
        //  calling into this .ctor.
        //
        internal SslState(Stream innerStream, RemoteCertValidationCallback certValidationCallback, LocalCertSelectionCallback  certSelectionCallback) 
        {
            _InnerStream = innerStream; 
            _Reader = new FixedSizeReader(innerStream); 
            _CertValidationDelegate = certValidationCallback;
            _CertSelectionDelegate  = certSelectionCallback; 

        }
        //
        // 
        //
        internal void ValidateCreateContext(bool isServer, string targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, bool remoteCertRequired, bool checkCertRevocationStatus) 
        { 
            ValidateCreateContext( isServer, targetHost, enabledSslProtocols, serverCertificate, clientCertificates, remoteCertRequired,
                                   checkCertRevocationStatus, !isServer); 
        }
        internal void ValidateCreateContext(bool isServer, string targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, bool remoteCertRequired, bool checkCertRevocationStatus, bool checkCertName)
        {
 
            //
            // We don;t support SSL alerts right now, hence any exception is fatal and cannot be retried 
            // Consider: make use if SSL alerts to re-sync SslStream on both sides for retrying. 
            //
            if (_Exception != null && !_CanRetryAuthentication) { 
                throw _Exception;
            }

            if (Context != null && Context.IsValidContext) { 
                throw new InvalidOperationException(SR.GetString(SR.net_auth_reauth));
            } 
 
            if (Context != null && IsServer != isServer) {
                throw new InvalidOperationException(SR.GetString(SR.net_auth_client_server)); 
            }

            if (targetHost == null) {
                throw new ArgumentNullException("targetHost"); 
            }
 
 
            if (isServer ) {
                enabledSslProtocols &= (SslProtocols)SchProtocols.ServerMask; 
                if (serverCertificate == null)
                    throw new ArgumentNullException("serverCertificate");
            }
            else { 
                enabledSslProtocols &= (SslProtocols)SchProtocols.ClientMask;
            } 
 
            if ((int)enabledSslProtocols == 0) {
                throw new ArgumentException(SR.GetString(SR.net_invalid_enum, "SslProtocolType"), "sslProtocolType"); 
            }

            if (clientCertificates == null) {
                clientCertificates = new X509CertificateCollection(); 
            }
 
            if (targetHost.Length == 0) { 
               targetHost = "?" + Interlocked.Increment(ref UniqueNameInteger).ToString(NumberFormatInfo.InvariantInfo);
            } 

            _Exception = null;
            try {
                _Context = new SecureChannel(targetHost, isServer, (SchProtocols)((int)enabledSslProtocols), serverCertificate, clientCertificates, remoteCertRequired, checkCertName, checkCertRevocationStatus, _CertSelectionDelegate); 
            }
            catch (Win32Exception e) 
            { 
                throw new AuthenticationException(SR.GetString(SR.net_auth_SSPI), e);
            } 

        }

        // 
        // General informational properties
        // 
 
        //
        // 
        internal bool IsAuthenticated {
            get {
                return _Context != null && _Context.IsValidContext && _Exception == null && HandshakeCompleted;
            } 
        }
        // 
        // 
        //
        internal  bool IsMutuallyAuthenticated { 
            get {
                return
                    IsAuthenticated &&
                    (Context.IsServer? Context.LocalServerCertificate: Context.LocalClientCertificate) != null && 
                    Context.IsRemoteCertificateAvailable; /* does not work: Context.IsMutualAuthFlag;*/
            } 
        } 
        //
        internal bool RemoteCertRequired 
        {
            get {
                return  Context == null || Context.RemoteCertRequired;
            } 
        }
        // 
        internal bool IsServer { 
            get {
                return  Context != null && Context.IsServer; 
            }
        }
        //
        // SSL related properties 
        //
        internal void SetCertValidationDelegate(RemoteCertValidationCallback certValidationCallback) 
        { 
            _CertValidationDelegate = certValidationCallback;
        } 
        //
        // This will return selected local cert for both client/server streams
        //
        internal X509Certificate LocalCertificate { 
            get {
                CheckThrow(true); 
                return InternalLocalCertificate; 
            }
        } 
        //
        // Used directly from ServicePoint for client side only
        internal X509Certificate InternalLocalCertificate {
            get { 
                return Context.IsServer? Context.LocalServerCertificate : Context.LocalClientCertificate;
            } 
        } 
        //
        internal bool CheckCertRevocationStatus { 
            get {
                return Context != null && Context.CheckCertRevocationStatus;
            }
        } 
        //
        internal SecurityStatus LastSecurityStatus { 
            get {return _SecurityStatus;} 
        }
        // 
        internal bool IsCertValidationFailed {
            get {
                return _CertValidationFailed;
            } 
        }
        // 
        internal bool DataAvailable { 
            get {
                return IsAuthenticated && (SecureStream.DataAvailable || _QueuedReadCount != 0); 
            }
        }
        //
        internal CipherAlgorithmType CipherAlgorithm { 
            get {
                CheckThrow(true); 
                SslConnectionInfo info = Context.ConnectionInfo; 
                if (info == null) {
                    return (CipherAlgorithmType)0; 
                }
                return (CipherAlgorithmType)info.DataCipherAlg;
            }
        } 
        //
        internal int CipherStrength { 
            get { 
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo; 
                if (info == null) {
                    return 0;
                }
                return info.DataKeySize; 
            }
        } 
        // 
        internal HashAlgorithmType HashAlgorithm {
            get { 
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null) {
                    return (HashAlgorithmType)0; 
                }
                return (HashAlgorithmType)info.DataHashAlg; 
            } 
        }
        // 
        internal int HashStrength {
            get {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo; 
                if (info == null) {
                    return 0; 
                } 
                return info.DataHashKeySize;
            } 
        }
        //
        internal ExchangeAlgorithmType KeyExchangeAlgorithm {
            get { 
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo; 
                if (info == null) { 
                    return (ExchangeAlgorithmType)0;
                } 
                return (ExchangeAlgorithmType)info.KeyExchangeAlg;
            }
        }
        // 
        internal int KeyExchangeStrength {
            get { 
                CheckThrow(true); 
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null) { 
                    return 0;
                }
                return info.KeyExchKeySize;
            } 
        }
        // 
        internal SslProtocols SslProtocol { 
            get {
                CheckThrow(true); 
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null) {
                    return SslProtocols.None;
                } 
                SslProtocols proto = (SslProtocols)info.Protocol;
                // restore client/server bits so the result maps exaclty on published constants 
                if ((proto & SslProtocols.Ssl2) != 0) { 
                    proto |= SslProtocols.Ssl2;
                } 
                if ((proto & SslProtocols.Ssl3) != 0) {
                    proto |= SslProtocols.Ssl3;
                }
                if ((proto & SslProtocols.Tls) != 0) { 
                    proto |= SslProtocols.Tls;
                } 
                return proto; 
            }
        } 
        //
        //
        //
        internal Stream InnerStream { 
            get {
                return _InnerStream; 
            } 
        }
        // 
        //
        //
        internal _SslStream SecureStream {
            get{ 
                CheckThrow(true);
                if (_SecureStream == null) 
                { 
                    Interlocked.CompareExchange<_SslStream>(ref _SecureStream, new _SslStream(this), null);
                } 
                return _SecureStream;
            }
        }
        // 
        internal int HeaderSize {
            get { 
                return Context.HeaderSize; 
            }
        } 
        //
        internal int MaxDataSize {
            get {
                return Context.MaxDataSize; 
            }
        } 
        internal byte[] LastPayload { 
            get {
                return _LastPayload; 
            }
        }
        internal void LastPayloadConsumed()
        { 
            _LastPayload = null;
        } 
        // 
        private Exception SetException(Exception e)
        { 
            if (_Exception == null)
            {
                _Exception = e;
            } 
            if (_Exception != null && Context != null) {
                Context.Close(); 
            } 
            return _Exception;
        } 
        //
        private bool HandshakeCompleted {
            get {
                return _HandshakeCompleted; 
            }
        } 
        // 
        //
        // 
        private SecureChannel Context {
            get {
                return _Context;
            } 
        }
        // 
        // Methods 
        //
 
        //
        //
        internal void CheckThrow(bool authSucessCheck) {
            if (_Exception != null) { 
                throw _Exception;
            } 
            if (authSucessCheck && !IsAuthenticated) { 
                throw new InvalidOperationException(SR.GetString(SR.net_auth_noauth));
            } 
        }
        //
        internal void Flush()
        { 
            InnerStream.Flush();
        } 
        // 
        // This is to not depend on GC&SafeHandle class if the context is not needed anymore.
        // 
        internal void Close()
        {
            _Exception = new ObjectDisposedException("SslStream");
            if (Context != null) 
            {
                Context.Close(); 
            } 
        }
        // 
        //
        //
        internal SecurityStatus EncryptData(byte[] buffer, int offset, int count, ref byte[] outBuffer, out int outSize)
        { 
            CheckThrow(true);
            return Context.Encrypt(buffer, offset, count, ref outBuffer, out outSize); 
        } 
        //
        // 
        //
        internal SecurityStatus DecryptData(byte[] buffer, ref int offset, ref int count)
        {
            CheckThrow(true); 
            return PrivateDecryptData(buffer, ref offset, ref count);
        } 
        // 
        //
        private SecurityStatus PrivateDecryptData(byte[] buffer, ref int offset, ref int count) 
        {
            return Context.Decrypt(buffer, ref offset, ref count);
        }
        // 
        //
        //  Called by re-handshake if found data decrypted with the old key 
        // 
        private Exception EnqueueOldKeyDecryptedData(byte[] buffer, int offset, int count)
        { 
            lock (this)
            {
                if (_QueuedReadCount + count > _ConstMaxQueuedReadBytes)
                { 
                    return new IOException(SR.GetString(SR.net_auth_ignored_reauth, _ConstMaxQueuedReadBytes.ToString(NumberFormatInfo.CurrentInfo)));
                } 
                if (count !=0) 
                {
                    // This is inefficient yet simple and that should be a rare case of receiving data encrypted with "old" key 
                    _QueuedReadData = EnsureBufferSize(_QueuedReadData, _QueuedReadCount, _QueuedReadCount + count);
                    Buffer.BlockCopy(buffer, offset, _QueuedReadData, _QueuedReadCount, count);
                    _QueuedReadCount+=count;
                    FinishHandshakeRead(LockHandshake); 
                }
            } 
            return null; 
        }
        // 
        // When re-handshaking the "old" key decrypted data are queued until the handshake is done
        // When stream calls for decryption we will feed it queued data left from "old" encryption key.
        //
        // Must be called under the lock in case concurent handshake is going 
        //
        internal int CheckOldKeyDecryptedData(byte[] buffer, int offset, int count) 
        { 
            CheckThrow(true);
            if (_QueuedReadData != null) 
            {
                // This is inefficient yet simple and should be a REALLY rare case.
                int toCopy = Math.Min(_QueuedReadCount, count);
                Buffer.BlockCopy(_QueuedReadData, 0, buffer, offset, toCopy); 
                _QueuedReadCount -= toCopy;
                if (_QueuedReadCount == 0) 
                { 
                    _QueuedReadData = null;
                } 
                else
                {
                    Buffer.BlockCopy(_QueuedReadData, toCopy, _QueuedReadData, 0, _QueuedReadCount);
                } 
                return toCopy;
            } 
            return -1; 
        }
        // 
        // This method assumes that a SSPI context is already in a good shape.
        // For example it is either a fresh context or already authenticated context that needs renegotiation.
        //
        internal void ProcessAuthentication(LazyAsyncResult lazyResult) 
        {
            if (Interlocked.Exchange(ref _NestedAuth, 1) == 1) { 
                throw new InvalidOperationException(SR.GetString(SR.net_io_invalidnestedcall, lazyResult==null?"BeginAuthenticate":"Authenticate", "authenticate")); 
            }
            try { 
                CheckThrow(false);
                AsyncProtocolRequest asyncRequest = null;
                if (lazyResult != null)
                { 
                    asyncRequest = new AsyncProtocolRequest(lazyResult);
                    asyncRequest.Buffer = null; 
#if DEBUG 
                    lazyResult._DebugAsyncChain = asyncRequest;
#endif 
                }

                //  A Win 9x trick to discover and avoid cached sessions
                _CachedSession = CachedSessionStatus.Unknown; 

                ForceAuthentication(Context.IsServer, null, asyncRequest); 
            } 
            finally
            { 
                if (lazyResult == null || _Exception != null)
                {
                    _NestedAuth = 0;
                } 
            }
        } 
        // 
        // This is used to reply on re-handshake when received SEC_I_RENEGOTIATE on Read()
        // 
        internal void ReplyOnReAuthentication(byte[] buffer)
        {
            lock (this)
            { 
                // Note we are already inside the read, so checking for already going concurent handshake
                _LockReadState = LockHandshake; 
 
                if (_UserRequestedHandshake != null || _PendingReHandshake)
                { 
                    // A concurent handshake is pending, resume
                    FinishRead(buffer);
                    return;
                } 
            }
            // Start rehandshake from here 
 
            // forcing async mode with dummy callback since read will independently loop once we return from here.
            AsyncProtocolRequest asyncRequest = new AsyncProtocolRequest(new LazyAsyncResult(this, null, null)); 
            // Buffer contains a result from DecryptMessage that will be passed to ISC/ASC
            asyncRequest.Buffer = buffer;
            ForceAuthentication(false, buffer, asyncRequest);
        } 

        /* 
        // Consider removing. 
        //
        // This will force re-handshake. Currently reserved but can eventually become public. 
        //
        internal void InitiateReAuthentication(AsyncProtocolRequest asyncRequest)
        {
            if (asyncRequest != null) 
            {
                // User requested rehandshake does not have any buffer being passed to ISC/ASC 
                asyncRequest.Buffer = null; 
            }
            LazyAsyncResult lazyResult = null; 
            lock (this)
            {
                int lockState = Interlocked.CompareExchange(ref _LockReadState, LockHandshake, LockNone);
 
                if (lockState == LockHandshake || lockState == LockPendingRead)
                { 
                    // no need for reauth it's already going since requested remotely 
                    // If user requested this op and there is already a pending one
                    // we want to link user request to already going auth. 
                    if (asyncRequest == null)
                    {
                        lazyResult = new LazyAsyncResult(null, null, null); //must be null
                        _UserRequestedHandshake  = lazyResult; 
                    }
                    else 
                    { 
                        _UserRequestedHandshake = asyncRequest;
                        return; 
                    }
                }
                else
                { 
                    // Provide a chance to refresh credentials
                    Context.SetRefreshCredentialNeeded(); 
 
                    // This will tell that a decrypt must be called first
                    // and if it's not renegotiate status, the decrypted data must be cached 
                    _PendingReHandshake = true;
                    // This will  send a hello message and start auth re-handshake
                }
            } 

            if (lazyResult != null) 
            { 
                lazyResult.InternalWaitForCompletion();
            } 
            else
            {
                // This will  send a hello message and start auth re-handshake
                ForceAuthentication(false, null, asyncRequest); 
            }
        } 
        */ 

        // 
        //
        // This method attempts to start authentication.
        // Incoming buffer is either null or is the result of "renegotiate" decrypted message
        // If write is in progress the method will either wait or be put on hold 
        //
        private void ForceAuthentication(bool receiveFirst, byte[] buffer, AsyncProtocolRequest asyncRequest) 
        { 
            if (CheckEnqueueHandshake(buffer, asyncRequest))
            { 
                // Async handshake is enqueued and will resume later
                return;
            }
            // Either Sync handshake is ready to go or async handshake won the race over write. 

            // This will tell that we don't know the framing yet (what SSL version is) 
            _Framing = Framing.None; 

            try { 
                if (receiveFirst)
                {
                    // Listen for a client blob
                    StartReceiveBlob(buffer, asyncRequest); 
                }
                else 
                { 
                    // we start with the first blob
                    StartSendBlob(buffer, (buffer == null? 0: buffer.Length), asyncRequest); 
                }
            }
            catch (Exception e) {
                // Failed auth, reset the framing if any. 
                _Framing = Framing.None;
                _HandshakeCompleted = false; 
 
                if (SetException(e) == e)
                { 
                    throw;
                }
                else
                { 
                    throw _Exception;
                } 
            } 
            catch {
                // Failed auth, reset the framing if any. 
                _Framing = Framing.None;
                _HandshakeCompleted = false;

                throw SetException(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
            }
            finally { 
                if (_Exception != null) { 
                    // This either a failed handshake
                    // Release waiting IO if any. Presumably it should not throw. 
                    // Otheriwse application may receive not expected type of the exception.
                    FinishHandshake(null, null);
                }
            } 
        }
        // 
        // 
        //
        internal void EndProcessAuthentication(IAsyncResult result) 
        {
            if (result == null)
            {
                throw new ArgumentNullException("asyncResult"); 
            }
            LazyAsyncResult lazyResult = result as LazyAsyncResult; 
            if (lazyResult == null) 
            {
                throw new ArgumentException(SR.GetString(SR.net_io_async_result, result.GetType().FullName), "asyncResult"); 
            }
            if (Interlocked.Exchange(ref _NestedAuth, 0) == 0)
            {
                throw new InvalidOperationException(SR.GetString(SR.net_io_invalidendcall, "EndAuthenticate")); 
            }
            InternalEndProcessAuthentication(lazyResult); 
        } 
        //
        // 
        //
        internal void InternalEndProcessAuthentication(LazyAsyncResult lazyResult)
        {
            // No "artificial" timeouts implemented so far, InnerStream controls that. 
            lazyResult.InternalWaitForCompletion();
            Exception e = lazyResult.Result as Exception; 
 
            if (e != null)
            { 
                // Failed auth, reset the framing if any.
                _Framing = Framing.None;
                _HandshakeCompleted = false;
 
                throw SetException(e);
            } 
        } 

        // 
        // Client side starts here, but server also loops through this method
        //
        private void StartSendBlob(byte[] incoming, int count, AsyncProtocolRequest asyncRequest)
        { 
            ProtocolToken message = Context.NextMessage(incoming, 0, count);
            _SecurityStatus = message.Status; 
 
            if (message.Size != 0)
            { 
                if (Context.IsServer && _CachedSession == CachedSessionStatus.Unknown)
                {
                    //
                    //[Schannel] If the first call to ASC returns a token less than 200 bytes, 
                    //           then it's a reconnect (a handshake based on a cache entry)
                    // 
                    _CachedSession = message.Size < 200? CachedSessionStatus.IsCached: CachedSessionStatus.IsNotCached; 
                }
 
                if (_Framing == Framing.Unified)
                {
                    _Framing = DetectFraming(message.Payload, message.Payload.Length);
                } 

                // Even if we are comleted, there could be a blob for sending. 
                // ONLY for TlsStream we want to delay it if the underlined stream is a NetworkStream that is subject to Nagle algorithm 
                //
                if ( message.Done && _ForceBufferingLastHandshakePayload && InnerStream.GetType() == typeof(NetworkStream) && !_PendingReHandshake && !CheckWin9xCachedSession()) 
                {
                    _LastPayload = message.Payload;
                }
                else 
                {
                    if (asyncRequest == null) 
                    { 
                        InnerStream.Write(message.Payload, 0, message.Size);
                    } 
                    else
                    {
                        asyncRequest.AsyncState = message;
                        IAsyncResult ar = InnerStream.BeginWrite(message.Payload, 0, message.Size, _WriteCallback, asyncRequest); 
                        if (!ar.CompletedSynchronously)
                        { 
#if DEBUG 
                            asyncRequest._DebugAsyncChain = ar;
#endif 
                            return;
                        }
                        InnerStream.EndWrite(ar);
                    } 
                }
            } 
            CheckCompletionBeforeNextReceive(message, asyncRequest); 
        }
        // 
        // This will check and logically complete / fail the auth handshake
        //
        private void CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
        { 
            if (message.Failed)
            { 
                StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.GetString(SR.net_auth_SSPI), message.GetException())); 
                return;
            } 
            else if (message.Done && !_PendingReHandshake)
            {
                if (CheckWin9xCachedSession())
                { 
                    // This will tell that a decrypt must be called first
                    // If it's not a renegotiate reply status, then decrypted data must be buffered 
                    // until rehandshake is done 
                    _PendingReHandshake = true;
                    // Allow this to happen only once 
                    Win9xSessionRestarted();
                    ForceAuthentication(false, null, asyncRequest);
                    return;
                } 
                if (!CompleteHandshake())
                { 
                    StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.GetString(SR.net_ssl_io_cert_validation), null)); 
                    return;
                } 
                // Release waiting IO if any. Presumably it should not throw.
                // Otheriwse application may get not expected type of the exception.
                FinishHandshake(null, asyncRequest);
                return; 
            }
 
            StartReceiveBlob(message.Payload, asyncRequest); 
        }
        // 
        // Server side starts here, but client also loops through this method
        //
        private void StartReceiveBlob(byte[] buffer, AsyncProtocolRequest asyncRequest)
        { 
            if (_PendingReHandshake)
            { 
                if (CheckEnqueueHandshakeRead(ref buffer, asyncRequest)) 
                {
                    return; 
                }
                if (!_PendingReHandshake)
                {
                    // read fed us renegotiate proceed to the next step 
                    ProcessReceivedBlob(buffer, buffer.Length, asyncRequest);
                    return; 
                } 
            }
 
            //This is first server read
            buffer = EnsureBufferSize(buffer, 0, Context.HeaderSize);

            int readBytes = 0; 
            if (asyncRequest == null)
            { 
                readBytes = _Reader.ReadPacket(buffer, 0, Context.HeaderSize); 
            }
            else 
            {
                asyncRequest.SetNextRequest(buffer, 0, Context.HeaderSize, _PartialFrameCallback);
                _Reader.AsyncReadPacket(asyncRequest);
                if (!asyncRequest.MustCompleteSynchronously) 
                {
                    return; 
                } 
                readBytes = asyncRequest.Result;
            } 
            StartReadFrame(buffer, readBytes, asyncRequest);
        }
        //
        private void StartReadFrame(byte[] buffer, int readBytes, AsyncProtocolRequest asyncRequest) 
        {
            if (readBytes == 0) 
            { 
                // EOF, throw
                throw new IOException(SR.GetString(SR.net_auth_eof)); 
            }

            if (_Framing == Framing.None) {
                // figure out the Framing 
                _Framing = DetectFraming(buffer, readBytes);
            } 
 
            int restBytes = GetRemainingFrameSize(buffer, readBytes);
 
            if (restBytes < 0)
            {
                throw new IOException(SR.GetString(SR.net_ssl_io_frame));
            } 
            if (restBytes == 0)
            { 
                // EOF received 
                throw new AuthenticationException(SR.GetString(SR.net_auth_eof), null);
            } 

            buffer = EnsureBufferSize(buffer, readBytes, readBytes + restBytes);

            if (asyncRequest == null) 
            {
                restBytes = _Reader.ReadPacket(buffer, readBytes, restBytes); 
            } 
            else
            { 
                asyncRequest.SetNextRequest(buffer, readBytes, restBytes, _ReadFrameCallback);
                _Reader.AsyncReadPacket(asyncRequest);
                if (!asyncRequest.MustCompleteSynchronously)
                { 
                    return;
                } 
                restBytes = asyncRequest.Result; 
                if (restBytes == 0) {
                    //eof, fail 
                    readBytes = 0;
                }
            }
            ProcessReceivedBlob(buffer, readBytes + restBytes, asyncRequest); 
        }
        // 
        // 
        //
        private void ProcessReceivedBlob(byte[] buffer, int count, AsyncProtocolRequest asyncRequest) 
        {
            if (count == 0)
            {
                // EOF received 
                throw new AuthenticationException(SR.GetString(SR.net_auth_eof), null);
            } 
 
            if (_PendingReHandshake)
            { 
                int offset = 0;
                SecurityStatus status = PrivateDecryptData(buffer, ref offset, ref count);

                if (status == SecurityStatus.OK) 
                {
                    Exception e = EnqueueOldKeyDecryptedData(buffer, offset, count); 
                    if (e != null) 
                    {
                        StartSendAuthResetSignal(null, asyncRequest, e); 
                        return;
                    }
                    // Again, forget about framing we can get a new one at any time
                    _Framing = Framing.None; 
                    StartReceiveBlob(buffer, asyncRequest);
                    return; 
                } 
                else if (status != SecurityStatus.Renegotiate)
                { 
                    // fail re-handshake
                    ProtocolToken message = new ProtocolToken(null, status);
                    StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.GetString(SR.net_auth_SSPI), message.GetException()));
                    return; 
                }
 
                // Got it, now we should expect only handshake messages 
                _PendingReHandshake = false;
                if (offset != 0) 
                {
                    Buffer.BlockCopy(buffer, offset, buffer, 0, count);
                }
            } 
            StartSendBlob(buffer, count, asyncRequest);
        } 
        // 
        //  This is to reset auth state on remote side.
        //  If this write succeeds we will allow auth retrying. 
        //
        private void StartSendAuthResetSignal(ProtocolToken message, AsyncProtocolRequest asyncRequest,  Exception exception)
        {
            if (message == null || message.Size == 0) 
            {
                // 
                // we don't have an alert to send so cannot retry and fail prematurely. 
                //
                throw exception; 
            }

            if (asyncRequest == null)
            { 
                InnerStream.Write(message.Payload, 0, message.Size);
            } 
            else 
            {
                asyncRequest.AsyncState = exception; 
                IAsyncResult ar = InnerStream.BeginWrite(message.Payload, 0, message.Size, _WriteCallback, asyncRequest);
                if (!ar.CompletedSynchronously)
                {
                    return; 
                }
                InnerStream.EndWrite(ar); 
            } 
            throw exception;
        } 

        //
        //
        // 
        private bool CheckWin9xCachedSession()
        { 
            if (ComNetOS.IsWin9x && _CachedSession == CachedSessionStatus.IsCached && Context.IsServer && Context.RemoteCertRequired) 
            {
                X509Certificate2 remoteClientCert = null; 
                X509Certificate2Collection dummyStore;
                try {
                    remoteClientCert = Context.GetRemoteCertificate(out dummyStore);
                    if (remoteClientCert == null) 
                        return true;
                } 
                finally { 
                    if (remoteClientCert != null)
                        remoteClientCert.Reset(); 
                }
            }
            return false;
        } 
        private void Win9xSessionRestarted()
        { 
            _CachedSession = CachedSessionStatus.Renegotiated; 
        }
 

        // - Loads the channel parameters
        // - Optionally Verifies the Remote Certificate
        // - Sets HandshakeCompleted flag 
        // - Sets the guarding event if other thread is waiting for
        //   handshake completino 
        // 
        // - Returns false if failed to verify the Remote Cert
        // 
        private bool CompleteHandshake() {
            GlobalLog.Enter("CompleteHandshake");
            Context.ProcessHandshakeSuccess();
 
            if (!Context.VerifyRemoteCertificate(_CertValidationDelegate))
            { 
                _HandshakeCompleted = false; 
                _CertValidationFailed = true;
                GlobalLog.Leave("CompleteHandshake", false); 
                return false;
            }

            _CertValidationFailed = false; 
            _HandshakeCompleted = true;
            GlobalLog.Leave("CompleteHandshake", true); 
            return true; 
        }
 
        //
        //
        //
        private static void WriteCallback(IAsyncResult transportResult) 
        {
            if (transportResult.CompletedSynchronously) 
            { 
                return;
            } 

            AsyncProtocolRequest asyncRequest;
            SslState sslState;
 
#if DEBUG
            try 
            { 
#endif
            asyncRequest = (AsyncProtocolRequest) transportResult.AsyncState; 
            sslState = (SslState) asyncRequest.AsyncObject;
#if DEBUG
            }
            catch (Exception exception) 
            {
                if (!NclUtilities.IsFatal(exception)){ 
                    GlobalLog.Assert("SslState::WriteCallback", "Exception while decoding context. type:" + exception.GetType().ToString() + " message:" + exception.Message); 
                }
                throw; 
            }
#endif

            // Async completion 
            try
            { 
                sslState.InnerStream.EndWrite(transportResult); 
                //special case for an error notification
                object asyncState = asyncRequest.AsyncState; 
                Exception exception = asyncState as Exception;
                if (exception != null)
                {
                    throw exception; 
                }
                sslState.CheckCompletionBeforeNextReceive((ProtocolToken) asyncState, asyncRequest); 
            } 
            catch (Exception e)
            { 
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread.
                    throw;
                } 
                sslState.FinishHandshake(e, asyncRequest);
            } 
            catch { 
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread. 
                    throw;
                }
                sslState.FinishHandshake(new Exception(SR.GetString(SR.net_nonClsCompliantException)), asyncRequest);
            } 
        }
        // 
        // 
        //
        private static void PartialFrameCallback(AsyncProtocolRequest asyncRequest) 
        {
            GlobalLog.Print("SslState::PartialFrameCallback()");
            // Async ONLY completion
            SslState sslState = (SslState)asyncRequest.AsyncObject; 
            try
            { 
                sslState.StartReadFrame(asyncRequest.Buffer, asyncRequest.Result, asyncRequest); 
            }
            catch (Exception e) 
            {
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread.
                    throw; 
                }
                sslState.FinishHandshake(e, asyncRequest); 
            } 
            catch {
                if (asyncRequest.IsUserCompleted) { 
                    // This will throw on a worker thread.
                    throw;
                }
                sslState.FinishHandshake(new Exception(SR.GetString(SR.net_nonClsCompliantException)), asyncRequest); 
            }
        } 
        // 
        //
        private static void ReadFrameCallback(AsyncProtocolRequest asyncRequest) 
        {
            GlobalLog.Print("SslState::ReadFrameCallback()");

            // Async ONLY completion 
            SslState sslState = (SslState)asyncRequest.AsyncObject;
            try 
            { 
                if (asyncRequest.Result == 0)
                { 
                    //eof, will fail
                    asyncRequest.Offset = 0;
                }
                sslState.ProcessReceivedBlob(asyncRequest.Buffer, asyncRequest.Offset + asyncRequest.Result, asyncRequest); 
            }
            catch (Exception e) 
            { 
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread. 
                    throw;
                }
                sslState.FinishHandshake(e, asyncRequest);
            } 
            catch {
                if (asyncRequest.IsUserCompleted) { 
                    // This will throw on a worker thread. 
                    throw;
                } 
                sslState.FinishHandshake(new Exception(SR.GetString(SR.net_nonClsCompliantException)), asyncRequest);
            }
        }
        // 
        //
        private bool CheckEnqueueHandshakeRead(ref byte[] buffer, AsyncProtocolRequest request) 
        { 
            LazyAsyncResult lazyResult = null;
            lock (this) 
            {
                if (_LockReadState == LockPendingRead)
                {
                    // we own the whole process and will never let read to take over until completed. 
                    return false;
                } 
                int lockState = Interlocked.Exchange(ref _LockReadState, LockHandshake); 
                if (lockState != LockRead)
                { 
                    // we came first
                    return false;
                }
 
                if (request != null)
                { 
                    // Request queued 
                    _QueuedReadStateRequest = request;
                    return true; 
                }
                lazyResult = new LazyAsyncResult(null, null,/*must be */ null);
                _QueuedReadStateRequest = lazyResult;
            } 
            // need to exit from lock before waiting
            lazyResult.InternalWaitForCompletion(); 
            buffer = (byte[])lazyResult.Result; 
            return false;
        } 
        //
        // lock is redundant here still included for clarity
        //
        private void FinishHandshakeRead(int newState) 
        {
            lock (this) 
            { 
                int lockState = Interlocked.Exchange(ref _LockReadState, newState);
 
                if (lockState != LockPendingRead)
                {
                    return;
                } 

                _LockReadState = LockRead; 
                object obj = _QueuedReadStateRequest; 
                if (obj == null)
                { 
                    // other thread did not get under the lock yet
                    return;
                }
                _QueuedReadStateRequest = null; 
                if (obj is LazyAsyncResult)
                { 
                    ((LazyAsyncResult)obj).InvokeCallback(); 
                }
                else 
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteRequestWaitCallback), obj);
                }
            } 
        }
 
        // 
        // -1    - proceed
        // 0     - queued 
        // X     - some bytes are ready, no need for IO
        internal int CheckEnqueueRead(byte[] buffer, int offset, int count, AsyncProtocolRequest request)
        {
            int lockState = Interlocked.CompareExchange(ref _LockReadState, LockRead, LockNone); 

            if (lockState != LockHandshake) 
            { 
                // proceed, no concurent handshake is going so no need for a lock.
                return CheckOldKeyDecryptedData(buffer, offset, count); 
            }

            LazyAsyncResult lazyResult = null;
            lock (this) 
            {
                int result = CheckOldKeyDecryptedData(buffer, offset, count); 
                if (result != -1) 
                {
                    return result; 
                }

                //check again under the lock
                if (_LockReadState != LockHandshake) 
                {
                    // other thread has finished before we grabbed the lock 
                    _LockReadState = LockRead; 
                    return -1;
                } 

                _LockReadState = LockPendingRead;

                if (request != null) 
                {
                    // Request queued 
                    _QueuedReadStateRequest = request; 
                    return 0;
                } 
                lazyResult = new LazyAsyncResult(null, null, /*must be */ null);
                _QueuedReadStateRequest = lazyResult;
            }
            // need to exit from lock before waiting 
            lazyResult.InternalWaitForCompletion();
            lock (this) 
            { 
                return CheckOldKeyDecryptedData(buffer, offset, count);
            } 
        }
        //
        //
        // 
        internal void FinishRead(byte[] renegotiateBuffer)
        { 
            int lockState = Interlocked.CompareExchange(ref _LockReadState, LockNone, LockRead); 

            if (lockState != LockHandshake) 
            {
                return;
            }
 
            lock (this)
            { 
                LazyAsyncResult ar = _QueuedReadStateRequest as LazyAsyncResult; 
                if (ar != null)
                { 
                    _QueuedReadStateRequest = null;
                    ar.InvokeCallback(renegotiateBuffer);
                }
                else 
                {
                    AsyncProtocolRequest request = (AsyncProtocolRequest) _QueuedReadStateRequest; 
                    request.Buffer = renegotiateBuffer; 
                    _QueuedReadStateRequest = null;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncResumeHandshakeRead), request); 
                }
            }
        }
        // 
        //
        // true  - operation queued 
        // false - operation can proceed 
        internal bool CheckEnqueueWrite(AsyncProtocolRequest asyncRequest)
        { 
            // cleare previous request
            _QueuedWriteStateRequest = null;
            int lockState = Interlocked.CompareExchange(ref _LockWriteState, LockWrite, LockNone);
            if (lockState != LockHandshake) 
            {
                // proceed with write 
                return false; 
            }
            LazyAsyncResult lazyResult = null; 
            lock (this)
            {
                if (_LockWriteState == LockWrite)
                { 
                    // handshake has completed before we grabbed the lock
                    CheckThrow(true); 
                    return false; 
                }
 
                _LockWriteState = LockPendingWrite;

                // still pending, wait or enqueue
                if (asyncRequest != null) 
                {
                    _QueuedWriteStateRequest = asyncRequest; 
                    return true; 
                }
                lazyResult = new LazyAsyncResult(null, null, /*must be */null); 
                _QueuedWriteStateRequest = lazyResult;
            }
            // need to exit from lock before waiting
            lazyResult.InternalWaitForCompletion(); 
            CheckThrow(true);
            return false; 
        } 
        //
        internal void FinishWrite() 
        {
            int lockState = Interlocked.CompareExchange(ref _LockWriteState, LockNone, LockWrite);
            if (lockState != LockHandshake)
            { 
                return;
            } 
            lock (this) 
            {
                object obj = _QueuedWriteStateRequest; 
                if (obj == null)
                {
                    // a repeated call
                    return; 
                }
 
                _QueuedWriteStateRequest = null; 
                if (obj is LazyAsyncResult)
                { 
                    // sync handshake is waiting on other thread.
                    ((LazyAsyncResult)obj).InvokeCallback();
                }
                else 
                {
                    // async handshake is pending, start it on other thread 
                    // Consider: we could start it in on this thread but that will delay THIS write completion 
                    ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncResumeHandshake), obj);
                } 
            }
        }
        // true  - operation queued
        // false - operation can proceed 
        private bool CheckEnqueueHandshake(byte[] buffer, AsyncProtocolRequest asyncRequest)
        { 
            LazyAsyncResult lazyResult = null; 

            lock (this) 
            {
                if (_LockWriteState == LockPendingWrite)
                {
                    return false; 
                }
                int lockState = Interlocked.Exchange(ref _LockWriteState, LockHandshake); 
                if (lockState != LockWrite) 
                {
                    // proceed with handshake 
                    return false;
                }
                if (asyncRequest != null)
                { 
                    asyncRequest.Buffer = buffer;
                    _QueuedWriteStateRequest = asyncRequest; 
                    return true; 
                }
                lazyResult = new LazyAsyncResult(null, null, /*must be*/null); 
                _QueuedWriteStateRequest = lazyResult;
            }
            lazyResult.InternalWaitForCompletion();
            return false; 
        }
        // 
        // 
        private void FinishHandshake(Exception e, AsyncProtocolRequest asyncRequest)
        { 
            try
            {
                lock (this)
                { 
                    if (e != null)
                    { 
                        SetException(e); 
                    }
 
                    // Complete (if any) rehandshake charged by user
                    object obj = _UserRequestedHandshake;
                    if (obj != null)
                    { 
                        _UserRequestedHandshake = null;
                        if (obj is LazyAsyncResult) 
                        { 
                            ((LazyAsyncResult)obj).InvokeCallback();
                        } 
                        else
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteUserWaitCallback), obj);
                        } 
                    }
 
                    // Release read if any 
                    FinishHandshakeRead(LockNone);
 
                    // If there is a pending write we want to keep it's lock state
                    int lockState = Interlocked.CompareExchange(ref _LockWriteState, LockNone, LockHandshake);
                    if (lockState != LockPendingWrite)
                    { 
                        return;
                    } 
 
                    _LockWriteState = LockWrite;
                    obj = _QueuedWriteStateRequest; 
                    if (obj == null)
                    {
                        //We finished before Write has grabbed the lock
                        return; 
                    }
 
                    _QueuedWriteStateRequest = null; 

                    if (obj is LazyAsyncResult) 
                    {
                        // sync write is waiting on other thread.
                        ((LazyAsyncResult)obj).InvokeCallback();
                    } 
                    else
                    { 
                        // Async write is pending, start it on other thread. 
                        // Consider: we could start it in on this thread but that will delay THIS handshake completion
                        ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteRequestWaitCallback), obj); 
                    }
                }
            }
            finally 
            {
                if (asyncRequest != null) 
                { 
                    if (e != null)
                    { 
                        asyncRequest.CompleteWithError(e);
                    }
                    else
                    { 
                        asyncRequest.CompleteUser();
                    } 
                } 
            }
        } 
        //
        //
        private static byte[] EnsureBufferSize(byte[] buffer, int copyCount, int size) {
            if (buffer == null || buffer.Length < size) { 
                byte[] saved = buffer;
                buffer = new byte[size]; 
                if (saved != null && copyCount != 0) 
                {
                    Buffer.BlockCopy(saved, 0, buffer, 0, copyCount); 
                }
            }
            return buffer;
        } 
        //
        // 
        // 
        private enum Framing
        { 
            None = 0,
            BeforeSSL3,
            SinceSSL3,
            Unified, 
            Invalid
        } 
        // This is set on the first packet to figure out the framing style 
        private Framing _Framing = Framing.None;
 
        //SSL3/TLS protocol frames definitions
        private enum FrameType: byte
        {
           ChangeCipherSpec = 20, 
           Alert            = 21,
           Handshake        = 22, 
           AppData          = 23 
        }
 
        // We need at least 5 bytes to determine what we have.
        private Framing DetectFraming(byte[] bytes, int length) {
            /* PCTv1.0 Hello starts with
             * RECORD_LENGTH_MSB  (ignore) 
             * RECORD_LENGTH_LSB  (ignore)
             * PCT1_CLIENT_HELLO  (must be equal) 
             * PCT1_CLIENT_VERSION_MSB (if version greater than PCTv1) 
             * PCT1_CLIENT_VERSION_LSB (if version greater than PCTv1)
             * 
             * ... PCT hello ...
             */

            /* Microsft Unihello starts with 
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore) 
             * SSL2_CLIENT_HELLO  (must be equal) 
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3)
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3) 
             *
             * ... SSLv2 Compatible Hello ...
             */
 
            /* SSLv2 CLIENT_HELLO starts with
             * RECORD_LENGTH_MSB  (ignore) 
             * RECORD_LENGTH_LSB  (ignore) 
             * SSL2_CLIENT_HELLO  (must be equal)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3) 
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3)
             *
             * ... SSLv2 CLIENT_HELLO ...
             */ 

            /* SSLv2 SERVER_HELLO starts with 
             * RECORD_LENGTH_MSB  (ignore) 
             * RECORD_LENGTH_LSB  (ignore)
             * SSL2_SERVER_HELLO  (must be equal) 
             * SSL2_SESSION_ID_HIT (ignore)
             * SSL2_CERTIFICATE_TYPE (ignore)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3)
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3) 
             *
             * ... SSLv2 SERVER_HELLO ... 
             */ 

           /* SSLv3 Type 2 Hello starts with 
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore)
             * SSL2_CLIENT_HELLO  (must be equal)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv3) 
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv3)
             * 
             * ... SSLv2 Compatible Hello ... 
             */
 
            /* SSLv3 Type 3 Hello starts with
             * 22 (HANDSHAKE MESSAGE)
             * VERSION MSB
             * VERSION LSB 
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore) 
             * HS TYPE (CLIENT_HELLO) 
             * 3 bytes HS record length
             * HS Version 
             * HS Version
             */

            /* SSLv2 message codes 
             * SSL_MT_ERROR                0
             * SSL_MT_CLIENT_HELLO         1 
             * SSL_MT_CLIENT_MASTER_KEY    2 
             * SSL_MT_CLIENT_FINISHED      3
             * SSL_MT_SERVER_HELLO         4 
             * SSL_MT_SERVER_VERIFY        5
             * SSL_MT_SERVER_FINISHED      6
             * SSL_MT_REQUEST_CERTIFICATE  7
             * SSL_MT_CLIENT_CERTIFICATE   8 
             */
 
            int version=-1; 

            GlobalLog.Assert((bytes != null && bytes.Length > 0), "SslState::DetectFraming()|Header buffer is not allocated will boom shortly."); 

            // If the first byte is SSL3 HandShake, then check if we have a
            // SSLv3 Type3 client hello
            if(bytes[0] == (byte)FrameType.Handshake || bytes[0] == (byte)FrameType.AppData) 
            {
                if (length < 3) { 
                    return Framing.Invalid; 
                }
#if TRAVE 
                if (bytes[1] != 3)
                    GlobalLog.Print("WARNING: SslState::DetectFraming() SSL protocol is > 3, trying SSL3 framing in retail = " + bytes[1].ToString("x", NumberFormatInfo.InvariantInfo));
#endif
 
                version = (bytes[1]<<8) | bytes[2];
                if (version < 0x300 || version >= 0x500) { 
                    return Framing.Invalid; 
                }
                // 
                // This is an SSL3 Framing
                //
                return Framing.SinceSSL3;
 
            }
#if TRAVE 
            if ((bytes[0] & 0x80) == 0) 
                // We have a three-byte header format
                GlobalLog.Print("WARNING: SslState::DetectFraming() SSL v <=2 HELLO has no high bit set for 3 bytes header, we are broken, received byte = " + bytes[0].ToString("x", NumberFormatInfo.InvariantInfo)); 
#endif

            if (length < 3) {
                return Framing.Invalid; 
            }
            if (bytes[2] > 8) 
                return Framing.Invalid; 

            if (bytes[2] == 0x1)  // SSL_MT_CLIENT_HELLO 
            {
                if (length >= 5)
                    version = (bytes[3]<<8) | bytes[4];
            } 
            else if (bytes[2] == 0x4) // SSL_MT_SERVER_HELLO
            { 
                if (length >= 7) 
                    version = (bytes[5]<<8) | bytes[6];
            } 

            if (version != -1)
            {
                // If this is the first packet, the client may start with an SSL2 packet 
                // but stating that the version is 3.x, so check the full range.
                // For the subsequent packets we assume that an SSL2 packet should have a 2.x version. 
                if (_Framing == Framing.None) 
                {
                    if (version != 0x0002 && (version < 0x200 || version >= 0x500)) 
                        return Framing.Invalid;
                }
                else
                { 
                    if (version != 0x0002)
                        return Framing.Invalid; 
                } 
            }
 
            // When server has replied the framing is already fixed depending on the prior client packet
            if (!Context.IsServer || _Framing == Framing.Unified) {
                return Framing.BeforeSSL3;
            } 

            return Framing.Unified; //this will use Ssl2 just for this frame 
        } 
        //
        // This is called from SslStream class too 
        internal int GetRemainingFrameSize(byte[] buffer, int dataSize) {
            GlobalLog.Enter("GetRemainingFrameSize", "dataSize = " + dataSize);
            int payloadSize = -1;
            switch (_Framing) { 
            case Framing.Unified:
            case Framing.BeforeSSL3: 
                                    if(dataSize < 2) { 
                                        throw new System.IO.IOException(SR.GetString(SR.net_ssl_io_frame));
                                    } 
                                    // Note: Cannot detect version mismatch for <= SSL2

                                    if((buffer[0] & 0x80) != 0) {
                                        // two bytes 
                                        payloadSize = (((buffer[0] & 0x7f) << 8) | buffer[1]) + 2;
                                        payloadSize -= dataSize; 
                                    } 
                                    else {
                                        //three bytes 
                                        payloadSize = (((buffer[0] & 0x3f) << 8) | buffer[1]) + 3;
                                        payloadSize -= dataSize;
                                    }
 
                                    break;
            case Framing.SinceSSL3: 
                                    if(dataSize < 5) { 
                                        throw new System.IO.IOException(SR.GetString(SR.net_ssl_io_frame));
                                    } 

                                    payloadSize = ((buffer[3]<<8) | buffer[4]) + 5;
                                    payloadSize -= dataSize;
                                    break; 
            default:
                                    break; 
            } 
            GlobalLog.Leave("GetRemainingFrameSize", payloadSize);
            return payloadSize; 
        }
        //
        // Called with no user stack
        // 
        private void AsyncResumeHandshake(object state)
        { 
            AsyncProtocolRequest request = state as AsyncProtocolRequest; 
            ForceAuthentication(Context.IsServer, request.Buffer, request);
        } 
        //
        // Called with no user stack
        //
        private void AsyncResumeHandshakeRead(object state) 
        {
            AsyncProtocolRequest asyncRequest = (AsyncProtocolRequest)state; 
            try { 
                if (_PendingReHandshake)
                { 
                    // resume as read a blob
                    StartReceiveBlob(asyncRequest.Buffer, asyncRequest);
                }
                else 
                {
                    // resume as process the blob 
                    ProcessReceivedBlob(asyncRequest.Buffer, asyncRequest.Buffer == null? 0: asyncRequest.Buffer.Length, asyncRequest); 
                }
            } 
            catch (Exception e)
            {
               if (asyncRequest.IsUserCompleted) {
                   // This will throw on a worker thread. 
                   throw;
               } 
               FinishHandshake(e, asyncRequest); 
           }
           catch { 
               if (asyncRequest.IsUserCompleted) {
                   // This will throw on a worker thread.
                   throw;
               } 
               FinishHandshake(new Exception(SR.GetString(SR.net_nonClsCompliantException)), asyncRequest);
           } 
         } 
        //
        // Called with no user stack 
        //
        private void CompleteRequestWaitCallback(object state)
        {
            AsyncProtocolRequest request = (AsyncProtocolRequest)state; 
            // Force async completion
            if (request.MustCompleteSynchronously) 
                throw new InternalException(); 
            request.CompleteRequest(0);
        } 
        //
        // Called with no user stack
        //
        private static void CompleteUserWaitCallback(object state) 
        {
            AsyncProtocolRequest request = (AsyncProtocolRequest)state; 
            request.CompleteUser(); 
        }
 
#if TRAVE
        [System.Diagnostics.Conditional("TRAVE")]
        internal void Debug() {
            GlobalLog.Print("_HandshakeCompleted: " + _HandshakeCompleted); 
            GlobalLog.Print("_CertValidationFailed: " + _CertValidationFailed);
            GlobalLog.Print("_Context: "); 
            _Context.Debug(); 
        }
#endif 
    }
}
/*++ 
Copyright (c) 2000 Microsoft Corporation

Module Name:
 
    _SslState.cs
 
Abstract: 

 
Author:

    Mauro Ottaviani   07-Nov-2001
    Arthur Bierer     16-Nov-2001 
    Alexei Vopilov    26-Jun-2002
 
Revision History: 
    22-Aug-2003     Adopted for new Ssl feature design.
    15-Sept-2003    Implemented concurent rehanshake 

--*/

namespace System.Net.Security { 
    using System;
    using System.IO; 
    using System.Threading; 
    using System.Security.Cryptography.X509Certificates;
    using System.Collections; 
    using System.Runtime.InteropServices;
    using System.Globalization;
    using System.Net.Sockets;
    using System.Security.Authentication; 
    using System.ComponentModel;
 
    internal class SslState { 

        static int UniqueNameInteger = 123; 
        static AsyncProtocolCallback _PartialFrameCallback  = new AsyncProtocolCallback(PartialFrameCallback);
        static AsyncProtocolCallback _ReadFrameCallback     = new AsyncProtocolCallback(ReadFrameCallback);
        static AsyncCallback         _WriteCallback         = new AsyncCallback(WriteCallback);
 

        private RemoteCertValidationCallback    _CertValidationDelegate; 
        private LocalCertSelectionCallback      _CertSelectionDelegate; 

        private bool                            _CanRetryAuthentication;    // for now it's always false. 

        private Stream          _InnerStream;

        private _SslStream      _SecureStream; 

        private FixedSizeReader _Reader; 
 
        private int             _NestedAuth;
        private SecureChannel   _Context; 

        private bool            _HandshakeCompleted;
        private bool            _CertValidationFailed;
        private SecurityStatus  _SecurityStatus; 
        private Exception       _Exception;
 
        enum CachedSessionStatus: byte 
        {
            Unknown         = 0, 
            IsNotCached     = 1,
            IsCached        = 2,
            Renegotiated    = 3
        } 
        private CachedSessionStatus _CachedSession;
 
        // This block is used by rehandshake code to buffer data decryptred with the old key 
        private byte[]  _QueuedReadData;
        private int     _QueuedReadCount; 
        private bool    _PendingReHandshake;
        private const int _ConstMaxQueuedReadBytes = 1024 * 128;

        // 
        // This block is used to rule the >>re-handshakes<< that are concurent with read/write io requests
        // 
        private const int LockNone          = 0; 
        private const int LockWrite         = 1;
        private const int LockHandshake     = 2; 
        private const int LockPendingWrite  = 3;
        private const int LockRead          = 4;
        private const int LockPendingRead   = 6;
 
        private int     _LockWriteState;
        private object  _QueuedWriteStateRequest; 
 
        private object  _UserRequestedHandshake;
 
        private int     _LockReadState;
        private object  _QueuedReadStateRequest;

        // This is a perf trick ONLY for HTTPS TlsStream. 
        //
 
 
        private bool    _ForceBufferingLastHandshakePayload;
        private byte[]  _LastPayload; 

        //
        // This .ctor is only for internal TlsStream class, used only from client side ssl and it also has some special requirements.
        // 
        internal SslState(Stream innerStream, bool isHTTP): this(innerStream, null, null)
        { 
            _ForceBufferingLastHandshakePayload = isHTTP; 
        }
        // 
        //  The public Client and Server classes enforce the parameters rules  before
        //  calling into this .ctor.
        //
        internal SslState(Stream innerStream, RemoteCertValidationCallback certValidationCallback, LocalCertSelectionCallback  certSelectionCallback) 
        {
            _InnerStream = innerStream; 
            _Reader = new FixedSizeReader(innerStream); 
            _CertValidationDelegate = certValidationCallback;
            _CertSelectionDelegate  = certSelectionCallback; 

        }
        //
        // 
        //
        internal void ValidateCreateContext(bool isServer, string targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, bool remoteCertRequired, bool checkCertRevocationStatus) 
        { 
            ValidateCreateContext( isServer, targetHost, enabledSslProtocols, serverCertificate, clientCertificates, remoteCertRequired,
                                   checkCertRevocationStatus, !isServer); 
        }
        internal void ValidateCreateContext(bool isServer, string targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, bool remoteCertRequired, bool checkCertRevocationStatus, bool checkCertName)
        {
 
            //
            // We don;t support SSL alerts right now, hence any exception is fatal and cannot be retried 
            // Consider: make use if SSL alerts to re-sync SslStream on both sides for retrying. 
            //
            if (_Exception != null && !_CanRetryAuthentication) { 
                throw _Exception;
            }

            if (Context != null && Context.IsValidContext) { 
                throw new InvalidOperationException(SR.GetString(SR.net_auth_reauth));
            } 
 
            if (Context != null && IsServer != isServer) {
                throw new InvalidOperationException(SR.GetString(SR.net_auth_client_server)); 
            }

            if (targetHost == null) {
                throw new ArgumentNullException("targetHost"); 
            }
 
 
            if (isServer ) {
                enabledSslProtocols &= (SslProtocols)SchProtocols.ServerMask; 
                if (serverCertificate == null)
                    throw new ArgumentNullException("serverCertificate");
            }
            else { 
                enabledSslProtocols &= (SslProtocols)SchProtocols.ClientMask;
            } 
 
            if ((int)enabledSslProtocols == 0) {
                throw new ArgumentException(SR.GetString(SR.net_invalid_enum, "SslProtocolType"), "sslProtocolType"); 
            }

            if (clientCertificates == null) {
                clientCertificates = new X509CertificateCollection(); 
            }
 
            if (targetHost.Length == 0) { 
               targetHost = "?" + Interlocked.Increment(ref UniqueNameInteger).ToString(NumberFormatInfo.InvariantInfo);
            } 

            _Exception = null;
            try {
                _Context = new SecureChannel(targetHost, isServer, (SchProtocols)((int)enabledSslProtocols), serverCertificate, clientCertificates, remoteCertRequired, checkCertName, checkCertRevocationStatus, _CertSelectionDelegate); 
            }
            catch (Win32Exception e) 
            { 
                throw new AuthenticationException(SR.GetString(SR.net_auth_SSPI), e);
            } 

        }

        // 
        // General informational properties
        // 
 
        //
        // 
        internal bool IsAuthenticated {
            get {
                return _Context != null && _Context.IsValidContext && _Exception == null && HandshakeCompleted;
            } 
        }
        // 
        // 
        //
        internal  bool IsMutuallyAuthenticated { 
            get {
                return
                    IsAuthenticated &&
                    (Context.IsServer? Context.LocalServerCertificate: Context.LocalClientCertificate) != null && 
                    Context.IsRemoteCertificateAvailable; /* does not work: Context.IsMutualAuthFlag;*/
            } 
        } 
        //
        internal bool RemoteCertRequired 
        {
            get {
                return  Context == null || Context.RemoteCertRequired;
            } 
        }
        // 
        internal bool IsServer { 
            get {
                return  Context != null && Context.IsServer; 
            }
        }
        //
        // SSL related properties 
        //
        internal void SetCertValidationDelegate(RemoteCertValidationCallback certValidationCallback) 
        { 
            _CertValidationDelegate = certValidationCallback;
        } 
        //
        // This will return selected local cert for both client/server streams
        //
        internal X509Certificate LocalCertificate { 
            get {
                CheckThrow(true); 
                return InternalLocalCertificate; 
            }
        } 
        //
        // Used directly from ServicePoint for client side only
        internal X509Certificate InternalLocalCertificate {
            get { 
                return Context.IsServer? Context.LocalServerCertificate : Context.LocalClientCertificate;
            } 
        } 
        //
        internal bool CheckCertRevocationStatus { 
            get {
                return Context != null && Context.CheckCertRevocationStatus;
            }
        } 
        //
        internal SecurityStatus LastSecurityStatus { 
            get {return _SecurityStatus;} 
        }
        // 
        internal bool IsCertValidationFailed {
            get {
                return _CertValidationFailed;
            } 
        }
        // 
        internal bool DataAvailable { 
            get {
                return IsAuthenticated && (SecureStream.DataAvailable || _QueuedReadCount != 0); 
            }
        }
        //
        internal CipherAlgorithmType CipherAlgorithm { 
            get {
                CheckThrow(true); 
                SslConnectionInfo info = Context.ConnectionInfo; 
                if (info == null) {
                    return (CipherAlgorithmType)0; 
                }
                return (CipherAlgorithmType)info.DataCipherAlg;
            }
        } 
        //
        internal int CipherStrength { 
            get { 
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo; 
                if (info == null) {
                    return 0;
                }
                return info.DataKeySize; 
            }
        } 
        // 
        internal HashAlgorithmType HashAlgorithm {
            get { 
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null) {
                    return (HashAlgorithmType)0; 
                }
                return (HashAlgorithmType)info.DataHashAlg; 
            } 
        }
        // 
        internal int HashStrength {
            get {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo; 
                if (info == null) {
                    return 0; 
                } 
                return info.DataHashKeySize;
            } 
        }
        //
        internal ExchangeAlgorithmType KeyExchangeAlgorithm {
            get { 
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo; 
                if (info == null) { 
                    return (ExchangeAlgorithmType)0;
                } 
                return (ExchangeAlgorithmType)info.KeyExchangeAlg;
            }
        }
        // 
        internal int KeyExchangeStrength {
            get { 
                CheckThrow(true); 
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null) { 
                    return 0;
                }
                return info.KeyExchKeySize;
            } 
        }
        // 
        internal SslProtocols SslProtocol { 
            get {
                CheckThrow(true); 
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null) {
                    return SslProtocols.None;
                } 
                SslProtocols proto = (SslProtocols)info.Protocol;
                // restore client/server bits so the result maps exaclty on published constants 
                if ((proto & SslProtocols.Ssl2) != 0) { 
                    proto |= SslProtocols.Ssl2;
                } 
                if ((proto & SslProtocols.Ssl3) != 0) {
                    proto |= SslProtocols.Ssl3;
                }
                if ((proto & SslProtocols.Tls) != 0) { 
                    proto |= SslProtocols.Tls;
                } 
                return proto; 
            }
        } 
        //
        //
        //
        internal Stream InnerStream { 
            get {
                return _InnerStream; 
            } 
        }
        // 
        //
        //
        internal _SslStream SecureStream {
            get{ 
                CheckThrow(true);
                if (_SecureStream == null) 
                { 
                    Interlocked.CompareExchange<_SslStream>(ref _SecureStream, new _SslStream(this), null);
                } 
                return _SecureStream;
            }
        }
        // 
        internal int HeaderSize {
            get { 
                return Context.HeaderSize; 
            }
        } 
        //
        internal int MaxDataSize {
            get {
                return Context.MaxDataSize; 
            }
        } 
        internal byte[] LastPayload { 
            get {
                return _LastPayload; 
            }
        }
        internal void LastPayloadConsumed()
        { 
            _LastPayload = null;
        } 
        // 
        private Exception SetException(Exception e)
        { 
            if (_Exception == null)
            {
                _Exception = e;
            } 
            if (_Exception != null && Context != null) {
                Context.Close(); 
            } 
            return _Exception;
        } 
        //
        private bool HandshakeCompleted {
            get {
                return _HandshakeCompleted; 
            }
        } 
        // 
        //
        // 
        private SecureChannel Context {
            get {
                return _Context;
            } 
        }
        // 
        // Methods 
        //
 
        //
        //
        internal void CheckThrow(bool authSucessCheck) {
            if (_Exception != null) { 
                throw _Exception;
            } 
            if (authSucessCheck && !IsAuthenticated) { 
                throw new InvalidOperationException(SR.GetString(SR.net_auth_noauth));
            } 
        }
        //
        internal void Flush()
        { 
            InnerStream.Flush();
        } 
        // 
        // This is to not depend on GC&SafeHandle class if the context is not needed anymore.
        // 
        internal void Close()
        {
            _Exception = new ObjectDisposedException("SslStream");
            if (Context != null) 
            {
                Context.Close(); 
            } 
        }
        // 
        //
        //
        internal SecurityStatus EncryptData(byte[] buffer, int offset, int count, ref byte[] outBuffer, out int outSize)
        { 
            CheckThrow(true);
            return Context.Encrypt(buffer, offset, count, ref outBuffer, out outSize); 
        } 
        //
        // 
        //
        internal SecurityStatus DecryptData(byte[] buffer, ref int offset, ref int count)
        {
            CheckThrow(true); 
            return PrivateDecryptData(buffer, ref offset, ref count);
        } 
        // 
        //
        private SecurityStatus PrivateDecryptData(byte[] buffer, ref int offset, ref int count) 
        {
            return Context.Decrypt(buffer, ref offset, ref count);
        }
        // 
        //
        //  Called by re-handshake if found data decrypted with the old key 
        // 
        private Exception EnqueueOldKeyDecryptedData(byte[] buffer, int offset, int count)
        { 
            lock (this)
            {
                if (_QueuedReadCount + count > _ConstMaxQueuedReadBytes)
                { 
                    return new IOException(SR.GetString(SR.net_auth_ignored_reauth, _ConstMaxQueuedReadBytes.ToString(NumberFormatInfo.CurrentInfo)));
                } 
                if (count !=0) 
                {
                    // This is inefficient yet simple and that should be a rare case of receiving data encrypted with "old" key 
                    _QueuedReadData = EnsureBufferSize(_QueuedReadData, _QueuedReadCount, _QueuedReadCount + count);
                    Buffer.BlockCopy(buffer, offset, _QueuedReadData, _QueuedReadCount, count);
                    _QueuedReadCount+=count;
                    FinishHandshakeRead(LockHandshake); 
                }
            } 
            return null; 
        }
        // 
        // When re-handshaking the "old" key decrypted data are queued until the handshake is done
        // When stream calls for decryption we will feed it queued data left from "old" encryption key.
        //
        // Must be called under the lock in case concurent handshake is going 
        //
        internal int CheckOldKeyDecryptedData(byte[] buffer, int offset, int count) 
        { 
            CheckThrow(true);
            if (_QueuedReadData != null) 
            {
                // This is inefficient yet simple and should be a REALLY rare case.
                int toCopy = Math.Min(_QueuedReadCount, count);
                Buffer.BlockCopy(_QueuedReadData, 0, buffer, offset, toCopy); 
                _QueuedReadCount -= toCopy;
                if (_QueuedReadCount == 0) 
                { 
                    _QueuedReadData = null;
                } 
                else
                {
                    Buffer.BlockCopy(_QueuedReadData, toCopy, _QueuedReadData, 0, _QueuedReadCount);
                } 
                return toCopy;
            } 
            return -1; 
        }
        // 
        // This method assumes that a SSPI context is already in a good shape.
        // For example it is either a fresh context or already authenticated context that needs renegotiation.
        //
        internal void ProcessAuthentication(LazyAsyncResult lazyResult) 
        {
            if (Interlocked.Exchange(ref _NestedAuth, 1) == 1) { 
                throw new InvalidOperationException(SR.GetString(SR.net_io_invalidnestedcall, lazyResult==null?"BeginAuthenticate":"Authenticate", "authenticate")); 
            }
            try { 
                CheckThrow(false);
                AsyncProtocolRequest asyncRequest = null;
                if (lazyResult != null)
                { 
                    asyncRequest = new AsyncProtocolRequest(lazyResult);
                    asyncRequest.Buffer = null; 
#if DEBUG 
                    lazyResult._DebugAsyncChain = asyncRequest;
#endif 
                }

                //  A Win 9x trick to discover and avoid cached sessions
                _CachedSession = CachedSessionStatus.Unknown; 

                ForceAuthentication(Context.IsServer, null, asyncRequest); 
            } 
            finally
            { 
                if (lazyResult == null || _Exception != null)
                {
                    _NestedAuth = 0;
                } 
            }
        } 
        // 
        // This is used to reply on re-handshake when received SEC_I_RENEGOTIATE on Read()
        // 
        internal void ReplyOnReAuthentication(byte[] buffer)
        {
            lock (this)
            { 
                // Note we are already inside the read, so checking for already going concurent handshake
                _LockReadState = LockHandshake; 
 
                if (_UserRequestedHandshake != null || _PendingReHandshake)
                { 
                    // A concurent handshake is pending, resume
                    FinishRead(buffer);
                    return;
                } 
            }
            // Start rehandshake from here 
 
            // forcing async mode with dummy callback since read will independently loop once we return from here.
            AsyncProtocolRequest asyncRequest = new AsyncProtocolRequest(new LazyAsyncResult(this, null, null)); 
            // Buffer contains a result from DecryptMessage that will be passed to ISC/ASC
            asyncRequest.Buffer = buffer;
            ForceAuthentication(false, buffer, asyncRequest);
        } 

        /* 
        // Consider removing. 
        //
        // This will force re-handshake. Currently reserved but can eventually become public. 
        //
        internal void InitiateReAuthentication(AsyncProtocolRequest asyncRequest)
        {
            if (asyncRequest != null) 
            {
                // User requested rehandshake does not have any buffer being passed to ISC/ASC 
                asyncRequest.Buffer = null; 
            }
            LazyAsyncResult lazyResult = null; 
            lock (this)
            {
                int lockState = Interlocked.CompareExchange(ref _LockReadState, LockHandshake, LockNone);
 
                if (lockState == LockHandshake || lockState == LockPendingRead)
                { 
                    // no need for reauth it's already going since requested remotely 
                    // If user requested this op and there is already a pending one
                    // we want to link user request to already going auth. 
                    if (asyncRequest == null)
                    {
                        lazyResult = new LazyAsyncResult(null, null, null); //must be null
                        _UserRequestedHandshake  = lazyResult; 
                    }
                    else 
                    { 
                        _UserRequestedHandshake = asyncRequest;
                        return; 
                    }
                }
                else
                { 
                    // Provide a chance to refresh credentials
                    Context.SetRefreshCredentialNeeded(); 
 
                    // This will tell that a decrypt must be called first
                    // and if it's not renegotiate status, the decrypted data must be cached 
                    _PendingReHandshake = true;
                    // This will  send a hello message and start auth re-handshake
                }
            } 

            if (lazyResult != null) 
            { 
                lazyResult.InternalWaitForCompletion();
            } 
            else
            {
                // This will  send a hello message and start auth re-handshake
                ForceAuthentication(false, null, asyncRequest); 
            }
        } 
        */ 

        // 
        //
        // This method attempts to start authentication.
        // Incoming buffer is either null or is the result of "renegotiate" decrypted message
        // If write is in progress the method will either wait or be put on hold 
        //
        private void ForceAuthentication(bool receiveFirst, byte[] buffer, AsyncProtocolRequest asyncRequest) 
        { 
            if (CheckEnqueueHandshake(buffer, asyncRequest))
            { 
                // Async handshake is enqueued and will resume later
                return;
            }
            // Either Sync handshake is ready to go or async handshake won the race over write. 

            // This will tell that we don't know the framing yet (what SSL version is) 
            _Framing = Framing.None; 

            try { 
                if (receiveFirst)
                {
                    // Listen for a client blob
                    StartReceiveBlob(buffer, asyncRequest); 
                }
                else 
                { 
                    // we start with the first blob
                    StartSendBlob(buffer, (buffer == null? 0: buffer.Length), asyncRequest); 
                }
            }
            catch (Exception e) {
                // Failed auth, reset the framing if any. 
                _Framing = Framing.None;
                _HandshakeCompleted = false; 
 
                if (SetException(e) == e)
                { 
                    throw;
                }
                else
                { 
                    throw _Exception;
                } 
            } 
            catch {
                // Failed auth, reset the framing if any. 
                _Framing = Framing.None;
                _HandshakeCompleted = false;

                throw SetException(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
            }
            finally { 
                if (_Exception != null) { 
                    // This either a failed handshake
                    // Release waiting IO if any. Presumably it should not throw. 
                    // Otheriwse application may receive not expected type of the exception.
                    FinishHandshake(null, null);
                }
            } 
        }
        // 
        // 
        //
        internal void EndProcessAuthentication(IAsyncResult result) 
        {
            if (result == null)
            {
                throw new ArgumentNullException("asyncResult"); 
            }
            LazyAsyncResult lazyResult = result as LazyAsyncResult; 
            if (lazyResult == null) 
            {
                throw new ArgumentException(SR.GetString(SR.net_io_async_result, result.GetType().FullName), "asyncResult"); 
            }
            if (Interlocked.Exchange(ref _NestedAuth, 0) == 0)
            {
                throw new InvalidOperationException(SR.GetString(SR.net_io_invalidendcall, "EndAuthenticate")); 
            }
            InternalEndProcessAuthentication(lazyResult); 
        } 
        //
        // 
        //
        internal void InternalEndProcessAuthentication(LazyAsyncResult lazyResult)
        {
            // No "artificial" timeouts implemented so far, InnerStream controls that. 
            lazyResult.InternalWaitForCompletion();
            Exception e = lazyResult.Result as Exception; 
 
            if (e != null)
            { 
                // Failed auth, reset the framing if any.
                _Framing = Framing.None;
                _HandshakeCompleted = false;
 
                throw SetException(e);
            } 
        } 

        // 
        // Client side starts here, but server also loops through this method
        //
        private void StartSendBlob(byte[] incoming, int count, AsyncProtocolRequest asyncRequest)
        { 
            ProtocolToken message = Context.NextMessage(incoming, 0, count);
            _SecurityStatus = message.Status; 
 
            if (message.Size != 0)
            { 
                if (Context.IsServer && _CachedSession == CachedSessionStatus.Unknown)
                {
                    //
                    //[Schannel] If the first call to ASC returns a token less than 200 bytes, 
                    //           then it's a reconnect (a handshake based on a cache entry)
                    // 
                    _CachedSession = message.Size < 200? CachedSessionStatus.IsCached: CachedSessionStatus.IsNotCached; 
                }
 
                if (_Framing == Framing.Unified)
                {
                    _Framing = DetectFraming(message.Payload, message.Payload.Length);
                } 

                // Even if we are comleted, there could be a blob for sending. 
                // ONLY for TlsStream we want to delay it if the underlined stream is a NetworkStream that is subject to Nagle algorithm 
                //
                if ( message.Done && _ForceBufferingLastHandshakePayload && InnerStream.GetType() == typeof(NetworkStream) && !_PendingReHandshake && !CheckWin9xCachedSession()) 
                {
                    _LastPayload = message.Payload;
                }
                else 
                {
                    if (asyncRequest == null) 
                    { 
                        InnerStream.Write(message.Payload, 0, message.Size);
                    } 
                    else
                    {
                        asyncRequest.AsyncState = message;
                        IAsyncResult ar = InnerStream.BeginWrite(message.Payload, 0, message.Size, _WriteCallback, asyncRequest); 
                        if (!ar.CompletedSynchronously)
                        { 
#if DEBUG 
                            asyncRequest._DebugAsyncChain = ar;
#endif 
                            return;
                        }
                        InnerStream.EndWrite(ar);
                    } 
                }
            } 
            CheckCompletionBeforeNextReceive(message, asyncRequest); 
        }
        // 
        // This will check and logically complete / fail the auth handshake
        //
        private void CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
        { 
            if (message.Failed)
            { 
                StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.GetString(SR.net_auth_SSPI), message.GetException())); 
                return;
            } 
            else if (message.Done && !_PendingReHandshake)
            {
                if (CheckWin9xCachedSession())
                { 
                    // This will tell that a decrypt must be called first
                    // If it's not a renegotiate reply status, then decrypted data must be buffered 
                    // until rehandshake is done 
                    _PendingReHandshake = true;
                    // Allow this to happen only once 
                    Win9xSessionRestarted();
                    ForceAuthentication(false, null, asyncRequest);
                    return;
                } 
                if (!CompleteHandshake())
                { 
                    StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.GetString(SR.net_ssl_io_cert_validation), null)); 
                    return;
                } 
                // Release waiting IO if any. Presumably it should not throw.
                // Otheriwse application may get not expected type of the exception.
                FinishHandshake(null, asyncRequest);
                return; 
            }
 
            StartReceiveBlob(message.Payload, asyncRequest); 
        }
        // 
        // Server side starts here, but client also loops through this method
        //
        private void StartReceiveBlob(byte[] buffer, AsyncProtocolRequest asyncRequest)
        { 
            if (_PendingReHandshake)
            { 
                if (CheckEnqueueHandshakeRead(ref buffer, asyncRequest)) 
                {
                    return; 
                }
                if (!_PendingReHandshake)
                {
                    // read fed us renegotiate proceed to the next step 
                    ProcessReceivedBlob(buffer, buffer.Length, asyncRequest);
                    return; 
                } 
            }
 
            //This is first server read
            buffer = EnsureBufferSize(buffer, 0, Context.HeaderSize);

            int readBytes = 0; 
            if (asyncRequest == null)
            { 
                readBytes = _Reader.ReadPacket(buffer, 0, Context.HeaderSize); 
            }
            else 
            {
                asyncRequest.SetNextRequest(buffer, 0, Context.HeaderSize, _PartialFrameCallback);
                _Reader.AsyncReadPacket(asyncRequest);
                if (!asyncRequest.MustCompleteSynchronously) 
                {
                    return; 
                } 
                readBytes = asyncRequest.Result;
            } 
            StartReadFrame(buffer, readBytes, asyncRequest);
        }
        //
        private void StartReadFrame(byte[] buffer, int readBytes, AsyncProtocolRequest asyncRequest) 
        {
            if (readBytes == 0) 
            { 
                // EOF, throw
                throw new IOException(SR.GetString(SR.net_auth_eof)); 
            }

            if (_Framing == Framing.None) {
                // figure out the Framing 
                _Framing = DetectFraming(buffer, readBytes);
            } 
 
            int restBytes = GetRemainingFrameSize(buffer, readBytes);
 
            if (restBytes < 0)
            {
                throw new IOException(SR.GetString(SR.net_ssl_io_frame));
            } 
            if (restBytes == 0)
            { 
                // EOF received 
                throw new AuthenticationException(SR.GetString(SR.net_auth_eof), null);
            } 

            buffer = EnsureBufferSize(buffer, readBytes, readBytes + restBytes);

            if (asyncRequest == null) 
            {
                restBytes = _Reader.ReadPacket(buffer, readBytes, restBytes); 
            } 
            else
            { 
                asyncRequest.SetNextRequest(buffer, readBytes, restBytes, _ReadFrameCallback);
                _Reader.AsyncReadPacket(asyncRequest);
                if (!asyncRequest.MustCompleteSynchronously)
                { 
                    return;
                } 
                restBytes = asyncRequest.Result; 
                if (restBytes == 0) {
                    //eof, fail 
                    readBytes = 0;
                }
            }
            ProcessReceivedBlob(buffer, readBytes + restBytes, asyncRequest); 
        }
        // 
        // 
        //
        private void ProcessReceivedBlob(byte[] buffer, int count, AsyncProtocolRequest asyncRequest) 
        {
            if (count == 0)
            {
                // EOF received 
                throw new AuthenticationException(SR.GetString(SR.net_auth_eof), null);
            } 
 
            if (_PendingReHandshake)
            { 
                int offset = 0;
                SecurityStatus status = PrivateDecryptData(buffer, ref offset, ref count);

                if (status == SecurityStatus.OK) 
                {
                    Exception e = EnqueueOldKeyDecryptedData(buffer, offset, count); 
                    if (e != null) 
                    {
                        StartSendAuthResetSignal(null, asyncRequest, e); 
                        return;
                    }
                    // Again, forget about framing we can get a new one at any time
                    _Framing = Framing.None; 
                    StartReceiveBlob(buffer, asyncRequest);
                    return; 
                } 
                else if (status != SecurityStatus.Renegotiate)
                { 
                    // fail re-handshake
                    ProtocolToken message = new ProtocolToken(null, status);
                    StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.GetString(SR.net_auth_SSPI), message.GetException()));
                    return; 
                }
 
                // Got it, now we should expect only handshake messages 
                _PendingReHandshake = false;
                if (offset != 0) 
                {
                    Buffer.BlockCopy(buffer, offset, buffer, 0, count);
                }
            } 
            StartSendBlob(buffer, count, asyncRequest);
        } 
        // 
        //  This is to reset auth state on remote side.
        //  If this write succeeds we will allow auth retrying. 
        //
        private void StartSendAuthResetSignal(ProtocolToken message, AsyncProtocolRequest asyncRequest,  Exception exception)
        {
            if (message == null || message.Size == 0) 
            {
                // 
                // we don't have an alert to send so cannot retry and fail prematurely. 
                //
                throw exception; 
            }

            if (asyncRequest == null)
            { 
                InnerStream.Write(message.Payload, 0, message.Size);
            } 
            else 
            {
                asyncRequest.AsyncState = exception; 
                IAsyncResult ar = InnerStream.BeginWrite(message.Payload, 0, message.Size, _WriteCallback, asyncRequest);
                if (!ar.CompletedSynchronously)
                {
                    return; 
                }
                InnerStream.EndWrite(ar); 
            } 
            throw exception;
        } 

        //
        //
        // 
        private bool CheckWin9xCachedSession()
        { 
            if (ComNetOS.IsWin9x && _CachedSession == CachedSessionStatus.IsCached && Context.IsServer && Context.RemoteCertRequired) 
            {
                X509Certificate2 remoteClientCert = null; 
                X509Certificate2Collection dummyStore;
                try {
                    remoteClientCert = Context.GetRemoteCertificate(out dummyStore);
                    if (remoteClientCert == null) 
                        return true;
                } 
                finally { 
                    if (remoteClientCert != null)
                        remoteClientCert.Reset(); 
                }
            }
            return false;
        } 
        private void Win9xSessionRestarted()
        { 
            _CachedSession = CachedSessionStatus.Renegotiated; 
        }
 

        // - Loads the channel parameters
        // - Optionally Verifies the Remote Certificate
        // - Sets HandshakeCompleted flag 
        // - Sets the guarding event if other thread is waiting for
        //   handshake completino 
        // 
        // - Returns false if failed to verify the Remote Cert
        // 
        private bool CompleteHandshake() {
            GlobalLog.Enter("CompleteHandshake");
            Context.ProcessHandshakeSuccess();
 
            if (!Context.VerifyRemoteCertificate(_CertValidationDelegate))
            { 
                _HandshakeCompleted = false; 
                _CertValidationFailed = true;
                GlobalLog.Leave("CompleteHandshake", false); 
                return false;
            }

            _CertValidationFailed = false; 
            _HandshakeCompleted = true;
            GlobalLog.Leave("CompleteHandshake", true); 
            return true; 
        }
 
        //
        //
        //
        private static void WriteCallback(IAsyncResult transportResult) 
        {
            if (transportResult.CompletedSynchronously) 
            { 
                return;
            } 

            AsyncProtocolRequest asyncRequest;
            SslState sslState;
 
#if DEBUG
            try 
            { 
#endif
            asyncRequest = (AsyncProtocolRequest) transportResult.AsyncState; 
            sslState = (SslState) asyncRequest.AsyncObject;
#if DEBUG
            }
            catch (Exception exception) 
            {
                if (!NclUtilities.IsFatal(exception)){ 
                    GlobalLog.Assert("SslState::WriteCallback", "Exception while decoding context. type:" + exception.GetType().ToString() + " message:" + exception.Message); 
                }
                throw; 
            }
#endif

            // Async completion 
            try
            { 
                sslState.InnerStream.EndWrite(transportResult); 
                //special case for an error notification
                object asyncState = asyncRequest.AsyncState; 
                Exception exception = asyncState as Exception;
                if (exception != null)
                {
                    throw exception; 
                }
                sslState.CheckCompletionBeforeNextReceive((ProtocolToken) asyncState, asyncRequest); 
            } 
            catch (Exception e)
            { 
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread.
                    throw;
                } 
                sslState.FinishHandshake(e, asyncRequest);
            } 
            catch { 
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread. 
                    throw;
                }
                sslState.FinishHandshake(new Exception(SR.GetString(SR.net_nonClsCompliantException)), asyncRequest);
            } 
        }
        // 
        // 
        //
        private static void PartialFrameCallback(AsyncProtocolRequest asyncRequest) 
        {
            GlobalLog.Print("SslState::PartialFrameCallback()");
            // Async ONLY completion
            SslState sslState = (SslState)asyncRequest.AsyncObject; 
            try
            { 
                sslState.StartReadFrame(asyncRequest.Buffer, asyncRequest.Result, asyncRequest); 
            }
            catch (Exception e) 
            {
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread.
                    throw; 
                }
                sslState.FinishHandshake(e, asyncRequest); 
            } 
            catch {
                if (asyncRequest.IsUserCompleted) { 
                    // This will throw on a worker thread.
                    throw;
                }
                sslState.FinishHandshake(new Exception(SR.GetString(SR.net_nonClsCompliantException)), asyncRequest); 
            }
        } 
        // 
        //
        private static void ReadFrameCallback(AsyncProtocolRequest asyncRequest) 
        {
            GlobalLog.Print("SslState::ReadFrameCallback()");

            // Async ONLY completion 
            SslState sslState = (SslState)asyncRequest.AsyncObject;
            try 
            { 
                if (asyncRequest.Result == 0)
                { 
                    //eof, will fail
                    asyncRequest.Offset = 0;
                }
                sslState.ProcessReceivedBlob(asyncRequest.Buffer, asyncRequest.Offset + asyncRequest.Result, asyncRequest); 
            }
            catch (Exception e) 
            { 
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread. 
                    throw;
                }
                sslState.FinishHandshake(e, asyncRequest);
            } 
            catch {
                if (asyncRequest.IsUserCompleted) { 
                    // This will throw on a worker thread. 
                    throw;
                } 
                sslState.FinishHandshake(new Exception(SR.GetString(SR.net_nonClsCompliantException)), asyncRequest);
            }
        }
        // 
        //
        private bool CheckEnqueueHandshakeRead(ref byte[] buffer, AsyncProtocolRequest request) 
        { 
            LazyAsyncResult lazyResult = null;
            lock (this) 
            {
                if (_LockReadState == LockPendingRead)
                {
                    // we own the whole process and will never let read to take over until completed. 
                    return false;
                } 
                int lockState = Interlocked.Exchange(ref _LockReadState, LockHandshake); 
                if (lockState != LockRead)
                { 
                    // we came first
                    return false;
                }
 
                if (request != null)
                { 
                    // Request queued 
                    _QueuedReadStateRequest = request;
                    return true; 
                }
                lazyResult = new LazyAsyncResult(null, null,/*must be */ null);
                _QueuedReadStateRequest = lazyResult;
            } 
            // need to exit from lock before waiting
            lazyResult.InternalWaitForCompletion(); 
            buffer = (byte[])lazyResult.Result; 
            return false;
        } 
        //
        // lock is redundant here still included for clarity
        //
        private void FinishHandshakeRead(int newState) 
        {
            lock (this) 
            { 
                int lockState = Interlocked.Exchange(ref _LockReadState, newState);
 
                if (lockState != LockPendingRead)
                {
                    return;
                } 

                _LockReadState = LockRead; 
                object obj = _QueuedReadStateRequest; 
                if (obj == null)
                { 
                    // other thread did not get under the lock yet
                    return;
                }
                _QueuedReadStateRequest = null; 
                if (obj is LazyAsyncResult)
                { 
                    ((LazyAsyncResult)obj).InvokeCallback(); 
                }
                else 
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteRequestWaitCallback), obj);
                }
            } 
        }
 
        // 
        // -1    - proceed
        // 0     - queued 
        // X     - some bytes are ready, no need for IO
        internal int CheckEnqueueRead(byte[] buffer, int offset, int count, AsyncProtocolRequest request)
        {
            int lockState = Interlocked.CompareExchange(ref _LockReadState, LockRead, LockNone); 

            if (lockState != LockHandshake) 
            { 
                // proceed, no concurent handshake is going so no need for a lock.
                return CheckOldKeyDecryptedData(buffer, offset, count); 
            }

            LazyAsyncResult lazyResult = null;
            lock (this) 
            {
                int result = CheckOldKeyDecryptedData(buffer, offset, count); 
                if (result != -1) 
                {
                    return result; 
                }

                //check again under the lock
                if (_LockReadState != LockHandshake) 
                {
                    // other thread has finished before we grabbed the lock 
                    _LockReadState = LockRead; 
                    return -1;
                } 

                _LockReadState = LockPendingRead;

                if (request != null) 
                {
                    // Request queued 
                    _QueuedReadStateRequest = request; 
                    return 0;
                } 
                lazyResult = new LazyAsyncResult(null, null, /*must be */ null);
                _QueuedReadStateRequest = lazyResult;
            }
            // need to exit from lock before waiting 
            lazyResult.InternalWaitForCompletion();
            lock (this) 
            { 
                return CheckOldKeyDecryptedData(buffer, offset, count);
            } 
        }
        //
        //
        // 
        internal void FinishRead(byte[] renegotiateBuffer)
        { 
            int lockState = Interlocked.CompareExchange(ref _LockReadState, LockNone, LockRead); 

            if (lockState != LockHandshake) 
            {
                return;
            }
 
            lock (this)
            { 
                LazyAsyncResult ar = _QueuedReadStateRequest as LazyAsyncResult; 
                if (ar != null)
                { 
                    _QueuedReadStateRequest = null;
                    ar.InvokeCallback(renegotiateBuffer);
                }
                else 
                {
                    AsyncProtocolRequest request = (AsyncProtocolRequest) _QueuedReadStateRequest; 
                    request.Buffer = renegotiateBuffer; 
                    _QueuedReadStateRequest = null;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncResumeHandshakeRead), request); 
                }
            }
        }
        // 
        //
        // true  - operation queued 
        // false - operation can proceed 
        internal bool CheckEnqueueWrite(AsyncProtocolRequest asyncRequest)
        { 
            // cleare previous request
            _QueuedWriteStateRequest = null;
            int lockState = Interlocked.CompareExchange(ref _LockWriteState, LockWrite, LockNone);
            if (lockState != LockHandshake) 
            {
                // proceed with write 
                return false; 
            }
            LazyAsyncResult lazyResult = null; 
            lock (this)
            {
                if (_LockWriteState == LockWrite)
                { 
                    // handshake has completed before we grabbed the lock
                    CheckThrow(true); 
                    return false; 
                }
 
                _LockWriteState = LockPendingWrite;

                // still pending, wait or enqueue
                if (asyncRequest != null) 
                {
                    _QueuedWriteStateRequest = asyncRequest; 
                    return true; 
                }
                lazyResult = new LazyAsyncResult(null, null, /*must be */null); 
                _QueuedWriteStateRequest = lazyResult;
            }
            // need to exit from lock before waiting
            lazyResult.InternalWaitForCompletion(); 
            CheckThrow(true);
            return false; 
        } 
        //
        internal void FinishWrite() 
        {
            int lockState = Interlocked.CompareExchange(ref _LockWriteState, LockNone, LockWrite);
            if (lockState != LockHandshake)
            { 
                return;
            } 
            lock (this) 
            {
                object obj = _QueuedWriteStateRequest; 
                if (obj == null)
                {
                    // a repeated call
                    return; 
                }
 
                _QueuedWriteStateRequest = null; 
                if (obj is LazyAsyncResult)
                { 
                    // sync handshake is waiting on other thread.
                    ((LazyAsyncResult)obj).InvokeCallback();
                }
                else 
                {
                    // async handshake is pending, start it on other thread 
                    // Consider: we could start it in on this thread but that will delay THIS write completion 
                    ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncResumeHandshake), obj);
                } 
            }
        }
        // true  - operation queued
        // false - operation can proceed 
        private bool CheckEnqueueHandshake(byte[] buffer, AsyncProtocolRequest asyncRequest)
        { 
            LazyAsyncResult lazyResult = null; 

            lock (this) 
            {
                if (_LockWriteState == LockPendingWrite)
                {
                    return false; 
                }
                int lockState = Interlocked.Exchange(ref _LockWriteState, LockHandshake); 
                if (lockState != LockWrite) 
                {
                    // proceed with handshake 
                    return false;
                }
                if (asyncRequest != null)
                { 
                    asyncRequest.Buffer = buffer;
                    _QueuedWriteStateRequest = asyncRequest; 
                    return true; 
                }
                lazyResult = new LazyAsyncResult(null, null, /*must be*/null); 
                _QueuedWriteStateRequest = lazyResult;
            }
            lazyResult.InternalWaitForCompletion();
            return false; 
        }
        // 
        // 
        private void FinishHandshake(Exception e, AsyncProtocolRequest asyncRequest)
        { 
            try
            {
                lock (this)
                { 
                    if (e != null)
                    { 
                        SetException(e); 
                    }
 
                    // Complete (if any) rehandshake charged by user
                    object obj = _UserRequestedHandshake;
                    if (obj != null)
                    { 
                        _UserRequestedHandshake = null;
                        if (obj is LazyAsyncResult) 
                        { 
                            ((LazyAsyncResult)obj).InvokeCallback();
                        } 
                        else
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteUserWaitCallback), obj);
                        } 
                    }
 
                    // Release read if any 
                    FinishHandshakeRead(LockNone);
 
                    // If there is a pending write we want to keep it's lock state
                    int lockState = Interlocked.CompareExchange(ref _LockWriteState, LockNone, LockHandshake);
                    if (lockState != LockPendingWrite)
                    { 
                        return;
                    } 
 
                    _LockWriteState = LockWrite;
                    obj = _QueuedWriteStateRequest; 
                    if (obj == null)
                    {
                        //We finished before Write has grabbed the lock
                        return; 
                    }
 
                    _QueuedWriteStateRequest = null; 

                    if (obj is LazyAsyncResult) 
                    {
                        // sync write is waiting on other thread.
                        ((LazyAsyncResult)obj).InvokeCallback();
                    } 
                    else
                    { 
                        // Async write is pending, start it on other thread. 
                        // Consider: we could start it in on this thread but that will delay THIS handshake completion
                        ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteRequestWaitCallback), obj); 
                    }
                }
            }
            finally 
            {
                if (asyncRequest != null) 
                { 
                    if (e != null)
                    { 
                        asyncRequest.CompleteWithError(e);
                    }
                    else
                    { 
                        asyncRequest.CompleteUser();
                    } 
                } 
            }
        } 
        //
        //
        private static byte[] EnsureBufferSize(byte[] buffer, int copyCount, int size) {
            if (buffer == null || buffer.Length < size) { 
                byte[] saved = buffer;
                buffer = new byte[size]; 
                if (saved != null && copyCount != 0) 
                {
                    Buffer.BlockCopy(saved, 0, buffer, 0, copyCount); 
                }
            }
            return buffer;
        } 
        //
        // 
        // 
        private enum Framing
        { 
            None = 0,
            BeforeSSL3,
            SinceSSL3,
            Unified, 
            Invalid
        } 
        // This is set on the first packet to figure out the framing style 
        private Framing _Framing = Framing.None;
 
        //SSL3/TLS protocol frames definitions
        private enum FrameType: byte
        {
           ChangeCipherSpec = 20, 
           Alert            = 21,
           Handshake        = 22, 
           AppData          = 23 
        }
 
        // We need at least 5 bytes to determine what we have.
        private Framing DetectFraming(byte[] bytes, int length) {
            /* PCTv1.0 Hello starts with
             * RECORD_LENGTH_MSB  (ignore) 
             * RECORD_LENGTH_LSB  (ignore)
             * PCT1_CLIENT_HELLO  (must be equal) 
             * PCT1_CLIENT_VERSION_MSB (if version greater than PCTv1) 
             * PCT1_CLIENT_VERSION_LSB (if version greater than PCTv1)
             * 
             * ... PCT hello ...
             */

            /* Microsft Unihello starts with 
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore) 
             * SSL2_CLIENT_HELLO  (must be equal) 
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3)
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3) 
             *
             * ... SSLv2 Compatible Hello ...
             */
 
            /* SSLv2 CLIENT_HELLO starts with
             * RECORD_LENGTH_MSB  (ignore) 
             * RECORD_LENGTH_LSB  (ignore) 
             * SSL2_CLIENT_HELLO  (must be equal)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3) 
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3)
             *
             * ... SSLv2 CLIENT_HELLO ...
             */ 

            /* SSLv2 SERVER_HELLO starts with 
             * RECORD_LENGTH_MSB  (ignore) 
             * RECORD_LENGTH_LSB  (ignore)
             * SSL2_SERVER_HELLO  (must be equal) 
             * SSL2_SESSION_ID_HIT (ignore)
             * SSL2_CERTIFICATE_TYPE (ignore)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3)
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3) 
             *
             * ... SSLv2 SERVER_HELLO ... 
             */ 

           /* SSLv3 Type 2 Hello starts with 
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore)
             * SSL2_CLIENT_HELLO  (must be equal)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv3) 
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv3)
             * 
             * ... SSLv2 Compatible Hello ... 
             */
 
            /* SSLv3 Type 3 Hello starts with
             * 22 (HANDSHAKE MESSAGE)
             * VERSION MSB
             * VERSION LSB 
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore) 
             * HS TYPE (CLIENT_HELLO) 
             * 3 bytes HS record length
             * HS Version 
             * HS Version
             */

            /* SSLv2 message codes 
             * SSL_MT_ERROR                0
             * SSL_MT_CLIENT_HELLO         1 
             * SSL_MT_CLIENT_MASTER_KEY    2 
             * SSL_MT_CLIENT_FINISHED      3
             * SSL_MT_SERVER_HELLO         4 
             * SSL_MT_SERVER_VERIFY        5
             * SSL_MT_SERVER_FINISHED      6
             * SSL_MT_REQUEST_CERTIFICATE  7
             * SSL_MT_CLIENT_CERTIFICATE   8 
             */
 
            int version=-1; 

            GlobalLog.Assert((bytes != null && bytes.Length > 0), "SslState::DetectFraming()|Header buffer is not allocated will boom shortly."); 

            // If the first byte is SSL3 HandShake, then check if we have a
            // SSLv3 Type3 client hello
            if(bytes[0] == (byte)FrameType.Handshake || bytes[0] == (byte)FrameType.AppData) 
            {
                if (length < 3) { 
                    return Framing.Invalid; 
                }
#if TRAVE 
                if (bytes[1] != 3)
                    GlobalLog.Print("WARNING: SslState::DetectFraming() SSL protocol is > 3, trying SSL3 framing in retail = " + bytes[1].ToString("x", NumberFormatInfo.InvariantInfo));
#endif
 
                version = (bytes[1]<<8) | bytes[2];
                if (version < 0x300 || version >= 0x500) { 
                    return Framing.Invalid; 
                }
                // 
                // This is an SSL3 Framing
                //
                return Framing.SinceSSL3;
 
            }
#if TRAVE 
            if ((bytes[0] & 0x80) == 0) 
                // We have a three-byte header format
                GlobalLog.Print("WARNING: SslState::DetectFraming() SSL v <=2 HELLO has no high bit set for 3 bytes header, we are broken, received byte = " + bytes[0].ToString("x", NumberFormatInfo.InvariantInfo)); 
#endif

            if (length < 3) {
                return Framing.Invalid; 
            }
            if (bytes[2] > 8) 
                return Framing.Invalid; 

            if (bytes[2] == 0x1)  // SSL_MT_CLIENT_HELLO 
            {
                if (length >= 5)
                    version = (bytes[3]<<8) | bytes[4];
            } 
            else if (bytes[2] == 0x4) // SSL_MT_SERVER_HELLO
            { 
                if (length >= 7) 
                    version = (bytes[5]<<8) | bytes[6];
            } 

            if (version != -1)
            {
                // If this is the first packet, the client may start with an SSL2 packet 
                // but stating that the version is 3.x, so check the full range.
                // For the subsequent packets we assume that an SSL2 packet should have a 2.x version. 
                if (_Framing == Framing.None) 
                {
                    if (version != 0x0002 && (version < 0x200 || version >= 0x500)) 
                        return Framing.Invalid;
                }
                else
                { 
                    if (version != 0x0002)
                        return Framing.Invalid; 
                } 
            }
 
            // When server has replied the framing is already fixed depending on the prior client packet
            if (!Context.IsServer || _Framing == Framing.Unified) {
                return Framing.BeforeSSL3;
            } 

            return Framing.Unified; //this will use Ssl2 just for this frame 
        } 
        //
        // This is called from SslStream class too 
        internal int GetRemainingFrameSize(byte[] buffer, int dataSize) {
            GlobalLog.Enter("GetRemainingFrameSize", "dataSize = " + dataSize);
            int payloadSize = -1;
            switch (_Framing) { 
            case Framing.Unified:
            case Framing.BeforeSSL3: 
                                    if(dataSize < 2) { 
                                        throw new System.IO.IOException(SR.GetString(SR.net_ssl_io_frame));
                                    } 
                                    // Note: Cannot detect version mismatch for <= SSL2

                                    if((buffer[0] & 0x80) != 0) {
                                        // two bytes 
                                        payloadSize = (((buffer[0] & 0x7f) << 8) | buffer[1]) + 2;
                                        payloadSize -= dataSize; 
                                    } 
                                    else {
                                        //three bytes 
                                        payloadSize = (((buffer[0] & 0x3f) << 8) | buffer[1]) + 3;
                                        payloadSize -= dataSize;
                                    }
 
                                    break;
            case Framing.SinceSSL3: 
                                    if(dataSize < 5) { 
                                        throw new System.IO.IOException(SR.GetString(SR.net_ssl_io_frame));
                                    } 

                                    payloadSize = ((buffer[3]<<8) | buffer[4]) + 5;
                                    payloadSize -= dataSize;
                                    break; 
            default:
                                    break; 
            } 
            GlobalLog.Leave("GetRemainingFrameSize", payloadSize);
            return payloadSize; 
        }
        //
        // Called with no user stack
        // 
        private void AsyncResumeHandshake(object state)
        { 
            AsyncProtocolRequest request = state as AsyncProtocolRequest; 
            ForceAuthentication(Context.IsServer, request.Buffer, request);
        } 
        //
        // Called with no user stack
        //
        private void AsyncResumeHandshakeRead(object state) 
        {
            AsyncProtocolRequest asyncRequest = (AsyncProtocolRequest)state; 
            try { 
                if (_PendingReHandshake)
                { 
                    // resume as read a blob
                    StartReceiveBlob(asyncRequest.Buffer, asyncRequest);
                }
                else 
                {
                    // resume as process the blob 
                    ProcessReceivedBlob(asyncRequest.Buffer, asyncRequest.Buffer == null? 0: asyncRequest.Buffer.Length, asyncRequest); 
                }
            } 
            catch (Exception e)
            {
               if (asyncRequest.IsUserCompleted) {
                   // This will throw on a worker thread. 
                   throw;
               } 
               FinishHandshake(e, asyncRequest); 
           }
           catch { 
               if (asyncRequest.IsUserCompleted) {
                   // This will throw on a worker thread.
                   throw;
               } 
               FinishHandshake(new Exception(SR.GetString(SR.net_nonClsCompliantException)), asyncRequest);
           } 
         } 
        //
        // Called with no user stack 
        //
        private void CompleteRequestWaitCallback(object state)
        {
            AsyncProtocolRequest request = (AsyncProtocolRequest)state; 
            // Force async completion
            if (request.MustCompleteSynchronously) 
                throw new InternalException(); 
            request.CompleteRequest(0);
        } 
        //
        // Called with no user stack
        //
        private static void CompleteUserWaitCallback(object state) 
        {
            AsyncProtocolRequest request = (AsyncProtocolRequest)state; 
            request.CompleteUser(); 
        }
 
#if TRAVE
        [System.Diagnostics.Conditional("TRAVE")]
        internal void Debug() {
            GlobalLog.Print("_HandshakeCompleted: " + _HandshakeCompleted); 
            GlobalLog.Print("_CertValidationFailed: " + _CertValidationFailed);
            GlobalLog.Print("_Context: "); 
            _Context.Debug(); 
        }
#endif 
    }
}
