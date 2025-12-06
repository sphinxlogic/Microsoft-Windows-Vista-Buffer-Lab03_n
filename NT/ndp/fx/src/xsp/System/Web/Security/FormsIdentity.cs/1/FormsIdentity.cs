//------------------------------------------------------------------------------ 
// <copyright file="FormsIdentity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * FormsIdentity 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */

namespace System.Web.Security {
    using  System.Security.Principal; 
    using System.Security.Permissions;
 
 
    /// <devdoc>
    ///    This class is an IIdentity derived class 
    ///    used by FormsAuthenticationModule. It provides a way for an application to
    ///    access the cookie authentication ticket.
    /// </devdoc>
    [Serializable] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class FormsIdentity : IIdentity { 
 
        /// <devdoc>
        ///    The name of the identity (in this case, the 
        ///    passport user name).
        /// </devdoc>
        public  String                       Name { get { return _Ticket.Name;}}
 
        /// <devdoc>
        ///    The type of the identity (in this case, 
        ///    "Forms"). 
        /// </devdoc>
        public  String                       AuthenticationType { get { return "Forms";}} 

        /// <devdoc>
        ///    Indicates whether or not authentication took
        ///    place. 
        /// </devdoc>
        public  bool                         IsAuthenticated { get { return true;}} 
 
        /// <devdoc>
        ///    Returns the FormsAuthenticationTicket 
        ///    associated with the current request.
        /// </devdoc>
        public  FormsAuthenticationTicket   Ticket { get { return _Ticket;}}
 

        /// <devdoc> 
        ///    Constructor. 
        /// </devdoc>
        public FormsIdentity (FormsAuthenticationTicket ticket) { 
            _Ticket = ticket;
        }

 
        private FormsAuthenticationTicket _Ticket;
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="FormsIdentity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * FormsIdentity 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */

namespace System.Web.Security {
    using  System.Security.Principal; 
    using System.Security.Permissions;
 
 
    /// <devdoc>
    ///    This class is an IIdentity derived class 
    ///    used by FormsAuthenticationModule. It provides a way for an application to
    ///    access the cookie authentication ticket.
    /// </devdoc>
    [Serializable] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class FormsIdentity : IIdentity { 
 
        /// <devdoc>
        ///    The name of the identity (in this case, the 
        ///    passport user name).
        /// </devdoc>
        public  String                       Name { get { return _Ticket.Name;}}
 
        /// <devdoc>
        ///    The type of the identity (in this case, 
        ///    "Forms"). 
        /// </devdoc>
        public  String                       AuthenticationType { get { return "Forms";}} 

        /// <devdoc>
        ///    Indicates whether or not authentication took
        ///    place. 
        /// </devdoc>
        public  bool                         IsAuthenticated { get { return true;}} 
 
        /// <devdoc>
        ///    Returns the FormsAuthenticationTicket 
        ///    associated with the current request.
        /// </devdoc>
        public  FormsAuthenticationTicket   Ticket { get { return _Ticket;}}
 

        /// <devdoc> 
        ///    Constructor. 
        /// </devdoc>
        public FormsIdentity (FormsAuthenticationTicket ticket) { 
            _Ticket = ticket;
        }

 
        private FormsAuthenticationTicket _Ticket;
    } 
} 
