//------------------------------------------------------------------------------ 
// <copyright file="PanelContainerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\PanelContainerDesigner.uex' path='docs/doc[@for="PanelContainerDesigner"]/*' />
    public class PanelContainerDesigner : ContainerControlDesigner { 
 
        private const string PanelWithCaptionDesignTimeHtml =
            @"<div style=""{0}{2}{3}{4}{6}{10}"" class=""{11}""> 
    <fieldset>
        <legend>{5}</legend>
        <div {7}=0></div>
    </fieldset> 
</div>";
 
        private const string PanelNoCaptionDesignTimeHtml = 
            @"<div style=""{0}{2}{3}{4}{6}{10}"" class=""{11}"" {7}=0></div>";
 
        internal override string DesignTimeHtml {
            get {
                if (FrameCaption.Length > 0) {
                    return PanelWithCaptionDesignTimeHtml; 
                }
                return PanelNoCaptionDesignTimeHtml; 
            } 
        }
 
        /// <include file='doc\PanelContainerDesigner.uex' path='docs/doc[@for="PanelContainerDesigner.FrameCaption"]/*' />
        /// <internalonly />
        public override string FrameCaption {
            get { 
                return ((Panel)Component).GroupingText;
            } 
        } 

        /// <include file='doc\PanelContainerDesigner.uex' path='docs/doc[@for="PanelContainerDesigner.FrameStyle"]/*' /> 
        /// <internalonly />
        public override Style FrameStyle {
            get {
                if (((Panel)Component).GroupingText.Length == 0) { 
                    return new Style();
                } 
                else { 
                    return base.FrameStyle;
                } 
            }
        }

        protected override void AddDesignTimeCssAttributes(IDictionary styleAttributes) { 
            Panel panel = (Panel)Component;
            switch (panel.Direction) { 
                case ContentDirection.RightToLeft: 
                    styleAttributes["direction"] = "rtl";
                    break; 
                case ContentDirection.LeftToRight:
                    styleAttributes["direction"] = "ltr";
                    break;
            } 

            string s = panel.BackImageUrl; 
            if (s.Trim().Length > 0) { 
                IUrlResolutionService resolutionService = (IUrlResolutionService)GetService(typeof(IUrlResolutionService));
                if (resolutionService != null) { 
                    s = resolutionService.ResolveClientUrl(s);
                    styleAttributes["background-image"] = "url(" + s + ")";
                }
            } 

            switch (panel.ScrollBars) { 
                case ScrollBars.Horizontal: 
                    styleAttributes["overflow-x"] = "scroll";
                    break; 
                case ScrollBars.Vertical:
                    styleAttributes["overflow-y"] = "scroll";
                    break;
                case ScrollBars.Both: 
                    styleAttributes["overflow"] = "scroll";
                    break; 
                case ScrollBars.Auto: 
                    styleAttributes["overflow"] = "auto";
                    break; 
            }

            HorizontalAlign hAlign = panel.HorizontalAlign;
            if (hAlign != HorizontalAlign.NotSet) { 
                TypeConverter hac = TypeDescriptor.GetConverter(typeof(HorizontalAlign));
                styleAttributes["text-align"] = hac.ConvertToInvariantString(hAlign).ToLowerInvariant(); 
            } 

            if (!panel.Wrap) { 
                styleAttributes["white-space"] = "nowrap";
            }

            base.AddDesignTimeCssAttributes(styleAttributes); 
        }
 
        protected override bool UsePreviewControl { 
            get {
                return true; 
            }
        }

        /// <include file='doc\PanelContainerDesigner.uex' path='docs/doc[@for="PanelContainerDesigner.Initialize"]/*' /> 
        /// <internalonly />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(Panel)); 
            base.Initialize(component);
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PanelContainerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\PanelContainerDesigner.uex' path='docs/doc[@for="PanelContainerDesigner"]/*' />
    public class PanelContainerDesigner : ContainerControlDesigner { 
 
        private const string PanelWithCaptionDesignTimeHtml =
            @"<div style=""{0}{2}{3}{4}{6}{10}"" class=""{11}""> 
    <fieldset>
        <legend>{5}</legend>
        <div {7}=0></div>
    </fieldset> 
</div>";
 
        private const string PanelNoCaptionDesignTimeHtml = 
            @"<div style=""{0}{2}{3}{4}{6}{10}"" class=""{11}"" {7}=0></div>";
 
        internal override string DesignTimeHtml {
            get {
                if (FrameCaption.Length > 0) {
                    return PanelWithCaptionDesignTimeHtml; 
                }
                return PanelNoCaptionDesignTimeHtml; 
            } 
        }
 
        /// <include file='doc\PanelContainerDesigner.uex' path='docs/doc[@for="PanelContainerDesigner.FrameCaption"]/*' />
        /// <internalonly />
        public override string FrameCaption {
            get { 
                return ((Panel)Component).GroupingText;
            } 
        } 

        /// <include file='doc\PanelContainerDesigner.uex' path='docs/doc[@for="PanelContainerDesigner.FrameStyle"]/*' /> 
        /// <internalonly />
        public override Style FrameStyle {
            get {
                if (((Panel)Component).GroupingText.Length == 0) { 
                    return new Style();
                } 
                else { 
                    return base.FrameStyle;
                } 
            }
        }

        protected override void AddDesignTimeCssAttributes(IDictionary styleAttributes) { 
            Panel panel = (Panel)Component;
            switch (panel.Direction) { 
                case ContentDirection.RightToLeft: 
                    styleAttributes["direction"] = "rtl";
                    break; 
                case ContentDirection.LeftToRight:
                    styleAttributes["direction"] = "ltr";
                    break;
            } 

            string s = panel.BackImageUrl; 
            if (s.Trim().Length > 0) { 
                IUrlResolutionService resolutionService = (IUrlResolutionService)GetService(typeof(IUrlResolutionService));
                if (resolutionService != null) { 
                    s = resolutionService.ResolveClientUrl(s);
                    styleAttributes["background-image"] = "url(" + s + ")";
                }
            } 

            switch (panel.ScrollBars) { 
                case ScrollBars.Horizontal: 
                    styleAttributes["overflow-x"] = "scroll";
                    break; 
                case ScrollBars.Vertical:
                    styleAttributes["overflow-y"] = "scroll";
                    break;
                case ScrollBars.Both: 
                    styleAttributes["overflow"] = "scroll";
                    break; 
                case ScrollBars.Auto: 
                    styleAttributes["overflow"] = "auto";
                    break; 
            }

            HorizontalAlign hAlign = panel.HorizontalAlign;
            if (hAlign != HorizontalAlign.NotSet) { 
                TypeConverter hac = TypeDescriptor.GetConverter(typeof(HorizontalAlign));
                styleAttributes["text-align"] = hac.ConvertToInvariantString(hAlign).ToLowerInvariant(); 
            } 

            if (!panel.Wrap) { 
                styleAttributes["white-space"] = "nowrap";
            }

            base.AddDesignTimeCssAttributes(styleAttributes); 
        }
 
        protected override bool UsePreviewControl { 
            get {
                return true; 
            }
        }

        /// <include file='doc\PanelContainerDesigner.uex' path='docs/doc[@for="PanelContainerDesigner.Initialize"]/*' /> 
        /// <internalonly />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(Panel)); 
            base.Initialize(component);
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
