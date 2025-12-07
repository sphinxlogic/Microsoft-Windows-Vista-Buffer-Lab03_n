//------------------------------------------------------------------------------ 
// <copyright file="DesignerHost.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization; 
    using System.Reflection;
    using System.Text; 

    /// <devdoc>
    ///     This is the main hosting object.  DesignerHost implements
    ///     services and interfaces specific to the design time 
    ///     IContainer object.  The services this class implements
    ///     are generally non-removable (they work as a unit so removing 
    ///     them would break things). 
    /// </devdoc>
    internal sealed class DesignerHost : 
        Container,
        IDesignerLoaderHost2,
        IDesignerHostTransactionState,
        IComponentChangeService, 
        IReflect {
 
        // State flags for the state of the designer host 
        //
        private static readonly int StateLoading               = BitVector32.CreateMask();                // Designer is currently loading from the loader host. 
        private static readonly int StateUnloading             = BitVector32.CreateMask(StateLoading);    // Designer is currently unloading.
        private static readonly int StateIsClosingTransaction  = BitVector32.CreateMask(StateUnloading);  // A transaction is in the process of being Canceled or Commited.

        private static Type[] DefaultServices = new Type[] { 
                                                typeof(IDesignerHost),
                                                typeof(IContainer), 
                                                typeof(IComponentChangeService), 
                                                typeof(IDesignerLoaderHost2)
                                            }; 

        // IDesignerHost events
        //
        private static readonly object EventActivated           = new object(); // Designer has been activated 
        private static readonly object EventDeactivated         = new object(); // Designer has been deactivated
        private static readonly object EventLoadComplete        = new object(); // Loading has been completed 
        private static readonly object EventTransactionClosed   = new object(); // The last transaction has been closed 
        private static readonly object EventTransactionClosing  = new object(); // The last transaction is about to be closed
        private static readonly object EventTransactionOpened   = new object(); // The first transaction has been opened 
        private static readonly object EventTransactionOpening  = new object(); // The first transaction is about to be opened

        // IComponentChangeService events
        // 
        private static readonly object EventComponentAdding     = new object(); // A component is about to be added to the container
        private static readonly object EventComponentAdded      = new object(); // A component was just added to the container 
        private static readonly object EventComponentChanging   = new object(); // A component is about to be changed 
        private static readonly object EventComponentChanged    = new object(); // A component has changed
        private static readonly object EventComponentRemoving   = new object(); // A component is about to be removed from the container 
        private static readonly object EventComponentRemoved    = new object(); // A component has been removed from the container
        private static readonly object EventComponentRename     = new object(); // A component has been renamed

        // Member variables 
        //
        private BitVector32                     _state;                     // state for this host 
        private DesignSurface                   _surface;                   // the owning designer surface. 
        private string                          _newComponentName;          // transient value indicating the name of a component that is being created
        private Stack                           _transactions;              // stack of transactions.  Each entry in the stack is a DesignerTransaction 
        private IComponent                      _rootComponent;             // the root of our design
        private string                          _rootComponentClassName;    // class name of the root of our design
        private Hashtable                       _designers;                 // designer -> component mapping
        private EventHandlerList                _events;                    // event list 
        private DesignerLoader                  _loader;                    // the loader that loads our designers
        private ICollection                     _savedSelection;            // set of selected components saved across reloads 
        private HostDesigntimeLicenseContext    _licenseCtx; 
        private IDesignerEventService           _designerEventService;
        private static readonly object          _selfLock = new object(); 
        private bool                            _ignoreErrorsDuringReload;
        private bool                            _canReloadWithErrors;

 
        public DesignerHost(DesignSurface surface) {
 
            _surface = surface; 
            _state = new BitVector32();
            _designers = new Hashtable(); 
            _events = new EventHandlerList();

            // Add the relevant services.  We try to add these
            // as "fixed" services.  A fixed service cannot be 
            // removed by the user.  The reason for this is that
            // each of these services depends on each other, so 
            // you can't really remove and replace just one of them. 
            //
            // If we can't get our own service container that supports 
            // fixed services, we add these as regular services.
            //
            DesignSurfaceServiceContainer dsc = GetService(typeof(DesignSurfaceServiceContainer)) as DesignSurfaceServiceContainer;
            if (dsc != null) { 
                foreach(Type t in DefaultServices) {
                    dsc.AddFixedService(t, this); 
                } 
            }
            else { 
                IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer;
                Debug.Assert(sc != null, "DesignerHost: Ctor needs a service provider that provides IServiceContainer");
                if (sc != null) {
                    foreach(Type t in DefaultServices) { 
                        sc.AddService(t, this);
                    } 
                } 
            }
        } 

        internal HostDesigntimeLicenseContext LicenseContext {
            get {
                if (_licenseCtx == null) { 
                    _licenseCtx = new HostDesigntimeLicenseContext(this);
                } 
 
                return _licenseCtx;
            } 
        }

        // Internal flag which is used to track when we are in the process of commiting or canceling a transaction.
        internal bool IsClosingTransaction { 
            get { return _state[StateIsClosingTransaction]; }
            set { _state[StateIsClosingTransaction] = value; } 
        } 

        bool IDesignerHostTransactionState.IsClosingTransaction { 
            get { return this.IsClosingTransaction; }
        }

        /// <summary> 
        ///     Override of Container.Add
        /// </summary> 
        /// <param name="component"></param> 
        /// <param name="name"></param>
        public override void Add(IComponent component, string name) { 

            if (AddToContainerPreProcess(component, name, this)) {

                // Site creation fabricates a name for this component. 
                //
                base.Add(component, name); 
 
                try {
                    AddToContainerPostProcess(component, name, this); 
                }
                catch (Exception t) {
                    if (t != CheckoutException.Canceled) {
                        Remove(component); 
                    }
                    throw; 
                } 
                catch {
                    Remove(component); 
                    throw;
                }
            }
        } 

        /// <devdoc> 
        ///     We support adding to either our main IDesignerHost container or to a private 
        ///     per-site container for nested objects.  This code is the stock add code
        ///     that creates a designer, etc.  See Add (above) for an example of how to call 
        ///     this correctly.
        ///
        ///     This method is called before the component is actually added.  It returns true
        ///     if the component can be added to this container or false if the add should 
        ///     not occur (because the component may already be in this container, for example.)
        ///     It may also throw if adding this component is illegal. 
        /// </devdoc> 
        internal bool AddToContainerPreProcess(IComponent component, string name, IContainer containerToAddTo) {
            if (component == null) { 
                throw new ArgumentNullException("component");
            }

            // We should never add anything while we're unloading. 
            //
            if (_state[StateUnloading]) { 
                Exception ex = new Exception(SR.GetString(SR.DesignerHostUnloading)); 
                ex.HelpLink = SR.DesignerHostUnloading;
                throw ex; 
            }

            // Make sure we're not adding an instance of the root component to itself.
            // 
            if (_rootComponent != null) {
                if (string.Equals(component.GetType().FullName, _rootComponentClassName, StringComparison.OrdinalIgnoreCase)) { 
                    Exception ex = new Exception(SR.GetString(SR.DesignerHostCyclicAdd, component.GetType().FullName, _rootComponentClassName)); 
                    ex.HelpLink = SR.DesignerHostCyclicAdd;
                    throw ex; 
                }
            }

            ISite existingSite = component.Site; 

            // If the component is already in our container, we just rename. 
            // 
            if (existingSite != null && existingSite.Container == this) {
                if (name != null) { 
                    existingSite.Name = name;
                }
                return false;
            } 

            // Raise an adding event for our container if the container is us. 
            // 
            ComponentEventArgs ce = new ComponentEventArgs(component);
            ComponentEventHandler eh = _events[EventComponentAdding] as ComponentEventHandler; 
            if (eh != null) {
                eh(containerToAddTo, ce);
            }
 
            return true;
        } 
 
        /// <devdoc>
        ///     We support adding to either our main IDesignerHost container or to a private 
        ///     per-site container for nested objects.  This code is the stock add code
        ///     that creates a designer, etc.  See Add (above) for an example of how to call
        ///     this correctly.
        /// </devdoc> 
        internal void AddToContainerPostProcess(IComponent component, string name, IContainer containerToAddTo) {
            // Now that we've added, check to see if this is an extender provider.  If it is, 
            // add it to our extender provider service so it is available. 
            //
            if (component is IExtenderProvider && 
                //
                !TypeDescriptor.GetAttributes(component).Contains(InheritanceAttribute.InheritedReadOnly)) {
                IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                if (eps != null) { 
                    eps.AddExtenderProvider((IExtenderProvider)component);
                } 
            } 

            // Is this the first component the loader has created?  If so, then it must 
            // be the root component (by definition) so we will expect there to be a root
            // designer associated with the component.  Otherwise, we search for a
            // normal designer, which can be optionally provided.
            // 
            IDesigner designer = null;
 
            if (_rootComponent == null) { 

                designer = _surface.CreateDesigner(component, true) as IRootDesigner; 

                if (designer == null) {
                    Exception ex = new Exception(SR.GetString(SR.DesignerHostNoTopLevelDesigner, component.GetType().FullName));
                    ex.HelpLink = SR.DesignerHostNoTopLevelDesigner; 
                    throw ex;
                } 
 
                _rootComponent = component;
 
                // Check and see if anyone has set the class name of the root component.
                // we default to the component name.
                //
                if (_rootComponentClassName == null) { 
                    _rootComponentClassName = component.Site.Name;
                } 
            } 
            else {
                designer = _surface.CreateDesigner(component, false); 
            }

            if (designer != null) {
 
                // The presence of a designer in this table
                // allows the designer to filter the component's 
                // properties, which is often needed during designer 
                // initialization.  So, we stuff it in the table
                // first, initialize, and if it throws we remove 
                // it from the table.
                //
                _designers[component] = designer;
 
                try {
                    designer.Initialize(component); 
                    if (designer.Component == null) { 
                        throw new InvalidOperationException(SR.GetString(SR.DesignerHostDesignerNeedsComponent));
                    } 
                }
                catch {
                    _designers.Remove(component);
                    throw; 
                }
 
                // Designers can also implement IExtenderProvider. 
                //
                if (designer is IExtenderProvider) { 
                    IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                    if (eps != null) {
                        eps.AddExtenderProvider((IExtenderProvider)designer);
                    } 
                }
            } 
 
            // The component has been added.  Note that it is tempting to move this above the
            // designer because the designer will never need to know that its own component just 
            // got added, but this would be bad because the designer is needed to extract
            // shadowed properties from the component.
            //
            ComponentEventArgs ce = new ComponentEventArgs(component); 
            ComponentEventHandler eh = _events[EventComponentAdded] as ComponentEventHandler;
            if (eh != null) { 
                eh(containerToAddTo, ce); 
            }
        } 

        /// <devdoc>
        ///     Called by DesignerSurface to begin loading the designer.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        internal void BeginLoad(DesignerLoader loader) { 
 
            if (_loader != null && _loader != loader) {
                Exception ex = new InvalidOperationException(SR.GetString(SR.DesignerHostLoaderSpecified)); 
                ex.HelpLink = SR.DesignerHostLoaderSpecified;
                throw ex;
            }
 
            IDesignerEventService des = null;
            bool reloading = (_loader != null); 
            _loader = loader; 

            if (!reloading) { 
                if (loader is IExtenderProvider) {
                    IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                    if (eps != null) {
                        eps.AddExtenderProvider((IExtenderProvider)loader); 
                    }
                } 
 
                des = GetService(typeof(IDesignerEventService)) as IDesignerEventService;
                if (des != null) { 
                    des.ActiveDesignerChanged += new ActiveDesignerEventHandler(this.OnActiveDesignerChanged);
                    _designerEventService = des;
                }
            } 

            _state[StateLoading] = true; 
            _surface.OnLoading(); 

            try { 
                _loader.BeginLoad(this);
            }
            catch (Exception e) {
 
                if (e is TargetInvocationException) {
                    e = e.InnerException; 
                } 

                string message = e.Message; 

                // We must handle the case of an exception with no message.
                //
                if (message == null || message.Length == 0) { 
                    e = new Exception(SR.GetString(SR.DesignSurfaceFatalError, e.ToString()), e);
                } 
 
                // Loader blew up.  Add this exception to our error list.
                // 
                ((IDesignerLoaderHost)this).EndLoad(null, false, new object[] {e});
            }

            if (_designerEventService == null) { 
                // If there is no designer event service, make this designer the currently
                // active designer.  It will remain active. 
                OnActiveDesignerChanged(null, new ActiveDesignerEventArgs(null, this)); 
            }
        } 

        /// <summary>
        ///     Override of CreateSite.  We create a custom site here, called Site,
        ///     which is an inner class of DesignerHost.  DesignerSite contains an instance 
        ///     of the designer for the component.
        /// </summary> 
        /// <param name="component"> 
        ///     The component to create the site for
        /// </param> 
        /// <param name="name">
        ///     The name of the component.  If no name is provided this
        ///     will fabricate a name for you.
        /// </param> 
        /// <returns>
        ///     The newly created site 
        /// </returns> 
        protected override ISite CreateSite(IComponent component, string name) {
            Debug.Assert(component != null, "Caller should have guarded against a null component"); 

            // We need to handle the case where a component's ctor adds itself
            // to the container.  We don't want to do the work of creating a name,
            // and then immediately renaming.  So, DesignerHost's CreateComponent 
            // will set _newComponentName to the newly created name before
            // creating the component. 
            // 
            if (_newComponentName != null) {
                name = _newComponentName; 
                _newComponentName = null;
            }

            INameCreationService nameCreate = GetService(typeof(INameCreationService)) as INameCreationService; 

            // Fabricate a name if one wasn't provided.  We try to use the name 
            // creation service, but if it is not available we will just use an 
            // empty string.
            // 
            if (name == null) {
                if (nameCreate != null) {
                    name = nameCreate.CreateName(this, TypeDescriptor.GetReflectionType(component));
                } 
                else {
                    name = string.Empty; 
                } 
            }
            else { 
                if (nameCreate != null) {
                    nameCreate.ValidateName(name);
                }
            } 

            return new Site(component, this, name, this); 
        } 

        /// <devdoc> 
        ///     Override of dispose to clean up our state.
        /// </devdoc>
        protected override void Dispose(bool disposing) {
 
            if (disposing) {
                throw new InvalidOperationException(SR.GetString(SR.DesignSurfaceContainerDispose)); 
            } 

            base.Dispose(disposing); 
        }

        /// <devdoc>
        ///     We move all "dispose" functionality to the 
        ///     DisposeHost method.  The reason for this is that
        ///     Dispose is inherited from our container implementation, 
        ///     and we do not want someone disposing the container.  That 
        ///     would leave the design surface still alive, but it would
        ///     kill the host.  Instead, DesignSurface always calls 
        ///     DisposeHost, which calls the base version of Dispose
        ///     to clean out the container.
        /// </devdoc>
        internal void DisposeHost() { 

            try { 
                if (_loader != null) { 
                    _loader.Dispose();
                    Unload(); 
                }

                if (_surface != null) {
 
                    if (_designerEventService != null) {
                        _designerEventService.ActiveDesignerChanged -= new ActiveDesignerEventHandler(this.OnActiveDesignerChanged); 
                    } 

                    DesignSurfaceServiceContainer dsc = GetService(typeof(DesignSurfaceServiceContainer)) as DesignSurfaceServiceContainer; 
                    if (dsc != null) {
                        foreach(Type t in DefaultServices) {
                            dsc.RemoveFixedService(t);
                        } 
                    }
                    else { 
                        IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
                        Debug.Assert(sc != null, "DesignerHost: Ctor needs a service provider that provides IServiceContainer");
                        if (sc != null) { 
                            foreach(Type t in DefaultServices) {
                                sc.RemoveService(t);
                            }
                        } 
                    }
                } 
            } 
            finally {
                _loader = null; 
                _surface = null;
                _events.Dispose();
            }
 
            base.Dispose(true);
        } 
 
        /// <devdoc>
        ///     Invokes flush on the designer loader. 
        /// </devdoc>
        internal void Flush() {
            if (_loader != null) {
                _loader.Flush(); 
            }
        } 
 
        /// <summary>
        ///     Override of Container's GetService method.  This just delegates to the 
        ///     parent service provider.
        /// </summary>
        /// <param name="service">
        ///     The type of service to retrieve 
        /// </param>
        /// <returns> 
        ///     An instance of the service. 
        /// </returns>
        protected override object GetService(Type service) { 
            object serviceInstance = null;

            if (service == null) {
                throw new ArgumentNullException("service"); 
            }
 
            serviceInstance = base.GetService(service); 

            if (serviceInstance == null && _surface != null) { 
                serviceInstance = _surface.GetService(service);
            }

            return serviceInstance; 
        }
 
        /// <devdoc> 
        ///     Called in response to a designer becoming active or inactive.
        /// </devdoc> 
        private void OnActiveDesignerChanged(object sender, ActiveDesignerEventArgs e) {

            // NOTE: sender can be null (we call this directly in BeginLoad)
 
            object eventobj = null;
 
            if (e.OldDesigner == this) { 
                eventobj = EventDeactivated;
            } 
            else if (e.NewDesigner == this) {
                eventobj = EventActivated;
            }
 
            // Not our document, so we don't fire.
            // 
            if (eventobj == null) { 
                return;
            } 


            // If we are deactivating, flush any code changes.
            // We always route through the design surface 
            // so it can correctly raise its Flushed event.
            // 
            Debug.Assert(_surface != null, "calling OnActiveDesignerChanged on a disposed DesignerHost"); 
            if (e.OldDesigner == this && _surface != null) {
                _surface.Flush(); 
            }


 
            // Fire the appropriate event.
            // 
            EventHandler handler = _events[eventobj] as EventHandler; 
            if (handler != null) {
                handler(this, EventArgs.Empty); 
            }
        }

        /// <devdoc> 
        ///     Method is called by the site when a component is renamed.
        /// </devdoc> 
        private void OnComponentRename(IComponent component, string oldName, string newName) { 
            // If the root component is being renamed we need to update RootComponentClassName.
            if(component == _rootComponent) { 
                string className = _rootComponentClassName;
                int oldNameIndex = className.LastIndexOf(oldName);

                if (oldNameIndex + oldName.Length == className.Length                // If oldName occurs at the end of className 
                    && (oldNameIndex - 1 >= 0 && className[oldNameIndex - 1] == '.'))   // and is preceeded by a period
                { 
                    // We assume the preceeding chars are the namespace and preserve it. 
                    _rootComponentClassName = className.Substring(0, oldNameIndex) + newName;
                } 
                else {
                    _rootComponentClassName = newName;
                }
            } 

            ComponentRenameEventHandler eh = _events[EventComponentRename] as ComponentRenameEventHandler; 
            if (eh != null) { 
                eh(this, new ComponentRenameEventArgs(component, oldName, newName));
            } 
        }

        /// <devdoc>
        ///     Method is called when the designer has finished loading. 
        /// </devdoc>
        private void OnLoadComplete(EventArgs e) { 
            EventHandler eh = _events[EventLoadComplete] as EventHandler; 
            if (eh != null) eh(this, e);
        } 

        /// <devdoc>
        ///     Method is called when the last transaction has closed.
        /// </devdoc> 
        private void OnTransactionClosed(DesignerTransactionCloseEventArgs e) {
            DesignerTransactionCloseEventHandler eh = _events[EventTransactionClosed] as DesignerTransactionCloseEventHandler; 
            if (eh != null) eh(this, e); 
        }
 
        /// <devdoc>
        ///     Method is called when the last transaction is closing.
        /// </devdoc>
        private void OnTransactionClosing(DesignerTransactionCloseEventArgs e) { 
            DesignerTransactionCloseEventHandler eh = _events[EventTransactionClosing] as DesignerTransactionCloseEventHandler;
            if (eh != null) eh(this, e); 
        } 

        /// <devdoc> 
        ///     Method is called when the first transaction has opened.
        /// </devdoc>
        private void OnTransactionOpened(EventArgs e) {
            EventHandler eh = _events[EventTransactionOpened] as EventHandler; 
            if (eh != null) eh(this, e);
        } 
 
        /// <devdoc>
        ///     Method is called when the first transaction is opening. 
        /// </devdoc>
        private void OnTransactionOpening(EventArgs e) {
            EventHandler eh = _events[EventTransactionOpening] as EventHandler;
            if (eh != null) eh(this, e); 
        }
 
        /// <devdoc> 
        ///     Called to remove a component from its container.
        /// </devdoc> 
        public override void Remove(IComponent component) {
            if (RemoveFromContainerPreProcess(component, this)) {
                Site site = component.Site as Site;
                Debug.Assert(site != null, "RemoveFromContainerPreProcess should have returned false for this."); 
                RemoveWithoutUnsiting(component);
                site.Disposed = true; 
                RemoveFromContainerPostProcess(component, this); 
            }
        } 

        internal bool RemoveFromContainerPreProcess(IComponent component, IContainer container) {
            if (component == null) {
                throw new ArgumentNullException("component"); 
            }
 
            ISite site = component.Site; 
            if (site == null || site.Container != container) {
                return false; 
            }

            ComponentEventHandler eh;
            ComponentEventArgs ce = new ComponentEventArgs(component); 

            /* 
 

 



 

 
 

 
*/

            eh = _events[EventComponentRemoving] as ComponentEventHandler;
            if (eh != null) { 
                eh(this, ce);
            } 
 
            // If the component is an extender provider, remove it from
            // the extender provider service, should one exist. 
            //
            if (component is IExtenderProvider) {
                IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                if (eps != null) { 
                    eps.RemoveExtenderProvider((IExtenderProvider)component);
                } 
            } 

            // Same for the component's designer 
            //
            IDesigner designer = _designers[component] as IDesigner;

            if (designer is IExtenderProvider) { 
                IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                if (eps != null) { 
                    eps.RemoveExtenderProvider((IExtenderProvider)designer); 
                }
            } 

            if (designer != null) {
                designer.Dispose();
                _designers.Remove(component); 
            }
 
            if (component == _rootComponent) { 
                _rootComponent = null;
                _rootComponentClassName = null; 
            }

            return true;
        } 

        internal void RemoveFromContainerPostProcess(IComponent component, IContainer container) { 
 
            // VSWhidbey 464535
            // At one point during Whidbey, the component used to be unsited earlier in this process 
            // and it would be temporarily resited here before raising OnComponentRemoved. The problem with resiting
            // it is that some 3rd party controls take action when a component is sited (such as displaying
            // a dialog a control is dropped on the form) and resiting here caused them to think they were
            // being initialized for the first time.  To preserve compat, we shouldn't resite the component 
            // during Remove.
 
            try { 
                ComponentEventHandler eh = _events[EventComponentRemoved] as ComponentEventHandler;
                ComponentEventArgs ce = new ComponentEventArgs(component); 
                if (eh != null) {
                    eh(this, ce);
                }
            } 
            finally {
                component.Site = null; 
            } 
        }
 
        /// <devdoc>
        ///     Called to unload the design surface.
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private void Unload() {
 
            _surface.OnUnloading(); 

            IHelpService helpService = GetService(typeof(IHelpService)) as IHelpService; 
            if (helpService != null && _rootComponent != null && _designers[_rootComponent] != null) {
                helpService.RemoveContextAttribute("Keyword", "Designer_" + _designers[_rootComponent].GetType().FullName);
            }
 
            ISelectionService selectionService = (ISelectionService)GetService(typeof(ISelectionService));
            if (selectionService != null) { 
                selectionService.SetSelectedComponents(null, SelectionTypes.Replace); 
            }
 
            // Now remove all the designers and their components.  We save the root
            // for last.  Note that we eat any exceptions that components or their
            // designers generate.  A bad component or designer should not prevent
            // an unload from happening.  We do all of this in a transaction to help 
            // reduce the number of events we generate.
            // 
            _state[StateUnloading] = true; 
            DesignerTransaction t = ((IDesignerHost)this).CreateTransaction();
            ArrayList exceptions = new ArrayList(); 

            try {
                IComponent[] components = new IComponent[Components.Count];
                Components.CopyTo(components, 0); 

                foreach(IComponent comp in components) { 
                    if (!object.ReferenceEquals(comp,_rootComponent)) { 
                        IDesigner designer = _designers[comp] as IDesigner;
                        if (designer != null) { 
                            _designers.Remove(comp);
                            try {designer.Dispose();}
                            catch (Exception e){
                                string failedComponent = designer != null ? designer.GetType().Name : string.Empty; 
                                Debug.Fail( string.Format(CultureInfo.CurrentCulture, "Designer threw during unload: {0}", failedComponent));
                                exceptions.Add(e); 
                            } 

                        } 
                        try {comp.Dispose();}
                        catch (Exception e) {
                            string failedComponent = comp != null ? comp.GetType().Name : string.Empty;
                            Debug.Fail( string.Format(CultureInfo.CurrentCulture, "Component threw during unload: {0}", failedComponent)); 
                            exceptions.Add(e);
                        } 
                    } 
                }
 
                if (_rootComponent != null) {
                    IDesigner designer = _designers[_rootComponent] as IDesigner;
                    if (designer != null) {
                        _designers.Remove(_rootComponent); 
                        try {designer.Dispose();}
                        catch (Exception e) { 
                            string failedComponent = designer != null ? designer.GetType().Name : string.Empty; 
                            Debug.Fail( string.Format(CultureInfo.CurrentCulture, "Designer threw during unload: {0}", failedComponent));
                            exceptions.Add(e); 
                        }
                    }
                    try {_rootComponent.Dispose();}
                    catch (Exception e) { 
                        string failedComponent = _rootComponent != null ? _rootComponent.GetType().Name : string.Empty;
                        Debug.Fail( string.Format(CultureInfo.CurrentCulture, "Component threw during unload: {0}", failedComponent)); 
                        exceptions.Add(e); 
                    }
                } 

                _designers.Clear();
                while(Components.Count > 0) Remove(Components[0]);
            } 
            finally {
                t.Commit(); 
                _state[StateUnloading] = false; 
            }
 
            // There should be no open transactions.  Commit all of the ones that are
            // open.
            //
            if (_transactions != null && _transactions.Count > 0) { 
                Debug.Fail("There are open transactions at unload");
                while(_transactions.Count > 0) { 
                    DesignerTransaction trans = (DesignerTransaction)_transactions.Peek(); // it'll get pop'ed in the OnCommit for DesignerHostTransaction 
                    #if DEBUG
                    Debug.Fail(string.Format(CultureInfo.CurrentCulture, "Stack of {0}:\r\n{1}", trans.Description, ((DesignerHostTransaction)trans).CreatorStack)); 
                    #endif
                    trans.Commit();
                }
            } 

            _surface.OnUnloaded(); 
 
            if (exceptions.Count > 0) {
                throw new ExceptionCollection(exceptions); 
            }
        }

        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentAdded event.
        /// </devdoc> 
        event ComponentEventHandler IComponentChangeService.ComponentAdded { 
            add {
                _events.AddHandler(EventComponentAdded, value); 
            }
            remove {
                _events.RemoveHandler(EventComponentAdded, value);
            } 
        }
 
        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentAdding event.
        /// </devdoc> 
        event ComponentEventHandler IComponentChangeService.ComponentAdding {
            add {
                _events.AddHandler(EventComponentAdding, value);
            } 
            remove {
                _events.RemoveHandler(EventComponentAdding, value); 
            } 
        }
 
        /// <devdoc>
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentChanged event.
        /// </devdoc>
        event ComponentChangedEventHandler IComponentChangeService.ComponentChanged { 
            add {
                _events.AddHandler(EventComponentChanged, value); 
            } 
            remove {
                _events.RemoveHandler(EventComponentChanged, value); 
            }
        }

        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentChanging event.
        /// </devdoc> 
        event ComponentChangingEventHandler IComponentChangeService.ComponentChanging { 
            add {
                _events.AddHandler(EventComponentChanging, value); 
            }
            remove {
                _events.RemoveHandler(EventComponentChanging, value);
            } 
        }
 
        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRemoved event.
        /// </devdoc> 
        event ComponentEventHandler IComponentChangeService.ComponentRemoved {
            add {
                _events.AddHandler(EventComponentRemoved, value);
            } 
            remove {
                _events.RemoveHandler(EventComponentRemoved, value); 
            } 
        }
 
        /// <devdoc>
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRemoving event.
        /// </devdoc>
        event ComponentEventHandler IComponentChangeService.ComponentRemoving { 
            add {
                _events.AddHandler(EventComponentRemoving, value); 
            } 
            remove {
                _events.RemoveHandler(EventComponentRemoving, value); 
            }
        }

        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRename event.
        /// </devdoc> 
        event ComponentRenameEventHandler IComponentChangeService.ComponentRename { 
            add {
                _events.AddHandler(EventComponentRename, value); 
            }
            remove {
                _events.RemoveHandler(EventComponentRename, value);
            } 
        }
 
        /// <devdoc> 
        ///     Announces to the component change service that a particular component has changed.
        /// </devdoc> 
        void IComponentChangeService.OnComponentChanged(object component, MemberDescriptor member, object oldValue, object newValue) {

            if (!((IDesignerHost)this).Loading) {
                ComponentChangedEventHandler eh = _events[EventComponentChanged] as ComponentChangedEventHandler; 
                if (eh != null) {
                    eh(this, new ComponentChangedEventArgs(component, member, oldValue, newValue)); 
                } 
            }
        } 

        /// <devdoc>
        ///     Announces to the component change service that a particular component is changing.
        /// </devdoc> 
        void IComponentChangeService.OnComponentChanging(object component, MemberDescriptor member) {
            if (!((IDesignerHost)this).Loading) { 
                ComponentChangingEventHandler eh = _events[EventComponentChanging] as ComponentChangingEventHandler; 
                if (eh != null) {
                    eh(this, new ComponentChangingEventArgs(component, member)); 
                }
            }
        }
 
        /// <devdoc>
        ///     Gets or sets a value indicating whether the designer host 
        ///     is currently loading the document. 
        /// </devdoc>
        bool IDesignerHost.Loading { 
            get {
                return _state[StateLoading] || _state[StateUnloading] || (_loader != null && _loader.Loading);
            }
        } 

        /// <devdoc> 
        ///     Gets a value indicating whether the designer host is currently in a transaction. 
        /// </devdoc>
        bool IDesignerHost.InTransaction { 
            get {
                return (_transactions != null && _transactions.Count > 0) || IsClosingTransaction;
            }
        } 

        /// <devdoc> 
        ///     Gets the container for this designer host. 
        /// </devdoc>
        IContainer IDesignerHost.Container { 
            get {
                return this;
            }
        } 

        /// <devdoc> 
        ///     Gets the instance of the base class used as the base class for the current design. 
        /// </devdoc>
        IComponent IDesignerHost.RootComponent { 
            get {
                return _rootComponent;
            }
        } 

        /// <devdoc> 
        ///     Gets the fully qualified name of the class that is being designed. 
        /// </devdoc>
        string IDesignerHost.RootComponentClassName { 
            get {
                return _rootComponentClassName;
            }
        } 

        /// <devdoc> 
        ///     Gets the description of the current transaction. 
        /// </devdoc>
        string IDesignerHost.TransactionDescription { 
            get {
                if (_transactions != null && _transactions.Count > 0) {
                    return ((DesignerTransaction)_transactions.Peek()).Description;
                } 
                return null;
            } 
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Activated'/> event.
        /// </devdoc>
        event EventHandler IDesignerHost.Activated {
            add { 
                _events.AddHandler(EventActivated, value);
            } 
            remove { 
                _events.RemoveHandler(EventActivated, value);
            } 
        }

        /// <devdoc>
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Deactivated'/> event. 
        /// </devdoc>
        event EventHandler IDesignerHost.Deactivated { 
            add { 
                _events.AddHandler(EventDeactivated, value);
            } 
            remove {
                _events.RemoveHandler(EventDeactivated, value);
            }
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.LoadComplete'/> event. 
        /// </devdoc>
        event EventHandler IDesignerHost.LoadComplete { 
            add {
                _events.AddHandler(EventLoadComplete, value);
            }
            remove { 
                _events.RemoveHandler(EventLoadComplete, value);
            } 
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosed'/> event.
        /// </devdoc>
        event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosed {
            add { 
                _events.AddHandler(EventTransactionClosed, value);
            } 
            remove { 
                _events.RemoveHandler(EventTransactionClosed, value);
            } 
        }

        /// <devdoc>
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosing'/> event. 
        /// </devdoc>
        event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosing { 
            add { 
                _events.AddHandler(EventTransactionClosing, value);
            } 
            remove {
                _events.RemoveHandler(EventTransactionClosing, value);
            }
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpened'/> event. 
        /// </devdoc>
        event EventHandler IDesignerHost.TransactionOpened { 
            add {
                _events.AddHandler(EventTransactionOpened, value);
            }
            remove { 
                _events.RemoveHandler(EventTransactionOpened, value);
            } 
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpening'/> event.
        /// </devdoc>
        event EventHandler IDesignerHost.TransactionOpening {
            add { 
                _events.AddHandler(EventTransactionOpening, value);
            } 
            remove { 
                _events.RemoveHandler(EventTransactionOpening, value);
            } 
        }

        /// <devdoc>
        ///     Activates the designer that this host is hosting. 
        /// </devdoc>
        void IDesignerHost.Activate() { 
            _surface.OnViewActivate(); 
        }
 
        /// <devdoc>
        ///     Creates a component of the specified class type.
        /// </devdoc>
        IComponent IDesignerHost.CreateComponent(Type componentType) { 
            return ((IDesignerHost)this).CreateComponent(componentType, null);
        } 
 
        /// <devdoc>
        ///     Creates a component of the given class type and name and places it into the designer container. 
        /// </devdoc>
        IComponent IDesignerHost.CreateComponent(Type componentType, string name) {
            if (componentType == null) {
                throw new ArgumentNullException("componentType"); 
            }
 
            IComponent component; 

            LicenseContext oldContext = LicenseManager.CurrentContext; 
            bool changingContext = false; // we don't want if there is a recursivity (creating a component create another one)
                                            // to change the context again. we already have the one we want and that would create
                                            // a locking problem. see bug VSWhidbey 441200
            if(oldContext != LicenseContext) { 
                LicenseManager.CurrentContext = LicenseContext;
                LicenseManager.LockContext(_selfLock); 
                changingContext = true; 
            }
 
            try {
                try {
                    _newComponentName = name;
                    component = _surface.CreateInstance(componentType) as IComponent; 
                }
                finally { 
                    _newComponentName = null; 
                }
 
                if (component == null) {
                    InvalidOperationException ex = new InvalidOperationException(SR.GetString(SR.DesignerHostFailedComponentCreate, componentType.Name));
                    ex.HelpLink = SR.DesignerHostFailedComponentCreate;
                    throw ex; 
                }
 
                // Add this component to our container 
                //
                if (component.Site == null || component.Site.Container != this) { 
                    Add(component, name);
                }
            }
            finally { 
                if(changingContext) {
                    LicenseManager.UnlockContext(_selfLock); 
                    LicenseManager.CurrentContext = oldContext; 
                }
            } 

            return component;
        }
 
        /// <devdoc>
        ///     Lengthy operations that involve multiple components may raise many events.  These events 
        ///     may cause other side-effects, such as flicker or performance degradation.  When operating 
        ///     on multiple components at one time, or setting multiple properties on a single component,
        ///     you should encompass these changes inside a transaction.  Transactions are used 
        ///     to improve performance and reduce flicker.  Slow operations can listen to
        ///     transaction events and only do work when the transaction completes.
        /// </devdoc>
        DesignerTransaction IDesignerHost.CreateTransaction() { 
            return ((IDesignerHost)this).CreateTransaction(null);
        } 
 
        /// <devdoc>
        ///     Lengthy operations that involve multiple components may raise many events.  These events 
        ///     may cause other side-effects, such as flicker or performance degradation.  When operating
        ///     on multiple components at one time, or setting multiple properties on a single component,
        ///     you should encompass these changes inside a transaction.  Transactions are used
        ///     to improve performance and reduce flicker.  Slow operations can listen to 
        ///     transaction events and only do work when the transaction completes.
        /// </devdoc> 
        DesignerTransaction IDesignerHost.CreateTransaction(string description) { 
            if (description == null) {
                description = SR.GetString(SR.DesignerHostGenericTransactionName); 
            }

            return new DesignerHostTransaction(this, description);
        } 

        /// <devdoc> 
        ///     Destroys the given component, removing it from the design container. 
        /// </devdoc>
        void IDesignerHost.DestroyComponent(IComponent component) { 
            string name;

            if (component == null) {
                throw new ArgumentNullException("component"); 
            }
 
            if (component.Site != null && component.Site.Name != null) { 
                name = component.Site.Name;
            } 
            else {
                name = component.GetType().Name;
            }
 
            // Make sure the component is not being inherited -- we can't delete these!
            // 
            // 
            InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(component)[typeof(InheritanceAttribute)];
            if (ia != null && ia.InheritanceLevel != InheritanceLevel.NotInherited) { 
                Exception ex = new InvalidOperationException(SR.GetString(SR.DesignerHostCantDestroyInheritedComponent, name));
                ex.HelpLink = SR.DesignerHostCantDestroyInheritedComponent;
                throw ex;
            } 

            if (((IDesignerHost)this).InTransaction) 
            { 
                 Remove(component);		
                 component.Dispose(); 
            }
            else
            {
 
                DesignerTransaction t;
                using (t = ((IDesignerHost)this).CreateTransaction(SR.GetString(SR.DesignerHostDestroyComponentTransaction, name))) { 
 
                    // We need to signal changing and then perform the remove.  Remove must be done by us and not
                    // by Dispose because (a) people need a chance to cancel through a Removing event, and (b) 
                    // Dispose removes from the container last and anything that would sync Removed would end up
                    // with a dead component.
                    //
                    // 
                    Remove(component);
                    // 
    				 
                    component.Dispose();
                    t.Commit(); 
                }
            }
        }
 
        /// <devdoc>
        ///     Gets the designer instance for the specified component. 
        /// </devdoc> 
        IDesigner IDesignerHost.GetDesigner(IComponent component) {
            if (component == null) { 
                throw new ArgumentNullException("component");
            }
            return _designers[component] as IDesigner;
        } 

        /// <devdoc> 
        ///     Gets the type instance for the specified fully qualified type name <paramref name="TypeName"/>. 
        /// </devdoc>
        Type IDesignerHost.GetType(string typeName) { 

            if (typeName == null) {
                throw new ArgumentNullException("typeName");
            } 

            ITypeResolutionService ts = GetService(typeof(ITypeResolutionService)) as ITypeResolutionService; 
            if (ts != null) { 
                return ts.GetType(typeName);
            } 
            return Type.GetType(typeName);
        }

        /// <devdoc> 
        ///     This is called by the designer loader to indicate that the load has
        ///     terminated.  If there were errors, they should be passed in the errorCollection 
        ///     as a collection of exceptions (if they are not exceptions the designer 
        ///     loader host may just call ToString on them).  If the load was successful then
        ///     errorCollection should either be null or contain an empty collection. 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        void IDesignerLoaderHost.EndLoad(string rootClassName, bool successful, ICollection errorCollection) {
 
            bool wasLoading = _state[StateLoading];
            _state[StateLoading] = false; 
 
            if (rootClassName != null) {
                _rootComponentClassName = rootClassName; 
            }
            else if (_rootComponent != null && _rootComponent.Site != null) {
                _rootComponentClassName = _rootComponent.Site.Name;
            } 

            // If the loader indicated success, but it never created a component, that is 
            // an error. 
            //
            if (successful && _rootComponent == null) { 
                ArrayList errorList = new ArrayList();
                InvalidOperationException ex = new InvalidOperationException(SR.GetString(SR.DesignerHostNoBaseClass));
                ex.HelpLink = SR.DesignerHostNoBaseClass;
                errorList.Add(ex); 
                errorCollection = errorList;
                successful = false; 
            } 

            // If we failed, unload the doc so that the OnLoaded event 
            // can't get to anything that actually did work.
            //
            if (!successful) {
                Unload(); 
            }
 
            if (wasLoading && _surface != null) { 
                _surface.OnLoaded(successful, errorCollection);
            } 

            if (successful) {

                // We may be invoked to do an EndLoad when we are already loaded.  This can happen 
                // if the user called AddLoadDependency, essentially putting us in a loading state
                // while we are already loaded.  This is OK, and is used as a hint that the user 
                // is going to monkey with settings but doesn't want the code engine to report 
                // it.
                // 
                if (wasLoading) {

                    IRootDesigner rootDesigner = ((IDesignerHost)this).GetDesigner(_rootComponent) as IRootDesigner;
 
                    // Offer up our base help attribute
                    // 
                    IHelpService helpService = GetService(typeof(IHelpService)) as IHelpService; 
                    if (helpService != null) {
                        helpService.AddContextAttribute("Keyword", "Designer_" + rootDesigner.GetType().FullName, HelpKeywordType.F1Keyword); 
                    }

                    // and let everyone know that we're loaded
                    // 
                    try {
                        OnLoadComplete(EventArgs.Empty); 
                    } 
                    catch (Exception ex) {
 
                        Debug.Fail("Exception thrown on LoadComplete event handler.  You should not throw here : " + ex.ToString());

                        // The load complete failed.  Put us back in the loading state and unload.
                        // 
                        _state[StateLoading] = true;
                        Unload(); 
 
                        ArrayList errorList = new ArrayList();
                        errorList.Add(ex); 
                        if (errorCollection != null) {
                            errorList.AddRange(errorCollection);
                        }
                        errorCollection = errorList; 
                        successful = false;
 
                        if (_surface != null) { 
                            _surface.OnLoaded(successful, errorCollection);
                        } 

                        // We re-throw.  If this was a synchronous load this will
                        // error back to BeginLoad (and, as a side effect, may call
                        // us again).  For asynchronous loads we need to throw so the 
                        // caller knows what happened.
                        // 
                        throw; 
                    }
 
                    // If we saved a selection as a result of a reload, try to replace it.
                    //
                    if (successful && _savedSelection != null) {
                        ISelectionService ss = GetService(typeof(ISelectionService)) as ISelectionService; 
                        if (ss != null) {
                            ArrayList selectedComponents = new ArrayList(_savedSelection.Count); 
                            foreach(string name in _savedSelection) { 
                                IComponent comp = Components[name];
                                if (comp != null) { 
                                    selectedComponents.Add(comp);
                                }
                            }
 
                            _savedSelection = null;
                            ss.SetSelectedComponents(selectedComponents, SelectionTypes.Replace); 
                        } 
                    }
                } 
            }
        }

        /// <devdoc> 
        ///     This is called by the designer loader when it wishes to reload the
        ///     design document.  The reload will happen immediately so the caller 
        ///     should ensure that it is in a state where BeginLoad may be called again. 
        /// </devdoc>
        void IDesignerLoaderHost.Reload() { 
            if (_loader != null) {

                // Flush the loader to make sure there aren't any pending
                // changes.  We always route through the design surface 
                // so it can correctly raise its Flushed event.
                // 
                _surface.Flush(); 

                // Next, stash off the set of selected objects by name.  After 
                // the reload we will attempt to re-select them.
                //
                ISelectionService ss = GetService(typeof(ISelectionService)) as ISelectionService;
                if (ss != null) { 
                    ArrayList list = new ArrayList(ss.SelectionCount);
                    foreach(object o in ss.GetSelectedComponents()) { 
                        IComponent comp = o as IComponent; 
                        if (comp != null && comp.Site != null && comp.Site.Name != null) {
                            list.Add(comp.Site.Name); 
                        }
                    }
                    _savedSelection = list;
                } 

                Unload(); 
                BeginLoad(_loader); 
            }
        } 

        bool IDesignerLoaderHost2.IgnoreErrorsDuringReload {
            get {
                return _ignoreErrorsDuringReload; 
            }
 
            set { 
                // Only allow to set to true if we CanReloadWithErrors
                if (!value || ((IDesignerLoaderHost2)this).CanReloadWithErrors) { 
                    _ignoreErrorsDuringReload = value;
                }
            }
        } 

        bool IDesignerLoaderHost2.CanReloadWithErrors { 
            get { 
                return _canReloadWithErrors;
            } 

            set {
                _canReloadWithErrors = value;
            } 
        }
 
 
        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc>
        MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) {
            return typeof(IDesignerHost).GetMethod(name, bindingAttr, binder, types, modifiers); 
        }
 
        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private. 
        /// </devdoc>
        MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetMethod(name, bindingAttr);
        } 

        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc> 
        MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetMethods(bindingAttr);
        }
 
        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private. 
        /// </devdoc>
        FieldInfo IReflect.GetField(string name, BindingFlags bindingAttr) { 
            return typeof(IDesignerHost).GetField(name, bindingAttr);
        }

        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private. 
        /// </devdoc> 
        FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetFields(bindingAttr); 
        }

        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc> 
        PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr) { 
            return typeof(IDesignerHost).GetProperty(name, bindingAttr);
        } 

        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private. 
        /// </devdoc>
        PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) { 
            return typeof(IDesignerHost).GetProperty(name, bindingAttr, binder, returnType, types, modifiers); 
        }
 
        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private.
        /// </devdoc> 
        PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetProperties(bindingAttr); 
        } 

        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private.
        /// </devdoc>
        MemberInfo[] IReflect.GetMember(string name, BindingFlags bindingAttr) { 
            return typeof(IDesignerHost).GetMember(name, bindingAttr);
        } 
 
        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc>
        MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetMembers(bindingAttr); 
        }
 
        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private. 
        /// </devdoc>
        object IReflect.InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) {
            return typeof(IDesignerHost).InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        } 

        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc> 
        Type IReflect.UnderlyingSystemType {
            get {
                return typeof(IDesignerHost).UnderlyingSystemType;
            } 
        }
 
        /// <devdoc> 
        ///     Adds the given service to the service container.
        /// </devdoc> 
        void IServiceContainer.AddService(Type serviceType, object serviceInstance) {

            // Our service container is implemented on the parenting DesignSurface
            // object, so we just ask for its service container and run with it. 
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
            if (sc == null) { 
                throw new ObjectDisposedException("IServiceContainer");
            } 
            sc.AddService(serviceType, serviceInstance);
        }

        /// <devdoc> 
        ///     Adds the given service to the service container.
        /// </devdoc> 
        void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) { 

            // Our service container is implemented on the parenting DesignSurface 
            // object, so we just ask for its service container and run with it.
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer;
            if (sc == null) { 
                throw new ObjectDisposedException("IServiceContainer");
            } 
            sc.AddService(serviceType, serviceInstance, promote); 
        }
 
        /// <devdoc>
        ///     Adds the given service to the service container.
        /// </devdoc>
        void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) { 

            // Our service container is implemented on the parenting DesignSurface 
            // object, so we just ask for its service container and run with it. 
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
            if (sc == null) {
                throw new ObjectDisposedException("IServiceContainer");
            }
            sc.AddService(serviceType, callback); 
        }
 
        /// <devdoc> 
        ///     Adds the given service to the service container.
        /// </devdoc> 
        void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) {

            // Our service container is implemented on the parenting DesignSurface
            // object, so we just ask for its service container and run with it. 
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
            if (sc == null) { 
                throw new ObjectDisposedException("IServiceContainer");
            } 
            sc.AddService(serviceType, callback, promote);
        }

        /// <devdoc> 
        ///     Removes the given service type from the service container.
        /// </devdoc> 
        void IServiceContainer.RemoveService(Type serviceType) { 

            // Our service container is implemented on the parenting DesignSurface 
            // object, so we just ask for its service container and run with it.
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer;
            if (sc == null) { 
                throw new ObjectDisposedException("IServiceContainer");
            } 
            sc.RemoveService(serviceType); 
        }
 
        /// <devdoc>
        ///     Removes the given service type from the service container.
        /// </devdoc>
        void IServiceContainer.RemoveService(Type serviceType, bool promote) { 

            // Our service container is implemented on the parenting DesignSurface 
            // object, so we just ask for its service container and run with it. 
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
            if (sc == null) {
                throw new ObjectDisposedException("IServiceContainer");
            }
            sc.RemoveService(serviceType, promote); 
        }
 
        /// <devdoc> 
        ///     IServiceProvider implementation.  We just delegate to the
        ///     protected GetService method we are inheriting from our 
        ///     container.
        /// </devdoc>
        object IServiceProvider.GetService(Type serviceType) {
            return GetService(serviceType); 
        }
 
        /// <devdoc> 
        ///     DesignerHostTransaction is our implementation of the
        ///     DesignerTransaction abstract class. 
        /// </devdoc>
        private sealed class DesignerHostTransaction : DesignerTransaction {

            private DesignerHost _host; 

            #if DEBUG 
            private string _creatorStack; 
            #endif
 
            public DesignerHostTransaction(DesignerHost host, string description) : base(description) {
                _host = host;
                if (_host._transactions == null) {
                    _host._transactions = new Stack(); 
                }
 
                _host._transactions.Push(this); 
                _host.OnTransactionOpening(EventArgs.Empty);
                _host.OnTransactionOpened(EventArgs.Empty); 

                #if DEBUG
                _creatorStack = Environment.StackTrace;
                #endif 
            }
 
            #if DEBUG 
            /// <devdoc>
            ///     Debug info that displays the stack of the creation call of this 
            ///     transaction.  This is useful for tracking down orphaned transactions.
            /// </devdoc>
            public string CreatorStack {
                get { 
                    return _creatorStack;
                } 
            } 
            #endif
 
            #if DEBUG
            /// <devdoc>
            ///     We override Dispose to handle finalization cases so we
            ///     can display the stack of the transaction creator. 
            /// </devdoc>
            protected override void Dispose(bool disposing) { 
                base.Dispose(disposing); 

                if (!disposing) { 
                    Debug.Fail("Callstack of transaction creator: " + CreatorStack);
                }
            }
            #endif 

            /// <devdoc> 
            ///     User code should implement this method to perform 
            ///     the actual work of committing a transaction.
            /// </devdoc> 
            protected override void OnCancel() {
                if (_host != null) {
                    if (_host._transactions.Peek() != this) {
                        string nestedDescription = ((DesignerTransaction)_host._transactions.Peek()).Description; 
                        throw new InvalidOperationException(SR.GetString(SR.DesignerHostNestedTransaction, Description, nestedDescription));
                    } 
 
                    _host.IsClosingTransaction = true;
                    try { 
                        _host._transactions.Pop();
                        DesignerTransactionCloseEventArgs e = new DesignerTransactionCloseEventArgs(false, _host._transactions.Count == 0);
                        _host.OnTransactionClosing(e);
                        _host.OnTransactionClosed(e); 
                    } finally {
                        _host.IsClosingTransaction = false; 
                        _host = null; 
                    }
                } 
            }

            /// <devdoc>
            ///     User code should implement this method to perform 
            ///     the actual work of committing a transaction.
            /// </devdoc> 
            protected override void OnCommit() { 
                if (_host != null) {
                    if (_host._transactions.Peek() != this) { 
                        string nestedDescription = ((DesignerTransaction)_host._transactions.Peek()).Description;
                        throw new InvalidOperationException(SR.GetString(SR.DesignerHostNestedTransaction, Description, nestedDescription));
                    }
 
                    _host.IsClosingTransaction = true;
                    try { 
                        _host._transactions.Pop(); 
                        DesignerTransactionCloseEventArgs e = new DesignerTransactionCloseEventArgs(true, _host._transactions.Count == 0);
                        _host.OnTransactionClosing(e); 
                        _host.OnTransactionClosed(e);
                    } finally {
                        _host.IsClosingTransaction = false;
                        _host = null; 
                    }
                } 
            } 
        }
 
        /// <summary>
        ///     Site is the site we use at design time when we host
        ///     components.
        /// </summary> 
        internal class Site : ISite, IServiceContainer, IDictionaryService {
 
            private IComponent              _component; 
            private Hashtable               _dictionary;
            private DesignerHost            _host; 
            private string                  _name;
            private bool                    _disposed;
            private SiteNestedContainer     _nestedContainer;
            private Container               _container; 

            internal Site(IComponent component, DesignerHost host, string name, Container container) { 
                _component = component; 
                _host = host;
                _name = name; 
                _container = container;
            }

            /// <devdoc> 
            ///     Used by the IServiceContainer implementation to return a container-specific
            ///     service container. 
            /// </devdoc> 
            private IServiceContainer SiteServiceContainer {
                get { 
                    SiteNestedContainer nc = ((IServiceProvider)this).GetService(typeof(INestedContainer)) as SiteNestedContainer;
                    Debug.Assert(nc != null, "We failed to resolve a nested container.");
                    IServiceContainer sc = nc.GetServiceInternal(typeof(IServiceContainer)) as IServiceContainer;
                    Debug.Assert(sc != null, "We failed to resolve a service container from the nested container."); 
                    return sc;
                } 
            } 

            /// <devdoc> 
            ///     Retrieves the key corresponding to the given value.
            /// </devdoc>
            object IDictionaryService.GetKey(object value) {
                if (_dictionary != null) { 
                    foreach(DictionaryEntry de in _dictionary) {
                        object o = de.Value; 
                        if (value != null && value.Equals(o)) { 
                            return de.Key;
                        } 
                    }
                }
                return null;
            } 

            /// <devdoc> 
            ///     Retrieves the value corresponding to the given key. 
            /// </devdoc>
            object IDictionaryService.GetValue(object key) { 
                if (_dictionary != null) {
                    return _dictionary[key];
                }
                return null; 
            }
 
            /// <devdoc> 
            ///     Stores the given key-value pair in an object's site.  This key-value
            ///     pair is stored on a per-object basis, and is a handy place to save 
            ///     additional information about a component.
            /// </devdoc>
            void IDictionaryService.SetValue(object key, object value) {
                if (_dictionary == null) { 
                    _dictionary = new Hashtable();
                } 
                if (value == null) { 
                    _dictionary.Remove(key);
                } 
                else {
                    _dictionary[key] = value;
                }
            } 

            /// <devdoc> 
            ///     Adds the given service to the service container. 
            /// </devdoc>
            void IServiceContainer.AddService(Type serviceType, object serviceInstance) { 
                SiteServiceContainer.AddService(serviceType, serviceInstance);
            }

            /// <devdoc> 
            ///     Adds the given service to the service container.
            /// </devdoc> 
            void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) { 
                SiteServiceContainer.AddService(serviceType, serviceInstance, promote);
            } 

            /// <devdoc>
            ///     Adds the given service to the service container.
            /// </devdoc> 
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) {
                SiteServiceContainer.AddService(serviceType, callback); 
            } 

            /// <devdoc> 
            ///     Adds the given service to the service container.
            /// </devdoc>
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) {
                SiteServiceContainer.AddService(serviceType, callback, promote); 
            }
 
            /// <devdoc> 
            ///     Removes the given service type from the service container.
            /// </devdoc> 
            void IServiceContainer.RemoveService(Type serviceType) {
                SiteServiceContainer.RemoveService(serviceType);
            }
 
            /// <devdoc>
            ///     Removes the given service type from the service container. 
            /// </devdoc> 
            void IServiceContainer.RemoveService(Type serviceType, bool promote) {
                SiteServiceContainer.RemoveService(serviceType, promote); 
            }

            /// <summary>
            ///     Returns the requested service. 
            /// </summary>
            /// <param name="service"></param> 
            /// <returns></returns> 
            object IServiceProvider.GetService(Type service) {
                if (service == null) { 
                    throw new ArgumentNullException("service");
                }

                // We always resolve IDictionaryService to ourselves. 

                if (service == typeof(IDictionaryService)) { 
                    return this; 
                }
 
                // NestedContainer is demand created

                if (service == typeof(INestedContainer)) {
                    if (_nestedContainer == null) { 
                        _nestedContainer = new SiteNestedContainer(_component, null, _host);
                    } 
                    return _nestedContainer; 
                }
 
                // SiteNestedContainer does offer IServiceContainer and IContainer as services, but we
                // always want a default site query for these services to delegate to the host.
                // Because it is more common to add  services to the host than it is to add them to the site itself, and
                // also because we need this for backward compatibility. 

                if (service != typeof(IServiceContainer) && service != typeof(IContainer) && _nestedContainer != null) { 
                    return _nestedContainer.GetServiceInternal(service); 
                }
 
                return _host.GetService(service);
            }

            /// <summary> 
            ///     The component sited by this component site.
            /// </summary> 
            IComponent ISite.Component { 
                get {
                    return _component; 
                }
            }

            /// <summary> 
            ///     The container in which the component is sited.
            /// </summary> 
            IContainer ISite.Container { 
                get {
                    return _container; 
                }
            }

            /// <summary> 
            ///     Indicates whether the component is in design mode.
            /// </summary> 
            bool ISite.DesignMode { 
                get {
                    return true; 
                }
            }

            /// <summary> 
            ///     Indicates whether this Site has been disposed.
            /// </summary> 
            internal bool Disposed { 
                get {
                    return _disposed; 
                }
                set {
                    this._disposed = value;
                } 
            }
 
            /// <summary> 
            ///     The name of the component.
            /// </summary> 
            string ISite.Name {
                get {
                    return _name;
                } 
                set {
                    if (value == null) { 
                        value = string.Empty; 
                    }
 
                    if (_name != value) {
                        bool validateName = true;

                        if (value.Length > 0) { 
                            IComponent namedComponent = _container.Components[value];
 
                            validateName = (_component != namedComponent); 

                            // allow renames that are just case changes of the current name. 
                            //
                            if (namedComponent != null && validateName) {
                                Exception ex = new Exception(SR.GetString(SR.DesignerHostDuplicateName, value));
 
                                ex.HelpLink = SR.DesignerHostDuplicateName;
                                throw ex; 
                            } 
                        }
 
                        if (validateName) {
                            INameCreationService nameService = (INameCreationService)((IServiceProvider)this).GetService(typeof(INameCreationService));

                            if (nameService != null) { 
                                nameService.ValidateName(value);
                            } 
                        } 

                        // It is OK to change the name to this value.  Announce the change 
                        // and do it.
                        //
                        string oldName = _name;
                        _name = value; 
                        _host.OnComponentRename(_component, oldName, _name);
                    } 
                } 
            }
        } 
    }


    /// <summary> 
    ///     This is a nested container.  Anything added to the nested container will
    ///     be hostable in a designer. 
    /// </summary> 
    internal sealed class SiteNestedContainer: NestedContainer {
        private DesignerHost _host; 
        private IServiceContainer _services;
        private string _containerName;
        private bool _safeToCallOwner;
 
        internal SiteNestedContainer(IComponent owner, string containerName, DesignerHost host) : base(owner) {
            _containerName = containerName; 
            _host = host; 
            _safeToCallOwner = true;
        } 

        /// <devdoc>
        ///     Override to support named containers.
        /// </devdoc> 
        protected override string OwnerName {
            get { 
                string ownerName = base.OwnerName; 
                if (_containerName != null && _containerName.Length > 0) {
                    ownerName = string.Format(CultureInfo.CurrentCulture, "{0}.{1}", ownerName, _containerName); 
                }

                return ownerName;
            } 
        }
 
        /// <devdoc> 
        ///     Called to add a component to its container.
        /// </devdoc> 
        public override void Add(IComponent component, string name) {

            if (_host.AddToContainerPreProcess(component, name, this)) {
 
                // Site creation fabricates a name for this component.
                // 
                base.Add(component, name); 

                try { 
                    _host.AddToContainerPostProcess(component, name, this);
                }
                catch (Exception t) {
                    if (t != CheckoutException.Canceled) { 
                        Remove(component);
                    } 
                    throw; 
                }
                catch { 
                    Remove(component);
                    throw;
                }
            } 
        }
 
        /// <include file='doc\SiteNestedContainer.uex' path='docs/doc[@for="SiteNestedContainer.CreateSite"]/*' /> 
        /// <devdoc>
        ///     Creates a site for the component within the container. 
        /// </devdoc>
        protected override ISite CreateSite(IComponent component, string name) {
            if (component == null) {
                throw new ArgumentNullException("component"); 
            }
            return new NestedSite(component, _host, name, this); 
        } 

        /// <devdoc> 
        ///     Called to remove a component from its container.
        /// </devdoc>
        public override void Remove(IComponent component) {
            if (_host.RemoveFromContainerPreProcess(component, this)) { 
                ISite site = component.Site;
                Debug.Assert(site != null, "RemoveFromContainerPreProcess should have returned false for this."); 
                RemoveWithoutUnsiting(component); 
                _host.RemoveFromContainerPostProcess(component, this);
            } 
        }


        protected override object GetService(Type serviceType) { 
            object service = base.GetService(serviceType);
 
            if (service != null) { 
                return service;
            } 


            if (serviceType == typeof(IServiceContainer)) {
                if (_services == null) { 
                    _services = new ServiceContainer(_host);
                } 
 
                return _services;
            } 


            if (_services != null) {
                return _services.GetService(serviceType); 
            }
            else { 
                if (Owner.Site != null && _safeToCallOwner) { 
                    try {
                        _safeToCallOwner = false; 
                        return Owner.Site.GetService(serviceType);
                    }
                    finally {
                        _safeToCallOwner = true; 
                    }
                } 
            } 

            return null; 
        }

        internal object GetServiceInternal(Type serviceType) {
            return GetService(serviceType); 
        }
 
        private sealed class NestedSite : DesignerHost.Site, INestedSite 
        {
            private SiteNestedContainer _container; 
            private string           _name;

            internal NestedSite(IComponent component, DesignerHost host, string name, Container container)
            : base(component, host, name, container) 
            {
                 _container = container as SiteNestedContainer; 
                 _name = name; 
            }
 
            public string FullName {
                get {
                    if (_name != null) {
                        string ownerName = _container.OwnerName; 
                        string childName = ((ISite)this).Name;
                        if (ownerName != null) { 
                            childName = string.Format(CultureInfo.CurrentCulture, "{0}.{1}", ownerName, childName); 
                        }
 
                        return childName;
                    }

                    return _name; 
                }
            } 
        } 

    } 

}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerHost.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization; 
    using System.Reflection;
    using System.Text; 

    /// <devdoc>
    ///     This is the main hosting object.  DesignerHost implements
    ///     services and interfaces specific to the design time 
    ///     IContainer object.  The services this class implements
    ///     are generally non-removable (they work as a unit so removing 
    ///     them would break things). 
    /// </devdoc>
    internal sealed class DesignerHost : 
        Container,
        IDesignerLoaderHost2,
        IDesignerHostTransactionState,
        IComponentChangeService, 
        IReflect {
 
        // State flags for the state of the designer host 
        //
        private static readonly int StateLoading               = BitVector32.CreateMask();                // Designer is currently loading from the loader host. 
        private static readonly int StateUnloading             = BitVector32.CreateMask(StateLoading);    // Designer is currently unloading.
        private static readonly int StateIsClosingTransaction  = BitVector32.CreateMask(StateUnloading);  // A transaction is in the process of being Canceled or Commited.

        private static Type[] DefaultServices = new Type[] { 
                                                typeof(IDesignerHost),
                                                typeof(IContainer), 
                                                typeof(IComponentChangeService), 
                                                typeof(IDesignerLoaderHost2)
                                            }; 

        // IDesignerHost events
        //
        private static readonly object EventActivated           = new object(); // Designer has been activated 
        private static readonly object EventDeactivated         = new object(); // Designer has been deactivated
        private static readonly object EventLoadComplete        = new object(); // Loading has been completed 
        private static readonly object EventTransactionClosed   = new object(); // The last transaction has been closed 
        private static readonly object EventTransactionClosing  = new object(); // The last transaction is about to be closed
        private static readonly object EventTransactionOpened   = new object(); // The first transaction has been opened 
        private static readonly object EventTransactionOpening  = new object(); // The first transaction is about to be opened

        // IComponentChangeService events
        // 
        private static readonly object EventComponentAdding     = new object(); // A component is about to be added to the container
        private static readonly object EventComponentAdded      = new object(); // A component was just added to the container 
        private static readonly object EventComponentChanging   = new object(); // A component is about to be changed 
        private static readonly object EventComponentChanged    = new object(); // A component has changed
        private static readonly object EventComponentRemoving   = new object(); // A component is about to be removed from the container 
        private static readonly object EventComponentRemoved    = new object(); // A component has been removed from the container
        private static readonly object EventComponentRename     = new object(); // A component has been renamed

        // Member variables 
        //
        private BitVector32                     _state;                     // state for this host 
        private DesignSurface                   _surface;                   // the owning designer surface. 
        private string                          _newComponentName;          // transient value indicating the name of a component that is being created
        private Stack                           _transactions;              // stack of transactions.  Each entry in the stack is a DesignerTransaction 
        private IComponent                      _rootComponent;             // the root of our design
        private string                          _rootComponentClassName;    // class name of the root of our design
        private Hashtable                       _designers;                 // designer -> component mapping
        private EventHandlerList                _events;                    // event list 
        private DesignerLoader                  _loader;                    // the loader that loads our designers
        private ICollection                     _savedSelection;            // set of selected components saved across reloads 
        private HostDesigntimeLicenseContext    _licenseCtx; 
        private IDesignerEventService           _designerEventService;
        private static readonly object          _selfLock = new object(); 
        private bool                            _ignoreErrorsDuringReload;
        private bool                            _canReloadWithErrors;

 
        public DesignerHost(DesignSurface surface) {
 
            _surface = surface; 
            _state = new BitVector32();
            _designers = new Hashtable(); 
            _events = new EventHandlerList();

            // Add the relevant services.  We try to add these
            // as "fixed" services.  A fixed service cannot be 
            // removed by the user.  The reason for this is that
            // each of these services depends on each other, so 
            // you can't really remove and replace just one of them. 
            //
            // If we can't get our own service container that supports 
            // fixed services, we add these as regular services.
            //
            DesignSurfaceServiceContainer dsc = GetService(typeof(DesignSurfaceServiceContainer)) as DesignSurfaceServiceContainer;
            if (dsc != null) { 
                foreach(Type t in DefaultServices) {
                    dsc.AddFixedService(t, this); 
                } 
            }
            else { 
                IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer;
                Debug.Assert(sc != null, "DesignerHost: Ctor needs a service provider that provides IServiceContainer");
                if (sc != null) {
                    foreach(Type t in DefaultServices) { 
                        sc.AddService(t, this);
                    } 
                } 
            }
        } 

        internal HostDesigntimeLicenseContext LicenseContext {
            get {
                if (_licenseCtx == null) { 
                    _licenseCtx = new HostDesigntimeLicenseContext(this);
                } 
 
                return _licenseCtx;
            } 
        }

        // Internal flag which is used to track when we are in the process of commiting or canceling a transaction.
        internal bool IsClosingTransaction { 
            get { return _state[StateIsClosingTransaction]; }
            set { _state[StateIsClosingTransaction] = value; } 
        } 

        bool IDesignerHostTransactionState.IsClosingTransaction { 
            get { return this.IsClosingTransaction; }
        }

        /// <summary> 
        ///     Override of Container.Add
        /// </summary> 
        /// <param name="component"></param> 
        /// <param name="name"></param>
        public override void Add(IComponent component, string name) { 

            if (AddToContainerPreProcess(component, name, this)) {

                // Site creation fabricates a name for this component. 
                //
                base.Add(component, name); 
 
                try {
                    AddToContainerPostProcess(component, name, this); 
                }
                catch (Exception t) {
                    if (t != CheckoutException.Canceled) {
                        Remove(component); 
                    }
                    throw; 
                } 
                catch {
                    Remove(component); 
                    throw;
                }
            }
        } 

        /// <devdoc> 
        ///     We support adding to either our main IDesignerHost container or to a private 
        ///     per-site container for nested objects.  This code is the stock add code
        ///     that creates a designer, etc.  See Add (above) for an example of how to call 
        ///     this correctly.
        ///
        ///     This method is called before the component is actually added.  It returns true
        ///     if the component can be added to this container or false if the add should 
        ///     not occur (because the component may already be in this container, for example.)
        ///     It may also throw if adding this component is illegal. 
        /// </devdoc> 
        internal bool AddToContainerPreProcess(IComponent component, string name, IContainer containerToAddTo) {
            if (component == null) { 
                throw new ArgumentNullException("component");
            }

            // We should never add anything while we're unloading. 
            //
            if (_state[StateUnloading]) { 
                Exception ex = new Exception(SR.GetString(SR.DesignerHostUnloading)); 
                ex.HelpLink = SR.DesignerHostUnloading;
                throw ex; 
            }

            // Make sure we're not adding an instance of the root component to itself.
            // 
            if (_rootComponent != null) {
                if (string.Equals(component.GetType().FullName, _rootComponentClassName, StringComparison.OrdinalIgnoreCase)) { 
                    Exception ex = new Exception(SR.GetString(SR.DesignerHostCyclicAdd, component.GetType().FullName, _rootComponentClassName)); 
                    ex.HelpLink = SR.DesignerHostCyclicAdd;
                    throw ex; 
                }
            }

            ISite existingSite = component.Site; 

            // If the component is already in our container, we just rename. 
            // 
            if (existingSite != null && existingSite.Container == this) {
                if (name != null) { 
                    existingSite.Name = name;
                }
                return false;
            } 

            // Raise an adding event for our container if the container is us. 
            // 
            ComponentEventArgs ce = new ComponentEventArgs(component);
            ComponentEventHandler eh = _events[EventComponentAdding] as ComponentEventHandler; 
            if (eh != null) {
                eh(containerToAddTo, ce);
            }
 
            return true;
        } 
 
        /// <devdoc>
        ///     We support adding to either our main IDesignerHost container or to a private 
        ///     per-site container for nested objects.  This code is the stock add code
        ///     that creates a designer, etc.  See Add (above) for an example of how to call
        ///     this correctly.
        /// </devdoc> 
        internal void AddToContainerPostProcess(IComponent component, string name, IContainer containerToAddTo) {
            // Now that we've added, check to see if this is an extender provider.  If it is, 
            // add it to our extender provider service so it is available. 
            //
            if (component is IExtenderProvider && 
                //
                !TypeDescriptor.GetAttributes(component).Contains(InheritanceAttribute.InheritedReadOnly)) {
                IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                if (eps != null) { 
                    eps.AddExtenderProvider((IExtenderProvider)component);
                } 
            } 

            // Is this the first component the loader has created?  If so, then it must 
            // be the root component (by definition) so we will expect there to be a root
            // designer associated with the component.  Otherwise, we search for a
            // normal designer, which can be optionally provided.
            // 
            IDesigner designer = null;
 
            if (_rootComponent == null) { 

                designer = _surface.CreateDesigner(component, true) as IRootDesigner; 

                if (designer == null) {
                    Exception ex = new Exception(SR.GetString(SR.DesignerHostNoTopLevelDesigner, component.GetType().FullName));
                    ex.HelpLink = SR.DesignerHostNoTopLevelDesigner; 
                    throw ex;
                } 
 
                _rootComponent = component;
 
                // Check and see if anyone has set the class name of the root component.
                // we default to the component name.
                //
                if (_rootComponentClassName == null) { 
                    _rootComponentClassName = component.Site.Name;
                } 
            } 
            else {
                designer = _surface.CreateDesigner(component, false); 
            }

            if (designer != null) {
 
                // The presence of a designer in this table
                // allows the designer to filter the component's 
                // properties, which is often needed during designer 
                // initialization.  So, we stuff it in the table
                // first, initialize, and if it throws we remove 
                // it from the table.
                //
                _designers[component] = designer;
 
                try {
                    designer.Initialize(component); 
                    if (designer.Component == null) { 
                        throw new InvalidOperationException(SR.GetString(SR.DesignerHostDesignerNeedsComponent));
                    } 
                }
                catch {
                    _designers.Remove(component);
                    throw; 
                }
 
                // Designers can also implement IExtenderProvider. 
                //
                if (designer is IExtenderProvider) { 
                    IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                    if (eps != null) {
                        eps.AddExtenderProvider((IExtenderProvider)designer);
                    } 
                }
            } 
 
            // The component has been added.  Note that it is tempting to move this above the
            // designer because the designer will never need to know that its own component just 
            // got added, but this would be bad because the designer is needed to extract
            // shadowed properties from the component.
            //
            ComponentEventArgs ce = new ComponentEventArgs(component); 
            ComponentEventHandler eh = _events[EventComponentAdded] as ComponentEventHandler;
            if (eh != null) { 
                eh(containerToAddTo, ce); 
            }
        } 

        /// <devdoc>
        ///     Called by DesignerSurface to begin loading the designer.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        internal void BeginLoad(DesignerLoader loader) { 
 
            if (_loader != null && _loader != loader) {
                Exception ex = new InvalidOperationException(SR.GetString(SR.DesignerHostLoaderSpecified)); 
                ex.HelpLink = SR.DesignerHostLoaderSpecified;
                throw ex;
            }
 
            IDesignerEventService des = null;
            bool reloading = (_loader != null); 
            _loader = loader; 

            if (!reloading) { 
                if (loader is IExtenderProvider) {
                    IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                    if (eps != null) {
                        eps.AddExtenderProvider((IExtenderProvider)loader); 
                    }
                } 
 
                des = GetService(typeof(IDesignerEventService)) as IDesignerEventService;
                if (des != null) { 
                    des.ActiveDesignerChanged += new ActiveDesignerEventHandler(this.OnActiveDesignerChanged);
                    _designerEventService = des;
                }
            } 

            _state[StateLoading] = true; 
            _surface.OnLoading(); 

            try { 
                _loader.BeginLoad(this);
            }
            catch (Exception e) {
 
                if (e is TargetInvocationException) {
                    e = e.InnerException; 
                } 

                string message = e.Message; 

                // We must handle the case of an exception with no message.
                //
                if (message == null || message.Length == 0) { 
                    e = new Exception(SR.GetString(SR.DesignSurfaceFatalError, e.ToString()), e);
                } 
 
                // Loader blew up.  Add this exception to our error list.
                // 
                ((IDesignerLoaderHost)this).EndLoad(null, false, new object[] {e});
            }

            if (_designerEventService == null) { 
                // If there is no designer event service, make this designer the currently
                // active designer.  It will remain active. 
                OnActiveDesignerChanged(null, new ActiveDesignerEventArgs(null, this)); 
            }
        } 

        /// <summary>
        ///     Override of CreateSite.  We create a custom site here, called Site,
        ///     which is an inner class of DesignerHost.  DesignerSite contains an instance 
        ///     of the designer for the component.
        /// </summary> 
        /// <param name="component"> 
        ///     The component to create the site for
        /// </param> 
        /// <param name="name">
        ///     The name of the component.  If no name is provided this
        ///     will fabricate a name for you.
        /// </param> 
        /// <returns>
        ///     The newly created site 
        /// </returns> 
        protected override ISite CreateSite(IComponent component, string name) {
            Debug.Assert(component != null, "Caller should have guarded against a null component"); 

            // We need to handle the case where a component's ctor adds itself
            // to the container.  We don't want to do the work of creating a name,
            // and then immediately renaming.  So, DesignerHost's CreateComponent 
            // will set _newComponentName to the newly created name before
            // creating the component. 
            // 
            if (_newComponentName != null) {
                name = _newComponentName; 
                _newComponentName = null;
            }

            INameCreationService nameCreate = GetService(typeof(INameCreationService)) as INameCreationService; 

            // Fabricate a name if one wasn't provided.  We try to use the name 
            // creation service, but if it is not available we will just use an 
            // empty string.
            // 
            if (name == null) {
                if (nameCreate != null) {
                    name = nameCreate.CreateName(this, TypeDescriptor.GetReflectionType(component));
                } 
                else {
                    name = string.Empty; 
                } 
            }
            else { 
                if (nameCreate != null) {
                    nameCreate.ValidateName(name);
                }
            } 

            return new Site(component, this, name, this); 
        } 

        /// <devdoc> 
        ///     Override of dispose to clean up our state.
        /// </devdoc>
        protected override void Dispose(bool disposing) {
 
            if (disposing) {
                throw new InvalidOperationException(SR.GetString(SR.DesignSurfaceContainerDispose)); 
            } 

            base.Dispose(disposing); 
        }

        /// <devdoc>
        ///     We move all "dispose" functionality to the 
        ///     DisposeHost method.  The reason for this is that
        ///     Dispose is inherited from our container implementation, 
        ///     and we do not want someone disposing the container.  That 
        ///     would leave the design surface still alive, but it would
        ///     kill the host.  Instead, DesignSurface always calls 
        ///     DisposeHost, which calls the base version of Dispose
        ///     to clean out the container.
        /// </devdoc>
        internal void DisposeHost() { 

            try { 
                if (_loader != null) { 
                    _loader.Dispose();
                    Unload(); 
                }

                if (_surface != null) {
 
                    if (_designerEventService != null) {
                        _designerEventService.ActiveDesignerChanged -= new ActiveDesignerEventHandler(this.OnActiveDesignerChanged); 
                    } 

                    DesignSurfaceServiceContainer dsc = GetService(typeof(DesignSurfaceServiceContainer)) as DesignSurfaceServiceContainer; 
                    if (dsc != null) {
                        foreach(Type t in DefaultServices) {
                            dsc.RemoveFixedService(t);
                        } 
                    }
                    else { 
                        IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
                        Debug.Assert(sc != null, "DesignerHost: Ctor needs a service provider that provides IServiceContainer");
                        if (sc != null) { 
                            foreach(Type t in DefaultServices) {
                                sc.RemoveService(t);
                            }
                        } 
                    }
                } 
            } 
            finally {
                _loader = null; 
                _surface = null;
                _events.Dispose();
            }
 
            base.Dispose(true);
        } 
 
        /// <devdoc>
        ///     Invokes flush on the designer loader. 
        /// </devdoc>
        internal void Flush() {
            if (_loader != null) {
                _loader.Flush(); 
            }
        } 
 
        /// <summary>
        ///     Override of Container's GetService method.  This just delegates to the 
        ///     parent service provider.
        /// </summary>
        /// <param name="service">
        ///     The type of service to retrieve 
        /// </param>
        /// <returns> 
        ///     An instance of the service. 
        /// </returns>
        protected override object GetService(Type service) { 
            object serviceInstance = null;

            if (service == null) {
                throw new ArgumentNullException("service"); 
            }
 
            serviceInstance = base.GetService(service); 

            if (serviceInstance == null && _surface != null) { 
                serviceInstance = _surface.GetService(service);
            }

            return serviceInstance; 
        }
 
        /// <devdoc> 
        ///     Called in response to a designer becoming active or inactive.
        /// </devdoc> 
        private void OnActiveDesignerChanged(object sender, ActiveDesignerEventArgs e) {

            // NOTE: sender can be null (we call this directly in BeginLoad)
 
            object eventobj = null;
 
            if (e.OldDesigner == this) { 
                eventobj = EventDeactivated;
            } 
            else if (e.NewDesigner == this) {
                eventobj = EventActivated;
            }
 
            // Not our document, so we don't fire.
            // 
            if (eventobj == null) { 
                return;
            } 


            // If we are deactivating, flush any code changes.
            // We always route through the design surface 
            // so it can correctly raise its Flushed event.
            // 
            Debug.Assert(_surface != null, "calling OnActiveDesignerChanged on a disposed DesignerHost"); 
            if (e.OldDesigner == this && _surface != null) {
                _surface.Flush(); 
            }


 
            // Fire the appropriate event.
            // 
            EventHandler handler = _events[eventobj] as EventHandler; 
            if (handler != null) {
                handler(this, EventArgs.Empty); 
            }
        }

        /// <devdoc> 
        ///     Method is called by the site when a component is renamed.
        /// </devdoc> 
        private void OnComponentRename(IComponent component, string oldName, string newName) { 
            // If the root component is being renamed we need to update RootComponentClassName.
            if(component == _rootComponent) { 
                string className = _rootComponentClassName;
                int oldNameIndex = className.LastIndexOf(oldName);

                if (oldNameIndex + oldName.Length == className.Length                // If oldName occurs at the end of className 
                    && (oldNameIndex - 1 >= 0 && className[oldNameIndex - 1] == '.'))   // and is preceeded by a period
                { 
                    // We assume the preceeding chars are the namespace and preserve it. 
                    _rootComponentClassName = className.Substring(0, oldNameIndex) + newName;
                } 
                else {
                    _rootComponentClassName = newName;
                }
            } 

            ComponentRenameEventHandler eh = _events[EventComponentRename] as ComponentRenameEventHandler; 
            if (eh != null) { 
                eh(this, new ComponentRenameEventArgs(component, oldName, newName));
            } 
        }

        /// <devdoc>
        ///     Method is called when the designer has finished loading. 
        /// </devdoc>
        private void OnLoadComplete(EventArgs e) { 
            EventHandler eh = _events[EventLoadComplete] as EventHandler; 
            if (eh != null) eh(this, e);
        } 

        /// <devdoc>
        ///     Method is called when the last transaction has closed.
        /// </devdoc> 
        private void OnTransactionClosed(DesignerTransactionCloseEventArgs e) {
            DesignerTransactionCloseEventHandler eh = _events[EventTransactionClosed] as DesignerTransactionCloseEventHandler; 
            if (eh != null) eh(this, e); 
        }
 
        /// <devdoc>
        ///     Method is called when the last transaction is closing.
        /// </devdoc>
        private void OnTransactionClosing(DesignerTransactionCloseEventArgs e) { 
            DesignerTransactionCloseEventHandler eh = _events[EventTransactionClosing] as DesignerTransactionCloseEventHandler;
            if (eh != null) eh(this, e); 
        } 

        /// <devdoc> 
        ///     Method is called when the first transaction has opened.
        /// </devdoc>
        private void OnTransactionOpened(EventArgs e) {
            EventHandler eh = _events[EventTransactionOpened] as EventHandler; 
            if (eh != null) eh(this, e);
        } 
 
        /// <devdoc>
        ///     Method is called when the first transaction is opening. 
        /// </devdoc>
        private void OnTransactionOpening(EventArgs e) {
            EventHandler eh = _events[EventTransactionOpening] as EventHandler;
            if (eh != null) eh(this, e); 
        }
 
        /// <devdoc> 
        ///     Called to remove a component from its container.
        /// </devdoc> 
        public override void Remove(IComponent component) {
            if (RemoveFromContainerPreProcess(component, this)) {
                Site site = component.Site as Site;
                Debug.Assert(site != null, "RemoveFromContainerPreProcess should have returned false for this."); 
                RemoveWithoutUnsiting(component);
                site.Disposed = true; 
                RemoveFromContainerPostProcess(component, this); 
            }
        } 

        internal bool RemoveFromContainerPreProcess(IComponent component, IContainer container) {
            if (component == null) {
                throw new ArgumentNullException("component"); 
            }
 
            ISite site = component.Site; 
            if (site == null || site.Container != container) {
                return false; 
            }

            ComponentEventHandler eh;
            ComponentEventArgs ce = new ComponentEventArgs(component); 

            /* 
 

 



 

 
 

 
*/

            eh = _events[EventComponentRemoving] as ComponentEventHandler;
            if (eh != null) { 
                eh(this, ce);
            } 
 
            // If the component is an extender provider, remove it from
            // the extender provider service, should one exist. 
            //
            if (component is IExtenderProvider) {
                IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                if (eps != null) { 
                    eps.RemoveExtenderProvider((IExtenderProvider)component);
                } 
            } 

            // Same for the component's designer 
            //
            IDesigner designer = _designers[component] as IDesigner;

            if (designer is IExtenderProvider) { 
                IExtenderProviderService eps = GetService(typeof(IExtenderProviderService)) as IExtenderProviderService;
                if (eps != null) { 
                    eps.RemoveExtenderProvider((IExtenderProvider)designer); 
                }
            } 

            if (designer != null) {
                designer.Dispose();
                _designers.Remove(component); 
            }
 
            if (component == _rootComponent) { 
                _rootComponent = null;
                _rootComponentClassName = null; 
            }

            return true;
        } 

        internal void RemoveFromContainerPostProcess(IComponent component, IContainer container) { 
 
            // VSWhidbey 464535
            // At one point during Whidbey, the component used to be unsited earlier in this process 
            // and it would be temporarily resited here before raising OnComponentRemoved. The problem with resiting
            // it is that some 3rd party controls take action when a component is sited (such as displaying
            // a dialog a control is dropped on the form) and resiting here caused them to think they were
            // being initialized for the first time.  To preserve compat, we shouldn't resite the component 
            // during Remove.
 
            try { 
                ComponentEventHandler eh = _events[EventComponentRemoved] as ComponentEventHandler;
                ComponentEventArgs ce = new ComponentEventArgs(component); 
                if (eh != null) {
                    eh(this, ce);
                }
            } 
            finally {
                component.Site = null; 
            } 
        }
 
        /// <devdoc>
        ///     Called to unload the design surface.
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private void Unload() {
 
            _surface.OnUnloading(); 

            IHelpService helpService = GetService(typeof(IHelpService)) as IHelpService; 
            if (helpService != null && _rootComponent != null && _designers[_rootComponent] != null) {
                helpService.RemoveContextAttribute("Keyword", "Designer_" + _designers[_rootComponent].GetType().FullName);
            }
 
            ISelectionService selectionService = (ISelectionService)GetService(typeof(ISelectionService));
            if (selectionService != null) { 
                selectionService.SetSelectedComponents(null, SelectionTypes.Replace); 
            }
 
            // Now remove all the designers and their components.  We save the root
            // for last.  Note that we eat any exceptions that components or their
            // designers generate.  A bad component or designer should not prevent
            // an unload from happening.  We do all of this in a transaction to help 
            // reduce the number of events we generate.
            // 
            _state[StateUnloading] = true; 
            DesignerTransaction t = ((IDesignerHost)this).CreateTransaction();
            ArrayList exceptions = new ArrayList(); 

            try {
                IComponent[] components = new IComponent[Components.Count];
                Components.CopyTo(components, 0); 

                foreach(IComponent comp in components) { 
                    if (!object.ReferenceEquals(comp,_rootComponent)) { 
                        IDesigner designer = _designers[comp] as IDesigner;
                        if (designer != null) { 
                            _designers.Remove(comp);
                            try {designer.Dispose();}
                            catch (Exception e){
                                string failedComponent = designer != null ? designer.GetType().Name : string.Empty; 
                                Debug.Fail( string.Format(CultureInfo.CurrentCulture, "Designer threw during unload: {0}", failedComponent));
                                exceptions.Add(e); 
                            } 

                        } 
                        try {comp.Dispose();}
                        catch (Exception e) {
                            string failedComponent = comp != null ? comp.GetType().Name : string.Empty;
                            Debug.Fail( string.Format(CultureInfo.CurrentCulture, "Component threw during unload: {0}", failedComponent)); 
                            exceptions.Add(e);
                        } 
                    } 
                }
 
                if (_rootComponent != null) {
                    IDesigner designer = _designers[_rootComponent] as IDesigner;
                    if (designer != null) {
                        _designers.Remove(_rootComponent); 
                        try {designer.Dispose();}
                        catch (Exception e) { 
                            string failedComponent = designer != null ? designer.GetType().Name : string.Empty; 
                            Debug.Fail( string.Format(CultureInfo.CurrentCulture, "Designer threw during unload: {0}", failedComponent));
                            exceptions.Add(e); 
                        }
                    }
                    try {_rootComponent.Dispose();}
                    catch (Exception e) { 
                        string failedComponent = _rootComponent != null ? _rootComponent.GetType().Name : string.Empty;
                        Debug.Fail( string.Format(CultureInfo.CurrentCulture, "Component threw during unload: {0}", failedComponent)); 
                        exceptions.Add(e); 
                    }
                } 

                _designers.Clear();
                while(Components.Count > 0) Remove(Components[0]);
            } 
            finally {
                t.Commit(); 
                _state[StateUnloading] = false; 
            }
 
            // There should be no open transactions.  Commit all of the ones that are
            // open.
            //
            if (_transactions != null && _transactions.Count > 0) { 
                Debug.Fail("There are open transactions at unload");
                while(_transactions.Count > 0) { 
                    DesignerTransaction trans = (DesignerTransaction)_transactions.Peek(); // it'll get pop'ed in the OnCommit for DesignerHostTransaction 
                    #if DEBUG
                    Debug.Fail(string.Format(CultureInfo.CurrentCulture, "Stack of {0}:\r\n{1}", trans.Description, ((DesignerHostTransaction)trans).CreatorStack)); 
                    #endif
                    trans.Commit();
                }
            } 

            _surface.OnUnloaded(); 
 
            if (exceptions.Count > 0) {
                throw new ExceptionCollection(exceptions); 
            }
        }

        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentAdded event.
        /// </devdoc> 
        event ComponentEventHandler IComponentChangeService.ComponentAdded { 
            add {
                _events.AddHandler(EventComponentAdded, value); 
            }
            remove {
                _events.RemoveHandler(EventComponentAdded, value);
            } 
        }
 
        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentAdding event.
        /// </devdoc> 
        event ComponentEventHandler IComponentChangeService.ComponentAdding {
            add {
                _events.AddHandler(EventComponentAdding, value);
            } 
            remove {
                _events.RemoveHandler(EventComponentAdding, value); 
            } 
        }
 
        /// <devdoc>
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentChanged event.
        /// </devdoc>
        event ComponentChangedEventHandler IComponentChangeService.ComponentChanged { 
            add {
                _events.AddHandler(EventComponentChanged, value); 
            } 
            remove {
                _events.RemoveHandler(EventComponentChanged, value); 
            }
        }

        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentChanging event.
        /// </devdoc> 
        event ComponentChangingEventHandler IComponentChangeService.ComponentChanging { 
            add {
                _events.AddHandler(EventComponentChanging, value); 
            }
            remove {
                _events.RemoveHandler(EventComponentChanging, value);
            } 
        }
 
        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRemoved event.
        /// </devdoc> 
        event ComponentEventHandler IComponentChangeService.ComponentRemoved {
            add {
                _events.AddHandler(EventComponentRemoved, value);
            } 
            remove {
                _events.RemoveHandler(EventComponentRemoved, value); 
            } 
        }
 
        /// <devdoc>
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRemoving event.
        /// </devdoc>
        event ComponentEventHandler IComponentChangeService.ComponentRemoving { 
            add {
                _events.AddHandler(EventComponentRemoving, value); 
            } 
            remove {
                _events.RemoveHandler(EventComponentRemoving, value); 
            }
        }

        /// <devdoc> 
        ///     Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRename event.
        /// </devdoc> 
        event ComponentRenameEventHandler IComponentChangeService.ComponentRename { 
            add {
                _events.AddHandler(EventComponentRename, value); 
            }
            remove {
                _events.RemoveHandler(EventComponentRename, value);
            } 
        }
 
        /// <devdoc> 
        ///     Announces to the component change service that a particular component has changed.
        /// </devdoc> 
        void IComponentChangeService.OnComponentChanged(object component, MemberDescriptor member, object oldValue, object newValue) {

            if (!((IDesignerHost)this).Loading) {
                ComponentChangedEventHandler eh = _events[EventComponentChanged] as ComponentChangedEventHandler; 
                if (eh != null) {
                    eh(this, new ComponentChangedEventArgs(component, member, oldValue, newValue)); 
                } 
            }
        } 

        /// <devdoc>
        ///     Announces to the component change service that a particular component is changing.
        /// </devdoc> 
        void IComponentChangeService.OnComponentChanging(object component, MemberDescriptor member) {
            if (!((IDesignerHost)this).Loading) { 
                ComponentChangingEventHandler eh = _events[EventComponentChanging] as ComponentChangingEventHandler; 
                if (eh != null) {
                    eh(this, new ComponentChangingEventArgs(component, member)); 
                }
            }
        }
 
        /// <devdoc>
        ///     Gets or sets a value indicating whether the designer host 
        ///     is currently loading the document. 
        /// </devdoc>
        bool IDesignerHost.Loading { 
            get {
                return _state[StateLoading] || _state[StateUnloading] || (_loader != null && _loader.Loading);
            }
        } 

        /// <devdoc> 
        ///     Gets a value indicating whether the designer host is currently in a transaction. 
        /// </devdoc>
        bool IDesignerHost.InTransaction { 
            get {
                return (_transactions != null && _transactions.Count > 0) || IsClosingTransaction;
            }
        } 

        /// <devdoc> 
        ///     Gets the container for this designer host. 
        /// </devdoc>
        IContainer IDesignerHost.Container { 
            get {
                return this;
            }
        } 

        /// <devdoc> 
        ///     Gets the instance of the base class used as the base class for the current design. 
        /// </devdoc>
        IComponent IDesignerHost.RootComponent { 
            get {
                return _rootComponent;
            }
        } 

        /// <devdoc> 
        ///     Gets the fully qualified name of the class that is being designed. 
        /// </devdoc>
        string IDesignerHost.RootComponentClassName { 
            get {
                return _rootComponentClassName;
            }
        } 

        /// <devdoc> 
        ///     Gets the description of the current transaction. 
        /// </devdoc>
        string IDesignerHost.TransactionDescription { 
            get {
                if (_transactions != null && _transactions.Count > 0) {
                    return ((DesignerTransaction)_transactions.Peek()).Description;
                } 
                return null;
            } 
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Activated'/> event.
        /// </devdoc>
        event EventHandler IDesignerHost.Activated {
            add { 
                _events.AddHandler(EventActivated, value);
            } 
            remove { 
                _events.RemoveHandler(EventActivated, value);
            } 
        }

        /// <devdoc>
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Deactivated'/> event. 
        /// </devdoc>
        event EventHandler IDesignerHost.Deactivated { 
            add { 
                _events.AddHandler(EventDeactivated, value);
            } 
            remove {
                _events.RemoveHandler(EventDeactivated, value);
            }
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.LoadComplete'/> event. 
        /// </devdoc>
        event EventHandler IDesignerHost.LoadComplete { 
            add {
                _events.AddHandler(EventLoadComplete, value);
            }
            remove { 
                _events.RemoveHandler(EventLoadComplete, value);
            } 
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosed'/> event.
        /// </devdoc>
        event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosed {
            add { 
                _events.AddHandler(EventTransactionClosed, value);
            } 
            remove { 
                _events.RemoveHandler(EventTransactionClosed, value);
            } 
        }

        /// <devdoc>
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosing'/> event. 
        /// </devdoc>
        event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosing { 
            add { 
                _events.AddHandler(EventTransactionClosing, value);
            } 
            remove {
                _events.RemoveHandler(EventTransactionClosing, value);
            }
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpened'/> event. 
        /// </devdoc>
        event EventHandler IDesignerHost.TransactionOpened { 
            add {
                _events.AddHandler(EventTransactionOpened, value);
            }
            remove { 
                _events.RemoveHandler(EventTransactionOpened, value);
            } 
        } 

        /// <devdoc> 
        ///     Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpening'/> event.
        /// </devdoc>
        event EventHandler IDesignerHost.TransactionOpening {
            add { 
                _events.AddHandler(EventTransactionOpening, value);
            } 
            remove { 
                _events.RemoveHandler(EventTransactionOpening, value);
            } 
        }

        /// <devdoc>
        ///     Activates the designer that this host is hosting. 
        /// </devdoc>
        void IDesignerHost.Activate() { 
            _surface.OnViewActivate(); 
        }
 
        /// <devdoc>
        ///     Creates a component of the specified class type.
        /// </devdoc>
        IComponent IDesignerHost.CreateComponent(Type componentType) { 
            return ((IDesignerHost)this).CreateComponent(componentType, null);
        } 
 
        /// <devdoc>
        ///     Creates a component of the given class type and name and places it into the designer container. 
        /// </devdoc>
        IComponent IDesignerHost.CreateComponent(Type componentType, string name) {
            if (componentType == null) {
                throw new ArgumentNullException("componentType"); 
            }
 
            IComponent component; 

            LicenseContext oldContext = LicenseManager.CurrentContext; 
            bool changingContext = false; // we don't want if there is a recursivity (creating a component create another one)
                                            // to change the context again. we already have the one we want and that would create
                                            // a locking problem. see bug VSWhidbey 441200
            if(oldContext != LicenseContext) { 
                LicenseManager.CurrentContext = LicenseContext;
                LicenseManager.LockContext(_selfLock); 
                changingContext = true; 
            }
 
            try {
                try {
                    _newComponentName = name;
                    component = _surface.CreateInstance(componentType) as IComponent; 
                }
                finally { 
                    _newComponentName = null; 
                }
 
                if (component == null) {
                    InvalidOperationException ex = new InvalidOperationException(SR.GetString(SR.DesignerHostFailedComponentCreate, componentType.Name));
                    ex.HelpLink = SR.DesignerHostFailedComponentCreate;
                    throw ex; 
                }
 
                // Add this component to our container 
                //
                if (component.Site == null || component.Site.Container != this) { 
                    Add(component, name);
                }
            }
            finally { 
                if(changingContext) {
                    LicenseManager.UnlockContext(_selfLock); 
                    LicenseManager.CurrentContext = oldContext; 
                }
            } 

            return component;
        }
 
        /// <devdoc>
        ///     Lengthy operations that involve multiple components may raise many events.  These events 
        ///     may cause other side-effects, such as flicker or performance degradation.  When operating 
        ///     on multiple components at one time, or setting multiple properties on a single component,
        ///     you should encompass these changes inside a transaction.  Transactions are used 
        ///     to improve performance and reduce flicker.  Slow operations can listen to
        ///     transaction events and only do work when the transaction completes.
        /// </devdoc>
        DesignerTransaction IDesignerHost.CreateTransaction() { 
            return ((IDesignerHost)this).CreateTransaction(null);
        } 
 
        /// <devdoc>
        ///     Lengthy operations that involve multiple components may raise many events.  These events 
        ///     may cause other side-effects, such as flicker or performance degradation.  When operating
        ///     on multiple components at one time, or setting multiple properties on a single component,
        ///     you should encompass these changes inside a transaction.  Transactions are used
        ///     to improve performance and reduce flicker.  Slow operations can listen to 
        ///     transaction events and only do work when the transaction completes.
        /// </devdoc> 
        DesignerTransaction IDesignerHost.CreateTransaction(string description) { 
            if (description == null) {
                description = SR.GetString(SR.DesignerHostGenericTransactionName); 
            }

            return new DesignerHostTransaction(this, description);
        } 

        /// <devdoc> 
        ///     Destroys the given component, removing it from the design container. 
        /// </devdoc>
        void IDesignerHost.DestroyComponent(IComponent component) { 
            string name;

            if (component == null) {
                throw new ArgumentNullException("component"); 
            }
 
            if (component.Site != null && component.Site.Name != null) { 
                name = component.Site.Name;
            } 
            else {
                name = component.GetType().Name;
            }
 
            // Make sure the component is not being inherited -- we can't delete these!
            // 
            // 
            InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(component)[typeof(InheritanceAttribute)];
            if (ia != null && ia.InheritanceLevel != InheritanceLevel.NotInherited) { 
                Exception ex = new InvalidOperationException(SR.GetString(SR.DesignerHostCantDestroyInheritedComponent, name));
                ex.HelpLink = SR.DesignerHostCantDestroyInheritedComponent;
                throw ex;
            } 

            if (((IDesignerHost)this).InTransaction) 
            { 
                 Remove(component);		
                 component.Dispose(); 
            }
            else
            {
 
                DesignerTransaction t;
                using (t = ((IDesignerHost)this).CreateTransaction(SR.GetString(SR.DesignerHostDestroyComponentTransaction, name))) { 
 
                    // We need to signal changing and then perform the remove.  Remove must be done by us and not
                    // by Dispose because (a) people need a chance to cancel through a Removing event, and (b) 
                    // Dispose removes from the container last and anything that would sync Removed would end up
                    // with a dead component.
                    //
                    // 
                    Remove(component);
                    // 
    				 
                    component.Dispose();
                    t.Commit(); 
                }
            }
        }
 
        /// <devdoc>
        ///     Gets the designer instance for the specified component. 
        /// </devdoc> 
        IDesigner IDesignerHost.GetDesigner(IComponent component) {
            if (component == null) { 
                throw new ArgumentNullException("component");
            }
            return _designers[component] as IDesigner;
        } 

        /// <devdoc> 
        ///     Gets the type instance for the specified fully qualified type name <paramref name="TypeName"/>. 
        /// </devdoc>
        Type IDesignerHost.GetType(string typeName) { 

            if (typeName == null) {
                throw new ArgumentNullException("typeName");
            } 

            ITypeResolutionService ts = GetService(typeof(ITypeResolutionService)) as ITypeResolutionService; 
            if (ts != null) { 
                return ts.GetType(typeName);
            } 
            return Type.GetType(typeName);
        }

        /// <devdoc> 
        ///     This is called by the designer loader to indicate that the load has
        ///     terminated.  If there were errors, they should be passed in the errorCollection 
        ///     as a collection of exceptions (if they are not exceptions the designer 
        ///     loader host may just call ToString on them).  If the load was successful then
        ///     errorCollection should either be null or contain an empty collection. 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        void IDesignerLoaderHost.EndLoad(string rootClassName, bool successful, ICollection errorCollection) {
 
            bool wasLoading = _state[StateLoading];
            _state[StateLoading] = false; 
 
            if (rootClassName != null) {
                _rootComponentClassName = rootClassName; 
            }
            else if (_rootComponent != null && _rootComponent.Site != null) {
                _rootComponentClassName = _rootComponent.Site.Name;
            } 

            // If the loader indicated success, but it never created a component, that is 
            // an error. 
            //
            if (successful && _rootComponent == null) { 
                ArrayList errorList = new ArrayList();
                InvalidOperationException ex = new InvalidOperationException(SR.GetString(SR.DesignerHostNoBaseClass));
                ex.HelpLink = SR.DesignerHostNoBaseClass;
                errorList.Add(ex); 
                errorCollection = errorList;
                successful = false; 
            } 

            // If we failed, unload the doc so that the OnLoaded event 
            // can't get to anything that actually did work.
            //
            if (!successful) {
                Unload(); 
            }
 
            if (wasLoading && _surface != null) { 
                _surface.OnLoaded(successful, errorCollection);
            } 

            if (successful) {

                // We may be invoked to do an EndLoad when we are already loaded.  This can happen 
                // if the user called AddLoadDependency, essentially putting us in a loading state
                // while we are already loaded.  This is OK, and is used as a hint that the user 
                // is going to monkey with settings but doesn't want the code engine to report 
                // it.
                // 
                if (wasLoading) {

                    IRootDesigner rootDesigner = ((IDesignerHost)this).GetDesigner(_rootComponent) as IRootDesigner;
 
                    // Offer up our base help attribute
                    // 
                    IHelpService helpService = GetService(typeof(IHelpService)) as IHelpService; 
                    if (helpService != null) {
                        helpService.AddContextAttribute("Keyword", "Designer_" + rootDesigner.GetType().FullName, HelpKeywordType.F1Keyword); 
                    }

                    // and let everyone know that we're loaded
                    // 
                    try {
                        OnLoadComplete(EventArgs.Empty); 
                    } 
                    catch (Exception ex) {
 
                        Debug.Fail("Exception thrown on LoadComplete event handler.  You should not throw here : " + ex.ToString());

                        // The load complete failed.  Put us back in the loading state and unload.
                        // 
                        _state[StateLoading] = true;
                        Unload(); 
 
                        ArrayList errorList = new ArrayList();
                        errorList.Add(ex); 
                        if (errorCollection != null) {
                            errorList.AddRange(errorCollection);
                        }
                        errorCollection = errorList; 
                        successful = false;
 
                        if (_surface != null) { 
                            _surface.OnLoaded(successful, errorCollection);
                        } 

                        // We re-throw.  If this was a synchronous load this will
                        // error back to BeginLoad (and, as a side effect, may call
                        // us again).  For asynchronous loads we need to throw so the 
                        // caller knows what happened.
                        // 
                        throw; 
                    }
 
                    // If we saved a selection as a result of a reload, try to replace it.
                    //
                    if (successful && _savedSelection != null) {
                        ISelectionService ss = GetService(typeof(ISelectionService)) as ISelectionService; 
                        if (ss != null) {
                            ArrayList selectedComponents = new ArrayList(_savedSelection.Count); 
                            foreach(string name in _savedSelection) { 
                                IComponent comp = Components[name];
                                if (comp != null) { 
                                    selectedComponents.Add(comp);
                                }
                            }
 
                            _savedSelection = null;
                            ss.SetSelectedComponents(selectedComponents, SelectionTypes.Replace); 
                        } 
                    }
                } 
            }
        }

        /// <devdoc> 
        ///     This is called by the designer loader when it wishes to reload the
        ///     design document.  The reload will happen immediately so the caller 
        ///     should ensure that it is in a state where BeginLoad may be called again. 
        /// </devdoc>
        void IDesignerLoaderHost.Reload() { 
            if (_loader != null) {

                // Flush the loader to make sure there aren't any pending
                // changes.  We always route through the design surface 
                // so it can correctly raise its Flushed event.
                // 
                _surface.Flush(); 

                // Next, stash off the set of selected objects by name.  After 
                // the reload we will attempt to re-select them.
                //
                ISelectionService ss = GetService(typeof(ISelectionService)) as ISelectionService;
                if (ss != null) { 
                    ArrayList list = new ArrayList(ss.SelectionCount);
                    foreach(object o in ss.GetSelectedComponents()) { 
                        IComponent comp = o as IComponent; 
                        if (comp != null && comp.Site != null && comp.Site.Name != null) {
                            list.Add(comp.Site.Name); 
                        }
                    }
                    _savedSelection = list;
                } 

                Unload(); 
                BeginLoad(_loader); 
            }
        } 

        bool IDesignerLoaderHost2.IgnoreErrorsDuringReload {
            get {
                return _ignoreErrorsDuringReload; 
            }
 
            set { 
                // Only allow to set to true if we CanReloadWithErrors
                if (!value || ((IDesignerLoaderHost2)this).CanReloadWithErrors) { 
                    _ignoreErrorsDuringReload = value;
                }
            }
        } 

        bool IDesignerLoaderHost2.CanReloadWithErrors { 
            get { 
                return _canReloadWithErrors;
            } 

            set {
                _canReloadWithErrors = value;
            } 
        }
 
 
        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc>
        MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) {
            return typeof(IDesignerHost).GetMethod(name, bindingAttr, binder, types, modifiers); 
        }
 
        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private. 
        /// </devdoc>
        MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetMethod(name, bindingAttr);
        } 

        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc> 
        MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetMethods(bindingAttr);
        }
 
        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private. 
        /// </devdoc>
        FieldInfo IReflect.GetField(string name, BindingFlags bindingAttr) { 
            return typeof(IDesignerHost).GetField(name, bindingAttr);
        }

        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private. 
        /// </devdoc> 
        FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetFields(bindingAttr); 
        }

        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc> 
        PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr) { 
            return typeof(IDesignerHost).GetProperty(name, bindingAttr);
        } 

        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private. 
        /// </devdoc>
        PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) { 
            return typeof(IDesignerHost).GetProperty(name, bindingAttr, binder, returnType, types, modifiers); 
        }
 
        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private.
        /// </devdoc> 
        PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetProperties(bindingAttr); 
        } 

        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private.
        /// </devdoc>
        MemberInfo[] IReflect.GetMember(string name, BindingFlags bindingAttr) { 
            return typeof(IDesignerHost).GetMember(name, bindingAttr);
        } 
 
        /// <devdoc>
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc>
        MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr) {
            return typeof(IDesignerHost).GetMembers(bindingAttr); 
        }
 
        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps
        ///     keep us private. 
        /// </devdoc>
        object IReflect.InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) {
            return typeof(IDesignerHost).InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        } 

        /// <devdoc> 
        ///     IReflect implementation to map DesignerHost to IDesignerHost.  This helps 
        ///     keep us private.
        /// </devdoc> 
        Type IReflect.UnderlyingSystemType {
            get {
                return typeof(IDesignerHost).UnderlyingSystemType;
            } 
        }
 
        /// <devdoc> 
        ///     Adds the given service to the service container.
        /// </devdoc> 
        void IServiceContainer.AddService(Type serviceType, object serviceInstance) {

            // Our service container is implemented on the parenting DesignSurface
            // object, so we just ask for its service container and run with it. 
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
            if (sc == null) { 
                throw new ObjectDisposedException("IServiceContainer");
            } 
            sc.AddService(serviceType, serviceInstance);
        }

        /// <devdoc> 
        ///     Adds the given service to the service container.
        /// </devdoc> 
        void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) { 

            // Our service container is implemented on the parenting DesignSurface 
            // object, so we just ask for its service container and run with it.
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer;
            if (sc == null) { 
                throw new ObjectDisposedException("IServiceContainer");
            } 
            sc.AddService(serviceType, serviceInstance, promote); 
        }
 
        /// <devdoc>
        ///     Adds the given service to the service container.
        /// </devdoc>
        void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) { 

            // Our service container is implemented on the parenting DesignSurface 
            // object, so we just ask for its service container and run with it. 
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
            if (sc == null) {
                throw new ObjectDisposedException("IServiceContainer");
            }
            sc.AddService(serviceType, callback); 
        }
 
        /// <devdoc> 
        ///     Adds the given service to the service container.
        /// </devdoc> 
        void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) {

            // Our service container is implemented on the parenting DesignSurface
            // object, so we just ask for its service container and run with it. 
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
            if (sc == null) { 
                throw new ObjectDisposedException("IServiceContainer");
            } 
            sc.AddService(serviceType, callback, promote);
        }

        /// <devdoc> 
        ///     Removes the given service type from the service container.
        /// </devdoc> 
        void IServiceContainer.RemoveService(Type serviceType) { 

            // Our service container is implemented on the parenting DesignSurface 
            // object, so we just ask for its service container and run with it.
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer;
            if (sc == null) { 
                throw new ObjectDisposedException("IServiceContainer");
            } 
            sc.RemoveService(serviceType); 
        }
 
        /// <devdoc>
        ///     Removes the given service type from the service container.
        /// </devdoc>
        void IServiceContainer.RemoveService(Type serviceType, bool promote) { 

            // Our service container is implemented on the parenting DesignSurface 
            // object, so we just ask for its service container and run with it. 
            //
            IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
            if (sc == null) {
                throw new ObjectDisposedException("IServiceContainer");
            }
            sc.RemoveService(serviceType, promote); 
        }
 
        /// <devdoc> 
        ///     IServiceProvider implementation.  We just delegate to the
        ///     protected GetService method we are inheriting from our 
        ///     container.
        /// </devdoc>
        object IServiceProvider.GetService(Type serviceType) {
            return GetService(serviceType); 
        }
 
        /// <devdoc> 
        ///     DesignerHostTransaction is our implementation of the
        ///     DesignerTransaction abstract class. 
        /// </devdoc>
        private sealed class DesignerHostTransaction : DesignerTransaction {

            private DesignerHost _host; 

            #if DEBUG 
            private string _creatorStack; 
            #endif
 
            public DesignerHostTransaction(DesignerHost host, string description) : base(description) {
                _host = host;
                if (_host._transactions == null) {
                    _host._transactions = new Stack(); 
                }
 
                _host._transactions.Push(this); 
                _host.OnTransactionOpening(EventArgs.Empty);
                _host.OnTransactionOpened(EventArgs.Empty); 

                #if DEBUG
                _creatorStack = Environment.StackTrace;
                #endif 
            }
 
            #if DEBUG 
            /// <devdoc>
            ///     Debug info that displays the stack of the creation call of this 
            ///     transaction.  This is useful for tracking down orphaned transactions.
            /// </devdoc>
            public string CreatorStack {
                get { 
                    return _creatorStack;
                } 
            } 
            #endif
 
            #if DEBUG
            /// <devdoc>
            ///     We override Dispose to handle finalization cases so we
            ///     can display the stack of the transaction creator. 
            /// </devdoc>
            protected override void Dispose(bool disposing) { 
                base.Dispose(disposing); 

                if (!disposing) { 
                    Debug.Fail("Callstack of transaction creator: " + CreatorStack);
                }
            }
            #endif 

            /// <devdoc> 
            ///     User code should implement this method to perform 
            ///     the actual work of committing a transaction.
            /// </devdoc> 
            protected override void OnCancel() {
                if (_host != null) {
                    if (_host._transactions.Peek() != this) {
                        string nestedDescription = ((DesignerTransaction)_host._transactions.Peek()).Description; 
                        throw new InvalidOperationException(SR.GetString(SR.DesignerHostNestedTransaction, Description, nestedDescription));
                    } 
 
                    _host.IsClosingTransaction = true;
                    try { 
                        _host._transactions.Pop();
                        DesignerTransactionCloseEventArgs e = new DesignerTransactionCloseEventArgs(false, _host._transactions.Count == 0);
                        _host.OnTransactionClosing(e);
                        _host.OnTransactionClosed(e); 
                    } finally {
                        _host.IsClosingTransaction = false; 
                        _host = null; 
                    }
                } 
            }

            /// <devdoc>
            ///     User code should implement this method to perform 
            ///     the actual work of committing a transaction.
            /// </devdoc> 
            protected override void OnCommit() { 
                if (_host != null) {
                    if (_host._transactions.Peek() != this) { 
                        string nestedDescription = ((DesignerTransaction)_host._transactions.Peek()).Description;
                        throw new InvalidOperationException(SR.GetString(SR.DesignerHostNestedTransaction, Description, nestedDescription));
                    }
 
                    _host.IsClosingTransaction = true;
                    try { 
                        _host._transactions.Pop(); 
                        DesignerTransactionCloseEventArgs e = new DesignerTransactionCloseEventArgs(true, _host._transactions.Count == 0);
                        _host.OnTransactionClosing(e); 
                        _host.OnTransactionClosed(e);
                    } finally {
                        _host.IsClosingTransaction = false;
                        _host = null; 
                    }
                } 
            } 
        }
 
        /// <summary>
        ///     Site is the site we use at design time when we host
        ///     components.
        /// </summary> 
        internal class Site : ISite, IServiceContainer, IDictionaryService {
 
            private IComponent              _component; 
            private Hashtable               _dictionary;
            private DesignerHost            _host; 
            private string                  _name;
            private bool                    _disposed;
            private SiteNestedContainer     _nestedContainer;
            private Container               _container; 

            internal Site(IComponent component, DesignerHost host, string name, Container container) { 
                _component = component; 
                _host = host;
                _name = name; 
                _container = container;
            }

            /// <devdoc> 
            ///     Used by the IServiceContainer implementation to return a container-specific
            ///     service container. 
            /// </devdoc> 
            private IServiceContainer SiteServiceContainer {
                get { 
                    SiteNestedContainer nc = ((IServiceProvider)this).GetService(typeof(INestedContainer)) as SiteNestedContainer;
                    Debug.Assert(nc != null, "We failed to resolve a nested container.");
                    IServiceContainer sc = nc.GetServiceInternal(typeof(IServiceContainer)) as IServiceContainer;
                    Debug.Assert(sc != null, "We failed to resolve a service container from the nested container."); 
                    return sc;
                } 
            } 

            /// <devdoc> 
            ///     Retrieves the key corresponding to the given value.
            /// </devdoc>
            object IDictionaryService.GetKey(object value) {
                if (_dictionary != null) { 
                    foreach(DictionaryEntry de in _dictionary) {
                        object o = de.Value; 
                        if (value != null && value.Equals(o)) { 
                            return de.Key;
                        } 
                    }
                }
                return null;
            } 

            /// <devdoc> 
            ///     Retrieves the value corresponding to the given key. 
            /// </devdoc>
            object IDictionaryService.GetValue(object key) { 
                if (_dictionary != null) {
                    return _dictionary[key];
                }
                return null; 
            }
 
            /// <devdoc> 
            ///     Stores the given key-value pair in an object's site.  This key-value
            ///     pair is stored on a per-object basis, and is a handy place to save 
            ///     additional information about a component.
            /// </devdoc>
            void IDictionaryService.SetValue(object key, object value) {
                if (_dictionary == null) { 
                    _dictionary = new Hashtable();
                } 
                if (value == null) { 
                    _dictionary.Remove(key);
                } 
                else {
                    _dictionary[key] = value;
                }
            } 

            /// <devdoc> 
            ///     Adds the given service to the service container. 
            /// </devdoc>
            void IServiceContainer.AddService(Type serviceType, object serviceInstance) { 
                SiteServiceContainer.AddService(serviceType, serviceInstance);
            }

            /// <devdoc> 
            ///     Adds the given service to the service container.
            /// </devdoc> 
            void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) { 
                SiteServiceContainer.AddService(serviceType, serviceInstance, promote);
            } 

            /// <devdoc>
            ///     Adds the given service to the service container.
            /// </devdoc> 
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) {
                SiteServiceContainer.AddService(serviceType, callback); 
            } 

            /// <devdoc> 
            ///     Adds the given service to the service container.
            /// </devdoc>
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) {
                SiteServiceContainer.AddService(serviceType, callback, promote); 
            }
 
            /// <devdoc> 
            ///     Removes the given service type from the service container.
            /// </devdoc> 
            void IServiceContainer.RemoveService(Type serviceType) {
                SiteServiceContainer.RemoveService(serviceType);
            }
 
            /// <devdoc>
            ///     Removes the given service type from the service container. 
            /// </devdoc> 
            void IServiceContainer.RemoveService(Type serviceType, bool promote) {
                SiteServiceContainer.RemoveService(serviceType, promote); 
            }

            /// <summary>
            ///     Returns the requested service. 
            /// </summary>
            /// <param name="service"></param> 
            /// <returns></returns> 
            object IServiceProvider.GetService(Type service) {
                if (service == null) { 
                    throw new ArgumentNullException("service");
                }

                // We always resolve IDictionaryService to ourselves. 

                if (service == typeof(IDictionaryService)) { 
                    return this; 
                }
 
                // NestedContainer is demand created

                if (service == typeof(INestedContainer)) {
                    if (_nestedContainer == null) { 
                        _nestedContainer = new SiteNestedContainer(_component, null, _host);
                    } 
                    return _nestedContainer; 
                }
 
                // SiteNestedContainer does offer IServiceContainer and IContainer as services, but we
                // always want a default site query for these services to delegate to the host.
                // Because it is more common to add  services to the host than it is to add them to the site itself, and
                // also because we need this for backward compatibility. 

                if (service != typeof(IServiceContainer) && service != typeof(IContainer) && _nestedContainer != null) { 
                    return _nestedContainer.GetServiceInternal(service); 
                }
 
                return _host.GetService(service);
            }

            /// <summary> 
            ///     The component sited by this component site.
            /// </summary> 
            IComponent ISite.Component { 
                get {
                    return _component; 
                }
            }

            /// <summary> 
            ///     The container in which the component is sited.
            /// </summary> 
            IContainer ISite.Container { 
                get {
                    return _container; 
                }
            }

            /// <summary> 
            ///     Indicates whether the component is in design mode.
            /// </summary> 
            bool ISite.DesignMode { 
                get {
                    return true; 
                }
            }

            /// <summary> 
            ///     Indicates whether this Site has been disposed.
            /// </summary> 
            internal bool Disposed { 
                get {
                    return _disposed; 
                }
                set {
                    this._disposed = value;
                } 
            }
 
            /// <summary> 
            ///     The name of the component.
            /// </summary> 
            string ISite.Name {
                get {
                    return _name;
                } 
                set {
                    if (value == null) { 
                        value = string.Empty; 
                    }
 
                    if (_name != value) {
                        bool validateName = true;

                        if (value.Length > 0) { 
                            IComponent namedComponent = _container.Components[value];
 
                            validateName = (_component != namedComponent); 

                            // allow renames that are just case changes of the current name. 
                            //
                            if (namedComponent != null && validateName) {
                                Exception ex = new Exception(SR.GetString(SR.DesignerHostDuplicateName, value));
 
                                ex.HelpLink = SR.DesignerHostDuplicateName;
                                throw ex; 
                            } 
                        }
 
                        if (validateName) {
                            INameCreationService nameService = (INameCreationService)((IServiceProvider)this).GetService(typeof(INameCreationService));

                            if (nameService != null) { 
                                nameService.ValidateName(value);
                            } 
                        } 

                        // It is OK to change the name to this value.  Announce the change 
                        // and do it.
                        //
                        string oldName = _name;
                        _name = value; 
                        _host.OnComponentRename(_component, oldName, _name);
                    } 
                } 
            }
        } 
    }


    /// <summary> 
    ///     This is a nested container.  Anything added to the nested container will
    ///     be hostable in a designer. 
    /// </summary> 
    internal sealed class SiteNestedContainer: NestedContainer {
        private DesignerHost _host; 
        private IServiceContainer _services;
        private string _containerName;
        private bool _safeToCallOwner;
 
        internal SiteNestedContainer(IComponent owner, string containerName, DesignerHost host) : base(owner) {
            _containerName = containerName; 
            _host = host; 
            _safeToCallOwner = true;
        } 

        /// <devdoc>
        ///     Override to support named containers.
        /// </devdoc> 
        protected override string OwnerName {
            get { 
                string ownerName = base.OwnerName; 
                if (_containerName != null && _containerName.Length > 0) {
                    ownerName = string.Format(CultureInfo.CurrentCulture, "{0}.{1}", ownerName, _containerName); 
                }

                return ownerName;
            } 
        }
 
        /// <devdoc> 
        ///     Called to add a component to its container.
        /// </devdoc> 
        public override void Add(IComponent component, string name) {

            if (_host.AddToContainerPreProcess(component, name, this)) {
 
                // Site creation fabricates a name for this component.
                // 
                base.Add(component, name); 

                try { 
                    _host.AddToContainerPostProcess(component, name, this);
                }
                catch (Exception t) {
                    if (t != CheckoutException.Canceled) { 
                        Remove(component);
                    } 
                    throw; 
                }
                catch { 
                    Remove(component);
                    throw;
                }
            } 
        }
 
        /// <include file='doc\SiteNestedContainer.uex' path='docs/doc[@for="SiteNestedContainer.CreateSite"]/*' /> 
        /// <devdoc>
        ///     Creates a site for the component within the container. 
        /// </devdoc>
        protected override ISite CreateSite(IComponent component, string name) {
            if (component == null) {
                throw new ArgumentNullException("component"); 
            }
            return new NestedSite(component, _host, name, this); 
        } 

        /// <devdoc> 
        ///     Called to remove a component from its container.
        /// </devdoc>
        public override void Remove(IComponent component) {
            if (_host.RemoveFromContainerPreProcess(component, this)) { 
                ISite site = component.Site;
                Debug.Assert(site != null, "RemoveFromContainerPreProcess should have returned false for this."); 
                RemoveWithoutUnsiting(component); 
                _host.RemoveFromContainerPostProcess(component, this);
            } 
        }


        protected override object GetService(Type serviceType) { 
            object service = base.GetService(serviceType);
 
            if (service != null) { 
                return service;
            } 


            if (serviceType == typeof(IServiceContainer)) {
                if (_services == null) { 
                    _services = new ServiceContainer(_host);
                } 
 
                return _services;
            } 


            if (_services != null) {
                return _services.GetService(serviceType); 
            }
            else { 
                if (Owner.Site != null && _safeToCallOwner) { 
                    try {
                        _safeToCallOwner = false; 
                        return Owner.Site.GetService(serviceType);
                    }
                    finally {
                        _safeToCallOwner = true; 
                    }
                } 
            } 

            return null; 
        }

        internal object GetServiceInternal(Type serviceType) {
            return GetService(serviceType); 
        }
 
        private sealed class NestedSite : DesignerHost.Site, INestedSite 
        {
            private SiteNestedContainer _container; 
            private string           _name;

            internal NestedSite(IComponent component, DesignerHost host, string name, Container container)
            : base(component, host, name, container) 
            {
                 _container = container as SiteNestedContainer; 
                 _name = name; 
            }
 
            public string FullName {
                get {
                    if (_name != null) {
                        string ownerName = _container.OwnerName; 
                        string childName = ((ISite)this).Name;
                        if (ownerName != null) { 
                            childName = string.Format(CultureInfo.CurrentCulture, "{0}.{1}", ownerName, childName); 
                        }
 
                        return childName;
                    }

                    return _name; 
                }
            } 
        } 

    } 

}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
