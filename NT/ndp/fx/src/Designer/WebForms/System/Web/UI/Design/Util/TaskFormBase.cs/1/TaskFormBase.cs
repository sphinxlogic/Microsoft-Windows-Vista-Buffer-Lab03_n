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
    internal abstract class TaskFormBase : DesignerForm {
 
        private System.Windows.Forms.Panel _taskPanel;
        private System.Windows.Forms.Label _bottomDividerLabel; 
        private System.Windows.Forms.Panel _headerPanel; 
        private System.Windows.Forms.Label _captionLabel;
        private System.Windows.Forms.PictureBox _glyphPictureBox; 

        /// <devdoc>
        /// Creates a new TaskForm with a given service provider.
        /// </devdoc> 
        public TaskFormBase(IServiceProvider serviceProvider)
            : base(serviceProvider) { 
            InitializeComponent(); 
            InitializeUI();
        } 

        protected Label CaptionLabel {
            get {
                return _captionLabel; 
            }
        } 
 
        /// <devdoc>
        /// A glyph for the wizard. 
        /// </devdoc>
        public Image Glyph {
            get {
                return _glyphPictureBox.Image; 
            }
            set { 
                _glyphPictureBox.Image = value; 
            }
        } 

        protected Panel TaskPanel {
            get {
                return _taskPanel; 
            }
        } 
 
        #region Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() { 

            this._taskPanel = new System.Windows.Forms.Panel(); 
            this._bottomDividerLabel = new System.Windows.Forms.Label(); 
            this._captionLabel = new System.Windows.Forms.Label();
            this._headerPanel = new System.Windows.Forms.Panel(); 
            this._glyphPictureBox = new System.Windows.Forms.PictureBox();
            this._headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._glyphPictureBox)).BeginInit();
            this.SuspendLayout(); 

            // 
            // _taskPanel 
            //
            this._taskPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._taskPanel.Location = new System.Drawing.Point(14, 78);
            this._taskPanel.Name = "_taskPanel"; 
            this._taskPanel.Size = new System.Drawing.Size(544, 274);
            this._taskPanel.TabIndex = 30; 
            // 
            // _bottomDividerLabel
            // 
            this._bottomDividerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._bottomDividerLabel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this._bottomDividerLabel.Location = new System.Drawing.Point(0, 366); 
            this._bottomDividerLabel.Name = "_bottomDividerLabel";
            this._bottomDividerLabel.Size = new System.Drawing.Size(572, 1); 
            this._bottomDividerLabel.TabIndex = 40; 
            //
            // _headerPanel 
            //
            this._headerPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._headerPanel.BackColor = System.Drawing.SystemColors.ControlLightLight; 
            this._headerPanel.Controls.Add(this._glyphPictureBox);
            this._headerPanel.Controls.Add(this._captionLabel); 
            this._headerPanel.Location = new System.Drawing.Point(0, 0); 
            this._headerPanel.Name = "_headerPanel";
            this._headerPanel.Size = new System.Drawing.Size(572, 64); 
            this._headerPanel.TabIndex = 10;
            //
            // _glyphPictureBox
            // 
            this._glyphPictureBox.Location = new System.Drawing.Point(0, 0);
            this._glyphPictureBox.Name = "_glyphPictureBox"; 
            this._glyphPictureBox.Size = new System.Drawing.Size(65, 64); 
            this._glyphPictureBox.TabIndex = 20;
            this._glyphPictureBox.TabStop = false; 
            //
            // _captionLabel
            //
            this._captionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._captionLabel.Location = new System.Drawing.Point(71, 17); 
            this._captionLabel.Name = "_captionLabel"; 
            this._captionLabel.Size = new System.Drawing.Size(487, 47);
            this._captionLabel.TabIndex = 10; 
            //
            // TaskForm
            //
            this.ClientSize = new System.Drawing.Size(572, 416); 
            this.Controls.Add(this._headerPanel);
            this.Controls.Add(this._bottomDividerLabel); 
            this.Controls.Add(this._taskPanel); 
            this.MinimumSize = new System.Drawing.Size(580, 450);
            this.Name = "TaskForm"; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this._headerPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._glyphPictureBox)).EndInit();
            this.ResumeLayout(false); 
            this.PerformLayout();
        } 
        #endregion 

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer.
        /// </devdoc>
        private void InitializeUI() { 
            UpdateFonts();
        } 
 
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e); 
            UpdateFonts();
        }

        private void UpdateFonts() { 
            _captionLabel.Font = new Font(Font.FontFamily, Font.Size + 2.0f, FontStyle.Bold, Font.Unit);
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
    internal abstract class TaskFormBase : DesignerForm {
 
        private System.Windows.Forms.Panel _taskPanel;
        private System.Windows.Forms.Label _bottomDividerLabel; 
        private System.Windows.Forms.Panel _headerPanel; 
        private System.Windows.Forms.Label _captionLabel;
        private System.Windows.Forms.PictureBox _glyphPictureBox; 

        /// <devdoc>
        /// Creates a new TaskForm with a given service provider.
        /// </devdoc> 
        public TaskFormBase(IServiceProvider serviceProvider)
            : base(serviceProvider) { 
            InitializeComponent(); 
            InitializeUI();
        } 

        protected Label CaptionLabel {
            get {
                return _captionLabel; 
            }
        } 
 
        /// <devdoc>
        /// A glyph for the wizard. 
        /// </devdoc>
        public Image Glyph {
            get {
                return _glyphPictureBox.Image; 
            }
            set { 
                _glyphPictureBox.Image = value; 
            }
        } 

        protected Panel TaskPanel {
            get {
                return _taskPanel; 
            }
        } 
 
        #region Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() { 

            this._taskPanel = new System.Windows.Forms.Panel(); 
            this._bottomDividerLabel = new System.Windows.Forms.Label(); 
            this._captionLabel = new System.Windows.Forms.Label();
            this._headerPanel = new System.Windows.Forms.Panel(); 
            this._glyphPictureBox = new System.Windows.Forms.PictureBox();
            this._headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._glyphPictureBox)).BeginInit();
            this.SuspendLayout(); 

            // 
            // _taskPanel 
            //
            this._taskPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._taskPanel.Location = new System.Drawing.Point(14, 78);
            this._taskPanel.Name = "_taskPanel"; 
            this._taskPanel.Size = new System.Drawing.Size(544, 274);
            this._taskPanel.TabIndex = 30; 
            // 
            // _bottomDividerLabel
            // 
            this._bottomDividerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._bottomDividerLabel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this._bottomDividerLabel.Location = new System.Drawing.Point(0, 366); 
            this._bottomDividerLabel.Name = "_bottomDividerLabel";
            this._bottomDividerLabel.Size = new System.Drawing.Size(572, 1); 
            this._bottomDividerLabel.TabIndex = 40; 
            //
            // _headerPanel 
            //
            this._headerPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._headerPanel.BackColor = System.Drawing.SystemColors.ControlLightLight; 
            this._headerPanel.Controls.Add(this._glyphPictureBox);
            this._headerPanel.Controls.Add(this._captionLabel); 
            this._headerPanel.Location = new System.Drawing.Point(0, 0); 
            this._headerPanel.Name = "_headerPanel";
            this._headerPanel.Size = new System.Drawing.Size(572, 64); 
            this._headerPanel.TabIndex = 10;
            //
            // _glyphPictureBox
            // 
            this._glyphPictureBox.Location = new System.Drawing.Point(0, 0);
            this._glyphPictureBox.Name = "_glyphPictureBox"; 
            this._glyphPictureBox.Size = new System.Drawing.Size(65, 64); 
            this._glyphPictureBox.TabIndex = 20;
            this._glyphPictureBox.TabStop = false; 
            //
            // _captionLabel
            //
            this._captionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._captionLabel.Location = new System.Drawing.Point(71, 17); 
            this._captionLabel.Name = "_captionLabel"; 
            this._captionLabel.Size = new System.Drawing.Size(487, 47);
            this._captionLabel.TabIndex = 10; 
            //
            // TaskForm
            //
            this.ClientSize = new System.Drawing.Size(572, 416); 
            this.Controls.Add(this._headerPanel);
            this.Controls.Add(this._bottomDividerLabel); 
            this.Controls.Add(this._taskPanel); 
            this.MinimumSize = new System.Drawing.Size(580, 450);
            this.Name = "TaskForm"; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this._headerPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._glyphPictureBox)).EndInit();
            this.ResumeLayout(false); 
            this.PerformLayout();
        } 
        #endregion 

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer.
        /// </devdoc>
        private void InitializeUI() { 
            UpdateFonts();
        } 
 
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e); 
            UpdateFonts();
        }

        private void UpdateFonts() { 
            _captionLabel.Font = new Font(Font.FontFamily, Font.Size + 2.0f, FontStyle.Bold, Font.Unit);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
