//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Text; 
    using System.Web;
    using System.Web.UI; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls;
    using System.Windows.Forms;
 
    /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner"]/*' />
    /// <summary> 
    /// DataBoundControlDesigner is the designer associated with a 
    /// DataBoundControl. It provides the basic shared design-time experience
    /// for all data-bound controls. 
    /// </summary>
    public class DataBoundControlDesigner : BaseDataBoundControlDesigner, IDataBindingSchemaProvider, IDataSourceProvider {

        private IDataSourceDesigner _dataSourceDesigner; 

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DataMember"]/*' /> 
        /// <summary> 
        /// Adds designer actions to the ActionLists collection.
        /// </summary> 
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                if (UseDataSourcePickerActionList) { 
                    actionLists.Add(new DataBoundControlActionList(this, DataSourceDesigner));
                } 
                actionLists.AddRange(base.ActionLists); 
                return actionLists;
            } 
        }

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DataMember1"]/*' />
        /// <summary> 
        /// Implements the designer's version of the DataMember property.
        /// This is used to shadow the DataMember property of the 
        /// runtime control. 
        /// </summary>
        public string DataMember { 
            get {
                return ((DataBoundControl)Component).DataMember;
            }
            set { 
                ((DataBoundControl)Component).DataMember = value;
                OnDataSourceChanged(true); 
            } 
        }
 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DataSourceDesigner"]/*' />
        /// <summary>
        /// Provides access to the designer of the DataControl, when one
        /// is selected for data binding. 
        /// </summary>
        public IDataSourceDesigner DataSourceDesigner { 
            get { 
                return _dataSourceDesigner;
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

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.SampleRowCount"]/*' />
        /// <summary> 
        /// Used to determine the number of sample rows the control wishes to show.
        /// </summary> 
        protected virtual int SampleRowCount { 
            get {
                return 5; 
            }
        }

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.Verbs"]/*' /> 
        /// <summary>
        /// Used to determine whether the control should render its default action lists, 
        /// containing a DataSourceID dropdown and related tasks. 
        /// </summary>
        protected virtual bool UseDataSourcePickerActionList { 
            get {
                return true;
            }
        } 

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.ConnectToDataSource"]/*' /> 
        protected override bool ConnectToDataSource() { 
            IDataSourceDesigner designer = GetDataSourceDesigner();
 
            if (_dataSourceDesigner != designer) {
                if (_dataSourceDesigner != null) {
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(OnDataSourceChanged);
                    _dataSourceDesigner.SchemaRefreshed -= new EventHandler(OnSchemaRefreshed); 
                }
 
                _dataSourceDesigner = designer; 
                if (_dataSourceDesigner != null) {
                    _dataSourceDesigner.DataSourceChanged += new EventHandler(OnDataSourceChanged); 
                    _dataSourceDesigner.SchemaRefreshed += new EventHandler(OnSchemaRefreshed);
                }

                return true; 
            }
            return false; 
        } 

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.CreateDataSource"]/*' /> 
        /// <devdoc>
        /// Calls the transacted change for creating a new datasource
        /// </devdoc>
        protected override void CreateDataSource() { 
            InvokeTransactedChange(Component, new TransactedChangeCallback(CreateDataSourceCallback), null, SR.GetString(SR.BaseDataBoundControl_CreateDataSourceTransaction));
        } 
 
        /// <devdoc>
        /// Transacted callback for creating a datasource 
        /// </devdoc>
        private bool CreateDataSourceCallback(object context) {
            string newDataSourceID;
            DialogResult result = BaseDataBoundControlDesigner.ShowCreateDataSourceDialog(this, typeof(IDataSource), true, out newDataSourceID); 
            if (newDataSourceID.Length > 0) {
                DataSourceID = newDataSourceID; 
            } 
            return (result == DialogResult.OK);
        } 

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DataBind"]/*' />
        protected override void DataBind(BaseDataBoundControl dataBoundControl) {
            IEnumerable designTimeDataSource = GetDesignTimeDataSource(); 
            string oldDataSourceID = dataBoundControl.DataSourceID;
            object oldDataSource = dataBoundControl.DataSource; 
 
            dataBoundControl.DataSource = designTimeDataSource;
            dataBoundControl.DataSourceID = String.Empty; 

            try {
                if (designTimeDataSource != null) {
                    dataBoundControl.DataBind(); 
                }
            } 
            finally { 
                dataBoundControl.DataSource = oldDataSource;
                dataBoundControl.DataSourceID = oldDataSourceID; 
            }
        }

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DisconnectFromDataSource"]/*' /> 
        protected override void DisconnectFromDataSource() {
            if (_dataSourceDesigner != null) { 
                _dataSourceDesigner.DataSourceChanged -= new EventHandler(OnDataSourceChanged); 
                _dataSourceDesigner.SchemaRefreshed -= new EventHandler(OnSchemaRefreshed);
                _dataSourceDesigner = null; 
            }
        }

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.Dispose"]/*' /> 
        protected override void Dispose(bool disposing) {
            if (disposing) { 
                if (_dataSourceDesigner != null) { 
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(OnDataSourceChanged);
                    _dataSourceDesigner.SchemaRefreshed -= new EventHandler(OnSchemaRefreshed); 
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

        protected virtual IEnumerable GetDesignTimeDataSource() { 
            bool isSampleData; // 
            IEnumerable data = null;
 
            DesignerDataSourceView view = DesignerView;
            if (view != null) {
                // Use IDataSourceDesigner interface if available
                try { 
                    data = view.GetDesignTimeData(SampleRowCount, out isSampleData);
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
            else { 
                // Use IDataSourceProvider interface if available
                IEnumerable selectedDataSource = ((IDataSourceProvider)this).GetResolvedSelectedDataSource(); 
                if (selectedDataSource != null) {
                    DataTable dataTable = DesignTimeData.CreateSampleDataTable(selectedDataSource);
                    data = DesignTimeData.GetDesignTimeDataSource(dataTable, SampleRowCount);
                    isSampleData = true; 
                }
            } 
 
            if (data != null) {
                ICollection collectionData = data as ICollection; 
                if ((collectionData == null) || (collectionData.Count > 0)) {
                    return data;
                }
            } 

            // No design time data is available from the data source, create our own sample data 
            isSampleData = true; 
            return GetSampleDataSource();
        } 

        protected virtual IEnumerable GetSampleDataSource() {
            //
            DataTable dummyDataTable = null; 
            if (((DataBoundControl)Component).DataSourceID.Length > 0) {
                dummyDataTable = DesignTimeData.CreateDummyDataBoundDataTable(); 
            } 
            else {
                dummyDataTable = DesignTimeData.CreateDummyDataTable(); 
            }
            IEnumerable liveDataSource = DesignTimeData.GetDesignTimeDataSource(dummyDataTable, SampleRowCount);
            return liveDataSource;
        } 

        private void OnDataSourceChanged(object sender, EventArgs e) { 
            OnDataSourceChanged(true); 
        }
 
        private void OnSchemaRefreshed(object sender, EventArgs e) {
            OnSchemaRefreshed();
        }
 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.PreFilterProperties"]/*' />
        /// <summary> 
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements.
        /// </summary> 
        /// <param name="properties">
        /// The properties to be filtered.
        /// </param>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);
 
            PropertyDescriptor prop; 

            prop = (PropertyDescriptor)properties["DataMember"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataMemberConverter)) 
                                                 });
            properties["DataMember"] = prop; 
 

            prop = (PropertyDescriptor)properties["DataSource"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataSourceConverter)) 
                                                 });
            properties["DataSource"] = prop; 
 

            prop = (PropertyDescriptor)properties["DataSourceID"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataSourceIDConverter)) 
                                                 });
            properties["DataSourceID"] = prop; 
        } 

        #region Implementation of IDataSourceProvider 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.GetResolvedSelectedDataSource"]/*' />
        /// <summary>
        /// Returns the selected datasource.
        /// </summary> 
        IEnumerable IDataSourceProvider.GetResolvedSelectedDataSource() {
            IEnumerable selectedDataSource = null; 
 
            DataBinding binding = DataBindings["DataSource"];
            if (binding != null) { 
                selectedDataSource = DesignTimeData.GetSelectedDataSource(((DataBoundControl)Component), binding.Expression, DataMember);
            }

            return selectedDataSource; 
        }
 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.GetSelectedDataSource"]/*' /> 
        /// <summary>
        /// Returns the selected datasource. 
        /// </summary>
        object IDataSourceProvider.GetSelectedDataSource() {
            object selectedDataSource = null;
 
            DataBinding binding = DataBindings["DataSource"];
 
            if (binding != null) { 
                selectedDataSource = DesignTimeData.GetSelectedDataSource(((DataBoundControl)Component), binding.Expression);
            } 
            return selectedDataSource;
        }
        #endregion
 
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
// <copyright file="DataBoundControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Text; 
    using System.Web;
    using System.Web.UI; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls;
    using System.Windows.Forms;
 
    /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner"]/*' />
    /// <summary> 
    /// DataBoundControlDesigner is the designer associated with a 
    /// DataBoundControl. It provides the basic shared design-time experience
    /// for all data-bound controls. 
    /// </summary>
    public class DataBoundControlDesigner : BaseDataBoundControlDesigner, IDataBindingSchemaProvider, IDataSourceProvider {

        private IDataSourceDesigner _dataSourceDesigner; 

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DataMember"]/*' /> 
        /// <summary> 
        /// Adds designer actions to the ActionLists collection.
        /// </summary> 
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                if (UseDataSourcePickerActionList) { 
                    actionLists.Add(new DataBoundControlActionList(this, DataSourceDesigner));
                } 
                actionLists.AddRange(base.ActionLists); 
                return actionLists;
            } 
        }

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DataMember1"]/*' />
        /// <summary> 
        /// Implements the designer's version of the DataMember property.
        /// This is used to shadow the DataMember property of the 
        /// runtime control. 
        /// </summary>
        public string DataMember { 
            get {
                return ((DataBoundControl)Component).DataMember;
            }
            set { 
                ((DataBoundControl)Component).DataMember = value;
                OnDataSourceChanged(true); 
            } 
        }
 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DataSourceDesigner"]/*' />
        /// <summary>
        /// Provides access to the designer of the DataControl, when one
        /// is selected for data binding. 
        /// </summary>
        public IDataSourceDesigner DataSourceDesigner { 
            get { 
                return _dataSourceDesigner;
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

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.SampleRowCount"]/*' />
        /// <summary> 
        /// Used to determine the number of sample rows the control wishes to show.
        /// </summary> 
        protected virtual int SampleRowCount { 
            get {
                return 5; 
            }
        }

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.Verbs"]/*' /> 
        /// <summary>
        /// Used to determine whether the control should render its default action lists, 
        /// containing a DataSourceID dropdown and related tasks. 
        /// </summary>
        protected virtual bool UseDataSourcePickerActionList { 
            get {
                return true;
            }
        } 

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.ConnectToDataSource"]/*' /> 
        protected override bool ConnectToDataSource() { 
            IDataSourceDesigner designer = GetDataSourceDesigner();
 
            if (_dataSourceDesigner != designer) {
                if (_dataSourceDesigner != null) {
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(OnDataSourceChanged);
                    _dataSourceDesigner.SchemaRefreshed -= new EventHandler(OnSchemaRefreshed); 
                }
 
                _dataSourceDesigner = designer; 
                if (_dataSourceDesigner != null) {
                    _dataSourceDesigner.DataSourceChanged += new EventHandler(OnDataSourceChanged); 
                    _dataSourceDesigner.SchemaRefreshed += new EventHandler(OnSchemaRefreshed);
                }

                return true; 
            }
            return false; 
        } 

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.CreateDataSource"]/*' /> 
        /// <devdoc>
        /// Calls the transacted change for creating a new datasource
        /// </devdoc>
        protected override void CreateDataSource() { 
            InvokeTransactedChange(Component, new TransactedChangeCallback(CreateDataSourceCallback), null, SR.GetString(SR.BaseDataBoundControl_CreateDataSourceTransaction));
        } 
 
        /// <devdoc>
        /// Transacted callback for creating a datasource 
        /// </devdoc>
        private bool CreateDataSourceCallback(object context) {
            string newDataSourceID;
            DialogResult result = BaseDataBoundControlDesigner.ShowCreateDataSourceDialog(this, typeof(IDataSource), true, out newDataSourceID); 
            if (newDataSourceID.Length > 0) {
                DataSourceID = newDataSourceID; 
            } 
            return (result == DialogResult.OK);
        } 

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DataBind"]/*' />
        protected override void DataBind(BaseDataBoundControl dataBoundControl) {
            IEnumerable designTimeDataSource = GetDesignTimeDataSource(); 
            string oldDataSourceID = dataBoundControl.DataSourceID;
            object oldDataSource = dataBoundControl.DataSource; 
 
            dataBoundControl.DataSource = designTimeDataSource;
            dataBoundControl.DataSourceID = String.Empty; 

            try {
                if (designTimeDataSource != null) {
                    dataBoundControl.DataBind(); 
                }
            } 
            finally { 
                dataBoundControl.DataSource = oldDataSource;
                dataBoundControl.DataSourceID = oldDataSourceID; 
            }
        }

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.DisconnectFromDataSource"]/*' /> 
        protected override void DisconnectFromDataSource() {
            if (_dataSourceDesigner != null) { 
                _dataSourceDesigner.DataSourceChanged -= new EventHandler(OnDataSourceChanged); 
                _dataSourceDesigner.SchemaRefreshed -= new EventHandler(OnSchemaRefreshed);
                _dataSourceDesigner = null; 
            }
        }

        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.Dispose"]/*' /> 
        protected override void Dispose(bool disposing) {
            if (disposing) { 
                if (_dataSourceDesigner != null) { 
                    _dataSourceDesigner.DataSourceChanged -= new EventHandler(OnDataSourceChanged);
                    _dataSourceDesigner.SchemaRefreshed -= new EventHandler(OnSchemaRefreshed); 
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

        protected virtual IEnumerable GetDesignTimeDataSource() { 
            bool isSampleData; // 
            IEnumerable data = null;
 
            DesignerDataSourceView view = DesignerView;
            if (view != null) {
                // Use IDataSourceDesigner interface if available
                try { 
                    data = view.GetDesignTimeData(SampleRowCount, out isSampleData);
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
            else { 
                // Use IDataSourceProvider interface if available
                IEnumerable selectedDataSource = ((IDataSourceProvider)this).GetResolvedSelectedDataSource(); 
                if (selectedDataSource != null) {
                    DataTable dataTable = DesignTimeData.CreateSampleDataTable(selectedDataSource);
                    data = DesignTimeData.GetDesignTimeDataSource(dataTable, SampleRowCount);
                    isSampleData = true; 
                }
            } 
 
            if (data != null) {
                ICollection collectionData = data as ICollection; 
                if ((collectionData == null) || (collectionData.Count > 0)) {
                    return data;
                }
            } 

            // No design time data is available from the data source, create our own sample data 
            isSampleData = true; 
            return GetSampleDataSource();
        } 

        protected virtual IEnumerable GetSampleDataSource() {
            //
            DataTable dummyDataTable = null; 
            if (((DataBoundControl)Component).DataSourceID.Length > 0) {
                dummyDataTable = DesignTimeData.CreateDummyDataBoundDataTable(); 
            } 
            else {
                dummyDataTable = DesignTimeData.CreateDummyDataTable(); 
            }
            IEnumerable liveDataSource = DesignTimeData.GetDesignTimeDataSource(dummyDataTable, SampleRowCount);
            return liveDataSource;
        } 

        private void OnDataSourceChanged(object sender, EventArgs e) { 
            OnDataSourceChanged(true); 
        }
 
        private void OnSchemaRefreshed(object sender, EventArgs e) {
            OnSchemaRefreshed();
        }
 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.PreFilterProperties"]/*' />
        /// <summary> 
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements.
        /// </summary> 
        /// <param name="properties">
        /// The properties to be filtered.
        /// </param>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);
 
            PropertyDescriptor prop; 

            prop = (PropertyDescriptor)properties["DataMember"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataMemberConverter)) 
                                                 });
            properties["DataMember"] = prop; 
 

            prop = (PropertyDescriptor)properties["DataSource"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataSourceConverter)) 
                                                 });
            properties["DataSource"] = prop; 
 

            prop = (PropertyDescriptor)properties["DataSourceID"]; 
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop,
                                                 new Attribute[] {
                                                     new TypeConverterAttribute(typeof(DataSourceIDConverter)) 
                                                 });
            properties["DataSourceID"] = prop; 
        } 

        #region Implementation of IDataSourceProvider 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.GetResolvedSelectedDataSource"]/*' />
        /// <summary>
        /// Returns the selected datasource.
        /// </summary> 
        IEnumerable IDataSourceProvider.GetResolvedSelectedDataSource() {
            IEnumerable selectedDataSource = null; 
 
            DataBinding binding = DataBindings["DataSource"];
            if (binding != null) { 
                selectedDataSource = DesignTimeData.GetSelectedDataSource(((DataBoundControl)Component), binding.Expression, DataMember);
            }

            return selectedDataSource; 
        }
 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.GetSelectedDataSource"]/*' /> 
        /// <summary>
        /// Returns the selected datasource. 
        /// </summary>
        object IDataSourceProvider.GetSelectedDataSource() {
            object selectedDataSource = null;
 
            DataBinding binding = DataBindings["DataSource"];
 
            if (binding != null) { 
                selectedDataSource = DesignTimeData.GetSelectedDataSource(((DataBoundControl)Component), binding.Expression);
            } 
            return selectedDataSource;
        }
        #endregion
 
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
