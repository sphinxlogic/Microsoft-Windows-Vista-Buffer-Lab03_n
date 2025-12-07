//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Security.Permissions;
    using System.Web.Util;
    using System.Web.UI.WebControls.Adapters; 

 
    /// <summary> 
    /// A DataBoundControl is bound to a data source and generates its
    /// user interface (or child control hierarchy typically), by enumerating 
    /// the items in the data source it is bound to.
    /// DataBoundControl is an abstract base class that defines the common
    /// characteristics of all controls that use a list as a data source, such as
    /// DataGrid, DataBoundTable, ListBox etc. It encapsulates the logic 
    /// of how a data-bound control binds to collections or DataControl instances.
    /// </summary> 
 
    [
    Designer("System.Web.UI.Design.WebControls.DataBoundControlDesigner, " + AssemblyRef.SystemDesign), 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public abstract class DataBoundControl : BaseDataBoundControl { 

        private DataSourceView _currentView; 
        private bool _currentViewIsFromDataSourceID; 
        private bool _currentViewValid;
        private IDataSource _currentDataSource; 
        private bool _currentDataSourceValid;
        private DataSourceSelectArguments _arguments;
        private bool _pagePreLoadFired;
        private bool _ignoreDataSourceViewChanged; 

        private const string DataBoundViewStateKey = "_!DataBound"; 
 

        /// <summary> 
        /// The name of the list that the DataBoundControl should bind to when
        /// its data source contains more than one list of data items.
        /// </summary>
        [ 
        DefaultValue(""),
        Themeable(false), 
        WebCategory("Data"), 
        WebSysDescription(SR.DataBoundControl_DataMember)
        ] 
        public virtual string DataMember {
            get {
                object o = ViewState["DataMember"];
                if (o != null) { 
                    return (string)o;
                } 
                return String.Empty; 
            }
            set { 
                ViewState["DataMember"] = value;
                OnDataPropertyChanged();
            }
        } 

 
        /// <internalonly /> 
        [
        IDReferenceProperty(typeof(DataSourceControl)) 
        ]
        public override string DataSourceID {
            get {
                return base.DataSourceID; 
            }
            set { 
                base.DataSourceID = value; 
            }
        } 

        protected DataSourceSelectArguments SelectArguments {
            get {
                if (_arguments == null) { 
                    _arguments = CreateDataSourceSelectArguments();
                } 
                return _arguments; 
            }
        } 

        /// <devdoc>
        /// Connects this data bound control to the appropriate DataSourceView
        /// and hooks up the appropriate event listener for the 
        /// DataSourceViewChanged event. The return value is the new view (if
        /// any) that was connected to. An exception is thrown if there is 
        /// a problem finding the requested view or data source. 
        /// </devdoc>
        private DataSourceView ConnectToDataSourceView() { 
            if (_currentViewValid && !DesignMode) {
                // If the current view is correct, there is no need to reconnect
                return _currentView;
            } 

            // Disconnect from old view, if necessary 
            if ((_currentView != null) && (_currentViewIsFromDataSourceID)) { 
                // We only care about this event if we are bound through the DataSourceID property
                _currentView.DataSourceViewChanged -= new EventHandler(OnDataSourceViewChanged); 
            }

            // Connect to new view
            _currentDataSource = GetDataSource(); 
            string dataMember = DataMember;
 
            if (_currentDataSource == null) { 
                // DataSource control was not found, construct a temporary data source to wrap the data
                _currentDataSource = new ReadOnlyDataSource(DataSource, dataMember); 
            }
            else {
                // Ensure that both DataSourceID as well as DataSource are not set at the same time
                if (DataSource != null) { 
                    throw new InvalidOperationException(SR.GetString(SR.DataControl_MultipleDataSources, ID));
                } 
            } 
            _currentDataSourceValid = true;
 
            // IDataSource was found, extract the appropriate view and return it
            DataSourceView newView = _currentDataSource.GetView(dataMember);
            if (newView == null) {
                throw new InvalidOperationException(SR.GetString(SR.DataControl_ViewNotFound, ID)); 
            }
 
            _currentViewIsFromDataSourceID = IsBoundUsingDataSourceID; 
            _currentView = newView;
            if ((_currentView != null) && (_currentViewIsFromDataSourceID)) { 
                // We only care about this event if we are bound through the DataSourceID property
                _currentView.DataSourceViewChanged += new EventHandler(OnDataSourceViewChanged);
            }
            _currentViewValid = true; 

            return _currentView; 
        } 

        /// <summary> 
        ///  Override to create the DataSourceSelectArguments that will be passed to the view's Select command.
        /// </summary>
        protected virtual DataSourceSelectArguments CreateDataSourceSelectArguments() {
            return DataSourceSelectArguments.Empty; 
        }
 
 
        /// <devdoc>
        /// Gets the DataSourceView of the IDataSource that this control is 
        /// bound to, if any.
        /// </devdoc>
        protected virtual DataSourceView GetData() {
            DataSourceView view = ConnectToDataSourceView(); 

            Debug.Assert(_currentViewValid); 
 
            return view;
        } 


        /// <devdoc>
        /// Gets the IDataSource that this control is bound to, if any. 
        /// Because this method can be called directly by derived classes, it's virtual so data can be retrieved
        /// from data sources that don't live on the page. 
        /// </devdoc> 
        protected virtual IDataSource GetDataSource() {
            if (!DesignMode && _currentDataSourceValid && (_currentDataSource != null)) { 
                return _currentDataSource;
            }

            IDataSource ds = null; 
            string dataSourceID = DataSourceID;
 
            if (dataSourceID.Length != 0) { 
                // Try to find a DataSource control with the ID specified in DataSourceID
                Control control = DataBoundControlHelper.FindControl(this, dataSourceID); 
                if (control == null) {
                    throw new HttpException(SR.GetString(SR.DataControl_DataSourceDoesntExist, ID, dataSourceID));
                }
                ds = control as IDataSource; 
                if (ds == null) {
                    throw new HttpException(SR.GetString(SR.DataControl_DataSourceIDMustBeDataControl, ID, dataSourceID)); 
                } 
            }
            return ds; 
        }

        protected void MarkAsDataBound() {
            ViewState[DataBoundViewStateKey] = true; 
        }
 
 
        protected override void OnDataPropertyChanged() {
            _currentViewValid = false; 
            _currentDataSourceValid = false;
            base.OnDataPropertyChanged();
        }
 

        /// <devdoc> 
        ///  This method is called when the DataSourceView raises a DataSourceViewChanged event. 
        /// </devdoc>
        protected virtual void OnDataSourceViewChanged(object sender, EventArgs e) { 
            if (!_ignoreDataSourceViewChanged) {
                RequiresDataBinding = true;
            }
        } 

        private void OnDataSourceViewSelectCallback(IEnumerable data) { 
            _ignoreDataSourceViewChanged = false; 
            // We only call OnDataBinding here if we haven't done it already in PerformSelect().
            if (DataSourceID.Length > 0) { 
                OnDataBinding(EventArgs.Empty);
            }

            if(_adapter != null) { 
                DataBoundControlAdapter dataBoundControlAdapter = _adapter as DataBoundControlAdapter;
                if(dataBoundControlAdapter != null) { 
                    dataBoundControlAdapter.PerformDataBinding(data); 
                }
                else { 
                    PerformDataBinding(data);
                }
            }
            else { 
                PerformDataBinding(data);
            } 
            OnDataBound(EventArgs.Empty); 
        }
 

        protected internal override void OnLoad(EventArgs e) {
            ConfirmInitState();
            ConnectToDataSourceView(); 

            if (Page != null && !_pagePreLoadFired && ViewState[DataBoundViewStateKey] == null) { 
                // If the control was added after PagePreLoad, we still need to databind it because it missed its 
                // first change in PagePreLoad.  If this control was created by a call to a parent control's DataBind
                // in Page_Load (with is relatively common), this control will already have been databound even 
                // though pagePreLoad never fired and the page isn't a postback.
                if (!Page.IsPostBack) {
                    RequiresDataBinding = true;
                } 
                // If the control was added to the page after page.PreLoad, we'll never get the event and we'll
                // never databind the control.  So if we're catching up and Load happens but PreLoad never happened, 
                // call DataBind.  This may make the control get databound twice if the user called DataBind on the control 
                // directly in Page.OnLoad, but better to bind twice than never to bind at all.
                else if (IsViewStateEnabled) { 
                    RequiresDataBinding = true;
                }
            }
 
            base.OnLoad(e);
        } 
 
        protected override void OnPagePreLoad(object sender, EventArgs e) {
            base.OnPagePreLoad(sender, e); 

            if (Page != null) {
                // Setting RequiresDataBinding to true in OnLoad is too late because the OnLoad page event
                // happens before the control.OnLoad method gets called.  So a page_load handler on the page 
                // that calls DataBind won't prevent DataBind from getting called again in PreRender.
                if (!Page.IsPostBack) { 
                    RequiresDataBinding = true; 
                }
                // If this is a postback and viewstate is enabled, but we have never bound the control 
                // before, it is probably because its visibility was changed in the postback.  In this
                // case, we need to bind the control or it will never appear.  This is a common scenario
                // for Wizard and MultiView.
                else if (IsViewStateEnabled && ViewState[DataBoundViewStateKey] == null) { 
                    RequiresDataBinding = true;
                } 
            } 
            _pagePreLoadFired = true;
        } 



        /// <devdoc> 
        ///  This method should be overridden by databound controls to perform their databinding.
        ///  Overriding this method instead of DataBind() will allow the DataBound control developer 
        ///  to not worry about DataBinding events to be called in the right order. 
        /// </devdoc>
        protected internal virtual void PerformDataBinding(IEnumerable data) { 
        }

        /// <summary>
        ///  Issues an asynchronous request for data to the data source using the arguments from CreateDataSourceSelectArguments. 
        /// </summary>
        protected override void PerformSelect() { 
            // We need to call OnDataBinding here if we're potentially bound to a DataSource (instead of a DataSourceID) 
            // because the databinding statement that is the datasource needs to be evaluated before the call to GetData()
            // happens, because we don't rebind when the datasource is changed unless DataSourceID.Length > 0. 
            if (DataSourceID.Length == 0) {
                OnDataBinding(EventArgs.Empty);
            }
            DataSourceView view = GetData(); 
            _arguments = CreateDataSourceSelectArguments();
            _ignoreDataSourceViewChanged = true; 
            RequiresDataBinding = false; 
            MarkAsDataBound();
            view.Select(_arguments, OnDataSourceViewSelectCallback); 
        }


        protected override void ValidateDataSource(object dataSource) { 
            if ((dataSource == null) ||
                (dataSource is IListSource) || 
                (dataSource is IEnumerable) || 
                (dataSource is IDataSource)) {
                return; 
            }
            throw new InvalidOperationException(SR.GetString(SR.DataBoundControl_InvalidDataSourceType));
        }
    } 
}
 
//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Security.Permissions;
    using System.Web.Util;
    using System.Web.UI.WebControls.Adapters; 

 
    /// <summary> 
    /// A DataBoundControl is bound to a data source and generates its
    /// user interface (or child control hierarchy typically), by enumerating 
    /// the items in the data source it is bound to.
    /// DataBoundControl is an abstract base class that defines the common
    /// characteristics of all controls that use a list as a data source, such as
    /// DataGrid, DataBoundTable, ListBox etc. It encapsulates the logic 
    /// of how a data-bound control binds to collections or DataControl instances.
    /// </summary> 
 
    [
    Designer("System.Web.UI.Design.WebControls.DataBoundControlDesigner, " + AssemblyRef.SystemDesign), 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public abstract class DataBoundControl : BaseDataBoundControl { 

        private DataSourceView _currentView; 
        private bool _currentViewIsFromDataSourceID; 
        private bool _currentViewValid;
        private IDataSource _currentDataSource; 
        private bool _currentDataSourceValid;
        private DataSourceSelectArguments _arguments;
        private bool _pagePreLoadFired;
        private bool _ignoreDataSourceViewChanged; 

        private const string DataBoundViewStateKey = "_!DataBound"; 
 

        /// <summary> 
        /// The name of the list that the DataBoundControl should bind to when
        /// its data source contains more than one list of data items.
        /// </summary>
        [ 
        DefaultValue(""),
        Themeable(false), 
        WebCategory("Data"), 
        WebSysDescription(SR.DataBoundControl_DataMember)
        ] 
        public virtual string DataMember {
            get {
                object o = ViewState["DataMember"];
                if (o != null) { 
                    return (string)o;
                } 
                return String.Empty; 
            }
            set { 
                ViewState["DataMember"] = value;
                OnDataPropertyChanged();
            }
        } 

 
        /// <internalonly /> 
        [
        IDReferenceProperty(typeof(DataSourceControl)) 
        ]
        public override string DataSourceID {
            get {
                return base.DataSourceID; 
            }
            set { 
                base.DataSourceID = value; 
            }
        } 

        protected DataSourceSelectArguments SelectArguments {
            get {
                if (_arguments == null) { 
                    _arguments = CreateDataSourceSelectArguments();
                } 
                return _arguments; 
            }
        } 

        /// <devdoc>
        /// Connects this data bound control to the appropriate DataSourceView
        /// and hooks up the appropriate event listener for the 
        /// DataSourceViewChanged event. The return value is the new view (if
        /// any) that was connected to. An exception is thrown if there is 
        /// a problem finding the requested view or data source. 
        /// </devdoc>
        private DataSourceView ConnectToDataSourceView() { 
            if (_currentViewValid && !DesignMode) {
                // If the current view is correct, there is no need to reconnect
                return _currentView;
            } 

            // Disconnect from old view, if necessary 
            if ((_currentView != null) && (_currentViewIsFromDataSourceID)) { 
                // We only care about this event if we are bound through the DataSourceID property
                _currentView.DataSourceViewChanged -= new EventHandler(OnDataSourceViewChanged); 
            }

            // Connect to new view
            _currentDataSource = GetDataSource(); 
            string dataMember = DataMember;
 
            if (_currentDataSource == null) { 
                // DataSource control was not found, construct a temporary data source to wrap the data
                _currentDataSource = new ReadOnlyDataSource(DataSource, dataMember); 
            }
            else {
                // Ensure that both DataSourceID as well as DataSource are not set at the same time
                if (DataSource != null) { 
                    throw new InvalidOperationException(SR.GetString(SR.DataControl_MultipleDataSources, ID));
                } 
            } 
            _currentDataSourceValid = true;
 
            // IDataSource was found, extract the appropriate view and return it
            DataSourceView newView = _currentDataSource.GetView(dataMember);
            if (newView == null) {
                throw new InvalidOperationException(SR.GetString(SR.DataControl_ViewNotFound, ID)); 
            }
 
            _currentViewIsFromDataSourceID = IsBoundUsingDataSourceID; 
            _currentView = newView;
            if ((_currentView != null) && (_currentViewIsFromDataSourceID)) { 
                // We only care about this event if we are bound through the DataSourceID property
                _currentView.DataSourceViewChanged += new EventHandler(OnDataSourceViewChanged);
            }
            _currentViewValid = true; 

            return _currentView; 
        } 

        /// <summary> 
        ///  Override to create the DataSourceSelectArguments that will be passed to the view's Select command.
        /// </summary>
        protected virtual DataSourceSelectArguments CreateDataSourceSelectArguments() {
            return DataSourceSelectArguments.Empty; 
        }
 
 
        /// <devdoc>
        /// Gets the DataSourceView of the IDataSource that this control is 
        /// bound to, if any.
        /// </devdoc>
        protected virtual DataSourceView GetData() {
            DataSourceView view = ConnectToDataSourceView(); 

            Debug.Assert(_currentViewValid); 
 
            return view;
        } 


        /// <devdoc>
        /// Gets the IDataSource that this control is bound to, if any. 
        /// Because this method can be called directly by derived classes, it's virtual so data can be retrieved
        /// from data sources that don't live on the page. 
        /// </devdoc> 
        protected virtual IDataSource GetDataSource() {
            if (!DesignMode && _currentDataSourceValid && (_currentDataSource != null)) { 
                return _currentDataSource;
            }

            IDataSource ds = null; 
            string dataSourceID = DataSourceID;
 
            if (dataSourceID.Length != 0) { 
                // Try to find a DataSource control with the ID specified in DataSourceID
                Control control = DataBoundControlHelper.FindControl(this, dataSourceID); 
                if (control == null) {
                    throw new HttpException(SR.GetString(SR.DataControl_DataSourceDoesntExist, ID, dataSourceID));
                }
                ds = control as IDataSource; 
                if (ds == null) {
                    throw new HttpException(SR.GetString(SR.DataControl_DataSourceIDMustBeDataControl, ID, dataSourceID)); 
                } 
            }
            return ds; 
        }

        protected void MarkAsDataBound() {
            ViewState[DataBoundViewStateKey] = true; 
        }
 
 
        protected override void OnDataPropertyChanged() {
            _currentViewValid = false; 
            _currentDataSourceValid = false;
            base.OnDataPropertyChanged();
        }
 

        /// <devdoc> 
        ///  This method is called when the DataSourceView raises a DataSourceViewChanged event. 
        /// </devdoc>
        protected virtual void OnDataSourceViewChanged(object sender, EventArgs e) { 
            if (!_ignoreDataSourceViewChanged) {
                RequiresDataBinding = true;
            }
        } 

        private void OnDataSourceViewSelectCallback(IEnumerable data) { 
            _ignoreDataSourceViewChanged = false; 
            // We only call OnDataBinding here if we haven't done it already in PerformSelect().
            if (DataSourceID.Length > 0) { 
                OnDataBinding(EventArgs.Empty);
            }

            if(_adapter != null) { 
                DataBoundControlAdapter dataBoundControlAdapter = _adapter as DataBoundControlAdapter;
                if(dataBoundControlAdapter != null) { 
                    dataBoundControlAdapter.PerformDataBinding(data); 
                }
                else { 
                    PerformDataBinding(data);
                }
            }
            else { 
                PerformDataBinding(data);
            } 
            OnDataBound(EventArgs.Empty); 
        }
 

        protected internal override void OnLoad(EventArgs e) {
            ConfirmInitState();
            ConnectToDataSourceView(); 

            if (Page != null && !_pagePreLoadFired && ViewState[DataBoundViewStateKey] == null) { 
                // If the control was added after PagePreLoad, we still need to databind it because it missed its 
                // first change in PagePreLoad.  If this control was created by a call to a parent control's DataBind
                // in Page_Load (with is relatively common), this control will already have been databound even 
                // though pagePreLoad never fired and the page isn't a postback.
                if (!Page.IsPostBack) {
                    RequiresDataBinding = true;
                } 
                // If the control was added to the page after page.PreLoad, we'll never get the event and we'll
                // never databind the control.  So if we're catching up and Load happens but PreLoad never happened, 
                // call DataBind.  This may make the control get databound twice if the user called DataBind on the control 
                // directly in Page.OnLoad, but better to bind twice than never to bind at all.
                else if (IsViewStateEnabled) { 
                    RequiresDataBinding = true;
                }
            }
 
            base.OnLoad(e);
        } 
 
        protected override void OnPagePreLoad(object sender, EventArgs e) {
            base.OnPagePreLoad(sender, e); 

            if (Page != null) {
                // Setting RequiresDataBinding to true in OnLoad is too late because the OnLoad page event
                // happens before the control.OnLoad method gets called.  So a page_load handler on the page 
                // that calls DataBind won't prevent DataBind from getting called again in PreRender.
                if (!Page.IsPostBack) { 
                    RequiresDataBinding = true; 
                }
                // If this is a postback and viewstate is enabled, but we have never bound the control 
                // before, it is probably because its visibility was changed in the postback.  In this
                // case, we need to bind the control or it will never appear.  This is a common scenario
                // for Wizard and MultiView.
                else if (IsViewStateEnabled && ViewState[DataBoundViewStateKey] == null) { 
                    RequiresDataBinding = true;
                } 
            } 
            _pagePreLoadFired = true;
        } 



        /// <devdoc> 
        ///  This method should be overridden by databound controls to perform their databinding.
        ///  Overriding this method instead of DataBind() will allow the DataBound control developer 
        ///  to not worry about DataBinding events to be called in the right order. 
        /// </devdoc>
        protected internal virtual void PerformDataBinding(IEnumerable data) { 
        }

        /// <summary>
        ///  Issues an asynchronous request for data to the data source using the arguments from CreateDataSourceSelectArguments. 
        /// </summary>
        protected override void PerformSelect() { 
            // We need to call OnDataBinding here if we're potentially bound to a DataSource (instead of a DataSourceID) 
            // because the databinding statement that is the datasource needs to be evaluated before the call to GetData()
            // happens, because we don't rebind when the datasource is changed unless DataSourceID.Length > 0. 
            if (DataSourceID.Length == 0) {
                OnDataBinding(EventArgs.Empty);
            }
            DataSourceView view = GetData(); 
            _arguments = CreateDataSourceSelectArguments();
            _ignoreDataSourceViewChanged = true; 
            RequiresDataBinding = false; 
            MarkAsDataBound();
            view.Select(_arguments, OnDataSourceViewSelectCallback); 
        }


        protected override void ValidateDataSource(object dataSource) { 
            if ((dataSource == null) ||
                (dataSource is IListSource) || 
                (dataSource is IEnumerable) || 
                (dataSource is IDataSource)) {
                return; 
            }
            throw new InvalidOperationException(SR.GetString(SR.DataBoundControl_InvalidDataSourceType));
        }
    } 
}
 
