//------------------------------------------------------------------------------ 
// <copyright file="PassportPrincipal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * PassportPrincipal 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */

namespace System.Web.Security {
    using System.Security.Principal; 
    using System.Security.Permissions;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class PassportPrincipal : GenericPrincipal {
        public PassportPrincipal(PassportIdentity identity, string[] roles) : base(identity, roles) 
        { }
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="PassportPrincipal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * PassportPrincipal 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */

namespace System.Web.Security {
    using System.Security.Principal; 
    using System.Security.Permissions;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class PassportPrincipal : GenericPrincipal {
        public PassportPrincipal(PassportIdentity identity, string[] roles) : base(identity, roles) 
        { }
    }
}
