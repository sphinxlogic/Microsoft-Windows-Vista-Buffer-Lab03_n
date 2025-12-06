//------------------------------------------------------------------------------ 
// <copyright file="ImageCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ListViewGroupCollectionEditor..ctor(System.Type)")] 
 
namespace System.Windows.Forms.Design {
 
    using System.Runtime.InteropServices;

    using System.Diagnostics;
    using System; 
    using System.IO;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Windows.Forms;
    using System.Drawing;
    using System.Design;
    using System.Drawing.Design; 
    using System.Windows.Forms.ComponentModel;
 
    /// <include file='doc\ListViewGroupCollectionEditor.uex' path='docs/doc[@for="ListViewGroupCollectionEditor"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides an editor for an image collection.</para>
    /// </devdoc>
    internal class ListViewGroupCollectionEditor : CollectionEditor {
 
        object editValue;
        /// <include file='doc\ListViewGroupCollectionEditor.uex' path='docs/doc[@for="ListViewGroupCollectionEditor.ListViewGroupCollectionEditor"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public ListViewGroupCollectionEditor(Type type) : base(type){ 
        }

        /// <include file='doc\ListViewGroupCollectionEditor.uex' path='docs/doc[@for="ListViewGroupCollectionEditor.CreatesInstance"]/*' />
        /// <devdoc> 
        ///      Creates a ListViewGroup instance.
        /// </devdoc> 
        protected override object CreateInstance(Type itemType) { 
            ListViewGroup lvg = (ListViewGroup) base.CreateInstance(itemType);
 
            // Create an unique name for the list view group.
            lvg.Name = CreateListViewGroupName((ListViewGroupCollection) this.editValue);

            return lvg; 
        }
 
        private string CreateListViewGroupName(ListViewGroupCollection lvgCollection) { 
            string lvgName = "ListViewGroup";
            string resultName; 
            INameCreationService ncs = this.GetService(typeof(INameCreationService)) as INameCreationService;
            IContainer container = this.GetService(typeof(IContainer)) as IContainer;

            if (ncs != null && container != null) { 
                lvgName = ncs.CreateName(container, typeof(ListViewGroup));
            } 
 
            // strip the digits from the end.
            while (Char.IsDigit(lvgName[lvgName.Length - 1])) { 
                lvgName = lvgName.Substring(0, lvgName.Length - 1);
            }

            int i = 1; 
            resultName = lvgName + i.ToString(System.Globalization.CultureInfo.CurrentCulture);
            while (lvgCollection[resultName] != null) { 
                i ++; 
                resultName = lvgName + i.ToString(System.Globalization.CultureInfo.CurrentCulture);
            } 

            return resultName;
        }
 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            this.editValue = value; 
            object ret; 

            // This will block while the ListViewGroupCollectionDialog is running. 
            ret = base.EditValue(context, provider, value);

            // The user is done w/ the ListViewGroupCollectionDialog.
            // Don't need the edit value any longer 
            this.editValue = null;
 
            return ret; 
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

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ListViewGroupCollectionEditor..ctor(System.Type)")] 
 
namespace System.Windows.Forms.Design {
 
    using System.Runtime.InteropServices;

    using System.Diagnostics;
    using System; 
    using System.IO;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Windows.Forms;
    using System.Drawing;
    using System.Design;
    using System.Drawing.Design; 
    using System.Windows.Forms.ComponentModel;
 
    /// <include file='doc\ListViewGroupCollectionEditor.uex' path='docs/doc[@for="ListViewGroupCollectionEditor"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides an editor for an image collection.</para>
    /// </devdoc>
    internal class ListViewGroupCollectionEditor : CollectionEditor {
 
        object editValue;
        /// <include file='doc\ListViewGroupCollectionEditor.uex' path='docs/doc[@for="ListViewGroupCollectionEditor.ListViewGroupCollectionEditor"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public ListViewGroupCollectionEditor(Type type) : base(type){ 
        }

        /// <include file='doc\ListViewGroupCollectionEditor.uex' path='docs/doc[@for="ListViewGroupCollectionEditor.CreatesInstance"]/*' />
        /// <devdoc> 
        ///      Creates a ListViewGroup instance.
        /// </devdoc> 
        protected override object CreateInstance(Type itemType) { 
            ListViewGroup lvg = (ListViewGroup) base.CreateInstance(itemType);
 
            // Create an unique name for the list view group.
            lvg.Name = CreateListViewGroupName((ListViewGroupCollection) this.editValue);

            return lvg; 
        }
 
        private string CreateListViewGroupName(ListViewGroupCollection lvgCollection) { 
            string lvgName = "ListViewGroup";
            string resultName; 
            INameCreationService ncs = this.GetService(typeof(INameCreationService)) as INameCreationService;
            IContainer container = this.GetService(typeof(IContainer)) as IContainer;

            if (ncs != null && container != null) { 
                lvgName = ncs.CreateName(container, typeof(ListViewGroup));
            } 
 
            // strip the digits from the end.
            while (Char.IsDigit(lvgName[lvgName.Length - 1])) { 
                lvgName = lvgName.Substring(0, lvgName.Length - 1);
            }

            int i = 1; 
            resultName = lvgName + i.ToString(System.Globalization.CultureInfo.CurrentCulture);
            while (lvgCollection[resultName] != null) { 
                i ++; 
                resultName = lvgName + i.ToString(System.Globalization.CultureInfo.CurrentCulture);
            } 

            return resultName;
        }
 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            this.editValue = value; 
            object ret; 

            // This will block while the ListViewGroupCollectionDialog is running. 
            ret = base.EditValue(context, provider, value);

            // The user is done w/ the ListViewGroupCollectionDialog.
            // Don't need the edit value any longer 
            this.editValue = null;
 
            return ret; 
        }
    } 

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
