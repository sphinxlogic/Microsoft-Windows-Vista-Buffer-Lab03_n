//------------------------------------------------------------------------------ 
// <copyright file="TreeViewBindingsEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics;
    using System.Drawing.Design;
    using System.Web.UI.WebControls; 

    /// <devdoc> 
    ///    The editor for tree bindings collection in the TreeView. 
    /// </devdoc>
    public class TreeViewBindingsEditor : UITypeEditor { 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            IDesignerHost designerHost = (IDesignerHost)context.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Didn't get a DesignerHost."); 

            Debug.Assert(context.Instance is TreeView, "Expected System.Web.UI.WebControls.TreeView"); 
            TreeView treeView = (TreeView)context.Instance; 

            TreeViewDesigner designer = (TreeViewDesigner)designerHost.GetDesigner(treeView); 
            Debug.Assert(designer != null, "Didn't get a designer.");

            designer.InvokeTreeViewBindingsEditor();
            return value; 
        }
 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TreeViewBindingsEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics;
    using System.Drawing.Design;
    using System.Web.UI.WebControls; 

    /// <devdoc> 
    ///    The editor for tree bindings collection in the TreeView. 
    /// </devdoc>
    public class TreeViewBindingsEditor : UITypeEditor { 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            IDesignerHost designerHost = (IDesignerHost)context.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Didn't get a DesignerHost."); 

            Debug.Assert(context.Instance is TreeView, "Expected System.Web.UI.WebControls.TreeView"); 
            TreeView treeView = (TreeView)context.Instance; 

            TreeViewDesigner designer = (TreeViewDesigner)designerHost.GetDesigner(treeView); 
            Debug.Assert(designer != null, "Didn't get a designer.");

            designer.InvokeTreeViewBindingsEditor();
            return value; 
        }
 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
