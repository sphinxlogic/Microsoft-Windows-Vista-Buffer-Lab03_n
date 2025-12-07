//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewCellStyleEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System.Runtime.InteropServices;

    using System.Diagnostics;
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using Microsoft.Win32;
    using System.Drawing; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using System.Drawing.Design;
 
    internal class DataGridViewCellStyleEditor : UITypeEditor {
 
        private DataGridViewCellStyleBuilder builderDialog; 
        private object value;
 
        /// <include file='doc\TableCellStyleEditor.uex' path='docs/doc[@for="DataGridViewCellStyleEditor.EditValue"]/*' />
        /// <devdoc>
        ///      Edits the given object value using the editor style provided by
        ///      GetEditorStyle.  A service provider is provided so that any 
        ///      required editing services can be obtained.
        /// </devdoc> 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 

            this.value = value; 

            Debug.Assert(provider != null, "No service provider; we cannot edit the value");
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
                IUIService uiService = (IUIService) provider.GetService(typeof(IUIService));
                IComponent comp = context.Instance as IComponent; 
 
                Debug.Assert(edSvc != null, "No editor service; we cannot edit the value");
                if (edSvc != null) { 
                    if (builderDialog == null) {
                        builderDialog = new DataGridViewCellStyleBuilder(provider, comp);
                    }
 
                    if (uiService != null) {
                        builderDialog.Font = (Font) uiService.Styles["DialogFont"]; 
                    } 

                    DataGridViewCellStyle dgvcs = value as DataGridViewCellStyle; 
                    if (dgvcs != null) {
                        builderDialog.CellStyle = dgvcs;
                    }
 
                    builderDialog.Context = context;
 
                    //IntPtr hwndFocus = UnsafeNativeMethods.GetFocus(); 
                    try {
                        if (builderDialog.ShowDialog() == DialogResult.OK) { 
                            this.value = builderDialog.CellStyle;
                        }
                    }
                    finally { 
                        //if (hwndFocus != IntPtr.Zero) {
                        //    UnsafeNativeMethods.SetFocus(new HandleRef(null, hwndFocus)); 
                        //} 
                    }
                } 
            }

            // Now pull out the updated value, if there was one.
            // 
            value = this.value;
            this.value = null; 
 
            return value;
        } 

        /// <include file='doc\TableCellStyleEditor.uex' path='docs/doc[@for="DataGridViewCellStyleEditor.GetEditStyle"]/*' />
        /// <devdoc>
        ///      Retrieves the editing style of the Edit method.  If the method 
        ///      is not supported, this will return None.
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewCellStyleEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System.Runtime.InteropServices;

    using System.Diagnostics;
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using Microsoft.Win32;
    using System.Drawing; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using System.Drawing.Design;
 
    internal class DataGridViewCellStyleEditor : UITypeEditor {
 
        private DataGridViewCellStyleBuilder builderDialog; 
        private object value;
 
        /// <include file='doc\TableCellStyleEditor.uex' path='docs/doc[@for="DataGridViewCellStyleEditor.EditValue"]/*' />
        /// <devdoc>
        ///      Edits the given object value using the editor style provided by
        ///      GetEditorStyle.  A service provider is provided so that any 
        ///      required editing services can be obtained.
        /// </devdoc> 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 

            this.value = value; 

            Debug.Assert(provider != null, "No service provider; we cannot edit the value");
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
                IUIService uiService = (IUIService) provider.GetService(typeof(IUIService));
                IComponent comp = context.Instance as IComponent; 
 
                Debug.Assert(edSvc != null, "No editor service; we cannot edit the value");
                if (edSvc != null) { 
                    if (builderDialog == null) {
                        builderDialog = new DataGridViewCellStyleBuilder(provider, comp);
                    }
 
                    if (uiService != null) {
                        builderDialog.Font = (Font) uiService.Styles["DialogFont"]; 
                    } 

                    DataGridViewCellStyle dgvcs = value as DataGridViewCellStyle; 
                    if (dgvcs != null) {
                        builderDialog.CellStyle = dgvcs;
                    }
 
                    builderDialog.Context = context;
 
                    //IntPtr hwndFocus = UnsafeNativeMethods.GetFocus(); 
                    try {
                        if (builderDialog.ShowDialog() == DialogResult.OK) { 
                            this.value = builderDialog.CellStyle;
                        }
                    }
                    finally { 
                        //if (hwndFocus != IntPtr.Zero) {
                        //    UnsafeNativeMethods.SetFocus(new HandleRef(null, hwndFocus)); 
                        //} 
                    }
                } 
            }

            // Now pull out the updated value, if there was one.
            // 
            value = this.value;
            this.value = null; 
 
            return value;
        } 

        /// <include file='doc\TableCellStyleEditor.uex' path='docs/doc[@for="DataGridViewCellStyleEditor.GetEditStyle"]/*' />
        /// <devdoc>
        ///      Retrieves the editing style of the Edit method.  If the method 
        ///      is not supported, this will return None.
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
