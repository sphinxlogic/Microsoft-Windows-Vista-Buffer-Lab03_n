//------------------------------------------------------------------------------ 
// <copyright file="QuotedPrintableStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.IO; 

    /// <summary>
    /// This stream performs in-place decoding of quoted-printable
    /// encoded streams.  Encoding requires copying into a separate 
    /// buffer as the data being encoded will most likely grow.
    /// Encoding and decoding is done transparently to the caller. 
    /// </summary> 
    internal class QuotedPrintableStream : DelegatedStream
    { 
        bool encodeCRLF;
        static int DefaultLineLength = 76;

        static byte[] hexDecodeMap = new byte[] {// 0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 0
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 1 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 2 
                                                    0,  1,  2,  3,  4,  5,  6,  7,  8,  9,255,255,255,255,255,255, // 3
                                                  255, 10, 11, 12, 13, 14, 15,255,255,255,255,255,255,255,255,255, // 4 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 5
                                                  255, 10, 11, 12, 13, 14, 15,255,255,255,255,255,255,255,255,255, // 6
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 7
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 8 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 9
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // A 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // B 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // C
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // D 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // E
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // F
        };
 
        static byte[] hexEncodeMap = new byte[] {  48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 65, 66, 67, 68, 69, 70};
 
        int lineLength; 
        ReadStateInfo readState;
        WriteStateInfo writeState; 

        /// <summary>
        /// ctor.
        /// </summary> 
        /// <param name="stream">Underlying stream</param>
        /// <param name="lineLength">Preferred maximum line-length for writes</param> 
        internal QuotedPrintableStream(Stream stream, int lineLength) : base(stream) 
        {
            if (lineLength < 0) 
                throw new ArgumentOutOfRangeException("lineLength");

            this.lineLength = lineLength;
        } 

        /* 
        // Consider removing. 
        /// <summary>
        /// ctor. 
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        internal QuotedPrintableStream(Stream stream) : this(stream, DefaultLineLength)
        { 
        }
        */ 
 
        internal QuotedPrintableStream(Stream stream,bool encodeCRLF) : this(stream, DefaultLineLength)
        { 
            this.encodeCRLF = encodeCRLF;
        }

 
        internal QuotedPrintableStream() {
            this.lineLength = DefaultLineLength; 
        } 

 
        internal QuotedPrintableStream(int lineLength) {
            this.lineLength = lineLength;
        }
 

        ReadStateInfo ReadState 
        { 
            get
            { 
                if (this.readState == null)
                    this.readState = new ReadStateInfo();
                return this.readState;
            } 
        }
 
        internal WriteStateInfo WriteState 
        {
            get 
            {
                if (this.writeState == null)
                    this.writeState = new WriteStateInfo(1024);
                return this.writeState; 
            }
        } 
 
        /*
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) 
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
 
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset"); 
 
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count"); 

            ReadAsyncResult result = new ReadAsyncResult(this, buffer, offset, count, callback, state);
            result.Read();
            return result; 
        }
        */ 
 
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        { 
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset > buffer.Length) 
                throw new ArgumentOutOfRangeException("offset");
 
            if (offset + count > buffer.Length) 
                throw new ArgumentOutOfRangeException("count");
 
            WriteAsyncResult result = new WriteAsyncResult(this, buffer, offset, count, callback, state);
            result.Write();
            return result;
        } 

        public override void Close() 
        { 
            FlushInternal();
            base.Close(); 
        }

        internal int DecodeBytes(byte[] buffer, int offset, int count)
        { 
            unsafe
            { 
                fixed (byte* pBuffer = buffer) 
                {
                    byte* start = pBuffer + offset; 
                    byte* source = start;
                    byte* dest = start;
                    byte* end = start + count;
 
                    // if the last read ended in a partially decoded
                    // sequence, pick up where we left off. 
                    if (ReadState.IsEscaped) 
                    {
                        // this will be -1 if the previous read ended 
                        // with an escape character.
                        if (ReadState.Byte == -1)
                        {
                            // if we only read one byte from the underlying 
                            // stream, we'll need to save the byte and
                            // ask for more. 
                            if (count == 1) 
                            {
                                ReadState.Byte = *source; 
                                return 0;
                            }

                            // '=\r\n' means a soft (aka. invisible) CRLF sequence... 
                            if (source[0] != '\r' || source[1] != '\n')
                            { 
                                byte b1 = hexDecodeMap[source[0]]; 
                                byte b2 = hexDecodeMap[source[1]];
                                if (b1 == 255) 
                                    throw new FormatException(SR.GetString(SR.InvalidHexDigit, b1));
                                if (b2 == 255)
                                    throw new FormatException(SR.GetString(SR.InvalidHexDigit, b2));
 
                                *dest++ = (byte)((b1 << 4) + b2);
                            } 
 
                            source += 2;
                        } 
                        else
                        {
                            // '=\r\n' means a soft (aka. invisible) CRLF sequence...
                            if (ReadState.Byte != '\r' || *source != '\n') 
                            {
                                byte b1 = hexDecodeMap[ReadState.Byte]; 
                                byte b2 = hexDecodeMap[*source]; 
                                if (b1 == 255)
                                    throw new FormatException(SR.GetString(SR.InvalidHexDigit, b1)); 
                                if (b2 == 255)
                                    throw new FormatException(SR.GetString(SR.InvalidHexDigit, b2));
                                *dest++ = (byte)((b1 << 4) + b2);
                            } 
                            source++;
                        } 
                        // reset state for next read. 
                        ReadState.IsEscaped = false;
                        ReadState.Byte = -1; 
                    }

                    // Here's where most of the decoding takes place.
                    // We'll loop around until we've inspected all the 
                    // bytes read.
                    while (source < end) 
                    { 
                        // if the source is not an escape character, then
                        // just copy as-is. 
                        if (*source != '=')
                        {
                            *dest++ = *source++;
                        } 
                        else
                        { 
                            // determine where we are relative to the end 
                            // of the data.  If we don't have enough data to
                            // decode the escape sequence, save off what we 
                            // have and continue the decoding in the next
                            // read.  Otherwise, decode the data and copy
                            // into dest.
                            switch (end - source) 
                            {
                                case 2: 
                                    ReadState.Byte = source[1]; 
                                    goto case 1;
                                case 1: 
                                    ReadState.IsEscaped = true;
                                    goto EndWhile;
                                default:
                                    if (source[1] != '\r' || source[2] != '\n') 
                                    {
                                        byte b1 = hexDecodeMap[source[1]]; 
                                        byte b2 = hexDecodeMap[source[2]]; 
                                        if (b1 == 255)
                                            throw new FormatException(SR.GetString(SR.InvalidHexDigit, b1)); 
                                        if (b2 == 255)
                                            throw new FormatException(SR.GetString(SR.InvalidHexDigit, b2));

                                        *dest++ = (byte)((b1 << 4) + b2); 
                                    }
                                    source += 3; 
                                    break; 
                            }
                        } 
                    }
                EndWhile:
                    count = (int)(dest - start);
                } 
            }
            return count; 
        } 

 
        internal int EncodeBytes(byte[] buffer, int offset, int count)
        {
            int cur = offset;
            for (; cur < count + offset; cur++) 
            {
                //only fold if we're before a whitespace 
                if (lineLength != -1 && WriteState.CurrentLineLength + 5 >= this.lineLength && (buffer[cur] == ' ' || 
                    buffer[cur] == '\t' || buffer[cur] == '\r' || buffer[cur] == '\n'))
                { 
                    if (WriteState.Buffer.Length - WriteState.Length < 3)
                        return cur - offset;

                    WriteState.CurrentLineLength = 0; 
                    WriteState.Buffer[WriteState.Length++] = (byte)'=';
                    WriteState.Buffer[WriteState.Length++] = (byte)'\r'; 
                    WriteState.Buffer[WriteState.Length++] = (byte)'\n'; 
                }
 
                //need to dot stuff  - rfc  2821 4.5.2 Transparency
                if(WriteState.CurrentLineLength == 0 && buffer[cur] == '.'){
                    WriteState.Buffer[WriteState.Length++] = (byte)'.';
                } 

                if (buffer[cur] == '\r' && cur + 1 < count + offset && buffer[cur+1] == '\n') 
                { 
                    if (WriteState.Buffer.Length - WriteState.Length < (encodeCRLF ? 6 : 2))
                        return cur - offset; 
                    cur++;

                    if(encodeCRLF){
                        WriteState.Buffer[WriteState.Length++] = (byte)'='; 
                        WriteState.Buffer[WriteState.Length++] = (byte)'0';
                        WriteState.Buffer[WriteState.Length++] = (byte)'D'; 
                        WriteState.Buffer[WriteState.Length++] = (byte)'='; 
                        WriteState.Buffer[WriteState.Length++] = (byte)'0';
                        WriteState.Buffer[WriteState.Length++] = (byte)'A'; 
                        WriteState.CurrentLineLength += 6;
                    }
                    else{
                        WriteState.Buffer[WriteState.Length++] = (byte)'\r'; 
                        WriteState.Buffer[WriteState.Length++] = (byte)'\n';
                        WriteState.CurrentLineLength = 0; 
                    } 
                }
                else if ((buffer[cur] < 32 && buffer[cur] != '\t') || 
                    buffer[cur] == '=' ||
                    buffer[cur] > 126)
                {
                    if (WriteState.Buffer.Length - WriteState.Length < 3) 
                        return cur - offset;
 
                    WriteState.CurrentLineLength += 3; 

                    WriteState.Buffer[WriteState.Length++] = (byte)'='; 
                    WriteState.Buffer[WriteState.Length++] = hexEncodeMap[buffer[cur] >> 4];
                    WriteState.Buffer[WriteState.Length++] = hexEncodeMap[buffer[cur] & 0xF];
                }
                else 
                {
                    if (WriteState.Buffer.Length - WriteState.Length < 1) 
                        return cur - offset; 

                    WriteState.CurrentLineLength++; 
                    WriteState.Buffer[WriteState.Length++] = buffer[cur];
                }
            }
            return cur - offset; 
        }
 
        /* 
        public override int EndRead(IAsyncResult asyncResult)
        { 
            int read = ReadAsyncResult.End(asyncResult);
            return read;
        }
        */ 

        public override void EndWrite(IAsyncResult asyncResult) 
        { 
            WriteAsyncResult.End(asyncResult);
        } 

        public override void Flush()
        {
            FlushInternal(); 
            base.Flush();
        } 
 
        void FlushInternal()
        { 
            if (this.writeState != null && this.writeState.Length > 0)
            {
                base.Write(WriteState.Buffer, 0, WriteState.Length);
                WriteState.Length = 0; 
            }
        } 
 
        /// <summary>
        /// Reads data from the underlying stream into the supplied 
        /// buffer and does decoding in-place.
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset within buffer to start writing</param> 
        /// <param name="count">Maximum count of bytes to read</param>
        /// <returns>Number of decoded bytes read, 0 if EOS</returns> 
 
        /*
        public override int Read(byte[] buffer, int offset, int count) 
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
 
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset"); 
 
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count"); 

            for (;;)
            {
                // read data from the underlying stream 
                int read = base.Read(buffer, offset, count);
 
                // if the underlying stream returns 0 then there 
                // is no more data - ust return 0.
                if (read == 0) 
                {
                    return 0;
                }
 
                // while decoding, we may end up not having
                // any bytes to return pending additional data 
                // from the underlying stream. 
                read = DecodeBytes(buffer, offset, read);
                if (read > 0) 
                {
                    return read;
                }
            } 
        }
        */ 
 
        public override void Write(byte[] buffer, int offset, int count)
        { 
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset > buffer.Length) 
                throw new ArgumentOutOfRangeException("offset");
 
            if (offset + count > buffer.Length) 
                throw new ArgumentOutOfRangeException("count");
 
            int written = 0;
            for (;;)
            {
                written += EncodeBytes(buffer, offset + written, count - written); 
                if (written < count)
                    FlushInternal(); 
                else 
                    break;
            } 
        }

        /*
        class ReadAsyncResult : LazyAsyncResult 
        {
            QuotedPrintableStream parent; 
            byte[] buffer; 
            int offset;
            int count; 
            int read;
            static AsyncCallback onRead = new AsyncCallback(OnRead);

            internal ReadAsyncResult(QuotedPrintableStream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null, state, callback) 
            {
                this.parent = parent; 
                this.buffer = buffer; 
                this.offset = offset;
                this.count = count; 
            }

            bool CompleteRead(IAsyncResult result)
            { 
                this.read = this.parent.BaseStream.EndRead(result);
 
                // if the underlying stream returns 0 then there 
                // is no more data - ust return 0.
                if (read == 0) 
                {
                    InvokeCallback();
                    return true;
                } 

                // while decoding, we may end up not having 
                // any bytes to return pending additional data 
                // from the underlying stream.
                this.read = this.parent.DecodeBytes(this.buffer, this.offset, this.read); 
                if (this.read > 0)
                {
                    InvokeCallback();
                    return true; 
                }
 
                return false; 
            }
 
            static void OnRead(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                { 
                    ReadAsyncResult thisPtr = (ReadAsyncResult)result.AsyncState;
                    try 
                    { 
                        if (!thisPtr.CompleteRead(result))
                            thisPtr.Read(); 
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

            internal void Read()
            { 
                for (;;)
                { 
                    IAsyncResult result = this.parent.BaseStream.BeginRead(this.buffer, this.offset, this.count, onRead, this); 
                    if (!result.CompletedSynchronously || CompleteRead(result))
                        break; 
                }
            }

            internal static int End(IAsyncResult result) 
            {
                ReadAsyncResult thisPtr = (ReadAsyncResult)result; 
                thisPtr.InternalWaitForCompletion(); 
                return thisPtr.read;
            } 
        }
                      */

        class ReadStateInfo 
        {
            bool isEscaped = false; 
            short b1 = -1; 

            internal bool IsEscaped 
            {
                get { return this.isEscaped; }
                set { this.isEscaped = value; }
            } 

            internal short Byte 
            { 
                get { return this.b1; }
                set { this.b1 = value; } 
            }
        }

 
        internal class WriteStateInfo
        { 
            int currentLineLength = 0; 
            byte[] buffer;
            int length; 

            internal WriteStateInfo(int bufferSize)
            {
                this.buffer = new byte[bufferSize]; 
            }
 
            internal byte[] Buffer 
            {
                get { return this.buffer; } 
            }

            internal int CurrentLineLength
            { 
                get { return this.currentLineLength; }
                set { this.currentLineLength = value; } 
            } 

            internal int Length 
            {
                get { return this.length; }
                set { this.length = value; }
            } 
        }
 
        class WriteAsyncResult : LazyAsyncResult 
        {
            QuotedPrintableStream parent; 
            byte[] buffer;
            int offset;
            int count;
            static AsyncCallback onWrite = new AsyncCallback(OnWrite); 
            int written;
 
            internal WriteAsyncResult(QuotedPrintableStream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null, state, callback) 
            {
                this.parent = parent; 
                this.buffer = buffer;
                this.offset = offset;
                this.count = count;
            } 

            void CompleteWrite(IAsyncResult result) 
            { 
                this.parent.BaseStream.EndWrite(result);
                this.parent.WriteState.Length = 0; 
            }

            internal static void End(IAsyncResult result)
            { 
                WriteAsyncResult thisPtr = (WriteAsyncResult)result;
                thisPtr.InternalWaitForCompletion(); 
                System.Diagnostics.Debug.Assert(thisPtr.written == thisPtr.count); 
            }
 
            static void OnWrite(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                { 
                    WriteAsyncResult thisPtr = (WriteAsyncResult)result.AsyncState;
                    try 
                    { 
                        thisPtr.CompleteWrite(result);
                        thisPtr.Write(); 
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

            internal void Write()
            { 
                for (;;)
                { 
                    this.written += this.parent.EncodeBytes(this.buffer, this.offset + this.written, this.count - this.written); 
                    if (this.written < this.count)
                    { 
                        IAsyncResult result = this.parent.BaseStream.BeginWrite(this.parent.WriteState.Buffer, 0, this.parent.WriteState.Length, onWrite, this);
                        if (!result.CompletedSynchronously)
                            break;
                        CompleteWrite(result); 
                    }
                    else 
                    { 
                        InvokeCallback();
                        break; 
                    }
                }
            }
        } 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="QuotedPrintableStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.IO; 

    /// <summary>
    /// This stream performs in-place decoding of quoted-printable
    /// encoded streams.  Encoding requires copying into a separate 
    /// buffer as the data being encoded will most likely grow.
    /// Encoding and decoding is done transparently to the caller. 
    /// </summary> 
    internal class QuotedPrintableStream : DelegatedStream
    { 
        bool encodeCRLF;
        static int DefaultLineLength = 76;

        static byte[] hexDecodeMap = new byte[] {// 0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 0
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 1 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 2 
                                                    0,  1,  2,  3,  4,  5,  6,  7,  8,  9,255,255,255,255,255,255, // 3
                                                  255, 10, 11, 12, 13, 14, 15,255,255,255,255,255,255,255,255,255, // 4 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 5
                                                  255, 10, 11, 12, 13, 14, 15,255,255,255,255,255,255,255,255,255, // 6
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 7
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 8 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 9
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // A 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // B 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // C
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // D 
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // E
                                                  255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // F
        };
 
        static byte[] hexEncodeMap = new byte[] {  48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 65, 66, 67, 68, 69, 70};
 
        int lineLength; 
        ReadStateInfo readState;
        WriteStateInfo writeState; 

        /// <summary>
        /// ctor.
        /// </summary> 
        /// <param name="stream">Underlying stream</param>
        /// <param name="lineLength">Preferred maximum line-length for writes</param> 
        internal QuotedPrintableStream(Stream stream, int lineLength) : base(stream) 
        {
            if (lineLength < 0) 
                throw new ArgumentOutOfRangeException("lineLength");

            this.lineLength = lineLength;
        } 

        /* 
        // Consider removing. 
        /// <summary>
        /// ctor. 
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        internal QuotedPrintableStream(Stream stream) : this(stream, DefaultLineLength)
        { 
        }
        */ 
 
        internal QuotedPrintableStream(Stream stream,bool encodeCRLF) : this(stream, DefaultLineLength)
        { 
            this.encodeCRLF = encodeCRLF;
        }

 
        internal QuotedPrintableStream() {
            this.lineLength = DefaultLineLength; 
        } 

 
        internal QuotedPrintableStream(int lineLength) {
            this.lineLength = lineLength;
        }
 

        ReadStateInfo ReadState 
        { 
            get
            { 
                if (this.readState == null)
                    this.readState = new ReadStateInfo();
                return this.readState;
            } 
        }
 
        internal WriteStateInfo WriteState 
        {
            get 
            {
                if (this.writeState == null)
                    this.writeState = new WriteStateInfo(1024);
                return this.writeState; 
            }
        } 
 
        /*
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) 
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
 
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset"); 
 
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count"); 

            ReadAsyncResult result = new ReadAsyncResult(this, buffer, offset, count, callback, state);
            result.Read();
            return result; 
        }
        */ 
 
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        { 
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset > buffer.Length) 
                throw new ArgumentOutOfRangeException("offset");
 
            if (offset + count > buffer.Length) 
                throw new ArgumentOutOfRangeException("count");
 
            WriteAsyncResult result = new WriteAsyncResult(this, buffer, offset, count, callback, state);
            result.Write();
            return result;
        } 

        public override void Close() 
        { 
            FlushInternal();
            base.Close(); 
        }

        internal int DecodeBytes(byte[] buffer, int offset, int count)
        { 
            unsafe
            { 
                fixed (byte* pBuffer = buffer) 
                {
                    byte* start = pBuffer + offset; 
                    byte* source = start;
                    byte* dest = start;
                    byte* end = start + count;
 
                    // if the last read ended in a partially decoded
                    // sequence, pick up where we left off. 
                    if (ReadState.IsEscaped) 
                    {
                        // this will be -1 if the previous read ended 
                        // with an escape character.
                        if (ReadState.Byte == -1)
                        {
                            // if we only read one byte from the underlying 
                            // stream, we'll need to save the byte and
                            // ask for more. 
                            if (count == 1) 
                            {
                                ReadState.Byte = *source; 
                                return 0;
                            }

                            // '=\r\n' means a soft (aka. invisible) CRLF sequence... 
                            if (source[0] != '\r' || source[1] != '\n')
                            { 
                                byte b1 = hexDecodeMap[source[0]]; 
                                byte b2 = hexDecodeMap[source[1]];
                                if (b1 == 255) 
                                    throw new FormatException(SR.GetString(SR.InvalidHexDigit, b1));
                                if (b2 == 255)
                                    throw new FormatException(SR.GetString(SR.InvalidHexDigit, b2));
 
                                *dest++ = (byte)((b1 << 4) + b2);
                            } 
 
                            source += 2;
                        } 
                        else
                        {
                            // '=\r\n' means a soft (aka. invisible) CRLF sequence...
                            if (ReadState.Byte != '\r' || *source != '\n') 
                            {
                                byte b1 = hexDecodeMap[ReadState.Byte]; 
                                byte b2 = hexDecodeMap[*source]; 
                                if (b1 == 255)
                                    throw new FormatException(SR.GetString(SR.InvalidHexDigit, b1)); 
                                if (b2 == 255)
                                    throw new FormatException(SR.GetString(SR.InvalidHexDigit, b2));
                                *dest++ = (byte)((b1 << 4) + b2);
                            } 
                            source++;
                        } 
                        // reset state for next read. 
                        ReadState.IsEscaped = false;
                        ReadState.Byte = -1; 
                    }

                    // Here's where most of the decoding takes place.
                    // We'll loop around until we've inspected all the 
                    // bytes read.
                    while (source < end) 
                    { 
                        // if the source is not an escape character, then
                        // just copy as-is. 
                        if (*source != '=')
                        {
                            *dest++ = *source++;
                        } 
                        else
                        { 
                            // determine where we are relative to the end 
                            // of the data.  If we don't have enough data to
                            // decode the escape sequence, save off what we 
                            // have and continue the decoding in the next
                            // read.  Otherwise, decode the data and copy
                            // into dest.
                            switch (end - source) 
                            {
                                case 2: 
                                    ReadState.Byte = source[1]; 
                                    goto case 1;
                                case 1: 
                                    ReadState.IsEscaped = true;
                                    goto EndWhile;
                                default:
                                    if (source[1] != '\r' || source[2] != '\n') 
                                    {
                                        byte b1 = hexDecodeMap[source[1]]; 
                                        byte b2 = hexDecodeMap[source[2]]; 
                                        if (b1 == 255)
                                            throw new FormatException(SR.GetString(SR.InvalidHexDigit, b1)); 
                                        if (b2 == 255)
                                            throw new FormatException(SR.GetString(SR.InvalidHexDigit, b2));

                                        *dest++ = (byte)((b1 << 4) + b2); 
                                    }
                                    source += 3; 
                                    break; 
                            }
                        } 
                    }
                EndWhile:
                    count = (int)(dest - start);
                } 
            }
            return count; 
        } 

 
        internal int EncodeBytes(byte[] buffer, int offset, int count)
        {
            int cur = offset;
            for (; cur < count + offset; cur++) 
            {
                //only fold if we're before a whitespace 
                if (lineLength != -1 && WriteState.CurrentLineLength + 5 >= this.lineLength && (buffer[cur] == ' ' || 
                    buffer[cur] == '\t' || buffer[cur] == '\r' || buffer[cur] == '\n'))
                { 
                    if (WriteState.Buffer.Length - WriteState.Length < 3)
                        return cur - offset;

                    WriteState.CurrentLineLength = 0; 
                    WriteState.Buffer[WriteState.Length++] = (byte)'=';
                    WriteState.Buffer[WriteState.Length++] = (byte)'\r'; 
                    WriteState.Buffer[WriteState.Length++] = (byte)'\n'; 
                }
 
                //need to dot stuff  - rfc  2821 4.5.2 Transparency
                if(WriteState.CurrentLineLength == 0 && buffer[cur] == '.'){
                    WriteState.Buffer[WriteState.Length++] = (byte)'.';
                } 

                if (buffer[cur] == '\r' && cur + 1 < count + offset && buffer[cur+1] == '\n') 
                { 
                    if (WriteState.Buffer.Length - WriteState.Length < (encodeCRLF ? 6 : 2))
                        return cur - offset; 
                    cur++;

                    if(encodeCRLF){
                        WriteState.Buffer[WriteState.Length++] = (byte)'='; 
                        WriteState.Buffer[WriteState.Length++] = (byte)'0';
                        WriteState.Buffer[WriteState.Length++] = (byte)'D'; 
                        WriteState.Buffer[WriteState.Length++] = (byte)'='; 
                        WriteState.Buffer[WriteState.Length++] = (byte)'0';
                        WriteState.Buffer[WriteState.Length++] = (byte)'A'; 
                        WriteState.CurrentLineLength += 6;
                    }
                    else{
                        WriteState.Buffer[WriteState.Length++] = (byte)'\r'; 
                        WriteState.Buffer[WriteState.Length++] = (byte)'\n';
                        WriteState.CurrentLineLength = 0; 
                    } 
                }
                else if ((buffer[cur] < 32 && buffer[cur] != '\t') || 
                    buffer[cur] == '=' ||
                    buffer[cur] > 126)
                {
                    if (WriteState.Buffer.Length - WriteState.Length < 3) 
                        return cur - offset;
 
                    WriteState.CurrentLineLength += 3; 

                    WriteState.Buffer[WriteState.Length++] = (byte)'='; 
                    WriteState.Buffer[WriteState.Length++] = hexEncodeMap[buffer[cur] >> 4];
                    WriteState.Buffer[WriteState.Length++] = hexEncodeMap[buffer[cur] & 0xF];
                }
                else 
                {
                    if (WriteState.Buffer.Length - WriteState.Length < 1) 
                        return cur - offset; 

                    WriteState.CurrentLineLength++; 
                    WriteState.Buffer[WriteState.Length++] = buffer[cur];
                }
            }
            return cur - offset; 
        }
 
        /* 
        public override int EndRead(IAsyncResult asyncResult)
        { 
            int read = ReadAsyncResult.End(asyncResult);
            return read;
        }
        */ 

        public override void EndWrite(IAsyncResult asyncResult) 
        { 
            WriteAsyncResult.End(asyncResult);
        } 

        public override void Flush()
        {
            FlushInternal(); 
            base.Flush();
        } 
 
        void FlushInternal()
        { 
            if (this.writeState != null && this.writeState.Length > 0)
            {
                base.Write(WriteState.Buffer, 0, WriteState.Length);
                WriteState.Length = 0; 
            }
        } 
 
        /// <summary>
        /// Reads data from the underlying stream into the supplied 
        /// buffer and does decoding in-place.
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset within buffer to start writing</param> 
        /// <param name="count">Maximum count of bytes to read</param>
        /// <returns>Number of decoded bytes read, 0 if EOS</returns> 
 
        /*
        public override int Read(byte[] buffer, int offset, int count) 
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
 
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset"); 
 
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count"); 

            for (;;)
            {
                // read data from the underlying stream 
                int read = base.Read(buffer, offset, count);
 
                // if the underlying stream returns 0 then there 
                // is no more data - ust return 0.
                if (read == 0) 
                {
                    return 0;
                }
 
                // while decoding, we may end up not having
                // any bytes to return pending additional data 
                // from the underlying stream. 
                read = DecodeBytes(buffer, offset, read);
                if (read > 0) 
                {
                    return read;
                }
            } 
        }
        */ 
 
        public override void Write(byte[] buffer, int offset, int count)
        { 
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset > buffer.Length) 
                throw new ArgumentOutOfRangeException("offset");
 
            if (offset + count > buffer.Length) 
                throw new ArgumentOutOfRangeException("count");
 
            int written = 0;
            for (;;)
            {
                written += EncodeBytes(buffer, offset + written, count - written); 
                if (written < count)
                    FlushInternal(); 
                else 
                    break;
            } 
        }

        /*
        class ReadAsyncResult : LazyAsyncResult 
        {
            QuotedPrintableStream parent; 
            byte[] buffer; 
            int offset;
            int count; 
            int read;
            static AsyncCallback onRead = new AsyncCallback(OnRead);

            internal ReadAsyncResult(QuotedPrintableStream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null, state, callback) 
            {
                this.parent = parent; 
                this.buffer = buffer; 
                this.offset = offset;
                this.count = count; 
            }

            bool CompleteRead(IAsyncResult result)
            { 
                this.read = this.parent.BaseStream.EndRead(result);
 
                // if the underlying stream returns 0 then there 
                // is no more data - ust return 0.
                if (read == 0) 
                {
                    InvokeCallback();
                    return true;
                } 

                // while decoding, we may end up not having 
                // any bytes to return pending additional data 
                // from the underlying stream.
                this.read = this.parent.DecodeBytes(this.buffer, this.offset, this.read); 
                if (this.read > 0)
                {
                    InvokeCallback();
                    return true; 
                }
 
                return false; 
            }
 
            static void OnRead(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                { 
                    ReadAsyncResult thisPtr = (ReadAsyncResult)result.AsyncState;
                    try 
                    { 
                        if (!thisPtr.CompleteRead(result))
                            thisPtr.Read(); 
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

            internal void Read()
            { 
                for (;;)
                { 
                    IAsyncResult result = this.parent.BaseStream.BeginRead(this.buffer, this.offset, this.count, onRead, this); 
                    if (!result.CompletedSynchronously || CompleteRead(result))
                        break; 
                }
            }

            internal static int End(IAsyncResult result) 
            {
                ReadAsyncResult thisPtr = (ReadAsyncResult)result; 
                thisPtr.InternalWaitForCompletion(); 
                return thisPtr.read;
            } 
        }
                      */

        class ReadStateInfo 
        {
            bool isEscaped = false; 
            short b1 = -1; 

            internal bool IsEscaped 
            {
                get { return this.isEscaped; }
                set { this.isEscaped = value; }
            } 

            internal short Byte 
            { 
                get { return this.b1; }
                set { this.b1 = value; } 
            }
        }

 
        internal class WriteStateInfo
        { 
            int currentLineLength = 0; 
            byte[] buffer;
            int length; 

            internal WriteStateInfo(int bufferSize)
            {
                this.buffer = new byte[bufferSize]; 
            }
 
            internal byte[] Buffer 
            {
                get { return this.buffer; } 
            }

            internal int CurrentLineLength
            { 
                get { return this.currentLineLength; }
                set { this.currentLineLength = value; } 
            } 

            internal int Length 
            {
                get { return this.length; }
                set { this.length = value; }
            } 
        }
 
        class WriteAsyncResult : LazyAsyncResult 
        {
            QuotedPrintableStream parent; 
            byte[] buffer;
            int offset;
            int count;
            static AsyncCallback onWrite = new AsyncCallback(OnWrite); 
            int written;
 
            internal WriteAsyncResult(QuotedPrintableStream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null, state, callback) 
            {
                this.parent = parent; 
                this.buffer = buffer;
                this.offset = offset;
                this.count = count;
            } 

            void CompleteWrite(IAsyncResult result) 
            { 
                this.parent.BaseStream.EndWrite(result);
                this.parent.WriteState.Length = 0; 
            }

            internal static void End(IAsyncResult result)
            { 
                WriteAsyncResult thisPtr = (WriteAsyncResult)result;
                thisPtr.InternalWaitForCompletion(); 
                System.Diagnostics.Debug.Assert(thisPtr.written == thisPtr.count); 
            }
 
            static void OnWrite(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                { 
                    WriteAsyncResult thisPtr = (WriteAsyncResult)result.AsyncState;
                    try 
                    { 
                        thisPtr.CompleteWrite(result);
                        thisPtr.Write(); 
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

            internal void Write()
            { 
                for (;;)
                { 
                    this.written += this.parent.EncodeBytes(this.buffer, this.offset + this.written, this.count - this.written); 
                    if (this.written < this.count)
                    { 
                        IAsyncResult result = this.parent.BaseStream.BeginWrite(this.parent.WriteState.Buffer, 0, this.parent.WriteState.Length, onWrite, this);
                        if (!result.CompletedSynchronously)
                            break;
                        CompleteWrite(result); 
                    }
                    else 
                    { 
                        InvokeCallback();
                        break; 
                    }
                }
            }
        } 
    }
} 
