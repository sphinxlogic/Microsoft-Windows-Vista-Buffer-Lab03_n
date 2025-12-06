//------------------------------------------------------------------------------ 
// <copyright file="FormViewCommandEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// <para>Provides data for some <see cref='System.Web.UI.WebControls.FormView'/> events.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class FormViewCommandEventArgs : CommandEventArgs {

        private object _commandSource; 

 
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.FormViewCommandEventArgs'/>
        /// class.</para> 
        /// </devdoc>
        public FormViewCommandEventArgs(object commandSource, CommandEventArgs originalArgs) : base(originalArgs) {
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
// <copyright file="FormViewCommandEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// <para>Provides data for some <see cref='System.Web.UI.WebControls.FormView'/> events.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class FormViewCommandEventArgs : CommandEventArgs {

        private object _commandSource; 

 
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.FormViewCommandEventArgs'/>
        /// class.</para> 
        /// </devdoc>
        public FormViewCommandEventArgs(object commandSource, CommandEventArgs originalArgs) : base(originalArgs) {
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

