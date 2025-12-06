//------------------------------------------------------------------------------ 
// <copyright file="DataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.ComponentModel;
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Drawing;
 
    /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner"]/*' />
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class DataSourceDesigner : ControlDesigner, IDataSourceDesigner { 

        private event EventHandler _dataSourceChangedEvent; 
        private event EventHandler _schemaRefreshedEvent;
        private int _suppressEventsCount;
        private bool _raiseDataSourceChangedEvent;
        private bool _raiseSchemaRefreshedEvent; 

 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new DataSourceDesignerActionList(this));
 
                return actionLists;
            } 
        } 

        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.CanConfigure"]/*' /> 
        /// <devdoc>
        /// Indicates whether the Configure() method can be called.
        /// </devdoc>
        public virtual bool CanConfigure { 
            get {
                return false; 
            } 
        }
 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.CanRefreshSchema"]/*' />
        /// <devdoc>
        /// Indicates whether the RefreshSchema() method can be called.
        /// </devdoc> 
        public virtual bool CanRefreshSchema {
            get { 
                return false; 
            }
        } 

        /// <devdoc>
        /// Raised when the properties of the DataSource have changed. This allows
        /// a data-bound control designer to take actions to refresh its 
        /// control in the designer.
        /// </devdoc> 
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

 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.Configure"]/*' />
        /// <devdoc>
        /// Launches the data source's configuration wizard.
        /// This method should only be called if the CanConfigure property is true. 
        /// </devdoc>
        public virtual void Configure() { 
            throw new NotSupportedException(); 
        }
 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc>
        /// Gets the design-time HTML.
        /// </devdoc> 
        public override string GetDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(); 
        } 

        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.GetView"]/*' /> 
        /// <devdoc>
        /// Gets a DesignerDataSourceView representing the view indicated by
        /// the viewName parameter. If the view does not exist, null should
        /// be returned. 
        /// </devdoc>
        public virtual DesignerDataSourceView GetView(string viewName) { 
            return null; 
        }
 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.GetViewNames"]/*' />
        /// <devdoc>
        /// Returns an array of the view names available in this data source.
        /// </devdoc> 
        public virtual string[] GetViewNames() {
            return new string[0]; 
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
 
        /// <devdoc> 
        /// Compares two schemas based on their views and the names and types
        /// of the fields in the views they contain. Returns true if they are 
        /// equivalent.
        /// </devdoc>
        public static bool SchemasEquivalent(IDataSourceSchema schema1, IDataSourceSchema schema2) {
            if (schema1 == null ^ schema2 == null) { 
                return false;
            } 
            if (schema1 == null && schema2 == null) { 
                return true;
            } 

            IDataSourceViewSchema[] viewSchemas1 = schema1.GetViews();
            IDataSourceViewSchema[] viewSchemas2 = schema2.GetViews();
            if (viewSchemas1 == null ^ viewSchemas2 == null) { 
                return false;
            } 
            if (viewSchemas1 == null && viewSchemas2 == null) { 
                return true;
            } 

            int viewSchemasCount1 = viewSchemas1.Length;
            int viewSchemasCount2 = viewSchemas2.Length;
 
            if (viewSchemasCount1 != viewSchemasCount2) {
                return false; 
            } 

            foreach (IDataSourceViewSchema viewSchema1 in viewSchemas1) { 
                bool foundView = false;
                string viewName1 = viewSchema1.Name;
                foreach (IDataSourceViewSchema viewSchema2 in viewSchemas2) {
                    if (viewName1 == viewSchema2.Name && 
                        ViewSchemasEquivalent(viewSchema1, viewSchema2)) {
                        foundView = true; 
                        break; 
                    }
                } 
                if (!foundView) {
                    return false;
                }
            } 
            return true;
        } 
 
        /// <devdoc>
        /// Compares two view schemas based on the names and types of the 
        /// fields they contain. Returns true if they are equivalent.
        /// </devdoc>
        public static bool ViewSchemasEquivalent(IDataSourceViewSchema viewSchema1, IDataSourceViewSchema viewSchema2) {
            if (viewSchema1 == null ^ viewSchema2 == null) { 
                return false;
            } 
            if (viewSchema1 == null && viewSchema2 == null) { 
                return true;
            } 

            IDataSourceFieldSchema[] fieldSchemas1 = viewSchema1.GetFields();
            IDataSourceFieldSchema[] fieldSchemas2 = viewSchema2.GetFields();
            if (fieldSchemas1 == null ^ fieldSchemas2 == null) { 
                return false;
            } 
            if (fieldSchemas1 == null && fieldSchemas2 == null) { 
                return true;
            } 

            int fieldSchemasCount1 = fieldSchemas1.Length;
            int fieldSchemasCount2 = fieldSchemas2.Length;
 
            if (fieldSchemasCount1 != fieldSchemasCount2) {
                return false; 
            } 

            foreach (IDataSourceFieldSchema fieldSchema1 in fieldSchemas1) { 
                bool foundField = false;
                string fieldName1 = fieldSchema1.Name;
                Type fieldType1 = fieldSchema1.DataType;
                foreach (IDataSourceFieldSchema fieldSchema2 in fieldSchemas2) { 
                    if (fieldName1 == fieldSchema2.Name &&
                        fieldType1 == fieldSchema2.DataType) { 
                        foundField = true; 
                        break;
                    } 
                }
                if (!foundField) {
                    return false;
                } 
            }
            return true; 
        } 

 
        private class DataSourceDesignerActionList : DesignerActionList {
            private DataSourceDesigner _parent;

            public DataSourceDesignerActionList(DataSourceDesigner parent) : base(parent.Component) { 
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
// <copyright file="DataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.ComponentModel;
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Drawing;
 
    /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner"]/*' />
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class DataSourceDesigner : ControlDesigner, IDataSourceDesigner { 

        private event EventHandler _dataSourceChangedEvent; 
        private event EventHandler _schemaRefreshedEvent;
        private int _suppressEventsCount;
        private bool _raiseDataSourceChangedEvent;
        private bool _raiseSchemaRefreshedEvent; 

 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new DataSourceDesignerActionList(this));
 
                return actionLists;
            } 
        } 

        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.CanConfigure"]/*' /> 
        /// <devdoc>
        /// Indicates whether the Configure() method can be called.
        /// </devdoc>
        public virtual bool CanConfigure { 
            get {
                return false; 
            } 
        }
 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.CanRefreshSchema"]/*' />
        /// <devdoc>
        /// Indicates whether the RefreshSchema() method can be called.
        /// </devdoc> 
        public virtual bool CanRefreshSchema {
            get { 
                return false; 
            }
        } 

        /// <devdoc>
        /// Raised when the properties of the DataSource have changed. This allows
        /// a data-bound control designer to take actions to refresh its 
        /// control in the designer.
        /// </devdoc> 
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

 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.Configure"]/*' />
        /// <devdoc>
        /// Launches the data source's configuration wizard.
        /// This method should only be called if the CanConfigure property is true. 
        /// </devdoc>
        public virtual void Configure() { 
            throw new NotSupportedException(); 
        }
 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc>
        /// Gets the design-time HTML.
        /// </devdoc> 
        public override string GetDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(); 
        } 

        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.GetView"]/*' /> 
        /// <devdoc>
        /// Gets a DesignerDataSourceView representing the view indicated by
        /// the viewName parameter. If the view does not exist, null should
        /// be returned. 
        /// </devdoc>
        public virtual DesignerDataSourceView GetView(string viewName) { 
            return null; 
        }
 
        /// <include file='doc\DataSourceDesigner.uex' path='docs/doc[@for="DataSourceDesigner.GetViewNames"]/*' />
        /// <devdoc>
        /// Returns an array of the view names available in this data source.
        /// </devdoc> 
        public virtual string[] GetViewNames() {
            return new string[0]; 
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
 
        /// <devdoc> 
        /// Compares two schemas based on their views and the names and types
        /// of the fields in the views they contain. Returns true if they are 
        /// equivalent.
        /// </devdoc>
        public static bool SchemasEquivalent(IDataSourceSchema schema1, IDataSourceSchema schema2) {
            if (schema1 == null ^ schema2 == null) { 
                return false;
            } 
            if (schema1 == null && schema2 == null) { 
                return true;
            } 

            IDataSourceViewSchema[] viewSchemas1 = schema1.GetViews();
            IDataSourceViewSchema[] viewSchemas2 = schema2.GetViews();
            if (viewSchemas1 == null ^ viewSchemas2 == null) { 
                return false;
            } 
            if (viewSchemas1 == null && viewSchemas2 == null) { 
                return true;
            } 

            int viewSchemasCount1 = viewSchemas1.Length;
            int viewSchemasCount2 = viewSchemas2.Length;
 
            if (viewSchemasCount1 != viewSchemasCount2) {
                return false; 
            } 

            foreach (IDataSourceViewSchema viewSchema1 in viewSchemas1) { 
                bool foundView = false;
                string viewName1 = viewSchema1.Name;
                foreach (IDataSourceViewSchema viewSchema2 in viewSchemas2) {
                    if (viewName1 == viewSchema2.Name && 
                        ViewSchemasEquivalent(viewSchema1, viewSchema2)) {
                        foundView = true; 
                        break; 
                    }
                } 
                if (!foundView) {
                    return false;
                }
            } 
            return true;
        } 
 
        /// <devdoc>
        /// Compares two view schemas based on the names and types of the 
        /// fields they contain. Returns true if they are equivalent.
        /// </devdoc>
        public static bool ViewSchemasEquivalent(IDataSourceViewSchema viewSchema1, IDataSourceViewSchema viewSchema2) {
            if (viewSchema1 == null ^ viewSchema2 == null) { 
                return false;
            } 
            if (viewSchema1 == null && viewSchema2 == null) { 
                return true;
            } 

            IDataSourceFieldSchema[] fieldSchemas1 = viewSchema1.GetFields();
            IDataSourceFieldSchema[] fieldSchemas2 = viewSchema2.GetFields();
            if (fieldSchemas1 == null ^ fieldSchemas2 == null) { 
                return false;
            } 
            if (fieldSchemas1 == null && fieldSchemas2 == null) { 
                return true;
            } 

            int fieldSchemasCount1 = fieldSchemas1.Length;
            int fieldSchemasCount2 = fieldSchemas2.Length;
 
            if (fieldSchemasCount1 != fieldSchemasCount2) {
                return false; 
            } 

            foreach (IDataSourceFieldSchema fieldSchema1 in fieldSchemas1) { 
                bool foundField = false;
                string fieldName1 = fieldSchema1.Name;
                Type fieldType1 = fieldSchema1.DataType;
                foreach (IDataSourceFieldSchema fieldSchema2 in fieldSchemas2) { 
                    if (fieldName1 == fieldSchema2.Name &&
                        fieldType1 == fieldSchema2.DataType) { 
                        foundField = true; 
                        break;
                    } 
                }
                if (!foundField) {
                    return false;
                } 
            }
            return true; 
        } 

 
        private class DataSourceDesignerActionList : DesignerActionList {
            private DataSourceDesigner _parent;

            public DataSourceDesignerActionList(DataSourceDesigner parent) : base(parent.Component) { 
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
