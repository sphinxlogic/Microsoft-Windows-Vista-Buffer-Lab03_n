//------------------------------------------------------------------------------ 
// <copyright file="TaskForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design; 

    /// <devdoc> 
    /// Represents a wizard used to guide users through configuration processes.
    /// </devdoc>
    internal abstract class TaskForm : TaskFormBase {
 
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Label _dummyLabel1; 
        private System.Windows.Forms.Button _cancelButton; 
        private System.Windows.Forms.TableLayoutPanel _dialogButtonsTableLayoutPanel;
 
        /// <devdoc>
        /// Creates a new TaskForm with a given service provider.
        /// </devdoc>
        public TaskForm(IServiceProvider serviceProvider) : base(serviceProvider) { 
            InitializeComponent();
            InitializeUI(); 
        } 

        protected Button OKButton { 
            get {
                return _okButton;
            }
        } 

        #region Designer generated code 
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent() {

            this._dialogButtonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button(); 
            this._dummyLabel1 = new System.Windows.Forms.Label(); 

            this._dialogButtonsTableLayoutPanel.SuspendLayout(); 
            this.SuspendLayout();

            //
            // _dialogButtonsTableLayoutPanel 
            //
            this._dialogButtonsTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._dialogButtonsTableLayoutPanel.AutoSize = true; 
            this._dialogButtonsTableLayoutPanel.ColumnCount = 3;
            this._dialogButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this._dialogButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 6F));
            this._dialogButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._dialogButtonsTableLayoutPanel.Controls.Add(this._okButton);
            this._dialogButtonsTableLayoutPanel.Controls.Add(this._dummyLabel1); 
            this._dialogButtonsTableLayoutPanel.Controls.Add(this._cancelButton);
            this._dialogButtonsTableLayoutPanel.Location = new System.Drawing.Point(404, 381); 
            this._dialogButtonsTableLayoutPanel.Name = "_dialogButtonsTableLayoutPanel"; 
            this._dialogButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._dialogButtonsTableLayoutPanel.Size = new System.Drawing.Size(156, 23); 
            this._dialogButtonsTableLayoutPanel.TabIndex = 100;
            //
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.AutoSize = true; 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this._okButton.Enabled = false;
            this._okButton.Location = new System.Drawing.Point(0, 0); 
            this._okButton.Margin = new System.Windows.Forms.Padding(0);
            this._okButton.MinimumSize = new System.Drawing.Size(75, 23);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 50; 
            this._okButton.Click += new System.EventHandler(this.OnOKButtonClick);
            // 
            // _dummyLabel1 
            //
            this._dummyLabel1.Location = new System.Drawing.Point(75, 0); 
            this._dummyLabel1.Margin = new System.Windows.Forms.Padding(0);
            this._dummyLabel1.Name = "_dummyLabel1";
            this._dummyLabel1.Size = new System.Drawing.Size(6, 0);
            this._dummyLabel1.TabIndex = 20; 
            //
            // _cancelButton 
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.AutoSize = true; 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(81, 0);
            this._cancelButton.Margin = new System.Windows.Forms.Padding(0);
            this._cancelButton.MinimumSize = new System.Drawing.Size(75, 23); 
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 70; 
            this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick); 
            //
            // TaskForm 
            //
            this.AcceptButton = this._okButton;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.CancelButton = this._cancelButton; 
            this.Controls.Add(this._dialogButtonsTableLayoutPanel);
            this._dialogButtonsTableLayoutPanel.ResumeLayout(false); 
            this._dialogButtonsTableLayoutPanel.PerformLayout(); 
            this.ResumeLayout(false);
            this.PerformLayout(); 
        }
        #endregion

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc> 
        private void InitializeUI() {
            _cancelButton.Text = SR.GetString(SR.Wizard_CancelButton); 
            _okButton.Text = SR.GetString(SR.OKCaption);
        }

        /// <devdoc> 
        /// Click event handler for the Cancel button.
        /// </devdoc> 
        protected virtual void OnCancelButtonClick(object sender, System.EventArgs e) { 
            DialogResult = DialogResult.Cancel;
            Close(); 
        }

        /// <devdoc>
        /// Click event handler for the OK button. 
        /// </devdoc>
        protected virtual void OnOKButtonClick(object sender, System.EventArgs e) { 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TaskForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design; 

    /// <devdoc> 
    /// Represents a wizard used to guide users through configuration processes.
    /// </devdoc>
    internal abstract class TaskForm : TaskFormBase {
 
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Label _dummyLabel1; 
        private System.Windows.Forms.Button _cancelButton; 
        private System.Windows.Forms.TableLayoutPanel _dialogButtonsTableLayoutPanel;
 
        /// <devdoc>
        /// Creates a new TaskForm with a given service provider.
        /// </devdoc>
        public TaskForm(IServiceProvider serviceProvider) : base(serviceProvider) { 
            InitializeComponent();
            InitializeUI(); 
        } 

        protected Button OKButton { 
            get {
                return _okButton;
            }
        } 

        #region Designer generated code 
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent() {

            this._dialogButtonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button(); 
            this._dummyLabel1 = new System.Windows.Forms.Label(); 

            this._dialogButtonsTableLayoutPanel.SuspendLayout(); 
            this.SuspendLayout();

            //
            // _dialogButtonsTableLayoutPanel 
            //
            this._dialogButtonsTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._dialogButtonsTableLayoutPanel.AutoSize = true; 
            this._dialogButtonsTableLayoutPanel.ColumnCount = 3;
            this._dialogButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this._dialogButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 6F));
            this._dialogButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._dialogButtonsTableLayoutPanel.Controls.Add(this._okButton);
            this._dialogButtonsTableLayoutPanel.Controls.Add(this._dummyLabel1); 
            this._dialogButtonsTableLayoutPanel.Controls.Add(this._cancelButton);
            this._dialogButtonsTableLayoutPanel.Location = new System.Drawing.Point(404, 381); 
            this._dialogButtonsTableLayoutPanel.Name = "_dialogButtonsTableLayoutPanel"; 
            this._dialogButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._dialogButtonsTableLayoutPanel.Size = new System.Drawing.Size(156, 23); 
            this._dialogButtonsTableLayoutPanel.TabIndex = 100;
            //
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.AutoSize = true; 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this._okButton.Enabled = false;
            this._okButton.Location = new System.Drawing.Point(0, 0); 
            this._okButton.Margin = new System.Windows.Forms.Padding(0);
            this._okButton.MinimumSize = new System.Drawing.Size(75, 23);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 50; 
            this._okButton.Click += new System.EventHandler(this.OnOKButtonClick);
            // 
            // _dummyLabel1 
            //
            this._dummyLabel1.Location = new System.Drawing.Point(75, 0); 
            this._dummyLabel1.Margin = new System.Windows.Forms.Padding(0);
            this._dummyLabel1.Name = "_dummyLabel1";
            this._dummyLabel1.Size = new System.Drawing.Size(6, 0);
            this._dummyLabel1.TabIndex = 20; 
            //
            // _cancelButton 
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.AutoSize = true; 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(81, 0);
            this._cancelButton.Margin = new System.Windows.Forms.Padding(0);
            this._cancelButton.MinimumSize = new System.Drawing.Size(75, 23); 
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 70; 
            this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick); 
            //
            // TaskForm 
            //
            this.AcceptButton = this._okButton;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.CancelButton = this._cancelButton; 
            this.Controls.Add(this._dialogButtonsTableLayoutPanel);
            this._dialogButtonsTableLayoutPanel.ResumeLayout(false); 
            this._dialogButtonsTableLayoutPanel.PerformLayout(); 
            this.ResumeLayout(false);
            this.PerformLayout(); 
        }
        #endregion

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc> 
        private void InitializeUI() {
            _cancelButton.Text = SR.GetString(SR.Wizard_CancelButton); 
            _okButton.Text = SR.GetString(SR.OKCaption);
        }

        /// <devdoc> 
        /// Click event handler for the Cancel button.
        /// </devdoc> 
        protected virtual void OnCancelButtonClick(object sender, System.EventArgs e) { 
            DialogResult = DialogResult.Cancel;
            Close(); 
        }

        /// <devdoc>
        /// Click event handler for the OK button. 
        /// </devdoc>
        protected virtual void OnOKButtonClick(object sender, System.EventArgs e) { 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
