//------------------------------------------------------------------------------ 
// <copyright file="LoginNameDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
    using System.Design; 
    using System.Web.UI.WebControls;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class LoginNameDesigner : ControlDesigner { 

        protected override bool UsePreviewControl { 
            get { 
                return true;
            } 
        }

        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        }
 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LoginNameDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
    using System.Design; 
    using System.Web.UI.WebControls;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class LoginNameDesigner : ControlDesigner { 

        protected override bool UsePreviewControl { 
            get { 
                return true;
            } 
        }

        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        }
 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
