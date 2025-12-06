//------------------------------------------------------------------------------ 
// <copyright file="BorderSidesEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing.Design;
    using System.Windows.Forms; 
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design; 
 
    /// <internalonly/>
    /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor"]/*' /> 
    /// <devdoc>
    ///     Provides an editor for setting the ToolStripStatusLabel BorderSides property..
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class BorderSidesEditor : UITypeEditor
    { 
        private BorderSidesEditorUI borderSidesEditorUI; 

        /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.EditValue"]/*' /> 
        /// <devdoc>
        ///     Edits the given object value using the editor style provided by BorderSidesEditor.GetEditStyle.
        /// </devdoc>
 
        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        { 
            if (provider != null)
            {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null) 
                {
                    if (this.borderSidesEditorUI == null) 
                    { 
                        this.borderSidesEditorUI = new BorderSidesEditorUI(this);
                    } 
                    this.borderSidesEditorUI.Start(edSvc, value);
                    edSvc.DropDownControl(this.borderSidesEditorUI);

                    if (this.borderSidesEditorUI.Value != null) 
                    {
                        value = this.borderSidesEditorUI.Value; 
                    } 
                    this.borderSidesEditorUI.End();
                } 
            }
            return value;
        }
 
        /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///     Gets the editing style of the Edit method. If the method 
        ///     is not supported, this will return UITypeEditorEditStyle.None.
        /// </devdoc> 

        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        { 
            return UITypeEditorEditStyle.DropDown; 
        }
 
        /// <devdoc>
        ///      Editor UI for the BorderSides editor.
        /// </devdoc>
        private class BorderSidesEditorUI : UserControl 
        {
            private BorderSidesEditor editor; 
            private IWindowsFormsEditorService edSvc; 
            private object originalValue, currentValue;
 
            private bool updateCurrentValue = false;

            private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    		private System.Windows.Forms.CheckBox allCheckBox; 
    		private System.Windows.Forms.CheckBox noneCheckBox;
    		private System.Windows.Forms.CheckBox topCheckBox; 
    		private System.Windows.Forms.CheckBox bottomCheckBox; 
    		private System.Windows.Forms.CheckBox leftCheckBox;
    		private System.Windows.Forms.CheckBox rightCheckBox; 
            private System.Windows.Forms.Label splitterLabel;

            private bool allChecked = false;
            private bool noneChecked = false; 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.BorderSidesEditorUI"]/*' /> 
            public BorderSidesEditorUI(BorderSidesEditor editor) 
            {
                this.editor = editor; 
                End();
                InitializeComponent();
                this.Size = PreferredSize;
 
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.EditorService"]/*' /> 
            /// <devdoc>
            ///      Allows someone else to close our dropdown. 
            /// </devdoc>
            public IWindowsFormsEditorService EditorService
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
                get
                { 
                    return this.edSvc; 
                }
            } 


            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.Value"]/*' />
            /// <devdoc> 
            ///      Retrns the current value of BorderSides, if nothing is selected returns BorderSides.None.
            /// </devdoc> 
            public object Value 
            {
                get 
                {
                   //Return the new value....
                   return this.currentValue;
                } 
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.End"]/*' /> 
            public void End()
            { 

                this.edSvc = null;
                this.originalValue = null;
                this.currentValue = null; 
                this.updateCurrentValue = false;
            } 
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.OnGotFocus"]/*' />
            /// <devdoc> 
            ///      The first checkBox (allCheckBox) gets the focus by default.
            /// </devdoc>
            protected override void OnGotFocus(EventArgs e)
            { 
                base.OnGotFocus(e);
                this.noneCheckBox.Focus(); 
            } 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.InitializeComponent"]/*' /> 
            private void InitializeComponent(){
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BorderSidesEditor));
                this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
                this.noneCheckBox = new System.Windows.Forms.CheckBox(); 
                this.allCheckBox = new System.Windows.Forms.CheckBox();
                this.topCheckBox = new System.Windows.Forms.CheckBox(); 
                this.bottomCheckBox = new System.Windows.Forms.CheckBox(); 
                this.rightCheckBox = new System.Windows.Forms.CheckBox();
                this.leftCheckBox = new System.Windows.Forms.CheckBox(); 
                this.splitterLabel = new System.Windows.Forms.Label();
                this.tableLayoutPanel1.SuspendLayout();
                this.SuspendLayout();
                // 
                // tableLayoutPanel1
                // 
                resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1"); 
                this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                this.tableLayoutPanel1.BackColor = System.Drawing.SystemColors.Window; 
                this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
                this.tableLayoutPanel1.Controls.Add(this.noneCheckBox, 0, 0);
                this.tableLayoutPanel1.Controls.Add(this.allCheckBox, 0, 2);
                this.tableLayoutPanel1.Controls.Add(this.topCheckBox, 0, 3); 
                this.tableLayoutPanel1.Controls.Add(this.bottomCheckBox, 0, 4);
                this.tableLayoutPanel1.Controls.Add(this.rightCheckBox, 0, 6); 
                this.tableLayoutPanel1.Controls.Add(this.leftCheckBox, 0, 5); 
                this.tableLayoutPanel1.Controls.Add(this.splitterLabel, 0, 1);
                this.tableLayoutPanel1.Name = "tableLayoutPanel1"; 
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
                // 
                // noneCheckBox
                //
                resources.ApplyResources(this.noneCheckBox, "noneCheckBox");
                this.noneCheckBox.Name = "noneCheckBox"; 
                this.noneCheckBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 1);
                // 
                // allCheckBox 
                //
                resources.ApplyResources(this.allCheckBox, "allCheckBox"); 
                this.allCheckBox.Name = "allCheckBox";
                this.allCheckBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 1);
                //
                // topCheckBox 
                //
                resources.ApplyResources(this.topCheckBox, "topCheckBox"); 
                this.topCheckBox.Margin = new System.Windows.Forms.Padding(20, 1, 3, 1); 
                this.topCheckBox.Name = "topCheckBox";
                // 
                // bottomCheckBox
                //
                resources.ApplyResources(this.bottomCheckBox, "bottomCheckBox");
                this.bottomCheckBox.Margin = new System.Windows.Forms.Padding(20, 1, 3, 1); 
                this.bottomCheckBox.Name = "bottomCheckBox";
                // 
                // rightCheckBox 
                //
                resources.ApplyResources(this.rightCheckBox, "rightCheckBox"); 
                this.rightCheckBox.Margin = new System.Windows.Forms.Padding(20, 1, 3, 1);
                this.rightCheckBox.Name = "rightCheckBox";
                //
                // leftCheckBox 
                //
                resources.ApplyResources(this.leftCheckBox, "leftCheckBox"); 
                this.leftCheckBox.Margin = new System.Windows.Forms.Padding(20, 1, 3, 1); 
                this.leftCheckBox.Name = "leftCheckBox";
                // 
                // splitterLabel
                //
                resources.ApplyResources(this.splitterLabel, "splitterLabel");
                this.splitterLabel.BackColor = System.Drawing.SystemColors.ControlDark; 
                this.splitterLabel.Name = "splitterLabel";
                // 
                // Control 
                //
                resources.ApplyResources(this, "$this"); 
                this.Controls.Add(this.tableLayoutPanel1);
                this.Padding = new System.Windows.Forms.Padding(1,1,1,1);
                this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
                this.AutoScaleDimensions = new System.Drawing.SizeF(6F,13F);
                this.tableLayoutPanel1.ResumeLayout(false); 
                this.tableLayoutPanel1.PerformLayout(); 
                this.ResumeLayout(false);
                this.PerformLayout(); 



                //Events 
                this.rightCheckBox.CheckedChanged += new System.EventHandler(this.rightCheckBox_CheckedChanged);
                this.leftCheckBox.CheckedChanged += new System.EventHandler(this.leftCheckBox_CheckedChanged); 
                this.bottomCheckBox.CheckedChanged += new System.EventHandler(this.bottomCheckBox_CheckedChanged); 
                this.topCheckBox.CheckedChanged += new System.EventHandler(this.topCheckBox_CheckedChanged);
                this.noneCheckBox.CheckedChanged += new System.EventHandler(this.noneCheckBox_CheckedChanged); 
                this.allCheckBox.CheckedChanged += new System.EventHandler(this.allCheckBox_CheckedChanged);

                this.noneCheckBox.Click += new System.EventHandler(this.noneCheckBoxClicked);
                this.allCheckBox.Click += new System.EventHandler(this.allCheckBoxClicked); 

            } 
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.rightCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values.
            /// </devdoc>
            private void rightCheckBox_CheckedChanged(object sender, System.EventArgs e)
            { 
                CheckBox senderCheckBox = sender as CheckBox;
                if (senderCheckBox.Checked) 
                { 
                    this.noneCheckBox.Checked = false;
                } 
                else // this is turned off....
                {
                    if (this.allCheckBox.Checked)
                    { 
                       this.allCheckBox.Checked = false;
                    } 
                } 
                UpdateCurrentValue();
            } 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.leftCheckBox_CheckedChanged"]/*' />
            /// <devdoc>
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values. 
            /// </devdoc>
            private void leftCheckBox_CheckedChanged(object sender, System.EventArgs e) 
            { 
                CheckBox senderCheckBox = sender as CheckBox;
                if (senderCheckBox.Checked) 
                {
                    this.noneCheckBox.Checked = false;
                }
                else // this is turned off.... 
                {
                    if (this.allCheckBox.Checked) 
                    { 
                       this.allCheckBox.Checked = false;
                    } 
                }
                UpdateCurrentValue();
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.bottomCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values. 
            /// </devdoc>
            private void bottomCheckBox_CheckedChanged(object sender, System.EventArgs e) 
            {
                CheckBox senderCheckBox = sender as CheckBox;
                if (senderCheckBox.Checked)
                { 
                    this.noneCheckBox.Checked = false;
                } 
                else // this is turned off.... 
                {
                    if (this.allCheckBox.Checked) 
                    {
                       this.allCheckBox.Checked = false;
                    }
                } 
                UpdateCurrentValue();
            } 
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.topCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values.
            /// </devdoc>
            private void topCheckBox_CheckedChanged(object sender, System.EventArgs e)
            { 
                CheckBox senderCheckBox = sender as CheckBox;
                if (senderCheckBox.Checked) 
                { 
                    this.noneCheckBox.Checked = false;
 
                }
                else // this is turned off....
                {
                    if (this.allCheckBox.Checked) 
                    {
                       this.allCheckBox.Checked = false; 
                    } 
                }
                UpdateCurrentValue(); 
            }

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.noneCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values.
            /// </devdoc> 
            private void noneCheckBox_CheckedChanged(object sender, System.EventArgs e) 
            {
                CheckBox senderCheckBox = sender as CheckBox; 
                if (senderCheckBox.Checked)
                {
                    this.allCheckBox.Checked = false;
                    this.topCheckBox.Checked = false; 
                    this.bottomCheckBox.Checked = false;
                    this.leftCheckBox.Checked = false; 
                    this.rightCheckBox.Checked = false; 
                }
                UpdateCurrentValue(); 
            }

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.allCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values.
            /// </devdoc> 
            private void allCheckBox_CheckedChanged(object sender, System.EventArgs e) 
            {
                CheckBox senderCheckBox = sender as CheckBox; 
                if (senderCheckBox.Checked)
                {

                    this.noneCheckBox.Checked = false; 
                    this.topCheckBox.Checked = true;
                    this.bottomCheckBox.Checked = true; 
                    this.leftCheckBox.Checked = true; 
                    this.rightCheckBox.Checked = true;
 
                }
                UpdateCurrentValue();
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.noneCheckBoxClicked"]/*' />
            /// <devdoc> 
            ///      Click event. 
            /// </devdoc>
            private void noneCheckBoxClicked(object sender, System.EventArgs e) 
            {
                if (noneChecked)
                {
                    this.noneCheckBox.Checked = true; 
                }
            } 
 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.allCheckBoxClicked"]/*' /> 
            /// <devdoc>
            ///      Click event.
            /// </devdoc>
            private void allCheckBoxClicked(object sender, System.EventArgs e) 
            {
                if (allChecked) 
                { 
                    this.allCheckBox.Checked = true;
                } 
            }


            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.ResetCheckBoxState"]/*' /> 
            /// <devdoc>
            ///      Allows to reset the state and start afresh. 
            /// </devdoc> 
            private void ResetCheckBoxState()
            { 
                this.allCheckBox.Checked = false;
                this.noneCheckBox.Checked = false;
                this.topCheckBox.Checked = false;
                this.bottomCheckBox.Checked = false; 
                this.leftCheckBox.Checked = false;
                this.rightCheckBox.Checked = false; 
            } 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.SetCheckBoxCheckState"]/*' /> 
            /// <devdoc>
            ///      Allows to select proper values..
            /// </devdoc>
            private void SetCheckBoxCheckState(ToolStripStatusLabelBorderSides sides) 
            {
 
                ResetCheckBoxState(); 
 				if ((sides & ToolStripStatusLabelBorderSides.All) == ToolStripStatusLabelBorderSides.All)
				{ 

                    this.allCheckBox.Checked = true;
                    this.topCheckBox.Checked = true;
                    this.bottomCheckBox.Checked = true; 
                    this.leftCheckBox.Checked = true;
                    this.rightCheckBox.Checked = true; 
                    this.allCheckBox.Checked = true; 
                }
                else { 
                    this.noneCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.None) == ToolStripStatusLabelBorderSides.None);
                    this.topCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.Top) == ToolStripStatusLabelBorderSides.Top);
                    this.bottomCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.Bottom) == ToolStripStatusLabelBorderSides.Bottom);
                    this.leftCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.Left) == ToolStripStatusLabelBorderSides.Left); 
                    this.rightCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.Right) == ToolStripStatusLabelBorderSides.Right);
                } 
 
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.Start"]/*' />
            /// <devdoc>
            ///      Triggered whenever the user drops down the editor.
            /// </devdoc> 
            public void Start(IWindowsFormsEditorService edSvc, object value)
            { 
                Debug.Assert(edSvc != null); 
                Debug.Assert(value is ToolStripStatusLabelBorderSides);
 
                this.edSvc = edSvc;
                this.originalValue = this.currentValue = value;

                ToolStripStatusLabelBorderSides currentSides = (ToolStripStatusLabelBorderSides) value; 

                SetCheckBoxCheckState(currentSides); 
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

                ToolStripStatusLabelBorderSides valueSide = ToolStripStatusLabelBorderSides.None;
                if (this.allCheckBox.Checked) 
                {
                    valueSide |= ToolStripStatusLabelBorderSides.All; 
                    this.currentValue = valueSide; 
                    this.allChecked = true;
                    this.noneChecked = false; 
                    return;
                }
                if (this.noneCheckBox.Checked)
                { 
                    valueSide |= ToolStripStatusLabelBorderSides.None;
                } 
                if (this.topCheckBox.Checked) 
                {
                    valueSide |= ToolStripStatusLabelBorderSides.Top; 
                }
                if (this.bottomCheckBox.Checked)
                {
                    valueSide |= ToolStripStatusLabelBorderSides.Bottom; 
                }
                if (this.leftCheckBox.Checked) 
                { 
                    valueSide |= ToolStripStatusLabelBorderSides.Left;
                } 
                if (this.rightCheckBox.Checked)
                {
                    valueSide |= ToolStripStatusLabelBorderSides.Right;
                } 

                if (valueSide == ToolStripStatusLabelBorderSides.None) 
                { 
                    this.allChecked = false;
                    this.noneChecked = true; 
                    this.noneCheckBox.Checked = true;
                }

                if (valueSide == (ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Top |ToolStripStatusLabelBorderSides.Bottom)) 
                {
                    this.allChecked = true; 
                    this.noneChecked = false; 
                    this.allCheckBox.Checked = true;
                } 
                this.currentValue = valueSide;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BorderSidesEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing.Design;
    using System.Windows.Forms; 
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design; 
 
    /// <internalonly/>
    /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor"]/*' /> 
    /// <devdoc>
    ///     Provides an editor for setting the ToolStripStatusLabel BorderSides property..
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class BorderSidesEditor : UITypeEditor
    { 
        private BorderSidesEditorUI borderSidesEditorUI; 

        /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.EditValue"]/*' /> 
        /// <devdoc>
        ///     Edits the given object value using the editor style provided by BorderSidesEditor.GetEditStyle.
        /// </devdoc>
 
        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        { 
            if (provider != null)
            {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null) 
                {
                    if (this.borderSidesEditorUI == null) 
                    { 
                        this.borderSidesEditorUI = new BorderSidesEditorUI(this);
                    } 
                    this.borderSidesEditorUI.Start(edSvc, value);
                    edSvc.DropDownControl(this.borderSidesEditorUI);

                    if (this.borderSidesEditorUI.Value != null) 
                    {
                        value = this.borderSidesEditorUI.Value; 
                    } 
                    this.borderSidesEditorUI.End();
                } 
            }
            return value;
        }
 
        /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///     Gets the editing style of the Edit method. If the method 
        ///     is not supported, this will return UITypeEditorEditStyle.None.
        /// </devdoc> 

        // This should be okay since System.Design only runs in FullTrust.
        // SECREVIEW: Isn't that true
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        { 
            return UITypeEditorEditStyle.DropDown; 
        }
 
        /// <devdoc>
        ///      Editor UI for the BorderSides editor.
        /// </devdoc>
        private class BorderSidesEditorUI : UserControl 
        {
            private BorderSidesEditor editor; 
            private IWindowsFormsEditorService edSvc; 
            private object originalValue, currentValue;
 
            private bool updateCurrentValue = false;

            private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    		private System.Windows.Forms.CheckBox allCheckBox; 
    		private System.Windows.Forms.CheckBox noneCheckBox;
    		private System.Windows.Forms.CheckBox topCheckBox; 
    		private System.Windows.Forms.CheckBox bottomCheckBox; 
    		private System.Windows.Forms.CheckBox leftCheckBox;
    		private System.Windows.Forms.CheckBox rightCheckBox; 
            private System.Windows.Forms.Label splitterLabel;

            private bool allChecked = false;
            private bool noneChecked = false; 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.BorderSidesEditorUI"]/*' /> 
            public BorderSidesEditorUI(BorderSidesEditor editor) 
            {
                this.editor = editor; 
                End();
                InitializeComponent();
                this.Size = PreferredSize;
 
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.EditorService"]/*' /> 
            /// <devdoc>
            ///      Allows someone else to close our dropdown. 
            /// </devdoc>
            public IWindowsFormsEditorService EditorService
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
                get
                { 
                    return this.edSvc; 
                }
            } 


            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.Value"]/*' />
            /// <devdoc> 
            ///      Retrns the current value of BorderSides, if nothing is selected returns BorderSides.None.
            /// </devdoc> 
            public object Value 
            {
                get 
                {
                   //Return the new value....
                   return this.currentValue;
                } 
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.End"]/*' /> 
            public void End()
            { 

                this.edSvc = null;
                this.originalValue = null;
                this.currentValue = null; 
                this.updateCurrentValue = false;
            } 
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.OnGotFocus"]/*' />
            /// <devdoc> 
            ///      The first checkBox (allCheckBox) gets the focus by default.
            /// </devdoc>
            protected override void OnGotFocus(EventArgs e)
            { 
                base.OnGotFocus(e);
                this.noneCheckBox.Focus(); 
            } 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.InitializeComponent"]/*' /> 
            private void InitializeComponent(){
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BorderSidesEditor));
                this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
                this.noneCheckBox = new System.Windows.Forms.CheckBox(); 
                this.allCheckBox = new System.Windows.Forms.CheckBox();
                this.topCheckBox = new System.Windows.Forms.CheckBox(); 
                this.bottomCheckBox = new System.Windows.Forms.CheckBox(); 
                this.rightCheckBox = new System.Windows.Forms.CheckBox();
                this.leftCheckBox = new System.Windows.Forms.CheckBox(); 
                this.splitterLabel = new System.Windows.Forms.Label();
                this.tableLayoutPanel1.SuspendLayout();
                this.SuspendLayout();
                // 
                // tableLayoutPanel1
                // 
                resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1"); 
                this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                this.tableLayoutPanel1.BackColor = System.Drawing.SystemColors.Window; 
                this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
                this.tableLayoutPanel1.Controls.Add(this.noneCheckBox, 0, 0);
                this.tableLayoutPanel1.Controls.Add(this.allCheckBox, 0, 2);
                this.tableLayoutPanel1.Controls.Add(this.topCheckBox, 0, 3); 
                this.tableLayoutPanel1.Controls.Add(this.bottomCheckBox, 0, 4);
                this.tableLayoutPanel1.Controls.Add(this.rightCheckBox, 0, 6); 
                this.tableLayoutPanel1.Controls.Add(this.leftCheckBox, 0, 5); 
                this.tableLayoutPanel1.Controls.Add(this.splitterLabel, 0, 1);
                this.tableLayoutPanel1.Name = "tableLayoutPanel1"; 
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
                this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
                // 
                // noneCheckBox
                //
                resources.ApplyResources(this.noneCheckBox, "noneCheckBox");
                this.noneCheckBox.Name = "noneCheckBox"; 
                this.noneCheckBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 1);
                // 
                // allCheckBox 
                //
                resources.ApplyResources(this.allCheckBox, "allCheckBox"); 
                this.allCheckBox.Name = "allCheckBox";
                this.allCheckBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 1);
                //
                // topCheckBox 
                //
                resources.ApplyResources(this.topCheckBox, "topCheckBox"); 
                this.topCheckBox.Margin = new System.Windows.Forms.Padding(20, 1, 3, 1); 
                this.topCheckBox.Name = "topCheckBox";
                // 
                // bottomCheckBox
                //
                resources.ApplyResources(this.bottomCheckBox, "bottomCheckBox");
                this.bottomCheckBox.Margin = new System.Windows.Forms.Padding(20, 1, 3, 1); 
                this.bottomCheckBox.Name = "bottomCheckBox";
                // 
                // rightCheckBox 
                //
                resources.ApplyResources(this.rightCheckBox, "rightCheckBox"); 
                this.rightCheckBox.Margin = new System.Windows.Forms.Padding(20, 1, 3, 1);
                this.rightCheckBox.Name = "rightCheckBox";
                //
                // leftCheckBox 
                //
                resources.ApplyResources(this.leftCheckBox, "leftCheckBox"); 
                this.leftCheckBox.Margin = new System.Windows.Forms.Padding(20, 1, 3, 1); 
                this.leftCheckBox.Name = "leftCheckBox";
                // 
                // splitterLabel
                //
                resources.ApplyResources(this.splitterLabel, "splitterLabel");
                this.splitterLabel.BackColor = System.Drawing.SystemColors.ControlDark; 
                this.splitterLabel.Name = "splitterLabel";
                // 
                // Control 
                //
                resources.ApplyResources(this, "$this"); 
                this.Controls.Add(this.tableLayoutPanel1);
                this.Padding = new System.Windows.Forms.Padding(1,1,1,1);
                this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
                this.AutoScaleDimensions = new System.Drawing.SizeF(6F,13F);
                this.tableLayoutPanel1.ResumeLayout(false); 
                this.tableLayoutPanel1.PerformLayout(); 
                this.ResumeLayout(false);
                this.PerformLayout(); 



                //Events 
                this.rightCheckBox.CheckedChanged += new System.EventHandler(this.rightCheckBox_CheckedChanged);
                this.leftCheckBox.CheckedChanged += new System.EventHandler(this.leftCheckBox_CheckedChanged); 
                this.bottomCheckBox.CheckedChanged += new System.EventHandler(this.bottomCheckBox_CheckedChanged); 
                this.topCheckBox.CheckedChanged += new System.EventHandler(this.topCheckBox_CheckedChanged);
                this.noneCheckBox.CheckedChanged += new System.EventHandler(this.noneCheckBox_CheckedChanged); 
                this.allCheckBox.CheckedChanged += new System.EventHandler(this.allCheckBox_CheckedChanged);

                this.noneCheckBox.Click += new System.EventHandler(this.noneCheckBoxClicked);
                this.allCheckBox.Click += new System.EventHandler(this.allCheckBoxClicked); 

            } 
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.rightCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values.
            /// </devdoc>
            private void rightCheckBox_CheckedChanged(object sender, System.EventArgs e)
            { 
                CheckBox senderCheckBox = sender as CheckBox;
                if (senderCheckBox.Checked) 
                { 
                    this.noneCheckBox.Checked = false;
                } 
                else // this is turned off....
                {
                    if (this.allCheckBox.Checked)
                    { 
                       this.allCheckBox.Checked = false;
                    } 
                } 
                UpdateCurrentValue();
            } 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.leftCheckBox_CheckedChanged"]/*' />
            /// <devdoc>
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values. 
            /// </devdoc>
            private void leftCheckBox_CheckedChanged(object sender, System.EventArgs e) 
            { 
                CheckBox senderCheckBox = sender as CheckBox;
                if (senderCheckBox.Checked) 
                {
                    this.noneCheckBox.Checked = false;
                }
                else // this is turned off.... 
                {
                    if (this.allCheckBox.Checked) 
                    { 
                       this.allCheckBox.Checked = false;
                    } 
                }
                UpdateCurrentValue();
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.bottomCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values. 
            /// </devdoc>
            private void bottomCheckBox_CheckedChanged(object sender, System.EventArgs e) 
            {
                CheckBox senderCheckBox = sender as CheckBox;
                if (senderCheckBox.Checked)
                { 
                    this.noneCheckBox.Checked = false;
                } 
                else // this is turned off.... 
                {
                    if (this.allCheckBox.Checked) 
                    {
                       this.allCheckBox.Checked = false;
                    }
                } 
                UpdateCurrentValue();
            } 
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.topCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values.
            /// </devdoc>
            private void topCheckBox_CheckedChanged(object sender, System.EventArgs e)
            { 
                CheckBox senderCheckBox = sender as CheckBox;
                if (senderCheckBox.Checked) 
                { 
                    this.noneCheckBox.Checked = false;
 
                }
                else // this is turned off....
                {
                    if (this.allCheckBox.Checked) 
                    {
                       this.allCheckBox.Checked = false; 
                    } 
                }
                UpdateCurrentValue(); 
            }

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.noneCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values.
            /// </devdoc> 
            private void noneCheckBox_CheckedChanged(object sender, System.EventArgs e) 
            {
                CheckBox senderCheckBox = sender as CheckBox; 
                if (senderCheckBox.Checked)
                {
                    this.allCheckBox.Checked = false;
                    this.topCheckBox.Checked = false; 
                    this.bottomCheckBox.Checked = false;
                    this.leftCheckBox.Checked = false; 
                    this.rightCheckBox.Checked = false; 
                }
                UpdateCurrentValue(); 
            }

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.allCheckBox_CheckedChanged"]/*' />
            /// <devdoc> 
            ///      CheckBox CheckedChanged event.. allows selecting/Deselecting proper values.
            /// </devdoc> 
            private void allCheckBox_CheckedChanged(object sender, System.EventArgs e) 
            {
                CheckBox senderCheckBox = sender as CheckBox; 
                if (senderCheckBox.Checked)
                {

                    this.noneCheckBox.Checked = false; 
                    this.topCheckBox.Checked = true;
                    this.bottomCheckBox.Checked = true; 
                    this.leftCheckBox.Checked = true; 
                    this.rightCheckBox.Checked = true;
 
                }
                UpdateCurrentValue();
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.noneCheckBoxClicked"]/*' />
            /// <devdoc> 
            ///      Click event. 
            /// </devdoc>
            private void noneCheckBoxClicked(object sender, System.EventArgs e) 
            {
                if (noneChecked)
                {
                    this.noneCheckBox.Checked = true; 
                }
            } 
 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.allCheckBoxClicked"]/*' /> 
            /// <devdoc>
            ///      Click event.
            /// </devdoc>
            private void allCheckBoxClicked(object sender, System.EventArgs e) 
            {
                if (allChecked) 
                { 
                    this.allCheckBox.Checked = true;
                } 
            }


            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.ResetCheckBoxState"]/*' /> 
            /// <devdoc>
            ///      Allows to reset the state and start afresh. 
            /// </devdoc> 
            private void ResetCheckBoxState()
            { 
                this.allCheckBox.Checked = false;
                this.noneCheckBox.Checked = false;
                this.topCheckBox.Checked = false;
                this.bottomCheckBox.Checked = false; 
                this.leftCheckBox.Checked = false;
                this.rightCheckBox.Checked = false; 
            } 

            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.SetCheckBoxCheckState"]/*' /> 
            /// <devdoc>
            ///      Allows to select proper values..
            /// </devdoc>
            private void SetCheckBoxCheckState(ToolStripStatusLabelBorderSides sides) 
            {
 
                ResetCheckBoxState(); 
 				if ((sides & ToolStripStatusLabelBorderSides.All) == ToolStripStatusLabelBorderSides.All)
				{ 

                    this.allCheckBox.Checked = true;
                    this.topCheckBox.Checked = true;
                    this.bottomCheckBox.Checked = true; 
                    this.leftCheckBox.Checked = true;
                    this.rightCheckBox.Checked = true; 
                    this.allCheckBox.Checked = true; 
                }
                else { 
                    this.noneCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.None) == ToolStripStatusLabelBorderSides.None);
                    this.topCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.Top) == ToolStripStatusLabelBorderSides.Top);
                    this.bottomCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.Bottom) == ToolStripStatusLabelBorderSides.Bottom);
                    this.leftCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.Left) == ToolStripStatusLabelBorderSides.Left); 
                    this.rightCheckBox.Checked = ((sides & ToolStripStatusLabelBorderSides.Right) == ToolStripStatusLabelBorderSides.Right);
                } 
 
            }
 
            /// <include file='doc\BorderSidesEditor.uex' path='docs/doc[@for="BorderSidesEditor.BorderSidesEditorUI.Start"]/*' />
            /// <devdoc>
            ///      Triggered whenever the user drops down the editor.
            /// </devdoc> 
            public void Start(IWindowsFormsEditorService edSvc, object value)
            { 
                Debug.Assert(edSvc != null); 
                Debug.Assert(value is ToolStripStatusLabelBorderSides);
 
                this.edSvc = edSvc;
                this.originalValue = this.currentValue = value;

                ToolStripStatusLabelBorderSides currentSides = (ToolStripStatusLabelBorderSides) value; 

                SetCheckBoxCheckState(currentSides); 
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

                ToolStripStatusLabelBorderSides valueSide = ToolStripStatusLabelBorderSides.None;
                if (this.allCheckBox.Checked) 
                {
                    valueSide |= ToolStripStatusLabelBorderSides.All; 
                    this.currentValue = valueSide; 
                    this.allChecked = true;
                    this.noneChecked = false; 
                    return;
                }
                if (this.noneCheckBox.Checked)
                { 
                    valueSide |= ToolStripStatusLabelBorderSides.None;
                } 
                if (this.topCheckBox.Checked) 
                {
                    valueSide |= ToolStripStatusLabelBorderSides.Top; 
                }
                if (this.bottomCheckBox.Checked)
                {
                    valueSide |= ToolStripStatusLabelBorderSides.Bottom; 
                }
                if (this.leftCheckBox.Checked) 
                { 
                    valueSide |= ToolStripStatusLabelBorderSides.Left;
                } 
                if (this.rightCheckBox.Checked)
                {
                    valueSide |= ToolStripStatusLabelBorderSides.Right;
                } 

                if (valueSide == ToolStripStatusLabelBorderSides.None) 
                { 
                    this.allChecked = false;
                    this.noneChecked = true; 
                    this.noneCheckBox.Checked = true;
                }

                if (valueSide == (ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Top |ToolStripStatusLabelBorderSides.Bottom)) 
                {
                    this.allChecked = true; 
                    this.noneChecked = false; 
                    this.allCheckBox.Checked = true;
                } 
                this.currentValue = valueSide;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
