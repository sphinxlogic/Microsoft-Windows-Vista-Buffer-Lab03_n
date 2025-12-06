//------------------------------------------------------------------------------ 
// <copyright file="ControlParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text; 
    using System.Web.UI;
 
    /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser"]/*' /> 
    /// <devdoc>
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class ControlParser {
 
        private ControlParser() {
        } 
 
        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseControl"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Control ParseControl(IDesignerHost designerHost, string controlText) {
            if (designerHost == null) { 
                throw new ArgumentNullException("designerHost");
            } 
            if ((controlText == null) || (controlText.Length == 0)) { 
                throw new ArgumentNullException("controlText");
            } 

            return ControlSerializer.DeserializeControl(controlText, designerHost);
        }
 
        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseControl"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        internal static Control ParseControl(IDesignerHost designerHost, string controlText, bool applyTheme) { 
            if (designerHost == null) {
                throw new ArgumentNullException("designerHost");
            }
            if ((controlText == null) || (controlText.Length == 0)) { 
                throw new ArgumentNullException("controlText");
            } 
 
            return ControlSerializer.DeserializeControlInternal(controlText, designerHost, applyTheme);
        } 

        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseControl1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static Control ParseControl(IDesignerHost designerHost, string controlText, string directives) { 
            if (designerHost == null) { 
                throw new ArgumentNullException("designerHost");
            } 
            if ((controlText == null) || (controlText.Length == 0)) {
                throw new ArgumentNullException("controlText");
            }
 
            if ((directives != null) && (directives.Length != 0)) {
                controlText = directives + controlText; 
            } 

            return ControlSerializer.DeserializeControl(controlText, designerHost); 
        }

        public static Control[] ParseControls(IDesignerHost designerHost, string controlText) {
            if (designerHost == null) { 
                throw new ArgumentNullException("designerHost");
            } 
            if ((controlText == null) || (controlText.Length == 0)) { 
                throw new ArgumentNullException("controlText");
            } 

            return ControlSerializer.DeserializeControls(controlText, designerHost);
        }
 
        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseTemplate"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static ITemplate ParseTemplate(IDesignerHost designerHost, string templateText) { 
            if (designerHost == null) {
                throw new ArgumentNullException("designerHost");
            }
 
            return ControlSerializer.DeserializeTemplate(templateText, designerHost);
        } 
 
        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseTemplate1"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static ITemplate ParseTemplate(IDesignerHost designerHost, string templateText, string directives) {
            if (designerHost == null) { 
                throw new ArgumentNullException("designerHost");
            } 
 
            return ControlSerializer.DeserializeTemplate(templateText, designerHost);
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text; 
    using System.Web.UI;
 
    /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser"]/*' /> 
    /// <devdoc>
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class ControlParser {
 
        private ControlParser() {
        } 
 
        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseControl"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Control ParseControl(IDesignerHost designerHost, string controlText) {
            if (designerHost == null) { 
                throw new ArgumentNullException("designerHost");
            } 
            if ((controlText == null) || (controlText.Length == 0)) { 
                throw new ArgumentNullException("controlText");
            } 

            return ControlSerializer.DeserializeControl(controlText, designerHost);
        }
 
        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseControl"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        internal static Control ParseControl(IDesignerHost designerHost, string controlText, bool applyTheme) { 
            if (designerHost == null) {
                throw new ArgumentNullException("designerHost");
            }
            if ((controlText == null) || (controlText.Length == 0)) { 
                throw new ArgumentNullException("controlText");
            } 
 
            return ControlSerializer.DeserializeControlInternal(controlText, designerHost, applyTheme);
        } 

        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseControl1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static Control ParseControl(IDesignerHost designerHost, string controlText, string directives) { 
            if (designerHost == null) { 
                throw new ArgumentNullException("designerHost");
            } 
            if ((controlText == null) || (controlText.Length == 0)) {
                throw new ArgumentNullException("controlText");
            }
 
            if ((directives != null) && (directives.Length != 0)) {
                controlText = directives + controlText; 
            } 

            return ControlSerializer.DeserializeControl(controlText, designerHost); 
        }

        public static Control[] ParseControls(IDesignerHost designerHost, string controlText) {
            if (designerHost == null) { 
                throw new ArgumentNullException("designerHost");
            } 
            if ((controlText == null) || (controlText.Length == 0)) { 
                throw new ArgumentNullException("controlText");
            } 

            return ControlSerializer.DeserializeControls(controlText, designerHost);
        }
 
        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseTemplate"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static ITemplate ParseTemplate(IDesignerHost designerHost, string templateText) { 
            if (designerHost == null) {
                throw new ArgumentNullException("designerHost");
            }
 
            return ControlSerializer.DeserializeTemplate(templateText, designerHost);
        } 
 
        /// <include file='doc\ControlParser.uex' path='docs/doc[@for="ControlParser.ParseTemplate1"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static ITemplate ParseTemplate(IDesignerHost designerHost, string templateText, string directives) {
            if (designerHost == null) { 
                throw new ArgumentNullException("designerHost");
            } 
 
            return ControlSerializer.DeserializeTemplate(templateText, designerHost);
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
