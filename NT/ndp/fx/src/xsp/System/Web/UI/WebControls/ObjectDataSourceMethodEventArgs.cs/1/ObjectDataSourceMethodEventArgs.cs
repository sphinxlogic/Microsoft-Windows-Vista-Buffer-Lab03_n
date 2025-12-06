//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceMethodEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Security.Permissions;

 
    /// <devdoc>
    /// Represents data that is passed into an ObjectDataSourceMethodEventHandler delegate. 
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ObjectDataSourceMethodEventArgs : CancelEventArgs {

        private IOrderedDictionary _inputParameters;
 

 
        /// <devdoc> 
        /// Creates a new instance of ObjectDataSourceMethodEventArgs.
        /// </devdoc> 
        public ObjectDataSourceMethodEventArgs(IOrderedDictionary inputParameters) {
            _inputParameters = inputParameters;
        }
 

 
        /// <devdoc> 
        /// The input parameters that will be passed to the method that will be invoked.
        /// Change these parameters if the names and/or types need to be modified 
        /// for the invocation to succeed.
        /// </devdoc>
        public IOrderedDictionary InputParameters {
            get { 
                return _inputParameters;
            } 
        } 
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceMethodEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Security.Permissions;

 
    /// <devdoc>
    /// Represents data that is passed into an ObjectDataSourceMethodEventHandler delegate. 
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ObjectDataSourceMethodEventArgs : CancelEventArgs {

        private IOrderedDictionary _inputParameters;
 

 
        /// <devdoc> 
        /// Creates a new instance of ObjectDataSourceMethodEventArgs.
        /// </devdoc> 
        public ObjectDataSourceMethodEventArgs(IOrderedDictionary inputParameters) {
            _inputParameters = inputParameters;
        }
 

 
        /// <devdoc> 
        /// The input parameters that will be passed to the method that will be invoked.
        /// Change these parameters if the names and/or types need to be modified 
        /// for the invocation to succeed.
        /// </devdoc>
        public IOrderedDictionary InputParameters {
            get { 
                return _inputParameters;
            } 
        } 
    }
} 

