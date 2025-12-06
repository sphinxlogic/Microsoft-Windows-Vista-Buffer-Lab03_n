//------------------------------------------------------------------------------ 
// <copyright file="BulletedListEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Web; 
    using System.Security.Permissions;


    /// <devdoc> 
    /// <para>The event args when a bulletedlist causes a postback.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class BulletedListEventArgs : EventArgs { 

        private int _index;

 
        /// <devdoc>
        /// Constructor. 
        /// </devdoc> 
        /// <param name="index">The index of the element which caused the event.</param>
        public BulletedListEventArgs(int index) { 
            _index = index;
        }

 
        /// <devdoc>
        /// The index of the element which caused the event. 
        /// </devdoc> 
        public int Index {
            get { 
                return _index;
            }
        }
    } 
}
 
//------------------------------------------------------------------------------ 
// <copyright file="BulletedListEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Web; 
    using System.Security.Permissions;


    /// <devdoc> 
    /// <para>The event args when a bulletedlist causes a postback.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class BulletedListEventArgs : EventArgs { 

        private int _index;

 
        /// <devdoc>
        /// Constructor. 
        /// </devdoc> 
        /// <param name="index">The index of the element which caused the event.</param>
        public BulletedListEventArgs(int index) { 
            _index = index;
        }

 
        /// <devdoc>
        /// The index of the element which caused the event. 
        /// </devdoc> 
        public int Index {
            get { 
                return _index;
            }
        }
    } 
}
 
