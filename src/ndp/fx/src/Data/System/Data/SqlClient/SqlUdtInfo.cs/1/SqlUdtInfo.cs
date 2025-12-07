//------------------------------------------------------------------------------ 
//  <copyright file="SqlUdtInfo.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All Rights Reserved.
//     Information Contained Herein is Proprietary and Confidential.
//  </copyright> 
// <owner current="true" primary="true">[....]</owner>
// <owner current="true" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.SqlClient { 

    using System;
    using System.Collections;
    using System.Data.Common; 
    using System.Data.Sql;
    using System.Data.SqlTypes; 
    using System.Diagnostics; 
    using System.Text;
    using System.IO; 
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary; 
    using System.Reflection.Emit;
    using System.Security.Permissions; 
 
    using Microsoft.SqlServer.Server;
 
    internal class SqlUdtInfo {
        internal readonly Microsoft.SqlServer.Server.Format SerializationFormat;
        internal readonly bool IsByteOrdered;
        internal readonly bool IsFixedLength; 
        internal readonly int MaxByteSize;
        internal readonly string Name; 
        internal readonly string ValidationMethodName; 

        private SqlUdtInfo(Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute attr) { 
            SerializationFormat = (Microsoft.SqlServer.Server.Format)attr.Format;
            IsByteOrdered       = attr.IsByteOrdered;
            IsFixedLength       = attr.IsFixedLength;
            MaxByteSize         = attr.MaxByteSize; 
            Name                = attr.Name;
            ValidationMethodName= attr.ValidationMethodName; 
        } 
#if NOUDTATTRIBUTEUSAGE
//#include <windows.h> 
//#include <objbase.h>
//#include <cor.h>
//
 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 

        private SqlUdtInfo(Microsoft.SqlServer.Server.Format serializationFormat, bool isByteOrdered, bool isFixedLength, int maxByteSize, string validationMethodName, string name) {
            SerializationFormat = serializationFormat;
            IsByteOrdered       = isByteOrdered; 
            IsFixedLength       = isFixedLength;
            MaxByteSize         = maxByteSize; 
            Name                = name; 
            ValidationMethodName= validationMethodName;
        } 

        internal static SqlUdtInfo GetFromType(Type target) {
            byte[] blob = TryGetUdtAttributeBlob(target);
            if (null == blob) { 
                throw System.Data.Sql.InvalidUdtException.Create(target, Res.SqlUdtReason_NoUdtAttribute);
            } 
 
            SqlUdtInfo result = UnpackUdtAttributeBlob(target, blob);
            return result; 
        }

        private static Exception InvalidUdtAttribute(Type target) {
            return System.Data.Sql.InvalidUdtException.Create(target, Res.SqlUdtReason_InvalidUdtAttribute); 
        }
 
        internal static SqlUdtInfo TryGetFromType(Type target) { 
            SqlUdtInfo result = null;
            byte[] blob = TryGetUdtAttributeBlob(target); 

            if (null != blob) {
                result = UnpackUdtAttributeBlob(target, blob);
            } 

            return result; 
        } 
        //
 



 

 
 

 



 

 
 

 


        private static bool UnpackBooleanValue(Type target, byte[] blob, ref int offset) {
            if (offset + 1 > blob.Length) { 
                throw InvalidUdtAttribute(target); // too short to read binary value;
            } 
 
            bool result = (blob[offset] == 1);
            offset += sizeof(byte); 
            return result;
        }

        private static int UnpackInt32Value(Type target, byte[] blob, ref int offset) { 
            if (offset + 4 > blob.Length) {
                throw InvalidUdtAttribute(target); // too short to read int32 value; 
            } 

            int result = BitConverter.ToInt32(blob, offset);; 
            offset += sizeof(int);
            return result;
        }
 
        private static string UnpackStringValue(Type target, byte[] blob, ref int offset) {
            if (offset + 1 > blob.Length) { 
                throw InvalidUdtAttribute(target); // too short to read utf8 string length; 
            }
 
            byte stringLength = blob[offset];
            offset += sizeof(byte);

            if (offset + stringLength > blob.Length) { 
                throw InvalidUdtAttribute(target); // too short to read utf8 string value;
            } 
 
            string result = System.Text.Encoding.UTF8.GetString(blob, offset, stringLength);
            offset += stringLength; 
            return result;
        }

        // Copied from CorElementType in cor.h 
        private const byte ELEMENT_TYPE_END = 0x0;
        private const byte ELEMENT_TYPE_VOID = 0x1; 
        private const byte ELEMENT_TYPE_BOOLEAN = 0x2; 
        private const byte ELEMENT_TYPE_CHAR = 0x3;
        private const byte ELEMENT_TYPE_I1 = 0x4; 
        private const byte ELEMENT_TYPE_U1 = 0x5;
        private const byte ELEMENT_TYPE_I2 = 0x6;
        private const byte ELEMENT_TYPE_U2 = 0x7;
        private const byte ELEMENT_TYPE_I4 = 0x8; 
        private const byte ELEMENT_TYPE_U4 = 0x9;
        private const byte ELEMENT_TYPE_I8 = 0xa; 
        private const byte ELEMENT_TYPE_U8 = 0xb; 
        private const byte ELEMENT_TYPE_R4 = 0xc;
        private const byte ELEMENT_TYPE_R8 = 0xd; 
        private const byte ELEMENT_TYPE_STRING = 0xe;
        private const byte ELEMENT_TYPE_SZARRAY = 0x1D;     // Shortcut for single dimension zero lower bound array

        // Copied from CorSerializationType in cor.h 
        private const byte SERIALIZATION_TYPE_UNDEFINED    = 0;
        private const byte SERIALIZATION_TYPE_BOOLEAN      = ELEMENT_TYPE_BOOLEAN; 
        private const byte SERIALIZATION_TYPE_CHAR         = ELEMENT_TYPE_CHAR; 
        private const byte SERIALIZATION_TYPE_I1           = ELEMENT_TYPE_I1;
        private const byte SERIALIZATION_TYPE_U1           = ELEMENT_TYPE_U1; 
        private const byte SERIALIZATION_TYPE_I2           = ELEMENT_TYPE_I2;
        private const byte SERIALIZATION_TYPE_U2           = ELEMENT_TYPE_U2;
        private const byte SERIALIZATION_TYPE_I4           = ELEMENT_TYPE_I4;
        private const byte SERIALIZATION_TYPE_U4           = ELEMENT_TYPE_U4; 
        private const byte SERIALIZATION_TYPE_I8           = ELEMENT_TYPE_I8;
        private const byte SERIALIZATION_TYPE_U8           = ELEMENT_TYPE_U8; 
        private const byte SERIALIZATION_TYPE_R4           = ELEMENT_TYPE_R4; 
        private const byte SERIALIZATION_TYPE_R8           = ELEMENT_TYPE_R8;
        private const byte SERIALIZATION_TYPE_STRING       = ELEMENT_TYPE_STRING; 
        private const byte SERIALIZATION_TYPE_SZARRAY      = ELEMENT_TYPE_SZARRAY; // Shortcut for single dimension zero lower bound array
        private const byte SERIALIZATION_TYPE_TYPE         = 0x50;
        private const byte SERIALIZATION_TYPE_TAGGED_OBJECT= 0x51;
        private const byte SERIALIZATION_TYPE_FIELD        = 0x53; 
        private const byte SERIALIZATION_TYPE_PROPERTY     = 0x54;
        private const byte SERIALIZATION_TYPE_ENUM         = 0x55; 
 

        private static SqlUdtInfo UnpackUdtAttributeBlob(Type target, byte[] blob) { 

            Format serializationFormat = 0;
            bool isByteOrdered = false;
            bool isFixedLength = false; 
            int maxByteSize = 0;
            string validationMethodName = null; 
            string name = null; 

            int offset = 0; 

            // The attribute blob must contain at least the signature (2 bytes)
            // + serialization format (4 bytes) + count of name/value pairs (2 bytes)
            if (sizeof(Int16) + sizeof(Int32) + sizeof(Int16) > blob.Length) { 
                throw InvalidUdtAttribute(target); // too short of a blob;
            } 
 
            // All attributes start with a signature: 0x0001
            if (0x0001 != BitConverter.ToInt16(blob, offset)) { 
                throw InvalidUdtAttribute(target); // wrong signature;
            }

            offset += 2; 

            serializationFormat = (Format)BitConverter.ToInt32(blob, offset); 
            offset += sizeof(Int32); 

            // Only argument to the private constructor of this attribute is the 
            // serialization format, which must be either Native or UserDefined
            // (or Structured for WINFS)
            if (Format.Native      != serializationFormat
             && Format.UserDefined != serializationFormat 
#if WINFSFunctionality
             && Format.Structured  != serializationFormat 
#endif 
            ) {
                throw InvalidUdtAttribute(target); // unknown serialization format; 
            }

            short nameValuePairCount = BitConverter.ToInt16(blob, offset);  //
            offset += sizeof(Int16); 

            for (int i = 0; i < nameValuePairCount; i++) { 
                // Need to have enough blob left to read the name value pair... 
                if (offset + 2 > blob.Length) {
                    throw InvalidUdtAttribute(target); // too short for name value pair; 
                }

                byte targetType              = blob[offset];
                offset += sizeof(byte); 

                byte targetSerializationCode = blob[offset]; 
                offset += sizeof(byte); 

                // We only have property setters on the udt attribute, so we don't 
                // expect anything but property setters to be in the attributes
                // blob.
                if (SERIALIZATION_TYPE_PROPERTY != targetType) {
                    throw InvalidUdtAttribute(target); // unexpected serialization type; 
                }
 
                string targetName = UnpackStringValue(target, blob, ref offset); 

                if (SERIALIZATION_TYPE_BOOLEAN == targetSerializationCode      && targetName == "IsByteOrdered" ) { 
                    isByteOrdered = UnpackBooleanValue(target, blob, ref offset);
                }
                else if (SERIALIZATION_TYPE_BOOLEAN == targetSerializationCode && targetName == "IsFixedLength" ) {
                    isFixedLength = UnpackBooleanValue(target, blob, ref offset); 
                }
                else if (SERIALIZATION_TYPE_I4      == targetSerializationCode && targetName == "MaxByteSize" ) { 
                    maxByteSize = UnpackInt32Value(target, blob, ref offset); 
                }
                else if (SERIALIZATION_TYPE_STRING  == targetSerializationCode && targetName == "ValidationMethodName" ) { 
                    validationMethodName = UnpackStringValue(target, blob, ref offset);
                }
                else if (SERIALIZATION_TYPE_STRING  == targetSerializationCode && targetName == "Name" ) {
                    name = UnpackStringValue(target, blob, ref offset); 
                }
                else { 
                    throw InvalidUdtAttribute(target); // invalid targetType/targetSerializationCode combination; 
                }
            } 

            SqlUdtInfo result = new SqlUdtInfo(serializationFormat, isByteOrdered, isFixedLength, maxByteSize, validationMethodName, name);
            return result;
        } 
#else //UDTATTRIBUTEUSAGE
        internal static SqlUdtInfo GetFromType(Type target) { 
            SqlUdtInfo udtAttr = TryGetFromType(target); 
            if (udtAttr == null) {
                throw InvalidUdtException.Create(target, Res.SqlUdtReason_NoUdtAttribute); 
            }
            return udtAttr;
        }
 
        internal static SqlUdtInfo TryGetFromType(Type target) {
            SqlUdtInfo udtAttr = null; 
            object[] attr = target.GetCustomAttributes(typeof(Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute), false); 
            if (attr != null && attr.Length == 1) {
                udtAttr = new SqlUdtInfo((Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute) attr[0]); 
            }
            return udtAttr;
        }
#endif //UDTATTRIBUTEUSAGE 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
//  <copyright file="SqlUdtInfo.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All Rights Reserved.
//     Information Contained Herein is Proprietary and Confidential.
//  </copyright> 
// <owner current="true" primary="true">[....]</owner>
// <owner current="true" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.SqlClient { 

    using System;
    using System.Collections;
    using System.Data.Common; 
    using System.Data.Sql;
    using System.Data.SqlTypes; 
    using System.Diagnostics; 
    using System.Text;
    using System.IO; 
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary; 
    using System.Reflection.Emit;
    using System.Security.Permissions; 
 
    using Microsoft.SqlServer.Server;
 
    internal class SqlUdtInfo {
        internal readonly Microsoft.SqlServer.Server.Format SerializationFormat;
        internal readonly bool IsByteOrdered;
        internal readonly bool IsFixedLength; 
        internal readonly int MaxByteSize;
        internal readonly string Name; 
        internal readonly string ValidationMethodName; 

        private SqlUdtInfo(Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute attr) { 
            SerializationFormat = (Microsoft.SqlServer.Server.Format)attr.Format;
            IsByteOrdered       = attr.IsByteOrdered;
            IsFixedLength       = attr.IsFixedLength;
            MaxByteSize         = attr.MaxByteSize; 
            Name                = attr.Name;
            ValidationMethodName= attr.ValidationMethodName; 
        } 
#if NOUDTATTRIBUTEUSAGE
//#include <windows.h> 
//#include <objbase.h>
//#include <cor.h>
//
 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 

        private SqlUdtInfo(Microsoft.SqlServer.Server.Format serializationFormat, bool isByteOrdered, bool isFixedLength, int maxByteSize, string validationMethodName, string name) {
            SerializationFormat = serializationFormat;
            IsByteOrdered       = isByteOrdered; 
            IsFixedLength       = isFixedLength;
            MaxByteSize         = maxByteSize; 
            Name                = name; 
            ValidationMethodName= validationMethodName;
        } 

        internal static SqlUdtInfo GetFromType(Type target) {
            byte[] blob = TryGetUdtAttributeBlob(target);
            if (null == blob) { 
                throw System.Data.Sql.InvalidUdtException.Create(target, Res.SqlUdtReason_NoUdtAttribute);
            } 
 
            SqlUdtInfo result = UnpackUdtAttributeBlob(target, blob);
            return result; 
        }

        private static Exception InvalidUdtAttribute(Type target) {
            return System.Data.Sql.InvalidUdtException.Create(target, Res.SqlUdtReason_InvalidUdtAttribute); 
        }
 
        internal static SqlUdtInfo TryGetFromType(Type target) { 
            SqlUdtInfo result = null;
            byte[] blob = TryGetUdtAttributeBlob(target); 

            if (null != blob) {
                result = UnpackUdtAttributeBlob(target, blob);
            } 

            return result; 
        } 
        //
 



 

 
 

 



 

 
 

 


        private static bool UnpackBooleanValue(Type target, byte[] blob, ref int offset) {
            if (offset + 1 > blob.Length) { 
                throw InvalidUdtAttribute(target); // too short to read binary value;
            } 
 
            bool result = (blob[offset] == 1);
            offset += sizeof(byte); 
            return result;
        }

        private static int UnpackInt32Value(Type target, byte[] blob, ref int offset) { 
            if (offset + 4 > blob.Length) {
                throw InvalidUdtAttribute(target); // too short to read int32 value; 
            } 

            int result = BitConverter.ToInt32(blob, offset);; 
            offset += sizeof(int);
            return result;
        }
 
        private static string UnpackStringValue(Type target, byte[] blob, ref int offset) {
            if (offset + 1 > blob.Length) { 
                throw InvalidUdtAttribute(target); // too short to read utf8 string length; 
            }
 
            byte stringLength = blob[offset];
            offset += sizeof(byte);

            if (offset + stringLength > blob.Length) { 
                throw InvalidUdtAttribute(target); // too short to read utf8 string value;
            } 
 
            string result = System.Text.Encoding.UTF8.GetString(blob, offset, stringLength);
            offset += stringLength; 
            return result;
        }

        // Copied from CorElementType in cor.h 
        private const byte ELEMENT_TYPE_END = 0x0;
        private const byte ELEMENT_TYPE_VOID = 0x1; 
        private const byte ELEMENT_TYPE_BOOLEAN = 0x2; 
        private const byte ELEMENT_TYPE_CHAR = 0x3;
        private const byte ELEMENT_TYPE_I1 = 0x4; 
        private const byte ELEMENT_TYPE_U1 = 0x5;
        private const byte ELEMENT_TYPE_I2 = 0x6;
        private const byte ELEMENT_TYPE_U2 = 0x7;
        private const byte ELEMENT_TYPE_I4 = 0x8; 
        private const byte ELEMENT_TYPE_U4 = 0x9;
        private const byte ELEMENT_TYPE_I8 = 0xa; 
        private const byte ELEMENT_TYPE_U8 = 0xb; 
        private const byte ELEMENT_TYPE_R4 = 0xc;
        private const byte ELEMENT_TYPE_R8 = 0xd; 
        private const byte ELEMENT_TYPE_STRING = 0xe;
        private const byte ELEMENT_TYPE_SZARRAY = 0x1D;     // Shortcut for single dimension zero lower bound array

        // Copied from CorSerializationType in cor.h 
        private const byte SERIALIZATION_TYPE_UNDEFINED    = 0;
        private const byte SERIALIZATION_TYPE_BOOLEAN      = ELEMENT_TYPE_BOOLEAN; 
        private const byte SERIALIZATION_TYPE_CHAR         = ELEMENT_TYPE_CHAR; 
        private const byte SERIALIZATION_TYPE_I1           = ELEMENT_TYPE_I1;
        private const byte SERIALIZATION_TYPE_U1           = ELEMENT_TYPE_U1; 
        private const byte SERIALIZATION_TYPE_I2           = ELEMENT_TYPE_I2;
        private const byte SERIALIZATION_TYPE_U2           = ELEMENT_TYPE_U2;
        private const byte SERIALIZATION_TYPE_I4           = ELEMENT_TYPE_I4;
        private const byte SERIALIZATION_TYPE_U4           = ELEMENT_TYPE_U4; 
        private const byte SERIALIZATION_TYPE_I8           = ELEMENT_TYPE_I8;
        private const byte SERIALIZATION_TYPE_U8           = ELEMENT_TYPE_U8; 
        private const byte SERIALIZATION_TYPE_R4           = ELEMENT_TYPE_R4; 
        private const byte SERIALIZATION_TYPE_R8           = ELEMENT_TYPE_R8;
        private const byte SERIALIZATION_TYPE_STRING       = ELEMENT_TYPE_STRING; 
        private const byte SERIALIZATION_TYPE_SZARRAY      = ELEMENT_TYPE_SZARRAY; // Shortcut for single dimension zero lower bound array
        private const byte SERIALIZATION_TYPE_TYPE         = 0x50;
        private const byte SERIALIZATION_TYPE_TAGGED_OBJECT= 0x51;
        private const byte SERIALIZATION_TYPE_FIELD        = 0x53; 
        private const byte SERIALIZATION_TYPE_PROPERTY     = 0x54;
        private const byte SERIALIZATION_TYPE_ENUM         = 0x55; 
 

        private static SqlUdtInfo UnpackUdtAttributeBlob(Type target, byte[] blob) { 

            Format serializationFormat = 0;
            bool isByteOrdered = false;
            bool isFixedLength = false; 
            int maxByteSize = 0;
            string validationMethodName = null; 
            string name = null; 

            int offset = 0; 

            // The attribute blob must contain at least the signature (2 bytes)
            // + serialization format (4 bytes) + count of name/value pairs (2 bytes)
            if (sizeof(Int16) + sizeof(Int32) + sizeof(Int16) > blob.Length) { 
                throw InvalidUdtAttribute(target); // too short of a blob;
            } 
 
            // All attributes start with a signature: 0x0001
            if (0x0001 != BitConverter.ToInt16(blob, offset)) { 
                throw InvalidUdtAttribute(target); // wrong signature;
            }

            offset += 2; 

            serializationFormat = (Format)BitConverter.ToInt32(blob, offset); 
            offset += sizeof(Int32); 

            // Only argument to the private constructor of this attribute is the 
            // serialization format, which must be either Native or UserDefined
            // (or Structured for WINFS)
            if (Format.Native      != serializationFormat
             && Format.UserDefined != serializationFormat 
#if WINFSFunctionality
             && Format.Structured  != serializationFormat 
#endif 
            ) {
                throw InvalidUdtAttribute(target); // unknown serialization format; 
            }

            short nameValuePairCount = BitConverter.ToInt16(blob, offset);  //
            offset += sizeof(Int16); 

            for (int i = 0; i < nameValuePairCount; i++) { 
                // Need to have enough blob left to read the name value pair... 
                if (offset + 2 > blob.Length) {
                    throw InvalidUdtAttribute(target); // too short for name value pair; 
                }

                byte targetType              = blob[offset];
                offset += sizeof(byte); 

                byte targetSerializationCode = blob[offset]; 
                offset += sizeof(byte); 

                // We only have property setters on the udt attribute, so we don't 
                // expect anything but property setters to be in the attributes
                // blob.
                if (SERIALIZATION_TYPE_PROPERTY != targetType) {
                    throw InvalidUdtAttribute(target); // unexpected serialization type; 
                }
 
                string targetName = UnpackStringValue(target, blob, ref offset); 

                if (SERIALIZATION_TYPE_BOOLEAN == targetSerializationCode      && targetName == "IsByteOrdered" ) { 
                    isByteOrdered = UnpackBooleanValue(target, blob, ref offset);
                }
                else if (SERIALIZATION_TYPE_BOOLEAN == targetSerializationCode && targetName == "IsFixedLength" ) {
                    isFixedLength = UnpackBooleanValue(target, blob, ref offset); 
                }
                else if (SERIALIZATION_TYPE_I4      == targetSerializationCode && targetName == "MaxByteSize" ) { 
                    maxByteSize = UnpackInt32Value(target, blob, ref offset); 
                }
                else if (SERIALIZATION_TYPE_STRING  == targetSerializationCode && targetName == "ValidationMethodName" ) { 
                    validationMethodName = UnpackStringValue(target, blob, ref offset);
                }
                else if (SERIALIZATION_TYPE_STRING  == targetSerializationCode && targetName == "Name" ) {
                    name = UnpackStringValue(target, blob, ref offset); 
                }
                else { 
                    throw InvalidUdtAttribute(target); // invalid targetType/targetSerializationCode combination; 
                }
            } 

            SqlUdtInfo result = new SqlUdtInfo(serializationFormat, isByteOrdered, isFixedLength, maxByteSize, validationMethodName, name);
            return result;
        } 
#else //UDTATTRIBUTEUSAGE
        internal static SqlUdtInfo GetFromType(Type target) { 
            SqlUdtInfo udtAttr = TryGetFromType(target); 
            if (udtAttr == null) {
                throw InvalidUdtException.Create(target, Res.SqlUdtReason_NoUdtAttribute); 
            }
            return udtAttr;
        }
 
        internal static SqlUdtInfo TryGetFromType(Type target) {
            SqlUdtInfo udtAttr = null; 
            object[] attr = target.GetCustomAttributes(typeof(Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute), false); 
            if (attr != null && attr.Length == 1) {
                udtAttr = new SqlUdtInfo((Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute) attr[0]); 
            }
            return udtAttr;
        }
#endif //UDTATTRIBUTEUSAGE 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
