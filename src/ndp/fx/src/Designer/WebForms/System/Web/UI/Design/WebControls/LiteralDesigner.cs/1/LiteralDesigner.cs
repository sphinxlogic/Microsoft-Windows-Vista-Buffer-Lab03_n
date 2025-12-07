//------------------------------------------------------------------------------ 
// <copyright file="LiteralDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Web.UI.WebControls;

    /// <devdoc>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [SupportsPreviewControl(true)] 
    public class LiteralDesigner : ControlDesigner { 
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs ce) {
            base.OnComponentChanged(sender, new ComponentChangedEventArgs(ce.Component, null, null, null)); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LiteralDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Web.UI.WebControls;

    /// <devdoc>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [SupportsPreviewControl(true)] 
    public class LiteralDesigner : ControlDesigner { 
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs ce) {
            base.OnComponentChanged(sender, new ComponentChangedEventArgs(ce.Component, null, null, null)); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
