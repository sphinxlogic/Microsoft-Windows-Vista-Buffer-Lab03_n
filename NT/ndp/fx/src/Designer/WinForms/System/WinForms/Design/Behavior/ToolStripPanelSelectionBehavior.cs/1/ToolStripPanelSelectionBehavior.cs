namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
    using System.Drawing.Design; 

    /// <include file='doc\ToolStripPanelSelectionBehavior.uex' path='docs/doc[@for="ToolStripPanelSelectionBehavior"]/*' />
    /// <devdoc>
    /// 
    /// </devdoc>
    internal sealed class ToolStripPanelSelectionBehavior : Behavior { 
 
        private ToolStripPanel          relatedControl;             //our related control
        private IServiceProvider        serviceProvider;            //used for starting a drag/drop 
        private BehaviorService         behaviorService;            //ptr to where we start our drag/drop operation

        private const int defaultBounds = 25;
 
        /// <include file='doc\ToolStripPanelSelectionBehavior.uex' path='docs/doc[@for="ToolStripPanelSelectionBehavior.ToolStripPanelSelectionBehavior"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        internal ToolStripPanelSelectionBehavior(ToolStripPanel containerControl, IServiceProvider serviceProvider) {
            this.behaviorService = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService)); 
            if (behaviorService == null) {
                Debug.Fail("Could not get the BehaviorService from ContainerSelectroBehavior!");
                return;
            } 

            this.relatedControl = containerControl; 
            this.serviceProvider = serviceProvider; 
        }
 
        private static bool DragComponentContainsToolStrip(DropSourceBehavior.BehaviorDataObject data)
        {

            bool containsTool = false; 

            if (data != null) { 
                ArrayList dragComps = new ArrayList(data.DragComponents); 
                foreach(Component dragComp in dragComps)
                { 
                    ToolStrip tool = dragComp as ToolStrip;
                    if (tool != null)
                    {
                        containsTool = true; 
                        break;
                    } 
                } 
            }
            return containsTool; 

        }

        private void ExpandPanel(bool setSelection) 
        {
            //Change the padding to "dynamically" increase the bounds.. 
            switch (relatedControl.Dock) 
            {
                case DockStyle.Top: 
                    relatedControl.Padding = new Padding (0, 0, 0, defaultBounds);
                    break;
                case DockStyle.Left:
                    relatedControl.Padding = new Padding (0, 0, defaultBounds, 0); 
                    break;
                case DockStyle.Right: 
                    relatedControl.Padding = new Padding (defaultBounds, 0, 0, 0); 
                    break;
                case DockStyle.Bottom: 
                    relatedControl.Padding = new Padding (0, defaultBounds, 0, 0);
                    break;
            }
 

            if (setSelection) { 
                //select our component 
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                if (selSvc != null) { 
                    selSvc.SetSelectedComponents(new object[] {relatedControl}, SelectionTypes.Replace);
                }
            }
        } 

 
        /// <include file='doc\ToolStripPanelSelectionBehavior.uex' path='docs/doc[@for="ToolStripPanelSelectionBehavior.OnMouseUp"]/*' /> 
        /// <devdoc>
        ///     Simply clear the initial drag point, so we can start again 
        ///     on the next mouse down.
        /// </devdoc>
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) {
            ToolStripPanelSelectionGlyph selectionGlyph = g as ToolStripPanelSelectionGlyph; 
            if (button == MouseButtons.Left && selectionGlyph != null) {
                if (!selectionGlyph.IsExpanded) 
                { 
                    ExpandPanel(true);
 
                    // Invalidate
                    Rectangle oldBounds = selectionGlyph.Bounds;
                    selectionGlyph.IsExpanded = true;
                    behaviorService.Invalidate(oldBounds); 
                    behaviorService.Invalidate(selectionGlyph.Bounds);
 
                } 
                else {
 
                    //Change the padding to "dynamically" increase the bounds..
                    relatedControl.Padding = new Padding(0);
                    // Invalidate
                    Rectangle oldBounds = selectionGlyph.Bounds; 
                    selectionGlyph.IsExpanded = false;
                    behaviorService.Invalidate(oldBounds); 
                    behaviorService.Invalidate(selectionGlyph.Bounds); 

                    //select our parent... 
                    ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                    Component curSel = selSvc.PrimarySelection as Component;

                    if (curSel != relatedControl.Parent) 
                    {
                        if (selSvc != null) { 
                            selSvc.SetSelectedComponents(new object[] {relatedControl.Parent}, SelectionTypes.Replace); 
                        }
                    } 
                    else
                    {
                        Control parent = relatedControl.Parent;
                        parent.PerformLayout(); 

                        SelectionManager selMgr = (SelectionManager)serviceProvider.GetService(typeof(SelectionManager)); 
                        selMgr.Refresh(); 

                        Point loc = behaviorService.ControlToAdornerWindow(parent); 
                        Rectangle translatedBounds = new Rectangle(loc, parent.Size);
                        behaviorService.Invalidate(translatedBounds);
                    }
 

 
                } 
            }
            return false; 
        }


        /// <devdoc> 
        ///     Reparent the toolStripcontrol
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        private void ReParentControls(ArrayList controls, bool copy)
        { 
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
            if (host != null)
            {
                if (controls.Count > 0) 
                {
                    string transDesc; 
 
                    if (controls.Count == 1 && controls[0] is ToolStrip) {
                        string name = TypeDescriptor.GetComponentName(controls[0]); 
                        if (name == null || name.Length == 0) {
                            name = controls[0].GetType().Name;
                        }
                        transDesc = SR.GetString(copy ? SR.BehaviorServiceCopyControl : SR.BehaviorServiceMoveControl, name); 
                    }
                    else { 
                        transDesc = SR.GetString(copy ? SR.BehaviorServiceCopyControls : SR.BehaviorServiceMoveControls, controls.Count); 
                    }
 
                    // create a transaction so this happens as an atomic unit.
                    DesignerTransaction changeParent = host.CreateTransaction(transDesc);

                    try 
                    {
                        ArrayList temp = null; 
                        ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 

                        if (copy) { 
                            temp = new ArrayList();
                            selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                        }
 
                        for (int i = 0; i < controls.Count; i++)
                        { 
                            Control c = controls[i] as Control; 

                            if (!(c is ToolStrip)) 
                            {
                                continue;
                            }
 

                            if (copy) { 
                                temp.Clear(); 
                                temp.Add(c);
                                //if we are copying, then well, make a copy of the control 
                                temp = DesignerUtils.CopyDragObjects(temp, serviceProvider) as ArrayList;
                                if (temp != null) {
                                    c = temp[0] as Control;
                                    c.Visible = true; 
                                }
                            } 
 
                            Control newParent = relatedControl;
 
                            IComponentChangeService changeSvc = serviceProvider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                            PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(newParent)["Controls"];

                            Control oldParent = c.Parent; 

                            if (oldParent != null && !copy) { 
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
                            if (changeSvc != null && oldParent != null && !copy) { 
                                changeSvc.OnComponentChanged(oldParent, controlsProp, null, null);
                            } 
 
                            // fire comp changed on the newParent
                            if (changeSvc != null) { 
                                changeSvc.OnComponentChanged(newParent, controlsProp, null, null);
                            }

                            if (selSvc != null) { 
                                selSvc.SetSelectedComponents(new object[] {c}, i == 0 ? SelectionTypes.Primary | SelectionTypes.Replace : SelectionTypes.Add);
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
                    } 
                } 

            } 
        }

        /// <include file='doc\ToolStripPanelSelectionBehavior.uex' path='docs/doc[@for="ToolStripPanelSelectionBehavior.OnMouseUp"]/*' />
        /// <devdoc> 
        ///     Simply clear the initial drag point, so we can start again
        ///     on the next mouse down. 
        /// </devdoc> 
        public override void OnDragDrop(Glyph g, DragEventArgs e)
        { 
            ToolStripPanelSelectionGlyph selectionGlyph = g as ToolStripPanelSelectionGlyph;
            // Expand the glyph only if ToolStrip is dragged around
            bool expandPanel = false;
            ArrayList dragComps = null; 

            DropSourceBehavior.BehaviorDataObject data = e.Data as DropSourceBehavior.BehaviorDataObject; 
            if (data != null) { 
                dragComps = new ArrayList(data.DragComponents);
                foreach(Component dragComp in dragComps) 
                {
                    ToolStrip tool = dragComp as ToolStrip;
                    if (tool != null && tool.Parent != relatedControl)
                    { 
                        expandPanel = true;
                        break; 
                    } 
                }
                if (expandPanel) 
                {
                    Control root = relatedControl.Parent;
                    if (root != null)
                    { 
                        try
                        { 
                            root.SuspendLayout(); 
                            ExpandPanel(false);
                            // Invalidate 
                            Rectangle oldBounds = selectionGlyph.Bounds;
                            selectionGlyph.IsExpanded = true;
                            behaviorService.Invalidate(oldBounds);
                            behaviorService.Invalidate(selectionGlyph.Bounds); 
                            //change Parent
                            ReParentControls(dragComps, e.Effect == DragDropEffects.Copy); 
                        } 
                        finally
                        { 
                            root.ResumeLayout(true);
                        }
                    }
                } 
                data.CleanupDrag();
            } 
            else if (e.Data is DataObject && dragComps == null) 
            {
                IToolboxService tbxService = (IToolboxService)serviceProvider.GetService(typeof(IToolboxService)); 
                IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;

                if (tbxService != null && host != null)
                { 
                    ToolboxItem item = tbxService.DeserializeToolboxItem(e.Data, host);
                    if (item.GetType(host) == typeof(ToolStrip) 
                        || item.GetType(host) == typeof(MenuStrip) 
                        || item.GetType(host) == typeof(StatusStrip))
                    { 
                        ToolStripPanelDesigner panelDesigner = host.GetDesigner(relatedControl) as ToolStripPanelDesigner;
                        if (panelDesigner != null)
                        {
                            OleDragDropHandler ddh = panelDesigner.GetOleDragHandler(); 
                            if (ddh != null)
                            { 
                                ddh.CreateTool(item, relatedControl, 0, 0, 0, 0, false, false); 
                            }
                        } 
                    }
                 }
            }
        } 

        /// <devdoc> 
        ///     OnDragEnter can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        public override void OnDragEnter(Glyph g, DragEventArgs e)
        {
            e.Effect = DragComponentContainsToolStrip(e.Data as DropSourceBehavior.BehaviorDataObject) ? (Control.ModifierKeys == Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move : DragDropEffects.None;
        } 

        /// <devdoc> 
        ///     OnDragEnter can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        public override void OnDragOver(Glyph g, DragEventArgs e)
        {
            e.Effect =DragComponentContainsToolStrip(e.Data as DropSourceBehavior.BehaviorDataObject) ? (Control.ModifierKeys == Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move : DragDropEffects.None ;
        } 

    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
    using System.Drawing.Design; 

    /// <include file='doc\ToolStripPanelSelectionBehavior.uex' path='docs/doc[@for="ToolStripPanelSelectionBehavior"]/*' />
    /// <devdoc>
    /// 
    /// </devdoc>
    internal sealed class ToolStripPanelSelectionBehavior : Behavior { 
 
        private ToolStripPanel          relatedControl;             //our related control
        private IServiceProvider        serviceProvider;            //used for starting a drag/drop 
        private BehaviorService         behaviorService;            //ptr to where we start our drag/drop operation

        private const int defaultBounds = 25;
 
        /// <include file='doc\ToolStripPanelSelectionBehavior.uex' path='docs/doc[@for="ToolStripPanelSelectionBehavior.ToolStripPanelSelectionBehavior"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        internal ToolStripPanelSelectionBehavior(ToolStripPanel containerControl, IServiceProvider serviceProvider) {
            this.behaviorService = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService)); 
            if (behaviorService == null) {
                Debug.Fail("Could not get the BehaviorService from ContainerSelectroBehavior!");
                return;
            } 

            this.relatedControl = containerControl; 
            this.serviceProvider = serviceProvider; 
        }
 
        private static bool DragComponentContainsToolStrip(DropSourceBehavior.BehaviorDataObject data)
        {

            bool containsTool = false; 

            if (data != null) { 
                ArrayList dragComps = new ArrayList(data.DragComponents); 
                foreach(Component dragComp in dragComps)
                { 
                    ToolStrip tool = dragComp as ToolStrip;
                    if (tool != null)
                    {
                        containsTool = true; 
                        break;
                    } 
                } 
            }
            return containsTool; 

        }

        private void ExpandPanel(bool setSelection) 
        {
            //Change the padding to "dynamically" increase the bounds.. 
            switch (relatedControl.Dock) 
            {
                case DockStyle.Top: 
                    relatedControl.Padding = new Padding (0, 0, 0, defaultBounds);
                    break;
                case DockStyle.Left:
                    relatedControl.Padding = new Padding (0, 0, defaultBounds, 0); 
                    break;
                case DockStyle.Right: 
                    relatedControl.Padding = new Padding (defaultBounds, 0, 0, 0); 
                    break;
                case DockStyle.Bottom: 
                    relatedControl.Padding = new Padding (0, defaultBounds, 0, 0);
                    break;
            }
 

            if (setSelection) { 
                //select our component 
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                if (selSvc != null) { 
                    selSvc.SetSelectedComponents(new object[] {relatedControl}, SelectionTypes.Replace);
                }
            }
        } 

 
        /// <include file='doc\ToolStripPanelSelectionBehavior.uex' path='docs/doc[@for="ToolStripPanelSelectionBehavior.OnMouseUp"]/*' /> 
        /// <devdoc>
        ///     Simply clear the initial drag point, so we can start again 
        ///     on the next mouse down.
        /// </devdoc>
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) {
            ToolStripPanelSelectionGlyph selectionGlyph = g as ToolStripPanelSelectionGlyph; 
            if (button == MouseButtons.Left && selectionGlyph != null) {
                if (!selectionGlyph.IsExpanded) 
                { 
                    ExpandPanel(true);
 
                    // Invalidate
                    Rectangle oldBounds = selectionGlyph.Bounds;
                    selectionGlyph.IsExpanded = true;
                    behaviorService.Invalidate(oldBounds); 
                    behaviorService.Invalidate(selectionGlyph.Bounds);
 
                } 
                else {
 
                    //Change the padding to "dynamically" increase the bounds..
                    relatedControl.Padding = new Padding(0);
                    // Invalidate
                    Rectangle oldBounds = selectionGlyph.Bounds; 
                    selectionGlyph.IsExpanded = false;
                    behaviorService.Invalidate(oldBounds); 
                    behaviorService.Invalidate(selectionGlyph.Bounds); 

                    //select our parent... 
                    ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                    Component curSel = selSvc.PrimarySelection as Component;

                    if (curSel != relatedControl.Parent) 
                    {
                        if (selSvc != null) { 
                            selSvc.SetSelectedComponents(new object[] {relatedControl.Parent}, SelectionTypes.Replace); 
                        }
                    } 
                    else
                    {
                        Control parent = relatedControl.Parent;
                        parent.PerformLayout(); 

                        SelectionManager selMgr = (SelectionManager)serviceProvider.GetService(typeof(SelectionManager)); 
                        selMgr.Refresh(); 

                        Point loc = behaviorService.ControlToAdornerWindow(parent); 
                        Rectangle translatedBounds = new Rectangle(loc, parent.Size);
                        behaviorService.Invalidate(translatedBounds);
                    }
 

 
                } 
            }
            return false; 
        }


        /// <devdoc> 
        ///     Reparent the toolStripcontrol
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        private void ReParentControls(ArrayList controls, bool copy)
        { 
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
            if (host != null)
            {
                if (controls.Count > 0) 
                {
                    string transDesc; 
 
                    if (controls.Count == 1 && controls[0] is ToolStrip) {
                        string name = TypeDescriptor.GetComponentName(controls[0]); 
                        if (name == null || name.Length == 0) {
                            name = controls[0].GetType().Name;
                        }
                        transDesc = SR.GetString(copy ? SR.BehaviorServiceCopyControl : SR.BehaviorServiceMoveControl, name); 
                    }
                    else { 
                        transDesc = SR.GetString(copy ? SR.BehaviorServiceCopyControls : SR.BehaviorServiceMoveControls, controls.Count); 
                    }
 
                    // create a transaction so this happens as an atomic unit.
                    DesignerTransaction changeParent = host.CreateTransaction(transDesc);

                    try 
                    {
                        ArrayList temp = null; 
                        ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 

                        if (copy) { 
                            temp = new ArrayList();
                            selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                        }
 
                        for (int i = 0; i < controls.Count; i++)
                        { 
                            Control c = controls[i] as Control; 

                            if (!(c is ToolStrip)) 
                            {
                                continue;
                            }
 

                            if (copy) { 
                                temp.Clear(); 
                                temp.Add(c);
                                //if we are copying, then well, make a copy of the control 
                                temp = DesignerUtils.CopyDragObjects(temp, serviceProvider) as ArrayList;
                                if (temp != null) {
                                    c = temp[0] as Control;
                                    c.Visible = true; 
                                }
                            } 
 
                            Control newParent = relatedControl;
 
                            IComponentChangeService changeSvc = serviceProvider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                            PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(newParent)["Controls"];

                            Control oldParent = c.Parent; 

                            if (oldParent != null && !copy) { 
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
                            if (changeSvc != null && oldParent != null && !copy) { 
                                changeSvc.OnComponentChanged(oldParent, controlsProp, null, null);
                            } 
 
                            // fire comp changed on the newParent
                            if (changeSvc != null) { 
                                changeSvc.OnComponentChanged(newParent, controlsProp, null, null);
                            }

                            if (selSvc != null) { 
                                selSvc.SetSelectedComponents(new object[] {c}, i == 0 ? SelectionTypes.Primary | SelectionTypes.Replace : SelectionTypes.Add);
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
                    } 
                } 

            } 
        }

        /// <include file='doc\ToolStripPanelSelectionBehavior.uex' path='docs/doc[@for="ToolStripPanelSelectionBehavior.OnMouseUp"]/*' />
        /// <devdoc> 
        ///     Simply clear the initial drag point, so we can start again
        ///     on the next mouse down. 
        /// </devdoc> 
        public override void OnDragDrop(Glyph g, DragEventArgs e)
        { 
            ToolStripPanelSelectionGlyph selectionGlyph = g as ToolStripPanelSelectionGlyph;
            // Expand the glyph only if ToolStrip is dragged around
            bool expandPanel = false;
            ArrayList dragComps = null; 

            DropSourceBehavior.BehaviorDataObject data = e.Data as DropSourceBehavior.BehaviorDataObject; 
            if (data != null) { 
                dragComps = new ArrayList(data.DragComponents);
                foreach(Component dragComp in dragComps) 
                {
                    ToolStrip tool = dragComp as ToolStrip;
                    if (tool != null && tool.Parent != relatedControl)
                    { 
                        expandPanel = true;
                        break; 
                    } 
                }
                if (expandPanel) 
                {
                    Control root = relatedControl.Parent;
                    if (root != null)
                    { 
                        try
                        { 
                            root.SuspendLayout(); 
                            ExpandPanel(false);
                            // Invalidate 
                            Rectangle oldBounds = selectionGlyph.Bounds;
                            selectionGlyph.IsExpanded = true;
                            behaviorService.Invalidate(oldBounds);
                            behaviorService.Invalidate(selectionGlyph.Bounds); 
                            //change Parent
                            ReParentControls(dragComps, e.Effect == DragDropEffects.Copy); 
                        } 
                        finally
                        { 
                            root.ResumeLayout(true);
                        }
                    }
                } 
                data.CleanupDrag();
            } 
            else if (e.Data is DataObject && dragComps == null) 
            {
                IToolboxService tbxService = (IToolboxService)serviceProvider.GetService(typeof(IToolboxService)); 
                IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;

                if (tbxService != null && host != null)
                { 
                    ToolboxItem item = tbxService.DeserializeToolboxItem(e.Data, host);
                    if (item.GetType(host) == typeof(ToolStrip) 
                        || item.GetType(host) == typeof(MenuStrip) 
                        || item.GetType(host) == typeof(StatusStrip))
                    { 
                        ToolStripPanelDesigner panelDesigner = host.GetDesigner(relatedControl) as ToolStripPanelDesigner;
                        if (panelDesigner != null)
                        {
                            OleDragDropHandler ddh = panelDesigner.GetOleDragHandler(); 
                            if (ddh != null)
                            { 
                                ddh.CreateTool(item, relatedControl, 0, 0, 0, 0, false, false); 
                            }
                        } 
                    }
                 }
            }
        } 

        /// <devdoc> 
        ///     OnDragEnter can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        public override void OnDragEnter(Glyph g, DragEventArgs e)
        {
            e.Effect = DragComponentContainsToolStrip(e.Data as DropSourceBehavior.BehaviorDataObject) ? (Control.ModifierKeys == Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move : DragDropEffects.None;
        } 

        /// <devdoc> 
        ///     OnDragEnter can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        public override void OnDragOver(Glyph g, DragEventArgs e)
        {
            e.Effect =DragComponentContainsToolStrip(e.Data as DropSourceBehavior.BehaviorDataObject) ? (Control.ModifierKeys == Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move : DragDropEffects.None ;
        } 

    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
