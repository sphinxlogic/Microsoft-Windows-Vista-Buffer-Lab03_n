//------------------------------------------------------------------------------ 
// <copyright file="ShortcutKeysEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel; 
    using System.Windows.Forms.Design; 

    /// <internalonly/> 
    /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor"]/*' />
    /// <devdoc>
    ///     Provides an editor for picking shortcut keys.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")] 
    public class ShortcutKeysEditor : UITypeEditor 
    {
        private ShortcutKeysUI shortcutKeysUI; 

        /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.EditValue"]/*' />
        /// <devdoc>
        ///     Edits the given object value using the editor style provided by ShortcutKeysEditor.GetEditStyle. 
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) 
        { 
            if (provider != null)
            { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null)
                {
                    if (this.shortcutKeysUI == null) 
                    {
                        this.shortcutKeysUI = new ShortcutKeysUI(this); 
                    } 
                    this.shortcutKeysUI.Start(edSvc, value);
                    edSvc.DropDownControl(this.shortcutKeysUI); 

                    if (this.shortcutKeysUI.Value != null)
                    {
                        value = this.shortcutKeysUI.Value; 
                    }
                    this.shortcutKeysUI.End(); 
                } 
            }
            return value; 
        }

        /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///     Gets the editing style of the Edit method. If the method
        ///     is not supported, this will return UITypeEditorEditStyle.None. 
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        { 
            return UITypeEditorEditStyle.DropDown;
        }

        /// <devdoc> 
        ///      Editor UI for the shortcut keys editor.
        /// </devdoc> 
        private class ShortcutKeysUI : UserControl 
        {
            private ShortcutKeysEditor editor; 
            private IWindowsFormsEditorService edSvc;
            private object originalValue, currentValue;
            private TypeConverter keysConverter;
            private Keys unknownKeyCode; 
            private bool updateCurrentValue;
            private TableLayoutPanel tlpOuter; 
            private TableLayoutPanel tlpInner; 
            private Label lblModifiers;
            private Label lblKey; 
            private CheckBox chkCtrl;
            private CheckBox chkAlt;
            private CheckBox chkShift;
            private ComboBox cmbKey; 
            private Button btnReset;
 
            /// <devdoc> 
            ///     Array of keys that are present in the drop down list of the combo box.
            /// </devdoc> 
            private static Keys[] validKeys =
            {
                Keys.A, Keys.B, Keys.C, Keys.D, Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9,
                Keys.Delete, Keys.Down, Keys.E, Keys.End, Keys.F, Keys.F1, Keys.F10, Keys.F11, Keys.F12, Keys.F13, Keys.F14, Keys.F15, 
                Keys.F16, Keys.F17, Keys.F18, Keys.F19, Keys.F2, Keys.F20, Keys.F21, Keys.F22, Keys.F23, Keys.F24, Keys.F3, Keys.F4,
                Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.G, Keys.H, Keys.I, Keys.Insert, Keys.J, Keys.K, Keys.L, Keys.Left, 
                Keys.M, Keys.N, Keys.NumLock, Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, 
                Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9, Keys.O, Keys.OemBackslash, Keys.OemClear, Keys.OemCloseBrackets,
                Keys.Oemcomma, Keys.OemMinus, Keys.OemOpenBrackets, Keys.OemPeriod, Keys.OemPipe, Keys.Oemplus, Keys.OemQuestion, 
                Keys.OemQuotes, Keys.OemSemicolon, Keys.Oemtilde, Keys.P, Keys.Pause, Keys.Q, Keys.R, Keys.Right, Keys.S, Keys.Space,
                Keys.T, Keys.Tab, Keys.U, Keys.Up, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z
            };
 
            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.ShortcutKeysUI"]/*' />
            public ShortcutKeysUI(ShortcutKeysEditor editor) 
            { 
                this.editor = editor;
                this.keysConverter = null; 
                End();
                InitializeComponent();

#if DEBUG 
                // Looking for duplicates in validKeys
                int keyCount = validKeys.Length; 
                for (int key1 = 0; key1 < keyCount-1; key1++) 
                {
                    for (int key2 = key1+1; key2 < keyCount; key2++) 
                    {
                        Debug.Assert((int) validKeys[key1] != (int) validKeys[key2]);
                    }
                } 
#endif
            } 
 
            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.EditorService"]/*' />
            /// <devdoc> 
            ///      Allows someone else to close our dropdown.
            /// </devdoc>
            // Can be called through reflection.
            public IWindowsFormsEditorService EditorService 
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
                get 
                {
                    return this.edSvc; 
                }
            }

            /// <devdoc> 
            ///      Returns the Keys' type converter.
            /// </devdoc> 
            private TypeConverter KeysConverter 
            {
                get 
                {
                    if (this.keysConverter == null)
                    {
                        this.keysConverter = TypeDescriptor.GetConverter(typeof(Keys)); 
                    }
                    Debug.Assert(this.keysConverter != null); 
                    return this.keysConverter; 
                }
            } 

            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.Value"]/*' />
            /// <devdoc>
            ///      Returns the selected keys. If only modifers were selected, we return Keys.None. 
            /// </devdoc>
            public object Value 
            { 
                get
                { 
                    if (((Keys) this.currentValue & Keys.KeyCode) == 0)
                    {
                        return Keys.None;
                    } 
                    return this.currentValue;
                } 
            } 

            /// <devdoc> 
            ///      Triggered when the user clicks the Reset button. The value is set to Keys.None
            /// </devdoc>
            private void btnReset_Click(object sender, System.EventArgs e)
            { 
                this.chkCtrl.Checked = false;
                this.chkAlt.Checked = false; 
                this.chkShift.Checked = false; 
                this.cmbKey.SelectedIndex = -1;
            } 

            private void chkModifier_CheckedChanged(object sender, System.EventArgs e)
            {
                UpdateCurrentValue(); 
            }
 
            private void cmbKey_SelectedIndexChanged(object sender, System.EventArgs e) 
            {
                UpdateCurrentValue(); 
            }

            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.End"]/*' />
            public void End() 
            {
                this.edSvc = null; 
                this.originalValue = null; 
                this.currentValue = null;
                this.updateCurrentValue = false; 
                if (this.unknownKeyCode != Keys.None)
                {
                    this.cmbKey.Items.RemoveAt(0);
                    this.unknownKeyCode = Keys.None; 
                }
            } 
 
            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.InitializeComponent"]/*' />
            private void InitializeComponent() 
            {
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShortcutKeysEditor));

                this.tlpOuter = new System.Windows.Forms.TableLayoutPanel(); 
                this.lblModifiers = new System.Windows.Forms.Label();
                this.chkCtrl = new System.Windows.Forms.CheckBox(); 
                this.chkAlt = new System.Windows.Forms.CheckBox(); 
                this.chkShift = new System.Windows.Forms.CheckBox();
                this.tlpInner = new System.Windows.Forms.TableLayoutPanel(); 
                this.lblKey = new System.Windows.Forms.Label();
                this.cmbKey = new System.Windows.Forms.ComboBox();
                this.btnReset = new System.Windows.Forms.Button();
                this.tlpOuter.SuspendLayout(); 
                this.tlpInner.SuspendLayout();
                this.SuspendLayout(); 
 
                //
                // tlpOuter 
                //
                resources.ApplyResources(this.tlpOuter, "tlpOuter");
                this.tlpOuter.ColumnCount = 3;
                this.tlpOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.tlpOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
                this.tlpOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.tlpOuter.Controls.Add(this.lblModifiers, 0, 0); 
                this.tlpOuter.Controls.Add(this.chkCtrl, 0, 1);
                this.tlpOuter.Controls.Add(this.chkShift, 1, 1); 
                this.tlpOuter.Controls.Add(this.chkAlt, 2, 1);
                this.tlpOuter.Name = "tlpOuter";
                this.tlpOuter.RowCount = 2;
                this.tlpOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.Absolute, 20F)); 
                this.tlpOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.Absolute, 24F));
 
                // 
                // lblModifiers
                // 
                resources.ApplyResources(this.lblModifiers, "lblModifiers");
                this.tlpOuter.SetColumnSpan(this.lblModifiers, 3);
                this.lblModifiers.Name = "lblModifiers";
 
                //
                // chkCtrl 
                // 
                resources.ApplyResources(this.chkCtrl, "chkCtrl");
                this.chkCtrl.Name = "chkCtrl"; 
                // this margin setting makes this control left-align with cmbKey and indented from lblModifiers/lblKey
                this.chkCtrl.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);

                this.chkCtrl.CheckedChanged += new System.EventHandler(this.chkModifier_CheckedChanged); 

                // 
                // chkAlt 
                //
                resources.ApplyResources(this.chkAlt, "chkAlt"); 
                this.chkAlt.Name = "chkAlt";

                this.chkAlt.CheckedChanged += new System.EventHandler(this.chkModifier_CheckedChanged);
 
                //
                // chkShift 
                // 
                resources.ApplyResources(this.chkShift, "chkShift");
                this.chkShift.Name = "chkShift"; 

                this.chkShift.CheckedChanged += new System.EventHandler(this.chkModifier_CheckedChanged);

                // 
                // tlpInner
                // 
                resources.ApplyResources(this.tlpInner, "tlpInner"); 
                this.tlpInner.ColumnCount = 2;
                this.tlpInner.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(SizeType.AutoSize)); 
                this.tlpInner.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(SizeType.AutoSize));
                this.tlpInner.Controls.Add(this.lblKey, 0, 0);
                this.tlpInner.Controls.Add(this.cmbKey, 0, 1);
                this.tlpInner.Controls.Add(this.btnReset, 1, 1); 
                this.tlpInner.Name = "tlpInner";
                this.tlpInner.RowCount = 2; 
                this.tlpInner.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.Absolute, 20F)); 
                this.tlpInner.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.AutoSize));
 
                //
                // lblKey
                //
                resources.ApplyResources(this.lblKey, "lblKey"); 
                this.tlpInner.SetColumnSpan(this.lblKey, 2);
                this.lblKey.Name = "lblKey"; 
 
                //
                // cmbKey 
                //
                resources.ApplyResources(this.cmbKey, "cmbKey");
                this.cmbKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                this.cmbKey.Name = "cmbKey"; 
                // this margin setting makes this control align with chkCtrl and indented from lblModifiers/lblKey
                // the top margin makes the combobox and btnReset align properly 
                this.cmbKey.Margin = new System.Windows.Forms.Padding(9, 4, 3, 3); 
                this.cmbKey.Padding = cmbKey.Margin;
 
                foreach (Keys keyCode in validKeys)
                {
                    this.cmbKey.Items.Add(this.KeysConverter.ConvertToString(keyCode));
                } 
                this.cmbKey.SelectedIndexChanged += new System.EventHandler(this.cmbKey_SelectedIndexChanged);
 
                // 
                // btnReset
                // 
                resources.ApplyResources(this.btnReset, "btnReset");
                this.btnReset.Name = "btnReset";

                this.btnReset.Click += new System.EventHandler(this.btnReset_Click); 

                resources.ApplyResources(this, "$this"); 
                this.Controls.AddRange(new Control[]{this.tlpInner, this.tlpOuter}); 
                this.Name = "ShortcutKeysUI";
                this.Padding = new System.Windows.Forms.Padding(4); 

                this.tlpOuter.ResumeLayout(false);
                this.tlpOuter.PerformLayout();
                this.tlpInner.ResumeLayout(false); 
                this.tlpInner.PerformLayout();
                this.ResumeLayout(false); 
                this.PerformLayout(); 
            }
 
            /// <devdoc>
            ///      Returns True if the given key is part of the valid keys array.
            /// </devdoc>
            private static bool IsValidKey(Keys keyCode) 
            {
                Debug.Assert((keyCode & Keys.KeyCode) == keyCode); 
                foreach (Keys validKeyCode in validKeys) 
                {
                    if (validKeyCode == keyCode) 
                    {
                        return true;
                    }
                } 
                return false;
            } 
 
            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.OnGotFocus"]/*' />
            /// <devdoc> 
            ///      The Ctrl checkbox gets the focus by default.
            /// </devdoc>
            protected override void OnGotFocus(EventArgs e)
            { 
                base.OnGotFocus(e);
                this.chkCtrl.Focus(); 
            } 

            /// <devdoc> 
            ///      Fix keyboard navigation and handle escape key
            /// </devdoc>
            protected override bool ProcessDialogKey(Keys keyData)
            { 
                Keys keyCode = keyData & Keys.KeyCode;
                Keys keyModifiers = keyData & Keys.Modifiers; 
                switch (keyCode) 
                {
                    // [....]: We shouldn't have to handle this. Could be a bug in the table layout panel. Check it out. 
                    case Keys.Tab:
                        if (keyModifiers == Keys.Shift &&
                            this.chkCtrl.Focused)
                        { 
                            this.btnReset.Focus();
                            return true; 
                        } 
                        break;
 
                    case Keys.Left:
                        if ((keyModifiers & (Keys.Control | Keys.Alt)) == 0)
                        {
                            if (this.chkCtrl.Focused) 
                            {
                                this.btnReset.Focus(); 
                                return true; 
                            }
                            /* [....]: Does not get hit 
                            else if (this.cmbKey.Focused)
                            {
                                this.chkShift.Focus();
                                return true; 
                            }
                            */ 
                        } 
                        break;
 
                    case Keys.Right:
                        if ((keyModifiers & (Keys.Control | Keys.Alt)) == 0)
                        {
                            if (this.chkShift.Focused) 
                            {
                                this.cmbKey.Focus(); 
                                return true; 
                            }
                            else if (this.btnReset.Focused) 
                            {
                                this.chkCtrl.Focus();
                                return true;
                            } 
                        }
                        break; 
 
                    case Keys.Escape:
                        if (!this.cmbKey.Focused || 
                            (keyModifiers & (Keys.Control | Keys.Alt)) != 0 ||
                            !this.cmbKey.DroppedDown)
                        {
                            this.currentValue = this.originalValue; 
                        }
                        break; 
                } 
                return base.ProcessDialogKey(keyData);
            } 

            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.Start"]/*' />
            /// <devdoc>
            ///      Triggered whenever the user drops down the editor. 
            /// </devdoc>
            public void Start(IWindowsFormsEditorService edSvc, object value) 
            { 
                Debug.Assert(edSvc != null);
                Debug.Assert(value is Keys); 
                Debug.Assert(!this.updateCurrentValue);
                this.edSvc = edSvc;
                this.originalValue = this.currentValue = value;
 
                Keys keys = (Keys) value;
                this.chkCtrl.Checked = ((keys & Keys.Control) != 0); 
                this.chkAlt.Checked = ((keys & Keys.Alt) != 0); 
                this.chkShift.Checked = ((keys & Keys.Shift) != 0);
 
                Keys keyCode = keys & Keys.KeyCode;
                if (keyCode == Keys.None)
                {
                    this.cmbKey.SelectedIndex = -1; 
                }
                else if (IsValidKey(keyCode)) 
                { 
                    this.cmbKey.SelectedItem = this.KeysConverter.ConvertToString(keyCode);
                } 
                else
                {
                    this.cmbKey.Items.Insert(0, SR.GetString(SR.ShortcutKeys_InvalidKey));
                    this.cmbKey.SelectedIndex = 0; 
                    this.unknownKeyCode = keyCode;
                } 
                this.updateCurrentValue = true; 
            }
 
            /// <devdoc>
            ///      Update the current value based on the state of the UI controls.
            /// </devdoc>
            private void UpdateCurrentValue() 
            {
                if (!this.updateCurrentValue) 
                { 
                    return;
                } 
                int cmbKeySelectedIndex = this.cmbKey.SelectedIndex;
                Keys valueKeys = Keys.None;
                if (this.chkCtrl.Checked)
                { 
                    valueKeys |= Keys.Control;
                } 
                if (this.chkAlt.Checked) 
                {
                    valueKeys |= Keys.Alt; 
                }
                if (this.chkShift.Checked)
                {
                    valueKeys |= Keys.Shift; 
                }
                if (this.unknownKeyCode != Keys.None && cmbKeySelectedIndex == 0) 
                { 
                    valueKeys |= this.unknownKeyCode;
                } 
                else if (cmbKeySelectedIndex != -1)
                {
                    valueKeys |= validKeys[this.unknownKeyCode == Keys.None ? cmbKeySelectedIndex : cmbKeySelectedIndex-1];
                } 
                this.currentValue = valueKeys;
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ShortcutKeysEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel; 
    using System.Windows.Forms.Design; 

    /// <internalonly/> 
    /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor"]/*' />
    /// <devdoc>
    ///     Provides an editor for picking shortcut keys.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")] 
    public class ShortcutKeysEditor : UITypeEditor 
    {
        private ShortcutKeysUI shortcutKeysUI; 

        /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.EditValue"]/*' />
        /// <devdoc>
        ///     Edits the given object value using the editor style provided by ShortcutKeysEditor.GetEditStyle. 
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) 
        { 
            if (provider != null)
            { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null)
                {
                    if (this.shortcutKeysUI == null) 
                    {
                        this.shortcutKeysUI = new ShortcutKeysUI(this); 
                    } 
                    this.shortcutKeysUI.Start(edSvc, value);
                    edSvc.DropDownControl(this.shortcutKeysUI); 

                    if (this.shortcutKeysUI.Value != null)
                    {
                        value = this.shortcutKeysUI.Value; 
                    }
                    this.shortcutKeysUI.End(); 
                } 
            }
            return value; 
        }

        /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///     Gets the editing style of the Edit method. If the method
        ///     is not supported, this will return UITypeEditorEditStyle.None. 
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        { 
            return UITypeEditorEditStyle.DropDown;
        }

        /// <devdoc> 
        ///      Editor UI for the shortcut keys editor.
        /// </devdoc> 
        private class ShortcutKeysUI : UserControl 
        {
            private ShortcutKeysEditor editor; 
            private IWindowsFormsEditorService edSvc;
            private object originalValue, currentValue;
            private TypeConverter keysConverter;
            private Keys unknownKeyCode; 
            private bool updateCurrentValue;
            private TableLayoutPanel tlpOuter; 
            private TableLayoutPanel tlpInner; 
            private Label lblModifiers;
            private Label lblKey; 
            private CheckBox chkCtrl;
            private CheckBox chkAlt;
            private CheckBox chkShift;
            private ComboBox cmbKey; 
            private Button btnReset;
 
            /// <devdoc> 
            ///     Array of keys that are present in the drop down list of the combo box.
            /// </devdoc> 
            private static Keys[] validKeys =
            {
                Keys.A, Keys.B, Keys.C, Keys.D, Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9,
                Keys.Delete, Keys.Down, Keys.E, Keys.End, Keys.F, Keys.F1, Keys.F10, Keys.F11, Keys.F12, Keys.F13, Keys.F14, Keys.F15, 
                Keys.F16, Keys.F17, Keys.F18, Keys.F19, Keys.F2, Keys.F20, Keys.F21, Keys.F22, Keys.F23, Keys.F24, Keys.F3, Keys.F4,
                Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.G, Keys.H, Keys.I, Keys.Insert, Keys.J, Keys.K, Keys.L, Keys.Left, 
                Keys.M, Keys.N, Keys.NumLock, Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, 
                Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9, Keys.O, Keys.OemBackslash, Keys.OemClear, Keys.OemCloseBrackets,
                Keys.Oemcomma, Keys.OemMinus, Keys.OemOpenBrackets, Keys.OemPeriod, Keys.OemPipe, Keys.Oemplus, Keys.OemQuestion, 
                Keys.OemQuotes, Keys.OemSemicolon, Keys.Oemtilde, Keys.P, Keys.Pause, Keys.Q, Keys.R, Keys.Right, Keys.S, Keys.Space,
                Keys.T, Keys.Tab, Keys.U, Keys.Up, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z
            };
 
            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.ShortcutKeysUI"]/*' />
            public ShortcutKeysUI(ShortcutKeysEditor editor) 
            { 
                this.editor = editor;
                this.keysConverter = null; 
                End();
                InitializeComponent();

#if DEBUG 
                // Looking for duplicates in validKeys
                int keyCount = validKeys.Length; 
                for (int key1 = 0; key1 < keyCount-1; key1++) 
                {
                    for (int key2 = key1+1; key2 < keyCount; key2++) 
                    {
                        Debug.Assert((int) validKeys[key1] != (int) validKeys[key2]);
                    }
                } 
#endif
            } 
 
            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.EditorService"]/*' />
            /// <devdoc> 
            ///      Allows someone else to close our dropdown.
            /// </devdoc>
            // Can be called through reflection.
            public IWindowsFormsEditorService EditorService 
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
                get 
                {
                    return this.edSvc; 
                }
            }

            /// <devdoc> 
            ///      Returns the Keys' type converter.
            /// </devdoc> 
            private TypeConverter KeysConverter 
            {
                get 
                {
                    if (this.keysConverter == null)
                    {
                        this.keysConverter = TypeDescriptor.GetConverter(typeof(Keys)); 
                    }
                    Debug.Assert(this.keysConverter != null); 
                    return this.keysConverter; 
                }
            } 

            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.Value"]/*' />
            /// <devdoc>
            ///      Returns the selected keys. If only modifers were selected, we return Keys.None. 
            /// </devdoc>
            public object Value 
            { 
                get
                { 
                    if (((Keys) this.currentValue & Keys.KeyCode) == 0)
                    {
                        return Keys.None;
                    } 
                    return this.currentValue;
                } 
            } 

            /// <devdoc> 
            ///      Triggered when the user clicks the Reset button. The value is set to Keys.None
            /// </devdoc>
            private void btnReset_Click(object sender, System.EventArgs e)
            { 
                this.chkCtrl.Checked = false;
                this.chkAlt.Checked = false; 
                this.chkShift.Checked = false; 
                this.cmbKey.SelectedIndex = -1;
            } 

            private void chkModifier_CheckedChanged(object sender, System.EventArgs e)
            {
                UpdateCurrentValue(); 
            }
 
            private void cmbKey_SelectedIndexChanged(object sender, System.EventArgs e) 
            {
                UpdateCurrentValue(); 
            }

            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.End"]/*' />
            public void End() 
            {
                this.edSvc = null; 
                this.originalValue = null; 
                this.currentValue = null;
                this.updateCurrentValue = false; 
                if (this.unknownKeyCode != Keys.None)
                {
                    this.cmbKey.Items.RemoveAt(0);
                    this.unknownKeyCode = Keys.None; 
                }
            } 
 
            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.InitializeComponent"]/*' />
            private void InitializeComponent() 
            {
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShortcutKeysEditor));

                this.tlpOuter = new System.Windows.Forms.TableLayoutPanel(); 
                this.lblModifiers = new System.Windows.Forms.Label();
                this.chkCtrl = new System.Windows.Forms.CheckBox(); 
                this.chkAlt = new System.Windows.Forms.CheckBox(); 
                this.chkShift = new System.Windows.Forms.CheckBox();
                this.tlpInner = new System.Windows.Forms.TableLayoutPanel(); 
                this.lblKey = new System.Windows.Forms.Label();
                this.cmbKey = new System.Windows.Forms.ComboBox();
                this.btnReset = new System.Windows.Forms.Button();
                this.tlpOuter.SuspendLayout(); 
                this.tlpInner.SuspendLayout();
                this.SuspendLayout(); 
 
                //
                // tlpOuter 
                //
                resources.ApplyResources(this.tlpOuter, "tlpOuter");
                this.tlpOuter.ColumnCount = 3;
                this.tlpOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.tlpOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
                this.tlpOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
                this.tlpOuter.Controls.Add(this.lblModifiers, 0, 0); 
                this.tlpOuter.Controls.Add(this.chkCtrl, 0, 1);
                this.tlpOuter.Controls.Add(this.chkShift, 1, 1); 
                this.tlpOuter.Controls.Add(this.chkAlt, 2, 1);
                this.tlpOuter.Name = "tlpOuter";
                this.tlpOuter.RowCount = 2;
                this.tlpOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.Absolute, 20F)); 
                this.tlpOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.Absolute, 24F));
 
                // 
                // lblModifiers
                // 
                resources.ApplyResources(this.lblModifiers, "lblModifiers");
                this.tlpOuter.SetColumnSpan(this.lblModifiers, 3);
                this.lblModifiers.Name = "lblModifiers";
 
                //
                // chkCtrl 
                // 
                resources.ApplyResources(this.chkCtrl, "chkCtrl");
                this.chkCtrl.Name = "chkCtrl"; 
                // this margin setting makes this control left-align with cmbKey and indented from lblModifiers/lblKey
                this.chkCtrl.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);

                this.chkCtrl.CheckedChanged += new System.EventHandler(this.chkModifier_CheckedChanged); 

                // 
                // chkAlt 
                //
                resources.ApplyResources(this.chkAlt, "chkAlt"); 
                this.chkAlt.Name = "chkAlt";

                this.chkAlt.CheckedChanged += new System.EventHandler(this.chkModifier_CheckedChanged);
 
                //
                // chkShift 
                // 
                resources.ApplyResources(this.chkShift, "chkShift");
                this.chkShift.Name = "chkShift"; 

                this.chkShift.CheckedChanged += new System.EventHandler(this.chkModifier_CheckedChanged);

                // 
                // tlpInner
                // 
                resources.ApplyResources(this.tlpInner, "tlpInner"); 
                this.tlpInner.ColumnCount = 2;
                this.tlpInner.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(SizeType.AutoSize)); 
                this.tlpInner.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(SizeType.AutoSize));
                this.tlpInner.Controls.Add(this.lblKey, 0, 0);
                this.tlpInner.Controls.Add(this.cmbKey, 0, 1);
                this.tlpInner.Controls.Add(this.btnReset, 1, 1); 
                this.tlpInner.Name = "tlpInner";
                this.tlpInner.RowCount = 2; 
                this.tlpInner.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.Absolute, 20F)); 
                this.tlpInner.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.AutoSize));
 
                //
                // lblKey
                //
                resources.ApplyResources(this.lblKey, "lblKey"); 
                this.tlpInner.SetColumnSpan(this.lblKey, 2);
                this.lblKey.Name = "lblKey"; 
 
                //
                // cmbKey 
                //
                resources.ApplyResources(this.cmbKey, "cmbKey");
                this.cmbKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                this.cmbKey.Name = "cmbKey"; 
                // this margin setting makes this control align with chkCtrl and indented from lblModifiers/lblKey
                // the top margin makes the combobox and btnReset align properly 
                this.cmbKey.Margin = new System.Windows.Forms.Padding(9, 4, 3, 3); 
                this.cmbKey.Padding = cmbKey.Margin;
 
                foreach (Keys keyCode in validKeys)
                {
                    this.cmbKey.Items.Add(this.KeysConverter.ConvertToString(keyCode));
                } 
                this.cmbKey.SelectedIndexChanged += new System.EventHandler(this.cmbKey_SelectedIndexChanged);
 
                // 
                // btnReset
                // 
                resources.ApplyResources(this.btnReset, "btnReset");
                this.btnReset.Name = "btnReset";

                this.btnReset.Click += new System.EventHandler(this.btnReset_Click); 

                resources.ApplyResources(this, "$this"); 
                this.Controls.AddRange(new Control[]{this.tlpInner, this.tlpOuter}); 
                this.Name = "ShortcutKeysUI";
                this.Padding = new System.Windows.Forms.Padding(4); 

                this.tlpOuter.ResumeLayout(false);
                this.tlpOuter.PerformLayout();
                this.tlpInner.ResumeLayout(false); 
                this.tlpInner.PerformLayout();
                this.ResumeLayout(false); 
                this.PerformLayout(); 
            }
 
            /// <devdoc>
            ///      Returns True if the given key is part of the valid keys array.
            /// </devdoc>
            private static bool IsValidKey(Keys keyCode) 
            {
                Debug.Assert((keyCode & Keys.KeyCode) == keyCode); 
                foreach (Keys validKeyCode in validKeys) 
                {
                    if (validKeyCode == keyCode) 
                    {
                        return true;
                    }
                } 
                return false;
            } 
 
            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.OnGotFocus"]/*' />
            /// <devdoc> 
            ///      The Ctrl checkbox gets the focus by default.
            /// </devdoc>
            protected override void OnGotFocus(EventArgs e)
            { 
                base.OnGotFocus(e);
                this.chkCtrl.Focus(); 
            } 

            /// <devdoc> 
            ///      Fix keyboard navigation and handle escape key
            /// </devdoc>
            protected override bool ProcessDialogKey(Keys keyData)
            { 
                Keys keyCode = keyData & Keys.KeyCode;
                Keys keyModifiers = keyData & Keys.Modifiers; 
                switch (keyCode) 
                {
                    // [....]: We shouldn't have to handle this. Could be a bug in the table layout panel. Check it out. 
                    case Keys.Tab:
                        if (keyModifiers == Keys.Shift &&
                            this.chkCtrl.Focused)
                        { 
                            this.btnReset.Focus();
                            return true; 
                        } 
                        break;
 
                    case Keys.Left:
                        if ((keyModifiers & (Keys.Control | Keys.Alt)) == 0)
                        {
                            if (this.chkCtrl.Focused) 
                            {
                                this.btnReset.Focus(); 
                                return true; 
                            }
                            /* [....]: Does not get hit 
                            else if (this.cmbKey.Focused)
                            {
                                this.chkShift.Focus();
                                return true; 
                            }
                            */ 
                        } 
                        break;
 
                    case Keys.Right:
                        if ((keyModifiers & (Keys.Control | Keys.Alt)) == 0)
                        {
                            if (this.chkShift.Focused) 
                            {
                                this.cmbKey.Focus(); 
                                return true; 
                            }
                            else if (this.btnReset.Focused) 
                            {
                                this.chkCtrl.Focus();
                                return true;
                            } 
                        }
                        break; 
 
                    case Keys.Escape:
                        if (!this.cmbKey.Focused || 
                            (keyModifiers & (Keys.Control | Keys.Alt)) != 0 ||
                            !this.cmbKey.DroppedDown)
                        {
                            this.currentValue = this.originalValue; 
                        }
                        break; 
                } 
                return base.ProcessDialogKey(keyData);
            } 

            /// <include file='doc\ShortcutKeysEditor.uex' path='docs/doc[@for="ShortcutKeysEditor.ShortcutKeysUI.Start"]/*' />
            /// <devdoc>
            ///      Triggered whenever the user drops down the editor. 
            /// </devdoc>
            public void Start(IWindowsFormsEditorService edSvc, object value) 
            { 
                Debug.Assert(edSvc != null);
                Debug.Assert(value is Keys); 
                Debug.Assert(!this.updateCurrentValue);
                this.edSvc = edSvc;
                this.originalValue = this.currentValue = value;
 
                Keys keys = (Keys) value;
                this.chkCtrl.Checked = ((keys & Keys.Control) != 0); 
                this.chkAlt.Checked = ((keys & Keys.Alt) != 0); 
                this.chkShift.Checked = ((keys & Keys.Shift) != 0);
 
                Keys keyCode = keys & Keys.KeyCode;
                if (keyCode == Keys.None)
                {
                    this.cmbKey.SelectedIndex = -1; 
                }
                else if (IsValidKey(keyCode)) 
                { 
                    this.cmbKey.SelectedItem = this.KeysConverter.ConvertToString(keyCode);
                } 
                else
                {
                    this.cmbKey.Items.Insert(0, SR.GetString(SR.ShortcutKeys_InvalidKey));
                    this.cmbKey.SelectedIndex = 0; 
                    this.unknownKeyCode = keyCode;
                } 
                this.updateCurrentValue = true; 
            }
 
            /// <devdoc>
            ///      Update the current value based on the state of the UI controls.
            /// </devdoc>
            private void UpdateCurrentValue() 
            {
                if (!this.updateCurrentValue) 
                { 
                    return;
                } 
                int cmbKeySelectedIndex = this.cmbKey.SelectedIndex;
                Keys valueKeys = Keys.None;
                if (this.chkCtrl.Checked)
                { 
                    valueKeys |= Keys.Control;
                } 
                if (this.chkAlt.Checked) 
                {
                    valueKeys |= Keys.Alt; 
                }
                if (this.chkShift.Checked)
                {
                    valueKeys |= Keys.Shift; 
                }
                if (this.unknownKeyCode != Keys.None && cmbKeySelectedIndex == 0) 
                { 
                    valueKeys |= this.unknownKeyCode;
                } 
                else if (cmbKeySelectedIndex != -1)
                {
                    valueKeys |= validKeys[this.unknownKeyCode == Keys.None ? cmbKeySelectedIndex : cmbKeySelectedIndex-1];
                } 
                this.currentValue = valueKeys;
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
