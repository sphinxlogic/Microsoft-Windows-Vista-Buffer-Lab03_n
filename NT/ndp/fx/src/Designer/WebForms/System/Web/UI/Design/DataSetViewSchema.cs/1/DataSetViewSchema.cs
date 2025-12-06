//------------------------------------------------------------------------------ 
// <copyright file="DataSetViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Data;
 
    /// <devdoc>
    ///    <para>
    ///       Provides schema information for a single data source view.  This schema information is used at
    ///       at designtime to make decisions about what views should be shown in the 
    ///       DataMember picker.
    ///    </para> 
    /// </devdoc> 
    public sealed class DataSetViewSchema : IDataSourceViewSchema {
 
        private DataTable _dataTable;

        public DataSetViewSchema(DataTable dataTable) {
            if (dataTable == null) { 
                throw new ArgumentNullException("dataTable");
            } 
            _dataTable = dataTable; 
        }
 
        /// <devdoc>
        /// Returns the name of the view.  This name should match the name returned by the runtime method GetViewNames.
        /// </devdoc>
        public string Name { 
             get {
                 return _dataTable.TableName; 
             } 
        }
 
        /// <devdoc>
        /// Returns an array of IDataSourceViewSchema objects that represent the child views contained in the current view.
        /// </devdoc>
        public IDataSourceViewSchema[] GetChildren() { 
            return null;    // todo: implement for hierarchy
        } 
 
        /// <devdoc>
        /// Returns an array of IDataSourceFieldSchema objects that represent the fields contained in the view. 
        /// </devdoc>
        public IDataSourceFieldSchema[] GetFields() {
            System.Collections.Generic.List<DataSetFieldSchema> fieldSchemas = new System.Collections.Generic.List<DataSetFieldSchema>();
            foreach (DataColumn c in _dataTable.Columns) { 
                if (c.ColumnMapping != MappingType.Hidden) {
                    fieldSchemas.Add(new DataSetFieldSchema(c)); 
                } 
            }
 
            return fieldSchemas.ToArray();
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataSetViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Data;
 
    /// <devdoc>
    ///    <para>
    ///       Provides schema information for a single data source view.  This schema information is used at
    ///       at designtime to make decisions about what views should be shown in the 
    ///       DataMember picker.
    ///    </para> 
    /// </devdoc> 
    public sealed class DataSetViewSchema : IDataSourceViewSchema {
 
        private DataTable _dataTable;

        public DataSetViewSchema(DataTable dataTable) {
            if (dataTable == null) { 
                throw new ArgumentNullException("dataTable");
            } 
            _dataTable = dataTable; 
        }
 
        /// <devdoc>
        /// Returns the name of the view.  This name should match the name returned by the runtime method GetViewNames.
        /// </devdoc>
        public string Name { 
             get {
                 return _dataTable.TableName; 
             } 
        }
 
        /// <devdoc>
        /// Returns an array of IDataSourceViewSchema objects that represent the child views contained in the current view.
        /// </devdoc>
        public IDataSourceViewSchema[] GetChildren() { 
            return null;    // todo: implement for hierarchy
        } 
 
        /// <devdoc>
        /// Returns an array of IDataSourceFieldSchema objects that represent the fields contained in the view. 
        /// </devdoc>
        public IDataSourceFieldSchema[] GetFields() {
            System.Collections.Generic.List<DataSetFieldSchema> fieldSchemas = new System.Collections.Generic.List<DataSetFieldSchema>();
            foreach (DataColumn c in _dataTable.Columns) { 
                if (c.ColumnMapping != MappingType.Hidden) {
                    fieldSchemas.Add(new DataSetFieldSchema(c)); 
                } 
            }
 
            return fieldSchemas.ToArray();
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
