 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2001' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
/*
 */ 
namespace System.Data.Design {

    using System;
    using System.Text; 
    using System.Data;
    using System.Data.Common; 
    using System.Data.OleDb; 
    using System.Data.SqlClient;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Forms;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
 
 
    /// <summary>
    ///    This class provides the data class designer specific utilities 
    /// </summary>
    internal sealed class DataDesignUtil {
        internal static string DataSetClassName = typeof(DataSet).ToString();
 
        // private constructor to avoid class being instantiated.
        private DataDesignUtil() { } 
 
        internal enum MappingDirection {
            SourceToDataSet, 
            DataSetToSource
        }
        internal static string[] MapColumnNames( DataColumnMappingCollection mappingCollection, string[] names, MappingDirection direction ) {
            Debug.Assert( mappingCollection != null ); Debug.Assert( names != null ); 
            if( mappingCollection == null || names == null ) {
                return new string[] {}; 
            } 

            ArrayList result = new ArrayList(); 
            string mappedName;
            DataColumnMapping mapping;

            foreach( string columnName in names ) { 
                try {
                    if( direction == MappingDirection.DataSetToSource ) { 
                        mapping = mappingCollection.GetByDataSetColumn( columnName ); 
                        mappedName = mapping.SourceColumn;
                    } 
                    else {
                        mapping = mappingCollection[columnName];
                        mappedName = mapping.DataSetColumn;
                    } 
                }
                catch( System.IndexOutOfRangeException ) { 
                    mappedName = columnName; 
                }
 
                Debug.Assert( StringUtil.NotEmptyAfterTrim(mappedName) );
                result.Add( mappedName );
            }
 
            return (string[]) result.ToArray( typeof(string) );
        } 
 

        // CopyColumn -- Copy column members from src to dest. 
        public static void CopyColumn(DataColumn srcColumn, DataColumn destColumn){
            destColumn.AllowDBNull          = srcColumn.AllowDBNull;
            destColumn.AutoIncrement        = srcColumn.AutoIncrement;
            destColumn.AutoIncrementSeed    = srcColumn.AutoIncrementSeed; 
            destColumn.AutoIncrementStep    = srcColumn.AutoIncrementStep;
            destColumn.Caption              = srcColumn.Caption; 
            destColumn.ColumnMapping        = srcColumn.ColumnMapping; 
            destColumn.ColumnName           = srcColumn.ColumnName;
            destColumn.DataType             = srcColumn.DataType; 
            destColumn.DefaultValue         = srcColumn.DefaultValue;
            destColumn.Expression           = srcColumn.Expression;
            destColumn.MaxLength            = srcColumn.MaxLength;
            destColumn.Prefix               = srcColumn.Prefix; 
            destColumn.ReadOnly             = srcColumn.ReadOnly;
 
            // Do not touch Unique property - setting Unique to true creates a UniqueConstraint 
            // behind the scenes and we do not want it.
        } 


        // CloneColumn -- Creates a copy of the given column.
        public static DataColumn CloneColumn(DataColumn column){ 
            DataColumn newColumn = new DataColumn();
 
            CopyColumn(column, newColumn); 

            return newColumn; 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2001' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
/*
 */ 
namespace System.Data.Design {

    using System;
    using System.Text; 
    using System.Data;
    using System.Data.Common; 
    using System.Data.OleDb; 
    using System.Data.SqlClient;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Forms;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
 
 
    /// <summary>
    ///    This class provides the data class designer specific utilities 
    /// </summary>
    internal sealed class DataDesignUtil {
        internal static string DataSetClassName = typeof(DataSet).ToString();
 
        // private constructor to avoid class being instantiated.
        private DataDesignUtil() { } 
 
        internal enum MappingDirection {
            SourceToDataSet, 
            DataSetToSource
        }
        internal static string[] MapColumnNames( DataColumnMappingCollection mappingCollection, string[] names, MappingDirection direction ) {
            Debug.Assert( mappingCollection != null ); Debug.Assert( names != null ); 
            if( mappingCollection == null || names == null ) {
                return new string[] {}; 
            } 

            ArrayList result = new ArrayList(); 
            string mappedName;
            DataColumnMapping mapping;

            foreach( string columnName in names ) { 
                try {
                    if( direction == MappingDirection.DataSetToSource ) { 
                        mapping = mappingCollection.GetByDataSetColumn( columnName ); 
                        mappedName = mapping.SourceColumn;
                    } 
                    else {
                        mapping = mappingCollection[columnName];
                        mappedName = mapping.DataSetColumn;
                    } 
                }
                catch( System.IndexOutOfRangeException ) { 
                    mappedName = columnName; 
                }
 
                Debug.Assert( StringUtil.NotEmptyAfterTrim(mappedName) );
                result.Add( mappedName );
            }
 
            return (string[]) result.ToArray( typeof(string) );
        } 
 

        // CopyColumn -- Copy column members from src to dest. 
        public static void CopyColumn(DataColumn srcColumn, DataColumn destColumn){
            destColumn.AllowDBNull          = srcColumn.AllowDBNull;
            destColumn.AutoIncrement        = srcColumn.AutoIncrement;
            destColumn.AutoIncrementSeed    = srcColumn.AutoIncrementSeed; 
            destColumn.AutoIncrementStep    = srcColumn.AutoIncrementStep;
            destColumn.Caption              = srcColumn.Caption; 
            destColumn.ColumnMapping        = srcColumn.ColumnMapping; 
            destColumn.ColumnName           = srcColumn.ColumnName;
            destColumn.DataType             = srcColumn.DataType; 
            destColumn.DefaultValue         = srcColumn.DefaultValue;
            destColumn.Expression           = srcColumn.Expression;
            destColumn.MaxLength            = srcColumn.MaxLength;
            destColumn.Prefix               = srcColumn.Prefix; 
            destColumn.ReadOnly             = srcColumn.ReadOnly;
 
            // Do not touch Unique property - setting Unique to true creates a UniqueConstraint 
            // behind the scenes and we do not want it.
        } 


        // CloneColumn -- Creates a copy of the given column.
        public static DataColumn CloneColumn(DataColumn column){ 
            DataColumn newColumn = new DataColumn();
 
            CopyColumn(column, newColumn); 

            return newColumn; 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
