//------------------------------------------------------------------------------ 
// <copyright file="BuilderPropertyEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System.Security.Permissions;
 
    /// <devdoc>
    /// Abstract base class for all property entries that require a ControlBuilder
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public abstract class BuilderPropertyEntry : PropertyEntry { 
        private ControlBuilder _builder; 

        internal BuilderPropertyEntry() { 
        }


        /// <devdoc> 
        /// </devdoc>
        public ControlBuilder Builder { 
            get { 
                return _builder;
            } 
            set {
                _builder = value;
            }
        } 
    }
} 
 

//------------------------------------------------------------------------------ 
// <copyright file="BuilderPropertyEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System.Security.Permissions;
 
    /// <devdoc>
    /// Abstract base class for all property entries that require a ControlBuilder
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public abstract class BuilderPropertyEntry : PropertyEntry { 
        private ControlBuilder _builder; 

        internal BuilderPropertyEntry() { 
        }


        /// <devdoc> 
        /// </devdoc>
        public ControlBuilder Builder { 
            get { 
                return _builder;
            } 
            set {
                _builder = value;
            }
        } 
    }
} 
 

