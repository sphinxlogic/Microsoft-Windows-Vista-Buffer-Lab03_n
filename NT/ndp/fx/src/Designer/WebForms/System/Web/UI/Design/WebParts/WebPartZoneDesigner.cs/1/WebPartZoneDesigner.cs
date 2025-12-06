//------------------------------------------------------------------------------ 
// <copyright file="WebPartZoneDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
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

    /// <devdoc>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class WebPartZoneDesigner : WebPartZoneBaseDesigner {
 
        private static DesignerAutoFormatCollection _autoFormats; 

        private WebPartZone _zone; 
        private TemplateGroup _templateGroup;

        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.WEBPARTZONE_SCHEMES, 
                        delegate(DataRow schemeData) { return new WebPartZoneAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            }
        }

        public override TemplateGroupCollection TemplateGroups { 
            get {
                TemplateGroupCollection groups = base.TemplateGroups; 
                if (_templateGroup == null) { 
                    _templateGroup = CreateZoneTemplateGroup();
                } 
                groups.Add(_templateGroup);
                return groups;
            }
        } 

        public override string GetDesignTimeHtml() { 
             return GetDesignTimeHtml(null); 
        }
 
        /// <devdoc>
        /// Provides the layout html for the control in the designer, including regions.
        /// </devdoc>
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            string designTimeHtml;
            try { 
                WebPartZone zone = (WebPartZone)ViewControl; 

                bool useRegions = UseRegions(regions, _zone.ZoneTemplate, zone.ZoneTemplate); 

                // When there is an editable region, we want to use the regular control
                // rendering instead of the EmptyDesignTimeHtml
                if (zone.ZoneTemplate == null && !useRegions) { 
                    designTimeHtml = GetEmptyDesignTimeHtml();
                } 
                else { 
                    ((ICompositeControlDesignerAccessor)zone).RecreateChildControls();
                    if (useRegions) { 
                        // If the tools supports editable regions, the initial rendering of the
                        // WebParts in the Zone is thrown away by the tool anyway, so we should clear
                        // the controls collection before rendering.  If we don't clear the controls
                        // collection and a WebPart inside the Zone throws an exception when rendering, 
                        // this would cause the whole Zone to render as an error, instead of just
                        // the offending WebPart.  This also improves perf. 
                        zone.Controls.Clear(); 

                        WebPartEditableDesignerRegion region = new WebPartEditableDesignerRegion(zone, TemplateDefinition); 
                        region.IsSingleInstanceTemplate = true;
                        region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                        regions.Add(region);
                    } 
                    designTimeHtml = base.GetDesignTimeHtml();
                } 
            } 
            catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e); 
            }
            return designTimeHtml;
        }
 
        /// <internalonly/>
        /// <devdoc> 
        /// Get the content for the specified region 
        /// </devdoc>
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            Debug.Assert(region != null);

            // Occasionally getting NullRef here in WebMatrix.  Maybe Zone is null?
            Debug.Assert(_zone != null); 
            return ControlPersister.PersistTemplate(_zone.ZoneTemplate, (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)));
        } 
 
        protected override string GetEmptyDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.WebPartZoneDesigner_Empty)); 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(WebPartZone)); 
            base.Initialize(component);
            _zone = (WebPartZone)component; 
        } 

        /// <internalonly/> 
        /// <devdoc>
        /// Set the content for the specified region
        /// </devdoc>
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            Debug.Assert(region != null);
            _zone.ZoneTemplate = ControlParser.ParseTemplate((IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)), content); 
            IsDirtyInternal = true; 
        }
 
        private sealed class WebPartEditableDesignerRegion : TemplatedEditableDesignerRegion {
            private WebPartZoneBase _zone;

            public WebPartEditableDesignerRegion(WebPartZoneBase zone, TemplateDefinition templateDefinition) 
                : base(templateDefinition) {
                _zone = zone; 
            } 

            public override ViewRendering GetChildViewRendering(Control control) { 
                if (control == null) {
                    throw new ArgumentNullException("control");
                }
 
                DesignerWebPartChrome chrome = new DesignerWebPartChrome(_zone);
                return chrome.GetViewRendering(control); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WebPartZoneDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
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

    /// <devdoc>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class WebPartZoneDesigner : WebPartZoneBaseDesigner {
 
        private static DesignerAutoFormatCollection _autoFormats; 

        private WebPartZone _zone; 
        private TemplateGroup _templateGroup;

        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.WEBPARTZONE_SCHEMES, 
                        delegate(DataRow schemeData) { return new WebPartZoneAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            }
        }

        public override TemplateGroupCollection TemplateGroups { 
            get {
                TemplateGroupCollection groups = base.TemplateGroups; 
                if (_templateGroup == null) { 
                    _templateGroup = CreateZoneTemplateGroup();
                } 
                groups.Add(_templateGroup);
                return groups;
            }
        } 

        public override string GetDesignTimeHtml() { 
             return GetDesignTimeHtml(null); 
        }
 
        /// <devdoc>
        /// Provides the layout html for the control in the designer, including regions.
        /// </devdoc>
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            string designTimeHtml;
            try { 
                WebPartZone zone = (WebPartZone)ViewControl; 

                bool useRegions = UseRegions(regions, _zone.ZoneTemplate, zone.ZoneTemplate); 

                // When there is an editable region, we want to use the regular control
                // rendering instead of the EmptyDesignTimeHtml
                if (zone.ZoneTemplate == null && !useRegions) { 
                    designTimeHtml = GetEmptyDesignTimeHtml();
                } 
                else { 
                    ((ICompositeControlDesignerAccessor)zone).RecreateChildControls();
                    if (useRegions) { 
                        // If the tools supports editable regions, the initial rendering of the
                        // WebParts in the Zone is thrown away by the tool anyway, so we should clear
                        // the controls collection before rendering.  If we don't clear the controls
                        // collection and a WebPart inside the Zone throws an exception when rendering, 
                        // this would cause the whole Zone to render as an error, instead of just
                        // the offending WebPart.  This also improves perf. 
                        zone.Controls.Clear(); 

                        WebPartEditableDesignerRegion region = new WebPartEditableDesignerRegion(zone, TemplateDefinition); 
                        region.IsSingleInstanceTemplate = true;
                        region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                        regions.Add(region);
                    } 
                    designTimeHtml = base.GetDesignTimeHtml();
                } 
            } 
            catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e); 
            }
            return designTimeHtml;
        }
 
        /// <internalonly/>
        /// <devdoc> 
        /// Get the content for the specified region 
        /// </devdoc>
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            Debug.Assert(region != null);

            // Occasionally getting NullRef here in WebMatrix.  Maybe Zone is null?
            Debug.Assert(_zone != null); 
            return ControlPersister.PersistTemplate(_zone.ZoneTemplate, (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)));
        } 
 
        protected override string GetEmptyDesignTimeHtml() {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.WebPartZoneDesigner_Empty)); 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(WebPartZone)); 
            base.Initialize(component);
            _zone = (WebPartZone)component; 
        } 

        /// <internalonly/> 
        /// <devdoc>
        /// Set the content for the specified region
        /// </devdoc>
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            Debug.Assert(region != null);
            _zone.ZoneTemplate = ControlParser.ParseTemplate((IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)), content); 
            IsDirtyInternal = true; 
        }
 
        private sealed class WebPartEditableDesignerRegion : TemplatedEditableDesignerRegion {
            private WebPartZoneBase _zone;

            public WebPartEditableDesignerRegion(WebPartZoneBase zone, TemplateDefinition templateDefinition) 
                : base(templateDefinition) {
                _zone = zone; 
            } 

            public override ViewRendering GetChildViewRendering(Control control) { 
                if (control == null) {
                    throw new ArgumentNullException("control");
                }
 
                DesignerWebPartChrome chrome = new DesignerWebPartChrome(_zone);
                return chrome.GetViewRendering(control); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
