//#define DEBUGDESIGNERTASKS 
//------------------------------------------------------------------------------
// <copyright file="DesignerActionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.ComponentModel.Design { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.Timers;
    using System.Diagnostics.CodeAnalysis; 
    using System.Diagnostics;
 
    /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService"]/*' /> 
    /// <devdoc>
    /// </devdoc> 
    public sealed class DesignerActionUIService : IDisposable {

        private DesignerActionUIStateChangeEventHandler     designerActionUIStateChangedEventHandler;
        private IServiceProvider                            serviceProvider;//standard service provider 
        private DesignerActionService                       designerActionService;
 
        internal DesignerActionUIService(IServiceProvider serviceProvider) { 
            this.serviceProvider = serviceProvider;
 
            if (serviceProvider != null) {
                this.serviceProvider = serviceProvider;

                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
                host.AddService(typeof(DesignerActionUIService), this);
 
 
                designerActionService = serviceProvider.GetService(typeof(DesignerActionService)) as DesignerActionService;
                Debug.Assert(designerActionService != null, "we should have created and registered the DAService first"); 
            }
        }

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes all resources and unhooks all events. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")] 
        public void Dispose() {
            if (serviceProvider != null) {
                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                if (host != null) { 
                    host.RemoveService(typeof(DesignerActionUIService));
                } 
            } 
        }
 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.DesignerActionUIChange"]/*' />
        /// <devdoc>
        ///     This event is thrown whenever a request is made to show/hide the ui
        /// </devdoc> 
        public event DesignerActionUIStateChangeEventHandler DesignerActionUIStateChange {
            add { 
                designerActionUIStateChangedEventHandler += value; 
            }
            remove { 
                designerActionUIStateChangedEventHandler -= value;
            }
        }
 

        public void HideUI(IComponent component) { 
            OnDesignerActionUIStateChange(new DesignerActionUIStateChangeEventArgs(component, DesignerActionUIStateChangeType.Hide)); 
        }
 
        public void ShowUI(IComponent component) {
            OnDesignerActionUIStateChange(new DesignerActionUIStateChangeEventArgs(component, DesignerActionUIStateChangeType.Show));
        }
 
         /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Refresh"]/*' />
        /// <devdoc> 
        ///     This is a new Helper Method that the service provides to refresh the DesignerActionGlyph as well as DesignerActionPanels. 
        /// </devdoc>
        public void Refresh(IComponent component) { 
            OnDesignerActionUIStateChange(new DesignerActionUIStateChangeEventArgs(component, DesignerActionUIStateChangeType.Refresh));
        }

                /// <devdoc> 
        ///     This fires our DesignerActionsChanged event.
        /// </devdoc> 
        private void OnDesignerActionUIStateChange(DesignerActionUIStateChangeEventArgs e) { 
            if (designerActionUIStateChangedEventHandler != null) {
                designerActionUIStateChangedEventHandler(this, e); 
            }
        }

 
        public bool ShouldAutoShow(IComponent component) {
 
            // Check the designer options... 
            if (serviceProvider != null) {
                DesignerOptionService opts = serviceProvider.GetService(typeof(DesignerOptionService)) as DesignerOptionService; 
                if (opts != null) {
                    PropertyDescriptor p = opts.Options.Properties["ObjectBoundSmartTagAutoShow"];
                    if (p != null && p.PropertyType == typeof(bool) && !(bool)p.GetValue(null)) {
                        return false; 
                    }
                } 
            } 

            if(designerActionService != null) { 
                DesignerActionListCollection coll = designerActionService.GetComponentActions(component);
                if(coll != null && coll.Count > 0) {
                    for(int i = 0;i<coll.Count; i++) {
                        if(coll[i].AutoShow) { 
                            return true;
                        } 
                    } 
                }
            } 
            return false;
        }

    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//#define DEBUGDESIGNERTASKS 
//------------------------------------------------------------------------------
// <copyright file="DesignerActionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.ComponentModel.Design { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.Timers;
    using System.Diagnostics.CodeAnalysis; 
    using System.Diagnostics;
 
    /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService"]/*' /> 
    /// <devdoc>
    /// </devdoc> 
    public sealed class DesignerActionUIService : IDisposable {

        private DesignerActionUIStateChangeEventHandler     designerActionUIStateChangedEventHandler;
        private IServiceProvider                            serviceProvider;//standard service provider 
        private DesignerActionService                       designerActionService;
 
        internal DesignerActionUIService(IServiceProvider serviceProvider) { 
            this.serviceProvider = serviceProvider;
 
            if (serviceProvider != null) {
                this.serviceProvider = serviceProvider;

                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
                host.AddService(typeof(DesignerActionUIService), this);
 
 
                designerActionService = serviceProvider.GetService(typeof(DesignerActionService)) as DesignerActionService;
                Debug.Assert(designerActionService != null, "we should have created and registered the DAService first"); 
            }
        }

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes all resources and unhooks all events. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")] 
        public void Dispose() {
            if (serviceProvider != null) {
                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                if (host != null) { 
                    host.RemoveService(typeof(DesignerActionUIService));
                } 
            } 
        }
 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.DesignerActionUIChange"]/*' />
        /// <devdoc>
        ///     This event is thrown whenever a request is made to show/hide the ui
        /// </devdoc> 
        public event DesignerActionUIStateChangeEventHandler DesignerActionUIStateChange {
            add { 
                designerActionUIStateChangedEventHandler += value; 
            }
            remove { 
                designerActionUIStateChangedEventHandler -= value;
            }
        }
 

        public void HideUI(IComponent component) { 
            OnDesignerActionUIStateChange(new DesignerActionUIStateChangeEventArgs(component, DesignerActionUIStateChangeType.Hide)); 
        }
 
        public void ShowUI(IComponent component) {
            OnDesignerActionUIStateChange(new DesignerActionUIStateChangeEventArgs(component, DesignerActionUIStateChangeType.Show));
        }
 
         /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Refresh"]/*' />
        /// <devdoc> 
        ///     This is a new Helper Method that the service provides to refresh the DesignerActionGlyph as well as DesignerActionPanels. 
        /// </devdoc>
        public void Refresh(IComponent component) { 
            OnDesignerActionUIStateChange(new DesignerActionUIStateChangeEventArgs(component, DesignerActionUIStateChangeType.Refresh));
        }

                /// <devdoc> 
        ///     This fires our DesignerActionsChanged event.
        /// </devdoc> 
        private void OnDesignerActionUIStateChange(DesignerActionUIStateChangeEventArgs e) { 
            if (designerActionUIStateChangedEventHandler != null) {
                designerActionUIStateChangedEventHandler(this, e); 
            }
        }

 
        public bool ShouldAutoShow(IComponent component) {
 
            // Check the designer options... 
            if (serviceProvider != null) {
                DesignerOptionService opts = serviceProvider.GetService(typeof(DesignerOptionService)) as DesignerOptionService; 
                if (opts != null) {
                    PropertyDescriptor p = opts.Options.Properties["ObjectBoundSmartTagAutoShow"];
                    if (p != null && p.PropertyType == typeof(bool) && !(bool)p.GetValue(null)) {
                        return false; 
                    }
                } 
            } 

            if(designerActionService != null) { 
                DesignerActionListCollection coll = designerActionService.GetComponentActions(component);
                if(coll != null && coll.Count > 0) {
                    for(int i = 0;i<coll.Count; i++) {
                        if(coll[i].AutoShow) { 
                            return true;
                        } 
                    } 
                }
            } 
            return false;
        }

    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
