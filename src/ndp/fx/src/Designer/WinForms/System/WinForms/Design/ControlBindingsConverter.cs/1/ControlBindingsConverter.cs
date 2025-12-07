//------------------------------------------------------------------------------ 
// <copyright file="ControlBindingsConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ControlBindingsConverter..ctor()")]
 
namespace System.Windows.Forms.Design { 

    using System; 
    using Microsoft.Win32;
    using System.Collections;
    using System.ComponentModel;
    using System.Globalization; 

    internal class ControlBindingsConverter : TypeConverter { 
 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) { 
                // return "(Bindings)";
                // return an empty string, since we don't want a meaningless
                // string displayed as the value for the expandable Bindings property
                return ""; 
            }
            return base.ConvertTo(context, culture, value, destinationType); 
        } 

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) { 
            if (value is ControlBindingsCollection) {
                ControlBindingsCollection collection = (ControlBindingsCollection)value;
                IBindableComponent control = collection.BindableComponent;
                Type type = control.GetType(); 

                PropertyDescriptorCollection bindableProperties = TypeDescriptor.GetProperties(control, null); 
                ArrayList props = new ArrayList(); 
                for (int i = 0; i < bindableProperties.Count; i++) {
                    // Create a read only binding if the data source is not one of the values we support. 
                    Binding binding = collection[bindableProperties[i].Name];
                    bool readOnly = !(binding == null || binding.DataSource is IListSource || binding.DataSource is IList || binding.DataSource is Array);
                    DesignBindingPropertyDescriptor property = new DesignBindingPropertyDescriptor(bindableProperties[i], null, readOnly);
                    bool bindable = ((BindableAttribute)bindableProperties[i].Attributes[typeof(BindableAttribute)]).Bindable; 
                    if (bindable || !((DesignBinding)property.GetValue(collection)).IsNull) {
                        props.Add(property); 
                    } 
                }
 
                props.Add(new AdvancedBindingPropertyDescriptor());
                PropertyDescriptor[] propArray = new PropertyDescriptor[props.Count];
                props.CopyTo(propArray,0);
                return new PropertyDescriptorCollection(propArray); 
            }
            return new PropertyDescriptorCollection(new PropertyDescriptor[0]); 
        } 

        public override bool GetPropertiesSupported(ITypeDescriptorContext context) { 
            return true;
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlBindingsConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ControlBindingsConverter..ctor()")]
 
namespace System.Windows.Forms.Design { 

    using System; 
    using Microsoft.Win32;
    using System.Collections;
    using System.ComponentModel;
    using System.Globalization; 

    internal class ControlBindingsConverter : TypeConverter { 
 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) { 
                // return "(Bindings)";
                // return an empty string, since we don't want a meaningless
                // string displayed as the value for the expandable Bindings property
                return ""; 
            }
            return base.ConvertTo(context, culture, value, destinationType); 
        } 

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) { 
            if (value is ControlBindingsCollection) {
                ControlBindingsCollection collection = (ControlBindingsCollection)value;
                IBindableComponent control = collection.BindableComponent;
                Type type = control.GetType(); 

                PropertyDescriptorCollection bindableProperties = TypeDescriptor.GetProperties(control, null); 
                ArrayList props = new ArrayList(); 
                for (int i = 0; i < bindableProperties.Count; i++) {
                    // Create a read only binding if the data source is not one of the values we support. 
                    Binding binding = collection[bindableProperties[i].Name];
                    bool readOnly = !(binding == null || binding.DataSource is IListSource || binding.DataSource is IList || binding.DataSource is Array);
                    DesignBindingPropertyDescriptor property = new DesignBindingPropertyDescriptor(bindableProperties[i], null, readOnly);
                    bool bindable = ((BindableAttribute)bindableProperties[i].Attributes[typeof(BindableAttribute)]).Bindable; 
                    if (bindable || !((DesignBinding)property.GetValue(collection)).IsNull) {
                        props.Add(property); 
                    } 
                }
 
                props.Add(new AdvancedBindingPropertyDescriptor());
                PropertyDescriptor[] propArray = new PropertyDescriptor[props.Count];
                props.CopyTo(propArray,0);
                return new PropertyDescriptorCollection(propArray); 
            }
            return new PropertyDescriptorCollection(new PropertyDescriptor[0]); 
        } 

        public override bool GetPropertiesSupported(ITypeDescriptorContext context) { 
            return true;
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
