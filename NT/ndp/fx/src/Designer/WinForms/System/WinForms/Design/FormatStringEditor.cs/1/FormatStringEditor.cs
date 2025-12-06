//------------------------------------------------------------------------------ 
// <copyright file="FormatStringEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.FormatStringEditor..ctor()")] 
 
namespace System.Windows.Forms.Design {
 
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Windows.Forms; 

    /// <include file='doc\FormatStringEditor.uex' path='docs/doc[@for="FormatStringEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides an editor to edit advanced binding objects.</para>
    /// </devdoc>
    internal class FormatStringEditor : UITypeEditor { 

        private FormatStringDialog formatStringDialog; 
 
        /// <include file='doc\FormatStringEditor.uex' path='docs/doc[@for="FormatStringEditor.EditValue"]/*' />
        /// <devdoc> 
        ///    <para>Edits the specified value using the specified provider
        ///       within the specified context.</para>
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) { 
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
 
                if (edSvc != null) {
                    DataGridViewCellStyle dgvCellStyle = context.Instance as DataGridViewCellStyle; 
                    ListControl listControl = context.Instance as ListControl;

                    Debug.Assert(listControl != null || dgvCellStyle != null, "this editor is used for the DataGridViewCellStyle::Format and the ListControl::FormatString properties");
 
                    if (formatStringDialog == null) {
                        formatStringDialog = new FormatStringDialog(context); 
                    } 

                    if (listControl != null) { 
                        formatStringDialog.ListControl = listControl;
                    } else {
                        formatStringDialog.DataGridViewCellStyle = dgvCellStyle;
                    } 

                    IComponentChangeService changeSvc = (IComponentChangeService)provider.GetService(typeof(IComponentChangeService)); 
                    if (changeSvc != null) { 
                        if (dgvCellStyle != null) {
                            changeSvc.OnComponentChanging(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["Format"]); 
                            changeSvc.OnComponentChanging(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["NullValue"]);
                            changeSvc.OnComponentChanging(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["FormatProvider"]);
                        } else {
                            changeSvc.OnComponentChanging(listControl, TypeDescriptor.GetProperties(listControl)["FormatString"]); 
                            changeSvc.OnComponentChanging(listControl, TypeDescriptor.GetProperties(listControl)["FormatInfo"]);
                        } 
                    } 

                    edSvc.ShowDialog(formatStringDialog); 
                    formatStringDialog.End();

                    if (formatStringDialog.Dirty) {
                        // since the bindings may have changed, the properties listed in the properties window 
                        // need to be refreshed
                        TypeDescriptor.Refresh(context.Instance); 
                        if (changeSvc != null) { 
                            if (dgvCellStyle != null) {
                                changeSvc.OnComponentChanged(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["Format"], null, null); 
                                changeSvc.OnComponentChanged(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["NullValue"], null, null);
                                changeSvc.OnComponentChanged(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["FormatProvider"], null, null);
                            } else {
                                changeSvc.OnComponentChanged(listControl, TypeDescriptor.GetProperties(listControl)["FormatString"], null, null); 
                                changeSvc.OnComponentChanged(listControl, TypeDescriptor.GetProperties(listControl)["FormatInfo"], null, null);
                            } 
                        } 
                    }
                } 
            }

            return value;
        } 

        /// <include file='doc\FormatStringEditor.uex' path='docs/doc[@for="FormatStringEditor.GetEditStyle"]/*' /> 
        /// <devdoc> 
        ///    <para>Gets the edit style from the current context.</para>
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="FormatStringEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.FormatStringEditor..ctor()")] 
 
namespace System.Windows.Forms.Design {
 
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Windows.Forms; 

    /// <include file='doc\FormatStringEditor.uex' path='docs/doc[@for="FormatStringEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides an editor to edit advanced binding objects.</para>
    /// </devdoc>
    internal class FormatStringEditor : UITypeEditor { 

        private FormatStringDialog formatStringDialog; 
 
        /// <include file='doc\FormatStringEditor.uex' path='docs/doc[@for="FormatStringEditor.EditValue"]/*' />
        /// <devdoc> 
        ///    <para>Edits the specified value using the specified provider
        ///       within the specified context.</para>
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) { 
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
 
                if (edSvc != null) {
                    DataGridViewCellStyle dgvCellStyle = context.Instance as DataGridViewCellStyle; 
                    ListControl listControl = context.Instance as ListControl;

                    Debug.Assert(listControl != null || dgvCellStyle != null, "this editor is used for the DataGridViewCellStyle::Format and the ListControl::FormatString properties");
 
                    if (formatStringDialog == null) {
                        formatStringDialog = new FormatStringDialog(context); 
                    } 

                    if (listControl != null) { 
                        formatStringDialog.ListControl = listControl;
                    } else {
                        formatStringDialog.DataGridViewCellStyle = dgvCellStyle;
                    } 

                    IComponentChangeService changeSvc = (IComponentChangeService)provider.GetService(typeof(IComponentChangeService)); 
                    if (changeSvc != null) { 
                        if (dgvCellStyle != null) {
                            changeSvc.OnComponentChanging(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["Format"]); 
                            changeSvc.OnComponentChanging(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["NullValue"]);
                            changeSvc.OnComponentChanging(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["FormatProvider"]);
                        } else {
                            changeSvc.OnComponentChanging(listControl, TypeDescriptor.GetProperties(listControl)["FormatString"]); 
                            changeSvc.OnComponentChanging(listControl, TypeDescriptor.GetProperties(listControl)["FormatInfo"]);
                        } 
                    } 

                    edSvc.ShowDialog(formatStringDialog); 
                    formatStringDialog.End();

                    if (formatStringDialog.Dirty) {
                        // since the bindings may have changed, the properties listed in the properties window 
                        // need to be refreshed
                        TypeDescriptor.Refresh(context.Instance); 
                        if (changeSvc != null) { 
                            if (dgvCellStyle != null) {
                                changeSvc.OnComponentChanged(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["Format"], null, null); 
                                changeSvc.OnComponentChanged(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["NullValue"], null, null);
                                changeSvc.OnComponentChanged(dgvCellStyle, TypeDescriptor.GetProperties(dgvCellStyle)["FormatProvider"], null, null);
                            } else {
                                changeSvc.OnComponentChanged(listControl, TypeDescriptor.GetProperties(listControl)["FormatString"], null, null); 
                                changeSvc.OnComponentChanged(listControl, TypeDescriptor.GetProperties(listControl)["FormatInfo"], null, null);
                            } 
                        } 
                    }
                } 
            }

            return value;
        } 

        /// <include file='doc\FormatStringEditor.uex' path='docs/doc[@for="FormatStringEditor.GetEditStyle"]/*' /> 
        /// <devdoc> 
        ///    <para>Gets the edit style from the current context.</para>
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
