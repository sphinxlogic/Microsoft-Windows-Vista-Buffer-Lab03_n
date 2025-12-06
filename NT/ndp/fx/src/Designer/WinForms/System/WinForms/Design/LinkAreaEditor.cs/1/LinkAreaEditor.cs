//------------------------------------------------------------------------------ 
// <copyright file="LinkAreaEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
    using System.Runtime.Serialization.Formatters; 
    using System.Runtime.InteropServices;
 
    using System.Diagnostics;
    using System;
    using System.Design;
    using System.Security.Permissions; 
    using Microsoft.Win32;
    using System.Collections; 
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel; 

    /// <include file='doc\LinkAreaEditor.uex' path='docs/doc[@for="LinkAreaEditor"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       Provides an editor that can be used to visually select and configure the link area of a link 
    ///       label.
    ///    </para>
    /// </devdoc>
    internal class LinkAreaEditor : UITypeEditor { 

        private LinkAreaUI linkAreaUI; 
 
        /// <include file='doc\LinkAreaEditor.uex' path='docs/doc[@for="LinkAreaEditor.EditValue"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Edits the given object value using the editor style provided by
        ///       GetEditorStyle.
        ///    </para> 
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) { 
 
            Debug.Assert(provider != null, "No service provider; we cannot edit the value");
            if (provider != null) { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                IHelpService helpService = (IHelpService)provider.GetService(typeof(IHelpService));

                Debug.Assert(edSvc != null, "No editor service; we cannot edit the value"); 
                if (edSvc != null) {
                    if (linkAreaUI == null) { 
                        linkAreaUI = new LinkAreaUI(this, helpService); 
                    }
 
                    string text = string.Empty;
                    PropertyDescriptor prop = null;

                    if (context != null && context.Instance != null) { 
                        prop = TypeDescriptor.GetProperties(context.Instance)["Text"];
                        if (prop != null && prop.PropertyType == typeof(string)) { 
                            text = (string)prop.GetValue(context.Instance); 
                        }
                    } 

                    string originalText = text;
                    linkAreaUI.SampleText = text;
                    linkAreaUI.Start(edSvc, value); 

                    if (edSvc.ShowDialog(linkAreaUI) == DialogResult.OK) { 
                        value = linkAreaUI.Value; 

                        text = linkAreaUI.SampleText; 
                        if (!originalText.Equals(text) && prop != null && prop.PropertyType == typeof(string)) {
                            prop.SetValue(context.Instance, text);
                        }
 
                    }
 
                    linkAreaUI.End(); 
                }
            } 

            return value;
        }
 
        /// <include file='doc\LinkAreaEditor.uex' path='docs/doc[@for="LinkAreaEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets the editing style of the Edit method.
        ///    </para> 
        /// </devdoc>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        } 

        /// <include file='doc\LinkAreaEditor.uex' path='docs/doc[@for="LinkAreaEditor.LinkAreaUI"]/*' /> 
        /// <devdoc> 
        ///      Dialog box for the link area.
        /// </devdoc> 
        internal class LinkAreaUI : Form {
            private Label caption = new Label();
            private TextBox sampleEdit = new TextBox();
            private Button okButton = new Button(); 
            private Button cancelButton = new Button();
            private System.Windows.Forms.TableLayoutPanel okCancelTableLayoutPanel; 
            private LinkAreaEditor editor; 
            private IWindowsFormsEditorService edSvc;
            private object value; 
            private IHelpService helpService = null;

            public LinkAreaUI(LinkAreaEditor editor, IHelpService helpService) {
                this.editor = editor; 
                this.helpService = helpService;
                InitializeComponent(); 
            } 

            public string SampleText { 
                get {
                    return sampleEdit.Text;
                }
                set { 
                    sampleEdit.Text = value;
                    UpdateSelection(); 
                } 
            }
 
            public object Value {
                get {
                    return value;
                } 
            }
 
            public void End() { 
                edSvc = null;
                value = null; 
            }


           private void InitializeComponent() { 
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkAreaEditor));
                this.caption = new System.Windows.Forms.Label(); 
                this.sampleEdit = new System.Windows.Forms.TextBox(); 
                this.okButton = new System.Windows.Forms.Button();
                this.cancelButton = new System.Windows.Forms.Button(); 
                this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
                this.okCancelTableLayoutPanel.SuspendLayout();
                this.SuspendLayout();
                this.okButton.Click += new EventHandler(this.okButton_click); 
//
// caption 
// 
                resources.ApplyResources(this.caption, "caption");
                this.caption.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0); 
                this.caption.Name = "caption";
//
// sampleEdit
// 
                resources.ApplyResources(this.sampleEdit, "sampleEdit");
                this.sampleEdit.Margin = new System.Windows.Forms.Padding(3, 2, 3, 3); 
                this.sampleEdit.Name = "sampleEdit"; 
                this.sampleEdit.HideSelection = false;
                this.sampleEdit.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; 
//
// okButton
//
                resources.ApplyResources(this.okButton, "okButton"); 
                this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.okButton.Margin = new System.Windows.Forms.Padding(0, 0, 2, 0); 
                this.okButton.Name = "okButton"; 
//
// cancelButton 
//
                resources.ApplyResources(this.cancelButton, "cancelButton");
                this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0); 
                this.cancelButton.Name = "cancelButton";
// 
// okCancelTableLayoutPanel 
//
                resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel"); 
                this.okCancelTableLayoutPanel.ColumnCount = 2;
                this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0); 
                this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
                this.okCancelTableLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 1, 3, 3); 
                this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
                this.okCancelTableLayoutPanel.RowCount = 1;
                this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
//
// LinkAreaEditor
// 
                resources.ApplyResources(this, "$this");
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
                this.CancelButton = this.cancelButton; 
                this.Controls.Add(this.okCancelTableLayoutPanel);
                this.Controls.Add(this.sampleEdit); 
                this.Controls.Add(this.caption);
                this.HelpButton = true;
                this.MaximizeBox = false;
                this.MinimizeBox = false; 
                this.Name = "LinkAreaEditor";
                this.ShowIcon = false; 
                this.ShowInTaskbar = false; 
                this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.LinkAreaEditor_HelpButtonClicked);
                this.okCancelTableLayoutPanel.ResumeLayout(false); 
                this.okCancelTableLayoutPanel.PerformLayout();
                this.ResumeLayout(false);
                this.PerformLayout();
            } 

            private void okButton_click(object sender, EventArgs e) { 
                value = new LinkArea(sampleEdit.SelectionStart, sampleEdit.SelectionLength); 
            }
 
            private string HelpTopic {
                get {
                    return "net.ComponentModel.LinkAreaEditor";
                } 
            }
 
            /// <devdoc> 
            ///    <para>
            ///       Called when the help button is clicked. 
            ///    </para>
            /// </devdoc>
            private void ShowHelp() {
                if (helpService != null) { 
                    helpService.ShowHelpFromKeyword(HelpTopic);
                } 
                else { 
                    Debug.Fail("Unable to get IHelpService.");
                } 
            }

            private void LinkAreaEditor_HelpButtonClicked(object sender, CancelEventArgs e) {
                e.Cancel = true; 
                ShowHelp();
            } 
 
            public void Start(IWindowsFormsEditorService edSvc, object value) {
                this.edSvc = edSvc; 
                this.value = value;
                UpdateSelection();
                ActiveControl = sampleEdit;
            } 

            private void UpdateSelection() { 
                if (value is LinkArea) { 
                    LinkArea pt = (LinkArea)value;
                    try { 
                        sampleEdit.SelectionStart = pt.Start;
                        sampleEdit.SelectionLength = pt.Length;
                    }
                    catch(Exception ex) { 
                        if (ClientUtils.IsCriticalException(ex)) {
                            throw; 
                        } 
                    }
 
                    catch {
                        Debug.Fail("non-CLS compliant exception");
                    }
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LinkAreaEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
    using System.Runtime.Serialization.Formatters; 
    using System.Runtime.InteropServices;
 
    using System.Diagnostics;
    using System;
    using System.Design;
    using System.Security.Permissions; 
    using Microsoft.Win32;
    using System.Collections; 
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel; 

    /// <include file='doc\LinkAreaEditor.uex' path='docs/doc[@for="LinkAreaEditor"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       Provides an editor that can be used to visually select and configure the link area of a link 
    ///       label.
    ///    </para>
    /// </devdoc>
    internal class LinkAreaEditor : UITypeEditor { 

        private LinkAreaUI linkAreaUI; 
 
        /// <include file='doc\LinkAreaEditor.uex' path='docs/doc[@for="LinkAreaEditor.EditValue"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Edits the given object value using the editor style provided by
        ///       GetEditorStyle.
        ///    </para> 
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) { 
 
            Debug.Assert(provider != null, "No service provider; we cannot edit the value");
            if (provider != null) { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                IHelpService helpService = (IHelpService)provider.GetService(typeof(IHelpService));

                Debug.Assert(edSvc != null, "No editor service; we cannot edit the value"); 
                if (edSvc != null) {
                    if (linkAreaUI == null) { 
                        linkAreaUI = new LinkAreaUI(this, helpService); 
                    }
 
                    string text = string.Empty;
                    PropertyDescriptor prop = null;

                    if (context != null && context.Instance != null) { 
                        prop = TypeDescriptor.GetProperties(context.Instance)["Text"];
                        if (prop != null && prop.PropertyType == typeof(string)) { 
                            text = (string)prop.GetValue(context.Instance); 
                        }
                    } 

                    string originalText = text;
                    linkAreaUI.SampleText = text;
                    linkAreaUI.Start(edSvc, value); 

                    if (edSvc.ShowDialog(linkAreaUI) == DialogResult.OK) { 
                        value = linkAreaUI.Value; 

                        text = linkAreaUI.SampleText; 
                        if (!originalText.Equals(text) && prop != null && prop.PropertyType == typeof(string)) {
                            prop.SetValue(context.Instance, text);
                        }
 
                    }
 
                    linkAreaUI.End(); 
                }
            } 

            return value;
        }
 
        /// <include file='doc\LinkAreaEditor.uex' path='docs/doc[@for="LinkAreaEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets the editing style of the Edit method.
        ///    </para> 
        /// </devdoc>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        } 

        /// <include file='doc\LinkAreaEditor.uex' path='docs/doc[@for="LinkAreaEditor.LinkAreaUI"]/*' /> 
        /// <devdoc> 
        ///      Dialog box for the link area.
        /// </devdoc> 
        internal class LinkAreaUI : Form {
            private Label caption = new Label();
            private TextBox sampleEdit = new TextBox();
            private Button okButton = new Button(); 
            private Button cancelButton = new Button();
            private System.Windows.Forms.TableLayoutPanel okCancelTableLayoutPanel; 
            private LinkAreaEditor editor; 
            private IWindowsFormsEditorService edSvc;
            private object value; 
            private IHelpService helpService = null;

            public LinkAreaUI(LinkAreaEditor editor, IHelpService helpService) {
                this.editor = editor; 
                this.helpService = helpService;
                InitializeComponent(); 
            } 

            public string SampleText { 
                get {
                    return sampleEdit.Text;
                }
                set { 
                    sampleEdit.Text = value;
                    UpdateSelection(); 
                } 
            }
 
            public object Value {
                get {
                    return value;
                } 
            }
 
            public void End() { 
                edSvc = null;
                value = null; 
            }


           private void InitializeComponent() { 
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkAreaEditor));
                this.caption = new System.Windows.Forms.Label(); 
                this.sampleEdit = new System.Windows.Forms.TextBox(); 
                this.okButton = new System.Windows.Forms.Button();
                this.cancelButton = new System.Windows.Forms.Button(); 
                this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
                this.okCancelTableLayoutPanel.SuspendLayout();
                this.SuspendLayout();
                this.okButton.Click += new EventHandler(this.okButton_click); 
//
// caption 
// 
                resources.ApplyResources(this.caption, "caption");
                this.caption.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0); 
                this.caption.Name = "caption";
//
// sampleEdit
// 
                resources.ApplyResources(this.sampleEdit, "sampleEdit");
                this.sampleEdit.Margin = new System.Windows.Forms.Padding(3, 2, 3, 3); 
                this.sampleEdit.Name = "sampleEdit"; 
                this.sampleEdit.HideSelection = false;
                this.sampleEdit.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; 
//
// okButton
//
                resources.ApplyResources(this.okButton, "okButton"); 
                this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.okButton.Margin = new System.Windows.Forms.Padding(0, 0, 2, 0); 
                this.okButton.Name = "okButton"; 
//
// cancelButton 
//
                resources.ApplyResources(this.cancelButton, "cancelButton");
                this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0); 
                this.cancelButton.Name = "cancelButton";
// 
// okCancelTableLayoutPanel 
//
                resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel"); 
                this.okCancelTableLayoutPanel.ColumnCount = 2;
                this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0); 
                this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
                this.okCancelTableLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 1, 3, 3); 
                this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
                this.okCancelTableLayoutPanel.RowCount = 1;
                this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
//
// LinkAreaEditor
// 
                resources.ApplyResources(this, "$this");
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
                this.CancelButton = this.cancelButton; 
                this.Controls.Add(this.okCancelTableLayoutPanel);
                this.Controls.Add(this.sampleEdit); 
                this.Controls.Add(this.caption);
                this.HelpButton = true;
                this.MaximizeBox = false;
                this.MinimizeBox = false; 
                this.Name = "LinkAreaEditor";
                this.ShowIcon = false; 
                this.ShowInTaskbar = false; 
                this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.LinkAreaEditor_HelpButtonClicked);
                this.okCancelTableLayoutPanel.ResumeLayout(false); 
                this.okCancelTableLayoutPanel.PerformLayout();
                this.ResumeLayout(false);
                this.PerformLayout();
            } 

            private void okButton_click(object sender, EventArgs e) { 
                value = new LinkArea(sampleEdit.SelectionStart, sampleEdit.SelectionLength); 
            }
 
            private string HelpTopic {
                get {
                    return "net.ComponentModel.LinkAreaEditor";
                } 
            }
 
            /// <devdoc> 
            ///    <para>
            ///       Called when the help button is clicked. 
            ///    </para>
            /// </devdoc>
            private void ShowHelp() {
                if (helpService != null) { 
                    helpService.ShowHelpFromKeyword(HelpTopic);
                } 
                else { 
                    Debug.Fail("Unable to get IHelpService.");
                } 
            }

            private void LinkAreaEditor_HelpButtonClicked(object sender, CancelEventArgs e) {
                e.Cancel = true; 
                ShowHelp();
            } 
 
            public void Start(IWindowsFormsEditorService edSvc, object value) {
                this.edSvc = edSvc; 
                this.value = value;
                UpdateSelection();
                ActiveControl = sampleEdit;
            } 

            private void UpdateSelection() { 
                if (value is LinkArea) { 
                    LinkArea pt = (LinkArea)value;
                    try { 
                        sampleEdit.SelectionStart = pt.Start;
                        sampleEdit.SelectionLength = pt.Length;
                    }
                    catch(Exception ex) { 
                        if (ClientUtils.IsCriticalException(ex)) {
                            throw; 
                        } 
                    }
 
                    catch {
                        Debug.Fail("non-CLS compliant exception");
                    }
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
