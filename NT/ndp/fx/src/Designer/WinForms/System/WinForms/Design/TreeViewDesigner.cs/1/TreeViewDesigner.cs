//------------------------------------------------------------------------------ 
// <copyright file="TreeViewDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TreeViewDesigner..ctor()")] 
namespace System.Windows.Forms.Design {


    using System.Diagnostics; 

    using System; 
    using System.Design; 
    using System.Drawing;
    using System.Windows.Forms; 
    using Microsoft.Win32;
 	using System.ComponentModel.Design;
	using System.ComponentModel;
 
    /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner"]/*' />
    /// <devdoc> 
    ///      This is the designer for tree view controls.  It inherits 
    ///      from the base control designer and adds live hit testing
    ///      capabilites for the tree view control. 
    /// </devdoc>
    internal class TreeViewDesigner : ControlDesigner {
        private NativeMethods.TV_HITTESTINFO tvhit = new NativeMethods.TV_HITTESTINFO();
        private DesignerActionListCollection _actionLists; 
        private TreeView treeView = null;
 
        public TreeViewDesigner() { 
            AutoResizeHandles = true;
        } 


        /// <devdoc>
        ///      Disposes of this object. 
        /// </devdoc>
        protected override void Dispose(bool disposing) { 
            if (disposing) { 
               if (treeView != null)
                { 
                    treeView.AfterExpand -= new System.Windows.Forms.TreeViewEventHandler(TreeViewInvalidate);
                    treeView.AfterCollapse -= new System.Windows.Forms.TreeViewEventHandler(TreeViewInvalidate);
                    treeView = null;
                } 
            }
 
            base.Dispose(disposing); 
        }
 
        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetHitTest"]/*' />
        /// <devdoc>
        ///    <para>Allows your component to support a design time user interface. A TabStrip
        ///       control, for example, has a design time user interface that allows the user 
        ///       to click the tabs to change tabs. To implement this, TabStrip returns
        ///       true whenever the given point is within its tabs.</para> 
        /// </devdoc> 
        protected override bool GetHitTest(Point point) {
            point = Control.PointToClient(point); 
            tvhit.pt_x = point.X;
            tvhit.pt_y = point.Y;
            NativeMethods.SendMessage(Control.Handle, NativeMethods.TVM_HITTEST, 0, tvhit);
            if (tvhit.flags == NativeMethods.TVHT_ONITEMBUTTON) 
                return true;
            return false; 
        } 

        public override void Initialize(IComponent component) 
        {
            base.Initialize(component);
            treeView = component as TreeView;
            Debug.Assert(treeView != null, "TreeView is null in TreeViewDesigner"); 
            if (treeView != null)
            { 
                treeView.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(TreeViewInvalidate); 
                treeView.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(TreeViewInvalidate);
            } 
        }

        private void TreeViewInvalidate(object sender, TreeViewEventArgs e)
        { 
            if (treeView != null)
            { 
                treeView.Invalidate(); 
            }
        } 

        public override DesignerActionListCollection ActionLists {
            get {
                if (_actionLists == null) { 
                    _actionLists = new DesignerActionListCollection();
                    _actionLists.Add(new TreeViewActionList(this)); 
                } 
                return _actionLists;
            } 
        }
    }

    internal class TreeViewActionList : DesignerActionList { 
        private TreeViewDesigner _designer;
        public TreeViewActionList(TreeViewDesigner designer) : base(designer.Component)   { 
            _designer = designer; 
        }
 
        public void InvokeNodesDialog() {
            EditorServiceContext.EditValue(_designer, Component, "Nodes");
        }
 
        public ImageList ImageList {
            get { 
                return ((TreeView)Component).ImageList; 
            }
            set { 
                TypeDescriptor.GetProperties(Component)["ImageList"].SetValue(Component, value);
            }
        }
 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            items.Add(new DesignerActionMethodItem(this, "InvokeNodesDialog", SR.GetString(SR.InvokeNodesDialogDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.InvokeNodesDialogDescription), true)); 
            items.Add(new DesignerActionPropertyItem("ImageList", SR.GetString(SR.ImageListDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.ImageListDescription)));
            return items; 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TreeViewDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TreeViewDesigner..ctor()")] 
namespace System.Windows.Forms.Design {


    using System.Diagnostics; 

    using System; 
    using System.Design; 
    using System.Drawing;
    using System.Windows.Forms; 
    using Microsoft.Win32;
 	using System.ComponentModel.Design;
	using System.ComponentModel;
 
    /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner"]/*' />
    /// <devdoc> 
    ///      This is the designer for tree view controls.  It inherits 
    ///      from the base control designer and adds live hit testing
    ///      capabilites for the tree view control. 
    /// </devdoc>
    internal class TreeViewDesigner : ControlDesigner {
        private NativeMethods.TV_HITTESTINFO tvhit = new NativeMethods.TV_HITTESTINFO();
        private DesignerActionListCollection _actionLists; 
        private TreeView treeView = null;
 
        public TreeViewDesigner() { 
            AutoResizeHandles = true;
        } 


        /// <devdoc>
        ///      Disposes of this object. 
        /// </devdoc>
        protected override void Dispose(bool disposing) { 
            if (disposing) { 
               if (treeView != null)
                { 
                    treeView.AfterExpand -= new System.Windows.Forms.TreeViewEventHandler(TreeViewInvalidate);
                    treeView.AfterCollapse -= new System.Windows.Forms.TreeViewEventHandler(TreeViewInvalidate);
                    treeView = null;
                } 
            }
 
            base.Dispose(disposing); 
        }
 
        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetHitTest"]/*' />
        /// <devdoc>
        ///    <para>Allows your component to support a design time user interface. A TabStrip
        ///       control, for example, has a design time user interface that allows the user 
        ///       to click the tabs to change tabs. To implement this, TabStrip returns
        ///       true whenever the given point is within its tabs.</para> 
        /// </devdoc> 
        protected override bool GetHitTest(Point point) {
            point = Control.PointToClient(point); 
            tvhit.pt_x = point.X;
            tvhit.pt_y = point.Y;
            NativeMethods.SendMessage(Control.Handle, NativeMethods.TVM_HITTEST, 0, tvhit);
            if (tvhit.flags == NativeMethods.TVHT_ONITEMBUTTON) 
                return true;
            return false; 
        } 

        public override void Initialize(IComponent component) 
        {
            base.Initialize(component);
            treeView = component as TreeView;
            Debug.Assert(treeView != null, "TreeView is null in TreeViewDesigner"); 
            if (treeView != null)
            { 
                treeView.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(TreeViewInvalidate); 
                treeView.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(TreeViewInvalidate);
            } 
        }

        private void TreeViewInvalidate(object sender, TreeViewEventArgs e)
        { 
            if (treeView != null)
            { 
                treeView.Invalidate(); 
            }
        } 

        public override DesignerActionListCollection ActionLists {
            get {
                if (_actionLists == null) { 
                    _actionLists = new DesignerActionListCollection();
                    _actionLists.Add(new TreeViewActionList(this)); 
                } 
                return _actionLists;
            } 
        }
    }

    internal class TreeViewActionList : DesignerActionList { 
        private TreeViewDesigner _designer;
        public TreeViewActionList(TreeViewDesigner designer) : base(designer.Component)   { 
            _designer = designer; 
        }
 
        public void InvokeNodesDialog() {
            EditorServiceContext.EditValue(_designer, Component, "Nodes");
        }
 
        public ImageList ImageList {
            get { 
                return ((TreeView)Component).ImageList; 
            }
            set { 
                TypeDescriptor.GetProperties(Component)["ImageList"].SetValue(Component, value);
            }
        }
 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            items.Add(new DesignerActionMethodItem(this, "InvokeNodesDialog", SR.GetString(SR.InvokeNodesDialogDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.InvokeNodesDialogDescription), true)); 
            items.Add(new DesignerActionPropertyItem("ImageList", SR.GetString(SR.ImageListDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.ImageListDescription)));
            return items; 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
