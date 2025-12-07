//------------------------------------------------------------------------------ 
// <copyright file="DataMemberConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Runtime.InteropServices; 
    using System.Web.UI.Design.WebControls;
 

    /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter"]/*' />
    /// <devdoc>
    ///    <para> 
    ///       Provides design-time support for a component's DataMember properties.
    ///    </para> 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class DataMemberConverter : TypeConverter { 

        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.DataMemberConverter"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.DataFieldConverter'/>.
        ///    </para> 
        /// </devdoc> 
        public DataMemberConverter() {
        } 

        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.CanConvertFrom"]/*' />
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
 
        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.ConvertFrom"]/*' />
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

        /// <devdoc> 
        ///    <para> 
        ///       Returns the DesignerDataSourceView of the given data bound control designer.
        ///    </para> 
        /// </devdoc>
        private DesignerDataSourceView GetView(IDesigner dataBoundControlDesigner) {
            DataBoundControlDesigner dbcDesigner = dataBoundControlDesigner as DataBoundControlDesigner;
            if (dbcDesigner != null) { 
                return dbcDesigner.DesignerView;
            } 
            else { 
                BaseDataListDesigner baseDataListDesigner = dataBoundControlDesigner as BaseDataListDesigner;
                if (baseDataListDesigner != null) { 
                    return baseDataListDesigner.DesignerView;
                }
                else {
                    RepeaterDesigner repeaterDesigner = dataBoundControlDesigner as RepeaterDesigner; 
                    if (repeaterDesigner != null) {
                        return repeaterDesigner.DesignerView; 
                    } 
                }
            } 
            return null;
        }

        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.GetStandardValues"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets the fields present within the selected data source if information about them is available. 
        ///    </para>
        /// </devdoc> 
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            string[] names = null;

            if (context != null) { 
                //
 
 

                // This converter shouldn't be used in a multi-select scenario. If it is, it simply 
                // returns no standard values.
                IComponent component = context.Instance as IComponent;
                if (component != null) {
                    ISite componentSite = component.Site; 
                    if (componentSite != null) {
                        IDesignerHost designerHost = (IDesignerHost)componentSite.GetService(typeof(IDesignerHost)); 
                        if (designerHost != null) { 
                            IDesigner dataBoundControlDesigner = designerHost.GetDesigner(component);
                            DesignerDataSourceView view = GetView(dataBoundControlDesigner); 

                            if (view != null) {
                                IDataSourceDesigner dsDesigner = view.DataSourceDesigner;
                                if (dsDesigner != null) { 
                                    string[] viewNames = dsDesigner.GetViewNames();
                                    if (viewNames != null) { 
                                        names = new string[viewNames.Length]; 
                                        viewNames.CopyTo(names, 0);
                                    } 
                                }
                            }
                            if ((names == null) && (dataBoundControlDesigner != null) && (dataBoundControlDesigner is IDataSourceProvider)) {
                                IDataSourceProvider dataSourceProvider = dataBoundControlDesigner as IDataSourceProvider; 
                                object dataSource = null;
 
                                if (dataSourceProvider != null) { 
                                    dataSource = dataSourceProvider.GetSelectedDataSource();
                                } 

                                if (dataSource != null) {
                                    names = DesignTimeData.GetDataMembers(dataSource);
                                } 
                            }
                        } 
                    } 
                }
                if (names == null) { 
                    names = new string[0];
                }
                Array.Sort(names, Comparer.Default);
            } 
            return new StandardValuesCollection(names);
        } 
 
        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.GetStandardValuesExclusive"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets a value indicating whether the collection of standard values returned from
        ///    <see cref='System.ComponentModel.TypeConverter.GetStandardValues'/> is an exclusive
        ///       list of possible values, using the specified context. 
        ///    </para>
        /// </devdoc> 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
            return false;
        } 

        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.GetStandardValuesSupported"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets a value indicating whether this object supports a standard set of values
        ///       that can be picked from a list. 
        ///    </para> 
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
            if (context != null && context.Instance is IComponent) {
                // We only support the dropdown in single-select mode.
                return true;
            } 
            return false;
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataMemberConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Runtime.InteropServices; 
    using System.Web.UI.Design.WebControls;
 

    /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter"]/*' />
    /// <devdoc>
    ///    <para> 
    ///       Provides design-time support for a component's DataMember properties.
    ///    </para> 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class DataMemberConverter : TypeConverter { 

        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.DataMemberConverter"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.DataFieldConverter'/>.
        ///    </para> 
        /// </devdoc> 
        public DataMemberConverter() {
        } 

        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.CanConvertFrom"]/*' />
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
 
        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.ConvertFrom"]/*' />
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

        /// <devdoc> 
        ///    <para> 
        ///       Returns the DesignerDataSourceView of the given data bound control designer.
        ///    </para> 
        /// </devdoc>
        private DesignerDataSourceView GetView(IDesigner dataBoundControlDesigner) {
            DataBoundControlDesigner dbcDesigner = dataBoundControlDesigner as DataBoundControlDesigner;
            if (dbcDesigner != null) { 
                return dbcDesigner.DesignerView;
            } 
            else { 
                BaseDataListDesigner baseDataListDesigner = dataBoundControlDesigner as BaseDataListDesigner;
                if (baseDataListDesigner != null) { 
                    return baseDataListDesigner.DesignerView;
                }
                else {
                    RepeaterDesigner repeaterDesigner = dataBoundControlDesigner as RepeaterDesigner; 
                    if (repeaterDesigner != null) {
                        return repeaterDesigner.DesignerView; 
                    } 
                }
            } 
            return null;
        }

        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.GetStandardValues"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets the fields present within the selected data source if information about them is available. 
        ///    </para>
        /// </devdoc> 
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            string[] names = null;

            if (context != null) { 
                //
 
 

                // This converter shouldn't be used in a multi-select scenario. If it is, it simply 
                // returns no standard values.
                IComponent component = context.Instance as IComponent;
                if (component != null) {
                    ISite componentSite = component.Site; 
                    if (componentSite != null) {
                        IDesignerHost designerHost = (IDesignerHost)componentSite.GetService(typeof(IDesignerHost)); 
                        if (designerHost != null) { 
                            IDesigner dataBoundControlDesigner = designerHost.GetDesigner(component);
                            DesignerDataSourceView view = GetView(dataBoundControlDesigner); 

                            if (view != null) {
                                IDataSourceDesigner dsDesigner = view.DataSourceDesigner;
                                if (dsDesigner != null) { 
                                    string[] viewNames = dsDesigner.GetViewNames();
                                    if (viewNames != null) { 
                                        names = new string[viewNames.Length]; 
                                        viewNames.CopyTo(names, 0);
                                    } 
                                }
                            }
                            if ((names == null) && (dataBoundControlDesigner != null) && (dataBoundControlDesigner is IDataSourceProvider)) {
                                IDataSourceProvider dataSourceProvider = dataBoundControlDesigner as IDataSourceProvider; 
                                object dataSource = null;
 
                                if (dataSourceProvider != null) { 
                                    dataSource = dataSourceProvider.GetSelectedDataSource();
                                } 

                                if (dataSource != null) {
                                    names = DesignTimeData.GetDataMembers(dataSource);
                                } 
                            }
                        } 
                    } 
                }
                if (names == null) { 
                    names = new string[0];
                }
                Array.Sort(names, Comparer.Default);
            } 
            return new StandardValuesCollection(names);
        } 
 
        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.GetStandardValuesExclusive"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets a value indicating whether the collection of standard values returned from
        ///    <see cref='System.ComponentModel.TypeConverter.GetStandardValues'/> is an exclusive
        ///       list of possible values, using the specified context. 
        ///    </para>
        /// </devdoc> 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
            return false;
        } 

        /// <include file='doc\DataMemberConverter.uex' path='docs/doc[@for="DataMemberConverter.GetStandardValuesSupported"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets a value indicating whether this object supports a standard set of values
        ///       that can be picked from a list. 
        ///    </para> 
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
            if (context != null && context.Instance is IComponent) {
                // We only support the dropdown in single-select mode.
                return true;
            } 
            return false;
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
