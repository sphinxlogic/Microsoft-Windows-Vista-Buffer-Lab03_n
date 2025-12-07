//------------------------------------------------------------------------------ 
// <copyright file="LabelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Text; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
 
    /// <include file='doc\LabelDesigner.uex' path='docs/doc[@for="LabelDesigner"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       The designer for the <see cref='System.Web.UI.WebControls.Label'/>
    ///       web control.
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)] 
    public class LabelDesigner : TextControlDesigner {
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs ce) { 
            base.OnComponentChanged(sender, new ComponentChangedEventArgs(ce.Component, null, null, null));
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LabelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Text; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
 
    /// <include file='doc\LabelDesigner.uex' path='docs/doc[@for="LabelDesigner"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       The designer for the <see cref='System.Web.UI.WebControls.Label'/>
    ///       web control.
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)] 
    public class LabelDesigner : TextControlDesigner {
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs ce) { 
            base.OnComponentChanged(sender, new ComponentChangedEventArgs(ce.Component, null, null, null));
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
