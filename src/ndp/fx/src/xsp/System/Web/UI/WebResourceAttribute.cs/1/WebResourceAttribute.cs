//------------------------------------------------------------------------------ 
// <copyright file="WebResourceAttribute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;
    using System.Web.Util;

 
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class WebResourceAttribute : Attribute { 

        private string _contentType; 
        private bool _performSubstitution;
        private string _webResource;

 
        public WebResourceAttribute(string webResource, string contentType) {
            if (String.IsNullOrEmpty(webResource)) { 
                throw ExceptionUtil.ParameterNullOrEmpty("webResource"); 
            }
 
            if (String.IsNullOrEmpty(contentType)) {
                throw ExceptionUtil.ParameterNullOrEmpty("contentType");
            }
 
            _contentType = contentType;
            _webResource = webResource; 
            _performSubstitution = false; 
        }
 

        public string ContentType {
            get {
                return _contentType; 
            }
        } 
 

        public bool PerformSubstitution { 
            get {
                return _performSubstitution;
            }
            set { 
                _performSubstitution = value;
            } 
        } 

 
        public string WebResource {
            get {
                return _webResource;
            } 
        }
    } 
} 

 
//------------------------------------------------------------------------------ 
// <copyright file="WebResourceAttribute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;
    using System.Web.Util;

 
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class WebResourceAttribute : Attribute { 

        private string _contentType; 
        private bool _performSubstitution;
        private string _webResource;

 
        public WebResourceAttribute(string webResource, string contentType) {
            if (String.IsNullOrEmpty(webResource)) { 
                throw ExceptionUtil.ParameterNullOrEmpty("webResource"); 
            }
 
            if (String.IsNullOrEmpty(contentType)) {
                throw ExceptionUtil.ParameterNullOrEmpty("contentType");
            }
 
            _contentType = contentType;
            _webResource = webResource; 
            _performSubstitution = false; 
        }
 

        public string ContentType {
            get {
                return _contentType; 
            }
        } 
 

        public bool PerformSubstitution { 
            get {
                return _performSubstitution;
            }
            set { 
                _performSubstitution = value;
            } 
        } 

 
        public string WebResource {
            get {
                return _webResource;
            } 
        }
    } 
} 

 
