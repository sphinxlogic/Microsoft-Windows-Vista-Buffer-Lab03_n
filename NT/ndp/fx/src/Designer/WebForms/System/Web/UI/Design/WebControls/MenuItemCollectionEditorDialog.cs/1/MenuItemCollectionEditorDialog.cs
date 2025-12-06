//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2002' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
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
    using System.Runtime.InteropServices; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design;

    using WebMenuItem = System.Web.UI.WebControls.MenuItem; 
    using WebMenuItemCollection = System.Web.UI.WebControls.MenuItemCollection;
    using WebMenu = System.Web.UI.WebControls.Menu; 
 
    using WinTreeNode = System.Windows.Forms.TreeNode;
    using WinTreeNodeCollection = System.Windows.Forms.TreeNodeCollection; 
    using WinTreeView = System.Windows.Forms.TreeView;
    using BorderStyle = System.Windows.Forms.BorderStyle;
    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label; 
    using Panel = System.Windows.Forms.Panel;
 
    internal sealed class MenuItemCollectionEditorDialog : CollectionEditorDialog { 
        private Panel _treeViewPanel;
        private WinTreeView _treeView; 
        private PropertyGrid _propertyGrid;
        private Button _okButton;
        private Button _cancelButton;
 
        private Label _propertiesLabel;
        private Label _nodesLabel; 
        private ToolStripButton _addRootButton; 
        private ToolStripButton _addChildButton;
        private ToolStripButton _removeButton; 
        private ToolStripButton _moveUpButton;
        private ToolStripButton _moveDownButton;
        private ToolStripButton _indentButton;
        private ToolStripButton _unindentButton; 
        private ToolStripSeparator _toolBarSeparator;
        private ToolStrip _treeViewToolBar; 
 
        private WebMenu _webMenu;
        private MenuDesigner _menuDesigner; 

        public MenuItemCollectionEditorDialog(WebMenu menu, MenuDesigner menuDesigner) : base(menu.Site) {
            _webMenu = menu;
            _menuDesigner = menuDesigner; 

            _treeViewPanel = new Panel(); 
            _treeView = new WinTreeView(); 
            _treeViewToolBar = new ToolStrip();
            ToolStripRenderer toolStripRenderer = UIServiceHelper.GetToolStripRenderer(ServiceProvider); 
            if (toolStripRenderer != null) {
                _treeViewToolBar.Renderer = toolStripRenderer;
            }
            _propertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider); 
            _okButton = new Button();
            _cancelButton = new Button(); 
            _propertiesLabel = new Label(); 
            _nodesLabel = new Label();
            _addRootButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_AddRoot), 3); 
            _addChildButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_AddChild), 2);
            _removeButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_Remove), 4);
            _moveUpButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_MoveUp), 5);
            _moveDownButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_MoveDown), 6); 
            _indentButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_Indent), 1);
            _unindentButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_Unindent), 0); 
            _toolBarSeparator = new ToolStripSeparator(); 
            _treeViewPanel.SuspendLayout();
            SuspendLayout(); 

            //
            // _treeViewPanel
            // 
            _treeViewPanel.Anchor = (((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Left) 
                | AnchorStyles.Right); 
            _treeViewPanel.BackColor = SystemColors.ControlDark;
            _treeViewPanel.Controls.Add(_treeView); 
            _treeViewPanel.DockPadding.Left = 1;
            _treeViewPanel.DockPadding.Right = 1;
            _treeViewPanel.DockPadding.Bottom = 1;
            _treeViewPanel.DockPadding.Top = 1; 
            _treeViewPanel.Location = new Point(12, 54);
            _treeViewPanel.Name = "_treeViewPanel"; 
            _treeViewPanel.Size = new Size(227, 233); 
            _treeViewPanel.TabIndex = 1;
            // 
            // _treeView
            //
            _treeView.BorderStyle = BorderStyle.None;
            _treeView.Dock = DockStyle.Fill; 
            _treeView.ImageIndex = -1;
            _treeView.HideSelection = false; 
            _treeView.Location = new Point(1, 1); 
            _treeView.Name = "_treeView";
            _treeView.SelectedImageIndex = -1; 
            _treeView.TabIndex = 0;
            _treeView.AfterSelect += new TreeViewEventHandler(OnTreeViewAfterSelect);
            //            _treeView.AfterCollapse += new TreeViewEventHandler(OnTreeViewAfterCollapse);
            //            _treeView.AfterExpand += new TreeViewEventHandler(OnTreeViewAfterExpand); 
            _treeView.KeyDown += new KeyEventHandler(OnTreeViewKeyDown);
            // 
            // _treeViewToolBar 
            //
            _treeViewToolBar.Items.AddRange(new ToolStripItem[] { 
                                                                                                _addRootButton,
                                                                                                _addChildButton,
                                                                                                _removeButton,
                                                                                                _toolBarSeparator, 
                                                                                                _moveUpButton,
                                                                                                _moveDownButton, 
                                                                                                _unindentButton, 
                                                                                                _indentButton});
            _treeViewToolBar.Location = new Point(12, 28); 
            _treeViewToolBar.Anchor = (((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Left)
                | AnchorStyles.Right);
            _treeViewToolBar.AutoSize = false; 
            _treeViewToolBar.Size = new Size(227, 26);
            _treeViewToolBar.CanOverflow = false; 
            Padding toolStripPadding = _treeViewToolBar.Padding; 
            toolStripPadding.Left = 2;
            _treeViewToolBar.Padding = toolStripPadding; 
            _treeViewToolBar.Name = "_treeViewToolBar";
            _treeViewToolBar.ShowItemToolTips = true;
            _treeViewToolBar.GripStyle = ToolStripGripStyle.Hidden;
            _treeViewToolBar.TabIndex = 1; 
            _treeViewToolBar.TabStop = true;
            _treeViewToolBar.ItemClicked += new ToolStripItemClickedEventHandler(OnTreeViewToolBarButtonClick); 
            // 
            // _propertyGrid
            // 
            _propertyGrid.Anchor = ((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Right);
            _propertyGrid.CommandsVisibleIfAvailable = true;
            _propertyGrid.LargeButtons = false; 
            _propertyGrid.LineColor = SystemColors.ScrollBar;
            _propertyGrid.Location = new Point(260, 28); 
            _propertyGrid.Name = "_propertyGrid"; 
            _propertyGrid.PropertySort = PropertySort.Alphabetical;
            _propertyGrid.Size = new Size(204, 259); 
            _propertyGrid.TabIndex = 3;
            _propertyGrid.Text = SR.GetString(SR.MenuItemCollectionEditor_PropertyGrid);
            _propertyGrid.ToolbarVisible = true;
            _propertyGrid.ViewBackColor = SystemColors.Window; 
            _propertyGrid.ViewForeColor = SystemColors.WindowText;
            _propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(OnPropertyGridPropertyValueChanged); 
            _propertyGrid.Site = _webMenu.Site; 
            //
            // _okButton 
            //
            _okButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            _okButton.FlatStyle = FlatStyle.System;
            _okButton.Location = new Point(309, 296); 
            _okButton.Name = "_okButton";
            _okButton.Size = new Size(75, 23); 
            _okButton.TabIndex = 9; 
            _okButton.Text = SR.GetString(SR.MenuItemCollectionEditor_OK);
            _okButton.Click += new EventHandler(OnOkButtonClick); 
            //
            // _cancelButton
            //
            _cancelButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right); 
            _cancelButton.FlatStyle = FlatStyle.System;
            _cancelButton.Location = new Point(389, 296); 
            _cancelButton.Name = "_cancelButton"; 
            _cancelButton.Size = new Size(75, 23);
            _cancelButton.TabIndex = 10; 
            _cancelButton.Text = SR.GetString(SR.MenuItemCollectionEditor_Cancel);
            _cancelButton.Click += new EventHandler(OnCancelButtonClick);
            //
            // _propertiesLabel 
            //
            _propertiesLabel.Anchor = (AnchorStyles.Top | AnchorStyles.Right); 
            _propertiesLabel.Location = new Point(260, 12); 
            _propertiesLabel.Name = "_propertiesLabel";
            _propertiesLabel.Size = new Size(204, 14); 
            _propertiesLabel.TabIndex = 2;
            _propertiesLabel.Text = SR.GetString(SR.MenuItemCollectionEditor_Properties);
            //
            // _nodesLabel 
            //
            _nodesLabel.Location = new Point(12, 12); 
            _nodesLabel.Name = "_nodesLabel"; 
            _nodesLabel.Size = new Size(100, 14);
            _nodesLabel.TabIndex = 0; 
            _nodesLabel.Text = SR.GetString(SR.MenuItemCollectionEditor_Nodes);
            ImageList images = new ImageList();
            images.ImageSize = new Size(16, 16);
            images.TransparentColor = Color.Magenta; 
            images.Images.AddStrip(new Bitmap(GetType(), "Commands.bmp"));
            _treeViewToolBar.ImageList = images; 
            // 
            // TreeNodeEditor
            // 
            ClientSize = new Size(478, 331);
            CancelButton = _cancelButton;
            Controls.AddRange(new Control[] {
                                                                          _nodesLabel, 
                                                                          _propertiesLabel,
                                                                          _cancelButton, 
                                                                          _okButton, 
                                                                          _propertyGrid,
                                                                          _treeViewPanel, 
                                                                          _treeViewToolBar});
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimumSize = new Size(484, 331);
            Name = "TreeNodeEditor"; 
            SizeGripStyle = SizeGripStyle.Hide;
            Text = SR.GetString(SR.MenuItemCollectionEditor_Title); 
            _treeViewPanel.ResumeLayout(false); 

            InitializeForm(); 

            ResumeLayout(false);
        }
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.Menu.CollectionEditor"; 
            }
        } 

        /// <devdoc>
        ///     Load the WinForms nodes from the Web Menu
        /// </devdoc> 
        private void LoadNodes(WinTreeNodeCollection destNodes, WebMenuItemCollection sourceNodes) {
            foreach (WebMenuItem node in sourceNodes) { 
                MenuItemContainer newNode = new MenuItemContainer(); 
                destNodes.Add(newNode);
                newNode.Text = node.Text; 
                WebMenuItem clonedMenuItem = (WebMenuItem)((ICloneable)node).Clone();
                _menuDesigner.RegisterClone(node, clonedMenuItem);
                newNode.WebMenuItem = clonedMenuItem;
 
                if (node.ChildItems.Count > 0) {
                    LoadNodes(newNode.Nodes, node.ChildItems); 
                } 
            }
        } 

        /// <devdoc>
        ///     Add a child node to the current node;
        /// </devdoc> 
        private void OnAddChildButtonClick() {
            ValidatePropertyGrid(); 
            WinTreeNode selectedNode = _treeView.SelectedNode; 
            if (selectedNode != null) {
                MenuItemContainer node = new MenuItemContainer(); 
                selectedNode.Nodes.Add(node);

                string newNodeText = SR.GetString(SR.MenuItemCollectionEditor_NewNodeText);
                node.Text = newNodeText; 
                node.WebMenuItem.Text = newNodeText;
 
                selectedNode.Expand(); 

                _treeView.SelectedNode = node; 
            }
        }

        /// <devdoc> 
        ///     Add a new root node
        /// </devdoc> 
        private void OnAddRootButtonClick() { 
            ValidatePropertyGrid();
            MenuItemContainer node = new MenuItemContainer(); 
            _treeView.Nodes.Add(node);

            string newNodeText = SR.GetString(SR.MenuItemCollectionEditor_NewNodeText);
            node.Text = newNodeText; 
            node.WebMenuItem.Text = newNodeText;
 
            _treeView.SelectedNode = node; 
        }
 
        private void OnCancelButtonClick(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        } 

        /// <devdoc> 
        ///     Moves the selected node to be a child of the previous node 
        /// </devdoc>
        private void OnIndentButtonClick() { 
            ValidatePropertyGrid();
            WinTreeNode selectedNode = _treeView.SelectedNode;
            if (selectedNode != null) {
                WinTreeNode previousNode = selectedNode.PrevNode; 
                if (previousNode != null) {
                    selectedNode.Remove(); 
                    previousNode.Nodes.Add(selectedNode); 
                    _treeView.SelectedNode = selectedNode;
                } 
            }
        }

        /// <devdoc> 
        ///     Load nodes in the first activation to workaround the Menu horz scrollbar bug.  Also fix all the
        ///     expand states of the nodes (another WinForms workaround).  Select the first node if there is one. 
        /// </devdoc> 
        protected override void OnInitialActivated(EventArgs e) {
            base.OnInitialActivated(e); 

            LoadNodes(_treeView.Nodes, _webMenu.Items);

            if (_treeView.Nodes.Count > 0) { 
                _treeView.SelectedNode = _treeView.Nodes[0];
            } 
 
            UpdateEnabledState();
        } 

        /// <devdoc>
        ///     Update the property grid's selected object and enabled states of the buttons
        /// </devdoc> 
        private void OnTreeViewAfterSelect(object sender, TreeViewEventArgs e) {
            if (e.Node != null) { 
                _propertyGrid.SelectedObject = ((MenuItemContainer)e.Node).WebMenuItem; 
            }
            else { 
                _propertyGrid.SelectedObject = null;
            }

            UpdateEnabledState(); 
        }
 
        private void OnTreeViewKeyDown(object sender, KeyEventArgs e) { 
            if (e.KeyCode == Keys.Insert) {
                if ((ModifierKeys & Keys.Alt) != 0) { 
                    OnAddChildButtonClick();
                }
                else {
                    OnAddRootButtonClick(); 
                }
                e.Handled = true; 
            } 
            else if (e.KeyCode == Keys.Delete) {
                OnRemoveButtonClick(); 
                e.Handled = true;
            }
            else if ((ModifierKeys & Keys.Shift) != 0) {
                if (e.KeyCode == Keys.Up) { 
                    OnMoveUpButtonClick();
                } 
                else if (e.KeyCode == Keys.Down) { 
                    OnMoveDownButtonClick();
                } 
                else if (e.KeyCode == Keys.Left) {
                    OnUnindentButtonClick();
                }
                else if (e.KeyCode == Keys.Right) { 
                    OnIndentButtonClick();
                } 
                e.Handled = true; 
            }
        } 

        private void OnTreeViewToolBarButtonClick(object sender, System.Windows.Forms.ToolStripItemClickedEventArgs e) {
            if (e.ClickedItem == _addRootButton) {
                OnAddRootButtonClick(); 
            }
            else if (e.ClickedItem == _addChildButton) { 
                OnAddChildButtonClick(); 
            }
            else if (e.ClickedItem == _removeButton) { 
                OnRemoveButtonClick();
            }
            else if (e.ClickedItem == _moveUpButton) {
                OnMoveUpButtonClick(); 
            }
            else if (e.ClickedItem == _unindentButton) { 
                OnUnindentButtonClick(); 
            }
            else if (e.ClickedItem == _indentButton) { 
                OnIndentButtonClick();
            }
            else if (e.ClickedItem == _moveDownButton) {
                OnMoveDownButtonClick(); 
            }
        } 
 
        /// <devdoc>
        ///     Move the selected node down in its set of siblings 
        /// </devdoc>
        private void OnMoveDownButtonClick() {
            ValidatePropertyGrid();
            WinTreeNode selectedNode = _treeView.SelectedNode; 
            if (selectedNode != null) {
                WinTreeNode nextNode = selectedNode.NextNode; 
                WinTreeNodeCollection nodes = _treeView.Nodes; 
                if (selectedNode.Parent != null) {
                    nodes = selectedNode.Parent.Nodes; 
                }

                if (nextNode != null) {
                    selectedNode.Remove(); 
                    nodes.Insert(nextNode.Index + 1, selectedNode);
                    _treeView.SelectedNode = selectedNode; 
                } 
            }
        } 

        /// <devdoc>
        ///     Move the selected node up in its set of siblings
        /// </devdoc> 
        private void OnMoveUpButtonClick() {
            ValidatePropertyGrid(); 
            WinTreeNode selectedNode = _treeView.SelectedNode; 
            if (selectedNode != null) {
                WinTreeNode previousNode = selectedNode.PrevNode; 
                WinTreeNodeCollection nodes = _treeView.Nodes;
                if (selectedNode.Parent != null) {
                    nodes = selectedNode.Parent.Nodes;
                } 

                if (previousNode != null) { 
                    selectedNode.Remove(); 
                    nodes.Insert(previousNode.Index, selectedNode);
                    _treeView.SelectedNode = selectedNode; 
                }
            }
        }
 
        private void OnOkButtonClick(object sender, EventArgs e) {
            ValidatePropertyGrid(); 
            SaveNodes(_webMenu.Items, _treeView.Nodes); 
            DialogResult = DialogResult.OK;
            Close(); 
        }

        private void OnPropertyGridPropertyValueChanged(object sender, PropertyValueChangedEventArgs e) {
            ValidatePropertyGrid(); 
            MenuItemContainer selectedNode = (MenuItemContainer)_treeView.SelectedNode;
            if (selectedNode != null) { 
                selectedNode.Text = selectedNode.WebMenuItem.Text; 

                //                UpdateExpandStateRecursive(_treeView.Nodes); 
            }
            _propertyGrid.Refresh();
        }
 
        /// <devdoc>
        ///     Removes the selected node 
        /// </devdoc> 
        private void OnRemoveButtonClick() {
            ValidatePropertyGrid(); 
            WinTreeNode selectedNode = _treeView.SelectedNode;
            if (selectedNode != null) {
                WinTreeNodeCollection nodes = null;
                if (selectedNode.Parent != null) { 
                    nodes = selectedNode.Parent.Nodes;
                } 
                else { 
                    nodes = _treeView.Nodes;
                } 

                if (nodes.Count == 1) {
                    _treeView.SelectedNode = selectedNode.Parent;
                } 
                else if (selectedNode.NextNode != null) {
                    _treeView.SelectedNode = selectedNode.NextNode; 
                } 
                else {
                    _treeView.SelectedNode = selectedNode.PrevNode; 
                }

                selectedNode.Remove();
 
                // Special case here since AfterSelect isn't being called.
                if (_treeView.SelectedNode == null) { 
                    _propertyGrid.SelectedObject = null; 
                }
 
                UpdateEnabledState();
            }
        }
 
        /// <devdoc>
        ///     Moves the selected node to be a sibling of its parent node 
        /// </devdoc> 
        private void OnUnindentButtonClick() {
            ValidatePropertyGrid(); 
            WinTreeNode selectedNode = _treeView.SelectedNode;
            if (selectedNode != null) {
                WinTreeNode parentNode = selectedNode.Parent;
                if (parentNode != null) { 
                    WinTreeNodeCollection nodes = _treeView.Nodes;
                    if (parentNode.Parent != null) { 
                        nodes = parentNode.Parent.Nodes; 
                    }
 
                    if (parentNode != null) {
                        selectedNode.Remove();
                        nodes.Insert(parentNode.Index + 1, selectedNode);
                        _treeView.SelectedNode = selectedNode; 
                    }
                } 
            } 
        }
 
        /// <devdoc>
        ///     Save all the created/edited nodes to the web Menu
        /// </devdoc>
        private void SaveNodes(WebMenuItemCollection destNodes, WinTreeNodeCollection sourceNodes) { 
            ValidatePropertyGrid();
            destNodes.Clear(); 
            foreach (MenuItemContainer node in sourceNodes) { 
                WebMenuItem newNode = node.WebMenuItem;
                destNodes.Add(newNode); 

                if (node.Nodes.Count > 0) {
                    SaveNodes(newNode.ChildItems, node.Nodes);
                } 
            }
        } 
 
        /// <devdoc>
        ///     Sets the enabled states of all the buttons 
        /// </devdoc>
        private void UpdateEnabledState() {
            WinTreeNode selectedNode = _treeView.SelectedNode;
            if (selectedNode != null) { 
                _addChildButton.Enabled = true;
                _removeButton.Enabled = true; 
 
                _moveUpButton.Enabled = (selectedNode.PrevNode != null);
                _moveDownButton.Enabled = (selectedNode.NextNode != null); 
                _indentButton.Enabled = (selectedNode.PrevNode != null);
                _unindentButton.Enabled = (selectedNode.Parent != null);
            }
            else { 
                _addChildButton.Enabled = false;
                _removeButton.Enabled = false; 
                _moveUpButton.Enabled = false; 
                _moveDownButton.Enabled = false;
                _indentButton.Enabled = false; 
                _unindentButton.Enabled = false;
            }
        }
 
        private void ValidatePropertyGrid() {
            MenuItemContainer selectedNode = (MenuItemContainer)_treeView.SelectedNode; 
            if (selectedNode != null) { 
                selectedNode.Text = selectedNode.WebMenuItem.Text;
                if (selectedNode.WebMenuItem.Selected && 
                    !(selectedNode.WebMenuItem.Selectable && selectedNode.WebMenuItem.Enabled)) {

                    UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.MenuItemCollectionEditor_CantSelect));
                    selectedNode.WebMenuItem.Selected = false; 
                    _propertyGrid.Refresh();
                } 
            } 
        }
 
        /// <devdoc>
        ///     Covenience class for storing a Web MenuNode inside a WinForms MenuNode
        /// </devdoc>
        private class MenuItemContainer : WinTreeNode { 
            private WebMenuItem _webMenuNode;
 
            public WebMenuItem WebMenuItem { 
                get {
                    if (_webMenuNode == null) { 
                        _webMenuNode = new WebMenuItem();
                    }
                    return _webMenuNode;
                } 
                set {
                    _webMenuNode = value; 
                } 
            }
        } 

    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2002' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
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
    using System.Runtime.InteropServices; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design;

    using WebMenuItem = System.Web.UI.WebControls.MenuItem; 
    using WebMenuItemCollection = System.Web.UI.WebControls.MenuItemCollection;
    using WebMenu = System.Web.UI.WebControls.Menu; 
 
    using WinTreeNode = System.Windows.Forms.TreeNode;
    using WinTreeNodeCollection = System.Windows.Forms.TreeNodeCollection; 
    using WinTreeView = System.Windows.Forms.TreeView;
    using BorderStyle = System.Windows.Forms.BorderStyle;
    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label; 
    using Panel = System.Windows.Forms.Panel;
 
    internal sealed class MenuItemCollectionEditorDialog : CollectionEditorDialog { 
        private Panel _treeViewPanel;
        private WinTreeView _treeView; 
        private PropertyGrid _propertyGrid;
        private Button _okButton;
        private Button _cancelButton;
 
        private Label _propertiesLabel;
        private Label _nodesLabel; 
        private ToolStripButton _addRootButton; 
        private ToolStripButton _addChildButton;
        private ToolStripButton _removeButton; 
        private ToolStripButton _moveUpButton;
        private ToolStripButton _moveDownButton;
        private ToolStripButton _indentButton;
        private ToolStripButton _unindentButton; 
        private ToolStripSeparator _toolBarSeparator;
        private ToolStrip _treeViewToolBar; 
 
        private WebMenu _webMenu;
        private MenuDesigner _menuDesigner; 

        public MenuItemCollectionEditorDialog(WebMenu menu, MenuDesigner menuDesigner) : base(menu.Site) {
            _webMenu = menu;
            _menuDesigner = menuDesigner; 

            _treeViewPanel = new Panel(); 
            _treeView = new WinTreeView(); 
            _treeViewToolBar = new ToolStrip();
            ToolStripRenderer toolStripRenderer = UIServiceHelper.GetToolStripRenderer(ServiceProvider); 
            if (toolStripRenderer != null) {
                _treeViewToolBar.Renderer = toolStripRenderer;
            }
            _propertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider); 
            _okButton = new Button();
            _cancelButton = new Button(); 
            _propertiesLabel = new Label(); 
            _nodesLabel = new Label();
            _addRootButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_AddRoot), 3); 
            _addChildButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_AddChild), 2);
            _removeButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_Remove), 4);
            _moveUpButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_MoveUp), 5);
            _moveDownButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_MoveDown), 6); 
            _indentButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_Indent), 1);
            _unindentButton = CreatePushButton(SR.GetString(SR.MenuItemCollectionEditor_Unindent), 0); 
            _toolBarSeparator = new ToolStripSeparator(); 
            _treeViewPanel.SuspendLayout();
            SuspendLayout(); 

            //
            // _treeViewPanel
            // 
            _treeViewPanel.Anchor = (((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Left) 
                | AnchorStyles.Right); 
            _treeViewPanel.BackColor = SystemColors.ControlDark;
            _treeViewPanel.Controls.Add(_treeView); 
            _treeViewPanel.DockPadding.Left = 1;
            _treeViewPanel.DockPadding.Right = 1;
            _treeViewPanel.DockPadding.Bottom = 1;
            _treeViewPanel.DockPadding.Top = 1; 
            _treeViewPanel.Location = new Point(12, 54);
            _treeViewPanel.Name = "_treeViewPanel"; 
            _treeViewPanel.Size = new Size(227, 233); 
            _treeViewPanel.TabIndex = 1;
            // 
            // _treeView
            //
            _treeView.BorderStyle = BorderStyle.None;
            _treeView.Dock = DockStyle.Fill; 
            _treeView.ImageIndex = -1;
            _treeView.HideSelection = false; 
            _treeView.Location = new Point(1, 1); 
            _treeView.Name = "_treeView";
            _treeView.SelectedImageIndex = -1; 
            _treeView.TabIndex = 0;
            _treeView.AfterSelect += new TreeViewEventHandler(OnTreeViewAfterSelect);
            //            _treeView.AfterCollapse += new TreeViewEventHandler(OnTreeViewAfterCollapse);
            //            _treeView.AfterExpand += new TreeViewEventHandler(OnTreeViewAfterExpand); 
            _treeView.KeyDown += new KeyEventHandler(OnTreeViewKeyDown);
            // 
            // _treeViewToolBar 
            //
            _treeViewToolBar.Items.AddRange(new ToolStripItem[] { 
                                                                                                _addRootButton,
                                                                                                _addChildButton,
                                                                                                _removeButton,
                                                                                                _toolBarSeparator, 
                                                                                                _moveUpButton,
                                                                                                _moveDownButton, 
                                                                                                _unindentButton, 
                                                                                                _indentButton});
            _treeViewToolBar.Location = new Point(12, 28); 
            _treeViewToolBar.Anchor = (((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Left)
                | AnchorStyles.Right);
            _treeViewToolBar.AutoSize = false; 
            _treeViewToolBar.Size = new Size(227, 26);
            _treeViewToolBar.CanOverflow = false; 
            Padding toolStripPadding = _treeViewToolBar.Padding; 
            toolStripPadding.Left = 2;
            _treeViewToolBar.Padding = toolStripPadding; 
            _treeViewToolBar.Name = "_treeViewToolBar";
            _treeViewToolBar.ShowItemToolTips = true;
            _treeViewToolBar.GripStyle = ToolStripGripStyle.Hidden;
            _treeViewToolBar.TabIndex = 1; 
            _treeViewToolBar.TabStop = true;
            _treeViewToolBar.ItemClicked += new ToolStripItemClickedEventHandler(OnTreeViewToolBarButtonClick); 
            // 
            // _propertyGrid
            // 
            _propertyGrid.Anchor = ((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Right);
            _propertyGrid.CommandsVisibleIfAvailable = true;
            _propertyGrid.LargeButtons = false; 
            _propertyGrid.LineColor = SystemColors.ScrollBar;
            _propertyGrid.Location = new Point(260, 28); 
            _propertyGrid.Name = "_propertyGrid"; 
            _propertyGrid.PropertySort = PropertySort.Alphabetical;
            _propertyGrid.Size = new Size(204, 259); 
            _propertyGrid.TabIndex = 3;
            _propertyGrid.Text = SR.GetString(SR.MenuItemCollectionEditor_PropertyGrid);
            _propertyGrid.ToolbarVisible = true;
            _propertyGrid.ViewBackColor = SystemColors.Window; 
            _propertyGrid.ViewForeColor = SystemColors.WindowText;
            _propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(OnPropertyGridPropertyValueChanged); 
            _propertyGrid.Site = _webMenu.Site; 
            //
            // _okButton 
            //
            _okButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            _okButton.FlatStyle = FlatStyle.System;
            _okButton.Location = new Point(309, 296); 
            _okButton.Name = "_okButton";
            _okButton.Size = new Size(75, 23); 
            _okButton.TabIndex = 9; 
            _okButton.Text = SR.GetString(SR.MenuItemCollectionEditor_OK);
            _okButton.Click += new EventHandler(OnOkButtonClick); 
            //
            // _cancelButton
            //
            _cancelButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right); 
            _cancelButton.FlatStyle = FlatStyle.System;
            _cancelButton.Location = new Point(389, 296); 
            _cancelButton.Name = "_cancelButton"; 
            _cancelButton.Size = new Size(75, 23);
            _cancelButton.TabIndex = 10; 
            _cancelButton.Text = SR.GetString(SR.MenuItemCollectionEditor_Cancel);
            _cancelButton.Click += new EventHandler(OnCancelButtonClick);
            //
            // _propertiesLabel 
            //
            _propertiesLabel.Anchor = (AnchorStyles.Top | AnchorStyles.Right); 
            _propertiesLabel.Location = new Point(260, 12); 
            _propertiesLabel.Name = "_propertiesLabel";
            _propertiesLabel.Size = new Size(204, 14); 
            _propertiesLabel.TabIndex = 2;
            _propertiesLabel.Text = SR.GetString(SR.MenuItemCollectionEditor_Properties);
            //
            // _nodesLabel 
            //
            _nodesLabel.Location = new Point(12, 12); 
            _nodesLabel.Name = "_nodesLabel"; 
            _nodesLabel.Size = new Size(100, 14);
            _nodesLabel.TabIndex = 0; 
            _nodesLabel.Text = SR.GetString(SR.MenuItemCollectionEditor_Nodes);
            ImageList images = new ImageList();
            images.ImageSize = new Size(16, 16);
            images.TransparentColor = Color.Magenta; 
            images.Images.AddStrip(new Bitmap(GetType(), "Commands.bmp"));
            _treeViewToolBar.ImageList = images; 
            // 
            // TreeNodeEditor
            // 
            ClientSize = new Size(478, 331);
            CancelButton = _cancelButton;
            Controls.AddRange(new Control[] {
                                                                          _nodesLabel, 
                                                                          _propertiesLabel,
                                                                          _cancelButton, 
                                                                          _okButton, 
                                                                          _propertyGrid,
                                                                          _treeViewPanel, 
                                                                          _treeViewToolBar});
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimumSize = new Size(484, 331);
            Name = "TreeNodeEditor"; 
            SizeGripStyle = SizeGripStyle.Hide;
            Text = SR.GetString(SR.MenuItemCollectionEditor_Title); 
            _treeViewPanel.ResumeLayout(false); 

            InitializeForm(); 

            ResumeLayout(false);
        }
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.Menu.CollectionEditor"; 
            }
        } 

        /// <devdoc>
        ///     Load the WinForms nodes from the Web Menu
        /// </devdoc> 
        private void LoadNodes(WinTreeNodeCollection destNodes, WebMenuItemCollection sourceNodes) {
            foreach (WebMenuItem node in sourceNodes) { 
                MenuItemContainer newNode = new MenuItemContainer(); 
                destNodes.Add(newNode);
                newNode.Text = node.Text; 
                WebMenuItem clonedMenuItem = (WebMenuItem)((ICloneable)node).Clone();
                _menuDesigner.RegisterClone(node, clonedMenuItem);
                newNode.WebMenuItem = clonedMenuItem;
 
                if (node.ChildItems.Count > 0) {
                    LoadNodes(newNode.Nodes, node.ChildItems); 
                } 
            }
        } 

        /// <devdoc>
        ///     Add a child node to the current node;
        /// </devdoc> 
        private void OnAddChildButtonClick() {
            ValidatePropertyGrid(); 
            WinTreeNode selectedNode = _treeView.SelectedNode; 
            if (selectedNode != null) {
                MenuItemContainer node = new MenuItemContainer(); 
                selectedNode.Nodes.Add(node);

                string newNodeText = SR.GetString(SR.MenuItemCollectionEditor_NewNodeText);
                node.Text = newNodeText; 
                node.WebMenuItem.Text = newNodeText;
 
                selectedNode.Expand(); 

                _treeView.SelectedNode = node; 
            }
        }

        /// <devdoc> 
        ///     Add a new root node
        /// </devdoc> 
        private void OnAddRootButtonClick() { 
            ValidatePropertyGrid();
            MenuItemContainer node = new MenuItemContainer(); 
            _treeView.Nodes.Add(node);

            string newNodeText = SR.GetString(SR.MenuItemCollectionEditor_NewNodeText);
            node.Text = newNodeText; 
            node.WebMenuItem.Text = newNodeText;
 
            _treeView.SelectedNode = node; 
        }
 
        private void OnCancelButtonClick(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        } 

        /// <devdoc> 
        ///     Moves the selected node to be a child of the previous node 
        /// </devdoc>
        private void OnIndentButtonClick() { 
            ValidatePropertyGrid();
            WinTreeNode selectedNode = _treeView.SelectedNode;
            if (selectedNode != null) {
                WinTreeNode previousNode = selectedNode.PrevNode; 
                if (previousNode != null) {
                    selectedNode.Remove(); 
                    previousNode.Nodes.Add(selectedNode); 
                    _treeView.SelectedNode = selectedNode;
                } 
            }
        }

        /// <devdoc> 
        ///     Load nodes in the first activation to workaround the Menu horz scrollbar bug.  Also fix all the
        ///     expand states of the nodes (another WinForms workaround).  Select the first node if there is one. 
        /// </devdoc> 
        protected override void OnInitialActivated(EventArgs e) {
            base.OnInitialActivated(e); 

            LoadNodes(_treeView.Nodes, _webMenu.Items);

            if (_treeView.Nodes.Count > 0) { 
                _treeView.SelectedNode = _treeView.Nodes[0];
            } 
 
            UpdateEnabledState();
        } 

        /// <devdoc>
        ///     Update the property grid's selected object and enabled states of the buttons
        /// </devdoc> 
        private void OnTreeViewAfterSelect(object sender, TreeViewEventArgs e) {
            if (e.Node != null) { 
                _propertyGrid.SelectedObject = ((MenuItemContainer)e.Node).WebMenuItem; 
            }
            else { 
                _propertyGrid.SelectedObject = null;
            }

            UpdateEnabledState(); 
        }
 
        private void OnTreeViewKeyDown(object sender, KeyEventArgs e) { 
            if (e.KeyCode == Keys.Insert) {
                if ((ModifierKeys & Keys.Alt) != 0) { 
                    OnAddChildButtonClick();
                }
                else {
                    OnAddRootButtonClick(); 
                }
                e.Handled = true; 
            } 
            else if (e.KeyCode == Keys.Delete) {
                OnRemoveButtonClick(); 
                e.Handled = true;
            }
            else if ((ModifierKeys & Keys.Shift) != 0) {
                if (e.KeyCode == Keys.Up) { 
                    OnMoveUpButtonClick();
                } 
                else if (e.KeyCode == Keys.Down) { 
                    OnMoveDownButtonClick();
                } 
                else if (e.KeyCode == Keys.Left) {
                    OnUnindentButtonClick();
                }
                else if (e.KeyCode == Keys.Right) { 
                    OnIndentButtonClick();
                } 
                e.Handled = true; 
            }
        } 

        private void OnTreeViewToolBarButtonClick(object sender, System.Windows.Forms.ToolStripItemClickedEventArgs e) {
            if (e.ClickedItem == _addRootButton) {
                OnAddRootButtonClick(); 
            }
            else if (e.ClickedItem == _addChildButton) { 
                OnAddChildButtonClick(); 
            }
            else if (e.ClickedItem == _removeButton) { 
                OnRemoveButtonClick();
            }
            else if (e.ClickedItem == _moveUpButton) {
                OnMoveUpButtonClick(); 
            }
            else if (e.ClickedItem == _unindentButton) { 
                OnUnindentButtonClick(); 
            }
            else if (e.ClickedItem == _indentButton) { 
                OnIndentButtonClick();
            }
            else if (e.ClickedItem == _moveDownButton) {
                OnMoveDownButtonClick(); 
            }
        } 
 
        /// <devdoc>
        ///     Move the selected node down in its set of siblings 
        /// </devdoc>
        private void OnMoveDownButtonClick() {
            ValidatePropertyGrid();
            WinTreeNode selectedNode = _treeView.SelectedNode; 
            if (selectedNode != null) {
                WinTreeNode nextNode = selectedNode.NextNode; 
                WinTreeNodeCollection nodes = _treeView.Nodes; 
                if (selectedNode.Parent != null) {
                    nodes = selectedNode.Parent.Nodes; 
                }

                if (nextNode != null) {
                    selectedNode.Remove(); 
                    nodes.Insert(nextNode.Index + 1, selectedNode);
                    _treeView.SelectedNode = selectedNode; 
                } 
            }
        } 

        /// <devdoc>
        ///     Move the selected node up in its set of siblings
        /// </devdoc> 
        private void OnMoveUpButtonClick() {
            ValidatePropertyGrid(); 
            WinTreeNode selectedNode = _treeView.SelectedNode; 
            if (selectedNode != null) {
                WinTreeNode previousNode = selectedNode.PrevNode; 
                WinTreeNodeCollection nodes = _treeView.Nodes;
                if (selectedNode.Parent != null) {
                    nodes = selectedNode.Parent.Nodes;
                } 

                if (previousNode != null) { 
                    selectedNode.Remove(); 
                    nodes.Insert(previousNode.Index, selectedNode);
                    _treeView.SelectedNode = selectedNode; 
                }
            }
        }
 
        private void OnOkButtonClick(object sender, EventArgs e) {
            ValidatePropertyGrid(); 
            SaveNodes(_webMenu.Items, _treeView.Nodes); 
            DialogResult = DialogResult.OK;
            Close(); 
        }

        private void OnPropertyGridPropertyValueChanged(object sender, PropertyValueChangedEventArgs e) {
            ValidatePropertyGrid(); 
            MenuItemContainer selectedNode = (MenuItemContainer)_treeView.SelectedNode;
            if (selectedNode != null) { 
                selectedNode.Text = selectedNode.WebMenuItem.Text; 

                //                UpdateExpandStateRecursive(_treeView.Nodes); 
            }
            _propertyGrid.Refresh();
        }
 
        /// <devdoc>
        ///     Removes the selected node 
        /// </devdoc> 
        private void OnRemoveButtonClick() {
            ValidatePropertyGrid(); 
            WinTreeNode selectedNode = _treeView.SelectedNode;
            if (selectedNode != null) {
                WinTreeNodeCollection nodes = null;
                if (selectedNode.Parent != null) { 
                    nodes = selectedNode.Parent.Nodes;
                } 
                else { 
                    nodes = _treeView.Nodes;
                } 

                if (nodes.Count == 1) {
                    _treeView.SelectedNode = selectedNode.Parent;
                } 
                else if (selectedNode.NextNode != null) {
                    _treeView.SelectedNode = selectedNode.NextNode; 
                } 
                else {
                    _treeView.SelectedNode = selectedNode.PrevNode; 
                }

                selectedNode.Remove();
 
                // Special case here since AfterSelect isn't being called.
                if (_treeView.SelectedNode == null) { 
                    _propertyGrid.SelectedObject = null; 
                }
 
                UpdateEnabledState();
            }
        }
 
        /// <devdoc>
        ///     Moves the selected node to be a sibling of its parent node 
        /// </devdoc> 
        private void OnUnindentButtonClick() {
            ValidatePropertyGrid(); 
            WinTreeNode selectedNode = _treeView.SelectedNode;
            if (selectedNode != null) {
                WinTreeNode parentNode = selectedNode.Parent;
                if (parentNode != null) { 
                    WinTreeNodeCollection nodes = _treeView.Nodes;
                    if (parentNode.Parent != null) { 
                        nodes = parentNode.Parent.Nodes; 
                    }
 
                    if (parentNode != null) {
                        selectedNode.Remove();
                        nodes.Insert(parentNode.Index + 1, selectedNode);
                        _treeView.SelectedNode = selectedNode; 
                    }
                } 
            } 
        }
 
        /// <devdoc>
        ///     Save all the created/edited nodes to the web Menu
        /// </devdoc>
        private void SaveNodes(WebMenuItemCollection destNodes, WinTreeNodeCollection sourceNodes) { 
            ValidatePropertyGrid();
            destNodes.Clear(); 
            foreach (MenuItemContainer node in sourceNodes) { 
                WebMenuItem newNode = node.WebMenuItem;
                destNodes.Add(newNode); 

                if (node.Nodes.Count > 0) {
                    SaveNodes(newNode.ChildItems, node.Nodes);
                } 
            }
        } 
 
        /// <devdoc>
        ///     Sets the enabled states of all the buttons 
        /// </devdoc>
        private void UpdateEnabledState() {
            WinTreeNode selectedNode = _treeView.SelectedNode;
            if (selectedNode != null) { 
                _addChildButton.Enabled = true;
                _removeButton.Enabled = true; 
 
                _moveUpButton.Enabled = (selectedNode.PrevNode != null);
                _moveDownButton.Enabled = (selectedNode.NextNode != null); 
                _indentButton.Enabled = (selectedNode.PrevNode != null);
                _unindentButton.Enabled = (selectedNode.Parent != null);
            }
            else { 
                _addChildButton.Enabled = false;
                _removeButton.Enabled = false; 
                _moveUpButton.Enabled = false; 
                _moveDownButton.Enabled = false;
                _indentButton.Enabled = false; 
                _unindentButton.Enabled = false;
            }
        }
 
        private void ValidatePropertyGrid() {
            MenuItemContainer selectedNode = (MenuItemContainer)_treeView.SelectedNode; 
            if (selectedNode != null) { 
                selectedNode.Text = selectedNode.WebMenuItem.Text;
                if (selectedNode.WebMenuItem.Selected && 
                    !(selectedNode.WebMenuItem.Selectable && selectedNode.WebMenuItem.Enabled)) {

                    UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.MenuItemCollectionEditor_CantSelect));
                    selectedNode.WebMenuItem.Selected = false; 
                    _propertyGrid.Refresh();
                } 
            } 
        }
 
        /// <devdoc>
        ///     Covenience class for storing a Web MenuNode inside a WinForms MenuNode
        /// </devdoc>
        private class MenuItemContainer : WinTreeNode { 
            private WebMenuItem _webMenuNode;
 
            public WebMenuItem WebMenuItem { 
                get {
                    if (_webMenuNode == null) { 
                        _webMenuNode = new WebMenuItem();
                    }
                    return _webMenuNode;
                } 
                set {
                    _webMenuNode = value; 
                } 
            }
        } 

    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
