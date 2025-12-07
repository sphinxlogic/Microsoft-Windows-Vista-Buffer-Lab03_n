//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewPageEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;


    /// <devdoc> 
    ///    <para>Provides data for
    ///       the <see langword='DetailsViewPage'/> 
    ///       event.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DetailsViewPageEventArgs : CancelEventArgs {

        private int _newPageIndex; 

 
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.DetailsViewPageEventArgs'/> class.</para>
        /// </devdoc> 
        public DetailsViewPageEventArgs(int newPageIndex) {
            this._newPageIndex = newPageIndex;
        }
 
        /// <devdoc>
        /// <para>Gets the index of the first new page to be displayed in the <see cref='System.Web.UI.WebControls.DetailsView'/>. 
        ///    This property is read-only.</para> 
        /// </devdoc>
        public int NewPageIndex { 
            get {
                return _newPageIndex;
            }
            set { 
                _newPageIndex = value;
            } 
        } 
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewPageEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;


    /// <devdoc> 
    ///    <para>Provides data for
    ///       the <see langword='DetailsViewPage'/> 
    ///       event.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DetailsViewPageEventArgs : CancelEventArgs {

        private int _newPageIndex; 

 
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.DetailsViewPageEventArgs'/> class.</para>
        /// </devdoc> 
        public DetailsViewPageEventArgs(int newPageIndex) {
            this._newPageIndex = newPageIndex;
        }
 
        /// <devdoc>
        /// <para>Gets the index of the first new page to be displayed in the <see cref='System.Web.UI.WebControls.DetailsView'/>. 
        ///    This property is read-only.</para> 
        /// </devdoc>
        public int NewPageIndex { 
            get {
                return _newPageIndex;
            }
            set { 
                _newPageIndex = value;
            } 
        } 
    }
} 

