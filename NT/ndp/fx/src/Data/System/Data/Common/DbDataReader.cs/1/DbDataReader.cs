//------------------------------------------------------------------------------ 
// <copyright file="DbDataReader.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 

#if WINFSInternalOnly 
    internal 
#else
    public 
#endif
    abstract class DbDataReader : MarshalByRefObject, IDataReader, IEnumerable { // V1.2.3300
        protected DbDataReader() : base() {
        } 

        abstract public int Depth { 
            get; 
        }
 
        abstract public int FieldCount {
            get;
        }
 
        abstract public bool HasRows {
            get; 
        } 

        abstract public bool IsClosed { 
            get;
        }

        abstract public int RecordsAffected { 
            get;
        } 
 
        virtual public int VisibleFieldCount {
            // NOTE: This is virtual because not all providers may choose to support 
            //       this property, since it was added in Whidbey
            get {
                return FieldCount;
            } 
        }
 
        abstract public object this [ int ordinal ] { 
            get;
        } 

        abstract public object this [ string name ] {
            get;
        } 

        abstract public void Close(); 
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ]
        public void Dispose() {
            Dispose(true);
        } 

        protected virtual void Dispose(bool disposing) { 
            if (disposing) { 
                Close();
            } 
        }

        abstract public string GetDataTypeName(int ordinal);
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ] 
        abstract public IEnumerator GetEnumerator();
 
        abstract public Type GetFieldType(int ordinal);

        abstract public string GetName(int ordinal);
 
        abstract public int GetOrdinal(string name);
 
        abstract public DataTable GetSchemaTable(); 

        abstract public bool GetBoolean(int ordinal); 

        abstract public byte GetByte(int ordinal);

        abstract public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length); 

        abstract public char GetChar(int ordinal); 
 
        abstract public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length);
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        public DbDataReader GetData(int ordinal) { 
            return GetDbDataReader(ordinal);
        } 
 
        IDataReader IDataRecord.GetData(int ordinal) {
            return GetDbDataReader(ordinal); 
        }

        virtual protected DbDataReader GetDbDataReader(int ordinal) {
            // NOTE: This method is virtual because we're required to implement 
            //       it however most providers won't support it. Only the OLE DB
            //       provider supports it right now, and they can override it. 
            throw ADP.NotSupported(); 
        }
 
        abstract public DateTime GetDateTime(int ordinal);

        abstract public Decimal GetDecimal(int ordinal);
 
        abstract public double GetDouble(int ordinal);
 
        abstract public float GetFloat(int ordinal); 

        abstract public Guid GetGuid(int ordinal); 

        abstract public Int16 GetInt16(int ordinal);

        abstract public Int32 GetInt32(int ordinal); 

        abstract public Int64 GetInt64(int ordinal); 
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ]
        virtual public Type GetProviderSpecificFieldType(int ordinal) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey. 
            return GetFieldType(ordinal);
        } 
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ]
        virtual public Object GetProviderSpecificValue(int ordinal) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey 
            return GetValue(ordinal);
        } 
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ]
        virtual public int GetProviderSpecificValues(object[] values) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey 
            return GetValues(values);
        } 
 
        abstract public String GetString(int ordinal);
 
        abstract public Object GetValue(int ordinal);

        abstract public int GetValues(object[] values);
 
        abstract public bool IsDBNull(int ordinal);
 
        abstract public bool NextResult(); 

        abstract public bool Read(); 
    }

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbDataReader.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 

#if WINFSInternalOnly 
    internal 
#else
    public 
#endif
    abstract class DbDataReader : MarshalByRefObject, IDataReader, IEnumerable { // V1.2.3300
        protected DbDataReader() : base() {
        } 

        abstract public int Depth { 
            get; 
        }
 
        abstract public int FieldCount {
            get;
        }
 
        abstract public bool HasRows {
            get; 
        } 

        abstract public bool IsClosed { 
            get;
        }

        abstract public int RecordsAffected { 
            get;
        } 
 
        virtual public int VisibleFieldCount {
            // NOTE: This is virtual because not all providers may choose to support 
            //       this property, since it was added in Whidbey
            get {
                return FieldCount;
            } 
        }
 
        abstract public object this [ int ordinal ] { 
            get;
        } 

        abstract public object this [ string name ] {
            get;
        } 

        abstract public void Close(); 
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ]
        public void Dispose() {
            Dispose(true);
        } 

        protected virtual void Dispose(bool disposing) { 
            if (disposing) { 
                Close();
            } 
        }

        abstract public string GetDataTypeName(int ordinal);
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ] 
        abstract public IEnumerator GetEnumerator();
 
        abstract public Type GetFieldType(int ordinal);

        abstract public string GetName(int ordinal);
 
        abstract public int GetOrdinal(string name);
 
        abstract public DataTable GetSchemaTable(); 

        abstract public bool GetBoolean(int ordinal); 

        abstract public byte GetByte(int ordinal);

        abstract public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length); 

        abstract public char GetChar(int ordinal); 
 
        abstract public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length);
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        public DbDataReader GetData(int ordinal) { 
            return GetDbDataReader(ordinal);
        } 
 
        IDataReader IDataRecord.GetData(int ordinal) {
            return GetDbDataReader(ordinal); 
        }

        virtual protected DbDataReader GetDbDataReader(int ordinal) {
            // NOTE: This method is virtual because we're required to implement 
            //       it however most providers won't support it. Only the OLE DB
            //       provider supports it right now, and they can override it. 
            throw ADP.NotSupported(); 
        }
 
        abstract public DateTime GetDateTime(int ordinal);

        abstract public Decimal GetDecimal(int ordinal);
 
        abstract public double GetDouble(int ordinal);
 
        abstract public float GetFloat(int ordinal); 

        abstract public Guid GetGuid(int ordinal); 

        abstract public Int16 GetInt16(int ordinal);

        abstract public Int32 GetInt32(int ordinal); 

        abstract public Int64 GetInt64(int ordinal); 
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ]
        virtual public Type GetProviderSpecificFieldType(int ordinal) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey. 
            return GetFieldType(ordinal);
        } 
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ]
        virtual public Object GetProviderSpecificValue(int ordinal) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey 
            return GetValue(ordinal);
        } 
 
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ]
        virtual public int GetProviderSpecificValues(object[] values) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey 
            return GetValues(values);
        } 
 
        abstract public String GetString(int ordinal);
 
        abstract public Object GetValue(int ordinal);

        abstract public int GetValues(object[] values);
 
        abstract public bool IsDBNull(int ordinal);
 
        abstract public bool NextResult(); 

        abstract public bool Read(); 
    }

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
