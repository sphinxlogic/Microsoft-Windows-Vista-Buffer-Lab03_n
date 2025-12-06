//------------------------------------------------------------------------------ 
// <copyright file="TemplatePropertyEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System.Security.Permissions;
 
    /// <devdoc>
    /// PropertyEntry for ITemplate properties
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class TemplatePropertyEntry : BuilderPropertyEntry { 
        private bool _bindableTemplate; 

        internal TemplatePropertyEntry() { 
        }

        internal TemplatePropertyEntry(bool bindableTemplate) {
            _bindableTemplate = bindableTemplate; 
        }
 
 
        public bool BindableTemplate {
            get { 
                return _bindableTemplate;
            }
        }
    } 

 
} 

 
//------------------------------------------------------------------------------ 
// <copyright file="TemplatePropertyEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System.Security.Permissions;
 
    /// <devdoc>
    /// PropertyEntry for ITemplate properties
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class TemplatePropertyEntry : BuilderPropertyEntry { 
        private bool _bindableTemplate; 

        internal TemplatePropertyEntry() { 
        }

        internal TemplatePropertyEntry(bool bindableTemplate) {
            _bindableTemplate = bindableTemplate; 
        }
 
 
        public bool BindableTemplate {
            get { 
                return _bindableTemplate;
            }
        }
    } 

 
} 

 
