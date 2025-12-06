//------------------------------------------------------------------------------ 
// <copyright file="TypeEnumerableViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection; 

    /// <devdoc> 
    /// Represents a View's schema based on a generic IEnumerable. The 
    /// strongly-typed row type is determined based on the generic argument
    /// of the enumerable. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class TypeGenericEnumerableViewSchema : BaseTypeViewSchema {
 
        public TypeGenericEnumerableViewSchema(string viewName, Type type)
            : base(viewName, type) { 
            Debug.Assert(TypeSchema.IsBoundGenericEnumerable(type), String.Format(CultureInfo.InvariantCulture, "The type '{0}' does not implement System.Collections.Generic.IEnumerable<T>, or the argument T is not bound to a type.", type.FullName)); 
        }
 
        protected override Type GetRowType(Type objectType) {
            // Get the IEnumerable<T> declaration
            Type enumerableType = null;
            if (objectType.IsInterface && objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) { 
                // If the type already is IEnumerable<T>, that's the interface we want to inspect
                enumerableType = objectType; 
            } 
            else {
                // Otherwise we get a list of all the interafaces the type implements 
                Type[] interfaces = objectType.GetInterfaces();
                foreach (Type i in interfaces) {
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                        enumerableType = i; 
                        break;
                    } 
                } 
            }
            Debug.Assert(enumerableType != null, "Should have found an implementation of IEnumerable<T>"); 

            // Now return the value of the T argument
            Type[] genericArguments = enumerableType.GetGenericArguments();
            Debug.Assert(genericArguments != null && genericArguments.Length == 1, "Expected IEnumerable<T> to have a generic argument"); 
            // If a type has IsGenericParameter=true that means it is not bound.
            if (genericArguments[0].IsGenericParameter) { 
                Debug.Fail("Expected the type argument to IEnumerable<T> to be bound"); 
                return null;
            } 
            else {
                return genericArguments[0];
            }
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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection; 

    /// <devdoc> 
    /// Represents a View's schema based on a generic IEnumerable. The 
    /// strongly-typed row type is determined based on the generic argument
    /// of the enumerable. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class TypeGenericEnumerableViewSchema : BaseTypeViewSchema {
 
        public TypeGenericEnumerableViewSchema(string viewName, Type type)
            : base(viewName, type) { 
            Debug.Assert(TypeSchema.IsBoundGenericEnumerable(type), String.Format(CultureInfo.InvariantCulture, "The type '{0}' does not implement System.Collections.Generic.IEnumerable<T>, or the argument T is not bound to a type.", type.FullName)); 
        }
 
        protected override Type GetRowType(Type objectType) {
            // Get the IEnumerable<T> declaration
            Type enumerableType = null;
            if (objectType.IsInterface && objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) { 
                // If the type already is IEnumerable<T>, that's the interface we want to inspect
                enumerableType = objectType; 
            } 
            else {
                // Otherwise we get a list of all the interafaces the type implements 
                Type[] interfaces = objectType.GetInterfaces();
                foreach (Type i in interfaces) {
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                        enumerableType = i; 
                        break;
                    } 
                } 
            }
            Debug.Assert(enumerableType != null, "Should have found an implementation of IEnumerable<T>"); 

            // Now return the value of the T argument
            Type[] genericArguments = enumerableType.GetGenericArguments();
            Debug.Assert(genericArguments != null && genericArguments.Length == 1, "Expected IEnumerable<T> to have a generic argument"); 
            // If a type has IsGenericParameter=true that means it is not bound.
            if (genericArguments[0].IsGenericParameter) { 
                Debug.Fail("Expected the type argument to IEnumerable<T> to be bound"); 
                return null;
            } 
            else {
                return genericArguments[0];
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
