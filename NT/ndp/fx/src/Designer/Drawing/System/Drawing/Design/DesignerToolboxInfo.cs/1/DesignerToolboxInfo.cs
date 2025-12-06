//------------------------------------------------------------------------------ 
// <copyright file="DesignerToolboxInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Drawing.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization;
    using System.IO; 
    using System.Reflection; 
    using System.Runtime.Remoting.Lifetime;
    using System.Runtime.Serialization; 
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Windows.Forms;

    /// <devdoc> 
    ///     This object lives as a service on a designer host
    ///     and provides per-designer toolbox information, such 
    ///     as the filter and current toolbox user. 
    /// </devdoc>
    internal sealed class DesignerToolboxInfo : IDisposable { 

        private ToolboxService              _toolboxService;
        private IDesignerHost               _host;
        private ISelectionService           _selectionService; 
        private ArrayList                   _filter;
        private IDesigner                   _filterDesigner; 
        private IToolboxUser                _toolboxUser; 
        private Hashtable                   _attributeHash;
 
        /// <devdoc>
        ///     Creates a new toolbox designer state object.
        /// </devdoc>
        internal DesignerToolboxInfo(ToolboxService toolboxService, IDesignerHost host) { 

            _toolboxService = toolboxService; 
            _host = host; 
            _selectionService = host.GetService(typeof(ISelectionService)) as ISelectionService;
 
            if (_selectionService != null) {
                _selectionService.SelectionChanged += new EventHandler(OnSelectionChanged);
            }
 
            if(_host.RootComponent != null) {
                _host.RootComponent.Disposed += new EventHandler(OnDesignerDisposed); 
            } 

            TypeDescriptor.Refreshed += new RefreshEventHandler(OnTypeDescriptorRefresh); 

        }

        /// <devdoc> 
        ///     Returns the designer host this toolbox state object
        ///     is associated with. 
        /// </devdoc> 
        internal IDesignerHost DesignerHost {
            get { 
                return _host;
            }
        }
 
        /// <devdoc>
        ///     Returns the current toolbox item filter. 
        /// </devdoc> 
        internal ICollection Filter {
            get { 
                if (_filter == null) {
                    Update();
                }
 
                return _filter;
            } 
        } 

        /// <devdoc> 
        ///     Returns the current toolbox user.
        /// </devdoc>
        internal IToolboxUser ToolboxUser {
            get { 
                if (_toolboxUser == null) {
                    Update(); 
                } 
                return _toolboxUser;
            } 
        }

        private void OnTypeDescriptorRefresh(RefreshEventArgs r) {
            if (r.ComponentChanged == _filterDesigner) { 
                _filter = null;
                _filterDesigner = null; 
            } 
        }
 
        /// <devdoc>
        ///     The GetDesignerAttributes method is used to retrieve a collection of attributes for
        ///     the specified designer.  If the designer implements ITreeDesigner, this method will
        ///     also collect attributes for all parent designers.  Attributes are merged such that 
        ///     child attributes replace matching parent attributes.
        /// </devdoc> 
        public AttributeCollection GetDesignerAttributes(IDesigner designer) { 

            if (designer == null) { 
                throw new ArgumentNullException("designer");
            }

            AttributeCollection attributes; 

            // We don't store the merged attribute set.  If we 
            // did, we would have to also store what designer 
            // the merge was for, and then we would have to worry
            // about designer GC and cleaning out our table.  That 
            // would be a pain.  But we do re-use the same
            // hashtable over and over so we don't have to "new"
            // one each time.  This keeps the buckets preallocated.
            // 
            if (_attributeHash == null) {
                _attributeHash = new Hashtable(); 
            } 
            else {
                _attributeHash.Clear(); 
            }

            // If the designer is a designer tree, then we rely
            // on the designer's "treeness" to get to the root. 
            // If it's not, then we include the root designer
            // directly. 
 
            if (!(designer is ITreeDesigner)) {
                IComponent root = _host.RootComponent; 
                if (root != null) {
                    RecurseDesignerTree(_host.GetDesigner(root), _attributeHash);
                }
            } 

            RecurseDesignerTree(designer, _attributeHash); 
 
            Attribute[] attributeArray = new Attribute[_attributeHash.Values.Count];
            _attributeHash.Values.CopyTo(attributeArray, 0); 
            attributes = new AttributeCollection(attributeArray);
            return attributes;
        }
 
        /// <devdoc>
        ///     Recurive algorithm to walk the designer tree for attributes. 
        /// </devdoc> 
        private void RecurseDesignerTree(IDesigner designer, Hashtable table) {
            ITreeDesigner tree = designer as ITreeDesigner; 
            if (tree != null) {
                IDesigner parent = tree.Parent;
                if (parent != null) {
                    RecurseDesignerTree(parent, table); 
                }
            } 
 
            foreach(Attribute a in TypeDescriptor.GetAttributes(designer)) {
                table[a.TypeId] = a; 
            }
        }

 
        private void OnDesignerDisposed(object sender, EventArgs e) {
            _host.RemoveService(typeof(DesignerToolboxInfo)); 
        } 

 

        /// <devdoc>
        ///     Called when the designer selection changes.  This recomputes the filter and
        ///     toolbox user data based on the primary selection.  If this is the 
        ///     currently active designer it will notify the toolbox service that
        ///     the filter has changed.  The toolbox service will raise its FilterChanged 
        ///     event. 
        /// </devdoc>
        private void OnSelectionChanged(object sender, EventArgs e) { 
            if (Update()) {
                _toolboxService.OnDesignerInfoChanged(this);
            }
        } 

        /// <devdoc> 
        ///     Called to recalculate our designer filter and toolbox 
        ///     user.  This will return true if the state has changed
        ///     in such a way as to require a requery of the toolbox item 
        ///     enabling.  For exapmle, if the toolbox user changes, but
        ///     the filter didn't change (and the filter didn't specify
        ///     "Custom"), then this will return false.  If the
        ///     filter changes, or if a custom filter is specified and 
        ///     the toolbox user changes, this will return true.
        /// </devdoc> 
        private bool Update() { 

            Debug.Assert(_selectionService != null, "Should not have a null service inside this event handler."); 

            bool update = false;
            IDesigner targetDesigner = null;
            IComponent target = _selectionService.PrimarySelection as IComponent; 

            if (target != null) { 
                targetDesigner = _host.GetDesigner(target); 
            }
 
            if (targetDesigner == null) {
                target = _host.RootComponent;

                if (target != null) { 
                    targetDesigner = _host.GetDesigner(target);
                } 
            } 

            if (targetDesigner != _filterDesigner) { 

                // We have a new designer.  This could mean that
                // the filter we have aquired is no longer valid.
                // Compute a new filter. 
                //
                ArrayList newFilter; 
 
                if (targetDesigner != null) {
                    AttributeCollection newAttributes = GetDesignerAttributes(targetDesigner); 

                    newFilter = new ArrayList(newAttributes.Count);

                    foreach(Attribute a in newAttributes) { 
                        if (a is ToolboxItemFilterAttribute) {
                            newFilter.Add(a); 
                        } 
                    }
                } 
                else {
                    newFilter = new ArrayList();
                }
 
                if (_filter == null) {
                    update = true; 
                } 
                else if (_filter.Count != newFilter.Count) {
                    update = true; 
                }
                else {
                    IEnumerator lastEnum = _filter.GetEnumerator();
                    IEnumerator newEnum = newFilter.GetEnumerator(); 
                    while(newEnum.MoveNext()) {
                        lastEnum.MoveNext(); 
                        if (!newEnum.Current.Equals(lastEnum.Current)) { 
                            update = true;
                            break; 
                        }
                        else {
                            // For custom filters, a change in the current designer could
                            // cause the toolbox item enabling to change, so we must always 
                            // update ourselves for custom filters.
                            // 
                            ToolboxItemFilterAttribute attr = (ToolboxItemFilterAttribute)newEnum.Current; 
                            if (attr.FilterType == ToolboxItemFilterType.Custom) {
                                update = true; 
                                break;
                            }
                        }
                    } 
                }
 
                _filter = newFilter; 
                _filterDesigner = targetDesigner;
                _toolboxUser = _filterDesigner as IToolboxUser; 

                if (_toolboxUser == null) {
                    ITreeDesigner tree = _filterDesigner as ITreeDesigner;
                    while(_toolboxUser == null && tree != null) { 
                        IDesigner designer = tree.Parent;
                        _toolboxUser = designer as IToolboxUser; 
                        tree = designer as ITreeDesigner; 
                    }
                } 

                if (_toolboxUser == null && _host.RootComponent != null) {
                    _toolboxUser = _host.GetDesigner(_host.RootComponent) as IToolboxUser;
                } 
            }
 
            if (_filter == null) { 
                _filter = new ArrayList();
            } 

            return update;
        }
 
        /// <devdoc>
        ///     This object is added as a service to the designer, and automatically 
        ///     disposed upon designer shutdown. 
        /// </devdoc>
        void IDisposable.Dispose() { 
            if (_selectionService != null) {
                _selectionService.SelectionChanged -= new EventHandler(OnSelectionChanged);
            }
            if(_host.RootComponent != null) { 
                _host.RootComponent.Disposed -= new EventHandler(OnDesignerDisposed);
            } 
 
            TypeDescriptor.Refreshed -= new RefreshEventHandler(OnTypeDescriptorRefresh);
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerToolboxInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Drawing.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization;
    using System.IO; 
    using System.Reflection; 
    using System.Runtime.Remoting.Lifetime;
    using System.Runtime.Serialization; 
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Windows.Forms;

    /// <devdoc> 
    ///     This object lives as a service on a designer host
    ///     and provides per-designer toolbox information, such 
    ///     as the filter and current toolbox user. 
    /// </devdoc>
    internal sealed class DesignerToolboxInfo : IDisposable { 

        private ToolboxService              _toolboxService;
        private IDesignerHost               _host;
        private ISelectionService           _selectionService; 
        private ArrayList                   _filter;
        private IDesigner                   _filterDesigner; 
        private IToolboxUser                _toolboxUser; 
        private Hashtable                   _attributeHash;
 
        /// <devdoc>
        ///     Creates a new toolbox designer state object.
        /// </devdoc>
        internal DesignerToolboxInfo(ToolboxService toolboxService, IDesignerHost host) { 

            _toolboxService = toolboxService; 
            _host = host; 
            _selectionService = host.GetService(typeof(ISelectionService)) as ISelectionService;
 
            if (_selectionService != null) {
                _selectionService.SelectionChanged += new EventHandler(OnSelectionChanged);
            }
 
            if(_host.RootComponent != null) {
                _host.RootComponent.Disposed += new EventHandler(OnDesignerDisposed); 
            } 

            TypeDescriptor.Refreshed += new RefreshEventHandler(OnTypeDescriptorRefresh); 

        }

        /// <devdoc> 
        ///     Returns the designer host this toolbox state object
        ///     is associated with. 
        /// </devdoc> 
        internal IDesignerHost DesignerHost {
            get { 
                return _host;
            }
        }
 
        /// <devdoc>
        ///     Returns the current toolbox item filter. 
        /// </devdoc> 
        internal ICollection Filter {
            get { 
                if (_filter == null) {
                    Update();
                }
 
                return _filter;
            } 
        } 

        /// <devdoc> 
        ///     Returns the current toolbox user.
        /// </devdoc>
        internal IToolboxUser ToolboxUser {
            get { 
                if (_toolboxUser == null) {
                    Update(); 
                } 
                return _toolboxUser;
            } 
        }

        private void OnTypeDescriptorRefresh(RefreshEventArgs r) {
            if (r.ComponentChanged == _filterDesigner) { 
                _filter = null;
                _filterDesigner = null; 
            } 
        }
 
        /// <devdoc>
        ///     The GetDesignerAttributes method is used to retrieve a collection of attributes for
        ///     the specified designer.  If the designer implements ITreeDesigner, this method will
        ///     also collect attributes for all parent designers.  Attributes are merged such that 
        ///     child attributes replace matching parent attributes.
        /// </devdoc> 
        public AttributeCollection GetDesignerAttributes(IDesigner designer) { 

            if (designer == null) { 
                throw new ArgumentNullException("designer");
            }

            AttributeCollection attributes; 

            // We don't store the merged attribute set.  If we 
            // did, we would have to also store what designer 
            // the merge was for, and then we would have to worry
            // about designer GC and cleaning out our table.  That 
            // would be a pain.  But we do re-use the same
            // hashtable over and over so we don't have to "new"
            // one each time.  This keeps the buckets preallocated.
            // 
            if (_attributeHash == null) {
                _attributeHash = new Hashtable(); 
            } 
            else {
                _attributeHash.Clear(); 
            }

            // If the designer is a designer tree, then we rely
            // on the designer's "treeness" to get to the root. 
            // If it's not, then we include the root designer
            // directly. 
 
            if (!(designer is ITreeDesigner)) {
                IComponent root = _host.RootComponent; 
                if (root != null) {
                    RecurseDesignerTree(_host.GetDesigner(root), _attributeHash);
                }
            } 

            RecurseDesignerTree(designer, _attributeHash); 
 
            Attribute[] attributeArray = new Attribute[_attributeHash.Values.Count];
            _attributeHash.Values.CopyTo(attributeArray, 0); 
            attributes = new AttributeCollection(attributeArray);
            return attributes;
        }
 
        /// <devdoc>
        ///     Recurive algorithm to walk the designer tree for attributes. 
        /// </devdoc> 
        private void RecurseDesignerTree(IDesigner designer, Hashtable table) {
            ITreeDesigner tree = designer as ITreeDesigner; 
            if (tree != null) {
                IDesigner parent = tree.Parent;
                if (parent != null) {
                    RecurseDesignerTree(parent, table); 
                }
            } 
 
            foreach(Attribute a in TypeDescriptor.GetAttributes(designer)) {
                table[a.TypeId] = a; 
            }
        }

 
        private void OnDesignerDisposed(object sender, EventArgs e) {
            _host.RemoveService(typeof(DesignerToolboxInfo)); 
        } 

 

        /// <devdoc>
        ///     Called when the designer selection changes.  This recomputes the filter and
        ///     toolbox user data based on the primary selection.  If this is the 
        ///     currently active designer it will notify the toolbox service that
        ///     the filter has changed.  The toolbox service will raise its FilterChanged 
        ///     event. 
        /// </devdoc>
        private void OnSelectionChanged(object sender, EventArgs e) { 
            if (Update()) {
                _toolboxService.OnDesignerInfoChanged(this);
            }
        } 

        /// <devdoc> 
        ///     Called to recalculate our designer filter and toolbox 
        ///     user.  This will return true if the state has changed
        ///     in such a way as to require a requery of the toolbox item 
        ///     enabling.  For exapmle, if the toolbox user changes, but
        ///     the filter didn't change (and the filter didn't specify
        ///     "Custom"), then this will return false.  If the
        ///     filter changes, or if a custom filter is specified and 
        ///     the toolbox user changes, this will return true.
        /// </devdoc> 
        private bool Update() { 

            Debug.Assert(_selectionService != null, "Should not have a null service inside this event handler."); 

            bool update = false;
            IDesigner targetDesigner = null;
            IComponent target = _selectionService.PrimarySelection as IComponent; 

            if (target != null) { 
                targetDesigner = _host.GetDesigner(target); 
            }
 
            if (targetDesigner == null) {
                target = _host.RootComponent;

                if (target != null) { 
                    targetDesigner = _host.GetDesigner(target);
                } 
            } 

            if (targetDesigner != _filterDesigner) { 

                // We have a new designer.  This could mean that
                // the filter we have aquired is no longer valid.
                // Compute a new filter. 
                //
                ArrayList newFilter; 
 
                if (targetDesigner != null) {
                    AttributeCollection newAttributes = GetDesignerAttributes(targetDesigner); 

                    newFilter = new ArrayList(newAttributes.Count);

                    foreach(Attribute a in newAttributes) { 
                        if (a is ToolboxItemFilterAttribute) {
                            newFilter.Add(a); 
                        } 
                    }
                } 
                else {
                    newFilter = new ArrayList();
                }
 
                if (_filter == null) {
                    update = true; 
                } 
                else if (_filter.Count != newFilter.Count) {
                    update = true; 
                }
                else {
                    IEnumerator lastEnum = _filter.GetEnumerator();
                    IEnumerator newEnum = newFilter.GetEnumerator(); 
                    while(newEnum.MoveNext()) {
                        lastEnum.MoveNext(); 
                        if (!newEnum.Current.Equals(lastEnum.Current)) { 
                            update = true;
                            break; 
                        }
                        else {
                            // For custom filters, a change in the current designer could
                            // cause the toolbox item enabling to change, so we must always 
                            // update ourselves for custom filters.
                            // 
                            ToolboxItemFilterAttribute attr = (ToolboxItemFilterAttribute)newEnum.Current; 
                            if (attr.FilterType == ToolboxItemFilterType.Custom) {
                                update = true; 
                                break;
                            }
                        }
                    } 
                }
 
                _filter = newFilter; 
                _filterDesigner = targetDesigner;
                _toolboxUser = _filterDesigner as IToolboxUser; 

                if (_toolboxUser == null) {
                    ITreeDesigner tree = _filterDesigner as ITreeDesigner;
                    while(_toolboxUser == null && tree != null) { 
                        IDesigner designer = tree.Parent;
                        _toolboxUser = designer as IToolboxUser; 
                        tree = designer as ITreeDesigner; 
                    }
                } 

                if (_toolboxUser == null && _host.RootComponent != null) {
                    _toolboxUser = _host.GetDesigner(_host.RootComponent) as IToolboxUser;
                } 
            }
 
            if (_filter == null) { 
                _filter = new ArrayList();
            } 

            return update;
        }
 
        /// <devdoc>
        ///     This object is added as a service to the designer, and automatically 
        ///     disposed upon designer shutdown. 
        /// </devdoc>
        void IDisposable.Dispose() { 
            if (_selectionService != null) {
                _selectionService.SelectionChanged -= new EventHandler(OnSelectionChanged);
            }
            if(_host.RootComponent != null) { 
                _host.RootComponent.Disposed -= new EventHandler(OnDesignerDisposed);
            } 
 
            TypeDescriptor.Refreshed -= new RefreshEventHandler(OnTypeDescriptorRefresh);
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
