//------------------------------------------------------------------------------ 
// <copyright file="ImageMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.WebControls {
 
    using System; 
    using System.ComponentModel;
    using System.Globalization; 
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.Util;
    using System.Web; 
    using System.Security.Permissions;
 
 
    /// <devdoc>
    /// <para>ImageMap class.  Provides support for multiple 
    /// region-defined actions within an image.</para>
    /// </devdoc>
    [
    DefaultEvent("Click"), 
    DefaultProperty("HotSpots"),
    ParseChildren(true, "HotSpots"), 
    SupportsEventValidation, 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class ImageMap : Image, IPostBackEventHandler {

        private static readonly object EventClick = new object(); 
        private bool _hasHotSpots;
        private HotSpotCollection _hotSpots; 
#if SITECOUNTERS 

        [ 
        WebCategory("SiteCounters"),
        DefaultValue("ImageMap"),
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_CounterGroup), 
        ]
        public String CounterGroup { 
            get { 
                String s = (String)ViewState["CounterGroup"];
                return((s == null) ? "ImageMap" : s); 
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

        [ 
        Browsable(true),
        EditorBrowsableAttribute(EditorBrowsableState.Always)
        ]
        public override bool Enabled { 
            get {
                return base.Enabled; 
            } 
            set {
                base.Enabled = value; 
            }
        }

        /// <devdoc> 
        /// <para>Gets the HotSpotCollection with defines the regions of ImageMap hot spots.</para>
        /// </devdoc> 
        [ 
        WebCategory("Behavior"),
        WebSysDescription(SR.ImageMap_HotSpots), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        NotifyParentProperty(true),
        PersistenceMode(PersistenceMode.InnerDefaultProperty)
        ] 
        public HotSpotCollection HotSpots {
            get { 
                if (_hotSpots == null) { 
                    _hotSpots = new HotSpotCollection();
                    if (IsTrackingViewState) { 
                        ((IStateManager)_hotSpots).TrackViewState();
                    }
                }
                return _hotSpots; 
            }
        } 
 

        /// <devdoc> 
        /// <para>Gets or sets the HotSpotMode to either postback or navigation.</para>
        /// </devdoc>
        [
        WebCategory("Behavior"), 
        DefaultValue(HotSpotMode.NotSet),
        WebSysDescription(SR.HotSpot_HotSpotMode), 
        ] 
        public virtual HotSpotMode HotSpotMode {
            get { 
                object obj = ViewState["HotSpotMode"];
                return (obj == null) ? HotSpotMode.NotSet : (HotSpotMode)obj;
            }
            set { 
                if (value < HotSpotMode.NotSet || value > HotSpotMode.Inactive) {
                    throw new ArgumentOutOfRangeException("value"); 
                } 
                ViewState["HotSpotMode"] = value;
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
        /// <para>Gets or sets the name of the window for navigation.</para> 
        /// </devdoc> 
        [
        WebCategory("Behavior"), 
        DefaultValue(""),
        WebSysDescription(SR.HotSpot_Target),
        ]
        public virtual string Target { 
            get {
                object value = ViewState["Target"]; 
                return (value == null)? String.Empty : (string)value; 
            }
            set { 
                ViewState["Target"] = value;
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

        /// <devdoc>
        /// <para>The event raised when a hotspot is clicked.</para> 
        /// </devdoc>
        [ 
        Category("Action"), 
        WebSysDescription(SR.ImageMap_Click)
        ] 
        public event ImageMapEventHandler Click {
            add {
                Events.AddHandler(EventClick, value);
            } 
            remove {
                Events.RemoveHandler(EventClick, value); 
            } 
        }
 

        /// <internalonly/>
        /// <devdoc>
        /// <para>Overridden to add the "usemap" attribute the the image tag. 
        /// Overrides WebControl.AddAttributesToRender.</para>
        /// </devdoc> 
        protected override void AddAttributesToRender(HtmlTextWriter writer) { 
            base.AddAttributesToRender(writer);
 
            if (_hasHotSpots) {
                writer.AddAttribute(HtmlTextWriterAttribute.Usemap, "#ImageMap" + ClientID, false);
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
                                        url, TrackApplicationName, TrackPageUrl, TrackNavigateUrl,
                                        SiteCountersProvider, RowsPerDay); 
                } 
            }
            return url; 
        }
#endif

        /// <devdoc> 
        /// <para>Restores view-state information that was saved by SaveViewState.
        /// Implements IStateManager.LoadViewState.</para> 
        /// </devdoc> 
        protected override void LoadViewState(object savedState) {
            object baseState = null; 
            object[] myState = null;

            if (savedState != null) {
                myState = (object[])savedState; 
                if (myState.Length != 2) {
                    throw new ArgumentException(SR.GetString(SR.ViewState_InvalidViewState)); 
                } 

                baseState = myState[0]; 
            }

            base.LoadViewState(baseState);
 
            if ((myState != null) && (myState[1] != null)) {
                ((IStateManager)HotSpots).LoadViewState(myState[1]); 
            } 
        }
 

        /// <devdoc>
        /// <para>Called when the user clicks the ImageMap.</para>
        /// </devdoc> 
        protected virtual void OnClick(ImageMapEventArgs e) {
            ImageMapEventHandler clickHandler = (ImageMapEventHandler)Events[EventClick]; 
            if (clickHandler != null) { 
                clickHandler(this, e);
            } 
        }
#if SITECOUNTERS

        protected internal override void OnPreRender(EventArgs e) { 
            base.OnPreRender(e);
 
            // VSWhidbey 83401: Check if the url is supported for redirect when 
            // CountClicks is true.
            if (CountClicks && _hotSpots != null && _hotSpots.Count > 0) { 
                foreach (HotSpot item in _hotSpots) {
                    if (!UrlPath.IsPathRedirectSupported(item.NavigateUrl)) {
                        throw new HttpException(
                            SR.GetString( 
                                SR.SiteCounters_url_not_supported_for_redirect, item.NavigateUrl));
                    } 
                } 
            }
        } 
#endif

        /// <internalonly/>
        /// <devdoc> 
        /// <para>Sends server control content to a provided HtmlTextWriter, which writes the content
        /// to be rendered to the client. 
        /// Overrides Control.Render.</para> 
        /// </devdoc>
        protected internal override void Render(HtmlTextWriter writer) { 
            if (Enabled && !IsEnabled) {
                // We need to do the cascade effect on the server, because the browser
                // only renders as disabled, but doesn't disable the functionality.
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled"); 
            }
 
            _hasHotSpots = ((_hotSpots != null) && (_hotSpots.Count > 0)); 

            base.Render(writer); 

            if (_hasHotSpots) {
                string fullClientID = "ImageMap" + ClientID;
                writer.AddAttribute(HtmlTextWriterAttribute.Name, fullClientID); 
                writer.AddAttribute(HtmlTextWriterAttribute.Id, fullClientID);
                writer.RenderBeginTag(HtmlTextWriterTag.Map); 
 
                HotSpotMode mapMode = HotSpotMode;
                if (mapMode == HotSpotMode.NotSet) { 
                    mapMode = HotSpotMode.Navigate;
                }
                HotSpotMode spotMode;
                int hotSpotIndex = 0; 
                string controlTarget = Target;
                foreach (HotSpot item in _hotSpots) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Shape, item.MarkupName, false); 
                    writer.AddAttribute(HtmlTextWriterAttribute.Coords, item.GetCoordinates());
                    spotMode = item.HotSpotMode; 
                    if (spotMode == HotSpotMode.NotSet) {
                        spotMode = mapMode;
                    }
                    if (spotMode == HotSpotMode.PostBack) { 
                        // Make sure the page has a server side form if we are posting back
                        if (Page != null) { 
                            Page.VerifyRenderingInServerForm(this); 
                        }
 
                        string eventArgument = hotSpotIndex.ToString(CultureInfo.InvariantCulture);
                        writer.AddAttribute(HtmlTextWriterAttribute.Href,
                            Page.ClientScript.GetPostBackClientHyperlink(this, eventArgument, true));
                    } 
                    else if (spotMode == HotSpotMode.Navigate) {
                        String resolvedUrl = ResolveClientUrl(item.NavigateUrl); 
#if SITECOUNTERS 
                        resolvedUrl = GetCountClickUrl(resolvedUrl);
#endif 
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, resolvedUrl);
                        // Use HotSpot target first, if not specified, use ImageMap's target
                        string target = item.Target;
                        if (target.Length == 0) target = controlTarget; 
                        if (target.Length > 0) writer.AddAttribute(HtmlTextWriterAttribute.Target, target);
                    } 
                    else if (spotMode == HotSpotMode.Inactive) { 
                        writer.AddAttribute("nohref", "true");
                    } 
                    writer.AddAttribute(HtmlTextWriterAttribute.Title, item.AlternateText);
                    writer.AddAttribute(HtmlTextWriterAttribute.Alt, item.AlternateText);
                    string s = item.AccessKey;
                    if (s.Length > 0) { 
                        writer.AddAttribute(HtmlTextWriterAttribute.Accesskey, s);
                    } 
                    int n = item.TabIndex; 
                    if (n != 0) {
                        writer.AddAttribute(HtmlTextWriterAttribute.Tabindex, n.ToString(NumberFormatInfo.InvariantInfo)); 
                    }

                    writer.RenderBeginTag(HtmlTextWriterTag.Area);
                    writer.RenderEndTag(); 
                    ++hotSpotIndex;
                } 
                writer.RenderEndTag();  // Map 
            }
        } 


        /// <devdoc>
        /// <para>Saves any server control view-state changes that have 
        /// occurred since the time the page was posted back to the server.
        /// Implements IStateManager.SaveViewState.</para> 
        /// </devdoc> 
        protected override object SaveViewState() {
            object baseState = base.SaveViewState(); 
            object hotSpotsState = null;

            if ((_hotSpots != null) && (_hotSpots.Count > 0)) {
                hotSpotsState = ((IStateManager)_hotSpots).SaveViewState(); 
            }
 
            if ((baseState != null) || (hotSpotsState != null)) { 
                object[] savedState = new object[2];
                savedState[0] = baseState; 
                savedState[1] = hotSpotsState;

                return savedState;
            } 

            return null; 
        } 

 
        /// <devdoc>
        /// <para>Causes the tracking of view-state changes to the server control.
        /// Implements IStateManager.TrackViewState.</para>
        /// </devdoc> 
        protected override void TrackViewState() {
            base.TrackViewState(); 
            if (_hotSpots != null) { 
                ((IStateManager)_hotSpots).TrackViewState();
            } 
        }

        #region Implementation of IPostBackEventHandler
 
        /// <internalonly/>
        /// <devdoc> 
        /// <para>Notifies the server control that caused the postback that 
        /// it should handle an incoming post back event.
        /// Implements IPostBackEventHandler.</para> 
        /// </devdoc>
        void IPostBackEventHandler.RaisePostBackEvent(string eventArgument) {
            RaisePostBackEvent(eventArgument);
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        /// <para>Notifies the server control that caused the postback that 
        /// it should handle an incoming post back event.
        /// Implements IPostBackEventHandler.</para>
        /// </devdoc>
        protected virtual void RaisePostBackEvent(string eventArgument) { 
            ValidateEvent(UniqueID, eventArgument);
 
            string postBackValue = null; 
            if (eventArgument != null && _hotSpots != null) {
                int hotSpotIndex = Int32.Parse(eventArgument, CultureInfo.InvariantCulture); 

                if (hotSpotIndex >= 0 && hotSpotIndex < _hotSpots.Count) {
                    HotSpot hotSpot = _hotSpots[hotSpotIndex];
                    HotSpotMode mode = hotSpot.HotSpotMode; 
                    if (mode == HotSpotMode.NotSet) {
                        mode = HotSpotMode; 
                    } 
                    if (mode == HotSpotMode.PostBack) {
                        postBackValue = hotSpot.PostBackValue; 
                    }
                }
            }
#if SITECOUNTERS 
            SiteCounters siteCounters = Context.SiteCounters;
            if (siteCounters.Enabled && CountClicks) { 
 
                // VSWhidbey 276548: Use HotSpot PostBackValue if available
                String counterName = postBackValue; 
                if (counterName == null) {
                    counterName = CounterName;
                    if (counterName.Length == 0) {
                        counterName = ID; 
                    }
                } 
                siteCounters.Write(CounterGroup, counterName, SiteCounters.ClickEventText, 
                                   null, TrackApplicationName, TrackPageUrl,
                                   SiteCountersProvider, RowsPerDay); 
            }
#endif
            // Ignore invalid indexes silently(VSWhidbey 185738)
            if (postBackValue != null) { 
                OnClick(new ImageMapEventArgs(postBackValue));
            } 
        } 
        #endregion
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="ImageMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.WebControls {
 
    using System; 
    using System.ComponentModel;
    using System.Globalization; 
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.Util;
    using System.Web; 
    using System.Security.Permissions;
 
 
    /// <devdoc>
    /// <para>ImageMap class.  Provides support for multiple 
    /// region-defined actions within an image.</para>
    /// </devdoc>
    [
    DefaultEvent("Click"), 
    DefaultProperty("HotSpots"),
    ParseChildren(true, "HotSpots"), 
    SupportsEventValidation, 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class ImageMap : Image, IPostBackEventHandler {

        private static readonly object EventClick = new object(); 
        private bool _hasHotSpots;
        private HotSpotCollection _hotSpots; 
#if SITECOUNTERS 

        [ 
        WebCategory("SiteCounters"),
        DefaultValue("ImageMap"),
        Themeable(false),
        WebSysDescription(SR.Control_For_SiteCounters_CounterGroup), 
        ]
        public String CounterGroup { 
            get { 
                String s = (String)ViewState["CounterGroup"];
                return((s == null) ? "ImageMap" : s); 
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

        [ 
        Browsable(true),
        EditorBrowsableAttribute(EditorBrowsableState.Always)
        ]
        public override bool Enabled { 
            get {
                return base.Enabled; 
            } 
            set {
                base.Enabled = value; 
            }
        }

        /// <devdoc> 
        /// <para>Gets the HotSpotCollection with defines the regions of ImageMap hot spots.</para>
        /// </devdoc> 
        [ 
        WebCategory("Behavior"),
        WebSysDescription(SR.ImageMap_HotSpots), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        NotifyParentProperty(true),
        PersistenceMode(PersistenceMode.InnerDefaultProperty)
        ] 
        public HotSpotCollection HotSpots {
            get { 
                if (_hotSpots == null) { 
                    _hotSpots = new HotSpotCollection();
                    if (IsTrackingViewState) { 
                        ((IStateManager)_hotSpots).TrackViewState();
                    }
                }
                return _hotSpots; 
            }
        } 
 

        /// <devdoc> 
        /// <para>Gets or sets the HotSpotMode to either postback or navigation.</para>
        /// </devdoc>
        [
        WebCategory("Behavior"), 
        DefaultValue(HotSpotMode.NotSet),
        WebSysDescription(SR.HotSpot_HotSpotMode), 
        ] 
        public virtual HotSpotMode HotSpotMode {
            get { 
                object obj = ViewState["HotSpotMode"];
                return (obj == null) ? HotSpotMode.NotSet : (HotSpotMode)obj;
            }
            set { 
                if (value < HotSpotMode.NotSet || value > HotSpotMode.Inactive) {
                    throw new ArgumentOutOfRangeException("value"); 
                } 
                ViewState["HotSpotMode"] = value;
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
        /// <para>Gets or sets the name of the window for navigation.</para> 
        /// </devdoc> 
        [
        WebCategory("Behavior"), 
        DefaultValue(""),
        WebSysDescription(SR.HotSpot_Target),
        ]
        public virtual string Target { 
            get {
                object value = ViewState["Target"]; 
                return (value == null)? String.Empty : (string)value; 
            }
            set { 
                ViewState["Target"] = value;
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

        /// <devdoc>
        /// <para>The event raised when a hotspot is clicked.</para> 
        /// </devdoc>
        [ 
        Category("Action"), 
        WebSysDescription(SR.ImageMap_Click)
        ] 
        public event ImageMapEventHandler Click {
            add {
                Events.AddHandler(EventClick, value);
            } 
            remove {
                Events.RemoveHandler(EventClick, value); 
            } 
        }
 

        /// <internalonly/>
        /// <devdoc>
        /// <para>Overridden to add the "usemap" attribute the the image tag. 
        /// Overrides WebControl.AddAttributesToRender.</para>
        /// </devdoc> 
        protected override void AddAttributesToRender(HtmlTextWriter writer) { 
            base.AddAttributesToRender(writer);
 
            if (_hasHotSpots) {
                writer.AddAttribute(HtmlTextWriterAttribute.Usemap, "#ImageMap" + ClientID, false);
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
                                        url, TrackApplicationName, TrackPageUrl, TrackNavigateUrl,
                                        SiteCountersProvider, RowsPerDay); 
                } 
            }
            return url; 
        }
#endif

        /// <devdoc> 
        /// <para>Restores view-state information that was saved by SaveViewState.
        /// Implements IStateManager.LoadViewState.</para> 
        /// </devdoc> 
        protected override void LoadViewState(object savedState) {
            object baseState = null; 
            object[] myState = null;

            if (savedState != null) {
                myState = (object[])savedState; 
                if (myState.Length != 2) {
                    throw new ArgumentException(SR.GetString(SR.ViewState_InvalidViewState)); 
                } 

                baseState = myState[0]; 
            }

            base.LoadViewState(baseState);
 
            if ((myState != null) && (myState[1] != null)) {
                ((IStateManager)HotSpots).LoadViewState(myState[1]); 
            } 
        }
 

        /// <devdoc>
        /// <para>Called when the user clicks the ImageMap.</para>
        /// </devdoc> 
        protected virtual void OnClick(ImageMapEventArgs e) {
            ImageMapEventHandler clickHandler = (ImageMapEventHandler)Events[EventClick]; 
            if (clickHandler != null) { 
                clickHandler(this, e);
            } 
        }
#if SITECOUNTERS

        protected internal override void OnPreRender(EventArgs e) { 
            base.OnPreRender(e);
 
            // VSWhidbey 83401: Check if the url is supported for redirect when 
            // CountClicks is true.
            if (CountClicks && _hotSpots != null && _hotSpots.Count > 0) { 
                foreach (HotSpot item in _hotSpots) {
                    if (!UrlPath.IsPathRedirectSupported(item.NavigateUrl)) {
                        throw new HttpException(
                            SR.GetString( 
                                SR.SiteCounters_url_not_supported_for_redirect, item.NavigateUrl));
                    } 
                } 
            }
        } 
#endif

        /// <internalonly/>
        /// <devdoc> 
        /// <para>Sends server control content to a provided HtmlTextWriter, which writes the content
        /// to be rendered to the client. 
        /// Overrides Control.Render.</para> 
        /// </devdoc>
        protected internal override void Render(HtmlTextWriter writer) { 
            if (Enabled && !IsEnabled) {
                // We need to do the cascade effect on the server, because the browser
                // only renders as disabled, but doesn't disable the functionality.
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled"); 
            }
 
            _hasHotSpots = ((_hotSpots != null) && (_hotSpots.Count > 0)); 

            base.Render(writer); 

            if (_hasHotSpots) {
                string fullClientID = "ImageMap" + ClientID;
                writer.AddAttribute(HtmlTextWriterAttribute.Name, fullClientID); 
                writer.AddAttribute(HtmlTextWriterAttribute.Id, fullClientID);
                writer.RenderBeginTag(HtmlTextWriterTag.Map); 
 
                HotSpotMode mapMode = HotSpotMode;
                if (mapMode == HotSpotMode.NotSet) { 
                    mapMode = HotSpotMode.Navigate;
                }
                HotSpotMode spotMode;
                int hotSpotIndex = 0; 
                string controlTarget = Target;
                foreach (HotSpot item in _hotSpots) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Shape, item.MarkupName, false); 
                    writer.AddAttribute(HtmlTextWriterAttribute.Coords, item.GetCoordinates());
                    spotMode = item.HotSpotMode; 
                    if (spotMode == HotSpotMode.NotSet) {
                        spotMode = mapMode;
                    }
                    if (spotMode == HotSpotMode.PostBack) { 
                        // Make sure the page has a server side form if we are posting back
                        if (Page != null) { 
                            Page.VerifyRenderingInServerForm(this); 
                        }
 
                        string eventArgument = hotSpotIndex.ToString(CultureInfo.InvariantCulture);
                        writer.AddAttribute(HtmlTextWriterAttribute.Href,
                            Page.ClientScript.GetPostBackClientHyperlink(this, eventArgument, true));
                    } 
                    else if (spotMode == HotSpotMode.Navigate) {
                        String resolvedUrl = ResolveClientUrl(item.NavigateUrl); 
#if SITECOUNTERS 
                        resolvedUrl = GetCountClickUrl(resolvedUrl);
#endif 
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, resolvedUrl);
                        // Use HotSpot target first, if not specified, use ImageMap's target
                        string target = item.Target;
                        if (target.Length == 0) target = controlTarget; 
                        if (target.Length > 0) writer.AddAttribute(HtmlTextWriterAttribute.Target, target);
                    } 
                    else if (spotMode == HotSpotMode.Inactive) { 
                        writer.AddAttribute("nohref", "true");
                    } 
                    writer.AddAttribute(HtmlTextWriterAttribute.Title, item.AlternateText);
                    writer.AddAttribute(HtmlTextWriterAttribute.Alt, item.AlternateText);
                    string s = item.AccessKey;
                    if (s.Length > 0) { 
                        writer.AddAttribute(HtmlTextWriterAttribute.Accesskey, s);
                    } 
                    int n = item.TabIndex; 
                    if (n != 0) {
                        writer.AddAttribute(HtmlTextWriterAttribute.Tabindex, n.ToString(NumberFormatInfo.InvariantInfo)); 
                    }

                    writer.RenderBeginTag(HtmlTextWriterTag.Area);
                    writer.RenderEndTag(); 
                    ++hotSpotIndex;
                } 
                writer.RenderEndTag();  // Map 
            }
        } 


        /// <devdoc>
        /// <para>Saves any server control view-state changes that have 
        /// occurred since the time the page was posted back to the server.
        /// Implements IStateManager.SaveViewState.</para> 
        /// </devdoc> 
        protected override object SaveViewState() {
            object baseState = base.SaveViewState(); 
            object hotSpotsState = null;

            if ((_hotSpots != null) && (_hotSpots.Count > 0)) {
                hotSpotsState = ((IStateManager)_hotSpots).SaveViewState(); 
            }
 
            if ((baseState != null) || (hotSpotsState != null)) { 
                object[] savedState = new object[2];
                savedState[0] = baseState; 
                savedState[1] = hotSpotsState;

                return savedState;
            } 

            return null; 
        } 

 
        /// <devdoc>
        /// <para>Causes the tracking of view-state changes to the server control.
        /// Implements IStateManager.TrackViewState.</para>
        /// </devdoc> 
        protected override void TrackViewState() {
            base.TrackViewState(); 
            if (_hotSpots != null) { 
                ((IStateManager)_hotSpots).TrackViewState();
            } 
        }

        #region Implementation of IPostBackEventHandler
 
        /// <internalonly/>
        /// <devdoc> 
        /// <para>Notifies the server control that caused the postback that 
        /// it should handle an incoming post back event.
        /// Implements IPostBackEventHandler.</para> 
        /// </devdoc>
        void IPostBackEventHandler.RaisePostBackEvent(string eventArgument) {
            RaisePostBackEvent(eventArgument);
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        /// <para>Notifies the server control that caused the postback that 
        /// it should handle an incoming post back event.
        /// Implements IPostBackEventHandler.</para>
        /// </devdoc>
        protected virtual void RaisePostBackEvent(string eventArgument) { 
            ValidateEvent(UniqueID, eventArgument);
 
            string postBackValue = null; 
            if (eventArgument != null && _hotSpots != null) {
                int hotSpotIndex = Int32.Parse(eventArgument, CultureInfo.InvariantCulture); 

                if (hotSpotIndex >= 0 && hotSpotIndex < _hotSpots.Count) {
                    HotSpot hotSpot = _hotSpots[hotSpotIndex];
                    HotSpotMode mode = hotSpot.HotSpotMode; 
                    if (mode == HotSpotMode.NotSet) {
                        mode = HotSpotMode; 
                    } 
                    if (mode == HotSpotMode.PostBack) {
                        postBackValue = hotSpot.PostBackValue; 
                    }
                }
            }
#if SITECOUNTERS 
            SiteCounters siteCounters = Context.SiteCounters;
            if (siteCounters.Enabled && CountClicks) { 
 
                // VSWhidbey 276548: Use HotSpot PostBackValue if available
                String counterName = postBackValue; 
                if (counterName == null) {
                    counterName = CounterName;
                    if (counterName.Length == 0) {
                        counterName = ID; 
                    }
                } 
                siteCounters.Write(CounterGroup, counterName, SiteCounters.ClickEventText, 
                                   null, TrackApplicationName, TrackPageUrl,
                                   SiteCountersProvider, RowsPerDay); 
            }
#endif
            // Ignore invalid indexes silently(VSWhidbey 185738)
            if (postBackValue != null) { 
                OnClick(new ImageMapEventArgs(postBackValue));
            } 
        } 
        #endregion
    } 
}
