//------------------------------------------------------------------------------ 
// <copyright file="WebPartDisplayModeEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class WebPartDisplayModeEventArgs : EventArgs { 
        private WebPartDisplayMode _oldDisplayMode;
 
        public WebPartDisplayModeEventArgs(WebPartDisplayMode oldDisplayMode) { 
            _oldDisplayMode = oldDisplayMode;
        } 

        public WebPartDisplayMode OldDisplayMode {
            get {
                return _oldDisplayMode; 
            }
            set { 
                _oldDisplayMode = value; 
            }
        } 
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="WebPartDisplayModeEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class WebPartDisplayModeEventArgs : EventArgs { 
        private WebPartDisplayMode _oldDisplayMode;
 
        public WebPartDisplayModeEventArgs(WebPartDisplayMode oldDisplayMode) { 
            _oldDisplayMode = oldDisplayMode;
        } 

        public WebPartDisplayMode OldDisplayMode {
            get {
                return _oldDisplayMode; 
            }
            set { 
                _oldDisplayMode = value; 
            }
        } 
    }
}
