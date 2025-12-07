//------------------------------------------------------------------------------ 
// <copyright file="StyleCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Reflection;

    /// <include file='doc\StyleCollectionEditor.uex' path='docs/doc[@for="StyleCollectionEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides an editor to edit styles collections.</para> 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class StyleCollectionEditor : CollectionEditor { 

        /// <include file='doc\StyleCollectionEditor.uex' path='docs/doc[@for="StyleCollectionEditor.StyleCollectionEditor"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.Design.WebControls.StyleCollectionEditor'/> class.</para> 
        /// </devdoc>
        public StyleCollectionEditor(Type type) : base(type) { 
        } 

        /// <include file='doc\StyleCollectionEditor.uex' path='docs/doc[@for="StyleCollectionEditor.CreateInstance"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override object CreateInstance(Type itemType) { 
            return Activator.CreateInstance(itemType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, null, null);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="StyleCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Reflection;

    /// <include file='doc\StyleCollectionEditor.uex' path='docs/doc[@for="StyleCollectionEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides an editor to edit styles collections.</para> 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class StyleCollectionEditor : CollectionEditor { 

        /// <include file='doc\StyleCollectionEditor.uex' path='docs/doc[@for="StyleCollectionEditor.StyleCollectionEditor"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.Design.WebControls.StyleCollectionEditor'/> class.</para> 
        /// </devdoc>
        public StyleCollectionEditor(Type type) : base(type) { 
        } 

        /// <include file='doc\StyleCollectionEditor.uex' path='docs/doc[@for="StyleCollectionEditor.CreateInstance"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override object CreateInstance(Type itemType) { 
            return Activator.CreateInstance(itemType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, null, null);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
