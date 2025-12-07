//------------------------------------------------------------------------------ 
// <copyright file="SevenBitStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.IO; 

    /// <summary>
    /// This stream validates outgoing bytes to be within the
    /// acceptible range of 0 - 127.  Writes will throw if a 
    /// value > 127 is found.
    /// </summary> 
    internal class SevenBitStream : DelegatedStream 
    {
        /// <summary> 
        /// ctor.
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        internal SevenBitStream(Stream stream) : base(stream) 
        {
        } 
 
        /// <summary>
        /// Writes the specified content to the underlying stream 
        /// </summary>
        /// <param name="buffer">Buffer to write</param>
        /// <param name="offset">Offset within buffer to start writing</param>
        /// <param name="count">Count of bytes to write</param> 
        /// <param name="callback">Callback to call when write completes</param>
        /// <param name="state">State to pass to callback</param> 
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) 
        {
            if (buffer == null) 
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException("offset"); 

            if (offset + count > buffer.Length) 
                throw new ArgumentOutOfRangeException("count"); 

            CheckBytes(buffer, offset, count); 
            IAsyncResult result = base.BeginWrite(buffer, offset, count, callback, state);
            return result;
        }
 
        /// <summary>
        /// Writes the specified content to the underlying stream 
        /// </summary> 
        /// <param name="buffer">Buffer to write</param>
        /// <param name="offset">Offset within buffer to start writing</param> 
        /// <param name="count">Count of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) 
                throw new ArgumentNullException("buffer");
 
            if (offset < 0 || offset >= buffer.Length) 
                throw new ArgumentOutOfRangeException("offset");
 
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            CheckBytes(buffer, offset, count); 
            base.Write(buffer, offset, count);
        } 
 
        // helper methods
 
        /// <summary>
        /// Checks the data in the buffer for bytes > 127.
        /// </summary>
        /// <param name="buffer">Buffer containing data</param> 
        /// <param name="offset">Offset within buffer to start checking</param>
        /// <param name="count">Count of bytes to check</param> 
        void CheckBytes(byte[] buffer, int offset, int count) 
        {
            for (int i = count; i < offset + count; i++) 
            {
                if (buffer[i] > 127)
                    throw new FormatException(SR.GetString(SR.Mail7BitStreamInvalidCharacter));
            } 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="SevenBitStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.IO; 

    /// <summary>
    /// This stream validates outgoing bytes to be within the
    /// acceptible range of 0 - 127.  Writes will throw if a 
    /// value > 127 is found.
    /// </summary> 
    internal class SevenBitStream : DelegatedStream 
    {
        /// <summary> 
        /// ctor.
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        internal SevenBitStream(Stream stream) : base(stream) 
        {
        } 
 
        /// <summary>
        /// Writes the specified content to the underlying stream 
        /// </summary>
        /// <param name="buffer">Buffer to write</param>
        /// <param name="offset">Offset within buffer to start writing</param>
        /// <param name="count">Count of bytes to write</param> 
        /// <param name="callback">Callback to call when write completes</param>
        /// <param name="state">State to pass to callback</param> 
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) 
        {
            if (buffer == null) 
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException("offset"); 

            if (offset + count > buffer.Length) 
                throw new ArgumentOutOfRangeException("count"); 

            CheckBytes(buffer, offset, count); 
            IAsyncResult result = base.BeginWrite(buffer, offset, count, callback, state);
            return result;
        }
 
        /// <summary>
        /// Writes the specified content to the underlying stream 
        /// </summary> 
        /// <param name="buffer">Buffer to write</param>
        /// <param name="offset">Offset within buffer to start writing</param> 
        /// <param name="count">Count of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) 
                throw new ArgumentNullException("buffer");
 
            if (offset < 0 || offset >= buffer.Length) 
                throw new ArgumentOutOfRangeException("offset");
 
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            CheckBytes(buffer, offset, count); 
            base.Write(buffer, offset, count);
        } 
 
        // helper methods
 
        /// <summary>
        /// Checks the data in the buffer for bytes > 127.
        /// </summary>
        /// <param name="buffer">Buffer containing data</param> 
        /// <param name="offset">Offset within buffer to start checking</param>
        /// <param name="count">Count of bytes to check</param> 
        void CheckBytes(byte[] buffer, int offset, int count) 
        {
            for (int i = count; i < offset + count; i++) 
            {
                if (buffer[i] > 127)
                    throw new FormatException(SR.GetString(SR.Mail7BitStreamInvalidCharacter));
            } 
        }
    } 
} 
