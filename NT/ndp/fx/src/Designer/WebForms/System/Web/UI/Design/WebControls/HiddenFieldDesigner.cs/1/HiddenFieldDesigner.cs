//------------------------------------------------------------------------------ 
// <copyright file="HiddenFieldDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
    using System.Web.UI.WebControls; 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class HiddenFieldDesigner : ControlDesigner {
 
        public override string GetDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(); 
        } 

        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(HiddenField));
            base.Initialize(component);
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="HiddenFieldDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
    using System.Web.UI.WebControls; 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class HiddenFieldDesigner : ControlDesigner {
 
        public override string GetDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(); 
        } 

        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(HiddenField));
            base.Initialize(component);
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
