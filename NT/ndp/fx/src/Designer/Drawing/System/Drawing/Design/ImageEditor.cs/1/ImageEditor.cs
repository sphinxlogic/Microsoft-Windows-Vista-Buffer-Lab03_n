//------------------------------------------------------------------------------ 
// <copyright file="ImageEditor.cs" company="Microsoft">
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
    using System.Reflection;
    using System.Runtime.InteropServices; 
    using System.Windows.Forms; 
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design; 

    /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor"]/*' />
    /// <devdoc>
    ///    <para>Provides an editor for visually picking an image.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ImageEditor : UITypeEditor { 

        internal static Type[] imageExtenders = new Type[] { typeof(BitmapEditor), /*gpr typeof(Icon),*/ typeof(MetafileEditor)}; 
        internal FileDialog fileDialog;

        // VSWhidbey 95227: accessor needed into the static field so that derived classes
        // can implement a different list of supported image types. 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        protected virtual Type[] GetImageExtenders() { 
                return imageExtenders; 
        }
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.CreateExtensionsString"]/*' />
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
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.CreateFilterEntry"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] // previously shipped this way. Would be a breaking change.
        protected static string CreateFilterEntry(ImageEditor e)
        { 
            string desc = e.GetFileDialogDescription();
            string exts = CreateExtensionsString(e.GetExtensions(),","); 
            string extsSemis = CreateExtensionsString(e.GetExtensions(),";"); 
            return desc + "(" + exts + ")|" + extsSemis;
        } 

        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.EditValue"]/*' />
        /// <devdoc>
        ///      Edits the given object value using the editor style provided by 
        ///      GetEditorStyle.  A service provider is provided so that any
        ///      required editing services can be obtained. 
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1818:DoNotConcatenateStringsInsideLoops")]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
 
                if (edSvc != null) {
                    if (fileDialog == null) { 
                        fileDialog = new OpenFileDialog(); 
                        string filter = CreateFilterEntry(this);
                        for (int i = 0; i < GetImageExtenders().Length; i++) { 
                            ImageEditor e = (ImageEditor) Activator.CreateInstance(GetImageExtenders()[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, null, null); //.CreateInstance();
                            Type myClass = this.GetType();
                            Type editorClass = e.GetType();
                            if (!myClass.Equals(editorClass) && e != null && myClass.IsInstanceOfType(e)) 
                                filter += "|" + CreateFilterEntry(e);
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

        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.GetEditStyle"]/*' /> 
        /// <devdoc>
        ///      Retrieves the editing style of the Edit method.  If the method
        ///      is not supported, this will return None.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal; 
        }
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.GetFileDialogDescription"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected virtual string GetFileDialogDescription() {
            return SR.GetString(SR.imageFileDescription); 
        } 

        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.GetExtensions"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        protected virtual string[] GetExtensions() {
            // We should probably smash them together... 
            ArrayList list = new ArrayList(); 
            for (int i = 0; i < GetImageExtenders().Length; i++) {
                ImageEditor e = (ImageEditor) Activator.CreateInstance(GetImageExtenders()[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, null, null);   //.CreateInstance(); 
                if (!e.GetType().Equals(typeof(ImageEditor)))
                    list.AddRange(new ArrayList(e.GetExtensions()));
            }
            return(string[]) list.ToArray(typeof(string)); 
        }
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.GetPaintValueSupported"]/*' /> 
        /// <devdoc>
        ///      Determines if this editor supports the painting of a representation 
        ///      of an object's value.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override bool GetPaintValueSupported(ITypeDescriptorContext context) { 
            return true;
        } 
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.LoadFromStream"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        protected virtual Image LoadFromStream(Stream stream) { 
            //Copy the original stream to a buffer, then wrap a
            //memory stream around it.  This way we can avoid 
            //locking the file 
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length); 
            MemoryStream ms = new MemoryStream(buffer);

            return Image.FromStream(ms);
        } 

        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.PaintValue"]/*' /> 
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
            Image image = e.Value as Image;
            if (image != null) { 
                Rectangle r = e.Bounds;
                r.Width --;
                r.Height--;
                e.Graphics.DrawRectangle(SystemPens.WindowFrame, r); 
                e.Graphics.DrawImage(image, e.Bounds);
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ImageEditor.cs" company="Microsoft">
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
    using System.Reflection;
    using System.Runtime.InteropServices; 
    using System.Windows.Forms; 
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design; 

    /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor"]/*' />
    /// <devdoc>
    ///    <para>Provides an editor for visually picking an image.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ImageEditor : UITypeEditor { 

        internal static Type[] imageExtenders = new Type[] { typeof(BitmapEditor), /*gpr typeof(Icon),*/ typeof(MetafileEditor)}; 
        internal FileDialog fileDialog;

        // VSWhidbey 95227: accessor needed into the static field so that derived classes
        // can implement a different list of supported image types. 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        protected virtual Type[] GetImageExtenders() { 
                return imageExtenders; 
        }
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.CreateExtensionsString"]/*' />
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
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.CreateFilterEntry"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] // previously shipped this way. Would be a breaking change.
        protected static string CreateFilterEntry(ImageEditor e)
        { 
            string desc = e.GetFileDialogDescription();
            string exts = CreateExtensionsString(e.GetExtensions(),","); 
            string extsSemis = CreateExtensionsString(e.GetExtensions(),";"); 
            return desc + "(" + exts + ")|" + extsSemis;
        } 

        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.EditValue"]/*' />
        /// <devdoc>
        ///      Edits the given object value using the editor style provided by 
        ///      GetEditorStyle.  A service provider is provided so that any
        ///      required editing services can be obtained. 
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1818:DoNotConcatenateStringsInsideLoops")]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
 
                if (edSvc != null) {
                    if (fileDialog == null) { 
                        fileDialog = new OpenFileDialog(); 
                        string filter = CreateFilterEntry(this);
                        for (int i = 0; i < GetImageExtenders().Length; i++) { 
                            ImageEditor e = (ImageEditor) Activator.CreateInstance(GetImageExtenders()[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, null, null); //.CreateInstance();
                            Type myClass = this.GetType();
                            Type editorClass = e.GetType();
                            if (!myClass.Equals(editorClass) && e != null && myClass.IsInstanceOfType(e)) 
                                filter += "|" + CreateFilterEntry(e);
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

        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.GetEditStyle"]/*' /> 
        /// <devdoc>
        ///      Retrieves the editing style of the Edit method.  If the method
        ///      is not supported, this will return None.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal; 
        }
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.GetFileDialogDescription"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected virtual string GetFileDialogDescription() {
            return SR.GetString(SR.imageFileDescription); 
        } 

        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.GetExtensions"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        protected virtual string[] GetExtensions() {
            // We should probably smash them together... 
            ArrayList list = new ArrayList(); 
            for (int i = 0; i < GetImageExtenders().Length; i++) {
                ImageEditor e = (ImageEditor) Activator.CreateInstance(GetImageExtenders()[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, null, null);   //.CreateInstance(); 
                if (!e.GetType().Equals(typeof(ImageEditor)))
                    list.AddRange(new ArrayList(e.GetExtensions()));
            }
            return(string[]) list.ToArray(typeof(string)); 
        }
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.GetPaintValueSupported"]/*' /> 
        /// <devdoc>
        ///      Determines if this editor supports the painting of a representation 
        ///      of an object's value.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override bool GetPaintValueSupported(ITypeDescriptorContext context) { 
            return true;
        } 
 
        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.LoadFromStream"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        protected virtual Image LoadFromStream(Stream stream) { 
            //Copy the original stream to a buffer, then wrap a
            //memory stream around it.  This way we can avoid 
            //locking the file 
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length); 
            MemoryStream ms = new MemoryStream(buffer);

            return Image.FromStream(ms);
        } 

        /// <include file='doc\ImageEditor.uex' path='docs/doc[@for="ImageEditor.PaintValue"]/*' /> 
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
            Image image = e.Value as Image;
            if (image != null) { 
                Rectangle r = e.Bounds;
                r.Width --;
                r.Height--;
                e.Graphics.DrawRectangle(SystemPens.WindowFrame, r); 
                e.Graphics.DrawImage(image, e.Bounds);
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
