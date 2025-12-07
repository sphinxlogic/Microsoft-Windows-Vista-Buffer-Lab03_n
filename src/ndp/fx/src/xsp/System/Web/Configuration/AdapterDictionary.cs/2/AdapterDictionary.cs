//------------------------------------------------------------------------------ 
// <copyright file="AdapterDictionary.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Security.Permissions;

    [Serializable] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class AdapterDictionary : OrderedDictionary { 
        public AdapterDictionary() {
        } 

        public string this[string key] {
            get {
                return (string)base[key]; 
            }
            set { 
                base[key] = value; 
            }
        } 
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="AdapterDictionary.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Security.Permissions;

    [Serializable] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class AdapterDictionary : OrderedDictionary { 
        public AdapterDictionary() {
        } 

        public string this[string key] {
            get {
                return (string)base[key]; 
            }
            set { 
                base[key] = value; 
            }
        } 
    }
}
