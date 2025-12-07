//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
 
namespace System.Data.Design {
 
    using System;


 
    internal class ConversionHelper {
 
        /// <summary> 
        /// Private contstructor to avoid class being instantiated.
        /// </summary> 
        private ConversionHelper() {
        }

        // Convertion table for the System.Convert class. Info taken from Convert.cs. 
        // Lines represent the 'From', columns represent the 'To'.
        // E.g. line 1, col 2 indicates that a boolean cannot be converted to a char. 
        // This table can be compressed much further but it makes the code less readable. 
        private static Int16[] urtConversionTable = {	0x5FFD, /*101111111111101*/ // Boolean
                                                        0x3FE1, /*011111111100001*/ // Char 
                                                        0x7FFD, /*111111111111101*/ // SByte
                                                        0x7FFD, /*111111111111101*/ // Byte
                                                        0x7FFD, /*111111111111101*/ // Int16
                                                        0x7FFD, /*111111111111101*/ // UInt16 
                                                        0x7FFD, /*111111111111101*/ // Int32
                                                        0x7FFD, /*111111111111101*/ // UInt32 
                                                        0x7FFD, /*111111111111101*/ // Int64 
                                                        0x7FFD, /*111111111111101*/ // UInt64
                                                        0x5FFD, /*101111111111101*/ // Single 
                                                        0x5FFD, /*101111111111101*/ // Double
                                                        0x5FFD, /*101111111111101*/ // Decimal
                                                        0x0003, /*000000000000011*/ // DateTime
                                                        0x7FFF, /*111111111111111*/ // String 
        };
 
        // This table represents the safe conversions, i.e. the ones that always succeed. 
        private static Int16[] urtSafeConversionTable = {	0x5FFD, /*101111111111101*/ // Boolean
                                                            0x3FE1, /*011111111100001*/ // Char 
                                                            0x7FFD, /*111111111111101*/ // SByte
                                                            0x7FFD, /*111111111111101*/ // Byte
                                                            0x7FFD, /*111111111111101*/ // Int16
                                                            0x7FFD, /*111111111111101*/ // UInt16 
                                                            0x7FFD, /*111111111111101*/ // Int32
                                                            0x7FFD, /*111111111111101*/ // UInt32 
                                                            0x7FFD, /*111111111111101*/ // Int64 
                                                            0x7FFD, /*111111111111101*/ // UInt64
                                                            0x5FFD, /*101111111111101*/ // Single 
                                                            0x5FFD, /*101111111111101*/ // Double
                                                            0x5FFD, /*101111111111101*/ // Decimal
                                                            0x0003, /*000000000000011*/ // DateTime
                                                            0x0001, /*000000000000001*/ // String 
        };
 
 
        // The index of the type in this table indicates the column/line to lookup for in conversion table.
        // E.g. SByte is at index 3, means that line 3 and column 3 of conv table represent SByte. 
        private static Type[] urtTypeIndexTable = {	
            typeof(System.Boolean),
            typeof(System.Char),
            typeof(System.SByte), 
            typeof(System.Byte),
            typeof(System.Int16), 
            typeof(System.UInt16), 
            typeof(System.Int32),
            typeof(System.UInt32), 
            typeof(System.Int64),
            typeof(System.UInt64),
            typeof(System.Single),
            typeof(System.Double), 
            typeof(System.Decimal),
            typeof(System.DateTime), 
            typeof(System.String) 
        };
 

        // tells if the conversion from sourceUrtType to destinationUrtType is possible at all.
        internal static bool CanConvert(Type sourceUrtType, Type destinationUrtType) {
            Int16 lineIndex = -1, columnIndex = -1; 

            for (Int16 i = 0; i < urtTypeIndexTable.Length; i++) { 
                if (sourceUrtType == urtTypeIndexTable[i]) { 
                    lineIndex = i;
                    break; 
                }
            }

            for (Int16 i = 0; i < urtTypeIndexTable.Length; i++) { 
                if (destinationUrtType == urtTypeIndexTable[i]) {
                    columnIndex = i; 
                    break; 
                }
            } 

            if (lineIndex != -1 && columnIndex != -1) {
                Int16 tableEntry = urtConversionTable[lineIndex];
                Int16 mask = (Int16)(0x4000 >> columnIndex); 

                if ((tableEntry & mask) != 0) { 
                    return true; 
                }
            } 

            return false;
        }
 
        // This method assumes that CanConvert was called first and returned true.
        internal static string GetConversionMethodName(Type sourceUrtType, Type targetUrtType) { 
#if DEBUG 
                System.Diagnostics.Debug.Assert(CanConvert(sourceUrtType, targetUrtType), "GetConversionMethodName was called even if cannot convert. Make sure to call CanConvert first.");
#endif 

            return "To" + targetUrtType.Name;
        }
 
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
 
 
namespace System.Data.Design {
 
    using System;


 
    internal class ConversionHelper {
 
        /// <summary> 
        /// Private contstructor to avoid class being instantiated.
        /// </summary> 
        private ConversionHelper() {
        }

        // Convertion table for the System.Convert class. Info taken from Convert.cs. 
        // Lines represent the 'From', columns represent the 'To'.
        // E.g. line 1, col 2 indicates that a boolean cannot be converted to a char. 
        // This table can be compressed much further but it makes the code less readable. 
        private static Int16[] urtConversionTable = {	0x5FFD, /*101111111111101*/ // Boolean
                                                        0x3FE1, /*011111111100001*/ // Char 
                                                        0x7FFD, /*111111111111101*/ // SByte
                                                        0x7FFD, /*111111111111101*/ // Byte
                                                        0x7FFD, /*111111111111101*/ // Int16
                                                        0x7FFD, /*111111111111101*/ // UInt16 
                                                        0x7FFD, /*111111111111101*/ // Int32
                                                        0x7FFD, /*111111111111101*/ // UInt32 
                                                        0x7FFD, /*111111111111101*/ // Int64 
                                                        0x7FFD, /*111111111111101*/ // UInt64
                                                        0x5FFD, /*101111111111101*/ // Single 
                                                        0x5FFD, /*101111111111101*/ // Double
                                                        0x5FFD, /*101111111111101*/ // Decimal
                                                        0x0003, /*000000000000011*/ // DateTime
                                                        0x7FFF, /*111111111111111*/ // String 
        };
 
        // This table represents the safe conversions, i.e. the ones that always succeed. 
        private static Int16[] urtSafeConversionTable = {	0x5FFD, /*101111111111101*/ // Boolean
                                                            0x3FE1, /*011111111100001*/ // Char 
                                                            0x7FFD, /*111111111111101*/ // SByte
                                                            0x7FFD, /*111111111111101*/ // Byte
                                                            0x7FFD, /*111111111111101*/ // Int16
                                                            0x7FFD, /*111111111111101*/ // UInt16 
                                                            0x7FFD, /*111111111111101*/ // Int32
                                                            0x7FFD, /*111111111111101*/ // UInt32 
                                                            0x7FFD, /*111111111111101*/ // Int64 
                                                            0x7FFD, /*111111111111101*/ // UInt64
                                                            0x5FFD, /*101111111111101*/ // Single 
                                                            0x5FFD, /*101111111111101*/ // Double
                                                            0x5FFD, /*101111111111101*/ // Decimal
                                                            0x0003, /*000000000000011*/ // DateTime
                                                            0x0001, /*000000000000001*/ // String 
        };
 
 
        // The index of the type in this table indicates the column/line to lookup for in conversion table.
        // E.g. SByte is at index 3, means that line 3 and column 3 of conv table represent SByte. 
        private static Type[] urtTypeIndexTable = {	
            typeof(System.Boolean),
            typeof(System.Char),
            typeof(System.SByte), 
            typeof(System.Byte),
            typeof(System.Int16), 
            typeof(System.UInt16), 
            typeof(System.Int32),
            typeof(System.UInt32), 
            typeof(System.Int64),
            typeof(System.UInt64),
            typeof(System.Single),
            typeof(System.Double), 
            typeof(System.Decimal),
            typeof(System.DateTime), 
            typeof(System.String) 
        };
 

        // tells if the conversion from sourceUrtType to destinationUrtType is possible at all.
        internal static bool CanConvert(Type sourceUrtType, Type destinationUrtType) {
            Int16 lineIndex = -1, columnIndex = -1; 

            for (Int16 i = 0; i < urtTypeIndexTable.Length; i++) { 
                if (sourceUrtType == urtTypeIndexTable[i]) { 
                    lineIndex = i;
                    break; 
                }
            }

            for (Int16 i = 0; i < urtTypeIndexTable.Length; i++) { 
                if (destinationUrtType == urtTypeIndexTable[i]) {
                    columnIndex = i; 
                    break; 
                }
            } 

            if (lineIndex != -1 && columnIndex != -1) {
                Int16 tableEntry = urtConversionTable[lineIndex];
                Int16 mask = (Int16)(0x4000 >> columnIndex); 

                if ((tableEntry & mask) != 0) { 
                    return true; 
                }
            } 

            return false;
        }
 
        // This method assumes that CanConvert was called first and returned true.
        internal static string GetConversionMethodName(Type sourceUrtType, Type targetUrtType) { 
#if DEBUG 
                System.Diagnostics.Debug.Assert(CanConvert(sourceUrtType, targetUrtType), "GetConversionMethodName was called even if cannot convert. Make sure to call CanConvert first.");
#endif 

            return "To" + targetUrtType.Name;
        }
 
    }
 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
