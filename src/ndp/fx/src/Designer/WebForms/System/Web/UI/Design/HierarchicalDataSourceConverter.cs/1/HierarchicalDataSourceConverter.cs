//------------------------------------------------------------------------------ 
// <copyright file="HierarchicalDataSourceConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.ComponentModel;
    using System.Web.UI; 

    /// <devdoc>
    ///    <para>
    ///       Provides design-time support for a component's data source property. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class HierarchicalDataSourceConverter : DataSourceConverter {
 
        protected override bool IsValidDataSource(IComponent component) {
            Control control = component as Control;
            if (control == null) {
                return false; 
            }
            if (String.IsNullOrEmpty(control.ID)) { 
                return false; 
            }
            return (component is IHierarchicalEnumerable); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="HierarchicalDataSourceConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.ComponentModel;
    using System.Web.UI; 

    /// <devdoc>
    ///    <para>
    ///       Provides design-time support for a component's data source property. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class HierarchicalDataSourceConverter : DataSourceConverter {
 
        protected override bool IsValidDataSource(IComponent component) {
            Control control = component as Control;
            if (control == null) {
                return false; 
            }
            if (String.IsNullOrEmpty(control.ID)) { 
                return false; 
            }
            return (component is IHierarchicalEnumerable); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
