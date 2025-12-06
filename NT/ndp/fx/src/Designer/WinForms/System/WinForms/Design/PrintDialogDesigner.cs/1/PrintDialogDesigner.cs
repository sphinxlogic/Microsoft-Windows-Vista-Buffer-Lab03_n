//------------------------------------------------------------------------------ 
// <copyright file="PrintDialogDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using Microsoft.Win32;
    using System.Collections;
    using System.Diagnostics; 

    /// <devdoc> 
    ///      This is the designer for PrintDialog components. 
    /// </devdoc>
    internal class PrintDialogDesigner : ComponentDesigner { 
        /// <devdoc>
        ///     This method is called when a component is first initialized, typically after being first added
        ///     to a design surface.  We need to override this since the printDialog when added in Whidbey should set
        ///     the UseEXDialog == true; 
        ///     UseEXDialog = true means to use the EX versions of the dialogs when running on XP or above, and to ignore the ShowHelp & ShowNetwork properties.
        ///     If running below XP then UseEXDialog is ignored and the non-EX dialogs are used & ShowHelp & ShowNetwork are respected. 
        ///     UseEXDialog = false means to never use the EX versions of the dialog regardless of which O/S app is running on. ShowHelp & ShowNetwork will work in this case. 
        /// </devdoc>
        public override void InitializeNewComponent(IDictionary defaultValues) { 
            PrintDialog pd = Component as PrintDialog;
            Debug.Assert(pd != null, " PrintDialog is null !!");
            if (pd != null)
            { 
                pd.UseEXDialog = true;
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PrintDialogDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using Microsoft.Win32;
    using System.Collections;
    using System.Diagnostics; 

    /// <devdoc> 
    ///      This is the designer for PrintDialog components. 
    /// </devdoc>
    internal class PrintDialogDesigner : ComponentDesigner { 
        /// <devdoc>
        ///     This method is called when a component is first initialized, typically after being first added
        ///     to a design surface.  We need to override this since the printDialog when added in Whidbey should set
        ///     the UseEXDialog == true; 
        ///     UseEXDialog = true means to use the EX versions of the dialogs when running on XP or above, and to ignore the ShowHelp & ShowNetwork properties.
        ///     If running below XP then UseEXDialog is ignored and the non-EX dialogs are used & ShowHelp & ShowNetwork are respected. 
        ///     UseEXDialog = false means to never use the EX versions of the dialog regardless of which O/S app is running on. ShowHelp & ShowNetwork will work in this case. 
        /// </devdoc>
        public override void InitializeNewComponent(IDictionary defaultValues) { 
            PrintDialog pd = Component as PrintDialog;
            Debug.Assert(pd != null, " PrintDialog is null !!");
            if (pd != null)
            { 
                pd.UseEXDialog = true;
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
