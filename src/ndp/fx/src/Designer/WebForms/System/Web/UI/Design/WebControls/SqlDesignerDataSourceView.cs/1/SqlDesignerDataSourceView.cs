//------------------------------------------------------------------------------ 
// <copyright file="SqlDesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Data;
    using System.Web.UI.WebControls;

    /// <devdoc> 
    /// SqlDesignerDataSourceView is the designer view associated with a SqlDataSourceDesigner.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class SqlDesignerDataSourceView : DesignerDataSourceView {
        private SqlDataSourceDesigner _owner; 

        public SqlDesignerDataSourceView(SqlDataSourceDesigner owner, string viewName) : base(owner, viewName) {
            _owner = owner;
        } 

        public override bool CanDelete { 
            get { 
                return (_owner.SqlDataSource.DeleteCommand.Length > 0);
            } 
        }

        public override bool CanInsert {
            get { 
                return (_owner.SqlDataSource.InsertCommand.Length > 0);
            } 
        } 

        public override bool CanPage { 
            get {
                return false;
            }
        } 

        public override bool CanRetrieveTotalRowCount { 
            get { 
                return false;
            } 
        }

        public override bool CanSort {
            get { 
                return (_owner.SqlDataSource.DataSourceMode == SqlDataSourceMode.DataSet) || (_owner.SqlDataSource.SortParameterName.Length > 0);
            } 
        } 

        public override bool CanUpdate { 
            get {
                return (_owner.SqlDataSource.UpdateCommand.Length > 0);
            }
        } 

        public override IDataSourceViewSchema Schema { 
            get { 
                DataTable schemaTable = _owner.LoadSchema();
                if (schemaTable == null) { 
                    return null;
                }
                return new DataSetViewSchema(schemaTable);
            } 
        }
 
        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) { 
            DataTable schemaTable = _owner.LoadSchema();
            if (schemaTable != null) { 
                isSampleData = true;
                return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateSampleDataTable(new DataView(schemaTable), true), minimumRows);
            }
 
            // Couldn't find design-time schema, use base implementation
            return base.GetDesignTimeData(minimumRows, out isSampleData); 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Data;
    using System.Web.UI.WebControls;

    /// <devdoc> 
    /// SqlDesignerDataSourceView is the designer view associated with a SqlDataSourceDesigner.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class SqlDesignerDataSourceView : DesignerDataSourceView {
        private SqlDataSourceDesigner _owner; 

        public SqlDesignerDataSourceView(SqlDataSourceDesigner owner, string viewName) : base(owner, viewName) {
            _owner = owner;
        } 

        public override bool CanDelete { 
            get { 
                return (_owner.SqlDataSource.DeleteCommand.Length > 0);
            } 
        }

        public override bool CanInsert {
            get { 
                return (_owner.SqlDataSource.InsertCommand.Length > 0);
            } 
        } 

        public override bool CanPage { 
            get {
                return false;
            }
        } 

        public override bool CanRetrieveTotalRowCount { 
            get { 
                return false;
            } 
        }

        public override bool CanSort {
            get { 
                return (_owner.SqlDataSource.DataSourceMode == SqlDataSourceMode.DataSet) || (_owner.SqlDataSource.SortParameterName.Length > 0);
            } 
        } 

        public override bool CanUpdate { 
            get {
                return (_owner.SqlDataSource.UpdateCommand.Length > 0);
            }
        } 

        public override IDataSourceViewSchema Schema { 
            get { 
                DataTable schemaTable = _owner.LoadSchema();
                if (schemaTable == null) { 
                    return null;
                }
                return new DataSetViewSchema(schemaTable);
            } 
        }
 
        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) { 
            DataTable schemaTable = _owner.LoadSchema();
            if (schemaTable != null) { 
                isSampleData = true;
                return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateSampleDataTable(new DataView(schemaTable), true), minimumRows);
            }
 
            // Couldn't find design-time schema, use base implementation
            return base.GetDesignTimeData(minimumRows, out isSampleData); 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
