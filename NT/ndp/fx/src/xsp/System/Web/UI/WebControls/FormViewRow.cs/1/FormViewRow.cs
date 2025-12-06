//------------------------------------------------------------------------------ 
// <copyright file="FormViewRow.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;


    /// <devdoc> 
    /// <para>Represents an individual row in the <see cref='System.Web.UI.WebControls.FormView'/>.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class FormViewRow : TableRow { 

        private int _itemIndex;
        private DataControlRowType _rowType;
        private DataControlRowState _rowState; 

 
 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.FormViewRow'/> class.</para> 
        /// </devdoc>
        public FormViewRow(int itemIndex, DataControlRowType rowType, DataControlRowState rowState) {
            this._itemIndex = itemIndex;
            this._rowType = rowType; 
            this._rowState = rowState;
        } 
 

        /// <devdoc> 
        /// <para>Indicates the index of the item in the <see cref='System.Web.UI.WebControls.FormView'/>. This property is
        ///    read-only.</para>
        /// </devdoc>
        public virtual int ItemIndex { 
            get {
                return _itemIndex; 
            } 
        }
 

        /// <devdoc>
        /// <para>Indicates the type of the row in the <see cref='System.Web.UI.WebControls.FormView'/>.</para>
        /// </devdoc> 
        public virtual DataControlRowState RowState {
            get { 
                return _rowState; 
            }
        } 


        /// <devdoc>
        /// <para>Indicates the type of the row in the <see cref='System.Web.UI.WebControls.FormView'/>.</para> 
        /// </devdoc>
        public virtual DataControlRowType RowType { 
            get { 
                return _rowType;
            } 
        }


 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc> 
        protected override bool OnBubbleEvent(object source, EventArgs e) {
            if (e is CommandEventArgs) { 
                FormViewCommandEventArgs args = new FormViewCommandEventArgs(source, (CommandEventArgs)e);

                RaiseBubbleEvent(this, args);
                return true; 
            }
            return false; 
        } 
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="FormViewRow.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.ComponentModel; 
    using System.Security.Permissions;


    /// <devdoc> 
    /// <para>Represents an individual row in the <see cref='System.Web.UI.WebControls.FormView'/>.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class FormViewRow : TableRow { 

        private int _itemIndex;
        private DataControlRowType _rowType;
        private DataControlRowState _rowState; 

 
 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.FormViewRow'/> class.</para> 
        /// </devdoc>
        public FormViewRow(int itemIndex, DataControlRowType rowType, DataControlRowState rowState) {
            this._itemIndex = itemIndex;
            this._rowType = rowType; 
            this._rowState = rowState;
        } 
 

        /// <devdoc> 
        /// <para>Indicates the index of the item in the <see cref='System.Web.UI.WebControls.FormView'/>. This property is
        ///    read-only.</para>
        /// </devdoc>
        public virtual int ItemIndex { 
            get {
                return _itemIndex; 
            } 
        }
 

        /// <devdoc>
        /// <para>Indicates the type of the row in the <see cref='System.Web.UI.WebControls.FormView'/>.</para>
        /// </devdoc> 
        public virtual DataControlRowState RowState {
            get { 
                return _rowState; 
            }
        } 


        /// <devdoc>
        /// <para>Indicates the type of the row in the <see cref='System.Web.UI.WebControls.FormView'/>.</para> 
        /// </devdoc>
        public virtual DataControlRowType RowType { 
            get { 
                return _rowType;
            } 
        }


 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc> 
        protected override bool OnBubbleEvent(object source, EventArgs e) {
            if (e is CommandEventArgs) { 
                FormViewCommandEventArgs args = new FormViewCommandEventArgs(source, (CommandEventArgs)e);

                RaiseBubbleEvent(this, args);
                return true; 
            }
            return false; 
        } 
    }
} 

