//------------------------------------------------------------------------------ 
// <copyright file="SmtpTransport.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mail 
{ 
    using System;
    using System.Collections; 
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets; 
    using System.Text;
    using System.Net.Security; 
    using System.Security.Cryptography.X509Certificates; 
    using System.Net.Mime;
    using System.Security.Principal; 
    using System.Security.Permissions;
    using System.Threading;

    internal enum SupportedAuth{ 
        None = 0, Login = 1,
#if !FEATURE_PAL 
        NTLM = 2, GGSAPI = 4, WDigest = 8 
#endif
    }; 

    internal class SmtpPooledStream:PooledStream{
        internal bool previouslyUsed;
        internal bool dsnEnabled;  //delivery  status notification 
        internal ICredentialsByHost creds;
        internal SmtpPooledStream(ConnectionPool connectionPool, TimeSpan lifetime, bool checkLifetime) : base (connectionPool,lifetime,checkLifetime) { 
        } 
    }
 
    internal class SmtpTransport
    {
        internal const int DefaultPort = 25;
 
        ISmtpAuthenticationModule[] authenticationModules;
        SmtpConnection connection; 
        SmtpClient client; 
        ICredentialsByHost credentials;
        int timeout = 100000; // seconds 
        ArrayList failedRecipientExceptions = new ArrayList();
        bool m_IdentityRequired;

        bool enableSsl = false; 
        X509CertificateCollection clientCertificates = null;
 
        internal SmtpTransport(SmtpClient client) : this(client, SmtpAuthenticationManager.GetModules()) { 
        }
 

        internal SmtpTransport(SmtpClient client, ISmtpAuthenticationModule[] authenticationModules)
        {
            this.client = client; 

            if (authenticationModules == null) 
            { 
                throw new ArgumentNullException("authenticationModules");
            } 

            this.authenticationModules = authenticationModules;
        }
 
        internal ICredentialsByHost Credentials
        { 
            get 
            {
                return credentials; 
            }
            set
            {
                credentials = value; 
            }
        } 
 
        internal bool IdentityRequired
        { 
            get
            {
                return m_IdentityRequired;
            } 

            set 
            { 
                m_IdentityRequired = value;
            } 
        }

        internal bool IsConnected
        { 
            get
            { 
                return connection != null && connection.IsConnected; 
            }
        } 

        internal int Timeout
        {
            get 
            {
                return timeout; 
            } 
            set
            { 
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                } 

                timeout = value; 
            } 
        }
 
        internal bool EnableSsl
        {
            get
            { 
                return enableSsl;
            } 
            set 
            {
#if !FEATURE_PAL 
                enableSsl = value;
#else
                throw new NotImplementedException("ROTORTODO");
#endif 
            }
        } 
 
        internal X509CertificateCollection ClientCertificates
        { 
            get {
                if (clientCertificates == null) {
                    clientCertificates = new X509CertificateCollection();
                } 
                return clientCertificates;
            } 
        } 

 
        internal void GetConnection(string host, int port)
        {
            if (host == null)
            { 
                throw new ArgumentNullException("host");
            } 
 
            if (port < 0 || port > 65535)
            { 
                throw new ArgumentOutOfRangeException("port");
            }

 
            try {
                connection = new SmtpConnection(this, client, credentials, authenticationModules); 
                connection.Timeout = timeout; 
                if(Logging.On)Logging.Associate(Logging.Web, this, connection);
 
                if (EnableSsl)
                {
                    connection.EnableSsl = true;
                    connection.ClientCertificates = ClientCertificates; 
                }
                connection.GetConnection(host, port); 
            } 
            finally {
 
            }
        }

 
        internal IAsyncResult BeginGetConnection(string host, int port, ContextAwareResult outerResult, AsyncCallback callback, object state)
        { 
            GlobalLog.Enter("SmtpTransport#" + ValidationHelper.HashString(this) + "::BeginConnect"); 
            if (host == null)
            { 
                throw new ArgumentNullException("host");
            }

            if (port < 0 || port > 65535) 
            {
                throw new ArgumentOutOfRangeException("port"); 
            } 

 


            IAsyncResult result = null;
            try{ 
                connection = new SmtpConnection(this, client, credentials, authenticationModules);
                connection.Timeout = timeout; 
                if(Logging.On)Logging.Associate(Logging.Web, this, connection); 
                if (EnableSsl)
                { 
                    connection.EnableSsl = true;
                    connection.ClientCertificates = ClientCertificates;
                }
 
                result = connection.BeginGetConnection(host, port, outerResult, callback, state);
            } 
            catch(Exception innerException){ 
                throw new SmtpException(SR.GetString(SR.MailHostNotFound), innerException);
            } 
            catch {
                throw new SmtpException(SR.GetString(SR.MailHostNotFound), new Exception(SR.GetString(SR.net_nonClsCompliantException)));
            }
            GlobalLog.Leave("SmtpTransport#" + ValidationHelper.HashString(this) + "::BeginConnect Sync Completion"); 
            return result;
        } 
 

        internal void EndGetConnection(IAsyncResult result) 
        {
            GlobalLog.Enter("SmtpTransport#" + ValidationHelper.HashString(this) + "::EndGetConnection");
            try {
                connection.EndGetConnection(result); 
            }
            finally { 
 
                GlobalLog.Leave("SmtpTransport#" + ValidationHelper.HashString(this) + "::EndConnect");
            } 
        }


        internal IAsyncResult BeginSendMail(MailAddress sender, MailAddressCollection recipients, string deliveryNotify, AsyncCallback callback, object state) 
        {
            if (sender == null) 
            { 
                throw new ArgumentNullException("sender");
            } 

            if (recipients == null)
            {
                throw new ArgumentNullException("recipients"); 
            }
 
            GlobalLog.Assert(recipients.Count > 0, "SmtpTransport::BeginSendMail()|recepients.Count <= 0"); 

            SendMailAsyncResult result = new SendMailAsyncResult(connection, sender.SmtpAddress, recipients, connection.DSNEnabled?deliveryNotify:null, callback, state); 
            result.Send();
            return result;
        }
 

        internal void ReleaseConnection() { 
            if(connection != null){ 
                connection.ReleaseConnection();
            } 
        }

        internal void Abort() {
            if(connection != null){ 
                connection.Abort();
            } 
        } 

 
        internal MailWriter EndSendMail(IAsyncResult result)
        {
            try {
                return SendMailAsyncResult.End(result); 
            }
            finally { 
 
            }
        } 

        internal MailWriter SendMail(MailAddress sender, MailAddressCollection recipients, string deliveryNotify, out SmtpFailedRecipientException exception)
        {
            if (sender == null) 
            {
                throw new ArgumentNullException("sender"); 
            } 

            if (recipients == null) 
            {
                throw new ArgumentNullException("recipients");
            }
 
            GlobalLog.Assert(recipients.Count > 0, "SmtpTransport::SendMail()|recepients.Count <= 0");
 
            MailCommand.Send(connection, SmtpCommands.Mail, sender.SmtpAddress); 
            failedRecipientExceptions.Clear();
 
            exception = null;
            string response;
            foreach (MailAddress address in recipients) {
                if (!RecipientCommand.Send(connection, connection.DSNEnabled?address.SmtpAddress + deliveryNotify:address.SmtpAddress, out response)) { 
                    failedRecipientExceptions.Add(new SmtpFailedRecipientException(connection.Reader.StatusCode, address.SmtpAddress, response));
                } 
            } 

            if (failedRecipientExceptions.Count > 0) 
            {
                if (failedRecipientExceptions.Count == 1)
                {
                    exception = (SmtpFailedRecipientException) failedRecipientExceptions[0]; 
                }
                else 
                { 
                    exception = new SmtpFailedRecipientsException(failedRecipientExceptions, failedRecipientExceptions.Count == recipients.Count);
                } 

                if (failedRecipientExceptions.Count == recipients.Count){
                    exception.fatal = true;
                    throw exception; 
                }
            } 
 
            DataCommand.Send(connection);
            return new MailWriter(connection.GetClosableStream()); 
        }
    }

 
    class SendMailAsyncResult : LazyAsyncResult
    { 
        SmtpConnection connection; 
        string from;
        string deliveryNotify; 
        static AsyncCallback sendMailFromCompleted = new AsyncCallback(SendMailFromCompleted);
        static AsyncCallback sendToCompleted = new AsyncCallback(SendToCompleted);
        static AsyncCallback sendToCollectionCompleted = new AsyncCallback(SendToCollectionCompleted);
        static AsyncCallback sendDataCompleted = new AsyncCallback(SendDataCompleted); 
        ArrayList failedRecipientExceptions = new ArrayList();
        Stream stream; 
        string to; 
        MailAddressCollection toCollection;
        int toIndex; 


        internal SendMailAsyncResult(SmtpConnection connection, string from, MailAddressCollection toCollection, string deliveryNotify, AsyncCallback callback, object state) : base(null, state, callback)
        { 
            this.toCollection = toCollection;
            this.connection = connection; 
            this.from = from; 
            this.deliveryNotify = deliveryNotify;
        } 

        internal void Send(){
            SendMailFrom();
        } 

        internal static MailWriter End(IAsyncResult result) 
        { 
            SendMailAsyncResult thisPtr = (SendMailAsyncResult)result;
            object sendMailResult = thisPtr.InternalWaitForCompletion(); 
            if (sendMailResult is Exception)
                throw (Exception)sendMailResult;
            return new MailWriter(thisPtr.stream);
        } 
        void SendMailFrom()
        { 
            IAsyncResult result = MailCommand.BeginSend(connection, SmtpCommands.Mail, from, sendMailFromCompleted, this); 
            if (!result.CompletedSynchronously)
            { 
                return;
            }

            MailCommand.EndSend(result); 
            SendTo();
        } 
 
        static void SendMailFromCompleted(IAsyncResult result)
        { 
            if (!result.CompletedSynchronously)
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState;
                try 
                {
                    MailCommand.EndSend(result); 
                    thisPtr.SendTo(); 
                }
                catch (Exception e) 
                {
                    thisPtr.InvokeCallback(e);
                }
                catch { 
                    thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException)));
                } 
            } 
        }
 
        void SendTo()
        {
            GlobalLog.Enter("SendMailAsyncResult#" + ValidationHelper.HashString(this) + "::SendTo");
            if (to != null) 
            {
                GlobalLog.Print("SendMailAsyncResult#" + ValidationHelper.HashString(this) + "::SendTo - to string"); 
                IAsyncResult result = RecipientCommand.BeginSend(connection, (deliveryNotify!=null)?to + deliveryNotify:to, sendToCompleted, this); 
                if (!result.CompletedSynchronously)
                { 
                    return;
                }
                string response;
                if (!RecipientCommand.EndSend(result, out response)) 
                {
                    throw new SmtpFailedRecipientException(connection.Reader.StatusCode, to, response); 
                } 
                SendData();
            } 

            else
            {
                GlobalLog.Print("SendMailAsyncResult#" + ValidationHelper.HashString(this) + "::SendTo - to collection"); 
                if (SendToCollection())
                { 
                    SendData(); 
                }
            } 

            GlobalLog.Leave("SendMailAsyncResult#" + ValidationHelper.HashString(this) + "::SendTo");
        }
 
        static void SendToCompleted(IAsyncResult result)
        { 
            if (!result.CompletedSynchronously) 
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState; 
                try
                {
                    string response;
                    if (RecipientCommand.EndSend(result, out response)) 
                    {
                        thisPtr.SendData(); 
                    } 
                    else
                    { 
                        thisPtr.InvokeCallback(new SmtpFailedRecipientException(thisPtr.connection.Reader.StatusCode, thisPtr.to, response));
                    }
                }
                catch (Exception e) 
                {
                    thisPtr.InvokeCallback(e); 
                } 
                catch {
                    thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                }
            }
        }
 
        bool SendToCollection()
        { 
            while (toIndex < toCollection.Count) 
            {
                MultiAsyncResult result = (MultiAsyncResult)RecipientCommand.BeginSend(connection, toCollection[toIndex++].SmtpAddress + deliveryNotify, sendToCollectionCompleted, this); 
                if (!result.CompletedSynchronously)
                {
                    return false;
                } 
                string response;
                if (!RecipientCommand.EndSend(result, out response)){ 
                    failedRecipientExceptions.Add(new SmtpFailedRecipientException(connection.Reader.StatusCode, toCollection[toIndex - 1].SmtpAddress, response)); 
                }
            } 
            return true;
        }

        static void SendToCollectionCompleted(IAsyncResult result) 
        {
            if (!result.CompletedSynchronously) 
            { 
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState;
                try 
                {
                    string response;
                    if (!RecipientCommand.EndSend(result, out response))
                    { 
                        thisPtr.failedRecipientExceptions.Add(new SmtpFailedRecipientException(thisPtr.connection.Reader.StatusCode, thisPtr.toCollection[thisPtr.toIndex - 1].SmtpAddress, response));
 
                        if (thisPtr.failedRecipientExceptions.Count == thisPtr.toCollection.Count) 
                        {
                            SmtpFailedRecipientException exception = null; 
                            if (thisPtr.toCollection.Count == 1)
                            {
                                exception = (SmtpFailedRecipientException)thisPtr.failedRecipientExceptions[0];
                            } 
                            else
                            { 
                                exception = new SmtpFailedRecipientsException(thisPtr.failedRecipientExceptions, true); 
                            }
                            exception.fatal = true; 
                            thisPtr.InvokeCallback(exception);
                            return;
                        }
                    } 
                    if (thisPtr.SendToCollection())
                    { 
                        thisPtr.SendData(); 
                    }
                } 
                catch (Exception e)
                {
                    thisPtr.InvokeCallback(e);
                } 
                catch {
                    thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                } 
            }
        } 

        void SendData()
        {
            IAsyncResult result = DataCommand.BeginSend(connection, sendDataCompleted, this); 
            if (!result.CompletedSynchronously)
            { 
                return; 
            }
            DataCommand.EndSend(result); 
            stream = connection.GetClosableStream();
            if (failedRecipientExceptions.Count > 1)
            {
                InvokeCallback(new SmtpFailedRecipientsException(failedRecipientExceptions, failedRecipientExceptions.Count == toCollection.Count)); 
            }
            else if (failedRecipientExceptions.Count == 1) 
            { 
                InvokeCallback(failedRecipientExceptions[0]);
            } 
            else
            {
                InvokeCallback();
            } 
        }
 
        static void SendDataCompleted(IAsyncResult result) 
        {
            if (!result.CompletedSynchronously) 
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState;
                try
                { 
                    DataCommand.EndSend(result);
                    thisPtr.stream = thisPtr.connection.GetClosableStream(); 
                    if (thisPtr.failedRecipientExceptions.Count > 1) 
                    {
                        thisPtr.InvokeCallback(new SmtpFailedRecipientsException(thisPtr.failedRecipientExceptions, thisPtr.failedRecipientExceptions.Count == thisPtr.toCollection.Count)); 
                    }
                    else if (thisPtr.failedRecipientExceptions.Count == 1)
                    {
                        thisPtr.InvokeCallback(thisPtr.failedRecipientExceptions[0]); 
                    }
                    else 
                    { 
                        thisPtr.InvokeCallback();
                    } 
                }
                catch (Exception e)
                {
                    thisPtr.InvokeCallback(e); 
                }
                catch { 
                    thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                }
            } 
        }
    }
 }
//------------------------------------------------------------------------------ 
// <copyright file="SmtpTransport.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mail 
{ 
    using System;
    using System.Collections; 
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets; 
    using System.Text;
    using System.Net.Security; 
    using System.Security.Cryptography.X509Certificates; 
    using System.Net.Mime;
    using System.Security.Principal; 
    using System.Security.Permissions;
    using System.Threading;

    internal enum SupportedAuth{ 
        None = 0, Login = 1,
#if !FEATURE_PAL 
        NTLM = 2, GGSAPI = 4, WDigest = 8 
#endif
    }; 

    internal class SmtpPooledStream:PooledStream{
        internal bool previouslyUsed;
        internal bool dsnEnabled;  //delivery  status notification 
        internal ICredentialsByHost creds;
        internal SmtpPooledStream(ConnectionPool connectionPool, TimeSpan lifetime, bool checkLifetime) : base (connectionPool,lifetime,checkLifetime) { 
        } 
    }
 
    internal class SmtpTransport
    {
        internal const int DefaultPort = 25;
 
        ISmtpAuthenticationModule[] authenticationModules;
        SmtpConnection connection; 
        SmtpClient client; 
        ICredentialsByHost credentials;
        int timeout = 100000; // seconds 
        ArrayList failedRecipientExceptions = new ArrayList();
        bool m_IdentityRequired;

        bool enableSsl = false; 
        X509CertificateCollection clientCertificates = null;
 
        internal SmtpTransport(SmtpClient client) : this(client, SmtpAuthenticationManager.GetModules()) { 
        }
 

        internal SmtpTransport(SmtpClient client, ISmtpAuthenticationModule[] authenticationModules)
        {
            this.client = client; 

            if (authenticationModules == null) 
            { 
                throw new ArgumentNullException("authenticationModules");
            } 

            this.authenticationModules = authenticationModules;
        }
 
        internal ICredentialsByHost Credentials
        { 
            get 
            {
                return credentials; 
            }
            set
            {
                credentials = value; 
            }
        } 
 
        internal bool IdentityRequired
        { 
            get
            {
                return m_IdentityRequired;
            } 

            set 
            { 
                m_IdentityRequired = value;
            } 
        }

        internal bool IsConnected
        { 
            get
            { 
                return connection != null && connection.IsConnected; 
            }
        } 

        internal int Timeout
        {
            get 
            {
                return timeout; 
            } 
            set
            { 
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                } 

                timeout = value; 
            } 
        }
 
        internal bool EnableSsl
        {
            get
            { 
                return enableSsl;
            } 
            set 
            {
#if !FEATURE_PAL 
                enableSsl = value;
#else
                throw new NotImplementedException("ROTORTODO");
#endif 
            }
        } 
 
        internal X509CertificateCollection ClientCertificates
        { 
            get {
                if (clientCertificates == null) {
                    clientCertificates = new X509CertificateCollection();
                } 
                return clientCertificates;
            } 
        } 

 
        internal void GetConnection(string host, int port)
        {
            if (host == null)
            { 
                throw new ArgumentNullException("host");
            } 
 
            if (port < 0 || port > 65535)
            { 
                throw new ArgumentOutOfRangeException("port");
            }

 
            try {
                connection = new SmtpConnection(this, client, credentials, authenticationModules); 
                connection.Timeout = timeout; 
                if(Logging.On)Logging.Associate(Logging.Web, this, connection);
 
                if (EnableSsl)
                {
                    connection.EnableSsl = true;
                    connection.ClientCertificates = ClientCertificates; 
                }
                connection.GetConnection(host, port); 
            } 
            finally {
 
            }
        }

 
        internal IAsyncResult BeginGetConnection(string host, int port, ContextAwareResult outerResult, AsyncCallback callback, object state)
        { 
            GlobalLog.Enter("SmtpTransport#" + ValidationHelper.HashString(this) + "::BeginConnect"); 
            if (host == null)
            { 
                throw new ArgumentNullException("host");
            }

            if (port < 0 || port > 65535) 
            {
                throw new ArgumentOutOfRangeException("port"); 
            } 

 


            IAsyncResult result = null;
            try{ 
                connection = new SmtpConnection(this, client, credentials, authenticationModules);
                connection.Timeout = timeout; 
                if(Logging.On)Logging.Associate(Logging.Web, this, connection); 
                if (EnableSsl)
                { 
                    connection.EnableSsl = true;
                    connection.ClientCertificates = ClientCertificates;
                }
 
                result = connection.BeginGetConnection(host, port, outerResult, callback, state);
            } 
            catch(Exception innerException){ 
                throw new SmtpException(SR.GetString(SR.MailHostNotFound), innerException);
            } 
            catch {
                throw new SmtpException(SR.GetString(SR.MailHostNotFound), new Exception(SR.GetString(SR.net_nonClsCompliantException)));
            }
            GlobalLog.Leave("SmtpTransport#" + ValidationHelper.HashString(this) + "::BeginConnect Sync Completion"); 
            return result;
        } 
 

        internal void EndGetConnection(IAsyncResult result) 
        {
            GlobalLog.Enter("SmtpTransport#" + ValidationHelper.HashString(this) + "::EndGetConnection");
            try {
                connection.EndGetConnection(result); 
            }
            finally { 
 
                GlobalLog.Leave("SmtpTransport#" + ValidationHelper.HashString(this) + "::EndConnect");
            } 
        }


        internal IAsyncResult BeginSendMail(MailAddress sender, MailAddressCollection recipients, string deliveryNotify, AsyncCallback callback, object state) 
        {
            if (sender == null) 
            { 
                throw new ArgumentNullException("sender");
            } 

            if (recipients == null)
            {
                throw new ArgumentNullException("recipients"); 
            }
 
            GlobalLog.Assert(recipients.Count > 0, "SmtpTransport::BeginSendMail()|recepients.Count <= 0"); 

            SendMailAsyncResult result = new SendMailAsyncResult(connection, sender.SmtpAddress, recipients, connection.DSNEnabled?deliveryNotify:null, callback, state); 
            result.Send();
            return result;
        }
 

        internal void ReleaseConnection() { 
            if(connection != null){ 
                connection.ReleaseConnection();
            } 
        }

        internal void Abort() {
            if(connection != null){ 
                connection.Abort();
            } 
        } 

 
        internal MailWriter EndSendMail(IAsyncResult result)
        {
            try {
                return SendMailAsyncResult.End(result); 
            }
            finally { 
 
            }
        } 

        internal MailWriter SendMail(MailAddress sender, MailAddressCollection recipients, string deliveryNotify, out SmtpFailedRecipientException exception)
        {
            if (sender == null) 
            {
                throw new ArgumentNullException("sender"); 
            } 

            if (recipients == null) 
            {
                throw new ArgumentNullException("recipients");
            }
 
            GlobalLog.Assert(recipients.Count > 0, "SmtpTransport::SendMail()|recepients.Count <= 0");
 
            MailCommand.Send(connection, SmtpCommands.Mail, sender.SmtpAddress); 
            failedRecipientExceptions.Clear();
 
            exception = null;
            string response;
            foreach (MailAddress address in recipients) {
                if (!RecipientCommand.Send(connection, connection.DSNEnabled?address.SmtpAddress + deliveryNotify:address.SmtpAddress, out response)) { 
                    failedRecipientExceptions.Add(new SmtpFailedRecipientException(connection.Reader.StatusCode, address.SmtpAddress, response));
                } 
            } 

            if (failedRecipientExceptions.Count > 0) 
            {
                if (failedRecipientExceptions.Count == 1)
                {
                    exception = (SmtpFailedRecipientException) failedRecipientExceptions[0]; 
                }
                else 
                { 
                    exception = new SmtpFailedRecipientsException(failedRecipientExceptions, failedRecipientExceptions.Count == recipients.Count);
                } 

                if (failedRecipientExceptions.Count == recipients.Count){
                    exception.fatal = true;
                    throw exception; 
                }
            } 
 
            DataCommand.Send(connection);
            return new MailWriter(connection.GetClosableStream()); 
        }
    }

 
    class SendMailAsyncResult : LazyAsyncResult
    { 
        SmtpConnection connection; 
        string from;
        string deliveryNotify; 
        static AsyncCallback sendMailFromCompleted = new AsyncCallback(SendMailFromCompleted);
        static AsyncCallback sendToCompleted = new AsyncCallback(SendToCompleted);
        static AsyncCallback sendToCollectionCompleted = new AsyncCallback(SendToCollectionCompleted);
        static AsyncCallback sendDataCompleted = new AsyncCallback(SendDataCompleted); 
        ArrayList failedRecipientExceptions = new ArrayList();
        Stream stream; 
        string to; 
        MailAddressCollection toCollection;
        int toIndex; 


        internal SendMailAsyncResult(SmtpConnection connection, string from, MailAddressCollection toCollection, string deliveryNotify, AsyncCallback callback, object state) : base(null, state, callback)
        { 
            this.toCollection = toCollection;
            this.connection = connection; 
            this.from = from; 
            this.deliveryNotify = deliveryNotify;
        } 

        internal void Send(){
            SendMailFrom();
        } 

        internal static MailWriter End(IAsyncResult result) 
        { 
            SendMailAsyncResult thisPtr = (SendMailAsyncResult)result;
            object sendMailResult = thisPtr.InternalWaitForCompletion(); 
            if (sendMailResult is Exception)
                throw (Exception)sendMailResult;
            return new MailWriter(thisPtr.stream);
        } 
        void SendMailFrom()
        { 
            IAsyncResult result = MailCommand.BeginSend(connection, SmtpCommands.Mail, from, sendMailFromCompleted, this); 
            if (!result.CompletedSynchronously)
            { 
                return;
            }

            MailCommand.EndSend(result); 
            SendTo();
        } 
 
        static void SendMailFromCompleted(IAsyncResult result)
        { 
            if (!result.CompletedSynchronously)
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState;
                try 
                {
                    MailCommand.EndSend(result); 
                    thisPtr.SendTo(); 
                }
                catch (Exception e) 
                {
                    thisPtr.InvokeCallback(e);
                }
                catch { 
                    thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException)));
                } 
            } 
        }
 
        void SendTo()
        {
            GlobalLog.Enter("SendMailAsyncResult#" + ValidationHelper.HashString(this) + "::SendTo");
            if (to != null) 
            {
                GlobalLog.Print("SendMailAsyncResult#" + ValidationHelper.HashString(this) + "::SendTo - to string"); 
                IAsyncResult result = RecipientCommand.BeginSend(connection, (deliveryNotify!=null)?to + deliveryNotify:to, sendToCompleted, this); 
                if (!result.CompletedSynchronously)
                { 
                    return;
                }
                string response;
                if (!RecipientCommand.EndSend(result, out response)) 
                {
                    throw new SmtpFailedRecipientException(connection.Reader.StatusCode, to, response); 
                } 
                SendData();
            } 

            else
            {
                GlobalLog.Print("SendMailAsyncResult#" + ValidationHelper.HashString(this) + "::SendTo - to collection"); 
                if (SendToCollection())
                { 
                    SendData(); 
                }
            } 

            GlobalLog.Leave("SendMailAsyncResult#" + ValidationHelper.HashString(this) + "::SendTo");
        }
 
        static void SendToCompleted(IAsyncResult result)
        { 
            if (!result.CompletedSynchronously) 
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState; 
                try
                {
                    string response;
                    if (RecipientCommand.EndSend(result, out response)) 
                    {
                        thisPtr.SendData(); 
                    } 
                    else
                    { 
                        thisPtr.InvokeCallback(new SmtpFailedRecipientException(thisPtr.connection.Reader.StatusCode, thisPtr.to, response));
                    }
                }
                catch (Exception e) 
                {
                    thisPtr.InvokeCallback(e); 
                } 
                catch {
                    thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                }
            }
        }
 
        bool SendToCollection()
        { 
            while (toIndex < toCollection.Count) 
            {
                MultiAsyncResult result = (MultiAsyncResult)RecipientCommand.BeginSend(connection, toCollection[toIndex++].SmtpAddress + deliveryNotify, sendToCollectionCompleted, this); 
                if (!result.CompletedSynchronously)
                {
                    return false;
                } 
                string response;
                if (!RecipientCommand.EndSend(result, out response)){ 
                    failedRecipientExceptions.Add(new SmtpFailedRecipientException(connection.Reader.StatusCode, toCollection[toIndex - 1].SmtpAddress, response)); 
                }
            } 
            return true;
        }

        static void SendToCollectionCompleted(IAsyncResult result) 
        {
            if (!result.CompletedSynchronously) 
            { 
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState;
                try 
                {
                    string response;
                    if (!RecipientCommand.EndSend(result, out response))
                    { 
                        thisPtr.failedRecipientExceptions.Add(new SmtpFailedRecipientException(thisPtr.connection.Reader.StatusCode, thisPtr.toCollection[thisPtr.toIndex - 1].SmtpAddress, response));
 
                        if (thisPtr.failedRecipientExceptions.Count == thisPtr.toCollection.Count) 
                        {
                            SmtpFailedRecipientException exception = null; 
                            if (thisPtr.toCollection.Count == 1)
                            {
                                exception = (SmtpFailedRecipientException)thisPtr.failedRecipientExceptions[0];
                            } 
                            else
                            { 
                                exception = new SmtpFailedRecipientsException(thisPtr.failedRecipientExceptions, true); 
                            }
                            exception.fatal = true; 
                            thisPtr.InvokeCallback(exception);
                            return;
                        }
                    } 
                    if (thisPtr.SendToCollection())
                    { 
                        thisPtr.SendData(); 
                    }
                } 
                catch (Exception e)
                {
                    thisPtr.InvokeCallback(e);
                } 
                catch {
                    thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                } 
            }
        } 

        void SendData()
        {
            IAsyncResult result = DataCommand.BeginSend(connection, sendDataCompleted, this); 
            if (!result.CompletedSynchronously)
            { 
                return; 
            }
            DataCommand.EndSend(result); 
            stream = connection.GetClosableStream();
            if (failedRecipientExceptions.Count > 1)
            {
                InvokeCallback(new SmtpFailedRecipientsException(failedRecipientExceptions, failedRecipientExceptions.Count == toCollection.Count)); 
            }
            else if (failedRecipientExceptions.Count == 1) 
            { 
                InvokeCallback(failedRecipientExceptions[0]);
            } 
            else
            {
                InvokeCallback();
            } 
        }
 
        static void SendDataCompleted(IAsyncResult result) 
        {
            if (!result.CompletedSynchronously) 
            {
                SendMailAsyncResult thisPtr = (SendMailAsyncResult)result.AsyncState;
                try
                { 
                    DataCommand.EndSend(result);
                    thisPtr.stream = thisPtr.connection.GetClosableStream(); 
                    if (thisPtr.failedRecipientExceptions.Count > 1) 
                    {
                        thisPtr.InvokeCallback(new SmtpFailedRecipientsException(thisPtr.failedRecipientExceptions, thisPtr.failedRecipientExceptions.Count == thisPtr.toCollection.Count)); 
                    }
                    else if (thisPtr.failedRecipientExceptions.Count == 1)
                    {
                        thisPtr.InvokeCallback(thisPtr.failedRecipientExceptions[0]); 
                    }
                    else 
                    { 
                        thisPtr.InvokeCallback();
                    } 
                }
                catch (Exception e)
                {
                    thisPtr.InvokeCallback(e); 
                }
                catch { 
                    thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                }
            } 
        }
    }
 }
