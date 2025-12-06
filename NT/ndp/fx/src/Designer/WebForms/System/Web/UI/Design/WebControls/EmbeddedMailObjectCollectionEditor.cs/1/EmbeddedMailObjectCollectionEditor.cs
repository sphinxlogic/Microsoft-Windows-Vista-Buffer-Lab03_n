//------------------------------------------------------------------------------ 
// <copyright file="EmbeddedMailObjectCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Design;
    using System.Reflection;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Web.UI.WebControls;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class EmbeddedMailObjectCollectionEditor : CollectionEditor {
 
        public EmbeddedMailObjectCollectionEditor(Type type) : base(type) {
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            try {
                context.OnComponentChanging(); 
                return base.EditValue(context, provider, value); 
            }
            finally { 
                context.OnComponentChanged();
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="EmbeddedMailObjectCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Design;
    using System.Reflection;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Web.UI.WebControls;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class EmbeddedMailObjectCollectionEditor : CollectionEditor {
 
        public EmbeddedMailObjectCollectionEditor(Type type) : base(type) {
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            try {
                context.OnComponentChanging(); 
                return base.EditValue(context, provider, value); 
            }
            finally { 
                context.OnComponentChanged();
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
