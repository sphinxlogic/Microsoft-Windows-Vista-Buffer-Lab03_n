using System; 
using System.Collections.Specialized;
using System.Net.Mime;
using System.Text;
using System.IO; 

namespace System.Net.Mail 
{ 
    /// <summary>
    /// Summary description for Message. 
    /// </summary>

    public enum MailPriority {
        Normal = 0, 
        Low = 1,
        High = 2 
    } 

 
    internal class Message
    {

        MailAddress from; 
        MailAddress sender;
        MailAddress replyTo; 
        MailAddressCollection to; 
        MailAddressCollection cc;
        MailAddressCollection bcc; 
        MimeBasePart content;
        HeaderCollection headers;
        HeaderCollection envelopeHeaders;
        string subject; 
        Encoding subjectEncoding;
        MailPriority priority = (MailPriority)(-1); 
 

        internal Message() { 
        }

        internal Message(string from, string to):this() {
            if (from == null) 
                throw new ArgumentNullException("from");
 
            if (to == null) 
                throw new ArgumentNullException("to");
 
            if (from == String.Empty)
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall,"from"), "from");

            if (to == String.Empty) 
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall,"to"), "to");
 
            this.from = new MailAddress(from); 
            MailAddressCollection collection = new MailAddressCollection();
            collection.Add(to); 
            this.to = collection;
        }

 
        internal Message(MailAddress from, MailAddress to):this() {
            this.from = from; 
            this.To.Add(to); 
        }
 

        public MailPriority Priority{
            get {
                return (((int)priority == -1)?MailPriority.Normal:priority); 
            }
            set{ 
                priority = value; 
            }
        } 

        internal MailAddress From {
            get {
                return from; 
            }
            set { 
                if (value == null) { 
                    throw new ArgumentNullException("value");
                } 
                from = value;
            }
        }
 

        internal MailAddress Sender { 
            get { 
                return sender;
            } 
            set {
                sender = value;
            }
        } 

 
        internal MailAddress ReplyTo { 
            get {
                return replyTo; 
            }
            set {
                replyTo = value;
            } 
        }
 
        internal MailAddressCollection To { 
            get {
                if (to == null) 
                    to = new MailAddressCollection();

                return to;
            } 
        }
 
        internal MailAddressCollection Bcc { 
            get {
                if (bcc == null) 
                    bcc = new MailAddressCollection();

                return bcc;
            } 
        }
 
        internal MailAddressCollection CC { 
            get {
                if (cc == null) 
                    cc = new MailAddressCollection();

                return cc;
            } 
        }
 
 
        internal string Subject {
            get { 
                return subject;
            }
            set {
                 if (value != null && MailBnfHelper.HasCROrLF(value)) { 
                     throw new ArgumentException(SR.GetString(SR.MailSubjectInvalidFormat));
                 } 
                 subject = value; 

                 if (subject != null && subjectEncoding == null && !MimeBasePart.IsAscii(subject,false)) { 
                     subjectEncoding = Encoding.GetEncoding(MimeBasePart.defaultCharSet);
                 }
            }
        } 

        internal Encoding SubjectEncoding { 
            get { 
                return subjectEncoding;
            } 
            set {
                subjectEncoding = value;
            }
        } 

        internal NameValueCollection Headers { 
            get { 
                if (headers == null) {
                    headers = new HeaderCollection(); 
                    if(Logging.On)Logging.Associate(Logging.Web, this, headers);
                }

                return headers; 
            }
        } 
 
        internal NameValueCollection EnvelopeHeaders {
            get { 
                if (envelopeHeaders == null) {
                    envelopeHeaders = new HeaderCollection();
                    if(Logging.On)Logging.Associate(Logging.Web, this, envelopeHeaders);
                } 

                return envelopeHeaders; 
            } 
        }
 

        internal virtual MimeBasePart Content {
            get {
                return content; 
            }
            set { 
                if (value == null) { 
                    throw new ArgumentNullException("value");
                } 

         /*       if (value is MimeEmbeddedMessagePart) {
                    throw new ArgumentException("value");
                } 
*/
                content = value; 
            } 
        }
 

        internal void EmptySendCallback(IAsyncResult result)
        {
            Exception e = null; 

            if(result.CompletedSynchronously){ 
                return; 
            }
 
            EmptySendContext context = (EmptySendContext)result.AsyncState;
            try{
                context.writer.EndGetContentStream(result).Close();
            } 
            catch(Exception ex){
                e = ex; 
            } 
            catch {
                e = new Exception(SR.GetString(SR.net_nonClsCompliantException)); 
            }
            context.result.InvokeCallback(e);
        }
 

        internal class EmptySendContext { 
            internal EmptySendContext(BaseWriter writer, LazyAsyncResult result) { 
                this.writer = writer;
                this.result = result; 
            }

            internal LazyAsyncResult result;
            internal BaseWriter writer; 
        }
 
 

        internal virtual IAsyncResult BeginSend(BaseWriter writer, bool sendEnvelope, AsyncCallback callback, object state) { 

            PrepareHeaders(sendEnvelope);
            writer.WriteHeaders(Headers);
 
            if (Content != null) {
                return Content.BeginSend(writer, callback, state); 
            } 
            else{
                LazyAsyncResult result = new LazyAsyncResult(this,state,callback); 
                IAsyncResult newResult = writer.BeginGetContentStream(EmptySendCallback, new EmptySendContext(writer,result));
                if(newResult.CompletedSynchronously){
                    writer.EndGetContentStream(newResult).Close();
                } 
                return result;
            } 
        } 

 
        internal virtual void EndSend(IAsyncResult asyncResult){
            if (asyncResult == null) {
                throw new ArgumentNullException("asyncResult");
            } 

            if (Content != null) { 
                Content.EndSend(asyncResult); 
            }
            else{ 
                LazyAsyncResult castedAsyncResult = asyncResult as LazyAsyncResult;

                if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this) {
                    throw new ArgumentException(SR.GetString(SR.net_io_invalidasyncresult)); 
                }
 
                if (castedAsyncResult.EndCalled) { 
                    throw new InvalidOperationException(SR.GetString(SR.net_io_invalidendcall, "EndSend"));
                } 

                castedAsyncResult.InternalWaitForCompletion();
                castedAsyncResult.EndCalled = true;
                if (castedAsyncResult.Result is Exception) { 
                    throw (Exception)castedAsyncResult.Result;
                } 
            } 
        }
 
        internal virtual void Send(BaseWriter writer, bool sendEnvelope) {

            if (sendEnvelope) {
                PrepareEnvelopeHeaders(sendEnvelope); 
                writer.WriteHeaders(EnvelopeHeaders);
            } 
 
            PrepareHeaders(sendEnvelope);
            writer.WriteHeaders(Headers); 

            if (Content != null) {
                Content.Send(writer);
            } 
            else{
                writer.GetContentStream().Close(); 
            } 
        }
 

        internal void PrepareEnvelopeHeaders(bool sendEnvelope) {

            EnvelopeHeaders[MailHeaderInfo.GetString(MailHeaderID.XSender)] = From.ToEncodedString(); 

            EnvelopeHeaders.Remove(MailHeaderInfo.GetString(MailHeaderID.XReceiver)); 
            foreach (MailAddress address in To) 
                EnvelopeHeaders.Add(MailHeaderInfo.GetString(MailHeaderID.XReceiver), address.ToEncodedString());
            foreach (MailAddress address in CC) 
                EnvelopeHeaders.Add(MailHeaderInfo.GetString(MailHeaderID.XReceiver), address.ToEncodedString());
            foreach (MailAddress address in Bcc)
                EnvelopeHeaders.Add(MailHeaderInfo.GetString(MailHeaderID.XReceiver), address.ToEncodedString());
        } 

        internal void PrepareHeaders(bool sendEnvelope) { 
 
            Headers[MailHeaderInfo.GetString(MailHeaderID.MimeVersion)] = "1.0";
 
            Headers[MailHeaderInfo.GetString(MailHeaderID.From)] = From.ToEncodedString();

            if(Sender != null){
                Headers[MailHeaderInfo.GetString(MailHeaderID.Sender)] = Sender.ToEncodedString(); 
            }
            else{ 
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Sender)); 
            }
 
            if (To.Count > 0)
                Headers[MailHeaderInfo.GetString(MailHeaderID.To)] = To.ToEncodedString();
            else
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.To)); 

            if (CC.Count > 0) 
                Headers[MailHeaderInfo.GetString(MailHeaderID.Cc)] = CC.ToEncodedString(); 
            else
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Cc)); 

            if (replyTo != null) {
                Headers[MailHeaderInfo.GetString(MailHeaderID.ReplyTo)] = ReplyTo.ToEncodedString();
            } 
            else
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ReplyTo)); 
 
            if (priority == MailPriority.High){
                Headers[MailHeaderInfo.GetString(MailHeaderID.XPriority)] = "1"; 
                Headers[MailHeaderInfo.GetString(MailHeaderID.Priority)] = "urgent";
                Headers[MailHeaderInfo.GetString(MailHeaderID.Importance)] = "high";
            }
            else if (priority == MailPriority.Low){ 
                Headers[MailHeaderInfo.GetString(MailHeaderID.XPriority)] = "5";
                Headers[MailHeaderInfo.GetString(MailHeaderID.Priority)] = "non-urgent"; 
                Headers[MailHeaderInfo.GetString(MailHeaderID.Importance)] = "low"; 
            }
            //if the priority was never set, allow the app to set the headers directly. 
            else if (((int)priority) != -1){
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.XPriority));
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Priority));
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Importance)); 
            }
 
            Headers[MailHeaderInfo.GetString(MailHeaderID.Date)] = MailBnfHelper.GetDateTimeString(DateTime.Now, null); 

 
            if (subject != null && subject != string.Empty){
                Headers[MailHeaderInfo.GetString(MailHeaderID.Subject)] = MimeBasePart.EncodeHeaderValue(subject, subjectEncoding, MimeBasePart.ShouldUseBase64Encoding(subjectEncoding));
            }
            else{ 
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Subject));
            } 
        } 
    }
} 
using System; 
using System.Collections.Specialized;
using System.Net.Mime;
using System.Text;
using System.IO; 

namespace System.Net.Mail 
{ 
    /// <summary>
    /// Summary description for Message. 
    /// </summary>

    public enum MailPriority {
        Normal = 0, 
        Low = 1,
        High = 2 
    } 

 
    internal class Message
    {

        MailAddress from; 
        MailAddress sender;
        MailAddress replyTo; 
        MailAddressCollection to; 
        MailAddressCollection cc;
        MailAddressCollection bcc; 
        MimeBasePart content;
        HeaderCollection headers;
        HeaderCollection envelopeHeaders;
        string subject; 
        Encoding subjectEncoding;
        MailPriority priority = (MailPriority)(-1); 
 

        internal Message() { 
        }

        internal Message(string from, string to):this() {
            if (from == null) 
                throw new ArgumentNullException("from");
 
            if (to == null) 
                throw new ArgumentNullException("to");
 
            if (from == String.Empty)
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall,"from"), "from");

            if (to == String.Empty) 
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall,"to"), "to");
 
            this.from = new MailAddress(from); 
            MailAddressCollection collection = new MailAddressCollection();
            collection.Add(to); 
            this.to = collection;
        }

 
        internal Message(MailAddress from, MailAddress to):this() {
            this.from = from; 
            this.To.Add(to); 
        }
 

        public MailPriority Priority{
            get {
                return (((int)priority == -1)?MailPriority.Normal:priority); 
            }
            set{ 
                priority = value; 
            }
        } 

        internal MailAddress From {
            get {
                return from; 
            }
            set { 
                if (value == null) { 
                    throw new ArgumentNullException("value");
                } 
                from = value;
            }
        }
 

        internal MailAddress Sender { 
            get { 
                return sender;
            } 
            set {
                sender = value;
            }
        } 

 
        internal MailAddress ReplyTo { 
            get {
                return replyTo; 
            }
            set {
                replyTo = value;
            } 
        }
 
        internal MailAddressCollection To { 
            get {
                if (to == null) 
                    to = new MailAddressCollection();

                return to;
            } 
        }
 
        internal MailAddressCollection Bcc { 
            get {
                if (bcc == null) 
                    bcc = new MailAddressCollection();

                return bcc;
            } 
        }
 
        internal MailAddressCollection CC { 
            get {
                if (cc == null) 
                    cc = new MailAddressCollection();

                return cc;
            } 
        }
 
 
        internal string Subject {
            get { 
                return subject;
            }
            set {
                 if (value != null && MailBnfHelper.HasCROrLF(value)) { 
                     throw new ArgumentException(SR.GetString(SR.MailSubjectInvalidFormat));
                 } 
                 subject = value; 

                 if (subject != null && subjectEncoding == null && !MimeBasePart.IsAscii(subject,false)) { 
                     subjectEncoding = Encoding.GetEncoding(MimeBasePart.defaultCharSet);
                 }
            }
        } 

        internal Encoding SubjectEncoding { 
            get { 
                return subjectEncoding;
            } 
            set {
                subjectEncoding = value;
            }
        } 

        internal NameValueCollection Headers { 
            get { 
                if (headers == null) {
                    headers = new HeaderCollection(); 
                    if(Logging.On)Logging.Associate(Logging.Web, this, headers);
                }

                return headers; 
            }
        } 
 
        internal NameValueCollection EnvelopeHeaders {
            get { 
                if (envelopeHeaders == null) {
                    envelopeHeaders = new HeaderCollection();
                    if(Logging.On)Logging.Associate(Logging.Web, this, envelopeHeaders);
                } 

                return envelopeHeaders; 
            } 
        }
 

        internal virtual MimeBasePart Content {
            get {
                return content; 
            }
            set { 
                if (value == null) { 
                    throw new ArgumentNullException("value");
                } 

         /*       if (value is MimeEmbeddedMessagePart) {
                    throw new ArgumentException("value");
                } 
*/
                content = value; 
            } 
        }
 

        internal void EmptySendCallback(IAsyncResult result)
        {
            Exception e = null; 

            if(result.CompletedSynchronously){ 
                return; 
            }
 
            EmptySendContext context = (EmptySendContext)result.AsyncState;
            try{
                context.writer.EndGetContentStream(result).Close();
            } 
            catch(Exception ex){
                e = ex; 
            } 
            catch {
                e = new Exception(SR.GetString(SR.net_nonClsCompliantException)); 
            }
            context.result.InvokeCallback(e);
        }
 

        internal class EmptySendContext { 
            internal EmptySendContext(BaseWriter writer, LazyAsyncResult result) { 
                this.writer = writer;
                this.result = result; 
            }

            internal LazyAsyncResult result;
            internal BaseWriter writer; 
        }
 
 

        internal virtual IAsyncResult BeginSend(BaseWriter writer, bool sendEnvelope, AsyncCallback callback, object state) { 

            PrepareHeaders(sendEnvelope);
            writer.WriteHeaders(Headers);
 
            if (Content != null) {
                return Content.BeginSend(writer, callback, state); 
            } 
            else{
                LazyAsyncResult result = new LazyAsyncResult(this,state,callback); 
                IAsyncResult newResult = writer.BeginGetContentStream(EmptySendCallback, new EmptySendContext(writer,result));
                if(newResult.CompletedSynchronously){
                    writer.EndGetContentStream(newResult).Close();
                } 
                return result;
            } 
        } 

 
        internal virtual void EndSend(IAsyncResult asyncResult){
            if (asyncResult == null) {
                throw new ArgumentNullException("asyncResult");
            } 

            if (Content != null) { 
                Content.EndSend(asyncResult); 
            }
            else{ 
                LazyAsyncResult castedAsyncResult = asyncResult as LazyAsyncResult;

                if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this) {
                    throw new ArgumentException(SR.GetString(SR.net_io_invalidasyncresult)); 
                }
 
                if (castedAsyncResult.EndCalled) { 
                    throw new InvalidOperationException(SR.GetString(SR.net_io_invalidendcall, "EndSend"));
                } 

                castedAsyncResult.InternalWaitForCompletion();
                castedAsyncResult.EndCalled = true;
                if (castedAsyncResult.Result is Exception) { 
                    throw (Exception)castedAsyncResult.Result;
                } 
            } 
        }
 
        internal virtual void Send(BaseWriter writer, bool sendEnvelope) {

            if (sendEnvelope) {
                PrepareEnvelopeHeaders(sendEnvelope); 
                writer.WriteHeaders(EnvelopeHeaders);
            } 
 
            PrepareHeaders(sendEnvelope);
            writer.WriteHeaders(Headers); 

            if (Content != null) {
                Content.Send(writer);
            } 
            else{
                writer.GetContentStream().Close(); 
            } 
        }
 

        internal void PrepareEnvelopeHeaders(bool sendEnvelope) {

            EnvelopeHeaders[MailHeaderInfo.GetString(MailHeaderID.XSender)] = From.ToEncodedString(); 

            EnvelopeHeaders.Remove(MailHeaderInfo.GetString(MailHeaderID.XReceiver)); 
            foreach (MailAddress address in To) 
                EnvelopeHeaders.Add(MailHeaderInfo.GetString(MailHeaderID.XReceiver), address.ToEncodedString());
            foreach (MailAddress address in CC) 
                EnvelopeHeaders.Add(MailHeaderInfo.GetString(MailHeaderID.XReceiver), address.ToEncodedString());
            foreach (MailAddress address in Bcc)
                EnvelopeHeaders.Add(MailHeaderInfo.GetString(MailHeaderID.XReceiver), address.ToEncodedString());
        } 

        internal void PrepareHeaders(bool sendEnvelope) { 
 
            Headers[MailHeaderInfo.GetString(MailHeaderID.MimeVersion)] = "1.0";
 
            Headers[MailHeaderInfo.GetString(MailHeaderID.From)] = From.ToEncodedString();

            if(Sender != null){
                Headers[MailHeaderInfo.GetString(MailHeaderID.Sender)] = Sender.ToEncodedString(); 
            }
            else{ 
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Sender)); 
            }
 
            if (To.Count > 0)
                Headers[MailHeaderInfo.GetString(MailHeaderID.To)] = To.ToEncodedString();
            else
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.To)); 

            if (CC.Count > 0) 
                Headers[MailHeaderInfo.GetString(MailHeaderID.Cc)] = CC.ToEncodedString(); 
            else
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Cc)); 

            if (replyTo != null) {
                Headers[MailHeaderInfo.GetString(MailHeaderID.ReplyTo)] = ReplyTo.ToEncodedString();
            } 
            else
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ReplyTo)); 
 
            if (priority == MailPriority.High){
                Headers[MailHeaderInfo.GetString(MailHeaderID.XPriority)] = "1"; 
                Headers[MailHeaderInfo.GetString(MailHeaderID.Priority)] = "urgent";
                Headers[MailHeaderInfo.GetString(MailHeaderID.Importance)] = "high";
            }
            else if (priority == MailPriority.Low){ 
                Headers[MailHeaderInfo.GetString(MailHeaderID.XPriority)] = "5";
                Headers[MailHeaderInfo.GetString(MailHeaderID.Priority)] = "non-urgent"; 
                Headers[MailHeaderInfo.GetString(MailHeaderID.Importance)] = "low"; 
            }
            //if the priority was never set, allow the app to set the headers directly. 
            else if (((int)priority) != -1){
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.XPriority));
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Priority));
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Importance)); 
            }
 
            Headers[MailHeaderInfo.GetString(MailHeaderID.Date)] = MailBnfHelper.GetDateTimeString(DateTime.Now, null); 

 
            if (subject != null && subject != string.Empty){
                Headers[MailHeaderInfo.GetString(MailHeaderID.Subject)] = MimeBasePart.EncodeHeaderValue(subject, subjectEncoding, MimeBasePart.ShouldUseBase64Encoding(subjectEncoding));
            }
            else{ 
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Subject));
            } 
        } 
    }
} 
