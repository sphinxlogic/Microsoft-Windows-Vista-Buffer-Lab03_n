//------------------------------------------------------------------------------ 
// <copyright file="HierarchicalDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.ComponentModel;
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Drawing;
 
    /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner"]/*' />
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class HierarchicalDataSourceDesigner : ControlDesigner, IHierarchicalDataSourceDesigner { 

        private event EventHandler _dataSourceChangedEvent; 
        private event EventHandler _schemaRefreshedEvent;
        private int _suppressEventsCount;
        private bool _raiseDataSourceChangedEvent;
        private bool _raiseSchemaRefreshedEvent; 

 
        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new HierarchicalDataSourceDesignerActionList(this));
 
                return actionLists;
            } 
        } 

        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.CanConfigure"]/*' /> 
        /// <devdoc>
        /// Indicates whether the Configure() method can be called.
        /// </devdoc>
        public virtual bool CanConfigure { 
            get {
                return false; 
            } 
        }
 
        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.CanRefreshSchema"]/*' />
        /// <devdoc>
        /// Indicates whether the RefreshSchema() method can be called.
        /// </devdoc> 
        public virtual bool CanRefreshSchema {
            get { 
                return false; 
            }
        } 

        /// <summary>
        /// Raised when the properties of a DataControl have changed. This allows
        /// a data-bound control designer to take actions to refresh its 
        /// control in the designer.
        /// </summary> 
        public event EventHandler DataSourceChanged { 
            add {
                _dataSourceChangedEvent += value; 
            }
            remove {
                _dataSourceChangedEvent -= value;
            } 
        }
 
        /// <devdoc> 
        /// Raised when the schema of the DataSource has changed. This notifies
        /// a data-bound control designer that the available schema fields have 
        /// changed.
        /// </devdoc>
        public event EventHandler SchemaRefreshed {
            add { 
                _schemaRefreshedEvent += value;
            } 
            remove { 
                _schemaRefreshedEvent -= value;
            } 
        }

        protected bool SuppressingDataSourceEvents {
            get { 
                return (_suppressEventsCount > 0);
            } 
        } 

 
        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.Configure"]/*' />
        /// <devdoc>
        /// Launches the data source's configuration wizard.
        /// This method should only be called if the CanConfigure property is true. 
        /// </devdoc>
        public virtual void Configure() { 
            throw new NotSupportedException(); 
        }
 
        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc>
        /// Gets the design-time HTML.
        /// </devdoc> 
        public override string GetDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(); 
        } 

        public virtual DesignerHierarchicalDataSourceView GetView(string viewPath) { 
            return null;
        }

        /// <devdoc> 
        /// Raises the DataSourceChanged ecent.
        /// </devdoc> 
        protected virtual void OnDataSourceChanged(EventArgs e) { 
            if (SuppressingDataSourceEvents) {
                _raiseDataSourceChangedEvent = true; 
                return;
            }
            if (_dataSourceChangedEvent != null) {
                _dataSourceChangedEvent(this, e); 
            }
            _raiseDataSourceChangedEvent = false; 
        } 

        /// <devdoc> 
        /// Raises the SchemaRefreshed event.
        /// </devdoc>
        protected virtual void OnSchemaRefreshed(EventArgs e) {
            if (SuppressingDataSourceEvents) { 
                _raiseSchemaRefreshedEvent = true;
                return; 
            } 
            if (_schemaRefreshedEvent != null) {
                _schemaRefreshedEvent(this, e); 
            }
            _raiseSchemaRefreshedEvent = false;
        }
 
        public virtual void RefreshSchema(bool preferSilent) {
            throw new NotSupportedException(); 
        } 

        public virtual void ResumeDataSourceEvents() { 
            if (_suppressEventsCount == 0) {
                throw new InvalidOperationException(SR.GetString(SR.DataSource_CannotResumeEvents));
            }
            _suppressEventsCount--; 
            if (_suppressEventsCount == 0) {
                // If this is the last call to resume, we raise the events if necessary 
                if (_raiseDataSourceChangedEvent) { 
                    OnDataSourceChanged(EventArgs.Empty);
                } 
                if (_raiseSchemaRefreshedEvent) {
                    OnSchemaRefreshed(EventArgs.Empty);
                }
            } 
        }
 
        public virtual void SuppressDataSourceEvents() { 
            _suppressEventsCount++;
        } 


        private class HierarchicalDataSourceDesignerActionList : DesignerActionList {
            private HierarchicalDataSourceDesigner _parent; 

            public HierarchicalDataSourceDesignerActionList(HierarchicalDataSourceDesigner parent) : base(parent.Component) { 
                _parent = parent; 
            }
 
            public override bool AutoShow {
                get {
                    return true;
                } 
                set {
                } 
            } 

            public void Configure() { 
                _parent.Configure();
            }

            public void RefreshSchema() { 
                _parent.RefreshSchema(false);
            } 
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                if (_parent.CanConfigure) {
                    DesignerActionMethodItem methodItem = new DesignerActionMethodItem(this,
                                                                                       "Configure",
                                                                                       SR.GetString(SR.DataSourceDesigner_ConfigureDataSourceVerb), 
                                                                                       SR.GetString(SR.DataSourceDesigner_DataActionGroup),
                                                                                       SR.GetString(SR.DataSourceDesigner_ConfigureDataSourceVerbDesc), 
                                                                                       true); 
                    methodItem.AllowAssociate = true;
                    items.Add(methodItem); 
                }

                if (_parent.CanRefreshSchema) {
                    DesignerActionMethodItem methodItem = new DesignerActionMethodItem(this, 
                                                                                       "RefreshSchema",
                                                                                       SR.GetString(SR.DataSourceDesigner_RefreshSchemaVerb), 
                                                                                       SR.GetString(SR.DataSourceDesigner_DataActionGroup), 
                                                                                       SR.GetString(SR.DataSourceDesigner_RefreshSchemaVerbDesc),
                                                                                       false); 
                    methodItem.AllowAssociate = true;
                    items.Add(methodItem);
                }
 
                return items;
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="HierarchicalDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.ComponentModel;
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Drawing;
 
    /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner"]/*' />
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class HierarchicalDataSourceDesigner : ControlDesigner, IHierarchicalDataSourceDesigner { 

        private event EventHandler _dataSourceChangedEvent; 
        private event EventHandler _schemaRefreshedEvent;
        private int _suppressEventsCount;
        private bool _raiseDataSourceChangedEvent;
        private bool _raiseSchemaRefreshedEvent; 

 
        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new HierarchicalDataSourceDesignerActionList(this));
 
                return actionLists;
            } 
        } 

        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.CanConfigure"]/*' /> 
        /// <devdoc>
        /// Indicates whether the Configure() method can be called.
        /// </devdoc>
        public virtual bool CanConfigure { 
            get {
                return false; 
            } 
        }
 
        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.CanRefreshSchema"]/*' />
        /// <devdoc>
        /// Indicates whether the RefreshSchema() method can be called.
        /// </devdoc> 
        public virtual bool CanRefreshSchema {
            get { 
                return false; 
            }
        } 

        /// <summary>
        /// Raised when the properties of a DataControl have changed. This allows
        /// a data-bound control designer to take actions to refresh its 
        /// control in the designer.
        /// </summary> 
        public event EventHandler DataSourceChanged { 
            add {
                _dataSourceChangedEvent += value; 
            }
            remove {
                _dataSourceChangedEvent -= value;
            } 
        }
 
        /// <devdoc> 
        /// Raised when the schema of the DataSource has changed. This notifies
        /// a data-bound control designer that the available schema fields have 
        /// changed.
        /// </devdoc>
        public event EventHandler SchemaRefreshed {
            add { 
                _schemaRefreshedEvent += value;
            } 
            remove { 
                _schemaRefreshedEvent -= value;
            } 
        }

        protected bool SuppressingDataSourceEvents {
            get { 
                return (_suppressEventsCount > 0);
            } 
        } 

 
        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.Configure"]/*' />
        /// <devdoc>
        /// Launches the data source's configuration wizard.
        /// This method should only be called if the CanConfigure property is true. 
        /// </devdoc>
        public virtual void Configure() { 
            throw new NotSupportedException(); 
        }
 
        /// <include file='doc\HierarchicalDataSourceDesigner.uex' path='docs/doc[@for="HierarchicalDataSourceDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc>
        /// Gets the design-time HTML.
        /// </devdoc> 
        public override string GetDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(); 
        } 

        public virtual DesignerHierarchicalDataSourceView GetView(string viewPath) { 
            return null;
        }

        /// <devdoc> 
        /// Raises the DataSourceChanged ecent.
        /// </devdoc> 
        protected virtual void OnDataSourceChanged(EventArgs e) { 
            if (SuppressingDataSourceEvents) {
                _raiseDataSourceChangedEvent = true; 
                return;
            }
            if (_dataSourceChangedEvent != null) {
                _dataSourceChangedEvent(this, e); 
            }
            _raiseDataSourceChangedEvent = false; 
        } 

        /// <devdoc> 
        /// Raises the SchemaRefreshed event.
        /// </devdoc>
        protected virtual void OnSchemaRefreshed(EventArgs e) {
            if (SuppressingDataSourceEvents) { 
                _raiseSchemaRefreshedEvent = true;
                return; 
            } 
            if (_schemaRefreshedEvent != null) {
                _schemaRefreshedEvent(this, e); 
            }
            _raiseSchemaRefreshedEvent = false;
        }
 
        public virtual void RefreshSchema(bool preferSilent) {
            throw new NotSupportedException(); 
        } 

        public virtual void ResumeDataSourceEvents() { 
            if (_suppressEventsCount == 0) {
                throw new InvalidOperationException(SR.GetString(SR.DataSource_CannotResumeEvents));
            }
            _suppressEventsCount--; 
            if (_suppressEventsCount == 0) {
                // If this is the last call to resume, we raise the events if necessary 
                if (_raiseDataSourceChangedEvent) { 
                    OnDataSourceChanged(EventArgs.Empty);
                } 
                if (_raiseSchemaRefreshedEvent) {
                    OnSchemaRefreshed(EventArgs.Empty);
                }
            } 
        }
 
        public virtual void SuppressDataSourceEvents() { 
            _suppressEventsCount++;
        } 


        private class HierarchicalDataSourceDesignerActionList : DesignerActionList {
            private HierarchicalDataSourceDesigner _parent; 

            public HierarchicalDataSourceDesignerActionList(HierarchicalDataSourceDesigner parent) : base(parent.Component) { 
                _parent = parent; 
            }
 
            public override bool AutoShow {
                get {
                    return true;
                } 
                set {
                } 
            } 

            public void Configure() { 
                _parent.Configure();
            }

            public void RefreshSchema() { 
                _parent.RefreshSchema(false);
            } 
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                if (_parent.CanConfigure) {
                    DesignerActionMethodItem methodItem = new DesignerActionMethodItem(this,
                                                                                       "Configure",
                                                                                       SR.GetString(SR.DataSourceDesigner_ConfigureDataSourceVerb), 
                                                                                       SR.GetString(SR.DataSourceDesigner_DataActionGroup),
                                                                                       SR.GetString(SR.DataSourceDesigner_ConfigureDataSourceVerbDesc), 
                                                                                       true); 
                    methodItem.AllowAssociate = true;
                    items.Add(methodItem); 
                }

                if (_parent.CanRefreshSchema) {
                    DesignerActionMethodItem methodItem = new DesignerActionMethodItem(this, 
                                                                                       "RefreshSchema",
                                                                                       SR.GetString(SR.DataSourceDesigner_RefreshSchemaVerb), 
                                                                                       SR.GetString(SR.DataSourceDesigner_DataActionGroup), 
                                                                                       SR.GetString(SR.DataSourceDesigner_RefreshSchemaVerbDesc),
                                                                                       false); 
                    methodItem.AllowAssociate = true;
                    items.Add(methodItem);
                }
 
                return items;
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
