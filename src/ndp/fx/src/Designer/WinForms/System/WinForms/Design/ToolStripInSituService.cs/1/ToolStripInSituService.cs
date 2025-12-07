//------------------------------------------------------------------------------ 
// <copyright file="ToolStripInSituService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Collections;
    using Microsoft.Win32; 
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Windows.Forms; 
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.Design.Behavior;
 

 
    /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService"]/*' /> 
    /// <devdoc>
    ///      This class implements the ISupportInSituService which enables some designers to 
    ///      go into InSitu Editing when Keys are pressed while the Component is Selected.
    /// </devdoc>
    internal class ToolStripInSituService : ISupportInSituService, IDisposable{
 
        private IServiceProvider sp;
        private IDesignerHost designerHost; 
        private IComponentChangeService componentChangeSvc; 

        private ToolStripDesigner toolDesigner; 
        private ToolStripItemDesigner toolItemDesigner;


        private ToolStripKeyboardHandlingService toolStripKeyBoardService; 

        /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService.ToolStripInSituService"]/*' /> 
        /// <devdoc> 
        ///      The constructor for this class which takes the serviceprovider used to get the selectionservice.
        ///      This ToolStripInSituService is ToolStrip specific. 
        /// </devdoc>

        public ToolStripInSituService(IServiceProvider provider)
        { 
            this.sp = provider;
 
            designerHost = (IDesignerHost)provider.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "ToolStripKeyboardHandlingService relies on the selection service, which is unavailable.");
 
            if (designerHost != null)
            {
                designerHost.AddService(typeof(ISupportInSituService), this);
            } 

            componentChangeSvc = (IComponentChangeService)designerHost.GetService(typeof(IComponentChangeService)); 
 
            Debug.Assert(componentChangeSvc != null, "ToolStripKeyboardHandlingService relies on the componentChange service, which is unavailable.");
            if (componentChangeSvc != null) 
            {
                componentChangeSvc.ComponentRemoved += new ComponentEventHandler(OnComponentRemoved);
            }
        } 

        /// <include file='doc\ControlCommandSet.uex' path='docs/doc[@for="ControlCommandSet.Dispose"]/*' /> 
        /// <devdoc> 
        ///     Disposes of this object, removing all commands from the menu service.
        /// </devdoc> 
        public void Dispose() {
            if (toolDesigner != null)
            {
                toolDesigner.Dispose(); 
                toolDesigner = null;
            } 
            if (toolItemDesigner != null) 
            {
                toolItemDesigner.Dispose(); 
                toolItemDesigner = null;
            }
            if (componentChangeSvc != null)
            { 
                componentChangeSvc.ComponentRemoved -= new ComponentEventHandler(OnComponentRemoved);
                componentChangeSvc = null; 
            } 
        }
 

        private ToolStripKeyboardHandlingService ToolStripKeyBoardService {
            get {
                if (toolStripKeyBoardService == null) { 
                    toolStripKeyBoardService = (ToolStripKeyboardHandlingService)sp.GetService(typeof(ToolStripKeyboardHandlingService));
                } 
                return toolStripKeyBoardService; 
            }
        } 


        /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService.IgnoreMessages"]/*' />
        /// <devdoc> 
        ///      Returning true for IgnoreMessages means that this service is interested in getting the KeyBoard characters.
        /// </devdoc> 
        public bool IgnoreMessages { 
            get  {
                ISelectionService selectionService = (ISelectionService)sp.GetService(typeof(ISelectionService)); 
                IDesignerHost host = (IDesignerHost)sp.GetService(typeof(IDesignerHost));
                if (selectionService != null && host != null)
                {
                    IComponent comp = selectionService.PrimarySelection as IComponent; 
                    if (comp == null)
                    { 
                        comp = (IComponent)ToolStripKeyBoardService.SelectedDesignerControl; 
                    }
                    if (comp != null) 
                    {
                        DesignerToolStripControlHost c = comp as DesignerToolStripControlHost;
                        if (c != null)
                        { 

                            ToolStripDropDown dropDown = c.GetCurrentParent() as ToolStripDropDown; 
                            if (dropDown != null) 
                            {
                                ToolStripDropDownItem parentItem = dropDown.OwnerItem as ToolStripDropDownItem; 
                                if (parentItem != null)
                                {
                                    ToolStripOverflowButton parent = parentItem as ToolStripOverflowButton;
                                    if (parent != null) 
                                    {
                                        //return false ...  We are on overflow.. 
                                        return false; 
                                    }
                                    else { 
                                        toolItemDesigner = host.GetDesigner(parentItem) as ToolStripMenuItemDesigner;
                                        if (toolItemDesigner != null) {
                                            toolDesigner = null;
                                            return true; 
                                        }
                                    } 
                                } 
                            }
                            else { 
                                MenuStrip tool = c.GetCurrentParent() as MenuStrip;
                                if (tool != null)
                                {
                                    toolDesigner = host.GetDesigner(tool) as ToolStripDesigner; 
                                    if (toolDesigner != null) {
                                        toolItemDesigner = null; 
                                        return true; 
                                    }
                                } 
                            }
                        }
                        else if (comp is ToolStripDropDown) //case for ToolStripDropDown..
                        { 
                            ToolStripDropDownDesigner designer = host.GetDesigner(comp) as ToolStripDropDownDesigner;
                            if (designer != null) 
                            { 
                                ToolStripMenuItem toolItem = designer.DesignerMenuItem;
                                if (toolItem != null) 
                                {
                                    toolItemDesigner = host.GetDesigner(toolItem) as ToolStripItemDesigner;
                                    if (toolItemDesigner != null) {
                                        toolDesigner = null; 
                                        return true;
                                    } 
                                } 
                            }
                        } 
                        else if (comp is MenuStrip)
                        {
                           toolDesigner = host.GetDesigner(comp) as ToolStripDesigner;
                           if (toolDesigner != null) { 
                               toolItemDesigner = null;
                               return true; 
                           } 
                        }
                        else if (comp is ToolStripMenuItem){ 
                           toolItemDesigner = host.GetDesigner(comp) as ToolStripItemDesigner;
                           if (toolItemDesigner != null) {
                               toolDesigner = null;
                               return true; 
                           }
                        } 
                    } 
                }
                return false; 
            }
        }

        /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService.HandleKeyChar"]/*' /> 
        /// <devdoc>
        ///      This function is called on the service when the PBRSFORWARD gets the first WM_CHAR message. 
        /// </devdoc> 
        public void HandleKeyChar()
        { 
            if (toolDesigner != null || toolItemDesigner != null) {
                if (toolDesigner != null)
                {
                   toolDesigner.ShowEditNode(false); 
                }
                else if (toolItemDesigner != null) 
                { 
                    ToolStripMenuItemDesigner menuDesigner = toolItemDesigner as ToolStripMenuItemDesigner;
                    if (menuDesigner != null) 
                    {
                         ISelectionService selService = (ISelectionService)sp.GetService(typeof(ISelectionService));
                         if (selService != null) {
                            object comp = selService.PrimarySelection; 
                            if (comp == null)
                            { 
                                comp = ToolStripKeyBoardService.SelectedDesignerControl; 
                            }
                            DesignerToolStripControlHost designerItem = comp as DesignerToolStripControlHost; 
                            if (designerItem != null || comp is ToolStripDropDown)
                            {
                                 menuDesigner.EditTemplateNode(false);
                            } 
                            else
                            { 
                                menuDesigner.ShowEditNode(false); 
                            }
                         } 
                    }
                    else {
                        toolItemDesigner.ShowEditNode(false);
                    } 
                }
            } 
        } 

 
        /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService.GetEditWindow"]/*' />
        /// <devdoc>
        ///      This function returns the Window handle that should get all the Keyboard messages.
        /// </devdoc> 
        public IntPtr GetEditWindow()
        { 
 
           IntPtr hWnd = IntPtr.Zero;
           if (toolDesigner != null && toolDesigner.Editor != null && toolDesigner.Editor.EditBox  != null) { 
               hWnd = (toolDesigner.Editor.EditBox.Visible) ? toolDesigner.Editor.EditBox.Handle : hWnd;
           }
           else if (toolItemDesigner != null  && toolItemDesigner.Editor != null && toolItemDesigner.Editor.EditBox  != null){
               hWnd = (toolItemDesigner.Editor.EditBox.Visible) ? toolItemDesigner.Editor.EditBox.Handle : hWnd; 
           }
           return hWnd; 
 
        }
 
        // Remove the Service when the last toolStrip is removed.
        private void OnComponentRemoved(object sender, ComponentEventArgs e)
        {
            bool toolStripPresent = false; 

            ComponentCollection comps = designerHost.Container.Components; 
            foreach (IComponent comp in comps) 
            {
                if (comp is ToolStrip) 
                {
                    toolStripPresent = true;
                    break;
                } 
            }
            if (!toolStripPresent) 
            { 
                ToolStripInSituService inSituService = (ToolStripInSituService)sp.GetService(typeof(ISupportInSituService));
                if (inSituService != null) 
                {
                    //since we are going away .. restore the old commands.
                    designerHost.RemoveService(typeof(ISupportInSituService));
                } 
            }
        } 
    } 
 }
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripInSituService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Collections;
    using Microsoft.Win32; 
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Windows.Forms; 
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.Design.Behavior;
 

 
    /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService"]/*' /> 
    /// <devdoc>
    ///      This class implements the ISupportInSituService which enables some designers to 
    ///      go into InSitu Editing when Keys are pressed while the Component is Selected.
    /// </devdoc>
    internal class ToolStripInSituService : ISupportInSituService, IDisposable{
 
        private IServiceProvider sp;
        private IDesignerHost designerHost; 
        private IComponentChangeService componentChangeSvc; 

        private ToolStripDesigner toolDesigner; 
        private ToolStripItemDesigner toolItemDesigner;


        private ToolStripKeyboardHandlingService toolStripKeyBoardService; 

        /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService.ToolStripInSituService"]/*' /> 
        /// <devdoc> 
        ///      The constructor for this class which takes the serviceprovider used to get the selectionservice.
        ///      This ToolStripInSituService is ToolStrip specific. 
        /// </devdoc>

        public ToolStripInSituService(IServiceProvider provider)
        { 
            this.sp = provider;
 
            designerHost = (IDesignerHost)provider.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "ToolStripKeyboardHandlingService relies on the selection service, which is unavailable.");
 
            if (designerHost != null)
            {
                designerHost.AddService(typeof(ISupportInSituService), this);
            } 

            componentChangeSvc = (IComponentChangeService)designerHost.GetService(typeof(IComponentChangeService)); 
 
            Debug.Assert(componentChangeSvc != null, "ToolStripKeyboardHandlingService relies on the componentChange service, which is unavailable.");
            if (componentChangeSvc != null) 
            {
                componentChangeSvc.ComponentRemoved += new ComponentEventHandler(OnComponentRemoved);
            }
        } 

        /// <include file='doc\ControlCommandSet.uex' path='docs/doc[@for="ControlCommandSet.Dispose"]/*' /> 
        /// <devdoc> 
        ///     Disposes of this object, removing all commands from the menu service.
        /// </devdoc> 
        public void Dispose() {
            if (toolDesigner != null)
            {
                toolDesigner.Dispose(); 
                toolDesigner = null;
            } 
            if (toolItemDesigner != null) 
            {
                toolItemDesigner.Dispose(); 
                toolItemDesigner = null;
            }
            if (componentChangeSvc != null)
            { 
                componentChangeSvc.ComponentRemoved -= new ComponentEventHandler(OnComponentRemoved);
                componentChangeSvc = null; 
            } 
        }
 

        private ToolStripKeyboardHandlingService ToolStripKeyBoardService {
            get {
                if (toolStripKeyBoardService == null) { 
                    toolStripKeyBoardService = (ToolStripKeyboardHandlingService)sp.GetService(typeof(ToolStripKeyboardHandlingService));
                } 
                return toolStripKeyBoardService; 
            }
        } 


        /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService.IgnoreMessages"]/*' />
        /// <devdoc> 
        ///      Returning true for IgnoreMessages means that this service is interested in getting the KeyBoard characters.
        /// </devdoc> 
        public bool IgnoreMessages { 
            get  {
                ISelectionService selectionService = (ISelectionService)sp.GetService(typeof(ISelectionService)); 
                IDesignerHost host = (IDesignerHost)sp.GetService(typeof(IDesignerHost));
                if (selectionService != null && host != null)
                {
                    IComponent comp = selectionService.PrimarySelection as IComponent; 
                    if (comp == null)
                    { 
                        comp = (IComponent)ToolStripKeyBoardService.SelectedDesignerControl; 
                    }
                    if (comp != null) 
                    {
                        DesignerToolStripControlHost c = comp as DesignerToolStripControlHost;
                        if (c != null)
                        { 

                            ToolStripDropDown dropDown = c.GetCurrentParent() as ToolStripDropDown; 
                            if (dropDown != null) 
                            {
                                ToolStripDropDownItem parentItem = dropDown.OwnerItem as ToolStripDropDownItem; 
                                if (parentItem != null)
                                {
                                    ToolStripOverflowButton parent = parentItem as ToolStripOverflowButton;
                                    if (parent != null) 
                                    {
                                        //return false ...  We are on overflow.. 
                                        return false; 
                                    }
                                    else { 
                                        toolItemDesigner = host.GetDesigner(parentItem) as ToolStripMenuItemDesigner;
                                        if (toolItemDesigner != null) {
                                            toolDesigner = null;
                                            return true; 
                                        }
                                    } 
                                } 
                            }
                            else { 
                                MenuStrip tool = c.GetCurrentParent() as MenuStrip;
                                if (tool != null)
                                {
                                    toolDesigner = host.GetDesigner(tool) as ToolStripDesigner; 
                                    if (toolDesigner != null) {
                                        toolItemDesigner = null; 
                                        return true; 
                                    }
                                } 
                            }
                        }
                        else if (comp is ToolStripDropDown) //case for ToolStripDropDown..
                        { 
                            ToolStripDropDownDesigner designer = host.GetDesigner(comp) as ToolStripDropDownDesigner;
                            if (designer != null) 
                            { 
                                ToolStripMenuItem toolItem = designer.DesignerMenuItem;
                                if (toolItem != null) 
                                {
                                    toolItemDesigner = host.GetDesigner(toolItem) as ToolStripItemDesigner;
                                    if (toolItemDesigner != null) {
                                        toolDesigner = null; 
                                        return true;
                                    } 
                                } 
                            }
                        } 
                        else if (comp is MenuStrip)
                        {
                           toolDesigner = host.GetDesigner(comp) as ToolStripDesigner;
                           if (toolDesigner != null) { 
                               toolItemDesigner = null;
                               return true; 
                           } 
                        }
                        else if (comp is ToolStripMenuItem){ 
                           toolItemDesigner = host.GetDesigner(comp) as ToolStripItemDesigner;
                           if (toolItemDesigner != null) {
                               toolDesigner = null;
                               return true; 
                           }
                        } 
                    } 
                }
                return false; 
            }
        }

        /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService.HandleKeyChar"]/*' /> 
        /// <devdoc>
        ///      This function is called on the service when the PBRSFORWARD gets the first WM_CHAR message. 
        /// </devdoc> 
        public void HandleKeyChar()
        { 
            if (toolDesigner != null || toolItemDesigner != null) {
                if (toolDesigner != null)
                {
                   toolDesigner.ShowEditNode(false); 
                }
                else if (toolItemDesigner != null) 
                { 
                    ToolStripMenuItemDesigner menuDesigner = toolItemDesigner as ToolStripMenuItemDesigner;
                    if (menuDesigner != null) 
                    {
                         ISelectionService selService = (ISelectionService)sp.GetService(typeof(ISelectionService));
                         if (selService != null) {
                            object comp = selService.PrimarySelection; 
                            if (comp == null)
                            { 
                                comp = ToolStripKeyBoardService.SelectedDesignerControl; 
                            }
                            DesignerToolStripControlHost designerItem = comp as DesignerToolStripControlHost; 
                            if (designerItem != null || comp is ToolStripDropDown)
                            {
                                 menuDesigner.EditTemplateNode(false);
                            } 
                            else
                            { 
                                menuDesigner.ShowEditNode(false); 
                            }
                         } 
                    }
                    else {
                        toolItemDesigner.ShowEditNode(false);
                    } 
                }
            } 
        } 

 
        /// <include file='doc\ToolStripInSituService.uex' path='docs/doc[@for="ToolStripInSituService.GetEditWindow"]/*' />
        /// <devdoc>
        ///      This function returns the Window handle that should get all the Keyboard messages.
        /// </devdoc> 
        public IntPtr GetEditWindow()
        { 
 
           IntPtr hWnd = IntPtr.Zero;
           if (toolDesigner != null && toolDesigner.Editor != null && toolDesigner.Editor.EditBox  != null) { 
               hWnd = (toolDesigner.Editor.EditBox.Visible) ? toolDesigner.Editor.EditBox.Handle : hWnd;
           }
           else if (toolItemDesigner != null  && toolItemDesigner.Editor != null && toolItemDesigner.Editor.EditBox  != null){
               hWnd = (toolItemDesigner.Editor.EditBox.Visible) ? toolItemDesigner.Editor.EditBox.Handle : hWnd; 
           }
           return hWnd; 
 
        }
 
        // Remove the Service when the last toolStrip is removed.
        private void OnComponentRemoved(object sender, ComponentEventArgs e)
        {
            bool toolStripPresent = false; 

            ComponentCollection comps = designerHost.Container.Components; 
            foreach (IComponent comp in comps) 
            {
                if (comp is ToolStrip) 
                {
                    toolStripPresent = true;
                    break;
                } 
            }
            if (!toolStripPresent) 
            { 
                ToolStripInSituService inSituService = (ToolStripInSituService)sp.GetService(typeof(ISupportInSituService));
                if (inSituService != null) 
                {
                    //since we are going away .. restore the old commands.
                    designerHost.RemoveService(typeof(ISupportInSituService));
                } 
            }
        } 
    } 
 }
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
