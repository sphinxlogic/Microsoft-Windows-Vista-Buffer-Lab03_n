//------------------------------------------------------------------------------ 
// <copyright file="AuthenticateEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System.Security.Permissions;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class AuthenticateEventArgs : EventArgs {
        private bool _authenticated; 

 
        public AuthenticateEventArgs() : this(false) { 
        }
 

        public AuthenticateEventArgs(bool authenticated) {
            _authenticated = authenticated;
        } 

 
        /// <devdoc> 
        /// Gets or sets the success of the authentication attempt.  Would be set by
        /// custom authentication logic in the Login.Authenticate event handler. 
        /// </devdoc>
        public bool Authenticated {
            get {
                return _authenticated; 
            }
            set { 
                _authenticated = value; 
            }
        } 
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="AuthenticateEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System.Security.Permissions;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class AuthenticateEventArgs : EventArgs {
        private bool _authenticated; 

 
        public AuthenticateEventArgs() : this(false) { 
        }
 

        public AuthenticateEventArgs(bool authenticated) {
            _authenticated = authenticated;
        } 

 
        /// <devdoc> 
        /// Gets or sets the success of the authentication attempt.  Would be set by
        /// custom authentication logic in the Login.Authenticate event handler. 
        /// </devdoc>
        public bool Authenticated {
            get {
                return _authenticated; 
            }
            set { 
                _authenticated = value; 
            }
        } 
    }
}
