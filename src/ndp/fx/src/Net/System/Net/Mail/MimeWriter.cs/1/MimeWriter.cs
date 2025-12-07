//------------------------------------------------------------------------------ 
// <copyright file="MimeWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.Collections; 
    using System.IO;
    using System.Text;
    using System.Collections.Specialized;
 
    /// <summary>
    /// Provides an abstraction for writing a MIME multi-part 
    /// message. 
    /// </summary>
 
    internal class MimeWriter:BaseWriter
    {
        static int DefaultLineLength = 78;
        static byte[] DASHDASH= new byte[] { (byte)'-', (byte)'-' }; 
        static byte[] CRLF = new byte[] { (byte)'\r', (byte)'\n' };
 
        byte[] boundaryBytes; 
        BufferBuilder bufferBuilder = new BufferBuilder();
        Stream contentStream; 
        bool isInContent;
        int lineLength;
        EventHandler onCloseHandler;
        Stream stream; 
        bool writeBoundary = true;
        string preface; 
 
        static AsyncCallback onWrite = new AsyncCallback(OnWrite);
 
        internal MimeWriter(Stream stream, string boundary) : this(stream, boundary, null, DefaultLineLength)
        {
        }
 
        /*
        // Consider removing. 
        internal MimeWriter(Stream stream, string boundary, int lineLength) : this(stream, boundary, null, lineLength) 
        {
        } 
        */

        /*
        // Consider removing. 
        internal MimeWriter(Stream stream, string boundary, string preface) : this(stream, boundary, preface, DefaultLineLength)
        { 
        } 
        */
 
        internal MimeWriter(Stream stream, string boundary, string preface, int lineLength)
        {
            if (stream == null)
                throw new ArgumentNullException("stream"); 

            if (boundary == null) 
                throw new ArgumentNullException("boundary"); 

            if (lineLength < 40) 
                throw new ArgumentOutOfRangeException("lineLength", lineLength, SR.GetString(SR.MailWriterLineLengthTooSmall));

            this.stream = stream;
            this.lineLength = lineLength; 
            this.onCloseHandler = new EventHandler(OnClose);
            this.boundaryBytes = Encoding.ASCII.GetBytes(boundary); 
            this.preface = preface; 
        }
 
        internal IAsyncResult BeginClose(AsyncCallback callback, object state)
        {
            MultiAsyncResult multiResult = new MultiAsyncResult(this, callback, state);
 
            Close(multiResult);
 
            multiResult.CompleteSequence(); 

            return multiResult; 
        }

        internal void EndClose(IAsyncResult result)
        { 
            MultiAsyncResult.End(result);
 
            this.stream.Close(); 
        }
 
        internal override void Close()
        {
            Close(null);
 
            this.stream.Close();
        } 
 
        void Close(MultiAsyncResult multiResult)
        { 
            this.bufferBuilder.Append(CRLF);
            this.bufferBuilder.Append(DASHDASH);
            this.bufferBuilder.Append(this.boundaryBytes);
            this.bufferBuilder.Append(DASHDASH); 
            this.bufferBuilder.Append(CRLF);
            Flush(multiResult); 
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
                throw (Exception)o;
            } 
            return (Stream)o; 
        }
 
        internal Stream GetContentStream(ContentTransferEncoding contentTransferEncoding)
        {
            if (this.isInContent)
                throw new InvalidOperationException(SR.GetString(SR.MailWriterIsInContent)); 

            this.isInContent = true; 
 
            return GetContentStream(contentTransferEncoding, null);
        } 

        internal override Stream GetContentStream()
        {
            return GetContentStream(ContentTransferEncoding.SevenBit); 
        }
 
        Stream GetContentStream(ContentTransferEncoding contentTransferEncoding, MultiAsyncResult multiResult) 
        {
            CheckBoundary(); 

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
 
            CheckBoundary();
            this.bufferBuilder.Append(name); 
            this.bufferBuilder.Append(": "); 
            WriteAndFold(value, name.Length + 2);
            this.bufferBuilder.Append(CRLF); 
        }


        internal override void WriteHeaders(NameValueCollection headers) { 
            if (headers == null)
                throw new ArgumentNullException("headers"); 
 
            foreach (string key in headers)
                WriteHeader(key,headers[key]); 
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
            if (this.contentStream != sender)
                return; // may have called WriteHeader 

            this.contentStream.Flush(); 
            this.contentStream = null; 
            this.writeBoundary = true;
 
            this.isInContent = false;
        }

        /// <summary> 
        /// Writes the boundary sequence if required.
        /// </summary> 
        void CheckBoundary() 
        {
            if (this.preface != null) 
            {
                this.bufferBuilder.Append(this.preface);
                this.preface = null;
            } 
            if (this.writeBoundary)
            { 
                this.bufferBuilder.Append(CRLF); 
                this.bufferBuilder.Append(DASHDASH);
                this.bufferBuilder.Append(this.boundaryBytes); 
                this.bufferBuilder.Append(CRLF);
                this.writeBoundary = false;
            }
        } 

        static void OnWrite(IAsyncResult result) 
        { 
            if (!result.CompletedSynchronously)
            { 
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState;
                MimeWriter thisPtr = (MimeWriter)multiResult.Context;
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
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="MimeWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.Collections; 
    using System.IO;
    using System.Text;
    using System.Collections.Specialized;
 
    /// <summary>
    /// Provides an abstraction for writing a MIME multi-part 
    /// message. 
    /// </summary>
 
    internal class MimeWriter:BaseWriter
    {
        static int DefaultLineLength = 78;
        static byte[] DASHDASH= new byte[] { (byte)'-', (byte)'-' }; 
        static byte[] CRLF = new byte[] { (byte)'\r', (byte)'\n' };
 
        byte[] boundaryBytes; 
        BufferBuilder bufferBuilder = new BufferBuilder();
        Stream contentStream; 
        bool isInContent;
        int lineLength;
        EventHandler onCloseHandler;
        Stream stream; 
        bool writeBoundary = true;
        string preface; 
 
        static AsyncCallback onWrite = new AsyncCallback(OnWrite);
 
        internal MimeWriter(Stream stream, string boundary) : this(stream, boundary, null, DefaultLineLength)
        {
        }
 
        /*
        // Consider removing. 
        internal MimeWriter(Stream stream, string boundary, int lineLength) : this(stream, boundary, null, lineLength) 
        {
        } 
        */

        /*
        // Consider removing. 
        internal MimeWriter(Stream stream, string boundary, string preface) : this(stream, boundary, preface, DefaultLineLength)
        { 
        } 
        */
 
        internal MimeWriter(Stream stream, string boundary, string preface, int lineLength)
        {
            if (stream == null)
                throw new ArgumentNullException("stream"); 

            if (boundary == null) 
                throw new ArgumentNullException("boundary"); 

            if (lineLength < 40) 
                throw new ArgumentOutOfRangeException("lineLength", lineLength, SR.GetString(SR.MailWriterLineLengthTooSmall));

            this.stream = stream;
            this.lineLength = lineLength; 
            this.onCloseHandler = new EventHandler(OnClose);
            this.boundaryBytes = Encoding.ASCII.GetBytes(boundary); 
            this.preface = preface; 
        }
 
        internal IAsyncResult BeginClose(AsyncCallback callback, object state)
        {
            MultiAsyncResult multiResult = new MultiAsyncResult(this, callback, state);
 
            Close(multiResult);
 
            multiResult.CompleteSequence(); 

            return multiResult; 
        }

        internal void EndClose(IAsyncResult result)
        { 
            MultiAsyncResult.End(result);
 
            this.stream.Close(); 
        }
 
        internal override void Close()
        {
            Close(null);
 
            this.stream.Close();
        } 
 
        void Close(MultiAsyncResult multiResult)
        { 
            this.bufferBuilder.Append(CRLF);
            this.bufferBuilder.Append(DASHDASH);
            this.bufferBuilder.Append(this.boundaryBytes);
            this.bufferBuilder.Append(DASHDASH); 
            this.bufferBuilder.Append(CRLF);
            Flush(multiResult); 
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
                throw (Exception)o;
            } 
            return (Stream)o; 
        }
 
        internal Stream GetContentStream(ContentTransferEncoding contentTransferEncoding)
        {
            if (this.isInContent)
                throw new InvalidOperationException(SR.GetString(SR.MailWriterIsInContent)); 

            this.isInContent = true; 
 
            return GetContentStream(contentTransferEncoding, null);
        } 

        internal override Stream GetContentStream()
        {
            return GetContentStream(ContentTransferEncoding.SevenBit); 
        }
 
        Stream GetContentStream(ContentTransferEncoding contentTransferEncoding, MultiAsyncResult multiResult) 
        {
            CheckBoundary(); 

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
 
            CheckBoundary();
            this.bufferBuilder.Append(name); 
            this.bufferBuilder.Append(": "); 
            WriteAndFold(value, name.Length + 2);
            this.bufferBuilder.Append(CRLF); 
        }


        internal override void WriteHeaders(NameValueCollection headers) { 
            if (headers == null)
                throw new ArgumentNullException("headers"); 
 
            foreach (string key in headers)
                WriteHeader(key,headers[key]); 
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
            if (this.contentStream != sender)
                return; // may have called WriteHeader 

            this.contentStream.Flush(); 
            this.contentStream = null; 
            this.writeBoundary = true;
 
            this.isInContent = false;
        }

        /// <summary> 
        /// Writes the boundary sequence if required.
        /// </summary> 
        void CheckBoundary() 
        {
            if (this.preface != null) 
            {
                this.bufferBuilder.Append(this.preface);
                this.preface = null;
            } 
            if (this.writeBoundary)
            { 
                this.bufferBuilder.Append(CRLF); 
                this.bufferBuilder.Append(DASHDASH);
                this.bufferBuilder.Append(this.boundaryBytes); 
                this.bufferBuilder.Append(CRLF);
                this.writeBoundary = false;
            }
        } 

        static void OnWrite(IAsyncResult result) 
        { 
            if (!result.CompletedSynchronously)
            { 
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState;
                MimeWriter thisPtr = (MimeWriter)multiResult.Context;
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
    } 
} 
