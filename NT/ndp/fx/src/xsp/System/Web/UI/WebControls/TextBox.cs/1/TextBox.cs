//------------------------------------------------------------------------------ 
// <copyright file="TextBox.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
    using System.Text; 
    using System.ComponentModel;
    using System.Drawing.Design; 
    using System;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls.Adapters; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Globalization; 
    using System.Security.Permissions;
 

    /// <devdoc>
    /// <para>Interacts with the parser to build a <see cref='System.Web.UI.WebControls.TextBox'/> control.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class TextBoxControlBuilder : ControlBuilder { 

 
        /// <internalonly/>
        /// <devdoc>
        ///    Specifies whether white space literals are allowed.
        /// </devdoc> 
        public override bool AllowWhitespaceLiterals() {
            return false; 
        } 

 
        public override bool HtmlDecodeLiterals() {
            // TextBox text gets rendered as an encoded attribute value or encoded content.

            // At parse time text specified as an attribute gets decoded, and so text specified as a 
            // literal needs to go through the same process.
 
            return true; 
        }
    } 



    /// <devdoc> 
    ///    <para>Constructs a text box and defines its properties.</para>
    /// </devdoc> 
    [ 
    ControlBuilderAttribute(typeof(TextBoxControlBuilder)),
    ControlValueProperty("Text"), 
    DataBindingHandler("System.Web.UI.Design.TextDataBindingHandler, " + AssemblyRef.SystemDesign),
    DefaultProperty("Text"),
    ValidationProperty("Text"),
    DefaultEvent("TextChanged"), 
    Designer("System.Web.UI.Design.WebControls.PreviewControlDesigner, " + AssemblyRef.SystemDesign),
    ParseChildren(true, "Text"), 
    SupportsEventValidation, 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class TextBox : WebControl, IPostBackDataHandler, IEditableTextControl {

        private static readonly object EventTextChanged = new Object(); 

        private const string _textBoxKeyHandlerCall = "if (WebForm_TextBoxKeyHandler(event) == false) return false;"; 
 
        private const int DefaultMutliLineRows = 2;
        private const int DefaultMutliLineColumns = 20; 

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.TextBox'/> class.</para>
        /// </devdoc> 
        public TextBox() : base(HtmlTextWriterTag.Input) {
        } 
 

        [ 
        DefaultValue(AutoCompleteType.None),
        Themeable(false),
        WebCategory("Behavior"),
        WebSysDescription(SR.TextBox_AutoCompleteType) 
        ]
        public virtual AutoCompleteType AutoCompleteType { 
            get { 
                object obj = ViewState["AutoCompleteType"];
                return (obj == null) ? AutoCompleteType.None : (AutoCompleteType) obj; 
            }
            set {
                if (value < AutoCompleteType.None || value > AutoCompleteType.Search) {
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["AutoCompleteType"] = value; 
            } 
        }
 

        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether an automatic
        ///       postback to the server will occur whenever the user changes the 
        ///       content of the text box.</para>
        /// </devdoc> 
        [ 
        DefaultValue(false),
        Themeable(false), 
        WebCategory("Behavior"),
        WebSysDescription(SR.TextBox_AutoPostBack),
        ]
        public virtual bool AutoPostBack { 
            get {
                object b = ViewState["AutoPostBack"]; 
                return((b == null) ? false : (bool)b); 
            }
            set { 
                ViewState["AutoPostBack"] = value;
            }
        }
 

        [ 
        DefaultValue(false), 
        Themeable(false),
        WebCategory("Behavior"), 
        WebSysDescription(SR.AutoPostBackControl_CausesValidation)
        ]
        public virtual bool CausesValidation {
            get { 
                object b = ViewState["CausesValidation"];
                return((b == null) ? false : (bool)b); 
            } 
            set {
                ViewState["CausesValidation"] = value; 
            }
        }

 
        /// <devdoc>
        ///    <para>Gets or sets the display 
        ///       width of the text box in characters.</para> 
        /// </devdoc>
        [ 
        WebCategory("Appearance"),
        DefaultValue(0),
        WebSysDescription(SR.TextBox_Columns)
        ] 
        public virtual int Columns {
            get { 
                object o = ViewState["Columns"]; 
                return((o == null) ? 0 : (int)o);
            } 
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("Columns", SR.GetString(SR.TextBox_InvalidColumns));
                } 
                ViewState["Columns"] = value;
            } 
        } 

 
        /// <devdoc>
        ///    <para>Gets or sets the maximum number of characters allowed in the text box.</para>
        /// </devdoc>
        [ 
        DefaultValue(0),
        Themeable(false), 
        WebCategory("Behavior"), 
        WebSysDescription(SR.TextBox_MaxLength),
        ] 
        public virtual int MaxLength {
            get {
                object o = ViewState["MaxLength"];
                return((o == null) ? 0 : (int)o); 
            }
            set { 
                if (value < 0) { 
                    throw new ArgumentOutOfRangeException("value");
                } 
                ViewState["MaxLength"] = value;
            }
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets the behavior mode of the text box.</para>
        /// </devdoc> 
        [
        DefaultValue(TextBoxMode.SingleLine),
        Themeable(false),
        WebCategory("Behavior"), 
        WebSysDescription(SR.TextBox_TextMode)
        ] 
        public virtual TextBoxMode TextMode { 
            get {
                object mode = ViewState["Mode"]; 
                return((mode == null) ? TextBoxMode.SingleLine : (TextBoxMode)mode);
            }
            set {
                if (value < TextBoxMode.SingleLine || value > TextBoxMode.Password) { 
                    throw new ArgumentOutOfRangeException("value");
                } 
                ViewState["Mode"] = value; 
            }
        } 


        /// <devdoc>
        ///    <para>Whether the textbox is in read-only mode.</para> 
        /// </devdoc>
        [ 
        Bindable(true), 
        DefaultValue(false),
        Themeable(false), 
        WebCategory("Behavior"),
        WebSysDescription(SR.TextBox_ReadOnly)
        ]
        public virtual bool ReadOnly { 
            get {
                object o = ViewState["ReadOnly"]; 
                return((o == null) ? false : (bool)o); 
            }
            set { 
                ViewState["ReadOnly"] = value;
            }
        }
 

        /// <devdoc> 
        ///    <para> Gets or sets the display height of a multiline text box.</para> 
        /// </devdoc>
        [ 
        DefaultValue(0),
        Themeable(false),
        WebCategory("Behavior"),
        WebSysDescription(SR.TextBox_Rows) 
        ]
        public virtual int Rows { 
            get { 
                object o = ViewState["Rows"];
                return((o == null) ? 0 : (int)o); 
            }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("Rows", SR.GetString(SR.TextBox_InvalidRows)); 
                }
                ViewState["Rows"] = value; 
            } 
        }
 
        /// <devdoc>
        ///    Determines whether the Text must be stored in view state, to
        ///    optimize the size of the saved state.
        /// </devdoc> 
        private bool SaveTextViewState {
            get { 
                // 

 
                // Must be saved when
                // 1. There is a registered event handler for SelectedIndexChanged
                // 2. Control is not enabled or visible, because the browser's post data will not include this control
                // 3. The instance is a derived instance, which might be overriding the OnTextChanged method 

                if (TextMode == TextBoxMode.Password) { 
                    return false; 
                }
 
                #if SHIPPINGADAPTERS
                if(_adapter != null) {
                    TextBoxAdapter adapter = _adapter as TextBoxAdapter;
                    if (adapter != null && adapter.AlwaysSaveNonPasswordViewState) { 
                        return true;
                    } 
                } 
                #endif
 
                if ((Events[EventTextChanged] != null) ||
                    (IsEnabled == false) ||
                    (Visible == false) ||
                    (ReadOnly) || 
                    (this.GetType() != typeof(TextBox))) {
                    return true; 
                } 

                return false; 
            }
        }

 
        /// <devdoc>
        ///    <para>A protected property. Gets the HTML tag 
        ///       for the text box control.</para> 
        /// </devdoc>
        protected override HtmlTextWriterTag TagKey { 
            get {
                if (TextMode == TextBoxMode.MultiLine)
                    return HtmlTextWriterTag.Textarea;
                else 
                    return HtmlTextWriterTag.Input;
            } 
        } 

 
        /// <devdoc>
        ///    <para> Gets
        ///       or sets the text content of the text box.</para>
        /// </devdoc> 
        [
        Localizable(true), 
        Bindable(true, BindingDirection.TwoWay), 
        WebCategory("Appearance"),
        DefaultValue(""), 
        WebSysDescription(SR.TextBox_Text),
        PersistenceMode(PersistenceMode.EncodedInnerDefaultProperty),
        Editor("System.ComponentModel.Design.MultilineStringEditor," + AssemblyRef.SystemDesign, typeof(UITypeEditor))
        ] 
        public virtual string Text {
            get { 
                string s = (string)ViewState["Text"]; 
                return((s == null) ? String.Empty : s);
            } 
            set {
                ViewState["Text"] = value;
            }
        } 

 
        [ 
        WebCategory("Behavior"),
        Themeable(false), 
        DefaultValue(""),
        WebSysDescription(SR.PostBackControl_ValidationGroup)
        ]
        public virtual string ValidationGroup { 
            get {
                string s = (string)ViewState["ValidationGroup"]; 
                return((s == null) ? string.Empty : s); 
            }
            set { 
                ViewState["ValidationGroup"] = value;
            }
        }
 

        /// <devdoc> 
        ///    Gets or sets a value indicating whether the 
        ///    text content wraps within the text box.
        /// </devdoc> 
        [
        WebCategory("Layout"),
        DefaultValue(true),
        WebSysDescription(SR.TextBox_Wrap) 
        ]
        public virtual bool Wrap { 
            get { 
                object b = ViewState["Wrap"];
                return((b == null) ? true : (bool)b); 
            }
            set {
                ViewState["Wrap"] = value;
            } 
        }
 
 

        /// <devdoc> 
        ///    <para>Occurs when the content of the text box is
        ///       changed upon server postback.</para>
        /// </devdoc>
        [ 
        WebCategory("Action"),
        WebSysDescription(SR.TextBox_OnTextChanged) 
        ] 
        public event EventHandler TextChanged {
            add { 
                Events.AddHandler(EventTextChanged, value);
            }
            remove {
                Events.RemoveHandler(EventTextChanged, value); 
            }
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc>
        protected override void AddAttributesToRender(HtmlTextWriter writer) {
 
            // Make sure we are in a form tag with runat=server.
            Page page = Page; 
            if (page != null) { 
                page.VerifyRenderingInServerForm(this);
            } 

            string uniqueID = UniqueID;
            if (uniqueID != null) {
                writer.AddAttribute(HtmlTextWriterAttribute.Name, uniqueID); 
            }
 
            TextBoxMode mode = TextMode; 

            if (mode == TextBoxMode.MultiLine) { 
                // MultiLine renders as textarea

                int rows = Rows;
                int columns = Columns; 
                bool adapterRenderZeroRowCol = false;
 
#if SHIPPINGADAPTERS 
                TextBoxAdapter adapter = _adapter as TextBoxAdapter;
                if (adapter != null) { 
                    if (!adapter.SuppressTextareaZeroRowColValue) {
                        adapterRenderZeroRowCol = true;
                    }
                } 
                else {
#endif 
                    if (!EnableLegacyRendering) { 
                        // VSWhidbey 497755
                        if (rows == 0) { 
                            rows = DefaultMutliLineRows;
                        }
                        if (columns == 0) {
                            columns = DefaultMutliLineColumns; 
                        }
                    } 
#if SHIPPINGADAPTERS 
                }
#endif 

                if (rows > 0 || adapterRenderZeroRowCol) {
                    writer.AddAttribute(HtmlTextWriterAttribute.Rows, rows.ToString(NumberFormatInfo.InvariantInfo));
                } 

                if (columns > 0 || adapterRenderZeroRowCol) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Cols, columns.ToString(NumberFormatInfo.InvariantInfo)); 
                }
 
                if (!Wrap) {
                    writer.AddAttribute(HtmlTextWriterAttribute.Wrap,"off");
                }
            } 
            else {
                // SingleLine renders as input 
                if (mode == TextBoxMode.SingleLine) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Type,"text");
 
                    // Renders the vcard_name attribute so that client browsers can support autocomplete
                    if (AutoCompleteType != AutoCompleteType.None &&
                        Context != null && Context.Request.Browser["supportsVCard"] == "true") {
 
                        if (AutoCompleteType == AutoCompleteType.Disabled) {
                            writer.AddAttribute(HtmlTextWriterAttribute.AutoComplete, "off"); 
                        } 
                        else if (AutoCompleteType == AutoCompleteType.Search) {
                            writer.AddAttribute(HtmlTextWriterAttribute.VCardName, "search"); 
                        }
                        else if (AutoCompleteType == AutoCompleteType.HomeCountryRegion) {
                            writer.AddAttribute(HtmlTextWriterAttribute.VCardName, "HomeCountry");
                        } 
                        else if (AutoCompleteType == AutoCompleteType.BusinessCountryRegion) {
                            writer.AddAttribute(HtmlTextWriterAttribute.VCardName, "BusinessCountry"); 
                        } 
                        else {
                            string name = Enum.Format(typeof(AutoCompleteType), AutoCompleteType, "G"); 
                            // Business and Home properties need to be prefixed with "."
                            if (name.StartsWith("Business", StringComparison.Ordinal)) {
                                name = name.Insert(8, ".");
                            } 
                            else if (name.StartsWith("Home", StringComparison.Ordinal)) {
                                name = name.Insert(4, "."); 
                            } 
                            writer.AddAttribute(HtmlTextWriterAttribute.VCardName, "vCard." + name);
                        } 
                    }

                    // only render value if we're not a password
                    string s = Text; 
                    if (s.Length > 0)
                        writer.AddAttribute(HtmlTextWriterAttribute.Value, s); 
                } 
                else if (mode == TextBoxMode.Password) {
                    writer.AddAttribute(HtmlTextWriterAttribute.Type, "password"); 
                }

                int n = MaxLength;
                if (n > 0) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Maxlength, n.ToString(NumberFormatInfo.InvariantInfo));
                } 
                n = Columns; 
                if (n > 0) {
                    writer.AddAttribute(HtmlTextWriterAttribute.Size, n.ToString(NumberFormatInfo.InvariantInfo)); 
                }
            }

            if (ReadOnly) { 
                writer.AddAttribute(HtmlTextWriterAttribute.ReadOnly, "readonly");
            } 
 
            if (AutoPostBack && (page != null) && page.ClientSupportsJavaScript) {
                string onChange = null; 
                if (HasAttributes) {
                    onChange = Attributes["onchange"];
                    if (onChange != null) {
                        onChange = Util.EnsureEndWithSemiColon(onChange); 
                        Attributes.Remove("onchange");
                    } 
                } 

                PostBackOptions options = new PostBackOptions(this, String.Empty); 

                // ASURT 98368
                // Need to merge the autopostback script with the user script
                if (CausesValidation) { 
                    options.PerformValidation = true;
                    options.ValidationGroup = ValidationGroup; 
                } 

                if (page.Form != null) { 
                    options.AutoPostBack = true;
                }

                onChange = Util.MergeScript(onChange, page.ClientScript.GetPostBackEventReference(options, true)); 
                writer.AddAttribute(HtmlTextWriterAttribute.Onchange, onChange);
 
                // VSWhidbey 482068: Enter key should be preserved in mult-line 
                // textbox so the textBoxKeyHandlerCall should not be hooked up
                if (mode != TextBoxMode.MultiLine) { 
                    string onKeyPress = _textBoxKeyHandlerCall;
                    if (HasAttributes) {
                        string userOnKeyPress = Attributes["onkeypress"];
                        if (userOnKeyPress != null) { 
                            onKeyPress += userOnKeyPress;
                            Attributes.Remove("onkeypress"); 
                        } 
                    }
                    writer.AddAttribute("onkeypress", onKeyPress); 
                }

                if (EnableLegacyRendering) {
                    writer.AddAttribute("language", "javascript", false); 
                }
            } 
            else if (page != null) { 
                page.ClientScript.RegisterForEventValidation(this.UniqueID, String.Empty);
            } 

            if (Enabled && !IsEnabled) {
                // We need to do the cascade effect on the server, because the browser
                // only renders as disabled, but doesn't disable the functionality. 
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
            } 
 
            base.AddAttributesToRender(writer);
        } 


        /// <internalonly/>
        /// <devdoc> 
        ///    Overridden to only allow literal controls to be added as Text property.
        /// </devdoc> 
        protected override void AddParsedSubObject(object obj) { 
            if (obj is LiteralControl) {
                Text = ((LiteralControl)obj).Text; 
            }
            else {
                throw new HttpException(SR.GetString(SR.Cannot_Have_Children_Of_Type, "TextBox", obj.GetType().Name.ToString(CultureInfo.InvariantCulture)));
            } 
        }
 
 
        /// <internalonly/>
        protected internal override void OnPreRender(EventArgs e) { 
            base.OnPreRender(e);

            Page page = Page;
            if ((page != null) && IsEnabled) { 
                if (SaveTextViewState == false) {
                    // Store a client-side array of enabled control, so we can re-enable them on 
                    // postback (in case they are disabled client-side) 
                    // Postback is needed when view state for the Text property is disabled
                    page.RegisterEnabledControl(this); 
                }

                if (AutoPostBack) {
                    page.RegisterWebFormsScript(); 
                    page.RegisterPostBackScript();
                    page.RegisterFocusScript(); 
                } 
            }
        } 


        /// <internalonly/>
        /// <devdoc> 
        /// <para>Loads the posted text box content if it is different
        /// from the last posting.</para> 
        /// </devdoc> 
        bool IPostBackDataHandler.LoadPostData(string postDataKey, NameValueCollection postCollection) {
            return LoadPostData(postDataKey, postCollection); 
        }


        /// <internalonly/> 
        /// <devdoc>
        /// <para>Loads the posted text box content if it is different 
        /// from the last posting.</para> 
        /// </devdoc>
        protected virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection) { 
            ValidateEvent(postDataKey);

            string current = Text;
            string postData = postCollection[postDataKey]; 

            // VSWhidbey 442850: Everett had current.Equals(postData), and it is 
            // equivalent to the option StringComparison.Ordinal in Whidbey 
            if (!ReadOnly && !current.Equals(postData, StringComparison.Ordinal)) {
                Text = postData; 
                return true;
            }
            return false;
        } 

 
 
        /// <devdoc>
        /// <para> Raises the <see langword='TextChanged'/> event.</para> 
        /// </devdoc>
        protected virtual void OnTextChanged(EventArgs e) {
            EventHandler onChangeHandler = (EventHandler)Events[EventTextChanged];
            if (onChangeHandler != null) onChangeHandler(this,e); 
        }
 
 
        /// <internalonly/>
        /// <devdoc> 
        /// <para>Invokes the <see cref='System.Web.UI.WebControls.TextBox.OnTextChanged'/> method
        /// whenever posted data for the text box has changed.</para>
        /// </devdoc>
        void IPostBackDataHandler.RaisePostDataChangedEvent() { 
            RaisePostDataChangedEvent();
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// <para>Invokes the <see cref='System.Web.UI.WebControls.TextBox.OnTextChanged'/> method
        /// whenever posted data for the text box has changed.</para>
        /// </devdoc> 
        protected virtual void RaisePostDataChangedEvent() {
            if (AutoPostBack && !Page.IsPostBackEventControlRegistered) { 
                // VSWhidbey 204824 
                Page.AutoPostBackControl = this;
 
                if (CausesValidation) {
                     Page.Validate(ValidationGroup);
                }
             } 
             OnTextChanged(EventArgs.Empty);
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc>
        protected internal override void Render(HtmlTextWriter writer) {
            RenderBeginTag(writer); 
            if (TextMode == TextBoxMode.MultiLine)
                HttpUtility.HtmlEncode(Text, writer); 
            RenderEndTag(writer); 
        }
 
        protected override object SaveViewState() {
            if (SaveTextViewState == false) {
                ViewState.SetItemDirty("Text", false);
            } 
            return base.SaveViewState();
        } 
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="TextBox.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
    using System.Text; 
    using System.ComponentModel;
    using System.Drawing.Design; 
    using System;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls.Adapters; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Globalization; 
    using System.Security.Permissions;
 

    /// <devdoc>
    /// <para>Interacts with the parser to build a <see cref='System.Web.UI.WebControls.TextBox'/> control.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class TextBoxControlBuilder : ControlBuilder { 

 
        /// <internalonly/>
        /// <devdoc>
        ///    Specifies whether white space literals are allowed.
        /// </devdoc> 
        public override bool AllowWhitespaceLiterals() {
            return false; 
        } 

 
        public override bool HtmlDecodeLiterals() {
            // TextBox text gets rendered as an encoded attribute value or encoded content.

            // At parse time text specified as an attribute gets decoded, and so text specified as a 
            // literal needs to go through the same process.
 
            return true; 
        }
    } 



    /// <devdoc> 
    ///    <para>Constructs a text box and defines its properties.</para>
    /// </devdoc> 
    [ 
    ControlBuilderAttribute(typeof(TextBoxControlBuilder)),
    ControlValueProperty("Text"), 
    DataBindingHandler("System.Web.UI.Design.TextDataBindingHandler, " + AssemblyRef.SystemDesign),
    DefaultProperty("Text"),
    ValidationProperty("Text"),
    DefaultEvent("TextChanged"), 
    Designer("System.Web.UI.Design.WebControls.PreviewControlDesigner, " + AssemblyRef.SystemDesign),
    ParseChildren(true, "Text"), 
    SupportsEventValidation, 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class TextBox : WebControl, IPostBackDataHandler, IEditableTextControl {

        private static readonly object EventTextChanged = new Object(); 

        private const string _textBoxKeyHandlerCall = "if (WebForm_TextBoxKeyHandler(event) == false) return false;"; 
 
        private const int DefaultMutliLineRows = 2;
        private const int DefaultMutliLineColumns = 20; 

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.TextBox'/> class.</para>
        /// </devdoc> 
        public TextBox() : base(HtmlTextWriterTag.Input) {
        } 
 

        [ 
        DefaultValue(AutoCompleteType.None),
        Themeable(false),
        WebCategory("Behavior"),
        WebSysDescription(SR.TextBox_AutoCompleteType) 
        ]
        public virtual AutoCompleteType AutoCompleteType { 
            get { 
                object obj = ViewState["AutoCompleteType"];
                return (obj == null) ? AutoCompleteType.None : (AutoCompleteType) obj; 
            }
            set {
                if (value < AutoCompleteType.None || value > AutoCompleteType.Search) {
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["AutoCompleteType"] = value; 
            } 
        }
 

        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether an automatic
        ///       postback to the server will occur whenever the user changes the 
        ///       content of the text box.</para>
        /// </devdoc> 
        [ 
        DefaultValue(false),
        Themeable(false), 
        WebCategory("Behavior"),
        WebSysDescription(SR.TextBox_AutoPostBack),
        ]
        public virtual bool AutoPostBack { 
            get {
                object b = ViewState["AutoPostBack"]; 
                return((b == null) ? false : (bool)b); 
            }
            set { 
                ViewState["AutoPostBack"] = value;
            }
        }
 

        [ 
        DefaultValue(false), 
        Themeable(false),
        WebCategory("Behavior"), 
        WebSysDescription(SR.AutoPostBackControl_CausesValidation)
        ]
        public virtual bool CausesValidation {
            get { 
                object b = ViewState["CausesValidation"];
                return((b == null) ? false : (bool)b); 
            } 
            set {
                ViewState["CausesValidation"] = value; 
            }
        }

 
        /// <devdoc>
        ///    <para>Gets or sets the display 
        ///       width of the text box in characters.</para> 
        /// </devdoc>
        [ 
        WebCategory("Appearance"),
        DefaultValue(0),
        WebSysDescription(SR.TextBox_Columns)
        ] 
        public virtual int Columns {
            get { 
                object o = ViewState["Columns"]; 
                return((o == null) ? 0 : (int)o);
            } 
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("Columns", SR.GetString(SR.TextBox_InvalidColumns));
                } 
                ViewState["Columns"] = value;
            } 
        } 

 
        /// <devdoc>
        ///    <para>Gets or sets the maximum number of characters allowed in the text box.</para>
        /// </devdoc>
        [ 
        DefaultValue(0),
        Themeable(false), 
        WebCategory("Behavior"), 
        WebSysDescription(SR.TextBox_MaxLength),
        ] 
        public virtual int MaxLength {
            get {
                object o = ViewState["MaxLength"];
                return((o == null) ? 0 : (int)o); 
            }
            set { 
                if (value < 0) { 
                    throw new ArgumentOutOfRangeException("value");
                } 
                ViewState["MaxLength"] = value;
            }
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets the behavior mode of the text box.</para>
        /// </devdoc> 
        [
        DefaultValue(TextBoxMode.SingleLine),
        Themeable(false),
        WebCategory("Behavior"), 
        WebSysDescription(SR.TextBox_TextMode)
        ] 
        public virtual TextBoxMode TextMode { 
            get {
                object mode = ViewState["Mode"]; 
                return((mode == null) ? TextBoxMode.SingleLine : (TextBoxMode)mode);
            }
            set {
                if (value < TextBoxMode.SingleLine || value > TextBoxMode.Password) { 
                    throw new ArgumentOutOfRangeException("value");
                } 
                ViewState["Mode"] = value; 
            }
        } 


        /// <devdoc>
        ///    <para>Whether the textbox is in read-only mode.</para> 
        /// </devdoc>
        [ 
        Bindable(true), 
        DefaultValue(false),
        Themeable(false), 
        WebCategory("Behavior"),
        WebSysDescription(SR.TextBox_ReadOnly)
        ]
        public virtual bool ReadOnly { 
            get {
                object o = ViewState["ReadOnly"]; 
                return((o == null) ? false : (bool)o); 
            }
            set { 
                ViewState["ReadOnly"] = value;
            }
        }
 

        /// <devdoc> 
        ///    <para> Gets or sets the display height of a multiline text box.</para> 
        /// </devdoc>
        [ 
        DefaultValue(0),
        Themeable(false),
        WebCategory("Behavior"),
        WebSysDescription(SR.TextBox_Rows) 
        ]
        public virtual int Rows { 
            get { 
                object o = ViewState["Rows"];
                return((o == null) ? 0 : (int)o); 
            }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("Rows", SR.GetString(SR.TextBox_InvalidRows)); 
                }
                ViewState["Rows"] = value; 
            } 
        }
 
        /// <devdoc>
        ///    Determines whether the Text must be stored in view state, to
        ///    optimize the size of the saved state.
        /// </devdoc> 
        private bool SaveTextViewState {
            get { 
                // 

 
                // Must be saved when
                // 1. There is a registered event handler for SelectedIndexChanged
                // 2. Control is not enabled or visible, because the browser's post data will not include this control
                // 3. The instance is a derived instance, which might be overriding the OnTextChanged method 

                if (TextMode == TextBoxMode.Password) { 
                    return false; 
                }
 
                #if SHIPPINGADAPTERS
                if(_adapter != null) {
                    TextBoxAdapter adapter = _adapter as TextBoxAdapter;
                    if (adapter != null && adapter.AlwaysSaveNonPasswordViewState) { 
                        return true;
                    } 
                } 
                #endif
 
                if ((Events[EventTextChanged] != null) ||
                    (IsEnabled == false) ||
                    (Visible == false) ||
                    (ReadOnly) || 
                    (this.GetType() != typeof(TextBox))) {
                    return true; 
                } 

                return false; 
            }
        }

 
        /// <devdoc>
        ///    <para>A protected property. Gets the HTML tag 
        ///       for the text box control.</para> 
        /// </devdoc>
        protected override HtmlTextWriterTag TagKey { 
            get {
                if (TextMode == TextBoxMode.MultiLine)
                    return HtmlTextWriterTag.Textarea;
                else 
                    return HtmlTextWriterTag.Input;
            } 
        } 

 
        /// <devdoc>
        ///    <para> Gets
        ///       or sets the text content of the text box.</para>
        /// </devdoc> 
        [
        Localizable(true), 
        Bindable(true, BindingDirection.TwoWay), 
        WebCategory("Appearance"),
        DefaultValue(""), 
        WebSysDescription(SR.TextBox_Text),
        PersistenceMode(PersistenceMode.EncodedInnerDefaultProperty),
        Editor("System.ComponentModel.Design.MultilineStringEditor," + AssemblyRef.SystemDesign, typeof(UITypeEditor))
        ] 
        public virtual string Text {
            get { 
                string s = (string)ViewState["Text"]; 
                return((s == null) ? String.Empty : s);
            } 
            set {
                ViewState["Text"] = value;
            }
        } 

 
        [ 
        WebCategory("Behavior"),
        Themeable(false), 
        DefaultValue(""),
        WebSysDescription(SR.PostBackControl_ValidationGroup)
        ]
        public virtual string ValidationGroup { 
            get {
                string s = (string)ViewState["ValidationGroup"]; 
                return((s == null) ? string.Empty : s); 
            }
            set { 
                ViewState["ValidationGroup"] = value;
            }
        }
 

        /// <devdoc> 
        ///    Gets or sets a value indicating whether the 
        ///    text content wraps within the text box.
        /// </devdoc> 
        [
        WebCategory("Layout"),
        DefaultValue(true),
        WebSysDescription(SR.TextBox_Wrap) 
        ]
        public virtual bool Wrap { 
            get { 
                object b = ViewState["Wrap"];
                return((b == null) ? true : (bool)b); 
            }
            set {
                ViewState["Wrap"] = value;
            } 
        }
 
 

        /// <devdoc> 
        ///    <para>Occurs when the content of the text box is
        ///       changed upon server postback.</para>
        /// </devdoc>
        [ 
        WebCategory("Action"),
        WebSysDescription(SR.TextBox_OnTextChanged) 
        ] 
        public event EventHandler TextChanged {
            add { 
                Events.AddHandler(EventTextChanged, value);
            }
            remove {
                Events.RemoveHandler(EventTextChanged, value); 
            }
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc>
        protected override void AddAttributesToRender(HtmlTextWriter writer) {
 
            // Make sure we are in a form tag with runat=server.
            Page page = Page; 
            if (page != null) { 
                page.VerifyRenderingInServerForm(this);
            } 

            string uniqueID = UniqueID;
            if (uniqueID != null) {
                writer.AddAttribute(HtmlTextWriterAttribute.Name, uniqueID); 
            }
 
            TextBoxMode mode = TextMode; 

            if (mode == TextBoxMode.MultiLine) { 
                // MultiLine renders as textarea

                int rows = Rows;
                int columns = Columns; 
                bool adapterRenderZeroRowCol = false;
 
#if SHIPPINGADAPTERS 
                TextBoxAdapter adapter = _adapter as TextBoxAdapter;
                if (adapter != null) { 
                    if (!adapter.SuppressTextareaZeroRowColValue) {
                        adapterRenderZeroRowCol = true;
                    }
                } 
                else {
#endif 
                    if (!EnableLegacyRendering) { 
                        // VSWhidbey 497755
                        if (rows == 0) { 
                            rows = DefaultMutliLineRows;
                        }
                        if (columns == 0) {
                            columns = DefaultMutliLineColumns; 
                        }
                    } 
#if SHIPPINGADAPTERS 
                }
#endif 

                if (rows > 0 || adapterRenderZeroRowCol) {
                    writer.AddAttribute(HtmlTextWriterAttribute.Rows, rows.ToString(NumberFormatInfo.InvariantInfo));
                } 

                if (columns > 0 || adapterRenderZeroRowCol) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Cols, columns.ToString(NumberFormatInfo.InvariantInfo)); 
                }
 
                if (!Wrap) {
                    writer.AddAttribute(HtmlTextWriterAttribute.Wrap,"off");
                }
            } 
            else {
                // SingleLine renders as input 
                if (mode == TextBoxMode.SingleLine) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Type,"text");
 
                    // Renders the vcard_name attribute so that client browsers can support autocomplete
                    if (AutoCompleteType != AutoCompleteType.None &&
                        Context != null && Context.Request.Browser["supportsVCard"] == "true") {
 
                        if (AutoCompleteType == AutoCompleteType.Disabled) {
                            writer.AddAttribute(HtmlTextWriterAttribute.AutoComplete, "off"); 
                        } 
                        else if (AutoCompleteType == AutoCompleteType.Search) {
                            writer.AddAttribute(HtmlTextWriterAttribute.VCardName, "search"); 
                        }
                        else if (AutoCompleteType == AutoCompleteType.HomeCountryRegion) {
                            writer.AddAttribute(HtmlTextWriterAttribute.VCardName, "HomeCountry");
                        } 
                        else if (AutoCompleteType == AutoCompleteType.BusinessCountryRegion) {
                            writer.AddAttribute(HtmlTextWriterAttribute.VCardName, "BusinessCountry"); 
                        } 
                        else {
                            string name = Enum.Format(typeof(AutoCompleteType), AutoCompleteType, "G"); 
                            // Business and Home properties need to be prefixed with "."
                            if (name.StartsWith("Business", StringComparison.Ordinal)) {
                                name = name.Insert(8, ".");
                            } 
                            else if (name.StartsWith("Home", StringComparison.Ordinal)) {
                                name = name.Insert(4, "."); 
                            } 
                            writer.AddAttribute(HtmlTextWriterAttribute.VCardName, "vCard." + name);
                        } 
                    }

                    // only render value if we're not a password
                    string s = Text; 
                    if (s.Length > 0)
                        writer.AddAttribute(HtmlTextWriterAttribute.Value, s); 
                } 
                else if (mode == TextBoxMode.Password) {
                    writer.AddAttribute(HtmlTextWriterAttribute.Type, "password"); 
                }

                int n = MaxLength;
                if (n > 0) { 
                    writer.AddAttribute(HtmlTextWriterAttribute.Maxlength, n.ToString(NumberFormatInfo.InvariantInfo));
                } 
                n = Columns; 
                if (n > 0) {
                    writer.AddAttribute(HtmlTextWriterAttribute.Size, n.ToString(NumberFormatInfo.InvariantInfo)); 
                }
            }

            if (ReadOnly) { 
                writer.AddAttribute(HtmlTextWriterAttribute.ReadOnly, "readonly");
            } 
 
            if (AutoPostBack && (page != null) && page.ClientSupportsJavaScript) {
                string onChange = null; 
                if (HasAttributes) {
                    onChange = Attributes["onchange"];
                    if (onChange != null) {
                        onChange = Util.EnsureEndWithSemiColon(onChange); 
                        Attributes.Remove("onchange");
                    } 
                } 

                PostBackOptions options = new PostBackOptions(this, String.Empty); 

                // ASURT 98368
                // Need to merge the autopostback script with the user script
                if (CausesValidation) { 
                    options.PerformValidation = true;
                    options.ValidationGroup = ValidationGroup; 
                } 

                if (page.Form != null) { 
                    options.AutoPostBack = true;
                }

                onChange = Util.MergeScript(onChange, page.ClientScript.GetPostBackEventReference(options, true)); 
                writer.AddAttribute(HtmlTextWriterAttribute.Onchange, onChange);
 
                // VSWhidbey 482068: Enter key should be preserved in mult-line 
                // textbox so the textBoxKeyHandlerCall should not be hooked up
                if (mode != TextBoxMode.MultiLine) { 
                    string onKeyPress = _textBoxKeyHandlerCall;
                    if (HasAttributes) {
                        string userOnKeyPress = Attributes["onkeypress"];
                        if (userOnKeyPress != null) { 
                            onKeyPress += userOnKeyPress;
                            Attributes.Remove("onkeypress"); 
                        } 
                    }
                    writer.AddAttribute("onkeypress", onKeyPress); 
                }

                if (EnableLegacyRendering) {
                    writer.AddAttribute("language", "javascript", false); 
                }
            } 
            else if (page != null) { 
                page.ClientScript.RegisterForEventValidation(this.UniqueID, String.Empty);
            } 

            if (Enabled && !IsEnabled) {
                // We need to do the cascade effect on the server, because the browser
                // only renders as disabled, but doesn't disable the functionality. 
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
            } 
 
            base.AddAttributesToRender(writer);
        } 


        /// <internalonly/>
        /// <devdoc> 
        ///    Overridden to only allow literal controls to be added as Text property.
        /// </devdoc> 
        protected override void AddParsedSubObject(object obj) { 
            if (obj is LiteralControl) {
                Text = ((LiteralControl)obj).Text; 
            }
            else {
                throw new HttpException(SR.GetString(SR.Cannot_Have_Children_Of_Type, "TextBox", obj.GetType().Name.ToString(CultureInfo.InvariantCulture)));
            } 
        }
 
 
        /// <internalonly/>
        protected internal override void OnPreRender(EventArgs e) { 
            base.OnPreRender(e);

            Page page = Page;
            if ((page != null) && IsEnabled) { 
                if (SaveTextViewState == false) {
                    // Store a client-side array of enabled control, so we can re-enable them on 
                    // postback (in case they are disabled client-side) 
                    // Postback is needed when view state for the Text property is disabled
                    page.RegisterEnabledControl(this); 
                }

                if (AutoPostBack) {
                    page.RegisterWebFormsScript(); 
                    page.RegisterPostBackScript();
                    page.RegisterFocusScript(); 
                } 
            }
        } 


        /// <internalonly/>
        /// <devdoc> 
        /// <para>Loads the posted text box content if it is different
        /// from the last posting.</para> 
        /// </devdoc> 
        bool IPostBackDataHandler.LoadPostData(string postDataKey, NameValueCollection postCollection) {
            return LoadPostData(postDataKey, postCollection); 
        }


        /// <internalonly/> 
        /// <devdoc>
        /// <para>Loads the posted text box content if it is different 
        /// from the last posting.</para> 
        /// </devdoc>
        protected virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection) { 
            ValidateEvent(postDataKey);

            string current = Text;
            string postData = postCollection[postDataKey]; 

            // VSWhidbey 442850: Everett had current.Equals(postData), and it is 
            // equivalent to the option StringComparison.Ordinal in Whidbey 
            if (!ReadOnly && !current.Equals(postData, StringComparison.Ordinal)) {
                Text = postData; 
                return true;
            }
            return false;
        } 

 
 
        /// <devdoc>
        /// <para> Raises the <see langword='TextChanged'/> event.</para> 
        /// </devdoc>
        protected virtual void OnTextChanged(EventArgs e) {
            EventHandler onChangeHandler = (EventHandler)Events[EventTextChanged];
            if (onChangeHandler != null) onChangeHandler(this,e); 
        }
 
 
        /// <internalonly/>
        /// <devdoc> 
        /// <para>Invokes the <see cref='System.Web.UI.WebControls.TextBox.OnTextChanged'/> method
        /// whenever posted data for the text box has changed.</para>
        /// </devdoc>
        void IPostBackDataHandler.RaisePostDataChangedEvent() { 
            RaisePostDataChangedEvent();
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// <para>Invokes the <see cref='System.Web.UI.WebControls.TextBox.OnTextChanged'/> method
        /// whenever posted data for the text box has changed.</para>
        /// </devdoc> 
        protected virtual void RaisePostDataChangedEvent() {
            if (AutoPostBack && !Page.IsPostBackEventControlRegistered) { 
                // VSWhidbey 204824 
                Page.AutoPostBackControl = this;
 
                if (CausesValidation) {
                     Page.Validate(ValidationGroup);
                }
             } 
             OnTextChanged(EventArgs.Empty);
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc>
        protected internal override void Render(HtmlTextWriter writer) {
            RenderBeginTag(writer); 
            if (TextMode == TextBoxMode.MultiLine)
                HttpUtility.HtmlEncode(Text, writer); 
            RenderEndTag(writer); 
        }
 
        protected override object SaveViewState() {
            if (SaveTextViewState == false) {
                ViewState.SetItemDirty("Text", false);
            } 
            return base.SaveViewState();
        } 
    } 
}
