//------------------------------------------------------------------------------ 
// <copyright file="ContentDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Diagnostics;
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Drawing; 
    using System.Drawing.Imaging; 
    using System.Globalization;
    using System.IO; 
    using System.Text;
    using System.Web.UI.WebControls;
    using System.Web.UI.Design;
 
    /// <include file='doc\ContentDesigner.uex' path='docs/doc[@for="ContentDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.Content'/>
    ///       web control. 
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ContentDesigner : ControlDesigner { 
        private const string _designtimeHTML =
        @"<table cellspacing=0 cellpadding=0 style=""border:1px solid black; width:100%; height:200px""> 
            <tr> 
              <td style=""width:100%; height:25px; font-family:Tahoma; font-size:{2}pt; color:{3}; background-color:{4}; padding:5px; border-bottom:1px solid black;"">
                &nbsp;{0} 
              </td>
            </tr>
            <tr>
              <td style=""width:100%; height:175px; vertical-align:top;"" {1}=""0""> 
              </td>
            </tr> 
          </table>"; 

        private string _content; 
        private ContentDefinition _contentDefinition;
        private IContentResolutionService _contentResolutionService;

        // Only the following properties are displayed during design time 
        private const string _idProperty = "ID";
        private const string _contentPlaceHolderIDProperty = "ContentPlaceHolderID"; 
 
        /// <include file='doc\ContentDesigner.uex' path='docs/doc[@for="ContentDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new ContentDesignerActionList(this)); 

                return actionLists; 
            } 
        }
 
        public override bool AllowResize {
            get {
                return true;
            } 
        }
 
        private IContentResolutionService ContentResolutionService { 
            get {
                if (_contentResolutionService == null) { 
                    _contentResolutionService = (IContentResolutionService)GetService(typeof(IContentResolutionService));
                }
                return _contentResolutionService;
            } 
        }
 
        // Clears the region content 
        private void ClearRegion() {
            if (ContentResolutionService != null && GetContentDefinition() != null) { 
                ContentResolutionService.SetContentDesignerState(GetContentDefinition().ContentPlaceHolderID, ContentDesignerState.ShowDefaultContent);
            }
        }
 
        // sets the region content to a blank content
        private void CreateBlankContent() { 
            if (ContentResolutionService != null && GetContentDefinition() != null) { 
                ContentResolutionService.SetContentDesignerState(GetContentDefinition().ContentPlaceHolderID, ContentDesignerState.ShowUserContent);
            } 
        }

        /// <internalonly/>
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            // Create the single editable region
            EditableDesignerRegion contentRegion = new EditableDesignerRegion(this, "Content"); 
            regions.Add(contentRegion); 

            Font defaultFont = SystemFonts.CaptionFont; 
            Color foreColor = SystemColors.ControlText;
            Color backColor = SystemColors.Control;

            string caption = Component.GetType().Name + " - " + Component.Site.Name; 
            return String.Format(CultureInfo.InvariantCulture, _designtimeHTML, caption, DesignerRegion.DesignerRegionAttributeName,
                defaultFont.SizeInPoints, ColorTranslator.ToHtml(foreColor), ColorTranslator.ToHtml(backColor)); 
        } 

        public override string GetPersistenceContent() { 
            return _content;
        }

        /// <devdoc> 
        /// </devdoc>
        private ContentDefinition GetContentDefinition() { 
            if (_contentDefinition == null) { 
                try {
                    ContentDefinition cntDef = (ContentDefinition)ContentResolutionService.ContentDefinitions[((Content)Component).ContentPlaceHolderID]; 
                    _contentDefinition = new ContentDefinition(cntDef.ContentPlaceHolderID,cntDef.DefaultContent,cntDef.DefaultDesignTimeHtml);
                }
                catch {
                } 
            }
            return _contentDefinition; 
        } 

        /// <internalonly/> 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            if (_content == null) {
                _content = Tag.GetContent();
            } 
            return _content != null ? _content : string.Empty;
        } 
 
        /// Clear all events
        /// <include file='doc\ContentDesigner.uex' path='docs/doc[@for="ContentDesigner.PreFilterEvents"]/*' /> 
        protected override void PreFilterEvents (IDictionary events) {
            events.Clear ();
        }
 
        /// <include file='doc\ContentDesigner.uex' path='docs/doc[@for="ContentDesigner.PostFilterProperties"]/*' />
        /// <devdoc> 
        /// Filters properties on control that are not applicable for a design time 
        /// </devdoc>
        protected override void PostFilterProperties (IDictionary properties) { 
            base.PostFilterProperties(properties);

            // Get the value of the ID and ContentPlaceHolderID properties
            PropertyDescriptor controlIDProp = 
                (PropertyDescriptor)properties[_idProperty];
            Debug.Assert(controlIDProp != null); 
 
            PropertyDescriptor contentPlaceHolderIDProp =
                (PropertyDescriptor)properties[_contentPlaceHolderIDProperty]; 
            Debug.Assert(contentPlaceHolderIDProp != null);

            // clear all properties other than ID and ContentName
            properties.Clear(); 

            ContentDesignerState state = ContentDesignerState.ShowDefaultContent; 
            ContentDefinition contentDefinition = GetContentDefinition(); 
            if (ContentResolutionService != null && contentDefinition != null) {
                state = ContentResolutionService.GetContentDesignerState(contentDefinition.ContentPlaceHolderID); 
            }

            controlIDProp = TypeDescriptor.CreateProperty(
                controlIDProp.ComponentType, 
                controlIDProp,
                new Attribute[] { state == ContentDesignerState.ShowDefaultContent ? 
                    ReadOnlyAttribute.Yes : ReadOnlyAttribute.No 
                });
 
            properties.Add(_idProperty, controlIDProp);

            contentPlaceHolderIDProp = TypeDescriptor.CreateProperty(
                contentPlaceHolderIDProp.ComponentType, 
                contentPlaceHolderIDProp,
                new Attribute[] { 
                    ReadOnlyAttribute.Yes 
                });
            properties.Add(_contentPlaceHolderIDProperty, contentPlaceHolderIDProp); 
        }

        /// <internalonly/>
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            if (String.Compare(_content,content,StringComparison.Ordinal) != 0) {
                _content = content; 
                Tag.SetDirty(true); 
            }
        } 

        private class ContentDesignerActionList : DesignerActionList {
            private ContentDesigner _parent;
 
            public ContentDesignerActionList(ContentDesigner parent) : base(parent.Component) {
                _parent = parent; 
            } 

            public override bool AutoShow { 
                get {
                    return true;
                }
                set { 
                }
            } 
 
            public void ClearRegion() {
                _parent.ClearRegion(); 
            }

            public void CreateBlankContent() {
                _parent.CreateBlankContent(); 
            }
 
            public override DesignerActionItemCollection GetSortedActionItems() { 
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                ContentDesignerState state = ContentDesignerState.ShowDefaultContent; 

                if (_parent.ContentResolutionService != null && _parent.GetContentDefinition() != null) {
                    state = _parent.ContentResolutionService.GetContentDesignerState(_parent.GetContentDefinition().ContentPlaceHolderID);
                } 
                if (ContentDesignerState.ShowDefaultContent == state) {
                    items.Add(new DesignerActionMethodItem(this, "CreateBlankContent", SR.GetString(SR.Content_CreateBlankContent), String.Empty, String.Empty, true)); 
                } 
                else if (ContentDesignerState.ShowUserContent == state) {
                    items.Add(new DesignerActionMethodItem(this, "ClearRegion", SR.GetString(SR.Content_ClearRegion), String.Empty, String.Empty, true)); 
                }

                return items;
            } 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ContentDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Diagnostics;
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Drawing; 
    using System.Drawing.Imaging; 
    using System.Globalization;
    using System.IO; 
    using System.Text;
    using System.Web.UI.WebControls;
    using System.Web.UI.Design;
 
    /// <include file='doc\ContentDesigner.uex' path='docs/doc[@for="ContentDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.Content'/>
    ///       web control. 
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ContentDesigner : ControlDesigner { 
        private const string _designtimeHTML =
        @"<table cellspacing=0 cellpadding=0 style=""border:1px solid black; width:100%; height:200px""> 
            <tr> 
              <td style=""width:100%; height:25px; font-family:Tahoma; font-size:{2}pt; color:{3}; background-color:{4}; padding:5px; border-bottom:1px solid black;"">
                &nbsp;{0} 
              </td>
            </tr>
            <tr>
              <td style=""width:100%; height:175px; vertical-align:top;"" {1}=""0""> 
              </td>
            </tr> 
          </table>"; 

        private string _content; 
        private ContentDefinition _contentDefinition;
        private IContentResolutionService _contentResolutionService;

        // Only the following properties are displayed during design time 
        private const string _idProperty = "ID";
        private const string _contentPlaceHolderIDProperty = "ContentPlaceHolderID"; 
 
        /// <include file='doc\ContentDesigner.uex' path='docs/doc[@for="ContentDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new ContentDesignerActionList(this)); 

                return actionLists; 
            } 
        }
 
        public override bool AllowResize {
            get {
                return true;
            } 
        }
 
        private IContentResolutionService ContentResolutionService { 
            get {
                if (_contentResolutionService == null) { 
                    _contentResolutionService = (IContentResolutionService)GetService(typeof(IContentResolutionService));
                }
                return _contentResolutionService;
            } 
        }
 
        // Clears the region content 
        private void ClearRegion() {
            if (ContentResolutionService != null && GetContentDefinition() != null) { 
                ContentResolutionService.SetContentDesignerState(GetContentDefinition().ContentPlaceHolderID, ContentDesignerState.ShowDefaultContent);
            }
        }
 
        // sets the region content to a blank content
        private void CreateBlankContent() { 
            if (ContentResolutionService != null && GetContentDefinition() != null) { 
                ContentResolutionService.SetContentDesignerState(GetContentDefinition().ContentPlaceHolderID, ContentDesignerState.ShowUserContent);
            } 
        }

        /// <internalonly/>
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            // Create the single editable region
            EditableDesignerRegion contentRegion = new EditableDesignerRegion(this, "Content"); 
            regions.Add(contentRegion); 

            Font defaultFont = SystemFonts.CaptionFont; 
            Color foreColor = SystemColors.ControlText;
            Color backColor = SystemColors.Control;

            string caption = Component.GetType().Name + " - " + Component.Site.Name; 
            return String.Format(CultureInfo.InvariantCulture, _designtimeHTML, caption, DesignerRegion.DesignerRegionAttributeName,
                defaultFont.SizeInPoints, ColorTranslator.ToHtml(foreColor), ColorTranslator.ToHtml(backColor)); 
        } 

        public override string GetPersistenceContent() { 
            return _content;
        }

        /// <devdoc> 
        /// </devdoc>
        private ContentDefinition GetContentDefinition() { 
            if (_contentDefinition == null) { 
                try {
                    ContentDefinition cntDef = (ContentDefinition)ContentResolutionService.ContentDefinitions[((Content)Component).ContentPlaceHolderID]; 
                    _contentDefinition = new ContentDefinition(cntDef.ContentPlaceHolderID,cntDef.DefaultContent,cntDef.DefaultDesignTimeHtml);
                }
                catch {
                } 
            }
            return _contentDefinition; 
        } 

        /// <internalonly/> 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            if (_content == null) {
                _content = Tag.GetContent();
            } 
            return _content != null ? _content : string.Empty;
        } 
 
        /// Clear all events
        /// <include file='doc\ContentDesigner.uex' path='docs/doc[@for="ContentDesigner.PreFilterEvents"]/*' /> 
        protected override void PreFilterEvents (IDictionary events) {
            events.Clear ();
        }
 
        /// <include file='doc\ContentDesigner.uex' path='docs/doc[@for="ContentDesigner.PostFilterProperties"]/*' />
        /// <devdoc> 
        /// Filters properties on control that are not applicable for a design time 
        /// </devdoc>
        protected override void PostFilterProperties (IDictionary properties) { 
            base.PostFilterProperties(properties);

            // Get the value of the ID and ContentPlaceHolderID properties
            PropertyDescriptor controlIDProp = 
                (PropertyDescriptor)properties[_idProperty];
            Debug.Assert(controlIDProp != null); 
 
            PropertyDescriptor contentPlaceHolderIDProp =
                (PropertyDescriptor)properties[_contentPlaceHolderIDProperty]; 
            Debug.Assert(contentPlaceHolderIDProp != null);

            // clear all properties other than ID and ContentName
            properties.Clear(); 

            ContentDesignerState state = ContentDesignerState.ShowDefaultContent; 
            ContentDefinition contentDefinition = GetContentDefinition(); 
            if (ContentResolutionService != null && contentDefinition != null) {
                state = ContentResolutionService.GetContentDesignerState(contentDefinition.ContentPlaceHolderID); 
            }

            controlIDProp = TypeDescriptor.CreateProperty(
                controlIDProp.ComponentType, 
                controlIDProp,
                new Attribute[] { state == ContentDesignerState.ShowDefaultContent ? 
                    ReadOnlyAttribute.Yes : ReadOnlyAttribute.No 
                });
 
            properties.Add(_idProperty, controlIDProp);

            contentPlaceHolderIDProp = TypeDescriptor.CreateProperty(
                contentPlaceHolderIDProp.ComponentType, 
                contentPlaceHolderIDProp,
                new Attribute[] { 
                    ReadOnlyAttribute.Yes 
                });
            properties.Add(_contentPlaceHolderIDProperty, contentPlaceHolderIDProp); 
        }

        /// <internalonly/>
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            if (String.Compare(_content,content,StringComparison.Ordinal) != 0) {
                _content = content; 
                Tag.SetDirty(true); 
            }
        } 

        private class ContentDesignerActionList : DesignerActionList {
            private ContentDesigner _parent;
 
            public ContentDesignerActionList(ContentDesigner parent) : base(parent.Component) {
                _parent = parent; 
            } 

            public override bool AutoShow { 
                get {
                    return true;
                }
                set { 
                }
            } 
 
            public void ClearRegion() {
                _parent.ClearRegion(); 
            }

            public void CreateBlankContent() {
                _parent.CreateBlankContent(); 
            }
 
            public override DesignerActionItemCollection GetSortedActionItems() { 
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                ContentDesignerState state = ContentDesignerState.ShowDefaultContent; 

                if (_parent.ContentResolutionService != null && _parent.GetContentDefinition() != null) {
                    state = _parent.ContentResolutionService.GetContentDesignerState(_parent.GetContentDefinition().ContentPlaceHolderID);
                } 
                if (ContentDesignerState.ShowDefaultContent == state) {
                    items.Add(new DesignerActionMethodItem(this, "CreateBlankContent", SR.GetString(SR.Content_CreateBlankContent), String.Empty, String.Empty, true)); 
                } 
                else if (ContentDesignerState.ShowUserContent == state) {
                    items.Add(new DesignerActionMethodItem(this, "ClearRegion", SR.GetString(SR.Content_ClearRegion), String.Empty, String.Empty, true)); 
                }

                return items;
            } 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
