//------------------------------------------------------------------------------ 
// <copyright file="LocalizeDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Text; 
    using System.Web.UI.Design; 
    using System.Web.UI.WebControls;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [SupportsPreviewControl(true)]
    internal class LocalizeDesigner : LiteralDesigner {
        private const string DesignTimeHtml = 
            @"<span {0}=0></span>";
 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            EditableDesignerRegion region = new EditableDesignerRegion(this, "Text");
            region.Description = SR.GetString(SR.LocalizeDesigner_RegionWatermark); 
            region.Properties[typeof(Control)] = Component;
            regions.Add(region);

            return String.Format(CultureInfo.InvariantCulture, 
                DesignTimeHtml,
                DesignerRegion.DesignerRegionAttributeName); 
        } 

        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(Component)["Text"];
            return (string)propDesc.GetValue(Component);
        }
 
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
            string text = content; 
            try { 
                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
                Control[] controls = ControlParser.ParseControls(designerHost, content); 
                text = String.Empty;
                foreach (Control c in controls) {
                    LiteralControl literal = c as LiteralControl;
                    if (literal != null) { 
                        text += literal.Text;
                    } 
                } 
            }
            catch { 
                // In the unlikely event that there is an error parsing controls from the
                // region content, we just end up using the raw content as the Text property.
                // It's highly unlikely to ever throw an exception since the tool just
                // persisted the content moments earlier. 
            }
            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(Component)["Text"]; 
            propDesc.SetValue(Component, text); 
        }
 
        protected override void PostFilterProperties(IDictionary properties) {
            // Hide all properties except for ID
            HideAllPropertiesExceptID(properties);
 
            base.PostFilterAttributes(properties);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LocalizeDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Text; 
    using System.Web.UI.Design; 
    using System.Web.UI.WebControls;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [SupportsPreviewControl(true)]
    internal class LocalizeDesigner : LiteralDesigner {
        private const string DesignTimeHtml = 
            @"<span {0}=0></span>";
 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            EditableDesignerRegion region = new EditableDesignerRegion(this, "Text");
            region.Description = SR.GetString(SR.LocalizeDesigner_RegionWatermark); 
            region.Properties[typeof(Control)] = Component;
            regions.Add(region);

            return String.Format(CultureInfo.InvariantCulture, 
                DesignTimeHtml,
                DesignerRegion.DesignerRegionAttributeName); 
        } 

        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(Component)["Text"];
            return (string)propDesc.GetValue(Component);
        }
 
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
            string text = content; 
            try { 
                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
                Control[] controls = ControlParser.ParseControls(designerHost, content); 
                text = String.Empty;
                foreach (Control c in controls) {
                    LiteralControl literal = c as LiteralControl;
                    if (literal != null) { 
                        text += literal.Text;
                    } 
                } 
            }
            catch { 
                // In the unlikely event that there is an error parsing controls from the
                // region content, we just end up using the raw content as the Text property.
                // It's highly unlikely to ever throw an exception since the tool just
                // persisted the content moments earlier. 
            }
            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(Component)["Text"]; 
            propDesc.SetValue(Component, text); 
        }
 
        protected override void PostFilterProperties(IDictionary properties) {
            // Hide all properties except for ID
            HideAllPropertiesExceptID(properties);
 
            base.PostFilterAttributes(properties);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
