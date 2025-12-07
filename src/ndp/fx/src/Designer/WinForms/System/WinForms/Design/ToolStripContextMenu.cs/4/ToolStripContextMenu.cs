//------------------------------------------------------------------------------ 
// <copyright file="ToolStripContextMenu.cs" company="Microsoft">
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
    using System.Diagnostics.CodeAnalysis;
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
    ///     Custom ContextMenu section for ToolStripMenuItems. 
    /// </summary>
    internal class ToolStripItemCustomMenuItemCollection : CustomMenuItemCollection
    {
 
        private ToolStripItem currentItem;
        private IServiceProvider serviceProvider; 
 
        private ToolStripMenuItem imageToolStripMenuItem;
        private ToolStripMenuItem enabledToolStripMenuItem; 

        private ToolStripMenuItem isLinkToolStripMenuItem;
        private ToolStripMenuItem springToolStripMenuItem;
 
        private ToolStripMenuItem checkedToolStripMenuItem;
        private ToolStripMenuItem showShortcutKeysToolStripMenuItem; 
 
        private ToolStripMenuItem alignmentToolStripMenuItem;
        private ToolStripMenuItem displayStyleToolStripMenuItem; 

        private ToolStripSeparator toolStripSeparator1;

        private ToolStripMenuItem convertToolStripMenuItem; 
        private ToolStripMenuItem insertToolStripMenuItem;
 
 
        private ToolStripMenuItem leftToolStripMenuItem;
        private ToolStripMenuItem rightToolStripMenuItem; 

        private ToolStripMenuItem noneStyleToolStripMenuItem;
        private ToolStripMenuItem textStyleToolStripMenuItem;
        private ToolStripMenuItem imageStyleToolStripMenuItem; 
        private ToolStripMenuItem imageTextStyleToolStripMenuItem;
 
        private ToolStripMenuItem editItemsToolStripMenuItem; 
        private CollectionEditVerbManager verbManager;
 
        public ToolStripItemCustomMenuItemCollection(IServiceProvider provider, Component currentItem): base()
        {
            this.serviceProvider = provider;
            this.currentItem = currentItem as ToolStripItem; 
            PopulateList();
        } 
 

        /// <devdoc> 
        ///      Parent ToolStrip.
        /// </devdoc>
        private ToolStrip ParentTool
        { 
            get
            { 
                return currentItem.Owner; 
            }
        } 

        /// <devdoc>
        /// creates a item representing an item, respecting Browsable.
        /// </devdoc> 
        private ToolStripMenuItem CreatePropertyBasedItem(string text, string propertyName, string imageName) {
            ToolStripMenuItem item = new ToolStripMenuItem(text); 
            bool browsable = IsPropertyBrowsable(propertyName); 
            item.Visible = browsable;
            if (browsable) { 
                if (!string.IsNullOrEmpty(imageName)) {
                    item.Image = new Bitmap(typeof(ToolStripMenuItem), imageName);
                    item.ImageTransparentColor = Color.Magenta;
                } 

                IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService; 
                if (uis != null) { 
                  item.DropDown.Renderer =(ToolStripProfessionalRenderer)uis.Styles["VsRenderer"];
                  item.DropDown.Font = (Font)uis.Styles["DialogFont"]; 
                }
            }
            return item;
        } 

        /// <devdoc> 
        /// creates an item that when clicked changes the enum value. 
        /// </devdoc>
        private ToolStripMenuItem CreateEnumValueItem(string propertyName, string name, object value) { 
            ToolStripMenuItem item = new ToolStripMenuItem(name);
            item.Tag = new EnumValueDescription(propertyName, value);
            item.Click += new EventHandler(OnEnumValueChanged);
            return item; 
        }
 
        private ToolStripMenuItem CreateBooleanItem(string text, string propertyName) { 
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            bool browsable = IsPropertyBrowsable(propertyName); 
            item.Visible = browsable;
            item.Tag = propertyName;
            item.CheckOnClick = true;
            item.Click += new EventHandler(OnBooleanValueChanged); 
            return item;
        } 
 
        // Property names are hard-coded intentionally
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
        private void PopulateList()
        {
            ToolStripItem selectedItem = currentItem;
 
            if (!(selectedItem is ToolStripControlHost) && !(selectedItem is ToolStripSeparator))
            { 
                this.imageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); 
                this.imageToolStripMenuItem.Text = SR.GetString(SR.ToolStripItemContextMenuSetImage);
                this.imageToolStripMenuItem.Image = new Bitmap(typeof(ToolStripMenuItem), "image.bmp"); 
                this.imageToolStripMenuItem.ImageTransparentColor = Color.Magenta;
                //Add event Handlers
                this.imageToolStripMenuItem.Click += new EventHandler(OnImageToolStripMenuItemClick);
 
                this.enabledToolStripMenuItem = CreateBooleanItem("E&nabled", "Enabled");
 
                this.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                    this.imageToolStripMenuItem,
                    this.enabledToolStripMenuItem}); 



                if (selectedItem is ToolStripMenuItem) { 
                    this.checkedToolStripMenuItem = CreateBooleanItem("C&hecked", "Checked");
 
                    this.showShortcutKeysToolStripMenuItem = CreateBooleanItem("ShowShortcut&Keys", "ShowShortcutKeys"); 
                    this.AddRange(new System.Windows.Forms.ToolStripItem[] {
                                    this.checkedToolStripMenuItem, 
                                    this.showShortcutKeysToolStripMenuItem});
                }
                else {
 
                    if (selectedItem is ToolStripLabel)
                    { 
                        this.isLinkToolStripMenuItem = CreateBooleanItem("IsLin&k", "IsLink"); 
                        this.Add(this.isLinkToolStripMenuItem);
                    } 

                    if (selectedItem is ToolStripStatusLabel)
                    {
                        this.springToolStripMenuItem = CreateBooleanItem("Sprin&g", "Spring"); 
                        this.Add(this.springToolStripMenuItem);
                    } 
 
                    this.leftToolStripMenuItem          = CreateEnumValueItem("Alignment", "Left", ToolStripItemAlignment.Left);
                    this.rightToolStripMenuItem         = CreateEnumValueItem("Alignment", "Right", ToolStripItemAlignment.Right); 

                    this.noneStyleToolStripMenuItem     = CreateEnumValueItem("DisplayStyle", "None", ToolStripItemDisplayStyle.None);
                    this.textStyleToolStripMenuItem     = CreateEnumValueItem("DisplayStyle", "Text", ToolStripItemDisplayStyle.Text);
                    this.imageStyleToolStripMenuItem    = CreateEnumValueItem("DisplayStyle", "Image", ToolStripItemDisplayStyle.Image); 
                    this.imageTextStyleToolStripMenuItem    = CreateEnumValueItem("DisplayStyle", "ImageAndText", ToolStripItemDisplayStyle.ImageAndText);
                    // 
                    // alignmentToolStripMenuItem 
                    //
 
                    this.alignmentToolStripMenuItem = CreatePropertyBasedItem("Ali&gnment", "Alignment", "alignment.bmp");
                    this.alignmentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                                                                this.leftToolStripMenuItem,
                                                                this.rightToolStripMenuItem}); 
                    //
                    // displayStyleToolStripMenuItem 
                    // 
                    this.displayStyleToolStripMenuItem = CreatePropertyBasedItem("Displa&yStyle", "DisplayStyle", "displaystyle.bmp");
                    this.displayStyleToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                                                                        this.noneStyleToolStripMenuItem,
                                                                        this.textStyleToolStripMenuItem,
                                                                        this.imageStyleToolStripMenuItem,
                                                                        this.imageTextStyleToolStripMenuItem}); 

                    this.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                                    this.alignmentToolStripMenuItem, 
                                    this.displayStyleToolStripMenuItem,
                                    }); 

                }

 
                this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
                this.Add(toolStripSeparator1); 
            } 

            this.convertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); 
            this.convertToolStripMenuItem.Text = SR.GetString(SR.ToolStripItemContextMenuConvertTo);
 			this.convertToolStripMenuItem.DropDown = ToolStripDesignerUtils.GetNewItemDropDown(ParentTool, currentItem, new EventHandler(AddNewItemClick), true, serviceProvider);

            this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); 
            this.insertToolStripMenuItem.Text = SR.GetString(SR.ToolStripItemContextMenuInsert);
            this.insertToolStripMenuItem.DropDown = ToolStripDesignerUtils.GetNewItemDropDown(ParentTool, currentItem, new EventHandler(AddNewItemClick), false, serviceProvider); 
 
            this.AddRange(new System.Windows.Forms.ToolStripItem[] {
                            this.convertToolStripMenuItem, 
                            this.insertToolStripMenuItem});

            if (currentItem is ToolStripDropDownItem)
            { 
                IDesignerHost _designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                if (_designerHost != null) 
                { 
                    ToolStripItemDesigner itemDesigner = _designerHost.GetDesigner(currentItem) as ToolStripItemDesigner;
                    if (itemDesigner != null) 
                    {
                        verbManager = new CollectionEditVerbManager(SR.GetString(SR.ToolStripDropDownItemCollectionEditorVerb), itemDesigner, TypeDescriptor.GetProperties(currentItem)["DropDownItems"], false);
                        this.editItemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                        this.editItemsToolStripMenuItem.Text = SR.GetString(SR.ToolStripDropDownItemCollectionEditorVerb); 
                        this.editItemsToolStripMenuItem.Click += new EventHandler(OnEditItemsMenuItemClick);
                        this.editItemsToolStripMenuItem.Image = new Bitmap(typeof(ToolStripMenuItem), "editdropdownlist.bmp"); 
                        this.editItemsToolStripMenuItem.ImageTransparentColor = Color.Magenta; 

                        this.Add(editItemsToolStripMenuItem); 
                    }
                }
            }
        } 

        private void OnEditItemsMenuItemClick(object sender, EventArgs e) 
        { 
            if (verbManager != null)
            { 
                verbManager.EditItemsVerb.Invoke();
            }
        }
 
        private void OnImageToolStripMenuItemClick(object sender, EventArgs e)
        { 
            IDesignerHost _designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            if (_designerHost != null)
            { 
                ToolStripItemDesigner itemDesigner = _designerHost.GetDesigner(currentItem) as ToolStripItemDesigner;
                if (itemDesigner != null)
                {
                    try 
                    {
                        // EditorServiceContext will check if the user has changed the property and set it for us. 
                        EditorServiceContext.EditValue(itemDesigner, currentItem, "Image"); 
                    }
                    catch (InvalidOperationException ex) 
                    {
                        IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
                        uiService.ShowError(ex.Message);
                    } 
                }
            } 
        } 

        private void OnBooleanValueChanged(object sender, EventArgs e) { 
            ToolStripItem item = sender as ToolStripItem;
            Debug.Assert(item != null, "Why is item null?");

            if (item != null) { 
                string propertyName = item.Tag as string;
                Debug.Assert(propertyName != null, "Why is propertyName null?"); 
                if (propertyName != null) { 
                    bool currentValue = (bool)GetProperty(propertyName);
                    ChangeProperty(propertyName, !currentValue); 
                }
            }
        }
 
        private void OnEnumValueChanged(object sender, EventArgs e) {
            ToolStripItem item = sender as ToolStripItem; 
            Debug.Assert(item != null, "Why is item null?"); 
            if (item != null) {
                EnumValueDescription desc = item.Tag as EnumValueDescription; 
                Debug.Assert(desc != null, "Why is desc null?");

                if (desc != null && !string.IsNullOrEmpty(desc.PropertyName)) {
                    ChangeProperty(desc.PropertyName, desc.Value); 
                }
            } 
        } 

        private void AddNewItemClick(object sender, EventArgs e) 
        {
            ItemTypeToolStripMenuItem senderItem = (ItemTypeToolStripMenuItem)sender;
            Type t = senderItem.ItemType;
 
            if (senderItem.ConvertTo)
            { 
                //we are morphing the currentItem 
                MorphToolStripItem(t);
            } 
            else
            {
                // we are inserting a new item..
                InsertItem(t); 
            }
        } 
 
        private void MorphToolStripItem(Type t)
        { 
            // Go thru morphing routine only if we have different type.
            if (t != currentItem.GetType())
            {
                IDesignerHost _designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
                ToolStripItemDesigner _designer = (ToolStripItemDesigner)_designerHost.GetDesigner(currentItem);
                _designer.MorphCurrentItem(t); 
            } 
        }
 

        // INSERT LOGIC ......
        private void InsertItem(Type t)
        { 
            ToolStripMenuItem item = currentItem as ToolStripMenuItem;
            if (item != null) 
            { 
                InsertMenuItem(t);
            } 
            else
            {
                InsertStripItem(t);
            } 
        }
 
        /// <devdoc> 
        /// Insert MenuItem into ToolStrip.
        /// </devdoc> 
        private void InsertStripItem(Type t)
        {
            StatusStrip parent = ParentTool as StatusStrip;
            if (parent != null) 
            {
                InsertIntoStatusStrip(parent, t); 
            } 
            else
            { 
                InsertToolStripItem(t);
            }

        } 

        /// <devdoc> 
        /// Insert MenuItem into ToolStrip. 
        /// </devdoc>
        private void InsertMenuItem(Type t) 
        {
            MenuStrip parent = ParentTool as MenuStrip;
            if (parent != null)
            { 
                InsertIntoMainMenu(parent, t);
            } 
            else 
            {
                InsertIntoDropDown((ToolStripDropDown)currentItem.Owner, t); 
            }

        }
 

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        private void TryCancelTransaction(ref DesignerTransaction transaction) 
        {
            if (transaction != null) 
            {
                try
                {
                    transaction.Cancel(); 
                    transaction = null;
                } 
                catch 
                {
                } 
            }
        }

        /// <devdoc> 
        /// Insert Item into DropDownMenu.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private void InsertIntoDropDown(ToolStripDropDown parent, Type t)
        { 
            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Why didn't we get a designer host?");

            int dummyIndex = parent.Items.IndexOf(currentItem); 
            if (parent != null)
            { 
                ToolStripDropDownItem ownerItem = parent.OwnerItem as ToolStripDropDownItem; 
                if (ownerItem != null)
                { 
                    if (ownerItem.DropDownDirection == ToolStripDropDownDirection.AboveLeft || ownerItem.DropDownDirection == ToolStripDropDownDirection.AboveRight)
                    {
                      dummyIndex++;
                    } 
                }
 
            } 
            IDesigner designer = null;
 
            DesignerTransaction newItemTransaction = designerHost.CreateTransaction(SR.GetString(SR.ToolStripAddingItem));

            try
            { 
                // the code in ComponentAdded will actually get the add done.
                // 
                IComponent component = designerHost.CreateComponent(t); 
                designer = designerHost.GetDesigner(component);
                if (designer is ComponentDesigner) 
                {
                    ((ComponentDesigner)designer).InitializeNewComponent(null);
                }
 
                parent.Items.Insert(dummyIndex, (ToolStripItem)component);
 
                // set the selection to our new item.. since we destroyed Original component.. 
                // we have to ask SelectionServive from new Component
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if (selSvc != null)
                {
                    selSvc.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace);
                } 
            }
            catch (Exception ex) 
            { 
                // We need to cancel the ToolStripDesigner's nested MenuItemTransaction; otherwise,
                // we can't cancel our Transaction and the Designer will be left in an unusable state 
                if ((parent != null) && (parent.OwnerItem != null) && (parent.OwnerItem.Owner != null))
                {
                    ToolStripDesigner toolStripDesigner = designerHost.GetDesigner(parent.OwnerItem.Owner) as ToolStripDesigner;
                    if (toolStripDesigner != null) 
                    {
                        toolStripDesigner.CancelPendingMenuItemTransaction(); 
                    } 
                }
 
                // Cancel our new Item transaction
                TryCancelTransaction(ref newItemTransaction);

                if (ClientUtils.IsCriticalException(ex)) 
                {
                    throw; 
                } 
            }
            finally 
            {
                if (newItemTransaction != null)
                {
                    newItemTransaction.Commit(); 
                    newItemTransaction = null;
                } 
            } 
        }
 

        /// <devdoc>
        /// Insert Item into Main MenuStrip.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        private void InsertIntoMainMenu(MenuStrip parent, Type t) 
        { 
            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Why didn't we get a designer host?"); 

            int dummyIndex = parent.Items.IndexOf(currentItem);
            IDesigner designer = null;
 
            DesignerTransaction newItemTransaction = designerHost.CreateTransaction(SR.GetString(SR.ToolStripAddingItem));
 
            try 
            {
 
                // the code in ComponentAdded will actually get the add done.
                //
                IComponent component = designerHost.CreateComponent(t);
                designer = designerHost.GetDesigner(component); 

                if (designer is ComponentDesigner) 
                { 
                    ((ComponentDesigner)designer).InitializeNewComponent(null);
                } 

                Debug.Assert(dummyIndex != -1, "Why is item index negative?");
                parent.Items.Insert(dummyIndex, (ToolStripItem)component);
 
                // set the selection to our new item.. since we destroyed Original component..
                // we have to ask SelectionServive from new Component 
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if (selSvc != null)
                { 
                    selSvc.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace);
                }
            }
            catch (Exception ex) 
            {
                TryCancelTransaction(ref newItemTransaction); 
                if (ClientUtils.IsCriticalException(ex)) 
                {
                    throw; 
                }
            }
            finally
            { 
                if (newItemTransaction != null)
                { 
                    newItemTransaction.Commit(); 
                    newItemTransaction = null;
                } 
            }
        }

        /// <devdoc> 
        /// Insert Item into StatusStrip.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private void InsertIntoStatusStrip(StatusStrip parent, Type t)
        { 
            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Why didn't we get a designer host?");

            int dummyIndex = parent.Items.IndexOf(currentItem); 
            IDesigner designer = null;
 
            DesignerTransaction newItemTransaction = designerHost.CreateTransaction(SR.GetString(SR.ToolStripAddingItem)); 

            try 
            {
                // the code in ComponentAdded will actually get the add done.
                //
                IComponent component = designerHost.CreateComponent(t); 
                designer = designerHost.GetDesigner(component);
                if (designer is ComponentDesigner) 
                { 
                    ((ComponentDesigner)designer).InitializeNewComponent(null);
                } 

                Debug.Assert(dummyIndex != -1, "Why is item index negative?");
                parent.Items.Insert(dummyIndex, (ToolStripItem)component);
 
                // set the selection to our new item.. since we destroyed Original component..
                // we have to ask SelectionServive from new Component 
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if (selSvc != null)
                { 
                    selSvc.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace);
                }
            }
            catch (Exception ex) 
            {
                TryCancelTransaction(ref newItemTransaction); 
                if (ClientUtils.IsCriticalException(ex)) 
                {
                    throw; 
                }
            }
            finally
            { 
                if (newItemTransaction != null)
                { 
                    newItemTransaction.Commit(); 
                    newItemTransaction = null;
                } 
            }
        }

 
        /// <devdoc>
        /// Insert Item into ToolStrip. 
        /// </devdoc> 

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        private void InsertToolStripItem(Type t)
        {
            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Why didn't we get a designer host?");
 
            ToolStrip parent = ParentTool; 

            int dummyIndex = parent.Items.IndexOf(currentItem); 
            IDesigner designer = null;

            DesignerTransaction newItemTransaction = designerHost.CreateTransaction(SR.GetString(SR.ToolStripAddingItem));
 
            try
            { 
                // the code in ComponentAdded will actually get the add done. 
                //
                IComponent component = designerHost.CreateComponent(t); 
                designer = designerHost.GetDesigner(component);
                if (designer is ComponentDesigner)
                {
                    ((ComponentDesigner)designer).InitializeNewComponent(null); 
                }
 
                //Set the Image property and DisplayStyle... 
                if (component is ToolStripButton || component is ToolStripSplitButton || component is ToolStripDropDownButton)
                { 
                    Image image = null;
                    try
                    {
                         image = new Bitmap(typeof(ToolStripButton), "blank.bmp"); 
                    }
                    catch (Exception ex) 
                    { 
                        if (ClientUtils.IsCriticalException(ex)) {
                            throw; 
                        }
                    }

                    ChangeProperty(component, "Image", image); 
                    ChangeProperty(component, "DisplayStyle", ToolStripItemDisplayStyle.Image);
                    ChangeProperty(component, "ImageTransparentColor", Color.Magenta); 
 
                }
 
                Debug.Assert(dummyIndex != -1, "Why is item index negative?");
                parent.Items.Insert(dummyIndex, (ToolStripItem)component);

                // set the selection to our new item.. since we destroyed Original component.. 
                // we have to ask SelectionServive from new Component
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if (selSvc != null) 
                {
                    selSvc.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace); 
                }

            }
            catch (Exception ex) 
            {
                if (newItemTransaction != null) 
                { 
                    newItemTransaction.Cancel();
                    newItemTransaction = null; 
                }
                if (ClientUtils.IsCriticalException(ex))
                {
                    throw; 
                }
            } 
 
            finally
            { 
                if (newItemTransaction != null)
                {
                    newItemTransaction.Commit();
                    newItemTransaction = null; 
                }
            } 
        } 
        // END LOGIC
 
        private bool IsPropertyBrowsable(string propertyName) {
            PropertyDescriptor getProperty = TypeDescriptor.GetProperties(currentItem)[propertyName];
            Debug.Assert(getProperty != null, "Could not find given property in control.");
            if (getProperty != null) 
            {
                BrowsableAttribute attribute = getProperty.Attributes[typeof(BrowsableAttribute)] as BrowsableAttribute; 
                if (attribute != null) { 
                    return attribute.Browsable;
                } 
            }
            return true;
        }
 

        //helper function to get the property on the actual Control 
        private object GetProperty(string propertyName) 
        {
            PropertyDescriptor getProperty = TypeDescriptor.GetProperties(currentItem)[propertyName]; 
            Debug.Assert(getProperty != null, "Could not find given property in control.");
            if (getProperty != null)
            {
                return getProperty.GetValue(currentItem); 
            }
            return null; 
        } 

        //helper function to change the property on the actual Control 
        protected void ChangeProperty(string propertyName, object value)
        {
            ChangeProperty(currentItem, propertyName, value);
        } 

        protected void ChangeProperty(IComponent target, string propertyName, object value) 
        { 

            PropertyDescriptor changingProperty = TypeDescriptor.GetProperties(target)[propertyName]; 
            Debug.Assert(changingProperty != null, "Could not find given property in control.");
            try {
                if (changingProperty != null) {
                    changingProperty.SetValue(target, value); 
                }
            } 
            catch (System.InvalidOperationException ex) { 
                IUIService uiService = (IUIService) serviceProvider.GetService(typeof(IUIService));
                uiService.ShowError(ex.Message); 
            }

        }
 
        private void RefreshAlignment()
        { 
            ToolStripItemAlignment currentAlignmentValue = (ToolStripItemAlignment)GetProperty("Alignment"); 

            this.leftToolStripMenuItem.Checked = (currentAlignmentValue == ToolStripItemAlignment.Left) ? true : false; 
            this.rightToolStripMenuItem.Checked = (currentAlignmentValue == ToolStripItemAlignment.Right) ? true : false;

        }
 
        private void RefreshDisplayStyle()
        { 
            ToolStripItemDisplayStyle currentDisplayStyleValue = (ToolStripItemDisplayStyle)GetProperty("DisplayStyle"); 

            this.noneStyleToolStripMenuItem.Checked = (currentDisplayStyleValue == ToolStripItemDisplayStyle.None) ? true : false; 
            this.textStyleToolStripMenuItem.Checked = (currentDisplayStyleValue == ToolStripItemDisplayStyle.Text) ? true : false;
            this.imageStyleToolStripMenuItem.Checked = (currentDisplayStyleValue == ToolStripItemDisplayStyle.Image) ? true : false;
            this.imageTextStyleToolStripMenuItem.Checked = (currentDisplayStyleValue == ToolStripItemDisplayStyle.ImageAndText) ? true : false;
 
        }
 
        public override void RefreshItems() 
        {
            base.RefreshItems(); 

            ToolStripItem selectedItem = currentItem;
            if (!(selectedItem is ToolStripControlHost) && !(selectedItem is ToolStripSeparator))
            { 

                this.enabledToolStripMenuItem.Checked = (bool)GetProperty("Enabled"); 
 
                if (selectedItem is ToolStripMenuItem)
                { 
                    this.checkedToolStripMenuItem.Checked = (bool)GetProperty("Checked");
                    this.showShortcutKeysToolStripMenuItem.Checked = (bool)GetProperty("ShowShortcutKeys");
                }
                else { 
                     if (selectedItem is ToolStripLabel)
                     { 
                         this.isLinkToolStripMenuItem.Checked = (bool)GetProperty("IsLink"); 
                     }
                     RefreshAlignment(); 
                     RefreshDisplayStyle();
                }
            }
        } 

        // tiny little class to handle enum value changes 
        private class EnumValueDescription { 
            public EnumValueDescription(string propertyName, object value) {
                PropertyName = propertyName; 
                Value = value;
            }
            public string PropertyName;
            public object Value; 

        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripContextMenu.cs" company="Microsoft">
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
    using System.Diagnostics.CodeAnalysis;
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
    ///     Custom ContextMenu section for ToolStripMenuItems. 
    /// </summary>
    internal class ToolStripItemCustomMenuItemCollection : CustomMenuItemCollection
    {
 
        private ToolStripItem currentItem;
        private IServiceProvider serviceProvider; 
 
        private ToolStripMenuItem imageToolStripMenuItem;
        private ToolStripMenuItem enabledToolStripMenuItem; 

        private ToolStripMenuItem isLinkToolStripMenuItem;
        private ToolStripMenuItem springToolStripMenuItem;
 
        private ToolStripMenuItem checkedToolStripMenuItem;
        private ToolStripMenuItem showShortcutKeysToolStripMenuItem; 
 
        private ToolStripMenuItem alignmentToolStripMenuItem;
        private ToolStripMenuItem displayStyleToolStripMenuItem; 

        private ToolStripSeparator toolStripSeparator1;

        private ToolStripMenuItem convertToolStripMenuItem; 
        private ToolStripMenuItem insertToolStripMenuItem;
 
 
        private ToolStripMenuItem leftToolStripMenuItem;
        private ToolStripMenuItem rightToolStripMenuItem; 

        private ToolStripMenuItem noneStyleToolStripMenuItem;
        private ToolStripMenuItem textStyleToolStripMenuItem;
        private ToolStripMenuItem imageStyleToolStripMenuItem; 
        private ToolStripMenuItem imageTextStyleToolStripMenuItem;
 
        private ToolStripMenuItem editItemsToolStripMenuItem; 
        private CollectionEditVerbManager verbManager;
 
        public ToolStripItemCustomMenuItemCollection(IServiceProvider provider, Component currentItem): base()
        {
            this.serviceProvider = provider;
            this.currentItem = currentItem as ToolStripItem; 
            PopulateList();
        } 
 

        /// <devdoc> 
        ///      Parent ToolStrip.
        /// </devdoc>
        private ToolStrip ParentTool
        { 
            get
            { 
                return currentItem.Owner; 
            }
        } 

        /// <devdoc>
        /// creates a item representing an item, respecting Browsable.
        /// </devdoc> 
        private ToolStripMenuItem CreatePropertyBasedItem(string text, string propertyName, string imageName) {
            ToolStripMenuItem item = new ToolStripMenuItem(text); 
            bool browsable = IsPropertyBrowsable(propertyName); 
            item.Visible = browsable;
            if (browsable) { 
                if (!string.IsNullOrEmpty(imageName)) {
                    item.Image = new Bitmap(typeof(ToolStripMenuItem), imageName);
                    item.ImageTransparentColor = Color.Magenta;
                } 

                IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService; 
                if (uis != null) { 
                  item.DropDown.Renderer =(ToolStripProfessionalRenderer)uis.Styles["VsRenderer"];
                  item.DropDown.Font = (Font)uis.Styles["DialogFont"]; 
                }
            }
            return item;
        } 

        /// <devdoc> 
        /// creates an item that when clicked changes the enum value. 
        /// </devdoc>
        private ToolStripMenuItem CreateEnumValueItem(string propertyName, string name, object value) { 
            ToolStripMenuItem item = new ToolStripMenuItem(name);
            item.Tag = new EnumValueDescription(propertyName, value);
            item.Click += new EventHandler(OnEnumValueChanged);
            return item; 
        }
 
        private ToolStripMenuItem CreateBooleanItem(string text, string propertyName) { 
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            bool browsable = IsPropertyBrowsable(propertyName); 
            item.Visible = browsable;
            item.Tag = propertyName;
            item.CheckOnClick = true;
            item.Click += new EventHandler(OnBooleanValueChanged); 
            return item;
        } 
 
        // Property names are hard-coded intentionally
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
        private void PopulateList()
        {
            ToolStripItem selectedItem = currentItem;
 
            if (!(selectedItem is ToolStripControlHost) && !(selectedItem is ToolStripSeparator))
            { 
                this.imageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); 
                this.imageToolStripMenuItem.Text = SR.GetString(SR.ToolStripItemContextMenuSetImage);
                this.imageToolStripMenuItem.Image = new Bitmap(typeof(ToolStripMenuItem), "image.bmp"); 
                this.imageToolStripMenuItem.ImageTransparentColor = Color.Magenta;
                //Add event Handlers
                this.imageToolStripMenuItem.Click += new EventHandler(OnImageToolStripMenuItemClick);
 
                this.enabledToolStripMenuItem = CreateBooleanItem("E&nabled", "Enabled");
 
                this.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                    this.imageToolStripMenuItem,
                    this.enabledToolStripMenuItem}); 



                if (selectedItem is ToolStripMenuItem) { 
                    this.checkedToolStripMenuItem = CreateBooleanItem("C&hecked", "Checked");
 
                    this.showShortcutKeysToolStripMenuItem = CreateBooleanItem("ShowShortcut&Keys", "ShowShortcutKeys"); 
                    this.AddRange(new System.Windows.Forms.ToolStripItem[] {
                                    this.checkedToolStripMenuItem, 
                                    this.showShortcutKeysToolStripMenuItem});
                }
                else {
 
                    if (selectedItem is ToolStripLabel)
                    { 
                        this.isLinkToolStripMenuItem = CreateBooleanItem("IsLin&k", "IsLink"); 
                        this.Add(this.isLinkToolStripMenuItem);
                    } 

                    if (selectedItem is ToolStripStatusLabel)
                    {
                        this.springToolStripMenuItem = CreateBooleanItem("Sprin&g", "Spring"); 
                        this.Add(this.springToolStripMenuItem);
                    } 
 
                    this.leftToolStripMenuItem          = CreateEnumValueItem("Alignment", "Left", ToolStripItemAlignment.Left);
                    this.rightToolStripMenuItem         = CreateEnumValueItem("Alignment", "Right", ToolStripItemAlignment.Right); 

                    this.noneStyleToolStripMenuItem     = CreateEnumValueItem("DisplayStyle", "None", ToolStripItemDisplayStyle.None);
                    this.textStyleToolStripMenuItem     = CreateEnumValueItem("DisplayStyle", "Text", ToolStripItemDisplayStyle.Text);
                    this.imageStyleToolStripMenuItem    = CreateEnumValueItem("DisplayStyle", "Image", ToolStripItemDisplayStyle.Image); 
                    this.imageTextStyleToolStripMenuItem    = CreateEnumValueItem("DisplayStyle", "ImageAndText", ToolStripItemDisplayStyle.ImageAndText);
                    // 
                    // alignmentToolStripMenuItem 
                    //
 
                    this.alignmentToolStripMenuItem = CreatePropertyBasedItem("Ali&gnment", "Alignment", "alignment.bmp");
                    this.alignmentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                                                                this.leftToolStripMenuItem,
                                                                this.rightToolStripMenuItem}); 
                    //
                    // displayStyleToolStripMenuItem 
                    // 
                    this.displayStyleToolStripMenuItem = CreatePropertyBasedItem("Displa&yStyle", "DisplayStyle", "displaystyle.bmp");
                    this.displayStyleToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                                                                        this.noneStyleToolStripMenuItem,
                                                                        this.textStyleToolStripMenuItem,
                                                                        this.imageStyleToolStripMenuItem,
                                                                        this.imageTextStyleToolStripMenuItem}); 

                    this.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                                    this.alignmentToolStripMenuItem, 
                                    this.displayStyleToolStripMenuItem,
                                    }); 

                }

 
                this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
                this.Add(toolStripSeparator1); 
            } 

            this.convertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); 
            this.convertToolStripMenuItem.Text = SR.GetString(SR.ToolStripItemContextMenuConvertTo);
 			this.convertToolStripMenuItem.DropDown = ToolStripDesignerUtils.GetNewItemDropDown(ParentTool, currentItem, new EventHandler(AddNewItemClick), true, serviceProvider);

            this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); 
            this.insertToolStripMenuItem.Text = SR.GetString(SR.ToolStripItemContextMenuInsert);
            this.insertToolStripMenuItem.DropDown = ToolStripDesignerUtils.GetNewItemDropDown(ParentTool, currentItem, new EventHandler(AddNewItemClick), false, serviceProvider); 
 
            this.AddRange(new System.Windows.Forms.ToolStripItem[] {
                            this.convertToolStripMenuItem, 
                            this.insertToolStripMenuItem});

            if (currentItem is ToolStripDropDownItem)
            { 
                IDesignerHost _designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                if (_designerHost != null) 
                { 
                    ToolStripItemDesigner itemDesigner = _designerHost.GetDesigner(currentItem) as ToolStripItemDesigner;
                    if (itemDesigner != null) 
                    {
                        verbManager = new CollectionEditVerbManager(SR.GetString(SR.ToolStripDropDownItemCollectionEditorVerb), itemDesigner, TypeDescriptor.GetProperties(currentItem)["DropDownItems"], false);
                        this.editItemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                        this.editItemsToolStripMenuItem.Text = SR.GetString(SR.ToolStripDropDownItemCollectionEditorVerb); 
                        this.editItemsToolStripMenuItem.Click += new EventHandler(OnEditItemsMenuItemClick);
                        this.editItemsToolStripMenuItem.Image = new Bitmap(typeof(ToolStripMenuItem), "editdropdownlist.bmp"); 
                        this.editItemsToolStripMenuItem.ImageTransparentColor = Color.Magenta; 

                        this.Add(editItemsToolStripMenuItem); 
                    }
                }
            }
        } 

        private void OnEditItemsMenuItemClick(object sender, EventArgs e) 
        { 
            if (verbManager != null)
            { 
                verbManager.EditItemsVerb.Invoke();
            }
        }
 
        private void OnImageToolStripMenuItemClick(object sender, EventArgs e)
        { 
            IDesignerHost _designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            if (_designerHost != null)
            { 
                ToolStripItemDesigner itemDesigner = _designerHost.GetDesigner(currentItem) as ToolStripItemDesigner;
                if (itemDesigner != null)
                {
                    try 
                    {
                        // EditorServiceContext will check if the user has changed the property and set it for us. 
                        EditorServiceContext.EditValue(itemDesigner, currentItem, "Image"); 
                    }
                    catch (InvalidOperationException ex) 
                    {
                        IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
                        uiService.ShowError(ex.Message);
                    } 
                }
            } 
        } 

        private void OnBooleanValueChanged(object sender, EventArgs e) { 
            ToolStripItem item = sender as ToolStripItem;
            Debug.Assert(item != null, "Why is item null?");

            if (item != null) { 
                string propertyName = item.Tag as string;
                Debug.Assert(propertyName != null, "Why is propertyName null?"); 
                if (propertyName != null) { 
                    bool currentValue = (bool)GetProperty(propertyName);
                    ChangeProperty(propertyName, !currentValue); 
                }
            }
        }
 
        private void OnEnumValueChanged(object sender, EventArgs e) {
            ToolStripItem item = sender as ToolStripItem; 
            Debug.Assert(item != null, "Why is item null?"); 
            if (item != null) {
                EnumValueDescription desc = item.Tag as EnumValueDescription; 
                Debug.Assert(desc != null, "Why is desc null?");

                if (desc != null && !string.IsNullOrEmpty(desc.PropertyName)) {
                    ChangeProperty(desc.PropertyName, desc.Value); 
                }
            } 
        } 

        private void AddNewItemClick(object sender, EventArgs e) 
        {
            ItemTypeToolStripMenuItem senderItem = (ItemTypeToolStripMenuItem)sender;
            Type t = senderItem.ItemType;
 
            if (senderItem.ConvertTo)
            { 
                //we are morphing the currentItem 
                MorphToolStripItem(t);
            } 
            else
            {
                // we are inserting a new item..
                InsertItem(t); 
            }
        } 
 
        private void MorphToolStripItem(Type t)
        { 
            // Go thru morphing routine only if we have different type.
            if (t != currentItem.GetType())
            {
                IDesignerHost _designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
                ToolStripItemDesigner _designer = (ToolStripItemDesigner)_designerHost.GetDesigner(currentItem);
                _designer.MorphCurrentItem(t); 
            } 
        }
 

        // INSERT LOGIC ......
        private void InsertItem(Type t)
        { 
            ToolStripMenuItem item = currentItem as ToolStripMenuItem;
            if (item != null) 
            { 
                InsertMenuItem(t);
            } 
            else
            {
                InsertStripItem(t);
            } 
        }
 
        /// <devdoc> 
        /// Insert MenuItem into ToolStrip.
        /// </devdoc> 
        private void InsertStripItem(Type t)
        {
            StatusStrip parent = ParentTool as StatusStrip;
            if (parent != null) 
            {
                InsertIntoStatusStrip(parent, t); 
            } 
            else
            { 
                InsertToolStripItem(t);
            }

        } 

        /// <devdoc> 
        /// Insert MenuItem into ToolStrip. 
        /// </devdoc>
        private void InsertMenuItem(Type t) 
        {
            MenuStrip parent = ParentTool as MenuStrip;
            if (parent != null)
            { 
                InsertIntoMainMenu(parent, t);
            } 
            else 
            {
                InsertIntoDropDown((ToolStripDropDown)currentItem.Owner, t); 
            }

        }
 

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        private void TryCancelTransaction(ref DesignerTransaction transaction) 
        {
            if (transaction != null) 
            {
                try
                {
                    transaction.Cancel(); 
                    transaction = null;
                } 
                catch 
                {
                } 
            }
        }

        /// <devdoc> 
        /// Insert Item into DropDownMenu.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private void InsertIntoDropDown(ToolStripDropDown parent, Type t)
        { 
            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Why didn't we get a designer host?");

            int dummyIndex = parent.Items.IndexOf(currentItem); 
            if (parent != null)
            { 
                ToolStripDropDownItem ownerItem = parent.OwnerItem as ToolStripDropDownItem; 
                if (ownerItem != null)
                { 
                    if (ownerItem.DropDownDirection == ToolStripDropDownDirection.AboveLeft || ownerItem.DropDownDirection == ToolStripDropDownDirection.AboveRight)
                    {
                      dummyIndex++;
                    } 
                }
 
            } 
            IDesigner designer = null;
 
            DesignerTransaction newItemTransaction = designerHost.CreateTransaction(SR.GetString(SR.ToolStripAddingItem));

            try
            { 
                // the code in ComponentAdded will actually get the add done.
                // 
                IComponent component = designerHost.CreateComponent(t); 
                designer = designerHost.GetDesigner(component);
                if (designer is ComponentDesigner) 
                {
                    ((ComponentDesigner)designer).InitializeNewComponent(null);
                }
 
                parent.Items.Insert(dummyIndex, (ToolStripItem)component);
 
                // set the selection to our new item.. since we destroyed Original component.. 
                // we have to ask SelectionServive from new Component
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if (selSvc != null)
                {
                    selSvc.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace);
                } 
            }
            catch (Exception ex) 
            { 
                // We need to cancel the ToolStripDesigner's nested MenuItemTransaction; otherwise,
                // we can't cancel our Transaction and the Designer will be left in an unusable state 
                if ((parent != null) && (parent.OwnerItem != null) && (parent.OwnerItem.Owner != null))
                {
                    ToolStripDesigner toolStripDesigner = designerHost.GetDesigner(parent.OwnerItem.Owner) as ToolStripDesigner;
                    if (toolStripDesigner != null) 
                    {
                        toolStripDesigner.CancelPendingMenuItemTransaction(); 
                    } 
                }
 
                // Cancel our new Item transaction
                TryCancelTransaction(ref newItemTransaction);

                if (ClientUtils.IsCriticalException(ex)) 
                {
                    throw; 
                } 
            }
            finally 
            {
                if (newItemTransaction != null)
                {
                    newItemTransaction.Commit(); 
                    newItemTransaction = null;
                } 
            } 
        }
 

        /// <devdoc>
        /// Insert Item into Main MenuStrip.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        private void InsertIntoMainMenu(MenuStrip parent, Type t) 
        { 
            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Why didn't we get a designer host?"); 

            int dummyIndex = parent.Items.IndexOf(currentItem);
            IDesigner designer = null;
 
            DesignerTransaction newItemTransaction = designerHost.CreateTransaction(SR.GetString(SR.ToolStripAddingItem));
 
            try 
            {
 
                // the code in ComponentAdded will actually get the add done.
                //
                IComponent component = designerHost.CreateComponent(t);
                designer = designerHost.GetDesigner(component); 

                if (designer is ComponentDesigner) 
                { 
                    ((ComponentDesigner)designer).InitializeNewComponent(null);
                } 

                Debug.Assert(dummyIndex != -1, "Why is item index negative?");
                parent.Items.Insert(dummyIndex, (ToolStripItem)component);
 
                // set the selection to our new item.. since we destroyed Original component..
                // we have to ask SelectionServive from new Component 
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if (selSvc != null)
                { 
                    selSvc.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace);
                }
            }
            catch (Exception ex) 
            {
                TryCancelTransaction(ref newItemTransaction); 
                if (ClientUtils.IsCriticalException(ex)) 
                {
                    throw; 
                }
            }
            finally
            { 
                if (newItemTransaction != null)
                { 
                    newItemTransaction.Commit(); 
                    newItemTransaction = null;
                } 
            }
        }

        /// <devdoc> 
        /// Insert Item into StatusStrip.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private void InsertIntoStatusStrip(StatusStrip parent, Type t)
        { 
            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Why didn't we get a designer host?");

            int dummyIndex = parent.Items.IndexOf(currentItem); 
            IDesigner designer = null;
 
            DesignerTransaction newItemTransaction = designerHost.CreateTransaction(SR.GetString(SR.ToolStripAddingItem)); 

            try 
            {
                // the code in ComponentAdded will actually get the add done.
                //
                IComponent component = designerHost.CreateComponent(t); 
                designer = designerHost.GetDesigner(component);
                if (designer is ComponentDesigner) 
                { 
                    ((ComponentDesigner)designer).InitializeNewComponent(null);
                } 

                Debug.Assert(dummyIndex != -1, "Why is item index negative?");
                parent.Items.Insert(dummyIndex, (ToolStripItem)component);
 
                // set the selection to our new item.. since we destroyed Original component..
                // we have to ask SelectionServive from new Component 
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if (selSvc != null)
                { 
                    selSvc.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace);
                }
            }
            catch (Exception ex) 
            {
                TryCancelTransaction(ref newItemTransaction); 
                if (ClientUtils.IsCriticalException(ex)) 
                {
                    throw; 
                }
            }
            finally
            { 
                if (newItemTransaction != null)
                { 
                    newItemTransaction.Commit(); 
                    newItemTransaction = null;
                } 
            }
        }

 
        /// <devdoc>
        /// Insert Item into ToolStrip. 
        /// </devdoc> 

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        private void InsertToolStripItem(Type t)
        {
            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Why didn't we get a designer host?");
 
            ToolStrip parent = ParentTool; 

            int dummyIndex = parent.Items.IndexOf(currentItem); 
            IDesigner designer = null;

            DesignerTransaction newItemTransaction = designerHost.CreateTransaction(SR.GetString(SR.ToolStripAddingItem));
 
            try
            { 
                // the code in ComponentAdded will actually get the add done. 
                //
                IComponent component = designerHost.CreateComponent(t); 
                designer = designerHost.GetDesigner(component);
                if (designer is ComponentDesigner)
                {
                    ((ComponentDesigner)designer).InitializeNewComponent(null); 
                }
 
                //Set the Image property and DisplayStyle... 
                if (component is ToolStripButton || component is ToolStripSplitButton || component is ToolStripDropDownButton)
                { 
                    Image image = null;
                    try
                    {
                         image = new Bitmap(typeof(ToolStripButton), "blank.bmp"); 
                    }
                    catch (Exception ex) 
                    { 
                        if (ClientUtils.IsCriticalException(ex)) {
                            throw; 
                        }
                    }

                    ChangeProperty(component, "Image", image); 
                    ChangeProperty(component, "DisplayStyle", ToolStripItemDisplayStyle.Image);
                    ChangeProperty(component, "ImageTransparentColor", Color.Magenta); 
 
                }
 
                Debug.Assert(dummyIndex != -1, "Why is item index negative?");
                parent.Items.Insert(dummyIndex, (ToolStripItem)component);

                // set the selection to our new item.. since we destroyed Original component.. 
                // we have to ask SelectionServive from new Component
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if (selSvc != null) 
                {
                    selSvc.SetSelectedComponents(new object[] { component }, SelectionTypes.Replace); 
                }

            }
            catch (Exception ex) 
            {
                if (newItemTransaction != null) 
                { 
                    newItemTransaction.Cancel();
                    newItemTransaction = null; 
                }
                if (ClientUtils.IsCriticalException(ex))
                {
                    throw; 
                }
            } 
 
            finally
            { 
                if (newItemTransaction != null)
                {
                    newItemTransaction.Commit();
                    newItemTransaction = null; 
                }
            } 
        } 
        // END LOGIC
 
        private bool IsPropertyBrowsable(string propertyName) {
            PropertyDescriptor getProperty = TypeDescriptor.GetProperties(currentItem)[propertyName];
            Debug.Assert(getProperty != null, "Could not find given property in control.");
            if (getProperty != null) 
            {
                BrowsableAttribute attribute = getProperty.Attributes[typeof(BrowsableAttribute)] as BrowsableAttribute; 
                if (attribute != null) { 
                    return attribute.Browsable;
                } 
            }
            return true;
        }
 

        //helper function to get the property on the actual Control 
        private object GetProperty(string propertyName) 
        {
            PropertyDescriptor getProperty = TypeDescriptor.GetProperties(currentItem)[propertyName]; 
            Debug.Assert(getProperty != null, "Could not find given property in control.");
            if (getProperty != null)
            {
                return getProperty.GetValue(currentItem); 
            }
            return null; 
        } 

        //helper function to change the property on the actual Control 
        protected void ChangeProperty(string propertyName, object value)
        {
            ChangeProperty(currentItem, propertyName, value);
        } 

        protected void ChangeProperty(IComponent target, string propertyName, object value) 
        { 

            PropertyDescriptor changingProperty = TypeDescriptor.GetProperties(target)[propertyName]; 
            Debug.Assert(changingProperty != null, "Could not find given property in control.");
            try {
                if (changingProperty != null) {
                    changingProperty.SetValue(target, value); 
                }
            } 
            catch (System.InvalidOperationException ex) { 
                IUIService uiService = (IUIService) serviceProvider.GetService(typeof(IUIService));
                uiService.ShowError(ex.Message); 
            }

        }
 
        private void RefreshAlignment()
        { 
            ToolStripItemAlignment currentAlignmentValue = (ToolStripItemAlignment)GetProperty("Alignment"); 

            this.leftToolStripMenuItem.Checked = (currentAlignmentValue == ToolStripItemAlignment.Left) ? true : false; 
            this.rightToolStripMenuItem.Checked = (currentAlignmentValue == ToolStripItemAlignment.Right) ? true : false;

        }
 
        private void RefreshDisplayStyle()
        { 
            ToolStripItemDisplayStyle currentDisplayStyleValue = (ToolStripItemDisplayStyle)GetProperty("DisplayStyle"); 

            this.noneStyleToolStripMenuItem.Checked = (currentDisplayStyleValue == ToolStripItemDisplayStyle.None) ? true : false; 
            this.textStyleToolStripMenuItem.Checked = (currentDisplayStyleValue == ToolStripItemDisplayStyle.Text) ? true : false;
            this.imageStyleToolStripMenuItem.Checked = (currentDisplayStyleValue == ToolStripItemDisplayStyle.Image) ? true : false;
            this.imageTextStyleToolStripMenuItem.Checked = (currentDisplayStyleValue == ToolStripItemDisplayStyle.ImageAndText) ? true : false;
 
        }
 
        public override void RefreshItems() 
        {
            base.RefreshItems(); 

            ToolStripItem selectedItem = currentItem;
            if (!(selectedItem is ToolStripControlHost) && !(selectedItem is ToolStripSeparator))
            { 

                this.enabledToolStripMenuItem.Checked = (bool)GetProperty("Enabled"); 
 
                if (selectedItem is ToolStripMenuItem)
                { 
                    this.checkedToolStripMenuItem.Checked = (bool)GetProperty("Checked");
                    this.showShortcutKeysToolStripMenuItem.Checked = (bool)GetProperty("ShowShortcutKeys");
                }
                else { 
                     if (selectedItem is ToolStripLabel)
                     { 
                         this.isLinkToolStripMenuItem.Checked = (bool)GetProperty("IsLink"); 
                     }
                     RefreshAlignment(); 
                     RefreshDisplayStyle();
                }
            }
        } 

        // tiny little class to handle enum value changes 
        private class EnumValueDescription { 
            public EnumValueDescription(string propertyName, object value) {
                PropertyName = propertyName; 
                Value = value;
            }
            public string PropertyName;
            public object Value; 

        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
