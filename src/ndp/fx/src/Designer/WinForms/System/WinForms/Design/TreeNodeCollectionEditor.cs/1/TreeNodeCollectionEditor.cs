//------------------------------------------------------------------------------ 
// <copyright file="TreeNodeCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System;
    using System.Design; 
    using System.Collections;
    using Microsoft.Win32; 
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Windows.Forms;
    using System.Windows.Forms.Layout;
    using System.Globalization;
 
    /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor"]/*' />
    /// <devdoc> 
    ///      The TreeNodeCollectionEditor is a collection editor that is specifically 
    ///      designed to edit a TreeNodeCollection.
    /// </devdoc> 
    internal class TreeNodeCollectionEditor : CollectionEditor
    {

        public TreeNodeCollectionEditor() 
            : base(typeof(TreeNodeCollection))
        { 
        } 

        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.CreateCollectionForm"]/*' /> 
        /// <devdoc>
        ///      Creates a new form to show the current collection.  You may inherit
        ///      from CollectionForm to provide your own form.
        /// </devdoc> 
        protected override CollectionForm CreateCollectionForm()
        { 
            return new TreeNodeCollectionForm(this); 
        }
 
        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.HelpTopic"]/*' />
        /// <devdoc>
        ///    <para>Gets the help topic to display for the dialog help button or pressing F1. Override to
        ///          display a different help topic.</para> 
        /// </devdoc>
        protected override string HelpTopic 
        { 
            get
            { 
                return "net.ComponentModel.TreeNodeCollectionEditor";
            }
        }
 
        private class TreeNodeCollectionForm : CollectionForm
        { 
            private int nextNode = 0; 
            private TreeNode curNode;
            private TreeNodeCollectionEditor editor = null; 
            private System.Windows.Forms.Button okButton;
            private System.Windows.Forms.Button btnCancel;
            private System.Windows.Forms.Button btnAddChild;
            private System.Windows.Forms.Button btnAddRoot; 
            private System.Windows.Forms.Button btnDelete;
            private Button moveDownButton; 
            private Button moveUpButton; 
            private System.Windows.Forms.Label label1;
            private System.Windows.Forms.TreeView treeView1; 
            private System.Windows.Forms.Label label2;
            private VsPropertyGrid propertyGrid1;
            private System.Windows.Forms.TableLayoutPanel okCancelPanel;
            private System.Windows.Forms.TableLayoutPanel nodeControlPanel; 
            private System.Windows.Forms.TableLayoutPanel overarchingTableLayoutPanel;
            private System.Windows.Forms.TableLayoutPanel navigationButtonsTableLayoutPanel; 
 

            private static object NextNodeKey = new object(); 
            private int intialNextNode = 0;

            public TreeNodeCollectionForm(CollectionEditor editor)
                : base(editor) 
            {
                this.editor = (TreeNodeCollectionEditor)editor; 
                InitializeComponent(); 
                HookEvents();
                // cache in the initial value before add so that we can put this value back 
                // if the operation is cancelled.
                intialNextNode = NextNode;
                SetButtonsState();
 
            }
 
            private TreeNode LastNode 
            {
                get 
                {
                    // Big-O of this loop == #levels in the tree.
                    TreeNode lastNode = treeView1.Nodes[treeView1.Nodes.Count - 1];
                    while (lastNode.Nodes.Count > 0) 
                    {
                        lastNode = lastNode.Nodes[lastNode.Nodes.Count - 1]; 
                    } 
                    return lastNode;
                } 
            }

            private TreeView TreeView
            { 
                get
                { 
                    if (Context != null && Context.Instance is TreeView) 
                    {
                        return (TreeView)Context.Instance; 
                    }
                    else
                    {
                        Debug.Assert(false, "TreeNodeCollectionEditor couldn't find the TreeView being designed"); 
                        return null;
                    } 
                } 
            }
 
            private int NextNode
            {
                get
                { 
                    if (TreeView != null && TreeView.Site != null)
                    { 
                        IDictionaryService ds = (IDictionaryService)TreeView.Site.GetService(typeof(IDictionaryService)); 
                        Debug.Assert(ds != null, "TreeNodeCollectionEditor relies on IDictionaryService, which is not available.");
                        if (ds != null) 
                        {
                            object dictionaryValue = ds.GetValue(NextNodeKey);
                            if (dictionaryValue != null)
                            { 
                                nextNode = (int)dictionaryValue;
                            } 
                            else 
                            {
                                nextNode = 0; 
                                ds.SetValue(NextNodeKey, 0);
                            }
                        }
                    } 
                    return nextNode;
                } 
                set 
                {
                    nextNode = value; 
                    if (TreeView != null && TreeView.Site != null)
                    {
                        IDictionaryService ds = (IDictionaryService)TreeView.Site.GetService(typeof(IDictionaryService));
                        Debug.Assert(ds != null, "TreeNodeCollectionEditor relies on IDictionaryService, which is not available."); 
                        if (ds != null)
                        { 
                            ds.SetValue(NextNodeKey, nextNode); 
                        }
                    } 
                }
            }

            private void Add(TreeNode parent) 
            {
 
                TreeNode newNode = null; 
                string baseNodeName = SR.GetString(SR.BaseNodeName);
 


                if (parent == null)
                { 
                    newNode = treeView1.Nodes.Add(baseNodeName + NextNode++.ToString(CultureInfo.InvariantCulture));
                    newNode.Name = newNode.Text; 
                } 
                else
                { 
                    newNode = parent.Nodes.Add(baseNodeName + NextNode++.ToString(CultureInfo.InvariantCulture));
                    newNode.Name = newNode.Text;

                    parent.Expand(); 
                }
 
                if (parent != null) 
                {
                    treeView1.SelectedNode = parent; 
                }
                else
                {
                    treeView1.SelectedNode = newNode; 
                    // we are adding a Root Node ... at Level 1
                    // so show the properties in the PropertyGrid 
                    SetNodeProps(newNode); 
                }
 


            }
 
            private void HookEvents()
            { 
                this.okButton.Click += new EventHandler(this.BtnOK_click); 
                this.btnCancel.Click += new EventHandler(this.BtnCancel_click);
                this.btnAddChild.Click += new EventHandler(this.BtnAddChild_click); 
                this.btnAddRoot.Click += new EventHandler(this.BtnAddRoot_click);
                this.btnDelete.Click += new EventHandler(this.BtnDelete_click);
                this.propertyGrid1.PropertyValueChanged += new PropertyValueChangedEventHandler(this.PropertyGrid_propertyValueChanged);
                this.treeView1.AfterSelect += new TreeViewEventHandler(this.treeView1_afterSelect); 
                this.treeView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.treeView1_DragEnter);
                this.treeView1.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.treeView1_ItemDrag); 
                this.treeView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.treeView1_DragDrop); 
                this.treeView1.DragOver += new System.Windows.Forms.DragEventHandler(this.treeView1_DragOver);
                this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.TreeNodeCollectionEditor_HelpButtonClicked); 
                this.moveDownButton.Click += new System.EventHandler(this.moveDownButton_Click);
                this.moveUpButton.Click += new System.EventHandler(this.moveUpButton_Click);
            }
 
            /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.TreeNodeCollectionForm.InitializeComponent"]/*' />
            /// <devdoc> 
            ///     NOTE: The following code is required by the form 
            ///     designer.  It can be modified using the form editor.  Do not
            ///     modify it using the code editor. 
            /// </devdoc>
            private void InitializeComponent()
            {
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TreeNodeCollectionEditor)); 
                this.okCancelPanel = new System.Windows.Forms.TableLayoutPanel();
                this.okButton = new System.Windows.Forms.Button(); 
                this.btnCancel = new System.Windows.Forms.Button(); 
                this.nodeControlPanel = new System.Windows.Forms.TableLayoutPanel();
                this.btnAddRoot = new System.Windows.Forms.Button(); 
                this.btnAddChild = new System.Windows.Forms.Button();
                this.btnDelete = new System.Windows.Forms.Button();
                this.moveDownButton = new System.Windows.Forms.Button();
                this.moveUpButton = new System.Windows.Forms.Button(); 
                this.propertyGrid1 = new VsPropertyGrid(Context /*IServiceProvider*/);
                this.label2 = new System.Windows.Forms.Label(); 
                this.treeView1 = new System.Windows.Forms.TreeView(); 
                this.label1 = new System.Windows.Forms.Label();
                this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
                this.navigationButtonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
                this.okCancelPanel.SuspendLayout();
                this.nodeControlPanel.SuspendLayout();
                this.overarchingTableLayoutPanel.SuspendLayout(); 
                this.navigationButtonsTableLayoutPanel.SuspendLayout();
                this.SuspendLayout(); 
                // 
                // okCancelPanel
                // 
                resources.ApplyResources(this.okCancelPanel, "okCancelPanel");
                this.okCancelPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.okCancelPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.okCancelPanel.Controls.Add(this.okButton, 0, 0); 
                this.okCancelPanel.Controls.Add(this.btnCancel, 1, 0);
                this.okCancelPanel.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0); 
                this.okCancelPanel.Name = "okCancelPanel"; 
                this.okCancelPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                // 
                // okButton
                //
                resources.ApplyResources(this.okButton, "okButton");
                this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
                this.okButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
                this.okButton.Name = "okButton"; 
                // 
                // btnCancel
                // 
                resources.ApplyResources(this.btnCancel, "btnCancel");
                this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
                this.btnCancel.Name = "btnCancel"; 
                //
                // nodeControlPanel 
                // 
                resources.ApplyResources(this.nodeControlPanel, "nodeControlPanel");
                this.nodeControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
                this.nodeControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.nodeControlPanel.Controls.Add(this.btnAddRoot, 0, 0);
                this.nodeControlPanel.Controls.Add(this.btnAddChild, 1, 0);
                this.nodeControlPanel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3); 
                this.nodeControlPanel.Name = "nodeControlPanel";
                this.nodeControlPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                // 
                // btnAddRoot
                // 
                resources.ApplyResources(this.btnAddRoot, "btnAddRoot");
                this.btnAddRoot.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
                this.btnAddRoot.Name = "btnAddRoot";
                // 
                // btnAddChild
                // 
                resources.ApplyResources(this.btnAddChild, "btnAddChild"); 
                this.btnAddChild.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
                this.btnAddChild.Name = "btnAddChild"; 
                //
                // btnDelete
                //
                resources.ApplyResources(this.btnDelete, "btnDelete"); 
                this.btnDelete.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
                this.btnDelete.Name = "btnDelete"; 
                // 
                // moveDownButton
                // 
                resources.ApplyResources(this.moveDownButton, "moveDownButton");
                this.moveDownButton.Margin = new System.Windows.Forms.Padding(0, 1, 0, 3);
                this.moveDownButton.Name = "moveDownButton";
                // 
                // moveUpButton
                // 
                resources.ApplyResources(this.moveUpButton, "moveUpButton"); 
                this.moveUpButton.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
                this.moveUpButton.Name = "moveUpButton"; 
                //
                // propertyGrid1
                //
                resources.ApplyResources(this.propertyGrid1, "propertyGrid1"); 
                this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
                this.propertyGrid1.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3); 
                this.propertyGrid1.Name = "propertyGrid1"; 
                this.overarchingTableLayoutPanel.SetRowSpan(this.propertyGrid1, 2);
                // 
                // label2
                //
                resources.ApplyResources(this.label2, "label2");
                this.label2.Margin = new System.Windows.Forms.Padding(3, 1, 0, 0); 
                this.label2.Name = "label2";
                // 
                // treeView1 
                //
                this.treeView1.AllowDrop = true; 
                resources.ApplyResources(this.treeView1, "treeView1");
                this.treeView1.HideSelection = false;
                this.treeView1.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
                this.treeView1.Name = "treeView1"; 
                //
                // label1 
                // 
                resources.ApplyResources(this.label1, "label1");
                this.label1.Margin = new System.Windows.Forms.Padding(0, 1, 3, 0); 
                this.label1.Name = "label1";
                //
                // overarchingTableLayoutPanel
                // 
                resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
                this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
                this.overarchingTableLayoutPanel.Controls.Add(this.navigationButtonsTableLayoutPanel, 1, 1); 
                this.overarchingTableLayoutPanel.Controls.Add(this.label2, 2, 0);
                this.overarchingTableLayoutPanel.Controls.Add(this.propertyGrid1, 2, 1);
                this.overarchingTableLayoutPanel.Controls.Add(this.treeView1, 0, 1);
                this.overarchingTableLayoutPanel.Controls.Add(this.label1, 0, 0); 
                this.overarchingTableLayoutPanel.Controls.Add(this.nodeControlPanel, 0, 2);
                this.overarchingTableLayoutPanel.Controls.Add(this.okCancelPanel, 2, 3); 
                this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"; 
                this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F)); 
                this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                //
                // navigationButtonsTableLayoutPanel 
                //
                resources.ApplyResources(this.navigationButtonsTableLayoutPanel, "navigationButtonsTableLayoutPanel"); 
                this.navigationButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.navigationButtonsTableLayoutPanel.Controls.Add(this.moveUpButton, 0, 0);
                this.navigationButtonsTableLayoutPanel.Controls.Add(this.btnDelete, 0, 2); 
                this.navigationButtonsTableLayoutPanel.Controls.Add(this.moveDownButton, 0, 1);
                this.navigationButtonsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 3, 18, 3);
                this.navigationButtonsTableLayoutPanel.Name = "navigationButtonsTableLayoutPanel";
                this.navigationButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.navigationButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.navigationButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                // 
                // TreeNodeCollectionEditor
                // 
                this.AcceptButton = this.okButton;
                resources.ApplyResources(this, "$this");
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.CancelButton = this.btnCancel; 
                this.Controls.Add(this.overarchingTableLayoutPanel);
                this.HelpButton = true; 
                this.MaximizeBox = false; 
                this.MinimizeBox = false;
                this.Name = "TreeNodeCollectionEditor"; 
                this.ShowIcon = false;
                this.ShowInTaskbar = false;
                this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
                this.okCancelPanel.ResumeLayout(false); 
                this.okCancelPanel.PerformLayout();
                this.nodeControlPanel.ResumeLayout(false); 
                this.nodeControlPanel.PerformLayout(); 
                this.overarchingTableLayoutPanel.ResumeLayout(false);
                this.overarchingTableLayoutPanel.PerformLayout(); 
                this.navigationButtonsTableLayoutPanel.ResumeLayout(false);
                this.ResumeLayout(false);
            }
 

            /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.TreeNodeCollectionForm.OnEditValueChanged"]/*' /> 
            /// <devdoc> 
            ///      This is called when the value property in the CollectionForm has changed.
            ///      In it you should update your user interface to reflect the current value. 
            /// </devdoc>
            protected override void OnEditValueChanged()
            {
 
                if (EditValue != null)
                { 
                    object[] items = Items; 

                    propertyGrid1.Site = new PropertyGridSite(Context, propertyGrid1); 

                    TreeNode[] nodes = new TreeNode[items.Length];

                    for (int i = 0; i < items.Length; i++) 
                    {
                        // We need to copy the nodes into our editor TreeView, not move them. 
                        // We overwrite the passed-in array with the new roots. 
                        //
                        nodes[i] = (TreeNode)((TreeNode)items[i]).Clone(); 
                    }

                    treeView1.Nodes.Clear();
                    treeView1.Nodes.AddRange(nodes); 

                    // Update current node related UI 
                    // 
                    curNode = null;
                    btnAddChild.Enabled = false; 
                    btnDelete.Enabled = false;

                    // The image list for the editor TreeView must be updated to be the same
                    // as the image list for the actual TreeView. 
                    //
                    TreeView actualTV = TreeView; 
                    if (actualTV != null) 
                    {
                        SetImageProps(actualTV); 
                    }

                    if (items.Length > 0 && nodes[0] != null)
                    { 
                        treeView1.SelectedNode = nodes[0];
                    } 
 

                } 
            }


            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.PropertyGrid_propertyValueChanged"]/*' /> 
            /// <devdoc>
            ///      When something in the properties window changes, we update pertinent text here. 
            /// </devdoc> 
            private void PropertyGrid_propertyValueChanged(object sender, PropertyValueChangedEventArgs e)
            { 
                //Update the string above the grid.
                label2.Text = SR.GetString(SR.CollectionEditorProperties, treeView1.SelectedNode.Text);
            }
 

            private void SetImageProps(TreeView actualTreeView) 
            { 

                if (actualTreeView.ImageList != null) 
                {

                    // Update the treeview image-related properties
                    // 
                    treeView1.ImageList = actualTreeView.ImageList;
                    treeView1.ImageIndex = actualTreeView.ImageIndex; 
                    treeView1.SelectedImageIndex = actualTreeView.SelectedImageIndex; 
                }
                else 
                {
                    // Update the treeview image-related properties
                    //
                    treeView1.ImageList = null; 
                    treeView1.ImageIndex = -1;
                    treeView1.SelectedImageIndex = -1; 
 
                }
 

                if (actualTreeView.StateImageList != null)
                {
                    treeView1.StateImageList = actualTreeView.StateImageList; 
                }
                else 
                { 
                    treeView1.StateImageList = null;
 
                }

                // also set the CheckBoxes from the actual TreeView
                treeView1.CheckBoxes = actualTreeView.CheckBoxes; 

            } 
 
            private void SetNodeProps(TreeNode node)
            { 
                if (node != null)
                {
                    label2.Text = SR.GetString(SR.CollectionEditorProperties, node.Name.ToString());
                } 
                // no node is selected ... revert back the Text of the label to Properties... VsWhidbey: 338248.
                else 
                { 
                    this.label2.Text = SR.GetString(SR.CollectionEditorPropertiesNone);
                } 
                propertyGrid1.SelectedObject = node;
            }

            private void treeView1_afterSelect(object sender, TreeViewEventArgs e) 
            {
                curNode = e.Node; 
                SetNodeProps(curNode); 
                SetButtonsState();
            } 

            private void treeView1_ItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e)
            {
                TreeNode item = (TreeNode)e.Item; 
                DoDragDrop(item, DragDropEffects.Move);
            } 
 

            private void treeView1_DragEnter(object sender, System.Windows.Forms.DragEventArgs e) 
            {
                if (e.Data.GetDataPresent(typeof(TreeNode)))
                {
                    e.Effect = DragDropEffects.Move; 
                }
                else 
                { 
                    e.Effect = DragDropEffects.None;
                } 

            }

            private void treeView1_DragDrop(System.Object sender, System.Windows.Forms.DragEventArgs e) 
            {
                TreeNode dragNode = (TreeNode)e.Data.GetData(typeof(TreeNode)); 
                Point position = new Point(0, 0); 
                position.X = e.X;
                position.Y = e.Y; 
                position = treeView1.PointToClient(position);
                TreeNode dropNode = this.treeView1.GetNodeAt(position);

 
                if (dragNode != dropNode)
                { 
                    // Remove this node after finding the new root 
                    // but before re-adding the node to the collection
                    this.treeView1.Nodes.Remove(dragNode); 

                    if (dropNode != null && !CheckParent(dropNode, dragNode)) //DROPPED ON LEVEL > 0
                    {
                        dropNode.Nodes.Add(dragNode); 

                    } 
                    else //DROPPED ON LEVEL 0 
                    {
                        this.treeView1.Nodes.Add(dragNode); 
                    }
                }
            }
 
            private bool CheckParent(TreeNode child, TreeNode parent)
            { 
                while (child != null) 
                {
                    if (parent == child.Parent) 
                    {
                        return true;
                    }
                    child = child.Parent; 
                }
                return false; 
            } 

            private void treeView1_DragOver(System.Object sender, System.Windows.Forms.DragEventArgs e) 
            {
                Point position = new Point(0, 0);
                position.X = e.X;
                position.Y = e.Y; 
                position = treeView1.PointToClient(position);
                TreeNode currentNode = this.treeView1.GetNodeAt(position); 
                this.treeView1.SelectedNode = currentNode; 

            } 

            private void BtnAddChild_click(object sender, EventArgs e)
            {
                Add(curNode); 
                SetButtonsState();
            } 
 
            private void BtnAddRoot_click(object sender, EventArgs e)
            { 
                Add(null);
                SetButtonsState();
            }
 
            private void BtnDelete_click(object sender, EventArgs e)
            { 
                curNode.Remove(); 
                if (treeView1.Nodes.Count == 0)
                { 
                    curNode = null;
                    SetNodeProps(null);
                }
                SetButtonsState(); 
            }
 
            private void BtnOK_click(object sender, EventArgs e) 
            {
                object[] values = new object[treeView1.Nodes.Count]; 
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = treeView1.Nodes[i].Clone();
                } 
                Items = values;
 
                //Now Treeview is not required .. Dispose it 
                this.treeView1.Dispose();
                this.treeView1 = null; 
            }

            private void moveDownButton_Click(object sender, EventArgs e)
            { 
                TreeNode tempNode = curNode;
                TreeNode parent = curNode.Parent; 
 
                if (parent == null)
                { 
                    treeView1.Nodes.RemoveAt(tempNode.Index);
                    treeView1.Nodes[tempNode.Index].Nodes.Insert(0, tempNode);
                }
                else 
                {
                    parent.Nodes.RemoveAt(tempNode.Index); 
                    if (tempNode.Index < parent.Nodes.Count) 
                    {
                        parent.Nodes[tempNode.Index].Nodes.Insert(0, tempNode); 
                    }
                    else
                    {
                        if (parent.Parent == null) 
                        {
                            treeView1.Nodes.Insert(parent.Index + 1, tempNode); 
                        } 
                        else
                        { 
                            parent.Parent.Nodes.Insert(parent.Index + 1, tempNode);
                        }
                    }
                } 
                treeView1.SelectedNode = tempNode;
                curNode = tempNode; 
 
            }
 
            private void moveUpButton_Click(object sender, EventArgs e)
            {
                TreeNode tempNode = curNode;
                TreeNode parent = curNode.Parent; 

                if (parent == null) 
                { 
                    treeView1.Nodes.RemoveAt(tempNode.Index);
                    treeView1.Nodes[tempNode.Index - 1].Nodes.Add(tempNode); 
                }
                else
                {
                    parent.Nodes.RemoveAt(tempNode.Index); 
                    if (tempNode.Index == 0)
                    { 
                        if (parent.Parent == null) 
                        {
                            treeView1.Nodes.Insert(parent.Index, tempNode); 
                        }
                        else
                        {
                            parent.Parent.Nodes.Insert(parent.Index, tempNode); 
                        }
                    } 
                    else 
                    {
                        parent.Nodes[tempNode.Index - 1].Nodes.Add(tempNode); 
                    }
                }
                treeView1.SelectedNode = tempNode;
                curNode = tempNode; 

            } 
 
            private void SetButtonsState()
            { 
                bool nodesExist = treeView1.Nodes.Count > 0;

                btnAddChild.Enabled = nodesExist;
                btnDelete.Enabled = nodesExist; 
                moveDownButton.Enabled = nodesExist && (curNode != LastNode || curNode.Level > 0) && curNode != treeView1.Nodes[treeView1.Nodes.Count - 1];
                moveUpButton.Enabled = nodesExist && curNode != treeView1.Nodes[0]; 
            } 

            private void TreeNodeCollectionEditor_HelpButtonClicked(object sender, CancelEventArgs e) 
            {
                e.Cancel = true;
                editor.ShowHelp();
            } 

            private void BtnCancel_click(object sender, EventArgs e) 
            { 
                if (NextNode != intialNextNode)
                { 
                    NextNode = intialNextNode;
                }
            }
        } 
    }
 
    internal class PropertyGridSite : ISite 
    {
 
        private IServiceProvider sp;
        private IComponent comp;
        private bool inGetService = false;
 
        public PropertyGridSite(IServiceProvider sp, IComponent comp)
        { 
            this.sp = sp; 
            this.comp = comp;
        } 

        /** The component sited by this component site. */
        /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Component"]/*' />
        /// <devdoc> 
        ///    <para>When implemented by a class, gets the component associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc> 
        public IComponent Component { get { return comp; } } 

        /** The container in which the component is sited. */ 
        /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Container"]/*' />
        /// <devdoc>
        /// <para>When implemented by a class, gets the container associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc> 
        public IContainer Container { get { return null; } }
 
        /** Indicates whether the component is in design mode. */ 
        /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.DesignMode"]/*' />
        /// <devdoc> 
        ///    <para>When implemented by a class, determines whether the component is in design mode.</para>
        /// </devdoc>
        public bool DesignMode { get { return false; } }
 
        /**
             * The name of the component. 
             */ 
        /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Name"]/*' />
        /// <devdoc> 
        ///    <para>When implemented by a class, gets or sets the name of
        ///       the component associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc>
        public String Name 
        {
            get { return null; } 
            set { } 
        }
 
        public object GetService(Type t)
        {
            if (!inGetService && sp != null)
            { 
                try
                { 
                    inGetService = true; 
                    return sp.GetService(t);
                } 
                finally
                {
                    inGetService = false;
                } 
            }
            return null; 
        } 

    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TreeNodeCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System;
    using System.Design; 
    using System.Collections;
    using Microsoft.Win32; 
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Windows.Forms;
    using System.Windows.Forms.Layout;
    using System.Globalization;
 
    /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor"]/*' />
    /// <devdoc> 
    ///      The TreeNodeCollectionEditor is a collection editor that is specifically 
    ///      designed to edit a TreeNodeCollection.
    /// </devdoc> 
    internal class TreeNodeCollectionEditor : CollectionEditor
    {

        public TreeNodeCollectionEditor() 
            : base(typeof(TreeNodeCollection))
        { 
        } 

        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.CreateCollectionForm"]/*' /> 
        /// <devdoc>
        ///      Creates a new form to show the current collection.  You may inherit
        ///      from CollectionForm to provide your own form.
        /// </devdoc> 
        protected override CollectionForm CreateCollectionForm()
        { 
            return new TreeNodeCollectionForm(this); 
        }
 
        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.HelpTopic"]/*' />
        /// <devdoc>
        ///    <para>Gets the help topic to display for the dialog help button or pressing F1. Override to
        ///          display a different help topic.</para> 
        /// </devdoc>
        protected override string HelpTopic 
        { 
            get
            { 
                return "net.ComponentModel.TreeNodeCollectionEditor";
            }
        }
 
        private class TreeNodeCollectionForm : CollectionForm
        { 
            private int nextNode = 0; 
            private TreeNode curNode;
            private TreeNodeCollectionEditor editor = null; 
            private System.Windows.Forms.Button okButton;
            private System.Windows.Forms.Button btnCancel;
            private System.Windows.Forms.Button btnAddChild;
            private System.Windows.Forms.Button btnAddRoot; 
            private System.Windows.Forms.Button btnDelete;
            private Button moveDownButton; 
            private Button moveUpButton; 
            private System.Windows.Forms.Label label1;
            private System.Windows.Forms.TreeView treeView1; 
            private System.Windows.Forms.Label label2;
            private VsPropertyGrid propertyGrid1;
            private System.Windows.Forms.TableLayoutPanel okCancelPanel;
            private System.Windows.Forms.TableLayoutPanel nodeControlPanel; 
            private System.Windows.Forms.TableLayoutPanel overarchingTableLayoutPanel;
            private System.Windows.Forms.TableLayoutPanel navigationButtonsTableLayoutPanel; 
 

            private static object NextNodeKey = new object(); 
            private int intialNextNode = 0;

            public TreeNodeCollectionForm(CollectionEditor editor)
                : base(editor) 
            {
                this.editor = (TreeNodeCollectionEditor)editor; 
                InitializeComponent(); 
                HookEvents();
                // cache in the initial value before add so that we can put this value back 
                // if the operation is cancelled.
                intialNextNode = NextNode;
                SetButtonsState();
 
            }
 
            private TreeNode LastNode 
            {
                get 
                {
                    // Big-O of this loop == #levels in the tree.
                    TreeNode lastNode = treeView1.Nodes[treeView1.Nodes.Count - 1];
                    while (lastNode.Nodes.Count > 0) 
                    {
                        lastNode = lastNode.Nodes[lastNode.Nodes.Count - 1]; 
                    } 
                    return lastNode;
                } 
            }

            private TreeView TreeView
            { 
                get
                { 
                    if (Context != null && Context.Instance is TreeView) 
                    {
                        return (TreeView)Context.Instance; 
                    }
                    else
                    {
                        Debug.Assert(false, "TreeNodeCollectionEditor couldn't find the TreeView being designed"); 
                        return null;
                    } 
                } 
            }
 
            private int NextNode
            {
                get
                { 
                    if (TreeView != null && TreeView.Site != null)
                    { 
                        IDictionaryService ds = (IDictionaryService)TreeView.Site.GetService(typeof(IDictionaryService)); 
                        Debug.Assert(ds != null, "TreeNodeCollectionEditor relies on IDictionaryService, which is not available.");
                        if (ds != null) 
                        {
                            object dictionaryValue = ds.GetValue(NextNodeKey);
                            if (dictionaryValue != null)
                            { 
                                nextNode = (int)dictionaryValue;
                            } 
                            else 
                            {
                                nextNode = 0; 
                                ds.SetValue(NextNodeKey, 0);
                            }
                        }
                    } 
                    return nextNode;
                } 
                set 
                {
                    nextNode = value; 
                    if (TreeView != null && TreeView.Site != null)
                    {
                        IDictionaryService ds = (IDictionaryService)TreeView.Site.GetService(typeof(IDictionaryService));
                        Debug.Assert(ds != null, "TreeNodeCollectionEditor relies on IDictionaryService, which is not available."); 
                        if (ds != null)
                        { 
                            ds.SetValue(NextNodeKey, nextNode); 
                        }
                    } 
                }
            }

            private void Add(TreeNode parent) 
            {
 
                TreeNode newNode = null; 
                string baseNodeName = SR.GetString(SR.BaseNodeName);
 


                if (parent == null)
                { 
                    newNode = treeView1.Nodes.Add(baseNodeName + NextNode++.ToString(CultureInfo.InvariantCulture));
                    newNode.Name = newNode.Text; 
                } 
                else
                { 
                    newNode = parent.Nodes.Add(baseNodeName + NextNode++.ToString(CultureInfo.InvariantCulture));
                    newNode.Name = newNode.Text;

                    parent.Expand(); 
                }
 
                if (parent != null) 
                {
                    treeView1.SelectedNode = parent; 
                }
                else
                {
                    treeView1.SelectedNode = newNode; 
                    // we are adding a Root Node ... at Level 1
                    // so show the properties in the PropertyGrid 
                    SetNodeProps(newNode); 
                }
 


            }
 
            private void HookEvents()
            { 
                this.okButton.Click += new EventHandler(this.BtnOK_click); 
                this.btnCancel.Click += new EventHandler(this.BtnCancel_click);
                this.btnAddChild.Click += new EventHandler(this.BtnAddChild_click); 
                this.btnAddRoot.Click += new EventHandler(this.BtnAddRoot_click);
                this.btnDelete.Click += new EventHandler(this.BtnDelete_click);
                this.propertyGrid1.PropertyValueChanged += new PropertyValueChangedEventHandler(this.PropertyGrid_propertyValueChanged);
                this.treeView1.AfterSelect += new TreeViewEventHandler(this.treeView1_afterSelect); 
                this.treeView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.treeView1_DragEnter);
                this.treeView1.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.treeView1_ItemDrag); 
                this.treeView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.treeView1_DragDrop); 
                this.treeView1.DragOver += new System.Windows.Forms.DragEventHandler(this.treeView1_DragOver);
                this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.TreeNodeCollectionEditor_HelpButtonClicked); 
                this.moveDownButton.Click += new System.EventHandler(this.moveDownButton_Click);
                this.moveUpButton.Click += new System.EventHandler(this.moveUpButton_Click);
            }
 
            /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.TreeNodeCollectionForm.InitializeComponent"]/*' />
            /// <devdoc> 
            ///     NOTE: The following code is required by the form 
            ///     designer.  It can be modified using the form editor.  Do not
            ///     modify it using the code editor. 
            /// </devdoc>
            private void InitializeComponent()
            {
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TreeNodeCollectionEditor)); 
                this.okCancelPanel = new System.Windows.Forms.TableLayoutPanel();
                this.okButton = new System.Windows.Forms.Button(); 
                this.btnCancel = new System.Windows.Forms.Button(); 
                this.nodeControlPanel = new System.Windows.Forms.TableLayoutPanel();
                this.btnAddRoot = new System.Windows.Forms.Button(); 
                this.btnAddChild = new System.Windows.Forms.Button();
                this.btnDelete = new System.Windows.Forms.Button();
                this.moveDownButton = new System.Windows.Forms.Button();
                this.moveUpButton = new System.Windows.Forms.Button(); 
                this.propertyGrid1 = new VsPropertyGrid(Context /*IServiceProvider*/);
                this.label2 = new System.Windows.Forms.Label(); 
                this.treeView1 = new System.Windows.Forms.TreeView(); 
                this.label1 = new System.Windows.Forms.Label();
                this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
                this.navigationButtonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
                this.okCancelPanel.SuspendLayout();
                this.nodeControlPanel.SuspendLayout();
                this.overarchingTableLayoutPanel.SuspendLayout(); 
                this.navigationButtonsTableLayoutPanel.SuspendLayout();
                this.SuspendLayout(); 
                // 
                // okCancelPanel
                // 
                resources.ApplyResources(this.okCancelPanel, "okCancelPanel");
                this.okCancelPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.okCancelPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.okCancelPanel.Controls.Add(this.okButton, 0, 0); 
                this.okCancelPanel.Controls.Add(this.btnCancel, 1, 0);
                this.okCancelPanel.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0); 
                this.okCancelPanel.Name = "okCancelPanel"; 
                this.okCancelPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                // 
                // okButton
                //
                resources.ApplyResources(this.okButton, "okButton");
                this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
                this.okButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
                this.okButton.Name = "okButton"; 
                // 
                // btnCancel
                // 
                resources.ApplyResources(this.btnCancel, "btnCancel");
                this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
                this.btnCancel.Name = "btnCancel"; 
                //
                // nodeControlPanel 
                // 
                resources.ApplyResources(this.nodeControlPanel, "nodeControlPanel");
                this.nodeControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
                this.nodeControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.nodeControlPanel.Controls.Add(this.btnAddRoot, 0, 0);
                this.nodeControlPanel.Controls.Add(this.btnAddChild, 1, 0);
                this.nodeControlPanel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3); 
                this.nodeControlPanel.Name = "nodeControlPanel";
                this.nodeControlPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                // 
                // btnAddRoot
                // 
                resources.ApplyResources(this.btnAddRoot, "btnAddRoot");
                this.btnAddRoot.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
                this.btnAddRoot.Name = "btnAddRoot";
                // 
                // btnAddChild
                // 
                resources.ApplyResources(this.btnAddChild, "btnAddChild"); 
                this.btnAddChild.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
                this.btnAddChild.Name = "btnAddChild"; 
                //
                // btnDelete
                //
                resources.ApplyResources(this.btnDelete, "btnDelete"); 
                this.btnDelete.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
                this.btnDelete.Name = "btnDelete"; 
                // 
                // moveDownButton
                // 
                resources.ApplyResources(this.moveDownButton, "moveDownButton");
                this.moveDownButton.Margin = new System.Windows.Forms.Padding(0, 1, 0, 3);
                this.moveDownButton.Name = "moveDownButton";
                // 
                // moveUpButton
                // 
                resources.ApplyResources(this.moveUpButton, "moveUpButton"); 
                this.moveUpButton.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
                this.moveUpButton.Name = "moveUpButton"; 
                //
                // propertyGrid1
                //
                resources.ApplyResources(this.propertyGrid1, "propertyGrid1"); 
                this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
                this.propertyGrid1.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3); 
                this.propertyGrid1.Name = "propertyGrid1"; 
                this.overarchingTableLayoutPanel.SetRowSpan(this.propertyGrid1, 2);
                // 
                // label2
                //
                resources.ApplyResources(this.label2, "label2");
                this.label2.Margin = new System.Windows.Forms.Padding(3, 1, 0, 0); 
                this.label2.Name = "label2";
                // 
                // treeView1 
                //
                this.treeView1.AllowDrop = true; 
                resources.ApplyResources(this.treeView1, "treeView1");
                this.treeView1.HideSelection = false;
                this.treeView1.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
                this.treeView1.Name = "treeView1"; 
                //
                // label1 
                // 
                resources.ApplyResources(this.label1, "label1");
                this.label1.Margin = new System.Windows.Forms.Padding(0, 1, 3, 0); 
                this.label1.Name = "label1";
                //
                // overarchingTableLayoutPanel
                // 
                resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
                this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
                this.overarchingTableLayoutPanel.Controls.Add(this.navigationButtonsTableLayoutPanel, 1, 1); 
                this.overarchingTableLayoutPanel.Controls.Add(this.label2, 2, 0);
                this.overarchingTableLayoutPanel.Controls.Add(this.propertyGrid1, 2, 1);
                this.overarchingTableLayoutPanel.Controls.Add(this.treeView1, 0, 1);
                this.overarchingTableLayoutPanel.Controls.Add(this.label1, 0, 0); 
                this.overarchingTableLayoutPanel.Controls.Add(this.nodeControlPanel, 0, 2);
                this.overarchingTableLayoutPanel.Controls.Add(this.okCancelPanel, 2, 3); 
                this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"; 
                this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F)); 
                this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                //
                // navigationButtonsTableLayoutPanel 
                //
                resources.ApplyResources(this.navigationButtonsTableLayoutPanel, "navigationButtonsTableLayoutPanel"); 
                this.navigationButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.navigationButtonsTableLayoutPanel.Controls.Add(this.moveUpButton, 0, 0);
                this.navigationButtonsTableLayoutPanel.Controls.Add(this.btnDelete, 0, 2); 
                this.navigationButtonsTableLayoutPanel.Controls.Add(this.moveDownButton, 0, 1);
                this.navigationButtonsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 3, 18, 3);
                this.navigationButtonsTableLayoutPanel.Name = "navigationButtonsTableLayoutPanel";
                this.navigationButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.navigationButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.navigationButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                // 
                // TreeNodeCollectionEditor
                // 
                this.AcceptButton = this.okButton;
                resources.ApplyResources(this, "$this");
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.CancelButton = this.btnCancel; 
                this.Controls.Add(this.overarchingTableLayoutPanel);
                this.HelpButton = true; 
                this.MaximizeBox = false; 
                this.MinimizeBox = false;
                this.Name = "TreeNodeCollectionEditor"; 
                this.ShowIcon = false;
                this.ShowInTaskbar = false;
                this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
                this.okCancelPanel.ResumeLayout(false); 
                this.okCancelPanel.PerformLayout();
                this.nodeControlPanel.ResumeLayout(false); 
                this.nodeControlPanel.PerformLayout(); 
                this.overarchingTableLayoutPanel.ResumeLayout(false);
                this.overarchingTableLayoutPanel.PerformLayout(); 
                this.navigationButtonsTableLayoutPanel.ResumeLayout(false);
                this.ResumeLayout(false);
            }
 

            /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.TreeNodeCollectionForm.OnEditValueChanged"]/*' /> 
            /// <devdoc> 
            ///      This is called when the value property in the CollectionForm has changed.
            ///      In it you should update your user interface to reflect the current value. 
            /// </devdoc>
            protected override void OnEditValueChanged()
            {
 
                if (EditValue != null)
                { 
                    object[] items = Items; 

                    propertyGrid1.Site = new PropertyGridSite(Context, propertyGrid1); 

                    TreeNode[] nodes = new TreeNode[items.Length];

                    for (int i = 0; i < items.Length; i++) 
                    {
                        // We need to copy the nodes into our editor TreeView, not move them. 
                        // We overwrite the passed-in array with the new roots. 
                        //
                        nodes[i] = (TreeNode)((TreeNode)items[i]).Clone(); 
                    }

                    treeView1.Nodes.Clear();
                    treeView1.Nodes.AddRange(nodes); 

                    // Update current node related UI 
                    // 
                    curNode = null;
                    btnAddChild.Enabled = false; 
                    btnDelete.Enabled = false;

                    // The image list for the editor TreeView must be updated to be the same
                    // as the image list for the actual TreeView. 
                    //
                    TreeView actualTV = TreeView; 
                    if (actualTV != null) 
                    {
                        SetImageProps(actualTV); 
                    }

                    if (items.Length > 0 && nodes[0] != null)
                    { 
                        treeView1.SelectedNode = nodes[0];
                    } 
 

                } 
            }


            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.PropertyGrid_propertyValueChanged"]/*' /> 
            /// <devdoc>
            ///      When something in the properties window changes, we update pertinent text here. 
            /// </devdoc> 
            private void PropertyGrid_propertyValueChanged(object sender, PropertyValueChangedEventArgs e)
            { 
                //Update the string above the grid.
                label2.Text = SR.GetString(SR.CollectionEditorProperties, treeView1.SelectedNode.Text);
            }
 

            private void SetImageProps(TreeView actualTreeView) 
            { 

                if (actualTreeView.ImageList != null) 
                {

                    // Update the treeview image-related properties
                    // 
                    treeView1.ImageList = actualTreeView.ImageList;
                    treeView1.ImageIndex = actualTreeView.ImageIndex; 
                    treeView1.SelectedImageIndex = actualTreeView.SelectedImageIndex; 
                }
                else 
                {
                    // Update the treeview image-related properties
                    //
                    treeView1.ImageList = null; 
                    treeView1.ImageIndex = -1;
                    treeView1.SelectedImageIndex = -1; 
 
                }
 

                if (actualTreeView.StateImageList != null)
                {
                    treeView1.StateImageList = actualTreeView.StateImageList; 
                }
                else 
                { 
                    treeView1.StateImageList = null;
 
                }

                // also set the CheckBoxes from the actual TreeView
                treeView1.CheckBoxes = actualTreeView.CheckBoxes; 

            } 
 
            private void SetNodeProps(TreeNode node)
            { 
                if (node != null)
                {
                    label2.Text = SR.GetString(SR.CollectionEditorProperties, node.Name.ToString());
                } 
                // no node is selected ... revert back the Text of the label to Properties... VsWhidbey: 338248.
                else 
                { 
                    this.label2.Text = SR.GetString(SR.CollectionEditorPropertiesNone);
                } 
                propertyGrid1.SelectedObject = node;
            }

            private void treeView1_afterSelect(object sender, TreeViewEventArgs e) 
            {
                curNode = e.Node; 
                SetNodeProps(curNode); 
                SetButtonsState();
            } 

            private void treeView1_ItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e)
            {
                TreeNode item = (TreeNode)e.Item; 
                DoDragDrop(item, DragDropEffects.Move);
            } 
 

            private void treeView1_DragEnter(object sender, System.Windows.Forms.DragEventArgs e) 
            {
                if (e.Data.GetDataPresent(typeof(TreeNode)))
                {
                    e.Effect = DragDropEffects.Move; 
                }
                else 
                { 
                    e.Effect = DragDropEffects.None;
                } 

            }

            private void treeView1_DragDrop(System.Object sender, System.Windows.Forms.DragEventArgs e) 
            {
                TreeNode dragNode = (TreeNode)e.Data.GetData(typeof(TreeNode)); 
                Point position = new Point(0, 0); 
                position.X = e.X;
                position.Y = e.Y; 
                position = treeView1.PointToClient(position);
                TreeNode dropNode = this.treeView1.GetNodeAt(position);

 
                if (dragNode != dropNode)
                { 
                    // Remove this node after finding the new root 
                    // but before re-adding the node to the collection
                    this.treeView1.Nodes.Remove(dragNode); 

                    if (dropNode != null && !CheckParent(dropNode, dragNode)) //DROPPED ON LEVEL > 0
                    {
                        dropNode.Nodes.Add(dragNode); 

                    } 
                    else //DROPPED ON LEVEL 0 
                    {
                        this.treeView1.Nodes.Add(dragNode); 
                    }
                }
            }
 
            private bool CheckParent(TreeNode child, TreeNode parent)
            { 
                while (child != null) 
                {
                    if (parent == child.Parent) 
                    {
                        return true;
                    }
                    child = child.Parent; 
                }
                return false; 
            } 

            private void treeView1_DragOver(System.Object sender, System.Windows.Forms.DragEventArgs e) 
            {
                Point position = new Point(0, 0);
                position.X = e.X;
                position.Y = e.Y; 
                position = treeView1.PointToClient(position);
                TreeNode currentNode = this.treeView1.GetNodeAt(position); 
                this.treeView1.SelectedNode = currentNode; 

            } 

            private void BtnAddChild_click(object sender, EventArgs e)
            {
                Add(curNode); 
                SetButtonsState();
            } 
 
            private void BtnAddRoot_click(object sender, EventArgs e)
            { 
                Add(null);
                SetButtonsState();
            }
 
            private void BtnDelete_click(object sender, EventArgs e)
            { 
                curNode.Remove(); 
                if (treeView1.Nodes.Count == 0)
                { 
                    curNode = null;
                    SetNodeProps(null);
                }
                SetButtonsState(); 
            }
 
            private void BtnOK_click(object sender, EventArgs e) 
            {
                object[] values = new object[treeView1.Nodes.Count]; 
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = treeView1.Nodes[i].Clone();
                } 
                Items = values;
 
                //Now Treeview is not required .. Dispose it 
                this.treeView1.Dispose();
                this.treeView1 = null; 
            }

            private void moveDownButton_Click(object sender, EventArgs e)
            { 
                TreeNode tempNode = curNode;
                TreeNode parent = curNode.Parent; 
 
                if (parent == null)
                { 
                    treeView1.Nodes.RemoveAt(tempNode.Index);
                    treeView1.Nodes[tempNode.Index].Nodes.Insert(0, tempNode);
                }
                else 
                {
                    parent.Nodes.RemoveAt(tempNode.Index); 
                    if (tempNode.Index < parent.Nodes.Count) 
                    {
                        parent.Nodes[tempNode.Index].Nodes.Insert(0, tempNode); 
                    }
                    else
                    {
                        if (parent.Parent == null) 
                        {
                            treeView1.Nodes.Insert(parent.Index + 1, tempNode); 
                        } 
                        else
                        { 
                            parent.Parent.Nodes.Insert(parent.Index + 1, tempNode);
                        }
                    }
                } 
                treeView1.SelectedNode = tempNode;
                curNode = tempNode; 
 
            }
 
            private void moveUpButton_Click(object sender, EventArgs e)
            {
                TreeNode tempNode = curNode;
                TreeNode parent = curNode.Parent; 

                if (parent == null) 
                { 
                    treeView1.Nodes.RemoveAt(tempNode.Index);
                    treeView1.Nodes[tempNode.Index - 1].Nodes.Add(tempNode); 
                }
                else
                {
                    parent.Nodes.RemoveAt(tempNode.Index); 
                    if (tempNode.Index == 0)
                    { 
                        if (parent.Parent == null) 
                        {
                            treeView1.Nodes.Insert(parent.Index, tempNode); 
                        }
                        else
                        {
                            parent.Parent.Nodes.Insert(parent.Index, tempNode); 
                        }
                    } 
                    else 
                    {
                        parent.Nodes[tempNode.Index - 1].Nodes.Add(tempNode); 
                    }
                }
                treeView1.SelectedNode = tempNode;
                curNode = tempNode; 

            } 
 
            private void SetButtonsState()
            { 
                bool nodesExist = treeView1.Nodes.Count > 0;

                btnAddChild.Enabled = nodesExist;
                btnDelete.Enabled = nodesExist; 
                moveDownButton.Enabled = nodesExist && (curNode != LastNode || curNode.Level > 0) && curNode != treeView1.Nodes[treeView1.Nodes.Count - 1];
                moveUpButton.Enabled = nodesExist && curNode != treeView1.Nodes[0]; 
            } 

            private void TreeNodeCollectionEditor_HelpButtonClicked(object sender, CancelEventArgs e) 
            {
                e.Cancel = true;
                editor.ShowHelp();
            } 

            private void BtnCancel_click(object sender, EventArgs e) 
            { 
                if (NextNode != intialNextNode)
                { 
                    NextNode = intialNextNode;
                }
            }
        } 
    }
 
    internal class PropertyGridSite : ISite 
    {
 
        private IServiceProvider sp;
        private IComponent comp;
        private bool inGetService = false;
 
        public PropertyGridSite(IServiceProvider sp, IComponent comp)
        { 
            this.sp = sp; 
            this.comp = comp;
        } 

        /** The component sited by this component site. */
        /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Component"]/*' />
        /// <devdoc> 
        ///    <para>When implemented by a class, gets the component associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc> 
        public IComponent Component { get { return comp; } } 

        /** The container in which the component is sited. */ 
        /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Container"]/*' />
        /// <devdoc>
        /// <para>When implemented by a class, gets the container associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc> 
        public IContainer Container { get { return null; } }
 
        /** Indicates whether the component is in design mode. */ 
        /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.DesignMode"]/*' />
        /// <devdoc> 
        ///    <para>When implemented by a class, determines whether the component is in design mode.</para>
        /// </devdoc>
        public bool DesignMode { get { return false; } }
 
        /**
             * The name of the component. 
             */ 
        /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Name"]/*' />
        /// <devdoc> 
        ///    <para>When implemented by a class, gets or sets the name of
        ///       the component associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc>
        public String Name 
        {
            get { return null; } 
            set { } 
        }
 
        public object GetService(Type t)
        {
            if (!inGetService && sp != null)
            { 
                try
                { 
                    inGetService = true; 
                    return sp.GetService(t);
                } 
                finally
                {
                    inGetService = false;
                } 
            }
            return null; 
        } 

    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
