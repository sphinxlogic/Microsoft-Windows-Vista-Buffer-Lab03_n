//------------------------------------------------------------------------------ 
// <copyright file="DataGridSortCommandEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// <para>Provides data for the <see langword='DataGridSortCommand'/> event of a <see cref='System.Web.UI.WebControls.DataGrid'/>. 
    /// </para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DataGridSortCommandEventArgs : EventArgs {
 
        private string sortExpression;
        private object commandSource; 
 

        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.DataGridSortCommandEventArgs'/> class.</para>
        /// </devdoc>
        public DataGridSortCommandEventArgs(object commandSource, DataGridCommandEventArgs dce) {
            this.commandSource = commandSource; 
            this.sortExpression = (string)dce.CommandArgument;
        } 
 

 
        /// <devdoc>
        ///    <para>Gets the source of the command. This property is read-only. </para>
        /// </devdoc>
        public object CommandSource { 
            get {
                return commandSource; 
            } 
        }
 

        /// <devdoc>
        ///    <para>Gets the expression used to sort. This property is read-only.</para>
        /// </devdoc> 
        public string SortExpression {
            get { 
                return sortExpression; 
            }
        } 
    }
}

//------------------------------------------------------------------------------ 
// <copyright file="DataGridSortCommandEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// <para>Provides data for the <see langword='DataGridSortCommand'/> event of a <see cref='System.Web.UI.WebControls.DataGrid'/>. 
    /// </para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DataGridSortCommandEventArgs : EventArgs {
 
        private string sortExpression;
        private object commandSource; 
 

        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.DataGridSortCommandEventArgs'/> class.</para>
        /// </devdoc>
        public DataGridSortCommandEventArgs(object commandSource, DataGridCommandEventArgs dce) {
            this.commandSource = commandSource; 
            this.sortExpression = (string)dce.CommandArgument;
        } 
 

 
        /// <devdoc>
        ///    <para>Gets the source of the command. This property is read-only. </para>
        /// </devdoc>
        public object CommandSource { 
            get {
                return commandSource; 
            } 
        }
 

        /// <devdoc>
        ///    <para>Gets the expression used to sort. This property is read-only.</para>
        /// </devdoc> 
        public string SortExpression {
            get { 
                return sortExpression; 
            }
        } 
    }
}

