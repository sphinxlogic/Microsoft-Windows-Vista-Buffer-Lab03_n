//------------------------------------------------------------------------------ 
// <copyright file="CatalogZoneAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Data;
    using System.Design; 
    using System.Globalization;
    using System.Web.UI.WebControls.WebParts;

    internal sealed class CatalogZoneAutoFormat : BaseAutoFormat { 

        internal const string PreviewControlID = "AutoFormatPreviewControl"; 
 
        public CatalogZoneAutoFormat(DataRow schemeData) : base(schemeData) {
            // Use default Height 
            Style.Width = 300;
        }

        public override Control GetPreviewControl(Control runtimeControl) { 
            CatalogZone previewZone = (CatalogZone)base.GetPreviewControl(runtimeControl);
 
            // If the zone contains no CatalogParts, set the ZoneTemplate to a dummy Template, so 
            // that there is at least one CatalogPart in the AutoFormat preview
            if (previewZone != null && previewZone.CatalogParts.Count == 0) { 
                previewZone.ZoneTemplate = new AutoFormatTemplate();
            }

            // Set the ID of the zone to the special PreviewControlID, so the CatalogZoneDesigner 
            // doesn't render the placeholder for this control in the AutoFormat dialog, regardless
            // of whether ViewInEditMode is true. 
            previewZone.ID = PreviewControlID; 
            return previewZone;
        } 

        private sealed class AutoFormatTemplate : ITemplate {
            public void InstantiateIn(Control container) {
                DeclarativeCatalogPart sampleCatalogPart = new DeclarativeCatalogPart(); 
                sampleCatalogPart.WebPartsTemplate = new SampleCatalogPartTemplate();
                sampleCatalogPart.ID = "SampleCatalogPart"; 
                container.Controls.Add(sampleCatalogPart); 
            }
 
            private sealed class SampleCatalogPartTemplate : ITemplate {
                public void InstantiateIn(Control container) {
                    SampleWebPart sampleWebPart = new SampleWebPart();
                    sampleWebPart.ID = "SampleWebPart1"; 
                    sampleWebPart.Title = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.CatalogZone_SampleWebPartTitle), "1");
                    container.Controls.Add(sampleWebPart); 
 
                    sampleWebPart = new SampleWebPart();
                    sampleWebPart.ID = "SampleWebPart2"; 
                    sampleWebPart.Title = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.CatalogZone_SampleWebPartTitle), "2");
                    container.Controls.Add(sampleWebPart);

                    sampleWebPart = new SampleWebPart(); 
                    sampleWebPart.ID = "SampleWebPart3";
                    sampleWebPart.Title = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.CatalogZone_SampleWebPartTitle), "3"); 
                    container.Controls.Add(sampleWebPart); 
                }
 
                private sealed class SampleWebPart : WebPart {
                }
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="CatalogZoneAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Data;
    using System.Design; 
    using System.Globalization;
    using System.Web.UI.WebControls.WebParts;

    internal sealed class CatalogZoneAutoFormat : BaseAutoFormat { 

        internal const string PreviewControlID = "AutoFormatPreviewControl"; 
 
        public CatalogZoneAutoFormat(DataRow schemeData) : base(schemeData) {
            // Use default Height 
            Style.Width = 300;
        }

        public override Control GetPreviewControl(Control runtimeControl) { 
            CatalogZone previewZone = (CatalogZone)base.GetPreviewControl(runtimeControl);
 
            // If the zone contains no CatalogParts, set the ZoneTemplate to a dummy Template, so 
            // that there is at least one CatalogPart in the AutoFormat preview
            if (previewZone != null && previewZone.CatalogParts.Count == 0) { 
                previewZone.ZoneTemplate = new AutoFormatTemplate();
            }

            // Set the ID of the zone to the special PreviewControlID, so the CatalogZoneDesigner 
            // doesn't render the placeholder for this control in the AutoFormat dialog, regardless
            // of whether ViewInEditMode is true. 
            previewZone.ID = PreviewControlID; 
            return previewZone;
        } 

        private sealed class AutoFormatTemplate : ITemplate {
            public void InstantiateIn(Control container) {
                DeclarativeCatalogPart sampleCatalogPart = new DeclarativeCatalogPart(); 
                sampleCatalogPart.WebPartsTemplate = new SampleCatalogPartTemplate();
                sampleCatalogPart.ID = "SampleCatalogPart"; 
                container.Controls.Add(sampleCatalogPart); 
            }
 
            private sealed class SampleCatalogPartTemplate : ITemplate {
                public void InstantiateIn(Control container) {
                    SampleWebPart sampleWebPart = new SampleWebPart();
                    sampleWebPart.ID = "SampleWebPart1"; 
                    sampleWebPart.Title = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.CatalogZone_SampleWebPartTitle), "1");
                    container.Controls.Add(sampleWebPart); 
 
                    sampleWebPart = new SampleWebPart();
                    sampleWebPart.ID = "SampleWebPart2"; 
                    sampleWebPart.Title = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.CatalogZone_SampleWebPartTitle), "2");
                    container.Controls.Add(sampleWebPart);

                    sampleWebPart = new SampleWebPart(); 
                    sampleWebPart.ID = "SampleWebPart3";
                    sampleWebPart.Title = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.CatalogZone_SampleWebPartTitle), "3"); 
                    container.Controls.Add(sampleWebPart); 
                }
 
                private sealed class SampleWebPart : WebPart {
                }
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
