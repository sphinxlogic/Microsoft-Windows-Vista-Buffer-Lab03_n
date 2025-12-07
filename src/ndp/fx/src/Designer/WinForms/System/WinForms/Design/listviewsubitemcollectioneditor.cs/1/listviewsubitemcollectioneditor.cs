//------------------------------------------------------------------------------ 
// <copyright file="ListViewSubItemCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ListViewSubItemCollectionEditor..ctor(System.Type)")] 
 
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
    internal class ListViewSubItemCollectionEditor : CollectionEditor {

        private static int count = 0; 
        ListViewItem.ListViewSubItem firstSubItem = null;
        /// <include file='doc\ListViewSubItemCollectionEditor.uex' path='docs/doc[@for="ListViewSubItemCollectionEditor.ListViewSubItemCollectionEditor"]/*' /> 
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.ListViewSubItemCollectionEditor'/> class.</para>
        /// </devdoc> 
        public ListViewSubItemCollectionEditor(Type type) : base(type){
        }

        /// <include file='doc\ListViewSubItemCollectionEditor.uex' path='docs/doc[@for="ListViewSubItemCollectionEditor.CreateInstance"]/*' /> 
        /// <devdoc>
        ///    <para>Creates an instance of the specified type in the collection.</para> 
        /// </devdoc> 
        // VSWhidbey 122909: Set the Name property in the item's properties.
        protected override object CreateInstance(Type type) { 

            object instance = base.CreateInstance(type);

            // slap in a default site-like name 
            if (instance is ListViewItem.ListViewSubItem) {
                ((ListViewItem.ListViewSubItem)instance).Name = SR.GetString(SR.ListViewSubItemBaseName) + count++; 
            } 
            return instance;
        } 

        /// <include file='doc\ListViewItemCollectionEditor.uex' path='docs/doc[@for="ListViewItemCollectionEditor.GetDisplayText"]/*' />
        /// <devdoc>
        ///      Retrieves the display text for the given list sub item. 
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

        /// <include file='doc\ListViewSubItemCollectionEditor.uex' path='docs/doc[@for="ListViewSubItemCollectionEditor.GetItems"]/*' />
        protected override object[] GetItems(object editValue) { 
            // take the fist sub item out of the collection
            ListViewItem.ListViewSubItemCollection subItemsColl = (ListViewItem.ListViewSubItemCollection) editValue; 
 
            // add all the other sub items
            object[] values = new object[subItemsColl.Count]; 
            ((ICollection)subItemsColl).CopyTo(values, 0);

            if (values.Length > 0) {
 
                // save the first sub item
                firstSubItem =  subItemsColl[0]; 
 
                // now return the rest.
                // 
                object[] subValues = new object[values.Length - 1];
                Array.Copy(values, 1, subValues, 0, subValues.Length);
                values = subValues;
            } 

            return values; 
        } 

        /// <include file='doc\ListViewSubItemCollectionEditor.uex' path='docs/doc[@for="ListViewSubItemCollectionEditor.SetItems"]/*' /> 
        protected override object SetItems(object editValue, object[] value) {
            IList list = editValue as IList;
            list.Clear();
 
            list.Add(firstSubItem);
 
            for (int i = 0; i < value.Length; i ++) { 
                list.Add(value[i]);
            } 

            return editValue;
        }
    } 

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ListViewSubItemCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ListViewSubItemCollectionEditor..ctor(System.Type)")] 
 
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
    internal class ListViewSubItemCollectionEditor : CollectionEditor {

        private static int count = 0; 
        ListViewItem.ListViewSubItem firstSubItem = null;
        /// <include file='doc\ListViewSubItemCollectionEditor.uex' path='docs/doc[@for="ListViewSubItemCollectionEditor.ListViewSubItemCollectionEditor"]/*' /> 
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.ListViewSubItemCollectionEditor'/> class.</para>
        /// </devdoc> 
        public ListViewSubItemCollectionEditor(Type type) : base(type){
        }

        /// <include file='doc\ListViewSubItemCollectionEditor.uex' path='docs/doc[@for="ListViewSubItemCollectionEditor.CreateInstance"]/*' /> 
        /// <devdoc>
        ///    <para>Creates an instance of the specified type in the collection.</para> 
        /// </devdoc> 
        // VSWhidbey 122909: Set the Name property in the item's properties.
        protected override object CreateInstance(Type type) { 

            object instance = base.CreateInstance(type);

            // slap in a default site-like name 
            if (instance is ListViewItem.ListViewSubItem) {
                ((ListViewItem.ListViewSubItem)instance).Name = SR.GetString(SR.ListViewSubItemBaseName) + count++; 
            } 
            return instance;
        } 

        /// <include file='doc\ListViewItemCollectionEditor.uex' path='docs/doc[@for="ListViewItemCollectionEditor.GetDisplayText"]/*' />
        /// <devdoc>
        ///      Retrieves the display text for the given list sub item. 
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

        /// <include file='doc\ListViewSubItemCollectionEditor.uex' path='docs/doc[@for="ListViewSubItemCollectionEditor.GetItems"]/*' />
        protected override object[] GetItems(object editValue) { 
            // take the fist sub item out of the collection
            ListViewItem.ListViewSubItemCollection subItemsColl = (ListViewItem.ListViewSubItemCollection) editValue; 
 
            // add all the other sub items
            object[] values = new object[subItemsColl.Count]; 
            ((ICollection)subItemsColl).CopyTo(values, 0);

            if (values.Length > 0) {
 
                // save the first sub item
                firstSubItem =  subItemsColl[0]; 
 
                // now return the rest.
                // 
                object[] subValues = new object[values.Length - 1];
                Array.Copy(values, 1, subValues, 0, subValues.Length);
                values = subValues;
            } 

            return values; 
        } 

        /// <include file='doc\ListViewSubItemCollectionEditor.uex' path='docs/doc[@for="ListViewSubItemCollectionEditor.SetItems"]/*' /> 
        protected override object SetItems(object editValue, object[] value) {
            IList list = editValue as IList;
            list.Clear();
 
            list.Add(firstSubItem);
 
            for (int i = 0; i < value.Length; i ++) { 
                list.Add(value[i]);
            } 

            return editValue;
        }
    } 

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
