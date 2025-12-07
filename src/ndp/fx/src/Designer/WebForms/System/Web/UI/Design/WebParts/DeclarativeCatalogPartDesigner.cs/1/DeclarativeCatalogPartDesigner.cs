//------------------------------------------------------------------------------ 
// <copyright file="DeclarativeCatalogPartDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Design;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class DeclarativeCatalogPartDesigner : CatalogPartDesigner {
 
        private const string templateName = "WebPartsTemplate";

        private DeclarativeCatalogPart _catalogPart;
        private TemplateGroup _templateGroup; 

        public override TemplateGroupCollection TemplateGroups { 
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
                if (_templateGroup == null) {
                    _templateGroup = new TemplateGroup(templateName, _catalogPart.ControlStyle);
                    _templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, _catalogPart, templateName, _catalogPart.ControlStyle));
                } 

                groups.Add(_templateGroup); 
 
                return groups;
            } 
        }

        public override string GetDesignTimeHtml() {
            if (!(_catalogPart.Parent is CatalogZoneBase)) { 
                return CreateInvalidParentDesignTimeHtml(typeof(CatalogPart), typeof(CatalogZoneBase));
            } 
 
            string designTimeHtml = String.Empty;
            try { 
                if (((DeclarativeCatalogPart)ViewControl).WebPartsTemplate == null) {
                    designTimeHtml = GetEmptyDesignTimeHtml();
                }
                else { 
                    // DeclarativeCatalogPart has no default runtime rendering, so GetDesignTimeHtml() should also
                    // return String.Empty, so we don't get the '[Type "ID"]' rendered in the designer. 
                    // 
                    designTimeHtml = String.Empty;
                } 
            } catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e);
            }
 
            return designTimeHtml;
        } 
 
        protected override string GetEmptyDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.DeclarativeCatalogPartDesigner_Empty)); 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(DeclarativeCatalogPart)); 

            base.Initialize(component); 
            _catalogPart = (DeclarativeCatalogPart)component; 

            if (View != null) { 
                View.SetFlags(ViewFlags.TemplateEditing, true);
            }
        }
 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DeclarativeCatalogPartDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Design;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class DeclarativeCatalogPartDesigner : CatalogPartDesigner {
 
        private const string templateName = "WebPartsTemplate";

        private DeclarativeCatalogPart _catalogPart;
        private TemplateGroup _templateGroup; 

        public override TemplateGroupCollection TemplateGroups { 
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
                if (_templateGroup == null) {
                    _templateGroup = new TemplateGroup(templateName, _catalogPart.ControlStyle);
                    _templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, _catalogPart, templateName, _catalogPart.ControlStyle));
                } 

                groups.Add(_templateGroup); 
 
                return groups;
            } 
        }

        public override string GetDesignTimeHtml() {
            if (!(_catalogPart.Parent is CatalogZoneBase)) { 
                return CreateInvalidParentDesignTimeHtml(typeof(CatalogPart), typeof(CatalogZoneBase));
            } 
 
            string designTimeHtml = String.Empty;
            try { 
                if (((DeclarativeCatalogPart)ViewControl).WebPartsTemplate == null) {
                    designTimeHtml = GetEmptyDesignTimeHtml();
                }
                else { 
                    // DeclarativeCatalogPart has no default runtime rendering, so GetDesignTimeHtml() should also
                    // return String.Empty, so we don't get the '[Type "ID"]' rendered in the designer. 
                    // 
                    designTimeHtml = String.Empty;
                } 
            } catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e);
            }
 
            return designTimeHtml;
        } 
 
        protected override string GetEmptyDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.DeclarativeCatalogPartDesigner_Empty)); 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(DeclarativeCatalogPart)); 

            base.Initialize(component); 
            _catalogPart = (DeclarativeCatalogPart)component; 

            if (View != null) { 
                View.SetFlags(ViewFlags.TemplateEditing, true);
            }
        }
 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
