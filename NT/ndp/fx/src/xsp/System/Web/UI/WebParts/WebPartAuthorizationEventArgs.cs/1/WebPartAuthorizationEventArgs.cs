//------------------------------------------------------------------------------ 
// <copyright file="WebPartAuthorizationEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class WebPartAuthorizationEventArgs : EventArgs { 
        private Type _type;
        private string _path; 
        private string _authorizationFilter; 
        private bool _isShared;
        private bool _isAuthorized; 

        public WebPartAuthorizationEventArgs(Type type, string path, string authorizationFilter, bool isShared) {
            _type = type;
            _path = path; 
            _authorizationFilter = authorizationFilter;
            _isShared = isShared; 
            _isAuthorized = true; 
        }
 
        public string AuthorizationFilter {
            get {
                return _authorizationFilter;
            } 
        }
 
        public bool IsAuthorized { 
            get {
                return _isAuthorized; 
            }
            set {
                _isAuthorized = value;
            } 
        }
 
        public bool IsShared { 
            get {
                return _isShared; 
            }
        }

        public string Path { 
            get {
                return _path; 
            } 
        }
 
        public Type Type {
            get {
                return _type;
            } 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="WebPartAuthorizationEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class WebPartAuthorizationEventArgs : EventArgs { 
        private Type _type;
        private string _path; 
        private string _authorizationFilter; 
        private bool _isShared;
        private bool _isAuthorized; 

        public WebPartAuthorizationEventArgs(Type type, string path, string authorizationFilter, bool isShared) {
            _type = type;
            _path = path; 
            _authorizationFilter = authorizationFilter;
            _isShared = isShared; 
            _isAuthorized = true; 
        }
 
        public string AuthorizationFilter {
            get {
                return _authorizationFilter;
            } 
        }
 
        public bool IsAuthorized { 
            get {
                return _isAuthorized; 
            }
            set {
                _isAuthorized = value;
            } 
        }
 
        public bool IsShared { 
            get {
                return _isShared; 
            }
        }

        public string Path { 
            get {
                return _path; 
            } 
        }
 
        public Type Type {
            get {
                return _type;
            } 
        }
    } 
} 
