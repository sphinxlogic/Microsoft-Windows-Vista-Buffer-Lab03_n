//------------------------------------------------------------------------------ 
// <copyright file="DataStorage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.Common { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.SqlTypes; 
    using System.Diagnostics;
    using System.Xml; 
    using System.Xml.Serialization; 

    internal enum StorageType { 
        Empty       = TypeCode.Empty, // 0
        Object      = TypeCode.Object,
        DBNull      = TypeCode.DBNull,
        Boolean     = TypeCode.Boolean, 
        Char        = TypeCode.Char,
        SByte       = TypeCode.SByte, 
        Byte        = TypeCode.Byte, 
        Int16       = TypeCode.Int16,
        UInt16      = TypeCode.UInt16, 
        Int32       = TypeCode.Int32,
        UInt32      = TypeCode.UInt32,
        Int64       = TypeCode.Int64,
        UInt64      = TypeCode.UInt64, 
        Single      = TypeCode.Single,
        Double      = TypeCode.Double, 
        Decimal     = TypeCode.Decimal, // 15 
        DateTime    = TypeCode.DateTime, // 16
        TimeSpan    = 17, 
        String      = TypeCode.String, // 18
        Guid        = 19,

        ByteArray   = 20, 
        CharArray   = 21,
        Type        = 22, 
        Uri = 23, 

 
        SqlBinary   = 24, // SqlTypes should remain at the end for IsSqlType checking
        SqlBoolean  = 25,
        SqlByte     = 26,
        SqlBytes    = 27, 
        SqlChars    = 28,
        SqlDateTime = 29, 
        SqlDecimal  = 30, 
        SqlDouble   = 31,
        SqlGuid     = 32, 
        SqlInt16    = 33,
        SqlInt32    = 34,
        SqlInt64    = 35,
        SqlMoney    = 36, 
        SqlSingle   = 37,
        SqlString   = 38, 
//        SqlXml      = 39, 
    };
 
    abstract internal class DataStorage {

        // for Whidbey 40426, searching down the Type[] is about 20% faster than using a Dictionary
        // must keep in same order as enum StorageType 
        private static readonly Type[] StorageClassType = new Type[] {
            null, 
            typeof(Object), 
            typeof(DBNull),
            typeof(Boolean), 
            typeof(Char),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16), 
            typeof(UInt16),
            typeof(Int32), 
            typeof(UInt32), 
            typeof(Int64),
            typeof(UInt64), 
            typeof(Single),
            typeof(Double),
            typeof(Decimal),
            typeof(DateTime), 
            typeof(TimeSpan),
            typeof(String), 
            typeof(Guid), 

            typeof(byte[]), 
            typeof(char[]),
            typeof(Type),
            typeof(Uri),
 
            typeof(SqlBinary),
            typeof(SqlBoolean), 
            typeof(SqlByte), 
            typeof(SqlBytes),
            typeof(SqlChars), 
            typeof(SqlDateTime),
            typeof(SqlDecimal),
            typeof(SqlDouble),
            typeof(SqlGuid), 
            typeof(SqlInt16),
            typeof(SqlInt32), 
            typeof(SqlInt64), 
            typeof(SqlMoney),
            typeof(SqlSingle), 
            typeof(SqlString),
//            typeof(SqlXml),
        };
 
        internal readonly DataColumn Column;
        internal readonly DataTable Table; 
        internal readonly Type DataType; 
        internal readonly StorageType StorageTypeCode;
        private System.Collections.BitArray dbNullBits; 

        private readonly object DefaultValue;
        internal readonly object NullValue;
 
        internal readonly bool IsCloneable;
        internal readonly bool IsCustomDefinedType; 
        internal readonly bool IsStringType; 
        internal readonly bool IsValueType;
 
        protected DataStorage(DataColumn column, Type type, object defaultValue) : this(column, type, defaultValue, DBNull.Value, false) {
        }

        protected DataStorage(DataColumn column, Type type, object defaultValue, object nullValue) : this(column, type, defaultValue, nullValue, false) { 
        }
 
        protected DataStorage(DataColumn column, Type type, object defaultValue, object nullValue, bool isICloneable) { 
            Column = column;
            Table = column.Table; 
            DataType = type;
            StorageTypeCode = GetStorageType(type);
            DefaultValue = defaultValue;
            NullValue = nullValue; 
            IsCloneable = isICloneable;
            IsCustomDefinedType = IsTypeCustomType(StorageTypeCode); 
            IsStringType = ((StorageType.String == StorageTypeCode) || (StorageType.SqlString == StorageTypeCode)); 
            IsValueType = DetermineIfValueType(StorageTypeCode, type);
        } 

        internal DataSetDateTime DateTimeMode {
            get {
                return Column.DateTimeMode; 
            }
        } 
 
        internal IFormatProvider FormatProvider {
            get { 
                return Table.FormatProvider;
            }
        }
 
        public virtual Object Aggregate(int[] recordNos, AggregateType kind) {
            if (AggregateType.Count == kind) { 
                int count = 0; 
                for (int i = 0; i < recordNos.Length; i++) {
                    if (!this.dbNullBits.Get(recordNos[i])) 
                        count++;
                }
                return count;
            } 
            return null;
        } 
 
        protected int CompareBits(int recordNo1, int recordNo2) {
            bool recordNo1Null = this.dbNullBits.Get(recordNo1); 
            bool recordNo2Null = this.dbNullBits.Get(recordNo2);
            if (recordNo1Null ^ recordNo2Null) {
                if (recordNo1Null)
                    return -1; 
                else
                    return 1; 
            } 

            return 0; 
        }

        public virtual int Compare(int recordNo1, int recordNo2) {
            object valueNo1 = Get(recordNo1); 
            if (valueNo1 is IComparable) {
                object valueNo2 = Get(recordNo2); 
                if (valueNo2.GetType() == valueNo1.GetType()) 
                    return((IComparable) valueNo1).CompareTo(valueNo2);
                else 
                    CompareBits(recordNo1, recordNo2);
            }
            return 0;
        } 

        // only does comparision, expect value to be of the correct type 
        public abstract int CompareValueTo(int recordNo1, object value); 

        // only does conversion with support for reference null 
        public virtual object ConvertValue(object value) {
            return value;
        }
 
        protected void CopyBits(int srcRecordNo, int dstRecordNo) {
            this.dbNullBits.Set(dstRecordNo, this.dbNullBits.Get(srcRecordNo)); 
        } 

        abstract public void Copy(int recordNo1, int recordNo2); 

        abstract public Object Get(int recordNo);

        protected Object GetBits(int recordNo) { 
            if (this.dbNullBits.Get(recordNo)) {
                return NullValue; 
            } 
            return DefaultValue;
        } 

        virtual public int GetStringLength(int record) {
            System.Diagnostics.Debug.Assert(false, "not a String or SqlString column");
            return Int32.MaxValue; 
        }
 
        protected bool HasValue(int recordNo) { 
           return !this.dbNullBits.Get(recordNo);
        } 

        public virtual bool IsNull(int recordNo) {
           return this.dbNullBits.Get(recordNo);
        } 

        // convert (may not support reference null) and store the value 
        abstract public void Set(int recordNo, Object value); 

        protected void SetNullBit(int recordNo, bool flag) { 
            this.dbNullBits.Set(recordNo, flag);
        }

        virtual public void SetCapacity(int capacity) { 
            if (null == this.dbNullBits) {
                this.dbNullBits = new BitArray(capacity); 
            } 
            else {
                this.dbNullBits.Length = capacity; 
            }
        }

        abstract public object ConvertXmlToObject(string s); 
        public virtual object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute xmlAttrib) {
            return ConvertXmlToObject(xmlReader.Value); 
        } 

        abstract public string ConvertObjectToXml(object value); 
        public virtual void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute xmlAttrib) {
            xmlWriter.WriteString(ConvertObjectToXml(value));// should it be NO OP?
        }
 
        public static DataStorage CreateStorage(DataColumn column, Type dataType) {
            StorageType typeCode = GetStorageType(dataType); 
            if ((StorageType.Empty == typeCode) && (null != dataType)) { 
                if (typeof(INullable).IsAssignableFrom(dataType)) { // Udt, OracleTypes
                    return new SqlUdtStorage(column, dataType); 
                }
                else {
                    return new ObjectStorage(column, dataType); // non-nullable non-primitives
                } 
            }
 
            switch (typeCode) { 
            case StorageType.Empty:          throw ExceptionBuilder.InvalidStorageType(TypeCode.Empty);
            case StorageType.DBNull:         throw ExceptionBuilder.InvalidStorageType(TypeCode.DBNull); 
            case StorageType.Object:         return new ObjectStorage(column, dataType);
            case StorageType.Boolean:        return new BooleanStorage(column);
            case StorageType.Char:           return new CharStorage(column);
            case StorageType.SByte:          return new SByteStorage(column); 
            case StorageType.Byte:           return new ByteStorage(column);
            case StorageType.Int16:          return new Int16Storage(column); 
            case StorageType.UInt16:         return new UInt16Storage(column); 
            case StorageType.Int32:          return new Int32Storage(column);
            case StorageType.UInt32:         return new UInt32Storage(column); 
            case StorageType.Int64:          return new Int64Storage(column);
            case StorageType.UInt64:         return new UInt64Storage(column);
            case StorageType.Single:         return new SingleStorage(column);
            case StorageType.Double:         return new DoubleStorage(column); 
            case StorageType.Decimal:        return new DecimalStorage(column);
            case StorageType.DateTime:       return new DateTimeStorage(column); 
            case StorageType.TimeSpan:       return new TimeSpanStorage(column); 
            case StorageType.String:         return new StringStorage(column);
            case StorageType.Guid:           return new ObjectStorage(column, dataType); 

            case StorageType.ByteArray:      return new ObjectStorage(column, dataType);
            case StorageType.CharArray:      return new ObjectStorage(column, dataType);
            case StorageType.Type:           return new ObjectStorage(column, dataType); 
            case StorageType.Uri:            return new ObjectStorage(column, dataType);
 
            case StorageType.SqlBinary:      return new SqlBinaryStorage(column); 
            case StorageType.SqlBoolean:     return new SqlBooleanStorage(column);
            case StorageType.SqlByte:        return new SqlByteStorage(column); 
            case StorageType.SqlBytes:       return new SqlBytesStorage(column);
            case StorageType.SqlChars:       return new SqlCharsStorage(column);
            case StorageType.SqlDateTime:    return new SqlDateTimeStorage(column); //???/ what to do
            case StorageType.SqlDecimal:     return new SqlDecimalStorage(column); 
            case StorageType.SqlDouble:      return new SqlDoubleStorage(column);
            case StorageType.SqlGuid:        return new SqlGuidStorage(column); 
            case StorageType.SqlInt16:       return new SqlInt16Storage(column); 
            case StorageType.SqlInt32:       return new SqlInt32Storage(column);
            case StorageType.SqlInt64:       return new SqlInt64Storage(column); 
            case StorageType.SqlMoney:       return new SqlMoneyStorage(column);
            case StorageType.SqlSingle:      return new SqlSingleStorage(column);
            case StorageType.SqlString:      return new SqlStringStorage(column);
//            case StorageType.SqlXml:         return new SqlXmlStorage(column); 

            default: 
                System.Diagnostics.Debug.Assert(false, "shouldn't be here"); 
                goto case StorageType.Object;
            } 
        }

        internal static StorageType GetStorageType(Type dataType) {
            for(int i = 0; i < StorageClassType.Length; ++i) { 
                if (dataType == StorageClassType[i]) {
                    return (StorageType)i; 
                } 
            }
            TypeCode tcode = Type.GetTypeCode(dataType); 
            if (TypeCode.Object != tcode) { // enum -> Int64/Int32/Int16/Byte
                return (StorageType)tcode;
            }
            return StorageType.Empty; 
        }
 
        internal static Type GetTypeStorage(StorageType storageType) { 
            return StorageClassType[(int)storageType];
        } 

        internal static bool IsTypeCustomType(Type type) {
            return IsTypeCustomType(GetStorageType(type));
        } 

        internal static bool IsTypeCustomType(StorageType typeCode) { 
            return ((StorageType.Object == typeCode) ||(StorageType.Empty == typeCode) || (StorageType.CharArray == typeCode)); 
        }
 
        internal static bool IsSqlType(StorageType storageType) {
            return (StorageType.SqlBinary <= storageType);
        }
 
        public static bool IsSqlType(Type dataType) {
            for(int i = (int)StorageType.SqlBinary; i < StorageClassType.Length; ++i) { 
                if (dataType == StorageClassType[i]) { 
                    return true;
                } 
            }
            return false;
        }
 
        private static bool DetermineIfValueType(StorageType typeCode, Type dataType) {
            bool result; 
            switch(typeCode) { 
            case StorageType.Boolean:
            case StorageType.Char: 
            case StorageType.SByte:
            case StorageType.Byte:
            case StorageType.Int16:
            case StorageType.UInt16: 
            case StorageType.Int32:
            case StorageType.UInt32: 
            case StorageType.Int64: 
            case StorageType.UInt64:
            case StorageType.Single: 
            case StorageType.Double:
            case StorageType.Decimal:
            case StorageType.DateTime:
            case StorageType.TimeSpan: 
            case StorageType.Guid:
            case StorageType.SqlBinary: 
            case StorageType.SqlBoolean: 
            case StorageType.SqlByte:
            case StorageType.SqlDateTime: 
            case StorageType.SqlDecimal:
            case StorageType.SqlDouble:
            case StorageType.SqlGuid:
            case StorageType.SqlInt16: 
            case StorageType.SqlInt32:
            case StorageType.SqlInt64: 
            case StorageType.SqlMoney: 
            case StorageType.SqlSingle:
            case StorageType.SqlString: 
                result = true;
                break;

            case StorageType.String: 
            case StorageType.ByteArray:
            case StorageType.CharArray: 
            case StorageType.Type: 
            case StorageType.Uri:
            case StorageType.SqlBytes: 
            case StorageType.SqlChars:
                result = false;
                break;
 
            default:
                result = dataType.IsValueType; 
                break; 
            }
            Debug.Assert(result == dataType.IsValueType, "typeCode mismatches dataType"); 
            return result;
        }

        internal static void ImplementsInterfaces( 
                                    StorageType typeCode,
                                    Type dataType, 
                                    out bool sqlType, 
                                    out bool nullable,
                                    out bool xmlSerializable, 
                                    out bool changeTracking,
                                    out bool revertibleChangeTracking)
        {
            Debug.Assert(typeCode == GetStorageType(dataType), "typeCode mismatches dataType"); 
            if (IsSqlType(typeCode)) {
                sqlType = true; 
                nullable = true; 
                changeTracking = false;
                revertibleChangeTracking = false; 
                xmlSerializable = true;
            }
            else if (StorageType.Empty != typeCode) {
                sqlType = false; 
                nullable = false;
                changeTracking = false; 
                revertibleChangeTracking = false; 
                xmlSerializable = false;
            } 
            else {
                sqlType = false;
                nullable = typeof(System.Data.SqlTypes.INullable).IsAssignableFrom(dataType);
                changeTracking = typeof(System.ComponentModel.IChangeTracking).IsAssignableFrom(dataType); 
                revertibleChangeTracking = typeof(System.ComponentModel.IRevertibleChangeTracking).IsAssignableFrom(dataType);
                xmlSerializable = typeof(System.Xml.Serialization.IXmlSerializable).IsAssignableFrom(dataType); 
            } 
            Debug.Assert(nullable == typeof(System.Data.SqlTypes.INullable).IsAssignableFrom(dataType), "INullable");
            Debug.Assert(changeTracking == typeof(System.ComponentModel.IChangeTracking).IsAssignableFrom(dataType), "IChangeTracking"); 
            Debug.Assert(revertibleChangeTracking == typeof(System.ComponentModel.IRevertibleChangeTracking).IsAssignableFrom(dataType), "IRevertibleChangeTracking");
            Debug.Assert(xmlSerializable == typeof(System.Xml.Serialization.IXmlSerializable).IsAssignableFrom(dataType), "IXmlSerializable");
        }
 
        internal static bool ImplementsINullableValue(StorageType typeCode, Type dataType) {
            Debug.Assert(typeCode == GetStorageType(dataType), "typeCode mismatches dataType"); 
            return ((StorageType.Empty == typeCode) && dataType.IsGenericType && (dataType.GetGenericTypeDefinition() == typeof(System.Nullable<>))); 
        }
 
        public static bool IsObjectNull(object value){
            return ((null == value) || (DBNull.Value == value) || IsObjectSqlNull(value));
        }
 
        public static bool IsObjectSqlNull(object value){
            INullable inullable = (value as INullable); 
            return ((null != inullable) && inullable.IsNull); 
        }
 
        internal object GetEmptyStorageInternal(int recordCount) {
            return GetEmptyStorage(recordCount);
       }
 
        internal  void CopyValueInternal(int record, object store, BitArray nullbits, int storeIndex) {
            CopyValue(record, store, nullbits, storeIndex); 
        } 

        internal void SetStorageInternal(object store, BitArray nullbits) { 
            SetStorage(store, nullbits);
        }

        abstract protected Object GetEmptyStorage(int recordCount); 
        abstract protected void CopyValue(int record, object store, BitArray nullbits, int storeIndex);
        abstract protected void SetStorage(object store, BitArray nullbits); 
        protected void SetNullStorage(BitArray nullbits) { 
            dbNullBits = nullbits;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataStorage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.Common { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.SqlTypes; 
    using System.Diagnostics;
    using System.Xml; 
    using System.Xml.Serialization; 

    internal enum StorageType { 
        Empty       = TypeCode.Empty, // 0
        Object      = TypeCode.Object,
        DBNull      = TypeCode.DBNull,
        Boolean     = TypeCode.Boolean, 
        Char        = TypeCode.Char,
        SByte       = TypeCode.SByte, 
        Byte        = TypeCode.Byte, 
        Int16       = TypeCode.Int16,
        UInt16      = TypeCode.UInt16, 
        Int32       = TypeCode.Int32,
        UInt32      = TypeCode.UInt32,
        Int64       = TypeCode.Int64,
        UInt64      = TypeCode.UInt64, 
        Single      = TypeCode.Single,
        Double      = TypeCode.Double, 
        Decimal     = TypeCode.Decimal, // 15 
        DateTime    = TypeCode.DateTime, // 16
        TimeSpan    = 17, 
        String      = TypeCode.String, // 18
        Guid        = 19,

        ByteArray   = 20, 
        CharArray   = 21,
        Type        = 22, 
        Uri = 23, 

 
        SqlBinary   = 24, // SqlTypes should remain at the end for IsSqlType checking
        SqlBoolean  = 25,
        SqlByte     = 26,
        SqlBytes    = 27, 
        SqlChars    = 28,
        SqlDateTime = 29, 
        SqlDecimal  = 30, 
        SqlDouble   = 31,
        SqlGuid     = 32, 
        SqlInt16    = 33,
        SqlInt32    = 34,
        SqlInt64    = 35,
        SqlMoney    = 36, 
        SqlSingle   = 37,
        SqlString   = 38, 
//        SqlXml      = 39, 
    };
 
    abstract internal class DataStorage {

        // for Whidbey 40426, searching down the Type[] is about 20% faster than using a Dictionary
        // must keep in same order as enum StorageType 
        private static readonly Type[] StorageClassType = new Type[] {
            null, 
            typeof(Object), 
            typeof(DBNull),
            typeof(Boolean), 
            typeof(Char),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16), 
            typeof(UInt16),
            typeof(Int32), 
            typeof(UInt32), 
            typeof(Int64),
            typeof(UInt64), 
            typeof(Single),
            typeof(Double),
            typeof(Decimal),
            typeof(DateTime), 
            typeof(TimeSpan),
            typeof(String), 
            typeof(Guid), 

            typeof(byte[]), 
            typeof(char[]),
            typeof(Type),
            typeof(Uri),
 
            typeof(SqlBinary),
            typeof(SqlBoolean), 
            typeof(SqlByte), 
            typeof(SqlBytes),
            typeof(SqlChars), 
            typeof(SqlDateTime),
            typeof(SqlDecimal),
            typeof(SqlDouble),
            typeof(SqlGuid), 
            typeof(SqlInt16),
            typeof(SqlInt32), 
            typeof(SqlInt64), 
            typeof(SqlMoney),
            typeof(SqlSingle), 
            typeof(SqlString),
//            typeof(SqlXml),
        };
 
        internal readonly DataColumn Column;
        internal readonly DataTable Table; 
        internal readonly Type DataType; 
        internal readonly StorageType StorageTypeCode;
        private System.Collections.BitArray dbNullBits; 

        private readonly object DefaultValue;
        internal readonly object NullValue;
 
        internal readonly bool IsCloneable;
        internal readonly bool IsCustomDefinedType; 
        internal readonly bool IsStringType; 
        internal readonly bool IsValueType;
 
        protected DataStorage(DataColumn column, Type type, object defaultValue) : this(column, type, defaultValue, DBNull.Value, false) {
        }

        protected DataStorage(DataColumn column, Type type, object defaultValue, object nullValue) : this(column, type, defaultValue, nullValue, false) { 
        }
 
        protected DataStorage(DataColumn column, Type type, object defaultValue, object nullValue, bool isICloneable) { 
            Column = column;
            Table = column.Table; 
            DataType = type;
            StorageTypeCode = GetStorageType(type);
            DefaultValue = defaultValue;
            NullValue = nullValue; 
            IsCloneable = isICloneable;
            IsCustomDefinedType = IsTypeCustomType(StorageTypeCode); 
            IsStringType = ((StorageType.String == StorageTypeCode) || (StorageType.SqlString == StorageTypeCode)); 
            IsValueType = DetermineIfValueType(StorageTypeCode, type);
        } 

        internal DataSetDateTime DateTimeMode {
            get {
                return Column.DateTimeMode; 
            }
        } 
 
        internal IFormatProvider FormatProvider {
            get { 
                return Table.FormatProvider;
            }
        }
 
        public virtual Object Aggregate(int[] recordNos, AggregateType kind) {
            if (AggregateType.Count == kind) { 
                int count = 0; 
                for (int i = 0; i < recordNos.Length; i++) {
                    if (!this.dbNullBits.Get(recordNos[i])) 
                        count++;
                }
                return count;
            } 
            return null;
        } 
 
        protected int CompareBits(int recordNo1, int recordNo2) {
            bool recordNo1Null = this.dbNullBits.Get(recordNo1); 
            bool recordNo2Null = this.dbNullBits.Get(recordNo2);
            if (recordNo1Null ^ recordNo2Null) {
                if (recordNo1Null)
                    return -1; 
                else
                    return 1; 
            } 

            return 0; 
        }

        public virtual int Compare(int recordNo1, int recordNo2) {
            object valueNo1 = Get(recordNo1); 
            if (valueNo1 is IComparable) {
                object valueNo2 = Get(recordNo2); 
                if (valueNo2.GetType() == valueNo1.GetType()) 
                    return((IComparable) valueNo1).CompareTo(valueNo2);
                else 
                    CompareBits(recordNo1, recordNo2);
            }
            return 0;
        } 

        // only does comparision, expect value to be of the correct type 
        public abstract int CompareValueTo(int recordNo1, object value); 

        // only does conversion with support for reference null 
        public virtual object ConvertValue(object value) {
            return value;
        }
 
        protected void CopyBits(int srcRecordNo, int dstRecordNo) {
            this.dbNullBits.Set(dstRecordNo, this.dbNullBits.Get(srcRecordNo)); 
        } 

        abstract public void Copy(int recordNo1, int recordNo2); 

        abstract public Object Get(int recordNo);

        protected Object GetBits(int recordNo) { 
            if (this.dbNullBits.Get(recordNo)) {
                return NullValue; 
            } 
            return DefaultValue;
        } 

        virtual public int GetStringLength(int record) {
            System.Diagnostics.Debug.Assert(false, "not a String or SqlString column");
            return Int32.MaxValue; 
        }
 
        protected bool HasValue(int recordNo) { 
           return !this.dbNullBits.Get(recordNo);
        } 

        public virtual bool IsNull(int recordNo) {
           return this.dbNullBits.Get(recordNo);
        } 

        // convert (may not support reference null) and store the value 
        abstract public void Set(int recordNo, Object value); 

        protected void SetNullBit(int recordNo, bool flag) { 
            this.dbNullBits.Set(recordNo, flag);
        }

        virtual public void SetCapacity(int capacity) { 
            if (null == this.dbNullBits) {
                this.dbNullBits = new BitArray(capacity); 
            } 
            else {
                this.dbNullBits.Length = capacity; 
            }
        }

        abstract public object ConvertXmlToObject(string s); 
        public virtual object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute xmlAttrib) {
            return ConvertXmlToObject(xmlReader.Value); 
        } 

        abstract public string ConvertObjectToXml(object value); 
        public virtual void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute xmlAttrib) {
            xmlWriter.WriteString(ConvertObjectToXml(value));// should it be NO OP?
        }
 
        public static DataStorage CreateStorage(DataColumn column, Type dataType) {
            StorageType typeCode = GetStorageType(dataType); 
            if ((StorageType.Empty == typeCode) && (null != dataType)) { 
                if (typeof(INullable).IsAssignableFrom(dataType)) { // Udt, OracleTypes
                    return new SqlUdtStorage(column, dataType); 
                }
                else {
                    return new ObjectStorage(column, dataType); // non-nullable non-primitives
                } 
            }
 
            switch (typeCode) { 
            case StorageType.Empty:          throw ExceptionBuilder.InvalidStorageType(TypeCode.Empty);
            case StorageType.DBNull:         throw ExceptionBuilder.InvalidStorageType(TypeCode.DBNull); 
            case StorageType.Object:         return new ObjectStorage(column, dataType);
            case StorageType.Boolean:        return new BooleanStorage(column);
            case StorageType.Char:           return new CharStorage(column);
            case StorageType.SByte:          return new SByteStorage(column); 
            case StorageType.Byte:           return new ByteStorage(column);
            case StorageType.Int16:          return new Int16Storage(column); 
            case StorageType.UInt16:         return new UInt16Storage(column); 
            case StorageType.Int32:          return new Int32Storage(column);
            case StorageType.UInt32:         return new UInt32Storage(column); 
            case StorageType.Int64:          return new Int64Storage(column);
            case StorageType.UInt64:         return new UInt64Storage(column);
            case StorageType.Single:         return new SingleStorage(column);
            case StorageType.Double:         return new DoubleStorage(column); 
            case StorageType.Decimal:        return new DecimalStorage(column);
            case StorageType.DateTime:       return new DateTimeStorage(column); 
            case StorageType.TimeSpan:       return new TimeSpanStorage(column); 
            case StorageType.String:         return new StringStorage(column);
            case StorageType.Guid:           return new ObjectStorage(column, dataType); 

            case StorageType.ByteArray:      return new ObjectStorage(column, dataType);
            case StorageType.CharArray:      return new ObjectStorage(column, dataType);
            case StorageType.Type:           return new ObjectStorage(column, dataType); 
            case StorageType.Uri:            return new ObjectStorage(column, dataType);
 
            case StorageType.SqlBinary:      return new SqlBinaryStorage(column); 
            case StorageType.SqlBoolean:     return new SqlBooleanStorage(column);
            case StorageType.SqlByte:        return new SqlByteStorage(column); 
            case StorageType.SqlBytes:       return new SqlBytesStorage(column);
            case StorageType.SqlChars:       return new SqlCharsStorage(column);
            case StorageType.SqlDateTime:    return new SqlDateTimeStorage(column); //???/ what to do
            case StorageType.SqlDecimal:     return new SqlDecimalStorage(column); 
            case StorageType.SqlDouble:      return new SqlDoubleStorage(column);
            case StorageType.SqlGuid:        return new SqlGuidStorage(column); 
            case StorageType.SqlInt16:       return new SqlInt16Storage(column); 
            case StorageType.SqlInt32:       return new SqlInt32Storage(column);
            case StorageType.SqlInt64:       return new SqlInt64Storage(column); 
            case StorageType.SqlMoney:       return new SqlMoneyStorage(column);
            case StorageType.SqlSingle:      return new SqlSingleStorage(column);
            case StorageType.SqlString:      return new SqlStringStorage(column);
//            case StorageType.SqlXml:         return new SqlXmlStorage(column); 

            default: 
                System.Diagnostics.Debug.Assert(false, "shouldn't be here"); 
                goto case StorageType.Object;
            } 
        }

        internal static StorageType GetStorageType(Type dataType) {
            for(int i = 0; i < StorageClassType.Length; ++i) { 
                if (dataType == StorageClassType[i]) {
                    return (StorageType)i; 
                } 
            }
            TypeCode tcode = Type.GetTypeCode(dataType); 
            if (TypeCode.Object != tcode) { // enum -> Int64/Int32/Int16/Byte
                return (StorageType)tcode;
            }
            return StorageType.Empty; 
        }
 
        internal static Type GetTypeStorage(StorageType storageType) { 
            return StorageClassType[(int)storageType];
        } 

        internal static bool IsTypeCustomType(Type type) {
            return IsTypeCustomType(GetStorageType(type));
        } 

        internal static bool IsTypeCustomType(StorageType typeCode) { 
            return ((StorageType.Object == typeCode) ||(StorageType.Empty == typeCode) || (StorageType.CharArray == typeCode)); 
        }
 
        internal static bool IsSqlType(StorageType storageType) {
            return (StorageType.SqlBinary <= storageType);
        }
 
        public static bool IsSqlType(Type dataType) {
            for(int i = (int)StorageType.SqlBinary; i < StorageClassType.Length; ++i) { 
                if (dataType == StorageClassType[i]) { 
                    return true;
                } 
            }
            return false;
        }
 
        private static bool DetermineIfValueType(StorageType typeCode, Type dataType) {
            bool result; 
            switch(typeCode) { 
            case StorageType.Boolean:
            case StorageType.Char: 
            case StorageType.SByte:
            case StorageType.Byte:
            case StorageType.Int16:
            case StorageType.UInt16: 
            case StorageType.Int32:
            case StorageType.UInt32: 
            case StorageType.Int64: 
            case StorageType.UInt64:
            case StorageType.Single: 
            case StorageType.Double:
            case StorageType.Decimal:
            case StorageType.DateTime:
            case StorageType.TimeSpan: 
            case StorageType.Guid:
            case StorageType.SqlBinary: 
            case StorageType.SqlBoolean: 
            case StorageType.SqlByte:
            case StorageType.SqlDateTime: 
            case StorageType.SqlDecimal:
            case StorageType.SqlDouble:
            case StorageType.SqlGuid:
            case StorageType.SqlInt16: 
            case StorageType.SqlInt32:
            case StorageType.SqlInt64: 
            case StorageType.SqlMoney: 
            case StorageType.SqlSingle:
            case StorageType.SqlString: 
                result = true;
                break;

            case StorageType.String: 
            case StorageType.ByteArray:
            case StorageType.CharArray: 
            case StorageType.Type: 
            case StorageType.Uri:
            case StorageType.SqlBytes: 
            case StorageType.SqlChars:
                result = false;
                break;
 
            default:
                result = dataType.IsValueType; 
                break; 
            }
            Debug.Assert(result == dataType.IsValueType, "typeCode mismatches dataType"); 
            return result;
        }

        internal static void ImplementsInterfaces( 
                                    StorageType typeCode,
                                    Type dataType, 
                                    out bool sqlType, 
                                    out bool nullable,
                                    out bool xmlSerializable, 
                                    out bool changeTracking,
                                    out bool revertibleChangeTracking)
        {
            Debug.Assert(typeCode == GetStorageType(dataType), "typeCode mismatches dataType"); 
            if (IsSqlType(typeCode)) {
                sqlType = true; 
                nullable = true; 
                changeTracking = false;
                revertibleChangeTracking = false; 
                xmlSerializable = true;
            }
            else if (StorageType.Empty != typeCode) {
                sqlType = false; 
                nullable = false;
                changeTracking = false; 
                revertibleChangeTracking = false; 
                xmlSerializable = false;
            } 
            else {
                sqlType = false;
                nullable = typeof(System.Data.SqlTypes.INullable).IsAssignableFrom(dataType);
                changeTracking = typeof(System.ComponentModel.IChangeTracking).IsAssignableFrom(dataType); 
                revertibleChangeTracking = typeof(System.ComponentModel.IRevertibleChangeTracking).IsAssignableFrom(dataType);
                xmlSerializable = typeof(System.Xml.Serialization.IXmlSerializable).IsAssignableFrom(dataType); 
            } 
            Debug.Assert(nullable == typeof(System.Data.SqlTypes.INullable).IsAssignableFrom(dataType), "INullable");
            Debug.Assert(changeTracking == typeof(System.ComponentModel.IChangeTracking).IsAssignableFrom(dataType), "IChangeTracking"); 
            Debug.Assert(revertibleChangeTracking == typeof(System.ComponentModel.IRevertibleChangeTracking).IsAssignableFrom(dataType), "IRevertibleChangeTracking");
            Debug.Assert(xmlSerializable == typeof(System.Xml.Serialization.IXmlSerializable).IsAssignableFrom(dataType), "IXmlSerializable");
        }
 
        internal static bool ImplementsINullableValue(StorageType typeCode, Type dataType) {
            Debug.Assert(typeCode == GetStorageType(dataType), "typeCode mismatches dataType"); 
            return ((StorageType.Empty == typeCode) && dataType.IsGenericType && (dataType.GetGenericTypeDefinition() == typeof(System.Nullable<>))); 
        }
 
        public static bool IsObjectNull(object value){
            return ((null == value) || (DBNull.Value == value) || IsObjectSqlNull(value));
        }
 
        public static bool IsObjectSqlNull(object value){
            INullable inullable = (value as INullable); 
            return ((null != inullable) && inullable.IsNull); 
        }
 
        internal object GetEmptyStorageInternal(int recordCount) {
            return GetEmptyStorage(recordCount);
       }
 
        internal  void CopyValueInternal(int record, object store, BitArray nullbits, int storeIndex) {
            CopyValue(record, store, nullbits, storeIndex); 
        } 

        internal void SetStorageInternal(object store, BitArray nullbits) { 
            SetStorage(store, nullbits);
        }

        abstract protected Object GetEmptyStorage(int recordCount); 
        abstract protected void CopyValue(int record, object store, BitArray nullbits, int storeIndex);
        abstract protected void SetStorage(object store, BitArray nullbits); 
        protected void SetNullStorage(BitArray nullbits) { 
            dbNullBits = nullbits;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
