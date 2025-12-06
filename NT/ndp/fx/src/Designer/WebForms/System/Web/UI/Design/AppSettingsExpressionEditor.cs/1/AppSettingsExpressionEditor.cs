//------------------------------------------------------------------------------ 
// <copyright file="AppSettingsExpressionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Configuration;
    using System.Design;
    using System.Globalization; 

    /// <include file='doc\AppSettingsExpressionEditor.uex' path='docs/doc[@for="AppSettingsExpressionEditor"]/*' /> 
    public class AppSettingsExpressionEditor : ExpressionEditor { 

        // Get the collection of appSettings from config 
        private KeyValueConfigurationCollection GetAppSettings(IServiceProvider serviceProvider){
            if (serviceProvider != null) {
                IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication));
                if (webApp != null) { 
                    Configuration config = webApp.OpenWebConfiguration(true);
                    if (config != null) { 
                        AppSettingsSection settingsSection = config.AppSettings; 
                        if (settingsSection!= null) {
                            return settingsSection.Settings; 
                        }
                    }
                }
            } 

            return null; 
        } 

        public override ExpressionEditorSheet GetExpressionEditorSheet(string expression, IServiceProvider serviceProvider) { 
            return new AppSettingsExpressionEditorSheet(expression, this, serviceProvider);
        }

        /// <include file='doc\AppSettingsExpressionEditor.uex' path='docs/doc[@for="AppSettingsExpressionEditor.EvaluateExpression"]/*' /> 
        public override object EvaluateExpression(string expression, object parseTimeData, Type propertyType, IServiceProvider serviceProvider) {
            KeyValueConfigurationCollection settings = GetAppSettings(serviceProvider); 
            if (settings != null) { 
                KeyValueConfigurationElement element = settings[expression];
                if (element != null) { 
                    return element.Value;
                }
            }
 
            return null;
        } 
 
        private class AppSettingsExpressionEditorSheet : ExpressionEditorSheet {
            private AppSettingsExpressionEditor _owner; 
            private string _appSetting;

            public AppSettingsExpressionEditorSheet(string expression, AppSettingsExpressionEditor owner, IServiceProvider serviceProvider) : base(serviceProvider) {
                _owner = owner; 
                _appSetting = expression;
            } 
 
            [DefaultValue("")]
            [SRDescription(SR.AppSettingExpressionEditor_AppSetting)] 
            [TypeConverter(typeof(AppSettingsTypeConverter))]
            public string AppSetting {
                get {
                    return _appSetting; 
                }
                set { 
                    _appSetting = value; 
                }
            } 

            public override bool IsValid {
                get {
                    return !String.IsNullOrEmpty(AppSetting); 
                }
            } 
 
            public override string GetExpression() {
                return _appSetting; 
            }

            private class AppSettingsTypeConverter : TypeConverter {
                private static readonly string NoAppSetting = "(None)"; 

                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
                    if (sourceType == typeof(string)) { 
                        return true;
                    } 
                    return base.CanConvertFrom(context, sourceType);
                }

                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) { 
                    if (value is string) {
                        if (String.Equals((string)value, NoAppSetting, StringComparison.OrdinalIgnoreCase)) { 
                            return String.Empty; 
                        }
 
                        return value;
                    }
                    return base.ConvertFrom(context, culture, value);
                } 

                public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) { 
                    if (destType == typeof(string)) { 
                        return true;
                    } 
                    return base.CanConvertTo(context, destType);
                }

                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
                    if (value is string) {
                        if (((string)value).Length == 0) { 
                            return NoAppSetting; 
                        }
 
                        return value;
                    }
                    return base.ConvertTo(context, culture, value, destinationType);
                } 

                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
                    return false; 
                }
 
                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
                    if (context != null) {
                        AppSettingsExpressionEditorSheet sheet = (AppSettingsExpressionEditorSheet)context.Instance;
                        AppSettingsExpressionEditor asee = sheet._owner; 
                        KeyValueConfigurationCollection appSettings = asee.GetAppSettings(sheet.ServiceProvider);
                        if (appSettings != null) { 
                            return (appSettings.Count > 0); 
                        }
                    } 

                    return base.GetStandardValuesSupported(context);
                }
 
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    if (context != null) { 
                        AppSettingsExpressionEditorSheet sheet = (AppSettingsExpressionEditorSheet)context.Instance; 
                        AppSettingsExpressionEditor asee = sheet._owner;
                        KeyValueConfigurationCollection appSettings = asee.GetAppSettings(sheet.ServiceProvider); 
                        if (appSettings != null) {
                            ArrayList valueList = new ArrayList(appSettings.AllKeys);
                            valueList.Sort();
                            valueList.Add(String.Empty); 
                            return new StandardValuesCollection(valueList);
                        } 
                    } 

                    return base.GetStandardValues(context); 
                }
            }

        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="AppSettingsExpressionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Configuration;
    using System.Design;
    using System.Globalization; 

    /// <include file='doc\AppSettingsExpressionEditor.uex' path='docs/doc[@for="AppSettingsExpressionEditor"]/*' /> 
    public class AppSettingsExpressionEditor : ExpressionEditor { 

        // Get the collection of appSettings from config 
        private KeyValueConfigurationCollection GetAppSettings(IServiceProvider serviceProvider){
            if (serviceProvider != null) {
                IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication));
                if (webApp != null) { 
                    Configuration config = webApp.OpenWebConfiguration(true);
                    if (config != null) { 
                        AppSettingsSection settingsSection = config.AppSettings; 
                        if (settingsSection!= null) {
                            return settingsSection.Settings; 
                        }
                    }
                }
            } 

            return null; 
        } 

        public override ExpressionEditorSheet GetExpressionEditorSheet(string expression, IServiceProvider serviceProvider) { 
            return new AppSettingsExpressionEditorSheet(expression, this, serviceProvider);
        }

        /// <include file='doc\AppSettingsExpressionEditor.uex' path='docs/doc[@for="AppSettingsExpressionEditor.EvaluateExpression"]/*' /> 
        public override object EvaluateExpression(string expression, object parseTimeData, Type propertyType, IServiceProvider serviceProvider) {
            KeyValueConfigurationCollection settings = GetAppSettings(serviceProvider); 
            if (settings != null) { 
                KeyValueConfigurationElement element = settings[expression];
                if (element != null) { 
                    return element.Value;
                }
            }
 
            return null;
        } 
 
        private class AppSettingsExpressionEditorSheet : ExpressionEditorSheet {
            private AppSettingsExpressionEditor _owner; 
            private string _appSetting;

            public AppSettingsExpressionEditorSheet(string expression, AppSettingsExpressionEditor owner, IServiceProvider serviceProvider) : base(serviceProvider) {
                _owner = owner; 
                _appSetting = expression;
            } 
 
            [DefaultValue("")]
            [SRDescription(SR.AppSettingExpressionEditor_AppSetting)] 
            [TypeConverter(typeof(AppSettingsTypeConverter))]
            public string AppSetting {
                get {
                    return _appSetting; 
                }
                set { 
                    _appSetting = value; 
                }
            } 

            public override bool IsValid {
                get {
                    return !String.IsNullOrEmpty(AppSetting); 
                }
            } 
 
            public override string GetExpression() {
                return _appSetting; 
            }

            private class AppSettingsTypeConverter : TypeConverter {
                private static readonly string NoAppSetting = "(None)"; 

                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
                    if (sourceType == typeof(string)) { 
                        return true;
                    } 
                    return base.CanConvertFrom(context, sourceType);
                }

                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) { 
                    if (value is string) {
                        if (String.Equals((string)value, NoAppSetting, StringComparison.OrdinalIgnoreCase)) { 
                            return String.Empty; 
                        }
 
                        return value;
                    }
                    return base.ConvertFrom(context, culture, value);
                } 

                public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) { 
                    if (destType == typeof(string)) { 
                        return true;
                    } 
                    return base.CanConvertTo(context, destType);
                }

                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
                    if (value is string) {
                        if (((string)value).Length == 0) { 
                            return NoAppSetting; 
                        }
 
                        return value;
                    }
                    return base.ConvertTo(context, culture, value, destinationType);
                } 

                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
                    return false; 
                }
 
                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
                    if (context != null) {
                        AppSettingsExpressionEditorSheet sheet = (AppSettingsExpressionEditorSheet)context.Instance;
                        AppSettingsExpressionEditor asee = sheet._owner; 
                        KeyValueConfigurationCollection appSettings = asee.GetAppSettings(sheet.ServiceProvider);
                        if (appSettings != null) { 
                            return (appSettings.Count > 0); 
                        }
                    } 

                    return base.GetStandardValuesSupported(context);
                }
 
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    if (context != null) { 
                        AppSettingsExpressionEditorSheet sheet = (AppSettingsExpressionEditorSheet)context.Instance; 
                        AppSettingsExpressionEditor asee = sheet._owner;
                        KeyValueConfigurationCollection appSettings = asee.GetAppSettings(sheet.ServiceProvider); 
                        if (appSettings != null) {
                            ArrayList valueList = new ArrayList(appSettings.AllKeys);
                            valueList.Sort();
                            valueList.Add(String.Empty); 
                            return new StandardValuesCollection(valueList);
                        } 
                    } 

                    return base.GetStandardValues(context); 
                }
            }

        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
