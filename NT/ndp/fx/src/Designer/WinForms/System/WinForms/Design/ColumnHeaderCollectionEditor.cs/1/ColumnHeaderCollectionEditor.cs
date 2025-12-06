//------------------------------------------------------------------------------ 
// <copyright file="ImageCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

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
    using System.Diagnostics.CodeAnalysis;
 
    /// <include file='doc\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="ColumnHeaderCollectionEditor"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides an editor for an image collection.</para>
    /// </devdoc> 
    internal class ColumnHeaderCollectionEditor : CollectionEditor {


        /// <include file='doc\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="ColumnHeaderCollectionEditor.ImageCollectionEditor"]/*' /> 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.ImageCollectionEditor'/> class.</para> 
        /// </devdoc> 

        //Called through reflection 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ColumnHeaderCollectionEditor(Type type) : base(type){
        }
 

        /// <include file='doc\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="ColumnHeaderCollectionEditor.HelpTopic"]/*' /> 
        /// <devdoc> 
        ///    <para>Gets the help topic to display for the dialog help button or pressing F1. Override to
        ///          display a different help topic.</para> 
        /// </devdoc>
        protected override string HelpTopic {
            get {
                return "net.ComponentModel.ColumnHeaderCollectionEditor"; 
            }
        } 
 
        /// <include file='doc\\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="\ColumnHeaderCollectionEditor.SetItems"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Sets
        ///       the specified collection to have the specified array of items.
        ///    </para> 
        /// </devdoc>
        protected override object SetItems(object editValue, object[] value) { 
 
            if (editValue != null) {
                Array oldValue = (Array)GetItems(editValue); 
                bool  valueSame = (oldValue.Length == value.Length);
                // We look to see if the value implements IList, and if it does,
                // we set through that.
                // 
                Debug.Assert(editValue is System.Collections.IList, "editValue is not an IList");
                System.Windows.Forms.ListView.ColumnHeaderCollection list = editValue as System.Windows.Forms.ListView.ColumnHeaderCollection; 
                if (editValue != null) { 
                   list.Clear();
 
                   System.Windows.Forms.ColumnHeader[] colHeaders = new System.Windows.Forms.ColumnHeader[value.Length];
                   Array.Copy(value, 0, colHeaders, 0, value.Length);
                   list.AddRange( colHeaders );
                } 
            }
 
            return editValue; 
        }
 
        /// <include file='doc\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="ColumnHeaderCollectionEditor.OnItemRemoving"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Removes the item from listview column header collection 
        ///    </para>
        /// </devdoc> 
        internal override void OnItemRemoving(object item) { 

            ListView listview = this.Context.Instance as ListView; 
            if (listview == null) {
                return;
            }
 
            System.Windows.Forms.ColumnHeader column = item as System.Windows.Forms.ColumnHeader;
 
            if (column != null) { 

               IComponentChangeService cs = GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
               PropertyDescriptor itemsProp = null;
               if (cs != null) {
                   itemsProp = TypeDescriptor.GetProperties(this.Context.Instance)["Columns"];
                   cs.OnComponentChanging(this.Context.Instance, itemsProp); 
               }
               listview.Columns.Remove( column ); 
 
               if (cs != null && itemsProp != null) {
                   cs.OnComponentChanged(this.Context.Instance, itemsProp, null, null); 
               }

            }
 
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
    using System.Diagnostics.CodeAnalysis;
 
    /// <include file='doc\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="ColumnHeaderCollectionEditor"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides an editor for an image collection.</para>
    /// </devdoc> 
    internal class ColumnHeaderCollectionEditor : CollectionEditor {


        /// <include file='doc\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="ColumnHeaderCollectionEditor.ImageCollectionEditor"]/*' /> 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.ImageCollectionEditor'/> class.</para> 
        /// </devdoc> 

        //Called through reflection 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ColumnHeaderCollectionEditor(Type type) : base(type){
        }
 

        /// <include file='doc\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="ColumnHeaderCollectionEditor.HelpTopic"]/*' /> 
        /// <devdoc> 
        ///    <para>Gets the help topic to display for the dialog help button or pressing F1. Override to
        ///          display a different help topic.</para> 
        /// </devdoc>
        protected override string HelpTopic {
            get {
                return "net.ComponentModel.ColumnHeaderCollectionEditor"; 
            }
        } 
 
        /// <include file='doc\\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="\ColumnHeaderCollectionEditor.SetItems"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Sets
        ///       the specified collection to have the specified array of items.
        ///    </para> 
        /// </devdoc>
        protected override object SetItems(object editValue, object[] value) { 
 
            if (editValue != null) {
                Array oldValue = (Array)GetItems(editValue); 
                bool  valueSame = (oldValue.Length == value.Length);
                // We look to see if the value implements IList, and if it does,
                // we set through that.
                // 
                Debug.Assert(editValue is System.Collections.IList, "editValue is not an IList");
                System.Windows.Forms.ListView.ColumnHeaderCollection list = editValue as System.Windows.Forms.ListView.ColumnHeaderCollection; 
                if (editValue != null) { 
                   list.Clear();
 
                   System.Windows.Forms.ColumnHeader[] colHeaders = new System.Windows.Forms.ColumnHeader[value.Length];
                   Array.Copy(value, 0, colHeaders, 0, value.Length);
                   list.AddRange( colHeaders );
                } 
            }
 
            return editValue; 
        }
 
        /// <include file='doc\ColumnHeaderCollectionEditor.uex' path='docs/doc[@for="ColumnHeaderCollectionEditor.OnItemRemoving"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Removes the item from listview column header collection 
        ///    </para>
        /// </devdoc> 
        internal override void OnItemRemoving(object item) { 

            ListView listview = this.Context.Instance as ListView; 
            if (listview == null) {
                return;
            }
 
            System.Windows.Forms.ColumnHeader column = item as System.Windows.Forms.ColumnHeader;
 
            if (column != null) { 

               IComponentChangeService cs = GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
               PropertyDescriptor itemsProp = null;
               if (cs != null) {
                   itemsProp = TypeDescriptor.GetProperties(this.Context.Instance)["Columns"];
                   cs.OnComponentChanging(this.Context.Instance, itemsProp); 
               }
               listview.Columns.Remove( column ); 
 
               if (cs != null && itemsProp != null) {
                   cs.OnComponentChanged(this.Context.Instance, itemsProp, null, null); 
               }

            }
 
        }
 
    } 

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
