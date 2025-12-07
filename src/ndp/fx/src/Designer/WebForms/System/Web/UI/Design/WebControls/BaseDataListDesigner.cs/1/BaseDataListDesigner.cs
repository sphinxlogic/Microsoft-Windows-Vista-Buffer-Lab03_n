//------------------------------------------------------------------------------ 
// <copyright file="BaseDataListDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Design;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Diagnostics; 
    using System.Drawing;
    using System.IO; 
    using System.Text; 
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.Design.WebControls.ListControls;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design;
 
    using AttributeCollection = System.ComponentModel.AttributeCollection; 
    using Control = System.Web.UI.Control;
    using DataBinding = System.Web.UI.DataBinding; 
    using DataGrid = System.Web.UI.WebControls.DataGrid;
    using DataSourceConverter = System.Web.UI.Design.DataSourceConverter;

    /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides the base designer class for the DataList WebControl. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public abstract class BaseDataListDesigner : TemplatedControlDesigner, IDataBindingSchemaProvider, IDataSourceProvider {

        private BaseDataList bdl; 

        private DataTable dummyDataTable; 
        private DataTable designTimeDataTable; 
        private IDataSourceDesigner _dataSourceDesigner;
        private bool _keepDataSourceBrowsable; 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.ActionLists"]/*' />
        /// <summary>
        /// Adds designer actions to the ActionLists collection. 
        /// </summary>
        public override DesignerActionListCollection ActionLists { 
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new BaseDataListActionList(this, DataSourceDesigner));
                return actionLists;
            }
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataKeyField"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public string DataKeyField { 
            get {
                return bdl.DataKeyField;
            }
            set { 
                bdl.DataKeyField = value;
            } 
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataMember"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string DataMember { 
            get {
                return bdl.DataMember; 
            } 
            set {
                bdl.DataMember = value; 
                OnDataSourceChanged();
            }
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataSource"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets the data source property.
        ///    </para> 
        /// </devdoc>
        public string DataSource {
            get {
                DataBinding binding = DataBindings["DataSource"]; 

                if (binding != null) { 
                    return binding.Expression; 
                }
                return String.Empty; 
            }
            set {
                if ((value == null) || (value.Length == 0)) {
                    DataBindings.Remove("DataSource"); 
                }
                else { 
                    DataBinding binding = DataBindings["DataSource"]; 

                    if (binding == null) { 
                        binding = new DataBinding("DataSource", typeof(IEnumerable), value);
                    }
                    else {
                        binding.Expression = value; 
                    }
                    DataBindings.Add(binding); 
                } 

                OnDataSourceChanged(); 
                OnBindingsCollectionChangedInternal("DataSource");
            }
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataSourceDesigner"]/*' />
        /// <summary> 
        /// Provides access to the designer of the DataControl, when one 
        /// is selected for data binding.
        /// </summary> 
        public IDataSourceDesigner DataSourceDesigner {
            get {
                return _dataSourceDesigner;
            } 
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataSourceID"]/*' /> 
        /// <summary>
        /// Implements the designer's version of the DataSourceID property. 
        /// This is used to shadow the DataSourceID property of the
        /// runtime control.
        /// </summary>
        public string DataSourceID { 
            get {
                return bdl.DataSourceID; 
            } 
            set {
                if (value == DataSourceID) { 
                    return;
                }

                if (value == SR.GetString(SR.DataSourceIDChromeConverter_NewDataSource)) { 
                    CreateDataSource();
                    return; 
                } 

                if (value == SR.GetString(SR.DataSourceIDChromeConverter_NoDataSource)) { 
                    value = String.Empty;
                }
                bdl.DataSourceID = value;
                OnDataSourceChanged(); 
                OnSchemaRefreshed();
            } 
        } 

        public DesignerDataSourceView DesignerView { 
            get {
                // Get the current view based on the DataMember
                DesignerDataSourceView view = null;
                if (DataSourceDesigner != null) { 
                    view = DataSourceDesigner.GetView(DataMember);
                    if (view == null && (String.IsNullOrEmpty(DataMember))) { 
                        // DataMember is not set, and view was not found, get the first view 
                        string[] viewNames = DataSourceDesigner.GetViewNames();
                        if (viewNames != null && viewNames.Length > 0) { 
                            view = DataSourceDesigner.GetView(viewNames[0]);
                        }
                    }
                } 
                return view;
            } 
        } 

        private bool ConnectToDataSource() { 
            IDataSourceDesigner designer = GetDataSourceDesigner();

            if (_dataSourceDesigner != designer) {
                if (_dataSourceDesigner != null) { 
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged);
                    _dataSourceDesigner.SchemaRefreshed -= new EventHandler(SchemaRefreshed); 
                } 

                _dataSourceDesigner = designer; 
                if (_dataSourceDesigner != null) {
                    _dataSourceDesigner.DataSourceChanged += new EventHandler(DataSourceChanged);
                    _dataSourceDesigner.SchemaRefreshed += new EventHandler(SchemaRefreshed);
                } 

                return true; 
            } 
            return false;
        } 

        /// <devdoc>
        /// Calls the transacted change for creating a new datasource
        /// </devdoc> 
        private void CreateDataSource() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(CreateDataSourceCallback), null, SR.GetString(SR.BaseDataBoundControl_CreateDataSourceTransaction)); 
        } 

        /// <devdoc> 
        /// Transacted callback for creating a datasource
        /// </devdoc>
        private bool CreateDataSourceCallback(object context) {
            CreateDataSourceDialog dialog = new CreateDataSourceDialog(this, typeof(IDataSource), true); 
            DialogResult result = UIServiceHelper.ShowDialog(Component.Site, dialog);
            string newDataSourceID = dialog.DataSourceID; 
            if (newDataSourceID.Length > 0) { 
                DataSourceID = newDataSourceID;
            } 
            return (result == DialogResult.OK);
        }

        private void DataSourceChanged(object sender, EventArgs e) { 
            designTimeDataTable = null;
            UpdateDesignTimeHtml(); 
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Disposes of the resources (other than memory) used by
        ///       the <see cref='System.Web.UI.Design.WebControls.BaseDataListDesigner'/>. 
        ///    </para>
        /// </devdoc> 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                if (Component != null && Component.Site != null) { 
                    if (RootDesigner != null) {
                        RootDesigner.LoadComplete -= new EventHandler(OnDesignerLoadComplete);
                    }
 
                    IComponentChangeService changeService = (IComponentChangeService)Component.Site.GetService(typeof(IComponentChangeService));
                    if (changeService != null) { 
                        changeService.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded); 
                        changeService.ComponentRemoving -= new ComponentEventHandler(this.OnComponentRemoving);
                        changeService.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved); 
                        changeService.ComponentChanged -= new ComponentChangedEventHandler(this.OnAnyComponentChanged);
                    }
                }
 
                bdl = null;
                if (_dataSourceDesigner != null) { 
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged); 
                    _dataSourceDesigner.SchemaRefreshed -= new EventHandler(SchemaRefreshed);
                    _dataSourceDesigner = null; 
                }
            }

            base.Dispose(disposing); 
        }
 
        private IDataSourceDesigner GetDataSourceDesigner() { 
            IDataSourceDesigner designer = null;
            string dataSourceID = DataSourceID; 

            if (!String.IsNullOrEmpty(dataSourceID)) {
                System.Web.UI.Control dataSourceControl = ControlHelper.FindControl(Component.Site, (System.Web.UI.Control)Component, dataSourceID);
                if (dataSourceControl != null && dataSourceControl.Site != null) { 
                    IDesignerHost designerHost = (IDesignerHost)dataSourceControl.Site.GetService(typeof(IDesignerHost));
                    if (designerHost != null) { 
                        designer = designerHost.GetDesigner(dataSourceControl) as IDataSourceDesigner; 
                    }
                } 
            }
            return designer;
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetDesignTimeDataSource"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets sample data matching the schema of the selected data source.
        ///    </para> 
        /// </devdoc>
        protected IEnumerable GetDesignTimeDataSource(int minimumRows, out bool dummyDataSource) {
            IEnumerable selectedDataSource = GetResolvedSelectedDataSource();
            return GetDesignTimeDataSource(selectedDataSource, minimumRows, out dummyDataSource); 
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetDesignTimeDataSource1"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets sample data matching the schema of the selected data source.
        ///    </para>
        /// </devdoc>
        protected IEnumerable GetDesignTimeDataSource(IEnumerable selectedDataSource, int minimumRows, out bool dummyDataSource) { 
            DataTable dataTable = designTimeDataTable;
            dummyDataSource = false; 
 
            // use the datatable corresponding to the selected datasource if possible
            if (dataTable == null) { 
                if (selectedDataSource != null) {
                    designTimeDataTable = DesignTimeData.CreateSampleDataTable(selectedDataSource);

                    dataTable = designTimeDataTable; 
                }
 
                if (dataTable == null) { 
                    // fallback on a dummy datasource if we can't create a sample datatable
                    if (dummyDataTable == null) { 
                        dummyDataTable = DesignTimeData.CreateDummyDataTable();
                    }

                    dataTable = dummyDataTable; 
                    dummyDataSource = true;
                } 
            } 

            IEnumerable liveDataSource = DesignTimeData.GetDesignTimeDataSource(dataTable, minimumRows); 
            return liveDataSource;
        }

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetResolvedSelectedDataSource"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public IEnumerable GetResolvedSelectedDataSource() {
            IEnumerable selectedDataSource = null; 

            DataBinding binding = DataBindings["DataSource"];

            if (binding != null) { 
                selectedDataSource = DesignTimeData.GetSelectedDataSource(bdl, binding.Expression, DataMember);
            } 
 
            return selectedDataSource;
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetSelectedDataSource"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets the selected data source component from the component's container.
        ///    </para> 
        /// </devdoc> 
        public object GetSelectedDataSource() {
            object selectedDataSource = null; 

            DataBinding binding = DataBindings["DataSource"];

            if (binding != null) { 
                selectedDataSource = DesignTimeData.GetSelectedDataSource(bdl, binding.Expression);
            } 
 
            return selectedDataSource;
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetTemplateContainerDataSource"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets the template's container's data source.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public override IEnumerable GetTemplateContainerDataSource(string templateName) { 
            return GetResolvedSelectedDataSource();
        }

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.Initialize"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes the designer with the DataGrid control that this instance 
        ///       of the designer is associated with.
        ///    </para> 
        /// </devdoc>
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(BaseDataList));
            bdl = (BaseDataList)component; 
            base.Initialize(component);
 
            SetViewFlags(ViewFlags.DesignTimeHtmlRequiresLoadComplete, true); 

            if (RootDesigner != null) { 
                if (RootDesigner.IsLoading) {
                    RootDesigner.LoadComplete += new EventHandler(OnDesignerLoadComplete);
                }
                else { 
                    OnDesignerLoadComplete(null, EventArgs.Empty);
                } 
            } 

            IComponentChangeService changeService = (IComponentChangeService)component.Site.GetService(typeof(IComponentChangeService)); 
            if (changeService != null) {
                changeService.ComponentAdded += new ComponentEventHandler(this.OnComponentAdded);
                changeService.ComponentRemoving += new ComponentEventHandler(this.OnComponentRemoving);
                changeService.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved); 
                changeService.ComponentChanged += new ComponentChangedEventHandler(this.OnAnyComponentChanged);
            } 
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.InvokePropertyBuilder"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Invokes the property builder beginning with the specified page.
        ///    </para> 
        /// </devdoc>
        protected internal void InvokePropertyBuilder(int initialPage) { 
            // the property builder isn't a transacted change because it has an apply button. 
            ComponentEditor compEditor;
            if (bdl is DataGrid) { 
                compEditor = new DataGridComponentEditor(initialPage);
            }
            else {
                compEditor = new DataListComponentEditor(initialPage); 
            }
 
            compEditor.EditComponent(bdl); 
            return;
        } 

        private void OnAnyComponentChanged(object sender, ComponentChangedEventArgs e) {
            if (e.Member != null) {
                Object component = e.Component; 
                IDataSource dsControl = component as IDataSource;
                if (dsControl != null && dsControl is Control) { 
                    if (e.Member.Name == "ID" && Component != null) { 
                        if((string)e.OldValue == DataSourceID || (string)e.NewValue == DataSourceID) {
                            ConnectToDataSource(); 
                            UpdateDesignTimeHtml();
                        }
                    }
                } 
            }
        } 
 
        [Obsolete("Use of this method is not recommended because the AutoFormat dialog is launched by the designer host. The list of available AutoFormats is exposed on the ControlDesigner in the AutoFormats property. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected void OnAutoFormat(object sender, EventArgs e) { 
            // This is a breaking change from v1.x but was decided at Won't Fix in VSWhidbey 312409.
            // The old code would launch the auto format dialog, but now that the dialog is launched
            // by the designer host, there is no way to invoke it from here.
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnAutoFormatApplied"]/*' /> 
        /// <internalonly/> 
        public override void OnAutoFormatApplied(DesignerAutoFormat appliedAutoFormat) {
            OnStylesChanged(); 
            base.OnAutoFormatApplied(appliedAutoFormat);
        }

        /// <summary> 
        ///   Fires when a component is added.  This may be our DataSourceControl
        /// </summary> 
        private void OnComponentAdded(object sender, ComponentEventArgs e) { 
            IComponent component = e.Component;
            IDataSource dsControl = component as IDataSource; 
            if (dsControl != null && component is Control) {
                if (((Control)dsControl).ID == DataSourceID) {
                    ConnectToDataSource();
                    UpdateDesignTimeHtml(); 
                }
            } 
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnComponentChanged"]/*' /> 
        /// <summary>
        /// Fires when a component is changing.  This may be our DataSourceControl
        /// </summary>
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs e) { 
            if (e.Member != null) {
                string memberName = e.Member.Name; 
                if (memberName.Equals("DataSource") || memberName.Equals("DataMember")) { 
                    OnDataSourceChanged();
                } 
                else if (memberName.Equals("ItemStyle") ||
                         memberName.Equals("AlternatingItemStyle") ||
                         memberName.Equals("SelectedItemStyle") ||
                         memberName.Equals("EditItemStyle") || 
                         memberName.Equals("HeaderStyle") ||
                         memberName.Equals("FooterStyle") || 
                         memberName.Equals("SeparatorStyle") || 
                         memberName.Equals("Font") ||
                         memberName.Equals("ForeColor") || 
                         memberName.Equals("BackColor")) {
                    OnStylesChanged();
                }
            } 
            base.OnComponentChanged(sender, e);
        } 
 
        /// <summary>
        ///   Fires when a component is being removed.  This may be our DataSourceControl 
        /// </summary>
        private void OnComponentRemoving(object sender, ComponentEventArgs e) {
            IComponent component = e.Component;
            IDataSource dsControl = component as IDataSource; 
            if (dsControl != null && dsControl is Control && Component != null) {
                if (((Control)dsControl).ID == DataSourceID) { 
                    if (_dataSourceDesigner != null) { 
                        _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged);
                        _dataSourceDesigner.SchemaRefreshed -= new EventHandler(SchemaRefreshed); 
                        _dataSourceDesigner = null;
                    }
                }
            } 
        }
 
        /// <summary> 
        ///   Fires when a component is removed.  This may be our DataSourceControl
        /// </summary> 
        private void OnComponentRemoved(object sender, ComponentEventArgs e) {
            IComponent component = e.Component;
            IDataSource dsControl = component as IDataSource;
            if (dsControl != null && dsControl is Control && Component != null) { 
                if (((Control)dsControl).ID == DataSourceID) {
                    IDesignerHost designerHost = (IDesignerHost)this.GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null, "Did not get DesignerHost service."); 

                    if (designerHost != null && !designerHost.Loading) { 
                        ConnectToDataSource();
                        UpdateDesignTimeHtml();
                    }
                } 
            }
        } 
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnDataSourceChanged"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        ///    <para>
        ///       Raises the DataSourceChanged event.
        ///    </para> 
        /// </devdoc>
        protected internal virtual void OnDataSourceChanged() { 
            ConnectToDataSource(); 
            designTimeDataTable = null;
        } 

        private void OnDesignerLoadComplete(object sender, EventArgs e) {
            ConnectToDataSource();
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnPropertyBuilder"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Represents the method that will handle the property builder event. 
        ///    </para>
        /// </devdoc>
        protected void OnPropertyBuilder(object sender, EventArgs e) {
            InvokePropertyBuilder(0); 
        }
 
        /// <summary> 
        /// Raised when the data source that this control is bound to has
        /// new schema. Designers can override this to perform additional 
        /// actions required when new schema is available.
        /// </summary>
        protected virtual void OnSchemaRefreshed() {
            UpdateDesignTimeHtml(); 
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnStylesChanged"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when styles have been changed.
        ///    </para>
        /// </devdoc>
        protected internal void OnStylesChanged() { 
            OnTemplateEditingVerbsChanged();
        } 
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnTemplateEditingVerbsChanged"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Notification that is called when templates are changed.
        ///    </para>
        /// </devdoc> 
        protected abstract void OnTemplateEditingVerbsChanged();
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Filter the properties to replace the runtime DataSource property
        ///       descriptor with the designer's.
        ///    </para>
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 
 
            PropertyDescriptor prop;
 
            prop = (PropertyDescriptor)properties["DataSource"];
            Debug.Assert(prop != null);

            // we can't create the designer DataSource property based on the runtime property since these 
            // types do not match. Therefore, we have to copy over all the attributes from the runtime
            // and use them that way. 
 
            // Set the BrowsableAttribute for DataSource to false if DataSource is empty
            // so the user isn't confused about which DataSource property to use 
            System.ComponentModel.AttributeCollection runtimeAttributes = prop.Attributes;
            int browsableAttributeIndex = -1;
            int attributeCount;
            int runtimeAttributeCount = runtimeAttributes.Count; 
            string dataSource = DataSource;
 
            if (dataSource.Length > 0) { 
                _keepDataSourceBrowsable = true;
            } 

            // find the position of the BrowsableAttribute
            for (int i = 0; i < runtimeAttributes.Count; i++ ) {
                if (runtimeAttributes[i] is BrowsableAttribute) { 
                    browsableAttributeIndex = i;
                    break; 
                } 
            }
 
            // allocate the right sized array for attributes
            if (browsableAttributeIndex == -1 && dataSource.Length == 0 && !_keepDataSourceBrowsable) {
                attributeCount = runtimeAttributeCount + 2;
            } 
            else {
                attributeCount = runtimeAttributeCount + 1; 
            } 
            Attribute[] attrs = new Attribute[attributeCount];
 
            runtimeAttributes.CopyTo(attrs, 0);
            attrs[runtimeAttributeCount] = new TypeConverterAttribute(typeof(DataSourceConverter));

            // if DataSource is not empty and there was no BrowsableAttribute, add one.  Otherwise, 
            // change the one that's there to be false.
            if (dataSource.Length == 0 && !_keepDataSourceBrowsable) { 
                if (browsableAttributeIndex == -1) { 
                    attrs[runtimeAttributeCount + 1] = BrowsableAttribute.No;
                } 
                else {
                    attrs[browsableAttributeIndex] = BrowsableAttribute.No;
                }
            } 
            prop = TypeDescriptor.CreateProperty(this.GetType(), "DataSource", typeof(string),
                                                 attrs); 
            properties["DataSource"] = prop; 

            prop = (PropertyDescriptor)properties["DataMember"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataMemberConverter)) 
                                                 });
            properties["DataMember"] = prop; 
 
            prop = (PropertyDescriptor)properties["DataKeyField"];
            Debug.Assert(prop != null); 
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataFieldConverter))
                                                 }); 
            properties["DataKeyField"] = prop;
 
            prop = (PropertyDescriptor)properties["DataSourceID"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop, 
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataSourceIDConverter))
                                                 });
            properties["DataSourceID"] = prop; 
        }
 
        private void SchemaRefreshed(object sender, EventArgs e) { 
            OnSchemaRefreshed();
        } 

        #region Implementation of IDataBindingSchemaProvider
        bool IDataBindingSchemaProvider.CanRefreshSchema {
            get { 
                IDataSourceDesigner dataSourceDesigner = DataSourceDesigner;
                if (dataSourceDesigner != null) { 
                    return dataSourceDesigner.CanRefreshSchema; 
                }
                return false; 
            }
        }

        IDataSourceViewSchema IDataBindingSchemaProvider.Schema { 
            get {
                DesignerDataSourceView designerView = DesignerView; 
                if (designerView != null) { 
                    return designerView.Schema;
                } 
                return null;
            }
        }
 
        void IDataBindingSchemaProvider.RefreshSchema(bool preferSilent) {
            IDataSourceDesigner dataSourceDesigner = DataSourceDesigner; 
            if (dataSourceDesigner != null) { 
                dataSourceDesigner.RefreshSchema(preferSilent);
            } 
        }
        #endregion
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BaseDataListDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Design;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Diagnostics; 
    using System.Drawing;
    using System.IO; 
    using System.Text; 
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.Design.WebControls.ListControls;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design;
 
    using AttributeCollection = System.ComponentModel.AttributeCollection; 
    using Control = System.Web.UI.Control;
    using DataBinding = System.Web.UI.DataBinding; 
    using DataGrid = System.Web.UI.WebControls.DataGrid;
    using DataSourceConverter = System.Web.UI.Design.DataSourceConverter;

    /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides the base designer class for the DataList WebControl. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public abstract class BaseDataListDesigner : TemplatedControlDesigner, IDataBindingSchemaProvider, IDataSourceProvider {

        private BaseDataList bdl; 

        private DataTable dummyDataTable; 
        private DataTable designTimeDataTable; 
        private IDataSourceDesigner _dataSourceDesigner;
        private bool _keepDataSourceBrowsable; 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.ActionLists"]/*' />
        /// <summary>
        /// Adds designer actions to the ActionLists collection. 
        /// </summary>
        public override DesignerActionListCollection ActionLists { 
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new BaseDataListActionList(this, DataSourceDesigner));
                return actionLists;
            }
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataKeyField"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public string DataKeyField { 
            get {
                return bdl.DataKeyField;
            }
            set { 
                bdl.DataKeyField = value;
            } 
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataMember"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string DataMember { 
            get {
                return bdl.DataMember; 
            } 
            set {
                bdl.DataMember = value; 
                OnDataSourceChanged();
            }
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataSource"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets the data source property.
        ///    </para> 
        /// </devdoc>
        public string DataSource {
            get {
                DataBinding binding = DataBindings["DataSource"]; 

                if (binding != null) { 
                    return binding.Expression; 
                }
                return String.Empty; 
            }
            set {
                if ((value == null) || (value.Length == 0)) {
                    DataBindings.Remove("DataSource"); 
                }
                else { 
                    DataBinding binding = DataBindings["DataSource"]; 

                    if (binding == null) { 
                        binding = new DataBinding("DataSource", typeof(IEnumerable), value);
                    }
                    else {
                        binding.Expression = value; 
                    }
                    DataBindings.Add(binding); 
                } 

                OnDataSourceChanged(); 
                OnBindingsCollectionChangedInternal("DataSource");
            }
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataSourceDesigner"]/*' />
        /// <summary> 
        /// Provides access to the designer of the DataControl, when one 
        /// is selected for data binding.
        /// </summary> 
        public IDataSourceDesigner DataSourceDesigner {
            get {
                return _dataSourceDesigner;
            } 
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.DataSourceID"]/*' /> 
        /// <summary>
        /// Implements the designer's version of the DataSourceID property. 
        /// This is used to shadow the DataSourceID property of the
        /// runtime control.
        /// </summary>
        public string DataSourceID { 
            get {
                return bdl.DataSourceID; 
            } 
            set {
                if (value == DataSourceID) { 
                    return;
                }

                if (value == SR.GetString(SR.DataSourceIDChromeConverter_NewDataSource)) { 
                    CreateDataSource();
                    return; 
                } 

                if (value == SR.GetString(SR.DataSourceIDChromeConverter_NoDataSource)) { 
                    value = String.Empty;
                }
                bdl.DataSourceID = value;
                OnDataSourceChanged(); 
                OnSchemaRefreshed();
            } 
        } 

        public DesignerDataSourceView DesignerView { 
            get {
                // Get the current view based on the DataMember
                DesignerDataSourceView view = null;
                if (DataSourceDesigner != null) { 
                    view = DataSourceDesigner.GetView(DataMember);
                    if (view == null && (String.IsNullOrEmpty(DataMember))) { 
                        // DataMember is not set, and view was not found, get the first view 
                        string[] viewNames = DataSourceDesigner.GetViewNames();
                        if (viewNames != null && viewNames.Length > 0) { 
                            view = DataSourceDesigner.GetView(viewNames[0]);
                        }
                    }
                } 
                return view;
            } 
        } 

        private bool ConnectToDataSource() { 
            IDataSourceDesigner designer = GetDataSourceDesigner();

            if (_dataSourceDesigner != designer) {
                if (_dataSourceDesigner != null) { 
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged);
                    _dataSourceDesigner.SchemaRefreshed -= new EventHandler(SchemaRefreshed); 
                } 

                _dataSourceDesigner = designer; 
                if (_dataSourceDesigner != null) {
                    _dataSourceDesigner.DataSourceChanged += new EventHandler(DataSourceChanged);
                    _dataSourceDesigner.SchemaRefreshed += new EventHandler(SchemaRefreshed);
                } 

                return true; 
            } 
            return false;
        } 

        /// <devdoc>
        /// Calls the transacted change for creating a new datasource
        /// </devdoc> 
        private void CreateDataSource() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(CreateDataSourceCallback), null, SR.GetString(SR.BaseDataBoundControl_CreateDataSourceTransaction)); 
        } 

        /// <devdoc> 
        /// Transacted callback for creating a datasource
        /// </devdoc>
        private bool CreateDataSourceCallback(object context) {
            CreateDataSourceDialog dialog = new CreateDataSourceDialog(this, typeof(IDataSource), true); 
            DialogResult result = UIServiceHelper.ShowDialog(Component.Site, dialog);
            string newDataSourceID = dialog.DataSourceID; 
            if (newDataSourceID.Length > 0) { 
                DataSourceID = newDataSourceID;
            } 
            return (result == DialogResult.OK);
        }

        private void DataSourceChanged(object sender, EventArgs e) { 
            designTimeDataTable = null;
            UpdateDesignTimeHtml(); 
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Disposes of the resources (other than memory) used by
        ///       the <see cref='System.Web.UI.Design.WebControls.BaseDataListDesigner'/>. 
        ///    </para>
        /// </devdoc> 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                if (Component != null && Component.Site != null) { 
                    if (RootDesigner != null) {
                        RootDesigner.LoadComplete -= new EventHandler(OnDesignerLoadComplete);
                    }
 
                    IComponentChangeService changeService = (IComponentChangeService)Component.Site.GetService(typeof(IComponentChangeService));
                    if (changeService != null) { 
                        changeService.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded); 
                        changeService.ComponentRemoving -= new ComponentEventHandler(this.OnComponentRemoving);
                        changeService.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved); 
                        changeService.ComponentChanged -= new ComponentChangedEventHandler(this.OnAnyComponentChanged);
                    }
                }
 
                bdl = null;
                if (_dataSourceDesigner != null) { 
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged); 
                    _dataSourceDesigner.SchemaRefreshed -= new EventHandler(SchemaRefreshed);
                    _dataSourceDesigner = null; 
                }
            }

            base.Dispose(disposing); 
        }
 
        private IDataSourceDesigner GetDataSourceDesigner() { 
            IDataSourceDesigner designer = null;
            string dataSourceID = DataSourceID; 

            if (!String.IsNullOrEmpty(dataSourceID)) {
                System.Web.UI.Control dataSourceControl = ControlHelper.FindControl(Component.Site, (System.Web.UI.Control)Component, dataSourceID);
                if (dataSourceControl != null && dataSourceControl.Site != null) { 
                    IDesignerHost designerHost = (IDesignerHost)dataSourceControl.Site.GetService(typeof(IDesignerHost));
                    if (designerHost != null) { 
                        designer = designerHost.GetDesigner(dataSourceControl) as IDataSourceDesigner; 
                    }
                } 
            }
            return designer;
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetDesignTimeDataSource"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets sample data matching the schema of the selected data source.
        ///    </para> 
        /// </devdoc>
        protected IEnumerable GetDesignTimeDataSource(int minimumRows, out bool dummyDataSource) {
            IEnumerable selectedDataSource = GetResolvedSelectedDataSource();
            return GetDesignTimeDataSource(selectedDataSource, minimumRows, out dummyDataSource); 
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetDesignTimeDataSource1"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets sample data matching the schema of the selected data source.
        ///    </para>
        /// </devdoc>
        protected IEnumerable GetDesignTimeDataSource(IEnumerable selectedDataSource, int minimumRows, out bool dummyDataSource) { 
            DataTable dataTable = designTimeDataTable;
            dummyDataSource = false; 
 
            // use the datatable corresponding to the selected datasource if possible
            if (dataTable == null) { 
                if (selectedDataSource != null) {
                    designTimeDataTable = DesignTimeData.CreateSampleDataTable(selectedDataSource);

                    dataTable = designTimeDataTable; 
                }
 
                if (dataTable == null) { 
                    // fallback on a dummy datasource if we can't create a sample datatable
                    if (dummyDataTable == null) { 
                        dummyDataTable = DesignTimeData.CreateDummyDataTable();
                    }

                    dataTable = dummyDataTable; 
                    dummyDataSource = true;
                } 
            } 

            IEnumerable liveDataSource = DesignTimeData.GetDesignTimeDataSource(dataTable, minimumRows); 
            return liveDataSource;
        }

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetResolvedSelectedDataSource"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public IEnumerable GetResolvedSelectedDataSource() {
            IEnumerable selectedDataSource = null; 

            DataBinding binding = DataBindings["DataSource"];

            if (binding != null) { 
                selectedDataSource = DesignTimeData.GetSelectedDataSource(bdl, binding.Expression, DataMember);
            } 
 
            return selectedDataSource;
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetSelectedDataSource"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets the selected data source component from the component's container.
        ///    </para> 
        /// </devdoc> 
        public object GetSelectedDataSource() {
            object selectedDataSource = null; 

            DataBinding binding = DataBindings["DataSource"];

            if (binding != null) { 
                selectedDataSource = DesignTimeData.GetSelectedDataSource(bdl, binding.Expression);
            } 
 
            return selectedDataSource;
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.GetTemplateContainerDataSource"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets the template's container's data source.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public override IEnumerable GetTemplateContainerDataSource(string templateName) { 
            return GetResolvedSelectedDataSource();
        }

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.Initialize"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes the designer with the DataGrid control that this instance 
        ///       of the designer is associated with.
        ///    </para> 
        /// </devdoc>
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(BaseDataList));
            bdl = (BaseDataList)component; 
            base.Initialize(component);
 
            SetViewFlags(ViewFlags.DesignTimeHtmlRequiresLoadComplete, true); 

            if (RootDesigner != null) { 
                if (RootDesigner.IsLoading) {
                    RootDesigner.LoadComplete += new EventHandler(OnDesignerLoadComplete);
                }
                else { 
                    OnDesignerLoadComplete(null, EventArgs.Empty);
                } 
            } 

            IComponentChangeService changeService = (IComponentChangeService)component.Site.GetService(typeof(IComponentChangeService)); 
            if (changeService != null) {
                changeService.ComponentAdded += new ComponentEventHandler(this.OnComponentAdded);
                changeService.ComponentRemoving += new ComponentEventHandler(this.OnComponentRemoving);
                changeService.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved); 
                changeService.ComponentChanged += new ComponentChangedEventHandler(this.OnAnyComponentChanged);
            } 
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.InvokePropertyBuilder"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Invokes the property builder beginning with the specified page.
        ///    </para> 
        /// </devdoc>
        protected internal void InvokePropertyBuilder(int initialPage) { 
            // the property builder isn't a transacted change because it has an apply button. 
            ComponentEditor compEditor;
            if (bdl is DataGrid) { 
                compEditor = new DataGridComponentEditor(initialPage);
            }
            else {
                compEditor = new DataListComponentEditor(initialPage); 
            }
 
            compEditor.EditComponent(bdl); 
            return;
        } 

        private void OnAnyComponentChanged(object sender, ComponentChangedEventArgs e) {
            if (e.Member != null) {
                Object component = e.Component; 
                IDataSource dsControl = component as IDataSource;
                if (dsControl != null && dsControl is Control) { 
                    if (e.Member.Name == "ID" && Component != null) { 
                        if((string)e.OldValue == DataSourceID || (string)e.NewValue == DataSourceID) {
                            ConnectToDataSource(); 
                            UpdateDesignTimeHtml();
                        }
                    }
                } 
            }
        } 
 
        [Obsolete("Use of this method is not recommended because the AutoFormat dialog is launched by the designer host. The list of available AutoFormats is exposed on the ControlDesigner in the AutoFormats property. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected void OnAutoFormat(object sender, EventArgs e) { 
            // This is a breaking change from v1.x but was decided at Won't Fix in VSWhidbey 312409.
            // The old code would launch the auto format dialog, but now that the dialog is launched
            // by the designer host, there is no way to invoke it from here.
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnAutoFormatApplied"]/*' /> 
        /// <internalonly/> 
        public override void OnAutoFormatApplied(DesignerAutoFormat appliedAutoFormat) {
            OnStylesChanged(); 
            base.OnAutoFormatApplied(appliedAutoFormat);
        }

        /// <summary> 
        ///   Fires when a component is added.  This may be our DataSourceControl
        /// </summary> 
        private void OnComponentAdded(object sender, ComponentEventArgs e) { 
            IComponent component = e.Component;
            IDataSource dsControl = component as IDataSource; 
            if (dsControl != null && component is Control) {
                if (((Control)dsControl).ID == DataSourceID) {
                    ConnectToDataSource();
                    UpdateDesignTimeHtml(); 
                }
            } 
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnComponentChanged"]/*' /> 
        /// <summary>
        /// Fires when a component is changing.  This may be our DataSourceControl
        /// </summary>
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs e) { 
            if (e.Member != null) {
                string memberName = e.Member.Name; 
                if (memberName.Equals("DataSource") || memberName.Equals("DataMember")) { 
                    OnDataSourceChanged();
                } 
                else if (memberName.Equals("ItemStyle") ||
                         memberName.Equals("AlternatingItemStyle") ||
                         memberName.Equals("SelectedItemStyle") ||
                         memberName.Equals("EditItemStyle") || 
                         memberName.Equals("HeaderStyle") ||
                         memberName.Equals("FooterStyle") || 
                         memberName.Equals("SeparatorStyle") || 
                         memberName.Equals("Font") ||
                         memberName.Equals("ForeColor") || 
                         memberName.Equals("BackColor")) {
                    OnStylesChanged();
                }
            } 
            base.OnComponentChanged(sender, e);
        } 
 
        /// <summary>
        ///   Fires when a component is being removed.  This may be our DataSourceControl 
        /// </summary>
        private void OnComponentRemoving(object sender, ComponentEventArgs e) {
            IComponent component = e.Component;
            IDataSource dsControl = component as IDataSource; 
            if (dsControl != null && dsControl is Control && Component != null) {
                if (((Control)dsControl).ID == DataSourceID) { 
                    if (_dataSourceDesigner != null) { 
                        _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged);
                        _dataSourceDesigner.SchemaRefreshed -= new EventHandler(SchemaRefreshed); 
                        _dataSourceDesigner = null;
                    }
                }
            } 
        }
 
        /// <summary> 
        ///   Fires when a component is removed.  This may be our DataSourceControl
        /// </summary> 
        private void OnComponentRemoved(object sender, ComponentEventArgs e) {
            IComponent component = e.Component;
            IDataSource dsControl = component as IDataSource;
            if (dsControl != null && dsControl is Control && Component != null) { 
                if (((Control)dsControl).ID == DataSourceID) {
                    IDesignerHost designerHost = (IDesignerHost)this.GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null, "Did not get DesignerHost service."); 

                    if (designerHost != null && !designerHost.Loading) { 
                        ConnectToDataSource();
                        UpdateDesignTimeHtml();
                    }
                } 
            }
        } 
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnDataSourceChanged"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        ///    <para>
        ///       Raises the DataSourceChanged event.
        ///    </para> 
        /// </devdoc>
        protected internal virtual void OnDataSourceChanged() { 
            ConnectToDataSource(); 
            designTimeDataTable = null;
        } 

        private void OnDesignerLoadComplete(object sender, EventArgs e) {
            ConnectToDataSource();
        } 

        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnPropertyBuilder"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Represents the method that will handle the property builder event. 
        ///    </para>
        /// </devdoc>
        protected void OnPropertyBuilder(object sender, EventArgs e) {
            InvokePropertyBuilder(0); 
        }
 
        /// <summary> 
        /// Raised when the data source that this control is bound to has
        /// new schema. Designers can override this to perform additional 
        /// actions required when new schema is available.
        /// </summary>
        protected virtual void OnSchemaRefreshed() {
            UpdateDesignTimeHtml(); 
        }
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnStylesChanged"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when styles have been changed.
        ///    </para>
        /// </devdoc>
        protected internal void OnStylesChanged() { 
            OnTemplateEditingVerbsChanged();
        } 
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.OnTemplateEditingVerbsChanged"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Notification that is called when templates are changed.
        ///    </para>
        /// </devdoc> 
        protected abstract void OnTemplateEditingVerbsChanged();
 
        /// <include file='doc\BaseDataListDesigner.uex' path='docs/doc[@for="BaseDataListDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Filter the properties to replace the runtime DataSource property
        ///       descriptor with the designer's.
        ///    </para>
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 
 
            PropertyDescriptor prop;
 
            prop = (PropertyDescriptor)properties["DataSource"];
            Debug.Assert(prop != null);

            // we can't create the designer DataSource property based on the runtime property since these 
            // types do not match. Therefore, we have to copy over all the attributes from the runtime
            // and use them that way. 
 
            // Set the BrowsableAttribute for DataSource to false if DataSource is empty
            // so the user isn't confused about which DataSource property to use 
            System.ComponentModel.AttributeCollection runtimeAttributes = prop.Attributes;
            int browsableAttributeIndex = -1;
            int attributeCount;
            int runtimeAttributeCount = runtimeAttributes.Count; 
            string dataSource = DataSource;
 
            if (dataSource.Length > 0) { 
                _keepDataSourceBrowsable = true;
            } 

            // find the position of the BrowsableAttribute
            for (int i = 0; i < runtimeAttributes.Count; i++ ) {
                if (runtimeAttributes[i] is BrowsableAttribute) { 
                    browsableAttributeIndex = i;
                    break; 
                } 
            }
 
            // allocate the right sized array for attributes
            if (browsableAttributeIndex == -1 && dataSource.Length == 0 && !_keepDataSourceBrowsable) {
                attributeCount = runtimeAttributeCount + 2;
            } 
            else {
                attributeCount = runtimeAttributeCount + 1; 
            } 
            Attribute[] attrs = new Attribute[attributeCount];
 
            runtimeAttributes.CopyTo(attrs, 0);
            attrs[runtimeAttributeCount] = new TypeConverterAttribute(typeof(DataSourceConverter));

            // if DataSource is not empty and there was no BrowsableAttribute, add one.  Otherwise, 
            // change the one that's there to be false.
            if (dataSource.Length == 0 && !_keepDataSourceBrowsable) { 
                if (browsableAttributeIndex == -1) { 
                    attrs[runtimeAttributeCount + 1] = BrowsableAttribute.No;
                } 
                else {
                    attrs[browsableAttributeIndex] = BrowsableAttribute.No;
                }
            } 
            prop = TypeDescriptor.CreateProperty(this.GetType(), "DataSource", typeof(string),
                                                 attrs); 
            properties["DataSource"] = prop; 

            prop = (PropertyDescriptor)properties["DataMember"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataMemberConverter)) 
                                                 });
            properties["DataMember"] = prop; 
 
            prop = (PropertyDescriptor)properties["DataKeyField"];
            Debug.Assert(prop != null); 
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataFieldConverter))
                                                 }); 
            properties["DataKeyField"] = prop;
 
            prop = (PropertyDescriptor)properties["DataSourceID"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop, 
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataSourceIDConverter))
                                                 });
            properties["DataSourceID"] = prop; 
        }
 
        private void SchemaRefreshed(object sender, EventArgs e) { 
            OnSchemaRefreshed();
        } 

        #region Implementation of IDataBindingSchemaProvider
        bool IDataBindingSchemaProvider.CanRefreshSchema {
            get { 
                IDataSourceDesigner dataSourceDesigner = DataSourceDesigner;
                if (dataSourceDesigner != null) { 
                    return dataSourceDesigner.CanRefreshSchema; 
                }
                return false; 
            }
        }

        IDataSourceViewSchema IDataBindingSchemaProvider.Schema { 
            get {
                DesignerDataSourceView designerView = DesignerView; 
                if (designerView != null) { 
                    return designerView.Schema;
                } 
                return null;
            }
        }
 
        void IDataBindingSchemaProvider.RefreshSchema(bool preferSilent) {
            IDataSourceDesigner dataSourceDesigner = DataSourceDesigner; 
            if (dataSourceDesigner != null) { 
                dataSourceDesigner.RefreshSchema(preferSilent);
            } 
        }
        #endregion
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
