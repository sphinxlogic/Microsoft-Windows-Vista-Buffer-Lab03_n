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

    public class MenuBindingsEditor : UITypeEditor { 
 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            IDesignerHost designerHost = (IDesignerHost)context.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Didn't get a DesignerHost.");

            Debug.Assert(context.Instance is WebMenu, "Expected System.Web.UI.WebControls.Menu");
            WebMenu menu = (WebMenu)context.Instance; 

            MenuDesigner designer = (MenuDesigner)designerHost.GetDesigner(menu); 
            Debug.Assert(designer != null, "Didn't get a designer."); 

            designer.InvokeMenuBindingsEditor(); 
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

    public class MenuBindingsEditor : UITypeEditor { 
 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            IDesignerHost designerHost = (IDesignerHost)context.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Didn't get a DesignerHost.");

            Debug.Assert(context.Instance is WebMenu, "Expected System.Web.UI.WebControls.Menu");
            WebMenu menu = (WebMenu)context.Instance; 

            MenuDesigner designer = (MenuDesigner)designerHost.GetDesigner(menu); 
            Debug.Assert(designer != null, "Didn't get a designer."); 

            designer.InvokeMenuBindingsEditor(); 
            return value;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
