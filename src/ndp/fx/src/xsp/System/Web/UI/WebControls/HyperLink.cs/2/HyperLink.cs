//------------------------------------------------------------------------------ 
// <copyright file="HyperLink.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Web;
    using System.Web.UI;
    using System.Web.Util;
    using System.Drawing.Design; 
    using System.Text;
    using System.Security.Permissions; 
 

    /// <devdoc> 
    /// <para>Interacts with the parser to build a <see cref='System.Web.UI.WebControls.HyperLink'/>.</para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class HyperLinkControlBuilder : ControlBuilder {
 
 
        /// <devdoc>
        ///    <para>Gets a value to indicate whether or not white spaces are allowed in literals for this control. This 
        ///       property is read-only.</para>
        /// </devdoc>
        public override bool AllowWhitespaceLiterals() {
            return false; 
        }
    } 
 

 
    /// <devdoc>
    ///    <para>Creates a link for the browser to navigate to another page.</para>
    /// </devdoc>
    [ 
    ControlBuilderAttribute(typeof(HyperLinkControlBuilder)),
    DataBindingHandler("System.Web.UI.Design.HyperLinkDataBindingHandler, " + AssemblyRef.SystemDesign), 
    DefaultProperty("Text"), 
    Designer("System.Web.UI.Design.WebControls.HyperLinkDesigner, " + AssemblyRef.SystemDesign),
    ToolboxData("<{0}:HyperLink runat=\"server\">HyperLink</{0}:HyperLink>"), 
    ParseChildren(false)
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class HyperLink : WebControl {
 
#if SHIPPINGADAPTERS 
        private string _renderHref = null;
        private bool _urlResolved = false; 
#endif


        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.HyperLink'/> class.</para>
        /// </devdoc> 
        public HyperLink() : base(HtmlTextWriterTag.A) { 
        }
 
#if SITECOUNTERS

        [
        WebCategory("SiteCounters"), 
        DefaultValue("HyperLink"),
        Themeable(false), 
        WebSysDescription(SR.Control_For_SiteCounters_CounterGroup), 
        ]
        public virtual String CounterGroup { 
            get {
                String s = (String)ViewState["CounterGroup"];
                return((s == null) ? "HyperLink" : s);
            } 
            set {
                ViewState["CounterGroup"] = value; 
            } 
        }
 

        /// <devdoc>
        ///    <para>Gets or sets the target name for qualifying click-through counters.</para>
        /// </devdoc> 
        [
        WebCategory("SiteCounters"), 
        DefaultValue(""), 
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_CounterName), 
        ]
        public String CounterName {
            get {
                String s = (String)ViewState["CounterName"]; 
                return((s == null) ? String.Empty : s);
            } 
            set { 
                ViewState["CounterName"] = value;
            } 
        }


        [ 
        WebCategory("SiteCounters"),
        DefaultValue(false), 
        Themeable(false), 
        WebSysDescription(SR.Control_For_SiteCounters_CountClicks),
        ] 
        public bool CountClicks {
            get {
                object b = ViewState["CountClicks"];
                return((b == null) ? false : (bool)b); 
            }
            set { 
                ViewState["CountClicks"] = value; 
            }
        } 
#endif


        /// <devdoc> 
        ///    <para>Gets or sets the URL reference to an image to display as an alternative to plain text for the
        ///       hyperlink.</para> 
        /// </devdoc> 
        [
        Bindable(true), 
        WebCategory("Appearance"),
        DefaultValue(""),
        Editor("System.Web.UI.Design.ImageUrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)),
        UrlProperty(), 
        WebSysDescription(SR.HyperLink_ImageUrl)
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
 

 
        /// <devdoc>
        ///    <para>Gets or sets the URL reference to navigate to when the hyperlink is clicked.</para>
        /// </devdoc>
        [ 
        Bindable(true),
        WebCategory("Navigation"), 
        DefaultValue(""), 
        Editor("System.Web.UI.Design.UrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)),
        UrlProperty(), 
        WebSysDescription(SR.HyperLink_NavigateUrl)
        ]
        public string NavigateUrl {
            get { 
                string s = (string)ViewState["NavigateUrl"];
                return((s == null) ? String.Empty : s); 
            } 
            set {
                ViewState["NavigateUrl"] = value; 
            }
        }

 
        internal override bool RequiresLegacyRendering {
            get { 
                return true; 
            }
        } 


#if SITECOUNTERS
 
        [
        WebCategory("SiteCounters"), 
        DefaultValue(-1), 
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_RowsPerDay), 
        ]
        public int RowsPerDay {
            get {
                Object o = ViewState["RowsPerDay"]; 
                return((o == null) ? -1 : (int) o);
            } 
            set { 
                if (value == 0) {
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["RowsPerDay"] = value;
            }
        } 

 
        [ 
        WebCategory("SiteCounters"),
        DefaultValue(""), 
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_SiteCountersProvider)
        ]
        public String SiteCountersProvider { 
            get {
                String s = (String) ViewState["SiteCountersProvider"]; 
                return((s != null) ? s : String.Empty); 
            }
            set { 
                ViewState["SiteCountersProvider"] = value;
            }
        }
#endif 

 
        /// <devdoc> 
        ///    <para>Gets or sets the target window or frame the contents of
        ///       the <see cref='System.Web.UI.WebControls.HyperLink'/> will be displayed into when clicked.</para> 
        /// </devdoc>
        [
        WebCategory("Navigation"),
        DefaultValue(""), 
        WebSysDescription(SR.HyperLink_Target),
        TypeConverter(typeof(TargetConverter)) 
        ] 
        public string Target {
            get { 
                string s = (string)ViewState["Target"];
                return((s == null) ? String.Empty : s);
            }
            set { 
                ViewState["Target"] = value;
            } 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the text displayed for the <see cref='System.Web.UI.WebControls.HyperLink'/>.</para>
        /// </devdoc> 
        [
        Localizable(true), 
        Bindable(true), 
        WebCategory("Appearance"),
        DefaultValue(""), 
        WebSysDescription(SR.HyperLink_Text),
        PersistenceMode(PersistenceMode.InnerDefaultProperty)
        ]
        public virtual string Text { 
            get {
                object o = ViewState["Text"]; 
                return((o == null) ? String.Empty : (string)o); 
            }
            set { 
                if (HasControls()) {
                    Controls.Clear();
                }
                ViewState["Text"] = value; 
            }
        } 
#if SITECOUNTERS 

        [ 
        WebCategory("SiteCounters"),
        DefaultValue(true),
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_TrackApplicationName), 
        ]
        public bool TrackApplicationName { 
            get { 
                object b = ViewState["TrackApplicationName"];
                return((b == null) ? true : (bool)b); 
            }
            set {
                ViewState["TrackApplicationName"] = value;
            } 
        }
 
 
        [
        WebCategory("SiteCounters"), 
        DefaultValue(true),
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_TrackNavigateUrl),
        ] 
        public bool TrackNavigateUrl {
            get { 
                object b = ViewState["TrackNavigateUrl"]; 
                return((b == null) ? true : (bool)b);
            } 
            set {
                ViewState["TrackNavigateUrl"] = value;
            }
        } 

 
        [ 
        WebCategory("SiteCounters"),
        DefaultValue(true), 
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_TrackPageUrl),
        ]
        public bool TrackPageUrl { 
            get {
                object b = ViewState["TrackPageUrl"]; 
                return((b == null) ? true : (bool)b); 
            }
            set { 
                ViewState["TrackPageUrl"] = value;
            }
        }
#endif 
#if SHIPPINGADAPTERS
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
#endif 

 
        /// <internalonly/>
        /// <devdoc>
        /// <para>Adds the attribututes of the a <see cref='System.Web.UI.WebControls.HyperLink'/> to the output
        ///    stream for rendering.</para> 
        /// </devdoc>
        protected override void AddAttributesToRender(HtmlTextWriter writer) { 
            if (Enabled && !IsEnabled) { 
                // We need to do the cascade effect on the server, because the browser
                // only renders as disabled, but doesn't disable the functionality. 
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
            }

            base.AddAttributesToRender(writer); 

#if SHIPPINGADAPTERS 
            string s = (!String.IsNullOrEmpty(_renderHref)) ? _renderHref : NavigateUrl; 
#else
            string s = NavigateUrl; 
#endif
            if ((s.Length > 0) && IsEnabled) {
#if SHIPPINGADAPTERS
                string resolvedUrl = (UrlResolved) ? s : ResolveClientUrl(s); 
#else
                string resolvedUrl = ResolveClientUrl(s); 
#endif 
#if SITECOUNTERS
                resolvedUrl = GetCountClickUrl(resolvedUrl); 
#endif
                writer.AddAttribute(HtmlTextWriterAttribute.Href, resolvedUrl);
            }
            s = Target; 
            if (s.Length > 0) {
                writer.AddAttribute(HtmlTextWriterAttribute.Target, s); 
            } 
        }
 
        protected override void AddParsedSubObject(object obj) {
            if (HasControls()) {
                base.AddParsedSubObject(obj);
            } 
            else {
                if (obj is LiteralControl) { 
                    Text = ((LiteralControl)obj).Text; 
                }
                else { 
                    string currentText = Text;
                    if (currentText.Length != 0) {
                        Text = String.Empty;
                        base.AddParsedSubObject(new LiteralControl(currentText)); 
                    }
                    base.AddParsedSubObject(obj); 
                } 
            }
        } 

#if SITECOUNTERS
        internal string GetCountClickUrl(string url) {
            if (CountClicks && url != null && url.Length > 0) { 
                HttpContext context = Context;
                SiteCounters siteCounters = (context == null) ? null : context.SiteCounters; 
                if (siteCounters != null && siteCounters.Enabled) { 
                    String counterName = CounterName;
                    if (counterName != null && counterName.Length == 0) { 
                        if (UrlPath.IsRelativeUrl(url)) {
                            // Make the path absolute for data reporting
                            counterName = UrlPath.Combine(context.Request.FilePathObject, url);
                        } 
                        else {
                            counterName = url; 
                        } 
                    }
                    url = siteCounters.GetRedirectUrl( 
                                        CounterGroup, counterName, SiteCounters.ClickEventText,
                                        url, TrackApplicationName,
                                        TrackPageUrl, TrackNavigateUrl,
                                        SiteCountersProvider, RowsPerDay); 
                }
            } 
            return url; 
        }
#endif 

        /// <internalonly/>
        /// <devdoc>
        ///    Load previously saved state. 
        ///    Overridden to synchronize Text property with LiteralContent.
        /// </devdoc> 
        protected override void LoadViewState(object savedState) { 
            if (savedState != null) {
                base.LoadViewState(savedState); 
                string s = (string)ViewState["Text"];
                if (s != null)
                    Text = s;
            } 
        }
 
#if SITECOUNTERS 

        protected internal override void OnPreRender(EventArgs e) { 
            base.OnPreRender(e);

            // VSWhidbey 83401: Check if the url is supported for redirect when
            // CountClicks is true. 
            if (CountClicks && !UrlPath.IsPathRedirectSupported(NavigateUrl)) {
                throw new HttpException( 
                    SR.GetString( 
                        SR.SiteCounters_url_not_supported_for_redirect, NavigateUrl));
            } 
        }
#endif

        /// <internalonly/> 
        /// <devdoc>
        /// <para>Displays the <see cref='System.Web.UI.WebControls.HyperLink'/> on a page.</para> 
        /// </devdoc> 
        protected internal override void RenderContents(HtmlTextWriter writer) {
            string s = ImageUrl; 
            if (s.Length > 0) {
                Image img = new Image();

                // NOTE: The Url resolution happens right here, because the image is not parented 
                //       and will not be able to resolve when it tries to do so.
                img.ImageUrl = ResolveClientUrl(s); 
 
                s = ToolTip;
                if (s.Length != 0) { 
                    img.ToolTip = s;
                }

                s = Text; 
                if (s.Length != 0) {
                    img.AlternateText = s; 
                } 
                img.RenderControl(writer);
            } 
            else {
                if (HasRenderingData()) {
                    base.RenderContents(writer);
                } 
                else {
                    writer.Write(Text); 
                } 
            }
        } 

#if SHIPPINGADAPTERS
        internal void SetRenderHref(string s) {
            _renderHref = s; 
        }
#endif 
    } 
}
 
//------------------------------------------------------------------------------ 
// <copyright file="HyperLink.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Web;
    using System.Web.UI;
    using System.Web.Util;
    using System.Drawing.Design; 
    using System.Text;
    using System.Security.Permissions; 
 

    /// <devdoc> 
    /// <para>Interacts with the parser to build a <see cref='System.Web.UI.WebControls.HyperLink'/>.</para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class HyperLinkControlBuilder : ControlBuilder {
 
 
        /// <devdoc>
        ///    <para>Gets a value to indicate whether or not white spaces are allowed in literals for this control. This 
        ///       property is read-only.</para>
        /// </devdoc>
        public override bool AllowWhitespaceLiterals() {
            return false; 
        }
    } 
 

 
    /// <devdoc>
    ///    <para>Creates a link for the browser to navigate to another page.</para>
    /// </devdoc>
    [ 
    ControlBuilderAttribute(typeof(HyperLinkControlBuilder)),
    DataBindingHandler("System.Web.UI.Design.HyperLinkDataBindingHandler, " + AssemblyRef.SystemDesign), 
    DefaultProperty("Text"), 
    Designer("System.Web.UI.Design.WebControls.HyperLinkDesigner, " + AssemblyRef.SystemDesign),
    ToolboxData("<{0}:HyperLink runat=\"server\">HyperLink</{0}:HyperLink>"), 
    ParseChildren(false)
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class HyperLink : WebControl {
 
#if SHIPPINGADAPTERS 
        private string _renderHref = null;
        private bool _urlResolved = false; 
#endif


        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.HyperLink'/> class.</para>
        /// </devdoc> 
        public HyperLink() : base(HtmlTextWriterTag.A) { 
        }
 
#if SITECOUNTERS

        [
        WebCategory("SiteCounters"), 
        DefaultValue("HyperLink"),
        Themeable(false), 
        WebSysDescription(SR.Control_For_SiteCounters_CounterGroup), 
        ]
        public virtual String CounterGroup { 
            get {
                String s = (String)ViewState["CounterGroup"];
                return((s == null) ? "HyperLink" : s);
            } 
            set {
                ViewState["CounterGroup"] = value; 
            } 
        }
 

        /// <devdoc>
        ///    <para>Gets or sets the target name for qualifying click-through counters.</para>
        /// </devdoc> 
        [
        WebCategory("SiteCounters"), 
        DefaultValue(""), 
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_CounterName), 
        ]
        public String CounterName {
            get {
                String s = (String)ViewState["CounterName"]; 
                return((s == null) ? String.Empty : s);
            } 
            set { 
                ViewState["CounterName"] = value;
            } 
        }


        [ 
        WebCategory("SiteCounters"),
        DefaultValue(false), 
        Themeable(false), 
        WebSysDescription(SR.Control_For_SiteCounters_CountClicks),
        ] 
        public bool CountClicks {
            get {
                object b = ViewState["CountClicks"];
                return((b == null) ? false : (bool)b); 
            }
            set { 
                ViewState["CountClicks"] = value; 
            }
        } 
#endif


        /// <devdoc> 
        ///    <para>Gets or sets the URL reference to an image to display as an alternative to plain text for the
        ///       hyperlink.</para> 
        /// </devdoc> 
        [
        Bindable(true), 
        WebCategory("Appearance"),
        DefaultValue(""),
        Editor("System.Web.UI.Design.ImageUrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)),
        UrlProperty(), 
        WebSysDescription(SR.HyperLink_ImageUrl)
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
 

 
        /// <devdoc>
        ///    <para>Gets or sets the URL reference to navigate to when the hyperlink is clicked.</para>
        /// </devdoc>
        [ 
        Bindable(true),
        WebCategory("Navigation"), 
        DefaultValue(""), 
        Editor("System.Web.UI.Design.UrlEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor)),
        UrlProperty(), 
        WebSysDescription(SR.HyperLink_NavigateUrl)
        ]
        public string NavigateUrl {
            get { 
                string s = (string)ViewState["NavigateUrl"];
                return((s == null) ? String.Empty : s); 
            } 
            set {
                ViewState["NavigateUrl"] = value; 
            }
        }

 
        internal override bool RequiresLegacyRendering {
            get { 
                return true; 
            }
        } 


#if SITECOUNTERS
 
        [
        WebCategory("SiteCounters"), 
        DefaultValue(-1), 
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_RowsPerDay), 
        ]
        public int RowsPerDay {
            get {
                Object o = ViewState["RowsPerDay"]; 
                return((o == null) ? -1 : (int) o);
            } 
            set { 
                if (value == 0) {
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["RowsPerDay"] = value;
            }
        } 

 
        [ 
        WebCategory("SiteCounters"),
        DefaultValue(""), 
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_SiteCountersProvider)
        ]
        public String SiteCountersProvider { 
            get {
                String s = (String) ViewState["SiteCountersProvider"]; 
                return((s != null) ? s : String.Empty); 
            }
            set { 
                ViewState["SiteCountersProvider"] = value;
            }
        }
#endif 

 
        /// <devdoc> 
        ///    <para>Gets or sets the target window or frame the contents of
        ///       the <see cref='System.Web.UI.WebControls.HyperLink'/> will be displayed into when clicked.</para> 
        /// </devdoc>
        [
        WebCategory("Navigation"),
        DefaultValue(""), 
        WebSysDescription(SR.HyperLink_Target),
        TypeConverter(typeof(TargetConverter)) 
        ] 
        public string Target {
            get { 
                string s = (string)ViewState["Target"];
                return((s == null) ? String.Empty : s);
            }
            set { 
                ViewState["Target"] = value;
            } 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the text displayed for the <see cref='System.Web.UI.WebControls.HyperLink'/>.</para>
        /// </devdoc> 
        [
        Localizable(true), 
        Bindable(true), 
        WebCategory("Appearance"),
        DefaultValue(""), 
        WebSysDescription(SR.HyperLink_Text),
        PersistenceMode(PersistenceMode.InnerDefaultProperty)
        ]
        public virtual string Text { 
            get {
                object o = ViewState["Text"]; 
                return((o == null) ? String.Empty : (string)o); 
            }
            set { 
                if (HasControls()) {
                    Controls.Clear();
                }
                ViewState["Text"] = value; 
            }
        } 
#if SITECOUNTERS 

        [ 
        WebCategory("SiteCounters"),
        DefaultValue(true),
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_TrackApplicationName), 
        ]
        public bool TrackApplicationName { 
            get { 
                object b = ViewState["TrackApplicationName"];
                return((b == null) ? true : (bool)b); 
            }
            set {
                ViewState["TrackApplicationName"] = value;
            } 
        }
 
 
        [
        WebCategory("SiteCounters"), 
        DefaultValue(true),
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_TrackNavigateUrl),
        ] 
        public bool TrackNavigateUrl {
            get { 
                object b = ViewState["TrackNavigateUrl"]; 
                return((b == null) ? true : (bool)b);
            } 
            set {
                ViewState["TrackNavigateUrl"] = value;
            }
        } 

 
        [ 
        WebCategory("SiteCounters"),
        DefaultValue(true), 
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_TrackPageUrl),
        ]
        public bool TrackPageUrl { 
            get {
                object b = ViewState["TrackPageUrl"]; 
                return((b == null) ? true : (bool)b); 
            }
            set { 
                ViewState["TrackPageUrl"] = value;
            }
        }
#endif 
#if SHIPPINGADAPTERS
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
#endif 

 
        /// <internalonly/>
        /// <devdoc>
        /// <para>Adds the attribututes of the a <see cref='System.Web.UI.WebControls.HyperLink'/> to the output
        ///    stream for rendering.</para> 
        /// </devdoc>
        protected override void AddAttributesToRender(HtmlTextWriter writer) { 
            if (Enabled && !IsEnabled) { 
                // We need to do the cascade effect on the server, because the browser
                // only renders as disabled, but doesn't disable the functionality. 
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
            }

            base.AddAttributesToRender(writer); 

#if SHIPPINGADAPTERS 
            string s = (!String.IsNullOrEmpty(_renderHref)) ? _renderHref : NavigateUrl; 
#else
            string s = NavigateUrl; 
#endif
            if ((s.Length > 0) && IsEnabled) {
#if SHIPPINGADAPTERS
                string resolvedUrl = (UrlResolved) ? s : ResolveClientUrl(s); 
#else
                string resolvedUrl = ResolveClientUrl(s); 
#endif 
#if SITECOUNTERS
                resolvedUrl = GetCountClickUrl(resolvedUrl); 
#endif
                writer.AddAttribute(HtmlTextWriterAttribute.Href, resolvedUrl);
            }
            s = Target; 
            if (s.Length > 0) {
                writer.AddAttribute(HtmlTextWriterAttribute.Target, s); 
            } 
        }
 
        protected override void AddParsedSubObject(object obj) {
            if (HasControls()) {
                base.AddParsedSubObject(obj);
            } 
            else {
                if (obj is LiteralControl) { 
                    Text = ((LiteralControl)obj).Text; 
                }
                else { 
                    string currentText = Text;
                    if (currentText.Length != 0) {
                        Text = String.Empty;
                        base.AddParsedSubObject(new LiteralControl(currentText)); 
                    }
                    base.AddParsedSubObject(obj); 
                } 
            }
        } 

#if SITECOUNTERS
        internal string GetCountClickUrl(string url) {
            if (CountClicks && url != null && url.Length > 0) { 
                HttpContext context = Context;
                SiteCounters siteCounters = (context == null) ? null : context.SiteCounters; 
                if (siteCounters != null && siteCounters.Enabled) { 
                    String counterName = CounterName;
                    if (counterName != null && counterName.Length == 0) { 
                        if (UrlPath.IsRelativeUrl(url)) {
                            // Make the path absolute for data reporting
                            counterName = UrlPath.Combine(context.Request.FilePathObject, url);
                        } 
                        else {
                            counterName = url; 
                        } 
                    }
                    url = siteCounters.GetRedirectUrl( 
                                        CounterGroup, counterName, SiteCounters.ClickEventText,
                                        url, TrackApplicationName,
                                        TrackPageUrl, TrackNavigateUrl,
                                        SiteCountersProvider, RowsPerDay); 
                }
            } 
            return url; 
        }
#endif 

        /// <internalonly/>
        /// <devdoc>
        ///    Load previously saved state. 
        ///    Overridden to synchronize Text property with LiteralContent.
        /// </devdoc> 
        protected override void LoadViewState(object savedState) { 
            if (savedState != null) {
                base.LoadViewState(savedState); 
                string s = (string)ViewState["Text"];
                if (s != null)
                    Text = s;
            } 
        }
 
#if SITECOUNTERS 

        protected internal override void OnPreRender(EventArgs e) { 
            base.OnPreRender(e);

            // VSWhidbey 83401: Check if the url is supported for redirect when
            // CountClicks is true. 
            if (CountClicks && !UrlPath.IsPathRedirectSupported(NavigateUrl)) {
                throw new HttpException( 
                    SR.GetString( 
                        SR.SiteCounters_url_not_supported_for_redirect, NavigateUrl));
            } 
        }
#endif

        /// <internalonly/> 
        /// <devdoc>
        /// <para>Displays the <see cref='System.Web.UI.WebControls.HyperLink'/> on a page.</para> 
        /// </devdoc> 
        protected internal override void RenderContents(HtmlTextWriter writer) {
            string s = ImageUrl; 
            if (s.Length > 0) {
                Image img = new Image();

                // NOTE: The Url resolution happens right here, because the image is not parented 
                //       and will not be able to resolve when it tries to do so.
                img.ImageUrl = ResolveClientUrl(s); 
 
                s = ToolTip;
                if (s.Length != 0) { 
                    img.ToolTip = s;
                }

                s = Text; 
                if (s.Length != 0) {
                    img.AlternateText = s; 
                } 
                img.RenderControl(writer);
            } 
            else {
                if (HasRenderingData()) {
                    base.RenderContents(writer);
                } 
                else {
                    writer.Write(Text); 
                } 
            }
        } 

#if SHIPPINGADAPTERS
        internal void SetRenderHref(string s) {
            _renderHref = s; 
        }
#endif 
    } 
}
 
