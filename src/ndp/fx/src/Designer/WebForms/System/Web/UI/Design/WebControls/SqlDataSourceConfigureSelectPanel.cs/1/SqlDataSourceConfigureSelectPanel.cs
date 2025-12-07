//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConfigureSelectPanel.cs" company="Microsoft">
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
    using System.Globalization; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    using ConflictOptions = System.Web.UI.ConflictOptions;
 
    /// <devdoc>
    /// Wizard panel for building a select command for a SqlDataSource. 
    /// </devdoc> 
    internal class SqlDataSourceConfigureSelectPanel : WizardPanel {
        private const string CompareAllValuesFormatString = "original_{0}"; 
        private const string OverwriteChangesFormatString = "{0}";

        private System.Windows.Forms.Label _retrieveDataLabel;
        private System.Windows.Forms.RadioButton _tableRadioButton; 
        private System.Windows.Forms.RadioButton _customSqlRadioButton;
        private System.Windows.Forms.Button _advancedOptionsButton; 
        private System.Windows.Forms.TextBox _previewTextBox; 
        private System.Windows.Forms.Label _previewLabel;
        private System.Windows.Forms.Label _tableNameLabel; 
        private System.Windows.Forms.Button _addSortButton;
        private System.Windows.Forms.Button _addFilterButton;
        private System.Windows.Forms.CheckBox _selectDistinctCheckBox;
        private System.Windows.Forms.CheckedListBox _fieldsCheckedListBox; 
        private System.Windows.Forms.Label _fieldsLabel;
        private AutoSizeComboBox _tablesComboBox; 
        private System.Windows.Forms.TableLayoutPanel _columnsTableLayoutPanel; 
        private System.Windows.Forms.TableLayoutPanel _optionsTableLayoutPanel;
        private System.Windows.Forms.Panel _fieldChooserPanel; 

        private bool _requiresRefresh = true;
        private DesignerDataConnection _dataConnection;
        private SqlDataSourceDesigner _sqlDataSourceDesigner; 

        private TableItem _previousTable; 
        private bool _ignoreFieldCheckChanges; 
        private SqlDataSourceTableQuery _tableQuery;
 
        // Autogenerate mode:
        // 0 - don't generate any additional statements
        // 1 - generate statements, but do not use optimistic concurrency (e.g. check only primary key in update/delete)
        // 2 - generate statements, and use optimistic concurrency (e.g. check all fields in update/delete) 
        private int _generateMode = 0;
 
 

        /// <devdoc> 
        /// Creates a new SqlDataSourceConfigureSelectPanel.
        /// </devdoc>
        public SqlDataSourceConfigureSelectPanel(SqlDataSourceDesigner sqlDataSourceDesigner) {
            Debug.Assert(sqlDataSourceDesigner != null); 
            _sqlDataSourceDesigner = sqlDataSourceDesigner;
            InitializeComponent(); 
            InitializeUI(); 
        }
 
        /// <devdoc>
        /// Gets a format string for the old values in a pessimistic query. This checks
        /// that the format string contains valid a formatting expression. If it does not,
        /// a default format expression will be used. 
        /// </devdoc>
        private static string GetOldValuesFormatString(SqlDataSource sqlDataSource, bool adjustForOptimisticConcurrency) { 
            const string sampleString = "test"; 
            try {
                string formattedSampleString = String.Format(CultureInfo.InvariantCulture, sqlDataSource.OldValuesParameterFormatString, sampleString); 
                if (String.Equals(formattedSampleString, sqlDataSource.OldValuesParameterFormatString, StringComparison.Ordinal)) {
                    // The format string doesn't really format anything, e.g. the format string is "foo",
                    // so we return one of our pre-baked format strings.
                    return (adjustForOptimisticConcurrency ? CompareAllValuesFormatString : OverwriteChangesFormatString); 
                }
                else { 
                    // The format string is a valid format string (e.g. "prefix{0}" or even just "{0}") 
                    if (adjustForOptimisticConcurrency && String.Equals(sampleString, formattedSampleString)) {
                        // The format string was technically valid, but since we want to use 
                        // optimistic concurrency, and the current string is an identity transformation,
                        // we have to adjust it to actually be non-identity.
                        return CompareAllValuesFormatString;
                    } 
                    else {
                        // The format string is valid, and since we're not using optimistic concurrency, 
                        // we don't care what the exact format is. 
                        return sqlDataSource.OldValuesParameterFormatString;
                    } 
                }
            }
            catch {
                // The format string was horribly wrong and an exception was thrown, so we use 
                // a pre-baked format string.
                return (adjustForOptimisticConcurrency ? CompareAllValuesFormatString : OverwriteChangesFormatString); 
            } 
        }
 
        #region Designer generated code
        private void InitializeComponent() {
            this._retrieveDataLabel = new System.Windows.Forms.Label();
            this._tableRadioButton = new System.Windows.Forms.RadioButton(); 
            this._customSqlRadioButton = new System.Windows.Forms.RadioButton();
            this._advancedOptionsButton = new System.Windows.Forms.Button(); 
            this._previewTextBox = new System.Windows.Forms.TextBox(); 
            this._previewLabel = new System.Windows.Forms.Label();
            this._tableNameLabel = new System.Windows.Forms.Label(); 
            this._addSortButton = new System.Windows.Forms.Button();
            this._addFilterButton = new System.Windows.Forms.Button();
            this._selectDistinctCheckBox = new System.Windows.Forms.CheckBox();
            this._fieldsCheckedListBox = new System.Windows.Forms.CheckedListBox(); 
            this._fieldsLabel = new System.Windows.Forms.Label();
            this._tablesComboBox = new AutoSizeComboBox(); 
            this._columnsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this._optionsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this._fieldChooserPanel = new System.Windows.Forms.Panel(); 
            this._columnsTableLayoutPanel.SuspendLayout();
            this._optionsTableLayoutPanel.SuspendLayout();
            this._fieldChooserPanel.SuspendLayout();
            this.SuspendLayout(); 
            //
            // _retrieveDataLabel 
            // 
            this._retrieveDataLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._retrieveDataLabel.Location = new System.Drawing.Point(0, 0);
            this._retrieveDataLabel.Name = "_retrieveDataLabel";
            this._retrieveDataLabel.Size = new System.Drawing.Size(544, 16);
            this._retrieveDataLabel.TabIndex = 10; 
            //
            // _customSqlRadioButton 
            // 
            this._customSqlRadioButton.Location = new System.Drawing.Point(7, 19);
            this._customSqlRadioButton.Name = "_customSqlRadioButton"; 
            this._customSqlRadioButton.Size = new System.Drawing.Size(537, 18);
            this._customSqlRadioButton.TabIndex = 20;
            this._customSqlRadioButton.CheckedChanged += new System.EventHandler(this.OnCustomSqlRadioButtonCheckedChanged);
            // 
            // _tableRadioButton
            // 
            this._tableRadioButton.Location = new System.Drawing.Point(7, 38); 
            this._tableRadioButton.Name = "_tableRadioButton";
            this._tableRadioButton.Size = new System.Drawing.Size(537, 18); 
            this._tableRadioButton.TabIndex = 30;
            this._tableRadioButton.CheckedChanged += new System.EventHandler(this.OnTableRadioButtonCheckedChanged);

 
            //
            // _fieldChooserPanel 
            // 
            this._fieldChooserPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._fieldChooserPanel.Controls.Add(this._tableNameLabel);
            this._fieldChooserPanel.Controls.Add(this._tablesComboBox);
            this._fieldChooserPanel.Controls.Add(this._fieldsLabel); 
            this._fieldChooserPanel.Controls.Add(this._columnsTableLayoutPanel);
            this._fieldChooserPanel.Controls.Add(this._previewLabel); 
            this._fieldChooserPanel.Controls.Add(this._previewTextBox); 
            this._fieldChooserPanel.Location = new System.Drawing.Point(25, 58);
            this._fieldChooserPanel.Name = "_fieldChooserPanel"; 
            this._fieldChooserPanel.Size = new System.Drawing.Size(519, 216);
            this._fieldChooserPanel.TabIndex = 40;

            // 
            // _tableNameLabel
            // 
            this._tableNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._tableNameLabel.Location = new System.Drawing.Point(0, 0); 
            this._tableNameLabel.Name = "_tableNameLabel";
            this._tableNameLabel.Size = new System.Drawing.Size(519, 16);
            this._tableNameLabel.TabIndex = 10;
            // 
            // _tablesComboBox
            // 
            this._tablesComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._tablesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._tablesComboBox.Location = new System.Drawing.Point(0, 16);
            this._tablesComboBox.Name = "_tablesComboBox";
            this._tablesComboBox.Size = new System.Drawing.Size(263, 21);
            this._tablesComboBox.TabIndex = 20; 
            this._tablesComboBox.SelectedIndexChanged += new System.EventHandler(this.OnTablesComboBoxSelectedIndexChanged);
            // 
            // _fieldsLabel 
            //
            this._fieldsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._fieldsLabel.Location = new System.Drawing.Point(0, 42);
            this._fieldsLabel.Name = "_fieldsLabel";
            this._fieldsLabel.Size = new System.Drawing.Size(519, 16); 
            this._fieldsLabel.TabIndex = 30;
            // 
            // _columnsTableLayoutPanel 
            //
            this._columnsTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._columnsTableLayoutPanel.ColumnCount = 2;
            this._columnsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); 
            this._columnsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._columnsTableLayoutPanel.Controls.Add(this._optionsTableLayoutPanel, 0, 1); 
            this._columnsTableLayoutPanel.Controls.Add(this._fieldsCheckedListBox, 0, 0); 
            this._columnsTableLayoutPanel.Controls.Add(this._selectDistinctCheckBox, 1, 0);
            this._columnsTableLayoutPanel.Location = new System.Drawing.Point(0, 58); 
            this._columnsTableLayoutPanel.Name = "_columnsTableLayoutPanel";
            this._columnsTableLayoutPanel.RowCount = 2;
            this._columnsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._columnsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this._columnsTableLayoutPanel.Size = new System.Drawing.Size(519, 100);
            this._columnsTableLayoutPanel.TabIndex = 40; 
            // 
            // _previewLabel
            // 
            this._previewLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._previewLabel.Location = new System.Drawing.Point(0, 164);
            this._previewLabel.Name = "_previewLabel"; 
            this._previewLabel.Size = new System.Drawing.Size(519, 16);
            this._previewLabel.TabIndex = 50; 
            // 
            // _previewTextBox
            // 
            this._previewTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._previewTextBox.BackColor = System.Drawing.SystemColors.Control;
            this._previewTextBox.Location = new System.Drawing.Point(0, 180); 
            this._previewTextBox.Multiline = true;
            this._previewTextBox.Name = "_previewTextBox"; 
            this._previewTextBox.ReadOnly = true; 
            this._previewTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._previewTextBox.Size = new System.Drawing.Size(519, 36); 
            this._previewTextBox.TabIndex = 60;
            this._previewTextBox.Text = "";

 
            //
            // _fieldsCheckedListBox 
            // 
            this._fieldsCheckedListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._fieldsCheckedListBox.CheckOnClick = true;
            this._fieldsCheckedListBox.IntegralHeight = false;
            this._fieldsCheckedListBox.Location = new System.Drawing.Point(0, 0); 
            this._fieldsCheckedListBox.MultiColumn = true;
            this._fieldsCheckedListBox.Name = "_fieldsCheckedListBox"; 
            this._fieldsCheckedListBox.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this._columnsTableLayoutPanel.SetRowSpan(this._fieldsCheckedListBox, 2);
            this._fieldsCheckedListBox.Size = new System.Drawing.Size(388, 100); 
            this._fieldsCheckedListBox.TabIndex = 10;
            this._fieldsCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.OnFieldsCheckedListBoxItemCheck);
            //
            // _selectDistinctCheckBox 
            //
            this._selectDistinctCheckBox.AutoSize = true; 
            this._selectDistinctCheckBox.Location = new System.Drawing.Point(394, 2); 
            this._selectDistinctCheckBox.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this._selectDistinctCheckBox.Name = "_selectDistinctCheckBox"; 
            this._selectDistinctCheckBox.Size = new System.Drawing.Size(15, 14);
            this._selectDistinctCheckBox.TabIndex = 20;
            this._selectDistinctCheckBox.CheckedChanged += new System.EventHandler(this.OnSelectDistinctCheckBoxCheckedChanged);
            // 
            // _optionsTableLayoutPanel
            // 
            this._optionsTableLayoutPanel.AutoSize = true; 
            this._optionsTableLayoutPanel.ColumnCount = 1;
            this._optionsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
            this._optionsTableLayoutPanel.Controls.Add(this._addFilterButton, 0, 0);
            this._optionsTableLayoutPanel.Controls.Add(this._addSortButton, 0, 1);
            this._optionsTableLayoutPanel.Controls.Add(this._advancedOptionsButton, 0, 2);
            this._optionsTableLayoutPanel.Location = new System.Drawing.Point(394, 19); 
            this._optionsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this._optionsTableLayoutPanel.Name = "_optionsTableLayoutPanel"; 
            this._optionsTableLayoutPanel.RowCount = 3; 
            this._optionsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._optionsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this._optionsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._optionsTableLayoutPanel.Size = new System.Drawing.Size(115, 81);
            this._optionsTableLayoutPanel.TabIndex = 30;
 
            //
            // _addFilterButton 
            // 
            this._addFilterButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._addFilterButton.AutoSize = true; 
            this._addFilterButton.Location = new System.Drawing.Point(0, 2);
            this._addFilterButton.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this._addFilterButton.MinimumSize = new System.Drawing.Size(115, 23);
            this._addFilterButton.Name = "_addFilterButton"; 
            this._addFilterButton.Size = new System.Drawing.Size(115, 23);
            this._addFilterButton.TabIndex = 10; 
            this._addFilterButton.Click += new System.EventHandler(this.OnAddFilterButtonClick); 
            //
            // _addSortButton 
            //
            this._addSortButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._addSortButton.AutoSize = true;
            this._addSortButton.Location = new System.Drawing.Point(0, 29); 
            this._addSortButton.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this._addSortButton.MinimumSize = new System.Drawing.Size(115, 23); 
            this._addSortButton.Name = "_addSortButton"; 
            this._addSortButton.Size = new System.Drawing.Size(115, 23);
            this._addSortButton.TabIndex = 20; 
            this._addSortButton.Click += new System.EventHandler(this.OnAddSortButtonClick);
            //
            // _advancedOptionsButton
            // 
            this._advancedOptionsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._advancedOptionsButton.AutoSize = true; 
            this._advancedOptionsButton.Location = new System.Drawing.Point(0, 56); 
            this._advancedOptionsButton.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this._advancedOptionsButton.MinimumSize = new System.Drawing.Size(115, 23); 
            this._advancedOptionsButton.Name = "_advancedOptionsButton";
            this._advancedOptionsButton.Size = new System.Drawing.Size(115, 23);
            this._advancedOptionsButton.TabIndex = 30;
            this._advancedOptionsButton.Click += new System.EventHandler(this.OnAdvancedOptionsButtonClick); 

            // 
            // SqlDataSourceConfigureSelectPanel 
            //
            this.Controls.Add(this._fieldChooserPanel); 
            this.Controls.Add(this._customSqlRadioButton);
            this.Controls.Add(this._tableRadioButton);
            this.Controls.Add(this._retrieveDataLabel);
            this.Name = "SqlDataSourceConfigureSelectPanel"; 
            this.Size = new System.Drawing.Size(544, 274);
            this._columnsTableLayoutPanel.ResumeLayout(false); 
            this._columnsTableLayoutPanel.PerformLayout(); 
            this._optionsTableLayoutPanel.ResumeLayout(false);
            this._optionsTableLayoutPanel.PerformLayout(); 
            this._fieldChooserPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion 

        /// <devdoc> 
        /// Attempts to find a given column in a given table. 
        /// </devdoc>
        private DesignerDataColumn GetColumnFromTable(DesignerDataTableBase designerDataTable, string columnName) { 
            foreach (DesignerDataColumn designerDataColumn in designerDataTable.Columns) {
                if (designerDataColumn.Name == columnName) {
                    return designerDataColumn;
                } 
            }
            return null; 
        } 

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer.
        /// </devdoc>
        private void InitializeUI() { 
            Caption = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_PanelCaption);
 
            _retrieveDataLabel.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_RetrieveDataLabel); 
            _tableRadioButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_TableLabel);
            _customSqlRadioButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_CustomSqlLabel); 
            _previewLabel.Text = SR.GetString(SR.SqlDataSource_General_PreviewLabel);
            _tableNameLabel.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_TableNameLabel);
            _addSortButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_SortButton);
            _addFilterButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_FilterLabel); 
            _selectDistinctCheckBox.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_SelectDistinctLabel);
            _advancedOptionsButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_AdvancedOptions); 
            _fieldsLabel.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_FieldsLabel); 

            // Accessibility text 
            _tableRadioButton.AccessibleDescription = _retrieveDataLabel.Text;
            _tableRadioButton.AccessibleName = _tableRadioButton.Text;
            _customSqlRadioButton.AccessibleDescription = _retrieveDataLabel.Text;
            _customSqlRadioButton.AccessibleName = _customSqlRadioButton.Text; 

            UpdateFonts(); 
        } 

        /// <devdoc> 
        /// Attempts to load a serialized table query and populat the UI with
        /// the correct state. It will silently fail if the state is
        /// inconsistent with the metadata available.
        /// </devdoc> 
        private bool LoadTableQueryState(Hashtable tableQueryState) {
            Debug.Assert(tableQueryState != null); 
 
            SqlDataSource sqlDataSource = _sqlDataSourceDesigner.Component as SqlDataSource;
 
            //

            int connectionStringHash = (int)tableQueryState["Conn_ConnectionStringHash"];
            string providerName = (string)tableQueryState["Conn_ProviderName"]; 

            // Check if this is the correct connection for recreating the table query 
            if ((connectionStringHash != _dataConnection.ConnectionString.GetHashCode()) || 
                (providerName != _dataConnection.ProviderName)) {
                return false; 
            }

            // Generation info
            int generateMode = (int)tableQueryState["Generate_Mode"]; 

            // Try to find the table that was used 
            string tableName = (string)tableQueryState["Table_Name"]; 
            TableItem tableItem = null;
            foreach (TableItem item in _tablesComboBox.Items) { 
                if (item.DesignerDataTable.Name == tableName) {
                    tableItem = item;
                    break;
                } 
            }
            if (tableItem == null) { 
                return false; 
            }
            DesignerDataTableBase designerDataTable = tableItem.DesignerDataTable; 

            // Make sure that all the required fields are present
            int fieldCount = (int)tableQueryState["Fields_Count"];
            ArrayList fields = new ArrayList(); 
            for (int i = 0; i < fieldCount; i++) {
                string fieldName = (string)tableQueryState["Fields_FieldName" + i.ToString(CultureInfo.InvariantCulture)]; 
                DesignerDataColumn designerDataColumn = GetColumnFromTable(designerDataTable, fieldName); 
                if (designerDataColumn == null) {
                    return false; 
                }
                fields.Add(designerDataColumn);
            }
 
            // Extract asterisk field value
            bool asteriskField = (bool)tableQueryState["AsteriskField"]; 
 
            // Extract distinct value
            bool distinct = (bool)tableQueryState["Distinct"]; 


            // Create a list of unused parameters so that we can assign them to
            // the appropriate filter clause. 
            System.Collections.Generic.List<Parameter> unusedParameters = new System.Collections.Generic.List<Parameter>();
            foreach (ICloneable parameter in sqlDataSource.SelectParameters) { 
                unusedParameters.Add((Parameter)parameter.Clone()); 
            }
 
            DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
            bool supportsNamedParameters = SqlDataSourceDesigner.SupportsNamedParameters(factory);

            // Make sure that all the required fields in the filters are present 
            int filterCount = (int)tableQueryState["Filters_Count"];
            ArrayList filters = new ArrayList(); 
            for (int i = 0; i < filterCount; i++) { 
                string fieldName = (string)tableQueryState["Filters_FieldName" + i.ToString(CultureInfo.InvariantCulture)];
                string operatorFormat = (string)tableQueryState["Filters_OperatorFormat" + i.ToString(CultureInfo.InvariantCulture)]; 
                bool isBinary = (bool)tableQueryState["Filters_IsBinary" + i.ToString(CultureInfo.InvariantCulture)];
                string value = (string)tableQueryState["Filters_Value" + i.ToString(CultureInfo.InvariantCulture)];
                string parameterName = (string)tableQueryState["Filters_ParameterName" + i.ToString(CultureInfo.InvariantCulture)];
                DesignerDataColumn designerDataColumn = GetColumnFromTable(designerDataTable, fieldName); 
                if (designerDataColumn == null) {
                    return false; 
                } 

                // Figure out the parameter that is supposed to be used with this filter clause 
                Parameter parameter = null;
                if (parameterName != null) {
                    if (supportsNamedParameters) {
                        // Try to find the right named parameter 
                        foreach (Parameter p in unusedParameters) {
                            if (p.Name == parameterName) { 
                                parameter = p; 
                                break;
                            } 
                        }
                        if (parameter != null) {
                            // Found named parameter, remove it from list of unused parameters
                            unusedParameters.Remove(parameter); 
                        }
                        else { 
                            // Could not find named parameter, create a new one (sort of an error case) 
                            parameter = new Parameter(parameterName);
                        } 
                    }
                    else {
                        // Named parameters not supported, just get the next unused parameter
                        if (unusedParameters.Count > 0) { 
                            parameter = unusedParameters[0];
                            unusedParameters.RemoveAt(0); 
                        } 
                        else {
                            // No more parameters left, this is sort of an error case, but we allow it 
                            parameter = new Parameter(parameterName);
                        }
                    }
                } 

                filters.Add(new SqlDataSourceFilterClause(_dataConnection, designerDataTable, designerDataColumn, operatorFormat, isBinary, value, parameter)); 
            } 

            // Make sure that all the required fields in the orders are present 
            int orderCount = (int)tableQueryState["Orders_Count"];
            ArrayList orders = new ArrayList();
            for (int i = 0; i < orderCount; i++) {
                string fieldName = (string)tableQueryState["Orders_FieldName" + i.ToString(CultureInfo.InvariantCulture)]; 
                bool isDescending = (bool)tableQueryState["Orders_IsDescending" + i.ToString(CultureInfo.InvariantCulture)];
                DesignerDataColumn designerDataColumn = GetColumnFromTable(designerDataTable, fieldName); 
                if (designerDataColumn == null) { 
                    return false;
                } 
                orders.Add(new SqlDataSourceOrderClause(_dataConnection, designerDataTable, designerDataColumn, isDescending));
            }

 
            // Before we fully load the query, we have to check if the current
            // SELECT command is the same as the one that would be generated 
            // from this query. 
            SqlDataSourceTableQuery tempQuery = new SqlDataSourceTableQuery(_dataConnection, designerDataTable);
            foreach (DesignerDataColumn designerDataColumn in fields) { 
                tempQuery.Fields.Add(designerDataColumn);
            }
            tempQuery.AsteriskField = asteriskField;
            tempQuery.Distinct = distinct; 
            foreach (SqlDataSourceFilterClause filterClause in filters) {
                tempQuery.FilterClauses.Add(filterClause); 
            } 
            foreach (SqlDataSourceOrderClause orderClause in orders) {
                tempQuery.OrderClauses.Add(orderClause); 
            }

            bool includeOldValues = (generateMode == 2);
            string oldValuesFormatString = GetOldValuesFormatString(sqlDataSource, false); 
            SqlDataSourceQuery selectQuery = tempQuery.GetSelectQuery();
            SqlDataSourceQuery insertQuery = tempQuery.GetInsertQuery(); 
            SqlDataSourceQuery updateQuery = tempQuery.GetUpdateQuery(oldValuesFormatString, includeOldValues); 
            SqlDataSourceQuery deleteQuery = tempQuery.GetDeleteQuery(oldValuesFormatString, includeOldValues);
 
            // If and of the commands don't match, it must be a custom query
            if ((selectQuery != null) && (sqlDataSource.SelectCommand != selectQuery.Command)) {
                return false;
            } 
            if ((insertQuery != null) && (sqlDataSource.InsertCommand.Trim().Length > 0) && (sqlDataSource.InsertCommand != insertQuery.Command)) {
                return false; 
            } 
            if ((updateQuery != null) && (sqlDataSource.UpdateCommand.Trim().Length > 0) && (sqlDataSource.UpdateCommand != updateQuery.Command)) {
                return false; 
            }
            if ((deleteQuery != null) && (sqlDataSource.DeleteCommand.Trim().Length > 0) && (sqlDataSource.DeleteCommand != deleteQuery.Command)) {
                return false;
            } 

 
            // Everything looks in order, go ahead and set all the properties 
            _tableQuery = new SqlDataSourceTableQuery(_dataConnection, designerDataTable);
 

            _tablesComboBox.SelectedItem = tableItem;
            ArrayList fieldIndices = new ArrayList();
            foreach (DesignerDataColumn designerDataColumn in fields) { 
                foreach (ColumnItem columnItem in _fieldsCheckedListBox.Items) {
                    if (columnItem.DesignerDataColumn == designerDataColumn) { 
                        fieldIndices.Add(_fieldsCheckedListBox.Items.IndexOf(columnItem)); 
                    }
                } 
            }
            foreach (int fieldIndex in fieldIndices) {
                _fieldsCheckedListBox.SetItemChecked(fieldIndex, true);
            } 

            if (asteriskField) { 
                _fieldsCheckedListBox.SetItemChecked(0, true); 
            }
 
            _selectDistinctCheckBox.Checked = distinct;

            _generateMode = generateMode;
 
            foreach (SqlDataSourceFilterClause filterClause in filters) {
                _tableQuery.FilterClauses.Add(filterClause); 
            } 

            foreach (SqlDataSourceOrderClause orderClause in orders) { 
                _tableQuery.OrderClauses.Add(orderClause);
            }

            return true; 
        }
 
        /// <devdoc> 
        /// </devdoc>
        private void OnAddFilterButtonClick(object sender, System.EventArgs e) { 
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active");

            SqlDataSourceConfigureFilterForm form = new SqlDataSourceConfigureFilterForm(_sqlDataSourceDesigner, _tableQuery);
            DialogResult result = UIServiceHelper.ShowDialog(_sqlDataSourceDesigner.Component.Site, form); 
            if (result == DialogResult.OK) {
                // Copy new list of filter clauses 
                _tableQuery.FilterClauses.Clear(); 
                foreach (SqlDataSourceFilterClause clause in form.FilterClauses) {
                    _tableQuery.FilterClauses.Add(clause); 
                }
                UpdatePreview();
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        private void OnAddSortButtonClick(object sender, System.EventArgs e) {
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active"); 

            SqlDataSourceConfigureSortForm form = new SqlDataSourceConfigureSortForm(_sqlDataSourceDesigner, _tableQuery);
            DialogResult result = UIServiceHelper.ShowDialog(_sqlDataSourceDesigner.Component.Site, form);
            if (result == DialogResult.OK) { 
                // Copy new list of filter clauses
                _tableQuery.OrderClauses.Clear(); 
                foreach (SqlDataSourceOrderClause clause in form.OrderClauses) { 
                    _tableQuery.OrderClauses.Add(clause);
                } 
                UpdatePreview();
            }
        }
 
        private void OnAdvancedOptionsButtonClick(object sender, System.EventArgs e) {
            SqlDataSourceAdvancedOptionsForm form = new SqlDataSourceAdvancedOptionsForm(ServiceProvider); 
            form.SetAllowAutogenerate(_tableQuery.IsPrimaryKeySelected() && !_selectDistinctCheckBox.Checked); 
            form.GenerateStatements = (_generateMode > 0);
            form.OptimisticConcurrency = (_generateMode == 2); 
            DialogResult result = UIServiceHelper.ShowDialog(ServiceProvider, form);
            if (result == DialogResult.OK) {
                _generateMode = 0;
                if (form.GenerateStatements) { 
                    if (form.OptimisticConcurrency) {
                        _generateMode = 2; 
                    } 
                    else {
                        _generateMode = 1; 
                    }
                }
            }
        } 

        /// <devdoc> 
        /// Called when the user click Finish on the wizard. 
        /// </devdoc>
        protected internal override void OnComplete() { 
            // If the table/field picker was used, store the state of the picker for later re-entrancy
            if (_tableRadioButton.Checked) {
                _sqlDataSourceDesigner.TableQueryState = SaveTableQueryState();
 
                // Store optimistic/pessimistic settings
                SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component; 
                bool includeOldValues = (_generateMode == 2); 
                sqlDataSource.OldValuesParameterFormatString = GetOldValuesFormatString(sqlDataSource, includeOldValues);
                if (includeOldValues) { 
                    sqlDataSource.ConflictDetection = ConflictOptions.CompareAllValues;
                }
                else {
                    sqlDataSource.ConflictDetection = ConflictOptions.OverwriteChanges; 
                }
            } 
            else { 
                _sqlDataSourceDesigner.TableQueryState = null;
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        private void OnCustomSqlRadioButtonCheckedChanged(object sender, System.EventArgs e) {
            UpdateEnabledUI(); 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void OnFieldsCheckedListBoxItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e) {
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active");
 
            if (_ignoreFieldCheckChanges) {
                return; 
            } 

            UpdateEnabledUI(); 

            // If a check is about to be removed, and there is only one check, disable Next
            ParentWizard.NextButton.Enabled = !((e.NewValue == CheckState.Unchecked) && (_fieldsCheckedListBox.CheckedItems.Count == 1));
 
            Debug.Assert(_tableQuery != null);
 
            // If the item being checked is the asterisk field, select it and clear out all others 
            if ((e.Index == 0) && (e.NewValue == CheckState.Checked)) {
                _tableQuery.AsteriskField = true; // Automatically clears out internal field list 
                _ignoreFieldCheckChanges = true;
                for (int i = 1; i < _fieldsCheckedListBox.Items.Count; i++) {
                    _fieldsCheckedListBox.SetItemChecked(i, false);
                } 
                _ignoreFieldCheckChanges = false;
            } 
            else { 
                // Always un-set the asterisk field
                _tableQuery.AsteriskField = false; 

                _ignoreFieldCheckChanges = true;
                _fieldsCheckedListBox.SetItemChecked(0, false);
                if (e.Index > 0) { 
                    if (e.NewValue == CheckState.Checked) {
                        // Add the field to the field list 
                        _tableQuery.Fields.Add(((ColumnItem)_fieldsCheckedListBox.Items[e.Index]).DesignerDataColumn); 
                    }
                    else { 
                        // Remove the field from the field list
                        _tableQuery.Fields.Remove(((ColumnItem)_fieldsCheckedListBox.Items[e.Index]).DesignerDataColumn);
                    }
                } 
                _ignoreFieldCheckChanges = false;
            } 
 
            // If not all primary key fields have been selected or distinct is checked, turn off autogenerate
            if (!_tableQuery.IsPrimaryKeySelected() || _selectDistinctCheckBox.Checked) { 
                _generateMode = 0;
            }

            UpdatePreview(); 
        }
 
        protected override void OnFontChanged(EventArgs e) { 
            base.OnFontChanged(e);
            UpdateFonts(); 
        }

        /// <devdoc>
        /// </devdoc> 
        public override bool OnNext() {
            // Check what the previous panel was, and check how different the state is. 
            // If the state is too different, just new up a fresh panel. If it is the same, 
            // then just use the existing panel.
            if (_tableRadioButton.Checked) { 
                // User is using the Table/Field picker

                Debug.Assert(_tableQuery != null, "This code should not be called unless the table/field picker is active");
 
                // Get all the autogenerated queries for the commands
                SqlDataSourceQuery selectQuery = _tableQuery.GetSelectQuery(); 
                Debug.Assert(selectQuery != null, "Did not expect a null SelectQuery"); 
                if (selectQuery == null) {
                    selectQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]); 
                }

                SqlDataSourceQuery insertQuery;
                SqlDataSourceQuery updateQuery; 
                SqlDataSourceQuery deleteQuery;
 
                if (_generateMode > 0) { 
                    SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
                    bool includeOldValues = (_generateMode == 2); 
                    string oldValuesFormatString = GetOldValuesFormatString(sqlDataSource, includeOldValues);
                    insertQuery = _tableQuery.GetInsertQuery();
                    if (insertQuery == null) {
                        insertQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]); 
                    }
                    updateQuery = _tableQuery.GetUpdateQuery(oldValuesFormatString, includeOldValues); 
                    if (updateQuery == null) { 
                        updateQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    } 
                    deleteQuery = _tableQuery.GetDeleteQuery(oldValuesFormatString, includeOldValues);
                    if (deleteQuery == null) {
                        deleteQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    } 
                }
                else { 
                    // Just create empty queries 
                    insertQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    updateQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]); 
                    deleteQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                }

 
                // Go straight to summary panel - parameters have already been
                // configured in the WHERE clause builder 
                SqlDataSourceSummaryPanel nextPanel = NextPanel as SqlDataSourceSummaryPanel; 
                if (nextPanel == null) {
                    nextPanel = ((SqlDataSourceWizardForm)ParentWizard).GetSummaryPanel(); 
                    NextPanel = nextPanel;
                }
                nextPanel.SetQueries(_dataConnection,
                    selectQuery, 
                    insertQuery,
                    updateQuery, 
                    deleteQuery); 
                return true;
            } 
            else {
                // User want to enter a custom SQL command or stored procedure

                SqlDataSourceCustomCommandPanel nextPanel = NextPanel as SqlDataSourceCustomCommandPanel; 
                if (nextPanel == null) {
                    // Only create a new custom command panel if we did not previously have one 
                    nextPanel = ((SqlDataSourceWizardForm)ParentWizard).GetCustomCommandPanel(); 
                    NextPanel = nextPanel;
                } 

                // Clone the lists of parameters so we don't touch the originals
                SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
                ArrayList selectParameters = new ArrayList(); 
                ArrayList insertParameters = new ArrayList();
                ArrayList updateParameters = new ArrayList(); 
                ArrayList deleteParameters = new ArrayList(); 
                _sqlDataSourceDesigner.CopyList(sqlDataSource.SelectParameters, selectParameters);
                _sqlDataSourceDesigner.CopyList(sqlDataSource.InsertParameters, insertParameters); 
                _sqlDataSourceDesigner.CopyList(sqlDataSource.UpdateParameters, updateParameters);
                _sqlDataSourceDesigner.CopyList(sqlDataSource.DeleteParameters, deleteParameters);

                nextPanel.SetQueries( 
                    _dataConnection,
                    new SqlDataSourceQuery(sqlDataSource.SelectCommand, sqlDataSource.SelectCommandType, selectParameters), 
                    new SqlDataSourceQuery(sqlDataSource.InsertCommand, sqlDataSource.InsertCommandType, insertParameters), 
                    new SqlDataSourceQuery(sqlDataSource.UpdateCommand, sqlDataSource.UpdateCommandType, updateParameters),
                    new SqlDataSourceQuery(sqlDataSource.DeleteCommand, sqlDataSource.DeleteCommandType, deleteParameters)); 

                return true;
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        public override void OnPrevious() {
        } 

        /// <devdoc>
        /// </devdoc>
        private void OnSelectDistinctCheckBoxCheckedChanged(object sender, System.EventArgs e) { 
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active");
 
            _tableQuery.Distinct = _selectDistinctCheckBox.Checked; 

            // If not all primary key fields have been selected or distinct is checked, turn off autogenerate 
            if (!_tableQuery.IsPrimaryKeySelected() || _selectDistinctCheckBox.Checked) {
                _generateMode = 0;
            }
 
            UpdatePreview();
        } 
 
        /// <devdoc>
        /// </devdoc> 
        private void OnTableRadioButtonCheckedChanged(object sender, System.EventArgs e) {
            UpdateEnabledUI();
        }
 
        /// <devdoc>
        /// </devdoc> 
        private void OnTablesComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            // WinForms raises a SelectedIndexChanged event even if you re-select
            // the currently selected item, so we have to ignore that, except 
            // when we are trying to clear the selection (table == null).
            TableItem currentTable = _tablesComboBox.SelectedItem as TableItem;
            if ((currentTable != null) && (_previousTable == currentTable)) {
                return; 
            }
 
            Cursor originalCursor = Cursor.Current; 
            // If the table changed, reset all the selections and create new defaults
            _fieldsCheckedListBox.Items.Clear(); 
            _selectDistinctCheckBox.Checked = false;
            _generateMode = 0;
            try {
                Cursor.Current = Cursors.WaitCursor; 

                if (currentTable != null) { 
                    ICollection columns = currentTable.DesignerDataTable.Columns; 
                    _tableQuery = new SqlDataSourceTableQuery(_dataConnection, currentTable.DesignerDataTable);
                    // Add the asterisk field and all the columns 
                    _fieldsCheckedListBox.Items.Add(new ColumnItem());
                    foreach (DesignerDataColumn dataColumn in columns) {
                        _fieldsCheckedListBox.Items.Add(new ColumnItem(dataColumn));
                    } 
                }
                else { 
                    // No new table is selected, clear the old query 
                    _tableQuery = null;
                } 

                _previousTable = currentTable;
            }
            catch (Exception ex) { 
                UIServiceHelper.ShowError(
                    ServiceProvider, 
                    ex, 
                    SR.GetString(SR.SqlDataSourceConfigureSelectPanel_CouldNotGetTableSchema));
            } 
            finally {
                UpdateFieldsCheckedListBoxColumnWidth();
                UpdateEnabledUI();
                UpdatePreview(); 
                Cursor.Current = originalCursor;
            } 
        } 

        /// <devdoc> 
        /// </devdoc>
        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
 
            if (Visible) {
                ParentWizard.FinishButton.Enabled = false; 
 
                // If the data connection has changed, force a refresh of the database metadata
                DesignerDataConnection newConnection = ((SqlDataSourceWizardForm)ParentWizard).DesignerDataConnection; 
                if (!SqlDataSourceDesigner.ConnectionsEqual(_dataConnection, newConnection)) {
                    _dataConnection = newConnection;
                    _requiresRefresh = true;
                } 

                if (_requiresRefresh) { 
                    Debug.Assert(_dataConnection != null); 

                    Cursor originalCursor = Cursor.Current; 
                    try {
                        Cursor.Current = Cursors.WaitCursor;

                        // Reset UI 
                        _tablesComboBox.SelectedIndex = -1;
                        _tablesComboBox.Items.Clear(); 
 
                        // Try to get schema for the connection
                        IDataEnvironment dataEnvironment = ((SqlDataSourceWizardForm)ParentWizard).DataEnvironment; 
                        IDesignerDataSchema designerDataSchema = null;
                        if (_dataConnection != null) {
                            designerDataSchema = dataEnvironment.GetConnectionSchema(_dataConnection);
                        } 

                        // Try to get list of tables and views 
                        if (designerDataSchema != null) { 
                            // Populate tables list
                            System.Collections.Generic.List<TableItem> tableItems = new System.Collections.Generic.List<TableItem>(); 
                            if (designerDataSchema.SupportsSchemaClass(DesignerDataSchemaClass.Tables)) {
                                ICollection tableCollection = designerDataSchema.GetSchemaItems(DesignerDataSchemaClass.Tables);
                                if (tableCollection != null) {
                                    foreach (DesignerDataTable dataTable in tableCollection) { 
                                        // Hide special ASP.net tables used for cache invalidation
                                        if (!dataTable.Name.ToLowerInvariant().StartsWith(SqlDataSourceDesigner.AspNetDatabaseObjectPrefix.ToLowerInvariant(), StringComparison.Ordinal)) { 
                                            tableItems.Add(new TableItem(dataTable)); 
                                        }
                                    } 
                                }
                            }

                            if (designerDataSchema.SupportsSchemaClass(DesignerDataSchemaClass.Views)) { 
                                ICollection viewCollection = designerDataSchema.GetSchemaItems(DesignerDataSchemaClass.Views);
                                if (viewCollection != null) { 
                                    foreach (DesignerDataView dataView in viewCollection) { 
                                        tableItems.Add(new TableItem(dataView));
                                    } 
                                }
                            }

                            tableItems.Sort(new System.Comparison<TableItem>( 
                                delegate(TableItem a, TableItem b) {
                                    return String.Compare(a.DesignerDataTable.Name, b.DesignerDataTable.Name, StringComparison.InvariantCultureIgnoreCase); 
                                })); 
                            _tablesComboBox.Items.AddRange(tableItems.ToArray());
                            _tablesComboBox.InvalidateDropDownWidth(); 
                        }

                        // Only enable table/field picker if there is at least one table
                        if (_tablesComboBox.Items.Count > 0) { 

                            // Try to perform "smart" re-entry 
                            Hashtable tableQueryState = _sqlDataSourceDesigner.TableQueryState; 
                            bool successfullReentry = false;
                            if (tableQueryState != null) { 
                                successfullReentry = LoadTableQueryState(tableQueryState);
                            }
                            if (!successfullReentry) {
                                // Initial attempt to reenter failed, try to parse command 
                                successfullReentry = LoadParsedSqlState();
                            } 
 
                            if (!successfullReentry) {
                                // If smart re-entry failed, potentially select Custom SQL automatically 

                                // Auto-select first table
                                _tablesComboBox.SelectedIndex = 0;
 
                                SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
                                bool hasCustomSql = ((sqlDataSource.SelectCommand.Trim().Length > 0) || 
                                                    (sqlDataSource.InsertCommand.Trim().Length > 0) || 
                                                    (sqlDataSource.UpdateCommand.Trim().Length > 0) ||
                                                    (sqlDataSource.DeleteCommand.Trim().Length > 0)); 

                                // Use table picker by default only if there is no custom command set
                                _tableRadioButton.Checked = !hasCustomSql;
                                _customSqlRadioButton.Checked = hasCustomSql; 
                            }
                            else { 
                                // Re-entry was successfull, maintain state 
                                _tableRadioButton.Checked = true;
                                _customSqlRadioButton.Checked = false; 
                            }

                            _tableRadioButton.Enabled = true;
                        } 
                        else {
                            // If there is no schema, force the user to use Custom SQL, and disable other options 
                            _customSqlRadioButton.Checked = true; 

                            _tableRadioButton.Enabled = false; 
                        }

                        UpdatePreview();
                    } 
                    finally {
                        Cursor.Current = originalCursor; 
                    } 

                    _requiresRefresh = false; 
                }

                UpdateEnabledUI();
            } 
        }
 
        private bool LoadParsedSqlState() { 
            SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
 
            // Only a SELECT command is specified, go ahead and try to parse it
            string[] commandParts = SqlDataSourceCommandParser.ParseSqlString(_sqlDataSourceDesigner.SelectCommand);
            if (commandParts == null) {
                return false; 
            }
 
            Debug.Assert(commandParts.Length >= 2, "SQL Parser: Expected at least two parts in the command (1 field + 1 table)"); 

 
            bool hasAsterisk = false;
            string tableName = SqlDataSourceCommandParser.GetLastIdentifierPart(commandParts[commandParts.Length - 1]);
            if (tableName == null) {
                // Problem parsing table name, abort 
                return false;
            } 
            System.Collections.Generic.List<string> fieldNames = new System.Collections.Generic.List<string>(); 
            for (int i = 0; i < commandParts.Length - 1; i++) {
                string fieldName = SqlDataSourceCommandParser.GetLastIdentifierPart(commandParts[i]); 
                if (fieldName == null) {
                    // Problem parsing field name, abort
                    return false;
                } 
                if (fieldName == "*") {
                    hasAsterisk = true; 
                } 
                else {
                    if (fieldName.Length == 0) { 
                        return false;
                    }
                    fieldNames.Add(fieldName);
                } 
            }
 
            // We only support the asterisk field if it is the only field 
            if (hasAsterisk && (fieldNames.Count != 0)) {
                return false; 
            }


            // Try to find the table that was used 
            TableItem tableItem = null;
            foreach (TableItem item in _tablesComboBox.Items) { 
                if (item.DesignerDataTable.Name == tableName) { 
                    tableItem = item;
                    break; 
                }
            }
            if (tableItem == null) {
                return false; 
            }
            DesignerDataTableBase designerDataTable = tableItem.DesignerDataTable; 
 
            // Make sure that all the required fields are present
            System.Collections.Generic.List<DesignerDataColumn> fields = new System.Collections.Generic.List<DesignerDataColumn>(); 
            foreach (string fieldName in fieldNames) {
                DesignerDataColumn designerDataColumn = GetColumnFromTable(designerDataTable, fieldName);
                if (designerDataColumn == null) {
                    return false; 
                }
                fields.Add(designerDataColumn); 
            } 

 
            // NOTE: We do not verify that the new generated SELECT statement
            // matches the existing SELECT statement because more than likely
            // they will be different.
 

            // If any of the three modification commands are set, we need 
            // to make sure they are the same as the ones we would autogenerate 
            // anyway. If not, we can't reload the wizard's query state since
            // it would involve data loss for those commands. 
            bool generateOtherCommands = (sqlDataSource.DeleteCommand.Trim().Length > 0) ||
                                         (sqlDataSource.InsertCommand.Trim().Length > 0) ||
                                         (sqlDataSource.UpdateCommand.Trim().Length > 0);
            if (generateOtherCommands) { 
                // Create a temporary query so we can generate the delete/insert/update commands
                SqlDataSourceTableQuery tempQuery = new SqlDataSourceTableQuery(_dataConnection, designerDataTable); 
                foreach (DesignerDataColumn designerDataColumn in fields) { 
                    tempQuery.Fields.Add(designerDataColumn);
                } 
                tempQuery.AsteriskField = hasAsterisk;

                SqlDataSourceQuery insertQuery = tempQuery.GetInsertQuery();
                string oldValuesFormatString = GetOldValuesFormatString(sqlDataSource, false); 
                SqlDataSourceQuery updateQuery = tempQuery.GetUpdateQuery(oldValuesFormatString, false);
                SqlDataSourceQuery deleteQuery = tempQuery.GetDeleteQuery(oldValuesFormatString, false); 
 
                if ((insertQuery != null) && (sqlDataSource.InsertCommand.Trim().Length > 0) && (sqlDataSource.InsertCommand != insertQuery.Command)) {
                    return false; 
                }
                if ((updateQuery != null) && (sqlDataSource.UpdateCommand.Trim().Length > 0) && (sqlDataSource.UpdateCommand != updateQuery.Command)) {
                    return false;
                } 
                if ((deleteQuery != null) && (sqlDataSource.DeleteCommand.Trim().Length > 0) && (sqlDataSource.DeleteCommand != deleteQuery.Command)) {
                    return false; 
                } 
            }
 
            // Everything looks in order, go ahead and set all the properties
            _tableQuery = new SqlDataSourceTableQuery(_dataConnection, designerDataTable);

 
            _tablesComboBox.SelectedItem = tableItem;
            ArrayList fieldIndices = new ArrayList(); 
            foreach (DesignerDataColumn designerDataColumn in fields) { 
                foreach (ColumnItem columnItem in _fieldsCheckedListBox.Items) {
                    if (columnItem.DesignerDataColumn == designerDataColumn) { 
                        fieldIndices.Add(_fieldsCheckedListBox.Items.IndexOf(columnItem));
                    }
                }
            } 
            foreach (int fieldIndex in fieldIndices) {
                _fieldsCheckedListBox.SetItemChecked(fieldIndex, true); 
            } 

            if (hasAsterisk) { 
                _fieldsCheckedListBox.SetItemChecked(0, true);
            }

            // Set the autogenerate mode based on our findings (see note at top of file about the mode values) 
            // This has to be set last since other actions above might have reset the value
            _generateMode = (generateOtherCommands ? 1 : 0); 
 
            return true;
        } 

        public void ResetUI() {
            _tableRadioButton.Checked = true;
            _customSqlRadioButton.Checked = false; 
            _generateMode = 0;
            _tablesComboBox.Items.Clear(); 
            _fieldsCheckedListBox.Items.Clear(); 
            _requiresRefresh = true;
        } 

        /// <devdoc>
        /// Serializes the entire state of a table query into a hashtable.
        /// </devdoc> 
        private Hashtable SaveTableQueryState() {
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active"); 
 
            Hashtable tableQueryState = new Hashtable();
 
            // Connection information
            tableQueryState.Add("Conn_ConnectionStringHash", _tableQuery.DesignerDataConnection.ConnectionString.GetHashCode());
            tableQueryState.Add("Conn_ProviderName", _tableQuery.DesignerDataConnection.ProviderName);
 
            // Generation info
            tableQueryState.Add("Generate_Mode", _generateMode); 
 
            // Table (name)
            tableQueryState.Add("Table_Name", _tableQuery.DesignerDataTable.Name); 

            // Fields
            tableQueryState.Add("Fields_Count", _tableQuery.Fields.Count);
            for (int i = 0; i < _tableQuery.Fields.Count; i++) { 
                tableQueryState.Add("Fields_FieldName" + i.ToString(CultureInfo.InvariantCulture), ((DesignerDataColumn)_tableQuery.Fields[i]).Name);
            } 
 
            // Asterisk
            tableQueryState.Add("AsteriskField", _tableQuery.AsteriskField); 

            // Distinct
            tableQueryState.Add("Distinct", _tableQuery.Distinct);
 
            // Filters (field + operator + binary + value)
            tableQueryState.Add("Filters_Count", _tableQuery.FilterClauses.Count); 
            for (int i = 0; i < _tableQuery.FilterClauses.Count; i++) { 
                SqlDataSourceFilterClause filterClause = _tableQuery.FilterClauses[i] as SqlDataSourceFilterClause;
                string s = i.ToString(CultureInfo.InvariantCulture); 
                tableQueryState.Add("Filters_FieldName" + s, filterClause.DesignerDataColumn.Name);
                tableQueryState.Add("Filters_OperatorFormat" + s, filterClause.OperatorFormat);
                tableQueryState.Add("Filters_IsBinary" + s, filterClause.IsBinary);
                tableQueryState.Add("Filters_Value" + s, filterClause.Value); 
                tableQueryState.Add("Filters_ParameterName" + s, (filterClause.Parameter != null ? filterClause.Parameter.Name : null));
            } 
 
            // Orders (column + isdescending)
            tableQueryState.Add("Orders_Count", _tableQuery.OrderClauses.Count); 
            for (int i = 0; i < _tableQuery.OrderClauses.Count; i++) {
                tableQueryState.Add("Orders_FieldName" + i.ToString(CultureInfo.InvariantCulture), ((SqlDataSourceOrderClause)_tableQuery.OrderClauses[i]).DesignerDataColumn.Name);
                tableQueryState.Add("Orders_IsDescending" + i.ToString(CultureInfo.InvariantCulture), ((SqlDataSourceOrderClause)_tableQuery.OrderClauses[i]).IsDescending);
            } 

            return tableQueryState; 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void UpdateEnabledUI() {
            _fieldChooserPanel.Enabled = _tableRadioButton.Checked;
 
            if (_customSqlRadioButton.Checked) {
                ParentWizard.NextButton.Enabled = true; 
            } 

            if (_tableRadioButton.Checked) { 
                // If there are no columns checked, disable Next
                ParentWizard.NextButton.Enabled = (_tablesComboBox.Items.Count > 0) && (_fieldsCheckedListBox.CheckedItems.Count > 0);

                // Enable/disable UI in the event that there are no fields for this 
                // table (this happens when there was an error retrieving schema)
                bool columnsAvailable = (_fieldsCheckedListBox.Items.Count > 0); 
                _fieldsLabel.Enabled = columnsAvailable; 
                _fieldsCheckedListBox.Enabled = columnsAvailable;
                _selectDistinctCheckBox.Enabled = columnsAvailable; 
                _addFilterButton.Enabled = columnsAvailable;
                _addSortButton.Enabled = columnsAvailable;
                _advancedOptionsButton.Enabled = columnsAvailable;
                _previewLabel.Enabled = columnsAvailable; 
                _previewTextBox.Enabled = columnsAvailable;
            } 
        } 

        private void UpdateFieldsCheckedListBoxColumnWidth() { 
            // Find the size of the widest column name
            int maxWidth = 0;
            using (Graphics g = _fieldsCheckedListBox.CreateGraphics()) {
                foreach (ColumnItem item in _fieldsCheckedListBox.Items) { 
                    string text = item.ToString();
                    maxWidth = Math.Max(maxWidth, (int)g.MeasureString(text, _fieldsCheckedListBox.Font).Width); 
                } 
            }
 
            // Add room for the checkbox
            maxWidth += 50;

            // Restrict column width to half the width of the control 
            maxWidth = Math.Min(maxWidth, _fieldsCheckedListBox.Width / 2);
 
            _fieldsCheckedListBox.ColumnWidth = maxWidth; 
        }
 
        private void UpdateFonts() {
            Font boldFont = new Font(Font, FontStyle.Bold);
            _retrieveDataLabel.Font = boldFont;
        } 

        /// <devdoc> 
        /// Updates the SQL preview textbox with the current query. 
        /// </devdoc>
        private void UpdatePreview() { 
            if (_tableQuery != null) {
                SqlDataSourceQuery selectQuery = _tableQuery.GetSelectQuery();
                _previewTextBox.Text = (selectQuery == null ? String.Empty : selectQuery.Command);
            } 
            else {
                _previewTextBox.Text = String.Empty; 
            } 
        }
 

        /// <devdoc>
        /// Represents a column a user can select.
        /// </devdoc> 
        private sealed class ColumnItem {
            private DesignerDataColumn _designerDataColumn; 
 
            public ColumnItem() {
                // Asterisk field 
            }

            public ColumnItem(DesignerDataColumn designerDataColumn) {
                // Column field 
                Debug.Assert(designerDataColumn != null);
                _designerDataColumn = designerDataColumn; 
            } 

            public DesignerDataColumn DesignerDataColumn { 
                get {
                    return _designerDataColumn;
                }
            } 

            public override string ToString() { 
                if (_designerDataColumn != null) { 
                    return _designerDataColumn.Name;
                } 
                else {
                    return "*";
                }
            } 
        }
 
 
        /// <devdoc>
        /// Represents a table a user can select fields from. 
        /// </devdoc>
        private sealed class TableItem {
            private DesignerDataTableBase _designerDataTable;
 
            public TableItem(DesignerDataTableBase designerDataTable) {
                Debug.Assert(designerDataTable != null); 
                _designerDataTable = designerDataTable; 
            }
 
            public DesignerDataTableBase DesignerDataTable {
                get {
                    return _designerDataTable;
                } 
            }
 
            public override string ToString() { 
                return _designerDataTable.Name;
            } 
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

    using ConflictOptions = System.Web.UI.ConflictOptions;
 
    /// <devdoc>
    /// Wizard panel for building a select command for a SqlDataSource. 
    /// </devdoc> 
    internal class SqlDataSourceConfigureSelectPanel : WizardPanel {
        private const string CompareAllValuesFormatString = "original_{0}"; 
        private const string OverwriteChangesFormatString = "{0}";

        private System.Windows.Forms.Label _retrieveDataLabel;
        private System.Windows.Forms.RadioButton _tableRadioButton; 
        private System.Windows.Forms.RadioButton _customSqlRadioButton;
        private System.Windows.Forms.Button _advancedOptionsButton; 
        private System.Windows.Forms.TextBox _previewTextBox; 
        private System.Windows.Forms.Label _previewLabel;
        private System.Windows.Forms.Label _tableNameLabel; 
        private System.Windows.Forms.Button _addSortButton;
        private System.Windows.Forms.Button _addFilterButton;
        private System.Windows.Forms.CheckBox _selectDistinctCheckBox;
        private System.Windows.Forms.CheckedListBox _fieldsCheckedListBox; 
        private System.Windows.Forms.Label _fieldsLabel;
        private AutoSizeComboBox _tablesComboBox; 
        private System.Windows.Forms.TableLayoutPanel _columnsTableLayoutPanel; 
        private System.Windows.Forms.TableLayoutPanel _optionsTableLayoutPanel;
        private System.Windows.Forms.Panel _fieldChooserPanel; 

        private bool _requiresRefresh = true;
        private DesignerDataConnection _dataConnection;
        private SqlDataSourceDesigner _sqlDataSourceDesigner; 

        private TableItem _previousTable; 
        private bool _ignoreFieldCheckChanges; 
        private SqlDataSourceTableQuery _tableQuery;
 
        // Autogenerate mode:
        // 0 - don't generate any additional statements
        // 1 - generate statements, but do not use optimistic concurrency (e.g. check only primary key in update/delete)
        // 2 - generate statements, and use optimistic concurrency (e.g. check all fields in update/delete) 
        private int _generateMode = 0;
 
 

        /// <devdoc> 
        /// Creates a new SqlDataSourceConfigureSelectPanel.
        /// </devdoc>
        public SqlDataSourceConfigureSelectPanel(SqlDataSourceDesigner sqlDataSourceDesigner) {
            Debug.Assert(sqlDataSourceDesigner != null); 
            _sqlDataSourceDesigner = sqlDataSourceDesigner;
            InitializeComponent(); 
            InitializeUI(); 
        }
 
        /// <devdoc>
        /// Gets a format string for the old values in a pessimistic query. This checks
        /// that the format string contains valid a formatting expression. If it does not,
        /// a default format expression will be used. 
        /// </devdoc>
        private static string GetOldValuesFormatString(SqlDataSource sqlDataSource, bool adjustForOptimisticConcurrency) { 
            const string sampleString = "test"; 
            try {
                string formattedSampleString = String.Format(CultureInfo.InvariantCulture, sqlDataSource.OldValuesParameterFormatString, sampleString); 
                if (String.Equals(formattedSampleString, sqlDataSource.OldValuesParameterFormatString, StringComparison.Ordinal)) {
                    // The format string doesn't really format anything, e.g. the format string is "foo",
                    // so we return one of our pre-baked format strings.
                    return (adjustForOptimisticConcurrency ? CompareAllValuesFormatString : OverwriteChangesFormatString); 
                }
                else { 
                    // The format string is a valid format string (e.g. "prefix{0}" or even just "{0}") 
                    if (adjustForOptimisticConcurrency && String.Equals(sampleString, formattedSampleString)) {
                        // The format string was technically valid, but since we want to use 
                        // optimistic concurrency, and the current string is an identity transformation,
                        // we have to adjust it to actually be non-identity.
                        return CompareAllValuesFormatString;
                    } 
                    else {
                        // The format string is valid, and since we're not using optimistic concurrency, 
                        // we don't care what the exact format is. 
                        return sqlDataSource.OldValuesParameterFormatString;
                    } 
                }
            }
            catch {
                // The format string was horribly wrong and an exception was thrown, so we use 
                // a pre-baked format string.
                return (adjustForOptimisticConcurrency ? CompareAllValuesFormatString : OverwriteChangesFormatString); 
            } 
        }
 
        #region Designer generated code
        private void InitializeComponent() {
            this._retrieveDataLabel = new System.Windows.Forms.Label();
            this._tableRadioButton = new System.Windows.Forms.RadioButton(); 
            this._customSqlRadioButton = new System.Windows.Forms.RadioButton();
            this._advancedOptionsButton = new System.Windows.Forms.Button(); 
            this._previewTextBox = new System.Windows.Forms.TextBox(); 
            this._previewLabel = new System.Windows.Forms.Label();
            this._tableNameLabel = new System.Windows.Forms.Label(); 
            this._addSortButton = new System.Windows.Forms.Button();
            this._addFilterButton = new System.Windows.Forms.Button();
            this._selectDistinctCheckBox = new System.Windows.Forms.CheckBox();
            this._fieldsCheckedListBox = new System.Windows.Forms.CheckedListBox(); 
            this._fieldsLabel = new System.Windows.Forms.Label();
            this._tablesComboBox = new AutoSizeComboBox(); 
            this._columnsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this._optionsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this._fieldChooserPanel = new System.Windows.Forms.Panel(); 
            this._columnsTableLayoutPanel.SuspendLayout();
            this._optionsTableLayoutPanel.SuspendLayout();
            this._fieldChooserPanel.SuspendLayout();
            this.SuspendLayout(); 
            //
            // _retrieveDataLabel 
            // 
            this._retrieveDataLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._retrieveDataLabel.Location = new System.Drawing.Point(0, 0);
            this._retrieveDataLabel.Name = "_retrieveDataLabel";
            this._retrieveDataLabel.Size = new System.Drawing.Size(544, 16);
            this._retrieveDataLabel.TabIndex = 10; 
            //
            // _customSqlRadioButton 
            // 
            this._customSqlRadioButton.Location = new System.Drawing.Point(7, 19);
            this._customSqlRadioButton.Name = "_customSqlRadioButton"; 
            this._customSqlRadioButton.Size = new System.Drawing.Size(537, 18);
            this._customSqlRadioButton.TabIndex = 20;
            this._customSqlRadioButton.CheckedChanged += new System.EventHandler(this.OnCustomSqlRadioButtonCheckedChanged);
            // 
            // _tableRadioButton
            // 
            this._tableRadioButton.Location = new System.Drawing.Point(7, 38); 
            this._tableRadioButton.Name = "_tableRadioButton";
            this._tableRadioButton.Size = new System.Drawing.Size(537, 18); 
            this._tableRadioButton.TabIndex = 30;
            this._tableRadioButton.CheckedChanged += new System.EventHandler(this.OnTableRadioButtonCheckedChanged);

 
            //
            // _fieldChooserPanel 
            // 
            this._fieldChooserPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._fieldChooserPanel.Controls.Add(this._tableNameLabel);
            this._fieldChooserPanel.Controls.Add(this._tablesComboBox);
            this._fieldChooserPanel.Controls.Add(this._fieldsLabel); 
            this._fieldChooserPanel.Controls.Add(this._columnsTableLayoutPanel);
            this._fieldChooserPanel.Controls.Add(this._previewLabel); 
            this._fieldChooserPanel.Controls.Add(this._previewTextBox); 
            this._fieldChooserPanel.Location = new System.Drawing.Point(25, 58);
            this._fieldChooserPanel.Name = "_fieldChooserPanel"; 
            this._fieldChooserPanel.Size = new System.Drawing.Size(519, 216);
            this._fieldChooserPanel.TabIndex = 40;

            // 
            // _tableNameLabel
            // 
            this._tableNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._tableNameLabel.Location = new System.Drawing.Point(0, 0); 
            this._tableNameLabel.Name = "_tableNameLabel";
            this._tableNameLabel.Size = new System.Drawing.Size(519, 16);
            this._tableNameLabel.TabIndex = 10;
            // 
            // _tablesComboBox
            // 
            this._tablesComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._tablesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._tablesComboBox.Location = new System.Drawing.Point(0, 16);
            this._tablesComboBox.Name = "_tablesComboBox";
            this._tablesComboBox.Size = new System.Drawing.Size(263, 21);
            this._tablesComboBox.TabIndex = 20; 
            this._tablesComboBox.SelectedIndexChanged += new System.EventHandler(this.OnTablesComboBoxSelectedIndexChanged);
            // 
            // _fieldsLabel 
            //
            this._fieldsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._fieldsLabel.Location = new System.Drawing.Point(0, 42);
            this._fieldsLabel.Name = "_fieldsLabel";
            this._fieldsLabel.Size = new System.Drawing.Size(519, 16); 
            this._fieldsLabel.TabIndex = 30;
            // 
            // _columnsTableLayoutPanel 
            //
            this._columnsTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._columnsTableLayoutPanel.ColumnCount = 2;
            this._columnsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); 
            this._columnsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._columnsTableLayoutPanel.Controls.Add(this._optionsTableLayoutPanel, 0, 1); 
            this._columnsTableLayoutPanel.Controls.Add(this._fieldsCheckedListBox, 0, 0); 
            this._columnsTableLayoutPanel.Controls.Add(this._selectDistinctCheckBox, 1, 0);
            this._columnsTableLayoutPanel.Location = new System.Drawing.Point(0, 58); 
            this._columnsTableLayoutPanel.Name = "_columnsTableLayoutPanel";
            this._columnsTableLayoutPanel.RowCount = 2;
            this._columnsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._columnsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this._columnsTableLayoutPanel.Size = new System.Drawing.Size(519, 100);
            this._columnsTableLayoutPanel.TabIndex = 40; 
            // 
            // _previewLabel
            // 
            this._previewLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._previewLabel.Location = new System.Drawing.Point(0, 164);
            this._previewLabel.Name = "_previewLabel"; 
            this._previewLabel.Size = new System.Drawing.Size(519, 16);
            this._previewLabel.TabIndex = 50; 
            // 
            // _previewTextBox
            // 
            this._previewTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._previewTextBox.BackColor = System.Drawing.SystemColors.Control;
            this._previewTextBox.Location = new System.Drawing.Point(0, 180); 
            this._previewTextBox.Multiline = true;
            this._previewTextBox.Name = "_previewTextBox"; 
            this._previewTextBox.ReadOnly = true; 
            this._previewTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._previewTextBox.Size = new System.Drawing.Size(519, 36); 
            this._previewTextBox.TabIndex = 60;
            this._previewTextBox.Text = "";

 
            //
            // _fieldsCheckedListBox 
            // 
            this._fieldsCheckedListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._fieldsCheckedListBox.CheckOnClick = true;
            this._fieldsCheckedListBox.IntegralHeight = false;
            this._fieldsCheckedListBox.Location = new System.Drawing.Point(0, 0); 
            this._fieldsCheckedListBox.MultiColumn = true;
            this._fieldsCheckedListBox.Name = "_fieldsCheckedListBox"; 
            this._fieldsCheckedListBox.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this._columnsTableLayoutPanel.SetRowSpan(this._fieldsCheckedListBox, 2);
            this._fieldsCheckedListBox.Size = new System.Drawing.Size(388, 100); 
            this._fieldsCheckedListBox.TabIndex = 10;
            this._fieldsCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.OnFieldsCheckedListBoxItemCheck);
            //
            // _selectDistinctCheckBox 
            //
            this._selectDistinctCheckBox.AutoSize = true; 
            this._selectDistinctCheckBox.Location = new System.Drawing.Point(394, 2); 
            this._selectDistinctCheckBox.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this._selectDistinctCheckBox.Name = "_selectDistinctCheckBox"; 
            this._selectDistinctCheckBox.Size = new System.Drawing.Size(15, 14);
            this._selectDistinctCheckBox.TabIndex = 20;
            this._selectDistinctCheckBox.CheckedChanged += new System.EventHandler(this.OnSelectDistinctCheckBoxCheckedChanged);
            // 
            // _optionsTableLayoutPanel
            // 
            this._optionsTableLayoutPanel.AutoSize = true; 
            this._optionsTableLayoutPanel.ColumnCount = 1;
            this._optionsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
            this._optionsTableLayoutPanel.Controls.Add(this._addFilterButton, 0, 0);
            this._optionsTableLayoutPanel.Controls.Add(this._addSortButton, 0, 1);
            this._optionsTableLayoutPanel.Controls.Add(this._advancedOptionsButton, 0, 2);
            this._optionsTableLayoutPanel.Location = new System.Drawing.Point(394, 19); 
            this._optionsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this._optionsTableLayoutPanel.Name = "_optionsTableLayoutPanel"; 
            this._optionsTableLayoutPanel.RowCount = 3; 
            this._optionsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._optionsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this._optionsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._optionsTableLayoutPanel.Size = new System.Drawing.Size(115, 81);
            this._optionsTableLayoutPanel.TabIndex = 30;
 
            //
            // _addFilterButton 
            // 
            this._addFilterButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._addFilterButton.AutoSize = true; 
            this._addFilterButton.Location = new System.Drawing.Point(0, 2);
            this._addFilterButton.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this._addFilterButton.MinimumSize = new System.Drawing.Size(115, 23);
            this._addFilterButton.Name = "_addFilterButton"; 
            this._addFilterButton.Size = new System.Drawing.Size(115, 23);
            this._addFilterButton.TabIndex = 10; 
            this._addFilterButton.Click += new System.EventHandler(this.OnAddFilterButtonClick); 
            //
            // _addSortButton 
            //
            this._addSortButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._addSortButton.AutoSize = true;
            this._addSortButton.Location = new System.Drawing.Point(0, 29); 
            this._addSortButton.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this._addSortButton.MinimumSize = new System.Drawing.Size(115, 23); 
            this._addSortButton.Name = "_addSortButton"; 
            this._addSortButton.Size = new System.Drawing.Size(115, 23);
            this._addSortButton.TabIndex = 20; 
            this._addSortButton.Click += new System.EventHandler(this.OnAddSortButtonClick);
            //
            // _advancedOptionsButton
            // 
            this._advancedOptionsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._advancedOptionsButton.AutoSize = true; 
            this._advancedOptionsButton.Location = new System.Drawing.Point(0, 56); 
            this._advancedOptionsButton.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this._advancedOptionsButton.MinimumSize = new System.Drawing.Size(115, 23); 
            this._advancedOptionsButton.Name = "_advancedOptionsButton";
            this._advancedOptionsButton.Size = new System.Drawing.Size(115, 23);
            this._advancedOptionsButton.TabIndex = 30;
            this._advancedOptionsButton.Click += new System.EventHandler(this.OnAdvancedOptionsButtonClick); 

            // 
            // SqlDataSourceConfigureSelectPanel 
            //
            this.Controls.Add(this._fieldChooserPanel); 
            this.Controls.Add(this._customSqlRadioButton);
            this.Controls.Add(this._tableRadioButton);
            this.Controls.Add(this._retrieveDataLabel);
            this.Name = "SqlDataSourceConfigureSelectPanel"; 
            this.Size = new System.Drawing.Size(544, 274);
            this._columnsTableLayoutPanel.ResumeLayout(false); 
            this._columnsTableLayoutPanel.PerformLayout(); 
            this._optionsTableLayoutPanel.ResumeLayout(false);
            this._optionsTableLayoutPanel.PerformLayout(); 
            this._fieldChooserPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion 

        /// <devdoc> 
        /// Attempts to find a given column in a given table. 
        /// </devdoc>
        private DesignerDataColumn GetColumnFromTable(DesignerDataTableBase designerDataTable, string columnName) { 
            foreach (DesignerDataColumn designerDataColumn in designerDataTable.Columns) {
                if (designerDataColumn.Name == columnName) {
                    return designerDataColumn;
                } 
            }
            return null; 
        } 

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer.
        /// </devdoc>
        private void InitializeUI() { 
            Caption = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_PanelCaption);
 
            _retrieveDataLabel.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_RetrieveDataLabel); 
            _tableRadioButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_TableLabel);
            _customSqlRadioButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_CustomSqlLabel); 
            _previewLabel.Text = SR.GetString(SR.SqlDataSource_General_PreviewLabel);
            _tableNameLabel.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_TableNameLabel);
            _addSortButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_SortButton);
            _addFilterButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_FilterLabel); 
            _selectDistinctCheckBox.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_SelectDistinctLabel);
            _advancedOptionsButton.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_AdvancedOptions); 
            _fieldsLabel.Text = SR.GetString(SR.SqlDataSourceConfigureSelectPanel_FieldsLabel); 

            // Accessibility text 
            _tableRadioButton.AccessibleDescription = _retrieveDataLabel.Text;
            _tableRadioButton.AccessibleName = _tableRadioButton.Text;
            _customSqlRadioButton.AccessibleDescription = _retrieveDataLabel.Text;
            _customSqlRadioButton.AccessibleName = _customSqlRadioButton.Text; 

            UpdateFonts(); 
        } 

        /// <devdoc> 
        /// Attempts to load a serialized table query and populat the UI with
        /// the correct state. It will silently fail if the state is
        /// inconsistent with the metadata available.
        /// </devdoc> 
        private bool LoadTableQueryState(Hashtable tableQueryState) {
            Debug.Assert(tableQueryState != null); 
 
            SqlDataSource sqlDataSource = _sqlDataSourceDesigner.Component as SqlDataSource;
 
            //

            int connectionStringHash = (int)tableQueryState["Conn_ConnectionStringHash"];
            string providerName = (string)tableQueryState["Conn_ProviderName"]; 

            // Check if this is the correct connection for recreating the table query 
            if ((connectionStringHash != _dataConnection.ConnectionString.GetHashCode()) || 
                (providerName != _dataConnection.ProviderName)) {
                return false; 
            }

            // Generation info
            int generateMode = (int)tableQueryState["Generate_Mode"]; 

            // Try to find the table that was used 
            string tableName = (string)tableQueryState["Table_Name"]; 
            TableItem tableItem = null;
            foreach (TableItem item in _tablesComboBox.Items) { 
                if (item.DesignerDataTable.Name == tableName) {
                    tableItem = item;
                    break;
                } 
            }
            if (tableItem == null) { 
                return false; 
            }
            DesignerDataTableBase designerDataTable = tableItem.DesignerDataTable; 

            // Make sure that all the required fields are present
            int fieldCount = (int)tableQueryState["Fields_Count"];
            ArrayList fields = new ArrayList(); 
            for (int i = 0; i < fieldCount; i++) {
                string fieldName = (string)tableQueryState["Fields_FieldName" + i.ToString(CultureInfo.InvariantCulture)]; 
                DesignerDataColumn designerDataColumn = GetColumnFromTable(designerDataTable, fieldName); 
                if (designerDataColumn == null) {
                    return false; 
                }
                fields.Add(designerDataColumn);
            }
 
            // Extract asterisk field value
            bool asteriskField = (bool)tableQueryState["AsteriskField"]; 
 
            // Extract distinct value
            bool distinct = (bool)tableQueryState["Distinct"]; 


            // Create a list of unused parameters so that we can assign them to
            // the appropriate filter clause. 
            System.Collections.Generic.List<Parameter> unusedParameters = new System.Collections.Generic.List<Parameter>();
            foreach (ICloneable parameter in sqlDataSource.SelectParameters) { 
                unusedParameters.Add((Parameter)parameter.Clone()); 
            }
 
            DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(_dataConnection.ProviderName);
            bool supportsNamedParameters = SqlDataSourceDesigner.SupportsNamedParameters(factory);

            // Make sure that all the required fields in the filters are present 
            int filterCount = (int)tableQueryState["Filters_Count"];
            ArrayList filters = new ArrayList(); 
            for (int i = 0; i < filterCount; i++) { 
                string fieldName = (string)tableQueryState["Filters_FieldName" + i.ToString(CultureInfo.InvariantCulture)];
                string operatorFormat = (string)tableQueryState["Filters_OperatorFormat" + i.ToString(CultureInfo.InvariantCulture)]; 
                bool isBinary = (bool)tableQueryState["Filters_IsBinary" + i.ToString(CultureInfo.InvariantCulture)];
                string value = (string)tableQueryState["Filters_Value" + i.ToString(CultureInfo.InvariantCulture)];
                string parameterName = (string)tableQueryState["Filters_ParameterName" + i.ToString(CultureInfo.InvariantCulture)];
                DesignerDataColumn designerDataColumn = GetColumnFromTable(designerDataTable, fieldName); 
                if (designerDataColumn == null) {
                    return false; 
                } 

                // Figure out the parameter that is supposed to be used with this filter clause 
                Parameter parameter = null;
                if (parameterName != null) {
                    if (supportsNamedParameters) {
                        // Try to find the right named parameter 
                        foreach (Parameter p in unusedParameters) {
                            if (p.Name == parameterName) { 
                                parameter = p; 
                                break;
                            } 
                        }
                        if (parameter != null) {
                            // Found named parameter, remove it from list of unused parameters
                            unusedParameters.Remove(parameter); 
                        }
                        else { 
                            // Could not find named parameter, create a new one (sort of an error case) 
                            parameter = new Parameter(parameterName);
                        } 
                    }
                    else {
                        // Named parameters not supported, just get the next unused parameter
                        if (unusedParameters.Count > 0) { 
                            parameter = unusedParameters[0];
                            unusedParameters.RemoveAt(0); 
                        } 
                        else {
                            // No more parameters left, this is sort of an error case, but we allow it 
                            parameter = new Parameter(parameterName);
                        }
                    }
                } 

                filters.Add(new SqlDataSourceFilterClause(_dataConnection, designerDataTable, designerDataColumn, operatorFormat, isBinary, value, parameter)); 
            } 

            // Make sure that all the required fields in the orders are present 
            int orderCount = (int)tableQueryState["Orders_Count"];
            ArrayList orders = new ArrayList();
            for (int i = 0; i < orderCount; i++) {
                string fieldName = (string)tableQueryState["Orders_FieldName" + i.ToString(CultureInfo.InvariantCulture)]; 
                bool isDescending = (bool)tableQueryState["Orders_IsDescending" + i.ToString(CultureInfo.InvariantCulture)];
                DesignerDataColumn designerDataColumn = GetColumnFromTable(designerDataTable, fieldName); 
                if (designerDataColumn == null) { 
                    return false;
                } 
                orders.Add(new SqlDataSourceOrderClause(_dataConnection, designerDataTable, designerDataColumn, isDescending));
            }

 
            // Before we fully load the query, we have to check if the current
            // SELECT command is the same as the one that would be generated 
            // from this query. 
            SqlDataSourceTableQuery tempQuery = new SqlDataSourceTableQuery(_dataConnection, designerDataTable);
            foreach (DesignerDataColumn designerDataColumn in fields) { 
                tempQuery.Fields.Add(designerDataColumn);
            }
            tempQuery.AsteriskField = asteriskField;
            tempQuery.Distinct = distinct; 
            foreach (SqlDataSourceFilterClause filterClause in filters) {
                tempQuery.FilterClauses.Add(filterClause); 
            } 
            foreach (SqlDataSourceOrderClause orderClause in orders) {
                tempQuery.OrderClauses.Add(orderClause); 
            }

            bool includeOldValues = (generateMode == 2);
            string oldValuesFormatString = GetOldValuesFormatString(sqlDataSource, false); 
            SqlDataSourceQuery selectQuery = tempQuery.GetSelectQuery();
            SqlDataSourceQuery insertQuery = tempQuery.GetInsertQuery(); 
            SqlDataSourceQuery updateQuery = tempQuery.GetUpdateQuery(oldValuesFormatString, includeOldValues); 
            SqlDataSourceQuery deleteQuery = tempQuery.GetDeleteQuery(oldValuesFormatString, includeOldValues);
 
            // If and of the commands don't match, it must be a custom query
            if ((selectQuery != null) && (sqlDataSource.SelectCommand != selectQuery.Command)) {
                return false;
            } 
            if ((insertQuery != null) && (sqlDataSource.InsertCommand.Trim().Length > 0) && (sqlDataSource.InsertCommand != insertQuery.Command)) {
                return false; 
            } 
            if ((updateQuery != null) && (sqlDataSource.UpdateCommand.Trim().Length > 0) && (sqlDataSource.UpdateCommand != updateQuery.Command)) {
                return false; 
            }
            if ((deleteQuery != null) && (sqlDataSource.DeleteCommand.Trim().Length > 0) && (sqlDataSource.DeleteCommand != deleteQuery.Command)) {
                return false;
            } 

 
            // Everything looks in order, go ahead and set all the properties 
            _tableQuery = new SqlDataSourceTableQuery(_dataConnection, designerDataTable);
 

            _tablesComboBox.SelectedItem = tableItem;
            ArrayList fieldIndices = new ArrayList();
            foreach (DesignerDataColumn designerDataColumn in fields) { 
                foreach (ColumnItem columnItem in _fieldsCheckedListBox.Items) {
                    if (columnItem.DesignerDataColumn == designerDataColumn) { 
                        fieldIndices.Add(_fieldsCheckedListBox.Items.IndexOf(columnItem)); 
                    }
                } 
            }
            foreach (int fieldIndex in fieldIndices) {
                _fieldsCheckedListBox.SetItemChecked(fieldIndex, true);
            } 

            if (asteriskField) { 
                _fieldsCheckedListBox.SetItemChecked(0, true); 
            }
 
            _selectDistinctCheckBox.Checked = distinct;

            _generateMode = generateMode;
 
            foreach (SqlDataSourceFilterClause filterClause in filters) {
                _tableQuery.FilterClauses.Add(filterClause); 
            } 

            foreach (SqlDataSourceOrderClause orderClause in orders) { 
                _tableQuery.OrderClauses.Add(orderClause);
            }

            return true; 
        }
 
        /// <devdoc> 
        /// </devdoc>
        private void OnAddFilterButtonClick(object sender, System.EventArgs e) { 
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active");

            SqlDataSourceConfigureFilterForm form = new SqlDataSourceConfigureFilterForm(_sqlDataSourceDesigner, _tableQuery);
            DialogResult result = UIServiceHelper.ShowDialog(_sqlDataSourceDesigner.Component.Site, form); 
            if (result == DialogResult.OK) {
                // Copy new list of filter clauses 
                _tableQuery.FilterClauses.Clear(); 
                foreach (SqlDataSourceFilterClause clause in form.FilterClauses) {
                    _tableQuery.FilterClauses.Add(clause); 
                }
                UpdatePreview();
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        private void OnAddSortButtonClick(object sender, System.EventArgs e) {
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active"); 

            SqlDataSourceConfigureSortForm form = new SqlDataSourceConfigureSortForm(_sqlDataSourceDesigner, _tableQuery);
            DialogResult result = UIServiceHelper.ShowDialog(_sqlDataSourceDesigner.Component.Site, form);
            if (result == DialogResult.OK) { 
                // Copy new list of filter clauses
                _tableQuery.OrderClauses.Clear(); 
                foreach (SqlDataSourceOrderClause clause in form.OrderClauses) { 
                    _tableQuery.OrderClauses.Add(clause);
                } 
                UpdatePreview();
            }
        }
 
        private void OnAdvancedOptionsButtonClick(object sender, System.EventArgs e) {
            SqlDataSourceAdvancedOptionsForm form = new SqlDataSourceAdvancedOptionsForm(ServiceProvider); 
            form.SetAllowAutogenerate(_tableQuery.IsPrimaryKeySelected() && !_selectDistinctCheckBox.Checked); 
            form.GenerateStatements = (_generateMode > 0);
            form.OptimisticConcurrency = (_generateMode == 2); 
            DialogResult result = UIServiceHelper.ShowDialog(ServiceProvider, form);
            if (result == DialogResult.OK) {
                _generateMode = 0;
                if (form.GenerateStatements) { 
                    if (form.OptimisticConcurrency) {
                        _generateMode = 2; 
                    } 
                    else {
                        _generateMode = 1; 
                    }
                }
            }
        } 

        /// <devdoc> 
        /// Called when the user click Finish on the wizard. 
        /// </devdoc>
        protected internal override void OnComplete() { 
            // If the table/field picker was used, store the state of the picker for later re-entrancy
            if (_tableRadioButton.Checked) {
                _sqlDataSourceDesigner.TableQueryState = SaveTableQueryState();
 
                // Store optimistic/pessimistic settings
                SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component; 
                bool includeOldValues = (_generateMode == 2); 
                sqlDataSource.OldValuesParameterFormatString = GetOldValuesFormatString(sqlDataSource, includeOldValues);
                if (includeOldValues) { 
                    sqlDataSource.ConflictDetection = ConflictOptions.CompareAllValues;
                }
                else {
                    sqlDataSource.ConflictDetection = ConflictOptions.OverwriteChanges; 
                }
            } 
            else { 
                _sqlDataSourceDesigner.TableQueryState = null;
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        private void OnCustomSqlRadioButtonCheckedChanged(object sender, System.EventArgs e) {
            UpdateEnabledUI(); 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void OnFieldsCheckedListBoxItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e) {
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active");
 
            if (_ignoreFieldCheckChanges) {
                return; 
            } 

            UpdateEnabledUI(); 

            // If a check is about to be removed, and there is only one check, disable Next
            ParentWizard.NextButton.Enabled = !((e.NewValue == CheckState.Unchecked) && (_fieldsCheckedListBox.CheckedItems.Count == 1));
 
            Debug.Assert(_tableQuery != null);
 
            // If the item being checked is the asterisk field, select it and clear out all others 
            if ((e.Index == 0) && (e.NewValue == CheckState.Checked)) {
                _tableQuery.AsteriskField = true; // Automatically clears out internal field list 
                _ignoreFieldCheckChanges = true;
                for (int i = 1; i < _fieldsCheckedListBox.Items.Count; i++) {
                    _fieldsCheckedListBox.SetItemChecked(i, false);
                } 
                _ignoreFieldCheckChanges = false;
            } 
            else { 
                // Always un-set the asterisk field
                _tableQuery.AsteriskField = false; 

                _ignoreFieldCheckChanges = true;
                _fieldsCheckedListBox.SetItemChecked(0, false);
                if (e.Index > 0) { 
                    if (e.NewValue == CheckState.Checked) {
                        // Add the field to the field list 
                        _tableQuery.Fields.Add(((ColumnItem)_fieldsCheckedListBox.Items[e.Index]).DesignerDataColumn); 
                    }
                    else { 
                        // Remove the field from the field list
                        _tableQuery.Fields.Remove(((ColumnItem)_fieldsCheckedListBox.Items[e.Index]).DesignerDataColumn);
                    }
                } 
                _ignoreFieldCheckChanges = false;
            } 
 
            // If not all primary key fields have been selected or distinct is checked, turn off autogenerate
            if (!_tableQuery.IsPrimaryKeySelected() || _selectDistinctCheckBox.Checked) { 
                _generateMode = 0;
            }

            UpdatePreview(); 
        }
 
        protected override void OnFontChanged(EventArgs e) { 
            base.OnFontChanged(e);
            UpdateFonts(); 
        }

        /// <devdoc>
        /// </devdoc> 
        public override bool OnNext() {
            // Check what the previous panel was, and check how different the state is. 
            // If the state is too different, just new up a fresh panel. If it is the same, 
            // then just use the existing panel.
            if (_tableRadioButton.Checked) { 
                // User is using the Table/Field picker

                Debug.Assert(_tableQuery != null, "This code should not be called unless the table/field picker is active");
 
                // Get all the autogenerated queries for the commands
                SqlDataSourceQuery selectQuery = _tableQuery.GetSelectQuery(); 
                Debug.Assert(selectQuery != null, "Did not expect a null SelectQuery"); 
                if (selectQuery == null) {
                    selectQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]); 
                }

                SqlDataSourceQuery insertQuery;
                SqlDataSourceQuery updateQuery; 
                SqlDataSourceQuery deleteQuery;
 
                if (_generateMode > 0) { 
                    SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
                    bool includeOldValues = (_generateMode == 2); 
                    string oldValuesFormatString = GetOldValuesFormatString(sqlDataSource, includeOldValues);
                    insertQuery = _tableQuery.GetInsertQuery();
                    if (insertQuery == null) {
                        insertQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]); 
                    }
                    updateQuery = _tableQuery.GetUpdateQuery(oldValuesFormatString, includeOldValues); 
                    if (updateQuery == null) { 
                        updateQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    } 
                    deleteQuery = _tableQuery.GetDeleteQuery(oldValuesFormatString, includeOldValues);
                    if (deleteQuery == null) {
                        deleteQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    } 
                }
                else { 
                    // Just create empty queries 
                    insertQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                    updateQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]); 
                    deleteQuery = new SqlDataSourceQuery(String.Empty, SqlDataSourceCommandType.Text, new Parameter[0]);
                }

 
                // Go straight to summary panel - parameters have already been
                // configured in the WHERE clause builder 
                SqlDataSourceSummaryPanel nextPanel = NextPanel as SqlDataSourceSummaryPanel; 
                if (nextPanel == null) {
                    nextPanel = ((SqlDataSourceWizardForm)ParentWizard).GetSummaryPanel(); 
                    NextPanel = nextPanel;
                }
                nextPanel.SetQueries(_dataConnection,
                    selectQuery, 
                    insertQuery,
                    updateQuery, 
                    deleteQuery); 
                return true;
            } 
            else {
                // User want to enter a custom SQL command or stored procedure

                SqlDataSourceCustomCommandPanel nextPanel = NextPanel as SqlDataSourceCustomCommandPanel; 
                if (nextPanel == null) {
                    // Only create a new custom command panel if we did not previously have one 
                    nextPanel = ((SqlDataSourceWizardForm)ParentWizard).GetCustomCommandPanel(); 
                    NextPanel = nextPanel;
                } 

                // Clone the lists of parameters so we don't touch the originals
                SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
                ArrayList selectParameters = new ArrayList(); 
                ArrayList insertParameters = new ArrayList();
                ArrayList updateParameters = new ArrayList(); 
                ArrayList deleteParameters = new ArrayList(); 
                _sqlDataSourceDesigner.CopyList(sqlDataSource.SelectParameters, selectParameters);
                _sqlDataSourceDesigner.CopyList(sqlDataSource.InsertParameters, insertParameters); 
                _sqlDataSourceDesigner.CopyList(sqlDataSource.UpdateParameters, updateParameters);
                _sqlDataSourceDesigner.CopyList(sqlDataSource.DeleteParameters, deleteParameters);

                nextPanel.SetQueries( 
                    _dataConnection,
                    new SqlDataSourceQuery(sqlDataSource.SelectCommand, sqlDataSource.SelectCommandType, selectParameters), 
                    new SqlDataSourceQuery(sqlDataSource.InsertCommand, sqlDataSource.InsertCommandType, insertParameters), 
                    new SqlDataSourceQuery(sqlDataSource.UpdateCommand, sqlDataSource.UpdateCommandType, updateParameters),
                    new SqlDataSourceQuery(sqlDataSource.DeleteCommand, sqlDataSource.DeleteCommandType, deleteParameters)); 

                return true;
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        public override void OnPrevious() {
        } 

        /// <devdoc>
        /// </devdoc>
        private void OnSelectDistinctCheckBoxCheckedChanged(object sender, System.EventArgs e) { 
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active");
 
            _tableQuery.Distinct = _selectDistinctCheckBox.Checked; 

            // If not all primary key fields have been selected or distinct is checked, turn off autogenerate 
            if (!_tableQuery.IsPrimaryKeySelected() || _selectDistinctCheckBox.Checked) {
                _generateMode = 0;
            }
 
            UpdatePreview();
        } 
 
        /// <devdoc>
        /// </devdoc> 
        private void OnTableRadioButtonCheckedChanged(object sender, System.EventArgs e) {
            UpdateEnabledUI();
        }
 
        /// <devdoc>
        /// </devdoc> 
        private void OnTablesComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            // WinForms raises a SelectedIndexChanged event even if you re-select
            // the currently selected item, so we have to ignore that, except 
            // when we are trying to clear the selection (table == null).
            TableItem currentTable = _tablesComboBox.SelectedItem as TableItem;
            if ((currentTable != null) && (_previousTable == currentTable)) {
                return; 
            }
 
            Cursor originalCursor = Cursor.Current; 
            // If the table changed, reset all the selections and create new defaults
            _fieldsCheckedListBox.Items.Clear(); 
            _selectDistinctCheckBox.Checked = false;
            _generateMode = 0;
            try {
                Cursor.Current = Cursors.WaitCursor; 

                if (currentTable != null) { 
                    ICollection columns = currentTable.DesignerDataTable.Columns; 
                    _tableQuery = new SqlDataSourceTableQuery(_dataConnection, currentTable.DesignerDataTable);
                    // Add the asterisk field and all the columns 
                    _fieldsCheckedListBox.Items.Add(new ColumnItem());
                    foreach (DesignerDataColumn dataColumn in columns) {
                        _fieldsCheckedListBox.Items.Add(new ColumnItem(dataColumn));
                    } 
                }
                else { 
                    // No new table is selected, clear the old query 
                    _tableQuery = null;
                } 

                _previousTable = currentTable;
            }
            catch (Exception ex) { 
                UIServiceHelper.ShowError(
                    ServiceProvider, 
                    ex, 
                    SR.GetString(SR.SqlDataSourceConfigureSelectPanel_CouldNotGetTableSchema));
            } 
            finally {
                UpdateFieldsCheckedListBoxColumnWidth();
                UpdateEnabledUI();
                UpdatePreview(); 
                Cursor.Current = originalCursor;
            } 
        } 

        /// <devdoc> 
        /// </devdoc>
        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
 
            if (Visible) {
                ParentWizard.FinishButton.Enabled = false; 
 
                // If the data connection has changed, force a refresh of the database metadata
                DesignerDataConnection newConnection = ((SqlDataSourceWizardForm)ParentWizard).DesignerDataConnection; 
                if (!SqlDataSourceDesigner.ConnectionsEqual(_dataConnection, newConnection)) {
                    _dataConnection = newConnection;
                    _requiresRefresh = true;
                } 

                if (_requiresRefresh) { 
                    Debug.Assert(_dataConnection != null); 

                    Cursor originalCursor = Cursor.Current; 
                    try {
                        Cursor.Current = Cursors.WaitCursor;

                        // Reset UI 
                        _tablesComboBox.SelectedIndex = -1;
                        _tablesComboBox.Items.Clear(); 
 
                        // Try to get schema for the connection
                        IDataEnvironment dataEnvironment = ((SqlDataSourceWizardForm)ParentWizard).DataEnvironment; 
                        IDesignerDataSchema designerDataSchema = null;
                        if (_dataConnection != null) {
                            designerDataSchema = dataEnvironment.GetConnectionSchema(_dataConnection);
                        } 

                        // Try to get list of tables and views 
                        if (designerDataSchema != null) { 
                            // Populate tables list
                            System.Collections.Generic.List<TableItem> tableItems = new System.Collections.Generic.List<TableItem>(); 
                            if (designerDataSchema.SupportsSchemaClass(DesignerDataSchemaClass.Tables)) {
                                ICollection tableCollection = designerDataSchema.GetSchemaItems(DesignerDataSchemaClass.Tables);
                                if (tableCollection != null) {
                                    foreach (DesignerDataTable dataTable in tableCollection) { 
                                        // Hide special ASP.net tables used for cache invalidation
                                        if (!dataTable.Name.ToLowerInvariant().StartsWith(SqlDataSourceDesigner.AspNetDatabaseObjectPrefix.ToLowerInvariant(), StringComparison.Ordinal)) { 
                                            tableItems.Add(new TableItem(dataTable)); 
                                        }
                                    } 
                                }
                            }

                            if (designerDataSchema.SupportsSchemaClass(DesignerDataSchemaClass.Views)) { 
                                ICollection viewCollection = designerDataSchema.GetSchemaItems(DesignerDataSchemaClass.Views);
                                if (viewCollection != null) { 
                                    foreach (DesignerDataView dataView in viewCollection) { 
                                        tableItems.Add(new TableItem(dataView));
                                    } 
                                }
                            }

                            tableItems.Sort(new System.Comparison<TableItem>( 
                                delegate(TableItem a, TableItem b) {
                                    return String.Compare(a.DesignerDataTable.Name, b.DesignerDataTable.Name, StringComparison.InvariantCultureIgnoreCase); 
                                })); 
                            _tablesComboBox.Items.AddRange(tableItems.ToArray());
                            _tablesComboBox.InvalidateDropDownWidth(); 
                        }

                        // Only enable table/field picker if there is at least one table
                        if (_tablesComboBox.Items.Count > 0) { 

                            // Try to perform "smart" re-entry 
                            Hashtable tableQueryState = _sqlDataSourceDesigner.TableQueryState; 
                            bool successfullReentry = false;
                            if (tableQueryState != null) { 
                                successfullReentry = LoadTableQueryState(tableQueryState);
                            }
                            if (!successfullReentry) {
                                // Initial attempt to reenter failed, try to parse command 
                                successfullReentry = LoadParsedSqlState();
                            } 
 
                            if (!successfullReentry) {
                                // If smart re-entry failed, potentially select Custom SQL automatically 

                                // Auto-select first table
                                _tablesComboBox.SelectedIndex = 0;
 
                                SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
                                bool hasCustomSql = ((sqlDataSource.SelectCommand.Trim().Length > 0) || 
                                                    (sqlDataSource.InsertCommand.Trim().Length > 0) || 
                                                    (sqlDataSource.UpdateCommand.Trim().Length > 0) ||
                                                    (sqlDataSource.DeleteCommand.Trim().Length > 0)); 

                                // Use table picker by default only if there is no custom command set
                                _tableRadioButton.Checked = !hasCustomSql;
                                _customSqlRadioButton.Checked = hasCustomSql; 
                            }
                            else { 
                                // Re-entry was successfull, maintain state 
                                _tableRadioButton.Checked = true;
                                _customSqlRadioButton.Checked = false; 
                            }

                            _tableRadioButton.Enabled = true;
                        } 
                        else {
                            // If there is no schema, force the user to use Custom SQL, and disable other options 
                            _customSqlRadioButton.Checked = true; 

                            _tableRadioButton.Enabled = false; 
                        }

                        UpdatePreview();
                    } 
                    finally {
                        Cursor.Current = originalCursor; 
                    } 

                    _requiresRefresh = false; 
                }

                UpdateEnabledUI();
            } 
        }
 
        private bool LoadParsedSqlState() { 
            SqlDataSource sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
 
            // Only a SELECT command is specified, go ahead and try to parse it
            string[] commandParts = SqlDataSourceCommandParser.ParseSqlString(_sqlDataSourceDesigner.SelectCommand);
            if (commandParts == null) {
                return false; 
            }
 
            Debug.Assert(commandParts.Length >= 2, "SQL Parser: Expected at least two parts in the command (1 field + 1 table)"); 

 
            bool hasAsterisk = false;
            string tableName = SqlDataSourceCommandParser.GetLastIdentifierPart(commandParts[commandParts.Length - 1]);
            if (tableName == null) {
                // Problem parsing table name, abort 
                return false;
            } 
            System.Collections.Generic.List<string> fieldNames = new System.Collections.Generic.List<string>(); 
            for (int i = 0; i < commandParts.Length - 1; i++) {
                string fieldName = SqlDataSourceCommandParser.GetLastIdentifierPart(commandParts[i]); 
                if (fieldName == null) {
                    // Problem parsing field name, abort
                    return false;
                } 
                if (fieldName == "*") {
                    hasAsterisk = true; 
                } 
                else {
                    if (fieldName.Length == 0) { 
                        return false;
                    }
                    fieldNames.Add(fieldName);
                } 
            }
 
            // We only support the asterisk field if it is the only field 
            if (hasAsterisk && (fieldNames.Count != 0)) {
                return false; 
            }


            // Try to find the table that was used 
            TableItem tableItem = null;
            foreach (TableItem item in _tablesComboBox.Items) { 
                if (item.DesignerDataTable.Name == tableName) { 
                    tableItem = item;
                    break; 
                }
            }
            if (tableItem == null) {
                return false; 
            }
            DesignerDataTableBase designerDataTable = tableItem.DesignerDataTable; 
 
            // Make sure that all the required fields are present
            System.Collections.Generic.List<DesignerDataColumn> fields = new System.Collections.Generic.List<DesignerDataColumn>(); 
            foreach (string fieldName in fieldNames) {
                DesignerDataColumn designerDataColumn = GetColumnFromTable(designerDataTable, fieldName);
                if (designerDataColumn == null) {
                    return false; 
                }
                fields.Add(designerDataColumn); 
            } 

 
            // NOTE: We do not verify that the new generated SELECT statement
            // matches the existing SELECT statement because more than likely
            // they will be different.
 

            // If any of the three modification commands are set, we need 
            // to make sure they are the same as the ones we would autogenerate 
            // anyway. If not, we can't reload the wizard's query state since
            // it would involve data loss for those commands. 
            bool generateOtherCommands = (sqlDataSource.DeleteCommand.Trim().Length > 0) ||
                                         (sqlDataSource.InsertCommand.Trim().Length > 0) ||
                                         (sqlDataSource.UpdateCommand.Trim().Length > 0);
            if (generateOtherCommands) { 
                // Create a temporary query so we can generate the delete/insert/update commands
                SqlDataSourceTableQuery tempQuery = new SqlDataSourceTableQuery(_dataConnection, designerDataTable); 
                foreach (DesignerDataColumn designerDataColumn in fields) { 
                    tempQuery.Fields.Add(designerDataColumn);
                } 
                tempQuery.AsteriskField = hasAsterisk;

                SqlDataSourceQuery insertQuery = tempQuery.GetInsertQuery();
                string oldValuesFormatString = GetOldValuesFormatString(sqlDataSource, false); 
                SqlDataSourceQuery updateQuery = tempQuery.GetUpdateQuery(oldValuesFormatString, false);
                SqlDataSourceQuery deleteQuery = tempQuery.GetDeleteQuery(oldValuesFormatString, false); 
 
                if ((insertQuery != null) && (sqlDataSource.InsertCommand.Trim().Length > 0) && (sqlDataSource.InsertCommand != insertQuery.Command)) {
                    return false; 
                }
                if ((updateQuery != null) && (sqlDataSource.UpdateCommand.Trim().Length > 0) && (sqlDataSource.UpdateCommand != updateQuery.Command)) {
                    return false;
                } 
                if ((deleteQuery != null) && (sqlDataSource.DeleteCommand.Trim().Length > 0) && (sqlDataSource.DeleteCommand != deleteQuery.Command)) {
                    return false; 
                } 
            }
 
            // Everything looks in order, go ahead and set all the properties
            _tableQuery = new SqlDataSourceTableQuery(_dataConnection, designerDataTable);

 
            _tablesComboBox.SelectedItem = tableItem;
            ArrayList fieldIndices = new ArrayList(); 
            foreach (DesignerDataColumn designerDataColumn in fields) { 
                foreach (ColumnItem columnItem in _fieldsCheckedListBox.Items) {
                    if (columnItem.DesignerDataColumn == designerDataColumn) { 
                        fieldIndices.Add(_fieldsCheckedListBox.Items.IndexOf(columnItem));
                    }
                }
            } 
            foreach (int fieldIndex in fieldIndices) {
                _fieldsCheckedListBox.SetItemChecked(fieldIndex, true); 
            } 

            if (hasAsterisk) { 
                _fieldsCheckedListBox.SetItemChecked(0, true);
            }

            // Set the autogenerate mode based on our findings (see note at top of file about the mode values) 
            // This has to be set last since other actions above might have reset the value
            _generateMode = (generateOtherCommands ? 1 : 0); 
 
            return true;
        } 

        public void ResetUI() {
            _tableRadioButton.Checked = true;
            _customSqlRadioButton.Checked = false; 
            _generateMode = 0;
            _tablesComboBox.Items.Clear(); 
            _fieldsCheckedListBox.Items.Clear(); 
            _requiresRefresh = true;
        } 

        /// <devdoc>
        /// Serializes the entire state of a table query into a hashtable.
        /// </devdoc> 
        private Hashtable SaveTableQueryState() {
            Debug.Assert(_tableQuery != null, "This method should not be called unless the table/field picker is active"); 
 
            Hashtable tableQueryState = new Hashtable();
 
            // Connection information
            tableQueryState.Add("Conn_ConnectionStringHash", _tableQuery.DesignerDataConnection.ConnectionString.GetHashCode());
            tableQueryState.Add("Conn_ProviderName", _tableQuery.DesignerDataConnection.ProviderName);
 
            // Generation info
            tableQueryState.Add("Generate_Mode", _generateMode); 
 
            // Table (name)
            tableQueryState.Add("Table_Name", _tableQuery.DesignerDataTable.Name); 

            // Fields
            tableQueryState.Add("Fields_Count", _tableQuery.Fields.Count);
            for (int i = 0; i < _tableQuery.Fields.Count; i++) { 
                tableQueryState.Add("Fields_FieldName" + i.ToString(CultureInfo.InvariantCulture), ((DesignerDataColumn)_tableQuery.Fields[i]).Name);
            } 
 
            // Asterisk
            tableQueryState.Add("AsteriskField", _tableQuery.AsteriskField); 

            // Distinct
            tableQueryState.Add("Distinct", _tableQuery.Distinct);
 
            // Filters (field + operator + binary + value)
            tableQueryState.Add("Filters_Count", _tableQuery.FilterClauses.Count); 
            for (int i = 0; i < _tableQuery.FilterClauses.Count; i++) { 
                SqlDataSourceFilterClause filterClause = _tableQuery.FilterClauses[i] as SqlDataSourceFilterClause;
                string s = i.ToString(CultureInfo.InvariantCulture); 
                tableQueryState.Add("Filters_FieldName" + s, filterClause.DesignerDataColumn.Name);
                tableQueryState.Add("Filters_OperatorFormat" + s, filterClause.OperatorFormat);
                tableQueryState.Add("Filters_IsBinary" + s, filterClause.IsBinary);
                tableQueryState.Add("Filters_Value" + s, filterClause.Value); 
                tableQueryState.Add("Filters_ParameterName" + s, (filterClause.Parameter != null ? filterClause.Parameter.Name : null));
            } 
 
            // Orders (column + isdescending)
            tableQueryState.Add("Orders_Count", _tableQuery.OrderClauses.Count); 
            for (int i = 0; i < _tableQuery.OrderClauses.Count; i++) {
                tableQueryState.Add("Orders_FieldName" + i.ToString(CultureInfo.InvariantCulture), ((SqlDataSourceOrderClause)_tableQuery.OrderClauses[i]).DesignerDataColumn.Name);
                tableQueryState.Add("Orders_IsDescending" + i.ToString(CultureInfo.InvariantCulture), ((SqlDataSourceOrderClause)_tableQuery.OrderClauses[i]).IsDescending);
            } 

            return tableQueryState; 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void UpdateEnabledUI() {
            _fieldChooserPanel.Enabled = _tableRadioButton.Checked;
 
            if (_customSqlRadioButton.Checked) {
                ParentWizard.NextButton.Enabled = true; 
            } 

            if (_tableRadioButton.Checked) { 
                // If there are no columns checked, disable Next
                ParentWizard.NextButton.Enabled = (_tablesComboBox.Items.Count > 0) && (_fieldsCheckedListBox.CheckedItems.Count > 0);

                // Enable/disable UI in the event that there are no fields for this 
                // table (this happens when there was an error retrieving schema)
                bool columnsAvailable = (_fieldsCheckedListBox.Items.Count > 0); 
                _fieldsLabel.Enabled = columnsAvailable; 
                _fieldsCheckedListBox.Enabled = columnsAvailable;
                _selectDistinctCheckBox.Enabled = columnsAvailable; 
                _addFilterButton.Enabled = columnsAvailable;
                _addSortButton.Enabled = columnsAvailable;
                _advancedOptionsButton.Enabled = columnsAvailable;
                _previewLabel.Enabled = columnsAvailable; 
                _previewTextBox.Enabled = columnsAvailable;
            } 
        } 

        private void UpdateFieldsCheckedListBoxColumnWidth() { 
            // Find the size of the widest column name
            int maxWidth = 0;
            using (Graphics g = _fieldsCheckedListBox.CreateGraphics()) {
                foreach (ColumnItem item in _fieldsCheckedListBox.Items) { 
                    string text = item.ToString();
                    maxWidth = Math.Max(maxWidth, (int)g.MeasureString(text, _fieldsCheckedListBox.Font).Width); 
                } 
            }
 
            // Add room for the checkbox
            maxWidth += 50;

            // Restrict column width to half the width of the control 
            maxWidth = Math.Min(maxWidth, _fieldsCheckedListBox.Width / 2);
 
            _fieldsCheckedListBox.ColumnWidth = maxWidth; 
        }
 
        private void UpdateFonts() {
            Font boldFont = new Font(Font, FontStyle.Bold);
            _retrieveDataLabel.Font = boldFont;
        } 

        /// <devdoc> 
        /// Updates the SQL preview textbox with the current query. 
        /// </devdoc>
        private void UpdatePreview() { 
            if (_tableQuery != null) {
                SqlDataSourceQuery selectQuery = _tableQuery.GetSelectQuery();
                _previewTextBox.Text = (selectQuery == null ? String.Empty : selectQuery.Command);
            } 
            else {
                _previewTextBox.Text = String.Empty; 
            } 
        }
 

        /// <devdoc>
        /// Represents a column a user can select.
        /// </devdoc> 
        private sealed class ColumnItem {
            private DesignerDataColumn _designerDataColumn; 
 
            public ColumnItem() {
                // Asterisk field 
            }

            public ColumnItem(DesignerDataColumn designerDataColumn) {
                // Column field 
                Debug.Assert(designerDataColumn != null);
                _designerDataColumn = designerDataColumn; 
            } 

            public DesignerDataColumn DesignerDataColumn { 
                get {
                    return _designerDataColumn;
                }
            } 

            public override string ToString() { 
                if (_designerDataColumn != null) { 
                    return _designerDataColumn.Name;
                } 
                else {
                    return "*";
                }
            } 
        }
 
 
        /// <devdoc>
        /// Represents a table a user can select fields from. 
        /// </devdoc>
        private sealed class TableItem {
            private DesignerDataTableBase _designerDataTable;
 
            public TableItem(DesignerDataTableBase designerDataTable) {
                Debug.Assert(designerDataTable != null); 
                _designerDataTable = designerDataTable; 
            }
 
            public DesignerDataTableBase DesignerDataTable {
                get {
                    return _designerDataTable;
                } 
            }
 
            public override string ToString() { 
                return _designerDataTable.Name;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
