namespace System.ComponentModel.Design.Serialization 
{
    using System;
    using System.Configuration;
    using System.Diagnostics; 
    using System.Design;
    using System.CodeDom; 
    using System.Collections; 
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.Windows.Forms;
    using System.Globalization;

 

    /// <include file='doc\ComponentCache.uex' path='docs/doc[@for="ComponentCache"]/*' /> 
    /// <devdoc> 
    ///  This class is used to cache serialized properties and events of components to speed-up Design to Code view switches
    /// </devdoc> 
    internal class ComponentCache : IDisposable
    {
        private Dictionary<object, Entry> cache;
        private IDesignerSerializationManager serManager; 
        private bool enabled = true;
 
 
        internal ComponentCache(IDesignerSerializationManager manager){
            serManager = manager; 
            IComponentChangeService cs = (IComponentChangeService)manager.GetService(typeof(IComponentChangeService));
            if (cs != null){
                cs.ComponentChanging  += new ComponentChangingEventHandler(this.OnComponentChanging);
                cs.ComponentChanged   += new ComponentChangedEventHandler(this.OnComponentChanged); 
                cs.ComponentRemoving  += new ComponentEventHandler(this.OnComponentRemove);
                cs.ComponentRemoved   += new ComponentEventHandler(this.OnComponentRemove); 
                cs.ComponentRename    += new ComponentRenameEventHandler(this.OnComponentRename); 
            }
 
            DesignerOptionService options = manager.GetService(typeof(DesignerOptionService)) as DesignerOptionService;

            object optionValue = null;
            if (options != null) { 
                PropertyDescriptor componentCacheProp = options.Options.Properties["UseOptimizedCodeGeneration"];
                if (componentCacheProp != null) { 
                    optionValue = componentCacheProp.GetValue(null); 
                }
 
                if (optionValue != null && optionValue is bool) {
                    enabled = (bool) optionValue;
                }
            } 
        }
 
        internal bool Enabled { 
            get {
                return enabled; 
            }
        }

 

        /// <include file='doc\ComponentCache.uex' path='docs/doc[@for="ComponentCache.this"]/*' /> 
        /// <devdoc> 
        ///     Access serialized Properties and events for the given component
        /// </devdoc> 
        internal Entry this[object component]
        {
            get {
 
                if (component == null)
                { 
                    throw new ArgumentNullException("component"); 
                }
 
                Entry result;

                if (cache != null && cache.TryGetValue(component, out result)) {
                    if (result != null && result.Valid && Enabled) { 
                        return result;
                    } 
                } 
                return null;
            } 
            set {
                if (cache == null && Enabled) {
                    cache = new Dictionary<object, Entry>();
                } 
                // it's a 1:1 relationship so we can go back from entry to
                // component (if it's not setup yet.. which should not happen, see ComponentCodeDomSerializer.cs::Serialize for more info) 
 
                if (cache != null && component is IComponent) {
                    if(value != null && value.Component == null) { 
                        value.Component = component;
                    }
                    cache[component] = value;
                } 
            }
        } 
 
        internal Entry GetEntryAll(object component) {
            Entry result = null; 
            if (cache != null && cache.TryGetValue(component, out result)) {
                return result;
            }
 
            return null;
        } 
 
        internal bool ContainsLocalName(string name) {
            if (cache == null) { 
                return false;
            }

            foreach(KeyValuePair<object, Entry> kvp in cache) { 
                List<string> localNames = kvp.Value.LocalNames;
                if (localNames != null && localNames.Contains(name)) { 
                    return true; 
                }
            } 

            return false;
        }
 
        public void Dispose()
        { 
            if (serManager != null) { 
                IComponentChangeService cs = (IComponentChangeService)serManager.GetService(typeof(IComponentChangeService));
 
                if (cs != null)
                {
                    cs.ComponentChanging  -= new ComponentChangingEventHandler(this.OnComponentChanging);
                    cs.ComponentChanged   -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                    cs.ComponentRemoving  -= new ComponentEventHandler(this.OnComponentRemove);
                    cs.ComponentRemoved   -= new ComponentEventHandler(this.OnComponentRemove); 
                    cs.ComponentRename    -= new ComponentRenameEventHandler(this.OnComponentRename); 
                }
            } 
        }

        private void OnComponentRename(object source, ComponentRenameEventArgs args) {
            // we might have a symbolic rename that has side effects beyond our control, 
            // so we don't have a choice but to clear the whole cache when a component gets renamed...
            if(cache!= null) { 
                cache.Clear(); 
            }
        } 

        private void OnComponentChanging(object source, ComponentChangingEventArgs ce) {
            if (cache != null) {
                if (ce.Component != null) { 
                    RemoveEntry(ce.Component);
 
                    if (!(ce.Component is IComponent) && serManager != null) { 
                        IReferenceService rs = serManager.GetService(typeof(IReferenceService)) as IReferenceService;
                        if (rs != null) { 
                            IComponent owningComp = rs.GetComponent(ce.Component);
                            if (owningComp != null) {
                                RemoveEntry(owningComp);
                            } 
                            else {
                                // Hmm. We were notified about an object change, but were unable to relate it 
                                // back to a component we know about. In this situation, we have no option 
                                // but to clear the whole cache, since we don't want serialization to miss
                                // something. See VSWhidbey #404813 for an example of what we would have missed. 
                                cache.Clear();
                            }
                        }
                    } 
                }
                else { 
                    cache.Clear(); 
                }
            } 
        }

        private void OnComponentChanged(object source, ComponentChangedEventArgs ce) {
            if (cache != null) { 
                if (ce.Component != null) {
                    RemoveEntry(ce.Component); 
 
                    if (!(ce.Component is IComponent) && serManager != null) {
                        IReferenceService rs = serManager.GetService(typeof(IReferenceService)) as IReferenceService; 
                        if (rs != null) {
                            IComponent owningComp = rs.GetComponent(ce.Component);
                            if (owningComp != null) {
                                RemoveEntry(owningComp); 
                            }
                            else { 
                                // Hmm. We were notified about an object change, but were unable to relate it 
                                // back to a component we know about. In this situation, we have no option
                                // but to clear the whole cache, since we don't want serialization to miss 
                                // something. See VSWhidbey #404813 for an example of what we would have missed.
                                cache.Clear();
                            }
                        } 
                    }
                } 
                else { 
                    cache.Clear();
                } 
            }
        }

        private void OnComponentRemove(object source, ComponentEventArgs ce) { 
           if (cache != null) {
               if (ce.Component != null && !(ce.Component is IExtenderProvider)) { 
                   RemoveEntry(ce.Component); 
               }
               else { 
                   cache.Clear();
               }
            }
        } 

        /// <devdoc> 
        ///     Helper to remove an entry from the cache. 
        /// </devdoc>
        internal void RemoveEntry(object component) { 
                Entry entry = null;

                if (cache != null && cache.TryGetValue(component, out entry)) {
                    if (entry.Tracking) { 
                        cache.Clear();
                        return; 
                    } 

                    cache.Remove(component); 

                    // Clear its dependencies, if any
                    if (entry.Dependencies != null) {
                        foreach(object parent in entry.Dependencies) { 
                            RemoveEntry(parent);
                        } 
                    } 

                } 
        }

        internal struct ResourceEntry {
            public bool ForceInvariant; 
            public bool EnsureInvariant;
            public bool ShouldSerializeValue; 
            public string Name; 
            public object Value;
            public PropertyDescriptor PropertyDescriptor; 
            public ExpressionContext ExpressionContext;
        }

        // A single cache entry 
        internal sealed class Entry {
            private ComponentCache cache; 
            private List<object> dependencies; 
            private List<string> localNames;
            private List<ResourceEntry> resources; 
            private List<ResourceEntry> metadata;
            private bool valid;
            private bool tracking;
 
            internal Entry(ComponentCache cache) {
                this.cache = cache; 
                valid = true; 
            }
 
            public object Component; // pointer back to the component that generated this entry
            public CodeStatementCollection Statements;

            public ICollection<ResourceEntry> Metadata { 
                get {
                    return metadata; 
                } 
            }
 
            public ICollection<ResourceEntry> Resources {
                get {
                    return resources;
                } 
            }
 
            public List<object> Dependencies { 
                get {
                    return dependencies; 
                }
            }

            internal List<string> LocalNames { 
                get {
                    return localNames; 
                } 
            }
 
            internal bool Valid {
                get {
                    return valid;
                } 
                set {
                    valid = value; 
                } 
            }
 
            internal bool Tracking {
                get {
                    return tracking;
                } 
                set {
                    tracking = value; 
                } 
            }
 		 
            internal void AddLocalName(string name) {
                if (localNames == null) {
                    localNames = new List<string>();
                } 

                localNames.Add(name); 
            } 

            public void AddDependency(object dep) { 
                if(dependencies == null) {
                    dependencies = new List<object>();
                }
 
                if (!dependencies.Contains(dep)) {
                    dependencies.Add(dep); 
                } 
            }
 
            public void AddMetadata(ResourceEntry re) {
                if (metadata == null) {
                    metadata = new List<ResourceEntry>();
                } 
                metadata.Add(re);
            } 
 
            public void AddResource(ResourceEntry re) {
                if (resources == null) { 
                    resources = new List<ResourceEntry>();
                }
                resources.Add(re);
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.ComponentModel.Design.Serialization 
{
    using System;
    using System.Configuration;
    using System.Diagnostics; 
    using System.Design;
    using System.CodeDom; 
    using System.Collections; 
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.Windows.Forms;
    using System.Globalization;

 

    /// <include file='doc\ComponentCache.uex' path='docs/doc[@for="ComponentCache"]/*' /> 
    /// <devdoc> 
    ///  This class is used to cache serialized properties and events of components to speed-up Design to Code view switches
    /// </devdoc> 
    internal class ComponentCache : IDisposable
    {
        private Dictionary<object, Entry> cache;
        private IDesignerSerializationManager serManager; 
        private bool enabled = true;
 
 
        internal ComponentCache(IDesignerSerializationManager manager){
            serManager = manager; 
            IComponentChangeService cs = (IComponentChangeService)manager.GetService(typeof(IComponentChangeService));
            if (cs != null){
                cs.ComponentChanging  += new ComponentChangingEventHandler(this.OnComponentChanging);
                cs.ComponentChanged   += new ComponentChangedEventHandler(this.OnComponentChanged); 
                cs.ComponentRemoving  += new ComponentEventHandler(this.OnComponentRemove);
                cs.ComponentRemoved   += new ComponentEventHandler(this.OnComponentRemove); 
                cs.ComponentRename    += new ComponentRenameEventHandler(this.OnComponentRename); 
            }
 
            DesignerOptionService options = manager.GetService(typeof(DesignerOptionService)) as DesignerOptionService;

            object optionValue = null;
            if (options != null) { 
                PropertyDescriptor componentCacheProp = options.Options.Properties["UseOptimizedCodeGeneration"];
                if (componentCacheProp != null) { 
                    optionValue = componentCacheProp.GetValue(null); 
                }
 
                if (optionValue != null && optionValue is bool) {
                    enabled = (bool) optionValue;
                }
            } 
        }
 
        internal bool Enabled { 
            get {
                return enabled; 
            }
        }

 

        /// <include file='doc\ComponentCache.uex' path='docs/doc[@for="ComponentCache.this"]/*' /> 
        /// <devdoc> 
        ///     Access serialized Properties and events for the given component
        /// </devdoc> 
        internal Entry this[object component]
        {
            get {
 
                if (component == null)
                { 
                    throw new ArgumentNullException("component"); 
                }
 
                Entry result;

                if (cache != null && cache.TryGetValue(component, out result)) {
                    if (result != null && result.Valid && Enabled) { 
                        return result;
                    } 
                } 
                return null;
            } 
            set {
                if (cache == null && Enabled) {
                    cache = new Dictionary<object, Entry>();
                } 
                // it's a 1:1 relationship so we can go back from entry to
                // component (if it's not setup yet.. which should not happen, see ComponentCodeDomSerializer.cs::Serialize for more info) 
 
                if (cache != null && component is IComponent) {
                    if(value != null && value.Component == null) { 
                        value.Component = component;
                    }
                    cache[component] = value;
                } 
            }
        } 
 
        internal Entry GetEntryAll(object component) {
            Entry result = null; 
            if (cache != null && cache.TryGetValue(component, out result)) {
                return result;
            }
 
            return null;
        } 
 
        internal bool ContainsLocalName(string name) {
            if (cache == null) { 
                return false;
            }

            foreach(KeyValuePair<object, Entry> kvp in cache) { 
                List<string> localNames = kvp.Value.LocalNames;
                if (localNames != null && localNames.Contains(name)) { 
                    return true; 
                }
            } 

            return false;
        }
 
        public void Dispose()
        { 
            if (serManager != null) { 
                IComponentChangeService cs = (IComponentChangeService)serManager.GetService(typeof(IComponentChangeService));
 
                if (cs != null)
                {
                    cs.ComponentChanging  -= new ComponentChangingEventHandler(this.OnComponentChanging);
                    cs.ComponentChanged   -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                    cs.ComponentRemoving  -= new ComponentEventHandler(this.OnComponentRemove);
                    cs.ComponentRemoved   -= new ComponentEventHandler(this.OnComponentRemove); 
                    cs.ComponentRename    -= new ComponentRenameEventHandler(this.OnComponentRename); 
                }
            } 
        }

        private void OnComponentRename(object source, ComponentRenameEventArgs args) {
            // we might have a symbolic rename that has side effects beyond our control, 
            // so we don't have a choice but to clear the whole cache when a component gets renamed...
            if(cache!= null) { 
                cache.Clear(); 
            }
        } 

        private void OnComponentChanging(object source, ComponentChangingEventArgs ce) {
            if (cache != null) {
                if (ce.Component != null) { 
                    RemoveEntry(ce.Component);
 
                    if (!(ce.Component is IComponent) && serManager != null) { 
                        IReferenceService rs = serManager.GetService(typeof(IReferenceService)) as IReferenceService;
                        if (rs != null) { 
                            IComponent owningComp = rs.GetComponent(ce.Component);
                            if (owningComp != null) {
                                RemoveEntry(owningComp);
                            } 
                            else {
                                // Hmm. We were notified about an object change, but were unable to relate it 
                                // back to a component we know about. In this situation, we have no option 
                                // but to clear the whole cache, since we don't want serialization to miss
                                // something. See VSWhidbey #404813 for an example of what we would have missed. 
                                cache.Clear();
                            }
                        }
                    } 
                }
                else { 
                    cache.Clear(); 
                }
            } 
        }

        private void OnComponentChanged(object source, ComponentChangedEventArgs ce) {
            if (cache != null) { 
                if (ce.Component != null) {
                    RemoveEntry(ce.Component); 
 
                    if (!(ce.Component is IComponent) && serManager != null) {
                        IReferenceService rs = serManager.GetService(typeof(IReferenceService)) as IReferenceService; 
                        if (rs != null) {
                            IComponent owningComp = rs.GetComponent(ce.Component);
                            if (owningComp != null) {
                                RemoveEntry(owningComp); 
                            }
                            else { 
                                // Hmm. We were notified about an object change, but were unable to relate it 
                                // back to a component we know about. In this situation, we have no option
                                // but to clear the whole cache, since we don't want serialization to miss 
                                // something. See VSWhidbey #404813 for an example of what we would have missed.
                                cache.Clear();
                            }
                        } 
                    }
                } 
                else { 
                    cache.Clear();
                } 
            }
        }

        private void OnComponentRemove(object source, ComponentEventArgs ce) { 
           if (cache != null) {
               if (ce.Component != null && !(ce.Component is IExtenderProvider)) { 
                   RemoveEntry(ce.Component); 
               }
               else { 
                   cache.Clear();
               }
            }
        } 

        /// <devdoc> 
        ///     Helper to remove an entry from the cache. 
        /// </devdoc>
        internal void RemoveEntry(object component) { 
                Entry entry = null;

                if (cache != null && cache.TryGetValue(component, out entry)) {
                    if (entry.Tracking) { 
                        cache.Clear();
                        return; 
                    } 

                    cache.Remove(component); 

                    // Clear its dependencies, if any
                    if (entry.Dependencies != null) {
                        foreach(object parent in entry.Dependencies) { 
                            RemoveEntry(parent);
                        } 
                    } 

                } 
        }

        internal struct ResourceEntry {
            public bool ForceInvariant; 
            public bool EnsureInvariant;
            public bool ShouldSerializeValue; 
            public string Name; 
            public object Value;
            public PropertyDescriptor PropertyDescriptor; 
            public ExpressionContext ExpressionContext;
        }

        // A single cache entry 
        internal sealed class Entry {
            private ComponentCache cache; 
            private List<object> dependencies; 
            private List<string> localNames;
            private List<ResourceEntry> resources; 
            private List<ResourceEntry> metadata;
            private bool valid;
            private bool tracking;
 
            internal Entry(ComponentCache cache) {
                this.cache = cache; 
                valid = true; 
            }
 
            public object Component; // pointer back to the component that generated this entry
            public CodeStatementCollection Statements;

            public ICollection<ResourceEntry> Metadata { 
                get {
                    return metadata; 
                } 
            }
 
            public ICollection<ResourceEntry> Resources {
                get {
                    return resources;
                } 
            }
 
            public List<object> Dependencies { 
                get {
                    return dependencies; 
                }
            }

            internal List<string> LocalNames { 
                get {
                    return localNames; 
                } 
            }
 
            internal bool Valid {
                get {
                    return valid;
                } 
                set {
                    valid = value; 
                } 
            }
 
            internal bool Tracking {
                get {
                    return tracking;
                } 
                set {
                    tracking = value; 
                } 
            }
 		 
            internal void AddLocalName(string name) {
                if (localNames == null) {
                    localNames = new List<string>();
                } 

                localNames.Add(name); 
            } 

            public void AddDependency(object dep) { 
                if(dependencies == null) {
                    dependencies = new List<object>();
                }
 
                if (!dependencies.Contains(dep)) {
                    dependencies.Add(dep); 
                } 
            }
 
            public void AddMetadata(ResourceEntry re) {
                if (metadata == null) {
                    metadata = new List<ResourceEntry>();
                } 
                metadata.Add(re);
            } 
 
            public void AddResource(ResourceEntry re) {
                if (resources == null) { 
                    resources = new List<ResourceEntry>();
                }
                resources.Add(re);
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
