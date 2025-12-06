//------------------------------------------------------------------------------ 
// <copyright file="TypeEnumerableViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
 
    /// <devdoc>
    /// Represents a View's schema based on a strongly typed enumerable. The 
    /// strongly-typed row type is determined based on the indexer property 
    /// of the enumerable.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class TypeEnumerableViewSchema : BaseTypeViewSchema {

        public TypeEnumerableViewSchema(string viewName, Type type) : base(viewName, type) { 
            Debug.Assert(typeof(IEnumerable).IsAssignableFrom(type), String.Format(CultureInfo.InvariantCulture, "The type '{0}' does not implement System.Collections.IEnumerable.", type.FullName));
        } 
 
        protected override Type GetRowType(Type objectType) {
            // For arrays we just get the element type 
            if (objectType.IsArray) {
                Debug.Assert(objectType.HasElementType, "Expected array type to have an ElementType");
                Debug.Assert(objectType.GetElementType() != null, "Did not expect array type to have null ElementType");
                return objectType.GetElementType(); 
            }
 
            // Search for indexer property 
            PropertyInfo[] properties = objectType.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo pi in properties) { 
                ParameterInfo[] indexParams = pi.GetIndexParameters();
                if (indexParams.Length > 0) {
                    // We assume that this was the only indexer, so we can immediately stop looking for more
                    // 
                    return pi.PropertyType;
                } 
            } 
            return null;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TypeEnumerableViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
 
    /// <devdoc>
    /// Represents a View's schema based on a strongly typed enumerable. The 
    /// strongly-typed row type is determined based on the indexer property 
    /// of the enumerable.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class TypeEnumerableViewSchema : BaseTypeViewSchema {

        public TypeEnumerableViewSchema(string viewName, Type type) : base(viewName, type) { 
            Debug.Assert(typeof(IEnumerable).IsAssignableFrom(type), String.Format(CultureInfo.InvariantCulture, "The type '{0}' does not implement System.Collections.IEnumerable.", type.FullName));
        } 
 
        protected override Type GetRowType(Type objectType) {
            // For arrays we just get the element type 
            if (objectType.IsArray) {
                Debug.Assert(objectType.HasElementType, "Expected array type to have an ElementType");
                Debug.Assert(objectType.GetElementType() != null, "Did not expect array type to have null ElementType");
                return objectType.GetElementType(); 
            }
 
            // Search for indexer property 
            PropertyInfo[] properties = objectType.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo pi in properties) { 
                ParameterInfo[] indexParams = pi.GetIndexParameters();
                if (indexParams.Length > 0) {
                    // We assume that this was the only indexer, so we can immediately stop looking for more
                    // 
                    return pi.PropertyType;
                } 
            } 
            return null;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
