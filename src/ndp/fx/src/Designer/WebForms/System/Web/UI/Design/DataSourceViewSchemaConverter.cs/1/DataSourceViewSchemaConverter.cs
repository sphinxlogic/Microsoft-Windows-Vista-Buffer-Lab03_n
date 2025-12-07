//------------------------------------------------------------------------------ 
// <copyright file="DataSourceViewSchemaConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Runtime.InteropServices; 
    using System.Web.UI;
 
 
    /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides design-time support for getting schema from an object
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class DataSourceViewSchemaConverter : TypeConverter { 
 
        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.DataSourceViewSchemaConverter"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public DataSourceViewSchemaConverter() {
        }
 
        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.CanConvertFrom"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets a value indicating whether this converter can
        ///       convert an object in the given source type to the native type of the converter 
        ///       using the context.
        ///    </para>
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
            if (sourceType == typeof(string)) {
                return true; 
            } 
            return false;
        } 

        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.ConvertFrom"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Converts the given object to the converter's native type.
        ///    </para> 
        /// </devdoc> 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value == null) { 
                return String.Empty;
            }
            else if (value.GetType() == typeof(string)) {
                return (string)value; 
            }
            throw GetConvertFromException(value); 
        } 

        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.GetStandardValues"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the fields present within the selected object's schema
        ///    </para> 
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            return GetStandardValues(context, null); 
        }
 
        public virtual StandardValuesCollection GetStandardValues(ITypeDescriptorContext context, Type typeFilter) {
            string[] names = null;

            if (context != null) { 
                IDataSourceViewSchemaAccessor schemaAccessor = context.Instance as IDataSourceViewSchemaAccessor;
                if (schemaAccessor != null) { 
                    IDataSourceViewSchema schema = schemaAccessor.DataSourceViewSchema as IDataSourceViewSchema; 
                    if (schema != null) {
                        IDataSourceFieldSchema[] fields = schema.GetFields(); 
                        string[] tempNames = new string[fields.Length];
                        int fieldCount = 0;
                        for (int i = 0; i < fields.Length; i++) {
                            if ((typeFilter != null && fields[i].DataType == typeFilter) || typeFilter == null) { 
                                tempNames[fieldCount] = fields[i].Name;
                                fieldCount++; 
                            } 
                        }
                        names = new string[fieldCount]; 
                        Array.Copy(tempNames, names, fieldCount);
                    }
                }
 
                if (names == null) {
                    names = new string[0]; 
                } 
                Array.Sort(names, Comparer.Default);
            } 

            return new StandardValuesCollection(names);

        } 

        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.GetStandardValuesExclusive"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Gets a value indicating whether the collection of standard values returned from 
        ///       <see cref='System.ComponentModel.TypeConverter.GetStandardValues'/> is an exclusive
        ///       list of possible values, using the specified context.
        ///    </para>
        /// </devdoc> 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return false; 
        } 

        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.GetStandardValuesSupported"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether this object supports a standard set of values
        ///       that can be picked from a list. 
        ///    </para>
        /// </devdoc> 
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
            if ((context != null) && (context.Instance is IDataSourceViewSchemaAccessor)) {
                return true; 
            }
            return false;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataSourceViewSchemaConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Runtime.InteropServices; 
    using System.Web.UI;
 
 
    /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides design-time support for getting schema from an object
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class DataSourceViewSchemaConverter : TypeConverter { 
 
        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.DataSourceViewSchemaConverter"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public DataSourceViewSchemaConverter() {
        }
 
        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.CanConvertFrom"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets a value indicating whether this converter can
        ///       convert an object in the given source type to the native type of the converter 
        ///       using the context.
        ///    </para>
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
            if (sourceType == typeof(string)) {
                return true; 
            } 
            return false;
        } 

        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.ConvertFrom"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Converts the given object to the converter's native type.
        ///    </para> 
        /// </devdoc> 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value == null) { 
                return String.Empty;
            }
            else if (value.GetType() == typeof(string)) {
                return (string)value; 
            }
            throw GetConvertFromException(value); 
        } 

        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.GetStandardValues"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the fields present within the selected object's schema
        ///    </para> 
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            return GetStandardValues(context, null); 
        }
 
        public virtual StandardValuesCollection GetStandardValues(ITypeDescriptorContext context, Type typeFilter) {
            string[] names = null;

            if (context != null) { 
                IDataSourceViewSchemaAccessor schemaAccessor = context.Instance as IDataSourceViewSchemaAccessor;
                if (schemaAccessor != null) { 
                    IDataSourceViewSchema schema = schemaAccessor.DataSourceViewSchema as IDataSourceViewSchema; 
                    if (schema != null) {
                        IDataSourceFieldSchema[] fields = schema.GetFields(); 
                        string[] tempNames = new string[fields.Length];
                        int fieldCount = 0;
                        for (int i = 0; i < fields.Length; i++) {
                            if ((typeFilter != null && fields[i].DataType == typeFilter) || typeFilter == null) { 
                                tempNames[fieldCount] = fields[i].Name;
                                fieldCount++; 
                            } 
                        }
                        names = new string[fieldCount]; 
                        Array.Copy(tempNames, names, fieldCount);
                    }
                }
 
                if (names == null) {
                    names = new string[0]; 
                } 
                Array.Sort(names, Comparer.Default);
            } 

            return new StandardValuesCollection(names);

        } 

        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.GetStandardValuesExclusive"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Gets a value indicating whether the collection of standard values returned from 
        ///       <see cref='System.ComponentModel.TypeConverter.GetStandardValues'/> is an exclusive
        ///       list of possible values, using the specified context.
        ///    </para>
        /// </devdoc> 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return false; 
        } 

        /// <include file='doc\DataSourceViewSchemaConverter.uex' path='docs/doc[@for="DataSourceViewSchemaConverter.GetStandardValuesSupported"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether this object supports a standard set of values
        ///       that can be picked from a list. 
        ///    </para>
        /// </devdoc> 
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
            if ((context != null) && (context.Instance is IDataSourceViewSchemaAccessor)) {
                return true; 
            }
            return false;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
