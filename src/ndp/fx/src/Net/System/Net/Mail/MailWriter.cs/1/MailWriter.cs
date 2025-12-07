//------------------------------------------------------------------------------ 
// <copyright file="MailWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mail 
{ 
    using System;
    using System.Collections; 
    using System.IO;
    using System.Text;
    using System.Collections.Specialized;
    using System.Net.Mime; 

    internal class MailWriter:BaseWriter 
    { 
        static byte[] CRLF = new byte[] { (byte)'\r', (byte)'\n' };
        static int DefaultLineLength = 78; 

        Stream contentStream;
        bool isInContent;
        int lineLength; 
        EventHandler onCloseHandler;
        Stream stream; 
        BufferBuilder bufferBuilder = new BufferBuilder(); 

        static AsyncCallback onWrite = new AsyncCallback(OnWrite); 

        /// <summary>
        /// ctor.
        /// </summary> 
        /// <param name="stream">Underlying stream</param>
        internal MailWriter(Stream stream) : this(stream, DefaultLineLength) 
        { 
        }
 
        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="stream">Underlying stream</param> 
        /// <param name="lineLength">Preferred line length</param>
        internal MailWriter(Stream stream, int lineLength) 
        { 
            if (stream == null)
                throw new ArgumentNullException("stream"); 

            if (lineLength < 0)
                throw new ArgumentOutOfRangeException("lineLength");
 
            this.stream = stream;
            this.lineLength = lineLength; 
            this.onCloseHandler = new EventHandler(OnClose); 
        }
 
        /// <summary>
        /// Closes underlying stream.
        /// </summary>
        internal override void Close() 
        {
            // 
 
            this.stream.Write(CRLF,0,2);
            this.stream.Close(); 
        }

        internal IAsyncResult BeginGetContentStream(ContentTransferEncoding contentTransferEncoding, AsyncCallback callback, object state)
        { 
            MultiAsyncResult multiResult = new MultiAsyncResult(this, callback, state);
 
            Stream s = GetContentStream(contentTransferEncoding, multiResult); 

            if (!(multiResult.Result is Exception)) 
                multiResult.Result = s;

            multiResult.CompleteSequence();
 
            return multiResult;
        } 
 
        internal override IAsyncResult BeginGetContentStream(AsyncCallback callback, object state)
        { 
            return BeginGetContentStream(ContentTransferEncoding.SevenBit, callback, state);
        }

        internal override Stream EndGetContentStream(IAsyncResult result) 
        {
            object o = MultiAsyncResult.End(result); 
            if(o is Exception){ 
                throw (Exception) o;
            } 
            return (Stream)o;
        }

        internal Stream GetContentStream(ContentTransferEncoding contentTransferEncoding) 
        {
            return GetContentStream(contentTransferEncoding, null); 
        } 

        internal override Stream GetContentStream() 
        {
            return GetContentStream(ContentTransferEncoding.SevenBit);
        }
 
        Stream GetContentStream(ContentTransferEncoding contentTransferEncoding, MultiAsyncResult multiResult)
        { 
            if (this.isInContent) 
                throw new InvalidOperationException(SR.GetString(SR.MailWriterIsInContent));
 
            this.isInContent = true;

            this.bufferBuilder.Append(CRLF);
            Flush(multiResult); 

            Stream stream = this.stream; 
            if (contentTransferEncoding == ContentTransferEncoding.SevenBit) 
                stream = new SevenBitStream(stream);
            else if (contentTransferEncoding == ContentTransferEncoding.QuotedPrintable) 
                stream = new QuotedPrintableStream(stream, this.lineLength);
            else if (contentTransferEncoding == ContentTransferEncoding.Base64)
                stream = new Base64Stream(stream, this.lineLength);
 
            ClosableStream cs = new ClosableStream(stream, this.onCloseHandler);
            this.contentStream = cs; 
            return cs; 
        }
 

        internal override void WriteHeader(string name, string value)
        {
            if (name == null) 
                throw new ArgumentNullException("name");
 
            if (value == null) 
                throw new ArgumentNullException("value");
 
            if (this.isInContent)
                throw new InvalidOperationException(SR.GetString(SR.MailWriterIsInContent));

            this.bufferBuilder.Append(name); 
            this.bufferBuilder.Append(": ");
            WriteAndFold(value);//, name.Length + 2); 
            this.bufferBuilder.Append(CRLF); 
        }
 

        internal override void WriteHeaders(NameValueCollection headers) {
            if (headers == null)
                throw new ArgumentNullException("headers"); 

            if (this.isInContent) 
                throw new InvalidOperationException(SR.GetString(SR.MailWriterIsInContent)); 

            foreach (string key in headers) { 
                string[] values = headers.GetValues(key);
                foreach (string value in values)
                    WriteHeader(key, value);
            } 
        }
 
        // helper methods 

        /// <summary> 
        /// Called when the current stream is closed.  Allows us to
        /// prepare for the next message part.
        /// </summary>
        /// <param name="sender">Sender of the close event</param> 
        /// <param name="args">Event args (not used)</param>
        void OnClose(object sender, EventArgs args) 
        { 
            System.Diagnostics.Debug.Assert(this.contentStream == sender);
 
            this.contentStream.Flush();

            this.contentStream = null;
        } 

        static void OnWrite(IAsyncResult result) 
        { 
            if (!result.CompletedSynchronously)
            { 
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState;
                MailWriter thisPtr = (MailWriter)multiResult.Context;
                try
                { 
                    thisPtr.stream.EndWrite(result);
                    multiResult.Leave(); 
                } 
                catch (Exception e)
                { 
                    multiResult.Leave(e);
                }
                catch {
                    multiResult.Leave(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                }
            } 
        } 

        void Flush(MultiAsyncResult multiResult) 
        {
            if (this.bufferBuilder.Length > 0)
            {
                if (multiResult != null) 
                {
                    multiResult.Enter(); 
                    IAsyncResult result = this.stream.BeginWrite(this.bufferBuilder.GetBuffer(), 0, this.bufferBuilder.Length, onWrite, multiResult); 
                    if (result.CompletedSynchronously)
                    { 
                        this.stream.EndWrite(result);
                        multiResult.Leave();
                    }
                } 
                else
                { 
                    this.stream.Write(this.bufferBuilder.GetBuffer(), 0, this.bufferBuilder.Length); 
                }
                this.bufferBuilder.Reset(); 
            }
        }

 

        void WriteAndFold(string value) { 
            if (value.Length < DefaultLineLength) { 
                bufferBuilder.Append(value);
                return; 
            }

            int i = 0;
            int j = value.Length; 

            //new char[2] { '\t', ' ' } 
            while (j - i > DefaultLineLength){ 
                int whiteSpace = value.LastIndexOf(' ', i + DefaultLineLength - 1, DefaultLineLength-1);
                if (whiteSpace > -1) { 
                    bufferBuilder.Append(value, i, whiteSpace-i);
                    bufferBuilder.Append(CRLF);
                    i = whiteSpace;
                } 
                else {
                    bufferBuilder.Append(value, i, DefaultLineLength); 
                    i += DefaultLineLength; 
                }
            } 
            if (i < j){
                bufferBuilder.Append(value, i, j-i);
            }
        } 

/*  old 
 
        void WriteAndFold(string value, int startLength)
        { 
            for (int i = 0, l = 0, s = 0; ; i++)
            {
                if (i == value.Length)
                { 
                    if (i - s > 0)
                    { 
                        this.bufferBuilder.Append(value, s, i - s); 
                    }
                    break; 
                }
                if (value[i] == ' ' || value[i] == '\t')
                {
                    if (i - s >= this.lineLength - startLength) 
                    {
                        startLength = 0; 
                        if (l == s) 
                        {
                            l = i; 
                        }
                        this.bufferBuilder.Append(value, s, l - s);
                        this.bufferBuilder.Append(CRLF);
                        s = l; 
                    }
                    l = i; 
                } 
            }
        } 
        */
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="MailWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mail 
{ 
    using System;
    using System.Collections; 
    using System.IO;
    using System.Text;
    using System.Collections.Specialized;
    using System.Net.Mime; 

    internal class MailWriter:BaseWriter 
    { 
        static byte[] CRLF = new byte[] { (byte)'\r', (byte)'\n' };
        static int DefaultLineLength = 78; 

        Stream contentStream;
        bool isInContent;
        int lineLength; 
        EventHandler onCloseHandler;
        Stream stream; 
        BufferBuilder bufferBuilder = new BufferBuilder(); 

        static AsyncCallback onWrite = new AsyncCallback(OnWrite); 

        /// <summary>
        /// ctor.
        /// </summary> 
        /// <param name="stream">Underlying stream</param>
        internal MailWriter(Stream stream) : this(stream, DefaultLineLength) 
        { 
        }
 
        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="stream">Underlying stream</param> 
        /// <param name="lineLength">Preferred line length</param>
        internal MailWriter(Stream stream, int lineLength) 
        { 
            if (stream == null)
                throw new ArgumentNullException("stream"); 

            if (lineLength < 0)
                throw new ArgumentOutOfRangeException("lineLength");
 
            this.stream = stream;
            this.lineLength = lineLength; 
            this.onCloseHandler = new EventHandler(OnClose); 
        }
 
        /// <summary>
        /// Closes underlying stream.
        /// </summary>
        internal override void Close() 
        {
            // 
 
            this.stream.Write(CRLF,0,2);
            this.stream.Close(); 
        }

        internal IAsyncResult BeginGetContentStream(ContentTransferEncoding contentTransferEncoding, AsyncCallback callback, object state)
        { 
            MultiAsyncResult multiResult = new MultiAsyncResult(this, callback, state);
 
            Stream s = GetContentStream(contentTransferEncoding, multiResult); 

            if (!(multiResult.Result is Exception)) 
                multiResult.Result = s;

            multiResult.CompleteSequence();
 
            return multiResult;
        } 
 
        internal override IAsyncResult BeginGetContentStream(AsyncCallback callback, object state)
        { 
            return BeginGetContentStream(ContentTransferEncoding.SevenBit, callback, state);
        }

        internal override Stream EndGetContentStream(IAsyncResult result) 
        {
            object o = MultiAsyncResult.End(result); 
            if(o is Exception){ 
                throw (Exception) o;
            } 
            return (Stream)o;
        }

        internal Stream GetContentStream(ContentTransferEncoding contentTransferEncoding) 
        {
            return GetContentStream(contentTransferEncoding, null); 
        } 

        internal override Stream GetContentStream() 
        {
            return GetContentStream(ContentTransferEncoding.SevenBit);
        }
 
        Stream GetContentStream(ContentTransferEncoding contentTransferEncoding, MultiAsyncResult multiResult)
        { 
            if (this.isInContent) 
                throw new InvalidOperationException(SR.GetString(SR.MailWriterIsInContent));
 
            this.isInContent = true;

            this.bufferBuilder.Append(CRLF);
            Flush(multiResult); 

            Stream stream = this.stream; 
            if (contentTransferEncoding == ContentTransferEncoding.SevenBit) 
                stream = new SevenBitStream(stream);
            else if (contentTransferEncoding == ContentTransferEncoding.QuotedPrintable) 
                stream = new QuotedPrintableStream(stream, this.lineLength);
            else if (contentTransferEncoding == ContentTransferEncoding.Base64)
                stream = new Base64Stream(stream, this.lineLength);
 
            ClosableStream cs = new ClosableStream(stream, this.onCloseHandler);
            this.contentStream = cs; 
            return cs; 
        }
 

        internal override void WriteHeader(string name, string value)
        {
            if (name == null) 
                throw new ArgumentNullException("name");
 
            if (value == null) 
                throw new ArgumentNullException("value");
 
            if (this.isInContent)
                throw new InvalidOperationException(SR.GetString(SR.MailWriterIsInContent));

            this.bufferBuilder.Append(name); 
            this.bufferBuilder.Append(": ");
            WriteAndFold(value);//, name.Length + 2); 
            this.bufferBuilder.Append(CRLF); 
        }
 

        internal override void WriteHeaders(NameValueCollection headers) {
            if (headers == null)
                throw new ArgumentNullException("headers"); 

            if (this.isInContent) 
                throw new InvalidOperationException(SR.GetString(SR.MailWriterIsInContent)); 

            foreach (string key in headers) { 
                string[] values = headers.GetValues(key);
                foreach (string value in values)
                    WriteHeader(key, value);
            } 
        }
 
        // helper methods 

        /// <summary> 
        /// Called when the current stream is closed.  Allows us to
        /// prepare for the next message part.
        /// </summary>
        /// <param name="sender">Sender of the close event</param> 
        /// <param name="args">Event args (not used)</param>
        void OnClose(object sender, EventArgs args) 
        { 
            System.Diagnostics.Debug.Assert(this.contentStream == sender);
 
            this.contentStream.Flush();

            this.contentStream = null;
        } 

        static void OnWrite(IAsyncResult result) 
        { 
            if (!result.CompletedSynchronously)
            { 
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState;
                MailWriter thisPtr = (MailWriter)multiResult.Context;
                try
                { 
                    thisPtr.stream.EndWrite(result);
                    multiResult.Leave(); 
                } 
                catch (Exception e)
                { 
                    multiResult.Leave(e);
                }
                catch {
                    multiResult.Leave(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                }
            } 
        } 

        void Flush(MultiAsyncResult multiResult) 
        {
            if (this.bufferBuilder.Length > 0)
            {
                if (multiResult != null) 
                {
                    multiResult.Enter(); 
                    IAsyncResult result = this.stream.BeginWrite(this.bufferBuilder.GetBuffer(), 0, this.bufferBuilder.Length, onWrite, multiResult); 
                    if (result.CompletedSynchronously)
                    { 
                        this.stream.EndWrite(result);
                        multiResult.Leave();
                    }
                } 
                else
                { 
                    this.stream.Write(this.bufferBuilder.GetBuffer(), 0, this.bufferBuilder.Length); 
                }
                this.bufferBuilder.Reset(); 
            }
        }

 

        void WriteAndFold(string value) { 
            if (value.Length < DefaultLineLength) { 
                bufferBuilder.Append(value);
                return; 
            }

            int i = 0;
            int j = value.Length; 

            //new char[2] { '\t', ' ' } 
            while (j - i > DefaultLineLength){ 
                int whiteSpace = value.LastIndexOf(' ', i + DefaultLineLength - 1, DefaultLineLength-1);
                if (whiteSpace > -1) { 
                    bufferBuilder.Append(value, i, whiteSpace-i);
                    bufferBuilder.Append(CRLF);
                    i = whiteSpace;
                } 
                else {
                    bufferBuilder.Append(value, i, DefaultLineLength); 
                    i += DefaultLineLength; 
                }
            } 
            if (i < j){
                bufferBuilder.Append(value, i, j-i);
            }
        } 

/*  old 
 
        void WriteAndFold(string value, int startLength)
        { 
            for (int i = 0, l = 0, s = 0; ; i++)
            {
                if (i == value.Length)
                { 
                    if (i - s > 0)
                    { 
                        this.bufferBuilder.Append(value, s, i - s); 
                    }
                    break; 
                }
                if (value[i] == ' ' || value[i] == '\t')
                {
                    if (i - s >= this.lineLength - startLength) 
                    {
                        startLength = 0; 
                        if (l == s) 
                        {
                            l = i; 
                        }
                        this.bufferBuilder.Append(value, s, l - s);
                        this.bufferBuilder.Append(CRLF);
                        s = l; 
                    }
                    l = i; 
                } 
            }
        } 
        */
    }
}
