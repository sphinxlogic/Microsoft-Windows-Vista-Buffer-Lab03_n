//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceSelectingEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections.Specialized; 
    using System.Security.Permissions;

    /// <devdoc>
    /// Represents data that is passed into an ObjectDataSourceSelectingEventHandler delegate. 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ObjectDataSourceSelectingEventArgs : ObjectDataSourceMethodEventArgs {
 
        private DataSourceSelectArguments _arguments;
        private bool _executingSelectCount;

 
        /// <devdoc>
        /// Creates a new instance of ObjectDataSourceSelectingEventArgs. 
        /// </devdoc> 
        public ObjectDataSourceSelectingEventArgs(IOrderedDictionary inputParameters, DataSourceSelectArguments arguments, bool executingSelectCount) : base(inputParameters) {
            _arguments = arguments; 
            _executingSelectCount = executingSelectCount;
        }

        public DataSourceSelectArguments Arguments { 
            get {
                return _arguments; 
            } 
        }
 
        public bool ExecutingSelectCount {
            get {
                return _executingSelectCount;
            } 
        }
    } 
} 

//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceSelectingEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections.Specialized; 
    using System.Security.Permissions;

    /// <devdoc>
    /// Represents data that is passed into an ObjectDataSourceSelectingEventHandler delegate. 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ObjectDataSourceSelectingEventArgs : ObjectDataSourceMethodEventArgs {
 
        private DataSourceSelectArguments _arguments;
        private bool _executingSelectCount;

 
        /// <devdoc>
        /// Creates a new instance of ObjectDataSourceSelectingEventArgs. 
        /// </devdoc> 
        public ObjectDataSourceSelectingEventArgs(IOrderedDictionary inputParameters, DataSourceSelectArguments arguments, bool executingSelectCount) : base(inputParameters) {
            _arguments = arguments; 
            _executingSelectCount = executingSelectCount;
        }

        public DataSourceSelectArguments Arguments { 
            get {
                return _arguments; 
            } 
        }
 
        public bool ExecutingSelectCount {
            get {
                return _executingSelectCount;
            } 
        }
    } 
} 

