//------------------------------------------------------------------------------ 
// <copyright file="BaseContextMenu.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics; 
    using System;
    using System.Security; 
    using System.Security.Permissions; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Drawing;
    using System.Drawing.Design;
    using System.Windows.Forms.Design; 
    using System.Windows.Forms.Design.Behavior;
 
    /// <summary> 
    /// This class is going to replace the shell contextMenu and uses the ContextMenuStrip.
    /// The ContextMenuStrip contains groups and groupOrder which it uses to add items to itself. 
    /// ControlDesigners can add custom items to the contextMenu, using the new member to the
    /// group and add the groupOrder to the ContextMenu.
    /// </summary>
    internal class BaseContextMenuStrip : GroupedContextMenuStrip 
    {
        private IServiceProvider serviceProvider; 
        private Component component; 
        private ToolStripMenuItem selectionMenuItem;
 
        /// <summary>
        /// Constructor.
        /// </summary>
        public BaseContextMenuStrip(IServiceProvider provider, Component component) 
            : base()
        { 
            this.serviceProvider = provider; 
            this.component = component;
 


            // Now initialiaze the contextMenu
            InitializeContextMenu(); 
        }
 
        /// <summary> 
        ///  Helper function to add the "View Code" menuItem.
        /// </summary> 
        private void AddCodeMenuItem()
        {
            StandardCommandToolStripMenuItem codeMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.ViewCode, SR.GetString(SR.ContextMenuViewCode), "viewcode", serviceProvider);
            this.Groups[StandardGroups.Code].Items.Add(codeMenuItem); 
        }
 
        /// <summary> 
        ///  Helper function to add the "SendToBack/BringToFront" menuItem.
        /// </summary> 
        private void AddZorderMenuItem()
        {
            StandardCommandToolStripMenuItem ZOrderMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.BringToFront, SR.GetString(SR.ContextMenuBringToFront), "bringToFront", serviceProvider);
            this.Groups[StandardGroups.ZORder].Items.Add(ZOrderMenuItem); 

            ZOrderMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.SendToBack, SR.GetString(SR.ContextMenuSendToBack), "sendToBack", serviceProvider); 
            this.Groups[StandardGroups.ZORder].Items.Add(ZOrderMenuItem); 
        }
 
        /// <summary>
        ///  Helper function to add the "Alignment" menuItem.
        /// </summary>
        private void AddGridMenuItem() 
        {
            StandardCommandToolStripMenuItem gridMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.AlignToGrid, SR.GetString(SR.ContextMenuAlignToGrid), "alignToGrid", serviceProvider); 
            this.Groups[StandardGroups.Grid].Items.Add(gridMenuItem); 
        }
 
        /// <summary>
        ///  Helper function to add the "Locked" menuItem.
        /// </summary>
        private void AddLockMenuItem() 
        {
            StandardCommandToolStripMenuItem lockMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.LockControls, SR.GetString(SR.ContextMenuLockControls), "lockControls", serviceProvider); 
            this.Groups[StandardGroups.Lock].Items.Add(lockMenuItem); 
        }
 
        /// <summary>
        ///  Helper function to add the Select Parent menuItem.
        /// </summary>
        private void RefreshSelectionMenuItem() 
        {
 
 
            int index = -1;
            if (selectionMenuItem != null) 
            {
                index = this.Items.IndexOf(selectionMenuItem);
                this.Groups[StandardGroups.Selection].Items.Remove(selectionMenuItem);
                this.Items.Remove(selectionMenuItem); 
            }
 
 
            ArrayList parentControls = new ArrayList();
            int nParentControls = 0; 

            //
            // Get the currently selected Control
            ISelectionService selectionService = serviceProvider.GetService(typeof(ISelectionService)) as ISelectionService; 
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
            if (selectionService != null && host != null) 
            { 
                IComponent root = host.RootComponent;
                Debug.Assert(root != null, "Null root component. Will be unable to build selection menu"); 
                Control selectedControl = selectionService.PrimarySelection as Control;
                if (selectedControl != null && root != null && selectedControl != root)
                {
                    Control parentControl = selectedControl.Parent; 
                    while (parentControl != null)
                    { 
                        if (parentControl.Site != null) 
                        {
                            parentControls.Add(parentControl); 
                            nParentControls++;
                        }
                        if (parentControl == root)
                        { 
                            break;
                        } 
 
                        parentControl = parentControl.Parent;
                    } 
                }
                else if (selectionService.PrimarySelection is ToolStripItem)
                {
                    ToolStripItem selectedItem = selectionService.PrimarySelection as ToolStripItem; 
                    ToolStripItemDesigner itemDesigner = host.GetDesigner(selectedItem) as ToolStripItemDesigner;
                    if (itemDesigner != null) 
                    { 
                        parentControls = itemDesigner.AddParentTree();
                        nParentControls = parentControls.Count; 
                    }
                }

            } 
            if (nParentControls > 0)
            { 
                selectionMenuItem = new ToolStripMenuItem(); 

                IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService; 
                if (uis != null) {
                  selectionMenuItem.DropDown.Renderer = (ToolStripProfessionalRenderer)uis.Styles["VsRenderer"];
                  //Set the right Font
                  selectionMenuItem.DropDown.Font = (Font)uis.Styles["DialogFont"]; 
                }
 
                selectionMenuItem.Text = SR.GetString(SR.ContextMenuSelect); 
                foreach (Component parent in parentControls)
                { 
                    ToolStripMenuItem selectListItem = new SelectToolStripMenuItem(parent, serviceProvider);
                    selectionMenuItem.DropDownItems.Add(selectListItem);
                }
                this.Groups[StandardGroups.Selection].Items.Add(selectionMenuItem); 
                // Re add the newly refreshed item..
                if (index != -1) 
                { 
                    this.Items.Insert(index, selectionMenuItem);
                } 
            }
        }

 
        /// <summary>
        ///  Helper function to add the Verbs. 
        /// </summary> 
        private void AddVerbMenuItem()
        { 
            //Add Designer Verbs..
            IMenuCommandService menuCommandService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService));
            if (menuCommandService != null)
            { 
                DesignerVerbCollection verbCollection = menuCommandService.Verbs;
                foreach (DesignerVerb verb in verbCollection) 
                { 
                    DesignerVerbToolStripMenuItem verbItem = new DesignerVerbToolStripMenuItem(verb);
                    this.Groups[StandardGroups.Verbs].Items.Add(verbItem); 
                }
            }
        }
 
        /// <summary>
        ///  Helper function to add the "Cut/Copy/Paste/Delete" menuItem. 
        /// </summary> 
        private void AddEditMenuItem()
        { 
            StandardCommandToolStripMenuItem stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.Cut, SR.GetString(SR.ContextMenuCut), "cut", serviceProvider);
            this.Groups[StandardGroups.Edit].Items.Add(stdMenuItem);

 
            stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.Copy, SR.GetString(SR.ContextMenuCopy), "copy", serviceProvider);
            this.Groups[StandardGroups.Edit].Items.Add(stdMenuItem); 
 

            stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.Paste, SR.GetString(SR.ContextMenuPaste), "paste", serviceProvider); 
            this.Groups[StandardGroups.Edit].Items.Add(stdMenuItem);

            stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.Delete, SR.GetString(SR.ContextMenuDelete), "delete", serviceProvider);
            this.Groups[StandardGroups.Edit].Items.Add(stdMenuItem); 

        } 
 
        /// <summary>
        ///  Helper function to add the "Properties" menuItem. 
        /// </summary>
        private void AddPropertiesMenuItem()
        {
            StandardCommandToolStripMenuItem stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.DocumentOutline, SR.GetString(SR.ContextMenuDocumentOutline), "", serviceProvider); 
            this.Groups[StandardGroups.Properties].Items.Add(stdMenuItem);
 
            stdMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.DesignerProperties, SR.GetString(SR.ContextMenuProperties), "properties", serviceProvider); 
            this.Groups[StandardGroups.Properties].Items.Add(stdMenuItem);
        } 


        /// <summary>
        ///  Basic Initialize method. 
        /// </summary>
        private void InitializeContextMenu() 
        { 
            //this.Opening += new CancelEventHandler(OnContextMenuOpening);
            this.Name = "designerContextMenuStrip"; 

            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService;
            if (uis != null) {
              this.Renderer = (ToolStripProfessionalRenderer)uis.Styles["VsRenderer"]; 
            }
 
            GroupOrdering.AddRange(new string[] { StandardGroups.Code, 
                                               StandardGroups.ZORder,
                                               StandardGroups.Grid, 
                                               StandardGroups.Lock,
                                               StandardGroups.Verbs,
                                               StandardGroups.Custom,
                                               StandardGroups.Selection, 
                                               StandardGroups.Edit,
                                               StandardGroups.Properties}); 
            // ADD MENUITEMS 
            AddCodeMenuItem();
            AddZorderMenuItem(); 
            AddGridMenuItem();
            AddLockMenuItem();
            AddVerbMenuItem();
            RefreshSelectionMenuItem(); 
            AddEditMenuItem();
            AddPropertiesMenuItem(); 
        } 

 
        /// <summary>
        ///  Public function that allows the individual MenuItems to get refreshed each time the ContextMenu is opened.
        /// </summary>
        public override void RefreshItems() 
        {
            //Set the right Font 
            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService; 
            if (uis != null) {
              this.Font = (Font)uis.Styles["DialogFont"]; 
            }

            foreach (ToolStripItem item in this.Items)
            { 
                StandardCommandToolStripMenuItem stdItem = item as StandardCommandToolStripMenuItem;
                if (stdItem != null) 
                { 
                    stdItem.RefreshItem();
                } 
            }
            RefreshSelectionMenuItem();
        }
 
        /// <summary>
        ///  A ToolStripMenuItem that gets added for the "Select" menuitem. 
        /// </summary> 
        private class SelectToolStripMenuItem : ToolStripMenuItem
        { 
            private Component comp;
            private IServiceProvider serviceProvider;
            private Type _itemType;
            private bool _cachedImage = false; 
            private Image _image = null;
 
            private static string systemWindowsFormsNamespace = typeof(System.Windows.Forms.ToolStripItem).Namespace; 

            public SelectToolStripMenuItem(Component c, IServiceProvider provider) 
            {
                comp = c;
                serviceProvider = provider;
                // Get NestedSiteName... 
                string compName = null;
                if (comp != null) 
                { 
                    ISite site = comp.Site;
                    if (site != null) 
                    {
                        INestedSite nestedSite = site as INestedSite;
                        if (nestedSite != null && !string.IsNullOrEmpty(nestedSite.FullName))
                        { 
                            compName = nestedSite.FullName;
                        } 
                        else if (!string.IsNullOrEmpty(site.Name)) 
                        {
                            compName = site.Name; 
                        }
                    }
                }
                this.Text = SR.GetString(SR.ToolStripSelectMenuItem, compName); 
                this._itemType = c.GetType();
            } 
 
            public override Image Image
            { 
                get
                {
                    // Defer loading the image until we're sure we need it
                    if (!_cachedImage) 
                    {
                        _cachedImage = true; 
                        // give the toolbox item attribute the first shot 
                        ToolboxItem tbxItem = ToolboxService.GetToolboxItem(_itemType);
                        if (tbxItem != null) 
                        {
                            _image = tbxItem.Bitmap;
                        }
                        else 
                        {
                            // else attempt to get the resource from a known place in the manifest 
                            // if and only if the namespace of the type is System.Windows.Forms. 
                            // else attempt to get the resource from a known place in the manifest
                            if (_itemType.Namespace == systemWindowsFormsNamespace) 
                            {
                                _image = ToolboxBitmapAttribute.GetImageFromResource(_itemType, null, false);
                            }
                        } 

                        // if all else fails, throw up a default image. 
                        if (_image == null) 
                        {
                            _image = ToolboxBitmapAttribute.GetImageFromResource(comp.GetType(), null, false); 
                        }

                    }
                    return _image; 
                }
                set 
                { 
                    _image = value;
                    _cachedImage = true; 
                }
            }

            /// <summary> 
            ///  Items OnClick event, to select the Parent Control.
            /// </summary> 
            protected override void OnClick(System.EventArgs e) 
            {
                ISelectionService selectionService = serviceProvider.GetService(typeof(ISelectionService)) as ISelectionService; 
                if (selectionService != null)
                {
                    selectionService.SetSelectedComponents(new object[] { comp }, SelectionTypes.Replace);
                } 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BaseContextMenu.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics; 
    using System;
    using System.Security; 
    using System.Security.Permissions; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Drawing;
    using System.Drawing.Design;
    using System.Windows.Forms.Design; 
    using System.Windows.Forms.Design.Behavior;
 
    /// <summary> 
    /// This class is going to replace the shell contextMenu and uses the ContextMenuStrip.
    /// The ContextMenuStrip contains groups and groupOrder which it uses to add items to itself. 
    /// ControlDesigners can add custom items to the contextMenu, using the new member to the
    /// group and add the groupOrder to the ContextMenu.
    /// </summary>
    internal class BaseContextMenuStrip : GroupedContextMenuStrip 
    {
        private IServiceProvider serviceProvider; 
        private Component component; 
        private ToolStripMenuItem selectionMenuItem;
 
        /// <summary>
        /// Constructor.
        /// </summary>
        public BaseContextMenuStrip(IServiceProvider provider, Component component) 
            : base()
        { 
            this.serviceProvider = provider; 
            this.component = component;
 


            // Now initialiaze the contextMenu
            InitializeContextMenu(); 
        }
 
        /// <summary> 
        ///  Helper function to add the "View Code" menuItem.
        /// </summary> 
        private void AddCodeMenuItem()
        {
            StandardCommandToolStripMenuItem codeMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.ViewCode, SR.GetString(SR.ContextMenuViewCode), "viewcode", serviceProvider);
            this.Groups[StandardGroups.Code].Items.Add(codeMenuItem); 
        }
 
        /// <summary> 
        ///  Helper function to add the "SendToBack/BringToFront" menuItem.
        /// </summary> 
        private void AddZorderMenuItem()
        {
            StandardCommandToolStripMenuItem ZOrderMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.BringToFront, SR.GetString(SR.ContextMenuBringToFront), "bringToFront", serviceProvider);
            this.Groups[StandardGroups.ZORder].Items.Add(ZOrderMenuItem); 

            ZOrderMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.SendToBack, SR.GetString(SR.ContextMenuSendToBack), "sendToBack", serviceProvider); 
            this.Groups[StandardGroups.ZORder].Items.Add(ZOrderMenuItem); 
        }
 
        /// <summary>
        ///  Helper function to add the "Alignment" menuItem.
        /// </summary>
        private void AddGridMenuItem() 
        {
            StandardCommandToolStripMenuItem gridMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.AlignToGrid, SR.GetString(SR.ContextMenuAlignToGrid), "alignToGrid", serviceProvider); 
            this.Groups[StandardGroups.Grid].Items.Add(gridMenuItem); 
        }
 
        /// <summary>
        ///  Helper function to add the "Locked" menuItem.
        /// </summary>
        private void AddLockMenuItem() 
        {
            StandardCommandToolStripMenuItem lockMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.LockControls, SR.GetString(SR.ContextMenuLockControls), "lockControls", serviceProvider); 
            this.Groups[StandardGroups.Lock].Items.Add(lockMenuItem); 
        }
 
        /// <summary>
        ///  Helper function to add the Select Parent menuItem.
        /// </summary>
        private void RefreshSelectionMenuItem() 
        {
 
 
            int index = -1;
            if (selectionMenuItem != null) 
            {
                index = this.Items.IndexOf(selectionMenuItem);
                this.Groups[StandardGroups.Selection].Items.Remove(selectionMenuItem);
                this.Items.Remove(selectionMenuItem); 
            }
 
 
            ArrayList parentControls = new ArrayList();
            int nParentControls = 0; 

            //
            // Get the currently selected Control
            ISelectionService selectionService = serviceProvider.GetService(typeof(ISelectionService)) as ISelectionService; 
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
            if (selectionService != null && host != null) 
            { 
                IComponent root = host.RootComponent;
                Debug.Assert(root != null, "Null root component. Will be unable to build selection menu"); 
                Control selectedControl = selectionService.PrimarySelection as Control;
                if (selectedControl != null && root != null && selectedControl != root)
                {
                    Control parentControl = selectedControl.Parent; 
                    while (parentControl != null)
                    { 
                        if (parentControl.Site != null) 
                        {
                            parentControls.Add(parentControl); 
                            nParentControls++;
                        }
                        if (parentControl == root)
                        { 
                            break;
                        } 
 
                        parentControl = parentControl.Parent;
                    } 
                }
                else if (selectionService.PrimarySelection is ToolStripItem)
                {
                    ToolStripItem selectedItem = selectionService.PrimarySelection as ToolStripItem; 
                    ToolStripItemDesigner itemDesigner = host.GetDesigner(selectedItem) as ToolStripItemDesigner;
                    if (itemDesigner != null) 
                    { 
                        parentControls = itemDesigner.AddParentTree();
                        nParentControls = parentControls.Count; 
                    }
                }

            } 
            if (nParentControls > 0)
            { 
                selectionMenuItem = new ToolStripMenuItem(); 

                IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService; 
                if (uis != null) {
                  selectionMenuItem.DropDown.Renderer = (ToolStripProfessionalRenderer)uis.Styles["VsRenderer"];
                  //Set the right Font
                  selectionMenuItem.DropDown.Font = (Font)uis.Styles["DialogFont"]; 
                }
 
                selectionMenuItem.Text = SR.GetString(SR.ContextMenuSelect); 
                foreach (Component parent in parentControls)
                { 
                    ToolStripMenuItem selectListItem = new SelectToolStripMenuItem(parent, serviceProvider);
                    selectionMenuItem.DropDownItems.Add(selectListItem);
                }
                this.Groups[StandardGroups.Selection].Items.Add(selectionMenuItem); 
                // Re add the newly refreshed item..
                if (index != -1) 
                { 
                    this.Items.Insert(index, selectionMenuItem);
                } 
            }
        }

 
        /// <summary>
        ///  Helper function to add the Verbs. 
        /// </summary> 
        private void AddVerbMenuItem()
        { 
            //Add Designer Verbs..
            IMenuCommandService menuCommandService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService));
            if (menuCommandService != null)
            { 
                DesignerVerbCollection verbCollection = menuCommandService.Verbs;
                foreach (DesignerVerb verb in verbCollection) 
                { 
                    DesignerVerbToolStripMenuItem verbItem = new DesignerVerbToolStripMenuItem(verb);
                    this.Groups[StandardGroups.Verbs].Items.Add(verbItem); 
                }
            }
        }
 
        /// <summary>
        ///  Helper function to add the "Cut/Copy/Paste/Delete" menuItem. 
        /// </summary> 
        private void AddEditMenuItem()
        { 
            StandardCommandToolStripMenuItem stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.Cut, SR.GetString(SR.ContextMenuCut), "cut", serviceProvider);
            this.Groups[StandardGroups.Edit].Items.Add(stdMenuItem);

 
            stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.Copy, SR.GetString(SR.ContextMenuCopy), "copy", serviceProvider);
            this.Groups[StandardGroups.Edit].Items.Add(stdMenuItem); 
 

            stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.Paste, SR.GetString(SR.ContextMenuPaste), "paste", serviceProvider); 
            this.Groups[StandardGroups.Edit].Items.Add(stdMenuItem);

            stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.Delete, SR.GetString(SR.ContextMenuDelete), "delete", serviceProvider);
            this.Groups[StandardGroups.Edit].Items.Add(stdMenuItem); 

        } 
 
        /// <summary>
        ///  Helper function to add the "Properties" menuItem. 
        /// </summary>
        private void AddPropertiesMenuItem()
        {
            StandardCommandToolStripMenuItem stdMenuItem = new StandardCommandToolStripMenuItem(StandardCommands.DocumentOutline, SR.GetString(SR.ContextMenuDocumentOutline), "", serviceProvider); 
            this.Groups[StandardGroups.Properties].Items.Add(stdMenuItem);
 
            stdMenuItem = new StandardCommandToolStripMenuItem(MenuCommands.DesignerProperties, SR.GetString(SR.ContextMenuProperties), "properties", serviceProvider); 
            this.Groups[StandardGroups.Properties].Items.Add(stdMenuItem);
        } 


        /// <summary>
        ///  Basic Initialize method. 
        /// </summary>
        private void InitializeContextMenu() 
        { 
            //this.Opening += new CancelEventHandler(OnContextMenuOpening);
            this.Name = "designerContextMenuStrip"; 

            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService;
            if (uis != null) {
              this.Renderer = (ToolStripProfessionalRenderer)uis.Styles["VsRenderer"]; 
            }
 
            GroupOrdering.AddRange(new string[] { StandardGroups.Code, 
                                               StandardGroups.ZORder,
                                               StandardGroups.Grid, 
                                               StandardGroups.Lock,
                                               StandardGroups.Verbs,
                                               StandardGroups.Custom,
                                               StandardGroups.Selection, 
                                               StandardGroups.Edit,
                                               StandardGroups.Properties}); 
            // ADD MENUITEMS 
            AddCodeMenuItem();
            AddZorderMenuItem(); 
            AddGridMenuItem();
            AddLockMenuItem();
            AddVerbMenuItem();
            RefreshSelectionMenuItem(); 
            AddEditMenuItem();
            AddPropertiesMenuItem(); 
        } 

 
        /// <summary>
        ///  Public function that allows the individual MenuItems to get refreshed each time the ContextMenu is opened.
        /// </summary>
        public override void RefreshItems() 
        {
            //Set the right Font 
            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService; 
            if (uis != null) {
              this.Font = (Font)uis.Styles["DialogFont"]; 
            }

            foreach (ToolStripItem item in this.Items)
            { 
                StandardCommandToolStripMenuItem stdItem = item as StandardCommandToolStripMenuItem;
                if (stdItem != null) 
                { 
                    stdItem.RefreshItem();
                } 
            }
            RefreshSelectionMenuItem();
        }
 
        /// <summary>
        ///  A ToolStripMenuItem that gets added for the "Select" menuitem. 
        /// </summary> 
        private class SelectToolStripMenuItem : ToolStripMenuItem
        { 
            private Component comp;
            private IServiceProvider serviceProvider;
            private Type _itemType;
            private bool _cachedImage = false; 
            private Image _image = null;
 
            private static string systemWindowsFormsNamespace = typeof(System.Windows.Forms.ToolStripItem).Namespace; 

            public SelectToolStripMenuItem(Component c, IServiceProvider provider) 
            {
                comp = c;
                serviceProvider = provider;
                // Get NestedSiteName... 
                string compName = null;
                if (comp != null) 
                { 
                    ISite site = comp.Site;
                    if (site != null) 
                    {
                        INestedSite nestedSite = site as INestedSite;
                        if (nestedSite != null && !string.IsNullOrEmpty(nestedSite.FullName))
                        { 
                            compName = nestedSite.FullName;
                        } 
                        else if (!string.IsNullOrEmpty(site.Name)) 
                        {
                            compName = site.Name; 
                        }
                    }
                }
                this.Text = SR.GetString(SR.ToolStripSelectMenuItem, compName); 
                this._itemType = c.GetType();
            } 
 
            public override Image Image
            { 
                get
                {
                    // Defer loading the image until we're sure we need it
                    if (!_cachedImage) 
                    {
                        _cachedImage = true; 
                        // give the toolbox item attribute the first shot 
                        ToolboxItem tbxItem = ToolboxService.GetToolboxItem(_itemType);
                        if (tbxItem != null) 
                        {
                            _image = tbxItem.Bitmap;
                        }
                        else 
                        {
                            // else attempt to get the resource from a known place in the manifest 
                            // if and only if the namespace of the type is System.Windows.Forms. 
                            // else attempt to get the resource from a known place in the manifest
                            if (_itemType.Namespace == systemWindowsFormsNamespace) 
                            {
                                _image = ToolboxBitmapAttribute.GetImageFromResource(_itemType, null, false);
                            }
                        } 

                        // if all else fails, throw up a default image. 
                        if (_image == null) 
                        {
                            _image = ToolboxBitmapAttribute.GetImageFromResource(comp.GetType(), null, false); 
                        }

                    }
                    return _image; 
                }
                set 
                { 
                    _image = value;
                    _cachedImage = true; 
                }
            }

            /// <summary> 
            ///  Items OnClick event, to select the Parent Control.
            /// </summary> 
            protected override void OnClick(System.EventArgs e) 
            {
                ISelectionService selectionService = serviceProvider.GetService(typeof(ISelectionService)) as ISelectionService; 
                if (selectionService != null)
                {
                    selectionService.SetSelectedComponents(new object[] { comp }, SelectionTypes.Replace);
                } 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
