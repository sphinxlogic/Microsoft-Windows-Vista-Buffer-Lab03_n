//------------------------------------------------------------------------------ 
// <copyright file="TypeFieldSchema.cs" company="Microsoft">
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
    /// Represents a field's schema based on a PropertyDescriptor object. 
    /// This is used by the TypeSchema class to provide schema for arbitrary types. 
    /// If the property has the DataObjectFieldAttribute then it is used to get
    /// additional information about the field. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class TypeFieldSchema : IDataSourceFieldSchema {
        private PropertyDescriptor _fieldDescriptor; 
        private bool _retrievedMetaData;
        private bool _primaryKey; 
        private bool _isIdentity; 
        private bool _isNullable;
        private int _length = -1; 

        public TypeFieldSchema(PropertyDescriptor fieldDescriptor) {
            if (fieldDescriptor == null) {
                throw new ArgumentNullException("fieldDescriptor"); 
            }
            _fieldDescriptor = fieldDescriptor; 
        } 

        public Type DataType { 
            get {
                // If the type is Nullable<T> then we just want the T
                Type type = _fieldDescriptor.PropertyType;
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>))) { 
                    return type.GetGenericArguments()[0];
                } 
                return type; 
            }
        } 

        public bool Identity {
            get {
                EnsureMetaData(); 
                return _isIdentity;
            } 
        } 

        public bool IsReadOnly { 
            get {
                return _fieldDescriptor.IsReadOnly;
            }
        } 

        public bool IsUnique { 
            get { 
                return false;
            } 
        }

        public int Length {
            get { 
                EnsureMetaData();
                return _length; 
            } 
        }
 
        public string Name {
            get {
                return _fieldDescriptor.Name;
            } 
        }
 
        public bool Nullable { 
            get {
                // All reference types are nullable, and value types wrapped 
                // in Nullable<> are nullable too.
                EnsureMetaData();
                Type type = _fieldDescriptor.PropertyType;
                return (!type.IsValueType) || _isNullable || 
                    (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
            } 
        } 

        public int Precision { 
            get {
                return -1;
            }
        } 

        public bool PrimaryKey { 
            get { 
                EnsureMetaData();
                return _primaryKey; 
            }
        }

        public int Scale { 
            get {
                return -1; 
            } 
        }
 
        private void EnsureMetaData() {
            if (_retrievedMetaData) {
                return;
            } 
            DataObjectFieldAttribute attr = (DataObjectFieldAttribute)_fieldDescriptor.Attributes[typeof(DataObjectFieldAttribute)];
            if (attr != null) { 
                _primaryKey = attr.PrimaryKey; 
                _isIdentity = attr.IsIdentity;
                _isNullable = attr.IsNullable; 
                _length = attr.Length;
            }
            _retrievedMetaData = true;
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TypeFieldSchema.cs" company="Microsoft">
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
    /// Represents a field's schema based on a PropertyDescriptor object. 
    /// This is used by the TypeSchema class to provide schema for arbitrary types. 
    /// If the property has the DataObjectFieldAttribute then it is used to get
    /// additional information about the field. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class TypeFieldSchema : IDataSourceFieldSchema {
        private PropertyDescriptor _fieldDescriptor; 
        private bool _retrievedMetaData;
        private bool _primaryKey; 
        private bool _isIdentity; 
        private bool _isNullable;
        private int _length = -1; 

        public TypeFieldSchema(PropertyDescriptor fieldDescriptor) {
            if (fieldDescriptor == null) {
                throw new ArgumentNullException("fieldDescriptor"); 
            }
            _fieldDescriptor = fieldDescriptor; 
        } 

        public Type DataType { 
            get {
                // If the type is Nullable<T> then we just want the T
                Type type = _fieldDescriptor.PropertyType;
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>))) { 
                    return type.GetGenericArguments()[0];
                } 
                return type; 
            }
        } 

        public bool Identity {
            get {
                EnsureMetaData(); 
                return _isIdentity;
            } 
        } 

        public bool IsReadOnly { 
            get {
                return _fieldDescriptor.IsReadOnly;
            }
        } 

        public bool IsUnique { 
            get { 
                return false;
            } 
        }

        public int Length {
            get { 
                EnsureMetaData();
                return _length; 
            } 
        }
 
        public string Name {
            get {
                return _fieldDescriptor.Name;
            } 
        }
 
        public bool Nullable { 
            get {
                // All reference types are nullable, and value types wrapped 
                // in Nullable<> are nullable too.
                EnsureMetaData();
                Type type = _fieldDescriptor.PropertyType;
                return (!type.IsValueType) || _isNullable || 
                    (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
            } 
        } 

        public int Precision { 
            get {
                return -1;
            }
        } 

        public bool PrimaryKey { 
            get { 
                EnsureMetaData();
                return _primaryKey; 
            }
        }

        public int Scale { 
            get {
                return -1; 
            } 
        }
 
        private void EnsureMetaData() {
            if (_retrievedMetaData) {
                return;
            } 
            DataObjectFieldAttribute attr = (DataObjectFieldAttribute)_fieldDescriptor.Attributes[typeof(DataObjectFieldAttribute)];
            if (attr != null) { 
                _primaryKey = attr.PrimaryKey; 
                _isIdentity = attr.IsIdentity;
                _isNullable = attr.IsNullable; 
                _length = attr.Length;
            }
            _retrievedMetaData = true;
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
