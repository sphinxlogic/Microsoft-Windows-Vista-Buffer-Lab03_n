//------------------------------------------------------------------------------ 
// <copyright file="ContextMenu.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.RichTextBoxContextMenu..ctor()")]
 
namespace System.Windows.Forms.Design { 
    using Microsoft.Win32;
    using System; 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing;
    using System.Windows.Forms; 
 
    /// <devdoc>
    ///     Context menu for the RichTextBox control 
    ///     We only allow copy/cut/paste of texts
    /// </devdoc>
    internal class RichTextBoxContextMenu : ContextMenu {
        private MenuItem undoMenu; 
        private MenuItem cutMenu;
        private MenuItem copyMenu; 
        private MenuItem pasteMenu; 
        private MenuItem deleteMenu;
        private MenuItem selectAllMenu; 
        private RichTextBox parent;  //the RichTextBox which hosts this context menu


        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
        public RichTextBoxContextMenu(RichTextBox parent) : base() {
            undoMenu = new MenuItem(SR.GetString(SR.StandardMenuUndo), new EventHandler(undoMenu_Clicked)); 
            cutMenu = new MenuItem(SR.GetString(SR.StandardMenuCut), new EventHandler(cutMenu_Clicked)); 
            copyMenu = new MenuItem(SR.GetString(SR.StandardMenuCopy), new EventHandler(copyMenu_Clicked));
            pasteMenu = new MenuItem(SR.GetString(SR.StandardMenuPaste), new EventHandler(pasteMenu_Clicked)); 
            deleteMenu = new MenuItem(SR.GetString(SR.StandardMenuDelete), new EventHandler(deleteMenu_Clicked));
            selectAllMenu = new MenuItem(SR.GetString(SR.StandardMenuSelectAll), new EventHandler(selectAllMenu_Clicked));
            MenuItem splitter1 = new MenuItem("-");
            MenuItem splitter2 = new MenuItem("-"); 

            this.MenuItems.Add(undoMenu); 
            this.MenuItems.Add(splitter1); 
            this.MenuItems.Add(cutMenu);
            this.MenuItems.Add(copyMenu); 
            this.MenuItems.Add(pasteMenu);
            this.MenuItems.Add(deleteMenu);
            this.MenuItems.Add(splitter2);
            this.MenuItems.Add(selectAllMenu); 

            this.parent = parent; 
        } 

        /// <devdoc> 
        ///     Set the appropriate visibility of the menu items
        /// </devdoc>
        protected override void OnPopup(EventArgs e) {
            if (parent.SelectionLength > 0) { 
                cutMenu.Enabled = true;
                copyMenu.Enabled = true; 
                deleteMenu.Enabled = true; 
            }
            else { 
                cutMenu.Enabled = false;
                copyMenu.Enabled = false;
                deleteMenu.Enabled = false;
            } 
            if (Clipboard.GetText() != null) {
                pasteMenu.Enabled = true; 
            } 
            else {
                pasteMenu.Enabled = false; 
            }
            if (parent.CanUndo) {
                undoMenu.Enabled = true;
            } 
            else {
                undoMenu.Enabled = false; 
            } 
        }
 
        private void cutMenu_Clicked(object sender, EventArgs e) {
            Clipboard.SetText(parent.SelectedText);
            parent.SelectedText = "";
        } 

        private void copyMenu_Clicked(object sender, EventArgs e) { 
            Clipboard.SetText(parent.SelectedText); 
        }
 
        private void deleteMenu_Clicked(object sender, EventArgs e) {
            parent.SelectedText = "";
        }
 
        private void pasteMenu_Clicked(object sender, EventArgs e) {
            parent.SelectedText = Clipboard.GetText(); 
        } 

        private void selectAllMenu_Clicked(object sender, EventArgs e) { 
            parent.SelectAll();
        }

        private void undoMenu_Clicked(object sender, EventArgs e) { 
            parent.Undo();
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ContextMenu.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.RichTextBoxContextMenu..ctor()")]
 
namespace System.Windows.Forms.Design { 
    using Microsoft.Win32;
    using System; 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing;
    using System.Windows.Forms; 
 
    /// <devdoc>
    ///     Context menu for the RichTextBox control 
    ///     We only allow copy/cut/paste of texts
    /// </devdoc>
    internal class RichTextBoxContextMenu : ContextMenu {
        private MenuItem undoMenu; 
        private MenuItem cutMenu;
        private MenuItem copyMenu; 
        private MenuItem pasteMenu; 
        private MenuItem deleteMenu;
        private MenuItem selectAllMenu; 
        private RichTextBox parent;  //the RichTextBox which hosts this context menu


        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
        public RichTextBoxContextMenu(RichTextBox parent) : base() {
            undoMenu = new MenuItem(SR.GetString(SR.StandardMenuUndo), new EventHandler(undoMenu_Clicked)); 
            cutMenu = new MenuItem(SR.GetString(SR.StandardMenuCut), new EventHandler(cutMenu_Clicked)); 
            copyMenu = new MenuItem(SR.GetString(SR.StandardMenuCopy), new EventHandler(copyMenu_Clicked));
            pasteMenu = new MenuItem(SR.GetString(SR.StandardMenuPaste), new EventHandler(pasteMenu_Clicked)); 
            deleteMenu = new MenuItem(SR.GetString(SR.StandardMenuDelete), new EventHandler(deleteMenu_Clicked));
            selectAllMenu = new MenuItem(SR.GetString(SR.StandardMenuSelectAll), new EventHandler(selectAllMenu_Clicked));
            MenuItem splitter1 = new MenuItem("-");
            MenuItem splitter2 = new MenuItem("-"); 

            this.MenuItems.Add(undoMenu); 
            this.MenuItems.Add(splitter1); 
            this.MenuItems.Add(cutMenu);
            this.MenuItems.Add(copyMenu); 
            this.MenuItems.Add(pasteMenu);
            this.MenuItems.Add(deleteMenu);
            this.MenuItems.Add(splitter2);
            this.MenuItems.Add(selectAllMenu); 

            this.parent = parent; 
        } 

        /// <devdoc> 
        ///     Set the appropriate visibility of the menu items
        /// </devdoc>
        protected override void OnPopup(EventArgs e) {
            if (parent.SelectionLength > 0) { 
                cutMenu.Enabled = true;
                copyMenu.Enabled = true; 
                deleteMenu.Enabled = true; 
            }
            else { 
                cutMenu.Enabled = false;
                copyMenu.Enabled = false;
                deleteMenu.Enabled = false;
            } 
            if (Clipboard.GetText() != null) {
                pasteMenu.Enabled = true; 
            } 
            else {
                pasteMenu.Enabled = false; 
            }
            if (parent.CanUndo) {
                undoMenu.Enabled = true;
            } 
            else {
                undoMenu.Enabled = false; 
            } 
        }
 
        private void cutMenu_Clicked(object sender, EventArgs e) {
            Clipboard.SetText(parent.SelectedText);
            parent.SelectedText = "";
        } 

        private void copyMenu_Clicked(object sender, EventArgs e) { 
            Clipboard.SetText(parent.SelectedText); 
        }
 
        private void deleteMenu_Clicked(object sender, EventArgs e) {
            parent.SelectedText = "";
        }
 
        private void pasteMenu_Clicked(object sender, EventArgs e) {
            parent.SelectedText = Clipboard.GetText(); 
        } 

        private void selectAllMenu_Clicked(object sender, EventArgs e) { 
            parent.SelectAll();
        }

        private void undoMenu_Clicked(object sender, EventArgs e) { 
            parent.Undo();
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
