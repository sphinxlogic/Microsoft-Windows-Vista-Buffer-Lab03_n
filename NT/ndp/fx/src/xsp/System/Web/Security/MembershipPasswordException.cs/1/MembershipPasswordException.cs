//------------------------------------------------------------------------------ 
// <copyright file="MembershipPasswordException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Security { 
    using System; 
    using System.Runtime.Serialization;
    using System.Web; 
    using System.Security.Permissions;


    [Serializable] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class MembershipPasswordException : Exception 
    {
 
        public MembershipPasswordException(String message) : base(message)
        { }

 
        protected MembershipPasswordException(SerializationInfo info, StreamingContext context) : base(info, context)
        { } 
 
        public MembershipPasswordException()
        { } 

        public MembershipPasswordException(String message, Exception innerException) : base(message, innerException)
        { }
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="MembershipPasswordException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Security { 
    using System; 
    using System.Runtime.Serialization;
    using System.Web; 
    using System.Security.Permissions;


    [Serializable] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class MembershipPasswordException : Exception 
    {
 
        public MembershipPasswordException(String message) : base(message)
        { }

 
        protected MembershipPasswordException(SerializationInfo info, StreamingContext context) : base(info, context)
        { } 
 
        public MembershipPasswordException()
        { } 

        public MembershipPasswordException(String message, Exception innerException) : base(message, innerException)
        { }
    } 
}
