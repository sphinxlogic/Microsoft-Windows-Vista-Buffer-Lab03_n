//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceCustomCommandEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.Collections.Generic; 
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.ComponentModel.Design.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms;

    /// <devdoc> 
    /// UserControl for editing a custom SQL command or choosing a
    /// stored procedure for a SqlDataSource. 
    /// </devdoc> 
    internal class SqlDataSourceCustomCommandEditor : UserControl {
 
        private static readonly object EventCommandChanged = new object();

        private System.Windows.Forms.TextBox _commandTextBox;
        private System.Windows.Forms.Button _queryBuilderButton; 
        private System.Windows.Forms.RadioButton _sqlRadioButton;
        private System.Windows.Forms.RadioButton _storedProcedureRadioButton; 
        private AutoSizeComboBox _storedProcedureComboBox; 
        private System.Windows.Forms.Panel _sqlPanel;
        private System.Windows.Forms.Panel _storedProcedurePanel; 

        private QueryBuilderMode _editorMode;
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
        private DesignerDataConnection _dataConnection; 
        private ICollection _storedProcedures;
        private IDataEnvironment _dataEnvironment; 
 
        private ICollection _parameters;
        private string _originalCommand; 
        private SqlDataSourceCommandType _commandType;
        private bool _queryInitialized;

 
        /// <devdoc>
        /// Creates a new SqlDataSourceCustomCommandEditor. 
        /// </devdoc> 
        public SqlDataSourceCustomCommandEditor() {
            InitializeComponent(); 
            InitializeUI();
        }

        /// <devdoc> 
        /// Indicates that a query has been entered (though it may be invalid).
        /// </devdoc> 
        public bool HasQuery { 
            get {
                Debug.Assert(_dataConnection != null); 

                if (_sqlRadioButton.Checked) {
                    // SQL command text in textbox
                    return (_commandTextBox.Text.Trim().Length > 0); 
                }
                else { 
                    // Stored procedure from list 
                    StoredProcedureItem sprocItem = _storedProcedureComboBox.SelectedItem as StoredProcedureItem;
                    return (sprocItem != null); 
                }
            }
        }
 
        /// <devdoc>
        /// Notifies listeners that the command has changed. 
        /// This is used by the custom command panel to update the UI to 
        /// reflect a newly chosen Select command.
        /// </devdoc> 
        public event EventHandler CommandChanged {
            add {
                Events.AddHandler(EventCommandChanged, value);
            } 
            remove {
                Events.RemoveHandler(EventCommandChanged, value); 
            } 
        }
 
        #region Designer generated code
        private void InitializeComponent() {
            this._commandTextBox = new System.Windows.Forms.TextBox();
            this._queryBuilderButton = new System.Windows.Forms.Button(); 
            this._sqlRadioButton = new System.Windows.Forms.RadioButton();
            this._storedProcedureRadioButton = new System.Windows.Forms.RadioButton(); 
            this._storedProcedureComboBox = new AutoSizeComboBox(); 
            this._storedProcedurePanel = new System.Windows.Forms.Panel();
            this._sqlPanel = new System.Windows.Forms.Panel(); 
            this._storedProcedurePanel.SuspendLayout();
            this._sqlPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _sqlRadioButton
            // 
            this._sqlRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sqlRadioButton.Location = new System.Drawing.Point(12, 12); 
            this._sqlRadioButton.Name = "_sqlRadioButton";
            this._sqlRadioButton.Size = new System.Drawing.Size(489, 20);
            this._sqlRadioButton.TabIndex = 10;
            this._sqlRadioButton.CheckedChanged += new System.EventHandler(this.OnSqlRadioButtonCheckedChanged); 
            //
            // _sqlPanel 
            // 
            this._sqlPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sqlPanel.Controls.Add(this._queryBuilderButton);
            this._sqlPanel.Controls.Add(this._commandTextBox);
            this._sqlPanel.Location = new System.Drawing.Point(28, 32); 
            this._sqlPanel.Name = "_sqlPanel";
            this._sqlPanel.Size = new System.Drawing.Size(480, 121); 
            this._sqlPanel.TabIndex = 20; 
            //
            // _storedProcedureRadioButton 
            //
            this._storedProcedureRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._storedProcedureRadioButton.Location = new System.Drawing.Point(12, 160); 
            this._storedProcedureRadioButton.Name = "_storedProcedureRadioButton";
            this._storedProcedureRadioButton.Size = new System.Drawing.Size(489, 20); 
            this._storedProcedureRadioButton.TabIndex = 30; 
            //
            // _storedProcedurePanel 
            //
            this._storedProcedurePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._storedProcedurePanel.Controls.Add(this._storedProcedureComboBox); 
            this._storedProcedurePanel.Location = new System.Drawing.Point(28, 180);
            this._storedProcedurePanel.Name = "_storedProcedurePanel"; 
            this._storedProcedurePanel.Size = new System.Drawing.Size(265, 21); 
            this._storedProcedurePanel.TabIndex = 40;
 
            //
            // _commandTextBox
            //
            this._commandTextBox.AcceptsReturn = true; 
            this._commandTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._commandTextBox.Location = new System.Drawing.Point(0, 0);
            this._commandTextBox.Multiline = true; 
            this._commandTextBox.Name = "_commandTextBox";
            this._commandTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._commandTextBox.Size = new System.Drawing.Size(480, 93);
            this._commandTextBox.TabIndex = 20; 
            this._commandTextBox.TextChanged += new System.EventHandler(this.OnCommandTextBoxTextChanged);
            // 
            // _queryBuilderButton 
            //
            this._queryBuilderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._queryBuilderButton.Location = new System.Drawing.Point(330, 98);
            this._queryBuilderButton.Name = "_queryBuilderButton";
            this._queryBuilderButton.Size = new System.Drawing.Size(150, 23);
            this._queryBuilderButton.TabIndex = 30; 
            this._queryBuilderButton.Click += new System.EventHandler(this.OnQueryBuilderButtonClick);
 
            // 
            // _storedProcedureComboBox
            // 
            this._storedProcedureComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._storedProcedureComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._storedProcedureComboBox.Location = new System.Drawing.Point(0, 0);
            this._storedProcedureComboBox.Name = "_storedProcedureComboBox"; 
            this._storedProcedureComboBox.Size = new System.Drawing.Size(265, 21); 
            this._storedProcedureComboBox.TabIndex = 10;
            this._storedProcedureComboBox.SelectedIndexChanged += new System.EventHandler(this.OnStoredProcedureComboBoxSelectedIndexChanged); 
            //
            // SqlDataSourceCustomCommandEditor
            //
            this.Controls.Add(this._sqlRadioButton); 
            this.Controls.Add(this._sqlPanel);
            this.Controls.Add(this._storedProcedureRadioButton); 
            this.Controls.Add(this._storedProcedurePanel); 
            this.Name = "SqlDataSourceCustomCommandEditor";
            this.Size = new System.Drawing.Size(522, 230); 
            this._storedProcedurePanel.ResumeLayout(false);
            this._sqlPanel.ResumeLayout(false);
            this._sqlPanel.PerformLayout();
            this.ResumeLayout(false); 
        }
        #endregion 
 
        /// <devdoc>
        /// Gets the current query (command + parameters) that the user has entered. 
        /// </devdoc>
        public SqlDataSourceQuery GetQuery() {
            Debug.Assert(_dataConnection != null);
 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 

                DbProviderFactory factory; 

                // Get parameters from the query
                if (_sqlRadioButton.Checked) {
                    if (_commandTextBox.Text.Trim().Length > 0) { 
                        // We assume that new text typed into the textbox is command text
                        // and not a stored procedure. However if the user didn't change the 
                        // text, we preserve their old setting. 
                        SqlDataSourceCommandType commandType;
                        if (String.Equals(_commandTextBox.Text, _originalCommand, StringComparison.OrdinalIgnoreCase)) { 
                            commandType = _commandType;
                        }
                        else {
                            commandType = SqlDataSourceCommandType.Text; 
                        }
                        // If the provider supports named parameters (SqlClient, OracleClient), then 
                        // we parse the command to detect the parameters. If named parameters are not 
                        // supported, then we just use the old parameters that the user already had set.
                        // For the Select command we always parse regardless of the provider since we 
                        // need to detect new parameters for the Configure Parameters panel.
                        ICollection mergedParameters;
                        factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
                        if ((_editorMode == QueryBuilderMode.Select) || 
                            SqlDataSourceDesigner.SupportsNamedParameters(factory)) {
                            Parameter[] derivedParameters = _sqlDataSourceDesigner.InferParameterNames(_dataConnection, _commandTextBox.Text, commandType); 
                            if (derivedParameters == null) { 
                                // Parameters could not be derived
                                return null; 
                            }
                            ArrayList newParameters = new ArrayList(derivedParameters);
                            mergedParameters = MergeParameters(_parameters, newParameters, SqlDataSourceDesigner.SupportsNamedParameters(factory));
                        } 
                        else {
                            mergedParameters = _parameters; 
                        } 
                        return new SqlDataSourceQuery(_commandTextBox.Text, commandType, mergedParameters);
                    } 
                    else {
                        // No text specified, return empty query
                        return new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    } 
                }
                else { 
                    // Stored procedure from list - get from DesignerDataStoredProcedure.Parameters 
                    StoredProcedureItem sprocItem = _storedProcedureComboBox.SelectedItem as StoredProcedureItem;
                    if (sprocItem == null) { 
                        // No stored procedure selected, return empty query
                        return new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    }
 
                    // Get parameters from stored procedure object
                    ArrayList newParameters = new ArrayList(); 
                    ICollection sprocParameters = null; 
                    try {
                        sprocParameters = sprocItem.DesignerDataStoredProcedure.Parameters; 
                    }
                    catch (Exception ex) {
                        UIServiceHelper.ShowError(
                            _sqlDataSourceDesigner.Component.Site, 
                            ex,
                            SR.GetString(SR.SqlDataSourceCustomCommandEditor_CouldNotGetStoredProcedureSchema)); 
                        return null; 
                    }
 
                    factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
                    if (sprocParameters != null && sprocParameters.Count > 0) {
                        foreach (DesignerDataParameter designerDataParameter in sprocParameters) {
                            // Trim off the parameter prefix if it is present 
                            string parameterName = SqlDataSourceDesigner.StripParameterPrefix(designerDataParameter.Name);
                            Parameter parameter = new Parameter(parameterName, SqlDataSourceDesigner.ConvertDbTypeToTypeCode(designerDataParameter.DataType)); 
                            parameter.Direction = designerDataParameter.Direction; 
                            newParameters.Add(parameter);
                        } 
                    }
                    ICollection mergedParameters = MergeParameters(_parameters, newParameters, SqlDataSourceDesigner.SupportsNamedParameters(factory));
                    //
                    return new SqlDataSourceQuery(sprocItem.DesignerDataStoredProcedure.Name, SqlDataSourceCommandType.StoredProcedure, mergedParameters); 
                }
            } 
            finally { 
                Cursor.Current = originalCursor;
            } 
        }

        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that 
        /// are not supported by the designer.
        /// </devdoc> 
        private void InitializeUI() { 
            _queryBuilderButton.Text = SR.GetString(SR.SqlDataSourceCustomCommandEditor_QueryBuilderButton);
            _sqlRadioButton.Text = SR.GetString(SR.SqlDataSourceCustomCommandEditor_SqlLabel); 
            _storedProcedureRadioButton.Text = SR.GetString(SR.SqlDataSourceCustomCommandEditor_StoredProcedureLabel);
        }

        /// <devdoc> 
        /// Goes through all the new parameters and merges in original
        /// parameters that are matches. 
        /// </devdoc> 
        private ICollection MergeParameters(ICollection originalParameters, ArrayList newParameters, bool useNamedParameters) {
            System.Collections.Generic.List<Parameter> unusedOriginalParameters = new System.Collections.Generic.List<Parameter>(); 
            foreach (Parameter p in originalParameters) {
                unusedOriginalParameters.Add(p);
            }
 
            System.Collections.Generic.List<Parameter> mergedParameters = new System.Collections.Generic.List<Parameter>();
 
            for (int i = 0; i < newParameters.Count; i++) { 
                Parameter newParameter = (Parameter)newParameters[i];
                Parameter foundParameter = null; 
                foreach (Parameter originalParameter in unusedOriginalParameters) {
                    // Parameters are matched if either their name and direction match,
                    // or if they are both ReturnValue parameters (since in that case the
                    // names don't matter). 
                    bool namesAndDirectionsMatch =
                        useNamedParameters ? 
                        (String.Equals(originalParameter.Name, newParameter.Name, StringComparison.OrdinalIgnoreCase) && originalParameter.Direction == newParameter.Direction) : 
                        (originalParameter.Direction == newParameter.Direction);
                    bool bothAreReturnValue = (originalParameter.Direction == ParameterDirection.ReturnValue) && 
                                              (newParameter.Direction == ParameterDirection.ReturnValue);
                    if (namesAndDirectionsMatch || bothAreReturnValue) {
                        // A matching original parameter was found
                        foundParameter = originalParameter; 
                        break;
                    } 
                } 
                if (foundParameter != null) {
                    // Replace the new parameter with the original and remove it from the unused list 
                    mergedParameters.Add(foundParameter);
                    unusedOriginalParameters.Remove(foundParameter);
                }
                else { 
                    // Add the new parameter only if it is an input or input/output parameter
                    if (newParameter.Direction == ParameterDirection.Input || 
                        newParameter.Direction == ParameterDirection.InputOutput) { 
                        mergedParameters.Add(newParameter);
                    } 
                }
            }

            return mergedParameters; 
        }
 
        private void OnCommandChanged(EventArgs e) { 
            EventHandler handler = Events[EventCommandChanged] as EventHandler;
            if (handler != null) { 
                handler(this, e);
            }
        }
 
        private void OnCommandTextBoxTextChanged(object sender, System.EventArgs e) {
            OnCommandChanged(EventArgs.Empty); 
        } 

        private void OnQueryBuilderButtonClick(object sender, System.EventArgs e) { 
            Debug.Assert(_dataEnvironment != null);
            Debug.Assert(_dataConnection != null);

            IServiceProvider serviceProvider = _sqlDataSourceDesigner.Component.Site; 

            if ((_dataConnection.ConnectionString != null) && (_dataConnection.ConnectionString.Trim().Length == 0)) { 
                UIServiceHelper.ShowError(serviceProvider, SR.GetString(SR.SqlDataSourceCustomCommandEditor_NoConnectionString)); 
                return;
            } 

            // Launch query builder

            DesignerDataConnection newConnection = _dataConnection; 
            if (String.IsNullOrEmpty(_dataConnection.ProviderName)) {
                newConnection = new DesignerDataConnection(_dataConnection.Name, SqlDataSourceDesigner.DefaultProviderName, _dataConnection.ConnectionString, _dataConnection.IsConfigured); 
            } 

            string newQuery = _dataEnvironment.BuildQuery(this, newConnection, _editorMode, _commandTextBox.Text); 
            if ((newQuery != null) && (newQuery.Length > 0)) {
                _commandTextBox.Text = newQuery;
                _commandTextBox.Focus();
                _commandTextBox.Select(0, 0); 
            }
        } 
 
        private void OnSqlRadioButtonCheckedChanged(object sender, System.EventArgs e) {
            UpdateEnabledState(); 
        }

        private void OnStoredProcedureComboBoxSelectedIndexChanged(object sender, EventArgs e) {
            OnCommandChanged(EventArgs.Empty); 
        }
 
        public void SetCommandData(SqlDataSourceDesigner sqlDataSourceDesigner, QueryBuilderMode editorMode) { 
            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
            _editorMode = editorMode;

            _queryBuilderButton.Enabled = false;
            IServiceProvider serviceProvider = _sqlDataSourceDesigner.Component.Site; 
            Debug.Assert(serviceProvider != null);
            if (serviceProvider != null) { 
                // Get data environment 
                _dataEnvironment = (IDataEnvironment)serviceProvider.GetService(typeof(IDataEnvironment));
            } 
        }

        public void SetConnection(DesignerDataConnection dataConnection) {
            Debug.Assert(dataConnection != null); 
            _dataConnection = dataConnection;
        } 
 
        /// <devdoc>
        /// Sets the current query (command + parameters) to prepopulate the UI. 
        /// </devdoc>
        public void SetQuery(SqlDataSourceQuery query) {
            _storedProcedureComboBox.SelectedIndex = -1;
 
            if (_storedProcedures != null) {
                // Try to find stored procedure in list, assuming the command is a stored procedure 
                foreach (StoredProcedureItem sprocItem in _storedProcedureComboBox.Items) { 
                    if (sprocItem.DesignerDataStoredProcedure.Name == query.Command) {
                        _storedProcedureComboBox.SelectedItem = sprocItem; 
                        break;
                    }
                }
            } 

            if (_storedProcedureComboBox.SelectedIndex != -1) { 
                // Matching stored procedure was found, so enable the sproc picker 
                _sqlRadioButton.Checked = false;
                _storedProcedureRadioButton.Checked = true; 
            }
            else {
                // No matching stored procedure was found, the command must be a
                // regular SQL command. By default we also select the first sproc 
                // in the dropdown as well.
                _sqlRadioButton.Checked = true; 
                _storedProcedureRadioButton.Checked = false; 
                if (_storedProcedureComboBox.Items.Count > 0) {
                    _storedProcedureComboBox.SelectedIndex = 0; 
                }
            }

            if (!_queryInitialized) { 
                // If the query has never been initialized, initialize it
                _commandTextBox.Text = query.Command; 
                _originalCommand = query.Command; 
                _commandType = query.CommandType;
                _parameters = query.Parameters; 

                _queryInitialized = true;
            }
 
            UpdateEnabledState();
        } 
 

        public void SetStoredProcedures(ICollection storedProcedures) { 
            _storedProcedures = storedProcedures;

            // Disable stored procedure UI if there are none
            bool hasStoredProcedures = ((_storedProcedures != null) && (_storedProcedures.Count > 0)); 
            _storedProcedureRadioButton.Enabled = hasStoredProcedures;
 
            // Populate stored procedure combobox 
            _storedProcedureComboBox.Items.Clear();
            if (hasStoredProcedures) { 
                System.Collections.Generic.List<StoredProcedureItem> sprocItems = new System.Collections.Generic.List<StoredProcedureItem>();
                foreach (DesignerDataStoredProcedure storedProcedure in _storedProcedures) {
                    sprocItems.Add(new StoredProcedureItem(storedProcedure));
                } 
                sprocItems.Sort(new System.Comparison<StoredProcedureItem>(
                    delegate(StoredProcedureItem a, StoredProcedureItem b) { 
                        return String.Compare(a.DesignerDataStoredProcedure.Name, b.DesignerDataStoredProcedure.Name, StringComparison.InvariantCultureIgnoreCase); 
                    }));
                _storedProcedureComboBox.Items.AddRange(sprocItems.ToArray()); 
                _storedProcedureComboBox.InvalidateDropDownWidth();
            }
        }
 
        private void UpdateEnabledState() {
            bool inCommandTextMode = _sqlRadioButton.Checked; 
            _commandTextBox.Enabled = inCommandTextMode; 
            _queryBuilderButton.Enabled = inCommandTextMode;
            _storedProcedureComboBox.Enabled = !inCommandTextMode; 

            OnCommandChanged(EventArgs.Empty);
        }
 

        /// <devdoc> 
        /// Represents a stored procedure a user can select. 
        /// </devdoc>
        private sealed class StoredProcedureItem { 
            private DesignerDataStoredProcedure _designerDataStoredProcedure;

            public StoredProcedureItem(DesignerDataStoredProcedure designerDataStoredProcedure) {
                Debug.Assert(designerDataStoredProcedure != null); 
                _designerDataStoredProcedure = designerDataStoredProcedure;
            } 
 
            public DesignerDataStoredProcedure DesignerDataStoredProcedure {
                get { 
                    return _designerDataStoredProcedure;
                }
            }
 
            public override string ToString() {
                return _designerDataStoredProcedure.Name; 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceCustomCommandEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.Collections.Generic; 
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.ComponentModel.Design.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms;

    /// <devdoc> 
    /// UserControl for editing a custom SQL command or choosing a
    /// stored procedure for a SqlDataSource. 
    /// </devdoc> 
    internal class SqlDataSourceCustomCommandEditor : UserControl {
 
        private static readonly object EventCommandChanged = new object();

        private System.Windows.Forms.TextBox _commandTextBox;
        private System.Windows.Forms.Button _queryBuilderButton; 
        private System.Windows.Forms.RadioButton _sqlRadioButton;
        private System.Windows.Forms.RadioButton _storedProcedureRadioButton; 
        private AutoSizeComboBox _storedProcedureComboBox; 
        private System.Windows.Forms.Panel _sqlPanel;
        private System.Windows.Forms.Panel _storedProcedurePanel; 

        private QueryBuilderMode _editorMode;
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
        private DesignerDataConnection _dataConnection; 
        private ICollection _storedProcedures;
        private IDataEnvironment _dataEnvironment; 
 
        private ICollection _parameters;
        private string _originalCommand; 
        private SqlDataSourceCommandType _commandType;
        private bool _queryInitialized;

 
        /// <devdoc>
        /// Creates a new SqlDataSourceCustomCommandEditor. 
        /// </devdoc> 
        public SqlDataSourceCustomCommandEditor() {
            InitializeComponent(); 
            InitializeUI();
        }

        /// <devdoc> 
        /// Indicates that a query has been entered (though it may be invalid).
        /// </devdoc> 
        public bool HasQuery { 
            get {
                Debug.Assert(_dataConnection != null); 

                if (_sqlRadioButton.Checked) {
                    // SQL command text in textbox
                    return (_commandTextBox.Text.Trim().Length > 0); 
                }
                else { 
                    // Stored procedure from list 
                    StoredProcedureItem sprocItem = _storedProcedureComboBox.SelectedItem as StoredProcedureItem;
                    return (sprocItem != null); 
                }
            }
        }
 
        /// <devdoc>
        /// Notifies listeners that the command has changed. 
        /// This is used by the custom command panel to update the UI to 
        /// reflect a newly chosen Select command.
        /// </devdoc> 
        public event EventHandler CommandChanged {
            add {
                Events.AddHandler(EventCommandChanged, value);
            } 
            remove {
                Events.RemoveHandler(EventCommandChanged, value); 
            } 
        }
 
        #region Designer generated code
        private void InitializeComponent() {
            this._commandTextBox = new System.Windows.Forms.TextBox();
            this._queryBuilderButton = new System.Windows.Forms.Button(); 
            this._sqlRadioButton = new System.Windows.Forms.RadioButton();
            this._storedProcedureRadioButton = new System.Windows.Forms.RadioButton(); 
            this._storedProcedureComboBox = new AutoSizeComboBox(); 
            this._storedProcedurePanel = new System.Windows.Forms.Panel();
            this._sqlPanel = new System.Windows.Forms.Panel(); 
            this._storedProcedurePanel.SuspendLayout();
            this._sqlPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _sqlRadioButton
            // 
            this._sqlRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sqlRadioButton.Location = new System.Drawing.Point(12, 12); 
            this._sqlRadioButton.Name = "_sqlRadioButton";
            this._sqlRadioButton.Size = new System.Drawing.Size(489, 20);
            this._sqlRadioButton.TabIndex = 10;
            this._sqlRadioButton.CheckedChanged += new System.EventHandler(this.OnSqlRadioButtonCheckedChanged); 
            //
            // _sqlPanel 
            // 
            this._sqlPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sqlPanel.Controls.Add(this._queryBuilderButton);
            this._sqlPanel.Controls.Add(this._commandTextBox);
            this._sqlPanel.Location = new System.Drawing.Point(28, 32); 
            this._sqlPanel.Name = "_sqlPanel";
            this._sqlPanel.Size = new System.Drawing.Size(480, 121); 
            this._sqlPanel.TabIndex = 20; 
            //
            // _storedProcedureRadioButton 
            //
            this._storedProcedureRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._storedProcedureRadioButton.Location = new System.Drawing.Point(12, 160); 
            this._storedProcedureRadioButton.Name = "_storedProcedureRadioButton";
            this._storedProcedureRadioButton.Size = new System.Drawing.Size(489, 20); 
            this._storedProcedureRadioButton.TabIndex = 30; 
            //
            // _storedProcedurePanel 
            //
            this._storedProcedurePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._storedProcedurePanel.Controls.Add(this._storedProcedureComboBox); 
            this._storedProcedurePanel.Location = new System.Drawing.Point(28, 180);
            this._storedProcedurePanel.Name = "_storedProcedurePanel"; 
            this._storedProcedurePanel.Size = new System.Drawing.Size(265, 21); 
            this._storedProcedurePanel.TabIndex = 40;
 
            //
            // _commandTextBox
            //
            this._commandTextBox.AcceptsReturn = true; 
            this._commandTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._commandTextBox.Location = new System.Drawing.Point(0, 0);
            this._commandTextBox.Multiline = true; 
            this._commandTextBox.Name = "_commandTextBox";
            this._commandTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._commandTextBox.Size = new System.Drawing.Size(480, 93);
            this._commandTextBox.TabIndex = 20; 
            this._commandTextBox.TextChanged += new System.EventHandler(this.OnCommandTextBoxTextChanged);
            // 
            // _queryBuilderButton 
            //
            this._queryBuilderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._queryBuilderButton.Location = new System.Drawing.Point(330, 98);
            this._queryBuilderButton.Name = "_queryBuilderButton";
            this._queryBuilderButton.Size = new System.Drawing.Size(150, 23);
            this._queryBuilderButton.TabIndex = 30; 
            this._queryBuilderButton.Click += new System.EventHandler(this.OnQueryBuilderButtonClick);
 
            // 
            // _storedProcedureComboBox
            // 
            this._storedProcedureComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._storedProcedureComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._storedProcedureComboBox.Location = new System.Drawing.Point(0, 0);
            this._storedProcedureComboBox.Name = "_storedProcedureComboBox"; 
            this._storedProcedureComboBox.Size = new System.Drawing.Size(265, 21); 
            this._storedProcedureComboBox.TabIndex = 10;
            this._storedProcedureComboBox.SelectedIndexChanged += new System.EventHandler(this.OnStoredProcedureComboBoxSelectedIndexChanged); 
            //
            // SqlDataSourceCustomCommandEditor
            //
            this.Controls.Add(this._sqlRadioButton); 
            this.Controls.Add(this._sqlPanel);
            this.Controls.Add(this._storedProcedureRadioButton); 
            this.Controls.Add(this._storedProcedurePanel); 
            this.Name = "SqlDataSourceCustomCommandEditor";
            this.Size = new System.Drawing.Size(522, 230); 
            this._storedProcedurePanel.ResumeLayout(false);
            this._sqlPanel.ResumeLayout(false);
            this._sqlPanel.PerformLayout();
            this.ResumeLayout(false); 
        }
        #endregion 
 
        /// <devdoc>
        /// Gets the current query (command + parameters) that the user has entered. 
        /// </devdoc>
        public SqlDataSourceQuery GetQuery() {
            Debug.Assert(_dataConnection != null);
 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 

                DbProviderFactory factory; 

                // Get parameters from the query
                if (_sqlRadioButton.Checked) {
                    if (_commandTextBox.Text.Trim().Length > 0) { 
                        // We assume that new text typed into the textbox is command text
                        // and not a stored procedure. However if the user didn't change the 
                        // text, we preserve their old setting. 
                        SqlDataSourceCommandType commandType;
                        if (String.Equals(_commandTextBox.Text, _originalCommand, StringComparison.OrdinalIgnoreCase)) { 
                            commandType = _commandType;
                        }
                        else {
                            commandType = SqlDataSourceCommandType.Text; 
                        }
                        // If the provider supports named parameters (SqlClient, OracleClient), then 
                        // we parse the command to detect the parameters. If named parameters are not 
                        // supported, then we just use the old parameters that the user already had set.
                        // For the Select command we always parse regardless of the provider since we 
                        // need to detect new parameters for the Configure Parameters panel.
                        ICollection mergedParameters;
                        factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
                        if ((_editorMode == QueryBuilderMode.Select) || 
                            SqlDataSourceDesigner.SupportsNamedParameters(factory)) {
                            Parameter[] derivedParameters = _sqlDataSourceDesigner.InferParameterNames(_dataConnection, _commandTextBox.Text, commandType); 
                            if (derivedParameters == null) { 
                                // Parameters could not be derived
                                return null; 
                            }
                            ArrayList newParameters = new ArrayList(derivedParameters);
                            mergedParameters = MergeParameters(_parameters, newParameters, SqlDataSourceDesigner.SupportsNamedParameters(factory));
                        } 
                        else {
                            mergedParameters = _parameters; 
                        } 
                        return new SqlDataSourceQuery(_commandTextBox.Text, commandType, mergedParameters);
                    } 
                    else {
                        // No text specified, return empty query
                        return new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    } 
                }
                else { 
                    // Stored procedure from list - get from DesignerDataStoredProcedure.Parameters 
                    StoredProcedureItem sprocItem = _storedProcedureComboBox.SelectedItem as StoredProcedureItem;
                    if (sprocItem == null) { 
                        // No stored procedure selected, return empty query
                        return new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    }
 
                    // Get parameters from stored procedure object
                    ArrayList newParameters = new ArrayList(); 
                    ICollection sprocParameters = null; 
                    try {
                        sprocParameters = sprocItem.DesignerDataStoredProcedure.Parameters; 
                    }
                    catch (Exception ex) {
                        UIServiceHelper.ShowError(
                            _sqlDataSourceDesigner.Component.Site, 
                            ex,
                            SR.GetString(SR.SqlDataSourceCustomCommandEditor_CouldNotGetStoredProcedureSchema)); 
                        return null; 
                    }
 
                    factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
                    if (sprocParameters != null && sprocParameters.Count > 0) {
                        foreach (DesignerDataParameter designerDataParameter in sprocParameters) {
                            // Trim off the parameter prefix if it is present 
                            string parameterName = SqlDataSourceDesigner.StripParameterPrefix(designerDataParameter.Name);
                            Parameter parameter = new Parameter(parameterName, SqlDataSourceDesigner.ConvertDbTypeToTypeCode(designerDataParameter.DataType)); 
                            parameter.Direction = designerDataParameter.Direction; 
                            newParameters.Add(parameter);
                        } 
                    }
                    ICollection mergedParameters = MergeParameters(_parameters, newParameters, SqlDataSourceDesigner.SupportsNamedParameters(factory));
                    //
                    return new SqlDataSourceQuery(sprocItem.DesignerDataStoredProcedure.Name, SqlDataSourceCommandType.StoredProcedure, mergedParameters); 
                }
            } 
            finally { 
                Cursor.Current = originalCursor;
            } 
        }

        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that 
        /// are not supported by the designer.
        /// </devdoc> 
        private void InitializeUI() { 
            _queryBuilderButton.Text = SR.GetString(SR.SqlDataSourceCustomCommandEditor_QueryBuilderButton);
            _sqlRadioButton.Text = SR.GetString(SR.SqlDataSourceCustomCommandEditor_SqlLabel); 
            _storedProcedureRadioButton.Text = SR.GetString(SR.SqlDataSourceCustomCommandEditor_StoredProcedureLabel);
        }

        /// <devdoc> 
        /// Goes through all the new parameters and merges in original
        /// parameters that are matches. 
        /// </devdoc> 
        private ICollection MergeParameters(ICollection originalParameters, ArrayList newParameters, bool useNamedParameters) {
            System.Collections.Generic.List<Parameter> unusedOriginalParameters = new System.Collections.Generic.List<Parameter>(); 
            foreach (Parameter p in originalParameters) {
                unusedOriginalParameters.Add(p);
            }
 
            System.Collections.Generic.List<Parameter> mergedParameters = new System.Collections.Generic.List<Parameter>();
 
            for (int i = 0; i < newParameters.Count; i++) { 
                Parameter newParameter = (Parameter)newParameters[i];
                Parameter foundParameter = null; 
                foreach (Parameter originalParameter in unusedOriginalParameters) {
                    // Parameters are matched if either their name and direction match,
                    // or if they are both ReturnValue parameters (since in that case the
                    // names don't matter). 
                    bool namesAndDirectionsMatch =
                        useNamedParameters ? 
                        (String.Equals(originalParameter.Name, newParameter.Name, StringComparison.OrdinalIgnoreCase) && originalParameter.Direction == newParameter.Direction) : 
                        (originalParameter.Direction == newParameter.Direction);
                    bool bothAreReturnValue = (originalParameter.Direction == ParameterDirection.ReturnValue) && 
                                              (newParameter.Direction == ParameterDirection.ReturnValue);
                    if (namesAndDirectionsMatch || bothAreReturnValue) {
                        // A matching original parameter was found
                        foundParameter = originalParameter; 
                        break;
                    } 
                } 
                if (foundParameter != null) {
                    // Replace the new parameter with the original and remove it from the unused list 
                    mergedParameters.Add(foundParameter);
                    unusedOriginalParameters.Remove(foundParameter);
                }
                else { 
                    // Add the new parameter only if it is an input or input/output parameter
                    if (newParameter.Direction == ParameterDirection.Input || 
                        newParameter.Direction == ParameterDirection.InputOutput) { 
                        mergedParameters.Add(newParameter);
                    } 
                }
            }

            return mergedParameters; 
        }
 
        private void OnCommandChanged(EventArgs e) { 
            EventHandler handler = Events[EventCommandChanged] as EventHandler;
            if (handler != null) { 
                handler(this, e);
            }
        }
 
        private void OnCommandTextBoxTextChanged(object sender, System.EventArgs e) {
            OnCommandChanged(EventArgs.Empty); 
        } 

        private void OnQueryBuilderButtonClick(object sender, System.EventArgs e) { 
            Debug.Assert(_dataEnvironment != null);
            Debug.Assert(_dataConnection != null);

            IServiceProvider serviceProvider = _sqlDataSourceDesigner.Component.Site; 

            if ((_dataConnection.ConnectionString != null) && (_dataConnection.ConnectionString.Trim().Length == 0)) { 
                UIServiceHelper.ShowError(serviceProvider, SR.GetString(SR.SqlDataSourceCustomCommandEditor_NoConnectionString)); 
                return;
            } 

            // Launch query builder

            DesignerDataConnection newConnection = _dataConnection; 
            if (String.IsNullOrEmpty(_dataConnection.ProviderName)) {
                newConnection = new DesignerDataConnection(_dataConnection.Name, SqlDataSourceDesigner.DefaultProviderName, _dataConnection.ConnectionString, _dataConnection.IsConfigured); 
            } 

            string newQuery = _dataEnvironment.BuildQuery(this, newConnection, _editorMode, _commandTextBox.Text); 
            if ((newQuery != null) && (newQuery.Length > 0)) {
                _commandTextBox.Text = newQuery;
                _commandTextBox.Focus();
                _commandTextBox.Select(0, 0); 
            }
        } 
 
        private void OnSqlRadioButtonCheckedChanged(object sender, System.EventArgs e) {
            UpdateEnabledState(); 
        }

        private void OnStoredProcedureComboBoxSelectedIndexChanged(object sender, EventArgs e) {
            OnCommandChanged(EventArgs.Empty); 
        }
 
        public void SetCommandData(SqlDataSourceDesigner sqlDataSourceDesigner, QueryBuilderMode editorMode) { 
            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
            _editorMode = editorMode;

            _queryBuilderButton.Enabled = false;
            IServiceProvider serviceProvider = _sqlDataSourceDesigner.Component.Site; 
            Debug.Assert(serviceProvider != null);
            if (serviceProvider != null) { 
                // Get data environment 
                _dataEnvironment = (IDataEnvironment)serviceProvider.GetService(typeof(IDataEnvironment));
            } 
        }

        public void SetConnection(DesignerDataConnection dataConnection) {
            Debug.Assert(dataConnection != null); 
            _dataConnection = dataConnection;
        } 
 
        /// <devdoc>
        /// Sets the current query (command + parameters) to prepopulate the UI. 
        /// </devdoc>
        public void SetQuery(SqlDataSourceQuery query) {
            _storedProcedureComboBox.SelectedIndex = -1;
 
            if (_storedProcedures != null) {
                // Try to find stored procedure in list, assuming the command is a stored procedure 
                foreach (StoredProcedureItem sprocItem in _storedProcedureComboBox.Items) { 
                    if (sprocItem.DesignerDataStoredProcedure.Name == query.Command) {
                        _storedProcedureComboBox.SelectedItem = sprocItem; 
                        break;
                    }
                }
            } 

            if (_storedProcedureComboBox.SelectedIndex != -1) { 
                // Matching stored procedure was found, so enable the sproc picker 
                _sqlRadioButton.Checked = false;
                _storedProcedureRadioButton.Checked = true; 
            }
            else {
                // No matching stored procedure was found, the command must be a
                // regular SQL command. By default we also select the first sproc 
                // in the dropdown as well.
                _sqlRadioButton.Checked = true; 
                _storedProcedureRadioButton.Checked = false; 
                if (_storedProcedureComboBox.Items.Count > 0) {
                    _storedProcedureComboBox.SelectedIndex = 0; 
                }
            }

            if (!_queryInitialized) { 
                // If the query has never been initialized, initialize it
                _commandTextBox.Text = query.Command; 
                _originalCommand = query.Command; 
                _commandType = query.CommandType;
                _parameters = query.Parameters; 

                _queryInitialized = true;
            }
 
            UpdateEnabledState();
        } 
 

        public void SetStoredProcedures(ICollection storedProcedures) { 
            _storedProcedures = storedProcedures;

            // Disable stored procedure UI if there are none
            bool hasStoredProcedures = ((_storedProcedures != null) && (_storedProcedures.Count > 0)); 
            _storedProcedureRadioButton.Enabled = hasStoredProcedures;
 
            // Populate stored procedure combobox 
            _storedProcedureComboBox.Items.Clear();
            if (hasStoredProcedures) { 
                System.Collections.Generic.List<StoredProcedureItem> sprocItems = new System.Collections.Generic.List<StoredProcedureItem>();
                foreach (DesignerDataStoredProcedure storedProcedure in _storedProcedures) {
                    sprocItems.Add(new StoredProcedureItem(storedProcedure));
                } 
                sprocItems.Sort(new System.Comparison<StoredProcedureItem>(
                    delegate(StoredProcedureItem a, StoredProcedureItem b) { 
                        return String.Compare(a.DesignerDataStoredProcedure.Name, b.DesignerDataStoredProcedure.Name, StringComparison.InvariantCultureIgnoreCase); 
                    }));
                _storedProcedureComboBox.Items.AddRange(sprocItems.ToArray()); 
                _storedProcedureComboBox.InvalidateDropDownWidth();
            }
        }
 
        private void UpdateEnabledState() {
            bool inCommandTextMode = _sqlRadioButton.Checked; 
            _commandTextBox.Enabled = inCommandTextMode; 
            _queryBuilderButton.Enabled = inCommandTextMode;
            _storedProcedureComboBox.Enabled = !inCommandTextMode; 

            OnCommandChanged(EventArgs.Empty);
        }
 

        /// <devdoc> 
        /// Represents a stored procedure a user can select. 
        /// </devdoc>
        private sealed class StoredProcedureItem { 
            private DesignerDataStoredProcedure _designerDataStoredProcedure;

            public StoredProcedureItem(DesignerDataStoredProcedure designerDataStoredProcedure) {
                Debug.Assert(designerDataStoredProcedure != null); 
                _designerDataStoredProcedure = designerDataStoredProcedure;
            } 
 
            public DesignerDataStoredProcedure DesignerDataStoredProcedure {
                get { 
                    return _designerDataStoredProcedure;
                }
            }
 
            public override string ToString() {
                return _designerDataStoredProcedure.Name; 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
