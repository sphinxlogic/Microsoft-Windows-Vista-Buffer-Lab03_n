//------------------------------------------------------------------------------ 
// <copyright file="ConfigUtil.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
    using System; 
    using System.Threading;
    using System.Configuration; 
    using System.Xml;
    using System.Web.Compilation;
    using System.Web.Util;
 
    internal class ConfigUtil {
        private ConfigUtil() { 
        } 

        internal static void CheckBaseType(Type expectedBaseType, Type userBaseType, string propertyName, ConfigurationElement configElement) { 
            // Make sure the base type is valid
            if (!expectedBaseType.IsAssignableFrom(userBaseType)) {
                throw new ConfigurationErrorsException(
                    SR.GetString(SR.Invalid_type_to_inherit_from, 
                        userBaseType.FullName,
                        expectedBaseType.FullName), configElement.ElementInformation.Properties[propertyName].Source, 
                        configElement.ElementInformation.Properties[propertyName].LineNumber); 
            }
        } 

        internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement,
            XmlNode node, bool checkAptcaBit, bool ignoreCase) {
 
            // We should get either a propertyName/configElement or node, but not both.
            // They are used only for error reporting. 
            Debug.Assert((propertyName != null) != (node != null)); 

            Type val; 
            try {
                val = BuildManager.GetType(typeName, true /*throwOnError*/, ignoreCase);
            }
            catch (Exception e) { 
                if (e is ThreadAbortException || e is StackOverflowException || e is OutOfMemoryException) {
                    throw; 
                } 

                if (node != null) { 
                    throw new ConfigurationErrorsException(e.Message, e, node);
                }
                else {
                    if (configElement != null) { 
                        throw new ConfigurationErrorsException(e.Message, e,
                                                               configElement.ElementInformation.Properties[propertyName].Source, 
                                                               configElement.ElementInformation.Properties[propertyName].LineNumber); 
                    }
                    else { 
                        throw new ConfigurationErrorsException(e.Message, e);
                    }
                }
            } 

            // If we're not in full trust, only allow types that have the APTCA bit (ASURT 139687), 
            // unless the checkAptcaBit flag is false 
            if (checkAptcaBit) {
                if (node != null) { 
                    HttpRuntime.FailIfNoAPTCABit(val, node);
                }
                else {
                    HttpRuntime.FailIfNoAPTCABit(val, 
                        configElement != null ? configElement.ElementInformation : null,
                        propertyName); 
                } 
            }
 
            return val;
        }

        internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement) { 
            return GetType(typeName, propertyName, configElement, true /*checkAptcaBit*/);
        } 
 
        internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement, bool checkAptcaBit) {
            return GetType(typeName, propertyName, configElement, checkAptcaBit, false); 
        }

        internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement, bool checkAptcaBit, bool ignoreCase) {
            return GetType(typeName, propertyName, configElement, null /*node*/, checkAptcaBit, ignoreCase); 
        }
 
        internal static Type GetType(string typeName, XmlNode node) { 
            return GetType(typeName, node, false /*ignoreCase*/);
        } 

        internal static Type GetType(string typeName, XmlNode node, bool ignoreCase) {
            return GetType(typeName, null, null, node, true /*checkAptcaBit*/, ignoreCase);
        } 

        internal static void CheckAssignableType(Type baseType, Type type, ConfigurationElement configElement, string propertyName) { 
            if (!baseType.IsAssignableFrom(type)) { 
                throw new ConfigurationErrorsException(
                                SR.GetString(SR.Type_doesnt_inherit_from_type, type.FullName, baseType.FullName), 
                                configElement.ElementInformation.Properties[propertyName].Source, configElement.ElementInformation.Properties[propertyName].LineNumber);
            }
        }
 
        internal static void CheckAssignableType(Type baseType, Type baseType2, Type type, ConfigurationElement configElement, string propertyName) {
            if (!baseType.IsAssignableFrom(type) && !baseType2.IsAssignableFrom(type)) { 
                throw new ConfigurationErrorsException( 
                                SR.GetString(SR.Type_doesnt_inherit_from_type, type.FullName, baseType.FullName),
                                configElement.ElementInformation.Properties[propertyName].Source, 
                                configElement.ElementInformation.Properties[propertyName].LineNumber);
            }
        }
 
        internal static bool IsTypeHandlerOrFactory(Type t) {
            return typeof(IHttpHandler).IsAssignableFrom(t) 
                || typeof(IHttpHandlerFactory).IsAssignableFrom(t); 
        }
 
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="ConfigUtil.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
    using System; 
    using System.Threading;
    using System.Configuration; 
    using System.Xml;
    using System.Web.Compilation;
    using System.Web.Util;
 
    internal class ConfigUtil {
        private ConfigUtil() { 
        } 

        internal static void CheckBaseType(Type expectedBaseType, Type userBaseType, string propertyName, ConfigurationElement configElement) { 
            // Make sure the base type is valid
            if (!expectedBaseType.IsAssignableFrom(userBaseType)) {
                throw new ConfigurationErrorsException(
                    SR.GetString(SR.Invalid_type_to_inherit_from, 
                        userBaseType.FullName,
                        expectedBaseType.FullName), configElement.ElementInformation.Properties[propertyName].Source, 
                        configElement.ElementInformation.Properties[propertyName].LineNumber); 
            }
        } 

        internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement,
            XmlNode node, bool checkAptcaBit, bool ignoreCase) {
 
            // We should get either a propertyName/configElement or node, but not both.
            // They are used only for error reporting. 
            Debug.Assert((propertyName != null) != (node != null)); 

            Type val; 
            try {
                val = BuildManager.GetType(typeName, true /*throwOnError*/, ignoreCase);
            }
            catch (Exception e) { 
                if (e is ThreadAbortException || e is StackOverflowException || e is OutOfMemoryException) {
                    throw; 
                } 

                if (node != null) { 
                    throw new ConfigurationErrorsException(e.Message, e, node);
                }
                else {
                    if (configElement != null) { 
                        throw new ConfigurationErrorsException(e.Message, e,
                                                               configElement.ElementInformation.Properties[propertyName].Source, 
                                                               configElement.ElementInformation.Properties[propertyName].LineNumber); 
                    }
                    else { 
                        throw new ConfigurationErrorsException(e.Message, e);
                    }
                }
            } 

            // If we're not in full trust, only allow types that have the APTCA bit (ASURT 139687), 
            // unless the checkAptcaBit flag is false 
            if (checkAptcaBit) {
                if (node != null) { 
                    HttpRuntime.FailIfNoAPTCABit(val, node);
                }
                else {
                    HttpRuntime.FailIfNoAPTCABit(val, 
                        configElement != null ? configElement.ElementInformation : null,
                        propertyName); 
                } 
            }
 
            return val;
        }

        internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement) { 
            return GetType(typeName, propertyName, configElement, true /*checkAptcaBit*/);
        } 
 
        internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement, bool checkAptcaBit) {
            return GetType(typeName, propertyName, configElement, checkAptcaBit, false); 
        }

        internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement, bool checkAptcaBit, bool ignoreCase) {
            return GetType(typeName, propertyName, configElement, null /*node*/, checkAptcaBit, ignoreCase); 
        }
 
        internal static Type GetType(string typeName, XmlNode node) { 
            return GetType(typeName, node, false /*ignoreCase*/);
        } 

        internal static Type GetType(string typeName, XmlNode node, bool ignoreCase) {
            return GetType(typeName, null, null, node, true /*checkAptcaBit*/, ignoreCase);
        } 

        internal static void CheckAssignableType(Type baseType, Type type, ConfigurationElement configElement, string propertyName) { 
            if (!baseType.IsAssignableFrom(type)) { 
                throw new ConfigurationErrorsException(
                                SR.GetString(SR.Type_doesnt_inherit_from_type, type.FullName, baseType.FullName), 
                                configElement.ElementInformation.Properties[propertyName].Source, configElement.ElementInformation.Properties[propertyName].LineNumber);
            }
        }
 
        internal static void CheckAssignableType(Type baseType, Type baseType2, Type type, ConfigurationElement configElement, string propertyName) {
            if (!baseType.IsAssignableFrom(type) && !baseType2.IsAssignableFrom(type)) { 
                throw new ConfigurationErrorsException( 
                                SR.GetString(SR.Type_doesnt_inherit_from_type, type.FullName, baseType.FullName),
                                configElement.ElementInformation.Properties[propertyName].Source, 
                                configElement.ElementInformation.Properties[propertyName].LineNumber);
            }
        }
 
        internal static bool IsTypeHandlerOrFactory(Type t) {
            return typeof(IHttpHandler).IsAssignableFrom(t) 
                || typeof(IHttpHandlerFactory).IsAssignableFrom(t); 
        }
 
    }
}
