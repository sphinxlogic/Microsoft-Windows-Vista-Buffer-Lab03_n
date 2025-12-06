//------------------------------------------------------------------------------ 
// <copyright file="Pair.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System;
    using System.Security.Permissions; 


    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [Serializable] 
    public sealed class Pair {
 
        public object First; 

        public object Second; 


        public Pair () {
        } 

 
        public Pair (object x, object y) { 
            First = x;
            Second = y; 
        }
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="Pair.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System;
    using System.Security.Permissions; 


    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [Serializable] 
    public sealed class Pair {
 
        public object First; 

        public object Second; 


        public Pair () {
        } 

 
        public Pair (object x, object y) { 
            First = x;
            Second = y; 
        }
    }
}
