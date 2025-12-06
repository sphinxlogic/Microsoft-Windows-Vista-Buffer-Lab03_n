//------------------------------------------------------------------------------ 
// <copyright file="ConnectionsZoneDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.IO; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ConnectionsZoneDesigner : ToolZoneDesigner { 

        // We want to hide these properties in the designer, but we cannot override 
        // them on ConnectionsZone since they are non-virtual.  So we hide them in PreFilterProperties(). 
        private static readonly string[] _hiddenProperties = new string[] {
            "EmptyZoneTextStyle", 
            "PartChromeStyle",
            "PartStyle",
            "PartTitleStyle",
        }; 

        private static DesignerAutoFormatCollection _autoFormats; 
        private ConnectionsZone _zone; 

        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.CONNECTIONSZONE_SCHEMES,
                        delegate(DataRow schemeData) { return new ConnectionsZoneAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            } 
        }
 
        public override string GetDesignTimeHtml() {
            string designTimeHtml;

            try { 
                ConnectionsZone zone = (ConnectionsZone)ViewControl;
 
                designTimeHtml = base.GetDesignTimeHtml(); 

                if (ViewInBrowseMode && zone.ID != CatalogZoneAutoFormat.PreviewControlID) { 
                    designTimeHtml = CreatePlaceHolderDesignTimeHtml();
                }
            }
            catch (Exception e) { 
                designTimeHtml = GetErrorDesignTimeHtml(e);
            } 
 
            return designTimeHtml;
        } 

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(ConnectionsZone));
            base.Initialize(component); 
            _zone = (ConnectionsZone)component;
        } 
 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 

            Attribute[] newAttributes = new Attribute[] {
                new BrowsableAttribute(false),
                new EditorBrowsableAttribute(EditorBrowsableState.Never), 
                new ThemeableAttribute(false),
            }; 
 
            foreach (string propertyName in _hiddenProperties) {
                PropertyDescriptor property = (PropertyDescriptor) properties[propertyName]; 
                Debug.Assert(property != null, "Property is null: " + propertyName);
                if (property != null) {
                    properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, newAttributes);
                } 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ConnectionsZoneDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.IO; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ConnectionsZoneDesigner : ToolZoneDesigner { 

        // We want to hide these properties in the designer, but we cannot override 
        // them on ConnectionsZone since they are non-virtual.  So we hide them in PreFilterProperties(). 
        private static readonly string[] _hiddenProperties = new string[] {
            "EmptyZoneTextStyle", 
            "PartChromeStyle",
            "PartStyle",
            "PartTitleStyle",
        }; 

        private static DesignerAutoFormatCollection _autoFormats; 
        private ConnectionsZone _zone; 

        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.CONNECTIONSZONE_SCHEMES,
                        delegate(DataRow schemeData) { return new ConnectionsZoneAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            } 
        }
 
        public override string GetDesignTimeHtml() {
            string designTimeHtml;

            try { 
                ConnectionsZone zone = (ConnectionsZone)ViewControl;
 
                designTimeHtml = base.GetDesignTimeHtml(); 

                if (ViewInBrowseMode && zone.ID != CatalogZoneAutoFormat.PreviewControlID) { 
                    designTimeHtml = CreatePlaceHolderDesignTimeHtml();
                }
            }
            catch (Exception e) { 
                designTimeHtml = GetErrorDesignTimeHtml(e);
            } 
 
            return designTimeHtml;
        } 

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(ConnectionsZone));
            base.Initialize(component); 
            _zone = (ConnectionsZone)component;
        } 
 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 

            Attribute[] newAttributes = new Attribute[] {
                new BrowsableAttribute(false),
                new EditorBrowsableAttribute(EditorBrowsableState.Never), 
                new ThemeableAttribute(false),
            }; 
 
            foreach (string propertyName in _hiddenProperties) {
                PropertyDescriptor property = (PropertyDescriptor) properties[propertyName]; 
                Debug.Assert(property != null, "Property is null: " + propertyName);
                if (property != null) {
                    properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, newAttributes);
                } 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
