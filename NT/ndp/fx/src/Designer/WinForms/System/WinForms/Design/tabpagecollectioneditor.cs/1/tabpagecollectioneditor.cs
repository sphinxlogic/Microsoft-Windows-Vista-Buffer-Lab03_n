//------------------------------------------------------------------------------ 
// <copyright file="ToolStripCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TabPageCollectionEditor..ctor()")] 


namespace System.Windows.Forms.Design {
 
    using System;
    using System.Drawing; 
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Data;
    using System.Drawing.Design;
    using System.Diagnostics; 
    using System.Design;
    using System.Windows.Forms.Layout; 
 
    /// <summary>
    /// Main class for collection editor for TabPageCollection.  Allows a single level of ToolStripItem children to be designed. 
    /// </summary>
    internal class TabPageCollectionEditor : CollectionEditor
    {
 
        /// <summary>
        /// Default contstructor. 
        /// </summary> 
        public TabPageCollectionEditor() : base(typeof(TabControl.TabPageCollection)) {
        } 

        /// <include file='doc\TabPageCollectionEditor.uex' path='docs/doc[@for="TabPageCollectionEditor.SetItems"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Sets
        ///       the specified collection to have the specified array of items. 
        ///    </para> 
        /// </devdoc>
        protected override object SetItems(object editValue, object[] value) { 

            TabControl tc = this.Context.Instance as TabControl;
            if (tc != null) {
                tc.SuspendLayout(); 
            }
            // Set the UseVisualStyleBackColor for all the tabPages added through the collectionEditor. 
            foreach (object tab in value) 
            {
                TabPage page = tab as TabPage; 
                if (page != null)
                {
                    PropertyDescriptor styleProp = TypeDescriptor.GetProperties(page)["UseVisualStyleBackColor"];
                    if (styleProp != null && styleProp.PropertyType == typeof(bool) && !styleProp.IsReadOnly && styleProp.IsBrowsable) 
                    {
                        styleProp.SetValue(page, true); 
                    } 
                }
            } 
            object retValue = base.SetItems(editValue, value);

            if (tc != null) {
                tc.ResumeLayout(); 
            }
 
 
            return retValue;
        } 

    }
}
 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TabPageCollectionEditor..ctor()")] 


namespace System.Windows.Forms.Design {
 
    using System;
    using System.Drawing; 
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Data;
    using System.Drawing.Design;
    using System.Diagnostics; 
    using System.Design;
    using System.Windows.Forms.Layout; 
 
    /// <summary>
    /// Main class for collection editor for TabPageCollection.  Allows a single level of ToolStripItem children to be designed. 
    /// </summary>
    internal class TabPageCollectionEditor : CollectionEditor
    {
 
        /// <summary>
        /// Default contstructor. 
        /// </summary> 
        public TabPageCollectionEditor() : base(typeof(TabControl.TabPageCollection)) {
        } 

        /// <include file='doc\TabPageCollectionEditor.uex' path='docs/doc[@for="TabPageCollectionEditor.SetItems"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Sets
        ///       the specified collection to have the specified array of items. 
        ///    </para> 
        /// </devdoc>
        protected override object SetItems(object editValue, object[] value) { 

            TabControl tc = this.Context.Instance as TabControl;
            if (tc != null) {
                tc.SuspendLayout(); 
            }
            // Set the UseVisualStyleBackColor for all the tabPages added through the collectionEditor. 
            foreach (object tab in value) 
            {
                TabPage page = tab as TabPage; 
                if (page != null)
                {
                    PropertyDescriptor styleProp = TypeDescriptor.GetProperties(page)["UseVisualStyleBackColor"];
                    if (styleProp != null && styleProp.PropertyType == typeof(bool) && !styleProp.IsReadOnly && styleProp.IsBrowsable) 
                    {
                        styleProp.SetValue(page, true); 
                    } 
                }
            } 
            object retValue = base.SetItems(editValue, value);

            if (tc != null) {
                tc.ResumeLayout(); 
            }
 
 
            return retValue;
        } 

    }
}
 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
