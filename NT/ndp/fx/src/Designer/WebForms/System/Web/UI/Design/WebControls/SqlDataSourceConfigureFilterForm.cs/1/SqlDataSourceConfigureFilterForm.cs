//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConfigureFilterForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Data.Common; 
    using System.ComponentModel.Design.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms;

    using ControlItem = ParameterEditorUserControl.ControlItem; 

    /// <devdoc> 
    /// Form for building a select command filter for a SqlDataSource. 
    /// </devdoc>
    internal class SqlDataSourceConfigureFilterForm : DesignerForm { 
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.Label _columnLabel;
        private AutoSizeComboBox _columnsComboBox;
        private AutoSizeComboBox _operatorsComboBox; 
        private System.Windows.Forms.Label _operatorLabel;
        private System.Windows.Forms.Label _expressionLabel; 
        private System.Windows.Forms.Button _addButton; 
        private System.Windows.Forms.GroupBox _propertiesGroupBox;
        private System.Windows.Forms.TextBox _expressionTextBox; 
        private System.Windows.Forms.Label _whereClausesLabel;
        private System.Windows.Forms.Button _removeButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Panel _propertiesPanel;
        private AutoSizeComboBox _sourceComboBox; 
        private System.Windows.Forms.ListView _whereClausesListView; 
        private System.Windows.Forms.ColumnHeader _expressionColumnHeader;
        private System.Windows.Forms.ColumnHeader _valueColumnHeader; 
        private System.Windows.Forms.TextBox _valueTextBox;
        private System.Windows.Forms.Label _valueLabel;
        private System.Windows.Forms.Label _sourceLabel;
 
        private static System.Collections.Generic.IDictionary<Type, ParameterEditor> _parameterEditors;
        private SqlDataSourceDesigner _sqlDataSourceDesigner; 
        private SqlDataSourceTableQuery _tableQuery; 

 
        /// <devdoc>
        /// Creates a new SqlDataSourceConfigureFilterForm.
        /// </devdoc>
        public SqlDataSourceConfigureFilterForm(SqlDataSourceDesigner sqlDataSourceDesigner, SqlDataSourceTableQuery tableQuery) : base(sqlDataSourceDesigner.Component.Site) { 
            Debug.Assert(sqlDataSourceDesigner != null);
            Debug.Assert(tableQuery != null); 
 
            _sqlDataSourceDesigner = sqlDataSourceDesigner;
            _tableQuery = tableQuery.Clone(); 

            InitializeComponent();
            InitializeUI();
 
            // Add editors to drop down list and parent them to the form
            CreateParameterList(); 
            foreach (ParameterEditor editor in _parameterEditors.Values) { 
                editor.Visible = false;
                _propertiesPanel.Controls.Add(editor); 
                _sourceComboBox.Items.Add(editor);
                editor.ParameterChanged += new EventHandler(OnParameterChanged);
            }
            _sourceComboBox.InvalidateDropDownWidth(); 

            Cursor originalCursor = Cursor.Current; 
            try { 
                Cursor.Current = Cursors.WaitCursor;
 

                // Populate field list
                foreach (DesignerDataColumn designerDataColumn in tableQuery.DesignerDataTable.Columns) {
                    // 
                    _columnsComboBox.Items.Add(new ColumnItem(designerDataColumn));
                } 
                _columnsComboBox.InvalidateDropDownWidth(); 

 
                // Populate initial filter list
                foreach (SqlDataSourceFilterClause filterClause in _tableQuery.FilterClauses) {
                    FilterClauseItem item = new FilterClauseItem(_sqlDataSourceDesigner.Component.Site, _tableQuery, filterClause, (SqlDataSource)_sqlDataSourceDesigner.Component);
                    _whereClausesListView.Items.Add(item); 
                    item.Refresh();
                } 
                if (_whereClausesListView.Items.Count > 0) { 
                    _whereClausesListView.Items[0].Selected = true;
                    _whereClausesListView.Items[0].Focused = true; 
                }


                // Disable the OK button until the user makes a change (add/remove parameter) 
                _okButton.Enabled = false;
 
                // Update UI 
                UpdateDeleteButton();
                UpdateOperators(); 
            }
            finally {
                Cursor.Current = originalCursor;
            } 
        }
 
        /// <devdoc> 
        /// Gets the list of filter clauses created in the form.
        /// </devdoc> 
        public System.Collections.Generic.IList<SqlDataSourceFilterClause> FilterClauses {
            get {
                return _tableQuery.FilterClauses;
            } 
        }
 
        protected override string HelpTopic { 
            get {
                return "net.Asp.SqlDataSource.ConfigureFilter"; 
            }
        }

        /// <devdoc> 
        /// Creates the internal list of parameter editors.
        /// </devdoc> 
        private void CreateParameterList() { 
            _parameterEditors = new System.Collections.Generic.Dictionary<Type, ParameterEditor>();
            _parameterEditors.Add(typeof(Parameter), new StaticParameterEditor(ServiceProvider)); 
            _parameterEditors.Add(typeof(ControlParameter), new ControlParameterEditor(ServiceProvider, (SqlDataSource)_sqlDataSourceDesigner.Component));
            _parameterEditors.Add(typeof(CookieParameter), new CookieParameterEditor(ServiceProvider));
            _parameterEditors.Add(typeof(FormParameter), new FormParameterEditor(ServiceProvider));
            _parameterEditors.Add(typeof(ProfileParameter), new ProfileParameterEditor(ServiceProvider)); 
            _parameterEditors.Add(typeof(QueryStringParameter), new QueryStringParameterEditor(ServiceProvider));
            _parameterEditors.Add(typeof(SessionParameter), new SessionParameterEditor(ServiceProvider)); 
        } 

        #region Designer generated code 
        private void InitializeComponent() {
            this._helpLabel = new System.Windows.Forms.Label();
            this._columnLabel = new System.Windows.Forms.Label();
            this._columnsComboBox = new AutoSizeComboBox(); 
            this._operatorsComboBox = new AutoSizeComboBox();
            this._operatorLabel = new System.Windows.Forms.Label(); 
            this._whereClausesLabel = new System.Windows.Forms.Label(); 
            this._addButton = new System.Windows.Forms.Button();
            this._removeButton = new System.Windows.Forms.Button(); 
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._expressionLabel = new System.Windows.Forms.Label();
            this._propertiesGroupBox = new System.Windows.Forms.GroupBox(); 
            this._propertiesPanel = new System.Windows.Forms.Panel();
            this._sourceComboBox = new AutoSizeComboBox(); 
            this._sourceLabel = new System.Windows.Forms.Label(); 
            this._expressionTextBox = new System.Windows.Forms.TextBox();
            this._whereClausesListView = new System.Windows.Forms.ListView(); 
            this._expressionColumnHeader = new System.Windows.Forms.ColumnHeader("");
            this._valueColumnHeader = new System.Windows.Forms.ColumnHeader("");
            this._valueTextBox = new System.Windows.Forms.TextBox();
            this._valueLabel = new System.Windows.Forms.Label(); 
            this._propertiesGroupBox.SuspendLayout();
            this.SuspendLayout(); 
            // 
            // _helpLabel
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(12, 11);
            this._helpLabel.Name = "_helpLabel"; 
            this._helpLabel.Size = new System.Drawing.Size(524, 42);
            this._helpLabel.TabIndex = 10; 
            // 
            // _columnLabel
            // 
            this._columnLabel.Location = new System.Drawing.Point(12, 59);
            this._columnLabel.Name = "_columnLabel";
            this._columnLabel.Size = new System.Drawing.Size(172, 15);
            this._columnLabel.TabIndex = 20; 
            //
            // _columnsComboBox 
            // 
            this._columnsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._columnsComboBox.Location = new System.Drawing.Point(12, 77); 
            this._columnsComboBox.Name = "_columnsComboBox";
            this._columnsComboBox.Size = new System.Drawing.Size(172, 21);
            this._columnsComboBox.Sorted = true;
            this._columnsComboBox.TabIndex = 30; 
            this._columnsComboBox.SelectedIndexChanged += new System.EventHandler(this.OnColumnsComboBoxSelectedIndexChanged);
            // 
            // _operatorLabel 
            //
            this._operatorLabel.Location = new System.Drawing.Point(12, 104); 
            this._operatorLabel.Name = "_operatorLabel";
            this._operatorLabel.Size = new System.Drawing.Size(172, 15);
            this._operatorLabel.TabIndex = 40;
            // 
            // _operatorsComboBox
            // 
            this._operatorsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._operatorsComboBox.Location = new System.Drawing.Point(12, 122);
            this._operatorsComboBox.Name = "_operatorsComboBox"; 
            this._operatorsComboBox.Size = new System.Drawing.Size(172, 21);
            this._operatorsComboBox.TabIndex = 50;
            this._operatorsComboBox.SelectedIndexChanged += new System.EventHandler(this.OnOperatorsComboBoxSelectedIndexChanged);
            // 
            // _sourceLabel
            // 
            this._sourceLabel.Location = new System.Drawing.Point(12, 148); 
            this._sourceLabel.Name = "_sourceLabel";
            this._sourceLabel.Size = new System.Drawing.Size(172, 15); 
            this._sourceLabel.TabIndex = 60;
            //
            // _sourceComboBox
            // 
            this._sourceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._sourceComboBox.Location = new System.Drawing.Point(12, 166); 
            this._sourceComboBox.Name = "_sourceComboBox"; 
            this._sourceComboBox.Size = new System.Drawing.Size(172, 21);
            this._sourceComboBox.TabIndex = 70; 
            this._sourceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnSourceComboBoxSelectedIndexChanged);
            //
            // _propertiesGroupBox
            // 
            this._propertiesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._propertiesGroupBox.Controls.Add(this._propertiesPanel); 
            this._propertiesGroupBox.Location = new System.Drawing.Point(243, 59);
            this._propertiesGroupBox.Name = "_propertiesGroupBox"; 
            this._propertiesGroupBox.Size = new System.Drawing.Size(194, 127);
            this._propertiesGroupBox.TabIndex = 80;
            this._propertiesGroupBox.TabStop = false;
            // 
            // _propertiesPanel
            // 
            this._propertiesPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._propertiesPanel.Location = new System.Drawing.Point(10, 15);
            this._propertiesPanel.Name = "_propertiesPanel";
            this._propertiesPanel.Size = new System.Drawing.Size(164, 100);
            this._propertiesPanel.TabIndex = 10; 
            //
            // _expressionLabel 
            // 
            this._expressionLabel.Location = new System.Drawing.Point(12, 194);
            this._expressionLabel.Name = "_expressionLabel"; 
            this._expressionLabel.Size = new System.Drawing.Size(225, 15);
            this._expressionLabel.TabIndex = 90;
            //
            // _expressionTextBox 
            //
            this._expressionTextBox.Location = new System.Drawing.Point(12, 212); 
            this._expressionTextBox.Name = "_expressionTextBox"; 
            this._expressionTextBox.ReadOnly = true;
            this._expressionTextBox.Size = new System.Drawing.Size(224, 20); 
            this._expressionTextBox.TabIndex = 100;
            //
            // _valueLabel
            // 
            this._valueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._valueLabel.Location = new System.Drawing.Point(243, 194); 
            this._valueLabel.Name = "_valueLabel";
            this._valueLabel.Size = new System.Drawing.Size(194, 15); 
            this._valueLabel.TabIndex = 110;
            //
            // _valueTextBox
            // 
            this._valueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._valueTextBox.Location = new System.Drawing.Point(243, 212); 
            this._valueTextBox.Name = "_valueTextBox";
            this._valueTextBox.ReadOnly = true; 
            this._valueTextBox.Size = new System.Drawing.Size(194, 20);
            this._valueTextBox.TabIndex = 120;
            //
            // _addButton 
            //
            this._addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right))); 
            this._addButton.Location = new System.Drawing.Point(443, 212); 
            this._addButton.Name = "_addButton";
            this._addButton.Size = new System.Drawing.Size(90, 23); 
            this._addButton.TabIndex = 125;
            this._addButton.Click += new System.EventHandler(this.OnAddButtonClick);
            //
            // _whereClausesLabel 
            //
            this._whereClausesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._whereClausesLabel.Location = new System.Drawing.Point(12, 242);
            this._whereClausesLabel.Name = "_whereClausesLabel"; 
            this._whereClausesLabel.Size = new System.Drawing.Size(425, 15);
            this._whereClausesLabel.TabIndex = 130;
            //
            // _whereClausesListView 
            //
            this._whereClausesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._whereClausesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { 
            this._expressionColumnHeader,
            this._valueColumnHeader});
            this._whereClausesListView.FullRowSelect = true;
            this._whereClausesListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable; 
            this._whereClausesListView.HideSelection = false;
            this._whereClausesListView.Location = new System.Drawing.Point(12, 260); 
            this._whereClausesListView.MultiSelect = false; 
            this._whereClausesListView.Name = "_whereClausesListView";
            this._whereClausesListView.Size = new System.Drawing.Size(425, 78); 
            this._whereClausesListView.TabIndex = 135;
            this._whereClausesListView.View = System.Windows.Forms.View.Details;
            this._whereClausesListView.SelectedIndexChanged += new System.EventHandler(this.OnWhereClausesListViewSelectedIndexChanged);
            // 
            // _expressionColumnHeader
            // 
            this._expressionColumnHeader.Text = ""; 
            this._expressionColumnHeader.Width = 225;
            // 
            // _valueColumnHeader
            //
            this._valueColumnHeader.Text = "";
            this._valueColumnHeader.Width = 160; 
            //
            // _removeButton 
            // 
            this._removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._removeButton.Location = new System.Drawing.Point(442, 260); 
            this._removeButton.Name = "_removeButton";
            this._removeButton.Size = new System.Drawing.Size(90, 23);
            this._removeButton.TabIndex = 140;
            this._removeButton.Click += new System.EventHandler(this.OnRemoveButtonClick); 
            //
            // _okButton 
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.Location = new System.Drawing.Point(376, 346); 
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 150;
            this._okButton.Click += new System.EventHandler(this.OnOkButtonClick); 
            //
            // _cancelButton 
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this._cancelButton.Location = new System.Drawing.Point(457, 346);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 160; 
            this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick);
            // 
            // SqlDataSourceConfigureFilterForm 
            //
            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(544, 381);
            this.Controls.Add(this._valueTextBox);
            this.Controls.Add(this._valueLabel); 
            this.Controls.Add(this._whereClausesListView);
            this.Controls.Add(this._expressionTextBox); 
            this.Controls.Add(this._propertiesGroupBox); 
            this.Controls.Add(this._expressionLabel);
            this.Controls.Add(this._okButton); 
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._removeButton);
            this.Controls.Add(this._addButton);
            this.Controls.Add(this._whereClausesLabel); 
            this.Controls.Add(this._operatorsComboBox);
            this.Controls.Add(this._operatorLabel); 
            this.Controls.Add(this._columnsComboBox); 
            this.Controls.Add(this._columnLabel);
            this.Controls.Add(this._helpLabel); 
            this.Controls.Add(this._sourceLabel);
            this.Controls.Add(this._sourceComboBox);
            this.MinimumSize = new System.Drawing.Size(552, 415);
            this.Name = "SqlDataSourceConfigureFilterForm"; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this._propertiesGroupBox.ResumeLayout(false); 
 
            InitializeForm();
 
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion 

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that 
        /// are not supported by the designer.
        /// </devdoc> 
        private void InitializeUI() {
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_HelpLabel);
            _columnLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ColumnLabel);
            _operatorLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_OperatorLabel); 
            _whereClausesLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_WhereLabel);
            _expressionLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ExpressionLabel); 
            _valueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ValueLabel); 
            _expressionColumnHeader.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ExpressionColumnHeader);
            _valueColumnHeader.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ValueColumnHeader); 

            _propertiesGroupBox.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterPropertiesGroup);
            _sourceLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_SourceLabel);
            _addButton.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_AddButton); 
            _removeButton.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_RemoveButton);
 
            _okButton.Text = SR.GetString(SR.OK); 
            _cancelButton.Text = SR.GetString(SR.Cancel);
            Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_Caption); 
        }

        /// <devdoc>
        /// Gets the filter clause that is currently being edited. If there is 
        /// not enough information to get the filter clause (e.g. some
        /// parameter value is not specified), null is returned. 
        /// </devdoc> 
        private SqlDataSourceFilterClause GetCurrentFilterClause() {
            OperatorItem operatorItem = _operatorsComboBox.SelectedItem as OperatorItem; 
            if (operatorItem == null) {
                return null;
            }
            ColumnItem columnItem = _columnsComboBox.SelectedItem as ColumnItem; 
            if (columnItem == null) {
                return null; 
            } 

            string value; 
            Parameter parameter;

            if (operatorItem.IsBinary) {
                // Binary operator needs a valid parameter object for the r-value 
                ParameterEditor editor = _sourceComboBox.SelectedItem as ParameterEditor;
                if (editor == null) { 
                    return null; 
                }
 
                // Finish setting up the parameter, if there is one
                parameter = editor.Parameter;
                if (parameter != null) {
                    // Create list of existing parameter names so we can ensure that 
                    // the new parameter we are adding gets a unique name.
                    SqlDataSourceQuery selectQuery = _tableQuery.GetSelectQuery(); 
                    StringCollection usedParameterNames = new StringCollection(); 
                    if (selectQuery != null && selectQuery.Parameters != null) {
                        foreach (Parameter p in selectQuery.Parameters) { 
                            usedParameterNames.Add(p.Name);
                        }
                    }
 
                    SqlDataSourceColumnData columnData = new SqlDataSourceColumnData(_tableQuery.DesignerDataConnection, columnItem.DesignerDataColumn, usedParameterNames);
 
                    parameter.Name = columnData.WebParameterName; 
                    parameter.Type = SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnItem.DesignerDataColumn.DataType);
 
                    // Get the parameter placeholder (e.g. ? or @column1)
                    value = columnData.ParameterPlaceholder;
                }
                else { 
                    value = String.Empty;
                } 
            } 
            else {
                // Unary operators have no r-value 
                value = "";
                parameter = null;
            }
 

            SqlDataSourceFilterClause filterClause = new SqlDataSourceFilterClause( 
                _tableQuery.DesignerDataConnection, 
                _tableQuery.DesignerDataTable,
                columnItem.DesignerDataColumn, 
                operatorItem.OperatorFormat,
                operatorItem.IsBinary,
                value,
                parameter); 

            return filterClause; 
        } 

        private void OnAddButtonClick(object sender, System.EventArgs e) { 

            SqlDataSourceFilterClause filterClause = GetCurrentFilterClause();

            FilterClauseItem item = new FilterClauseItem(_sqlDataSourceDesigner.Component.Site, _tableQuery, filterClause, (SqlDataSource)_sqlDataSourceDesigner.Component); 
            _whereClausesListView.Items.Add(item);
            item.Selected = true; 
            item.Focused = true; 
            item.EnsureVisible();
            _tableQuery.FilterClauses.Add(filterClause); 

            _columnsComboBox.SelectedIndex = -1;

            // Now that the user has made a change, we enable the OK button 
            _okButton.Enabled = true;
            item.Refresh(); 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void OnCancelButtonClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close(); 
        }
 
        private void OnColumnsComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateOperators();
        } 

        /// <devdoc>
        /// </devdoc>
        private void OnOkButtonClick(object sender, System.EventArgs e) { 
            DialogResult = DialogResult.OK;
            Close(); 
        } 

        private void OnOperatorsComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateParameter();
        }

        private void OnParameterChanged(object sender, EventArgs e) { 
            UpdateExpression();
            UpdateAddButtonEnabled(); 
        } 

        private void OnRemoveButtonClick(object sender, System.EventArgs e) { 
            if (_whereClausesListView.SelectedItems.Count > 0) {
                int selectedIndex = _whereClausesListView.SelectedIndices[0];
                FilterClauseItem selectedFilterClause = _whereClausesListView.SelectedItems[0] as FilterClauseItem;
                _whereClausesListView.Items.Remove(selectedFilterClause); 
                _tableQuery.FilterClauses.Remove(selectedFilterClause.FilterClause);
 
                // Now that the user has made a change, we enable the OK button 
                _okButton.Enabled = true;
 
                if (selectedIndex < _whereClausesListView.Items.Count) {
                    ListViewItem item = _whereClausesListView.Items[selectedIndex];
                    item.Selected = true;
                    item.Focused = true; 
                    item.EnsureVisible();
                    _whereClausesListView.Focus(); 
                } 
                else if (_whereClausesListView.Items.Count > 0) {
                    ListViewItem item = _whereClausesListView.Items[selectedIndex - 1]; 
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    _whereClausesListView.Focus(); 
                }
            } 
        } 

        private void OnSourceComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateParameter();
        }

        private void OnWhereClausesListViewSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateDeleteButton();
        } 
 
        /// <devdoc>
        /// Updates the enabled state of the Delete button based on the user's selection 
        /// </devdoc>
        private void UpdateDeleteButton() {
            _removeButton.Enabled = (_whereClausesListView.SelectedItems.Count > 0);
        } 

        /// <devdoc> 
        /// Updates the enabled state of the Add button based on the user's selection 
        /// </devdoc>
        private void UpdateAddButtonEnabled() { 
            ColumnItem columnItem = _columnsComboBox.SelectedItem as ColumnItem;
            if (columnItem == null) {
                _addButton.Enabled = false;
                return; 
            }
 
            OperatorItem operatorItem = _operatorsComboBox.SelectedItem as OperatorItem; 
            if (operatorItem == null) {
                _addButton.Enabled = false; 
                return;
            }

            ParameterEditor editor = _sourceComboBox.SelectedItem as ParameterEditor; 
            _addButton.Enabled = (!operatorItem.IsBinary) ^ (editor != null && editor.HasCompleteInformation);
        } 
 
        /// <devdoc>
        /// Updates the operator list based on the selected field. 
        /// </devdoc>
        private void UpdateOperators() {
            if (_columnsComboBox.SelectedItem == null) {
                _operatorsComboBox.SelectedItem = -1; 
                _operatorsComboBox.Items.Clear();
                _operatorsComboBox.Enabled = false; 
                _operatorLabel.Enabled = false; 

                UpdateParameter(); 
                return;
            }

            _operatorsComboBox.Enabled = true; 
            _operatorLabel.Enabled = true;
 
 
            // Populate operator list
            _operatorsComboBox.Items.Clear(); 

            // Standard operators (apply to all types)
            _operatorsComboBox.Items.Add(new OperatorItem("{0} = {1}", "=", true));
            _operatorsComboBox.Items.Add(new OperatorItem("{0} < {1}", "<", true)); 
            _operatorsComboBox.Items.Add(new OperatorItem("{0} > {1}", ">", true));
            _operatorsComboBox.Items.Add(new OperatorItem("{0} <= {1}", "<=", true)); 
            _operatorsComboBox.Items.Add(new OperatorItem("{0} >= {1}", ">=", true)); 
            _operatorsComboBox.Items.Add(new OperatorItem("{0} <> {1}", "<>", true));
 
            ColumnItem columnItem = (ColumnItem)_columnsComboBox.SelectedItem;
            DesignerDataColumn column = columnItem.DesignerDataColumn;
            if (column.Nullable) {
                // Only show these operators for nullable types 
                _operatorsComboBox.Items.Add(new OperatorItem("{0} IS NULL", "IS NULL", false));
                _operatorsComboBox.Items.Add(new OperatorItem("{0} IS NOT NULL", "IS NOT NULL", false)); 
            } 

            DbType dataType = column.DataType; 
            if ((dataType == DbType.String) ||
                (dataType == DbType.AnsiString) ||
                (dataType == DbType.AnsiStringFixedLength) ||
                (dataType == DbType.StringFixedLength)) { 
                // Only show these operators for string types
                _operatorsComboBox.Items.Add(new OperatorItem("{0} LIKE '%' + {1} + '%'", "LIKE", true)); 
                _operatorsComboBox.Items.Add(new OperatorItem("{0} NOT LIKE '%' + {1} + '%'", "NOT LIKE", true)); 
                _operatorsComboBox.Items.Add(new OperatorItem("CONTAINS({0}, {1})", "CONTAINS", true));
            } 

            _operatorsComboBox.InvalidateDropDownWidth();

            // Automatically select the "=" operator 
            _operatorsComboBox.SelectedIndex = 0;
 
            UpdateParameter(); 
        }
 
        /// <devdoc>
        /// Updates the expression textbox.
        /// </devdoc>
        private void UpdateExpression() { 
            ParameterEditor editor = _sourceComboBox.SelectedItem as ParameterEditor;
            if ((_operatorsComboBox.SelectedItem != null) && 
                (editor != null)) { 

                SqlDataSourceFilterClause filterClause = GetCurrentFilterClause(); 
                if (filterClause != null) {
                    _expressionTextBox.Text = filterClause.ToString();
                }
                else { 
                    _expressionTextBox.Text = String.Empty;
                } 
 
                if (editor.Parameter == null) {
                    _valueTextBox.Text = String.Empty; 
                }
                else {
                    bool isHelperText;
                    string expression = ParameterEditorUserControl.GetParameterExpression(_sqlDataSourceDesigner.Component.Site, editor.Parameter, (SqlDataSource)_sqlDataSourceDesigner.Component, out isHelperText); 
                    if (isHelperText) {
                        _valueTextBox.Text = String.Empty; 
                    } 
                    else {
                        _valueTextBox.Text = expression; 
                    }
                }
            }
            else { 
                _expressionTextBox.Text = String.Empty;
                _valueTextBox.Text = String.Empty; 
            } 
        }
 
        /// <devdoc>
        /// Updates the parameter configuration area.
        /// </devdoc>
        private void UpdateParameter() { 
            OperatorItem operatorItem = _operatorsComboBox.SelectedItem as OperatorItem;
            if ((operatorItem != null) && (operatorItem.IsBinary)) { 
                _expressionLabel.Enabled = true; 
                _expressionTextBox.Enabled = true;
                _valueLabel.Enabled = true; 
                _valueTextBox.Enabled = true;

                _propertiesGroupBox.Enabled = true;
                _sourceLabel.Enabled = true; 
                _sourceComboBox.Enabled = true;
            } 
            else { 
                _expressionLabel.Enabled = false;
                _expressionTextBox.Enabled = false; 
                _valueLabel.Enabled = false;
                _valueTextBox.Enabled = false;

                _propertiesGroupBox.Enabled = false; 
                _sourceLabel.Enabled = false;
                _sourceComboBox.Enabled = false; 
                _sourceComboBox.SelectedItem = null; 
            }
 
            // Figure out which parameter editor, if any, we should show

            // Hide all the editors
            foreach (ParameterEditor pe in _parameterEditors.Values) { 
                pe.Visible = false;
            } 
 
            ParameterEditor editor = _sourceComboBox.SelectedItem as ParameterEditor;
            if (editor != null) { 
                // Pick only the one editor we need and reinitialize it
                editor.Visible = true;
                editor.Initialize();
                _propertiesPanel.Visible = true; 
            }
            else { 
                _propertiesPanel.Visible = false; 
            }
 
            UpdateExpression();
            UpdateAddButtonEnabled();
        }
 

        /// <devdoc> 
        /// Represents a column a user can select to filter by. 
        /// </devdoc>
        private sealed class ColumnItem { 
            private DesignerDataColumn _designerDataColumn;

            public ColumnItem(DesignerDataColumn designerDataColumn) {
                Debug.Assert(designerDataColumn != null); 
                _designerDataColumn = designerDataColumn;
            } 
 
            public DesignerDataColumn DesignerDataColumn {
                get { 
                    return _designerDataColumn;
                }
            }
 
            public override string ToString() {
                return _designerDataColumn.Name; 
            } 
        }
 
        /// <devdoc>
        /// Represents an operator a user can select to filter with.
        /// </devdoc>
        private sealed class OperatorItem { 
            private string _operatorName;
            private bool _isBinary; 
            private string _operatorFormat; 

            public OperatorItem(string operatorFormat, string operatorName, bool isBinary) { 
                _operatorName = operatorName;
                _operatorFormat = operatorFormat;
                _isBinary = isBinary;
            } 

            public bool IsBinary { 
                get { 
                    return _isBinary;
                } 
            }

            public string OperatorFormat {
                get { 
                    return _operatorFormat;
                } 
            } 

            public string OperatorName { 
                get {
                    return _operatorName;
                }
            } 

            public override string ToString() { 
                return _operatorName; 
            }
        } 

        /// <devdoc>
        /// Represents a filter clause in the select query.
        /// </devdoc> 
        private sealed class FilterClauseItem : ListViewItem {
            private SqlDataSourceFilterClause _filterClause; 
            private SqlDataSourceTableQuery _tableQuery; 
            private IServiceProvider _serviceProvider;
            private SqlDataSource _sqlDataSource; 

            public FilterClauseItem(IServiceProvider serviceProvider, SqlDataSourceTableQuery tableQuery, SqlDataSourceFilterClause filterClause, SqlDataSource sqlDataSource) {
                Debug.Assert(filterClause != null, "Did not expect null FilterClause");
                Debug.Assert(tableQuery != null, "Did not expect null TableQuery"); 
                _filterClause = filterClause;
                _tableQuery = tableQuery; 
                _serviceProvider = serviceProvider; 
                _sqlDataSource = sqlDataSource;
            } 

            public SqlDataSourceFilterClause FilterClause {
                get {
                    return _filterClause; 
                }
            } 
 
            /// <devdoc>
            /// Refreshes the properties of the ListViewItem. 
            /// </devdoc>
            public void Refresh() {
                SubItems.Clear();
 
                Text = _filterClause.ToString();
 
                ListView listView = ListView; 
                IServiceProvider serviceProvider = null;
                if (listView != null) { 
                    serviceProvider = ((SqlDataSourceConfigureFilterForm)listView.Parent).ServiceProvider;
                }
                string parameterExpression;
                if (_filterClause.Parameter == null) { 
                    parameterExpression = String.Empty;
                } 
                else { 
                    // Parameter expression
                    bool isHelperText; 
                    parameterExpression = ParameterEditorUserControl.GetParameterExpression(serviceProvider, _filterClause.Parameter, _sqlDataSource, out isHelperText);
                    if (isHelperText) {
                        parameterExpression = String.Empty;
                    } 
                }
                ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem(); 
                subItem.Text = parameterExpression; 
                SubItems.Add(subItem);
            } 
        }


        #region Parameter editors 
        /// <devdoc>
        /// An abstract base class for all parameter editors. 
        /// </devdoc> 
        private abstract class ParameterEditor : System.Windows.Forms.Panel {
            private static readonly object EventParameterChanged = new object(); 
            protected const int ControlWidth = 220;

            private IServiceProvider _serviceProvider;
 
            protected ParameterEditor(IServiceProvider serviceProvider) {
                _serviceProvider = serviceProvider; 
            } 

            /// <devdoc> 
            /// The display name for this editor.
            /// </devdoc>
            public abstract string EditorName {
                get; 
            }
 
            /// <devdoc> 
            /// Indicates whether this editor has sufficient information to continue.
            /// </devdoc> 
            public abstract bool HasCompleteInformation {
                get;
            }
 
            /// <devdoc>
            /// The parameter, if any, that this editor is editing. 
            /// </devdoc> 
            public abstract Parameter Parameter {
                get; 
            }

            protected IServiceProvider ServiceProvider {
                get { 
                    return _serviceProvider;
                } 
            } 

            /// <devdoc> 
            /// Raised when the parameter being edited changes.
            /// </devdoc>
            public event EventHandler ParameterChanged {
                add { 
                    Events.AddHandler(EventParameterChanged, value);
                } 
                remove { 
                    Events.RemoveHandler(EventParameterChanged, value);
                } 
            }

            /// <devdoc>
            /// Initializes the editor to a clean state. 
            /// </devdoc>
            public abstract void Initialize(); 
 
            protected void OnParameterChanged() {
                EventHandler handler = Events[EventParameterChanged] as EventHandler; 
                if (handler != null) {
                    handler(this, EventArgs.Empty);
                }
            } 

            public override string ToString() { 
                return EditorName; 
            }
        } 

        /// <devdoc>
        /// Parameter editor for static parameters.
        /// </devdoc> 
        private sealed class StaticParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox; 
            private Parameter _parameter;
 
            public StaticParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) {
                SuspendLayout();
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 0); 
                _defaultValueLabel.Name = "StaticDefaultValueLabel";
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 10;
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_StaticParameterEditor_ValueLabel); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 23); 
                _defaultValueTextBox.Name = "StaticDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _defaultValueTextBox.TabIndex = 20;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);

                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
 
                Dock = DockStyle.Fill; 
                ResumeLayout();
            } 

            public override string EditorName {
                get {
                    return "None"; 
                }
            } 
 
            public override bool HasCompleteInformation {
                get { 
                    return true;
                }
            }
 
            public override Parameter Parameter {
                get { 
                    return _parameter; 
                }
            } 

            public override void Initialize() {
                _parameter = new Parameter();
                _defaultValueTextBox.Text = String.Empty; 
            }
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
                OnParameterChanged(); 
            }
        }

        /// <devdoc> 
        /// Parameter editor for cookie parameters.
        /// </devdoc> 
        private sealed class CookieParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _cookieNameLabel;
            private System.Windows.Forms.TextBox _cookieNameTextBox; 
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private CookieParameter _parameter;
 
            public CookieParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) {
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44); 

                _cookieNameLabel = new System.Windows.Forms.Label(); 
                _cookieNameTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox();
 
                _cookieNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _cookieNameLabel.Location = new System.Drawing.Point(0, 0); 
                _cookieNameLabel.Name = "CookieNameLabel"; 
                _cookieNameLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _cookieNameLabel.TabIndex = 10; 
                _cookieNameLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_CookieParameterEditor_CookieNameLabel);

                _cookieNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _cookieNameTextBox.Location = new System.Drawing.Point(0, 23); 
                _cookieNameTextBox.Name = "CookieNameTextBox";
                _cookieNameTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _cookieNameTextBox.TabIndex = 20; 
                _cookieNameTextBox.TextChanged += new System.EventHandler(this.OnCookieNameTextBoxTextChanged);
 
                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "CookieDefaultValueLabel";
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _defaultValueLabel.TabIndex = 30;
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68); 
                _defaultValueTextBox.Name = "CookieDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                Controls.Add(_cookieNameLabel); 
                Controls.Add(_cookieNameTextBox); 
                Controls.Add(_defaultValueLabel);
                Controls.Add(_defaultValueTextBox); 

                Dock = DockStyle.Fill;
                ResumeLayout();
            } 

            public override string EditorName { 
                get { 
                    return "Cookie";
                } 
            }

            public override bool HasCompleteInformation {
                get { 
                    return (_parameter.CookieName.Length > 0);
                } 
            } 

            public override Parameter Parameter { 
                get {
                    return _parameter;
                }
            } 

            public override void Initialize() { 
                _parameter = new CookieParameter(); 
                _cookieNameTextBox.Text = String.Empty;
                _defaultValueTextBox.Text = String.Empty; 
            }

            private void OnCookieNameTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.CookieName = _cookieNameTextBox.Text; 
                OnParameterChanged();
            } 
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.DefaultValue = _defaultValueTextBox.Text; 
            }
        }

        /// <devdoc> 
        /// Parameter editor for control parameters.
        /// </devdoc> 
        private sealed class ControlParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _controlIDLabel;
            private AutoSizeComboBox _controlIDComboBox; 
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private ControlParameter _parameter;
            private System.Web.UI.Control _control; 

            public ControlParameterEditor(IServiceProvider serviceProvider, System.Web.UI.Control control) : base(serviceProvider) { 
                _control = control; 

                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);

                _controlIDLabel = new System.Windows.Forms.Label();
                _controlIDComboBox = new AutoSizeComboBox(); 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
 
                _controlIDLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _controlIDLabel.Location = new System.Drawing.Point(0, 0); 
                _controlIDLabel.Name = "ControlIDLabel";
                _controlIDLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _controlIDLabel.TabIndex = 10;
                _controlIDLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ControlParameterEditor_ControlIDLabel); 

                _controlIDComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _controlIDComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
                _controlIDComboBox.Location = new System.Drawing.Point(0, 23);
                _controlIDComboBox.Name = "ControlIDComboBox"; 
                _controlIDComboBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _controlIDComboBox.Sorted = true;
                _controlIDComboBox.TabIndex = 20;
                _controlIDComboBox.SelectedIndexChanged += new System.EventHandler(this.OnControlIDComboBoxSelectedIndexChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48); 
                _defaultValueLabel.Name = "ControlDefaultValueLabel";
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _defaultValueLabel.TabIndex = 30;
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue);

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "ControlDefaultValueTextBox"; 
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _defaultValueTextBox.TabIndex = 40;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                Controls.Add(_controlIDLabel);
                Controls.Add(_controlIDComboBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
 
 
                // Populate control list
                if (ServiceProvider != null) { 
                    IDesignerHost designerHost = (IDesignerHost)ServiceProvider.GetService(typeof(IDesignerHost));
                    if (designerHost != null) {
                        ControlItem[] controlItems = ControlItem.GetControlItems(designerHost, _control);
                        foreach (ControlItem controlItem in controlItems) { 
                            _controlIDComboBox.Items.Add(controlItem);
                        } 
                        _controlIDComboBox.InvalidateDropDownWidth(); 
                    }
                } 

                Dock = DockStyle.Fill;
                ResumeLayout();
            } 

            public override string EditorName { 
                get { 
                    return "Control";
                } 
            }

            public override bool HasCompleteInformation {
                get { 
                    return (_controlIDComboBox.SelectedItem != null);
                } 
            } 

            public override Parameter Parameter { 
                get {
                    return _parameter;
                }
            } 

            public override void Initialize() { 
                _parameter = new ControlParameter(); 
                _controlIDComboBox.SelectedItem = null;
                _defaultValueTextBox.Text = String.Empty; 
            }

            private void OnControlIDComboBoxSelectedIndexChanged(object s, System.EventArgs e) {
                ControlItem controlItem = _controlIDComboBox.SelectedItem as ControlItem; 

                if (controlItem == null) { 
                    _parameter.ControlID = String.Empty; 
                    _parameter.PropertyName = String.Empty;
                } 
                else {
                    _parameter.ControlID = controlItem.ControlID;
                    _parameter.PropertyName = controlItem.PropertyName;
                } 

                OnParameterChanged(); 
            } 

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            }
        }
 
        /// <devdoc>
        /// Parameter editor for form parameters. 
        /// </devdoc> 
        private sealed class FormParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _formFieldLabel; 
            private System.Windows.Forms.TextBox _formFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private FormParameter _parameter; 

            public FormParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _formFieldLabel = new System.Windows.Forms.Label();
                _formFieldTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 

                _formFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _formFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _formFieldLabel.Name = "FormFieldLabel";
                _formFieldLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _formFieldLabel.TabIndex = 10;
                _formFieldLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_FormParameterEditor_FormFieldLabel);

                _formFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _formFieldTextBox.Location = new System.Drawing.Point(0, 23);
                _formFieldTextBox.Name = "FormFieldTextBox"; 
                _formFieldTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _formFieldTextBox.TabIndex = 20;
                _formFieldTextBox.TextChanged += new System.EventHandler(this.OnFormFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "FormDefaultValueLabel"; 
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "FormDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                Controls.Add(_formFieldLabel); 
                Controls.Add(_formFieldTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);

                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }
 
            public override string EditorName { 
                get {
                    return "Form"; 
                }
            }

            public override bool HasCompleteInformation { 
                get {
                    return (_parameter.FormField.Length > 0); 
                } 
            }
 
            public override Parameter Parameter {
                get {
                    return _parameter;
                } 
            }
 
            public override void Initialize() { 
                _parameter = new FormParameter();
                _formFieldTextBox.Text = String.Empty; 
                _defaultValueTextBox.Text = String.Empty;
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            } 
 
            private void OnFormFieldTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.FormField = _formFieldTextBox.Text; 
                OnParameterChanged();
            }
        }
 
        /// <devdoc>
        /// Parameter editor for session parameters. 
        /// </devdoc> 
        private sealed class SessionParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _sessionFieldLabel; 
            private System.Windows.Forms.TextBox _sessionFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private SessionParameter _parameter; 

            public SessionParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _sessionFieldLabel = new System.Windows.Forms.Label();
                _sessionFieldTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 

                _sessionFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _sessionFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _sessionFieldLabel.Name = "SessionFieldLabel";
                _sessionFieldLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _sessionFieldLabel.TabIndex = 10;
                _sessionFieldLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_SessionParameterEditor_SessionFieldLabel);

                _sessionFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _sessionFieldTextBox.Location = new System.Drawing.Point(0, 23);
                _sessionFieldTextBox.Name = "SessionFieldTextBox"; 
                _sessionFieldTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _sessionFieldTextBox.TabIndex = 20;
                _sessionFieldTextBox.TextChanged += new System.EventHandler(this.OnSessionFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "SessionDefaultValueLabel"; 
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "SessionDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                Controls.Add(_sessionFieldLabel); 
                Controls.Add(_sessionFieldTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);

                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }
 
            public override string EditorName { 
                get {
                    return "Session"; 
                }
            }

            public override bool HasCompleteInformation { 
                get {
                    return (_parameter.SessionField.Length > 0); 
                } 
            }
 
            public override Parameter Parameter {
                get {
                    return _parameter;
                } 
            }
 
            public override void Initialize() { 
                _parameter = new SessionParameter();
                _sessionFieldTextBox.Text = String.Empty; 
                _defaultValueTextBox.Text = String.Empty;
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            } 
 
            private void OnSessionFieldTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.SessionField = _sessionFieldTextBox.Text; 
                OnParameterChanged();
            }
        }
 
        /// <devdoc>
        /// Parameter editor for query string parameters. 
        /// </devdoc> 
        private sealed class QueryStringParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _queryStringFieldLabel; 
            private System.Windows.Forms.TextBox _queryStringFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private QueryStringParameter _parameter; 

            public QueryStringParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _queryStringFieldLabel = new System.Windows.Forms.Label();
                _queryStringFieldTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 

                _queryStringFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _queryStringFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _queryStringFieldLabel.Name = "QueryStringFieldLabel";
                _queryStringFieldLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _queryStringFieldLabel.TabIndex = 10;
                _queryStringFieldLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_QueryStringParameterEditor_QueryStringFieldLabel);

                _queryStringFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _queryStringFieldTextBox.Location = new System.Drawing.Point(0, 23);
                _queryStringFieldTextBox.Name = "QueryStringFieldTextBox"; 
                _queryStringFieldTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _queryStringFieldTextBox.TabIndex = 20;
                _queryStringFieldTextBox.TextChanged += new System.EventHandler(this.OnQueryStringFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "QueryStringDefaultValueLabel"; 
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "QueryStringDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                Controls.Add(_queryStringFieldLabel); 
                Controls.Add(_queryStringFieldTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);

                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }
 
            public override string EditorName { 
                get {
                    return "QueryString"; 
                }
            }

            public override bool HasCompleteInformation { 
                get {
                    return (_parameter.QueryStringField.Length > 0); 
                } 
            }
 
            public override Parameter Parameter {
                get {
                    return _parameter;
                } 
            }
 
            public override void Initialize() { 
                _parameter = new QueryStringParameter();
                _queryStringFieldTextBox.Text = String.Empty; 
                _defaultValueTextBox.Text = String.Empty;
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            } 
 
            private void OnQueryStringFieldTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.QueryStringField = _queryStringFieldTextBox.Text; 
                OnParameterChanged();
            }
        }
 
        /// <devdoc>
        /// Parameter editor for profile parameters. 
        /// </devdoc> 
        private sealed class ProfileParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _propertyNameLabel; 
            private System.Windows.Forms.TextBox _propertyNameTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private ProfileParameter _parameter; 

            public ProfileParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _propertyNameLabel = new System.Windows.Forms.Label();
                _propertyNameTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 

                _propertyNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _propertyNameLabel.Location = new System.Drawing.Point(0, 0); 
                _propertyNameLabel.Name = "ProfilePropertyNameLabel";
                _propertyNameLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _propertyNameLabel.TabIndex = 10;
                _propertyNameLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ProfileParameterEditor_PropertyNameLabel);

                _propertyNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _propertyNameTextBox.Location = new System.Drawing.Point(0, 23);
                _propertyNameTextBox.Name = "ProfilePropertyNameTextBox"; 
                _propertyNameTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _propertyNameTextBox.TabIndex = 20;
                _propertyNameTextBox.TextChanged += new System.EventHandler(this.OnPropertyNameTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "ProfileDefaultValueLabel"; 
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "ProfileDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                Controls.Add(_propertyNameLabel); 
                Controls.Add(_propertyNameTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);

                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }
 
            public override string EditorName { 
                get {
                    return "Profile"; 
                }
            }

            public override bool HasCompleteInformation { 
                get {
                    return (_parameter.PropertyName.Length > 0); 
                } 
            }
 
            public override Parameter Parameter {
                get {
                    return _parameter;
                } 
            }
 
            public override void Initialize() { 
                _parameter = new ProfileParameter();
                _propertyNameTextBox.Text = String.Empty; 
                _defaultValueTextBox.Text = String.Empty;
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            } 
 
            private void OnPropertyNameTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.PropertyName = _propertyNameTextBox.Text; 
                OnParameterChanged();
            }
        }
        #endregion 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConfigureFilterForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Data.Common; 
    using System.ComponentModel.Design.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms;

    using ControlItem = ParameterEditorUserControl.ControlItem; 

    /// <devdoc> 
    /// Form for building a select command filter for a SqlDataSource. 
    /// </devdoc>
    internal class SqlDataSourceConfigureFilterForm : DesignerForm { 
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.Label _columnLabel;
        private AutoSizeComboBox _columnsComboBox;
        private AutoSizeComboBox _operatorsComboBox; 
        private System.Windows.Forms.Label _operatorLabel;
        private System.Windows.Forms.Label _expressionLabel; 
        private System.Windows.Forms.Button _addButton; 
        private System.Windows.Forms.GroupBox _propertiesGroupBox;
        private System.Windows.Forms.TextBox _expressionTextBox; 
        private System.Windows.Forms.Label _whereClausesLabel;
        private System.Windows.Forms.Button _removeButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Panel _propertiesPanel;
        private AutoSizeComboBox _sourceComboBox; 
        private System.Windows.Forms.ListView _whereClausesListView; 
        private System.Windows.Forms.ColumnHeader _expressionColumnHeader;
        private System.Windows.Forms.ColumnHeader _valueColumnHeader; 
        private System.Windows.Forms.TextBox _valueTextBox;
        private System.Windows.Forms.Label _valueLabel;
        private System.Windows.Forms.Label _sourceLabel;
 
        private static System.Collections.Generic.IDictionary<Type, ParameterEditor> _parameterEditors;
        private SqlDataSourceDesigner _sqlDataSourceDesigner; 
        private SqlDataSourceTableQuery _tableQuery; 

 
        /// <devdoc>
        /// Creates a new SqlDataSourceConfigureFilterForm.
        /// </devdoc>
        public SqlDataSourceConfigureFilterForm(SqlDataSourceDesigner sqlDataSourceDesigner, SqlDataSourceTableQuery tableQuery) : base(sqlDataSourceDesigner.Component.Site) { 
            Debug.Assert(sqlDataSourceDesigner != null);
            Debug.Assert(tableQuery != null); 
 
            _sqlDataSourceDesigner = sqlDataSourceDesigner;
            _tableQuery = tableQuery.Clone(); 

            InitializeComponent();
            InitializeUI();
 
            // Add editors to drop down list and parent them to the form
            CreateParameterList(); 
            foreach (ParameterEditor editor in _parameterEditors.Values) { 
                editor.Visible = false;
                _propertiesPanel.Controls.Add(editor); 
                _sourceComboBox.Items.Add(editor);
                editor.ParameterChanged += new EventHandler(OnParameterChanged);
            }
            _sourceComboBox.InvalidateDropDownWidth(); 

            Cursor originalCursor = Cursor.Current; 
            try { 
                Cursor.Current = Cursors.WaitCursor;
 

                // Populate field list
                foreach (DesignerDataColumn designerDataColumn in tableQuery.DesignerDataTable.Columns) {
                    // 
                    _columnsComboBox.Items.Add(new ColumnItem(designerDataColumn));
                } 
                _columnsComboBox.InvalidateDropDownWidth(); 

 
                // Populate initial filter list
                foreach (SqlDataSourceFilterClause filterClause in _tableQuery.FilterClauses) {
                    FilterClauseItem item = new FilterClauseItem(_sqlDataSourceDesigner.Component.Site, _tableQuery, filterClause, (SqlDataSource)_sqlDataSourceDesigner.Component);
                    _whereClausesListView.Items.Add(item); 
                    item.Refresh();
                } 
                if (_whereClausesListView.Items.Count > 0) { 
                    _whereClausesListView.Items[0].Selected = true;
                    _whereClausesListView.Items[0].Focused = true; 
                }


                // Disable the OK button until the user makes a change (add/remove parameter) 
                _okButton.Enabled = false;
 
                // Update UI 
                UpdateDeleteButton();
                UpdateOperators(); 
            }
            finally {
                Cursor.Current = originalCursor;
            } 
        }
 
        /// <devdoc> 
        /// Gets the list of filter clauses created in the form.
        /// </devdoc> 
        public System.Collections.Generic.IList<SqlDataSourceFilterClause> FilterClauses {
            get {
                return _tableQuery.FilterClauses;
            } 
        }
 
        protected override string HelpTopic { 
            get {
                return "net.Asp.SqlDataSource.ConfigureFilter"; 
            }
        }

        /// <devdoc> 
        /// Creates the internal list of parameter editors.
        /// </devdoc> 
        private void CreateParameterList() { 
            _parameterEditors = new System.Collections.Generic.Dictionary<Type, ParameterEditor>();
            _parameterEditors.Add(typeof(Parameter), new StaticParameterEditor(ServiceProvider)); 
            _parameterEditors.Add(typeof(ControlParameter), new ControlParameterEditor(ServiceProvider, (SqlDataSource)_sqlDataSourceDesigner.Component));
            _parameterEditors.Add(typeof(CookieParameter), new CookieParameterEditor(ServiceProvider));
            _parameterEditors.Add(typeof(FormParameter), new FormParameterEditor(ServiceProvider));
            _parameterEditors.Add(typeof(ProfileParameter), new ProfileParameterEditor(ServiceProvider)); 
            _parameterEditors.Add(typeof(QueryStringParameter), new QueryStringParameterEditor(ServiceProvider));
            _parameterEditors.Add(typeof(SessionParameter), new SessionParameterEditor(ServiceProvider)); 
        } 

        #region Designer generated code 
        private void InitializeComponent() {
            this._helpLabel = new System.Windows.Forms.Label();
            this._columnLabel = new System.Windows.Forms.Label();
            this._columnsComboBox = new AutoSizeComboBox(); 
            this._operatorsComboBox = new AutoSizeComboBox();
            this._operatorLabel = new System.Windows.Forms.Label(); 
            this._whereClausesLabel = new System.Windows.Forms.Label(); 
            this._addButton = new System.Windows.Forms.Button();
            this._removeButton = new System.Windows.Forms.Button(); 
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._expressionLabel = new System.Windows.Forms.Label();
            this._propertiesGroupBox = new System.Windows.Forms.GroupBox(); 
            this._propertiesPanel = new System.Windows.Forms.Panel();
            this._sourceComboBox = new AutoSizeComboBox(); 
            this._sourceLabel = new System.Windows.Forms.Label(); 
            this._expressionTextBox = new System.Windows.Forms.TextBox();
            this._whereClausesListView = new System.Windows.Forms.ListView(); 
            this._expressionColumnHeader = new System.Windows.Forms.ColumnHeader("");
            this._valueColumnHeader = new System.Windows.Forms.ColumnHeader("");
            this._valueTextBox = new System.Windows.Forms.TextBox();
            this._valueLabel = new System.Windows.Forms.Label(); 
            this._propertiesGroupBox.SuspendLayout();
            this.SuspendLayout(); 
            // 
            // _helpLabel
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(12, 11);
            this._helpLabel.Name = "_helpLabel"; 
            this._helpLabel.Size = new System.Drawing.Size(524, 42);
            this._helpLabel.TabIndex = 10; 
            // 
            // _columnLabel
            // 
            this._columnLabel.Location = new System.Drawing.Point(12, 59);
            this._columnLabel.Name = "_columnLabel";
            this._columnLabel.Size = new System.Drawing.Size(172, 15);
            this._columnLabel.TabIndex = 20; 
            //
            // _columnsComboBox 
            // 
            this._columnsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._columnsComboBox.Location = new System.Drawing.Point(12, 77); 
            this._columnsComboBox.Name = "_columnsComboBox";
            this._columnsComboBox.Size = new System.Drawing.Size(172, 21);
            this._columnsComboBox.Sorted = true;
            this._columnsComboBox.TabIndex = 30; 
            this._columnsComboBox.SelectedIndexChanged += new System.EventHandler(this.OnColumnsComboBoxSelectedIndexChanged);
            // 
            // _operatorLabel 
            //
            this._operatorLabel.Location = new System.Drawing.Point(12, 104); 
            this._operatorLabel.Name = "_operatorLabel";
            this._operatorLabel.Size = new System.Drawing.Size(172, 15);
            this._operatorLabel.TabIndex = 40;
            // 
            // _operatorsComboBox
            // 
            this._operatorsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._operatorsComboBox.Location = new System.Drawing.Point(12, 122);
            this._operatorsComboBox.Name = "_operatorsComboBox"; 
            this._operatorsComboBox.Size = new System.Drawing.Size(172, 21);
            this._operatorsComboBox.TabIndex = 50;
            this._operatorsComboBox.SelectedIndexChanged += new System.EventHandler(this.OnOperatorsComboBoxSelectedIndexChanged);
            // 
            // _sourceLabel
            // 
            this._sourceLabel.Location = new System.Drawing.Point(12, 148); 
            this._sourceLabel.Name = "_sourceLabel";
            this._sourceLabel.Size = new System.Drawing.Size(172, 15); 
            this._sourceLabel.TabIndex = 60;
            //
            // _sourceComboBox
            // 
            this._sourceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._sourceComboBox.Location = new System.Drawing.Point(12, 166); 
            this._sourceComboBox.Name = "_sourceComboBox"; 
            this._sourceComboBox.Size = new System.Drawing.Size(172, 21);
            this._sourceComboBox.TabIndex = 70; 
            this._sourceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnSourceComboBoxSelectedIndexChanged);
            //
            // _propertiesGroupBox
            // 
            this._propertiesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._propertiesGroupBox.Controls.Add(this._propertiesPanel); 
            this._propertiesGroupBox.Location = new System.Drawing.Point(243, 59);
            this._propertiesGroupBox.Name = "_propertiesGroupBox"; 
            this._propertiesGroupBox.Size = new System.Drawing.Size(194, 127);
            this._propertiesGroupBox.TabIndex = 80;
            this._propertiesGroupBox.TabStop = false;
            // 
            // _propertiesPanel
            // 
            this._propertiesPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._propertiesPanel.Location = new System.Drawing.Point(10, 15);
            this._propertiesPanel.Name = "_propertiesPanel";
            this._propertiesPanel.Size = new System.Drawing.Size(164, 100);
            this._propertiesPanel.TabIndex = 10; 
            //
            // _expressionLabel 
            // 
            this._expressionLabel.Location = new System.Drawing.Point(12, 194);
            this._expressionLabel.Name = "_expressionLabel"; 
            this._expressionLabel.Size = new System.Drawing.Size(225, 15);
            this._expressionLabel.TabIndex = 90;
            //
            // _expressionTextBox 
            //
            this._expressionTextBox.Location = new System.Drawing.Point(12, 212); 
            this._expressionTextBox.Name = "_expressionTextBox"; 
            this._expressionTextBox.ReadOnly = true;
            this._expressionTextBox.Size = new System.Drawing.Size(224, 20); 
            this._expressionTextBox.TabIndex = 100;
            //
            // _valueLabel
            // 
            this._valueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._valueLabel.Location = new System.Drawing.Point(243, 194); 
            this._valueLabel.Name = "_valueLabel";
            this._valueLabel.Size = new System.Drawing.Size(194, 15); 
            this._valueLabel.TabIndex = 110;
            //
            // _valueTextBox
            // 
            this._valueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._valueTextBox.Location = new System.Drawing.Point(243, 212); 
            this._valueTextBox.Name = "_valueTextBox";
            this._valueTextBox.ReadOnly = true; 
            this._valueTextBox.Size = new System.Drawing.Size(194, 20);
            this._valueTextBox.TabIndex = 120;
            //
            // _addButton 
            //
            this._addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right))); 
            this._addButton.Location = new System.Drawing.Point(443, 212); 
            this._addButton.Name = "_addButton";
            this._addButton.Size = new System.Drawing.Size(90, 23); 
            this._addButton.TabIndex = 125;
            this._addButton.Click += new System.EventHandler(this.OnAddButtonClick);
            //
            // _whereClausesLabel 
            //
            this._whereClausesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._whereClausesLabel.Location = new System.Drawing.Point(12, 242);
            this._whereClausesLabel.Name = "_whereClausesLabel"; 
            this._whereClausesLabel.Size = new System.Drawing.Size(425, 15);
            this._whereClausesLabel.TabIndex = 130;
            //
            // _whereClausesListView 
            //
            this._whereClausesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._whereClausesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { 
            this._expressionColumnHeader,
            this._valueColumnHeader});
            this._whereClausesListView.FullRowSelect = true;
            this._whereClausesListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable; 
            this._whereClausesListView.HideSelection = false;
            this._whereClausesListView.Location = new System.Drawing.Point(12, 260); 
            this._whereClausesListView.MultiSelect = false; 
            this._whereClausesListView.Name = "_whereClausesListView";
            this._whereClausesListView.Size = new System.Drawing.Size(425, 78); 
            this._whereClausesListView.TabIndex = 135;
            this._whereClausesListView.View = System.Windows.Forms.View.Details;
            this._whereClausesListView.SelectedIndexChanged += new System.EventHandler(this.OnWhereClausesListViewSelectedIndexChanged);
            // 
            // _expressionColumnHeader
            // 
            this._expressionColumnHeader.Text = ""; 
            this._expressionColumnHeader.Width = 225;
            // 
            // _valueColumnHeader
            //
            this._valueColumnHeader.Text = "";
            this._valueColumnHeader.Width = 160; 
            //
            // _removeButton 
            // 
            this._removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._removeButton.Location = new System.Drawing.Point(442, 260); 
            this._removeButton.Name = "_removeButton";
            this._removeButton.Size = new System.Drawing.Size(90, 23);
            this._removeButton.TabIndex = 140;
            this._removeButton.Click += new System.EventHandler(this.OnRemoveButtonClick); 
            //
            // _okButton 
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.Location = new System.Drawing.Point(376, 346); 
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 150;
            this._okButton.Click += new System.EventHandler(this.OnOkButtonClick); 
            //
            // _cancelButton 
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this._cancelButton.Location = new System.Drawing.Point(457, 346);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 160; 
            this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick);
            // 
            // SqlDataSourceConfigureFilterForm 
            //
            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(544, 381);
            this.Controls.Add(this._valueTextBox);
            this.Controls.Add(this._valueLabel); 
            this.Controls.Add(this._whereClausesListView);
            this.Controls.Add(this._expressionTextBox); 
            this.Controls.Add(this._propertiesGroupBox); 
            this.Controls.Add(this._expressionLabel);
            this.Controls.Add(this._okButton); 
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._removeButton);
            this.Controls.Add(this._addButton);
            this.Controls.Add(this._whereClausesLabel); 
            this.Controls.Add(this._operatorsComboBox);
            this.Controls.Add(this._operatorLabel); 
            this.Controls.Add(this._columnsComboBox); 
            this.Controls.Add(this._columnLabel);
            this.Controls.Add(this._helpLabel); 
            this.Controls.Add(this._sourceLabel);
            this.Controls.Add(this._sourceComboBox);
            this.MinimumSize = new System.Drawing.Size(552, 415);
            this.Name = "SqlDataSourceConfigureFilterForm"; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this._propertiesGroupBox.ResumeLayout(false); 
 
            InitializeForm();
 
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion 

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that 
        /// are not supported by the designer.
        /// </devdoc> 
        private void InitializeUI() {
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_HelpLabel);
            _columnLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ColumnLabel);
            _operatorLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_OperatorLabel); 
            _whereClausesLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_WhereLabel);
            _expressionLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ExpressionLabel); 
            _valueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ValueLabel); 
            _expressionColumnHeader.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ExpressionColumnHeader);
            _valueColumnHeader.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ValueColumnHeader); 

            _propertiesGroupBox.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterPropertiesGroup);
            _sourceLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_SourceLabel);
            _addButton.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_AddButton); 
            _removeButton.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_RemoveButton);
 
            _okButton.Text = SR.GetString(SR.OK); 
            _cancelButton.Text = SR.GetString(SR.Cancel);
            Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_Caption); 
        }

        /// <devdoc>
        /// Gets the filter clause that is currently being edited. If there is 
        /// not enough information to get the filter clause (e.g. some
        /// parameter value is not specified), null is returned. 
        /// </devdoc> 
        private SqlDataSourceFilterClause GetCurrentFilterClause() {
            OperatorItem operatorItem = _operatorsComboBox.SelectedItem as OperatorItem; 
            if (operatorItem == null) {
                return null;
            }
            ColumnItem columnItem = _columnsComboBox.SelectedItem as ColumnItem; 
            if (columnItem == null) {
                return null; 
            } 

            string value; 
            Parameter parameter;

            if (operatorItem.IsBinary) {
                // Binary operator needs a valid parameter object for the r-value 
                ParameterEditor editor = _sourceComboBox.SelectedItem as ParameterEditor;
                if (editor == null) { 
                    return null; 
                }
 
                // Finish setting up the parameter, if there is one
                parameter = editor.Parameter;
                if (parameter != null) {
                    // Create list of existing parameter names so we can ensure that 
                    // the new parameter we are adding gets a unique name.
                    SqlDataSourceQuery selectQuery = _tableQuery.GetSelectQuery(); 
                    StringCollection usedParameterNames = new StringCollection(); 
                    if (selectQuery != null && selectQuery.Parameters != null) {
                        foreach (Parameter p in selectQuery.Parameters) { 
                            usedParameterNames.Add(p.Name);
                        }
                    }
 
                    SqlDataSourceColumnData columnData = new SqlDataSourceColumnData(_tableQuery.DesignerDataConnection, columnItem.DesignerDataColumn, usedParameterNames);
 
                    parameter.Name = columnData.WebParameterName; 
                    parameter.Type = SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnItem.DesignerDataColumn.DataType);
 
                    // Get the parameter placeholder (e.g. ? or @column1)
                    value = columnData.ParameterPlaceholder;
                }
                else { 
                    value = String.Empty;
                } 
            } 
            else {
                // Unary operators have no r-value 
                value = "";
                parameter = null;
            }
 

            SqlDataSourceFilterClause filterClause = new SqlDataSourceFilterClause( 
                _tableQuery.DesignerDataConnection, 
                _tableQuery.DesignerDataTable,
                columnItem.DesignerDataColumn, 
                operatorItem.OperatorFormat,
                operatorItem.IsBinary,
                value,
                parameter); 

            return filterClause; 
        } 

        private void OnAddButtonClick(object sender, System.EventArgs e) { 

            SqlDataSourceFilterClause filterClause = GetCurrentFilterClause();

            FilterClauseItem item = new FilterClauseItem(_sqlDataSourceDesigner.Component.Site, _tableQuery, filterClause, (SqlDataSource)_sqlDataSourceDesigner.Component); 
            _whereClausesListView.Items.Add(item);
            item.Selected = true; 
            item.Focused = true; 
            item.EnsureVisible();
            _tableQuery.FilterClauses.Add(filterClause); 

            _columnsComboBox.SelectedIndex = -1;

            // Now that the user has made a change, we enable the OK button 
            _okButton.Enabled = true;
            item.Refresh(); 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void OnCancelButtonClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close(); 
        }
 
        private void OnColumnsComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateOperators();
        } 

        /// <devdoc>
        /// </devdoc>
        private void OnOkButtonClick(object sender, System.EventArgs e) { 
            DialogResult = DialogResult.OK;
            Close(); 
        } 

        private void OnOperatorsComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateParameter();
        }

        private void OnParameterChanged(object sender, EventArgs e) { 
            UpdateExpression();
            UpdateAddButtonEnabled(); 
        } 

        private void OnRemoveButtonClick(object sender, System.EventArgs e) { 
            if (_whereClausesListView.SelectedItems.Count > 0) {
                int selectedIndex = _whereClausesListView.SelectedIndices[0];
                FilterClauseItem selectedFilterClause = _whereClausesListView.SelectedItems[0] as FilterClauseItem;
                _whereClausesListView.Items.Remove(selectedFilterClause); 
                _tableQuery.FilterClauses.Remove(selectedFilterClause.FilterClause);
 
                // Now that the user has made a change, we enable the OK button 
                _okButton.Enabled = true;
 
                if (selectedIndex < _whereClausesListView.Items.Count) {
                    ListViewItem item = _whereClausesListView.Items[selectedIndex];
                    item.Selected = true;
                    item.Focused = true; 
                    item.EnsureVisible();
                    _whereClausesListView.Focus(); 
                } 
                else if (_whereClausesListView.Items.Count > 0) {
                    ListViewItem item = _whereClausesListView.Items[selectedIndex - 1]; 
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    _whereClausesListView.Focus(); 
                }
            } 
        } 

        private void OnSourceComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateParameter();
        }

        private void OnWhereClausesListViewSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateDeleteButton();
        } 
 
        /// <devdoc>
        /// Updates the enabled state of the Delete button based on the user's selection 
        /// </devdoc>
        private void UpdateDeleteButton() {
            _removeButton.Enabled = (_whereClausesListView.SelectedItems.Count > 0);
        } 

        /// <devdoc> 
        /// Updates the enabled state of the Add button based on the user's selection 
        /// </devdoc>
        private void UpdateAddButtonEnabled() { 
            ColumnItem columnItem = _columnsComboBox.SelectedItem as ColumnItem;
            if (columnItem == null) {
                _addButton.Enabled = false;
                return; 
            }
 
            OperatorItem operatorItem = _operatorsComboBox.SelectedItem as OperatorItem; 
            if (operatorItem == null) {
                _addButton.Enabled = false; 
                return;
            }

            ParameterEditor editor = _sourceComboBox.SelectedItem as ParameterEditor; 
            _addButton.Enabled = (!operatorItem.IsBinary) ^ (editor != null && editor.HasCompleteInformation);
        } 
 
        /// <devdoc>
        /// Updates the operator list based on the selected field. 
        /// </devdoc>
        private void UpdateOperators() {
            if (_columnsComboBox.SelectedItem == null) {
                _operatorsComboBox.SelectedItem = -1; 
                _operatorsComboBox.Items.Clear();
                _operatorsComboBox.Enabled = false; 
                _operatorLabel.Enabled = false; 

                UpdateParameter(); 
                return;
            }

            _operatorsComboBox.Enabled = true; 
            _operatorLabel.Enabled = true;
 
 
            // Populate operator list
            _operatorsComboBox.Items.Clear(); 

            // Standard operators (apply to all types)
            _operatorsComboBox.Items.Add(new OperatorItem("{0} = {1}", "=", true));
            _operatorsComboBox.Items.Add(new OperatorItem("{0} < {1}", "<", true)); 
            _operatorsComboBox.Items.Add(new OperatorItem("{0} > {1}", ">", true));
            _operatorsComboBox.Items.Add(new OperatorItem("{0} <= {1}", "<=", true)); 
            _operatorsComboBox.Items.Add(new OperatorItem("{0} >= {1}", ">=", true)); 
            _operatorsComboBox.Items.Add(new OperatorItem("{0} <> {1}", "<>", true));
 
            ColumnItem columnItem = (ColumnItem)_columnsComboBox.SelectedItem;
            DesignerDataColumn column = columnItem.DesignerDataColumn;
            if (column.Nullable) {
                // Only show these operators for nullable types 
                _operatorsComboBox.Items.Add(new OperatorItem("{0} IS NULL", "IS NULL", false));
                _operatorsComboBox.Items.Add(new OperatorItem("{0} IS NOT NULL", "IS NOT NULL", false)); 
            } 

            DbType dataType = column.DataType; 
            if ((dataType == DbType.String) ||
                (dataType == DbType.AnsiString) ||
                (dataType == DbType.AnsiStringFixedLength) ||
                (dataType == DbType.StringFixedLength)) { 
                // Only show these operators for string types
                _operatorsComboBox.Items.Add(new OperatorItem("{0} LIKE '%' + {1} + '%'", "LIKE", true)); 
                _operatorsComboBox.Items.Add(new OperatorItem("{0} NOT LIKE '%' + {1} + '%'", "NOT LIKE", true)); 
                _operatorsComboBox.Items.Add(new OperatorItem("CONTAINS({0}, {1})", "CONTAINS", true));
            } 

            _operatorsComboBox.InvalidateDropDownWidth();

            // Automatically select the "=" operator 
            _operatorsComboBox.SelectedIndex = 0;
 
            UpdateParameter(); 
        }
 
        /// <devdoc>
        /// Updates the expression textbox.
        /// </devdoc>
        private void UpdateExpression() { 
            ParameterEditor editor = _sourceComboBox.SelectedItem as ParameterEditor;
            if ((_operatorsComboBox.SelectedItem != null) && 
                (editor != null)) { 

                SqlDataSourceFilterClause filterClause = GetCurrentFilterClause(); 
                if (filterClause != null) {
                    _expressionTextBox.Text = filterClause.ToString();
                }
                else { 
                    _expressionTextBox.Text = String.Empty;
                } 
 
                if (editor.Parameter == null) {
                    _valueTextBox.Text = String.Empty; 
                }
                else {
                    bool isHelperText;
                    string expression = ParameterEditorUserControl.GetParameterExpression(_sqlDataSourceDesigner.Component.Site, editor.Parameter, (SqlDataSource)_sqlDataSourceDesigner.Component, out isHelperText); 
                    if (isHelperText) {
                        _valueTextBox.Text = String.Empty; 
                    } 
                    else {
                        _valueTextBox.Text = expression; 
                    }
                }
            }
            else { 
                _expressionTextBox.Text = String.Empty;
                _valueTextBox.Text = String.Empty; 
            } 
        }
 
        /// <devdoc>
        /// Updates the parameter configuration area.
        /// </devdoc>
        private void UpdateParameter() { 
            OperatorItem operatorItem = _operatorsComboBox.SelectedItem as OperatorItem;
            if ((operatorItem != null) && (operatorItem.IsBinary)) { 
                _expressionLabel.Enabled = true; 
                _expressionTextBox.Enabled = true;
                _valueLabel.Enabled = true; 
                _valueTextBox.Enabled = true;

                _propertiesGroupBox.Enabled = true;
                _sourceLabel.Enabled = true; 
                _sourceComboBox.Enabled = true;
            } 
            else { 
                _expressionLabel.Enabled = false;
                _expressionTextBox.Enabled = false; 
                _valueLabel.Enabled = false;
                _valueTextBox.Enabled = false;

                _propertiesGroupBox.Enabled = false; 
                _sourceLabel.Enabled = false;
                _sourceComboBox.Enabled = false; 
                _sourceComboBox.SelectedItem = null; 
            }
 
            // Figure out which parameter editor, if any, we should show

            // Hide all the editors
            foreach (ParameterEditor pe in _parameterEditors.Values) { 
                pe.Visible = false;
            } 
 
            ParameterEditor editor = _sourceComboBox.SelectedItem as ParameterEditor;
            if (editor != null) { 
                // Pick only the one editor we need and reinitialize it
                editor.Visible = true;
                editor.Initialize();
                _propertiesPanel.Visible = true; 
            }
            else { 
                _propertiesPanel.Visible = false; 
            }
 
            UpdateExpression();
            UpdateAddButtonEnabled();
        }
 

        /// <devdoc> 
        /// Represents a column a user can select to filter by. 
        /// </devdoc>
        private sealed class ColumnItem { 
            private DesignerDataColumn _designerDataColumn;

            public ColumnItem(DesignerDataColumn designerDataColumn) {
                Debug.Assert(designerDataColumn != null); 
                _designerDataColumn = designerDataColumn;
            } 
 
            public DesignerDataColumn DesignerDataColumn {
                get { 
                    return _designerDataColumn;
                }
            }
 
            public override string ToString() {
                return _designerDataColumn.Name; 
            } 
        }
 
        /// <devdoc>
        /// Represents an operator a user can select to filter with.
        /// </devdoc>
        private sealed class OperatorItem { 
            private string _operatorName;
            private bool _isBinary; 
            private string _operatorFormat; 

            public OperatorItem(string operatorFormat, string operatorName, bool isBinary) { 
                _operatorName = operatorName;
                _operatorFormat = operatorFormat;
                _isBinary = isBinary;
            } 

            public bool IsBinary { 
                get { 
                    return _isBinary;
                } 
            }

            public string OperatorFormat {
                get { 
                    return _operatorFormat;
                } 
            } 

            public string OperatorName { 
                get {
                    return _operatorName;
                }
            } 

            public override string ToString() { 
                return _operatorName; 
            }
        } 

        /// <devdoc>
        /// Represents a filter clause in the select query.
        /// </devdoc> 
        private sealed class FilterClauseItem : ListViewItem {
            private SqlDataSourceFilterClause _filterClause; 
            private SqlDataSourceTableQuery _tableQuery; 
            private IServiceProvider _serviceProvider;
            private SqlDataSource _sqlDataSource; 

            public FilterClauseItem(IServiceProvider serviceProvider, SqlDataSourceTableQuery tableQuery, SqlDataSourceFilterClause filterClause, SqlDataSource sqlDataSource) {
                Debug.Assert(filterClause != null, "Did not expect null FilterClause");
                Debug.Assert(tableQuery != null, "Did not expect null TableQuery"); 
                _filterClause = filterClause;
                _tableQuery = tableQuery; 
                _serviceProvider = serviceProvider; 
                _sqlDataSource = sqlDataSource;
            } 

            public SqlDataSourceFilterClause FilterClause {
                get {
                    return _filterClause; 
                }
            } 
 
            /// <devdoc>
            /// Refreshes the properties of the ListViewItem. 
            /// </devdoc>
            public void Refresh() {
                SubItems.Clear();
 
                Text = _filterClause.ToString();
 
                ListView listView = ListView; 
                IServiceProvider serviceProvider = null;
                if (listView != null) { 
                    serviceProvider = ((SqlDataSourceConfigureFilterForm)listView.Parent).ServiceProvider;
                }
                string parameterExpression;
                if (_filterClause.Parameter == null) { 
                    parameterExpression = String.Empty;
                } 
                else { 
                    // Parameter expression
                    bool isHelperText; 
                    parameterExpression = ParameterEditorUserControl.GetParameterExpression(serviceProvider, _filterClause.Parameter, _sqlDataSource, out isHelperText);
                    if (isHelperText) {
                        parameterExpression = String.Empty;
                    } 
                }
                ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem(); 
                subItem.Text = parameterExpression; 
                SubItems.Add(subItem);
            } 
        }


        #region Parameter editors 
        /// <devdoc>
        /// An abstract base class for all parameter editors. 
        /// </devdoc> 
        private abstract class ParameterEditor : System.Windows.Forms.Panel {
            private static readonly object EventParameterChanged = new object(); 
            protected const int ControlWidth = 220;

            private IServiceProvider _serviceProvider;
 
            protected ParameterEditor(IServiceProvider serviceProvider) {
                _serviceProvider = serviceProvider; 
            } 

            /// <devdoc> 
            /// The display name for this editor.
            /// </devdoc>
            public abstract string EditorName {
                get; 
            }
 
            /// <devdoc> 
            /// Indicates whether this editor has sufficient information to continue.
            /// </devdoc> 
            public abstract bool HasCompleteInformation {
                get;
            }
 
            /// <devdoc>
            /// The parameter, if any, that this editor is editing. 
            /// </devdoc> 
            public abstract Parameter Parameter {
                get; 
            }

            protected IServiceProvider ServiceProvider {
                get { 
                    return _serviceProvider;
                } 
            } 

            /// <devdoc> 
            /// Raised when the parameter being edited changes.
            /// </devdoc>
            public event EventHandler ParameterChanged {
                add { 
                    Events.AddHandler(EventParameterChanged, value);
                } 
                remove { 
                    Events.RemoveHandler(EventParameterChanged, value);
                } 
            }

            /// <devdoc>
            /// Initializes the editor to a clean state. 
            /// </devdoc>
            public abstract void Initialize(); 
 
            protected void OnParameterChanged() {
                EventHandler handler = Events[EventParameterChanged] as EventHandler; 
                if (handler != null) {
                    handler(this, EventArgs.Empty);
                }
            } 

            public override string ToString() { 
                return EditorName; 
            }
        } 

        /// <devdoc>
        /// Parameter editor for static parameters.
        /// </devdoc> 
        private sealed class StaticParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox; 
            private Parameter _parameter;
 
            public StaticParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) {
                SuspendLayout();
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 0); 
                _defaultValueLabel.Name = "StaticDefaultValueLabel";
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 10;
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_StaticParameterEditor_ValueLabel); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 23); 
                _defaultValueTextBox.Name = "StaticDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _defaultValueTextBox.TabIndex = 20;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);

                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
 
                Dock = DockStyle.Fill; 
                ResumeLayout();
            } 

            public override string EditorName {
                get {
                    return "None"; 
                }
            } 
 
            public override bool HasCompleteInformation {
                get { 
                    return true;
                }
            }
 
            public override Parameter Parameter {
                get { 
                    return _parameter; 
                }
            } 

            public override void Initialize() {
                _parameter = new Parameter();
                _defaultValueTextBox.Text = String.Empty; 
            }
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
                OnParameterChanged(); 
            }
        }

        /// <devdoc> 
        /// Parameter editor for cookie parameters.
        /// </devdoc> 
        private sealed class CookieParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _cookieNameLabel;
            private System.Windows.Forms.TextBox _cookieNameTextBox; 
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private CookieParameter _parameter;
 
            public CookieParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) {
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44); 

                _cookieNameLabel = new System.Windows.Forms.Label(); 
                _cookieNameTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox();
 
                _cookieNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _cookieNameLabel.Location = new System.Drawing.Point(0, 0); 
                _cookieNameLabel.Name = "CookieNameLabel"; 
                _cookieNameLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _cookieNameLabel.TabIndex = 10; 
                _cookieNameLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_CookieParameterEditor_CookieNameLabel);

                _cookieNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _cookieNameTextBox.Location = new System.Drawing.Point(0, 23); 
                _cookieNameTextBox.Name = "CookieNameTextBox";
                _cookieNameTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _cookieNameTextBox.TabIndex = 20; 
                _cookieNameTextBox.TextChanged += new System.EventHandler(this.OnCookieNameTextBoxTextChanged);
 
                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "CookieDefaultValueLabel";
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _defaultValueLabel.TabIndex = 30;
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68); 
                _defaultValueTextBox.Name = "CookieDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                Controls.Add(_cookieNameLabel); 
                Controls.Add(_cookieNameTextBox); 
                Controls.Add(_defaultValueLabel);
                Controls.Add(_defaultValueTextBox); 

                Dock = DockStyle.Fill;
                ResumeLayout();
            } 

            public override string EditorName { 
                get { 
                    return "Cookie";
                } 
            }

            public override bool HasCompleteInformation {
                get { 
                    return (_parameter.CookieName.Length > 0);
                } 
            } 

            public override Parameter Parameter { 
                get {
                    return _parameter;
                }
            } 

            public override void Initialize() { 
                _parameter = new CookieParameter(); 
                _cookieNameTextBox.Text = String.Empty;
                _defaultValueTextBox.Text = String.Empty; 
            }

            private void OnCookieNameTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.CookieName = _cookieNameTextBox.Text; 
                OnParameterChanged();
            } 
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.DefaultValue = _defaultValueTextBox.Text; 
            }
        }

        /// <devdoc> 
        /// Parameter editor for control parameters.
        /// </devdoc> 
        private sealed class ControlParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _controlIDLabel;
            private AutoSizeComboBox _controlIDComboBox; 
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private ControlParameter _parameter;
            private System.Web.UI.Control _control; 

            public ControlParameterEditor(IServiceProvider serviceProvider, System.Web.UI.Control control) : base(serviceProvider) { 
                _control = control; 

                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);

                _controlIDLabel = new System.Windows.Forms.Label();
                _controlIDComboBox = new AutoSizeComboBox(); 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
 
                _controlIDLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _controlIDLabel.Location = new System.Drawing.Point(0, 0); 
                _controlIDLabel.Name = "ControlIDLabel";
                _controlIDLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _controlIDLabel.TabIndex = 10;
                _controlIDLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ControlParameterEditor_ControlIDLabel); 

                _controlIDComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _controlIDComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
                _controlIDComboBox.Location = new System.Drawing.Point(0, 23);
                _controlIDComboBox.Name = "ControlIDComboBox"; 
                _controlIDComboBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _controlIDComboBox.Sorted = true;
                _controlIDComboBox.TabIndex = 20;
                _controlIDComboBox.SelectedIndexChanged += new System.EventHandler(this.OnControlIDComboBoxSelectedIndexChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48); 
                _defaultValueLabel.Name = "ControlDefaultValueLabel";
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _defaultValueLabel.TabIndex = 30;
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue);

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "ControlDefaultValueTextBox"; 
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _defaultValueTextBox.TabIndex = 40;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                Controls.Add(_controlIDLabel);
                Controls.Add(_controlIDComboBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
 
 
                // Populate control list
                if (ServiceProvider != null) { 
                    IDesignerHost designerHost = (IDesignerHost)ServiceProvider.GetService(typeof(IDesignerHost));
                    if (designerHost != null) {
                        ControlItem[] controlItems = ControlItem.GetControlItems(designerHost, _control);
                        foreach (ControlItem controlItem in controlItems) { 
                            _controlIDComboBox.Items.Add(controlItem);
                        } 
                        _controlIDComboBox.InvalidateDropDownWidth(); 
                    }
                } 

                Dock = DockStyle.Fill;
                ResumeLayout();
            } 

            public override string EditorName { 
                get { 
                    return "Control";
                } 
            }

            public override bool HasCompleteInformation {
                get { 
                    return (_controlIDComboBox.SelectedItem != null);
                } 
            } 

            public override Parameter Parameter { 
                get {
                    return _parameter;
                }
            } 

            public override void Initialize() { 
                _parameter = new ControlParameter(); 
                _controlIDComboBox.SelectedItem = null;
                _defaultValueTextBox.Text = String.Empty; 
            }

            private void OnControlIDComboBoxSelectedIndexChanged(object s, System.EventArgs e) {
                ControlItem controlItem = _controlIDComboBox.SelectedItem as ControlItem; 

                if (controlItem == null) { 
                    _parameter.ControlID = String.Empty; 
                    _parameter.PropertyName = String.Empty;
                } 
                else {
                    _parameter.ControlID = controlItem.ControlID;
                    _parameter.PropertyName = controlItem.PropertyName;
                } 

                OnParameterChanged(); 
            } 

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            }
        }
 
        /// <devdoc>
        /// Parameter editor for form parameters. 
        /// </devdoc> 
        private sealed class FormParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _formFieldLabel; 
            private System.Windows.Forms.TextBox _formFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private FormParameter _parameter; 

            public FormParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _formFieldLabel = new System.Windows.Forms.Label();
                _formFieldTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 

                _formFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _formFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _formFieldLabel.Name = "FormFieldLabel";
                _formFieldLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _formFieldLabel.TabIndex = 10;
                _formFieldLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_FormParameterEditor_FormFieldLabel);

                _formFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _formFieldTextBox.Location = new System.Drawing.Point(0, 23);
                _formFieldTextBox.Name = "FormFieldTextBox"; 
                _formFieldTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _formFieldTextBox.TabIndex = 20;
                _formFieldTextBox.TextChanged += new System.EventHandler(this.OnFormFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "FormDefaultValueLabel"; 
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "FormDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                Controls.Add(_formFieldLabel); 
                Controls.Add(_formFieldTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);

                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }
 
            public override string EditorName { 
                get {
                    return "Form"; 
                }
            }

            public override bool HasCompleteInformation { 
                get {
                    return (_parameter.FormField.Length > 0); 
                } 
            }
 
            public override Parameter Parameter {
                get {
                    return _parameter;
                } 
            }
 
            public override void Initialize() { 
                _parameter = new FormParameter();
                _formFieldTextBox.Text = String.Empty; 
                _defaultValueTextBox.Text = String.Empty;
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            } 
 
            private void OnFormFieldTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.FormField = _formFieldTextBox.Text; 
                OnParameterChanged();
            }
        }
 
        /// <devdoc>
        /// Parameter editor for session parameters. 
        /// </devdoc> 
        private sealed class SessionParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _sessionFieldLabel; 
            private System.Windows.Forms.TextBox _sessionFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private SessionParameter _parameter; 

            public SessionParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _sessionFieldLabel = new System.Windows.Forms.Label();
                _sessionFieldTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 

                _sessionFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _sessionFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _sessionFieldLabel.Name = "SessionFieldLabel";
                _sessionFieldLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _sessionFieldLabel.TabIndex = 10;
                _sessionFieldLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_SessionParameterEditor_SessionFieldLabel);

                _sessionFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _sessionFieldTextBox.Location = new System.Drawing.Point(0, 23);
                _sessionFieldTextBox.Name = "SessionFieldTextBox"; 
                _sessionFieldTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _sessionFieldTextBox.TabIndex = 20;
                _sessionFieldTextBox.TextChanged += new System.EventHandler(this.OnSessionFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "SessionDefaultValueLabel"; 
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "SessionDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                Controls.Add(_sessionFieldLabel); 
                Controls.Add(_sessionFieldTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);

                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }
 
            public override string EditorName { 
                get {
                    return "Session"; 
                }
            }

            public override bool HasCompleteInformation { 
                get {
                    return (_parameter.SessionField.Length > 0); 
                } 
            }
 
            public override Parameter Parameter {
                get {
                    return _parameter;
                } 
            }
 
            public override void Initialize() { 
                _parameter = new SessionParameter();
                _sessionFieldTextBox.Text = String.Empty; 
                _defaultValueTextBox.Text = String.Empty;
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            } 
 
            private void OnSessionFieldTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.SessionField = _sessionFieldTextBox.Text; 
                OnParameterChanged();
            }
        }
 
        /// <devdoc>
        /// Parameter editor for query string parameters. 
        /// </devdoc> 
        private sealed class QueryStringParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _queryStringFieldLabel; 
            private System.Windows.Forms.TextBox _queryStringFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private QueryStringParameter _parameter; 

            public QueryStringParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _queryStringFieldLabel = new System.Windows.Forms.Label();
                _queryStringFieldTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 

                _queryStringFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _queryStringFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _queryStringFieldLabel.Name = "QueryStringFieldLabel";
                _queryStringFieldLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _queryStringFieldLabel.TabIndex = 10;
                _queryStringFieldLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_QueryStringParameterEditor_QueryStringFieldLabel);

                _queryStringFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _queryStringFieldTextBox.Location = new System.Drawing.Point(0, 23);
                _queryStringFieldTextBox.Name = "QueryStringFieldTextBox"; 
                _queryStringFieldTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _queryStringFieldTextBox.TabIndex = 20;
                _queryStringFieldTextBox.TextChanged += new System.EventHandler(this.OnQueryStringFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "QueryStringDefaultValueLabel"; 
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "QueryStringDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                Controls.Add(_queryStringFieldLabel); 
                Controls.Add(_queryStringFieldTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);

                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }
 
            public override string EditorName { 
                get {
                    return "QueryString"; 
                }
            }

            public override bool HasCompleteInformation { 
                get {
                    return (_parameter.QueryStringField.Length > 0); 
                } 
            }
 
            public override Parameter Parameter {
                get {
                    return _parameter;
                } 
            }
 
            public override void Initialize() { 
                _parameter = new QueryStringParameter();
                _queryStringFieldTextBox.Text = String.Empty; 
                _defaultValueTextBox.Text = String.Empty;
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            } 
 
            private void OnQueryStringFieldTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.QueryStringField = _queryStringFieldTextBox.Text; 
                OnParameterChanged();
            }
        }
 
        /// <devdoc>
        /// Parameter editor for profile parameters. 
        /// </devdoc> 
        private sealed class ProfileParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _propertyNameLabel; 
            private System.Windows.Forms.TextBox _propertyNameTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private ProfileParameter _parameter; 

            public ProfileParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout(); 
                Size = new System.Drawing.Size(ControlWidth, 44);
 
                _propertyNameLabel = new System.Windows.Forms.Label();
                _propertyNameTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 

                _propertyNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _propertyNameLabel.Location = new System.Drawing.Point(0, 0); 
                _propertyNameLabel.Name = "ProfilePropertyNameLabel";
                _propertyNameLabel.Size = new System.Drawing.Size(ControlWidth, 16); 
                _propertyNameLabel.TabIndex = 10;
                _propertyNameLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ProfileParameterEditor_PropertyNameLabel);

                _propertyNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _propertyNameTextBox.Location = new System.Drawing.Point(0, 23);
                _propertyNameTextBox.Name = "ProfilePropertyNameTextBox"; 
                _propertyNameTextBox.Size = new System.Drawing.Size(ControlWidth, 20); 
                _propertyNameTextBox.TabIndex = 20;
                _propertyNameTextBox.TextChanged += new System.EventHandler(this.OnPropertyNameTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 48);
                _defaultValueLabel.Name = "ProfileDefaultValueLabel"; 
                _defaultValueLabel.Size = new System.Drawing.Size(ControlWidth, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.SqlDataSourceConfigureFilterForm_ParameterEditor_DefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 68);
                _defaultValueTextBox.Name = "ProfileDefaultValueTextBox";
                _defaultValueTextBox.Size = new System.Drawing.Size(ControlWidth, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                Controls.Add(_propertyNameLabel); 
                Controls.Add(_propertyNameTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);

                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }
 
            public override string EditorName { 
                get {
                    return "Profile"; 
                }
            }

            public override bool HasCompleteInformation { 
                get {
                    return (_parameter.PropertyName.Length > 0); 
                } 
            }
 
            public override Parameter Parameter {
                get {
                    return _parameter;
                } 
            }
 
            public override void Initialize() { 
                _parameter = new ProfileParameter();
                _propertyNameTextBox.Text = String.Empty; 
                _defaultValueTextBox.Text = String.Empty;
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                _parameter.DefaultValue = _defaultValueTextBox.Text;
            } 
 
            private void OnPropertyNameTextBoxTextChanged(object s, System.EventArgs e) {
                _parameter.PropertyName = _propertyNameTextBox.Text; 
                OnParameterChanged();
            }
        }
        #endregion 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
