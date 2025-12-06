//------------------------------------------------------------------------------ 
// <copyright file="TrayIconDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.NotifyIconDesigner..ctor()")] 
namespace System.Windows.Forms.Design {

    using Microsoft.Win32;
    using System; 
    using System.Design;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Windows.Forms;

    /// <include file='doc\TrayIconDesigner.uex' path='docs/doc[@for="NotifyIconDesigner"]/*' />
    /// <devdoc> 
    ///      This is the designer for OpenFileDialog components.
    /// </devdoc> 
    internal class NotifyIconDesigner : ComponentDesigner { 

        private DesignerActionListCollection _actionLists; 

        /// <devdoc>
        /// </devdoc>
        public override void InitializeNewComponent(IDictionary defaultValues) { 
            base.InitializeNewComponent(defaultValues);
            NotifyIcon icon = (NotifyIcon)Component; 
            icon.Visible = true; 
        }
 

        public override DesignerActionListCollection ActionLists {
            get {
                if (_actionLists == null) { 
                    _actionLists = new DesignerActionListCollection();
                    _actionLists.Add(new NotifyIconActionList(this)); 
                } 
                return _actionLists;
            } 
        }
    }

    internal class NotifyIconActionList : DesignerActionList { 
        private NotifyIconDesigner _designer;
        public NotifyIconActionList(NotifyIconDesigner designer) : base(designer.Component) { 
            _designer = designer; 
        }
 
        public void ChooseIcon() {
            EditorServiceContext.EditValue(_designer, Component, "Icon");
        }
 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            items.Add(new DesignerActionMethodItem(this, "ChooseIcon", SR.GetString(SR.ChooseIconDisplayName), true)); 
            return items;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TrayIconDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.NotifyIconDesigner..ctor()")] 
namespace System.Windows.Forms.Design {

    using Microsoft.Win32;
    using System; 
    using System.Design;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Windows.Forms;

    /// <include file='doc\TrayIconDesigner.uex' path='docs/doc[@for="NotifyIconDesigner"]/*' />
    /// <devdoc> 
    ///      This is the designer for OpenFileDialog components.
    /// </devdoc> 
    internal class NotifyIconDesigner : ComponentDesigner { 

        private DesignerActionListCollection _actionLists; 

        /// <devdoc>
        /// </devdoc>
        public override void InitializeNewComponent(IDictionary defaultValues) { 
            base.InitializeNewComponent(defaultValues);
            NotifyIcon icon = (NotifyIcon)Component; 
            icon.Visible = true; 
        }
 

        public override DesignerActionListCollection ActionLists {
            get {
                if (_actionLists == null) { 
                    _actionLists = new DesignerActionListCollection();
                    _actionLists.Add(new NotifyIconActionList(this)); 
                } 
                return _actionLists;
            } 
        }
    }

    internal class NotifyIconActionList : DesignerActionList { 
        private NotifyIconDesigner _designer;
        public NotifyIconActionList(NotifyIconDesigner designer) : base(designer.Component) { 
            _designer = designer; 
        }
 
        public void ChooseIcon() {
            EditorServiceContext.EditValue(_designer, Component, "Icon");
        }
 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            items.Add(new DesignerActionMethodItem(this, "ChooseIcon", SR.GetString(SR.ChooseIconDisplayName), true)); 
            return items;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
