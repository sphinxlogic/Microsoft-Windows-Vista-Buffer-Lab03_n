using System; 
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Design; 

namespace System.ComponentModel.Design { 
 
    /// <devdoc>
    ///     A service container that supports "fixed" services.  Fixed 
    ///     services cannot be removed.
    /// </devdoc>
    internal sealed class DesignSurfaceServiceContainer : ServiceContainer {
 
        private Hashtable _fixedServices;
 
        /// <summary> 
        ///     We always add ourselves as a service.
        /// </summary> 
        internal DesignSurfaceServiceContainer(IServiceProvider parentProvider) : base(parentProvider) {
            AddFixedService(typeof(DesignSurfaceServiceContainer), this);
        }
 
        /// <devdoc>
        ///     Removes the given service type from the service container. 
        /// </devdoc> 
        internal void AddFixedService(Type serviceType, object serviceInstance) {
            AddService(serviceType, serviceInstance); 
            if (_fixedServices == null) {
                _fixedServices = new Hashtable();
            }
            _fixedServices[serviceType] = serviceType; 
        }
 
        /// <devdoc> 
        ///     Removes a previously added fixed service.
        /// </devdoc> 
        internal void RemoveFixedService(Type serviceType) {
            if (_fixedServices != null) {
                _fixedServices.Remove(serviceType);
            } 
            RemoveService(serviceType);
        } 
 
        /// <devdoc>
        ///     Removes the given service type from the service container.  Throws 
        ///     an exception if the service is fixed.
        /// </devdoc>
        public override void RemoveService(Type serviceType, bool promote) {
            if (serviceType != null && _fixedServices != null && _fixedServices.ContainsKey(serviceType)) { 
                throw new InvalidOperationException(SR.GetString(SR.DesignSurfaceServiceIsFixed, serviceType.Name));
            } 
            base.RemoveService(serviceType, promote); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Design; 

namespace System.ComponentModel.Design { 
 
    /// <devdoc>
    ///     A service container that supports "fixed" services.  Fixed 
    ///     services cannot be removed.
    /// </devdoc>
    internal sealed class DesignSurfaceServiceContainer : ServiceContainer {
 
        private Hashtable _fixedServices;
 
        /// <summary> 
        ///     We always add ourselves as a service.
        /// </summary> 
        internal DesignSurfaceServiceContainer(IServiceProvider parentProvider) : base(parentProvider) {
            AddFixedService(typeof(DesignSurfaceServiceContainer), this);
        }
 
        /// <devdoc>
        ///     Removes the given service type from the service container. 
        /// </devdoc> 
        internal void AddFixedService(Type serviceType, object serviceInstance) {
            AddService(serviceType, serviceInstance); 
            if (_fixedServices == null) {
                _fixedServices = new Hashtable();
            }
            _fixedServices[serviceType] = serviceType; 
        }
 
        /// <devdoc> 
        ///     Removes a previously added fixed service.
        /// </devdoc> 
        internal void RemoveFixedService(Type serviceType) {
            if (_fixedServices != null) {
                _fixedServices.Remove(serviceType);
            } 
            RemoveService(serviceType);
        } 
 
        /// <devdoc>
        ///     Removes the given service type from the service container.  Throws 
        ///     an exception if the service is fixed.
        /// </devdoc>
        public override void RemoveService(Type serviceType, bool promote) {
            if (serviceType != null && _fixedServices != null && _fixedServices.ContainsKey(serviceType)) { 
                throw new InvalidOperationException(SR.GetString(SR.DesignSurfaceServiceIsFixed, serviceType.Name));
            } 
            base.RemoveService(serviceType, promote); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
