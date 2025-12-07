//------------------------------------------------------------------------------ 
// <copyright file="CalendarDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.IO; 
    using System.Runtime.InteropServices; 
    using System.Text;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms;

    using Calendar = System.Web.UI.WebControls.Calendar; 

    /// <include file='doc\CalendarDesigner.uex' path='docs/doc[@for="CalendarDesigner"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       Provides a designer for the <see cref='System.Web.UI.WebControls.Calendar'/> 
    ///       control.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)]
    public class CalendarDesigner : ControlDesigner { 
 
        private static DesignerAutoFormatCollection _autoFormats;
 
        public override DesignerAutoFormatCollection AutoFormats {
            get {
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.CALENDAR_SCHEMES, 
                        delegate(DataRow schemeData) { return new CalendarAutoFormat(schemeData); });
                } 
                return _autoFormats; 
            }
        } 

        /// <include file='doc\CalendarDesigner.uex' path='docs/doc[@for="CalendarDesigner.Initialize"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes the designer with the component for design.
        ///    </para> 
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(Calendar)); 
            base.Initialize(component);
        }

        /// <include file='doc\CalendarDesigner.uex' path='docs/doc[@for="CalendarDesigner.OnAutoFormat"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Delegate to handle the the AutoFormat verb by calling the AutoFormat dialog. 
        ///    </para>
        /// </devdoc> 
        protected void OnAutoFormat(object sender, EventArgs e) {
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="CalendarDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.IO; 
    using System.Runtime.InteropServices; 
    using System.Text;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms;

    using Calendar = System.Web.UI.WebControls.Calendar; 

    /// <include file='doc\CalendarDesigner.uex' path='docs/doc[@for="CalendarDesigner"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       Provides a designer for the <see cref='System.Web.UI.WebControls.Calendar'/> 
    ///       control.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)]
    public class CalendarDesigner : ControlDesigner { 
 
        private static DesignerAutoFormatCollection _autoFormats;
 
        public override DesignerAutoFormatCollection AutoFormats {
            get {
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.CALENDAR_SCHEMES, 
                        delegate(DataRow schemeData) { return new CalendarAutoFormat(schemeData); });
                } 
                return _autoFormats; 
            }
        } 

        /// <include file='doc\CalendarDesigner.uex' path='docs/doc[@for="CalendarDesigner.Initialize"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes the designer with the component for design.
        ///    </para> 
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(Calendar)); 
            base.Initialize(component);
        }

        /// <include file='doc\CalendarDesigner.uex' path='docs/doc[@for="CalendarDesigner.OnAutoFormat"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Delegate to handle the the AutoFormat verb by calling the AutoFormat dialog. 
        ///    </para>
        /// </devdoc> 
        protected void OnAutoFormat(object sender, EventArgs e) {
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
