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
 
    /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor"]/*' />
    /// <devdoc> 
    /// The editor for tree nodes collection in the TreeView.
    /// </devdoc>
    public class TreeNodeCollectionEditor : UITypeEditor {
 
        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.EditValue"]/*' />
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            IDesignerHost designerHost = (IDesignerHost)context.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Didn't get a DesignerHost.");
 
            Debug.Assert(context.Instance is WebTreeView, "Expected System.Web.UI.WebControls.TreeView");
            WebTreeView treeView = (WebTreeView)context.Instance;

            TreeViewDesigner designer = (TreeViewDesigner)designerHost.GetDesigner(treeView); 
            Debug.Assert(designer != null, "Didn't get a designer.");
 
            designer.InvokeTreeNodeCollectionEditor(); 
            return value;
        } 

        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.GetEditStyle"]/*' />
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal; 
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
 
    /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor"]/*' />
    /// <devdoc> 
    /// The editor for tree nodes collection in the TreeView.
    /// </devdoc>
    public class TreeNodeCollectionEditor : UITypeEditor {
 
        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.EditValue"]/*' />
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            IDesignerHost designerHost = (IDesignerHost)context.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Didn't get a DesignerHost.");
 
            Debug.Assert(context.Instance is WebTreeView, "Expected System.Web.UI.WebControls.TreeView");
            WebTreeView treeView = (WebTreeView)context.Instance;

            TreeViewDesigner designer = (TreeViewDesigner)designerHost.GetDesigner(treeView); 
            Debug.Assert(designer != null, "Didn't get a designer.");
 
            designer.InvokeTreeNodeCollectionEditor(); 
            return value;
        } 

        /// <include file='doc\TreeNodeCollectionEditor.uex' path='docs/doc[@for="TreeNodeCollectionEditor.GetEditStyle"]/*' />
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
