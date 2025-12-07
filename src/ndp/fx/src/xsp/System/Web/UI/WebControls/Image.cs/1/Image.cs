//------------------------------------------------------------------------------ 
// <copyright file="Image.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;
    using System.Web; 
    using System.Web.UI;
    using System.Drawing.Design; 
    using System.Security.Permissions; 

 
    /// <devdoc>
    ///    <para>Displays an image on a page.</para>
    /// </devdoc>
    [ 
    DefaultProperty("ImageUrl"),
    Designer("System.Web.UI.Design.WebControls.PreviewControlDesigner, " + AssemblyRef.SystemDesign), 
    ] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class Image : WebControl {

        private bool _urlResolved;
 

        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.Image'/> class.</para> 
        /// </devdoc>
        public Image() : base(HtmlTextWriterTag.Img) { 
        }


        /// <devdoc> 
        ///    <para>Specifies alternate text displayed when the image fails to load.</para>
        /// </devdoc> 
        [ 
#if ORCAS
//        Verification(guideline, checkpoint, VerificationReportLevel.Error, priority, message); 
        Verification("WCAG", "1.1", VerificationReportLevel.Error, 1, SR.Accessibility_ImageAlternateTextMissing),
        Verification("ADA", "1194.22(a)", VerificationReportLevel.Error, 1, SR.Accessibility_ImageAlternateTextMissing),
#endif
        Localizable(true), 
        Bindable(true),
        WebCategory("Appearance"), 
        DefaultValue(""), 
        WebSysDescription(SR.Image_AlternateText)
        ] 
        public virtual string AlternateText {
            get {
                string s = (string)ViewState["AlternateText"];
                return((s == null) ? String.Empty : s); 
            }
            set { 
                ViewState["AlternateText"] = value; 
            }
        } 


        /// <devdoc>
        /// </devdoc> 
        [
        DefaultValue(""), 
        Editor("System.Web.UI.Design.UrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)), 
        UrlProperty(),
        WebCategory("Accessibility"), 
        WebSysDescription(SR.Image_DescriptionUrl)
        ]
        public virtual string DescriptionUrl {
            get { 
                string s = (string)ViewState["DescriptionUrl"];
                return((s == null) ? String.Empty : s); 
            } 
            set {
                ViewState["DescriptionUrl"] = value; 
            }
        }

 
        /// <devdoc>
        ///    <para>Gets the font properties for the alternate text. This property is read-only.</para> 
        /// </devdoc> 
        [
        Browsable(false), 
        EditorBrowsableAttribute(EditorBrowsableState.Never),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public override FontInfo Font { 
            // Font is meaningless for image, so hide it from the developer by
            // making it non-browsable. 
            get { 
                return base.Font;
            } 
        }


        [ 
        Browsable(false),
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ] 
        public override bool Enabled {
            get { 
                return base.Enabled;
            }
            set {
                base.Enabled = value; 
            }
        } 
 
        [
        DefaultValue(false), 
        WebCategory("Accessibility"),
        WebSysDescription(SR.Image_GenerateEmptyAlternateText)
        ]
        public virtual bool GenerateEmptyAlternateText { 
            get {
                object o = ViewState["GenerateEmptyAlternateText"]; 
                return((o == null) ? false : (bool)o); 
            }
            set { 
                ViewState["GenerateEmptyAlternateText"] = value;
            }
        }
 

        /// <devdoc> 
        ///    <para>Gets or 
        ///       sets the alignment of the image within the text flow.</para>
        /// </devdoc> 
        [
        WebCategory("Layout"),
        DefaultValue(ImageAlign.NotSet),
        WebSysDescription(SR.Image_ImageAlign) 
        ]
        public virtual ImageAlign ImageAlign { 
            get { 
                object o = ViewState["ImageAlign"];
                return((o == null) ? ImageAlign.NotSet : (ImageAlign)o); 
            }
            set {
                if (value < ImageAlign.NotSet || value > ImageAlign.TextTop) {
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["ImageAlign"] = value; 
            } 
        }
 

        /// <devdoc>
        ///    <para>Gets or sets
        ///       the URL reference to the image to display.</para> 
        /// </devdoc>
        [ 
        Bindable(true), 
        WebCategory("Appearance"),
        DefaultValue(""), 
        Editor("System.Web.UI.Design.ImageUrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)),
        UrlProperty(),
        WebSysDescription(SR.Image_ImageUrl)
        ] 
        public virtual string ImageUrl {
            get { 
                string s = (string)ViewState["ImageUrl"]; 
                return((s == null) ? String.Empty : s);
            } 
            set {
                ViewState["ImageUrl"] = value;
            }
        } 

        // Perf work: Specially for AdRotator which uses the control while it 
        // resolves the url on its own. 
        internal bool UrlResolved {
            get { 
                return _urlResolved;
            }
            set {
                _urlResolved = value; 
            }
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// <para>Adds the attributes of an <see cref='System.Web.UI.WebControls.Image'/> to the output stream for rendering on
        ///    the client.</para>
        /// </devdoc> 
        protected override void AddAttributesToRender(HtmlTextWriter writer) {
            base.AddAttributesToRender(writer); 
 
            string s = ImageUrl;
            if (!UrlResolved) { 
                s = ResolveClientUrl(s);
            }
            if (s.Length > 0 || !EnableLegacyRendering) {
                writer.AddAttribute(HtmlTextWriterAttribute.Src, s); 
            }
 
            s = DescriptionUrl; 
            if (s.Length != 0) {
                writer.AddAttribute(HtmlTextWriterAttribute.Longdesc, ResolveClientUrl(s)); 
            }

            // always write out alt for accessibility purposes
            s = AlternateText; 
            if (s.Length > 0 || GenerateEmptyAlternateText) {
                writer.AddAttribute(HtmlTextWriterAttribute.Alt,s); 
            } 

            ImageAlign align = ImageAlign; 
            if (align != ImageAlign.NotSet) {
                string imageAlignValue;
                switch (align) {
                    case ImageAlign.Left: 
                        imageAlignValue = "left";
                        break; 
                    case ImageAlign.Right: 
                        imageAlignValue = "right";
                        break; 
                    case ImageAlign.Baseline:
                        imageAlignValue = "baseline";
                        break;
                    case ImageAlign.Top: 
                        imageAlignValue = "top";
                        break; 
                    case ImageAlign.Middle: 
                        imageAlignValue = "middle";
                        break; 
                    case ImageAlign.Bottom:
                        imageAlignValue = "bottom";
                        break;
                    case ImageAlign.AbsBottom: 
                        imageAlignValue = "absbottom";
                        break; 
                    case ImageAlign.AbsMiddle: 
                        imageAlignValue = "absmiddle";
                        break; 
                    default:
                        imageAlignValue = "texttop";
                        break;
                } 
                writer.AddAttribute(HtmlTextWriterAttribute.Align, imageAlignValue);
            } 
 
            if (BorderWidth.IsEmpty) {
                if (EnableLegacyRendering) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Border, "0", false);
                }
                else {
                    writer.AddStyleAttribute(HtmlTextWriterStyle.BorderWidth, "0px"); 
                }
            } 
        } 

 
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        protected internal override void RenderContents(HtmlTextWriter writer) { 
            // Do not render the children of a button since it does not
            // make sense to have children of an <input> tag. 
        } 
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="Image.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;
    using System.Web; 
    using System.Web.UI;
    using System.Drawing.Design; 
    using System.Security.Permissions; 

 
    /// <devdoc>
    ///    <para>Displays an image on a page.</para>
    /// </devdoc>
    [ 
    DefaultProperty("ImageUrl"),
    Designer("System.Web.UI.Design.WebControls.PreviewControlDesigner, " + AssemblyRef.SystemDesign), 
    ] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class Image : WebControl {

        private bool _urlResolved;
 

        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.Image'/> class.</para> 
        /// </devdoc>
        public Image() : base(HtmlTextWriterTag.Img) { 
        }


        /// <devdoc> 
        ///    <para>Specifies alternate text displayed when the image fails to load.</para>
        /// </devdoc> 
        [ 
#if ORCAS
//        Verification(guideline, checkpoint, VerificationReportLevel.Error, priority, message); 
        Verification("WCAG", "1.1", VerificationReportLevel.Error, 1, SR.Accessibility_ImageAlternateTextMissing),
        Verification("ADA", "1194.22(a)", VerificationReportLevel.Error, 1, SR.Accessibility_ImageAlternateTextMissing),
#endif
        Localizable(true), 
        Bindable(true),
        WebCategory("Appearance"), 
        DefaultValue(""), 
        WebSysDescription(SR.Image_AlternateText)
        ] 
        public virtual string AlternateText {
            get {
                string s = (string)ViewState["AlternateText"];
                return((s == null) ? String.Empty : s); 
            }
            set { 
                ViewState["AlternateText"] = value; 
            }
        } 


        /// <devdoc>
        /// </devdoc> 
        [
        DefaultValue(""), 
        Editor("System.Web.UI.Design.UrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)), 
        UrlProperty(),
        WebCategory("Accessibility"), 
        WebSysDescription(SR.Image_DescriptionUrl)
        ]
        public virtual string DescriptionUrl {
            get { 
                string s = (string)ViewState["DescriptionUrl"];
                return((s == null) ? String.Empty : s); 
            } 
            set {
                ViewState["DescriptionUrl"] = value; 
            }
        }

 
        /// <devdoc>
        ///    <para>Gets the font properties for the alternate text. This property is read-only.</para> 
        /// </devdoc> 
        [
        Browsable(false), 
        EditorBrowsableAttribute(EditorBrowsableState.Never),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public override FontInfo Font { 
            // Font is meaningless for image, so hide it from the developer by
            // making it non-browsable. 
            get { 
                return base.Font;
            } 
        }


        [ 
        Browsable(false),
        EditorBrowsableAttribute(EditorBrowsableState.Never) 
        ] 
        public override bool Enabled {
            get { 
                return base.Enabled;
            }
            set {
                base.Enabled = value; 
            }
        } 
 
        [
        DefaultValue(false), 
        WebCategory("Accessibility"),
        WebSysDescription(SR.Image_GenerateEmptyAlternateText)
        ]
        public virtual bool GenerateEmptyAlternateText { 
            get {
                object o = ViewState["GenerateEmptyAlternateText"]; 
                return((o == null) ? false : (bool)o); 
            }
            set { 
                ViewState["GenerateEmptyAlternateText"] = value;
            }
        }
 

        /// <devdoc> 
        ///    <para>Gets or 
        ///       sets the alignment of the image within the text flow.</para>
        /// </devdoc> 
        [
        WebCategory("Layout"),
        DefaultValue(ImageAlign.NotSet),
        WebSysDescription(SR.Image_ImageAlign) 
        ]
        public virtual ImageAlign ImageAlign { 
            get { 
                object o = ViewState["ImageAlign"];
                return((o == null) ? ImageAlign.NotSet : (ImageAlign)o); 
            }
            set {
                if (value < ImageAlign.NotSet || value > ImageAlign.TextTop) {
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["ImageAlign"] = value; 
            } 
        }
 

        /// <devdoc>
        ///    <para>Gets or sets
        ///       the URL reference to the image to display.</para> 
        /// </devdoc>
        [ 
        Bindable(true), 
        WebCategory("Appearance"),
        DefaultValue(""), 
        Editor("System.Web.UI.Design.ImageUrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)),
        UrlProperty(),
        WebSysDescription(SR.Image_ImageUrl)
        ] 
        public virtual string ImageUrl {
            get { 
                string s = (string)ViewState["ImageUrl"]; 
                return((s == null) ? String.Empty : s);
            } 
            set {
                ViewState["ImageUrl"] = value;
            }
        } 

        // Perf work: Specially for AdRotator which uses the control while it 
        // resolves the url on its own. 
        internal bool UrlResolved {
            get { 
                return _urlResolved;
            }
            set {
                _urlResolved = value; 
            }
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// <para>Adds the attributes of an <see cref='System.Web.UI.WebControls.Image'/> to the output stream for rendering on
        ///    the client.</para>
        /// </devdoc> 
        protected override void AddAttributesToRender(HtmlTextWriter writer) {
            base.AddAttributesToRender(writer); 
 
            string s = ImageUrl;
            if (!UrlResolved) { 
                s = ResolveClientUrl(s);
            }
            if (s.Length > 0 || !EnableLegacyRendering) {
                writer.AddAttribute(HtmlTextWriterAttribute.Src, s); 
            }
 
            s = DescriptionUrl; 
            if (s.Length != 0) {
                writer.AddAttribute(HtmlTextWriterAttribute.Longdesc, ResolveClientUrl(s)); 
            }

            // always write out alt for accessibility purposes
            s = AlternateText; 
            if (s.Length > 0 || GenerateEmptyAlternateText) {
                writer.AddAttribute(HtmlTextWriterAttribute.Alt,s); 
            } 

            ImageAlign align = ImageAlign; 
            if (align != ImageAlign.NotSet) {
                string imageAlignValue;
                switch (align) {
                    case ImageAlign.Left: 
                        imageAlignValue = "left";
                        break; 
                    case ImageAlign.Right: 
                        imageAlignValue = "right";
                        break; 
                    case ImageAlign.Baseline:
                        imageAlignValue = "baseline";
                        break;
                    case ImageAlign.Top: 
                        imageAlignValue = "top";
                        break; 
                    case ImageAlign.Middle: 
                        imageAlignValue = "middle";
                        break; 
                    case ImageAlign.Bottom:
                        imageAlignValue = "bottom";
                        break;
                    case ImageAlign.AbsBottom: 
                        imageAlignValue = "absbottom";
                        break; 
                    case ImageAlign.AbsMiddle: 
                        imageAlignValue = "absmiddle";
                        break; 
                    default:
                        imageAlignValue = "texttop";
                        break;
                } 
                writer.AddAttribute(HtmlTextWriterAttribute.Align, imageAlignValue);
            } 
 
            if (BorderWidth.IsEmpty) {
                if (EnableLegacyRendering) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Border, "0", false);
                }
                else {
                    writer.AddStyleAttribute(HtmlTextWriterStyle.BorderWidth, "0px"); 
                }
            } 
        } 

 
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        protected internal override void RenderContents(HtmlTextWriter writer) { 
            // Do not render the children of a button since it does not
            // make sense to have children of an <input> tag. 
        } 
    }
} 

