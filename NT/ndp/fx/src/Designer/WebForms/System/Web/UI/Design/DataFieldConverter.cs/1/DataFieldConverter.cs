//------------------------------------------------------------------------------ 
// <copyright file="DataFieldConverter.cs" company="Microsoft">
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
    using System.Runtime.InteropServices; 
    using System.Globalization; 
    using System.Web.UI.Design.WebControls;
 
    /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides design-time support for a component's data field properties. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class DataFieldConverter : TypeConverter {
 
        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.DataFieldConverter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.DataFieldConverter'/>. 
        ///    </para>
        /// </devdoc> 
        public DataFieldConverter() { 
        }
 
        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.CanConvertFrom"]/*' />
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
 
        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.ConvertFrom"]/*' /> 
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

        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.GetStandardValues"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the fields present within the selected data source if information about them is available. 
        ///    </para> 
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            object[] names = null;

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
                                IDataSourceViewSchema schema = null;
                                try { 
                                    schema = view.Schema;
                                } 
                                catch (Exception ex) { 
                                    IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)componentSite.GetService(typeof(IComponentDesignerDebugService));
                                    if (debugService != null) { 
                                        debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message));
                                    }
                                }
                                if (schema != null) { 
                                    IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                                    if (fieldSchemas != null) { 
                                        names = new object[fieldSchemas.Length]; 
                                        for (int i = 0; i < fieldSchemas.Length; i++) {
                                            names[i] = fieldSchemas[i].Name; 
                                        }
                                    }
                                }
                            } 
                            if (names == null && dataBoundControlDesigner != null && dataBoundControlDesigner is IDataSourceProvider) {
                                IDataSourceProvider dataSourceProvider = dataBoundControlDesigner as IDataSourceProvider; 
                                IEnumerable dataSource = null; 

                                if (dataSourceProvider != null) { 
                                    dataSource = dataSourceProvider.GetResolvedSelectedDataSource();
                                }

                                if (dataSource != null) { 
                                    PropertyDescriptorCollection props = DesignTimeData.GetDataFields(dataSource);
                                    if (props != null) { 
                                        ArrayList list = new ArrayList(); 
                                        foreach (PropertyDescriptor propDesc in props) {
                                            list.Add(propDesc.Name); 
                                        }
                                        names = list.ToArray();
                                    }
                                } 
                            }
                        } 
                    } 
                }
            } 
            return new StandardValuesCollection(names);
        }

        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.GetStandardValuesExclusive"]/*' /> 
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
 
        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.GetStandardValuesSupported"]/*' /> 
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
// <copyright file="DataFieldConverter.cs" company="Microsoft">
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
    using System.Runtime.InteropServices; 
    using System.Globalization; 
    using System.Web.UI.Design.WebControls;
 
    /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides design-time support for a component's data field properties. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class DataFieldConverter : TypeConverter {
 
        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.DataFieldConverter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.DataFieldConverter'/>. 
        ///    </para>
        /// </devdoc> 
        public DataFieldConverter() { 
        }
 
        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.CanConvertFrom"]/*' />
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
 
        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.ConvertFrom"]/*' /> 
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

        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.GetStandardValues"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the fields present within the selected data source if information about them is available. 
        ///    </para> 
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            object[] names = null;

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
                                IDataSourceViewSchema schema = null;
                                try { 
                                    schema = view.Schema;
                                } 
                                catch (Exception ex) { 
                                    IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)componentSite.GetService(typeof(IComponentDesignerDebugService));
                                    if (debugService != null) { 
                                        debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message));
                                    }
                                }
                                if (schema != null) { 
                                    IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                                    if (fieldSchemas != null) { 
                                        names = new object[fieldSchemas.Length]; 
                                        for (int i = 0; i < fieldSchemas.Length; i++) {
                                            names[i] = fieldSchemas[i].Name; 
                                        }
                                    }
                                }
                            } 
                            if (names == null && dataBoundControlDesigner != null && dataBoundControlDesigner is IDataSourceProvider) {
                                IDataSourceProvider dataSourceProvider = dataBoundControlDesigner as IDataSourceProvider; 
                                IEnumerable dataSource = null; 

                                if (dataSourceProvider != null) { 
                                    dataSource = dataSourceProvider.GetResolvedSelectedDataSource();
                                }

                                if (dataSource != null) { 
                                    PropertyDescriptorCollection props = DesignTimeData.GetDataFields(dataSource);
                                    if (props != null) { 
                                        ArrayList list = new ArrayList(); 
                                        foreach (PropertyDescriptor propDesc in props) {
                                            list.Add(propDesc.Name); 
                                        }
                                        names = list.ToArray();
                                    }
                                } 
                            }
                        } 
                    } 
                }
            } 
            return new StandardValuesCollection(names);
        }

        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.GetStandardValuesExclusive"]/*' /> 
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
 
        /// <include file='doc\DataFieldConverter.uex' path='docs/doc[@for="DataFieldConverter.GetStandardValuesSupported"]/*' /> 
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
