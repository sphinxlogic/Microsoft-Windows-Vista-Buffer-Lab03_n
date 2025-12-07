//------------------------------------------------------------------------------ 
// <copyright file="SqlCachedBuffer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization; 
    using System.Text;
    using System.Xml; 
    using System.Data.SqlTypes;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Reflection; 

    // Caches the bytes returned from partial length prefixed datatypes, like XML 
    sealed internal class SqlCachedBuffer : System.Data.SqlTypes.INullable{ 

       ArrayList    _cachedBytes; 
 	const int _maxChunkSize = 2048;	// Arbitrary value for chunk size. Revisit this later for better perf

        // Reads off from the network buffer and caches bytes. Only reads one column value in the current row.
        internal SqlCachedBuffer(SqlMetaDataPriv metadata, TdsParser parser, TdsParserStateObject stateObj ) { 
            int cb = 0;
            ulong  plplength; 
            byte[] byteArr; 

            Debug.Assert(_cachedBytes == null, "Cache is already filled!"); 
            _cachedBytes = new ArrayList();

            // the very first length is already read.
            plplength = parser.PlpBytesLeft(stateObj);; 
            // For now we  only handle Plp data from the parser directly.
            Debug.Assert(metadata.metaType.IsPlp, "SqlCachedBuffer call on a non-plp data"); 
            do { 
                if (plplength == 0)
                    break; 
                do {
                    cb = (plplength > (ulong) _maxChunkSize) ?  _maxChunkSize : (int)plplength ;
                    byteArr = new byte[cb];
                    cb = stateObj.ReadPlpBytes(ref byteArr, 0, cb); 
                    Debug.Assert(cb == byteArr.Length);
                    if (_cachedBytes.Count == 0) { 
                        // Add the Byte order mark if needed if we read the first array 
                        AddByteOrderMark(byteArr);
                    } 
                    _cachedBytes.Add(byteArr);
                    plplength -= (ulong)cb;
                } while (plplength > 0);
                plplength = parser.PlpBytesLeft(stateObj); 
            } while (plplength > 0);
            Debug.Assert(stateObj._longlen == 0 && stateObj._longlenleft == 0); 
        } 

 
        // Reads off from a reader and caches bytes. Only reads the first column of each row.
        internal SqlCachedBuffer(SqlDataReader dataRdr,  bool cacheAllRows) {
            byte[] byteArr;
            long cb; 
            long bytesRead;
 
            Debug.Assert(_cachedBytes == null, "Cache is already filled!"); 
            _cachedBytes = new ArrayList();
 
            do {
                 if (dataRdr.Read()) {
                    do {
                        bytesRead = 0; 
                        cb = dataRdr.GetBytesInternal(0, 0, null, 0, 0);
                        //InternalGetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) 
                        // Get no. of bytes 
                        while (cb != 0) {
                            int cbread = (cb > (long)_maxChunkSize || (cb < 0) ) ? _maxChunkSize : (int) cb; 
                            byteArr = new byte[cbread];
                            cbread = (int) dataRdr.GetBytesInternal(0, bytesRead, byteArr, 0, cbread);
				// If we got less bytes than we requested, we hit the end							
                            if((cbread > 0) && (cbread < byteArr.Length)) { 
					byte[] newByteArr = new byte[cbread];
					Buffer.BlockCopy(byteArr, 0, newByteArr, 0, cbread); 
 					byteArr = newByteArr; 
                            }
                            if (_cachedBytes.Count == 0) { 
                                // Add the Byte order mark if needed if we read the first array
                                AddByteOrderMark(byteArr);
                            }
                            if (cbread == 0) 
                                break;
                            _cachedBytes.Add(byteArr); 
                            cb -= (long)cbread; 
                            bytesRead += cbread;
                            if (cb == 0) { 
                                cb = dataRdr.GetBytesInternal(0, 0, null, 0, 0);
                            }
                        }
                    }while(cacheAllRows && dataRdr.Read()); 
                }
            } while (cacheAllRows && dataRdr.NextResult()); 
        } 

        private void AddByteOrderMark(byte[] byteArr ) { 

            int bom = 0xfeff;
            // Need to find out if we should add byte order mark or not.
            // We need to add this if we are getting ntext xml, not if we are getting binary xml 
            // Binary Xml always begins with the bytes 0xDF and 0xFF
            // If we aren't getting these, then we are getting unicode xml 
            if ((byteArr.Length >= 2 ) && (byteArr[0] == 0xDF) && (byteArr[1] == 0xFF)){ 
                bom = 0;
            } 
            if (bom != 0) {
                byte[]  b = new byte[2];
                b[0] = (byte)bom;
                bom >>= 8; 
                b[1] = (byte)bom;
                Debug.Assert(_cachedBytes.Count == 0); 
                _cachedBytes.Add(b); 
            }
            return; 
        }


        private SqlCachedBuffer() { 
            // For constructing Null
        } 
 
        public  static readonly SqlCachedBuffer Null = new SqlCachedBuffer();
 
        internal ArrayList  CachedBytes {
            get {
                return _cachedBytes;
            } 
        }
 
        override public string ToString() { 
            if (IsNull)
                throw new SqlNullValueException(); 

            if (_cachedBytes.Count == 0) {
                return String.Empty;
            } 
            SqlCachedStream fragment = new SqlCachedStream(this);
            SqlXml   sxml = new SqlXml((Stream)fragment); 
            return sxml.Value; 
        }
 
        internal SqlString ToSqlString() {
            if (IsNull)
                return SqlString.Null;
            string str = ToString(); 
            return new SqlString(str);
        } 
 
        internal SqlXml ToSqlXml() {
            SqlCachedStream fragment = new SqlCachedStream(this); 
            SqlXml  sx = new SqlXml((Stream)fragment);
            return sx;
        }
 
        internal XmlReader ToXmlReader() {
            SqlCachedStream fragment = new SqlCachedStream(this); 
            //XmlTextReader xr = new XmlTextReader(fragment, XmlNodeType.Element, null); 
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment; 

            // Call internal XmlReader.CreateSqlReader from System.Xml.
            // Signature: internal static XmlReader CreateSqlReader(Stream input, XmlReaderSettings settings, XmlParserContext inputContext);
            MethodInfo createSqlReaderMethodInfo = typeof(System.Xml.XmlReader).GetMethod("CreateSqlReader", BindingFlags.Static | BindingFlags.NonPublic); 
            object[] args = new object[3] { (Stream)fragment, readerSettings, null };
            XmlReader xr; 
 
            new System.Security.Permissions.ReflectionPermission(System.Security.Permissions.ReflectionPermissionFlag.MemberAccess).Assert();
            try { 
                xr = (XmlReader)createSqlReaderMethodInfo.Invoke(null, args);
            }
            finally {
                System.Security.Permissions.ReflectionPermission.RevertAssert(); 
            }
            return xr; 
        } 

        public bool IsNull { 
            get {
                return (_cachedBytes == null) ? true : false ;
            }
        } 

    } 
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlCachedBuffer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization; 
    using System.Text;
    using System.Xml; 
    using System.Data.SqlTypes;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Reflection; 

    // Caches the bytes returned from partial length prefixed datatypes, like XML 
    sealed internal class SqlCachedBuffer : System.Data.SqlTypes.INullable{ 

       ArrayList    _cachedBytes; 
 	const int _maxChunkSize = 2048;	// Arbitrary value for chunk size. Revisit this later for better perf

        // Reads off from the network buffer and caches bytes. Only reads one column value in the current row.
        internal SqlCachedBuffer(SqlMetaDataPriv metadata, TdsParser parser, TdsParserStateObject stateObj ) { 
            int cb = 0;
            ulong  plplength; 
            byte[] byteArr; 

            Debug.Assert(_cachedBytes == null, "Cache is already filled!"); 
            _cachedBytes = new ArrayList();

            // the very first length is already read.
            plplength = parser.PlpBytesLeft(stateObj);; 
            // For now we  only handle Plp data from the parser directly.
            Debug.Assert(metadata.metaType.IsPlp, "SqlCachedBuffer call on a non-plp data"); 
            do { 
                if (plplength == 0)
                    break; 
                do {
                    cb = (plplength > (ulong) _maxChunkSize) ?  _maxChunkSize : (int)plplength ;
                    byteArr = new byte[cb];
                    cb = stateObj.ReadPlpBytes(ref byteArr, 0, cb); 
                    Debug.Assert(cb == byteArr.Length);
                    if (_cachedBytes.Count == 0) { 
                        // Add the Byte order mark if needed if we read the first array 
                        AddByteOrderMark(byteArr);
                    } 
                    _cachedBytes.Add(byteArr);
                    plplength -= (ulong)cb;
                } while (plplength > 0);
                plplength = parser.PlpBytesLeft(stateObj); 
            } while (plplength > 0);
            Debug.Assert(stateObj._longlen == 0 && stateObj._longlenleft == 0); 
        } 

 
        // Reads off from a reader and caches bytes. Only reads the first column of each row.
        internal SqlCachedBuffer(SqlDataReader dataRdr,  bool cacheAllRows) {
            byte[] byteArr;
            long cb; 
            long bytesRead;
 
            Debug.Assert(_cachedBytes == null, "Cache is already filled!"); 
            _cachedBytes = new ArrayList();
 
            do {
                 if (dataRdr.Read()) {
                    do {
                        bytesRead = 0; 
                        cb = dataRdr.GetBytesInternal(0, 0, null, 0, 0);
                        //InternalGetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) 
                        // Get no. of bytes 
                        while (cb != 0) {
                            int cbread = (cb > (long)_maxChunkSize || (cb < 0) ) ? _maxChunkSize : (int) cb; 
                            byteArr = new byte[cbread];
                            cbread = (int) dataRdr.GetBytesInternal(0, bytesRead, byteArr, 0, cbread);
				// If we got less bytes than we requested, we hit the end							
                            if((cbread > 0) && (cbread < byteArr.Length)) { 
					byte[] newByteArr = new byte[cbread];
					Buffer.BlockCopy(byteArr, 0, newByteArr, 0, cbread); 
 					byteArr = newByteArr; 
                            }
                            if (_cachedBytes.Count == 0) { 
                                // Add the Byte order mark if needed if we read the first array
                                AddByteOrderMark(byteArr);
                            }
                            if (cbread == 0) 
                                break;
                            _cachedBytes.Add(byteArr); 
                            cb -= (long)cbread; 
                            bytesRead += cbread;
                            if (cb == 0) { 
                                cb = dataRdr.GetBytesInternal(0, 0, null, 0, 0);
                            }
                        }
                    }while(cacheAllRows && dataRdr.Read()); 
                }
            } while (cacheAllRows && dataRdr.NextResult()); 
        } 

        private void AddByteOrderMark(byte[] byteArr ) { 

            int bom = 0xfeff;
            // Need to find out if we should add byte order mark or not.
            // We need to add this if we are getting ntext xml, not if we are getting binary xml 
            // Binary Xml always begins with the bytes 0xDF and 0xFF
            // If we aren't getting these, then we are getting unicode xml 
            if ((byteArr.Length >= 2 ) && (byteArr[0] == 0xDF) && (byteArr[1] == 0xFF)){ 
                bom = 0;
            } 
            if (bom != 0) {
                byte[]  b = new byte[2];
                b[0] = (byte)bom;
                bom >>= 8; 
                b[1] = (byte)bom;
                Debug.Assert(_cachedBytes.Count == 0); 
                _cachedBytes.Add(b); 
            }
            return; 
        }


        private SqlCachedBuffer() { 
            // For constructing Null
        } 
 
        public  static readonly SqlCachedBuffer Null = new SqlCachedBuffer();
 
        internal ArrayList  CachedBytes {
            get {
                return _cachedBytes;
            } 
        }
 
        override public string ToString() { 
            if (IsNull)
                throw new SqlNullValueException(); 

            if (_cachedBytes.Count == 0) {
                return String.Empty;
            } 
            SqlCachedStream fragment = new SqlCachedStream(this);
            SqlXml   sxml = new SqlXml((Stream)fragment); 
            return sxml.Value; 
        }
 
        internal SqlString ToSqlString() {
            if (IsNull)
                return SqlString.Null;
            string str = ToString(); 
            return new SqlString(str);
        } 
 
        internal SqlXml ToSqlXml() {
            SqlCachedStream fragment = new SqlCachedStream(this); 
            SqlXml  sx = new SqlXml((Stream)fragment);
            return sx;
        }
 
        internal XmlReader ToXmlReader() {
            SqlCachedStream fragment = new SqlCachedStream(this); 
            //XmlTextReader xr = new XmlTextReader(fragment, XmlNodeType.Element, null); 
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment; 

            // Call internal XmlReader.CreateSqlReader from System.Xml.
            // Signature: internal static XmlReader CreateSqlReader(Stream input, XmlReaderSettings settings, XmlParserContext inputContext);
            MethodInfo createSqlReaderMethodInfo = typeof(System.Xml.XmlReader).GetMethod("CreateSqlReader", BindingFlags.Static | BindingFlags.NonPublic); 
            object[] args = new object[3] { (Stream)fragment, readerSettings, null };
            XmlReader xr; 
 
            new System.Security.Permissions.ReflectionPermission(System.Security.Permissions.ReflectionPermissionFlag.MemberAccess).Assert();
            try { 
                xr = (XmlReader)createSqlReaderMethodInfo.Invoke(null, args);
            }
            finally {
                System.Security.Permissions.ReflectionPermission.RevertAssert(); 
            }
            return xr; 
        } 

        public bool IsNull { 
            get {
                return (_cachedBytes == null) ? true : false ;
            }
        } 

    } 
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
