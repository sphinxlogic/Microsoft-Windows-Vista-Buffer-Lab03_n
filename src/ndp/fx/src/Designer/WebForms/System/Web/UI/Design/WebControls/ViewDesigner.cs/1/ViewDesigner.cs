//------------------------------------------------------------------------------ 
// <copyright file="ViewDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\ViewDesigner.uex' path='docs/doc[@for="ViewDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.View'/>
    ///       web control. 
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ViewDesigner : ContainerControlDesigner { 

        public ViewDesigner() { 
            FrameStyleInternal.Width = Unit.Percentage(100); 
        }
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(View));
            base.Initialize(component);
        } 

        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            return GetDesignTimeHtmlHelper(true, regions); 
        }
 
        public override string GetDesignTimeHtml() {
            return GetDesignTimeHtmlHelper(false, null);
        }
 
        private string GetDesignTimeHtmlHelper(bool useRegions, DesignerRegionCollection regions) {
            View view = (View)Component; 
            if (!(view.Parent is MultiView)) { 
                return CreateInvalidParentDesignTimeHtml(typeof(View), typeof(MultiView));
            } 

            if (useRegions) {
                return base.GetDesignTimeHtml(regions);
            } 
            else {
                return base.GetDesignTimeHtml(); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ViewDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\ViewDesigner.uex' path='docs/doc[@for="ViewDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.View'/>
    ///       web control. 
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ViewDesigner : ContainerControlDesigner { 

        public ViewDesigner() { 
            FrameStyleInternal.Width = Unit.Percentage(100); 
        }
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(View));
            base.Initialize(component);
        } 

        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            return GetDesignTimeHtmlHelper(true, regions); 
        }
 
        public override string GetDesignTimeHtml() {
            return GetDesignTimeHtmlHelper(false, null);
        }
 
        private string GetDesignTimeHtmlHelper(bool useRegions, DesignerRegionCollection regions) {
            View view = (View)Component; 
            if (!(view.Parent is MultiView)) { 
                return CreateInvalidParentDesignTimeHtml(typeof(View), typeof(MultiView));
            } 

            if (useRegions) {
                return base.GetDesignTimeHtml(regions);
            } 
            else {
                return base.GetDesignTimeHtml(); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
