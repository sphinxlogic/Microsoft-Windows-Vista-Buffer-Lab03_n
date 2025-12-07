//------------------------------------------------------------------------------ 
// <copyright file="ControlDesignerState.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;

    /// <devdoc> 
    /// Class to wrap the IComponentDesignerStateService
    /// to expose a simple indexer property. 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class ControlDesignerState { 
        private IDictionary _designerState;
        private IComponent _component;

        internal ControlDesignerState(IComponent component) { 
            _component = component;
        } 
 
        public object this[string key] {
            get { 
                if (_designerState == null) {
                    // Try to use designer state service
                    if ((_component != null) && (_component.Site != null)) {
                        IComponentDesignerStateService designerStateService = (IComponentDesignerStateService)_component.Site.GetService(typeof(IComponentDesignerStateService)); 
                        if (designerStateService != null) {
                            return designerStateService.GetState(_component, key); 
                        } 
                    }
 
                    // State service does not exist, use private hashtable instead
                    _designerState = new Hashtable();
                }
                return _designerState[key]; 
            }
            set { 
                if (_designerState == null) { 
                    // Try to use designer state service
                    if ((_component != null) && (_component.Site != null)) { 
                        IComponentDesignerStateService designerStateService = (IComponentDesignerStateService)_component.Site.GetService(typeof(IComponentDesignerStateService));
                        if (designerStateService != null) {
                            designerStateService.SetState(_component, key, value);
                            return; 
                        }
                    } 
 
                    // State service does not exist, use private hashtable instead
                    _designerState = new Hashtable(); 
                }
                _designerState[key] = value;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlDesignerState.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;

    /// <devdoc> 
    /// Class to wrap the IComponentDesignerStateService
    /// to expose a simple indexer property. 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class ControlDesignerState { 
        private IDictionary _designerState;
        private IComponent _component;

        internal ControlDesignerState(IComponent component) { 
            _component = component;
        } 
 
        public object this[string key] {
            get { 
                if (_designerState == null) {
                    // Try to use designer state service
                    if ((_component != null) && (_component.Site != null)) {
                        IComponentDesignerStateService designerStateService = (IComponentDesignerStateService)_component.Site.GetService(typeof(IComponentDesignerStateService)); 
                        if (designerStateService != null) {
                            return designerStateService.GetState(_component, key); 
                        } 
                    }
 
                    // State service does not exist, use private hashtable instead
                    _designerState = new Hashtable();
                }
                return _designerState[key]; 
            }
            set { 
                if (_designerState == null) { 
                    // Try to use designer state service
                    if ((_component != null) && (_component.Site != null)) { 
                        IComponentDesignerStateService designerStateService = (IComponentDesignerStateService)_component.Site.GetService(typeof(IComponentDesignerStateService));
                        if (designerStateService != null) {
                            designerStateService.SetState(_component, key, value);
                            return; 
                        }
                    } 
 
                    // State service does not exist, use private hashtable instead
                    _designerState = new Hashtable(); 
                }
                _designerState[key] = value;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
