//------------------------------------------------------------------------------ 
// <copyright file="DesignerEditorPartChrome.cs" company="Microsoft">
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
    internal class DesignerEditorPartChrome : EditorPartChrome {

        private ViewRendering _partViewRendering;
 
        public DesignerEditorPartChrome(EditorZone zone) : base(zone) {
        } 
 
        public ViewRendering GetViewRendering(Control control) {
            EditorPart part = control as EditorPart; 

            if (part == null) {
                // The control is not an EditorPart, so we should render an error block. (VSWhidbey 232109)
                string errorDesignTimeHtml = ControlDesigner.CreateErrorDesignTimeHtml( 
                    SR.GetString(SR.EditorZoneDesigner_OnlyEditorParts), null, control);
                return new ViewRendering(errorDesignTimeHtml, new DesignerRegionCollection()); 
            } 
            else {
                string designTimeHtml; 
                DesignerRegionCollection regions;
                try {
                    // Set Zone for EditorPart at design-time
                    IDictionary param = new HybridDictionary(1); 
                    param["Zone"] = Zone;
                    ((IControlDesignerAccessor)part).SetDesignModeState(param); 
 
                    _partViewRendering = ControlDesigner.GetViewRendering(part);
                    regions = _partViewRendering.Regions; 

                    StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
                    // Pass in the ViewControl instead of the EditorPart, so that design-time themes are
                    // reflected in the Chrome rendering 
                    RenderEditorPart(new DesignTimeHtmlTextWriter(writer), (EditorPart)PartDesigner.GetViewControl(part));
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

        protected override void RenderPartContents(HtmlTextWriter writer, EditorPart editorPart) { 
            writer.Write(_partViewRendering.Content);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerEditorPartChrome.cs" company="Microsoft">
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
    internal class DesignerEditorPartChrome : EditorPartChrome {

        private ViewRendering _partViewRendering;
 
        public DesignerEditorPartChrome(EditorZone zone) : base(zone) {
        } 
 
        public ViewRendering GetViewRendering(Control control) {
            EditorPart part = control as EditorPart; 

            if (part == null) {
                // The control is not an EditorPart, so we should render an error block. (VSWhidbey 232109)
                string errorDesignTimeHtml = ControlDesigner.CreateErrorDesignTimeHtml( 
                    SR.GetString(SR.EditorZoneDesigner_OnlyEditorParts), null, control);
                return new ViewRendering(errorDesignTimeHtml, new DesignerRegionCollection()); 
            } 
            else {
                string designTimeHtml; 
                DesignerRegionCollection regions;
                try {
                    // Set Zone for EditorPart at design-time
                    IDictionary param = new HybridDictionary(1); 
                    param["Zone"] = Zone;
                    ((IControlDesignerAccessor)part).SetDesignModeState(param); 
 
                    _partViewRendering = ControlDesigner.GetViewRendering(part);
                    regions = _partViewRendering.Regions; 

                    StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
                    // Pass in the ViewControl instead of the EditorPart, so that design-time themes are
                    // reflected in the Chrome rendering 
                    RenderEditorPart(new DesignTimeHtmlTextWriter(writer), (EditorPart)PartDesigner.GetViewControl(part));
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

        protected override void RenderPartContents(HtmlTextWriter writer, EditorPart editorPart) { 
            writer.Write(_partViewRendering.Content);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
