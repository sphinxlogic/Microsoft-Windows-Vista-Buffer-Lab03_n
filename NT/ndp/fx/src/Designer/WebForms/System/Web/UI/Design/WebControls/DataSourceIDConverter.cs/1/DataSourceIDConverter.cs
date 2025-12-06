//------------------------------------------------------------------------------ 
// <copyright file="DataSourceIDConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.CodeDom; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Runtime.InteropServices;
    using System.Globalization; 
    using System.Collections.Generic;

    /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter"]/*' />
    public class DataSourceIDConverter : TypeConverter { 

        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.DataSourceIDConverter"]/*' /> 
        public DataSourceIDConverter() { 
        }
 
        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.CanConvertFrom"]/*' />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true; 
            }
            return false; 
        } 

        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.ConvertFrom"]/*' /> 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value == null) {
                return String.Empty;
            } 
            else if (value.GetType() == typeof(string)) {
                return (string)value; 
            } 
            throw GetConvertFromException(value);
        } 

        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.GetStandardValues"]/*' />
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            string[] idsArray = null; 

            if (context != null) { 
                WebFormsRootDesigner rootDesigner = null; 
                IDesignerHost designerHost = (IDesignerHost)(context.GetService(typeof(IDesignerHost)));
                Debug.Assert(designerHost != null, "Did not get DesignerHost service."); 

                if (designerHost != null) {
                    IComponent rootComponent = designerHost.RootComponent;
                    if (rootComponent != null) { 
                        rootDesigner = designerHost.GetDesigner(rootComponent) as WebFormsRootDesigner;
                    } 
                } 

                if (rootDesigner != null && !rootDesigner.IsDesignerViewLocked) { 
                    // Walk up the list of naming containers to get all accessible data sources
                    IComponent component = context.Instance as IComponent;
                    if (component == null) {
                        // In case we are hosted in a DesignerActionList we need 
                        // to find out the component that the action list belongs to
                        DesignerActionList actionList = context.Instance as DesignerActionList; 
                        if (actionList != null) { 
                            component = actionList.Component;
                        } 
                    }

                    IList<IComponent> allComponents = ControlHelper.GetAllComponents(component, new ControlHelper.IsValidComponentDelegate(IsValidDataSource));
                    List<string> uniqueControlIDs = new List<string>(); 
                    foreach (IComponent c in allComponents) {
                        Control control = c as Control; 
                        if (control != null && !String.IsNullOrEmpty(control.ID)) { 
                            if (!uniqueControlIDs.Contains(control.ID)) {
                                uniqueControlIDs.Add(control.ID); 
                            }
                        }
                    }
 
                    uniqueControlIDs.Sort(StringComparer.OrdinalIgnoreCase);
                    uniqueControlIDs.Insert(0, SR.GetString(SR.DataSourceIDChromeConverter_NoDataSource)); 
                    uniqueControlIDs.Add(SR.GetString(SR.DataSourceIDChromeConverter_NewDataSource)); 
                    idsArray = uniqueControlIDs.ToArray();
                } 
            }
            return new StandardValuesCollection(idsArray);
        }
 
        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.GetStandardValuesExclusive"]/*' />
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
            return false; 
        }
 
        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.GetStandardValuesSupported"]/*' />
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        } 

        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.IsValidDataSource"]/*' /> 
        protected virtual bool IsValidDataSource(IComponent component) { 
            Control control = component as Control;
            if (control == null) { 
                return false;
            }
            if (String.IsNullOrEmpty(control.ID)) {
                return false; 
            }
            return (component is IDataSource); 
        } 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataSourceIDConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.CodeDom; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Runtime.InteropServices;
    using System.Globalization; 
    using System.Collections.Generic;

    /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter"]/*' />
    public class DataSourceIDConverter : TypeConverter { 

        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.DataSourceIDConverter"]/*' /> 
        public DataSourceIDConverter() { 
        }
 
        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.CanConvertFrom"]/*' />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true; 
            }
            return false; 
        } 

        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.ConvertFrom"]/*' /> 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value == null) {
                return String.Empty;
            } 
            else if (value.GetType() == typeof(string)) {
                return (string)value; 
            } 
            throw GetConvertFromException(value);
        } 

        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.GetStandardValues"]/*' />
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            string[] idsArray = null; 

            if (context != null) { 
                WebFormsRootDesigner rootDesigner = null; 
                IDesignerHost designerHost = (IDesignerHost)(context.GetService(typeof(IDesignerHost)));
                Debug.Assert(designerHost != null, "Did not get DesignerHost service."); 

                if (designerHost != null) {
                    IComponent rootComponent = designerHost.RootComponent;
                    if (rootComponent != null) { 
                        rootDesigner = designerHost.GetDesigner(rootComponent) as WebFormsRootDesigner;
                    } 
                } 

                if (rootDesigner != null && !rootDesigner.IsDesignerViewLocked) { 
                    // Walk up the list of naming containers to get all accessible data sources
                    IComponent component = context.Instance as IComponent;
                    if (component == null) {
                        // In case we are hosted in a DesignerActionList we need 
                        // to find out the component that the action list belongs to
                        DesignerActionList actionList = context.Instance as DesignerActionList; 
                        if (actionList != null) { 
                            component = actionList.Component;
                        } 
                    }

                    IList<IComponent> allComponents = ControlHelper.GetAllComponents(component, new ControlHelper.IsValidComponentDelegate(IsValidDataSource));
                    List<string> uniqueControlIDs = new List<string>(); 
                    foreach (IComponent c in allComponents) {
                        Control control = c as Control; 
                        if (control != null && !String.IsNullOrEmpty(control.ID)) { 
                            if (!uniqueControlIDs.Contains(control.ID)) {
                                uniqueControlIDs.Add(control.ID); 
                            }
                        }
                    }
 
                    uniqueControlIDs.Sort(StringComparer.OrdinalIgnoreCase);
                    uniqueControlIDs.Insert(0, SR.GetString(SR.DataSourceIDChromeConverter_NoDataSource)); 
                    uniqueControlIDs.Add(SR.GetString(SR.DataSourceIDChromeConverter_NewDataSource)); 
                    idsArray = uniqueControlIDs.ToArray();
                } 
            }
            return new StandardValuesCollection(idsArray);
        }
 
        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.GetStandardValuesExclusive"]/*' />
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
            return false; 
        }
 
        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.GetStandardValuesSupported"]/*' />
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        } 

        /// <include file='doc\DataSourceIDConverter.uex' path='docs/doc[@for="DataSourceIDConverter.IsValidDataSource"]/*' /> 
        protected virtual bool IsValidDataSource(IComponent component) { 
            Control control = component as Control;
            if (control == null) { 
                return false;
            }
            if (String.IsNullOrEmpty(control.ID)) {
                return false; 
            }
            return (component is IDataSource); 
        } 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
