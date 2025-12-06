//------------------------------------------------------------------------------ 
// <copyright file="DesignerWebPartChrome.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Design;
    using System.Globalization; 
    using System.IO;
    using System.Web.UI;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts;
 
    /// <devdoc> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    internal class DesignerWebPartChrome : WebPartChrome {

        private ViewRendering _partViewRendering;
 
        public DesignerWebPartChrome(WebPartZoneBase zone) : base(zone, null) {
        } 
 
        public ViewRendering GetViewRendering(Control control) {
            string designTimeHtml; 
            DesignerRegionCollection regions;
            try {
                _partViewRendering = ControlDesigner.GetViewRendering(control);
                regions = _partViewRendering.Regions; 

                WebPart webPart = control as WebPart; 
                if (webPart == null) { 
                    // We should not reparent the control, so we must use the DesignerGenericWebPart instead
                    // of the regular GenericWebPart. 
                    // Pass in the ViewControl instead of the Control, so that design-time themes are
                    // reflected in the Chrome rendering
                    webPart = new DesignerGenericWebPart(PartDesigner.GetViewControl(control));
                } 

                StringWriter innerWriter = new StringWriter(CultureInfo.InvariantCulture); 
                // Pass in the ViewControl instead of the WebPart, so that design-time themes are 
                // reflected in the Chrome rendering
                RenderWebPart(new DesignTimeHtmlTextWriter(innerWriter), (WebPart)PartDesigner.GetViewControl(webPart)); 
                designTimeHtml = innerWriter.ToString();
            }
            catch (Exception e) {
                designTimeHtml = ControlDesigner.CreateErrorDesignTimeHtml( 
                    SR.GetString(SR.ControlDesigner_UnhandledException), e, control);
                regions = new DesignerRegionCollection(); 
            } 

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture); 
            DesignTimeHtmlTextWriter htmlTextWriter = new DesignTimeHtmlTextWriter(writer);

            bool horizontal = (Zone.LayoutOrientation == Orientation.Horizontal);
            if (horizontal)  { 
                htmlTextWriter.AddStyleAttribute("display", "inline-block");
                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.Height, "100%"); 
                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Span); 
            }
 
            htmlTextWriter.Write(designTimeHtml);

            if (horizontal) {
                htmlTextWriter.RenderEndTag(); 
            }
 
            return new ViewRendering(writer.ToString(), regions); 
        }
 
        protected override void RenderPartContents(HtmlTextWriter writer, WebPart webPart) {
            writer.Write(_partViewRendering.Content);
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerWebPartChrome.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Design;
    using System.Globalization; 
    using System.IO;
    using System.Web.UI;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts;
 
    /// <devdoc> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    internal class DesignerWebPartChrome : WebPartChrome {

        private ViewRendering _partViewRendering;
 
        public DesignerWebPartChrome(WebPartZoneBase zone) : base(zone, null) {
        } 
 
        public ViewRendering GetViewRendering(Control control) {
            string designTimeHtml; 
            DesignerRegionCollection regions;
            try {
                _partViewRendering = ControlDesigner.GetViewRendering(control);
                regions = _partViewRendering.Regions; 

                WebPart webPart = control as WebPart; 
                if (webPart == null) { 
                    // We should not reparent the control, so we must use the DesignerGenericWebPart instead
                    // of the regular GenericWebPart. 
                    // Pass in the ViewControl instead of the Control, so that design-time themes are
                    // reflected in the Chrome rendering
                    webPart = new DesignerGenericWebPart(PartDesigner.GetViewControl(control));
                } 

                StringWriter innerWriter = new StringWriter(CultureInfo.InvariantCulture); 
                // Pass in the ViewControl instead of the WebPart, so that design-time themes are 
                // reflected in the Chrome rendering
                RenderWebPart(new DesignTimeHtmlTextWriter(innerWriter), (WebPart)PartDesigner.GetViewControl(webPart)); 
                designTimeHtml = innerWriter.ToString();
            }
            catch (Exception e) {
                designTimeHtml = ControlDesigner.CreateErrorDesignTimeHtml( 
                    SR.GetString(SR.ControlDesigner_UnhandledException), e, control);
                regions = new DesignerRegionCollection(); 
            } 

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture); 
            DesignTimeHtmlTextWriter htmlTextWriter = new DesignTimeHtmlTextWriter(writer);

            bool horizontal = (Zone.LayoutOrientation == Orientation.Horizontal);
            if (horizontal)  { 
                htmlTextWriter.AddStyleAttribute("display", "inline-block");
                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.Height, "100%"); 
                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Span); 
            }
 
            htmlTextWriter.Write(designTimeHtml);

            if (horizontal) {
                htmlTextWriter.RenderEndTag(); 
            }
 
            return new ViewRendering(writer.ToString(), regions); 
        }
 
        protected override void RenderPartContents(HtmlTextWriter writer, WebPart webPart) {
            writer.Write(_partViewRendering.Content);
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
