//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewCommandEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// <para>Provides data for some <see cref='System.Web.UI.WebControls.DetailsView'/> events.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DetailsViewCommandEventArgs : CommandEventArgs {

        private object _commandSource; 

 
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.DetailsViewCommandEventArgs'/>
        /// class.</para> 
        /// </devdoc>
        public DetailsViewCommandEventArgs(object commandSource, CommandEventArgs originalArgs) : base(originalArgs) {
            this._commandSource = commandSource;
        } 

 
        /// <devdoc> 
        ///    <para>Gets the source of the command. This property is read-only.</para>
        /// </devdoc> 
        public object CommandSource {
            get {
                return _commandSource;
            } 
        }
    } 
} 

//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewCommandEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// <para>Provides data for some <see cref='System.Web.UI.WebControls.DetailsView'/> events.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DetailsViewCommandEventArgs : CommandEventArgs {

        private object _commandSource; 

 
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.DetailsViewCommandEventArgs'/>
        /// class.</para> 
        /// </devdoc>
        public DetailsViewCommandEventArgs(object commandSource, CommandEventArgs originalArgs) : base(originalArgs) {
            this._commandSource = commandSource;
        } 

 
        /// <devdoc> 
        ///    <para>Gets the source of the command. This property is read-only.</para>
        /// </devdoc> 
        public object CommandSource {
            get {
                return _commandSource;
            } 
        }
    } 
} 

