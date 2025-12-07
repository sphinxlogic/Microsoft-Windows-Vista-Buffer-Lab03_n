//------------------------------------------------------------------------------ 
// <copyright file="TypeSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Reflection; 

    /// <devdoc> 
    /// Represents a schema based on an arbitrary object. 
    ///
    /// DataSets and DataTables are special cased and processed for their strongly-typed properties. 
    /// Types that implement the generic IEnumerable&lt;T&gt; will use the bound type T as their row type.
    /// Arbitrary enumerables are processed using their indexer's type.
    /// All other objects are directly reflected on and their public properties are exposed as fields.
    /// 
    /// DataSet and DataTable schema is retrieved by creating instances of the
    /// objects, whereas all other schema is retrived using Reflection. 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class TypeSchema : IDataSourceSchema { 
        private Type _type;
        private IDataSourceViewSchema[] _schema;

        public TypeSchema(Type type) { 
            if (type == null) {
                throw new ArgumentNullException("type"); 
            } 
            _type = type;
 
            if (typeof(DataTable).IsAssignableFrom(_type)) {
                _schema = GetDataTableSchema(_type);
            }
            else { 
                if (typeof(DataSet).IsAssignableFrom(_type)) {
                    _schema = GetDataSetSchema(_type); 
                } 
                else {
                    if (IsBoundGenericEnumerable(_type)) { 
                        _schema = GetGenericEnumerableSchema(_type);
                    }
                    else {
                        if (typeof(IEnumerable).IsAssignableFrom(_type)) { 
                            _schema = GetEnumerableSchema(_type);
                        } 
                        else { 
                            _schema = GetTypeSchema(_type);
                        } 
                    }
                }
            }
        } 

        public IDataSourceViewSchema[] GetViews() { 
            return _schema; 
        }
 
        /// <devdoc>
        /// Gets schema for a strongly typed DataSet.
        /// </devdoc>
        private static IDataSourceViewSchema[] GetDataSetSchema(Type t) { 
            try {
                DataSet dataSet = Activator.CreateInstance(t) as DataSet; 
 
                System.Collections.Generic.List<IDataSourceViewSchema> views = new System.Collections.Generic.List<IDataSourceViewSchema>();
                foreach (DataTable table in dataSet.Tables) { 
                    views.Add(new DataSetViewSchema(table));
                }
                return views.ToArray();
            } 
            catch {
                return null; 
            } 
        }
 
        /// <devdoc>
        /// Gets schema for a strongly typed DataTable.
        /// </devdoc>
        private static IDataSourceViewSchema[] GetDataTableSchema(Type t) { 
            try {
                DataTable table = Activator.CreateInstance(t) as DataTable; 
 
                DataSetViewSchema tableSchema = new DataSetViewSchema(table);
                return new IDataSourceViewSchema[1] { tableSchema }; 
            }
            catch {
                return null;
            } 
        }
 
        /// <devdoc> 
        /// Gets schema for a strongly typed enumerable.
        /// </devdoc> 
        private static IDataSourceViewSchema[] GetEnumerableSchema(Type t) {
            TypeEnumerableViewSchema enumerableSchema = new TypeEnumerableViewSchema(String.Empty, t);
            return new IDataSourceViewSchema[1] { enumerableSchema };
        } 

        /// <devdoc> 
        /// Gets schema for a generic IEnumerable. 
        /// </devdoc>
        private static IDataSourceViewSchema[] GetGenericEnumerableSchema(Type t) { 
            TypeGenericEnumerableViewSchema enumerableSchema = new TypeGenericEnumerableViewSchema(String.Empty, t);
            return new IDataSourceViewSchema[1] { enumerableSchema };
        }
 
        /// <devdoc>
        /// Gets schema for an arbitrary type. 
        /// </devdoc> 
        private static IDataSourceViewSchema[] GetTypeSchema(Type t) {
            TypeViewSchema typeSchema = new TypeViewSchema(String.Empty, t); 
            return new IDataSourceViewSchema[1] { typeSchema };
        }

        /// <devdoc> 
        /// Returns true if the type implements IEnumerable&lt;T&gt; with a bound
        /// value for T. In other words, this type: 
        ///     public class Foo&lt;T&gt; : IEnumerable&lt;T&gt; { ... } 
        /// Does not bind the value of T. However this type:
        ///     public class Foo2 : IEnumerable&lt;string&gt; { ... } 
        /// Does have T bound to the type System.String.
        ///
        /// Although it would seem that this check:
        ///     typeof(IEnumerable&lt;&gt;).IsAssignableFrom(t) 
        /// Might be sufficient, it actually doesn't work, possibly due
        /// to covariance/contravariance limitations, etc. 
        /// </devdoc> 
        internal static bool IsBoundGenericEnumerable(Type t) {
            Type[] interfaces = null; 
            if (t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                // If the type already is IEnumerable<T>, that's the interface we want to inspect
                interfaces = new Type[] { t };
            } 
            else {
                // Otherwise we get a list of all the interafaces the type implements 
                interfaces = t.GetInterfaces(); 
            }
            foreach (Type i in interfaces) { 
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                    Type[] genericArguments = i.GetGenericArguments();
                    Debug.Assert(genericArguments != null && genericArguments.Length == 1, "Expected IEnumerable<T> to have a generic argument");
                    // If a type has IsGenericParameter=true that means it is not bound. 
                    return !genericArguments[0].IsGenericParameter;
                } 
            } 
            return false;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TypeSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Reflection; 

    /// <devdoc> 
    /// Represents a schema based on an arbitrary object. 
    ///
    /// DataSets and DataTables are special cased and processed for their strongly-typed properties. 
    /// Types that implement the generic IEnumerable&lt;T&gt; will use the bound type T as their row type.
    /// Arbitrary enumerables are processed using their indexer's type.
    /// All other objects are directly reflected on and their public properties are exposed as fields.
    /// 
    /// DataSet and DataTable schema is retrieved by creating instances of the
    /// objects, whereas all other schema is retrived using Reflection. 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class TypeSchema : IDataSourceSchema { 
        private Type _type;
        private IDataSourceViewSchema[] _schema;

        public TypeSchema(Type type) { 
            if (type == null) {
                throw new ArgumentNullException("type"); 
            } 
            _type = type;
 
            if (typeof(DataTable).IsAssignableFrom(_type)) {
                _schema = GetDataTableSchema(_type);
            }
            else { 
                if (typeof(DataSet).IsAssignableFrom(_type)) {
                    _schema = GetDataSetSchema(_type); 
                } 
                else {
                    if (IsBoundGenericEnumerable(_type)) { 
                        _schema = GetGenericEnumerableSchema(_type);
                    }
                    else {
                        if (typeof(IEnumerable).IsAssignableFrom(_type)) { 
                            _schema = GetEnumerableSchema(_type);
                        } 
                        else { 
                            _schema = GetTypeSchema(_type);
                        } 
                    }
                }
            }
        } 

        public IDataSourceViewSchema[] GetViews() { 
            return _schema; 
        }
 
        /// <devdoc>
        /// Gets schema for a strongly typed DataSet.
        /// </devdoc>
        private static IDataSourceViewSchema[] GetDataSetSchema(Type t) { 
            try {
                DataSet dataSet = Activator.CreateInstance(t) as DataSet; 
 
                System.Collections.Generic.List<IDataSourceViewSchema> views = new System.Collections.Generic.List<IDataSourceViewSchema>();
                foreach (DataTable table in dataSet.Tables) { 
                    views.Add(new DataSetViewSchema(table));
                }
                return views.ToArray();
            } 
            catch {
                return null; 
            } 
        }
 
        /// <devdoc>
        /// Gets schema for a strongly typed DataTable.
        /// </devdoc>
        private static IDataSourceViewSchema[] GetDataTableSchema(Type t) { 
            try {
                DataTable table = Activator.CreateInstance(t) as DataTable; 
 
                DataSetViewSchema tableSchema = new DataSetViewSchema(table);
                return new IDataSourceViewSchema[1] { tableSchema }; 
            }
            catch {
                return null;
            } 
        }
 
        /// <devdoc> 
        /// Gets schema for a strongly typed enumerable.
        /// </devdoc> 
        private static IDataSourceViewSchema[] GetEnumerableSchema(Type t) {
            TypeEnumerableViewSchema enumerableSchema = new TypeEnumerableViewSchema(String.Empty, t);
            return new IDataSourceViewSchema[1] { enumerableSchema };
        } 

        /// <devdoc> 
        /// Gets schema for a generic IEnumerable. 
        /// </devdoc>
        private static IDataSourceViewSchema[] GetGenericEnumerableSchema(Type t) { 
            TypeGenericEnumerableViewSchema enumerableSchema = new TypeGenericEnumerableViewSchema(String.Empty, t);
            return new IDataSourceViewSchema[1] { enumerableSchema };
        }
 
        /// <devdoc>
        /// Gets schema for an arbitrary type. 
        /// </devdoc> 
        private static IDataSourceViewSchema[] GetTypeSchema(Type t) {
            TypeViewSchema typeSchema = new TypeViewSchema(String.Empty, t); 
            return new IDataSourceViewSchema[1] { typeSchema };
        }

        /// <devdoc> 
        /// Returns true if the type implements IEnumerable&lt;T&gt; with a bound
        /// value for T. In other words, this type: 
        ///     public class Foo&lt;T&gt; : IEnumerable&lt;T&gt; { ... } 
        /// Does not bind the value of T. However this type:
        ///     public class Foo2 : IEnumerable&lt;string&gt; { ... } 
        /// Does have T bound to the type System.String.
        ///
        /// Although it would seem that this check:
        ///     typeof(IEnumerable&lt;&gt;).IsAssignableFrom(t) 
        /// Might be sufficient, it actually doesn't work, possibly due
        /// to covariance/contravariance limitations, etc. 
        /// </devdoc> 
        internal static bool IsBoundGenericEnumerable(Type t) {
            Type[] interfaces = null; 
            if (t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                // If the type already is IEnumerable<T>, that's the interface we want to inspect
                interfaces = new Type[] { t };
            } 
            else {
                // Otherwise we get a list of all the interafaces the type implements 
                interfaces = t.GetInterfaces(); 
            }
            foreach (Type i in interfaces) { 
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                    Type[] genericArguments = i.GetGenericArguments();
                    Debug.Assert(genericArguments != null && genericArguments.Length == 1, "Expected IEnumerable<T> to have a generic argument");
                    // If a type has IsGenericParameter=true that means it is not bound. 
                    return !genericArguments[0].IsGenericParameter;
                } 
            } 
            return false;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
