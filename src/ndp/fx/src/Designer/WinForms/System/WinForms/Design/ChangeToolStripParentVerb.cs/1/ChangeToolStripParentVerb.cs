//------------------------------------------------------------------------------ 
// <copyright file="ChangeToolStripParentVerb.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using Accessibility;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Runtime.Serialization;
    using System.ComponentModel.Design.Serialization;
    using System.Drawing;
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing.Design;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <devdoc>
    ///  Internal class to provide 'Embed in ToolStripContainer" verb for ToolStrips & MenuStrips. 
    /// </devdoc>
    internal class ChangeToolStripParentVerb {

        private ToolStripDesigner               _designer; 
        private IDesignerHost                   _host;
        private IComponentChangeService         componentChangeSvc; 
        private IServiceProvider                _provider; 

        /// <devdoc> 
        /// Create one of these things...
        /// </devdoc>
        internal ChangeToolStripParentVerb(string text, ToolStripDesigner designer) {
            Debug.Assert(designer != null, "Can't have a StandardMenuStripVerb without an associated designer"); 
            this._designer = designer;
            this._provider = designer.Component.Site; 
            this._host = (IDesignerHost)_provider.GetService(typeof(IDesignerHost)); 
            componentChangeSvc = (IComponentChangeService)_provider.GetService(typeof(IComponentChangeService));
        } 

        /// <devdoc>
        /// When the verb is invoked, change the parent of the ToolStrip.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        // This is actually called... 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ChangeParent() { 
            Cursor current = Cursor.Current;
            // create a transaction so this happens as an atomic unit.
            DesignerTransaction changeParent = _host.CreateTransaction("Add ToolStripContainer Transaction");
 
            try
            { 
                Cursor.Current = Cursors.WaitCursor; 
                //Add a New ToolStripContainer to the RootComponent ...
                Control root = _host.RootComponent as Control; 
                ParentControlDesigner rootDesigner = _host.GetDesigner(root) as ParentControlDesigner;
                if (rootDesigner != null)
                {
                    // close the DAP first - this is so that the autoshown panel on drag drop here is not conflicting with the currently opened panel 
                    // if the verb was called from the panel
                    ToolStrip toolStrip = _designer.Component as ToolStrip; 
                    if(toolStrip != null && _designer != null && _designer.Component != null && _provider != null) { 
                        DesignerActionUIService dapuisvc = _provider.GetService(typeof(DesignerActionUIService)) as DesignerActionUIService;
                        dapuisvc.HideUI(toolStrip); 
                    }

                    // Get OleDragHandler ...
                    ToolboxItem tbi = new ToolboxItem(typeof(System.Windows.Forms.ToolStripContainer)); 
                    OleDragDropHandler ddh = rootDesigner.GetOleDragHandler();
                    if (ddh != null) 
                    { 
                        IComponent[] newComp = ddh.CreateTool(tbi, root, 0, 0, 0, 0, false, false);
                        ToolStripContainer tsc = newComp[0] as ToolStripContainer; 
                        if (tsc != null)
                        {
                            if(toolStrip != null)
                            { 
                                IComponentChangeService changeSvc = _provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
 
                                Control newParent = GetParent(tsc, toolStrip); 

                                PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(newParent)["Controls"]; 

                                Control oldParent = toolStrip.Parent;
                                if (oldParent != null) {
                                    changeSvc.OnComponentChanging(oldParent, controlsProp); 
                                    //remove control from the old parent
                                    oldParent.Controls.Remove(toolStrip); 
                                } 

                                if (newParent != null) 
                                {
                                    changeSvc.OnComponentChanging(newParent, controlsProp);
                                    //finally add & relocate the control with the new parent
                                    newParent.Controls.Add(toolStrip); 
                                }
 
                                //fire our comp changed events 
                                if (changeSvc != null && oldParent != null && newParent != null) {
                                    changeSvc.OnComponentChanged(oldParent, controlsProp, null, null); 
                                    changeSvc.OnComponentChanged(newParent, controlsProp, null, null);
                                }

                                //Set the Selection on the new Parent ... so that the selection is restored to the new item, 
                                ISelectionService selSvc = _provider.GetService(typeof(ISelectionService)) as ISelectionService;
                                if (selSvc != null) 
                                { 
                                    selSvc.SetSelectedComponents(new IComponent[] { tsc });
                                } 
                            }
                        }
                    }
                } 
            }
            catch (Exception e){ 
                if (e is System.InvalidOperationException) { 
                    IUIService uiService = (IUIService)_provider.GetService(typeof(IUIService));
                    uiService.ShowError(e.Message); 
                }

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
                Cursor.Current = current; 
            }
        } 

        private Control GetParent(ToolStripContainer container, Control c)
        {
            Control newParent = container.ContentPanel; 
            DockStyle dock = c.Dock;
            if (c.Parent is ToolStripPanel) 
            { 
                dock = c.Parent.Dock;
            } 


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
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ChangeToolStripParentVerb.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using Accessibility;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Runtime.Serialization;
    using System.ComponentModel.Design.Serialization;
    using System.Drawing;
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing.Design;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <devdoc>
    ///  Internal class to provide 'Embed in ToolStripContainer" verb for ToolStrips & MenuStrips. 
    /// </devdoc>
    internal class ChangeToolStripParentVerb {

        private ToolStripDesigner               _designer; 
        private IDesignerHost                   _host;
        private IComponentChangeService         componentChangeSvc; 
        private IServiceProvider                _provider; 

        /// <devdoc> 
        /// Create one of these things...
        /// </devdoc>
        internal ChangeToolStripParentVerb(string text, ToolStripDesigner designer) {
            Debug.Assert(designer != null, "Can't have a StandardMenuStripVerb without an associated designer"); 
            this._designer = designer;
            this._provider = designer.Component.Site; 
            this._host = (IDesignerHost)_provider.GetService(typeof(IDesignerHost)); 
            componentChangeSvc = (IComponentChangeService)_provider.GetService(typeof(IComponentChangeService));
        } 

        /// <devdoc>
        /// When the verb is invoked, change the parent of the ToolStrip.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        // This is actually called... 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ChangeParent() { 
            Cursor current = Cursor.Current;
            // create a transaction so this happens as an atomic unit.
            DesignerTransaction changeParent = _host.CreateTransaction("Add ToolStripContainer Transaction");
 
            try
            { 
                Cursor.Current = Cursors.WaitCursor; 
                //Add a New ToolStripContainer to the RootComponent ...
                Control root = _host.RootComponent as Control; 
                ParentControlDesigner rootDesigner = _host.GetDesigner(root) as ParentControlDesigner;
                if (rootDesigner != null)
                {
                    // close the DAP first - this is so that the autoshown panel on drag drop here is not conflicting with the currently opened panel 
                    // if the verb was called from the panel
                    ToolStrip toolStrip = _designer.Component as ToolStrip; 
                    if(toolStrip != null && _designer != null && _designer.Component != null && _provider != null) { 
                        DesignerActionUIService dapuisvc = _provider.GetService(typeof(DesignerActionUIService)) as DesignerActionUIService;
                        dapuisvc.HideUI(toolStrip); 
                    }

                    // Get OleDragHandler ...
                    ToolboxItem tbi = new ToolboxItem(typeof(System.Windows.Forms.ToolStripContainer)); 
                    OleDragDropHandler ddh = rootDesigner.GetOleDragHandler();
                    if (ddh != null) 
                    { 
                        IComponent[] newComp = ddh.CreateTool(tbi, root, 0, 0, 0, 0, false, false);
                        ToolStripContainer tsc = newComp[0] as ToolStripContainer; 
                        if (tsc != null)
                        {
                            if(toolStrip != null)
                            { 
                                IComponentChangeService changeSvc = _provider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
 
                                Control newParent = GetParent(tsc, toolStrip); 

                                PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(newParent)["Controls"]; 

                                Control oldParent = toolStrip.Parent;
                                if (oldParent != null) {
                                    changeSvc.OnComponentChanging(oldParent, controlsProp); 
                                    //remove control from the old parent
                                    oldParent.Controls.Remove(toolStrip); 
                                } 

                                if (newParent != null) 
                                {
                                    changeSvc.OnComponentChanging(newParent, controlsProp);
                                    //finally add & relocate the control with the new parent
                                    newParent.Controls.Add(toolStrip); 
                                }
 
                                //fire our comp changed events 
                                if (changeSvc != null && oldParent != null && newParent != null) {
                                    changeSvc.OnComponentChanged(oldParent, controlsProp, null, null); 
                                    changeSvc.OnComponentChanged(newParent, controlsProp, null, null);
                                }

                                //Set the Selection on the new Parent ... so that the selection is restored to the new item, 
                                ISelectionService selSvc = _provider.GetService(typeof(ISelectionService)) as ISelectionService;
                                if (selSvc != null) 
                                { 
                                    selSvc.SetSelectedComponents(new IComponent[] { tsc });
                                } 
                            }
                        }
                    }
                } 
            }
            catch (Exception e){ 
                if (e is System.InvalidOperationException) { 
                    IUIService uiService = (IUIService)_provider.GetService(typeof(IUIService));
                    uiService.ShowError(e.Message); 
                }

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
                Cursor.Current = current; 
            }
        } 

        private Control GetParent(ToolStripContainer container, Control c)
        {
            Control newParent = container.ContentPanel; 
            DockStyle dock = c.Dock;
            if (c.Parent is ToolStripPanel) 
            { 
                dock = c.Parent.Dock;
            } 


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
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
