//------------------------------------------------------------------------------ 
// <copyright file="ConnectionStringsExpressionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Configuration;
    using System.Design;
    using System.Globalization;
 
    /// <include file='doc\ConnectionStringsExpressionEditor.uex' path='docs/doc[@for="ConnectionStringsExpressionEditor"]/*' />
    public class ConnectionStringsExpressionEditor : ExpressionEditor { 
 
        // Gets the collection of connection strings from config
        private ConnectionStringSettingsCollection GetConnectionStringSettingsCollection(IServiceProvider serviceProvider) { 
            if (serviceProvider != null) {
                IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication));
                if (webApp != null) {
                    Configuration config = webApp.OpenWebConfiguration(true); 
                    if (config != null) {
                        ConnectionStringsSection connSection = (ConnectionStringsSection)config.GetSection("connectionStrings"); 
                        if (connSection != null) { 
                            return connSection.ConnectionStrings;
                        } 
                    }
                }
            }
 
            return null;
        } 
 
        public override ExpressionEditorSheet GetExpressionEditorSheet(string expression, IServiceProvider serviceProvider) {
            return new ConnectionStringsExpressionEditorSheet(expression, this, serviceProvider); 
        }

        /// <include file='doc\ConnectionStringsExpressionEditor.uex' path='docs/doc[@for="ConnectionStringsExpressionEditor.EvaluateExpression"]/*' />
        public override object EvaluateExpression(string expression, object parseTimeData, Type propertyType, IServiceProvider serviceProvider) { 
            Pair p = (Pair)parseTimeData;
            string name = (string)p.First; 
            bool isConnectionString = (bool)p.Second; 

            ConnectionStringSettingsCollection connections = GetConnectionStringSettingsCollection(serviceProvider); 
            ConnectionStringSettings setting = null;
            foreach (ConnectionStringSettings item in connections) {
                if (String.Equals(name, item.Name, StringComparison.OrdinalIgnoreCase)) {
                    setting = item; 
                    break;
                } 
            } 

            if (setting != null) { 
                if (isConnectionString) {
                    return setting.ConnectionString;
                }
                else { 
                    return setting.ProviderName;
                } 
            } 

            // If we couldn't find a connection, just return null 
            return null;
        }

        private static string ParseExpression(string expression, out bool isConnectionString) { 
            // Copied from ConnectionStringsExpressionBuilder.ParseExpression()
            const string connectionStringSuffix = ".connectionstring"; 
            const string providerNameSuffix = ".providername"; 
            isConnectionString = true;
            expression = expression.Trim(); 
            if (expression.EndsWith(connectionStringSuffix, StringComparison.OrdinalIgnoreCase)) {
                return expression.Substring(0, expression.Length - connectionStringSuffix.Length);
            }
            else { 
                if (expression.EndsWith(providerNameSuffix, StringComparison.OrdinalIgnoreCase)) {
                    isConnectionString = false; 
                    return expression.Substring(0, expression.Length - providerNameSuffix.Length); 
                }
                else { 
                    return expression;
                }
            }
        } 

        private class ConnectionStringsExpressionEditorSheet : ExpressionEditorSheet { 
            private string _connectionName; 
            private ConnectionType _connectionType;
            private ConnectionStringsExpressionEditor _owner; 

            public ConnectionStringsExpressionEditorSheet(string expression, ConnectionStringsExpressionEditor owner, IServiceProvider serviceProvider) : base(serviceProvider) {
                _owner = owner;
 
                bool isConnectionString;
                _connectionName = ParseExpression(expression, out isConnectionString); 
 
                _connectionType = (isConnectionString ? ConnectionType.ConnectionString : ConnectionType.ProviderName);
            } 

            [DefaultValue("")]
            [SRDescription(SR.ConnectionStringsExpressionEditor_ConnectionName)]
            [TypeConverter(typeof(ConnectionStringsTypeConverter))] 
            public string ConnectionName {
                get { 
                    return _connectionName; 
                }
                set { 
                    _connectionName = value;
                }
            }
 
            public override bool IsValid {
                get { 
                    return !String.IsNullOrEmpty(ConnectionName); 
                }
            } 

            [DefaultValue(ConnectionType.ConnectionString)]
            [SRDescription(SR.ConnectionStringsExpressionEditor_ConnectionType)]
            public ConnectionType Type { 
                get {
                    return _connectionType; 
                } 
                set {
                    _connectionType = value; 
                }
            }

            public override string GetExpression() { 
                if (String.IsNullOrEmpty(_connectionName)) {
                    return String.Empty; 
                } 

                string expression = _connectionName; 
                if (Type == ConnectionType.ProviderName) {
                    expression += ".ProviderName";
                }
 
                return expression;
            } 
 
            public enum ConnectionType {
                ConnectionString = 0, 
                ProviderName = 1
            }

            private class ConnectionStringsTypeConverter : TypeConverter { 
                private static readonly string NoConnectionName = "(None)";
 
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
                    if (sourceType == typeof(string)) {
                        return true; 
                    }
                    return base.CanConvertFrom(context, sourceType);
                }
 
                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
                    if (value is string) { 
                        if (String.Equals((string)value, NoConnectionName, StringComparison.OrdinalIgnoreCase)) { 
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
                            return NoConnectionName;
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
                        ConnectionStringsExpressionEditorSheet sheet = (ConnectionStringsExpressionEditorSheet)context.Instance; 
                        ConnectionStringsExpressionEditor csee = sheet._owner;
                        ConnectionStringSettingsCollection connectionsColl = csee.GetConnectionStringSettingsCollection(sheet.ServiceProvider); 
                        if (connectionsColl != null) { 
                            return (connectionsColl.Count > 0);
                        } 
                    }

                    return base.GetStandardValuesSupported(context);
                } 

                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
                    if (context != null) { 
                        ConnectionStringsExpressionEditorSheet sheet = (ConnectionStringsExpressionEditorSheet)context.Instance;
                        ConnectionStringsExpressionEditor csee = sheet._owner; 
                        ConnectionStringSettingsCollection connectionsColl = csee.GetConnectionStringSettingsCollection(sheet.ServiceProvider);
                        if (connectionsColl != null) {
                            ArrayList valueList = new ArrayList();
                            foreach (ConnectionStringSettings setting in connectionsColl) { 
                                valueList.Add(setting.Name);
                            } 
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
// <copyright file="ConnectionStringsExpressionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Configuration;
    using System.Design;
    using System.Globalization;
 
    /// <include file='doc\ConnectionStringsExpressionEditor.uex' path='docs/doc[@for="ConnectionStringsExpressionEditor"]/*' />
    public class ConnectionStringsExpressionEditor : ExpressionEditor { 
 
        // Gets the collection of connection strings from config
        private ConnectionStringSettingsCollection GetConnectionStringSettingsCollection(IServiceProvider serviceProvider) { 
            if (serviceProvider != null) {
                IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication));
                if (webApp != null) {
                    Configuration config = webApp.OpenWebConfiguration(true); 
                    if (config != null) {
                        ConnectionStringsSection connSection = (ConnectionStringsSection)config.GetSection("connectionStrings"); 
                        if (connSection != null) { 
                            return connSection.ConnectionStrings;
                        } 
                    }
                }
            }
 
            return null;
        } 
 
        public override ExpressionEditorSheet GetExpressionEditorSheet(string expression, IServiceProvider serviceProvider) {
            return new ConnectionStringsExpressionEditorSheet(expression, this, serviceProvider); 
        }

        /// <include file='doc\ConnectionStringsExpressionEditor.uex' path='docs/doc[@for="ConnectionStringsExpressionEditor.EvaluateExpression"]/*' />
        public override object EvaluateExpression(string expression, object parseTimeData, Type propertyType, IServiceProvider serviceProvider) { 
            Pair p = (Pair)parseTimeData;
            string name = (string)p.First; 
            bool isConnectionString = (bool)p.Second; 

            ConnectionStringSettingsCollection connections = GetConnectionStringSettingsCollection(serviceProvider); 
            ConnectionStringSettings setting = null;
            foreach (ConnectionStringSettings item in connections) {
                if (String.Equals(name, item.Name, StringComparison.OrdinalIgnoreCase)) {
                    setting = item; 
                    break;
                } 
            } 

            if (setting != null) { 
                if (isConnectionString) {
                    return setting.ConnectionString;
                }
                else { 
                    return setting.ProviderName;
                } 
            } 

            // If we couldn't find a connection, just return null 
            return null;
        }

        private static string ParseExpression(string expression, out bool isConnectionString) { 
            // Copied from ConnectionStringsExpressionBuilder.ParseExpression()
            const string connectionStringSuffix = ".connectionstring"; 
            const string providerNameSuffix = ".providername"; 
            isConnectionString = true;
            expression = expression.Trim(); 
            if (expression.EndsWith(connectionStringSuffix, StringComparison.OrdinalIgnoreCase)) {
                return expression.Substring(0, expression.Length - connectionStringSuffix.Length);
            }
            else { 
                if (expression.EndsWith(providerNameSuffix, StringComparison.OrdinalIgnoreCase)) {
                    isConnectionString = false; 
                    return expression.Substring(0, expression.Length - providerNameSuffix.Length); 
                }
                else { 
                    return expression;
                }
            }
        } 

        private class ConnectionStringsExpressionEditorSheet : ExpressionEditorSheet { 
            private string _connectionName; 
            private ConnectionType _connectionType;
            private ConnectionStringsExpressionEditor _owner; 

            public ConnectionStringsExpressionEditorSheet(string expression, ConnectionStringsExpressionEditor owner, IServiceProvider serviceProvider) : base(serviceProvider) {
                _owner = owner;
 
                bool isConnectionString;
                _connectionName = ParseExpression(expression, out isConnectionString); 
 
                _connectionType = (isConnectionString ? ConnectionType.ConnectionString : ConnectionType.ProviderName);
            } 

            [DefaultValue("")]
            [SRDescription(SR.ConnectionStringsExpressionEditor_ConnectionName)]
            [TypeConverter(typeof(ConnectionStringsTypeConverter))] 
            public string ConnectionName {
                get { 
                    return _connectionName; 
                }
                set { 
                    _connectionName = value;
                }
            }
 
            public override bool IsValid {
                get { 
                    return !String.IsNullOrEmpty(ConnectionName); 
                }
            } 

            [DefaultValue(ConnectionType.ConnectionString)]
            [SRDescription(SR.ConnectionStringsExpressionEditor_ConnectionType)]
            public ConnectionType Type { 
                get {
                    return _connectionType; 
                } 
                set {
                    _connectionType = value; 
                }
            }

            public override string GetExpression() { 
                if (String.IsNullOrEmpty(_connectionName)) {
                    return String.Empty; 
                } 

                string expression = _connectionName; 
                if (Type == ConnectionType.ProviderName) {
                    expression += ".ProviderName";
                }
 
                return expression;
            } 
 
            public enum ConnectionType {
                ConnectionString = 0, 
                ProviderName = 1
            }

            private class ConnectionStringsTypeConverter : TypeConverter { 
                private static readonly string NoConnectionName = "(None)";
 
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
                    if (sourceType == typeof(string)) {
                        return true; 
                    }
                    return base.CanConvertFrom(context, sourceType);
                }
 
                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
                    if (value is string) { 
                        if (String.Equals((string)value, NoConnectionName, StringComparison.OrdinalIgnoreCase)) { 
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
                            return NoConnectionName;
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
                        ConnectionStringsExpressionEditorSheet sheet = (ConnectionStringsExpressionEditorSheet)context.Instance; 
                        ConnectionStringsExpressionEditor csee = sheet._owner;
                        ConnectionStringSettingsCollection connectionsColl = csee.GetConnectionStringSettingsCollection(sheet.ServiceProvider); 
                        if (connectionsColl != null) { 
                            return (connectionsColl.Count > 0);
                        } 
                    }

                    return base.GetStandardValuesSupported(context);
                } 

                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
                    if (context != null) { 
                        ConnectionStringsExpressionEditorSheet sheet = (ConnectionStringsExpressionEditorSheet)context.Instance;
                        ConnectionStringsExpressionEditor csee = sheet._owner; 
                        ConnectionStringSettingsCollection connectionsColl = csee.GetConnectionStringSettingsCollection(sheet.ServiceProvider);
                        if (connectionsColl != null) {
                            ArrayList valueList = new ArrayList();
                            foreach (ConnectionStringSettings setting in connectionsColl) { 
                                valueList.Add(setting.Name);
                            } 
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
