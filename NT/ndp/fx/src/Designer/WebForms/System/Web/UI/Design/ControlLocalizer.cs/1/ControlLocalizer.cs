//------------------------------------------------------------------------------ 
// <copyright file="ControlLocalizer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.Design {
    using System; 
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics;
    using System.Text;
    using System.Web.Compilation; 

    // 
 

 
    internal static class ControlLocalizer {
        private const string LocalizationResourceExpressionPrefix = "resources";
        private const char filterDelimiter = ':';
        private const char objectDelimiter = '.'; 
        private const char OMDelimiter = '.';
 
        private static bool IsPropertyLocalizable(PropertyDescriptor propertyDescriptor) { 
            DesignerSerializationVisibilityAttribute serializationAttr = (DesignerSerializationVisibilityAttribute)propertyDescriptor.Attributes[typeof(DesignerSerializationVisibilityAttribute)];
            if (serializationAttr != null && serializationAttr.Visibility == DesignerSerializationVisibility.Hidden) { 
                return false;
            }
            LocalizableAttribute localizableAttr = (LocalizableAttribute)propertyDescriptor.Attributes[typeof(LocalizableAttribute)];
            if (localizableAttr != null) { 
                return localizableAttr.IsLocalizable;
            } 
            return false; 
        }
 
        /// <devdoc>
        /// This method makes virtually no assumptions since it creates a copy of the control
        /// by persisting and re-parsing the control in its default state before localization
        /// is performed. The host must implement certain services such as IFilterResolutionService 
        /// in order for this to work.
        /// </devdoc> 
        public static string LocalizeControl(Control control, IDesignTimeResourceWriter resourceWriter, out string newInnerContent) { 
            if (control == null) {
                throw new ArgumentNullException("control"); 
            }
            if (resourceWriter == null) {
                throw new ArgumentNullException("resourceWriter");
            } 
            if (control.Site == null) {
                throw new InvalidOperationException(); 
            } 

            // Create a copy of the original control as the "localization control" - just 
            // like we do for ViewControl in ControlDesigner.
            // This is required since the tool might be in a specific device filter, and
            // the evaluated values of properties reflect that device filter, whereas we
            // want to persist localization resources as though we are in the default filter. 

            IDesignerHost desHost = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost)); 
            IDesignerHost localizationDesignerHost = new LocalizationDesignerHost(desHost); 

            ControlDesigner designer = desHost.GetDesigner(control) as ControlDesigner; 
            Control localizationControl = designer.CreateClonedControl(localizationDesignerHost, false);
            ((IControlDesignerAccessor)localizationControl).SetOwnerControl(control);

            bool shouldLocalizeInnerContents = ShouldLocalizeInnerContents(control.Site, control); 
            string newResourceKey = LocalizeControl(localizationControl, localizationDesignerHost, resourceWriter, shouldLocalizeInnerContents);
            if (shouldLocalizeInnerContents) { 
                newInnerContent = ControlSerializer.SerializeInnerContents(localizationControl, localizationDesignerHost); 
            }
            else { 
                newInnerContent = null;
            }
            return newResourceKey;
        } 

        private static string LocalizeControl(Control control, IServiceProvider serviceProvider, IDesignTimeResourceWriter resourceWriter, bool shouldLocalizeInnerContent) { 
#if ORCAS 
            IFilterResolutionService filterService = (IFilterResolutionService)serviceProvider.GetService(typeof(IFilterResolutionService));
            string filter = filterService.CurrentFilter; 
            if (String.Equals(filter, "Default", StringComparison.InvariantCultureIgnoreCase)) {
                filter = String.Empty;
            }
 
            Debug.Assert(filter.Length == 0, "Should always be in default filter mode when localizing!");
#endif 
 
            ResourceExpressionEditor resEditor = (ResourceExpressionEditor)ExpressionEditor.GetExpressionEditor(LocalizationResourceExpressionPrefix, serviceProvider);
 
            IControlBuilderAccessor cba = (IControlBuilderAccessor)control;
            ControlBuilder controlBuilder = cba.ControlBuilder;
            ObjectPersistData persistData = controlBuilder.GetObjectPersistData();
 
            string resourceKey = controlBuilder.GetResourceKey();
            string newResourceKey = LocalizeObject(serviceProvider, control, persistData, resEditor, resourceWriter, resourceKey, String.Empty, control, String.Empty, shouldLocalizeInnerContent, false, false); 
            if (!String.Equals(resourceKey, newResourceKey, StringComparison.OrdinalIgnoreCase)) { 
                controlBuilder.SetResourceKey(newResourceKey);
            } 

            // Localize all bound properties that are persisted as hyphenated names, for
            // example, ItemStyle-BackColor="<%$ foo:bar %>"
            if (persistData != null) { 
                foreach (PropertyEntry entry in persistData.AllPropertyEntries) {
                    BoundPropertyEntry bpe = entry as BoundPropertyEntry; 
                    if (bpe != null && !bpe.Generated) { 
                        string[] parts = bpe.Name.Split(OMDelimiter);
                        if (parts.Length > 1) { 
                            object propValue = control;
                            foreach (string part in parts) {
                                PropertyDescriptor subPropDesc = TypeDescriptor.GetProperties(propValue)[part];
                                // Ignore non-existent properties (i.e. expandos) 
                                if (subPropDesc == null) {
                                    break; 
                                } 
                                PersistenceModeAttribute mode = subPropDesc.Attributes[typeof(PersistenceModeAttribute)] as PersistenceModeAttribute;
                                if (mode != PersistenceModeAttribute.Attribute) { 
                                    // Localize the value

                                    // If it's a page-level resource expression, preserve it's value
                                    if (String.Equals(bpe.ExpressionPrefix, LocalizationResourceExpressionPrefix, StringComparison.OrdinalIgnoreCase)) { 
                                        ResourceExpressionFields fields = bpe.ParsedExpressionData as ResourceExpressionFields;
                                        if (fields != null) { 
                                            if (String.IsNullOrEmpty(fields.ClassKey)) { 
                                                // Page resource
                                                object resourceValue = resEditor.EvaluateExpression(bpe.Expression, bpe.ParsedExpressionData, bpe.PropertyInfo.PropertyType, serviceProvider); 
                                                // Never persist null values since they look bad in the persistence
                                                if (resourceValue == null) {
                                                    object realTarget;
                                                    PropertyDescriptor realPropDesc = ControlDesigner.GetComplexProperty(control, bpe.Name, out realTarget); 
                                                    resourceValue = realPropDesc.GetValue(realTarget);
                                                } 
                                                resourceWriter.AddResource(fields.ResourceKey, resourceValue); 
                                            }
                                        } 
                                    }
                                    break;
                                }
                                propValue = subPropDesc.GetValue(propValue); 
                            }
                        } 
                    } 
                }
            } 

            return newResourceKey;
        }
 
        /// <devdoc>
        /// Indicates whether or not the Controls collection of a control should 
        /// be localized or not. For example, a Panel has child controls but they 
        /// are top-level controls in the designer because it is not an INamingContainer,
        /// so the designer will call Localize() on each one. However, a Label can also 
        /// have child controls but they are not top-level controls, so we have to
        /// localize them.
        /// </devdoc>
        private static bool ShouldLocalizeInnerContents(IServiceProvider serviceProvider, object obj) { 
            Control c = obj as Control;
            if (c == null) { 
                return false; 
            }
            IDesignerHost desHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            if (desHost == null) {
                return false;
            }
            ControlDesigner designer = desHost.GetDesigner(c) as ControlDesigner; 
            if (designer != null) {
                if (!designer.ReadOnlyInternal) { 
                    return false; 
                }
            } 
            return true;
        }

        private static bool ParseChildren(Type controlType) { 
            object[] attrs = controlType.GetCustomAttributes(typeof(ParseChildrenAttribute), true);
            if (attrs != null && attrs.Length > 0) { 
                return ((ParseChildrenAttribute)attrs[0]).ChildrenAsProperties; 
            }
            return false; 
        }

        private static string LocalizeObject(IServiceProvider serviceProvider, object obj, ObjectPersistData persistData,
            ResourceExpressionEditor resEditor, IDesignTimeResourceWriter resourceWriter, string resourceKey, string objectModelName, 
            object topLevelObject, string filter, bool shouldLocalizeInnerContent, bool isComplexProperty, bool implicitlyLocalizeComplexProperty) {
 
            bool doImplicitLocalization; 
            if (isComplexProperty) {
                // If it's a complex property we need to know whether we localize implicitly based on our parent 
                doImplicitLocalization = implicitlyLocalizeComplexProperty;
            }
            else {
                // If it's not a complex property, just follow whatever the persist data says. 
                doImplicitLocalization = ((persistData == null) || persistData.Localize);
            } 
 
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj);
 
            for (int i = 0; i < properties.Count; i++) {
                try {
                    PropertyDescriptor propDesc = properties[i];
 
                    if (String.Equals(propDesc.Name, "Controls", StringComparison.Ordinal)) {
                        Control c = obj as Control; 
                        if (c != null && shouldLocalizeInnerContent) { 
                            if (!ParseChildren(c.GetType())) {
                                ControlCollection controls = c.Controls; 
                                foreach (Control childControl in controls) {
                                    IControlBuilderAccessor cba = (IControlBuilderAccessor)childControl;
                                    ControlBuilder builder = cba.ControlBuilder;
                                    if (builder != null) { 
                                        // Use the existing resource key or create a new one
                                        string childResourceKey = builder.GetResourceKey(); 
                                        string newChildResourceKey = LocalizeObject(serviceProvider, childControl, builder.GetObjectPersistData(), resEditor, resourceWriter, childResourceKey, String.Empty, childControl, String.Empty, true, false, false); 
                                        if (!String.Equals(childResourceKey, newChildResourceKey, StringComparison.OrdinalIgnoreCase)) {
                                            builder.SetResourceKey(newChildResourceKey); 
                                        }
                                    }
                                }
                            } 
                            continue;
                        } 
                    } 

                    PersistenceModeAttribute persistenceMode = (PersistenceModeAttribute)propDesc.Attributes[typeof(PersistenceModeAttribute)]; 

                    string newObjectModelName = (objectModelName.Length > 0) ? (objectModelName + OMDelimiter + propDesc.Name) : propDesc.Name;

                    // For complex properties persisted as attribute and with designer visibility as content 
                    // recurse in and use '.' syntax (ie TreeViewResource1.Font.Bold)
                    if ((persistenceMode.Mode == PersistenceMode.Attribute) && (propDesc.SerializationVisibility == DesignerSerializationVisibility.Content)) { 
                        resourceKey = LocalizeObject(serviceProvider, propDesc.GetValue(obj), persistData, resEditor, resourceWriter, 
                                                     resourceKey,
                                                     newObjectModelName, 
                                                     topLevelObject,
                                                     filter, true, true, doImplicitLocalization);
                        continue;
                    } 

                    // If it's an attribute or a string property 
                    if ((persistenceMode.Mode == PersistenceMode.Attribute) || (propDesc.PropertyType == typeof(string))) { 
                        bool pushValue = false;
                        bool havePropValue = false; 
                        object propValue = null;

                        string newResourceKey = String.Empty;
 
                        // Check if the property is bound
                        if (persistData != null) { 
                            PropertyEntry entry = persistData.GetFilteredProperty(String.Empty, newObjectModelName); 
                            if (entry is BoundPropertyEntry) {
                                BoundPropertyEntry bpe = (BoundPropertyEntry)entry; 
                                if (!bpe.Generated) {
                                    // If it's a page-level resource expression, preserve it's value
                                    if (String.Equals(bpe.ExpressionPrefix, LocalizationResourceExpressionPrefix, StringComparison.OrdinalIgnoreCase)) {
                                        ResourceExpressionFields fields = bpe.ParsedExpressionData as ResourceExpressionFields; 
                                        if (fields != null) {
                                            if (String.IsNullOrEmpty(fields.ClassKey)) { 
                                                // Page resource 
                                                newResourceKey = fields.ResourceKey;
                                                propValue = resEditor.EvaluateExpression(bpe.Expression, bpe.ParsedExpressionData, propDesc.PropertyType, serviceProvider); 
                                                if (propValue != null) {
                                                    // We only say we have a property value if it is not null.
                                                    // Otherwise we get the property's live value later.
                                                    havePropValue = true; 
                                                }
                                                pushValue = true; 
                                            } 
                                        }
                                    } 
                                }
                                else {
                                    // Property is bound but is auto-generated, so we want to
                                    // push the current value of the property into the resource 
                                    // file such that it will match the current value of the control.
                                    // There is no need to check the Localizable attribute since we 
                                    // always re-push values of previously localized properties. 
                                    pushValue = true;
                                } 
                            }
                            else {
                                // Property is declared on the tag, but doesn't use a resource
                                // expression, so we push only if it is localizable. 
                                pushValue = doImplicitLocalization && IsPropertyLocalizable(propDesc);
                            } 
                        } 
                        else {
                            // Property is not present on the tag, so we push only if 
                            // it is localizable.
                            pushValue = doImplicitLocalization && IsPropertyLocalizable(propDesc);
                        }
 
                        if (pushValue) {
                            if (!havePropValue) { 
                                propValue = propDesc.GetValue(obj); 
                            }
 
                            // Create a resource key for this control if it doesn't already have one
                            if (newResourceKey.Length == 0) {
                                if (String.IsNullOrEmpty(resourceKey)) {
                                    resourceKey = resourceWriter.CreateResourceKey(null, topLevelObject); 
                                }
                                newResourceKey = resourceKey + objectDelimiter + newObjectModelName; 
 
                                if (filter.Length != 0) {
                                    newResourceKey = filter + filterDelimiter + newResourceKey; 
                                }
                            }
                            // Push the value to the page resources
                            resourceWriter.AddResource(newResourceKey, propValue); 
                        }
 
                        // Push values for any filtered properties 
                        if (persistData != null) {
                            ICollection filteredProperties = persistData.GetPropertyAllFilters(newObjectModelName); 

                            foreach (PropertyEntry filteredEntry in filteredProperties) {
                                if (filteredEntry.Filter.Length > 0) {
                                    if (filteredEntry is SimplePropertyEntry) { 
                                        // For simple properties (e.g. Text="hello") we push the
                                        // literal value if the property is localizable 
                                        if (doImplicitLocalization && IsPropertyLocalizable(propDesc)) { 
                                            if (newResourceKey.Length == 0) {
                                                if (String.IsNullOrEmpty(resourceKey)) { 
                                                    resourceKey = resourceWriter.CreateResourceKey(null, topLevelObject);
                                                }
                                                newResourceKey = resourceKey + objectDelimiter + newObjectModelName;
                                            } 
                                            string filterResourceKey = filteredEntry.Filter + filterDelimiter + newResourceKey;
                                            resourceWriter.AddResource(filterResourceKey, ((SimplePropertyEntry)filteredEntry).Value); 
                                        } 
                                    }
                                    else { 
                                        if (filteredEntry is ComplexPropertyEntry) {
                                            Debug.Fail("When does this happen?");
                                        }
                                        else { 
                                            if (filteredEntry is BoundPropertyEntry) {
                                                // For bound properties (e.g. Text="<%$ expression:value %>") we push 
                                                // the value only if it is a resource expression. 
                                                BoundPropertyEntry bpe = (BoundPropertyEntry)filteredEntry;
                                                if (!bpe.Generated) { 
                                                    // If it's a page-level resource expression, preserve it's value
                                                    if (String.Equals(bpe.ExpressionPrefix, LocalizationResourceExpressionPrefix, StringComparison.OrdinalIgnoreCase)) {
                                                        ResourceExpressionFields fields = bpe.ParsedExpressionData as ResourceExpressionFields;
                                                        if (fields != null) { 
                                                            if (String.IsNullOrEmpty(fields.ClassKey)) {
                                                                // Page resource 
                                                                object filteredPropValue = resEditor.EvaluateExpression(bpe.Expression, bpe.ParsedExpressionData, filteredEntry.PropertyInfo.PropertyType, serviceProvider); 
                                                                // Never persist null values since they look bad in the persistence
                                                                if (filteredPropValue == null) { 
                                                                    filteredPropValue = String.Empty;
                                                                }
                                                                resourceWriter.AddResource(fields.ResourceKey, filteredPropValue);
                                                            } 
                                                        }
                                                    } 
                                                } 
                                            }
                                        } 
                                    }
                                }
                            }
                        } 

                        continue; 
                    } 

                    if (!shouldLocalizeInnerContent) { 
                        continue;
                    }

                    // For a collection, generate (or re-use) a new resource key for each collection item 
                    if (typeof(ICollection).IsAssignableFrom(propDesc.PropertyType)) {
                        // Localize all filtered collections 
                        if (persistData != null) { 
                            ICollection filteredProperties = persistData.GetPropertyAllFilters(propDesc.Name);
                            foreach (ComplexPropertyEntry entry in filteredProperties) { 
                                ObjectPersistData collectionPersistData = entry.Builder.GetObjectPersistData();
                                foreach (ComplexPropertyEntry cpe in collectionPersistData.CollectionItems) {
                                    Debug.Assert(cpe.IsCollectionItem, "Expected a collection item- how did this get in there?");
                                    ControlBuilder builder = cpe.Builder; 
                                    object current = builder.BuildObject();
 
                                    // Use the existing resource key or create a new one 
                                    string childResourceKey = builder.GetResourceKey();
                                    string newChildResourceKey = LocalizeObject(serviceProvider, current, builder.GetObjectPersistData(), resEditor, resourceWriter, childResourceKey, String.Empty, current, String.Empty, true, false, false); 
                                    if (!String.Equals(childResourceKey, newChildResourceKey, StringComparison.OrdinalIgnoreCase)) {
                                        builder.SetResourceKey(newChildResourceKey);
                                    }
                                } 
                            }
                        } 
 
                        continue;
                    } 

                    // For a template, parse the template, walk the child controls, localize them, then re-persist with
                    // resource keys
                    if (typeof(ITemplate).IsAssignableFrom(propDesc.PropertyType)) { 
                        // Localize all filtered templates
                        if (persistData != null) { 
                            ICollection filteredProperties = persistData.GetPropertyAllFilters(propDesc.Name); 
                            foreach (TemplatePropertyEntry entry in filteredProperties) {
                                TemplateBuilder templateBuilder = (TemplateBuilder)entry.Builder; 

                                IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                                Debug.Assert(designerHost != null);
 
                                Control[] controls = ControlParser.ParseControls(designerHost, templateBuilder.Text);
                                for (int j = 0; j < controls.Length; j++) { 
                                    if (!(controls[j] is LiteralControl) && !(controls[j] is DesignerDataBoundLiteralControl)) { 
                                        LocalizeControl(controls[j], serviceProvider, resourceWriter, true);
                                    } 
                                }

                                StringBuilder newTemplateText = new StringBuilder();
                                for (int j = 0; j < controls.Length; j++) { 
                                    if (controls[j] is LiteralControl) {
                                        newTemplateText.Append(((LiteralControl)controls[j]).Text); 
                                    } 
                                    else {
                                        newTemplateText.Append(ControlPersister.PersistControl(controls[j], designerHost)); 
                                    }
                                }

                                templateBuilder.Text = newTemplateText.ToString(); 
                            }
                        } 
 
                        continue;
                    } 

                    // Ordinary complex properties are handled using '.' syntax (ie TreeViewResource1.NodeStyle.BackColor)
                    {
                        if (persistData != null) { 
                            object childObject = propDesc.GetValue(obj);
                            ObjectPersistData childPersistData = null; 
                            ComplexPropertyEntry cpe = (ComplexPropertyEntry)persistData.GetFilteredProperty(String.Empty, propDesc.Name); 
                            if (cpe != null) {
                                childPersistData = cpe.Builder.GetObjectPersistData(); 
                            }

                            resourceKey = LocalizeObject(serviceProvider, childObject, childPersistData, resEditor, resourceWriter, resourceKey, newObjectModelName, topLevelObject, String.Empty, true, true, doImplicitLocalization);
 
                            ICollection filteredProperties = persistData.GetPropertyAllFilters(propDesc.Name);
                            foreach (ComplexPropertyEntry entry in filteredProperties) { 
                                if (entry.Filter.Length > 0) { 
                                    ControlBuilder builder = entry.Builder;
                                    childPersistData = builder.GetObjectPersistData(); 
                                    childObject = builder.BuildObject();

                                    resourceKey = LocalizeObject(serviceProvider, childObject, childPersistData, resEditor, resourceWriter, resourceKey, newObjectModelName, topLevelObject, entry.Filter, true, true, doImplicitLocalization);
                                } 
                            }
                        } 
                    } 
                }
                catch (Exception e) { 
                    if (serviceProvider != null) {
                        // Log the error with the IComponentDesignerDebugService
                        IComponentDesignerDebugService debugService = serviceProvider.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService;
                        if (debugService != null) { 
                            debugService.Fail(e.Message);
                        } 
                    } 
                }
            } 

            return resourceKey;
        }
 

        /// <devdoc> 
        /// Dummy implementation of a designer host. This is needed so that we can pass 
        /// in our own filter resolution service that pretends that it is always in the
        /// default filter mode. 
        /// All calls are delegated to the real designer host except for GetService(),
        /// which is special-cased to return our own IFilterResolutionService.
        /// </devdoc>
        private sealed class LocalizationDesignerHost : IDesignerHost { 

            private IDesignerHost _parentHost; 
            private LocalizationFilterResolutionService _localizationFilterService; 

            internal LocalizationDesignerHost(IDesignerHost parentHost) { 
                _parentHost = parentHost;
            }

            #region Implementation of IDesignerHost 
            IContainer IDesignerHost.Container {
                get { 
                    return _parentHost.Container; 
                }
            } 

            bool IDesignerHost.InTransaction {
                get {
                    return _parentHost.InTransaction; 
                }
            } 
 
            bool IDesignerHost.Loading {
                get { 
                    return _parentHost.Loading;
                }
            }
 
            string IDesignerHost.TransactionDescription {
                get { 
                    return _parentHost.TransactionDescription; 
                }
            } 

            IComponent IDesignerHost.RootComponent {
                get {
                    return _parentHost.RootComponent; 
                }
            } 
 
            string IDesignerHost.RootComponentClassName {
                get { 
                    return _parentHost.RootComponentClassName;
                }
            }
 
            event EventHandler IDesignerHost.Activated {
                add { 
                    _parentHost.Activated += value; 
                }
                remove { 
                    _parentHost.Activated -= value;
                }
            }
 
            event EventHandler IDesignerHost.Deactivated {
                add { 
                    _parentHost.Deactivated += value; 
                }
                remove { 
                    _parentHost.Deactivated -= value;
                }
            }
 
            event EventHandler IDesignerHost.LoadComplete {
                add { 
                    _parentHost.LoadComplete += value; 
                }
                remove { 
                    _parentHost.LoadComplete -= value;
                }
            }
 
            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosed {
                add { 
                    _parentHost.TransactionClosed += value; 
                }
                remove { 
                    _parentHost.TransactionClosed -= value;
                }
            }
 
            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosing {
                add { 
                    _parentHost.TransactionClosing += value; 
                }
                remove { 
                    _parentHost.TransactionClosing -= value;
                }
            }
 
            event EventHandler IDesignerHost.TransactionOpened {
                add { 
                    _parentHost.TransactionOpened += value; 
                }
                remove { 
                    _parentHost.TransactionOpened -= value;
                }
            }
 
            event EventHandler IDesignerHost.TransactionOpening {
                add { 
                    _parentHost.TransactionOpening += value; 
                }
                remove { 
                    _parentHost.TransactionOpening -= value;
                }
            }
 
            void IDesignerHost.Activate() {
            } 
 
            IComponent IDesignerHost.CreateComponent(Type componentType) {
                return _parentHost.CreateComponent(componentType); 
            }

            IComponent IDesignerHost.CreateComponent(Type componentType, string name) {
                return _parentHost.CreateComponent(componentType, name); 
            }
 
            DesignerTransaction IDesignerHost.CreateTransaction() { 
                return _parentHost.CreateTransaction();
            } 

            DesignerTransaction IDesignerHost.CreateTransaction(string description) {
                return _parentHost.CreateTransaction(description);
            } 

            void IDesignerHost.DestroyComponent(IComponent component) { 
                _parentHost.DestroyComponent(component); 
            }
 
            Type IDesignerHost.GetType(string typeName) {
                return _parentHost.GetType(typeName);
            }
 
            IDesigner IDesignerHost.GetDesigner(IComponent component) {
                return _parentHost.GetDesigner(component); 
            } 

            void IServiceContainer.RemoveService(Type serviceType, bool promote) { 
                _parentHost.RemoveService(serviceType, promote);
            }

            void IServiceContainer.RemoveService(Type serviceType) { 
                _parentHost.RemoveService(serviceType);
            } 
 
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) {
                _parentHost.AddService(serviceType, callback, promote); 
            }

            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) {
                _parentHost.AddService(serviceType, callback); 
            }
 
            void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) { 
                _parentHost.AddService(serviceType, serviceInstance, promote);
            } 

            void IServiceContainer.AddService(Type serviceType, object serviceInstance) {
                _parentHost.AddService(serviceType, serviceInstance);
            } 

            object IServiceProvider.GetService(Type serviceType) { 
                if (serviceType == typeof(IFilterResolutionService)) { 
                    if (_localizationFilterService == null) {
                        IFilterResolutionService realFilterService = (IFilterResolutionService)_parentHost.GetService(typeof(IFilterResolutionService)); 
                        if (realFilterService == null) {
                            throw new InvalidOperationException(SR.GetString(SR.ControlLocalizer_RequiresFilterService));
                        }
                        _localizationFilterService = new LocalizationFilterResolutionService(realFilterService); 
                    }
                    return _localizationFilterService; 
                } 
                return _parentHost.GetService(serviceType);
            } 
            #endregion

            private sealed class LocalizationFilterResolutionService : IFilterResolutionService {
                private IFilterResolutionService _realFilterService; 

                internal LocalizationFilterResolutionService(IFilterResolutionService realFilterService) { 
                    _realFilterService = realFilterService; 
                }
 
#if ORCAS
                string IFilterResolutionService.CurrentFilter {
                    get {
                        // Always return the default filter 
                        return String.Empty;
                    } 
                } 
#endif
 
                int IFilterResolutionService.CompareFilters(string filter1, string filter2) {
                    // Filter comparison is done by the host's filter resolution service
                    return _realFilterService.CompareFilters(filter1, filter2);
                } 

                bool IFilterResolutionService.EvaluateFilter(string filterName) { 
                    // Only the default filter applies to the current state 
                    if ((filterName == null) || (filterName.Length == 0) ||
                        (String.Equals(filterName, "default", StringComparison.OrdinalIgnoreCase))) { 
                        return true;
                    }
                    // All other filters do not apply
                    return false; 
                }
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlLocalizer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.Design {
    using System; 
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics;
    using System.Text;
    using System.Web.Compilation; 

    // 
 

 
    internal static class ControlLocalizer {
        private const string LocalizationResourceExpressionPrefix = "resources";
        private const char filterDelimiter = ':';
        private const char objectDelimiter = '.'; 
        private const char OMDelimiter = '.';
 
        private static bool IsPropertyLocalizable(PropertyDescriptor propertyDescriptor) { 
            DesignerSerializationVisibilityAttribute serializationAttr = (DesignerSerializationVisibilityAttribute)propertyDescriptor.Attributes[typeof(DesignerSerializationVisibilityAttribute)];
            if (serializationAttr != null && serializationAttr.Visibility == DesignerSerializationVisibility.Hidden) { 
                return false;
            }
            LocalizableAttribute localizableAttr = (LocalizableAttribute)propertyDescriptor.Attributes[typeof(LocalizableAttribute)];
            if (localizableAttr != null) { 
                return localizableAttr.IsLocalizable;
            } 
            return false; 
        }
 
        /// <devdoc>
        /// This method makes virtually no assumptions since it creates a copy of the control
        /// by persisting and re-parsing the control in its default state before localization
        /// is performed. The host must implement certain services such as IFilterResolutionService 
        /// in order for this to work.
        /// </devdoc> 
        public static string LocalizeControl(Control control, IDesignTimeResourceWriter resourceWriter, out string newInnerContent) { 
            if (control == null) {
                throw new ArgumentNullException("control"); 
            }
            if (resourceWriter == null) {
                throw new ArgumentNullException("resourceWriter");
            } 
            if (control.Site == null) {
                throw new InvalidOperationException(); 
            } 

            // Create a copy of the original control as the "localization control" - just 
            // like we do for ViewControl in ControlDesigner.
            // This is required since the tool might be in a specific device filter, and
            // the evaluated values of properties reflect that device filter, whereas we
            // want to persist localization resources as though we are in the default filter. 

            IDesignerHost desHost = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost)); 
            IDesignerHost localizationDesignerHost = new LocalizationDesignerHost(desHost); 

            ControlDesigner designer = desHost.GetDesigner(control) as ControlDesigner; 
            Control localizationControl = designer.CreateClonedControl(localizationDesignerHost, false);
            ((IControlDesignerAccessor)localizationControl).SetOwnerControl(control);

            bool shouldLocalizeInnerContents = ShouldLocalizeInnerContents(control.Site, control); 
            string newResourceKey = LocalizeControl(localizationControl, localizationDesignerHost, resourceWriter, shouldLocalizeInnerContents);
            if (shouldLocalizeInnerContents) { 
                newInnerContent = ControlSerializer.SerializeInnerContents(localizationControl, localizationDesignerHost); 
            }
            else { 
                newInnerContent = null;
            }
            return newResourceKey;
        } 

        private static string LocalizeControl(Control control, IServiceProvider serviceProvider, IDesignTimeResourceWriter resourceWriter, bool shouldLocalizeInnerContent) { 
#if ORCAS 
            IFilterResolutionService filterService = (IFilterResolutionService)serviceProvider.GetService(typeof(IFilterResolutionService));
            string filter = filterService.CurrentFilter; 
            if (String.Equals(filter, "Default", StringComparison.InvariantCultureIgnoreCase)) {
                filter = String.Empty;
            }
 
            Debug.Assert(filter.Length == 0, "Should always be in default filter mode when localizing!");
#endif 
 
            ResourceExpressionEditor resEditor = (ResourceExpressionEditor)ExpressionEditor.GetExpressionEditor(LocalizationResourceExpressionPrefix, serviceProvider);
 
            IControlBuilderAccessor cba = (IControlBuilderAccessor)control;
            ControlBuilder controlBuilder = cba.ControlBuilder;
            ObjectPersistData persistData = controlBuilder.GetObjectPersistData();
 
            string resourceKey = controlBuilder.GetResourceKey();
            string newResourceKey = LocalizeObject(serviceProvider, control, persistData, resEditor, resourceWriter, resourceKey, String.Empty, control, String.Empty, shouldLocalizeInnerContent, false, false); 
            if (!String.Equals(resourceKey, newResourceKey, StringComparison.OrdinalIgnoreCase)) { 
                controlBuilder.SetResourceKey(newResourceKey);
            } 

            // Localize all bound properties that are persisted as hyphenated names, for
            // example, ItemStyle-BackColor="<%$ foo:bar %>"
            if (persistData != null) { 
                foreach (PropertyEntry entry in persistData.AllPropertyEntries) {
                    BoundPropertyEntry bpe = entry as BoundPropertyEntry; 
                    if (bpe != null && !bpe.Generated) { 
                        string[] parts = bpe.Name.Split(OMDelimiter);
                        if (parts.Length > 1) { 
                            object propValue = control;
                            foreach (string part in parts) {
                                PropertyDescriptor subPropDesc = TypeDescriptor.GetProperties(propValue)[part];
                                // Ignore non-existent properties (i.e. expandos) 
                                if (subPropDesc == null) {
                                    break; 
                                } 
                                PersistenceModeAttribute mode = subPropDesc.Attributes[typeof(PersistenceModeAttribute)] as PersistenceModeAttribute;
                                if (mode != PersistenceModeAttribute.Attribute) { 
                                    // Localize the value

                                    // If it's a page-level resource expression, preserve it's value
                                    if (String.Equals(bpe.ExpressionPrefix, LocalizationResourceExpressionPrefix, StringComparison.OrdinalIgnoreCase)) { 
                                        ResourceExpressionFields fields = bpe.ParsedExpressionData as ResourceExpressionFields;
                                        if (fields != null) { 
                                            if (String.IsNullOrEmpty(fields.ClassKey)) { 
                                                // Page resource
                                                object resourceValue = resEditor.EvaluateExpression(bpe.Expression, bpe.ParsedExpressionData, bpe.PropertyInfo.PropertyType, serviceProvider); 
                                                // Never persist null values since they look bad in the persistence
                                                if (resourceValue == null) {
                                                    object realTarget;
                                                    PropertyDescriptor realPropDesc = ControlDesigner.GetComplexProperty(control, bpe.Name, out realTarget); 
                                                    resourceValue = realPropDesc.GetValue(realTarget);
                                                } 
                                                resourceWriter.AddResource(fields.ResourceKey, resourceValue); 
                                            }
                                        } 
                                    }
                                    break;
                                }
                                propValue = subPropDesc.GetValue(propValue); 
                            }
                        } 
                    } 
                }
            } 

            return newResourceKey;
        }
 
        /// <devdoc>
        /// Indicates whether or not the Controls collection of a control should 
        /// be localized or not. For example, a Panel has child controls but they 
        /// are top-level controls in the designer because it is not an INamingContainer,
        /// so the designer will call Localize() on each one. However, a Label can also 
        /// have child controls but they are not top-level controls, so we have to
        /// localize them.
        /// </devdoc>
        private static bool ShouldLocalizeInnerContents(IServiceProvider serviceProvider, object obj) { 
            Control c = obj as Control;
            if (c == null) { 
                return false; 
            }
            IDesignerHost desHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            if (desHost == null) {
                return false;
            }
            ControlDesigner designer = desHost.GetDesigner(c) as ControlDesigner; 
            if (designer != null) {
                if (!designer.ReadOnlyInternal) { 
                    return false; 
                }
            } 
            return true;
        }

        private static bool ParseChildren(Type controlType) { 
            object[] attrs = controlType.GetCustomAttributes(typeof(ParseChildrenAttribute), true);
            if (attrs != null && attrs.Length > 0) { 
                return ((ParseChildrenAttribute)attrs[0]).ChildrenAsProperties; 
            }
            return false; 
        }

        private static string LocalizeObject(IServiceProvider serviceProvider, object obj, ObjectPersistData persistData,
            ResourceExpressionEditor resEditor, IDesignTimeResourceWriter resourceWriter, string resourceKey, string objectModelName, 
            object topLevelObject, string filter, bool shouldLocalizeInnerContent, bool isComplexProperty, bool implicitlyLocalizeComplexProperty) {
 
            bool doImplicitLocalization; 
            if (isComplexProperty) {
                // If it's a complex property we need to know whether we localize implicitly based on our parent 
                doImplicitLocalization = implicitlyLocalizeComplexProperty;
            }
            else {
                // If it's not a complex property, just follow whatever the persist data says. 
                doImplicitLocalization = ((persistData == null) || persistData.Localize);
            } 
 
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj);
 
            for (int i = 0; i < properties.Count; i++) {
                try {
                    PropertyDescriptor propDesc = properties[i];
 
                    if (String.Equals(propDesc.Name, "Controls", StringComparison.Ordinal)) {
                        Control c = obj as Control; 
                        if (c != null && shouldLocalizeInnerContent) { 
                            if (!ParseChildren(c.GetType())) {
                                ControlCollection controls = c.Controls; 
                                foreach (Control childControl in controls) {
                                    IControlBuilderAccessor cba = (IControlBuilderAccessor)childControl;
                                    ControlBuilder builder = cba.ControlBuilder;
                                    if (builder != null) { 
                                        // Use the existing resource key or create a new one
                                        string childResourceKey = builder.GetResourceKey(); 
                                        string newChildResourceKey = LocalizeObject(serviceProvider, childControl, builder.GetObjectPersistData(), resEditor, resourceWriter, childResourceKey, String.Empty, childControl, String.Empty, true, false, false); 
                                        if (!String.Equals(childResourceKey, newChildResourceKey, StringComparison.OrdinalIgnoreCase)) {
                                            builder.SetResourceKey(newChildResourceKey); 
                                        }
                                    }
                                }
                            } 
                            continue;
                        } 
                    } 

                    PersistenceModeAttribute persistenceMode = (PersistenceModeAttribute)propDesc.Attributes[typeof(PersistenceModeAttribute)]; 

                    string newObjectModelName = (objectModelName.Length > 0) ? (objectModelName + OMDelimiter + propDesc.Name) : propDesc.Name;

                    // For complex properties persisted as attribute and with designer visibility as content 
                    // recurse in and use '.' syntax (ie TreeViewResource1.Font.Bold)
                    if ((persistenceMode.Mode == PersistenceMode.Attribute) && (propDesc.SerializationVisibility == DesignerSerializationVisibility.Content)) { 
                        resourceKey = LocalizeObject(serviceProvider, propDesc.GetValue(obj), persistData, resEditor, resourceWriter, 
                                                     resourceKey,
                                                     newObjectModelName, 
                                                     topLevelObject,
                                                     filter, true, true, doImplicitLocalization);
                        continue;
                    } 

                    // If it's an attribute or a string property 
                    if ((persistenceMode.Mode == PersistenceMode.Attribute) || (propDesc.PropertyType == typeof(string))) { 
                        bool pushValue = false;
                        bool havePropValue = false; 
                        object propValue = null;

                        string newResourceKey = String.Empty;
 
                        // Check if the property is bound
                        if (persistData != null) { 
                            PropertyEntry entry = persistData.GetFilteredProperty(String.Empty, newObjectModelName); 
                            if (entry is BoundPropertyEntry) {
                                BoundPropertyEntry bpe = (BoundPropertyEntry)entry; 
                                if (!bpe.Generated) {
                                    // If it's a page-level resource expression, preserve it's value
                                    if (String.Equals(bpe.ExpressionPrefix, LocalizationResourceExpressionPrefix, StringComparison.OrdinalIgnoreCase)) {
                                        ResourceExpressionFields fields = bpe.ParsedExpressionData as ResourceExpressionFields; 
                                        if (fields != null) {
                                            if (String.IsNullOrEmpty(fields.ClassKey)) { 
                                                // Page resource 
                                                newResourceKey = fields.ResourceKey;
                                                propValue = resEditor.EvaluateExpression(bpe.Expression, bpe.ParsedExpressionData, propDesc.PropertyType, serviceProvider); 
                                                if (propValue != null) {
                                                    // We only say we have a property value if it is not null.
                                                    // Otherwise we get the property's live value later.
                                                    havePropValue = true; 
                                                }
                                                pushValue = true; 
                                            } 
                                        }
                                    } 
                                }
                                else {
                                    // Property is bound but is auto-generated, so we want to
                                    // push the current value of the property into the resource 
                                    // file such that it will match the current value of the control.
                                    // There is no need to check the Localizable attribute since we 
                                    // always re-push values of previously localized properties. 
                                    pushValue = true;
                                } 
                            }
                            else {
                                // Property is declared on the tag, but doesn't use a resource
                                // expression, so we push only if it is localizable. 
                                pushValue = doImplicitLocalization && IsPropertyLocalizable(propDesc);
                            } 
                        } 
                        else {
                            // Property is not present on the tag, so we push only if 
                            // it is localizable.
                            pushValue = doImplicitLocalization && IsPropertyLocalizable(propDesc);
                        }
 
                        if (pushValue) {
                            if (!havePropValue) { 
                                propValue = propDesc.GetValue(obj); 
                            }
 
                            // Create a resource key for this control if it doesn't already have one
                            if (newResourceKey.Length == 0) {
                                if (String.IsNullOrEmpty(resourceKey)) {
                                    resourceKey = resourceWriter.CreateResourceKey(null, topLevelObject); 
                                }
                                newResourceKey = resourceKey + objectDelimiter + newObjectModelName; 
 
                                if (filter.Length != 0) {
                                    newResourceKey = filter + filterDelimiter + newResourceKey; 
                                }
                            }
                            // Push the value to the page resources
                            resourceWriter.AddResource(newResourceKey, propValue); 
                        }
 
                        // Push values for any filtered properties 
                        if (persistData != null) {
                            ICollection filteredProperties = persistData.GetPropertyAllFilters(newObjectModelName); 

                            foreach (PropertyEntry filteredEntry in filteredProperties) {
                                if (filteredEntry.Filter.Length > 0) {
                                    if (filteredEntry is SimplePropertyEntry) { 
                                        // For simple properties (e.g. Text="hello") we push the
                                        // literal value if the property is localizable 
                                        if (doImplicitLocalization && IsPropertyLocalizable(propDesc)) { 
                                            if (newResourceKey.Length == 0) {
                                                if (String.IsNullOrEmpty(resourceKey)) { 
                                                    resourceKey = resourceWriter.CreateResourceKey(null, topLevelObject);
                                                }
                                                newResourceKey = resourceKey + objectDelimiter + newObjectModelName;
                                            } 
                                            string filterResourceKey = filteredEntry.Filter + filterDelimiter + newResourceKey;
                                            resourceWriter.AddResource(filterResourceKey, ((SimplePropertyEntry)filteredEntry).Value); 
                                        } 
                                    }
                                    else { 
                                        if (filteredEntry is ComplexPropertyEntry) {
                                            Debug.Fail("When does this happen?");
                                        }
                                        else { 
                                            if (filteredEntry is BoundPropertyEntry) {
                                                // For bound properties (e.g. Text="<%$ expression:value %>") we push 
                                                // the value only if it is a resource expression. 
                                                BoundPropertyEntry bpe = (BoundPropertyEntry)filteredEntry;
                                                if (!bpe.Generated) { 
                                                    // If it's a page-level resource expression, preserve it's value
                                                    if (String.Equals(bpe.ExpressionPrefix, LocalizationResourceExpressionPrefix, StringComparison.OrdinalIgnoreCase)) {
                                                        ResourceExpressionFields fields = bpe.ParsedExpressionData as ResourceExpressionFields;
                                                        if (fields != null) { 
                                                            if (String.IsNullOrEmpty(fields.ClassKey)) {
                                                                // Page resource 
                                                                object filteredPropValue = resEditor.EvaluateExpression(bpe.Expression, bpe.ParsedExpressionData, filteredEntry.PropertyInfo.PropertyType, serviceProvider); 
                                                                // Never persist null values since they look bad in the persistence
                                                                if (filteredPropValue == null) { 
                                                                    filteredPropValue = String.Empty;
                                                                }
                                                                resourceWriter.AddResource(fields.ResourceKey, filteredPropValue);
                                                            } 
                                                        }
                                                    } 
                                                } 
                                            }
                                        } 
                                    }
                                }
                            }
                        } 

                        continue; 
                    } 

                    if (!shouldLocalizeInnerContent) { 
                        continue;
                    }

                    // For a collection, generate (or re-use) a new resource key for each collection item 
                    if (typeof(ICollection).IsAssignableFrom(propDesc.PropertyType)) {
                        // Localize all filtered collections 
                        if (persistData != null) { 
                            ICollection filteredProperties = persistData.GetPropertyAllFilters(propDesc.Name);
                            foreach (ComplexPropertyEntry entry in filteredProperties) { 
                                ObjectPersistData collectionPersistData = entry.Builder.GetObjectPersistData();
                                foreach (ComplexPropertyEntry cpe in collectionPersistData.CollectionItems) {
                                    Debug.Assert(cpe.IsCollectionItem, "Expected a collection item- how did this get in there?");
                                    ControlBuilder builder = cpe.Builder; 
                                    object current = builder.BuildObject();
 
                                    // Use the existing resource key or create a new one 
                                    string childResourceKey = builder.GetResourceKey();
                                    string newChildResourceKey = LocalizeObject(serviceProvider, current, builder.GetObjectPersistData(), resEditor, resourceWriter, childResourceKey, String.Empty, current, String.Empty, true, false, false); 
                                    if (!String.Equals(childResourceKey, newChildResourceKey, StringComparison.OrdinalIgnoreCase)) {
                                        builder.SetResourceKey(newChildResourceKey);
                                    }
                                } 
                            }
                        } 
 
                        continue;
                    } 

                    // For a template, parse the template, walk the child controls, localize them, then re-persist with
                    // resource keys
                    if (typeof(ITemplate).IsAssignableFrom(propDesc.PropertyType)) { 
                        // Localize all filtered templates
                        if (persistData != null) { 
                            ICollection filteredProperties = persistData.GetPropertyAllFilters(propDesc.Name); 
                            foreach (TemplatePropertyEntry entry in filteredProperties) {
                                TemplateBuilder templateBuilder = (TemplateBuilder)entry.Builder; 

                                IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                                Debug.Assert(designerHost != null);
 
                                Control[] controls = ControlParser.ParseControls(designerHost, templateBuilder.Text);
                                for (int j = 0; j < controls.Length; j++) { 
                                    if (!(controls[j] is LiteralControl) && !(controls[j] is DesignerDataBoundLiteralControl)) { 
                                        LocalizeControl(controls[j], serviceProvider, resourceWriter, true);
                                    } 
                                }

                                StringBuilder newTemplateText = new StringBuilder();
                                for (int j = 0; j < controls.Length; j++) { 
                                    if (controls[j] is LiteralControl) {
                                        newTemplateText.Append(((LiteralControl)controls[j]).Text); 
                                    } 
                                    else {
                                        newTemplateText.Append(ControlPersister.PersistControl(controls[j], designerHost)); 
                                    }
                                }

                                templateBuilder.Text = newTemplateText.ToString(); 
                            }
                        } 
 
                        continue;
                    } 

                    // Ordinary complex properties are handled using '.' syntax (ie TreeViewResource1.NodeStyle.BackColor)
                    {
                        if (persistData != null) { 
                            object childObject = propDesc.GetValue(obj);
                            ObjectPersistData childPersistData = null; 
                            ComplexPropertyEntry cpe = (ComplexPropertyEntry)persistData.GetFilteredProperty(String.Empty, propDesc.Name); 
                            if (cpe != null) {
                                childPersistData = cpe.Builder.GetObjectPersistData(); 
                            }

                            resourceKey = LocalizeObject(serviceProvider, childObject, childPersistData, resEditor, resourceWriter, resourceKey, newObjectModelName, topLevelObject, String.Empty, true, true, doImplicitLocalization);
 
                            ICollection filteredProperties = persistData.GetPropertyAllFilters(propDesc.Name);
                            foreach (ComplexPropertyEntry entry in filteredProperties) { 
                                if (entry.Filter.Length > 0) { 
                                    ControlBuilder builder = entry.Builder;
                                    childPersistData = builder.GetObjectPersistData(); 
                                    childObject = builder.BuildObject();

                                    resourceKey = LocalizeObject(serviceProvider, childObject, childPersistData, resEditor, resourceWriter, resourceKey, newObjectModelName, topLevelObject, entry.Filter, true, true, doImplicitLocalization);
                                } 
                            }
                        } 
                    } 
                }
                catch (Exception e) { 
                    if (serviceProvider != null) {
                        // Log the error with the IComponentDesignerDebugService
                        IComponentDesignerDebugService debugService = serviceProvider.GetService(typeof(IComponentDesignerDebugService)) as IComponentDesignerDebugService;
                        if (debugService != null) { 
                            debugService.Fail(e.Message);
                        } 
                    } 
                }
            } 

            return resourceKey;
        }
 

        /// <devdoc> 
        /// Dummy implementation of a designer host. This is needed so that we can pass 
        /// in our own filter resolution service that pretends that it is always in the
        /// default filter mode. 
        /// All calls are delegated to the real designer host except for GetService(),
        /// which is special-cased to return our own IFilterResolutionService.
        /// </devdoc>
        private sealed class LocalizationDesignerHost : IDesignerHost { 

            private IDesignerHost _parentHost; 
            private LocalizationFilterResolutionService _localizationFilterService; 

            internal LocalizationDesignerHost(IDesignerHost parentHost) { 
                _parentHost = parentHost;
            }

            #region Implementation of IDesignerHost 
            IContainer IDesignerHost.Container {
                get { 
                    return _parentHost.Container; 
                }
            } 

            bool IDesignerHost.InTransaction {
                get {
                    return _parentHost.InTransaction; 
                }
            } 
 
            bool IDesignerHost.Loading {
                get { 
                    return _parentHost.Loading;
                }
            }
 
            string IDesignerHost.TransactionDescription {
                get { 
                    return _parentHost.TransactionDescription; 
                }
            } 

            IComponent IDesignerHost.RootComponent {
                get {
                    return _parentHost.RootComponent; 
                }
            } 
 
            string IDesignerHost.RootComponentClassName {
                get { 
                    return _parentHost.RootComponentClassName;
                }
            }
 
            event EventHandler IDesignerHost.Activated {
                add { 
                    _parentHost.Activated += value; 
                }
                remove { 
                    _parentHost.Activated -= value;
                }
            }
 
            event EventHandler IDesignerHost.Deactivated {
                add { 
                    _parentHost.Deactivated += value; 
                }
                remove { 
                    _parentHost.Deactivated -= value;
                }
            }
 
            event EventHandler IDesignerHost.LoadComplete {
                add { 
                    _parentHost.LoadComplete += value; 
                }
                remove { 
                    _parentHost.LoadComplete -= value;
                }
            }
 
            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosed {
                add { 
                    _parentHost.TransactionClosed += value; 
                }
                remove { 
                    _parentHost.TransactionClosed -= value;
                }
            }
 
            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosing {
                add { 
                    _parentHost.TransactionClosing += value; 
                }
                remove { 
                    _parentHost.TransactionClosing -= value;
                }
            }
 
            event EventHandler IDesignerHost.TransactionOpened {
                add { 
                    _parentHost.TransactionOpened += value; 
                }
                remove { 
                    _parentHost.TransactionOpened -= value;
                }
            }
 
            event EventHandler IDesignerHost.TransactionOpening {
                add { 
                    _parentHost.TransactionOpening += value; 
                }
                remove { 
                    _parentHost.TransactionOpening -= value;
                }
            }
 
            void IDesignerHost.Activate() {
            } 
 
            IComponent IDesignerHost.CreateComponent(Type componentType) {
                return _parentHost.CreateComponent(componentType); 
            }

            IComponent IDesignerHost.CreateComponent(Type componentType, string name) {
                return _parentHost.CreateComponent(componentType, name); 
            }
 
            DesignerTransaction IDesignerHost.CreateTransaction() { 
                return _parentHost.CreateTransaction();
            } 

            DesignerTransaction IDesignerHost.CreateTransaction(string description) {
                return _parentHost.CreateTransaction(description);
            } 

            void IDesignerHost.DestroyComponent(IComponent component) { 
                _parentHost.DestroyComponent(component); 
            }
 
            Type IDesignerHost.GetType(string typeName) {
                return _parentHost.GetType(typeName);
            }
 
            IDesigner IDesignerHost.GetDesigner(IComponent component) {
                return _parentHost.GetDesigner(component); 
            } 

            void IServiceContainer.RemoveService(Type serviceType, bool promote) { 
                _parentHost.RemoveService(serviceType, promote);
            }

            void IServiceContainer.RemoveService(Type serviceType) { 
                _parentHost.RemoveService(serviceType);
            } 
 
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) {
                _parentHost.AddService(serviceType, callback, promote); 
            }

            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) {
                _parentHost.AddService(serviceType, callback); 
            }
 
            void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) { 
                _parentHost.AddService(serviceType, serviceInstance, promote);
            } 

            void IServiceContainer.AddService(Type serviceType, object serviceInstance) {
                _parentHost.AddService(serviceType, serviceInstance);
            } 

            object IServiceProvider.GetService(Type serviceType) { 
                if (serviceType == typeof(IFilterResolutionService)) { 
                    if (_localizationFilterService == null) {
                        IFilterResolutionService realFilterService = (IFilterResolutionService)_parentHost.GetService(typeof(IFilterResolutionService)); 
                        if (realFilterService == null) {
                            throw new InvalidOperationException(SR.GetString(SR.ControlLocalizer_RequiresFilterService));
                        }
                        _localizationFilterService = new LocalizationFilterResolutionService(realFilterService); 
                    }
                    return _localizationFilterService; 
                } 
                return _parentHost.GetService(serviceType);
            } 
            #endregion

            private sealed class LocalizationFilterResolutionService : IFilterResolutionService {
                private IFilterResolutionService _realFilterService; 

                internal LocalizationFilterResolutionService(IFilterResolutionService realFilterService) { 
                    _realFilterService = realFilterService; 
                }
 
#if ORCAS
                string IFilterResolutionService.CurrentFilter {
                    get {
                        // Always return the default filter 
                        return String.Empty;
                    } 
                } 
#endif
 
                int IFilterResolutionService.CompareFilters(string filter1, string filter2) {
                    // Filter comparison is done by the host's filter resolution service
                    return _realFilterService.CompareFilters(filter1, filter2);
                } 

                bool IFilterResolutionService.EvaluateFilter(string filterName) { 
                    // Only the default filter applies to the current state 
                    if ((filterName == null) || (filterName.Length == 0) ||
                        (String.Equals(filterName, "default", StringComparison.OrdinalIgnoreCase))) { 
                        return true;
                    }
                    // All other filters do not apply
                    return false; 
                }
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
