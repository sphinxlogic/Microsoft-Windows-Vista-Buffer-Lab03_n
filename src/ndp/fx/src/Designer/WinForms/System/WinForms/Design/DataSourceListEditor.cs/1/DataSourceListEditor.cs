//------------------------------------------------------------------------------ 
// <copyright file="DataSourceListEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.DataSourceListEditor..ctor()")] 
 
namespace System.Windows.Forms.Design {
 
    using System.Design;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design; 
 
    internal class DataSourceListEditor : UITypeEditor {
 
        private DesignBindingPicker designBindingPicker;

        public override bool IsDropDownResizable {
            get { 
                return true;
            } 
        } 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider  provider, object value) { 
            if (provider != null && context.Instance != null) {
                if (designBindingPicker == null) {
                    designBindingPicker = new DesignBindingPicker();
                } 
                DesignBinding oldSelection = new DesignBinding(value, "");
                DesignBinding newSelection = designBindingPicker.Pick(context, provider, 
                                                                      true,  /* showDataSources   */ 
                                                                      false, /* showDataMembers   */
                                                                      false, /* selectListMembers */ 
                                                                      null, String.Empty,
                                                                      oldSelection);
                if (newSelection != null) {
                    value = newSelection.DataSource; 
                }
            } 
 
            return value;
        } 

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.DropDown;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataSourceListEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.DataSourceListEditor..ctor()")] 
 
namespace System.Windows.Forms.Design {
 
    using System.Design;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design; 
 
    internal class DataSourceListEditor : UITypeEditor {
 
        private DesignBindingPicker designBindingPicker;

        public override bool IsDropDownResizable {
            get { 
                return true;
            } 
        } 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider  provider, object value) { 
            if (provider != null && context.Instance != null) {
                if (designBindingPicker == null) {
                    designBindingPicker = new DesignBindingPicker();
                } 
                DesignBinding oldSelection = new DesignBinding(value, "");
                DesignBinding newSelection = designBindingPicker.Pick(context, provider, 
                                                                      true,  /* showDataSources   */ 
                                                                      false, /* showDataMembers   */
                                                                      false, /* selectListMembers */ 
                                                                      null, String.Empty,
                                                                      oldSelection);
                if (newSelection != null) {
                    value = newSelection.DataSource; 
                }
            } 
 
            return value;
        } 

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.DropDown;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
