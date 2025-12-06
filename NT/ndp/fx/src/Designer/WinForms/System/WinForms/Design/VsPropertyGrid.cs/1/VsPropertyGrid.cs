//------------------------------------------------------------------------------ 
// <copyright file="VsPropertyGrid.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

 
namespace System.Windows.Forms.Design {

    using System;
    using System.Drawing; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Data; 
    using System.Drawing.Design;
    using System.Diagnostics;
    using System.Design;
    using System.Windows.Forms.Layout; 

    // Internal wrapper for the propertyGrids on the CollectionEditors so that they have the correct renderer to Render VS-Shell like ToolStrips. 
    internal class VsPropertyGrid : PropertyGrid 
    {
        public VsPropertyGrid(IServiceProvider serviceProvider) : base() 
        {
            if (serviceProvider != null)
            {
                IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService; 
                if (uis != null) {
                  this.ToolStripRenderer = (ToolStripProfessionalRenderer)uis.Styles["VsToolWindowRenderer"]; 
                } 
            }
        } 
    }
}

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="VsPropertyGrid.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

 
namespace System.Windows.Forms.Design {

    using System;
    using System.Drawing; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Data; 
    using System.Drawing.Design;
    using System.Diagnostics;
    using System.Design;
    using System.Windows.Forms.Layout; 

    // Internal wrapper for the propertyGrids on the CollectionEditors so that they have the correct renderer to Render VS-Shell like ToolStrips. 
    internal class VsPropertyGrid : PropertyGrid 
    {
        public VsPropertyGrid(IServiceProvider serviceProvider) : base() 
        {
            if (serviceProvider != null)
            {
                IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService; 
                if (uis != null) {
                  this.ToolStripRenderer = (ToolStripProfessionalRenderer)uis.Styles["VsToolWindowRenderer"]; 
                } 
            }
        } 
    }
}

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
