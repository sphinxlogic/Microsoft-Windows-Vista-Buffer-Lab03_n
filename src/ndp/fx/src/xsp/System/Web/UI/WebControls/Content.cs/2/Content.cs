//------------------------------------------------------------------------------ 
// <copyright file="Content.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Globalization;
    using System.Security.Permissions;
    using System.Web.UI; 
    using System.Web.Util;
 
    // ContentBuilder is a builder for content control but acts like a template builder. 
    // Designertime it will create the <asp:Content> as a control, but a template
    // builder at runtime. ContentBuilder only works if the parent builder is a 
    // MasterPageBuilder, otherwise this builder is simply ignored.
    internal class ContentBuilderInternal: TemplateBuilder {

        private const string _contentPlaceHolderIDPropName = "ContentPlaceHolderID"; 

        private string _contentPlaceHolder; 
        private string _contentPlaceHolderFilter; 

 
        /// <devdoc>
        /// </devdoc>
        public override Type BindingContainerType {
            get { 
                return typeof(Control);
            } 
        } 

        internal string ContentPlaceHolderFilter { 
            get {
                return _contentPlaceHolderFilter;
            }
        } 

        internal string ContentPlaceHolder { 
            get { 
                return _contentPlaceHolder;
            } 
        }

        // To return the content control for designtime support
        public override object BuildObject() { 

            if (InDesigner) 
                return BuildObjectInternal(); 

            return base.BuildObject(); 
        }

        public override void InstantiateIn(Control container) {
            base.InstantiateIn(container); 

            // Set all the children's TemplateControl properties to the owning page, 
            // to prevent them from incorrectly resolving to the Master page (VSWhidbey 602525) 
            HttpContext context = HttpContext.Current;
            if (context != null) { 
                TemplateControl templateControl = context.TemplateControl;

                if (templateControl != null && templateControl.NoCompile) {
                    foreach (Control child in container.Controls) { 
                        child.TemplateControl = templateControl;
                    } 
                } 
            }
        } 

        public override void Init(TemplateParser parser, ControlBuilder parentBuilder,
                                  Type type, string tagName, string ID, IDictionary attribs) {
 
            ParsedAttributeCollection parsedAttributes = ConvertDictionaryToParsedAttributeCollection(attribs);
            foreach (FilteredAttributeDictionary filteredAttributes in parsedAttributes.GetFilteredAttributeDictionaries()) { 
                string filter = filteredAttributes.Filter; 
                foreach (DictionaryEntry entry in filteredAttributes) {
                    if (StringUtil.EqualsIgnoreCase((string)entry.Key, _contentPlaceHolderIDPropName)) { 
                        if (_contentPlaceHolder != null) {
                            throw new HttpException(SR.GetString(SR.Content_only_one_contentPlaceHolderID_allowed));
                        }
 
                        string key = entry.Key.ToString();
                        _contentPlaceHolder = entry.Value.ToString(); 
                        _contentPlaceHolderFilter = filter; 
                    }
                } 
            }

            if (!parser.FInDesigner) {
                if (_contentPlaceHolder == null) 
                    throw new HttpException(SR.GetString(SR.Control_Missing_Attribute, _contentPlaceHolderIDPropName, type.Name));
 
                attribs.Clear(); 
            }
 
            base.Init(parser, parentBuilder, type, tagName, ID, attribs);
        }

        internal override void SetParentBuilder(ControlBuilder parentBuilder) { 
            if (!InDesigner && !(parentBuilder is FileLevelPageControlBuilder)) {
                throw new HttpException(SR.GetString(SR.Content_allowed_in_top_level_only)); 
            } 

            base.SetParentBuilder(parentBuilder); 
        }
    }

    // Factory used to efficiently create builder instances 
    internal class ContentBuilderInternalFactory: IWebObjectFactory {
        object IWebObjectFactory.CreateInstance() { 
            return new ContentBuilderInternal(); 
        }
    } 

    /* This control represents the ITemplate property on the content page that will be applied
       to the MasterPage template property. The ContentPlaceHolderID is never assigned at runtime. */
 
    [
    ControlBuilderAttribute(typeof(ContentBuilderInternal)), 
    ] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [Designer("System.Web.UI.Design.WebControls.ContentDesigner, " + AssemblyRef.SystemDesign)]
    [ToolboxItem(false)]
    public class Content : Control, INonBindingContainer {
        private string _contentPlaceHolderID; 

 
        [ 
        DefaultValue(""),
        IDReferenceProperty(typeof(ContentPlaceHolder)), 
        Themeable(false),
        WebCategory("Behavior"),
        WebSysDescription(SR.Content_ContentPlaceHolderID),
        ] 
        public string ContentPlaceHolderID {
            get { 
                if (_contentPlaceHolderID == null) { 
                    return String.Empty;
                } 
                return _contentPlaceHolderID;
            }
            set {
                if (!DesignMode) 
                    throw new NotSupportedException(SR.GetString(SR.Property_Set_Not_Supported, "ContentPlaceHolderID", this.GetType().ToString()));
 
                _contentPlaceHolderID = value; 
            }
        } 

        #region hide these events in the designer since they will not be invoked.
        [
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ] 
        public new event EventHandler DataBinding { 
            add {
                base.DataBinding += value; 
            }
            remove {
                base.DataBinding -= value;
            } 
        }
 
        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ]
        public new event EventHandler Disposed {
            add {
                base.Disposed += value; 
            }
            remove { 
                base.Disposed -= value; 
            }
        } 

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ]
        public new event EventHandler Init { 
            add { 
                base.Init += value;
            } 
            remove {
                base.Init -= value;
            }
        } 

        [ 
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ] 
        public new event EventHandler Load {
            add {
                base.Load += value;
            } 
            remove {
                base.Load -= value; 
            } 
        }
 
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ] 
        public new event EventHandler PreRender {
            add { 
                base.PreRender += value; 
            }
            remove { 
                base.PreRender -= value;
            }
        }
 
        [
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ]
        public new event EventHandler Unload { 
            add {
                base.Unload += value;
            }
            remove { 
                base.Unload -= value;
            } 
        } 
        #endregion
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="Content.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Globalization;
    using System.Security.Permissions;
    using System.Web.UI; 
    using System.Web.Util;
 
    // ContentBuilder is a builder for content control but acts like a template builder. 
    // Designertime it will create the <asp:Content> as a control, but a template
    // builder at runtime. ContentBuilder only works if the parent builder is a 
    // MasterPageBuilder, otherwise this builder is simply ignored.
    internal class ContentBuilderInternal: TemplateBuilder {

        private const string _contentPlaceHolderIDPropName = "ContentPlaceHolderID"; 

        private string _contentPlaceHolder; 
        private string _contentPlaceHolderFilter; 

 
        /// <devdoc>
        /// </devdoc>
        public override Type BindingContainerType {
            get { 
                return typeof(Control);
            } 
        } 

        internal string ContentPlaceHolderFilter { 
            get {
                return _contentPlaceHolderFilter;
            }
        } 

        internal string ContentPlaceHolder { 
            get { 
                return _contentPlaceHolder;
            } 
        }

        // To return the content control for designtime support
        public override object BuildObject() { 

            if (InDesigner) 
                return BuildObjectInternal(); 

            return base.BuildObject(); 
        }

        public override void InstantiateIn(Control container) {
            base.InstantiateIn(container); 

            // Set all the children's TemplateControl properties to the owning page, 
            // to prevent them from incorrectly resolving to the Master page (VSWhidbey 602525) 
            HttpContext context = HttpContext.Current;
            if (context != null) { 
                TemplateControl templateControl = context.TemplateControl;

                if (templateControl != null && templateControl.NoCompile) {
                    foreach (Control child in container.Controls) { 
                        child.TemplateControl = templateControl;
                    } 
                } 
            }
        } 

        public override void Init(TemplateParser parser, ControlBuilder parentBuilder,
                                  Type type, string tagName, string ID, IDictionary attribs) {
 
            ParsedAttributeCollection parsedAttributes = ConvertDictionaryToParsedAttributeCollection(attribs);
            foreach (FilteredAttributeDictionary filteredAttributes in parsedAttributes.GetFilteredAttributeDictionaries()) { 
                string filter = filteredAttributes.Filter; 
                foreach (DictionaryEntry entry in filteredAttributes) {
                    if (StringUtil.EqualsIgnoreCase((string)entry.Key, _contentPlaceHolderIDPropName)) { 
                        if (_contentPlaceHolder != null) {
                            throw new HttpException(SR.GetString(SR.Content_only_one_contentPlaceHolderID_allowed));
                        }
 
                        string key = entry.Key.ToString();
                        _contentPlaceHolder = entry.Value.ToString(); 
                        _contentPlaceHolderFilter = filter; 
                    }
                } 
            }

            if (!parser.FInDesigner) {
                if (_contentPlaceHolder == null) 
                    throw new HttpException(SR.GetString(SR.Control_Missing_Attribute, _contentPlaceHolderIDPropName, type.Name));
 
                attribs.Clear(); 
            }
 
            base.Init(parser, parentBuilder, type, tagName, ID, attribs);
        }

        internal override void SetParentBuilder(ControlBuilder parentBuilder) { 
            if (!InDesigner && !(parentBuilder is FileLevelPageControlBuilder)) {
                throw new HttpException(SR.GetString(SR.Content_allowed_in_top_level_only)); 
            } 

            base.SetParentBuilder(parentBuilder); 
        }
    }

    // Factory used to efficiently create builder instances 
    internal class ContentBuilderInternalFactory: IWebObjectFactory {
        object IWebObjectFactory.CreateInstance() { 
            return new ContentBuilderInternal(); 
        }
    } 

    /* This control represents the ITemplate property on the content page that will be applied
       to the MasterPage template property. The ContentPlaceHolderID is never assigned at runtime. */
 
    [
    ControlBuilderAttribute(typeof(ContentBuilderInternal)), 
    ] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [Designer("System.Web.UI.Design.WebControls.ContentDesigner, " + AssemblyRef.SystemDesign)]
    [ToolboxItem(false)]
    public class Content : Control, INonBindingContainer {
        private string _contentPlaceHolderID; 

 
        [ 
        DefaultValue(""),
        IDReferenceProperty(typeof(ContentPlaceHolder)), 
        Themeable(false),
        WebCategory("Behavior"),
        WebSysDescription(SR.Content_ContentPlaceHolderID),
        ] 
        public string ContentPlaceHolderID {
            get { 
                if (_contentPlaceHolderID == null) { 
                    return String.Empty;
                } 
                return _contentPlaceHolderID;
            }
            set {
                if (!DesignMode) 
                    throw new NotSupportedException(SR.GetString(SR.Property_Set_Not_Supported, "ContentPlaceHolderID", this.GetType().ToString()));
 
                _contentPlaceHolderID = value; 
            }
        } 

        #region hide these events in the designer since they will not be invoked.
        [
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ] 
        public new event EventHandler DataBinding { 
            add {
                base.DataBinding += value; 
            }
            remove {
                base.DataBinding -= value;
            } 
        }
 
        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ]
        public new event EventHandler Disposed {
            add {
                base.Disposed += value; 
            }
            remove { 
                base.Disposed -= value; 
            }
        } 

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ]
        public new event EventHandler Init { 
            add { 
                base.Init += value;
            } 
            remove {
                base.Init -= value;
            }
        } 

        [ 
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ] 
        public new event EventHandler Load {
            add {
                base.Load += value;
            } 
            remove {
                base.Load -= value; 
            } 
        }
 
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ] 
        public new event EventHandler PreRender {
            add { 
                base.PreRender += value; 
            }
            remove { 
                base.PreRender -= value;
            }
        }
 
        [
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ]
        public new event EventHandler Unload { 
            add {
                base.Unload += value;
            }
            remove { 
                base.Unload -= value;
            } 
        } 
        #endregion
    } 
}
