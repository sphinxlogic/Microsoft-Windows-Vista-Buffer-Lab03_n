//------------------------------------------------------------------------------ 
// <copyright file="HierarchicalDataSourceIDConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
 
    public class HierarchicalDataSourceIDConverter : DataSourceIDConverter {

        protected override bool IsValidDataSource(IComponent component) {
            return (component is IHierarchicalDataSource); 
        }
    } 
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="HierarchicalDataSourceIDConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
 
    public class HierarchicalDataSourceIDConverter : DataSourceIDConverter {

        protected override bool IsValidDataSource(IComponent component) {
            return (component is IHierarchicalDataSource); 
        }
    } 
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
