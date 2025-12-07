//------------------------------------------------------------------------------ 
// <copyright file="ConnectionsZoneAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Data;
    using System.Design; 
    using System.Web.UI.WebControls.WebParts;

    internal sealed class ConnectionsZoneAutoFormat : BaseAutoFormat {
        internal const string PreviewControlID = "AutoFormatPreviewControl"; 

        public ConnectionsZoneAutoFormat(DataRow schemeData) : base(schemeData) { 
            // Use default height 
            Style.Width = 225;
        } 

        public override Control GetPreviewControl(Control runtimeControl) {
            ConnectionsZone previewZone = (ConnectionsZone)base.GetPreviewControl(runtimeControl);
 
            // Set the ID of the zone to the special PreviewControlID, so the ConnectionsZoneDesigner
            // doesn't render the placeholder for this control in the AutoFormat dialog, regardless 
            // of whether ViewInEditMode is true. 
            previewZone.ID = PreviewControlID;
            return previewZone; 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ConnectionsZoneAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Data;
    using System.Design; 
    using System.Web.UI.WebControls.WebParts;

    internal sealed class ConnectionsZoneAutoFormat : BaseAutoFormat {
        internal const string PreviewControlID = "AutoFormatPreviewControl"; 

        public ConnectionsZoneAutoFormat(DataRow schemeData) : base(schemeData) { 
            // Use default height 
            Style.Width = 225;
        } 

        public override Control GetPreviewControl(Control runtimeControl) {
            ConnectionsZone previewZone = (ConnectionsZone)base.GetPreviewControl(runtimeControl);
 
            // Set the ID of the zone to the special PreviewControlID, so the ConnectionsZoneDesigner
            // doesn't render the placeholder for this control in the AutoFormat dialog, regardless 
            // of whether ViewInEditMode is true. 
            previewZone.ID = PreviewControlID;
            return previewZone; 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
