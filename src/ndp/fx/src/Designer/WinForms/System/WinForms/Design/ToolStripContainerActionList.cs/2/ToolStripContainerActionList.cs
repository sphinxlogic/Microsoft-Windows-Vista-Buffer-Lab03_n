//------------------------------------------------------------------------------ 
// <copyright file="ToolStripContainerActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System.Windows.Forms.Design.Behavior; 

    /// <devdoc> 
    ///     Describes the list of actions that can be performed for the ToolStripContainer control from the
    ///     chrome pannel.
    /// </devdoc>
    internal class ToolStripContainerActionList : DesignerActionList 
    {
        ToolStripContainer container; 
        IServiceProvider provider = null; 
        IDesignerHost host = null;
 
        /// <devdoc>
        ///     ToolStripcontainer ActionList.
        /// </devdoc>
        public ToolStripContainerActionList(ToolStripContainer control) : base(control) 
        {
            this.container = control; 
            provider = container.Site; 
            host = provider.GetService (typeof(IDesignerHost)) as IDesignerHost;
        } 

        //helper function to get the property on the comp
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private object GetProperty(Component comp, string propertyName) 
        {
            PropertyDescriptor getProperty = TypeDescriptor.GetProperties(comp)[propertyName]; 
            Debug.Assert( getProperty != null, "Could not find given property in control."); 
            if( getProperty != null )
            { 
               return getProperty.GetValue(comp);
            }
            return null;
        } 

        //helper function to change the property on the comp 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private void ChangeProperty(Component comp, string propertyName, object value)
        { 
            if (host != null)
            {
                ToolStripPanel panel = comp as ToolStripPanel;
                ToolStripPanelDesigner panelDesigner = host.GetDesigner(comp) as ToolStripPanelDesigner; 

                if (propertyName.Equals("Visible")) 
                { 
                    foreach(Control c in panel.Controls)
                    { 
                        PropertyDescriptor visibleProperty = TypeDescriptor.GetProperties(c)["Visible"];
                        Debug.Assert(visibleProperty != null, "Could not find given property in control." );
                        if(visibleProperty != null )
                        { 
                           visibleProperty.SetValue(c, value);
                        } 
                    } 

                    if (!((bool)value)) 
                    {
                        if (panel != null)
                        {
                            panel.Padding = new Padding(0); 
                        }
                        if (panelDesigner != null) 
                        { 
                            if(panelDesigner.ToolStripPanelSelectorGlyph != null)
                            { 
                                panelDesigner.ToolStripPanelSelectorGlyph.IsExpanded = false;
                            }

                        } 
                    }
                } 
 
                PropertyDescriptor changingProperty = TypeDescriptor.GetProperties(comp)[propertyName];
                Debug.Assert( changingProperty != null, "Could not find given property in control." ); 
                if( changingProperty != null )
                {
                   changingProperty.SetValue(comp, value);
                } 

                //Reset the Glyphs. 
                SelectionManager selMgr = (SelectionManager)provider.GetService(typeof(SelectionManager)); 
                if (selMgr != null)
                { 
                    selMgr.Refresh();
                }

                // Invlidate the Window... 
                if (panelDesigner != null)
                { 
                    panelDesigner.InvalidateGlyph(); 
                }
            } 
        }

        /// <devdoc>
        ///     Checks if the ToolStrip Container is DOCK FILLED 
        /// </devdoc>
        private bool IsDockFilled 
        { 
            get
            { 
                PropertyDescriptor dockProp = TypeDescriptor.GetProperties(container)["Dock"];
                if (dockProp != null && ((DockStyle)dockProp.GetValue(container)) != DockStyle.Fill) {
                    return false;
                } 
                return true;
            } 
        } 

        /// <devdoc> 
        ///     Checks if the ToolStripContainer is a child control of the designerHost's rootComponent
        /// </devdoc>
        private bool ProvideReparent
        { 
            get
            { 
 
                if (host != null)
                { 
                    Control root = host.RootComponent as Control;
                    // Reparent the Controls only if the ToolStripContainer is a child of the RootComponent...
                    if (root != null && container.Parent == root && IsDockFilled && root.Controls.Count > 1)
                    { 
                        return true;
                    } 
                } 
                return false;
            } 
        }

        /// <devdoc>
        ///     Sets the Dock.. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public void SetDockToForm() 
        {
            if (host != null) 
            {
                Control root = host.RootComponent as Control;
                //change the Parent only if its not parented to the form.
                if (root != null && container.Parent != root) 
                {
                    root.Controls.Add(container); 
                } 
                //set the dock prop to DockStyle.Fill
                if (!IsDockFilled) 
                {
                    PropertyDescriptor dockProp = TypeDescriptor.GetProperties(container)["Dock"];
                    if (dockProp != null)
                    { 
                        dockProp.SetValue(container, DockStyle.Fill);
                    } 
                } 
            }
        } 

        /// <devdoc>
        ///     Reparent the controls on the form.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public void ReparentControls() 
        {
            if (host != null) 
            {
                Control root = host.RootComponent as Control;
                // Reparent the Controls only if the ToolStripContainer is a child of the RootComponent...
                if (root != null && container.Parent == root && root.Controls.Count > 1) 
                {
                    Control newParent = container.ContentPanel; 
                    PropertyDescriptor autoScrollProp = TypeDescriptor.GetProperties(newParent)["AutoScroll"]; 
                    if (autoScrollProp != null)
                    { 
                        autoScrollProp.SetValue(newParent, true);
                    }

                    // create a transaction so this happens as an atomic unit. 
                    DesignerTransaction changeParent = host.CreateTransaction("Reparent Transaction");
 
                    try 
                    {
                        Control[] childControls = new Control[root.Controls.Count]; 
                        root.Controls.CopyTo(childControls, 0);

                        foreach(Control c in childControls)
                        { 
                            if (c == container || c is MdiClient)
                            { 
                                continue; 
                            }
 
                            // We should not reparent inherited Controls...
                            InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(c)[typeof(InheritanceAttribute)];
                            if (ia == null || ia.InheritanceLevel == InheritanceLevel.InheritedReadOnly)
                            { 
                                continue;
                            } 
 
                            IComponentChangeService changeSvc = provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
 
                            if (c is ToolStrip)
                            {
                                newParent = GetParent(c);
                            } 
                            else
                            { 
                                newParent = container.ContentPanel; 
                            }
 
                            PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(newParent)["Controls"];
                            Control oldParent = c.Parent;

                            if (oldParent != null) { 
                                if (changeSvc != null) {
                                    changeSvc.OnComponentChanging(oldParent, controlsProp); 
                                } 
                                //remove control from the old parent
                                oldParent.Controls.Remove(c); 
                            }

                            if (changeSvc != null) {
                                changeSvc.OnComponentChanging(newParent, controlsProp); 
                            }
                            //finally add & relocate the control with the new parent 
                            newParent.Controls.Add(c); 

                            //fire our comp changed events 
                            if (changeSvc != null && oldParent != null) {
                                changeSvc.OnComponentChanged(oldParent, controlsProp, null, null);
                            }
 
                            // fire comp changed on the newParent
                            if (changeSvc != null) { 
                                changeSvc.OnComponentChanged(newParent, controlsProp, null, null); 
                            }
                        } 
                    }
                    catch {
                        if (changeParent != null)
                        { 
                            changeParent.Cancel();
                            changeParent = null; 
                        } 

                    } 
                    finally {
                        if (changeParent != null)
                        {
                            changeParent.Commit(); 
                            changeParent = null;
                        } 
 
                        //Set the Selection on the new Parent ... so that the selection is restored to the new item,
                        ISelectionService selSvc = provider.GetService(typeof(ISelectionService)) as ISelectionService; 
                        if (selSvc != null)
                        {
                            selSvc.SetSelectedComponents(new IComponent[] { newParent });
                        } 
                    }
                } 
 
            }
        } 

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private Control GetParent(Control c)
        { 
            Control newParent = container.ContentPanel;
            DockStyle dock = c.Dock; 
 
            foreach(Control panel in container.Controls)
            { 
                if (panel is ToolStripPanel)
                {
                    if (panel.Dock == dock)
                    { 
                        newParent = panel;
                        break; 
                    } 
                }
            } 
            return newParent;
        }

        /// <devdoc> 
        ///     Visibility of TopToolStripPanel.
        /// </devdoc> 
        public bool TopVisible 
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            get
            {
                return (bool)GetProperty(container, "TopToolStripPanelVisible");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set 
            { 
                if (value != TopVisible) {
                    ChangeProperty(container, "TopToolStripPanelVisible", (object)value); 
                }
            }
        }
 
        /// <devdoc>
        ///     Visibility of BottomToolStripPanel. 
        /// </devdoc> 
        public bool BottomVisible
        { 

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get
            { 
                return (bool)GetProperty(container, "BottomToolStripPanelVisible");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            set
            { 
                if (value != BottomVisible) {
                    ChangeProperty(container, "BottomToolStripPanelVisible", (object)value);
                }
            } 
        }
 
        /// <devdoc> 
        ///     Visibility of LeftToolStripPanel.
        /// </devdoc> 
        public bool LeftVisible
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get 
            {
                return (bool)GetProperty(container, "LeftToolStripPanelVisible"); 
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set 
            {
                if (value != LeftVisible) {
                    ChangeProperty(container, "LeftToolStripPanelVisible", (object)value);
                } 
            }
        } 
 
        /// <devdoc>
        ///     Visibility of RightToolStripPanel. 
        /// </devdoc>
        public bool RightVisible
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            get
            { 
                return (bool)GetProperty(container, "RightToolStripPanelVisible"); 
            }
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            set
            {
                if (value != RightVisible) {
                    ChangeProperty(container, "RightToolStripPanelVisible", (object)value); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Returns the control's action list items.
        /// </devdoc>
        public override DesignerActionItemCollection GetSortedActionItems()
        { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
 
            items.Add(new DesignerActionHeaderItem(SR.GetString(SR.ToolStripContainerActionList_Visible), SR.GetString(SR.ToolStripContainerActionList_Show))); 
            items.Add(new DesignerActionPropertyItem("TopVisible",
                                                     SR.GetString(SR.ToolStripContainerActionList_Top), 
                                                     SR.GetString(SR.ToolStripContainerActionList_Show),
                                                     SR.GetString(SR.ToolStripContainerActionList_TopDesc)));

            items.Add(new DesignerActionPropertyItem("BottomVisible", 
                                                     SR.GetString(SR.ToolStripContainerActionList_Bottom),
                                                     SR.GetString(SR.ToolStripContainerActionList_Show), 
                                                     SR.GetString(SR.ToolStripContainerActionList_BottomDesc))); 

            items.Add(new DesignerActionPropertyItem("LeftVisible", 
                                                    SR.GetString(SR.ToolStripContainerActionList_Left),
                                                    SR.GetString(SR.ToolStripContainerActionList_Show),
                                                    SR.GetString(SR.ToolStripContainerActionList_LeftDesc)));
 
            items.Add(new DesignerActionPropertyItem("RightVisible",
                                                    SR.GetString(SR.ToolStripContainerActionList_Right), 
                                                    SR.GetString(SR.ToolStripContainerActionList_Show), 
                                                    SR.GetString(SR.ToolStripContainerActionList_RightDesc)));
            if (!IsDockFilled) { 
                bool parentIsForm = true;
                if (host != null)
                {
                    Control userControl = host.RootComponent as UserControl; 
                    if (userControl != null)
                    { 
                        parentIsForm = false; 
                    }
                } 

                items.Add(new DesignerActionMethodItem(this, "SetDockToForm", parentIsForm ? SR.GetString(SR.DesignerShortcutDockInForm) : SR.GetString(SR.DesignerShortcutDockInUserControl)));
            }
            if (ProvideReparent) 
            {
                items.Add(new DesignerActionMethodItem(this, "ReparentControls", SR.GetString(SR.DesignerShortcutReparentControls))); 
            } 

            return items; 
        }

    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripContainerActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System.Windows.Forms.Design.Behavior; 

    /// <devdoc> 
    ///     Describes the list of actions that can be performed for the ToolStripContainer control from the
    ///     chrome pannel.
    /// </devdoc>
    internal class ToolStripContainerActionList : DesignerActionList 
    {
        ToolStripContainer container; 
        IServiceProvider provider = null; 
        IDesignerHost host = null;
 
        /// <devdoc>
        ///     ToolStripcontainer ActionList.
        /// </devdoc>
        public ToolStripContainerActionList(ToolStripContainer control) : base(control) 
        {
            this.container = control; 
            provider = container.Site; 
            host = provider.GetService (typeof(IDesignerHost)) as IDesignerHost;
        } 

        //helper function to get the property on the comp
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private object GetProperty(Component comp, string propertyName) 
        {
            PropertyDescriptor getProperty = TypeDescriptor.GetProperties(comp)[propertyName]; 
            Debug.Assert( getProperty != null, "Could not find given property in control."); 
            if( getProperty != null )
            { 
               return getProperty.GetValue(comp);
            }
            return null;
        } 

        //helper function to change the property on the comp 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private void ChangeProperty(Component comp, string propertyName, object value)
        { 
            if (host != null)
            {
                ToolStripPanel panel = comp as ToolStripPanel;
                ToolStripPanelDesigner panelDesigner = host.GetDesigner(comp) as ToolStripPanelDesigner; 

                if (propertyName.Equals("Visible")) 
                { 
                    foreach(Control c in panel.Controls)
                    { 
                        PropertyDescriptor visibleProperty = TypeDescriptor.GetProperties(c)["Visible"];
                        Debug.Assert(visibleProperty != null, "Could not find given property in control." );
                        if(visibleProperty != null )
                        { 
                           visibleProperty.SetValue(c, value);
                        } 
                    } 

                    if (!((bool)value)) 
                    {
                        if (panel != null)
                        {
                            panel.Padding = new Padding(0); 
                        }
                        if (panelDesigner != null) 
                        { 
                            if(panelDesigner.ToolStripPanelSelectorGlyph != null)
                            { 
                                panelDesigner.ToolStripPanelSelectorGlyph.IsExpanded = false;
                            }

                        } 
                    }
                } 
 
                PropertyDescriptor changingProperty = TypeDescriptor.GetProperties(comp)[propertyName];
                Debug.Assert( changingProperty != null, "Could not find given property in control." ); 
                if( changingProperty != null )
                {
                   changingProperty.SetValue(comp, value);
                } 

                //Reset the Glyphs. 
                SelectionManager selMgr = (SelectionManager)provider.GetService(typeof(SelectionManager)); 
                if (selMgr != null)
                { 
                    selMgr.Refresh();
                }

                // Invlidate the Window... 
                if (panelDesigner != null)
                { 
                    panelDesigner.InvalidateGlyph(); 
                }
            } 
        }

        /// <devdoc>
        ///     Checks if the ToolStrip Container is DOCK FILLED 
        /// </devdoc>
        private bool IsDockFilled 
        { 
            get
            { 
                PropertyDescriptor dockProp = TypeDescriptor.GetProperties(container)["Dock"];
                if (dockProp != null && ((DockStyle)dockProp.GetValue(container)) != DockStyle.Fill) {
                    return false;
                } 
                return true;
            } 
        } 

        /// <devdoc> 
        ///     Checks if the ToolStripContainer is a child control of the designerHost's rootComponent
        /// </devdoc>
        private bool ProvideReparent
        { 
            get
            { 
 
                if (host != null)
                { 
                    Control root = host.RootComponent as Control;
                    // Reparent the Controls only if the ToolStripContainer is a child of the RootComponent...
                    if (root != null && container.Parent == root && IsDockFilled && root.Controls.Count > 1)
                    { 
                        return true;
                    } 
                } 
                return false;
            } 
        }

        /// <devdoc>
        ///     Sets the Dock.. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public void SetDockToForm() 
        {
            if (host != null) 
            {
                Control root = host.RootComponent as Control;
                //change the Parent only if its not parented to the form.
                if (root != null && container.Parent != root) 
                {
                    root.Controls.Add(container); 
                } 
                //set the dock prop to DockStyle.Fill
                if (!IsDockFilled) 
                {
                    PropertyDescriptor dockProp = TypeDescriptor.GetProperties(container)["Dock"];
                    if (dockProp != null)
                    { 
                        dockProp.SetValue(container, DockStyle.Fill);
                    } 
                } 
            }
        } 

        /// <devdoc>
        ///     Reparent the controls on the form.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public void ReparentControls() 
        {
            if (host != null) 
            {
                Control root = host.RootComponent as Control;
                // Reparent the Controls only if the ToolStripContainer is a child of the RootComponent...
                if (root != null && container.Parent == root && root.Controls.Count > 1) 
                {
                    Control newParent = container.ContentPanel; 
                    PropertyDescriptor autoScrollProp = TypeDescriptor.GetProperties(newParent)["AutoScroll"]; 
                    if (autoScrollProp != null)
                    { 
                        autoScrollProp.SetValue(newParent, true);
                    }

                    // create a transaction so this happens as an atomic unit. 
                    DesignerTransaction changeParent = host.CreateTransaction("Reparent Transaction");
 
                    try 
                    {
                        Control[] childControls = new Control[root.Controls.Count]; 
                        root.Controls.CopyTo(childControls, 0);

                        foreach(Control c in childControls)
                        { 
                            if (c == container || c is MdiClient)
                            { 
                                continue; 
                            }
 
                            // We should not reparent inherited Controls...
                            InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(c)[typeof(InheritanceAttribute)];
                            if (ia == null || ia.InheritanceLevel == InheritanceLevel.InheritedReadOnly)
                            { 
                                continue;
                            } 
 
                            IComponentChangeService changeSvc = provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
 
                            if (c is ToolStrip)
                            {
                                newParent = GetParent(c);
                            } 
                            else
                            { 
                                newParent = container.ContentPanel; 
                            }
 
                            PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(newParent)["Controls"];
                            Control oldParent = c.Parent;

                            if (oldParent != null) { 
                                if (changeSvc != null) {
                                    changeSvc.OnComponentChanging(oldParent, controlsProp); 
                                } 
                                //remove control from the old parent
                                oldParent.Controls.Remove(c); 
                            }

                            if (changeSvc != null) {
                                changeSvc.OnComponentChanging(newParent, controlsProp); 
                            }
                            //finally add & relocate the control with the new parent 
                            newParent.Controls.Add(c); 

                            //fire our comp changed events 
                            if (changeSvc != null && oldParent != null) {
                                changeSvc.OnComponentChanged(oldParent, controlsProp, null, null);
                            }
 
                            // fire comp changed on the newParent
                            if (changeSvc != null) { 
                                changeSvc.OnComponentChanged(newParent, controlsProp, null, null); 
                            }
                        } 
                    }
                    catch {
                        if (changeParent != null)
                        { 
                            changeParent.Cancel();
                            changeParent = null; 
                        } 

                    } 
                    finally {
                        if (changeParent != null)
                        {
                            changeParent.Commit(); 
                            changeParent = null;
                        } 
 
                        //Set the Selection on the new Parent ... so that the selection is restored to the new item,
                        ISelectionService selSvc = provider.GetService(typeof(ISelectionService)) as ISelectionService; 
                        if (selSvc != null)
                        {
                            selSvc.SetSelectedComponents(new IComponent[] { newParent });
                        } 
                    }
                } 
 
            }
        } 

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private Control GetParent(Control c)
        { 
            Control newParent = container.ContentPanel;
            DockStyle dock = c.Dock; 
 
            foreach(Control panel in container.Controls)
            { 
                if (panel is ToolStripPanel)
                {
                    if (panel.Dock == dock)
                    { 
                        newParent = panel;
                        break; 
                    } 
                }
            } 
            return newParent;
        }

        /// <devdoc> 
        ///     Visibility of TopToolStripPanel.
        /// </devdoc> 
        public bool TopVisible 
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            get
            {
                return (bool)GetProperty(container, "TopToolStripPanelVisible");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set 
            { 
                if (value != TopVisible) {
                    ChangeProperty(container, "TopToolStripPanelVisible", (object)value); 
                }
            }
        }
 
        /// <devdoc>
        ///     Visibility of BottomToolStripPanel. 
        /// </devdoc> 
        public bool BottomVisible
        { 

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get
            { 
                return (bool)GetProperty(container, "BottomToolStripPanelVisible");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            set
            { 
                if (value != BottomVisible) {
                    ChangeProperty(container, "BottomToolStripPanelVisible", (object)value);
                }
            } 
        }
 
        /// <devdoc> 
        ///     Visibility of LeftToolStripPanel.
        /// </devdoc> 
        public bool LeftVisible
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get 
            {
                return (bool)GetProperty(container, "LeftToolStripPanelVisible"); 
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set 
            {
                if (value != LeftVisible) {
                    ChangeProperty(container, "LeftToolStripPanelVisible", (object)value);
                } 
            }
        } 
 
        /// <devdoc>
        ///     Visibility of RightToolStripPanel. 
        /// </devdoc>
        public bool RightVisible
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            get
            { 
                return (bool)GetProperty(container, "RightToolStripPanelVisible"); 
            }
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            set
            {
                if (value != RightVisible) {
                    ChangeProperty(container, "RightToolStripPanelVisible", (object)value); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Returns the control's action list items.
        /// </devdoc>
        public override DesignerActionItemCollection GetSortedActionItems()
        { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
 
            items.Add(new DesignerActionHeaderItem(SR.GetString(SR.ToolStripContainerActionList_Visible), SR.GetString(SR.ToolStripContainerActionList_Show))); 
            items.Add(new DesignerActionPropertyItem("TopVisible",
                                                     SR.GetString(SR.ToolStripContainerActionList_Top), 
                                                     SR.GetString(SR.ToolStripContainerActionList_Show),
                                                     SR.GetString(SR.ToolStripContainerActionList_TopDesc)));

            items.Add(new DesignerActionPropertyItem("BottomVisible", 
                                                     SR.GetString(SR.ToolStripContainerActionList_Bottom),
                                                     SR.GetString(SR.ToolStripContainerActionList_Show), 
                                                     SR.GetString(SR.ToolStripContainerActionList_BottomDesc))); 

            items.Add(new DesignerActionPropertyItem("LeftVisible", 
                                                    SR.GetString(SR.ToolStripContainerActionList_Left),
                                                    SR.GetString(SR.ToolStripContainerActionList_Show),
                                                    SR.GetString(SR.ToolStripContainerActionList_LeftDesc)));
 
            items.Add(new DesignerActionPropertyItem("RightVisible",
                                                    SR.GetString(SR.ToolStripContainerActionList_Right), 
                                                    SR.GetString(SR.ToolStripContainerActionList_Show), 
                                                    SR.GetString(SR.ToolStripContainerActionList_RightDesc)));
            if (!IsDockFilled) { 
                bool parentIsForm = true;
                if (host != null)
                {
                    Control userControl = host.RootComponent as UserControl; 
                    if (userControl != null)
                    { 
                        parentIsForm = false; 
                    }
                } 

                items.Add(new DesignerActionMethodItem(this, "SetDockToForm", parentIsForm ? SR.GetString(SR.DesignerShortcutDockInForm) : SR.GetString(SR.DesignerShortcutDockInUserControl)));
            }
            if (ProvideReparent) 
            {
                items.Add(new DesignerActionMethodItem(this, "ReparentControls", SR.GetString(SR.DesignerShortcutReparentControls))); 
            } 

            return items; 
        }

    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
