//------------------------------------------------------------------------------ 
// <copyright file="WebPartDisplayModeCancelEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class WebPartDisplayModeCancelEventArgs : CancelEventArgs {
        private WebPartDisplayMode _newDisplayMode; 
 
        public WebPartDisplayModeCancelEventArgs(WebPartDisplayMode newDisplayMode) {
            _newDisplayMode = newDisplayMode; 
        }

        public WebPartDisplayMode NewDisplayMode {
            get { 
                return _newDisplayMode;
            } 
            set { 
                _newDisplayMode = value;
            } 
        }
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="WebPartDisplayModeCancelEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class WebPartDisplayModeCancelEventArgs : CancelEventArgs {
        private WebPartDisplayMode _newDisplayMode; 
 
        public WebPartDisplayModeCancelEventArgs(WebPartDisplayMode newDisplayMode) {
            _newDisplayMode = newDisplayMode; 
        }

        public WebPartDisplayMode NewDisplayMode {
            get { 
                return _newDisplayMode;
            } 
            set { 
                _newDisplayMode = value;
            } 
        }
    }
}
