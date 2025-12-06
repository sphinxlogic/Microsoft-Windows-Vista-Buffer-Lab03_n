//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Diagnostics;
 
    /// <devdoc> 
    ///     Describes the list of actions that can be performed in the MaskedTextBox control from the
    ///     Chrome pannel. 
    /// </devdoc>
    internal class MaskedTextBoxDesignerActionList : System.ComponentModel.Design.DesignerActionList
    {
        MaskedTextBox maskedTextBox; 
        ITypeDiscoveryService discoverySvc;
        IUIService uiSvc; 
        IHelpService helpService = null; 

        /// <devdoc> 
        ///     Constructor receiving a MaskedTextBox control the action list applies to.  The ITypeDiscoveryService
        ///     service provider is used to populate the canned mask list control in the MaskDesignerDialog dialog and
        ///     the IUIService provider is used to display the MaskDesignerDialog within VS.
        /// </devdoc> 
        public MaskedTextBoxDesignerActionList(MaskedTextBoxDesigner designer) : base(designer.Component)
        { 
            this.maskedTextBox = (MaskedTextBox)designer.Component; 
            this.discoverySvc  = GetService(typeof(ITypeDiscoveryService)) as ITypeDiscoveryService;
            this.uiSvc         = GetService(typeof(IUIService)) as IUIService; 
            this.helpService = GetService(typeof(IHelpService)) as IHelpService;

            if (discoverySvc == null || uiSvc == null) {
                Debug.Fail("could not get either ITypeDiscoveryService or IUIService"); 
            }
        } 
 
        /// <devdoc>
        ///     Pops up the Mask design dialog for the user to set the control's mask. 
        /// </devdoc>
        public void SetMask()
        {
            string mask = MaskPropertyEditor.EditMask(this.discoverySvc, this.uiSvc, this.maskedTextBox, helpService); 

            if( mask != null ) 
            { 
                PropertyDescriptor maskProperty = TypeDescriptor.GetProperties(this.maskedTextBox)["Mask"];
 
                Debug.Assert( maskProperty != null, "Could not find 'Mask' property in control." );

                if( maskProperty != null )
                { 
                    maskProperty.SetValue(this.maskedTextBox, mask);
                } 
            } 
        }
 
        /// <devdoc>
        ///     Returns the control's action list items.
        /// </devdoc>
        public override DesignerActionItemCollection GetSortedActionItems() 
        {
 			DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            items.Add(new DesignerActionMethodItem(this, "SetMask", SR.GetString(SR.MaskedTextBoxDesignerVerbsSetMaskDesc))); 
            return items;
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
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Diagnostics;
 
    /// <devdoc> 
    ///     Describes the list of actions that can be performed in the MaskedTextBox control from the
    ///     Chrome pannel. 
    /// </devdoc>
    internal class MaskedTextBoxDesignerActionList : System.ComponentModel.Design.DesignerActionList
    {
        MaskedTextBox maskedTextBox; 
        ITypeDiscoveryService discoverySvc;
        IUIService uiSvc; 
        IHelpService helpService = null; 

        /// <devdoc> 
        ///     Constructor receiving a MaskedTextBox control the action list applies to.  The ITypeDiscoveryService
        ///     service provider is used to populate the canned mask list control in the MaskDesignerDialog dialog and
        ///     the IUIService provider is used to display the MaskDesignerDialog within VS.
        /// </devdoc> 
        public MaskedTextBoxDesignerActionList(MaskedTextBoxDesigner designer) : base(designer.Component)
        { 
            this.maskedTextBox = (MaskedTextBox)designer.Component; 
            this.discoverySvc  = GetService(typeof(ITypeDiscoveryService)) as ITypeDiscoveryService;
            this.uiSvc         = GetService(typeof(IUIService)) as IUIService; 
            this.helpService = GetService(typeof(IHelpService)) as IHelpService;

            if (discoverySvc == null || uiSvc == null) {
                Debug.Fail("could not get either ITypeDiscoveryService or IUIService"); 
            }
        } 
 
        /// <devdoc>
        ///     Pops up the Mask design dialog for the user to set the control's mask. 
        /// </devdoc>
        public void SetMask()
        {
            string mask = MaskPropertyEditor.EditMask(this.discoverySvc, this.uiSvc, this.maskedTextBox, helpService); 

            if( mask != null ) 
            { 
                PropertyDescriptor maskProperty = TypeDescriptor.GetProperties(this.maskedTextBox)["Mask"];
 
                Debug.Assert( maskProperty != null, "Could not find 'Mask' property in control." );

                if( maskProperty != null )
                { 
                    maskProperty.SetValue(this.maskedTextBox, mask);
                } 
            } 
        }
 
        /// <devdoc>
        ///     Returns the control's action list items.
        /// </devdoc>
        public override DesignerActionItemCollection GetSortedActionItems() 
        {
 			DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            items.Add(new DesignerActionMethodItem(this, "SetMask", SR.GetString(SR.MaskedTextBoxDesignerVerbsSetMaskDesc))); 
            return items;
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
