//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControlAdapter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.Adapters { 
 
    using System.Collections;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DataBoundControlAdapter : WebControlAdapter { 

        protected new DataBoundControl Control { 
            get { 
                return (DataBoundControl)base.Control;
            } 
        }

        protected internal virtual void PerformDataBinding(IEnumerable data) {
            Control.PerformDataBinding(data); 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControlAdapter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.Adapters { 
 
    using System.Collections;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DataBoundControlAdapter : WebControlAdapter { 

        protected new DataBoundControl Control { 
            get { 
                return (DataBoundControl)base.Control;
            } 
        }

        protected internal virtual void PerformDataBinding(IEnumerable data) {
            Control.PerformDataBinding(data); 
        }
    } 
} 
