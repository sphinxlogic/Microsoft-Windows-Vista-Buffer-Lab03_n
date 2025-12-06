//------------------------------------------------------------------------------ 
// <copyright file="RadioButtonList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.Web; 
    using System.Web.UI;
    using System.Security.Permissions; 
 

    /// <devdoc> 
    ///    <para>Generates a single-selection radio button group and
    ///       defines its properties.</para>
    /// </devdoc>
    [ 
    ValidationProperty("SelectedItem"),
    SupportsEventValidation, 
    ] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class RadioButtonList : ListControl, IRepeatInfoUser, INamingContainer, IPostBackDataHandler
#if ORCAS
        , IItemPaginationInfo, IPaginationContainer
#endif 
    {
 
        RadioButton _controlToRepeat; 
        private bool _cachedIsEnabled;
        private bool _cachedRegisterEnabled; 
        private int _offset;
#if ORCAS
        private int _itemCount;
#endif 

 
        public RadioButtonList() { 
            _offset = 0;
#if ORCAS 
            _itemCount = -1;
#endif
        }
 

        /// <devdoc> 
        ///  CellPadding property. 
        ///  The padding between each item.
        /// </devdoc> 
        [
        WebCategory("Layout"),
        DefaultValue(-1),
        WebSysDescription(SR.RadioButtonList_CellPadding) 
        ]
        public virtual int CellPadding { 
            get { 
                if (ControlStyleCreated == false) {
                    return -1; 
                }
                return ((TableStyle)ControlStyle).CellPadding;
            }
            set { 
                ((TableStyle)ControlStyle).CellPadding = value;
            } 
        } 

 
        /// <devdoc>
        ///  CellSpacing property.
        ///  The spacing between each item.
        /// </devdoc> 
        [
        WebCategory("Layout"), 
        DefaultValue(-1), 
        WebSysDescription(SR.RadioButtonList_CellSpacing)
        ] 
        public virtual int CellSpacing {
            get {
                if (ControlStyleCreated == false) {
                    return -1; 
                }
                return ((TableStyle)ControlStyle).CellSpacing; 
            } 
            set {
                ((TableStyle)ControlStyle).CellSpacing = value; 
            }
        }

        private RadioButton ControlToRepeat { 
            get {
                if (_controlToRepeat != null) 
                    return _controlToRepeat; 

                _controlToRepeat = new RadioButton(); 
                _controlToRepeat.EnableViewState = false;
                Controls.Add(_controlToRepeat);

                // A note is that we don't need to set the GroupName on the radio 
                // button as the radio button would simply use its naming container
                // (which would be this control) as its name attribute for postback. 
 
                // Apply properties that are the same for each radio button
                _controlToRepeat.AutoPostBack = AutoPostBack; 
                _controlToRepeat.CausesValidation = CausesValidation;
                _controlToRepeat.ValidationGroup = ValidationGroup;

                return _controlToRepeat; 
            }
        } 
 

        /// <devdoc> 
        ///    Indicates the column count of radio buttons
        ///    within the group.
        /// </devdoc>
        [ 
        WebCategory("Layout"),
        DefaultValue(0), 
        WebSysDescription(SR.RadioButtonList_RepeatColumns) 
        ]
        public virtual int RepeatColumns { 
            get {
                object o = ViewState["RepeatColumns"];
                return((o == null) ? 0 : (int)o);
            } 
            set {
                if (value < 0) { 
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["RepeatColumns"] = value; 
            }
        }

 
        /// <devdoc>
        ///    <para>Gets or sets the direction of flow of 
        ///       the radio buttons within the group.</para> 
        /// </devdoc>
        [ 
        WebCategory("Layout"),
        DefaultValue(RepeatDirection.Vertical),
        WebSysDescription(SR.Item_RepeatDirection)
        ] 
        public virtual RepeatDirection RepeatDirection {
            get { 
                object o = ViewState["RepeatDirection"]; 
                return((o == null) ? RepeatDirection.Vertical : (RepeatDirection)o);
            } 
            set {
                if (value < RepeatDirection.Horizontal || value > RepeatDirection.Vertical) {
                    throw new ArgumentOutOfRangeException("value");
                } 
                ViewState["RepeatDirection"] = value;
            } 
        } 

 
        /// <devdoc>
        ///    <para>Indicates the layout of radio buttons within the
        ///       group.</para>
        /// </devdoc> 
        [
        WebCategory("Layout"), 
        DefaultValue(RepeatLayout.Table), 
        WebSysDescription(SR.WebControl_RepeatLayout)
        ] 
        public virtual RepeatLayout RepeatLayout {
            get {
                object o = ViewState["RepeatLayout"];
                return((o == null) ? RepeatLayout.Table : (RepeatLayout)o); 
            }
            set { 
                if (value < RepeatLayout.Table || value > RepeatLayout.Flow) { 
                    throw new ArgumentOutOfRangeException("value");
                } 
                ViewState["RepeatLayout"] = value;
            }
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Indicates the label text alignment for the radio buttons within the group.</para>
        /// </devdoc> 
        [
        WebCategory("Appearance"),
        DefaultValue(TextAlign.Right),
        WebSysDescription(SR.WebControl_TextAlign) 
        ]
        public virtual TextAlign TextAlign { 
            get { 
                object align = ViewState["TextAlign"];
                return((align == null) ? TextAlign.Right : (TextAlign)align); 
            }
            set {
                if (value < TextAlign.Left || value > TextAlign.Right) {
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["TextAlign"] = value; 
            } 
        }
 

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc> 
        protected override Style CreateControlStyle() {
            return new TableStyle(ViewState); 
        } 

 
        /// <internalonly/>
        /// <devdoc>
        /// <para>Catches auto postback from a <see cref='System.Web.UI.WebControls.RadioButton'/> in the list.</para>
        /// </devdoc> 
        protected override Control FindControl(string id, int pathOffset) {
            return this; 
        } 
#if ORCAS
        #region IItemPaginationInfo implementation 

        int IItemPaginationInfo.FirstVisibleItemIndex {
            get {
                return this.FirstVisibleItemIndex; 
            }
        } 
 

        protected int FirstVisibleItemIndex { 
            get {
                return _offset;
            }
        } 

 
        /// <devdoc> 
        ///    <para>
        ///       The number of items held by the container for pagination calculations. 
        ///    </para>
        /// </devdoc>
        int IItemPaginationInfo.ItemCount {
            get { 
                return ItemCount;
            } 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       The number of items held by the container for pagination calculations.
        ///    </para> 
        /// </devdoc>
        protected virtual int ItemCount { 
            get { 
                EnsureDataBound();
                return Items.Count; 
            }
        }

 
        /// <devdoc>
        ///    <para> 
        ///       The weight for pagination calculations for each item of the container. 
        ///       Returns a constant value of 100 for RadioButtonList.
        ///    </para> 
        /// </devdoc>
        int IItemPaginationInfo.ItemWeight {
            get {
                return ItemWeight; 
            }
        } 
 

        /// <devdoc> 
        ///    <para>
        ///       The weight for pagination calculations for each item of the container.
        ///       Returns a constant value of 100 for RadioButtonList.
        ///    </para> 
        /// </devdoc>
        protected virtual int ItemWeight { 
            get { 
                return 100;
            } 
        }


        int IItemPaginationInfo.VisibleItemCount { 
            get {
                return this.VisibleItemCount; 
            } 
        }
 

        protected int VisibleItemCount {
            get {
                if (_itemCount >= 0) { 
                    return _itemCount;
                } 
                else { 
                    return ItemCount;
                } 
            }
        }

 
        /// <devdoc>
        ///    <para> 
        ///       Indicates which child items of the container are visible on the current page. 
        ///    </para>
        /// </devdoc> 
        void IItemPaginationInfo.SetVisibleItems(int firstItem, int itemCount) {
            SetVisibleItems(firstItem, itemCount);
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Indicates which child items of the container are visible on the current page.
        ///    </para> 
        /// </devdoc>
        protected virtual void SetVisibleItems(int firstItem, int itemCount) {
            _offset = firstItem;
            _itemCount = itemCount; 
        }
        #endregion 
#endif 

 
        /// <internalonly/>
        /// <devdoc>
        /// <para>Loads the posted content of the list control if it is different from the last
        /// posting.</para> 
        /// </devdoc>
        bool IPostBackDataHandler.LoadPostData(String postDataKey, NameValueCollection postCollection) { 
            return LoadPostData(postDataKey, postCollection); 
        }
 

        /// <internalonly/>
        /// <devdoc>
        /// <para>Loads the posted content of the list control if it is different from the last 
        /// posting.</para>
        /// </devdoc> 
        protected virtual bool LoadPostData(String postDataKey, NameValueCollection postCollection) { 

            // When a RadioButtonList is disabled, then there is no postback data for it. 
            // Since RadioButtonList doesn't call RegisterRequiresPostBack, this method will
            // never be called, so we don't need to worry about ignoring empty postback data.

            string post = postCollection[postDataKey]; 
            int currentSelectedIndex = SelectedIndex;
 
            EnsureDataBound(); 
            int n = Items.Count;
            for (int i=0; i < n; i++) { 
                if (post == Items[i].Value && Items[i].Enabled) {
                    ValidateEvent(postDataKey, post);

                    if (i != currentSelectedIndex) { 
                        SetPostDataSelection(i);
                        return true; 
                    } 
                    return false;
                } 
            }

            return false;
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        /// <para>Invokes the OnSelectedIndexChanged 
        /// method whenever posted data for the <see cref='System.Web.UI.WebControls.RadioButtonList'/>
        /// control has changed.</para>
        /// </devdoc>
        void IPostBackDataHandler.RaisePostDataChangedEvent() { 
            RaisePostDataChangedEvent();
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// <para>Invokes the OnSelectedIndexChanged
        /// method whenever posted data for the <see cref='System.Web.UI.WebControls.RadioButtonList'/>
        /// control has changed.</para> 
        /// </devdoc>
        protected virtual void RaisePostDataChangedEvent() { 
            if (AutoPostBack && Page != null && !Page.IsPostBackEventControlRegistered) { 
                // VSWhidbey 204824
                Page.AutoPostBackControl = this; 

                if (CausesValidation) {
                    Page.Validate(ValidationGroup);
                } 
            }
            OnSelectedIndexChanged(EventArgs.Empty); 
        } 

        protected internal override void Render(HtmlTextWriter writer) { 
#if ORCAS
            if (VisibleItemCount == 0) {
                return;
            } 
#else
            if (Items.Count == 0 && !EnableLegacyRendering) { 
                return; 
            }
#endif 

            RepeatInfo repeatInfo = new RepeatInfo();
            Style style = (ControlStyleCreated ? ControlStyle : null);
            short tabIndex = TabIndex; 
            bool undirtyTabIndex = false;
 
            // TabIndex here is special... it needs to be applied to the individual 
            // radiobuttons and not the outer control itself
 
            // cache away the TabIndex property state
            ControlToRepeat.TabIndex = tabIndex;

            if (tabIndex != 0) { 
                if (ViewState.IsItemDirty("TabIndex") == false) {
                    undirtyTabIndex = true; 
                } 
                TabIndex = 0;
            } 

            repeatInfo.RepeatColumns = RepeatColumns;
            repeatInfo.RepeatDirection = RepeatDirection;
 
            // If the device does not support tables, use the flow layout to render
            if (!DesignMode && !Context.Request.Browser.Tables) { 
                repeatInfo.RepeatLayout = RepeatLayout.Flow; 
            }
            else { 
                repeatInfo.RepeatLayout = RepeatLayout;
            }

            if (repeatInfo.RepeatLayout == RepeatLayout.Flow) { 
                repeatInfo.EnableLegacyRendering = EnableLegacyRendering;
            } 
 
            repeatInfo.RenderRepeater(writer, (IRepeatInfoUser)this, style, this);
 
            if (Page != null) {
                Page.ClientScript.RegisterForEventValidation(UniqueID);
            }
 
            // restore the state of the TabIndex property
            if (tabIndex != 0) { 
                TabIndex = tabIndex; 
            }
            if (undirtyTabIndex) { 
                ViewState.SetItemDirty("TabIndex", false);
            }
        }
 

        /// <internalonly/> 
        /// <devdoc> 
        /// </devdoc>
        bool IRepeatInfoUser.HasFooter { 
            get {
                return HasFooter;
            }
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual bool HasFooter {
            get {
                return false;
            } 
        }
 
 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc>
        bool IRepeatInfoUser.HasHeader {
            get {
                return HasHeader; 
            }
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc>
        protected virtual bool HasHeader {
            get { 
                return false;
            } 
        } 

 
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        bool IRepeatInfoUser.HasSeparators { 
            get {
                return HasSeparators; 
            } 
        }
 

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc> 
        protected virtual bool HasSeparators {
            get { 
                return false; 
            }
        } 


        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc>
        int IRepeatInfoUser.RepeatedItemCount { 
            get { 
                return RepeatedItemCount;
            } 
        }


        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual int RepeatedItemCount { 
            get {
#if ORCAS 
                // If the whole control is contained on a single page,
                // the count is the number of items.
                if (VirtualStartPage == VirtualEndPage || _itemCount == -1) {
                    return Items.Count; 
                }
                return _itemCount; 
#else 
                return (Items != null) ? Items.Count : 0;
#endif 
            }
        }

 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc> 
        Style IRepeatInfoUser.GetItemStyle(ListItemType itemType, int repeatIndex) {
            return GetItemStyle(itemType, repeatIndex); 
        }


        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual Style GetItemStyle(ListItemType itemType, int repeatIndex) { 
            return null;
        } 


        /// <internalonly/>
        /// <devdoc> 
        /// Called by the RepeatInfo helper to render each item
        /// </devdoc> 
        void IRepeatInfoUser.RenderItem(ListItemType itemType, int repeatIndex, RepeatInfo repeatInfo, HtmlTextWriter writer) { 
            RenderItem(itemType, repeatIndex, repeatInfo, writer);
        } 


        /// <internalonly/>
        /// <devdoc> 
        /// Called by the RepeatInfo helper to render each item
        /// </devdoc> 
        protected virtual void RenderItem(ListItemType itemType, int repeatIndex, RepeatInfo repeatInfo, HtmlTextWriter writer) { 
            if (repeatIndex == 0) {
                _cachedIsEnabled = IsEnabled; 
                _cachedRegisterEnabled = (Page != null) && (SaveSelectedIndicesViewState == false);
            }

            RadioButton controlToRepeat = ControlToRepeat; 

            // Apply properties of the list items 
            int repeatIndexOffset = repeatIndex + _offset; 

            ListItem item = Items[repeatIndexOffset]; 

            // VSWhidbey 153920 Render expando attributes.
            controlToRepeat.Attributes.Clear();
            if (item.HasAttributes) { 
                foreach (string key in item.Attributes.Keys) {
                    controlToRepeat.Attributes[key] = item.Attributes[key]; 
                } 
            }
 
            controlToRepeat.ID = repeatIndexOffset.ToString(NumberFormatInfo.InvariantInfo);
            controlToRepeat.Text = item.Text;

            controlToRepeat.Attributes["value"] = item.Value; 
            controlToRepeat.Checked = item.Selected;
            controlToRepeat.Enabled = _cachedIsEnabled && item.Enabled; 
            controlToRepeat.TextAlign = TextAlign; 
            controlToRepeat.RenderControl(writer);
 
            if (controlToRepeat.Enabled && _cachedRegisterEnabled && Page != null) {
                // Store a client-side array of enabled control, so we can re-enable them on
                // postback (in case they are disabled client-side)
                // Postback is needed when SelectedIndices is not saved in view state 
                Page.RegisterEnabledControl(controlToRepeat);
            } 
        } 
#if ORCAS
 
        /// <internalonly/>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden) 
        ]
        public virtual int MaximumWeight { 
            get { 
                if (Page != null && Page.Form != null) {
                    return Page.Form.MaximumWeight; 
                }
                return 0;
            }
        } 
#endif
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="RadioButtonList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.Web; 
    using System.Web.UI;
    using System.Security.Permissions; 
 

    /// <devdoc> 
    ///    <para>Generates a single-selection radio button group and
    ///       defines its properties.</para>
    /// </devdoc>
    [ 
    ValidationProperty("SelectedItem"),
    SupportsEventValidation, 
    ] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class RadioButtonList : ListControl, IRepeatInfoUser, INamingContainer, IPostBackDataHandler
#if ORCAS
        , IItemPaginationInfo, IPaginationContainer
#endif 
    {
 
        RadioButton _controlToRepeat; 
        private bool _cachedIsEnabled;
        private bool _cachedRegisterEnabled; 
        private int _offset;
#if ORCAS
        private int _itemCount;
#endif 

 
        public RadioButtonList() { 
            _offset = 0;
#if ORCAS 
            _itemCount = -1;
#endif
        }
 

        /// <devdoc> 
        ///  CellPadding property. 
        ///  The padding between each item.
        /// </devdoc> 
        [
        WebCategory("Layout"),
        DefaultValue(-1),
        WebSysDescription(SR.RadioButtonList_CellPadding) 
        ]
        public virtual int CellPadding { 
            get { 
                if (ControlStyleCreated == false) {
                    return -1; 
                }
                return ((TableStyle)ControlStyle).CellPadding;
            }
            set { 
                ((TableStyle)ControlStyle).CellPadding = value;
            } 
        } 

 
        /// <devdoc>
        ///  CellSpacing property.
        ///  The spacing between each item.
        /// </devdoc> 
        [
        WebCategory("Layout"), 
        DefaultValue(-1), 
        WebSysDescription(SR.RadioButtonList_CellSpacing)
        ] 
        public virtual int CellSpacing {
            get {
                if (ControlStyleCreated == false) {
                    return -1; 
                }
                return ((TableStyle)ControlStyle).CellSpacing; 
            } 
            set {
                ((TableStyle)ControlStyle).CellSpacing = value; 
            }
        }

        private RadioButton ControlToRepeat { 
            get {
                if (_controlToRepeat != null) 
                    return _controlToRepeat; 

                _controlToRepeat = new RadioButton(); 
                _controlToRepeat.EnableViewState = false;
                Controls.Add(_controlToRepeat);

                // A note is that we don't need to set the GroupName on the radio 
                // button as the radio button would simply use its naming container
                // (which would be this control) as its name attribute for postback. 
 
                // Apply properties that are the same for each radio button
                _controlToRepeat.AutoPostBack = AutoPostBack; 
                _controlToRepeat.CausesValidation = CausesValidation;
                _controlToRepeat.ValidationGroup = ValidationGroup;

                return _controlToRepeat; 
            }
        } 
 

        /// <devdoc> 
        ///    Indicates the column count of radio buttons
        ///    within the group.
        /// </devdoc>
        [ 
        WebCategory("Layout"),
        DefaultValue(0), 
        WebSysDescription(SR.RadioButtonList_RepeatColumns) 
        ]
        public virtual int RepeatColumns { 
            get {
                object o = ViewState["RepeatColumns"];
                return((o == null) ? 0 : (int)o);
            } 
            set {
                if (value < 0) { 
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["RepeatColumns"] = value; 
            }
        }

 
        /// <devdoc>
        ///    <para>Gets or sets the direction of flow of 
        ///       the radio buttons within the group.</para> 
        /// </devdoc>
        [ 
        WebCategory("Layout"),
        DefaultValue(RepeatDirection.Vertical),
        WebSysDescription(SR.Item_RepeatDirection)
        ] 
        public virtual RepeatDirection RepeatDirection {
            get { 
                object o = ViewState["RepeatDirection"]; 
                return((o == null) ? RepeatDirection.Vertical : (RepeatDirection)o);
            } 
            set {
                if (value < RepeatDirection.Horizontal || value > RepeatDirection.Vertical) {
                    throw new ArgumentOutOfRangeException("value");
                } 
                ViewState["RepeatDirection"] = value;
            } 
        } 

 
        /// <devdoc>
        ///    <para>Indicates the layout of radio buttons within the
        ///       group.</para>
        /// </devdoc> 
        [
        WebCategory("Layout"), 
        DefaultValue(RepeatLayout.Table), 
        WebSysDescription(SR.WebControl_RepeatLayout)
        ] 
        public virtual RepeatLayout RepeatLayout {
            get {
                object o = ViewState["RepeatLayout"];
                return((o == null) ? RepeatLayout.Table : (RepeatLayout)o); 
            }
            set { 
                if (value < RepeatLayout.Table || value > RepeatLayout.Flow) { 
                    throw new ArgumentOutOfRangeException("value");
                } 
                ViewState["RepeatLayout"] = value;
            }
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Indicates the label text alignment for the radio buttons within the group.</para>
        /// </devdoc> 
        [
        WebCategory("Appearance"),
        DefaultValue(TextAlign.Right),
        WebSysDescription(SR.WebControl_TextAlign) 
        ]
        public virtual TextAlign TextAlign { 
            get { 
                object align = ViewState["TextAlign"];
                return((align == null) ? TextAlign.Right : (TextAlign)align); 
            }
            set {
                if (value < TextAlign.Left || value > TextAlign.Right) {
                    throw new ArgumentOutOfRangeException("value"); 
                }
                ViewState["TextAlign"] = value; 
            } 
        }
 

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc> 
        protected override Style CreateControlStyle() {
            return new TableStyle(ViewState); 
        } 

 
        /// <internalonly/>
        /// <devdoc>
        /// <para>Catches auto postback from a <see cref='System.Web.UI.WebControls.RadioButton'/> in the list.</para>
        /// </devdoc> 
        protected override Control FindControl(string id, int pathOffset) {
            return this; 
        } 
#if ORCAS
        #region IItemPaginationInfo implementation 

        int IItemPaginationInfo.FirstVisibleItemIndex {
            get {
                return this.FirstVisibleItemIndex; 
            }
        } 
 

        protected int FirstVisibleItemIndex { 
            get {
                return _offset;
            }
        } 

 
        /// <devdoc> 
        ///    <para>
        ///       The number of items held by the container for pagination calculations. 
        ///    </para>
        /// </devdoc>
        int IItemPaginationInfo.ItemCount {
            get { 
                return ItemCount;
            } 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       The number of items held by the container for pagination calculations.
        ///    </para> 
        /// </devdoc>
        protected virtual int ItemCount { 
            get { 
                EnsureDataBound();
                return Items.Count; 
            }
        }

 
        /// <devdoc>
        ///    <para> 
        ///       The weight for pagination calculations for each item of the container. 
        ///       Returns a constant value of 100 for RadioButtonList.
        ///    </para> 
        /// </devdoc>
        int IItemPaginationInfo.ItemWeight {
            get {
                return ItemWeight; 
            }
        } 
 

        /// <devdoc> 
        ///    <para>
        ///       The weight for pagination calculations for each item of the container.
        ///       Returns a constant value of 100 for RadioButtonList.
        ///    </para> 
        /// </devdoc>
        protected virtual int ItemWeight { 
            get { 
                return 100;
            } 
        }


        int IItemPaginationInfo.VisibleItemCount { 
            get {
                return this.VisibleItemCount; 
            } 
        }
 

        protected int VisibleItemCount {
            get {
                if (_itemCount >= 0) { 
                    return _itemCount;
                } 
                else { 
                    return ItemCount;
                } 
            }
        }

 
        /// <devdoc>
        ///    <para> 
        ///       Indicates which child items of the container are visible on the current page. 
        ///    </para>
        /// </devdoc> 
        void IItemPaginationInfo.SetVisibleItems(int firstItem, int itemCount) {
            SetVisibleItems(firstItem, itemCount);
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Indicates which child items of the container are visible on the current page.
        ///    </para> 
        /// </devdoc>
        protected virtual void SetVisibleItems(int firstItem, int itemCount) {
            _offset = firstItem;
            _itemCount = itemCount; 
        }
        #endregion 
#endif 

 
        /// <internalonly/>
        /// <devdoc>
        /// <para>Loads the posted content of the list control if it is different from the last
        /// posting.</para> 
        /// </devdoc>
        bool IPostBackDataHandler.LoadPostData(String postDataKey, NameValueCollection postCollection) { 
            return LoadPostData(postDataKey, postCollection); 
        }
 

        /// <internalonly/>
        /// <devdoc>
        /// <para>Loads the posted content of the list control if it is different from the last 
        /// posting.</para>
        /// </devdoc> 
        protected virtual bool LoadPostData(String postDataKey, NameValueCollection postCollection) { 

            // When a RadioButtonList is disabled, then there is no postback data for it. 
            // Since RadioButtonList doesn't call RegisterRequiresPostBack, this method will
            // never be called, so we don't need to worry about ignoring empty postback data.

            string post = postCollection[postDataKey]; 
            int currentSelectedIndex = SelectedIndex;
 
            EnsureDataBound(); 
            int n = Items.Count;
            for (int i=0; i < n; i++) { 
                if (post == Items[i].Value && Items[i].Enabled) {
                    ValidateEvent(postDataKey, post);

                    if (i != currentSelectedIndex) { 
                        SetPostDataSelection(i);
                        return true; 
                    } 
                    return false;
                } 
            }

            return false;
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        /// <para>Invokes the OnSelectedIndexChanged 
        /// method whenever posted data for the <see cref='System.Web.UI.WebControls.RadioButtonList'/>
        /// control has changed.</para>
        /// </devdoc>
        void IPostBackDataHandler.RaisePostDataChangedEvent() { 
            RaisePostDataChangedEvent();
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// <para>Invokes the OnSelectedIndexChanged
        /// method whenever posted data for the <see cref='System.Web.UI.WebControls.RadioButtonList'/>
        /// control has changed.</para> 
        /// </devdoc>
        protected virtual void RaisePostDataChangedEvent() { 
            if (AutoPostBack && Page != null && !Page.IsPostBackEventControlRegistered) { 
                // VSWhidbey 204824
                Page.AutoPostBackControl = this; 

                if (CausesValidation) {
                    Page.Validate(ValidationGroup);
                } 
            }
            OnSelectedIndexChanged(EventArgs.Empty); 
        } 

        protected internal override void Render(HtmlTextWriter writer) { 
#if ORCAS
            if (VisibleItemCount == 0) {
                return;
            } 
#else
            if (Items.Count == 0 && !EnableLegacyRendering) { 
                return; 
            }
#endif 

            RepeatInfo repeatInfo = new RepeatInfo();
            Style style = (ControlStyleCreated ? ControlStyle : null);
            short tabIndex = TabIndex; 
            bool undirtyTabIndex = false;
 
            // TabIndex here is special... it needs to be applied to the individual 
            // radiobuttons and not the outer control itself
 
            // cache away the TabIndex property state
            ControlToRepeat.TabIndex = tabIndex;

            if (tabIndex != 0) { 
                if (ViewState.IsItemDirty("TabIndex") == false) {
                    undirtyTabIndex = true; 
                } 
                TabIndex = 0;
            } 

            repeatInfo.RepeatColumns = RepeatColumns;
            repeatInfo.RepeatDirection = RepeatDirection;
 
            // If the device does not support tables, use the flow layout to render
            if (!DesignMode && !Context.Request.Browser.Tables) { 
                repeatInfo.RepeatLayout = RepeatLayout.Flow; 
            }
            else { 
                repeatInfo.RepeatLayout = RepeatLayout;
            }

            if (repeatInfo.RepeatLayout == RepeatLayout.Flow) { 
                repeatInfo.EnableLegacyRendering = EnableLegacyRendering;
            } 
 
            repeatInfo.RenderRepeater(writer, (IRepeatInfoUser)this, style, this);
 
            if (Page != null) {
                Page.ClientScript.RegisterForEventValidation(UniqueID);
            }
 
            // restore the state of the TabIndex property
            if (tabIndex != 0) { 
                TabIndex = tabIndex; 
            }
            if (undirtyTabIndex) { 
                ViewState.SetItemDirty("TabIndex", false);
            }
        }
 

        /// <internalonly/> 
        /// <devdoc> 
        /// </devdoc>
        bool IRepeatInfoUser.HasFooter { 
            get {
                return HasFooter;
            }
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual bool HasFooter {
            get {
                return false;
            } 
        }
 
 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc>
        bool IRepeatInfoUser.HasHeader {
            get {
                return HasHeader; 
            }
        } 
 

        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc>
        protected virtual bool HasHeader {
            get { 
                return false;
            } 
        } 

 
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        bool IRepeatInfoUser.HasSeparators { 
            get {
                return HasSeparators; 
            } 
        }
 

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc> 
        protected virtual bool HasSeparators {
            get { 
                return false; 
            }
        } 


        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc>
        int IRepeatInfoUser.RepeatedItemCount { 
            get { 
                return RepeatedItemCount;
            } 
        }


        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual int RepeatedItemCount { 
            get {
#if ORCAS 
                // If the whole control is contained on a single page,
                // the count is the number of items.
                if (VirtualStartPage == VirtualEndPage || _itemCount == -1) {
                    return Items.Count; 
                }
                return _itemCount; 
#else 
                return (Items != null) ? Items.Count : 0;
#endif 
            }
        }

 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc> 
        Style IRepeatInfoUser.GetItemStyle(ListItemType itemType, int repeatIndex) {
            return GetItemStyle(itemType, repeatIndex); 
        }


        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual Style GetItemStyle(ListItemType itemType, int repeatIndex) { 
            return null;
        } 


        /// <internalonly/>
        /// <devdoc> 
        /// Called by the RepeatInfo helper to render each item
        /// </devdoc> 
        void IRepeatInfoUser.RenderItem(ListItemType itemType, int repeatIndex, RepeatInfo repeatInfo, HtmlTextWriter writer) { 
            RenderItem(itemType, repeatIndex, repeatInfo, writer);
        } 


        /// <internalonly/>
        /// <devdoc> 
        /// Called by the RepeatInfo helper to render each item
        /// </devdoc> 
        protected virtual void RenderItem(ListItemType itemType, int repeatIndex, RepeatInfo repeatInfo, HtmlTextWriter writer) { 
            if (repeatIndex == 0) {
                _cachedIsEnabled = IsEnabled; 
                _cachedRegisterEnabled = (Page != null) && (SaveSelectedIndicesViewState == false);
            }

            RadioButton controlToRepeat = ControlToRepeat; 

            // Apply properties of the list items 
            int repeatIndexOffset = repeatIndex + _offset; 

            ListItem item = Items[repeatIndexOffset]; 

            // VSWhidbey 153920 Render expando attributes.
            controlToRepeat.Attributes.Clear();
            if (item.HasAttributes) { 
                foreach (string key in item.Attributes.Keys) {
                    controlToRepeat.Attributes[key] = item.Attributes[key]; 
                } 
            }
 
            controlToRepeat.ID = repeatIndexOffset.ToString(NumberFormatInfo.InvariantInfo);
            controlToRepeat.Text = item.Text;

            controlToRepeat.Attributes["value"] = item.Value; 
            controlToRepeat.Checked = item.Selected;
            controlToRepeat.Enabled = _cachedIsEnabled && item.Enabled; 
            controlToRepeat.TextAlign = TextAlign; 
            controlToRepeat.RenderControl(writer);
 
            if (controlToRepeat.Enabled && _cachedRegisterEnabled && Page != null) {
                // Store a client-side array of enabled control, so we can re-enable them on
                // postback (in case they are disabled client-side)
                // Postback is needed when SelectedIndices is not saved in view state 
                Page.RegisterEnabledControl(controlToRepeat);
            } 
        } 
#if ORCAS
 
        /// <internalonly/>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden) 
        ]
        public virtual int MaximumWeight { 
            get { 
                if (Page != null && Page.Form != null) {
                    return Page.Form.MaximumWeight; 
                }
                return 0;
            }
        } 
#endif
    } 
} 
