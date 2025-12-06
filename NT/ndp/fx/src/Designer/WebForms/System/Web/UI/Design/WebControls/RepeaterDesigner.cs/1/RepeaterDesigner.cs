//------------------------------------------------------------------------------ 
// <copyright file="RepeaterDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Design; 
    using System.Drawing;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Diagnostics; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;


    using AttributeCollection = System.ComponentModel.AttributeCollection; 
    using Control = System.Web.UI.Control;
    using DataBinding = System.Web.UI.DataBinding; 
 
    /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner"]/*' />
    /// <internalonly/> 
    /// <devdoc>
    ///    <para>
    ///       Provides a designer for the <see cref='System.Web.UI.WebControls.Repeater'/> control.
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class RepeaterDesigner : ControlDesigner, IDataSourceProvider { 

        internal static TraceSwitch RepeaterDesignerSwitch = 
            new TraceSwitch("RepeaterDesigner", "Enable Repeater designer general purpose traces.");

        private DataTable dummyDataTable;
        private DataTable designTimeDataTable; 
        private IDataSourceDesigner _dataSourceDesigner;
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.RepeaterDesigner"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Web.UI.Design.WebControls.RepeaterDesigner'/> class.
        ///    </para>
        /// </devdoc>
        public RepeaterDesigner() { 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.ActionLists"]/*' /> 
        /// <summary>
        /// Adds designer actions to the ActionLists collection. 
        /// </summary>
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists);
 
                actionLists.Add(new DataBoundControlActionList(this, DataSourceDesigner)); 
                return actionLists;
            } 
        }

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.DataMember"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public string DataMember { 
            get {
                return ((Repeater)Component).DataMember; 
            }
            set {
                ((Repeater)Component).DataMember = value;
                OnDataSourceChanged(); 
            }
        } 
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.DataSource"]/*' />
        /// <devdoc> 
        ///   Designer implementation of DataSource property that operates on
        ///   the DataSource property in the control's binding collection.
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
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.DataSourceDesigner"]/*' /> 
        /// <summary>
        /// Provides access to the designer of the DataControl, when one 
        /// is selected for data binding.
        /// </summary>
        public IDataSourceDesigner DataSourceDesigner {
            get { 
                return _dataSourceDesigner;
            } 
        } 

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.DataSourceID"]/*' /> 
        /// <summary>
        /// Implements the designer's version of the DataSourceID property.
        /// This is used to shadow the DataSourceID property of the
        /// runtime control. 
        /// </summary>
        public string DataSourceID { 
            get { 
                return ((Repeater)Component).DataSourceID;
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
                ((Repeater)Component).DataSourceID = value;
                OnDataSourceChanged(); 
                ExecuteChooseDataSourcePostSteps(); 
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

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.TemplatesExist"]/*' />
        /// <devdoc>
        /// </devdoc> 
        protected bool TemplatesExist {
            get { 
                Repeater repeater = ((Repeater)ViewControl); 
                return (repeater.ItemTemplate != null) ||
                       (repeater.HeaderTemplate != null) || 
                       (repeater.FooterTemplate != null) ||
                       (repeater.AlternatingItemTemplate != null);
            }
        } 

        private bool ConnectToDataSource() { 
            IDataSourceDesigner designer = GetDataSourceDesigner(); 

            if (_dataSourceDesigner != designer) { 
                if (_dataSourceDesigner != null) {
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged);
                }
 
                _dataSourceDesigner = designer;
                if (_dataSourceDesigner != null) { 
                    _dataSourceDesigner.DataSourceChanged += new EventHandler(DataSourceChanged); 
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

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///   Performs the cleanup of the designer class. 
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
            }
 
            base.Dispose(disposing); 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.ExecuteChooseDataSourcePostSteps"]/*' />
        /// <devdoc>
        /// Override to execute custom UI-less poststeps to choosing a data source
        /// </devdoc> 
        protected virtual void ExecuteChooseDataSourcePostSteps() {
            return; 
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
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetDesignTimeDataSource"]/*' /> 
        /// <devdoc>
        ///   Returns a sample data matching the schema of the selected datasource. 
        /// </devdoc>
        protected IEnumerable GetDesignTimeDataSource(int minimumRows) {
            IEnumerable selectedDataSource = GetResolvedSelectedDataSource();
            return GetDesignTimeDataSource(selectedDataSource, minimumRows); 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetDesignTimeDataSource1"]/*' /> 
        /// <devdoc>
        ///   Returns a sample data matching the schema of the selected datasource. 
        /// </devdoc>
        protected IEnumerable GetDesignTimeDataSource(IEnumerable selectedDataSource, int minimumRows) {
            DataTable dataTable = designTimeDataTable;
 
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
                }
            }
 
            IEnumerable liveDataSource = DesignTimeData.GetDesignTimeDataSource(dataTable, minimumRows);
            return liveDataSource; 
        } 

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetDesignTimeHtml"]/*' /> 
        /// <devdoc>
        ///   Retrieves the HTML to be used for the design time representation
        ///   of the control.
        /// </devdoc> 
        public override string GetDesignTimeHtml() {
            IEnumerable selectedDataSource = null; 
            bool hasATemplate = this.TemplatesExist; 
            Repeater repeater = (Repeater)ViewControl;
 
            string designTimeHTML;

            if (hasATemplate) {
                DesignerDataSourceView view = DesignerView; 
                IEnumerable designTimeDataSource = null;
                bool dataSourceIDChanged = false; 
                string oldDataSourceID = String.Empty; 

                if (view == null) { 
                    selectedDataSource = GetResolvedSelectedDataSource();
                    designTimeDataSource = GetDesignTimeDataSource(selectedDataSource, 5);
                }
                else { 
                    try {
                        bool dummyDataSource; 
                        designTimeDataSource = view.GetDesignTimeData(5, out dummyDataSource); 
                    }
                    catch (Exception ex) { 
                        if (Component.Site != null) {
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)Component.Site.GetService(typeof(IComponentDesignerDebugService));

                            if (debugService != null) { 
                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.GetDesignTimeData", ex.Message));
                            } 
                        } 
                    }
                } 

                try {
                    repeater.DataSource = designTimeDataSource;
                    oldDataSourceID = repeater.DataSourceID; 
                    repeater.DataSourceID = String.Empty;
                    dataSourceIDChanged = true; 
                    repeater.DataBind(); 
                    designTimeHTML = base.GetDesignTimeHtml();
                } 
                catch (Exception e) {
                    designTimeHTML = GetErrorDesignTimeHtml(e);
                }
                finally { 
                    repeater.DataSource = null;
                    if (dataSourceIDChanged) { 
                        repeater.DataSourceID = oldDataSourceID; 
                    }
                } 
            }
            else {
                designTimeHTML = GetEmptyDesignTimeHtml();
            } 

            return designTimeHTML; 
        } 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetEmptyDesignTimeHtml"]/*' />
        /// <devdoc> 
        /// </devdoc>
        protected override string GetEmptyDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Repeater_NoTemplatesInst));
        } 

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetErrorDesignTimeHtml"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        protected override string GetErrorDesignTimeHtml(Exception e) { 
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRendering));
        }

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetResolvedSelectedDataSource"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public IEnumerable GetResolvedSelectedDataSource() { 
            IEnumerable selectedDataSource = null;
 
            DataBinding binding = DataBindings["DataSource"];

            if (binding != null) {
                selectedDataSource = DesignTimeData.GetSelectedDataSource(Component, binding.Expression, DataMember); 
            }
 
            return selectedDataSource; 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetSelectedDataSource"]/*' />
        /// <devdoc>
        ///   Retrieves the selected datasource component from the component's container.
        /// </devdoc> 
        public object GetSelectedDataSource() {
            object selectedDataSource = null; 
 
            DataBinding binding = DataBindings["DataSource"];
 
            if (binding != null) {
                selectedDataSource = DesignTimeData.GetSelectedDataSource(Component, binding.Expression);
            }
 
            return selectedDataSource;
        } 
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///   Initializes the designer with the Repeater control that this instance
        ///   of the designer is associated with.
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(Repeater));
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
 
        private void OnAnyComponentChanged(object source, ComponentChangedEventArgs ce) {
            if (ce.Member != null) {
                Object component = ce.Component;
                Control dsControl = component as Control; 
                if (dsControl != null) {
                    if (ce.Member.Name == "ID" && Component != null) { 
                        if((string)ce.OldValue == DataSourceID || (string)ce.NewValue == DataSourceID) { 
                            ConnectToDataSource();
                            UpdateDesignTimeHtml(); 
                        }
                    }
                }
            } 
        }
 
        /// <summary> 
        ///   Fires when a component is added.  This may be our DataSourceControl
        /// </summary> 
        private void OnComponentAdded(object sender, ComponentEventArgs e) {
            IComponent component = e.Component;
            Control dsControl = component as Control;
            if (dsControl != null) { 
                if (dsControl.ID == DataSourceID) {
                    ConnectToDataSource(); 
                    UpdateDesignTimeHtml(); 
                }
            } 
        }

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.OnComponentChanged"]/*' />
        /// <devdoc> 
        ///   Handles changes made to the component. This includes changes made
        ///   in the properties window. 
        /// </devdoc> 
        public override void OnComponentChanged(object source, ComponentChangedEventArgs ce) {
            if (ce.Member != null) { 
                string memberName = ce.Member.Name;
                if (memberName.Equals("DataSource") || memberName.Equals("DataMember")) {
                    OnDataSourceChanged();
                } 
            }
 
            base.OnComponentChanged(source, ce); 
        }
 
        /// <summary>
        ///   Fires when a component is being removed.  This may be our DataSourceControl
        /// </summary>
        private void OnComponentRemoving(object sender, ComponentEventArgs e) { 
            IComponent component = e.Component;
            Control dsControl = component as Control; 
            if (dsControl != null) { 
                if (dsControl.ID == DataSourceID && Component != null) {
                    if (_dataSourceDesigner != null) { 
                        _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged);
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
            Control dsControl = component as Control; 
            if (dsControl != null && Component != null) {
                if (dsControl.ID == DataSourceID) { 
                    IDesignerHost designerHost = (IDesignerHost)this.GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null, "Did not get DesignerHost service.");
 
                    if (designerHost != null && !designerHost.Loading) {
                        ConnectToDataSource();
                        UpdateDesignTimeHtml();
                    } 
                }
            } 
        } 

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.OnDataSourceChanged"]/*' /> 
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Raises the DataSourceChanged event.  Public because it was shipped in V1 that way. 
        ///    </para>
        /// </devdoc> 
        public virtual void OnDataSourceChanged() { 
            ConnectToDataSource();
            designTimeDataTable = null; 
        }

        private void OnDesignerLoadComplete(object sender, EventArgs e) {
            ConnectToDataSource(); 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        ///   Filter the properties to replace the runtime DataSource property 
        ///   descriptor with the designer's.
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 
 
            prop = (PropertyDescriptor)properties["DataSource"];
            Debug.Assert(prop != null); 

            // we can't create the designer DataSource property based on the runtime property since theie
            // types do not match. Therefore, we have to copy over all the attributes from the runtime
            // and use them that way. 
            // Set the BrowsableAttribute for DataSource to false if DataSource is empty
            // so the user isn't confused about which DataSource property to use 
            System.ComponentModel.AttributeCollection runtimeAttributes = prop.Attributes; 
            int browsableAttributeIndex = -1;
            int attributeCount; 
            int runtimeAttributeCount = runtimeAttributes.Count;
            string dataSource = DataSource;

            // find the position of the BrowsableAttribute 
            for (int i = 0; i < runtimeAttributes.Count; i++ ) {
                if (runtimeAttributes[i] is BrowsableAttribute) { 
                    browsableAttributeIndex = i; 
                }
            } 

            // allocate the right sized array for attributes
            if (browsableAttributeIndex == -1 && dataSource.Length == 0) {
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
            if (dataSource.Length == 0) { 
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

            prop = (PropertyDescriptor)properties["DataSourceID"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataSourceIDConverter)) 
                                                 });
            properties["DataSourceID"] = prop; 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="RepeaterDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Design; 
    using System.Drawing;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Diagnostics; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;


    using AttributeCollection = System.ComponentModel.AttributeCollection; 
    using Control = System.Web.UI.Control;
    using DataBinding = System.Web.UI.DataBinding; 
 
    /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner"]/*' />
    /// <internalonly/> 
    /// <devdoc>
    ///    <para>
    ///       Provides a designer for the <see cref='System.Web.UI.WebControls.Repeater'/> control.
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class RepeaterDesigner : ControlDesigner, IDataSourceProvider { 

        internal static TraceSwitch RepeaterDesignerSwitch = 
            new TraceSwitch("RepeaterDesigner", "Enable Repeater designer general purpose traces.");

        private DataTable dummyDataTable;
        private DataTable designTimeDataTable; 
        private IDataSourceDesigner _dataSourceDesigner;
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.RepeaterDesigner"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Web.UI.Design.WebControls.RepeaterDesigner'/> class.
        ///    </para>
        /// </devdoc>
        public RepeaterDesigner() { 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.ActionLists"]/*' /> 
        /// <summary>
        /// Adds designer actions to the ActionLists collection. 
        /// </summary>
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists);
 
                actionLists.Add(new DataBoundControlActionList(this, DataSourceDesigner)); 
                return actionLists;
            } 
        }

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.DataMember"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public string DataMember { 
            get {
                return ((Repeater)Component).DataMember; 
            }
            set {
                ((Repeater)Component).DataMember = value;
                OnDataSourceChanged(); 
            }
        } 
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.DataSource"]/*' />
        /// <devdoc> 
        ///   Designer implementation of DataSource property that operates on
        ///   the DataSource property in the control's binding collection.
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
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.DataSourceDesigner"]/*' /> 
        /// <summary>
        /// Provides access to the designer of the DataControl, when one 
        /// is selected for data binding.
        /// </summary>
        public IDataSourceDesigner DataSourceDesigner {
            get { 
                return _dataSourceDesigner;
            } 
        } 

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.DataSourceID"]/*' /> 
        /// <summary>
        /// Implements the designer's version of the DataSourceID property.
        /// This is used to shadow the DataSourceID property of the
        /// runtime control. 
        /// </summary>
        public string DataSourceID { 
            get { 
                return ((Repeater)Component).DataSourceID;
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
                ((Repeater)Component).DataSourceID = value;
                OnDataSourceChanged(); 
                ExecuteChooseDataSourcePostSteps(); 
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

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.TemplatesExist"]/*' />
        /// <devdoc>
        /// </devdoc> 
        protected bool TemplatesExist {
            get { 
                Repeater repeater = ((Repeater)ViewControl); 
                return (repeater.ItemTemplate != null) ||
                       (repeater.HeaderTemplate != null) || 
                       (repeater.FooterTemplate != null) ||
                       (repeater.AlternatingItemTemplate != null);
            }
        } 

        private bool ConnectToDataSource() { 
            IDataSourceDesigner designer = GetDataSourceDesigner(); 

            if (_dataSourceDesigner != designer) { 
                if (_dataSourceDesigner != null) {
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged);
                }
 
                _dataSourceDesigner = designer;
                if (_dataSourceDesigner != null) { 
                    _dataSourceDesigner.DataSourceChanged += new EventHandler(DataSourceChanged); 
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

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///   Performs the cleanup of the designer class. 
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
            }
 
            base.Dispose(disposing); 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.ExecuteChooseDataSourcePostSteps"]/*' />
        /// <devdoc>
        /// Override to execute custom UI-less poststeps to choosing a data source
        /// </devdoc> 
        protected virtual void ExecuteChooseDataSourcePostSteps() {
            return; 
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
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetDesignTimeDataSource"]/*' /> 
        /// <devdoc>
        ///   Returns a sample data matching the schema of the selected datasource. 
        /// </devdoc>
        protected IEnumerable GetDesignTimeDataSource(int minimumRows) {
            IEnumerable selectedDataSource = GetResolvedSelectedDataSource();
            return GetDesignTimeDataSource(selectedDataSource, minimumRows); 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetDesignTimeDataSource1"]/*' /> 
        /// <devdoc>
        ///   Returns a sample data matching the schema of the selected datasource. 
        /// </devdoc>
        protected IEnumerable GetDesignTimeDataSource(IEnumerable selectedDataSource, int minimumRows) {
            DataTable dataTable = designTimeDataTable;
 
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
                }
            }
 
            IEnumerable liveDataSource = DesignTimeData.GetDesignTimeDataSource(dataTable, minimumRows);
            return liveDataSource; 
        } 

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetDesignTimeHtml"]/*' /> 
        /// <devdoc>
        ///   Retrieves the HTML to be used for the design time representation
        ///   of the control.
        /// </devdoc> 
        public override string GetDesignTimeHtml() {
            IEnumerable selectedDataSource = null; 
            bool hasATemplate = this.TemplatesExist; 
            Repeater repeater = (Repeater)ViewControl;
 
            string designTimeHTML;

            if (hasATemplate) {
                DesignerDataSourceView view = DesignerView; 
                IEnumerable designTimeDataSource = null;
                bool dataSourceIDChanged = false; 
                string oldDataSourceID = String.Empty; 

                if (view == null) { 
                    selectedDataSource = GetResolvedSelectedDataSource();
                    designTimeDataSource = GetDesignTimeDataSource(selectedDataSource, 5);
                }
                else { 
                    try {
                        bool dummyDataSource; 
                        designTimeDataSource = view.GetDesignTimeData(5, out dummyDataSource); 
                    }
                    catch (Exception ex) { 
                        if (Component.Site != null) {
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)Component.Site.GetService(typeof(IComponentDesignerDebugService));

                            if (debugService != null) { 
                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.GetDesignTimeData", ex.Message));
                            } 
                        } 
                    }
                } 

                try {
                    repeater.DataSource = designTimeDataSource;
                    oldDataSourceID = repeater.DataSourceID; 
                    repeater.DataSourceID = String.Empty;
                    dataSourceIDChanged = true; 
                    repeater.DataBind(); 
                    designTimeHTML = base.GetDesignTimeHtml();
                } 
                catch (Exception e) {
                    designTimeHTML = GetErrorDesignTimeHtml(e);
                }
                finally { 
                    repeater.DataSource = null;
                    if (dataSourceIDChanged) { 
                        repeater.DataSourceID = oldDataSourceID; 
                    }
                } 
            }
            else {
                designTimeHTML = GetEmptyDesignTimeHtml();
            } 

            return designTimeHTML; 
        } 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetEmptyDesignTimeHtml"]/*' />
        /// <devdoc> 
        /// </devdoc>
        protected override string GetEmptyDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Repeater_NoTemplatesInst));
        } 

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetErrorDesignTimeHtml"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        protected override string GetErrorDesignTimeHtml(Exception e) { 
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRendering));
        }

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetResolvedSelectedDataSource"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public IEnumerable GetResolvedSelectedDataSource() { 
            IEnumerable selectedDataSource = null;
 
            DataBinding binding = DataBindings["DataSource"];

            if (binding != null) {
                selectedDataSource = DesignTimeData.GetSelectedDataSource(Component, binding.Expression, DataMember); 
            }
 
            return selectedDataSource; 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.GetSelectedDataSource"]/*' />
        /// <devdoc>
        ///   Retrieves the selected datasource component from the component's container.
        /// </devdoc> 
        public object GetSelectedDataSource() {
            object selectedDataSource = null; 
 
            DataBinding binding = DataBindings["DataSource"];
 
            if (binding != null) {
                selectedDataSource = DesignTimeData.GetSelectedDataSource(Component, binding.Expression);
            }
 
            return selectedDataSource;
        } 
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///   Initializes the designer with the Repeater control that this instance
        ///   of the designer is associated with.
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(Repeater));
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
 
        private void OnAnyComponentChanged(object source, ComponentChangedEventArgs ce) {
            if (ce.Member != null) {
                Object component = ce.Component;
                Control dsControl = component as Control; 
                if (dsControl != null) {
                    if (ce.Member.Name == "ID" && Component != null) { 
                        if((string)ce.OldValue == DataSourceID || (string)ce.NewValue == DataSourceID) { 
                            ConnectToDataSource();
                            UpdateDesignTimeHtml(); 
                        }
                    }
                }
            } 
        }
 
        /// <summary> 
        ///   Fires when a component is added.  This may be our DataSourceControl
        /// </summary> 
        private void OnComponentAdded(object sender, ComponentEventArgs e) {
            IComponent component = e.Component;
            Control dsControl = component as Control;
            if (dsControl != null) { 
                if (dsControl.ID == DataSourceID) {
                    ConnectToDataSource(); 
                    UpdateDesignTimeHtml(); 
                }
            } 
        }

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.OnComponentChanged"]/*' />
        /// <devdoc> 
        ///   Handles changes made to the component. This includes changes made
        ///   in the properties window. 
        /// </devdoc> 
        public override void OnComponentChanged(object source, ComponentChangedEventArgs ce) {
            if (ce.Member != null) { 
                string memberName = ce.Member.Name;
                if (memberName.Equals("DataSource") || memberName.Equals("DataMember")) {
                    OnDataSourceChanged();
                } 
            }
 
            base.OnComponentChanged(source, ce); 
        }
 
        /// <summary>
        ///   Fires when a component is being removed.  This may be our DataSourceControl
        /// </summary>
        private void OnComponentRemoving(object sender, ComponentEventArgs e) { 
            IComponent component = e.Component;
            Control dsControl = component as Control; 
            if (dsControl != null) { 
                if (dsControl.ID == DataSourceID && Component != null) {
                    if (_dataSourceDesigner != null) { 
                        _dataSourceDesigner.DataSourceChanged -= new EventHandler(DataSourceChanged);
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
            Control dsControl = component as Control; 
            if (dsControl != null && Component != null) {
                if (dsControl.ID == DataSourceID) { 
                    IDesignerHost designerHost = (IDesignerHost)this.GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null, "Did not get DesignerHost service.");
 
                    if (designerHost != null && !designerHost.Loading) {
                        ConnectToDataSource();
                        UpdateDesignTimeHtml();
                    } 
                }
            } 
        } 

        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.OnDataSourceChanged"]/*' /> 
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Raises the DataSourceChanged event.  Public because it was shipped in V1 that way. 
        ///    </para>
        /// </devdoc> 
        public virtual void OnDataSourceChanged() { 
            ConnectToDataSource();
            designTimeDataTable = null; 
        }

        private void OnDesignerLoadComplete(object sender, EventArgs e) {
            ConnectToDataSource(); 
        }
 
        /// <include file='doc\RepeaterDesigner.uex' path='docs/doc[@for="RepeaterDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        ///   Filter the properties to replace the runtime DataSource property 
        ///   descriptor with the designer's.
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 
 
            prop = (PropertyDescriptor)properties["DataSource"];
            Debug.Assert(prop != null); 

            // we can't create the designer DataSource property based on the runtime property since theie
            // types do not match. Therefore, we have to copy over all the attributes from the runtime
            // and use them that way. 
            // Set the BrowsableAttribute for DataSource to false if DataSource is empty
            // so the user isn't confused about which DataSource property to use 
            System.ComponentModel.AttributeCollection runtimeAttributes = prop.Attributes; 
            int browsableAttributeIndex = -1;
            int attributeCount; 
            int runtimeAttributeCount = runtimeAttributes.Count;
            string dataSource = DataSource;

            // find the position of the BrowsableAttribute 
            for (int i = 0; i < runtimeAttributes.Count; i++ ) {
                if (runtimeAttributes[i] is BrowsableAttribute) { 
                    browsableAttributeIndex = i; 
                }
            } 

            // allocate the right sized array for attributes
            if (browsableAttributeIndex == -1 && dataSource.Length == 0) {
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
            if (dataSource.Length == 0) { 
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

            prop = (PropertyDescriptor)properties["DataSourceID"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataSourceIDConverter)) 
                                                 });
            properties["DataSourceID"] = prop; 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
