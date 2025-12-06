//------------------------------------------------------------------------------ 
// <copyright file="DesignerCatalogPartChrome.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Design;
    using System.Globalization;
    using System.IO;
    using System.Web.UI; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts; 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    internal class DesignerCatalogPartChrome : CatalogPartChrome {

        private ViewRendering _partViewRendering;
 
        public DesignerCatalogPartChrome(CatalogZone zone) : base(zone) {
        } 
 
        public ViewRendering GetViewRendering(Control control) {
            CatalogPart part = control as CatalogPart; 

            if (part == null) {
                // The control is not a CatalogPart, so we should render an error block. (VSWhidbey 232109)
                string errorDesignTimeHtml = ControlDesigner.CreateErrorDesignTimeHtml( 
                    SR.GetString(SR.CatalogZoneDesigner_OnlyCatalogParts), null, control);
                return new ViewRendering(errorDesignTimeHtml, new DesignerRegionCollection()); 
            } 
            else {
                string designTimeHtml; 
                DesignerRegionCollection regions;
                try {
                    // Set Zone for CatalogPart at design-time
                    IDictionary param = new HybridDictionary(1); 
                    param["Zone"] = Zone;
                    ((IControlDesignerAccessor)part).SetDesignModeState(param); 
 
                    _partViewRendering = ControlDesigner.GetViewRendering(part);
                    regions = _partViewRendering.Regions; 

                    StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
                    // Pass in the ViewControl instead of the CatalogPart, so that design-time themes are
                    // reflected in the Chrome rendering 
                    RenderCatalogPart(new DesignTimeHtmlTextWriter(writer), (CatalogPart)PartDesigner.GetViewControl(part));
                    designTimeHtml = writer.ToString(); 
                } 
                catch (Exception e) {
                    designTimeHtml = ControlDesigner.CreateErrorDesignTimeHtml( 
                        SR.GetString(SR.ControlDesigner_UnhandledException), e, control);
                    regions = new DesignerRegionCollection();
                }
 
                return new ViewRendering(designTimeHtml, regions);
            } 
        } 

        protected override void RenderPartContents(HtmlTextWriter writer, CatalogPart catalogPart) { 
            writer.Write(_partViewRendering.Content);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerCatalogPartChrome.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Design;
    using System.Globalization;
    using System.IO;
    using System.Web.UI; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts; 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    internal class DesignerCatalogPartChrome : CatalogPartChrome {

        private ViewRendering _partViewRendering;
 
        public DesignerCatalogPartChrome(CatalogZone zone) : base(zone) {
        } 
 
        public ViewRendering GetViewRendering(Control control) {
            CatalogPart part = control as CatalogPart; 

            if (part == null) {
                // The control is not a CatalogPart, so we should render an error block. (VSWhidbey 232109)
                string errorDesignTimeHtml = ControlDesigner.CreateErrorDesignTimeHtml( 
                    SR.GetString(SR.CatalogZoneDesigner_OnlyCatalogParts), null, control);
                return new ViewRendering(errorDesignTimeHtml, new DesignerRegionCollection()); 
            } 
            else {
                string designTimeHtml; 
                DesignerRegionCollection regions;
                try {
                    // Set Zone for CatalogPart at design-time
                    IDictionary param = new HybridDictionary(1); 
                    param["Zone"] = Zone;
                    ((IControlDesignerAccessor)part).SetDesignModeState(param); 
 
                    _partViewRendering = ControlDesigner.GetViewRendering(part);
                    regions = _partViewRendering.Regions; 

                    StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
                    // Pass in the ViewControl instead of the CatalogPart, so that design-time themes are
                    // reflected in the Chrome rendering 
                    RenderCatalogPart(new DesignTimeHtmlTextWriter(writer), (CatalogPart)PartDesigner.GetViewControl(part));
                    designTimeHtml = writer.ToString(); 
                } 
                catch (Exception e) {
                    designTimeHtml = ControlDesigner.CreateErrorDesignTimeHtml( 
                        SR.GetString(SR.ControlDesigner_UnhandledException), e, control);
                    regions = new DesignerRegionCollection();
                }
 
                return new ViewRendering(designTimeHtml, regions);
            } 
        } 

        protected override void RenderPartContents(HtmlTextWriter writer, CatalogPart catalogPart) { 
            writer.Write(_partViewRendering.Content);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
