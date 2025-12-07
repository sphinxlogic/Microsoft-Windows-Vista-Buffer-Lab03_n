//------------------------------------------------------------------------------ 
// <copyright file="WebPartEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class WebPartEventArgs : EventArgs { 
        private WebPart _webPart;
 
        public WebPartEventArgs(WebPart webPart) { 
            _webPart = webPart;
        } 

        public WebPart WebPart {
            get {
                return _webPart; 
            }
        } 
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="WebPartEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class WebPartEventArgs : EventArgs { 
        private WebPart _webPart;
 
        public WebPartEventArgs(WebPart webPart) { 
            _webPart = webPart;
        } 

        public WebPart WebPart {
            get {
                return _webPart; 
            }
        } 
    } 
}
