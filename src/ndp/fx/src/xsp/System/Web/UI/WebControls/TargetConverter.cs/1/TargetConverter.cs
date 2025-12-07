//------------------------------------------------------------------------------ 
// <copyright file="TargetConverter.cs" company="Microsoft">
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
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class TargetConverter: StringConverter {

        private static string []  targetValues = {
            "_blank", 
            "_parent",
            "_search", 
            "_self", 
            "_top"
        }; 

        private StandardValuesCollection values;

 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc> 
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            if (values == null) { 
                values = new StandardValuesCollection(targetValues);
            }
            return values;
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return false;
        }
 

        /// <internalonly/> 
        /// <devdoc> 
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
            return true;
        }

    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="TargetConverter.cs" company="Microsoft">
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
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class TargetConverter: StringConverter {

        private static string []  targetValues = {
            "_blank", 
            "_parent",
            "_search", 
            "_self", 
            "_top"
        }; 

        private StandardValuesCollection values;

 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc> 
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            if (values == null) { 
                values = new StandardValuesCollection(targetValues);
            }
            return values;
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return false;
        }
 

        /// <internalonly/> 
        /// <devdoc> 
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
            return true;
        }

    } 
}
