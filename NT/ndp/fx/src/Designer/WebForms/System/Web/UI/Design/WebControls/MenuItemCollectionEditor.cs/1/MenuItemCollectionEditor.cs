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

    using WebMenu = System.Web.UI.WebControls.Menu; 

    /// <devdoc> 
    /// The editor for tree nodes collection in the Menu. 
    /// </devdoc>
    public class MenuItemCollectionEditor : UITypeEditor { 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            IDesignerHost designerHost = (IDesignerHost)context.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Didn't get a DesignerHost."); 

            Debug.Assert(context.Instance is WebMenu, "Expected System.Web.UI.WebControls.Menu"); 
            WebMenu menu = (WebMenu)context.Instance; 

            MenuDesigner designer = (MenuDesigner)designerHost.GetDesigner(menu); 
            Debug.Assert(designer != null, "Didn't get a designer.");

            designer.InvokeMenuItemCollectionEditor();
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

    using WebMenu = System.Web.UI.WebControls.Menu; 

    /// <devdoc> 
    /// The editor for tree nodes collection in the Menu. 
    /// </devdoc>
    public class MenuItemCollectionEditor : UITypeEditor { 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            IDesignerHost designerHost = (IDesignerHost)context.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Didn't get a DesignerHost."); 

            Debug.Assert(context.Instance is WebMenu, "Expected System.Web.UI.WebControls.Menu"); 
            WebMenu menu = (WebMenu)context.Instance; 

            MenuDesigner designer = (MenuDesigner)designerHost.GetDesigner(menu); 
            Debug.Assert(designer != null, "Didn't get a designer.");

            designer.InvokeMenuItemCollectionEditor();
            return value; 
        }
 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
