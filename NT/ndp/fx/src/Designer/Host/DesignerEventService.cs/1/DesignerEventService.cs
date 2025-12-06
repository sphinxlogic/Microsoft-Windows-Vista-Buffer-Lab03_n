//------------------------------------------------------------------------------ 
// <copyright file="DesignerEventService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
 
    /// <devdoc>
    ///     This service tracks individual designer events.  The class itself 
    ///     receives event information by direct calls from DesignerApplication. 
    ///     Those wishing to replace this service may do so but need to override
    ///     the appropriate virtual methods on DesignerApplication. 
    /// </devdoc>
    internal sealed class DesignerEventService : IDesignerEventService {

        private static readonly object EventActiveDesignerChanged   = new object(); 
        private static readonly object EventDesignerCreated         = new object();
        private static readonly object EventDesignerDisposed        = new object(); 
        private static readonly object EventSelectionChanged        = new object(); 

        private ArrayList           _designerList;          // read write list used as data for the collection 
        private DesignerCollection  _designerCollection;    // public read only view of the above list
        private IDesignerHost       _activeDesigner;        // the currently active designer.  Can be null
        private EventHandlerList    _events;                // list of events.  Can be null
        private bool                _inTransaction;         // true if we are in a transaction 
        private bool                _deferredSelChange;     // true if we have a deferred selection change notification pending
 
        /// <devdoc> 
        ///     Internal ctor to prevent semitrust from creating us.
        /// </devdoc> 
        internal DesignerEventService() {
        }

        /// <devdoc> 
        ///     This is called by the DesignerApplication class when
        ///     a designer is activated.  The passed in designer can 
        ///     be null to signify no designer is currently active. 
        /// </devdoc>
        internal void OnActivateDesigner(DesignSurface surface) { 

            IDesignerHost host = null;
            if (surface != null) {
                host = surface.GetService(typeof(IDesignerHost)) as IDesignerHost; 
                Debug.Assert(host != null, "Design surface did not provide us with a designer host");
            } 
 
            // If the designer host is not in our collection, add it.
            if (host != null && (_designerList == null || !_designerList.Contains(host))) { 
                OnCreateDesigner(surface);
            }

            if (_activeDesigner != host) { 
                IDesignerHost oldDesigner = _activeDesigner;
                _activeDesigner = host; 
 
                if (oldDesigner != null) {
                    SinkChangeEvents(oldDesigner, false); 
                }

                if (_activeDesigner != null) {
                    SinkChangeEvents(_activeDesigner, true); 
                }
 
                if (_events != null) { 
                    ActiveDesignerEventHandler eh = _events[EventActiveDesignerChanged] as ActiveDesignerEventHandler;
                    if (eh != null) { 
                        eh(this, new ActiveDesignerEventArgs(oldDesigner, host));
                    }
                }
 
                // Activating a new designer automatically pushes a new selection.
                // 
                OnSelectionChanged(this, EventArgs.Empty); 
            }
        } 

        /// <devdoc>
        ///     Called when a component is added or removed from the active designer.
        ///     We raise a selection change event here. 
        /// </devdoc>
        private void OnComponentAddedRemoved(object sender, ComponentEventArgs ce) { 
            IComponent comp = ce.Component as IComponent; 
            if (comp != null) {
                ISite site = comp.Site; 
                if (site != null) {
                    IDesignerHost host = site.Container as IDesignerHost;
                    if (host != null && host.Loading) {
                        _deferredSelChange = true; 
                        return;
                    } 
                } 
            }
 
            OnSelectionChanged(this, EventArgs.Empty);
        }

        /// <devdoc> 
        ///     Called when a component has changed on the active designer. Here
        ///     we grab the active selection service and see if the component that 
        ///     has changed is also selected.  If it is, then we raise a global 
        ///     selection changed event.
        /// </devdoc> 
        private void OnComponentChanged(object sender, ComponentChangedEventArgs ce) {
            IComponent comp = ce.Component as IComponent;
            if (comp != null) {
                ISite site = comp.Site; 
                if (site != null) {
                    ISelectionService ss = site.GetService(typeof(ISelectionService)) as ISelectionService; 
                    if (ss != null && ss.GetComponentSelected(comp)) { 
                        OnSelectionChanged(this, EventArgs.Empty);
                    } 
                }
            }
        }
 
        /// <devdoc>
        ///     This is called by the DesignerApplication class when 
        ///     a designer is created.  Activation generally follows. 
        /// </devdoc>
        internal void OnCreateDesigner(DesignSurface surface) { 

            Debug.Assert(surface != null, "DesignerApplication should not pass null here");
            IDesignerHost host = surface.GetService(typeof(IDesignerHost)) as IDesignerHost;
            Debug.Assert(host != null, "Design surface did not provide us with a designer host"); 

            if (_designerList == null) { 
                _designerList = new ArrayList(); 
            }
 
            _designerList.Add(host);

            // Hookup an object disposed handler on the design surface so we know when it's
            // gone. 
            //
            surface.Disposed += new EventHandler(this.OnDesignerDisposed); 
 
            if (_events != null) {
                DesignerEventHandler eh = _events[EventDesignerCreated] as DesignerEventHandler; 
                if (eh != null) {
                    eh(this, new DesignerEventArgs(host));
                }
            } 
        }
 
        /// <devdoc> 
        ///     Called by DesignSurface when it is about to be disposed.
        /// </devdoc> 
        private void OnDesignerDisposed(object sender, EventArgs e) {
            DesignSurface surface = (DesignSurface)sender;
            surface.Disposed -= new EventHandler(this.OnDesignerDisposed);
 
            // Detatch the selection change and add/remove events, if we were monitoring such events
            // 
            SinkChangeEvents(surface, false); 

            IDesignerHost host = surface.GetService(typeof(IDesignerHost)) as IDesignerHost; 
            Debug.Assert(host != null, "Design surface removed host too early in dispose");
            if (host != null) {

                if (_events != null) { 
                    DesignerEventHandler eh = _events[EventDesignerDisposed] as DesignerEventHandler;
                    if (eh != null) { 
                        eh(this, new DesignerEventArgs(host)); 
                    }
                } 

                if (_designerList != null) {
                    _designerList.Remove(host);
                } 
            }
        } 
 
        /// <devdoc>
        ///     Called by the active designer's selection service when the selection changes. 
        ///     Also called directly by us when the active designer changes, as this is
        ///     also a change to the global selection context.
        /// </devdoc>
        private void OnSelectionChanged(object sender, EventArgs e) { 

            if (_inTransaction) { 
                _deferredSelChange = true; 
            }
            else { 
                if (_events != null) {
                    EventHandler eh = _events[EventSelectionChanged] as EventHandler;
                    if (eh != null) {
                        eh(this, e); 
                    }
                } 
            } 
        }
 
        /// <devdoc>
        ///     Called by the designer host when it is done loading
        ///     Here we queue up selection notification.
        /// </devdoc> 
        private void OnLoadComplete(object sender, EventArgs e) {
            if (_deferredSelChange) { 
                _deferredSelChange = false; 
                OnSelectionChanged(this, EventArgs.Empty);
            } 
        }

        /// <devdoc>
        ///     Called by the designer host when it is entering or leaving a batch 
        ///     operation.  Here we queue up selection notification and we turn off
        ///     our UI. 
        /// </devdoc> 
        private void OnTransactionClosed(object sender, DesignerTransactionCloseEventArgs e) {
            if (e.LastTransaction) { 
                _inTransaction = false;
                if (_deferredSelChange) {
                    _deferredSelChange = false;
                    OnSelectionChanged(this, EventArgs.Empty); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Called by the designer host when it is entering or leaving a batch
        ///     operation.  Here we queue up selection notification and we turn off
        ///     our UI.
        /// </devdoc> 
        private void OnTransactionOpened(object sender, EventArgs e) {
            _inTransaction = true; 
        } 

        /// <devdoc> 
        ///     Sinks or unsinks selection and component change events from the
        ///     provided service provider.  We need to raise global selection change
        ///     notifications.  A global selection change should be raised whenever
        ///     the selection of the active designer changes, whenever a component 
        ///     is added or removed from the active designer, or whenever the
        ///     active designer itself changes. 
        /// </devdoc> 
        private void SinkChangeEvents(IServiceProvider provider, bool sink) {
            ISelectionService ss = provider.GetService(typeof(ISelectionService)) as ISelectionService; 
            IComponentChangeService cs = provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
            IDesignerHost host = provider.GetService(typeof(IDesignerHost)) as IDesignerHost;

            if (sink) { 
                if (ss != null) {
                    ss.SelectionChanged += new EventHandler(this.OnSelectionChanged); 
                } 
                if (cs != null) {
                    ComponentEventHandler ce = new ComponentEventHandler(this.OnComponentAddedRemoved); 
                    cs.ComponentAdded += ce;
                    cs.ComponentRemoved += ce;
                    cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
                } 
                if (host != null) {
                    host.TransactionOpened += new EventHandler(this.OnTransactionOpened); 
                    host.TransactionClosed += new DesignerTransactionCloseEventHandler(this.OnTransactionClosed); 
                    host.LoadComplete += new EventHandler(this.OnLoadComplete);
                    if (host.InTransaction) { 
                        OnTransactionOpened(host, EventArgs.Empty);
                    }
                }
            } 
            else {
                if (ss != null) { 
                    ss.SelectionChanged -= new EventHandler(this.OnSelectionChanged); 
                }
                if (cs != null) { 
                    ComponentEventHandler ce = new ComponentEventHandler(this.OnComponentAddedRemoved);
                    cs.ComponentAdded -= ce;
                    cs.ComponentRemoved -= ce;
                    cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                }
                if (host != null) { 
                    host.TransactionOpened -= new EventHandler(this.OnTransactionOpened); 
                    host.TransactionClosed -= new DesignerTransactionCloseEventHandler(this.OnTransactionClosed);
                    host.LoadComplete -= new EventHandler(this.OnLoadComplete); 
                    if (host.InTransaction) {
                        OnTransactionClosed(host, new DesignerTransactionCloseEventArgs(false, true));
                    }
                } 
            }
        } 
 
        /// <devdoc>
        ///     Gets the currently active designer. 
        /// </devdoc>
        IDesignerHost IDesignerEventService.ActiveDesigner {
            get {
                return _activeDesigner; 
            }
        } 
 
        /// <devdoc>
        ///     Gets or 
        ///     sets a collection of running design documents in the development environment.
        /// </devdoc>
        DesignerCollection IDesignerEventService.Designers {
            get { 
                if (_designerList == null) {
                    _designerList = new ArrayList(); 
                } 
                if (_designerCollection == null) {
                    _designerCollection = new DesignerCollection(_designerList); 
                }
                return _designerCollection;
            }
        } 

        /// <devdoc> 
        ///     Adds an event that will be raised when the currently active designer 
        ///     changes.
        /// </devdoc> 
        event ActiveDesignerEventHandler IDesignerEventService.ActiveDesignerChanged {
            add {
                if (_events == null) {
                    _events = new EventHandlerList(); 
                }
                _events[EventActiveDesignerChanged] = Delegate.Combine((Delegate)_events[EventActiveDesignerChanged], value); 
            } 
            remove {
                if (_events != null) { 
                    _events[EventActiveDesignerChanged] = Delegate.Remove((Delegate)_events[EventActiveDesignerChanged], value);
                }
            }
        } 

        /// <devdoc> 
        ///     Adds an event that will be raised when a designer is created. 
        /// </devdoc>
        event DesignerEventHandler IDesignerEventService.DesignerCreated { 
            add {
                if (_events == null) {
                    _events = new EventHandlerList();
                } 
                _events[EventDesignerCreated] = Delegate.Combine((Delegate)_events[EventDesignerCreated], value);
            } 
            remove { 
                if (_events != null) {
                    _events[EventDesignerCreated] = Delegate.Remove((Delegate)_events[EventDesignerCreated], value); 
                }
            }
        }
 
        /// <devdoc>
        ///     Adds an event that will be raised when a designer is disposed. 
        /// </devdoc> 
        event DesignerEventHandler IDesignerEventService.DesignerDisposed {
            add { 
                if (_events == null) {
                    _events = new EventHandlerList();
                }
                _events[EventDesignerDisposed] = Delegate.Combine((Delegate)_events[EventDesignerDisposed], value); 
            }
            remove { 
                if (_events != null) { 
                    _events[EventDesignerDisposed] = Delegate.Remove((Delegate)_events[EventDesignerDisposed], value);
                } 
            }
        }

        /// <devdoc> 
        ///     Adds an event that will be raised when the global selection changes.
        /// </devdoc> 
        event EventHandler IDesignerEventService.SelectionChanged { 
            add {
                if (_events == null) { 
                    _events = new EventHandlerList();
                }
                _events[EventSelectionChanged] = Delegate.Combine((Delegate)_events[EventSelectionChanged], value);
            } 
            remove {
                if (_events != null) { 
                    _events[EventSelectionChanged] = Delegate.Remove((Delegate)_events[EventSelectionChanged], value); 
                }
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerEventService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
 
    /// <devdoc>
    ///     This service tracks individual designer events.  The class itself 
    ///     receives event information by direct calls from DesignerApplication. 
    ///     Those wishing to replace this service may do so but need to override
    ///     the appropriate virtual methods on DesignerApplication. 
    /// </devdoc>
    internal sealed class DesignerEventService : IDesignerEventService {

        private static readonly object EventActiveDesignerChanged   = new object(); 
        private static readonly object EventDesignerCreated         = new object();
        private static readonly object EventDesignerDisposed        = new object(); 
        private static readonly object EventSelectionChanged        = new object(); 

        private ArrayList           _designerList;          // read write list used as data for the collection 
        private DesignerCollection  _designerCollection;    // public read only view of the above list
        private IDesignerHost       _activeDesigner;        // the currently active designer.  Can be null
        private EventHandlerList    _events;                // list of events.  Can be null
        private bool                _inTransaction;         // true if we are in a transaction 
        private bool                _deferredSelChange;     // true if we have a deferred selection change notification pending
 
        /// <devdoc> 
        ///     Internal ctor to prevent semitrust from creating us.
        /// </devdoc> 
        internal DesignerEventService() {
        }

        /// <devdoc> 
        ///     This is called by the DesignerApplication class when
        ///     a designer is activated.  The passed in designer can 
        ///     be null to signify no designer is currently active. 
        /// </devdoc>
        internal void OnActivateDesigner(DesignSurface surface) { 

            IDesignerHost host = null;
            if (surface != null) {
                host = surface.GetService(typeof(IDesignerHost)) as IDesignerHost; 
                Debug.Assert(host != null, "Design surface did not provide us with a designer host");
            } 
 
            // If the designer host is not in our collection, add it.
            if (host != null && (_designerList == null || !_designerList.Contains(host))) { 
                OnCreateDesigner(surface);
            }

            if (_activeDesigner != host) { 
                IDesignerHost oldDesigner = _activeDesigner;
                _activeDesigner = host; 
 
                if (oldDesigner != null) {
                    SinkChangeEvents(oldDesigner, false); 
                }

                if (_activeDesigner != null) {
                    SinkChangeEvents(_activeDesigner, true); 
                }
 
                if (_events != null) { 
                    ActiveDesignerEventHandler eh = _events[EventActiveDesignerChanged] as ActiveDesignerEventHandler;
                    if (eh != null) { 
                        eh(this, new ActiveDesignerEventArgs(oldDesigner, host));
                    }
                }
 
                // Activating a new designer automatically pushes a new selection.
                // 
                OnSelectionChanged(this, EventArgs.Empty); 
            }
        } 

        /// <devdoc>
        ///     Called when a component is added or removed from the active designer.
        ///     We raise a selection change event here. 
        /// </devdoc>
        private void OnComponentAddedRemoved(object sender, ComponentEventArgs ce) { 
            IComponent comp = ce.Component as IComponent; 
            if (comp != null) {
                ISite site = comp.Site; 
                if (site != null) {
                    IDesignerHost host = site.Container as IDesignerHost;
                    if (host != null && host.Loading) {
                        _deferredSelChange = true; 
                        return;
                    } 
                } 
            }
 
            OnSelectionChanged(this, EventArgs.Empty);
        }

        /// <devdoc> 
        ///     Called when a component has changed on the active designer. Here
        ///     we grab the active selection service and see if the component that 
        ///     has changed is also selected.  If it is, then we raise a global 
        ///     selection changed event.
        /// </devdoc> 
        private void OnComponentChanged(object sender, ComponentChangedEventArgs ce) {
            IComponent comp = ce.Component as IComponent;
            if (comp != null) {
                ISite site = comp.Site; 
                if (site != null) {
                    ISelectionService ss = site.GetService(typeof(ISelectionService)) as ISelectionService; 
                    if (ss != null && ss.GetComponentSelected(comp)) { 
                        OnSelectionChanged(this, EventArgs.Empty);
                    } 
                }
            }
        }
 
        /// <devdoc>
        ///     This is called by the DesignerApplication class when 
        ///     a designer is created.  Activation generally follows. 
        /// </devdoc>
        internal void OnCreateDesigner(DesignSurface surface) { 

            Debug.Assert(surface != null, "DesignerApplication should not pass null here");
            IDesignerHost host = surface.GetService(typeof(IDesignerHost)) as IDesignerHost;
            Debug.Assert(host != null, "Design surface did not provide us with a designer host"); 

            if (_designerList == null) { 
                _designerList = new ArrayList(); 
            }
 
            _designerList.Add(host);

            // Hookup an object disposed handler on the design surface so we know when it's
            // gone. 
            //
            surface.Disposed += new EventHandler(this.OnDesignerDisposed); 
 
            if (_events != null) {
                DesignerEventHandler eh = _events[EventDesignerCreated] as DesignerEventHandler; 
                if (eh != null) {
                    eh(this, new DesignerEventArgs(host));
                }
            } 
        }
 
        /// <devdoc> 
        ///     Called by DesignSurface when it is about to be disposed.
        /// </devdoc> 
        private void OnDesignerDisposed(object sender, EventArgs e) {
            DesignSurface surface = (DesignSurface)sender;
            surface.Disposed -= new EventHandler(this.OnDesignerDisposed);
 
            // Detatch the selection change and add/remove events, if we were monitoring such events
            // 
            SinkChangeEvents(surface, false); 

            IDesignerHost host = surface.GetService(typeof(IDesignerHost)) as IDesignerHost; 
            Debug.Assert(host != null, "Design surface removed host too early in dispose");
            if (host != null) {

                if (_events != null) { 
                    DesignerEventHandler eh = _events[EventDesignerDisposed] as DesignerEventHandler;
                    if (eh != null) { 
                        eh(this, new DesignerEventArgs(host)); 
                    }
                } 

                if (_designerList != null) {
                    _designerList.Remove(host);
                } 
            }
        } 
 
        /// <devdoc>
        ///     Called by the active designer's selection service when the selection changes. 
        ///     Also called directly by us when the active designer changes, as this is
        ///     also a change to the global selection context.
        /// </devdoc>
        private void OnSelectionChanged(object sender, EventArgs e) { 

            if (_inTransaction) { 
                _deferredSelChange = true; 
            }
            else { 
                if (_events != null) {
                    EventHandler eh = _events[EventSelectionChanged] as EventHandler;
                    if (eh != null) {
                        eh(this, e); 
                    }
                } 
            } 
        }
 
        /// <devdoc>
        ///     Called by the designer host when it is done loading
        ///     Here we queue up selection notification.
        /// </devdoc> 
        private void OnLoadComplete(object sender, EventArgs e) {
            if (_deferredSelChange) { 
                _deferredSelChange = false; 
                OnSelectionChanged(this, EventArgs.Empty);
            } 
        }

        /// <devdoc>
        ///     Called by the designer host when it is entering or leaving a batch 
        ///     operation.  Here we queue up selection notification and we turn off
        ///     our UI. 
        /// </devdoc> 
        private void OnTransactionClosed(object sender, DesignerTransactionCloseEventArgs e) {
            if (e.LastTransaction) { 
                _inTransaction = false;
                if (_deferredSelChange) {
                    _deferredSelChange = false;
                    OnSelectionChanged(this, EventArgs.Empty); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Called by the designer host when it is entering or leaving a batch
        ///     operation.  Here we queue up selection notification and we turn off
        ///     our UI.
        /// </devdoc> 
        private void OnTransactionOpened(object sender, EventArgs e) {
            _inTransaction = true; 
        } 

        /// <devdoc> 
        ///     Sinks or unsinks selection and component change events from the
        ///     provided service provider.  We need to raise global selection change
        ///     notifications.  A global selection change should be raised whenever
        ///     the selection of the active designer changes, whenever a component 
        ///     is added or removed from the active designer, or whenever the
        ///     active designer itself changes. 
        /// </devdoc> 
        private void SinkChangeEvents(IServiceProvider provider, bool sink) {
            ISelectionService ss = provider.GetService(typeof(ISelectionService)) as ISelectionService; 
            IComponentChangeService cs = provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
            IDesignerHost host = provider.GetService(typeof(IDesignerHost)) as IDesignerHost;

            if (sink) { 
                if (ss != null) {
                    ss.SelectionChanged += new EventHandler(this.OnSelectionChanged); 
                } 
                if (cs != null) {
                    ComponentEventHandler ce = new ComponentEventHandler(this.OnComponentAddedRemoved); 
                    cs.ComponentAdded += ce;
                    cs.ComponentRemoved += ce;
                    cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
                } 
                if (host != null) {
                    host.TransactionOpened += new EventHandler(this.OnTransactionOpened); 
                    host.TransactionClosed += new DesignerTransactionCloseEventHandler(this.OnTransactionClosed); 
                    host.LoadComplete += new EventHandler(this.OnLoadComplete);
                    if (host.InTransaction) { 
                        OnTransactionOpened(host, EventArgs.Empty);
                    }
                }
            } 
            else {
                if (ss != null) { 
                    ss.SelectionChanged -= new EventHandler(this.OnSelectionChanged); 
                }
                if (cs != null) { 
                    ComponentEventHandler ce = new ComponentEventHandler(this.OnComponentAddedRemoved);
                    cs.ComponentAdded -= ce;
                    cs.ComponentRemoved -= ce;
                    cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                }
                if (host != null) { 
                    host.TransactionOpened -= new EventHandler(this.OnTransactionOpened); 
                    host.TransactionClosed -= new DesignerTransactionCloseEventHandler(this.OnTransactionClosed);
                    host.LoadComplete -= new EventHandler(this.OnLoadComplete); 
                    if (host.InTransaction) {
                        OnTransactionClosed(host, new DesignerTransactionCloseEventArgs(false, true));
                    }
                } 
            }
        } 
 
        /// <devdoc>
        ///     Gets the currently active designer. 
        /// </devdoc>
        IDesignerHost IDesignerEventService.ActiveDesigner {
            get {
                return _activeDesigner; 
            }
        } 
 
        /// <devdoc>
        ///     Gets or 
        ///     sets a collection of running design documents in the development environment.
        /// </devdoc>
        DesignerCollection IDesignerEventService.Designers {
            get { 
                if (_designerList == null) {
                    _designerList = new ArrayList(); 
                } 
                if (_designerCollection == null) {
                    _designerCollection = new DesignerCollection(_designerList); 
                }
                return _designerCollection;
            }
        } 

        /// <devdoc> 
        ///     Adds an event that will be raised when the currently active designer 
        ///     changes.
        /// </devdoc> 
        event ActiveDesignerEventHandler IDesignerEventService.ActiveDesignerChanged {
            add {
                if (_events == null) {
                    _events = new EventHandlerList(); 
                }
                _events[EventActiveDesignerChanged] = Delegate.Combine((Delegate)_events[EventActiveDesignerChanged], value); 
            } 
            remove {
                if (_events != null) { 
                    _events[EventActiveDesignerChanged] = Delegate.Remove((Delegate)_events[EventActiveDesignerChanged], value);
                }
            }
        } 

        /// <devdoc> 
        ///     Adds an event that will be raised when a designer is created. 
        /// </devdoc>
        event DesignerEventHandler IDesignerEventService.DesignerCreated { 
            add {
                if (_events == null) {
                    _events = new EventHandlerList();
                } 
                _events[EventDesignerCreated] = Delegate.Combine((Delegate)_events[EventDesignerCreated], value);
            } 
            remove { 
                if (_events != null) {
                    _events[EventDesignerCreated] = Delegate.Remove((Delegate)_events[EventDesignerCreated], value); 
                }
            }
        }
 
        /// <devdoc>
        ///     Adds an event that will be raised when a designer is disposed. 
        /// </devdoc> 
        event DesignerEventHandler IDesignerEventService.DesignerDisposed {
            add { 
                if (_events == null) {
                    _events = new EventHandlerList();
                }
                _events[EventDesignerDisposed] = Delegate.Combine((Delegate)_events[EventDesignerDisposed], value); 
            }
            remove { 
                if (_events != null) { 
                    _events[EventDesignerDisposed] = Delegate.Remove((Delegate)_events[EventDesignerDisposed], value);
                } 
            }
        }

        /// <devdoc> 
        ///     Adds an event that will be raised when the global selection changes.
        /// </devdoc> 
        event EventHandler IDesignerEventService.SelectionChanged { 
            add {
                if (_events == null) { 
                    _events = new EventHandlerList();
                }
                _events[EventSelectionChanged] = Delegate.Combine((Delegate)_events[EventSelectionChanged], value);
            } 
            remove {
                if (_events != null) { 
                    _events[EventSelectionChanged] = Delegate.Remove((Delegate)_events[EventSelectionChanged], value); 
                }
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
