//------------------------------------------------------------------------------ 
// <copyright file="TypeViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Reflection;

    /// <devdoc> 
    /// Represents a View's schema based on an arbitrary type. The
    /// strongly-typed row type is the type itself. 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class TypeViewSchema : BaseTypeViewSchema { 

        public TypeViewSchema(string viewName, Type type) : base(viewName, type) {
        }
 
        protected override Type GetRowType(Type objectType) {
            return objectType; 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TypeViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Reflection;

    /// <devdoc> 
    /// Represents a View's schema based on an arbitrary type. The
    /// strongly-typed row type is the type itself. 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class TypeViewSchema : BaseTypeViewSchema { 

        public TypeViewSchema(string viewName, Type type) : base(viewName, type) {
        }
 
        protected override Type GetRowType(Type objectType) {
            return objectType; 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
