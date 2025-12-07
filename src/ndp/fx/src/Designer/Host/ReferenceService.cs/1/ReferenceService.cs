//------------------------------------------------------------------------------ 
// <copyright file="ReferenceService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.ComponentModel.Design {
 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Globalization;
 
    /// <devdoc>
    ///     This service allows clients to work with all references on a form, not just 
    ///     the top-level sited components. 
    /// </devdoc>
    internal sealed class ReferenceService : IReferenceService, IDisposable { 

        private static readonly Attribute[] _attributes = new Attribute[] {DesignerSerializationVisibilityAttribute.Content};

        private IServiceProvider  _provider;            // service provider we use to get to other services 
        private ArrayList         _addedComponents;     // list of newly added components
        private ArrayList         _removedComponents;   // list of newly removed components 
        private ArrayList         _references;          // our current list of references 
        private bool              _populating;
 
        /// <devdoc>
        ///     Constructs the ReferenceService.
        /// </devdoc>
        internal ReferenceService(IServiceProvider provider) { 
            _provider = provider;
        } 
 
        /// <devdoc>
        ///     Creates an entry for a top-level component and it's children. 
        /// </devdoc>
        private void CreateReferences(IComponent component) {
            CreateReferences(string.Empty, component, component);
        } 

        /// <devdoc> 
        ///     Recursively creates references for namespaced objects. 
        /// </devdoc>
        private void CreateReferences(string trailingName, object reference, IComponent sitedComponent) { 

            if (object.ReferenceEquals(reference, null)) {
                return;
            } 

            _references.Add(new ReferenceHolder(trailingName, reference, sitedComponent)); 
 
            foreach(PropertyDescriptor property in TypeDescriptor.GetProperties(reference, _attributes)) {
                if (property.IsReadOnly) { 
                    CreateReferences(
                        string.Format(CultureInfo.CurrentCulture, "{0}.{1}", trailingName, property.Name),
                        property.GetValue(reference),
                        sitedComponent); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Demand populates the _references variable.
        /// </devdoc>
        private void EnsureReferences() {
 
            // If the references are null, create them for the first time and connect
            // up our events to listen to changes to the container.  Otherwise, check to 
            // see if the added or removed lists contain anything for us to sync up. 
            //
            if (_references == null) { 

                if (_provider == null) {
                    throw new ObjectDisposedException("IReferenceService");
                } 

                IComponentChangeService cs = _provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                Debug.Assert(cs != null, "Reference service relies on IComponentChangeService"); 
                if (cs != null) {
                    cs.ComponentAdded += new ComponentEventHandler(this.OnComponentAdded); 
                    cs.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved);
                    cs.ComponentRename += new ComponentRenameEventHandler(this.OnComponentRename);
                }
 
                IContainer container = _provider.GetService(typeof(IContainer)) as IContainer;
                if (container == null) { 
                    Debug.Fail("Reference service cannot operate without IContainer"); 
                    throw new InvalidOperationException();
                } 

                _references = new ArrayList(container.Components.Count);

                foreach(IComponent component in container.Components) { 
                    CreateReferences(component);
                } 
            } 
            else  if (!_populating) {
                _populating = true; 
                try {
                    if (_addedComponents != null && _addedComponents.Count > 0) {

                        // There is a possibility that this component already exists. 
                        // If it does, just remove it first and then re-add it.
                        // 
                        foreach (IComponent ic in _addedComponents) { 
                            RemoveReferences(ic);
                            CreateReferences(ic); 
                        }

                        _addedComponents.Clear();
                    } 

                    if (_removedComponents != null && _removedComponents.Count > 0) { 
                        foreach (IComponent ic in _removedComponents) { 
                            RemoveReferences(ic);
                        } 

                        _removedComponents.Clear();
                    }
                } 
                finally {
                    _populating = false; 
                } 
            }
        } 

        /// <devdoc>
        ///     Listens for component additions to find all the references it contributes.
        /// </devdoc> 
        private void OnComponentAdded(object sender, ComponentEventArgs cevent) {
 
            if (_addedComponents == null) { 
                _addedComponents = new ArrayList();
            } 

            IComponent compAdded = cevent.Component;
            if (!(compAdded.Site is INestedSite))
            { 
                _addedComponents.Add(compAdded);
 
                if (_removedComponents != null) { 
                    _removedComponents.Remove(compAdded);
                } 
            }
        }

        /// <devdoc> 
        ///     Listens for component removes to delete all the references it holds.
        /// </devdoc> 
        private void OnComponentRemoved(object sender, ComponentEventArgs cevent) { 

            if (_removedComponents == null) { 
                _removedComponents = new ArrayList();
            }

            IComponent compRemoved = cevent.Component; 
            if (!(compRemoved.Site is INestedSite))
            { 
                _removedComponents.Add(compRemoved); 

                if (_addedComponents != null) { 
                    _addedComponents.Remove(compRemoved);
                }
            }
        } 

        /// <devdoc> 
        ///     Listens for component removes to delete all the references it holds. 
        /// </devdoc>
        private void OnComponentRename(object sender, ComponentRenameEventArgs cevent) { 
            foreach (ReferenceHolder reference in _references) {
                if (object.ReferenceEquals(reference.SitedComponent, cevent.Component)) {
                    reference.ResetName();
                    return; 
                }
            } 
        } 

        /// <devdoc> 
        ///     Removes all the references that this component owns.
        /// </devdoc>
        private void RemoveReferences(IComponent component) {
            if (_references != null) { 
                int size = _references.Count;
                for (int i = size - 1; i >= 0; i--) { 
                    if (object.ReferenceEquals(((ReferenceHolder)_references[i]).SitedComponent, component)) { 
                        _references.RemoveAt(i);
                    } 
                }
            }
        }
 
        /// <devdoc>
        ///     Cleanup and detach from our events. 
        /// </devdoc> 
        void IDisposable.Dispose() {
 
            if (_references != null && _provider != null) {

                IComponentChangeService cs = _provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                if (cs != null) { 
                    cs.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded);
                    cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved); 
                    cs.ComponentRename -= new ComponentRenameEventHandler(this.OnComponentRename); 
                }
 
                _references = null;
                _provider = null;
            }
        } 

        /// <devdoc> 
        ///     Finds the sited component for a given reference, returning null if not found. 
        /// </devdoc>
        IComponent IReferenceService.GetComponent(object reference) { 

            if (object.ReferenceEquals(reference, null)) {
                throw new ArgumentNullException("reference");
            } 

            EnsureReferences(); 
 
            foreach(ReferenceHolder holder in _references) {
                if (object.ReferenceEquals(holder.Reference, reference)) { 
                    return holder.SitedComponent;
                }
            }
 
            return null;
        } 
 
        /// <devdoc>
        ///     Finds name for a given reference, returning null if not found. 
        /// </devdoc>
        string IReferenceService.GetName(object reference) {

            if (object.ReferenceEquals(reference, null)) { 
                throw new ArgumentNullException("reference");
            } 
 
            EnsureReferences();
 
            foreach(ReferenceHolder holder in _references) {
                if (object.ReferenceEquals(holder.Reference, reference)) {
                    return holder.Name;
                } 
            }
 
            return null; 
        }
 
        /// <devdoc>
        ///     Finds a reference with the given name, returning null if not found.
        /// </devdoc>
        object IReferenceService.GetReference(string name) { 

            if (name == null) { 
                throw new ArgumentNullException("name"); 
            }
 
            EnsureReferences();

            foreach(ReferenceHolder holder in _references) {
                if (string.Equals(holder.Name, name, StringComparison.OrdinalIgnoreCase)) { 
                    return holder.Reference;
                } 
            } 

            return null; 
        }

        /// <devdoc>
        ///     Returns all references available in this designer. 
        /// </devdoc>
        object[] IReferenceService.GetReferences() { 
            EnsureReferences(); 

            object[] references = new object[_references.Count]; 

            for(int i = 0; i < references.Length; i++) {
                references[i] = ((ReferenceHolder)_references[i]).Reference;
            } 

            return references; 
        } 

        /// <devdoc> 
        ///     Returns all references available in this designer that are assignable to the given type.
        /// </devdoc>
        object[] IReferenceService.GetReferences(Type baseType) {
 
            if (baseType == null) {
                throw new ArgumentNullException("baseType"); 
            } 

            EnsureReferences(); 

            ArrayList results = new ArrayList(_references.Count);

            foreach(ReferenceHolder holder in _references) { 
                object reference = holder.Reference;
                if (baseType.IsAssignableFrom(reference.GetType())) { 
                    results.Add(reference); 
                }
            } 

            object[] references = new object[results.Count];
            results.CopyTo(references, 0);
            return references; 
        }
 
        /// <devdoc> 
        ///     The class that holds the information about a reference.
        /// </devdoc> 
        private sealed class ReferenceHolder {

            private string      _trailingName;
            private object      _reference; 
            private IComponent  _sitedComponent;
            private string      _fullName; 
 
            /// <devdoc>
            ///     Creates a new reference holder. 
            /// </devdoc>
            internal ReferenceHolder(string trailingName, object reference, IComponent sitedComponent) {

                _trailingName = trailingName; 
                _reference = reference;
                _sitedComponent = sitedComponent; 
 
                Debug.Assert(trailingName != null, "Expected a trailing name");
                Debug.Assert(reference != null, "Expected a reference"); 

                #if DEBUG
                Debug.Assert(sitedComponent != null, "Expected a sited component");
                if (sitedComponent != null) Debug.Assert(sitedComponent.Site != null, "Sited component is not really sited: " + sitedComponent.ToString()); 
                if (sitedComponent != null) Debug.Assert(TypeDescriptor.GetComponentName(sitedComponent) != null, "Sited component has no name: " + sitedComponent.ToString());
                #endif // DEBUG 
            } 

            /// <devdoc> 
            ///     Resets the name of this reference holder.  It will be re-aquired on demand
            /// </devdoc>
            internal void ResetName() {
                _fullName = null; 
            }
 
            /// <devdoc> 
            ///     The name of the reference we are holding.
            /// </devdoc> 
            internal string Name {
                get {
                    if (_fullName == null) {
                        if (_sitedComponent != null) { 
                            string siteName = TypeDescriptor.GetComponentName(_sitedComponent);
                            if (siteName != null) { 
                                _fullName = string.Format(CultureInfo.CurrentCulture, "{0}{1}", siteName, _trailingName); 
                            }
                        } 

                        if (_fullName == null) {
                            _fullName = string.Empty;
                            #if DEBUG 
                            if (_sitedComponent != null) Debug.Assert(_sitedComponent.Site != null, "Sited component is not really sited: " + _sitedComponent.ToString());
                            if (_sitedComponent != null) Debug.Assert(TypeDescriptor.GetComponentName(_sitedComponent) != null, "Sited component has no name: " + _sitedComponent.ToString()); 
                            #endif // DEBUG 
                        }
                    } 
                    return _fullName;
                }
            }
 
            /// <devdoc>
            ///     The reference we are holding. 
            /// </devdoc> 
            internal object Reference {
                get { 
                    return _reference;
                }
            }
 
            /// <devdoc>
            ///     The sited component associated with this reference. 
            /// </devdoc> 
            internal IComponent SitedComponent {
                get { 
                    return _sitedComponent;
                }
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ReferenceService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.ComponentModel.Design {
 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Globalization;
 
    /// <devdoc>
    ///     This service allows clients to work with all references on a form, not just 
    ///     the top-level sited components. 
    /// </devdoc>
    internal sealed class ReferenceService : IReferenceService, IDisposable { 

        private static readonly Attribute[] _attributes = new Attribute[] {DesignerSerializationVisibilityAttribute.Content};

        private IServiceProvider  _provider;            // service provider we use to get to other services 
        private ArrayList         _addedComponents;     // list of newly added components
        private ArrayList         _removedComponents;   // list of newly removed components 
        private ArrayList         _references;          // our current list of references 
        private bool              _populating;
 
        /// <devdoc>
        ///     Constructs the ReferenceService.
        /// </devdoc>
        internal ReferenceService(IServiceProvider provider) { 
            _provider = provider;
        } 
 
        /// <devdoc>
        ///     Creates an entry for a top-level component and it's children. 
        /// </devdoc>
        private void CreateReferences(IComponent component) {
            CreateReferences(string.Empty, component, component);
        } 

        /// <devdoc> 
        ///     Recursively creates references for namespaced objects. 
        /// </devdoc>
        private void CreateReferences(string trailingName, object reference, IComponent sitedComponent) { 

            if (object.ReferenceEquals(reference, null)) {
                return;
            } 

            _references.Add(new ReferenceHolder(trailingName, reference, sitedComponent)); 
 
            foreach(PropertyDescriptor property in TypeDescriptor.GetProperties(reference, _attributes)) {
                if (property.IsReadOnly) { 
                    CreateReferences(
                        string.Format(CultureInfo.CurrentCulture, "{0}.{1}", trailingName, property.Name),
                        property.GetValue(reference),
                        sitedComponent); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Demand populates the _references variable.
        /// </devdoc>
        private void EnsureReferences() {
 
            // If the references are null, create them for the first time and connect
            // up our events to listen to changes to the container.  Otherwise, check to 
            // see if the added or removed lists contain anything for us to sync up. 
            //
            if (_references == null) { 

                if (_provider == null) {
                    throw new ObjectDisposedException("IReferenceService");
                } 

                IComponentChangeService cs = _provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                Debug.Assert(cs != null, "Reference service relies on IComponentChangeService"); 
                if (cs != null) {
                    cs.ComponentAdded += new ComponentEventHandler(this.OnComponentAdded); 
                    cs.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved);
                    cs.ComponentRename += new ComponentRenameEventHandler(this.OnComponentRename);
                }
 
                IContainer container = _provider.GetService(typeof(IContainer)) as IContainer;
                if (container == null) { 
                    Debug.Fail("Reference service cannot operate without IContainer"); 
                    throw new InvalidOperationException();
                } 

                _references = new ArrayList(container.Components.Count);

                foreach(IComponent component in container.Components) { 
                    CreateReferences(component);
                } 
            } 
            else  if (!_populating) {
                _populating = true; 
                try {
                    if (_addedComponents != null && _addedComponents.Count > 0) {

                        // There is a possibility that this component already exists. 
                        // If it does, just remove it first and then re-add it.
                        // 
                        foreach (IComponent ic in _addedComponents) { 
                            RemoveReferences(ic);
                            CreateReferences(ic); 
                        }

                        _addedComponents.Clear();
                    } 

                    if (_removedComponents != null && _removedComponents.Count > 0) { 
                        foreach (IComponent ic in _removedComponents) { 
                            RemoveReferences(ic);
                        } 

                        _removedComponents.Clear();
                    }
                } 
                finally {
                    _populating = false; 
                } 
            }
        } 

        /// <devdoc>
        ///     Listens for component additions to find all the references it contributes.
        /// </devdoc> 
        private void OnComponentAdded(object sender, ComponentEventArgs cevent) {
 
            if (_addedComponents == null) { 
                _addedComponents = new ArrayList();
            } 

            IComponent compAdded = cevent.Component;
            if (!(compAdded.Site is INestedSite))
            { 
                _addedComponents.Add(compAdded);
 
                if (_removedComponents != null) { 
                    _removedComponents.Remove(compAdded);
                } 
            }
        }

        /// <devdoc> 
        ///     Listens for component removes to delete all the references it holds.
        /// </devdoc> 
        private void OnComponentRemoved(object sender, ComponentEventArgs cevent) { 

            if (_removedComponents == null) { 
                _removedComponents = new ArrayList();
            }

            IComponent compRemoved = cevent.Component; 
            if (!(compRemoved.Site is INestedSite))
            { 
                _removedComponents.Add(compRemoved); 

                if (_addedComponents != null) { 
                    _addedComponents.Remove(compRemoved);
                }
            }
        } 

        /// <devdoc> 
        ///     Listens for component removes to delete all the references it holds. 
        /// </devdoc>
        private void OnComponentRename(object sender, ComponentRenameEventArgs cevent) { 
            foreach (ReferenceHolder reference in _references) {
                if (object.ReferenceEquals(reference.SitedComponent, cevent.Component)) {
                    reference.ResetName();
                    return; 
                }
            } 
        } 

        /// <devdoc> 
        ///     Removes all the references that this component owns.
        /// </devdoc>
        private void RemoveReferences(IComponent component) {
            if (_references != null) { 
                int size = _references.Count;
                for (int i = size - 1; i >= 0; i--) { 
                    if (object.ReferenceEquals(((ReferenceHolder)_references[i]).SitedComponent, component)) { 
                        _references.RemoveAt(i);
                    } 
                }
            }
        }
 
        /// <devdoc>
        ///     Cleanup and detach from our events. 
        /// </devdoc> 
        void IDisposable.Dispose() {
 
            if (_references != null && _provider != null) {

                IComponentChangeService cs = _provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                if (cs != null) { 
                    cs.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded);
                    cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved); 
                    cs.ComponentRename -= new ComponentRenameEventHandler(this.OnComponentRename); 
                }
 
                _references = null;
                _provider = null;
            }
        } 

        /// <devdoc> 
        ///     Finds the sited component for a given reference, returning null if not found. 
        /// </devdoc>
        IComponent IReferenceService.GetComponent(object reference) { 

            if (object.ReferenceEquals(reference, null)) {
                throw new ArgumentNullException("reference");
            } 

            EnsureReferences(); 
 
            foreach(ReferenceHolder holder in _references) {
                if (object.ReferenceEquals(holder.Reference, reference)) { 
                    return holder.SitedComponent;
                }
            }
 
            return null;
        } 
 
        /// <devdoc>
        ///     Finds name for a given reference, returning null if not found. 
        /// </devdoc>
        string IReferenceService.GetName(object reference) {

            if (object.ReferenceEquals(reference, null)) { 
                throw new ArgumentNullException("reference");
            } 
 
            EnsureReferences();
 
            foreach(ReferenceHolder holder in _references) {
                if (object.ReferenceEquals(holder.Reference, reference)) {
                    return holder.Name;
                } 
            }
 
            return null; 
        }
 
        /// <devdoc>
        ///     Finds a reference with the given name, returning null if not found.
        /// </devdoc>
        object IReferenceService.GetReference(string name) { 

            if (name == null) { 
                throw new ArgumentNullException("name"); 
            }
 
            EnsureReferences();

            foreach(ReferenceHolder holder in _references) {
                if (string.Equals(holder.Name, name, StringComparison.OrdinalIgnoreCase)) { 
                    return holder.Reference;
                } 
            } 

            return null; 
        }

        /// <devdoc>
        ///     Returns all references available in this designer. 
        /// </devdoc>
        object[] IReferenceService.GetReferences() { 
            EnsureReferences(); 

            object[] references = new object[_references.Count]; 

            for(int i = 0; i < references.Length; i++) {
                references[i] = ((ReferenceHolder)_references[i]).Reference;
            } 

            return references; 
        } 

        /// <devdoc> 
        ///     Returns all references available in this designer that are assignable to the given type.
        /// </devdoc>
        object[] IReferenceService.GetReferences(Type baseType) {
 
            if (baseType == null) {
                throw new ArgumentNullException("baseType"); 
            } 

            EnsureReferences(); 

            ArrayList results = new ArrayList(_references.Count);

            foreach(ReferenceHolder holder in _references) { 
                object reference = holder.Reference;
                if (baseType.IsAssignableFrom(reference.GetType())) { 
                    results.Add(reference); 
                }
            } 

            object[] references = new object[results.Count];
            results.CopyTo(references, 0);
            return references; 
        }
 
        /// <devdoc> 
        ///     The class that holds the information about a reference.
        /// </devdoc> 
        private sealed class ReferenceHolder {

            private string      _trailingName;
            private object      _reference; 
            private IComponent  _sitedComponent;
            private string      _fullName; 
 
            /// <devdoc>
            ///     Creates a new reference holder. 
            /// </devdoc>
            internal ReferenceHolder(string trailingName, object reference, IComponent sitedComponent) {

                _trailingName = trailingName; 
                _reference = reference;
                _sitedComponent = sitedComponent; 
 
                Debug.Assert(trailingName != null, "Expected a trailing name");
                Debug.Assert(reference != null, "Expected a reference"); 

                #if DEBUG
                Debug.Assert(sitedComponent != null, "Expected a sited component");
                if (sitedComponent != null) Debug.Assert(sitedComponent.Site != null, "Sited component is not really sited: " + sitedComponent.ToString()); 
                if (sitedComponent != null) Debug.Assert(TypeDescriptor.GetComponentName(sitedComponent) != null, "Sited component has no name: " + sitedComponent.ToString());
                #endif // DEBUG 
            } 

            /// <devdoc> 
            ///     Resets the name of this reference holder.  It will be re-aquired on demand
            /// </devdoc>
            internal void ResetName() {
                _fullName = null; 
            }
 
            /// <devdoc> 
            ///     The name of the reference we are holding.
            /// </devdoc> 
            internal string Name {
                get {
                    if (_fullName == null) {
                        if (_sitedComponent != null) { 
                            string siteName = TypeDescriptor.GetComponentName(_sitedComponent);
                            if (siteName != null) { 
                                _fullName = string.Format(CultureInfo.CurrentCulture, "{0}{1}", siteName, _trailingName); 
                            }
                        } 

                        if (_fullName == null) {
                            _fullName = string.Empty;
                            #if DEBUG 
                            if (_sitedComponent != null) Debug.Assert(_sitedComponent.Site != null, "Sited component is not really sited: " + _sitedComponent.ToString());
                            if (_sitedComponent != null) Debug.Assert(TypeDescriptor.GetComponentName(_sitedComponent) != null, "Sited component has no name: " + _sitedComponent.ToString()); 
                            #endif // DEBUG 
                        }
                    } 
                    return _fullName;
                }
            }
 
            /// <devdoc>
            ///     The reference we are holding. 
            /// </devdoc> 
            internal object Reference {
                get { 
                    return _reference;
                }
            }
 
            /// <devdoc>
            ///     The sited component associated with this reference. 
            /// </devdoc> 
            internal IComponent SitedComponent {
                get { 
                    return _sitedComponent;
                }
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
