//------------------------------------------------------------------------------ 
// <copyright file="ContainerControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Globalization;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Imaging;
    using System.IO; 
    using System.Text;
    using System.Web.UI;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\ContainerControlDesigner.uex' path='docs/doc[@for="ContainerControlDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.ContainerControl'/>
    ///       web control. 
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ContainerControlDesigner : ControlDesigner { 

        private const string ContainerControlWithCaptionDesignTimeHtml = 
            @"<table height=""{8}"" width=""{9}"" style=""{0}{2}{10}"" cellpadding=1 cellspacing=0> 
                <tr>
                    <td nowrap align=center valign=middle style=""{1}{2}{3}{4}"">{5}</td> 
                </tr>
                <tr>
                    <td nowrap style=""vertical-align:top;{6}{10}"" {7}=0></td>
                </tr> 
            </table>";
 
        private const string ContainerControlNoCaptionDesignTimeHtml = 
            @"<div height=""{8}"" width=""{9}"" style=""{0}{2}{3}{4}{6}{10}"" {7}=0></div>";
 
        private Style _defaultFrameStyle;

        public ContainerControlDesigner() {
            _defaultFrameStyle = new Style(); 
            _defaultFrameStyle.Font.Name = "Tahoma";
            _defaultFrameStyle.ForeColor = SystemColors.ControlText; 
            _defaultFrameStyle.BackColor = SystemColors.Control; 
        }
 
        public override bool AllowResize {
            get {
                return true;
            } 
        }
 
        internal virtual string DesignTimeHtml { 
            get {
                if (!String.IsNullOrEmpty(FrameCaption)) { 
                    return ContainerControlWithCaptionDesignTimeHtml;
                }
                return ContainerControlNoCaptionDesignTimeHtml;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public virtual string FrameCaption { 
            get {
                return ID;
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        public virtual Style FrameStyle {
            get { 
                return _defaultFrameStyle;
            }
        }
 
        // Non virtual version so we can call this from the constructor of designers that extend this(i.e. MultiView/View)
        internal Style FrameStyleInternal { 
            get { 
                return _defaultFrameStyle;
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        public virtual IDictionary GetDesignTimeCssAttributes() {
            IDictionary styleAttributes = new HybridDictionary(); 
            AddDesignTimeCssAttributes(styleAttributes); 
            return styleAttributes;
        } 

        protected virtual void AddDesignTimeCssAttributes(IDictionary styleAttributes) {
            if (!IsWebControl) {
                return; 
            }
 
            WebControl control = ViewControl as WebControl; 

            Unit width = control.Width; 
            if (!width.IsEmpty && width.Value != 0) {
                styleAttributes["width"] = width.ToString(CultureInfo.InvariantCulture);
            }
 
            Unit height = control.Height;
            if (!height.IsEmpty && height.Value != 0) { 
                styleAttributes["height"] = height.ToString(CultureInfo.InvariantCulture); 
            }
 
            string colorValue;

            colorValue = System.Drawing.ColorTranslator.ToHtml((System.Drawing.Color)control.BackColor);
            if (colorValue.Length > 0) { 
                styleAttributes["background-color"] = colorValue;
            } 
 
            colorValue = System.Drawing.ColorTranslator.ToHtml((System.Drawing.Color)control.ForeColor);
            if (colorValue.Length > 0) { 
                styleAttributes["color"] = colorValue;
            }

            colorValue = System.Drawing.ColorTranslator.ToHtml((System.Drawing.Color)control.BorderColor); 
            if (colorValue.Length > 0) {
                styleAttributes["border-color"] = colorValue; 
            } 

            Unit borderWidth = control.BorderWidth; 
            if (borderWidth.IsEmpty == false && borderWidth.Value != 0) {
                styleAttributes["border-width"] = borderWidth.ToString(CultureInfo.InvariantCulture);
            }
 
            BorderStyle borderStyle = control.BorderStyle;
            if (borderStyle != BorderStyle.NotSet) { 
                styleAttributes["border-style"] = borderStyle; 
            }
            else { 
                if (!borderWidth.IsEmpty && borderWidth.Value != 0) {
                    styleAttributes["border-style"] = BorderStyle.Solid;
                }
            } 

            string fontName = control.Font.Name; 
            if (fontName.Length != 0) { 
                styleAttributes["font-family"] = System.Web.HttpUtility.HtmlEncode(fontName);
            } 

            FontUnit fontSize = control.Font.Size;
            if (fontSize != FontUnit.Empty) {
                styleAttributes["font-size"] = fontSize.ToString(CultureInfo.InvariantCulture); 
            }
 
            bool boolValue = control.Font.Bold; 
            if (boolValue) {
                styleAttributes["font-weight"] = "bold"; 
            }

            boolValue = control.Font.Italic;
            if (boolValue) { 
                styleAttributes["font-style"] = "italic";
            } 
 
            string textDecoration = String.Empty;
            boolValue = control.Font.Underline; 
            if (boolValue) {
                textDecoration += "underline ";
            }
 
            boolValue = control.Font.Strikeout;
            if (boolValue) { 
                textDecoration += "line-through"; 
            }
 
            boolValue = control.Font.Overline;
            if (boolValue) {
                textDecoration += "overline";
            } 

            if (textDecoration.Length > 0) { 
                styleAttributes["text-decoration"] = textDecoration.Trim(); 
            }
        } 

        public override string GetPersistenceContent() {
            // See comment in ControlDesigner for LocalizedInnerContent
            if (LocalizedInnerContent != null) { 
                return LocalizedInnerContent;
            } 
 
            // In general we return null because the control's content was explicitly
            // set from the editable designer region by calling Tag.SetContent(). Returning 
            // null implies that we have no new content to return and that the existing
            // tag content should be preserved.
            return null;
        } 

        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            string content = string.Empty; 
            if (Tag != null) {
                try { 
                    content = Tag.GetContent();
                    if (content == null) {
                        content = string.Empty;
                    } 
                }
                catch (Exception ex) { 
                    Debug.Fail(ex.ToString()); 
                }
            } 
            return content;
        }

        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            if (Tag != null) {
                try { 
                    Tag.SetContent(content); 
                }
                catch (Exception ex) { 
                    Debug.Fail(ex.ToString());
                }
            }
        } 

        private IDictionary ConvertFontInfoToCss(FontInfo font) { 
            // The code in this function is adapted from AddDesignTimeCssAttributes 

            IDictionary styleAttributes = new System.Collections.Specialized.HybridDictionary(); 

            string fontName = font.Name;
            if (fontName.Length != 0) {
                styleAttributes["font-family"] = System.Web.HttpUtility.HtmlEncode(fontName); 
            }
 
            FontUnit fontSize = font.Size; 
            if (fontSize != FontUnit.Empty) {
                styleAttributes["font-size"] = fontSize.ToString(CultureInfo.CurrentCulture); 
            }

            bool boolValue = font.Bold;
            if (boolValue) { 
                styleAttributes["font-weight"] = "bold";
            } 
 
            boolValue = font.Italic;
            if (boolValue) { 
                styleAttributes["font-style"] = "italic";
            }

            string textDecoration = String.Empty; 
            boolValue = font.Underline;
            if (boolValue) { 
                textDecoration += "underline "; 
            }
 
            boolValue = font.Strikeout;
            if (boolValue) {
                textDecoration += "line-through";
            } 

            boolValue = font.Overline; 
            if (boolValue) { 
                textDecoration += "overline";
            } 

            if (textDecoration.Length > 0) {
                styleAttributes["text-decoration"] = textDecoration.Trim();
            } 

            return styleAttributes; 
        } 

        private string CssDictionaryToString(IDictionary dictionary) { 
            string css = string.Empty;
            if (dictionary != null) {
                foreach (DictionaryEntry attr in dictionary) {
                    css += attr.Key + ":" + attr.Value + "; "; 
                }
            } 
            return css; 
        }
 
        private string GenerateDesignTimeHtml() {
            string caption = FrameCaption;

            Unit height = FrameStyle.Height; 
            Unit width = FrameStyle.Width;
 
            string cssClassName = String.Empty; 

            if (IsWebControl) { 
                WebControl control = (WebControl)ViewControl;
                if (height.IsEmpty) {
                    height = control.Height;
                } 
                if (width.IsEmpty) {
                    width = control.Width; 
                } 
                cssClassName = control.CssClass;
            } 

            if (caption == null) {
                caption = string.Empty;
            } 
            else if (caption.IndexOf('\0') > -1) {
                // Ignore null characters (VSWhidbey 129511) 
                // 

 
                caption = caption.Replace("\0", String.Empty);
            }

            string cssStyle = String.Empty; 
            WebControl webcontrol = ViewControl as WebControl;
            if (webcontrol != null) { 
                cssStyle = webcontrol.Style.Value + ";"; 
            }
 
            string borderCss = String.Empty;
            string borderBottomCss = String.Empty;
            if ((!FrameStyle.BorderWidth.IsEmpty && FrameStyle.BorderWidth.Value != 0) ||
                (FrameStyle.BorderStyle != BorderStyle.NotSet) || 
                (!FrameStyle.BorderColor.IsEmpty)) {
                borderCss = String.Format(CultureInfo.InvariantCulture, 
                    "border:{0} {1} {2}; ", 
                    FrameStyle.BorderWidth,
                    FrameStyle.BorderStyle, 
                    ColorTranslator.ToHtml(FrameStyle.BorderColor));
                borderBottomCss = String.Format(CultureInfo.InvariantCulture,
                    "border-bottom:{0} {1} {2}; ",
                    FrameStyle.BorderWidth, 
                    FrameStyle.BorderStyle,
                    ColorTranslator.ToHtml(FrameStyle.BorderColor)); 
            } 

            string foreColorCss = String.Empty; 
            if (!FrameStyle.ForeColor.IsEmpty) {
                foreColorCss = String.Format(CultureInfo.InvariantCulture,
                    "color:{0}; ",
                    ColorTranslator.ToHtml(FrameStyle.ForeColor)); 
            }
 
            string backColorCss = String.Empty; 
            if (!FrameStyle.BackColor.IsEmpty) {
                backColorCss = String.Format(CultureInfo.InvariantCulture, 
                    "background-color:{0}; ",
                    ColorTranslator.ToHtml(FrameStyle.BackColor));
            }
 
            return String.Format(CultureInfo.InvariantCulture,
                DesignTimeHtml, 
                borderCss, // 0 (has trailing semicolon) 
                borderBottomCss, // 1 (has trailing semicolon)
                foreColorCss, // 2 (has trailing semicolon) 
                backColorCss, // 3 (has trailing semicolon)
                CssDictionaryToString(ConvertFontInfoToCss(FrameStyle.Font)), // 4 (has trailing semicolon)
                caption, // 5
                CssDictionaryToString(GetDesignTimeCssAttributes()), // 6 (has trailing semicolon) 
                DesignerRegion.DesignerRegionAttributeName, // 7
                height, // 8 
                width, // 9 
                cssStyle, // 10 (has trailing semicolon)
                cssClassName); // 11 
        }

        /// <include file='doc\ContainerControlDesigner.uex' path='docs/doc[@for="ContainerControlDesigner.GetDesignTimeHtml"]/*' />
        /// <internalonly/> 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            // We must clear the child controls because the tool will parent the controls 
            // created from the editable region to the component.  Venus was already doing this 
            // for backwards compat with v1, this code was added so FrontPage would not have
            // to do the same. 
            Control control = (Control)Component;
            control.Controls.Clear();

            // Create the single editable region 
            EditableDesignerRegion region = new EditableDesignerRegion(this, "Content");
            region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark); 
            region.Properties[typeof(Control)] = control; 
            region.EnsureSize = true;
            regions.Add(region); 

            return GenerateDesignTimeHtml();
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ContainerControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Globalization;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Imaging;
    using System.IO; 
    using System.Text;
    using System.Web.UI;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\ContainerControlDesigner.uex' path='docs/doc[@for="ContainerControlDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.ContainerControl'/>
    ///       web control. 
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ContainerControlDesigner : ControlDesigner { 

        private const string ContainerControlWithCaptionDesignTimeHtml = 
            @"<table height=""{8}"" width=""{9}"" style=""{0}{2}{10}"" cellpadding=1 cellspacing=0> 
                <tr>
                    <td nowrap align=center valign=middle style=""{1}{2}{3}{4}"">{5}</td> 
                </tr>
                <tr>
                    <td nowrap style=""vertical-align:top;{6}{10}"" {7}=0></td>
                </tr> 
            </table>";
 
        private const string ContainerControlNoCaptionDesignTimeHtml = 
            @"<div height=""{8}"" width=""{9}"" style=""{0}{2}{3}{4}{6}{10}"" {7}=0></div>";
 
        private Style _defaultFrameStyle;

        public ContainerControlDesigner() {
            _defaultFrameStyle = new Style(); 
            _defaultFrameStyle.Font.Name = "Tahoma";
            _defaultFrameStyle.ForeColor = SystemColors.ControlText; 
            _defaultFrameStyle.BackColor = SystemColors.Control; 
        }
 
        public override bool AllowResize {
            get {
                return true;
            } 
        }
 
        internal virtual string DesignTimeHtml { 
            get {
                if (!String.IsNullOrEmpty(FrameCaption)) { 
                    return ContainerControlWithCaptionDesignTimeHtml;
                }
                return ContainerControlNoCaptionDesignTimeHtml;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public virtual string FrameCaption { 
            get {
                return ID;
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        public virtual Style FrameStyle {
            get { 
                return _defaultFrameStyle;
            }
        }
 
        // Non virtual version so we can call this from the constructor of designers that extend this(i.e. MultiView/View)
        internal Style FrameStyleInternal { 
            get { 
                return _defaultFrameStyle;
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        public virtual IDictionary GetDesignTimeCssAttributes() {
            IDictionary styleAttributes = new HybridDictionary(); 
            AddDesignTimeCssAttributes(styleAttributes); 
            return styleAttributes;
        } 

        protected virtual void AddDesignTimeCssAttributes(IDictionary styleAttributes) {
            if (!IsWebControl) {
                return; 
            }
 
            WebControl control = ViewControl as WebControl; 

            Unit width = control.Width; 
            if (!width.IsEmpty && width.Value != 0) {
                styleAttributes["width"] = width.ToString(CultureInfo.InvariantCulture);
            }
 
            Unit height = control.Height;
            if (!height.IsEmpty && height.Value != 0) { 
                styleAttributes["height"] = height.ToString(CultureInfo.InvariantCulture); 
            }
 
            string colorValue;

            colorValue = System.Drawing.ColorTranslator.ToHtml((System.Drawing.Color)control.BackColor);
            if (colorValue.Length > 0) { 
                styleAttributes["background-color"] = colorValue;
            } 
 
            colorValue = System.Drawing.ColorTranslator.ToHtml((System.Drawing.Color)control.ForeColor);
            if (colorValue.Length > 0) { 
                styleAttributes["color"] = colorValue;
            }

            colorValue = System.Drawing.ColorTranslator.ToHtml((System.Drawing.Color)control.BorderColor); 
            if (colorValue.Length > 0) {
                styleAttributes["border-color"] = colorValue; 
            } 

            Unit borderWidth = control.BorderWidth; 
            if (borderWidth.IsEmpty == false && borderWidth.Value != 0) {
                styleAttributes["border-width"] = borderWidth.ToString(CultureInfo.InvariantCulture);
            }
 
            BorderStyle borderStyle = control.BorderStyle;
            if (borderStyle != BorderStyle.NotSet) { 
                styleAttributes["border-style"] = borderStyle; 
            }
            else { 
                if (!borderWidth.IsEmpty && borderWidth.Value != 0) {
                    styleAttributes["border-style"] = BorderStyle.Solid;
                }
            } 

            string fontName = control.Font.Name; 
            if (fontName.Length != 0) { 
                styleAttributes["font-family"] = System.Web.HttpUtility.HtmlEncode(fontName);
            } 

            FontUnit fontSize = control.Font.Size;
            if (fontSize != FontUnit.Empty) {
                styleAttributes["font-size"] = fontSize.ToString(CultureInfo.InvariantCulture); 
            }
 
            bool boolValue = control.Font.Bold; 
            if (boolValue) {
                styleAttributes["font-weight"] = "bold"; 
            }

            boolValue = control.Font.Italic;
            if (boolValue) { 
                styleAttributes["font-style"] = "italic";
            } 
 
            string textDecoration = String.Empty;
            boolValue = control.Font.Underline; 
            if (boolValue) {
                textDecoration += "underline ";
            }
 
            boolValue = control.Font.Strikeout;
            if (boolValue) { 
                textDecoration += "line-through"; 
            }
 
            boolValue = control.Font.Overline;
            if (boolValue) {
                textDecoration += "overline";
            } 

            if (textDecoration.Length > 0) { 
                styleAttributes["text-decoration"] = textDecoration.Trim(); 
            }
        } 

        public override string GetPersistenceContent() {
            // See comment in ControlDesigner for LocalizedInnerContent
            if (LocalizedInnerContent != null) { 
                return LocalizedInnerContent;
            } 
 
            // In general we return null because the control's content was explicitly
            // set from the editable designer region by calling Tag.SetContent(). Returning 
            // null implies that we have no new content to return and that the existing
            // tag content should be preserved.
            return null;
        } 

        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            string content = string.Empty; 
            if (Tag != null) {
                try { 
                    content = Tag.GetContent();
                    if (content == null) {
                        content = string.Empty;
                    } 
                }
                catch (Exception ex) { 
                    Debug.Fail(ex.ToString()); 
                }
            } 
            return content;
        }

        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            if (Tag != null) {
                try { 
                    Tag.SetContent(content); 
                }
                catch (Exception ex) { 
                    Debug.Fail(ex.ToString());
                }
            }
        } 

        private IDictionary ConvertFontInfoToCss(FontInfo font) { 
            // The code in this function is adapted from AddDesignTimeCssAttributes 

            IDictionary styleAttributes = new System.Collections.Specialized.HybridDictionary(); 

            string fontName = font.Name;
            if (fontName.Length != 0) {
                styleAttributes["font-family"] = System.Web.HttpUtility.HtmlEncode(fontName); 
            }
 
            FontUnit fontSize = font.Size; 
            if (fontSize != FontUnit.Empty) {
                styleAttributes["font-size"] = fontSize.ToString(CultureInfo.CurrentCulture); 
            }

            bool boolValue = font.Bold;
            if (boolValue) { 
                styleAttributes["font-weight"] = "bold";
            } 
 
            boolValue = font.Italic;
            if (boolValue) { 
                styleAttributes["font-style"] = "italic";
            }

            string textDecoration = String.Empty; 
            boolValue = font.Underline;
            if (boolValue) { 
                textDecoration += "underline "; 
            }
 
            boolValue = font.Strikeout;
            if (boolValue) {
                textDecoration += "line-through";
            } 

            boolValue = font.Overline; 
            if (boolValue) { 
                textDecoration += "overline";
            } 

            if (textDecoration.Length > 0) {
                styleAttributes["text-decoration"] = textDecoration.Trim();
            } 

            return styleAttributes; 
        } 

        private string CssDictionaryToString(IDictionary dictionary) { 
            string css = string.Empty;
            if (dictionary != null) {
                foreach (DictionaryEntry attr in dictionary) {
                    css += attr.Key + ":" + attr.Value + "; "; 
                }
            } 
            return css; 
        }
 
        private string GenerateDesignTimeHtml() {
            string caption = FrameCaption;

            Unit height = FrameStyle.Height; 
            Unit width = FrameStyle.Width;
 
            string cssClassName = String.Empty; 

            if (IsWebControl) { 
                WebControl control = (WebControl)ViewControl;
                if (height.IsEmpty) {
                    height = control.Height;
                } 
                if (width.IsEmpty) {
                    width = control.Width; 
                } 
                cssClassName = control.CssClass;
            } 

            if (caption == null) {
                caption = string.Empty;
            } 
            else if (caption.IndexOf('\0') > -1) {
                // Ignore null characters (VSWhidbey 129511) 
                // 

 
                caption = caption.Replace("\0", String.Empty);
            }

            string cssStyle = String.Empty; 
            WebControl webcontrol = ViewControl as WebControl;
            if (webcontrol != null) { 
                cssStyle = webcontrol.Style.Value + ";"; 
            }
 
            string borderCss = String.Empty;
            string borderBottomCss = String.Empty;
            if ((!FrameStyle.BorderWidth.IsEmpty && FrameStyle.BorderWidth.Value != 0) ||
                (FrameStyle.BorderStyle != BorderStyle.NotSet) || 
                (!FrameStyle.BorderColor.IsEmpty)) {
                borderCss = String.Format(CultureInfo.InvariantCulture, 
                    "border:{0} {1} {2}; ", 
                    FrameStyle.BorderWidth,
                    FrameStyle.BorderStyle, 
                    ColorTranslator.ToHtml(FrameStyle.BorderColor));
                borderBottomCss = String.Format(CultureInfo.InvariantCulture,
                    "border-bottom:{0} {1} {2}; ",
                    FrameStyle.BorderWidth, 
                    FrameStyle.BorderStyle,
                    ColorTranslator.ToHtml(FrameStyle.BorderColor)); 
            } 

            string foreColorCss = String.Empty; 
            if (!FrameStyle.ForeColor.IsEmpty) {
                foreColorCss = String.Format(CultureInfo.InvariantCulture,
                    "color:{0}; ",
                    ColorTranslator.ToHtml(FrameStyle.ForeColor)); 
            }
 
            string backColorCss = String.Empty; 
            if (!FrameStyle.BackColor.IsEmpty) {
                backColorCss = String.Format(CultureInfo.InvariantCulture, 
                    "background-color:{0}; ",
                    ColorTranslator.ToHtml(FrameStyle.BackColor));
            }
 
            return String.Format(CultureInfo.InvariantCulture,
                DesignTimeHtml, 
                borderCss, // 0 (has trailing semicolon) 
                borderBottomCss, // 1 (has trailing semicolon)
                foreColorCss, // 2 (has trailing semicolon) 
                backColorCss, // 3 (has trailing semicolon)
                CssDictionaryToString(ConvertFontInfoToCss(FrameStyle.Font)), // 4 (has trailing semicolon)
                caption, // 5
                CssDictionaryToString(GetDesignTimeCssAttributes()), // 6 (has trailing semicolon) 
                DesignerRegion.DesignerRegionAttributeName, // 7
                height, // 8 
                width, // 9 
                cssStyle, // 10 (has trailing semicolon)
                cssClassName); // 11 
        }

        /// <include file='doc\ContainerControlDesigner.uex' path='docs/doc[@for="ContainerControlDesigner.GetDesignTimeHtml"]/*' />
        /// <internalonly/> 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            // We must clear the child controls because the tool will parent the controls 
            // created from the editable region to the component.  Venus was already doing this 
            // for backwards compat with v1, this code was added so FrontPage would not have
            // to do the same. 
            Control control = (Control)Component;
            control.Controls.Clear();

            // Create the single editable region 
            EditableDesignerRegion region = new EditableDesignerRegion(this, "Content");
            region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark); 
            region.Properties[typeof(Control)] = control; 
            region.EnsureSize = true;
            regions.Add(region); 

            return GenerateDesignTimeHtml();
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
