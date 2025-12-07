//------------------------------------------------------------------------------ 
// <copyright file="HideDisabledControlAdapter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.Adapters { 
 
    using System;
    using System.Security.Permissions; 
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.Adapters;
 
    // Used for controls which use their default rendering, but are hidden when disabled.
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class HideDisabledControlAdapter : WebControlAdapter {
        // Returns without doing anything if the control is disabled, otherwise, uses the default rendering. 
        protected internal override void Render(HtmlTextWriter writer) {
            if (Control.Enabled == false) {
                return;
            } 
            Control.Render(writer);
        } 
 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="HideDisabledControlAdapter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.Adapters { 
 
    using System;
    using System.Security.Permissions; 
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.Adapters;
 
    // Used for controls which use their default rendering, but are hidden when disabled.
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class HideDisabledControlAdapter : WebControlAdapter {
        // Returns without doing anything if the control is disabled, otherwise, uses the default rendering. 
        protected internal override void Render(HtmlTextWriter writer) {
            if (Control.Enabled == false) {
                return;
            } 
            Control.Render(writer);
        } 
 
    }
} 
