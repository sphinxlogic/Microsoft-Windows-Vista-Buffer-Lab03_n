//------------------------------------------------------------------------------ 
// <copyright file="DesignBindingEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.DesignBindingEditor..ctor()")] 
 

namespace System.Windows.Forms.Design { 

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design; 
 
    internal class DesignBindingEditor : UITypeEditor {
 
        private DesignBindingPicker designBindingPicker;

        public override bool IsDropDownResizable {
            get { 
                return true;
            } 
        } 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            if (provider != null) {
                if (designBindingPicker == null) {
                    designBindingPicker = new DesignBindingPicker();
                } 
                value = designBindingPicker.Pick(context, provider,
                                                 true,  /* showDataSources   */ 
                                                 true,  /* showDataMembers   */ 
                                                 false, /* selectListMembers */
                                                 null, String.Empty, 
                                                 (DesignBinding) value);
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
// <copyright file="DesignBindingEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.DesignBindingEditor..ctor()")] 
 

namespace System.Windows.Forms.Design { 

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design; 
 
    internal class DesignBindingEditor : UITypeEditor {
 
        private DesignBindingPicker designBindingPicker;

        public override bool IsDropDownResizable {
            get { 
                return true;
            } 
        } 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            if (provider != null) {
                if (designBindingPicker == null) {
                    designBindingPicker = new DesignBindingPicker();
                } 
                value = designBindingPicker.Pick(context, provider,
                                                 true,  /* showDataSources   */ 
                                                 true,  /* showDataMembers   */ 
                                                 false, /* selectListMembers */
                                                 null, String.Empty, 
                                                 (DesignBinding) value);
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
