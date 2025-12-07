//------------------------------------------------------------------------------ 
// <copyright file="IconEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Drawing.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing; 
    using System.IO;
    using System.Runtime.InteropServices; 
    using System.Windows.Forms; 
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design; 

    /// <internalonly/>
    /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor"]/*' />
    /// <devdoc> 
    ///    <para>Provides an editor for visually picking an icon.</para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class IconEditor : UITypeEditor {
    // NOTE: this class should be almost identical to ImageEditor.  The main exception is PaintValue, 
    // which has logic that should probably be copied into ImageEditor.

        internal static Type[] imageExtenders = new Type[] { };
 
        internal FileDialog fileDialog;
 
        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.CreateExtensionsString"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1818:DoNotConcatenateStringsInsideLoops")]
        protected static string CreateExtensionsString(string[] extensions, string sep) {
            if (extensions == null || extensions.Length == 0) 
                return null;
            string text = null; 
            for (int i = 0; i < extensions.Length - 1; i++) 
                text = text + "*." + extensions[i] + sep;
            text = text + "*." + extensions[extensions.Length-1]; 
            return text;
        }

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.CreateFilterEntry"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] // previously shipped this way. Would be a breaking change.
        protected static string CreateFilterEntry(IconEditor e) 
        {
            string desc = e.GetFileDialogDescription();
            string exts = CreateExtensionsString(e.GetExtensions(),",");
            string extsSemis = CreateExtensionsString(e.GetExtensions(),";"); 
            return desc + "(" + exts + ")|" + extsSemis;
        } 
 
        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.EditValue"]/*' />
        /// <devdoc> 
        ///      Edits the given object value using the editor style provided by
        ///      GetEditorStyle.  A service provider is provided so that any
        ///      required editing services can be obtained.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            if (provider != null) { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
 
                if (edSvc != null) {
                    if (fileDialog == null) {
                        fileDialog = new OpenFileDialog();
                        string filter = CreateFilterEntry(this); 
                        for (int i = 0; i < imageExtenders.Length; i++) {
                    Debug.Fail("Why does IconEditor have subclasses if Icon doesn't?"); 
                        } 
                        fileDialog.Filter = filter;
                    } 

                    IntPtr hwndFocus = UnsafeNativeMethods.GetFocus();
                    try {
                        if (fileDialog.ShowDialog() == DialogResult.OK) { 
                            FileStream file = new FileStream(fileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                            value = LoadFromStream(file); 
                        } 
                    }
                    finally { 
                        if (hwndFocus != IntPtr.Zero) {
                            UnsafeNativeMethods.SetFocus(new HandleRef(null, hwndFocus));
                        }
                    } 

                } 
            } 
            return value;
        } 

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.GetEditStyle"]/*' />
        /// <devdoc>
        ///      Retrieves the editing style of the Edit method.  If the method 
        ///      is not supported, this will return None.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal; 
        }

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.GetFileDialogDescription"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected virtual string GetFileDialogDescription() { 
            return SR.GetString(SR.iconFileDescription);
        } 

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.GetExtensions"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        protected virtual string[] GetExtensions() { 
            return new string[] { "ico"};
        } 

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.GetPaintValueSupported"]/*' />
        /// <devdoc>
        ///      Determines if this editor supports the painting of a representation 
        ///      of an object's value.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override bool GetPaintValueSupported(ITypeDescriptorContext context) {
            return true; 
        }

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.LoadFromStream"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected virtual Icon LoadFromStream(Stream stream) { 
            return new Icon(stream);
        } 

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.PaintValue"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Paints a representative value of the given object to the provided
        ///       canvas. Painting should be done within the boundaries of the 
        ///       provided rectangle. 
        ///    </para>
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")] //Benign code
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override void PaintValue(PaintValueEventArgs e) {
           Icon icon = e.Value as Icon; 
           if (icon != null) {
                // If icon is smaller than rectangle, just center it unscaled in the rectangle 
                Size iconSize = icon.Size; 
                Rectangle rectangle = e.Bounds;
                if (icon.Width < rectangle.Width) { 
                    rectangle.X = (rectangle.Width - icon.Width) / 2;
                    rectangle.Width = icon.Width;
                }
                if (icon.Height < rectangle.Height) { 
                    rectangle.X = (rectangle.Height - icon.Height) / 2;
                    rectangle.Height = icon.Height; 
                } 

                e.Graphics.DrawIcon(icon, rectangle); 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="IconEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Drawing.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing; 
    using System.IO;
    using System.Runtime.InteropServices; 
    using System.Windows.Forms; 
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design; 

    /// <internalonly/>
    /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor"]/*' />
    /// <devdoc> 
    ///    <para>Provides an editor for visually picking an icon.</para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class IconEditor : UITypeEditor {
    // NOTE: this class should be almost identical to ImageEditor.  The main exception is PaintValue, 
    // which has logic that should probably be copied into ImageEditor.

        internal static Type[] imageExtenders = new Type[] { };
 
        internal FileDialog fileDialog;
 
        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.CreateExtensionsString"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1818:DoNotConcatenateStringsInsideLoops")]
        protected static string CreateExtensionsString(string[] extensions, string sep) {
            if (extensions == null || extensions.Length == 0) 
                return null;
            string text = null; 
            for (int i = 0; i < extensions.Length - 1; i++) 
                text = text + "*." + extensions[i] + sep;
            text = text + "*." + extensions[extensions.Length-1]; 
            return text;
        }

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.CreateFilterEntry"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] // previously shipped this way. Would be a breaking change.
        protected static string CreateFilterEntry(IconEditor e) 
        {
            string desc = e.GetFileDialogDescription();
            string exts = CreateExtensionsString(e.GetExtensions(),",");
            string extsSemis = CreateExtensionsString(e.GetExtensions(),";"); 
            return desc + "(" + exts + ")|" + extsSemis;
        } 
 
        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.EditValue"]/*' />
        /// <devdoc> 
        ///      Edits the given object value using the editor style provided by
        ///      GetEditorStyle.  A service provider is provided so that any
        ///      required editing services can be obtained.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            if (provider != null) { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
 
                if (edSvc != null) {
                    if (fileDialog == null) {
                        fileDialog = new OpenFileDialog();
                        string filter = CreateFilterEntry(this); 
                        for (int i = 0; i < imageExtenders.Length; i++) {
                    Debug.Fail("Why does IconEditor have subclasses if Icon doesn't?"); 
                        } 
                        fileDialog.Filter = filter;
                    } 

                    IntPtr hwndFocus = UnsafeNativeMethods.GetFocus();
                    try {
                        if (fileDialog.ShowDialog() == DialogResult.OK) { 
                            FileStream file = new FileStream(fileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                            value = LoadFromStream(file); 
                        } 
                    }
                    finally { 
                        if (hwndFocus != IntPtr.Zero) {
                            UnsafeNativeMethods.SetFocus(new HandleRef(null, hwndFocus));
                        }
                    } 

                } 
            } 
            return value;
        } 

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.GetEditStyle"]/*' />
        /// <devdoc>
        ///      Retrieves the editing style of the Edit method.  If the method 
        ///      is not supported, this will return None.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal; 
        }

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.GetFileDialogDescription"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected virtual string GetFileDialogDescription() { 
            return SR.GetString(SR.iconFileDescription);
        } 

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.GetExtensions"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        protected virtual string[] GetExtensions() { 
            return new string[] { "ico"};
        } 

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.GetPaintValueSupported"]/*' />
        /// <devdoc>
        ///      Determines if this editor supports the painting of a representation 
        ///      of an object's value.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override bool GetPaintValueSupported(ITypeDescriptorContext context) {
            return true; 
        }

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.LoadFromStream"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected virtual Icon LoadFromStream(Stream stream) { 
            return new Icon(stream);
        } 

        /// <include file='doc\IconEditor.uex' path='docs/doc[@for="IconEditor.PaintValue"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Paints a representative value of the given object to the provided
        ///       canvas. Painting should be done within the boundaries of the 
        ///       provided rectangle. 
        ///    </para>
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")] //Benign code
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override void PaintValue(PaintValueEventArgs e) {
           Icon icon = e.Value as Icon; 
           if (icon != null) {
                // If icon is smaller than rectangle, just center it unscaled in the rectangle 
                Size iconSize = icon.Size; 
                Rectangle rectangle = e.Bounds;
                if (icon.Width < rectangle.Width) { 
                    rectangle.X = (rectangle.Width - icon.Width) / 2;
                    rectangle.Width = icon.Width;
                }
                if (icon.Height < rectangle.Height) { 
                    rectangle.X = (rectangle.Height - icon.Height) / 2;
                    rectangle.Height = icon.Height; 
                } 

                e.Graphics.DrawIcon(icon, rectangle); 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
