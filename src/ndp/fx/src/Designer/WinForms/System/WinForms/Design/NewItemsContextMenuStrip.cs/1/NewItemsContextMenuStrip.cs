//------------------------------------------------------------------------------ 
// <copyright file="NewItemsContextMenuStrip.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections.Generic;
    using System.Text;
    using System.Collections.Specialized;
    using System.Windows.Forms; 
    using System.ComponentModel;
 
    internal class NewItemsContextMenuStrip : GroupedContextMenuStrip { 
        IComponent component = null;
        EventHandler onClick = null; 
        bool convertTo = false;
        IServiceProvider serviceProvider = null;
        ToolStripItem currentItem;
 

        public NewItemsContextMenuStrip(IComponent component, ToolStripItem currentItem, EventHandler onClick, bool convertTo, IServiceProvider serviceProvider) 
        { 
            this.component = component;
            this.onClick = onClick; 
            this.convertTo = convertTo;
            this.serviceProvider = serviceProvider;
            this.currentItem = currentItem;
 
            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService;
            if (uis != null) { 
                this.Renderer =(ToolStripProfessionalRenderer)uis.Styles["VsRenderer"]; 
            }
        } 
        protected override void OnOpening(CancelEventArgs e) {
            this.Groups["StandardList"].Items.Clear();
            this.Groups["CustomList"].Items.Clear();
 			Populated = false; 

            // plumb through the standard and custom items. 
            foreach (ToolStripItem item in ToolStripDesignerUtils.GetStandardItemMenuItems(component, onClick, convertTo)) { 
                this.Groups["StandardList"].Items.Add(item);
                if (convertTo) 
                {
                    ItemTypeToolStripMenuItem toolItem = item as ItemTypeToolStripMenuItem;
                    if (toolItem != null && currentItem != null && toolItem.ItemType == currentItem.GetType())
                    { 
                        toolItem.Enabled = false;
                    } 
                } 

            } 
            foreach (ToolStripItem item in ToolStripDesignerUtils.GetCustomItemMenuItems(component, onClick, convertTo, serviceProvider)) {
                this.Groups["CustomList"].Items.Add(item);
                if (convertTo)
                { 
                    ItemTypeToolStripMenuItem toolItem = item as ItemTypeToolStripMenuItem;
                    if (toolItem != null && currentItem != null && toolItem.ItemType == currentItem.GetType()) 
                    { 
                        toolItem.Enabled = false;
                    } 
                }
            }
            base.OnOpening(e);
        } 

        // Please refer to VsW: 505199 for more details. We dont want the runtime behavior for this Design Time only DropDown and hence we overide the ProcessDialogKey and 
        // just close the DropDown instead of running through the runtime implementation for RIGHT/LEFT Keys which ends up setting ModalMenuFilter. 
        protected override bool ProcessDialogKey(Keys keyData) {
            Keys keyCode = (Keys)keyData & Keys.KeyCode; 
            switch (keyCode) {
                case Keys.Left:
                case Keys.Right:
                    this.Close(); 
                    return true;
            } 
            return base.ProcessDialogKey(keyData); 
        }
 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="NewItemsContextMenuStrip.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections.Generic;
    using System.Text;
    using System.Collections.Specialized;
    using System.Windows.Forms; 
    using System.ComponentModel;
 
    internal class NewItemsContextMenuStrip : GroupedContextMenuStrip { 
        IComponent component = null;
        EventHandler onClick = null; 
        bool convertTo = false;
        IServiceProvider serviceProvider = null;
        ToolStripItem currentItem;
 

        public NewItemsContextMenuStrip(IComponent component, ToolStripItem currentItem, EventHandler onClick, bool convertTo, IServiceProvider serviceProvider) 
        { 
            this.component = component;
            this.onClick = onClick; 
            this.convertTo = convertTo;
            this.serviceProvider = serviceProvider;
            this.currentItem = currentItem;
 
            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService;
            if (uis != null) { 
                this.Renderer =(ToolStripProfessionalRenderer)uis.Styles["VsRenderer"]; 
            }
        } 
        protected override void OnOpening(CancelEventArgs e) {
            this.Groups["StandardList"].Items.Clear();
            this.Groups["CustomList"].Items.Clear();
 			Populated = false; 

            // plumb through the standard and custom items. 
            foreach (ToolStripItem item in ToolStripDesignerUtils.GetStandardItemMenuItems(component, onClick, convertTo)) { 
                this.Groups["StandardList"].Items.Add(item);
                if (convertTo) 
                {
                    ItemTypeToolStripMenuItem toolItem = item as ItemTypeToolStripMenuItem;
                    if (toolItem != null && currentItem != null && toolItem.ItemType == currentItem.GetType())
                    { 
                        toolItem.Enabled = false;
                    } 
                } 

            } 
            foreach (ToolStripItem item in ToolStripDesignerUtils.GetCustomItemMenuItems(component, onClick, convertTo, serviceProvider)) {
                this.Groups["CustomList"].Items.Add(item);
                if (convertTo)
                { 
                    ItemTypeToolStripMenuItem toolItem = item as ItemTypeToolStripMenuItem;
                    if (toolItem != null && currentItem != null && toolItem.ItemType == currentItem.GetType()) 
                    { 
                        toolItem.Enabled = false;
                    } 
                }
            }
            base.OnOpening(e);
        } 

        // Please refer to VsW: 505199 for more details. We dont want the runtime behavior for this Design Time only DropDown and hence we overide the ProcessDialogKey and 
        // just close the DropDown instead of running through the runtime implementation for RIGHT/LEFT Keys which ends up setting ModalMenuFilter. 
        protected override bool ProcessDialogKey(Keys keyData) {
            Keys keyCode = (Keys)keyData & Keys.KeyCode; 
            switch (keyCode) {
                case Keys.Left:
                case Keys.Right:
                    this.Close(); 
                    return true;
            } 
            return base.ProcessDialogKey(keyData); 
        }
 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
