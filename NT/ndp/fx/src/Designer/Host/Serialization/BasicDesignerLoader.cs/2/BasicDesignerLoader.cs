//------------------------------------------------------------------------------ 
// <copyright file="BasicDesignerLoader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Design; 
    using System.Diagnostics;
    using System.Reflection; 
    using System.Text; 
    using System.Windows.Forms;
 
    /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader"]/*' />
    /// <devdoc>
    ///     This is a class that derives from DesignerLoader but provides some default functionality.
    ///     This class tracks changes from the loader host and sets its "Modified" bit to true when a 
    ///     change occurs.  Also, this class implements IDesignerLoaderService to support multiple
    ///     load dependencies.  To use BaseDesignerLoader, you need to implement the PerformLoad 
    ///     and PerformFlush methods. 
    /// </devdoc>
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public abstract class BasicDesignerLoader :

        DesignerLoader, 
        IDesignerLoaderService {
 
        // Flags that we use 
        //
        private static readonly int StateLoaded                   = BitVector32.CreateMask();                       // Have we loaded, or tried to load, the document? 
        private static readonly int StateLoadFailed               = BitVector32.CreateMask(StateLoaded);            // True if we loaded, but had a fatal error.
        private static readonly int StateFlushInProgress          = BitVector32.CreateMask(StateLoadFailed);        // True if we are in the process of flushing code.
        private static readonly int StateModified                 = BitVector32.CreateMask(StateFlushInProgress);   // True if the designer is modified.
        private static readonly int StateReloadSupported          = BitVector32.CreateMask(StateModified);          // True if the serializer supports reload. 
        private static readonly int StateActiveDocument           = BitVector32.CreateMask(StateReloadSupported);   // Is this the currently active document?
        private static readonly int StateDeferredReload           = BitVector32.CreateMask(StateActiveDocument);    // Set to true if a reload was requested but we aren't the active doc. 
        private static readonly int StateReloadAtIdle             = BitVector32.CreateMask(StateDeferredReload);    // Set if we are waiting to reload at idle.  Prevents multiple idle event handlers. 
        private static readonly int StateForceReload              = BitVector32.CreateMask(StateReloadAtIdle);      // True if we should always reload, False if we should check the code dom for changes first.
        private static readonly int StateFlushReload              = BitVector32.CreateMask(StateForceReload);       // True if we should flush before reloading. 
        private static readonly int StateModifyIfErrors           = BitVector32.CreateMask(StateFlushReload);       // True if we we should modify the buffer if we have fatal errors after load.
        private static readonly int StateEnableComponentEvents    = BitVector32.CreateMask(StateModifyIfErrors);    // True if we are currently listening to OnComponent* events

        // State for the designer loader. 
        //
        private BitVector32                     _state = new BitVector32(); 
        private IDesignerLoaderHost             _host; 
        private int                             _loadDependencyCount;
        private string                          _baseComponentClassName; 
        private bool                            _hostInitialized;
        private bool                            _loading;

        // State for serialization. 
        //
        private DesignerSerializationManager    _serializationManager; 
        private IDisposable                     _serializationSession; 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.BasicDesignerLoader"]/*' /> 
        /// <devdoc>
        ///     Creates a new BasicDesignerLoader
        /// </devdoc>
        protected BasicDesignerLoader() { 

            _state[StateFlushInProgress] = false; 
            _state[StateReloadSupported] = true; 
            _state[StateEnableComponentEvents] = false;
            _hostInitialized = false; 
            _loading = false;
        }

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Modified"]/*' /> 
        /// <devdoc>
        ///     This protected property indicates if there have been any 
        ///     changes made to the design surface.  The Flush method 
        ///     gets the value of this property to determine if it needs
        ///     to generate a code dom tree.  This property is set by 
        ///     the designer loader when it detects a change to the
        ///     design surface.  You can override this to perform
        ///     additional work, such as checking out a file from source
        ///     code control. 
        /// </devdoc>
        protected virtual bool Modified { 
            get { 
                return _state[StateModified];
            } 
            set {
                _state[StateModified] = value;
            }
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.LoaderHost"]/*' /> 
        /// <devdoc> 
        ///     Retruns the loader host that was given to this designer loader.  This can be null if BeginLoad has not
        ///     been called yet, or if this designer loader has been disposed. 
        /// </devdoc>
        protected IDesignerLoaderHost LoaderHost {
            get {
                if (_host == null) { 
                    if (_hostInitialized) {
                        throw new ObjectDisposedException(this.GetType().Name); 
                    } 
                    else {
                        throw new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderNotInitialized)); 
                    }
                }
                return _host;
            } 
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Loading"]/*' /> 
        /// <devdoc>
        ///     Returns true when the designer is in the process of loading. 
        ///     Clients that are sinking notifications from the designer often
        ///     want to ignore them while the desingner is loading
        ///     and only respond to them if they result from user interatcions.
        /// </devdoc> 
        public override bool Loading {
            get { 
                return _loadDependencyCount > 0 || _loading; 
            }
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.PropertyProvider"]/*' />
        /// <devdoc>
        ///     Provides an object whose public properties will be made available to the designer serialization manager's 
        ///     Properties property.  The default value of this property is null.
        /// </devdoc> 
        protected object PropertyProvider { 
            get {
                if (_serializationManager == null) { 
                    throw new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderNotInitialized));
                }
                return _serializationManager.PropertyProvider;
            } 
            set {
                if (_serializationManager == null) { 
                    throw new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderNotInitialized)); 
                }
                _serializationManager.PropertyProvider = value; 
            }
        }

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.ReloadSupported"]/*' /> 
        /// <devdoc>
        ///     Calling Reload doesn't actually perform a reload immediately - it just schedules an asynchronous 
        ///     reload. This property is used to determine if there is currently a reload pending. 
        /// </devdoc>
        protected bool ReloadPending { 
            get {
                return _state[StateReloadAtIdle];
            }
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.BeginLoad"]/*' /> 
        /// <devdoc> 
        ///     Called by the designer host to begin the loading process.
        ///     The designer host passes in an instance of a designer loader 
        ///     host.  This loader host allows the designer loader to reload
        ///     the design document and also allows the designer loader to indicate
        ///     that it has finished loading the design document.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public override void BeginLoad(IDesignerLoaderHost host) { 
 
            if (host == null) {
                throw new ArgumentNullException("host"); 
            }

            if (_state[StateLoaded]) {
                Exception ex = new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderAlreadyLoaded)); 
                ex.HelpLink = SR.BasicDesignerLoaderAlreadyLoaded;
                throw ex; 
            } 

            if (_host != null && _host != host) { 
                Exception ex = new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderDifferentHost));
                ex.HelpLink = SR.BasicDesignerLoaderDifferentHost;
                throw ex;
            } 

            _state[StateLoaded | StateLoadFailed] = false; 
            _loadDependencyCount = 0; 

            if (_host == null) { 

                _host = host;
                _hostInitialized = true;
                _serializationManager = new DesignerSerializationManager(_host); 

                // Add our services.  We do IDesignerSerializationManager separate because 
                // it is not something the user can replace. 
                DesignSurfaceServiceContainer dsc = GetService(typeof(DesignSurfaceServiceContainer)) as DesignSurfaceServiceContainer;
                if (dsc != null) { 
                    dsc.AddFixedService(typeof(IDesignerSerializationManager), _serializationManager);
                }
                else {
                    IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
                    if (sc == null) {
                        ThrowMissingService(typeof(IServiceContainer)); 
                    } 
                    sc.AddService(typeof(IDesignerSerializationManager), _serializationManager);
                } 

                Initialize();

                host.Activated += new EventHandler(this.OnDesignerActivate); 
                host.Deactivated += new EventHandler(this.OnDesignerDeactivate);
            } 
 
            // Now that we're initialized, let's begin the load.  We assume
            // we support reload until the codeLoader tells us we 
            // can't.  That way, we will do the reload if we didn't get a
            // valid loader to start with.
            //
            // StartTimingMark(); 
            bool successful = true;
            ArrayList localErrorList = null; 
            IDesignerLoaderService ls = GetService(typeof(IDesignerLoaderService)) as IDesignerLoaderService; 

            try { 

                if (ls != null) {
                    ls.AddLoadDependency();
                } 
                else {
                    _loading = true; 
                    OnBeginLoad(); 
                }
 
                PerformLoad(_serializationManager);

            }
            catch (Exception e) { 

                while (e is TargetInvocationException) { 
                    e = e.InnerException; 
                }
 
                localErrorList = new ArrayList();
                localErrorList.Add(e);
                successful = false;
            } 

            if (ls != null) { 
                ls.DependentLoadComplete(successful, localErrorList); 
            }
            else { 
                OnEndLoad(successful, localErrorList);
                _loading = false;
            }
 
            // EndTimingMark("Full Load");
        } 
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes this designer loader.  The designer host will call
        ///     this method when the design document itself is being destroyed.
        ///     Once called, the designer loader will never be called again.
        ///     This implementation removes any previously added services.  It 
        ///     does not flush changes, which allows for fast teardown of a
        ///     designer that wasn't saved. 
        /// </devdoc> 
        public override void Dispose() {
 
            if (_state[StateReloadAtIdle]) {
                Application.Idle -= new EventHandler(this.OnIdle);
            }
 
            UnloadDocument();
 
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
            if (cs != null) {
                cs.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded); 
                cs.ComponentAdding -= new ComponentEventHandler(this.OnComponentAdding);
                cs.ComponentRemoving -= new ComponentEventHandler(this.OnComponentRemoving);
                cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved);
                cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                cs.ComponentChanging -= new ComponentChangingEventHandler(this.OnComponentChanging);
                cs.ComponentRename -= new ComponentRenameEventHandler(OnComponentRename); 
            } 

            if (_host != null) { 
                _host.RemoveService(typeof(IDesignerLoaderService));
                _host.Activated -= new EventHandler(this.OnDesignerActivate);
                _host.Deactivated -= new EventHandler(this.OnDesignerDeactivate);
                _host = null; 
            }
        } 
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Flush"]/*' />
        /// <devdoc> 
        ///     The designer host will call this periodically when it wants to
        ///     ensure that any changes that have been made to the document
        ///     have been saved by the designer loader.  This method allows
        ///     designer loaders to implement a lazy-write scheme to improve 
        ///     performance.  This designer loader implements lazy writes by
        ///     listening to component change events.  If a component has 
        ///     changed it sets a "modified" bit.  When Flush is called the 
        ///     loader will write out a new code dom tree.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public override void Flush() {

 
            if (_state[StateFlushInProgress] || !_state[StateLoaded] || !Modified) {
                return; 
            } 
            _state[StateFlushInProgress] = true;
 
            Cursor oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try { 
                IDesignerLoaderHost host = _host;
                Debug.Assert(host != null, "designer loader was asked to flush after it has been disposed."); 
 
                // If the host has a null root component, it probably failed
                // its last load.  In that case, there is nothing to flush. 
                //
                bool shouldChangeModified = true;
                if (host != null && host.RootComponent != null) {
                    using (_serializationManager.CreateSession()) { 
                        try {
                            PerformFlush(_serializationManager); 
                        } 
                        catch (CheckoutException) {
                            shouldChangeModified = false; // don't need to report that one it already has shown an error message 
                            throw;
                        }
                        catch (Exception ex) {
                            _serializationManager.Errors.Add(ex); 
                        }
 
                        ICollection errors = _serializationManager.Errors; 
                        if (errors != null && errors.Count > 0) {
                            ReportFlushErrors(errors); 
                        }
                    }
                }
                if(shouldChangeModified) { 
                    Modified = false;
                } 
            } 
            finally {
                _state[StateFlushInProgress] = false; 
                Cursor.Current = oldCursor;
            }
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.GetService"]/*' />
        /// <devdoc> 
        ///     Helper method that gives access to the service provider. 
        /// </devdoc>
        protected object GetService(Type serviceType) { 
            object service = null;

            if (_host != null) {
                service = _host.GetService(serviceType); 
            }
 
            return service; 
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Initialize"]/*' />
        /// <devdoc>
        ///     This method is called immediately after the first time
        ///     BeginLoad is invoked.  This is an appopriate place to 
        ///     add custom services to the loader host.  Remember to
        ///     remove any custom services you add here by overriding 
        ///     Dispose. 
        /// </devdoc>
        protected virtual void Initialize() { 
            LoaderHost.AddService(typeof(IDesignerLoaderService), this);
        }

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.IsReloadNeeded"]/*' /> 
        /// <devdoc>
        ///     This method an be overridden to provide some intelligent 
        ///     logic to determine if a reload is required.  This method is 
        ///     called when someone requests a reload but doesn't force
        ///     the reload.  It gives the loader an opportunity to scan 
        ///     the underlying storage to determine if a reload is acutually
        ///     needed.  The default implementation of this method always
        ///     returns true.
        /// </devdoc> 
        protected virtual bool IsReloadNeeded() {
            return true; 
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.OnBeginLoad"]/*' /> 
        /// <devdoc>
        ///     This method should be called by the designer loader service
        ///     when the first dependent load has started.  This initializes
        ///     the state of the code dom loader and prepares it for loading. 
        ///     By default, the designer loader provides
        ///     IDesignerLoaderService itself, so this is called automatically. 
        ///     If you provide your own loader service, or if you choose not 
        ///     to provide a loader service, you are responsible for calling
        ///     this method.  BeginLoad will automatically call this, either 
        ///     indirectly by calling AddLoadDependency if IDesignerLoaderService
        ///     is available, or directly if it is not.
        /// </devdoc>
        protected virtual void OnBeginLoad() { 

            _serializationSession = _serializationManager.CreateSession(); 
            _state[StateLoaded] = false; 

            // Make sure that we're removed any event sinks we added after we finished the load. 
            // Make sure that we're removed any event sinks we added after we finished the load.
            EnableComponentNotification(false);
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (cs != null) { 
                cs.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded);
                cs.ComponentAdding -= new ComponentEventHandler(this.OnComponentAdding); 
                cs.ComponentRemoving -= new ComponentEventHandler(this.OnComponentRemoving); 
                cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved);
                cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                cs.ComponentChanging -= new ComponentChangingEventHandler(this.OnComponentChanging);
                cs.ComponentRename -= new ComponentRenameEventHandler(OnComponentRename);
            }
 
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.EnableComponentNotification"]/*' /> 
        /// <devdoc>
        /// This method can be used to Enable or Disable component notification by the DesignerLoader. 
        /// </devdoc>
        protected virtual bool EnableComponentNotification(bool enable) {
            bool previouslyEnabled = _state[StateEnableComponentEvents];
 
            if (!previouslyEnabled  && enable) {
                _state[StateEnableComponentEvents] = true; 
            } 
            else if (previouslyEnabled && !enable) {
                _state[StateEnableComponentEvents] = false; 
            }

            return previouslyEnabled;
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.OnBeginUnload"]/*' /> 
        /// <devdoc> 
        ///     This method is called immediately before the document is unloaded.
        ///     The document may be unloaded in preparation for reload, or 
        ///     if the document failed the load.  If you added document-specific
        ///     services in OnBeginLoad or OnEndLoad, you should remove them
        ///     here.
        /// </devdoc> 
        protected virtual void OnBeginUnload() {
        } 
 
        /// <devdoc>
        ///     This is called whenever a new component is added to the design surface. 
        /// </devdoc>
        private void OnComponentAdded(object sender, ComponentEventArgs e) {

            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might 
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) Modified = true;
        } 

        /// <devdoc>
        ///     This is called right before a component is added to the design surface.
        /// </devdoc> 
        private void OnComponentAdding(object sender, ComponentEventArgs e) {
 
            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might 
            // be listening when asked to unload.
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) OnModifying();
        }
 
        /// <devdoc>
        ///     This is called whenever a component on the design surface changes. 
        /// </devdoc> 
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) {
 
            // We check the loader host here.  We do not actually listen to
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) Modified = true;
        } 
 
        /// <devdoc>
        ///     This is called right before a component on the design surface changes. 
        /// </devdoc>
        private void OnComponentChanging(object sender, ComponentChangingEventArgs e) {

            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might 
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) OnModifying();
        } 

        /// <devdoc>
        ///     This is called whenever a component is removed from the design surface.
        /// </devdoc> 
        private void OnComponentRemoved(object sender, ComponentEventArgs e) {
 
            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might 
            // be listening when asked to unload.
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) Modified = true;
        }
 
        /// <devdoc>
        ///     This is called right before a component is removed from the design surface. 
        /// </devdoc> 
        private void OnComponentRemoving(object sender, ComponentEventArgs e) {
 
            // We check the loader host here.  We do not actually listen to
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) OnModifying();
        } 
 
        /// <devdoc>
        ///     Raised by the host when a component is renamed.  Here we modify ourselves 
        ///     and then whack the component declaration.  At the next code gen
        ///     cycle we will recreate the declaration.
        /// </devdoc>
        private void OnComponentRename(object sender, ComponentRenameEventArgs e) { 

            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we 
            // succeeded the load and the loader then failed later, we might
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) {
                OnModifying();
                Modified = true;
            } 
        }
 
        /// <devdoc> 
        ///     Called when this document becomes active.  here we check to see if
        ///     someone else has modified the contents of our buffer.  If so, we 
        ///     ask the designer to reload.
        /// </devdoc>
        private void OnDesignerActivate(object sender, EventArgs e) {
            _state[StateActiveDocument] = true; 

            if (_state[StateDeferredReload] && _host != null) { 
                _state[StateDeferredReload] = false; 
                ReloadOptions flags = ReloadOptions.Default;
 
                if (_state[StateForceReload]) flags |= ReloadOptions.Force;
                if (!_state[StateFlushReload]) flags |= ReloadOptions.NoFlush;
                if (_state[StateModifyIfErrors]) flags |= ReloadOptions.ModifyOnError;
 
                Reload(flags);
            } 
        } 

        /// <devdoc> 
        ///     Called when this document loses activation.  We just remember this
        ///     for later.
        /// </devdoc>
        private void OnDesignerDeactivate(object sender, EventArgs e) { 
            _state[StateActiveDocument] = false;
        } 
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.OnEndLoad"]/*' />
        /// <devdoc> 
        ///     This method should be called by the designer loader service
        ///     when all dependent loads have been completed.  This
        ///     "shuts down" the loading process that was initiated by
        ///     BeginLoad.  By default, the designer loader provides 
        ///     IDesignerLoaderService itself, so this is called automatically.
        ///     If you provide your own loader service, or if you choose not 
        ///     to provide a loader service, you are responsible for calling 
        ///     this method.  BeginLoad will automatically call this, either
        ///     indirectly by calling DependentLoadComplete if IDesignerLoaderService 
        ///     is available, or directly if it is not.
        /// </devdoc>
        protected virtual void OnEndLoad(bool successful, ICollection errors) {
            //we don't want successful to be true here if there were load errors. 
            //this may allow a situation where we have a dirtied WSOD and might allow
            //a user to save a partially loaded designer docdata. 
            successful = successful 
                                && (errors == null || errors.Count == 0)
                                && (_serializationManager.Errors == null 
                                    || _serializationManager.Errors.Count == 0);
            try {
                _state[StateLoaded] = true;
 
                IDesignerLoaderHost2 lh2 = GetService(typeof(IDesignerLoaderHost2)) as IDesignerLoaderHost2;
 
                if (!successful && (lh2 == null || !lh2.IgnoreErrorsDuringReload)) { 
                    // Can we even show the Continue Ignore errors in DTEL?
                    if (lh2 != null) { 
                        lh2.CanReloadWithErrors = LoaderHost.RootComponent != null;
                    }
                    UnloadDocument();
                } 
                else {
                    successful = true; 
                } 

 
                // Inform the serialization manager that we are all done.  The serialization
                // manager clears state at this point to help enforce a stateless serialization
                // mechanism.
                // 
                if (errors != null) {
                    foreach(object err in errors) { 
                        _serializationManager.Errors.Add(err); 
                    }
                } 
                errors = _serializationManager.Errors;
            }
            finally {
                _serializationSession.Dispose(); 
                _serializationSession = null;
            } 
 
            if (successful) {
 
                // After a successful load we will want to monitor a bunch of events so we know when
                // to make the loader modified.
                //
 
                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                if (cs != null) { 
                    cs.ComponentAdded += new ComponentEventHandler(this.OnComponentAdded); 
                    cs.ComponentAdding += new ComponentEventHandler(this.OnComponentAdding);
                    cs.ComponentRemoving += new ComponentEventHandler(this.OnComponentRemoving); 
                    cs.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved);
                    cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
                    cs.ComponentChanging += new ComponentChangingEventHandler(this.OnComponentChanging);
                    cs.ComponentRename += new ComponentRenameEventHandler(OnComponentRename); 
                }
                EnableComponentNotification(true); 
            } 

            LoaderHost.EndLoad(_baseComponentClassName, successful, errors); 

            // if we got errors in the load, set ourselves as modified so we'll regen code.  If this fails, we don't
            // care; the Modified bit was only a hint.
            // 
            if (_state[StateModifyIfErrors] && errors != null && errors.Count > 0) {
                try { 
                    OnModifying(); 
                    Modified = true;
                } 
                catch (CheckoutException ex) {
                    if (ex != CheckoutException.Canceled) {
                        throw;
                    } 
                }
            } 
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.OnModifying"]/*' /> 
        /// <devdoc>
        ///     This method is called in response to a component changing, adding or removing event to indicate
        ///     that the designer is about to be modified.  Those interested in implementing source code
        ///     control may do so by overriding this method.  A call to OnModifying does not mean that the 
        ///     Modified property will later be set to true; it is merly an intention to do so.
        /// </devdoc> 
        protected virtual void OnModifying() { 
        }
 
        /// <devdoc>
        ///     Invoked by the loader host when it actually performs the reload, but before
        ///     the reload actually happens.  Here we unload our part of the loader
        ///     and get us ready for the pending reload. 
        /// </devdoc>
        private void OnIdle(object sender, EventArgs e) { 
            Application.Idle -= new EventHandler(this.OnIdle); 
            if (_state[StateReloadAtIdle]) {
                _state[StateReloadAtIdle] = false; 

                //check to see if we are actually the active document.
                DesignSurfaceManager mgr = (DesignSurfaceManager)GetService(typeof(DesignSurfaceManager));
                DesignSurface thisSurface = (DesignSurface)GetService(typeof(DesignSurface)); 
                Debug.Assert(mgr != null && thisSurface != null);
                if (mgr != null && thisSurface != null) { 
                    if (!object.ReferenceEquals(mgr.ActiveDesignSurface, thisSurface)) { 
                        //somehow, we got deactivated and weren't told.
                        _state[StateActiveDocument] = false; 
                        _state[StateDeferredReload] = true; //reload on activate
                        return;
                    }
                } 

                IDesignerLoaderHost host = LoaderHost; 
                if(host != null) { 
                    if (_state[StateForceReload] || IsReloadNeeded()) {
 
                        try {
                            if (_state[StateFlushReload]) {
                                Flush();
                            } 

                            UnloadDocument(); 
                            host.Reload(); 
                        }
                        finally { 
                            _state[StateForceReload | StateModifyIfErrors | StateFlushReload] = false;
                        }
                    }
                } 
            }
        } 
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.PerformFlush"]/*' />
        /// <devdoc> 
        ///     This method is called when it is time to flush the
        ///     contents of the loader.  You should save any state
        ///     at this time.
        /// </devdoc> 
        protected abstract void PerformFlush(IDesignerSerializationManager serializationManager);
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.PerformLoad"]/*' /> 
        /// <devdoc>
        ///     This method is called when it is time to load the 
        ///     design surface.  If you are loading asynchronously
        ///     you should ask for IDesignerLoaderService and call
        ///     AddLoadDependency.  When loading asynchronously you
        ///     should at least create the root component during 
        ///     PerformLoad.  The DesignSurface is only able to provide
        ///     a view when there is a root component. 
        /// </devdoc> 
        protected abstract void PerformLoad(IDesignerSerializationManager serializationManager);
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Reload"]/*' />
        /// <devdoc>
        ///     This method schedules a reload of the designer.
        ///     Designer reloading happens asynchronously in order 
        ///     to unwind the stack before the reload begins.  If
        ///     force is true, a reload is always performed.  If 
        ///     it is false, a reload is only performed if the 
        ///     underlying code dom tree has changed in a way that
        ///     would affect the form. 
        ///     If flush is true, the designer is flushed before performing
        ///     a reload.  If false, any designer changes are abandonded.
        ///     If ModifyOnError is true, the designer loader will be put
        ///     in the modified state if any errors happened during the 
        ///     load.
        /// </devdoc> 
        protected void Reload(ReloadOptions flags) { 

            _state[StateForceReload] = ((flags & ReloadOptions.Force) != 0); 
            _state[StateFlushReload] = ((flags & ReloadOptions.NoFlush) == 0);
            _state[StateModifyIfErrors] = ((flags & ReloadOptions.ModifyOnError) != 0);

            // Our implementation of Reload only reloads if we are the 
            // active designer.  Otherwise, we wait until we become
            // active and reload at that time.  We also never do a 
            // reload if we are flushing code. 
            //
            if (!_state[StateFlushInProgress]) { 
                if (_state[StateActiveDocument]) {
                    if (!_state[StateReloadAtIdle]) {
                        Application.Idle += new EventHandler(this.OnIdle);
                        _state[StateReloadAtIdle] = true; 
                    }
                } 
                else { 
                    _state[StateDeferredReload] = true;
                } 
            }
        }

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.ReportFlushErrors"]/*' /> 
        /// <devdoc>
        ///     This method is called during flush if one or more errors occurred while 
        ///     flushing changes.  The values in the errors collection may either be 
        ///     exceptions or objects whose ToString value describes the error.  The default
        ///     implementation of this method takes last exception in the collection and 
        ///     raises it as an exception.
        /// </devdoc>
        protected virtual void ReportFlushErrors(ICollection errors) {
            object lastError = null; 
            foreach(object e in errors) {
                lastError = e; 
            } 

            Debug.Assert(lastError != null, "Someone embedded a null in the error collection"); 

            if (lastError != null) {
                Exception ex = lastError as Exception;
                if (ex == null) { 
                    ex = new InvalidOperationException(lastError.ToString());
                } 
                throw ex; 
            }
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.SetBaseComponentClassName"]/*' />
        /// <devdoc>
        ///     This property provides the name the designer surface 
        ///     will use for the base class.  Normally this is a fully
        ///     qualified name such as "Project1.Form1".  You should set 
        ///     this before finishing the load.  Generally this is set 
        ///     during PerformLoad.
        /// </devdoc> 
        protected void SetBaseComponentClassName(string name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            } 
            _baseComponentClassName = name;
        } 
 
        /// <devdoc>
        ///     Simple helper routine that will throw an exception if we need a service, but cannot get 
        ///     to it.  You should only throw for missing services that are absolutely essential for
        ///     operation.  If there is a way to gracefully degrade, then you should do it.
        /// </devdoc>
        private void ThrowMissingService(Type serviceType) { 
            Exception ex = new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderMissingService, serviceType.Name));
            ex.HelpLink = SR.BasicDesignerLoaderMissingService; 
            throw ex; 
        }
 
        /// <devdoc>
        ///     This method will be called when the document is to be unloaded.  It
        ///     does not dispose us, but it gets us ready for a dispose or a reload.
        /// </devdoc> 
        private void UnloadDocument() {
            OnBeginUnload(); 
            _state[StateLoaded] = false; 
            _baseComponentClassName = null;
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.IDesignerLoaderService.AddLoadDependency"]/*' />
        /// <devdoc>
        ///     Adds a load dependency to this loader.  This indicates that some other 
        ///     object is also participating in the load, and that the designer loader
        ///     should not call EndLoad on the loader host until all load dependencies 
        ///     have called DependentLoadComplete on the designer loader. 
        /// </devdoc>
        void IDesignerLoaderService.AddLoadDependency() { 
            if (_serializationManager == null) {
                throw new InvalidOperationException();
            }
 
            if (_loadDependencyCount++ == 0) {
                OnBeginLoad(); 
            } 
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.IDesignerLoaderService.DependentLoadComplete"]/*' />
        /// <devdoc>
        ///     This is called by any object that has previously called
        ///     AddLoadDependency to signal that the dependent load has completed. 
        ///     The caller should pass either an empty collection or null to indicate
        ///     a successful load, or a collection of exceptions that indicate the 
        ///     reason(s) for failure. 
        /// </devdoc>
        void IDesignerLoaderService.DependentLoadComplete(bool successful, ICollection errorCollection) { 

            if (_loadDependencyCount == 0) {
                throw new InvalidOperationException();
            } 

            // If the dependent load failed, remember it.  There may be multiple 
            // dependent loads.  If any one fails, we're sunk. 
            //
            if (!successful) { 
                _state[StateLoadFailed] = true;
            }

            if (--_loadDependencyCount == 0) { 

                // We have just completed the last dependent load.  Report this. 
                // 
                OnEndLoad(!_state[StateLoadFailed], errorCollection);
            } 
            else {

                // Otherwise, add these errors to the serialization manager.
                // 
                if (errorCollection != null) {
                    foreach(object err in errorCollection) { 
                        _serializationManager.Errors.Add(err); 
                    }
                } 
            }
        }

        /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.IDesignerLoaderService.Reload"]/*' /> 
        /// <devdoc>
        ///     This can be called by an outside object to request that the loader 
        ///     reload the design document.  If it supports reloading and wants to 
        ///     comply with the reload, the designer loader should return true.  Otherwise
        ///     it should return false, indicating that the reload will not occur. 
        ///     Callers should not rely on the reload happening immediately; the
        ///     designer loader may schedule this for some other time, or it may
        ///     try to reload at once.
        /// </devdoc> 
        bool IDesignerLoaderService.Reload() {
 
            if (_state[StateReloadSupported] && _loadDependencyCount == 0) { 
                Reload(ReloadOptions.Force);
                return true; 
            }

            return false;
        } 

        /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions"]/*' /> 
        /// <devdoc> 
        ///     A list of flags that indicate rules to apply when requesting
        ///     that the designer reload itself. 
        /// </devdoc>
        [Flags]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
        protected enum ReloadOptions { 

            /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions.Default"]/*' /> 
            /// <devdoc> 
            ///     Peform the default behavior.
            /// </devdoc> 
            Default  = 0x00,

            /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions.ModifyOnError"]/*' />
            /// <devdoc> 
            ///     If this flag is set, any error encoutered during the
            ///     reload will automatically put the designer loader in 
            ///     the modified state. 
            /// </devdoc>
            ModifyOnError = 0x01, 

            /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions.Force"]/*' />
            /// <devdoc>
            ///     If this flag is set, a reload will occur.  If the 
            ///     flag is not set a reload will only occur if the
            ///     IsReloadNeeded method returns true. 
            /// </devdoc> 
            Force = 0x02,
 
            /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions.NoFlush"]/*' />
            /// <devdoc>
            ///     If this flag is set, any pending changes in the
            ///     designer will be abandonded.  If this flag is not 
            ///     set, designer changes will be flushed through the
            ///     designer loader before reloading the design surface. 
            /// </devdoc> 
            NoFlush = 0x04,
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BasicDesignerLoader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Design; 
    using System.Diagnostics;
    using System.Reflection; 
    using System.Text; 
    using System.Windows.Forms;
 
    /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader"]/*' />
    /// <devdoc>
    ///     This is a class that derives from DesignerLoader but provides some default functionality.
    ///     This class tracks changes from the loader host and sets its "Modified" bit to true when a 
    ///     change occurs.  Also, this class implements IDesignerLoaderService to support multiple
    ///     load dependencies.  To use BaseDesignerLoader, you need to implement the PerformLoad 
    ///     and PerformFlush methods. 
    /// </devdoc>
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public abstract class BasicDesignerLoader :

        DesignerLoader, 
        IDesignerLoaderService {
 
        // Flags that we use 
        //
        private static readonly int StateLoaded                   = BitVector32.CreateMask();                       // Have we loaded, or tried to load, the document? 
        private static readonly int StateLoadFailed               = BitVector32.CreateMask(StateLoaded);            // True if we loaded, but had a fatal error.
        private static readonly int StateFlushInProgress          = BitVector32.CreateMask(StateLoadFailed);        // True if we are in the process of flushing code.
        private static readonly int StateModified                 = BitVector32.CreateMask(StateFlushInProgress);   // True if the designer is modified.
        private static readonly int StateReloadSupported          = BitVector32.CreateMask(StateModified);          // True if the serializer supports reload. 
        private static readonly int StateActiveDocument           = BitVector32.CreateMask(StateReloadSupported);   // Is this the currently active document?
        private static readonly int StateDeferredReload           = BitVector32.CreateMask(StateActiveDocument);    // Set to true if a reload was requested but we aren't the active doc. 
        private static readonly int StateReloadAtIdle             = BitVector32.CreateMask(StateDeferredReload);    // Set if we are waiting to reload at idle.  Prevents multiple idle event handlers. 
        private static readonly int StateForceReload              = BitVector32.CreateMask(StateReloadAtIdle);      // True if we should always reload, False if we should check the code dom for changes first.
        private static readonly int StateFlushReload              = BitVector32.CreateMask(StateForceReload);       // True if we should flush before reloading. 
        private static readonly int StateModifyIfErrors           = BitVector32.CreateMask(StateFlushReload);       // True if we we should modify the buffer if we have fatal errors after load.
        private static readonly int StateEnableComponentEvents    = BitVector32.CreateMask(StateModifyIfErrors);    // True if we are currently listening to OnComponent* events

        // State for the designer loader. 
        //
        private BitVector32                     _state = new BitVector32(); 
        private IDesignerLoaderHost             _host; 
        private int                             _loadDependencyCount;
        private string                          _baseComponentClassName; 
        private bool                            _hostInitialized;
        private bool                            _loading;

        // State for serialization. 
        //
        private DesignerSerializationManager    _serializationManager; 
        private IDisposable                     _serializationSession; 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.BasicDesignerLoader"]/*' /> 
        /// <devdoc>
        ///     Creates a new BasicDesignerLoader
        /// </devdoc>
        protected BasicDesignerLoader() { 

            _state[StateFlushInProgress] = false; 
            _state[StateReloadSupported] = true; 
            _state[StateEnableComponentEvents] = false;
            _hostInitialized = false; 
            _loading = false;
        }

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Modified"]/*' /> 
        /// <devdoc>
        ///     This protected property indicates if there have been any 
        ///     changes made to the design surface.  The Flush method 
        ///     gets the value of this property to determine if it needs
        ///     to generate a code dom tree.  This property is set by 
        ///     the designer loader when it detects a change to the
        ///     design surface.  You can override this to perform
        ///     additional work, such as checking out a file from source
        ///     code control. 
        /// </devdoc>
        protected virtual bool Modified { 
            get { 
                return _state[StateModified];
            } 
            set {
                _state[StateModified] = value;
            }
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.LoaderHost"]/*' /> 
        /// <devdoc> 
        ///     Retruns the loader host that was given to this designer loader.  This can be null if BeginLoad has not
        ///     been called yet, or if this designer loader has been disposed. 
        /// </devdoc>
        protected IDesignerLoaderHost LoaderHost {
            get {
                if (_host == null) { 
                    if (_hostInitialized) {
                        throw new ObjectDisposedException(this.GetType().Name); 
                    } 
                    else {
                        throw new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderNotInitialized)); 
                    }
                }
                return _host;
            } 
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Loading"]/*' /> 
        /// <devdoc>
        ///     Returns true when the designer is in the process of loading. 
        ///     Clients that are sinking notifications from the designer often
        ///     want to ignore them while the desingner is loading
        ///     and only respond to them if they result from user interatcions.
        /// </devdoc> 
        public override bool Loading {
            get { 
                return _loadDependencyCount > 0 || _loading; 
            }
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.PropertyProvider"]/*' />
        /// <devdoc>
        ///     Provides an object whose public properties will be made available to the designer serialization manager's 
        ///     Properties property.  The default value of this property is null.
        /// </devdoc> 
        protected object PropertyProvider { 
            get {
                if (_serializationManager == null) { 
                    throw new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderNotInitialized));
                }
                return _serializationManager.PropertyProvider;
            } 
            set {
                if (_serializationManager == null) { 
                    throw new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderNotInitialized)); 
                }
                _serializationManager.PropertyProvider = value; 
            }
        }

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.ReloadSupported"]/*' /> 
        /// <devdoc>
        ///     Calling Reload doesn't actually perform a reload immediately - it just schedules an asynchronous 
        ///     reload. This property is used to determine if there is currently a reload pending. 
        /// </devdoc>
        protected bool ReloadPending { 
            get {
                return _state[StateReloadAtIdle];
            }
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.BeginLoad"]/*' /> 
        /// <devdoc> 
        ///     Called by the designer host to begin the loading process.
        ///     The designer host passes in an instance of a designer loader 
        ///     host.  This loader host allows the designer loader to reload
        ///     the design document and also allows the designer loader to indicate
        ///     that it has finished loading the design document.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public override void BeginLoad(IDesignerLoaderHost host) { 
 
            if (host == null) {
                throw new ArgumentNullException("host"); 
            }

            if (_state[StateLoaded]) {
                Exception ex = new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderAlreadyLoaded)); 
                ex.HelpLink = SR.BasicDesignerLoaderAlreadyLoaded;
                throw ex; 
            } 

            if (_host != null && _host != host) { 
                Exception ex = new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderDifferentHost));
                ex.HelpLink = SR.BasicDesignerLoaderDifferentHost;
                throw ex;
            } 

            _state[StateLoaded | StateLoadFailed] = false; 
            _loadDependencyCount = 0; 

            if (_host == null) { 

                _host = host;
                _hostInitialized = true;
                _serializationManager = new DesignerSerializationManager(_host); 

                // Add our services.  We do IDesignerSerializationManager separate because 
                // it is not something the user can replace. 
                DesignSurfaceServiceContainer dsc = GetService(typeof(DesignSurfaceServiceContainer)) as DesignSurfaceServiceContainer;
                if (dsc != null) { 
                    dsc.AddFixedService(typeof(IDesignerSerializationManager), _serializationManager);
                }
                else {
                    IServiceContainer sc = GetService(typeof(IServiceContainer)) as IServiceContainer; 
                    if (sc == null) {
                        ThrowMissingService(typeof(IServiceContainer)); 
                    } 
                    sc.AddService(typeof(IDesignerSerializationManager), _serializationManager);
                } 

                Initialize();

                host.Activated += new EventHandler(this.OnDesignerActivate); 
                host.Deactivated += new EventHandler(this.OnDesignerDeactivate);
            } 
 
            // Now that we're initialized, let's begin the load.  We assume
            // we support reload until the codeLoader tells us we 
            // can't.  That way, we will do the reload if we didn't get a
            // valid loader to start with.
            //
            // StartTimingMark(); 
            bool successful = true;
            ArrayList localErrorList = null; 
            IDesignerLoaderService ls = GetService(typeof(IDesignerLoaderService)) as IDesignerLoaderService; 

            try { 

                if (ls != null) {
                    ls.AddLoadDependency();
                } 
                else {
                    _loading = true; 
                    OnBeginLoad(); 
                }
 
                PerformLoad(_serializationManager);

            }
            catch (Exception e) { 

                while (e is TargetInvocationException) { 
                    e = e.InnerException; 
                }
 
                localErrorList = new ArrayList();
                localErrorList.Add(e);
                successful = false;
            } 

            if (ls != null) { 
                ls.DependentLoadComplete(successful, localErrorList); 
            }
            else { 
                OnEndLoad(successful, localErrorList);
                _loading = false;
            }
 
            // EndTimingMark("Full Load");
        } 
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes this designer loader.  The designer host will call
        ///     this method when the design document itself is being destroyed.
        ///     Once called, the designer loader will never be called again.
        ///     This implementation removes any previously added services.  It 
        ///     does not flush changes, which allows for fast teardown of a
        ///     designer that wasn't saved. 
        /// </devdoc> 
        public override void Dispose() {
 
            if (_state[StateReloadAtIdle]) {
                Application.Idle -= new EventHandler(this.OnIdle);
            }
 
            UnloadDocument();
 
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
            if (cs != null) {
                cs.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded); 
                cs.ComponentAdding -= new ComponentEventHandler(this.OnComponentAdding);
                cs.ComponentRemoving -= new ComponentEventHandler(this.OnComponentRemoving);
                cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved);
                cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                cs.ComponentChanging -= new ComponentChangingEventHandler(this.OnComponentChanging);
                cs.ComponentRename -= new ComponentRenameEventHandler(OnComponentRename); 
            } 

            if (_host != null) { 
                _host.RemoveService(typeof(IDesignerLoaderService));
                _host.Activated -= new EventHandler(this.OnDesignerActivate);
                _host.Deactivated -= new EventHandler(this.OnDesignerDeactivate);
                _host = null; 
            }
        } 
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Flush"]/*' />
        /// <devdoc> 
        ///     The designer host will call this periodically when it wants to
        ///     ensure that any changes that have been made to the document
        ///     have been saved by the designer loader.  This method allows
        ///     designer loaders to implement a lazy-write scheme to improve 
        ///     performance.  This designer loader implements lazy writes by
        ///     listening to component change events.  If a component has 
        ///     changed it sets a "modified" bit.  When Flush is called the 
        ///     loader will write out a new code dom tree.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public override void Flush() {

 
            if (_state[StateFlushInProgress] || !_state[StateLoaded] || !Modified) {
                return; 
            } 
            _state[StateFlushInProgress] = true;
 
            Cursor oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try { 
                IDesignerLoaderHost host = _host;
                Debug.Assert(host != null, "designer loader was asked to flush after it has been disposed."); 
 
                // If the host has a null root component, it probably failed
                // its last load.  In that case, there is nothing to flush. 
                //
                bool shouldChangeModified = true;
                if (host != null && host.RootComponent != null) {
                    using (_serializationManager.CreateSession()) { 
                        try {
                            PerformFlush(_serializationManager); 
                        } 
                        catch (CheckoutException) {
                            shouldChangeModified = false; // don't need to report that one it already has shown an error message 
                            throw;
                        }
                        catch (Exception ex) {
                            _serializationManager.Errors.Add(ex); 
                        }
 
                        ICollection errors = _serializationManager.Errors; 
                        if (errors != null && errors.Count > 0) {
                            ReportFlushErrors(errors); 
                        }
                    }
                }
                if(shouldChangeModified) { 
                    Modified = false;
                } 
            } 
            finally {
                _state[StateFlushInProgress] = false; 
                Cursor.Current = oldCursor;
            }
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.GetService"]/*' />
        /// <devdoc> 
        ///     Helper method that gives access to the service provider. 
        /// </devdoc>
        protected object GetService(Type serviceType) { 
            object service = null;

            if (_host != null) {
                service = _host.GetService(serviceType); 
            }
 
            return service; 
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Initialize"]/*' />
        /// <devdoc>
        ///     This method is called immediately after the first time
        ///     BeginLoad is invoked.  This is an appopriate place to 
        ///     add custom services to the loader host.  Remember to
        ///     remove any custom services you add here by overriding 
        ///     Dispose. 
        /// </devdoc>
        protected virtual void Initialize() { 
            LoaderHost.AddService(typeof(IDesignerLoaderService), this);
        }

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.IsReloadNeeded"]/*' /> 
        /// <devdoc>
        ///     This method an be overridden to provide some intelligent 
        ///     logic to determine if a reload is required.  This method is 
        ///     called when someone requests a reload but doesn't force
        ///     the reload.  It gives the loader an opportunity to scan 
        ///     the underlying storage to determine if a reload is acutually
        ///     needed.  The default implementation of this method always
        ///     returns true.
        /// </devdoc> 
        protected virtual bool IsReloadNeeded() {
            return true; 
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.OnBeginLoad"]/*' /> 
        /// <devdoc>
        ///     This method should be called by the designer loader service
        ///     when the first dependent load has started.  This initializes
        ///     the state of the code dom loader and prepares it for loading. 
        ///     By default, the designer loader provides
        ///     IDesignerLoaderService itself, so this is called automatically. 
        ///     If you provide your own loader service, or if you choose not 
        ///     to provide a loader service, you are responsible for calling
        ///     this method.  BeginLoad will automatically call this, either 
        ///     indirectly by calling AddLoadDependency if IDesignerLoaderService
        ///     is available, or directly if it is not.
        /// </devdoc>
        protected virtual void OnBeginLoad() { 

            _serializationSession = _serializationManager.CreateSession(); 
            _state[StateLoaded] = false; 

            // Make sure that we're removed any event sinks we added after we finished the load. 
            // Make sure that we're removed any event sinks we added after we finished the load.
            EnableComponentNotification(false);
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (cs != null) { 
                cs.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded);
                cs.ComponentAdding -= new ComponentEventHandler(this.OnComponentAdding); 
                cs.ComponentRemoving -= new ComponentEventHandler(this.OnComponentRemoving); 
                cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved);
                cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                cs.ComponentChanging -= new ComponentChangingEventHandler(this.OnComponentChanging);
                cs.ComponentRename -= new ComponentRenameEventHandler(OnComponentRename);
            }
 
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.EnableComponentNotification"]/*' /> 
        /// <devdoc>
        /// This method can be used to Enable or Disable component notification by the DesignerLoader. 
        /// </devdoc>
        protected virtual bool EnableComponentNotification(bool enable) {
            bool previouslyEnabled = _state[StateEnableComponentEvents];
 
            if (!previouslyEnabled  && enable) {
                _state[StateEnableComponentEvents] = true; 
            } 
            else if (previouslyEnabled && !enable) {
                _state[StateEnableComponentEvents] = false; 
            }

            return previouslyEnabled;
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.OnBeginUnload"]/*' /> 
        /// <devdoc> 
        ///     This method is called immediately before the document is unloaded.
        ///     The document may be unloaded in preparation for reload, or 
        ///     if the document failed the load.  If you added document-specific
        ///     services in OnBeginLoad or OnEndLoad, you should remove them
        ///     here.
        /// </devdoc> 
        protected virtual void OnBeginUnload() {
        } 
 
        /// <devdoc>
        ///     This is called whenever a new component is added to the design surface. 
        /// </devdoc>
        private void OnComponentAdded(object sender, ComponentEventArgs e) {

            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might 
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) Modified = true;
        } 

        /// <devdoc>
        ///     This is called right before a component is added to the design surface.
        /// </devdoc> 
        private void OnComponentAdding(object sender, ComponentEventArgs e) {
 
            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might 
            // be listening when asked to unload.
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) OnModifying();
        }
 
        /// <devdoc>
        ///     This is called whenever a component on the design surface changes. 
        /// </devdoc> 
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) {
 
            // We check the loader host here.  We do not actually listen to
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) Modified = true;
        } 
 
        /// <devdoc>
        ///     This is called right before a component on the design surface changes. 
        /// </devdoc>
        private void OnComponentChanging(object sender, ComponentChangingEventArgs e) {

            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might 
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) OnModifying();
        } 

        /// <devdoc>
        ///     This is called whenever a component is removed from the design surface.
        /// </devdoc> 
        private void OnComponentRemoved(object sender, ComponentEventArgs e) {
 
            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might 
            // be listening when asked to unload.
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) Modified = true;
        }
 
        /// <devdoc>
        ///     This is called right before a component is removed from the design surface. 
        /// </devdoc> 
        private void OnComponentRemoving(object sender, ComponentEventArgs e) {
 
            // We check the loader host here.  We do not actually listen to
            // this event until the loader has finished loading but if we
            // succeeded the load and the loader then failed later, we might
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) OnModifying();
        } 
 
        /// <devdoc>
        ///     Raised by the host when a component is renamed.  Here we modify ourselves 
        ///     and then whack the component declaration.  At the next code gen
        ///     cycle we will recreate the declaration.
        /// </devdoc>
        private void OnComponentRename(object sender, ComponentRenameEventArgs e) { 

            // We check the loader host here.  We do not actually listen to 
            // this event until the loader has finished loading but if we 
            // succeeded the load and the loader then failed later, we might
            // be listening when asked to unload. 
            if (_state[StateEnableComponentEvents] && !LoaderHost.Loading) {
                OnModifying();
                Modified = true;
            } 
        }
 
        /// <devdoc> 
        ///     Called when this document becomes active.  here we check to see if
        ///     someone else has modified the contents of our buffer.  If so, we 
        ///     ask the designer to reload.
        /// </devdoc>
        private void OnDesignerActivate(object sender, EventArgs e) {
            _state[StateActiveDocument] = true; 

            if (_state[StateDeferredReload] && _host != null) { 
                _state[StateDeferredReload] = false; 
                ReloadOptions flags = ReloadOptions.Default;
 
                if (_state[StateForceReload]) flags |= ReloadOptions.Force;
                if (!_state[StateFlushReload]) flags |= ReloadOptions.NoFlush;
                if (_state[StateModifyIfErrors]) flags |= ReloadOptions.ModifyOnError;
 
                Reload(flags);
            } 
        } 

        /// <devdoc> 
        ///     Called when this document loses activation.  We just remember this
        ///     for later.
        /// </devdoc>
        private void OnDesignerDeactivate(object sender, EventArgs e) { 
            _state[StateActiveDocument] = false;
        } 
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.OnEndLoad"]/*' />
        /// <devdoc> 
        ///     This method should be called by the designer loader service
        ///     when all dependent loads have been completed.  This
        ///     "shuts down" the loading process that was initiated by
        ///     BeginLoad.  By default, the designer loader provides 
        ///     IDesignerLoaderService itself, so this is called automatically.
        ///     If you provide your own loader service, or if you choose not 
        ///     to provide a loader service, you are responsible for calling 
        ///     this method.  BeginLoad will automatically call this, either
        ///     indirectly by calling DependentLoadComplete if IDesignerLoaderService 
        ///     is available, or directly if it is not.
        /// </devdoc>
        protected virtual void OnEndLoad(bool successful, ICollection errors) {
            //we don't want successful to be true here if there were load errors. 
            //this may allow a situation where we have a dirtied WSOD and might allow
            //a user to save a partially loaded designer docdata. 
            successful = successful 
                                && (errors == null || errors.Count == 0)
                                && (_serializationManager.Errors == null 
                                    || _serializationManager.Errors.Count == 0);
            try {
                _state[StateLoaded] = true;
 
                IDesignerLoaderHost2 lh2 = GetService(typeof(IDesignerLoaderHost2)) as IDesignerLoaderHost2;
 
                if (!successful && (lh2 == null || !lh2.IgnoreErrorsDuringReload)) { 
                    // Can we even show the Continue Ignore errors in DTEL?
                    if (lh2 != null) { 
                        lh2.CanReloadWithErrors = LoaderHost.RootComponent != null;
                    }
                    UnloadDocument();
                } 
                else {
                    successful = true; 
                } 

 
                // Inform the serialization manager that we are all done.  The serialization
                // manager clears state at this point to help enforce a stateless serialization
                // mechanism.
                // 
                if (errors != null) {
                    foreach(object err in errors) { 
                        _serializationManager.Errors.Add(err); 
                    }
                } 
                errors = _serializationManager.Errors;
            }
            finally {
                _serializationSession.Dispose(); 
                _serializationSession = null;
            } 
 
            if (successful) {
 
                // After a successful load we will want to monitor a bunch of events so we know when
                // to make the loader modified.
                //
 
                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                if (cs != null) { 
                    cs.ComponentAdded += new ComponentEventHandler(this.OnComponentAdded); 
                    cs.ComponentAdding += new ComponentEventHandler(this.OnComponentAdding);
                    cs.ComponentRemoving += new ComponentEventHandler(this.OnComponentRemoving); 
                    cs.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved);
                    cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
                    cs.ComponentChanging += new ComponentChangingEventHandler(this.OnComponentChanging);
                    cs.ComponentRename += new ComponentRenameEventHandler(OnComponentRename); 
                }
                EnableComponentNotification(true); 
            } 

            LoaderHost.EndLoad(_baseComponentClassName, successful, errors); 

            // if we got errors in the load, set ourselves as modified so we'll regen code.  If this fails, we don't
            // care; the Modified bit was only a hint.
            // 
            if (_state[StateModifyIfErrors] && errors != null && errors.Count > 0) {
                try { 
                    OnModifying(); 
                    Modified = true;
                } 
                catch (CheckoutException ex) {
                    if (ex != CheckoutException.Canceled) {
                        throw;
                    } 
                }
            } 
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.OnModifying"]/*' /> 
        /// <devdoc>
        ///     This method is called in response to a component changing, adding or removing event to indicate
        ///     that the designer is about to be modified.  Those interested in implementing source code
        ///     control may do so by overriding this method.  A call to OnModifying does not mean that the 
        ///     Modified property will later be set to true; it is merly an intention to do so.
        /// </devdoc> 
        protected virtual void OnModifying() { 
        }
 
        /// <devdoc>
        ///     Invoked by the loader host when it actually performs the reload, but before
        ///     the reload actually happens.  Here we unload our part of the loader
        ///     and get us ready for the pending reload. 
        /// </devdoc>
        private void OnIdle(object sender, EventArgs e) { 
            Application.Idle -= new EventHandler(this.OnIdle); 
            if (_state[StateReloadAtIdle]) {
                _state[StateReloadAtIdle] = false; 

                //check to see if we are actually the active document.
                DesignSurfaceManager mgr = (DesignSurfaceManager)GetService(typeof(DesignSurfaceManager));
                DesignSurface thisSurface = (DesignSurface)GetService(typeof(DesignSurface)); 
                Debug.Assert(mgr != null && thisSurface != null);
                if (mgr != null && thisSurface != null) { 
                    if (!object.ReferenceEquals(mgr.ActiveDesignSurface, thisSurface)) { 
                        //somehow, we got deactivated and weren't told.
                        _state[StateActiveDocument] = false; 
                        _state[StateDeferredReload] = true; //reload on activate
                        return;
                    }
                } 

                IDesignerLoaderHost host = LoaderHost; 
                if(host != null) { 
                    if (_state[StateForceReload] || IsReloadNeeded()) {
 
                        try {
                            if (_state[StateFlushReload]) {
                                Flush();
                            } 

                            UnloadDocument(); 
                            host.Reload(); 
                        }
                        finally { 
                            _state[StateForceReload | StateModifyIfErrors | StateFlushReload] = false;
                        }
                    }
                } 
            }
        } 
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.PerformFlush"]/*' />
        /// <devdoc> 
        ///     This method is called when it is time to flush the
        ///     contents of the loader.  You should save any state
        ///     at this time.
        /// </devdoc> 
        protected abstract void PerformFlush(IDesignerSerializationManager serializationManager);
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.PerformLoad"]/*' /> 
        /// <devdoc>
        ///     This method is called when it is time to load the 
        ///     design surface.  If you are loading asynchronously
        ///     you should ask for IDesignerLoaderService and call
        ///     AddLoadDependency.  When loading asynchronously you
        ///     should at least create the root component during 
        ///     PerformLoad.  The DesignSurface is only able to provide
        ///     a view when there is a root component. 
        /// </devdoc> 
        protected abstract void PerformLoad(IDesignerSerializationManager serializationManager);
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.Reload"]/*' />
        /// <devdoc>
        ///     This method schedules a reload of the designer.
        ///     Designer reloading happens asynchronously in order 
        ///     to unwind the stack before the reload begins.  If
        ///     force is true, a reload is always performed.  If 
        ///     it is false, a reload is only performed if the 
        ///     underlying code dom tree has changed in a way that
        ///     would affect the form. 
        ///     If flush is true, the designer is flushed before performing
        ///     a reload.  If false, any designer changes are abandonded.
        ///     If ModifyOnError is true, the designer loader will be put
        ///     in the modified state if any errors happened during the 
        ///     load.
        /// </devdoc> 
        protected void Reload(ReloadOptions flags) { 

            _state[StateForceReload] = ((flags & ReloadOptions.Force) != 0); 
            _state[StateFlushReload] = ((flags & ReloadOptions.NoFlush) == 0);
            _state[StateModifyIfErrors] = ((flags & ReloadOptions.ModifyOnError) != 0);

            // Our implementation of Reload only reloads if we are the 
            // active designer.  Otherwise, we wait until we become
            // active and reload at that time.  We also never do a 
            // reload if we are flushing code. 
            //
            if (!_state[StateFlushInProgress]) { 
                if (_state[StateActiveDocument]) {
                    if (!_state[StateReloadAtIdle]) {
                        Application.Idle += new EventHandler(this.OnIdle);
                        _state[StateReloadAtIdle] = true; 
                    }
                } 
                else { 
                    _state[StateDeferredReload] = true;
                } 
            }
        }

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.ReportFlushErrors"]/*' /> 
        /// <devdoc>
        ///     This method is called during flush if one or more errors occurred while 
        ///     flushing changes.  The values in the errors collection may either be 
        ///     exceptions or objects whose ToString value describes the error.  The default
        ///     implementation of this method takes last exception in the collection and 
        ///     raises it as an exception.
        /// </devdoc>
        protected virtual void ReportFlushErrors(ICollection errors) {
            object lastError = null; 
            foreach(object e in errors) {
                lastError = e; 
            } 

            Debug.Assert(lastError != null, "Someone embedded a null in the error collection"); 

            if (lastError != null) {
                Exception ex = lastError as Exception;
                if (ex == null) { 
                    ex = new InvalidOperationException(lastError.ToString());
                } 
                throw ex; 
            }
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.SetBaseComponentClassName"]/*' />
        /// <devdoc>
        ///     This property provides the name the designer surface 
        ///     will use for the base class.  Normally this is a fully
        ///     qualified name such as "Project1.Form1".  You should set 
        ///     this before finishing the load.  Generally this is set 
        ///     during PerformLoad.
        /// </devdoc> 
        protected void SetBaseComponentClassName(string name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            } 
            _baseComponentClassName = name;
        } 
 
        /// <devdoc>
        ///     Simple helper routine that will throw an exception if we need a service, but cannot get 
        ///     to it.  You should only throw for missing services that are absolutely essential for
        ///     operation.  If there is a way to gracefully degrade, then you should do it.
        /// </devdoc>
        private void ThrowMissingService(Type serviceType) { 
            Exception ex = new InvalidOperationException(SR.GetString(SR.BasicDesignerLoaderMissingService, serviceType.Name));
            ex.HelpLink = SR.BasicDesignerLoaderMissingService; 
            throw ex; 
        }
 
        /// <devdoc>
        ///     This method will be called when the document is to be unloaded.  It
        ///     does not dispose us, but it gets us ready for a dispose or a reload.
        /// </devdoc> 
        private void UnloadDocument() {
            OnBeginUnload(); 
            _state[StateLoaded] = false; 
            _baseComponentClassName = null;
        } 

        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.IDesignerLoaderService.AddLoadDependency"]/*' />
        /// <devdoc>
        ///     Adds a load dependency to this loader.  This indicates that some other 
        ///     object is also participating in the load, and that the designer loader
        ///     should not call EndLoad on the loader host until all load dependencies 
        ///     have called DependentLoadComplete on the designer loader. 
        /// </devdoc>
        void IDesignerLoaderService.AddLoadDependency() { 
            if (_serializationManager == null) {
                throw new InvalidOperationException();
            }
 
            if (_loadDependencyCount++ == 0) {
                OnBeginLoad(); 
            } 
        }
 
        /// <include file='doc\BasicDesignerLoader.uex' path='docs/doc[@for="BasicDesignerLoader.IDesignerLoaderService.DependentLoadComplete"]/*' />
        /// <devdoc>
        ///     This is called by any object that has previously called
        ///     AddLoadDependency to signal that the dependent load has completed. 
        ///     The caller should pass either an empty collection or null to indicate
        ///     a successful load, or a collection of exceptions that indicate the 
        ///     reason(s) for failure. 
        /// </devdoc>
        void IDesignerLoaderService.DependentLoadComplete(bool successful, ICollection errorCollection) { 

            if (_loadDependencyCount == 0) {
                throw new InvalidOperationException();
            } 

            // If the dependent load failed, remember it.  There may be multiple 
            // dependent loads.  If any one fails, we're sunk. 
            //
            if (!successful) { 
                _state[StateLoadFailed] = true;
            }

            if (--_loadDependencyCount == 0) { 

                // We have just completed the last dependent load.  Report this. 
                // 
                OnEndLoad(!_state[StateLoadFailed], errorCollection);
            } 
            else {

                // Otherwise, add these errors to the serialization manager.
                // 
                if (errorCollection != null) {
                    foreach(object err in errorCollection) { 
                        _serializationManager.Errors.Add(err); 
                    }
                } 
            }
        }

        /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.IDesignerLoaderService.Reload"]/*' /> 
        /// <devdoc>
        ///     This can be called by an outside object to request that the loader 
        ///     reload the design document.  If it supports reloading and wants to 
        ///     comply with the reload, the designer loader should return true.  Otherwise
        ///     it should return false, indicating that the reload will not occur. 
        ///     Callers should not rely on the reload happening immediately; the
        ///     designer loader may schedule this for some other time, or it may
        ///     try to reload at once.
        /// </devdoc> 
        bool IDesignerLoaderService.Reload() {
 
            if (_state[StateReloadSupported] && _loadDependencyCount == 0) { 
                Reload(ReloadOptions.Force);
                return true; 
            }

            return false;
        } 

        /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions"]/*' /> 
        /// <devdoc> 
        ///     A list of flags that indicate rules to apply when requesting
        ///     that the designer reload itself. 
        /// </devdoc>
        [Flags]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
        protected enum ReloadOptions { 

            /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions.Default"]/*' /> 
            /// <devdoc> 
            ///     Peform the default behavior.
            /// </devdoc> 
            Default  = 0x00,

            /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions.ModifyOnError"]/*' />
            /// <devdoc> 
            ///     If this flag is set, any error encoutered during the
            ///     reload will automatically put the designer loader in 
            ///     the modified state. 
            /// </devdoc>
            ModifyOnError = 0x01, 

            /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions.Force"]/*' />
            /// <devdoc>
            ///     If this flag is set, a reload will occur.  If the 
            ///     flag is not set a reload will only occur if the
            ///     IsReloadNeeded method returns true. 
            /// </devdoc> 
            Force = 0x02,
 
            /// <include file='doc\BaseDesignerLoader.uex' path='docs/doc[@for="BaseDesignerLoader.ReloadOptions.NoFlush"]/*' />
            /// <devdoc>
            ///     If this flag is set, any pending changes in the
            ///     designer will be abandonded.  If this flag is not 
            ///     set, designer changes will be flushed through the
            ///     designer loader before reloading the design surface. 
            /// </devdoc> 
            NoFlush = 0x04,
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
