//------------------------------------------------------------------------------ 
// <copyright file="BaseAutoFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
    using System.Data; 
    using System.Design;
    using System.Diagnostics;
    using System.Reflection;
    using System.Web.UI; 
    using System.Web.UI.WebControls;
    using System.Web.Util; 
    using System.Globalization; 

    internal class BaseAutoFormat : DesignerAutoFormat { 
        private const char PERSIST_CHAR = '-';
        private const char OM_CHAR = '.';

        private DataRow _schemeData; 

        public BaseAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) { 
            _schemeData = schemeData; 
        }
 
        public override void Apply(Control control) {
            foreach (DataColumn column in _schemeData.Table.Columns) {
                string propertyName = column.ColumnName;
                // The SchemeName is the name of the AutoFormat Scheme, and should not be applied 
                // to the Control.
                if (String.Equals(propertyName, "SchemeName", StringComparison.Ordinal)) { 
                    continue; 
                }
                else if (propertyName.EndsWith("--ClearDefaults", StringComparison.Ordinal)) { 
                    if (_schemeData[propertyName].ToString().Equals("true", StringComparison.OrdinalIgnoreCase)) {
                        ClearDefaults(control, propertyName.Substring(0, propertyName.Length - 15));
                    }
                } 
                else {
                    SetPropertyValue(control, propertyName, _schemeData[propertyName].ToString()); 
                } 
            }
        } 

        private void ClearDefaults(Control control, string propertyName) {
            InstanceAndPropertyInfo propInstance = GetMemberInfo(control, propertyName);
            if (propInstance.PropertyInfo != null && propInstance.Instance != null) {   // found a public property 
                object property = propInstance.PropertyInfo.GetValue(propInstance.Instance, null);
                Type currentType = property.GetType(); 
                currentType.InvokeMember("ClearDefaults", 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase,
                    null, 
                    property,
                    new object[] {},
                    System.Globalization.CultureInfo.InvariantCulture);
            } 
        }
 
        // 
        private static InstanceAndPropertyInfo GetMemberInfo(Control control, string name) {
 
            Type currentType = control.GetType();
            PropertyInfo propInfo = null;
            object instance = control;
            object nextInstance = control; 

            string mappedName = name.Replace(PERSIST_CHAR,OM_CHAR); 
 
            int startIndex = 0;
            while (startIndex < mappedName.Length) {   // parse thru dots of object model to locate PropertyInfo 
                string propName;
                int index = mappedName.IndexOf(OM_CHAR, startIndex);

                if (index < 0) { 
                    propName = mappedName.Substring(startIndex);
                    startIndex = mappedName.Length; 
                } 
                else {
                    propName = mappedName.Substring(startIndex, index - startIndex); 
                    startIndex = index + 1;
                }

                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase; 

                try { 
                    propInfo = currentType.GetProperty(propName, flags); 
                }
                catch (AmbiguousMatchException) { 
                    flags |= BindingFlags.DeclaredOnly;
                    propInfo = currentType.GetProperty(propName, flags);
                }
 
                if (propInfo != null) {   // found a public property
                    currentType = propInfo.PropertyType; 
                    if (nextInstance != null) { 
                        instance = nextInstance;
                        nextInstance = propInfo.GetValue(instance, null); 
                    }
                }
            }
            return new InstanceAndPropertyInfo(instance, propInfo); 
        }
 
        protected void SetPropertyValue(Control control, string propertyName, string propertyValue) { 
            object typedPropertyValue = null;
 
            // Find the right property
            InstanceAndPropertyInfo iapi = GetMemberInfo(control, propertyName);
            PropertyInfo pi = iapi.PropertyInfo;
            Debug.Assert(pi != null, String.Format(CultureInfo.CurrentCulture, 
                "Property '{0}' does not exist on control of type '{1}'", propertyName, control.GetType()));
 
            // Find a type converter 
            TypeConverter converter = null;
            // See if the property itself has a type converter associated with it 
            TypeConverterAttribute attr = Attribute.GetCustomAttribute(pi, typeof(TypeConverterAttribute), true) as TypeConverterAttribute;
            if (attr != null) {
                Type converterType = Type.GetType(attr.ConverterTypeName, false);
                if (converterType != null) { 
                    converter = (TypeConverter)(Activator.CreateInstance(converterType));
                } 
            } 
            if (converter != null && converter.CanConvertFrom(typeof(string))) {
                typedPropertyValue = converter.ConvertFromInvariantString(propertyValue); 
            }
            else {
                // Then see if there is a converter from string on the type itself
                converter = TypeDescriptor.GetConverter(pi.PropertyType); 
                if (converter != null && converter.CanConvertFrom(typeof(string))) {
                    typedPropertyValue = converter.ConvertFromInvariantString(propertyValue); 
                } 
            }
            pi.SetValue(iapi.Instance, typedPropertyValue, null); 
        }

        private struct InstanceAndPropertyInfo {
            public object Instance; 
            public PropertyInfo PropertyInfo;
            public InstanceAndPropertyInfo(object instance, PropertyInfo propertyInfo) { 
                Instance = instance; 
                PropertyInfo = propertyInfo;
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BaseAutoFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
    using System.Data; 
    using System.Design;
    using System.Diagnostics;
    using System.Reflection;
    using System.Web.UI; 
    using System.Web.UI.WebControls;
    using System.Web.Util; 
    using System.Globalization; 

    internal class BaseAutoFormat : DesignerAutoFormat { 
        private const char PERSIST_CHAR = '-';
        private const char OM_CHAR = '.';

        private DataRow _schemeData; 

        public BaseAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) { 
            _schemeData = schemeData; 
        }
 
        public override void Apply(Control control) {
            foreach (DataColumn column in _schemeData.Table.Columns) {
                string propertyName = column.ColumnName;
                // The SchemeName is the name of the AutoFormat Scheme, and should not be applied 
                // to the Control.
                if (String.Equals(propertyName, "SchemeName", StringComparison.Ordinal)) { 
                    continue; 
                }
                else if (propertyName.EndsWith("--ClearDefaults", StringComparison.Ordinal)) { 
                    if (_schemeData[propertyName].ToString().Equals("true", StringComparison.OrdinalIgnoreCase)) {
                        ClearDefaults(control, propertyName.Substring(0, propertyName.Length - 15));
                    }
                } 
                else {
                    SetPropertyValue(control, propertyName, _schemeData[propertyName].ToString()); 
                } 
            }
        } 

        private void ClearDefaults(Control control, string propertyName) {
            InstanceAndPropertyInfo propInstance = GetMemberInfo(control, propertyName);
            if (propInstance.PropertyInfo != null && propInstance.Instance != null) {   // found a public property 
                object property = propInstance.PropertyInfo.GetValue(propInstance.Instance, null);
                Type currentType = property.GetType(); 
                currentType.InvokeMember("ClearDefaults", 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase,
                    null, 
                    property,
                    new object[] {},
                    System.Globalization.CultureInfo.InvariantCulture);
            } 
        }
 
        // 
        private static InstanceAndPropertyInfo GetMemberInfo(Control control, string name) {
 
            Type currentType = control.GetType();
            PropertyInfo propInfo = null;
            object instance = control;
            object nextInstance = control; 

            string mappedName = name.Replace(PERSIST_CHAR,OM_CHAR); 
 
            int startIndex = 0;
            while (startIndex < mappedName.Length) {   // parse thru dots of object model to locate PropertyInfo 
                string propName;
                int index = mappedName.IndexOf(OM_CHAR, startIndex);

                if (index < 0) { 
                    propName = mappedName.Substring(startIndex);
                    startIndex = mappedName.Length; 
                } 
                else {
                    propName = mappedName.Substring(startIndex, index - startIndex); 
                    startIndex = index + 1;
                }

                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase; 

                try { 
                    propInfo = currentType.GetProperty(propName, flags); 
                }
                catch (AmbiguousMatchException) { 
                    flags |= BindingFlags.DeclaredOnly;
                    propInfo = currentType.GetProperty(propName, flags);
                }
 
                if (propInfo != null) {   // found a public property
                    currentType = propInfo.PropertyType; 
                    if (nextInstance != null) { 
                        instance = nextInstance;
                        nextInstance = propInfo.GetValue(instance, null); 
                    }
                }
            }
            return new InstanceAndPropertyInfo(instance, propInfo); 
        }
 
        protected void SetPropertyValue(Control control, string propertyName, string propertyValue) { 
            object typedPropertyValue = null;
 
            // Find the right property
            InstanceAndPropertyInfo iapi = GetMemberInfo(control, propertyName);
            PropertyInfo pi = iapi.PropertyInfo;
            Debug.Assert(pi != null, String.Format(CultureInfo.CurrentCulture, 
                "Property '{0}' does not exist on control of type '{1}'", propertyName, control.GetType()));
 
            // Find a type converter 
            TypeConverter converter = null;
            // See if the property itself has a type converter associated with it 
            TypeConverterAttribute attr = Attribute.GetCustomAttribute(pi, typeof(TypeConverterAttribute), true) as TypeConverterAttribute;
            if (attr != null) {
                Type converterType = Type.GetType(attr.ConverterTypeName, false);
                if (converterType != null) { 
                    converter = (TypeConverter)(Activator.CreateInstance(converterType));
                } 
            } 
            if (converter != null && converter.CanConvertFrom(typeof(string))) {
                typedPropertyValue = converter.ConvertFromInvariantString(propertyValue); 
            }
            else {
                // Then see if there is a converter from string on the type itself
                converter = TypeDescriptor.GetConverter(pi.PropertyType); 
                if (converter != null && converter.CanConvertFrom(typeof(string))) {
                    typedPropertyValue = converter.ConvertFromInvariantString(propertyValue); 
                } 
            }
            pi.SetValue(iapi.Instance, typedPropertyValue, null); 
        }

        private struct InstanceAndPropertyInfo {
            public object Instance; 
            public PropertyInfo PropertyInfo;
            public InstanceAndPropertyInfo(object instance, PropertyInfo propertyInfo) { 
                Instance = instance; 
                PropertyInfo = propertyInfo;
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
