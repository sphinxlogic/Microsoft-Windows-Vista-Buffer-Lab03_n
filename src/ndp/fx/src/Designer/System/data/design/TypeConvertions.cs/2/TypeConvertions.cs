 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
using System;
using System.Data; 
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data.SqlTypes;
 
namespace System.Data.Design
{ 
    internal enum OleDbParameterDirection { 
        // OLE DB parameter directions
        Input = 0x01, 
        InputOutput = 0x02,
        Output = 0x03,
        ReturnValue = 0x04
    } 

    internal enum OleDbDataType { 
        // OLE DB data types 
        adEmpty = 0x0,
        adTinyInt = 0x10, 
        adSmallInt = 0x2,
        adInteger = 0x3,
        adBigInt = 0x14,
        adUnsignedTinyInt = 0x11, 
        adUnsignedSmallInt = 0x12,
        adUnsignedInt = 0x13, 
        adUnsignedBigInt = 0x15, 
        adSingle = 0x4,
        adDouble = 0x5, 
        adCurrency = 0x6,
        adDecimal = 0xE,
        adNumeric = 0x83,
        adBoolean = 0xB, 
        adError = 0xA,
        adUserDefined = 0x84, 
        adVariant = 0xC, 
        adIDispatch = 0x9,
        adIUnknown = 0xD, 
        adGUID = 0x48,
        adDate = 0x7,
        adDBDate = 0x85,
        adDBTime = 0x86, 
        adDBTimeStamp = 0x87,
        adBSTR = 0x08, 
        adChar = 0x81, 
        adVarChar = 0xC8,
        adLongVarChar = 0xC9, 
        adWChar = 0x82,
        adVarWChar = 0xCA,
        adLongVarWChar = 0xCB,
        adBinary = 0x80, 
        adVarBinary = 0xCC,
        adLongVarBinary = 0xCD, 
        adChapter = 0x88, 
        adFileTime = 0x40,
        adPropVariant = 0x8A, 
        adVarNumeric = 0x8B,
        adArray = 0x2000
    }
 
    /// <summary>
    /// Summary description for TypeConvertions. 
    /// </summary> 
    internal class TypeConvertions {
 
        /// <summary>
        /// Private contstructor to avoid class being instantiated.
        /// </summary>
        private TypeConvertions() { 
        }
 
        private static int[] oleDbToAdoPlusDirectionMap = new int[] { 
            (int) OleDbParameterDirection.Input, (int) ParameterDirection.Input,
            (int) OleDbParameterDirection.InputOutput, (int) ParameterDirection.InputOutput, 
            (int) OleDbParameterDirection.Output, (int) ParameterDirection.Output,
            (int) OleDbParameterDirection.ReturnValue, (int) ParameterDirection.ReturnValue
        };
 
        private static int[] oleDbTypeToDbTypeMap = new int[] {
            (int) OleDbDataType.adBSTR, (int) DbType.AnsiString, 
            (int) OleDbDataType.adBigInt, (int) DbType.Int64, 
            (int) OleDbDataType.adBinary, (int) DbType.Binary,
            (int) OleDbDataType.adBoolean, (int) DbType.Boolean, 
            (int) OleDbDataType.adChar, (int) DbType.AnsiString,
            (int) OleDbDataType.adCurrency, (int) DbType.Currency,
            (int) OleDbDataType.adDate, (int) DbType.Date,
            (int) OleDbDataType.adDBDate, (int) DbType.Date, 
            (int) OleDbDataType.adDBTime, (int) DbType.DateTime,
            (int) OleDbDataType.adDBTimeStamp, (int) DbType.DateTime, 
            (int) OleDbDataType.adDecimal, (int) DbType.Decimal, 
            (int) OleDbDataType.adDouble, (int) DbType.Double,
            (int) OleDbDataType.adEmpty, (int) DbType.Object, 
            (int) OleDbDataType.adError, (int) DbType.Object,
            (int) OleDbDataType.adFileTime, (int) DbType.DateTime,
            (int) OleDbDataType.adGUID, (int) DbType.Guid,
            (int) OleDbDataType.adIDispatch, (int) DbType.Object, 
            (int) OleDbDataType.adIUnknown, (int) DbType.Object,
            (int) OleDbDataType.adInteger, (int) DbType.Int32, 
            (int) OleDbDataType.adLongVarBinary, (int) DbType.Binary, 
            (int) OleDbDataType.adLongVarChar, (int) DbType.AnsiString,
            (int) OleDbDataType.adLongVarWChar, (int) DbType.String, 
            (int) OleDbDataType.adNumeric, (int) DbType.Decimal,
            (int) OleDbDataType.adPropVariant, (int) DbType.Object,
            (int) OleDbDataType.adSingle, (int) DbType.Single,
            (int) OleDbDataType.adSmallInt, (int) DbType.Int16, 
            (int) OleDbDataType.adTinyInt, (int) DbType.SByte,
            (int) OleDbDataType.adUnsignedBigInt, (int) DbType.UInt64, 
            (int) OleDbDataType.adUnsignedInt, (int) DbType.UInt32, 
            (int) OleDbDataType.adUnsignedSmallInt, (int) DbType.UInt16,
            (int) OleDbDataType.adUnsignedTinyInt, (int) DbType.Byte, 
            (int) OleDbDataType.adVarBinary, (int) DbType.Binary,
            (int) OleDbDataType.adVarChar, (int) DbType.AnsiString,
            (int) OleDbDataType.adVarNumeric, (int) DbType.VarNumeric,
            (int) OleDbDataType.adVarWChar, (int) DbType.String, 
            (int) OleDbDataType.adVariant, (int) DbType.Object,
            (int) OleDbDataType.adWChar, (int) DbType.String 
        }; 

        private static object[] sqlTypeToSqlDbTypeMap = new object[] { 
            typeof(SqlBinary),  SqlDbType.Binary,
            typeof(SqlInt64),  SqlDbType.BigInt,
            typeof(SqlBoolean), SqlDbType.Bit,
            typeof(SqlString),  SqlDbType.Char, 
            typeof(SqlDateTime),  SqlDbType.DateTime,
            typeof(SqlDecimal),  SqlDbType.Decimal, 
            typeof(SqlDouble),  SqlDbType.Float, 
            typeof(SqlBinary),  SqlDbType.Image,
            typeof(SqlInt32),  SqlDbType.Int, 
            typeof(SqlMoney),  SqlDbType.Money,
            typeof(SqlString),  SqlDbType.NChar,
            typeof(SqlString),  SqlDbType.NText,
            typeof(SqlString),  SqlDbType.NVarChar, 
            typeof(SqlSingle),  SqlDbType.Real,
            typeof(SqlDateTime),  SqlDbType.SmallDateTime, 
            typeof(SqlInt16),  SqlDbType.SmallInt, 
            typeof(SqlMoney),  SqlDbType.SmallMoney,
            typeof(Object),  SqlDbType.Variant, 
            typeof(SqlString),  SqlDbType.VarChar,
            typeof(SqlString),  SqlDbType.Text,
            typeof(SqlBinary),  SqlDbType.Timestamp,
            typeof(SqlByte),  SqlDbType.TinyInt, 
            typeof(SqlBinary),  SqlDbType.VarBinary,
            typeof(SqlString),  SqlDbType.VarChar, 
            typeof(SqlGuid),  SqlDbType.UniqueIdentifier 
        };
 

        private static object[] sqlTypeToUrtType = new object[] {
            typeof(SqlBinary),   typeof(System.Byte[]),
            typeof(SqlByte),     typeof(System.Byte), 
            typeof(SqlDecimal),  typeof(System.Decimal),
            typeof(SqlDouble),   typeof(System.Double), 
            typeof(SqlGuid),     typeof(System.Guid), 
            typeof(SqlString),   typeof(System.String),
            typeof(SqlSingle),   typeof(System.Single), 
            typeof(SqlDateTime), typeof(System.DateTime),
            typeof(SqlInt16),    typeof(System.Int16),
            typeof(SqlInt32),    typeof(System.Int32),
            typeof(SqlInt64),    typeof(System.Int64), 
            typeof(SqlMoney),    typeof(System.Decimal),
            typeof(Object),      typeof(System.Object) 
        }; 

        private static object[] dbTypeToUrtTypeMap = new object[] { 
            DbType.AnsiString, typeof(System.String),
            DbType.AnsiStringFixedLength, typeof(System.String),
            DbType.Binary, typeof(System.Byte[]),
            DbType.Boolean, typeof(System.Boolean), 
            DbType.Byte, typeof(System.Byte),
            DbType.Currency, typeof(System.Decimal), 
            DbType.Date, typeof(System.DateTime), 
            DbType.DateTime, typeof(System.DateTime),
            DbType.DateTime2, typeof(System.DateTime), 
            DbType.DateTimeOffset, typeof(System.DateTimeOffset),
            DbType.Decimal, typeof(System.Decimal),
            DbType.Double, typeof(System.Double),
            DbType.Guid, typeof(System.Guid), 
            DbType.Int16, typeof(System.Int16),
            DbType.Int32, typeof(System.Int32), 
            DbType.Int64, typeof(System.Int64), 
            DbType.Object, typeof(System.Object),
            DbType.SByte, typeof(System.Byte), 
            DbType.Single, typeof(float),
            DbType.String, typeof(System.String),
            DbType.StringFixedLength, typeof(System.String),
            DbType.Time, typeof(System.DateTime), 
            DbType.UInt16, typeof(System.UInt16),
            DbType.UInt32, typeof(System.UInt32), 
            DbType.UInt64, typeof(System.UInt64), 
            DbType.VarNumeric, typeof(System.Decimal)
        }; 


        internal static Type SqlDbTypeToSqlType(SqlDbType sqlDbType) {
            for(int i = 1; i < sqlTypeToSqlDbTypeMap.Length; i += 2) { 
                if(sqlDbType == (SqlDbType) sqlTypeToSqlDbTypeMap[i]) {
                    return (Type) sqlTypeToSqlDbTypeMap[i-1]; 
                } 
            }
 
            return null;
        }

 
        //internal static Type SqlTypeToUrtType(Type sqlType) {
        //    for (int i = 0; i < sqlTypeToUrtType.Length; i += 2){ 
        //        if (sqlType == (Type)sqlTypeToUrtType[i]){ 
        //            return (Type)sqlTypeToUrtType[i + 1];
        //        } 
        //    }

        //    return null;
        //} 

 
        //internal static Type UrtTypeToSqlType(Type urtType) { 
        //    for (int i = 1; i < sqlTypeToUrtType.Length; i += 2) {
        //        if (urtType == sqlTypeToUrtType[i]) { 
        //            return (Type)sqlTypeToUrtType[i - 1];
        //        }
        //    }
 
        //    return null;
        //} 
 
        internal static Type DbTypeToUrtType(DbType dbType) {
            for (int i = 0; i < dbTypeToUrtTypeMap.Length; i += 2) { 
                if (dbType == (DbType) dbTypeToUrtTypeMap[i]) {
                    return (Type)dbTypeToUrtTypeMap[i + 1];
                }
            } 

            return null; 
        } 

 
        //internal static ParameterDirection OleDbToAdoPlusDirection(int oleDbDirection) {
        //    return (ParameterDirection) DoMapping( oleDbToAdoPlusDirectionMap, oleDbDirection, (int) ParameterDirection.Input );
        //}
 

        //internal static Type OleDbToUrtType( int oleDbType ) { 
        //    Type returnType = null; 
        //    bool isArray = (oleDbType & (int) OleDbDataType.adArray) > 0;
        //    oleDbType = oleDbType & ~((int) OleDbDataType.adArray); 

        //    switch (oleDbType) {
        //        case (int) OleDbDataType.adSmallInt:
        //            returnType = typeof(Int16); 
        //            break;
        //        case (int) OleDbDataType.adInteger: 
        //            returnType = typeof(Int32); 
        //            break;
        //        case (int) OleDbDataType.adSingle: 
        //            returnType = typeof(Single);
        //            break;
        //        case (int) OleDbDataType.adDouble:
        //            returnType = typeof(Double); 
        //            break;
        //        case (int) OleDbDataType.adCurrency: 
        //        case (int) OleDbDataType.adNumeric: 
        //        case (int) OleDbDataType.adDecimal:
        //            returnType = typeof(Decimal); 
        //            break;
        //        case (int) OleDbDataType.adDate:
        //        case (int) OleDbDataType.adDBDate:
        //        case (int) OleDbDataType.adDBTimeStamp: 
        //            returnType = typeof(DateTime);
        //            break; 
        //        case (int) OleDbDataType.adBSTR: 
        //        case (int) OleDbDataType.adChar:
        //        case (int) OleDbDataType.adWChar: 
        //            returnType = typeof(String);
        //            break;
        //        case (int) OleDbDataType.adEmpty:
        //        case (int) OleDbDataType.adIDispatch: 
        //        case (int) OleDbDataType.adVariant:
        //        case (int) OleDbDataType.adIUnknown: 
        //            returnType = typeof(Object); 
        //            break;
        //        case (int) OleDbDataType.adError: 
        //            returnType = typeof(Exception);
        //            break;
        //        case (int) OleDbDataType.adBoolean:
        //            returnType = typeof(Boolean); 
        //            break;
        //        case (int) OleDbDataType.adUnsignedTinyInt: 
        //            returnType = typeof(Byte); 
        //            break;
        //        case (int) OleDbDataType.adTinyInt: 
        //            returnType = typeof(SByte);
        //            break;
        //        case (int) OleDbDataType.adUnsignedSmallInt:
        //            returnType = typeof(UInt16); 
        //            break;
        //        case (int) OleDbDataType.adUnsignedInt: 
        //            returnType = typeof(UInt32); 
        //            break;
        //        case (int) OleDbDataType.adBigInt: 
        //            returnType = typeof(Int64);
        //            break;
        //        case (int) OleDbDataType.adUnsignedBigInt:
        //            returnType = typeof(UInt64); 
        //            break;
        //        case (int) OleDbDataType.adGUID: 
        //            returnType = typeof(Guid); 
        //            break;
        //        case (int) OleDbDataType.adBinary: 
        //            returnType = typeof(Byte[]);
        //            break;
        //        case (int) OleDbDataType.adDBTime:
        //            returnType = typeof(TimeSpan); 
        //            break;
        //        default: 
        //            returnType = typeof(Object); 
        //            break;
        //    } 

        //    //

 

        //internal static DbType OleDbToDbType( int oleDbType ) { 
        //    return (DbType) DoMapping( oleDbTypeToDbTypeMap, oleDbType, (int) DbType.Object ); 
        //}
 

        //private static int DoMapping( int[] mapTable, int from, int defaultTarget ) {
        //    for (int i = 0; i< mapTable.Length; i= i+2) {
        //        if( mapTable[i] == from ) 
        //            return mapTable[i+1];
        //    } 
 
        //    return defaultTarget;
        //} 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
using System;
using System.Data; 
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data.SqlTypes;
 
namespace System.Data.Design
{ 
    internal enum OleDbParameterDirection { 
        // OLE DB parameter directions
        Input = 0x01, 
        InputOutput = 0x02,
        Output = 0x03,
        ReturnValue = 0x04
    } 

    internal enum OleDbDataType { 
        // OLE DB data types 
        adEmpty = 0x0,
        adTinyInt = 0x10, 
        adSmallInt = 0x2,
        adInteger = 0x3,
        adBigInt = 0x14,
        adUnsignedTinyInt = 0x11, 
        adUnsignedSmallInt = 0x12,
        adUnsignedInt = 0x13, 
        adUnsignedBigInt = 0x15, 
        adSingle = 0x4,
        adDouble = 0x5, 
        adCurrency = 0x6,
        adDecimal = 0xE,
        adNumeric = 0x83,
        adBoolean = 0xB, 
        adError = 0xA,
        adUserDefined = 0x84, 
        adVariant = 0xC, 
        adIDispatch = 0x9,
        adIUnknown = 0xD, 
        adGUID = 0x48,
        adDate = 0x7,
        adDBDate = 0x85,
        adDBTime = 0x86, 
        adDBTimeStamp = 0x87,
        adBSTR = 0x08, 
        adChar = 0x81, 
        adVarChar = 0xC8,
        adLongVarChar = 0xC9, 
        adWChar = 0x82,
        adVarWChar = 0xCA,
        adLongVarWChar = 0xCB,
        adBinary = 0x80, 
        adVarBinary = 0xCC,
        adLongVarBinary = 0xCD, 
        adChapter = 0x88, 
        adFileTime = 0x40,
        adPropVariant = 0x8A, 
        adVarNumeric = 0x8B,
        adArray = 0x2000
    }
 
    /// <summary>
    /// Summary description for TypeConvertions. 
    /// </summary> 
    internal class TypeConvertions {
 
        /// <summary>
        /// Private contstructor to avoid class being instantiated.
        /// </summary>
        private TypeConvertions() { 
        }
 
        private static int[] oleDbToAdoPlusDirectionMap = new int[] { 
            (int) OleDbParameterDirection.Input, (int) ParameterDirection.Input,
            (int) OleDbParameterDirection.InputOutput, (int) ParameterDirection.InputOutput, 
            (int) OleDbParameterDirection.Output, (int) ParameterDirection.Output,
            (int) OleDbParameterDirection.ReturnValue, (int) ParameterDirection.ReturnValue
        };
 
        private static int[] oleDbTypeToDbTypeMap = new int[] {
            (int) OleDbDataType.adBSTR, (int) DbType.AnsiString, 
            (int) OleDbDataType.adBigInt, (int) DbType.Int64, 
            (int) OleDbDataType.adBinary, (int) DbType.Binary,
            (int) OleDbDataType.adBoolean, (int) DbType.Boolean, 
            (int) OleDbDataType.adChar, (int) DbType.AnsiString,
            (int) OleDbDataType.adCurrency, (int) DbType.Currency,
            (int) OleDbDataType.adDate, (int) DbType.Date,
            (int) OleDbDataType.adDBDate, (int) DbType.Date, 
            (int) OleDbDataType.adDBTime, (int) DbType.DateTime,
            (int) OleDbDataType.adDBTimeStamp, (int) DbType.DateTime, 
            (int) OleDbDataType.adDecimal, (int) DbType.Decimal, 
            (int) OleDbDataType.adDouble, (int) DbType.Double,
            (int) OleDbDataType.adEmpty, (int) DbType.Object, 
            (int) OleDbDataType.adError, (int) DbType.Object,
            (int) OleDbDataType.adFileTime, (int) DbType.DateTime,
            (int) OleDbDataType.adGUID, (int) DbType.Guid,
            (int) OleDbDataType.adIDispatch, (int) DbType.Object, 
            (int) OleDbDataType.adIUnknown, (int) DbType.Object,
            (int) OleDbDataType.adInteger, (int) DbType.Int32, 
            (int) OleDbDataType.adLongVarBinary, (int) DbType.Binary, 
            (int) OleDbDataType.adLongVarChar, (int) DbType.AnsiString,
            (int) OleDbDataType.adLongVarWChar, (int) DbType.String, 
            (int) OleDbDataType.adNumeric, (int) DbType.Decimal,
            (int) OleDbDataType.adPropVariant, (int) DbType.Object,
            (int) OleDbDataType.adSingle, (int) DbType.Single,
            (int) OleDbDataType.adSmallInt, (int) DbType.Int16, 
            (int) OleDbDataType.adTinyInt, (int) DbType.SByte,
            (int) OleDbDataType.adUnsignedBigInt, (int) DbType.UInt64, 
            (int) OleDbDataType.adUnsignedInt, (int) DbType.UInt32, 
            (int) OleDbDataType.adUnsignedSmallInt, (int) DbType.UInt16,
            (int) OleDbDataType.adUnsignedTinyInt, (int) DbType.Byte, 
            (int) OleDbDataType.adVarBinary, (int) DbType.Binary,
            (int) OleDbDataType.adVarChar, (int) DbType.AnsiString,
            (int) OleDbDataType.adVarNumeric, (int) DbType.VarNumeric,
            (int) OleDbDataType.adVarWChar, (int) DbType.String, 
            (int) OleDbDataType.adVariant, (int) DbType.Object,
            (int) OleDbDataType.adWChar, (int) DbType.String 
        }; 

        private static object[] sqlTypeToSqlDbTypeMap = new object[] { 
            typeof(SqlBinary),  SqlDbType.Binary,
            typeof(SqlInt64),  SqlDbType.BigInt,
            typeof(SqlBoolean), SqlDbType.Bit,
            typeof(SqlString),  SqlDbType.Char, 
            typeof(SqlDateTime),  SqlDbType.DateTime,
            typeof(SqlDecimal),  SqlDbType.Decimal, 
            typeof(SqlDouble),  SqlDbType.Float, 
            typeof(SqlBinary),  SqlDbType.Image,
            typeof(SqlInt32),  SqlDbType.Int, 
            typeof(SqlMoney),  SqlDbType.Money,
            typeof(SqlString),  SqlDbType.NChar,
            typeof(SqlString),  SqlDbType.NText,
            typeof(SqlString),  SqlDbType.NVarChar, 
            typeof(SqlSingle),  SqlDbType.Real,
            typeof(SqlDateTime),  SqlDbType.SmallDateTime, 
            typeof(SqlInt16),  SqlDbType.SmallInt, 
            typeof(SqlMoney),  SqlDbType.SmallMoney,
            typeof(Object),  SqlDbType.Variant, 
            typeof(SqlString),  SqlDbType.VarChar,
            typeof(SqlString),  SqlDbType.Text,
            typeof(SqlBinary),  SqlDbType.Timestamp,
            typeof(SqlByte),  SqlDbType.TinyInt, 
            typeof(SqlBinary),  SqlDbType.VarBinary,
            typeof(SqlString),  SqlDbType.VarChar, 
            typeof(SqlGuid),  SqlDbType.UniqueIdentifier 
        };
 

        private static object[] sqlTypeToUrtType = new object[] {
            typeof(SqlBinary),   typeof(System.Byte[]),
            typeof(SqlByte),     typeof(System.Byte), 
            typeof(SqlDecimal),  typeof(System.Decimal),
            typeof(SqlDouble),   typeof(System.Double), 
            typeof(SqlGuid),     typeof(System.Guid), 
            typeof(SqlString),   typeof(System.String),
            typeof(SqlSingle),   typeof(System.Single), 
            typeof(SqlDateTime), typeof(System.DateTime),
            typeof(SqlInt16),    typeof(System.Int16),
            typeof(SqlInt32),    typeof(System.Int32),
            typeof(SqlInt64),    typeof(System.Int64), 
            typeof(SqlMoney),    typeof(System.Decimal),
            typeof(Object),      typeof(System.Object) 
        }; 

        private static object[] dbTypeToUrtTypeMap = new object[] { 
            DbType.AnsiString, typeof(System.String),
            DbType.AnsiStringFixedLength, typeof(System.String),
            DbType.Binary, typeof(System.Byte[]),
            DbType.Boolean, typeof(System.Boolean), 
            DbType.Byte, typeof(System.Byte),
            DbType.Currency, typeof(System.Decimal), 
            DbType.Date, typeof(System.DateTime), 
            DbType.DateTime, typeof(System.DateTime),
            DbType.DateTime2, typeof(System.DateTime), 
            DbType.DateTimeOffset, typeof(System.DateTimeOffset),
            DbType.Decimal, typeof(System.Decimal),
            DbType.Double, typeof(System.Double),
            DbType.Guid, typeof(System.Guid), 
            DbType.Int16, typeof(System.Int16),
            DbType.Int32, typeof(System.Int32), 
            DbType.Int64, typeof(System.Int64), 
            DbType.Object, typeof(System.Object),
            DbType.SByte, typeof(System.Byte), 
            DbType.Single, typeof(float),
            DbType.String, typeof(System.String),
            DbType.StringFixedLength, typeof(System.String),
            DbType.Time, typeof(System.DateTime), 
            DbType.UInt16, typeof(System.UInt16),
            DbType.UInt32, typeof(System.UInt32), 
            DbType.UInt64, typeof(System.UInt64), 
            DbType.VarNumeric, typeof(System.Decimal)
        }; 


        internal static Type SqlDbTypeToSqlType(SqlDbType sqlDbType) {
            for(int i = 1; i < sqlTypeToSqlDbTypeMap.Length; i += 2) { 
                if(sqlDbType == (SqlDbType) sqlTypeToSqlDbTypeMap[i]) {
                    return (Type) sqlTypeToSqlDbTypeMap[i-1]; 
                } 
            }
 
            return null;
        }

 
        //internal static Type SqlTypeToUrtType(Type sqlType) {
        //    for (int i = 0; i < sqlTypeToUrtType.Length; i += 2){ 
        //        if (sqlType == (Type)sqlTypeToUrtType[i]){ 
        //            return (Type)sqlTypeToUrtType[i + 1];
        //        } 
        //    }

        //    return null;
        //} 

 
        //internal static Type UrtTypeToSqlType(Type urtType) { 
        //    for (int i = 1; i < sqlTypeToUrtType.Length; i += 2) {
        //        if (urtType == sqlTypeToUrtType[i]) { 
        //            return (Type)sqlTypeToUrtType[i - 1];
        //        }
        //    }
 
        //    return null;
        //} 
 
        internal static Type DbTypeToUrtType(DbType dbType) {
            for (int i = 0; i < dbTypeToUrtTypeMap.Length; i += 2) { 
                if (dbType == (DbType) dbTypeToUrtTypeMap[i]) {
                    return (Type)dbTypeToUrtTypeMap[i + 1];
                }
            } 

            return null; 
        } 

 
        //internal static ParameterDirection OleDbToAdoPlusDirection(int oleDbDirection) {
        //    return (ParameterDirection) DoMapping( oleDbToAdoPlusDirectionMap, oleDbDirection, (int) ParameterDirection.Input );
        //}
 

        //internal static Type OleDbToUrtType( int oleDbType ) { 
        //    Type returnType = null; 
        //    bool isArray = (oleDbType & (int) OleDbDataType.adArray) > 0;
        //    oleDbType = oleDbType & ~((int) OleDbDataType.adArray); 

        //    switch (oleDbType) {
        //        case (int) OleDbDataType.adSmallInt:
        //            returnType = typeof(Int16); 
        //            break;
        //        case (int) OleDbDataType.adInteger: 
        //            returnType = typeof(Int32); 
        //            break;
        //        case (int) OleDbDataType.adSingle: 
        //            returnType = typeof(Single);
        //            break;
        //        case (int) OleDbDataType.adDouble:
        //            returnType = typeof(Double); 
        //            break;
        //        case (int) OleDbDataType.adCurrency: 
        //        case (int) OleDbDataType.adNumeric: 
        //        case (int) OleDbDataType.adDecimal:
        //            returnType = typeof(Decimal); 
        //            break;
        //        case (int) OleDbDataType.adDate:
        //        case (int) OleDbDataType.adDBDate:
        //        case (int) OleDbDataType.adDBTimeStamp: 
        //            returnType = typeof(DateTime);
        //            break; 
        //        case (int) OleDbDataType.adBSTR: 
        //        case (int) OleDbDataType.adChar:
        //        case (int) OleDbDataType.adWChar: 
        //            returnType = typeof(String);
        //            break;
        //        case (int) OleDbDataType.adEmpty:
        //        case (int) OleDbDataType.adIDispatch: 
        //        case (int) OleDbDataType.adVariant:
        //        case (int) OleDbDataType.adIUnknown: 
        //            returnType = typeof(Object); 
        //            break;
        //        case (int) OleDbDataType.adError: 
        //            returnType = typeof(Exception);
        //            break;
        //        case (int) OleDbDataType.adBoolean:
        //            returnType = typeof(Boolean); 
        //            break;
        //        case (int) OleDbDataType.adUnsignedTinyInt: 
        //            returnType = typeof(Byte); 
        //            break;
        //        case (int) OleDbDataType.adTinyInt: 
        //            returnType = typeof(SByte);
        //            break;
        //        case (int) OleDbDataType.adUnsignedSmallInt:
        //            returnType = typeof(UInt16); 
        //            break;
        //        case (int) OleDbDataType.adUnsignedInt: 
        //            returnType = typeof(UInt32); 
        //            break;
        //        case (int) OleDbDataType.adBigInt: 
        //            returnType = typeof(Int64);
        //            break;
        //        case (int) OleDbDataType.adUnsignedBigInt:
        //            returnType = typeof(UInt64); 
        //            break;
        //        case (int) OleDbDataType.adGUID: 
        //            returnType = typeof(Guid); 
        //            break;
        //        case (int) OleDbDataType.adBinary: 
        //            returnType = typeof(Byte[]);
        //            break;
        //        case (int) OleDbDataType.adDBTime:
        //            returnType = typeof(TimeSpan); 
        //            break;
        //        default: 
        //            returnType = typeof(Object); 
        //            break;
        //    } 

        //    //

 

        //internal static DbType OleDbToDbType( int oleDbType ) { 
        //    return (DbType) DoMapping( oleDbTypeToDbTypeMap, oleDbType, (int) DbType.Object ); 
        //}
 

        //private static int DoMapping( int[] mapTable, int from, int defaultTarget ) {
        //    for (int i = 0; i< mapTable.Length; i= i+2) {
        //        if( mapTable[i] == from ) 
        //            return mapTable[i+1];
        //    } 
 
        //    return defaultTarget;
        //} 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
