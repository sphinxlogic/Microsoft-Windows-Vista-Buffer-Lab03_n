//------------------------------------------------------------------------------ 
// <copyright file="ImageClickEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class ImageClickEventArgs : EventArgs {

        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int X; 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int Y;

 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public ImageClickEventArgs(int x,int y) { 
            this.X = x;
            this.Y = y;
        }
 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="ImageClickEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI { 

    using System;
    using System.Security.Permissions;
 

    /// <devdoc> 
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class ImageClickEventArgs : EventArgs {

        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int X; 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public int Y;

 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public ImageClickEventArgs(int x,int y) { 
            this.X = x;
            this.Y = y;
        }
 
    }
} 
