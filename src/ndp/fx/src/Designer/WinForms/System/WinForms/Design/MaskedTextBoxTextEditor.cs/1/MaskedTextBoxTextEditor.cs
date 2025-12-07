using System; 
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing; 
using System.Windows.Forms;
using System.Windows.Forms.Design; 
using System.Drawing.Design; 
using System.Diagnostics;
 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.MaskedTextBoxTextEditor..ctor()")]

namespace System.Windows.Forms.Design
{ 
    class MaskedTextBoxTextEditor : UITypeEditor
    { 
        public MaskedTextBoxTextEditor() 
        {
        } 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorSvc = null; 

            if (context != null && context.Instance != null && provider != null) 
            { 
                editorSvc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
 
                if (editorSvc != null && context.Instance != null)
                {
                    MaskedTextBox mtb = context.Instance as MaskedTextBox;
 
                    // If multiple instances selected, mtb will be null.
                    if( mtb == null ) 
                    { 
                        mtb = new MaskedTextBox();
                        mtb.Text = value as string; 
                    }

                    MaskedTextBoxTextEditorDropDown dropDown = new MaskedTextBoxTextEditorDropDown(mtb);
                    editorSvc.DropDownControl(dropDown); 

                    if (dropDown.Value != null) 
                    { 
                        value = dropDown.Value;
                    } 
                }
            }

            return value; 
        }
 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
        {
            if (context != null && context.Instance != null) 
            {
                return UITypeEditorEditStyle.DropDown;
            }
 
            return base.GetEditStyle(context);
        } 
 
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        { 
            if (context != null && context.Instance != null)
            {
                return false;
            } 

            return base.GetPaintValueSupported(context); 
        } 

        public override bool IsDropDownResizable 
        {
            get
            {
                return false; 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing; 
using System.Windows.Forms;
using System.Windows.Forms.Design; 
using System.Drawing.Design; 
using System.Diagnostics;
 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.MaskedTextBoxTextEditor..ctor()")]

namespace System.Windows.Forms.Design
{ 
    class MaskedTextBoxTextEditor : UITypeEditor
    { 
        public MaskedTextBoxTextEditor() 
        {
        } 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorSvc = null; 

            if (context != null && context.Instance != null && provider != null) 
            { 
                editorSvc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
 
                if (editorSvc != null && context.Instance != null)
                {
                    MaskedTextBox mtb = context.Instance as MaskedTextBox;
 
                    // If multiple instances selected, mtb will be null.
                    if( mtb == null ) 
                    { 
                        mtb = new MaskedTextBox();
                        mtb.Text = value as string; 
                    }

                    MaskedTextBoxTextEditorDropDown dropDown = new MaskedTextBoxTextEditorDropDown(mtb);
                    editorSvc.DropDownControl(dropDown); 

                    if (dropDown.Value != null) 
                    { 
                        value = dropDown.Value;
                    } 
                }
            }

            return value; 
        }
 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
        {
            if (context != null && context.Instance != null) 
            {
                return UITypeEditorEditStyle.DropDown;
            }
 
            return base.GetEditStyle(context);
        } 
 
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        { 
            if (context != null && context.Instance != null)
            {
                return false;
            } 

            return base.GetPaintValueSupported(context); 
        } 

        public override bool IsDropDownResizable 
        {
            get
            {
                return false; 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
