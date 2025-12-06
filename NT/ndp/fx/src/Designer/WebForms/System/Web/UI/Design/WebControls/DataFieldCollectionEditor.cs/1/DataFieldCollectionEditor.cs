//------------------------------------------------------------------------------ 
// <copyright file="DataFieldCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using Microsoft.Win32; 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 

    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label;
    using ListBox = System.Windows.Forms.ListBox; 
    using CollectionForm = System.ComponentModel.Design;
    using Panel = System.Windows.Forms.Panel; 
 

    /// <devdoc> 
    ///      The DataFieldCollectionEditor is a collection editor for DataField
    ///      properties on DataBoundControls.  It uses database schema to populate
    ///      a field picker.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal class DataFieldCollectionEditor : StringCollectionEditor { 
        private const int SC_CONTEXTHELP = 0xF180; 
        private const int WM_SYSCOMMAND = 0x0112;
 
        public DataFieldCollectionEditor(Type type) : base(type) {
        }

        private bool HasSchema { 
            get {
                ITypeDescriptorContext context = Context; 
                bool hasSchema = false; 

                if (context != null && context.Instance != null) { 
                    System.Web.UI.Control control = context.Instance as System.Web.UI.Control;
                    if (control != null) {
                        ISite componentSite = control.Site;
                        if (componentSite != null) { 
                            IDesignerHost designerHost = (IDesignerHost)componentSite.GetService(typeof(IDesignerHost));
                            if (designerHost != null) { 
                                IDesigner designer = designerHost.GetDesigner(control); 
                                DataBoundControlDesigner controlDesigner = designer as DataBoundControlDesigner;
                                if (controlDesigner != null) { 
                                    DesignerDataSourceView view = controlDesigner.DesignerView;
                                    if (view != null) {
                                        IDataSourceViewSchema schema = null;
                                        try { 
                                            schema = view.Schema;
                                        } 
                                        catch (Exception ex) { 
                                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)componentSite.GetService(typeof(IComponentDesignerDebugService));
                                            if (debugService != null) { 
                                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message));
                                            }
                                        }
 
                                        if (schema != null) {
                                            hasSchema = true; 
                                        } 
                                    }
                                } 
                            }
                        }
                    }
                    else { 
                        IDataSourceViewSchemaAccessor schemaAccessor = context.Instance as IDataSourceViewSchemaAccessor;
                        if (schemaAccessor != null) { 
                            if (schemaAccessor.DataSourceViewSchema != null) { 
                                hasSchema = true;
                            } 
                        }
                    }
                }
                return hasSchema; 
            }
        } 
 

        /// <devdoc> 
        ///      Creates a new form to show the current collection.  You may inherit
        ///      from CollectionForm to provide your own form.
        /// </devdoc>
        protected override CollectionForm CreateCollectionForm() { 
            if (HasSchema) {
                ITypeDescriptorContext context = Context; 
 
                if (context != null && context.Instance != null) {
                    System.Web.UI.Control control = context.Instance as System.Web.UI.Control; 
                    if (control != null) {
                        ISite componentSite = control.Site;
                        return new DataFieldCollectionForm(componentSite, this);
                    } 
                }
            } 
            return base.CreateCollectionForm(); 
        }
 
        /// <devdoc>
        ///     DataFieldCollectionForm allows visible editing of a string array. Each line in
        ///     the edit box is an array entry.
        /// </devdoc> 
        private class DataFieldCollectionForm : CollectionForm {
            private Label fieldLabel = new Label(); 
            private ListBoxWithEnter fieldsList = new ListBoxWithEnter(); 
            private Label selectedFieldsLabel = new Label();
            private ListBoxWithEnter selectedFieldsList = new ListBoxWithEnter(); 
            private Button moveLeft = new Button();
            private Button moveRight = new Button();
            private Button moveUp = new Button();
            private Button moveDown = new Button(); 
            private Button okButton = new Button();
            private Button cancelButton = new Button(); 
            private TableLayoutPanel layoutPanel = new TableLayoutPanel(); 
            private Panel moveUpDownPanel = new Panel();
            private Panel moveLeftRightPanel = new Panel(); 

            private DataFieldCollectionEditor editor = null;
            private ArrayList fields = null;
            private string[] _dataFields; 
            private IServiceProvider _serviceProvider;
 
            /// <devdoc> 
            ///     Constructs a StringCollectionForm.
            /// </devdoc> 
            public DataFieldCollectionForm(IServiceProvider serviceProvider, CollectionEditor editor) : base(editor) {
                this.editor = (DataFieldCollectionEditor) editor;
                _serviceProvider = serviceProvider;
 
                // Set RightToLeft mode based on resource file
                string rtlText = SR.GetString(SR.RTL); 
                if (!String.Equals(rtlText, "RTL_False", StringComparison.Ordinal)) { 
                    RightToLeft = RightToLeft.Yes;
                    RightToLeftLayout = true; 
                }

                InitializeComponent();
                _dataFields = GetControlDataFieldNames(); 
            }
 
            /// <devdoc> 
            /// moves a field from the available list to the list of selected fields
            /// </devdoc> 
            private void AddFieldToSelectedList() {
                int selectedFieldIndex = fieldsList.SelectedIndex;
                object selectedField = fieldsList.SelectedItem;
 
                if (selectedFieldIndex >= 0) {
                    fieldsList.Items.RemoveAt(selectedFieldIndex); 
                    selectedFieldsList.SelectedIndex = selectedFieldsList.Items.Add(selectedField); 
                    if (fieldsList.Items.Count > 0) {
                        fieldsList.SelectedIndex = (fieldsList.Items.Count > selectedFieldIndex) ? selectedFieldIndex : fieldsList.Items.Count - 1; 
                    }
                }
            }
 
            /// <devdoc>
            /// Returns an array of string indicating the fields of the schema, paying attention 
            /// to DataMember if there is one. 
            /// </devdoc>
            private string[] GetControlDataFieldNames() { 
                if (_dataFields == null) {
                    ITypeDescriptorContext context = editor.Context;
                    IDataSourceFieldSchema[] fieldSchemas = null;
                    IDataSourceViewSchema schema = null; 

                    if (context != null && context.Instance != null) { 
                        System.Web.UI.Control control = context.Instance as System.Web.UI.Control; 
                        if (control != null) {
                            ISite componentSite = control.Site; 
                            if (componentSite != null) {
                                IDesignerHost designerHost = (IDesignerHost)componentSite.GetService(typeof(IDesignerHost));
                                if (designerHost != null) {
                                    IDesigner designer = designerHost.GetDesigner(control); 
                                    DataBoundControlDesigner controlDesigner = designer as DataBoundControlDesigner;
                                    if (controlDesigner != null) { 
                                        DesignerDataSourceView view = controlDesigner.DesignerView; 
                                        if (view != null) {
                                            try { 
                                                schema = view.Schema;
                                            }
                                            catch (Exception ex) {
                                                IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)componentSite.GetService(typeof(IComponentDesignerDebugService)); 
                                                if (debugService != null) {
                                                    debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message)); 
                                                } 
                                            }
                                        } 
                                    }
                                }
                            }
                        } 
                        else {
                            IDataSourceViewSchemaAccessor schemaAccessor = context.Instance as IDataSourceViewSchemaAccessor; 
                            if (schemaAccessor != null) { 
                                schema = schemaAccessor.DataSourceViewSchema as IDataSourceViewSchema;
                            } 
                        }
                    }

                    if (schema != null) { 
                        fieldSchemas = schema.GetFields();
                        if (fieldSchemas != null) { 
                            int fieldSchemasLength = fieldSchemas.Length; 
                            _dataFields = new string[fieldSchemasLength];
                            for (int i = 0; i < fieldSchemasLength; i++) { 
                                _dataFields[i] = fieldSchemas[i].Name;
                            }
                        }
                    } 
                }
 
                return _dataFields; 
            }
 
            /// <devdoc>
            ///     NOTE: The following code is required by the form
            ///     designer.  It can be modified using the form editor.  Do not
            ///     modify it using the code editor. 
            /// </devdoc>
            private void InitializeComponent() { 
                const int topPadding = 12; 
                const int leftPadding = 12;
                const int bottomPadding = 12; 
                const int rightPadding = 12;
                const int horizPadding = 6;
                const int vertPadding = 10;
                const int labelHeight = 15; 
                const int listBoxHeight = 130;
                const int listBoxWidth = 135; 
                const int buttonHeight = 23; 
                const int buttonWidth = 75;
                const int smallButtonHeight = 23; 
                const int smallButtonWidth = 26;
                const int buttonVertPadding = 1;
                const int vertControlPadding = 2;
                int formHeight = topPadding + bottomPadding + (labelHeight * 2) + vertPadding + listBoxHeight + buttonHeight; 
                int formWidth = leftPadding + rightPadding + (listBoxWidth * 2) + (horizPadding * 3) + (smallButtonWidth * 2);
 
                this.SuspendLayout(); 

                fieldLabel.AutoSize = true; 
                fieldLabel.TabStop = false;
                fieldLabel.TabIndex = 0;
                fieldLabel.Text = SR.GetString(SR.DataFieldCollectionAvailableFields);
                fieldLabel.MinimumSize = new Size(listBoxWidth, labelHeight); 
                fieldLabel.MaximumSize = new Size(listBoxWidth, labelHeight * 2);
                fieldLabel.SetBounds(0, 
                                     0, 
                                     listBoxWidth,
                                     labelHeight); 

                selectedFieldsLabel.AutoSize = true;
                selectedFieldsLabel.TabStop = false;
                selectedFieldsLabel.Text = SR.GetString(SR.DataFieldCollectionSelectedFields); 
                selectedFieldsLabel.MinimumSize = new Size(listBoxWidth, labelHeight);
                selectedFieldsLabel.MaximumSize = new Size(listBoxWidth, labelHeight * 2); 
                selectedFieldsLabel.SetBounds(listBoxWidth + smallButtonWidth + (horizPadding * 2), 
                                              0,
                                              listBoxWidth, 
                                              labelHeight);

                fieldsList.TabIndex = 1;
                fieldsList.AllowDrop = false; 
                fieldsList.SelectedIndexChanged += new EventHandler(this.OnFieldsSelectedIndexChanged);
                fieldsList.MouseDoubleClick += new MouseEventHandler(this.OnDoubleClickField); 
                fieldsList.KeyPress += new KeyPressEventHandler(this.OnKeyPressField); 
                fieldsList.SetBounds(0,
                                     0, 
                                     listBoxWidth,
                                     listBoxHeight);

                selectedFieldsList.TabIndex = 3; 
                selectedFieldsList.AllowDrop = false;
                selectedFieldsList.SelectedIndexChanged += new EventHandler(this.OnSelectedFieldsSelectedIndexChanged); 
                selectedFieldsList.MouseDoubleClick += new MouseEventHandler(this.OnDoubleClickSelectedField); 
                selectedFieldsList.KeyPress += new KeyPressEventHandler(this.OnKeyPressSelectedField);
                selectedFieldsList.SetBounds(0, 
                                     0,
                                     listBoxWidth,
                                     listBoxHeight);
 
                moveRight.TabIndex = 100;
                moveRight.Text = ">"; 
                moveRight.AccessibleName = SR.GetString(SR.DataFieldCollection_MoveRight); 
                moveRight.AccessibleDescription = SR.GetString(SR.DataFieldCollection_MoveRightDesc);
                moveRight.Click += new EventHandler(this.OnMoveRight); 
                moveRight.Location = new Point(0, ((listBoxHeight / 2) - (smallButtonHeight + (buttonVertPadding / 2))));
                moveRight.Size = new Size(smallButtonWidth, smallButtonHeight);

                moveLeft.TabIndex = 101; 
                moveLeft.Text = "<";
                moveLeft.AccessibleName = SR.GetString(SR.DataFieldCollection_MoveLeft); 
                moveLeft.AccessibleDescription = SR.GetString(SR.DataFieldCollection_MoveLeftDesc); 
                moveLeft.Click += new EventHandler(this.OnMoveLeft);
                moveLeft.Location = new Point(0, ((listBoxHeight / 2) + (buttonVertPadding / 2))); 
                moveLeft.Size = new Size(smallButtonWidth, smallButtonHeight);

                moveLeftRightPanel.TabIndex = 2;
                moveLeftRightPanel.Location = new Point(horizPadding, 0); 
                moveLeftRightPanel.Size = new Size(smallButtonWidth + (2 * buttonVertPadding), listBoxHeight);
                moveLeftRightPanel.Controls.Add(moveLeft); 
                moveLeftRightPanel.Controls.Add(moveRight); 

                moveUp.TabIndex = 200; 
                Bitmap moveUpBitmap = new Icon(this.GetType(), "SortUp.ico").ToBitmap();
                moveUpBitmap.MakeTransparent();
                moveUp.Image = moveUpBitmap;
                moveUp.AccessibleName = SR.GetString(SR.DataFieldCollection_MoveUp); 
                moveUp.AccessibleDescription = SR.GetString(SR.DataFieldCollection_MoveUpDesc);
                moveUp.Click += new EventHandler(this.OnMoveUp); 
                moveUp.Location = new Point(0, 0); 
                moveUp.Size = new Size(smallButtonWidth, smallButtonHeight);
 
                moveDown.TabIndex = 201;
                Bitmap moveDownBitmap = new Icon(this.GetType(), "SortDown.ico").ToBitmap();
                moveDownBitmap.MakeTransparent();
                moveDown.Image = moveDownBitmap; 
                moveDown.AccessibleName = SR.GetString(SR.DataFieldCollection_MoveDown);
                moveDown.AccessibleDescription = SR.GetString(SR.DataFieldCollection_MoveDownDesc); 
                moveDown.Click += new EventHandler(this.OnMoveDown); 
                moveDown.Location = new Point(0, buttonVertPadding + smallButtonHeight);
                moveDown.Size = new Size(smallButtonWidth, smallButtonHeight); 

                moveUpDownPanel.TabIndex = 4;
                moveUpDownPanel.Location = new Point(horizPadding, 0);
                moveUpDownPanel.Size = new Size(smallButtonWidth, (smallButtonHeight * 2) + buttonVertPadding); 
                moveUpDownPanel.Controls.Add(moveUp);
                moveUpDownPanel.Controls.Add(moveDown); 
 
                okButton.TabIndex = 5;
                okButton.Text = SR.GetString(SR.OKCaption); 
                okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                okButton.DialogResult = DialogResult.OK;
                okButton.Click += new EventHandler(this.OKButton_click);
                okButton.SetBounds(formWidth - rightPadding - (buttonWidth * 2) - horizPadding, 
                                   formHeight - bottomPadding - buttonHeight,
                                   buttonWidth, 
                                   buttonHeight); 

                cancelButton.TabIndex = 6; 
                cancelButton.Text = SR.GetString(SR.CancelCaption);
                cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                cancelButton.DialogResult = DialogResult.Cancel;
                cancelButton.SetBounds(formWidth - rightPadding - buttonWidth, 
                                   formHeight - bottomPadding - buttonHeight,
                                   buttonWidth, 
                                   buttonHeight); 

                layoutPanel.AutoSize = true; 
                layoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                layoutPanel.ColumnCount = 4;
                layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, listBoxWidth));
                layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, smallButtonWidth + (horizPadding * 2))); 
                layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, listBoxWidth));
                layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, smallButtonWidth + horizPadding)); 
                layoutPanel.Location = new Point(leftPadding, topPadding); 
                layoutPanel.Size = new Size((listBoxWidth * 2) + (smallButtonWidth * 2) + (horizPadding * 3),
                                            labelHeight + vertControlPadding + listBoxHeight); 
                layoutPanel.RowCount = 2;
                layoutPanel.RowStyles.Add(new RowStyle());
                layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, listBoxHeight));
                layoutPanel.Controls.Add(fieldLabel, 0, 0); 
                layoutPanel.Controls.Add(selectedFieldsLabel, 2, 0);
                layoutPanel.Controls.Add(fieldsList, 0, 1); 
                layoutPanel.Controls.Add(selectedFieldsList, 2, 1); 
                layoutPanel.Controls.Add(moveLeftRightPanel, 1, 1);
                layoutPanel.Controls.Add(moveUpDownPanel, 3, 1); 

                Font dialogFont = UIServiceHelper.GetDialogFont(_serviceProvider);
                if (dialogFont != null) {
                    Font = dialogFont; 
                }
 
                this.Text = SR.GetString(SR.DataFieldCollectionEditorTitle); 
                this.AcceptButton = okButton;
#pragma warning disable 618 
                this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
#pragma warning restore 618
                this.CancelButton = cancelButton;
                this.ClientSize = new Size(formWidth, formHeight); 
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.HelpButton = true; 
                this.MaximizeBox = false; 
                this.MinimizeBox = false;
 
                // Set RightToLeft mode based on resource file
                string rtlText = SR.GetString(SR.RTL);
                if (!String.Equals(rtlText, "RTL_False", StringComparison.Ordinal)) {
                    this.RightToLeft = RightToLeft.Yes; 
                    this.RightToLeftLayout = true;
                } 
 
                this.ShowIcon = false;
                this.ShowInTaskbar = false; 
                this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;


                this.Controls.Clear(); 
                this.Controls.AddRange(new Control[] {
                                        layoutPanel, 
                                        okButton, 
                                        cancelButton,
                                        }); 
                this.ResumeLayout(false);
                this.PerformLayout();
            }
 
            /// <devdoc>
            ///      Commits the changes to the editor. 
            /// </devdoc> 
            private void OKButton_click(object sender, EventArgs e) {
                int selectedFieldsCount = selectedFieldsList.Items.Count; 
                object[] dataFieldArray = new object[selectedFieldsCount];

                selectedFieldsList.Items.CopyTo(dataFieldArray, 0);
                Items = dataFieldArray; 
            }
 
            /// <devdoc> 
            /// double-clicking moves a field to the selected list
            /// </devdoc> 
            private void OnDoubleClickField(object sender, MouseEventArgs e) {
                if (fieldsList.IndexFromPoint(e.Location) != -1 && e.Button == MouseButtons.Left) {
                    AddFieldToSelectedList();
                } 
            }
 
            /// <devdoc> 
            /// double-clicking moves a selected field to the available list
            /// </devdoc> 
            private void OnDoubleClickSelectedField(object sender, MouseEventArgs e) {
                if (selectedFieldsList.IndexFromPoint(e.Location) != -1 && e.Button == MouseButtons.Left) {
                    RemoveFieldFromSelectedList();
                } 
            }
 
            // <summary> 
            //      This is called when the value property in the CollectionForm has changed.
            //      In it you should update your user interface to reflect the current value. 
            // </summary>
            // </doc>
            protected override void OnEditValueChanged() {
                fields = null; 
                fieldsList.Items.Clear();
                selectedFieldsList.Items.Clear(); 
 
                fields = new ArrayList();
                foreach (string field in GetControlDataFieldNames()) { 
                    fields.Add(field);
                    if (Array.IndexOf(Items, field) < 0) {
                        fieldsList.Items.Add(field);
                    } 
                }
 
                foreach (string field in Items) { 
                    selectedFieldsList.Items.Add(field);
                } 

                if (fieldsList.Items.Count > 0) {
                    fieldsList.SelectedIndex = 0;
                } 

                SetButtonsEnabled(); 
            } 

            /// <devdoc> 
            /// event handler for selectedIndexChanged on the fieldsList
            /// </devdoc>
            private void OnFieldsSelectedIndexChanged(object sender, EventArgs e) {
                if (fieldsList.SelectedIndex > -1) { 
                    selectedFieldsList.SelectedIndex = -1;
                } 
                SetButtonsEnabled(); 
            }
 
            /// <devdoc>
            /// event handler for Enter (the accessible version of mouse double-click)
            /// </devdoc>
            private void OnKeyPressField(object sender, KeyPressEventArgs e) { 
                if (e.KeyChar == (char)13) {
                    AddFieldToSelectedList(); 
                    e.Handled = true; 
                }
            } 

            /// <devdoc>
            /// event handler for Enter (the accessible version of mouse double-click)
            /// </devdoc> 
            private void OnKeyPressSelectedField(object sender, KeyPressEventArgs e) {
                if (e.KeyChar == (char)13) { 
                    RemoveFieldFromSelectedList(); 
                    e.Handled = true;
                } 
            }

            /// <devdoc>
            /// event handler for the move down button 
            /// </devdoc>
            private void OnMoveDown(object sender, EventArgs e) { 
                int selectedIndex = selectedFieldsList.SelectedIndex; 
                object selectedItem = selectedFieldsList.SelectedItem;
 
                selectedFieldsList.Items.RemoveAt(selectedIndex);
                selectedFieldsList.Items.Insert(selectedIndex + 1, selectedItem);
                selectedFieldsList.SelectedIndex = selectedIndex + 1;
            } 

            /// <devdoc> 
            /// event handler for the move left button 
            /// </devdoc>
            private void OnMoveLeft(object sender, EventArgs e) { 
                RemoveFieldFromSelectedList();
            }

            /// <devdoc> 
            /// event handler for the move right button
            /// </devdoc> 
            private void OnMoveRight(object sender, EventArgs e) { 
                AddFieldToSelectedList();
            } 

            /// <devdoc>
            /// event handler for the move up button
            /// </devdoc> 
            private void OnMoveUp(object sender, EventArgs e) {
                int selectedIndex = selectedFieldsList.SelectedIndex; 
                object selectedItem = selectedFieldsList.SelectedItem; 

                selectedFieldsList.Items.RemoveAt(selectedIndex); 
                selectedFieldsList.Items.Insert(selectedIndex - 1, selectedItem);
                selectedFieldsList.SelectedIndex = selectedIndex - 1;
            }
 
            /// <devdoc>
            /// event handler for selectedIndexChanged on the selectedFieldsList 
            /// </devdoc> 
            private void OnSelectedFieldsSelectedIndexChanged(object sender, EventArgs e) {
                if (selectedFieldsList.SelectedIndex > -1) { 
                    fieldsList.SelectedIndex = -1;
                }
                SetButtonsEnabled();
            } 

            /// <devdoc> 
            /// moves a field from the selected list to the list of available fields 
            /// </devdoc>
            private void RemoveFieldFromSelectedList() { 
                int selectedFieldIndex = selectedFieldsList.SelectedIndex;
                string selectedItem;
                int fieldListIndex = 0;
                int fieldIndex = 0; 

                if (selectedFieldIndex >= 0) { 
                    selectedItem = selectedFieldsList.SelectedItem.ToString(); 
                    fieldIndex = fields.IndexOf(selectedItem);
 
                    // find the right place in the list for the item
                    for (int i = 0; i < fieldsList.Items.Count; i++) {
                        if (fields.IndexOf(fieldsList.Items[i]) > fieldIndex) {
                            break; 
                        }
                        fieldListIndex++; 
                    } 

                    fieldsList.Items.Insert(fieldListIndex, selectedItem); 
                    selectedFieldsList.Items.RemoveAt(selectedFieldIndex);
                    fieldsList.SelectedIndex = fieldListIndex;

                    if (selectedFieldsList.Items.Count > 0) { 
                        selectedFieldsList.SelectedIndex = (selectedFieldsList.Items.Count > selectedFieldIndex) ? selectedFieldIndex : selectedFieldsList.Items.Count - 1;
                    } 
                } 
            }
 
            /// <devdoc>
            /// set the enabled properties for the left and right, up and down move buttons based on list contents
            /// </devdoc>
            private void SetButtonsEnabled() { 
                int selectedFieldsCount = selectedFieldsList.Items.Count;
                int selectedFieldsIndex = selectedFieldsList.SelectedIndex; 
                bool moveUpEnabled = false; 
                bool moveDownEnabled = false;
                bool moveRightEnabled = false; 
                bool moveLeftEnabled = false;

                if (fieldsList.SelectedIndex > -1) {
                    moveRightEnabled = true; 
                }
 
                if (selectedFieldsIndex > -1) { 
                    moveLeftEnabled = true;
                    if (selectedFieldsCount > 0) { 
                        if (selectedFieldsIndex > 0) {
                            moveUpEnabled = true;
                        }
                        if (selectedFieldsIndex < selectedFieldsCount - 1) { 
                            moveDownEnabled = true;
                        } 
                    } 
                }
 
                moveRight.Enabled = moveRightEnabled;
                moveLeft.Enabled = moveLeftEnabled;
                moveUp.Enabled = moveUpEnabled;
                moveDown.Enabled = moveDownEnabled; 
            }
 
            /// <devdoc> 
            /// Overridden to reroute the context-help button to our own handler.
            /// </devdoc> 
            protected override void WndProc(ref Message m) {
                if ((m.Msg == WM_SYSCOMMAND) && ((int)m.WParam == SC_CONTEXTHELP)) {
                    // Show help
                    if (_serviceProvider != null) { 
                        IHelpService helpService = (IHelpService)_serviceProvider.GetService(typeof(IHelpService));
                        if (helpService != null) { 
                            helpService.ShowHelpFromKeyword("net.Asp.DataFieldCollectionEditor"); 
                        }
                    } 
                }
                else {
                    base.WndProc(ref m);
                } 
            }
 
            private class ListBoxWithEnter : ListBox { 
                protected override bool IsInputKey(Keys keyData) {
                    if (keyData == Keys.Enter) { 
                        return true;
                    }
                    return base.IsInputKey(keyData);
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataFieldCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using Microsoft.Win32; 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 

    using Button = System.Windows.Forms.Button;
    using Label = System.Windows.Forms.Label;
    using ListBox = System.Windows.Forms.ListBox; 
    using CollectionForm = System.ComponentModel.Design;
    using Panel = System.Windows.Forms.Panel; 
 

    /// <devdoc> 
    ///      The DataFieldCollectionEditor is a collection editor for DataField
    ///      properties on DataBoundControls.  It uses database schema to populate
    ///      a field picker.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal class DataFieldCollectionEditor : StringCollectionEditor { 
        private const int SC_CONTEXTHELP = 0xF180; 
        private const int WM_SYSCOMMAND = 0x0112;
 
        public DataFieldCollectionEditor(Type type) : base(type) {
        }

        private bool HasSchema { 
            get {
                ITypeDescriptorContext context = Context; 
                bool hasSchema = false; 

                if (context != null && context.Instance != null) { 
                    System.Web.UI.Control control = context.Instance as System.Web.UI.Control;
                    if (control != null) {
                        ISite componentSite = control.Site;
                        if (componentSite != null) { 
                            IDesignerHost designerHost = (IDesignerHost)componentSite.GetService(typeof(IDesignerHost));
                            if (designerHost != null) { 
                                IDesigner designer = designerHost.GetDesigner(control); 
                                DataBoundControlDesigner controlDesigner = designer as DataBoundControlDesigner;
                                if (controlDesigner != null) { 
                                    DesignerDataSourceView view = controlDesigner.DesignerView;
                                    if (view != null) {
                                        IDataSourceViewSchema schema = null;
                                        try { 
                                            schema = view.Schema;
                                        } 
                                        catch (Exception ex) { 
                                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)componentSite.GetService(typeof(IComponentDesignerDebugService));
                                            if (debugService != null) { 
                                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message));
                                            }
                                        }
 
                                        if (schema != null) {
                                            hasSchema = true; 
                                        } 
                                    }
                                } 
                            }
                        }
                    }
                    else { 
                        IDataSourceViewSchemaAccessor schemaAccessor = context.Instance as IDataSourceViewSchemaAccessor;
                        if (schemaAccessor != null) { 
                            if (schemaAccessor.DataSourceViewSchema != null) { 
                                hasSchema = true;
                            } 
                        }
                    }
                }
                return hasSchema; 
            }
        } 
 

        /// <devdoc> 
        ///      Creates a new form to show the current collection.  You may inherit
        ///      from CollectionForm to provide your own form.
        /// </devdoc>
        protected override CollectionForm CreateCollectionForm() { 
            if (HasSchema) {
                ITypeDescriptorContext context = Context; 
 
                if (context != null && context.Instance != null) {
                    System.Web.UI.Control control = context.Instance as System.Web.UI.Control; 
                    if (control != null) {
                        ISite componentSite = control.Site;
                        return new DataFieldCollectionForm(componentSite, this);
                    } 
                }
            } 
            return base.CreateCollectionForm(); 
        }
 
        /// <devdoc>
        ///     DataFieldCollectionForm allows visible editing of a string array. Each line in
        ///     the edit box is an array entry.
        /// </devdoc> 
        private class DataFieldCollectionForm : CollectionForm {
            private Label fieldLabel = new Label(); 
            private ListBoxWithEnter fieldsList = new ListBoxWithEnter(); 
            private Label selectedFieldsLabel = new Label();
            private ListBoxWithEnter selectedFieldsList = new ListBoxWithEnter(); 
            private Button moveLeft = new Button();
            private Button moveRight = new Button();
            private Button moveUp = new Button();
            private Button moveDown = new Button(); 
            private Button okButton = new Button();
            private Button cancelButton = new Button(); 
            private TableLayoutPanel layoutPanel = new TableLayoutPanel(); 
            private Panel moveUpDownPanel = new Panel();
            private Panel moveLeftRightPanel = new Panel(); 

            private DataFieldCollectionEditor editor = null;
            private ArrayList fields = null;
            private string[] _dataFields; 
            private IServiceProvider _serviceProvider;
 
            /// <devdoc> 
            ///     Constructs a StringCollectionForm.
            /// </devdoc> 
            public DataFieldCollectionForm(IServiceProvider serviceProvider, CollectionEditor editor) : base(editor) {
                this.editor = (DataFieldCollectionEditor) editor;
                _serviceProvider = serviceProvider;
 
                // Set RightToLeft mode based on resource file
                string rtlText = SR.GetString(SR.RTL); 
                if (!String.Equals(rtlText, "RTL_False", StringComparison.Ordinal)) { 
                    RightToLeft = RightToLeft.Yes;
                    RightToLeftLayout = true; 
                }

                InitializeComponent();
                _dataFields = GetControlDataFieldNames(); 
            }
 
            /// <devdoc> 
            /// moves a field from the available list to the list of selected fields
            /// </devdoc> 
            private void AddFieldToSelectedList() {
                int selectedFieldIndex = fieldsList.SelectedIndex;
                object selectedField = fieldsList.SelectedItem;
 
                if (selectedFieldIndex >= 0) {
                    fieldsList.Items.RemoveAt(selectedFieldIndex); 
                    selectedFieldsList.SelectedIndex = selectedFieldsList.Items.Add(selectedField); 
                    if (fieldsList.Items.Count > 0) {
                        fieldsList.SelectedIndex = (fieldsList.Items.Count > selectedFieldIndex) ? selectedFieldIndex : fieldsList.Items.Count - 1; 
                    }
                }
            }
 
            /// <devdoc>
            /// Returns an array of string indicating the fields of the schema, paying attention 
            /// to DataMember if there is one. 
            /// </devdoc>
            private string[] GetControlDataFieldNames() { 
                if (_dataFields == null) {
                    ITypeDescriptorContext context = editor.Context;
                    IDataSourceFieldSchema[] fieldSchemas = null;
                    IDataSourceViewSchema schema = null; 

                    if (context != null && context.Instance != null) { 
                        System.Web.UI.Control control = context.Instance as System.Web.UI.Control; 
                        if (control != null) {
                            ISite componentSite = control.Site; 
                            if (componentSite != null) {
                                IDesignerHost designerHost = (IDesignerHost)componentSite.GetService(typeof(IDesignerHost));
                                if (designerHost != null) {
                                    IDesigner designer = designerHost.GetDesigner(control); 
                                    DataBoundControlDesigner controlDesigner = designer as DataBoundControlDesigner;
                                    if (controlDesigner != null) { 
                                        DesignerDataSourceView view = controlDesigner.DesignerView; 
                                        if (view != null) {
                                            try { 
                                                schema = view.Schema;
                                            }
                                            catch (Exception ex) {
                                                IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)componentSite.GetService(typeof(IComponentDesignerDebugService)); 
                                                if (debugService != null) {
                                                    debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message)); 
                                                } 
                                            }
                                        } 
                                    }
                                }
                            }
                        } 
                        else {
                            IDataSourceViewSchemaAccessor schemaAccessor = context.Instance as IDataSourceViewSchemaAccessor; 
                            if (schemaAccessor != null) { 
                                schema = schemaAccessor.DataSourceViewSchema as IDataSourceViewSchema;
                            } 
                        }
                    }

                    if (schema != null) { 
                        fieldSchemas = schema.GetFields();
                        if (fieldSchemas != null) { 
                            int fieldSchemasLength = fieldSchemas.Length; 
                            _dataFields = new string[fieldSchemasLength];
                            for (int i = 0; i < fieldSchemasLength; i++) { 
                                _dataFields[i] = fieldSchemas[i].Name;
                            }
                        }
                    } 
                }
 
                return _dataFields; 
            }
 
            /// <devdoc>
            ///     NOTE: The following code is required by the form
            ///     designer.  It can be modified using the form editor.  Do not
            ///     modify it using the code editor. 
            /// </devdoc>
            private void InitializeComponent() { 
                const int topPadding = 12; 
                const int leftPadding = 12;
                const int bottomPadding = 12; 
                const int rightPadding = 12;
                const int horizPadding = 6;
                const int vertPadding = 10;
                const int labelHeight = 15; 
                const int listBoxHeight = 130;
                const int listBoxWidth = 135; 
                const int buttonHeight = 23; 
                const int buttonWidth = 75;
                const int smallButtonHeight = 23; 
                const int smallButtonWidth = 26;
                const int buttonVertPadding = 1;
                const int vertControlPadding = 2;
                int formHeight = topPadding + bottomPadding + (labelHeight * 2) + vertPadding + listBoxHeight + buttonHeight; 
                int formWidth = leftPadding + rightPadding + (listBoxWidth * 2) + (horizPadding * 3) + (smallButtonWidth * 2);
 
                this.SuspendLayout(); 

                fieldLabel.AutoSize = true; 
                fieldLabel.TabStop = false;
                fieldLabel.TabIndex = 0;
                fieldLabel.Text = SR.GetString(SR.DataFieldCollectionAvailableFields);
                fieldLabel.MinimumSize = new Size(listBoxWidth, labelHeight); 
                fieldLabel.MaximumSize = new Size(listBoxWidth, labelHeight * 2);
                fieldLabel.SetBounds(0, 
                                     0, 
                                     listBoxWidth,
                                     labelHeight); 

                selectedFieldsLabel.AutoSize = true;
                selectedFieldsLabel.TabStop = false;
                selectedFieldsLabel.Text = SR.GetString(SR.DataFieldCollectionSelectedFields); 
                selectedFieldsLabel.MinimumSize = new Size(listBoxWidth, labelHeight);
                selectedFieldsLabel.MaximumSize = new Size(listBoxWidth, labelHeight * 2); 
                selectedFieldsLabel.SetBounds(listBoxWidth + smallButtonWidth + (horizPadding * 2), 
                                              0,
                                              listBoxWidth, 
                                              labelHeight);

                fieldsList.TabIndex = 1;
                fieldsList.AllowDrop = false; 
                fieldsList.SelectedIndexChanged += new EventHandler(this.OnFieldsSelectedIndexChanged);
                fieldsList.MouseDoubleClick += new MouseEventHandler(this.OnDoubleClickField); 
                fieldsList.KeyPress += new KeyPressEventHandler(this.OnKeyPressField); 
                fieldsList.SetBounds(0,
                                     0, 
                                     listBoxWidth,
                                     listBoxHeight);

                selectedFieldsList.TabIndex = 3; 
                selectedFieldsList.AllowDrop = false;
                selectedFieldsList.SelectedIndexChanged += new EventHandler(this.OnSelectedFieldsSelectedIndexChanged); 
                selectedFieldsList.MouseDoubleClick += new MouseEventHandler(this.OnDoubleClickSelectedField); 
                selectedFieldsList.KeyPress += new KeyPressEventHandler(this.OnKeyPressSelectedField);
                selectedFieldsList.SetBounds(0, 
                                     0,
                                     listBoxWidth,
                                     listBoxHeight);
 
                moveRight.TabIndex = 100;
                moveRight.Text = ">"; 
                moveRight.AccessibleName = SR.GetString(SR.DataFieldCollection_MoveRight); 
                moveRight.AccessibleDescription = SR.GetString(SR.DataFieldCollection_MoveRightDesc);
                moveRight.Click += new EventHandler(this.OnMoveRight); 
                moveRight.Location = new Point(0, ((listBoxHeight / 2) - (smallButtonHeight + (buttonVertPadding / 2))));
                moveRight.Size = new Size(smallButtonWidth, smallButtonHeight);

                moveLeft.TabIndex = 101; 
                moveLeft.Text = "<";
                moveLeft.AccessibleName = SR.GetString(SR.DataFieldCollection_MoveLeft); 
                moveLeft.AccessibleDescription = SR.GetString(SR.DataFieldCollection_MoveLeftDesc); 
                moveLeft.Click += new EventHandler(this.OnMoveLeft);
                moveLeft.Location = new Point(0, ((listBoxHeight / 2) + (buttonVertPadding / 2))); 
                moveLeft.Size = new Size(smallButtonWidth, smallButtonHeight);

                moveLeftRightPanel.TabIndex = 2;
                moveLeftRightPanel.Location = new Point(horizPadding, 0); 
                moveLeftRightPanel.Size = new Size(smallButtonWidth + (2 * buttonVertPadding), listBoxHeight);
                moveLeftRightPanel.Controls.Add(moveLeft); 
                moveLeftRightPanel.Controls.Add(moveRight); 

                moveUp.TabIndex = 200; 
                Bitmap moveUpBitmap = new Icon(this.GetType(), "SortUp.ico").ToBitmap();
                moveUpBitmap.MakeTransparent();
                moveUp.Image = moveUpBitmap;
                moveUp.AccessibleName = SR.GetString(SR.DataFieldCollection_MoveUp); 
                moveUp.AccessibleDescription = SR.GetString(SR.DataFieldCollection_MoveUpDesc);
                moveUp.Click += new EventHandler(this.OnMoveUp); 
                moveUp.Location = new Point(0, 0); 
                moveUp.Size = new Size(smallButtonWidth, smallButtonHeight);
 
                moveDown.TabIndex = 201;
                Bitmap moveDownBitmap = new Icon(this.GetType(), "SortDown.ico").ToBitmap();
                moveDownBitmap.MakeTransparent();
                moveDown.Image = moveDownBitmap; 
                moveDown.AccessibleName = SR.GetString(SR.DataFieldCollection_MoveDown);
                moveDown.AccessibleDescription = SR.GetString(SR.DataFieldCollection_MoveDownDesc); 
                moveDown.Click += new EventHandler(this.OnMoveDown); 
                moveDown.Location = new Point(0, buttonVertPadding + smallButtonHeight);
                moveDown.Size = new Size(smallButtonWidth, smallButtonHeight); 

                moveUpDownPanel.TabIndex = 4;
                moveUpDownPanel.Location = new Point(horizPadding, 0);
                moveUpDownPanel.Size = new Size(smallButtonWidth, (smallButtonHeight * 2) + buttonVertPadding); 
                moveUpDownPanel.Controls.Add(moveUp);
                moveUpDownPanel.Controls.Add(moveDown); 
 
                okButton.TabIndex = 5;
                okButton.Text = SR.GetString(SR.OKCaption); 
                okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                okButton.DialogResult = DialogResult.OK;
                okButton.Click += new EventHandler(this.OKButton_click);
                okButton.SetBounds(formWidth - rightPadding - (buttonWidth * 2) - horizPadding, 
                                   formHeight - bottomPadding - buttonHeight,
                                   buttonWidth, 
                                   buttonHeight); 

                cancelButton.TabIndex = 6; 
                cancelButton.Text = SR.GetString(SR.CancelCaption);
                cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                cancelButton.DialogResult = DialogResult.Cancel;
                cancelButton.SetBounds(formWidth - rightPadding - buttonWidth, 
                                   formHeight - bottomPadding - buttonHeight,
                                   buttonWidth, 
                                   buttonHeight); 

                layoutPanel.AutoSize = true; 
                layoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                layoutPanel.ColumnCount = 4;
                layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, listBoxWidth));
                layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, smallButtonWidth + (horizPadding * 2))); 
                layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, listBoxWidth));
                layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, smallButtonWidth + horizPadding)); 
                layoutPanel.Location = new Point(leftPadding, topPadding); 
                layoutPanel.Size = new Size((listBoxWidth * 2) + (smallButtonWidth * 2) + (horizPadding * 3),
                                            labelHeight + vertControlPadding + listBoxHeight); 
                layoutPanel.RowCount = 2;
                layoutPanel.RowStyles.Add(new RowStyle());
                layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, listBoxHeight));
                layoutPanel.Controls.Add(fieldLabel, 0, 0); 
                layoutPanel.Controls.Add(selectedFieldsLabel, 2, 0);
                layoutPanel.Controls.Add(fieldsList, 0, 1); 
                layoutPanel.Controls.Add(selectedFieldsList, 2, 1); 
                layoutPanel.Controls.Add(moveLeftRightPanel, 1, 1);
                layoutPanel.Controls.Add(moveUpDownPanel, 3, 1); 

                Font dialogFont = UIServiceHelper.GetDialogFont(_serviceProvider);
                if (dialogFont != null) {
                    Font = dialogFont; 
                }
 
                this.Text = SR.GetString(SR.DataFieldCollectionEditorTitle); 
                this.AcceptButton = okButton;
#pragma warning disable 618 
                this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
#pragma warning restore 618
                this.CancelButton = cancelButton;
                this.ClientSize = new Size(formWidth, formHeight); 
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.HelpButton = true; 
                this.MaximizeBox = false; 
                this.MinimizeBox = false;
 
                // Set RightToLeft mode based on resource file
                string rtlText = SR.GetString(SR.RTL);
                if (!String.Equals(rtlText, "RTL_False", StringComparison.Ordinal)) {
                    this.RightToLeft = RightToLeft.Yes; 
                    this.RightToLeftLayout = true;
                } 
 
                this.ShowIcon = false;
                this.ShowInTaskbar = false; 
                this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;


                this.Controls.Clear(); 
                this.Controls.AddRange(new Control[] {
                                        layoutPanel, 
                                        okButton, 
                                        cancelButton,
                                        }); 
                this.ResumeLayout(false);
                this.PerformLayout();
            }
 
            /// <devdoc>
            ///      Commits the changes to the editor. 
            /// </devdoc> 
            private void OKButton_click(object sender, EventArgs e) {
                int selectedFieldsCount = selectedFieldsList.Items.Count; 
                object[] dataFieldArray = new object[selectedFieldsCount];

                selectedFieldsList.Items.CopyTo(dataFieldArray, 0);
                Items = dataFieldArray; 
            }
 
            /// <devdoc> 
            /// double-clicking moves a field to the selected list
            /// </devdoc> 
            private void OnDoubleClickField(object sender, MouseEventArgs e) {
                if (fieldsList.IndexFromPoint(e.Location) != -1 && e.Button == MouseButtons.Left) {
                    AddFieldToSelectedList();
                } 
            }
 
            /// <devdoc> 
            /// double-clicking moves a selected field to the available list
            /// </devdoc> 
            private void OnDoubleClickSelectedField(object sender, MouseEventArgs e) {
                if (selectedFieldsList.IndexFromPoint(e.Location) != -1 && e.Button == MouseButtons.Left) {
                    RemoveFieldFromSelectedList();
                } 
            }
 
            // <summary> 
            //      This is called when the value property in the CollectionForm has changed.
            //      In it you should update your user interface to reflect the current value. 
            // </summary>
            // </doc>
            protected override void OnEditValueChanged() {
                fields = null; 
                fieldsList.Items.Clear();
                selectedFieldsList.Items.Clear(); 
 
                fields = new ArrayList();
                foreach (string field in GetControlDataFieldNames()) { 
                    fields.Add(field);
                    if (Array.IndexOf(Items, field) < 0) {
                        fieldsList.Items.Add(field);
                    } 
                }
 
                foreach (string field in Items) { 
                    selectedFieldsList.Items.Add(field);
                } 

                if (fieldsList.Items.Count > 0) {
                    fieldsList.SelectedIndex = 0;
                } 

                SetButtonsEnabled(); 
            } 

            /// <devdoc> 
            /// event handler for selectedIndexChanged on the fieldsList
            /// </devdoc>
            private void OnFieldsSelectedIndexChanged(object sender, EventArgs e) {
                if (fieldsList.SelectedIndex > -1) { 
                    selectedFieldsList.SelectedIndex = -1;
                } 
                SetButtonsEnabled(); 
            }
 
            /// <devdoc>
            /// event handler for Enter (the accessible version of mouse double-click)
            /// </devdoc>
            private void OnKeyPressField(object sender, KeyPressEventArgs e) { 
                if (e.KeyChar == (char)13) {
                    AddFieldToSelectedList(); 
                    e.Handled = true; 
                }
            } 

            /// <devdoc>
            /// event handler for Enter (the accessible version of mouse double-click)
            /// </devdoc> 
            private void OnKeyPressSelectedField(object sender, KeyPressEventArgs e) {
                if (e.KeyChar == (char)13) { 
                    RemoveFieldFromSelectedList(); 
                    e.Handled = true;
                } 
            }

            /// <devdoc>
            /// event handler for the move down button 
            /// </devdoc>
            private void OnMoveDown(object sender, EventArgs e) { 
                int selectedIndex = selectedFieldsList.SelectedIndex; 
                object selectedItem = selectedFieldsList.SelectedItem;
 
                selectedFieldsList.Items.RemoveAt(selectedIndex);
                selectedFieldsList.Items.Insert(selectedIndex + 1, selectedItem);
                selectedFieldsList.SelectedIndex = selectedIndex + 1;
            } 

            /// <devdoc> 
            /// event handler for the move left button 
            /// </devdoc>
            private void OnMoveLeft(object sender, EventArgs e) { 
                RemoveFieldFromSelectedList();
            }

            /// <devdoc> 
            /// event handler for the move right button
            /// </devdoc> 
            private void OnMoveRight(object sender, EventArgs e) { 
                AddFieldToSelectedList();
            } 

            /// <devdoc>
            /// event handler for the move up button
            /// </devdoc> 
            private void OnMoveUp(object sender, EventArgs e) {
                int selectedIndex = selectedFieldsList.SelectedIndex; 
                object selectedItem = selectedFieldsList.SelectedItem; 

                selectedFieldsList.Items.RemoveAt(selectedIndex); 
                selectedFieldsList.Items.Insert(selectedIndex - 1, selectedItem);
                selectedFieldsList.SelectedIndex = selectedIndex - 1;
            }
 
            /// <devdoc>
            /// event handler for selectedIndexChanged on the selectedFieldsList 
            /// </devdoc> 
            private void OnSelectedFieldsSelectedIndexChanged(object sender, EventArgs e) {
                if (selectedFieldsList.SelectedIndex > -1) { 
                    fieldsList.SelectedIndex = -1;
                }
                SetButtonsEnabled();
            } 

            /// <devdoc> 
            /// moves a field from the selected list to the list of available fields 
            /// </devdoc>
            private void RemoveFieldFromSelectedList() { 
                int selectedFieldIndex = selectedFieldsList.SelectedIndex;
                string selectedItem;
                int fieldListIndex = 0;
                int fieldIndex = 0; 

                if (selectedFieldIndex >= 0) { 
                    selectedItem = selectedFieldsList.SelectedItem.ToString(); 
                    fieldIndex = fields.IndexOf(selectedItem);
 
                    // find the right place in the list for the item
                    for (int i = 0; i < fieldsList.Items.Count; i++) {
                        if (fields.IndexOf(fieldsList.Items[i]) > fieldIndex) {
                            break; 
                        }
                        fieldListIndex++; 
                    } 

                    fieldsList.Items.Insert(fieldListIndex, selectedItem); 
                    selectedFieldsList.Items.RemoveAt(selectedFieldIndex);
                    fieldsList.SelectedIndex = fieldListIndex;

                    if (selectedFieldsList.Items.Count > 0) { 
                        selectedFieldsList.SelectedIndex = (selectedFieldsList.Items.Count > selectedFieldIndex) ? selectedFieldIndex : selectedFieldsList.Items.Count - 1;
                    } 
                } 
            }
 
            /// <devdoc>
            /// set the enabled properties for the left and right, up and down move buttons based on list contents
            /// </devdoc>
            private void SetButtonsEnabled() { 
                int selectedFieldsCount = selectedFieldsList.Items.Count;
                int selectedFieldsIndex = selectedFieldsList.SelectedIndex; 
                bool moveUpEnabled = false; 
                bool moveDownEnabled = false;
                bool moveRightEnabled = false; 
                bool moveLeftEnabled = false;

                if (fieldsList.SelectedIndex > -1) {
                    moveRightEnabled = true; 
                }
 
                if (selectedFieldsIndex > -1) { 
                    moveLeftEnabled = true;
                    if (selectedFieldsCount > 0) { 
                        if (selectedFieldsIndex > 0) {
                            moveUpEnabled = true;
                        }
                        if (selectedFieldsIndex < selectedFieldsCount - 1) { 
                            moveDownEnabled = true;
                        } 
                    } 
                }
 
                moveRight.Enabled = moveRightEnabled;
                moveLeft.Enabled = moveLeftEnabled;
                moveUp.Enabled = moveUpEnabled;
                moveDown.Enabled = moveDownEnabled; 
            }
 
            /// <devdoc> 
            /// Overridden to reroute the context-help button to our own handler.
            /// </devdoc> 
            protected override void WndProc(ref Message m) {
                if ((m.Msg == WM_SYSCOMMAND) && ((int)m.WParam == SC_CONTEXTHELP)) {
                    // Show help
                    if (_serviceProvider != null) { 
                        IHelpService helpService = (IHelpService)_serviceProvider.GetService(typeof(IHelpService));
                        if (helpService != null) { 
                            helpService.ShowHelpFromKeyword("net.Asp.DataFieldCollectionEditor"); 
                        }
                    } 
                }
                else {
                    base.WndProc(ref m);
                } 
            }
 
            private class ListBoxWithEnter : ListBox { 
                protected override bool IsInputKey(Keys keyData) {
                    if (keyData == Keys.Enter) { 
                        return true;
                    }
                    return base.IsInputKey(keyData);
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
