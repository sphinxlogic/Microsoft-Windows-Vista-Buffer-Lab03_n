//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Drawing.Design;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Diagnostics; 
 
    /// <devdoc>
    ///     Design time editing class for the Mask property of the MaskedTextBox control. 
    /// </devdoc>
    internal class MaskPropertyEditor : UITypeEditor
    {
        /// <devdoc> 
        ///     Constructor.
        /// </devdoc> 
        public MaskPropertyEditor() 
        {
        } 

        /// <devdoc>
        ///     Gets the mask property value fromt the MaskDesignerDialog.
        ///     The IUIService is used to show the mask designer dialog within VS so it doesn't get blocked if focus 
        ///     is moved to anoter app.
        /// </devdoc> 
        internal static string EditMask(ITypeDiscoveryService discoverySvc, IUIService uiSvc, MaskedTextBox instance, IHelpService helpService) { 
            Debug.Assert( instance != null, "Null masked text box." );
 
            string mask = null;
            MaskDesignerDialog dlg = new MaskDesignerDialog(instance, helpService);

            try 
            {
                dlg.DiscoverMaskDescriptors( discoverySvc );  // fine if service is null. 
 
                // Show dialog from VS.
                // Debug.Assert( uiSvc != null, "Expected IUIService, defaulting to an intrusive way to show the dialog..." ); 

                DialogResult dlgResult = uiSvc != null ? uiSvc.ShowDialog( dlg ) : dlg.ShowDialog();

                if ( dlgResult == DialogResult.OK) 
                {
                    mask = dlg.Mask; 
 
                    // ValidatingType is not browsable so we don't need to set the property through the designer.
                    if (dlg.ValidatingType != instance.ValidatingType) 
                    {
                        instance.ValidatingType = dlg.ValidatingType;
                    }
                } 
            }
            finally 
            { 
                dlg.Dispose();
            } 

            // Will return null if dlgResult != OK.
            return mask;
        } 

        /// <devdoc> 
        ///     Edits the Mask property of the MaskedTextBox control from the PropertyGrid. 
        /// </devdoc>
 
        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")]
        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value) 
        {
            if (context != null && provider != null) 
            { 
                ITypeDiscoveryService discoverySvc = (ITypeDiscoveryService) provider.GetService(typeof(ITypeDiscoveryService));  // fine if service is not found.
                IUIService uiSvc = (IUIService) provider.GetService(typeof(IUIService)); 
                IHelpService helpService = (IHelpService)provider.GetService(typeof(IHelpService));
                string mask = MaskPropertyEditor.EditMask(discoverySvc, uiSvc, context.Instance as MaskedTextBox, helpService);

                if( mask != null ) 
                {
                    return mask; 
                } 
            }
 
            return value;
        }

        /// <devdoc> 
        ///     Painting a representation of the Mask value is not supported.
        /// </devdoc> 
 
        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")]
        public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
        {
            return false; 
        }
 
        /// <devdoc> 
        ///     Gets the edit style of the type editor.
        /// </devdoc> 

        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] 
        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        { 
            return UITypeEditorEditStyle.Modal; 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Drawing.Design;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Diagnostics; 
 
    /// <devdoc>
    ///     Design time editing class for the Mask property of the MaskedTextBox control. 
    /// </devdoc>
    internal class MaskPropertyEditor : UITypeEditor
    {
        /// <devdoc> 
        ///     Constructor.
        /// </devdoc> 
        public MaskPropertyEditor() 
        {
        } 

        /// <devdoc>
        ///     Gets the mask property value fromt the MaskDesignerDialog.
        ///     The IUIService is used to show the mask designer dialog within VS so it doesn't get blocked if focus 
        ///     is moved to anoter app.
        /// </devdoc> 
        internal static string EditMask(ITypeDiscoveryService discoverySvc, IUIService uiSvc, MaskedTextBox instance, IHelpService helpService) { 
            Debug.Assert( instance != null, "Null masked text box." );
 
            string mask = null;
            MaskDesignerDialog dlg = new MaskDesignerDialog(instance, helpService);

            try 
            {
                dlg.DiscoverMaskDescriptors( discoverySvc );  // fine if service is null. 
 
                // Show dialog from VS.
                // Debug.Assert( uiSvc != null, "Expected IUIService, defaulting to an intrusive way to show the dialog..." ); 

                DialogResult dlgResult = uiSvc != null ? uiSvc.ShowDialog( dlg ) : dlg.ShowDialog();

                if ( dlgResult == DialogResult.OK) 
                {
                    mask = dlg.Mask; 
 
                    // ValidatingType is not browsable so we don't need to set the property through the designer.
                    if (dlg.ValidatingType != instance.ValidatingType) 
                    {
                        instance.ValidatingType = dlg.ValidatingType;
                    }
                } 
            }
            finally 
            { 
                dlg.Dispose();
            } 

            // Will return null if dlgResult != OK.
            return mask;
        } 

        /// <devdoc> 
        ///     Edits the Mask property of the MaskedTextBox control from the PropertyGrid. 
        /// </devdoc>
 
        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")]
        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value) 
        {
            if (context != null && provider != null) 
            { 
                ITypeDiscoveryService discoverySvc = (ITypeDiscoveryService) provider.GetService(typeof(ITypeDiscoveryService));  // fine if service is not found.
                IUIService uiSvc = (IUIService) provider.GetService(typeof(IUIService)); 
                IHelpService helpService = (IHelpService)provider.GetService(typeof(IHelpService));
                string mask = MaskPropertyEditor.EditMask(discoverySvc, uiSvc, context.Instance as MaskedTextBox, helpService);

                if( mask != null ) 
                {
                    return mask; 
                } 
            }
 
            return value;
        }

        /// <devdoc> 
        ///     Painting a representation of the Mask value is not supported.
        /// </devdoc> 
 
        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")]
        public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
        {
            return false; 
        }
 
        /// <devdoc> 
        ///     Gets the edit style of the type editor.
        /// </devdoc> 

        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] 
        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        { 
            return UITypeEditorEditStyle.Modal; 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
