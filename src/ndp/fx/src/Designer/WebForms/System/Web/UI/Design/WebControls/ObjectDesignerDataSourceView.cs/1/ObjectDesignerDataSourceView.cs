//------------------------------------------------------------------------------ 
// <copyright file="ObjectDesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Data;
    using System.Web.UI.WebControls;

    /// <devdoc> 
    /// ObjectDesignerDataSourceView is the designer view associated with a ObjectDataSourceDesigner.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ObjectDesignerDataSourceView : DesignerDataSourceView {
        private ObjectDataSourceDesigner _owner; 

        public ObjectDesignerDataSourceView(ObjectDataSourceDesigner owner, string viewName) : base(owner, viewName) {
            _owner = owner;
        } 

        public override bool CanDelete { 
            get { 
                return (_owner.ObjectDataSource.DeleteMethod.Length > 0);
            } 
        }

        public override bool CanInsert {
            get { 
                return (_owner.ObjectDataSource.InsertMethod.Length > 0);
            } 
        } 

        public override bool CanPage { 
            get {
                return _owner.ObjectDataSource.EnablePaging;
            }
        } 

        public override bool CanRetrieveTotalRowCount { 
            get { 
                return (_owner.ObjectDataSource.SelectCountMethod.Length > 0);
            } 
        }

        public override bool CanSort {
            get { 
                // We can sort if either the business object can do custom sorting,
                // or the return type of the select method is one of a few known types. 
                if (_owner.ObjectDataSource.SortParameterName.Length > 0) { 
                    return true;
                } 

                Type selectMethodReturnType = _owner.SelectMethodReturnType;
                return ((selectMethodReturnType != null) &&
                    (typeof(DataSet).IsAssignableFrom(selectMethodReturnType) || 
                    typeof(DataTable).IsAssignableFrom(selectMethodReturnType) ||
                    typeof(DataView).IsAssignableFrom(selectMethodReturnType))); 
            } 
        }
 
        public override bool CanUpdate {
            get {
                return (_owner.ObjectDataSource.UpdateMethod.Length > 0);
            } 
        }
 
        public override IDataSourceViewSchema Schema { 
            get {
                // Extract the serialized data from DesignerState 
                DataTable[] schemaTables = _owner.LoadSchema();
                if ((schemaTables != null) && (schemaTables.Length > 0)) {
                    if (Name.Length == 0) {
                        return new DataSetViewSchema(schemaTables[0]); 
                    }
                    foreach (DataTable dataTable in schemaTables) { 
                        if (String.Equals(dataTable.TableName, Name, StringComparison.OrdinalIgnoreCase)) { 
                            return new DataSetViewSchema(dataTable);
                        } 
                    }
                }
                return null;
            } 
        }
 
        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) { 
            isSampleData = true;
 
            DataTable[] schemaTables = _owner.LoadSchema();
            if ((schemaTables != null) && (schemaTables.Length > 0)) {
                if (Name.Length == 0) {
                    // View name was not specified, just get the first (default) table and create dummy data 
                    return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateSampleDataTable(new DataView(schemaTables[0]), true), minimumRows);
                } 
 
                // Try to find the requested table by name (case-insensitive)
                foreach (DataTable dataTable in schemaTables) { 
                    if (String.Equals(dataTable.TableName, Name, StringComparison.OrdinalIgnoreCase)) {
                        // Found the table, create some dummy data
                        return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateSampleDataTable(new DataView(dataTable), true), minimumRows);
                    } 
                }
            } 
 
            // Couldn't find design-time schema, use base implementation
            return base.GetDesignTimeData(minimumRows, out isSampleData); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ObjectDesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Data;
    using System.Web.UI.WebControls;

    /// <devdoc> 
    /// ObjectDesignerDataSourceView is the designer view associated with a ObjectDataSourceDesigner.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ObjectDesignerDataSourceView : DesignerDataSourceView {
        private ObjectDataSourceDesigner _owner; 

        public ObjectDesignerDataSourceView(ObjectDataSourceDesigner owner, string viewName) : base(owner, viewName) {
            _owner = owner;
        } 

        public override bool CanDelete { 
            get { 
                return (_owner.ObjectDataSource.DeleteMethod.Length > 0);
            } 
        }

        public override bool CanInsert {
            get { 
                return (_owner.ObjectDataSource.InsertMethod.Length > 0);
            } 
        } 

        public override bool CanPage { 
            get {
                return _owner.ObjectDataSource.EnablePaging;
            }
        } 

        public override bool CanRetrieveTotalRowCount { 
            get { 
                return (_owner.ObjectDataSource.SelectCountMethod.Length > 0);
            } 
        }

        public override bool CanSort {
            get { 
                // We can sort if either the business object can do custom sorting,
                // or the return type of the select method is one of a few known types. 
                if (_owner.ObjectDataSource.SortParameterName.Length > 0) { 
                    return true;
                } 

                Type selectMethodReturnType = _owner.SelectMethodReturnType;
                return ((selectMethodReturnType != null) &&
                    (typeof(DataSet).IsAssignableFrom(selectMethodReturnType) || 
                    typeof(DataTable).IsAssignableFrom(selectMethodReturnType) ||
                    typeof(DataView).IsAssignableFrom(selectMethodReturnType))); 
            } 
        }
 
        public override bool CanUpdate {
            get {
                return (_owner.ObjectDataSource.UpdateMethod.Length > 0);
            } 
        }
 
        public override IDataSourceViewSchema Schema { 
            get {
                // Extract the serialized data from DesignerState 
                DataTable[] schemaTables = _owner.LoadSchema();
                if ((schemaTables != null) && (schemaTables.Length > 0)) {
                    if (Name.Length == 0) {
                        return new DataSetViewSchema(schemaTables[0]); 
                    }
                    foreach (DataTable dataTable in schemaTables) { 
                        if (String.Equals(dataTable.TableName, Name, StringComparison.OrdinalIgnoreCase)) { 
                            return new DataSetViewSchema(dataTable);
                        } 
                    }
                }
                return null;
            } 
        }
 
        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) { 
            isSampleData = true;
 
            DataTable[] schemaTables = _owner.LoadSchema();
            if ((schemaTables != null) && (schemaTables.Length > 0)) {
                if (Name.Length == 0) {
                    // View name was not specified, just get the first (default) table and create dummy data 
                    return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateSampleDataTable(new DataView(schemaTables[0]), true), minimumRows);
                } 
 
                // Try to find the requested table by name (case-insensitive)
                foreach (DataTable dataTable in schemaTables) { 
                    if (String.Equals(dataTable.TableName, Name, StringComparison.OrdinalIgnoreCase)) {
                        // Found the table, create some dummy data
                        return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateSampleDataTable(new DataView(dataTable), true), minimumRows);
                    } 
                }
            } 
 
            // Couldn't find design-time schema, use base implementation
            return base.GetDesignTimeData(minimumRows, out isSampleData); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
