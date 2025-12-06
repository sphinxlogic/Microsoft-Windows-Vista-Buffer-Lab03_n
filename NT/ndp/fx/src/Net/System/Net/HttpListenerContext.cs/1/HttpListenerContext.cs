//------------------------------------------------------------------------------ 
// <copyright file="HttpListenerContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.Security.Principal; 
    using System.Security.Permissions;
 
    public sealed unsafe class HttpListenerContext /* BaseHttpContext, */   {
        private HttpListener m_Listener;
        private HttpListenerRequest m_Request;
        private HttpListenerResponse m_Response; 
        private IPrincipal m_User;
        private string m_MutualAuthentication; 
        private bool m_PromoteCookiesToRfc2965; 

        internal const string NTLM = "NTLM"; 


        internal HttpListenerContext(HttpListener httpListener, RequestContextBase memoryBlob)
        { 
            if (Logging.On) Logging.PrintInfo(Logging.HttpListener, this, ".ctor", "httpListener#" + ValidationHelper.HashString(httpListener) + " requestBlob=" + ValidationHelper.HashString((IntPtr) memoryBlob.RequestBlob));
            m_Listener = httpListener; 
            m_Request = new HttpListenerRequest(this, memoryBlob); 
            GlobalLog.Print("HttpListenerContext#" + ValidationHelper.HashString(this) + "::.ctor() HttpListener#" + ValidationHelper.HashString(m_Listener) + " HttpListenerRequest#" + ValidationHelper.HashString(m_Request));
        } 

        // Call this right after construction, and only once!  Not after it's been handed to a user.
        internal void SetIdentity(IPrincipal principal, string mutualAuthentication)
        { 
            m_MutualAuthentication = mutualAuthentication;
            m_User = principal; 
            GlobalLog.Print("HttpListenerContext#" + ValidationHelper.HashString(this) + "::SetIdentity() mutual:" + (mutualAuthentication == null ? "<null>" : mutualAuthentication) + " Principal#" + ValidationHelper.HashString(principal)); 
        }
 
        public /* new */ HttpListenerRequest Request {
            get {
                return m_Request;
            } 
        }
 
        public /* new */ HttpListenerResponse Response { 
            get {
                if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Response", ""); 
                if (m_Response==null) {
                    m_Response = new HttpListenerResponse(this);
                    GlobalLog.Print("HttpListenerContext#" + ValidationHelper.HashString(this) + "::.Response_get() HttpListener#" + ValidationHelper.HashString(m_Listener) + " HttpListenerRequest#" + ValidationHelper.HashString(m_Request) + " HttpListenerResponse#" + ValidationHelper.HashString(m_Response));
                } 
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Response", "");
                return m_Response; 
            } 
        }
 
        /*
        protected override BaseHttpRequest GetRequest() {
            return Request;
        } 

        protected override BaseHttpResponse GetResponse() { 
            return Response; 
        }
        */ 

        // Requires ControlPrincipal permission if the request was authenticated with Negotiate, NTLM, or Digest.
        // IsAuthenticated depends on the demand here, so if it is changed (like to a LinkDemand) make sure IsAuthenticated is ok.
        public /* override */ IPrincipal User { 
            get {
                if (m_User as WindowsPrincipal == null) 
                { 
                    return m_User;
                } 

                new SecurityPermission(SecurityPermissionFlag.ControlPrincipal).Demand();
                return m_User;
            } 
        }
 
        // < 

 



 

 
 

 


        internal bool PromoteCookiesToRfc2965 {
            get { 
                return m_PromoteCookiesToRfc2965;
            } 
        } 

        internal string MutualAuthentication { 
            get {
                return m_MutualAuthentication;
            }
        } 

        internal HttpListener Listener { 
            get { 
                return m_Listener;
            } 
        }

        internal SafeCloseHandle RequestQueueHandle {
            get { 
                return m_Listener.RequestQueueHandle;
            } 
        } 

        internal void EnsureBoundHandle() 
        {
            m_Listener.EnsureBoundHandle();
        }
 
        internal ulong RequestId {
            get { 
                return Request.RequestId; 
            }
        } 

        internal void Close() {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Close()", "");
 
            try {
                if (m_Response!=null) { 
                    m_Response.Close(); 
                }
            } 
            finally {
                try {
                    m_Request.Close();
                } 
                finally {
                    IDisposable user = m_User == null ? null : m_User.Identity as IDisposable; 
 
                    // For unsafe connection ntlm auth we dont dispose this identity as yet since its cached
                    if ((user != null) && 
                        (m_User.Identity.AuthenticationType != NTLM) &&
                        (!m_Listener.UnsafeConnectionNtlmAuthentication))
                    {
                            user.Dispose(); 
                    }
                } 
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Close", "");
        } 

        internal void Abort() {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Abort", "");
            HttpListenerContext.CancelRequest(RequestQueueHandle, m_Request.RequestId); 
            try {
                m_Request.Close(); 
            } 
            finally {
                IDisposable user = m_User == null ? null : m_User.Identity as IDisposable; 
                if (user != null) {
                    user.Dispose();
                }
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Abort", "");
        } 
 

        internal UnsafeNclNativeMethods.HttpApi.HTTP_VERB GetKnownMethod() { 
            GlobalLog.Print("HttpListenerContext::GetKnownMethod()");
            return UnsafeNclNativeMethods.HttpApi.GetKnownVerb(Request.RequestBuffer, Request.OriginalBlobAddress);
        }
 
        internal unsafe static void CancelRequest(SafeCloseHandle requestQueueHandle, ulong requestId) {
            GlobalLog.Print("HttpListenerContext::CancelRequest() requestQueueHandle:" + requestQueueHandle + " requestId:" + requestId); 
 
            UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK httpNoData = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
            httpNoData.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory; 

            // Give it a valid buffer pointer even though it's to nothing.
            httpNoData.pBuffer = (byte*)&httpNoData;
 
            uint statusCode =
                UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody( 
                    requestQueueHandle, 
                    requestId,
                    (uint) UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT, 
                    1,
                    &httpNoData,
                    null,
                    SafeLocalFree.Zero, 
                    0,
                    null, 
                    null); 

            GlobalLog.Print("HttpListenerContext::CancelRequest() call to UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody returned:" + statusCode); 
        }
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="HttpListenerContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.Security.Principal; 
    using System.Security.Permissions;
 
    public sealed unsafe class HttpListenerContext /* BaseHttpContext, */   {
        private HttpListener m_Listener;
        private HttpListenerRequest m_Request;
        private HttpListenerResponse m_Response; 
        private IPrincipal m_User;
        private string m_MutualAuthentication; 
        private bool m_PromoteCookiesToRfc2965; 

        internal const string NTLM = "NTLM"; 


        internal HttpListenerContext(HttpListener httpListener, RequestContextBase memoryBlob)
        { 
            if (Logging.On) Logging.PrintInfo(Logging.HttpListener, this, ".ctor", "httpListener#" + ValidationHelper.HashString(httpListener) + " requestBlob=" + ValidationHelper.HashString((IntPtr) memoryBlob.RequestBlob));
            m_Listener = httpListener; 
            m_Request = new HttpListenerRequest(this, memoryBlob); 
            GlobalLog.Print("HttpListenerContext#" + ValidationHelper.HashString(this) + "::.ctor() HttpListener#" + ValidationHelper.HashString(m_Listener) + " HttpListenerRequest#" + ValidationHelper.HashString(m_Request));
        } 

        // Call this right after construction, and only once!  Not after it's been handed to a user.
        internal void SetIdentity(IPrincipal principal, string mutualAuthentication)
        { 
            m_MutualAuthentication = mutualAuthentication;
            m_User = principal; 
            GlobalLog.Print("HttpListenerContext#" + ValidationHelper.HashString(this) + "::SetIdentity() mutual:" + (mutualAuthentication == null ? "<null>" : mutualAuthentication) + " Principal#" + ValidationHelper.HashString(principal)); 
        }
 
        public /* new */ HttpListenerRequest Request {
            get {
                return m_Request;
            } 
        }
 
        public /* new */ HttpListenerResponse Response { 
            get {
                if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Response", ""); 
                if (m_Response==null) {
                    m_Response = new HttpListenerResponse(this);
                    GlobalLog.Print("HttpListenerContext#" + ValidationHelper.HashString(this) + "::.Response_get() HttpListener#" + ValidationHelper.HashString(m_Listener) + " HttpListenerRequest#" + ValidationHelper.HashString(m_Request) + " HttpListenerResponse#" + ValidationHelper.HashString(m_Response));
                } 
                if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Response", "");
                return m_Response; 
            } 
        }
 
        /*
        protected override BaseHttpRequest GetRequest() {
            return Request;
        } 

        protected override BaseHttpResponse GetResponse() { 
            return Response; 
        }
        */ 

        // Requires ControlPrincipal permission if the request was authenticated with Negotiate, NTLM, or Digest.
        // IsAuthenticated depends on the demand here, so if it is changed (like to a LinkDemand) make sure IsAuthenticated is ok.
        public /* override */ IPrincipal User { 
            get {
                if (m_User as WindowsPrincipal == null) 
                { 
                    return m_User;
                } 

                new SecurityPermission(SecurityPermissionFlag.ControlPrincipal).Demand();
                return m_User;
            } 
        }
 
        // < 

 



 

 
 

 


        internal bool PromoteCookiesToRfc2965 {
            get { 
                return m_PromoteCookiesToRfc2965;
            } 
        } 

        internal string MutualAuthentication { 
            get {
                return m_MutualAuthentication;
            }
        } 

        internal HttpListener Listener { 
            get { 
                return m_Listener;
            } 
        }

        internal SafeCloseHandle RequestQueueHandle {
            get { 
                return m_Listener.RequestQueueHandle;
            } 
        } 

        internal void EnsureBoundHandle() 
        {
            m_Listener.EnsureBoundHandle();
        }
 
        internal ulong RequestId {
            get { 
                return Request.RequestId; 
            }
        } 

        internal void Close() {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Close()", "");
 
            try {
                if (m_Response!=null) { 
                    m_Response.Close(); 
                }
            } 
            finally {
                try {
                    m_Request.Close();
                } 
                finally {
                    IDisposable user = m_User == null ? null : m_User.Identity as IDisposable; 
 
                    // For unsafe connection ntlm auth we dont dispose this identity as yet since its cached
                    if ((user != null) && 
                        (m_User.Identity.AuthenticationType != NTLM) &&
                        (!m_Listener.UnsafeConnectionNtlmAuthentication))
                    {
                            user.Dispose(); 
                    }
                } 
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Close", "");
        } 

        internal void Abort() {
            if(Logging.On)Logging.Enter(Logging.HttpListener, this, "Abort", "");
            HttpListenerContext.CancelRequest(RequestQueueHandle, m_Request.RequestId); 
            try {
                m_Request.Close(); 
            } 
            finally {
                IDisposable user = m_User == null ? null : m_User.Identity as IDisposable; 
                if (user != null) {
                    user.Dispose();
                }
            } 
            if(Logging.On)Logging.Exit(Logging.HttpListener, this, "Abort", "");
        } 
 

        internal UnsafeNclNativeMethods.HttpApi.HTTP_VERB GetKnownMethod() { 
            GlobalLog.Print("HttpListenerContext::GetKnownMethod()");
            return UnsafeNclNativeMethods.HttpApi.GetKnownVerb(Request.RequestBuffer, Request.OriginalBlobAddress);
        }
 
        internal unsafe static void CancelRequest(SafeCloseHandle requestQueueHandle, ulong requestId) {
            GlobalLog.Print("HttpListenerContext::CancelRequest() requestQueueHandle:" + requestQueueHandle + " requestId:" + requestId); 
 
            UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK httpNoData = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
            httpNoData.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory; 

            // Give it a valid buffer pointer even though it's to nothing.
            httpNoData.pBuffer = (byte*)&httpNoData;
 
            uint statusCode =
                UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody( 
                    requestQueueHandle, 
                    requestId,
                    (uint) UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT, 
                    1,
                    &httpNoData,
                    null,
                    SafeLocalFree.Zero, 
                    0,
                    null, 
                    null); 

            GlobalLog.Print("HttpListenerContext::CancelRequest() call to UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody returned:" + statusCode); 
        }
    }
}
