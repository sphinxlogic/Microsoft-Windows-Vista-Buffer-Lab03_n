using System; 
using System.ComponentModel;
using System.ComponentModel.Design;

namespace System.Web.UI.Design.WebControls { 
    internal sealed class TypeDescriptorContext : ITypeDescriptorContext{
        private IDesignerHost _designerHost; 
        private PropertyDescriptor _propDesc; 
        private object _instance;
 
        public TypeDescriptorContext(IDesignerHost designerHost, PropertyDescriptor propDesc, object instance) {
            _designerHost = designerHost;
            _propDesc = propDesc;
            _instance = instance; 
        }
 
        private IComponentChangeService ComponentChangeService { 
            get {
                return (IComponentChangeService)_designerHost.GetService(typeof(IComponentChangeService)); 
            }
        }

        public IContainer Container { 
            get {
                return (IContainer)_designerHost.GetService(typeof(IContainer)); 
            } 
        }
 
        public object Instance {
            get {
                return _instance;
            } 
        }
 
        public PropertyDescriptor PropertyDescriptor { 
            get {
                return _propDesc; 
            }
        }

        public object GetService(Type serviceType) { 
            return _designerHost.GetService(serviceType);
        } 
 
        public bool OnComponentChanging() {
            if (ComponentChangeService != null) { 
                try {
                    ComponentChangeService.OnComponentChanging(_instance, _propDesc);
                }
                catch (CheckoutException ce) { 
                    if (ce == CheckoutException.Canceled) {
                        return false; 
                    } 
                    throw ce;
                } 
            }

            return true;
        } 

        public void OnComponentChanged() { 
            if (ComponentChangeService != null) { 
                ComponentChangeService.OnComponentChanged(_instance, _propDesc, null, null);
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.ComponentModel;
using System.ComponentModel.Design;

namespace System.Web.UI.Design.WebControls { 
    internal sealed class TypeDescriptorContext : ITypeDescriptorContext{
        private IDesignerHost _designerHost; 
        private PropertyDescriptor _propDesc; 
        private object _instance;
 
        public TypeDescriptorContext(IDesignerHost designerHost, PropertyDescriptor propDesc, object instance) {
            _designerHost = designerHost;
            _propDesc = propDesc;
            _instance = instance; 
        }
 
        private IComponentChangeService ComponentChangeService { 
            get {
                return (IComponentChangeService)_designerHost.GetService(typeof(IComponentChangeService)); 
            }
        }

        public IContainer Container { 
            get {
                return (IContainer)_designerHost.GetService(typeof(IContainer)); 
            } 
        }
 
        public object Instance {
            get {
                return _instance;
            } 
        }
 
        public PropertyDescriptor PropertyDescriptor { 
            get {
                return _propDesc; 
            }
        }

        public object GetService(Type serviceType) { 
            return _designerHost.GetService(serviceType);
        } 
 
        public bool OnComponentChanging() {
            if (ComponentChangeService != null) { 
                try {
                    ComponentChangeService.OnComponentChanging(_instance, _propDesc);
                }
                catch (CheckoutException ce) { 
                    if (ce == CheckoutException.Canceled) {
                        return false; 
                    } 
                    throw ce;
                } 
            }

            return true;
        } 

        public void OnComponentChanged() { 
            if (ComponentChangeService != null) { 
                ComponentChangeService.OnComponentChanged(_instance, _propDesc, null, null);
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
