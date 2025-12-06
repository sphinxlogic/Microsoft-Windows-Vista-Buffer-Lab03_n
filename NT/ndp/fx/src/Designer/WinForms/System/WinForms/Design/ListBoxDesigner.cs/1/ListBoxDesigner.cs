//------------------------------------------------------------------------------ 
// <copyright file="ListBoxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ListBoxDesigner..ctor()")] 

namespace System.Windows.Forms.Design {

    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics; 
    using System;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Drawing;
    using System.Collections;
 
    /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner"]/*' />
    /// <devdoc> 
    ///      This class handles all design time behavior for the list box class. 
    ///      It adds a sample item to the list box at design time.
    /// </devdoc> 
    internal class ListBoxDesigner : ControlDesigner {

        private DesignerActionListCollection _actionLists;
 
        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.Dispose"]/*' />
        /// <devdoc> 
        ///      Destroys this designer. 
        /// </devdoc>
        protected override void Dispose(bool disposing) { 

            if (disposing) {
                // Now, hook the component rename event so we can update the text in the
                // list box. 
                //
                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                if (cs != null) { 
                    cs.ComponentRename -= new ComponentRenameEventHandler(this.OnComponentRename);
                    cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                }
            }
            base.Dispose(disposing);
        } 

        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.Initialize"]/*' /> 
        /// <devdoc> 
        ///     Called by the host when we're first initialized.
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            base.Initialize(component);

            AutoResizeHandles = true; 

            // Now, hook the component rename event so we can update the text in the 
            // list box. 
            //
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
            if (cs != null) {
                cs.ComponentRename += new ComponentRenameEventHandler(this.OnComponentRename);
                cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
            } 

        } 
 
        public override void InitializeNewComponent(IDictionary defaultValues) {
            base.InitializeNewComponent(defaultValues); 

            // in Whidbey, formattingEnabled is true
            ((ListBox) this.Component).FormattingEnabled = true;
 
            // VSWhidbey 497239 - Setting FormattingEnabled clears the text we set in
            // OnCreateHandle so let's set it here again. We need to keep setting the text in 
            // OnCreateHandle, otherwise we introduce VSWhidbey 498162. 
            PropertyDescriptor nameProp = TypeDescriptor.GetProperties(Component)["Name"];
            if (nameProp != null) { 
                UpdateControlName(nameProp.GetValue(Component).ToString());
            }

        } 

        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.OnComponentRename"]/*' /> 
        /// <devdoc> 
        ///      Raised when a component's name changes.  Here we update the contents of the list box
        ///      if we are displaying the component's name in it. 
        /// </devdoc>
        private void OnComponentRename(object sender, ComponentRenameEventArgs e) {
            if (e.Component == Component) {
                UpdateControlName(e.NewName); 
            }
        } 
 
        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.OnComponentChanged"]/*' />
        /// <devdoc> 
        ///      Raised when ComponentChanges. We listen to this to check if the "Items" propertychanged.
        ///      and if so .. then update the Text within the ListBox.
        /// </devdoc>
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) { 
            if (e.Component == Component && e.Member != null && e.Member.Name == "Items") {
                PropertyDescriptor nameProp = TypeDescriptor.GetProperties(Component)["Name"]; 
                if (nameProp != null) { 
                    UpdateControlName(nameProp.GetValue(Component).ToString());
                } 
            }
        }

        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.OnCreateHandle"]/*' /> 
        /// <devdoc>
        ///      This is called immediately after the control handle has been created. 
        /// </devdoc> 
        protected override void OnCreateHandle() {
            base.OnCreateHandle(); 
            PropertyDescriptor nameProp = TypeDescriptor.GetProperties(Component)["Name"];
            if (nameProp != null) {
                UpdateControlName(nameProp.GetValue(Component).ToString());
            } 
        }
 
        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.UpdateControlName"]/*' /> 
        /// <devdoc>
        ///      Updates the name being displayed on this control.  This will do nothing if 
        ///      the control has items in it.
        /// </devdoc>
        private void UpdateControlName(string name) {
            ListBox lb = (ListBox)Control; 
            if (lb.IsHandleCreated && lb.Items.Count == 0) {
                NativeMethods.SendMessage(lb.Handle, NativeMethods.LB_RESETCONTENT, 0, 0); 
                NativeMethods.SendMessage(lb.Handle, NativeMethods.LB_ADDSTRING, 0, name); 
            }
        } 


        public override DesignerActionListCollection ActionLists {
            get { 
                if (_actionLists == null) {
                    _actionLists = new DesignerActionListCollection(); 
                    if (this.Component is CheckedListBox) { 
                        _actionLists.Add(new ListControlUnboundActionList(this));
                    } else { 
                        _actionLists.Add(new ListControlBoundActionList(this));
                    }
                }
                return _actionLists; 
            }
        } 
    } 

    internal class ListControlUnboundActionList : DesignerActionList { 
        private ComponentDesigner _designer;

        public ListControlUnboundActionList(ComponentDesigner designer) : base(designer.Component) {
            _designer = designer; 
        }
 
        public void InvokeItemsDialog() { 
            EditorServiceContext.EditValue(_designer, Component, "Items");
        } 

        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection returnItems = new DesignerActionItemCollection();
            returnItems.Add(new DesignerActionMethodItem(this, "InvokeItemsDialog", 
                SR.GetString(SR.ListControlUnboundActionListEditItemsDisplayName),
                SR.GetString(SR.ItemsCategoryName), 
                SR.GetString(SR.ListControlUnboundActionListEditItemsDescription), true)); 
            return returnItems;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ListBoxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ListBoxDesigner..ctor()")] 

namespace System.Windows.Forms.Design {

    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics; 
    using System;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Drawing;
    using System.Collections;
 
    /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner"]/*' />
    /// <devdoc> 
    ///      This class handles all design time behavior for the list box class. 
    ///      It adds a sample item to the list box at design time.
    /// </devdoc> 
    internal class ListBoxDesigner : ControlDesigner {

        private DesignerActionListCollection _actionLists;
 
        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.Dispose"]/*' />
        /// <devdoc> 
        ///      Destroys this designer. 
        /// </devdoc>
        protected override void Dispose(bool disposing) { 

            if (disposing) {
                // Now, hook the component rename event so we can update the text in the
                // list box. 
                //
                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                if (cs != null) { 
                    cs.ComponentRename -= new ComponentRenameEventHandler(this.OnComponentRename);
                    cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                }
            }
            base.Dispose(disposing);
        } 

        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.Initialize"]/*' /> 
        /// <devdoc> 
        ///     Called by the host when we're first initialized.
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            base.Initialize(component);

            AutoResizeHandles = true; 

            // Now, hook the component rename event so we can update the text in the 
            // list box. 
            //
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
            if (cs != null) {
                cs.ComponentRename += new ComponentRenameEventHandler(this.OnComponentRename);
                cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
            } 

        } 
 
        public override void InitializeNewComponent(IDictionary defaultValues) {
            base.InitializeNewComponent(defaultValues); 

            // in Whidbey, formattingEnabled is true
            ((ListBox) this.Component).FormattingEnabled = true;
 
            // VSWhidbey 497239 - Setting FormattingEnabled clears the text we set in
            // OnCreateHandle so let's set it here again. We need to keep setting the text in 
            // OnCreateHandle, otherwise we introduce VSWhidbey 498162. 
            PropertyDescriptor nameProp = TypeDescriptor.GetProperties(Component)["Name"];
            if (nameProp != null) { 
                UpdateControlName(nameProp.GetValue(Component).ToString());
            }

        } 

        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.OnComponentRename"]/*' /> 
        /// <devdoc> 
        ///      Raised when a component's name changes.  Here we update the contents of the list box
        ///      if we are displaying the component's name in it. 
        /// </devdoc>
        private void OnComponentRename(object sender, ComponentRenameEventArgs e) {
            if (e.Component == Component) {
                UpdateControlName(e.NewName); 
            }
        } 
 
        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.OnComponentChanged"]/*' />
        /// <devdoc> 
        ///      Raised when ComponentChanges. We listen to this to check if the "Items" propertychanged.
        ///      and if so .. then update the Text within the ListBox.
        /// </devdoc>
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) { 
            if (e.Component == Component && e.Member != null && e.Member.Name == "Items") {
                PropertyDescriptor nameProp = TypeDescriptor.GetProperties(Component)["Name"]; 
                if (nameProp != null) { 
                    UpdateControlName(nameProp.GetValue(Component).ToString());
                } 
            }
        }

        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.OnCreateHandle"]/*' /> 
        /// <devdoc>
        ///      This is called immediately after the control handle has been created. 
        /// </devdoc> 
        protected override void OnCreateHandle() {
            base.OnCreateHandle(); 
            PropertyDescriptor nameProp = TypeDescriptor.GetProperties(Component)["Name"];
            if (nameProp != null) {
                UpdateControlName(nameProp.GetValue(Component).ToString());
            } 
        }
 
        /// <include file='doc\ListBoxDesigner.uex' path='docs/doc[@for="ListBoxDesigner.UpdateControlName"]/*' /> 
        /// <devdoc>
        ///      Updates the name being displayed on this control.  This will do nothing if 
        ///      the control has items in it.
        /// </devdoc>
        private void UpdateControlName(string name) {
            ListBox lb = (ListBox)Control; 
            if (lb.IsHandleCreated && lb.Items.Count == 0) {
                NativeMethods.SendMessage(lb.Handle, NativeMethods.LB_RESETCONTENT, 0, 0); 
                NativeMethods.SendMessage(lb.Handle, NativeMethods.LB_ADDSTRING, 0, name); 
            }
        } 


        public override DesignerActionListCollection ActionLists {
            get { 
                if (_actionLists == null) {
                    _actionLists = new DesignerActionListCollection(); 
                    if (this.Component is CheckedListBox) { 
                        _actionLists.Add(new ListControlUnboundActionList(this));
                    } else { 
                        _actionLists.Add(new ListControlBoundActionList(this));
                    }
                }
                return _actionLists; 
            }
        } 
    } 

    internal class ListControlUnboundActionList : DesignerActionList { 
        private ComponentDesigner _designer;

        public ListControlUnboundActionList(ComponentDesigner designer) : base(designer.Component) {
            _designer = designer; 
        }
 
        public void InvokeItemsDialog() { 
            EditorServiceContext.EditValue(_designer, Component, "Items");
        } 

        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection returnItems = new DesignerActionItemCollection();
            returnItems.Add(new DesignerActionMethodItem(this, "InvokeItemsDialog", 
                SR.GetString(SR.ListControlUnboundActionListEditItemsDisplayName),
                SR.GetString(SR.ItemsCategoryName), 
                SR.GetString(SR.ListControlUnboundActionListEditItemsDescription), true)); 
            return returnItems;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
