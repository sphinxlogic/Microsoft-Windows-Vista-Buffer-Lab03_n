//------------------------------------------------------------------------------ 
// <copyright file="BindingNavigatorDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.BindingNavigatorDesigner..ctor()")] 
 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.Design;
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization; 
    using System.Diagnostics;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design.Behavior;
    using System.Reflection;

    /// <include file='doc\BindingNavigatorDesigner.uex' path='docs/doc[@for="BindingNavigatorDesigner"]/*' /> 
    /// <devdoc>
    ///     Designer for the BindingNavigator class. 
    /// </devdoc> 
    internal class BindingNavigatorDesigner : ToolStripDesigner {
 
        static string[] itemNames = new string[] { "MovePreviousItem", "MoveFirstItem", "MoveNextItem", "MoveLastItem", "AddNewItem",
                                                "DeleteItem", "PositionItem", "CountItem"};

        /// <summary> 
        ///     Initialize this designer.
        /// </summary> 
        public override void Initialize(IComponent component) { 
            base.Initialize(component);
 
            IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
            if (componentChangeSvc != null) {
                componentChangeSvc.ComponentRemoved += new ComponentEventHandler(ComponentChangeSvc_ComponentRemoved);
                componentChangeSvc.ComponentChanged += new ComponentChangedEventHandler(ComponentChangeSvc_ComponentChanged); 
            }
        } 
 
        /// <summary>
        ///     Dispose this designer. 
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService)); 
                if (componentChangeSvc != null) {
                    componentChangeSvc.ComponentRemoved -= new ComponentEventHandler(ComponentChangeSvc_ComponentRemoved); 
                    componentChangeSvc.ComponentChanged -= new ComponentChangedEventHandler(ComponentChangeSvc_ComponentChanged); 
                }
            } 

            base.Dispose(disposing);
        }
 
        /// <summary>
        /// When a new BindingNavigator is created, we pre-propulate it with a set of standard items. 
        /// The items are created by the control's own AddStandardItems() virtual method. After the 
        /// items have been created, we have to properly site them, so that they designer knows
        /// about them and will include them in its code spit. 
        /// </summary>
        public override void InitializeNewComponent(IDictionary defaultValues) {
            base.InitializeNewComponent(defaultValues);
 
            BindingNavigator dn = (BindingNavigator) Component;
            IDesignerHost host = (IDesignerHost) this.Component.Site.GetService(typeof(IDesignerHost)); 
 
            try {
                _autoAddNewItems = false;    // Temporarily suppress "new items go to the selected strip" behavior 
                dn.SuspendLayout();          // Turn off layout while adding items
                dn.AddStandardItems();       // Let the control add its standard items (user overridable)
                SiteItems(host, dn.Items);   // Recursively site and name all the items on the strip
                RaiseItemsChanged();         // Make designer Undo engine aware of the newly added and sited items 
                dn.ResumeLayout();           // Allow strip to lay out now
                dn.ShowItemToolTips = true;  // Non-default property setting for ShowToolTips 
            } 
            finally {
                _autoAddNewItems = true; 
            }
        }

        /// <summary> 
        ///     Raise design time events to signal that the strip's Items collection has changed. This
        ///     ensures the items added during component initialization are picked up by the Undo engine. 
        /// </summary> 
        private void RaiseItemsChanged() {
            BindingNavigator dn = (BindingNavigator) Component; 
            IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService));

            if (componentChangeSvc != null) {
                MemberDescriptor itemsProp = TypeDescriptor.GetProperties(dn)["Items"]; 
                componentChangeSvc.OnComponentChanging(dn, itemsProp);
                componentChangeSvc.OnComponentChanged(dn, itemsProp, null, null); 
 
                foreach (string itemName in itemNames) {
                    PropertyDescriptor prop = TypeDescriptor.GetProperties(dn)[itemName]; 

                    if (prop != null) {
                        componentChangeSvc.OnComponentChanging(dn, prop);
                        componentChangeSvc.OnComponentChanged(dn, prop, null, null); 
                    }
                } 
            } 
        }
 
        /// <summary>
        ///     Site a tool strip item, and any items contained within it.
        /// </summary>
        private void SiteItem(IDesignerHost host, ToolStripItem item) { 
            // Skip any controls added for design-time use only
            if (item is DesignerToolStripControlHost) { 
                return; 
            }
 
            // Site the item in the container, giving it a unique site name based on its initial Name property
            host.Container.Add(item, DesignerUtils.GetUniqueSiteName(host, item.Name));

            // Update the item's Name property to reflect the unique site name that it was actually given 
            item.Name = item.Site.Name;
 
            // Site any sub-items of this item 
            ToolStripDropDownItem dropDownItem = item as ToolStripDropDownItem;
            if (dropDownItem != null && dropDownItem.HasDropDownItems) { 
                SiteItems(host, dropDownItem.DropDownItems);
            }
        }
 
        /// <summary>
        ///     Site a collection of tool strip items. 
        /// </summary> 
        private void SiteItems(IDesignerHost host, ToolStripItemCollection items) {
            foreach (ToolStripItem item in items) { 
                SiteItem(host, item);
            }
        }
 
        /// <summary>
        ///     When a tool strip item is removed from this BindingNavigator tool strip, check to see whether 
        ///     its currently being referenced by any of the BindingNavigator's "magic item" properties. If 
        ///     so, clear that reference so that the strip is not hanging onto deleted items.
        /// </summary> 
        private void ComponentChangeSvc_ComponentRemoved(object sender, ComponentEventArgs e) {
            ToolStripItem item = e.Component as ToolStripItem;

            if (item != null) { 
                BindingNavigator dn = (BindingNavigator) Component;
 
                if (item == dn.MoveFirstItem) { 
                    dn.MoveFirstItem = null;
                } 
                else if (item == dn.MovePreviousItem) {
                    dn.MovePreviousItem = null;
                }
                else if (item == dn.MoveNextItem) { 
                    dn.MoveNextItem = null;
                } 
                else if (item == dn.MoveLastItem) { 
                    dn.MoveLastItem = null;
                } 
                else if (item == dn.PositionItem) {
                    dn.PositionItem = null;
                }
                else if (item == dn.CountItem) { 
                    dn.CountItem = null;
                } 
                else if (item == dn.AddNewItem) { 
                    dn.AddNewItem = null;
                } 
                else if (item == dn.DeleteItem) {
                    dn.DeleteItem = null;
                }
            } 
        }
 
        /// <summary> 
        /// </summary>
        private void ComponentChangeSvc_ComponentChanged(object sender, ComponentChangedEventArgs e) { 
            BindingNavigator dn = (BindingNavigator) Component;

            if (e.Component != null && e.Component == dn.CountItem && e.Member != null && e.Member.Name == "Text") {
                dn.CountItemFormat = dn.CountItem.Text; 
            }
        } 
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BindingNavigatorDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.BindingNavigatorDesigner..ctor()")] 
 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.Design;
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization; 
    using System.Diagnostics;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design.Behavior;
    using System.Reflection;

    /// <include file='doc\BindingNavigatorDesigner.uex' path='docs/doc[@for="BindingNavigatorDesigner"]/*' /> 
    /// <devdoc>
    ///     Designer for the BindingNavigator class. 
    /// </devdoc> 
    internal class BindingNavigatorDesigner : ToolStripDesigner {
 
        static string[] itemNames = new string[] { "MovePreviousItem", "MoveFirstItem", "MoveNextItem", "MoveLastItem", "AddNewItem",
                                                "DeleteItem", "PositionItem", "CountItem"};

        /// <summary> 
        ///     Initialize this designer.
        /// </summary> 
        public override void Initialize(IComponent component) { 
            base.Initialize(component);
 
            IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
            if (componentChangeSvc != null) {
                componentChangeSvc.ComponentRemoved += new ComponentEventHandler(ComponentChangeSvc_ComponentRemoved);
                componentChangeSvc.ComponentChanged += new ComponentChangedEventHandler(ComponentChangeSvc_ComponentChanged); 
            }
        } 
 
        /// <summary>
        ///     Dispose this designer. 
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService)); 
                if (componentChangeSvc != null) {
                    componentChangeSvc.ComponentRemoved -= new ComponentEventHandler(ComponentChangeSvc_ComponentRemoved); 
                    componentChangeSvc.ComponentChanged -= new ComponentChangedEventHandler(ComponentChangeSvc_ComponentChanged); 
                }
            } 

            base.Dispose(disposing);
        }
 
        /// <summary>
        /// When a new BindingNavigator is created, we pre-propulate it with a set of standard items. 
        /// The items are created by the control's own AddStandardItems() virtual method. After the 
        /// items have been created, we have to properly site them, so that they designer knows
        /// about them and will include them in its code spit. 
        /// </summary>
        public override void InitializeNewComponent(IDictionary defaultValues) {
            base.InitializeNewComponent(defaultValues);
 
            BindingNavigator dn = (BindingNavigator) Component;
            IDesignerHost host = (IDesignerHost) this.Component.Site.GetService(typeof(IDesignerHost)); 
 
            try {
                _autoAddNewItems = false;    // Temporarily suppress "new items go to the selected strip" behavior 
                dn.SuspendLayout();          // Turn off layout while adding items
                dn.AddStandardItems();       // Let the control add its standard items (user overridable)
                SiteItems(host, dn.Items);   // Recursively site and name all the items on the strip
                RaiseItemsChanged();         // Make designer Undo engine aware of the newly added and sited items 
                dn.ResumeLayout();           // Allow strip to lay out now
                dn.ShowItemToolTips = true;  // Non-default property setting for ShowToolTips 
            } 
            finally {
                _autoAddNewItems = true; 
            }
        }

        /// <summary> 
        ///     Raise design time events to signal that the strip's Items collection has changed. This
        ///     ensures the items added during component initialization are picked up by the Undo engine. 
        /// </summary> 
        private void RaiseItemsChanged() {
            BindingNavigator dn = (BindingNavigator) Component; 
            IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService));

            if (componentChangeSvc != null) {
                MemberDescriptor itemsProp = TypeDescriptor.GetProperties(dn)["Items"]; 
                componentChangeSvc.OnComponentChanging(dn, itemsProp);
                componentChangeSvc.OnComponentChanged(dn, itemsProp, null, null); 
 
                foreach (string itemName in itemNames) {
                    PropertyDescriptor prop = TypeDescriptor.GetProperties(dn)[itemName]; 

                    if (prop != null) {
                        componentChangeSvc.OnComponentChanging(dn, prop);
                        componentChangeSvc.OnComponentChanged(dn, prop, null, null); 
                    }
                } 
            } 
        }
 
        /// <summary>
        ///     Site a tool strip item, and any items contained within it.
        /// </summary>
        private void SiteItem(IDesignerHost host, ToolStripItem item) { 
            // Skip any controls added for design-time use only
            if (item is DesignerToolStripControlHost) { 
                return; 
            }
 
            // Site the item in the container, giving it a unique site name based on its initial Name property
            host.Container.Add(item, DesignerUtils.GetUniqueSiteName(host, item.Name));

            // Update the item's Name property to reflect the unique site name that it was actually given 
            item.Name = item.Site.Name;
 
            // Site any sub-items of this item 
            ToolStripDropDownItem dropDownItem = item as ToolStripDropDownItem;
            if (dropDownItem != null && dropDownItem.HasDropDownItems) { 
                SiteItems(host, dropDownItem.DropDownItems);
            }
        }
 
        /// <summary>
        ///     Site a collection of tool strip items. 
        /// </summary> 
        private void SiteItems(IDesignerHost host, ToolStripItemCollection items) {
            foreach (ToolStripItem item in items) { 
                SiteItem(host, item);
            }
        }
 
        /// <summary>
        ///     When a tool strip item is removed from this BindingNavigator tool strip, check to see whether 
        ///     its currently being referenced by any of the BindingNavigator's "magic item" properties. If 
        ///     so, clear that reference so that the strip is not hanging onto deleted items.
        /// </summary> 
        private void ComponentChangeSvc_ComponentRemoved(object sender, ComponentEventArgs e) {
            ToolStripItem item = e.Component as ToolStripItem;

            if (item != null) { 
                BindingNavigator dn = (BindingNavigator) Component;
 
                if (item == dn.MoveFirstItem) { 
                    dn.MoveFirstItem = null;
                } 
                else if (item == dn.MovePreviousItem) {
                    dn.MovePreviousItem = null;
                }
                else if (item == dn.MoveNextItem) { 
                    dn.MoveNextItem = null;
                } 
                else if (item == dn.MoveLastItem) { 
                    dn.MoveLastItem = null;
                } 
                else if (item == dn.PositionItem) {
                    dn.PositionItem = null;
                }
                else if (item == dn.CountItem) { 
                    dn.CountItem = null;
                } 
                else if (item == dn.AddNewItem) { 
                    dn.AddNewItem = null;
                } 
                else if (item == dn.DeleteItem) {
                    dn.DeleteItem = null;
                }
            } 
        }
 
        /// <summary> 
        /// </summary>
        private void ComponentChangeSvc_ComponentChanged(object sender, ComponentChangedEventArgs e) { 
            BindingNavigator dn = (BindingNavigator) Component;

            if (e.Component != null && e.Component == dn.CountItem && e.Member != null && e.Member.Name == "Text") {
                dn.CountItemFormat = dn.CountItem.Text; 
            }
        } 
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
