//------------------------------------------------------------------------------ 
// <copyright file="HierarchicalDataBoundControlAdapter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.Adapters { 
 
    using System.Security.Permissions;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class HierarchicalDataBoundControlAdapter : WebControlAdapter {
 
        protected new HierarchicalDataBoundControl Control {
            get { 
                return (HierarchicalDataBoundControl)base.Control; 
            }
        } 

        protected internal virtual void PerformDataBinding() {
            Control.PerformDataBinding();
        } 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="HierarchicalDataBoundControlAdapter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.Adapters { 
 
    using System.Security.Permissions;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class HierarchicalDataBoundControlAdapter : WebControlAdapter {
 
        protected new HierarchicalDataBoundControl Control {
            get { 
                return (HierarchicalDataBoundControl)base.Control; 
            }
        } 

        protected internal virtual void PerformDataBinding() {
            Control.PerformDataBinding();
        } 
    }
} 
