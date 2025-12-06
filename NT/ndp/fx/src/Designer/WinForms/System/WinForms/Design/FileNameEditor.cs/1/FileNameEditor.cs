//------------------------------------------------------------------------------ 
// <copyright file="FileNameEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Runtime.Serialization.Formatters; 

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using Microsoft.Win32;

    /// <include file='doc\FileNameEditor.uex' path='docs/doc[@for="FileNameEditor"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides an 
    ///       editor for filenames.</para>
    /// </devdoc> 
    public class FileNameEditor : UITypeEditor {

        private OpenFileDialog openFileDialog;
 
        /// <include file='doc\FileNameEditor.uex' path='docs/doc[@for="FileNameEditor.EditValue"]/*' />
        /// <devdoc> 
        ///      Edits the given object value using the editor style provided by 
        ///      GetEditorStyle.  A service provider is provided so that any
        ///      required editing services can be obtained. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
 
            Debug.Assert(provider != null, "No service provider; we cannot edit the value");
            if (provider != null) { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 

                Debug.Assert(edSvc != null, "No editor service; we cannot edit the value"); 
                if (edSvc != null) {
                    if (openFileDialog == null) {
                        openFileDialog = new OpenFileDialog();
                        InitializeDialog(openFileDialog); 
                    }
 
                    if (value is string) { 
                        openFileDialog.FileName = (string)value;
                    } 

                    if (openFileDialog.ShowDialog() == DialogResult.OK) {
                        value = openFileDialog.FileName;
 
                    }
                } 
            } 

            return value; 
        }

        /// <include file='doc\FileNameEditor.uex' path='docs/doc[@for="FileNameEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///    <para>Gets the editing style of the Edit method. If the method
        ///       is not supported, this will return None.</para> 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        }

        /// <include file='doc\FileNameEditor.uex' path='docs/doc[@for="FileNameEditor.InitializeDialog"]/*' /> 
        /// <devdoc>
        ///      Initializes the open file dialog when it is created.  This gives you 
        ///      an opportunity to configure the dialog as you please.  The default 
        ///      implementation provides a generic file filter and title.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        protected virtual void InitializeDialog(OpenFileDialog openFileDialog) {
            openFileDialog.Filter = SR.GetString(SR.GenericFileFilter);
            openFileDialog.Title = SR.GetString(SR.GenericOpenFile); 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="FileNameEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Runtime.Serialization.Formatters; 

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using Microsoft.Win32;

    /// <include file='doc\FileNameEditor.uex' path='docs/doc[@for="FileNameEditor"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides an 
    ///       editor for filenames.</para>
    /// </devdoc> 
    public class FileNameEditor : UITypeEditor {

        private OpenFileDialog openFileDialog;
 
        /// <include file='doc\FileNameEditor.uex' path='docs/doc[@for="FileNameEditor.EditValue"]/*' />
        /// <devdoc> 
        ///      Edits the given object value using the editor style provided by 
        ///      GetEditorStyle.  A service provider is provided so that any
        ///      required editing services can be obtained. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
 
            Debug.Assert(provider != null, "No service provider; we cannot edit the value");
            if (provider != null) { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 

                Debug.Assert(edSvc != null, "No editor service; we cannot edit the value"); 
                if (edSvc != null) {
                    if (openFileDialog == null) {
                        openFileDialog = new OpenFileDialog();
                        InitializeDialog(openFileDialog); 
                    }
 
                    if (value is string) { 
                        openFileDialog.FileName = (string)value;
                    } 

                    if (openFileDialog.ShowDialog() == DialogResult.OK) {
                        value = openFileDialog.FileName;
 
                    }
                } 
            } 

            return value; 
        }

        /// <include file='doc\FileNameEditor.uex' path='docs/doc[@for="FileNameEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///    <para>Gets the editing style of the Edit method. If the method
        ///       is not supported, this will return None.</para> 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        }

        /// <include file='doc\FileNameEditor.uex' path='docs/doc[@for="FileNameEditor.InitializeDialog"]/*' /> 
        /// <devdoc>
        ///      Initializes the open file dialog when it is created.  This gives you 
        ///      an opportunity to configure the dialog as you please.  The default 
        ///      implementation provides a generic file filter and title.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        protected virtual void InitializeDialog(OpenFileDialog openFileDialog) {
            openFileDialog.Filter = SR.GetString(SR.GenericFileFilter);
            openFileDialog.Title = SR.GetString(SR.GenericOpenFile); 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
