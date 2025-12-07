//------------------------------------------------------------------------------ 
// <copyright file="ToolStripTemplateNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.Security; 
    using System.Security.Permissions; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design;
    using System.Windows.Forms.Design.Behavior;
    using System.Runtime.InteropServices; 
    using System.Drawing.Drawing2D;
 
 
    /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode"]/*' />
    /// <devdoc> 
    ///     This internal class wraps the InSitu Editor. The editor is a runtime Winbar
    ///     control which contains a leftButton (for image), centerLabel (for text) which
    ///     gets swaped by a centerTextBox (when InSitu is ON).
    /// 
    ///     The ToolStripTemplateNode is also responsible for intercepting the Escape and Enter keys
    ///     and implements the IMenuStatusHandler so that it can commit and rollback as required. 
    /// 
    ///     Finally this ToolStripTemplateNode has a private class ItemTypeToolStripMenuItem for adding
    ///     ToolStripItem types to the Dropdown for addItemButton. 
    ///
    /// </devdoc>
    internal class ToolStripTemplateNode : IMenuStatusHandler
    { 

        private const int GLYPHBORDER = 1; 
        private const int GLYPHINSET = 2; 

        // 
        // Component for this InSitu Editor... (this is a ToolStripItem)
        // that wants to go into InSitu
        //
        private IComponent component; 

        // 
        // Current Designer for the comopenent that in InSitu mode 
        //
        private IDesigner _designer = null; 

        //Get DesignerHost.
        private IDesignerHost _designerHost = null;
 
        //
        // Menu Commands to override 
        // 
        private MenuCommand[] commands;
 
        //
        // MenuCommands to Add
        //
        private MenuCommand[] addCommands; 

        // 
        // Actual InSitu Editor and its components... 
        //
        private TransparentToolStrip _miniToolStrip; 

        //
        // Center Label for MenuStrip TemplateNode
        // 
        private ToolStripLabel centerLabel;
 
        // SplitButton reAdded for ToolStrip specific TemplateNode 
        private ToolStripSplitButton addItemButton;
 
        //swaped in text...
        private ToolStripControlHost centerTextBox;

        //reqd as rtb does accept Enter.. 
        internal bool ignoreFirstKeyUp = false;
 
        // 
        // This is the Bounding Rectangle for the ToolStripTemplateNode. This is set
        // by the itemDesigner in terms of the "AdornerWindow" bounds. 
        // The ToolStripEditorManager uses this Bounds to actually activate the
        // editor on the AdornerWindow.
        //
        private Rectangle boundingRect; 

        // 
        // Keeps track of Insitu Mode. 
        //
        private bool inSituMode = false; 

        //
        // Tells whether the editorNode is listening to Menu commands.
        // 
        private bool active = false;
 
        // 
        // Need to keep a track of Last Selection to uncheck it.
        // This is the Checked property on ToolStripItems on the Menu. We check this 
        // cached in value to the current Selection on the addItemButton and if different
        // then uncheck the Checked for this lastSelection.. Check for the currentSelection
        // and finally save the currentSelection as the lastSelection for future check.
        // 
        private ItemTypeToolStripMenuItem lastSelection = null;
 
        // This is the renderer used to Draw the Strips..... 
        private MiniToolStripRenderer renderer;
 
        // This is the Type that the user has selected for the new Item
        private Type itemType;

        //Get the ToolStripKeyBoardService to notify that the TemplateNode is Active and so it shouldnt process the KeyMessages. 
        private ToolStripKeyboardHandlingService toolStripKeyBoardService;
 
        //Cached ISelectionService 
        private ISelectionService selectionService;
 
        //Cached BehaviorService
        private BehaviorService behaviorService;

        //ControlHost for selection on mouseclicks 
        DesignerToolStripControlHost controlHost = null;
 
        // On DropDowns the component passed in is the parent (ownerItem) and hence we need the 
        // reference for actual item
        ToolStripItem activeItem = null; 

        //
        //Event
        // 
        EventHandler onActivated;
 
        // 
        //Event
        // 
        EventHandler onClosed;

        //
        //Event 
        //
        EventHandler onDeactivated; 
 
        // Old Undo/Redo Commands
        private MenuCommand oldUndoCommand = null; 
        private MenuCommand oldRedoCommand = null;

        // The DropDown for the TemplateNode
        private ToolStripDropDown contextMenu; 

        // the Hot Region within the templateNode ... this is used for the menustrips 
        private Rectangle hotRegion; 

        // when a dummyItem is added we set this property to that item so that Selection works .. 
        // If we set the selection before we get redundant calls to "DropDownResize" and hence cause a lot of flicker.
        // private ToolStripItem startItemForSelection;

        private bool imeModeSet = false; 

        //DesignSurface to hook up to the Flushed event 
        private DesignSurface _designSurface= null; 

 
        // Is system context menu displayed for the insitu text box?
        private bool isSystemContextMenuDisplayed = false;

        // 
        // Constructor
        // 
        /// <include file='doc\ToolStripToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripToolStripTemplateNode.ToolStripTemplateNode"]/*' /> 
        public ToolStripTemplateNode(IComponent component, string text, Image image)
        { 
            this.component = component;

            // In most of the cases this is true; except for ToolStripItems on DropDowns.
            // the toolstripMenuItemDesigners sets the public property in those cases. 
            this.activeItem = component as ToolStripItem;
 
            _designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 
            _designer = _designerHost.GetDesigner(component);
            _designSurface = (DesignSurface)component.Site.GetService(typeof(DesignSurface)); 
            _designSurface.Flushed += new EventHandler(OnLoaderFlushed);

            //Setup EditNode
            //ToolStripItem item = component as ToolStripItem; 
            SetupNewEditNode(this, text, image, component);
 
 
            commands = new MenuCommand[] {
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyMoveUp), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyMoveDown),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyMoveLeft),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyMoveRight),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Delete), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Cut),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Copy), 
 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeUp),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeDown), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeLeft),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeRight),

                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeySizeWidthIncrease), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeySizeHeightIncrease),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeySizeWidthDecrease), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeySizeHeightDecrease), 

                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeWidthIncrease), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeHeightIncrease),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeWidthDecrease),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeHeightDecrease)
 
            };
 
 
            addCommands = new MenuCommand[] {
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Undo), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Redo)
            };

        } 

 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Active"]/*' /> 
        /// <devdoc>
        ///    This property enables / disables Menu Command Handler. 
        /// </devdoc>
        /// <internalonly/>
        public bool Active
        { 
            get
            { 
                return active; 
            }
            set 
            {
                if (active != value)
                {
                    active = value; 

                    if (KeyboardService != null) 
                    { 
                        KeyboardService.TemplateNodeActive = value;
                    } 


                    if (active)
                    { 
                        //Active.. Fire Activated
                        OnActivated(new EventArgs()); 
 
                        if (KeyboardService != null)
                        { 
                            KeyboardService.ActiveTemplateNode = this;
                        }

                        IMenuCommandService menuService = (IMenuCommandService)component.Site.GetService(typeof(IMenuCommandService)); 

                        if (menuService != null) 
                        { 
                            oldUndoCommand = menuService.FindCommand(MenuCommands.Undo);
                            if (oldUndoCommand != null) 
                            {
                                menuService.RemoveCommand(oldUndoCommand);
                            }
 
                            oldRedoCommand = menuService.FindCommand(MenuCommands.Redo);
                            if (oldRedoCommand != null) 
                            { 
                                menuService.RemoveCommand(oldRedoCommand);
                            } 

                            // Disable the Commands
                            for (int i = 0; i < addCommands.Length; i++)
                            { 
                                addCommands[i].Enabled = false;
                                menuService.AddCommand(addCommands[i]); 
                            } 
                        }
 
                        // Listen to command and key events
                        //
                        IEventHandlerService ehs = (IEventHandlerService)component.Site.GetService(typeof(IEventHandlerService));
                        if (ehs != null) 
                        {
                            ehs.PushHandler(this); 
                        } 
                    }
                    else 
                    {

                        //Active == false.. Fire Deactivated
                        OnDeactivated(new EventArgs()); 

                        if (KeyboardService != null) 
                        { 
                            KeyboardService.ActiveTemplateNode = null;
                        } 

                        IMenuCommandService menuService = (IMenuCommandService)component.Site.GetService(typeof(IMenuCommandService));

                        if (menuService != null) 
                        {
                            for (int i = 0; i < addCommands.Length; i++) 
                            { 
                                menuService.RemoveCommand(addCommands[i]);
                            } 
                        }

                        if (oldUndoCommand != null)
                        { 
                            menuService.AddCommand(oldUndoCommand);
                        } 
 
                        if (oldRedoCommand != null)
                        { 
                            menuService.AddCommand(oldRedoCommand);
                        }

                        // Stop listening to command and key events 
                        IEventHandlerService ehs = (IEventHandlerService)component.Site.GetService(typeof(IEventHandlerService));
                        if (ehs != null) 
                        { 
                            ehs.PopHandler(this);
                        } 
                    }
                }
            }
        } 

        // Need to have a reference of the actual item that is edited. 
        public ToolStripItem ActiveItem 
        {
            get 
            {
                return activeItem;
            }
            set 
            {
                activeItem = value; 
            } 

        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Activated"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public event EventHandler Activated 
        { 
            add
            { 
                this.onActivated += value;
            }
            remove
            { 
                this.onActivated -= value;
            } 
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Bounds"]/*' /> 
        /// <devdoc>
        ///    Returns the Bounds of this ToolStripTemplateNode.
        /// </devdoc>
        /// <internalonly/> 
        public Rectangle Bounds
        { 
            get 
            {
                return boundingRect; 
            }
            set
            {
                this.boundingRect = value; 
            }
        } 
 
        /// <devdoc>
        ///    The Warpper ControlHost .. 
        /// </devdoc>
        public DesignerToolStripControlHost ControlHost
        {
            get 
            {
                return controlHost; 
            } 
            set
            { 
                controlHost = value;
            }
        }
 
        /// <devdoc>
        ///   This is the designer contextMenu that pops when rightclicked on the TemplateNode. 
        /// </devdoc> 
        private ContextMenuStrip DesignerContextMenu
        { 
            get
            {

                BaseContextMenuStrip templateNodeContextMenu = new BaseContextMenuStrip(component.Site, controlHost); 
                templateNodeContextMenu.Populated = false;
                templateNodeContextMenu.GroupOrdering.Clear(); 
                templateNodeContextMenu.GroupOrdering.AddRange(new string[] { StandardGroups.Code, 
                                                                          StandardGroups.Custom,
                                                                          StandardGroups.Selection, 
                                                                          StandardGroups.Edit });
                templateNodeContextMenu.Text = "CustomContextMenu";

                TemplateNodeCustomMenuItemCollection templateNodeCustomMenuItemCollection = new TemplateNodeCustomMenuItemCollection(component.Site, controlHost); 
                foreach (ToolStripItem item in templateNodeCustomMenuItemCollection)
                { 
                    templateNodeContextMenu.Groups[StandardGroups.Custom].Items.Add(item); 
                }
 

                return templateNodeContextMenu;
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Deactivated"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public event EventHandler Deactivated
        {
            add
            { 
                this.onDeactivated += value;
            } 
            remove 
            {
                this.onDeactivated -= value; 
            }
        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Closed"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public event EventHandler Closed
        { 
            add
            {
                this.onClosed += value;
            } 
            remove
            { 
                this.onClosed -= value; 
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.EditorToolStrip"]/*' />
        /// <devdoc>
        ///    This property returns the actual editor ToolStrip. 
        /// </devdoc>
        /// <internalonly/> 
        public ToolStrip EditorToolStrip 
        {
            get 
            {
                return _miniToolStrip;

            } 
        }
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.EditorToolStrip"]/*' /> 
        /// <devdoc>
        ///    This property returns the actual editor ToolStrip. 
        /// </devdoc>
        /// <internalonly/>
        internal TextBox EditBox
        { 
            get
            { 
                return (centerTextBox != null) ? (TextBox)centerTextBox.Control : null; 

            } 
        }

        /// <devdoc>
        ///    HotRegion within the templateNode. this is the region which responds to the mouse. 
        /// </devdoc>
        public Rectangle HotRegion 
        { 
            get
            { 
                return hotRegion;
            }
            set
            { 
                hotRegion = value;
            } 
 
        }
 
        /// <devdoc>
        ///   value to suggest if IME mode is set.
        /// </devdoc>
        public bool IMEModeSet 
        {
            get 
            { 
                return imeModeSet;
            } 
            set
            {
                imeModeSet = value;
            } 
        }
 
        /// <devdoc> 
        ///    KeyBoardHandling service.
        /// </devdoc> 
        private ToolStripKeyboardHandlingService KeyboardService
        {
            get
            { 
                if (toolStripKeyBoardService == null)
                { 
                    toolStripKeyBoardService = (ToolStripKeyboardHandlingService)component.Site.GetService(typeof(ToolStripKeyboardHandlingService)); 
                }
                return toolStripKeyBoardService; 
            }
        }

 

        /// <devdoc> 
        ///    SelectionService. 
        /// </devdoc>
        private ISelectionService SelectionService 
        {
            get
            {
                if (selectionService == null) 
                {
                    selectionService = (ISelectionService)component.Site.GetService(typeof(ISelectionService)); 
                } 
                return selectionService;
            } 
        }


        private BehaviorService BehaviorService 
        {
            get 
            { 
                if (behaviorService == null)
                { 
                    behaviorService = (BehaviorService)component.Site.GetService(typeof(BehaviorService));
                }
                return behaviorService;
            } 
        }
 
 
        /// <devdoc>
        ///    Type of the new Item to be added. 
        /// </devdoc>
        public Type ToolStripItemType
        {
            get 
            {
                return itemType; 
            } 
            set
            { 
                itemType = value;
            }
        }
 
        /// <devdoc>
        ///    Is system context menu for the insitu edit box displayed?. 
        /// </devdoc> 
        internal bool IsSystemContextMenuDisplayed
        { 
            get
            {
                return isSystemContextMenuDisplayed;
            } 
            set
            { 
                isSystemContextMenuDisplayed = value; 
            }
        } 


        ////////////////////////////////////////////////////////////////////////////////////
        ////                                                                //// 
        ////                          Methods                               ////
        ////                                                                //// 
        //////////////////////////////////////////////////////////////////////////////////// 

        /// <devdoc> 
        ///    Helper function to add new Item when the DropDownItem (in the ToolStripTemplateNode) is clicked
        /// </devdoc>
        private void AddNewItemClick(object sender, EventArgs e)
        { 
            // Close the DropDown.. Important for Morphing ....
            if (addItemButton != null) 
            { 
                addItemButton.DropDown.Visible = false;
            } 

            if (component is ToolStrip && SelectionService != null)
            {
                // Stop the Designer from closing the Overflow if its open 
                ToolStripDesigner designer = _designerHost.GetDesigner(component) as ToolStripDesigner;
                try 
                { 
                    if (designer != null)
                    { 
                        designer.DontCloseOverflow = true;
                    }
                    SelectionService.SetSelectedComponents(new object[] { component });
                } 
                finally
                { 
                    if (designer != null) 
                    {
                        designer.DontCloseOverflow = false; 
                    }
                }
            }
 
            ItemTypeToolStripMenuItem senderItem = (ItemTypeToolStripMenuItem)sender;
            if (lastSelection != null) 
            { 
                lastSelection.Checked = false;
            } 

            // set the appropriate Checked state
            senderItem.Checked = true;
            lastSelection = senderItem; 

            // Set the property used in the CommitEditor (.. ) to add the correct Type. 
            ToolStripItemType = senderItem.ItemType; 

            //Select the parent before adding 
            ToolStrip parent = controlHost.GetCurrentParent() as ToolStrip;

            // this will add the item to the ToolStrip..
            if (parent is MenuStrip) 
            {
                CommitEditor(true, true, false); 
            } 
            else
            { 
                // In case of toolStrips/StatusStrip we want the currently added item to be selected instead of selecting the next item
                CommitEditor(true, false, false);
            }
 
            if (KeyboardService != null)
            { 
                KeyboardService.TemplateNodeActive = false; 
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.CenterLabelClick"]/*' />
        /// <devdoc>
        ///      Called when the user clicks the CenterLabel of the ToolStripTemplateNode. 
        /// </devdoc>
        /// <internalonly/> 
        private void CenterLabelClick(object sender, MouseEventArgs e) 
        {
            //For Right Button we show the DesignerContextMenu... 
            if (e.Button == MouseButtons.Right)
            {
                //Dont show the DesignerContextMenu if there is any active templateNode.
                if (KeyboardService != null && KeyboardService.TemplateNodeActive) { 
                    return;
                } 
                if (KeyboardService != null) { 
                    KeyboardService.SelectedDesignerControl = controlHost;
                } 
                SelectionService.SetSelectedComponents(null, SelectionTypes.Replace);
                if (BehaviorService != null)
                {
                    Point loc = BehaviorService.ControlToAdornerWindow(_miniToolStrip); 
                    loc = BehaviorService.AdornerWindowPointToScreen(loc);
                    loc.Offset(e.Location); 
                    DesignerContextMenu.Show(loc); 
                }
            } 
            else
            {
                if (hotRegion.Contains(e.Location) && !KeyboardService.TemplateNodeActive)
                { 
                    if (KeyboardService != null) {
                        KeyboardService.SelectedDesignerControl = controlHost; 
                    } 
                    SelectionService.SetSelectedComponents(null, SelectionTypes.Replace);
                    ToolStripDropDown oldContextMenu = contextMenu; 
                    // PERF: Consider refresh mechanism for the derived items.
                    if (oldContextMenu != null)
                    {
                        oldContextMenu.Closed -= new ToolStripDropDownClosedEventHandler(OnContextMenuClosed); 
                        oldContextMenu.Opened -= new EventHandler(OnContextMenuOpened);
                        oldContextMenu.Dispose(); 
                    } 
                    contextMenu = null;
                    ShowDropDownMenu(); 

                }
                else
                { 
                    // Remember the click position.
                    ToolStripDesigner.LastCursorPosition = Cursor.Position; 
 
                    if (_designer is ToolStripDesigner)
                    { 
                        if (KeyboardService.TemplateNodeActive)
                        {
                            KeyboardService.ActiveTemplateNode.Commit(false, false);
                        } 
                        // cause a selectionChange...
                        if (SelectionService.PrimarySelection == null) 
                        { 
                            SelectionService.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace);
                        } 

                        KeyboardService.SelectedDesignerControl = controlHost;
                        SelectionService.SetSelectedComponents(null, SelectionTypes.Replace);
                        ((ToolStripDesigner)_designer).ShowEditNode(true); 
                    }
                    if (_designer is ToolStripMenuItemDesigner) 
                    { 
                        // cache the serviceProvider (Site) since the component can potential get disposed after the call to CommitAndSelect();
                        IServiceProvider svcProvider = component.Site as IServiceProvider; 

                        // Commit any InsituEdit Node.
                        if (KeyboardService.TemplateNodeActive)
                        { 
                            ToolStripItem currentItem = component as ToolStripItem;
                            if (currentItem != null) 
                            { 
                                // We have clicked the TemplateNode of a visible Item .. so just commit the current Insitu...
                                if (currentItem.Visible) 
                                {
                                    // If templateNode Active .. commit
                                    KeyboardService.ActiveTemplateNode.Commit(false, false);
                                } 
                                else  //we have clicked the templateNode of a Invisible Item ... so a dummyItem. In this case select the item.
                                { 
                                    // If templateNode Active .. commit and Select 
                                    KeyboardService.ActiveTemplateNode.Commit(false, true);
                                } 
                            }
                            else  //If Component is not a ToolStripItem
                            {
                                KeyboardService.ActiveTemplateNode.Commit(false, false); 
                            }
                        } 
 
                        if (_designer != null)
                        { 
                            ((ToolStripMenuItemDesigner)_designer).EditTemplateNode(true);
                        }
                        else
                        { 
                            ISelectionService cachedSelSvc = (ISelectionService)svcProvider.GetService(typeof(ISelectionService));
                            ToolStripItem selectedItem = cachedSelSvc.PrimarySelection as ToolStripItem; 
                            if (selectedItem != null && _designerHost != null) 
                            {
                                ToolStripMenuItemDesigner itemDesigner = _designerHost.GetDesigner(selectedItem) as ToolStripMenuItemDesigner; 
                                if (itemDesigner != null)
                                {
                                    //Invalidate the item only if its toplevel.
                                    if (!selectedItem.IsOnDropDown) 
                                    {
                                        Rectangle bounds = itemDesigner.GetGlyphBounds(); 
                                        ToolStripDesignerUtils.GetAdjustedBounds(selectedItem, ref bounds); 
                                        BehaviorService bSvc = svcProvider.GetService(typeof(BehaviorService)) as BehaviorService;
                                        if (bSvc != null) 
                                        {
                                            bSvc.Invalidate(bounds);
                                        }
                                    } 
                                    itemDesigner.EditTemplateNode(true);
                                } 
                            } 
                        }
                    } 
                }
            }
        }
 
        /// <devdoc>
        ///      Painting of the templateNode on MouseEnter. 
        /// </devdoc> 
        private void CenterLabelMouseEnter(object sender, EventArgs e)
        { 
            if (renderer != null && !KeyboardService.TemplateNodeActive)
            {
                if (renderer.State != (int)TemplateNodeSelectionState.HotRegionSelected)
                { 
                    renderer.State = (int)TemplateNodeSelectionState.MouseOverLabel;
                    _miniToolStrip.Invalidate(); 
                } 
            }
 
        }

        /// <devdoc>
        ///      Painting of the templateNode on MouseMove 
        /// </devdoc>
        private void CenterLabelMouseMove(object sender, MouseEventArgs e) 
        { 

            if (renderer != null && !KeyboardService.TemplateNodeActive) 
            {
                if (renderer.State != (int)TemplateNodeSelectionState.HotRegionSelected)
                {
                    if (hotRegion.Contains(e.Location)) 
                    {
                        renderer.State = (int)TemplateNodeSelectionState.MouseOverHotRegion; 
                    } 
                    else
                    { 
                        renderer.State = (int)TemplateNodeSelectionState.MouseOverLabel;
                    }
                    _miniToolStrip.Invalidate();
                } 
            }
 
        } 

        /// <devdoc> 
        ///      Painting of the templateNode on MouseLeave
        /// </devdoc>
        private void CenterLabelMouseLeave(object sender, EventArgs e)
        { 

            if (renderer != null && !KeyboardService.TemplateNodeActive) 
            { 
                if (renderer.State != (int)TemplateNodeSelectionState.HotRegionSelected)
                { 
                    renderer.State = (int)TemplateNodeSelectionState.None;
                }
                if (KeyboardService != null && KeyboardService.SelectedDesignerControl == controlHost)
                { 
                    renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                } 
                _miniToolStrip.Invalidate(); 
            }
 
        }

        /// <devdoc>
        ///      Painting of the templateNode on MouseEnter 
        /// </devdoc>
        private void CenterTextBoxMouseEnter(object sender, EventArgs e) 
        { 
            if (renderer != null)
            { 
                renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                _miniToolStrip.Invalidate();
            }
        } 

        /// <devdoc> 
        ///      Painting of the templateNode on TextBox mouseLeave (in case of MenuStrip) 
        /// </devdoc>
        private void CenterTextBoxMouseLeave(object sender, EventArgs e) 
        {

            if (renderer != null && !Active)
            { 
                renderer.State = (int)TemplateNodeSelectionState.None;
                _miniToolStrip.Invalidate(); 
            } 

        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.CloseEditor"]/*' />
        /// <devdoc>
        ///      This Internal function is called from the ToolStripItemDesigner 
        ///      to relinquish the resources used by the EditorToolStrip.
        ///      This Fucntion disposes the ToolStrip and its components and also clears the 
        ///      event handlers associated. 
        /// </devdoc>
        /// <internalonly/> 
        internal void CloseEditor()
        {
            if (_miniToolStrip != null)
            { 

                Active = false; 
 
                if (lastSelection != null)
                { 
                    lastSelection.Dispose();
                    lastSelection = null;
                }
 
                ToolStrip strip = component as ToolStrip;
                if (strip != null) 
                { 
                    strip.RightToLeftChanged -= new System.EventHandler(this.OnRightToLeftChanged);
                } 
                else {

                    ToolStripDropDownItem stripItem = component as ToolStripDropDownItem;
                    if (stripItem != null) 
                    {
                        stripItem.RightToLeftChanged -= new System.EventHandler(this.OnRightToLeftChanged); 
                    } 
                }
 
                if (centerLabel != null)
                {
                    centerLabel.MouseUp -= new MouseEventHandler(CenterLabelClick);
                    centerLabel.MouseEnter -= new EventHandler(CenterLabelMouseEnter); 
                    centerLabel.MouseMove -= new MouseEventHandler(CenterLabelMouseMove);
                    centerLabel.MouseLeave -= new EventHandler(CenterLabelMouseLeave); 
                    centerLabel.Dispose(); 
                    centerLabel = null;
                } 

                if (addItemButton != null)
                {
                    addItemButton.MouseMove -= new System.Windows.Forms.MouseEventHandler(OnMouseMove); 
                    addItemButton.MouseUp -= new System.Windows.Forms.MouseEventHandler(OnMouseUp);
                    addItemButton.MouseDown -= new System.Windows.Forms.MouseEventHandler(OnMouseDown); 
                    addItemButton.DropDownOpened -= new EventHandler(OnAddItemButtonDropDownOpened); 
                    addItemButton.DropDown.Dispose();
                    addItemButton.Dispose(); 
                    addItemButton = null;
                }
                if (contextMenu != null)
                { 
                    contextMenu.Closed -= new ToolStripDropDownClosedEventHandler(OnContextMenuClosed);
                    contextMenu.Opened -= new EventHandler(OnContextMenuOpened); 
                    contextMenu = null; 
                }
 
                _miniToolStrip.MouseLeave -= new System.EventHandler(OnMouseLeave);
                _miniToolStrip.Dispose();
                _miniToolStrip = null;
                _designSurface.Flushed -= new EventHandler(OnLoaderFlushed); 
                _designSurface = null;
                _designer = null; 
 
                OnClosed(new EventArgs());
 
            }
        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.RollBack"]/*' /> 
        /// <devdoc>
        ///      This internal Function is called by item designers to ROLLBACK the current 
        ///      Insitu editing mode. 
        /// </devdoc>
        /// <internalonly/> 
        internal void Commit(bool enterKeyPressed, bool tabKeyPressed)
        {
            // Commit only iff we are still available !!
            if (_miniToolStrip != null && inSituMode) 
            {
                string text = ((TextBox)(centerTextBox.Control)).Text; 
                if (string.IsNullOrEmpty(text)) 
                {
                    RollBack(); 
                }
                else
                {
                    CommitEditor(true, enterKeyPressed, tabKeyPressed); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Internal function that would commit the TemplateNode
        /// </devdoc>
        internal void CommitAndSelect()
        { 
            Commit(false, false);
        } 
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.CommitEditor"]/*' />
        /// <devdoc> 
        ///      This private function performs the job of commiting the current InSitu Editor.
        ///      This will call the CommitEdit(...) function for the appropriate designers
        ///      so that they can actually do their own Specific things for commiting (or ROLLBACKING)
        ///      the Insitu Edit mode. 
        ///      The commit flag is used for commit or rollback.
        ///      BE SURE TO ALWAYS call ExitInSituEdit from this function to 
        ///      put the EditorToolStrip in a sane "NON EDIT" mode. 
        /// </devdoc>
        /// <internalonly/> 
        private void CommitEditor(bool commit, bool enterKeyPressed, bool tabKeyPressed)
        {
            // After the node is commited the templateNode gets the selection.
            // But the original selection is not invalidated. 
            // consider following case
            // FOO -> BAR 
            //    -> TEMPLATENODE node 
            // When the TemplateNode is committed "FOO" is selected
            // but after the commit is complete, The TemplateNode gets the selection but "FOO" is never invalidated 
            // and hence retains selection.
            // So we get the selection and then invalidate it at the end of this function.
            //Get the currentSelection to invalidate
            ToolStripItem curSel = SelectionService.PrimarySelection as ToolStripItem; 

            string text = (centerTextBox != null) ? ((TextBox)(centerTextBox.Control)).Text : String.Empty; 
            ExitInSituEdit(); 
            FocusForm();
 
            if (commit && (_designer is ToolStripDesigner || _designer is ToolStripMenuItemDesigner))
            {
                Type selectedType = null;
                // If user has typed in "-" then Add a Separator only on DropDowns. 
                if (text == "-" && _designer is ToolStripMenuItemDesigner)
                { 
                    ToolStripItemType = typeof(ToolStripSeparator); 
                }
                if (ToolStripItemType != null) 
                {
                    selectedType = ToolStripItemType;
                    ToolStripItemType = null;
                } 
                else
                { 
                    Type[] supportedTypes = ToolStripDesignerUtils.GetStandardItemTypes(component); 
                    selectedType = supportedTypes[0];
                } 
                if (_designer is ToolStripDesigner)
                {
                    ((ToolStripDesigner)_designer).AddNewItem(selectedType, text, enterKeyPressed, tabKeyPressed);
                } 
                else
                { 
                    ((ToolStripItemDesigner)_designer).CommitEdit(selectedType, text, commit, enterKeyPressed, tabKeyPressed); 
                }
            } 
            else if (_designer is ToolStripItemDesigner)
            {
                ((ToolStripItemDesigner)_designer).CommitEdit(_designer.Component.GetType(), text, commit, enterKeyPressed, tabKeyPressed);
            } 

            // finally Invalidate the selection rect ... 
            if (curSel != null) 
            {
                if (_designerHost != null) 
                {
                    ToolStripItemDesigner designer = _designerHost.GetDesigner(curSel) as ToolStripItemDesigner;
                    if (designer != null)
                    { 
                        Rectangle invalidateBounds = designer.GetGlyphBounds();
                        ToolStripDesignerUtils.GetAdjustedBounds(curSel, ref invalidateBounds); 
                        invalidateBounds.Inflate(GLYPHBORDER, GLYPHBORDER); 
                        Region rgn = new Region(invalidateBounds);
                        invalidateBounds.Inflate(-GLYPHINSET, -GLYPHINSET); 
                        rgn.Exclude(invalidateBounds);
                        if (BehaviorService != null)
                        {
                            BehaviorService.Invalidate(rgn); 
                        }
                        rgn.Dispose(); 
                    } 
                }
            } 
        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.EnterInSituEdit"]/*' />
        /// <devdoc> 
        ///      The ToolStripTemplateNode enters into InSitu Edit Mode through this Function.
        ///      This Function is called by FocusEditor( ) which starts the InSitu. 
        ///      The centerLabel is SWAPPED by centerTextBox and the ToolStripTemplateNode is Ready for 
        ///      Text.
        ///      Settting "Active = true" pushes the IEventHandler which now intercepts the 
        ///      Escape and Enter keys to ROLLBACK or COMMIT the InSitu Editing respectively.
        /// </devdoc>
        /// <internalonly/>
        private void EnterInSituEdit() 
        {
            if (!inSituMode) 
            { 

 
                // Listen For Commandss....
                if (_miniToolStrip.Parent != null)
                {
                    _miniToolStrip.Parent.SuspendLayout(); 
                }
                try 
                { 

                    Active = true; 
                    inSituMode = true;
                    // set the renderer state to Selected...
                    if (renderer != null)
                    { 
                        renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                    } 
 

                    // Set UP textBox for InSitu 
                    //
                    TextBox tb = new TemplateTextBox(_miniToolStrip, this);
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.Text = centerLabel.Text; 
                    tb.ForeColor = SystemColors.WindowText;
                    int width = 90; 
 
                    centerTextBox = new ToolStripControlHost(tb);
                    centerTextBox.Dock = DockStyle.None; 
                    centerTextBox.AutoSize = false;
                    centerTextBox.Width = width;

 
                    ToolStripDropDownItem item = activeItem as ToolStripDropDownItem;
                    if (item != null && !item.IsOnDropDown) 
                    { 
                        centerTextBox.Margin = new System.Windows.Forms.Padding(1, 2, 1, 3);
                    } 
                    else
                    {
                        centerTextBox.Margin = new System.Windows.Forms.Padding(1);
                    } 

                    centerTextBox.Size = _miniToolStrip.DisplayRectangle.Size - centerTextBox.Margin.Size; 
 
                    centerTextBox.Name = "centerTextBox";
 
                    centerTextBox.MouseEnter += new EventHandler(CenterTextBoxMouseEnter);
                    centerTextBox.MouseLeave += new EventHandler(CenterTextBoxMouseLeave);

                    int index = _miniToolStrip.Items.IndexOf(centerLabel); 

                    //swap in our insitu textbox 
                    if (index != -1) 
                    {
                        _miniToolStrip.Items.Insert(index, centerTextBox); 
                        _miniToolStrip.Items.Remove(centerLabel);
                    }

                    tb.KeyUp += new KeyEventHandler(this.OnKeyUp); 
                    tb.KeyDown += new KeyEventHandler(this.OnKeyDown);
                    tb.SelectAll(); 
 
                    Control baseComponent = null;
 
                    if (_designerHost != null)
                    {
                        baseComponent = (Control)_designerHost.RootComponent;
                        NativeMethods.SendMessage(baseComponent.Handle, NativeMethods.WM_SETREDRAW, 0, 0); 
                        tb.Focus();
                        NativeMethods.SendMessage(baseComponent.Handle, NativeMethods.WM_SETREDRAW, 1, 0); 
                    } 
                }
                finally 
                {
                    if (_miniToolStrip.Parent != null)
                    {
                        _miniToolStrip.Parent.ResumeLayout(); 
                    }
                } 
            } 
        }
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.ExitInSituEdit"]/*' />
        /// <devdoc>
        ///      The ToolStripTemplateNode exits from InSitu Edit Mode through this Function.
        ///      This Function is called by CommitEditor( ) which stops the InSitu. 
        ///      The centerTextBox is SWAPPED by centerLabel and the ToolStripTemplateNode is exits the
        ///      InSitu Mode. 
        ///      Settting "Active = false" pops the IEventHandler. 
        /// </devdoc>
        /// <internalonly/> 
        private void ExitInSituEdit()
        {
            // put the ToolStripTemplateNode back into "non edit state"
 
            if (centerTextBox != null && inSituMode)
            { 
 
                if (_miniToolStrip.Parent != null)
                { 
                    _miniToolStrip.Parent.SuspendLayout();
                }
                try
                { 

                    //if going insitu with a real item, set & select all the text 
                    int index = _miniToolStrip.Items.IndexOf(centerTextBox); 
                    //validate index
                    if (index != -1) 
                    {
                        centerLabel.Text = SR.GetString(SR.ToolStripDesignerTemplateNodeEnterText);
                        //swap in our insitu textbox
                        _miniToolStrip.Items.Insert(index, centerLabel); 
                        _miniToolStrip.Items.Remove(centerTextBox);
                        ((TextBox)(centerTextBox.Control)).KeyUp -= new KeyEventHandler(this.OnKeyUp); 
                        ((TextBox)(centerTextBox.Control)).KeyDown -= new KeyEventHandler(this.OnKeyDown); 

                    } 

                    centerTextBox.MouseEnter -= new EventHandler(CenterTextBoxMouseEnter);
                    centerTextBox.MouseLeave -= new EventHandler(CenterTextBoxMouseLeave);
 
                    centerTextBox.Dispose();
                    centerTextBox = null; 
                    inSituMode = false; 
                    //reset the Size....
                    SetWidth(null); 
                }
                finally
                {
                    if (_miniToolStrip.Parent != null) 
                    {
                        _miniToolStrip.Parent.ResumeLayout(); 
                    } 

                    // POP of the Handler !!! 
                    Active = false;
                }
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.FocusEditor"]/*' /> 
        /// <devdoc> 
        ///      This internal function is called from ToolStripItemDesigner to put the
        ///      current item into InSitu Edit Mode. 
        /// </devdoc>
        /// <internalonly/>
        internal void FocusEditor(ToolStripItem currentItem)
        { 
            if (currentItem != null)
            { 
                centerLabel.Text = currentItem.Text; 
            }
            EnterInSituEdit(); 

        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.ActivateForm"]/*' /> 
        /// <devdoc>
        ///      Called when the user enters into the InSitu edit mode.This keeps the 
        ///      fdesigner Form Active..... 
        /// </devdoc>
        /// <internalonly/> 
        private void FocusForm()
        {
            DesignerFrame designerFrame = component.Site.GetService(typeof(ISplitWindowService)) as DesignerFrame;
            if (designerFrame != null) 
            {
                Control baseComponent = null; 
 
                if (_designerHost != null)
                { 
                    baseComponent = (Control)_designerHost.RootComponent;
                    NativeMethods.SendMessage(baseComponent.Handle, NativeMethods.WM_SETREDRAW, 0, 0);
                    designerFrame.Focus();
                    NativeMethods.SendMessage(baseComponent.Handle, NativeMethods.WM_SETREDRAW, 1, 0); 
                }
 
            } 
        }
 
        /// <include file='doc\AutoCompleteStringCollection.uex' path='docs/doc[@for="AutoCompleteStringCollection.OnActivated"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected void OnActivated(EventArgs e)
        { 
            if (this.onActivated != null) 
            {
                this.onActivated(this, e); 
            }
        }

        private void OnAddItemButtonDropDownOpened(object sender, EventArgs e) 
        {
            addItemButton.DropDown.Focus(); 
        } 

        /// <include file='doc\AutoCompleteStringCollection.uex' path='docs/doc[@for="AutoCompleteStringCollection.OnCollectionChanged"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected void OnClosed(EventArgs e) 
        {
            if (this.onClosed != null) 
            { 
                this.onClosed(this, e);
            } 
        }

        /// <devdoc>
        ///      Painting of the templateNode on when the contextMenu is closed 
        /// </devdoc>
        private void OnContextMenuClosed(object sender, ToolStripDropDownClosedEventArgs e) 
        { 
            if (renderer != null)
            { 
                renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                _miniToolStrip.Invalidate();
            }
        } 

        /// <devdoc> 
        ///      Set the KeyBoardService member, so the the deisgner knows that the "ContextMenu" is opened. 
        /// </devdoc>
        private void OnContextMenuOpened(object sender, EventArgs e) 
        {
            // Disable All Commands .. the Commands would be reenabled by AddNewItemClick call.
            if (KeyboardService != null)
            { 
                KeyboardService.TemplateNodeContextMenuOpen = true;
            } 
        } 

        /// <include file='doc\AutoCompleteStringCollection.uex' path='docs/doc[@for="AutoCompleteStringCollection.OnDeactivated"]/*' /> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected void OnDeactivated(EventArgs e)
        { 
            if (this.onDeactivated != null)
            { 
                this.onDeactivated(this, e); 
            }
        } 

        /// <devdoc>
        ///     Called by the design surface when it is being
        ///     flushed.  This will save any changes made to TemplateNode. 
        ///
        /// </devdoc> 
        private void OnLoaderFlushed(object sender, EventArgs e) { 
            Commit(false, false);
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OnKeyUp"]/*' />
        /// <devdoc>
        ///      This is small HACK. For some reason if the InSituEditor's textbox has focus the 
        ///      escape key is lost and the menu service doesnt get it.... but the textbox gets it.
        ///      So need to check for the escape key here and call CommitEditor(false) which 
        ///      will ROLLBACK the edit. 
        /// </devdoc>
        /// <internalonly/> 
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (IMEModeSet)
            { 
                return;
            } 
            switch (e.KeyCode) 
            {
 
                case Keys.Up:
                    Commit(false, true);
                    if (KeyboardService != null)
                    { 
                        KeyboardService.ProcessUpDown(false);
                    } 
                    break; 
                case Keys.Down:
                    Commit(true, false); 
                    break;
                case Keys.Escape:
                    CommitEditor(false, false, false);
                    break; 
                case Keys.Return:
                    if (ignoreFirstKeyUp) 
                    { 
                        ignoreFirstKeyUp = false;
                        return; 
                    }
                    OnKeyDefaultAction(sender, e);
                    break;
 
            }
        } 
 
        /// <devdoc>
        ///      Select text on KeyDown. 
        /// </devdoc>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (IMEModeSet) 
            {
                return; 
            } 
            if (e.KeyCode == Keys.A && (e.KeyData & Keys.Control) != 0)
            { 
                TextBox t = sender as TextBox;
                if (t != null)
                {
                    t.SelectAll(); 
                }
            } 
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OnKeyDefaultAction"]/*' /> 
        /// <devdoc>
        ///      Check for the Enter key here and call CommitEditor(true) which
        ///      will COMMIT the edit.
        /// </devdoc> 
        /// <internalonly/>
        private void OnKeyDefaultAction(object sender, EventArgs e) 
        { 
            //exit Insitu with commiting....
            Active = false; 
            Debug.Assert(centerTextBox.Control != null, "The TextBox is null");
            if (centerTextBox.Control != null)
            {
                string text = ((TextBox)(centerTextBox.Control)).Text; 
                if (string.IsNullOrEmpty(text))
                { 
                    CommitEditor(false, false, false); 
                }
                else 
                {
                    CommitEditor(true, true, false);
                }
 
            }
        } 
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OnMenuCut"]/*' />
        /// <devdoc> 
        ///     Called when the delete menu item is selected.
        /// </devdoc>
        private void OnMenuCut(object sender, EventArgs e)
        { 

        } 
 
        /// <devdoc>
        ///      Show ContextMenu if the Right Mouse button was pressed and we have received the following MouseUp 
        /// </devdoc>
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) 
            {
                if (BehaviorService != null) 
                { 
                    Point loc = BehaviorService.ControlToAdornerWindow(_miniToolStrip);
                    loc = BehaviorService.AdornerWindowPointToScreen(loc); 
                    loc.Offset(e.Location);
                    DesignerContextMenu.Show(loc);
                }
            } 

        } 
 
        /// <devdoc>
        ///      Set the selection to the component. 
        /// </devdoc>
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (KeyboardService != null) { 
                KeyboardService.SelectedDesignerControl = controlHost;
            } 
            SelectionService.SetSelectedComponents(null, SelectionTypes.Replace); 

        } 

        /// <devdoc>
        ///      Painting on the button for mouse Move.
        /// </devdoc> 
        private void OnMouseMove(object sender, MouseEventArgs e)
        { 
            renderer.State = (int)TemplateNodeSelectionState.None; 
            if (renderer != null)
            { 
                if (addItemButton != null)
                {
                    if (addItemButton.ButtonBounds.Contains(e.Location))
                    { 
                        renderer.State = (int)TemplateNodeSelectionState.SplitButtonSelected;
                    } 
                    else if (addItemButton.DropDownButtonBounds.Contains(e.Location)) 
                    {
                        renderer.State = (int)TemplateNodeSelectionState.DropDownSelected; 
                    }
                }
                _miniToolStrip.Invalidate();
            } 
        }
 
        /// <devdoc> 
        ///      Painting on the button for mouse Leave.
        /// </devdoc> 
        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (SelectionService != null)
            { 
                ToolStripItem selectedObj = SelectionService.PrimarySelection as ToolStripItem;
                if (selectedObj != null && renderer != null && renderer.State != (int)TemplateNodeSelectionState.HotRegionSelected) 
                { 
                    renderer.State = (int)TemplateNodeSelectionState.None;
                } 
                if (KeyboardService != null && KeyboardService.SelectedDesignerControl == controlHost)
                {
                    renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                } 

                _miniToolStrip.Invalidate(); 
            } 
        }
 
        private void OnRightToLeftChanged(object sender, EventArgs e)
        {
            ToolStrip strip = sender as ToolStrip;
            if (strip != null) 
            {
                _miniToolStrip.RightToLeft = strip.RightToLeft; 
            } 
            else
            { 
                ToolStripDropDownItem stripItem = sender as ToolStripDropDownItem;
                _miniToolStrip.RightToLeft = stripItem.RightToLeft;
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OverrideInvoke"]/*' /> 
        /// <devdoc> 
        ///     Intercept invokation of specific commands and keys
        /// </devdoc> 
        public bool OverrideInvoke(MenuCommand cmd)
        {
            for (int i = 0; i < commands.Length; i++)
            { 
                if (commands[i].CommandID.Equals(cmd.CommandID))
                { 
                    if (cmd.CommandID == MenuCommands.Delete || cmd.CommandID == MenuCommands.Cut || cmd.CommandID == MenuCommands.Copy) 
                    {
                        commands[i].Invoke(); 
                        return true;
                    }
                }
            } 
            return false;
        } 
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OverrideStatus"]/*' />
        /// <devdoc> 
        ///     Intercept invokation of specific commands and keys
        /// </devdoc>
        public bool OverrideStatus(MenuCommand cmd)
        { 

            for (int i = 0; i < commands.Length; i++) 
            { 
                if (commands[i].CommandID.Equals(cmd.CommandID))
                { 
                    cmd.Enabled = false;
                    return true;
                }
            } 

            return false; 
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.RollBack"]/*' /> 
        /// <devdoc>
        ///      This internal Function is called by item designers to ROLLBACK the current
        ///      Insitu editing mode.
        /// </devdoc> 
        /// <internalonly/>
        internal void RollBack() 
        { 
            // RollBack only iff we are still available !!
            if (_miniToolStrip != null && inSituMode) 
            {
                CommitEditor(false, false, false);
            }
        } 
        /// <devdoc>
        ///     Show the contextMenu... 
        /// </devdoc> 
        /// <internalonly/>
        internal void ShowContextMenu(Point pt) 
        {
            DesignerContextMenu.Show(pt);
        }
 
        /// <devdoc>
        ///     Show the drop Down Menu... 
        /// </devdoc> 
        /// <internalonly/>
        internal void ShowDropDownMenu() 
        {
            if (addItemButton != null)
            {
                addItemButton.ShowDropDown(); 
            }
            else 
            { 
                if (BehaviorService != null)
                { 
                    Point loc = BehaviorService.ControlToAdornerWindow(_miniToolStrip);
                    loc = BehaviorService.AdornerWindowPointToScreen(loc);
                    Rectangle translatedBounds = new Rectangle(loc, _miniToolStrip.Size);
                    if (contextMenu == null) 
                    {
                        contextMenu = ToolStripDesignerUtils.GetNewItemDropDown(component, null, new EventHandler(AddNewItemClick), false, component.Site); 
                        contextMenu.Closed += new ToolStripDropDownClosedEventHandler(OnContextMenuClosed); 
                        contextMenu.Opened += new EventHandler(OnContextMenuOpened);
                        contextMenu.Text = "ItemSelectionMenu"; 
                    }
                    ToolStrip strip = component as ToolStrip;
                    if (strip != null)
                    { 
                        contextMenu.RightToLeft = strip.RightToLeft;
                    } 
                    else 
                    {
                        ToolStripDropDownItem stripItem = component as ToolStripDropDownItem; 
                        if (stripItem != null)
                        {
                            contextMenu.RightToLeft = stripItem.RightToLeft;
                        } 
                    }
                    contextMenu.Show(translatedBounds.X, translatedBounds.Y + translatedBounds.Height); 
                    contextMenu.Focus(); 

                    if (renderer != null) 
                    {
                        renderer.State = (int)TemplateNodeSelectionState.HotRegionSelected;
                        _miniToolStrip.Invalidate();
                    } 
                }
            } 
        } 

 

        /// <devdoc>
        ///      This function sets up the Menu specific TemplateNODE..
        /// </devdoc> 
        /// <internalonly/>
        private void SetUpMenuTemplateNode(ToolStripTemplateNode owner, string text, Image image, IComponent currentItem) 
        { 
            centerLabel = new ToolStripLabel();
            centerLabel.Text = text; 
            centerLabel.AutoSize = false;
            centerLabel.IsLink = false;

            centerLabel.Margin = new System.Windows.Forms.Padding(1); 
            if (currentItem is ToolStripDropDownItem)
            { 
                centerLabel.Margin = new System.Windows.Forms.Padding(1, 2, 1, 3); 
            }
            centerLabel.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0); 
            centerLabel.Name = "centerLabel";

            centerLabel.Size = _miniToolStrip.DisplayRectangle.Size - centerLabel.Margin.Size;
            centerLabel.ToolTipText = SR.GetString(SR.ToolStripDesignerTemplateNodeLabelToolTip); 

            centerLabel.MouseUp += new MouseEventHandler(CenterLabelClick); 
            centerLabel.MouseEnter += new EventHandler(CenterLabelMouseEnter); 
            centerLabel.MouseMove += new MouseEventHandler(CenterLabelMouseMove);
            centerLabel.MouseLeave += new EventHandler(CenterLabelMouseLeave); 

            _miniToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { centerLabel });

 
        }
 
        /// <devdoc> 
        ///      This function sets up the Menu specific TemplateNODE..
        /// </devdoc> 
        /// <internalonly/>
        // Standard 'catch all - rethrow critical' exception pattern
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void SetUpToolTemplateNode(ToolStripTemplateNode owner, string text, Image image, IComponent component) 
        { 

            addItemButton = new ToolStripSplitButton(); 
            addItemButton.AutoSize = false;
            addItemButton.Margin = new Padding(1);
            addItemButton.Size = _miniToolStrip.DisplayRectangle.Size - addItemButton.Margin.Size;
            addItemButton.DropDownButtonWidth = 11; 
            addItemButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            if (component is StatusStrip) 
            { 
                addItemButton.ToolTipText = SR.GetString(SR.ToolStripDesignerTemplateNodeSplitButtonStatusStripToolTip);
            } 
            else
            {
                addItemButton.ToolTipText = SR.GetString(SR.ToolStripDesignerTemplateNodeSplitButtonToolTip);
            } 

            addItemButton.MouseDown += new System.Windows.Forms.MouseEventHandler(OnMouseDown); 
            addItemButton.MouseMove += new System.Windows.Forms.MouseEventHandler(OnMouseMove); 
            addItemButton.MouseUp += new System.Windows.Forms.MouseEventHandler(OnMouseUp);
            addItemButton.DropDownOpened += new EventHandler(OnAddItemButtonDropDownOpened); 

            contextMenu = ToolStripDesignerUtils.GetNewItemDropDown(component, null, new EventHandler(AddNewItemClick), false, component.Site);
            contextMenu.Text = "ItemSelectionMenu";
 
            contextMenu.Closed += new ToolStripDropDownClosedEventHandler(OnContextMenuClosed);
            contextMenu.Opened += new EventHandler(OnContextMenuOpened); 
 
            addItemButton.DropDown = contextMenu;
            // 
            //  Set up default item and image.
            //
            try
            { 

                if (addItemButton.DropDownItems.Count > 0) 
                { 
                    ItemTypeToolStripMenuItem firstItem = (ItemTypeToolStripMenuItem)addItemButton.DropDownItems[0];
                    addItemButton.ImageTransparentColor = Color.Lime; 
                    addItemButton.Image = new Bitmap(typeof(ToolStripTemplateNode), "ToolStripTemplateNode.bmp");
                    addItemButton.DefaultItem = firstItem;
                }
            } 
            catch (Exception ex)
            { 
                if (ClientUtils.IsCriticalException(ex)) 
                {
                    throw; 
                }
            }

            _miniToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                addItemButton
            }); 
        } 

 

        /// <include file='doc\TemplateNode.uex' path='docs/doc[@for="TemplateNode.SetupNewEditNode"]/*' />
        /// <devdoc>
        ///      This method does actual edit node creation. 
        /// </devdoc>
        /// <internalonly/> 
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
        private void SetupNewEditNode(ToolStripTemplateNode owner, string text, Image image, IComponent currentItem)
        { 
            //
            // setup the MINIToolStrip host...
            //
            renderer = new MiniToolStripRenderer(owner); 

            _miniToolStrip = new TransparentToolStrip(owner); 
 
            ToolStrip strip = currentItem as ToolStrip;
            if (strip != null) 
            {
                _miniToolStrip.RightToLeft = strip.RightToLeft;
                strip.RightToLeftChanged += new System.EventHandler(this.OnRightToLeftChanged);
            } 
            ToolStripDropDownItem stripItem = currentItem as ToolStripDropDownItem;
            if (stripItem != null) 
            { 
                _miniToolStrip.RightToLeft = stripItem.RightToLeft;
                stripItem.RightToLeftChanged += new System.EventHandler(this.OnRightToLeftChanged); 
            }

            //
            // _miniToolStrip 
            //
            _miniToolStrip.SuspendLayout(); 
            _miniToolStrip.CanOverflow = false; 
            _miniToolStrip.Cursor = System.Windows.Forms.Cursors.Default;
            _miniToolStrip.Dock = System.Windows.Forms.DockStyle.None; 
            _miniToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _miniToolStrip.Name = "miniToolStrip";
            _miniToolStrip.TabIndex = 0;
            _miniToolStrip.Text = "miniToolStrip"; 
            _miniToolStrip.Visible = true;
            _miniToolStrip.Renderer = renderer; 
 
            // ADD items to the Template ToolStrip depending upon the Parent Type...
            if (currentItem is MenuStrip || currentItem is ToolStripDropDownItem) 
            {
                SetUpMenuTemplateNode(owner, text, image, currentItem);
            }
            else 
            {
 
                SetUpToolTemplateNode(owner, text, image, currentItem); 
            }
 
            _miniToolStrip.MouseLeave += new System.EventHandler(OnMouseLeave);
            _miniToolStrip.ResumeLayout();

        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.SetWidth"]/*' /> 
        /// <devdoc> 
        ///      This method does sets the width of the Editor (_miniToolStrip) based on the
        ///      text passed in. 
        /// </devdoc>
        /// <internalonly/>
        internal void SetWidth(string text)
        { 
            //
            if (string.IsNullOrEmpty(text)) 
            { 
                _miniToolStrip.Width = centerLabel.Width + 2;
 
            }
            else
            {
                centerLabel.Text = text; 
            }
 
        } 

        /// <devdoc> 
        ///      Private class that implements the textBox for the InSitu Editor.
        /// </devdoc>
        private class TemplateTextBox : TextBox
        { 
            TransparentToolStrip parent;
            ToolStripTemplateNode owner; 
            private const int IMEMODE = 229; 

            public TemplateTextBox(TransparentToolStrip parent, ToolStripTemplateNode owner) 
                : base()
            {
                this.parent = parent;
                this.owner = owner; 
                this.AutoSize = false;
                this.Multiline = false; 
            } 

            /// <devdoc> 
            ///      Get Parent Handle.
            /// </devdoc>
            private bool IsParentWindow(IntPtr hWnd)
            { 
                if (hWnd == parent.Handle)
                { 
                    return true; 
                }
                return false; 
            }

            protected override bool IsInputKey(Keys keyData) {
                switch (keyData & Keys.KeyCode) { 
                    case Keys.Return:
                        owner.Commit(true, false); 
                        return true; 
                }
                return base.IsInputKey(keyData); 
            }

            /// <devdoc>
            ///      Process the IMEMode message.. 
            /// </devdoc>
            protected override bool ProcessDialogKey(Keys keyData) 
            { 
                if ((int)keyData == IMEMODE)
                { 
                    owner.IMEModeSet = true;
                }
                else
                { 
                    owner.IMEModeSet = false;
                    owner.ignoreFirstKeyUp = false; 
                } 
                return base.ProcessDialogKey(keyData);
            } 

            /// <devdoc>
            ///      Process the WNDPROC for WM_KILLFOCUS to commit the Insitu Editor..
            /// </devdoc> 
            protected override void WndProc(ref Message m)
            { 
                switch (m.Msg) 
                {
                    case NativeMethods.WM_KILLFOCUS: 
                        base.WndProc(ref m);
                        IntPtr focussedWindow = (IntPtr)m.WParam;
                        if (!IsParentWindow(focussedWindow))
                        { 
                            owner.Commit(false, false);
                        } 
                        break; 

                    // DevDiv Bugs 144618 : 
                    // 1.Slowly click on a menu strip item twice to make it editable, while the item's dropdown menu is visible
                    // 2.Select the text of the item and right click on it
                    // 3.Left click 'Copy' or 'Cut' in the context menu
                    // IDE crashed because left click in step3 invoked glyph 
                    // behavior, which commited and destroyed the insitu edit box and thus
                    // the 'copy' or 'cut' action has no text to work with. 
                    // Thus need to block glyph behaviors while the context menu is displayed. 
                    case NativeMethods.WM_CONTEXTMENU:
                        this.owner.IsSystemContextMenuDisplayed = true; 
                        base.WndProc(ref m);
                        this.owner.IsSystemContextMenuDisplayed = false;
                        break;
 
                    default:
                        base.WndProc(ref m); 
                        break; 
                }
            } 
        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.TransparentToolStrip"]/*' />
        /// <devdoc> 
        ///      Private class to Change the Winbar to a TranparentWinbar.
        ///      Our EditorToolStrip is a TranparentToolStrip so that it picks up the itemColor. 
        /// </devdoc> 
        /// <internalonly/>
        public class TransparentToolStrip : ToolStrip 
        {
            ToolStripTemplateNode owner;
            IComponent currentItem;
 
            public TransparentToolStrip(ToolStripTemplateNode owner)
            { 
                this.owner = owner; 
                currentItem = owner.component;
                this.TabStop = true; 
                SetStyle(ControlStyles.Selectable, true);
                this.AutoSize = false;
            }
 
            /// <devdoc>
            ///      Owner TemplateNode.. 
            /// </devdoc> 
            public ToolStripTemplateNode TemplateNode
            { 
                get
                {
                    return owner;
                } 
            }
 
            /// <devdoc> 
            ///      Commit the node and move to next selection.
            /// </devdoc> 
            private void CommitAndSelectNext(bool forward)
            {
                owner.Commit(false, true);
                if (owner.KeyboardService != null) 
                {
                    owner.KeyboardService.ProcessKeySelect(!forward, null); 
                } 
            }
 
            /// <devdoc>
            ///      get current selection.
            /// </devdoc>
            private ToolStripItem GetSelectedItem() 
            {
                ToolStripItem selectedItem = null; 
                for (int i = 0; i < Items.Count; i++) 
                {
                    if (Items[i].Selected) 
                    {
                        selectedItem = Items[i];
                    }
                } 
                return selectedItem;
            } 
 
            /// <include file='doc\TransparentToolStrip.uex' path='docs/doc[@for="TransparentToolStrip.GetPreferredSize"]/*' />
            [EditorBrowsable(EditorBrowsableState.Advanced)] 
            public override Size GetPreferredSize(Size proposedSize)
            {
                if (currentItem is ToolStripDropDownItem)
                { 
                    return new Size(this.Width, 22);
                } 
                else 
                {
                    return new Size(this.Width, 19); 
                }
            }

            /// <devdoc> 
            ///     Process the Tab Key..
            /// </devdoc> 
            private bool ProcessTabKey(bool forward) 
            {
                // Give the ToolStripItem first dibs 
                ToolStripItem item = this.GetSelectedItem();
                if (item is ToolStripControlHost)
                {
 
                    CommitAndSelectNext(forward);
                    return true; 
                } 
                return false;
            } 

            /// <devdoc>
            ///      Process the Dialog Keys for the Templatenode ToolStrip..
            /// </devdoc> 
            protected override bool ProcessDialogKey(Keys keyData)
            { 
                bool retVal = false; 
                if (owner.Active)
                { 
                    if ((keyData & (Keys.Alt | Keys.Control)) == Keys.None)
                    {

                        Keys keyCode = (Keys)keyData & Keys.KeyCode; 

                        switch (keyCode) 
                        { 
                            case Keys.Tab:
                                retVal = ProcessTabKey((keyData & Keys.Shift) == Keys.None); 
                                break;
                        }
                    }
 
                    if (retVal)
                    { 
                        return retVal; 
                    }
                } 
                return base.ProcessDialogKey(keyData);
            }

            /// <include file='doc\Form.uex' path='docs/doc[@for="TransparentToolStrip.SetBoundsCore"]/*' /> 
            /// <devdoc>
            /// </devdoc> 
            [EditorBrowsable(EditorBrowsableState.Advanced)] 
            protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
            { 
                if (currentItem is ToolStripDropDownItem)
                {
                    base.SetBoundsCore(x, y, 92, 22, specified);
                } 
                else if (currentItem is MenuStrip)
                { 
                    base.SetBoundsCore(x, y, 92, 19, specified); 
                }
                else 
                {
                    base.SetBoundsCore(x, y, 31, 19, specified);
                }
            } 
        }
 
        /// <devdoc> 
        ///      Private class that implements the custom Renderer for the TemplateNode ToolStrip.
        /// </devdoc> 
        public class MiniToolStripRenderer : ToolStripSystemRenderer
        {
            private int state = (int)TemplateNodeSelectionState.None;
            private Color selectedBorderColor; 
            private Color defaultBorderColor;
            private Color dropDownMouseOverColor; 
            private Color dropDownMouseDownColor; 
            private Color toolStripBorderColor;
 
            private ToolStripTemplateNode owner;
            private Rectangle hotRegion = Rectangle.Empty;

 

 
            public MiniToolStripRenderer(ToolStripTemplateNode owner) 
                : base()
            { 

                //Add Colors
                this.owner = owner;
                selectedBorderColor = Color.FromArgb(46, 106, 197); 
                defaultBorderColor = Color.FromArgb(171, 171, 171);
                dropDownMouseOverColor = Color.FromArgb(193, 210, 238); 
                dropDownMouseDownColor = Color.FromArgb(152, 181, 226); 
                toolStripBorderColor = Color.White;
 
            }

            /// <devdoc>
            ///      Current state of the TemplateNode UI.. 
            /// </devdoc>
            public int State 
            { 
                get
                { 
                    return state;
                }
                set
                { 
                    state = value;
                } 
            } 

            /// <devdoc> 
            ///      Custom method to draw DOWN arrow on the DropDown.
            /// </devdoc>
            private void DrawArrow(Graphics g, Rectangle bounds)
            { 
                bounds.Width--;
                DrawArrow(new ToolStripArrowRenderEventArgs(g, null, bounds, SystemColors.ControlText, ArrowDirection.Down)); 
            } 

            /// <devdoc> 
            ///      Drawing different DropDown states.
            /// </devdoc>
            private void DrawDropDown(Graphics g, Rectangle bounds, int state)
            { 
                switch (state)
                { 
                    case 4: //MouseOver 
                        using (LinearGradientBrush brush = new LinearGradientBrush(bounds, Color.White, defaultBorderColor, LinearGradientMode.Vertical))
                        { 
                            g.FillRectangle(brush, bounds);
                        }
                        break;
                    case 5: //MouseOnthe HotRegion 
                        using (SolidBrush b = new SolidBrush(dropDownMouseOverColor))
                        { 
                            g.FillRectangle(b, hotRegion); 
                        }
                        break; 
                    case 6: //HotRegionSelected
                        using (SolidBrush b = new SolidBrush(dropDownMouseDownColor))
                        {
                            g.FillRectangle(b, hotRegion); 
                        }
                        break; 
                } 
                DrawArrow(g, bounds);
 
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) {
                if (owner.component is MenuStrip || owner.component is ToolStripDropDownItem) 
                {
                    Graphics g = e.Graphics; 
                    g.Clear(toolStripBorderColor); 
                }
                else 
                {
                    base.OnRenderToolStripBackground(e);
                }
 
            }
 
 
            /// <devdoc>
            ///      Render ToolStrip Border 
            /// </devdoc>
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                Graphics g = e.Graphics; 
                Rectangle bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
                Pen selectborderPen = new Pen(toolStripBorderColor); 
                Rectangle drawRect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1); 
                g.DrawRectangle(selectborderPen, drawRect);
                selectborderPen.Dispose(); 
            }

            /// <devdoc>
            ///      Render the Center Label on the TemplateNode ToolStrip. 
            /// </devdoc>
            protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e) 
            { 
                base.OnRenderLabelBackground(e);
 
                ToolStripItem item = e.Item;
                ToolStrip tool = e.ToolStrip;
                Graphics g = e.Graphics;
                Rectangle bounds = new Rectangle(Point.Empty, item.Size); 

                Rectangle drawRect = new Rectangle(bounds.X, bounds.Y, bounds.Width -1, bounds.Height -1); 
                Pen borderPen = new Pen(defaultBorderColor); 

 
                if (state == (int)TemplateNodeSelectionState.TemplateNodeSelected) //state Template node is selected.
                {
                    g.FillRectangle(new SolidBrush(toolStripBorderColor), drawRect);
 
                    if (owner.EditorToolStrip.RightToLeft == RightToLeft.Yes)
                    { 
                        hotRegion = new Rectangle(bounds.Left + 2, bounds.Top + 2, 9, bounds.Bottom -4); 
                    }
                    else 
                    {
                        hotRegion = new Rectangle(bounds.Right - 11, bounds.Top + 2, 9, bounds.Bottom -4);
                    }
                    owner.HotRegion = hotRegion; 

                    // do the Actual Drawing 
                    borderPen.Color = Color.Black; 
                    item.ForeColor = defaultBorderColor;
                    g.DrawRectangle(borderPen, drawRect); 
                }

                if (state == (int)TemplateNodeSelectionState.MouseOverLabel) //state Template node is selected.
                { 
                    if (owner.EditorToolStrip.RightToLeft == RightToLeft.Yes)
                    { 
                        hotRegion = new Rectangle(bounds.Left + 2, bounds.Top + 2, 9, bounds.Bottom -4); 
                    }
                    else 
                    {
                        hotRegion = new Rectangle(bounds.Right - 11, bounds.Top + 2, 9, bounds.Bottom -4);
                    }
                    owner.HotRegion = hotRegion; 

                    g.Clear(toolStripBorderColor); 
                    DrawDropDown(g, hotRegion, state); 

                    borderPen.Color = Color.Black; 
                    borderPen.DashStyle = DashStyle.Dot;
                    g.DrawRectangle(borderPen, drawRect);
                }
 
                if (state == (int)TemplateNodeSelectionState.MouseOverHotRegion)
                { 
                    g.Clear(toolStripBorderColor); 
                    DrawDropDown(g, hotRegion, state);
 
                    borderPen.Color = Color.Black;
                    borderPen.DashStyle = DashStyle.Dot;

                    item.ForeColor = defaultBorderColor; 
                    g.DrawRectangle(borderPen, drawRect);
                } 
 
                if (state == (int)TemplateNodeSelectionState.HotRegionSelected)
                { 
                    g.Clear(toolStripBorderColor);
                    DrawDropDown(g, hotRegion, state);

                    borderPen.Color = Color.Black; 

                    item.ForeColor = defaultBorderColor; 
                    g.DrawRectangle(borderPen, drawRect); 
                }
 
                if (state == (int)TemplateNodeSelectionState.None) //state Template node is not selected.
                {
                    g.Clear(toolStripBorderColor);
                    g.DrawRectangle(borderPen, drawRect); 
                    item.ForeColor = defaultBorderColor;
                } 
 
                borderPen.Dispose();
 
            }

            /// <devdoc>
            ///      Render the splitButton on the TemplateNode ToolStrip.. 
            /// </devdoc>
            protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e) 
            { 
                // DONT CALL THE BASE AS IT DOESNT ALLOW US TO RENDER THE DROPDOWN BUTTON ....
                //base.OnRenderSplitButtonBackground(e); 
                Graphics g = e.Graphics;
                ToolStripSplitButton splitButton = e.Item as ToolStripSplitButton;

                if (splitButton != null) 
                {
                    // Get the DropDownButton Bounds 
                    Rectangle buttonBounds = splitButton.DropDownButtonBounds; 
                    // Draw the White Divider Line...
                    using (Pen p = new Pen(toolStripBorderColor)) 
                    {
                        g.DrawLine(p, buttonBounds.Left, buttonBounds.Top + 1, buttonBounds.Left, buttonBounds.Bottom - 1);
                    }
 
                    Rectangle bounds = new Rectangle(Point.Empty, splitButton.Size);
                    Pen selectborderPen = null; 
                    bool splitButtonSelected = false; 

                    if (splitButton.DropDownButtonPressed) 
                    {
                        //Button is pressed
                        state = 0;
                        Rectangle fillRect = new Rectangle(buttonBounds.Left + 1, buttonBounds.Top, buttonBounds.Right, buttonBounds.Bottom); 
                        g.FillRectangle(new SolidBrush(dropDownMouseDownColor), fillRect);
                        splitButtonSelected = true; 
                    } 
                    else if (state == (int)TemplateNodeSelectionState.SplitButtonSelected)
                    { 
                        g.FillRectangle(new SolidBrush(dropDownMouseOverColor), splitButton.ButtonBounds);
                        splitButtonSelected = true;
                    }
                    else if (state == (int)TemplateNodeSelectionState.DropDownSelected) 
                    {
                        Rectangle fillRect = new Rectangle(buttonBounds.Left + 1, buttonBounds.Top, buttonBounds.Right, buttonBounds.Bottom); 
                        g.FillRectangle(new SolidBrush(dropDownMouseOverColor), fillRect); 
                        splitButtonSelected = true;
                    } 
                    else if (state == (int)TemplateNodeSelectionState.TemplateNodeSelected)
                    {
                        splitButtonSelected = true;
                    } 

                    if (splitButtonSelected) 
                    { 
                        //DrawSeleted Boder
                        selectborderPen = new Pen(selectedBorderColor); 
                    }
                    else
                    {
                        // Draw Gray Border 
                        selectborderPen = new Pen(defaultBorderColor);
                    } 
 
                    Rectangle drawRect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                    g.DrawRectangle(selectborderPen, drawRect); 
                    selectborderPen.Dispose();

                    // Draw the Arrow
                    DrawArrow(new ToolStripArrowRenderEventArgs(g, splitButton, splitButton.DropDownButtonBounds, SystemColors.ControlText, ArrowDirection.Down)); 
                }
            } 
        } 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripTemplateNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.Security; 
    using System.Security.Permissions; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design;
    using System.Windows.Forms.Design.Behavior;
    using System.Runtime.InteropServices; 
    using System.Drawing.Drawing2D;
 
 
    /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode"]/*' />
    /// <devdoc> 
    ///     This internal class wraps the InSitu Editor. The editor is a runtime Winbar
    ///     control which contains a leftButton (for image), centerLabel (for text) which
    ///     gets swaped by a centerTextBox (when InSitu is ON).
    /// 
    ///     The ToolStripTemplateNode is also responsible for intercepting the Escape and Enter keys
    ///     and implements the IMenuStatusHandler so that it can commit and rollback as required. 
    /// 
    ///     Finally this ToolStripTemplateNode has a private class ItemTypeToolStripMenuItem for adding
    ///     ToolStripItem types to the Dropdown for addItemButton. 
    ///
    /// </devdoc>
    internal class ToolStripTemplateNode : IMenuStatusHandler
    { 

        private const int GLYPHBORDER = 1; 
        private const int GLYPHINSET = 2; 

        // 
        // Component for this InSitu Editor... (this is a ToolStripItem)
        // that wants to go into InSitu
        //
        private IComponent component; 

        // 
        // Current Designer for the comopenent that in InSitu mode 
        //
        private IDesigner _designer = null; 

        //Get DesignerHost.
        private IDesignerHost _designerHost = null;
 
        //
        // Menu Commands to override 
        // 
        private MenuCommand[] commands;
 
        //
        // MenuCommands to Add
        //
        private MenuCommand[] addCommands; 

        // 
        // Actual InSitu Editor and its components... 
        //
        private TransparentToolStrip _miniToolStrip; 

        //
        // Center Label for MenuStrip TemplateNode
        // 
        private ToolStripLabel centerLabel;
 
        // SplitButton reAdded for ToolStrip specific TemplateNode 
        private ToolStripSplitButton addItemButton;
 
        //swaped in text...
        private ToolStripControlHost centerTextBox;

        //reqd as rtb does accept Enter.. 
        internal bool ignoreFirstKeyUp = false;
 
        // 
        // This is the Bounding Rectangle for the ToolStripTemplateNode. This is set
        // by the itemDesigner in terms of the "AdornerWindow" bounds. 
        // The ToolStripEditorManager uses this Bounds to actually activate the
        // editor on the AdornerWindow.
        //
        private Rectangle boundingRect; 

        // 
        // Keeps track of Insitu Mode. 
        //
        private bool inSituMode = false; 

        //
        // Tells whether the editorNode is listening to Menu commands.
        // 
        private bool active = false;
 
        // 
        // Need to keep a track of Last Selection to uncheck it.
        // This is the Checked property on ToolStripItems on the Menu. We check this 
        // cached in value to the current Selection on the addItemButton and if different
        // then uncheck the Checked for this lastSelection.. Check for the currentSelection
        // and finally save the currentSelection as the lastSelection for future check.
        // 
        private ItemTypeToolStripMenuItem lastSelection = null;
 
        // This is the renderer used to Draw the Strips..... 
        private MiniToolStripRenderer renderer;
 
        // This is the Type that the user has selected for the new Item
        private Type itemType;

        //Get the ToolStripKeyBoardService to notify that the TemplateNode is Active and so it shouldnt process the KeyMessages. 
        private ToolStripKeyboardHandlingService toolStripKeyBoardService;
 
        //Cached ISelectionService 
        private ISelectionService selectionService;
 
        //Cached BehaviorService
        private BehaviorService behaviorService;

        //ControlHost for selection on mouseclicks 
        DesignerToolStripControlHost controlHost = null;
 
        // On DropDowns the component passed in is the parent (ownerItem) and hence we need the 
        // reference for actual item
        ToolStripItem activeItem = null; 

        //
        //Event
        // 
        EventHandler onActivated;
 
        // 
        //Event
        // 
        EventHandler onClosed;

        //
        //Event 
        //
        EventHandler onDeactivated; 
 
        // Old Undo/Redo Commands
        private MenuCommand oldUndoCommand = null; 
        private MenuCommand oldRedoCommand = null;

        // The DropDown for the TemplateNode
        private ToolStripDropDown contextMenu; 

        // the Hot Region within the templateNode ... this is used for the menustrips 
        private Rectangle hotRegion; 

        // when a dummyItem is added we set this property to that item so that Selection works .. 
        // If we set the selection before we get redundant calls to "DropDownResize" and hence cause a lot of flicker.
        // private ToolStripItem startItemForSelection;

        private bool imeModeSet = false; 

        //DesignSurface to hook up to the Flushed event 
        private DesignSurface _designSurface= null; 

 
        // Is system context menu displayed for the insitu text box?
        private bool isSystemContextMenuDisplayed = false;

        // 
        // Constructor
        // 
        /// <include file='doc\ToolStripToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripToolStripTemplateNode.ToolStripTemplateNode"]/*' /> 
        public ToolStripTemplateNode(IComponent component, string text, Image image)
        { 
            this.component = component;

            // In most of the cases this is true; except for ToolStripItems on DropDowns.
            // the toolstripMenuItemDesigners sets the public property in those cases. 
            this.activeItem = component as ToolStripItem;
 
            _designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 
            _designer = _designerHost.GetDesigner(component);
            _designSurface = (DesignSurface)component.Site.GetService(typeof(DesignSurface)); 
            _designSurface.Flushed += new EventHandler(OnLoaderFlushed);

            //Setup EditNode
            //ToolStripItem item = component as ToolStripItem; 
            SetupNewEditNode(this, text, image, component);
 
 
            commands = new MenuCommand[] {
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyMoveUp), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyMoveDown),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyMoveLeft),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyMoveRight),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Delete), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Cut),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Copy), 
 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeUp),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeDown), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeLeft),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeRight),

                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeySizeWidthIncrease), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeySizeHeightIncrease),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeySizeWidthDecrease), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeySizeHeightDecrease), 

                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeWidthIncrease), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeHeightIncrease),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeWidthDecrease),
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.KeyNudgeHeightDecrease)
 
            };
 
 
            addCommands = new MenuCommand[] {
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Undo), 
                new MenuCommand(new EventHandler(OnMenuCut), MenuCommands.Redo)
            };

        } 

 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Active"]/*' /> 
        /// <devdoc>
        ///    This property enables / disables Menu Command Handler. 
        /// </devdoc>
        /// <internalonly/>
        public bool Active
        { 
            get
            { 
                return active; 
            }
            set 
            {
                if (active != value)
                {
                    active = value; 

                    if (KeyboardService != null) 
                    { 
                        KeyboardService.TemplateNodeActive = value;
                    } 


                    if (active)
                    { 
                        //Active.. Fire Activated
                        OnActivated(new EventArgs()); 
 
                        if (KeyboardService != null)
                        { 
                            KeyboardService.ActiveTemplateNode = this;
                        }

                        IMenuCommandService menuService = (IMenuCommandService)component.Site.GetService(typeof(IMenuCommandService)); 

                        if (menuService != null) 
                        { 
                            oldUndoCommand = menuService.FindCommand(MenuCommands.Undo);
                            if (oldUndoCommand != null) 
                            {
                                menuService.RemoveCommand(oldUndoCommand);
                            }
 
                            oldRedoCommand = menuService.FindCommand(MenuCommands.Redo);
                            if (oldRedoCommand != null) 
                            { 
                                menuService.RemoveCommand(oldRedoCommand);
                            } 

                            // Disable the Commands
                            for (int i = 0; i < addCommands.Length; i++)
                            { 
                                addCommands[i].Enabled = false;
                                menuService.AddCommand(addCommands[i]); 
                            } 
                        }
 
                        // Listen to command and key events
                        //
                        IEventHandlerService ehs = (IEventHandlerService)component.Site.GetService(typeof(IEventHandlerService));
                        if (ehs != null) 
                        {
                            ehs.PushHandler(this); 
                        } 
                    }
                    else 
                    {

                        //Active == false.. Fire Deactivated
                        OnDeactivated(new EventArgs()); 

                        if (KeyboardService != null) 
                        { 
                            KeyboardService.ActiveTemplateNode = null;
                        } 

                        IMenuCommandService menuService = (IMenuCommandService)component.Site.GetService(typeof(IMenuCommandService));

                        if (menuService != null) 
                        {
                            for (int i = 0; i < addCommands.Length; i++) 
                            { 
                                menuService.RemoveCommand(addCommands[i]);
                            } 
                        }

                        if (oldUndoCommand != null)
                        { 
                            menuService.AddCommand(oldUndoCommand);
                        } 
 
                        if (oldRedoCommand != null)
                        { 
                            menuService.AddCommand(oldRedoCommand);
                        }

                        // Stop listening to command and key events 
                        IEventHandlerService ehs = (IEventHandlerService)component.Site.GetService(typeof(IEventHandlerService));
                        if (ehs != null) 
                        { 
                            ehs.PopHandler(this);
                        } 
                    }
                }
            }
        } 

        // Need to have a reference of the actual item that is edited. 
        public ToolStripItem ActiveItem 
        {
            get 
            {
                return activeItem;
            }
            set 
            {
                activeItem = value; 
            } 

        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Activated"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public event EventHandler Activated 
        { 
            add
            { 
                this.onActivated += value;
            }
            remove
            { 
                this.onActivated -= value;
            } 
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Bounds"]/*' /> 
        /// <devdoc>
        ///    Returns the Bounds of this ToolStripTemplateNode.
        /// </devdoc>
        /// <internalonly/> 
        public Rectangle Bounds
        { 
            get 
            {
                return boundingRect; 
            }
            set
            {
                this.boundingRect = value; 
            }
        } 
 
        /// <devdoc>
        ///    The Warpper ControlHost .. 
        /// </devdoc>
        public DesignerToolStripControlHost ControlHost
        {
            get 
            {
                return controlHost; 
            } 
            set
            { 
                controlHost = value;
            }
        }
 
        /// <devdoc>
        ///   This is the designer contextMenu that pops when rightclicked on the TemplateNode. 
        /// </devdoc> 
        private ContextMenuStrip DesignerContextMenu
        { 
            get
            {

                BaseContextMenuStrip templateNodeContextMenu = new BaseContextMenuStrip(component.Site, controlHost); 
                templateNodeContextMenu.Populated = false;
                templateNodeContextMenu.GroupOrdering.Clear(); 
                templateNodeContextMenu.GroupOrdering.AddRange(new string[] { StandardGroups.Code, 
                                                                          StandardGroups.Custom,
                                                                          StandardGroups.Selection, 
                                                                          StandardGroups.Edit });
                templateNodeContextMenu.Text = "CustomContextMenu";

                TemplateNodeCustomMenuItemCollection templateNodeCustomMenuItemCollection = new TemplateNodeCustomMenuItemCollection(component.Site, controlHost); 
                foreach (ToolStripItem item in templateNodeCustomMenuItemCollection)
                { 
                    templateNodeContextMenu.Groups[StandardGroups.Custom].Items.Add(item); 
                }
 

                return templateNodeContextMenu;
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Deactivated"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public event EventHandler Deactivated
        {
            add
            { 
                this.onDeactivated += value;
            } 
            remove 
            {
                this.onDeactivated -= value; 
            }
        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.Closed"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public event EventHandler Closed
        { 
            add
            {
                this.onClosed += value;
            } 
            remove
            { 
                this.onClosed -= value; 
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.EditorToolStrip"]/*' />
        /// <devdoc>
        ///    This property returns the actual editor ToolStrip. 
        /// </devdoc>
        /// <internalonly/> 
        public ToolStrip EditorToolStrip 
        {
            get 
            {
                return _miniToolStrip;

            } 
        }
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.EditorToolStrip"]/*' /> 
        /// <devdoc>
        ///    This property returns the actual editor ToolStrip. 
        /// </devdoc>
        /// <internalonly/>
        internal TextBox EditBox
        { 
            get
            { 
                return (centerTextBox != null) ? (TextBox)centerTextBox.Control : null; 

            } 
        }

        /// <devdoc>
        ///    HotRegion within the templateNode. this is the region which responds to the mouse. 
        /// </devdoc>
        public Rectangle HotRegion 
        { 
            get
            { 
                return hotRegion;
            }
            set
            { 
                hotRegion = value;
            } 
 
        }
 
        /// <devdoc>
        ///   value to suggest if IME mode is set.
        /// </devdoc>
        public bool IMEModeSet 
        {
            get 
            { 
                return imeModeSet;
            } 
            set
            {
                imeModeSet = value;
            } 
        }
 
        /// <devdoc> 
        ///    KeyBoardHandling service.
        /// </devdoc> 
        private ToolStripKeyboardHandlingService KeyboardService
        {
            get
            { 
                if (toolStripKeyBoardService == null)
                { 
                    toolStripKeyBoardService = (ToolStripKeyboardHandlingService)component.Site.GetService(typeof(ToolStripKeyboardHandlingService)); 
                }
                return toolStripKeyBoardService; 
            }
        }

 

        /// <devdoc> 
        ///    SelectionService. 
        /// </devdoc>
        private ISelectionService SelectionService 
        {
            get
            {
                if (selectionService == null) 
                {
                    selectionService = (ISelectionService)component.Site.GetService(typeof(ISelectionService)); 
                } 
                return selectionService;
            } 
        }


        private BehaviorService BehaviorService 
        {
            get 
            { 
                if (behaviorService == null)
                { 
                    behaviorService = (BehaviorService)component.Site.GetService(typeof(BehaviorService));
                }
                return behaviorService;
            } 
        }
 
 
        /// <devdoc>
        ///    Type of the new Item to be added. 
        /// </devdoc>
        public Type ToolStripItemType
        {
            get 
            {
                return itemType; 
            } 
            set
            { 
                itemType = value;
            }
        }
 
        /// <devdoc>
        ///    Is system context menu for the insitu edit box displayed?. 
        /// </devdoc> 
        internal bool IsSystemContextMenuDisplayed
        { 
            get
            {
                return isSystemContextMenuDisplayed;
            } 
            set
            { 
                isSystemContextMenuDisplayed = value; 
            }
        } 


        ////////////////////////////////////////////////////////////////////////////////////
        ////                                                                //// 
        ////                          Methods                               ////
        ////                                                                //// 
        //////////////////////////////////////////////////////////////////////////////////// 

        /// <devdoc> 
        ///    Helper function to add new Item when the DropDownItem (in the ToolStripTemplateNode) is clicked
        /// </devdoc>
        private void AddNewItemClick(object sender, EventArgs e)
        { 
            // Close the DropDown.. Important for Morphing ....
            if (addItemButton != null) 
            { 
                addItemButton.DropDown.Visible = false;
            } 

            if (component is ToolStrip && SelectionService != null)
            {
                // Stop the Designer from closing the Overflow if its open 
                ToolStripDesigner designer = _designerHost.GetDesigner(component) as ToolStripDesigner;
                try 
                { 
                    if (designer != null)
                    { 
                        designer.DontCloseOverflow = true;
                    }
                    SelectionService.SetSelectedComponents(new object[] { component });
                } 
                finally
                { 
                    if (designer != null) 
                    {
                        designer.DontCloseOverflow = false; 
                    }
                }
            }
 
            ItemTypeToolStripMenuItem senderItem = (ItemTypeToolStripMenuItem)sender;
            if (lastSelection != null) 
            { 
                lastSelection.Checked = false;
            } 

            // set the appropriate Checked state
            senderItem.Checked = true;
            lastSelection = senderItem; 

            // Set the property used in the CommitEditor (.. ) to add the correct Type. 
            ToolStripItemType = senderItem.ItemType; 

            //Select the parent before adding 
            ToolStrip parent = controlHost.GetCurrentParent() as ToolStrip;

            // this will add the item to the ToolStrip..
            if (parent is MenuStrip) 
            {
                CommitEditor(true, true, false); 
            } 
            else
            { 
                // In case of toolStrips/StatusStrip we want the currently added item to be selected instead of selecting the next item
                CommitEditor(true, false, false);
            }
 
            if (KeyboardService != null)
            { 
                KeyboardService.TemplateNodeActive = false; 
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.CenterLabelClick"]/*' />
        /// <devdoc>
        ///      Called when the user clicks the CenterLabel of the ToolStripTemplateNode. 
        /// </devdoc>
        /// <internalonly/> 
        private void CenterLabelClick(object sender, MouseEventArgs e) 
        {
            //For Right Button we show the DesignerContextMenu... 
            if (e.Button == MouseButtons.Right)
            {
                //Dont show the DesignerContextMenu if there is any active templateNode.
                if (KeyboardService != null && KeyboardService.TemplateNodeActive) { 
                    return;
                } 
                if (KeyboardService != null) { 
                    KeyboardService.SelectedDesignerControl = controlHost;
                } 
                SelectionService.SetSelectedComponents(null, SelectionTypes.Replace);
                if (BehaviorService != null)
                {
                    Point loc = BehaviorService.ControlToAdornerWindow(_miniToolStrip); 
                    loc = BehaviorService.AdornerWindowPointToScreen(loc);
                    loc.Offset(e.Location); 
                    DesignerContextMenu.Show(loc); 
                }
            } 
            else
            {
                if (hotRegion.Contains(e.Location) && !KeyboardService.TemplateNodeActive)
                { 
                    if (KeyboardService != null) {
                        KeyboardService.SelectedDesignerControl = controlHost; 
                    } 
                    SelectionService.SetSelectedComponents(null, SelectionTypes.Replace);
                    ToolStripDropDown oldContextMenu = contextMenu; 
                    // PERF: Consider refresh mechanism for the derived items.
                    if (oldContextMenu != null)
                    {
                        oldContextMenu.Closed -= new ToolStripDropDownClosedEventHandler(OnContextMenuClosed); 
                        oldContextMenu.Opened -= new EventHandler(OnContextMenuOpened);
                        oldContextMenu.Dispose(); 
                    } 
                    contextMenu = null;
                    ShowDropDownMenu(); 

                }
                else
                { 
                    // Remember the click position.
                    ToolStripDesigner.LastCursorPosition = Cursor.Position; 
 
                    if (_designer is ToolStripDesigner)
                    { 
                        if (KeyboardService.TemplateNodeActive)
                        {
                            KeyboardService.ActiveTemplateNode.Commit(false, false);
                        } 
                        // cause a selectionChange...
                        if (SelectionService.PrimarySelection == null) 
                        { 
                            SelectionService.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace);
                        } 

                        KeyboardService.SelectedDesignerControl = controlHost;
                        SelectionService.SetSelectedComponents(null, SelectionTypes.Replace);
                        ((ToolStripDesigner)_designer).ShowEditNode(true); 
                    }
                    if (_designer is ToolStripMenuItemDesigner) 
                    { 
                        // cache the serviceProvider (Site) since the component can potential get disposed after the call to CommitAndSelect();
                        IServiceProvider svcProvider = component.Site as IServiceProvider; 

                        // Commit any InsituEdit Node.
                        if (KeyboardService.TemplateNodeActive)
                        { 
                            ToolStripItem currentItem = component as ToolStripItem;
                            if (currentItem != null) 
                            { 
                                // We have clicked the TemplateNode of a visible Item .. so just commit the current Insitu...
                                if (currentItem.Visible) 
                                {
                                    // If templateNode Active .. commit
                                    KeyboardService.ActiveTemplateNode.Commit(false, false);
                                } 
                                else  //we have clicked the templateNode of a Invisible Item ... so a dummyItem. In this case select the item.
                                { 
                                    // If templateNode Active .. commit and Select 
                                    KeyboardService.ActiveTemplateNode.Commit(false, true);
                                } 
                            }
                            else  //If Component is not a ToolStripItem
                            {
                                KeyboardService.ActiveTemplateNode.Commit(false, false); 
                            }
                        } 
 
                        if (_designer != null)
                        { 
                            ((ToolStripMenuItemDesigner)_designer).EditTemplateNode(true);
                        }
                        else
                        { 
                            ISelectionService cachedSelSvc = (ISelectionService)svcProvider.GetService(typeof(ISelectionService));
                            ToolStripItem selectedItem = cachedSelSvc.PrimarySelection as ToolStripItem; 
                            if (selectedItem != null && _designerHost != null) 
                            {
                                ToolStripMenuItemDesigner itemDesigner = _designerHost.GetDesigner(selectedItem) as ToolStripMenuItemDesigner; 
                                if (itemDesigner != null)
                                {
                                    //Invalidate the item only if its toplevel.
                                    if (!selectedItem.IsOnDropDown) 
                                    {
                                        Rectangle bounds = itemDesigner.GetGlyphBounds(); 
                                        ToolStripDesignerUtils.GetAdjustedBounds(selectedItem, ref bounds); 
                                        BehaviorService bSvc = svcProvider.GetService(typeof(BehaviorService)) as BehaviorService;
                                        if (bSvc != null) 
                                        {
                                            bSvc.Invalidate(bounds);
                                        }
                                    } 
                                    itemDesigner.EditTemplateNode(true);
                                } 
                            } 
                        }
                    } 
                }
            }
        }
 
        /// <devdoc>
        ///      Painting of the templateNode on MouseEnter. 
        /// </devdoc> 
        private void CenterLabelMouseEnter(object sender, EventArgs e)
        { 
            if (renderer != null && !KeyboardService.TemplateNodeActive)
            {
                if (renderer.State != (int)TemplateNodeSelectionState.HotRegionSelected)
                { 
                    renderer.State = (int)TemplateNodeSelectionState.MouseOverLabel;
                    _miniToolStrip.Invalidate(); 
                } 
            }
 
        }

        /// <devdoc>
        ///      Painting of the templateNode on MouseMove 
        /// </devdoc>
        private void CenterLabelMouseMove(object sender, MouseEventArgs e) 
        { 

            if (renderer != null && !KeyboardService.TemplateNodeActive) 
            {
                if (renderer.State != (int)TemplateNodeSelectionState.HotRegionSelected)
                {
                    if (hotRegion.Contains(e.Location)) 
                    {
                        renderer.State = (int)TemplateNodeSelectionState.MouseOverHotRegion; 
                    } 
                    else
                    { 
                        renderer.State = (int)TemplateNodeSelectionState.MouseOverLabel;
                    }
                    _miniToolStrip.Invalidate();
                } 
            }
 
        } 

        /// <devdoc> 
        ///      Painting of the templateNode on MouseLeave
        /// </devdoc>
        private void CenterLabelMouseLeave(object sender, EventArgs e)
        { 

            if (renderer != null && !KeyboardService.TemplateNodeActive) 
            { 
                if (renderer.State != (int)TemplateNodeSelectionState.HotRegionSelected)
                { 
                    renderer.State = (int)TemplateNodeSelectionState.None;
                }
                if (KeyboardService != null && KeyboardService.SelectedDesignerControl == controlHost)
                { 
                    renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                } 
                _miniToolStrip.Invalidate(); 
            }
 
        }

        /// <devdoc>
        ///      Painting of the templateNode on MouseEnter 
        /// </devdoc>
        private void CenterTextBoxMouseEnter(object sender, EventArgs e) 
        { 
            if (renderer != null)
            { 
                renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                _miniToolStrip.Invalidate();
            }
        } 

        /// <devdoc> 
        ///      Painting of the templateNode on TextBox mouseLeave (in case of MenuStrip) 
        /// </devdoc>
        private void CenterTextBoxMouseLeave(object sender, EventArgs e) 
        {

            if (renderer != null && !Active)
            { 
                renderer.State = (int)TemplateNodeSelectionState.None;
                _miniToolStrip.Invalidate(); 
            } 

        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.CloseEditor"]/*' />
        /// <devdoc>
        ///      This Internal function is called from the ToolStripItemDesigner 
        ///      to relinquish the resources used by the EditorToolStrip.
        ///      This Fucntion disposes the ToolStrip and its components and also clears the 
        ///      event handlers associated. 
        /// </devdoc>
        /// <internalonly/> 
        internal void CloseEditor()
        {
            if (_miniToolStrip != null)
            { 

                Active = false; 
 
                if (lastSelection != null)
                { 
                    lastSelection.Dispose();
                    lastSelection = null;
                }
 
                ToolStrip strip = component as ToolStrip;
                if (strip != null) 
                { 
                    strip.RightToLeftChanged -= new System.EventHandler(this.OnRightToLeftChanged);
                } 
                else {

                    ToolStripDropDownItem stripItem = component as ToolStripDropDownItem;
                    if (stripItem != null) 
                    {
                        stripItem.RightToLeftChanged -= new System.EventHandler(this.OnRightToLeftChanged); 
                    } 
                }
 
                if (centerLabel != null)
                {
                    centerLabel.MouseUp -= new MouseEventHandler(CenterLabelClick);
                    centerLabel.MouseEnter -= new EventHandler(CenterLabelMouseEnter); 
                    centerLabel.MouseMove -= new MouseEventHandler(CenterLabelMouseMove);
                    centerLabel.MouseLeave -= new EventHandler(CenterLabelMouseLeave); 
                    centerLabel.Dispose(); 
                    centerLabel = null;
                } 

                if (addItemButton != null)
                {
                    addItemButton.MouseMove -= new System.Windows.Forms.MouseEventHandler(OnMouseMove); 
                    addItemButton.MouseUp -= new System.Windows.Forms.MouseEventHandler(OnMouseUp);
                    addItemButton.MouseDown -= new System.Windows.Forms.MouseEventHandler(OnMouseDown); 
                    addItemButton.DropDownOpened -= new EventHandler(OnAddItemButtonDropDownOpened); 
                    addItemButton.DropDown.Dispose();
                    addItemButton.Dispose(); 
                    addItemButton = null;
                }
                if (contextMenu != null)
                { 
                    contextMenu.Closed -= new ToolStripDropDownClosedEventHandler(OnContextMenuClosed);
                    contextMenu.Opened -= new EventHandler(OnContextMenuOpened); 
                    contextMenu = null; 
                }
 
                _miniToolStrip.MouseLeave -= new System.EventHandler(OnMouseLeave);
                _miniToolStrip.Dispose();
                _miniToolStrip = null;
                _designSurface.Flushed -= new EventHandler(OnLoaderFlushed); 
                _designSurface = null;
                _designer = null; 
 
                OnClosed(new EventArgs());
 
            }
        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.RollBack"]/*' /> 
        /// <devdoc>
        ///      This internal Function is called by item designers to ROLLBACK the current 
        ///      Insitu editing mode. 
        /// </devdoc>
        /// <internalonly/> 
        internal void Commit(bool enterKeyPressed, bool tabKeyPressed)
        {
            // Commit only iff we are still available !!
            if (_miniToolStrip != null && inSituMode) 
            {
                string text = ((TextBox)(centerTextBox.Control)).Text; 
                if (string.IsNullOrEmpty(text)) 
                {
                    RollBack(); 
                }
                else
                {
                    CommitEditor(true, enterKeyPressed, tabKeyPressed); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Internal function that would commit the TemplateNode
        /// </devdoc>
        internal void CommitAndSelect()
        { 
            Commit(false, false);
        } 
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.CommitEditor"]/*' />
        /// <devdoc> 
        ///      This private function performs the job of commiting the current InSitu Editor.
        ///      This will call the CommitEdit(...) function for the appropriate designers
        ///      so that they can actually do their own Specific things for commiting (or ROLLBACKING)
        ///      the Insitu Edit mode. 
        ///      The commit flag is used for commit or rollback.
        ///      BE SURE TO ALWAYS call ExitInSituEdit from this function to 
        ///      put the EditorToolStrip in a sane "NON EDIT" mode. 
        /// </devdoc>
        /// <internalonly/> 
        private void CommitEditor(bool commit, bool enterKeyPressed, bool tabKeyPressed)
        {
            // After the node is commited the templateNode gets the selection.
            // But the original selection is not invalidated. 
            // consider following case
            // FOO -> BAR 
            //    -> TEMPLATENODE node 
            // When the TemplateNode is committed "FOO" is selected
            // but after the commit is complete, The TemplateNode gets the selection but "FOO" is never invalidated 
            // and hence retains selection.
            // So we get the selection and then invalidate it at the end of this function.
            //Get the currentSelection to invalidate
            ToolStripItem curSel = SelectionService.PrimarySelection as ToolStripItem; 

            string text = (centerTextBox != null) ? ((TextBox)(centerTextBox.Control)).Text : String.Empty; 
            ExitInSituEdit(); 
            FocusForm();
 
            if (commit && (_designer is ToolStripDesigner || _designer is ToolStripMenuItemDesigner))
            {
                Type selectedType = null;
                // If user has typed in "-" then Add a Separator only on DropDowns. 
                if (text == "-" && _designer is ToolStripMenuItemDesigner)
                { 
                    ToolStripItemType = typeof(ToolStripSeparator); 
                }
                if (ToolStripItemType != null) 
                {
                    selectedType = ToolStripItemType;
                    ToolStripItemType = null;
                } 
                else
                { 
                    Type[] supportedTypes = ToolStripDesignerUtils.GetStandardItemTypes(component); 
                    selectedType = supportedTypes[0];
                } 
                if (_designer is ToolStripDesigner)
                {
                    ((ToolStripDesigner)_designer).AddNewItem(selectedType, text, enterKeyPressed, tabKeyPressed);
                } 
                else
                { 
                    ((ToolStripItemDesigner)_designer).CommitEdit(selectedType, text, commit, enterKeyPressed, tabKeyPressed); 
                }
            } 
            else if (_designer is ToolStripItemDesigner)
            {
                ((ToolStripItemDesigner)_designer).CommitEdit(_designer.Component.GetType(), text, commit, enterKeyPressed, tabKeyPressed);
            } 

            // finally Invalidate the selection rect ... 
            if (curSel != null) 
            {
                if (_designerHost != null) 
                {
                    ToolStripItemDesigner designer = _designerHost.GetDesigner(curSel) as ToolStripItemDesigner;
                    if (designer != null)
                    { 
                        Rectangle invalidateBounds = designer.GetGlyphBounds();
                        ToolStripDesignerUtils.GetAdjustedBounds(curSel, ref invalidateBounds); 
                        invalidateBounds.Inflate(GLYPHBORDER, GLYPHBORDER); 
                        Region rgn = new Region(invalidateBounds);
                        invalidateBounds.Inflate(-GLYPHINSET, -GLYPHINSET); 
                        rgn.Exclude(invalidateBounds);
                        if (BehaviorService != null)
                        {
                            BehaviorService.Invalidate(rgn); 
                        }
                        rgn.Dispose(); 
                    } 
                }
            } 
        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.EnterInSituEdit"]/*' />
        /// <devdoc> 
        ///      The ToolStripTemplateNode enters into InSitu Edit Mode through this Function.
        ///      This Function is called by FocusEditor( ) which starts the InSitu. 
        ///      The centerLabel is SWAPPED by centerTextBox and the ToolStripTemplateNode is Ready for 
        ///      Text.
        ///      Settting "Active = true" pushes the IEventHandler which now intercepts the 
        ///      Escape and Enter keys to ROLLBACK or COMMIT the InSitu Editing respectively.
        /// </devdoc>
        /// <internalonly/>
        private void EnterInSituEdit() 
        {
            if (!inSituMode) 
            { 

 
                // Listen For Commandss....
                if (_miniToolStrip.Parent != null)
                {
                    _miniToolStrip.Parent.SuspendLayout(); 
                }
                try 
                { 

                    Active = true; 
                    inSituMode = true;
                    // set the renderer state to Selected...
                    if (renderer != null)
                    { 
                        renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                    } 
 

                    // Set UP textBox for InSitu 
                    //
                    TextBox tb = new TemplateTextBox(_miniToolStrip, this);
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.Text = centerLabel.Text; 
                    tb.ForeColor = SystemColors.WindowText;
                    int width = 90; 
 
                    centerTextBox = new ToolStripControlHost(tb);
                    centerTextBox.Dock = DockStyle.None; 
                    centerTextBox.AutoSize = false;
                    centerTextBox.Width = width;

 
                    ToolStripDropDownItem item = activeItem as ToolStripDropDownItem;
                    if (item != null && !item.IsOnDropDown) 
                    { 
                        centerTextBox.Margin = new System.Windows.Forms.Padding(1, 2, 1, 3);
                    } 
                    else
                    {
                        centerTextBox.Margin = new System.Windows.Forms.Padding(1);
                    } 

                    centerTextBox.Size = _miniToolStrip.DisplayRectangle.Size - centerTextBox.Margin.Size; 
 
                    centerTextBox.Name = "centerTextBox";
 
                    centerTextBox.MouseEnter += new EventHandler(CenterTextBoxMouseEnter);
                    centerTextBox.MouseLeave += new EventHandler(CenterTextBoxMouseLeave);

                    int index = _miniToolStrip.Items.IndexOf(centerLabel); 

                    //swap in our insitu textbox 
                    if (index != -1) 
                    {
                        _miniToolStrip.Items.Insert(index, centerTextBox); 
                        _miniToolStrip.Items.Remove(centerLabel);
                    }

                    tb.KeyUp += new KeyEventHandler(this.OnKeyUp); 
                    tb.KeyDown += new KeyEventHandler(this.OnKeyDown);
                    tb.SelectAll(); 
 
                    Control baseComponent = null;
 
                    if (_designerHost != null)
                    {
                        baseComponent = (Control)_designerHost.RootComponent;
                        NativeMethods.SendMessage(baseComponent.Handle, NativeMethods.WM_SETREDRAW, 0, 0); 
                        tb.Focus();
                        NativeMethods.SendMessage(baseComponent.Handle, NativeMethods.WM_SETREDRAW, 1, 0); 
                    } 
                }
                finally 
                {
                    if (_miniToolStrip.Parent != null)
                    {
                        _miniToolStrip.Parent.ResumeLayout(); 
                    }
                } 
            } 
        }
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.ExitInSituEdit"]/*' />
        /// <devdoc>
        ///      The ToolStripTemplateNode exits from InSitu Edit Mode through this Function.
        ///      This Function is called by CommitEditor( ) which stops the InSitu. 
        ///      The centerTextBox is SWAPPED by centerLabel and the ToolStripTemplateNode is exits the
        ///      InSitu Mode. 
        ///      Settting "Active = false" pops the IEventHandler. 
        /// </devdoc>
        /// <internalonly/> 
        private void ExitInSituEdit()
        {
            // put the ToolStripTemplateNode back into "non edit state"
 
            if (centerTextBox != null && inSituMode)
            { 
 
                if (_miniToolStrip.Parent != null)
                { 
                    _miniToolStrip.Parent.SuspendLayout();
                }
                try
                { 

                    //if going insitu with a real item, set & select all the text 
                    int index = _miniToolStrip.Items.IndexOf(centerTextBox); 
                    //validate index
                    if (index != -1) 
                    {
                        centerLabel.Text = SR.GetString(SR.ToolStripDesignerTemplateNodeEnterText);
                        //swap in our insitu textbox
                        _miniToolStrip.Items.Insert(index, centerLabel); 
                        _miniToolStrip.Items.Remove(centerTextBox);
                        ((TextBox)(centerTextBox.Control)).KeyUp -= new KeyEventHandler(this.OnKeyUp); 
                        ((TextBox)(centerTextBox.Control)).KeyDown -= new KeyEventHandler(this.OnKeyDown); 

                    } 

                    centerTextBox.MouseEnter -= new EventHandler(CenterTextBoxMouseEnter);
                    centerTextBox.MouseLeave -= new EventHandler(CenterTextBoxMouseLeave);
 
                    centerTextBox.Dispose();
                    centerTextBox = null; 
                    inSituMode = false; 
                    //reset the Size....
                    SetWidth(null); 
                }
                finally
                {
                    if (_miniToolStrip.Parent != null) 
                    {
                        _miniToolStrip.Parent.ResumeLayout(); 
                    } 

                    // POP of the Handler !!! 
                    Active = false;
                }
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.FocusEditor"]/*' /> 
        /// <devdoc> 
        ///      This internal function is called from ToolStripItemDesigner to put the
        ///      current item into InSitu Edit Mode. 
        /// </devdoc>
        /// <internalonly/>
        internal void FocusEditor(ToolStripItem currentItem)
        { 
            if (currentItem != null)
            { 
                centerLabel.Text = currentItem.Text; 
            }
            EnterInSituEdit(); 

        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.ActivateForm"]/*' /> 
        /// <devdoc>
        ///      Called when the user enters into the InSitu edit mode.This keeps the 
        ///      fdesigner Form Active..... 
        /// </devdoc>
        /// <internalonly/> 
        private void FocusForm()
        {
            DesignerFrame designerFrame = component.Site.GetService(typeof(ISplitWindowService)) as DesignerFrame;
            if (designerFrame != null) 
            {
                Control baseComponent = null; 
 
                if (_designerHost != null)
                { 
                    baseComponent = (Control)_designerHost.RootComponent;
                    NativeMethods.SendMessage(baseComponent.Handle, NativeMethods.WM_SETREDRAW, 0, 0);
                    designerFrame.Focus();
                    NativeMethods.SendMessage(baseComponent.Handle, NativeMethods.WM_SETREDRAW, 1, 0); 
                }
 
            } 
        }
 
        /// <include file='doc\AutoCompleteStringCollection.uex' path='docs/doc[@for="AutoCompleteStringCollection.OnActivated"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected void OnActivated(EventArgs e)
        { 
            if (this.onActivated != null) 
            {
                this.onActivated(this, e); 
            }
        }

        private void OnAddItemButtonDropDownOpened(object sender, EventArgs e) 
        {
            addItemButton.DropDown.Focus(); 
        } 

        /// <include file='doc\AutoCompleteStringCollection.uex' path='docs/doc[@for="AutoCompleteStringCollection.OnCollectionChanged"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected void OnClosed(EventArgs e) 
        {
            if (this.onClosed != null) 
            { 
                this.onClosed(this, e);
            } 
        }

        /// <devdoc>
        ///      Painting of the templateNode on when the contextMenu is closed 
        /// </devdoc>
        private void OnContextMenuClosed(object sender, ToolStripDropDownClosedEventArgs e) 
        { 
            if (renderer != null)
            { 
                renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                _miniToolStrip.Invalidate();
            }
        } 

        /// <devdoc> 
        ///      Set the KeyBoardService member, so the the deisgner knows that the "ContextMenu" is opened. 
        /// </devdoc>
        private void OnContextMenuOpened(object sender, EventArgs e) 
        {
            // Disable All Commands .. the Commands would be reenabled by AddNewItemClick call.
            if (KeyboardService != null)
            { 
                KeyboardService.TemplateNodeContextMenuOpen = true;
            } 
        } 

        /// <include file='doc\AutoCompleteStringCollection.uex' path='docs/doc[@for="AutoCompleteStringCollection.OnDeactivated"]/*' /> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected void OnDeactivated(EventArgs e)
        { 
            if (this.onDeactivated != null)
            { 
                this.onDeactivated(this, e); 
            }
        } 

        /// <devdoc>
        ///     Called by the design surface when it is being
        ///     flushed.  This will save any changes made to TemplateNode. 
        ///
        /// </devdoc> 
        private void OnLoaderFlushed(object sender, EventArgs e) { 
            Commit(false, false);
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OnKeyUp"]/*' />
        /// <devdoc>
        ///      This is small HACK. For some reason if the InSituEditor's textbox has focus the 
        ///      escape key is lost and the menu service doesnt get it.... but the textbox gets it.
        ///      So need to check for the escape key here and call CommitEditor(false) which 
        ///      will ROLLBACK the edit. 
        /// </devdoc>
        /// <internalonly/> 
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (IMEModeSet)
            { 
                return;
            } 
            switch (e.KeyCode) 
            {
 
                case Keys.Up:
                    Commit(false, true);
                    if (KeyboardService != null)
                    { 
                        KeyboardService.ProcessUpDown(false);
                    } 
                    break; 
                case Keys.Down:
                    Commit(true, false); 
                    break;
                case Keys.Escape:
                    CommitEditor(false, false, false);
                    break; 
                case Keys.Return:
                    if (ignoreFirstKeyUp) 
                    { 
                        ignoreFirstKeyUp = false;
                        return; 
                    }
                    OnKeyDefaultAction(sender, e);
                    break;
 
            }
        } 
 
        /// <devdoc>
        ///      Select text on KeyDown. 
        /// </devdoc>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (IMEModeSet) 
            {
                return; 
            } 
            if (e.KeyCode == Keys.A && (e.KeyData & Keys.Control) != 0)
            { 
                TextBox t = sender as TextBox;
                if (t != null)
                {
                    t.SelectAll(); 
                }
            } 
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OnKeyDefaultAction"]/*' /> 
        /// <devdoc>
        ///      Check for the Enter key here and call CommitEditor(true) which
        ///      will COMMIT the edit.
        /// </devdoc> 
        /// <internalonly/>
        private void OnKeyDefaultAction(object sender, EventArgs e) 
        { 
            //exit Insitu with commiting....
            Active = false; 
            Debug.Assert(centerTextBox.Control != null, "The TextBox is null");
            if (centerTextBox.Control != null)
            {
                string text = ((TextBox)(centerTextBox.Control)).Text; 
                if (string.IsNullOrEmpty(text))
                { 
                    CommitEditor(false, false, false); 
                }
                else 
                {
                    CommitEditor(true, true, false);
                }
 
            }
        } 
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OnMenuCut"]/*' />
        /// <devdoc> 
        ///     Called when the delete menu item is selected.
        /// </devdoc>
        private void OnMenuCut(object sender, EventArgs e)
        { 

        } 
 
        /// <devdoc>
        ///      Show ContextMenu if the Right Mouse button was pressed and we have received the following MouseUp 
        /// </devdoc>
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) 
            {
                if (BehaviorService != null) 
                { 
                    Point loc = BehaviorService.ControlToAdornerWindow(_miniToolStrip);
                    loc = BehaviorService.AdornerWindowPointToScreen(loc); 
                    loc.Offset(e.Location);
                    DesignerContextMenu.Show(loc);
                }
            } 

        } 
 
        /// <devdoc>
        ///      Set the selection to the component. 
        /// </devdoc>
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (KeyboardService != null) { 
                KeyboardService.SelectedDesignerControl = controlHost;
            } 
            SelectionService.SetSelectedComponents(null, SelectionTypes.Replace); 

        } 

        /// <devdoc>
        ///      Painting on the button for mouse Move.
        /// </devdoc> 
        private void OnMouseMove(object sender, MouseEventArgs e)
        { 
            renderer.State = (int)TemplateNodeSelectionState.None; 
            if (renderer != null)
            { 
                if (addItemButton != null)
                {
                    if (addItemButton.ButtonBounds.Contains(e.Location))
                    { 
                        renderer.State = (int)TemplateNodeSelectionState.SplitButtonSelected;
                    } 
                    else if (addItemButton.DropDownButtonBounds.Contains(e.Location)) 
                    {
                        renderer.State = (int)TemplateNodeSelectionState.DropDownSelected; 
                    }
                }
                _miniToolStrip.Invalidate();
            } 
        }
 
        /// <devdoc> 
        ///      Painting on the button for mouse Leave.
        /// </devdoc> 
        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (SelectionService != null)
            { 
                ToolStripItem selectedObj = SelectionService.PrimarySelection as ToolStripItem;
                if (selectedObj != null && renderer != null && renderer.State != (int)TemplateNodeSelectionState.HotRegionSelected) 
                { 
                    renderer.State = (int)TemplateNodeSelectionState.None;
                } 
                if (KeyboardService != null && KeyboardService.SelectedDesignerControl == controlHost)
                {
                    renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                } 

                _miniToolStrip.Invalidate(); 
            } 
        }
 
        private void OnRightToLeftChanged(object sender, EventArgs e)
        {
            ToolStrip strip = sender as ToolStrip;
            if (strip != null) 
            {
                _miniToolStrip.RightToLeft = strip.RightToLeft; 
            } 
            else
            { 
                ToolStripDropDownItem stripItem = sender as ToolStripDropDownItem;
                _miniToolStrip.RightToLeft = stripItem.RightToLeft;
            }
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OverrideInvoke"]/*' /> 
        /// <devdoc> 
        ///     Intercept invokation of specific commands and keys
        /// </devdoc> 
        public bool OverrideInvoke(MenuCommand cmd)
        {
            for (int i = 0; i < commands.Length; i++)
            { 
                if (commands[i].CommandID.Equals(cmd.CommandID))
                { 
                    if (cmd.CommandID == MenuCommands.Delete || cmd.CommandID == MenuCommands.Cut || cmd.CommandID == MenuCommands.Copy) 
                    {
                        commands[i].Invoke(); 
                        return true;
                    }
                }
            } 
            return false;
        } 
 
        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.OverrideStatus"]/*' />
        /// <devdoc> 
        ///     Intercept invokation of specific commands and keys
        /// </devdoc>
        public bool OverrideStatus(MenuCommand cmd)
        { 

            for (int i = 0; i < commands.Length; i++) 
            { 
                if (commands[i].CommandID.Equals(cmd.CommandID))
                { 
                    cmd.Enabled = false;
                    return true;
                }
            } 

            return false; 
        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.RollBack"]/*' /> 
        /// <devdoc>
        ///      This internal Function is called by item designers to ROLLBACK the current
        ///      Insitu editing mode.
        /// </devdoc> 
        /// <internalonly/>
        internal void RollBack() 
        { 
            // RollBack only iff we are still available !!
            if (_miniToolStrip != null && inSituMode) 
            {
                CommitEditor(false, false, false);
            }
        } 
        /// <devdoc>
        ///     Show the contextMenu... 
        /// </devdoc> 
        /// <internalonly/>
        internal void ShowContextMenu(Point pt) 
        {
            DesignerContextMenu.Show(pt);
        }
 
        /// <devdoc>
        ///     Show the drop Down Menu... 
        /// </devdoc> 
        /// <internalonly/>
        internal void ShowDropDownMenu() 
        {
            if (addItemButton != null)
            {
                addItemButton.ShowDropDown(); 
            }
            else 
            { 
                if (BehaviorService != null)
                { 
                    Point loc = BehaviorService.ControlToAdornerWindow(_miniToolStrip);
                    loc = BehaviorService.AdornerWindowPointToScreen(loc);
                    Rectangle translatedBounds = new Rectangle(loc, _miniToolStrip.Size);
                    if (contextMenu == null) 
                    {
                        contextMenu = ToolStripDesignerUtils.GetNewItemDropDown(component, null, new EventHandler(AddNewItemClick), false, component.Site); 
                        contextMenu.Closed += new ToolStripDropDownClosedEventHandler(OnContextMenuClosed); 
                        contextMenu.Opened += new EventHandler(OnContextMenuOpened);
                        contextMenu.Text = "ItemSelectionMenu"; 
                    }
                    ToolStrip strip = component as ToolStrip;
                    if (strip != null)
                    { 
                        contextMenu.RightToLeft = strip.RightToLeft;
                    } 
                    else 
                    {
                        ToolStripDropDownItem stripItem = component as ToolStripDropDownItem; 
                        if (stripItem != null)
                        {
                            contextMenu.RightToLeft = stripItem.RightToLeft;
                        } 
                    }
                    contextMenu.Show(translatedBounds.X, translatedBounds.Y + translatedBounds.Height); 
                    contextMenu.Focus(); 

                    if (renderer != null) 
                    {
                        renderer.State = (int)TemplateNodeSelectionState.HotRegionSelected;
                        _miniToolStrip.Invalidate();
                    } 
                }
            } 
        } 

 

        /// <devdoc>
        ///      This function sets up the Menu specific TemplateNODE..
        /// </devdoc> 
        /// <internalonly/>
        private void SetUpMenuTemplateNode(ToolStripTemplateNode owner, string text, Image image, IComponent currentItem) 
        { 
            centerLabel = new ToolStripLabel();
            centerLabel.Text = text; 
            centerLabel.AutoSize = false;
            centerLabel.IsLink = false;

            centerLabel.Margin = new System.Windows.Forms.Padding(1); 
            if (currentItem is ToolStripDropDownItem)
            { 
                centerLabel.Margin = new System.Windows.Forms.Padding(1, 2, 1, 3); 
            }
            centerLabel.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0); 
            centerLabel.Name = "centerLabel";

            centerLabel.Size = _miniToolStrip.DisplayRectangle.Size - centerLabel.Margin.Size;
            centerLabel.ToolTipText = SR.GetString(SR.ToolStripDesignerTemplateNodeLabelToolTip); 

            centerLabel.MouseUp += new MouseEventHandler(CenterLabelClick); 
            centerLabel.MouseEnter += new EventHandler(CenterLabelMouseEnter); 
            centerLabel.MouseMove += new MouseEventHandler(CenterLabelMouseMove);
            centerLabel.MouseLeave += new EventHandler(CenterLabelMouseLeave); 

            _miniToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { centerLabel });

 
        }
 
        /// <devdoc> 
        ///      This function sets up the Menu specific TemplateNODE..
        /// </devdoc> 
        /// <internalonly/>
        // Standard 'catch all - rethrow critical' exception pattern
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void SetUpToolTemplateNode(ToolStripTemplateNode owner, string text, Image image, IComponent component) 
        { 

            addItemButton = new ToolStripSplitButton(); 
            addItemButton.AutoSize = false;
            addItemButton.Margin = new Padding(1);
            addItemButton.Size = _miniToolStrip.DisplayRectangle.Size - addItemButton.Margin.Size;
            addItemButton.DropDownButtonWidth = 11; 
            addItemButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            if (component is StatusStrip) 
            { 
                addItemButton.ToolTipText = SR.GetString(SR.ToolStripDesignerTemplateNodeSplitButtonStatusStripToolTip);
            } 
            else
            {
                addItemButton.ToolTipText = SR.GetString(SR.ToolStripDesignerTemplateNodeSplitButtonToolTip);
            } 

            addItemButton.MouseDown += new System.Windows.Forms.MouseEventHandler(OnMouseDown); 
            addItemButton.MouseMove += new System.Windows.Forms.MouseEventHandler(OnMouseMove); 
            addItemButton.MouseUp += new System.Windows.Forms.MouseEventHandler(OnMouseUp);
            addItemButton.DropDownOpened += new EventHandler(OnAddItemButtonDropDownOpened); 

            contextMenu = ToolStripDesignerUtils.GetNewItemDropDown(component, null, new EventHandler(AddNewItemClick), false, component.Site);
            contextMenu.Text = "ItemSelectionMenu";
 
            contextMenu.Closed += new ToolStripDropDownClosedEventHandler(OnContextMenuClosed);
            contextMenu.Opened += new EventHandler(OnContextMenuOpened); 
 
            addItemButton.DropDown = contextMenu;
            // 
            //  Set up default item and image.
            //
            try
            { 

                if (addItemButton.DropDownItems.Count > 0) 
                { 
                    ItemTypeToolStripMenuItem firstItem = (ItemTypeToolStripMenuItem)addItemButton.DropDownItems[0];
                    addItemButton.ImageTransparentColor = Color.Lime; 
                    addItemButton.Image = new Bitmap(typeof(ToolStripTemplateNode), "ToolStripTemplateNode.bmp");
                    addItemButton.DefaultItem = firstItem;
                }
            } 
            catch (Exception ex)
            { 
                if (ClientUtils.IsCriticalException(ex)) 
                {
                    throw; 
                }
            }

            _miniToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                addItemButton
            }); 
        } 

 

        /// <include file='doc\TemplateNode.uex' path='docs/doc[@for="TemplateNode.SetupNewEditNode"]/*' />
        /// <devdoc>
        ///      This method does actual edit node creation. 
        /// </devdoc>
        /// <internalonly/> 
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
        private void SetupNewEditNode(ToolStripTemplateNode owner, string text, Image image, IComponent currentItem)
        { 
            //
            // setup the MINIToolStrip host...
            //
            renderer = new MiniToolStripRenderer(owner); 

            _miniToolStrip = new TransparentToolStrip(owner); 
 
            ToolStrip strip = currentItem as ToolStrip;
            if (strip != null) 
            {
                _miniToolStrip.RightToLeft = strip.RightToLeft;
                strip.RightToLeftChanged += new System.EventHandler(this.OnRightToLeftChanged);
            } 
            ToolStripDropDownItem stripItem = currentItem as ToolStripDropDownItem;
            if (stripItem != null) 
            { 
                _miniToolStrip.RightToLeft = stripItem.RightToLeft;
                stripItem.RightToLeftChanged += new System.EventHandler(this.OnRightToLeftChanged); 
            }

            //
            // _miniToolStrip 
            //
            _miniToolStrip.SuspendLayout(); 
            _miniToolStrip.CanOverflow = false; 
            _miniToolStrip.Cursor = System.Windows.Forms.Cursors.Default;
            _miniToolStrip.Dock = System.Windows.Forms.DockStyle.None; 
            _miniToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _miniToolStrip.Name = "miniToolStrip";
            _miniToolStrip.TabIndex = 0;
            _miniToolStrip.Text = "miniToolStrip"; 
            _miniToolStrip.Visible = true;
            _miniToolStrip.Renderer = renderer; 
 
            // ADD items to the Template ToolStrip depending upon the Parent Type...
            if (currentItem is MenuStrip || currentItem is ToolStripDropDownItem) 
            {
                SetUpMenuTemplateNode(owner, text, image, currentItem);
            }
            else 
            {
 
                SetUpToolTemplateNode(owner, text, image, currentItem); 
            }
 
            _miniToolStrip.MouseLeave += new System.EventHandler(OnMouseLeave);
            _miniToolStrip.ResumeLayout();

        } 

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.SetWidth"]/*' /> 
        /// <devdoc> 
        ///      This method does sets the width of the Editor (_miniToolStrip) based on the
        ///      text passed in. 
        /// </devdoc>
        /// <internalonly/>
        internal void SetWidth(string text)
        { 
            //
            if (string.IsNullOrEmpty(text)) 
            { 
                _miniToolStrip.Width = centerLabel.Width + 2;
 
            }
            else
            {
                centerLabel.Text = text; 
            }
 
        } 

        /// <devdoc> 
        ///      Private class that implements the textBox for the InSitu Editor.
        /// </devdoc>
        private class TemplateTextBox : TextBox
        { 
            TransparentToolStrip parent;
            ToolStripTemplateNode owner; 
            private const int IMEMODE = 229; 

            public TemplateTextBox(TransparentToolStrip parent, ToolStripTemplateNode owner) 
                : base()
            {
                this.parent = parent;
                this.owner = owner; 
                this.AutoSize = false;
                this.Multiline = false; 
            } 

            /// <devdoc> 
            ///      Get Parent Handle.
            /// </devdoc>
            private bool IsParentWindow(IntPtr hWnd)
            { 
                if (hWnd == parent.Handle)
                { 
                    return true; 
                }
                return false; 
            }

            protected override bool IsInputKey(Keys keyData) {
                switch (keyData & Keys.KeyCode) { 
                    case Keys.Return:
                        owner.Commit(true, false); 
                        return true; 
                }
                return base.IsInputKey(keyData); 
            }

            /// <devdoc>
            ///      Process the IMEMode message.. 
            /// </devdoc>
            protected override bool ProcessDialogKey(Keys keyData) 
            { 
                if ((int)keyData == IMEMODE)
                { 
                    owner.IMEModeSet = true;
                }
                else
                { 
                    owner.IMEModeSet = false;
                    owner.ignoreFirstKeyUp = false; 
                } 
                return base.ProcessDialogKey(keyData);
            } 

            /// <devdoc>
            ///      Process the WNDPROC for WM_KILLFOCUS to commit the Insitu Editor..
            /// </devdoc> 
            protected override void WndProc(ref Message m)
            { 
                switch (m.Msg) 
                {
                    case NativeMethods.WM_KILLFOCUS: 
                        base.WndProc(ref m);
                        IntPtr focussedWindow = (IntPtr)m.WParam;
                        if (!IsParentWindow(focussedWindow))
                        { 
                            owner.Commit(false, false);
                        } 
                        break; 

                    // DevDiv Bugs 144618 : 
                    // 1.Slowly click on a menu strip item twice to make it editable, while the item's dropdown menu is visible
                    // 2.Select the text of the item and right click on it
                    // 3.Left click 'Copy' or 'Cut' in the context menu
                    // IDE crashed because left click in step3 invoked glyph 
                    // behavior, which commited and destroyed the insitu edit box and thus
                    // the 'copy' or 'cut' action has no text to work with. 
                    // Thus need to block glyph behaviors while the context menu is displayed. 
                    case NativeMethods.WM_CONTEXTMENU:
                        this.owner.IsSystemContextMenuDisplayed = true; 
                        base.WndProc(ref m);
                        this.owner.IsSystemContextMenuDisplayed = false;
                        break;
 
                    default:
                        base.WndProc(ref m); 
                        break; 
                }
            } 
        }

        /// <include file='doc\ToolStripTemplateNode.uex' path='docs/doc[@for="ToolStripTemplateNode.TransparentToolStrip"]/*' />
        /// <devdoc> 
        ///      Private class to Change the Winbar to a TranparentWinbar.
        ///      Our EditorToolStrip is a TranparentToolStrip so that it picks up the itemColor. 
        /// </devdoc> 
        /// <internalonly/>
        public class TransparentToolStrip : ToolStrip 
        {
            ToolStripTemplateNode owner;
            IComponent currentItem;
 
            public TransparentToolStrip(ToolStripTemplateNode owner)
            { 
                this.owner = owner; 
                currentItem = owner.component;
                this.TabStop = true; 
                SetStyle(ControlStyles.Selectable, true);
                this.AutoSize = false;
            }
 
            /// <devdoc>
            ///      Owner TemplateNode.. 
            /// </devdoc> 
            public ToolStripTemplateNode TemplateNode
            { 
                get
                {
                    return owner;
                } 
            }
 
            /// <devdoc> 
            ///      Commit the node and move to next selection.
            /// </devdoc> 
            private void CommitAndSelectNext(bool forward)
            {
                owner.Commit(false, true);
                if (owner.KeyboardService != null) 
                {
                    owner.KeyboardService.ProcessKeySelect(!forward, null); 
                } 
            }
 
            /// <devdoc>
            ///      get current selection.
            /// </devdoc>
            private ToolStripItem GetSelectedItem() 
            {
                ToolStripItem selectedItem = null; 
                for (int i = 0; i < Items.Count; i++) 
                {
                    if (Items[i].Selected) 
                    {
                        selectedItem = Items[i];
                    }
                } 
                return selectedItem;
            } 
 
            /// <include file='doc\TransparentToolStrip.uex' path='docs/doc[@for="TransparentToolStrip.GetPreferredSize"]/*' />
            [EditorBrowsable(EditorBrowsableState.Advanced)] 
            public override Size GetPreferredSize(Size proposedSize)
            {
                if (currentItem is ToolStripDropDownItem)
                { 
                    return new Size(this.Width, 22);
                } 
                else 
                {
                    return new Size(this.Width, 19); 
                }
            }

            /// <devdoc> 
            ///     Process the Tab Key..
            /// </devdoc> 
            private bool ProcessTabKey(bool forward) 
            {
                // Give the ToolStripItem first dibs 
                ToolStripItem item = this.GetSelectedItem();
                if (item is ToolStripControlHost)
                {
 
                    CommitAndSelectNext(forward);
                    return true; 
                } 
                return false;
            } 

            /// <devdoc>
            ///      Process the Dialog Keys for the Templatenode ToolStrip..
            /// </devdoc> 
            protected override bool ProcessDialogKey(Keys keyData)
            { 
                bool retVal = false; 
                if (owner.Active)
                { 
                    if ((keyData & (Keys.Alt | Keys.Control)) == Keys.None)
                    {

                        Keys keyCode = (Keys)keyData & Keys.KeyCode; 

                        switch (keyCode) 
                        { 
                            case Keys.Tab:
                                retVal = ProcessTabKey((keyData & Keys.Shift) == Keys.None); 
                                break;
                        }
                    }
 
                    if (retVal)
                    { 
                        return retVal; 
                    }
                } 
                return base.ProcessDialogKey(keyData);
            }

            /// <include file='doc\Form.uex' path='docs/doc[@for="TransparentToolStrip.SetBoundsCore"]/*' /> 
            /// <devdoc>
            /// </devdoc> 
            [EditorBrowsable(EditorBrowsableState.Advanced)] 
            protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
            { 
                if (currentItem is ToolStripDropDownItem)
                {
                    base.SetBoundsCore(x, y, 92, 22, specified);
                } 
                else if (currentItem is MenuStrip)
                { 
                    base.SetBoundsCore(x, y, 92, 19, specified); 
                }
                else 
                {
                    base.SetBoundsCore(x, y, 31, 19, specified);
                }
            } 
        }
 
        /// <devdoc> 
        ///      Private class that implements the custom Renderer for the TemplateNode ToolStrip.
        /// </devdoc> 
        public class MiniToolStripRenderer : ToolStripSystemRenderer
        {
            private int state = (int)TemplateNodeSelectionState.None;
            private Color selectedBorderColor; 
            private Color defaultBorderColor;
            private Color dropDownMouseOverColor; 
            private Color dropDownMouseDownColor; 
            private Color toolStripBorderColor;
 
            private ToolStripTemplateNode owner;
            private Rectangle hotRegion = Rectangle.Empty;

 

 
            public MiniToolStripRenderer(ToolStripTemplateNode owner) 
                : base()
            { 

                //Add Colors
                this.owner = owner;
                selectedBorderColor = Color.FromArgb(46, 106, 197); 
                defaultBorderColor = Color.FromArgb(171, 171, 171);
                dropDownMouseOverColor = Color.FromArgb(193, 210, 238); 
                dropDownMouseDownColor = Color.FromArgb(152, 181, 226); 
                toolStripBorderColor = Color.White;
 
            }

            /// <devdoc>
            ///      Current state of the TemplateNode UI.. 
            /// </devdoc>
            public int State 
            { 
                get
                { 
                    return state;
                }
                set
                { 
                    state = value;
                } 
            } 

            /// <devdoc> 
            ///      Custom method to draw DOWN arrow on the DropDown.
            /// </devdoc>
            private void DrawArrow(Graphics g, Rectangle bounds)
            { 
                bounds.Width--;
                DrawArrow(new ToolStripArrowRenderEventArgs(g, null, bounds, SystemColors.ControlText, ArrowDirection.Down)); 
            } 

            /// <devdoc> 
            ///      Drawing different DropDown states.
            /// </devdoc>
            private void DrawDropDown(Graphics g, Rectangle bounds, int state)
            { 
                switch (state)
                { 
                    case 4: //MouseOver 
                        using (LinearGradientBrush brush = new LinearGradientBrush(bounds, Color.White, defaultBorderColor, LinearGradientMode.Vertical))
                        { 
                            g.FillRectangle(brush, bounds);
                        }
                        break;
                    case 5: //MouseOnthe HotRegion 
                        using (SolidBrush b = new SolidBrush(dropDownMouseOverColor))
                        { 
                            g.FillRectangle(b, hotRegion); 
                        }
                        break; 
                    case 6: //HotRegionSelected
                        using (SolidBrush b = new SolidBrush(dropDownMouseDownColor))
                        {
                            g.FillRectangle(b, hotRegion); 
                        }
                        break; 
                } 
                DrawArrow(g, bounds);
 
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) {
                if (owner.component is MenuStrip || owner.component is ToolStripDropDownItem) 
                {
                    Graphics g = e.Graphics; 
                    g.Clear(toolStripBorderColor); 
                }
                else 
                {
                    base.OnRenderToolStripBackground(e);
                }
 
            }
 
 
            /// <devdoc>
            ///      Render ToolStrip Border 
            /// </devdoc>
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                Graphics g = e.Graphics; 
                Rectangle bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
                Pen selectborderPen = new Pen(toolStripBorderColor); 
                Rectangle drawRect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1); 
                g.DrawRectangle(selectborderPen, drawRect);
                selectborderPen.Dispose(); 
            }

            /// <devdoc>
            ///      Render the Center Label on the TemplateNode ToolStrip. 
            /// </devdoc>
            protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e) 
            { 
                base.OnRenderLabelBackground(e);
 
                ToolStripItem item = e.Item;
                ToolStrip tool = e.ToolStrip;
                Graphics g = e.Graphics;
                Rectangle bounds = new Rectangle(Point.Empty, item.Size); 

                Rectangle drawRect = new Rectangle(bounds.X, bounds.Y, bounds.Width -1, bounds.Height -1); 
                Pen borderPen = new Pen(defaultBorderColor); 

 
                if (state == (int)TemplateNodeSelectionState.TemplateNodeSelected) //state Template node is selected.
                {
                    g.FillRectangle(new SolidBrush(toolStripBorderColor), drawRect);
 
                    if (owner.EditorToolStrip.RightToLeft == RightToLeft.Yes)
                    { 
                        hotRegion = new Rectangle(bounds.Left + 2, bounds.Top + 2, 9, bounds.Bottom -4); 
                    }
                    else 
                    {
                        hotRegion = new Rectangle(bounds.Right - 11, bounds.Top + 2, 9, bounds.Bottom -4);
                    }
                    owner.HotRegion = hotRegion; 

                    // do the Actual Drawing 
                    borderPen.Color = Color.Black; 
                    item.ForeColor = defaultBorderColor;
                    g.DrawRectangle(borderPen, drawRect); 
                }

                if (state == (int)TemplateNodeSelectionState.MouseOverLabel) //state Template node is selected.
                { 
                    if (owner.EditorToolStrip.RightToLeft == RightToLeft.Yes)
                    { 
                        hotRegion = new Rectangle(bounds.Left + 2, bounds.Top + 2, 9, bounds.Bottom -4); 
                    }
                    else 
                    {
                        hotRegion = new Rectangle(bounds.Right - 11, bounds.Top + 2, 9, bounds.Bottom -4);
                    }
                    owner.HotRegion = hotRegion; 

                    g.Clear(toolStripBorderColor); 
                    DrawDropDown(g, hotRegion, state); 

                    borderPen.Color = Color.Black; 
                    borderPen.DashStyle = DashStyle.Dot;
                    g.DrawRectangle(borderPen, drawRect);
                }
 
                if (state == (int)TemplateNodeSelectionState.MouseOverHotRegion)
                { 
                    g.Clear(toolStripBorderColor); 
                    DrawDropDown(g, hotRegion, state);
 
                    borderPen.Color = Color.Black;
                    borderPen.DashStyle = DashStyle.Dot;

                    item.ForeColor = defaultBorderColor; 
                    g.DrawRectangle(borderPen, drawRect);
                } 
 
                if (state == (int)TemplateNodeSelectionState.HotRegionSelected)
                { 
                    g.Clear(toolStripBorderColor);
                    DrawDropDown(g, hotRegion, state);

                    borderPen.Color = Color.Black; 

                    item.ForeColor = defaultBorderColor; 
                    g.DrawRectangle(borderPen, drawRect); 
                }
 
                if (state == (int)TemplateNodeSelectionState.None) //state Template node is not selected.
                {
                    g.Clear(toolStripBorderColor);
                    g.DrawRectangle(borderPen, drawRect); 
                    item.ForeColor = defaultBorderColor;
                } 
 
                borderPen.Dispose();
 
            }

            /// <devdoc>
            ///      Render the splitButton on the TemplateNode ToolStrip.. 
            /// </devdoc>
            protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e) 
            { 
                // DONT CALL THE BASE AS IT DOESNT ALLOW US TO RENDER THE DROPDOWN BUTTON ....
                //base.OnRenderSplitButtonBackground(e); 
                Graphics g = e.Graphics;
                ToolStripSplitButton splitButton = e.Item as ToolStripSplitButton;

                if (splitButton != null) 
                {
                    // Get the DropDownButton Bounds 
                    Rectangle buttonBounds = splitButton.DropDownButtonBounds; 
                    // Draw the White Divider Line...
                    using (Pen p = new Pen(toolStripBorderColor)) 
                    {
                        g.DrawLine(p, buttonBounds.Left, buttonBounds.Top + 1, buttonBounds.Left, buttonBounds.Bottom - 1);
                    }
 
                    Rectangle bounds = new Rectangle(Point.Empty, splitButton.Size);
                    Pen selectborderPen = null; 
                    bool splitButtonSelected = false; 

                    if (splitButton.DropDownButtonPressed) 
                    {
                        //Button is pressed
                        state = 0;
                        Rectangle fillRect = new Rectangle(buttonBounds.Left + 1, buttonBounds.Top, buttonBounds.Right, buttonBounds.Bottom); 
                        g.FillRectangle(new SolidBrush(dropDownMouseDownColor), fillRect);
                        splitButtonSelected = true; 
                    } 
                    else if (state == (int)TemplateNodeSelectionState.SplitButtonSelected)
                    { 
                        g.FillRectangle(new SolidBrush(dropDownMouseOverColor), splitButton.ButtonBounds);
                        splitButtonSelected = true;
                    }
                    else if (state == (int)TemplateNodeSelectionState.DropDownSelected) 
                    {
                        Rectangle fillRect = new Rectangle(buttonBounds.Left + 1, buttonBounds.Top, buttonBounds.Right, buttonBounds.Bottom); 
                        g.FillRectangle(new SolidBrush(dropDownMouseOverColor), fillRect); 
                        splitButtonSelected = true;
                    } 
                    else if (state == (int)TemplateNodeSelectionState.TemplateNodeSelected)
                    {
                        splitButtonSelected = true;
                    } 

                    if (splitButtonSelected) 
                    { 
                        //DrawSeleted Boder
                        selectborderPen = new Pen(selectedBorderColor); 
                    }
                    else
                    {
                        // Draw Gray Border 
                        selectborderPen = new Pen(defaultBorderColor);
                    } 
 
                    Rectangle drawRect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                    g.DrawRectangle(selectborderPen, drawRect); 
                    selectborderPen.Dispose();

                    // Draw the Arrow
                    DrawArrow(new ToolStripArrowRenderEventArgs(g, splitButton, splitButton.DropDownButtonBounds, SystemColors.ControlText, ArrowDirection.Down)); 
                }
            } 
        } 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
