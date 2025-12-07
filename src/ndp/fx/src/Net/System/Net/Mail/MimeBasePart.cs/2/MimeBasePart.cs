using System; 
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Net.Mail; 

namespace System.Net.Mime 
{ 

    internal class MimeBasePart 
    {
        protected ContentType contentType;
        protected ContentDisposition contentDisposition;
        HeaderCollection headers; 
        internal const string defaultCharSet = "utf-8";//"iso-8859-1";
 
 
        internal MimeBasePart()
        { 
        }


        internal static bool ShouldUseBase64Encoding(Encoding encoding){ 
            if (encoding == Encoding.Unicode || encoding == Encoding.UTF8 || encoding == Encoding.UTF32  || encoding == Encoding.BigEndianUnicode) {
                return true; 
            } 
            return false;
        } 


        internal static string EncodeHeaderValue(string value, Encoding encoding, bool base64Encoding) {
            StringBuilder newString = new StringBuilder(); 

            if (encoding == null && IsAscii(value,false)) { 
                return value; 
            }
 
            if (encoding == null) {
                encoding = Encoding.GetEncoding(MimeBasePart.defaultCharSet);
            }
 
            string encodingName = encoding.BodyName;
            if(encoding == Encoding.BigEndianUnicode){ 
                encodingName = "utf-16be"; 
            }
 
            newString.Append("=?");
            newString.Append(encodingName);
            newString.Append("?");
            newString.Append(base64Encoding ? "B" : "Q"); 
            newString.Append("?");
 
            byte[] buffer = encoding.GetBytes(value); 

            if (base64Encoding) { 
                Base64Stream s = new Base64Stream(-1);

                s.EncodeBytes(buffer, 0, buffer.Length, true);
                newString.Append(ASCIIEncoding.ASCII.GetString(s.WriteState.Buffer, 0, s.WriteState.Length)); 
            }
            else { 
                QuotedPrintableStream s = new QuotedPrintableStream(-1); 

                s.EncodeBytes(buffer, 0, buffer.Length); 
                newString.Append(ASCIIEncoding.ASCII.GetString(s.WriteState.Buffer, 0, s.WriteState.Length));
            }

            newString.Append("?="); 
            return newString.ToString();
        } 
 
        internal static string DecodeHeaderValue(string value) {
            if(value == null || value.Length == 0){ 
                return String.Empty;
            }

            string[] subStrings = value.Split('?'); 
            if ((subStrings.Length != 5 || subStrings[0] != "=" || subStrings[4] != "=")) {
                return value; 
            } 
            string charSet = subStrings[1];
            bool base64Encoding = (subStrings[2] == "B"); 
            byte[] buffer = ASCIIEncoding.ASCII.GetBytes(subStrings[3]);
            int newLength;

            if (base64Encoding) { 
                Base64Stream s = new Base64Stream();
 
                newLength = s.DecodeBytes(buffer, 0, buffer.Length); 
            }
            else { 
                QuotedPrintableStream s = new QuotedPrintableStream();

                newLength = s.DecodeBytes(buffer, 0, buffer.Length);
            } 

            Encoding encoding = Encoding.GetEncoding(charSet); 
            string newValue = encoding.GetString(buffer, 0, newLength); 

            return newValue; 
        }


        internal static Encoding DecodeEncoding(string value) { 
            if(value == null || value.Length == 0){
                return null; 
            } 

            string[] subStrings = value.Split('?'); 
            if ((subStrings.Length != 5 || subStrings[0] != "=" || subStrings[4] != "=")) {
                return null;
            }
            string charSet = subStrings[1]; 
            return Encoding.GetEncoding(charSet);
        } 
 

 
        internal static bool IsAscii(string value, bool permitCROrLF) {
            if (value == null)
                throw new ArgumentNullException("value");
 
            foreach (char c in value) {
                if ((int)c > 0x7f) { 
                    return false; 
                }
                if (!permitCROrLF && (c=='\r' || c=='\n')) { 
                    return false;
                }
            }
            return true; 
        }
 
        internal static bool IsAnsi(string value, bool permitCROrLF) { 
            if (value == null)
                throw new ArgumentNullException("value"); 

            foreach (char c in value) {
                if ((int)c > 0xff) {
                    return false; 
                }
                if (!permitCROrLF && (c=='\r' || c=='\n')) { 
                    return false; 
                }
            } 
            return true;
        }

 
        /*
        // Consider removing. 
        internal string ContentDescription { 
            get {
                return Headers[MimePartHeaderNames.ContentDescription]; 
            }
            set {
                if (value == null)
                    throw new ArgumentNullException("value"); 

                Headers[MimePartHeaderNames.ContentDescription] = value; 
            } 
        }
        */ 

        internal string ContentID {
            get {
                return Headers[MailHeaderInfo.GetString(MailHeaderID.ContentID)]; 
            }
            set { 
                if (string.IsNullOrEmpty(value)) 
                {
                    Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentID)); 
                }
                else
                {
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentID)] = value; 
                }
            } 
        } 

        internal string ContentLocation 
        {
            get
            {
                return Headers[MailHeaderInfo.GetString(MailHeaderID.ContentLocation)]; 
            }
            set 
            { 
                if (string.IsNullOrEmpty(value))
                { 
                    Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentLocation));
                }
                else
                { 
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentLocation)] = value;
                } 
            } 
        }
 
        internal NameValueCollection Headers
        {
            get {
                //persist existing info before returning 
                if (headers == null)
                    headers = new HeaderCollection(); 
 
                if (contentType == null){
                    contentType = new ContentType(); 
                }
                contentType.PersistIfNeeded(headers,false);

                if (contentDisposition != null) 
                    contentDisposition.PersistIfNeeded(headers,false);
                return headers; 
            } 
        }
 
        internal ContentType ContentType{
            get{
                if (contentType == null){
                    contentType = new ContentType(); 
                }
                return contentType; 
            } 
            set {
                if (value == null) 
                    throw new ArgumentNullException("value");

                contentType = value;
                contentType.PersistIfNeeded((HeaderCollection)Headers,true); 
            }
        } 
 

        internal virtual void Send(BaseWriter writer) { throw new NotImplementedException(); } 
        internal virtual IAsyncResult BeginSend(BaseWriter writer, AsyncCallback callback, object state) { throw new NotImplementedException(); }

        internal void EndSend(IAsyncResult asyncResult) {
 
            if (asyncResult == null) {
                throw new ArgumentNullException("asyncResult"); 
            } 

            LazyAsyncResult castedAsyncResult = asyncResult as MimePartAsyncResult; 

            if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this) {
                throw new ArgumentException(SR.GetString(SR.net_io_invalidasyncresult), "asyncResult");
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
 
        internal class MimePartAsyncResult: LazyAsyncResult {
            internal MimePartAsyncResult(MimeBasePart part, object state, AsyncCallback callback):base(part,state,callback) { 
            }
        }
    }
} 

using System; 
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Net.Mail; 

namespace System.Net.Mime 
{ 

    internal class MimeBasePart 
    {
        protected ContentType contentType;
        protected ContentDisposition contentDisposition;
        HeaderCollection headers; 
        internal const string defaultCharSet = "utf-8";//"iso-8859-1";
 
 
        internal MimeBasePart()
        { 
        }


        internal static bool ShouldUseBase64Encoding(Encoding encoding){ 
            if (encoding == Encoding.Unicode || encoding == Encoding.UTF8 || encoding == Encoding.UTF32  || encoding == Encoding.BigEndianUnicode) {
                return true; 
            } 
            return false;
        } 


        internal static string EncodeHeaderValue(string value, Encoding encoding, bool base64Encoding) {
            StringBuilder newString = new StringBuilder(); 

            if (encoding == null && IsAscii(value,false)) { 
                return value; 
            }
 
            if (encoding == null) {
                encoding = Encoding.GetEncoding(MimeBasePart.defaultCharSet);
            }
 
            string encodingName = encoding.BodyName;
            if(encoding == Encoding.BigEndianUnicode){ 
                encodingName = "utf-16be"; 
            }
 
            newString.Append("=?");
            newString.Append(encodingName);
            newString.Append("?");
            newString.Append(base64Encoding ? "B" : "Q"); 
            newString.Append("?");
 
            byte[] buffer = encoding.GetBytes(value); 

            if (base64Encoding) { 
                Base64Stream s = new Base64Stream(-1);

                s.EncodeBytes(buffer, 0, buffer.Length, true);
                newString.Append(ASCIIEncoding.ASCII.GetString(s.WriteState.Buffer, 0, s.WriteState.Length)); 
            }
            else { 
                QuotedPrintableStream s = new QuotedPrintableStream(-1); 

                s.EncodeBytes(buffer, 0, buffer.Length); 
                newString.Append(ASCIIEncoding.ASCII.GetString(s.WriteState.Buffer, 0, s.WriteState.Length));
            }

            newString.Append("?="); 
            return newString.ToString();
        } 
 
        internal static string DecodeHeaderValue(string value) {
            if(value == null || value.Length == 0){ 
                return String.Empty;
            }

            string[] subStrings = value.Split('?'); 
            if ((subStrings.Length != 5 || subStrings[0] != "=" || subStrings[4] != "=")) {
                return value; 
            } 
            string charSet = subStrings[1];
            bool base64Encoding = (subStrings[2] == "B"); 
            byte[] buffer = ASCIIEncoding.ASCII.GetBytes(subStrings[3]);
            int newLength;

            if (base64Encoding) { 
                Base64Stream s = new Base64Stream();
 
                newLength = s.DecodeBytes(buffer, 0, buffer.Length); 
            }
            else { 
                QuotedPrintableStream s = new QuotedPrintableStream();

                newLength = s.DecodeBytes(buffer, 0, buffer.Length);
            } 

            Encoding encoding = Encoding.GetEncoding(charSet); 
            string newValue = encoding.GetString(buffer, 0, newLength); 

            return newValue; 
        }


        internal static Encoding DecodeEncoding(string value) { 
            if(value == null || value.Length == 0){
                return null; 
            } 

            string[] subStrings = value.Split('?'); 
            if ((subStrings.Length != 5 || subStrings[0] != "=" || subStrings[4] != "=")) {
                return null;
            }
            string charSet = subStrings[1]; 
            return Encoding.GetEncoding(charSet);
        } 
 

 
        internal static bool IsAscii(string value, bool permitCROrLF) {
            if (value == null)
                throw new ArgumentNullException("value");
 
            foreach (char c in value) {
                if ((int)c > 0x7f) { 
                    return false; 
                }
                if (!permitCROrLF && (c=='\r' || c=='\n')) { 
                    return false;
                }
            }
            return true; 
        }
 
        internal static bool IsAnsi(string value, bool permitCROrLF) { 
            if (value == null)
                throw new ArgumentNullException("value"); 

            foreach (char c in value) {
                if ((int)c > 0xff) {
                    return false; 
                }
                if (!permitCROrLF && (c=='\r' || c=='\n')) { 
                    return false; 
                }
            } 
            return true;
        }

 
        /*
        // Consider removing. 
        internal string ContentDescription { 
            get {
                return Headers[MimePartHeaderNames.ContentDescription]; 
            }
            set {
                if (value == null)
                    throw new ArgumentNullException("value"); 

                Headers[MimePartHeaderNames.ContentDescription] = value; 
            } 
        }
        */ 

        internal string ContentID {
            get {
                return Headers[MailHeaderInfo.GetString(MailHeaderID.ContentID)]; 
            }
            set { 
                if (string.IsNullOrEmpty(value)) 
                {
                    Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentID)); 
                }
                else
                {
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentID)] = value; 
                }
            } 
        } 

        internal string ContentLocation 
        {
            get
            {
                return Headers[MailHeaderInfo.GetString(MailHeaderID.ContentLocation)]; 
            }
            set 
            { 
                if (string.IsNullOrEmpty(value))
                { 
                    Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentLocation));
                }
                else
                { 
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentLocation)] = value;
                } 
            } 
        }
 
        internal NameValueCollection Headers
        {
            get {
                //persist existing info before returning 
                if (headers == null)
                    headers = new HeaderCollection(); 
 
                if (contentType == null){
                    contentType = new ContentType(); 
                }
                contentType.PersistIfNeeded(headers,false);

                if (contentDisposition != null) 
                    contentDisposition.PersistIfNeeded(headers,false);
                return headers; 
            } 
        }
 
        internal ContentType ContentType{
            get{
                if (contentType == null){
                    contentType = new ContentType(); 
                }
                return contentType; 
            } 
            set {
                if (value == null) 
                    throw new ArgumentNullException("value");

                contentType = value;
                contentType.PersistIfNeeded((HeaderCollection)Headers,true); 
            }
        } 
 

        internal virtual void Send(BaseWriter writer) { throw new NotImplementedException(); } 
        internal virtual IAsyncResult BeginSend(BaseWriter writer, AsyncCallback callback, object state) { throw new NotImplementedException(); }

        internal void EndSend(IAsyncResult asyncResult) {
 
            if (asyncResult == null) {
                throw new ArgumentNullException("asyncResult"); 
            } 

            LazyAsyncResult castedAsyncResult = asyncResult as MimePartAsyncResult; 

            if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this) {
                throw new ArgumentException(SR.GetString(SR.net_io_invalidasyncresult), "asyncResult");
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
 
        internal class MimePartAsyncResult: LazyAsyncResult {
            internal MimePartAsyncResult(MimeBasePart part, object state, AsyncCallback callback):base(part,state,callback) { 
            }
        }
    }
} 

