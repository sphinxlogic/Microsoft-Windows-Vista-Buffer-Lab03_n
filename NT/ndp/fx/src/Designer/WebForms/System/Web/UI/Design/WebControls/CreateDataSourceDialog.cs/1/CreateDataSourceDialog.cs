//------------------------------------------------------------------------------ 
// <copyright file="CreateDataSourceDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Drawing.Design; 
    using System.Globalization;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
 
    using Control = System.Web.UI.Control;
    using ControlDesigner = System.Web.UI.Design.ControlDesigner; 
    using GridView = System.Web.UI.WebControls.GridView; 

    using BorderStyle = System.Windows.Forms.BorderStyle; 
    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label;
    using ComboBox = System.Windows.Forms.ComboBox;
    using TextBox = System.Windows.Forms.TextBox; 
    using CheckBox = System.Windows.Forms.CheckBox;
    using Panel = System.Windows.Forms.Panel; 
 
    /// <devdoc>
    ///   The CreateDataSource dialog used for web controls.  This is invoked when you select "new DataSource" from the chrome dropdown. 
    /// </devdoc>
    /// <internalonly/>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class CreateDataSourceDialog : TaskForm { 
        private ControlDesigner _controlDesigner;
        private string _controlID; 
        private Type _dataSourceType; 
        private DisplayNameComparer _displayNameComparer;
        private string _dataSourceID; 
        private bool _configure;

        private System.Windows.Forms.Label _selectLabel;
        private System.Windows.Forms.ListView _dataSourceTypesListView; 
        private System.Windows.Forms.TextBox _descriptionBox;
        private System.Windows.Forms.Label _idLabel; 
        private System.Windows.Forms.TextBox _idTextBox; 

        /// <devdoc> 
        ///  Creates a new instance of the class
        /// </devdoc>
        public CreateDataSourceDialog(ControlDesigner controlDesigner, Type dataSourceType, bool configure) : base(controlDesigner.Component.Site) {
            Debug.Assert(dataSourceType != null, "dataSourceType must be specified"); 
            this._controlDesigner = controlDesigner;
            this._controlID = ((System.Web.UI.Control)controlDesigner.Component).ID; 
            this._dataSourceType = dataSourceType; 
            this._configure = configure;
 
            this._displayNameComparer = new DisplayNameComparer();
            Glyph = new Bitmap(this.GetType(), "datasourcewizard.bmp");
            CreatePanel();
        } 

        public string DataSourceID { 
            get { 
                if (_dataSourceID == null) {
                    return String.Empty; 
                }
                return _dataSourceID;
            }
        } 

        protected override string HelpTopic { 
            get { 
                return "net.Asp.DataBoundControl.CreateDataSourceDialog";
            } 
        }

        /// <devdoc>
        ///  Creates a new datasource and adds it to the page, then calls configure on it.  When this 
        ///  function is done, a new datasource should be in the available datasources list, selected.
        /// </devdoc> 
        private string CreateNewDataSource(Type dataSourceType) { 
            string newDataSourceName = _idTextBox.Text;
            string id = String.Empty; 

            if (dataSourceType != null) {
                object dataSourceObject = Activator.CreateInstance(dataSourceType);
                if (dataSourceObject != null) { 
                    Debug.Assert(_dataSourceType.IsAssignableFrom(dataSourceObject.GetType()), "DataSource object created did not implement '" + _dataSourceType.Name + "'.");
                    Control dataSourceControl = dataSourceObject as Control; 
                    if (dataSourceControl != null) { 
                        dataSourceControl.ID = newDataSourceName;
                        ISite site = GetSite(); 
                        if (site != null) {
                            INameCreationService nameCreationService = (INameCreationService)(site.GetService(typeof(INameCreationService)));

                            if (nameCreationService != null) { 
                                try {
                                    nameCreationService.ValidateName(newDataSourceName); 
                                } 
                                catch (Exception ex) {
                                    UIServiceHelper.ShowError((IServiceProvider)site, SR.GetString(SR.CreateDataSource_NameNotValid, ex.Message)); 
                                    _idTextBox.Focus();
                                    return id;
                                }
                                // make sure name is unique 
                                IContainer container = site.Container;
                                if (container != null) { 
                                    ComponentCollection components = container.Components; 
                                    if (components != null) {
                                        if (components[newDataSourceName] != null) { 
                                            UIServiceHelper.ShowError((IServiceProvider)site, SR.GetString(SR.CreateDataSource_NameNotUnique));
                                            _idTextBox.Focus();
                                            return id;
                                        } 

                                    } 
                                } 
                            }
 
                            IDesignerHost designerHost = (IDesignerHost)(site.GetService(typeof(IDesignerHost)));
                            Debug.Assert(designerHost != null, "Did not get DesignerHost service.");

                            if (designerHost != null) { 
                                IComponent rootComponent = designerHost.RootComponent;
                                if (rootComponent != null) { 
                                    WebFormsRootDesigner rootDesigner = designerHost.GetDesigner(rootComponent) as WebFormsRootDesigner; 
                                    if (rootDesigner != null) {
                                        Control referenceControl = GetComponent() as Control; 
                                        id = rootDesigner.AddControlToDocument(dataSourceControl, referenceControl, ControlLocation.After);
                                        Debug.Assert(id == newDataSourceName, "AddControlToDocument returned an unexpected value.");

                                        IDesigner designer = designerHost.GetDesigner(dataSourceControl); 
                                        Debug.Assert(designer != null, "GetDesigner on the new control returned null.");
                                        IDataSourceDesigner dsd = designer as IDataSourceDesigner; 
                                        if (dsd != null) { 
                                            if (dsd.CanConfigure && _configure) {
                                                dsd.Configure(); 
                                            }
                                        }
                                        else {
                                            IHierarchicalDataSourceDesigner hdsd = designer as IHierarchicalDataSourceDesigner; 
                                            if (hdsd != null) {
                                                if (hdsd.CanConfigure && _configure) { 
                                                    hdsd.Configure(); 
                                                }
                                            } 
                                        }
                                    }
                                }
                            } 
                        }
                    } 
                } 
            }
            return id; 
        }

        private void CreatePanel() {
            SuspendLayout(); 

            CreatePanelControls(); 
            InitializePanelControls(); 

            InitializeForm(); 

            ResumeLayout(false);
            PerformLayout();
        } 

        private void CreatePanelControls() { 
            this._selectLabel = new System.Windows.Forms.Label(); 
            this._dataSourceTypesListView = new System.Windows.Forms.ListView();
            this._descriptionBox = new System.Windows.Forms.TextBox(); 
            this._idLabel = new System.Windows.Forms.Label();
            this._idTextBox = new System.Windows.Forms.TextBox();

            // 
            // _selectLabel
            // 
            this._selectLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._selectLabel.Location = new System.Drawing.Point(0, 0); 
            this._selectLabel.Name = "_selectLabel";
            this._selectLabel.Size = new System.Drawing.Size(544, 16);
            this._selectLabel.TabIndex = 0;
 
            //
            // _dataSourceTypesListView 
            // 
            this._dataSourceTypesListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._dataSourceTypesListView.Location = new System.Drawing.Point(0, 18);
            this._dataSourceTypesListView.Name = "_dataSourceTypesListView";
            this._dataSourceTypesListView.Size = new System.Drawing.Size(544, 90);
            this._dataSourceTypesListView.TabIndex = 1; 
            this._dataSourceTypesListView.SelectedIndexChanged += new System.EventHandler(this.OnDataSourceTypeChosen);
            this._dataSourceTypesListView.Alignment = ListViewAlignment.Left; 
            this._dataSourceTypesListView.LabelWrap = true; 
            this._dataSourceTypesListView.MultiSelect = false;
            this._dataSourceTypesListView.HideSelection = false; 
            this._dataSourceTypesListView.ListViewItemSorter = _displayNameComparer;
            this._dataSourceTypesListView.Sorting = SortOrder.Ascending;
            this._dataSourceTypesListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnListViewDoubleClick);
 
            //
            // _descriptionBox 
            // 
            this._descriptionBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._descriptionBox.Location = new System.Drawing.Point(0, 112);
            this._descriptionBox.Name = "_descriptionBox";
            this._descriptionBox.Size = new System.Drawing.Size(544, 55);
            this._descriptionBox.TabIndex = 2; 
            this._descriptionBox.ReadOnly = true;
            this._descriptionBox.Multiline = true; 
            this._descriptionBox.TabStop = false; 
            this._descriptionBox.BackColor = System.Drawing.SystemColors.Control;
            this._descriptionBox.Multiline = true; 

            //
            // _idLabel
            // 
            this._idLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._idLabel.Location = new System.Drawing.Point(0, 176); 
            this._idLabel.Name = "_idLabel";
            this._idLabel.Size = new System.Drawing.Size(544, 16); 
            this._idLabel.TabIndex = 3;

            //
            // _idTextBox 
            //
            this._idTextBox.Location = new System.Drawing.Point(0, 194); 
            this._idTextBox.Name = "_idTextBox"; 
            this._idTextBox.Size = new System.Drawing.Size(220, 20);
            this._idTextBox.TabIndex = 4; 
            this._idTextBox.TextChanged += new System.EventHandler(this.OnIDChanged);

            //
            // Form1 
            //
            TaskPanel.Controls.Add(this._idTextBox); 
            TaskPanel.Controls.Add(this._idLabel); 
            TaskPanel.Controls.Add(this._descriptionBox);
            TaskPanel.Controls.Add(this._dataSourceTypesListView); 
            TaskPanel.Controls.Add(this._selectLabel);
        }

        private IComponent GetComponent() { 
            if (_controlDesigner != null) {
                return _controlDesigner.Component; 
            } 
            return null;
        } 

        private string GetNewDataSourceName(Type dataSourceType) {
            if (dataSourceType != null) {
                ISite site = GetSite(); 
                if (site != null) {
                    INameCreationService nameCreationService = (INameCreationService)(site.GetService(typeof(INameCreationService))); 
 
                    // INameCreationService is an optional host service, so don't assert
                    if (nameCreationService != null) { 
                        return nameCreationService.CreateName(site.Container, dataSourceType);
                    }
                    else {
                        return site.Name + "_DataSource"; 
                    }
                } 
            } 
            return String.Empty;
        } 

        private ISite GetSite() {
            IComponent component = GetComponent();
            if (component != null) { 
                return component.Site;
            } 
            return null; 
        }
 
        private void InitializePanelControls() {
            _selectLabel.Text = SR.GetString(SR.CreateDataSource_SelectType);
            _idLabel.Text = SR.GetString(SR.CreateDataSource_ID);
            OKButton.Enabled = false; 
            this.Text = SR.GetString(SR.CreateDataSource_Title);
            _descriptionBox.Text = SR.GetString(SR.CreateDataSource_SelectTypeDesc); 
 
            // Set the description and caption of the task bar
            AccessibleDescription = SR.GetString(SR.CreateDataSource_Description); 
            CaptionLabel.Text = SR.GetString(SR.CreateDataSource_Caption);

            UpdateFonts();
 
            ISite site = GetSite();
 
            if (site != null) { 
                IComponentDiscoveryService componentDiscoveryService = (IComponentDiscoveryService)(site.GetService(typeof(IComponentDiscoveryService)));
 
                IDesignerHost designerHost = null;

                if (componentDiscoveryService != null) {
                    ICollection types = componentDiscoveryService.GetComponentTypes(designerHost, _dataSourceType); 
                    if (types != null) {
                        ImageList imageList = new ImageList(); 
                        imageList.ColorDepth = ColorDepth.Depth32Bit; 

                        Type[] sortedTypes = new Type[types.Count]; 
                        types.CopyTo(sortedTypes, 0);
                        foreach (Type type in sortedTypes) {
                            AttributeCollection attrs = TypeDescriptor.GetAttributes(type);
                            Bitmap toolboxImage = null; 
                            if (attrs != null) {
                                ToolboxBitmapAttribute bitmapAttr = attrs[typeof(ToolboxBitmapAttribute)] as ToolboxBitmapAttribute; 
                                if (bitmapAttr != null && !bitmapAttr.Equals(ToolboxBitmapAttribute.Default)) { 
                                    toolboxImage = bitmapAttr.GetImage(type, true) as Bitmap;
                                } 
                            }
                            if (toolboxImage == null) {
                                toolboxImage = new Bitmap(this.GetType(), "CustomDataSource.bmp");
                            } 

                            imageList.ImageSize = new Size(32, 32); 
                            imageList.Images.Add(type.FullName, toolboxImage); 
                            _dataSourceTypesListView.Items.Add(new DataSourceListViewItem(type));
                        } 
                        _dataSourceTypesListView.Sort();
                        _dataSourceTypesListView.LargeImageList = imageList;
                    }
                } 
            }
        } 
 
        protected override void OnClosing(CancelEventArgs e) {
            if (DialogResult == DialogResult.OK) { 
                if (_dataSourceTypesListView.SelectedItems.Count > 0) {
                    DataSourceListViewItem selectedItem = _dataSourceTypesListView.SelectedItems[0] as DataSourceListViewItem;
                    Type chosenType = selectedItem.DataSourceType;
 
                    Debug.Assert(chosenType != null, "no chosen type");
 
                    string newDataSource = CreateNewDataSource(chosenType); 
                    if (newDataSource.Length > 0) {
                        _dataSourceID = newDataSource; 
                    }
                    else {  // cancel the event if there was an error creating the datasource
                        e.Cancel = true;
                    } 
                    TypeDescriptor.Refresh(GetComponent());
                } 
            } 
        }
 
        private void OnDataSourceTypeChosen(object sender, EventArgs e) {
            if (_dataSourceTypesListView.SelectedItems.Count > 0) {
                DataSourceListViewItem selectedItem = _dataSourceTypesListView.SelectedItems[0] as DataSourceListViewItem;
                Type chosenType = selectedItem.DataSourceType; 

                _idTextBox.Text = GetNewDataSourceName(chosenType); 
                _descriptionBox.Text = selectedItem.GetDescriptionText(); 
            }
            UpdateOKButtonEnabled(); 
        }

        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e); 
            UpdateFonts();
        } 
 
        private void OnIDChanged(object sender, EventArgs e) {
            UpdateOKButtonEnabled(); 
        }

        private void OnListViewDoubleClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) { 
                DialogResult = DialogResult.OK;
                this.Close(); 
            } 
        }
 
        private void UpdateFonts() {
            _selectLabel.Font = new Font(Font, FontStyle.Bold);
        }
 
        private void UpdateOKButtonEnabled() {
            if (_idTextBox.Text.Length > 0 && _dataSourceTypesListView.SelectedItems.Count > 0) { 
                OKButton.Enabled = true; 
            }
            else { 
                OKButton.Enabled = false;
            }
        }
 
        private class DataSourceListViewItem : ListViewItem {
            Type _dataSourceType; 
            string _displayName; 

            public DataSourceListViewItem(Type dataSourceType) : base() { 
                _dataSourceType = dataSourceType;
                this.Text = GetDisplayName();
                this.ImageKey = _dataSourceType.FullName;
            } 

            public Type DataSourceType { 
                get { 
                    return _dataSourceType;
                } 
            }

            public string GetDescriptionText() {
                AttributeCollection attributes = TypeDescriptor.GetAttributes(_dataSourceType); 
                if (attributes != null) {
                    DescriptionAttribute attribute = attributes[typeof(DescriptionAttribute)] as DescriptionAttribute; 
                    if (attribute != null) { 
                        return attribute.Description;
                    } 
                }
                return String.Empty;
            }
 
            public string GetDisplayName() {
                if (_displayName == null) { 
                    AttributeCollection attributes = TypeDescriptor.GetAttributes(_dataSourceType); 
                    _displayName = String.Empty;
                    if (attributes != null) { 
                        DisplayNameAttribute attribute = attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;
                        if (attribute != null) {
                            _displayName = attribute.DisplayName;
                        } 
                    }
                    if (String.IsNullOrEmpty(_displayName)) { 
                        _displayName = _dataSourceType.Name; 
                    }
                } 
                return _displayName;
            }
        }
 
        private class DisplayNameComparer : IComparer {
            public int Compare(object x, object y) { 
                if (!(x is DataSourceListViewItem) || !(y is DataSourceListViewItem)) { 
                    Debug.Fail("Wrong types passed ty DataSourceComparer.");
                    return 0; 
                }
                return Compare((DataSourceListViewItem)x, (DataSourceListViewItem)y);
            }
 
            private int Compare(DataSourceListViewItem x, DataSourceListViewItem y) {
                StringComparer comparer = StringComparer.Create(CultureInfo.CurrentCulture, true); 
                return comparer.Compare(x.GetDisplayName(), y.GetDisplayName()); 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="CreateDataSourceDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Drawing.Design; 
    using System.Globalization;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
 
    using Control = System.Web.UI.Control;
    using ControlDesigner = System.Web.UI.Design.ControlDesigner; 
    using GridView = System.Web.UI.WebControls.GridView; 

    using BorderStyle = System.Windows.Forms.BorderStyle; 
    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label;
    using ComboBox = System.Windows.Forms.ComboBox;
    using TextBox = System.Windows.Forms.TextBox; 
    using CheckBox = System.Windows.Forms.CheckBox;
    using Panel = System.Windows.Forms.Panel; 
 
    /// <devdoc>
    ///   The CreateDataSource dialog used for web controls.  This is invoked when you select "new DataSource" from the chrome dropdown. 
    /// </devdoc>
    /// <internalonly/>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class CreateDataSourceDialog : TaskForm { 
        private ControlDesigner _controlDesigner;
        private string _controlID; 
        private Type _dataSourceType; 
        private DisplayNameComparer _displayNameComparer;
        private string _dataSourceID; 
        private bool _configure;

        private System.Windows.Forms.Label _selectLabel;
        private System.Windows.Forms.ListView _dataSourceTypesListView; 
        private System.Windows.Forms.TextBox _descriptionBox;
        private System.Windows.Forms.Label _idLabel; 
        private System.Windows.Forms.TextBox _idTextBox; 

        /// <devdoc> 
        ///  Creates a new instance of the class
        /// </devdoc>
        public CreateDataSourceDialog(ControlDesigner controlDesigner, Type dataSourceType, bool configure) : base(controlDesigner.Component.Site) {
            Debug.Assert(dataSourceType != null, "dataSourceType must be specified"); 
            this._controlDesigner = controlDesigner;
            this._controlID = ((System.Web.UI.Control)controlDesigner.Component).ID; 
            this._dataSourceType = dataSourceType; 
            this._configure = configure;
 
            this._displayNameComparer = new DisplayNameComparer();
            Glyph = new Bitmap(this.GetType(), "datasourcewizard.bmp");
            CreatePanel();
        } 

        public string DataSourceID { 
            get { 
                if (_dataSourceID == null) {
                    return String.Empty; 
                }
                return _dataSourceID;
            }
        } 

        protected override string HelpTopic { 
            get { 
                return "net.Asp.DataBoundControl.CreateDataSourceDialog";
            } 
        }

        /// <devdoc>
        ///  Creates a new datasource and adds it to the page, then calls configure on it.  When this 
        ///  function is done, a new datasource should be in the available datasources list, selected.
        /// </devdoc> 
        private string CreateNewDataSource(Type dataSourceType) { 
            string newDataSourceName = _idTextBox.Text;
            string id = String.Empty; 

            if (dataSourceType != null) {
                object dataSourceObject = Activator.CreateInstance(dataSourceType);
                if (dataSourceObject != null) { 
                    Debug.Assert(_dataSourceType.IsAssignableFrom(dataSourceObject.GetType()), "DataSource object created did not implement '" + _dataSourceType.Name + "'.");
                    Control dataSourceControl = dataSourceObject as Control; 
                    if (dataSourceControl != null) { 
                        dataSourceControl.ID = newDataSourceName;
                        ISite site = GetSite(); 
                        if (site != null) {
                            INameCreationService nameCreationService = (INameCreationService)(site.GetService(typeof(INameCreationService)));

                            if (nameCreationService != null) { 
                                try {
                                    nameCreationService.ValidateName(newDataSourceName); 
                                } 
                                catch (Exception ex) {
                                    UIServiceHelper.ShowError((IServiceProvider)site, SR.GetString(SR.CreateDataSource_NameNotValid, ex.Message)); 
                                    _idTextBox.Focus();
                                    return id;
                                }
                                // make sure name is unique 
                                IContainer container = site.Container;
                                if (container != null) { 
                                    ComponentCollection components = container.Components; 
                                    if (components != null) {
                                        if (components[newDataSourceName] != null) { 
                                            UIServiceHelper.ShowError((IServiceProvider)site, SR.GetString(SR.CreateDataSource_NameNotUnique));
                                            _idTextBox.Focus();
                                            return id;
                                        } 

                                    } 
                                } 
                            }
 
                            IDesignerHost designerHost = (IDesignerHost)(site.GetService(typeof(IDesignerHost)));
                            Debug.Assert(designerHost != null, "Did not get DesignerHost service.");

                            if (designerHost != null) { 
                                IComponent rootComponent = designerHost.RootComponent;
                                if (rootComponent != null) { 
                                    WebFormsRootDesigner rootDesigner = designerHost.GetDesigner(rootComponent) as WebFormsRootDesigner; 
                                    if (rootDesigner != null) {
                                        Control referenceControl = GetComponent() as Control; 
                                        id = rootDesigner.AddControlToDocument(dataSourceControl, referenceControl, ControlLocation.After);
                                        Debug.Assert(id == newDataSourceName, "AddControlToDocument returned an unexpected value.");

                                        IDesigner designer = designerHost.GetDesigner(dataSourceControl); 
                                        Debug.Assert(designer != null, "GetDesigner on the new control returned null.");
                                        IDataSourceDesigner dsd = designer as IDataSourceDesigner; 
                                        if (dsd != null) { 
                                            if (dsd.CanConfigure && _configure) {
                                                dsd.Configure(); 
                                            }
                                        }
                                        else {
                                            IHierarchicalDataSourceDesigner hdsd = designer as IHierarchicalDataSourceDesigner; 
                                            if (hdsd != null) {
                                                if (hdsd.CanConfigure && _configure) { 
                                                    hdsd.Configure(); 
                                                }
                                            } 
                                        }
                                    }
                                }
                            } 
                        }
                    } 
                } 
            }
            return id; 
        }

        private void CreatePanel() {
            SuspendLayout(); 

            CreatePanelControls(); 
            InitializePanelControls(); 

            InitializeForm(); 

            ResumeLayout(false);
            PerformLayout();
        } 

        private void CreatePanelControls() { 
            this._selectLabel = new System.Windows.Forms.Label(); 
            this._dataSourceTypesListView = new System.Windows.Forms.ListView();
            this._descriptionBox = new System.Windows.Forms.TextBox(); 
            this._idLabel = new System.Windows.Forms.Label();
            this._idTextBox = new System.Windows.Forms.TextBox();

            // 
            // _selectLabel
            // 
            this._selectLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._selectLabel.Location = new System.Drawing.Point(0, 0); 
            this._selectLabel.Name = "_selectLabel";
            this._selectLabel.Size = new System.Drawing.Size(544, 16);
            this._selectLabel.TabIndex = 0;
 
            //
            // _dataSourceTypesListView 
            // 
            this._dataSourceTypesListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._dataSourceTypesListView.Location = new System.Drawing.Point(0, 18);
            this._dataSourceTypesListView.Name = "_dataSourceTypesListView";
            this._dataSourceTypesListView.Size = new System.Drawing.Size(544, 90);
            this._dataSourceTypesListView.TabIndex = 1; 
            this._dataSourceTypesListView.SelectedIndexChanged += new System.EventHandler(this.OnDataSourceTypeChosen);
            this._dataSourceTypesListView.Alignment = ListViewAlignment.Left; 
            this._dataSourceTypesListView.LabelWrap = true; 
            this._dataSourceTypesListView.MultiSelect = false;
            this._dataSourceTypesListView.HideSelection = false; 
            this._dataSourceTypesListView.ListViewItemSorter = _displayNameComparer;
            this._dataSourceTypesListView.Sorting = SortOrder.Ascending;
            this._dataSourceTypesListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnListViewDoubleClick);
 
            //
            // _descriptionBox 
            // 
            this._descriptionBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._descriptionBox.Location = new System.Drawing.Point(0, 112);
            this._descriptionBox.Name = "_descriptionBox";
            this._descriptionBox.Size = new System.Drawing.Size(544, 55);
            this._descriptionBox.TabIndex = 2; 
            this._descriptionBox.ReadOnly = true;
            this._descriptionBox.Multiline = true; 
            this._descriptionBox.TabStop = false; 
            this._descriptionBox.BackColor = System.Drawing.SystemColors.Control;
            this._descriptionBox.Multiline = true; 

            //
            // _idLabel
            // 
            this._idLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._idLabel.Location = new System.Drawing.Point(0, 176); 
            this._idLabel.Name = "_idLabel";
            this._idLabel.Size = new System.Drawing.Size(544, 16); 
            this._idLabel.TabIndex = 3;

            //
            // _idTextBox 
            //
            this._idTextBox.Location = new System.Drawing.Point(0, 194); 
            this._idTextBox.Name = "_idTextBox"; 
            this._idTextBox.Size = new System.Drawing.Size(220, 20);
            this._idTextBox.TabIndex = 4; 
            this._idTextBox.TextChanged += new System.EventHandler(this.OnIDChanged);

            //
            // Form1 
            //
            TaskPanel.Controls.Add(this._idTextBox); 
            TaskPanel.Controls.Add(this._idLabel); 
            TaskPanel.Controls.Add(this._descriptionBox);
            TaskPanel.Controls.Add(this._dataSourceTypesListView); 
            TaskPanel.Controls.Add(this._selectLabel);
        }

        private IComponent GetComponent() { 
            if (_controlDesigner != null) {
                return _controlDesigner.Component; 
            } 
            return null;
        } 

        private string GetNewDataSourceName(Type dataSourceType) {
            if (dataSourceType != null) {
                ISite site = GetSite(); 
                if (site != null) {
                    INameCreationService nameCreationService = (INameCreationService)(site.GetService(typeof(INameCreationService))); 
 
                    // INameCreationService is an optional host service, so don't assert
                    if (nameCreationService != null) { 
                        return nameCreationService.CreateName(site.Container, dataSourceType);
                    }
                    else {
                        return site.Name + "_DataSource"; 
                    }
                } 
            } 
            return String.Empty;
        } 

        private ISite GetSite() {
            IComponent component = GetComponent();
            if (component != null) { 
                return component.Site;
            } 
            return null; 
        }
 
        private void InitializePanelControls() {
            _selectLabel.Text = SR.GetString(SR.CreateDataSource_SelectType);
            _idLabel.Text = SR.GetString(SR.CreateDataSource_ID);
            OKButton.Enabled = false; 
            this.Text = SR.GetString(SR.CreateDataSource_Title);
            _descriptionBox.Text = SR.GetString(SR.CreateDataSource_SelectTypeDesc); 
 
            // Set the description and caption of the task bar
            AccessibleDescription = SR.GetString(SR.CreateDataSource_Description); 
            CaptionLabel.Text = SR.GetString(SR.CreateDataSource_Caption);

            UpdateFonts();
 
            ISite site = GetSite();
 
            if (site != null) { 
                IComponentDiscoveryService componentDiscoveryService = (IComponentDiscoveryService)(site.GetService(typeof(IComponentDiscoveryService)));
 
                IDesignerHost designerHost = null;

                if (componentDiscoveryService != null) {
                    ICollection types = componentDiscoveryService.GetComponentTypes(designerHost, _dataSourceType); 
                    if (types != null) {
                        ImageList imageList = new ImageList(); 
                        imageList.ColorDepth = ColorDepth.Depth32Bit; 

                        Type[] sortedTypes = new Type[types.Count]; 
                        types.CopyTo(sortedTypes, 0);
                        foreach (Type type in sortedTypes) {
                            AttributeCollection attrs = TypeDescriptor.GetAttributes(type);
                            Bitmap toolboxImage = null; 
                            if (attrs != null) {
                                ToolboxBitmapAttribute bitmapAttr = attrs[typeof(ToolboxBitmapAttribute)] as ToolboxBitmapAttribute; 
                                if (bitmapAttr != null && !bitmapAttr.Equals(ToolboxBitmapAttribute.Default)) { 
                                    toolboxImage = bitmapAttr.GetImage(type, true) as Bitmap;
                                } 
                            }
                            if (toolboxImage == null) {
                                toolboxImage = new Bitmap(this.GetType(), "CustomDataSource.bmp");
                            } 

                            imageList.ImageSize = new Size(32, 32); 
                            imageList.Images.Add(type.FullName, toolboxImage); 
                            _dataSourceTypesListView.Items.Add(new DataSourceListViewItem(type));
                        } 
                        _dataSourceTypesListView.Sort();
                        _dataSourceTypesListView.LargeImageList = imageList;
                    }
                } 
            }
        } 
 
        protected override void OnClosing(CancelEventArgs e) {
            if (DialogResult == DialogResult.OK) { 
                if (_dataSourceTypesListView.SelectedItems.Count > 0) {
                    DataSourceListViewItem selectedItem = _dataSourceTypesListView.SelectedItems[0] as DataSourceListViewItem;
                    Type chosenType = selectedItem.DataSourceType;
 
                    Debug.Assert(chosenType != null, "no chosen type");
 
                    string newDataSource = CreateNewDataSource(chosenType); 
                    if (newDataSource.Length > 0) {
                        _dataSourceID = newDataSource; 
                    }
                    else {  // cancel the event if there was an error creating the datasource
                        e.Cancel = true;
                    } 
                    TypeDescriptor.Refresh(GetComponent());
                } 
            } 
        }
 
        private void OnDataSourceTypeChosen(object sender, EventArgs e) {
            if (_dataSourceTypesListView.SelectedItems.Count > 0) {
                DataSourceListViewItem selectedItem = _dataSourceTypesListView.SelectedItems[0] as DataSourceListViewItem;
                Type chosenType = selectedItem.DataSourceType; 

                _idTextBox.Text = GetNewDataSourceName(chosenType); 
                _descriptionBox.Text = selectedItem.GetDescriptionText(); 
            }
            UpdateOKButtonEnabled(); 
        }

        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e); 
            UpdateFonts();
        } 
 
        private void OnIDChanged(object sender, EventArgs e) {
            UpdateOKButtonEnabled(); 
        }

        private void OnListViewDoubleClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) { 
                DialogResult = DialogResult.OK;
                this.Close(); 
            } 
        }
 
        private void UpdateFonts() {
            _selectLabel.Font = new Font(Font, FontStyle.Bold);
        }
 
        private void UpdateOKButtonEnabled() {
            if (_idTextBox.Text.Length > 0 && _dataSourceTypesListView.SelectedItems.Count > 0) { 
                OKButton.Enabled = true; 
            }
            else { 
                OKButton.Enabled = false;
            }
        }
 
        private class DataSourceListViewItem : ListViewItem {
            Type _dataSourceType; 
            string _displayName; 

            public DataSourceListViewItem(Type dataSourceType) : base() { 
                _dataSourceType = dataSourceType;
                this.Text = GetDisplayName();
                this.ImageKey = _dataSourceType.FullName;
            } 

            public Type DataSourceType { 
                get { 
                    return _dataSourceType;
                } 
            }

            public string GetDescriptionText() {
                AttributeCollection attributes = TypeDescriptor.GetAttributes(_dataSourceType); 
                if (attributes != null) {
                    DescriptionAttribute attribute = attributes[typeof(DescriptionAttribute)] as DescriptionAttribute; 
                    if (attribute != null) { 
                        return attribute.Description;
                    } 
                }
                return String.Empty;
            }
 
            public string GetDisplayName() {
                if (_displayName == null) { 
                    AttributeCollection attributes = TypeDescriptor.GetAttributes(_dataSourceType); 
                    _displayName = String.Empty;
                    if (attributes != null) { 
                        DisplayNameAttribute attribute = attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;
                        if (attribute != null) {
                            _displayName = attribute.DisplayName;
                        } 
                    }
                    if (String.IsNullOrEmpty(_displayName)) { 
                        _displayName = _dataSourceType.Name; 
                    }
                } 
                return _displayName;
            }
        }
 
        private class DisplayNameComparer : IComparer {
            public int Compare(object x, object y) { 
                if (!(x is DataSourceListViewItem) || !(y is DataSourceListViewItem)) { 
                    Debug.Fail("Wrong types passed ty DataSourceComparer.");
                    return 0; 
                }
                return Compare((DataSourceListViewItem)x, (DataSourceListViewItem)y);
            }
 
            private int Compare(DataSourceListViewItem x, DataSourceListViewItem y) {
                StringComparer comparer = StringComparer.Create(CultureInfo.CurrentCulture, true); 
                return comparer.Compare(x.GetDisplayName(), y.GetDisplayName()); 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
