//------------------------------------------------------------------------------ 
// <copyright file="TreeNodeStyleCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Reflection;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\TreeNodeStyleCollectionEditor.uex' path='docs/doc[@for="TreeNodeStyleCollectionEditor"]/*' />
    /// <devdoc> 
    ///    <para>Provides an editor to edit rows in a table.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class TreeNodeStyleCollectionEditor : StyleCollectionEditor {

        /// <include file='doc\TreeNodeStyleCollectionEditor.uex' path='docs/doc[@for="TreeNodeStyleCollectionEditor.TreeNodeStyleCollectionEditor"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.Design.WebControls.TreeNodeStyleCollectionEditor'/> class.</para>
        /// </devdoc> 
        public TreeNodeStyleCollectionEditor(Type type) : base(type) { 
        }
 
        /// <include file='doc\TreeNodeStyleCollectionEditor.uex' path='docs/doc[@for="TreeNodeStyleCollectionEditor.CreateInstance"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected override Type CreateCollectionItemType() {
            return typeof(TreeNodeStyle); 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TreeNodeStyleCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Reflection;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\TreeNodeStyleCollectionEditor.uex' path='docs/doc[@for="TreeNodeStyleCollectionEditor"]/*' />
    /// <devdoc> 
    ///    <para>Provides an editor to edit rows in a table.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class TreeNodeStyleCollectionEditor : StyleCollectionEditor {

        /// <include file='doc\TreeNodeStyleCollectionEditor.uex' path='docs/doc[@for="TreeNodeStyleCollectionEditor.TreeNodeStyleCollectionEditor"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.Design.WebControls.TreeNodeStyleCollectionEditor'/> class.</para>
        /// </devdoc> 
        public TreeNodeStyleCollectionEditor(Type type) : base(type) { 
        }
 
        /// <include file='doc\TreeNodeStyleCollectionEditor.uex' path='docs/doc[@for="TreeNodeStyleCollectionEditor.CreateInstance"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected override Type CreateCollectionItemType() {
            return typeof(TreeNodeStyle); 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
