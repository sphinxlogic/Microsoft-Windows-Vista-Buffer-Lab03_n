//------------------------------------------------------------------------------ 
// <copyright file="GridViewActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms; 

    /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList"]/*' /> 
    internal class GridViewActionList : DesignerActionList {
        private GridViewDesigner _gridViewDesigner;

        private bool _allowDeleting; 
        private bool _allowEditing;
        private bool _allowSorting; 
        private bool _allowPaging; 
        private bool _allowSelection;
 
        private bool _allowRemoveField;
        private bool _allowMoveLeft;
        private bool _allowMoveRight;
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.GridViewActionList"]/*' />
        public GridViewActionList(GridViewDesigner gridViewDesigner) : base(gridViewDesigner.Component) { 
            _gridViewDesigner = gridViewDesigner; 
        }
 
        /// <summary>
        /// Lets the GridView designer specify whether the Delete action should be visible/enabled
        /// </summary>
        internal bool AllowDeleting { 
            get {
                return _allowDeleting; 
            } 
            set {
                _allowDeleting = value; 
            }
        }

        /// <summary> 
        /// Lets the GridView designer specify whether the Edit action should be visible/enabled
        /// </summary> 
        internal bool AllowEditing { 
            get {
                return _allowEditing; 
            }
            set {
                _allowEditing = value;
            } 
        }
 
        /// <summary> 
        /// Lets the GridView designer specify whether the Move Left action should be visible/enabled
        /// </summary> 
        internal bool AllowMoveLeft {
            get {
                return _allowMoveLeft;
            } 
            set {
                _allowMoveLeft = value; 
            } 
        }
 
        /// <summary>
        /// Lets the GridView designer specify whether the Move Right action should be visible/enabled
        /// </summary>
        internal bool AllowMoveRight { 
            get {
                return _allowMoveRight; 
            } 
            set {
                _allowMoveRight = value; 
            }
        }

        /// <summary> 
        /// Lets the GridView designer specify whether the Page action should be visible/enabled
        /// </summary> 
        internal bool AllowPaging { 
            get {
                return _allowPaging; 
            }
            set {
                _allowPaging = value;
            } 
        }
 
        /// <summary> 
        /// Lets the GridView designer specify whether the Remove Field action should be visible/enabled
        /// </summary> 
        internal bool AllowRemoveField {
            get {
                return _allowRemoveField;
            } 
            set {
                _allowRemoveField = value; 
            } 
        }
 
        /// <summary>
        /// Lets the GridView designer specify whether the Select action should be visible/enabled
        /// </summary>
        internal bool AllowSelection { 
            get {
                return _allowSelection; 
            } 
            set {
                _allowSelection = value; 
            }
        }

        /// <summary> 
        /// Lets the GridView designer specify whether the Sort action should be visible/enabled
        /// </summary> 
        internal bool AllowSorting { 
            get {
                return _allowSorting; 
            }
            set {
                _allowSorting = value;
            } 
        }
 
        public override bool AutoShow { 
            get {
                return true; 
            }
            set {
            }
        } 

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnableDeleting"]/*' /> 
        /// <summary> 
        /// Property used by chrome to display the delete checkbox.  Called through reflection.
        /// </summary> 
        public bool EnableDeleting {
            get {
                return _gridViewDesigner.EnableDeleting;
            } 
            set {
                _gridViewDesigner.EnableDeleting = value; 
            } 
        }
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnableEditing"]/*' />
        /// <summary>
        /// Property used by chrome to display the delete checkbox.  Called through reflection.
        /// </summary> 
        public bool EnableEditing {
            get { 
                return _gridViewDesigner.EnableEditing; 
            }
            set { 
                _gridViewDesigner.EnableEditing = value;
            }
        }
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnablePaging"]/*' />
        /// <summary> 
        /// Property used by chrome to display the page checkbox.  Called through reflection. 
        /// </summary>
        public bool EnablePaging { 
            get {
                return _gridViewDesigner.EnablePaging;
            }
            set { 
                _gridViewDesigner.EnablePaging = value;
            } 
        } 

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnableSelection"]/*' /> 
        /// <summary>
        /// Property used by chrome to display the select checkbox.  Called through reflection.
        /// </summary>
        public bool EnableSelection { 
            get {
                return _gridViewDesigner.EnableSelection; 
            } 
            set {
                _gridViewDesigner.EnableSelection = value; 
            }
        }

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnableSorting"]/*' /> 
        /// <summary>
        /// Property used by chrome to display the sort checkbox.  Called through reflection. 
        /// </summary> 
        public bool EnableSorting {
            get { 
                return _gridViewDesigner.EnableSorting;
            }
            set {
                _gridViewDesigner.EnableSorting = value; 
            }
        } 
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.AddNewField"]/*' />
        /// <summary> 
        /// Handler for the Add new field action.  Called through reflection.
        /// </summary>
        public void AddNewField() {
            _gridViewDesigner.AddNewField(); 
        }
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EditFields"]/*' /> 
        /// <summary>
        /// Handler for the edit fields action.  Called through reflection. 
        /// </summary>
        public void EditFields() {
            _gridViewDesigner.EditFields();
        } 

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.MoveFieldLeft"]/*' /> 
        /// <summary> 
        /// Handler for the move field left action.  Called through reflection.
        /// </summary> 
        public void MoveFieldLeft() {
            _gridViewDesigner.MoveLeft();
        }
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.MoveFieldRight"]/*' />
        /// <summary> 
        /// Handler for the move field right action.  Called through reflection. 
        /// </summary>
        public void MoveFieldRight() { 
            _gridViewDesigner.MoveRight();
        }

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.RemoveField"]/*' /> 
        /// <summary>
        /// Handler for the remove field action.  Called through reflection. 
        /// </summary> 
        public void RemoveField() {
            _gridViewDesigner.RemoveField(); 
        }

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.GetSortedActionItems"]/*' />
        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
 
            items.Add(new DesignerActionMethodItem(this, 
                                                                 "EditFields",
                                                                 SR.GetString(SR.GridView_EditFieldsVerb), 
                                                                 "Action",
                                                                 SR.GetString(SR.GridView_EditFieldsDesc)));
            items.Add(new DesignerActionMethodItem(this,
                                                                 "AddNewField", 
                                                                 SR.GetString(SR.GridView_AddNewFieldVerb),
                                                                 "Action", 
                                                                 SR.GetString(SR.GridView_AddNewFieldDesc))); 
            if (AllowMoveLeft) {
                items.Add(new DesignerActionMethodItem(this, 
                                                                     "MoveFieldLeft",
                                                                     SR.GetString(SR.GridView_MoveFieldLeftVerb),
                                                                     "Action",
                                                                     SR.GetString(SR.GridView_MoveFieldLeftDesc))); 
            }
            if (AllowMoveRight) { 
                items.Add(new DesignerActionMethodItem(this, 
                                                                     "MoveFieldRight",
                                                                     SR.GetString(SR.GridView_MoveFieldRightVerb), 
                                                                     "Action",
                                                                     SR.GetString(SR.GridView_MoveFieldRightDesc)));
            }
            if (AllowRemoveField) { 
                items.Add(new DesignerActionMethodItem(this,
                                                                     "RemoveField", 
                                                                     SR.GetString(SR.GridView_RemoveFieldVerb), 
                                                                     "Action",
                                                                     SR.GetString(SR.GridView_RemoveFieldDesc))); 
            }
            if (AllowPaging) {
                items.Add(new DesignerActionPropertyItem("EnablePaging",
                                                                       SR.GetString(SR.GridView_EnablePaging), 
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnablePagingDesc))); 
            } 
            if (AllowSorting) {
                items.Add(new DesignerActionPropertyItem("EnableSorting", 
                                                                       SR.GetString(SR.GridView_EnableSorting),
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnableSortingDesc)));
            } 
            if (AllowEditing) {
                items.Add(new DesignerActionPropertyItem("EnableEditing", 
                                                                       SR.GetString(SR.GridView_EnableEditing), 
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnableEditingDesc))); 
            }
            if (AllowDeleting) {
                items.Add(new DesignerActionPropertyItem("EnableDeleting",
                                                                       SR.GetString(SR.GridView_EnableDeleting), 
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnableDeletingDesc))); 
            } 
            if (AllowSelection) {
                items.Add(new DesignerActionPropertyItem("EnableSelection", 
                                                                       SR.GetString(SR.GridView_EnableSelection),
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnableSelectionDesc)));
            } 
            return items;
        } 
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="GridViewActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms; 

    /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList"]/*' /> 
    internal class GridViewActionList : DesignerActionList {
        private GridViewDesigner _gridViewDesigner;

        private bool _allowDeleting; 
        private bool _allowEditing;
        private bool _allowSorting; 
        private bool _allowPaging; 
        private bool _allowSelection;
 
        private bool _allowRemoveField;
        private bool _allowMoveLeft;
        private bool _allowMoveRight;
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.GridViewActionList"]/*' />
        public GridViewActionList(GridViewDesigner gridViewDesigner) : base(gridViewDesigner.Component) { 
            _gridViewDesigner = gridViewDesigner; 
        }
 
        /// <summary>
        /// Lets the GridView designer specify whether the Delete action should be visible/enabled
        /// </summary>
        internal bool AllowDeleting { 
            get {
                return _allowDeleting; 
            } 
            set {
                _allowDeleting = value; 
            }
        }

        /// <summary> 
        /// Lets the GridView designer specify whether the Edit action should be visible/enabled
        /// </summary> 
        internal bool AllowEditing { 
            get {
                return _allowEditing; 
            }
            set {
                _allowEditing = value;
            } 
        }
 
        /// <summary> 
        /// Lets the GridView designer specify whether the Move Left action should be visible/enabled
        /// </summary> 
        internal bool AllowMoveLeft {
            get {
                return _allowMoveLeft;
            } 
            set {
                _allowMoveLeft = value; 
            } 
        }
 
        /// <summary>
        /// Lets the GridView designer specify whether the Move Right action should be visible/enabled
        /// </summary>
        internal bool AllowMoveRight { 
            get {
                return _allowMoveRight; 
            } 
            set {
                _allowMoveRight = value; 
            }
        }

        /// <summary> 
        /// Lets the GridView designer specify whether the Page action should be visible/enabled
        /// </summary> 
        internal bool AllowPaging { 
            get {
                return _allowPaging; 
            }
            set {
                _allowPaging = value;
            } 
        }
 
        /// <summary> 
        /// Lets the GridView designer specify whether the Remove Field action should be visible/enabled
        /// </summary> 
        internal bool AllowRemoveField {
            get {
                return _allowRemoveField;
            } 
            set {
                _allowRemoveField = value; 
            } 
        }
 
        /// <summary>
        /// Lets the GridView designer specify whether the Select action should be visible/enabled
        /// </summary>
        internal bool AllowSelection { 
            get {
                return _allowSelection; 
            } 
            set {
                _allowSelection = value; 
            }
        }

        /// <summary> 
        /// Lets the GridView designer specify whether the Sort action should be visible/enabled
        /// </summary> 
        internal bool AllowSorting { 
            get {
                return _allowSorting; 
            }
            set {
                _allowSorting = value;
            } 
        }
 
        public override bool AutoShow { 
            get {
                return true; 
            }
            set {
            }
        } 

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnableDeleting"]/*' /> 
        /// <summary> 
        /// Property used by chrome to display the delete checkbox.  Called through reflection.
        /// </summary> 
        public bool EnableDeleting {
            get {
                return _gridViewDesigner.EnableDeleting;
            } 
            set {
                _gridViewDesigner.EnableDeleting = value; 
            } 
        }
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnableEditing"]/*' />
        /// <summary>
        /// Property used by chrome to display the delete checkbox.  Called through reflection.
        /// </summary> 
        public bool EnableEditing {
            get { 
                return _gridViewDesigner.EnableEditing; 
            }
            set { 
                _gridViewDesigner.EnableEditing = value;
            }
        }
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnablePaging"]/*' />
        /// <summary> 
        /// Property used by chrome to display the page checkbox.  Called through reflection. 
        /// </summary>
        public bool EnablePaging { 
            get {
                return _gridViewDesigner.EnablePaging;
            }
            set { 
                _gridViewDesigner.EnablePaging = value;
            } 
        } 

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnableSelection"]/*' /> 
        /// <summary>
        /// Property used by chrome to display the select checkbox.  Called through reflection.
        /// </summary>
        public bool EnableSelection { 
            get {
                return _gridViewDesigner.EnableSelection; 
            } 
            set {
                _gridViewDesigner.EnableSelection = value; 
            }
        }

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EnableSorting"]/*' /> 
        /// <summary>
        /// Property used by chrome to display the sort checkbox.  Called through reflection. 
        /// </summary> 
        public bool EnableSorting {
            get { 
                return _gridViewDesigner.EnableSorting;
            }
            set {
                _gridViewDesigner.EnableSorting = value; 
            }
        } 
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.AddNewField"]/*' />
        /// <summary> 
        /// Handler for the Add new field action.  Called through reflection.
        /// </summary>
        public void AddNewField() {
            _gridViewDesigner.AddNewField(); 
        }
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.EditFields"]/*' /> 
        /// <summary>
        /// Handler for the edit fields action.  Called through reflection. 
        /// </summary>
        public void EditFields() {
            _gridViewDesigner.EditFields();
        } 

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.MoveFieldLeft"]/*' /> 
        /// <summary> 
        /// Handler for the move field left action.  Called through reflection.
        /// </summary> 
        public void MoveFieldLeft() {
            _gridViewDesigner.MoveLeft();
        }
 
        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.MoveFieldRight"]/*' />
        /// <summary> 
        /// Handler for the move field right action.  Called through reflection. 
        /// </summary>
        public void MoveFieldRight() { 
            _gridViewDesigner.MoveRight();
        }

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.RemoveField"]/*' /> 
        /// <summary>
        /// Handler for the remove field action.  Called through reflection. 
        /// </summary> 
        public void RemoveField() {
            _gridViewDesigner.RemoveField(); 
        }

        /// <include file='doc\GridViewActionList.uex' path='docs/doc[@for="GridViewActionList.GetSortedActionItems"]/*' />
        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
 
            items.Add(new DesignerActionMethodItem(this, 
                                                                 "EditFields",
                                                                 SR.GetString(SR.GridView_EditFieldsVerb), 
                                                                 "Action",
                                                                 SR.GetString(SR.GridView_EditFieldsDesc)));
            items.Add(new DesignerActionMethodItem(this,
                                                                 "AddNewField", 
                                                                 SR.GetString(SR.GridView_AddNewFieldVerb),
                                                                 "Action", 
                                                                 SR.GetString(SR.GridView_AddNewFieldDesc))); 
            if (AllowMoveLeft) {
                items.Add(new DesignerActionMethodItem(this, 
                                                                     "MoveFieldLeft",
                                                                     SR.GetString(SR.GridView_MoveFieldLeftVerb),
                                                                     "Action",
                                                                     SR.GetString(SR.GridView_MoveFieldLeftDesc))); 
            }
            if (AllowMoveRight) { 
                items.Add(new DesignerActionMethodItem(this, 
                                                                     "MoveFieldRight",
                                                                     SR.GetString(SR.GridView_MoveFieldRightVerb), 
                                                                     "Action",
                                                                     SR.GetString(SR.GridView_MoveFieldRightDesc)));
            }
            if (AllowRemoveField) { 
                items.Add(new DesignerActionMethodItem(this,
                                                                     "RemoveField", 
                                                                     SR.GetString(SR.GridView_RemoveFieldVerb), 
                                                                     "Action",
                                                                     SR.GetString(SR.GridView_RemoveFieldDesc))); 
            }
            if (AllowPaging) {
                items.Add(new DesignerActionPropertyItem("EnablePaging",
                                                                       SR.GetString(SR.GridView_EnablePaging), 
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnablePagingDesc))); 
            } 
            if (AllowSorting) {
                items.Add(new DesignerActionPropertyItem("EnableSorting", 
                                                                       SR.GetString(SR.GridView_EnableSorting),
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnableSortingDesc)));
            } 
            if (AllowEditing) {
                items.Add(new DesignerActionPropertyItem("EnableEditing", 
                                                                       SR.GetString(SR.GridView_EnableEditing), 
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnableEditingDesc))); 
            }
            if (AllowDeleting) {
                items.Add(new DesignerActionPropertyItem("EnableDeleting",
                                                                       SR.GetString(SR.GridView_EnableDeleting), 
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnableDeletingDesc))); 
            } 
            if (AllowSelection) {
                items.Add(new DesignerActionPropertyItem("EnableSelection", 
                                                                       SR.GetString(SR.GridView_EnableSelection),
                                                                       "Behavior",
                                                                       SR.GetString(SR.GridView_EnableSelectionDesc)));
            } 
            return items;
        } 
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
