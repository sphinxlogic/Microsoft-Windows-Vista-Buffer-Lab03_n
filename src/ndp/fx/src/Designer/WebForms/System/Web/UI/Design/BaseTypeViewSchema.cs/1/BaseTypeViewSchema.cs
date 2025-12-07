//------------------------------------------------------------------------------ 
// <copyright file="BaseTypeViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
 
    /// <devdoc>
    /// Represents a view's schema based on a Type object retrieved 
    /// through Reflection. This is the base class for several view schema 
    /// types.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal abstract class BaseTypeViewSchema : IDataSourceViewSchema {
        private Type _type;
        private string _viewName; 

        protected BaseTypeViewSchema(string viewName, Type type) { 
            Debug.Assert(type != null); 
            _type = type;
            _viewName = viewName; 
        }

        public IDataSourceFieldSchema[] GetFields() {
            // Search for indexer property 
            System.Collections.Generic.List<IDataSourceFieldSchema> fields = new System.Collections.Generic.List<IDataSourceFieldSchema>();
            Type rowType = GetRowType(_type); 
            if (rowType != null) { 
                // We specifically don't get schema when the type implements
                // ICustomTypeDescriptor since it is unlikely to have the 
                // correct schema at design time.
                if (!typeof(ICustomTypeDescriptor).IsAssignableFrom(rowType)) {
                    PropertyDescriptorCollection rowProperties = TypeDescriptor.GetProperties(rowType);
                    foreach (PropertyDescriptor rowProperty in rowProperties) { 
                        fields.Add(new TypeFieldSchema(rowProperty));
                    } 
                } 
            }
            return fields.ToArray(); 
        }

        public IDataSourceViewSchema[] GetChildren() {
            return null; 
        }
 
        /// <devdoc> 
        /// Derived classes must implement this method to retrieve the row
        /// type for a given object types. For example, in a strongly typed 
        /// DataTable the row type would be the strongly typed DataRow.
        /// </devdoc>
        protected abstract Type GetRowType(Type objectType);
 
        public string Name {
            get { 
                return _viewName; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BaseTypeViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
 
    /// <devdoc>
    /// Represents a view's schema based on a Type object retrieved 
    /// through Reflection. This is the base class for several view schema 
    /// types.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal abstract class BaseTypeViewSchema : IDataSourceViewSchema {
        private Type _type;
        private string _viewName; 

        protected BaseTypeViewSchema(string viewName, Type type) { 
            Debug.Assert(type != null); 
            _type = type;
            _viewName = viewName; 
        }

        public IDataSourceFieldSchema[] GetFields() {
            // Search for indexer property 
            System.Collections.Generic.List<IDataSourceFieldSchema> fields = new System.Collections.Generic.List<IDataSourceFieldSchema>();
            Type rowType = GetRowType(_type); 
            if (rowType != null) { 
                // We specifically don't get schema when the type implements
                // ICustomTypeDescriptor since it is unlikely to have the 
                // correct schema at design time.
                if (!typeof(ICustomTypeDescriptor).IsAssignableFrom(rowType)) {
                    PropertyDescriptorCollection rowProperties = TypeDescriptor.GetProperties(rowType);
                    foreach (PropertyDescriptor rowProperty in rowProperties) { 
                        fields.Add(new TypeFieldSchema(rowProperty));
                    } 
                } 
            }
            return fields.ToArray(); 
        }

        public IDataSourceViewSchema[] GetChildren() {
            return null; 
        }
 
        /// <devdoc> 
        /// Derived classes must implement this method to retrieve the row
        /// type for a given object types. For example, in a strongly typed 
        /// DataTable the row type would be the strongly typed DataRow.
        /// </devdoc>
        protected abstract Type GetRowType(Type objectType);
 
        public string Name {
            get { 
                return _viewName; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
