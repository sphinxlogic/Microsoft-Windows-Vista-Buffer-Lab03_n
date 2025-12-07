//------------------------------------------------------------------------------ 
// <copyright file="TitleStyle.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class TitleStyle : TableItemStyle { 

        public TitleStyle() { 
            Wrap = false; 
        }
 
        [
        DefaultValue(false)
        ]
        public override bool Wrap { 
            get {
                return base.Wrap; 
            } 
            set {
                base.Wrap = value; 
            }
        }
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="TitleStyle.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class TitleStyle : TableItemStyle { 

        public TitleStyle() { 
            Wrap = false; 
        }
 
        [
        DefaultValue(false)
        ]
        public override bool Wrap { 
            get {
                return base.Wrap; 
            } 
            set {
                base.Wrap = value; 
            }
        }
    }
} 
