//------------------------------------------------------------------------------ 
// <copyright file="ControlSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.Design {
    using System; 
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO; 
    using System.Reflection;
    using System.Text; 
    using System.Web; 
    using System.Web.UI;
    using System.Web.UI.HtmlControls; 
    using System.Web.UI.WebControls;
    using AttributeCollection = System.Web.UI.AttributeCollection;
    using System.Collections.Generic;
 
    /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer"]/*' />
    internal static class ControlSerializer { 
        private static readonly object licenseManagerLock = new object(); 

        private const char PERSIST_CHAR = '-'; 

        private const char OM_CHAR = '.';

        private const char FILTER_SEPARATOR_CHAR = ':'; 

        /// <devdoc> 
        /// Whether a string should be serialized as inner default 
        /// </devdoc>
        private static bool CanSerializeAsInnerDefaultString(string filter, string name, Type type, ObjectPersistData persistData, PersistenceMode mode, DataBindingCollection dataBindings, ExpressionBindingCollection expressions) { 
            if (type == typeof(string)) {
                if (filter.Length == 0) {
                    if ((mode == PersistenceMode.InnerDefaultProperty) || (mode == PersistenceMode.EncodedInnerDefaultProperty)) {
                        if (((dataBindings == null) || (dataBindings[name] == null)) && ((expressions == null) || (expressions[name] == null))) { 
                            if (persistData == null) {
                                return true; 
                            } 

                            ICollection filteredProperties = persistData.GetPropertyAllFilters(name); 

                            if (filteredProperties.Count == 0) {
                                // If there wasn't a previously inner default property, this one
                                // can be serialized as inner default 
                                return true;
                            } 
                            else if (filteredProperties.Count == 1) { 
                                // If there is only one previous inner default property and it's the
                                // default filter and it was specified as an inner default, 
                                // it can be serialized as inner default
                                foreach (PropertyEntry entry in filteredProperties) {
                                    if (entry.Filter.Length == 0) {
                                        if (entry is ComplexPropertyEntry) { 
                                            return true;
                                        } 
                                    } 
                                }
                            } 
                        }
                    }
                }
            } 

            return false; 
        } 

        /// <devdoc> 
        /// Converts a dot-syntax name to a dash-syntax name (Font.Bold -> Font-Bold)
        /// </devdoc>
        private static string ConvertObjectModelToPersistName(string objectModelName) {
            return objectModelName.Replace(OM_CHAR, PERSIST_CHAR); 
        }
 
        /// <devdoc> 
        /// Converts a dash-syntax name to a dot-syntax name (Font-Bold -> Font.Bold)
        /// </devdoc> 
        private static string ConvertPersistToObjectModelName(string persistName) {
            return persistName.Replace(PERSIST_CHAR, OM_CHAR);
        }
 
        /// <devdoc>
        /// Gets an designer expando attributes in the ObjectPersistData with the specified name and filter 
        /// </devdoc> 
        private static IDictionary GetExpandos(string filter, string name, ObjectPersistData persistData) {
            IDictionary expandos = null; 

            if (persistData != null) {
                BuilderPropertyEntry currentFilterEntry = persistData.GetFilteredProperty(filter, name) as BuilderPropertyEntry;
 
                if (currentFilterEntry != null) {
                    ObjectPersistData currentFilterPersistData = currentFilterEntry.Builder.GetObjectPersistData(); 
 
                    expandos = currentFilterPersistData.GetFilteredProperties(ControlBuilder.DesignerFilter);
                } 
            }

            return expandos;
        } 

        /// <devdoc> 
        /// Used by ControlDesigner to get a list of persisted attributes 
        /// so it can write directly to the IControlDesignerTag
        /// </devdoc> 
        internal static ArrayList GetControlPersistedAttributes(Control control, IDesignerHost host) {
            ObjectPersistData persistData = null;
            IControlBuilderAccessor cba = (IControlBuilderAccessor)control;
 
            if (cba.ControlBuilder != null) {
                persistData = cba.ControlBuilder.GetObjectPersistData(); 
            } 

            return SerializeAttributes(control, host, String.Empty, persistData, GetCurrentFilter(host), true); 
        }

        /// <devdoc>
        /// Used by ControlDesigner to get a list of persisted attributes 
        /// so it can write directly to the IControlDesignerTag
        /// </devdoc> 
        internal static ArrayList GetControlPersistedAttribute(Control control, PropertyDescriptor propDesc, IDesignerHost host) { 
            ObjectPersistData persistData = null;
            IControlBuilderAccessor cba = (IControlBuilderAccessor)control; 

            if (cba.ControlBuilder != null) {
                persistData = cba.ControlBuilder.GetObjectPersistData();
            } 

            string prefix = String.Empty; 
            object propValue = control; 
            ArrayList attributes = new ArrayList();
 
            if (propDesc.SerializationVisibility == DesignerSerializationVisibility.Content) {
                propValue = propDesc.GetValue(control);
                prefix = propDesc.Name;
                SerializeAttributesRecursive(propValue, host, prefix, persistData, GetCurrentFilter(host), attributes, null, null, true); 
            }
            else { 
                DataBindingCollection dataBindings = ((IDataBindingsAccessor)control).DataBindings; 
                ExpressionBindingCollection expressions = ((IExpressionsAccessor)control).Expressions;
 
                SerializeAttribute(propValue, propDesc, dataBindings, expressions, host, prefix, persistData, GetCurrentFilter(host), attributes, true);
            }

            return attributes; 
        }
 
        /// <devdoc> 
        /// Returns the default value for a property given a PropertyDescriptor and ObjectPersistData
        /// Checks the DefaultValueAttribute and String.Empty filter for the default value 
        /// </devdoc>
        private static object GetPropertyDefaultValue(PropertyDescriptor propDesc, string name, ObjectPersistData defaultPropertyEntries, string filter, IDesignerHost host) {
            if ((filter.Length > 0) && (defaultPropertyEntries != null)) {
                string objectModelName = ConvertPersistToObjectModelName(name); 
                IFilterResolutionService filterResolutionService = null;
                ServiceContainer container = new ServiceContainer(); 
 
                if (host != null) {
                    filterResolutionService = (IFilterResolutionService)host.GetService(typeof(IFilterResolutionService)); 
                    if (filterResolutionService != null)
                        container.AddService(typeof(IFilterResolutionService), filterResolutionService);

                    IThemeResolutionService themeResolutionService = (IThemeResolutionService)host.GetService(typeof(IThemeResolutionService)); 

                    if (themeResolutionService != null) 
                        container.AddService(typeof(IThemeResolutionService), themeResolutionService); 
                }
 
                // Get the property entry from the default filter (String.Empty), if there is one
                PropertyEntry entry = null;

#if ORCAS 
                // Find the default filter using the device filter resolution service
                // Just enumerate the properties in order since they should already be in 
                // most-specific to least-specific order 
                if (filterResolutionService != null) {
                    ICollection allProps = defaultPropertyEntries.GetPropertyAllFilters(objectModelName); 

                    foreach (PropertyEntry propertyEntry in allProps) {
                        if (!String.Equals(filterResolutionService.CurrentFilter, propertyEntry.Filter, StringComparison.InvariantCultureIgnoreCase) && filterResolutionService.EvaluateFilter(propertyEntry.Filter)) {
                            entry = propertyEntry; 
                            break;
                        } 
                    } 
                }
                else { 
                    // Just use the default filter if there is no filter resolution service
                    entry = defaultPropertyEntries.GetFilteredProperty(String.Empty, objectModelName);
                }
#else 
                // Just use the default filter if there is no filter resolution service
                entry = defaultPropertyEntries.GetFilteredProperty(String.Empty, objectModelName); 
#endif 

 
                if (entry is SimplePropertyEntry) {
                    return ((SimplePropertyEntry)entry).Value;
                }
                else if (entry is BoundPropertyEntry) { 
                    // Trim these values since we do it in ControlBuilder too
                    string expression = ((BoundPropertyEntry)entry).Expression.Trim(); 
                    string expressionPrefix = ((BoundPropertyEntry)entry).ExpressionPrefix.Trim(); 

                    if (expressionPrefix.Length > 0) { 
                        expression = expressionPrefix + ":" + expression;
                    }

                    return expression; 
                }
                else if (entry is ComplexPropertyEntry) { 
                    ControlBuilder controlBuilder = ((ComplexPropertyEntry)entry).Builder; 

                    Debug.Assert(controlBuilder.ServiceProvider == null); 
                    controlBuilder.SetServiceProvider(container);

                    object o = null;
 
                    try {
                        o = controlBuilder.BuildObject(); 
                    } 
                    finally {
                        controlBuilder.SetServiceProvider(null); 
                    }
                    return o;
                }
                else if (entry == null) { 
                }
                else { 
                    Debug.Fail("Unexpected PropertyEntry type in GetPropertyDefaultValue : " + entry.GetType()); 
                }
            } 

            // If there was no default filter entry, use the default value attriubte
            DefaultValueAttribute defValAttr = (DefaultValueAttribute)propDesc.Attributes[typeof(DefaultValueAttribute)];
 
            if (defValAttr != null) {
                return defValAttr.Value; 
            } 

            return null; 
        }

        /// <devdoc>
        /// Get a string containing the register directives 
        /// </devdoc>
        private static string GetDirectives(IDesignerHost designerHost) { 
            Debug.Assert(designerHost != null); 

            string directives = String.Empty; 
            WebFormsReferenceManager referenceManager = null;

            if (designerHost.RootComponent != null) {
                WebFormsRootDesigner rootDesigner = designerHost.GetDesigner(designerHost.RootComponent) as WebFormsRootDesigner; 

                if (rootDesigner != null) { 
                    referenceManager = rootDesigner.ReferenceManager; 
                }
            } 

            if (referenceManager == null) {
#pragma warning disable 618
                IWebFormReferenceManager oldReferenceManager = (IWebFormReferenceManager)designerHost.GetService(typeof(IWebFormReferenceManager)); 

                if (oldReferenceManager != null) { 
                    directives = oldReferenceManager.GetRegisterDirectives(); 
                }
#pragma warning restore 618 
            }
            else {
                StringBuilder sb = new StringBuilder();
 
                foreach (string s in referenceManager.GetRegisterDirectives()) {
                    sb.Append(s); 
                } 

                directives = sb.ToString(); 
            }

            return directives;
        } 

        private static string GetCurrentFilter(IDesignerHost host) { 
#if ORCAS 
            string filter = String.Empty;
 
            if (host != null) {
                IFilterResolutionService filterResolutionService = (IFilterResolutionService)host.GetService(typeof(IFilterResolutionService));

                if (filterResolutionService != null) { 
                    filter = filterResolutionService.CurrentFilter;
                } 
            } 

            return filter; 
#else
            // Only the default filter is supported
            return String.Empty;
#endif 
        }
 
        /// <devdoc> 
        /// Gets the string to persist for the specified value for the specified property.
        /// 
        /// COMMENTS NOTE:
        /// More details available in VSWhidbey 420273. Theoretically the serializer should
        /// HTML encode all attribute values (except for top-level values, since those are
        /// done by the tool) to ensure valid content in the ASPX. The runtime HTML decodes 
        /// all attributes at parse time. Unfortunately the tool does not decode attribute
        /// values properly, and the fix for the tool is too risky, so we can't change this 
        /// just yet. 
        /// </devdoc>
        private static string GetPersistValue(PropertyDescriptor propDesc, Type propType, object propValue, BindingType bindingType, bool topLevelInDesigner) { 
            string persistValue = String.Empty;

            if (bindingType == BindingType.Data) {
                persistValue = "<%# " + propValue.ToString() + " %>"; 
                // See note above for why this is commented out
                //if (topLevelInDesigner) { 
                //    persistValue = "<%# " + propValue.ToString() + " %>"; 
                //}
                //else { 
                //    persistValue = "<%# " + HttpUtility.HtmlEncode(propValue.ToString()) + " %>";
                //}
            }
            else if (bindingType == BindingType.Expression) { 
                persistValue = "<%$ " + propValue.ToString() + " %>";
                // See note above for why this is commented out 
                //if (topLevelInDesigner) { 
                //    persistValue = "<%$ " + propValue.ToString() + " %>";
                //} 
                //else {
                //    persistValue = "<%$ " + HttpUtility.HtmlEncode(propValue.ToString()) + " %>";
                //}
            } 
            else if (propType.IsEnum) {
                persistValue = Enum.Format(propType, propValue, "G"); 
            } 
            else if (propType == typeof(string)) {
                if (propValue != null) { 
                    persistValue = propValue.ToString();
                    if (!topLevelInDesigner) {
                        persistValue = HttpUtility.HtmlEncode(persistValue);
                    } 
                }
            } 
            else { 
                // Use the TypeConverter to get the string value of the object
                TypeConverter converter = null; 

                if (propDesc != null) {
                    converter = propDesc.Converter;
                } 
                else {
                    converter = TypeDescriptor.GetConverter(propValue); 
                } 

                if (converter != null) { 
                    persistValue = converter.ConvertToInvariantString(null, propValue);
                }
                else {
                    persistValue = propValue.ToString(); 
                }
 
                if (!topLevelInDesigner) { 
                    persistValue = HttpUtility.HtmlEncode(persistValue);
                } 
            }

            return persistValue;
        } 

        /// <devdoc> 
        /// Checks the ShouldSerialize method on the specified object's specified property name 
        /// </devdoc>
        private static bool GetShouldSerializeValue(object obj, string name, out bool useResult) { 
            useResult = false;

            Type objType = obj.GetType();
            BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance; 
            PropertyInfo propInfo = objType.GetProperty(name, flags);
 
            Debug.Assert(propInfo != null, "Couldn't get property '" + name + "' for type '" + objType + "'"); 
            flags |= BindingFlags.NonPublic;
 
            MethodInfo shouldSerializeMethod = propInfo.DeclaringType.GetMethod("ShouldSerialize" + name, flags);

            if (shouldSerializeMethod != null) {
                useResult = true; 

                object o = shouldSerializeMethod.Invoke(obj, new object[0]); 
 
                Debug.Assert((o != null) && (o is bool));
                return (bool)o; 
            }

            return true;
        } 

        /// <devdoc> 
        /// Gets the tag name for the specified type using the IWebFormsReferenceManager 
        /// </devdoc>
        private static string GetTagName(Type type, IDesignerHost host) { 
            string tagName = String.Empty;
            string tagPrefix = String.Empty;
            WebFormsReferenceManager referenceManager = null;
 
            if (host.RootComponent != null) {
                WebFormsRootDesigner rootDesigner = host.GetDesigner(host.RootComponent) as WebFormsRootDesigner; 
 
                if (rootDesigner != null) {
                    referenceManager = rootDesigner.ReferenceManager; 
                }
            }

            if (referenceManager == null) { 
#pragma warning disable 618
                IWebFormReferenceManager oldReferenceManager = (IWebFormReferenceManager)host.GetService(typeof(IWebFormReferenceManager)); 
 
                Debug.Assert(oldReferenceManager != null, "Did not get back IWebFormReferenceManager service from host.");
                if (oldReferenceManager != null) { 
                    tagPrefix = oldReferenceManager.GetTagPrefix(type);
                }
#pragma warning restore 618
            } 
            else {
                tagPrefix = referenceManager.GetTagPrefix(type); 
            } 

            // If there wasn't an existing tag prefix, add a new one to the document 
            if (String.IsNullOrEmpty(tagPrefix)) {
                tagPrefix = referenceManager.RegisterTagPrefix(type);
                Debug.Assert(!String.IsNullOrEmpty(tagPrefix), "Did not expect empty tag prefix from ReferenceManager.RegisterTagPrefix().");
            } 

            if ((tagPrefix != null) && (tagPrefix.Length != 0)) { 
                tagName = tagPrefix + ":" + type.Name; 
            }
 
            if (tagName.Length == 0) {
                tagName = type.FullName;
            }
 
            return tagName;
        } 
 
        internal static Control DeserializeControlInternal(string text, IDesignerHost host, bool applyTheme) {
            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            if ((text == null) || (text.Length == 0)) { 
                throw new ArgumentNullException("text");
            } 
 
            string directives = GetDirectives(host);
 
            if ((directives != null) && (directives.Length > 0)) {
                text = directives + text;
            }
 
            DesignTimeParseData parseData = new DesignTimeParseData(host, text, GetCurrentFilter(host));
 
            parseData.ShouldApplyTheme = applyTheme; 
            parseData.DataBindingHandler = GlobalDataBindingHandler.Handler;
 
            Control parsedControl = null;

            lock (typeof(LicenseManager)) {
                LicenseContext originalContext = LicenseManager.CurrentContext; 

                try { 
                    LicenseManager.CurrentContext = new WebFormsDesigntimeLicenseContext(host); 
                    LicenseManager.LockContext(licenseManagerLock);
                    parsedControl = DesignTimeTemplateParser.ParseControl(parseData); 
                }
                catch (TargetInvocationException e) {
                    Debug.Assert(e.InnerException != null);
                    throw e.InnerException; 
                }
                finally { 
                    LicenseManager.UnlockContext(licenseManagerLock); 
                    LicenseManager.CurrentContext = originalContext;
                } 
            }

            return parsedControl;
        } 

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.DeserializeControl1"]/*' /> 
        /// <devdoc> 
        /// Creates a control instance from the specified text using the filtered property values
        /// </devdoc> 
        public static Control DeserializeControl(string text, IDesignerHost host) {
            return DeserializeControlInternal(text, host, false);
        }
 
        public static Control[] DeserializeControls(string text, IDesignerHost host) {
            return DeserializeControlsInternal(text, host, null); 
        } 

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.DeserializeControls1"]/*' /> 
        /// <devdoc>
        /// Creates a set of controls from the specified text using the default filtered values
        /// </devdoc>
        internal static Control[] DeserializeControlsInternal(string text, IDesignerHost host, List<Triplet> userControlRegisterEntries) { 
            if (host == null) {
                throw new ArgumentNullException("host"); 
            } 

            if ((text == null) || (text.Length == 0)) { 
                throw new ArgumentNullException("text");
            }

            string directives = GetDirectives(host); 

            if ((directives != null) && (directives.Length > 0)) { 
                text = directives + text; 
            }
 
            DesignTimeParseData parseData = new DesignTimeParseData(host, text, GetCurrentFilter(host));

            parseData.DataBindingHandler = GlobalDataBindingHandler.Handler;
 
            Control[] parsedControls = null;
 
            lock (typeof(LicenseManager)) { 
                LicenseContext originalContext = LicenseManager.CurrentContext;
 
                try {
                    LicenseManager.CurrentContext = new WebFormsDesigntimeLicenseContext(host);
                    LicenseManager.LockContext(licenseManagerLock);
                    parsedControls = DesignTimeTemplateParser.ParseControls(parseData); 
                }
                catch (TargetInvocationException e) { 
                    Debug.Assert(e.InnerException != null); 
                    throw e.InnerException;
                } 
                finally {
                    LicenseManager.UnlockContext(licenseManagerLock);
                    LicenseManager.CurrentContext = originalContext;
                } 
            }
 
            if (userControlRegisterEntries != null && parseData.UserControlRegisterEntries != null) { 
                foreach (Triplet triplet in parseData.UserControlRegisterEntries) {
                    userControlRegisterEntries.Add(triplet); 
                }
            }

            return parsedControls; 
        }
 
        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.DeserializeTemplate"]/*' /> 
        /// <devdoc>
        /// Creates an ITemplate from the specified text 
        /// </devdoc>
        public static ITemplate DeserializeTemplate(string text, IDesignerHost host) {
            if (host == null) {
                throw new ArgumentNullException("host"); 
            }
 
            if ((text == null) || (text.Length == 0)) { 
                return null;
            } 

            string parseText = text;
            string directives = GetDirectives(host);
 
            if ((directives != null) && (directives.Length > 0)) {
                parseText = directives + text; 
            } 

            DesignTimeParseData parseData = new DesignTimeParseData(host, parseText); 

            parseData.DataBindingHandler = GlobalDataBindingHandler.Handler;

            ITemplate parsedTemplate = null; 

            lock (typeof(LicenseManager)) { 
                LicenseContext originalContext = LicenseManager.CurrentContext; 

                try { 
                    LicenseManager.CurrentContext = new WebFormsDesigntimeLicenseContext(host);
                    LicenseManager.LockContext(licenseManagerLock);
                    parsedTemplate = DesignTimeTemplateParser.ParseTemplate(parseData);
                } 
                catch (TargetInvocationException e) {
                    Debug.Assert(e.InnerException != null); 
                    throw e.InnerException; 
                }
                finally { 
                    LicenseManager.UnlockContext(licenseManagerLock);
                    LicenseManager.CurrentContext = originalContext;
                }
            } 

            if (parsedTemplate != null) { 
                // The parsed template contains all the text sent to the parser 
                // which includes the register directives.
                // We don't want to have these directives end up in the template 
                // text. Unfortunately, theres no way to pass them as a separate
                // text block to the parser, so we'll have to do some fixup here.
                Debug.Assert(parsedTemplate is TemplateBuilder, "Unexpected type of ITemplate implementation.");
                if (parsedTemplate is TemplateBuilder) { 
                    ((TemplateBuilder)parsedTemplate).Text = text;
                } 
            } 

            return parsedTemplate; 
        }

        private static void SerializeAttribute(object obj, PropertyDescriptor propDesc, DataBindingCollection dataBindings, ExpressionBindingCollection expressions, IDesignerHost host, string prefix, ObjectPersistData persistData, string filter, ArrayList attributes, bool topLevelInDesigner) {
            // Skip design-time only properties such as DefaultModifiers and Name 
            DesignOnlyAttribute designOnlyAttr = (DesignOnlyAttribute)propDesc.Attributes[typeof(DesignOnlyAttribute)];
 
            if ((designOnlyAttr != null) && designOnlyAttr.IsDesignOnly) { 
                return;
            } 

            string propName = propDesc.Name;
            Type propType = propDesc.PropertyType;
            PersistenceMode persistenceMode = ((PersistenceModeAttribute)propDesc.Attributes[typeof(PersistenceModeAttribute)]).Mode; 

            bool isDataBound = (dataBindings != null && dataBindings[propName] != null); 
            bool isExpressionBound = (expressions != null && expressions[propName] != null); 

            // Skip anything that should be hidden and is not data-bound or expression-bound 
            if (!isDataBound && !isExpressionBound && (propDesc.SerializationVisibility == DesignerSerializationVisibility.Hidden)) {
                return;
            }
 
            // Persist attributes and databound inner-property strings as attributes, skip all others
            if ((persistenceMode != PersistenceMode.Attribute) && (!isDataBound || !isExpressionBound || (propType != typeof(string))) && 
                // Maybe persist defaultinner and encoded default inner as attributes 
                ((persistenceMode == PersistenceMode.InnerProperty) || (propType != typeof(string)))) {
                return; 
            }

            string persistName = String.Empty;
 
            if (prefix.Length > 0) {
                persistName = prefix + "-" + propName; 
            } 
            else {
                persistName = propName; 
            }

            if (propDesc.SerializationVisibility == DesignerSerializationVisibility.Content) {
                object propValue = propDesc.GetValue(obj); 

                // Recursively persist dash-syntax properties, re-using the same default properties persist data, 
                // since dash-syntax properties are stored in the same control builder 
                SerializeAttributesRecursive(propValue, host, persistName, persistData, filter, attributes, dataBindings, expressions, topLevelInDesigner);
            } 
            else {
                // Skip read-only properties unless the object is an IAttributeAccessor, except
                // when there is already a valid entry in the attribute collection.
                IAttributeAccessor attributeAccessor = obj as IAttributeAccessor; 
                if (propDesc.IsReadOnly && ((attributeAccessor == null) || (attributeAccessor.GetAttribute(persistName) == null))) {
                    return; 
                } 

                string objectModelName = ConvertPersistToObjectModelName(persistName); 

                // If the property is not filterable, don't put a filter on the value
                if (!FilterableAttribute.IsPropertyFilterable(propDesc)) {
                    filter = String.Empty; 
                }
 
                // Don't persist inner defaults as attributes. 
                if (CanSerializeAsInnerDefaultString(filter, objectModelName, propType, persistData, persistenceMode, dataBindings, expressions)) {
                    // Make sure we remove these from the attributes if we are persisting top-level 
                    // attributes in the designer
                    if (topLevelInDesigner) {
                        attributes.Add(new Triplet(filter, persistName, null));
                    } 

                    return; 
                } 

                /* 
                bool shouldSerialize = true;

                object defaultValue = GetPropertyDefaultValue(propDesc, persistName, persistData, filter);
 
                if (defaultValue == null){
                    if (filter.Length == 0) { 
                        // Check the ShouldSerialize method to see if we need to serialize this string 
                        // Only do this for the default filter
                        shouldSerialize = GetShouldSerializeValue(obj, propName); 
                    }
                }
                else {
                    shouldSerialize = !object.Equals(propValue, defaultValue); 
                }
                */ 
                bool shouldSerialize = true; 
                object defaultValue = null;
 
                object propValue = propDesc.GetValue(obj);

                // Check if the property is in the databindings collection
                BindingType bindingType = BindingType.None; 

                if (dataBindings != null) { 
                    DataBinding binding = dataBindings[objectModelName]; 

                    if (binding != null) { 
                        propValue = binding.Expression;
                        bindingType = BindingType.Data;
                    }
                } 

                if (bindingType == BindingType.None) { 
                    if (expressions != null) { 
                        ExpressionBinding binding = expressions[objectModelName];
 
                        if (binding != null) {
                            // If the binding is not auto-generated, persist it as an explicit expression.
                            // Otherwise, do nothing and pretend as though it was a regular property so
                            // that we end up persisting the live value of the property. 
                            if (!binding.Generated) {
                                propValue = binding.ExpressionPrefix + ":" + binding.Expression; 
                                bindingType = BindingType.Expression; 
                            }
                        } 
                    }
                    else if (persistData != null) {
                        // We need to persist the expression for this property if it was expression-bound
                        // but the object isn't an IExpressionsAccessor 
                        BoundPropertyEntry bpe = persistData.GetFilteredProperty(filter, propName) as BoundPropertyEntry;
                        if (bpe != null) { 
                            // If the binding is not auto-generated, persist it as an explicit expression. 
                            // Otherwise, do nothing and pretend as though it was a regular property so
                            // that we end up persisting the live value of the property. 
                            if (!bpe.Generated) {
                                // Don't use the expression binding if we already have a value for
                                // this attribute. This is because if the user changed the value of
                                // a property for an object that is not an IExpressionAccessor, we 
                                // want to persist their new value (which would be a simple value, not
                                // an expression), not the old expression. 
                                defaultValue = GetPropertyDefaultValue(propDesc, persistName, persistData, filter, host); 
                                if (Object.Equals(propValue, defaultValue)) {
                                    propValue = bpe.ExpressionPrefix + ":" + bpe.Expression; 
                                    bindingType = BindingType.Expression;
                                }
                            }
                        } 
                    }
                } 
 
                if (filter.Length == 0) {
                    bool useResult = false; 
                    bool customShouldSerialize = false;

                    // If it wasn't data or expression-bound, check the ShouldSerialize value and default values to
                    // determine if we want to serialize.  It was bound, always serialize. 
                    if (bindingType == BindingType.None) {
                        customShouldSerialize = GetShouldSerializeValue(obj, propName, out useResult); 
                    } 

                    if (useResult) { 
                        shouldSerialize = customShouldSerialize;
                    }
                    else {
                        defaultValue = GetPropertyDefaultValue(propDesc, persistName, persistData, filter, host); 
                        shouldSerialize = !object.Equals(propValue, defaultValue);
                    } 
                } 
                else {
                    defaultValue = GetPropertyDefaultValue(propDesc, persistName, persistData, filter, host); 
                    shouldSerialize = !object.Equals(propValue, defaultValue);
                }

                // If the value of the property is non-null and different from the default value 
                if (shouldSerialize) {
                    string persistValue = GetPersistValue(propDesc, propType, propValue, bindingType, topLevelInDesigner); 
 
                    if (topLevelInDesigner) {
                        // HACK: Trident will remove attributes with a String.Empty value, 
                        //       so stick in a single space string
                        if ((defaultValue != null) && ((persistValue == null) || (persistValue.Length == 0)) && ShouldPersistBlankValue(defaultValue, propType)) {
                            persistValue = String.Empty;
                        } 
                    }
 
                    // 

                    if ((persistValue != null) && (!propType.IsArray || persistValue.Length > 0)) { 
                        attributes.Add(new Triplet(filter, persistName, persistValue));
                    }
                    else if (topLevelInDesigner) {
                        attributes.Add(new Triplet(filter, persistName, null)); 
                    }
                } 
                else if (topLevelInDesigner) { 
                    attributes.Add(new Triplet(filter, persistName, null));
                } 

                // Add properties from all other filters
                if (persistData != null) {
                    ICollection filteredProperties = persistData.GetPropertyAllFilters(objectModelName); 

                    foreach (PropertyEntry entry in filteredProperties) { 
                        if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0) { 
                            string persistValue = String.Empty;
 
                            if (entry is SimplePropertyEntry) {
                                SimplePropertyEntry spe = (SimplePropertyEntry)entry;

                                if (spe.UseSetAttribute) { 
                                    persistValue = spe.Value.ToString();
                                } 
                                else { 
                                    persistValue = GetPersistValue(propDesc, entry.Type, spe.Value, BindingType.None, topLevelInDesigner);
                                } 
                            }
                            else if (entry is BoundPropertyEntry) {
                                BoundPropertyEntry bpe = (BoundPropertyEntry)entry;
                                if (bpe.Generated) { 
                                    continue;
                                } 
                                string expression = bpe.Expression.Trim(); 

                                bindingType = BindingType.Data; 

                                string expressionPrefix = bpe.ExpressionPrefix;

                                if (expressionPrefix.Length > 0) { 
                                    expression = expressionPrefix + ":" + expression;
                                    bindingType = BindingType.Expression; 
                                } 

                                persistValue = GetPersistValue(propDesc, entry.Type, expression, bindingType, topLevelInDesigner); 
                            }
                            else if (entry is ComplexPropertyEntry) {
                                ComplexPropertyEntry cpe = (ComplexPropertyEntry)entry;
                                object o = cpe.Builder.BuildObject(); 

                                Debug.Assert(o is string); 
                                persistValue = (string)o; 
                            }
                            else { 
                                Debug.Fail("Unexpected PropertyEntry type!");
                            }

                            attributes.Add(new Triplet(entry.Filter, persistName, persistValue)); 
                        }
                    } 
                } 
            }
        } 

        private static void SerializeAttributes(object obj, IDesignerHost host, string prefix, ObjectPersistData persistData, TextWriter writer, string filter) {
            ArrayList attributes = SerializeAttributes(obj, host, prefix, persistData, filter, false);
 
            // Write out all attributes accumulated
            foreach (Triplet triplet in attributes) { 
                WriteAttribute(writer, triplet.First.ToString(), triplet.Second.ToString(), triplet.Third.ToString()); 
            }
        } 

        /// <devdoc>
        /// Persists all the attributes of the specified object
        /// topLevelInDesigner is used by the ControlDesigner to include entries for all possible attributes 
        ///     attrs with values get the value
        ///     attrs with no value get String.Empty 
        ///     special attributes (e.g. Color, string, DateTime) get a single space string since it would get eaten by Trident otherwise 
        /// </devdoc>
        private static ArrayList SerializeAttributes(object obj, IDesignerHost host, string prefix, ObjectPersistData persistData, string filter, bool topLevelInDesigner) { 
            // Fill this with Triplets of filter, attribute name, and attribute value
            ArrayList attributes = new ArrayList();

            // Get all properties 
            SerializeAttributesRecursive(obj, host, prefix, persistData, filter, attributes, null, null, topLevelInDesigner);
 
            // Serialize all bound properties that are persisted as hyphenated names, for 
            // example, ItemStyle-BackColor="<%$ foo:bar %>"
            if (persistData != null) { 
                foreach (PropertyEntry entry in persistData.AllPropertyEntries) {
                    BoundPropertyEntry bpe = entry as BoundPropertyEntry;
                    if (bpe != null && !bpe.Generated) {
                        string[] parts = bpe.Name.Split(OM_CHAR); 
                        if (parts.Length > 1) {
                            object propValue = obj; 
                            foreach (string part in parts) { 
                                PropertyDescriptor propDesc = TypeDescriptor.GetProperties(propValue)[part];
                                // Ignore non-existent properties (i.e. expandos) 
                                if (propDesc == null) {
                                    break;
                                }
                                PersistenceModeAttribute mode = propDesc.Attributes[typeof(PersistenceModeAttribute)] as PersistenceModeAttribute; 
                                if (mode != PersistenceModeAttribute.Attribute) {
                                    // Serialize the value 
                                    string expressionValue = (String.IsNullOrEmpty(bpe.ExpressionPrefix) ? bpe.Expression : (bpe.ExpressionPrefix + ":" + bpe.Expression)); 
                                    string persistValue = GetPersistValue(
                                        TypeDescriptor.GetProperties(bpe.PropertyInfo.DeclaringType)[bpe.PropertyInfo.Name], 
                                        bpe.Type,
                                        expressionValue,
                                        (String.IsNullOrEmpty(bpe.ExpressionPrefix) ? BindingType.Data : BindingType.Expression),
                                        topLevelInDesigner); 
                                    attributes.Add(new Triplet(bpe.Filter, ConvertObjectModelToPersistName(bpe.Name), persistValue));
                                    break; 
                                } 
                                propValue = propDesc.GetValue(propValue);
                            } 
                        }
                    }
                }
            } 

            // Get all expandos from the control for this filter 
            if (obj is Control) { 
                AttributeCollection expandos = null;
                if (obj is WebControl) { 
                    expandos = ((WebControl)obj).Attributes;
                }
                else if (obj is HtmlControl) {
                    expandos = ((HtmlControl)obj).Attributes; 
                }
                else if (obj is UserControl) { 
                    expandos = ((UserControl)obj).Attributes; 
                }
 
                if (expandos != null) {
                    foreach (string key in expandos.Keys) {
                        string value = expandos[key];
 
                        bool persist = false;
 
                        if (value != null) { 

                            // If this expando matches a property that is read-write, ignore the expando 
                            bool hasWriteableProperty = false;
                            string objectModelName = ConvertPersistToObjectModelName(key);
                            object realTarget;
                            PropertyDescriptor propDesc = ControlDesigner.GetComplexProperty(obj, objectModelName, out realTarget); 
                            if (propDesc != null && !propDesc.IsReadOnly) {
                                hasWriteableProperty = true; 
                            } 

                            if (!hasWriteableProperty) { 
                                if (filter.Length == 0) {
                                    persist = true;
                                }
                                else { 
                                    PropertyEntry entry = null;
                                    if (persistData != null) { 
                                        entry = persistData.GetFilteredProperty(String.Empty, key) as PropertyEntry; 
                                    }
 
                                    if (entry is SimplePropertyEntry) {
                                        persist = !value.Equals(((SimplePropertyEntry)entry).PersistedValue);
                                    }
                                    else if (entry is BoundPropertyEntry) { 
                                        string expression = ((BoundPropertyEntry)entry).Expression;
                                        string expressionPrefix = ((BoundPropertyEntry)entry).ExpressionPrefix; 
 
                                        if (expressionPrefix.Length > 0) {
                                            expression = expressionPrefix + ":" + expression; 
                                        }

                                        persist = !value.Equals(expression);
                                    } 
                                    else if (entry == null) {
                                        persist = true; 
                                    } 
                                    else {
                                        Debug.Fail("Unexpected PropertyEntry type!"); 
                                    }
                                }
                            }
 
                            if (persist) {
                                attributes.Add(new Triplet(filter, key, value)); 
                            } 
                        }
                    } 
                }
            }

            if (persistData != null) { 
                if (!String.IsNullOrEmpty(persistData.ResourceKey)) {
                    attributes.Add(new Triplet("meta", "resourceKey", persistData.ResourceKey)); 
                } 
                if (persistData.Localize == false) {
                    attributes.Add(new Triplet("meta", "localize", "false")); 
                }

                // Get expandos for all other filters
                foreach (PropertyEntry entry in persistData.AllPropertyEntries) { 
                    if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0) {
                        string persistValue = String.Empty; 
 
                        if (entry is SimplePropertyEntry) {
                            SimplePropertyEntry spe = (SimplePropertyEntry)entry; 

                            if (spe.UseSetAttribute) {
                                attributes.Add(new Triplet(entry.Filter, ConvertObjectModelToPersistName(entry.Name), spe.Value.ToString()));
                            } 
                        }
                        else if (entry is BoundPropertyEntry) { 
                            BoundPropertyEntry bpe = (BoundPropertyEntry)entry; 

                            if (bpe.UseSetAttribute) { 
                                string expression = ((BoundPropertyEntry)entry).Expression;
                                string expressionPrefix = ((BoundPropertyEntry)entry).ExpressionPrefix;

                                if (expressionPrefix.Length > 0) { 
                                    expression = expressionPrefix + ":" + expression;
                                } 
 
                                attributes.Add(new Triplet(entry.Filter, ConvertObjectModelToPersistName(entry.Name), expression));
                            } 
                        }
                    }
                }
            } 

            if ((obj is Control) && (persistData != null) && (host.GetDesigner((Control)obj) == null)) { 
                foreach (EventEntry entry in persistData.EventEntries) { 
                    attributes.Add(new Triplet(String.Empty, "On" + entry.Name, entry.HandlerMethodName));
                } 
            }

            return attributes;
        } 

        /// <devdoc> 
        /// Helper method for persisting attributes 
        /// </devdoc>
        private static void SerializeAttributesRecursive(object obj, IDesignerHost host, string prefix, ObjectPersistData persistData, string filter, ArrayList attributes, DataBindingCollection dataBindings, ExpressionBindingCollection expressions, bool topLevelInDesigner) { 
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj);

            if (obj is IDataBindingsAccessor) {
                dataBindings = ((IDataBindingsAccessor)obj).DataBindings; 
            }
 
            if (obj is Control) { 
                // Force ChildControl Creation (needed for controls like CreateUserWizard which change the persistence)
                try { 
                    ControlCollection controls = ((Control)obj).Controls;
                }
                catch (Exception ex) {
                    // Log the error with the IComponentDesignerDebugService 
                    IComponentDesignerDebugService debugService = host.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService;
                    if (debugService != null) { 
                        debugService.Fail(ex.Message); 
                    }
                } 
            }

            if (obj is IExpressionsAccessor) {
                expressions = ((IExpressionsAccessor)obj).Expressions; 
            }
 
            for (int i = 0; i < properties.Count; i++) { 
                try {
                    SerializeAttribute(obj, properties[i], dataBindings, expressions, host, prefix, persistData, filter, attributes, topLevelInDesigner); 
                }
                catch (Exception e) {
                    if (host != null) {
                        // Log the error with the IComponentDesignerDebugService 
                        IComponentDesignerDebugService debugService = host.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService;
                        if (debugService != null) { 
                            debugService.Fail(e.Message); 
                        }
                    } 
                }
            }
        }
 
        /// <devdoc>
        /// Helper method for persisting out collections 
        /// </devdoc> 
        private static void SerializeCollectionProperty(object obj, IDesignerHost host, PropertyDescriptor propDesc, ObjectPersistData persistData, PersistenceMode persistenceMode, TextWriter writer, string filter) {
            string name = propDesc.Name; 
            bool persistNewCollection = false;

            // Get the number of items in the current filter's collection
            ICollection collection = propDesc.GetValue(obj) as ICollection; 
            int count = 0;
 
            if (collection != null) { 
                count = collection.Count;
            } 

            // Get the number of items in the default filter's collection
            int defaultCount = 0;
            ObjectPersistData defaultPersistData = null; 

            if (persistData != null) { 
                ComplexPropertyEntry defaultEntry = persistData.GetFilteredProperty(String.Empty, name) as ComplexPropertyEntry; 

                if (defaultEntry != null) { 
                    defaultPersistData = defaultEntry.Builder.GetObjectPersistData();
                    defaultCount = defaultPersistData.CollectionItems.Count;
                }
            } 

            // Make sure we persist the default filter's collection 
            if (filter.Length == 0) { 
                persistNewCollection = true;
            } 
            else if (persistData != null) {
                // If the collection was originally a seperate collection, persist it like that
                if ((persistData.GetFilteredProperty(filter, name) as ComplexPropertyEntry) != null) {
                    persistNewCollection = true; 
                }
                    // If the collection are different sizes, we need to persist a new collection 
                else if (defaultCount != count) { 
                    persistNewCollection = true;
                } 
                    // Also check if all the types in the collections are in the same order and of the same type
                    // if not, we also need a new collection
                else if (defaultPersistData != null) {
                    IEnumerator enumerator = collection.GetEnumerator(); 
                    IEnumerator defaultCollectionEnumerator = defaultPersistData.CollectionItems.GetEnumerator();
 
                    while (enumerator.MoveNext()) { 
                        defaultCollectionEnumerator.MoveNext();
                        Debug.Assert(defaultCollectionEnumerator.Current is ComplexPropertyEntry && ((ComplexPropertyEntry)defaultCollectionEnumerator.Current).IsCollectionItem); 

                        ComplexPropertyEntry cipe = (ComplexPropertyEntry)defaultCollectionEnumerator.Current;

                        if (enumerator.Current.GetType() != cipe.Builder.ControlType) { 
                            persistNewCollection = true;
                            break; 
                        } 
                    }
                } 
            }

            // Used to remember if we already persisted the default filter's collection
            bool skipDefaultFilter = false; 

            // Store all the persisted collection texts 
            ArrayList collections = new ArrayList(); 

            // If the collections are the same size, just use filtered attributes on the collection items 
            if (count > 0) {
                StringWriter collectionWriter = new StringWriter(CultureInfo.InvariantCulture);

                // Build a table of object to ControlBuilder mappings 
                // so we can match collection items to their original ControlBuilders
                IDictionary objectControlBuilderTable = new Hashtable(ReferenceKeyComparer.Default); 
 
                if (defaultPersistData != null) {
                    foreach (ComplexPropertyEntry cpe in defaultPersistData.CollectionItems) { 
                        Debug.Assert(cpe.IsCollectionItem, "Expected a collection item");
                        ObjectPersistData itemPersistData = cpe.Builder.GetObjectPersistData();

                        if (itemPersistData != null) { 
                            itemPersistData.AddToObjectControlBuilderTable(objectControlBuilderTable);
                        } 
                    } 
                }
 
                if (!persistNewCollection) {
                    // Remember that we've already persisted the default filter's collection
                    skipDefaultFilter = true;
 
                    // Loop through all items in the collection
                    foreach (object current in collection) { 
                        string itemTypeName = GetTagName(current.GetType(), host); 

                        // Get the persist data for this instance of the collection item 
                        ObjectPersistData itemPersistData = null;
                        ControlBuilder itemBuilder = (ControlBuilder)objectControlBuilderTable[current];

                        if (itemBuilder != null) { 
                            itemPersistData = itemBuilder.GetObjectPersistData();
                        } 
 
                        collectionWriter.Write('<');
                        collectionWriter.Write(itemTypeName); 

                        // Persist out the filtered attributes of each item
                        SerializeAttributes(current, host, String.Empty, itemPersistData, collectionWriter, filter);
                        collectionWriter.Write('>'); 

                        // Persist out the filtered inner props of each item 
                        SerializeInnerProperties(current, host, itemPersistData, collectionWriter, filter); 
                        collectionWriter.Write("</");
                        collectionWriter.Write(itemTypeName); 
                        collectionWriter.WriteLine('>');
                    }

                    IDictionary expandos = GetExpandos(filter, name, defaultPersistData); 

                    collections.Add(new Triplet(String.Empty, collectionWriter, expandos)); 
                } 
                    // Otherwise, create an entirely new collection
                else { 
                    foreach (object current in collection) {
                        string itemTypeName = GetTagName(current.GetType(), host);

                        if (current is Control) { 
                            // default filter string since the the child controls collection can never be filtered
                            SerializeControl((Control)current, host, collectionWriter, String.Empty); 
                        } 
                        else {
                            collectionWriter.Write('<'); 
                            collectionWriter.Write(itemTypeName);

                            // Get the persist data for this instance of the collection item
                            ObjectPersistData itemPersistData = null; 
                            ControlBuilder itemBuilder = (ControlBuilder)objectControlBuilderTable[current];
 
                            if (itemBuilder != null) { 
                                itemPersistData = itemBuilder.GetObjectPersistData();
                            } 

                            if ((filter.Length == 0) && (itemPersistData != null)) {
                                // If this is the default collection, we need to pass in persistData for the collection
                                SerializeAttributes(current, host, String.Empty, itemPersistData, collectionWriter, String.Empty); 
                                collectionWriter.Write('>');
                                SerializeInnerProperties(current, host, itemPersistData, collectionWriter, String.Empty); 
                            } 
                            else {
                                // no persistData or filter since this is a new collection item 
                                SerializeAttributes(current, host, String.Empty, null, collectionWriter, String.Empty);
                                collectionWriter.Write('>');
                                SerializeInnerProperties(current, host, null, collectionWriter, String.Empty);
                            } 

                            collectionWriter.Write("</"); 
                            collectionWriter.Write(itemTypeName); 
                            collectionWriter.WriteLine('>');
                        } 
                    }

                    IDictionary expandos = GetExpandos(filter, name, persistData);
 
                    collections.Add(new Triplet(filter, collectionWriter, expandos));
                } 
            } 
            else if (defaultCount > 0) {
                // Since we'll be emitting a collection for the default filter, we need to emit an empty collection for this filter 
                IDictionary expandos = GetExpandos(filter, name, persistData);

                collections.Add(new Triplet(filter, new StringWriter(CultureInfo.InvariantCulture), expandos));
            } 

            // Persist any other filtered collections 
            if (persistData != null) { 
                ICollection filteredProperties = persistData.GetPropertyAllFilters(name);
 
                foreach (ComplexPropertyEntry entry in filteredProperties) {
                    StringWriter collectionWriter = new StringWriter(CultureInfo.InvariantCulture);

                    // Only persist collections different than the current one and if 
                    // we already persisted the default filter, skip that too
                    if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0 && (!skipDefaultFilter || entry.Filter.Length > 0)) { 
                        ObjectPersistData collectionPersistData = entry.Builder.GetObjectPersistData(); 
                        IEnumerator enumerator = collectionPersistData.CollectionItems.GetEnumerator();
 
                        foreach (ComplexPropertyEntry cpe in collectionPersistData.CollectionItems) {
                            Debug.Assert(cpe.IsCollectionItem, "Expected a collection item");
                            object current = cpe.Builder.BuildObject();
 
                            if (current is Control) {
                                // default filter string since the the child controls collection can never be filtered 
                                SerializeControl((Control)current, host, collectionWriter, String.Empty); 
                            }
                            else { 
                                string itemTypeName = GetTagName(current.GetType(), host);
                                ObjectPersistData itemPersistData = cpe.Builder.GetObjectPersistData();

                                collectionWriter.Write('<'); 
                                collectionWriter.Write(itemTypeName);
 
                                // no filter since this is a new collection 
                                SerializeAttributes(current, host, String.Empty, itemPersistData, collectionWriter, String.Empty);
                                collectionWriter.Write('>'); 

                                // no filter since this is a new collection
                                SerializeInnerProperties(current, host, itemPersistData, collectionWriter, String.Empty);
                                collectionWriter.Write("</"); 
                                collectionWriter.Write(itemTypeName);
                                collectionWriter.WriteLine('>'); 
                            } 
                        }
 
                        IDictionary expandos = GetExpandos(entry.Filter, name, persistData);

                        collections.Add(new Triplet(entry.Filter, collectionWriter, expandos));
                    } 
                }
            } 
 
            // Write out all the filtered collections
            foreach (Triplet triplet in collections) { 
                string collectionFilter = triplet.First.ToString();
                IDictionary expandos = (IDictionary)triplet.Third;

                if ((collections.Count == 1) && (collectionFilter.Length == 0) && (persistenceMode != PersistenceMode.InnerProperty) && ((expandos == null) || (expandos.Count == 0))) { 
                    writer.Write(triplet.Second.ToString());
                } 
                else { 
                    string collectionContent = triplet.Second.ToString().Trim();
 
                    if (collectionContent.Length > 0) {
                        WriteInnerPropertyBeginTag(writer, collectionFilter, name, expandos, true);
                        writer.WriteLine(collectionContent);
                        WriteInnerPropertyEndTag(writer, collectionFilter, name); 
                    }
                } 
            } 
        }
 
        private static void SerializeComplexProperty(object obj, IDesignerHost host, PropertyDescriptor propDesc, ObjectPersistData persistData, TextWriter writer, string filter) {
            string name = propDesc.Name;

            // Check if anything would be persisted for this complex property 
            // If everything was set to the default values, we don't want to persist anything
            object propValue = propDesc.GetValue(obj); 
            ObjectPersistData childPersistData = null; 

            // Get the persistData for the property's value 
            if (persistData != null) {
                ComplexPropertyEntry entry = persistData.GetFilteredProperty(String.Empty, name) as ComplexPropertyEntry;

                if (entry != null) { 
                    childPersistData = entry.Builder.GetObjectPersistData();
                } 
            } 

            StringWriter innerPropertyWriter = new StringWriter(CultureInfo.InvariantCulture); 

            SerializeInnerProperties(propValue, host, childPersistData, innerPropertyWriter, filter);

            string innerPropertyString = innerPropertyWriter.ToString(); 
            ArrayList attributes = SerializeAttributes(propValue, host, String.Empty, childPersistData, filter, false);
            StringWriter attributeWriter = new StringWriter(CultureInfo.InvariantCulture); 
            bool onlyDesignerAttributes = true; 

            foreach (Triplet triplet in attributes) { 
                string attributeFilter = triplet.First.ToString();

                if (attributeFilter != ControlBuilder.DesignerFilter) {
                    onlyDesignerAttributes = false; 
                }
 
                WriteAttribute(attributeWriter, attributeFilter, triplet.Second.ToString(), triplet.Third.ToString()); 
            }
 
            // When persist attributes, we only want to persist attribute if
            // there is an attribute which isn't a designer attribute
            // so we can remove the entire complex property tag, if there are no valid attributes
            string attributeString = String.Empty; 

            if (!onlyDesignerAttributes || (innerPropertyString.Length > 0)) { 
                attributeString = attributeWriter.ToString(); 
            }
 
            // If anything was persisted, we need to persist the tag
            if (attributeString.Length + innerPropertyString.Length > 0) {
                // Always write out a single tag for complex properties since we can just filter each of its attributes and properties
                writer.WriteLine(); 
                writer.Write('<');
                writer.Write(name); 
                writer.Write(attributeString); 
                writer.Write('>');
                writer.Write(innerPropertyString); 
                WriteInnerPropertyEndTag(writer, String.Empty, name);
            }

            // Persist the rest of the filters' complex properties 
            if (persistData != null) {
                ICollection filteredProperties = persistData.GetPropertyAllFilters(name); 
 
                foreach (ComplexPropertyEntry entry in filteredProperties) {
                    if (entry.Filter.Length > 0) { 
                        object current = entry.Builder.BuildObject();

                        writer.WriteLine();
                        writer.Write('<'); 
                        writer.Write(entry.Filter);
                        writer.Write(FILTER_SEPARATOR_CHAR); 
                        writer.Write(name); 

                        // No persist data or filter since this the outside property is already filtered 
                        SerializeAttributes(current, host, String.Empty, null, writer, String.Empty);
                        writer.Write('>');

                        // No persist data or filter since this the outside property is already filtered 
                        SerializeInnerProperties(current, host, null, writer, String.Empty);
                        WriteInnerPropertyEndTag(writer, entry.Filter, name); 
                    } 
                }
            } 
        }

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeControl1"]/*' />
        /// <devdoc> 
        /// Gets a string that contains the persisted form of the control, persisting its properties values to the specified filter's properties
        /// </devdoc> 
        public static string SerializeControl(Control control) { 
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
 
            SerializeControl(control, writer);
            return writer.ToString();
        }
 
        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeControl4"]/*' />
        /// <devdoc> 
        /// Gets a string that contains the persisted form of the control, persisting its properties values to the specified filter's properties 
        /// </devdoc>
        public static string SerializeControl(Control control, IDesignerHost host) { 
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

            SerializeControl(control, host, writer);
            return writer.ToString(); 
        }
 
        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeControl5"]/*' /> 
        /// <devdoc>
        /// Writes the persisted form of the control to the specified TextWriter, persisting its properties values to the specified filter's properties 
        /// </devdoc>
        public static void SerializeControl(Control control, TextWriter writer) {
            ISite site = control.Site;
 
            if (site == null) {
                IComponent baseComponent = (IComponent)control.Page; 
 
                if (baseComponent != null) {
                    site = baseComponent.Site; 
                }
            }

            IDesignerHost host = null; 

            if (site != null) { 
                host = (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
            }
 
            Debug.Assert(host != null, "Did not get a valid IDesignerHost reference. Expect persistence problems!");
            SerializeControl(control, host, writer);
        }
 
        public static void SerializeControl(Control control, IDesignerHost host, TextWriter writer) {
            SerializeControl(control, host, writer, GetCurrentFilter(host)); 
        } 

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeControl7"]/*' /> 
        /// <devdoc>
        /// Writes the persisted form of the control to the specified TextWriter, persisting its properties values to the specified filter's properties
        /// </devdoc>
        private static void SerializeControl(Control control, IDesignerHost host, TextWriter writer, string filter) { 
            if (control == null) {
                throw new ArgumentNullException("control"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            if (writer == null) { 
                throw new ArgumentNullException("writer");
            } 
 
            // Special case for Literal Controls - just persist the text
            if (control is LiteralControl) { 
                writer.Write(((LiteralControl)control).Text);
            }
                // Special case for designer databound literals - just persist the expression
            else if (control is DesignerDataBoundLiteralControl) { 
                Debug.Assert(((IDataBindingsAccessor)control).HasDataBindings == true);
 
                DataBindingCollection bindings = ((IDataBindingsAccessor)control).DataBindings; 
                DataBinding textBinding = bindings["Text"];
 
                Debug.Assert(textBinding != null, "Did not get a Text databinding from DesignerDataBoundLiteralControl");
                if (textBinding != null) {
                    writer.Write("<%# ");
                    writer.Write(textBinding.Expression); 
                    writer.Write(" %>");
                } 
            } 
                // Special case for user controls, just persist the attributes
            else if (control is UserControl) { 
                IUserControlDesignerAccessor accessor = (IUserControlDesignerAccessor)control;
                string tagName = accessor.TagName;

                Debug.Assert((tagName != null) && (tagName.Length > 0)); 
                if (tagName.Length > 0) {
                    writer.Write('<'); 
                    writer.Write(tagName); 
                    writer.Write(" runat=\"server\"");
 
                    ObjectPersistData persistData = null;
                    IControlBuilderAccessor cba = (IControlBuilderAccessor)control;

                    if (cba.ControlBuilder != null) { 
                        persistData = cba.ControlBuilder.GetObjectPersistData();
                    } 
 
                    SerializeAttributes(control, host, String.Empty, persistData, writer, filter);
                    writer.Write('>'); 

                    string innerText = accessor.InnerText;

                    if ((innerText != null) && (innerText.Length > 0)) { 
                        writer.Write(accessor.InnerText);
                    } 
 
                    writer.Write("</");
                    writer.Write(tagName); 
                    writer.WriteLine('>');
                }
            }
            else { 
                string tagName;
                HtmlControl htmlControl = control as HtmlControl; 
                if (htmlControl != null) { 
                    tagName = htmlControl.TagName;
                } 
                else {
                    tagName = GetTagName(control.GetType(), host);
                }
 
                writer.Write('<');
                writer.Write(tagName); 
                writer.Write(" runat=\"server\""); 

                ObjectPersistData persistData = null; 
                IControlBuilderAccessor cba = (IControlBuilderAccessor)control;

                if (cba.ControlBuilder != null) {
                    persistData = cba.ControlBuilder.GetObjectPersistData(); 
                }
 
                SerializeAttributes(control, host, String.Empty, persistData, writer, filter); 
                writer.Write('>');
 
                SerializeInnerContents(control, host, persistData, writer, filter);

                writer.Write("</");
                writer.Write(tagName); 
                writer.WriteLine('>');
            } 
        } 

        public static string SerializeInnerContents(Control control, IDesignerHost host) { 
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

            ObjectPersistData persistData = null;
            IControlBuilderAccessor cba = (IControlBuilderAccessor)control; 
            if (cba.ControlBuilder != null) {
                persistData = cba.ControlBuilder.GetObjectPersistData(); 
            } 

            SerializeInnerContents(control, host, persistData, writer, GetCurrentFilter(host)); 
            return writer.ToString();
        }

        internal static void SerializeInnerContents(Control control, IDesignerHost host, ObjectPersistData persistData, TextWriter writer, string filter) { 
            PersistChildrenAttribute persistChildrenAttr = (PersistChildrenAttribute)TypeDescriptor.GetAttributes(control)[typeof(PersistChildrenAttribute)];
            ParseChildrenAttribute parseChildrenAttr = (ParseChildrenAttribute)TypeDescriptor.GetAttributes(control)[typeof(ParseChildrenAttribute)]; 
 
            // If we are supposed to persist the children, persist them
            if (persistChildrenAttr.Persist || !parseChildrenAttr.ChildrenAsProperties && control.HasControls()) { 
                for (int i = 0; i < control.Controls.Count; i++) {
                    // Send in the empty filter since we aren't filtering the child controls collection
                    SerializeControl(control.Controls[i], host, writer, String.Empty);
                } 
            }
            else { 
                // Otherwise, persist this control's properties 
                SerializeInnerProperties(control, host, persistData, writer, filter);
            } 
        }

        public static string SerializeInnerProperties(object obj, IDesignerHost host) {
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture); 

            SerializeInnerProperties(obj, host, writer); 
            return writer.ToString(); 
        }
 
        internal static void SerializeInnerProperties(object obj, IDesignerHost host, TextWriter writer) {
            ObjectPersistData persistData = null;
            IControlBuilderAccessor cba = (IControlBuilderAccessor)obj;
 
            if (cba.ControlBuilder != null) {
                persistData = cba.ControlBuilder.GetObjectPersistData(); 
            } 

            SerializeInnerProperties(obj, host, persistData, writer, GetCurrentFilter(host)); 
        }

        private static void SerializeInnerProperties(object obj, IDesignerHost host, ObjectPersistData persistData, TextWriter writer, string filter) {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj); 

            if (obj is Control) { 
                // Force ChildControl Creation (needed for controls like CreateUserWizard which change the persistence) 
                try {
                    ControlCollection controls = ((Control)obj).Controls; 
                }
                catch (Exception ex) {
                    // Log the error with the IComponentDesignerDebugService
                    IComponentDesignerDebugService debugService = host.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService; 
                    if (debugService != null) {
                        debugService.Fail(ex.Message); 
                    } 
                }
            } 

            for (int i = 0; i < properties.Count; i++) {
                try {
                    string realFilter = filter; 

                    // If the property is not filterable, don't put a filter on the value 
                    if (!FilterableAttribute.IsPropertyFilterable(properties[i])) { 
                        realFilter = String.Empty;
                    } 

                    // Skip anything that should be hidden
                    if (properties[i].SerializationVisibility == DesignerSerializationVisibility.Hidden) {
                        continue; 
                    }
 
                    // Skip attributes 
                    PersistenceModeAttribute persistenceMode = (PersistenceModeAttribute)properties[i].Attributes[typeof(PersistenceModeAttribute)];
 
                    if (persistenceMode.Mode == PersistenceMode.Attribute) {
                        continue;
                    }
 
                    // Skip design-time only properties such as DefaultModifiers and Name
                    DesignOnlyAttribute designOnlyAttr = (DesignOnlyAttribute)properties[i].Attributes[typeof(DesignOnlyAttribute)]; 
 
                    if ((designOnlyAttr != null) && designOnlyAttr.IsDesignOnly) {
                        continue; 
                    }

                    string name = properties[i].Name;
 
                    if (properties[i].PropertyType == typeof(string)) {
                        SerializeStringProperty(obj, host, properties[i], persistData, persistenceMode.Mode, writer, filter); 
                    } 
                    else if (typeof(ICollection).IsAssignableFrom(properties[i].PropertyType)) {
                        SerializeCollectionProperty(obj, host, properties[i], persistData, persistenceMode.Mode, writer, filter); 
                    }
                    else if (typeof(ITemplate).IsAssignableFrom(properties[i].PropertyType)) {
                        SerializeTemplateProperty(obj, host, properties[i], persistData, writer, filter);
                    } 
                    else {
                        SerializeComplexProperty(obj, host, properties[i], persistData, writer, filter); 
                    } 
                }
                catch (Exception e) { 
                    if (host != null) {
                        // Log the error with the IComponentDesignerDebugService
                        IComponentDesignerDebugService debugService = host.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService;
                        if (debugService != null) { 
                            debugService.Fail(e.Message);
                        } 
                    } 
                }
            } 
        }

        private static void SerializeStringProperty(object obj, IDesignerHost host, PropertyDescriptor propDesc, ObjectPersistData persistData, PersistenceMode persistenceMode, TextWriter writer, string filter) {
            string name = propDesc.Name; 
            DataBindingCollection dataBindings = null;
 
            if (obj is IDataBindingsAccessor) { 
                dataBindings = ((IDataBindingsAccessor)obj).DataBindings;
            } 

            ExpressionBindingCollection expressions = null;

            if (obj is IExpressionsAccessor) { 
                expressions = ((IExpressionsAccessor)obj).Expressions;
            } 
 
            if ((persistenceMode != PersistenceMode.InnerProperty) && !CanSerializeAsInnerDefaultString(filter, name, propDesc.PropertyType, persistData, persistenceMode, dataBindings, expressions)) {
                return; 
            }

            ArrayList stringInnerProperties = new ArrayList();
 
            // Skip databound properties since we've already set them as attributes
            if ((dataBindings == null) || (dataBindings[name] == null) || (expressions == null) || (expressions[name] == null)) { 
                string persistValue = String.Empty; 
                object propValue = propDesc.GetValue(obj);
 
                if (propValue != null) {
                    persistValue = propValue.ToString();
                }
 
                /*
                bool shouldSerialize = true; 
 
                object defaultValue = GetPropertyDefaultValue(propDesc, name, persistData, filter);
 
                if (defaultValue == null){
                    if (filter.Length == 0) {
                        // Check the ShouldSerialize method to see if we need to serialize this string
                        // Only do this for the default filter 
                        shouldSerialize = GetShouldSerializeValue(obj, name);
                    } 
                } 
                else {
                    shouldSerialize = !object.Equals(propValue, defaultValue); 
                }
                */
                bool shouldSerialize = true;
 
                if (filter.Length == 0) {
                    bool useResult; 
                    bool customShouldSerialize = GetShouldSerializeValue(obj, name, out useResult); 

                    if (useResult) { 
                        shouldSerialize = customShouldSerialize;
                    }
                    else {
                        object defaultValue = GetPropertyDefaultValue(propDesc, name, persistData, filter, host); 

                        shouldSerialize = !object.Equals(propValue, defaultValue); 
                    } 
                }
                else { 
                    object defaultValue = GetPropertyDefaultValue(propDesc, name, persistData, filter, host);

                    shouldSerialize = !object.Equals(propValue, defaultValue);
                } 

                // Make sure the current filter's value is not equal to the default value 
                if (shouldSerialize) { 
                    // Grab all the designer expandos on the template and write them out
                    IDictionary expandos = GetExpandos(filter, name, persistData); 

                    // Add the current filter's value to the list
                    stringInnerProperties.Add(new Triplet(filter, persistValue, expandos));
                } 
            }
 
            // Add the rest of the filters' values to the list 
            if (persistData != null) {
                ICollection filteredProperties = persistData.GetPropertyAllFilters(name); 

                foreach (PropertyEntry entry in filteredProperties) {
                    if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0 && (entry is ComplexPropertyEntry)) {
                        ComplexPropertyEntry cpe = (ComplexPropertyEntry)entry; 
                        object o = cpe.Builder.BuildObject();
 
                        Debug.Assert(o is string); 

                        string strValue = o.ToString(); 

                        // Grab all the designer expandos on the template and write them out
                        IDictionary expandos = GetExpandos(entry.Filter, name, persistData);
 
                        stringInnerProperties.Add(new Triplet(entry.Filter, strValue, expandos));
                    } 
                } 
            }
 
            // Write out all the inner string properties
            foreach (Triplet triplet in stringInnerProperties) {
                bool handled = false;
                IDictionary expandos = triplet.Third as IDictionary; 

                // If there's only the non-filtered value, and this is an inner default property, try to persist it as default 
                if ((stringInnerProperties.Count == 1) && (triplet.First.ToString().Length == 0) && ((expandos == null) || (expandos.Count == 0))) { 
                    if (persistenceMode == PersistenceMode.InnerDefaultProperty) {
                        writer.Write(triplet.Second.ToString()); 
                        handled = true;
                    }
                    else if (persistenceMode == PersistenceMode.EncodedInnerDefaultProperty) {
                        HttpUtility.HtmlEncode(triplet.Second.ToString(), writer); 
                        handled = true;
                    } 
                } 

                // Otherwise, it's a normal inner property 
                if (!handled) {
                    string currentFilter = triplet.First.ToString();

                    WriteInnerPropertyBeginTag(writer, currentFilter, name, expandos, true); 
                    writer.Write(triplet.Second.ToString());
                    WriteInnerPropertyEndTag(writer, currentFilter, name); 
                } 
            }
        } 

        private static void SerializeTemplateProperty(object obj, IDesignerHost host, PropertyDescriptor propDesc, ObjectPersistData persistData, TextWriter writer, string filter) {
            string name = propDesc.Name;
 
            // Get the string representation of the current filter's template
            string templateString = String.Empty; 
            ITemplate template = (ITemplate)propDesc.GetValue(obj); 

            // If template is null, do not persist anything. 
            // But don't forget to persist out the other filtered templates
            if (template != null) {
                templateString = SerializeTemplate(template, host);
 
                // Get the string representation of the default filter's template
                string defaultTemplateString = String.Empty; 
 
                if ((filter.Length > 0) && (persistData != null)) {
                    TemplatePropertyEntry defaultEntry = persistData.GetFilteredProperty(String.Empty, name) as TemplatePropertyEntry; 

                    if (defaultEntry != null) {
                        defaultTemplateString = SerializeTemplate(defaultEntry.Builder as ITemplate, host);
                    } 
                }
 
                // Grab all the designer expandos on the template and write them out 
                IDictionary expandos = GetExpandos(filter, name, persistData);
 
                if (((template != null) && (expandos != null && expandos.Count > 0)) || !string.Equals(defaultTemplateString, templateString)) {
                    WriteInnerPropertyBeginTag(writer, filter, name, expandos, false);
                    if (templateString.Length > 0 && !templateString.StartsWith("\r\n", StringComparison.Ordinal)) {
                        writer.WriteLine(); 
                    }
 
                    writer.Write(templateString); 
                    if (templateString.Length > 0 && !templateString.EndsWith("\r\n", StringComparison.Ordinal)) {
                        writer.WriteLine(); 
                    }

                    WriteInnerPropertyEndTag(writer, filter, name);
                } 
            }
 
            if (persistData != null) { 
                // Add the rest of the filters' values to the list
                ICollection filteredProperties = persistData.GetPropertyAllFilters(name); 

                foreach (TemplatePropertyEntry entry in filteredProperties) {
                    if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0) {
                        // Grab all the designer expandos on the template and write them out 
                        IDictionary expandos = GetExpandos(entry.Filter, name, persistData);
 
                        WriteInnerPropertyBeginTag(writer, entry.Filter, name, expandos, false); 

                        string filteredTemplateString = SerializeTemplate((ITemplate)entry.Builder, host); 

                        if (filteredTemplateString != null) {
                            if (!filteredTemplateString.StartsWith("\r\n", StringComparison.Ordinal)) {
                                writer.WriteLine(); 
                            }
 
                            writer.Write(filteredTemplateString); 
                            if (!filteredTemplateString.EndsWith("\r\n", StringComparison.Ordinal)) {
                                writer.WriteLine(); 
                            }

                            WriteInnerPropertyEndTag(writer, entry.Filter, name);
                        } 
                    }
                } 
            } 
        }
 
        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeTemplate2"]/*' />
        /// <devdoc>
        /// Writes the persisted form of the specified template to the specified TextWriter
        /// </devdoc> 
        public static string SerializeTemplate(ITemplate template, IDesignerHost host) {
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture); 
 
            SerializeTemplate(template, writer, host);
            return writer.ToString(); 
        }

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeTemplate3"]/*' />
        /// <devdoc> 
        /// Writes the persisted form of the specified template to the specified TextWriter
        /// </devdoc> 
        public static void SerializeTemplate(ITemplate template, TextWriter writer, IDesignerHost host) { 
            if (template == null) {
                return; 
            }

            if (writer == null) {
                throw new ArgumentNullException("writer"); 
            }
 
            if (template is TemplateBuilder) { 
                writer.Write(((TemplateBuilder)template).Text);
            } 
            else {
                Control container = new Control();
                StringBuilder sb = new StringBuilder();
 
                try {
                    template.InstantiateIn(container); 
                    foreach (Control control in container.Controls) { 
                        sb.Append(SerializeControl(control, host));
                    } 

                    writer.Write(sb.ToString());
                }
                catch (Exception ex) { 
                    Debug.Fail(ex.ToString());
                } 
            } 

            writer.Flush(); 
        }

        /// <devdoc>
        /// Indicates whether we want to persist a blank string in the tag. We only want to do this for some types. 
        /// </devdoc>
        private static bool ShouldPersistBlankValue(object defValue, Type type) { 
            Debug.Assert(defValue != null && type != null, "default value attribute or type can't be null!"); 
            if (type == typeof(string)) {
                return !defValue.Equals(""); 
            }
            else if (type == typeof(Color)) {
                return !(((Color)defValue).IsEmpty);
            } 
            else if (type == typeof(FontUnit)) {
                return !(((FontUnit)defValue).IsEmpty); 
            } 
            else if (type == typeof(Unit)) {
                return !defValue.Equals(Unit.Empty); ; 
            }

            return false;
        } 

        /// <devdoc> 
        /// Write the specified filtered attribute and attribute value 
        /// </devdoc>
        private static void WriteAttribute(TextWriter writer, string filter, string name, string value) { 
            writer.Write(" ");
            if ((filter != null) && (filter.Length > 0)) {
                writer.Write(filter);
                writer.Write(FILTER_SEPARATOR_CHAR); 
            }
 
            writer.Write(name); 
            if (value.IndexOf('"') > -1) {
                writer.Write("=\'"); 
                writer.Write(value);
                writer.Write("\'");
            }
            else { 
                writer.Write("=\"");
                writer.Write(value); 
                writer.Write("\""); 
            }
        } 

        private static void WriteInnerPropertyBeginTag(TextWriter writer, string filter, string name, IDictionary expandos, bool requiresNewLine) {
            writer.Write('<');
            if ((filter != null) && (filter.Length > 0)) { 
                writer.Write(filter);
                writer.Write(FILTER_SEPARATOR_CHAR); 
            } 

            writer.Write(name); 
            if (expandos != null) {
                foreach (DictionaryEntry expando in expandos) {
                    SimplePropertyEntry spe = expando.Value as SimplePropertyEntry;
 
                    if (spe != null) {
                        WriteAttribute(writer, ControlBuilder.DesignerFilter, expando.Key.ToString(), spe.Value.ToString()); 
                    } 
                }
            } 

            if (requiresNewLine) {
                writer.WriteLine('>');
            } 
            else {
                writer.Write('>'); 
            } 
        }
 
        private static void WriteInnerPropertyEndTag(TextWriter writer, string filter, string name) {
            writer.Write("</");
            if ((filter != null) && (filter.Length > 0)) {
                writer.Write(filter); 
                writer.Write(FILTER_SEPARATOR_CHAR);
            } 
 
            writer.Write(name);
            writer.WriteLine('>'); 
        }

        private sealed class WebFormsDesigntimeLicenseContext : DesigntimeLicenseContext {
            private IServiceProvider provider; 

            public WebFormsDesigntimeLicenseContext(IServiceProvider provider) { 
                this.provider = provider; 
            }
 
            public override object GetService(Type serviceClass) {
                if (provider != null) {
                    return provider.GetService(serviceClass);
                } 
                else {
                    return null; 
                } 
            }
        } 

        private enum BindingType {
            None = 0,
            Data = 1, 
            Expression = 2
        } 
 
        private class ReferenceKeyComparer : IEqualityComparer {
            internal static readonly ReferenceKeyComparer Default = new ReferenceKeyComparer(); 

            bool IEqualityComparer.Equals(object x, object y) {
                return Object.ReferenceEquals(x, y);
            } 

            int IEqualityComparer.GetHashCode(object obj) { 
                return obj.GetHashCode(); 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.Design {
    using System; 
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO; 
    using System.Reflection;
    using System.Text; 
    using System.Web; 
    using System.Web.UI;
    using System.Web.UI.HtmlControls; 
    using System.Web.UI.WebControls;
    using AttributeCollection = System.Web.UI.AttributeCollection;
    using System.Collections.Generic;
 
    /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer"]/*' />
    internal static class ControlSerializer { 
        private static readonly object licenseManagerLock = new object(); 

        private const char PERSIST_CHAR = '-'; 

        private const char OM_CHAR = '.';

        private const char FILTER_SEPARATOR_CHAR = ':'; 

        /// <devdoc> 
        /// Whether a string should be serialized as inner default 
        /// </devdoc>
        private static bool CanSerializeAsInnerDefaultString(string filter, string name, Type type, ObjectPersistData persistData, PersistenceMode mode, DataBindingCollection dataBindings, ExpressionBindingCollection expressions) { 
            if (type == typeof(string)) {
                if (filter.Length == 0) {
                    if ((mode == PersistenceMode.InnerDefaultProperty) || (mode == PersistenceMode.EncodedInnerDefaultProperty)) {
                        if (((dataBindings == null) || (dataBindings[name] == null)) && ((expressions == null) || (expressions[name] == null))) { 
                            if (persistData == null) {
                                return true; 
                            } 

                            ICollection filteredProperties = persistData.GetPropertyAllFilters(name); 

                            if (filteredProperties.Count == 0) {
                                // If there wasn't a previously inner default property, this one
                                // can be serialized as inner default 
                                return true;
                            } 
                            else if (filteredProperties.Count == 1) { 
                                // If there is only one previous inner default property and it's the
                                // default filter and it was specified as an inner default, 
                                // it can be serialized as inner default
                                foreach (PropertyEntry entry in filteredProperties) {
                                    if (entry.Filter.Length == 0) {
                                        if (entry is ComplexPropertyEntry) { 
                                            return true;
                                        } 
                                    } 
                                }
                            } 
                        }
                    }
                }
            } 

            return false; 
        } 

        /// <devdoc> 
        /// Converts a dot-syntax name to a dash-syntax name (Font.Bold -> Font-Bold)
        /// </devdoc>
        private static string ConvertObjectModelToPersistName(string objectModelName) {
            return objectModelName.Replace(OM_CHAR, PERSIST_CHAR); 
        }
 
        /// <devdoc> 
        /// Converts a dash-syntax name to a dot-syntax name (Font-Bold -> Font.Bold)
        /// </devdoc> 
        private static string ConvertPersistToObjectModelName(string persistName) {
            return persistName.Replace(PERSIST_CHAR, OM_CHAR);
        }
 
        /// <devdoc>
        /// Gets an designer expando attributes in the ObjectPersistData with the specified name and filter 
        /// </devdoc> 
        private static IDictionary GetExpandos(string filter, string name, ObjectPersistData persistData) {
            IDictionary expandos = null; 

            if (persistData != null) {
                BuilderPropertyEntry currentFilterEntry = persistData.GetFilteredProperty(filter, name) as BuilderPropertyEntry;
 
                if (currentFilterEntry != null) {
                    ObjectPersistData currentFilterPersistData = currentFilterEntry.Builder.GetObjectPersistData(); 
 
                    expandos = currentFilterPersistData.GetFilteredProperties(ControlBuilder.DesignerFilter);
                } 
            }

            return expandos;
        } 

        /// <devdoc> 
        /// Used by ControlDesigner to get a list of persisted attributes 
        /// so it can write directly to the IControlDesignerTag
        /// </devdoc> 
        internal static ArrayList GetControlPersistedAttributes(Control control, IDesignerHost host) {
            ObjectPersistData persistData = null;
            IControlBuilderAccessor cba = (IControlBuilderAccessor)control;
 
            if (cba.ControlBuilder != null) {
                persistData = cba.ControlBuilder.GetObjectPersistData(); 
            } 

            return SerializeAttributes(control, host, String.Empty, persistData, GetCurrentFilter(host), true); 
        }

        /// <devdoc>
        /// Used by ControlDesigner to get a list of persisted attributes 
        /// so it can write directly to the IControlDesignerTag
        /// </devdoc> 
        internal static ArrayList GetControlPersistedAttribute(Control control, PropertyDescriptor propDesc, IDesignerHost host) { 
            ObjectPersistData persistData = null;
            IControlBuilderAccessor cba = (IControlBuilderAccessor)control; 

            if (cba.ControlBuilder != null) {
                persistData = cba.ControlBuilder.GetObjectPersistData();
            } 

            string prefix = String.Empty; 
            object propValue = control; 
            ArrayList attributes = new ArrayList();
 
            if (propDesc.SerializationVisibility == DesignerSerializationVisibility.Content) {
                propValue = propDesc.GetValue(control);
                prefix = propDesc.Name;
                SerializeAttributesRecursive(propValue, host, prefix, persistData, GetCurrentFilter(host), attributes, null, null, true); 
            }
            else { 
                DataBindingCollection dataBindings = ((IDataBindingsAccessor)control).DataBindings; 
                ExpressionBindingCollection expressions = ((IExpressionsAccessor)control).Expressions;
 
                SerializeAttribute(propValue, propDesc, dataBindings, expressions, host, prefix, persistData, GetCurrentFilter(host), attributes, true);
            }

            return attributes; 
        }
 
        /// <devdoc> 
        /// Returns the default value for a property given a PropertyDescriptor and ObjectPersistData
        /// Checks the DefaultValueAttribute and String.Empty filter for the default value 
        /// </devdoc>
        private static object GetPropertyDefaultValue(PropertyDescriptor propDesc, string name, ObjectPersistData defaultPropertyEntries, string filter, IDesignerHost host) {
            if ((filter.Length > 0) && (defaultPropertyEntries != null)) {
                string objectModelName = ConvertPersistToObjectModelName(name); 
                IFilterResolutionService filterResolutionService = null;
                ServiceContainer container = new ServiceContainer(); 
 
                if (host != null) {
                    filterResolutionService = (IFilterResolutionService)host.GetService(typeof(IFilterResolutionService)); 
                    if (filterResolutionService != null)
                        container.AddService(typeof(IFilterResolutionService), filterResolutionService);

                    IThemeResolutionService themeResolutionService = (IThemeResolutionService)host.GetService(typeof(IThemeResolutionService)); 

                    if (themeResolutionService != null) 
                        container.AddService(typeof(IThemeResolutionService), themeResolutionService); 
                }
 
                // Get the property entry from the default filter (String.Empty), if there is one
                PropertyEntry entry = null;

#if ORCAS 
                // Find the default filter using the device filter resolution service
                // Just enumerate the properties in order since they should already be in 
                // most-specific to least-specific order 
                if (filterResolutionService != null) {
                    ICollection allProps = defaultPropertyEntries.GetPropertyAllFilters(objectModelName); 

                    foreach (PropertyEntry propertyEntry in allProps) {
                        if (!String.Equals(filterResolutionService.CurrentFilter, propertyEntry.Filter, StringComparison.InvariantCultureIgnoreCase) && filterResolutionService.EvaluateFilter(propertyEntry.Filter)) {
                            entry = propertyEntry; 
                            break;
                        } 
                    } 
                }
                else { 
                    // Just use the default filter if there is no filter resolution service
                    entry = defaultPropertyEntries.GetFilteredProperty(String.Empty, objectModelName);
                }
#else 
                // Just use the default filter if there is no filter resolution service
                entry = defaultPropertyEntries.GetFilteredProperty(String.Empty, objectModelName); 
#endif 

 
                if (entry is SimplePropertyEntry) {
                    return ((SimplePropertyEntry)entry).Value;
                }
                else if (entry is BoundPropertyEntry) { 
                    // Trim these values since we do it in ControlBuilder too
                    string expression = ((BoundPropertyEntry)entry).Expression.Trim(); 
                    string expressionPrefix = ((BoundPropertyEntry)entry).ExpressionPrefix.Trim(); 

                    if (expressionPrefix.Length > 0) { 
                        expression = expressionPrefix + ":" + expression;
                    }

                    return expression; 
                }
                else if (entry is ComplexPropertyEntry) { 
                    ControlBuilder controlBuilder = ((ComplexPropertyEntry)entry).Builder; 

                    Debug.Assert(controlBuilder.ServiceProvider == null); 
                    controlBuilder.SetServiceProvider(container);

                    object o = null;
 
                    try {
                        o = controlBuilder.BuildObject(); 
                    } 
                    finally {
                        controlBuilder.SetServiceProvider(null); 
                    }
                    return o;
                }
                else if (entry == null) { 
                }
                else { 
                    Debug.Fail("Unexpected PropertyEntry type in GetPropertyDefaultValue : " + entry.GetType()); 
                }
            } 

            // If there was no default filter entry, use the default value attriubte
            DefaultValueAttribute defValAttr = (DefaultValueAttribute)propDesc.Attributes[typeof(DefaultValueAttribute)];
 
            if (defValAttr != null) {
                return defValAttr.Value; 
            } 

            return null; 
        }

        /// <devdoc>
        /// Get a string containing the register directives 
        /// </devdoc>
        private static string GetDirectives(IDesignerHost designerHost) { 
            Debug.Assert(designerHost != null); 

            string directives = String.Empty; 
            WebFormsReferenceManager referenceManager = null;

            if (designerHost.RootComponent != null) {
                WebFormsRootDesigner rootDesigner = designerHost.GetDesigner(designerHost.RootComponent) as WebFormsRootDesigner; 

                if (rootDesigner != null) { 
                    referenceManager = rootDesigner.ReferenceManager; 
                }
            } 

            if (referenceManager == null) {
#pragma warning disable 618
                IWebFormReferenceManager oldReferenceManager = (IWebFormReferenceManager)designerHost.GetService(typeof(IWebFormReferenceManager)); 

                if (oldReferenceManager != null) { 
                    directives = oldReferenceManager.GetRegisterDirectives(); 
                }
#pragma warning restore 618 
            }
            else {
                StringBuilder sb = new StringBuilder();
 
                foreach (string s in referenceManager.GetRegisterDirectives()) {
                    sb.Append(s); 
                } 

                directives = sb.ToString(); 
            }

            return directives;
        } 

        private static string GetCurrentFilter(IDesignerHost host) { 
#if ORCAS 
            string filter = String.Empty;
 
            if (host != null) {
                IFilterResolutionService filterResolutionService = (IFilterResolutionService)host.GetService(typeof(IFilterResolutionService));

                if (filterResolutionService != null) { 
                    filter = filterResolutionService.CurrentFilter;
                } 
            } 

            return filter; 
#else
            // Only the default filter is supported
            return String.Empty;
#endif 
        }
 
        /// <devdoc> 
        /// Gets the string to persist for the specified value for the specified property.
        /// 
        /// COMMENTS NOTE:
        /// More details available in VSWhidbey 420273. Theoretically the serializer should
        /// HTML encode all attribute values (except for top-level values, since those are
        /// done by the tool) to ensure valid content in the ASPX. The runtime HTML decodes 
        /// all attributes at parse time. Unfortunately the tool does not decode attribute
        /// values properly, and the fix for the tool is too risky, so we can't change this 
        /// just yet. 
        /// </devdoc>
        private static string GetPersistValue(PropertyDescriptor propDesc, Type propType, object propValue, BindingType bindingType, bool topLevelInDesigner) { 
            string persistValue = String.Empty;

            if (bindingType == BindingType.Data) {
                persistValue = "<%# " + propValue.ToString() + " %>"; 
                // See note above for why this is commented out
                //if (topLevelInDesigner) { 
                //    persistValue = "<%# " + propValue.ToString() + " %>"; 
                //}
                //else { 
                //    persistValue = "<%# " + HttpUtility.HtmlEncode(propValue.ToString()) + " %>";
                //}
            }
            else if (bindingType == BindingType.Expression) { 
                persistValue = "<%$ " + propValue.ToString() + " %>";
                // See note above for why this is commented out 
                //if (topLevelInDesigner) { 
                //    persistValue = "<%$ " + propValue.ToString() + " %>";
                //} 
                //else {
                //    persistValue = "<%$ " + HttpUtility.HtmlEncode(propValue.ToString()) + " %>";
                //}
            } 
            else if (propType.IsEnum) {
                persistValue = Enum.Format(propType, propValue, "G"); 
            } 
            else if (propType == typeof(string)) {
                if (propValue != null) { 
                    persistValue = propValue.ToString();
                    if (!topLevelInDesigner) {
                        persistValue = HttpUtility.HtmlEncode(persistValue);
                    } 
                }
            } 
            else { 
                // Use the TypeConverter to get the string value of the object
                TypeConverter converter = null; 

                if (propDesc != null) {
                    converter = propDesc.Converter;
                } 
                else {
                    converter = TypeDescriptor.GetConverter(propValue); 
                } 

                if (converter != null) { 
                    persistValue = converter.ConvertToInvariantString(null, propValue);
                }
                else {
                    persistValue = propValue.ToString(); 
                }
 
                if (!topLevelInDesigner) { 
                    persistValue = HttpUtility.HtmlEncode(persistValue);
                } 
            }

            return persistValue;
        } 

        /// <devdoc> 
        /// Checks the ShouldSerialize method on the specified object's specified property name 
        /// </devdoc>
        private static bool GetShouldSerializeValue(object obj, string name, out bool useResult) { 
            useResult = false;

            Type objType = obj.GetType();
            BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance; 
            PropertyInfo propInfo = objType.GetProperty(name, flags);
 
            Debug.Assert(propInfo != null, "Couldn't get property '" + name + "' for type '" + objType + "'"); 
            flags |= BindingFlags.NonPublic;
 
            MethodInfo shouldSerializeMethod = propInfo.DeclaringType.GetMethod("ShouldSerialize" + name, flags);

            if (shouldSerializeMethod != null) {
                useResult = true; 

                object o = shouldSerializeMethod.Invoke(obj, new object[0]); 
 
                Debug.Assert((o != null) && (o is bool));
                return (bool)o; 
            }

            return true;
        } 

        /// <devdoc> 
        /// Gets the tag name for the specified type using the IWebFormsReferenceManager 
        /// </devdoc>
        private static string GetTagName(Type type, IDesignerHost host) { 
            string tagName = String.Empty;
            string tagPrefix = String.Empty;
            WebFormsReferenceManager referenceManager = null;
 
            if (host.RootComponent != null) {
                WebFormsRootDesigner rootDesigner = host.GetDesigner(host.RootComponent) as WebFormsRootDesigner; 
 
                if (rootDesigner != null) {
                    referenceManager = rootDesigner.ReferenceManager; 
                }
            }

            if (referenceManager == null) { 
#pragma warning disable 618
                IWebFormReferenceManager oldReferenceManager = (IWebFormReferenceManager)host.GetService(typeof(IWebFormReferenceManager)); 
 
                Debug.Assert(oldReferenceManager != null, "Did not get back IWebFormReferenceManager service from host.");
                if (oldReferenceManager != null) { 
                    tagPrefix = oldReferenceManager.GetTagPrefix(type);
                }
#pragma warning restore 618
            } 
            else {
                tagPrefix = referenceManager.GetTagPrefix(type); 
            } 

            // If there wasn't an existing tag prefix, add a new one to the document 
            if (String.IsNullOrEmpty(tagPrefix)) {
                tagPrefix = referenceManager.RegisterTagPrefix(type);
                Debug.Assert(!String.IsNullOrEmpty(tagPrefix), "Did not expect empty tag prefix from ReferenceManager.RegisterTagPrefix().");
            } 

            if ((tagPrefix != null) && (tagPrefix.Length != 0)) { 
                tagName = tagPrefix + ":" + type.Name; 
            }
 
            if (tagName.Length == 0) {
                tagName = type.FullName;
            }
 
            return tagName;
        } 
 
        internal static Control DeserializeControlInternal(string text, IDesignerHost host, bool applyTheme) {
            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            if ((text == null) || (text.Length == 0)) { 
                throw new ArgumentNullException("text");
            } 
 
            string directives = GetDirectives(host);
 
            if ((directives != null) && (directives.Length > 0)) {
                text = directives + text;
            }
 
            DesignTimeParseData parseData = new DesignTimeParseData(host, text, GetCurrentFilter(host));
 
            parseData.ShouldApplyTheme = applyTheme; 
            parseData.DataBindingHandler = GlobalDataBindingHandler.Handler;
 
            Control parsedControl = null;

            lock (typeof(LicenseManager)) {
                LicenseContext originalContext = LicenseManager.CurrentContext; 

                try { 
                    LicenseManager.CurrentContext = new WebFormsDesigntimeLicenseContext(host); 
                    LicenseManager.LockContext(licenseManagerLock);
                    parsedControl = DesignTimeTemplateParser.ParseControl(parseData); 
                }
                catch (TargetInvocationException e) {
                    Debug.Assert(e.InnerException != null);
                    throw e.InnerException; 
                }
                finally { 
                    LicenseManager.UnlockContext(licenseManagerLock); 
                    LicenseManager.CurrentContext = originalContext;
                } 
            }

            return parsedControl;
        } 

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.DeserializeControl1"]/*' /> 
        /// <devdoc> 
        /// Creates a control instance from the specified text using the filtered property values
        /// </devdoc> 
        public static Control DeserializeControl(string text, IDesignerHost host) {
            return DeserializeControlInternal(text, host, false);
        }
 
        public static Control[] DeserializeControls(string text, IDesignerHost host) {
            return DeserializeControlsInternal(text, host, null); 
        } 

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.DeserializeControls1"]/*' /> 
        /// <devdoc>
        /// Creates a set of controls from the specified text using the default filtered values
        /// </devdoc>
        internal static Control[] DeserializeControlsInternal(string text, IDesignerHost host, List<Triplet> userControlRegisterEntries) { 
            if (host == null) {
                throw new ArgumentNullException("host"); 
            } 

            if ((text == null) || (text.Length == 0)) { 
                throw new ArgumentNullException("text");
            }

            string directives = GetDirectives(host); 

            if ((directives != null) && (directives.Length > 0)) { 
                text = directives + text; 
            }
 
            DesignTimeParseData parseData = new DesignTimeParseData(host, text, GetCurrentFilter(host));

            parseData.DataBindingHandler = GlobalDataBindingHandler.Handler;
 
            Control[] parsedControls = null;
 
            lock (typeof(LicenseManager)) { 
                LicenseContext originalContext = LicenseManager.CurrentContext;
 
                try {
                    LicenseManager.CurrentContext = new WebFormsDesigntimeLicenseContext(host);
                    LicenseManager.LockContext(licenseManagerLock);
                    parsedControls = DesignTimeTemplateParser.ParseControls(parseData); 
                }
                catch (TargetInvocationException e) { 
                    Debug.Assert(e.InnerException != null); 
                    throw e.InnerException;
                } 
                finally {
                    LicenseManager.UnlockContext(licenseManagerLock);
                    LicenseManager.CurrentContext = originalContext;
                } 
            }
 
            if (userControlRegisterEntries != null && parseData.UserControlRegisterEntries != null) { 
                foreach (Triplet triplet in parseData.UserControlRegisterEntries) {
                    userControlRegisterEntries.Add(triplet); 
                }
            }

            return parsedControls; 
        }
 
        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.DeserializeTemplate"]/*' /> 
        /// <devdoc>
        /// Creates an ITemplate from the specified text 
        /// </devdoc>
        public static ITemplate DeserializeTemplate(string text, IDesignerHost host) {
            if (host == null) {
                throw new ArgumentNullException("host"); 
            }
 
            if ((text == null) || (text.Length == 0)) { 
                return null;
            } 

            string parseText = text;
            string directives = GetDirectives(host);
 
            if ((directives != null) && (directives.Length > 0)) {
                parseText = directives + text; 
            } 

            DesignTimeParseData parseData = new DesignTimeParseData(host, parseText); 

            parseData.DataBindingHandler = GlobalDataBindingHandler.Handler;

            ITemplate parsedTemplate = null; 

            lock (typeof(LicenseManager)) { 
                LicenseContext originalContext = LicenseManager.CurrentContext; 

                try { 
                    LicenseManager.CurrentContext = new WebFormsDesigntimeLicenseContext(host);
                    LicenseManager.LockContext(licenseManagerLock);
                    parsedTemplate = DesignTimeTemplateParser.ParseTemplate(parseData);
                } 
                catch (TargetInvocationException e) {
                    Debug.Assert(e.InnerException != null); 
                    throw e.InnerException; 
                }
                finally { 
                    LicenseManager.UnlockContext(licenseManagerLock);
                    LicenseManager.CurrentContext = originalContext;
                }
            } 

            if (parsedTemplate != null) { 
                // The parsed template contains all the text sent to the parser 
                // which includes the register directives.
                // We don't want to have these directives end up in the template 
                // text. Unfortunately, theres no way to pass them as a separate
                // text block to the parser, so we'll have to do some fixup here.
                Debug.Assert(parsedTemplate is TemplateBuilder, "Unexpected type of ITemplate implementation.");
                if (parsedTemplate is TemplateBuilder) { 
                    ((TemplateBuilder)parsedTemplate).Text = text;
                } 
            } 

            return parsedTemplate; 
        }

        private static void SerializeAttribute(object obj, PropertyDescriptor propDesc, DataBindingCollection dataBindings, ExpressionBindingCollection expressions, IDesignerHost host, string prefix, ObjectPersistData persistData, string filter, ArrayList attributes, bool topLevelInDesigner) {
            // Skip design-time only properties such as DefaultModifiers and Name 
            DesignOnlyAttribute designOnlyAttr = (DesignOnlyAttribute)propDesc.Attributes[typeof(DesignOnlyAttribute)];
 
            if ((designOnlyAttr != null) && designOnlyAttr.IsDesignOnly) { 
                return;
            } 

            string propName = propDesc.Name;
            Type propType = propDesc.PropertyType;
            PersistenceMode persistenceMode = ((PersistenceModeAttribute)propDesc.Attributes[typeof(PersistenceModeAttribute)]).Mode; 

            bool isDataBound = (dataBindings != null && dataBindings[propName] != null); 
            bool isExpressionBound = (expressions != null && expressions[propName] != null); 

            // Skip anything that should be hidden and is not data-bound or expression-bound 
            if (!isDataBound && !isExpressionBound && (propDesc.SerializationVisibility == DesignerSerializationVisibility.Hidden)) {
                return;
            }
 
            // Persist attributes and databound inner-property strings as attributes, skip all others
            if ((persistenceMode != PersistenceMode.Attribute) && (!isDataBound || !isExpressionBound || (propType != typeof(string))) && 
                // Maybe persist defaultinner and encoded default inner as attributes 
                ((persistenceMode == PersistenceMode.InnerProperty) || (propType != typeof(string)))) {
                return; 
            }

            string persistName = String.Empty;
 
            if (prefix.Length > 0) {
                persistName = prefix + "-" + propName; 
            } 
            else {
                persistName = propName; 
            }

            if (propDesc.SerializationVisibility == DesignerSerializationVisibility.Content) {
                object propValue = propDesc.GetValue(obj); 

                // Recursively persist dash-syntax properties, re-using the same default properties persist data, 
                // since dash-syntax properties are stored in the same control builder 
                SerializeAttributesRecursive(propValue, host, persistName, persistData, filter, attributes, dataBindings, expressions, topLevelInDesigner);
            } 
            else {
                // Skip read-only properties unless the object is an IAttributeAccessor, except
                // when there is already a valid entry in the attribute collection.
                IAttributeAccessor attributeAccessor = obj as IAttributeAccessor; 
                if (propDesc.IsReadOnly && ((attributeAccessor == null) || (attributeAccessor.GetAttribute(persistName) == null))) {
                    return; 
                } 

                string objectModelName = ConvertPersistToObjectModelName(persistName); 

                // If the property is not filterable, don't put a filter on the value
                if (!FilterableAttribute.IsPropertyFilterable(propDesc)) {
                    filter = String.Empty; 
                }
 
                // Don't persist inner defaults as attributes. 
                if (CanSerializeAsInnerDefaultString(filter, objectModelName, propType, persistData, persistenceMode, dataBindings, expressions)) {
                    // Make sure we remove these from the attributes if we are persisting top-level 
                    // attributes in the designer
                    if (topLevelInDesigner) {
                        attributes.Add(new Triplet(filter, persistName, null));
                    } 

                    return; 
                } 

                /* 
                bool shouldSerialize = true;

                object defaultValue = GetPropertyDefaultValue(propDesc, persistName, persistData, filter);
 
                if (defaultValue == null){
                    if (filter.Length == 0) { 
                        // Check the ShouldSerialize method to see if we need to serialize this string 
                        // Only do this for the default filter
                        shouldSerialize = GetShouldSerializeValue(obj, propName); 
                    }
                }
                else {
                    shouldSerialize = !object.Equals(propValue, defaultValue); 
                }
                */ 
                bool shouldSerialize = true; 
                object defaultValue = null;
 
                object propValue = propDesc.GetValue(obj);

                // Check if the property is in the databindings collection
                BindingType bindingType = BindingType.None; 

                if (dataBindings != null) { 
                    DataBinding binding = dataBindings[objectModelName]; 

                    if (binding != null) { 
                        propValue = binding.Expression;
                        bindingType = BindingType.Data;
                    }
                } 

                if (bindingType == BindingType.None) { 
                    if (expressions != null) { 
                        ExpressionBinding binding = expressions[objectModelName];
 
                        if (binding != null) {
                            // If the binding is not auto-generated, persist it as an explicit expression.
                            // Otherwise, do nothing and pretend as though it was a regular property so
                            // that we end up persisting the live value of the property. 
                            if (!binding.Generated) {
                                propValue = binding.ExpressionPrefix + ":" + binding.Expression; 
                                bindingType = BindingType.Expression; 
                            }
                        } 
                    }
                    else if (persistData != null) {
                        // We need to persist the expression for this property if it was expression-bound
                        // but the object isn't an IExpressionsAccessor 
                        BoundPropertyEntry bpe = persistData.GetFilteredProperty(filter, propName) as BoundPropertyEntry;
                        if (bpe != null) { 
                            // If the binding is not auto-generated, persist it as an explicit expression. 
                            // Otherwise, do nothing and pretend as though it was a regular property so
                            // that we end up persisting the live value of the property. 
                            if (!bpe.Generated) {
                                // Don't use the expression binding if we already have a value for
                                // this attribute. This is because if the user changed the value of
                                // a property for an object that is not an IExpressionAccessor, we 
                                // want to persist their new value (which would be a simple value, not
                                // an expression), not the old expression. 
                                defaultValue = GetPropertyDefaultValue(propDesc, persistName, persistData, filter, host); 
                                if (Object.Equals(propValue, defaultValue)) {
                                    propValue = bpe.ExpressionPrefix + ":" + bpe.Expression; 
                                    bindingType = BindingType.Expression;
                                }
                            }
                        } 
                    }
                } 
 
                if (filter.Length == 0) {
                    bool useResult = false; 
                    bool customShouldSerialize = false;

                    // If it wasn't data or expression-bound, check the ShouldSerialize value and default values to
                    // determine if we want to serialize.  It was bound, always serialize. 
                    if (bindingType == BindingType.None) {
                        customShouldSerialize = GetShouldSerializeValue(obj, propName, out useResult); 
                    } 

                    if (useResult) { 
                        shouldSerialize = customShouldSerialize;
                    }
                    else {
                        defaultValue = GetPropertyDefaultValue(propDesc, persistName, persistData, filter, host); 
                        shouldSerialize = !object.Equals(propValue, defaultValue);
                    } 
                } 
                else {
                    defaultValue = GetPropertyDefaultValue(propDesc, persistName, persistData, filter, host); 
                    shouldSerialize = !object.Equals(propValue, defaultValue);
                }

                // If the value of the property is non-null and different from the default value 
                if (shouldSerialize) {
                    string persistValue = GetPersistValue(propDesc, propType, propValue, bindingType, topLevelInDesigner); 
 
                    if (topLevelInDesigner) {
                        // HACK: Trident will remove attributes with a String.Empty value, 
                        //       so stick in a single space string
                        if ((defaultValue != null) && ((persistValue == null) || (persistValue.Length == 0)) && ShouldPersistBlankValue(defaultValue, propType)) {
                            persistValue = String.Empty;
                        } 
                    }
 
                    // 

                    if ((persistValue != null) && (!propType.IsArray || persistValue.Length > 0)) { 
                        attributes.Add(new Triplet(filter, persistName, persistValue));
                    }
                    else if (topLevelInDesigner) {
                        attributes.Add(new Triplet(filter, persistName, null)); 
                    }
                } 
                else if (topLevelInDesigner) { 
                    attributes.Add(new Triplet(filter, persistName, null));
                } 

                // Add properties from all other filters
                if (persistData != null) {
                    ICollection filteredProperties = persistData.GetPropertyAllFilters(objectModelName); 

                    foreach (PropertyEntry entry in filteredProperties) { 
                        if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0) { 
                            string persistValue = String.Empty;
 
                            if (entry is SimplePropertyEntry) {
                                SimplePropertyEntry spe = (SimplePropertyEntry)entry;

                                if (spe.UseSetAttribute) { 
                                    persistValue = spe.Value.ToString();
                                } 
                                else { 
                                    persistValue = GetPersistValue(propDesc, entry.Type, spe.Value, BindingType.None, topLevelInDesigner);
                                } 
                            }
                            else if (entry is BoundPropertyEntry) {
                                BoundPropertyEntry bpe = (BoundPropertyEntry)entry;
                                if (bpe.Generated) { 
                                    continue;
                                } 
                                string expression = bpe.Expression.Trim(); 

                                bindingType = BindingType.Data; 

                                string expressionPrefix = bpe.ExpressionPrefix;

                                if (expressionPrefix.Length > 0) { 
                                    expression = expressionPrefix + ":" + expression;
                                    bindingType = BindingType.Expression; 
                                } 

                                persistValue = GetPersistValue(propDesc, entry.Type, expression, bindingType, topLevelInDesigner); 
                            }
                            else if (entry is ComplexPropertyEntry) {
                                ComplexPropertyEntry cpe = (ComplexPropertyEntry)entry;
                                object o = cpe.Builder.BuildObject(); 

                                Debug.Assert(o is string); 
                                persistValue = (string)o; 
                            }
                            else { 
                                Debug.Fail("Unexpected PropertyEntry type!");
                            }

                            attributes.Add(new Triplet(entry.Filter, persistName, persistValue)); 
                        }
                    } 
                } 
            }
        } 

        private static void SerializeAttributes(object obj, IDesignerHost host, string prefix, ObjectPersistData persistData, TextWriter writer, string filter) {
            ArrayList attributes = SerializeAttributes(obj, host, prefix, persistData, filter, false);
 
            // Write out all attributes accumulated
            foreach (Triplet triplet in attributes) { 
                WriteAttribute(writer, triplet.First.ToString(), triplet.Second.ToString(), triplet.Third.ToString()); 
            }
        } 

        /// <devdoc>
        /// Persists all the attributes of the specified object
        /// topLevelInDesigner is used by the ControlDesigner to include entries for all possible attributes 
        ///     attrs with values get the value
        ///     attrs with no value get String.Empty 
        ///     special attributes (e.g. Color, string, DateTime) get a single space string since it would get eaten by Trident otherwise 
        /// </devdoc>
        private static ArrayList SerializeAttributes(object obj, IDesignerHost host, string prefix, ObjectPersistData persistData, string filter, bool topLevelInDesigner) { 
            // Fill this with Triplets of filter, attribute name, and attribute value
            ArrayList attributes = new ArrayList();

            // Get all properties 
            SerializeAttributesRecursive(obj, host, prefix, persistData, filter, attributes, null, null, topLevelInDesigner);
 
            // Serialize all bound properties that are persisted as hyphenated names, for 
            // example, ItemStyle-BackColor="<%$ foo:bar %>"
            if (persistData != null) { 
                foreach (PropertyEntry entry in persistData.AllPropertyEntries) {
                    BoundPropertyEntry bpe = entry as BoundPropertyEntry;
                    if (bpe != null && !bpe.Generated) {
                        string[] parts = bpe.Name.Split(OM_CHAR); 
                        if (parts.Length > 1) {
                            object propValue = obj; 
                            foreach (string part in parts) { 
                                PropertyDescriptor propDesc = TypeDescriptor.GetProperties(propValue)[part];
                                // Ignore non-existent properties (i.e. expandos) 
                                if (propDesc == null) {
                                    break;
                                }
                                PersistenceModeAttribute mode = propDesc.Attributes[typeof(PersistenceModeAttribute)] as PersistenceModeAttribute; 
                                if (mode != PersistenceModeAttribute.Attribute) {
                                    // Serialize the value 
                                    string expressionValue = (String.IsNullOrEmpty(bpe.ExpressionPrefix) ? bpe.Expression : (bpe.ExpressionPrefix + ":" + bpe.Expression)); 
                                    string persistValue = GetPersistValue(
                                        TypeDescriptor.GetProperties(bpe.PropertyInfo.DeclaringType)[bpe.PropertyInfo.Name], 
                                        bpe.Type,
                                        expressionValue,
                                        (String.IsNullOrEmpty(bpe.ExpressionPrefix) ? BindingType.Data : BindingType.Expression),
                                        topLevelInDesigner); 
                                    attributes.Add(new Triplet(bpe.Filter, ConvertObjectModelToPersistName(bpe.Name), persistValue));
                                    break; 
                                } 
                                propValue = propDesc.GetValue(propValue);
                            } 
                        }
                    }
                }
            } 

            // Get all expandos from the control for this filter 
            if (obj is Control) { 
                AttributeCollection expandos = null;
                if (obj is WebControl) { 
                    expandos = ((WebControl)obj).Attributes;
                }
                else if (obj is HtmlControl) {
                    expandos = ((HtmlControl)obj).Attributes; 
                }
                else if (obj is UserControl) { 
                    expandos = ((UserControl)obj).Attributes; 
                }
 
                if (expandos != null) {
                    foreach (string key in expandos.Keys) {
                        string value = expandos[key];
 
                        bool persist = false;
 
                        if (value != null) { 

                            // If this expando matches a property that is read-write, ignore the expando 
                            bool hasWriteableProperty = false;
                            string objectModelName = ConvertPersistToObjectModelName(key);
                            object realTarget;
                            PropertyDescriptor propDesc = ControlDesigner.GetComplexProperty(obj, objectModelName, out realTarget); 
                            if (propDesc != null && !propDesc.IsReadOnly) {
                                hasWriteableProperty = true; 
                            } 

                            if (!hasWriteableProperty) { 
                                if (filter.Length == 0) {
                                    persist = true;
                                }
                                else { 
                                    PropertyEntry entry = null;
                                    if (persistData != null) { 
                                        entry = persistData.GetFilteredProperty(String.Empty, key) as PropertyEntry; 
                                    }
 
                                    if (entry is SimplePropertyEntry) {
                                        persist = !value.Equals(((SimplePropertyEntry)entry).PersistedValue);
                                    }
                                    else if (entry is BoundPropertyEntry) { 
                                        string expression = ((BoundPropertyEntry)entry).Expression;
                                        string expressionPrefix = ((BoundPropertyEntry)entry).ExpressionPrefix; 
 
                                        if (expressionPrefix.Length > 0) {
                                            expression = expressionPrefix + ":" + expression; 
                                        }

                                        persist = !value.Equals(expression);
                                    } 
                                    else if (entry == null) {
                                        persist = true; 
                                    } 
                                    else {
                                        Debug.Fail("Unexpected PropertyEntry type!"); 
                                    }
                                }
                            }
 
                            if (persist) {
                                attributes.Add(new Triplet(filter, key, value)); 
                            } 
                        }
                    } 
                }
            }

            if (persistData != null) { 
                if (!String.IsNullOrEmpty(persistData.ResourceKey)) {
                    attributes.Add(new Triplet("meta", "resourceKey", persistData.ResourceKey)); 
                } 
                if (persistData.Localize == false) {
                    attributes.Add(new Triplet("meta", "localize", "false")); 
                }

                // Get expandos for all other filters
                foreach (PropertyEntry entry in persistData.AllPropertyEntries) { 
                    if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0) {
                        string persistValue = String.Empty; 
 
                        if (entry is SimplePropertyEntry) {
                            SimplePropertyEntry spe = (SimplePropertyEntry)entry; 

                            if (spe.UseSetAttribute) {
                                attributes.Add(new Triplet(entry.Filter, ConvertObjectModelToPersistName(entry.Name), spe.Value.ToString()));
                            } 
                        }
                        else if (entry is BoundPropertyEntry) { 
                            BoundPropertyEntry bpe = (BoundPropertyEntry)entry; 

                            if (bpe.UseSetAttribute) { 
                                string expression = ((BoundPropertyEntry)entry).Expression;
                                string expressionPrefix = ((BoundPropertyEntry)entry).ExpressionPrefix;

                                if (expressionPrefix.Length > 0) { 
                                    expression = expressionPrefix + ":" + expression;
                                } 
 
                                attributes.Add(new Triplet(entry.Filter, ConvertObjectModelToPersistName(entry.Name), expression));
                            } 
                        }
                    }
                }
            } 

            if ((obj is Control) && (persistData != null) && (host.GetDesigner((Control)obj) == null)) { 
                foreach (EventEntry entry in persistData.EventEntries) { 
                    attributes.Add(new Triplet(String.Empty, "On" + entry.Name, entry.HandlerMethodName));
                } 
            }

            return attributes;
        } 

        /// <devdoc> 
        /// Helper method for persisting attributes 
        /// </devdoc>
        private static void SerializeAttributesRecursive(object obj, IDesignerHost host, string prefix, ObjectPersistData persistData, string filter, ArrayList attributes, DataBindingCollection dataBindings, ExpressionBindingCollection expressions, bool topLevelInDesigner) { 
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj);

            if (obj is IDataBindingsAccessor) {
                dataBindings = ((IDataBindingsAccessor)obj).DataBindings; 
            }
 
            if (obj is Control) { 
                // Force ChildControl Creation (needed for controls like CreateUserWizard which change the persistence)
                try { 
                    ControlCollection controls = ((Control)obj).Controls;
                }
                catch (Exception ex) {
                    // Log the error with the IComponentDesignerDebugService 
                    IComponentDesignerDebugService debugService = host.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService;
                    if (debugService != null) { 
                        debugService.Fail(ex.Message); 
                    }
                } 
            }

            if (obj is IExpressionsAccessor) {
                expressions = ((IExpressionsAccessor)obj).Expressions; 
            }
 
            for (int i = 0; i < properties.Count; i++) { 
                try {
                    SerializeAttribute(obj, properties[i], dataBindings, expressions, host, prefix, persistData, filter, attributes, topLevelInDesigner); 
                }
                catch (Exception e) {
                    if (host != null) {
                        // Log the error with the IComponentDesignerDebugService 
                        IComponentDesignerDebugService debugService = host.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService;
                        if (debugService != null) { 
                            debugService.Fail(e.Message); 
                        }
                    } 
                }
            }
        }
 
        /// <devdoc>
        /// Helper method for persisting out collections 
        /// </devdoc> 
        private static void SerializeCollectionProperty(object obj, IDesignerHost host, PropertyDescriptor propDesc, ObjectPersistData persistData, PersistenceMode persistenceMode, TextWriter writer, string filter) {
            string name = propDesc.Name; 
            bool persistNewCollection = false;

            // Get the number of items in the current filter's collection
            ICollection collection = propDesc.GetValue(obj) as ICollection; 
            int count = 0;
 
            if (collection != null) { 
                count = collection.Count;
            } 

            // Get the number of items in the default filter's collection
            int defaultCount = 0;
            ObjectPersistData defaultPersistData = null; 

            if (persistData != null) { 
                ComplexPropertyEntry defaultEntry = persistData.GetFilteredProperty(String.Empty, name) as ComplexPropertyEntry; 

                if (defaultEntry != null) { 
                    defaultPersistData = defaultEntry.Builder.GetObjectPersistData();
                    defaultCount = defaultPersistData.CollectionItems.Count;
                }
            } 

            // Make sure we persist the default filter's collection 
            if (filter.Length == 0) { 
                persistNewCollection = true;
            } 
            else if (persistData != null) {
                // If the collection was originally a seperate collection, persist it like that
                if ((persistData.GetFilteredProperty(filter, name) as ComplexPropertyEntry) != null) {
                    persistNewCollection = true; 
                }
                    // If the collection are different sizes, we need to persist a new collection 
                else if (defaultCount != count) { 
                    persistNewCollection = true;
                } 
                    // Also check if all the types in the collections are in the same order and of the same type
                    // if not, we also need a new collection
                else if (defaultPersistData != null) {
                    IEnumerator enumerator = collection.GetEnumerator(); 
                    IEnumerator defaultCollectionEnumerator = defaultPersistData.CollectionItems.GetEnumerator();
 
                    while (enumerator.MoveNext()) { 
                        defaultCollectionEnumerator.MoveNext();
                        Debug.Assert(defaultCollectionEnumerator.Current is ComplexPropertyEntry && ((ComplexPropertyEntry)defaultCollectionEnumerator.Current).IsCollectionItem); 

                        ComplexPropertyEntry cipe = (ComplexPropertyEntry)defaultCollectionEnumerator.Current;

                        if (enumerator.Current.GetType() != cipe.Builder.ControlType) { 
                            persistNewCollection = true;
                            break; 
                        } 
                    }
                } 
            }

            // Used to remember if we already persisted the default filter's collection
            bool skipDefaultFilter = false; 

            // Store all the persisted collection texts 
            ArrayList collections = new ArrayList(); 

            // If the collections are the same size, just use filtered attributes on the collection items 
            if (count > 0) {
                StringWriter collectionWriter = new StringWriter(CultureInfo.InvariantCulture);

                // Build a table of object to ControlBuilder mappings 
                // so we can match collection items to their original ControlBuilders
                IDictionary objectControlBuilderTable = new Hashtable(ReferenceKeyComparer.Default); 
 
                if (defaultPersistData != null) {
                    foreach (ComplexPropertyEntry cpe in defaultPersistData.CollectionItems) { 
                        Debug.Assert(cpe.IsCollectionItem, "Expected a collection item");
                        ObjectPersistData itemPersistData = cpe.Builder.GetObjectPersistData();

                        if (itemPersistData != null) { 
                            itemPersistData.AddToObjectControlBuilderTable(objectControlBuilderTable);
                        } 
                    } 
                }
 
                if (!persistNewCollection) {
                    // Remember that we've already persisted the default filter's collection
                    skipDefaultFilter = true;
 
                    // Loop through all items in the collection
                    foreach (object current in collection) { 
                        string itemTypeName = GetTagName(current.GetType(), host); 

                        // Get the persist data for this instance of the collection item 
                        ObjectPersistData itemPersistData = null;
                        ControlBuilder itemBuilder = (ControlBuilder)objectControlBuilderTable[current];

                        if (itemBuilder != null) { 
                            itemPersistData = itemBuilder.GetObjectPersistData();
                        } 
 
                        collectionWriter.Write('<');
                        collectionWriter.Write(itemTypeName); 

                        // Persist out the filtered attributes of each item
                        SerializeAttributes(current, host, String.Empty, itemPersistData, collectionWriter, filter);
                        collectionWriter.Write('>'); 

                        // Persist out the filtered inner props of each item 
                        SerializeInnerProperties(current, host, itemPersistData, collectionWriter, filter); 
                        collectionWriter.Write("</");
                        collectionWriter.Write(itemTypeName); 
                        collectionWriter.WriteLine('>');
                    }

                    IDictionary expandos = GetExpandos(filter, name, defaultPersistData); 

                    collections.Add(new Triplet(String.Empty, collectionWriter, expandos)); 
                } 
                    // Otherwise, create an entirely new collection
                else { 
                    foreach (object current in collection) {
                        string itemTypeName = GetTagName(current.GetType(), host);

                        if (current is Control) { 
                            // default filter string since the the child controls collection can never be filtered
                            SerializeControl((Control)current, host, collectionWriter, String.Empty); 
                        } 
                        else {
                            collectionWriter.Write('<'); 
                            collectionWriter.Write(itemTypeName);

                            // Get the persist data for this instance of the collection item
                            ObjectPersistData itemPersistData = null; 
                            ControlBuilder itemBuilder = (ControlBuilder)objectControlBuilderTable[current];
 
                            if (itemBuilder != null) { 
                                itemPersistData = itemBuilder.GetObjectPersistData();
                            } 

                            if ((filter.Length == 0) && (itemPersistData != null)) {
                                // If this is the default collection, we need to pass in persistData for the collection
                                SerializeAttributes(current, host, String.Empty, itemPersistData, collectionWriter, String.Empty); 
                                collectionWriter.Write('>');
                                SerializeInnerProperties(current, host, itemPersistData, collectionWriter, String.Empty); 
                            } 
                            else {
                                // no persistData or filter since this is a new collection item 
                                SerializeAttributes(current, host, String.Empty, null, collectionWriter, String.Empty);
                                collectionWriter.Write('>');
                                SerializeInnerProperties(current, host, null, collectionWriter, String.Empty);
                            } 

                            collectionWriter.Write("</"); 
                            collectionWriter.Write(itemTypeName); 
                            collectionWriter.WriteLine('>');
                        } 
                    }

                    IDictionary expandos = GetExpandos(filter, name, persistData);
 
                    collections.Add(new Triplet(filter, collectionWriter, expandos));
                } 
            } 
            else if (defaultCount > 0) {
                // Since we'll be emitting a collection for the default filter, we need to emit an empty collection for this filter 
                IDictionary expandos = GetExpandos(filter, name, persistData);

                collections.Add(new Triplet(filter, new StringWriter(CultureInfo.InvariantCulture), expandos));
            } 

            // Persist any other filtered collections 
            if (persistData != null) { 
                ICollection filteredProperties = persistData.GetPropertyAllFilters(name);
 
                foreach (ComplexPropertyEntry entry in filteredProperties) {
                    StringWriter collectionWriter = new StringWriter(CultureInfo.InvariantCulture);

                    // Only persist collections different than the current one and if 
                    // we already persisted the default filter, skip that too
                    if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0 && (!skipDefaultFilter || entry.Filter.Length > 0)) { 
                        ObjectPersistData collectionPersistData = entry.Builder.GetObjectPersistData(); 
                        IEnumerator enumerator = collectionPersistData.CollectionItems.GetEnumerator();
 
                        foreach (ComplexPropertyEntry cpe in collectionPersistData.CollectionItems) {
                            Debug.Assert(cpe.IsCollectionItem, "Expected a collection item");
                            object current = cpe.Builder.BuildObject();
 
                            if (current is Control) {
                                // default filter string since the the child controls collection can never be filtered 
                                SerializeControl((Control)current, host, collectionWriter, String.Empty); 
                            }
                            else { 
                                string itemTypeName = GetTagName(current.GetType(), host);
                                ObjectPersistData itemPersistData = cpe.Builder.GetObjectPersistData();

                                collectionWriter.Write('<'); 
                                collectionWriter.Write(itemTypeName);
 
                                // no filter since this is a new collection 
                                SerializeAttributes(current, host, String.Empty, itemPersistData, collectionWriter, String.Empty);
                                collectionWriter.Write('>'); 

                                // no filter since this is a new collection
                                SerializeInnerProperties(current, host, itemPersistData, collectionWriter, String.Empty);
                                collectionWriter.Write("</"); 
                                collectionWriter.Write(itemTypeName);
                                collectionWriter.WriteLine('>'); 
                            } 
                        }
 
                        IDictionary expandos = GetExpandos(entry.Filter, name, persistData);

                        collections.Add(new Triplet(entry.Filter, collectionWriter, expandos));
                    } 
                }
            } 
 
            // Write out all the filtered collections
            foreach (Triplet triplet in collections) { 
                string collectionFilter = triplet.First.ToString();
                IDictionary expandos = (IDictionary)triplet.Third;

                if ((collections.Count == 1) && (collectionFilter.Length == 0) && (persistenceMode != PersistenceMode.InnerProperty) && ((expandos == null) || (expandos.Count == 0))) { 
                    writer.Write(triplet.Second.ToString());
                } 
                else { 
                    string collectionContent = triplet.Second.ToString().Trim();
 
                    if (collectionContent.Length > 0) {
                        WriteInnerPropertyBeginTag(writer, collectionFilter, name, expandos, true);
                        writer.WriteLine(collectionContent);
                        WriteInnerPropertyEndTag(writer, collectionFilter, name); 
                    }
                } 
            } 
        }
 
        private static void SerializeComplexProperty(object obj, IDesignerHost host, PropertyDescriptor propDesc, ObjectPersistData persistData, TextWriter writer, string filter) {
            string name = propDesc.Name;

            // Check if anything would be persisted for this complex property 
            // If everything was set to the default values, we don't want to persist anything
            object propValue = propDesc.GetValue(obj); 
            ObjectPersistData childPersistData = null; 

            // Get the persistData for the property's value 
            if (persistData != null) {
                ComplexPropertyEntry entry = persistData.GetFilteredProperty(String.Empty, name) as ComplexPropertyEntry;

                if (entry != null) { 
                    childPersistData = entry.Builder.GetObjectPersistData();
                } 
            } 

            StringWriter innerPropertyWriter = new StringWriter(CultureInfo.InvariantCulture); 

            SerializeInnerProperties(propValue, host, childPersistData, innerPropertyWriter, filter);

            string innerPropertyString = innerPropertyWriter.ToString(); 
            ArrayList attributes = SerializeAttributes(propValue, host, String.Empty, childPersistData, filter, false);
            StringWriter attributeWriter = new StringWriter(CultureInfo.InvariantCulture); 
            bool onlyDesignerAttributes = true; 

            foreach (Triplet triplet in attributes) { 
                string attributeFilter = triplet.First.ToString();

                if (attributeFilter != ControlBuilder.DesignerFilter) {
                    onlyDesignerAttributes = false; 
                }
 
                WriteAttribute(attributeWriter, attributeFilter, triplet.Second.ToString(), triplet.Third.ToString()); 
            }
 
            // When persist attributes, we only want to persist attribute if
            // there is an attribute which isn't a designer attribute
            // so we can remove the entire complex property tag, if there are no valid attributes
            string attributeString = String.Empty; 

            if (!onlyDesignerAttributes || (innerPropertyString.Length > 0)) { 
                attributeString = attributeWriter.ToString(); 
            }
 
            // If anything was persisted, we need to persist the tag
            if (attributeString.Length + innerPropertyString.Length > 0) {
                // Always write out a single tag for complex properties since we can just filter each of its attributes and properties
                writer.WriteLine(); 
                writer.Write('<');
                writer.Write(name); 
                writer.Write(attributeString); 
                writer.Write('>');
                writer.Write(innerPropertyString); 
                WriteInnerPropertyEndTag(writer, String.Empty, name);
            }

            // Persist the rest of the filters' complex properties 
            if (persistData != null) {
                ICollection filteredProperties = persistData.GetPropertyAllFilters(name); 
 
                foreach (ComplexPropertyEntry entry in filteredProperties) {
                    if (entry.Filter.Length > 0) { 
                        object current = entry.Builder.BuildObject();

                        writer.WriteLine();
                        writer.Write('<'); 
                        writer.Write(entry.Filter);
                        writer.Write(FILTER_SEPARATOR_CHAR); 
                        writer.Write(name); 

                        // No persist data or filter since this the outside property is already filtered 
                        SerializeAttributes(current, host, String.Empty, null, writer, String.Empty);
                        writer.Write('>');

                        // No persist data or filter since this the outside property is already filtered 
                        SerializeInnerProperties(current, host, null, writer, String.Empty);
                        WriteInnerPropertyEndTag(writer, entry.Filter, name); 
                    } 
                }
            } 
        }

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeControl1"]/*' />
        /// <devdoc> 
        /// Gets a string that contains the persisted form of the control, persisting its properties values to the specified filter's properties
        /// </devdoc> 
        public static string SerializeControl(Control control) { 
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
 
            SerializeControl(control, writer);
            return writer.ToString();
        }
 
        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeControl4"]/*' />
        /// <devdoc> 
        /// Gets a string that contains the persisted form of the control, persisting its properties values to the specified filter's properties 
        /// </devdoc>
        public static string SerializeControl(Control control, IDesignerHost host) { 
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

            SerializeControl(control, host, writer);
            return writer.ToString(); 
        }
 
        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeControl5"]/*' /> 
        /// <devdoc>
        /// Writes the persisted form of the control to the specified TextWriter, persisting its properties values to the specified filter's properties 
        /// </devdoc>
        public static void SerializeControl(Control control, TextWriter writer) {
            ISite site = control.Site;
 
            if (site == null) {
                IComponent baseComponent = (IComponent)control.Page; 
 
                if (baseComponent != null) {
                    site = baseComponent.Site; 
                }
            }

            IDesignerHost host = null; 

            if (site != null) { 
                host = (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
            }
 
            Debug.Assert(host != null, "Did not get a valid IDesignerHost reference. Expect persistence problems!");
            SerializeControl(control, host, writer);
        }
 
        public static void SerializeControl(Control control, IDesignerHost host, TextWriter writer) {
            SerializeControl(control, host, writer, GetCurrentFilter(host)); 
        } 

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeControl7"]/*' /> 
        /// <devdoc>
        /// Writes the persisted form of the control to the specified TextWriter, persisting its properties values to the specified filter's properties
        /// </devdoc>
        private static void SerializeControl(Control control, IDesignerHost host, TextWriter writer, string filter) { 
            if (control == null) {
                throw new ArgumentNullException("control"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            if (writer == null) { 
                throw new ArgumentNullException("writer");
            } 
 
            // Special case for Literal Controls - just persist the text
            if (control is LiteralControl) { 
                writer.Write(((LiteralControl)control).Text);
            }
                // Special case for designer databound literals - just persist the expression
            else if (control is DesignerDataBoundLiteralControl) { 
                Debug.Assert(((IDataBindingsAccessor)control).HasDataBindings == true);
 
                DataBindingCollection bindings = ((IDataBindingsAccessor)control).DataBindings; 
                DataBinding textBinding = bindings["Text"];
 
                Debug.Assert(textBinding != null, "Did not get a Text databinding from DesignerDataBoundLiteralControl");
                if (textBinding != null) {
                    writer.Write("<%# ");
                    writer.Write(textBinding.Expression); 
                    writer.Write(" %>");
                } 
            } 
                // Special case for user controls, just persist the attributes
            else if (control is UserControl) { 
                IUserControlDesignerAccessor accessor = (IUserControlDesignerAccessor)control;
                string tagName = accessor.TagName;

                Debug.Assert((tagName != null) && (tagName.Length > 0)); 
                if (tagName.Length > 0) {
                    writer.Write('<'); 
                    writer.Write(tagName); 
                    writer.Write(" runat=\"server\"");
 
                    ObjectPersistData persistData = null;
                    IControlBuilderAccessor cba = (IControlBuilderAccessor)control;

                    if (cba.ControlBuilder != null) { 
                        persistData = cba.ControlBuilder.GetObjectPersistData();
                    } 
 
                    SerializeAttributes(control, host, String.Empty, persistData, writer, filter);
                    writer.Write('>'); 

                    string innerText = accessor.InnerText;

                    if ((innerText != null) && (innerText.Length > 0)) { 
                        writer.Write(accessor.InnerText);
                    } 
 
                    writer.Write("</");
                    writer.Write(tagName); 
                    writer.WriteLine('>');
                }
            }
            else { 
                string tagName;
                HtmlControl htmlControl = control as HtmlControl; 
                if (htmlControl != null) { 
                    tagName = htmlControl.TagName;
                } 
                else {
                    tagName = GetTagName(control.GetType(), host);
                }
 
                writer.Write('<');
                writer.Write(tagName); 
                writer.Write(" runat=\"server\""); 

                ObjectPersistData persistData = null; 
                IControlBuilderAccessor cba = (IControlBuilderAccessor)control;

                if (cba.ControlBuilder != null) {
                    persistData = cba.ControlBuilder.GetObjectPersistData(); 
                }
 
                SerializeAttributes(control, host, String.Empty, persistData, writer, filter); 
                writer.Write('>');
 
                SerializeInnerContents(control, host, persistData, writer, filter);

                writer.Write("</");
                writer.Write(tagName); 
                writer.WriteLine('>');
            } 
        } 

        public static string SerializeInnerContents(Control control, IDesignerHost host) { 
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

            ObjectPersistData persistData = null;
            IControlBuilderAccessor cba = (IControlBuilderAccessor)control; 
            if (cba.ControlBuilder != null) {
                persistData = cba.ControlBuilder.GetObjectPersistData(); 
            } 

            SerializeInnerContents(control, host, persistData, writer, GetCurrentFilter(host)); 
            return writer.ToString();
        }

        internal static void SerializeInnerContents(Control control, IDesignerHost host, ObjectPersistData persistData, TextWriter writer, string filter) { 
            PersistChildrenAttribute persistChildrenAttr = (PersistChildrenAttribute)TypeDescriptor.GetAttributes(control)[typeof(PersistChildrenAttribute)];
            ParseChildrenAttribute parseChildrenAttr = (ParseChildrenAttribute)TypeDescriptor.GetAttributes(control)[typeof(ParseChildrenAttribute)]; 
 
            // If we are supposed to persist the children, persist them
            if (persistChildrenAttr.Persist || !parseChildrenAttr.ChildrenAsProperties && control.HasControls()) { 
                for (int i = 0; i < control.Controls.Count; i++) {
                    // Send in the empty filter since we aren't filtering the child controls collection
                    SerializeControl(control.Controls[i], host, writer, String.Empty);
                } 
            }
            else { 
                // Otherwise, persist this control's properties 
                SerializeInnerProperties(control, host, persistData, writer, filter);
            } 
        }

        public static string SerializeInnerProperties(object obj, IDesignerHost host) {
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture); 

            SerializeInnerProperties(obj, host, writer); 
            return writer.ToString(); 
        }
 
        internal static void SerializeInnerProperties(object obj, IDesignerHost host, TextWriter writer) {
            ObjectPersistData persistData = null;
            IControlBuilderAccessor cba = (IControlBuilderAccessor)obj;
 
            if (cba.ControlBuilder != null) {
                persistData = cba.ControlBuilder.GetObjectPersistData(); 
            } 

            SerializeInnerProperties(obj, host, persistData, writer, GetCurrentFilter(host)); 
        }

        private static void SerializeInnerProperties(object obj, IDesignerHost host, ObjectPersistData persistData, TextWriter writer, string filter) {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj); 

            if (obj is Control) { 
                // Force ChildControl Creation (needed for controls like CreateUserWizard which change the persistence) 
                try {
                    ControlCollection controls = ((Control)obj).Controls; 
                }
                catch (Exception ex) {
                    // Log the error with the IComponentDesignerDebugService
                    IComponentDesignerDebugService debugService = host.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService; 
                    if (debugService != null) {
                        debugService.Fail(ex.Message); 
                    } 
                }
            } 

            for (int i = 0; i < properties.Count; i++) {
                try {
                    string realFilter = filter; 

                    // If the property is not filterable, don't put a filter on the value 
                    if (!FilterableAttribute.IsPropertyFilterable(properties[i])) { 
                        realFilter = String.Empty;
                    } 

                    // Skip anything that should be hidden
                    if (properties[i].SerializationVisibility == DesignerSerializationVisibility.Hidden) {
                        continue; 
                    }
 
                    // Skip attributes 
                    PersistenceModeAttribute persistenceMode = (PersistenceModeAttribute)properties[i].Attributes[typeof(PersistenceModeAttribute)];
 
                    if (persistenceMode.Mode == PersistenceMode.Attribute) {
                        continue;
                    }
 
                    // Skip design-time only properties such as DefaultModifiers and Name
                    DesignOnlyAttribute designOnlyAttr = (DesignOnlyAttribute)properties[i].Attributes[typeof(DesignOnlyAttribute)]; 
 
                    if ((designOnlyAttr != null) && designOnlyAttr.IsDesignOnly) {
                        continue; 
                    }

                    string name = properties[i].Name;
 
                    if (properties[i].PropertyType == typeof(string)) {
                        SerializeStringProperty(obj, host, properties[i], persistData, persistenceMode.Mode, writer, filter); 
                    } 
                    else if (typeof(ICollection).IsAssignableFrom(properties[i].PropertyType)) {
                        SerializeCollectionProperty(obj, host, properties[i], persistData, persistenceMode.Mode, writer, filter); 
                    }
                    else if (typeof(ITemplate).IsAssignableFrom(properties[i].PropertyType)) {
                        SerializeTemplateProperty(obj, host, properties[i], persistData, writer, filter);
                    } 
                    else {
                        SerializeComplexProperty(obj, host, properties[i], persistData, writer, filter); 
                    } 
                }
                catch (Exception e) { 
                    if (host != null) {
                        // Log the error with the IComponentDesignerDebugService
                        IComponentDesignerDebugService debugService = host.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService;
                        if (debugService != null) { 
                            debugService.Fail(e.Message);
                        } 
                    } 
                }
            } 
        }

        private static void SerializeStringProperty(object obj, IDesignerHost host, PropertyDescriptor propDesc, ObjectPersistData persistData, PersistenceMode persistenceMode, TextWriter writer, string filter) {
            string name = propDesc.Name; 
            DataBindingCollection dataBindings = null;
 
            if (obj is IDataBindingsAccessor) { 
                dataBindings = ((IDataBindingsAccessor)obj).DataBindings;
            } 

            ExpressionBindingCollection expressions = null;

            if (obj is IExpressionsAccessor) { 
                expressions = ((IExpressionsAccessor)obj).Expressions;
            } 
 
            if ((persistenceMode != PersistenceMode.InnerProperty) && !CanSerializeAsInnerDefaultString(filter, name, propDesc.PropertyType, persistData, persistenceMode, dataBindings, expressions)) {
                return; 
            }

            ArrayList stringInnerProperties = new ArrayList();
 
            // Skip databound properties since we've already set them as attributes
            if ((dataBindings == null) || (dataBindings[name] == null) || (expressions == null) || (expressions[name] == null)) { 
                string persistValue = String.Empty; 
                object propValue = propDesc.GetValue(obj);
 
                if (propValue != null) {
                    persistValue = propValue.ToString();
                }
 
                /*
                bool shouldSerialize = true; 
 
                object defaultValue = GetPropertyDefaultValue(propDesc, name, persistData, filter);
 
                if (defaultValue == null){
                    if (filter.Length == 0) {
                        // Check the ShouldSerialize method to see if we need to serialize this string
                        // Only do this for the default filter 
                        shouldSerialize = GetShouldSerializeValue(obj, name);
                    } 
                } 
                else {
                    shouldSerialize = !object.Equals(propValue, defaultValue); 
                }
                */
                bool shouldSerialize = true;
 
                if (filter.Length == 0) {
                    bool useResult; 
                    bool customShouldSerialize = GetShouldSerializeValue(obj, name, out useResult); 

                    if (useResult) { 
                        shouldSerialize = customShouldSerialize;
                    }
                    else {
                        object defaultValue = GetPropertyDefaultValue(propDesc, name, persistData, filter, host); 

                        shouldSerialize = !object.Equals(propValue, defaultValue); 
                    } 
                }
                else { 
                    object defaultValue = GetPropertyDefaultValue(propDesc, name, persistData, filter, host);

                    shouldSerialize = !object.Equals(propValue, defaultValue);
                } 

                // Make sure the current filter's value is not equal to the default value 
                if (shouldSerialize) { 
                    // Grab all the designer expandos on the template and write them out
                    IDictionary expandos = GetExpandos(filter, name, persistData); 

                    // Add the current filter's value to the list
                    stringInnerProperties.Add(new Triplet(filter, persistValue, expandos));
                } 
            }
 
            // Add the rest of the filters' values to the list 
            if (persistData != null) {
                ICollection filteredProperties = persistData.GetPropertyAllFilters(name); 

                foreach (PropertyEntry entry in filteredProperties) {
                    if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0 && (entry is ComplexPropertyEntry)) {
                        ComplexPropertyEntry cpe = (ComplexPropertyEntry)entry; 
                        object o = cpe.Builder.BuildObject();
 
                        Debug.Assert(o is string); 

                        string strValue = o.ToString(); 

                        // Grab all the designer expandos on the template and write them out
                        IDictionary expandos = GetExpandos(entry.Filter, name, persistData);
 
                        stringInnerProperties.Add(new Triplet(entry.Filter, strValue, expandos));
                    } 
                } 
            }
 
            // Write out all the inner string properties
            foreach (Triplet triplet in stringInnerProperties) {
                bool handled = false;
                IDictionary expandos = triplet.Third as IDictionary; 

                // If there's only the non-filtered value, and this is an inner default property, try to persist it as default 
                if ((stringInnerProperties.Count == 1) && (triplet.First.ToString().Length == 0) && ((expandos == null) || (expandos.Count == 0))) { 
                    if (persistenceMode == PersistenceMode.InnerDefaultProperty) {
                        writer.Write(triplet.Second.ToString()); 
                        handled = true;
                    }
                    else if (persistenceMode == PersistenceMode.EncodedInnerDefaultProperty) {
                        HttpUtility.HtmlEncode(triplet.Second.ToString(), writer); 
                        handled = true;
                    } 
                } 

                // Otherwise, it's a normal inner property 
                if (!handled) {
                    string currentFilter = triplet.First.ToString();

                    WriteInnerPropertyBeginTag(writer, currentFilter, name, expandos, true); 
                    writer.Write(triplet.Second.ToString());
                    WriteInnerPropertyEndTag(writer, currentFilter, name); 
                } 
            }
        } 

        private static void SerializeTemplateProperty(object obj, IDesignerHost host, PropertyDescriptor propDesc, ObjectPersistData persistData, TextWriter writer, string filter) {
            string name = propDesc.Name;
 
            // Get the string representation of the current filter's template
            string templateString = String.Empty; 
            ITemplate template = (ITemplate)propDesc.GetValue(obj); 

            // If template is null, do not persist anything. 
            // But don't forget to persist out the other filtered templates
            if (template != null) {
                templateString = SerializeTemplate(template, host);
 
                // Get the string representation of the default filter's template
                string defaultTemplateString = String.Empty; 
 
                if ((filter.Length > 0) && (persistData != null)) {
                    TemplatePropertyEntry defaultEntry = persistData.GetFilteredProperty(String.Empty, name) as TemplatePropertyEntry; 

                    if (defaultEntry != null) {
                        defaultTemplateString = SerializeTemplate(defaultEntry.Builder as ITemplate, host);
                    } 
                }
 
                // Grab all the designer expandos on the template and write them out 
                IDictionary expandos = GetExpandos(filter, name, persistData);
 
                if (((template != null) && (expandos != null && expandos.Count > 0)) || !string.Equals(defaultTemplateString, templateString)) {
                    WriteInnerPropertyBeginTag(writer, filter, name, expandos, false);
                    if (templateString.Length > 0 && !templateString.StartsWith("\r\n", StringComparison.Ordinal)) {
                        writer.WriteLine(); 
                    }
 
                    writer.Write(templateString); 
                    if (templateString.Length > 0 && !templateString.EndsWith("\r\n", StringComparison.Ordinal)) {
                        writer.WriteLine(); 
                    }

                    WriteInnerPropertyEndTag(writer, filter, name);
                } 
            }
 
            if (persistData != null) { 
                // Add the rest of the filters' values to the list
                ICollection filteredProperties = persistData.GetPropertyAllFilters(name); 

                foreach (TemplatePropertyEntry entry in filteredProperties) {
                    if (String.Compare(entry.Filter, filter, StringComparison.OrdinalIgnoreCase) != 0) {
                        // Grab all the designer expandos on the template and write them out 
                        IDictionary expandos = GetExpandos(entry.Filter, name, persistData);
 
                        WriteInnerPropertyBeginTag(writer, entry.Filter, name, expandos, false); 

                        string filteredTemplateString = SerializeTemplate((ITemplate)entry.Builder, host); 

                        if (filteredTemplateString != null) {
                            if (!filteredTemplateString.StartsWith("\r\n", StringComparison.Ordinal)) {
                                writer.WriteLine(); 
                            }
 
                            writer.Write(filteredTemplateString); 
                            if (!filteredTemplateString.EndsWith("\r\n", StringComparison.Ordinal)) {
                                writer.WriteLine(); 
                            }

                            WriteInnerPropertyEndTag(writer, entry.Filter, name);
                        } 
                    }
                } 
            } 
        }
 
        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeTemplate2"]/*' />
        /// <devdoc>
        /// Writes the persisted form of the specified template to the specified TextWriter
        /// </devdoc> 
        public static string SerializeTemplate(ITemplate template, IDesignerHost host) {
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture); 
 
            SerializeTemplate(template, writer, host);
            return writer.ToString(); 
        }

        /// <include file='doc\ControlSerializer.uex' path='docs/doc[@for="ControlSerializer.SerializeTemplate3"]/*' />
        /// <devdoc> 
        /// Writes the persisted form of the specified template to the specified TextWriter
        /// </devdoc> 
        public static void SerializeTemplate(ITemplate template, TextWriter writer, IDesignerHost host) { 
            if (template == null) {
                return; 
            }

            if (writer == null) {
                throw new ArgumentNullException("writer"); 
            }
 
            if (template is TemplateBuilder) { 
                writer.Write(((TemplateBuilder)template).Text);
            } 
            else {
                Control container = new Control();
                StringBuilder sb = new StringBuilder();
 
                try {
                    template.InstantiateIn(container); 
                    foreach (Control control in container.Controls) { 
                        sb.Append(SerializeControl(control, host));
                    } 

                    writer.Write(sb.ToString());
                }
                catch (Exception ex) { 
                    Debug.Fail(ex.ToString());
                } 
            } 

            writer.Flush(); 
        }

        /// <devdoc>
        /// Indicates whether we want to persist a blank string in the tag. We only want to do this for some types. 
        /// </devdoc>
        private static bool ShouldPersistBlankValue(object defValue, Type type) { 
            Debug.Assert(defValue != null && type != null, "default value attribute or type can't be null!"); 
            if (type == typeof(string)) {
                return !defValue.Equals(""); 
            }
            else if (type == typeof(Color)) {
                return !(((Color)defValue).IsEmpty);
            } 
            else if (type == typeof(FontUnit)) {
                return !(((FontUnit)defValue).IsEmpty); 
            } 
            else if (type == typeof(Unit)) {
                return !defValue.Equals(Unit.Empty); ; 
            }

            return false;
        } 

        /// <devdoc> 
        /// Write the specified filtered attribute and attribute value 
        /// </devdoc>
        private static void WriteAttribute(TextWriter writer, string filter, string name, string value) { 
            writer.Write(" ");
            if ((filter != null) && (filter.Length > 0)) {
                writer.Write(filter);
                writer.Write(FILTER_SEPARATOR_CHAR); 
            }
 
            writer.Write(name); 
            if (value.IndexOf('"') > -1) {
                writer.Write("=\'"); 
                writer.Write(value);
                writer.Write("\'");
            }
            else { 
                writer.Write("=\"");
                writer.Write(value); 
                writer.Write("\""); 
            }
        } 

        private static void WriteInnerPropertyBeginTag(TextWriter writer, string filter, string name, IDictionary expandos, bool requiresNewLine) {
            writer.Write('<');
            if ((filter != null) && (filter.Length > 0)) { 
                writer.Write(filter);
                writer.Write(FILTER_SEPARATOR_CHAR); 
            } 

            writer.Write(name); 
            if (expandos != null) {
                foreach (DictionaryEntry expando in expandos) {
                    SimplePropertyEntry spe = expando.Value as SimplePropertyEntry;
 
                    if (spe != null) {
                        WriteAttribute(writer, ControlBuilder.DesignerFilter, expando.Key.ToString(), spe.Value.ToString()); 
                    } 
                }
            } 

            if (requiresNewLine) {
                writer.WriteLine('>');
            } 
            else {
                writer.Write('>'); 
            } 
        }
 
        private static void WriteInnerPropertyEndTag(TextWriter writer, string filter, string name) {
            writer.Write("</");
            if ((filter != null) && (filter.Length > 0)) {
                writer.Write(filter); 
                writer.Write(FILTER_SEPARATOR_CHAR);
            } 
 
            writer.Write(name);
            writer.WriteLine('>'); 
        }

        private sealed class WebFormsDesigntimeLicenseContext : DesigntimeLicenseContext {
            private IServiceProvider provider; 

            public WebFormsDesigntimeLicenseContext(IServiceProvider provider) { 
                this.provider = provider; 
            }
 
            public override object GetService(Type serviceClass) {
                if (provider != null) {
                    return provider.GetService(serviceClass);
                } 
                else {
                    return null; 
                } 
            }
        } 

        private enum BindingType {
            None = 0,
            Data = 1, 
            Expression = 2
        } 
 
        private class ReferenceKeyComparer : IEqualityComparer {
            internal static readonly ReferenceKeyComparer Default = new ReferenceKeyComparer(); 

            bool IEqualityComparer.Equals(object x, object y) {
                return Object.ReferenceEquals(x, y);
            } 

            int IEqualityComparer.GetHashCode(object obj) { 
                return obj.GetHashCode(); 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
