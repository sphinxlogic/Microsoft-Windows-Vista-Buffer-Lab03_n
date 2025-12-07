//------------------------------------------------------------------------------ 
// <copyright file="MenuBindingsEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Globalization; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Windows.Forms; 

    using MenuItemBinding = System.Web.UI.WebControls.MenuItemBinding;
    using WebTreeNodeCollection = System.Web.UI.WebControls.TreeNodeCollection;
    using WebMenu = System.Web.UI.WebControls.Menu; 

    internal class MenuBindingsEditorForm : DesignerForm { 
        private Label _schemaLabel; 
        private Label _bindingsLabel;
        private ListBox _bindingsListView; 
        private Button _addBindingButton;
        private Label _propertiesLabel;
        private Button _cancelButton;
        private Button _okButton; 
        private Button _applyButton;
        private PropertyGrid _propertyGrid; 
        private TreeView _schemaTreeView; 
        private Button _moveBindingUpButton;
        private Button _deleteBindingButton; 
        private Button _moveBindingDownButton;
        private Container components = null;

        private WebMenu _menu; 
        private IDataSourceSchema _schema;
 
        public MenuBindingsEditorForm(IServiceProvider serviceProvider, WebMenu menu, MenuDesigner menuDesigner) : base(serviceProvider) { 
            _menu = menu;
 
            InitializeComponent();
            InitializeUI();

            foreach (MenuItemBinding binding in _menu.DataBindings) { 
                MenuItemBinding newItem = (MenuItemBinding)((ICloneable)binding).Clone();
                menuDesigner.RegisterClone(binding, newItem); 
                _bindingsListView.Items.Add(newItem); 
            }
        } 

        private IDataSourceSchema Schema {
            get {
                if (_schema == null) { 
                    IDesignerHost host = (IDesignerHost)ServiceProvider.GetService(typeof(IDesignerHost));
                    if (host != null) { 
                        HierarchicalDataBoundControlDesigner hierarchicalDataBoundControlDesigner = host.GetDesigner(_menu) as HierarchicalDataBoundControlDesigner; 
                        if (hierarchicalDataBoundControlDesigner != null) {
                            DesignerHierarchicalDataSourceView view = hierarchicalDataBoundControlDesigner.DesignerView; 
                            if (view != null) {
                                try {
                                    _schema = view.Schema;
                                } 
                                catch (Exception ex) {
                                    IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)ServiceProvider.GetService(typeof(IComponentDesignerDebugService)); 
                                    if (debugService != null) { 
                                        debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerHierarchicalDataSourceView.Schema", ex.Message));
                                    } 
                                }
                            }
                        }
                    } 
                }
                return _schema; 
            } 
        }
 
        private void AddBinding() {
            TreeNode selectedNode = _schemaTreeView.SelectedNode;
            if (selectedNode != null) {
                MenuItemBinding newBinding = new MenuItemBinding(); 

                if (selectedNode.Text != _schemaTreeView.Nodes[0].Text) { 
                    // Create a new binding based off the schema 
                    newBinding.DataMember = selectedNode.Text;
                    if (((SchemaTreeNode)selectedNode).Duplicate) { 
                        newBinding.Depth = selectedNode.FullPath.Split(_schemaTreeView.PathSeparator[0]).Length - 1;
                    }

                    ((IDataSourceViewSchemaAccessor)newBinding).DataSourceViewSchema = ((SchemaTreeNode)selectedNode).Schema; 

                    // Make sure it doesn't already exist in the list 
                    int index = _bindingsListView.Items.IndexOf(newBinding); 
                    if (index == -1) {
                        _bindingsListView.Items.Add(newBinding); 
                        _bindingsListView.SetSelected(_bindingsListView.Items.Count - 1, true);
                    }
                    else {
                        newBinding = (MenuItemBinding)_bindingsListView.Items[index]; 
                        _bindingsListView.SetSelected(index, true);
                    } 
                } 
                else {
                    // Add a new empty binding 
                    _bindingsListView.Items.Add(newBinding);
                    _bindingsListView.SetSelected(_bindingsListView.Items.Count - 1, true);
                }
 
                // Select the binding in the property grid
                _propertyGrid.SelectedObject = newBinding; 
                _propertyGrid.Refresh(); 

                UpdateEnabledStates(); 
            }

            _bindingsListView.Focus();
        } 

        private void ApplyBindings() { 
            ControlDesigner.InvokeTransactedChange(_menu, new TransactedChangeCallback(ApplyBindingsChangeCallback), null, SR.GetString(SR.MenuDesigner_EditBindingsTransactionDescription)); 
        }
 
        private bool ApplyBindingsChangeCallback(object context) {
            _menu.DataBindings.Clear();
            foreach (MenuItemBinding item in _bindingsListView.Items) {
                _menu.DataBindings.Add(item); 
            }
 
            return true; 
        }
 
        protected override void Dispose( bool disposing ) {
            if( disposing ) {
                if(components != null) {
                    components.Dispose(); 
                }
            } 
            base.Dispose( disposing ); 
        }
 
        private IDataSourceViewSchema FindViewSchema(string viewName, int level) {
            return TreeViewBindingsEditorForm.FindViewSchemaRecursive(Schema, 0, viewName, level, null);
        }
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.Menu.BindingsEditorForm"; 
            }
        } 

        private void InitializeComponent() {
            this._schemaLabel = new Label();
            this._bindingsLabel = new Label(); 
            this._bindingsListView = new ListBox();
            this._addBindingButton = new Button(); 
            this._propertiesLabel = new Label(); 
            this._cancelButton = new Button();
            this._propertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider); 
            this._schemaTreeView = new TreeView();
            this._moveBindingUpButton = new Button();
            this._moveBindingDownButton = new Button();
            this._deleteBindingButton = new Button(); 
            this._okButton = new Button();
            this._applyButton = new Button(); 
            this.SuspendLayout(); 

            // 
            // _schemaLabel
            //
            this._schemaLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            this._schemaLabel.Location = new Point(12, 12); 
            this._schemaLabel.Name = "_schemaLabel";
            this._schemaLabel.Size = new Size(196, 14); 
            this._schemaLabel.TabIndex = 10; 
            //
            // 
            // _bindingsLabel
            //
            this._bindingsLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
            this._bindingsLabel.Location = new Point(12, 186); 
            this._bindingsLabel.Name = "_bindingsLabel";
            this._bindingsLabel.Size = new Size(196, 14); 
            this._bindingsLabel.TabIndex = 25; 
            //
            // _bindingsListView 
            //
            this._bindingsListView.Anchor = AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left;
            this._bindingsListView.Location = new Point(12, 202);
            this._bindingsListView.Name = "_bindingsListView"; 
            this._bindingsListView.Size = new Size(164, 135);
            this._bindingsListView.TabIndex = 30; 
            this._bindingsListView.SelectedIndexChanged += new EventHandler(this.OnBindingsListViewSelectedIndexChanged); 
            this._bindingsListView.GotFocus += new EventHandler(OnBindingsListViewGotFocus);
            // 
            // _addBindingButton
            //
            this._addBindingButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this._addBindingButton.FlatStyle = FlatStyle.System; 
            this._addBindingButton.Location = new Point(133, 154);
            this._addBindingButton.Name = "_addBindingButton"; 
            this._addBindingButton.Size = new Size(75, 23); 
            this._addBindingButton.TabIndex = 20;
            this._addBindingButton.Click += new EventHandler(this.OnAddBindingButtonClick); 
            //
            // _propertiesLabel
            //
            this._propertiesLabel.Anchor = AnchorStyles.Right | AnchorStyles.Top; 
            this._propertiesLabel.Location = new Point(229, 12);
            this._propertiesLabel.Name = "_propertiesLabel"; 
            this._propertiesLabel.Size = new Size(266, 14); 
            this._propertiesLabel.TabIndex = 50;
            // 
            // _cancelButton
            //
            this._cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._cancelButton.FlatStyle = FlatStyle.System; 
            this._cancelButton.Location = new Point(340, 346);
            this._cancelButton.Name = "_cancelButton"; 
            this._cancelButton.TabIndex = 65; 
            this._cancelButton.Click += new EventHandler(this.OnCancelButtonClick);
            // 
            // _okButton
            //
            this._okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._okButton.FlatStyle = FlatStyle.System; 
            this._okButton.Location = new Point(260, 346);
            this._okButton.Name = "_okButton"; 
            this._okButton.TabIndex = 60; 
            this._okButton.Click += new EventHandler(this.OnOKButtonClick);
            // 
            // _applyButton
            //
            this._applyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._applyButton.FlatStyle = FlatStyle.System; 
            this._applyButton.Location = new Point(420, 346);
            this._applyButton.Name = "_applyButton"; 
            this._applyButton.TabIndex = 60; 
            this._applyButton.Click += new EventHandler(this.OnApplyButtonClick);
            this._applyButton.Enabled = false; 
            //
            // _propertyGrid
            //
            this._propertyGrid.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom; 
            this._propertyGrid.CommandsVisibleIfAvailable = true;
            this._propertyGrid.Cursor = Cursors.HSplit; 
            this._propertyGrid.LargeButtons = false; 
            this._propertyGrid.LineColor = SystemColors.ScrollBar;
            this._propertyGrid.Location = new Point(229, 28); 
            this._propertyGrid.Name = "_propertyGrid";
            this._propertyGrid.Size = new Size(266, 309);
            this._propertyGrid.TabIndex = 55;
            this._propertyGrid.Text = SR.GetString(SR.MenuItemCollectionEditor_PropertyGrid); 
            this._propertyGrid.ToolbarVisible = true;
            this._propertyGrid.ViewBackColor = SystemColors.Window; 
            this._propertyGrid.ViewForeColor = SystemColors.WindowText; 
            this._propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(this.OnPropertyGridPropertyValueChanged);
            this._propertyGrid.Site = _menu.Site; 
            //
            // _schemaTreeView
            //
            this._schemaTreeView.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom; 
            this._schemaTreeView.HideSelection = false;
            this._schemaTreeView.ImageIndex = -1; 
            this._schemaTreeView.Location = new Point(12, 28); 
            this._schemaTreeView.Name = "_schemaTreeView";
            this._schemaTreeView.SelectedImageIndex = -1; 
            this._schemaTreeView.Size = new Size(196, 120);
            this._schemaTreeView.TabIndex = 15;
            this._schemaTreeView.AfterSelect += new TreeViewEventHandler(this.OnSchemaTreeViewAfterSelect);
            this._schemaTreeView.GotFocus += new EventHandler(this.OnSchemaTreeViewGotFocus); 
            //
            // _moveBindingUpButton 
            // 
            this._moveBindingUpButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this._moveBindingUpButton.Location = new Point(182, 202); 
            this._moveBindingUpButton.Name = "_moveBindingUpButton";
            this._moveBindingUpButton.Size = new Size(26, 23);
            this._moveBindingUpButton.TabIndex = 35;
            this._moveBindingUpButton.Click += new EventHandler(this.OnMoveBindingUpButtonClick); 
            //
            // _moveBindingDownButton 
            // 
            this._moveBindingDownButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this._moveBindingDownButton.Location = new Point(182, 226); 
            this._moveBindingDownButton.Name = "_moveBindingDownButton";
            this._moveBindingDownButton.Size = new Size(26, 23);
            this._moveBindingDownButton.TabIndex = 40;
            this._moveBindingDownButton.Click += new EventHandler(this.OnMoveBindingDownButtonClick); 
            //
            // _deleteBindingButton 
            // 
            this._deleteBindingButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this._deleteBindingButton.Location = new Point(182, 255); 
            this._deleteBindingButton.Name = "_deleteBindingButton";
            this._deleteBindingButton.Size = new Size(26, 23);
            this._deleteBindingButton.TabIndex = 45;
            this._deleteBindingButton.Click += new EventHandler(this.OnDeleteBindingButtonClick); 

            this.AcceptButton = _okButton; 
            this.CancelButton = _cancelButton; 
            this.ClientSize = new Size(507, 381);
            this.Controls.Add(this._deleteBindingButton); 
            this.Controls.Add(this._moveBindingDownButton);
            this.Controls.Add(this._moveBindingUpButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelButton); 
            this.Controls.Add(this._applyButton);
            this.Controls.Add(this._propertiesLabel); 
            this.Controls.Add(this._addBindingButton); 
            this.Controls.Add(this._bindingsListView);
            this.Controls.Add(this._bindingsLabel); 
            this.Controls.Add(this._schemaTreeView);
            this.Controls.Add(this._schemaLabel);
            this.Controls.Add(this._propertyGrid);
            this.MinimumSize = new Size(507, 381); 
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Name = "MenuBindingsEditor"; 
            this.SizeGripStyle = SizeGripStyle.Hide; 

            InitializeForm(); 

            this.ResumeLayout(false);
        }
 
        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that 
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() { 
            _bindingsLabel.Text = SR.GetString(SR.MenuBindingsEditor_Bindings);
            _schemaLabel.Text = SR.GetString(SR.MenuBindingsEditor_Schema);
            _okButton.Text = SR.GetString(SR.MenuBindingsEditor_OK);
            _applyButton.Text = SR.GetString(SR.MenuBindingsEditor_Apply); 
            _cancelButton.Text = SR.GetString(SR.MenuBindingsEditor_Cancel);
            _propertiesLabel.Text = SR.GetString(SR.MenuBindingsEditor_BindingProperties); 
            _addBindingButton.Text = SR.GetString(SR.MenuBindingsEditor_AddBinding); 
            Text = SR.GetString(SR.MenuBindingsEditor_Title);
 
            Bitmap moveUpBitmap = new Icon(typeof(MenuBindingsEditorForm), "SortUp.ico").ToBitmap();
            moveUpBitmap.MakeTransparent();
            _moveBindingUpButton.Image = moveUpBitmap;
            _moveBindingUpButton.AccessibleName = SR.GetString(SR.MenuBindingsEditor_MoveBindingUpName); 
            _moveBindingUpButton.AccessibleDescription = SR.GetString(SR.MenuBindingsEditor_MoveBindingUpDescription);
 
            Bitmap moveDownBitmap = new Icon(typeof(MenuBindingsEditorForm), "SortDown.ico").ToBitmap(); 
            moveDownBitmap.MakeTransparent();
            _moveBindingDownButton.Image = moveDownBitmap; 
            _moveBindingDownButton.AccessibleName = SR.GetString(SR.MenuBindingsEditor_MoveBindingDownName);
            _moveBindingDownButton.AccessibleDescription = SR.GetString(SR.MenuBindingsEditor_MoveBindingDownDescription);

            Bitmap deleteBitmap = new Icon(typeof(MenuBindingsEditorForm), "Delete.ico").ToBitmap(); 
            deleteBitmap.MakeTransparent();
            _deleteBindingButton.Image = deleteBitmap; 
            _deleteBindingButton.AccessibleName = SR.GetString(SR.MenuBindingsEditor_DeleteBindingName); 
            _deleteBindingButton.AccessibleDescription = SR.GetString(SR.MenuBindingsEditor_DeleteBindingDescription);
 
            Icon = null;
        }

        private void OnApplyButtonClick(object sender, EventArgs e) { 
            ApplyBindings();
            _applyButton.Enabled = false; 
        } 

        private void OnAddBindingButtonClick(object sender, EventArgs e) { 
            _applyButton.Enabled = true;

            AddBinding();
        } 

        private void OnBindingsListViewGotFocus(object sender, EventArgs e) { 
            UpdateSelectedBinding(); 
        }
 
        private void OnBindingsListViewSelectedIndexChanged(object sender, EventArgs e) {
            UpdateSelectedBinding();
        }
 
        private void OnCancelButtonClick(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel; 
            Close(); 
        }
 
        private void OnDeleteBindingButtonClick(object sender, EventArgs e) {
            if (_bindingsListView.SelectedIndices.Count > 0) {
                _applyButton.Enabled = true;
 
                int index = _bindingsListView.SelectedIndices[0];
                _bindingsListView.Items.RemoveAt(index); 
 
                if (index >= _bindingsListView.Items.Count) {
                    index--; 
                }

                if ((index >= 0) && (_bindingsListView.Items.Count > 0)) {
                    _bindingsListView.SetSelected(index, true); 
                }
            } 
        } 

        protected override void OnInitialActivated(EventArgs e) { 
            base.OnInitialActivated(e);

            TreeNode emptyNode = this._schemaTreeView.Nodes.Add(SR.GetString(SR.MenuBindingsEditor_EmptyBindingText));
 
            if (Schema != null) {
                PopulateSchema(Schema); 
                _schemaTreeView.ExpandAll(); 
            }
 
            _schemaTreeView.SelectedNode = emptyNode;

            UpdateEnabledStates();
        } 

        private void OnMoveBindingUpButtonClick(object sender, EventArgs e) { 
            if (_bindingsListView.SelectedIndices.Count > 0) { 
                _applyButton.Enabled = true;
 
                int index = _bindingsListView.SelectedIndices[0];
                if (index > 0) {
                    MenuItemBinding tempItem = (MenuItemBinding)_bindingsListView.Items[index];
                    _bindingsListView.Items.RemoveAt(index); 
                    _bindingsListView.Items.Insert(index - 1, tempItem);
 
                    _bindingsListView.SetSelected(index - 1, true); 
                }
            } 
        }

        private void OnMoveBindingDownButtonClick(object sender, EventArgs e) {
            if (_bindingsListView.SelectedIndices.Count > 0) { 
                _applyButton.Enabled = true;
 
                int index = _bindingsListView.SelectedIndices[0]; 
                if (index + 1 < _bindingsListView.Items.Count) {
                    MenuItemBinding tempItem = (MenuItemBinding)_bindingsListView.Items[index]; 
                    _bindingsListView.Items.RemoveAt(index);
                    _bindingsListView.Items.Insert(index + 1, tempItem);

                    _bindingsListView.SetSelected(index + 1, true); 
                }
            } 
        } 

        private void OnOKButtonClick(object sender, EventArgs e) { 
            try {
                ApplyBindings();
            }
            finally { 
                DialogResult = DialogResult.OK;
                Close(); 
            } 
        }
 
        private void OnPropertyGridPropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
            _applyButton.Enabled = true;

            if (e.ChangedItem.PropertyDescriptor.Name == "DataMember") { 
                string newValue = (string)e.ChangedItem.Value;
                MenuItemBinding newBinding = (MenuItemBinding)_bindingsListView.Items[_bindingsListView.SelectedIndex]; 
                // Force the listbox to refresh the item text (VSWhidbey 393250) 
                _bindingsListView.Items[_bindingsListView.SelectedIndex] = newBinding;
                _bindingsListView.Refresh(); 
                // Repopulate the bindings (VSWhidbey 480074)
                IDataSourceViewSchema viewSchema = FindViewSchema(newValue, newBinding.Depth);
                if (viewSchema != null) {
                    ((IDataSourceViewSchemaAccessor)newBinding).DataSourceViewSchema = viewSchema; 
                }
                _propertyGrid.SelectedObject = newBinding; 
                _propertyGrid.Refresh(); 
            }
            else if (e.ChangedItem.PropertyDescriptor.Name == "Depth") { 
                int newValue = (int)e.ChangedItem.Value;
                MenuItemBinding newBinding = (MenuItemBinding)_bindingsListView.Items[_bindingsListView.SelectedIndex];
                // Repopulate the bindings (VSWhidbey 480074)
                IDataSourceViewSchema viewSchema = FindViewSchema(newBinding.DataMember, newValue); 
                if (viewSchema != null) {
                    ((IDataSourceViewSchemaAccessor)newBinding).DataSourceViewSchema = viewSchema; 
                } 
                _propertyGrid.SelectedObject = newBinding;
                _propertyGrid.Refresh(); 
            }
        }

        private void OnSchemaTreeViewAfterSelect(object sender, TreeViewEventArgs e) { 
            UpdateEnabledStates();
        } 
 
        private void OnSchemaTreeViewGotFocus(object sender, EventArgs e) {
            _propertyGrid.SelectedObject = null; 
        }

        private void PopulateSchema(IDataSourceSchema schema) {
            if (schema == null) { 
                return;
            } 
 
            IDictionary duplicates = new Hashtable();
            IDataSourceViewSchema[] views = schema.GetViews(); 
            if (views != null) {
                for (int i = 0; i < views.Length; i++) {
                    PopulateSchemaRecursive(_schemaTreeView.Nodes, views[i], 0, duplicates);
                } 
            }
        } 
 
        private void PopulateSchemaRecursive(TreeNodeCollection nodes, IDataSourceViewSchema viewSchema, int depth, IDictionary duplicates) {
            if (viewSchema == null) { 
                return;
            }

            SchemaTreeNode childNode = new SchemaTreeNode(viewSchema); 
            nodes.Add(childNode);
 
            SchemaTreeNode duplicateNode = (SchemaTreeNode)duplicates[viewSchema.Name]; 
            if (duplicateNode != null) {
                duplicateNode.Duplicate = true; 
                childNode.Duplicate = true;
            }

            // Associate schema with the existing bindings 
            foreach (MenuItemBinding item in _bindingsListView.Items) {
                if (String.Compare(item.DataMember, viewSchema.Name, StringComparison.OrdinalIgnoreCase) == 0) { 
                    IDataSourceViewSchemaAccessor accessor = (IDataSourceViewSchemaAccessor)item; 
                    if ((depth == item.Depth) || (accessor.DataSourceViewSchema == null)) {
                        accessor.DataSourceViewSchema = viewSchema; 
                    }
                }
            }
 
            IDataSourceViewSchema[] children = viewSchema.GetChildren();
            if (children != null) { 
                for (int i = 0; i < children.Length; i++) { 
                    PopulateSchemaRecursive(childNode.Nodes, children[i], depth + 1, duplicates);
                } 
            }
        }

        private void UpdateEnabledStates() { 
            if (_bindingsListView.SelectedIndices.Count > 0) {
                int index = _bindingsListView.SelectedIndices[0]; 
 
                _moveBindingDownButton.Enabled = (index + 1 < _bindingsListView.Items.Count);
                _moveBindingUpButton.Enabled = (index > 0); 
                _deleteBindingButton.Enabled = true;
            }
            else {
                _moveBindingDownButton.Enabled = false; 
                _moveBindingUpButton.Enabled = false;
                _deleteBindingButton.Enabled = false; 
            } 

            _addBindingButton.Enabled = (_schemaTreeView.SelectedNode != null); 
        }

        private void UpdateSelectedBinding() {
            // Get the new selected binding from the list view (null if there isn't one) 
            MenuItemBinding newBinding = null;
            if (_bindingsListView.SelectedItems.Count > 0) { 
                MenuItemBinding selectedItem = ((MenuItemBinding)_bindingsListView.SelectedItems[0]); 
                newBinding = selectedItem;
            } 

            // Select it in the property grid
            _propertyGrid.SelectedObject = newBinding;
            _propertyGrid.Refresh(); 

            UpdateEnabledStates(); 
        } 

        private class SchemaTreeNode : TreeNode { 
            private IDataSourceViewSchema _schema;
            private bool _duplicate;

            public SchemaTreeNode(IDataSourceViewSchema schema) : base (schema.Name) { 
                _schema = schema;
            } 
 
            public bool Duplicate {
                get { 
                    return _duplicate;
                }
                set {
                    _duplicate = value; 
                }
            } 
 
            public object Schema {
                get { 
                    return _schema;
                }
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="MenuBindingsEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Globalization; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Windows.Forms; 

    using MenuItemBinding = System.Web.UI.WebControls.MenuItemBinding;
    using WebTreeNodeCollection = System.Web.UI.WebControls.TreeNodeCollection;
    using WebMenu = System.Web.UI.WebControls.Menu; 

    internal class MenuBindingsEditorForm : DesignerForm { 
        private Label _schemaLabel; 
        private Label _bindingsLabel;
        private ListBox _bindingsListView; 
        private Button _addBindingButton;
        private Label _propertiesLabel;
        private Button _cancelButton;
        private Button _okButton; 
        private Button _applyButton;
        private PropertyGrid _propertyGrid; 
        private TreeView _schemaTreeView; 
        private Button _moveBindingUpButton;
        private Button _deleteBindingButton; 
        private Button _moveBindingDownButton;
        private Container components = null;

        private WebMenu _menu; 
        private IDataSourceSchema _schema;
 
        public MenuBindingsEditorForm(IServiceProvider serviceProvider, WebMenu menu, MenuDesigner menuDesigner) : base(serviceProvider) { 
            _menu = menu;
 
            InitializeComponent();
            InitializeUI();

            foreach (MenuItemBinding binding in _menu.DataBindings) { 
                MenuItemBinding newItem = (MenuItemBinding)((ICloneable)binding).Clone();
                menuDesigner.RegisterClone(binding, newItem); 
                _bindingsListView.Items.Add(newItem); 
            }
        } 

        private IDataSourceSchema Schema {
            get {
                if (_schema == null) { 
                    IDesignerHost host = (IDesignerHost)ServiceProvider.GetService(typeof(IDesignerHost));
                    if (host != null) { 
                        HierarchicalDataBoundControlDesigner hierarchicalDataBoundControlDesigner = host.GetDesigner(_menu) as HierarchicalDataBoundControlDesigner; 
                        if (hierarchicalDataBoundControlDesigner != null) {
                            DesignerHierarchicalDataSourceView view = hierarchicalDataBoundControlDesigner.DesignerView; 
                            if (view != null) {
                                try {
                                    _schema = view.Schema;
                                } 
                                catch (Exception ex) {
                                    IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)ServiceProvider.GetService(typeof(IComponentDesignerDebugService)); 
                                    if (debugService != null) { 
                                        debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerHierarchicalDataSourceView.Schema", ex.Message));
                                    } 
                                }
                            }
                        }
                    } 
                }
                return _schema; 
            } 
        }
 
        private void AddBinding() {
            TreeNode selectedNode = _schemaTreeView.SelectedNode;
            if (selectedNode != null) {
                MenuItemBinding newBinding = new MenuItemBinding(); 

                if (selectedNode.Text != _schemaTreeView.Nodes[0].Text) { 
                    // Create a new binding based off the schema 
                    newBinding.DataMember = selectedNode.Text;
                    if (((SchemaTreeNode)selectedNode).Duplicate) { 
                        newBinding.Depth = selectedNode.FullPath.Split(_schemaTreeView.PathSeparator[0]).Length - 1;
                    }

                    ((IDataSourceViewSchemaAccessor)newBinding).DataSourceViewSchema = ((SchemaTreeNode)selectedNode).Schema; 

                    // Make sure it doesn't already exist in the list 
                    int index = _bindingsListView.Items.IndexOf(newBinding); 
                    if (index == -1) {
                        _bindingsListView.Items.Add(newBinding); 
                        _bindingsListView.SetSelected(_bindingsListView.Items.Count - 1, true);
                    }
                    else {
                        newBinding = (MenuItemBinding)_bindingsListView.Items[index]; 
                        _bindingsListView.SetSelected(index, true);
                    } 
                } 
                else {
                    // Add a new empty binding 
                    _bindingsListView.Items.Add(newBinding);
                    _bindingsListView.SetSelected(_bindingsListView.Items.Count - 1, true);
                }
 
                // Select the binding in the property grid
                _propertyGrid.SelectedObject = newBinding; 
                _propertyGrid.Refresh(); 

                UpdateEnabledStates(); 
            }

            _bindingsListView.Focus();
        } 

        private void ApplyBindings() { 
            ControlDesigner.InvokeTransactedChange(_menu, new TransactedChangeCallback(ApplyBindingsChangeCallback), null, SR.GetString(SR.MenuDesigner_EditBindingsTransactionDescription)); 
        }
 
        private bool ApplyBindingsChangeCallback(object context) {
            _menu.DataBindings.Clear();
            foreach (MenuItemBinding item in _bindingsListView.Items) {
                _menu.DataBindings.Add(item); 
            }
 
            return true; 
        }
 
        protected override void Dispose( bool disposing ) {
            if( disposing ) {
                if(components != null) {
                    components.Dispose(); 
                }
            } 
            base.Dispose( disposing ); 
        }
 
        private IDataSourceViewSchema FindViewSchema(string viewName, int level) {
            return TreeViewBindingsEditorForm.FindViewSchemaRecursive(Schema, 0, viewName, level, null);
        }
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.Menu.BindingsEditorForm"; 
            }
        } 

        private void InitializeComponent() {
            this._schemaLabel = new Label();
            this._bindingsLabel = new Label(); 
            this._bindingsListView = new ListBox();
            this._addBindingButton = new Button(); 
            this._propertiesLabel = new Label(); 
            this._cancelButton = new Button();
            this._propertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider); 
            this._schemaTreeView = new TreeView();
            this._moveBindingUpButton = new Button();
            this._moveBindingDownButton = new Button();
            this._deleteBindingButton = new Button(); 
            this._okButton = new Button();
            this._applyButton = new Button(); 
            this.SuspendLayout(); 

            // 
            // _schemaLabel
            //
            this._schemaLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            this._schemaLabel.Location = new Point(12, 12); 
            this._schemaLabel.Name = "_schemaLabel";
            this._schemaLabel.Size = new Size(196, 14); 
            this._schemaLabel.TabIndex = 10; 
            //
            // 
            // _bindingsLabel
            //
            this._bindingsLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
            this._bindingsLabel.Location = new Point(12, 186); 
            this._bindingsLabel.Name = "_bindingsLabel";
            this._bindingsLabel.Size = new Size(196, 14); 
            this._bindingsLabel.TabIndex = 25; 
            //
            // _bindingsListView 
            //
            this._bindingsListView.Anchor = AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left;
            this._bindingsListView.Location = new Point(12, 202);
            this._bindingsListView.Name = "_bindingsListView"; 
            this._bindingsListView.Size = new Size(164, 135);
            this._bindingsListView.TabIndex = 30; 
            this._bindingsListView.SelectedIndexChanged += new EventHandler(this.OnBindingsListViewSelectedIndexChanged); 
            this._bindingsListView.GotFocus += new EventHandler(OnBindingsListViewGotFocus);
            // 
            // _addBindingButton
            //
            this._addBindingButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this._addBindingButton.FlatStyle = FlatStyle.System; 
            this._addBindingButton.Location = new Point(133, 154);
            this._addBindingButton.Name = "_addBindingButton"; 
            this._addBindingButton.Size = new Size(75, 23); 
            this._addBindingButton.TabIndex = 20;
            this._addBindingButton.Click += new EventHandler(this.OnAddBindingButtonClick); 
            //
            // _propertiesLabel
            //
            this._propertiesLabel.Anchor = AnchorStyles.Right | AnchorStyles.Top; 
            this._propertiesLabel.Location = new Point(229, 12);
            this._propertiesLabel.Name = "_propertiesLabel"; 
            this._propertiesLabel.Size = new Size(266, 14); 
            this._propertiesLabel.TabIndex = 50;
            // 
            // _cancelButton
            //
            this._cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._cancelButton.FlatStyle = FlatStyle.System; 
            this._cancelButton.Location = new Point(340, 346);
            this._cancelButton.Name = "_cancelButton"; 
            this._cancelButton.TabIndex = 65; 
            this._cancelButton.Click += new EventHandler(this.OnCancelButtonClick);
            // 
            // _okButton
            //
            this._okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._okButton.FlatStyle = FlatStyle.System; 
            this._okButton.Location = new Point(260, 346);
            this._okButton.Name = "_okButton"; 
            this._okButton.TabIndex = 60; 
            this._okButton.Click += new EventHandler(this.OnOKButtonClick);
            // 
            // _applyButton
            //
            this._applyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._applyButton.FlatStyle = FlatStyle.System; 
            this._applyButton.Location = new Point(420, 346);
            this._applyButton.Name = "_applyButton"; 
            this._applyButton.TabIndex = 60; 
            this._applyButton.Click += new EventHandler(this.OnApplyButtonClick);
            this._applyButton.Enabled = false; 
            //
            // _propertyGrid
            //
            this._propertyGrid.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom; 
            this._propertyGrid.CommandsVisibleIfAvailable = true;
            this._propertyGrid.Cursor = Cursors.HSplit; 
            this._propertyGrid.LargeButtons = false; 
            this._propertyGrid.LineColor = SystemColors.ScrollBar;
            this._propertyGrid.Location = new Point(229, 28); 
            this._propertyGrid.Name = "_propertyGrid";
            this._propertyGrid.Size = new Size(266, 309);
            this._propertyGrid.TabIndex = 55;
            this._propertyGrid.Text = SR.GetString(SR.MenuItemCollectionEditor_PropertyGrid); 
            this._propertyGrid.ToolbarVisible = true;
            this._propertyGrid.ViewBackColor = SystemColors.Window; 
            this._propertyGrid.ViewForeColor = SystemColors.WindowText; 
            this._propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(this.OnPropertyGridPropertyValueChanged);
            this._propertyGrid.Site = _menu.Site; 
            //
            // _schemaTreeView
            //
            this._schemaTreeView.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom; 
            this._schemaTreeView.HideSelection = false;
            this._schemaTreeView.ImageIndex = -1; 
            this._schemaTreeView.Location = new Point(12, 28); 
            this._schemaTreeView.Name = "_schemaTreeView";
            this._schemaTreeView.SelectedImageIndex = -1; 
            this._schemaTreeView.Size = new Size(196, 120);
            this._schemaTreeView.TabIndex = 15;
            this._schemaTreeView.AfterSelect += new TreeViewEventHandler(this.OnSchemaTreeViewAfterSelect);
            this._schemaTreeView.GotFocus += new EventHandler(this.OnSchemaTreeViewGotFocus); 
            //
            // _moveBindingUpButton 
            // 
            this._moveBindingUpButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this._moveBindingUpButton.Location = new Point(182, 202); 
            this._moveBindingUpButton.Name = "_moveBindingUpButton";
            this._moveBindingUpButton.Size = new Size(26, 23);
            this._moveBindingUpButton.TabIndex = 35;
            this._moveBindingUpButton.Click += new EventHandler(this.OnMoveBindingUpButtonClick); 
            //
            // _moveBindingDownButton 
            // 
            this._moveBindingDownButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this._moveBindingDownButton.Location = new Point(182, 226); 
            this._moveBindingDownButton.Name = "_moveBindingDownButton";
            this._moveBindingDownButton.Size = new Size(26, 23);
            this._moveBindingDownButton.TabIndex = 40;
            this._moveBindingDownButton.Click += new EventHandler(this.OnMoveBindingDownButtonClick); 
            //
            // _deleteBindingButton 
            // 
            this._deleteBindingButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this._deleteBindingButton.Location = new Point(182, 255); 
            this._deleteBindingButton.Name = "_deleteBindingButton";
            this._deleteBindingButton.Size = new Size(26, 23);
            this._deleteBindingButton.TabIndex = 45;
            this._deleteBindingButton.Click += new EventHandler(this.OnDeleteBindingButtonClick); 

            this.AcceptButton = _okButton; 
            this.CancelButton = _cancelButton; 
            this.ClientSize = new Size(507, 381);
            this.Controls.Add(this._deleteBindingButton); 
            this.Controls.Add(this._moveBindingDownButton);
            this.Controls.Add(this._moveBindingUpButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelButton); 
            this.Controls.Add(this._applyButton);
            this.Controls.Add(this._propertiesLabel); 
            this.Controls.Add(this._addBindingButton); 
            this.Controls.Add(this._bindingsListView);
            this.Controls.Add(this._bindingsLabel); 
            this.Controls.Add(this._schemaTreeView);
            this.Controls.Add(this._schemaLabel);
            this.Controls.Add(this._propertyGrid);
            this.MinimumSize = new Size(507, 381); 
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Name = "MenuBindingsEditor"; 
            this.SizeGripStyle = SizeGripStyle.Hide; 

            InitializeForm(); 

            this.ResumeLayout(false);
        }
 
        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that 
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() { 
            _bindingsLabel.Text = SR.GetString(SR.MenuBindingsEditor_Bindings);
            _schemaLabel.Text = SR.GetString(SR.MenuBindingsEditor_Schema);
            _okButton.Text = SR.GetString(SR.MenuBindingsEditor_OK);
            _applyButton.Text = SR.GetString(SR.MenuBindingsEditor_Apply); 
            _cancelButton.Text = SR.GetString(SR.MenuBindingsEditor_Cancel);
            _propertiesLabel.Text = SR.GetString(SR.MenuBindingsEditor_BindingProperties); 
            _addBindingButton.Text = SR.GetString(SR.MenuBindingsEditor_AddBinding); 
            Text = SR.GetString(SR.MenuBindingsEditor_Title);
 
            Bitmap moveUpBitmap = new Icon(typeof(MenuBindingsEditorForm), "SortUp.ico").ToBitmap();
            moveUpBitmap.MakeTransparent();
            _moveBindingUpButton.Image = moveUpBitmap;
            _moveBindingUpButton.AccessibleName = SR.GetString(SR.MenuBindingsEditor_MoveBindingUpName); 
            _moveBindingUpButton.AccessibleDescription = SR.GetString(SR.MenuBindingsEditor_MoveBindingUpDescription);
 
            Bitmap moveDownBitmap = new Icon(typeof(MenuBindingsEditorForm), "SortDown.ico").ToBitmap(); 
            moveDownBitmap.MakeTransparent();
            _moveBindingDownButton.Image = moveDownBitmap; 
            _moveBindingDownButton.AccessibleName = SR.GetString(SR.MenuBindingsEditor_MoveBindingDownName);
            _moveBindingDownButton.AccessibleDescription = SR.GetString(SR.MenuBindingsEditor_MoveBindingDownDescription);

            Bitmap deleteBitmap = new Icon(typeof(MenuBindingsEditorForm), "Delete.ico").ToBitmap(); 
            deleteBitmap.MakeTransparent();
            _deleteBindingButton.Image = deleteBitmap; 
            _deleteBindingButton.AccessibleName = SR.GetString(SR.MenuBindingsEditor_DeleteBindingName); 
            _deleteBindingButton.AccessibleDescription = SR.GetString(SR.MenuBindingsEditor_DeleteBindingDescription);
 
            Icon = null;
        }

        private void OnApplyButtonClick(object sender, EventArgs e) { 
            ApplyBindings();
            _applyButton.Enabled = false; 
        } 

        private void OnAddBindingButtonClick(object sender, EventArgs e) { 
            _applyButton.Enabled = true;

            AddBinding();
        } 

        private void OnBindingsListViewGotFocus(object sender, EventArgs e) { 
            UpdateSelectedBinding(); 
        }
 
        private void OnBindingsListViewSelectedIndexChanged(object sender, EventArgs e) {
            UpdateSelectedBinding();
        }
 
        private void OnCancelButtonClick(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel; 
            Close(); 
        }
 
        private void OnDeleteBindingButtonClick(object sender, EventArgs e) {
            if (_bindingsListView.SelectedIndices.Count > 0) {
                _applyButton.Enabled = true;
 
                int index = _bindingsListView.SelectedIndices[0];
                _bindingsListView.Items.RemoveAt(index); 
 
                if (index >= _bindingsListView.Items.Count) {
                    index--; 
                }

                if ((index >= 0) && (_bindingsListView.Items.Count > 0)) {
                    _bindingsListView.SetSelected(index, true); 
                }
            } 
        } 

        protected override void OnInitialActivated(EventArgs e) { 
            base.OnInitialActivated(e);

            TreeNode emptyNode = this._schemaTreeView.Nodes.Add(SR.GetString(SR.MenuBindingsEditor_EmptyBindingText));
 
            if (Schema != null) {
                PopulateSchema(Schema); 
                _schemaTreeView.ExpandAll(); 
            }
 
            _schemaTreeView.SelectedNode = emptyNode;

            UpdateEnabledStates();
        } 

        private void OnMoveBindingUpButtonClick(object sender, EventArgs e) { 
            if (_bindingsListView.SelectedIndices.Count > 0) { 
                _applyButton.Enabled = true;
 
                int index = _bindingsListView.SelectedIndices[0];
                if (index > 0) {
                    MenuItemBinding tempItem = (MenuItemBinding)_bindingsListView.Items[index];
                    _bindingsListView.Items.RemoveAt(index); 
                    _bindingsListView.Items.Insert(index - 1, tempItem);
 
                    _bindingsListView.SetSelected(index - 1, true); 
                }
            } 
        }

        private void OnMoveBindingDownButtonClick(object sender, EventArgs e) {
            if (_bindingsListView.SelectedIndices.Count > 0) { 
                _applyButton.Enabled = true;
 
                int index = _bindingsListView.SelectedIndices[0]; 
                if (index + 1 < _bindingsListView.Items.Count) {
                    MenuItemBinding tempItem = (MenuItemBinding)_bindingsListView.Items[index]; 
                    _bindingsListView.Items.RemoveAt(index);
                    _bindingsListView.Items.Insert(index + 1, tempItem);

                    _bindingsListView.SetSelected(index + 1, true); 
                }
            } 
        } 

        private void OnOKButtonClick(object sender, EventArgs e) { 
            try {
                ApplyBindings();
            }
            finally { 
                DialogResult = DialogResult.OK;
                Close(); 
            } 
        }
 
        private void OnPropertyGridPropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
            _applyButton.Enabled = true;

            if (e.ChangedItem.PropertyDescriptor.Name == "DataMember") { 
                string newValue = (string)e.ChangedItem.Value;
                MenuItemBinding newBinding = (MenuItemBinding)_bindingsListView.Items[_bindingsListView.SelectedIndex]; 
                // Force the listbox to refresh the item text (VSWhidbey 393250) 
                _bindingsListView.Items[_bindingsListView.SelectedIndex] = newBinding;
                _bindingsListView.Refresh(); 
                // Repopulate the bindings (VSWhidbey 480074)
                IDataSourceViewSchema viewSchema = FindViewSchema(newValue, newBinding.Depth);
                if (viewSchema != null) {
                    ((IDataSourceViewSchemaAccessor)newBinding).DataSourceViewSchema = viewSchema; 
                }
                _propertyGrid.SelectedObject = newBinding; 
                _propertyGrid.Refresh(); 
            }
            else if (e.ChangedItem.PropertyDescriptor.Name == "Depth") { 
                int newValue = (int)e.ChangedItem.Value;
                MenuItemBinding newBinding = (MenuItemBinding)_bindingsListView.Items[_bindingsListView.SelectedIndex];
                // Repopulate the bindings (VSWhidbey 480074)
                IDataSourceViewSchema viewSchema = FindViewSchema(newBinding.DataMember, newValue); 
                if (viewSchema != null) {
                    ((IDataSourceViewSchemaAccessor)newBinding).DataSourceViewSchema = viewSchema; 
                } 
                _propertyGrid.SelectedObject = newBinding;
                _propertyGrid.Refresh(); 
            }
        }

        private void OnSchemaTreeViewAfterSelect(object sender, TreeViewEventArgs e) { 
            UpdateEnabledStates();
        } 
 
        private void OnSchemaTreeViewGotFocus(object sender, EventArgs e) {
            _propertyGrid.SelectedObject = null; 
        }

        private void PopulateSchema(IDataSourceSchema schema) {
            if (schema == null) { 
                return;
            } 
 
            IDictionary duplicates = new Hashtable();
            IDataSourceViewSchema[] views = schema.GetViews(); 
            if (views != null) {
                for (int i = 0; i < views.Length; i++) {
                    PopulateSchemaRecursive(_schemaTreeView.Nodes, views[i], 0, duplicates);
                } 
            }
        } 
 
        private void PopulateSchemaRecursive(TreeNodeCollection nodes, IDataSourceViewSchema viewSchema, int depth, IDictionary duplicates) {
            if (viewSchema == null) { 
                return;
            }

            SchemaTreeNode childNode = new SchemaTreeNode(viewSchema); 
            nodes.Add(childNode);
 
            SchemaTreeNode duplicateNode = (SchemaTreeNode)duplicates[viewSchema.Name]; 
            if (duplicateNode != null) {
                duplicateNode.Duplicate = true; 
                childNode.Duplicate = true;
            }

            // Associate schema with the existing bindings 
            foreach (MenuItemBinding item in _bindingsListView.Items) {
                if (String.Compare(item.DataMember, viewSchema.Name, StringComparison.OrdinalIgnoreCase) == 0) { 
                    IDataSourceViewSchemaAccessor accessor = (IDataSourceViewSchemaAccessor)item; 
                    if ((depth == item.Depth) || (accessor.DataSourceViewSchema == null)) {
                        accessor.DataSourceViewSchema = viewSchema; 
                    }
                }
            }
 
            IDataSourceViewSchema[] children = viewSchema.GetChildren();
            if (children != null) { 
                for (int i = 0; i < children.Length; i++) { 
                    PopulateSchemaRecursive(childNode.Nodes, children[i], depth + 1, duplicates);
                } 
            }
        }

        private void UpdateEnabledStates() { 
            if (_bindingsListView.SelectedIndices.Count > 0) {
                int index = _bindingsListView.SelectedIndices[0]; 
 
                _moveBindingDownButton.Enabled = (index + 1 < _bindingsListView.Items.Count);
                _moveBindingUpButton.Enabled = (index > 0); 
                _deleteBindingButton.Enabled = true;
            }
            else {
                _moveBindingDownButton.Enabled = false; 
                _moveBindingUpButton.Enabled = false;
                _deleteBindingButton.Enabled = false; 
            } 

            _addBindingButton.Enabled = (_schemaTreeView.SelectedNode != null); 
        }

        private void UpdateSelectedBinding() {
            // Get the new selected binding from the list view (null if there isn't one) 
            MenuItemBinding newBinding = null;
            if (_bindingsListView.SelectedItems.Count > 0) { 
                MenuItemBinding selectedItem = ((MenuItemBinding)_bindingsListView.SelectedItems[0]); 
                newBinding = selectedItem;
            } 

            // Select it in the property grid
            _propertyGrid.SelectedObject = newBinding;
            _propertyGrid.Refresh(); 

            UpdateEnabledStates(); 
        } 

        private class SchemaTreeNode : TreeNode { 
            private IDataSourceViewSchema _schema;
            private bool _duplicate;

            public SchemaTreeNode(IDataSourceViewSchema schema) : base (schema.Name) { 
                _schema = schema;
            } 
 
            public bool Duplicate {
                get { 
                    return _duplicate;
                }
                set {
                    _duplicate = value; 
                }
            } 
 
            public object Schema {
                get { 
                    return _schema;
                }
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
