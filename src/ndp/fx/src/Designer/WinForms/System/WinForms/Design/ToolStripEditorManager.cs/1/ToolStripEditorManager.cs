//------------------------------------------------------------------------------ 
// <copyright file="ToolStripEditorManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing; 
    using System.Design;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.Design.Behavior; 
    using System.Collections;
 
 
    /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager"]/*' />
    /// <devdoc> 
    ///     This internal Class is used by all TOPLEVEL ToolStripItems to show the InSitu Editor.
    ///
    ///     When the ToolStripItem receives the MouseDown on its Glyph it calls the "ActivateEditor"
    ///     Function on this EditorManager. 
    ///
    ///     The ActivateEditor Function checks for any existing "EDITOR" active, closes that down 
    ///     and now opens the new editor on the "AdornerWindow". 
    ///
    ///     This class is also responsible for "hookingup" to the F2 Command on VS. 
    ///
    /// </devdoc>
    internal class ToolStripEditorManager {
        // 
        // Local copy of BehaviorService so that we can add the Insitu Editor to
        // the AdornerWindow. 
        // 
        private BehaviorService             behaviorService;
 
        //
        // Component for this InSitu Editor... (this is a ToolStripItem)
        // that wants to go into InSitu
        // 
        private IDesignerHost               designerHost;
 
        // 
        // This is always ToolStrip
        // 
        private IComponent                  comp;

        //
        // The current Bounds of the Insitu Editor on Adorner Window.. 
        // These are required for invalidation.
        // 
        private Rectangle                   lastKnownEditorBounds = Rectangle.Empty; 

        // 
        // The encapsulated Editor.
        //
        private ToolStripEditorControl         editor;
 
        //
        // Actual ToolStripEditor for the current ToolStripItem. 
        // 
        private ToolStripTemplateNode                editorUI;
 
        //
        // The Current ToolStripItem that needs to go into the InSitu Mode.
        // We keep a local copy so that when a new item comes in, we can "ROLLBACK"
        // the existing edit. 
        //
        private ToolStripItem                  currentItem; 
 
        //
        // The designer for current ToolStripItem. 
        //
        private ToolStripItemDesigner       itemDesigner;

 
        //
        // Constructor 
        // 
        /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager.ToolStripEditorManager"]/*' />
        public ToolStripEditorManager(IComponent comp) { 

            // get the parent
            this.comp = comp;
 
            this.behaviorService = (BehaviorService)comp.Site.GetService(typeof(BehaviorService));
            this.designerHost = (IDesignerHost)comp.Site.GetService(typeof(IDesignerHost)); 
 
        }
 

        ////////////////////////////////////////////////////////////////////////////////////
        ////                                                                            ////
        ////                          Methods                                           //// 
        ////                                                                            ////
        //////////////////////////////////////////////////////////////////////////////////// 
 
        /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager.ActivateEditor"]/*' />
        /// <devdoc> 
        ///     Activates the editor for the given item.If there's still an editor around
        ///     for the previous-edited item, it is deactivated.
        ///     Pass in 'null' to deactivate and remove the current editor, if any.
        /// </devdoc> 
        /// <internalonly/>
        internal void ActivateEditor(ToolStripItem item, bool clicked) { 
 
            if (item != currentItem) {
 
                // Remove old editor
                //
                if (editor != null ) {
                    behaviorService.AdornerWindowControl.Controls.Remove(editor); 
                    behaviorService.Invalidate(editor.Bounds);
                    editorUI = null; 
                    editor = null; 
                    currentItem = null;
                    itemDesigner.IsEditorActive = false; 

                    // Show the previously edited glyph
                    if (currentItem != null) {
                        currentItem = null; 
                    }
 
                } 
                if (item != null) {
 
                    // Add new editor from the item...
                    //
                    currentItem = item;
                    if (designerHost != null) { 
                        itemDesigner = (ToolStripItemDesigner)designerHost.GetDesigner(currentItem);
                    } 
                    editorUI = (ToolStripTemplateNode)itemDesigner.Editor; 

                    // If we got an editor, position and focus it. 
                    //
                    if (editorUI != null) {

                        // Hide this glyph while it's being edited 
                        //
                        itemDesigner.IsEditorActive = true; 
                        editor = new ToolStripEditorControl(editorUI.EditorToolStrip, editorUI.Bounds); 
                        behaviorService.AdornerWindowControl.Controls.Add(editor);
                        lastKnownEditorBounds = editor.Bounds; 

                        editor.BringToFront();
                        // this is important since the ToolStripEditorControl listens
                        // to textchanged messages from TextBox. 

                        editorUI.ignoreFirstKeyUp = true; 
 						// Select the Editor... 
                        // Put Text and Select it ...
                        editorUI.FocusEditor(currentItem); 
                    }
                }

            } 
        }
 
        /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager.CloseManager"]/*' /> 
        /// <devdoc>
        ///     This will remove the Command for F2. 
        /// </devdoc>
        /// <internalonly/>
        internal void CloseManager()
        { 
        }
 
        /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager.OnKeyEdit"]/*' /> 
        /// <devdoc>
        ///      This LISTENs to the Editor Resize for resizing the Insitu edit on 
        ///      the Adorner Window ... CURRENTLY DISABLED.
        /// </devdoc>
        private void OnEditorResize(object sender, EventArgs e) {
            // THIS IS CURRENTLY DISABLE !!!!! 
            // TO DO !! SHOULD WE SUPPORT AUTOSIZED INSITU ?????
            behaviorService.Invalidate(lastKnownEditorBounds); 
            if (editor != null) { 
                lastKnownEditorBounds = editor.Bounds;
            } 

        }

        //  --------------------------------------------------------------------------------- 
        //  Private Class Implemented for InSitu Editor.
        //  This class just Wraps the ToolStripEditor from the TemplateNode and puts it in 
        //  a Panel. 
        //
        //----------------------------------------------------------------------------------- 
        private class ToolStripEditorControl : Panel {

            private Control wrappedEditor;
            private Rectangle bounds; 

            [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
            public ToolStripEditorControl(Control editorToolStrip, Rectangle bounds) { 
                this.wrappedEditor = editorToolStrip;
                this.bounds = bounds; 
                this.wrappedEditor.Resize += new EventHandler(OnWrappedEditorResize);
                this.Controls.Add(editorToolStrip);

                this.Location = new Point(bounds.X, bounds.Y); 
                this.Text = "InSituEditorWrapper";
                UpdateSize(); 
            } 

            private void OnWrappedEditorResize(object sender, EventArgs e) { 
                //UpdateSize();
            }

            private void UpdateSize() { 
                this.Size = new Size(wrappedEditor.Size.Width, wrappedEditor.Size.Height);
            } 
 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripEditorManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing; 
    using System.Design;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.Design.Behavior; 
    using System.Collections;
 
 
    /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager"]/*' />
    /// <devdoc> 
    ///     This internal Class is used by all TOPLEVEL ToolStripItems to show the InSitu Editor.
    ///
    ///     When the ToolStripItem receives the MouseDown on its Glyph it calls the "ActivateEditor"
    ///     Function on this EditorManager. 
    ///
    ///     The ActivateEditor Function checks for any existing "EDITOR" active, closes that down 
    ///     and now opens the new editor on the "AdornerWindow". 
    ///
    ///     This class is also responsible for "hookingup" to the F2 Command on VS. 
    ///
    /// </devdoc>
    internal class ToolStripEditorManager {
        // 
        // Local copy of BehaviorService so that we can add the Insitu Editor to
        // the AdornerWindow. 
        // 
        private BehaviorService             behaviorService;
 
        //
        // Component for this InSitu Editor... (this is a ToolStripItem)
        // that wants to go into InSitu
        // 
        private IDesignerHost               designerHost;
 
        // 
        // This is always ToolStrip
        // 
        private IComponent                  comp;

        //
        // The current Bounds of the Insitu Editor on Adorner Window.. 
        // These are required for invalidation.
        // 
        private Rectangle                   lastKnownEditorBounds = Rectangle.Empty; 

        // 
        // The encapsulated Editor.
        //
        private ToolStripEditorControl         editor;
 
        //
        // Actual ToolStripEditor for the current ToolStripItem. 
        // 
        private ToolStripTemplateNode                editorUI;
 
        //
        // The Current ToolStripItem that needs to go into the InSitu Mode.
        // We keep a local copy so that when a new item comes in, we can "ROLLBACK"
        // the existing edit. 
        //
        private ToolStripItem                  currentItem; 
 
        //
        // The designer for current ToolStripItem. 
        //
        private ToolStripItemDesigner       itemDesigner;

 
        //
        // Constructor 
        // 
        /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager.ToolStripEditorManager"]/*' />
        public ToolStripEditorManager(IComponent comp) { 

            // get the parent
            this.comp = comp;
 
            this.behaviorService = (BehaviorService)comp.Site.GetService(typeof(BehaviorService));
            this.designerHost = (IDesignerHost)comp.Site.GetService(typeof(IDesignerHost)); 
 
        }
 

        ////////////////////////////////////////////////////////////////////////////////////
        ////                                                                            ////
        ////                          Methods                                           //// 
        ////                                                                            ////
        //////////////////////////////////////////////////////////////////////////////////// 
 
        /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager.ActivateEditor"]/*' />
        /// <devdoc> 
        ///     Activates the editor for the given item.If there's still an editor around
        ///     for the previous-edited item, it is deactivated.
        ///     Pass in 'null' to deactivate and remove the current editor, if any.
        /// </devdoc> 
        /// <internalonly/>
        internal void ActivateEditor(ToolStripItem item, bool clicked) { 
 
            if (item != currentItem) {
 
                // Remove old editor
                //
                if (editor != null ) {
                    behaviorService.AdornerWindowControl.Controls.Remove(editor); 
                    behaviorService.Invalidate(editor.Bounds);
                    editorUI = null; 
                    editor = null; 
                    currentItem = null;
                    itemDesigner.IsEditorActive = false; 

                    // Show the previously edited glyph
                    if (currentItem != null) {
                        currentItem = null; 
                    }
 
                } 
                if (item != null) {
 
                    // Add new editor from the item...
                    //
                    currentItem = item;
                    if (designerHost != null) { 
                        itemDesigner = (ToolStripItemDesigner)designerHost.GetDesigner(currentItem);
                    } 
                    editorUI = (ToolStripTemplateNode)itemDesigner.Editor; 

                    // If we got an editor, position and focus it. 
                    //
                    if (editorUI != null) {

                        // Hide this glyph while it's being edited 
                        //
                        itemDesigner.IsEditorActive = true; 
                        editor = new ToolStripEditorControl(editorUI.EditorToolStrip, editorUI.Bounds); 
                        behaviorService.AdornerWindowControl.Controls.Add(editor);
                        lastKnownEditorBounds = editor.Bounds; 

                        editor.BringToFront();
                        // this is important since the ToolStripEditorControl listens
                        // to textchanged messages from TextBox. 

                        editorUI.ignoreFirstKeyUp = true; 
 						// Select the Editor... 
                        // Put Text and Select it ...
                        editorUI.FocusEditor(currentItem); 
                    }
                }

            } 
        }
 
        /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager.CloseManager"]/*' /> 
        /// <devdoc>
        ///     This will remove the Command for F2. 
        /// </devdoc>
        /// <internalonly/>
        internal void CloseManager()
        { 
        }
 
        /// <include file='doc\ToolStripEditorManager.uex' path='docs/doc[@for="ToolStripEditorManager.OnKeyEdit"]/*' /> 
        /// <devdoc>
        ///      This LISTENs to the Editor Resize for resizing the Insitu edit on 
        ///      the Adorner Window ... CURRENTLY DISABLED.
        /// </devdoc>
        private void OnEditorResize(object sender, EventArgs e) {
            // THIS IS CURRENTLY DISABLE !!!!! 
            // TO DO !! SHOULD WE SUPPORT AUTOSIZED INSITU ?????
            behaviorService.Invalidate(lastKnownEditorBounds); 
            if (editor != null) { 
                lastKnownEditorBounds = editor.Bounds;
            } 

        }

        //  --------------------------------------------------------------------------------- 
        //  Private Class Implemented for InSitu Editor.
        //  This class just Wraps the ToolStripEditor from the TemplateNode and puts it in 
        //  a Panel. 
        //
        //----------------------------------------------------------------------------------- 
        private class ToolStripEditorControl : Panel {

            private Control wrappedEditor;
            private Rectangle bounds; 

            [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
            public ToolStripEditorControl(Control editorToolStrip, Rectangle bounds) { 
                this.wrappedEditor = editorToolStrip;
                this.bounds = bounds; 
                this.wrappedEditor.Resize += new EventHandler(OnWrappedEditorResize);
                this.Controls.Add(editorToolStrip);

                this.Location = new Point(bounds.X, bounds.Y); 
                this.Text = "InSituEditorWrapper";
                UpdateSize(); 
            } 

            private void OnWrappedEditorResize(object sender, EventArgs e) { 
                //UpdateSize();
            }

            private void UpdateSize() { 
                this.Size = new Size(wrappedEditor.Size.Width, wrappedEditor.Size.Height);
            } 
 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
