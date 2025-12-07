//------------------------------------------------------------------------------ 
// <copyright file="ContentPlaceHolderDesigner.cs" company="Microsoft">
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

 
    /// <include file='doc\ContentPlaceHolderDesigner.uex' path='docs/doc[@for="ContentPlaceHolderDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.ContentPlaceHolder'/>
    ///       web control. 
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ContentPlaceHolderDesigner : ControlDesigner { 

        private string _content; 
        private const string designtimeHTML = 
        @"<table cellspacing=0 cellpadding=0 style=""border:1px solid black; width:100%; height:200px;"">
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

        public override bool AllowResize {
            get {
                return true; 
            }
        } 
 
        private string CreateDesignTimeHTML() {
            StringBuilder sb = new StringBuilder(1024); 

            Font defaultFont = SystemFonts.CaptionFont;
            Color foreColor = SystemColors.ControlText;
            Color backColor = SystemColors.Control; 

            string caption = Component.GetType().Name + " - " + Component.Site.Name; 
 
            sb.Append(String.Format(CultureInfo.InvariantCulture, designtimeHTML, caption, DesignerRegion.DesignerRegionAttributeName,
                defaultFont.SizeInPoints, ColorTranslator.ToHtml(foreColor), ColorTranslator.ToHtml(backColor))); 
            return sb.ToString();
        }

        public override string GetDesignTimeHtml() { 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (!(host.RootComponent is MasterPage)) { 
                throw new InvalidOperationException(SR.GetString(SR.ContentPlaceHolder_Invalid_RootComponent)); 
            }
            return base.GetDesignTimeHtml(); 
        }

        /// <internalonly/>
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            if (!(host.RootComponent is MasterPage)) { 
                throw new InvalidOperationException(SR.GetString(SR.ContentPlaceHolder_Invalid_RootComponent));
            } 

            // Create the single editable region
            regions.Add(new EditableDesignerRegion(this, "Content"));
            return CreateDesignTimeHTML(); 
        }
 
        /// <internalonly/> 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            if (_content == null) { 
                _content = Tag.GetContent();
            }
            return _content != null ? _content.Trim() : string.Empty;
        } 

        public override string GetPersistenceContent() { 
            return _content; 
        }
 
        /// <internalonly/>
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
            _content = content;
            Tag.SetDirty(true); 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ContentPlaceHolderDesigner.cs" company="Microsoft">
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

 
    /// <include file='doc\ContentPlaceHolderDesigner.uex' path='docs/doc[@for="ContentPlaceHolderDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.ContentPlaceHolder'/>
    ///       web control. 
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ContentPlaceHolderDesigner : ControlDesigner { 

        private string _content; 
        private const string designtimeHTML = 
        @"<table cellspacing=0 cellpadding=0 style=""border:1px solid black; width:100%; height:200px;"">
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

        public override bool AllowResize {
            get {
                return true; 
            }
        } 
 
        private string CreateDesignTimeHTML() {
            StringBuilder sb = new StringBuilder(1024); 

            Font defaultFont = SystemFonts.CaptionFont;
            Color foreColor = SystemColors.ControlText;
            Color backColor = SystemColors.Control; 

            string caption = Component.GetType().Name + " - " + Component.Site.Name; 
 
            sb.Append(String.Format(CultureInfo.InvariantCulture, designtimeHTML, caption, DesignerRegion.DesignerRegionAttributeName,
                defaultFont.SizeInPoints, ColorTranslator.ToHtml(foreColor), ColorTranslator.ToHtml(backColor))); 
            return sb.ToString();
        }

        public override string GetDesignTimeHtml() { 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (!(host.RootComponent is MasterPage)) { 
                throw new InvalidOperationException(SR.GetString(SR.ContentPlaceHolder_Invalid_RootComponent)); 
            }
            return base.GetDesignTimeHtml(); 
        }

        /// <internalonly/>
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            if (!(host.RootComponent is MasterPage)) { 
                throw new InvalidOperationException(SR.GetString(SR.ContentPlaceHolder_Invalid_RootComponent));
            } 

            // Create the single editable region
            regions.Add(new EditableDesignerRegion(this, "Content"));
            return CreateDesignTimeHTML(); 
        }
 
        /// <internalonly/> 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            if (_content == null) { 
                _content = Tag.GetContent();
            }
            return _content != null ? _content.Trim() : string.Empty;
        } 

        public override string GetPersistenceContent() { 
            return _content; 
        }
 
        /// <internalonly/>
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
            _content = content;
            Tag.SetDirty(true); 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
