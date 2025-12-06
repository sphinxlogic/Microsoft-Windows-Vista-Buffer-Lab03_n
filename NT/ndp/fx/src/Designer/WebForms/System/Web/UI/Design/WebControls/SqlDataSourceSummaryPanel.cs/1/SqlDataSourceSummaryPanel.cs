//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceSummaryPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Common;
    using System.ComponentModel.Design.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <devdoc>
    /// Summary wizard panel for a SqlDataSource.
    /// </devdoc> 
    internal class SqlDataSourceSummaryPanel : WizardPanel {
        private System.Windows.Forms.TextBox _previewTextBox; 
        private System.Windows.Forms.Label _previewLabel; 
        private System.Windows.Forms.Button _testQueryButton;
        private System.Windows.Forms.Label _helpLabel; 
        private System.Windows.Forms.DataGridView _resultsGridView;

        private SqlDataSourceDesigner _sqlDataSourceDesigner;
 
        private DesignerDataConnection _dataConnection;
        private SqlDataSourceQuery _selectQuery; 
        private SqlDataSourceQuery _insertQuery; 
        private SqlDataSourceQuery _updateQuery;
        private SqlDataSourceQuery _deleteQuery; 


        /// <devdoc>
        /// Creates a new SqlDataSourceSummaryPanel. 
        /// </devdoc>
        public SqlDataSourceSummaryPanel(SqlDataSourceDesigner sqlDataSourceDesigner) { 
 
            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
            InitializeComponent();
            InitializeUI();
        }
 
        public void SetQueries(DesignerDataConnection dataConnection,
            SqlDataSourceQuery selectQuery, 
            SqlDataSourceQuery insertQuery, 
            SqlDataSourceQuery updateQuery,
            SqlDataSourceQuery deleteQuery) { 

            _dataConnection = dataConnection;
            _selectQuery = selectQuery;
            _insertQuery = insertQuery; 
            _updateQuery = updateQuery;
            _deleteQuery = deleteQuery; 
 
            _previewTextBox.Text = _selectQuery.Command;
        } 

        #region Designer generated code
        private void InitializeComponent() {
            this._resultsGridView = new System.Windows.Forms.DataGridView(); 
            this._testQueryButton = new System.Windows.Forms.Button();
            this._previewTextBox = new System.Windows.Forms.TextBox(); 
            this._previewLabel = new System.Windows.Forms.Label(); 
            this._helpLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._resultsGridView)).BeginInit(); 
            this.SuspendLayout();
            //
            // _helpLabel
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._helpLabel.Location = new System.Drawing.Point(0, 0); 
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(544, 32); 
            this._helpLabel.TabIndex = 10;
            //
            // _resultsGridView
            // 
            this._resultsGridView.AllowUserToAddRows = false;
            this._resultsGridView.AllowUserToDeleteRows = false; 
            this._resultsGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._resultsGridView.Location = new System.Drawing.Point(0, 38);
            this._resultsGridView.MultiSelect = false;
            this._resultsGridView.Name = "_resultsGridView";
            this._resultsGridView.ReadOnly = true; 
            this._resultsGridView.RowHeadersVisible = false;
            this._resultsGridView.Size = new System.Drawing.Size(544, 141); 
            this._resultsGridView.TabIndex = 20; 
            this._resultsGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnResultsGridViewDataError);
            // 
            // _testQueryButton
            //
            this._testQueryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._testQueryButton.Location = new System.Drawing.Point(424, 185); 
            this._testQueryButton.Name = "_testQueryButton";
            this._testQueryButton.Size = new System.Drawing.Size(120, 23); 
            this._testQueryButton.TabIndex = 30; 
            this._testQueryButton.Click += new System.EventHandler(this.OnTestQueryButtonClick);
            // 
            // _previewLabel
            //
            this._previewLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._previewLabel.Location = new System.Drawing.Point(0, 214);
            this._previewLabel.Name = "_previewLabel"; 
            this._previewLabel.Size = new System.Drawing.Size(544, 16); 
            this._previewLabel.TabIndex = 40;
            // 
            // _previewTextBox
            //
            this._previewTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._previewTextBox.BackColor = System.Drawing.SystemColors.Control;
            this._previewTextBox.Location = new System.Drawing.Point(0, 232); 
            this._previewTextBox.Multiline = true; 
            this._previewTextBox.Name = "_previewTextBox";
            this._previewTextBox.ReadOnly = true; 
            this._previewTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._previewTextBox.Size = new System.Drawing.Size(544, 42);
            this._previewTextBox.TabIndex = 50;
            this._previewTextBox.Text = ""; 
            //
            // SqlDataSourceSummaryPanel 
            // 
            this.Controls.Add(this._helpLabel);
            this.Controls.Add(this._previewLabel); 
            this.Controls.Add(this._previewTextBox);
            this.Controls.Add(this._testQueryButton);
            this.Controls.Add(this._resultsGridView);
            this.Name = "SqlDataSourceSummaryPanel"; 
            this.Size = new System.Drawing.Size(544, 274);
            ((System.ComponentModel.ISupportInitialize)(this._resultsGridView)).EndInit(); 
            this.ResumeLayout(false); 
        }
        #endregion 

        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() { 
            Caption = SR.GetString(SR.SqlDataSourceSummaryPanel_PanelCaption); 

            _testQueryButton.Text = SR.GetString(SR.SqlDataSourceSummaryPanel_TestQueryButton); 
            _previewLabel.Text = SR.GetString(SR.SqlDataSource_General_PreviewLabel);
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceSummaryPanel_HelpLabel);
            _resultsGridView.AccessibleName = SR.GetString(SR.SqlDataSourceSummaryPanel_ResultsAccessibleName);
        } 

        /// <devdoc> 
        /// Called when the user click Finish on the wizard. 
        /// </devdoc>
        protected internal override void OnComplete() { 
            // We use the property descriptors to reset and set values to
            // make sure we clear out any databindings or expressions that
            // may be set. However, we only set properties if they have
            // changed in order to try to preserve any previous settings. 

            PropertyDescriptor propDesc; 
            SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component; 

            if (sqlDataSource.DeleteCommand != _deleteQuery.Command) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["DeleteCommand"];
                propDesc.SetValue(sqlDataSource, _deleteQuery.Command);
            }
            if (sqlDataSource.DeleteCommandType != _deleteQuery.CommandType) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["DeleteCommandType"];
                propDesc.SetValue(sqlDataSource, _deleteQuery.CommandType); 
            } 

            if (sqlDataSource.InsertCommand != _insertQuery.Command) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["InsertCommand"];
                propDesc.SetValue(sqlDataSource, _insertQuery.Command);
            }
            if (sqlDataSource.InsertCommandType != _insertQuery.CommandType) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["InsertCommandType"];
                propDesc.SetValue(sqlDataSource, _insertQuery.CommandType); 
            } 

            if (sqlDataSource.SelectCommand != _selectQuery.Command) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["SelectCommand"];
                propDesc.SetValue(sqlDataSource, _selectQuery.Command);
            }
            if (sqlDataSource.SelectCommandType != _selectQuery.CommandType) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["SelectCommandType"];
                propDesc.SetValue(sqlDataSource, _selectQuery.CommandType); 
            } 

            if (sqlDataSource.UpdateCommand != _updateQuery.Command) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["UpdateCommand"];
                propDesc.SetValue(sqlDataSource, _updateQuery.Command);
            }
            if (sqlDataSource.UpdateCommandType != _updateQuery.CommandType) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["UpdateCommandType"];
                propDesc.SetValue(sqlDataSource, _updateQuery.CommandType); 
            } 

 
            // Collection properties are never databound or expression-built
            // so there is no need to go through the property descriptors.
            _sqlDataSourceDesigner.CopyList(_selectQuery.Parameters, sqlDataSource.SelectParameters);
            _sqlDataSourceDesigner.CopyList(_insertQuery.Parameters, sqlDataSource.InsertParameters); 
            _sqlDataSourceDesigner.CopyList(_updateQuery.Parameters, sqlDataSource.UpdateParameters);
            _sqlDataSourceDesigner.CopyList(_deleteQuery.Parameters, sqlDataSource.DeleteParameters); 
 
            // Try to refresh schema and ignore success status, just try to do it silently
            ParameterCollection parameters = new ParameterCollection(); 
            foreach (Parameter p in _selectQuery.Parameters) {
                parameters.Add(p);
            }
            _sqlDataSourceDesigner.RefreshSchema(_dataConnection, _selectQuery.Command, _selectQuery.CommandType, parameters, true); 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public override bool OnNext() { 
            return true;
        }

        /// <devdoc> 
        /// </devdoc>
        public override void OnPrevious() { 
        } 

        /// <devdoc> 
        /// Handles errors in the Test Query results grid. Basically we ignore all
        /// errors such as invalid image file formats, null values, etc.
        /// </devdoc>
        private void OnResultsGridViewDataError(object sender, DataGridViewDataErrorEventArgs e) { 
            e.ThrowException = false;
        } 
 
        /// <devdoc>
        /// </devdoc> 
        private void OnTestQueryButtonClick(object sender, System.EventArgs e) {
            // There is no need to re-infer parameters since we already got
            // the precise parameter names from the previous steps.
            ParameterCollection parameters = new ParameterCollection(); 
            foreach (Parameter parameter in _selectQuery.Parameters) {
                parameters.Add(new Parameter(parameter.Name, parameter.Type, parameter.DefaultValue)); 
            } 

            // If there are any parameters, prompt for type and value information 
            if (parameters.Count > 0) {
                SqlDataSourceParameterValueEditorForm parameterForm = new SqlDataSourceParameterValueEditorForm(ServiceProvider, parameters);
                DialogResult dialogResult = UIServiceHelper.ShowDialog(ServiceProvider, parameterForm);
                if (dialogResult == DialogResult.Cancel) { 
                    return;
                } 
            } 

            _resultsGridView.DataSource = null; 

            // Get schema from database
            DbCommand selectCommand = null;
            // 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 
                DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
 
                DbConnection connection = null;
                try {
                    connection = SqlDataSourceDesigner.GetDesignTimeConnection(ServiceProvider, _dataConnection);
                } 
                catch (Exception ex) {
                    if (connection == null) { 
                        UIServiceHelper.ShowError( 
                            ServiceProvider,
                            ex, 
                            SR.GetString(SR.SqlDataSourceSummaryPanel_CouldNotCreateConnection));
                        return;
                    }
                } 

                if (connection == null) { 
                    UIServiceHelper.ShowError( 
                        ServiceProvider,
                        SR.GetString(SR.SqlDataSourceSummaryPanel_CouldNotCreateConnection)); 
                    return;
                }
                selectCommand = _sqlDataSourceDesigner.BuildSelectCommand(factory, connection, _selectQuery.Command, parameters, _selectQuery.CommandType);
                DbDataAdapter adapter = SqlDataSourceDesigner.CreateDataAdapter(factory, selectCommand); 
                adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
 
                DataSet dataSet = new DataSet(); 
                adapter.Fill(dataSet);
 
                // Ensure that we actually got back a data table
                if (dataSet.Tables.Count == 0) {
                    UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.SqlDataSourceSummaryPanel_CannotExecuteQueryNoTables));
                    return; 
                }
 
                _resultsGridView.DataSource = dataSet.Tables[0]; 

                foreach (DataGridViewColumn column in _resultsGridView.Columns) { 
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                _resultsGridView.AutoResizeColumnHeadersHeight();
                _resultsGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells); 
            }
            catch (Exception ex) { 
                UIServiceHelper.ShowError(ServiceProvider, ex, SR.GetString(SR.SqlDataSourceSummaryPanel_CannotExecuteQuery)); 
            }
            finally { 
                if (selectCommand != null && selectCommand.Connection.State == ConnectionState.Open) {
                    selectCommand.Connection.Close();
                }
                Cursor.Current = originalCursor; 
            }
        } 
 
        /// <devdoc>
        /// </devdoc> 
        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);

            if (Visible) { 
                ParentWizard.NextButton.Enabled = false;
                ParentWizard.FinishButton.Enabled = true; 
            } 
        }
 
        public void ResetUI() {
            _resultsGridView.DataSource = null;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceSummaryPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Common;
    using System.ComponentModel.Design.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <devdoc>
    /// Summary wizard panel for a SqlDataSource.
    /// </devdoc> 
    internal class SqlDataSourceSummaryPanel : WizardPanel {
        private System.Windows.Forms.TextBox _previewTextBox; 
        private System.Windows.Forms.Label _previewLabel; 
        private System.Windows.Forms.Button _testQueryButton;
        private System.Windows.Forms.Label _helpLabel; 
        private System.Windows.Forms.DataGridView _resultsGridView;

        private SqlDataSourceDesigner _sqlDataSourceDesigner;
 
        private DesignerDataConnection _dataConnection;
        private SqlDataSourceQuery _selectQuery; 
        private SqlDataSourceQuery _insertQuery; 
        private SqlDataSourceQuery _updateQuery;
        private SqlDataSourceQuery _deleteQuery; 


        /// <devdoc>
        /// Creates a new SqlDataSourceSummaryPanel. 
        /// </devdoc>
        public SqlDataSourceSummaryPanel(SqlDataSourceDesigner sqlDataSourceDesigner) { 
 
            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
            InitializeComponent();
            InitializeUI();
        }
 
        public void SetQueries(DesignerDataConnection dataConnection,
            SqlDataSourceQuery selectQuery, 
            SqlDataSourceQuery insertQuery, 
            SqlDataSourceQuery updateQuery,
            SqlDataSourceQuery deleteQuery) { 

            _dataConnection = dataConnection;
            _selectQuery = selectQuery;
            _insertQuery = insertQuery; 
            _updateQuery = updateQuery;
            _deleteQuery = deleteQuery; 
 
            _previewTextBox.Text = _selectQuery.Command;
        } 

        #region Designer generated code
        private void InitializeComponent() {
            this._resultsGridView = new System.Windows.Forms.DataGridView(); 
            this._testQueryButton = new System.Windows.Forms.Button();
            this._previewTextBox = new System.Windows.Forms.TextBox(); 
            this._previewLabel = new System.Windows.Forms.Label(); 
            this._helpLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._resultsGridView)).BeginInit(); 
            this.SuspendLayout();
            //
            // _helpLabel
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._helpLabel.Location = new System.Drawing.Point(0, 0); 
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(544, 32); 
            this._helpLabel.TabIndex = 10;
            //
            // _resultsGridView
            // 
            this._resultsGridView.AllowUserToAddRows = false;
            this._resultsGridView.AllowUserToDeleteRows = false; 
            this._resultsGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._resultsGridView.Location = new System.Drawing.Point(0, 38);
            this._resultsGridView.MultiSelect = false;
            this._resultsGridView.Name = "_resultsGridView";
            this._resultsGridView.ReadOnly = true; 
            this._resultsGridView.RowHeadersVisible = false;
            this._resultsGridView.Size = new System.Drawing.Size(544, 141); 
            this._resultsGridView.TabIndex = 20; 
            this._resultsGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnResultsGridViewDataError);
            // 
            // _testQueryButton
            //
            this._testQueryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._testQueryButton.Location = new System.Drawing.Point(424, 185); 
            this._testQueryButton.Name = "_testQueryButton";
            this._testQueryButton.Size = new System.Drawing.Size(120, 23); 
            this._testQueryButton.TabIndex = 30; 
            this._testQueryButton.Click += new System.EventHandler(this.OnTestQueryButtonClick);
            // 
            // _previewLabel
            //
            this._previewLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._previewLabel.Location = new System.Drawing.Point(0, 214);
            this._previewLabel.Name = "_previewLabel"; 
            this._previewLabel.Size = new System.Drawing.Size(544, 16); 
            this._previewLabel.TabIndex = 40;
            // 
            // _previewTextBox
            //
            this._previewTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._previewTextBox.BackColor = System.Drawing.SystemColors.Control;
            this._previewTextBox.Location = new System.Drawing.Point(0, 232); 
            this._previewTextBox.Multiline = true; 
            this._previewTextBox.Name = "_previewTextBox";
            this._previewTextBox.ReadOnly = true; 
            this._previewTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._previewTextBox.Size = new System.Drawing.Size(544, 42);
            this._previewTextBox.TabIndex = 50;
            this._previewTextBox.Text = ""; 
            //
            // SqlDataSourceSummaryPanel 
            // 
            this.Controls.Add(this._helpLabel);
            this.Controls.Add(this._previewLabel); 
            this.Controls.Add(this._previewTextBox);
            this.Controls.Add(this._testQueryButton);
            this.Controls.Add(this._resultsGridView);
            this.Name = "SqlDataSourceSummaryPanel"; 
            this.Size = new System.Drawing.Size(544, 274);
            ((System.ComponentModel.ISupportInitialize)(this._resultsGridView)).EndInit(); 
            this.ResumeLayout(false); 
        }
        #endregion 

        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() { 
            Caption = SR.GetString(SR.SqlDataSourceSummaryPanel_PanelCaption); 

            _testQueryButton.Text = SR.GetString(SR.SqlDataSourceSummaryPanel_TestQueryButton); 
            _previewLabel.Text = SR.GetString(SR.SqlDataSource_General_PreviewLabel);
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceSummaryPanel_HelpLabel);
            _resultsGridView.AccessibleName = SR.GetString(SR.SqlDataSourceSummaryPanel_ResultsAccessibleName);
        } 

        /// <devdoc> 
        /// Called when the user click Finish on the wizard. 
        /// </devdoc>
        protected internal override void OnComplete() { 
            // We use the property descriptors to reset and set values to
            // make sure we clear out any databindings or expressions that
            // may be set. However, we only set properties if they have
            // changed in order to try to preserve any previous settings. 

            PropertyDescriptor propDesc; 
            SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component; 

            if (sqlDataSource.DeleteCommand != _deleteQuery.Command) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["DeleteCommand"];
                propDesc.SetValue(sqlDataSource, _deleteQuery.Command);
            }
            if (sqlDataSource.DeleteCommandType != _deleteQuery.CommandType) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["DeleteCommandType"];
                propDesc.SetValue(sqlDataSource, _deleteQuery.CommandType); 
            } 

            if (sqlDataSource.InsertCommand != _insertQuery.Command) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["InsertCommand"];
                propDesc.SetValue(sqlDataSource, _insertQuery.Command);
            }
            if (sqlDataSource.InsertCommandType != _insertQuery.CommandType) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["InsertCommandType"];
                propDesc.SetValue(sqlDataSource, _insertQuery.CommandType); 
            } 

            if (sqlDataSource.SelectCommand != _selectQuery.Command) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["SelectCommand"];
                propDesc.SetValue(sqlDataSource, _selectQuery.Command);
            }
            if (sqlDataSource.SelectCommandType != _selectQuery.CommandType) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["SelectCommandType"];
                propDesc.SetValue(sqlDataSource, _selectQuery.CommandType); 
            } 

            if (sqlDataSource.UpdateCommand != _updateQuery.Command) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["UpdateCommand"];
                propDesc.SetValue(sqlDataSource, _updateQuery.Command);
            }
            if (sqlDataSource.UpdateCommandType != _updateQuery.CommandType) { 
                propDesc = TypeDescriptor.GetProperties(sqlDataSource)["UpdateCommandType"];
                propDesc.SetValue(sqlDataSource, _updateQuery.CommandType); 
            } 

 
            // Collection properties are never databound or expression-built
            // so there is no need to go through the property descriptors.
            _sqlDataSourceDesigner.CopyList(_selectQuery.Parameters, sqlDataSource.SelectParameters);
            _sqlDataSourceDesigner.CopyList(_insertQuery.Parameters, sqlDataSource.InsertParameters); 
            _sqlDataSourceDesigner.CopyList(_updateQuery.Parameters, sqlDataSource.UpdateParameters);
            _sqlDataSourceDesigner.CopyList(_deleteQuery.Parameters, sqlDataSource.DeleteParameters); 
 
            // Try to refresh schema and ignore success status, just try to do it silently
            ParameterCollection parameters = new ParameterCollection(); 
            foreach (Parameter p in _selectQuery.Parameters) {
                parameters.Add(p);
            }
            _sqlDataSourceDesigner.RefreshSchema(_dataConnection, _selectQuery.Command, _selectQuery.CommandType, parameters, true); 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public override bool OnNext() { 
            return true;
        }

        /// <devdoc> 
        /// </devdoc>
        public override void OnPrevious() { 
        } 

        /// <devdoc> 
        /// Handles errors in the Test Query results grid. Basically we ignore all
        /// errors such as invalid image file formats, null values, etc.
        /// </devdoc>
        private void OnResultsGridViewDataError(object sender, DataGridViewDataErrorEventArgs e) { 
            e.ThrowException = false;
        } 
 
        /// <devdoc>
        /// </devdoc> 
        private void OnTestQueryButtonClick(object sender, System.EventArgs e) {
            // There is no need to re-infer parameters since we already got
            // the precise parameter names from the previous steps.
            ParameterCollection parameters = new ParameterCollection(); 
            foreach (Parameter parameter in _selectQuery.Parameters) {
                parameters.Add(new Parameter(parameter.Name, parameter.Type, parameter.DefaultValue)); 
            } 

            // If there are any parameters, prompt for type and value information 
            if (parameters.Count > 0) {
                SqlDataSourceParameterValueEditorForm parameterForm = new SqlDataSourceParameterValueEditorForm(ServiceProvider, parameters);
                DialogResult dialogResult = UIServiceHelper.ShowDialog(ServiceProvider, parameterForm);
                if (dialogResult == DialogResult.Cancel) { 
                    return;
                } 
            } 

            _resultsGridView.DataSource = null; 

            // Get schema from database
            DbCommand selectCommand = null;
            // 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 
                DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
 
                DbConnection connection = null;
                try {
                    connection = SqlDataSourceDesigner.GetDesignTimeConnection(ServiceProvider, _dataConnection);
                } 
                catch (Exception ex) {
                    if (connection == null) { 
                        UIServiceHelper.ShowError( 
                            ServiceProvider,
                            ex, 
                            SR.GetString(SR.SqlDataSourceSummaryPanel_CouldNotCreateConnection));
                        return;
                    }
                } 

                if (connection == null) { 
                    UIServiceHelper.ShowError( 
                        ServiceProvider,
                        SR.GetString(SR.SqlDataSourceSummaryPanel_CouldNotCreateConnection)); 
                    return;
                }
                selectCommand = _sqlDataSourceDesigner.BuildSelectCommand(factory, connection, _selectQuery.Command, parameters, _selectQuery.CommandType);
                DbDataAdapter adapter = SqlDataSourceDesigner.CreateDataAdapter(factory, selectCommand); 
                adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
 
                DataSet dataSet = new DataSet(); 
                adapter.Fill(dataSet);
 
                // Ensure that we actually got back a data table
                if (dataSet.Tables.Count == 0) {
                    UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.SqlDataSourceSummaryPanel_CannotExecuteQueryNoTables));
                    return; 
                }
 
                _resultsGridView.DataSource = dataSet.Tables[0]; 

                foreach (DataGridViewColumn column in _resultsGridView.Columns) { 
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                _resultsGridView.AutoResizeColumnHeadersHeight();
                _resultsGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells); 
            }
            catch (Exception ex) { 
                UIServiceHelper.ShowError(ServiceProvider, ex, SR.GetString(SR.SqlDataSourceSummaryPanel_CannotExecuteQuery)); 
            }
            finally { 
                if (selectCommand != null && selectCommand.Connection.State == ConnectionState.Open) {
                    selectCommand.Connection.Close();
                }
                Cursor.Current = originalCursor; 
            }
        } 
 
        /// <devdoc>
        /// </devdoc> 
        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);

            if (Visible) { 
                ParentWizard.NextButton.Enabled = false;
                ParentWizard.FinishButton.Enabled = true; 
            } 
        }
 
        public void ResetUI() {
            _resultsGridView.DataSource = null;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
