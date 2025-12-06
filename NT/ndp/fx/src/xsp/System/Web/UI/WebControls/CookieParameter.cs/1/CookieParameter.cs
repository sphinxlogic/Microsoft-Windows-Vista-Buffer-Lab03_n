//------------------------------------------------------------------------------ 
// <copyright file="CookieParameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;


 
    /// <devdoc>
    /// Represents a Parameter that gets its value from the application's request parameters. 
    /// </devdoc> 
    [
    DefaultProperty("CookieName"), 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class CookieParameter : Parameter { 

 
        /// <devdoc> 
        /// Creates an instance of the CookieParameter class.
        /// </devdoc> 
        public CookieParameter() {
        }

 
        /// <devdoc>
        /// Creates an instance of the CookieParameter class with the specified parameter name and request field. 
        /// </devdoc> 
        public CookieParameter(string name, string cookieName) : base(name) {
            CookieName = cookieName; 
        }


        /// <devdoc> 
        /// Creates an instance of the CookieParameter class with the specified parameter name, type, and request field.
        /// </devdoc> 
        public CookieParameter(string name, TypeCode type, string cookieName) : base(name, type) { 
            CookieName = cookieName;
        } 


        /// <devdoc>
        /// Used to clone a parameter. 
        /// </devdoc>
        protected CookieParameter(CookieParameter original) : base(original) { 
            CookieName = original.CookieName; 
        }
 


        /// <devdoc>
        /// The name of the request parameter to get the value from. 
        /// </devdoc>
        [ 
        DefaultValue(""), 
        WebCategory("Parameter"),
        WebSysDescription(SR.CookieParameter_CookieName), 
        ]
        public string CookieName {
            get {
                object o = ViewState["CookieName"]; 
                if (o == null)
                    return String.Empty; 
                return (string)o; 
            }
            set { 
                if (CookieName != value) {
                    ViewState["CookieName"] = value;
                    OnParameterChanged();
                } 
            }
        } 
 

        /// <devdoc> 
        /// Creates a new CookieParameter that is a copy of this CookieParameter.
        /// </devdoc>
        protected override Parameter Clone() {
            return new CookieParameter(this); 
        }
 
 
        /// <devdoc>
        /// Returns the updated value of the parameter. 
        /// </devdoc>
        protected override object Evaluate(HttpContext context, Control control) {
            if (context == null || context.Request == null) {
                return null; 
            }
            HttpCookie cookie = context.Request.Cookies[CookieName]; 
            if (cookie == null) { 
                return null;
            } 
            return cookie.Value;
        }
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="CookieParameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;


 
    /// <devdoc>
    /// Represents a Parameter that gets its value from the application's request parameters. 
    /// </devdoc> 
    [
    DefaultProperty("CookieName"), 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class CookieParameter : Parameter { 

 
        /// <devdoc> 
        /// Creates an instance of the CookieParameter class.
        /// </devdoc> 
        public CookieParameter() {
        }

 
        /// <devdoc>
        /// Creates an instance of the CookieParameter class with the specified parameter name and request field. 
        /// </devdoc> 
        public CookieParameter(string name, string cookieName) : base(name) {
            CookieName = cookieName; 
        }


        /// <devdoc> 
        /// Creates an instance of the CookieParameter class with the specified parameter name, type, and request field.
        /// </devdoc> 
        public CookieParameter(string name, TypeCode type, string cookieName) : base(name, type) { 
            CookieName = cookieName;
        } 


        /// <devdoc>
        /// Used to clone a parameter. 
        /// </devdoc>
        protected CookieParameter(CookieParameter original) : base(original) { 
            CookieName = original.CookieName; 
        }
 


        /// <devdoc>
        /// The name of the request parameter to get the value from. 
        /// </devdoc>
        [ 
        DefaultValue(""), 
        WebCategory("Parameter"),
        WebSysDescription(SR.CookieParameter_CookieName), 
        ]
        public string CookieName {
            get {
                object o = ViewState["CookieName"]; 
                if (o == null)
                    return String.Empty; 
                return (string)o; 
            }
            set { 
                if (CookieName != value) {
                    ViewState["CookieName"] = value;
                    OnParameterChanged();
                } 
            }
        } 
 

        /// <devdoc> 
        /// Creates a new CookieParameter that is a copy of this CookieParameter.
        /// </devdoc>
        protected override Parameter Clone() {
            return new CookieParameter(this); 
        }
 
 
        /// <devdoc>
        /// Returns the updated value of the parameter. 
        /// </devdoc>
        protected override object Evaluate(HttpContext context, Control control) {
            if (context == null || context.Request == null) {
                return null; 
            }
            HttpCookie cookie = context.Request.Cookies[CookieName]; 
            if (cookie == null) { 
                return null;
            } 
            return cookie.Value;
        }
    }
} 

