//------------------------------------------------------------------------------ 
// <copyright file="AdvancedBindingEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Windows.Forms;
 
    /// <include file='doc\AdvancedBindingEditor.uex' path='docs/doc[@for="AdvancedBindingEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides an editor to edit advanced binding objects.</para> 
    /// </devdoc>
    internal class AdvancedBindingEditor : UITypeEditor {

        private BindingFormattingDialog bindingFormattingDialog; 

        /// <include file='doc\AdvancedBindingEditor.uex' path='docs/doc[@for="AdvancedBindingEditor.EditValue"]/*' /> 
        /// <devdoc> 
        ///    <para>Edits the specified value using the specified provider
        ///       within the specified context.</para> 
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
                IDesignerHost host = provider.GetService(typeof(IDesignerHost)) as IDesignerHost;
 
                if (edSvc != null && host != null) { 
                    if (bindingFormattingDialog == null) {
                        bindingFormattingDialog = new BindingFormattingDialog(); 
                    }

                    bindingFormattingDialog.Context = context;
                    bindingFormattingDialog.Bindings = (ControlBindingsCollection) value; 
                    bindingFormattingDialog.Host = host;
 
                    using (DesignerTransaction t = host.CreateTransaction()) { 
                        edSvc.ShowDialog(bindingFormattingDialog);
 
                        if (bindingFormattingDialog.Dirty) {
                            // since the bindings may have changed, the properties listed in the properties window
                            // need to be refreshed
                            System.Diagnostics.Debug.Assert(context.Instance is ControlBindingsCollection); 
                            TypeDescriptor.Refresh(((ControlBindingsCollection)context.Instance).BindableComponent);
 
                            if (t != null) { 
                                t.Commit();
                            } 
                        }
                        else {
                            t.Cancel();
                        } 
                    }
                } 
            } 

            return value; 
        }

        /// <include file='doc\AdvancedBindingEditor.uex' path='docs/doc[@for="AdvancedBindingEditor.GetEditStyle"]/*' />
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
// <copyright file="AdvancedBindingEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Windows.Forms;
 
    /// <include file='doc\AdvancedBindingEditor.uex' path='docs/doc[@for="AdvancedBindingEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides an editor to edit advanced binding objects.</para> 
    /// </devdoc>
    internal class AdvancedBindingEditor : UITypeEditor {

        private BindingFormattingDialog bindingFormattingDialog; 

        /// <include file='doc\AdvancedBindingEditor.uex' path='docs/doc[@for="AdvancedBindingEditor.EditValue"]/*' /> 
        /// <devdoc> 
        ///    <para>Edits the specified value using the specified provider
        ///       within the specified context.</para> 
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
                IDesignerHost host = provider.GetService(typeof(IDesignerHost)) as IDesignerHost;
 
                if (edSvc != null && host != null) { 
                    if (bindingFormattingDialog == null) {
                        bindingFormattingDialog = new BindingFormattingDialog(); 
                    }

                    bindingFormattingDialog.Context = context;
                    bindingFormattingDialog.Bindings = (ControlBindingsCollection) value; 
                    bindingFormattingDialog.Host = host;
 
                    using (DesignerTransaction t = host.CreateTransaction()) { 
                        edSvc.ShowDialog(bindingFormattingDialog);
 
                        if (bindingFormattingDialog.Dirty) {
                            // since the bindings may have changed, the properties listed in the properties window
                            // need to be refreshed
                            System.Diagnostics.Debug.Assert(context.Instance is ControlBindingsCollection); 
                            TypeDescriptor.Refresh(((ControlBindingsCollection)context.Instance).BindableComponent);
 
                            if (t != null) { 
                                t.Commit();
                            } 
                        }
                        else {
                            t.Cancel();
                        } 
                    }
                } 
            } 

            return value; 
        }

        /// <include file='doc\AdvancedBindingEditor.uex' path='docs/doc[@for="AdvancedBindingEditor.GetEditStyle"]/*' />
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
