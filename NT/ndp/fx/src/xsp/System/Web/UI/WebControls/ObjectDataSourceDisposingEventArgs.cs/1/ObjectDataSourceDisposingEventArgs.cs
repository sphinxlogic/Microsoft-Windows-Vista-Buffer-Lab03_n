//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceDisposingEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Security.Permissions;

 
    /// <devdoc>
    /// Represents data that is passed into an ObjectDataSourceDisposingEventHandler delegate. 
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ObjectDataSourceDisposingEventArgs : CancelEventArgs {

        private object _objectInstance;
 

 
        /// <devdoc> 
        /// Creates a new instance of ObjectDataSourceDisposingEventArgs.
        /// </devdoc> 
        public ObjectDataSourceDisposingEventArgs(object objectInstance) : base() {
            _objectInstance = objectInstance;
        }
 

 
        /// <devdoc> 
        /// The instance of the object created by the ObjectDataSource. Set this
        /// property if you need to create the object using a non-default 
        /// constructor.
        /// </devdoc>
        public object ObjectInstance {
            get { 
                return _objectInstance;
            } 
        } 
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceDisposingEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Security.Permissions;

 
    /// <devdoc>
    /// Represents data that is passed into an ObjectDataSourceDisposingEventHandler delegate. 
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ObjectDataSourceDisposingEventArgs : CancelEventArgs {

        private object _objectInstance;
 

 
        /// <devdoc> 
        /// Creates a new instance of ObjectDataSourceDisposingEventArgs.
        /// </devdoc> 
        public ObjectDataSourceDisposingEventArgs(object objectInstance) : base() {
            _objectInstance = objectInstance;
        }
 

 
        /// <devdoc> 
        /// The instance of the object created by the ObjectDataSource. Set this
        /// property if you need to create the object using a non-default 
        /// constructor.
        /// </devdoc>
        public object ObjectInstance {
            get { 
                return _objectInstance;
            } 
        } 
    }
} 

