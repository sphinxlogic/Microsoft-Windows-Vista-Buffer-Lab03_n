//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceRefreshSchemaForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Data;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Windows.Forms; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Collections.Generic;

    /// <devdoc>
    /// The SqlDataSource Refresh Schema form. This guides the user through 
    /// the process of gathering data in order to be able to refresh schema.
    /// </devdoc> 
    internal class SqlDataSourceRefreshSchemaForm : DesignerForm { 
        private System.Windows.Forms.TextBox _commandTextBox;
        private System.Windows.Forms.Label _helpLabel; 
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Label _commandLabel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.DataGridView _parametersDataGridView; 
        private System.Windows.Forms.Label _parametersLabel;
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner; 
        private SqlDataSource _sqlDataSource;
        private string _connectionString; 
        private string _providerName;
        private string _selectCommand;
        private SqlDataSourceCommandType _selectCommandType;
 

        public SqlDataSourceRefreshSchemaForm(IServiceProvider serviceProvider, SqlDataSourceDesigner sqlDataSourceDesigner, ParameterCollection parameters) 
            : base(serviceProvider) { 
            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 

            _sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
            _connectionString = _sqlDataSourceDesigner.ConnectionString;
            _providerName = _sqlDataSourceDesigner.ProviderName; 
            _selectCommand = _sqlDataSourceDesigner.SelectCommand;
            _selectCommandType = _sqlDataSource.SelectCommandType; 
 
            InitializeComponent();
            InitializeUI(); 


            // Populate TypeCode combobox
            Array typeCodes = Enum.GetValues(typeof(TypeCode)); 
            Array.Sort(typeCodes, new TypeCodeComparer());
            foreach (TypeCode typeCode in typeCodes) { 
                ((DataGridViewComboBoxColumn)_parametersDataGridView.Columns[1]).Items.Add(typeCode); 
            }
 
            // Set up grid
            Debug.Assert(parameters != null && parameters.Count > 0, "Expected at least one parameter");
            ArrayList parameterItems = new ArrayList(parameters.Count);
            foreach (Parameter p in parameters) { 
                parameterItems.Add(new ParameterItem(p));
            } 
            _parametersDataGridView.DataSource = parameterItems; 

            // Set command text 
            _commandTextBox.Text = _selectCommand;
            _commandTextBox.Select(0, 0);
        }
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.SqlDataSource.RefreshSchema"; 
            }
        } 

        #region Windows Form Designer generated code
        private void InitializeComponent() {
            System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn(); 
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn11 = new System.Windows.Forms.DataGridViewComboBoxColumn(); 
            System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn(); 
            this._parametersDataGridView = new System.Windows.Forms.DataGridView();
            this._okButton = new System.Windows.Forms.Button(); 
            this._commandTextBox = new System.Windows.Forms.TextBox();
            this._commandLabel = new System.Windows.Forms.Label();
            this._helpLabel = new System.Windows.Forms.Label();
            this._cancelButton = new System.Windows.Forms.Button(); 
            this._parametersLabel = new System.Windows.Forms.Label();
            this.SuspendLayout(); 
            // 
            // _helpLabel
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(12, 12);
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(448, 47); 
            this._helpLabel.TabIndex = 10;
            // 
            // _commandLabel 
            //
            this._commandLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
            this._commandLabel.Location = new System.Drawing.Point(12, 64);
            this._commandLabel.Name = "_commandLabel";
            this._commandLabel.Size = new System.Drawing.Size(448, 16);
            this._commandLabel.TabIndex = 20; 
            //
            // _commandTextBox 
            // 
            this._commandTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._commandTextBox.BackColor = System.Drawing.SystemColors.Control; 
            this._commandTextBox.Location = new System.Drawing.Point(12, 82);
            this._commandTextBox.Multiline = true;
            this._commandTextBox.Name = "_commandTextBox";
            this._commandTextBox.ReadOnly = true; 
            this._commandTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._commandTextBox.Size = new System.Drawing.Size(448, 50); 
            this._commandTextBox.TabIndex = 30; 
            //
            // _parametersLabel 
            //
            this._parametersLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._parametersLabel.Location = new System.Drawing.Point(13, 142);
            this._parametersLabel.Name = "_parametersLabel"; 
            this._parametersLabel.Size = new System.Drawing.Size(448, 16);
            this._parametersLabel.TabIndex = 40; 
            // 
            // _parametersDataGridView
            // 
            this._parametersDataGridView.AllowUserToAddRows = false;
            this._parametersDataGridView.AllowUserToDeleteRows = false;
            this._parametersDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._parametersDataGridView.AutoGenerateColumns = false; 
            this._parametersDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; 
            dataGridViewTextBoxColumn11.DataPropertyName = "Name";
            dataGridViewTextBoxColumn11.DefaultCellStyle = dataGridViewCellStyle11; 
            dataGridViewTextBoxColumn11.Name = "_parameterNameColumn";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.ValueType = typeof(string);
            dataGridViewComboBoxColumn11.DataPropertyName = "Type"; 
            dataGridViewComboBoxColumn11.DefaultCellStyle = dataGridViewCellStyle11;
            dataGridViewComboBoxColumn11.Name = "_parameterTypeColumn"; 
            dataGridViewComboBoxColumn11.ValueType = typeof(string); 
            dataGridViewTextBoxColumn12.DataPropertyName = "DefaultValue";
            dataGridViewTextBoxColumn12.DefaultCellStyle = dataGridViewCellStyle11; 
            dataGridViewTextBoxColumn12.Name = "_parameterValueColumn";
            dataGridViewTextBoxColumn12.ValueType = typeof(string);
            this._parametersDataGridView.EditMode = DataGridViewEditMode.EditOnEnter;
            this._parametersDataGridView.Columns.Add(dataGridViewTextBoxColumn11); 
            this._parametersDataGridView.Columns.Add(dataGridViewComboBoxColumn11);
            this._parametersDataGridView.Columns.Add(dataGridViewTextBoxColumn12); 
            this._parametersDataGridView.Location = new System.Drawing.Point(12, 160); 
            this._parametersDataGridView.MultiSelect = false;
            this._parametersDataGridView.Name = "_parametersDataGridView"; 
            this._parametersDataGridView.RowHeadersVisible = false;
            this._parametersDataGridView.Size = new System.Drawing.Size(448, 156);
            this._parametersDataGridView.TabIndex = 50;
            // 
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._okButton.Location = new System.Drawing.Point(304, 331);
            this._okButton.Name = "_okButton"; 
            this._okButton.TabIndex = 60;
            this._okButton.Click += new System.EventHandler(this.OnOkButtonClick);
            //
            // _cancelButton 
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this._cancelButton.Location = new System.Drawing.Point(385, 331);
            this._cancelButton.Name = "_cancelButton"; 
            this._cancelButton.TabIndex = 70;
            //
            // SqlDataSourceRefreshSchemaForm
            // 
            this.AcceptButton = this._okButton;
            this.CancelButton = this._cancelButton; 
            this.ClientSize = new System.Drawing.Size(472, 366); 
            this.Controls.Add(this._parametersLabel);
            this.Controls.Add(this._parametersDataGridView); 
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._commandLabel);
            this.Controls.Add(this._helpLabel);
            this.Controls.Add(this._okButton); 
            this.Controls.Add(this._commandTextBox);
            this.MinimumSize = new System.Drawing.Size(472, 366); 
            this.Name = "SqlDataSourceRefreshSchemaForm"; 
            this.SizeGripStyle = SizeGripStyle.Show;
 
            InitializeForm();

            this.ResumeLayout(false);
        } 
        #endregion
 
        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() {
            Text = SR.GetString(SR.SqlDataSourceRefreshSchemaForm_Title, _sqlDataSource.ID);
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceRefreshSchemaForm_HelpLabel); 
            _commandLabel.Text = SR.GetString(SR.SqlDataSource_General_PreviewLabel);
            _parametersLabel.Text = SR.GetString(SR.SqlDataSourceRefreshSchemaForm_ParametersLabel); 
            _parametersDataGridView.AccessibleName = SR.GetString(SR.SqlDataSourceParameterValueEditorForm_ParametersGridAccessibleName); 

            _okButton.Text = SR.GetString(SR.OK); 
            _cancelButton.Text = SR.GetString(SR.Cancel);

            _parametersDataGridView.Columns[0].HeaderText = SR.GetString(SR.SqlDataSourceParameterValueEditorForm_ParameterColumnHeader);
            _parametersDataGridView.Columns[1].HeaderText = SR.GetString(SR.SqlDataSourceParameterValueEditorForm_TypeColumnHeader); 
            _parametersDataGridView.Columns[2].HeaderText = SR.GetString(SR.SqlDataSourceParameterValueEditorForm_ValueColumnHeader);
        } 
 
        private void OnOkButtonClick(object sender, EventArgs e) {
            // Collect the parameters and attempt to get schema 
            ICollection editedParameters = (ICollection)_parametersDataGridView.DataSource;
            ParameterCollection parameters = new ParameterCollection();

            foreach (ParameterItem p in editedParameters) { 
                parameters.Add(new Parameter(p.Name, p.Type, p.DefaultValue));
            } 
 
            bool success = _sqlDataSourceDesigner.RefreshSchema(new DesignerDataConnection(String.Empty, _providerName, _connectionString), _selectCommand, _selectCommandType, parameters, false);
 
            if (success) {
                DialogResult = DialogResult.OK;
                Close();
            } 
        }
 
        protected override void OnVisibleChanged(EventArgs e) { 
            base.OnVisibleChanged(e);
 
            if (Visible) {
                // Calculate a better column width to stretch the columns out a little
                int columnWidth = (int)Math.Floor((double)(_parametersDataGridView.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2 * SystemInformation.Border3DSize.Width) / 3.5D);
                _parametersDataGridView.Columns[0].Width = (int)(columnWidth * 1.5); 
                _parametersDataGridView.Columns[1].Width = columnWidth;
                _parametersDataGridView.Columns[2].Width = columnWidth; 
 
                _parametersDataGridView.AutoResizeColumnHeadersHeight();
                for (int rowIndex = 0; rowIndex < _parametersDataGridView.Rows.Count; rowIndex++) { 
                    _parametersDataGridView.AutoResizeRow(rowIndex, DataGridViewAutoSizeRowMode.AllCells);
                }
            }
        } 

        private sealed class ParameterItem { 
            private string _name; 
            private TypeCode _type;
            private string _defaultValue; 

            public ParameterItem(Parameter p) {
                _name = p.Name;
                _type = p.Type; 
                _defaultValue = p.DefaultValue;
            } 
 
            public string Name {
                get { 
                    return _name;
                }
            }
 
            public TypeCode Type {
                get { 
                    return _type; 
                }
                set { 
                    _type = value;
                }
            }
 
            public string DefaultValue {
                get { 
                    return _defaultValue; 
                }
                set { 
                    _defaultValue = value;
                }
            }
        } 

        private sealed class TypeCodeComparer : IComparer { 
            int IComparer.Compare(object x, object y) { 
                return String.Compare(Enum.GetName(typeof(TypeCode), x), Enum.GetName(typeof(TypeCode), y), StringComparison.OrdinalIgnoreCase);
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceRefreshSchemaForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Data;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Windows.Forms; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Collections.Generic;

    /// <devdoc>
    /// The SqlDataSource Refresh Schema form. This guides the user through 
    /// the process of gathering data in order to be able to refresh schema.
    /// </devdoc> 
    internal class SqlDataSourceRefreshSchemaForm : DesignerForm { 
        private System.Windows.Forms.TextBox _commandTextBox;
        private System.Windows.Forms.Label _helpLabel; 
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Label _commandLabel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.DataGridView _parametersDataGridView; 
        private System.Windows.Forms.Label _parametersLabel;
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner; 
        private SqlDataSource _sqlDataSource;
        private string _connectionString; 
        private string _providerName;
        private string _selectCommand;
        private SqlDataSourceCommandType _selectCommandType;
 

        public SqlDataSourceRefreshSchemaForm(IServiceProvider serviceProvider, SqlDataSourceDesigner sqlDataSourceDesigner, ParameterCollection parameters) 
            : base(serviceProvider) { 
            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 

            _sqlDataSource = (SqlDataSource)_sqlDataSourceDesigner.Component;
            _connectionString = _sqlDataSourceDesigner.ConnectionString;
            _providerName = _sqlDataSourceDesigner.ProviderName; 
            _selectCommand = _sqlDataSourceDesigner.SelectCommand;
            _selectCommandType = _sqlDataSource.SelectCommandType; 
 
            InitializeComponent();
            InitializeUI(); 


            // Populate TypeCode combobox
            Array typeCodes = Enum.GetValues(typeof(TypeCode)); 
            Array.Sort(typeCodes, new TypeCodeComparer());
            foreach (TypeCode typeCode in typeCodes) { 
                ((DataGridViewComboBoxColumn)_parametersDataGridView.Columns[1]).Items.Add(typeCode); 
            }
 
            // Set up grid
            Debug.Assert(parameters != null && parameters.Count > 0, "Expected at least one parameter");
            ArrayList parameterItems = new ArrayList(parameters.Count);
            foreach (Parameter p in parameters) { 
                parameterItems.Add(new ParameterItem(p));
            } 
            _parametersDataGridView.DataSource = parameterItems; 

            // Set command text 
            _commandTextBox.Text = _selectCommand;
            _commandTextBox.Select(0, 0);
        }
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.SqlDataSource.RefreshSchema"; 
            }
        } 

        #region Windows Form Designer generated code
        private void InitializeComponent() {
            System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn(); 
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn11 = new System.Windows.Forms.DataGridViewComboBoxColumn(); 
            System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn(); 
            this._parametersDataGridView = new System.Windows.Forms.DataGridView();
            this._okButton = new System.Windows.Forms.Button(); 
            this._commandTextBox = new System.Windows.Forms.TextBox();
            this._commandLabel = new System.Windows.Forms.Label();
            this._helpLabel = new System.Windows.Forms.Label();
            this._cancelButton = new System.Windows.Forms.Button(); 
            this._parametersLabel = new System.Windows.Forms.Label();
            this.SuspendLayout(); 
            // 
            // _helpLabel
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(12, 12);
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(448, 47); 
            this._helpLabel.TabIndex = 10;
            // 
            // _commandLabel 
            //
            this._commandLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
            this._commandLabel.Location = new System.Drawing.Point(12, 64);
            this._commandLabel.Name = "_commandLabel";
            this._commandLabel.Size = new System.Drawing.Size(448, 16);
            this._commandLabel.TabIndex = 20; 
            //
            // _commandTextBox 
            // 
            this._commandTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._commandTextBox.BackColor = System.Drawing.SystemColors.Control; 
            this._commandTextBox.Location = new System.Drawing.Point(12, 82);
            this._commandTextBox.Multiline = true;
            this._commandTextBox.Name = "_commandTextBox";
            this._commandTextBox.ReadOnly = true; 
            this._commandTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._commandTextBox.Size = new System.Drawing.Size(448, 50); 
            this._commandTextBox.TabIndex = 30; 
            //
            // _parametersLabel 
            //
            this._parametersLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._parametersLabel.Location = new System.Drawing.Point(13, 142);
            this._parametersLabel.Name = "_parametersLabel"; 
            this._parametersLabel.Size = new System.Drawing.Size(448, 16);
            this._parametersLabel.TabIndex = 40; 
            // 
            // _parametersDataGridView
            // 
            this._parametersDataGridView.AllowUserToAddRows = false;
            this._parametersDataGridView.AllowUserToDeleteRows = false;
            this._parametersDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._parametersDataGridView.AutoGenerateColumns = false; 
            this._parametersDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; 
            dataGridViewTextBoxColumn11.DataPropertyName = "Name";
            dataGridViewTextBoxColumn11.DefaultCellStyle = dataGridViewCellStyle11; 
            dataGridViewTextBoxColumn11.Name = "_parameterNameColumn";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.ValueType = typeof(string);
            dataGridViewComboBoxColumn11.DataPropertyName = "Type"; 
            dataGridViewComboBoxColumn11.DefaultCellStyle = dataGridViewCellStyle11;
            dataGridViewComboBoxColumn11.Name = "_parameterTypeColumn"; 
            dataGridViewComboBoxColumn11.ValueType = typeof(string); 
            dataGridViewTextBoxColumn12.DataPropertyName = "DefaultValue";
            dataGridViewTextBoxColumn12.DefaultCellStyle = dataGridViewCellStyle11; 
            dataGridViewTextBoxColumn12.Name = "_parameterValueColumn";
            dataGridViewTextBoxColumn12.ValueType = typeof(string);
            this._parametersDataGridView.EditMode = DataGridViewEditMode.EditOnEnter;
            this._parametersDataGridView.Columns.Add(dataGridViewTextBoxColumn11); 
            this._parametersDataGridView.Columns.Add(dataGridViewComboBoxColumn11);
            this._parametersDataGridView.Columns.Add(dataGridViewTextBoxColumn12); 
            this._parametersDataGridView.Location = new System.Drawing.Point(12, 160); 
            this._parametersDataGridView.MultiSelect = false;
            this._parametersDataGridView.Name = "_parametersDataGridView"; 
            this._parametersDataGridView.RowHeadersVisible = false;
            this._parametersDataGridView.Size = new System.Drawing.Size(448, 156);
            this._parametersDataGridView.TabIndex = 50;
            // 
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._okButton.Location = new System.Drawing.Point(304, 331);
            this._okButton.Name = "_okButton"; 
            this._okButton.TabIndex = 60;
            this._okButton.Click += new System.EventHandler(this.OnOkButtonClick);
            //
            // _cancelButton 
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this._cancelButton.Location = new System.Drawing.Point(385, 331);
            this._cancelButton.Name = "_cancelButton"; 
            this._cancelButton.TabIndex = 70;
            //
            // SqlDataSourceRefreshSchemaForm
            // 
            this.AcceptButton = this._okButton;
            this.CancelButton = this._cancelButton; 
            this.ClientSize = new System.Drawing.Size(472, 366); 
            this.Controls.Add(this._parametersLabel);
            this.Controls.Add(this._parametersDataGridView); 
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._commandLabel);
            this.Controls.Add(this._helpLabel);
            this.Controls.Add(this._okButton); 
            this.Controls.Add(this._commandTextBox);
            this.MinimumSize = new System.Drawing.Size(472, 366); 
            this.Name = "SqlDataSourceRefreshSchemaForm"; 
            this.SizeGripStyle = SizeGripStyle.Show;
 
            InitializeForm();

            this.ResumeLayout(false);
        } 
        #endregion
 
        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() {
            Text = SR.GetString(SR.SqlDataSourceRefreshSchemaForm_Title, _sqlDataSource.ID);
            _helpLabel.Text = SR.GetString(SR.SqlDataSourceRefreshSchemaForm_HelpLabel); 
            _commandLabel.Text = SR.GetString(SR.SqlDataSource_General_PreviewLabel);
            _parametersLabel.Text = SR.GetString(SR.SqlDataSourceRefreshSchemaForm_ParametersLabel); 
            _parametersDataGridView.AccessibleName = SR.GetString(SR.SqlDataSourceParameterValueEditorForm_ParametersGridAccessibleName); 

            _okButton.Text = SR.GetString(SR.OK); 
            _cancelButton.Text = SR.GetString(SR.Cancel);

            _parametersDataGridView.Columns[0].HeaderText = SR.GetString(SR.SqlDataSourceParameterValueEditorForm_ParameterColumnHeader);
            _parametersDataGridView.Columns[1].HeaderText = SR.GetString(SR.SqlDataSourceParameterValueEditorForm_TypeColumnHeader); 
            _parametersDataGridView.Columns[2].HeaderText = SR.GetString(SR.SqlDataSourceParameterValueEditorForm_ValueColumnHeader);
        } 
 
        private void OnOkButtonClick(object sender, EventArgs e) {
            // Collect the parameters and attempt to get schema 
            ICollection editedParameters = (ICollection)_parametersDataGridView.DataSource;
            ParameterCollection parameters = new ParameterCollection();

            foreach (ParameterItem p in editedParameters) { 
                parameters.Add(new Parameter(p.Name, p.Type, p.DefaultValue));
            } 
 
            bool success = _sqlDataSourceDesigner.RefreshSchema(new DesignerDataConnection(String.Empty, _providerName, _connectionString), _selectCommand, _selectCommandType, parameters, false);
 
            if (success) {
                DialogResult = DialogResult.OK;
                Close();
            } 
        }
 
        protected override void OnVisibleChanged(EventArgs e) { 
            base.OnVisibleChanged(e);
 
            if (Visible) {
                // Calculate a better column width to stretch the columns out a little
                int columnWidth = (int)Math.Floor((double)(_parametersDataGridView.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2 * SystemInformation.Border3DSize.Width) / 3.5D);
                _parametersDataGridView.Columns[0].Width = (int)(columnWidth * 1.5); 
                _parametersDataGridView.Columns[1].Width = columnWidth;
                _parametersDataGridView.Columns[2].Width = columnWidth; 
 
                _parametersDataGridView.AutoResizeColumnHeadersHeight();
                for (int rowIndex = 0; rowIndex < _parametersDataGridView.Rows.Count; rowIndex++) { 
                    _parametersDataGridView.AutoResizeRow(rowIndex, DataGridViewAutoSizeRowMode.AllCells);
                }
            }
        } 

        private sealed class ParameterItem { 
            private string _name; 
            private TypeCode _type;
            private string _defaultValue; 

            public ParameterItem(Parameter p) {
                _name = p.Name;
                _type = p.Type; 
                _defaultValue = p.DefaultValue;
            } 
 
            public string Name {
                get { 
                    return _name;
                }
            }
 
            public TypeCode Type {
                get { 
                    return _type; 
                }
                set { 
                    _type = value;
                }
            }
 
            public string DefaultValue {
                get { 
                    return _defaultValue; 
                }
                set { 
                    _defaultValue = value;
                }
            }
        } 

        private sealed class TypeCodeComparer : IComparer { 
            int IComparer.Compare(object x, object y) { 
                return String.Compare(Enum.GetName(typeof(TypeCode), x), Enum.GetName(typeof(TypeCode), y), StringComparison.OrdinalIgnoreCase);
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
