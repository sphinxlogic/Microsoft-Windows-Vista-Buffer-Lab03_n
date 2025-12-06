//------------------------------------------------------------------------------ 
// <copyright file="Base64Stream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net 
{ 
    using System;
    using System.IO; 

    internal class Base64Stream : DelegatedStream
    {
        static int DefaultLineLength = 76; 

        static byte[] base64DecodeMap = new byte[] { 
            //0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 0
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 1 
            255,255,255,255,255,255,255,255,255,255,255, 62,255,255,255, 63, // 2
             52, 53, 54, 55, 56, 57, 58, 59, 60, 61,255,255,255,255,255,255, // 3
            255,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, // 4
             15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25,255,255,255,255,255, // 5 
            255, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, // 6
             41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51,255,255,255,255,255, // 7 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 8 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 9
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // A 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // B
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // C
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // D
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // E 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // F
        }; 
 
        static byte[] base64EncodeMap = new byte[] {
             65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 
             81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 97, 98, 99,100,101,102,
            103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,
            119,120,121,122, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 43, 47,
            61 
            };
 
        int lineLength; 
        ReadStateInfo readState;
        WriteStateInfo writeState; 

        internal Base64Stream(Stream stream) : this(stream, DefaultLineLength)
        {
        } 

        internal Base64Stream(Stream stream, int lineLength) : base(stream) 
        { 
            this.lineLength = lineLength;
        } 

        internal Base64Stream() {
            this.lineLength = DefaultLineLength;
        } 

        internal Base64Stream(int lineLength) { 
            this.lineLength = lineLength; 
        }
 
        public override bool CanWrite
        {
            get
            { 
                return base.CanWrite;
            } 
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
            if (this.writeState != null && WriteState.Length > 0)
            {
                switch (WriteState.Padding)
                { 
                    case 2:
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[WriteState.LastBits]; 
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64]; 
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64];
                        break; 
                    case 1:
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[WriteState.LastBits];
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64];
                        break; 
                }
                WriteState.Padding = 0; 
                FlushInternal(); 
            }
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
 
                    while (source < end)
                    { 
                        // 

                        if (*source == '\r' || *source == '\n' || *source == '=') 
                        {
                            source++;
                            continue;
                        } 

                        byte s = base64DecodeMap[*source]; 
 
                        if (s == 255)
                            throw new FormatException(SR.GetString(SR.MailBase64InvalidCharacter)); 

                        switch (ReadState.Pos)
                        {
                            case 0: 
                                ReadState.Val = (byte)(s << 2);
                                ReadState.Pos++; 
                                break; 
                            case 1:
                                *dest++ = (byte)(ReadState.Val + (s >> 4)); 
                                ReadState.Val = (byte)(s << 4);
                                ReadState.Pos++;
                                break;
                            case 2: 
                                *dest++ = (byte)(ReadState.Val + (s >> 2));
                                ReadState.Val = (byte)(s << 6); 
                                ReadState.Pos++; 
                                break;
                            case 3: 
                                *dest++ = (byte)(ReadState.Val + s);
                                ReadState.Pos = 0;
                                break;
                        } 
                        source++;
                    } 
 
                    count = (int)(dest - start);
                } 
            }
            return count;
        }
 
        internal int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes)
        { 
            int cur = offset; 

            switch (WriteState.Padding) 
            {
                case 2:
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[WriteState.LastBits | ((buffer[cur]&0xf0)>>4)];
                    if (count == 1) 
                    {
                        WriteState.LastBits = (byte)((buffer[cur]&0x0f)<<2); 
                        WriteState.Padding = 1; 
                        return cur - offset;
                    } 
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur]&0x0f)<<2) | ((buffer[cur+1]&0xc0)>>6)];
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur+1]&0x3f)];
                    cur+=2;
                    count-=2; 
                    WriteState.Padding = 0;
                    WriteState.CurrentLineLength += 2; 
                    break; 
                case 1:
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[WriteState.LastBits | ((buffer[cur]&0xc0)>>6)]; 
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur]&0x3f)];
                    cur++;
                    count--;
                    WriteState.Padding = 0; 
                    WriteState.CurrentLineLength ++;
                    break; 
            } 

            int calcLength = cur + (count - (count%3)); 

            //Convert three bytes at a time to base64 notation.  This will consume 4 chars.
            for (; cur < calcLength; cur+=3)
            { 
                if (lineLength != -1 && WriteState.CurrentLineLength + 4 > lineLength - 2)
                { 
                    WriteState.Buffer[WriteState.Length++] = (byte)'\r'; 
                    WriteState.Buffer[WriteState.Length++] = (byte)'\n';
                    WriteState.CurrentLineLength = 0; 
                }

                if (WriteState.Length + 4 > WriteState.Buffer.Length)
                    return cur - offset; 

                WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur]&0xfc)>>2]; 
                WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur]&0x03)<<4) | ((buffer[cur+1]&0xf0)>>4)]; 
                WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur+1]&0x0f)<<2) | ((buffer[cur+2]&0xc0)>>6)];
                WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur+2]&0x3f)]; 
                WriteState.CurrentLineLength += 4;
            }

            cur = calcLength; //Where we left off before 

            if (WriteState.Length + 4 > WriteState.Buffer.Length) 
                return cur - offset; 

            if (lineLength != -1 && WriteState.CurrentLineLength + 4 > lineLength) 
            {
                WriteState.Buffer[WriteState.Length++] = (byte)'\r';
                WriteState.Buffer[WriteState.Length++] = (byte)'\n';
                WriteState.CurrentLineLength = 0; 
            }
 
            switch(count%3) 
            {
                case 2: //One character padding needed 
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur]&0xFC)>>2];
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur]&0x03)<<4)|((buffer[cur+1]&0xf0)>>4)];
                    if (dontDeferFinalBytes) {
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur+1]&0x0f)<<2)]; 
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64];
                        WriteState.Padding = 0; 
                        WriteState.CurrentLineLength += 4; 
                    }
                    else{ 
                        WriteState.LastBits = (byte)((buffer[cur+1]&0x0F)<<2);
                        WriteState.Padding = 1;
                        WriteState.CurrentLineLength += 2;
                    } 
                    cur += 2;
                    break; 
 
                case 1: // Two character padding needed
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur]&0xFC)>>2]; 
                    if (dontDeferFinalBytes) {
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(byte)((buffer[cur]&0x03)<<4)];
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64];
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64]; 
                        WriteState.Padding = 0;
                        WriteState.CurrentLineLength+=4; 
                    } 
                    else{
                        WriteState.LastBits = (byte)((buffer[cur]&0x03)<<4); 
                        WriteState.Padding = 2;
                        WriteState.CurrentLineLength ++;
                    }
                    cur++; 
                    break;
            } 
 
            System.Diagnostics.Debug.Assert(cur - offset == count);
 
            return cur - offset;
        }

        public override int EndRead(IAsyncResult asyncResult) 
        {
            if (asyncResult == null) 
                throw new ArgumentNullException("asyncResult"); 

            int read = ReadAsyncResult.End(asyncResult); 
            return read;
        }

        public override void EndWrite(IAsyncResult asyncResult) 
        {
            if (asyncResult == null) 
                throw new ArgumentNullException("asyncResult"); 

            WriteAsyncResult.End(asyncResult); 
        }

        public override void Flush()
        { 
            if (this.writeState != null && WriteState.Length > 0)
            { 
                FlushInternal(); 
            }
            base.Flush(); 
        }

        private void FlushInternal()
        { 
            base.Write(WriteState.Buffer, 0, WriteState.Length);
            WriteState.Length = 0; 
        } 

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
                    return 0;

                // while decoding, we may end up not having
                // any bytes to return pending additional data 
                // from the underlying stream.
                read = DecodeBytes(buffer, offset, read); 
                if (read > 0) 
                    return read;
            } 
        }

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
                written += EncodeBytes(buffer, offset + written, count - written, false);
                if (written < count) 
                    FlushInternal();
                else
                    break;
            } 
        }
 
        class ReadAsyncResult : LazyAsyncResult 
        {
            Base64Stream parent; 
            byte[] buffer;
            int offset;
            int count;
            int read; 

            static AsyncCallback onRead = new AsyncCallback(OnRead); 
 
            internal ReadAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null,state,callback)
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
 
            internal void Read()
            { 
                for (;;) 
                {
                    IAsyncResult result = this.parent.BaseStream.BeginRead(this.buffer, this.offset, this.count, onRead, this); 
                    if (!result.CompletedSynchronously || CompleteRead(result))
                        break;
                }
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
                        if (thisPtr.IsCompleted) 
                            throw;
                        thisPtr.InvokeCallback(e);
                    }
                    catch { 
                        if (thisPtr.IsCompleted)
                            throw; 
                        thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                    }
                } 
            }

            internal static int End(IAsyncResult result)
            { 
                ReadAsyncResult thisPtr = (ReadAsyncResult)result;
                thisPtr.InternalWaitForCompletion(); 
                return thisPtr.read; 
            }
        } 

        class WriteAsyncResult : LazyAsyncResult
        {
            Base64Stream parent; 
            byte[] buffer;
            int offset; 
            int count; 
            static AsyncCallback onWrite = new AsyncCallback(OnWrite);
            int written; 

            internal WriteAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null, state, callback)
            {
                this.parent = parent; 
                this.buffer = buffer;
                this.offset = offset; 
                this.count = count; 
            }
 
            internal void Write()
            {
                for (;;)
                { 
                    this.written += this.parent.EncodeBytes(this.buffer, this.offset + this.written, this.count - this.written, false);
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

            void CompleteWrite(IAsyncResult result)
            { 
                this.parent.BaseStream.EndWrite(result);
                this.parent.WriteState.Length = 0; 
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
                        if (thisPtr.IsCompleted)
                            throw; 
                        thisPtr.InvokeCallback(e);
                    } 
                    catch { 
                        if (thisPtr.IsCompleted)
                            throw; 
                        thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException)));
                    }
                }
            } 

            internal static void End(IAsyncResult result) 
            { 
                WriteAsyncResult thisPtr = (WriteAsyncResult)result;
                thisPtr.InternalWaitForCompletion(); 
                System.Diagnostics.Debug.Assert(thisPtr.written == thisPtr.count);
            }
        }
 
        class ReadStateInfo
        { 
            byte val; 
            byte pos;
 
            internal byte Val
            {
                get { return this.val; }
                set { this.val = value; } 
            }
 
            internal byte Pos 
            {
                get { return this.pos; } 
                set { this.pos = value; }
            }
        }
 
        internal class WriteStateInfo
        { 
            byte[] outBuffer; 
            int outLength;
            int padding; 
            byte lastBits;
            int currentLineLength;

            internal WriteStateInfo(int bufferSize) 
            {
                this.outBuffer = new byte[bufferSize]; 
            } 

            internal byte[] Buffer 
            {
                get { return this.outBuffer; }
            }
 
            internal int CurrentLineLength
            { 
                get { return this.currentLineLength; } 
                set { this.currentLineLength = value; }
 
            }

            internal int Length
            { 
                get { return this.outLength; }
                set { this.outLength = value; } 
            } 

            internal int Padding 
            {
                get { return this.padding; }
                set { this.padding = value; }
            } 

            internal byte LastBits 
            { 
                get { return this.lastBits; }
                set { this.lastBits = value; } 
            }
        }
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="Base64Stream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net 
{ 
    using System;
    using System.IO; 

    internal class Base64Stream : DelegatedStream
    {
        static int DefaultLineLength = 76; 

        static byte[] base64DecodeMap = new byte[] { 
            //0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 0
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 1 
            255,255,255,255,255,255,255,255,255,255,255, 62,255,255,255, 63, // 2
             52, 53, 54, 55, 56, 57, 58, 59, 60, 61,255,255,255,255,255,255, // 3
            255,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, // 4
             15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25,255,255,255,255,255, // 5 
            255, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, // 6
             41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51,255,255,255,255,255, // 7 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 8 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 9
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // A 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // B
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // C
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // D
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // E 
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // F
        }; 
 
        static byte[] base64EncodeMap = new byte[] {
             65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 
             81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 97, 98, 99,100,101,102,
            103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,
            119,120,121,122, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 43, 47,
            61 
            };
 
        int lineLength; 
        ReadStateInfo readState;
        WriteStateInfo writeState; 

        internal Base64Stream(Stream stream) : this(stream, DefaultLineLength)
        {
        } 

        internal Base64Stream(Stream stream, int lineLength) : base(stream) 
        { 
            this.lineLength = lineLength;
        } 

        internal Base64Stream() {
            this.lineLength = DefaultLineLength;
        } 

        internal Base64Stream(int lineLength) { 
            this.lineLength = lineLength; 
        }
 
        public override bool CanWrite
        {
            get
            { 
                return base.CanWrite;
            } 
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
            if (this.writeState != null && WriteState.Length > 0)
            {
                switch (WriteState.Padding)
                { 
                    case 2:
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[WriteState.LastBits]; 
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64]; 
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64];
                        break; 
                    case 1:
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[WriteState.LastBits];
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64];
                        break; 
                }
                WriteState.Padding = 0; 
                FlushInternal(); 
            }
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
 
                    while (source < end)
                    { 
                        // 

                        if (*source == '\r' || *source == '\n' || *source == '=') 
                        {
                            source++;
                            continue;
                        } 

                        byte s = base64DecodeMap[*source]; 
 
                        if (s == 255)
                            throw new FormatException(SR.GetString(SR.MailBase64InvalidCharacter)); 

                        switch (ReadState.Pos)
                        {
                            case 0: 
                                ReadState.Val = (byte)(s << 2);
                                ReadState.Pos++; 
                                break; 
                            case 1:
                                *dest++ = (byte)(ReadState.Val + (s >> 4)); 
                                ReadState.Val = (byte)(s << 4);
                                ReadState.Pos++;
                                break;
                            case 2: 
                                *dest++ = (byte)(ReadState.Val + (s >> 2));
                                ReadState.Val = (byte)(s << 6); 
                                ReadState.Pos++; 
                                break;
                            case 3: 
                                *dest++ = (byte)(ReadState.Val + s);
                                ReadState.Pos = 0;
                                break;
                        } 
                        source++;
                    } 
 
                    count = (int)(dest - start);
                } 
            }
            return count;
        }
 
        internal int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes)
        { 
            int cur = offset; 

            switch (WriteState.Padding) 
            {
                case 2:
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[WriteState.LastBits | ((buffer[cur]&0xf0)>>4)];
                    if (count == 1) 
                    {
                        WriteState.LastBits = (byte)((buffer[cur]&0x0f)<<2); 
                        WriteState.Padding = 1; 
                        return cur - offset;
                    } 
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur]&0x0f)<<2) | ((buffer[cur+1]&0xc0)>>6)];
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur+1]&0x3f)];
                    cur+=2;
                    count-=2; 
                    WriteState.Padding = 0;
                    WriteState.CurrentLineLength += 2; 
                    break; 
                case 1:
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[WriteState.LastBits | ((buffer[cur]&0xc0)>>6)]; 
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur]&0x3f)];
                    cur++;
                    count--;
                    WriteState.Padding = 0; 
                    WriteState.CurrentLineLength ++;
                    break; 
            } 

            int calcLength = cur + (count - (count%3)); 

            //Convert three bytes at a time to base64 notation.  This will consume 4 chars.
            for (; cur < calcLength; cur+=3)
            { 
                if (lineLength != -1 && WriteState.CurrentLineLength + 4 > lineLength - 2)
                { 
                    WriteState.Buffer[WriteState.Length++] = (byte)'\r'; 
                    WriteState.Buffer[WriteState.Length++] = (byte)'\n';
                    WriteState.CurrentLineLength = 0; 
                }

                if (WriteState.Length + 4 > WriteState.Buffer.Length)
                    return cur - offset; 

                WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur]&0xfc)>>2]; 
                WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur]&0x03)<<4) | ((buffer[cur+1]&0xf0)>>4)]; 
                WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur+1]&0x0f)<<2) | ((buffer[cur+2]&0xc0)>>6)];
                WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur+2]&0x3f)]; 
                WriteState.CurrentLineLength += 4;
            }

            cur = calcLength; //Where we left off before 

            if (WriteState.Length + 4 > WriteState.Buffer.Length) 
                return cur - offset; 

            if (lineLength != -1 && WriteState.CurrentLineLength + 4 > lineLength) 
            {
                WriteState.Buffer[WriteState.Length++] = (byte)'\r';
                WriteState.Buffer[WriteState.Length++] = (byte)'\n';
                WriteState.CurrentLineLength = 0; 
            }
 
            switch(count%3) 
            {
                case 2: //One character padding needed 
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur]&0xFC)>>2];
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur]&0x03)<<4)|((buffer[cur+1]&0xf0)>>4)];
                    if (dontDeferFinalBytes) {
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[((buffer[cur+1]&0x0f)<<2)]; 
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64];
                        WriteState.Padding = 0; 
                        WriteState.CurrentLineLength += 4; 
                    }
                    else{ 
                        WriteState.LastBits = (byte)((buffer[cur+1]&0x0F)<<2);
                        WriteState.Padding = 1;
                        WriteState.CurrentLineLength += 2;
                    } 
                    cur += 2;
                    break; 
 
                case 1: // Two character padding needed
                    WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(buffer[cur]&0xFC)>>2]; 
                    if (dontDeferFinalBytes) {
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[(byte)((buffer[cur]&0x03)<<4)];
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64];
                        WriteState.Buffer[WriteState.Length++] = base64EncodeMap[64]; 
                        WriteState.Padding = 0;
                        WriteState.CurrentLineLength+=4; 
                    } 
                    else{
                        WriteState.LastBits = (byte)((buffer[cur]&0x03)<<4); 
                        WriteState.Padding = 2;
                        WriteState.CurrentLineLength ++;
                    }
                    cur++; 
                    break;
            } 
 
            System.Diagnostics.Debug.Assert(cur - offset == count);
 
            return cur - offset;
        }

        public override int EndRead(IAsyncResult asyncResult) 
        {
            if (asyncResult == null) 
                throw new ArgumentNullException("asyncResult"); 

            int read = ReadAsyncResult.End(asyncResult); 
            return read;
        }

        public override void EndWrite(IAsyncResult asyncResult) 
        {
            if (asyncResult == null) 
                throw new ArgumentNullException("asyncResult"); 

            WriteAsyncResult.End(asyncResult); 
        }

        public override void Flush()
        { 
            if (this.writeState != null && WriteState.Length > 0)
            { 
                FlushInternal(); 
            }
            base.Flush(); 
        }

        private void FlushInternal()
        { 
            base.Write(WriteState.Buffer, 0, WriteState.Length);
            WriteState.Length = 0; 
        } 

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
                    return 0;

                // while decoding, we may end up not having
                // any bytes to return pending additional data 
                // from the underlying stream.
                read = DecodeBytes(buffer, offset, read); 
                if (read > 0) 
                    return read;
            } 
        }

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
                written += EncodeBytes(buffer, offset + written, count - written, false);
                if (written < count) 
                    FlushInternal();
                else
                    break;
            } 
        }
 
        class ReadAsyncResult : LazyAsyncResult 
        {
            Base64Stream parent; 
            byte[] buffer;
            int offset;
            int count;
            int read; 

            static AsyncCallback onRead = new AsyncCallback(OnRead); 
 
            internal ReadAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null,state,callback)
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
 
            internal void Read()
            { 
                for (;;) 
                {
                    IAsyncResult result = this.parent.BaseStream.BeginRead(this.buffer, this.offset, this.count, onRead, this); 
                    if (!result.CompletedSynchronously || CompleteRead(result))
                        break;
                }
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
                        if (thisPtr.IsCompleted) 
                            throw;
                        thisPtr.InvokeCallback(e);
                    }
                    catch { 
                        if (thisPtr.IsCompleted)
                            throw; 
                        thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
                    }
                } 
            }

            internal static int End(IAsyncResult result)
            { 
                ReadAsyncResult thisPtr = (ReadAsyncResult)result;
                thisPtr.InternalWaitForCompletion(); 
                return thisPtr.read; 
            }
        } 

        class WriteAsyncResult : LazyAsyncResult
        {
            Base64Stream parent; 
            byte[] buffer;
            int offset; 
            int count; 
            static AsyncCallback onWrite = new AsyncCallback(OnWrite);
            int written; 

            internal WriteAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null, state, callback)
            {
                this.parent = parent; 
                this.buffer = buffer;
                this.offset = offset; 
                this.count = count; 
            }
 
            internal void Write()
            {
                for (;;)
                { 
                    this.written += this.parent.EncodeBytes(this.buffer, this.offset + this.written, this.count - this.written, false);
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

            void CompleteWrite(IAsyncResult result)
            { 
                this.parent.BaseStream.EndWrite(result);
                this.parent.WriteState.Length = 0; 
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
                        if (thisPtr.IsCompleted)
                            throw; 
                        thisPtr.InvokeCallback(e);
                    } 
                    catch { 
                        if (thisPtr.IsCompleted)
                            throw; 
                        thisPtr.InvokeCallback(new Exception(SR.GetString(SR.net_nonClsCompliantException)));
                    }
                }
            } 

            internal static void End(IAsyncResult result) 
            { 
                WriteAsyncResult thisPtr = (WriteAsyncResult)result;
                thisPtr.InternalWaitForCompletion(); 
                System.Diagnostics.Debug.Assert(thisPtr.written == thisPtr.count);
            }
        }
 
        class ReadStateInfo
        { 
            byte val; 
            byte pos;
 
            internal byte Val
            {
                get { return this.val; }
                set { this.val = value; } 
            }
 
            internal byte Pos 
            {
                get { return this.pos; } 
                set { this.pos = value; }
            }
        }
 
        internal class WriteStateInfo
        { 
            byte[] outBuffer; 
            int outLength;
            int padding; 
            byte lastBits;
            int currentLineLength;

            internal WriteStateInfo(int bufferSize) 
            {
                this.outBuffer = new byte[bufferSize]; 
            } 

            internal byte[] Buffer 
            {
                get { return this.outBuffer; }
            }
 
            internal int CurrentLineLength
            { 
                get { return this.currentLineLength; } 
                set { this.currentLineLength = value; }
 
            }

            internal int Length
            { 
                get { return this.outLength; }
                set { this.outLength = value; } 
            } 

            internal int Padding 
            {
                get { return this.padding; }
                set { this.padding = value; }
            } 

            internal byte LastBits 
            { 
                get { return this.lastBits; }
                set { this.lastBits = value; } 
            }
        }
    }
} 
