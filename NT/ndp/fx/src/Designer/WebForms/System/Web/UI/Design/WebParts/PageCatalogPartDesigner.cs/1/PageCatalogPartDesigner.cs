//------------------------------------------------------------------------------ 
// <copyright file="PageCatalogPartDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class PageCatalogPartDesigner : CatalogPartDesigner { 
        private PageCatalogPart _catalogPart; 

        // PageCatalogPart has no default runtime rendering, so GetDesignTimeHtml() should also 
        // return String.Empty, so we don't get the '[Type "ID"]' rendered in the designer.
        public override string GetDesignTimeHtml() {
            if (!(_catalogPart.Parent is CatalogZoneBase)) {
                return CreateInvalidParentDesignTimeHtml(typeof(CatalogPart), typeof(CatalogZoneBase)); 
            }
 
            return String.Empty; 
        }
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(PageCatalogPart));
            _catalogPart = (PageCatalogPart)component;
            base.Initialize(component); 
        }
    } 
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PageCatalogPartDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class PageCatalogPartDesigner : CatalogPartDesigner { 
        private PageCatalogPart _catalogPart; 

        // PageCatalogPart has no default runtime rendering, so GetDesignTimeHtml() should also 
        // return String.Empty, so we don't get the '[Type "ID"]' rendered in the designer.
        public override string GetDesignTimeHtml() {
            if (!(_catalogPart.Parent is CatalogZoneBase)) {
                return CreateInvalidParentDesignTimeHtml(typeof(CatalogPart), typeof(CatalogZoneBase)); 
            }
 
            return String.Empty; 
        }
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(PageCatalogPart));
            _catalogPart = (PageCatalogPart)component;
            base.Initialize(component); 
        }
    } 
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
