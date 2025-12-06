//------------------------------------------------------------------------------ 
// <copyright file="_NegotiateClient.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.Diagnostics; 
    using System.Collections;
    using System.Net.Sockets; 
    using System.Security.Permissions;
    using System.Globalization;

    internal class NegotiateClient : ISessionAuthenticationModule { 

        internal const string AuthType = "Negotiate"; 
        internal static string Signature = AuthType.ToLower(CultureInfo.InvariantCulture); 
        internal static int SignatureSize = Signature.Length;
 
        // we can't work on non-NT2K platforms or non Win, so we shut off,
        // NOTE this exception IS caught internally.
        public NegotiateClient() {
            if (!ComNetOS.IsWin2K) { 
                throw new PlatformNotSupportedException(SR.GetString(SR.Win2000Required));
            } 
        } 

        public Authorization Authenticate(string challenge, WebRequest webRequest, ICredentials credentials) { 
            GlobalLog.Print("NegotiateClient::Authenticate() challenge:[" + ValidationHelper.ToString(challenge) + "] webRequest#" + ValidationHelper.HashString(webRequest) + " credentials#" + ValidationHelper.HashString(credentials) + " calling DoAuthenticate()");
            return DoAuthenticate(challenge, webRequest, credentials, false);
        }
 
        private Authorization DoAuthenticate(string challenge, WebRequest webRequest, ICredentials credentials, bool preAuthenticate) {
            GlobalLog.Print("NegotiateClient::DoAuthenticate() challenge:[" + ValidationHelper.ToString(challenge) + "] webRequest#" + ValidationHelper.HashString(webRequest) + " credentials#" + ValidationHelper.HashString(credentials) + " preAuthenticate:" + preAuthenticate.ToString()); 
 
            GlobalLog.Assert(credentials != null, "NegotiateClient::DoAuthenticate()|credentials == null");
            if (credentials == null) { 
                return null;
            }

            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest; 

            GlobalLog.Assert(httpWebRequest != null, "NegotiateClient::DoAuthenticate()|httpWebRequest == null"); 
            GlobalLog.Assert(httpWebRequest.ChallengedUri != null, "NegotiateClient::DoAuthenticate()|httpWebRequest.ChallengedUri == null"); 

            NTAuthentication authSession = null; 
            string incoming = null;

            if (!preAuthenticate) {
                int index = AuthenticationManager.FindSubstringNotInQuotes(challenge, Signature); 
                if (index < 0) {
                    return null; 
                } 

                int blobBegin = index + SignatureSize; 

                //
                // there may be multiple challenges. If the next character after the
                // package name is not a comma then it is challenge data 
                //
 
                if (challenge.Length > blobBegin && challenge[blobBegin] != ',') { 
                    ++blobBegin;
                } 
                else {
                    index = -1;
                }
 
                if (index >= 0 && challenge.Length > blobBegin)
                { 
                    // Strip other modules information in case of multiple challenges 
                    // i.e do not take ", NTLM" as part of the following Negotiate blob
                    // Negotiate TlRMTVNTUAACAAAADgAOADgAAAA1wo ... MAbwBmAHQALgBjAG8AbQAAAAAA,NTLM 
                    index = challenge.IndexOf(',', blobBegin);
                    if (index != -1)
                        incoming = challenge.Substring(blobBegin, index - blobBegin);
                    else 
                        incoming = challenge.Substring(blobBegin);
                } 
 
                authSession = httpWebRequest.CurrentAuthenticationState.GetSecurityContext(this);
                GlobalLog.Print("NegotiateClient::DoAuthenticate() key:" + ValidationHelper.HashString(httpWebRequest.CurrentAuthenticationState) + " retrieved authSession:" + ValidationHelper.HashString(authSession)); 
            }

            if (authSession==null)
            { 
                NetworkCredential NC = credentials.GetCredential(httpWebRequest.ChallengedUri, Signature);
                GlobalLog.Print("NegotiateClient::DoAuthenticate() GetCredential() returns:" + ValidationHelper.ToString(NC)); 
 
                string username = string.Empty;
                if (NC == null || (!(NC is SystemNetworkCredential) && (username = NC.InternalGetUserName()).Length == 0)) 
                {
                    return null;
                }
                // 
                // here we cover a hole in the SSPI layer. longer credentials
                // might corrupt the process and cause a reboot. 
                // 
                if (username.Length + NC.InternalGetPassword().Length + NC.InternalGetDomain().Length>NtlmClient.MaxNtlmCredentialSize) {
                    // 
                    // rather then throwing an exception here we return null so other packages can be used.
                    // this is questionable, hence:
                    // Consider: make this throw a NotSupportedException so it is discoverable
                    // 
                    return null;
                } 
 
                ICredentialPolicy policy = AuthenticationManager.CredentialPolicy;
                if (policy != null && !policy.ShouldSendCredential(httpWebRequest.ChallengedUri, httpWebRequest, NC, this)) 
                    return null;

                string spn = httpWebRequest.CurrentAuthenticationState.GetComputeSpn(httpWebRequest);
                GlobalLog.Print("NegotiateClient::Authenticate() ChallengedSpn:" + ValidationHelper.ToString(spn)); 

                authSession = 
                    new NTAuthentication( 
                        AuthType,
                        NC, 
                        spn,
                        httpWebRequest);

 
                GlobalLog.Print("NegotiateClient::DoAuthenticate() setting SecurityContext for:" + ValidationHelper.HashString(httpWebRequest.CurrentAuthenticationState) + " to authSession:" + ValidationHelper.HashString(authSession));
                httpWebRequest.CurrentAuthenticationState.SetSecurityContext(authSession, this); 
            } 

            string clientResponse = authSession.GetOutgoingBlob(incoming); 
            if (clientResponse==null) {
                return null;
            }
 
            bool canShareConnection = httpWebRequest.UnsafeOrProxyAuthenticatedConnectionSharing;
            if (canShareConnection) { 
                httpWebRequest.LockConnection = true; 
            }
 
            // this is the first leg of an NTLM handshake,
            // set the NtlmKeepAlive override *STRICTLY* only in this case.
            httpWebRequest.NtlmKeepAlive = incoming==null && authSession.IsValidContext && !authSession.IsKerberos;
 
            return AuthenticationManager.GetGroupAuthorization(this, AuthType + " " + clientResponse, authSession.IsCompleted, authSession, canShareConnection, authSession.IsKerberos);
        } 
 
        public bool CanPreAuthenticate {
            get { 
                return true;
            }
        }
 
        public Authorization PreAuthenticate(WebRequest webRequest, ICredentials credentials) {
            GlobalLog.Print("NegotiateClient::PreAuthenticate() webRequest#" + ValidationHelper.HashString(webRequest) + " credentials#" + ValidationHelper.HashString(credentials) + " calling DoAuthenticate()"); 
            return DoAuthenticate(null, webRequest, credentials, true); 
        }
 
        public string AuthenticationType {
            get {
                return AuthType;
            } 
        }
 
        // 
        // called when getting the final blob on the 200 OK from the server
        // 
        public bool Update(string challenge, WebRequest webRequest) {
            GlobalLog.Print("NegotiateClient::Update(): " + challenge);

            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest; 

            GlobalLog.Assert(httpWebRequest != null, "NegotiateClient::Update()|httpWebRequest == null"); 
            GlobalLog.Assert(httpWebRequest.ChallengedUri != null, "NegotiateClient::Update()|httpWebRequest.ChallengedUri == null"); 

            // 
            // try to retrieve the state of the ongoing handshake
            //

            NTAuthentication authSession = httpWebRequest.CurrentAuthenticationState.GetSecurityContext(this); 
            GlobalLog.Print("NegotiateClient::Update() key:" + ValidationHelper.HashString(httpWebRequest.CurrentAuthenticationState) + " retrieved authSession:" + ValidationHelper.HashString(authSession));
 
            if (authSession==null) { 
                GlobalLog.Print("NegotiateClient::Update() null session returning true");
                return true; 
            }

            GlobalLog.Print("NegotiateClient::Update() authSession.IsCompleted:" + authSession.IsCompleted.ToString());
 
            if (!authSession.IsCompleted && httpWebRequest.CurrentAuthenticationState.StatusCodeMatch==httpWebRequest.ResponseStatusCode) {
                GlobalLog.Print("NegotiateClient::Update() still handshaking (based on status code) returning false"); 
                return false; 
            }
 
            // now possibly close the ConnectionGroup after authentication is done.
            if (!httpWebRequest.UnsafeOrProxyAuthenticatedConnectionSharing) {
                GlobalLog.Print("NegotiateClient::Update() releasing ConnectionGroup:" + httpWebRequest.GetConnectionGroupLine());
                httpWebRequest.ServicePoint.ReleaseConnectionGroup(httpWebRequest.GetConnectionGroupLine()); 
            }
 
            // 
            // the whole point here is to close the Security Context (this will complete the authentication handshake
            // with server authentication for schemese that support it such as Kerberos) 
            //
            int index = challenge==null ? -1 : AuthenticationManager.FindSubstringNotInQuotes(challenge, Signature);
            if (index>=0) {
                int blobBegin = index + SignatureSize; 
                string incoming = null;
 
                // 
                // there may be multiple challenges. If the next character after the
                // package name is not a comma then it is challenge data 
                //
                if (challenge.Length > blobBegin && challenge[blobBegin] != ',') {
                    ++blobBegin;
                } else { 
                    index = -1;
                } 
                if (index >= 0 && challenge.Length > blobBegin) { 
                    incoming = challenge.Substring(blobBegin);
                } 
                GlobalLog.Print("NegotiateClient::Update() this must be a final incoming blob:[" + ValidationHelper.ToString(incoming) + "]");
                string clientResponse = authSession.GetOutgoingBlob(incoming);
                httpWebRequest.CurrentAuthenticationState.Authorization.MutuallyAuthenticated = authSession.IsMutualAuthFlag;
                GlobalLog.Print("NegotiateClient::Update() GetOutgoingBlob() returns clientResponse:[" + ValidationHelper.ToString(clientResponse) + "] IsCompleted:" + authSession.IsCompleted.ToString()); 
            }
            GlobalLog.Print("NegotiateClient::Update() session removed and ConnectionGroup released returning true"); 
            ClearSession(httpWebRequest); 
            return true;
        } 

        public void ClearSession(WebRequest webRequest) {
            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;
            GlobalLog.Assert(httpWebRequest != null, "NegotiateClient::ClearSession()|httpWebRequest == null"); 
            httpWebRequest.CurrentAuthenticationState.ClearSession();
        } 
 
        public bool CanUseDefaultCredentials {
            get { 
                return true;
            }
        }
 
    }; // class NegotiateClient
 
 
} // namespace System.Net
//------------------------------------------------------------------------------ 
// <copyright file="_NegotiateClient.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.Diagnostics; 
    using System.Collections;
    using System.Net.Sockets; 
    using System.Security.Permissions;
    using System.Globalization;

    internal class NegotiateClient : ISessionAuthenticationModule { 

        internal const string AuthType = "Negotiate"; 
        internal static string Signature = AuthType.ToLower(CultureInfo.InvariantCulture); 
        internal static int SignatureSize = Signature.Length;
 
        // we can't work on non-NT2K platforms or non Win, so we shut off,
        // NOTE this exception IS caught internally.
        public NegotiateClient() {
            if (!ComNetOS.IsWin2K) { 
                throw new PlatformNotSupportedException(SR.GetString(SR.Win2000Required));
            } 
        } 

        public Authorization Authenticate(string challenge, WebRequest webRequest, ICredentials credentials) { 
            GlobalLog.Print("NegotiateClient::Authenticate() challenge:[" + ValidationHelper.ToString(challenge) + "] webRequest#" + ValidationHelper.HashString(webRequest) + " credentials#" + ValidationHelper.HashString(credentials) + " calling DoAuthenticate()");
            return DoAuthenticate(challenge, webRequest, credentials, false);
        }
 
        private Authorization DoAuthenticate(string challenge, WebRequest webRequest, ICredentials credentials, bool preAuthenticate) {
            GlobalLog.Print("NegotiateClient::DoAuthenticate() challenge:[" + ValidationHelper.ToString(challenge) + "] webRequest#" + ValidationHelper.HashString(webRequest) + " credentials#" + ValidationHelper.HashString(credentials) + " preAuthenticate:" + preAuthenticate.ToString()); 
 
            GlobalLog.Assert(credentials != null, "NegotiateClient::DoAuthenticate()|credentials == null");
            if (credentials == null) { 
                return null;
            }

            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest; 

            GlobalLog.Assert(httpWebRequest != null, "NegotiateClient::DoAuthenticate()|httpWebRequest == null"); 
            GlobalLog.Assert(httpWebRequest.ChallengedUri != null, "NegotiateClient::DoAuthenticate()|httpWebRequest.ChallengedUri == null"); 

            NTAuthentication authSession = null; 
            string incoming = null;

            if (!preAuthenticate) {
                int index = AuthenticationManager.FindSubstringNotInQuotes(challenge, Signature); 
                if (index < 0) {
                    return null; 
                } 

                int blobBegin = index + SignatureSize; 

                //
                // there may be multiple challenges. If the next character after the
                // package name is not a comma then it is challenge data 
                //
 
                if (challenge.Length > blobBegin && challenge[blobBegin] != ',') { 
                    ++blobBegin;
                } 
                else {
                    index = -1;
                }
 
                if (index >= 0 && challenge.Length > blobBegin)
                { 
                    // Strip other modules information in case of multiple challenges 
                    // i.e do not take ", NTLM" as part of the following Negotiate blob
                    // Negotiate TlRMTVNTUAACAAAADgAOADgAAAA1wo ... MAbwBmAHQALgBjAG8AbQAAAAAA,NTLM 
                    index = challenge.IndexOf(',', blobBegin);
                    if (index != -1)
                        incoming = challenge.Substring(blobBegin, index - blobBegin);
                    else 
                        incoming = challenge.Substring(blobBegin);
                } 
 
                authSession = httpWebRequest.CurrentAuthenticationState.GetSecurityContext(this);
                GlobalLog.Print("NegotiateClient::DoAuthenticate() key:" + ValidationHelper.HashString(httpWebRequest.CurrentAuthenticationState) + " retrieved authSession:" + ValidationHelper.HashString(authSession)); 
            }

            if (authSession==null)
            { 
                NetworkCredential NC = credentials.GetCredential(httpWebRequest.ChallengedUri, Signature);
                GlobalLog.Print("NegotiateClient::DoAuthenticate() GetCredential() returns:" + ValidationHelper.ToString(NC)); 
 
                string username = string.Empty;
                if (NC == null || (!(NC is SystemNetworkCredential) && (username = NC.InternalGetUserName()).Length == 0)) 
                {
                    return null;
                }
                // 
                // here we cover a hole in the SSPI layer. longer credentials
                // might corrupt the process and cause a reboot. 
                // 
                if (username.Length + NC.InternalGetPassword().Length + NC.InternalGetDomain().Length>NtlmClient.MaxNtlmCredentialSize) {
                    // 
                    // rather then throwing an exception here we return null so other packages can be used.
                    // this is questionable, hence:
                    // Consider: make this throw a NotSupportedException so it is discoverable
                    // 
                    return null;
                } 
 
                ICredentialPolicy policy = AuthenticationManager.CredentialPolicy;
                if (policy != null && !policy.ShouldSendCredential(httpWebRequest.ChallengedUri, httpWebRequest, NC, this)) 
                    return null;

                string spn = httpWebRequest.CurrentAuthenticationState.GetComputeSpn(httpWebRequest);
                GlobalLog.Print("NegotiateClient::Authenticate() ChallengedSpn:" + ValidationHelper.ToString(spn)); 

                authSession = 
                    new NTAuthentication( 
                        AuthType,
                        NC, 
                        spn,
                        httpWebRequest);

 
                GlobalLog.Print("NegotiateClient::DoAuthenticate() setting SecurityContext for:" + ValidationHelper.HashString(httpWebRequest.CurrentAuthenticationState) + " to authSession:" + ValidationHelper.HashString(authSession));
                httpWebRequest.CurrentAuthenticationState.SetSecurityContext(authSession, this); 
            } 

            string clientResponse = authSession.GetOutgoingBlob(incoming); 
            if (clientResponse==null) {
                return null;
            }
 
            bool canShareConnection = httpWebRequest.UnsafeOrProxyAuthenticatedConnectionSharing;
            if (canShareConnection) { 
                httpWebRequest.LockConnection = true; 
            }
 
            // this is the first leg of an NTLM handshake,
            // set the NtlmKeepAlive override *STRICTLY* only in this case.
            httpWebRequest.NtlmKeepAlive = incoming==null && authSession.IsValidContext && !authSession.IsKerberos;
 
            return AuthenticationManager.GetGroupAuthorization(this, AuthType + " " + clientResponse, authSession.IsCompleted, authSession, canShareConnection, authSession.IsKerberos);
        } 
 
        public bool CanPreAuthenticate {
            get { 
                return true;
            }
        }
 
        public Authorization PreAuthenticate(WebRequest webRequest, ICredentials credentials) {
            GlobalLog.Print("NegotiateClient::PreAuthenticate() webRequest#" + ValidationHelper.HashString(webRequest) + " credentials#" + ValidationHelper.HashString(credentials) + " calling DoAuthenticate()"); 
            return DoAuthenticate(null, webRequest, credentials, true); 
        }
 
        public string AuthenticationType {
            get {
                return AuthType;
            } 
        }
 
        // 
        // called when getting the final blob on the 200 OK from the server
        // 
        public bool Update(string challenge, WebRequest webRequest) {
            GlobalLog.Print("NegotiateClient::Update(): " + challenge);

            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest; 

            GlobalLog.Assert(httpWebRequest != null, "NegotiateClient::Update()|httpWebRequest == null"); 
            GlobalLog.Assert(httpWebRequest.ChallengedUri != null, "NegotiateClient::Update()|httpWebRequest.ChallengedUri == null"); 

            // 
            // try to retrieve the state of the ongoing handshake
            //

            NTAuthentication authSession = httpWebRequest.CurrentAuthenticationState.GetSecurityContext(this); 
            GlobalLog.Print("NegotiateClient::Update() key:" + ValidationHelper.HashString(httpWebRequest.CurrentAuthenticationState) + " retrieved authSession:" + ValidationHelper.HashString(authSession));
 
            if (authSession==null) { 
                GlobalLog.Print("NegotiateClient::Update() null session returning true");
                return true; 
            }

            GlobalLog.Print("NegotiateClient::Update() authSession.IsCompleted:" + authSession.IsCompleted.ToString());
 
            if (!authSession.IsCompleted && httpWebRequest.CurrentAuthenticationState.StatusCodeMatch==httpWebRequest.ResponseStatusCode) {
                GlobalLog.Print("NegotiateClient::Update() still handshaking (based on status code) returning false"); 
                return false; 
            }
 
            // now possibly close the ConnectionGroup after authentication is done.
            if (!httpWebRequest.UnsafeOrProxyAuthenticatedConnectionSharing) {
                GlobalLog.Print("NegotiateClient::Update() releasing ConnectionGroup:" + httpWebRequest.GetConnectionGroupLine());
                httpWebRequest.ServicePoint.ReleaseConnectionGroup(httpWebRequest.GetConnectionGroupLine()); 
            }
 
            // 
            // the whole point here is to close the Security Context (this will complete the authentication handshake
            // with server authentication for schemese that support it such as Kerberos) 
            //
            int index = challenge==null ? -1 : AuthenticationManager.FindSubstringNotInQuotes(challenge, Signature);
            if (index>=0) {
                int blobBegin = index + SignatureSize; 
                string incoming = null;
 
                // 
                // there may be multiple challenges. If the next character after the
                // package name is not a comma then it is challenge data 
                //
                if (challenge.Length > blobBegin && challenge[blobBegin] != ',') {
                    ++blobBegin;
                } else { 
                    index = -1;
                } 
                if (index >= 0 && challenge.Length > blobBegin) { 
                    incoming = challenge.Substring(blobBegin);
                } 
                GlobalLog.Print("NegotiateClient::Update() this must be a final incoming blob:[" + ValidationHelper.ToString(incoming) + "]");
                string clientResponse = authSession.GetOutgoingBlob(incoming);
                httpWebRequest.CurrentAuthenticationState.Authorization.MutuallyAuthenticated = authSession.IsMutualAuthFlag;
                GlobalLog.Print("NegotiateClient::Update() GetOutgoingBlob() returns clientResponse:[" + ValidationHelper.ToString(clientResponse) + "] IsCompleted:" + authSession.IsCompleted.ToString()); 
            }
            GlobalLog.Print("NegotiateClient::Update() session removed and ConnectionGroup released returning true"); 
            ClearSession(httpWebRequest); 
            return true;
        } 

        public void ClearSession(WebRequest webRequest) {
            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;
            GlobalLog.Assert(httpWebRequest != null, "NegotiateClient::ClearSession()|httpWebRequest == null"); 
            httpWebRequest.CurrentAuthenticationState.ClearSession();
        } 
 
        public bool CanUseDefaultCredentials {
            get { 
                return true;
            }
        }
 
    }; // class NegotiateClient
 
 
} // namespace System.Net
