//------------------------------------------------------------------------------ 
// <copyright file="ProxyWebPartManagerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls.WebParts;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ProxyWebPartManagerDesigner : ControlDesigner {
 
        protected override bool UsePreviewControl { 
            get {
                return true; 
            }
        }

        public override string GetDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml();
        } 
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(ProxyWebPartManager)); 
            base.Initialize(component);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ProxyWebPartManagerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls.WebParts;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ProxyWebPartManagerDesigner : ControlDesigner {
 
        protected override bool UsePreviewControl { 
            get {
                return true; 
            }
        }

        public override string GetDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml();
        } 
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(ProxyWebPartManager)); 
            base.Initialize(component);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
