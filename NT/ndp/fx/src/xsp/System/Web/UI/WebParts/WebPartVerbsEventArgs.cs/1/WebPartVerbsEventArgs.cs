//------------------------------------------------------------------------------ 
// <copyright file="WebPartVerbsEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class WebPartVerbsEventArgs : EventArgs { 
        private WebPartVerbCollection _verbs;
 
        public WebPartVerbsEventArgs() : this(null) { 
        }
 
        public WebPartVerbsEventArgs(WebPartVerbCollection verbs) {
            _verbs = verbs;
        }
 
        public WebPartVerbCollection Verbs {
            get { 
                return (_verbs != null) ? _verbs : WebPartVerbCollection.Empty; 
            }
            set { 
                _verbs = value;
            }
        }
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="WebPartVerbsEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class WebPartVerbsEventArgs : EventArgs { 
        private WebPartVerbCollection _verbs;
 
        public WebPartVerbsEventArgs() : this(null) { 
        }
 
        public WebPartVerbsEventArgs(WebPartVerbCollection verbs) {
            _verbs = verbs;
        }
 
        public WebPartVerbCollection Verbs {
            get { 
                return (_verbs != null) ? _verbs : WebPartVerbCollection.Empty; 
            }
            set { 
                _verbs = value;
            }
        }
    } 
}
