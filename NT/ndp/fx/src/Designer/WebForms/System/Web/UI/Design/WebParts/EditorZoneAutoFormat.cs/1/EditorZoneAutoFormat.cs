//------------------------------------------------------------------------------ 
// <copyright file="EditorZoneAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Data;
    using System.Design; 
    using System.Web.UI.WebControls.WebParts;

    internal sealed class EditorZoneAutoFormat : BaseAutoFormat {
 
        internal const string PreviewControlID = "AutoFormatPreviewControl";
 
        public EditorZoneAutoFormat(DataRow schemeData) : base(schemeData) { 
            Style.Height = 275;
            Style.Width = 300; 
        }

        public override Control GetPreviewControl(Control runtimeControl) {
            EditorZone previewZone = (EditorZone)base.GetPreviewControl(runtimeControl); 

            // If the zone contains no EditorParts, set the ZoneTemplate to a dummy Template, so 
            // that there is at least one EditorPart in the AutoFormat preview 
            if (previewZone != null && previewZone.EditorParts.Count == 0) {
                previewZone.ZoneTemplate = new AutoFormatTemplate(); 
            }

            // Set the ID of the zone to the special PreviewControlID, so the EditorZoneDesigner
            // doesn't render the placeholder for this control in the AutoFormat dialog, regardless 
            // of whether ViewInEditMode is true.
            previewZone.ID = PreviewControlID; 
            return previewZone; 
        }
 
        private sealed class AutoFormatTemplate : ITemplate {
            public void InstantiateIn(Control container) {
                LayoutEditorPart layoutEditorPart = new LayoutEditorPart();
                layoutEditorPart.ID = "LayoutEditorPart"; 
                container.Controls.Add(layoutEditorPart);
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="EditorZoneAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Data;
    using System.Design; 
    using System.Web.UI.WebControls.WebParts;

    internal sealed class EditorZoneAutoFormat : BaseAutoFormat {
 
        internal const string PreviewControlID = "AutoFormatPreviewControl";
 
        public EditorZoneAutoFormat(DataRow schemeData) : base(schemeData) { 
            Style.Height = 275;
            Style.Width = 300; 
        }

        public override Control GetPreviewControl(Control runtimeControl) {
            EditorZone previewZone = (EditorZone)base.GetPreviewControl(runtimeControl); 

            // If the zone contains no EditorParts, set the ZoneTemplate to a dummy Template, so 
            // that there is at least one EditorPart in the AutoFormat preview 
            if (previewZone != null && previewZone.EditorParts.Count == 0) {
                previewZone.ZoneTemplate = new AutoFormatTemplate(); 
            }

            // Set the ID of the zone to the special PreviewControlID, so the EditorZoneDesigner
            // doesn't render the placeholder for this control in the AutoFormat dialog, regardless 
            // of whether ViewInEditMode is true.
            previewZone.ID = PreviewControlID; 
            return previewZone; 
        }
 
        private sealed class AutoFormatTemplate : ITemplate {
            public void InstantiateIn(Control container) {
                LayoutEditorPart layoutEditorPart = new LayoutEditorPart();
                layoutEditorPart.ID = "LayoutEditorPart"; 
                container.Controls.Add(layoutEditorPart);
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
