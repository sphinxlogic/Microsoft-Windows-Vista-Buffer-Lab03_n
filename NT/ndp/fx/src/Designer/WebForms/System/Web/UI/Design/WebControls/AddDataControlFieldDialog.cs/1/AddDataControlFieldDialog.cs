//------------------------------------------------------------------------------ 
// <copyright file="AddDataControlFieldDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design;

    using Control = System.Web.UI.Control;
    using ControlDesigner = System.Web.UI.Design.ControlDesigner; 
    using GridView = System.Web.UI.WebControls.GridView;
 
    using BorderStyle =System.Windows.Forms.BorderStyle; 
    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label; 
    using ComboBox = System.Windows.Forms.ComboBox;
    using TextBox = System.Windows.Forms.TextBox;
    using CheckBox = System.Windows.Forms.CheckBox;
    using RadioButton = System.Windows.Forms.RadioButton; 
    using Panel = System.Windows.Forms.Panel;
 
    /// <devdoc> 
    ///   The AddDataControlField dialog used for web controls.  This is invoked when you click the "Add new column" verb on DetailsView or GridView.
    /// </devdoc> 
    /// <internalonly/>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class AddDataControlFieldDialog : DesignerForm {
 
        private DataBoundControlDesigner _controlDesigner;
        DataControlFieldControl[] _dataControlFieldControls; 
        private IDataSourceFieldSchema[] _fieldSchemas; 
        private bool _initialIgnoreRefreshSchemaValue;
 
        private Button _okButton;
        private Button _cancelButton;
        private Label _fieldLabel;
        private ComboBox _fieldList; 
        private LinkLabel _refreshSchemaLink;
        private Panel _controlsPanel; 
 
        private const int buttonWidth = 75;
        private const int buttonHeight = 23; 
        private const int formHeight = 510;
        private const int formWidth = 330;
        private const int labelLeft = 12;
        private const int labelHeight = 17; 
        private const int labelPadding = 2;
        private const int labelWidth = 270; 
        private const int controlLeft = 12; 
        private const int controlHeight = 20;
        private const int fieldChooserWidth = 150; 
        private const int textBoxWidth = 270;
        private const int vertPadding = 4;
        private const int horizPadding = 6;
        private const int topPadding = 12; 
        private const int bottomPadding = 12;
        private const int rightPadding = 12; 
        private const int linkWidth = 100; 
        private const int checkBoxWidth = 125;
        private int fieldControlTop = topPadding + labelHeight + labelPadding + controlHeight; 

        /// <devdoc>
        ///  Creates a new instance of the class
        /// </devdoc> 
        public AddDataControlFieldDialog(DataBoundControlDesigner controlDesigner) : base(controlDesigner.Component.Site) {
            this._controlDesigner = controlDesigner; 
            IgnoreRefreshSchemaEvents(); 
            InitForm();
        } 

        private DataBoundControl Control {
            get {
                return _controlDesigner.Component as DataBoundControl; 
            }
        } 
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.DataControlField.AddDataControlFieldDialog";
            }
        }
 
        private bool IgnoreRefreshSchema {
            get { 
                if (_controlDesigner is GridViewDesigner) { 
                    return ((GridViewDesigner)_controlDesigner)._ignoreSchemaRefreshedEvent;
                } 
                if (_controlDesigner is DetailsViewDesigner) {
                    return ((DetailsViewDesigner)_controlDesigner)._ignoreSchemaRefreshedEvent;
                }
                return false; 
            }
            set { 
                if (_controlDesigner is GridViewDesigner) { 
                    ((GridViewDesigner)_controlDesigner)._ignoreSchemaRefreshedEvent = value;
                } 
                if (_controlDesigner is DetailsViewDesigner) {
                    ((DetailsViewDesigner)_controlDesigner)._ignoreSchemaRefreshedEvent = value;
                }
            } 
        }
 
        /// <devdoc> 
        /// Adds the controls that are common to all fields
        /// </devdoc> 
        private void AddControls() {
            _okButton.SetBounds(formWidth - rightPadding - (buttonWidth * 2) - horizPadding,
                               formHeight - bottomPadding - buttonHeight,
                               buttonWidth, 
                               buttonHeight);
            _okButton.Click += new EventHandler(this.OnClickOKButton); 
            _okButton.Text = SR.GetString(SR.OKCaption); 
            _okButton.TabIndex = 201;
            _okButton.FlatStyle = FlatStyle.System; 
            _okButton.DialogResult = System.Windows.Forms.DialogResult.OK;

            _cancelButton.SetBounds(formWidth - rightPadding - buttonWidth,
                                   formHeight - bottomPadding - buttonHeight, 
                                   buttonWidth,
                                   buttonHeight); 
            _cancelButton.DialogResult = DialogResult.Cancel; 
            _cancelButton.Text = SR.GetString(SR.CancelCaption);
            _cancelButton.FlatStyle = FlatStyle.System; 
            _cancelButton.TabIndex = 202;
            _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            _fieldLabel.Text = SR.GetString(SR.DCFAdd_ChooseField); 
            _fieldLabel.TabStop = false;
            _fieldLabel.TextAlign = ContentAlignment.BottomLeft; 
            _fieldLabel.SetBounds(labelLeft, 
                                  topPadding,
                                  formWidth - (labelLeft * 2), 
                                  labelHeight);
            _fieldLabel.TabIndex = 0;

            _fieldList.DropDownStyle = ComboBoxStyle.DropDownList; 
            _fieldList.TabIndex = 1;
 
            _controlsPanel.SetBounds(controlLeft, 
                                     fieldControlTop,
                                     formWidth, 
                                     formHeight - fieldControlTop - bottomPadding - buttonHeight - vertPadding);
            _controlsPanel.TabIndex = 100;

            for (int i = 0; i < GetDataControlFieldControls().Length; i++) { 
                DataControlFieldControl fieldControl = GetDataControlFieldControls()[i];
                _fieldList.Items.Add(fieldControl.FieldName); 
                fieldControl.Visible = false; 
                fieldControl.TabStop = false;
                fieldControl.SetBounds(0, 
                                       0,
                                       formWidth,
                                       formHeight - fieldControlTop - bottomPadding - buttonHeight - vertPadding);
                _controlsPanel.Controls.Add(fieldControl); 
            }
 
            _fieldList.SelectedIndex = 0; 
            _fieldList.SelectedIndexChanged += new EventHandler(this.OnSelectedFieldTypeChanged);
            SetSelectedFieldControlVisible(); 
            _fieldList.SetBounds(labelLeft,
                                 topPadding + labelHeight + labelPadding,
                                 fieldChooserWidth,
                                 controlHeight); 

            _refreshSchemaLink.SetBounds(labelLeft, 
                                    formHeight - bottomPadding - buttonHeight, 
                                    linkWidth,
                                    (labelHeight * 2) + (labelPadding * 2) + vertPadding); 
            _refreshSchemaLink.TabIndex = 200;
            _refreshSchemaLink.Visible = false;
            _refreshSchemaLink.Text = SR.GetString(SR.DataSourceDesigner_RefreshSchemaNoHotkey);
            _refreshSchemaLink.UseMnemonic = true; 
            _refreshSchemaLink.LinkClicked += new LinkLabelLinkClickedEventHandler(this.OnClickRefreshSchema);
 
            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
 
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                    _cancelButton,
                                    _okButton,
                                    _fieldLabel, 
                                    _fieldList,
                                    _controlsPanel, 
                                    _refreshSchemaLink 
                                    });
        } 

        private IDataSourceFieldSchema[] GetBooleanFieldSchemas() {
            IDataSourceFieldSchema[] fieldSchemas = GetFieldSchemas();
 
            ArrayList booleanFieldsList = new ArrayList();
            IDataSourceFieldSchema[] booleanFieldSchemas = null; 
            if (fieldSchemas != null) { 
                foreach (IDataSourceFieldSchema schema in fieldSchemas) {
                    if (schema.DataType == typeof(bool)) { 
                        booleanFieldsList.Add(schema);
                    }
                }
                booleanFieldSchemas = new IDataSourceFieldSchema[booleanFieldsList.Count]; 
                booleanFieldsList.CopyTo(booleanFieldSchemas);
            } 
            return booleanFieldSchemas; 
        }
 
        private DataControlFieldControl[] GetDataControlFieldControls() {
            Type controlType = Control.GetType();
            if (_dataControlFieldControls == null) {
                _dataControlFieldControls = new DataControlFieldControl[] { 
                    new BoundFieldControl(GetFieldSchemas(), controlType),
                    new CheckBoxFieldControl(GetBooleanFieldSchemas(), controlType), 
                    new HyperLinkFieldControl(GetFieldSchemas(), controlType), 
                    new ButtonFieldControl(null, controlType),
                    new CommandFieldControl(null, controlType), 
                    new ImageFieldControl(GetFieldSchemas(), controlType),
                    new TemplateFieldControl(null, controlType)
                };
            } 
            return _dataControlFieldControls;
        } 
 
        /// <devdoc>
        /// Returns an array of string indicating the fields of the schema, paying attention 
        /// to DataMember if there is one.
        /// </devdoc>
        private IDataSourceFieldSchema[] GetFieldSchemas() {
            if (_fieldSchemas == null) { 
                IDataSourceViewSchema schema = null;
                if (_controlDesigner != null) { 
                    DesignerDataSourceView view = _controlDesigner.DesignerView; 
                    if (view != null) {
                        try { 
                            schema = view.Schema;
                        }
                        catch (Exception ex) {
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)ServiceProvider.GetService(typeof(IComponentDesignerDebugService)); 
                            if (debugService != null) {
                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message)); 
                            } 
                        }
                    } 
                }

                if (schema != null) {
                    _fieldSchemas = schema.GetFields(); 
                }
            } 
            return _fieldSchemas; 
        }
 
        private void IgnoreRefreshSchemaEvents() {
            _initialIgnoreRefreshSchemaValue = IgnoreRefreshSchema;
            IgnoreRefreshSchema = true;
            IDataSourceDesigner dsd = _controlDesigner.DataSourceDesigner; 
            if (dsd != null) {
                dsd.SuppressDataSourceEvents(); 
            } 
        }
 
        /// <devdoc>
        /// Initialized the form and calls functions to add controls to the form
        /// </devdoc>
        private void InitForm() { 
            SuspendLayout();
 
            _okButton = new Button(); 
            _cancelButton = new Button();
            _fieldLabel = new Label(); 
            _fieldList = new ComboBox();
            _refreshSchemaLink = new LinkLabel();
            _controlsPanel = new Panel();
 
            AddControls();
 
            IDataSourceDesigner dataSourceDesigner = _controlDesigner.DataSourceDesigner; 
            if (dataSourceDesigner != null) {
                if (dataSourceDesigner.CanRefreshSchema) { 
                    _refreshSchemaLink.Visible = true;
                }
            }
 
            this.Text = SR.GetString(SR.DCFAdd_Title);
            this.FormBorderStyle = FormBorderStyle.FixedDialog; 
            this.ClientSize = new Size(formWidth, formHeight); 
            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton; 
            this.Icon = null;

            InitializeForm();
 
            ResumeLayout(false);
            PerformLayout(); 
        } 

        /// <devdoc> 
        /// Handles the OK click event and closes the form.
        /// </devdoc>
        private void OnClickOKButton(object sender, EventArgs e) {
            DataControlFieldControl fieldControl = GetDataControlFieldControls()[_fieldList.SelectedIndex]; 
            DataBoundControl control = Control;
            if (control is GridView) { 
                ((GridView)control).Columns.Add(fieldControl.SaveValues()); 
            }
            else if (control is DetailsView) { 
                ((DetailsView)control).Fields.Add(fieldControl.SaveValues());
            }
        }
 
        /// <devdoc>
        ///   Refreshes the schema of the data bound control. 
        /// </devdoc> 
        private void OnClickRefreshSchema(object source, LinkLabelLinkClickedEventArgs e) {
            if (_controlDesigner != null) { 
                IDataSourceDesigner dataSourceDesigner = _controlDesigner.DataSourceDesigner;
                if (dataSourceDesigner != null) {
                    if (dataSourceDesigner.CanRefreshSchema) {
                        IDictionary preservedFields = GetDataControlFieldControls()[_fieldList.SelectedIndex].PreserveFields(); 

                        dataSourceDesigner.RefreshSchema(false); 
                        _fieldSchemas = GetFieldSchemas(); 

                        GetDataControlFieldControls()[0].RefreshSchema(_fieldSchemas); 
                        GetDataControlFieldControls()[1].RefreshSchema(GetBooleanFieldSchemas());
                        GetDataControlFieldControls()[2].RefreshSchema(_fieldSchemas);
                        GetDataControlFieldControls()[5].RefreshSchema(_fieldSchemas);
 
                        GetDataControlFieldControls()[_fieldList.SelectedIndex].RestoreFields(preservedFields);
                    } 
                } 
            }
        } 

        protected override void OnClosed(EventArgs e) {
            IDataSourceDesigner dsd = _controlDesigner.DataSourceDesigner;
            if (dsd != null) { 
                dsd.ResumeDataSourceEvents();
            } 
            IgnoreRefreshSchema = _initialIgnoreRefreshSchemaValue; 
        }
 
        /// <devdoc>
        /// Handles a selection change in the drop down that picks a field type
        /// </devdoc>
        private void OnSelectedFieldTypeChanged(object sender, EventArgs e) { 
            SetSelectedFieldControlVisible();
        } 
 
        private void SetSelectedFieldControlVisible() {
            foreach (DataControlFieldControl fieldControl in GetDataControlFieldControls()) { 
                fieldControl.Visible = false;
            }
            GetDataControlFieldControls()[_fieldList.SelectedIndex].Visible = true;
            this.Refresh(); 
        }
 
        private abstract class DataControlFieldControl : System.Windows.Forms.Control { 
            protected string[] _fieldSchemaNames;
            protected Type _controlType; 
            protected bool _haveSchema;
            protected IDataSourceFieldSchema[] _fieldSchemas;

            private Label _headerTextLabel; 
            private TextBox _headerTextBox;
 
            public DataControlFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) { 
                _fieldSchemas = fieldSchemas;
                if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                    _haveSchema = true;
                }
                _controlType = controlType;
                InitializeComponent(); 
            }
 
            public abstract string FieldName { 
                get;
            } 

            protected string[] GetFieldSchemaNames() {
                if (_fieldSchemaNames == null) {
                    if (_fieldSchemas != null) { 
                        int fields = _fieldSchemas.Length;
                        _fieldSchemaNames = new string[fields]; 
                        for (int i = 0; i < fields; i++) { 
                            _fieldSchemaNames[i] = _fieldSchemas[i].Name;
                        } 
                    }
                }

                return _fieldSchemaNames; 
            }
 
            /// <devdoc> 
            /// Adds all necessary controls to this controls' control tree
            /// </devdoc> 
            protected virtual void InitializeComponent() {
                _headerTextLabel = new Label();
                _headerTextBox = new TextBox();
 
                _headerTextLabel.Text = SR.GetString(SR.DCFAdd_HeaderText);
                _headerTextLabel.TextAlign = ContentAlignment.BottomLeft; 
                _headerTextLabel.SetBounds(0, 0, labelWidth, labelHeight); 

                _headerTextBox.TabIndex = 0; 
                _headerTextBox.SetBounds(0, labelHeight + labelPadding, textBoxWidth, controlHeight);

                this.Controls.AddRange(new System.Windows.Forms.Control[] {
                    _headerTextLabel, 
                    _headerTextBox});
            } 
 
            /// <devdoc>
            /// Called on the field in focus right before RefreshSchema is called 
            /// </devdoc>
            public IDictionary PreserveFields() {
                Hashtable table = new Hashtable();
                table["HeaderText"] = _headerTextBox.Text; 
                PreserveFields(table);
                return table; 
            } 

            protected abstract void PreserveFields(IDictionary table); 

            public void RefreshSchema(IDataSourceFieldSchema[] fieldSchemas) {
                _fieldSchemas = fieldSchemas;
                _fieldSchemaNames = null; 
                if (fieldSchemas != null && fieldSchemas.Length > 0) {
                    _haveSchema = true; 
                } 

                RefreshSchemaFields(); 
            }

            protected virtual void RefreshSchemaFields() {
                return; 
            }
 
            /// <devdoc> 
            /// Called on the field in focus after RefreshSchema is called
            /// </devdoc> 
            public void RestoreFields(IDictionary table) {
                _headerTextBox.Text = table["HeaderText"].ToString();
                RestoreFieldsInternal(table);
            } 

            protected abstract void RestoreFieldsInternal(IDictionary table); 
 
            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc>
            protected abstract DataControlField SaveValues(string headerText);

            /// <devdoc> 
            /// Called when OK is pressed to reflect the control's values back into the newly created field.
            /// </devdoc> 
            public DataControlField SaveValues() { 
                string headerText = _headerTextBox.Text;
                return SaveValues(headerText); 
            }

            protected string StripAccelerators(string text) {
                return text.Replace("&", String.Empty); 
            }
        } 
 
        private class BoundFieldControl : DataControlFieldControl {
            Label _dataFieldLabel; 
            ComboBox _dataFieldList;
            TextBox _dataFieldBox;
            CheckBox _readOnlyCheckBox;
 
            public BoundFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 
 
            public override string FieldName {
                get { 
                    return "BoundField";
                }
            }
 
            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc> 
            protected override void InitializeComponent() {
                base.InitializeComponent(); 
                _dataFieldList = new ComboBox();
                _dataFieldBox = new TextBox();
                _dataFieldLabel = new Label();
                _readOnlyCheckBox = new CheckBox(); 

                _dataFieldLabel.Text = SR.GetString(SR.DCFAdd_DataField); 
                _dataFieldLabel.TextAlign = ContentAlignment.BottomLeft; 
                _dataFieldLabel.SetBounds(0,
                                          labelHeight + labelPadding + vertPadding + controlHeight, 
                                          labelWidth,
                                          labelHeight);

                _dataFieldList.DropDownStyle = ComboBoxStyle.DropDownList; 
                _dataFieldList.TabIndex = 1;
                _dataFieldList.SetBounds(0, 
                                         (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight, 
                                         textBoxWidth,
                                         controlHeight); 
                _dataFieldList.SelectedIndexChanged += new EventHandler(OnSelectedDataFieldChanged);

                _dataFieldBox.TabIndex = 1;
                _dataFieldBox.SetBounds(0, 
                                        (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight,
                                        textBoxWidth, 
                                        controlHeight); 

                _readOnlyCheckBox.TabIndex = 2; 
                _readOnlyCheckBox.Text = SR.GetString(SR.DCFAdd_ReadOnly);
                _readOnlyCheckBox.SetBounds(0,
                                            (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2),
                                            textBoxWidth, 
                                            controlHeight);
 
                RefreshSchemaFields(); 

                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _dataFieldLabel,
                    _dataFieldBox,
                    _dataFieldList,
                    _readOnlyCheckBox}); 
            }
 
            private void OnSelectedDataFieldChanged(object sender, EventArgs e) { 
                if (_haveSchema) {
                    int dataFieldIndex = Array.IndexOf(GetFieldSchemaNames(), _dataFieldList.Text); 
                    if (dataFieldIndex >= 0) {
                        if (_fieldSchemas[dataFieldIndex].PrimaryKey) {
                            _readOnlyCheckBox.Checked = true;
                            return; 
                        }
                    } 
                } 
                _readOnlyCheckBox.Checked = false;
            } 

            protected override void PreserveFields(IDictionary table) {
                if (_haveSchema) {
                    table["DataField"] = _dataFieldList.Text; 
                }
                else { 
                    table["DataField"] = _dataFieldBox.Text; 
                }
                table["ReadOnly"] = _readOnlyCheckBox.Checked; 
            }

            protected override void RefreshSchemaFields() {
                if (_haveSchema) { 
                    _dataFieldList.Items.Clear();
                    _dataFieldList.Items.AddRange(GetFieldSchemaNames()); 
                    _dataFieldList.SelectedIndex = 0; 
                    _dataFieldList.Visible = true;
                    _dataFieldBox.Visible = false; 
                }
                else {
                    _dataFieldList.Visible = false;
                    _dataFieldBox.Visible = true; 
                }
            } 
 
            protected override void RestoreFieldsInternal(IDictionary table) {
                string dataField = table["DataField"].ToString(); 
                if (_haveSchema) {
                    if (dataField.Length > 0) {
                        bool foundItem = false;
                        foreach (object listItem in _dataFieldList.Items) { 
                            if (String.Compare(dataField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _dataFieldList.SelectedItem = listItem; 
                                foundItem = true; 
                                break;
                            } 
                        }
                        if (!foundItem) {
                            _dataFieldList.Items.Insert(0, dataField);
                            _dataFieldList.SelectedIndex = 0; 
                        }
                    } 
                } 
                else {
                    _dataFieldBox.Text = dataField; 
                }
                _readOnlyCheckBox.Checked = (bool)table["ReadOnly"];
            }
 
            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc> 
            protected override DataControlField SaveValues(string headerText) {
                BoundField field = new BoundField(); 
                field.HeaderText = headerText;

                if (_haveSchema) {
                    field.DataField = _dataFieldList.Text; 
                }
                else { 
                    field.DataField = _dataFieldBox.Text; 
                }
                field.ReadOnly = _readOnlyCheckBox.Checked; 
                field.SortExpression = field.DataField;
                return field;
            }
        } 

        private class CheckBoxFieldControl : DataControlFieldControl { 
            Label _dataFieldLabel; 
            ComboBox _dataFieldList;
            TextBox _dataFieldBox; 
            CheckBox _readOnlyCheckBox;

            public CheckBoxFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 

            public override string FieldName { 
                get { 
                    return "CheckBoxField";
                } 
            }

            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc>
            protected override void InitializeComponent() { 
                base.InitializeComponent(); 
                _dataFieldList = new ComboBox();
                _dataFieldBox = new TextBox(); 
                _dataFieldLabel = new Label();
                _readOnlyCheckBox = new CheckBox();

                _dataFieldLabel.Text = SR.GetString(SR.DCFAdd_DataField); 
                _dataFieldLabel.TextAlign = ContentAlignment.BottomLeft;
                _dataFieldLabel.SetBounds(0, 
                                          labelHeight + labelPadding + vertPadding + controlHeight, 
                                          labelWidth,
                                          labelHeight); 

                _dataFieldList.DropDownStyle = ComboBoxStyle.DropDownList;
                _dataFieldList.TabIndex = 1;
                _dataFieldList.SetBounds(0, 
                                         (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight,
                                         textBoxWidth, 
                                         controlHeight); 

                _dataFieldBox.TabIndex = 1; 
                _dataFieldBox.SetBounds(0,
                                        (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight,
                                        textBoxWidth,
                                        controlHeight); 

                _readOnlyCheckBox.TabIndex = 2; 
                _readOnlyCheckBox.Text = SR.GetString(SR.DCFAdd_ReadOnly); 
                _readOnlyCheckBox.SetBounds(0,
                                            (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2), 
                                            textBoxWidth,
                                            controlHeight);

 
                RefreshSchemaFields();
 
                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _dataFieldLabel,
                    _dataFieldBox, 
                    _dataFieldList,
                    _readOnlyCheckBox});
            }
 
            protected override void PreserveFields(IDictionary table) {
                if (_haveSchema) { 
                    table["DataField"] = _dataFieldList.Text; 
                }
                else { 
                    table["DataField"] = _dataFieldBox.Text;
                }
                table["ReadOnly"] = _readOnlyCheckBox.Checked;
            } 

            protected override void RefreshSchemaFields() { 
                if (_haveSchema) { 
                    _dataFieldList.Items.Clear();
                    _dataFieldList.Items.AddRange(GetFieldSchemaNames()); 
                    _dataFieldList.SelectedIndex = 0;
                    _dataFieldList.Visible = true;
                    _dataFieldBox.Visible = false;
                } 
                else {
                    _dataFieldList.Visible = false; 
                    _dataFieldBox.Visible = true; 
                }
            } 

            protected override void RestoreFieldsInternal(IDictionary table) {
                string dataField = table["DataField"].ToString();
                if (_haveSchema) { 
                    if (dataField.Length > 0) {
                        bool foundItem = false; 
                        foreach (object listItem in _dataFieldList.Items) { 
                            if (String.Compare(dataField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _dataFieldList.SelectedItem = listItem; 
                                foundItem = true;
                                break;
                            }
                        } 
                        if (!foundItem) {
                            _dataFieldList.Items.Insert(0, dataField); 
                            _dataFieldList.SelectedIndex = 0; 
                        }
                    } 
                }
                else {
                    _dataFieldBox.Text = dataField;
                } 
                _readOnlyCheckBox.Checked = (bool)table["ReadOnly"];
            } 
 
            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc>
            protected override DataControlField SaveValues(string headerText) {
                CheckBoxField field = new CheckBoxField();
                field.HeaderText = headerText; 
                if (_haveSchema) {
                    field.DataField = _dataFieldList.Text; 
                } 
                else {
                    field.DataField = _dataFieldBox.Text; 
                }
                field.ReadOnly = _readOnlyCheckBox.Checked;
                field.SortExpression = field.DataField;
                return field; 
            }
        } 
 
        private class ButtonFieldControl : DataControlFieldControl {
            Label _buttonTypeLabel; 
            Label _commandNameLabel;
            Label _textLabel;
            ComboBox _buttonTypeList;
            ComboBox _commandNameList; 
            TextBox _textBox;
 
 
            public ButtonFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 

            public override string FieldName {
                get {
                    return "ButtonField"; 
                }
            } 
 
            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc>
            protected override void InitializeComponent() {
                base.InitializeComponent();
 
                _buttonTypeLabel = new Label();
                _commandNameLabel = new Label(); 
                _textLabel = new Label(); 
                _buttonTypeList = new ComboBox();
                _commandNameList = new ComboBox(); 
                _textBox = new TextBox();

                _buttonTypeLabel.Text = SR.GetString(SR.DCFAdd_ButtonType);
                _buttonTypeLabel.TextAlign = ContentAlignment.BottomLeft; 
                _buttonTypeLabel.SetBounds(0,
                                           labelHeight + labelPadding + vertPadding + controlHeight, 
                                           labelWidth, 
                                           labelHeight);
 
                _buttonTypeList.Items.Add(ButtonType.Link.ToString());
                _buttonTypeList.Items.Add(ButtonType.Button.ToString());
                _buttonTypeList.SelectedIndex = 0;
                _buttonTypeList.DropDownStyle = ComboBoxStyle.DropDownList; 
                _buttonTypeList.TabIndex= 1;
                _buttonTypeList.SetBounds(0, 
                                          (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight, 
                                          textBoxWidth,
                                          controlHeight); 

                _commandNameLabel.Text = SR.GetString(SR.DCFAdd_CommandName);
                _commandNameLabel.TextAlign = ContentAlignment.BottomLeft;
                _commandNameLabel.SetBounds(0, 
                                            (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2),
                                            labelWidth, 
                                            labelHeight); 

                _commandNameList.Items.Add("Cancel"); 
                _commandNameList.Items.Add("Delete");
                _commandNameList.Items.Add("Edit");
                _commandNameList.Items.Add("Update");
                if (_controlType == typeof(DetailsView)) { 
                    _commandNameList.Items.Insert(3, "Insert");
                    _commandNameList.Items.Insert(4, "New"); 
                } 
                else {
                    if (_controlType == typeof(GridView)) { 
                        _commandNameList.Items.Insert(3, "Select");
                    }
                }
                _commandNameList.SelectedIndex = 0; 
                _commandNameList.DropDownStyle = ComboBoxStyle.DropDownList;
                _commandNameList.TabIndex = 2; 
                _commandNameList.SetBounds(0, 
                                           (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 2),
                                           textBoxWidth, 
                                           controlHeight);

                _textLabel.Text = SR.GetString(SR.DCFAdd_Text);
                _textLabel.TextAlign = ContentAlignment.BottomLeft; 
                _textLabel.SetBounds(0,
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 3) + (controlHeight * 3), 
                                     labelWidth, 
                                     labelHeight);
 
                _textBox.TabIndex = 3;
                _textBox.Text = SR.GetString(SR.DCFEditor_Button);
                _textBox.SetBounds(0,
                                   (labelHeight * 4) + (labelPadding * 4) + (vertPadding * 3) + (controlHeight * 3), 
                                   textBoxWidth,
                                   controlHeight); 
 
                Controls.AddRange(new System.Windows.Forms.Control[] {
                    _buttonTypeLabel, 
                    _commandNameLabel,
                    _textLabel,
                    _buttonTypeList,
                    _commandNameList, 
                    _textBox});
            } 
 
            protected override void PreserveFields(IDictionary table) {
                table["ButtonType"] = _buttonTypeList.SelectedIndex; 
                table["CommandName"] = _commandNameList.SelectedIndex;
                table["Text"] = _textBox.Text;
            }
 
            protected override void RestoreFieldsInternal(IDictionary table) {
                _buttonTypeList.SelectedIndex = (int)table["ButtonType"]; 
                _commandNameList.SelectedIndex = (int)table["CommandName"]; 
                _textBox.Text = table["Text"].ToString();
            } 

            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field.
            /// </devdoc> 
            protected override DataControlField SaveValues(string headerText) {
                ButtonField field = new ButtonField(); 
                if (headerText != null && headerText.Length > 0) { 
                    field.HeaderText = headerText;
                    field.ShowHeader = true; 
                }
                field.CommandName = _commandNameList.Text;
                field.Text = _textBox.Text;
                if (_buttonTypeList.SelectedIndex == 0) { 
                    field.ButtonType = ButtonType.Link;
                } 
                else { 
                    field.ButtonType = ButtonType.Button;
                } 
                return field;
            }
        }
 
        private class HyperLinkFieldControl : DataControlFieldControl {
            TextBox _dataTextFieldBox; 
            TextBox _dataNavFieldBox; 
            TextBox _dataNavFSBox;
            TextBox _textBox; 
            TextBox _textFSBox;
            TextBox _linkBox;
            ComboBox _dataTextFieldList;
            ComboBox _dataNavFieldList; 
            RadioButton _staticTextRadio;
            RadioButton _bindTextRadio; 
            RadioButton _staticUrlRadio; 
            RadioButton _bindUrlRadio;
            Label _linkTextFormatStringLabel; 
            Label _linkUrlFormatStringLabel;
            Label _linkTextFormatStringExampleLabel;
            Label _linkUrlFormatStringExampleLabel;
            GroupBox _textGroupBox; 
            GroupBox _linkGroupBox;
            Panel _staticTextPanel; 
            Panel _bindTextPanel; 
            Panel _staticUrlPanel;
            Panel _bindUrlPanel; 

            public HyperLinkFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            }
 
            public override string FieldName {
                get { 
                    return "HyperLinkField"; 
                }
            } 

            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree
            /// </devdoc> 
            protected override void InitializeComponent() {
                base.InitializeComponent(); 
 
                _dataTextFieldBox = new TextBox();
                _dataNavFieldBox = new TextBox(); 
                _dataNavFSBox = new TextBox();
                _linkBox = new TextBox();
                _textBox = new TextBox();
                _textFSBox = new TextBox(); 
                _dataTextFieldList = new ComboBox();
                _dataNavFieldList = new ComboBox(); 
                _staticTextRadio = new RadioButton(); 
                _bindTextRadio = new RadioButton();
                _staticUrlRadio = new RadioButton(); 
                _bindUrlRadio = new RadioButton();
                _linkTextFormatStringLabel = new Label();
                _linkUrlFormatStringLabel = new Label();
                _linkTextFormatStringExampleLabel = new Label(); 
                _linkUrlFormatStringExampleLabel = new Label();
                _textGroupBox = new GroupBox(); 
                _linkGroupBox = new GroupBox(); 
                _staticTextPanel = new Panel();
                _bindTextPanel = new Panel(); 
                _staticUrlPanel = new Panel();
                _bindUrlPanel = new Panel();

                const int boxOffset = 9; 

                // text 
                _textGroupBox.SetBounds(0, 
                                        labelHeight + labelPadding + (vertPadding * 2) + controlHeight,
                                        labelWidth + 20, 
                                        (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 3) + (controlHeight * 5));
                _textGroupBox.Text = SR.GetString(SR.DCFAdd_HyperlinkText);
                _textGroupBox.TabIndex = 1;
 
                // static text radio
                _staticTextRadio.TabIndex = 0; 
                _staticTextRadio.Text = SR.GetString(SR.DCFAdd_SpecifyText); 
                _staticTextRadio.CheckedChanged += new EventHandler(OnTextRadioChanged);
                _staticTextRadio.Checked = true; 
                _staticTextRadio.SetBounds(boxOffset,
                                         labelHeight + labelPadding,
                                         labelWidth - boxOffset,
                                         controlHeight); 

 
                // _staticTextPanel contents 
                _textBox.TabIndex = 0;
                _textBox.SetBounds(0, 
                                   0,
                                   textBoxWidth - 15 - boxOffset,
                                   controlHeight);
                _textBox.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_SpecifyText)); 
                // end _staticTextPanel contents
 
                _staticTextPanel.TabIndex = 1; 
                _staticTextPanel.SetBounds(15 + boxOffset,
                                           labelHeight + labelPadding + (controlHeight * 1), 
                                           textBoxWidth - 15 - boxOffset,
                                           controlHeight + vertPadding);
                _staticTextPanel.Controls.Add(_textBox);
 
                // bind text radio
                _bindTextRadio.TabIndex = 2; 
                _bindTextRadio.Text = SR.GetString(SR.DCFAdd_BindText); 
                _bindTextRadio.SetBounds(boxOffset,
                                   (labelHeight * 1) + labelPadding + (controlHeight * 2) + vertPadding, 
                                   labelWidth - boxOffset,
                                   controlHeight);

 
                // _bindTextPanel contents
                _dataTextFieldList.TabIndex = 0; 
                _dataTextFieldList.SetBounds(0, 
                                             0,
                                             textBoxWidth - 15 - boxOffset, 
                                             controlHeight);
                _dataTextFieldList.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_BindText));

                _dataTextFieldBox.TabIndex = 1; 
                _dataTextFieldBox.SetBounds(0,
                                            0, 
                                             textBoxWidth - 15 - boxOffset, 
                                             controlHeight);
                _dataTextFieldBox.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_BindText)); 

                _linkTextFormatStringLabel.Text = SR.GetString(SR.DCFAdd_TextFormatString);
                _linkTextFormatStringLabel.TabIndex = 2;
                _linkTextFormatStringLabel.TextAlign = ContentAlignment.BottomLeft; 
                _linkTextFormatStringLabel.SetBounds(0,
                                             (controlHeight * 1), 
                                             labelWidth - 15 - boxOffset, 
                                             labelHeight);
 
                _textFSBox.TabIndex = 3;
                _textFSBox.SetBounds(0,
                                     (labelHeight * 1) + labelPadding + (controlHeight * 1),
                                     textBoxWidth - 15 - boxOffset, 
                                     controlHeight);
 
                _linkTextFormatStringExampleLabel.Text = SR.GetString(SR.DCFAdd_TextFormatStringExample); 
                _linkTextFormatStringExampleLabel.Enabled = false;
                _linkTextFormatStringExampleLabel.TextAlign = ContentAlignment.BottomLeft; 
                _linkTextFormatStringExampleLabel.SetBounds(0,
                                                             (labelHeight * 1) + labelPadding + (controlHeight * 2),
                                                             textBoxWidth - 15 - boxOffset,
                                                             labelHeight); 
                // end _bindTextPanel contents
 
                _bindTextPanel.TabIndex = 3; 
                _bindTextPanel.SetBounds(15 + boxOffset,
                         (labelHeight * 1) + labelPadding + (controlHeight * 3) + vertPadding, 
                         labelWidth - 15 - boxOffset,
                         (labelHeight * 2) + (labelPadding * 2) + (controlHeight * 2));

                _bindTextPanel.Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _bindTextRadio,
                    _dataTextFieldList, 
                    _dataTextFieldBox, 
                    _linkTextFormatStringLabel,
                    _textFSBox, 
                    _linkTextFormatStringExampleLabel});


                _textGroupBox.Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _staticTextRadio,
                    _staticTextPanel, 
                    _bindTextRadio, 
                    _bindTextPanel});
 
                // url
                _linkGroupBox.SetBounds(0,
                                        (labelHeight * 4) + (labelPadding * 4) + (vertPadding * 6) + (controlHeight * 6),
                                        labelWidth + 20, 
                                        (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 4) + (controlHeight * 5));
                _linkGroupBox.Text = SR.GetString(SR.DCFAdd_HyperlinkURL); 
                _linkGroupBox.TabIndex = 2; 

                // staticUrlRadio 
                _staticUrlRadio.TabIndex = 0;
                _staticUrlRadio.Text = SR.GetString(SR.DCFAdd_SpecifyURL);
                _staticUrlRadio.CheckedChanged += new EventHandler(OnUrlRadioChanged);
                _staticUrlRadio.Checked = true; 
                _staticUrlRadio.SetBounds(boxOffset,
                                         (labelHeight * 1) + labelPadding, 
                                         labelWidth - boxOffset, 
                                         controlHeight);
 
                // _staticUrlPanel contents
                _linkBox.TabIndex = 0;
                _linkBox.SetBounds(0,
                                   0, 
                                   textBoxWidth - 15 - boxOffset,
                                   controlHeight); 
                _linkBox.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_SpecifyURL)); 
                // end _staticUrlPanel contents
 
                _staticUrlPanel.TabIndex = 1;
                _staticUrlPanel.SetBounds(15 + boxOffset,
                                   (labelHeight * 1) + labelPadding + (controlHeight * 1),
                                   textBoxWidth - 15 - boxOffset, 
                                   controlHeight + vertPadding);
                _staticUrlPanel.Controls.Add(_linkBox); 
 
                // _bindUrlRadio
                _bindUrlRadio.TabIndex = 2; 
                _bindUrlRadio.Text = SR.GetString(SR.DCFAdd_BindURL);
                _bindUrlRadio.SetBounds(boxOffset,
                                   (labelHeight * 1) + labelPadding + (controlHeight * 2) + vertPadding,
                                   labelWidth - boxOffset, 
                                   controlHeight);
 
                // _bindUrlPanel contents 
                _dataNavFieldList.TabIndex = 0;
                _dataNavFieldList.SetBounds(0, 
                                            0,
                                             textBoxWidth - 15 - boxOffset,
                                             controlHeight);
                _dataNavFieldList.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_BindURL)); 

                _dataNavFieldBox.TabIndex = 1; 
                _dataNavFieldBox.SetBounds(0, 
                                           0,
                                             textBoxWidth - 15 - boxOffset, 
                                             controlHeight);
                _dataNavFieldBox.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_BindURL));

                _linkUrlFormatStringLabel.Text = SR.GetString(SR.DCFAdd_URLFormatString); 
                _linkUrlFormatStringLabel.TabIndex = 2;
                _linkUrlFormatStringLabel.TextAlign = ContentAlignment.BottomLeft; 
                _linkUrlFormatStringLabel.SetBounds(0, 
                                             (controlHeight * 1),
                                             labelWidth - 15 - boxOffset, 
                                             labelHeight);

                _dataNavFSBox.TabIndex = 3;
                _dataNavFSBox.SetBounds(0, 
                                     (labelHeight * 1) + labelPadding + (controlHeight * 1),
                                     textBoxWidth - 15 - boxOffset, 
                                     controlHeight); 

                _linkUrlFormatStringExampleLabel.Text = SR.GetString(SR.DCFAdd_URLFormatStringExample); 
                _linkUrlFormatStringExampleLabel.Enabled = false;
                _linkUrlFormatStringExampleLabel.TextAlign = ContentAlignment.BottomLeft;
                _linkUrlFormatStringExampleLabel.SetBounds(0,
                                                         (labelHeight * 1) + labelPadding + (controlHeight * 2), 
                                                         textBoxWidth - 15 - boxOffset,
                                                         labelHeight); 
                // end _bindUrlPanel contents 
                _bindUrlPanel.TabIndex = 3;
                _bindUrlPanel.SetBounds(15 + boxOffset, 
                                        (labelHeight * 1) + labelPadding + (controlHeight * 3) + vertPadding,
                                        labelWidth - 15 - boxOffset,
                                        (labelHeight * 2) + (labelPadding * 2) + (controlHeight * 2));
 
                _bindUrlPanel.Controls.AddRange(new System.Windows.Forms.Control[] {
                    _dataNavFieldList, 
                    _dataNavFieldBox, 
                    _linkUrlFormatStringLabel,
                    _dataNavFSBox, 
                    _linkUrlFormatStringExampleLabel});


                _linkGroupBox.Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _staticUrlRadio,
                    _staticUrlPanel, 
                    _bindUrlRadio, 
                    _bindUrlPanel});
 
                // end
                RefreshSchemaFields();

                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _textGroupBox,
                    _linkGroupBox}); 
 
            }
 
            private void OnTextRadioChanged(object sender, EventArgs e) {
                if (_staticTextRadio.Checked) {
                    _textBox.Enabled = true;
                    _dataTextFieldList.Enabled = false; 
                    _dataTextFieldBox.Enabled = false;
                    _textFSBox.Enabled = false; 
                    _linkTextFormatStringLabel.Enabled = false; 
                }
                else { 
                    _textBox.Enabled = false;
                    _dataTextFieldList.Enabled = true;
                    _dataTextFieldBox.Enabled = true;
                    _textFSBox.Enabled = true; 
                    _linkTextFormatStringLabel.Enabled = true;
                } 
            } 

            private void OnUrlRadioChanged(object sender, EventArgs e) { 
                if (_staticUrlRadio.Checked) {
                    _linkBox.Enabled = true;
                    _dataNavFieldList.Enabled = false;
                    _dataNavFieldBox.Enabled = false; 
                    _dataNavFSBox.Enabled = false;
                    _linkUrlFormatStringLabel.Enabled = false; 
                } 
                else {
                    _linkBox.Enabled = false; 
                    _dataNavFieldList.Enabled = true;
                    _dataNavFieldBox.Enabled = true;
                    _dataNavFSBox.Enabled = true;
                    _linkUrlFormatStringLabel.Enabled = true; 
                }
            } 
 
            protected override void PreserveFields(IDictionary table) {
                if (_haveSchema) { 
                    table["DataTextField"] = _dataTextFieldList.Text;
                    table["DataNavigateUrlField"] = _dataNavFieldList.Text;
                }
                else { 
                    table["DataTextField"] = _dataTextFieldBox.Text;
                    table["DataNavigateUrlField"] = _dataNavFieldBox.Text; 
                } 
                table["DataNavigateUrlFormatString"] = _dataNavFSBox.Text;
                table["DataTextFormatString"] = _textFSBox.Text; 
                table["NavigateUrl"] = _linkBox.Text;
                table["linkMode"] = _staticUrlRadio.Checked;
                table["textMode"] = _staticTextRadio.Checked;
                table["Text"] = _textBox.Text; 
            }
 
            protected override void RefreshSchemaFields() { 
                if (_haveSchema) {
                    _dataTextFieldList.Items.Clear(); 
                    _dataTextFieldList.Items.AddRange(GetFieldSchemaNames());
                    _dataTextFieldList.Items.Insert(0, String.Empty);
                    _dataTextFieldList.SelectedIndex = 0;
                    _dataTextFieldList.Visible = true; 
                    _dataTextFieldBox.Visible = false;
 
                    _dataNavFieldList.Items.Clear(); 
                    _dataNavFieldList.Items.AddRange(GetFieldSchemaNames());
                    _dataNavFieldList.Items.Insert(0, String.Empty); 
                    _dataNavFieldList.SelectedIndex = 0;
                    _dataNavFieldList.Visible = true;
                    _dataNavFieldBox.Visible = false;
                } 
                else {
                    _dataTextFieldList.Visible = false; 
                    _dataTextFieldBox.Visible = true; 
                    _dataNavFieldList.Visible = false;
                    _dataNavFieldBox.Visible = true; 
                }
            }

            protected override void RestoreFieldsInternal(IDictionary table) { 
                string dataTextField = table["DataTextField"].ToString();
                string dataNavigateUrlField = table["DataNavigateUrlField"].ToString(); 
                if (_haveSchema) { 
                    bool foundItem = false;
                    if (dataTextField.Length > 0) { 
                        foreach (object listItem in _dataTextFieldList.Items) {
                            if (String.Compare(dataTextField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _dataTextFieldList.SelectedItem = listItem;
                                foundItem = true; 
                                break;
                            } 
                        } 
                        if (!foundItem) {
                            _dataTextFieldList.Items.Insert(0, dataTextField); 
                            _dataTextFieldList.SelectedIndex = 0;
                        }
                    }
 
                    if (dataNavigateUrlField.Length > 0) {
                        foundItem = false; 
                        foreach (object listItem in _dataNavFieldList.Items) { 
                            if (String.Compare(dataNavigateUrlField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _dataNavFieldList.SelectedItem = listItem; 
                                foundItem = true;
                                break;
                            }
                        } 
                        if (!foundItem) {
                            _dataNavFieldList.Items.Insert(0, dataNavigateUrlField); 
                            _dataNavFieldList.SelectedIndex = 0; 
                        }
                    } 
                }
                else {
                    _dataTextFieldBox.Text = dataTextField;
                    _dataNavFieldBox.Text = dataNavigateUrlField; 
                }
                _dataNavFSBox.Text = table["DataNavigateUrlFormatString"].ToString(); 
                _textFSBox.Text = table["DataTextFormatString"].ToString(); 
                _linkBox.Text = table["NavigateUrl"].ToString();
                _textBox.Text = table["Text"].ToString(); 
                _staticUrlRadio.Checked = (bool)table["linkMode"];
                _staticTextRadio.Checked = (bool)table["textMode"];
            }
 
            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc> 
            protected override DataControlField SaveValues(string headerText) {
                HyperLinkField field = new HyperLinkField(); 
                field.HeaderText = headerText;

                if (_staticTextRadio.Checked) {
                    field.Text = _textBox.Text; 
                }
                else { 
                    field.DataTextFormatString = _textFSBox.Text; 
                    if (_haveSchema) {
                        field.DataTextField = _dataTextFieldList.Text; 
                    }
                    else {
                        field.DataTextField = _dataTextFieldBox.Text;
                    } 

                } 
 
                if (_staticUrlRadio.Checked) {
                    field.NavigateUrl = _linkBox.Text; 
                }
                else {
                    field.DataNavigateUrlFormatString = _dataNavFSBox.Text;
                    if (_haveSchema) { 
                        field.DataNavigateUrlFields = new string[1]{_dataNavFieldList.Text};
                    } 
                    else { 
                        field.DataNavigateUrlFields = new string[1]{_dataNavFieldBox.Text};
                    } 
                }
                return field;
            }
        } 

        private class CommandFieldControl : DataControlFieldControl { 
            Label _buttonTypeLabel; 
            Label _commandButtonsLabel;
            ComboBox _buttonTypeList; 
            CheckBox _deleteBox;
            CheckBox _selectBox;
            CheckBox _cancelBox;
            CheckBox _updateBox; 
            CheckBox _insertBox;
            const int checkBoxLeft = 8; 
 
            public CommandFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 

            public override string FieldName {
                get {
                    return "CommandField"; 
                }
            } 
 
            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc>
            protected override void InitializeComponent() {
                base.InitializeComponent();
                _buttonTypeLabel = new Label(); 
                _buttonTypeList = new ComboBox();
                _commandButtonsLabel = new Label(); 
                _deleteBox = new CheckBox(); 
                _selectBox = new CheckBox();
                _cancelBox = new CheckBox(); 
                _updateBox = new CheckBox();
                _insertBox = new CheckBox();

 
                _buttonTypeLabel.Text = SR.GetString(SR.DCFAdd_ButtonType);
                _buttonTypeLabel.TextAlign = ContentAlignment.BottomLeft; 
                _buttonTypeLabel.SetBounds(0, 
                                           labelHeight + labelPadding + vertPadding + controlHeight,
                                           labelWidth, 
                                           labelHeight);

                _buttonTypeList.Items.Add(ButtonType.Link.ToString());
                _buttonTypeList.Items.Add(ButtonType.Button.ToString()); 
                _buttonTypeList.SelectedIndex = 0;
                _buttonTypeList.DropDownStyle = ComboBoxStyle.DropDownList; 
                _buttonTypeList.TabIndex= 1; 
                _buttonTypeList.SetBounds(0,
                                          (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight, 
                                          textBoxWidth,
                                          controlHeight);

                _commandButtonsLabel.Text = SR.GetString(SR.DCFAdd_CommandButtons); 
                _commandButtonsLabel.TextAlign = ContentAlignment.BottomLeft;
                _commandButtonsLabel.SetBounds(0, 
                                               (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2), 
                                               labelWidth,
                                               labelHeight); 

                _deleteBox.Text = SR.GetString(SR.DCFAdd_Delete);
                _deleteBox.AccessibleDescription = SR.GetString(SR.DCFAdd_DeleteDesc);
                _deleteBox.TextAlign = ContentAlignment.TopLeft; 
                _deleteBox.CheckAlign = ContentAlignment.TopLeft;
                _deleteBox.TabIndex = 2; 
                _deleteBox.SetBounds(checkBoxLeft, 
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 2),
                                     checkBoxWidth, 
                                     controlHeight);

                _selectBox.Text = SR.GetString(SR.DCFAdd_Select);
                _selectBox.AccessibleDescription = SR.GetString(SR.DCFAdd_SelectDesc); 
                _selectBox.TextAlign = ContentAlignment.TopLeft;
                _selectBox.CheckAlign = ContentAlignment.TopLeft; 
                _selectBox.TabIndex = 4; 
                _selectBox.SetBounds(checkBoxLeft,
                                    (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 3), 
                                     checkBoxWidth,
                                     controlHeight);

                _cancelBox.Text = SR.GetString(SR.DCFAdd_ShowCancel); 
                _cancelBox.AccessibleDescription = SR.GetString(SR.DCFAdd_ShowCancelDesc);
                _cancelBox.TextAlign = ContentAlignment.TopLeft; 
                _cancelBox.CheckAlign = ContentAlignment.TopLeft; 
                _cancelBox.Enabled = false;
                _cancelBox.Checked = true; 
                _cancelBox.TabIndex = 6;
                _cancelBox.SetBounds(checkBoxLeft,
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 4),
                                     textBoxWidth, 
                                     controlHeight * 2 + vertPadding);
 
                _updateBox.Text = SR.GetString(SR.DCFAdd_EditUpdate); 
                _updateBox.AccessibleDescription = SR.GetString(SR.DCFAdd_EditUpdateDesc);
                _updateBox.TextAlign = ContentAlignment.TopLeft; 
                _updateBox.CheckAlign = ContentAlignment.TopLeft;
                _updateBox.TabIndex = 3;
                _updateBox.CheckedChanged += new EventHandler(this.OnCheckedChanged);
                _updateBox.SetBounds(checkBoxWidth + horizPadding + checkBoxLeft, 
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 2),
                                     checkBoxWidth, 
                                     controlHeight); 

                _insertBox.Text = SR.GetString(SR.DCFAdd_NewInsert); 
                _insertBox.AccessibleDescription = SR.GetString(SR.DCFAdd_NewInsertDesc);
                _insertBox.TextAlign = ContentAlignment.TopLeft;
                _insertBox.CheckAlign = ContentAlignment.TopLeft;
                _insertBox.TabIndex = 5; 
                _insertBox.CheckedChanged += new EventHandler(this.OnCheckedChanged);
                _insertBox.SetBounds(checkBoxLeft, 
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 3), 
                                     checkBoxWidth,
                                     controlHeight); 

                if (_controlType == typeof(GridView)) {
                    _insertBox.Visible = false;
                } 
                else if (_controlType == typeof(DetailsView)) {
                    _selectBox.Visible = false; 
                } 

                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _buttonTypeLabel,
                    _buttonTypeList,
                    _commandButtonsLabel,
                    _deleteBox, 
                    _selectBox,
                    _cancelBox, 
                    _updateBox, 
                    _insertBox });
            } 

            private void OnCheckedChanged(object sender, EventArgs e) {
                _cancelBox.Enabled = _updateBox.Checked || _insertBox.Checked;
            } 

            protected override void PreserveFields(IDictionary table) { 
                table["ButtonType"] = _buttonTypeList.SelectedIndex; 
                table["ShowDeleteButton"] = _deleteBox.Checked;
                table["ShowSelectButton"] = _selectBox.Checked; 
                table["ShowCancelButton"] = _cancelBox.Checked;
                table["ShowEditButton"] = _updateBox.Checked;
                table["ShowInsertButton"] = _insertBox.Checked;
            } 

            protected override void RestoreFieldsInternal(IDictionary table) { 
                _buttonTypeList.SelectedIndex = (int)table["ButtonType"]; 
                _deleteBox.Checked = (bool)table["ShowDeleteButton"];
                _selectBox.Checked = (bool)table["ShowSelectButton"]; 
                _cancelBox.Checked = (bool)table["ShowCancelButton"];
                _updateBox.Checked = (bool)table["ShowEditButton"];
                _insertBox.Checked = (bool)table["ShowInsertButton"];
            } 

            /// <devdoc> 
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc>
            protected override DataControlField SaveValues(string headerText) { 
                CommandField field = new CommandField();
                if (headerText != null && headerText.Length > 0) {
                    field.HeaderText = headerText;
                    field.ShowHeader = true; 
                }
                if (_buttonTypeList.SelectedIndex == 0) { 
                    field.ButtonType = ButtonType.Link; 
                }
                else { 
                    field.ButtonType = ButtonType.Button;
                }

                field.ShowDeleteButton = _deleteBox.Checked; 
                field.ShowSelectButton = _selectBox.Checked;
                if (_cancelBox.Enabled) { 
                    field.ShowCancelButton = _cancelBox.Checked; 
                }
                field.ShowEditButton = _updateBox.Checked; 
                field.ShowInsertButton = _insertBox.Checked;

                return field;
            } 
        }
 
        private class TemplateFieldControl : DataControlFieldControl { 
            public TemplateFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 

            public override string FieldName {
                get {
                    return "TemplateField"; 
                }
            } 
 
            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc>
            protected override void InitializeComponent() {
                base.InitializeComponent();
            } 

            protected override void PreserveFields(IDictionary table) { 
            } 

            protected override void RestoreFieldsInternal(IDictionary table) { 
            }

            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc>
            protected override DataControlField SaveValues(string headerText) { 
                TemplateField field = new TemplateField(); 
                field.HeaderText = headerText;
                return field; 
            }
        }

        private class ImageFieldControl : DataControlFieldControl { 
            Label _imageUrlFieldLabel;
            ComboBox _imageUrlFieldList; 
            TextBox _imageUrlFieldBox; 
            CheckBox _readOnlyCheckBox;
            TextBox _urlFormatBox; 
            Label _urlFormatBoxLabel;
            Label _urlFormatExampleLabel;

            public ImageFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) { 
            }
 
            public override string FieldName { 
                get {
                    return "ImageField"; 
                }
            }

            /// <devdoc> 
            /// Adds all necessary controls to this controls' control tree
            /// </devdoc> 
            protected override void InitializeComponent() { 
                base.InitializeComponent();
                _imageUrlFieldList = new ComboBox(); 
                _imageUrlFieldBox = new TextBox();
                _imageUrlFieldLabel = new Label();
                _readOnlyCheckBox = new CheckBox();
                _urlFormatBox = new TextBox(); 
                _urlFormatBoxLabel = new Label();
                _urlFormatExampleLabel = new Label(); 
 

                _imageUrlFieldLabel.Text = SR.GetString(SR.DCFAdd_DataField); 
                _imageUrlFieldLabel.TextAlign = ContentAlignment.BottomLeft;
                _imageUrlFieldLabel.SetBounds(0,
                                          labelHeight + labelPadding + vertPadding + controlHeight,
                                          labelWidth, 
                                          labelHeight);
 
                _imageUrlFieldList.DropDownStyle = ComboBoxStyle.DropDownList; 
                _imageUrlFieldList.TabIndex = 1;
                _imageUrlFieldList.SetBounds(0, 
                                         (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight,
                                         textBoxWidth,
                                         controlHeight);
 
                _imageUrlFieldBox.TabIndex = 2;
                _imageUrlFieldBox.SetBounds(0, 
                                        (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight, 
                                        textBoxWidth,
                                        controlHeight); 

                _urlFormatBoxLabel.TabIndex = 3;
                _urlFormatBoxLabel.Text = SR.GetString(SR.DCFAdd_LinkFormatString);
                _urlFormatBoxLabel.TextAlign = ContentAlignment.BottomLeft; 
                _urlFormatBoxLabel.SetBounds(0,
                                    (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2), 
                                     labelWidth, 
                                     labelHeight);
 
                _urlFormatBox.TabIndex = 4;
                _urlFormatBox.SetBounds(0,
                                        (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 2),
                                        labelWidth, 
                                        controlHeight);
 
                _urlFormatExampleLabel.Enabled = false; 
                _urlFormatExampleLabel.Text = SR.GetString(SR.DCFAdd_ExampleFormatString);
                _urlFormatExampleLabel.TextAlign = ContentAlignment.BottomLeft; 
                _urlFormatExampleLabel.SetBounds(0,
                                                 (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 3),
                                                 labelWidth,
                                                 labelHeight); 

                _readOnlyCheckBox.TabIndex = 5; 
                _readOnlyCheckBox.Text = SR.GetString(SR.DCFAdd_ReadOnly); 
                _readOnlyCheckBox.SetBounds(0,
                                            (labelHeight * 4) + (labelPadding * 4) + (vertPadding * 2) + (controlHeight * 3), 
                                            textBoxWidth,
                                            controlHeight);

 
                if (_haveSchema) {
                    _imageUrlFieldList.Items.AddRange(GetFieldSchemaNames()); 
                    _imageUrlFieldList.SelectedIndex = 0; 
                    _imageUrlFieldList.Visible = true;
                    _imageUrlFieldBox.Visible = false; 
                }
                else {
                    _imageUrlFieldList.Visible = false;
                    _imageUrlFieldBox.Visible = true; 
                }
 
                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _imageUrlFieldLabel,
                    _imageUrlFieldBox, 
                    _imageUrlFieldList,
                    _readOnlyCheckBox,
                    _urlFormatBoxLabel,
                    _urlFormatBox, 
                    _urlFormatExampleLabel});
            } 
 
            protected override void PreserveFields(IDictionary table) {
                if (_haveSchema) { 
                    table["ImageUrlField"] = _imageUrlFieldList.Text;
                }
                else {
                    table["ImageUrlField"] = _imageUrlFieldBox.Text; 
                }
                table["ReadOnly"] = _readOnlyCheckBox.Checked; 
                table["FormatString"] = _urlFormatBox.Text; 
            }
 
            protected override void RefreshSchemaFields() {
                if (_haveSchema) {
                    _imageUrlFieldList.Items.Clear();
                    _imageUrlFieldList.Items.AddRange(GetFieldSchemaNames()); 
                    _imageUrlFieldList.SelectedIndex = 0;
                    _imageUrlFieldList.Visible = true; 
                    _imageUrlFieldBox.Visible = false; 
                }
                else { 
                    _imageUrlFieldList.Visible = false;
                    _imageUrlFieldBox.Visible = true;
                }
            } 

            protected override void RestoreFieldsInternal(IDictionary table) { 
                string dataField = table["ImageUrlField"].ToString(); 
                if (_haveSchema) {
                    if (dataField.Length > 0) { 
                        bool foundItem = false;
                        foreach (object listItem in _imageUrlFieldList.Items) {
                            if (String.Compare(dataField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _imageUrlFieldList.SelectedItem = listItem; 
                                foundItem = true;
                                break; 
                            } 
                        }
                        if (!foundItem) { 
                            _imageUrlFieldList.Items.Insert(0, dataField);
                            _imageUrlFieldList.SelectedIndex = 0;
                        }
                    } 
                }
                else { 
                    _imageUrlFieldBox.Text = dataField; 
                }
                _readOnlyCheckBox.Checked = (bool)table["ReadOnly"]; 
                _urlFormatBox.Text = (string)table["FormatString"];
            }

            /// <devdoc> 
            /// Called when OK is pressed to reflect the control's values back into the newly created field.
            /// </devdoc> 
            protected override DataControlField SaveValues(string headerText) { 
                ImageField field = new ImageField();
                field.HeaderText = headerText; 
                if (_haveSchema) {
                    field.DataImageUrlField = _imageUrlFieldList.Text;
                }
                else { 
                    field.DataImageUrlField = _imageUrlFieldBox.Text;
                } 
                field.ReadOnly = _readOnlyCheckBox.Checked; 
                field.DataImageUrlFormatString = _urlFormatBox.Text;
                return field; 
            }
        }

    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="AddDataControlFieldDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design;

    using Control = System.Web.UI.Control;
    using ControlDesigner = System.Web.UI.Design.ControlDesigner; 
    using GridView = System.Web.UI.WebControls.GridView;
 
    using BorderStyle =System.Windows.Forms.BorderStyle; 
    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label; 
    using ComboBox = System.Windows.Forms.ComboBox;
    using TextBox = System.Windows.Forms.TextBox;
    using CheckBox = System.Windows.Forms.CheckBox;
    using RadioButton = System.Windows.Forms.RadioButton; 
    using Panel = System.Windows.Forms.Panel;
 
    /// <devdoc> 
    ///   The AddDataControlField dialog used for web controls.  This is invoked when you click the "Add new column" verb on DetailsView or GridView.
    /// </devdoc> 
    /// <internalonly/>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class AddDataControlFieldDialog : DesignerForm {
 
        private DataBoundControlDesigner _controlDesigner;
        DataControlFieldControl[] _dataControlFieldControls; 
        private IDataSourceFieldSchema[] _fieldSchemas; 
        private bool _initialIgnoreRefreshSchemaValue;
 
        private Button _okButton;
        private Button _cancelButton;
        private Label _fieldLabel;
        private ComboBox _fieldList; 
        private LinkLabel _refreshSchemaLink;
        private Panel _controlsPanel; 
 
        private const int buttonWidth = 75;
        private const int buttonHeight = 23; 
        private const int formHeight = 510;
        private const int formWidth = 330;
        private const int labelLeft = 12;
        private const int labelHeight = 17; 
        private const int labelPadding = 2;
        private const int labelWidth = 270; 
        private const int controlLeft = 12; 
        private const int controlHeight = 20;
        private const int fieldChooserWidth = 150; 
        private const int textBoxWidth = 270;
        private const int vertPadding = 4;
        private const int horizPadding = 6;
        private const int topPadding = 12; 
        private const int bottomPadding = 12;
        private const int rightPadding = 12; 
        private const int linkWidth = 100; 
        private const int checkBoxWidth = 125;
        private int fieldControlTop = topPadding + labelHeight + labelPadding + controlHeight; 

        /// <devdoc>
        ///  Creates a new instance of the class
        /// </devdoc> 
        public AddDataControlFieldDialog(DataBoundControlDesigner controlDesigner) : base(controlDesigner.Component.Site) {
            this._controlDesigner = controlDesigner; 
            IgnoreRefreshSchemaEvents(); 
            InitForm();
        } 

        private DataBoundControl Control {
            get {
                return _controlDesigner.Component as DataBoundControl; 
            }
        } 
 
        protected override string HelpTopic {
            get { 
                return "net.Asp.DataControlField.AddDataControlFieldDialog";
            }
        }
 
        private bool IgnoreRefreshSchema {
            get { 
                if (_controlDesigner is GridViewDesigner) { 
                    return ((GridViewDesigner)_controlDesigner)._ignoreSchemaRefreshedEvent;
                } 
                if (_controlDesigner is DetailsViewDesigner) {
                    return ((DetailsViewDesigner)_controlDesigner)._ignoreSchemaRefreshedEvent;
                }
                return false; 
            }
            set { 
                if (_controlDesigner is GridViewDesigner) { 
                    ((GridViewDesigner)_controlDesigner)._ignoreSchemaRefreshedEvent = value;
                } 
                if (_controlDesigner is DetailsViewDesigner) {
                    ((DetailsViewDesigner)_controlDesigner)._ignoreSchemaRefreshedEvent = value;
                }
            } 
        }
 
        /// <devdoc> 
        /// Adds the controls that are common to all fields
        /// </devdoc> 
        private void AddControls() {
            _okButton.SetBounds(formWidth - rightPadding - (buttonWidth * 2) - horizPadding,
                               formHeight - bottomPadding - buttonHeight,
                               buttonWidth, 
                               buttonHeight);
            _okButton.Click += new EventHandler(this.OnClickOKButton); 
            _okButton.Text = SR.GetString(SR.OKCaption); 
            _okButton.TabIndex = 201;
            _okButton.FlatStyle = FlatStyle.System; 
            _okButton.DialogResult = System.Windows.Forms.DialogResult.OK;

            _cancelButton.SetBounds(formWidth - rightPadding - buttonWidth,
                                   formHeight - bottomPadding - buttonHeight, 
                                   buttonWidth,
                                   buttonHeight); 
            _cancelButton.DialogResult = DialogResult.Cancel; 
            _cancelButton.Text = SR.GetString(SR.CancelCaption);
            _cancelButton.FlatStyle = FlatStyle.System; 
            _cancelButton.TabIndex = 202;
            _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            _fieldLabel.Text = SR.GetString(SR.DCFAdd_ChooseField); 
            _fieldLabel.TabStop = false;
            _fieldLabel.TextAlign = ContentAlignment.BottomLeft; 
            _fieldLabel.SetBounds(labelLeft, 
                                  topPadding,
                                  formWidth - (labelLeft * 2), 
                                  labelHeight);
            _fieldLabel.TabIndex = 0;

            _fieldList.DropDownStyle = ComboBoxStyle.DropDownList; 
            _fieldList.TabIndex = 1;
 
            _controlsPanel.SetBounds(controlLeft, 
                                     fieldControlTop,
                                     formWidth, 
                                     formHeight - fieldControlTop - bottomPadding - buttonHeight - vertPadding);
            _controlsPanel.TabIndex = 100;

            for (int i = 0; i < GetDataControlFieldControls().Length; i++) { 
                DataControlFieldControl fieldControl = GetDataControlFieldControls()[i];
                _fieldList.Items.Add(fieldControl.FieldName); 
                fieldControl.Visible = false; 
                fieldControl.TabStop = false;
                fieldControl.SetBounds(0, 
                                       0,
                                       formWidth,
                                       formHeight - fieldControlTop - bottomPadding - buttonHeight - vertPadding);
                _controlsPanel.Controls.Add(fieldControl); 
            }
 
            _fieldList.SelectedIndex = 0; 
            _fieldList.SelectedIndexChanged += new EventHandler(this.OnSelectedFieldTypeChanged);
            SetSelectedFieldControlVisible(); 
            _fieldList.SetBounds(labelLeft,
                                 topPadding + labelHeight + labelPadding,
                                 fieldChooserWidth,
                                 controlHeight); 

            _refreshSchemaLink.SetBounds(labelLeft, 
                                    formHeight - bottomPadding - buttonHeight, 
                                    linkWidth,
                                    (labelHeight * 2) + (labelPadding * 2) + vertPadding); 
            _refreshSchemaLink.TabIndex = 200;
            _refreshSchemaLink.Visible = false;
            _refreshSchemaLink.Text = SR.GetString(SR.DataSourceDesigner_RefreshSchemaNoHotkey);
            _refreshSchemaLink.UseMnemonic = true; 
            _refreshSchemaLink.LinkClicked += new LinkLabelLinkClickedEventHandler(this.OnClickRefreshSchema);
 
            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
 
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                    _cancelButton,
                                    _okButton,
                                    _fieldLabel, 
                                    _fieldList,
                                    _controlsPanel, 
                                    _refreshSchemaLink 
                                    });
        } 

        private IDataSourceFieldSchema[] GetBooleanFieldSchemas() {
            IDataSourceFieldSchema[] fieldSchemas = GetFieldSchemas();
 
            ArrayList booleanFieldsList = new ArrayList();
            IDataSourceFieldSchema[] booleanFieldSchemas = null; 
            if (fieldSchemas != null) { 
                foreach (IDataSourceFieldSchema schema in fieldSchemas) {
                    if (schema.DataType == typeof(bool)) { 
                        booleanFieldsList.Add(schema);
                    }
                }
                booleanFieldSchemas = new IDataSourceFieldSchema[booleanFieldsList.Count]; 
                booleanFieldsList.CopyTo(booleanFieldSchemas);
            } 
            return booleanFieldSchemas; 
        }
 
        private DataControlFieldControl[] GetDataControlFieldControls() {
            Type controlType = Control.GetType();
            if (_dataControlFieldControls == null) {
                _dataControlFieldControls = new DataControlFieldControl[] { 
                    new BoundFieldControl(GetFieldSchemas(), controlType),
                    new CheckBoxFieldControl(GetBooleanFieldSchemas(), controlType), 
                    new HyperLinkFieldControl(GetFieldSchemas(), controlType), 
                    new ButtonFieldControl(null, controlType),
                    new CommandFieldControl(null, controlType), 
                    new ImageFieldControl(GetFieldSchemas(), controlType),
                    new TemplateFieldControl(null, controlType)
                };
            } 
            return _dataControlFieldControls;
        } 
 
        /// <devdoc>
        /// Returns an array of string indicating the fields of the schema, paying attention 
        /// to DataMember if there is one.
        /// </devdoc>
        private IDataSourceFieldSchema[] GetFieldSchemas() {
            if (_fieldSchemas == null) { 
                IDataSourceViewSchema schema = null;
                if (_controlDesigner != null) { 
                    DesignerDataSourceView view = _controlDesigner.DesignerView; 
                    if (view != null) {
                        try { 
                            schema = view.Schema;
                        }
                        catch (Exception ex) {
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)ServiceProvider.GetService(typeof(IComponentDesignerDebugService)); 
                            if (debugService != null) {
                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message)); 
                            } 
                        }
                    } 
                }

                if (schema != null) {
                    _fieldSchemas = schema.GetFields(); 
                }
            } 
            return _fieldSchemas; 
        }
 
        private void IgnoreRefreshSchemaEvents() {
            _initialIgnoreRefreshSchemaValue = IgnoreRefreshSchema;
            IgnoreRefreshSchema = true;
            IDataSourceDesigner dsd = _controlDesigner.DataSourceDesigner; 
            if (dsd != null) {
                dsd.SuppressDataSourceEvents(); 
            } 
        }
 
        /// <devdoc>
        /// Initialized the form and calls functions to add controls to the form
        /// </devdoc>
        private void InitForm() { 
            SuspendLayout();
 
            _okButton = new Button(); 
            _cancelButton = new Button();
            _fieldLabel = new Label(); 
            _fieldList = new ComboBox();
            _refreshSchemaLink = new LinkLabel();
            _controlsPanel = new Panel();
 
            AddControls();
 
            IDataSourceDesigner dataSourceDesigner = _controlDesigner.DataSourceDesigner; 
            if (dataSourceDesigner != null) {
                if (dataSourceDesigner.CanRefreshSchema) { 
                    _refreshSchemaLink.Visible = true;
                }
            }
 
            this.Text = SR.GetString(SR.DCFAdd_Title);
            this.FormBorderStyle = FormBorderStyle.FixedDialog; 
            this.ClientSize = new Size(formWidth, formHeight); 
            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton; 
            this.Icon = null;

            InitializeForm();
 
            ResumeLayout(false);
            PerformLayout(); 
        } 

        /// <devdoc> 
        /// Handles the OK click event and closes the form.
        /// </devdoc>
        private void OnClickOKButton(object sender, EventArgs e) {
            DataControlFieldControl fieldControl = GetDataControlFieldControls()[_fieldList.SelectedIndex]; 
            DataBoundControl control = Control;
            if (control is GridView) { 
                ((GridView)control).Columns.Add(fieldControl.SaveValues()); 
            }
            else if (control is DetailsView) { 
                ((DetailsView)control).Fields.Add(fieldControl.SaveValues());
            }
        }
 
        /// <devdoc>
        ///   Refreshes the schema of the data bound control. 
        /// </devdoc> 
        private void OnClickRefreshSchema(object source, LinkLabelLinkClickedEventArgs e) {
            if (_controlDesigner != null) { 
                IDataSourceDesigner dataSourceDesigner = _controlDesigner.DataSourceDesigner;
                if (dataSourceDesigner != null) {
                    if (dataSourceDesigner.CanRefreshSchema) {
                        IDictionary preservedFields = GetDataControlFieldControls()[_fieldList.SelectedIndex].PreserveFields(); 

                        dataSourceDesigner.RefreshSchema(false); 
                        _fieldSchemas = GetFieldSchemas(); 

                        GetDataControlFieldControls()[0].RefreshSchema(_fieldSchemas); 
                        GetDataControlFieldControls()[1].RefreshSchema(GetBooleanFieldSchemas());
                        GetDataControlFieldControls()[2].RefreshSchema(_fieldSchemas);
                        GetDataControlFieldControls()[5].RefreshSchema(_fieldSchemas);
 
                        GetDataControlFieldControls()[_fieldList.SelectedIndex].RestoreFields(preservedFields);
                    } 
                } 
            }
        } 

        protected override void OnClosed(EventArgs e) {
            IDataSourceDesigner dsd = _controlDesigner.DataSourceDesigner;
            if (dsd != null) { 
                dsd.ResumeDataSourceEvents();
            } 
            IgnoreRefreshSchema = _initialIgnoreRefreshSchemaValue; 
        }
 
        /// <devdoc>
        /// Handles a selection change in the drop down that picks a field type
        /// </devdoc>
        private void OnSelectedFieldTypeChanged(object sender, EventArgs e) { 
            SetSelectedFieldControlVisible();
        } 
 
        private void SetSelectedFieldControlVisible() {
            foreach (DataControlFieldControl fieldControl in GetDataControlFieldControls()) { 
                fieldControl.Visible = false;
            }
            GetDataControlFieldControls()[_fieldList.SelectedIndex].Visible = true;
            this.Refresh(); 
        }
 
        private abstract class DataControlFieldControl : System.Windows.Forms.Control { 
            protected string[] _fieldSchemaNames;
            protected Type _controlType; 
            protected bool _haveSchema;
            protected IDataSourceFieldSchema[] _fieldSchemas;

            private Label _headerTextLabel; 
            private TextBox _headerTextBox;
 
            public DataControlFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) { 
                _fieldSchemas = fieldSchemas;
                if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                    _haveSchema = true;
                }
                _controlType = controlType;
                InitializeComponent(); 
            }
 
            public abstract string FieldName { 
                get;
            } 

            protected string[] GetFieldSchemaNames() {
                if (_fieldSchemaNames == null) {
                    if (_fieldSchemas != null) { 
                        int fields = _fieldSchemas.Length;
                        _fieldSchemaNames = new string[fields]; 
                        for (int i = 0; i < fields; i++) { 
                            _fieldSchemaNames[i] = _fieldSchemas[i].Name;
                        } 
                    }
                }

                return _fieldSchemaNames; 
            }
 
            /// <devdoc> 
            /// Adds all necessary controls to this controls' control tree
            /// </devdoc> 
            protected virtual void InitializeComponent() {
                _headerTextLabel = new Label();
                _headerTextBox = new TextBox();
 
                _headerTextLabel.Text = SR.GetString(SR.DCFAdd_HeaderText);
                _headerTextLabel.TextAlign = ContentAlignment.BottomLeft; 
                _headerTextLabel.SetBounds(0, 0, labelWidth, labelHeight); 

                _headerTextBox.TabIndex = 0; 
                _headerTextBox.SetBounds(0, labelHeight + labelPadding, textBoxWidth, controlHeight);

                this.Controls.AddRange(new System.Windows.Forms.Control[] {
                    _headerTextLabel, 
                    _headerTextBox});
            } 
 
            /// <devdoc>
            /// Called on the field in focus right before RefreshSchema is called 
            /// </devdoc>
            public IDictionary PreserveFields() {
                Hashtable table = new Hashtable();
                table["HeaderText"] = _headerTextBox.Text; 
                PreserveFields(table);
                return table; 
            } 

            protected abstract void PreserveFields(IDictionary table); 

            public void RefreshSchema(IDataSourceFieldSchema[] fieldSchemas) {
                _fieldSchemas = fieldSchemas;
                _fieldSchemaNames = null; 
                if (fieldSchemas != null && fieldSchemas.Length > 0) {
                    _haveSchema = true; 
                } 

                RefreshSchemaFields(); 
            }

            protected virtual void RefreshSchemaFields() {
                return; 
            }
 
            /// <devdoc> 
            /// Called on the field in focus after RefreshSchema is called
            /// </devdoc> 
            public void RestoreFields(IDictionary table) {
                _headerTextBox.Text = table["HeaderText"].ToString();
                RestoreFieldsInternal(table);
            } 

            protected abstract void RestoreFieldsInternal(IDictionary table); 
 
            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc>
            protected abstract DataControlField SaveValues(string headerText);

            /// <devdoc> 
            /// Called when OK is pressed to reflect the control's values back into the newly created field.
            /// </devdoc> 
            public DataControlField SaveValues() { 
                string headerText = _headerTextBox.Text;
                return SaveValues(headerText); 
            }

            protected string StripAccelerators(string text) {
                return text.Replace("&", String.Empty); 
            }
        } 
 
        private class BoundFieldControl : DataControlFieldControl {
            Label _dataFieldLabel; 
            ComboBox _dataFieldList;
            TextBox _dataFieldBox;
            CheckBox _readOnlyCheckBox;
 
            public BoundFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 
 
            public override string FieldName {
                get { 
                    return "BoundField";
                }
            }
 
            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc> 
            protected override void InitializeComponent() {
                base.InitializeComponent(); 
                _dataFieldList = new ComboBox();
                _dataFieldBox = new TextBox();
                _dataFieldLabel = new Label();
                _readOnlyCheckBox = new CheckBox(); 

                _dataFieldLabel.Text = SR.GetString(SR.DCFAdd_DataField); 
                _dataFieldLabel.TextAlign = ContentAlignment.BottomLeft; 
                _dataFieldLabel.SetBounds(0,
                                          labelHeight + labelPadding + vertPadding + controlHeight, 
                                          labelWidth,
                                          labelHeight);

                _dataFieldList.DropDownStyle = ComboBoxStyle.DropDownList; 
                _dataFieldList.TabIndex = 1;
                _dataFieldList.SetBounds(0, 
                                         (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight, 
                                         textBoxWidth,
                                         controlHeight); 
                _dataFieldList.SelectedIndexChanged += new EventHandler(OnSelectedDataFieldChanged);

                _dataFieldBox.TabIndex = 1;
                _dataFieldBox.SetBounds(0, 
                                        (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight,
                                        textBoxWidth, 
                                        controlHeight); 

                _readOnlyCheckBox.TabIndex = 2; 
                _readOnlyCheckBox.Text = SR.GetString(SR.DCFAdd_ReadOnly);
                _readOnlyCheckBox.SetBounds(0,
                                            (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2),
                                            textBoxWidth, 
                                            controlHeight);
 
                RefreshSchemaFields(); 

                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _dataFieldLabel,
                    _dataFieldBox,
                    _dataFieldList,
                    _readOnlyCheckBox}); 
            }
 
            private void OnSelectedDataFieldChanged(object sender, EventArgs e) { 
                if (_haveSchema) {
                    int dataFieldIndex = Array.IndexOf(GetFieldSchemaNames(), _dataFieldList.Text); 
                    if (dataFieldIndex >= 0) {
                        if (_fieldSchemas[dataFieldIndex].PrimaryKey) {
                            _readOnlyCheckBox.Checked = true;
                            return; 
                        }
                    } 
                } 
                _readOnlyCheckBox.Checked = false;
            } 

            protected override void PreserveFields(IDictionary table) {
                if (_haveSchema) {
                    table["DataField"] = _dataFieldList.Text; 
                }
                else { 
                    table["DataField"] = _dataFieldBox.Text; 
                }
                table["ReadOnly"] = _readOnlyCheckBox.Checked; 
            }

            protected override void RefreshSchemaFields() {
                if (_haveSchema) { 
                    _dataFieldList.Items.Clear();
                    _dataFieldList.Items.AddRange(GetFieldSchemaNames()); 
                    _dataFieldList.SelectedIndex = 0; 
                    _dataFieldList.Visible = true;
                    _dataFieldBox.Visible = false; 
                }
                else {
                    _dataFieldList.Visible = false;
                    _dataFieldBox.Visible = true; 
                }
            } 
 
            protected override void RestoreFieldsInternal(IDictionary table) {
                string dataField = table["DataField"].ToString(); 
                if (_haveSchema) {
                    if (dataField.Length > 0) {
                        bool foundItem = false;
                        foreach (object listItem in _dataFieldList.Items) { 
                            if (String.Compare(dataField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _dataFieldList.SelectedItem = listItem; 
                                foundItem = true; 
                                break;
                            } 
                        }
                        if (!foundItem) {
                            _dataFieldList.Items.Insert(0, dataField);
                            _dataFieldList.SelectedIndex = 0; 
                        }
                    } 
                } 
                else {
                    _dataFieldBox.Text = dataField; 
                }
                _readOnlyCheckBox.Checked = (bool)table["ReadOnly"];
            }
 
            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc> 
            protected override DataControlField SaveValues(string headerText) {
                BoundField field = new BoundField(); 
                field.HeaderText = headerText;

                if (_haveSchema) {
                    field.DataField = _dataFieldList.Text; 
                }
                else { 
                    field.DataField = _dataFieldBox.Text; 
                }
                field.ReadOnly = _readOnlyCheckBox.Checked; 
                field.SortExpression = field.DataField;
                return field;
            }
        } 

        private class CheckBoxFieldControl : DataControlFieldControl { 
            Label _dataFieldLabel; 
            ComboBox _dataFieldList;
            TextBox _dataFieldBox; 
            CheckBox _readOnlyCheckBox;

            public CheckBoxFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 

            public override string FieldName { 
                get { 
                    return "CheckBoxField";
                } 
            }

            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc>
            protected override void InitializeComponent() { 
                base.InitializeComponent(); 
                _dataFieldList = new ComboBox();
                _dataFieldBox = new TextBox(); 
                _dataFieldLabel = new Label();
                _readOnlyCheckBox = new CheckBox();

                _dataFieldLabel.Text = SR.GetString(SR.DCFAdd_DataField); 
                _dataFieldLabel.TextAlign = ContentAlignment.BottomLeft;
                _dataFieldLabel.SetBounds(0, 
                                          labelHeight + labelPadding + vertPadding + controlHeight, 
                                          labelWidth,
                                          labelHeight); 

                _dataFieldList.DropDownStyle = ComboBoxStyle.DropDownList;
                _dataFieldList.TabIndex = 1;
                _dataFieldList.SetBounds(0, 
                                         (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight,
                                         textBoxWidth, 
                                         controlHeight); 

                _dataFieldBox.TabIndex = 1; 
                _dataFieldBox.SetBounds(0,
                                        (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight,
                                        textBoxWidth,
                                        controlHeight); 

                _readOnlyCheckBox.TabIndex = 2; 
                _readOnlyCheckBox.Text = SR.GetString(SR.DCFAdd_ReadOnly); 
                _readOnlyCheckBox.SetBounds(0,
                                            (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2), 
                                            textBoxWidth,
                                            controlHeight);

 
                RefreshSchemaFields();
 
                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _dataFieldLabel,
                    _dataFieldBox, 
                    _dataFieldList,
                    _readOnlyCheckBox});
            }
 
            protected override void PreserveFields(IDictionary table) {
                if (_haveSchema) { 
                    table["DataField"] = _dataFieldList.Text; 
                }
                else { 
                    table["DataField"] = _dataFieldBox.Text;
                }
                table["ReadOnly"] = _readOnlyCheckBox.Checked;
            } 

            protected override void RefreshSchemaFields() { 
                if (_haveSchema) { 
                    _dataFieldList.Items.Clear();
                    _dataFieldList.Items.AddRange(GetFieldSchemaNames()); 
                    _dataFieldList.SelectedIndex = 0;
                    _dataFieldList.Visible = true;
                    _dataFieldBox.Visible = false;
                } 
                else {
                    _dataFieldList.Visible = false; 
                    _dataFieldBox.Visible = true; 
                }
            } 

            protected override void RestoreFieldsInternal(IDictionary table) {
                string dataField = table["DataField"].ToString();
                if (_haveSchema) { 
                    if (dataField.Length > 0) {
                        bool foundItem = false; 
                        foreach (object listItem in _dataFieldList.Items) { 
                            if (String.Compare(dataField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _dataFieldList.SelectedItem = listItem; 
                                foundItem = true;
                                break;
                            }
                        } 
                        if (!foundItem) {
                            _dataFieldList.Items.Insert(0, dataField); 
                            _dataFieldList.SelectedIndex = 0; 
                        }
                    } 
                }
                else {
                    _dataFieldBox.Text = dataField;
                } 
                _readOnlyCheckBox.Checked = (bool)table["ReadOnly"];
            } 
 
            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc>
            protected override DataControlField SaveValues(string headerText) {
                CheckBoxField field = new CheckBoxField();
                field.HeaderText = headerText; 
                if (_haveSchema) {
                    field.DataField = _dataFieldList.Text; 
                } 
                else {
                    field.DataField = _dataFieldBox.Text; 
                }
                field.ReadOnly = _readOnlyCheckBox.Checked;
                field.SortExpression = field.DataField;
                return field; 
            }
        } 
 
        private class ButtonFieldControl : DataControlFieldControl {
            Label _buttonTypeLabel; 
            Label _commandNameLabel;
            Label _textLabel;
            ComboBox _buttonTypeList;
            ComboBox _commandNameList; 
            TextBox _textBox;
 
 
            public ButtonFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 

            public override string FieldName {
                get {
                    return "ButtonField"; 
                }
            } 
 
            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc>
            protected override void InitializeComponent() {
                base.InitializeComponent();
 
                _buttonTypeLabel = new Label();
                _commandNameLabel = new Label(); 
                _textLabel = new Label(); 
                _buttonTypeList = new ComboBox();
                _commandNameList = new ComboBox(); 
                _textBox = new TextBox();

                _buttonTypeLabel.Text = SR.GetString(SR.DCFAdd_ButtonType);
                _buttonTypeLabel.TextAlign = ContentAlignment.BottomLeft; 
                _buttonTypeLabel.SetBounds(0,
                                           labelHeight + labelPadding + vertPadding + controlHeight, 
                                           labelWidth, 
                                           labelHeight);
 
                _buttonTypeList.Items.Add(ButtonType.Link.ToString());
                _buttonTypeList.Items.Add(ButtonType.Button.ToString());
                _buttonTypeList.SelectedIndex = 0;
                _buttonTypeList.DropDownStyle = ComboBoxStyle.DropDownList; 
                _buttonTypeList.TabIndex= 1;
                _buttonTypeList.SetBounds(0, 
                                          (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight, 
                                          textBoxWidth,
                                          controlHeight); 

                _commandNameLabel.Text = SR.GetString(SR.DCFAdd_CommandName);
                _commandNameLabel.TextAlign = ContentAlignment.BottomLeft;
                _commandNameLabel.SetBounds(0, 
                                            (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2),
                                            labelWidth, 
                                            labelHeight); 

                _commandNameList.Items.Add("Cancel"); 
                _commandNameList.Items.Add("Delete");
                _commandNameList.Items.Add("Edit");
                _commandNameList.Items.Add("Update");
                if (_controlType == typeof(DetailsView)) { 
                    _commandNameList.Items.Insert(3, "Insert");
                    _commandNameList.Items.Insert(4, "New"); 
                } 
                else {
                    if (_controlType == typeof(GridView)) { 
                        _commandNameList.Items.Insert(3, "Select");
                    }
                }
                _commandNameList.SelectedIndex = 0; 
                _commandNameList.DropDownStyle = ComboBoxStyle.DropDownList;
                _commandNameList.TabIndex = 2; 
                _commandNameList.SetBounds(0, 
                                           (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 2),
                                           textBoxWidth, 
                                           controlHeight);

                _textLabel.Text = SR.GetString(SR.DCFAdd_Text);
                _textLabel.TextAlign = ContentAlignment.BottomLeft; 
                _textLabel.SetBounds(0,
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 3) + (controlHeight * 3), 
                                     labelWidth, 
                                     labelHeight);
 
                _textBox.TabIndex = 3;
                _textBox.Text = SR.GetString(SR.DCFEditor_Button);
                _textBox.SetBounds(0,
                                   (labelHeight * 4) + (labelPadding * 4) + (vertPadding * 3) + (controlHeight * 3), 
                                   textBoxWidth,
                                   controlHeight); 
 
                Controls.AddRange(new System.Windows.Forms.Control[] {
                    _buttonTypeLabel, 
                    _commandNameLabel,
                    _textLabel,
                    _buttonTypeList,
                    _commandNameList, 
                    _textBox});
            } 
 
            protected override void PreserveFields(IDictionary table) {
                table["ButtonType"] = _buttonTypeList.SelectedIndex; 
                table["CommandName"] = _commandNameList.SelectedIndex;
                table["Text"] = _textBox.Text;
            }
 
            protected override void RestoreFieldsInternal(IDictionary table) {
                _buttonTypeList.SelectedIndex = (int)table["ButtonType"]; 
                _commandNameList.SelectedIndex = (int)table["CommandName"]; 
                _textBox.Text = table["Text"].ToString();
            } 

            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field.
            /// </devdoc> 
            protected override DataControlField SaveValues(string headerText) {
                ButtonField field = new ButtonField(); 
                if (headerText != null && headerText.Length > 0) { 
                    field.HeaderText = headerText;
                    field.ShowHeader = true; 
                }
                field.CommandName = _commandNameList.Text;
                field.Text = _textBox.Text;
                if (_buttonTypeList.SelectedIndex == 0) { 
                    field.ButtonType = ButtonType.Link;
                } 
                else { 
                    field.ButtonType = ButtonType.Button;
                } 
                return field;
            }
        }
 
        private class HyperLinkFieldControl : DataControlFieldControl {
            TextBox _dataTextFieldBox; 
            TextBox _dataNavFieldBox; 
            TextBox _dataNavFSBox;
            TextBox _textBox; 
            TextBox _textFSBox;
            TextBox _linkBox;
            ComboBox _dataTextFieldList;
            ComboBox _dataNavFieldList; 
            RadioButton _staticTextRadio;
            RadioButton _bindTextRadio; 
            RadioButton _staticUrlRadio; 
            RadioButton _bindUrlRadio;
            Label _linkTextFormatStringLabel; 
            Label _linkUrlFormatStringLabel;
            Label _linkTextFormatStringExampleLabel;
            Label _linkUrlFormatStringExampleLabel;
            GroupBox _textGroupBox; 
            GroupBox _linkGroupBox;
            Panel _staticTextPanel; 
            Panel _bindTextPanel; 
            Panel _staticUrlPanel;
            Panel _bindUrlPanel; 

            public HyperLinkFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            }
 
            public override string FieldName {
                get { 
                    return "HyperLinkField"; 
                }
            } 

            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree
            /// </devdoc> 
            protected override void InitializeComponent() {
                base.InitializeComponent(); 
 
                _dataTextFieldBox = new TextBox();
                _dataNavFieldBox = new TextBox(); 
                _dataNavFSBox = new TextBox();
                _linkBox = new TextBox();
                _textBox = new TextBox();
                _textFSBox = new TextBox(); 
                _dataTextFieldList = new ComboBox();
                _dataNavFieldList = new ComboBox(); 
                _staticTextRadio = new RadioButton(); 
                _bindTextRadio = new RadioButton();
                _staticUrlRadio = new RadioButton(); 
                _bindUrlRadio = new RadioButton();
                _linkTextFormatStringLabel = new Label();
                _linkUrlFormatStringLabel = new Label();
                _linkTextFormatStringExampleLabel = new Label(); 
                _linkUrlFormatStringExampleLabel = new Label();
                _textGroupBox = new GroupBox(); 
                _linkGroupBox = new GroupBox(); 
                _staticTextPanel = new Panel();
                _bindTextPanel = new Panel(); 
                _staticUrlPanel = new Panel();
                _bindUrlPanel = new Panel();

                const int boxOffset = 9; 

                // text 
                _textGroupBox.SetBounds(0, 
                                        labelHeight + labelPadding + (vertPadding * 2) + controlHeight,
                                        labelWidth + 20, 
                                        (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 3) + (controlHeight * 5));
                _textGroupBox.Text = SR.GetString(SR.DCFAdd_HyperlinkText);
                _textGroupBox.TabIndex = 1;
 
                // static text radio
                _staticTextRadio.TabIndex = 0; 
                _staticTextRadio.Text = SR.GetString(SR.DCFAdd_SpecifyText); 
                _staticTextRadio.CheckedChanged += new EventHandler(OnTextRadioChanged);
                _staticTextRadio.Checked = true; 
                _staticTextRadio.SetBounds(boxOffset,
                                         labelHeight + labelPadding,
                                         labelWidth - boxOffset,
                                         controlHeight); 

 
                // _staticTextPanel contents 
                _textBox.TabIndex = 0;
                _textBox.SetBounds(0, 
                                   0,
                                   textBoxWidth - 15 - boxOffset,
                                   controlHeight);
                _textBox.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_SpecifyText)); 
                // end _staticTextPanel contents
 
                _staticTextPanel.TabIndex = 1; 
                _staticTextPanel.SetBounds(15 + boxOffset,
                                           labelHeight + labelPadding + (controlHeight * 1), 
                                           textBoxWidth - 15 - boxOffset,
                                           controlHeight + vertPadding);
                _staticTextPanel.Controls.Add(_textBox);
 
                // bind text radio
                _bindTextRadio.TabIndex = 2; 
                _bindTextRadio.Text = SR.GetString(SR.DCFAdd_BindText); 
                _bindTextRadio.SetBounds(boxOffset,
                                   (labelHeight * 1) + labelPadding + (controlHeight * 2) + vertPadding, 
                                   labelWidth - boxOffset,
                                   controlHeight);

 
                // _bindTextPanel contents
                _dataTextFieldList.TabIndex = 0; 
                _dataTextFieldList.SetBounds(0, 
                                             0,
                                             textBoxWidth - 15 - boxOffset, 
                                             controlHeight);
                _dataTextFieldList.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_BindText));

                _dataTextFieldBox.TabIndex = 1; 
                _dataTextFieldBox.SetBounds(0,
                                            0, 
                                             textBoxWidth - 15 - boxOffset, 
                                             controlHeight);
                _dataTextFieldBox.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_BindText)); 

                _linkTextFormatStringLabel.Text = SR.GetString(SR.DCFAdd_TextFormatString);
                _linkTextFormatStringLabel.TabIndex = 2;
                _linkTextFormatStringLabel.TextAlign = ContentAlignment.BottomLeft; 
                _linkTextFormatStringLabel.SetBounds(0,
                                             (controlHeight * 1), 
                                             labelWidth - 15 - boxOffset, 
                                             labelHeight);
 
                _textFSBox.TabIndex = 3;
                _textFSBox.SetBounds(0,
                                     (labelHeight * 1) + labelPadding + (controlHeight * 1),
                                     textBoxWidth - 15 - boxOffset, 
                                     controlHeight);
 
                _linkTextFormatStringExampleLabel.Text = SR.GetString(SR.DCFAdd_TextFormatStringExample); 
                _linkTextFormatStringExampleLabel.Enabled = false;
                _linkTextFormatStringExampleLabel.TextAlign = ContentAlignment.BottomLeft; 
                _linkTextFormatStringExampleLabel.SetBounds(0,
                                                             (labelHeight * 1) + labelPadding + (controlHeight * 2),
                                                             textBoxWidth - 15 - boxOffset,
                                                             labelHeight); 
                // end _bindTextPanel contents
 
                _bindTextPanel.TabIndex = 3; 
                _bindTextPanel.SetBounds(15 + boxOffset,
                         (labelHeight * 1) + labelPadding + (controlHeight * 3) + vertPadding, 
                         labelWidth - 15 - boxOffset,
                         (labelHeight * 2) + (labelPadding * 2) + (controlHeight * 2));

                _bindTextPanel.Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _bindTextRadio,
                    _dataTextFieldList, 
                    _dataTextFieldBox, 
                    _linkTextFormatStringLabel,
                    _textFSBox, 
                    _linkTextFormatStringExampleLabel});


                _textGroupBox.Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _staticTextRadio,
                    _staticTextPanel, 
                    _bindTextRadio, 
                    _bindTextPanel});
 
                // url
                _linkGroupBox.SetBounds(0,
                                        (labelHeight * 4) + (labelPadding * 4) + (vertPadding * 6) + (controlHeight * 6),
                                        labelWidth + 20, 
                                        (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 4) + (controlHeight * 5));
                _linkGroupBox.Text = SR.GetString(SR.DCFAdd_HyperlinkURL); 
                _linkGroupBox.TabIndex = 2; 

                // staticUrlRadio 
                _staticUrlRadio.TabIndex = 0;
                _staticUrlRadio.Text = SR.GetString(SR.DCFAdd_SpecifyURL);
                _staticUrlRadio.CheckedChanged += new EventHandler(OnUrlRadioChanged);
                _staticUrlRadio.Checked = true; 
                _staticUrlRadio.SetBounds(boxOffset,
                                         (labelHeight * 1) + labelPadding, 
                                         labelWidth - boxOffset, 
                                         controlHeight);
 
                // _staticUrlPanel contents
                _linkBox.TabIndex = 0;
                _linkBox.SetBounds(0,
                                   0, 
                                   textBoxWidth - 15 - boxOffset,
                                   controlHeight); 
                _linkBox.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_SpecifyURL)); 
                // end _staticUrlPanel contents
 
                _staticUrlPanel.TabIndex = 1;
                _staticUrlPanel.SetBounds(15 + boxOffset,
                                   (labelHeight * 1) + labelPadding + (controlHeight * 1),
                                   textBoxWidth - 15 - boxOffset, 
                                   controlHeight + vertPadding);
                _staticUrlPanel.Controls.Add(_linkBox); 
 
                // _bindUrlRadio
                _bindUrlRadio.TabIndex = 2; 
                _bindUrlRadio.Text = SR.GetString(SR.DCFAdd_BindURL);
                _bindUrlRadio.SetBounds(boxOffset,
                                   (labelHeight * 1) + labelPadding + (controlHeight * 2) + vertPadding,
                                   labelWidth - boxOffset, 
                                   controlHeight);
 
                // _bindUrlPanel contents 
                _dataNavFieldList.TabIndex = 0;
                _dataNavFieldList.SetBounds(0, 
                                            0,
                                             textBoxWidth - 15 - boxOffset,
                                             controlHeight);
                _dataNavFieldList.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_BindURL)); 

                _dataNavFieldBox.TabIndex = 1; 
                _dataNavFieldBox.SetBounds(0, 
                                           0,
                                             textBoxWidth - 15 - boxOffset, 
                                             controlHeight);
                _dataNavFieldBox.AccessibleName = StripAccelerators(SR.GetString(SR.DCFAdd_BindURL));

                _linkUrlFormatStringLabel.Text = SR.GetString(SR.DCFAdd_URLFormatString); 
                _linkUrlFormatStringLabel.TabIndex = 2;
                _linkUrlFormatStringLabel.TextAlign = ContentAlignment.BottomLeft; 
                _linkUrlFormatStringLabel.SetBounds(0, 
                                             (controlHeight * 1),
                                             labelWidth - 15 - boxOffset, 
                                             labelHeight);

                _dataNavFSBox.TabIndex = 3;
                _dataNavFSBox.SetBounds(0, 
                                     (labelHeight * 1) + labelPadding + (controlHeight * 1),
                                     textBoxWidth - 15 - boxOffset, 
                                     controlHeight); 

                _linkUrlFormatStringExampleLabel.Text = SR.GetString(SR.DCFAdd_URLFormatStringExample); 
                _linkUrlFormatStringExampleLabel.Enabled = false;
                _linkUrlFormatStringExampleLabel.TextAlign = ContentAlignment.BottomLeft;
                _linkUrlFormatStringExampleLabel.SetBounds(0,
                                                         (labelHeight * 1) + labelPadding + (controlHeight * 2), 
                                                         textBoxWidth - 15 - boxOffset,
                                                         labelHeight); 
                // end _bindUrlPanel contents 
                _bindUrlPanel.TabIndex = 3;
                _bindUrlPanel.SetBounds(15 + boxOffset, 
                                        (labelHeight * 1) + labelPadding + (controlHeight * 3) + vertPadding,
                                        labelWidth - 15 - boxOffset,
                                        (labelHeight * 2) + (labelPadding * 2) + (controlHeight * 2));
 
                _bindUrlPanel.Controls.AddRange(new System.Windows.Forms.Control[] {
                    _dataNavFieldList, 
                    _dataNavFieldBox, 
                    _linkUrlFormatStringLabel,
                    _dataNavFSBox, 
                    _linkUrlFormatStringExampleLabel});


                _linkGroupBox.Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _staticUrlRadio,
                    _staticUrlPanel, 
                    _bindUrlRadio, 
                    _bindUrlPanel});
 
                // end
                RefreshSchemaFields();

                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _textGroupBox,
                    _linkGroupBox}); 
 
            }
 
            private void OnTextRadioChanged(object sender, EventArgs e) {
                if (_staticTextRadio.Checked) {
                    _textBox.Enabled = true;
                    _dataTextFieldList.Enabled = false; 
                    _dataTextFieldBox.Enabled = false;
                    _textFSBox.Enabled = false; 
                    _linkTextFormatStringLabel.Enabled = false; 
                }
                else { 
                    _textBox.Enabled = false;
                    _dataTextFieldList.Enabled = true;
                    _dataTextFieldBox.Enabled = true;
                    _textFSBox.Enabled = true; 
                    _linkTextFormatStringLabel.Enabled = true;
                } 
            } 

            private void OnUrlRadioChanged(object sender, EventArgs e) { 
                if (_staticUrlRadio.Checked) {
                    _linkBox.Enabled = true;
                    _dataNavFieldList.Enabled = false;
                    _dataNavFieldBox.Enabled = false; 
                    _dataNavFSBox.Enabled = false;
                    _linkUrlFormatStringLabel.Enabled = false; 
                } 
                else {
                    _linkBox.Enabled = false; 
                    _dataNavFieldList.Enabled = true;
                    _dataNavFieldBox.Enabled = true;
                    _dataNavFSBox.Enabled = true;
                    _linkUrlFormatStringLabel.Enabled = true; 
                }
            } 
 
            protected override void PreserveFields(IDictionary table) {
                if (_haveSchema) { 
                    table["DataTextField"] = _dataTextFieldList.Text;
                    table["DataNavigateUrlField"] = _dataNavFieldList.Text;
                }
                else { 
                    table["DataTextField"] = _dataTextFieldBox.Text;
                    table["DataNavigateUrlField"] = _dataNavFieldBox.Text; 
                } 
                table["DataNavigateUrlFormatString"] = _dataNavFSBox.Text;
                table["DataTextFormatString"] = _textFSBox.Text; 
                table["NavigateUrl"] = _linkBox.Text;
                table["linkMode"] = _staticUrlRadio.Checked;
                table["textMode"] = _staticTextRadio.Checked;
                table["Text"] = _textBox.Text; 
            }
 
            protected override void RefreshSchemaFields() { 
                if (_haveSchema) {
                    _dataTextFieldList.Items.Clear(); 
                    _dataTextFieldList.Items.AddRange(GetFieldSchemaNames());
                    _dataTextFieldList.Items.Insert(0, String.Empty);
                    _dataTextFieldList.SelectedIndex = 0;
                    _dataTextFieldList.Visible = true; 
                    _dataTextFieldBox.Visible = false;
 
                    _dataNavFieldList.Items.Clear(); 
                    _dataNavFieldList.Items.AddRange(GetFieldSchemaNames());
                    _dataNavFieldList.Items.Insert(0, String.Empty); 
                    _dataNavFieldList.SelectedIndex = 0;
                    _dataNavFieldList.Visible = true;
                    _dataNavFieldBox.Visible = false;
                } 
                else {
                    _dataTextFieldList.Visible = false; 
                    _dataTextFieldBox.Visible = true; 
                    _dataNavFieldList.Visible = false;
                    _dataNavFieldBox.Visible = true; 
                }
            }

            protected override void RestoreFieldsInternal(IDictionary table) { 
                string dataTextField = table["DataTextField"].ToString();
                string dataNavigateUrlField = table["DataNavigateUrlField"].ToString(); 
                if (_haveSchema) { 
                    bool foundItem = false;
                    if (dataTextField.Length > 0) { 
                        foreach (object listItem in _dataTextFieldList.Items) {
                            if (String.Compare(dataTextField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _dataTextFieldList.SelectedItem = listItem;
                                foundItem = true; 
                                break;
                            } 
                        } 
                        if (!foundItem) {
                            _dataTextFieldList.Items.Insert(0, dataTextField); 
                            _dataTextFieldList.SelectedIndex = 0;
                        }
                    }
 
                    if (dataNavigateUrlField.Length > 0) {
                        foundItem = false; 
                        foreach (object listItem in _dataNavFieldList.Items) { 
                            if (String.Compare(dataNavigateUrlField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _dataNavFieldList.SelectedItem = listItem; 
                                foundItem = true;
                                break;
                            }
                        } 
                        if (!foundItem) {
                            _dataNavFieldList.Items.Insert(0, dataNavigateUrlField); 
                            _dataNavFieldList.SelectedIndex = 0; 
                        }
                    } 
                }
                else {
                    _dataTextFieldBox.Text = dataTextField;
                    _dataNavFieldBox.Text = dataNavigateUrlField; 
                }
                _dataNavFSBox.Text = table["DataNavigateUrlFormatString"].ToString(); 
                _textFSBox.Text = table["DataTextFormatString"].ToString(); 
                _linkBox.Text = table["NavigateUrl"].ToString();
                _textBox.Text = table["Text"].ToString(); 
                _staticUrlRadio.Checked = (bool)table["linkMode"];
                _staticTextRadio.Checked = (bool)table["textMode"];
            }
 
            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc> 
            protected override DataControlField SaveValues(string headerText) {
                HyperLinkField field = new HyperLinkField(); 
                field.HeaderText = headerText;

                if (_staticTextRadio.Checked) {
                    field.Text = _textBox.Text; 
                }
                else { 
                    field.DataTextFormatString = _textFSBox.Text; 
                    if (_haveSchema) {
                        field.DataTextField = _dataTextFieldList.Text; 
                    }
                    else {
                        field.DataTextField = _dataTextFieldBox.Text;
                    } 

                } 
 
                if (_staticUrlRadio.Checked) {
                    field.NavigateUrl = _linkBox.Text; 
                }
                else {
                    field.DataNavigateUrlFormatString = _dataNavFSBox.Text;
                    if (_haveSchema) { 
                        field.DataNavigateUrlFields = new string[1]{_dataNavFieldList.Text};
                    } 
                    else { 
                        field.DataNavigateUrlFields = new string[1]{_dataNavFieldBox.Text};
                    } 
                }
                return field;
            }
        } 

        private class CommandFieldControl : DataControlFieldControl { 
            Label _buttonTypeLabel; 
            Label _commandButtonsLabel;
            ComboBox _buttonTypeList; 
            CheckBox _deleteBox;
            CheckBox _selectBox;
            CheckBox _cancelBox;
            CheckBox _updateBox; 
            CheckBox _insertBox;
            const int checkBoxLeft = 8; 
 
            public CommandFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 

            public override string FieldName {
                get {
                    return "CommandField"; 
                }
            } 
 
            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc>
            protected override void InitializeComponent() {
                base.InitializeComponent();
                _buttonTypeLabel = new Label(); 
                _buttonTypeList = new ComboBox();
                _commandButtonsLabel = new Label(); 
                _deleteBox = new CheckBox(); 
                _selectBox = new CheckBox();
                _cancelBox = new CheckBox(); 
                _updateBox = new CheckBox();
                _insertBox = new CheckBox();

 
                _buttonTypeLabel.Text = SR.GetString(SR.DCFAdd_ButtonType);
                _buttonTypeLabel.TextAlign = ContentAlignment.BottomLeft; 
                _buttonTypeLabel.SetBounds(0, 
                                           labelHeight + labelPadding + vertPadding + controlHeight,
                                           labelWidth, 
                                           labelHeight);

                _buttonTypeList.Items.Add(ButtonType.Link.ToString());
                _buttonTypeList.Items.Add(ButtonType.Button.ToString()); 
                _buttonTypeList.SelectedIndex = 0;
                _buttonTypeList.DropDownStyle = ComboBoxStyle.DropDownList; 
                _buttonTypeList.TabIndex= 1; 
                _buttonTypeList.SetBounds(0,
                                          (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight, 
                                          textBoxWidth,
                                          controlHeight);

                _commandButtonsLabel.Text = SR.GetString(SR.DCFAdd_CommandButtons); 
                _commandButtonsLabel.TextAlign = ContentAlignment.BottomLeft;
                _commandButtonsLabel.SetBounds(0, 
                                               (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2), 
                                               labelWidth,
                                               labelHeight); 

                _deleteBox.Text = SR.GetString(SR.DCFAdd_Delete);
                _deleteBox.AccessibleDescription = SR.GetString(SR.DCFAdd_DeleteDesc);
                _deleteBox.TextAlign = ContentAlignment.TopLeft; 
                _deleteBox.CheckAlign = ContentAlignment.TopLeft;
                _deleteBox.TabIndex = 2; 
                _deleteBox.SetBounds(checkBoxLeft, 
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 2),
                                     checkBoxWidth, 
                                     controlHeight);

                _selectBox.Text = SR.GetString(SR.DCFAdd_Select);
                _selectBox.AccessibleDescription = SR.GetString(SR.DCFAdd_SelectDesc); 
                _selectBox.TextAlign = ContentAlignment.TopLeft;
                _selectBox.CheckAlign = ContentAlignment.TopLeft; 
                _selectBox.TabIndex = 4; 
                _selectBox.SetBounds(checkBoxLeft,
                                    (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 3), 
                                     checkBoxWidth,
                                     controlHeight);

                _cancelBox.Text = SR.GetString(SR.DCFAdd_ShowCancel); 
                _cancelBox.AccessibleDescription = SR.GetString(SR.DCFAdd_ShowCancelDesc);
                _cancelBox.TextAlign = ContentAlignment.TopLeft; 
                _cancelBox.CheckAlign = ContentAlignment.TopLeft; 
                _cancelBox.Enabled = false;
                _cancelBox.Checked = true; 
                _cancelBox.TabIndex = 6;
                _cancelBox.SetBounds(checkBoxLeft,
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 4),
                                     textBoxWidth, 
                                     controlHeight * 2 + vertPadding);
 
                _updateBox.Text = SR.GetString(SR.DCFAdd_EditUpdate); 
                _updateBox.AccessibleDescription = SR.GetString(SR.DCFAdd_EditUpdateDesc);
                _updateBox.TextAlign = ContentAlignment.TopLeft; 
                _updateBox.CheckAlign = ContentAlignment.TopLeft;
                _updateBox.TabIndex = 3;
                _updateBox.CheckedChanged += new EventHandler(this.OnCheckedChanged);
                _updateBox.SetBounds(checkBoxWidth + horizPadding + checkBoxLeft, 
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 2),
                                     checkBoxWidth, 
                                     controlHeight); 

                _insertBox.Text = SR.GetString(SR.DCFAdd_NewInsert); 
                _insertBox.AccessibleDescription = SR.GetString(SR.DCFAdd_NewInsertDesc);
                _insertBox.TextAlign = ContentAlignment.TopLeft;
                _insertBox.CheckAlign = ContentAlignment.TopLeft;
                _insertBox.TabIndex = 5; 
                _insertBox.CheckedChanged += new EventHandler(this.OnCheckedChanged);
                _insertBox.SetBounds(checkBoxLeft, 
                                     (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 3), 
                                     checkBoxWidth,
                                     controlHeight); 

                if (_controlType == typeof(GridView)) {
                    _insertBox.Visible = false;
                } 
                else if (_controlType == typeof(DetailsView)) {
                    _selectBox.Visible = false; 
                } 

                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _buttonTypeLabel,
                    _buttonTypeList,
                    _commandButtonsLabel,
                    _deleteBox, 
                    _selectBox,
                    _cancelBox, 
                    _updateBox, 
                    _insertBox });
            } 

            private void OnCheckedChanged(object sender, EventArgs e) {
                _cancelBox.Enabled = _updateBox.Checked || _insertBox.Checked;
            } 

            protected override void PreserveFields(IDictionary table) { 
                table["ButtonType"] = _buttonTypeList.SelectedIndex; 
                table["ShowDeleteButton"] = _deleteBox.Checked;
                table["ShowSelectButton"] = _selectBox.Checked; 
                table["ShowCancelButton"] = _cancelBox.Checked;
                table["ShowEditButton"] = _updateBox.Checked;
                table["ShowInsertButton"] = _insertBox.Checked;
            } 

            protected override void RestoreFieldsInternal(IDictionary table) { 
                _buttonTypeList.SelectedIndex = (int)table["ButtonType"]; 
                _deleteBox.Checked = (bool)table["ShowDeleteButton"];
                _selectBox.Checked = (bool)table["ShowSelectButton"]; 
                _cancelBox.Checked = (bool)table["ShowCancelButton"];
                _updateBox.Checked = (bool)table["ShowEditButton"];
                _insertBox.Checked = (bool)table["ShowInsertButton"];
            } 

            /// <devdoc> 
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc>
            protected override DataControlField SaveValues(string headerText) { 
                CommandField field = new CommandField();
                if (headerText != null && headerText.Length > 0) {
                    field.HeaderText = headerText;
                    field.ShowHeader = true; 
                }
                if (_buttonTypeList.SelectedIndex == 0) { 
                    field.ButtonType = ButtonType.Link; 
                }
                else { 
                    field.ButtonType = ButtonType.Button;
                }

                field.ShowDeleteButton = _deleteBox.Checked; 
                field.ShowSelectButton = _selectBox.Checked;
                if (_cancelBox.Enabled) { 
                    field.ShowCancelButton = _cancelBox.Checked; 
                }
                field.ShowEditButton = _updateBox.Checked; 
                field.ShowInsertButton = _insertBox.Checked;

                return field;
            } 
        }
 
        private class TemplateFieldControl : DataControlFieldControl { 
            public TemplateFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) {
            } 

            public override string FieldName {
                get {
                    return "TemplateField"; 
                }
            } 
 
            /// <devdoc>
            /// Adds all necessary controls to this controls' control tree 
            /// </devdoc>
            protected override void InitializeComponent() {
                base.InitializeComponent();
            } 

            protected override void PreserveFields(IDictionary table) { 
            } 

            protected override void RestoreFieldsInternal(IDictionary table) { 
            }

            /// <devdoc>
            /// Called when OK is pressed to reflect the control's values back into the newly created field. 
            /// </devdoc>
            protected override DataControlField SaveValues(string headerText) { 
                TemplateField field = new TemplateField(); 
                field.HeaderText = headerText;
                return field; 
            }
        }

        private class ImageFieldControl : DataControlFieldControl { 
            Label _imageUrlFieldLabel;
            ComboBox _imageUrlFieldList; 
            TextBox _imageUrlFieldBox; 
            CheckBox _readOnlyCheckBox;
            TextBox _urlFormatBox; 
            Label _urlFormatBoxLabel;
            Label _urlFormatExampleLabel;

            public ImageFieldControl(IDataSourceFieldSchema[] fieldSchemas, Type controlType) : base(fieldSchemas, controlType) { 
            }
 
            public override string FieldName { 
                get {
                    return "ImageField"; 
                }
            }

            /// <devdoc> 
            /// Adds all necessary controls to this controls' control tree
            /// </devdoc> 
            protected override void InitializeComponent() { 
                base.InitializeComponent();
                _imageUrlFieldList = new ComboBox(); 
                _imageUrlFieldBox = new TextBox();
                _imageUrlFieldLabel = new Label();
                _readOnlyCheckBox = new CheckBox();
                _urlFormatBox = new TextBox(); 
                _urlFormatBoxLabel = new Label();
                _urlFormatExampleLabel = new Label(); 
 

                _imageUrlFieldLabel.Text = SR.GetString(SR.DCFAdd_DataField); 
                _imageUrlFieldLabel.TextAlign = ContentAlignment.BottomLeft;
                _imageUrlFieldLabel.SetBounds(0,
                                          labelHeight + labelPadding + vertPadding + controlHeight,
                                          labelWidth, 
                                          labelHeight);
 
                _imageUrlFieldList.DropDownStyle = ComboBoxStyle.DropDownList; 
                _imageUrlFieldList.TabIndex = 1;
                _imageUrlFieldList.SetBounds(0, 
                                         (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight,
                                         textBoxWidth,
                                         controlHeight);
 
                _imageUrlFieldBox.TabIndex = 2;
                _imageUrlFieldBox.SetBounds(0, 
                                        (labelHeight * 2) + (labelPadding * 2) + vertPadding + controlHeight, 
                                        textBoxWidth,
                                        controlHeight); 

                _urlFormatBoxLabel.TabIndex = 3;
                _urlFormatBoxLabel.Text = SR.GetString(SR.DCFAdd_LinkFormatString);
                _urlFormatBoxLabel.TextAlign = ContentAlignment.BottomLeft; 
                _urlFormatBoxLabel.SetBounds(0,
                                    (labelHeight * 2) + (labelPadding * 2) + (vertPadding * 2) + (controlHeight * 2), 
                                     labelWidth, 
                                     labelHeight);
 
                _urlFormatBox.TabIndex = 4;
                _urlFormatBox.SetBounds(0,
                                        (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 2),
                                        labelWidth, 
                                        controlHeight);
 
                _urlFormatExampleLabel.Enabled = false; 
                _urlFormatExampleLabel.Text = SR.GetString(SR.DCFAdd_ExampleFormatString);
                _urlFormatExampleLabel.TextAlign = ContentAlignment.BottomLeft; 
                _urlFormatExampleLabel.SetBounds(0,
                                                 (labelHeight * 3) + (labelPadding * 3) + (vertPadding * 2) + (controlHeight * 3),
                                                 labelWidth,
                                                 labelHeight); 

                _readOnlyCheckBox.TabIndex = 5; 
                _readOnlyCheckBox.Text = SR.GetString(SR.DCFAdd_ReadOnly); 
                _readOnlyCheckBox.SetBounds(0,
                                            (labelHeight * 4) + (labelPadding * 4) + (vertPadding * 2) + (controlHeight * 3), 
                                            textBoxWidth,
                                            controlHeight);

 
                if (_haveSchema) {
                    _imageUrlFieldList.Items.AddRange(GetFieldSchemaNames()); 
                    _imageUrlFieldList.SelectedIndex = 0; 
                    _imageUrlFieldList.Visible = true;
                    _imageUrlFieldBox.Visible = false; 
                }
                else {
                    _imageUrlFieldList.Visible = false;
                    _imageUrlFieldBox.Visible = true; 
                }
 
                Controls.AddRange(new System.Windows.Forms.Control[] { 
                    _imageUrlFieldLabel,
                    _imageUrlFieldBox, 
                    _imageUrlFieldList,
                    _readOnlyCheckBox,
                    _urlFormatBoxLabel,
                    _urlFormatBox, 
                    _urlFormatExampleLabel});
            } 
 
            protected override void PreserveFields(IDictionary table) {
                if (_haveSchema) { 
                    table["ImageUrlField"] = _imageUrlFieldList.Text;
                }
                else {
                    table["ImageUrlField"] = _imageUrlFieldBox.Text; 
                }
                table["ReadOnly"] = _readOnlyCheckBox.Checked; 
                table["FormatString"] = _urlFormatBox.Text; 
            }
 
            protected override void RefreshSchemaFields() {
                if (_haveSchema) {
                    _imageUrlFieldList.Items.Clear();
                    _imageUrlFieldList.Items.AddRange(GetFieldSchemaNames()); 
                    _imageUrlFieldList.SelectedIndex = 0;
                    _imageUrlFieldList.Visible = true; 
                    _imageUrlFieldBox.Visible = false; 
                }
                else { 
                    _imageUrlFieldList.Visible = false;
                    _imageUrlFieldBox.Visible = true;
                }
            } 

            protected override void RestoreFieldsInternal(IDictionary table) { 
                string dataField = table["ImageUrlField"].ToString(); 
                if (_haveSchema) {
                    if (dataField.Length > 0) { 
                        bool foundItem = false;
                        foreach (object listItem in _imageUrlFieldList.Items) {
                            if (String.Compare(dataField, listItem.ToString(), StringComparison.OrdinalIgnoreCase) == 0) {
                                _imageUrlFieldList.SelectedItem = listItem; 
                                foundItem = true;
                                break; 
                            } 
                        }
                        if (!foundItem) { 
                            _imageUrlFieldList.Items.Insert(0, dataField);
                            _imageUrlFieldList.SelectedIndex = 0;
                        }
                    } 
                }
                else { 
                    _imageUrlFieldBox.Text = dataField; 
                }
                _readOnlyCheckBox.Checked = (bool)table["ReadOnly"]; 
                _urlFormatBox.Text = (string)table["FormatString"];
            }

            /// <devdoc> 
            /// Called when OK is pressed to reflect the control's values back into the newly created field.
            /// </devdoc> 
            protected override DataControlField SaveValues(string headerText) { 
                ImageField field = new ImageField();
                field.HeaderText = headerText; 
                if (_haveSchema) {
                    field.DataImageUrlField = _imageUrlFieldList.Text;
                }
                else { 
                    field.DataImageUrlField = _imageUrlFieldBox.Text;
                } 
                field.ReadOnly = _readOnlyCheckBox.Checked; 
                field.DataImageUrlFormatString = _urlFormatBox.Text;
                return field; 
            }
        }

    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
