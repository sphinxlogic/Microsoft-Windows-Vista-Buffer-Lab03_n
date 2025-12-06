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

    using WebTreeNode = System.Web.UI.WebControls.TreeNode; 
    using WebTreeNodeCollection = System.Web.UI.WebControls.TreeNodeCollection;
    using WebTreeView = System.Web.UI.WebControls.TreeView; 
 
    using WinTreeNode = System.Windows.Forms.TreeNode;
    using WinTreeNodeCollection = System.Windows.Forms.TreeNodeCollection; 
    using WinTreeView = System.Windows.Forms.TreeView;
    using BorderStyle = System.Windows.Forms.BorderStyle;
    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label; 
    using Panel = System.Windows.Forms.Panel;
 
    /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditorDialog"]/*' /> 
    internal sealed class TreeNodeCollectionEditorDialog : CollectionEditorDialog {
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

        private WebTreeView _webTreeView; 
        private TreeViewDesigner _treeViewDesigner;

        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditorDialog.TreeNodeCollectionEditorDialog"]/*' />
        public TreeNodeCollectionEditorDialog(WebTreeView treeView, TreeViewDesigner treeViewDesigner) : base(treeView.Site) { 
            _webTreeView = treeView;
            _treeViewDesigner = treeViewDesigner; 
 
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
            _addRootButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_AddRoot), 3);
            _addChildButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_AddChild), 2);
            _removeButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_Remove), 4); 
            _moveUpButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_MoveUp), 5);
            _moveDownButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_MoveDown), 6); 
            _indentButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_Indent), 1); 
            _unindentButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_Unindent), 0);
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
            _propertyGrid.Site = _webTreeView.Site; 
            //
            // _okButton
            //
            _okButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right); 
            _okButton.FlatStyle = FlatStyle.System;
            _okButton.Location = new Point(309, 296); 
            _okButton.Name = "_okButton"; 
            _okButton.Size = new Size(75, 23);
            _okButton.TabIndex = 9; 
            _okButton.Text = SR.GetString(SR.TreeNodeCollectionEditor_OK);
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
            _cancelButton.Text = SR.GetString(SR.TreeNodeCollectionEditor_Cancel);
            _cancelButton.Click += new EventHandler(OnCancelButtonClick); 
            //
            // _propertiesLabel 
            // 
            _propertiesLabel.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            _propertiesLabel.Location = new Point(260, 12); 
            _propertiesLabel.Name = "_propertiesLabel";
            _propertiesLabel.Size = new Size(204, 14);
            _propertiesLabel.TabIndex = 2;
            _propertiesLabel.Text = SR.GetString(SR.TreeNodeCollectionEditor_Properties); 
            //
            // _nodesLabel 
            // 
            _nodesLabel.Location = new Point(12, 12);
            _nodesLabel.Name = "_nodesLabel"; 
            _nodesLabel.Size = new Size(100, 14);
            _nodesLabel.TabIndex = 0;
            _nodesLabel.Text = SR.GetString(SR.TreeNodeCollectionEditor_Nodes);
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
            Text = SR.GetString(SR.TreeNodeCollectionEditor_Title);
 
            InitializeForm();

            _treeViewPanel.ResumeLayout(false);
            ResumeLayout(false); 
        }
 
        protected override string HelpTopic { 
            get {
                return "net.Asp.TreeView.CollectionEditor"; 
            }
        }

        /// <devdoc> 
        ///     Load the WinForms nodes from the Web TreeView
        /// </devdoc> 
        private void LoadNodes(WinTreeNodeCollection destNodes, WebTreeNodeCollection sourceNodes) { 
            foreach (WebTreeNode node in sourceNodes) {
                TreeNodeContainer newNode = new TreeNodeContainer(); 
                destNodes.Add(newNode);
                newNode.Text = node.Text;
                WebTreeNode clonedNode = (WebTreeNode)((ICloneable)node).Clone();
                _treeViewDesigner.RegisterClone(node, clonedNode); 
                newNode.WebTreeNode = clonedNode;
 
                if (node.ChildNodes.Count > 0) { 
                    LoadNodes(newNode.Nodes, node.ChildNodes);
                } 
            }
        }

        /// <devdoc> 
        ///     Add a new root node
        /// </devdoc> 
        private void OnAddRootButtonClick() { 
            TreeNodeContainer node = new TreeNodeContainer();
            _treeView.Nodes.Add(node); 

            string newNodeText = SR.GetString(SR.TreeNodeCollectionEditor_NewNodeText);
            node.Text = newNodeText;
            node.WebTreeNode.Text = newNodeText; 

            _treeView.SelectedNode = node; 
        } 

        /// <devdoc> 
        ///     Add a child node to the current node;
        /// </devdoc>
        private void OnAddChildButtonClick() {
            WinTreeNode selectedNode = _treeView.SelectedNode; 
            if (selectedNode != null) {
                TreeNodeContainer node = new TreeNodeContainer(); 
                selectedNode.Nodes.Add(node); 

                string newNodeText = SR.GetString(SR.TreeNodeCollectionEditor_NewNodeText); 
                node.Text = newNodeText;
                node.WebTreeNode.Text = newNodeText;

                if (!selectedNode.IsExpanded) { 
                    selectedNode.Expand();
                } 
 
                _treeView.SelectedNode = node;
            } 
        }

        private void OnCancelButtonClick(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel; 
            Close();
        } 
 
        /// <devdoc>
        ///     Moves the selected node to be a child of the previous node 
        /// </devdoc>
        private void OnIndentButtonClick() {
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
        ///     Move the selected node down in its set of siblings 
        /// </devdoc> 
        private void OnMoveDownButtonClick() {
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
            SaveNodes(_webTreeView.Nodes, _treeView.Nodes);
            DialogResult = DialogResult.OK;
            Close();
        } 

        /// <devdoc> 
        ///     Update the WinForms TreeNode's text value to be the one changed in the property grid. 
        /// </devdoc>
        private void OnPropertyGridPropertyValueChanged(object sender, PropertyValueChangedEventArgs e) { 
            TreeNodeContainer selectedNode = (TreeNodeContainer)_treeView.SelectedNode;
            if (selectedNode != null) {
                selectedNode.Text = selectedNode.WebTreeNode.Text;
 
//                UpdateExpandStateRecursive(_treeView.Nodes);
            } 
            _propertyGrid.Refresh(); 
        }
 
        /// <devdoc>
        ///     Removes the selected node
        /// </devdoc>
        private void OnRemoveButtonClick() { 
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
        ///     Load nodes in the first activation to workaround the TreeView horz scrollbar bug.  Also fix all the
        ///     expand states of the nodes (another WinForms workaround).  Select the first node if there is one.
        /// </devdoc>
        protected override void OnInitialActivated(EventArgs e) { 
            base.OnInitialActivated(e);
 
            LoadNodes(_treeView.Nodes, _webTreeView.Nodes); 

//            UpdateExpandStateRecursive(_treeView.Nodes); 
            if (_treeView.Nodes.Count > 0) {
                _treeView.SelectedNode = _treeView.Nodes[0];
            }
 
            UpdateEnabledState();
        } 
/* 
        private void OnTreeViewAfterCollapse(object sender, TreeViewEventArgs e) {
            if (e.Node != null) { 
                ((TreeNodeContainer)e.Node).WebTreeNode.Expanded = false;
                _propertyGrid.Refresh();
            }
        } 

        private void OnTreeViewAfterExpand(object sender, TreeViewEventArgs e) { 
            if (e.Node != null) { 
                ((TreeNodeContainer)e.Node).WebTreeNode.Expanded = true;
                _propertyGrid.Refresh(); 
            }
        }
*/
        /// <devdoc> 
        ///     Update the property grid's selected object and enabled states of the buttons
        /// </devdoc> 
        private void OnTreeViewAfterSelect(object sender, TreeViewEventArgs e) { 
            if (e.Node != null) {
                _propertyGrid.SelectedObject = ((TreeNodeContainer)e.Node).WebTreeNode; 
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
        ///     Moves the selected node to be a sibling of its parent node
        /// </devdoc>
        private void OnUnindentButtonClick() { 
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
        ///     Save all the created/edited nodes to the web TreeView 
        /// </devdoc> 
        private void SaveNodes(WebTreeNodeCollection destNodes, WinTreeNodeCollection sourceNodes) {
            destNodes.Clear(); 
            foreach (TreeNodeContainer node in sourceNodes) {
                WebTreeNode newNode = node.WebTreeNode;
//                newNode.Expanded = node.IsExpanded;
                destNodes.Add(newNode); 

                if (node.Nodes.Count > 0) { 
                    SaveNodes(newNode.ChildNodes, node.Nodes); 
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
/*
        /// <devdoc> 
        ///     Syncs the WinTreeNodes' expand that with that of the WebTreeNodes
        /// </devdoc>
        private void UpdateExpandStateRecursive(WinTreeNodeCollection nodes) {
            foreach (TreeNodeContainer node in nodes) { 
                if (node.WebTreeNode.Expanded) {
                    node.Expand(); 
                    if (node.Nodes.Count > 0) { 
                        UpdateExpandStateRecursive(node.Nodes);
                    } 
                }
                else {
                    node.Collapse();
                } 
            }
        } 
*/ 
        /// <devdoc>
        ///     Covenience class for storing a Web TreeNode inside a WinForms TreeNode 
        /// </devdoc>
        private class TreeNodeContainer : WinTreeNode {
            private WebTreeNode _webTreeNode;
 
            public WebTreeNode WebTreeNode {
                get { 
                    if (_webTreeNode == null) { 
                        _webTreeNode = new WebTreeNode();
                    } 
                    return _webTreeNode;
                }
                set {
                    _webTreeNode = value; 
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

    using WebTreeNode = System.Web.UI.WebControls.TreeNode; 
    using WebTreeNodeCollection = System.Web.UI.WebControls.TreeNodeCollection;
    using WebTreeView = System.Web.UI.WebControls.TreeView; 
 
    using WinTreeNode = System.Windows.Forms.TreeNode;
    using WinTreeNodeCollection = System.Windows.Forms.TreeNodeCollection; 
    using WinTreeView = System.Windows.Forms.TreeView;
    using BorderStyle = System.Windows.Forms.BorderStyle;
    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label; 
    using Panel = System.Windows.Forms.Panel;
 
    /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditorDialog"]/*' /> 
    internal sealed class TreeNodeCollectionEditorDialog : CollectionEditorDialog {
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

        private WebTreeView _webTreeView; 
        private TreeViewDesigner _treeViewDesigner;

        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditorDialog.TreeNodeCollectionEditorDialog"]/*' />
        public TreeNodeCollectionEditorDialog(WebTreeView treeView, TreeViewDesigner treeViewDesigner) : base(treeView.Site) { 
            _webTreeView = treeView;
            _treeViewDesigner = treeViewDesigner; 
 
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
            _addRootButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_AddRoot), 3);
            _addChildButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_AddChild), 2);
            _removeButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_Remove), 4); 
            _moveUpButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_MoveUp), 5);
            _moveDownButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_MoveDown), 6); 
            _indentButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_Indent), 1); 
            _unindentButton = CreatePushButton(SR.GetString(SR.TreeNodeCollectionEditor_Unindent), 0);
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
            _propertyGrid.Site = _webTreeView.Site; 
            //
            // _okButton
            //
            _okButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right); 
            _okButton.FlatStyle = FlatStyle.System;
            _okButton.Location = new Point(309, 296); 
            _okButton.Name = "_okButton"; 
            _okButton.Size = new Size(75, 23);
            _okButton.TabIndex = 9; 
            _okButton.Text = SR.GetString(SR.TreeNodeCollectionEditor_OK);
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
            _cancelButton.Text = SR.GetString(SR.TreeNodeCollectionEditor_Cancel);
            _cancelButton.Click += new EventHandler(OnCancelButtonClick); 
            //
            // _propertiesLabel 
            // 
            _propertiesLabel.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            _propertiesLabel.Location = new Point(260, 12); 
            _propertiesLabel.Name = "_propertiesLabel";
            _propertiesLabel.Size = new Size(204, 14);
            _propertiesLabel.TabIndex = 2;
            _propertiesLabel.Text = SR.GetString(SR.TreeNodeCollectionEditor_Properties); 
            //
            // _nodesLabel 
            // 
            _nodesLabel.Location = new Point(12, 12);
            _nodesLabel.Name = "_nodesLabel"; 
            _nodesLabel.Size = new Size(100, 14);
            _nodesLabel.TabIndex = 0;
            _nodesLabel.Text = SR.GetString(SR.TreeNodeCollectionEditor_Nodes);
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
            Text = SR.GetString(SR.TreeNodeCollectionEditor_Title);
 
            InitializeForm();

            _treeViewPanel.ResumeLayout(false);
            ResumeLayout(false); 
        }
 
        protected override string HelpTopic { 
            get {
                return "net.Asp.TreeView.CollectionEditor"; 
            }
        }

        /// <devdoc> 
        ///     Load the WinForms nodes from the Web TreeView
        /// </devdoc> 
        private void LoadNodes(WinTreeNodeCollection destNodes, WebTreeNodeCollection sourceNodes) { 
            foreach (WebTreeNode node in sourceNodes) {
                TreeNodeContainer newNode = new TreeNodeContainer(); 
                destNodes.Add(newNode);
                newNode.Text = node.Text;
                WebTreeNode clonedNode = (WebTreeNode)((ICloneable)node).Clone();
                _treeViewDesigner.RegisterClone(node, clonedNode); 
                newNode.WebTreeNode = clonedNode;
 
                if (node.ChildNodes.Count > 0) { 
                    LoadNodes(newNode.Nodes, node.ChildNodes);
                } 
            }
        }

        /// <devdoc> 
        ///     Add a new root node
        /// </devdoc> 
        private void OnAddRootButtonClick() { 
            TreeNodeContainer node = new TreeNodeContainer();
            _treeView.Nodes.Add(node); 

            string newNodeText = SR.GetString(SR.TreeNodeCollectionEditor_NewNodeText);
            node.Text = newNodeText;
            node.WebTreeNode.Text = newNodeText; 

            _treeView.SelectedNode = node; 
        } 

        /// <devdoc> 
        ///     Add a child node to the current node;
        /// </devdoc>
        private void OnAddChildButtonClick() {
            WinTreeNode selectedNode = _treeView.SelectedNode; 
            if (selectedNode != null) {
                TreeNodeContainer node = new TreeNodeContainer(); 
                selectedNode.Nodes.Add(node); 

                string newNodeText = SR.GetString(SR.TreeNodeCollectionEditor_NewNodeText); 
                node.Text = newNodeText;
                node.WebTreeNode.Text = newNodeText;

                if (!selectedNode.IsExpanded) { 
                    selectedNode.Expand();
                } 
 
                _treeView.SelectedNode = node;
            } 
        }

        private void OnCancelButtonClick(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel; 
            Close();
        } 
 
        /// <devdoc>
        ///     Moves the selected node to be a child of the previous node 
        /// </devdoc>
        private void OnIndentButtonClick() {
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
        ///     Move the selected node down in its set of siblings 
        /// </devdoc> 
        private void OnMoveDownButtonClick() {
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
            SaveNodes(_webTreeView.Nodes, _treeView.Nodes);
            DialogResult = DialogResult.OK;
            Close();
        } 

        /// <devdoc> 
        ///     Update the WinForms TreeNode's text value to be the one changed in the property grid. 
        /// </devdoc>
        private void OnPropertyGridPropertyValueChanged(object sender, PropertyValueChangedEventArgs e) { 
            TreeNodeContainer selectedNode = (TreeNodeContainer)_treeView.SelectedNode;
            if (selectedNode != null) {
                selectedNode.Text = selectedNode.WebTreeNode.Text;
 
//                UpdateExpandStateRecursive(_treeView.Nodes);
            } 
            _propertyGrid.Refresh(); 
        }
 
        /// <devdoc>
        ///     Removes the selected node
        /// </devdoc>
        private void OnRemoveButtonClick() { 
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
        ///     Load nodes in the first activation to workaround the TreeView horz scrollbar bug.  Also fix all the
        ///     expand states of the nodes (another WinForms workaround).  Select the first node if there is one.
        /// </devdoc>
        protected override void OnInitialActivated(EventArgs e) { 
            base.OnInitialActivated(e);
 
            LoadNodes(_treeView.Nodes, _webTreeView.Nodes); 

//            UpdateExpandStateRecursive(_treeView.Nodes); 
            if (_treeView.Nodes.Count > 0) {
                _treeView.SelectedNode = _treeView.Nodes[0];
            }
 
            UpdateEnabledState();
        } 
/* 
        private void OnTreeViewAfterCollapse(object sender, TreeViewEventArgs e) {
            if (e.Node != null) { 
                ((TreeNodeContainer)e.Node).WebTreeNode.Expanded = false;
                _propertyGrid.Refresh();
            }
        } 

        private void OnTreeViewAfterExpand(object sender, TreeViewEventArgs e) { 
            if (e.Node != null) { 
                ((TreeNodeContainer)e.Node).WebTreeNode.Expanded = true;
                _propertyGrid.Refresh(); 
            }
        }
*/
        /// <devdoc> 
        ///     Update the property grid's selected object and enabled states of the buttons
        /// </devdoc> 
        private void OnTreeViewAfterSelect(object sender, TreeViewEventArgs e) { 
            if (e.Node != null) {
                _propertyGrid.SelectedObject = ((TreeNodeContainer)e.Node).WebTreeNode; 
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
        ///     Moves the selected node to be a sibling of its parent node
        /// </devdoc>
        private void OnUnindentButtonClick() { 
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
        ///     Save all the created/edited nodes to the web TreeView 
        /// </devdoc> 
        private void SaveNodes(WebTreeNodeCollection destNodes, WinTreeNodeCollection sourceNodes) {
            destNodes.Clear(); 
            foreach (TreeNodeContainer node in sourceNodes) {
                WebTreeNode newNode = node.WebTreeNode;
//                newNode.Expanded = node.IsExpanded;
                destNodes.Add(newNode); 

                if (node.Nodes.Count > 0) { 
                    SaveNodes(newNode.ChildNodes, node.Nodes); 
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
/*
        /// <devdoc> 
        ///     Syncs the WinTreeNodes' expand that with that of the WebTreeNodes
        /// </devdoc>
        private void UpdateExpandStateRecursive(WinTreeNodeCollection nodes) {
            foreach (TreeNodeContainer node in nodes) { 
                if (node.WebTreeNode.Expanded) {
                    node.Expand(); 
                    if (node.Nodes.Count > 0) { 
                        UpdateExpandStateRecursive(node.Nodes);
                    } 
                }
                else {
                    node.Collapse();
                } 
            }
        } 
*/ 
        /// <devdoc>
        ///     Covenience class for storing a Web TreeNode inside a WinForms TreeNode 
        /// </devdoc>
        private class TreeNodeContainer : WinTreeNode {
            private WebTreeNode _webTreeNode;
 
            public WebTreeNode WebTreeNode {
                get { 
                    if (_webTreeNode == null) { 
                        _webTreeNode = new WebTreeNode();
                    } 
                    return _webTreeNode;
                }
                set {
                    _webTreeNode = value; 
                }
            } 
        } 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
