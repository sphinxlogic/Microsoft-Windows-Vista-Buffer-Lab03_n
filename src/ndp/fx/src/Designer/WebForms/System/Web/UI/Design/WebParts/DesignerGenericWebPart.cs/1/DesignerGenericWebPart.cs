//------------------------------------------------------------------------------ 
// <copyright file="DesignerGenericWebPart.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.Design; 
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;

    // Doesn't add the ChildControl to its Control collection 
    internal sealed class DesignerGenericWebPart : GenericWebPart {
 
        public DesignerGenericWebPart(Control control) : base(control) { 
        }
 
        protected override void CreateChildControls() {
            // Don't add the ChildControl to the Controls collection
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerGenericWebPart.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.Design; 
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;

    // Doesn't add the ChildControl to its Control collection 
    internal sealed class DesignerGenericWebPart : GenericWebPart {
 
        public DesignerGenericWebPart(Control control) : base(control) { 
        }
 
        protected override void CreateChildControls() {
            // Don't add the ChildControl to the Controls collection
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
