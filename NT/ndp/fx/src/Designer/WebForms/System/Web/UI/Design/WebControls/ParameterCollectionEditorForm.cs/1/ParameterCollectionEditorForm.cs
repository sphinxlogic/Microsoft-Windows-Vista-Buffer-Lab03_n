//------------------------------------------------------------------------------ 
// <copyright file="ParameterCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Collections;
    using System.ComponentModel;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
 
    /// <devdoc>
    /// A form for editing ParameterCollection objects. 
    /// This simply hosts the ParameterEditorUserControl.
    /// </devdoc>
    internal class ParameterCollectionEditorForm : DesignerForm {
        private ParameterCollection _parameters; 
        private System.Web.UI.Control _control;
 
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Button _cancelButton;
        private System.Web.UI.Design.WebControls.ParameterEditorUserControl _parameterEditorUserControl; 

        /// <devdoc>
        /// Creates a new ParameterCollectionEditorForm for a given ParameterCollection.
        /// </devdoc> 
        public ParameterCollectionEditorForm(IServiceProvider serviceProvider, ParameterCollection parameters, ControlDesigner designer) : base(serviceProvider) {
            Debug.Assert(parameters != null); 
            _parameters = parameters; 
            if (designer != null) {
                _control = designer.Component as System.Web.UI.Control; 
            }

            InitializeComponent();
            InitializeUI(); 

            ArrayList paramlist = new ArrayList(); 
            foreach (ICloneable parameter in parameters) { 
                object clonedParameter = parameter.Clone();
                if (designer != null) { 
                    designer.RegisterClone(parameter, clonedParameter);
                }
                paramlist.Add(clonedParameter);
            } 

            _parameterEditorUserControl.AddParameters((Parameter[])paramlist.ToArray(typeof(Parameter))); 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.Parameter.CollectionEditor";
            }
        } 

        #region Windows Form Designer generated code 
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent() {
            _okButton = new System.Windows.Forms.Button();
            _cancelButton = new System.Windows.Forms.Button(); 
            _parameterEditorUserControl = new System.Web.UI.Design.WebControls.ParameterEditorUserControl(ServiceProvider, _control);
            SuspendLayout(); 
            // 
            // parameterEditorUserControl
            // 
            _parameterEditorUserControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            _parameterEditorUserControl.Location = new System.Drawing.Point(12, 12);
            _parameterEditorUserControl.Size = new System.Drawing.Size(560, 278);
            _parameterEditorUserControl.TabIndex = 10; 
            //
            // okButton 
            // 
            _okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            _okButton.Location = new System.Drawing.Point(416, 299); 
            _okButton.TabIndex = 20;
            _okButton.Click += new System.EventHandler(OnOkButtonClick);
            //
            // cancelButton 
            //
            _cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            _cancelButton.Location = new System.Drawing.Point(497, 299); 
            _cancelButton.TabIndex = 30;
            _cancelButton.Click += new System.EventHandler(OnCancelButtonClick); 
            //
            // ParameterCollectionEditorForm
            //
            AcceptButton = _okButton; 
            CancelButton = _cancelButton;
            ClientSize = new System.Drawing.Size(584, 334); 
            Controls.Add(_parameterEditorUserControl); 
            Controls.Add(_cancelButton);
            Controls.Add(_okButton); 
            MinimumSize = new System.Drawing.Size(484, 272);

            InitializeForm();
 
            ResumeLayout(false);
 
        } 
        #endregion
 
        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer.
        /// </devdoc> 
        private void InitializeUI() {
            _okButton.Text = SR.GetString(SR.OK); 
            _cancelButton.Text = SR.GetString(SR.Cancel); 
            Text = SR.GetString(SR.ParameterCollectionEditorForm_Caption);
        } 

        /// <devdoc>
        /// The click event handler for the OK button.
        /// </devdoc> 
        private void OnOkButtonClick(System.Object sender, System.EventArgs e) {
            Parameter[] parameters = _parameterEditorUserControl.GetParameters(); 
 
            _parameters.Clear();
            foreach (Parameter p in parameters) { 
                _parameters.Add(p);
            }

            DialogResult = DialogResult.OK; 
            Close();
        } 
 
        /// <devdoc>
        /// The click event handler for the Cancel button. 
        /// </devdoc>
        private void OnCancelButtonClick(System.Object sender, System.EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close(); 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ParameterCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Collections;
    using System.ComponentModel;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
 
    /// <devdoc>
    /// A form for editing ParameterCollection objects. 
    /// This simply hosts the ParameterEditorUserControl.
    /// </devdoc>
    internal class ParameterCollectionEditorForm : DesignerForm {
        private ParameterCollection _parameters; 
        private System.Web.UI.Control _control;
 
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Button _cancelButton;
        private System.Web.UI.Design.WebControls.ParameterEditorUserControl _parameterEditorUserControl; 

        /// <devdoc>
        /// Creates a new ParameterCollectionEditorForm for a given ParameterCollection.
        /// </devdoc> 
        public ParameterCollectionEditorForm(IServiceProvider serviceProvider, ParameterCollection parameters, ControlDesigner designer) : base(serviceProvider) {
            Debug.Assert(parameters != null); 
            _parameters = parameters; 
            if (designer != null) {
                _control = designer.Component as System.Web.UI.Control; 
            }

            InitializeComponent();
            InitializeUI(); 

            ArrayList paramlist = new ArrayList(); 
            foreach (ICloneable parameter in parameters) { 
                object clonedParameter = parameter.Clone();
                if (designer != null) { 
                    designer.RegisterClone(parameter, clonedParameter);
                }
                paramlist.Add(clonedParameter);
            } 

            _parameterEditorUserControl.AddParameters((Parameter[])paramlist.ToArray(typeof(Parameter))); 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.Parameter.CollectionEditor";
            }
        } 

        #region Windows Form Designer generated code 
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent() {
            _okButton = new System.Windows.Forms.Button();
            _cancelButton = new System.Windows.Forms.Button(); 
            _parameterEditorUserControl = new System.Web.UI.Design.WebControls.ParameterEditorUserControl(ServiceProvider, _control);
            SuspendLayout(); 
            // 
            // parameterEditorUserControl
            // 
            _parameterEditorUserControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            _parameterEditorUserControl.Location = new System.Drawing.Point(12, 12);
            _parameterEditorUserControl.Size = new System.Drawing.Size(560, 278);
            _parameterEditorUserControl.TabIndex = 10; 
            //
            // okButton 
            // 
            _okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            _okButton.Location = new System.Drawing.Point(416, 299); 
            _okButton.TabIndex = 20;
            _okButton.Click += new System.EventHandler(OnOkButtonClick);
            //
            // cancelButton 
            //
            _cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            _cancelButton.Location = new System.Drawing.Point(497, 299); 
            _cancelButton.TabIndex = 30;
            _cancelButton.Click += new System.EventHandler(OnCancelButtonClick); 
            //
            // ParameterCollectionEditorForm
            //
            AcceptButton = _okButton; 
            CancelButton = _cancelButton;
            ClientSize = new System.Drawing.Size(584, 334); 
            Controls.Add(_parameterEditorUserControl); 
            Controls.Add(_cancelButton);
            Controls.Add(_okButton); 
            MinimumSize = new System.Drawing.Size(484, 272);

            InitializeForm();
 
            ResumeLayout(false);
 
        } 
        #endregion
 
        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer.
        /// </devdoc> 
        private void InitializeUI() {
            _okButton.Text = SR.GetString(SR.OK); 
            _cancelButton.Text = SR.GetString(SR.Cancel); 
            Text = SR.GetString(SR.ParameterCollectionEditorForm_Caption);
        } 

        /// <devdoc>
        /// The click event handler for the OK button.
        /// </devdoc> 
        private void OnOkButtonClick(System.Object sender, System.EventArgs e) {
            Parameter[] parameters = _parameterEditorUserControl.GetParameters(); 
 
            _parameters.Clear();
            foreach (Parameter p in parameters) { 
                _parameters.Add(p);
            }

            DialogResult = DialogResult.OK; 
            Close();
        } 
 
        /// <devdoc>
        /// The click event handler for the Cancel button. 
        /// </devdoc>
        private void OnCancelButtonClick(System.Object sender, System.EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close(); 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
