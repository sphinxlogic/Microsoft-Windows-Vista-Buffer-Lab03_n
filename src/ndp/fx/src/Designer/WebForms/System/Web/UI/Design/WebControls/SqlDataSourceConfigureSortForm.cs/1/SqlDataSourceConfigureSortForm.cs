//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConfigureSortForm.cs" company="Microsoft">
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
    /// Form for building a select command sort clause for a SqlDataSource.
    /// </devdoc>
    internal class SqlDataSourceConfigureSortForm : DesignerForm { 
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.TextBox _previewTextBox; 
        private System.Windows.Forms.Label _previewLabel; 
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton; 
        private AutoSizeComboBox _fieldComboBox1;
        private AutoSizeComboBox _fieldComboBox2;
        private AutoSizeComboBox _fieldComboBox3;
        private System.Windows.Forms.RadioButton _sortAscendingRadioButton1; 
        private System.Windows.Forms.RadioButton _sortDescendingRadioButton1;
        private System.Windows.Forms.Panel _sortDirectionPanel1; 
        private System.Windows.Forms.RadioButton _sortDescendingRadioButton2; 
        private System.Windows.Forms.RadioButton _sortAscendingRadioButton2;
        private System.Windows.Forms.Panel _sortDirectionPanel2; 
        private System.Windows.Forms.RadioButton _sortDescendingRadioButton3;
        private System.Windows.Forms.RadioButton _sortAscendingRadioButton3;
        private System.Windows.Forms.Panel _sortDirectionPanel3;
        private System.Windows.Forms.GroupBox _sortByGroupBox1; 
        private System.Windows.Forms.GroupBox _sortByGroupBox2;
        private System.Windows.Forms.GroupBox _sortByGroupBox3; 
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
        private SqlDataSourceTableQuery _tableQuery; 

        private bool _loadingClauses;

 
        /// <devdoc>
        /// Creates a new SqlDataSourceConfigureSortForm. 
        /// </devdoc> 
        public SqlDataSourceConfigureSortForm(SqlDataSourceDesigner sqlDataSourceDesigner, SqlDataSourceTableQuery tableQuery) : base(sqlDataSourceDesigner.Component.Site) {
            Debug.Assert(sqlDataSourceDesigner != null); 
            Debug.Assert(tableQuery != null);
            Debug.Assert(tableQuery.OrderClauses.Count <= 3);

            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
            _tableQuery = tableQuery.Clone();
 
            InitializeComponent(); 
            InitializeUI();
 
            Cursor originalCursor = Cursor.Current;
            try {
                Cursor.Current = Cursors.WaitCursor;
 
                _loadingClauses = true;
 
                // Populate field list 
                _fieldComboBox1.Items.Add(new ColumnItem(null));
                _fieldComboBox2.Items.Add(new ColumnItem(null)); 
                _fieldComboBox3.Items.Add(new ColumnItem(null));
                foreach (DesignerDataColumn designerDataColumn in _tableQuery.DesignerDataTable.Columns) {
                    //
                    _fieldComboBox1.Items.Add(new ColumnItem(designerDataColumn)); 
                    _fieldComboBox2.Items.Add(new ColumnItem(designerDataColumn));
                    _fieldComboBox3.Items.Add(new ColumnItem(designerDataColumn)); 
                } 
                _fieldComboBox1.InvalidateDropDownWidth();
                _fieldComboBox2.InvalidateDropDownWidth(); 
                _fieldComboBox3.InvalidateDropDownWidth();

                _sortByGroupBox2.Enabled = false;
                _sortByGroupBox3.Enabled = false; 
                _sortDirectionPanel1.Enabled = false;
                _sortDirectionPanel2.Enabled = false; 
                _sortDirectionPanel3.Enabled = false; 
                _sortAscendingRadioButton1.Checked = true;
                _sortAscendingRadioButton2.Checked = true; 
                _sortAscendingRadioButton3.Checked = true;

                // Populate UI with existing order clauses
                if (_tableQuery.OrderClauses.Count >= 1) { 
                    SqlDataSourceOrderClause orderClause1 = (SqlDataSourceOrderClause)_tableQuery.OrderClauses[0];
                    SelectFieldItem(_fieldComboBox1, orderClause1.DesignerDataColumn); 
                    _sortAscendingRadioButton1.Checked = !orderClause1.IsDescending; 
                    _sortDescendingRadioButton1.Checked = orderClause1.IsDescending;
 
                    if (_tableQuery.OrderClauses.Count >= 2) {
                        SqlDataSourceOrderClause orderClause2 = (SqlDataSourceOrderClause)_tableQuery.OrderClauses[1];
                        SelectFieldItem(_fieldComboBox2, orderClause2.DesignerDataColumn);
                        _sortAscendingRadioButton2.Checked = !orderClause2.IsDescending; 
                        _sortDescendingRadioButton2.Checked = orderClause2.IsDescending;
 
                        if (_tableQuery.OrderClauses.Count >= 3) { 
                            SqlDataSourceOrderClause orderClause3 = (SqlDataSourceOrderClause)_tableQuery.OrderClauses[2];
                            SelectFieldItem(_fieldComboBox3, orderClause3.DesignerDataColumn); 
                            _sortAscendingRadioButton3.Checked = !orderClause3.IsDescending;
                            _sortDescendingRadioButton3.Checked = orderClause3.IsDescending;
                        }
                    } 
                }
 
                _loadingClauses = false; 

                // Update UI 
                UpdateOrderClauses();
                UpdatePreview();
            }
            finally { 
                Cursor.Current = originalCursor;
            } 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.SqlDataSource.ConfigureSort";
            }
        } 

        /// <devdoc> 
        /// Gets the list of sort clauses created in the form. 
        /// </devdoc>
        public System.Collections.Generic.IList<SqlDataSourceOrderClause> OrderClauses { 
            get {
                return _tableQuery.OrderClauses;
            }
        } 

        #region Designer generated code 
        private void InitializeComponent() { 
            this._helpLabel = new System.Windows.Forms.Label();
            this._previewLabel = new System.Windows.Forms.Label(); 
            this._previewTextBox = new System.Windows.Forms.TextBox();
            this._sortAscendingRadioButton1 = new System.Windows.Forms.RadioButton();
            this._sortDescendingRadioButton1 = new System.Windows.Forms.RadioButton();
            this._sortDirectionPanel1 = new System.Windows.Forms.Panel(); 
            this._fieldComboBox1 = new AutoSizeComboBox();
            this._okButton = new System.Windows.Forms.Button(); 
            this._cancelButton = new System.Windows.Forms.Button(); 
            this._sortDescendingRadioButton2 = new System.Windows.Forms.RadioButton();
            this._sortAscendingRadioButton2 = new System.Windows.Forms.RadioButton(); 
            this._fieldComboBox2 = new AutoSizeComboBox();
            this._sortDirectionPanel2 = new System.Windows.Forms.Panel();
            this._sortDescendingRadioButton3 = new System.Windows.Forms.RadioButton();
            this._sortAscendingRadioButton3 = new System.Windows.Forms.RadioButton(); 
            this._fieldComboBox3 = new AutoSizeComboBox();
            this._sortDirectionPanel3 = new System.Windows.Forms.Panel(); 
            this._sortByGroupBox1 = new System.Windows.Forms.GroupBox(); 
            this._sortByGroupBox2 = new System.Windows.Forms.GroupBox();
            this._sortByGroupBox3 = new System.Windows.Forms.GroupBox(); 
            this._sortDirectionPanel1.SuspendLayout();
            this._sortDirectionPanel2.SuspendLayout();
            this._sortDirectionPanel3.SuspendLayout();
            this._sortByGroupBox1.SuspendLayout(); 
            this._sortByGroupBox2.SuspendLayout();
            this._sortByGroupBox3.SuspendLayout(); 
            this.SuspendLayout(); 
            //
            // _helpLabel 
            //
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(12, 12); 
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(382, 16); 
            this._helpLabel.TabIndex = 10; 
            //
            // _sortAscendingRadioButton1 
            //
            this._sortAscendingRadioButton1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sortAscendingRadioButton1.Location = new System.Drawing.Point(0, 0); 
            this._sortAscendingRadioButton1.Name = "_sortAscendingRadioButton1";
            this._sortAscendingRadioButton1.Size = new System.Drawing.Size(200, 18); 
            this._sortAscendingRadioButton1.TabIndex = 10; 
            this._sortAscendingRadioButton1.CheckedChanged += new System.EventHandler(this.OnSortAscendingRadioButton1CheckedChanged);
            // 
            // _sortDescendingRadioButton1
            //
            this._sortDescendingRadioButton1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortDescendingRadioButton1.Location = new System.Drawing.Point(0, 18);
            this._sortDescendingRadioButton1.Name = "_sortDescendingRadioButton1"; 
            this._sortDescendingRadioButton1.Size = new System.Drawing.Size(200, 18); 
            this._sortDescendingRadioButton1.TabIndex = 20;
            // 
            // _sortDirectionPanel1
            //
            this._sortDirectionPanel1.Controls.Add(this._sortDescendingRadioButton1);
            this._sortDirectionPanel1.Controls.Add(this._sortAscendingRadioButton1); 
            this._sortDirectionPanel1.Location = new System.Drawing.Point(169, 12);
            this._sortDirectionPanel1.Name = "_sortDirectionPanel1"; 
            this._sortDirectionPanel1.Size = new System.Drawing.Size(200, 38); 
            this._sortDirectionPanel1.TabIndex = 20;
            // 
            // _fieldComboBox1
            //
            this._fieldComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._fieldComboBox1.Location = new System.Drawing.Point(9, 20); 
            this._fieldComboBox1.Name = "_fieldComboBox1";
            this._fieldComboBox1.Size = new System.Drawing.Size(153, 21); 
            this._fieldComboBox1.Sorted = true; 
            this._fieldComboBox1.TabIndex = 10;
            this._fieldComboBox1.SelectedIndexChanged += new System.EventHandler(this.OnFieldComboBox1SelectedIndexChanged); 
            //
            // _sortDescendingRadioButton2
            //
            this._sortDescendingRadioButton2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sortDescendingRadioButton2.Location = new System.Drawing.Point(0, 18); 
            this._sortDescendingRadioButton2.Name = "_sortDescendingRadioButton2"; 
            this._sortDescendingRadioButton2.Size = new System.Drawing.Size(200, 18);
            this._sortDescendingRadioButton2.TabIndex = 20; 
            //
            // _sortAscendingRadioButton2
            //
            this._sortAscendingRadioButton2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sortAscendingRadioButton2.Location = new System.Drawing.Point(0, 0); 
            this._sortAscendingRadioButton2.Name = "_sortAscendingRadioButton2"; 
            this._sortAscendingRadioButton2.Size = new System.Drawing.Size(200, 18);
            this._sortAscendingRadioButton2.TabIndex = 10; 
            this._sortAscendingRadioButton2.CheckedChanged += new System.EventHandler(this.OnSortAscendingRadioButton2CheckedChanged);
            //
            // _fieldComboBox2
            // 
            this._fieldComboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._fieldComboBox2.Location = new System.Drawing.Point(9, 20); 
            this._fieldComboBox2.Name = "_fieldComboBox2"; 
            this._fieldComboBox2.Size = new System.Drawing.Size(153, 21);
            this._fieldComboBox2.Sorted = true; 
            this._fieldComboBox2.TabIndex = 10;
            this._fieldComboBox2.SelectedIndexChanged += new System.EventHandler(this.OnFieldComboBox2SelectedIndexChanged);
            //
            // _sortDirectionPanel2 
            //
            this._sortDirectionPanel2.Controls.Add(this._sortDescendingRadioButton2); 
            this._sortDirectionPanel2.Controls.Add(this._sortAscendingRadioButton2); 
            this._sortDirectionPanel2.Location = new System.Drawing.Point(169, 12);
            this._sortDirectionPanel2.Name = "_sortDirectionPanel2"; 
            this._sortDirectionPanel2.Size = new System.Drawing.Size(200, 38);
            this._sortDirectionPanel2.TabIndex = 20;
            //
            // _sortDescendingRadioButton3 
            //
            this._sortDescendingRadioButton3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortDescendingRadioButton3.Location = new System.Drawing.Point(0, 18);
            this._sortDescendingRadioButton3.Name = "_sortDescendingRadioButton3"; 
            this._sortDescendingRadioButton3.Size = new System.Drawing.Size(200, 18);
            this._sortDescendingRadioButton3.TabIndex = 20;
            //
            // _sortAscendingRadioButton3 
            //
            this._sortAscendingRadioButton3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortAscendingRadioButton3.Location = new System.Drawing.Point(0, 0);
            this._sortAscendingRadioButton3.Name = "_sortAscendingRadioButton3"; 
            this._sortAscendingRadioButton3.Size = new System.Drawing.Size(200, 18);
            this._sortAscendingRadioButton3.TabIndex = 10;
            this._sortAscendingRadioButton3.CheckedChanged += new System.EventHandler(this.OnSortAscendingRadioButton3CheckedChanged);
            // 
            // _fieldComboBox3
            // 
            this._fieldComboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._fieldComboBox3.Location = new System.Drawing.Point(9, 20);
            this._fieldComboBox3.Name = "_fieldComboBox3"; 
            this._fieldComboBox3.Size = new System.Drawing.Size(153, 21);
            this._fieldComboBox3.Sorted = true;
            this._fieldComboBox3.TabIndex = 10;
            this._fieldComboBox3.SelectedIndexChanged += new System.EventHandler(this.OnFieldComboBox3SelectedIndexChanged); 
            //
            // _sortDirectionPanel3 
            // 
            this._sortDirectionPanel3.Controls.Add(this._sortDescendingRadioButton3);
            this._sortDirectionPanel3.Controls.Add(this._sortAscendingRadioButton3); 
            this._sortDirectionPanel3.Location = new System.Drawing.Point(169, 12);
            this._sortDirectionPanel3.Name = "_sortDirectionPanel3";
            this._sortDirectionPanel3.Size = new System.Drawing.Size(200, 38);
            this._sortDirectionPanel3.TabIndex = 20; 
            //
            // _sortByGroupBox1 
            // 
            this._sortByGroupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortByGroupBox1.Controls.Add(this._fieldComboBox1);
            this._sortByGroupBox1.Controls.Add(this._sortDirectionPanel1);
            this._sortByGroupBox1.Location = new System.Drawing.Point(12, 33);
            this._sortByGroupBox1.Name = "_sortByGroupBox1"; 
            this._sortByGroupBox1.Size = new System.Drawing.Size(384, 56);
            this._sortByGroupBox1.TabIndex = 20; 
            this._sortByGroupBox1.TabStop = false; 
            //
            // _sortByGroupBox2 
            //
            this._sortByGroupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sortByGroupBox2.Controls.Add(this._fieldComboBox2); 
            this._sortByGroupBox2.Controls.Add(this._sortDirectionPanel2);
            this._sortByGroupBox2.Location = new System.Drawing.Point(12, 95); 
            this._sortByGroupBox2.Name = "_sortByGroupBox2"; 
            this._sortByGroupBox2.Size = new System.Drawing.Size(384, 56);
            this._sortByGroupBox2.TabIndex = 30; 
            this._sortByGroupBox2.TabStop = false;
            //
            // _sortByGroupBox3
            // 
            this._sortByGroupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortByGroupBox3.Controls.Add(this._fieldComboBox3); 
            this._sortByGroupBox3.Controls.Add(this._sortDirectionPanel3);
            this._sortByGroupBox3.Location = new System.Drawing.Point(12, 157); 
            this._sortByGroupBox3.Name = "_sortByGroupBox3";
            this._sortByGroupBox3.Size = new System.Drawing.Size(384, 56);
            this._sortByGroupBox3.TabIndex = 40;
            this._sortByGroupBox3.TabStop = false; 
            //
            // _previewLabel 
            // 
            this._previewLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._previewLabel.Location = new System.Drawing.Point(12, 219);
            this._previewLabel.Name = "_previewLabel";
            this._previewLabel.Size = new System.Drawing.Size(384, 13);
            this._previewLabel.TabIndex = 50; 
            //
            // _previewTextBox 
            // 
            this._previewTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._previewTextBox.BackColor = System.Drawing.SystemColors.Control;
            this._previewTextBox.Location = new System.Drawing.Point(12, 237);
            this._previewTextBox.Multiline = true; 
            this._previewTextBox.Name = "_previewTextBox";
            this._previewTextBox.ReadOnly = true; 
            this._previewTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; 
            this._previewTextBox.Size = new System.Drawing.Size(384, 72);
            this._previewTextBox.TabIndex = 60; 
            this._previewTextBox.Text = "";
            //
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.Location = new System.Drawing.Point(240, 321); 
            this._okButton.Name = "_okButton"; 
            this._okButton.TabIndex = 70;
            this._okButton.Click += new System.EventHandler(this.OnOkButtonClick); 
            //
            // _cancelButton
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(321, 321); 
            this._cancelButton.Name = "_cancelButton"; 
            this._cancelButton.TabIndex = 80;
            this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick); 
            //
            // SqlDataSourceConfigureSortForm
            //
            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(408, 356); 
            this.Controls.Add(this._sortByGroupBox2); 
            this.Controls.Add(this._sortByGroupBox3);
            this.Controls.Add(this._sortByGroupBox1); 
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._previewTextBox);
            this.Controls.Add(this._previewLabel); 
            this.Controls.Add(this._helpLabel);
            this.MinimumSize = new System.Drawing.Size(416, 390); 
            this.Name = "SqlDataSourceConfigureSortForm"; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this._sortDirectionPanel1.ResumeLayout(false); 
            this._sortDirectionPanel2.ResumeLayout(false);
            this._sortDirectionPanel3.ResumeLayout(false);
            this._sortByGroupBox1.ResumeLayout(false);
            this._sortByGroupBox2.ResumeLayout(false); 
            this._sortByGroupBox3.ResumeLayout(false);
 
            InitializeForm(); 

            this.ResumeLayout(false); 
        }
        #endregion

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc> 
        private void InitializeUI() {
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_HelpLabel); 
            _previewLabel.Text = SR.GetString(SR.SqlDataSource_General_PreviewLabel);

            _sortByGroupBox1.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortByLabel);
            _sortByGroupBox2.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_ThenByLabel); 
            _sortByGroupBox3.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_ThenByLabel);
 
            _sortAscendingRadioButton1.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_AscendingLabel); 
            _sortDescendingRadioButton1.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_DescendingLabel);
            _sortAscendingRadioButton2.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_AscendingLabel); 
            _sortDescendingRadioButton2.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_DescendingLabel);
            _sortAscendingRadioButton3.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_AscendingLabel);
            _sortDescendingRadioButton3.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_DescendingLabel);
 
            // Accessibility strings
            _sortAscendingRadioButton1.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection1); 
            _sortDescendingRadioButton1.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection1); 
            _sortAscendingRadioButton2.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection2);
            _sortDescendingRadioButton2.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection2); 
            _sortAscendingRadioButton3.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection3);
            _sortDescendingRadioButton3.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection3);
            _fieldComboBox1.AccessibleName = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortColumn1);
            _fieldComboBox2.AccessibleName = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortColumn2); 
            _fieldComboBox3.AccessibleName = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortColumn3);
 
            _okButton.Text = SR.GetString(SR.OK); 
            _cancelButton.Text = SR.GetString(SR.Cancel);
            Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_Caption); 
        }

        /// <devdoc>
        /// </devdoc> 
        private void OnCancelButtonClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.Cancel; 
            Close(); 
        }
 
        /// <devdoc>
        /// </devdoc>
        private void OnFieldComboBox1SelectedIndexChanged(object sender, System.EventArgs e) {
            if ((_fieldComboBox1.SelectedIndex == -1) || 
                ((_fieldComboBox1.SelectedIndex == 0) && (((ColumnItem)_fieldComboBox1.Items[0]).DesignerDataColumn == null))) {
                // De-selecting a sort column 
 
                // Disable UI for further sorting of this field
                _sortDirectionPanel1.Enabled = false; 
                _sortAscendingRadioButton1.Checked = true;

                // Disable UI for further sorting of next fields
                _fieldComboBox2.SelectedIndex = -1; 
                _sortAscendingRadioButton2.Checked = true;
                _sortByGroupBox2.Enabled = false; 
                _fieldComboBox2.Enabled = false; 
            }
            else { 
                // Selecting a sort column

                _sortDirectionPanel1.Enabled = true;
                _sortByGroupBox2.Enabled = true; 
                _fieldComboBox2.Enabled = true;
            } 
 
            UpdateOrderClauses();
            UpdatePreview(); 
        }

        /// <devdoc>
        /// </devdoc> 
        private void OnFieldComboBox2SelectedIndexChanged(object sender, System.EventArgs e) {
            if ((_fieldComboBox2.SelectedIndex == -1) || 
                ((_fieldComboBox2.SelectedIndex == 0) && (((ColumnItem)_fieldComboBox2.Items[0]).DesignerDataColumn == null))) { 
                // De-selecting a sort column
 
                // Disable UI for further sorting of this field
                _sortDirectionPanel2.Enabled = false;
                _sortAscendingRadioButton2.Checked = true;
 
                // Disable UI for further sorting of next field
                _fieldComboBox3.SelectedIndex = -1; 
                _sortAscendingRadioButton3.Checked = true; 
                _sortByGroupBox3.Enabled = false;
                _fieldComboBox3.Enabled = false; 
            }
            else {
                // Selecting a sort column
 
                _sortDirectionPanel2.Enabled = true;
                _sortByGroupBox3.Enabled = true; 
                _fieldComboBox3.Enabled = true; 
            }
 
            UpdateOrderClauses();
            UpdatePreview();
        }
 
        /// <devdoc>
        /// </devdoc> 
        private void OnFieldComboBox3SelectedIndexChanged(object sender, System.EventArgs e) { 
            if ((_fieldComboBox3.SelectedIndex == -1) ||
                ((_fieldComboBox3.SelectedIndex == 0) && (((ColumnItem)_fieldComboBox3.Items[0]).DesignerDataColumn == null))) { 
                // De-selecting a sort column

                // Disable UI for further sorting of this field
                _sortDirectionPanel3.Enabled = false; 
                _sortAscendingRadioButton3.Checked = true;
            } 
            else { 
                // Selecting a sort column
 
                _sortDirectionPanel3.Enabled = true;
            }

            UpdateOrderClauses(); 
            UpdatePreview();
        } 
 
        /// <devdoc>
        /// </devdoc> 
        private void OnOkButtonClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.OK;
            Close();
        } 

        /// <devdoc> 
        /// </devdoc> 
        private void OnSortAscendingRadioButton1CheckedChanged(object sender, System.EventArgs e) {
            UpdateOrderClauses(); 
            UpdatePreview();
        }

        /// <devdoc> 
        /// </devdoc>
        private void OnSortAscendingRadioButton2CheckedChanged(object sender, System.EventArgs e) { 
            UpdateOrderClauses(); 
            UpdatePreview();
        } 

        /// <devdoc>
        /// </devdoc>
        private void OnSortAscendingRadioButton3CheckedChanged(object sender, System.EventArgs e) { 
            UpdateOrderClauses();
            UpdatePreview(); 
        } 

        /// <devdoc> 
        /// Selects a specified column in a combobox.
        /// </devdoc>
        private void SelectFieldItem(ComboBox comboBox, DesignerDataColumn field) {
            foreach (ColumnItem columnItem in comboBox.Items) { 
                if (columnItem.DesignerDataColumn == field) {
                    comboBox.SelectedItem = columnItem; 
                    return; 
                }
            } 
            Debug.Fail("Could not find field " + field.Name + " in column list!");
        }

        /// <devdoc> 
        /// Updates the order clauses when selections change
        /// </devdoc> 
        private void UpdateOrderClauses() { 
            if (_loadingClauses) {
                return; 
            }

            _tableQuery.OrderClauses.Clear();
 
            if (_fieldComboBox1.SelectedIndex >= 1) {
                SqlDataSourceOrderClause orderClause1 = new SqlDataSourceOrderClause( 
                    _tableQuery.DesignerDataConnection, 
                    _tableQuery.DesignerDataTable,
                    ((ColumnItem)_fieldComboBox1.SelectedItem).DesignerDataColumn, 
                    !_sortAscendingRadioButton1.Checked);
                _tableQuery.OrderClauses.Add(orderClause1);
            }
 
            if (_fieldComboBox2.SelectedIndex >= 1) {
                SqlDataSourceOrderClause orderClause2 = new SqlDataSourceOrderClause( 
                    _tableQuery.DesignerDataConnection, 
                    _tableQuery.DesignerDataTable,
                    ((ColumnItem)_fieldComboBox2.SelectedItem).DesignerDataColumn, 
                    !_sortAscendingRadioButton2.Checked);
                _tableQuery.OrderClauses.Add(orderClause2);
            }
 
            if (_fieldComboBox3.SelectedIndex >= 1) {
                SqlDataSourceOrderClause orderClause3 = new SqlDataSourceOrderClause( 
                    _tableQuery.DesignerDataConnection, 
                    _tableQuery.DesignerDataTable,
                    ((ColumnItem)_fieldComboBox3.SelectedItem).DesignerDataColumn, 
                    !_sortAscendingRadioButton3.Checked);
                _tableQuery.OrderClauses.Add(orderClause3);
            }
        } 

        /// <devdoc> 
        /// Updates the SQL preview textbox with the current query. 
        /// </devdoc>
        private void UpdatePreview() { 
            SqlDataSourceQuery selectQuery = _tableQuery.GetSelectQuery();
            _previewTextBox.Text = (selectQuery == null ? String.Empty : selectQuery.Command);
        }
 
        /// <devdoc>
        /// Represents a column a user can select to filter by. 
        /// </devdoc> 
        private sealed class ColumnItem {
            private DesignerDataColumn _designerDataColumn; 

            public ColumnItem(DesignerDataColumn designerDataColumn) {
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
                    return SR.GetString(SR.SqlDataSourceConfigureSortForm_SortNone);
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConfigureSortForm.cs" company="Microsoft">
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
    /// Form for building a select command sort clause for a SqlDataSource.
    /// </devdoc>
    internal class SqlDataSourceConfigureSortForm : DesignerForm { 
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.TextBox _previewTextBox; 
        private System.Windows.Forms.Label _previewLabel; 
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton; 
        private AutoSizeComboBox _fieldComboBox1;
        private AutoSizeComboBox _fieldComboBox2;
        private AutoSizeComboBox _fieldComboBox3;
        private System.Windows.Forms.RadioButton _sortAscendingRadioButton1; 
        private System.Windows.Forms.RadioButton _sortDescendingRadioButton1;
        private System.Windows.Forms.Panel _sortDirectionPanel1; 
        private System.Windows.Forms.RadioButton _sortDescendingRadioButton2; 
        private System.Windows.Forms.RadioButton _sortAscendingRadioButton2;
        private System.Windows.Forms.Panel _sortDirectionPanel2; 
        private System.Windows.Forms.RadioButton _sortDescendingRadioButton3;
        private System.Windows.Forms.RadioButton _sortAscendingRadioButton3;
        private System.Windows.Forms.Panel _sortDirectionPanel3;
        private System.Windows.Forms.GroupBox _sortByGroupBox1; 
        private System.Windows.Forms.GroupBox _sortByGroupBox2;
        private System.Windows.Forms.GroupBox _sortByGroupBox3; 
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
        private SqlDataSourceTableQuery _tableQuery; 

        private bool _loadingClauses;

 
        /// <devdoc>
        /// Creates a new SqlDataSourceConfigureSortForm. 
        /// </devdoc> 
        public SqlDataSourceConfigureSortForm(SqlDataSourceDesigner sqlDataSourceDesigner, SqlDataSourceTableQuery tableQuery) : base(sqlDataSourceDesigner.Component.Site) {
            Debug.Assert(sqlDataSourceDesigner != null); 
            Debug.Assert(tableQuery != null);
            Debug.Assert(tableQuery.OrderClauses.Count <= 3);

            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
            _tableQuery = tableQuery.Clone();
 
            InitializeComponent(); 
            InitializeUI();
 
            Cursor originalCursor = Cursor.Current;
            try {
                Cursor.Current = Cursors.WaitCursor;
 
                _loadingClauses = true;
 
                // Populate field list 
                _fieldComboBox1.Items.Add(new ColumnItem(null));
                _fieldComboBox2.Items.Add(new ColumnItem(null)); 
                _fieldComboBox3.Items.Add(new ColumnItem(null));
                foreach (DesignerDataColumn designerDataColumn in _tableQuery.DesignerDataTable.Columns) {
                    //
                    _fieldComboBox1.Items.Add(new ColumnItem(designerDataColumn)); 
                    _fieldComboBox2.Items.Add(new ColumnItem(designerDataColumn));
                    _fieldComboBox3.Items.Add(new ColumnItem(designerDataColumn)); 
                } 
                _fieldComboBox1.InvalidateDropDownWidth();
                _fieldComboBox2.InvalidateDropDownWidth(); 
                _fieldComboBox3.InvalidateDropDownWidth();

                _sortByGroupBox2.Enabled = false;
                _sortByGroupBox3.Enabled = false; 
                _sortDirectionPanel1.Enabled = false;
                _sortDirectionPanel2.Enabled = false; 
                _sortDirectionPanel3.Enabled = false; 
                _sortAscendingRadioButton1.Checked = true;
                _sortAscendingRadioButton2.Checked = true; 
                _sortAscendingRadioButton3.Checked = true;

                // Populate UI with existing order clauses
                if (_tableQuery.OrderClauses.Count >= 1) { 
                    SqlDataSourceOrderClause orderClause1 = (SqlDataSourceOrderClause)_tableQuery.OrderClauses[0];
                    SelectFieldItem(_fieldComboBox1, orderClause1.DesignerDataColumn); 
                    _sortAscendingRadioButton1.Checked = !orderClause1.IsDescending; 
                    _sortDescendingRadioButton1.Checked = orderClause1.IsDescending;
 
                    if (_tableQuery.OrderClauses.Count >= 2) {
                        SqlDataSourceOrderClause orderClause2 = (SqlDataSourceOrderClause)_tableQuery.OrderClauses[1];
                        SelectFieldItem(_fieldComboBox2, orderClause2.DesignerDataColumn);
                        _sortAscendingRadioButton2.Checked = !orderClause2.IsDescending; 
                        _sortDescendingRadioButton2.Checked = orderClause2.IsDescending;
 
                        if (_tableQuery.OrderClauses.Count >= 3) { 
                            SqlDataSourceOrderClause orderClause3 = (SqlDataSourceOrderClause)_tableQuery.OrderClauses[2];
                            SelectFieldItem(_fieldComboBox3, orderClause3.DesignerDataColumn); 
                            _sortAscendingRadioButton3.Checked = !orderClause3.IsDescending;
                            _sortDescendingRadioButton3.Checked = orderClause3.IsDescending;
                        }
                    } 
                }
 
                _loadingClauses = false; 

                // Update UI 
                UpdateOrderClauses();
                UpdatePreview();
            }
            finally { 
                Cursor.Current = originalCursor;
            } 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.SqlDataSource.ConfigureSort";
            }
        } 

        /// <devdoc> 
        /// Gets the list of sort clauses created in the form. 
        /// </devdoc>
        public System.Collections.Generic.IList<SqlDataSourceOrderClause> OrderClauses { 
            get {
                return _tableQuery.OrderClauses;
            }
        } 

        #region Designer generated code 
        private void InitializeComponent() { 
            this._helpLabel = new System.Windows.Forms.Label();
            this._previewLabel = new System.Windows.Forms.Label(); 
            this._previewTextBox = new System.Windows.Forms.TextBox();
            this._sortAscendingRadioButton1 = new System.Windows.Forms.RadioButton();
            this._sortDescendingRadioButton1 = new System.Windows.Forms.RadioButton();
            this._sortDirectionPanel1 = new System.Windows.Forms.Panel(); 
            this._fieldComboBox1 = new AutoSizeComboBox();
            this._okButton = new System.Windows.Forms.Button(); 
            this._cancelButton = new System.Windows.Forms.Button(); 
            this._sortDescendingRadioButton2 = new System.Windows.Forms.RadioButton();
            this._sortAscendingRadioButton2 = new System.Windows.Forms.RadioButton(); 
            this._fieldComboBox2 = new AutoSizeComboBox();
            this._sortDirectionPanel2 = new System.Windows.Forms.Panel();
            this._sortDescendingRadioButton3 = new System.Windows.Forms.RadioButton();
            this._sortAscendingRadioButton3 = new System.Windows.Forms.RadioButton(); 
            this._fieldComboBox3 = new AutoSizeComboBox();
            this._sortDirectionPanel3 = new System.Windows.Forms.Panel(); 
            this._sortByGroupBox1 = new System.Windows.Forms.GroupBox(); 
            this._sortByGroupBox2 = new System.Windows.Forms.GroupBox();
            this._sortByGroupBox3 = new System.Windows.Forms.GroupBox(); 
            this._sortDirectionPanel1.SuspendLayout();
            this._sortDirectionPanel2.SuspendLayout();
            this._sortDirectionPanel3.SuspendLayout();
            this._sortByGroupBox1.SuspendLayout(); 
            this._sortByGroupBox2.SuspendLayout();
            this._sortByGroupBox3.SuspendLayout(); 
            this.SuspendLayout(); 
            //
            // _helpLabel 
            //
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(12, 12); 
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(382, 16); 
            this._helpLabel.TabIndex = 10; 
            //
            // _sortAscendingRadioButton1 
            //
            this._sortAscendingRadioButton1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sortAscendingRadioButton1.Location = new System.Drawing.Point(0, 0); 
            this._sortAscendingRadioButton1.Name = "_sortAscendingRadioButton1";
            this._sortAscendingRadioButton1.Size = new System.Drawing.Size(200, 18); 
            this._sortAscendingRadioButton1.TabIndex = 10; 
            this._sortAscendingRadioButton1.CheckedChanged += new System.EventHandler(this.OnSortAscendingRadioButton1CheckedChanged);
            // 
            // _sortDescendingRadioButton1
            //
            this._sortDescendingRadioButton1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortDescendingRadioButton1.Location = new System.Drawing.Point(0, 18);
            this._sortDescendingRadioButton1.Name = "_sortDescendingRadioButton1"; 
            this._sortDescendingRadioButton1.Size = new System.Drawing.Size(200, 18); 
            this._sortDescendingRadioButton1.TabIndex = 20;
            // 
            // _sortDirectionPanel1
            //
            this._sortDirectionPanel1.Controls.Add(this._sortDescendingRadioButton1);
            this._sortDirectionPanel1.Controls.Add(this._sortAscendingRadioButton1); 
            this._sortDirectionPanel1.Location = new System.Drawing.Point(169, 12);
            this._sortDirectionPanel1.Name = "_sortDirectionPanel1"; 
            this._sortDirectionPanel1.Size = new System.Drawing.Size(200, 38); 
            this._sortDirectionPanel1.TabIndex = 20;
            // 
            // _fieldComboBox1
            //
            this._fieldComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._fieldComboBox1.Location = new System.Drawing.Point(9, 20); 
            this._fieldComboBox1.Name = "_fieldComboBox1";
            this._fieldComboBox1.Size = new System.Drawing.Size(153, 21); 
            this._fieldComboBox1.Sorted = true; 
            this._fieldComboBox1.TabIndex = 10;
            this._fieldComboBox1.SelectedIndexChanged += new System.EventHandler(this.OnFieldComboBox1SelectedIndexChanged); 
            //
            // _sortDescendingRadioButton2
            //
            this._sortDescendingRadioButton2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sortDescendingRadioButton2.Location = new System.Drawing.Point(0, 18); 
            this._sortDescendingRadioButton2.Name = "_sortDescendingRadioButton2"; 
            this._sortDescendingRadioButton2.Size = new System.Drawing.Size(200, 18);
            this._sortDescendingRadioButton2.TabIndex = 20; 
            //
            // _sortAscendingRadioButton2
            //
            this._sortAscendingRadioButton2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sortAscendingRadioButton2.Location = new System.Drawing.Point(0, 0); 
            this._sortAscendingRadioButton2.Name = "_sortAscendingRadioButton2"; 
            this._sortAscendingRadioButton2.Size = new System.Drawing.Size(200, 18);
            this._sortAscendingRadioButton2.TabIndex = 10; 
            this._sortAscendingRadioButton2.CheckedChanged += new System.EventHandler(this.OnSortAscendingRadioButton2CheckedChanged);
            //
            // _fieldComboBox2
            // 
            this._fieldComboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._fieldComboBox2.Location = new System.Drawing.Point(9, 20); 
            this._fieldComboBox2.Name = "_fieldComboBox2"; 
            this._fieldComboBox2.Size = new System.Drawing.Size(153, 21);
            this._fieldComboBox2.Sorted = true; 
            this._fieldComboBox2.TabIndex = 10;
            this._fieldComboBox2.SelectedIndexChanged += new System.EventHandler(this.OnFieldComboBox2SelectedIndexChanged);
            //
            // _sortDirectionPanel2 
            //
            this._sortDirectionPanel2.Controls.Add(this._sortDescendingRadioButton2); 
            this._sortDirectionPanel2.Controls.Add(this._sortAscendingRadioButton2); 
            this._sortDirectionPanel2.Location = new System.Drawing.Point(169, 12);
            this._sortDirectionPanel2.Name = "_sortDirectionPanel2"; 
            this._sortDirectionPanel2.Size = new System.Drawing.Size(200, 38);
            this._sortDirectionPanel2.TabIndex = 20;
            //
            // _sortDescendingRadioButton3 
            //
            this._sortDescendingRadioButton3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortDescendingRadioButton3.Location = new System.Drawing.Point(0, 18);
            this._sortDescendingRadioButton3.Name = "_sortDescendingRadioButton3"; 
            this._sortDescendingRadioButton3.Size = new System.Drawing.Size(200, 18);
            this._sortDescendingRadioButton3.TabIndex = 20;
            //
            // _sortAscendingRadioButton3 
            //
            this._sortAscendingRadioButton3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortAscendingRadioButton3.Location = new System.Drawing.Point(0, 0);
            this._sortAscendingRadioButton3.Name = "_sortAscendingRadioButton3"; 
            this._sortAscendingRadioButton3.Size = new System.Drawing.Size(200, 18);
            this._sortAscendingRadioButton3.TabIndex = 10;
            this._sortAscendingRadioButton3.CheckedChanged += new System.EventHandler(this.OnSortAscendingRadioButton3CheckedChanged);
            // 
            // _fieldComboBox3
            // 
            this._fieldComboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._fieldComboBox3.Location = new System.Drawing.Point(9, 20);
            this._fieldComboBox3.Name = "_fieldComboBox3"; 
            this._fieldComboBox3.Size = new System.Drawing.Size(153, 21);
            this._fieldComboBox3.Sorted = true;
            this._fieldComboBox3.TabIndex = 10;
            this._fieldComboBox3.SelectedIndexChanged += new System.EventHandler(this.OnFieldComboBox3SelectedIndexChanged); 
            //
            // _sortDirectionPanel3 
            // 
            this._sortDirectionPanel3.Controls.Add(this._sortDescendingRadioButton3);
            this._sortDirectionPanel3.Controls.Add(this._sortAscendingRadioButton3); 
            this._sortDirectionPanel3.Location = new System.Drawing.Point(169, 12);
            this._sortDirectionPanel3.Name = "_sortDirectionPanel3";
            this._sortDirectionPanel3.Size = new System.Drawing.Size(200, 38);
            this._sortDirectionPanel3.TabIndex = 20; 
            //
            // _sortByGroupBox1 
            // 
            this._sortByGroupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortByGroupBox1.Controls.Add(this._fieldComboBox1);
            this._sortByGroupBox1.Controls.Add(this._sortDirectionPanel1);
            this._sortByGroupBox1.Location = new System.Drawing.Point(12, 33);
            this._sortByGroupBox1.Name = "_sortByGroupBox1"; 
            this._sortByGroupBox1.Size = new System.Drawing.Size(384, 56);
            this._sortByGroupBox1.TabIndex = 20; 
            this._sortByGroupBox1.TabStop = false; 
            //
            // _sortByGroupBox2 
            //
            this._sortByGroupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sortByGroupBox2.Controls.Add(this._fieldComboBox2); 
            this._sortByGroupBox2.Controls.Add(this._sortDirectionPanel2);
            this._sortByGroupBox2.Location = new System.Drawing.Point(12, 95); 
            this._sortByGroupBox2.Name = "_sortByGroupBox2"; 
            this._sortByGroupBox2.Size = new System.Drawing.Size(384, 56);
            this._sortByGroupBox2.TabIndex = 30; 
            this._sortByGroupBox2.TabStop = false;
            //
            // _sortByGroupBox3
            // 
            this._sortByGroupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sortByGroupBox3.Controls.Add(this._fieldComboBox3); 
            this._sortByGroupBox3.Controls.Add(this._sortDirectionPanel3);
            this._sortByGroupBox3.Location = new System.Drawing.Point(12, 157); 
            this._sortByGroupBox3.Name = "_sortByGroupBox3";
            this._sortByGroupBox3.Size = new System.Drawing.Size(384, 56);
            this._sortByGroupBox3.TabIndex = 40;
            this._sortByGroupBox3.TabStop = false; 
            //
            // _previewLabel 
            // 
            this._previewLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._previewLabel.Location = new System.Drawing.Point(12, 219);
            this._previewLabel.Name = "_previewLabel";
            this._previewLabel.Size = new System.Drawing.Size(384, 13);
            this._previewLabel.TabIndex = 50; 
            //
            // _previewTextBox 
            // 
            this._previewTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._previewTextBox.BackColor = System.Drawing.SystemColors.Control;
            this._previewTextBox.Location = new System.Drawing.Point(12, 237);
            this._previewTextBox.Multiline = true; 
            this._previewTextBox.Name = "_previewTextBox";
            this._previewTextBox.ReadOnly = true; 
            this._previewTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; 
            this._previewTextBox.Size = new System.Drawing.Size(384, 72);
            this._previewTextBox.TabIndex = 60; 
            this._previewTextBox.Text = "";
            //
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.Location = new System.Drawing.Point(240, 321); 
            this._okButton.Name = "_okButton"; 
            this._okButton.TabIndex = 70;
            this._okButton.Click += new System.EventHandler(this.OnOkButtonClick); 
            //
            // _cancelButton
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(321, 321); 
            this._cancelButton.Name = "_cancelButton"; 
            this._cancelButton.TabIndex = 80;
            this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick); 
            //
            // SqlDataSourceConfigureSortForm
            //
            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(408, 356); 
            this.Controls.Add(this._sortByGroupBox2); 
            this.Controls.Add(this._sortByGroupBox3);
            this.Controls.Add(this._sortByGroupBox1); 
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._previewTextBox);
            this.Controls.Add(this._previewLabel); 
            this.Controls.Add(this._helpLabel);
            this.MinimumSize = new System.Drawing.Size(416, 390); 
            this.Name = "SqlDataSourceConfigureSortForm"; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this._sortDirectionPanel1.ResumeLayout(false); 
            this._sortDirectionPanel2.ResumeLayout(false);
            this._sortDirectionPanel3.ResumeLayout(false);
            this._sortByGroupBox1.ResumeLayout(false);
            this._sortByGroupBox2.ResumeLayout(false); 
            this._sortByGroupBox3.ResumeLayout(false);
 
            InitializeForm(); 

            this.ResumeLayout(false); 
        }
        #endregion

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc> 
        private void InitializeUI() {
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_HelpLabel); 
            _previewLabel.Text = SR.GetString(SR.SqlDataSource_General_PreviewLabel);

            _sortByGroupBox1.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortByLabel);
            _sortByGroupBox2.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_ThenByLabel); 
            _sortByGroupBox3.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_ThenByLabel);
 
            _sortAscendingRadioButton1.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_AscendingLabel); 
            _sortDescendingRadioButton1.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_DescendingLabel);
            _sortAscendingRadioButton2.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_AscendingLabel); 
            _sortDescendingRadioButton2.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_DescendingLabel);
            _sortAscendingRadioButton3.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_AscendingLabel);
            _sortDescendingRadioButton3.Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_DescendingLabel);
 
            // Accessibility strings
            _sortAscendingRadioButton1.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection1); 
            _sortDescendingRadioButton1.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection1); 
            _sortAscendingRadioButton2.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection2);
            _sortDescendingRadioButton2.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection2); 
            _sortAscendingRadioButton3.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection3);
            _sortDescendingRadioButton3.AccessibleDescription = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortDirection3);
            _fieldComboBox1.AccessibleName = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortColumn1);
            _fieldComboBox2.AccessibleName = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortColumn2); 
            _fieldComboBox3.AccessibleName = SR.GetString(SR.SqlDataSourceConfigureSortForm_SortColumn3);
 
            _okButton.Text = SR.GetString(SR.OK); 
            _cancelButton.Text = SR.GetString(SR.Cancel);
            Text = SR.GetString(SR.SqlDataSourceConfigureSortForm_Caption); 
        }

        /// <devdoc>
        /// </devdoc> 
        private void OnCancelButtonClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.Cancel; 
            Close(); 
        }
 
        /// <devdoc>
        /// </devdoc>
        private void OnFieldComboBox1SelectedIndexChanged(object sender, System.EventArgs e) {
            if ((_fieldComboBox1.SelectedIndex == -1) || 
                ((_fieldComboBox1.SelectedIndex == 0) && (((ColumnItem)_fieldComboBox1.Items[0]).DesignerDataColumn == null))) {
                // De-selecting a sort column 
 
                // Disable UI for further sorting of this field
                _sortDirectionPanel1.Enabled = false; 
                _sortAscendingRadioButton1.Checked = true;

                // Disable UI for further sorting of next fields
                _fieldComboBox2.SelectedIndex = -1; 
                _sortAscendingRadioButton2.Checked = true;
                _sortByGroupBox2.Enabled = false; 
                _fieldComboBox2.Enabled = false; 
            }
            else { 
                // Selecting a sort column

                _sortDirectionPanel1.Enabled = true;
                _sortByGroupBox2.Enabled = true; 
                _fieldComboBox2.Enabled = true;
            } 
 
            UpdateOrderClauses();
            UpdatePreview(); 
        }

        /// <devdoc>
        /// </devdoc> 
        private void OnFieldComboBox2SelectedIndexChanged(object sender, System.EventArgs e) {
            if ((_fieldComboBox2.SelectedIndex == -1) || 
                ((_fieldComboBox2.SelectedIndex == 0) && (((ColumnItem)_fieldComboBox2.Items[0]).DesignerDataColumn == null))) { 
                // De-selecting a sort column
 
                // Disable UI for further sorting of this field
                _sortDirectionPanel2.Enabled = false;
                _sortAscendingRadioButton2.Checked = true;
 
                // Disable UI for further sorting of next field
                _fieldComboBox3.SelectedIndex = -1; 
                _sortAscendingRadioButton3.Checked = true; 
                _sortByGroupBox3.Enabled = false;
                _fieldComboBox3.Enabled = false; 
            }
            else {
                // Selecting a sort column
 
                _sortDirectionPanel2.Enabled = true;
                _sortByGroupBox3.Enabled = true; 
                _fieldComboBox3.Enabled = true; 
            }
 
            UpdateOrderClauses();
            UpdatePreview();
        }
 
        /// <devdoc>
        /// </devdoc> 
        private void OnFieldComboBox3SelectedIndexChanged(object sender, System.EventArgs e) { 
            if ((_fieldComboBox3.SelectedIndex == -1) ||
                ((_fieldComboBox3.SelectedIndex == 0) && (((ColumnItem)_fieldComboBox3.Items[0]).DesignerDataColumn == null))) { 
                // De-selecting a sort column

                // Disable UI for further sorting of this field
                _sortDirectionPanel3.Enabled = false; 
                _sortAscendingRadioButton3.Checked = true;
            } 
            else { 
                // Selecting a sort column
 
                _sortDirectionPanel3.Enabled = true;
            }

            UpdateOrderClauses(); 
            UpdatePreview();
        } 
 
        /// <devdoc>
        /// </devdoc> 
        private void OnOkButtonClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.OK;
            Close();
        } 

        /// <devdoc> 
        /// </devdoc> 
        private void OnSortAscendingRadioButton1CheckedChanged(object sender, System.EventArgs e) {
            UpdateOrderClauses(); 
            UpdatePreview();
        }

        /// <devdoc> 
        /// </devdoc>
        private void OnSortAscendingRadioButton2CheckedChanged(object sender, System.EventArgs e) { 
            UpdateOrderClauses(); 
            UpdatePreview();
        } 

        /// <devdoc>
        /// </devdoc>
        private void OnSortAscendingRadioButton3CheckedChanged(object sender, System.EventArgs e) { 
            UpdateOrderClauses();
            UpdatePreview(); 
        } 

        /// <devdoc> 
        /// Selects a specified column in a combobox.
        /// </devdoc>
        private void SelectFieldItem(ComboBox comboBox, DesignerDataColumn field) {
            foreach (ColumnItem columnItem in comboBox.Items) { 
                if (columnItem.DesignerDataColumn == field) {
                    comboBox.SelectedItem = columnItem; 
                    return; 
                }
            } 
            Debug.Fail("Could not find field " + field.Name + " in column list!");
        }

        /// <devdoc> 
        /// Updates the order clauses when selections change
        /// </devdoc> 
        private void UpdateOrderClauses() { 
            if (_loadingClauses) {
                return; 
            }

            _tableQuery.OrderClauses.Clear();
 
            if (_fieldComboBox1.SelectedIndex >= 1) {
                SqlDataSourceOrderClause orderClause1 = new SqlDataSourceOrderClause( 
                    _tableQuery.DesignerDataConnection, 
                    _tableQuery.DesignerDataTable,
                    ((ColumnItem)_fieldComboBox1.SelectedItem).DesignerDataColumn, 
                    !_sortAscendingRadioButton1.Checked);
                _tableQuery.OrderClauses.Add(orderClause1);
            }
 
            if (_fieldComboBox2.SelectedIndex >= 1) {
                SqlDataSourceOrderClause orderClause2 = new SqlDataSourceOrderClause( 
                    _tableQuery.DesignerDataConnection, 
                    _tableQuery.DesignerDataTable,
                    ((ColumnItem)_fieldComboBox2.SelectedItem).DesignerDataColumn, 
                    !_sortAscendingRadioButton2.Checked);
                _tableQuery.OrderClauses.Add(orderClause2);
            }
 
            if (_fieldComboBox3.SelectedIndex >= 1) {
                SqlDataSourceOrderClause orderClause3 = new SqlDataSourceOrderClause( 
                    _tableQuery.DesignerDataConnection, 
                    _tableQuery.DesignerDataTable,
                    ((ColumnItem)_fieldComboBox3.SelectedItem).DesignerDataColumn, 
                    !_sortAscendingRadioButton3.Checked);
                _tableQuery.OrderClauses.Add(orderClause3);
            }
        } 

        /// <devdoc> 
        /// Updates the SQL preview textbox with the current query. 
        /// </devdoc>
        private void UpdatePreview() { 
            SqlDataSourceQuery selectQuery = _tableQuery.GetSelectQuery();
            _previewTextBox.Text = (selectQuery == null ? String.Empty : selectQuery.Command);
        }
 
        /// <devdoc>
        /// Represents a column a user can select to filter by. 
        /// </devdoc> 
        private sealed class ColumnItem {
            private DesignerDataColumn _designerDataColumn; 

            public ColumnItem(DesignerDataColumn designerDataColumn) {
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
                    return SR.GetString(SR.SqlDataSourceConfigureSortForm_SortNone);
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
