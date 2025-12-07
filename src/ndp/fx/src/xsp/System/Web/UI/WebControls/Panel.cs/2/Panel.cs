 
//------------------------------------------------------------------------------
// <copyright file="Panel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.WebControls { 

    using System; 
    using System.Web;
    using System.Web.UI;
    using System.Collections;
    using System.ComponentModel; 
    using System.Drawing.Design;
    using System.Globalization; 
    using System.Security.Permissions; 

 
    /// <devdoc>
    ///    <para>Constructs a panel for specifies layout regions
    ///       on a page and defines its properties.</para>
    /// </devdoc> 
    [
    Designer("System.Web.UI.Design.WebControls.PanelContainerDesigner, " + AssemblyRef.SystemDesign), 
    ParseChildren(false), 
    PersistChildren(true),
    ] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class Panel : WebControl
#if ORCAS 
        , IPaginationInfo, IPaginationContainer
#endif 
 { 

#if ORCAS 
        private int _maximumWeight;
#endif
        private string _defaultButton;
        private bool _renderedFieldSet = false; 

 
        /// <devdoc> 
        ///    Initializes a new instance of the <see cref='System.Web.UI.WebControls.Panel'/> class.
        /// </devdoc> 
        public Panel()
            : base(HtmlTextWriterTag.Div) {
#if ORCAS
            _maximumWeight = -1; 
#endif
        } 
 
#if ORCAS
 
        /// <devdoc>
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        [ 
        DefaultValue(true),
        Themeable(false), 
        WebCategory("Behavior"), 
        WebSysDescription(SR.Panel_AllowPaginate)
        ] 
        public virtual bool AllowPaginate {
            get {
                object u = ViewState["AllowPaginate"];
                return (u == null) ? true : (bool)u; 
            }
            set { 
                ViewState["AllowPaginate"] = value; 
            }
        } 
#endif


        /// <devdoc> 
        ///    <para>Gets or sets the URL of the background image for the panel control.</para>
        /// </devdoc> 
        [ 
        WebCategory("Appearance"),
        DefaultValue(""), 
        Editor("System.Web.UI.Design.ImageUrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)),
        UrlProperty(),
        WebSysDescription(SR.Panel_BackImageUrl)
        ] 
        public virtual string BackImageUrl {
            get { 
                if (ControlStyleCreated == false) { 
                    return String.Empty;
                } 
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) {
                    return panelStyle.BackImageUrl;
                } 
                string s = (string)ViewState["BackImageUrl"];
                return((s == null) ? String.Empty : s); 
            } 
            set {
                PanelStyle panelStyle = ControlStyle as PanelStyle; 
                if (panelStyle != null) {
                    panelStyle.BackImageUrl = value;
                }
                else { 
                    ViewState["BackImageUrl"] = value;
                } 
            } 
        }
 

        /// <devdoc>
        ///     Gets or sets default button for the panel
        /// </devdoc> 
        [
        DefaultValue(""), 
        Themeable(false), 
        WebCategory("Behavior"),
        WebSysDescription(SR.Panel_DefaultButton) 
        ]
        public virtual string DefaultButton {
            get {
                if (_defaultButton == null) { 
                    return String.Empty;
                } 
                return _defaultButton; 
            }
            set { 
                _defaultButton = value;
            }
        }
 
        /// <devdoc>
        /// <para>Gets or sets the direction of text in the panel</para> 
        /// </devdoc> 
        [
        DefaultValue(ContentDirection.NotSet), 
        WebCategory("Layout"),
        WebSysDescription(SR.Panel_Direction)
        ]
        public virtual ContentDirection Direction { 
            get {
                if (ControlStyleCreated == false) { 
                    return ContentDirection.NotSet; 
                }
                PanelStyle panelStyle = ControlStyle as PanelStyle; 
                if (panelStyle != null) {
                    return panelStyle.Direction;
                }
                object direction = ViewState["Direction"]; 
                return direction == null ? ContentDirection.NotSet : (ContentDirection) direction;
            } 
            set { 
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) { 
                    panelStyle.Direction = value;
                }
                else {
                    ViewState["Direction"] = value; 
                }
            } 
        } 

 
        [
        Localizable(true),
        DefaultValue(""),
        WebCategory("Appearance"), 
        WebSysDescription(SR.Panel_GroupingText)
        ] 
        public virtual string GroupingText { 
            get {
                string s = (string)ViewState["GroupingText"]; 
                return (s != null) ? s : String.Empty;
            }
            set {
                ViewState["GroupingText"] = value; 
            }
        } 
 

        /// <devdoc> 
        ///    <para>Gets or sets the horizontal alignment of the contents within the panel.</para>
        /// </devdoc>
        [
        WebCategory("Layout"), 
        DefaultValue(HorizontalAlign.NotSet),
        WebSysDescription(SR.Panel_HorizontalAlign) 
        ] 
        public virtual HorizontalAlign HorizontalAlign {
            get { 
                if (ControlStyleCreated == false) {
                    return HorizontalAlign.NotSet;
                }
                PanelStyle panelStyle = ControlStyle as PanelStyle; 
                if (panelStyle != null) {
                    return panelStyle.HorizontalAlign; 
                } 
                object o = ViewState["HorizontalAlign"];
                return ((o == null) ? HorizontalAlign.NotSet : (HorizontalAlign)o); 
            }
            set {
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) { 
                    panelStyle.HorizontalAlign = value;
                } 
                else { 
                    if (value < HorizontalAlign.NotSet || value > HorizontalAlign.Justify) {
                        throw new ArgumentOutOfRangeException("value"); 
                    }
                    ViewState["HorizontalAlign"] = value;
                }
            } 
        }
 
 
        /// <devdoc>
        /// <para>Gets or sets the scrollbar behavior of the panel.</para> 
        /// </devdoc>
        [
        DefaultValue(ScrollBars.None),
        WebCategory("Layout"), 
        WebSysDescription(SR.Panel_ScrollBars)
        ] 
        public virtual ScrollBars ScrollBars { 
            get {
                if (ControlStyleCreated == false) { 
                    return ScrollBars.None;
                }
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) { 
                    return panelStyle.ScrollBars;
                } 
                object scroll = ViewState["ScrollBars"]; 
                return ((scroll == null) ? ScrollBars.None : (ScrollBars)scroll);
            } 
            set {
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) {
                    panelStyle.ScrollBars = value; 
                }
                else { 
                    ViewState["ScrollBars"] = value; 
                }
            } 
        }


        /// <devdoc> 
        ///    <para>Gets or sets a value
        ///       indicating whether the content wraps within the panel.</para> 
        /// </devdoc> 
        [
        WebCategory("Layout"), 
        DefaultValue(true),
        WebSysDescription(SR.Panel_Wrap)
        ]
        public virtual bool Wrap { 
            get {
                if (ControlStyleCreated == false) { 
                    return true; 
                }
                PanelStyle panelStyle = ControlStyle as PanelStyle; 
                if (panelStyle != null) {
                    return panelStyle.Wrap;
                }
                object b = ViewState["Wrap"]; 
                return ((b == null) ? true : (bool)b);
            } 
            set { 
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) { 
                    panelStyle.Wrap = value;
                }
                else {
                    ViewState["Wrap"] = value; 
                }
            } 
        } 

 
        /// <internalonly/>
        /// <devdoc>
        ///    Add background-image to list of style attributes to render.
        ///    Add align and nowrap to list of attributes to render. 
        /// </devdoc>
        protected override void AddAttributesToRender(HtmlTextWriter writer) { 
            base.AddAttributesToRender(writer); 

            string s = BackImageUrl; 
            // Whidbey 12856
            if (s.Trim().Length > 0) {
                writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundImage, "url(" + ResolveClientUrl(s) + ")");
            } 

            AddScrollingAttribute(ScrollBars, writer); 
 
            HorizontalAlign hAlign = HorizontalAlign;
            if (hAlign != HorizontalAlign.NotSet) { 
                TypeConverter hac = TypeDescriptor.GetConverter(typeof(HorizontalAlign));
                writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, hac.ConvertToInvariantString(hAlign).ToLowerInvariant());
            }
 
            if (!Wrap) {
                if (EnableLegacyRendering) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "nowrap", false); 
                }
                else { 
                    writer.AddStyleAttribute(HtmlTextWriterStyle.WhiteSpace, "nowrap");
                }
            }
 
            if (Direction == ContentDirection.LeftToRight) {
                writer.AddAttribute(HtmlTextWriterAttribute.Dir, "ltr"); 
            } 
            else if (Direction == ContentDirection.RightToLeft) {
                writer.AddAttribute(HtmlTextWriterAttribute.Dir, "rtl"); 
            }

            if (!DesignMode &&
                (Page != null) && 
                (Page.RequestInternal != null) &&
                (Page.Request.Browser.EcmaScriptVersion.Major > 0) && 
                (Page.Request.Browser.W3CDomVersion.Major > 0)) { 
                if (DefaultButton.Length > 0) {
                    Control c = FindControl(DefaultButton); 
                    if (c is IButtonControl) {
                        Page.ClientScript.RegisterDefaultButtonScript(c, writer, true /* UseAddAttribute */);
                    }
                    else { 
                        throw new InvalidOperationException(SR.GetString(SR.HtmlForm_OnlyIButtonControlCanBeDefaultButton, ID));
                    } 
                } 
            }
        } 

        /// <devdoc>
        ///    [To be supplied.]
        /// </devdoc> 
        private void AddScrollingAttribute(ScrollBars scrollBars, HtmlTextWriter writer) {
            switch (scrollBars) { 
                case ScrollBars.Horizontal: 
                    writer.AddStyleAttribute(HtmlTextWriterStyle.OverflowX, "scroll");
                    break; 
                case ScrollBars.Vertical:
                    writer.AddStyleAttribute(HtmlTextWriterStyle.OverflowY, "scroll");
                    break;
                case ScrollBars.Both: 
                    writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "scroll");
                    break; 
                case ScrollBars.Auto: 
                    writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
                    break; 
                default:
                    break;
            }
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        ///    <para>A protected method. Creates a panel control style.</para> 
        /// </devdoc>
        protected override Style CreateControlStyle() {
            return new PanelStyle(ViewState);
        } 

 
        /// <internalonly/> 
        public override void RenderBeginTag(HtmlTextWriter writer) {
            AddAttributesToRender(writer); 

            HtmlTextWriterTag tagKey = TagKey;
            if (tagKey != HtmlTextWriterTag.Unknown) {
                writer.RenderBeginTag(tagKey); 
            }
            else { 
                writer.RenderBeginTag(TagName); 
            }
 
            string s = GroupingText;
            bool useGrouping = (s.Length != 0) && !(writer is Html32TextWriter);
            if (useGrouping) {
                writer.RenderBeginTag(HtmlTextWriterTag.Fieldset); 
                _renderedFieldSet = true;
                writer.RenderBeginTag(HtmlTextWriterTag.Legend); 
                writer.Write(s); 
                writer.RenderEndTag();
            } 
        }

        public override void RenderEndTag(HtmlTextWriter writer) {
            if (_renderedFieldSet) { 
                writer.RenderEndTag();
            } 
            base.RenderEndTag(writer); 
        }
 
#if ORCAS

        /// <internalonly/>
        int IPaginationContainer.MaximumWeight { 
            get {
                return MaximumWeight; 
            } 
        }
 

        /// <internalonly/>
        protected virtual int MaximumWeight {
            get { 
                if (_maximumWeight != -1) {
                    return _maximumWeight; 
                } 
                if(Context != null) {
                    _maximumWeight = Int32.Parse(Context.Request.Browser["optimumPageWeight"], CultureInfo.InvariantCulture); 
                    return _maximumWeight;
                    //
                }
                else { 
                    return -1;
                } 
            } 
        }
 


        /// <internalonly/>
        bool IPaginationInfo.PaginateChildren { 
            get {
                return PaginateChildren; 
            } 
        }
 

        /// <internalonly/>
        protected virtual bool PaginateChildren {
            get { 
                return AllowPaginate;
            } 
        } 

 
        /// <internalonly/>
        int IPaginationInfo.Weight {
            get {
                return Weight; 
            }
        } 
 

        protected virtual int Weight { 
            get {
                return 0;
            }
        } 
#endif
    } 
} 
 
//------------------------------------------------------------------------------
// <copyright file="Panel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.WebControls { 

    using System; 
    using System.Web;
    using System.Web.UI;
    using System.Collections;
    using System.ComponentModel; 
    using System.Drawing.Design;
    using System.Globalization; 
    using System.Security.Permissions; 

 
    /// <devdoc>
    ///    <para>Constructs a panel for specifies layout regions
    ///       on a page and defines its properties.</para>
    /// </devdoc> 
    [
    Designer("System.Web.UI.Design.WebControls.PanelContainerDesigner, " + AssemblyRef.SystemDesign), 
    ParseChildren(false), 
    PersistChildren(true),
    ] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class Panel : WebControl
#if ORCAS 
        , IPaginationInfo, IPaginationContainer
#endif 
 { 

#if ORCAS 
        private int _maximumWeight;
#endif
        private string _defaultButton;
        private bool _renderedFieldSet = false; 

 
        /// <devdoc> 
        ///    Initializes a new instance of the <see cref='System.Web.UI.WebControls.Panel'/> class.
        /// </devdoc> 
        public Panel()
            : base(HtmlTextWriterTag.Div) {
#if ORCAS
            _maximumWeight = -1; 
#endif
        } 
 
#if ORCAS
 
        /// <devdoc>
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        [ 
        DefaultValue(true),
        Themeable(false), 
        WebCategory("Behavior"), 
        WebSysDescription(SR.Panel_AllowPaginate)
        ] 
        public virtual bool AllowPaginate {
            get {
                object u = ViewState["AllowPaginate"];
                return (u == null) ? true : (bool)u; 
            }
            set { 
                ViewState["AllowPaginate"] = value; 
            }
        } 
#endif


        /// <devdoc> 
        ///    <para>Gets or sets the URL of the background image for the panel control.</para>
        /// </devdoc> 
        [ 
        WebCategory("Appearance"),
        DefaultValue(""), 
        Editor("System.Web.UI.Design.ImageUrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)),
        UrlProperty(),
        WebSysDescription(SR.Panel_BackImageUrl)
        ] 
        public virtual string BackImageUrl {
            get { 
                if (ControlStyleCreated == false) { 
                    return String.Empty;
                } 
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) {
                    return panelStyle.BackImageUrl;
                } 
                string s = (string)ViewState["BackImageUrl"];
                return((s == null) ? String.Empty : s); 
            } 
            set {
                PanelStyle panelStyle = ControlStyle as PanelStyle; 
                if (panelStyle != null) {
                    panelStyle.BackImageUrl = value;
                }
                else { 
                    ViewState["BackImageUrl"] = value;
                } 
            } 
        }
 

        /// <devdoc>
        ///     Gets or sets default button for the panel
        /// </devdoc> 
        [
        DefaultValue(""), 
        Themeable(false), 
        WebCategory("Behavior"),
        WebSysDescription(SR.Panel_DefaultButton) 
        ]
        public virtual string DefaultButton {
            get {
                if (_defaultButton == null) { 
                    return String.Empty;
                } 
                return _defaultButton; 
            }
            set { 
                _defaultButton = value;
            }
        }
 
        /// <devdoc>
        /// <para>Gets or sets the direction of text in the panel</para> 
        /// </devdoc> 
        [
        DefaultValue(ContentDirection.NotSet), 
        WebCategory("Layout"),
        WebSysDescription(SR.Panel_Direction)
        ]
        public virtual ContentDirection Direction { 
            get {
                if (ControlStyleCreated == false) { 
                    return ContentDirection.NotSet; 
                }
                PanelStyle panelStyle = ControlStyle as PanelStyle; 
                if (panelStyle != null) {
                    return panelStyle.Direction;
                }
                object direction = ViewState["Direction"]; 
                return direction == null ? ContentDirection.NotSet : (ContentDirection) direction;
            } 
            set { 
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) { 
                    panelStyle.Direction = value;
                }
                else {
                    ViewState["Direction"] = value; 
                }
            } 
        } 

 
        [
        Localizable(true),
        DefaultValue(""),
        WebCategory("Appearance"), 
        WebSysDescription(SR.Panel_GroupingText)
        ] 
        public virtual string GroupingText { 
            get {
                string s = (string)ViewState["GroupingText"]; 
                return (s != null) ? s : String.Empty;
            }
            set {
                ViewState["GroupingText"] = value; 
            }
        } 
 

        /// <devdoc> 
        ///    <para>Gets or sets the horizontal alignment of the contents within the panel.</para>
        /// </devdoc>
        [
        WebCategory("Layout"), 
        DefaultValue(HorizontalAlign.NotSet),
        WebSysDescription(SR.Panel_HorizontalAlign) 
        ] 
        public virtual HorizontalAlign HorizontalAlign {
            get { 
                if (ControlStyleCreated == false) {
                    return HorizontalAlign.NotSet;
                }
                PanelStyle panelStyle = ControlStyle as PanelStyle; 
                if (panelStyle != null) {
                    return panelStyle.HorizontalAlign; 
                } 
                object o = ViewState["HorizontalAlign"];
                return ((o == null) ? HorizontalAlign.NotSet : (HorizontalAlign)o); 
            }
            set {
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) { 
                    panelStyle.HorizontalAlign = value;
                } 
                else { 
                    if (value < HorizontalAlign.NotSet || value > HorizontalAlign.Justify) {
                        throw new ArgumentOutOfRangeException("value"); 
                    }
                    ViewState["HorizontalAlign"] = value;
                }
            } 
        }
 
 
        /// <devdoc>
        /// <para>Gets or sets the scrollbar behavior of the panel.</para> 
        /// </devdoc>
        [
        DefaultValue(ScrollBars.None),
        WebCategory("Layout"), 
        WebSysDescription(SR.Panel_ScrollBars)
        ] 
        public virtual ScrollBars ScrollBars { 
            get {
                if (ControlStyleCreated == false) { 
                    return ScrollBars.None;
                }
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) { 
                    return panelStyle.ScrollBars;
                } 
                object scroll = ViewState["ScrollBars"]; 
                return ((scroll == null) ? ScrollBars.None : (ScrollBars)scroll);
            } 
            set {
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) {
                    panelStyle.ScrollBars = value; 
                }
                else { 
                    ViewState["ScrollBars"] = value; 
                }
            } 
        }


        /// <devdoc> 
        ///    <para>Gets or sets a value
        ///       indicating whether the content wraps within the panel.</para> 
        /// </devdoc> 
        [
        WebCategory("Layout"), 
        DefaultValue(true),
        WebSysDescription(SR.Panel_Wrap)
        ]
        public virtual bool Wrap { 
            get {
                if (ControlStyleCreated == false) { 
                    return true; 
                }
                PanelStyle panelStyle = ControlStyle as PanelStyle; 
                if (panelStyle != null) {
                    return panelStyle.Wrap;
                }
                object b = ViewState["Wrap"]; 
                return ((b == null) ? true : (bool)b);
            } 
            set { 
                PanelStyle panelStyle = ControlStyle as PanelStyle;
                if (panelStyle != null) { 
                    panelStyle.Wrap = value;
                }
                else {
                    ViewState["Wrap"] = value; 
                }
            } 
        } 

 
        /// <internalonly/>
        /// <devdoc>
        ///    Add background-image to list of style attributes to render.
        ///    Add align and nowrap to list of attributes to render. 
        /// </devdoc>
        protected override void AddAttributesToRender(HtmlTextWriter writer) { 
            base.AddAttributesToRender(writer); 

            string s = BackImageUrl; 
            // Whidbey 12856
            if (s.Trim().Length > 0) {
                writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundImage, "url(" + ResolveClientUrl(s) + ")");
            } 

            AddScrollingAttribute(ScrollBars, writer); 
 
            HorizontalAlign hAlign = HorizontalAlign;
            if (hAlign != HorizontalAlign.NotSet) { 
                TypeConverter hac = TypeDescriptor.GetConverter(typeof(HorizontalAlign));
                writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, hac.ConvertToInvariantString(hAlign).ToLowerInvariant());
            }
 
            if (!Wrap) {
                if (EnableLegacyRendering) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "nowrap", false); 
                }
                else { 
                    writer.AddStyleAttribute(HtmlTextWriterStyle.WhiteSpace, "nowrap");
                }
            }
 
            if (Direction == ContentDirection.LeftToRight) {
                writer.AddAttribute(HtmlTextWriterAttribute.Dir, "ltr"); 
            } 
            else if (Direction == ContentDirection.RightToLeft) {
                writer.AddAttribute(HtmlTextWriterAttribute.Dir, "rtl"); 
            }

            if (!DesignMode &&
                (Page != null) && 
                (Page.RequestInternal != null) &&
                (Page.Request.Browser.EcmaScriptVersion.Major > 0) && 
                (Page.Request.Browser.W3CDomVersion.Major > 0)) { 
                if (DefaultButton.Length > 0) {
                    Control c = FindControl(DefaultButton); 
                    if (c is IButtonControl) {
                        Page.ClientScript.RegisterDefaultButtonScript(c, writer, true /* UseAddAttribute */);
                    }
                    else { 
                        throw new InvalidOperationException(SR.GetString(SR.HtmlForm_OnlyIButtonControlCanBeDefaultButton, ID));
                    } 
                } 
            }
        } 

        /// <devdoc>
        ///    [To be supplied.]
        /// </devdoc> 
        private void AddScrollingAttribute(ScrollBars scrollBars, HtmlTextWriter writer) {
            switch (scrollBars) { 
                case ScrollBars.Horizontal: 
                    writer.AddStyleAttribute(HtmlTextWriterStyle.OverflowX, "scroll");
                    break; 
                case ScrollBars.Vertical:
                    writer.AddStyleAttribute(HtmlTextWriterStyle.OverflowY, "scroll");
                    break;
                case ScrollBars.Both: 
                    writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "scroll");
                    break; 
                case ScrollBars.Auto: 
                    writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
                    break; 
                default:
                    break;
            }
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        ///    <para>A protected method. Creates a panel control style.</para> 
        /// </devdoc>
        protected override Style CreateControlStyle() {
            return new PanelStyle(ViewState);
        } 

 
        /// <internalonly/> 
        public override void RenderBeginTag(HtmlTextWriter writer) {
            AddAttributesToRender(writer); 

            HtmlTextWriterTag tagKey = TagKey;
            if (tagKey != HtmlTextWriterTag.Unknown) {
                writer.RenderBeginTag(tagKey); 
            }
            else { 
                writer.RenderBeginTag(TagName); 
            }
 
            string s = GroupingText;
            bool useGrouping = (s.Length != 0) && !(writer is Html32TextWriter);
            if (useGrouping) {
                writer.RenderBeginTag(HtmlTextWriterTag.Fieldset); 
                _renderedFieldSet = true;
                writer.RenderBeginTag(HtmlTextWriterTag.Legend); 
                writer.Write(s); 
                writer.RenderEndTag();
            } 
        }

        public override void RenderEndTag(HtmlTextWriter writer) {
            if (_renderedFieldSet) { 
                writer.RenderEndTag();
            } 
            base.RenderEndTag(writer); 
        }
 
#if ORCAS

        /// <internalonly/>
        int IPaginationContainer.MaximumWeight { 
            get {
                return MaximumWeight; 
            } 
        }
 

        /// <internalonly/>
        protected virtual int MaximumWeight {
            get { 
                if (_maximumWeight != -1) {
                    return _maximumWeight; 
                } 
                if(Context != null) {
                    _maximumWeight = Int32.Parse(Context.Request.Browser["optimumPageWeight"], CultureInfo.InvariantCulture); 
                    return _maximumWeight;
                    //
                }
                else { 
                    return -1;
                } 
            } 
        }
 


        /// <internalonly/>
        bool IPaginationInfo.PaginateChildren { 
            get {
                return PaginateChildren; 
            } 
        }
 

        /// <internalonly/>
        protected virtual bool PaginateChildren {
            get { 
                return AllowPaginate;
            } 
        } 

 
        /// <internalonly/>
        int IPaginationInfo.Weight {
            get {
                return Weight; 
            }
        } 
 

        protected virtual int Weight { 
            get {
                return 0;
            }
        } 
#endif
    } 
} 
