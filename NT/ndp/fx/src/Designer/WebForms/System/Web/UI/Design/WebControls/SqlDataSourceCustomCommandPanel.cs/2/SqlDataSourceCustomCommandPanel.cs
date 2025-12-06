//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceCustomCommandPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
    using System.ComponentModel.Design.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
 
    /// <devdoc>
    /// Wizard panel for editing commands and parameters for a SqlDataSource.
    /// </devdoc>
    internal class SqlDataSourceCustomCommandPanel : WizardPanel { 
        private System.Windows.Forms.TabPage _selectTabPage;
        private System.Windows.Forms.TabPage _updateTabPage; 
        private System.Windows.Forms.TabPage _insertTabPage; 
        private System.Windows.Forms.TabPage _deleteTabPage;
        private SqlDataSourceCustomCommandEditor _selectCommandEditor; 
        private SqlDataSourceCustomCommandEditor _insertCommandEditor;
        private SqlDataSourceCustomCommandEditor _updateCommandEditor;
        private SqlDataSourceCustomCommandEditor _deleteCommandEditor;
        private System.Windows.Forms.TabControl _commandsTabControl; 
        private System.Windows.Forms.Label _helpLabel;
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner; 
        private DesignerDataConnection _dataConnection;
 

        /// <devdoc>
        /// Creates a new SqlDataSourceCustomCommandPanel.
        /// </devdoc> 
        public SqlDataSourceCustomCommandPanel(SqlDataSourceDesigner sqlDataSourceDesigner) {
            Debug.Assert(sqlDataSourceDesigner != null); 
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
            InitializeComponent();
            InitializeUI(); 

            _selectCommandEditor.SetCommandData(_sqlDataSourceDesigner, QueryBuilderMode.Select);
            _insertCommandEditor.SetCommandData(_sqlDataSourceDesigner, QueryBuilderMode.Insert);
            _updateCommandEditor.SetCommandData(_sqlDataSourceDesigner, QueryBuilderMode.Update); 
            _deleteCommandEditor.SetCommandData(_sqlDataSourceDesigner, QueryBuilderMode.Delete);
        } 
 

        #region Designer generated code 
        private void InitializeComponent() {
            this._commandsTabControl = new System.Windows.Forms.TabControl();
            this._selectTabPage = new System.Windows.Forms.TabPage();
            this._selectCommandEditor = new SqlDataSourceCustomCommandEditor(); 
            this._updateTabPage = new System.Windows.Forms.TabPage();
            this._updateCommandEditor = new SqlDataSourceCustomCommandEditor(); 
            this._insertTabPage = new System.Windows.Forms.TabPage(); 
            this._insertCommandEditor = new SqlDataSourceCustomCommandEditor();
            this._deleteTabPage = new System.Windows.Forms.TabPage(); 
            this._deleteCommandEditor = new SqlDataSourceCustomCommandEditor();
            this._helpLabel = new System.Windows.Forms.Label();
            this._commandsTabControl.SuspendLayout();
            this._selectTabPage.SuspendLayout(); 
            this._updateTabPage.SuspendLayout();
            this._insertTabPage.SuspendLayout(); 
            this._deleteTabPage.SuspendLayout(); 
            this.SuspendLayout();
            // 
            // _helpLabel
            //
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._helpLabel.Location = new System.Drawing.Point(0, 0);
            this._helpLabel.Name = "_helpLabel"; 
            this._helpLabel.Size = new System.Drawing.Size(544, 16); 
            this._helpLabel.TabIndex = 10;
            // 
            // _commandsTabControl
            //
            this._commandsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._commandsTabControl.Controls.Add(this._selectTabPage); 
            this._commandsTabControl.Controls.Add(this._updateTabPage); 
            this._commandsTabControl.Controls.Add(this._insertTabPage);
            this._commandsTabControl.Controls.Add(this._deleteTabPage); 
            this._commandsTabControl.Location = new System.Drawing.Point(0, 22);
            this._commandsTabControl.Name = "_commandsTabControl";
            this._commandsTabControl.SelectedIndex = 0;
            this._commandsTabControl.ShowToolTips = true; 
            this._commandsTabControl.Size = new System.Drawing.Size(544, 252);
            this._commandsTabControl.TabIndex = 20; 
            // 
            // _selectTabPage
            // 
            this._selectTabPage.Controls.Add(this._selectCommandEditor);
            this._selectTabPage.Location = new System.Drawing.Point(4, 22);
            this._selectTabPage.Name = "_selectTabPage";
            this._selectTabPage.Size = new System.Drawing.Size(536, 226); 
            this._selectTabPage.TabIndex = 10;
            this._selectTabPage.Text = "SELECT"; 
            // 
            // _selectCommandEditor
            // 
            this._selectCommandEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._selectCommandEditor.Location = new System.Drawing.Point(0, 0);
            this._selectCommandEditor.Name = "_selectCommandEditor";
            this._selectCommandEditor.TabIndex = 10; 
            this._selectCommandEditor.CommandChanged += new System.EventHandler(this.OnSelectCommandChanged);
            // 
            // _updateTabPage 
            //
            this._updateTabPage.Controls.Add(this._updateCommandEditor); 
            this._updateTabPage.Location = new System.Drawing.Point(4, 22);
            this._updateTabPage.Name = "_updateTabPage";
            this._updateTabPage.Size = new System.Drawing.Size(536, 226);
            this._updateTabPage.TabIndex = 20; 
            this._updateTabPage.Text = "UPDATE";
            // 
            // _updateCommandEditor 
            //
            this._updateCommandEditor.Dock = System.Windows.Forms.DockStyle.Fill; 
            this._updateCommandEditor.Location = new System.Drawing.Point(0, 0);
            this._updateCommandEditor.Name = "_updateCommandEditor";
            this._updateCommandEditor.TabIndex = 10;
            // 
            // _insertTabPage
            // 
            this._insertTabPage.Controls.Add(this._insertCommandEditor); 
            this._insertTabPage.Location = new System.Drawing.Point(4, 22);
            this._insertTabPage.Name = "_insertTabPage"; 
            this._insertTabPage.Size = new System.Drawing.Size(536, 226);
            this._insertTabPage.TabIndex = 30;
            this._insertTabPage.Text = "INSERT";
            // 
            // _insertCommandEditor
            // 
            this._insertCommandEditor.Dock = System.Windows.Forms.DockStyle.Fill; 
            this._insertCommandEditor.Location = new System.Drawing.Point(0, 0);
            this._insertCommandEditor.Name = "_insertCommandEditor"; 
            this._insertCommandEditor.TabIndex = 10;
            //
            // _deleteTabPage
            // 
            this._deleteTabPage.Controls.Add(this._deleteCommandEditor);
            this._deleteTabPage.Location = new System.Drawing.Point(4, 22); 
            this._deleteTabPage.Name = "_deleteTabPage"; 
            this._deleteTabPage.Size = new System.Drawing.Size(522, 226);
            this._deleteTabPage.TabIndex = 40; 
            this._deleteTabPage.Text = "DELETE";
            //
            // _deleteCommandEditor
            // 
            this._deleteCommandEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._deleteCommandEditor.Location = new System.Drawing.Point(0, 0); 
            this._deleteCommandEditor.Name = "_deleteCommandEditor"; 
            this._deleteCommandEditor.TabIndex = 10;
            // 
            // SqlDataSourceCustomCommandPanel
            //
            this.Controls.Add(this._helpLabel);
            this.Controls.Add(this._commandsTabControl); 
            this.Name = "SqlDataSourceCustomCommandPanel";
            this.Size = new System.Drawing.Size(544, 274); 
            this._commandsTabControl.ResumeLayout(false); 
            this._selectTabPage.ResumeLayout(false);
            this._updateTabPage.ResumeLayout(false); 
            this._insertTabPage.ResumeLayout(false);
            this._deleteTabPage.ResumeLayout(false);
            this.ResumeLayout(false);
        } 
        #endregion
 
        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() {
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceCustomCommandPanel_HelpLabel);
            Caption = SR.GetString(SR.SqlDataSourceCustomCommandPanel_PanelCaption); 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public override bool OnNext() { 
            // Test for parameters in select query, choose between Parameters panel and Summary panel
            SqlDataSourceQuery selectQuery = _selectCommandEditor.GetQuery();
            SqlDataSourceQuery insertQuery = _insertCommandEditor.GetQuery();
            SqlDataSourceQuery updateQuery = _updateCommandEditor.GetQuery(); 
            SqlDataSourceQuery deleteQuery = _deleteCommandEditor.GetQuery();
            if ((selectQuery == null) || (insertQuery == null) || (updateQuery == null) || (deleteQuery == null)) { 
                // Problem getting command or parameter information, abort 
                // An error message would have been already displayed, so we just fail out
                return false; 
            }

            int inputParameterCount = 0;
            foreach (Parameter p in selectQuery.Parameters) { 
                if ((p.Direction == ParameterDirection.Input) ||
                    (p.Direction == ParameterDirection.InputOutput)) { 
                    inputParameterCount++; 
                }
            } 

            // Show parameters panel if there are parameters, otherwise go to the summary
            if (inputParameterCount == 0) {
                SqlDataSourceSummaryPanel nextPanel = NextPanel as SqlDataSourceSummaryPanel; 
                if (nextPanel == null) {
                    nextPanel = ((SqlDataSourceWizardForm)ParentWizard).GetSummaryPanel(); 
                    NextPanel = nextPanel; 
                }
                nextPanel.SetQueries( 
                    _dataConnection,
                    selectQuery,
                    insertQuery,
                    updateQuery, 
                    deleteQuery);
                return true; 
            } 
            else {
                SqlDataSourceConfigureParametersPanel nextPanel = NextPanel as SqlDataSourceConfigureParametersPanel; 
                if (nextPanel == null) {
                    nextPanel = ((SqlDataSourceWizardForm)ParentWizard).GetConfigureParametersPanel();
                    NextPanel = nextPanel;
                    SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component; 
                    Parameter[] selectParameters = new Parameter[sqlDataSource.SelectParameters.Count];
                    for (int i = 0; i < sqlDataSource.SelectParameters.Count; i++) { 
                        Parameter originalParameter = sqlDataSource.SelectParameters[i]; 
                        Parameter clonedParameter = (Parameter)(((ICloneable)originalParameter).Clone());
                        _sqlDataSourceDesigner.RegisterClone(originalParameter, clonedParameter); 
                        selectParameters[i] = clonedParameter;
                    }
                    nextPanel.InitializeParameters(selectParameters);
                } 

                nextPanel.SetQueries( 
                    _dataConnection, 
                    selectQuery,
                    insertQuery, 
                    updateQuery,
                    deleteQuery);
                return true;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public override void OnPrevious() { 
        }

        private void OnSelectCommandChanged(object sender, EventArgs e) {
            UpdateEnabledState(); 
        }
 
        /// <devdoc> 
        /// </devdoc>
        protected override void OnVisibleChanged(EventArgs e) { 
            base.OnVisibleChanged(e);

            if (Visible) {
                UpdateEnabledState(); 
            }
        } 
 
        public void ResetUI() {
        } 

        /// <devdoc>
        /// Sets all the queries for the data source.
        /// If this panel is used, there should always be at least one parameter. 
        /// </devdoc>
        public void SetQueries( 
            DesignerDataConnection dataConnection, 
            SqlDataSourceQuery selectQuery,
            SqlDataSourceQuery insertQuery, 
            SqlDataSourceQuery updateQuery,
            SqlDataSourceQuery deleteQuery) {

            // If the data connection has changed, force a refresh of the database metadata 
            DesignerDataConnection newConnection = dataConnection;
            if (!SqlDataSourceDesigner.ConnectionsEqual(_dataConnection, newConnection)) { 
                _dataConnection = newConnection; 

                // If the connection has changed, refresh the list of stored procedures 
                Cursor originalCursor = Cursor.Current;
                ArrayList filteredStoredProcedures = null;
                try {
                    Cursor.Current = Cursors.WaitCursor; 

                    IDataEnvironment dataEnvironment = (IDataEnvironment)_sqlDataSourceDesigner.Component.Site.GetService(typeof(IDataEnvironment)); 
                    Debug.Assert(dataEnvironment != null, "Could not find IDataEnvironment service"); 
                    if (dataEnvironment != null) {
                        IDesignerDataSchema designerDataSchema = dataEnvironment.GetConnectionSchema(_dataConnection); 
                        if (designerDataSchema != null) {
                            if (designerDataSchema.SupportsSchemaClass(DesignerDataSchemaClass.StoredProcedures)) {
                                ICollection databaseSprocs = designerDataSchema.GetSchemaItems(DesignerDataSchemaClass.StoredProcedures);
                                if (databaseSprocs != null && databaseSprocs.Count > 0) { 
                                    filteredStoredProcedures = new ArrayList();
                                    foreach (DesignerDataStoredProcedure sproc in databaseSprocs) { 
                                        // Hide special ASP.net stored procedures used for cache invalidation 
                                        if (!sproc.Name.ToLowerInvariant().StartsWith(SqlDataSourceDesigner.AspNetDatabaseObjectPrefix.ToLowerInvariant(), StringComparison.Ordinal)) {
                                            filteredStoredProcedures.Add(sproc); 
                                        }
                                    }
                                }
                            } 
                        }
                    } 
                } 
                catch (Exception ex) {
                    UIServiceHelper.ShowError( 
                        ServiceProvider,
                        ex,
                        SR.GetString(SR.SqlDataSourceConnectionPanel_CouldNotGetConnectionSchema));
                } 
                finally {
                    Cursor.Current = originalCursor; 
                } 

                // Initialize stored procedure list 
                _selectCommandEditor.SetConnection(_dataConnection);
                _selectCommandEditor.SetStoredProcedures(filteredStoredProcedures);
                _insertCommandEditor.SetConnection(_dataConnection);
                _insertCommandEditor.SetStoredProcedures(filteredStoredProcedures); 
                _updateCommandEditor.SetConnection(_dataConnection);
                _updateCommandEditor.SetStoredProcedures(filteredStoredProcedures); 
                _deleteCommandEditor.SetConnection(_dataConnection); 
                _deleteCommandEditor.SetStoredProcedures(filteredStoredProcedures);
 
                // Initialize query data
                _selectCommandEditor.SetQuery(selectQuery);
                _insertCommandEditor.SetQuery(insertQuery);
                _updateCommandEditor.SetQuery(updateQuery); 
                _deleteCommandEditor.SetQuery(deleteQuery);
            } 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void UpdateEnabledState() {
            Debug.Assert(ParentWizard != null, "Panel must be parented to update UI state");
            bool hasSelectQuery = _selectCommandEditor.HasQuery; 

            // Only enable Next button if a query has been entered 
            ParentWizard.NextButton.Enabled = hasSelectQuery; 
            ParentWizard.FinishButton.Enabled = false;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceCustomCommandPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
    using System.ComponentModel.Design.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
 
    /// <devdoc>
    /// Wizard panel for editing commands and parameters for a SqlDataSource.
    /// </devdoc>
    internal class SqlDataSourceCustomCommandPanel : WizardPanel { 
        private System.Windows.Forms.TabPage _selectTabPage;
        private System.Windows.Forms.TabPage _updateTabPage; 
        private System.Windows.Forms.TabPage _insertTabPage; 
        private System.Windows.Forms.TabPage _deleteTabPage;
        private SqlDataSourceCustomCommandEditor _selectCommandEditor; 
        private SqlDataSourceCustomCommandEditor _insertCommandEditor;
        private SqlDataSourceCustomCommandEditor _updateCommandEditor;
        private SqlDataSourceCustomCommandEditor _deleteCommandEditor;
        private System.Windows.Forms.TabControl _commandsTabControl; 
        private System.Windows.Forms.Label _helpLabel;
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner; 
        private DesignerDataConnection _dataConnection;
 

        /// <devdoc>
        /// Creates a new SqlDataSourceCustomCommandPanel.
        /// </devdoc> 
        public SqlDataSourceCustomCommandPanel(SqlDataSourceDesigner sqlDataSourceDesigner) {
            Debug.Assert(sqlDataSourceDesigner != null); 
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
            InitializeComponent();
            InitializeUI(); 

            _selectCommandEditor.SetCommandData(_sqlDataSourceDesigner, QueryBuilderMode.Select);
            _insertCommandEditor.SetCommandData(_sqlDataSourceDesigner, QueryBuilderMode.Insert);
            _updateCommandEditor.SetCommandData(_sqlDataSourceDesigner, QueryBuilderMode.Update); 
            _deleteCommandEditor.SetCommandData(_sqlDataSourceDesigner, QueryBuilderMode.Delete);
        } 
 

        #region Designer generated code 
        private void InitializeComponent() {
            this._commandsTabControl = new System.Windows.Forms.TabControl();
            this._selectTabPage = new System.Windows.Forms.TabPage();
            this._selectCommandEditor = new SqlDataSourceCustomCommandEditor(); 
            this._updateTabPage = new System.Windows.Forms.TabPage();
            this._updateCommandEditor = new SqlDataSourceCustomCommandEditor(); 
            this._insertTabPage = new System.Windows.Forms.TabPage(); 
            this._insertCommandEditor = new SqlDataSourceCustomCommandEditor();
            this._deleteTabPage = new System.Windows.Forms.TabPage(); 
            this._deleteCommandEditor = new SqlDataSourceCustomCommandEditor();
            this._helpLabel = new System.Windows.Forms.Label();
            this._commandsTabControl.SuspendLayout();
            this._selectTabPage.SuspendLayout(); 
            this._updateTabPage.SuspendLayout();
            this._insertTabPage.SuspendLayout(); 
            this._deleteTabPage.SuspendLayout(); 
            this.SuspendLayout();
            // 
            // _helpLabel
            //
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._helpLabel.Location = new System.Drawing.Point(0, 0);
            this._helpLabel.Name = "_helpLabel"; 
            this._helpLabel.Size = new System.Drawing.Size(544, 16); 
            this._helpLabel.TabIndex = 10;
            // 
            // _commandsTabControl
            //
            this._commandsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._commandsTabControl.Controls.Add(this._selectTabPage); 
            this._commandsTabControl.Controls.Add(this._updateTabPage); 
            this._commandsTabControl.Controls.Add(this._insertTabPage);
            this._commandsTabControl.Controls.Add(this._deleteTabPage); 
            this._commandsTabControl.Location = new System.Drawing.Point(0, 22);
            this._commandsTabControl.Name = "_commandsTabControl";
            this._commandsTabControl.SelectedIndex = 0;
            this._commandsTabControl.ShowToolTips = true; 
            this._commandsTabControl.Size = new System.Drawing.Size(544, 252);
            this._commandsTabControl.TabIndex = 20; 
            // 
            // _selectTabPage
            // 
            this._selectTabPage.Controls.Add(this._selectCommandEditor);
            this._selectTabPage.Location = new System.Drawing.Point(4, 22);
            this._selectTabPage.Name = "_selectTabPage";
            this._selectTabPage.Size = new System.Drawing.Size(536, 226); 
            this._selectTabPage.TabIndex = 10;
            this._selectTabPage.Text = "SELECT"; 
            // 
            // _selectCommandEditor
            // 
            this._selectCommandEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._selectCommandEditor.Location = new System.Drawing.Point(0, 0);
            this._selectCommandEditor.Name = "_selectCommandEditor";
            this._selectCommandEditor.TabIndex = 10; 
            this._selectCommandEditor.CommandChanged += new System.EventHandler(this.OnSelectCommandChanged);
            // 
            // _updateTabPage 
            //
            this._updateTabPage.Controls.Add(this._updateCommandEditor); 
            this._updateTabPage.Location = new System.Drawing.Point(4, 22);
            this._updateTabPage.Name = "_updateTabPage";
            this._updateTabPage.Size = new System.Drawing.Size(536, 226);
            this._updateTabPage.TabIndex = 20; 
            this._updateTabPage.Text = "UPDATE";
            // 
            // _updateCommandEditor 
            //
            this._updateCommandEditor.Dock = System.Windows.Forms.DockStyle.Fill; 
            this._updateCommandEditor.Location = new System.Drawing.Point(0, 0);
            this._updateCommandEditor.Name = "_updateCommandEditor";
            this._updateCommandEditor.TabIndex = 10;
            // 
            // _insertTabPage
            // 
            this._insertTabPage.Controls.Add(this._insertCommandEditor); 
            this._insertTabPage.Location = new System.Drawing.Point(4, 22);
            this._insertTabPage.Name = "_insertTabPage"; 
            this._insertTabPage.Size = new System.Drawing.Size(536, 226);
            this._insertTabPage.TabIndex = 30;
            this._insertTabPage.Text = "INSERT";
            // 
            // _insertCommandEditor
            // 
            this._insertCommandEditor.Dock = System.Windows.Forms.DockStyle.Fill; 
            this._insertCommandEditor.Location = new System.Drawing.Point(0, 0);
            this._insertCommandEditor.Name = "_insertCommandEditor"; 
            this._insertCommandEditor.TabIndex = 10;
            //
            // _deleteTabPage
            // 
            this._deleteTabPage.Controls.Add(this._deleteCommandEditor);
            this._deleteTabPage.Location = new System.Drawing.Point(4, 22); 
            this._deleteTabPage.Name = "_deleteTabPage"; 
            this._deleteTabPage.Size = new System.Drawing.Size(522, 226);
            this._deleteTabPage.TabIndex = 40; 
            this._deleteTabPage.Text = "DELETE";
            //
            // _deleteCommandEditor
            // 
            this._deleteCommandEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._deleteCommandEditor.Location = new System.Drawing.Point(0, 0); 
            this._deleteCommandEditor.Name = "_deleteCommandEditor"; 
            this._deleteCommandEditor.TabIndex = 10;
            // 
            // SqlDataSourceCustomCommandPanel
            //
            this.Controls.Add(this._helpLabel);
            this.Controls.Add(this._commandsTabControl); 
            this.Name = "SqlDataSourceCustomCommandPanel";
            this.Size = new System.Drawing.Size(544, 274); 
            this._commandsTabControl.ResumeLayout(false); 
            this._selectTabPage.ResumeLayout(false);
            this._updateTabPage.ResumeLayout(false); 
            this._insertTabPage.ResumeLayout(false);
            this._deleteTabPage.ResumeLayout(false);
            this.ResumeLayout(false);
        } 
        #endregion
 
        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() {
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceCustomCommandPanel_HelpLabel);
            Caption = SR.GetString(SR.SqlDataSourceCustomCommandPanel_PanelCaption); 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public override bool OnNext() { 
            // Test for parameters in select query, choose between Parameters panel and Summary panel
            SqlDataSourceQuery selectQuery = _selectCommandEditor.GetQuery();
            SqlDataSourceQuery insertQuery = _insertCommandEditor.GetQuery();
            SqlDataSourceQuery updateQuery = _updateCommandEditor.GetQuery(); 
            SqlDataSourceQuery deleteQuery = _deleteCommandEditor.GetQuery();
            if ((selectQuery == null) || (insertQuery == null) || (updateQuery == null) || (deleteQuery == null)) { 
                // Problem getting command or parameter information, abort 
                // An error message would have been already displayed, so we just fail out
                return false; 
            }

            int inputParameterCount = 0;
            foreach (Parameter p in selectQuery.Parameters) { 
                if ((p.Direction == ParameterDirection.Input) ||
                    (p.Direction == ParameterDirection.InputOutput)) { 
                    inputParameterCount++; 
                }
            } 

            // Show parameters panel if there are parameters, otherwise go to the summary
            if (inputParameterCount == 0) {
                SqlDataSourceSummaryPanel nextPanel = NextPanel as SqlDataSourceSummaryPanel; 
                if (nextPanel == null) {
                    nextPanel = ((SqlDataSourceWizardForm)ParentWizard).GetSummaryPanel(); 
                    NextPanel = nextPanel; 
                }
                nextPanel.SetQueries( 
                    _dataConnection,
                    selectQuery,
                    insertQuery,
                    updateQuery, 
                    deleteQuery);
                return true; 
            } 
            else {
                SqlDataSourceConfigureParametersPanel nextPanel = NextPanel as SqlDataSourceConfigureParametersPanel; 
                if (nextPanel == null) {
                    nextPanel = ((SqlDataSourceWizardForm)ParentWizard).GetConfigureParametersPanel();
                    NextPanel = nextPanel;
                    SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component; 
                    Parameter[] selectParameters = new Parameter[sqlDataSource.SelectParameters.Count];
                    for (int i = 0; i < sqlDataSource.SelectParameters.Count; i++) { 
                        Parameter originalParameter = sqlDataSource.SelectParameters[i]; 
                        Parameter clonedParameter = (Parameter)(((ICloneable)originalParameter).Clone());
                        _sqlDataSourceDesigner.RegisterClone(originalParameter, clonedParameter); 
                        selectParameters[i] = clonedParameter;
                    }
                    nextPanel.InitializeParameters(selectParameters);
                } 

                nextPanel.SetQueries( 
                    _dataConnection, 
                    selectQuery,
                    insertQuery, 
                    updateQuery,
                    deleteQuery);
                return true;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public override void OnPrevious() { 
        }

        private void OnSelectCommandChanged(object sender, EventArgs e) {
            UpdateEnabledState(); 
        }
 
        /// <devdoc> 
        /// </devdoc>
        protected override void OnVisibleChanged(EventArgs e) { 
            base.OnVisibleChanged(e);

            if (Visible) {
                UpdateEnabledState(); 
            }
        } 
 
        public void ResetUI() {
        } 

        /// <devdoc>
        /// Sets all the queries for the data source.
        /// If this panel is used, there should always be at least one parameter. 
        /// </devdoc>
        public void SetQueries( 
            DesignerDataConnection dataConnection, 
            SqlDataSourceQuery selectQuery,
            SqlDataSourceQuery insertQuery, 
            SqlDataSourceQuery updateQuery,
            SqlDataSourceQuery deleteQuery) {

            // If the data connection has changed, force a refresh of the database metadata 
            DesignerDataConnection newConnection = dataConnection;
            if (!SqlDataSourceDesigner.ConnectionsEqual(_dataConnection, newConnection)) { 
                _dataConnection = newConnection; 

                // If the connection has changed, refresh the list of stored procedures 
                Cursor originalCursor = Cursor.Current;
                ArrayList filteredStoredProcedures = null;
                try {
                    Cursor.Current = Cursors.WaitCursor; 

                    IDataEnvironment dataEnvironment = (IDataEnvironment)_sqlDataSourceDesigner.Component.Site.GetService(typeof(IDataEnvironment)); 
                    Debug.Assert(dataEnvironment != null, "Could not find IDataEnvironment service"); 
                    if (dataEnvironment != null) {
                        IDesignerDataSchema designerDataSchema = dataEnvironment.GetConnectionSchema(_dataConnection); 
                        if (designerDataSchema != null) {
                            if (designerDataSchema.SupportsSchemaClass(DesignerDataSchemaClass.StoredProcedures)) {
                                ICollection databaseSprocs = designerDataSchema.GetSchemaItems(DesignerDataSchemaClass.StoredProcedures);
                                if (databaseSprocs != null && databaseSprocs.Count > 0) { 
                                    filteredStoredProcedures = new ArrayList();
                                    foreach (DesignerDataStoredProcedure sproc in databaseSprocs) { 
                                        // Hide special ASP.net stored procedures used for cache invalidation 
                                        if (!sproc.Name.ToLowerInvariant().StartsWith(SqlDataSourceDesigner.AspNetDatabaseObjectPrefix.ToLowerInvariant(), StringComparison.Ordinal)) {
                                            filteredStoredProcedures.Add(sproc); 
                                        }
                                    }
                                }
                            } 
                        }
                    } 
                } 
                catch (Exception ex) {
                    UIServiceHelper.ShowError( 
                        ServiceProvider,
                        ex,
                        SR.GetString(SR.SqlDataSourceConnectionPanel_CouldNotGetConnectionSchema));
                } 
                finally {
                    Cursor.Current = originalCursor; 
                } 

                // Initialize stored procedure list 
                _selectCommandEditor.SetConnection(_dataConnection);
                _selectCommandEditor.SetStoredProcedures(filteredStoredProcedures);
                _insertCommandEditor.SetConnection(_dataConnection);
                _insertCommandEditor.SetStoredProcedures(filteredStoredProcedures); 
                _updateCommandEditor.SetConnection(_dataConnection);
                _updateCommandEditor.SetStoredProcedures(filteredStoredProcedures); 
                _deleteCommandEditor.SetConnection(_dataConnection); 
                _deleteCommandEditor.SetStoredProcedures(filteredStoredProcedures);
 
                // Initialize query data
                _selectCommandEditor.SetQuery(selectQuery);
                _insertCommandEditor.SetQuery(insertQuery);
                _updateCommandEditor.SetQuery(updateQuery); 
                _deleteCommandEditor.SetQuery(deleteQuery);
            } 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void UpdateEnabledState() {
            Debug.Assert(ParentWizard != null, "Panel must be parented to update UI state");
            bool hasSelectQuery = _selectCommandEditor.HasQuery; 

            // Only enable Next button if a query has been entered 
            ParentWizard.NextButton.Enabled = hasSelectQuery; 
            ParentWizard.FinishButton.Enabled = false;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
