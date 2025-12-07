//------------------------------------------------------------------------------ 
// <copyright file="GridViewSelectEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.ComponentModel;
    using System.Security.Permissions; 

 
    /// <devdoc> 
    ///    <para>Provides data for
    ///       the <see langword='GridViewSelect'/> 
    ///       event.</para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class GridViewSelectEventArgs : CancelEventArgs {
 
        private int _newSelectedIndex; 

 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.GridViewSelectEventArgs'/> class.</para>
        /// </devdoc>
        public GridViewSelectEventArgs(int newSelectedIndex) { 
            this._newSelectedIndex = newSelectedIndex;
        } 
 

        /// <devdoc> 
        /// <para>Gets the index of the selected row to be displayed in the <see cref='System.Web.UI.WebControls.GridView'/>.
        ///    This property is read-only.</para>
        /// </devdoc>
        public int NewSelectedIndex { 
            get {
                return _newSelectedIndex; 
            } 
            set {
                _newSelectedIndex = value; 
            }
        }
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="GridViewSelectEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Web.UI.WebControls { 

    using System;
    using System.ComponentModel;
    using System.Security.Permissions; 

 
    /// <devdoc> 
    ///    <para>Provides data for
    ///       the <see langword='GridViewSelect'/> 
    ///       event.</para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class GridViewSelectEventArgs : CancelEventArgs {
 
        private int _newSelectedIndex; 

 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.GridViewSelectEventArgs'/> class.</para>
        /// </devdoc>
        public GridViewSelectEventArgs(int newSelectedIndex) { 
            this._newSelectedIndex = newSelectedIndex;
        } 
 

        /// <devdoc> 
        /// <para>Gets the index of the selected row to be displayed in the <see cref='System.Web.UI.WebControls.GridView'/>.
        ///    This property is read-only.</para>
        /// </devdoc>
        public int NewSelectedIndex { 
            get {
                return _newSelectedIndex; 
            } 
            set {
                _newSelectedIndex = value; 
            }
        }
    }
} 

