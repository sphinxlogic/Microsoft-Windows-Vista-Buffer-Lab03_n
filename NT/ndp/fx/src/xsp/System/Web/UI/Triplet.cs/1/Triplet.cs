//------------------------------------------------------------------------------ 
// <copyright file="Triplet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System;
    using System.Security.Permissions; 


    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [Serializable] 
    public sealed class Triplet {
 
 
        public object First;
 
        public object Second;

        public object Third;
 

        public Triplet() { 
        } 

 
        public Triplet (object x, object y) {
            First = x;
            Second = y;
        } 

 
        public Triplet (object x, object y, object z) { 
            First = x;
            Second = y; 
            Third = z;
        }
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="Triplet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System;
    using System.Security.Permissions; 


    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [Serializable] 
    public sealed class Triplet {
 
 
        public object First;
 
        public object Second;

        public object Third;
 

        public Triplet() { 
        } 

 
        public Triplet (object x, object y) {
            First = x;
            Second = y;
        } 

 
        public Triplet (object x, object y, object z) { 
            First = x;
            Second = y; 
            Third = z;
        }
    }
} 
