//------------------------------------------------------------------------------ 
// <copyright file="CreateUserErrorEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
    using System.Web.Security; 
    using System.Security.Permissions;
 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class CreateUserErrorEventArgs : EventArgs { 
        private MembershipCreateStatus _error;
 
 
        public CreateUserErrorEventArgs(MembershipCreateStatus s ) {
            _error = s; 
        }


        /// <devdoc> 
        /// Gets or sets the error which caused the failure
        /// </devdoc> 
        public MembershipCreateStatus CreateUserError { 
            get {
                return _error; 
            }
            set {
                _error = value;
            } 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="CreateUserErrorEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
    using System.Web.Security; 
    using System.Security.Permissions;
 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class CreateUserErrorEventArgs : EventArgs { 
        private MembershipCreateStatus _error;
 
 
        public CreateUserErrorEventArgs(MembershipCreateStatus s ) {
            _error = s; 
        }


        /// <devdoc> 
        /// Gets or sets the error which caused the failure
        /// </devdoc> 
        public MembershipCreateStatus CreateUserError { 
            get {
                return _error; 
            }
            set {
                _error = value;
            } 
        }
    } 
} 
