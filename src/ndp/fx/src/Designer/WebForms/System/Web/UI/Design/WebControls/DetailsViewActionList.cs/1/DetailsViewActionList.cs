//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewActionList.cs" company="Microsoft">
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

    /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList"]/*' /> 
    internal class DetailsViewActionList : DesignerActionList {
        private DetailsViewDesigner _detailsViewDesigner;

        private bool _allowDeleting; 
        private bool _allowEditing;
        private bool _allowInserting; 
        private bool _allowPaging; 

        private bool _allowRemoveField; 
        private bool _allowMoveUp;
        private bool _allowMoveDown;

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.DetailsViewActionList"]/*' /> 
        public DetailsViewActionList(DetailsViewDesigner detailsViewDesigner) : base(detailsViewDesigner.Component) {
            _detailsViewDesigner = detailsViewDesigner; 
        } 

        /// <summary> 
        /// Lets the DetailsView designer specify whether the Delete action should be visible/enabled
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
        /// Lets the DetailsView designer specify whether the Edit action should be visible/enabled 
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
        /// Lets the DetailsView designer specify whether the Insert action should be visible/enabled 
        /// </summary>
        internal bool AllowInserting { 
            get {
                return _allowInserting;
            }
            set { 
                _allowInserting = value;
            } 
        } 

        /// <summary> 
        /// Lets the DetailsView designer specify whether the Move Down action should be visible/enabled
        /// </summary>
        internal bool AllowMoveDown {
            get { 
                return _allowMoveDown;
            } 
            set { 
                _allowMoveDown = value;
            } 
        }

        /// <summary>
        /// Lets the DetailsView designer specify whether the Move Up action should be visible/enabled 
        /// </summary>
        internal bool AllowMoveUp { 
            get { 
                return _allowMoveUp;
            } 
            set {
                _allowMoveUp = value;
            }
        } 

        /// <summary> 
        /// Lets the DetailsView designer specify whether the Page action should be visible/enabled 
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
        /// Lets the DetailsView designer specify whether the Remove Field action should be visible/enabled
        /// </summary>
        internal bool AllowRemoveField {
            get { 
                return _allowRemoveField;
            } 
            set { 
                _allowRemoveField = value;
            } 
        }

        public override bool AutoShow {
            get { 
                return true;
            } 
            set { 
            }
        } 

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EnableDeleting"]/*' />
        /// <summary>
        /// Property used by chrome to display the delete checkbox.  Called through reflection. 
        /// </summary>
        public bool EnableDeleting { 
            get { 
                return _detailsViewDesigner.EnableDeleting;
            } 
            set {
                _detailsViewDesigner.EnableDeleting = value;
            }
        } 

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EnableEditing"]/*' /> 
        /// <summary> 
        /// Property used by chrome to display the delete checkbox.  Called through reflection.
        /// </summary> 
        public bool EnableEditing {
            get {
                return _detailsViewDesigner.EnableEditing;
            } 
            set {
                _detailsViewDesigner.EnableEditing = value; 
            } 
        }
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EnableInserting"]/*' />
        /// <summary>
        /// Property used by chrome to display the insert checkbox.  Called through reflection.
        /// </summary> 
        public bool EnableInserting {
            get { 
                return _detailsViewDesigner.EnableInserting; 
            }
            set { 
                _detailsViewDesigner.EnableInserting = value;
            }
        }
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EnablePaging"]/*' />
        /// <summary> 
        /// Property used by chrome to display the page checkbox.  Called through reflection. 
        /// </summary>
        public bool EnablePaging { 
            get {
                return _detailsViewDesigner.EnablePaging;
            }
            set { 
                _detailsViewDesigner.EnablePaging = value;
            } 
        } 

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.AddNewField"]/*' /> 
        /// <summary>
        /// Handler for the Add new field action.  Called through reflection.
        /// </summary>
        public void AddNewField() { 
            _detailsViewDesigner.AddNewField();
        } 
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EditFields"]/*' />
        /// <summary> 
        /// Handler for the edit fields action.  Called through reflection.
        /// </summary>
        public void EditFields() {
            _detailsViewDesigner.EditFields(); 
        }
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.MoveFieldUp"]/*' /> 
        /// <summary>
        /// Handler for the move field up action.  Called through reflection. 
        /// </summary>
        public void MoveFieldUp() {
            _detailsViewDesigner.MoveUp();
        } 

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.MoveFieldDown"]/*' /> 
        /// <summary> 
        /// Handler for the move field down action.  Called through reflection.
        /// </summary> 
        public void MoveFieldDown() {
            _detailsViewDesigner.MoveDown();
        }
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.RemoveField"]/*' />
        /// <summary> 
        /// Handler for the remove field action.  Called through reflection. 
        /// </summary>
        public void RemoveField() { 
            _detailsViewDesigner.RemoveField();
        }

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.GetSortedActionItems"]/*' /> 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
 
            items.Add(new DesignerActionMethodItem(this,
                                                                 "EditFields", 
                                                                 SR.GetString(SR.DetailsView_EditFieldsVerb),
                                                                 "Action",
                                                                 SR.GetString(SR.DetailsView_EditFieldsDesc)));
            items.Add(new DesignerActionMethodItem(this, 
                                                                 "AddNewField",
                                                                 SR.GetString(SR.DetailsView_AddNewFieldVerb), 
                                                                 "Action", 
                                                                 SR.GetString(SR.DetailsView_AddNewFieldDesc)));
            if (AllowMoveUp) { 
                items.Add(new DesignerActionMethodItem(this,
                                                                     "MoveFieldUp",
                                                                     SR.GetString(SR.DetailsView_MoveFieldUpVerb),
                                                                     "Action", 
                                                                     SR.GetString(SR.DetailsView_MoveFieldUpDesc)));
            } 
            if (AllowMoveDown) { 
                items.Add(new DesignerActionMethodItem(this,
                                                                     "MoveFieldDown", 
                                                                     SR.GetString(SR.DetailsView_MoveFieldDownVerb),
                                                                     "Action",
                                                                     SR.GetString(SR.DetailsView_MoveFieldDownDesc)));
            } 
            if (AllowRemoveField) {
                items.Add(new DesignerActionMethodItem(this, 
                                                                     "RemoveField", 
                                                                     SR.GetString(SR.DetailsView_RemoveFieldVerb),
                                                                     "Action", 
                                                                     SR.GetString(SR.DetailsView_RemoveFieldDesc)));
            }
            if (AllowPaging) {
                items.Add(new DesignerActionPropertyItem("EnablePaging", 
                                                                       SR.GetString(SR.DetailsView_EnablePaging),
                                                                       "Behavior", 
                                                                       SR.GetString(SR.DetailsView_EnablePagingDesc))); 
            }
            if (AllowInserting) { 
                items.Add(new DesignerActionPropertyItem("EnableInserting",
                                                                       SR.GetString(SR.DetailsView_EnableInserting),
                                                                       "Behavior",
                                                                       SR.GetString(SR.DetailsView_EnableInsertingDesc))); 
            }
            if (AllowEditing) { 
                items.Add(new DesignerActionPropertyItem("EnableEditing", 
                                                                       SR.GetString(SR.DetailsView_EnableEditing),
                                                                       "Behavior", 
                                                                       SR.GetString(SR.DetailsView_EnableEditingDesc)));
            }
            if (AllowDeleting) {
                items.Add(new DesignerActionPropertyItem("EnableDeleting", 
                                                                       SR.GetString(SR.DetailsView_EnableDeleting),
                                                                       "Behavior", 
                                                                       SR.GetString(SR.DetailsView_EnableDeletingDesc))); 
            }
            return items; 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewActionList.cs" company="Microsoft">
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

    /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList"]/*' /> 
    internal class DetailsViewActionList : DesignerActionList {
        private DetailsViewDesigner _detailsViewDesigner;

        private bool _allowDeleting; 
        private bool _allowEditing;
        private bool _allowInserting; 
        private bool _allowPaging; 

        private bool _allowRemoveField; 
        private bool _allowMoveUp;
        private bool _allowMoveDown;

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.DetailsViewActionList"]/*' /> 
        public DetailsViewActionList(DetailsViewDesigner detailsViewDesigner) : base(detailsViewDesigner.Component) {
            _detailsViewDesigner = detailsViewDesigner; 
        } 

        /// <summary> 
        /// Lets the DetailsView designer specify whether the Delete action should be visible/enabled
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
        /// Lets the DetailsView designer specify whether the Edit action should be visible/enabled 
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
        /// Lets the DetailsView designer specify whether the Insert action should be visible/enabled 
        /// </summary>
        internal bool AllowInserting { 
            get {
                return _allowInserting;
            }
            set { 
                _allowInserting = value;
            } 
        } 

        /// <summary> 
        /// Lets the DetailsView designer specify whether the Move Down action should be visible/enabled
        /// </summary>
        internal bool AllowMoveDown {
            get { 
                return _allowMoveDown;
            } 
            set { 
                _allowMoveDown = value;
            } 
        }

        /// <summary>
        /// Lets the DetailsView designer specify whether the Move Up action should be visible/enabled 
        /// </summary>
        internal bool AllowMoveUp { 
            get { 
                return _allowMoveUp;
            } 
            set {
                _allowMoveUp = value;
            }
        } 

        /// <summary> 
        /// Lets the DetailsView designer specify whether the Page action should be visible/enabled 
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
        /// Lets the DetailsView designer specify whether the Remove Field action should be visible/enabled
        /// </summary>
        internal bool AllowRemoveField {
            get { 
                return _allowRemoveField;
            } 
            set { 
                _allowRemoveField = value;
            } 
        }

        public override bool AutoShow {
            get { 
                return true;
            } 
            set { 
            }
        } 

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EnableDeleting"]/*' />
        /// <summary>
        /// Property used by chrome to display the delete checkbox.  Called through reflection. 
        /// </summary>
        public bool EnableDeleting { 
            get { 
                return _detailsViewDesigner.EnableDeleting;
            } 
            set {
                _detailsViewDesigner.EnableDeleting = value;
            }
        } 

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EnableEditing"]/*' /> 
        /// <summary> 
        /// Property used by chrome to display the delete checkbox.  Called through reflection.
        /// </summary> 
        public bool EnableEditing {
            get {
                return _detailsViewDesigner.EnableEditing;
            } 
            set {
                _detailsViewDesigner.EnableEditing = value; 
            } 
        }
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EnableInserting"]/*' />
        /// <summary>
        /// Property used by chrome to display the insert checkbox.  Called through reflection.
        /// </summary> 
        public bool EnableInserting {
            get { 
                return _detailsViewDesigner.EnableInserting; 
            }
            set { 
                _detailsViewDesigner.EnableInserting = value;
            }
        }
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EnablePaging"]/*' />
        /// <summary> 
        /// Property used by chrome to display the page checkbox.  Called through reflection. 
        /// </summary>
        public bool EnablePaging { 
            get {
                return _detailsViewDesigner.EnablePaging;
            }
            set { 
                _detailsViewDesigner.EnablePaging = value;
            } 
        } 

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.AddNewField"]/*' /> 
        /// <summary>
        /// Handler for the Add new field action.  Called through reflection.
        /// </summary>
        public void AddNewField() { 
            _detailsViewDesigner.AddNewField();
        } 
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.EditFields"]/*' />
        /// <summary> 
        /// Handler for the edit fields action.  Called through reflection.
        /// </summary>
        public void EditFields() {
            _detailsViewDesigner.EditFields(); 
        }
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.MoveFieldUp"]/*' /> 
        /// <summary>
        /// Handler for the move field up action.  Called through reflection. 
        /// </summary>
        public void MoveFieldUp() {
            _detailsViewDesigner.MoveUp();
        } 

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.MoveFieldDown"]/*' /> 
        /// <summary> 
        /// Handler for the move field down action.  Called through reflection.
        /// </summary> 
        public void MoveFieldDown() {
            _detailsViewDesigner.MoveDown();
        }
 
        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.RemoveField"]/*' />
        /// <summary> 
        /// Handler for the remove field action.  Called through reflection. 
        /// </summary>
        public void RemoveField() { 
            _detailsViewDesigner.RemoveField();
        }

        /// <include file='doc\DetailsViewActionList.uex' path='docs/doc[@for="DetailsViewActionList.GetSortedActionItems"]/*' /> 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
 
            items.Add(new DesignerActionMethodItem(this,
                                                                 "EditFields", 
                                                                 SR.GetString(SR.DetailsView_EditFieldsVerb),
                                                                 "Action",
                                                                 SR.GetString(SR.DetailsView_EditFieldsDesc)));
            items.Add(new DesignerActionMethodItem(this, 
                                                                 "AddNewField",
                                                                 SR.GetString(SR.DetailsView_AddNewFieldVerb), 
                                                                 "Action", 
                                                                 SR.GetString(SR.DetailsView_AddNewFieldDesc)));
            if (AllowMoveUp) { 
                items.Add(new DesignerActionMethodItem(this,
                                                                     "MoveFieldUp",
                                                                     SR.GetString(SR.DetailsView_MoveFieldUpVerb),
                                                                     "Action", 
                                                                     SR.GetString(SR.DetailsView_MoveFieldUpDesc)));
            } 
            if (AllowMoveDown) { 
                items.Add(new DesignerActionMethodItem(this,
                                                                     "MoveFieldDown", 
                                                                     SR.GetString(SR.DetailsView_MoveFieldDownVerb),
                                                                     "Action",
                                                                     SR.GetString(SR.DetailsView_MoveFieldDownDesc)));
            } 
            if (AllowRemoveField) {
                items.Add(new DesignerActionMethodItem(this, 
                                                                     "RemoveField", 
                                                                     SR.GetString(SR.DetailsView_RemoveFieldVerb),
                                                                     "Action", 
                                                                     SR.GetString(SR.DetailsView_RemoveFieldDesc)));
            }
            if (AllowPaging) {
                items.Add(new DesignerActionPropertyItem("EnablePaging", 
                                                                       SR.GetString(SR.DetailsView_EnablePaging),
                                                                       "Behavior", 
                                                                       SR.GetString(SR.DetailsView_EnablePagingDesc))); 
            }
            if (AllowInserting) { 
                items.Add(new DesignerActionPropertyItem("EnableInserting",
                                                                       SR.GetString(SR.DetailsView_EnableInserting),
                                                                       "Behavior",
                                                                       SR.GetString(SR.DetailsView_EnableInsertingDesc))); 
            }
            if (AllowEditing) { 
                items.Add(new DesignerActionPropertyItem("EnableEditing", 
                                                                       SR.GetString(SR.DetailsView_EnableEditing),
                                                                       "Behavior", 
                                                                       SR.GetString(SR.DetailsView_EnableEditingDesc)));
            }
            if (AllowDeleting) {
                items.Add(new DesignerActionPropertyItem("EnableDeleting", 
                                                                       SR.GetString(SR.DetailsView_EnableDeleting),
                                                                       "Behavior", 
                                                                       SR.GetString(SR.DetailsView_EnableDeletingDesc))); 
            }
            return items; 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
