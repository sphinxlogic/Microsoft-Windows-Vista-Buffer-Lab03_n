//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceQueryEditorForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel.Design.Data;
    using System.Data; 
    using System.Data.Common;
    using System.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Collections;
    using System.Collections.Generic; 
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Globalization; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <devdoc> 
    /// Query editor for SqlDataSource queries. 
    /// Enables a user to edit query commands and add/remove and infer parameters.
    /// </devdoc> 
    internal class SqlDataSourceQueryEditorForm : DesignerForm {

        private System.Windows.Forms.Label _commandLabel;
        private System.Windows.Forms.TextBox _commandTextBox; 
        private ParameterEditorUserControl _parameterEditorUserControl;
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Button _cancelButton; 
        private System.Windows.Forms.Button _inferParametersButton;
        private System.Windows.Forms.Button _queryBuilderButton; 

        private QueryBuilderMode _queryBuilderMode;
        private IDataEnvironment _dataEnvironment;
        private SqlDataSourceCommandType _commandType; 
        private DesignerDataConnection _dataConnection;
        private IList _originalParameters; 
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
 
        /// <devdoc>
        /// Create a new instance of SqlDataSourceQueryEditorForm using a given
        /// connection string, command type (Select/Insert/etc.), command text,
        /// and a list of parameters. 
        /// The connection string can be null (this is the case for AccessDataSource
        /// when the MDB file does not exist or cannot be mapped to a local file). 
        /// </devdoc> 
        public SqlDataSourceQueryEditorForm(IServiceProvider serviceProvider, SqlDataSourceDesigner sqlDataSourceDesigner,
            string providerName, string connectionString, 
            DataSourceOperation operation, SqlDataSourceCommandType commandType, string command, IList originalParameters) : base(serviceProvider) {

            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 

            InitializeComponent(); 
            InitializeUI(); 

            if (String.IsNullOrEmpty(providerName)) { 
                providerName = SqlDataSourceDesigner.DefaultProviderName;
            }
            _dataConnection = new DesignerDataConnection(String.Empty, providerName, connectionString);
 
            _commandType = commandType;
            _commandTextBox.Text = command; 
            _originalParameters = originalParameters; 
            string operationText = Enum.GetName(typeof(DataSourceOperation), operation).ToUpperInvariant();
            _commandLabel.Text = SR.GetString(SR.SqlDataSourceQueryEditorForm_CommandLabel, operationText); 

            ArrayList parameterList = new ArrayList(originalParameters.Count);
            sqlDataSourceDesigner.CopyList(originalParameters, parameterList);
            _parameterEditorUserControl.AddParameters((Parameter[])parameterList.ToArray(typeof(Parameter))); 
            _commandTextBox.Select(0, 0);
 
            // Set the mode for the query builder 
            switch (operation) {
                case DataSourceOperation.Delete: 
                    _queryBuilderMode = QueryBuilderMode.Delete;
                    break;
                case DataSourceOperation.Insert:
                    _queryBuilderMode = QueryBuilderMode.Insert; 
                    break;
                case DataSourceOperation.Select: 
                    _queryBuilderMode = QueryBuilderMode.Select; 
                    break;
                case DataSourceOperation.Update: 
                    _queryBuilderMode = QueryBuilderMode.Update;
                    break;
                default:
                    Debug.Fail("Invalid DataSourceOperation"); 
                    break;
            } 
        } 

        /// <devdoc> 
        /// The command text that is in the command textbox.
        /// </devdoc>
        public string Command {
            get { 
                return _commandTextBox.Text;
            } 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.SqlDataSource.QueryEditor";
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
            _inferParametersButton = new System.Windows.Forms.Button();
            _queryBuilderButton = new System.Windows.Forms.Button(); 
 
            _commandLabel = new System.Windows.Forms.Label();
            _commandTextBox = new System.Windows.Forms.TextBox(); 
            _parameterEditorUserControl = new ParameterEditorUserControl(ServiceProvider, (SqlDataSource)_sqlDataSourceDesigner.Component);
            SuspendLayout();
            //
            // okButton 
            //
            _okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            _okButton.Location = new System.Drawing.Point(377, 379); 
            _okButton.TabIndex = 150;
            _okButton.Click += new System.EventHandler(OnOkButtonClick); 
            //
            // cancelButton
            //
            _cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            _cancelButton.Location = new System.Drawing.Point(457, 379);
            _cancelButton.TabIndex = 160; 
            _cancelButton.Click += new System.EventHandler(OnCancelButtonClick); 
            //
            // _commandLabel 
            //
            _commandLabel.Location = new System.Drawing.Point(12, 12);
            _commandLabel.Size = new System.Drawing.Size(200, 16);
            _commandLabel.TabIndex = 10; 
            //
            // _commandTextBox 
            // 
            _commandTextBox.AcceptsReturn = true;
            _commandTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
            _commandTextBox.Location = new System.Drawing.Point(12, 30);
            _commandTextBox.Multiline = true;
            _commandTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            _commandTextBox.Size = new System.Drawing.Size(520, 78); 
            _commandTextBox.TabIndex = 20;
            // 
            // inferParametersButton 
            //
            _inferParametersButton.AutoSize = true; 
            _inferParametersButton.Location = new System.Drawing.Point(12, 112);
            _inferParametersButton.Size = new System.Drawing.Size(128, 23);
            _inferParametersButton.TabIndex = 30;
            _inferParametersButton.Click += new System.EventHandler(OnInferParametersButtonClick); 
            //
            // queryBuilderButton 
            // 
            _queryBuilderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            _queryBuilderButton.AutoSize = true; 
            _queryBuilderButton.Location = new System.Drawing.Point(404, 112);
            _queryBuilderButton.Size = new System.Drawing.Size(128, 23);
            _queryBuilderButton.TabIndex = 40;
            _queryBuilderButton.Click += new System.EventHandler(OnQueryBuilderButtonClick); 
            //
            // _parameterEditorUserControl 
            // 
            _parameterEditorUserControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            _parameterEditorUserControl.Location = new System.Drawing.Point(12, 144); 
            _parameterEditorUserControl.Size = new System.Drawing.Size(520, 224);
            _parameterEditorUserControl.TabIndex = 50;
            //
            // CommandEditorForm 
            //
            AcceptButton = _okButton; 
            CancelButton = _cancelButton; 
            ClientSize = new System.Drawing.Size(544, 410);
            Controls.Add(_queryBuilderButton); 
            Controls.Add(_inferParametersButton);
            Controls.Add(_commandTextBox);
            Controls.Add(_commandLabel);
            Controls.Add(_cancelButton); 
            Controls.Add(_okButton);
            Controls.Add(_parameterEditorUserControl); 
            MinimumSize = new System.Drawing.Size(488, 440); 

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
            _inferParametersButton.Text = SR.GetString(SR.SqlDataSourceQueryEditorForm_InferParametersButton); 
            _queryBuilderButton.Text = SR.GetString(SR.SqlDataSourceQueryEditorForm_QueryBuilderButton);
            Text = SR.GetString(SR.SqlDataSourceQueryEditorForm_Caption); 
 
            // Disable query builder button if the service is not available
            _dataEnvironment = (IDataEnvironment)ServiceProvider.GetService(typeof(IDataEnvironment)); 
            _queryBuilderButton.Enabled = (_dataEnvironment != null);
        }

        /// <devdoc> 
        /// The Click event handler for the Cancel button.
        /// </devdoc> 
        private void OnCancelButtonClick(System.Object sender, System.EventArgs e) { 
            DialogResult = DialogResult.Cancel;
            Close(); 
        }

        /// <devdoc>
        /// The Click event handler for the Infer Parameters button. 
        /// </devdoc>
        private void OnInferParametersButtonClick(System.Object sender, System.EventArgs e) { 
            // Don't do anything if there is no command set 
            if (_commandTextBox.Text.Trim().Length == 0) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.SqlDataSourceQueryEditorForm_InferNeedsCommand)); 
                return;
            }

            Parameter[] derivedParameters = _sqlDataSourceDesigner.InferParameterNames(_dataConnection, _commandTextBox.Text, _commandType); 

            if (derivedParameters != null) { 
                // Get a list of all the names currently used by parameters (including duplicates) 
                Parameter[] currentParameters = _parameterEditorUserControl.GetParameters();
                StringCollection currentNames = new StringCollection(); 
                foreach (Parameter parameter in currentParameters) {
                    currentNames.Add(parameter.Name);
                }
 
                // Go through the list of derived parameters and only pick out the ones that do not already exist
                bool supportsNamedParameters = true; 
                try { 
                    DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
                    supportsNamedParameters = SqlDataSourceDesigner.SupportsNamedParameters(factory); 
                }
                catch {
                    // In case there's an unsupported provider we just assume it supports named parameters (pretty optimistic)
                } 

                if (supportsNamedParameters) { 
                    // Named parameters are supported, so add new uniquely named parameters 
                    List<Parameter> derivedParametersToAdd = new List<Parameter>();
                    foreach (Parameter derivedParameter in derivedParameters) { 
                        if (!currentNames.Contains(derivedParameter.Name)) {
                            derivedParametersToAdd.Add(derivedParameter);
                        }
                        else { 
                            currentNames.Remove(derivedParameter.Name);
                        } 
                    } 
                    _parameterEditorUserControl.AddParameters(derivedParametersToAdd.ToArray());
                } 
                else {
                    // Named parameters are not supported, so add new parameters based on index and direction

                    List<Parameter> remainingDerivedParameters = new List<Parameter>(); 
                    foreach (Parameter p in derivedParameters) {
                        remainingDerivedParameters.Add(p); 
                    } 

                    // Go through all the current parameters and remove matching one from the new derived parameters 
                    foreach (Parameter currentParameter in currentParameters) {
                        Parameter foundParameter = null;
                        foreach (Parameter remainingParameter in remainingDerivedParameters) {
                            if (remainingParameter.Direction == currentParameter.Direction) { 
                                foundParameter = remainingParameter;
                                break; 
                            } 
                        }
                        if (foundParameter != null) { 
                            remainingDerivedParameters.Remove(foundParameter);
                        }
                    }
 
                    // Then add all the remaining derived parameters to the list
                    _parameterEditorUserControl.AddParameters(remainingDerivedParameters.ToArray()); 
                } 
            }
        } 

        /// <devdoc>
        /// The Click event handler for the OK button.
        /// </devdoc> 
        private void OnOkButtonClick(System.Object sender, System.EventArgs e) {
            _sqlDataSourceDesigner.CopyList(_parameterEditorUserControl.GetParameters(), _originalParameters); 
 
            DialogResult = DialogResult.OK;
            Close(); 
        }

        /// <devdoc>
        /// Launch the query builder. 
        /// </devdoc>
        private void OnQueryBuilderButtonClick(System.Object sender, System.EventArgs e) { 
            if ((_dataConnection.ConnectionString == null) || (_dataConnection.ConnectionString.Trim().Length == 0)) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.SqlDataSourceQueryEditorForm_QueryBuilderNeedsConnectionString));
                return; 
            }

            // Launch query builder
            Debug.Assert(_dataEnvironment != null); 
            string newQuery = _dataEnvironment.BuildQuery(this, _dataConnection, _queryBuilderMode, _commandTextBox.Text);
            if ((newQuery != null) && (newQuery.Length > 0)) { 
                _commandTextBox.Text = newQuery; 
            }
 
            _commandTextBox.Focus();
            _commandTextBox.Select(0, 0);
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceQueryEditorForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel.Design.Data;
    using System.Data; 
    using System.Data.Common;
    using System.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Collections;
    using System.Collections.Generic; 
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Globalization; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <devdoc> 
    /// Query editor for SqlDataSource queries. 
    /// Enables a user to edit query commands and add/remove and infer parameters.
    /// </devdoc> 
    internal class SqlDataSourceQueryEditorForm : DesignerForm {

        private System.Windows.Forms.Label _commandLabel;
        private System.Windows.Forms.TextBox _commandTextBox; 
        private ParameterEditorUserControl _parameterEditorUserControl;
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Button _cancelButton; 
        private System.Windows.Forms.Button _inferParametersButton;
        private System.Windows.Forms.Button _queryBuilderButton; 

        private QueryBuilderMode _queryBuilderMode;
        private IDataEnvironment _dataEnvironment;
        private SqlDataSourceCommandType _commandType; 
        private DesignerDataConnection _dataConnection;
        private IList _originalParameters; 
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
 
        /// <devdoc>
        /// Create a new instance of SqlDataSourceQueryEditorForm using a given
        /// connection string, command type (Select/Insert/etc.), command text,
        /// and a list of parameters. 
        /// The connection string can be null (this is the case for AccessDataSource
        /// when the MDB file does not exist or cannot be mapped to a local file). 
        /// </devdoc> 
        public SqlDataSourceQueryEditorForm(IServiceProvider serviceProvider, SqlDataSourceDesigner sqlDataSourceDesigner,
            string providerName, string connectionString, 
            DataSourceOperation operation, SqlDataSourceCommandType commandType, string command, IList originalParameters) : base(serviceProvider) {

            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 

            InitializeComponent(); 
            InitializeUI(); 

            if (String.IsNullOrEmpty(providerName)) { 
                providerName = SqlDataSourceDesigner.DefaultProviderName;
            }
            _dataConnection = new DesignerDataConnection(String.Empty, providerName, connectionString);
 
            _commandType = commandType;
            _commandTextBox.Text = command; 
            _originalParameters = originalParameters; 
            string operationText = Enum.GetName(typeof(DataSourceOperation), operation).ToUpperInvariant();
            _commandLabel.Text = SR.GetString(SR.SqlDataSourceQueryEditorForm_CommandLabel, operationText); 

            ArrayList parameterList = new ArrayList(originalParameters.Count);
            sqlDataSourceDesigner.CopyList(originalParameters, parameterList);
            _parameterEditorUserControl.AddParameters((Parameter[])parameterList.ToArray(typeof(Parameter))); 
            _commandTextBox.Select(0, 0);
 
            // Set the mode for the query builder 
            switch (operation) {
                case DataSourceOperation.Delete: 
                    _queryBuilderMode = QueryBuilderMode.Delete;
                    break;
                case DataSourceOperation.Insert:
                    _queryBuilderMode = QueryBuilderMode.Insert; 
                    break;
                case DataSourceOperation.Select: 
                    _queryBuilderMode = QueryBuilderMode.Select; 
                    break;
                case DataSourceOperation.Update: 
                    _queryBuilderMode = QueryBuilderMode.Update;
                    break;
                default:
                    Debug.Fail("Invalid DataSourceOperation"); 
                    break;
            } 
        } 

        /// <devdoc> 
        /// The command text that is in the command textbox.
        /// </devdoc>
        public string Command {
            get { 
                return _commandTextBox.Text;
            } 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.SqlDataSource.QueryEditor";
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
            _inferParametersButton = new System.Windows.Forms.Button();
            _queryBuilderButton = new System.Windows.Forms.Button(); 
 
            _commandLabel = new System.Windows.Forms.Label();
            _commandTextBox = new System.Windows.Forms.TextBox(); 
            _parameterEditorUserControl = new ParameterEditorUserControl(ServiceProvider, (SqlDataSource)_sqlDataSourceDesigner.Component);
            SuspendLayout();
            //
            // okButton 
            //
            _okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            _okButton.Location = new System.Drawing.Point(377, 379); 
            _okButton.TabIndex = 150;
            _okButton.Click += new System.EventHandler(OnOkButtonClick); 
            //
            // cancelButton
            //
            _cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            _cancelButton.Location = new System.Drawing.Point(457, 379);
            _cancelButton.TabIndex = 160; 
            _cancelButton.Click += new System.EventHandler(OnCancelButtonClick); 
            //
            // _commandLabel 
            //
            _commandLabel.Location = new System.Drawing.Point(12, 12);
            _commandLabel.Size = new System.Drawing.Size(200, 16);
            _commandLabel.TabIndex = 10; 
            //
            // _commandTextBox 
            // 
            _commandTextBox.AcceptsReturn = true;
            _commandTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
            _commandTextBox.Location = new System.Drawing.Point(12, 30);
            _commandTextBox.Multiline = true;
            _commandTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            _commandTextBox.Size = new System.Drawing.Size(520, 78); 
            _commandTextBox.TabIndex = 20;
            // 
            // inferParametersButton 
            //
            _inferParametersButton.AutoSize = true; 
            _inferParametersButton.Location = new System.Drawing.Point(12, 112);
            _inferParametersButton.Size = new System.Drawing.Size(128, 23);
            _inferParametersButton.TabIndex = 30;
            _inferParametersButton.Click += new System.EventHandler(OnInferParametersButtonClick); 
            //
            // queryBuilderButton 
            // 
            _queryBuilderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            _queryBuilderButton.AutoSize = true; 
            _queryBuilderButton.Location = new System.Drawing.Point(404, 112);
            _queryBuilderButton.Size = new System.Drawing.Size(128, 23);
            _queryBuilderButton.TabIndex = 40;
            _queryBuilderButton.Click += new System.EventHandler(OnQueryBuilderButtonClick); 
            //
            // _parameterEditorUserControl 
            // 
            _parameterEditorUserControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            _parameterEditorUserControl.Location = new System.Drawing.Point(12, 144); 
            _parameterEditorUserControl.Size = new System.Drawing.Size(520, 224);
            _parameterEditorUserControl.TabIndex = 50;
            //
            // CommandEditorForm 
            //
            AcceptButton = _okButton; 
            CancelButton = _cancelButton; 
            ClientSize = new System.Drawing.Size(544, 410);
            Controls.Add(_queryBuilderButton); 
            Controls.Add(_inferParametersButton);
            Controls.Add(_commandTextBox);
            Controls.Add(_commandLabel);
            Controls.Add(_cancelButton); 
            Controls.Add(_okButton);
            Controls.Add(_parameterEditorUserControl); 
            MinimumSize = new System.Drawing.Size(488, 440); 

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
            _inferParametersButton.Text = SR.GetString(SR.SqlDataSourceQueryEditorForm_InferParametersButton); 
            _queryBuilderButton.Text = SR.GetString(SR.SqlDataSourceQueryEditorForm_QueryBuilderButton);
            Text = SR.GetString(SR.SqlDataSourceQueryEditorForm_Caption); 
 
            // Disable query builder button if the service is not available
            _dataEnvironment = (IDataEnvironment)ServiceProvider.GetService(typeof(IDataEnvironment)); 
            _queryBuilderButton.Enabled = (_dataEnvironment != null);
        }

        /// <devdoc> 
        /// The Click event handler for the Cancel button.
        /// </devdoc> 
        private void OnCancelButtonClick(System.Object sender, System.EventArgs e) { 
            DialogResult = DialogResult.Cancel;
            Close(); 
        }

        /// <devdoc>
        /// The Click event handler for the Infer Parameters button. 
        /// </devdoc>
        private void OnInferParametersButtonClick(System.Object sender, System.EventArgs e) { 
            // Don't do anything if there is no command set 
            if (_commandTextBox.Text.Trim().Length == 0) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.SqlDataSourceQueryEditorForm_InferNeedsCommand)); 
                return;
            }

            Parameter[] derivedParameters = _sqlDataSourceDesigner.InferParameterNames(_dataConnection, _commandTextBox.Text, _commandType); 

            if (derivedParameters != null) { 
                // Get a list of all the names currently used by parameters (including duplicates) 
                Parameter[] currentParameters = _parameterEditorUserControl.GetParameters();
                StringCollection currentNames = new StringCollection(); 
                foreach (Parameter parameter in currentParameters) {
                    currentNames.Add(parameter.Name);
                }
 
                // Go through the list of derived parameters and only pick out the ones that do not already exist
                bool supportsNamedParameters = true; 
                try { 
                    DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
                    supportsNamedParameters = SqlDataSourceDesigner.SupportsNamedParameters(factory); 
                }
                catch {
                    // In case there's an unsupported provider we just assume it supports named parameters (pretty optimistic)
                } 

                if (supportsNamedParameters) { 
                    // Named parameters are supported, so add new uniquely named parameters 
                    List<Parameter> derivedParametersToAdd = new List<Parameter>();
                    foreach (Parameter derivedParameter in derivedParameters) { 
                        if (!currentNames.Contains(derivedParameter.Name)) {
                            derivedParametersToAdd.Add(derivedParameter);
                        }
                        else { 
                            currentNames.Remove(derivedParameter.Name);
                        } 
                    } 
                    _parameterEditorUserControl.AddParameters(derivedParametersToAdd.ToArray());
                } 
                else {
                    // Named parameters are not supported, so add new parameters based on index and direction

                    List<Parameter> remainingDerivedParameters = new List<Parameter>(); 
                    foreach (Parameter p in derivedParameters) {
                        remainingDerivedParameters.Add(p); 
                    } 

                    // Go through all the current parameters and remove matching one from the new derived parameters 
                    foreach (Parameter currentParameter in currentParameters) {
                        Parameter foundParameter = null;
                        foreach (Parameter remainingParameter in remainingDerivedParameters) {
                            if (remainingParameter.Direction == currentParameter.Direction) { 
                                foundParameter = remainingParameter;
                                break; 
                            } 
                        }
                        if (foundParameter != null) { 
                            remainingDerivedParameters.Remove(foundParameter);
                        }
                    }
 
                    // Then add all the remaining derived parameters to the list
                    _parameterEditorUserControl.AddParameters(remainingDerivedParameters.ToArray()); 
                } 
            }
        } 

        /// <devdoc>
        /// The Click event handler for the OK button.
        /// </devdoc> 
        private void OnOkButtonClick(System.Object sender, System.EventArgs e) {
            _sqlDataSourceDesigner.CopyList(_parameterEditorUserControl.GetParameters(), _originalParameters); 
 
            DialogResult = DialogResult.OK;
            Close(); 
        }

        /// <devdoc>
        /// Launch the query builder. 
        /// </devdoc>
        private void OnQueryBuilderButtonClick(System.Object sender, System.EventArgs e) { 
            if ((_dataConnection.ConnectionString == null) || (_dataConnection.ConnectionString.Trim().Length == 0)) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.SqlDataSourceQueryEditorForm_QueryBuilderNeedsConnectionString));
                return; 
            }

            // Launch query builder
            Debug.Assert(_dataEnvironment != null); 
            string newQuery = _dataEnvironment.BuildQuery(this, _dataConnection, _queryBuilderMode, _commandTextBox.Text);
            if ((newQuery != null) && (newQuery.Length > 0)) { 
                _commandTextBox.Text = newQuery; 
            }
 
            _commandTextBox.Focus();
            _commandTextBox.Select(0, 0);
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
