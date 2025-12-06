//------------------------------------------------------------------------------ 
// <copyright file="TextBoxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Diagnostics;
    using System.Design;
    using System.Windows.Forms.Design;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms.Design.Behavior; 

 
    /// <include file='doc\TextBoxDesigner.uex' path='docs/doc[@for="TextBoxDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides a designer for TextBox.</para> 
    /// </devdoc>
    [ComplexBindingProperties("DataSource", "DataMember")] 
    internal class ListControlBoundActionList : DesignerActionList { 
        private ControlDesigner _owner;
        private bool _boundMode; 
        private object _boundSelectedValue = null;
        private DesignerActionUIService uiService = null;

        public ListControlBoundActionList(ControlDesigner owner)   : base(owner.Component) { 
            _owner = owner;
            ListControl listControl = (ListControl)Component; 
            if (listControl.DataSource != null) { // data source might not be set yet when this is created... 
                _boundMode = true;
            } 
            uiService = GetService(typeof(DesignerActionUIService)) as DesignerActionUIService;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private void RefreshPanelContent() {
            if (uiService != null) { 
                uiService.Refresh(_owner.Component); 
            }
        } 

        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection returnItems = new DesignerActionItemCollection();
 
            returnItems.Add(new DesignerActionPropertyItem("BoundMode",
                SR.GetString(SR.BoundModeDisplayName), 
                SR.GetString(SR.DataCategoryName), 
                SR.GetString(SR.BoundModeDescription)));
            ListControl listControl =Component as ListControl; 
            if (_boundMode || (listControl != null && listControl.DataSource != null)) { // data source might not be set yet when this is created...
                _boundMode = true;

                // Header item 
                returnItems.Add(new DesignerActionHeaderItem(SR.GetString(SR.BoundModeHeader), SR.GetString(SR.DataCategoryName)));
 
                // Property items 
                returnItems.Add(new DesignerActionPropertyItem("DataSource", SR.GetString(SR.DataSourceDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.DataSourceDescription)));
                returnItems.Add(new DesignerActionPropertyItem("DisplayMember", SR.GetString(SR.DisplayMemberDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.DisplayMemberDescription))); 
                returnItems.Add(new DesignerActionPropertyItem("ValueMember", SR.GetString(SR.ValueMemberDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.ValueMemberDescription)));
                returnItems.Add(new DesignerActionPropertyItem("BoundSelectedValue", SR.GetString(SR.BoundSelectedValueDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.BoundSelectedValueDescription)));

                return returnItems; 
            } else {
                // Header item 
                returnItems.Add(new DesignerActionHeaderItem(SR.GetString(SR.UnBoundModeHeader), SR.GetString(SR.DataCategoryName))); 

                // Property item 
                returnItems.Add(new DesignerActionMethodItem(this, "InvokeItemsDialog", SR.GetString(SR.EditItemDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.EditItemDescription), true));
                return returnItems;
            }
        } 

        public bool BoundMode { 
            get { 
                return _boundMode;
            } 
            set {
                if(!value) {
                    this.DataSource = null;
                } 
                if(this.DataSource == null) { // verify this worked... if not don't change anything...
                    _boundMode = value; 
                } 
                RefreshPanelContent();
 
            }
        }

        public void InvokeItemsDialog() { 
            EditorServiceContext.EditValue(_owner, Component, "Items");
        } 
 
        [AttributeProvider(typeof(IListSource))]
        public object DataSource { 
            get {
                return ((ListControl)Component).DataSource;
            }
            set { 
                // left to do: transaction stuff
                ListControl listControl = (ListControl)Component; 
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                IComponentChangeService changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                PropertyDescriptor dataSourceProp = TypeDescriptor.GetProperties(listControl)["DataSource"]; 

                if(host != null && changeService != null) {
                    using(DesignerTransaction transaction = host.CreateTransaction("DGV DataSource TX Name")) {
                        changeService.OnComponentChanging(Component, dataSourceProp); 
                        listControl.DataSource = value;
                        if (null == value) { 
                            listControl.DisplayMember = ""; 
                            listControl.ValueMember = "";
                        } 

                        changeService.OnComponentChanged(Component, dataSourceProp, null, null);
                        transaction.Commit();
                        RefreshPanelContent(); // could be set back to none... 
                    }
                } else { 
                    Debug.Fail("Could not get either IDEsignerHost or IComponentChangeService"); 
                }
            } 
        }
        /// <summary>
        ///
        /// </summary> 
        /// <returns></returns>
        private System.Windows.Forms.Binding GetSelectedValueBinding() { 
            ListControl listControl = (ListControl)Component; 
            System.Windows.Forms.Binding foundBinding = null;
            if (listControl.DataBindings != null) { 
                foreach (System.Windows.Forms.Binding binding in listControl.DataBindings) {
                    if (binding.PropertyName == "SelectedValue")
                        foundBinding = binding;
                } 
            }
            return foundBinding; 
        } 

        private void SetSelectedValueBinding(object dataSource, string dataMember) { 
            // left to do: transaction stuff
            ListControl listControl = (ListControl)Component;
            IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
            IComponentChangeService changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
            PropertyDescriptor dataSourceProp = TypeDescriptor.GetProperties(listControl)["DataBindings"];
 
            if(host != null && changeService != null) { 
                using(DesignerTransaction transaction = host.CreateTransaction("TextBox DataSource RESX")) {
                    changeService.OnComponentChanging(_owner.Component, dataSourceProp); 

                    System.Windows.Forms.Binding foundBinding = GetSelectedValueBinding();
                    if(foundBinding != null) {
                            listControl.DataBindings.Remove(foundBinding); 
                    }
                    if(listControl.DataBindings != null) { 
                        // This prototype doesn't do anything with the DataMember 
                        if (dataSource != null && !String.IsNullOrEmpty(dataMember)) {
                            listControl.DataBindings.Add("SelectedValue", dataSource, dataMember); 
                        }
                    }

                    changeService.OnComponentChanged(_owner.Component, dataSourceProp, null, null); 
                    transaction.Commit();
                } 
            } else { 
                Debug.Fail("Could not get either IDEsignerHost or IComponentChangeService");
            } 
        }


 

 
        [ 
        Editor("System.Windows.Forms.Design.DataMemberFieldEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor))
        ] 
        public string DisplayMember {
            get {
                return ((ListControl)Component).DisplayMember;
            } 
            set {
                // left to do: transaction stuff 
                ListControl listControl = (ListControl)Component; 
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                IComponentChangeService changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                PropertyDescriptor dataSourceProp = TypeDescriptor.GetProperties(listControl)["DisplayMember"];
                if(host != null && changeService != null) {
                    using(DesignerTransaction transaction = host.CreateTransaction("DGV DataSource TX Name")) {
                        changeService.OnComponentChanging(Component, dataSourceProp); 
                        listControl.DisplayMember = value;
                        changeService.OnComponentChanged(Component, dataSourceProp, null, null); 
                        transaction.Commit(); 
                    }
                } else { 
                    Debug.Fail("Could not get either IDEsignerHost or IComponentChangeService");
                }
            }
        } 

 
        [ 
        Editor("System.Windows.Forms.Design.DataMemberFieldEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor))
        ] 
        public string ValueMember {
            get {
                return ((ListControl)Component).ValueMember;
            } 
            set {
                // left to do: transaction stuff 
                ListControl listControl = (ListControl)_owner.Component; 
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                IComponentChangeService changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                PropertyDescriptor dataSourceProp = TypeDescriptor.GetProperties(listControl)["ValueMember"];
                if(host != null && changeService != null) {
                    using(DesignerTransaction transaction = host.CreateTransaction("DGV DataSource TX Name")) {
                        changeService.OnComponentChanging(Component, dataSourceProp); 
                        listControl.ValueMember = value;
                        changeService.OnComponentChanged(Component, dataSourceProp, null, null); 
                        transaction.Commit(); 
                    }
                } else { 
                    Debug.Fail("Could not get either IDEsignerHost or IComponentChangeService");
                }
            }
        } 

 
        [ 
        TypeConverterAttribute("System.Windows.Forms.Design.DesignBindingConverter"),
        Editor("System.Windows.Forms.Design.DesignBindingEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor)) 
        ]
        public object BoundSelectedValue {
            get {
                System.Windows.Forms.Binding b = GetSelectedValueBinding(); 
                string dataMember;
                object dataSource; 
                if (b == null) { 
                    dataMember = null;
                    dataSource = null; 
                } else {
                    dataMember = b.BindingMemberInfo.BindingMember;
                    dataSource = b.DataSource;
                } 
                string typeName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "System.Windows.Forms.Design.DesignBinding, {0}", typeof(ControlDesigner).Assembly.FullName);
                _boundSelectedValue = TypeDescriptor.CreateInstance(null, Type.GetType(typeName), new Type[] { typeof(object), typeof(string) }, new object[] { dataSource, dataMember }); 
 
                return _boundSelectedValue;
 
            }
            set {
                if (value is String) {
                    PropertyDescriptor pd = TypeDescriptor.GetProperties(this)["BoundSelectedValue"]; 
                    TypeConverter tc = pd.Converter;
                    _boundSelectedValue = tc.ConvertFrom(new EditorServiceContext(_owner), System.Globalization.CultureInfo.InvariantCulture, value); 
                } else { 
                    _boundSelectedValue = value;
                    if (value != null) { 
                        object dataSource = TypeDescriptor.GetProperties(_boundSelectedValue)["DataSource"].GetValue(_boundSelectedValue);
                        string dataMember = (string)TypeDescriptor.GetProperties(_boundSelectedValue)["DataMember"].GetValue(_boundSelectedValue);
                        SetSelectedValueBinding(dataSource, dataMember);
                    } 
                }
            } 
        } 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TextBoxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Diagnostics;
    using System.Design;
    using System.Windows.Forms.Design;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms.Design.Behavior; 

 
    /// <include file='doc\TextBoxDesigner.uex' path='docs/doc[@for="TextBoxDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides a designer for TextBox.</para> 
    /// </devdoc>
    [ComplexBindingProperties("DataSource", "DataMember")] 
    internal class ListControlBoundActionList : DesignerActionList { 
        private ControlDesigner _owner;
        private bool _boundMode; 
        private object _boundSelectedValue = null;
        private DesignerActionUIService uiService = null;

        public ListControlBoundActionList(ControlDesigner owner)   : base(owner.Component) { 
            _owner = owner;
            ListControl listControl = (ListControl)Component; 
            if (listControl.DataSource != null) { // data source might not be set yet when this is created... 
                _boundMode = true;
            } 
            uiService = GetService(typeof(DesignerActionUIService)) as DesignerActionUIService;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private void RefreshPanelContent() {
            if (uiService != null) { 
                uiService.Refresh(_owner.Component); 
            }
        } 

        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection returnItems = new DesignerActionItemCollection();
 
            returnItems.Add(new DesignerActionPropertyItem("BoundMode",
                SR.GetString(SR.BoundModeDisplayName), 
                SR.GetString(SR.DataCategoryName), 
                SR.GetString(SR.BoundModeDescription)));
            ListControl listControl =Component as ListControl; 
            if (_boundMode || (listControl != null && listControl.DataSource != null)) { // data source might not be set yet when this is created...
                _boundMode = true;

                // Header item 
                returnItems.Add(new DesignerActionHeaderItem(SR.GetString(SR.BoundModeHeader), SR.GetString(SR.DataCategoryName)));
 
                // Property items 
                returnItems.Add(new DesignerActionPropertyItem("DataSource", SR.GetString(SR.DataSourceDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.DataSourceDescription)));
                returnItems.Add(new DesignerActionPropertyItem("DisplayMember", SR.GetString(SR.DisplayMemberDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.DisplayMemberDescription))); 
                returnItems.Add(new DesignerActionPropertyItem("ValueMember", SR.GetString(SR.ValueMemberDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.ValueMemberDescription)));
                returnItems.Add(new DesignerActionPropertyItem("BoundSelectedValue", SR.GetString(SR.BoundSelectedValueDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.BoundSelectedValueDescription)));

                return returnItems; 
            } else {
                // Header item 
                returnItems.Add(new DesignerActionHeaderItem(SR.GetString(SR.UnBoundModeHeader), SR.GetString(SR.DataCategoryName))); 

                // Property item 
                returnItems.Add(new DesignerActionMethodItem(this, "InvokeItemsDialog", SR.GetString(SR.EditItemDisplayName), SR.GetString(SR.DataCategoryName), SR.GetString(SR.EditItemDescription), true));
                return returnItems;
            }
        } 

        public bool BoundMode { 
            get { 
                return _boundMode;
            } 
            set {
                if(!value) {
                    this.DataSource = null;
                } 
                if(this.DataSource == null) { // verify this worked... if not don't change anything...
                    _boundMode = value; 
                } 
                RefreshPanelContent();
 
            }
        }

        public void InvokeItemsDialog() { 
            EditorServiceContext.EditValue(_owner, Component, "Items");
        } 
 
        [AttributeProvider(typeof(IListSource))]
        public object DataSource { 
            get {
                return ((ListControl)Component).DataSource;
            }
            set { 
                // left to do: transaction stuff
                ListControl listControl = (ListControl)Component; 
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                IComponentChangeService changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                PropertyDescriptor dataSourceProp = TypeDescriptor.GetProperties(listControl)["DataSource"]; 

                if(host != null && changeService != null) {
                    using(DesignerTransaction transaction = host.CreateTransaction("DGV DataSource TX Name")) {
                        changeService.OnComponentChanging(Component, dataSourceProp); 
                        listControl.DataSource = value;
                        if (null == value) { 
                            listControl.DisplayMember = ""; 
                            listControl.ValueMember = "";
                        } 

                        changeService.OnComponentChanged(Component, dataSourceProp, null, null);
                        transaction.Commit();
                        RefreshPanelContent(); // could be set back to none... 
                    }
                } else { 
                    Debug.Fail("Could not get either IDEsignerHost or IComponentChangeService"); 
                }
            } 
        }
        /// <summary>
        ///
        /// </summary> 
        /// <returns></returns>
        private System.Windows.Forms.Binding GetSelectedValueBinding() { 
            ListControl listControl = (ListControl)Component; 
            System.Windows.Forms.Binding foundBinding = null;
            if (listControl.DataBindings != null) { 
                foreach (System.Windows.Forms.Binding binding in listControl.DataBindings) {
                    if (binding.PropertyName == "SelectedValue")
                        foundBinding = binding;
                } 
            }
            return foundBinding; 
        } 

        private void SetSelectedValueBinding(object dataSource, string dataMember) { 
            // left to do: transaction stuff
            ListControl listControl = (ListControl)Component;
            IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
            IComponentChangeService changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
            PropertyDescriptor dataSourceProp = TypeDescriptor.GetProperties(listControl)["DataBindings"];
 
            if(host != null && changeService != null) { 
                using(DesignerTransaction transaction = host.CreateTransaction("TextBox DataSource RESX")) {
                    changeService.OnComponentChanging(_owner.Component, dataSourceProp); 

                    System.Windows.Forms.Binding foundBinding = GetSelectedValueBinding();
                    if(foundBinding != null) {
                            listControl.DataBindings.Remove(foundBinding); 
                    }
                    if(listControl.DataBindings != null) { 
                        // This prototype doesn't do anything with the DataMember 
                        if (dataSource != null && !String.IsNullOrEmpty(dataMember)) {
                            listControl.DataBindings.Add("SelectedValue", dataSource, dataMember); 
                        }
                    }

                    changeService.OnComponentChanged(_owner.Component, dataSourceProp, null, null); 
                    transaction.Commit();
                } 
            } else { 
                Debug.Fail("Could not get either IDEsignerHost or IComponentChangeService");
            } 
        }


 

 
        [ 
        Editor("System.Windows.Forms.Design.DataMemberFieldEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor))
        ] 
        public string DisplayMember {
            get {
                return ((ListControl)Component).DisplayMember;
            } 
            set {
                // left to do: transaction stuff 
                ListControl listControl = (ListControl)Component; 
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                IComponentChangeService changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                PropertyDescriptor dataSourceProp = TypeDescriptor.GetProperties(listControl)["DisplayMember"];
                if(host != null && changeService != null) {
                    using(DesignerTransaction transaction = host.CreateTransaction("DGV DataSource TX Name")) {
                        changeService.OnComponentChanging(Component, dataSourceProp); 
                        listControl.DisplayMember = value;
                        changeService.OnComponentChanged(Component, dataSourceProp, null, null); 
                        transaction.Commit(); 
                    }
                } else { 
                    Debug.Fail("Could not get either IDEsignerHost or IComponentChangeService");
                }
            }
        } 

 
        [ 
        Editor("System.Windows.Forms.Design.DataMemberFieldEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor))
        ] 
        public string ValueMember {
            get {
                return ((ListControl)Component).ValueMember;
            } 
            set {
                // left to do: transaction stuff 
                ListControl listControl = (ListControl)_owner.Component; 
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                IComponentChangeService changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                PropertyDescriptor dataSourceProp = TypeDescriptor.GetProperties(listControl)["ValueMember"];
                if(host != null && changeService != null) {
                    using(DesignerTransaction transaction = host.CreateTransaction("DGV DataSource TX Name")) {
                        changeService.OnComponentChanging(Component, dataSourceProp); 
                        listControl.ValueMember = value;
                        changeService.OnComponentChanged(Component, dataSourceProp, null, null); 
                        transaction.Commit(); 
                    }
                } else { 
                    Debug.Fail("Could not get either IDEsignerHost or IComponentChangeService");
                }
            }
        } 

 
        [ 
        TypeConverterAttribute("System.Windows.Forms.Design.DesignBindingConverter"),
        Editor("System.Windows.Forms.Design.DesignBindingEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor)) 
        ]
        public object BoundSelectedValue {
            get {
                System.Windows.Forms.Binding b = GetSelectedValueBinding(); 
                string dataMember;
                object dataSource; 
                if (b == null) { 
                    dataMember = null;
                    dataSource = null; 
                } else {
                    dataMember = b.BindingMemberInfo.BindingMember;
                    dataSource = b.DataSource;
                } 
                string typeName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "System.Windows.Forms.Design.DesignBinding, {0}", typeof(ControlDesigner).Assembly.FullName);
                _boundSelectedValue = TypeDescriptor.CreateInstance(null, Type.GetType(typeName), new Type[] { typeof(object), typeof(string) }, new object[] { dataSource, dataMember }); 
 
                return _boundSelectedValue;
 
            }
            set {
                if (value is String) {
                    PropertyDescriptor pd = TypeDescriptor.GetProperties(this)["BoundSelectedValue"]; 
                    TypeConverter tc = pd.Converter;
                    _boundSelectedValue = tc.ConvertFrom(new EditorServiceContext(_owner), System.Globalization.CultureInfo.InvariantCulture, value); 
                } else { 
                    _boundSelectedValue = value;
                    if (value != null) { 
                        object dataSource = TypeDescriptor.GetProperties(_boundSelectedValue)["DataSource"].GetValue(_boundSelectedValue);
                        string dataMember = (string)TypeDescriptor.GetProperties(_boundSelectedValue)["DataMember"].GetValue(_boundSelectedValue);
                        SetSelectedValueBinding(dataSource, dataMember);
                    } 
                }
            } 
        } 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
