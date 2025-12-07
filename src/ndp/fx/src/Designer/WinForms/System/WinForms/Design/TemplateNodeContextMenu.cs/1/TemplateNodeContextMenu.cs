//------------------------------------------------------------------------------ 
// <copyright file="TemplateNodeCustomMenuItemCollection.cs" company="Microsoft">
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
    internal class TemplateNodeCustomMenuItemCollection : CustomMenuItemCollection
    {
 
        private ToolStripItem currentItem;
        private IServiceProvider serviceProvider; 
 
        private ToolStripMenuItem insertToolStripMenuItem;
 
        public TemplateNodeCustomMenuItemCollection(IServiceProvider provider, Component currentItem): base()
        {
            this.serviceProvider = provider;
            this.currentItem = currentItem as ToolStripItem; 
            PopulateList();
        } 
 
        /// <devdoc>
        ///      Immediate parent 
        ///         - can be ToolStrip if the Item is on the toplevel
        /// </devdoc>
        private ToolStrip ParentTool
        { 
            get
            { 
                return currentItem.Owner; 
            }
        } 


        private void PopulateList()
        { 

            this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); 
            this.insertToolStripMenuItem.Text = SR.GetString(SR.ToolStripItemContextMenuInsert); 
            this.insertToolStripMenuItem.DropDown = ToolStripDesignerUtils.GetNewItemDropDown(ParentTool, currentItem, new EventHandler(AddNewItemClick), false, serviceProvider);
 
            this.Add(insertToolStripMenuItem);

        }
 
        private void AddNewItemClick(object sender, EventArgs e)
        { 
            ItemTypeToolStripMenuItem senderItem = (ItemTypeToolStripMenuItem)sender; 
            Type t = senderItem.ItemType;
            // we are inserting a new item.. 
            InsertItem(t);
        }

        // INSERT LOGIC ...... 
        private void InsertItem(Type t)
        { 
            InsertToolStripItem(t); 

        } 

        /// <devdoc>
        /// Insert Item into ToolStrip.
        /// </devdoc> 
        // Standard 'catch all - rethrow critical' exception pattern
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
                // turn off Adding/Added events listened to by the ToolStripDesigner... 
                ToolStripDesigner._autoAddNewItems = false;
 
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
                    catch (Exception e) 
                    { 
                         if (ClientUtils.IsCriticalException(e)) {
                             throw; 
                         }
                    }

                    PropertyDescriptor imageProperty = TypeDescriptor.GetProperties(component)["Image"]; 
                    Debug.Assert(imageProperty != null, "Could not find 'Image' property in ToolStripItem.");
                    if (imageProperty != null && image != null) 
                    { 
                        imageProperty.SetValue(component, image);
                    } 

                    PropertyDescriptor dispProperty = TypeDescriptor.GetProperties(component)["DisplayStyle"];
                    Debug.Assert(dispProperty != null, "Could not find 'DisplayStyle' property in ToolStripItem.");
                    if (dispProperty != null) 
                    {
                        dispProperty.SetValue(component, ToolStripItemDisplayStyle.Image); 
                    } 

                    PropertyDescriptor imageTransProperty = TypeDescriptor.GetProperties(component)["ImageTransparentColor"]; 
                    Debug.Assert(imageTransProperty != null, "Could not find 'DisplayStyle' property in ToolStripItem.");
                    if (imageTransProperty != null)
                    {
                        imageTransProperty.SetValue(component, Color.Magenta); 
                    }
 
                } 

                Debug.Assert(dummyIndex != -1, "Why is the index of the Item negative?"); 
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
                // turn off Adding/Added events listened to by the ToolStripDesigner... 
                ToolStripDesigner._autoAddNewItems = true;
 
                // Add the glyphs if the parent is DropDown. 
                ToolStripDropDown parentDropDown = parent as ToolStripDropDown;
                if (parentDropDown != null && parentDropDown.Visible) 
                {
                    ToolStripDropDownItem ownerItem = parentDropDown.OwnerItem as ToolStripDropDownItem;
                    if (ownerItem != null)
                    { 
                        ToolStripMenuItemDesigner itemDesigner = designerHost.GetDesigner(ownerItem) as ToolStripMenuItemDesigner;
                        if (itemDesigner != null) 
                        { 
                            itemDesigner.ResetGlyphs(ownerItem);
                        } 
                    }
                }
            }
        } 
        // END LOGIC
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TemplateNodeCustomMenuItemCollection.cs" company="Microsoft">
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
    internal class TemplateNodeCustomMenuItemCollection : CustomMenuItemCollection
    {
 
        private ToolStripItem currentItem;
        private IServiceProvider serviceProvider; 
 
        private ToolStripMenuItem insertToolStripMenuItem;
 
        public TemplateNodeCustomMenuItemCollection(IServiceProvider provider, Component currentItem): base()
        {
            this.serviceProvider = provider;
            this.currentItem = currentItem as ToolStripItem; 
            PopulateList();
        } 
 
        /// <devdoc>
        ///      Immediate parent 
        ///         - can be ToolStrip if the Item is on the toplevel
        /// </devdoc>
        private ToolStrip ParentTool
        { 
            get
            { 
                return currentItem.Owner; 
            }
        } 


        private void PopulateList()
        { 

            this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); 
            this.insertToolStripMenuItem.Text = SR.GetString(SR.ToolStripItemContextMenuInsert); 
            this.insertToolStripMenuItem.DropDown = ToolStripDesignerUtils.GetNewItemDropDown(ParentTool, currentItem, new EventHandler(AddNewItemClick), false, serviceProvider);
 
            this.Add(insertToolStripMenuItem);

        }
 
        private void AddNewItemClick(object sender, EventArgs e)
        { 
            ItemTypeToolStripMenuItem senderItem = (ItemTypeToolStripMenuItem)sender; 
            Type t = senderItem.ItemType;
            // we are inserting a new item.. 
            InsertItem(t);
        }

        // INSERT LOGIC ...... 
        private void InsertItem(Type t)
        { 
            InsertToolStripItem(t); 

        } 

        /// <devdoc>
        /// Insert Item into ToolStrip.
        /// </devdoc> 
        // Standard 'catch all - rethrow critical' exception pattern
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
                // turn off Adding/Added events listened to by the ToolStripDesigner... 
                ToolStripDesigner._autoAddNewItems = false;
 
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
                    catch (Exception e) 
                    { 
                         if (ClientUtils.IsCriticalException(e)) {
                             throw; 
                         }
                    }

                    PropertyDescriptor imageProperty = TypeDescriptor.GetProperties(component)["Image"]; 
                    Debug.Assert(imageProperty != null, "Could not find 'Image' property in ToolStripItem.");
                    if (imageProperty != null && image != null) 
                    { 
                        imageProperty.SetValue(component, image);
                    } 

                    PropertyDescriptor dispProperty = TypeDescriptor.GetProperties(component)["DisplayStyle"];
                    Debug.Assert(dispProperty != null, "Could not find 'DisplayStyle' property in ToolStripItem.");
                    if (dispProperty != null) 
                    {
                        dispProperty.SetValue(component, ToolStripItemDisplayStyle.Image); 
                    } 

                    PropertyDescriptor imageTransProperty = TypeDescriptor.GetProperties(component)["ImageTransparentColor"]; 
                    Debug.Assert(imageTransProperty != null, "Could not find 'DisplayStyle' property in ToolStripItem.");
                    if (imageTransProperty != null)
                    {
                        imageTransProperty.SetValue(component, Color.Magenta); 
                    }
 
                } 

                Debug.Assert(dummyIndex != -1, "Why is the index of the Item negative?"); 
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
                // turn off Adding/Added events listened to by the ToolStripDesigner... 
                ToolStripDesigner._autoAddNewItems = true;
 
                // Add the glyphs if the parent is DropDown. 
                ToolStripDropDown parentDropDown = parent as ToolStripDropDown;
                if (parentDropDown != null && parentDropDown.Visible) 
                {
                    ToolStripDropDownItem ownerItem = parentDropDown.OwnerItem as ToolStripDropDownItem;
                    if (ownerItem != null)
                    { 
                        ToolStripMenuItemDesigner itemDesigner = designerHost.GetDesigner(ownerItem) as ToolStripMenuItemDesigner;
                        if (itemDesigner != null) 
                        { 
                            itemDesigner.ResetGlyphs(ownerItem);
                        } 
                    }
                }
            }
        } 
        // END LOGIC
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
