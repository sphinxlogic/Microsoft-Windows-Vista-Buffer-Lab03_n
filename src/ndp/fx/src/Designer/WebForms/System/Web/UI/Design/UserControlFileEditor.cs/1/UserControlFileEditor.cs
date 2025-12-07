//------------------------------------------------------------------------------ 
// <copyright file="UserControlFileEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Design;
    using System.Security.Permissions; 

    [SecurityPermission(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
    public class UserControlFileEditor: UrlEditor {
 
        protected override string Caption {
            get { 
                return SR.GetString(SR.UserControlFileEditor_Caption); 
            }
        } 

        protected override string Filter {
            get {
                return SR.GetString(SR.UserControlFileEditor_Filter); 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="UserControlFileEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Design;
    using System.Security.Permissions; 

    [SecurityPermission(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
    public class UserControlFileEditor: UrlEditor {
 
        protected override string Caption {
            get { 
                return SR.GetString(SR.UserControlFileEditor_Caption); 
            }
        } 

        protected override string Filter {
            get {
                return SR.GetString(SR.UserControlFileEditor_Filter); 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
