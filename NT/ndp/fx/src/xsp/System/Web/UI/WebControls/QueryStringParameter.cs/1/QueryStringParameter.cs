//------------------------------------------------------------------------------ 
// <copyright file="QueryStringParameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;


 
    /// <devdoc>
    /// Represents a Parameter that gets its value from the application's QueryString parameters. 
    /// </devdoc> 
    [
    DefaultProperty("QueryStringField"), 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class QueryStringParameter : Parameter { 

 
        /// <devdoc> 
        /// Creates an instance of the QueryStringParameter class.
        /// </devdoc> 
        public QueryStringParameter() {
        }

 
        /// <devdoc>
        /// Creates an instance of the QueryStringParameter class with the specified parameter name and QueryString field. 
        /// </devdoc> 
        public QueryStringParameter(string name, string queryStringField) : base(name) {
            QueryStringField = queryStringField; 
        }


        /// <devdoc> 
        /// Creates an instance of the QueryStringParameter class with the specified parameter name, type, and QueryString field.
        /// </devdoc> 
        public QueryStringParameter(string name, TypeCode type, string queryStringField) : base(name, type) { 
            QueryStringField = queryStringField;
        } 


        /// <devdoc>
        /// Used to clone a parameter. 
        /// </devdoc>
        protected QueryStringParameter(QueryStringParameter original) : base(original) { 
            QueryStringField = original.QueryStringField; 
        }
 


        /// <devdoc>
        /// The name of the QueryString parameter to get the value from. 
        /// </devdoc>
        [ 
        DefaultValue(""), 
        WebCategory("Parameter"),
        WebSysDescription(SR.QueryStringParameter_QueryStringField), 
        ]
        public string QueryStringField {
            get {
                object o = ViewState["QueryStringField"]; 
                if (o == null)
                    return String.Empty; 
                return (string)o; 
            }
            set { 
                if (QueryStringField != value) {
                    ViewState["QueryStringField"] = value;
                    OnParameterChanged();
                } 
            }
        } 
 

        /// <devdoc> 
        /// Creates a new QueryStringParameter that is a copy of this QueryStringParameter.
        /// </devdoc>
        protected override Parameter Clone() {
            return new QueryStringParameter(this); 
        }
 
 
        /// <devdoc>
        /// Returns the updated value of the parameter. 
        /// </devdoc>
        protected override object Evaluate(HttpContext context, Control control) {
            if (context == null || context.Request == null) {
                return null; 
            }
            return context.Request.QueryString[QueryStringField]; 
        } 
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="QueryStringParameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;


 
    /// <devdoc>
    /// Represents a Parameter that gets its value from the application's QueryString parameters. 
    /// </devdoc> 
    [
    DefaultProperty("QueryStringField"), 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class QueryStringParameter : Parameter { 

 
        /// <devdoc> 
        /// Creates an instance of the QueryStringParameter class.
        /// </devdoc> 
        public QueryStringParameter() {
        }

 
        /// <devdoc>
        /// Creates an instance of the QueryStringParameter class with the specified parameter name and QueryString field. 
        /// </devdoc> 
        public QueryStringParameter(string name, string queryStringField) : base(name) {
            QueryStringField = queryStringField; 
        }


        /// <devdoc> 
        /// Creates an instance of the QueryStringParameter class with the specified parameter name, type, and QueryString field.
        /// </devdoc> 
        public QueryStringParameter(string name, TypeCode type, string queryStringField) : base(name, type) { 
            QueryStringField = queryStringField;
        } 


        /// <devdoc>
        /// Used to clone a parameter. 
        /// </devdoc>
        protected QueryStringParameter(QueryStringParameter original) : base(original) { 
            QueryStringField = original.QueryStringField; 
        }
 


        /// <devdoc>
        /// The name of the QueryString parameter to get the value from. 
        /// </devdoc>
        [ 
        DefaultValue(""), 
        WebCategory("Parameter"),
        WebSysDescription(SR.QueryStringParameter_QueryStringField), 
        ]
        public string QueryStringField {
            get {
                object o = ViewState["QueryStringField"]; 
                if (o == null)
                    return String.Empty; 
                return (string)o; 
            }
            set { 
                if (QueryStringField != value) {
                    ViewState["QueryStringField"] = value;
                    OnParameterChanged();
                } 
            }
        } 
 

        /// <devdoc> 
        /// Creates a new QueryStringParameter that is a copy of this QueryStringParameter.
        /// </devdoc>
        protected override Parameter Clone() {
            return new QueryStringParameter(this); 
        }
 
 
        /// <devdoc>
        /// Returns the updated value of the parameter. 
        /// </devdoc>
        protected override object Evaluate(HttpContext context, Control control) {
            if (context == null || context.Request == null) {
                return null; 
            }
            return context.Request.QueryString[QueryStringField]; 
        } 
    }
} 

