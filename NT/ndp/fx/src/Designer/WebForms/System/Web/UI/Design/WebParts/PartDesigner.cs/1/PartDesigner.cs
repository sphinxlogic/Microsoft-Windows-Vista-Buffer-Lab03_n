//------------------------------------------------------------------------------ 
// <copyright file="PartDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.Design; 
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts; 
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public abstract class PartDesigner : CompositeControlDesigner { 
        // Internal to prevent subclassing outside this assembly
        internal PartDesigner() {
        }
 
        protected override bool UsePreviewControl {
            get { 
                return true; 
            }
        } 

        internal static Control GetViewControl(Control control) {
            Debug.Assert(control != null);
 
            ControlDesigner designer = GetDesigner(control);
            if (designer != null) { 
                return designer.ViewControl; 
            }
            else { 
                return control;
            }
        }
 
        private static ControlDesigner GetDesigner(Control control) {
            Debug.Assert(control != null); 
 
            ControlDesigner designer = null;
 
            ISite site = control.Site;
            if (site != null) {
                IDesignerHost host = (IDesignerHost)site.GetService(typeof(IDesignerHost));
                Debug.Assert(host != null, "Did not get a valid IDesignerHost reference"); 

                designer = host.GetDesigner(control) as ControlDesigner; 
            } 

            return designer; 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(Part)); 
            base.Initialize(component);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PartDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.Design; 
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts; 
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public abstract class PartDesigner : CompositeControlDesigner { 
        // Internal to prevent subclassing outside this assembly
        internal PartDesigner() {
        }
 
        protected override bool UsePreviewControl {
            get { 
                return true; 
            }
        } 

        internal static Control GetViewControl(Control control) {
            Debug.Assert(control != null);
 
            ControlDesigner designer = GetDesigner(control);
            if (designer != null) { 
                return designer.ViewControl; 
            }
            else { 
                return control;
            }
        }
 
        private static ControlDesigner GetDesigner(Control control) {
            Debug.Assert(control != null); 
 
            ControlDesigner designer = null;
 
            ISite site = control.Site;
            if (site != null) {
                IDesignerHost host = (IDesignerHost)site.GetService(typeof(IDesignerHost));
                Debug.Assert(host != null, "Did not get a valid IDesignerHost reference"); 

                designer = host.GetDesigner(control) as ControlDesigner; 
            } 

            return designer; 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(Part)); 
            base.Initialize(component);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
