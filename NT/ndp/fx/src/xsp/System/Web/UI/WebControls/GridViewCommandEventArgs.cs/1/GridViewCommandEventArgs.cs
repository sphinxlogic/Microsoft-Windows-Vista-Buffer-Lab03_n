//------------------------------------------------------------------------------ 
// <copyright file="GridViewCommandEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// <para>Provides data for some <see cref='System.Web.UI.WebControls.GridView'/> events.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class GridViewCommandEventArgs : CommandEventArgs {

        private GridViewRow _row; 
        private object _commandSource;
 
 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.GridViewCommandEventArgs'/> 
        /// class.</para>
        /// </devdoc>
        public GridViewCommandEventArgs(GridViewRow row, object commandSource, CommandEventArgs originalArgs) : base(originalArgs) {
            this._row = row; 
            this._commandSource = commandSource;
        } 
 

        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.GridViewCommandEventArgs'/>
        /// class.</para>
        /// </devdoc>
        public GridViewCommandEventArgs(object commandSource, CommandEventArgs originalArgs) : base(originalArgs) { 
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

 
        /// <devdoc>
        /// <para>Gets the row in the <see cref='System.Web.UI.WebControls.GridView'/> that was clicked. This property is read-only.</para>
        /// </devdoc>
        internal GridViewRow Row { 
            get {
                return _row; 
            } 
        }
    } 
}

//------------------------------------------------------------------------------ 
// <copyright file="GridViewCommandEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// <para>Provides data for some <see cref='System.Web.UI.WebControls.GridView'/> events.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class GridViewCommandEventArgs : CommandEventArgs {

        private GridViewRow _row; 
        private object _commandSource;
 
 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.GridViewCommandEventArgs'/> 
        /// class.</para>
        /// </devdoc>
        public GridViewCommandEventArgs(GridViewRow row, object commandSource, CommandEventArgs originalArgs) : base(originalArgs) {
            this._row = row; 
            this._commandSource = commandSource;
        } 
 

        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.GridViewCommandEventArgs'/>
        /// class.</para>
        /// </devdoc>
        public GridViewCommandEventArgs(object commandSource, CommandEventArgs originalArgs) : base(originalArgs) { 
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

 
        /// <devdoc>
        /// <para>Gets the row in the <see cref='System.Web.UI.WebControls.GridView'/> that was clicked. This property is read-only.</para>
        /// </devdoc>
        internal GridViewRow Row { 
            get {
                return _row; 
            } 
        }
    } 
}

