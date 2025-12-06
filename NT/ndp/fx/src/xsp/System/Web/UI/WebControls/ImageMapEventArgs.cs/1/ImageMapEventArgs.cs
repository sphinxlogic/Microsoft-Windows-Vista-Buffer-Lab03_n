//------------------------------------------------------------------------------ 
// <copyright file="ImageMapEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.WebControls {
 
    using System; 
    using System.Security.Permissions;
 

    /// <devdoc>
    /// <para>Provides data for the ImageMap click event.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ImageMapEventArgs : EventArgs { 

        private string _postBackValue; 


        public ImageMapEventArgs(string value) {
            _postBackValue = value; 
        }
 
 
        /// <devdoc>
        /// <para>Gets the value associated with the clicked area.</para> 
        /// </devdoc>
        public string PostBackValue {
            get {
                return _postBackValue; 
            }
        } 
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="ImageMapEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.WebControls {
 
    using System; 
    using System.Security.Permissions;
 

    /// <devdoc>
    /// <para>Provides data for the ImageMap click event.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ImageMapEventArgs : EventArgs { 

        private string _postBackValue; 


        public ImageMapEventArgs(string value) {
            _postBackValue = value; 
        }
 
 
        /// <devdoc>
        /// <para>Gets the value associated with the clicked area.</para> 
        /// </devdoc>
        public string PostBackValue {
            get {
                return _postBackValue; 
            }
        } 
    } 
}
