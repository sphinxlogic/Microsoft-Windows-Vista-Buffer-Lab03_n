//------------------------------------------------------------------------------ 
// <copyright file="BaseDataBoundControlDesigner.cs" company="Microsoft">
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
 
    using Control = System.Web.UI.Control;
 
    /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner"]/*' /> 
    /// <summary>
    /// BaseDataBoundControlDesigner is the designer associated with a 
    /// BaseDataBoundControl. It provides the basic shared design-time experience
    /// for all data-bound controls.
    /// </summary>
    public abstract class BaseDataBoundControlDesigner : ControlDesigner { 

        private bool _keepDataSourceBrowsable; 
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.DataSource"]/*' />
        /// <summary> 
        /// Implements the designer's version of the DataSource property.
        /// This is used to shadow the DataSource property of the
        /// runtime control.
        /// The data source is persisted by the designer as a data-binding 
        /// expression on the control's tag.
        /// </summary> 
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
 
                OnDataSourceChanged(true);
                OnBindingsCollectionChangedInternal("DataSource"); 
            } 
        }
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.DataSourceID"]/*' />
        /// <summary>
        /// Implements the designer's version of the DataSourceID property.
        /// This is used to shadow the DataSourceID property of the 
        /// runtime control.
        /// </summary> 
        public string DataSourceID { 
            get {
                return ((BaseDataBoundControl)Component).DataSourceID; 
            }
            set {
                if (value == DataSourceID) {
                    return; 
                }
 
                if (value == SR.GetString(SR.DataSourceIDChromeConverter_NewDataSource)) { 
                    CreateDataSource();
                    TypeDescriptor.Refresh(Component); 
                    return;
                }

                if (value == SR.GetString(SR.DataSourceIDChromeConverter_NoDataSource)) { 
                    value = String.Empty;
                } 
 
                // Refresh must be called before componentchanged (invoked by PropertyDescriptor.SetValue)
                // so the property grid can update the actions properly. 
                TypeDescriptor.Refresh(Component);

                // BaseDataBoundControl.DataSourceID = value;
                // Get the property descriptor with the explicit type so we don't get the designer's filtered property 
                PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(BaseDataBoundControl))["DataSourceID"];
                propDesc.SetValue(Component, value); 
                OnDataSourceChanged(false); 
                OnSchemaRefreshed();
             } 
        }

        /// <devdoc>
        /// Performs the actions necessary to connect to the current data 
        /// source. This typically involved unhooking events from the previous
        /// data source and then attaching new events to the new data source. 
        /// </devdoc> 
        protected abstract bool ConnectToDataSource();
 
        /// <devdoc>
        /// Creates a new data source for this data bound control.
        /// </devdoc>
        protected abstract void CreateDataSource(); 

        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.Dispose"]/*' /> 
        /// <devdoc> 
        /// Performs the necessary actions to set up the data bound control
        /// such that its child controls will be created with the proper 
        /// state so that when the design time HTML is retrieved the control
        /// will render properly.
        /// </devdoc>
        protected abstract void DataBind(BaseDataBoundControl dataBoundControl); 

        /// <devdoc> 
        /// Performs the actions necessary to disconnect from the current data 
        /// source. This typically involved unhooking events from the previous
        /// data source. 
        /// </devdoc>
        protected abstract void DisconnectFromDataSource();

        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.Dispose"]/*' /> 
        protected override void Dispose(bool disposing) {
            if (disposing) { 
                if (Component != null && Component.Site != null) { 
                    // Detach from data source designer events
                    DisconnectFromDataSource(); 

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
 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.GetDesignTimeHtml"]/*' />
        /// <summary>
        /// Provides the design-time HTML used to display the control
        /// at design-time. 
        /// DataBoundControlDesigner retrieves sample data used for binding
        /// purposes at design-time before rendering the control. 
        /// If the control is not data-bound it calls GetEmptyDesignTimeHtml. 
        /// If there is an error rendering the control, it calls
        /// GetErrorDesignTimeHtml. 
        /// </summary>
        /// <returns>
        /// The HTML used to render the control at design-time.
        /// </returns> 
        public override string GetDesignTimeHtml() {
            string designTimeHtml = String.Empty; 
 
            try {
                DataBind((BaseDataBoundControl)ViewControl); 
                designTimeHtml = base.GetDesignTimeHtml();
            }
            catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e); 
            }
 
            return designTimeHtml; 
        }
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.GetEmptyDesignTimeHtml"]/*' />
        /// <summary>
        /// Provides the design-time HTML used to display the control
        /// at design-time if the control is empty or the dataSource can't be 
        /// retrieved.
        /// BaseDataBoundControlDesigner retrieves sample data used for binding 
        /// purposes at design-time before rendering the control. 
        /// </summary>
        /// <returns> 
        /// The HTML used to render the control at design-time for an empty
        /// dataSource.
        /// </returns>
        protected override string GetEmptyDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml(null);
        } 
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.GetErrorDesignTimeHtml"]/*' />
        /// <summary> 
        /// Provides HTML to show in the designer when an error occurs.
        /// </summary>
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br/>" + e.Message); 
        }
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.Initialize"]/*' /> 
        /// <summary>
        /// Initializes the component 
        /// </summary>
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(BaseDataBoundControl));
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
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.OnComponentChanged"]/*' />
        /// <summary> 
        /// Fires when a component is changing.  This may be our DataSourceControl
        /// </summary>
        private void OnAnyComponentChanged(object sender, ComponentChangedEventArgs ce) {
            Control control = ce.Component as Control; 
            if (control != null) {
                if (ce.Member != null && ce.Member.Name == "ID" && Component != null) { 
                    if((string)ce.OldValue == DataSourceID || (string)ce.NewValue == DataSourceID) { 
                        OnDataSourceChanged(false);
                    } 
                }
            }
        }
 
        /// <summary>
        /// Fires when a component is added.  This may be our DataSourceControl 
        /// </summary> 
        private void OnComponentAdded(object sender, ComponentEventArgs e) {
            Control control = e.Component as Control; 
            if (control != null) {
                if (control.ID == DataSourceID) {
                    OnDataSourceChanged(false);
                } 
            }
        } 
 
        /// <summary>
        /// Fires when a component is being removed.  This may be our DataSourceControl 
        /// </summary>
        private void OnComponentRemoving(object sender, ComponentEventArgs e) {
            Control control = e.Component as Control;
            if (control != null && Component != null) { 
                if (control.ID == DataSourceID) {
                    DisconnectFromDataSource(); 
                } 
            }
        } 

        /// <summary>
        /// Fires when a component is removed.  This may be our DataSourceControl
        /// </summary> 
        private void OnComponentRemoved(object sender, ComponentEventArgs e) {
            Control control = e.Component as Control; 
            if (control != null && Component != null) { 
                if (control.ID == DataSourceID) {
                    IDesignerHost designerHost = (IDesignerHost)this.GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null, "Did not get DesignerHost service.");

                    if (designerHost != null && !designerHost.Loading) {
                        OnDataSourceChanged(false); 
                    }
                } 
            } 
        }
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.OnConfigureDataSourceCompleted"]/*' />
        /// <summary>
        /// Raised when the data source that this control is bound to has
        /// changed. Designers can override this to perform additional actions 
        /// required. Make sure to call the base implementation.
        /// </summary> 
        protected virtual void OnDataSourceChanged(bool forceUpdateView) { 
            bool dataSourceChanged = ConnectToDataSource();
            if (dataSourceChanged || forceUpdateView) { 
                UpdateDesignTimeHtml();
            }
        }
 
        private void OnDesignerLoadComplete(object sender, EventArgs e) {
            OnDataSourceChanged(false); 
        } 

        /// <summary> 
        /// Raised when the data source that this control is bound to has
        /// new schema. Designers can override this to perform additional
        /// actions required when new schema is available.
        /// </summary> 
        protected virtual void OnSchemaRefreshed() {
        } 
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.PreFilterProperties"]/*' />
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

            bool hasDataSource = (dataSource != null) && (dataSource.Length > 0); 
            if (hasDataSource) {
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
            if (browsableAttributeIndex == -1 && !hasDataSource && !_keepDataSourceBrowsable) {
                attributeCount = runtimeAttributeCount + 1; 
            } 
            else {
                attributeCount = runtimeAttributeCount; 
            }
            Attribute[] attrs = new Attribute[attributeCount];

            runtimeAttributes.CopyTo(attrs, 0); 

            // if DataSource is not empty and there was no BrowsableAttribute, add one.  Otherwise, 
            // change the one that's there to be false. 
            if (!hasDataSource && !_keepDataSourceBrowsable) {
                if (browsableAttributeIndex == -1) { 
                    attrs[runtimeAttributeCount] = BrowsableAttribute.No;
                }
                else {
                    attrs[browsableAttributeIndex] = BrowsableAttribute.No; 
                }
            } 
            prop = TypeDescriptor.CreateProperty(this.GetType(), "DataSource", typeof(string), 
                                                 attrs);
            properties["DataSource"] = prop; 
        }

        public static DialogResult ShowCreateDataSourceDialog(ControlDesigner controlDesigner, Type dataSourceType, bool configure, out string dataSourceID) {
            CreateDataSourceDialog dialog = new CreateDataSourceDialog(controlDesigner, dataSourceType, configure); 
            DialogResult result = UIServiceHelper.ShowDialog(controlDesigner.Component.Site, dialog);
            dataSourceID = dialog.DataSourceID; 
            return result; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BaseDataBoundControlDesigner.cs" company="Microsoft">
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
 
    using Control = System.Web.UI.Control;
 
    /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner"]/*' /> 
    /// <summary>
    /// BaseDataBoundControlDesigner is the designer associated with a 
    /// BaseDataBoundControl. It provides the basic shared design-time experience
    /// for all data-bound controls.
    /// </summary>
    public abstract class BaseDataBoundControlDesigner : ControlDesigner { 

        private bool _keepDataSourceBrowsable; 
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.DataSource"]/*' />
        /// <summary> 
        /// Implements the designer's version of the DataSource property.
        /// This is used to shadow the DataSource property of the
        /// runtime control.
        /// The data source is persisted by the designer as a data-binding 
        /// expression on the control's tag.
        /// </summary> 
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
 
                OnDataSourceChanged(true);
                OnBindingsCollectionChangedInternal("DataSource"); 
            } 
        }
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.DataSourceID"]/*' />
        /// <summary>
        /// Implements the designer's version of the DataSourceID property.
        /// This is used to shadow the DataSourceID property of the 
        /// runtime control.
        /// </summary> 
        public string DataSourceID { 
            get {
                return ((BaseDataBoundControl)Component).DataSourceID; 
            }
            set {
                if (value == DataSourceID) {
                    return; 
                }
 
                if (value == SR.GetString(SR.DataSourceIDChromeConverter_NewDataSource)) { 
                    CreateDataSource();
                    TypeDescriptor.Refresh(Component); 
                    return;
                }

                if (value == SR.GetString(SR.DataSourceIDChromeConverter_NoDataSource)) { 
                    value = String.Empty;
                } 
 
                // Refresh must be called before componentchanged (invoked by PropertyDescriptor.SetValue)
                // so the property grid can update the actions properly. 
                TypeDescriptor.Refresh(Component);

                // BaseDataBoundControl.DataSourceID = value;
                // Get the property descriptor with the explicit type so we don't get the designer's filtered property 
                PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(BaseDataBoundControl))["DataSourceID"];
                propDesc.SetValue(Component, value); 
                OnDataSourceChanged(false); 
                OnSchemaRefreshed();
             } 
        }

        /// <devdoc>
        /// Performs the actions necessary to connect to the current data 
        /// source. This typically involved unhooking events from the previous
        /// data source and then attaching new events to the new data source. 
        /// </devdoc> 
        protected abstract bool ConnectToDataSource();
 
        /// <devdoc>
        /// Creates a new data source for this data bound control.
        /// </devdoc>
        protected abstract void CreateDataSource(); 

        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.Dispose"]/*' /> 
        /// <devdoc> 
        /// Performs the necessary actions to set up the data bound control
        /// such that its child controls will be created with the proper 
        /// state so that when the design time HTML is retrieved the control
        /// will render properly.
        /// </devdoc>
        protected abstract void DataBind(BaseDataBoundControl dataBoundControl); 

        /// <devdoc> 
        /// Performs the actions necessary to disconnect from the current data 
        /// source. This typically involved unhooking events from the previous
        /// data source. 
        /// </devdoc>
        protected abstract void DisconnectFromDataSource();

        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.Dispose"]/*' /> 
        protected override void Dispose(bool disposing) {
            if (disposing) { 
                if (Component != null && Component.Site != null) { 
                    // Detach from data source designer events
                    DisconnectFromDataSource(); 

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
 
        /// <include file='doc\DataBoundControlDesigner.uex' path='docs/doc[@for="DataBoundControlDesigner.GetDesignTimeHtml"]/*' />
        /// <summary>
        /// Provides the design-time HTML used to display the control
        /// at design-time. 
        /// DataBoundControlDesigner retrieves sample data used for binding
        /// purposes at design-time before rendering the control. 
        /// If the control is not data-bound it calls GetEmptyDesignTimeHtml. 
        /// If there is an error rendering the control, it calls
        /// GetErrorDesignTimeHtml. 
        /// </summary>
        /// <returns>
        /// The HTML used to render the control at design-time.
        /// </returns> 
        public override string GetDesignTimeHtml() {
            string designTimeHtml = String.Empty; 
 
            try {
                DataBind((BaseDataBoundControl)ViewControl); 
                designTimeHtml = base.GetDesignTimeHtml();
            }
            catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e); 
            }
 
            return designTimeHtml; 
        }
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.GetEmptyDesignTimeHtml"]/*' />
        /// <summary>
        /// Provides the design-time HTML used to display the control
        /// at design-time if the control is empty or the dataSource can't be 
        /// retrieved.
        /// BaseDataBoundControlDesigner retrieves sample data used for binding 
        /// purposes at design-time before rendering the control. 
        /// </summary>
        /// <returns> 
        /// The HTML used to render the control at design-time for an empty
        /// dataSource.
        /// </returns>
        protected override string GetEmptyDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml(null);
        } 
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.GetErrorDesignTimeHtml"]/*' />
        /// <summary> 
        /// Provides HTML to show in the designer when an error occurs.
        /// </summary>
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br/>" + e.Message); 
        }
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.Initialize"]/*' /> 
        /// <summary>
        /// Initializes the component 
        /// </summary>
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(BaseDataBoundControl));
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
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.OnComponentChanged"]/*' />
        /// <summary> 
        /// Fires when a component is changing.  This may be our DataSourceControl
        /// </summary>
        private void OnAnyComponentChanged(object sender, ComponentChangedEventArgs ce) {
            Control control = ce.Component as Control; 
            if (control != null) {
                if (ce.Member != null && ce.Member.Name == "ID" && Component != null) { 
                    if((string)ce.OldValue == DataSourceID || (string)ce.NewValue == DataSourceID) { 
                        OnDataSourceChanged(false);
                    } 
                }
            }
        }
 
        /// <summary>
        /// Fires when a component is added.  This may be our DataSourceControl 
        /// </summary> 
        private void OnComponentAdded(object sender, ComponentEventArgs e) {
            Control control = e.Component as Control; 
            if (control != null) {
                if (control.ID == DataSourceID) {
                    OnDataSourceChanged(false);
                } 
            }
        } 
 
        /// <summary>
        /// Fires when a component is being removed.  This may be our DataSourceControl 
        /// </summary>
        private void OnComponentRemoving(object sender, ComponentEventArgs e) {
            Control control = e.Component as Control;
            if (control != null && Component != null) { 
                if (control.ID == DataSourceID) {
                    DisconnectFromDataSource(); 
                } 
            }
        } 

        /// <summary>
        /// Fires when a component is removed.  This may be our DataSourceControl
        /// </summary> 
        private void OnComponentRemoved(object sender, ComponentEventArgs e) {
            Control control = e.Component as Control; 
            if (control != null && Component != null) { 
                if (control.ID == DataSourceID) {
                    IDesignerHost designerHost = (IDesignerHost)this.GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null, "Did not get DesignerHost service.");

                    if (designerHost != null && !designerHost.Loading) {
                        OnDataSourceChanged(false); 
                    }
                } 
            } 
        }
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.OnConfigureDataSourceCompleted"]/*' />
        /// <summary>
        /// Raised when the data source that this control is bound to has
        /// changed. Designers can override this to perform additional actions 
        /// required. Make sure to call the base implementation.
        /// </summary> 
        protected virtual void OnDataSourceChanged(bool forceUpdateView) { 
            bool dataSourceChanged = ConnectToDataSource();
            if (dataSourceChanged || forceUpdateView) { 
                UpdateDesignTimeHtml();
            }
        }
 
        private void OnDesignerLoadComplete(object sender, EventArgs e) {
            OnDataSourceChanged(false); 
        } 

        /// <summary> 
        /// Raised when the data source that this control is bound to has
        /// new schema. Designers can override this to perform additional
        /// actions required when new schema is available.
        /// </summary> 
        protected virtual void OnSchemaRefreshed() {
        } 
 
        /// <include file='doc\BaseDataBoundControlDesigner.uex' path='docs/doc[@for="BaseDataBoundControlDesigner.PreFilterProperties"]/*' />
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

            bool hasDataSource = (dataSource != null) && (dataSource.Length > 0); 
            if (hasDataSource) {
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
            if (browsableAttributeIndex == -1 && !hasDataSource && !_keepDataSourceBrowsable) {
                attributeCount = runtimeAttributeCount + 1; 
            } 
            else {
                attributeCount = runtimeAttributeCount; 
            }
            Attribute[] attrs = new Attribute[attributeCount];

            runtimeAttributes.CopyTo(attrs, 0); 

            // if DataSource is not empty and there was no BrowsableAttribute, add one.  Otherwise, 
            // change the one that's there to be false. 
            if (!hasDataSource && !_keepDataSourceBrowsable) {
                if (browsableAttributeIndex == -1) { 
                    attrs[runtimeAttributeCount] = BrowsableAttribute.No;
                }
                else {
                    attrs[browsableAttributeIndex] = BrowsableAttribute.No; 
                }
            } 
            prop = TypeDescriptor.CreateProperty(this.GetType(), "DataSource", typeof(string), 
                                                 attrs);
            properties["DataSource"] = prop; 
        }

        public static DialogResult ShowCreateDataSourceDialog(ControlDesigner controlDesigner, Type dataSourceType, bool configure, out string dataSourceID) {
            CreateDataSourceDialog dialog = new CreateDataSourceDialog(controlDesigner, dataSourceType, configure); 
            DialogResult result = UIServiceHelper.ShowDialog(controlDesigner.Component.Site, dialog);
            dataSourceID = dialog.DataSourceID; 
            return result; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
