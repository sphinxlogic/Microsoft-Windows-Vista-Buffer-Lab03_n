//------------------------------------------------------------------------------ 
// <copyright file="DataSetSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Data;
 
    /// <devdoc>
    ///    <para>
    ///       Provides schema information for a data set.
    ///    </para> 
    /// </devdoc>
    public sealed class DataSetSchema : IDataSourceSchema { 
 
        private DataSet _dataSet;
 
        public DataSetSchema(DataSet dataSet) {
            if (dataSet == null) {
                throw new ArgumentNullException("dataSet");
            } 
            _dataSet = dataSet;
        } 
 
        /// <devdoc>
        /// Returns an array of DataSetViewSchema objects that represent the views contained in the datasource. 
        /// The views returned should match the names returned by the runtime method GetViewNames.
        /// </devdoc>
        public IDataSourceViewSchema[] GetViews() {
            DataTableCollection tables = _dataSet.Tables; 
            DataSetViewSchema[] viewSchemas = new DataSetViewSchema[tables.Count];
            for (int i = 0; i < tables.Count; i++) { 
                viewSchemas[i] = new DataSetViewSchema(tables[i]); 
            }
            return viewSchemas; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataSetSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Data;
 
    /// <devdoc>
    ///    <para>
    ///       Provides schema information for a data set.
    ///    </para> 
    /// </devdoc>
    public sealed class DataSetSchema : IDataSourceSchema { 
 
        private DataSet _dataSet;
 
        public DataSetSchema(DataSet dataSet) {
            if (dataSet == null) {
                throw new ArgumentNullException("dataSet");
            } 
            _dataSet = dataSet;
        } 
 
        /// <devdoc>
        /// Returns an array of DataSetViewSchema objects that represent the views contained in the datasource. 
        /// The views returned should match the names returned by the runtime method GetViewNames.
        /// </devdoc>
        public IDataSourceViewSchema[] GetViews() {
            DataTableCollection tables = _dataSet.Tables; 
            DataSetViewSchema[] viewSchemas = new DataSetViewSchema[tables.Count];
            for (int i = 0; i < tables.Count; i++) { 
                viewSchemas[i] = new DataSetViewSchema(tables[i]); 
            }
            return viewSchemas; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
