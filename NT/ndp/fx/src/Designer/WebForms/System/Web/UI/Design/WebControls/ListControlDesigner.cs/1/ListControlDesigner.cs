//------------------------------------------------------------------------------ 
// <copyright file="ListControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.IO;
    using System.Data; 
    using System.Web.UI; 
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms.Design;

    using DialogResult = System.Windows.Forms.DialogResult; 
    using AttributeCollection = System.ComponentModel.AttributeCollection;
    using DataBinding = System.Web.UI.DataBinding; 
 
    /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       This is the base class for all <see cref='System.Web.UI.WebControls.ListControl'/>
    ///       designers.
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)] 
    public class ListControlDesigner : DataBoundControlDesigner {
 
        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.ListControlDesigner"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.WebControls.ListControlDesigner'/> 
        ///       .
        ///    </para> 
        /// </devdoc> 
        public ListControlDesigner() {
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.ActionLists"]/*' />
        /// <summary>
        /// Adds designer actions to the ActionLists collection. 
        /// </summary>
        public override DesignerActionListCollection ActionLists { 
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new ListControlActionList(this, DataSourceDesigner));
                return actionLists;
            }
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.DataValueField"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public string DataValueField { 
            get {
                return ((ListControl)Component).DataValueField;
            }
            set { 
                ((ListControl)Component).DataValueField = value;
            } 
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.DataTextField"]/*' /> 
        /// <devdoc>
        ///   Retrieves the HTML to be used for the design time representation of the control runtime.
        /// </devdoc>
        public string DataTextField { 
            get {
                return ((ListControl)Component).DataTextField; 
            } 
            set {
                ((ListControl)Component).DataTextField = value; 
            }
        }

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.UseDataSourcePickerActionList"]/*' /> 
        /// <summary>
        /// Used to determine whether the control should render its default action lists, 
        /// containing a DataSourceID dropdown and related tasks. 
        /// </summary>
        protected override bool UseDataSourcePickerActionList { 
            get {
                return false;
            }
        } 

        internal void ConnectToDataSourceAction() { 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConnectToDataSourceCallback), null, SR.GetString(SR.ListControlDesigner_ConnectToDataSource)); 
        }
 
        private bool ConnectToDataSourceCallback(object context) {
            ListControlConnectToDataSourceDialog dialog = new ListControlConnectToDataSourceDialog(this);
            DialogResult result = UIServiceHelper.ShowDialog(Component.Site, dialog);
            return (result == DialogResult.OK); 
        }
 
        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.DataBind"]/*' /> 
        /// <summary>
        /// DataBinds the given control to get design time rendering 
        /// </summary>
        protected override void DataBind(BaseDataBoundControl dataBoundControl) {
            // We don't need to databind here because we just added items to the items collection
            return; 
        }
 
        /// <summary> 
        /// Starts collection editor for List Items
        /// </summary> 
        internal void EditItems() {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)["Items"];
            Debug.Assert(descriptor != null, "Expected to find Items property on ListControl");
            InvokeTransactedChange(Component, new TransactedChangeCallback(EditItemsCallback), descriptor, SR.GetString(SR.ListControlDesigner_EditItems), descriptor); 
        }
 
        /// <devdoc> 
        /// Transacted callback to invoke the Edit Items dialog.
        /// </devdoc> 
        private bool EditItemsCallback(object context) {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null);
 
            PropertyDescriptor descriptor = (PropertyDescriptor)context;
            ListItemsCollectionEditor editor = new ListItemsCollectionEditor(typeof(ListItemCollection)); 
            editor.EditValue(new TypeDescriptorContext(designerHost, descriptor, Component), 
                             new WindowsFormsEditorServiceHelper(this), descriptor.GetValue(Component));
            return true; 
        }

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc> 
        ///    <para>Gets the HTML to be used for the design time representation of the control runtime.</para>
        /// </devdoc> 
        public override string GetDesignTimeHtml() { 
            try {
                ListControl listControl = (ListControl)ViewControl; 

                ListItemCollection items = listControl.Items;
                bool isDataBound = IsDataBound();
                if (items.Count == 0 || isDataBound) { 
                    if (isDataBound) {
                        items.Clear(); 
                        items.Add(SR.GetString(SR.Sample_Databound_Text)); 
                    }
                    else { 
                        items.Add(SR.GetString(SR.Sample_Unbound_Text));
                    }
                }
 
                return base.GetDesignTimeHtml();
            } 
            catch (Exception e) { 
                return GetErrorDesignTimeHtml(e);
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        public IEnumerable GetResolvedSelectedDataSource() {
            return ((IDataSourceProvider)this).GetResolvedSelectedDataSource(); 
        } 

        /// <devdoc> 
        /// </devdoc>
        public object GetSelectedDataSource() {
            return ((IDataSourceProvider)this).GetSelectedDataSource();
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.Initialize"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Initializes the component for design. 
        ///    </para>
        /// </devdoc>
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(ListControl)); 
            base.Initialize(component);
        } 
 
        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.IsDataBound"]/*' />
        /// <devdoc> 
        ///   Return true if the control is databound.
        /// </devdoc>
        private bool IsDataBound() {
            DataBinding dataSourceBinding = DataBindings["DataSource"]; 

            return (dataSourceBinding != null || DataSourceID.Length > 0); 
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.OnDataSourceChanged"]/*' /> 
        /// <devdoc>
        /// This method is called when the data source the control is
        /// connected to changes. Override this method to perform any
        /// additional actions that your designer requires. Make sure to call 
        /// the base implementation as well.
        /// This method exists because it shipped in V1.  Removing it creates a breaking change. 
        /// Then call the base class' implementation. 
        /// </devdoc>
        public virtual void OnDataSourceChanged() { 
            // Call the base protected implementation to inherit the default behavior
            // of BaseDataBoundControlDesigner.
            base.OnDataSourceChanged(true);
        } 

        /// <devdoc> 
        /// Called by data bound control designers to raise the data source 
        /// changed event so that listeners can be notified.
        /// </devdoc> 
        protected override void OnDataSourceChanged(bool forceUpdateView) {
            // Call the public OnDataSourceChanged method without parameters introduced in this class,
            // which exists for back-compat.  This method is new in V2.
            // Derived classes overrode OnDataSourceChanged(), so make sure that gets called. 
            this.OnDataSourceChanged();
        } 
 
        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Filters the properties to replace the runtime DataSource property
        ///       descriptor with the designer's.
        ///    </para> 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 
            Attribute[] fieldPropAttrs = new Attribute[] {
                                             new TypeConverterAttribute(typeof(DataFieldConverter))
                                         };
 
            prop = (PropertyDescriptor)properties["DataTextField"];
            Debug.Assert(prop != null); 
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop, fieldPropAttrs); 
            properties["DataTextField"] = prop;
 
            prop = (PropertyDescriptor)properties["DataValueField"];
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop, fieldPropAttrs);
            properties["DataValueField"] = prop; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ListControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.IO;
    using System.Data; 
    using System.Web.UI; 
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms.Design;

    using DialogResult = System.Windows.Forms.DialogResult; 
    using AttributeCollection = System.ComponentModel.AttributeCollection;
    using DataBinding = System.Web.UI.DataBinding; 
 
    /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       This is the base class for all <see cref='System.Web.UI.WebControls.ListControl'/>
    ///       designers.
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)] 
    public class ListControlDesigner : DataBoundControlDesigner {
 
        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.ListControlDesigner"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.WebControls.ListControlDesigner'/> 
        ///       .
        ///    </para> 
        /// </devdoc> 
        public ListControlDesigner() {
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.ActionLists"]/*' />
        /// <summary>
        /// Adds designer actions to the ActionLists collection. 
        /// </summary>
        public override DesignerActionListCollection ActionLists { 
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new ListControlActionList(this, DataSourceDesigner));
                return actionLists;
            }
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.DataValueField"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public string DataValueField { 
            get {
                return ((ListControl)Component).DataValueField;
            }
            set { 
                ((ListControl)Component).DataValueField = value;
            } 
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.DataTextField"]/*' /> 
        /// <devdoc>
        ///   Retrieves the HTML to be used for the design time representation of the control runtime.
        /// </devdoc>
        public string DataTextField { 
            get {
                return ((ListControl)Component).DataTextField; 
            } 
            set {
                ((ListControl)Component).DataTextField = value; 
            }
        }

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.UseDataSourcePickerActionList"]/*' /> 
        /// <summary>
        /// Used to determine whether the control should render its default action lists, 
        /// containing a DataSourceID dropdown and related tasks. 
        /// </summary>
        protected override bool UseDataSourcePickerActionList { 
            get {
                return false;
            }
        } 

        internal void ConnectToDataSourceAction() { 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConnectToDataSourceCallback), null, SR.GetString(SR.ListControlDesigner_ConnectToDataSource)); 
        }
 
        private bool ConnectToDataSourceCallback(object context) {
            ListControlConnectToDataSourceDialog dialog = new ListControlConnectToDataSourceDialog(this);
            DialogResult result = UIServiceHelper.ShowDialog(Component.Site, dialog);
            return (result == DialogResult.OK); 
        }
 
        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.DataBind"]/*' /> 
        /// <summary>
        /// DataBinds the given control to get design time rendering 
        /// </summary>
        protected override void DataBind(BaseDataBoundControl dataBoundControl) {
            // We don't need to databind here because we just added items to the items collection
            return; 
        }
 
        /// <summary> 
        /// Starts collection editor for List Items
        /// </summary> 
        internal void EditItems() {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)["Items"];
            Debug.Assert(descriptor != null, "Expected to find Items property on ListControl");
            InvokeTransactedChange(Component, new TransactedChangeCallback(EditItemsCallback), descriptor, SR.GetString(SR.ListControlDesigner_EditItems), descriptor); 
        }
 
        /// <devdoc> 
        /// Transacted callback to invoke the Edit Items dialog.
        /// </devdoc> 
        private bool EditItemsCallback(object context) {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null);
 
            PropertyDescriptor descriptor = (PropertyDescriptor)context;
            ListItemsCollectionEditor editor = new ListItemsCollectionEditor(typeof(ListItemCollection)); 
            editor.EditValue(new TypeDescriptorContext(designerHost, descriptor, Component), 
                             new WindowsFormsEditorServiceHelper(this), descriptor.GetValue(Component));
            return true; 
        }

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc> 
        ///    <para>Gets the HTML to be used for the design time representation of the control runtime.</para>
        /// </devdoc> 
        public override string GetDesignTimeHtml() { 
            try {
                ListControl listControl = (ListControl)ViewControl; 

                ListItemCollection items = listControl.Items;
                bool isDataBound = IsDataBound();
                if (items.Count == 0 || isDataBound) { 
                    if (isDataBound) {
                        items.Clear(); 
                        items.Add(SR.GetString(SR.Sample_Databound_Text)); 
                    }
                    else { 
                        items.Add(SR.GetString(SR.Sample_Unbound_Text));
                    }
                }
 
                return base.GetDesignTimeHtml();
            } 
            catch (Exception e) { 
                return GetErrorDesignTimeHtml(e);
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        public IEnumerable GetResolvedSelectedDataSource() {
            return ((IDataSourceProvider)this).GetResolvedSelectedDataSource(); 
        } 

        /// <devdoc> 
        /// </devdoc>
        public object GetSelectedDataSource() {
            return ((IDataSourceProvider)this).GetSelectedDataSource();
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.Initialize"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Initializes the component for design. 
        ///    </para>
        /// </devdoc>
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(ListControl)); 
            base.Initialize(component);
        } 
 
        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.IsDataBound"]/*' />
        /// <devdoc> 
        ///   Return true if the control is databound.
        /// </devdoc>
        private bool IsDataBound() {
            DataBinding dataSourceBinding = DataBindings["DataSource"]; 

            return (dataSourceBinding != null || DataSourceID.Length > 0); 
        } 

        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.OnDataSourceChanged"]/*' /> 
        /// <devdoc>
        /// This method is called when the data source the control is
        /// connected to changes. Override this method to perform any
        /// additional actions that your designer requires. Make sure to call 
        /// the base implementation as well.
        /// This method exists because it shipped in V1.  Removing it creates a breaking change. 
        /// Then call the base class' implementation. 
        /// </devdoc>
        public virtual void OnDataSourceChanged() { 
            // Call the base protected implementation to inherit the default behavior
            // of BaseDataBoundControlDesigner.
            base.OnDataSourceChanged(true);
        } 

        /// <devdoc> 
        /// Called by data bound control designers to raise the data source 
        /// changed event so that listeners can be notified.
        /// </devdoc> 
        protected override void OnDataSourceChanged(bool forceUpdateView) {
            // Call the public OnDataSourceChanged method without parameters introduced in this class,
            // which exists for back-compat.  This method is new in V2.
            // Derived classes overrode OnDataSourceChanged(), so make sure that gets called. 
            this.OnDataSourceChanged();
        } 
 
        /// <include file='doc\ListControlDesigner.uex' path='docs/doc[@for="ListControlDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Filters the properties to replace the runtime DataSource property
        ///       descriptor with the designer's.
        ///    </para> 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 
            Attribute[] fieldPropAttrs = new Attribute[] {
                                             new TypeConverterAttribute(typeof(DataFieldConverter))
                                         };
 
            prop = (PropertyDescriptor)properties["DataTextField"];
            Debug.Assert(prop != null); 
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop, fieldPropAttrs); 
            properties["DataTextField"] = prop;
 
            prop = (PropertyDescriptor)properties["DataValueField"];
            Debug.Assert(prop != null);
            prop = TypeDescriptor.CreateProperty(this.GetType(), prop, fieldPropAttrs);
            properties["DataValueField"] = prop; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
