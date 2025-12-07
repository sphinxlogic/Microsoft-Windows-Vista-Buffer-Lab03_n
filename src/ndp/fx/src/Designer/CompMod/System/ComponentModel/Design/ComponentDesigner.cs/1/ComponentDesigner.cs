//------------------------------------------------------------------------------ 
// <copyright file="ComponentDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design {
    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Configuration;
    using System.Collections; 
    using System.Reflection; 
    using System.Windows.Forms;
    using Microsoft.Win32; 
    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel;
    using Microsoft.Internal.Performance;
    using System.Globalization; 

    /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       The default designer for all components. 
    ///    </para>
    /// </devdoc>
    public class ComponentDesigner : ITreeDesigner, IDesignerFilter, IComponentInitializer {
 
        IComponent                      component;
        InheritanceAttribute            inheritanceAttribute; 
        Hashtable                       inheritedProps; 
        DesignerVerbCollection          verbs;
        DesignerActionListCollection    actionLists; 
        ShadowPropertyCollection        shadowProperties;
        bool                            settingsKeyExplicitlySet = false;

        private static CodeMarkers codemarkers = CodeMarkers.Instance; 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.AssociatedComponents"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///        Gets the design-time actionlists supported by the component associated with the designer. 
        ///    </para>
        /// </devdoc>
        public virtual DesignerActionListCollection ActionLists {
            get { 
                if (actionLists== null) {
                    actionLists = new DesignerActionListCollection(); 
                } 
                return actionLists;
            } 
        }


 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.AssociatedComponents"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Retrieves a list of associated components.  These are components that should be incluced in a cut or copy operation on this component.
        ///    </para> 
        /// </devdoc>
        public virtual ICollection AssociatedComponents{
            get {
                return new IComponent[0]; 
            }
        } 
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.AssociatedComponents"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Returns true if this component can be parented to the designer passed in..
        ///    </para>
        /// </devdoc> 
        internal virtual bool CanBeAssociatedWith(IDesigner parentDesigner) {
          return true; 
        } 

 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Component"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the component this designer is designing. 
        ///    </para>
        /// </devdoc> 
        public IComponent Component { 
            get {
                return component; 
            }
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Inherited"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets a value 
        ///       indicating whether or not this component is being inherited.
        ///    </para> 
        /// </devdoc>
        protected bool Inherited {
            get {
                return !InheritanceAttribute.Equals(InheritanceAttribute.NotInherited); 
            }
        } 
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ParentComponent"]/*' />
        /// <devdoc> 
        ///     This property provides a generic mechanism for discovering parent relationships within designers,
        ///     and is used by ComponentDesigner's ITreeDesigner interface implementation.  This property
        ///     should only return null when this designer is the root component.  Otherwise, it should return
        ///     the parent component.  The default implementation of this property returns the root component 
        ///     for all components that are not the root component, and it returns null for the root component.
        /// </devdoc> 
        protected virtual IComponent ParentComponent { 
            get {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                IComponent root = host.RootComponent;
                if (root == Component) {
                    return null;
                } 
                else {
                    return root; 
                } 
            }
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InvokeGetInheritanceAttribute"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Invokes the get inheritance attribute of the specified ComponentDesigner.
        ///    </para> 
        /// </devdoc> 
        protected InheritanceAttribute InvokeGetInheritanceAttribute(ComponentDesigner toInvoke) {
            return toInvoke.InheritanceAttribute; 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InheritanceAttribute"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets or sets the inheritance attribute for this component. 
        ///    </para> 
        /// </devdoc>
        protected virtual InheritanceAttribute InheritanceAttribute { 
            get {
                if (inheritanceAttribute == null) {
                    // Record if this component is being inherited or not.
                    // 
                    IInheritanceService inher = (IInheritanceService)GetService(typeof(IInheritanceService));
                    if (inher != null) { 
                        inheritanceAttribute = inher.GetInheritanceAttribute(Component); 
                    }
                    else { 
                        inheritanceAttribute = InheritanceAttribute.Default;
                    }
                }
 
                return inheritanceAttribute;
            } 
        } 

        /// <devdoc> 
        ///    shadowing the SettingsKey so we can default it to be RootComponent.Name + "." + Control.Name
        /// </devdoc>
        private string SettingsKey {
            get { 
                if (string.IsNullOrEmpty((string) ShadowProperties["SettingsKey"]))  {
                    IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings; 
                    IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                    IComponent rootComponent = host != null ? host.RootComponent : null;
 
                    if (persistableComponent != null && rootComponent != null) {
                        if (String.IsNullOrEmpty(persistableComponent.SettingsKey)) {
                            if (rootComponent != null && rootComponent != persistableComponent) {
                                ShadowProperties["SettingsKey"] = String.Format(CultureInfo.CurrentCulture, "{0}.{1}", rootComponent.Site.Name, Component.Site.Name); 
                            }
                            else { 
                                ShadowProperties["SettingsKey"] = Component.Site.Name; 
                            }
                        } 

                        persistableComponent.SettingsKey =  ShadowProperties["SettingsKey"] as string;
                        return persistableComponent.SettingsKey;
                    } 
                }
 
                return ShadowProperties["SettingsKey"] as string; 
            }
            set { 
                 ShadowProperties["SettingsKey"] = value;
                 settingsKeyExplicitlySet = true;
                 IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings;
                 if (persistableComponent != null) { 
                     persistableComponent.SettingsKey =  value;
                 } 
 
            }
 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowProperties"]/*' />
        /// <devdoc> 
        ///     Gets a collection that houses shadow properties.  Shadow properties. are properties that fall
        ///     through to the underlying component before they are set, but return their set values once they 
        ///     are set. 
        /// </devdoc>
        protected ShadowPropertyCollection ShadowProperties { 
            get {
                if (shadowProperties == null) {
                    shadowProperties = new ShadowPropertyCollection(this);
                } 
                return shadowProperties;
            } 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Verbs"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the design-time verbs supported by the component associated with the designer.
        ///    </para> 
        /// </devdoc>
        public virtual DesignerVerbCollection Verbs { 
            get { 
                if (verbs == null) {
                    verbs = new DesignerVerbCollection(); 
                }
                return verbs;
            }
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.OnComponentRename"]/*' /> 
        /// <devdoc> 
        ///      Raised when a component's name changes.  Here we update the SettingsKey property if necessary.
        /// </devdoc> 
        private void OnComponentRename(object sender, ComponentRenameEventArgs e)
        {
            if (Component is IPersistComponentSettings) {
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                IComponent rootComponent = host != null ? host.RootComponent : null;
 
                // SettingsKey is formed based on the name of the component and the root component. If either of 
                // these change, we reset settings key (if it hasn't been explicitly set) so it can be recomputed.
                if (!settingsKeyExplicitlySet && (e.Component == Component || e.Component == rootComponent)) { 
                    ResetSettingsKey();
                }
            }
        } 

 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Disposes of the resources (other than memory) used
        ///       by the <see cref='System.ComponentModel.Design.ComponentDesigner'/>.
        ///    </para>
        /// </devdoc> 
        public void Dispose() {
            Dispose(true); 
            GC.SuppressFinalize(this); 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for=".Finalize"]/*' />
        ~ComponentDesigner() {
            Dispose(false);
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Dispose2"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Disposes of the resources (other than memory) used 
        ///       by the <see cref='System.ComponentModel.Design.ComponentDesigner'/>.
        ///    </para>
        /// </devdoc>
        protected virtual void Dispose(bool disposing) { 
            if (disposing) {
                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                if (cs != null) 
                {
                    cs.ComponentRename -= new ComponentRenameEventHandler(this.OnComponentRename); 
                }

                component = null;
            } 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.DoDefaultAction"]/*' /> 
        /// <devdoc>
        ///    <para>Creates a method signature in the source code file for the default event on the component and navigates 
        ///       the user's cursor to that location in preparation to assign
        ///       the default action.</para>
        /// </devdoc>
        public virtual void DoDefaultAction() { 
            IEventBindingService eps = (IEventBindingService)GetService(typeof(IEventBindingService));
 
            //If the event binding service is not available, there is nothing much we can do, so just return. 
            if (eps == null) {
                return; 
            }

            ISelectionService selectionService = (ISelectionService)GetService(typeof(ISelectionService));
            if (selectionService == null) { 
                return;
            } 
 
            ICollection components = selectionService.GetSelectedComponents();
 

            EventDescriptor thisDefaultEvent = null;
            string          thisHandler = null;
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            DesignerTransaction t = null; 
 
            try {
                foreach(object comp in components) { 

                    if (!(comp is IComponent)) {
                        continue;
                    } 

                    EventDescriptor defaultEvent = TypeDescriptor.GetDefaultEvent(comp); 
                    PropertyDescriptor defaultPropEvent = null; 
                    string handler = null;
                    bool eventChanged = false; 

                    if (defaultEvent != null) {
                        defaultPropEvent = eps.GetEventProperty(defaultEvent);
                    } 

                    // If we couldn't find a property for this event, or of the property is read only, then 
                    // abort. 
                    //
                    if (defaultPropEvent == null || defaultPropEvent.IsReadOnly) { 
                        continue;
                    }

                    try { 
                        if (host != null && t == null) {
                            t = host.CreateTransaction(SR.GetString(SR.ComponentDesignerAddEvent, defaultEvent.Name)); 
                        } 
                    }
                    catch (CheckoutException cxe) { 
                        if (cxe == CheckoutException.Canceled)
                            return;

                        throw cxe; 
                    }
 
                    // handler will be null if there is no explicit event hookup in the parsed init method 
                    handler = (string)defaultPropEvent.GetValue(comp);
 
                    if (handler == null) {
                        eventChanged = true;

                        handler = eps.CreateUniqueMethodName((IComponent)comp, defaultEvent); 
                    }
                    else { 
                        // ensure the handler is still there 
                        eventChanged = true;
                        foreach(string compatibleMethod in eps.GetCompatibleMethods(defaultEvent)) { 
                            if (handler == compatibleMethod) {
                                eventChanged = false;
                                break;
                            } 
                        }
                    } 
 
                    // Save the new value... BEFORE navigating to it!
                    // 
 					codemarkers.CodeMarker(CodeMarkerEvent.perfFXBindEventDesignToCode);
                    if (eventChanged && defaultPropEvent != null) {
                       defaultPropEvent.SetValue(comp, handler);
                    } 

                    if (component == comp){ 
                        thisDefaultEvent = defaultEvent; 
                        thisHandler = handler;
                    } 
                }
            }
            catch (InvalidOperationException) {
                // VSWhidbey 434433 - If the doc data for this designer is currently locked, we will get an exception here. 
                // In this case, just eat the exception.
 
                if (t != null) { 
                    t.Cancel();
                    t = null; 
                }
            }
            finally {
                if (t != null) { 
                    t.Commit();
                } 
            } 

            // Now show the event code. 
            //
            if (thisHandler != null && thisDefaultEvent != null) {
                eps.ShowCode(component, thisDefaultEvent);
            } 
        }
 
        internal bool IsRootDesigner { 
            get {
                Debug.Assert(this.component != null, 
                    "this.component needs to be set before this method is valid.");

                bool isRoot = false;
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                if (host != null && this.component == host.RootComponent) {
                    isRoot = true; 
                } 
                return isRoot;
            } 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.ComponentDesigner'/> 
        ///       class using the specified component. 
        ///    </para>
        /// </devdoc> 
        public virtual void Initialize(IComponent component) {

            Debug.Assert(component != null, "Can't create designer with no component!");
 
            this.component = component;
 
            // For inherited components, save off the current values so we can 
            // compute a delta.  We also do this for the root component, but,
            // as it is ALWAYS inherited, the computation of default values 
            // favors the presence of a default value attribute over
            // the current code value.
            //
            bool isRoot = false; 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host != null && component == host.RootComponent) { 
                isRoot = true; 
            }
 

            //Only add the service if it is not already added. VSWhidbey #395676.
            IServiceContainer sc = component.Site as IServiceContainer;
            if (sc != null && GetService(typeof(DesignerCommandSet)) == null) { 
                sc.AddService(typeof(DesignerCommandSet), new CDDesignerCommandSet(this));
            } 
 
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (cs != null) 
            {
                cs.ComponentRename += new ComponentRenameEventHandler(this.OnComponentRename);
            }
 
            if (isRoot || !InheritanceAttribute.Equals(InheritanceAttribute.NotInherited)) {
                InitializeInheritedProperties(isRoot); 
            } 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InitializeExistingComponent"]/*' />
        /// <devdoc>
        ///     This method is called when an existing component is being re-initialized.  This may occur after
        ///     dragging a component to another container, for example.  The defaultValues 
        ///     property contains a name/value dictionary of default values that should be applied
        ///     to properties. This dictionary may be null if no default values are specified. 
        ///     You may use the defaultValues dictionary to apply recommended defaults to proeprties 
        ///     but you should not modify component properties beyond what is stored in the
        ///     dictionary, because this is an existing component that may already have properties 
        ///     set on it.
        ///
        ///     The default implemenation of this method does nothing.
        /// </devdoc> 
        public virtual void InitializeExistingComponent(IDictionary defaultValues) {
#pragma warning disable 618 
            // execute legacy code 
            InitializeNonDefault();
#pragma warning restore 618 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InitializeNewComponent"]/*' />
        /// <devdoc> 
        ///     This method is called when a component is first initialized, typically after being first added
        ///     to a design surface.  The defaultValues property contains a name/value dictionary of default 
        ///     values that should be applied to properties.  This dictionary may be null if no default values 
        ///     are specified.  You may perform any initialization of this component that you like, and you
        ///     may even ignore the defaultValues dictionary altogether if you wish. 
        ///
        ///     The default implemenation of this method does nothing.
        /// </devdoc>
        public virtual void InitializeNewComponent(IDictionary defaultValues) { 

            // We have just dropped a new component to the form, if this component 
            // has a designerActionPanel available, we need to see if he wants to auto 
            // show the panel on drop, for that we look if any of the designeractionlist
            // has it's autoshow bit set to true, if that's the case we call showdesignerpanel 
            // on the service, that's gonna fire an event so that the UI can try to show the panel
            DesignerActionUIService daUIService = (DesignerActionUIService)GetService(typeof(DesignerActionUIService));
            if (daUIService != null) {
                if(daUIService.ShouldAutoShow(Component)) { 
                        IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
                        if(designerHost != null && designerHost.InTransaction) { 
                            designerHost.TransactionClosed += new DesignerTransactionCloseEventHandler(ShowDesignerActionUI); 
                        } else {
                            daUIService.ShowUI(Component); 
                        }
                }
            }
#pragma warning disable 618 
            // execute legacy code
            OnSetComponentDefaults(); 
#pragma warning restore 618 
        }
 
        private void ShowDesignerActionUI(object sender, DesignerTransactionCloseEventArgs e) {
            DesignerActionUIService daUIService = (DesignerActionUIService)GetService(typeof(DesignerActionUIService));
            if (daUIService != null) {
                daUIService.ShowUI(Component); 
            }
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            if(designerHost != null) { 
                designerHost.TransactionClosed -= new DesignerTransactionCloseEventHandler(ShowDesignerActionUI);
            } 
        }


        private void InitializeInheritedProperties(bool rootComponent) { 

            Hashtable props = new Hashtable(); 
 
            bool readOnlyInherit = (InheritanceAttribute.Equals(InheritanceAttribute.InheritedReadOnly));
 
            if (!readOnlyInherit) {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(Component);

                // Now loop through all the properties.  For each one, try to match a pre-created property. 
                // If that fails, then create a new property.
                // 
                PropertyDescriptor[] values = new PropertyDescriptor[properties.Count]; 
                properties.CopyTo(values, 0);
 
                for (int i = 0; i < values.Length; i++) {
                    PropertyDescriptor prop = values[i];

                    // Skip some properties 
                    //
                    if (object.Equals(prop.Attributes[typeof(DesignOnlyAttribute)], DesignOnlyAttribute.Yes)) { 
                        continue; 
                    }
 
                    if (prop.SerializationVisibility == DesignerSerializationVisibility.Hidden && !prop.IsBrowsable) {
                        continue;
                    }
 
                    PropertyDescriptor inheritedProp = (PropertyDescriptor)props[prop.Name];
 
                    if (inheritedProp == null) { 
                        // This ia a publicly inherited component.  We replace all component properties with
                        // inherited versions that reset the default property values to those that are 
                        // currently on the component.
                        //
                        props[prop.Name] = new InheritedPropertyDescriptor(prop, component, rootComponent);
                    } 
                }
            } 
 
            inheritedProps = props;
            TypeDescriptor.Refresh(Component); // force TypeDescriptor to re-query us. 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InitializeNonDefault"]/*' />
        /// <devdoc> 
        ///    <para>Called when the designer has been associated with a control that is not in it's default state,
        ///          such as one that has been pasted or drag-dropped onto the designer.  This is an opportunity 
        ///          to fixup any shadowed properties in a different way than for default components.  This is called 
        ///          after the other initialize functions.
        ///     </para> 
        /// </devdoc>
        [Obsolete("This method has been deprecated. Use InitializeExistingComponent instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual void InitializeNonDefault() {
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.GetService"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Provides 
        ///       a way for a designer to get services from the hosting
        ///       environment.
        ///    </para>
        /// </devdoc> 
        protected virtual object GetService(Type serviceType) {
            if (component != null) { 
                ISite site = component.Site; 
                if (site != null) {
                    return site.GetService(serviceType); 
                }
            }
            return null;
        } 

        private Attribute[] NonBrowsableAttributes(EventDescriptor e) { 
                Attribute[] attrs = new Attribute[e.Attributes.Count]; 
                e.Attributes.CopyTo(attrs, 0);
 
                for (int i = 0; i < attrs.Length; i++) {
                    if (attrs[i] != null && typeof(BrowsableAttribute).IsInstanceOfType(attrs[i]) && ((BrowsableAttribute)attrs[i]).Browsable) {
                        attrs[i] = BrowsableAttribute.No;
                        return attrs; 
                    }
                } 
 
                // we didn't find it, we have to alloc a new array
                Attribute[] newAttrs = new Attribute[attrs.Length+1]; 
                Array.Copy(attrs, 0, newAttrs, 0, attrs.Length);
                newAttrs[attrs.Length] = BrowsableAttribute.No;
                return newAttrs;
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.OnSetComponentDefaults"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Raises the SetComponentDefault event. 
        ///    </para>
        /// </devdoc>
        [Obsolete("This method has been deprecated. Use InitializeNewComponent instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual void OnSetComponentDefaults() { 
            // COMPAT: The following code shipped in Everett, so we need to continue to do this.
            // See VSWhidbey #419610 for details. 
 
            ISite site = Component.Site;
            if (site != null) { 
                IComponent component = Component;
                PropertyDescriptor pd = TypeDescriptor.GetDefaultProperty(component);
                if (pd != null && pd.PropertyType.Equals(typeof(string))) {
 
                    string current = (string)pd.GetValue(component);
                    if (current == null || current.Length == 0) { 
                        pd.SetValue(component, site.Name); 
                    }
                } 
            }
        }

 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnContextMenu"]/*' />
        /// <devdoc> 
        ///     Called when the context menu should be displayed 
        /// </devdoc>
        internal virtual void ShowContextMenu(int x, int y) { 
            IMenuCommandService mcs = (IMenuCommandService)GetService(typeof(IMenuCommandService));
            if (mcs != null) {
                mcs.ShowContextMenu(MenuCommands.SelectionMenu, x, y);
            } 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PostFilterAttributes"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Allows a
        ///       designer to filter the set of member attributes the
        ///       component it is designing will expose through the
        ///       TypeDescriptor object. 
        ///    </para>
        /// </devdoc> 
        protected virtual void PostFilterAttributes(IDictionary attributes) { 

            // If this component is being inherited, mark it as such in the class attributes. 
            //

            // VSWhidbey #424845 - if the InheritanceAttribute is set in attributes, then don't override it
            // Also, set our member variable to ensure that what you get by querying through the TypeDescriptor and 
            // through InheritanceAttribute directly is the same.
            if (attributes.Contains(typeof(InheritanceAttribute))) { 
                inheritanceAttribute = attributes[typeof(InheritanceAttribute)] as InheritanceAttribute; 
            }
            else if (!InheritanceAttribute.Equals(InheritanceAttribute.NotInherited)) { 
                attributes[typeof(InheritanceAttribute)] = InheritanceAttribute;
            }
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PostFilterEvents"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Allows
        ///       a designer to filter the set of events the 
        ///       component it is designing will expose through the
        ///       TypeDescriptor object.
        ///    </para>
        /// </devdoc> 
        protected virtual void PostFilterEvents(IDictionary events) {
 
            // If this component is being privately inherited, we need to filter 
            // the events to make them read-only.
            // 
            if (InheritanceAttribute.Equals(InheritanceAttribute.InheritedReadOnly)) {
                EventDescriptor[] values = new EventDescriptor[events.Values.Count];
                events.Values.CopyTo(values, 0);
 
                for (int i = 0; i < values.Length; i++) {
                    EventDescriptor evt = values[i]; 
                    events[evt.Name] = TypeDescriptor.CreateEvent(evt.ComponentType, evt, ReadOnlyAttribute.Yes); 
                }
            } 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PostFilterProperties"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Allows 
        ///       a designer to filter the set of properties the 
        ///       component it is designing will expose through the
        ///       TypeDescriptor object. 
        ///    </para>
        /// </devdoc>
        protected virtual void PostFilterProperties(IDictionary properties) {
 
            // Check for inheritance
            // 
            if (inheritedProps != null) { 

                bool readOnlyInherit = (InheritanceAttribute.Equals(InheritanceAttribute.InheritedReadOnly)); 

                if (readOnlyInherit) {
                    // Now loop through all the properties.  For each one, try to match a pre-created property.
                    // If that fails, then create a new property. 
                    //
                    PropertyDescriptor[] values = new PropertyDescriptor[properties.Values.Count]; 
                    properties.Values.CopyTo(values, 0); 

                    for (int i = 0; i < values.Length; i++) { 
                        PropertyDescriptor prop = values[i];
                          // This is a private component.  Therefore, the user should not be
                          // allowed to modify any properties.  We replace all properties with
                          // read-only versions. 
                          //
                          properties[prop.Name] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, ReadOnlyAttribute.Yes); 
                    } 
                }
                else { 
                    // otherwise apply our inherited properties to the actual property list.
                    //
                    foreach (DictionaryEntry de in inheritedProps) {
                        InheritedPropertyDescriptor inheritedPropDesc = de.Value as InheritedPropertyDescriptor; 

                        if (inheritedPropDesc != null) { 
                            // replace the property descriptor it was created 
                            // with with the new one in case we're shadowing
                            // 
                            PropertyDescriptor newInnerProp = (PropertyDescriptor)properties[de.Key];
                            if (newInnerProp != null) {
                                inheritedPropDesc.PropertyDescriptor = newInnerProp;
                                properties[de.Key] = inheritedPropDesc; 
                            }
                        } 
                    } 
                }
            } 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PreFilterAttributes"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Allows a designer 
        ///       to filter the set of member attributes the component 
        ///       it is designing will expose through the TypeDescriptor
        ///       object. 
        ///    </para>
        /// </devdoc>
        protected virtual void PreFilterAttributes(IDictionary attributes) {
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PreFilterEvents"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Allows a 
        ///       designer to filter the set of events the component
        ///       it is designing will expose through the TypeDescriptor
        ///       object.
        ///    </para> 
        /// </devdoc>
        protected virtual void PreFilterEvents(IDictionary events) { 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Allows a
        ///       designer to filter the set of properties the component 
        ///       it is designing will expose through the TypeDescriptor
        ///       object. 
        ///    </para> 
        /// </devdoc>
        protected virtual void PreFilterProperties(IDictionary properties) { 
            if (Component is IPersistComponentSettings) {
                 PropertyDescriptor prop = (PropertyDescriptor) properties["SettingsKey"];
                 if (prop != null) {
                    properties["SettingsKey"] = TypeDescriptor.CreateProperty(typeof(ComponentDesigner), prop, new Attribute[0]); 
                 }
            } 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.RaiseComponentChanged"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Notifies the <see cref='System.ComponentModel.Design.IComponentChangeService'/> that this component
        ///       has been changed. You only need to call this when you are 
        ///       affecting component properties directly and not through the
        ///       MemberDescriptor's accessors. 
        ///    </para> 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")] 
        protected void RaiseComponentChanged(MemberDescriptor member, Object oldValue, Object newValue) {
            IComponentChangeService changeSvc = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (changeSvc != null) {
                changeSvc.OnComponentChanged(Component, member, oldValue, newValue); 
            }
        } 
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.RaiseComponentChanging"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Notifies the <see cref='System.ComponentModel.Design.IComponentChangeService'/> that this component is
        ///       about to be changed. You only need to call this when you are
        ///       affecting component properties directly and not through the 
        ///       MemberDescriptor's accessors.
        ///    </para> 
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaiseComponentChanging(MemberDescriptor member) { 
            IComponentChangeService changeSvc = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (changeSvc != null) {
                changeSvc.OnComponentChanging(Component, member);
            } 
        }
 
        private void ResetSettingsKey() { 
            if (Component is IPersistComponentSettings) {
                SettingsKey = null; 
                settingsKeyExplicitlySet = false;
            }
        }
 
        private bool ShouldSerializeSettingsKey() {
            IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings; 
            return (persistableComponent != null && 
                   (settingsKeyExplicitlySet || (persistableComponent.SaveSettings && !String.IsNullOrEmpty(SettingsKey))));
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PostFilterAttributes"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para> Allows a designer to filter the set of
        /// attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> object.</para> 
        /// </devdoc> 
        void IDesignerFilter.PostFilterAttributes(IDictionary attributes) {
            PostFilterAttributes(attributes); 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PostFilterEvents"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// <para> Allows a designer to filter the set of events 
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.</para>
        /// </devdoc> 
        void IDesignerFilter.PostFilterEvents(IDictionary events) {
            PostFilterEvents(events);
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PostFilterProperties"]/*' />
        /// <internalonly/> 
        /// <devdoc> 
        /// <para> Allows a designer to filter the set of properties
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.</para>
        /// </devdoc>
        void IDesignerFilter.PostFilterProperties(IDictionary properties) {
            PostFilterProperties(properties); 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PreFilterAttributes"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// <para> Allows a designer to filter the set of
        /// attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        /// object.</para>
        /// </devdoc> 
        void IDesignerFilter.PreFilterAttributes(IDictionary attributes) {
            PreFilterAttributes(attributes); 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PreFilterEvents"]/*' /> 
        /// <internalonly/>
        /// <devdoc>
        /// <para> Allows a designer to filter the set of events
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.</para>
        /// </devdoc> 
        void IDesignerFilter.PreFilterEvents(IDictionary events) { 
            PreFilterEvents(events);
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PreFilterProperties"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para> Allows a designer to filter the set of properties
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.</para> 
        /// </devdoc>
        void IDesignerFilter.PreFilterProperties(IDictionary properties) { 
            PreFilterProperties(properties);
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ITreeDesigner.Children"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Retrieves the children of this designer.  This will return an empty collection 
        /// if this designer has no children.  The implementation of this maps to
        /// the AssociatedComponents property. 
        /// </devdoc>
        ICollection ITreeDesigner.Children {
            get {
                ICollection comps = AssociatedComponents; 
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (comps.Count > 0 && host != null) { 
                    IDesigner[] designers = new IDesigner[comps.Count]; 
                    int idx = 0;
                    foreach(IComponent comp in comps) { 
                        designers[idx] = host.GetDesigner(comp);
                        if (designers[idx] != null) {
                            idx++;
                        } 
                    }
 
                    // If there were missing designers, our 
                    // array could have some missing bits. Because
                    // that's not the norm, we don't optimize for that. 
                    //
                    if (idx < designers.Length) {
                        IDesigner[] newDesigners = new IDesigner[idx];
                        Array.Copy(designers, 0, newDesigners, 0, idx); 
                        designers = newDesigners;
                    } 
 
                    return designers;
                } 
                return new object[0];
            }
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ITreeDesigner.Parent"]/*' />
        /// <internalonly/> 
        /// <devdoc> 
        /// Retrieves the parent designer for this designer. This may return null if
        /// there is no parent. 
        /// </devdoc>
        IDesigner ITreeDesigner.Parent {
            get {
                IComponent parent = ParentComponent; 
                if (parent != null) {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                    if (host != null) { 
                        return host.GetDesigner(parent);
                    } 
                }
                return null;
            }
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.CDDesignerCommandSet"]/*' /> 
        /// <devdoc> 
        ///     DesignerCommandSet to be used as a site specific service.
        /// </devdoc> 
        private class CDDesignerCommandSet : DesignerCommandSet {
            private ComponentDesigner componentDesigner;

            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.CDDesignerCommandSet.CDDesignerCommandSet"]/*' /> 
            /// <devdoc>
            ///     [tbd] 
            /// </devdoc> 
            public CDDesignerCommandSet(ComponentDesigner componentDesigner) {
                this.componentDesigner = componentDesigner; 
            }

            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.CDDesignerCommandSet.GetCommands"]/*' />
            /// <devdoc> 
            ///     [tbd]
            /// </devdoc> 
            public override ICollection GetCommands(string name) { 
                if(name.Equals("Verbs")) {
                    return componentDesigner.Verbs; 
                } else if(name.Equals("ActionLists")) {
                    return componentDesigner.ActionLists;
                } else {
                    return base.GetCommands(name); 
                }
            } 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowPropertyCollection"]/*' /> 
        /// <devdoc>
        ///     Collection that holds shadow properties.
        /// </devdoc>
        protected sealed class ShadowPropertyCollection { 
            private ComponentDesigner designer;
            private Hashtable         properties; 
            private Hashtable         descriptors; 

            internal ShadowPropertyCollection(ComponentDesigner designer) { 
                this.designer = designer;
            }

            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowPropertyCollection.this"]/*' /> 
            /// <devdoc>
            ///     Accesses the given property name.  This will throw an exception if the property does not exsit on the 
            ///     base component. 
            /// </devdoc>
            public object this[string propertyName] { 
                get {
                    if (propertyName == null) {
                        throw new ArgumentNullException("propertyName");
                    } 

                    // First, check to see if the name is in the given properties table 
                    if (properties != null && properties.ContainsKey(propertyName)) { 
                        return properties[propertyName];
                    } 

                    // Next, check to see if the name is in the descriptors table.  If
                    // it isn't, we will search the underlying component and add it.
                    PropertyDescriptor property = GetShadowedPropertyDescriptor(propertyName); 

                    return property.GetValue(designer.Component); 
                } 
                set {
                    if (properties == null) { 
                        properties = new Hashtable();
                    }
                    properties[propertyName] = value;
                } 
            }
 
            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowPropertyCollection.Contains"]/*' /> 
            /// <devdoc>
            ///     Returns true if this shadow properties object contains the given property name. 
            /// </devdoc>
            public bool Contains(string propertyName) {
                return (properties != null && properties.ContainsKey(propertyName));
            } 

            /// <devdoc> 
            ///     Returns the underlying property descriptor for this property on the component 
            /// </devdoc>
            private PropertyDescriptor GetShadowedPropertyDescriptor(string propertyName) { 

                if (descriptors == null) {
                    descriptors = new Hashtable();
                } 

                PropertyDescriptor property = (PropertyDescriptor)descriptors[propertyName]; 
                if (property == null) { 
                    property = TypeDescriptor.GetProperties(designer.Component.GetType())[propertyName];
                    if (property == null) { 
                        throw new ArgumentException(SR.GetString(SR.DesignerPropNotFound, propertyName, designer.Component.GetType().FullName));
                    }
                    descriptors[propertyName] = property;
                } 
                return property;
            } 
 
            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowPropertyCollection.ShouldSerializeValue"]/*' />
            /// <devdoc> 
            ///     Returns true if the given property name should be serialized, or false
            ///     if not.  This is useful in implementing your own ShouldSerialize* methods
            ///     on shadowed properties.
            /// </devdoc> 
            internal bool ShouldSerializeValue(string propertyName, object defaultValue) {
 
                // 

                if (propertyName == null) { 
                    throw new ArgumentNullException("propertyName");
                }

                if (Contains(propertyName)) { 
                    return !object.Equals(this[propertyName], defaultValue);
                } 
                else { 
                    return GetShadowedPropertyDescriptor(propertyName).ShouldSerializeValue(designer.Component);
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ComponentDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design {
    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Configuration;
    using System.Collections; 
    using System.Reflection; 
    using System.Windows.Forms;
    using Microsoft.Win32; 
    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel;
    using Microsoft.Internal.Performance;
    using System.Globalization; 

    /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       The default designer for all components. 
    ///    </para>
    /// </devdoc>
    public class ComponentDesigner : ITreeDesigner, IDesignerFilter, IComponentInitializer {
 
        IComponent                      component;
        InheritanceAttribute            inheritanceAttribute; 
        Hashtable                       inheritedProps; 
        DesignerVerbCollection          verbs;
        DesignerActionListCollection    actionLists; 
        ShadowPropertyCollection        shadowProperties;
        bool                            settingsKeyExplicitlySet = false;

        private static CodeMarkers codemarkers = CodeMarkers.Instance; 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.AssociatedComponents"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///        Gets the design-time actionlists supported by the component associated with the designer. 
        ///    </para>
        /// </devdoc>
        public virtual DesignerActionListCollection ActionLists {
            get { 
                if (actionLists== null) {
                    actionLists = new DesignerActionListCollection(); 
                } 
                return actionLists;
            } 
        }


 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.AssociatedComponents"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Retrieves a list of associated components.  These are components that should be incluced in a cut or copy operation on this component.
        ///    </para> 
        /// </devdoc>
        public virtual ICollection AssociatedComponents{
            get {
                return new IComponent[0]; 
            }
        } 
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.AssociatedComponents"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Returns true if this component can be parented to the designer passed in..
        ///    </para>
        /// </devdoc> 
        internal virtual bool CanBeAssociatedWith(IDesigner parentDesigner) {
          return true; 
        } 

 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Component"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the component this designer is designing. 
        ///    </para>
        /// </devdoc> 
        public IComponent Component { 
            get {
                return component; 
            }
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Inherited"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets a value 
        ///       indicating whether or not this component is being inherited.
        ///    </para> 
        /// </devdoc>
        protected bool Inherited {
            get {
                return !InheritanceAttribute.Equals(InheritanceAttribute.NotInherited); 
            }
        } 
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ParentComponent"]/*' />
        /// <devdoc> 
        ///     This property provides a generic mechanism for discovering parent relationships within designers,
        ///     and is used by ComponentDesigner's ITreeDesigner interface implementation.  This property
        ///     should only return null when this designer is the root component.  Otherwise, it should return
        ///     the parent component.  The default implementation of this property returns the root component 
        ///     for all components that are not the root component, and it returns null for the root component.
        /// </devdoc> 
        protected virtual IComponent ParentComponent { 
            get {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                IComponent root = host.RootComponent;
                if (root == Component) {
                    return null;
                } 
                else {
                    return root; 
                } 
            }
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InvokeGetInheritanceAttribute"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Invokes the get inheritance attribute of the specified ComponentDesigner.
        ///    </para> 
        /// </devdoc> 
        protected InheritanceAttribute InvokeGetInheritanceAttribute(ComponentDesigner toInvoke) {
            return toInvoke.InheritanceAttribute; 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InheritanceAttribute"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets or sets the inheritance attribute for this component. 
        ///    </para> 
        /// </devdoc>
        protected virtual InheritanceAttribute InheritanceAttribute { 
            get {
                if (inheritanceAttribute == null) {
                    // Record if this component is being inherited or not.
                    // 
                    IInheritanceService inher = (IInheritanceService)GetService(typeof(IInheritanceService));
                    if (inher != null) { 
                        inheritanceAttribute = inher.GetInheritanceAttribute(Component); 
                    }
                    else { 
                        inheritanceAttribute = InheritanceAttribute.Default;
                    }
                }
 
                return inheritanceAttribute;
            } 
        } 

        /// <devdoc> 
        ///    shadowing the SettingsKey so we can default it to be RootComponent.Name + "." + Control.Name
        /// </devdoc>
        private string SettingsKey {
            get { 
                if (string.IsNullOrEmpty((string) ShadowProperties["SettingsKey"]))  {
                    IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings; 
                    IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                    IComponent rootComponent = host != null ? host.RootComponent : null;
 
                    if (persistableComponent != null && rootComponent != null) {
                        if (String.IsNullOrEmpty(persistableComponent.SettingsKey)) {
                            if (rootComponent != null && rootComponent != persistableComponent) {
                                ShadowProperties["SettingsKey"] = String.Format(CultureInfo.CurrentCulture, "{0}.{1}", rootComponent.Site.Name, Component.Site.Name); 
                            }
                            else { 
                                ShadowProperties["SettingsKey"] = Component.Site.Name; 
                            }
                        } 

                        persistableComponent.SettingsKey =  ShadowProperties["SettingsKey"] as string;
                        return persistableComponent.SettingsKey;
                    } 
                }
 
                return ShadowProperties["SettingsKey"] as string; 
            }
            set { 
                 ShadowProperties["SettingsKey"] = value;
                 settingsKeyExplicitlySet = true;
                 IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings;
                 if (persistableComponent != null) { 
                     persistableComponent.SettingsKey =  value;
                 } 
 
            }
 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowProperties"]/*' />
        /// <devdoc> 
        ///     Gets a collection that houses shadow properties.  Shadow properties. are properties that fall
        ///     through to the underlying component before they are set, but return their set values once they 
        ///     are set. 
        /// </devdoc>
        protected ShadowPropertyCollection ShadowProperties { 
            get {
                if (shadowProperties == null) {
                    shadowProperties = new ShadowPropertyCollection(this);
                } 
                return shadowProperties;
            } 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Verbs"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the design-time verbs supported by the component associated with the designer.
        ///    </para> 
        /// </devdoc>
        public virtual DesignerVerbCollection Verbs { 
            get { 
                if (verbs == null) {
                    verbs = new DesignerVerbCollection(); 
                }
                return verbs;
            }
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.OnComponentRename"]/*' /> 
        /// <devdoc> 
        ///      Raised when a component's name changes.  Here we update the SettingsKey property if necessary.
        /// </devdoc> 
        private void OnComponentRename(object sender, ComponentRenameEventArgs e)
        {
            if (Component is IPersistComponentSettings) {
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                IComponent rootComponent = host != null ? host.RootComponent : null;
 
                // SettingsKey is formed based on the name of the component and the root component. If either of 
                // these change, we reset settings key (if it hasn't been explicitly set) so it can be recomputed.
                if (!settingsKeyExplicitlySet && (e.Component == Component || e.Component == rootComponent)) { 
                    ResetSettingsKey();
                }
            }
        } 

 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Disposes of the resources (other than memory) used
        ///       by the <see cref='System.ComponentModel.Design.ComponentDesigner'/>.
        ///    </para>
        /// </devdoc> 
        public void Dispose() {
            Dispose(true); 
            GC.SuppressFinalize(this); 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for=".Finalize"]/*' />
        ~ComponentDesigner() {
            Dispose(false);
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Dispose2"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Disposes of the resources (other than memory) used 
        ///       by the <see cref='System.ComponentModel.Design.ComponentDesigner'/>.
        ///    </para>
        /// </devdoc>
        protected virtual void Dispose(bool disposing) { 
            if (disposing) {
                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                if (cs != null) 
                {
                    cs.ComponentRename -= new ComponentRenameEventHandler(this.OnComponentRename); 
                }

                component = null;
            } 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.DoDefaultAction"]/*' /> 
        /// <devdoc>
        ///    <para>Creates a method signature in the source code file for the default event on the component and navigates 
        ///       the user's cursor to that location in preparation to assign
        ///       the default action.</para>
        /// </devdoc>
        public virtual void DoDefaultAction() { 
            IEventBindingService eps = (IEventBindingService)GetService(typeof(IEventBindingService));
 
            //If the event binding service is not available, there is nothing much we can do, so just return. 
            if (eps == null) {
                return; 
            }

            ISelectionService selectionService = (ISelectionService)GetService(typeof(ISelectionService));
            if (selectionService == null) { 
                return;
            } 
 
            ICollection components = selectionService.GetSelectedComponents();
 

            EventDescriptor thisDefaultEvent = null;
            string          thisHandler = null;
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            DesignerTransaction t = null; 
 
            try {
                foreach(object comp in components) { 

                    if (!(comp is IComponent)) {
                        continue;
                    } 

                    EventDescriptor defaultEvent = TypeDescriptor.GetDefaultEvent(comp); 
                    PropertyDescriptor defaultPropEvent = null; 
                    string handler = null;
                    bool eventChanged = false; 

                    if (defaultEvent != null) {
                        defaultPropEvent = eps.GetEventProperty(defaultEvent);
                    } 

                    // If we couldn't find a property for this event, or of the property is read only, then 
                    // abort. 
                    //
                    if (defaultPropEvent == null || defaultPropEvent.IsReadOnly) { 
                        continue;
                    }

                    try { 
                        if (host != null && t == null) {
                            t = host.CreateTransaction(SR.GetString(SR.ComponentDesignerAddEvent, defaultEvent.Name)); 
                        } 
                    }
                    catch (CheckoutException cxe) { 
                        if (cxe == CheckoutException.Canceled)
                            return;

                        throw cxe; 
                    }
 
                    // handler will be null if there is no explicit event hookup in the parsed init method 
                    handler = (string)defaultPropEvent.GetValue(comp);
 
                    if (handler == null) {
                        eventChanged = true;

                        handler = eps.CreateUniqueMethodName((IComponent)comp, defaultEvent); 
                    }
                    else { 
                        // ensure the handler is still there 
                        eventChanged = true;
                        foreach(string compatibleMethod in eps.GetCompatibleMethods(defaultEvent)) { 
                            if (handler == compatibleMethod) {
                                eventChanged = false;
                                break;
                            } 
                        }
                    } 
 
                    // Save the new value... BEFORE navigating to it!
                    // 
 					codemarkers.CodeMarker(CodeMarkerEvent.perfFXBindEventDesignToCode);
                    if (eventChanged && defaultPropEvent != null) {
                       defaultPropEvent.SetValue(comp, handler);
                    } 

                    if (component == comp){ 
                        thisDefaultEvent = defaultEvent; 
                        thisHandler = handler;
                    } 
                }
            }
            catch (InvalidOperationException) {
                // VSWhidbey 434433 - If the doc data for this designer is currently locked, we will get an exception here. 
                // In this case, just eat the exception.
 
                if (t != null) { 
                    t.Cancel();
                    t = null; 
                }
            }
            finally {
                if (t != null) { 
                    t.Commit();
                } 
            } 

            // Now show the event code. 
            //
            if (thisHandler != null && thisDefaultEvent != null) {
                eps.ShowCode(component, thisDefaultEvent);
            } 
        }
 
        internal bool IsRootDesigner { 
            get {
                Debug.Assert(this.component != null, 
                    "this.component needs to be set before this method is valid.");

                bool isRoot = false;
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                if (host != null && this.component == host.RootComponent) {
                    isRoot = true; 
                } 
                return isRoot;
            } 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.ComponentDesigner'/> 
        ///       class using the specified component. 
        ///    </para>
        /// </devdoc> 
        public virtual void Initialize(IComponent component) {

            Debug.Assert(component != null, "Can't create designer with no component!");
 
            this.component = component;
 
            // For inherited components, save off the current values so we can 
            // compute a delta.  We also do this for the root component, but,
            // as it is ALWAYS inherited, the computation of default values 
            // favors the presence of a default value attribute over
            // the current code value.
            //
            bool isRoot = false; 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host != null && component == host.RootComponent) { 
                isRoot = true; 
            }
 

            //Only add the service if it is not already added. VSWhidbey #395676.
            IServiceContainer sc = component.Site as IServiceContainer;
            if (sc != null && GetService(typeof(DesignerCommandSet)) == null) { 
                sc.AddService(typeof(DesignerCommandSet), new CDDesignerCommandSet(this));
            } 
 
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (cs != null) 
            {
                cs.ComponentRename += new ComponentRenameEventHandler(this.OnComponentRename);
            }
 
            if (isRoot || !InheritanceAttribute.Equals(InheritanceAttribute.NotInherited)) {
                InitializeInheritedProperties(isRoot); 
            } 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InitializeExistingComponent"]/*' />
        /// <devdoc>
        ///     This method is called when an existing component is being re-initialized.  This may occur after
        ///     dragging a component to another container, for example.  The defaultValues 
        ///     property contains a name/value dictionary of default values that should be applied
        ///     to properties. This dictionary may be null if no default values are specified. 
        ///     You may use the defaultValues dictionary to apply recommended defaults to proeprties 
        ///     but you should not modify component properties beyond what is stored in the
        ///     dictionary, because this is an existing component that may already have properties 
        ///     set on it.
        ///
        ///     The default implemenation of this method does nothing.
        /// </devdoc> 
        public virtual void InitializeExistingComponent(IDictionary defaultValues) {
#pragma warning disable 618 
            // execute legacy code 
            InitializeNonDefault();
#pragma warning restore 618 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InitializeNewComponent"]/*' />
        /// <devdoc> 
        ///     This method is called when a component is first initialized, typically after being first added
        ///     to a design surface.  The defaultValues property contains a name/value dictionary of default 
        ///     values that should be applied to properties.  This dictionary may be null if no default values 
        ///     are specified.  You may perform any initialization of this component that you like, and you
        ///     may even ignore the defaultValues dictionary altogether if you wish. 
        ///
        ///     The default implemenation of this method does nothing.
        /// </devdoc>
        public virtual void InitializeNewComponent(IDictionary defaultValues) { 

            // We have just dropped a new component to the form, if this component 
            // has a designerActionPanel available, we need to see if he wants to auto 
            // show the panel on drop, for that we look if any of the designeractionlist
            // has it's autoshow bit set to true, if that's the case we call showdesignerpanel 
            // on the service, that's gonna fire an event so that the UI can try to show the panel
            DesignerActionUIService daUIService = (DesignerActionUIService)GetService(typeof(DesignerActionUIService));
            if (daUIService != null) {
                if(daUIService.ShouldAutoShow(Component)) { 
                        IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
                        if(designerHost != null && designerHost.InTransaction) { 
                            designerHost.TransactionClosed += new DesignerTransactionCloseEventHandler(ShowDesignerActionUI); 
                        } else {
                            daUIService.ShowUI(Component); 
                        }
                }
            }
#pragma warning disable 618 
            // execute legacy code
            OnSetComponentDefaults(); 
#pragma warning restore 618 
        }
 
        private void ShowDesignerActionUI(object sender, DesignerTransactionCloseEventArgs e) {
            DesignerActionUIService daUIService = (DesignerActionUIService)GetService(typeof(DesignerActionUIService));
            if (daUIService != null) {
                daUIService.ShowUI(Component); 
            }
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            if(designerHost != null) { 
                designerHost.TransactionClosed -= new DesignerTransactionCloseEventHandler(ShowDesignerActionUI);
            } 
        }


        private void InitializeInheritedProperties(bool rootComponent) { 

            Hashtable props = new Hashtable(); 
 
            bool readOnlyInherit = (InheritanceAttribute.Equals(InheritanceAttribute.InheritedReadOnly));
 
            if (!readOnlyInherit) {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(Component);

                // Now loop through all the properties.  For each one, try to match a pre-created property. 
                // If that fails, then create a new property.
                // 
                PropertyDescriptor[] values = new PropertyDescriptor[properties.Count]; 
                properties.CopyTo(values, 0);
 
                for (int i = 0; i < values.Length; i++) {
                    PropertyDescriptor prop = values[i];

                    // Skip some properties 
                    //
                    if (object.Equals(prop.Attributes[typeof(DesignOnlyAttribute)], DesignOnlyAttribute.Yes)) { 
                        continue; 
                    }
 
                    if (prop.SerializationVisibility == DesignerSerializationVisibility.Hidden && !prop.IsBrowsable) {
                        continue;
                    }
 
                    PropertyDescriptor inheritedProp = (PropertyDescriptor)props[prop.Name];
 
                    if (inheritedProp == null) { 
                        // This ia a publicly inherited component.  We replace all component properties with
                        // inherited versions that reset the default property values to those that are 
                        // currently on the component.
                        //
                        props[prop.Name] = new InheritedPropertyDescriptor(prop, component, rootComponent);
                    } 
                }
            } 
 
            inheritedProps = props;
            TypeDescriptor.Refresh(Component); // force TypeDescriptor to re-query us. 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.InitializeNonDefault"]/*' />
        /// <devdoc> 
        ///    <para>Called when the designer has been associated with a control that is not in it's default state,
        ///          such as one that has been pasted or drag-dropped onto the designer.  This is an opportunity 
        ///          to fixup any shadowed properties in a different way than for default components.  This is called 
        ///          after the other initialize functions.
        ///     </para> 
        /// </devdoc>
        [Obsolete("This method has been deprecated. Use InitializeExistingComponent instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual void InitializeNonDefault() {
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.GetService"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Provides 
        ///       a way for a designer to get services from the hosting
        ///       environment.
        ///    </para>
        /// </devdoc> 
        protected virtual object GetService(Type serviceType) {
            if (component != null) { 
                ISite site = component.Site; 
                if (site != null) {
                    return site.GetService(serviceType); 
                }
            }
            return null;
        } 

        private Attribute[] NonBrowsableAttributes(EventDescriptor e) { 
                Attribute[] attrs = new Attribute[e.Attributes.Count]; 
                e.Attributes.CopyTo(attrs, 0);
 
                for (int i = 0; i < attrs.Length; i++) {
                    if (attrs[i] != null && typeof(BrowsableAttribute).IsInstanceOfType(attrs[i]) && ((BrowsableAttribute)attrs[i]).Browsable) {
                        attrs[i] = BrowsableAttribute.No;
                        return attrs; 
                    }
                } 
 
                // we didn't find it, we have to alloc a new array
                Attribute[] newAttrs = new Attribute[attrs.Length+1]; 
                Array.Copy(attrs, 0, newAttrs, 0, attrs.Length);
                newAttrs[attrs.Length] = BrowsableAttribute.No;
                return newAttrs;
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.OnSetComponentDefaults"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Raises the SetComponentDefault event. 
        ///    </para>
        /// </devdoc>
        [Obsolete("This method has been deprecated. Use InitializeNewComponent instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual void OnSetComponentDefaults() { 
            // COMPAT: The following code shipped in Everett, so we need to continue to do this.
            // See VSWhidbey #419610 for details. 
 
            ISite site = Component.Site;
            if (site != null) { 
                IComponent component = Component;
                PropertyDescriptor pd = TypeDescriptor.GetDefaultProperty(component);
                if (pd != null && pd.PropertyType.Equals(typeof(string))) {
 
                    string current = (string)pd.GetValue(component);
                    if (current == null || current.Length == 0) { 
                        pd.SetValue(component, site.Name); 
                    }
                } 
            }
        }

 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnContextMenu"]/*' />
        /// <devdoc> 
        ///     Called when the context menu should be displayed 
        /// </devdoc>
        internal virtual void ShowContextMenu(int x, int y) { 
            IMenuCommandService mcs = (IMenuCommandService)GetService(typeof(IMenuCommandService));
            if (mcs != null) {
                mcs.ShowContextMenu(MenuCommands.SelectionMenu, x, y);
            } 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PostFilterAttributes"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Allows a
        ///       designer to filter the set of member attributes the
        ///       component it is designing will expose through the
        ///       TypeDescriptor object. 
        ///    </para>
        /// </devdoc> 
        protected virtual void PostFilterAttributes(IDictionary attributes) { 

            // If this component is being inherited, mark it as such in the class attributes. 
            //

            // VSWhidbey #424845 - if the InheritanceAttribute is set in attributes, then don't override it
            // Also, set our member variable to ensure that what you get by querying through the TypeDescriptor and 
            // through InheritanceAttribute directly is the same.
            if (attributes.Contains(typeof(InheritanceAttribute))) { 
                inheritanceAttribute = attributes[typeof(InheritanceAttribute)] as InheritanceAttribute; 
            }
            else if (!InheritanceAttribute.Equals(InheritanceAttribute.NotInherited)) { 
                attributes[typeof(InheritanceAttribute)] = InheritanceAttribute;
            }
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PostFilterEvents"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Allows
        ///       a designer to filter the set of events the 
        ///       component it is designing will expose through the
        ///       TypeDescriptor object.
        ///    </para>
        /// </devdoc> 
        protected virtual void PostFilterEvents(IDictionary events) {
 
            // If this component is being privately inherited, we need to filter 
            // the events to make them read-only.
            // 
            if (InheritanceAttribute.Equals(InheritanceAttribute.InheritedReadOnly)) {
                EventDescriptor[] values = new EventDescriptor[events.Values.Count];
                events.Values.CopyTo(values, 0);
 
                for (int i = 0; i < values.Length; i++) {
                    EventDescriptor evt = values[i]; 
                    events[evt.Name] = TypeDescriptor.CreateEvent(evt.ComponentType, evt, ReadOnlyAttribute.Yes); 
                }
            } 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PostFilterProperties"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Allows 
        ///       a designer to filter the set of properties the 
        ///       component it is designing will expose through the
        ///       TypeDescriptor object. 
        ///    </para>
        /// </devdoc>
        protected virtual void PostFilterProperties(IDictionary properties) {
 
            // Check for inheritance
            // 
            if (inheritedProps != null) { 

                bool readOnlyInherit = (InheritanceAttribute.Equals(InheritanceAttribute.InheritedReadOnly)); 

                if (readOnlyInherit) {
                    // Now loop through all the properties.  For each one, try to match a pre-created property.
                    // If that fails, then create a new property. 
                    //
                    PropertyDescriptor[] values = new PropertyDescriptor[properties.Values.Count]; 
                    properties.Values.CopyTo(values, 0); 

                    for (int i = 0; i < values.Length; i++) { 
                        PropertyDescriptor prop = values[i];
                          // This is a private component.  Therefore, the user should not be
                          // allowed to modify any properties.  We replace all properties with
                          // read-only versions. 
                          //
                          properties[prop.Name] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, ReadOnlyAttribute.Yes); 
                    } 
                }
                else { 
                    // otherwise apply our inherited properties to the actual property list.
                    //
                    foreach (DictionaryEntry de in inheritedProps) {
                        InheritedPropertyDescriptor inheritedPropDesc = de.Value as InheritedPropertyDescriptor; 

                        if (inheritedPropDesc != null) { 
                            // replace the property descriptor it was created 
                            // with with the new one in case we're shadowing
                            // 
                            PropertyDescriptor newInnerProp = (PropertyDescriptor)properties[de.Key];
                            if (newInnerProp != null) {
                                inheritedPropDesc.PropertyDescriptor = newInnerProp;
                                properties[de.Key] = inheritedPropDesc; 
                            }
                        } 
                    } 
                }
            } 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PreFilterAttributes"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Allows a designer 
        ///       to filter the set of member attributes the component 
        ///       it is designing will expose through the TypeDescriptor
        ///       object. 
        ///    </para>
        /// </devdoc>
        protected virtual void PreFilterAttributes(IDictionary attributes) {
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PreFilterEvents"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Allows a 
        ///       designer to filter the set of events the component
        ///       it is designing will expose through the TypeDescriptor
        ///       object.
        ///    </para> 
        /// </devdoc>
        protected virtual void PreFilterEvents(IDictionary events) { 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Allows a
        ///       designer to filter the set of properties the component 
        ///       it is designing will expose through the TypeDescriptor
        ///       object. 
        ///    </para> 
        /// </devdoc>
        protected virtual void PreFilterProperties(IDictionary properties) { 
            if (Component is IPersistComponentSettings) {
                 PropertyDescriptor prop = (PropertyDescriptor) properties["SettingsKey"];
                 if (prop != null) {
                    properties["SettingsKey"] = TypeDescriptor.CreateProperty(typeof(ComponentDesigner), prop, new Attribute[0]); 
                 }
            } 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.RaiseComponentChanged"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Notifies the <see cref='System.ComponentModel.Design.IComponentChangeService'/> that this component
        ///       has been changed. You only need to call this when you are 
        ///       affecting component properties directly and not through the
        ///       MemberDescriptor's accessors. 
        ///    </para> 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")] 
        protected void RaiseComponentChanged(MemberDescriptor member, Object oldValue, Object newValue) {
            IComponentChangeService changeSvc = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (changeSvc != null) {
                changeSvc.OnComponentChanged(Component, member, oldValue, newValue); 
            }
        } 
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.RaiseComponentChanging"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Notifies the <see cref='System.ComponentModel.Design.IComponentChangeService'/> that this component is
        ///       about to be changed. You only need to call this when you are
        ///       affecting component properties directly and not through the 
        ///       MemberDescriptor's accessors.
        ///    </para> 
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaiseComponentChanging(MemberDescriptor member) { 
            IComponentChangeService changeSvc = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (changeSvc != null) {
                changeSvc.OnComponentChanging(Component, member);
            } 
        }
 
        private void ResetSettingsKey() { 
            if (Component is IPersistComponentSettings) {
                SettingsKey = null; 
                settingsKeyExplicitlySet = false;
            }
        }
 
        private bool ShouldSerializeSettingsKey() {
            IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings; 
            return (persistableComponent != null && 
                   (settingsKeyExplicitlySet || (persistableComponent.SaveSettings && !String.IsNullOrEmpty(SettingsKey))));
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PostFilterAttributes"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para> Allows a designer to filter the set of
        /// attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> object.</para> 
        /// </devdoc> 
        void IDesignerFilter.PostFilterAttributes(IDictionary attributes) {
            PostFilterAttributes(attributes); 
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PostFilterEvents"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// <para> Allows a designer to filter the set of events 
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.</para>
        /// </devdoc> 
        void IDesignerFilter.PostFilterEvents(IDictionary events) {
            PostFilterEvents(events);
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PostFilterProperties"]/*' />
        /// <internalonly/> 
        /// <devdoc> 
        /// <para> Allows a designer to filter the set of properties
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.</para>
        /// </devdoc>
        void IDesignerFilter.PostFilterProperties(IDictionary properties) {
            PostFilterProperties(properties); 
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PreFilterAttributes"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// <para> Allows a designer to filter the set of
        /// attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        /// object.</para>
        /// </devdoc> 
        void IDesignerFilter.PreFilterAttributes(IDictionary attributes) {
            PreFilterAttributes(attributes); 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PreFilterEvents"]/*' /> 
        /// <internalonly/>
        /// <devdoc>
        /// <para> Allows a designer to filter the set of events
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.</para>
        /// </devdoc> 
        void IDesignerFilter.PreFilterEvents(IDictionary events) { 
            PreFilterEvents(events);
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.IDesignerFilter.PreFilterProperties"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para> Allows a designer to filter the set of properties
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.</para> 
        /// </devdoc>
        void IDesignerFilter.PreFilterProperties(IDictionary properties) { 
            PreFilterProperties(properties);
        }

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ITreeDesigner.Children"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Retrieves the children of this designer.  This will return an empty collection 
        /// if this designer has no children.  The implementation of this maps to
        /// the AssociatedComponents property. 
        /// </devdoc>
        ICollection ITreeDesigner.Children {
            get {
                ICollection comps = AssociatedComponents; 
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (comps.Count > 0 && host != null) { 
                    IDesigner[] designers = new IDesigner[comps.Count]; 
                    int idx = 0;
                    foreach(IComponent comp in comps) { 
                        designers[idx] = host.GetDesigner(comp);
                        if (designers[idx] != null) {
                            idx++;
                        } 
                    }
 
                    // If there were missing designers, our 
                    // array could have some missing bits. Because
                    // that's not the norm, we don't optimize for that. 
                    //
                    if (idx < designers.Length) {
                        IDesigner[] newDesigners = new IDesigner[idx];
                        Array.Copy(designers, 0, newDesigners, 0, idx); 
                        designers = newDesigners;
                    } 
 
                    return designers;
                } 
                return new object[0];
            }
        }
 
        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ITreeDesigner.Parent"]/*' />
        /// <internalonly/> 
        /// <devdoc> 
        /// Retrieves the parent designer for this designer. This may return null if
        /// there is no parent. 
        /// </devdoc>
        IDesigner ITreeDesigner.Parent {
            get {
                IComponent parent = ParentComponent; 
                if (parent != null) {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                    if (host != null) { 
                        return host.GetDesigner(parent);
                    } 
                }
                return null;
            }
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.CDDesignerCommandSet"]/*' /> 
        /// <devdoc> 
        ///     DesignerCommandSet to be used as a site specific service.
        /// </devdoc> 
        private class CDDesignerCommandSet : DesignerCommandSet {
            private ComponentDesigner componentDesigner;

            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.CDDesignerCommandSet.CDDesignerCommandSet"]/*' /> 
            /// <devdoc>
            ///     [tbd] 
            /// </devdoc> 
            public CDDesignerCommandSet(ComponentDesigner componentDesigner) {
                this.componentDesigner = componentDesigner; 
            }

            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.CDDesignerCommandSet.GetCommands"]/*' />
            /// <devdoc> 
            ///     [tbd]
            /// </devdoc> 
            public override ICollection GetCommands(string name) { 
                if(name.Equals("Verbs")) {
                    return componentDesigner.Verbs; 
                } else if(name.Equals("ActionLists")) {
                    return componentDesigner.ActionLists;
                } else {
                    return base.GetCommands(name); 
                }
            } 
        } 

        /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowPropertyCollection"]/*' /> 
        /// <devdoc>
        ///     Collection that holds shadow properties.
        /// </devdoc>
        protected sealed class ShadowPropertyCollection { 
            private ComponentDesigner designer;
            private Hashtable         properties; 
            private Hashtable         descriptors; 

            internal ShadowPropertyCollection(ComponentDesigner designer) { 
                this.designer = designer;
            }

            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowPropertyCollection.this"]/*' /> 
            /// <devdoc>
            ///     Accesses the given property name.  This will throw an exception if the property does not exsit on the 
            ///     base component. 
            /// </devdoc>
            public object this[string propertyName] { 
                get {
                    if (propertyName == null) {
                        throw new ArgumentNullException("propertyName");
                    } 

                    // First, check to see if the name is in the given properties table 
                    if (properties != null && properties.ContainsKey(propertyName)) { 
                        return properties[propertyName];
                    } 

                    // Next, check to see if the name is in the descriptors table.  If
                    // it isn't, we will search the underlying component and add it.
                    PropertyDescriptor property = GetShadowedPropertyDescriptor(propertyName); 

                    return property.GetValue(designer.Component); 
                } 
                set {
                    if (properties == null) { 
                        properties = new Hashtable();
                    }
                    properties[propertyName] = value;
                } 
            }
 
            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowPropertyCollection.Contains"]/*' /> 
            /// <devdoc>
            ///     Returns true if this shadow properties object contains the given property name. 
            /// </devdoc>
            public bool Contains(string propertyName) {
                return (properties != null && properties.ContainsKey(propertyName));
            } 

            /// <devdoc> 
            ///     Returns the underlying property descriptor for this property on the component 
            /// </devdoc>
            private PropertyDescriptor GetShadowedPropertyDescriptor(string propertyName) { 

                if (descriptors == null) {
                    descriptors = new Hashtable();
                } 

                PropertyDescriptor property = (PropertyDescriptor)descriptors[propertyName]; 
                if (property == null) { 
                    property = TypeDescriptor.GetProperties(designer.Component.GetType())[propertyName];
                    if (property == null) { 
                        throw new ArgumentException(SR.GetString(SR.DesignerPropNotFound, propertyName, designer.Component.GetType().FullName));
                    }
                    descriptors[propertyName] = property;
                } 
                return property;
            } 
 
            /// <include file='doc\ComponentDesigner.uex' path='docs/doc[@for="ComponentDesigner.ShadowPropertyCollection.ShouldSerializeValue"]/*' />
            /// <devdoc> 
            ///     Returns true if the given property name should be serialized, or false
            ///     if not.  This is useful in implementing your own ShouldSerialize* methods
            ///     on shadowed properties.
            /// </devdoc> 
            internal bool ShouldSerializeValue(string propertyName, object defaultValue) {
 
                // 

                if (propertyName == null) { 
                    throw new ArgumentNullException("propertyName");
                }

                if (Contains(propertyName)) { 
                    return !object.Equals(this[propertyName], defaultValue);
                } 
                else { 
                    return GetShadowedPropertyDescriptor(propertyName).ShouldSerializeValue(designer.Component);
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
