//------------------------------------------------------------------------------ 
// <copyright file="CompositeControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Design;
    using System.Web.UI;
    using System.Web.UI.WebControls; 

    /// <include file='doc\CompositeControlDesigner.uex' path='docs/doc[@for="CompositeControlDesigner"]/*' /> 
    /// <devdoc> 
    /// Base designer class, useful for any type of composite control, not only those that extend
    /// System.Web.UI.WebControls.CompositeControl. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class CompositeControlDesigner : ControlDesigner {
 
        /// <include file='doc\CompositeControlDesigner.uex' path='docs/doc[@for="CompositeControlDesigner.CreateChildControls"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        protected virtual void CreateChildControls() {
            ICompositeControlDesignerAccessor ccda = (ICompositeControlDesignerAccessor)ViewControl; 
            ccda.RecreateChildControls();
        }

        /// <include file='doc\CompositeControlDesigner.uex' path='docs/doc[@for="CompositeControlDesigner.GetDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public override string GetDesignTimeHtml() { 
            CreateChildControls();
 
            return base.GetDesignTimeHtml();
        }

        /// <include file='doc\CompositeControlDesigner.uex' path='docs/doc[@for="CompositeControlDesigner.Initialize"]/*' /> 
        /// <devdoc>
        /// Since this designer may be used with any type of composite control, only enforce that the control is an INamingContainer. 
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(INamingContainer)); 
            base.Initialize(component);
        }
    }
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="CompositeControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Design;
    using System.Web.UI;
    using System.Web.UI.WebControls; 

    /// <include file='doc\CompositeControlDesigner.uex' path='docs/doc[@for="CompositeControlDesigner"]/*' /> 
    /// <devdoc> 
    /// Base designer class, useful for any type of composite control, not only those that extend
    /// System.Web.UI.WebControls.CompositeControl. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class CompositeControlDesigner : ControlDesigner {
 
        /// <include file='doc\CompositeControlDesigner.uex' path='docs/doc[@for="CompositeControlDesigner.CreateChildControls"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        protected virtual void CreateChildControls() {
            ICompositeControlDesignerAccessor ccda = (ICompositeControlDesignerAccessor)ViewControl; 
            ccda.RecreateChildControls();
        }

        /// <include file='doc\CompositeControlDesigner.uex' path='docs/doc[@for="CompositeControlDesigner.GetDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public override string GetDesignTimeHtml() { 
            CreateChildControls();
 
            return base.GetDesignTimeHtml();
        }

        /// <include file='doc\CompositeControlDesigner.uex' path='docs/doc[@for="CompositeControlDesigner.Initialize"]/*' /> 
        /// <devdoc>
        /// Since this designer may be used with any type of composite control, only enforce that the control is an INamingContainer. 
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(INamingContainer)); 
            base.Initialize(component);
        }
    }
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
