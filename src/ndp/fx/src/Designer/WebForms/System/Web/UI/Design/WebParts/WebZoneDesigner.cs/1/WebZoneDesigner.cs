//------------------------------------------------------------------------------ 
// <copyright file="ZoneDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
 
    // Note that we derive from ControlDesigner instead of CompositeControlDesigner, even though WebZone
    // derives from CompositeControl.  The issue is that CompositeControlDesigner forces the child 
    // controls to be created before calling base.GetDesignTimeHtml().  However, we do not want to force 
    // this child controls to be created, since we explicitly clear the Controls collection before
    // calling base.GetDesignTimeHtml().  We do this because if a single WebPart has an error, we want to 
    // render an exception for just that WebPart, not the whole WebPartZone.
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public abstract class WebZoneDesigner : ControlDesigner {
        internal const string _templateName = "ZoneTemplate"; 

        // Internal to prevent subclassing outside this assembly 
        internal WebZoneDesigner() { 
        }
 
        internal TemplateDefinition TemplateDefinition {
            get {
                return new TemplateDefinition(this, _templateName, Component, _templateName, ((WebControl)ViewControl).ControlStyle, true);
            } 
        }
 
        internal TemplateGroup CreateZoneTemplateGroup() { 
            TemplateGroup zoneTemplateGroup = new TemplateGroup(_templateName, ((WebControl)ViewControl).ControlStyle);
            zoneTemplateGroup.AddTemplateDefinition(new TemplateDefinition(this, _templateName, Component, _templateName, ((WebControl)ViewControl).ControlStyle)); 
            return zoneTemplateGroup;
        }

        protected override bool UsePreviewControl { 
            get {
                return true; 
            } 
        }
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(WebZone));
            base.Initialize(component);
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ZoneDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
 
    // Note that we derive from ControlDesigner instead of CompositeControlDesigner, even though WebZone
    // derives from CompositeControl.  The issue is that CompositeControlDesigner forces the child 
    // controls to be created before calling base.GetDesignTimeHtml().  However, we do not want to force 
    // this child controls to be created, since we explicitly clear the Controls collection before
    // calling base.GetDesignTimeHtml().  We do this because if a single WebPart has an error, we want to 
    // render an exception for just that WebPart, not the whole WebPartZone.
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public abstract class WebZoneDesigner : ControlDesigner {
        internal const string _templateName = "ZoneTemplate"; 

        // Internal to prevent subclassing outside this assembly 
        internal WebZoneDesigner() { 
        }
 
        internal TemplateDefinition TemplateDefinition {
            get {
                return new TemplateDefinition(this, _templateName, Component, _templateName, ((WebControl)ViewControl).ControlStyle, true);
            } 
        }
 
        internal TemplateGroup CreateZoneTemplateGroup() { 
            TemplateGroup zoneTemplateGroup = new TemplateGroup(_templateName, ((WebControl)ViewControl).ControlStyle);
            zoneTemplateGroup.AddTemplateDefinition(new TemplateDefinition(this, _templateName, Component, _templateName, ((WebControl)ViewControl).ControlStyle)); 
            return zoneTemplateGroup;
        }

        protected override bool UsePreviewControl { 
            get {
                return true; 
            } 
        }
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(WebZone));
            base.Initialize(component);
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
