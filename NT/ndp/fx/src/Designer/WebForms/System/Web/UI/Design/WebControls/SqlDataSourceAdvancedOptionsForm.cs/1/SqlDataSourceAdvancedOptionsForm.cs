//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConfigureSelectPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Design;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization; 
    using System.Web.UI.Design.Util;
    using System.Windows.Forms; 
 
    internal sealed class SqlDataSourceAdvancedOptionsForm : DesignerForm {
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.CheckBox _generateCheckBox;
        private System.Windows.Forms.Label _generateHelpLabel;
        private System.Windows.Forms.CheckBox _optimisticCheckBox; 
        private System.Windows.Forms.Label _optimisticHelpLabel;
        private System.Windows.Forms.Button _cancelButton; 
 
        public SqlDataSourceAdvancedOptionsForm(IServiceProvider serviceProvider) : base(serviceProvider) {
            InitializeComponent(); 
            InitializeUI();
        }

        public bool GenerateStatements { 
            get {
                return _generateCheckBox.Checked; 
            } 
            set {
                _generateCheckBox.Checked = value; 
                UpdateEnabledState();
            }
        }
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.SqlDataSource.AdvancedOptions"; 
            }
        } 

        public bool OptimisticConcurrency {
            get {
                return _optimisticCheckBox.Checked; 
            }
            set { 
                _optimisticCheckBox.Checked = value; 
                UpdateEnabledState();
            } 
        }

        #region Windows Form Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary> 
        private void InitializeComponent() {
            this._helpLabel = new System.Windows.Forms.Label(); 
            this._generateCheckBox = new System.Windows.Forms.CheckBox();
            this._generateHelpLabel = new System.Windows.Forms.Label();
            this._optimisticCheckBox = new System.Windows.Forms.CheckBox();
            this._optimisticHelpLabel = new System.Windows.Forms.Label(); 
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button(); 
            this.SuspendLayout(); 
            //
            // _helpLabel 
            //
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(12, 12); 
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(374, 32); 
            this._helpLabel.TabIndex = 10; 
            //
            // _generateCheckBox 
            //
            this._generateCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._generateCheckBox.Location = new System.Drawing.Point(12, 52); 
            this._generateCheckBox.Name = "_generateCheckBox";
            this._generateCheckBox.Size = new System.Drawing.Size(374, 18); 
            this._generateCheckBox.TabIndex = 20; 
            this._generateCheckBox.CheckedChanged += new System.EventHandler(this.OnGenerateCheckBoxCheckedChanged);
            // 
            // _generateHelpLabel
            //
            this._generateHelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._generateHelpLabel.Location = new System.Drawing.Point(29, 73);
            this._generateHelpLabel.Name = "_generateHelpLabel"; 
            this._generateHelpLabel.Size = new System.Drawing.Size(357, 48); 
            this._generateHelpLabel.TabIndex = 30;
            // 
            // _optimisticCheckBox
            //
            this._optimisticCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._optimisticCheckBox.Location = new System.Drawing.Point(12, 132);
            this._optimisticCheckBox.Name = "_optimisticCheckBox"; 
            this._optimisticCheckBox.Size = new System.Drawing.Size(374, 18); 
            this._optimisticCheckBox.TabIndex = 40;
            // 
            // _optimisticHelpLabel
            //
            this._optimisticHelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._optimisticHelpLabel.Location = new System.Drawing.Point(29, 153);
            this._optimisticHelpLabel.Name = "_optimisticHelpLabel"; 
            this._optimisticHelpLabel.Size = new System.Drawing.Size(357, 52); 
            this._optimisticHelpLabel.TabIndex = 50;
            // 
            // _okButton
            //
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.Location = new System.Drawing.Point(230, 209); 
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 60; 
            this._okButton.Click += new System.EventHandler(this.OnOkButtonClick); 
            //
            // _cancelButton 
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(311, 209); 
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 70; 
            this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick); 
            //
            // SqlDataSourceAdvancedOptionsForm 
            //
            this.AcceptButton = this._okButton;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(398, 244); 
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton); 
            this.Controls.Add(this._optimisticHelpLabel); 
            this.Controls.Add(this._optimisticCheckBox);
            this.Controls.Add(this._generateHelpLabel); 
            this.Controls.Add(this._generateCheckBox);
            this.Controls.Add(this._helpLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SqlDataSourceAdvancedOptionsForm"; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
 
            InitializeForm(); 

            this.ResumeLayout(false); 
        }
        #endregion

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc> 
        private void InitializeUI() {
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_HelpLabel); 
            _generateCheckBox.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_GenerateCheckBox);
            _generateHelpLabel.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_GenerateHelpLabel);
            _optimisticCheckBox.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_OptimisticCheckBox);
            _optimisticHelpLabel.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_OptimisticLabel); 
            Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_Caption);
 
            _generateCheckBox.AccessibleDescription = _generateHelpLabel.Text; 
            _optimisticCheckBox.AccessibleDescription = _optimisticHelpLabel.Text;
 
            _okButton.Text = SR.GetString(SR.OK);
            _cancelButton.Text = SR.GetString(SR.Cancel);

            UpdateFonts(); 
        }
 
        private void OnCancelButtonClick(object sender, System.EventArgs e) { 
            DialogResult = DialogResult.Cancel;
            Close(); 
        }

        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e); 
            UpdateFonts();
        } 
 
        private void OnGenerateCheckBoxCheckedChanged(object sender, System.EventArgs e) {
            UpdateEnabledState(); 
        }

        private void OnOkButtonClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.OK; 
            Close();
        } 
 
        public void SetAllowAutogenerate(bool allowAutogenerate) {
            if (!allowAutogenerate) { 
                _generateCheckBox.Checked = false;
                _generateCheckBox.Enabled = false;
                _generateHelpLabel.Enabled = false;
                UpdateEnabledState(); 
            }
        } 
 
        private void UpdateEnabledState() {
            bool allowOptimistic = _generateCheckBox.Checked; 
            _optimisticCheckBox.Enabled = allowOptimistic;
            _optimisticHelpLabel.Enabled = allowOptimistic;
            if (!allowOptimistic) {
                _optimisticCheckBox.Checked = false; 
            }
        } 
 
        private void UpdateFonts() {
            Font boldFont = new Font(Font, FontStyle.Bold); 
            _generateCheckBox.Font = boldFont;
            _optimisticCheckBox.Font = boldFont;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConfigureSelectPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Design;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization; 
    using System.Web.UI.Design.Util;
    using System.Windows.Forms; 
 
    internal sealed class SqlDataSourceAdvancedOptionsForm : DesignerForm {
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.CheckBox _generateCheckBox;
        private System.Windows.Forms.Label _generateHelpLabel;
        private System.Windows.Forms.CheckBox _optimisticCheckBox; 
        private System.Windows.Forms.Label _optimisticHelpLabel;
        private System.Windows.Forms.Button _cancelButton; 
 
        public SqlDataSourceAdvancedOptionsForm(IServiceProvider serviceProvider) : base(serviceProvider) {
            InitializeComponent(); 
            InitializeUI();
        }

        public bool GenerateStatements { 
            get {
                return _generateCheckBox.Checked; 
            } 
            set {
                _generateCheckBox.Checked = value; 
                UpdateEnabledState();
            }
        }
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.SqlDataSource.AdvancedOptions"; 
            }
        } 

        public bool OptimisticConcurrency {
            get {
                return _optimisticCheckBox.Checked; 
            }
            set { 
                _optimisticCheckBox.Checked = value; 
                UpdateEnabledState();
            } 
        }

        #region Windows Form Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary> 
        private void InitializeComponent() {
            this._helpLabel = new System.Windows.Forms.Label(); 
            this._generateCheckBox = new System.Windows.Forms.CheckBox();
            this._generateHelpLabel = new System.Windows.Forms.Label();
            this._optimisticCheckBox = new System.Windows.Forms.CheckBox();
            this._optimisticHelpLabel = new System.Windows.Forms.Label(); 
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button(); 
            this.SuspendLayout(); 
            //
            // _helpLabel 
            //
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(12, 12); 
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(374, 32); 
            this._helpLabel.TabIndex = 10; 
            //
            // _generateCheckBox 
            //
            this._generateCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._generateCheckBox.Location = new System.Drawing.Point(12, 52); 
            this._generateCheckBox.Name = "_generateCheckBox";
            this._generateCheckBox.Size = new System.Drawing.Size(374, 18); 
            this._generateCheckBox.TabIndex = 20; 
            this._generateCheckBox.CheckedChanged += new System.EventHandler(this.OnGenerateCheckBoxCheckedChanged);
            // 
            // _generateHelpLabel
            //
            this._generateHelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._generateHelpLabel.Location = new System.Drawing.Point(29, 73);
            this._generateHelpLabel.Name = "_generateHelpLabel"; 
            this._generateHelpLabel.Size = new System.Drawing.Size(357, 48); 
            this._generateHelpLabel.TabIndex = 30;
            // 
            // _optimisticCheckBox
            //
            this._optimisticCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._optimisticCheckBox.Location = new System.Drawing.Point(12, 132);
            this._optimisticCheckBox.Name = "_optimisticCheckBox"; 
            this._optimisticCheckBox.Size = new System.Drawing.Size(374, 18); 
            this._optimisticCheckBox.TabIndex = 40;
            // 
            // _optimisticHelpLabel
            //
            this._optimisticHelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._optimisticHelpLabel.Location = new System.Drawing.Point(29, 153);
            this._optimisticHelpLabel.Name = "_optimisticHelpLabel"; 
            this._optimisticHelpLabel.Size = new System.Drawing.Size(357, 52); 
            this._optimisticHelpLabel.TabIndex = 50;
            // 
            // _okButton
            //
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.Location = new System.Drawing.Point(230, 209); 
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 60; 
            this._okButton.Click += new System.EventHandler(this.OnOkButtonClick); 
            //
            // _cancelButton 
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(311, 209); 
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 70; 
            this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick); 
            //
            // SqlDataSourceAdvancedOptionsForm 
            //
            this.AcceptButton = this._okButton;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(398, 244); 
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton); 
            this.Controls.Add(this._optimisticHelpLabel); 
            this.Controls.Add(this._optimisticCheckBox);
            this.Controls.Add(this._generateHelpLabel); 
            this.Controls.Add(this._generateCheckBox);
            this.Controls.Add(this._helpLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SqlDataSourceAdvancedOptionsForm"; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
 
            InitializeForm(); 

            this.ResumeLayout(false); 
        }
        #endregion

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc> 
        private void InitializeUI() {
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_HelpLabel); 
            _generateCheckBox.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_GenerateCheckBox);
            _generateHelpLabel.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_GenerateHelpLabel);
            _optimisticCheckBox.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_OptimisticCheckBox);
            _optimisticHelpLabel.Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_OptimisticLabel); 
            Text = SR.GetString(SR.SqlDataSourceAdvancedOptionsForm_Caption);
 
            _generateCheckBox.AccessibleDescription = _generateHelpLabel.Text; 
            _optimisticCheckBox.AccessibleDescription = _optimisticHelpLabel.Text;
 
            _okButton.Text = SR.GetString(SR.OK);
            _cancelButton.Text = SR.GetString(SR.Cancel);

            UpdateFonts(); 
        }
 
        private void OnCancelButtonClick(object sender, System.EventArgs e) { 
            DialogResult = DialogResult.Cancel;
            Close(); 
        }

        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e); 
            UpdateFonts();
        } 
 
        private void OnGenerateCheckBoxCheckedChanged(object sender, System.EventArgs e) {
            UpdateEnabledState(); 
        }

        private void OnOkButtonClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.OK; 
            Close();
        } 
 
        public void SetAllowAutogenerate(bool allowAutogenerate) {
            if (!allowAutogenerate) { 
                _generateCheckBox.Checked = false;
                _generateCheckBox.Enabled = false;
                _generateHelpLabel.Enabled = false;
                UpdateEnabledState(); 
            }
        } 
 
        private void UpdateEnabledState() {
            bool allowOptimistic = _generateCheckBox.Checked; 
            _optimisticCheckBox.Enabled = allowOptimistic;
            _optimisticHelpLabel.Enabled = allowOptimistic;
            if (!allowOptimistic) {
                _optimisticCheckBox.Checked = false; 
            }
        } 
 
        private void UpdateFonts() {
            Font boldFont = new Font(Font, FontStyle.Bold); 
            _generateCheckBox.Font = boldFont;
            _optimisticCheckBox.Font = boldFont;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
