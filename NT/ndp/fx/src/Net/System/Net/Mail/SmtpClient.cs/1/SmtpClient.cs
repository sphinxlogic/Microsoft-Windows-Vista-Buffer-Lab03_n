namespace System.Net.Mail 
{

    using System;
    using System.IO; 
    using System.Net;
    using System.Collections; 
    using System.ComponentModel; 
    using System.Configuration;
    using System.Net.Configuration; 
    using System.Threading;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Authentication; 
    using System.Security.Cryptography.X509Certificates;
    using System.Net.NetworkInformation; 
    using System.Runtime.Versioning; 

    public delegate void SendCompletedEventHandler(object sender, AsyncCompletedEventArgs e); 

    public enum SmtpDeliveryMethod {
        Network,
        SpecifiedPickupDirectory, 
#if !FEATURE_PAL
        PickupDirectoryFromIis 
#endif 
    }
 

    public class SmtpClient {

        string host; 
        int port;
        bool inCall; 
        bool cancelled; 
        bool timedOut;
        SmtpDeliveryMethod deliveryMethod = SmtpDeliveryMethod.Network; 
        string pickupDirectoryLocation = null;
        SmtpTransport transport;
        MailMessage message; //required to prevent premature finalization
        MailWriter writer; 
        MailAddressCollection recipients;
        SendOrPostCallback onSendCompletedDelegate; 
        Timer timer; 
        static MailSettingsSectionGroupInternal mailConfiguration;
        ContextAwareResult operationCompletedResult = null; 
        AsyncOperation asyncOp = null;
        static AsyncCallback _ContextSafeCompleteCallback = new AsyncCallback(ContextSafeCompleteCallback);
        static int defaultPort = 25;
        internal string localHostName; 

        public event SendCompletedEventHandler SendCompleted; 
 
        public SmtpClient(){
            if(Logging.On)Logging.Enter(Logging.Web, "SmtpClient", ".ctor", ""); 
            try {
                Initialize();
            } finally {
                if(Logging.On)Logging.Exit(Logging.Web, "SmtpClient", ".ctor", this); 
            }
        } 
 
        public SmtpClient(string host){
            if(Logging.On)Logging.Enter(Logging.Web, "SmtpClient", ".ctor", "host="+host); 
            try {
                this.host = host;
                Initialize();
            } finally { 
                if(Logging.On)Logging.Exit(Logging.Web, "SmtpClient", ".ctor", this);
            } 
        } 

        //?? should port throw or just default on 0? 
        public SmtpClient(string host, int port) {
            if(Logging.On)Logging.Enter(Logging.Web, "SmtpClient", ".ctor", "host="+host+", port="+port);
            try {
                if (port < 0){ 
                   throw new ArgumentOutOfRangeException("port");
                } 
 
                this.host = host;
                this.port = port; 
                Initialize();
            } finally {
                if(Logging.On)Logging.Exit(Logging.Web, "SmtpClient", ".ctor", this);
            } 
        }
 
        void Initialize(){ 
            if (port == defaultPort || port == 0) {
                new SmtpPermission(SmtpAccess.Connect).Demand(); 
            }
            else{
                new SmtpPermission(SmtpAccess.ConnectToUnrestrictedPort).Demand();
            } 

            transport = new SmtpTransport(this); 
            if(Logging.On)Logging.Associate(Logging.Web, this, transport); 
            onSendCompletedDelegate = new SendOrPostCallback (SendCompletedWaitCallback);
 
            if (MailConfiguration.Smtp != null)
            {
                if (MailConfiguration.Smtp.Network != null)
                { 
                    if (host == null || host.Length == 0){
                            host = MailConfiguration.Smtp.Network.Host; 
                    } 
                    if (port == 0) {
                        port = MailConfiguration.Smtp.Network.Port; 
                    }

                    transport.Credentials = MailConfiguration.Smtp.Network.Credential;
                } 
                deliveryMethod = MailConfiguration.Smtp.DeliveryMethod;
                if (MailConfiguration.Smtp.SpecifiedPickupDirectory != null) 
                    pickupDirectoryLocation = MailConfiguration.Smtp.SpecifiedPickupDirectory.PickupDirectoryLocation; 
            }
 
            if (host != null && host.Length != 0) {
                host = host.Trim();
            }
 
            if (port == 0) {
                port = defaultPort; 
            } 

            localHostName = IPGlobalProperties.InternalGetIPGlobalProperties().HostName; 
        }


        public string Host { 
            get {
                return host; 
            } 
            set {
 
                if (InCall) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
                }
 
                if (value == null)
                { 
                    throw new ArgumentNullException("value"); 
                }
 
                if (value == String.Empty)
                {
                    throw new ArgumentException(SR.GetString(SR.net_emptystringset), "value");
                } 

                host = value.Trim(); 
            } 
        }
 

        public int Port {
            get {
                return port; 
            }
            set { 
                if (InCall) { 
                    throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
                } 

                if (value <= 0){
                    throw new ArgumentOutOfRangeException("value");
                } 

                if (value != defaultPort) { 
                    new SmtpPermission(SmtpAccess.ConnectToUnrestrictedPort).Demand(); 
                }
 
                port = value;
            }
        }
 

        public bool UseDefaultCredentials { 
            get { 
                return (transport.Credentials is SystemNetworkCredential) ? true : false;
            } 
            set {
                if (InCall) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
                } 

                transport.Credentials = value ? CredentialCache.DefaultNetworkCredentials : null; 
            } 
        }
 

        public ICredentialsByHost Credentials{
           get{
               return transport.Credentials; 
              }
           set{ 
               if (InCall) { 
                   throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
               } 

               transport.Credentials = value;
           }
        } 

 
 
        public int Timeout {
            get { 
                return transport.Timeout;
            }
            set {
                if (InCall) { 
                    throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
                } 
 
                if (value < 0)
                { 
                    throw new ArgumentOutOfRangeException("value");
                }

                transport.Timeout = value; 
            }
        } 
 

        public ServicePoint ServicePoint{ 
            get{
                CheckHostAndPort();
                return ServicePointManager.FindServicePoint(host,port);
            } 
        }
 
        public SmtpDeliveryMethod DeliveryMethod { 
            get {
                return deliveryMethod; 
            }
            set {
                deliveryMethod = value;
            } 
        }
 
 
        public string PickupDirectoryLocation {
            [ResourceExposure(ResourceScope.Machine)] 
            [ResourceConsumption(ResourceScope.Machine)]
            get {
                return pickupDirectoryLocation;
            } 
            [ResourceExposure(ResourceScope.Machine)]
            [ResourceConsumption(ResourceScope.Machine)] 
            set { 
                pickupDirectoryLocation = value;
            } 
        }
        /// <summary>
        ///    <para>Set to true if we need SSL</para>
        /// </summary> 
        public bool EnableSsl {
            get { 
                return transport.EnableSsl; 
            }
            set { 
                transport.EnableSsl = value;
            }
        }
 
        /// <summary>
        /// Certificates used by the client for establishing an SSL connection with the server. 
        /// </summary> 
        public X509CertificateCollection ClientCertificates {
            get { 
                return transport.ClientCertificates;
            }
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        internal MailWriter GetFileMailWriter(string pickupDirectory) 
        {
            if(Logging.On) Logging.PrintInfo(Logging.Web, "SmtpClient.Send() pickupDirectory=" + pickupDirectory); 
            if (!Path.IsPathRooted(pickupDirectory))
                throw new SmtpException(SR.GetString(SR.SmtpNeedAbsolutePickupDirectory));
            string filename;
            string pathAndFilename; 
            while (true) {
                filename = Guid.NewGuid().ToString() + ".eml"; 
                pathAndFilename = Path.Combine(pickupDirectory, filename); 
                if (!File.Exists(pathAndFilename))
                    break; 
            }

            FileStream fileStream = new FileStream(pathAndFilename, FileMode.CreateNew);
            return new MailWriter(fileStream); 
        }
 
        protected void OnSendCompleted(AsyncCompletedEventArgs e) 
        {
            if (SendCompleted != null){ 
                SendCompleted (this,e);
            }
        }
 
        void SendCompletedWaitCallback (object operationState){
            OnSendCompleted((AsyncCompletedEventArgs)operationState); 
        } 

 
        public void Send(string from, string recipients, string subject, string body) {
            //validation happends in MailMessage constructor
            MailMessage mailMessage = new MailMessage(from, recipients, subject, body);
            Send(mailMessage); 
        }
 
 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
        public void Send(MailMessage message) {
            if(Logging.On) Logging.Enter(Logging.Web, this, "Send", message);
            try {
                if(Logging.On) Logging.PrintInfo(Logging.Web, this, "Send", "DeliveryMethod=" + DeliveryMethod.ToString()); 
                if(Logging.On) Logging.Associate(Logging.Web, this, message);
                SmtpFailedRecipientException recipientException = null; 
 
                if (InCall) {
                    throw new InvalidOperationException(SR.GetString(SR.net_inasync)); 
                }

                if (message == null) {
                    throw new ArgumentNullException("message"); 
                }
 
                if (DeliveryMethod == SmtpDeliveryMethod.Network) 
                    CheckHostAndPort();
 
                MailAddressCollection recipients = new MailAddressCollection();

                if (message.From == null) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpFromRequired)); 
                }
 
                if (message.To != null) { 
                    foreach(MailAddress address in message.To) {
                        recipients.Add(address); 
                    }
                }
                if (message.Bcc != null) {
                    foreach(MailAddress address in message.Bcc) { 
                        recipients.Add(address);
                    } 
                } 
                if (message.CC != null) {
                    foreach(MailAddress address in message.CC) { 
                        recipients.Add(address);
                    }
                }
 
                if (recipients.Count == 0) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpRecipientRequired)); 
                } 

                transport.IdentityRequired = false;  // everything completes on the same thread. 

                try {
                    InCall = true;
                    timedOut = false; 
                    timer = new Timer(new TimerCallback(this.TimeOutCallback), null, Timeout, Timeout);
 
                    MailWriter writer; 
                    switch (DeliveryMethod) {
                        case SmtpDeliveryMethod.SpecifiedPickupDirectory: 
                            if (EnableSsl)
                                throw new SmtpException(SR.GetString(SR.SmtpPickupDirectoryDoesnotSupportSsl));
                            writer = GetFileMailWriter(PickupDirectoryLocation);
                            break; 
#if !FEATURE_PAL
                        case SmtpDeliveryMethod.PickupDirectoryFromIis: 
                            if (EnableSsl) 
                                throw new SmtpException(SR.GetString(SR.SmtpPickupDirectoryDoesnotSupportSsl));
                            writer = GetFileMailWriter(IisPickupDirectory.GetPickupDirectory()); 
                            break;
#endif // !FEATURE_PAL
                        case SmtpDeliveryMethod.Network:
                        default: 
                            GetConnection();
                            writer = transport.SendMail(message.Sender != null ? message.Sender : message.From, recipients, message.BuildDeliveryStatusNotificationString(), out recipientException); 
                            break; 
                    }
                    this.message = message; 
                    message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network);
                    writer.Close();
                    transport.ReleaseConnection();
 
                    //throw if we couldn't send to any of the recipients
                    if (DeliveryMethod == SmtpDeliveryMethod.Network) 
                        if (recipientException != null) 
                            throw recipientException;
                } 
                catch (Exception e) {

                    if(Logging.On)Logging.Exception(Logging.Web, this, "Send", e);
 

                    if (e is SmtpFailedRecipientException && !((SmtpFailedRecipientException)e).fatal){ 
                        throw; 
                    }
 

                    Abort();
                    if (timedOut) {
                        throw new SmtpException(SR.GetString(SR.net_timeout)); 
                    }
 
                    if (e is SecurityException || 
                        e is AuthenticationException ||
                        e is SmtpException) 
                    {
                        throw;
                    }
 
                    throw new SmtpException(SR.GetString(SR.SmtpSendMailFailure),e);
                } 
                finally { 
                    InCall = false;
                    if(timer != null){ 
                        timer.Dispose();
                    }
                }
            } finally { 
                if(Logging.On)Logging.Exit(Logging.Web, this, "Send", null);
            } 
        } 

 

        [HostProtection(ExternalThreading=true)]
        public void SendAsync(string from, string recipients,string subject, string body, object userToken) {
            SendAsync(new MailMessage(from,recipients,subject,body),userToken); 
        }
 
 
        [HostProtection(ExternalThreading=true)]
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public void SendAsync(MailMessage message, object userToken) {
            if(Logging.On) Logging.Enter(Logging.Web, this, "SendAsync", "DeliveryMethod=" + DeliveryMethod.ToString());
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsync Transport#"+ValidationHelper.HashString(transport)); 
            try {
                if (InCall) { 
                    throw new InvalidOperationException(SR.GetString(SR.net_inasync)); 
                }
 
                if (message == null) {
                    throw new ArgumentNullException("message");
                }
 
                if (DeliveryMethod == SmtpDeliveryMethod.Network)
                    CheckHostAndPort(); 
 
                recipients = new MailAddressCollection();
 
                if (message.From == null) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpFromRequired));
                }
 
                if (message.To != null) {
                    foreach(MailAddress address in message.To) { 
                        recipients.Add(address); 
                    }
                } 
                if (message.Bcc != null) {
                    foreach(MailAddress address in message.Bcc) {
                        recipients.Add(address);
                    } 
                }
                if (message.CC != null) { 
                    foreach(MailAddress address in message.CC) { 
                        recipients.Add(address);
                    } 
                }

                if (recipients.Count == 0) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpRecipientRequired)); 
                }
 
                try { 
                    InCall = true;
                    cancelled = false; 
                    this.message = message;

#if !FEATURE_PAL
                    CredentialCache cache; 
                    // Skip token capturing if no credentials are used or they don't include a default one.
                    // Also do capture the token if ICredential is not of CredentialCache type so we don't know what the exact credential response will be. 
                    transport.IdentityRequired = Credentials != null && ComNetOS.IsWinNt && (Credentials is SystemNetworkCredential || (cache = Credentials as CredentialCache) == null || cache.IsDefaultInCache); 
#endif // !FEATURE_PAL
 
                    asyncOp = AsyncOperationManager.CreateOperation(userToken);
                    switch (DeliveryMethod) {
                        case SmtpDeliveryMethod.SpecifiedPickupDirectory:
                        { 
                            if (EnableSsl)
                                throw new SmtpException(SR.GetString(SR.SmtpPickupDirectoryDoesnotSupportSsl)); 
                            writer = GetFileMailWriter(PickupDirectoryLocation); 
                            message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network);
 
                            if (writer != null)
                                writer.Close();

                            transport.ReleaseConnection(); 
                            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(null, false, asyncOp.UserSuppliedState);
                            InCall = false; 
                            asyncOp.PostOperationCompleted(onSendCompletedDelegate, eventArgs); 
                            break;
                        } 
#if !FEATURE_PAL
                        case SmtpDeliveryMethod.PickupDirectoryFromIis:
                        {
                            if (EnableSsl) 
                                throw new SmtpException(SR.GetString(SR.SmtpPickupDirectoryDoesnotSupportSsl));
                            writer = GetFileMailWriter(IisPickupDirectory.GetPickupDirectory()); 
                            message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network); 

                            if (writer != null) 
                                writer.Close();

                            transport.ReleaseConnection();
                            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(null, false, asyncOp.UserSuppliedState); 
                            InCall = false;
                            asyncOp.PostOperationCompleted(onSendCompletedDelegate, eventArgs); 
                            break; 
                        }
#endif // !FEATURE_PAL 

                        case SmtpDeliveryMethod.Network:
                        default:
                            operationCompletedResult = new ContextAwareResult(transport.IdentityRequired, true, null, this, _ContextSafeCompleteCallback); 
                            lock (operationCompletedResult.StartPostingAsyncOp())
                            { 
                                GlobalLog.Print("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsync calling BeginConnect.  Transport#"+ValidationHelper.HashString(transport)); 
                                transport.BeginGetConnection(host, port, operationCompletedResult, ConnectCallback, operationCompletedResult);
                                operationCompletedResult.FinishPostingAsyncOp(); 
                            }
                            break;
                    }
 
                }
                catch (Exception e) { 
                    InCall = false; 

                    if(Logging.On)Logging.Exception(Logging.Web, this, "Send", e); 

                    if (e is SmtpFailedRecipientException && !((SmtpFailedRecipientException)e).fatal){
                        throw;
                    } 

                    Abort(); 
                    if (timedOut) { 
                        throw new SmtpException(SR.GetString(SR.net_timeout));
                    } 

                    if (e is SecurityException ||
                        e is AuthenticationException ||
                        e is SmtpException) 
                    {
                        throw; 
                    } 

                    throw new SmtpException(SR.GetString(SR.SmtpSendMailFailure),e); 
                }
            } finally {
                if(Logging.On)Logging.Exit(Logging.Web, this, "SendAsync", null);
                GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsync"); 
            }
        } 
 

        public void SendAsyncCancel() { 
            if(Logging.On)Logging.Enter(Logging.Web, this, "SendAsyncCancel", null);
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsyncCancel");
            try {
                if (!InCall || cancelled) { 
                    return;
                } 
 
                cancelled = true;
                Abort(); 
            } finally {
                if(Logging.On)Logging.Exit(Logging.Web, this, "SendAsyncCancel", null);
                GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsyncCancel");
            } 
        }
 
 
//*********************************
// private methods 
//********************************
        internal bool InCall {
            get {
                return inCall; 
            }
            set { 
                inCall = value; 
            }
        } 

        internal static MailSettingsSectionGroupInternal MailConfiguration {
            get{
                if (mailConfiguration == null) { 
                    mailConfiguration = MailSettingsSectionGroupInternal.GetSection();
                } 
                return mailConfiguration; 
            }
        } 


        void CheckHostAndPort(){
 
            if (host == null || host.Length == 0){
                throw new InvalidOperationException(SR.GetString(SR.UnspecifiedHost)); 
            } 
            if (port == 0) {
                throw new InvalidOperationException(SR.GetString(SR.InvalidPort)); 
            }
        }

 
        void TimeOutCallback(object state) {
            if (!timedOut) { 
                timedOut = true; 
                Abort();
            } 
        }


        void Complete(Exception exception, IAsyncResult result) { 
            ContextAwareResult operationCompletedResult = (ContextAwareResult) result.AsyncState;
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::Complete"); 
            try { 
                if (cancelled) {
                    //any exceptions were probably caused by cancellation, clear it. 
                    exception = null;
                    Abort();
                }
                // A failed recipient exception is benign 
                else if (exception != null && (!(exception is SmtpFailedRecipientException) || ((SmtpFailedRecipientException)exception).fatal))
                { 
                    GlobalLog.Print("SmtpClient#" + ValidationHelper.HashString(this) + "::Complete Exception: "+exception.ToString()); 
                    Abort();
 
                    if (!(exception is SmtpException)){
                        exception = new SmtpException(SR.GetString(SR.SmtpSendMailFailure),exception);
                    }
                } 
                else{
                    if (writer != null){ 
                        writer.Close(); 
                    }
                    transport.ReleaseConnection(); 
                }
            }
            finally {
                operationCompletedResult.InvokeCallback(exception); 
            }
            GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::Complete"); 
        } 

        static void ContextSafeCompleteCallback(IAsyncResult ar) 
        {
            ContextAwareResult result = (ContextAwareResult) ar;
            SmtpClient client = (SmtpClient)ar.AsyncState;
            Exception exception = result.Result as Exception; 
            AsyncOperation asyncOp = client.asyncOp;
            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(exception, client.cancelled, asyncOp.UserSuppliedState); 
            client.InCall = false; 
            asyncOp.PostOperationCompleted(client.onSendCompletedDelegate, eventArgs);
        } 

      /*
        void DisconnectCallback(IAsyncResult result) {
            SmtpClient client = (SmtpClient)result.AsyncState; 

            try { 
                client.transport.EndDisconnect(result); 
            }
            catch (Exception e) { 
                Complete(e);
                return;
            }
            Complete(null); 
        }
              */ 
 
        void SendMessageCallback(IAsyncResult result) {
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMessageCallback"); 
            try {
                message.EndSend(result);
                Complete(null, result);
            } 
            catch (Exception e) {
                Complete(e, result); 
            } 
            GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMessageCallback");
        } 


        void SendMailCallback(IAsyncResult result) {
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMailCallback"); 
            try {
                writer = transport.EndSendMail(result); 
            } 
            catch (Exception e)
            { 
                // Note the difference between the singular and plural FailedRecipient exceptions.
                // Only fail immediately if we couldn't send to any recipients.
                if (!(e is SmtpFailedRecipientException) || ((SmtpFailedRecipientException)e).fatal)
                { 
                    Complete(e, result);
                    GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMailCallback"); 
                    return; 
                }
            } 
            catch
            {
                // Note the difference between the singular and plural FailedRecipient exceptions.
                // Only fail immediately if we couldn't send to any recipients. 
                Complete(new Exception(SR.GetString(SR.net_nonClsCompliantException)), result);
                GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMailCallback"); 
                return; 
            }
            try { 
                if (cancelled) {
                    Complete(null, result);
                }
                else{ 
                    message.BeginSend(writer, DeliveryMethod != SmtpDeliveryMethod.Network, new AsyncCallback(SendMessageCallback), result.AsyncState);
                } 
            } 
            catch (Exception e) {
                Complete(e, result); 
            }
            GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMailCallback");
        }
 

        void ConnectCallback(IAsyncResult result) { 
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::ConnectCallback"); 
            try {
                transport.EndGetConnection(result); 
                if (cancelled) {
                    Complete(null,result);
                }
                else{ 
                    transport.BeginSendMail(message.Sender != null ? message.Sender : message.From, recipients, message.BuildDeliveryStatusNotificationString(), new AsyncCallback(SendMailCallback), result.AsyncState);
                } 
            } 
            catch (Exception e) {
                Complete(e, result); 
            }
            GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::ConnectCallback");
        }
 

        void GetConnection() { 
            if (!transport.IsConnected) { 
                transport.GetConnection(host, port);
            } 
        }


        void Abort() { 
            try{
                transport.Abort(); 
            } 
            catch{
            } 
        }
    }
}
namespace System.Net.Mail 
{

    using System;
    using System.IO; 
    using System.Net;
    using System.Collections; 
    using System.ComponentModel; 
    using System.Configuration;
    using System.Net.Configuration; 
    using System.Threading;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Authentication; 
    using System.Security.Cryptography.X509Certificates;
    using System.Net.NetworkInformation; 
    using System.Runtime.Versioning; 

    public delegate void SendCompletedEventHandler(object sender, AsyncCompletedEventArgs e); 

    public enum SmtpDeliveryMethod {
        Network,
        SpecifiedPickupDirectory, 
#if !FEATURE_PAL
        PickupDirectoryFromIis 
#endif 
    }
 

    public class SmtpClient {

        string host; 
        int port;
        bool inCall; 
        bool cancelled; 
        bool timedOut;
        SmtpDeliveryMethod deliveryMethod = SmtpDeliveryMethod.Network; 
        string pickupDirectoryLocation = null;
        SmtpTransport transport;
        MailMessage message; //required to prevent premature finalization
        MailWriter writer; 
        MailAddressCollection recipients;
        SendOrPostCallback onSendCompletedDelegate; 
        Timer timer; 
        static MailSettingsSectionGroupInternal mailConfiguration;
        ContextAwareResult operationCompletedResult = null; 
        AsyncOperation asyncOp = null;
        static AsyncCallback _ContextSafeCompleteCallback = new AsyncCallback(ContextSafeCompleteCallback);
        static int defaultPort = 25;
        internal string localHostName; 

        public event SendCompletedEventHandler SendCompleted; 
 
        public SmtpClient(){
            if(Logging.On)Logging.Enter(Logging.Web, "SmtpClient", ".ctor", ""); 
            try {
                Initialize();
            } finally {
                if(Logging.On)Logging.Exit(Logging.Web, "SmtpClient", ".ctor", this); 
            }
        } 
 
        public SmtpClient(string host){
            if(Logging.On)Logging.Enter(Logging.Web, "SmtpClient", ".ctor", "host="+host); 
            try {
                this.host = host;
                Initialize();
            } finally { 
                if(Logging.On)Logging.Exit(Logging.Web, "SmtpClient", ".ctor", this);
            } 
        } 

        //?? should port throw or just default on 0? 
        public SmtpClient(string host, int port) {
            if(Logging.On)Logging.Enter(Logging.Web, "SmtpClient", ".ctor", "host="+host+", port="+port);
            try {
                if (port < 0){ 
                   throw new ArgumentOutOfRangeException("port");
                } 
 
                this.host = host;
                this.port = port; 
                Initialize();
            } finally {
                if(Logging.On)Logging.Exit(Logging.Web, "SmtpClient", ".ctor", this);
            } 
        }
 
        void Initialize(){ 
            if (port == defaultPort || port == 0) {
                new SmtpPermission(SmtpAccess.Connect).Demand(); 
            }
            else{
                new SmtpPermission(SmtpAccess.ConnectToUnrestrictedPort).Demand();
            } 

            transport = new SmtpTransport(this); 
            if(Logging.On)Logging.Associate(Logging.Web, this, transport); 
            onSendCompletedDelegate = new SendOrPostCallback (SendCompletedWaitCallback);
 
            if (MailConfiguration.Smtp != null)
            {
                if (MailConfiguration.Smtp.Network != null)
                { 
                    if (host == null || host.Length == 0){
                            host = MailConfiguration.Smtp.Network.Host; 
                    } 
                    if (port == 0) {
                        port = MailConfiguration.Smtp.Network.Port; 
                    }

                    transport.Credentials = MailConfiguration.Smtp.Network.Credential;
                } 
                deliveryMethod = MailConfiguration.Smtp.DeliveryMethod;
                if (MailConfiguration.Smtp.SpecifiedPickupDirectory != null) 
                    pickupDirectoryLocation = MailConfiguration.Smtp.SpecifiedPickupDirectory.PickupDirectoryLocation; 
            }
 
            if (host != null && host.Length != 0) {
                host = host.Trim();
            }
 
            if (port == 0) {
                port = defaultPort; 
            } 

            localHostName = IPGlobalProperties.InternalGetIPGlobalProperties().HostName; 
        }


        public string Host { 
            get {
                return host; 
            } 
            set {
 
                if (InCall) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
                }
 
                if (value == null)
                { 
                    throw new ArgumentNullException("value"); 
                }
 
                if (value == String.Empty)
                {
                    throw new ArgumentException(SR.GetString(SR.net_emptystringset), "value");
                } 

                host = value.Trim(); 
            } 
        }
 

        public int Port {
            get {
                return port; 
            }
            set { 
                if (InCall) { 
                    throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
                } 

                if (value <= 0){
                    throw new ArgumentOutOfRangeException("value");
                } 

                if (value != defaultPort) { 
                    new SmtpPermission(SmtpAccess.ConnectToUnrestrictedPort).Demand(); 
                }
 
                port = value;
            }
        }
 

        public bool UseDefaultCredentials { 
            get { 
                return (transport.Credentials is SystemNetworkCredential) ? true : false;
            } 
            set {
                if (InCall) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
                } 

                transport.Credentials = value ? CredentialCache.DefaultNetworkCredentials : null; 
            } 
        }
 

        public ICredentialsByHost Credentials{
           get{
               return transport.Credentials; 
              }
           set{ 
               if (InCall) { 
                   throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
               } 

               transport.Credentials = value;
           }
        } 

 
 
        public int Timeout {
            get { 
                return transport.Timeout;
            }
            set {
                if (InCall) { 
                    throw new InvalidOperationException(SR.GetString(SR.SmtpInvalidOperationDuringSend));
                } 
 
                if (value < 0)
                { 
                    throw new ArgumentOutOfRangeException("value");
                }

                transport.Timeout = value; 
            }
        } 
 

        public ServicePoint ServicePoint{ 
            get{
                CheckHostAndPort();
                return ServicePointManager.FindServicePoint(host,port);
            } 
        }
 
        public SmtpDeliveryMethod DeliveryMethod { 
            get {
                return deliveryMethod; 
            }
            set {
                deliveryMethod = value;
            } 
        }
 
 
        public string PickupDirectoryLocation {
            [ResourceExposure(ResourceScope.Machine)] 
            [ResourceConsumption(ResourceScope.Machine)]
            get {
                return pickupDirectoryLocation;
            } 
            [ResourceExposure(ResourceScope.Machine)]
            [ResourceConsumption(ResourceScope.Machine)] 
            set { 
                pickupDirectoryLocation = value;
            } 
        }
        /// <summary>
        ///    <para>Set to true if we need SSL</para>
        /// </summary> 
        public bool EnableSsl {
            get { 
                return transport.EnableSsl; 
            }
            set { 
                transport.EnableSsl = value;
            }
        }
 
        /// <summary>
        /// Certificates used by the client for establishing an SSL connection with the server. 
        /// </summary> 
        public X509CertificateCollection ClientCertificates {
            get { 
                return transport.ClientCertificates;
            }
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        internal MailWriter GetFileMailWriter(string pickupDirectory) 
        {
            if(Logging.On) Logging.PrintInfo(Logging.Web, "SmtpClient.Send() pickupDirectory=" + pickupDirectory); 
            if (!Path.IsPathRooted(pickupDirectory))
                throw new SmtpException(SR.GetString(SR.SmtpNeedAbsolutePickupDirectory));
            string filename;
            string pathAndFilename; 
            while (true) {
                filename = Guid.NewGuid().ToString() + ".eml"; 
                pathAndFilename = Path.Combine(pickupDirectory, filename); 
                if (!File.Exists(pathAndFilename))
                    break; 
            }

            FileStream fileStream = new FileStream(pathAndFilename, FileMode.CreateNew);
            return new MailWriter(fileStream); 
        }
 
        protected void OnSendCompleted(AsyncCompletedEventArgs e) 
        {
            if (SendCompleted != null){ 
                SendCompleted (this,e);
            }
        }
 
        void SendCompletedWaitCallback (object operationState){
            OnSendCompleted((AsyncCompletedEventArgs)operationState); 
        } 

 
        public void Send(string from, string recipients, string subject, string body) {
            //validation happends in MailMessage constructor
            MailMessage mailMessage = new MailMessage(from, recipients, subject, body);
            Send(mailMessage); 
        }
 
 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
        public void Send(MailMessage message) {
            if(Logging.On) Logging.Enter(Logging.Web, this, "Send", message);
            try {
                if(Logging.On) Logging.PrintInfo(Logging.Web, this, "Send", "DeliveryMethod=" + DeliveryMethod.ToString()); 
                if(Logging.On) Logging.Associate(Logging.Web, this, message);
                SmtpFailedRecipientException recipientException = null; 
 
                if (InCall) {
                    throw new InvalidOperationException(SR.GetString(SR.net_inasync)); 
                }

                if (message == null) {
                    throw new ArgumentNullException("message"); 
                }
 
                if (DeliveryMethod == SmtpDeliveryMethod.Network) 
                    CheckHostAndPort();
 
                MailAddressCollection recipients = new MailAddressCollection();

                if (message.From == null) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpFromRequired)); 
                }
 
                if (message.To != null) { 
                    foreach(MailAddress address in message.To) {
                        recipients.Add(address); 
                    }
                }
                if (message.Bcc != null) {
                    foreach(MailAddress address in message.Bcc) { 
                        recipients.Add(address);
                    } 
                } 
                if (message.CC != null) {
                    foreach(MailAddress address in message.CC) { 
                        recipients.Add(address);
                    }
                }
 
                if (recipients.Count == 0) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpRecipientRequired)); 
                } 

                transport.IdentityRequired = false;  // everything completes on the same thread. 

                try {
                    InCall = true;
                    timedOut = false; 
                    timer = new Timer(new TimerCallback(this.TimeOutCallback), null, Timeout, Timeout);
 
                    MailWriter writer; 
                    switch (DeliveryMethod) {
                        case SmtpDeliveryMethod.SpecifiedPickupDirectory: 
                            if (EnableSsl)
                                throw new SmtpException(SR.GetString(SR.SmtpPickupDirectoryDoesnotSupportSsl));
                            writer = GetFileMailWriter(PickupDirectoryLocation);
                            break; 
#if !FEATURE_PAL
                        case SmtpDeliveryMethod.PickupDirectoryFromIis: 
                            if (EnableSsl) 
                                throw new SmtpException(SR.GetString(SR.SmtpPickupDirectoryDoesnotSupportSsl));
                            writer = GetFileMailWriter(IisPickupDirectory.GetPickupDirectory()); 
                            break;
#endif // !FEATURE_PAL
                        case SmtpDeliveryMethod.Network:
                        default: 
                            GetConnection();
                            writer = transport.SendMail(message.Sender != null ? message.Sender : message.From, recipients, message.BuildDeliveryStatusNotificationString(), out recipientException); 
                            break; 
                    }
                    this.message = message; 
                    message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network);
                    writer.Close();
                    transport.ReleaseConnection();
 
                    //throw if we couldn't send to any of the recipients
                    if (DeliveryMethod == SmtpDeliveryMethod.Network) 
                        if (recipientException != null) 
                            throw recipientException;
                } 
                catch (Exception e) {

                    if(Logging.On)Logging.Exception(Logging.Web, this, "Send", e);
 

                    if (e is SmtpFailedRecipientException && !((SmtpFailedRecipientException)e).fatal){ 
                        throw; 
                    }
 

                    Abort();
                    if (timedOut) {
                        throw new SmtpException(SR.GetString(SR.net_timeout)); 
                    }
 
                    if (e is SecurityException || 
                        e is AuthenticationException ||
                        e is SmtpException) 
                    {
                        throw;
                    }
 
                    throw new SmtpException(SR.GetString(SR.SmtpSendMailFailure),e);
                } 
                finally { 
                    InCall = false;
                    if(timer != null){ 
                        timer.Dispose();
                    }
                }
            } finally { 
                if(Logging.On)Logging.Exit(Logging.Web, this, "Send", null);
            } 
        } 

 

        [HostProtection(ExternalThreading=true)]
        public void SendAsync(string from, string recipients,string subject, string body, object userToken) {
            SendAsync(new MailMessage(from,recipients,subject,body),userToken); 
        }
 
 
        [HostProtection(ExternalThreading=true)]
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public void SendAsync(MailMessage message, object userToken) {
            if(Logging.On) Logging.Enter(Logging.Web, this, "SendAsync", "DeliveryMethod=" + DeliveryMethod.ToString());
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsync Transport#"+ValidationHelper.HashString(transport)); 
            try {
                if (InCall) { 
                    throw new InvalidOperationException(SR.GetString(SR.net_inasync)); 
                }
 
                if (message == null) {
                    throw new ArgumentNullException("message");
                }
 
                if (DeliveryMethod == SmtpDeliveryMethod.Network)
                    CheckHostAndPort(); 
 
                recipients = new MailAddressCollection();
 
                if (message.From == null) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpFromRequired));
                }
 
                if (message.To != null) {
                    foreach(MailAddress address in message.To) { 
                        recipients.Add(address); 
                    }
                } 
                if (message.Bcc != null) {
                    foreach(MailAddress address in message.Bcc) {
                        recipients.Add(address);
                    } 
                }
                if (message.CC != null) { 
                    foreach(MailAddress address in message.CC) { 
                        recipients.Add(address);
                    } 
                }

                if (recipients.Count == 0) {
                    throw new InvalidOperationException(SR.GetString(SR.SmtpRecipientRequired)); 
                }
 
                try { 
                    InCall = true;
                    cancelled = false; 
                    this.message = message;

#if !FEATURE_PAL
                    CredentialCache cache; 
                    // Skip token capturing if no credentials are used or they don't include a default one.
                    // Also do capture the token if ICredential is not of CredentialCache type so we don't know what the exact credential response will be. 
                    transport.IdentityRequired = Credentials != null && ComNetOS.IsWinNt && (Credentials is SystemNetworkCredential || (cache = Credentials as CredentialCache) == null || cache.IsDefaultInCache); 
#endif // !FEATURE_PAL
 
                    asyncOp = AsyncOperationManager.CreateOperation(userToken);
                    switch (DeliveryMethod) {
                        case SmtpDeliveryMethod.SpecifiedPickupDirectory:
                        { 
                            if (EnableSsl)
                                throw new SmtpException(SR.GetString(SR.SmtpPickupDirectoryDoesnotSupportSsl)); 
                            writer = GetFileMailWriter(PickupDirectoryLocation); 
                            message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network);
 
                            if (writer != null)
                                writer.Close();

                            transport.ReleaseConnection(); 
                            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(null, false, asyncOp.UserSuppliedState);
                            InCall = false; 
                            asyncOp.PostOperationCompleted(onSendCompletedDelegate, eventArgs); 
                            break;
                        } 
#if !FEATURE_PAL
                        case SmtpDeliveryMethod.PickupDirectoryFromIis:
                        {
                            if (EnableSsl) 
                                throw new SmtpException(SR.GetString(SR.SmtpPickupDirectoryDoesnotSupportSsl));
                            writer = GetFileMailWriter(IisPickupDirectory.GetPickupDirectory()); 
                            message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network); 

                            if (writer != null) 
                                writer.Close();

                            transport.ReleaseConnection();
                            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(null, false, asyncOp.UserSuppliedState); 
                            InCall = false;
                            asyncOp.PostOperationCompleted(onSendCompletedDelegate, eventArgs); 
                            break; 
                        }
#endif // !FEATURE_PAL 

                        case SmtpDeliveryMethod.Network:
                        default:
                            operationCompletedResult = new ContextAwareResult(transport.IdentityRequired, true, null, this, _ContextSafeCompleteCallback); 
                            lock (operationCompletedResult.StartPostingAsyncOp())
                            { 
                                GlobalLog.Print("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsync calling BeginConnect.  Transport#"+ValidationHelper.HashString(transport)); 
                                transport.BeginGetConnection(host, port, operationCompletedResult, ConnectCallback, operationCompletedResult);
                                operationCompletedResult.FinishPostingAsyncOp(); 
                            }
                            break;
                    }
 
                }
                catch (Exception e) { 
                    InCall = false; 

                    if(Logging.On)Logging.Exception(Logging.Web, this, "Send", e); 

                    if (e is SmtpFailedRecipientException && !((SmtpFailedRecipientException)e).fatal){
                        throw;
                    } 

                    Abort(); 
                    if (timedOut) { 
                        throw new SmtpException(SR.GetString(SR.net_timeout));
                    } 

                    if (e is SecurityException ||
                        e is AuthenticationException ||
                        e is SmtpException) 
                    {
                        throw; 
                    } 

                    throw new SmtpException(SR.GetString(SR.SmtpSendMailFailure),e); 
                }
            } finally {
                if(Logging.On)Logging.Exit(Logging.Web, this, "SendAsync", null);
                GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsync"); 
            }
        } 
 

        public void SendAsyncCancel() { 
            if(Logging.On)Logging.Enter(Logging.Web, this, "SendAsyncCancel", null);
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsyncCancel");
            try {
                if (!InCall || cancelled) { 
                    return;
                } 
 
                cancelled = true;
                Abort(); 
            } finally {
                if(Logging.On)Logging.Exit(Logging.Web, this, "SendAsyncCancel", null);
                GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendAsyncCancel");
            } 
        }
 
 
//*********************************
// private methods 
//********************************
        internal bool InCall {
            get {
                return inCall; 
            }
            set { 
                inCall = value; 
            }
        } 

        internal static MailSettingsSectionGroupInternal MailConfiguration {
            get{
                if (mailConfiguration == null) { 
                    mailConfiguration = MailSettingsSectionGroupInternal.GetSection();
                } 
                return mailConfiguration; 
            }
        } 


        void CheckHostAndPort(){
 
            if (host == null || host.Length == 0){
                throw new InvalidOperationException(SR.GetString(SR.UnspecifiedHost)); 
            } 
            if (port == 0) {
                throw new InvalidOperationException(SR.GetString(SR.InvalidPort)); 
            }
        }

 
        void TimeOutCallback(object state) {
            if (!timedOut) { 
                timedOut = true; 
                Abort();
            } 
        }


        void Complete(Exception exception, IAsyncResult result) { 
            ContextAwareResult operationCompletedResult = (ContextAwareResult) result.AsyncState;
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::Complete"); 
            try { 
                if (cancelled) {
                    //any exceptions were probably caused by cancellation, clear it. 
                    exception = null;
                    Abort();
                }
                // A failed recipient exception is benign 
                else if (exception != null && (!(exception is SmtpFailedRecipientException) || ((SmtpFailedRecipientException)exception).fatal))
                { 
                    GlobalLog.Print("SmtpClient#" + ValidationHelper.HashString(this) + "::Complete Exception: "+exception.ToString()); 
                    Abort();
 
                    if (!(exception is SmtpException)){
                        exception = new SmtpException(SR.GetString(SR.SmtpSendMailFailure),exception);
                    }
                } 
                else{
                    if (writer != null){ 
                        writer.Close(); 
                    }
                    transport.ReleaseConnection(); 
                }
            }
            finally {
                operationCompletedResult.InvokeCallback(exception); 
            }
            GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::Complete"); 
        } 

        static void ContextSafeCompleteCallback(IAsyncResult ar) 
        {
            ContextAwareResult result = (ContextAwareResult) ar;
            SmtpClient client = (SmtpClient)ar.AsyncState;
            Exception exception = result.Result as Exception; 
            AsyncOperation asyncOp = client.asyncOp;
            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(exception, client.cancelled, asyncOp.UserSuppliedState); 
            client.InCall = false; 
            asyncOp.PostOperationCompleted(client.onSendCompletedDelegate, eventArgs);
        } 

      /*
        void DisconnectCallback(IAsyncResult result) {
            SmtpClient client = (SmtpClient)result.AsyncState; 

            try { 
                client.transport.EndDisconnect(result); 
            }
            catch (Exception e) { 
                Complete(e);
                return;
            }
            Complete(null); 
        }
              */ 
 
        void SendMessageCallback(IAsyncResult result) {
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMessageCallback"); 
            try {
                message.EndSend(result);
                Complete(null, result);
            } 
            catch (Exception e) {
                Complete(e, result); 
            } 
            GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMessageCallback");
        } 


        void SendMailCallback(IAsyncResult result) {
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMailCallback"); 
            try {
                writer = transport.EndSendMail(result); 
            } 
            catch (Exception e)
            { 
                // Note the difference between the singular and plural FailedRecipient exceptions.
                // Only fail immediately if we couldn't send to any recipients.
                if (!(e is SmtpFailedRecipientException) || ((SmtpFailedRecipientException)e).fatal)
                { 
                    Complete(e, result);
                    GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMailCallback"); 
                    return; 
                }
            } 
            catch
            {
                // Note the difference between the singular and plural FailedRecipient exceptions.
                // Only fail immediately if we couldn't send to any recipients. 
                Complete(new Exception(SR.GetString(SR.net_nonClsCompliantException)), result);
                GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMailCallback"); 
                return; 
            }
            try { 
                if (cancelled) {
                    Complete(null, result);
                }
                else{ 
                    message.BeginSend(writer, DeliveryMethod != SmtpDeliveryMethod.Network, new AsyncCallback(SendMessageCallback), result.AsyncState);
                } 
            } 
            catch (Exception e) {
                Complete(e, result); 
            }
            GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::SendMailCallback");
        }
 

        void ConnectCallback(IAsyncResult result) { 
            GlobalLog.Enter("SmtpClient#" + ValidationHelper.HashString(this) + "::ConnectCallback"); 
            try {
                transport.EndGetConnection(result); 
                if (cancelled) {
                    Complete(null,result);
                }
                else{ 
                    transport.BeginSendMail(message.Sender != null ? message.Sender : message.From, recipients, message.BuildDeliveryStatusNotificationString(), new AsyncCallback(SendMailCallback), result.AsyncState);
                } 
            } 
            catch (Exception e) {
                Complete(e, result); 
            }
            GlobalLog.Leave("SmtpClient#" + ValidationHelper.HashString(this) + "::ConnectCallback");
        }
 

        void GetConnection() { 
            if (!transport.IsConnected) { 
                transport.GetConnection(host, port);
            } 
        }


        void Abort() { 
            try{
                transport.Abort(); 
            } 
            catch{
            } 
        }
    }
}
