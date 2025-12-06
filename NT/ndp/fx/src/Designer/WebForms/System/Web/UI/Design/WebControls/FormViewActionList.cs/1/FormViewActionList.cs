//------------------------------------------------------------------------------ 
// <copyright file="FormViewActionList.cs" company="Microsoft">
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

    /// <include file='doc\FormViewActionList.uex' path='docs/doc[@for="FormViewActionList"]/*' /> 
    internal class FormViewActionList : DesignerActionList {
        private FormViewDesigner _formViewDesigner;

        private bool _allowPaging; 

        /// <include file='doc\FormViewActionList.uex' path='docs/doc[@for="FormViewActionList.FormViewActionList"]/*' /> 
        public FormViewActionList(FormViewDesigner formViewDesigner) : base(formViewDesigner.Component) { 
            _formViewDesigner = formViewDesigner;
        } 

        /// <summary>
        /// Lets the FormView designer specify whether the Page action should be visible/enabled
        /// </summary> 
        internal bool AllowPaging {
            get { 
                return _allowPaging; 
            }
            set { 
                _allowPaging = value;
            }
        }
 
        public override bool AutoShow {
            get { 
                return true; 
            }
            set { 
            }
        }

        /// <summary> 
        /// Property used by chrome to display the page checkbox.  Called through reflection.
        /// </summary> 
        public bool EnablePaging { 
            get {
                return _formViewDesigner.EnablePaging; 
            }
            set {
                _formViewDesigner.EnablePaging = value;
            } 
        }
 
        /// <include file='doc\FormViewActionList.uex' path='docs/doc[@for="FormViewActionList.GetSortedActionItems"]/*' /> 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 

            if (AllowPaging) {
                items.Add(new DesignerActionPropertyItem("EnablePaging",
                                                                       SR.GetString(SR.FormView_EnablePaging), 
                                                                       "Behavior",
                                                                       SR.GetString(SR.FormView_EnablePagingDesc))); 
            } 
            return items;
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="FormViewActionList.cs" company="Microsoft">
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

    /// <include file='doc\FormViewActionList.uex' path='docs/doc[@for="FormViewActionList"]/*' /> 
    internal class FormViewActionList : DesignerActionList {
        private FormViewDesigner _formViewDesigner;

        private bool _allowPaging; 

        /// <include file='doc\FormViewActionList.uex' path='docs/doc[@for="FormViewActionList.FormViewActionList"]/*' /> 
        public FormViewActionList(FormViewDesigner formViewDesigner) : base(formViewDesigner.Component) { 
            _formViewDesigner = formViewDesigner;
        } 

        /// <summary>
        /// Lets the FormView designer specify whether the Page action should be visible/enabled
        /// </summary> 
        internal bool AllowPaging {
            get { 
                return _allowPaging; 
            }
            set { 
                _allowPaging = value;
            }
        }
 
        public override bool AutoShow {
            get { 
                return true; 
            }
            set { 
            }
        }

        /// <summary> 
        /// Property used by chrome to display the page checkbox.  Called through reflection.
        /// </summary> 
        public bool EnablePaging { 
            get {
                return _formViewDesigner.EnablePaging; 
            }
            set {
                _formViewDesigner.EnablePaging = value;
            } 
        }
 
        /// <include file='doc\FormViewActionList.uex' path='docs/doc[@for="FormViewActionList.GetSortedActionItems"]/*' /> 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 

            if (AllowPaging) {
                items.Add(new DesignerActionPropertyItem("EnablePaging",
                                                                       SR.GetString(SR.FormView_EnablePaging), 
                                                                       "Behavior",
                                                                       SR.GetString(SR.FormView_EnablePagingDesc))); 
            } 
            return items;
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
