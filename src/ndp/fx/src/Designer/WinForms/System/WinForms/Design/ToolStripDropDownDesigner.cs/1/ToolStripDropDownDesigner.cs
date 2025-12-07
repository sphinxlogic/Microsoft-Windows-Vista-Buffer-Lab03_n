//------------------------------------------------------------------------------ 
// <copyright file="ToolStripDropDownItemDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Windows.Forms.Design.ToolStripDropDownDesigner..ctor()")]
 
namespace System.Windows.Forms.Design
{
    using System.Design;
    using Accessibility; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.Windows.Forms;
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing;
    using System.Configuration; 
    using System.Globalization; 

    /// <summary> 
    /// Designer for ToolStripDropDowns...just provides the Edit... verb.
    /// </summary>
    internal class ToolStripDropDownDesigner : ComponentDesigner
    { 

        private ISelectionService selSvc; 
 
        private MenuStrip designMenu;
        private ToolStripMenuItem menuItem; 
        private IDesignerHost host;

        private ToolStripDropDown dropDown;
 
        private bool selected;
        private ControlBodyGlyph dummyToolStripGlyph; 
 
        private uint _editingCollection = 0;      // non-zero if the collection editor is up for this winbar or a child of it.
 
        MainMenu parentMenu = null;
        FormDocumentDesigner parentFormDesigner = null;

 
        internal ToolStripMenuItem currentParent = null;
        private INestedContainer nestedContainer = null; //NestedContainer for our DesignTime MenuItem. 
 
        private UndoEngine undoEngine = null;
 
        /// <devdoc>
        ///      ShadowProperty.
        /// </devdoc>
        private bool AutoClose 
        {
            get 
            { 
                return (bool)ShadowProperties["AutoClose"];
            } 
            set
            {
                ShadowProperties["AutoClose"] = value;
            } 
        }
 
 
        /// <devdoc>
        ///      ShadowProperty. 
        /// </devdoc>
        private bool AllowDrop
        {
            get 
            {
                return (bool)ShadowProperties["AllowDrop"]; 
            } 
            set
            { 
                ShadowProperties["AllowDrop"] = value;
            }
        }
 
        /// <include file='doc\ToolStripItemDesigner.uex' path='docs/doc[@for="ToolStripItemDesigner.ActionLists"]/*' />
        /// <summary> 
        /// Adds designer actions to the ActionLists collection. 
        /// </summary>
        public override DesignerActionListCollection ActionLists 
        {
            get
            {
 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                ContextMenuStripActionList cmActionList = new ContextMenuStripActionList(this); 
                if (cmActionList != null)
                { 
                    actionLists.Add(cmActionList);
                }

                // finally add the verbs for this component there... 
                DesignerVerbCollection cmVerbs = this.Verbs;
                if (cmVerbs != null && cmVerbs.Count != 0) 
                { 
                    DesignerVerb[] cmverbsArray = new DesignerVerb[cmVerbs.Count];
                    cmVerbs.CopyTo(cmverbsArray, 0); 
                    actionLists.Add(new DesignerActionVerbList(cmverbsArray));
                }
                return actionLists;
            } 
        }
 
        /// <summary> 
        ///  The ToolStripItems are the associated components.
        ///  We want those to come with in any cut, copy opreations. 
        /// </summary>
        public override System.Collections.ICollection AssociatedComponents
        {
            get 
            {
                return ((ToolStrip)Component).Items; 
            } 
        }
 
        // Dummy menuItem that is used for the contextMenuStrip design
        public ToolStripMenuItem DesignerMenuItem
        {
            get 
            {
                return menuItem; 
            } 
        }
 
        /// <summary>
        ///  Set by the ToolStripItemCollectionEditor when it's launched for this The Items property doesnt open another instance
        ///  of collectioneditor.  We count this so that we can deal with nestings.
        /// </summary> 
        internal bool EditingCollection
        { 
            get 
            {
                return _editingCollection != 0; 
            }
            set
            {
                if (value) 
                {
                    _editingCollection++; 
                } 
                else
                { 
                    _editingCollection--;
                }

            } 
        }
 
        // ContextMenuStrip if Inherited ACT as Readonly. 
        protected override InheritanceAttribute InheritanceAttribute
        { 
            get
            {
                if ((base.InheritanceAttribute == InheritanceAttribute.Inherited))
                { 
                    return InheritanceAttribute.InheritedReadOnly;
                } 
                return base.InheritanceAttribute; 
            }
        } 


        /// <summary>
        ///  Prefilter this property so that we can set the right To Left on the Design Menu... 
        /// </summary>
        private RightToLeft RightToLeft 
        { 
            get
            { 
                return dropDown.RightToLeft;
            }
            set
            { 
                if (menuItem != null && designMenu != null && value != RightToLeft)
                { 
                    Rectangle bounds = Rectangle.Empty; 

                    try 
                    {
                        bounds = dropDown.Bounds;
                        menuItem.HideDropDown();
                        designMenu.RightToLeft = value; 
                        dropDown.RightToLeft = value;
                    } 
                    finally 
                    {
                        BehaviorService behaviorService = (BehaviorService)GetService(typeof(BehaviorService)); 
                        if (behaviorService != null && bounds != Rectangle.Empty)
                        {
                            behaviorService.Invalidate(bounds);
                        } 

                        ToolStripMenuItemDesigner itemDesigner = (ToolStripMenuItemDesigner)host.GetDesigner(menuItem); 
                        if (itemDesigner != null) 
                        {
                            itemDesigner.InitializeDropDown(); 
                        }
                    }
                }
            } 
        }
 
 
        /// <devdoc>
        ///    shadowing the SettingsKey so we can default it to be RootComponent.Name + "." + Control.Name 
        /// </devdoc>
        private string SettingsKey
        {
            get 
            {
                if (string.IsNullOrEmpty((string)ShadowProperties["SettingsKey"])) 
                { 
                    IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings;
                    if (persistableComponent != null && host != null) 
                    {
                        if (persistableComponent.SettingsKey == null)
                        {
                            IComponent rootComponent = host.RootComponent; 
                            if (rootComponent != null && rootComponent != persistableComponent)
                            { 
                                ShadowProperties["SettingsKey"] = String.Format(CultureInfo.CurrentCulture, "{0}.{1}", rootComponent.Site.Name, Component.Site.Name); 
                            }
                            else 
                            {
                                ShadowProperties["SettingsKey"] = Component.Site.Name;
                            }
                        } 
                        persistableComponent.SettingsKey = ShadowProperties["SettingsKey"] as string;
                        return persistableComponent.SettingsKey; 
                    } 
                }
                return ShadowProperties["SettingsKey"] as string; 
            }
            set
            {
                ShadowProperties["SettingsKey"] = value; 
                IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings;
                if (persistableComponent != null) 
                { 
                    persistableComponent.SettingsKey = value;
                } 

            }

        } 

        // We have to add the glyphs ourselves. 
        private void AddSelectionGlyphs(SelectionManager selMgr, ISelectionService selectionService) 
        {
            //If one or many of our items are selected then Add Selection Glyphs ourselces since this is a ComponentDesigner which 
            // wont get called on the "GetGlyphs"
            ICollection selComponents = selectionService.GetSelectedComponents();
            GlyphCollection glyphs = new GlyphCollection();
 
            foreach (object selComp in selComponents)
            { 
                ToolStripItem item = selComp as ToolStripItem; 
                if (item != null)
                { 
                    ToolStripItemDesigner itemDesigner = (ToolStripItemDesigner)host.GetDesigner(item);
                    if (itemDesigner != null)
                    {
                        itemDesigner.GetGlyphs(ref glyphs, new ResizeBehavior(item.Site)); 
                    }
                } 
            } 
            // Get the Glyphs union Rectangle.
            // 
            if (glyphs.Count > 0)
            {
                // Add Glyphs and then invalidate the unionRect
                selMgr.SelectionGlyphAdorner.Glyphs.AddRange(glyphs); 
            }
        } 
 
        // internal method called by outside designers to add glyphs for the ContextMenuStrip
        internal void AddSelectionGlyphs() 
        {
            SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager));
            if (selMgr != null)
            { 
                AddSelectionGlyphs(selMgr, selSvc);
            } 
        } 

        /// <devdoc> 
        ///     Disposes of this designer.
        /// </devdoc>
        protected override void Dispose(bool disposing)
        { 
            if (disposing)
            { 
                // Unhook our services 
                if (selSvc != null)
                { 
                    selSvc.SelectionChanged -= new EventHandler(this.OnSelectionChanged);
                    selSvc.SelectionChanging -= new EventHandler(this.OnSelectionChanging);
                }
 
                DisposeMenu();
 
                if (designMenu != null) 
                {
                    designMenu.Dispose(); 
                    designMenu = null;
                }
                if (dummyToolStripGlyph != null)
                { 
                    dummyToolStripGlyph = null;
                } 
                if (undoEngine != null) 
                {
                    undoEngine.Undone -= new EventHandler(this.OnUndone); 
                }
            }
            base.Dispose(disposing);
        } 

        /// <devdoc> 
        ///     Disposes of this dummy menuItem and its designer.. 
        /// </devdoc>
        private void DisposeMenu() 
        {
            HideMenu();
            Control form = host.RootComponent as Control;
            if (form != null) 
            {
                if (designMenu != null) 
                { 
                    form.Controls.Remove(designMenu);
                } 
                if (menuItem != null)
                {
                    if (nestedContainer != null)
                    { 
                        //nestedContainer.Remove(menuItem);
                        nestedContainer.Dispose(); 
                        nestedContainer = null; 
                    }
                    menuItem.Dispose(); 
                    menuItem = null;
                }
            }
        } 

        // private helper function to Hide the ContextMenu structure. 
        private void HideMenu() 
        {
            if (menuItem == null) 
            {
                return;
            }
            //Add MenuBack 
            if (parentMenu != null && parentFormDesigner != null)
            { 
                parentFormDesigner.Menu = parentMenu; 
            }
 
            selected = false;
            Control form = host.RootComponent as Control;
            if (form != null)
            { 
                menuItem.DropDown.AutoClose = true;
                menuItem.HideDropDown(); 
                menuItem.Visible = false; 
                //Hide the MenuItem DropDown.
                designMenu.Visible = false; 

                //Invalidate the Bounds..
                ToolStripAdornerWindowService toolStripAdornerWindowService = (ToolStripAdornerWindowService)GetService(typeof(ToolStripAdornerWindowService));
                if (toolStripAdornerWindowService != null) 
                {
                    //toolStripAdornerWindowService.Invalidate(boundsToInvalidate); 
                    toolStripAdornerWindowService.Invalidate(); 
                }
 
                //Query for the Behavior Service and Remove Glyph....
                BehaviorService behaviorService = (BehaviorService)GetService(typeof(BehaviorService));
                if (behaviorService != null)
                { 
                    if (dummyToolStripGlyph != null)
                    { 
                        SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
                        if (selMgr != null)
                        { 
                            if (selMgr.BodyGlyphAdorner.Glyphs.Contains(dummyToolStripGlyph))
                            {
                                selMgr.BodyGlyphAdorner.Glyphs.Remove(dummyToolStripGlyph);
                            } 
                            selMgr.Refresh();
                        } 
                    } 
                    dummyToolStripGlyph = null;
                } 

                //Unhook all the events for DesignMenuItem
                if (menuItem != null)
                { 
                    ToolStripMenuItemDesigner itemDesigner = host.GetDesigner(menuItem) as ToolStripMenuItemDesigner;
                    if (itemDesigner != null) 
                    { 
                        itemDesigner.UnHookEvents();
                        itemDesigner.RemoveTypeHereNode(menuItem); 
                    }
                }

            } 
        }
 
        /// <summary> 
        ///  Initialize the item.
        /// </summary> 
        // EditorServiceContext is newed up to add Edit Items verb.
        [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults")]
        public override void Initialize(IComponent component)
        { 
            base.Initialize(component);
 
            host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            //Add the EditService so that the ToolStrip can do its own Tab and Keyboard Handling 
            ToolStripKeyboardHandlingService keyboardHandlingService = (ToolStripKeyboardHandlingService)GetService(typeof(ToolStripKeyboardHandlingService));
            if (keyboardHandlingService == null)
            {
                keyboardHandlingService = new ToolStripKeyboardHandlingService(component.Site); 
            }
 
            //Add the InsituEditService so that the ToolStrip can do its own Insitu Editing 
            ISupportInSituService inSituService = (ISupportInSituService)GetService(typeof(ISupportInSituService));
            if (inSituService == null) 
            {
                inSituService = new ToolStripInSituService(Component.Site);
            }
 
            dropDown = (ToolStripDropDown)Component;
            dropDown.Visible = false; 
 
            //shadow properties as we would change these for DropDowns at DesignTime.
            AutoClose = dropDown.AutoClose; 
            AllowDrop = dropDown.AllowDrop;


            selSvc = (ISelectionService)GetService(typeof(ISelectionService)); 
            if (selSvc != null)
            { 
                // first select the rootComponent and then hook on the events... 
                // but not if we are loading - VSWhidbey #484576
                if (host != null && !host.Loading) { 
                    selSvc.SetSelectedComponents(new IComponent[] { host.RootComponent }, SelectionTypes.Replace);
                }

                selSvc.SelectionChanging += new EventHandler(this.OnSelectionChanging); 
                selSvc.SelectionChanged += new EventHandler(this.OnSelectionChanged);
            } 
 
            designMenu = new MenuStrip();
 
            designMenu.Visible = false;
            designMenu.AutoSize = false;
            designMenu.Dock = DockStyle.Top;
 
            //Add MenuItem
            Control form = host.RootComponent as Control; 
            if (form != null) 
            {
                menuItem = new ToolStripMenuItem(); 
                menuItem.BackColor = SystemColors.Window;
                menuItem.Name = Component.Site.Name;
                menuItem.Text = (dropDown != null) ? dropDown.GetType().Name : menuItem.Name;
                designMenu.Items.Add(menuItem); 

                form.Controls.Add(designMenu); 
                designMenu.SendToBack(); 

                nestedContainer = GetService(typeof(INestedContainer)) as INestedContainer; 
                if (nestedContainer != null)
                {
                    nestedContainer.Add(menuItem, "ContextMenuStrip");
                } 

            } 
 
            // init the verb.
            // 
            new EditorServiceContext(this, TypeDescriptor.GetProperties(Component)["Items"], SR.GetString(SR.ToolStripItemCollectionEditorVerb));

            // use the UndoEngine.Undone to Show the DropDown Again..
            if (undoEngine == null) 
            {
                undoEngine = GetService(typeof(UndoEngine)) as UndoEngine; 
                if (undoEngine != null) 
                {
                    undoEngine.Undone += new EventHandler(this.OnUndone); 
                }
            }
        }
 
        // Helper function to check if the ToolStripItem on the ContextMenu is selected.
        private bool IsContextMenuStripItemSelected(ISelectionService selectionService) 
        { 
            bool showDesignMenu = false;
 
            if (menuItem == null)
            {
                return showDesignMenu;
            } 

            ToolStripDropDown topmost = null; 
            IComponent comp = (IComponent)selectionService.PrimarySelection; 
            if (comp == null && dropDown.Visible)
            { 
                ToolStripKeyboardHandlingService keyboardHandlingService = (ToolStripKeyboardHandlingService)GetService(typeof(ToolStripKeyboardHandlingService));
                if (keyboardHandlingService != null)
                {
                    comp = (IComponent)keyboardHandlingService.SelectedDesignerControl; 
                }
            } 
            // This case covers (a) and (b) above.... 
            if (comp is ToolStripDropDownItem)
            { 
                ToolStripDropDownItem currentItem = comp as ToolStripDropDownItem;
                if (currentItem != null && currentItem == menuItem)
                {
                    topmost = menuItem.DropDown; 
                }
                else 
                { 
                    ToolStripMenuItemDesigner itemDesigner = (ToolStripMenuItemDesigner)host.GetDesigner(comp);
                    if (itemDesigner != null) 
                    {
                        topmost = itemDesigner.GetFirstDropDown((ToolStripDropDownItem)comp);
                    }
                } 

            } 
            else if (comp is ToolStripItem) //case (c) 
            {
 
                ToolStripDropDown parent = ((ToolStripItem)comp).GetCurrentParent() as ToolStripDropDown;
                if (parent == null)
                {
                    // Try if the item has not laid out... 
                    parent = ((ToolStripItem)comp).Owner as ToolStripDropDown;
                } 
                if (parent != null && parent.Visible) 
                {
                    ToolStripItem ownerItem = parent.OwnerItem; 
                    if (ownerItem != null && ownerItem == menuItem)
                    {
                        topmost = menuItem.DropDown;
                    } 
                    else
                    { 
                        ToolStripMenuItemDesigner itemDesigner = (ToolStripMenuItemDesigner)host.GetDesigner(ownerItem); 
                        if (itemDesigner != null)
                        { 
                            topmost = itemDesigner.GetFirstDropDown((ToolStripDropDownItem)ownerItem);
                        }
                    }
                } 
            }
            if (topmost != null) 
            { 
                ToolStripItem topMostItem = topmost.OwnerItem;
                if (topMostItem == menuItem) 
                {
                    showDesignMenu = true;
                }
            } 
            return showDesignMenu;
        } 
 
        /// <devdoc>
        ///     Listens SelectionChanging to Show the MenuDesigner. 
        /// </devdoc>
        private void OnSelectionChanging(object sender, EventArgs e)
        {
            ISelectionService selectionService = (ISelectionService)sender; 

            // If we are no longer selected ... Hide the DropDown 
            bool showDesignMenu = IsContextMenuStripItemSelected(selectionService) || Component.Equals(selectionService.PrimarySelection); 

            if (selected && !showDesignMenu) 
            {
                HideMenu();
            }
 
        }
 
        /// <devdoc> 
        ///     Listens SelectionChanged to Show the MenuDesigner.
        /// </devdoc> 
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (Component == null || menuItem == null)
            { 
                return;
            } 
            ISelectionService selectionService = (ISelectionService)sender; 

            // Select the container if TopLevel Dummy MenuItem is selected. 
            if (selectionService.GetComponentSelected(menuItem))
            {
                selectionService.SetSelectedComponents(new IComponent[] { Component }, SelectionTypes.Replace);
            } 

            //return if DropDown is already is selected. 
            if (Component.Equals(selectionService.PrimarySelection) && selected) 
            {
                return; 
            }

            bool showDesignMenu = IsContextMenuStripItemSelected(selectionService) || Component.Equals(selectionService.PrimarySelection);
 
            if (showDesignMenu)
            { 
                if (!dropDown.Visible) 
                {
                    ShowMenu(); 
                }
                //Selection change would remove our Glyph from the BodyGlyph Collection.
                SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager));
                if (selMgr != null) 
                {
                    if (dummyToolStripGlyph != null) 
                    { 
                        selMgr.BodyGlyphAdorner.Glyphs.Insert(0, dummyToolStripGlyph);
                    } 

                    // Add our SelectionGlyphs and Invalidate.
                    AddSelectionGlyphs(selMgr, selectionService);
                } 
            }
        } 
 

 
        /// <include file='doc\ToolStripDropDownDesigner.uex' path='docs/doc[@for="ToolStripDropDownDesigner.PreFilterProperties"]/*' />
        /// <devdoc>
        ///      Allows a designer to filter the set of properties
        ///      the component it is designing will expose through the 
        ///      TypeDescriptor object.  This method is called
        ///      immediately before its corresponding "Post" method. 
        ///      If you are overriding this method you should call 
        ///      the base implementation before you perform your own
        ///      filtering. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties)
        {
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 
            string[] shadowProps = new string[] { 
                     "AutoClose",
                     "SettingsKey", 
                     "RightToLeft",
                     "AllowDrop"
                };
            Attribute[] empty = new Attribute[0]; 
            for (int i = 0; i < shadowProps.Length; i++)
            { 
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null)
                { 
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ToolStripDropDownDesigner), prop, empty);
                }
            }
 
        }
 
        // Reset Settings. 
        public void ResetSettingsKey()
        { 
            IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings;
            if (persistableComponent != null)
            {
                SettingsKey = null; 
            }
        } 
 
        // <devdoc>
        // Resets the ToolStripDropDown AutoClose to be the default padding 
        // <devdoc/>
        private void ResetAutoClose()
        {
            ShadowProperties["AutoClose"] = true; 
        }
        // <devdoc> 
        // Restores the ToolStripDropDown AutoClose to be the value set in the property grid. 
        // <devdoc/>
        private void RestoreAutoClose() 
        {
            dropDown.AutoClose = (bool)ShadowProperties["AutoClose"];
        }
 
        // <devdoc>
        // Resets the ToolStripDropDown AllowDrop to be the default padding 
        // <devdoc/> 
        private void ResetAllowDrop()
        { 
            ShadowProperties["AllowDrop"] = false;
        }
        // <devdoc>
        // Restores the ToolStripDropDown AllowDrop to be the value set in the property grid. 
        // <devdoc/>
        private void RestoreAllowDrop() 
        { 
            dropDown.AutoClose = (bool)ShadowProperties["AllowDrop"];
        } 

        // <devdoc>
        // Resets the ToolStripDropDown RightToLeft to be the default RightToLeft
        // <devdoc/> 
        private void ResetRightToLeft()
        { 
            RightToLeft = RightToLeft.No; 
        }
 

        /// <devdoc>
        ///     Show the MenuDesigner; used by ToolStripmenuItemdesigner to show the menu when the user selects the dropDown item through the PG or Document outline
        /// </devdoc> 
        public void ShowMenu()
        { 
 
            if (menuItem == null)
            { 
                return;
            }

            Control parent = designMenu.Parent as Control; 
            Form parentForm = parent as Form;
 
            if (parentForm != null) 
            {
                parentFormDesigner = host.GetDesigner(parentForm) as FormDocumentDesigner; 
                if (parentFormDesigner != null && parentFormDesigner.Menu != null)
                {
                    parentMenu = parentFormDesigner.Menu;
                    parentFormDesigner.Menu = null; 

                } 
            } 

            selected = true; 
            designMenu.Visible = true;
            designMenu.BringToFront();
            menuItem.Visible = true;
 

            // Check if this is a design-time DropDown 
            if (currentParent != null && currentParent != menuItem) 
            {
                ToolStripMenuItemDesigner ownerItemDesigner = host.GetDesigner(currentParent) as ToolStripMenuItemDesigner; 
                if (ownerItemDesigner != null)
                {
                    ownerItemDesigner.RemoveTypeHereNode(currentParent);
                } 
            }
 
            //Everytime you hide/show .. set the DropDown of the designer MenuItem to the component dropDown beign designed. 
            menuItem.DropDown = dropDown;
            menuItem.DropDown.OwnerItem = menuItem; 

            if (dropDown.Items.Count > 0)
            {
                ToolStripItem[] items = new ToolStripItem[dropDown.Items.Count]; 
                dropDown.Items.CopyTo(items, 0);
                foreach (ToolStripItem toolItem in items) 
                { 
                    if (toolItem is DesignerToolStripControlHost)
                    { 
                        dropDown.Items.Remove(toolItem);
                    }
                }
            } 

            ToolStripMenuItemDesigner itemDesigner = (ToolStripMenuItemDesigner)host.GetDesigner(menuItem); 
            BehaviorService behaviorService = (BehaviorService)GetService(typeof(BehaviorService)); 
            if (behaviorService != null)
            { 
                // Show the contextMenu only if the dummy menuStrip is contained in the Form.
                // Refer to VsWhidbey 484317 for more details.
                if (itemDesigner != null && parent != null)
                { 
                    Rectangle parentBounds = behaviorService.ControlRectInAdornerWindow(parent);
                    Rectangle menuBounds = behaviorService.ControlRectInAdornerWindow(designMenu); 
                    if (ToolStripDesigner.IsGlyphTotallyVisible(menuBounds, parentBounds)) 
                    {
                        itemDesigner.InitializeDropDown(); 
                    }
                }

 
                if (dummyToolStripGlyph == null)
                { 
                    Point loc = behaviorService.ControlToAdornerWindow(designMenu); 
                    Rectangle r = designMenu.Bounds;
                    r.Offset(loc); 
                    dummyToolStripGlyph = new ControlBodyGlyph(r, Cursor.Current, menuItem, new ContextMenuStripBehavior(menuItem));
                    SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager));
                    if (selMgr != null)
                    { 
                        selMgr.BodyGlyphAdorner.Glyphs.Insert(0, dummyToolStripGlyph);
                    } 
 
                }
 
                ToolStripKeyboardHandlingService keyboardHandlingService = (ToolStripKeyboardHandlingService)GetService(typeof(ToolStripKeyboardHandlingService));
                if (keyboardHandlingService != null)
                {
                    int count = dropDown.Items.Count - 1; 
                    if (count >= 0)
                    { 
                        keyboardHandlingService.SelectedDesignerControl = dropDown.Items[count]; 
                    }
                } 
            }
        }

        // Should the designer serialize the settings? 
        private bool ShouldSerializeSettingsKey()
        { 
            IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings; 
            return (persistableComponent != null
                    && persistableComponent.SaveSettings 
                    && SettingsKey != null);
        }

        // <devdoc> 
        // Since we're shadowing ToolStripDropDown AutoClose, we get called here to determine whether or not to serialize
        // <devdoc/> 
        private bool ShouldSerializeAutoClose() 
        {
            bool autoClose = (bool)ShadowProperties["AutoClose"]; 
            return (!autoClose);
        }

        // <devdoc> 
        // Since we're shadowing ToolStripDropDown AllowDrop, we get called here to determine whether or not to serialize
        // <devdoc/> 
        private bool ShouldSerializeAllowDrop() 
        {
            return AllowDrop; 
        }

        // <devdoc>
        // Since we're shadowing ToolStripDropDown RightToLeft, we get called here to determine whether or not to serialize 
        // <devdoc/>
        private bool ShouldSerializeRightToLeft() 
        { 
            return RightToLeft != RightToLeft.No;
        } 

        /// <devdoc>
        ///     ResumeLayout after Undone.
        /// </devdoc> 
        private void OnUndone(object source, EventArgs e)
        { 
 
            if (selSvc != null && Component.Equals(selSvc.PrimarySelection))
            { 
                HideMenu();
                ShowMenu();
            }
 
        }
 
        /// <summary> 
        ///  This is an internal class which provides the Behavior for our MenuStrip Body Glyph.
        ///  This will just eat the MouseUps... 
        /// </summary>
        internal class ContextMenuStripBehavior : System.Windows.Forms.Design.Behavior.Behavior
        {
            ToolStripMenuItem item; 

            internal ContextMenuStripBehavior(ToolStripMenuItem menuItem) 
            { 
                this.item = menuItem;
            } 

            public override bool OnMouseUp(Glyph g, MouseButtons button)
            {
                if (button == MouseButtons.Left) 
                {
                    return true; 
                } 
                return false;
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripDropDownItemDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Windows.Forms.Design.ToolStripDropDownDesigner..ctor()")]
 
namespace System.Windows.Forms.Design
{
    using System.Design;
    using Accessibility; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.Windows.Forms;
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing;
    using System.Configuration; 
    using System.Globalization; 

    /// <summary> 
    /// Designer for ToolStripDropDowns...just provides the Edit... verb.
    /// </summary>
    internal class ToolStripDropDownDesigner : ComponentDesigner
    { 

        private ISelectionService selSvc; 
 
        private MenuStrip designMenu;
        private ToolStripMenuItem menuItem; 
        private IDesignerHost host;

        private ToolStripDropDown dropDown;
 
        private bool selected;
        private ControlBodyGlyph dummyToolStripGlyph; 
 
        private uint _editingCollection = 0;      // non-zero if the collection editor is up for this winbar or a child of it.
 
        MainMenu parentMenu = null;
        FormDocumentDesigner parentFormDesigner = null;

 
        internal ToolStripMenuItem currentParent = null;
        private INestedContainer nestedContainer = null; //NestedContainer for our DesignTime MenuItem. 
 
        private UndoEngine undoEngine = null;
 
        /// <devdoc>
        ///      ShadowProperty.
        /// </devdoc>
        private bool AutoClose 
        {
            get 
            { 
                return (bool)ShadowProperties["AutoClose"];
            } 
            set
            {
                ShadowProperties["AutoClose"] = value;
            } 
        }
 
 
        /// <devdoc>
        ///      ShadowProperty. 
        /// </devdoc>
        private bool AllowDrop
        {
            get 
            {
                return (bool)ShadowProperties["AllowDrop"]; 
            } 
            set
            { 
                ShadowProperties["AllowDrop"] = value;
            }
        }
 
        /// <include file='doc\ToolStripItemDesigner.uex' path='docs/doc[@for="ToolStripItemDesigner.ActionLists"]/*' />
        /// <summary> 
        /// Adds designer actions to the ActionLists collection. 
        /// </summary>
        public override DesignerActionListCollection ActionLists 
        {
            get
            {
 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                ContextMenuStripActionList cmActionList = new ContextMenuStripActionList(this); 
                if (cmActionList != null)
                { 
                    actionLists.Add(cmActionList);
                }

                // finally add the verbs for this component there... 
                DesignerVerbCollection cmVerbs = this.Verbs;
                if (cmVerbs != null && cmVerbs.Count != 0) 
                { 
                    DesignerVerb[] cmverbsArray = new DesignerVerb[cmVerbs.Count];
                    cmVerbs.CopyTo(cmverbsArray, 0); 
                    actionLists.Add(new DesignerActionVerbList(cmverbsArray));
                }
                return actionLists;
            } 
        }
 
        /// <summary> 
        ///  The ToolStripItems are the associated components.
        ///  We want those to come with in any cut, copy opreations. 
        /// </summary>
        public override System.Collections.ICollection AssociatedComponents
        {
            get 
            {
                return ((ToolStrip)Component).Items; 
            } 
        }
 
        // Dummy menuItem that is used for the contextMenuStrip design
        public ToolStripMenuItem DesignerMenuItem
        {
            get 
            {
                return menuItem; 
            } 
        }
 
        /// <summary>
        ///  Set by the ToolStripItemCollectionEditor when it's launched for this The Items property doesnt open another instance
        ///  of collectioneditor.  We count this so that we can deal with nestings.
        /// </summary> 
        internal bool EditingCollection
        { 
            get 
            {
                return _editingCollection != 0; 
            }
            set
            {
                if (value) 
                {
                    _editingCollection++; 
                } 
                else
                { 
                    _editingCollection--;
                }

            } 
        }
 
        // ContextMenuStrip if Inherited ACT as Readonly. 
        protected override InheritanceAttribute InheritanceAttribute
        { 
            get
            {
                if ((base.InheritanceAttribute == InheritanceAttribute.Inherited))
                { 
                    return InheritanceAttribute.InheritedReadOnly;
                } 
                return base.InheritanceAttribute; 
            }
        } 


        /// <summary>
        ///  Prefilter this property so that we can set the right To Left on the Design Menu... 
        /// </summary>
        private RightToLeft RightToLeft 
        { 
            get
            { 
                return dropDown.RightToLeft;
            }
            set
            { 
                if (menuItem != null && designMenu != null && value != RightToLeft)
                { 
                    Rectangle bounds = Rectangle.Empty; 

                    try 
                    {
                        bounds = dropDown.Bounds;
                        menuItem.HideDropDown();
                        designMenu.RightToLeft = value; 
                        dropDown.RightToLeft = value;
                    } 
                    finally 
                    {
                        BehaviorService behaviorService = (BehaviorService)GetService(typeof(BehaviorService)); 
                        if (behaviorService != null && bounds != Rectangle.Empty)
                        {
                            behaviorService.Invalidate(bounds);
                        } 

                        ToolStripMenuItemDesigner itemDesigner = (ToolStripMenuItemDesigner)host.GetDesigner(menuItem); 
                        if (itemDesigner != null) 
                        {
                            itemDesigner.InitializeDropDown(); 
                        }
                    }
                }
            } 
        }
 
 
        /// <devdoc>
        ///    shadowing the SettingsKey so we can default it to be RootComponent.Name + "." + Control.Name 
        /// </devdoc>
        private string SettingsKey
        {
            get 
            {
                if (string.IsNullOrEmpty((string)ShadowProperties["SettingsKey"])) 
                { 
                    IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings;
                    if (persistableComponent != null && host != null) 
                    {
                        if (persistableComponent.SettingsKey == null)
                        {
                            IComponent rootComponent = host.RootComponent; 
                            if (rootComponent != null && rootComponent != persistableComponent)
                            { 
                                ShadowProperties["SettingsKey"] = String.Format(CultureInfo.CurrentCulture, "{0}.{1}", rootComponent.Site.Name, Component.Site.Name); 
                            }
                            else 
                            {
                                ShadowProperties["SettingsKey"] = Component.Site.Name;
                            }
                        } 
                        persistableComponent.SettingsKey = ShadowProperties["SettingsKey"] as string;
                        return persistableComponent.SettingsKey; 
                    } 
                }
                return ShadowProperties["SettingsKey"] as string; 
            }
            set
            {
                ShadowProperties["SettingsKey"] = value; 
                IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings;
                if (persistableComponent != null) 
                { 
                    persistableComponent.SettingsKey = value;
                } 

            }

        } 

        // We have to add the glyphs ourselves. 
        private void AddSelectionGlyphs(SelectionManager selMgr, ISelectionService selectionService) 
        {
            //If one or many of our items are selected then Add Selection Glyphs ourselces since this is a ComponentDesigner which 
            // wont get called on the "GetGlyphs"
            ICollection selComponents = selectionService.GetSelectedComponents();
            GlyphCollection glyphs = new GlyphCollection();
 
            foreach (object selComp in selComponents)
            { 
                ToolStripItem item = selComp as ToolStripItem; 
                if (item != null)
                { 
                    ToolStripItemDesigner itemDesigner = (ToolStripItemDesigner)host.GetDesigner(item);
                    if (itemDesigner != null)
                    {
                        itemDesigner.GetGlyphs(ref glyphs, new ResizeBehavior(item.Site)); 
                    }
                } 
            } 
            // Get the Glyphs union Rectangle.
            // 
            if (glyphs.Count > 0)
            {
                // Add Glyphs and then invalidate the unionRect
                selMgr.SelectionGlyphAdorner.Glyphs.AddRange(glyphs); 
            }
        } 
 
        // internal method called by outside designers to add glyphs for the ContextMenuStrip
        internal void AddSelectionGlyphs() 
        {
            SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager));
            if (selMgr != null)
            { 
                AddSelectionGlyphs(selMgr, selSvc);
            } 
        } 

        /// <devdoc> 
        ///     Disposes of this designer.
        /// </devdoc>
        protected override void Dispose(bool disposing)
        { 
            if (disposing)
            { 
                // Unhook our services 
                if (selSvc != null)
                { 
                    selSvc.SelectionChanged -= new EventHandler(this.OnSelectionChanged);
                    selSvc.SelectionChanging -= new EventHandler(this.OnSelectionChanging);
                }
 
                DisposeMenu();
 
                if (designMenu != null) 
                {
                    designMenu.Dispose(); 
                    designMenu = null;
                }
                if (dummyToolStripGlyph != null)
                { 
                    dummyToolStripGlyph = null;
                } 
                if (undoEngine != null) 
                {
                    undoEngine.Undone -= new EventHandler(this.OnUndone); 
                }
            }
            base.Dispose(disposing);
        } 

        /// <devdoc> 
        ///     Disposes of this dummy menuItem and its designer.. 
        /// </devdoc>
        private void DisposeMenu() 
        {
            HideMenu();
            Control form = host.RootComponent as Control;
            if (form != null) 
            {
                if (designMenu != null) 
                { 
                    form.Controls.Remove(designMenu);
                } 
                if (menuItem != null)
                {
                    if (nestedContainer != null)
                    { 
                        //nestedContainer.Remove(menuItem);
                        nestedContainer.Dispose(); 
                        nestedContainer = null; 
                    }
                    menuItem.Dispose(); 
                    menuItem = null;
                }
            }
        } 

        // private helper function to Hide the ContextMenu structure. 
        private void HideMenu() 
        {
            if (menuItem == null) 
            {
                return;
            }
            //Add MenuBack 
            if (parentMenu != null && parentFormDesigner != null)
            { 
                parentFormDesigner.Menu = parentMenu; 
            }
 
            selected = false;
            Control form = host.RootComponent as Control;
            if (form != null)
            { 
                menuItem.DropDown.AutoClose = true;
                menuItem.HideDropDown(); 
                menuItem.Visible = false; 
                //Hide the MenuItem DropDown.
                designMenu.Visible = false; 

                //Invalidate the Bounds..
                ToolStripAdornerWindowService toolStripAdornerWindowService = (ToolStripAdornerWindowService)GetService(typeof(ToolStripAdornerWindowService));
                if (toolStripAdornerWindowService != null) 
                {
                    //toolStripAdornerWindowService.Invalidate(boundsToInvalidate); 
                    toolStripAdornerWindowService.Invalidate(); 
                }
 
                //Query for the Behavior Service and Remove Glyph....
                BehaviorService behaviorService = (BehaviorService)GetService(typeof(BehaviorService));
                if (behaviorService != null)
                { 
                    if (dummyToolStripGlyph != null)
                    { 
                        SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
                        if (selMgr != null)
                        { 
                            if (selMgr.BodyGlyphAdorner.Glyphs.Contains(dummyToolStripGlyph))
                            {
                                selMgr.BodyGlyphAdorner.Glyphs.Remove(dummyToolStripGlyph);
                            } 
                            selMgr.Refresh();
                        } 
                    } 
                    dummyToolStripGlyph = null;
                } 

                //Unhook all the events for DesignMenuItem
                if (menuItem != null)
                { 
                    ToolStripMenuItemDesigner itemDesigner = host.GetDesigner(menuItem) as ToolStripMenuItemDesigner;
                    if (itemDesigner != null) 
                    { 
                        itemDesigner.UnHookEvents();
                        itemDesigner.RemoveTypeHereNode(menuItem); 
                    }
                }

            } 
        }
 
        /// <summary> 
        ///  Initialize the item.
        /// </summary> 
        // EditorServiceContext is newed up to add Edit Items verb.
        [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults")]
        public override void Initialize(IComponent component)
        { 
            base.Initialize(component);
 
            host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            //Add the EditService so that the ToolStrip can do its own Tab and Keyboard Handling 
            ToolStripKeyboardHandlingService keyboardHandlingService = (ToolStripKeyboardHandlingService)GetService(typeof(ToolStripKeyboardHandlingService));
            if (keyboardHandlingService == null)
            {
                keyboardHandlingService = new ToolStripKeyboardHandlingService(component.Site); 
            }
 
            //Add the InsituEditService so that the ToolStrip can do its own Insitu Editing 
            ISupportInSituService inSituService = (ISupportInSituService)GetService(typeof(ISupportInSituService));
            if (inSituService == null) 
            {
                inSituService = new ToolStripInSituService(Component.Site);
            }
 
            dropDown = (ToolStripDropDown)Component;
            dropDown.Visible = false; 
 
            //shadow properties as we would change these for DropDowns at DesignTime.
            AutoClose = dropDown.AutoClose; 
            AllowDrop = dropDown.AllowDrop;


            selSvc = (ISelectionService)GetService(typeof(ISelectionService)); 
            if (selSvc != null)
            { 
                // first select the rootComponent and then hook on the events... 
                // but not if we are loading - VSWhidbey #484576
                if (host != null && !host.Loading) { 
                    selSvc.SetSelectedComponents(new IComponent[] { host.RootComponent }, SelectionTypes.Replace);
                }

                selSvc.SelectionChanging += new EventHandler(this.OnSelectionChanging); 
                selSvc.SelectionChanged += new EventHandler(this.OnSelectionChanged);
            } 
 
            designMenu = new MenuStrip();
 
            designMenu.Visible = false;
            designMenu.AutoSize = false;
            designMenu.Dock = DockStyle.Top;
 
            //Add MenuItem
            Control form = host.RootComponent as Control; 
            if (form != null) 
            {
                menuItem = new ToolStripMenuItem(); 
                menuItem.BackColor = SystemColors.Window;
                menuItem.Name = Component.Site.Name;
                menuItem.Text = (dropDown != null) ? dropDown.GetType().Name : menuItem.Name;
                designMenu.Items.Add(menuItem); 

                form.Controls.Add(designMenu); 
                designMenu.SendToBack(); 

                nestedContainer = GetService(typeof(INestedContainer)) as INestedContainer; 
                if (nestedContainer != null)
                {
                    nestedContainer.Add(menuItem, "ContextMenuStrip");
                } 

            } 
 
            // init the verb.
            // 
            new EditorServiceContext(this, TypeDescriptor.GetProperties(Component)["Items"], SR.GetString(SR.ToolStripItemCollectionEditorVerb));

            // use the UndoEngine.Undone to Show the DropDown Again..
            if (undoEngine == null) 
            {
                undoEngine = GetService(typeof(UndoEngine)) as UndoEngine; 
                if (undoEngine != null) 
                {
                    undoEngine.Undone += new EventHandler(this.OnUndone); 
                }
            }
        }
 
        // Helper function to check if the ToolStripItem on the ContextMenu is selected.
        private bool IsContextMenuStripItemSelected(ISelectionService selectionService) 
        { 
            bool showDesignMenu = false;
 
            if (menuItem == null)
            {
                return showDesignMenu;
            } 

            ToolStripDropDown topmost = null; 
            IComponent comp = (IComponent)selectionService.PrimarySelection; 
            if (comp == null && dropDown.Visible)
            { 
                ToolStripKeyboardHandlingService keyboardHandlingService = (ToolStripKeyboardHandlingService)GetService(typeof(ToolStripKeyboardHandlingService));
                if (keyboardHandlingService != null)
                {
                    comp = (IComponent)keyboardHandlingService.SelectedDesignerControl; 
                }
            } 
            // This case covers (a) and (b) above.... 
            if (comp is ToolStripDropDownItem)
            { 
                ToolStripDropDownItem currentItem = comp as ToolStripDropDownItem;
                if (currentItem != null && currentItem == menuItem)
                {
                    topmost = menuItem.DropDown; 
                }
                else 
                { 
                    ToolStripMenuItemDesigner itemDesigner = (ToolStripMenuItemDesigner)host.GetDesigner(comp);
                    if (itemDesigner != null) 
                    {
                        topmost = itemDesigner.GetFirstDropDown((ToolStripDropDownItem)comp);
                    }
                } 

            } 
            else if (comp is ToolStripItem) //case (c) 
            {
 
                ToolStripDropDown parent = ((ToolStripItem)comp).GetCurrentParent() as ToolStripDropDown;
                if (parent == null)
                {
                    // Try if the item has not laid out... 
                    parent = ((ToolStripItem)comp).Owner as ToolStripDropDown;
                } 
                if (parent != null && parent.Visible) 
                {
                    ToolStripItem ownerItem = parent.OwnerItem; 
                    if (ownerItem != null && ownerItem == menuItem)
                    {
                        topmost = menuItem.DropDown;
                    } 
                    else
                    { 
                        ToolStripMenuItemDesigner itemDesigner = (ToolStripMenuItemDesigner)host.GetDesigner(ownerItem); 
                        if (itemDesigner != null)
                        { 
                            topmost = itemDesigner.GetFirstDropDown((ToolStripDropDownItem)ownerItem);
                        }
                    }
                } 
            }
            if (topmost != null) 
            { 
                ToolStripItem topMostItem = topmost.OwnerItem;
                if (topMostItem == menuItem) 
                {
                    showDesignMenu = true;
                }
            } 
            return showDesignMenu;
        } 
 
        /// <devdoc>
        ///     Listens SelectionChanging to Show the MenuDesigner. 
        /// </devdoc>
        private void OnSelectionChanging(object sender, EventArgs e)
        {
            ISelectionService selectionService = (ISelectionService)sender; 

            // If we are no longer selected ... Hide the DropDown 
            bool showDesignMenu = IsContextMenuStripItemSelected(selectionService) || Component.Equals(selectionService.PrimarySelection); 

            if (selected && !showDesignMenu) 
            {
                HideMenu();
            }
 
        }
 
        /// <devdoc> 
        ///     Listens SelectionChanged to Show the MenuDesigner.
        /// </devdoc> 
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (Component == null || menuItem == null)
            { 
                return;
            } 
            ISelectionService selectionService = (ISelectionService)sender; 

            // Select the container if TopLevel Dummy MenuItem is selected. 
            if (selectionService.GetComponentSelected(menuItem))
            {
                selectionService.SetSelectedComponents(new IComponent[] { Component }, SelectionTypes.Replace);
            } 

            //return if DropDown is already is selected. 
            if (Component.Equals(selectionService.PrimarySelection) && selected) 
            {
                return; 
            }

            bool showDesignMenu = IsContextMenuStripItemSelected(selectionService) || Component.Equals(selectionService.PrimarySelection);
 
            if (showDesignMenu)
            { 
                if (!dropDown.Visible) 
                {
                    ShowMenu(); 
                }
                //Selection change would remove our Glyph from the BodyGlyph Collection.
                SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager));
                if (selMgr != null) 
                {
                    if (dummyToolStripGlyph != null) 
                    { 
                        selMgr.BodyGlyphAdorner.Glyphs.Insert(0, dummyToolStripGlyph);
                    } 

                    // Add our SelectionGlyphs and Invalidate.
                    AddSelectionGlyphs(selMgr, selectionService);
                } 
            }
        } 
 

 
        /// <include file='doc\ToolStripDropDownDesigner.uex' path='docs/doc[@for="ToolStripDropDownDesigner.PreFilterProperties"]/*' />
        /// <devdoc>
        ///      Allows a designer to filter the set of properties
        ///      the component it is designing will expose through the 
        ///      TypeDescriptor object.  This method is called
        ///      immediately before its corresponding "Post" method. 
        ///      If you are overriding this method you should call 
        ///      the base implementation before you perform your own
        ///      filtering. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties)
        {
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 
            string[] shadowProps = new string[] { 
                     "AutoClose",
                     "SettingsKey", 
                     "RightToLeft",
                     "AllowDrop"
                };
            Attribute[] empty = new Attribute[0]; 
            for (int i = 0; i < shadowProps.Length; i++)
            { 
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null)
                { 
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ToolStripDropDownDesigner), prop, empty);
                }
            }
 
        }
 
        // Reset Settings. 
        public void ResetSettingsKey()
        { 
            IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings;
            if (persistableComponent != null)
            {
                SettingsKey = null; 
            }
        } 
 
        // <devdoc>
        // Resets the ToolStripDropDown AutoClose to be the default padding 
        // <devdoc/>
        private void ResetAutoClose()
        {
            ShadowProperties["AutoClose"] = true; 
        }
        // <devdoc> 
        // Restores the ToolStripDropDown AutoClose to be the value set in the property grid. 
        // <devdoc/>
        private void RestoreAutoClose() 
        {
            dropDown.AutoClose = (bool)ShadowProperties["AutoClose"];
        }
 
        // <devdoc>
        // Resets the ToolStripDropDown AllowDrop to be the default padding 
        // <devdoc/> 
        private void ResetAllowDrop()
        { 
            ShadowProperties["AllowDrop"] = false;
        }
        // <devdoc>
        // Restores the ToolStripDropDown AllowDrop to be the value set in the property grid. 
        // <devdoc/>
        private void RestoreAllowDrop() 
        { 
            dropDown.AutoClose = (bool)ShadowProperties["AllowDrop"];
        } 

        // <devdoc>
        // Resets the ToolStripDropDown RightToLeft to be the default RightToLeft
        // <devdoc/> 
        private void ResetRightToLeft()
        { 
            RightToLeft = RightToLeft.No; 
        }
 

        /// <devdoc>
        ///     Show the MenuDesigner; used by ToolStripmenuItemdesigner to show the menu when the user selects the dropDown item through the PG or Document outline
        /// </devdoc> 
        public void ShowMenu()
        { 
 
            if (menuItem == null)
            { 
                return;
            }

            Control parent = designMenu.Parent as Control; 
            Form parentForm = parent as Form;
 
            if (parentForm != null) 
            {
                parentFormDesigner = host.GetDesigner(parentForm) as FormDocumentDesigner; 
                if (parentFormDesigner != null && parentFormDesigner.Menu != null)
                {
                    parentMenu = parentFormDesigner.Menu;
                    parentFormDesigner.Menu = null; 

                } 
            } 

            selected = true; 
            designMenu.Visible = true;
            designMenu.BringToFront();
            menuItem.Visible = true;
 

            // Check if this is a design-time DropDown 
            if (currentParent != null && currentParent != menuItem) 
            {
                ToolStripMenuItemDesigner ownerItemDesigner = host.GetDesigner(currentParent) as ToolStripMenuItemDesigner; 
                if (ownerItemDesigner != null)
                {
                    ownerItemDesigner.RemoveTypeHereNode(currentParent);
                } 
            }
 
            //Everytime you hide/show .. set the DropDown of the designer MenuItem to the component dropDown beign designed. 
            menuItem.DropDown = dropDown;
            menuItem.DropDown.OwnerItem = menuItem; 

            if (dropDown.Items.Count > 0)
            {
                ToolStripItem[] items = new ToolStripItem[dropDown.Items.Count]; 
                dropDown.Items.CopyTo(items, 0);
                foreach (ToolStripItem toolItem in items) 
                { 
                    if (toolItem is DesignerToolStripControlHost)
                    { 
                        dropDown.Items.Remove(toolItem);
                    }
                }
            } 

            ToolStripMenuItemDesigner itemDesigner = (ToolStripMenuItemDesigner)host.GetDesigner(menuItem); 
            BehaviorService behaviorService = (BehaviorService)GetService(typeof(BehaviorService)); 
            if (behaviorService != null)
            { 
                // Show the contextMenu only if the dummy menuStrip is contained in the Form.
                // Refer to VsWhidbey 484317 for more details.
                if (itemDesigner != null && parent != null)
                { 
                    Rectangle parentBounds = behaviorService.ControlRectInAdornerWindow(parent);
                    Rectangle menuBounds = behaviorService.ControlRectInAdornerWindow(designMenu); 
                    if (ToolStripDesigner.IsGlyphTotallyVisible(menuBounds, parentBounds)) 
                    {
                        itemDesigner.InitializeDropDown(); 
                    }
                }

 
                if (dummyToolStripGlyph == null)
                { 
                    Point loc = behaviorService.ControlToAdornerWindow(designMenu); 
                    Rectangle r = designMenu.Bounds;
                    r.Offset(loc); 
                    dummyToolStripGlyph = new ControlBodyGlyph(r, Cursor.Current, menuItem, new ContextMenuStripBehavior(menuItem));
                    SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager));
                    if (selMgr != null)
                    { 
                        selMgr.BodyGlyphAdorner.Glyphs.Insert(0, dummyToolStripGlyph);
                    } 
 
                }
 
                ToolStripKeyboardHandlingService keyboardHandlingService = (ToolStripKeyboardHandlingService)GetService(typeof(ToolStripKeyboardHandlingService));
                if (keyboardHandlingService != null)
                {
                    int count = dropDown.Items.Count - 1; 
                    if (count >= 0)
                    { 
                        keyboardHandlingService.SelectedDesignerControl = dropDown.Items[count]; 
                    }
                } 
            }
        }

        // Should the designer serialize the settings? 
        private bool ShouldSerializeSettingsKey()
        { 
            IPersistComponentSettings persistableComponent = Component as IPersistComponentSettings; 
            return (persistableComponent != null
                    && persistableComponent.SaveSettings 
                    && SettingsKey != null);
        }

        // <devdoc> 
        // Since we're shadowing ToolStripDropDown AutoClose, we get called here to determine whether or not to serialize
        // <devdoc/> 
        private bool ShouldSerializeAutoClose() 
        {
            bool autoClose = (bool)ShadowProperties["AutoClose"]; 
            return (!autoClose);
        }

        // <devdoc> 
        // Since we're shadowing ToolStripDropDown AllowDrop, we get called here to determine whether or not to serialize
        // <devdoc/> 
        private bool ShouldSerializeAllowDrop() 
        {
            return AllowDrop; 
        }

        // <devdoc>
        // Since we're shadowing ToolStripDropDown RightToLeft, we get called here to determine whether or not to serialize 
        // <devdoc/>
        private bool ShouldSerializeRightToLeft() 
        { 
            return RightToLeft != RightToLeft.No;
        } 

        /// <devdoc>
        ///     ResumeLayout after Undone.
        /// </devdoc> 
        private void OnUndone(object source, EventArgs e)
        { 
 
            if (selSvc != null && Component.Equals(selSvc.PrimarySelection))
            { 
                HideMenu();
                ShowMenu();
            }
 
        }
 
        /// <summary> 
        ///  This is an internal class which provides the Behavior for our MenuStrip Body Glyph.
        ///  This will just eat the MouseUps... 
        /// </summary>
        internal class ContextMenuStripBehavior : System.Windows.Forms.Design.Behavior.Behavior
        {
            ToolStripMenuItem item; 

            internal ContextMenuStripBehavior(ToolStripMenuItem menuItem) 
            { 
                this.item = menuItem;
            } 

            public override bool OnMouseUp(Glyph g, MouseButtons button)
            {
                if (button == MouseButtons.Left) 
                {
                    return true; 
                } 
                return false;
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
