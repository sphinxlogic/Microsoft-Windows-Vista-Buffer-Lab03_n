//------------------------------------------------------------------------------ 
/// <copyright file="CollectionEditorDialog.cs" company="Microsoft Corporation">
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
using System.Web.UI.Design.Util; 
using System.Windows.Forms;
 
namespace System.Web.UI.Design.WebControls {

    // NOTE: For now the sole purpose of this internal class is created for
    // having a helper method used by both MenuItemCollectionEditorDialog and 
    // TreeNodeCollectionEditorDialog.  It can be extended to further refactor
    // the code between these two classes to avoid duplicate code. 
    internal abstract class CollectionEditorDialog : DesignerForm { 

        protected CollectionEditorDialog(IServiceProvider serviceProvider) : base(serviceProvider) { 
        }

        // VSWhidbey 504754: Minic the code from WinForms PropertyGrid to set
        // the properties on the push button accordingly. 
        protected ToolStripButton CreatePushButton(string toolTipText, int imageIndex) {
            // A note is that we could set the property AccessibleDescription on 
            // the button for accessibility.  However, since the string value for 
            // the Text property is already descriptive enough in our current
            // case, it will be used by the screen reader automatically when 
            // AccessibleDescription is not set.

            ToolStripButton button = new ToolStripButton();
            button.Text = toolTipText; 
            button.AutoToolTip = true;
            button.DisplayStyle = ToolStripItemDisplayStyle.Image; 
            button.ImageIndex = imageIndex; 
            button.ImageScaling = ToolStripItemImageScaling.SizeToFit;
            return button; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
/// <copyright file="CollectionEditorDialog.cs" company="Microsoft Corporation">
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
using System.Web.UI.Design.Util; 
using System.Windows.Forms;
 
namespace System.Web.UI.Design.WebControls {

    // NOTE: For now the sole purpose of this internal class is created for
    // having a helper method used by both MenuItemCollectionEditorDialog and 
    // TreeNodeCollectionEditorDialog.  It can be extended to further refactor
    // the code between these two classes to avoid duplicate code. 
    internal abstract class CollectionEditorDialog : DesignerForm { 

        protected CollectionEditorDialog(IServiceProvider serviceProvider) : base(serviceProvider) { 
        }

        // VSWhidbey 504754: Minic the code from WinForms PropertyGrid to set
        // the properties on the push button accordingly. 
        protected ToolStripButton CreatePushButton(string toolTipText, int imageIndex) {
            // A note is that we could set the property AccessibleDescription on 
            // the button for accessibility.  However, since the string value for 
            // the Text property is already descriptive enough in our current
            // case, it will be used by the screen reader automatically when 
            // AccessibleDescription is not set.

            ToolStripButton button = new ToolStripButton();
            button.Text = toolTipText; 
            button.AutoToolTip = true;
            button.DisplayStyle = ToolStripItemDisplayStyle.Image; 
            button.ImageIndex = imageIndex; 
            button.ImageScaling = ToolStripItemImageScaling.SizeToFit;
            return button; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
