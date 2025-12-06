//------------------------------------------------------------------------------ 
// <copyright file="CatalogPartDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Design;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls.WebParts; 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class EditorPartDesigner : PartDesigner { 
        private EditorPart _editorPart;
 
        protected override Control CreateViewControl() {
            Control viewControl = base.CreateViewControl();

            // Copy DesignModeState from the Component to the ViewControl, so that 
            // the Zone is set on the ViewControl. (VSWhidbey 456878)
            IDictionary state = ((IControlDesignerAccessor)_editorPart).GetDesignModeState(); 
            ((IControlDesignerAccessor)viewControl).SetDesignModeState(state); 

            return viewControl; 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(EditorPart)); 
            _editorPart = (EditorPart)component;
            base.Initialize(component); 
        } 

        public override string GetDesignTimeHtml() { 
            if (!(_editorPart.Parent is EditorZoneBase)) {
                return CreateInvalidParentDesignTimeHtml(typeof(EditorPart), typeof(EditorZoneBase));
            }
 
            return base.GetDesignTimeHtml();
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="CatalogPartDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Design;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls.WebParts; 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class EditorPartDesigner : PartDesigner { 
        private EditorPart _editorPart;
 
        protected override Control CreateViewControl() {
            Control viewControl = base.CreateViewControl();

            // Copy DesignModeState from the Component to the ViewControl, so that 
            // the Zone is set on the ViewControl. (VSWhidbey 456878)
            IDictionary state = ((IControlDesignerAccessor)_editorPart).GetDesignModeState(); 
            ((IControlDesignerAccessor)viewControl).SetDesignModeState(state); 

            return viewControl; 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(EditorPart)); 
            _editorPart = (EditorPart)component;
            base.Initialize(component); 
        } 

        public override string GetDesignTimeHtml() { 
            if (!(_editorPart.Parent is EditorZoneBase)) {
                return CreateInvalidParentDesignTimeHtml(typeof(EditorPart), typeof(EditorZoneBase));
            }
 
            return base.GetDesignTimeHtml();
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
