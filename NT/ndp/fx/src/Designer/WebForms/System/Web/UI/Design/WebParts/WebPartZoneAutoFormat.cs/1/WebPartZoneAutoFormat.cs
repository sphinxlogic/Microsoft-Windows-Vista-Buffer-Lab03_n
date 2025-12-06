//------------------------------------------------------------------------------ 
// <copyright file="WebPartZoneAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Data;
    using System.Design; 
    using System.Web.UI.WebControls.WebParts;

    internal sealed class WebPartZoneAutoFormat : BaseAutoFormat {
 
        public WebPartZoneAutoFormat(DataRow schemeData) : base(schemeData) {
            // Use default Height 
            Style.Width = 250; 
        }
 
        public override Control GetPreviewControl(Control runtimeControl) {
            WebPartZone previewZone = (WebPartZone)base.GetPreviewControl(runtimeControl);

            // If the zone contains no WebParts, set the ZoneTemplate to a dummy Template, so 
            // that there is at least one WebPart in the AutoFormat preview
            if (previewZone != null && previewZone.WebParts.Count == 0) { 
                previewZone.ZoneTemplate = new AutoFormatTemplate(); 
            }
 
            return previewZone;
        }

        private sealed class AutoFormatTemplate : ITemplate { 
            public void InstantiateIn(Control container) {
                container.Controls.Add(new SampleWebPart()); 
            } 

            private sealed class SampleWebPart : WebPart { 
                public SampleWebPart() {
                    Title = SR.GetString(SR.WebPartZoneAutoFormat_SampleWebPartTitle);
                    ID = "SampleWebPart";
                } 

                protected override void RenderContents(HtmlTextWriter writer) { 
                    writer.Write(SR.GetString(SR.WebPartZoneAutoFormat_SampleWebPartContents)); 
                }
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WebPartZoneAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Data;
    using System.Design; 
    using System.Web.UI.WebControls.WebParts;

    internal sealed class WebPartZoneAutoFormat : BaseAutoFormat {
 
        public WebPartZoneAutoFormat(DataRow schemeData) : base(schemeData) {
            // Use default Height 
            Style.Width = 250; 
        }
 
        public override Control GetPreviewControl(Control runtimeControl) {
            WebPartZone previewZone = (WebPartZone)base.GetPreviewControl(runtimeControl);

            // If the zone contains no WebParts, set the ZoneTemplate to a dummy Template, so 
            // that there is at least one WebPart in the AutoFormat preview
            if (previewZone != null && previewZone.WebParts.Count == 0) { 
                previewZone.ZoneTemplate = new AutoFormatTemplate(); 
            }
 
            return previewZone;
        }

        private sealed class AutoFormatTemplate : ITemplate { 
            public void InstantiateIn(Control container) {
                container.Controls.Add(new SampleWebPart()); 
            } 

            private sealed class SampleWebPart : WebPart { 
                public SampleWebPart() {
                    Title = SR.GetString(SR.WebPartZoneAutoFormat_SampleWebPartTitle);
                    ID = "SampleWebPart";
                } 

                protected override void RenderContents(HtmlTextWriter writer) { 
                    writer.Write(SR.GetString(SR.WebPartZoneAutoFormat_SampleWebPartContents)); 
                }
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
