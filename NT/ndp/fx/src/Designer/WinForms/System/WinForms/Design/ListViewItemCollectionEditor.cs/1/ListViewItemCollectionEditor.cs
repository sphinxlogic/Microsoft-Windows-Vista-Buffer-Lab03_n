//------------------------------------------------------------------------------ 
// <copyright file="ImageCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ListViewItemCollectionEditor..ctor(System.Type)")] 
 
namespace System.Windows.Forms.Design {
 
    using System.Runtime.InteropServices;

    using System.Diagnostics;
    using System; 
    using System.IO;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Design;
    using System.Drawing.Design;
    using System.Windows.Forms.ComponentModel; 

    /// <include file='doc\ListViewItemCollectionEditor.uex' path='docs/doc[@for="ListViewItemCollectionEditor"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       Provides an editor for an image collection.</para> 
    /// </devdoc>
    internal class ListViewItemCollectionEditor : CollectionEditor {

        /// Since this editor is using the generic form, we 
        /// we need to keep track of newly created items so
        /// to tie them back to the main ListView to have 
        /// access to all persistence properties (e.g., ImageList, etc). 

        /// <include file='doc\ListViewItemCollectionEditor.uex' path='docs/doc[@for="ListViewItemCollectionEditor.ImageCollectionEditor"]/*' /> 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.ImageCollectionEditor'/> class.</para>
        /// </devdoc>
        public ListViewItemCollectionEditor(Type type) : base(type){ 
        }
 
        /// <include file='doc\ListViewItemCollectionEditor.uex' path='docs/doc[@for="ListViewItemCollectionEditor.GetDisplayText"]/*' /> 
        /// <devdoc>
        ///      Retrieves the display text for the given list item. 
        /// </devdoc>
        protected override string GetDisplayText(object value) {
            string text;
 
            if (value == null) {
                return string.Empty; 
            } 

            PropertyDescriptor prop = TypeDescriptor.GetDefaultProperty(CollectionType); 
            if (prop != null && prop.PropertyType == typeof(string)) {
                text = (string)prop.GetValue(value);
                if (text != null && text.Length > 0) {
                    return text; 
                }
            } 
 
            text = TypeDescriptor.GetConverter(value).ConvertToString(value);
 
            if (text == null || text.Length == 0) {
                text = value.GetType().Name;
            }
 
            return text;
        } 
 
    }
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ImageCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ListViewItemCollectionEditor..ctor(System.Type)")] 
 
namespace System.Windows.Forms.Design {
 
    using System.Runtime.InteropServices;

    using System.Diagnostics;
    using System; 
    using System.IO;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Design;
    using System.Drawing.Design;
    using System.Windows.Forms.ComponentModel; 

    /// <include file='doc\ListViewItemCollectionEditor.uex' path='docs/doc[@for="ListViewItemCollectionEditor"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       Provides an editor for an image collection.</para> 
    /// </devdoc>
    internal class ListViewItemCollectionEditor : CollectionEditor {

        /// Since this editor is using the generic form, we 
        /// we need to keep track of newly created items so
        /// to tie them back to the main ListView to have 
        /// access to all persistence properties (e.g., ImageList, etc). 

        /// <include file='doc\ListViewItemCollectionEditor.uex' path='docs/doc[@for="ListViewItemCollectionEditor.ImageCollectionEditor"]/*' /> 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.ImageCollectionEditor'/> class.</para>
        /// </devdoc>
        public ListViewItemCollectionEditor(Type type) : base(type){ 
        }
 
        /// <include file='doc\ListViewItemCollectionEditor.uex' path='docs/doc[@for="ListViewItemCollectionEditor.GetDisplayText"]/*' /> 
        /// <devdoc>
        ///      Retrieves the display text for the given list item. 
        /// </devdoc>
        protected override string GetDisplayText(object value) {
            string text;
 
            if (value == null) {
                return string.Empty; 
            } 

            PropertyDescriptor prop = TypeDescriptor.GetDefaultProperty(CollectionType); 
            if (prop != null && prop.PropertyType == typeof(string)) {
                text = (string)prop.GetValue(value);
                if (text != null && text.Length > 0) {
                    return text; 
                }
            } 
 
            text = TypeDescriptor.GetConverter(value).ConvertToString(value);
 
            if (text == null || text.Length == 0) {
                text = value.GetType().Name;
            }
 
            return text;
        } 
 
    }
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
