//------------------------------------------------------------------------------ 
// <copyright file="AssociatedControlConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.Globalization;
    using System.Reflection;
    using System.Security.Permissions;
 

    /// <devdoc> 
    ///    <para> Filters and retrieves several types of values from validated controls.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class AssociatedControlConverter: ControlIDConverter {

        /// <devdoc> 
        ///    <para>Determines whether a given control should have its id added to the StandardValuesCollection.</para>
        /// </devdoc> 
        protected override bool FilterControl(Control control) { 
            return control is WebControl;
        } 
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="AssociatedControlConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.Globalization;
    using System.Reflection;
    using System.Security.Permissions;
 

    /// <devdoc> 
    ///    <para> Filters and retrieves several types of values from validated controls.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class AssociatedControlConverter: ControlIDConverter {

        /// <devdoc> 
        ///    <para>Determines whether a given control should have its id added to the StandardValuesCollection.</para>
        /// </devdoc> 
        protected override bool FilterControl(Control control) { 
            return control is WebControl;
        } 
    }
}
