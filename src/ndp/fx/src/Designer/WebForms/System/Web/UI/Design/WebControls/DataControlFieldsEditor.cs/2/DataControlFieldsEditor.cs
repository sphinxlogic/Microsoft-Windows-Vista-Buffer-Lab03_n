//------------------------------------------------------------------------------ 
// <copyright file="DataControlFieldsEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.CodeDom; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.IO;
    using System.Text;
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
 
    using ControlCollection = System.Web.UI.ControlCollection;
    using DataControlField = System.Web.UI.WebControls.DataControlField; 
    using DataControlFieldCollection = System.Web.UI.WebControls.DataControlFieldCollection;
    using DataBinding = System.Web.UI.DataBinding;
    using GridView = System.Web.UI.WebControls.GridView;
 
    using Button = System.Windows.Forms.Button;
    using CheckBox = System.Windows.Forms.CheckBox; 
    using Color = System.Drawing.Color; 
    using Image = System.Drawing.Image;
    using Label = System.Windows.Forms.Label; 
    using ListViewItem = System.Windows.Forms.ListViewItem;
    using Panel = System.Windows.Forms.Panel;
    using TextBox = System.Windows.Forms.TextBox;
    using TreeNode = System.Windows.Forms.TreeNode; 
    using TreeView = System.Windows.Forms.TreeView;
    using View = System.Windows.Forms.View; 
 
    /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor"]/*' />
    /// <devdoc> 
    ///   The Data page for DataBoundControls with DataControlFields
    /// </devdoc>
    /// <internalonly/>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    internal sealed class DataControlFieldsEditor : DesignerForm {
 
        private const int ILI_DATASOURCE = 0; 
        private const int ILI_BOUND = 1;
        private const int ILI_ALL = 2; 
        private const int ILI_CUSTOM = 3;
        private const int ILI_BUTTON = 4;
        private const int ILI_SELECTBUTTON = 5;
        private const int ILI_EDITBUTTON = 6; 
        private const int ILI_DELETEBUTTON = 7;
        private const int ILI_HYPERLINK = 8; 
        private const int ILI_TEMPLATE = 9; 
        private const int ILI_CHECKBOX = 10;
        private const int ILI_INSERTBUTTON = 11; 
        private const int ILI_COMMAND = 12;
        private const int ILI_BOOLDATASOURCE = 13;
        private const int ILI_IMAGE = 14;
 
        private const int CF_EDIT = 0;
        private const int CF_INSERT = 1; 
        private const int CF_SELECT = 2; 
        private const int CF_DELETE = 3;
 
        private const int MODE_READONLY = 0;
        private const int MODE_EDIT = 1;
        private const int MODE_INSERT = 2;
 
        private TreeViewWithEnter _availableFieldsTree;
        private System.Windows.Forms.Button _addFieldButton; 
        private ListViewWithEnter _selFieldsList; 
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton; 
        private System.Windows.Forms.Button _moveFieldUpButton;
        private System.Windows.Forms.Button _moveFieldDownButton;
        private System.Windows.Forms.Button _deleteFieldButton;
        private System.Windows.Forms.PropertyGrid _currentFieldProps; 
        private System.Windows.Forms.LinkLabel _refreshSchemaLink;
        private System.Windows.Forms.LinkLabel _templatizeLink; 
        private System.Windows.Forms.CheckBox _autoFieldCheck; 
        private System.Windows.Forms.Label _selFieldLabel;
        private System.Windows.Forms.Label _availableFieldsLabel; 
        private System.Windows.Forms.Label _selFieldsLabel;

        private DataSourceNode _selectedDataSourceNode;
        private BoolDataSourceNode _selectedCheckBoxDataSourceNode; 
        private FieldItem _currentFieldItem;
        private bool _propChangesPending; 
        private bool _fieldMovePending; 
        private DataControlFieldCollection _clonedFieldCollection;
 
        private DataBoundControlDesigner _controlDesigner;
        private bool _isLoading;

        private IDataSourceFieldSchema[] _fieldSchemas; 
        private IDataSourceViewSchema _viewSchema;
 
        private bool _initialActivate; 
        private bool _initialIgnoreRefreshSchemaValue;
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.DataControlFieldsEditor"]/*' />
        /// <devdoc>
        ///   Creates a new instance of DataControlFieldsEditor.
        /// </devdoc> 
        public DataControlFieldsEditor(DataBoundControlDesigner controlDesigner) : base(controlDesigner.Component.Site) {
            this._controlDesigner = controlDesigner; 
            InitializeComponent(); 
            InitForm();
            _initialActivate = true; 
            IgnoreRefreshSchemaEvents();
        }

        /// <devdoc> 
        /// Gets and sets the runtime control's AutoGenerate property for fields.
        /// </devdoc> 
        private bool AutoGenerateFields { 
            get {
                if (Control is GridView) { 
                    return ((GridView)Control).AutoGenerateColumns;
                }
                else if (Control is DetailsView) {
                    return ((DetailsView)Control).AutoGenerateRows; 
                }
                Debug.Assert(false, "The control must be either a DetailsView or a GridView"); 
                return false; 
            }
            set { 
                if (Control is GridView) {
                    ((GridView)Control).AutoGenerateColumns = value;
                }
                else if (Control is DetailsView) { 
                    ((DetailsView)Control).AutoGenerateRows = value;
                } 
                else { 
                    Debug.Assert(false, "The control must be either a DetailsView or a GridView");
                } 
            }
        }

        private DataBoundControl Control { 
            get {
                return _controlDesigner.Component as DataBoundControl; 
            } 
        }
 
        /// <devdoc>
        /// Returns the DataControlFieldCollection of the runtime control.
        /// </devdoc>
        private DataControlFieldCollection FieldCollection { 
            get {
                if (_clonedFieldCollection == null) { 
                    if (Control is GridView) { 
                        DataControlFieldCollection oldFields = ((GridView)Control).Columns;
                        _clonedFieldCollection = oldFields.CloneFields(); 
                        for (int i = 0; i < oldFields.Count; i++) {
                            _controlDesigner.RegisterClone(oldFields[i], _clonedFieldCollection[i]);
                        }
                    } 
                    else if (Control is DetailsView) {
                        DataControlFieldCollection oldFields = ((DetailsView)Control).Fields; 
                        _clonedFieldCollection = oldFields.CloneFields(); 
                        for (int i = 0; i < oldFields.Count; i++) {
                            _controlDesigner.RegisterClone(oldFields[i], _clonedFieldCollection[i]); 
                        }
                    }
                    else {
                        Debug.Assert(false, "The control must be either a DetailsView or a GridView"); 
                    }
                } 
                return _clonedFieldCollection; 
            }
        } 

        protected override string HelpTopic {
            get {
                return "net.Asp.DataControlField.DataControlFieldEditor"; 
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
 
        private void EnterLoadingMode() {
            _isLoading = true;
        }
 
        private void ExitLoadingMode() {
            _isLoading = false; 
        } 

        // create a unique id for controls generated by this dialog 
        private string GetNewDataSourceName(Type controlType, int editMode) {
            int buttonNameStartIndex = 1;
            return GetNewDataSourceName(controlType, editMode, ref buttonNameStartIndex);
        } 

        // create a unique id for controls generated by this dialog 
        private string GetNewDataSourceName(Type controlType, int editMode, ref int startIndex) { 
            int currentIndex = startIndex;
 
            DataControlFieldCollection fields = new DataControlFieldCollection();
            int fieldCount = _selFieldsList.Items.Count;

            // create shallow copy of Fields collection 
            for (int i = 0; i < fieldCount; i++) {
                FieldItem fieldItem = (FieldItem)_selFieldsList.Items[i]; 
                fields.Add(fieldItem.RuntimeField); 
            }
 
            if (fields != null && fields.Count > 0) {
                bool foundFreeIndex = false;
                while (!foundFreeIndex) {
                    for (int i = 0; i < fields.Count; i++) { 
                        DataControlField field = fields[i];
                        if (field is TemplateField) { 
                            ITemplate template = null; 
                            switch (editMode) {
                                case MODE_READONLY: 
                                    template = ((TemplateField)field).ItemTemplate;
                                    break;
                                case MODE_EDIT:
                                    template = ((TemplateField)field).EditItemTemplate; 
                                    break;
                                case MODE_INSERT: 
                                    template = ((TemplateField)field).InsertItemTemplate; 
                                    break;
                            } 
                            if (template != null) {
                                IDesignerHost designerHost = (IDesignerHost)Control.Site.GetService(typeof(IDesignerHost));
                                string templateContents = ControlSerializer.SerializeTemplate(template, designerHost);
                                if (templateContents.Contains(controlType.Name + currentIndex.ToString(NumberFormatInfo.InvariantInfo))) { 
                                    currentIndex++;
                                    break; 
                                } 
                            }
                        } 
                        if (i == (fields.Count - 1)) {
                            foundFreeIndex = true;
                        }
                    } 
                }
            } 
            startIndex = currentIndex; 
            return controlType.Name + currentIndex.ToString(NumberFormatInfo.InvariantInfo);
        } 

        /// <devdoc>
        /// Returns an the IDataSourceViewSchema of the associated DataSource
        /// </devdoc> 
        private IDataSourceViewSchema GetViewSchema() {
            if (_viewSchema == null) { 
                if (_controlDesigner != null) { 
                    DesignerDataSourceView view = _controlDesigner.DesignerView;
                    if (view != null) { 
                        try {
                            _viewSchema = view.Schema;
                        }
                        catch (Exception ex) { 
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)ServiceProvider.GetService(typeof(IComponentDesignerDebugService));
                            if (debugService != null) { 
                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message)); 
                            }
                        } 
                    }
                }
            }
 
            return _viewSchema;
        } 
 
        /// <devdoc>
        /// Returns an array of IDataSourceFieldSchema objects for the control being edited, paying attention 
        /// to DataMember if there is one.
        /// </devdoc>
        private IDataSourceFieldSchema[] GetFieldSchemas() {
            if (_fieldSchemas == null) { 
                IDataSourceViewSchema viewSchema = GetViewSchema();
                if (viewSchema != null) { 
                    _fieldSchemas = viewSchema.GetFields(); 
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

        #region Windows Form Designer generated code 
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent() { 
            this._availableFieldsTree = new TreeViewWithEnter ();
            this._selFieldsList = new ListViewWithEnter ();
            this._okButton = new System.Windows.Forms.Button ();
            this._cancelButton = new System.Windows.Forms.Button (); 
            this._moveFieldUpButton = new System.Windows.Forms.Button ();
            this._moveFieldDownButton = new System.Windows.Forms.Button (); 
            this._addFieldButton = new System.Windows.Forms.Button (); 
            this._deleteFieldButton = new System.Windows.Forms.Button ();
            this._currentFieldProps = new System.Windows.Forms.Design.VsPropertyGrid (ServiceProvider); 
            this._autoFieldCheck = new System.Windows.Forms.CheckBox ();
            this._refreshSchemaLink = new System.Windows.Forms.LinkLabel ();
            this._templatizeLink = new System.Windows.Forms.LinkLabel ();
            this._selFieldLabel = new System.Windows.Forms.Label (); 
            this._availableFieldsLabel = new System.Windows.Forms.Label ();
            this._selFieldsLabel = new System.Windows.Forms.Label (); 
            this.SuspendLayout (); 

            // 
            // _availableFieldsTree
            //
            this._availableFieldsTree.HideSelection = false;
            this._availableFieldsTree.ImageIndex = -1; 
            this._availableFieldsTree.Indent = 15;
            this._availableFieldsTree.Location = new System.Drawing.Point (12, 28); 
            this._availableFieldsTree.Name = "_availableFieldsTree"; 
            this._availableFieldsTree.SelectedImageIndex = -1;
            this._availableFieldsTree.Size = new System.Drawing.Size (196, 116); 
            this._availableFieldsTree.TabIndex = 1;
            this._availableFieldsTree.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler (this.OnAvailableFieldsDoubleClick);
            this._availableFieldsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler (this.OnSelChangedAvailableFields);
            this._availableFieldsTree.GotFocus += new System.EventHandler (this.OnAvailableFieldsGotFocus); 
            this._availableFieldsTree.KeyPress += new KeyPressEventHandler(this.OnAvailableFieldsKeyPress);
 
            // 
            // _selFieldsList
            // 
            this._selFieldsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._selFieldsList.HideSelection = false;
            this._selFieldsList.LabelWrap = false;
            this._selFieldsList.Location = new System.Drawing.Point (12, 197); 
            this._selFieldsList.MultiSelect = false;
            this._selFieldsList.Name = "_selFieldsList"; 
            this._selFieldsList.Size = new System.Drawing.Size (164, 112); 
            this._selFieldsList.TabIndex = 4;
            this._selFieldsList.View = System.Windows.Forms.View.Details; 
            this._selFieldsList.KeyDown += new System.Windows.Forms.KeyEventHandler (this.OnSelFieldsListKeyDown);
            this._selFieldsList.SelectedIndexChanged += new System.EventHandler (this.OnSelIndexChangedSelFieldsList);
            this._selFieldsList.ItemActivate += new System.EventHandler (this.OnClickDeleteField);
            this._selFieldsList.GotFocus += new System.EventHandler (this.OnSelFieldsListGotFocus); 

            // 
            // _okButton 
            //
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point (340, 350);
            this._okButton.Name = "_okButton"; 
            this._okButton.TabIndex = 100;
            this._okButton.Click += new System.EventHandler (this.OnClickOK); 
 
            //
            // _cancelButton 
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._cancelButton.Location = new System.Drawing.Point (420, 350);
            this._cancelButton.Name = "_cancelButton"; 
            this._cancelButton.TabIndex = 101; 

            // 
            // _moveFieldUpButton
            //
            this._moveFieldUpButton.Location = new System.Drawing.Point (186, 197);
            this._moveFieldUpButton.Name = "_moveFieldUpButton"; 
            this._moveFieldUpButton.Size = new System.Drawing.Size (26, 23);
            this._moveFieldUpButton.TabIndex = 5; 
            this._moveFieldUpButton.Click += new System.EventHandler (this.OnClickMoveFieldUp); 

            // 
            // _moveFieldDownButton
            //
            this._moveFieldDownButton.Location = new System.Drawing.Point (186, 221);
            this._moveFieldDownButton.Name = "_moveFieldDownButton"; 
            this._moveFieldDownButton.Size = new System.Drawing.Size (26, 23);
            this._moveFieldDownButton.TabIndex = 6; 
            this._moveFieldDownButton.Click += new System.EventHandler (this.OnClickMoveFieldDown); 

            // 
            // _addFieldButton
            //
            this._addFieldButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._addFieldButton.Location = new System.Drawing.Point (123, 150); 
            this._addFieldButton.Name = "_addFieldButton";
            this._addFieldButton.Size = new System.Drawing.Size (85, 23); 
            this._addFieldButton.TabIndex = 2; 
            this._addFieldButton.Click += new System.EventHandler (this.OnClickAddField);
 
            //
            // _deleteFieldButton
            //
            this._deleteFieldButton.Location = new System.Drawing.Point (186, 245); 
            this._deleteFieldButton.Name = "_deleteFieldButton";
            this._deleteFieldButton.Size = new System.Drawing.Size (26, 23); 
            this._deleteFieldButton.TabIndex = 7; 
            this._deleteFieldButton.Click += new System.EventHandler (this.OnClickDeleteField);
 
            //
            // _currentFieldProps
            //
            this._currentFieldProps.CommandsVisibleIfAvailable = true; 
            this._currentFieldProps.Enabled = false;
            this._currentFieldProps.LargeButtons = false; 
            this._currentFieldProps.LineColor = System.Drawing.SystemColors.ScrollBar; 
            this._currentFieldProps.Location = new System.Drawing.Point (244, 28);
            this._currentFieldProps.Name = "_currentFieldProps"; 
            this._currentFieldProps.Size = new System.Drawing.Size (248, 281);
            this._currentFieldProps.TabIndex = 9;
            this._currentFieldProps.ToolbarVisible = true;
            this._currentFieldProps.ViewBackColor = System.Drawing.SystemColors.Window; 
            this._currentFieldProps.ViewForeColor = System.Drawing.SystemColors.WindowText;
            this._currentFieldProps.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler (this.OnChangedPropertyValues); 
 
            //
            // _autoFieldCheck 
            //
            this._autoFieldCheck.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._autoFieldCheck.Location = new System.Drawing.Point (12, 313);
            this._autoFieldCheck.Name = "_autoFieldCheck"; 
            this._autoFieldCheck.Size = new System.Drawing.Size (172, 24);
            this._autoFieldCheck.TabIndex = 10; 
            this._autoFieldCheck.CheckedChanged += new System.EventHandler (this.OnCheckChangedAutoField); 
            this._autoFieldCheck.TextAlign = ContentAlignment.TopLeft;
            this._autoFieldCheck.CheckAlign = ContentAlignment.TopLeft; 

            //
            // _refreshSchemaLink
            // 
            this._refreshSchemaLink.Location = new System.Drawing.Point (12, 347);
            this._refreshSchemaLink.Name = "_refreshSchemaLink"; 
            this._refreshSchemaLink.Size = new System.Drawing.Size (196, 16); 
            this._refreshSchemaLink.TabIndex = 11;
            this._refreshSchemaLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler (this.OnClickRefreshSchema); 

            //
            // _templatizeLink
            // 
            this._templatizeLink.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._templatizeLink.Location = new System.Drawing.Point (244, 313); 
            this._templatizeLink.Name = "_templatizeLink"; 
            this._templatizeLink.Size = new System.Drawing.Size (248, 32);
            this._templatizeLink.TabIndex = 12; 
            this._templatizeLink.Visible = false;
            this._templatizeLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler (this.OnClickTemplatize);

            // 
            // _selFieldLabel
            // 
            this._selFieldLabel.Location = new System.Drawing.Point (244, 12); 
            this._selFieldLabel.Name = "_selFieldLabel";
            this._selFieldLabel.Size = new System.Drawing.Size (248, 16); 
            this._selFieldLabel.TabIndex = 8;

            //
            // _availableFieldsLabel 
            //
            this._availableFieldsLabel.Location = new System.Drawing.Point (12, 12); 
            this._availableFieldsLabel.Name = "_availableFieldsLabel"; 
            this._availableFieldsLabel.Size = new System.Drawing.Size (196, 16);
            this._availableFieldsLabel.TabIndex = 0; 

            //
            // _selFieldsLabel
            // 
            this._selFieldsLabel.Location = new System.Drawing.Point (12, 181);
            this._selFieldsLabel.Name = "_selFieldsLabel"; 
            this._selFieldsLabel.Size = new System.Drawing.Size (196, 16); 
            this._selFieldsLabel.TabIndex = 3;
 
            //
            // Form1
            //
            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size (507, 385); 
            this.Controls.Add (this._selFieldsLabel); 
            this.Controls.Add (this._availableFieldsLabel);
            this.Controls.Add (this._selFieldLabel); 
            this.Controls.Add (this._templatizeLink);
            this.Controls.Add (this._refreshSchemaLink);
            this.Controls.Add (this._autoFieldCheck);
            this.Controls.Add (this._currentFieldProps); 
            this.Controls.Add (this._deleteFieldButton);
            this.Controls.Add (this._addFieldButton); 
            this.Controls.Add (this._moveFieldDownButton); 
            this.Controls.Add (this._moveFieldUpButton);
            this.Controls.Add (this._cancelButton); 
            this.Controls.Add (this._okButton);
            this.Controls.Add (this._selFieldsList);
            this.Controls.Add (this._availableFieldsTree);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; 
            this.Name = "Form1";
 
            InitializeForm(); 

            this.ResumeLayout(false); 
        }
        #endregion

        private void InitForm() { 
            Image fieldNodesBitmap = new Bitmap(this.GetType(), "FieldNodes.bmp");
            ImageList fieldImages = new ImageList(); 
            fieldImages.TransparentColor = Color.Magenta; 
            fieldImages.Images.AddStrip(fieldNodesBitmap);
 
            _autoFieldCheck.Text = SR.GetString(SR.DCFEditor_AutoGen);

            _availableFieldsTree.ImageList = fieldImages;
 
            _addFieldButton.Text = SR.GetString(SR.DCFEditor_Add);
 
            ColumnHeader columnHeader = new ColumnHeader(); 
            columnHeader.Width = _selFieldsList.Width - 4;
 
            _selFieldsList.Columns.Add(columnHeader);
            _selFieldsList.SmallImageList = fieldImages;

            Icon moveUpIcon = new Icon(this.GetType(), "SortUp.ico"); 
            Bitmap moveUpBitmap = moveUpIcon.ToBitmap();
            moveUpBitmap.MakeTransparent(); 
            _moveFieldUpButton.Image = moveUpBitmap; 
            _moveFieldUpButton.AccessibleDescription = SR.GetString(SR.DCFEditor_MoveFieldUpDesc);
            _moveFieldUpButton.AccessibleName = SR.GetString(SR.DCFEditor_MoveFieldUpName); 

            Icon moveDownIcon = new Icon(this.GetType(), "SortDown.ico");
            Bitmap moveDownBitmap = moveDownIcon.ToBitmap();
            moveDownBitmap.MakeTransparent(); 
            _moveFieldDownButton.Image = moveDownBitmap;
            _moveFieldDownButton.AccessibleDescription = SR.GetString(SR.DCFEditor_MoveFieldDownDesc); 
            _moveFieldDownButton.AccessibleName = SR.GetString(SR.DCFEditor_MoveFieldDownName); 

            Icon deleteIcon = new Icon(this.GetType(), "Delete.ico"); 
            Bitmap deleteBitmap = deleteIcon.ToBitmap();
            deleteBitmap.MakeTransparent();
            _deleteFieldButton.Image = deleteBitmap;
            _deleteFieldButton.AccessibleDescription = SR.GetString(SR.DCFEditor_DeleteFieldDesc); 
            _deleteFieldButton.AccessibleName = SR.GetString(SR.DCFEditor_DeleteFieldName);
 
            _templatizeLink.Text = SR.GetString(SR.DCFEditor_Templatize); 

            _refreshSchemaLink.Text = SR.GetString(SR.DataSourceDesigner_RefreshSchemaNoHotkey); 
            _refreshSchemaLink.Visible = _controlDesigner.DataSourceDesigner == null ? false : _controlDesigner.DataSourceDesigner.CanRefreshSchema;

            _okButton.Text = SR.GetString(SR.OKCaption);
            _cancelButton.Text = SR.GetString(SR.CancelCaption); 

            _selFieldLabel.Text = SR.GetString(SR.DCFEditor_FieldProps); 
            _availableFieldsLabel.Text = SR.GetString(SR.DCFEditor_AvailableFields); 
            _selFieldsLabel.Text = SR.GetString(SR.DCFEditor_SelectedFields);
 
            _currentFieldProps.Site = _controlDesigner.Component.Site;

            this.Text = SR.GetString(SR.DCFEditor_Text);
            this.Icon = new Icon(this.GetType(), "DataControlFieldsEditor.ico"); 
        }
 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.InitPage"]/*' />
        /// <devdoc> 
        ///   Initializes the page before it can be loaded with the component.
        /// </devdoc>
        private void InitPage() {
            _autoFieldCheck.Checked = false; 

            _selectedDataSourceNode = null; 
            _selectedCheckBoxDataSourceNode = null; 
            _availableFieldsTree.Nodes.Clear();
            _selFieldsList.Items.Clear(); 
            _currentFieldItem = null;

            _propChangesPending = false;
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.LoadFields"]/*' /> 
        /// <devdoc> 
        ///   Loads the fields collection
        /// </devdoc> 
        private void LoadFields() {
            DataControlFieldCollection fields = FieldCollection;

            if (fields != null) { 
                int fieldCount = fields.Count;
 
                IDataSourceViewSchema viewSchema = GetViewSchema(); 
                for (int i = 0; i < fieldCount; i++) {
                    DataControlField field = fields[i]; 
                    FieldItem newItem = null;
                    Type fieldType = field.GetType();

                    // create the associated design time field 
                    if (fieldType == typeof(CheckBoxField)) {
                        newItem = new CheckBoxFieldItem(this, (CheckBoxField)field); 
                    } 
                    else if (fieldType == typeof(BoundField)) {
                        newItem = new BoundFieldItem(this, (BoundField)field); 
                    }
                    else if (fieldType == typeof(ButtonField)) {
                        newItem = new ButtonFieldItem(this, (ButtonField)field);
                    } 
                    else if (fieldType == typeof(HyperLinkField)) {
                        newItem = new HyperLinkFieldItem(this, (HyperLinkField)field); 
                    } 
                    else if (fieldType == typeof(TemplateField)) {
                        newItem = new TemplateFieldItem(this, (TemplateField)field); 
                    }
                    else if (fieldType == typeof(CommandField)) {
                        newItem = new CommandFieldItem(this, (CommandField)field);
                    } 
                    else if (fieldType == typeof(ImageField)) {
                        newItem = new ImageFieldItem(this, (ImageField)field); 
                    } 
                    else {
                        newItem = new CustomFieldItem(this, field); 
                    }

                    newItem.LoadFieldInfo();
 
                    IDataSourceViewSchemaAccessor schemaAccessor = newItem.RuntimeField as IDataSourceViewSchemaAccessor;
                    if (schemaAccessor != null) { 
                        schemaAccessor.DataSourceViewSchema = viewSchema; 
                    }
 
                    _selFieldsList.Items.Add(newItem);
                }

                if (_selFieldsList.Items.Count != 0) { 
                    _currentFieldItem = (FieldItem)_selFieldsList.Items[0];
                    _currentFieldItem.Selected = true; 
                } 
            }
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.LoadComponent"]/*' />
        /// <devdoc>
        ///   Loads the component into the page. 
        /// </devdoc>
        private void LoadComponent() { 
            InitPage(); 

            LoadAvailableFieldsTree(); 
            LoadDataSourceFields();

            _autoFieldCheck.Checked = AutoGenerateFields;
            LoadFields(); 

            UpdateEnabledVisibleState(); 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.LoadDataSourceFields"]/*' /> 
        /// <devdoc>
        ///   Loads the fields present in the selected datasource
        /// </devdoc>
        private void LoadDataSourceFields() { 
            EnterLoadingMode();
 
            Debug.Assert(_controlDesigner != null, "_controlDesigner is null"); 
            IDataSourceFieldSchema[] fieldSchemas = GetFieldSchemas();
 
            if (fieldSchemas != null && fieldSchemas.Length > 0) {
                DataFieldNode allFieldsNode = new DataFieldNode(this);
                _availableFieldsTree.Nodes.Insert(0, allFieldsNode);
                foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) { 
                    BoundNode fieldNode = new BoundNode(this, fieldSchema);
                    _selectedDataSourceNode.Nodes.Add(fieldNode); 
                } 
                _selectedDataSourceNode.Expand();
                foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) { 
                    if (fieldSchema.DataType == typeof(bool) ||
                        fieldSchema.DataType == typeof(bool?)) {
                        CheckBoxNode fieldNode = new CheckBoxNode(this, fieldSchema);
                        _selectedCheckBoxDataSourceNode.Nodes.Add(fieldNode); 
                    }
                } 
                _selectedCheckBoxDataSourceNode.Expand(); 
                _availableFieldsTree.SelectedNode = allFieldsNode;
                allFieldsNode.EnsureVisible(); 
            }
            else {
                BoundNode genericBoundField = new BoundNode(this, null);
                _availableFieldsTree.Nodes.Insert(0, genericBoundField); 
                genericBoundField.EnsureVisible();
 
                CheckBoxNode genericCheckBoxField = new CheckBoxNode(this, null); 
                _availableFieldsTree.Nodes.Insert(1, genericCheckBoxField);
                genericCheckBoxField.EnsureVisible(); 

                _availableFieldsTree.SelectedNode = genericBoundField;
            }
 
            ExitLoadingMode();
        } 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.LoadAvailableFieldsTree"]/*' />
        /// <devdoc> 
        ///    Loads the fixed nodes in the available fields tree, i.e., the
        ///    DataSource, Button and HyperLink nodes
        /// </devdoc>
        private void LoadAvailableFieldsTree() { 
            IDataSourceFieldSchema[] fieldSchemas = GetFieldSchemas();
            if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                _selectedDataSourceNode = new DataSourceNode(); 
                _availableFieldsTree.Nodes.Add(_selectedDataSourceNode);
 
                _selectedCheckBoxDataSourceNode = new BoolDataSourceNode();
                _availableFieldsTree.Nodes.Add(_selectedCheckBoxDataSourceNode);
            }
 
            HyperLinkNode hyperLinkNode = new HyperLinkNode(this);
            _availableFieldsTree.Nodes.Add(hyperLinkNode); 
 
            ImageNode imageNode = new ImageNode(this);
            _availableFieldsTree.Nodes.Add(imageNode); 

            ButtonNode buttonNode = new ButtonNode(this);
            _availableFieldsTree.Nodes.Add(buttonNode);
 
            CommandNode commandNode = new CommandNode(this);
            _availableFieldsTree.Nodes.Add(commandNode); 
 
            CommandNode editCommandNode = new CommandNode(this, CF_EDIT, SR.GetString(SR.DCFEditor_Node_Edit), ILI_EDITBUTTON);
            commandNode.Nodes.Add(editCommandNode); 

            if (Control is GridView) {
                CommandNode selectCommandNode = new CommandNode(this, CF_SELECT, SR.GetString(SR.DCFEditor_Node_Select), ILI_SELECTBUTTON);
                commandNode.Nodes.Add(selectCommandNode); 
            }
 
            CommandNode deleteCommandNode = new CommandNode(this, CF_DELETE, SR.GetString(SR.DCFEditor_Node_Delete), ILI_DELETEBUTTON); 
            commandNode.Nodes.Add(deleteCommandNode);
 
            if (Control is DetailsView) {
                CommandNode insertCommandNode = new CommandNode(this, CF_INSERT, SR.GetString(SR.DCFEditor_Node_Insert), ILI_INSERTBUTTON);
                commandNode.Nodes.Add(insertCommandNode);
            } 

            TemplateNode templateNode = new TemplateNode(this); 
            _availableFieldsTree.Nodes.Add(templateNode); 
        }
 
        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);

            if (_initialActivate) { 
                LoadComponent();
                _initialActivate = false; 
            } 
        }
 
        private void OnAvailableFieldsDoubleClick(object source, TreeNodeMouseClickEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                OnClickAddField(source, e);
            } 
        }
 
        private void OnAvailableFieldsGotFocus(object source, EventArgs e) { 
            _currentFieldProps.SelectedObject = null;
        } 

        private void OnAvailableFieldsKeyPress(object source, KeyPressEventArgs e) {
            if (e.KeyChar == (char)13) {
                OnClickAddField(source, e); 
                e.Handled = true;
            } 
        } 

        /// <devdoc> 
        ///    Handles changes to the field properties made in the field node editor.
        ///    Sets a flag to indicate there are pending changes.
        /// </devdoc>
        private void OnChangedPropertyValues(object source, PropertyValueChangedEventArgs e) { 
            if (_isLoading)
                return; 
 
            if (e.ChangedItem.Label == "HeaderText" || e.ChangedItem.PropertyDescriptor.ComponentType == typeof(CommandField)) {
                _propChangesPending = true; 
                SaveFieldProperties();
                if (_selFieldsList.SelectedItems.Count == 0)
                    _currentFieldItem = null;
                else { 
                    _currentFieldItem = (FieldItem)_selFieldsList.SelectedItems[0];
                    CommandFieldItem commandFieldItem = _currentFieldItem as CommandFieldItem; 
                    if (commandFieldItem != null) { 
                        commandFieldItem.UpdateImageIndex();
                    } 
                }
            }
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnCheckChangedAutoField"]/*' />
        /// <devdoc> 
        ///    Handles changes to the auto field generation choice. 
        ///    When this functionality is turned on, the fields collection is
        ///    cleared, and auto generated fields are shown. When it is turned 
        ///    off, nothing is done, which effectively makes the auto generated
        ///    fields part of the field collection.
        /// </devdoc>
        private void OnCheckChangedAutoField(object source, EventArgs e) { 
            if (_isLoading)
                return; 
 
            UpdateEnabledVisibleState();
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickAddField"]/*' />
        /// <devdoc>
        ///    Adds a field to the field collection 
        /// </devdoc>
        private void OnClickAddField(object source, EventArgs e) { 
            AvailableFieldNode selectedNode = (AvailableFieldNode)_availableFieldsTree.SelectedNode; 

            if (!_addFieldButton.Enabled) { 
                return;
            }

            Debug.Assert((selectedNode != null) && 
                         selectedNode.IsFieldCreator,
                         "Add button should not have been enabled"); 
 
            // first save off any pending changes
            if (_propChangesPending) { 
                SaveFieldProperties();
            }

            if (selectedNode.CreatesMultipleFields == false) { 
                FieldItem field = selectedNode.CreateField();
 
                _selFieldsList.Items.Add(field); 
                _currentFieldItem = field;
                _currentFieldItem.Selected = true; 
                _currentFieldItem.EnsureVisible();
            }
            else {
                IDataSourceFieldSchema[] fieldSchemas = GetFieldSchemas(); 
                FieldItem[] fields = selectedNode.CreateFields(Control, fieldSchemas);
                int fieldCount = fields.Length; 
 
                for (int i = 0; i < fieldCount; i++) {
                    _selFieldsList.Items.Add(fields[i]); 
                }
                _currentFieldItem = fields[fieldCount - 1];
                _currentFieldItem.Selected = true;
                _currentFieldItem.EnsureVisible(); 
            }
 
            IDataSourceViewSchemaAccessor schemaAccessor = _currentFieldItem.RuntimeField as IDataSourceViewSchemaAccessor; 
            if (schemaAccessor != null) {
                schemaAccessor.DataSourceViewSchema = GetViewSchema(); 
            }

            _selFieldsList.Focus();
            _selFieldsList.FocusedItem = _currentFieldItem; 

            UpdateEnabledVisibleState(); 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickDeleteField"]/*' /> 
        /// <devdoc>
        ///   Deletes a field from the field collection.
        /// </devdoc>
        private void OnClickDeleteField(object source, EventArgs e) { 
            Debug.Assert(_currentFieldItem != null, "Must have a field item to delete");
 
            int currentIndex = _currentFieldItem.Index; 
            int nextIndex = -1;
            int itemCount = _selFieldsList.Items.Count; 

            if (itemCount > 1) {
                if (currentIndex == (itemCount - 1))
                    nextIndex = currentIndex - 1; 
                else
                    nextIndex = currentIndex; 
            } 

            // discard changes that might have existed for the field 
            _propChangesPending = false;
            _currentFieldItem.Remove();
            _currentFieldItem = null;
 
            if (nextIndex != -1) {
                _currentFieldItem = (FieldItem)_selFieldsList.Items[nextIndex]; 
                _currentFieldItem.Selected = true; 
                _currentFieldItem.EnsureVisible();
                _deleteFieldButton.Focus(); 
            }

            UpdateEnabledVisibleState();
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickMoveFieldDown"]/*' /> 
        /// <devdoc> 
        ///   Move a field down within the field collection
        /// </devdoc> 
        private void OnClickMoveFieldDown(object source, EventArgs e) {
            Debug.Assert(_currentFieldItem != null, "Must have a field item to move");

            _fieldMovePending = true; 

            int indexCurrent = _currentFieldItem.Index; 
            Debug.Assert(indexCurrent < _selFieldsList.Items.Count - 1, 
                         "Move down not allowed");
 
            ListViewItem temp = _selFieldsList.Items[indexCurrent];
            _selFieldsList.Items.RemoveAt(indexCurrent);
            _selFieldsList.Items.Insert(indexCurrent + 1, temp);
 
            _currentFieldItem = (FieldItem)_selFieldsList.Items[indexCurrent + 1];
            _currentFieldItem.Selected = true; 
            _currentFieldItem.EnsureVisible(); 

            UpdateFieldPositionButtonsState(); 

            // If the down button is disabled but up is enabled, put focus on Up so it doesn't go to Delete.
            if (_moveFieldUpButton.Enabled && !_moveFieldDownButton.Enabled) {
                _moveFieldUpButton.Focus(); 
            }
            _fieldMovePending = false; 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickMoveFieldUp"]/*' /> 
        /// <devdoc>
        ///   Move a field up within the field collection
        /// </devdoc>
        private void OnClickMoveFieldUp(object source, EventArgs e) { 
            Debug.Assert(_currentFieldItem != null, "Must have a field item to move");
 
            _fieldMovePending = true; 

            int indexCurrent = _currentFieldItem.Index; 
            Debug.Assert(indexCurrent > 0, "Move up not allowed");

            ListViewItem temp = _selFieldsList.Items[indexCurrent];
            _selFieldsList.Items.RemoveAt(indexCurrent); 
            _selFieldsList.Items.Insert(indexCurrent - 1, temp);
 
            _currentFieldItem = (FieldItem)_selFieldsList.Items[indexCurrent - 1]; 
            _currentFieldItem.Selected = true;
            _currentFieldItem.EnsureVisible(); 

            UpdateFieldPositionButtonsState();
            _fieldMovePending = false;
        } 

        private void OnClickOK(object source, EventArgs e) { 
            SaveComponent(); 
            PersistClonedFieldsToControl();
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickRefreshSchema"]/*' />
        /// <devdoc>
        ///   Refreshes the schema of the data bound control. 
        /// </devdoc>
        private void OnClickRefreshSchema(object source, LinkLabelLinkClickedEventArgs e) { 
            _fieldSchemas = null; 
            _viewSchema = null;
 
            IDataSourceDesigner dsd = _controlDesigner.DataSourceDesigner;
            if (dsd != null) {
                if (dsd.CanRefreshSchema) {
                    dsd.RefreshSchema(false); 
                }
            } 
 
            IDataSourceViewSchema viewSchema = GetViewSchema();
            foreach (FieldItem fieldItem in _selFieldsList.Items) { 
                IDataSourceViewSchemaAccessor schemaAccessor = fieldItem.RuntimeField as IDataSourceViewSchemaAccessor;
                if (schemaAccessor != null) {
                    schemaAccessor.DataSourceViewSchema = viewSchema;
                } 
            }
            _availableFieldsTree.Nodes.Clear(); 
            LoadAvailableFieldsTree(); 
            LoadDataSourceFields();
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickTemplatize"]/*' />
        /// <devdoc>
        ///   Converts a field into an equivalent template field. 
        /// </devdoc>
        private void OnClickTemplatize(object source, LinkLabelLinkClickedEventArgs e) { 
            Debug.Assert(_currentFieldItem != null, "Must have a field item to templatize"); 
            Debug.Assert((_currentFieldItem is BoundFieldItem) ||
                         (_currentFieldItem is ButtonFieldItem) || 
                         (_currentFieldItem is HyperLinkFieldItem) ||
                         (_currentFieldItem is CheckBoxFieldItem) ||
                         (_currentFieldItem is CommandFieldItem) ||
                         (_currentFieldItem is ImageFieldItem), 
                         "Unexpected type of field being templatized");
 
            if (_propChangesPending) { 
                SaveFieldProperties();
            } 

            //_currentFieldItem.SaveFieldInfo();

            TemplateField newField; 
            TemplateFieldItem newFieldItem;
 
            newField = _currentFieldItem.GetTemplateField(Control); 
            newFieldItem = new TemplateFieldItem(this, newField);
            newFieldItem.LoadFieldInfo(); 

            _selFieldsList.Items[_currentFieldItem.Index] = newFieldItem;

            _currentFieldItem = newFieldItem; 
            _currentFieldItem.Selected = true;
 
            UpdateEnabledVisibleState(); 
        }
 
        protected override void OnClosed(EventArgs e) {
            IDataSourceDesigner dsd = _controlDesigner.DataSourceDesigner;
            if (dsd != null) {
                dsd.ResumeDataSourceEvents(); 
            }
            IgnoreRefreshSchema = _initialIgnoreRefreshSchemaValue; 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnSelChangedAvailableFields"]/*' /> 
        /// <devdoc>
        ///    Handles selection change in the available fields tree.
        /// </devdoc>
        private void OnSelChangedAvailableFields(object source, TreeViewEventArgs e) { 
            UpdateEnabledVisibleState();
        } 
 
        private void OnSelFieldsListGotFocus(object source, EventArgs e) {
            UpdateEnabledVisibleState(); 
        }

        /// <devdoc>
        ///      Handles keypress events for the list box. 
        /// </devdoc>
        private void OnSelFieldsListKeyDown(object sender, KeyEventArgs e) { 
            if (e.KeyData == Keys.Delete) { 
                if (_currentFieldItem != null) {
                    OnClickDeleteField(sender, e); 
                }
                e.Handled = true;
            }
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnSelIndexChangedSelFieldsList"]/*' /> 
        /// <devdoc> 
        ///    Handles selection change within the selected fields list.
        /// </devdoc> 
        private void OnSelIndexChangedSelFieldsList(object source, EventArgs e) {
            if (_fieldMovePending) {
                return;
            } 

            if (_propChangesPending) { 
                SaveFieldProperties(); 
            }
 
            if (_selFieldsList.SelectedItems.Count == 0)
                _currentFieldItem = null;
            else
                _currentFieldItem = (FieldItem)_selFieldsList.SelectedItems[0]; 

            SetFieldPropertyHeader(); 
            UpdateEnabledVisibleState(); 
        }
 
        private void PersistClonedFieldsToControl() {
            DataControlFieldCollection controlFields = null;
            if (Control is GridView) {
                controlFields = ((GridView)Control).Columns; 
            }
            else if (Control is DetailsView) { 
                controlFields = ((DetailsView)Control).Fields; 
            }
 
            if (controlFields != null) {
                controlFields.Clear();
                foreach (DataControlField field in FieldCollection) {
                    controlFields.Add(field); 
                }
            } 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.SaveFieldProperties"]/*' /> 
        /// <devdoc>
        ///   Saves the properties of a field from the ui
        /// </devdoc>
        private void SaveFieldProperties() { 
            Debug.Assert(_propChangesPending == true, "Unneccessary call to SaveFieldProperties.");
 
            if (_currentFieldItem != null) { 
                _currentFieldItem.HeaderText = _currentFieldItem.RuntimeField.HeaderText;
 
                if (_currentFieldProps.Visible) {
                    _currentFieldProps.Refresh();
                }
            } 

            _propChangesPending = false; 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.SaveComponent"]/*' /> 
        /// <devdoc>
        ///   Saves the component loaded into the page.
        /// </devdoc>
        private void SaveComponent() { 
            if (_propChangesPending) {
                SaveFieldProperties(); 
            } 

            AutoGenerateFields = _autoFieldCheck.Checked; 

            // save the fields collection
            DataControlFieldCollection fields = FieldCollection;
 
            if (fields != null) {
                fields.Clear(); 
                int fieldCount = _selFieldsList.Items.Count; 

                for (int i = 0; i < fieldCount; i++) { 
                    FieldItem fieldItem = (FieldItem)_selFieldsList.Items[i];
                    fields.Add(fieldItem.RuntimeField);
                }
            } 
        }
 
        /// <devdoc> 
        ///   Sets the label above the selected fields property grid.  Shows the type of the field.
        /// </devdoc> 
        private void SetFieldPropertyHeader() {
            string propGroupText = SR.GetString(SR.DCFEditor_FieldProps);

            if (_currentFieldItem != null) { 
                EnterLoadingMode();
                Type currentFieldItemType = _currentFieldItem.GetType(); 
 
                if (currentFieldItemType == typeof(CheckBoxFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_CheckBox)); 
                }
                else if (currentFieldItemType == typeof(BoundFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Bound));
                } 
                else if (currentFieldItemType == typeof(ButtonFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Button)); 
                } 
                else if (currentFieldItemType == typeof(HyperLinkFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_HyperLink)); 
                }
                else if (currentFieldItemType == typeof(CommandFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Command));
                } 
                else if (currentFieldItemType == typeof(TemplateFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Template)); 
                } 
                else if (currentFieldItemType == typeof(ImageFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Image)); 
                }

                ExitLoadingMode();
            } 
            _selFieldLabel.Text = propGroupText;
        } 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.UpdateEnabledVisibleState"]/*' />
        /// <devdoc> 
        /// </devdoc>
        private void UpdateEnabledVisibleState() {
            AvailableFieldNode selFieldNode = (AvailableFieldNode)_availableFieldsTree.SelectedNode;
 
            int fieldCount = _selFieldsList.Items.Count;
            int selFieldCount = _selFieldsList.SelectedItems.Count; 
            FieldItem selField = null; 
            int selFieldIndex = -1;
 
            if (selFieldCount != 0)
                selField = (FieldItem)_selFieldsList.SelectedItems[0];
            if (selField != null)
                selFieldIndex = selField.Index; 

            bool fieldSelected = (selFieldIndex != -1); 
 
            _addFieldButton.Enabled = (selFieldNode != null) && selFieldNode.IsFieldCreator;
            _deleteFieldButton.Enabled = fieldSelected; 
            UpdateFieldPositionButtonsState();

            _currentFieldProps.Enabled = selField != null;
 
            _currentFieldProps.SelectedObject = (selField != null && _selFieldsList.Focused) ? selField.RuntimeField : null;
 
            Type selFieldType = selField == null ? null : selField.RuntimeField.GetType(); 

            _templatizeLink.Visible = (fieldCount != 0 && selField != null && 
                                      (selFieldType == typeof(BoundField) ||
                                       selFieldType == typeof(CheckBoxField) ||
                                       selFieldType == typeof(ButtonField) ||
                                       selFieldType == typeof(HyperLinkField) || 
                                       selFieldType == typeof(CommandField) ||
                                       selFieldType == typeof(ImageField))); 
        } 

        private void UpdateFieldPositionButtonsState() { 
            int selFieldIndex = -1;
            int selFieldCount = _selFieldsList.SelectedItems.Count;
            FieldItem selField = null;
 
            if (selFieldCount > 0) {
                selField = _selFieldsList.SelectedItems[0] as FieldItem; 
            } 
            if (selField != null) {
                selFieldIndex = selField.Index; 
            }

            _moveFieldUpButton.Enabled = (selFieldIndex > 0);
            _moveFieldDownButton.Enabled = (selFieldIndex >= 0) && (selFieldIndex < (_selFieldsList.Items.Count - 1)); 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.AvailableFieldNode"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        private abstract class AvailableFieldNode : TreeNode {
            public AvailableFieldNode(string text, int icon) : base(text, icon, icon) {
            }
 
            public virtual bool CreatesMultipleFields {
                get { 
                    return false; 
                }
            } 

            public virtual bool IsFieldCreator {
                get {
                    return true; 
                }
            } 
 
            public virtual FieldItem CreateField() {
                return null; 
            }

            public virtual FieldItem[] CreateFields(DataBoundControl control, IDataSourceFieldSchema[] fieldSchemas) {
                return null; 
            }
        } 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.DataSourceNode"]/*' />
        /// <devdoc> 
        ///   This represents the datasource in the available fields tree.  This is used if there is a schema.
        /// </devdoc>
        private class DataSourceNode : AvailableFieldNode {
            public DataSourceNode() : base(SR.GetString(SR.DCFEditor_Node_Bound), DataControlFieldsEditor.ILI_DATASOURCE) { 
            }
 
            public override bool IsFieldCreator { 
                get {
                    return false; 
                }
            }
        }
 
        /// <devdoc>
        ///   This represents the check box field candidates in the available fields tree.  This is used if there is a schema. 
        /// </devdoc> 
        private class BoolDataSourceNode : AvailableFieldNode {
            public BoolDataSourceNode() : base(SR.GetString(SR.DCFEditor_Node_CheckBox), DataControlFieldsEditor.ILI_BOOLDATASOURCE) { 
            }

            public override bool IsFieldCreator {
                get { 
                    return false;
                } 
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.DataFieldNode"]/*' />
        /// <devdoc>
        ///   This represents the pseudo field implying all datafields.
        /// </devdoc> 
        private class DataFieldNode : AvailableFieldNode {
            private DataControlFieldsEditor _fieldsEditor; 
 
            public DataFieldNode(DataControlFieldsEditor fieldsEditor) : base(SR.GetString(SR.DCFEditor_Node_AllFields), DataControlFieldsEditor.ILI_ALL) {
                _fieldsEditor = fieldsEditor; 
            }

            public override bool CreatesMultipleFields {
                get { 
                    return true;
                } 
            } 

            public override FieldItem CreateField() { 
                throw new NotSupportedException();
            }

            public override FieldItem[] CreateFields(DataBoundControl control, IDataSourceFieldSchema[] fieldSchemas) { 
                if (fieldSchemas == null) {
                    return null; 
                } 

                ArrayList createdFields = new ArrayList(); 

                foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) {
                    if ((control is GridView && ((GridView)control).IsBindableType(fieldSchema.DataType)) ||
                        (control is DetailsView && ((DetailsView)control).IsBindableType(fieldSchema.DataType))) { 
                        BoundField runtimeField = null;
                        FieldItem field = null; 
                        string fieldSchemaName = fieldSchema.Name; 
                        if (fieldSchema.DataType == typeof(bool) ||
                            fieldSchema.DataType == typeof(bool?)) { 
                            runtimeField = new CheckBoxField();
                            runtimeField.HeaderText = fieldSchemaName;
                            runtimeField.DataField = fieldSchemaName;
                            runtimeField.SortExpression = fieldSchemaName; 

                            field = new CheckBoxFieldItem(_fieldsEditor, (CheckBoxField)runtimeField); 
                        } 
                        else {
                            runtimeField = new BoundField(); 
                            runtimeField.HeaderText = fieldSchemaName;
                            runtimeField.DataField = fieldSchemaName;
                            runtimeField.SortExpression = fieldSchemaName;
 
                            field = new BoundFieldItem(_fieldsEditor, runtimeField);
                        } 
                        if (fieldSchema.PrimaryKey) { 
                            runtimeField.ReadOnly = true;
                        } 
                        if (fieldSchema.Identity) {
                            runtimeField.InsertVisible = false;
                        }
 
                        field.LoadFieldInfo();
                        createdFields.Add(field); 
                    } 
                }
 
                return (FieldItem[])createdFields.ToArray(typeof(FieldItem));
            }
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.BoundNode"]/*' />
        /// <devdoc> 
        ///   This represents a boundfield available in the selected datasource within 
        ///   in the available fields tree.
        ///   It could also represent the pseudo field implying all datafields. 
        /// </devdoc>
        private class BoundNode : AvailableFieldNode {
            protected IDataSourceFieldSchema _fieldSchema;
            private bool _genericBoundField; 
            private DataControlFieldsEditor _fieldsEditor;
 
            public BoundNode(DataControlFieldsEditor fieldsEditor, IDataSourceFieldSchema fieldSchema) : base(fieldSchema == null ? String.Empty : fieldSchema.Name, DataControlFieldsEditor.ILI_BOUND) { 
                this._fieldSchema = fieldSchema;
                _fieldsEditor = fieldsEditor; 
                if (fieldSchema == null) {
                    _genericBoundField = true;
                    Text = SR.GetString(SR.DCFEditor_Node_Bound);
                } 
            }
 
            public override FieldItem CreateField() { 
                BoundField runtimeField = new BoundField();
                string fieldName = String.Empty; 
                if (_fieldSchema != null)
                    fieldName = _fieldSchema.Name;

                if (_genericBoundField == false) { 
                    runtimeField.HeaderText = fieldName;
                    runtimeField.DataField = fieldName; 
                    runtimeField.SortExpression = fieldName; 
                }
                if (_fieldSchema != null) { 
                    if (_fieldSchema.PrimaryKey) {
                        runtimeField.ReadOnly = true;
                    }
                    if (_fieldSchema.Identity) { 
                        runtimeField.InsertVisible = false;
                    } 
                } 

                FieldItem field = new BoundFieldItem(_fieldsEditor, runtimeField); 
                field.LoadFieldInfo();

                return field;
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.ButtonNode"]/*' /> 
        /// <devdoc>
        ///   This represents a button field in the available fields tree. 
        /// </devdoc>
        private class ButtonNode : AvailableFieldNode {

            private string command; 
            private string buttonText;
            private DataControlFieldsEditor _fieldsEditor; 
 
            public ButtonNode(DataControlFieldsEditor fieldsEditor) : this(fieldsEditor, String.Empty, SR.GetString(SR.DCFEditor_Button), SR.GetString(SR.DCFEditor_Node_Button)) {
            } 

            public ButtonNode(DataControlFieldsEditor fieldsEditor, string command, string buttonText, string text) : base(text, DataControlFieldsEditor.ILI_BUTTON) {
                _fieldsEditor = fieldsEditor;
                this.command = command; 
                this.buttonText = buttonText;
            } 
 
            public override FieldItem CreateField() {
                ButtonField runtimeField = new ButtonField(); 
                runtimeField.Text = buttonText;
                runtimeField.CommandName = command;

                FieldItem field = new ButtonFieldItem(_fieldsEditor, runtimeField); 
                field.LoadFieldInfo();
 
                return field; 
            }
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CheckBoxNode"]/*' />
        /// <devdoc>
        ///   This represents a CheckBox field in the available fields tree. 
        /// </devdoc>
        private class CheckBoxNode : AvailableFieldNode { 
 
            protected IDataSourceFieldSchema _fieldSchema;
            private bool _genericCheckBoxField; 
            private DataControlFieldsEditor _fieldsEditor;

            public CheckBoxNode(DataControlFieldsEditor fieldsEditor, IDataSourceFieldSchema fieldSchema) : base(fieldSchema == null ? String.Empty : fieldSchema.Name, DataControlFieldsEditor.ILI_CHECKBOX) {
                _fieldsEditor = fieldsEditor; 
                this._fieldSchema = fieldSchema;
                if (fieldSchema == null) { 
                    _genericCheckBoxField = true; 
                    Text = SR.GetString(SR.DCFEditor_Node_CheckBox);
                }} 

            public override FieldItem CreateField() {
                CheckBoxField runtimeField = new CheckBoxField();
                string fieldName = String.Empty; 
                if (_fieldSchema != null) {
                    fieldName = _fieldSchema.Name; 
                } 

                if (_genericCheckBoxField == false) { 
                    runtimeField.HeaderText = fieldName;
                    runtimeField.DataField = fieldName;
                    runtimeField.SortExpression = fieldName;
                } 
                if (_fieldSchema != null) {
                    if (_fieldSchema.PrimaryKey) { 
                        runtimeField.ReadOnly = true; 
                    }
                    if (_fieldSchema.Identity) { 
                        runtimeField.InsertVisible = false;
                    }
                }
 
                FieldItem field = new CheckBoxFieldItem(_fieldsEditor, runtimeField);
                field.LoadFieldInfo(); 
 
                return field;
            } 
        }

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.ImageNode"]/*' />
        /// <devdoc> 
        ///   This represents a Image field in the available fields tree.
        /// </devdoc> 
        private class ImageNode : AvailableFieldNode { 

            private DataControlFieldsEditor _fieldsEditor; 

            public ImageNode(DataControlFieldsEditor fieldsEditor) : base(String.Empty, DataControlFieldsEditor.ILI_IMAGE) {
                _fieldsEditor = fieldsEditor;
                Text = SR.GetString(SR.DCFEditor_Node_Image); 
            }
 
            public override FieldItem CreateField() { 
                ImageField runtimeField = new ImageField();
 
                FieldItem field = new ImageFieldItem(_fieldsEditor, runtimeField);
                field.LoadFieldInfo();

                return field; 
            }
        } 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CommandNode"]/*' />
        /// <devdoc> 
        /// </devdoc>
        private class CommandNode : AvailableFieldNode {

            private int commandType; 
            private DataControlFieldsEditor _fieldsEditor;
 
            public CommandNode(DataControlFieldsEditor fieldsEditor) : this(fieldsEditor, -1, SR.GetString(SR.DCFEditor_Node_Command), DataControlFieldsEditor.ILI_COMMAND) { 
            }
 
            public CommandNode(DataControlFieldsEditor fieldsEditor, int commandType, string text, int icon) : base(text, icon) {
                this.commandType = commandType;
                _fieldsEditor = fieldsEditor;
            } 

            public override FieldItem CreateField() { 
                CommandField runtimeField = new CommandField(); 

                switch (commandType) { 
                    case CF_EDIT:
                        runtimeField.ShowEditButton = true;
                        break;
                    case CF_SELECT: 
                        runtimeField.ShowSelectButton = true;
                        break; 
                    case CF_DELETE: 
                        runtimeField.ShowDeleteButton = true;
                        break; 
                    case CF_INSERT:
                        runtimeField.ShowInsertButton = true;
                        break;
                    default: 
                        break;
                } 
 

                FieldItem field = new CommandFieldItem(_fieldsEditor, runtimeField); 
                field.LoadFieldInfo();

                return field;
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.HyperLinkNode"]/*' /> 
        /// <devdoc>
        ///   This represents a HyperLink field in the available fields tree. 
        /// </devdoc>
        private class HyperLinkNode : AvailableFieldNode {
            private string hyperLinkText;
            private DataControlFieldsEditor _fieldsEditor; 

            public HyperLinkNode(DataControlFieldsEditor fieldsEditor) : this(fieldsEditor, SR.GetString(SR.DCFEditor_HyperLink)) { 
            } 

            public HyperLinkNode(DataControlFieldsEditor fieldsEditor, string hyperLinkText) : base(SR.GetString(SR.DCFEditor_Node_HyperLink), DataControlFieldsEditor.ILI_HYPERLINK) { 
                _fieldsEditor = fieldsEditor;
                this.hyperLinkText = hyperLinkText;
            }
 
            public override FieldItem CreateField() {
                HyperLinkField runtimeField = new HyperLinkField(); 
 
                FieldItem field = new HyperLinkFieldItem(_fieldsEditor, runtimeField);
                field.Text = hyperLinkText; 
                field.LoadFieldInfo();

                return field;
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.TemplateNode"]/*' /> 
        /// <devdoc>
        ///   This represents a template field in the available fields tree. 
        /// </devdoc>
        private class TemplateNode : AvailableFieldNode {
            private DataControlFieldsEditor _fieldsEditor;
 
            public TemplateNode(DataControlFieldsEditor fieldsEditor) : base(SR.GetString(SR.DCFEditor_Node_Template), DataControlFieldsEditor.ILI_TEMPLATE) {
                _fieldsEditor = fieldsEditor; 
            } 

            public override FieldItem CreateField() { 
                TemplateField runtimeField = new TemplateField();

                FieldItem field = new TemplateFieldItem(_fieldsEditor, runtimeField);
                field.LoadFieldInfo(); 

                return field; 
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.FieldItem"]/*' />
        /// <devdoc>
        ///   Represents a field in the fields collection of the DataGrid.
        /// </devdoc> 
        private abstract class FieldItem : ListViewItem {
            protected DataControlField runtimeField; 
            protected DataControlFieldsEditor fieldsEditor; 

            public FieldItem(DataControlFieldsEditor fieldsEditor, DataControlField runtimeField, int image) : base(String.Empty, image) { 
                this.fieldsEditor = fieldsEditor;
                this.runtimeField = runtimeField;
                this.Text = GetNodeText(null);
            } 

            public string HeaderText { 
                get { 
                    return runtimeField.HeaderText;
                } 
                set {
                    runtimeField.HeaderText = value;
                    UpdateDisplayText();
                } 
            }
 
            public DataControlField RuntimeField { 
                get {
                    return runtimeField; 
                }
            }

            protected virtual string GetDefaultNodeText() { 
                return runtimeField.GetType().Name;
            } 
 
            public virtual string GetNodeText(string headerText) {
                if ((headerText == null) || (headerText.Length == 0)) { 
                    return GetDefaultNodeText();
                }
                else {
                    return headerText; 
                }
            } 
 
            protected ITemplate GetTemplate(DataBoundControl control, string templateContent) {
                try { 
                    ISite site = control.Site;
                    Debug.Assert(site != null);

                    IDesignerHost designerHost = (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
                    if (templateContent != null && templateContent.Length > 0) {
                        return ControlParser.ParseTemplate(designerHost, templateContent, null); 
                    } 
                    return null;
                } catch (Exception e) { 
                    Debug.Fail(e.ToString());
                    return null;
                }
            } 

            public virtual TemplateField GetTemplateField(DataBoundControl dataBoundControl) { 
                TemplateField field = new TemplateField(); 

                field.HeaderText = runtimeField.HeaderText; 
                field.HeaderImageUrl = runtimeField.HeaderImageUrl;
                field.AccessibleHeaderText = runtimeField.AccessibleHeaderText;
                field.FooterText = runtimeField.FooterText;
 
                field.SortExpression = runtimeField.SortExpression;
                field.Visible = runtimeField.Visible; 
                field.InsertVisible = runtimeField.InsertVisible; 
                field.ShowHeader = runtimeField.ShowHeader;
 
                field.ControlStyle.CopyFrom(runtimeField.ControlStyle);
                field.FooterStyle.CopyFrom(runtimeField.FooterStyle);
                field.HeaderStyle.CopyFrom(runtimeField.HeaderStyle);
                field.ItemStyle.CopyFrom(runtimeField.ItemStyle); 

                return field; 
            } 

            public virtual void LoadFieldInfo() { 
                UpdateDisplayText();
            }

            protected string PrepareFormatString(string formatString) { 
                // replace a single quote character with its escaped version
                return formatString.Replace("'", "&#039;"); 
            } 

            protected void UpdateDisplayText() { 
                this.Text = GetNodeText(HeaderText);
            }
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.BoundFieldItem"]/*' />
        /// <devdoc> 
        ///    Represents a field bound to a datafield. 
        /// </devdoc>
        private class BoundFieldItem : FieldItem { 

            public BoundFieldItem(DataControlFieldsEditor fieldsEditor, BoundField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_BOUND) {
            }
 
            protected override string GetDefaultNodeText() {
                string dataField = ((BoundField)RuntimeField).DataField; 
                if ((dataField != null) && (dataField.Length != 0)) { 
                    return dataField;
                } 
                return SR.GetString(SR.DCFEditor_Node_Bound);
            }

            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) { 
                TemplateField field = base.GetTemplateField(dataBoundControl);
 
                field.SortExpression = RuntimeField.SortExpression; 
                field.ItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_READONLY, false));
                field.ConvertEmptyStringToNull = ((BoundField)RuntimeField).ConvertEmptyStringToNull; 
                field.EditItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_EDIT, ((BoundField)RuntimeField).ReadOnly));
                if (dataBoundControl is DetailsView && ((BoundField)RuntimeField).InsertVisible) {
                    field.InsertItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_INSERT, false));
                } 

                return field; 
            } 

            private string GetTemplateContent(int editMode, bool readOnly) { 
                StringBuilder sb = new StringBuilder();
                bool useReadOnlyEditTemplate = editMode == MODE_EDIT && readOnly;
                Type controlType = ((editMode == MODE_READONLY || useReadOnlyEditTemplate) ? typeof(System.Web.UI.WebControls.Label) : typeof(System.Web.UI.WebControls.TextBox));
 
                string dataFormatString = ((BoundField)RuntimeField).DataFormatString;
                string dataField = ((BoundField)this.RuntimeField).DataField; 
                string bindDataFormatString = String.Empty; 

                if ((editMode != MODE_EDIT || ((BoundField)this.RuntimeField).ApplyFormatInEditMode) || 
                    useReadOnlyEditTemplate) {
                    bindDataFormatString = PrepareFormatString(dataFormatString);
                }
 
                string bindString = (useReadOnlyEditTemplate) ?
                    DesignTimeDataBinding.CreateEvalExpression(dataField, bindDataFormatString) : 
                    DesignTimeDataBinding.CreateBindExpression(dataField, bindDataFormatString); 

                if (editMode == MODE_INSERT && !((BoundField)this.RuntimeField).InsertVisible) { 
                    return String.Empty;
                }

                sb.Append("<asp:"); 
                sb.Append(controlType.Name);
                sb.Append(" runat=\"server\""); 
 
                if (dataField.Length != 0) {
                    sb.Append(" Text='<%# "); 
                    sb.Append(bindString);
                    sb.Append(" %>'");
                }
 

                sb.Append(" id=\""); 
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, editMode)); 
                sb.Append("\"></asp:");
                sb.Append(controlType.Name); 
                sb.Append(">");

                return sb.ToString();
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.ButtonFieldItem"]/*' /> 
        /// <devdoc>
        ///   Represents a field containing a button. 
        /// </devdoc>
        private class ButtonFieldItem : FieldItem {
            public ButtonFieldItem(DataControlFieldsEditor fieldsEditor, ButtonField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_BUTTON) {
            } 

            protected override string GetDefaultNodeText() { 
                string buttonText = ((ButtonField)runtimeField).Text; 

                if ((buttonText != null) && (buttonText.Length != 0)) { 
                    return buttonText;
                }
                return SR.GetString(SR.DCFEditor_Node_Button);
            } 

            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) { 
                TemplateField field = base.GetTemplateField(dataBoundControl); 
                ButtonField runtimeField = (ButtonField)RuntimeField;
 
                StringBuilder sb = new StringBuilder();
                Type controlType = typeof(System.Web.UI.WebControls.Button);
                if (runtimeField.ButtonType == ButtonType.Link) {
                    controlType = typeof(System.Web.UI.WebControls.LinkButton); 
                }
                else if (runtimeField.ButtonType == ButtonType.Image) { 
                    controlType = typeof(System.Web.UI.WebControls.ImageButton); 
                }
 
                sb.Append("<asp:");
                sb.Append(controlType.Name);
                sb.Append(" runat=\"server\"");
 
                if (runtimeField.DataTextField.Length != 0) {
                    sb.Append(" Text='<%# "); 
                    sb.Append(DesignTimeDataBinding.CreateEvalExpression(runtimeField.DataTextField, PrepareFormatString(runtimeField.DataTextFormatString))); 
                    sb.Append(" %>'");
                } 
                else {
                    sb.Append(" Text=\"");
                    sb.Append(runtimeField.Text);
                    sb.Append("\""); 
                }
                sb.Append(" CommandName=\""); 
                sb.Append(runtimeField.CommandName); 
                sb.Append("\"");
 
                if (runtimeField.ButtonType == ButtonType.Image && runtimeField.ImageUrl.Length > 0) {
                    sb.Append(" ImageUrl=\"");
                    sb.Append(runtimeField.ImageUrl);
                    sb.Append("\""); 
                }
 
                sb.Append(" CausesValidation=\"false\" id=\""); 
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, MODE_READONLY));
                sb.Append("\"></asp:"); 
                sb.Append(controlType.Name);
                sb.Append(">");

                field.ItemTemplate = GetTemplate(dataBoundControl, sb.ToString()); 

                return field; 
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CheckBoxFieldItem"]/*' />
        /// <devdoc>
        ///    Represents a field bound to a datafield.
        /// </devdoc> 
        private class CheckBoxFieldItem : FieldItem {
            public CheckBoxFieldItem(DataControlFieldsEditor fieldsEditor, CheckBoxField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_CHECKBOX) { 
            } 

            protected override string GetDefaultNodeText() { 
                string dataField = ((CheckBoxField)RuntimeField).DataField;
                if ((dataField != null) && (dataField.Length != 0)) {
                    return dataField;
                } 
                return SR.GetString(SR.DCFEditor_Node_CheckBox);
            } 
 
            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) {
                TemplateField newField = base.GetTemplateField(dataBoundControl); 
                CheckBoxField field = (CheckBoxField)this.RuntimeField;

                newField.SortExpression = field.SortExpression;
                newField.ItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_READONLY)); 
                if (field.ReadOnly == false) {
                    newField.EditItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_EDIT)); 
                } 
                if (dataBoundControl is DetailsView && ((CheckBoxField)RuntimeField).InsertVisible) {
                    newField.InsertItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_INSERT)); 
                }

                return newField;
            } 

            private string GetTemplateContent(int editMode) { 
                StringBuilder sb = new StringBuilder(); 
                Type controlType = typeof(System.Web.UI.WebControls.CheckBox);
 
                if (editMode == MODE_INSERT && !((CheckBoxField)this.RuntimeField).InsertVisible) {
                    return String.Empty;
                }
 
                sb.Append("<asp:");
                sb.Append(controlType.Name); 
                sb.Append(" runat=\"server\""); 

                string dataField = ((CheckBoxField)this.RuntimeField).DataField; 
                if (dataField.Length != 0) {
                    sb.Append(" Checked='<%# ");
                    sb.Append(DesignTimeDataBinding.CreateBindExpression(dataField, String.Empty));
                    sb.Append(" %>'"); 
                    if (editMode == MODE_READONLY) {
                        sb.Append(" Enabled=\"false\""); 
                    } 
                }
 
                sb.Append(" id=\"");
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, editMode));
                sb.Append("\"></asp:");
                sb.Append(controlType.Name); 
                sb.Append(">");
 
                return sb.ToString(); 
            }
        } 

        /// <devdoc>
        ///    Represents a field bound to a datafield.
        /// </devdoc> 
        private class ImageFieldItem : FieldItem {
            public ImageFieldItem(DataControlFieldsEditor fieldsEditor, ImageField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_IMAGE) { 
            } 

            protected override string GetDefaultNodeText() { 
                string dataField = ((ImageField)RuntimeField).DataImageUrlField;
                if ((dataField != null) && (dataField.Length != 0)) {
                    return dataField;
                } 
                return SR.GetString(SR.DCFEditor_Node_Image);
            } 
 
            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) {
                TemplateField newField = base.GetTemplateField(dataBoundControl); 
                ImageField field = (ImageField)this.RuntimeField;

                newField.SortExpression = field.SortExpression;
                newField.ItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_READONLY)); 
                newField.ConvertEmptyStringToNull = field.ConvertEmptyStringToNull;
                if (field.ReadOnly == false) { 
                    newField.EditItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_EDIT)); 
                    if (dataBoundControl is DetailsView) {
                        newField.InsertItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_INSERT)); 
                    }
                }

                return newField; 
            }
 
            private string GetTemplateContent(int editMode) { 
                StringBuilder sb = new StringBuilder();
                Type controlType; 
                string imageUrlField = ((ImageField)this.RuntimeField).DataImageUrlField;
                string altTextValue;

                string altTextField = ((ImageField)this.runtimeField).DataAlternateTextField; 
                if (altTextField.Length > 0) {
                    string altTextFieldFormat = ((ImageField)this.runtimeField).DataAlternateTextFormatString; 
                    altTextValue = "'<%# " + DesignTimeDataBinding.CreateEvalExpression(altTextField, PrepareFormatString(altTextFieldFormat)) + " %>'"; 
                }
                else { 
                    altTextValue = ((ImageField)this.runtimeField).AlternateText;
                }

                if (editMode == MODE_READONLY) { 
                    controlType = typeof(System.Web.UI.WebControls.Image);
                } 
                else { 
                    controlType = typeof(System.Web.UI.WebControls.TextBox);
                } 


                sb.Append("<asp:");
                sb.Append(controlType.Name); 
                sb.Append(" runat=\"server\"");
 
                if (imageUrlField.Length > 0) { 
                    if (controlType == typeof(System.Web.UI.WebControls.Image)) {
                        sb.Append(" ImageUrl='<%# "); 
                        sb.Append(DesignTimeDataBinding.CreateEvalExpression(imageUrlField, PrepareFormatString(((ImageField)this.runtimeField).DataImageUrlFormatString)));
                    }
                    else if (controlType == typeof(System.Web.UI.WebControls.TextBox)) {
                        sb.Append(" Text='<%# "); 
                        sb.Append(DesignTimeDataBinding.CreateEvalExpression(imageUrlField, String.Empty));
                    } 
                    sb.Append(" %>' "); 
                }
 
                if (altTextValue.Length > 0) {
                    if (controlType == typeof(System.Web.UI.WebControls.TextBox)) {
                        sb.Append(" Tooltip=");
                    } 
                    else {
                        sb.Append(" AlternateText="); 
                    } 
                    sb.Append(altTextValue);
                } 

                sb.Append(" id=\"");
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, editMode));
                sb.Append("\"></asp:"); 
                sb.Append(controlType.Name);
                sb.Append(">"); 
 
                return sb.ToString();
            } 
        }

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.HyperLinkFieldItem"]/*' />
        /// <devdoc> 
        ///   Represents a field containing a hyperlink.
        /// </devdoc> 
        private class HyperLinkFieldItem : FieldItem { 
            public HyperLinkFieldItem(DataControlFieldsEditor fieldsEditor, HyperLinkField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_HYPERLINK) {
            } 

            protected override string GetDefaultNodeText() {
                string anchorText = ((HyperLinkField)RuntimeField).Text;
                if ((anchorText != null) && (anchorText.Length != 0)) { 
                    return anchorText;
                } 
                return SR.GetString(SR.DCFEditor_Node_HyperLink); 
            }
 
            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) {
                TemplateField newField = base.GetTemplateField(dataBoundControl);
                HyperLinkField field = (HyperLinkField)RuntimeField;
                Type controlType = typeof(System.Web.UI.WebControls.HyperLink); 

                StringBuilder sb = new StringBuilder(); 
 
                sb.Append("<asp:");
                sb.Append(controlType.Name); 
                sb.Append(" runat=\"server\"");

                if (field.DataTextField.Length != 0) {
                    sb.Append(" Text='<%# "); 
                    sb.Append(DesignTimeDataBinding.CreateEvalExpression(field.DataTextField, PrepareFormatString(field.DataTextFormatString)));
                    sb.Append(" %>'"); 
                } 
                else {
                    sb.Append(" Text=\""); 
                    sb.Append(field.Text);
                    sb.Append("\"");
                }
                if (field.DataNavigateUrlFields.Length != 0 && field.DataNavigateUrlFields[0].Length > 0) { 
                    sb.Append(" NavigateUrl='<%# ");
                    sb.Append(DesignTimeDataBinding.CreateEvalExpression(field.DataNavigateUrlFields[0], PrepareFormatString(field.DataNavigateUrlFormatString))); 
                    sb.Append(" %>'"); 
                }
                else { 
                    sb.Append(" NavigateUrl=\"");
                    sb.Append(field.NavigateUrl);
                    sb.Append("\"");
                } 
                if (field.Target.Length != 0) {
                    sb.Append(" Target=\""); 
                    sb.Append(field.Target); 
                    sb.Append("\"");
                } 

                sb.Append(" id=\"");
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, MODE_READONLY));
                sb.Append("\"></asp:"); 
                sb.Append(controlType.Name);
                sb.Append(">"); 
 
                newField.ItemTemplate = GetTemplate(dataBoundControl, sb.ToString());
 
                return newField;
            }
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.TemplateFieldItem"]/*' />
        /// <devdoc> 
        ///   Represents a field containing a template. 
        /// </devdoc>
        private class TemplateFieldItem : FieldItem { 

            public TemplateFieldItem(DataControlFieldsEditor fieldsEditor, TemplateField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_TEMPLATE) {
            }
 
            protected override string GetDefaultNodeText() {
                return SR.GetString(SR.DCFEditor_Node_Template); 
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CommandFieldItem"]/*' />
        /// <devdoc>
        ///   Represents a CommandField
        /// </devdoc> 
        private class CommandFieldItem : FieldItem {
 
            public CommandFieldItem(DataControlFieldsEditor fieldsEditor, CommandField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_COMMAND) { 
                UpdateImageIndex();
            } 

            private string BuildButtonString(Type controlType, string buttonText, string commandName, string imageUrl, bool causesValidation, int mode, ref int buttonNameStartIndex) {
                StringBuilder sb = new StringBuilder();
                sb.Append("<asp:"); 
                sb.Append(controlType.Name);
                sb.Append(" runat=\"server\""); 
                sb.Append(" Text=\""); 
                sb.Append(buttonText);
                sb.Append("\""); 
                sb.Append(" CommandName=\"");
                sb.Append(commandName);
                if (imageUrl != null && imageUrl.Length > 0) {
                    sb.Append("\" ImageUrl=\""); 
                    sb.Append(imageUrl);
                } 
                sb.Append("\" CausesValidation=\""); 
                sb.Append(causesValidation.ToString());
                sb.Append("\" id=\""); 
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, mode, ref buttonNameStartIndex));
                sb.Append("\"></asp:");
                sb.Append(controlType.Name);
                sb.Append(">"); 
                return sb.ToString();
            } 
 
            protected override string GetDefaultNodeText() {
                CommandField field = (CommandField)RuntimeField; 
                if (field.ShowEditButton && !field.ShowDeleteButton && !field.ShowSelectButton && !field.ShowInsertButton) {
                    return SR.GetString(SR.DCFEditor_Node_Edit);
                }
                if (field.ShowDeleteButton && !field.ShowEditButton && !field.ShowSelectButton && !field.ShowInsertButton) { 
                    return SR.GetString(SR.DCFEditor_Node_Delete);
                } 
                if (field.ShowSelectButton && !field.ShowDeleteButton && !field.ShowEditButton && !field.ShowInsertButton) { 
                    return SR.GetString(SR.DCFEditor_Node_Select);
                } 
                if (field.ShowInsertButton && !field.ShowDeleteButton && !field.ShowSelectButton && !field.ShowEditButton) {
                    return SR.GetString(SR.DCFEditor_Node_Insert);
                }
                return SR.GetString(SR.DCFEditor_Node_Command); 
            }
 
            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) { 
                TemplateField field = base.GetTemplateField(dataBoundControl);
 
                field.ItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_READONLY));
                field.EditItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_EDIT));
                if (dataBoundControl is DetailsView) {
                    field.InsertItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_INSERT)); 
                }
 
                return field; 
            }
 
            private string GetTemplateContent(int editMode) {
                StringBuilder sb = new StringBuilder();
                CommandField field = (CommandField)RuntimeField;
                Type controlType = typeof(System.Web.UI.WebControls.Button); 
                int buttonNameStartIndex = 1;
 
                if (field.ButtonType == ButtonType.Link) { 
                    controlType = typeof(System.Web.UI.WebControls.LinkButton);
                } 
                else if (field.ButtonType == ButtonType.Image) {
                    controlType = typeof(System.Web.UI.WebControls.ImageButton);
                }
 
                switch(editMode) {
                    case MODE_EDIT: 
                        if (field.ShowEditButton) { 
                            string updateImageButton = field.ButtonType == ButtonType.Image ? field.UpdateImageUrl : null;
                            sb.Append(BuildButtonString(controlType, field.UpdateText, "Update", updateImageButton, true, MODE_EDIT, ref buttonNameStartIndex)); 
                            buttonNameStartIndex++;
                            if (field.ShowCancelButton) {
                                sb.Append("&nbsp;");
                                string cancelImageButton = field.ButtonType == ButtonType.Image ? field.CancelImageUrl : null; 
                                sb.Append(BuildButtonString(controlType, field.CancelText, "Cancel", cancelImageButton, false, MODE_EDIT, ref buttonNameStartIndex));
                                buttonNameStartIndex++; 
                            } 
                        }
                        break; 
                    case MODE_READONLY:
                        bool isFirstButton = true;
                        if (field.ShowEditButton) {
                            string editImageButton = field.ButtonType == ButtonType.Image ? field.EditImageUrl : null; 
                            sb.Append(BuildButtonString(controlType, field.EditText, "Edit", editImageButton, false, MODE_READONLY, ref buttonNameStartIndex));
                            buttonNameStartIndex++; 
                            isFirstButton = false; 
                        }
                        if (field.ShowInsertButton) { 
                            if (!isFirstButton) {
                                sb.Append("&nbsp;");
                            }
                            string newImageButton = field.ButtonType == ButtonType.Image ? field.NewImageUrl : null; 
                            sb.Append(BuildButtonString(controlType, field.NewText, "New", newImageButton, false, MODE_READONLY, ref buttonNameStartIndex));
                            buttonNameStartIndex++; 
                        } 
                        if (field.ShowSelectButton) {
                            if (!isFirstButton) { 
                                sb.Append("&nbsp;");
                            }
                            string selectImageButton = field.ButtonType == ButtonType.Image ? field.SelectImageUrl : null;
                            sb.Append(BuildButtonString(controlType, field.SelectText, "Select", selectImageButton, false, MODE_READONLY, ref buttonNameStartIndex)); 
                            buttonNameStartIndex++;
                        } 
                        if (field.ShowDeleteButton) { 
                            if (!isFirstButton) {
                                sb.Append("&nbsp;"); 
                            }
                            string deleteImageButton = field.ButtonType == ButtonType.Image ? field.DeleteImageUrl : null;
                            sb.Append(BuildButtonString(controlType, field.DeleteText, "Delete", deleteImageButton, false, MODE_READONLY, ref buttonNameStartIndex));
                            buttonNameStartIndex++; 
                        }
                        break; 
                    case MODE_INSERT: 
                        if (field.ShowInsertButton) {
                            string insertImageButton = field.ButtonType == ButtonType.Image ? field.InsertImageUrl : null; 
                            sb.Append(BuildButtonString(controlType, field.InsertText, "Insert", insertImageButton, true, MODE_INSERT, ref buttonNameStartIndex));
                            buttonNameStartIndex++;
                            if (field.ShowCancelButton) {
                                sb.Append("&nbsp;"); 
                                string cancelImageButton = field.ButtonType == ButtonType.Image ? field.CancelImageUrl : null;
                                sb.Append(BuildButtonString(controlType, field.CancelText, "Cancel", cancelImageButton, false, MODE_INSERT, ref buttonNameStartIndex)); 
                                buttonNameStartIndex++; 
                            }
                        } 
                        break;
                }
                return sb.ToString();
            } 

            public void UpdateImageIndex() { 
                CommandField runtimeField = (CommandField)RuntimeField; 

                if (runtimeField.ShowEditButton && !runtimeField.ShowDeleteButton && !runtimeField.ShowSelectButton && !runtimeField.ShowInsertButton) { 
                    ImageIndex = ILI_EDITBUTTON;
                }
                else if (runtimeField.ShowDeleteButton && !runtimeField.ShowEditButton && !runtimeField.ShowSelectButton && !runtimeField.ShowInsertButton) {
                    ImageIndex = ILI_DELETEBUTTON; 
                }
                else if (runtimeField.ShowSelectButton && !runtimeField.ShowDeleteButton && !runtimeField.ShowEditButton && !runtimeField.ShowInsertButton) { 
                    ImageIndex = ILI_SELECTBUTTON; 
                }
                else if (runtimeField.ShowInsertButton && !runtimeField.ShowDeleteButton && !runtimeField.ShowSelectButton && !runtimeField.ShowEditButton) { 
                    ImageIndex = ILI_INSERTBUTTON;
                }
                else {
                    ImageIndex = ILI_COMMAND; 
                }
            } 
        } 

        private class TreeViewWithEnter : TreeView { 
            protected override bool IsInputKey(Keys keyCode) {
                if (keyCode == Keys.Enter) {
                    return true;
                } 
                return base.IsInputKey(keyCode);
            } 
        } 

        private class ListViewWithEnter : ListView { 
            protected override bool IsInputKey(Keys keyCode) {
                if (keyCode == Keys.Enter) {
                    return true;
                } 
                return base.IsInputKey(keyCode);
            } 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CustomFieldItem"]/*' /> 
        /// <devdoc>
        ///   Represents a field of an unknown/custom type.
        /// </devdoc>
        private class CustomFieldItem : FieldItem { 

            public CustomFieldItem(DataControlFieldsEditor fieldsEditor, DataControlField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_CUSTOM) { 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataControlFieldsEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.CodeDom; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.IO;
    using System.Text;
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
 
    using ControlCollection = System.Web.UI.ControlCollection;
    using DataControlField = System.Web.UI.WebControls.DataControlField; 
    using DataControlFieldCollection = System.Web.UI.WebControls.DataControlFieldCollection;
    using DataBinding = System.Web.UI.DataBinding;
    using GridView = System.Web.UI.WebControls.GridView;
 
    using Button = System.Windows.Forms.Button;
    using CheckBox = System.Windows.Forms.CheckBox; 
    using Color = System.Drawing.Color; 
    using Image = System.Drawing.Image;
    using Label = System.Windows.Forms.Label; 
    using ListViewItem = System.Windows.Forms.ListViewItem;
    using Panel = System.Windows.Forms.Panel;
    using TextBox = System.Windows.Forms.TextBox;
    using TreeNode = System.Windows.Forms.TreeNode; 
    using TreeView = System.Windows.Forms.TreeView;
    using View = System.Windows.Forms.View; 
 
    /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor"]/*' />
    /// <devdoc> 
    ///   The Data page for DataBoundControls with DataControlFields
    /// </devdoc>
    /// <internalonly/>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    internal sealed class DataControlFieldsEditor : DesignerForm {
 
        private const int ILI_DATASOURCE = 0; 
        private const int ILI_BOUND = 1;
        private const int ILI_ALL = 2; 
        private const int ILI_CUSTOM = 3;
        private const int ILI_BUTTON = 4;
        private const int ILI_SELECTBUTTON = 5;
        private const int ILI_EDITBUTTON = 6; 
        private const int ILI_DELETEBUTTON = 7;
        private const int ILI_HYPERLINK = 8; 
        private const int ILI_TEMPLATE = 9; 
        private const int ILI_CHECKBOX = 10;
        private const int ILI_INSERTBUTTON = 11; 
        private const int ILI_COMMAND = 12;
        private const int ILI_BOOLDATASOURCE = 13;
        private const int ILI_IMAGE = 14;
 
        private const int CF_EDIT = 0;
        private const int CF_INSERT = 1; 
        private const int CF_SELECT = 2; 
        private const int CF_DELETE = 3;
 
        private const int MODE_READONLY = 0;
        private const int MODE_EDIT = 1;
        private const int MODE_INSERT = 2;
 
        private TreeViewWithEnter _availableFieldsTree;
        private System.Windows.Forms.Button _addFieldButton; 
        private ListViewWithEnter _selFieldsList; 
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton; 
        private System.Windows.Forms.Button _moveFieldUpButton;
        private System.Windows.Forms.Button _moveFieldDownButton;
        private System.Windows.Forms.Button _deleteFieldButton;
        private System.Windows.Forms.PropertyGrid _currentFieldProps; 
        private System.Windows.Forms.LinkLabel _refreshSchemaLink;
        private System.Windows.Forms.LinkLabel _templatizeLink; 
        private System.Windows.Forms.CheckBox _autoFieldCheck; 
        private System.Windows.Forms.Label _selFieldLabel;
        private System.Windows.Forms.Label _availableFieldsLabel; 
        private System.Windows.Forms.Label _selFieldsLabel;

        private DataSourceNode _selectedDataSourceNode;
        private BoolDataSourceNode _selectedCheckBoxDataSourceNode; 
        private FieldItem _currentFieldItem;
        private bool _propChangesPending; 
        private bool _fieldMovePending; 
        private DataControlFieldCollection _clonedFieldCollection;
 
        private DataBoundControlDesigner _controlDesigner;
        private bool _isLoading;

        private IDataSourceFieldSchema[] _fieldSchemas; 
        private IDataSourceViewSchema _viewSchema;
 
        private bool _initialActivate; 
        private bool _initialIgnoreRefreshSchemaValue;
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.DataControlFieldsEditor"]/*' />
        /// <devdoc>
        ///   Creates a new instance of DataControlFieldsEditor.
        /// </devdoc> 
        public DataControlFieldsEditor(DataBoundControlDesigner controlDesigner) : base(controlDesigner.Component.Site) {
            this._controlDesigner = controlDesigner; 
            InitializeComponent(); 
            InitForm();
            _initialActivate = true; 
            IgnoreRefreshSchemaEvents();
        }

        /// <devdoc> 
        /// Gets and sets the runtime control's AutoGenerate property for fields.
        /// </devdoc> 
        private bool AutoGenerateFields { 
            get {
                if (Control is GridView) { 
                    return ((GridView)Control).AutoGenerateColumns;
                }
                else if (Control is DetailsView) {
                    return ((DetailsView)Control).AutoGenerateRows; 
                }
                Debug.Assert(false, "The control must be either a DetailsView or a GridView"); 
                return false; 
            }
            set { 
                if (Control is GridView) {
                    ((GridView)Control).AutoGenerateColumns = value;
                }
                else if (Control is DetailsView) { 
                    ((DetailsView)Control).AutoGenerateRows = value;
                } 
                else { 
                    Debug.Assert(false, "The control must be either a DetailsView or a GridView");
                } 
            }
        }

        private DataBoundControl Control { 
            get {
                return _controlDesigner.Component as DataBoundControl; 
            } 
        }
 
        /// <devdoc>
        /// Returns the DataControlFieldCollection of the runtime control.
        /// </devdoc>
        private DataControlFieldCollection FieldCollection { 
            get {
                if (_clonedFieldCollection == null) { 
                    if (Control is GridView) { 
                        DataControlFieldCollection oldFields = ((GridView)Control).Columns;
                        _clonedFieldCollection = oldFields.CloneFields(); 
                        for (int i = 0; i < oldFields.Count; i++) {
                            _controlDesigner.RegisterClone(oldFields[i], _clonedFieldCollection[i]);
                        }
                    } 
                    else if (Control is DetailsView) {
                        DataControlFieldCollection oldFields = ((DetailsView)Control).Fields; 
                        _clonedFieldCollection = oldFields.CloneFields(); 
                        for (int i = 0; i < oldFields.Count; i++) {
                            _controlDesigner.RegisterClone(oldFields[i], _clonedFieldCollection[i]); 
                        }
                    }
                    else {
                        Debug.Assert(false, "The control must be either a DetailsView or a GridView"); 
                    }
                } 
                return _clonedFieldCollection; 
            }
        } 

        protected override string HelpTopic {
            get {
                return "net.Asp.DataControlField.DataControlFieldEditor"; 
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
 
        private void EnterLoadingMode() {
            _isLoading = true;
        }
 
        private void ExitLoadingMode() {
            _isLoading = false; 
        } 

        // create a unique id for controls generated by this dialog 
        private string GetNewDataSourceName(Type controlType, int editMode) {
            int buttonNameStartIndex = 1;
            return GetNewDataSourceName(controlType, editMode, ref buttonNameStartIndex);
        } 

        // create a unique id for controls generated by this dialog 
        private string GetNewDataSourceName(Type controlType, int editMode, ref int startIndex) { 
            int currentIndex = startIndex;
 
            DataControlFieldCollection fields = new DataControlFieldCollection();
            int fieldCount = _selFieldsList.Items.Count;

            // create shallow copy of Fields collection 
            for (int i = 0; i < fieldCount; i++) {
                FieldItem fieldItem = (FieldItem)_selFieldsList.Items[i]; 
                fields.Add(fieldItem.RuntimeField); 
            }
 
            if (fields != null && fields.Count > 0) {
                bool foundFreeIndex = false;
                while (!foundFreeIndex) {
                    for (int i = 0; i < fields.Count; i++) { 
                        DataControlField field = fields[i];
                        if (field is TemplateField) { 
                            ITemplate template = null; 
                            switch (editMode) {
                                case MODE_READONLY: 
                                    template = ((TemplateField)field).ItemTemplate;
                                    break;
                                case MODE_EDIT:
                                    template = ((TemplateField)field).EditItemTemplate; 
                                    break;
                                case MODE_INSERT: 
                                    template = ((TemplateField)field).InsertItemTemplate; 
                                    break;
                            } 
                            if (template != null) {
                                IDesignerHost designerHost = (IDesignerHost)Control.Site.GetService(typeof(IDesignerHost));
                                string templateContents = ControlSerializer.SerializeTemplate(template, designerHost);
                                if (templateContents.Contains(controlType.Name + currentIndex.ToString(NumberFormatInfo.InvariantInfo))) { 
                                    currentIndex++;
                                    break; 
                                } 
                            }
                        } 
                        if (i == (fields.Count - 1)) {
                            foundFreeIndex = true;
                        }
                    } 
                }
            } 
            startIndex = currentIndex; 
            return controlType.Name + currentIndex.ToString(NumberFormatInfo.InvariantInfo);
        } 

        /// <devdoc>
        /// Returns an the IDataSourceViewSchema of the associated DataSource
        /// </devdoc> 
        private IDataSourceViewSchema GetViewSchema() {
            if (_viewSchema == null) { 
                if (_controlDesigner != null) { 
                    DesignerDataSourceView view = _controlDesigner.DesignerView;
                    if (view != null) { 
                        try {
                            _viewSchema = view.Schema;
                        }
                        catch (Exception ex) { 
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)ServiceProvider.GetService(typeof(IComponentDesignerDebugService));
                            if (debugService != null) { 
                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message)); 
                            }
                        } 
                    }
                }
            }
 
            return _viewSchema;
        } 
 
        /// <devdoc>
        /// Returns an array of IDataSourceFieldSchema objects for the control being edited, paying attention 
        /// to DataMember if there is one.
        /// </devdoc>
        private IDataSourceFieldSchema[] GetFieldSchemas() {
            if (_fieldSchemas == null) { 
                IDataSourceViewSchema viewSchema = GetViewSchema();
                if (viewSchema != null) { 
                    _fieldSchemas = viewSchema.GetFields(); 
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

        #region Windows Form Designer generated code 
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent() { 
            this._availableFieldsTree = new TreeViewWithEnter ();
            this._selFieldsList = new ListViewWithEnter ();
            this._okButton = new System.Windows.Forms.Button ();
            this._cancelButton = new System.Windows.Forms.Button (); 
            this._moveFieldUpButton = new System.Windows.Forms.Button ();
            this._moveFieldDownButton = new System.Windows.Forms.Button (); 
            this._addFieldButton = new System.Windows.Forms.Button (); 
            this._deleteFieldButton = new System.Windows.Forms.Button ();
            this._currentFieldProps = new System.Windows.Forms.Design.VsPropertyGrid (ServiceProvider); 
            this._autoFieldCheck = new System.Windows.Forms.CheckBox ();
            this._refreshSchemaLink = new System.Windows.Forms.LinkLabel ();
            this._templatizeLink = new System.Windows.Forms.LinkLabel ();
            this._selFieldLabel = new System.Windows.Forms.Label (); 
            this._availableFieldsLabel = new System.Windows.Forms.Label ();
            this._selFieldsLabel = new System.Windows.Forms.Label (); 
            this.SuspendLayout (); 

            // 
            // _availableFieldsTree
            //
            this._availableFieldsTree.HideSelection = false;
            this._availableFieldsTree.ImageIndex = -1; 
            this._availableFieldsTree.Indent = 15;
            this._availableFieldsTree.Location = new System.Drawing.Point (12, 28); 
            this._availableFieldsTree.Name = "_availableFieldsTree"; 
            this._availableFieldsTree.SelectedImageIndex = -1;
            this._availableFieldsTree.Size = new System.Drawing.Size (196, 116); 
            this._availableFieldsTree.TabIndex = 1;
            this._availableFieldsTree.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler (this.OnAvailableFieldsDoubleClick);
            this._availableFieldsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler (this.OnSelChangedAvailableFields);
            this._availableFieldsTree.GotFocus += new System.EventHandler (this.OnAvailableFieldsGotFocus); 
            this._availableFieldsTree.KeyPress += new KeyPressEventHandler(this.OnAvailableFieldsKeyPress);
 
            // 
            // _selFieldsList
            // 
            this._selFieldsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._selFieldsList.HideSelection = false;
            this._selFieldsList.LabelWrap = false;
            this._selFieldsList.Location = new System.Drawing.Point (12, 197); 
            this._selFieldsList.MultiSelect = false;
            this._selFieldsList.Name = "_selFieldsList"; 
            this._selFieldsList.Size = new System.Drawing.Size (164, 112); 
            this._selFieldsList.TabIndex = 4;
            this._selFieldsList.View = System.Windows.Forms.View.Details; 
            this._selFieldsList.KeyDown += new System.Windows.Forms.KeyEventHandler (this.OnSelFieldsListKeyDown);
            this._selFieldsList.SelectedIndexChanged += new System.EventHandler (this.OnSelIndexChangedSelFieldsList);
            this._selFieldsList.ItemActivate += new System.EventHandler (this.OnClickDeleteField);
            this._selFieldsList.GotFocus += new System.EventHandler (this.OnSelFieldsListGotFocus); 

            // 
            // _okButton 
            //
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point (340, 350);
            this._okButton.Name = "_okButton"; 
            this._okButton.TabIndex = 100;
            this._okButton.Click += new System.EventHandler (this.OnClickOK); 
 
            //
            // _cancelButton 
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._cancelButton.Location = new System.Drawing.Point (420, 350);
            this._cancelButton.Name = "_cancelButton"; 
            this._cancelButton.TabIndex = 101; 

            // 
            // _moveFieldUpButton
            //
            this._moveFieldUpButton.Location = new System.Drawing.Point (186, 197);
            this._moveFieldUpButton.Name = "_moveFieldUpButton"; 
            this._moveFieldUpButton.Size = new System.Drawing.Size (26, 23);
            this._moveFieldUpButton.TabIndex = 5; 
            this._moveFieldUpButton.Click += new System.EventHandler (this.OnClickMoveFieldUp); 

            // 
            // _moveFieldDownButton
            //
            this._moveFieldDownButton.Location = new System.Drawing.Point (186, 221);
            this._moveFieldDownButton.Name = "_moveFieldDownButton"; 
            this._moveFieldDownButton.Size = new System.Drawing.Size (26, 23);
            this._moveFieldDownButton.TabIndex = 6; 
            this._moveFieldDownButton.Click += new System.EventHandler (this.OnClickMoveFieldDown); 

            // 
            // _addFieldButton
            //
            this._addFieldButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._addFieldButton.Location = new System.Drawing.Point (123, 150); 
            this._addFieldButton.Name = "_addFieldButton";
            this._addFieldButton.Size = new System.Drawing.Size (85, 23); 
            this._addFieldButton.TabIndex = 2; 
            this._addFieldButton.Click += new System.EventHandler (this.OnClickAddField);
 
            //
            // _deleteFieldButton
            //
            this._deleteFieldButton.Location = new System.Drawing.Point (186, 245); 
            this._deleteFieldButton.Name = "_deleteFieldButton";
            this._deleteFieldButton.Size = new System.Drawing.Size (26, 23); 
            this._deleteFieldButton.TabIndex = 7; 
            this._deleteFieldButton.Click += new System.EventHandler (this.OnClickDeleteField);
 
            //
            // _currentFieldProps
            //
            this._currentFieldProps.CommandsVisibleIfAvailable = true; 
            this._currentFieldProps.Enabled = false;
            this._currentFieldProps.LargeButtons = false; 
            this._currentFieldProps.LineColor = System.Drawing.SystemColors.ScrollBar; 
            this._currentFieldProps.Location = new System.Drawing.Point (244, 28);
            this._currentFieldProps.Name = "_currentFieldProps"; 
            this._currentFieldProps.Size = new System.Drawing.Size (248, 281);
            this._currentFieldProps.TabIndex = 9;
            this._currentFieldProps.ToolbarVisible = true;
            this._currentFieldProps.ViewBackColor = System.Drawing.SystemColors.Window; 
            this._currentFieldProps.ViewForeColor = System.Drawing.SystemColors.WindowText;
            this._currentFieldProps.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler (this.OnChangedPropertyValues); 
 
            //
            // _autoFieldCheck 
            //
            this._autoFieldCheck.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._autoFieldCheck.Location = new System.Drawing.Point (12, 313);
            this._autoFieldCheck.Name = "_autoFieldCheck"; 
            this._autoFieldCheck.Size = new System.Drawing.Size (172, 24);
            this._autoFieldCheck.TabIndex = 10; 
            this._autoFieldCheck.CheckedChanged += new System.EventHandler (this.OnCheckChangedAutoField); 
            this._autoFieldCheck.TextAlign = ContentAlignment.TopLeft;
            this._autoFieldCheck.CheckAlign = ContentAlignment.TopLeft; 

            //
            // _refreshSchemaLink
            // 
            this._refreshSchemaLink.Location = new System.Drawing.Point (12, 347);
            this._refreshSchemaLink.Name = "_refreshSchemaLink"; 
            this._refreshSchemaLink.Size = new System.Drawing.Size (196, 16); 
            this._refreshSchemaLink.TabIndex = 11;
            this._refreshSchemaLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler (this.OnClickRefreshSchema); 

            //
            // _templatizeLink
            // 
            this._templatizeLink.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._templatizeLink.Location = new System.Drawing.Point (244, 313); 
            this._templatizeLink.Name = "_templatizeLink"; 
            this._templatizeLink.Size = new System.Drawing.Size (248, 32);
            this._templatizeLink.TabIndex = 12; 
            this._templatizeLink.Visible = false;
            this._templatizeLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler (this.OnClickTemplatize);

            // 
            // _selFieldLabel
            // 
            this._selFieldLabel.Location = new System.Drawing.Point (244, 12); 
            this._selFieldLabel.Name = "_selFieldLabel";
            this._selFieldLabel.Size = new System.Drawing.Size (248, 16); 
            this._selFieldLabel.TabIndex = 8;

            //
            // _availableFieldsLabel 
            //
            this._availableFieldsLabel.Location = new System.Drawing.Point (12, 12); 
            this._availableFieldsLabel.Name = "_availableFieldsLabel"; 
            this._availableFieldsLabel.Size = new System.Drawing.Size (196, 16);
            this._availableFieldsLabel.TabIndex = 0; 

            //
            // _selFieldsLabel
            // 
            this._selFieldsLabel.Location = new System.Drawing.Point (12, 181);
            this._selFieldsLabel.Name = "_selFieldsLabel"; 
            this._selFieldsLabel.Size = new System.Drawing.Size (196, 16); 
            this._selFieldsLabel.TabIndex = 3;
 
            //
            // Form1
            //
            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size (507, 385); 
            this.Controls.Add (this._selFieldsLabel); 
            this.Controls.Add (this._availableFieldsLabel);
            this.Controls.Add (this._selFieldLabel); 
            this.Controls.Add (this._templatizeLink);
            this.Controls.Add (this._refreshSchemaLink);
            this.Controls.Add (this._autoFieldCheck);
            this.Controls.Add (this._currentFieldProps); 
            this.Controls.Add (this._deleteFieldButton);
            this.Controls.Add (this._addFieldButton); 
            this.Controls.Add (this._moveFieldDownButton); 
            this.Controls.Add (this._moveFieldUpButton);
            this.Controls.Add (this._cancelButton); 
            this.Controls.Add (this._okButton);
            this.Controls.Add (this._selFieldsList);
            this.Controls.Add (this._availableFieldsTree);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; 
            this.Name = "Form1";
 
            InitializeForm(); 

            this.ResumeLayout(false); 
        }
        #endregion

        private void InitForm() { 
            Image fieldNodesBitmap = new Bitmap(this.GetType(), "FieldNodes.bmp");
            ImageList fieldImages = new ImageList(); 
            fieldImages.TransparentColor = Color.Magenta; 
            fieldImages.Images.AddStrip(fieldNodesBitmap);
 
            _autoFieldCheck.Text = SR.GetString(SR.DCFEditor_AutoGen);

            _availableFieldsTree.ImageList = fieldImages;
 
            _addFieldButton.Text = SR.GetString(SR.DCFEditor_Add);
 
            ColumnHeader columnHeader = new ColumnHeader(); 
            columnHeader.Width = _selFieldsList.Width - 4;
 
            _selFieldsList.Columns.Add(columnHeader);
            _selFieldsList.SmallImageList = fieldImages;

            Icon moveUpIcon = new Icon(this.GetType(), "SortUp.ico"); 
            Bitmap moveUpBitmap = moveUpIcon.ToBitmap();
            moveUpBitmap.MakeTransparent(); 
            _moveFieldUpButton.Image = moveUpBitmap; 
            _moveFieldUpButton.AccessibleDescription = SR.GetString(SR.DCFEditor_MoveFieldUpDesc);
            _moveFieldUpButton.AccessibleName = SR.GetString(SR.DCFEditor_MoveFieldUpName); 

            Icon moveDownIcon = new Icon(this.GetType(), "SortDown.ico");
            Bitmap moveDownBitmap = moveDownIcon.ToBitmap();
            moveDownBitmap.MakeTransparent(); 
            _moveFieldDownButton.Image = moveDownBitmap;
            _moveFieldDownButton.AccessibleDescription = SR.GetString(SR.DCFEditor_MoveFieldDownDesc); 
            _moveFieldDownButton.AccessibleName = SR.GetString(SR.DCFEditor_MoveFieldDownName); 

            Icon deleteIcon = new Icon(this.GetType(), "Delete.ico"); 
            Bitmap deleteBitmap = deleteIcon.ToBitmap();
            deleteBitmap.MakeTransparent();
            _deleteFieldButton.Image = deleteBitmap;
            _deleteFieldButton.AccessibleDescription = SR.GetString(SR.DCFEditor_DeleteFieldDesc); 
            _deleteFieldButton.AccessibleName = SR.GetString(SR.DCFEditor_DeleteFieldName);
 
            _templatizeLink.Text = SR.GetString(SR.DCFEditor_Templatize); 

            _refreshSchemaLink.Text = SR.GetString(SR.DataSourceDesigner_RefreshSchemaNoHotkey); 
            _refreshSchemaLink.Visible = _controlDesigner.DataSourceDesigner == null ? false : _controlDesigner.DataSourceDesigner.CanRefreshSchema;

            _okButton.Text = SR.GetString(SR.OKCaption);
            _cancelButton.Text = SR.GetString(SR.CancelCaption); 

            _selFieldLabel.Text = SR.GetString(SR.DCFEditor_FieldProps); 
            _availableFieldsLabel.Text = SR.GetString(SR.DCFEditor_AvailableFields); 
            _selFieldsLabel.Text = SR.GetString(SR.DCFEditor_SelectedFields);
 
            _currentFieldProps.Site = _controlDesigner.Component.Site;

            this.Text = SR.GetString(SR.DCFEditor_Text);
            this.Icon = new Icon(this.GetType(), "DataControlFieldsEditor.ico"); 
        }
 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.InitPage"]/*' />
        /// <devdoc> 
        ///   Initializes the page before it can be loaded with the component.
        /// </devdoc>
        private void InitPage() {
            _autoFieldCheck.Checked = false; 

            _selectedDataSourceNode = null; 
            _selectedCheckBoxDataSourceNode = null; 
            _availableFieldsTree.Nodes.Clear();
            _selFieldsList.Items.Clear(); 
            _currentFieldItem = null;

            _propChangesPending = false;
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.LoadFields"]/*' /> 
        /// <devdoc> 
        ///   Loads the fields collection
        /// </devdoc> 
        private void LoadFields() {
            DataControlFieldCollection fields = FieldCollection;

            if (fields != null) { 
                int fieldCount = fields.Count;
 
                IDataSourceViewSchema viewSchema = GetViewSchema(); 
                for (int i = 0; i < fieldCount; i++) {
                    DataControlField field = fields[i]; 
                    FieldItem newItem = null;
                    Type fieldType = field.GetType();

                    // create the associated design time field 
                    if (fieldType == typeof(CheckBoxField)) {
                        newItem = new CheckBoxFieldItem(this, (CheckBoxField)field); 
                    } 
                    else if (fieldType == typeof(BoundField)) {
                        newItem = new BoundFieldItem(this, (BoundField)field); 
                    }
                    else if (fieldType == typeof(ButtonField)) {
                        newItem = new ButtonFieldItem(this, (ButtonField)field);
                    } 
                    else if (fieldType == typeof(HyperLinkField)) {
                        newItem = new HyperLinkFieldItem(this, (HyperLinkField)field); 
                    } 
                    else if (fieldType == typeof(TemplateField)) {
                        newItem = new TemplateFieldItem(this, (TemplateField)field); 
                    }
                    else if (fieldType == typeof(CommandField)) {
                        newItem = new CommandFieldItem(this, (CommandField)field);
                    } 
                    else if (fieldType == typeof(ImageField)) {
                        newItem = new ImageFieldItem(this, (ImageField)field); 
                    } 
                    else {
                        newItem = new CustomFieldItem(this, field); 
                    }

                    newItem.LoadFieldInfo();
 
                    IDataSourceViewSchemaAccessor schemaAccessor = newItem.RuntimeField as IDataSourceViewSchemaAccessor;
                    if (schemaAccessor != null) { 
                        schemaAccessor.DataSourceViewSchema = viewSchema; 
                    }
 
                    _selFieldsList.Items.Add(newItem);
                }

                if (_selFieldsList.Items.Count != 0) { 
                    _currentFieldItem = (FieldItem)_selFieldsList.Items[0];
                    _currentFieldItem.Selected = true; 
                } 
            }
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.LoadComponent"]/*' />
        /// <devdoc>
        ///   Loads the component into the page. 
        /// </devdoc>
        private void LoadComponent() { 
            InitPage(); 

            LoadAvailableFieldsTree(); 
            LoadDataSourceFields();

            _autoFieldCheck.Checked = AutoGenerateFields;
            LoadFields(); 

            UpdateEnabledVisibleState(); 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.LoadDataSourceFields"]/*' /> 
        /// <devdoc>
        ///   Loads the fields present in the selected datasource
        /// </devdoc>
        private void LoadDataSourceFields() { 
            EnterLoadingMode();
 
            Debug.Assert(_controlDesigner != null, "_controlDesigner is null"); 
            IDataSourceFieldSchema[] fieldSchemas = GetFieldSchemas();
 
            if (fieldSchemas != null && fieldSchemas.Length > 0) {
                DataFieldNode allFieldsNode = new DataFieldNode(this);
                _availableFieldsTree.Nodes.Insert(0, allFieldsNode);
                foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) { 
                    BoundNode fieldNode = new BoundNode(this, fieldSchema);
                    _selectedDataSourceNode.Nodes.Add(fieldNode); 
                } 
                _selectedDataSourceNode.Expand();
                foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) { 
                    if (fieldSchema.DataType == typeof(bool) ||
                        fieldSchema.DataType == typeof(bool?)) {
                        CheckBoxNode fieldNode = new CheckBoxNode(this, fieldSchema);
                        _selectedCheckBoxDataSourceNode.Nodes.Add(fieldNode); 
                    }
                } 
                _selectedCheckBoxDataSourceNode.Expand(); 
                _availableFieldsTree.SelectedNode = allFieldsNode;
                allFieldsNode.EnsureVisible(); 
            }
            else {
                BoundNode genericBoundField = new BoundNode(this, null);
                _availableFieldsTree.Nodes.Insert(0, genericBoundField); 
                genericBoundField.EnsureVisible();
 
                CheckBoxNode genericCheckBoxField = new CheckBoxNode(this, null); 
                _availableFieldsTree.Nodes.Insert(1, genericCheckBoxField);
                genericCheckBoxField.EnsureVisible(); 

                _availableFieldsTree.SelectedNode = genericBoundField;
            }
 
            ExitLoadingMode();
        } 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.LoadAvailableFieldsTree"]/*' />
        /// <devdoc> 
        ///    Loads the fixed nodes in the available fields tree, i.e., the
        ///    DataSource, Button and HyperLink nodes
        /// </devdoc>
        private void LoadAvailableFieldsTree() { 
            IDataSourceFieldSchema[] fieldSchemas = GetFieldSchemas();
            if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                _selectedDataSourceNode = new DataSourceNode(); 
                _availableFieldsTree.Nodes.Add(_selectedDataSourceNode);
 
                _selectedCheckBoxDataSourceNode = new BoolDataSourceNode();
                _availableFieldsTree.Nodes.Add(_selectedCheckBoxDataSourceNode);
            }
 
            HyperLinkNode hyperLinkNode = new HyperLinkNode(this);
            _availableFieldsTree.Nodes.Add(hyperLinkNode); 
 
            ImageNode imageNode = new ImageNode(this);
            _availableFieldsTree.Nodes.Add(imageNode); 

            ButtonNode buttonNode = new ButtonNode(this);
            _availableFieldsTree.Nodes.Add(buttonNode);
 
            CommandNode commandNode = new CommandNode(this);
            _availableFieldsTree.Nodes.Add(commandNode); 
 
            CommandNode editCommandNode = new CommandNode(this, CF_EDIT, SR.GetString(SR.DCFEditor_Node_Edit), ILI_EDITBUTTON);
            commandNode.Nodes.Add(editCommandNode); 

            if (Control is GridView) {
                CommandNode selectCommandNode = new CommandNode(this, CF_SELECT, SR.GetString(SR.DCFEditor_Node_Select), ILI_SELECTBUTTON);
                commandNode.Nodes.Add(selectCommandNode); 
            }
 
            CommandNode deleteCommandNode = new CommandNode(this, CF_DELETE, SR.GetString(SR.DCFEditor_Node_Delete), ILI_DELETEBUTTON); 
            commandNode.Nodes.Add(deleteCommandNode);
 
            if (Control is DetailsView) {
                CommandNode insertCommandNode = new CommandNode(this, CF_INSERT, SR.GetString(SR.DCFEditor_Node_Insert), ILI_INSERTBUTTON);
                commandNode.Nodes.Add(insertCommandNode);
            } 

            TemplateNode templateNode = new TemplateNode(this); 
            _availableFieldsTree.Nodes.Add(templateNode); 
        }
 
        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);

            if (_initialActivate) { 
                LoadComponent();
                _initialActivate = false; 
            } 
        }
 
        private void OnAvailableFieldsDoubleClick(object source, TreeNodeMouseClickEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                OnClickAddField(source, e);
            } 
        }
 
        private void OnAvailableFieldsGotFocus(object source, EventArgs e) { 
            _currentFieldProps.SelectedObject = null;
        } 

        private void OnAvailableFieldsKeyPress(object source, KeyPressEventArgs e) {
            if (e.KeyChar == (char)13) {
                OnClickAddField(source, e); 
                e.Handled = true;
            } 
        } 

        /// <devdoc> 
        ///    Handles changes to the field properties made in the field node editor.
        ///    Sets a flag to indicate there are pending changes.
        /// </devdoc>
        private void OnChangedPropertyValues(object source, PropertyValueChangedEventArgs e) { 
            if (_isLoading)
                return; 
 
            if (e.ChangedItem.Label == "HeaderText" || e.ChangedItem.PropertyDescriptor.ComponentType == typeof(CommandField)) {
                _propChangesPending = true; 
                SaveFieldProperties();
                if (_selFieldsList.SelectedItems.Count == 0)
                    _currentFieldItem = null;
                else { 
                    _currentFieldItem = (FieldItem)_selFieldsList.SelectedItems[0];
                    CommandFieldItem commandFieldItem = _currentFieldItem as CommandFieldItem; 
                    if (commandFieldItem != null) { 
                        commandFieldItem.UpdateImageIndex();
                    } 
                }
            }
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnCheckChangedAutoField"]/*' />
        /// <devdoc> 
        ///    Handles changes to the auto field generation choice. 
        ///    When this functionality is turned on, the fields collection is
        ///    cleared, and auto generated fields are shown. When it is turned 
        ///    off, nothing is done, which effectively makes the auto generated
        ///    fields part of the field collection.
        /// </devdoc>
        private void OnCheckChangedAutoField(object source, EventArgs e) { 
            if (_isLoading)
                return; 
 
            UpdateEnabledVisibleState();
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickAddField"]/*' />
        /// <devdoc>
        ///    Adds a field to the field collection 
        /// </devdoc>
        private void OnClickAddField(object source, EventArgs e) { 
            AvailableFieldNode selectedNode = (AvailableFieldNode)_availableFieldsTree.SelectedNode; 

            if (!_addFieldButton.Enabled) { 
                return;
            }

            Debug.Assert((selectedNode != null) && 
                         selectedNode.IsFieldCreator,
                         "Add button should not have been enabled"); 
 
            // first save off any pending changes
            if (_propChangesPending) { 
                SaveFieldProperties();
            }

            if (selectedNode.CreatesMultipleFields == false) { 
                FieldItem field = selectedNode.CreateField();
 
                _selFieldsList.Items.Add(field); 
                _currentFieldItem = field;
                _currentFieldItem.Selected = true; 
                _currentFieldItem.EnsureVisible();
            }
            else {
                IDataSourceFieldSchema[] fieldSchemas = GetFieldSchemas(); 
                FieldItem[] fields = selectedNode.CreateFields(Control, fieldSchemas);
                int fieldCount = fields.Length; 
 
                for (int i = 0; i < fieldCount; i++) {
                    _selFieldsList.Items.Add(fields[i]); 
                }
                _currentFieldItem = fields[fieldCount - 1];
                _currentFieldItem.Selected = true;
                _currentFieldItem.EnsureVisible(); 
            }
 
            IDataSourceViewSchemaAccessor schemaAccessor = _currentFieldItem.RuntimeField as IDataSourceViewSchemaAccessor; 
            if (schemaAccessor != null) {
                schemaAccessor.DataSourceViewSchema = GetViewSchema(); 
            }

            _selFieldsList.Focus();
            _selFieldsList.FocusedItem = _currentFieldItem; 

            UpdateEnabledVisibleState(); 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickDeleteField"]/*' /> 
        /// <devdoc>
        ///   Deletes a field from the field collection.
        /// </devdoc>
        private void OnClickDeleteField(object source, EventArgs e) { 
            Debug.Assert(_currentFieldItem != null, "Must have a field item to delete");
 
            int currentIndex = _currentFieldItem.Index; 
            int nextIndex = -1;
            int itemCount = _selFieldsList.Items.Count; 

            if (itemCount > 1) {
                if (currentIndex == (itemCount - 1))
                    nextIndex = currentIndex - 1; 
                else
                    nextIndex = currentIndex; 
            } 

            // discard changes that might have existed for the field 
            _propChangesPending = false;
            _currentFieldItem.Remove();
            _currentFieldItem = null;
 
            if (nextIndex != -1) {
                _currentFieldItem = (FieldItem)_selFieldsList.Items[nextIndex]; 
                _currentFieldItem.Selected = true; 
                _currentFieldItem.EnsureVisible();
                _deleteFieldButton.Focus(); 
            }

            UpdateEnabledVisibleState();
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickMoveFieldDown"]/*' /> 
        /// <devdoc> 
        ///   Move a field down within the field collection
        /// </devdoc> 
        private void OnClickMoveFieldDown(object source, EventArgs e) {
            Debug.Assert(_currentFieldItem != null, "Must have a field item to move");

            _fieldMovePending = true; 

            int indexCurrent = _currentFieldItem.Index; 
            Debug.Assert(indexCurrent < _selFieldsList.Items.Count - 1, 
                         "Move down not allowed");
 
            ListViewItem temp = _selFieldsList.Items[indexCurrent];
            _selFieldsList.Items.RemoveAt(indexCurrent);
            _selFieldsList.Items.Insert(indexCurrent + 1, temp);
 
            _currentFieldItem = (FieldItem)_selFieldsList.Items[indexCurrent + 1];
            _currentFieldItem.Selected = true; 
            _currentFieldItem.EnsureVisible(); 

            UpdateFieldPositionButtonsState(); 

            // If the down button is disabled but up is enabled, put focus on Up so it doesn't go to Delete.
            if (_moveFieldUpButton.Enabled && !_moveFieldDownButton.Enabled) {
                _moveFieldUpButton.Focus(); 
            }
            _fieldMovePending = false; 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickMoveFieldUp"]/*' /> 
        /// <devdoc>
        ///   Move a field up within the field collection
        /// </devdoc>
        private void OnClickMoveFieldUp(object source, EventArgs e) { 
            Debug.Assert(_currentFieldItem != null, "Must have a field item to move");
 
            _fieldMovePending = true; 

            int indexCurrent = _currentFieldItem.Index; 
            Debug.Assert(indexCurrent > 0, "Move up not allowed");

            ListViewItem temp = _selFieldsList.Items[indexCurrent];
            _selFieldsList.Items.RemoveAt(indexCurrent); 
            _selFieldsList.Items.Insert(indexCurrent - 1, temp);
 
            _currentFieldItem = (FieldItem)_selFieldsList.Items[indexCurrent - 1]; 
            _currentFieldItem.Selected = true;
            _currentFieldItem.EnsureVisible(); 

            UpdateFieldPositionButtonsState();
            _fieldMovePending = false;
        } 

        private void OnClickOK(object source, EventArgs e) { 
            SaveComponent(); 
            PersistClonedFieldsToControl();
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickRefreshSchema"]/*' />
        /// <devdoc>
        ///   Refreshes the schema of the data bound control. 
        /// </devdoc>
        private void OnClickRefreshSchema(object source, LinkLabelLinkClickedEventArgs e) { 
            _fieldSchemas = null; 
            _viewSchema = null;
 
            IDataSourceDesigner dsd = _controlDesigner.DataSourceDesigner;
            if (dsd != null) {
                if (dsd.CanRefreshSchema) {
                    dsd.RefreshSchema(false); 
                }
            } 
 
            IDataSourceViewSchema viewSchema = GetViewSchema();
            foreach (FieldItem fieldItem in _selFieldsList.Items) { 
                IDataSourceViewSchemaAccessor schemaAccessor = fieldItem.RuntimeField as IDataSourceViewSchemaAccessor;
                if (schemaAccessor != null) {
                    schemaAccessor.DataSourceViewSchema = viewSchema;
                } 
            }
            _availableFieldsTree.Nodes.Clear(); 
            LoadAvailableFieldsTree(); 
            LoadDataSourceFields();
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnClickTemplatize"]/*' />
        /// <devdoc>
        ///   Converts a field into an equivalent template field. 
        /// </devdoc>
        private void OnClickTemplatize(object source, LinkLabelLinkClickedEventArgs e) { 
            Debug.Assert(_currentFieldItem != null, "Must have a field item to templatize"); 
            Debug.Assert((_currentFieldItem is BoundFieldItem) ||
                         (_currentFieldItem is ButtonFieldItem) || 
                         (_currentFieldItem is HyperLinkFieldItem) ||
                         (_currentFieldItem is CheckBoxFieldItem) ||
                         (_currentFieldItem is CommandFieldItem) ||
                         (_currentFieldItem is ImageFieldItem), 
                         "Unexpected type of field being templatized");
 
            if (_propChangesPending) { 
                SaveFieldProperties();
            } 

            //_currentFieldItem.SaveFieldInfo();

            TemplateField newField; 
            TemplateFieldItem newFieldItem;
 
            newField = _currentFieldItem.GetTemplateField(Control); 
            newFieldItem = new TemplateFieldItem(this, newField);
            newFieldItem.LoadFieldInfo(); 

            _selFieldsList.Items[_currentFieldItem.Index] = newFieldItem;

            _currentFieldItem = newFieldItem; 
            _currentFieldItem.Selected = true;
 
            UpdateEnabledVisibleState(); 
        }
 
        protected override void OnClosed(EventArgs e) {
            IDataSourceDesigner dsd = _controlDesigner.DataSourceDesigner;
            if (dsd != null) {
                dsd.ResumeDataSourceEvents(); 
            }
            IgnoreRefreshSchema = _initialIgnoreRefreshSchemaValue; 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnSelChangedAvailableFields"]/*' /> 
        /// <devdoc>
        ///    Handles selection change in the available fields tree.
        /// </devdoc>
        private void OnSelChangedAvailableFields(object source, TreeViewEventArgs e) { 
            UpdateEnabledVisibleState();
        } 
 
        private void OnSelFieldsListGotFocus(object source, EventArgs e) {
            UpdateEnabledVisibleState(); 
        }

        /// <devdoc>
        ///      Handles keypress events for the list box. 
        /// </devdoc>
        private void OnSelFieldsListKeyDown(object sender, KeyEventArgs e) { 
            if (e.KeyData == Keys.Delete) { 
                if (_currentFieldItem != null) {
                    OnClickDeleteField(sender, e); 
                }
                e.Handled = true;
            }
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.OnSelIndexChangedSelFieldsList"]/*' /> 
        /// <devdoc> 
        ///    Handles selection change within the selected fields list.
        /// </devdoc> 
        private void OnSelIndexChangedSelFieldsList(object source, EventArgs e) {
            if (_fieldMovePending) {
                return;
            } 

            if (_propChangesPending) { 
                SaveFieldProperties(); 
            }
 
            if (_selFieldsList.SelectedItems.Count == 0)
                _currentFieldItem = null;
            else
                _currentFieldItem = (FieldItem)_selFieldsList.SelectedItems[0]; 

            SetFieldPropertyHeader(); 
            UpdateEnabledVisibleState(); 
        }
 
        private void PersistClonedFieldsToControl() {
            DataControlFieldCollection controlFields = null;
            if (Control is GridView) {
                controlFields = ((GridView)Control).Columns; 
            }
            else if (Control is DetailsView) { 
                controlFields = ((DetailsView)Control).Fields; 
            }
 
            if (controlFields != null) {
                controlFields.Clear();
                foreach (DataControlField field in FieldCollection) {
                    controlFields.Add(field); 
                }
            } 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.SaveFieldProperties"]/*' /> 
        /// <devdoc>
        ///   Saves the properties of a field from the ui
        /// </devdoc>
        private void SaveFieldProperties() { 
            Debug.Assert(_propChangesPending == true, "Unneccessary call to SaveFieldProperties.");
 
            if (_currentFieldItem != null) { 
                _currentFieldItem.HeaderText = _currentFieldItem.RuntimeField.HeaderText;
 
                if (_currentFieldProps.Visible) {
                    _currentFieldProps.Refresh();
                }
            } 

            _propChangesPending = false; 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.SaveComponent"]/*' /> 
        /// <devdoc>
        ///   Saves the component loaded into the page.
        /// </devdoc>
        private void SaveComponent() { 
            if (_propChangesPending) {
                SaveFieldProperties(); 
            } 

            AutoGenerateFields = _autoFieldCheck.Checked; 

            // save the fields collection
            DataControlFieldCollection fields = FieldCollection;
 
            if (fields != null) {
                fields.Clear(); 
                int fieldCount = _selFieldsList.Items.Count; 

                for (int i = 0; i < fieldCount; i++) { 
                    FieldItem fieldItem = (FieldItem)_selFieldsList.Items[i];
                    fields.Add(fieldItem.RuntimeField);
                }
            } 
        }
 
        /// <devdoc> 
        ///   Sets the label above the selected fields property grid.  Shows the type of the field.
        /// </devdoc> 
        private void SetFieldPropertyHeader() {
            string propGroupText = SR.GetString(SR.DCFEditor_FieldProps);

            if (_currentFieldItem != null) { 
                EnterLoadingMode();
                Type currentFieldItemType = _currentFieldItem.GetType(); 
 
                if (currentFieldItemType == typeof(CheckBoxFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_CheckBox)); 
                }
                else if (currentFieldItemType == typeof(BoundFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Bound));
                } 
                else if (currentFieldItemType == typeof(ButtonFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Button)); 
                } 
                else if (currentFieldItemType == typeof(HyperLinkFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_HyperLink)); 
                }
                else if (currentFieldItemType == typeof(CommandFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Command));
                } 
                else if (currentFieldItemType == typeof(TemplateFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Template)); 
                } 
                else if (currentFieldItemType == typeof(ImageFieldItem)) {
                    propGroupText = SR.GetString(SR.DCFEditor_FieldPropsFormat, SR.GetString(SR.DCFEditor_Node_Image)); 
                }

                ExitLoadingMode();
            } 
            _selFieldLabel.Text = propGroupText;
        } 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.UpdateEnabledVisibleState"]/*' />
        /// <devdoc> 
        /// </devdoc>
        private void UpdateEnabledVisibleState() {
            AvailableFieldNode selFieldNode = (AvailableFieldNode)_availableFieldsTree.SelectedNode;
 
            int fieldCount = _selFieldsList.Items.Count;
            int selFieldCount = _selFieldsList.SelectedItems.Count; 
            FieldItem selField = null; 
            int selFieldIndex = -1;
 
            if (selFieldCount != 0)
                selField = (FieldItem)_selFieldsList.SelectedItems[0];
            if (selField != null)
                selFieldIndex = selField.Index; 

            bool fieldSelected = (selFieldIndex != -1); 
 
            _addFieldButton.Enabled = (selFieldNode != null) && selFieldNode.IsFieldCreator;
            _deleteFieldButton.Enabled = fieldSelected; 
            UpdateFieldPositionButtonsState();

            _currentFieldProps.Enabled = selField != null;
 
            _currentFieldProps.SelectedObject = (selField != null && _selFieldsList.Focused) ? selField.RuntimeField : null;
 
            Type selFieldType = selField == null ? null : selField.RuntimeField.GetType(); 

            _templatizeLink.Visible = (fieldCount != 0 && selField != null && 
                                      (selFieldType == typeof(BoundField) ||
                                       selFieldType == typeof(CheckBoxField) ||
                                       selFieldType == typeof(ButtonField) ||
                                       selFieldType == typeof(HyperLinkField) || 
                                       selFieldType == typeof(CommandField) ||
                                       selFieldType == typeof(ImageField))); 
        } 

        private void UpdateFieldPositionButtonsState() { 
            int selFieldIndex = -1;
            int selFieldCount = _selFieldsList.SelectedItems.Count;
            FieldItem selField = null;
 
            if (selFieldCount > 0) {
                selField = _selFieldsList.SelectedItems[0] as FieldItem; 
            } 
            if (selField != null) {
                selFieldIndex = selField.Index; 
            }

            _moveFieldUpButton.Enabled = (selFieldIndex > 0);
            _moveFieldDownButton.Enabled = (selFieldIndex >= 0) && (selFieldIndex < (_selFieldsList.Items.Count - 1)); 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.AvailableFieldNode"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        private abstract class AvailableFieldNode : TreeNode {
            public AvailableFieldNode(string text, int icon) : base(text, icon, icon) {
            }
 
            public virtual bool CreatesMultipleFields {
                get { 
                    return false; 
                }
            } 

            public virtual bool IsFieldCreator {
                get {
                    return true; 
                }
            } 
 
            public virtual FieldItem CreateField() {
                return null; 
            }

            public virtual FieldItem[] CreateFields(DataBoundControl control, IDataSourceFieldSchema[] fieldSchemas) {
                return null; 
            }
        } 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.DataSourceNode"]/*' />
        /// <devdoc> 
        ///   This represents the datasource in the available fields tree.  This is used if there is a schema.
        /// </devdoc>
        private class DataSourceNode : AvailableFieldNode {
            public DataSourceNode() : base(SR.GetString(SR.DCFEditor_Node_Bound), DataControlFieldsEditor.ILI_DATASOURCE) { 
            }
 
            public override bool IsFieldCreator { 
                get {
                    return false; 
                }
            }
        }
 
        /// <devdoc>
        ///   This represents the check box field candidates in the available fields tree.  This is used if there is a schema. 
        /// </devdoc> 
        private class BoolDataSourceNode : AvailableFieldNode {
            public BoolDataSourceNode() : base(SR.GetString(SR.DCFEditor_Node_CheckBox), DataControlFieldsEditor.ILI_BOOLDATASOURCE) { 
            }

            public override bool IsFieldCreator {
                get { 
                    return false;
                } 
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.DataFieldNode"]/*' />
        /// <devdoc>
        ///   This represents the pseudo field implying all datafields.
        /// </devdoc> 
        private class DataFieldNode : AvailableFieldNode {
            private DataControlFieldsEditor _fieldsEditor; 
 
            public DataFieldNode(DataControlFieldsEditor fieldsEditor) : base(SR.GetString(SR.DCFEditor_Node_AllFields), DataControlFieldsEditor.ILI_ALL) {
                _fieldsEditor = fieldsEditor; 
            }

            public override bool CreatesMultipleFields {
                get { 
                    return true;
                } 
            } 

            public override FieldItem CreateField() { 
                throw new NotSupportedException();
            }

            public override FieldItem[] CreateFields(DataBoundControl control, IDataSourceFieldSchema[] fieldSchemas) { 
                if (fieldSchemas == null) {
                    return null; 
                } 

                ArrayList createdFields = new ArrayList(); 

                foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) {
                    if ((control is GridView && ((GridView)control).IsBindableType(fieldSchema.DataType)) ||
                        (control is DetailsView && ((DetailsView)control).IsBindableType(fieldSchema.DataType))) { 
                        BoundField runtimeField = null;
                        FieldItem field = null; 
                        string fieldSchemaName = fieldSchema.Name; 
                        if (fieldSchema.DataType == typeof(bool) ||
                            fieldSchema.DataType == typeof(bool?)) { 
                            runtimeField = new CheckBoxField();
                            runtimeField.HeaderText = fieldSchemaName;
                            runtimeField.DataField = fieldSchemaName;
                            runtimeField.SortExpression = fieldSchemaName; 

                            field = new CheckBoxFieldItem(_fieldsEditor, (CheckBoxField)runtimeField); 
                        } 
                        else {
                            runtimeField = new BoundField(); 
                            runtimeField.HeaderText = fieldSchemaName;
                            runtimeField.DataField = fieldSchemaName;
                            runtimeField.SortExpression = fieldSchemaName;
 
                            field = new BoundFieldItem(_fieldsEditor, runtimeField);
                        } 
                        if (fieldSchema.PrimaryKey) { 
                            runtimeField.ReadOnly = true;
                        } 
                        if (fieldSchema.Identity) {
                            runtimeField.InsertVisible = false;
                        }
 
                        field.LoadFieldInfo();
                        createdFields.Add(field); 
                    } 
                }
 
                return (FieldItem[])createdFields.ToArray(typeof(FieldItem));
            }
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.BoundNode"]/*' />
        /// <devdoc> 
        ///   This represents a boundfield available in the selected datasource within 
        ///   in the available fields tree.
        ///   It could also represent the pseudo field implying all datafields. 
        /// </devdoc>
        private class BoundNode : AvailableFieldNode {
            protected IDataSourceFieldSchema _fieldSchema;
            private bool _genericBoundField; 
            private DataControlFieldsEditor _fieldsEditor;
 
            public BoundNode(DataControlFieldsEditor fieldsEditor, IDataSourceFieldSchema fieldSchema) : base(fieldSchema == null ? String.Empty : fieldSchema.Name, DataControlFieldsEditor.ILI_BOUND) { 
                this._fieldSchema = fieldSchema;
                _fieldsEditor = fieldsEditor; 
                if (fieldSchema == null) {
                    _genericBoundField = true;
                    Text = SR.GetString(SR.DCFEditor_Node_Bound);
                } 
            }
 
            public override FieldItem CreateField() { 
                BoundField runtimeField = new BoundField();
                string fieldName = String.Empty; 
                if (_fieldSchema != null)
                    fieldName = _fieldSchema.Name;

                if (_genericBoundField == false) { 
                    runtimeField.HeaderText = fieldName;
                    runtimeField.DataField = fieldName; 
                    runtimeField.SortExpression = fieldName; 
                }
                if (_fieldSchema != null) { 
                    if (_fieldSchema.PrimaryKey) {
                        runtimeField.ReadOnly = true;
                    }
                    if (_fieldSchema.Identity) { 
                        runtimeField.InsertVisible = false;
                    } 
                } 

                FieldItem field = new BoundFieldItem(_fieldsEditor, runtimeField); 
                field.LoadFieldInfo();

                return field;
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.ButtonNode"]/*' /> 
        /// <devdoc>
        ///   This represents a button field in the available fields tree. 
        /// </devdoc>
        private class ButtonNode : AvailableFieldNode {

            private string command; 
            private string buttonText;
            private DataControlFieldsEditor _fieldsEditor; 
 
            public ButtonNode(DataControlFieldsEditor fieldsEditor) : this(fieldsEditor, String.Empty, SR.GetString(SR.DCFEditor_Button), SR.GetString(SR.DCFEditor_Node_Button)) {
            } 

            public ButtonNode(DataControlFieldsEditor fieldsEditor, string command, string buttonText, string text) : base(text, DataControlFieldsEditor.ILI_BUTTON) {
                _fieldsEditor = fieldsEditor;
                this.command = command; 
                this.buttonText = buttonText;
            } 
 
            public override FieldItem CreateField() {
                ButtonField runtimeField = new ButtonField(); 
                runtimeField.Text = buttonText;
                runtimeField.CommandName = command;

                FieldItem field = new ButtonFieldItem(_fieldsEditor, runtimeField); 
                field.LoadFieldInfo();
 
                return field; 
            }
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CheckBoxNode"]/*' />
        /// <devdoc>
        ///   This represents a CheckBox field in the available fields tree. 
        /// </devdoc>
        private class CheckBoxNode : AvailableFieldNode { 
 
            protected IDataSourceFieldSchema _fieldSchema;
            private bool _genericCheckBoxField; 
            private DataControlFieldsEditor _fieldsEditor;

            public CheckBoxNode(DataControlFieldsEditor fieldsEditor, IDataSourceFieldSchema fieldSchema) : base(fieldSchema == null ? String.Empty : fieldSchema.Name, DataControlFieldsEditor.ILI_CHECKBOX) {
                _fieldsEditor = fieldsEditor; 
                this._fieldSchema = fieldSchema;
                if (fieldSchema == null) { 
                    _genericCheckBoxField = true; 
                    Text = SR.GetString(SR.DCFEditor_Node_CheckBox);
                }} 

            public override FieldItem CreateField() {
                CheckBoxField runtimeField = new CheckBoxField();
                string fieldName = String.Empty; 
                if (_fieldSchema != null) {
                    fieldName = _fieldSchema.Name; 
                } 

                if (_genericCheckBoxField == false) { 
                    runtimeField.HeaderText = fieldName;
                    runtimeField.DataField = fieldName;
                    runtimeField.SortExpression = fieldName;
                } 
                if (_fieldSchema != null) {
                    if (_fieldSchema.PrimaryKey) { 
                        runtimeField.ReadOnly = true; 
                    }
                    if (_fieldSchema.Identity) { 
                        runtimeField.InsertVisible = false;
                    }
                }
 
                FieldItem field = new CheckBoxFieldItem(_fieldsEditor, runtimeField);
                field.LoadFieldInfo(); 
 
                return field;
            } 
        }

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.ImageNode"]/*' />
        /// <devdoc> 
        ///   This represents a Image field in the available fields tree.
        /// </devdoc> 
        private class ImageNode : AvailableFieldNode { 

            private DataControlFieldsEditor _fieldsEditor; 

            public ImageNode(DataControlFieldsEditor fieldsEditor) : base(String.Empty, DataControlFieldsEditor.ILI_IMAGE) {
                _fieldsEditor = fieldsEditor;
                Text = SR.GetString(SR.DCFEditor_Node_Image); 
            }
 
            public override FieldItem CreateField() { 
                ImageField runtimeField = new ImageField();
 
                FieldItem field = new ImageFieldItem(_fieldsEditor, runtimeField);
                field.LoadFieldInfo();

                return field; 
            }
        } 
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CommandNode"]/*' />
        /// <devdoc> 
        /// </devdoc>
        private class CommandNode : AvailableFieldNode {

            private int commandType; 
            private DataControlFieldsEditor _fieldsEditor;
 
            public CommandNode(DataControlFieldsEditor fieldsEditor) : this(fieldsEditor, -1, SR.GetString(SR.DCFEditor_Node_Command), DataControlFieldsEditor.ILI_COMMAND) { 
            }
 
            public CommandNode(DataControlFieldsEditor fieldsEditor, int commandType, string text, int icon) : base(text, icon) {
                this.commandType = commandType;
                _fieldsEditor = fieldsEditor;
            } 

            public override FieldItem CreateField() { 
                CommandField runtimeField = new CommandField(); 

                switch (commandType) { 
                    case CF_EDIT:
                        runtimeField.ShowEditButton = true;
                        break;
                    case CF_SELECT: 
                        runtimeField.ShowSelectButton = true;
                        break; 
                    case CF_DELETE: 
                        runtimeField.ShowDeleteButton = true;
                        break; 
                    case CF_INSERT:
                        runtimeField.ShowInsertButton = true;
                        break;
                    default: 
                        break;
                } 
 

                FieldItem field = new CommandFieldItem(_fieldsEditor, runtimeField); 
                field.LoadFieldInfo();

                return field;
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.HyperLinkNode"]/*' /> 
        /// <devdoc>
        ///   This represents a HyperLink field in the available fields tree. 
        /// </devdoc>
        private class HyperLinkNode : AvailableFieldNode {
            private string hyperLinkText;
            private DataControlFieldsEditor _fieldsEditor; 

            public HyperLinkNode(DataControlFieldsEditor fieldsEditor) : this(fieldsEditor, SR.GetString(SR.DCFEditor_HyperLink)) { 
            } 

            public HyperLinkNode(DataControlFieldsEditor fieldsEditor, string hyperLinkText) : base(SR.GetString(SR.DCFEditor_Node_HyperLink), DataControlFieldsEditor.ILI_HYPERLINK) { 
                _fieldsEditor = fieldsEditor;
                this.hyperLinkText = hyperLinkText;
            }
 
            public override FieldItem CreateField() {
                HyperLinkField runtimeField = new HyperLinkField(); 
 
                FieldItem field = new HyperLinkFieldItem(_fieldsEditor, runtimeField);
                field.Text = hyperLinkText; 
                field.LoadFieldInfo();

                return field;
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.TemplateNode"]/*' /> 
        /// <devdoc>
        ///   This represents a template field in the available fields tree. 
        /// </devdoc>
        private class TemplateNode : AvailableFieldNode {
            private DataControlFieldsEditor _fieldsEditor;
 
            public TemplateNode(DataControlFieldsEditor fieldsEditor) : base(SR.GetString(SR.DCFEditor_Node_Template), DataControlFieldsEditor.ILI_TEMPLATE) {
                _fieldsEditor = fieldsEditor; 
            } 

            public override FieldItem CreateField() { 
                TemplateField runtimeField = new TemplateField();

                FieldItem field = new TemplateFieldItem(_fieldsEditor, runtimeField);
                field.LoadFieldInfo(); 

                return field; 
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.FieldItem"]/*' />
        /// <devdoc>
        ///   Represents a field in the fields collection of the DataGrid.
        /// </devdoc> 
        private abstract class FieldItem : ListViewItem {
            protected DataControlField runtimeField; 
            protected DataControlFieldsEditor fieldsEditor; 

            public FieldItem(DataControlFieldsEditor fieldsEditor, DataControlField runtimeField, int image) : base(String.Empty, image) { 
                this.fieldsEditor = fieldsEditor;
                this.runtimeField = runtimeField;
                this.Text = GetNodeText(null);
            } 

            public string HeaderText { 
                get { 
                    return runtimeField.HeaderText;
                } 
                set {
                    runtimeField.HeaderText = value;
                    UpdateDisplayText();
                } 
            }
 
            public DataControlField RuntimeField { 
                get {
                    return runtimeField; 
                }
            }

            protected virtual string GetDefaultNodeText() { 
                return runtimeField.GetType().Name;
            } 
 
            public virtual string GetNodeText(string headerText) {
                if ((headerText == null) || (headerText.Length == 0)) { 
                    return GetDefaultNodeText();
                }
                else {
                    return headerText; 
                }
            } 
 
            protected ITemplate GetTemplate(DataBoundControl control, string templateContent) {
                try { 
                    ISite site = control.Site;
                    Debug.Assert(site != null);

                    IDesignerHost designerHost = (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
                    if (templateContent != null && templateContent.Length > 0) {
                        return ControlParser.ParseTemplate(designerHost, templateContent, null); 
                    } 
                    return null;
                } catch (Exception e) { 
                    Debug.Fail(e.ToString());
                    return null;
                }
            } 

            public virtual TemplateField GetTemplateField(DataBoundControl dataBoundControl) { 
                TemplateField field = new TemplateField(); 

                field.HeaderText = runtimeField.HeaderText; 
                field.HeaderImageUrl = runtimeField.HeaderImageUrl;
                field.AccessibleHeaderText = runtimeField.AccessibleHeaderText;
                field.FooterText = runtimeField.FooterText;
 
                field.SortExpression = runtimeField.SortExpression;
                field.Visible = runtimeField.Visible; 
                field.InsertVisible = runtimeField.InsertVisible; 
                field.ShowHeader = runtimeField.ShowHeader;
 
                field.ControlStyle.CopyFrom(runtimeField.ControlStyle);
                field.FooterStyle.CopyFrom(runtimeField.FooterStyle);
                field.HeaderStyle.CopyFrom(runtimeField.HeaderStyle);
                field.ItemStyle.CopyFrom(runtimeField.ItemStyle); 

                return field; 
            } 

            public virtual void LoadFieldInfo() { 
                UpdateDisplayText();
            }

            protected string PrepareFormatString(string formatString) { 
                // replace a single quote character with its escaped version
                return formatString.Replace("'", "&#039;"); 
            } 

            protected void UpdateDisplayText() { 
                this.Text = GetNodeText(HeaderText);
            }
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.BoundFieldItem"]/*' />
        /// <devdoc> 
        ///    Represents a field bound to a datafield. 
        /// </devdoc>
        private class BoundFieldItem : FieldItem { 

            public BoundFieldItem(DataControlFieldsEditor fieldsEditor, BoundField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_BOUND) {
            }
 
            protected override string GetDefaultNodeText() {
                string dataField = ((BoundField)RuntimeField).DataField; 
                if ((dataField != null) && (dataField.Length != 0)) { 
                    return dataField;
                } 
                return SR.GetString(SR.DCFEditor_Node_Bound);
            }

            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) { 
                TemplateField field = base.GetTemplateField(dataBoundControl);
 
                field.SortExpression = RuntimeField.SortExpression; 
                field.ItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_READONLY, false));
                field.ConvertEmptyStringToNull = ((BoundField)RuntimeField).ConvertEmptyStringToNull; 
                field.EditItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_EDIT, ((BoundField)RuntimeField).ReadOnly));
                if (dataBoundControl is DetailsView && ((BoundField)RuntimeField).InsertVisible) {
                    field.InsertItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_INSERT, false));
                } 

                return field; 
            } 

            private string GetTemplateContent(int editMode, bool readOnly) { 
                StringBuilder sb = new StringBuilder();
                bool useReadOnlyEditTemplate = editMode == MODE_EDIT && readOnly;
                Type controlType = ((editMode == MODE_READONLY || useReadOnlyEditTemplate) ? typeof(System.Web.UI.WebControls.Label) : typeof(System.Web.UI.WebControls.TextBox));
 
                string dataFormatString = ((BoundField)RuntimeField).DataFormatString;
                string dataField = ((BoundField)this.RuntimeField).DataField; 
                string bindDataFormatString = String.Empty; 

                if ((editMode != MODE_EDIT || ((BoundField)this.RuntimeField).ApplyFormatInEditMode) || 
                    useReadOnlyEditTemplate) {
                    bindDataFormatString = PrepareFormatString(dataFormatString);
                }
 
                string bindString = (useReadOnlyEditTemplate) ?
                    DesignTimeDataBinding.CreateEvalExpression(dataField, bindDataFormatString) : 
                    DesignTimeDataBinding.CreateBindExpression(dataField, bindDataFormatString); 

                if (editMode == MODE_INSERT && !((BoundField)this.RuntimeField).InsertVisible) { 
                    return String.Empty;
                }

                sb.Append("<asp:"); 
                sb.Append(controlType.Name);
                sb.Append(" runat=\"server\""); 
 
                if (dataField.Length != 0) {
                    sb.Append(" Text='<%# "); 
                    sb.Append(bindString);
                    sb.Append(" %>'");
                }
 

                sb.Append(" id=\""); 
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, editMode)); 
                sb.Append("\"></asp:");
                sb.Append(controlType.Name); 
                sb.Append(">");

                return sb.ToString();
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.ButtonFieldItem"]/*' /> 
        /// <devdoc>
        ///   Represents a field containing a button. 
        /// </devdoc>
        private class ButtonFieldItem : FieldItem {
            public ButtonFieldItem(DataControlFieldsEditor fieldsEditor, ButtonField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_BUTTON) {
            } 

            protected override string GetDefaultNodeText() { 
                string buttonText = ((ButtonField)runtimeField).Text; 

                if ((buttonText != null) && (buttonText.Length != 0)) { 
                    return buttonText;
                }
                return SR.GetString(SR.DCFEditor_Node_Button);
            } 

            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) { 
                TemplateField field = base.GetTemplateField(dataBoundControl); 
                ButtonField runtimeField = (ButtonField)RuntimeField;
 
                StringBuilder sb = new StringBuilder();
                Type controlType = typeof(System.Web.UI.WebControls.Button);
                if (runtimeField.ButtonType == ButtonType.Link) {
                    controlType = typeof(System.Web.UI.WebControls.LinkButton); 
                }
                else if (runtimeField.ButtonType == ButtonType.Image) { 
                    controlType = typeof(System.Web.UI.WebControls.ImageButton); 
                }
 
                sb.Append("<asp:");
                sb.Append(controlType.Name);
                sb.Append(" runat=\"server\"");
 
                if (runtimeField.DataTextField.Length != 0) {
                    sb.Append(" Text='<%# "); 
                    sb.Append(DesignTimeDataBinding.CreateEvalExpression(runtimeField.DataTextField, PrepareFormatString(runtimeField.DataTextFormatString))); 
                    sb.Append(" %>'");
                } 
                else {
                    sb.Append(" Text=\"");
                    sb.Append(runtimeField.Text);
                    sb.Append("\""); 
                }
                sb.Append(" CommandName=\""); 
                sb.Append(runtimeField.CommandName); 
                sb.Append("\"");
 
                if (runtimeField.ButtonType == ButtonType.Image && runtimeField.ImageUrl.Length > 0) {
                    sb.Append(" ImageUrl=\"");
                    sb.Append(runtimeField.ImageUrl);
                    sb.Append("\""); 
                }
 
                sb.Append(" CausesValidation=\"false\" id=\""); 
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, MODE_READONLY));
                sb.Append("\"></asp:"); 
                sb.Append(controlType.Name);
                sb.Append(">");

                field.ItemTemplate = GetTemplate(dataBoundControl, sb.ToString()); 

                return field; 
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CheckBoxFieldItem"]/*' />
        /// <devdoc>
        ///    Represents a field bound to a datafield.
        /// </devdoc> 
        private class CheckBoxFieldItem : FieldItem {
            public CheckBoxFieldItem(DataControlFieldsEditor fieldsEditor, CheckBoxField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_CHECKBOX) { 
            } 

            protected override string GetDefaultNodeText() { 
                string dataField = ((CheckBoxField)RuntimeField).DataField;
                if ((dataField != null) && (dataField.Length != 0)) {
                    return dataField;
                } 
                return SR.GetString(SR.DCFEditor_Node_CheckBox);
            } 
 
            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) {
                TemplateField newField = base.GetTemplateField(dataBoundControl); 
                CheckBoxField field = (CheckBoxField)this.RuntimeField;

                newField.SortExpression = field.SortExpression;
                newField.ItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_READONLY)); 
                if (field.ReadOnly == false) {
                    newField.EditItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_EDIT)); 
                } 
                if (dataBoundControl is DetailsView && ((CheckBoxField)RuntimeField).InsertVisible) {
                    newField.InsertItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_INSERT)); 
                }

                return newField;
            } 

            private string GetTemplateContent(int editMode) { 
                StringBuilder sb = new StringBuilder(); 
                Type controlType = typeof(System.Web.UI.WebControls.CheckBox);
 
                if (editMode == MODE_INSERT && !((CheckBoxField)this.RuntimeField).InsertVisible) {
                    return String.Empty;
                }
 
                sb.Append("<asp:");
                sb.Append(controlType.Name); 
                sb.Append(" runat=\"server\""); 

                string dataField = ((CheckBoxField)this.RuntimeField).DataField; 
                if (dataField.Length != 0) {
                    sb.Append(" Checked='<%# ");
                    sb.Append(DesignTimeDataBinding.CreateBindExpression(dataField, String.Empty));
                    sb.Append(" %>'"); 
                    if (editMode == MODE_READONLY) {
                        sb.Append(" Enabled=\"false\""); 
                    } 
                }
 
                sb.Append(" id=\"");
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, editMode));
                sb.Append("\"></asp:");
                sb.Append(controlType.Name); 
                sb.Append(">");
 
                return sb.ToString(); 
            }
        } 

        /// <devdoc>
        ///    Represents a field bound to a datafield.
        /// </devdoc> 
        private class ImageFieldItem : FieldItem {
            public ImageFieldItem(DataControlFieldsEditor fieldsEditor, ImageField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_IMAGE) { 
            } 

            protected override string GetDefaultNodeText() { 
                string dataField = ((ImageField)RuntimeField).DataImageUrlField;
                if ((dataField != null) && (dataField.Length != 0)) {
                    return dataField;
                } 
                return SR.GetString(SR.DCFEditor_Node_Image);
            } 
 
            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) {
                TemplateField newField = base.GetTemplateField(dataBoundControl); 
                ImageField field = (ImageField)this.RuntimeField;

                newField.SortExpression = field.SortExpression;
                newField.ItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_READONLY)); 
                newField.ConvertEmptyStringToNull = field.ConvertEmptyStringToNull;
                if (field.ReadOnly == false) { 
                    newField.EditItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_EDIT)); 
                    if (dataBoundControl is DetailsView) {
                        newField.InsertItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_INSERT)); 
                    }
                }

                return newField; 
            }
 
            private string GetTemplateContent(int editMode) { 
                StringBuilder sb = new StringBuilder();
                Type controlType; 
                string imageUrlField = ((ImageField)this.RuntimeField).DataImageUrlField;
                string altTextValue;

                string altTextField = ((ImageField)this.runtimeField).DataAlternateTextField; 
                if (altTextField.Length > 0) {
                    string altTextFieldFormat = ((ImageField)this.runtimeField).DataAlternateTextFormatString; 
                    altTextValue = "'<%# " + DesignTimeDataBinding.CreateEvalExpression(altTextField, PrepareFormatString(altTextFieldFormat)) + " %>'"; 
                }
                else { 
                    altTextValue = ((ImageField)this.runtimeField).AlternateText;
                }

                if (editMode == MODE_READONLY) { 
                    controlType = typeof(System.Web.UI.WebControls.Image);
                } 
                else { 
                    controlType = typeof(System.Web.UI.WebControls.TextBox);
                } 


                sb.Append("<asp:");
                sb.Append(controlType.Name); 
                sb.Append(" runat=\"server\"");
 
                if (imageUrlField.Length > 0) { 
                    if (controlType == typeof(System.Web.UI.WebControls.Image)) {
                        sb.Append(" ImageUrl='<%# "); 
                        sb.Append(DesignTimeDataBinding.CreateEvalExpression(imageUrlField, PrepareFormatString(((ImageField)this.runtimeField).DataImageUrlFormatString)));
                    }
                    else if (controlType == typeof(System.Web.UI.WebControls.TextBox)) {
                        sb.Append(" Text='<%# "); 
                        sb.Append(DesignTimeDataBinding.CreateEvalExpression(imageUrlField, String.Empty));
                    } 
                    sb.Append(" %>' "); 
                }
 
                if (altTextValue.Length > 0) {
                    if (controlType == typeof(System.Web.UI.WebControls.TextBox)) {
                        sb.Append(" Tooltip=");
                    } 
                    else {
                        sb.Append(" AlternateText="); 
                    } 
                    sb.Append(altTextValue);
                } 

                sb.Append(" id=\"");
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, editMode));
                sb.Append("\"></asp:"); 
                sb.Append(controlType.Name);
                sb.Append(">"); 
 
                return sb.ToString();
            } 
        }

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.HyperLinkFieldItem"]/*' />
        /// <devdoc> 
        ///   Represents a field containing a hyperlink.
        /// </devdoc> 
        private class HyperLinkFieldItem : FieldItem { 
            public HyperLinkFieldItem(DataControlFieldsEditor fieldsEditor, HyperLinkField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_HYPERLINK) {
            } 

            protected override string GetDefaultNodeText() {
                string anchorText = ((HyperLinkField)RuntimeField).Text;
                if ((anchorText != null) && (anchorText.Length != 0)) { 
                    return anchorText;
                } 
                return SR.GetString(SR.DCFEditor_Node_HyperLink); 
            }
 
            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) {
                TemplateField newField = base.GetTemplateField(dataBoundControl);
                HyperLinkField field = (HyperLinkField)RuntimeField;
                Type controlType = typeof(System.Web.UI.WebControls.HyperLink); 

                StringBuilder sb = new StringBuilder(); 
 
                sb.Append("<asp:");
                sb.Append(controlType.Name); 
                sb.Append(" runat=\"server\"");

                if (field.DataTextField.Length != 0) {
                    sb.Append(" Text='<%# "); 
                    sb.Append(DesignTimeDataBinding.CreateEvalExpression(field.DataTextField, PrepareFormatString(field.DataTextFormatString)));
                    sb.Append(" %>'"); 
                } 
                else {
                    sb.Append(" Text=\""); 
                    sb.Append(field.Text);
                    sb.Append("\"");
                }
                if (field.DataNavigateUrlFields.Length != 0 && field.DataNavigateUrlFields[0].Length > 0) { 
                    sb.Append(" NavigateUrl='<%# ");
                    sb.Append(DesignTimeDataBinding.CreateEvalExpression(field.DataNavigateUrlFields[0], PrepareFormatString(field.DataNavigateUrlFormatString))); 
                    sb.Append(" %>'"); 
                }
                else { 
                    sb.Append(" NavigateUrl=\"");
                    sb.Append(field.NavigateUrl);
                    sb.Append("\"");
                } 
                if (field.Target.Length != 0) {
                    sb.Append(" Target=\""); 
                    sb.Append(field.Target); 
                    sb.Append("\"");
                } 

                sb.Append(" id=\"");
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, MODE_READONLY));
                sb.Append("\"></asp:"); 
                sb.Append(controlType.Name);
                sb.Append(">"); 
 
                newField.ItemTemplate = GetTemplate(dataBoundControl, sb.ToString());
 
                return newField;
            }
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.TemplateFieldItem"]/*' />
        /// <devdoc> 
        ///   Represents a field containing a template. 
        /// </devdoc>
        private class TemplateFieldItem : FieldItem { 

            public TemplateFieldItem(DataControlFieldsEditor fieldsEditor, TemplateField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_TEMPLATE) {
            }
 
            protected override string GetDefaultNodeText() {
                return SR.GetString(SR.DCFEditor_Node_Template); 
            } 
        }
 
        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CommandFieldItem"]/*' />
        /// <devdoc>
        ///   Represents a CommandField
        /// </devdoc> 
        private class CommandFieldItem : FieldItem {
 
            public CommandFieldItem(DataControlFieldsEditor fieldsEditor, CommandField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_COMMAND) { 
                UpdateImageIndex();
            } 

            private string BuildButtonString(Type controlType, string buttonText, string commandName, string imageUrl, bool causesValidation, int mode, ref int buttonNameStartIndex) {
                StringBuilder sb = new StringBuilder();
                sb.Append("<asp:"); 
                sb.Append(controlType.Name);
                sb.Append(" runat=\"server\""); 
                sb.Append(" Text=\""); 
                sb.Append(buttonText);
                sb.Append("\""); 
                sb.Append(" CommandName=\"");
                sb.Append(commandName);
                if (imageUrl != null && imageUrl.Length > 0) {
                    sb.Append("\" ImageUrl=\""); 
                    sb.Append(imageUrl);
                } 
                sb.Append("\" CausesValidation=\""); 
                sb.Append(causesValidation.ToString());
                sb.Append("\" id=\""); 
                sb.Append(fieldsEditor.GetNewDataSourceName(controlType, mode, ref buttonNameStartIndex));
                sb.Append("\"></asp:");
                sb.Append(controlType.Name);
                sb.Append(">"); 
                return sb.ToString();
            } 
 
            protected override string GetDefaultNodeText() {
                CommandField field = (CommandField)RuntimeField; 
                if (field.ShowEditButton && !field.ShowDeleteButton && !field.ShowSelectButton && !field.ShowInsertButton) {
                    return SR.GetString(SR.DCFEditor_Node_Edit);
                }
                if (field.ShowDeleteButton && !field.ShowEditButton && !field.ShowSelectButton && !field.ShowInsertButton) { 
                    return SR.GetString(SR.DCFEditor_Node_Delete);
                } 
                if (field.ShowSelectButton && !field.ShowDeleteButton && !field.ShowEditButton && !field.ShowInsertButton) { 
                    return SR.GetString(SR.DCFEditor_Node_Select);
                } 
                if (field.ShowInsertButton && !field.ShowDeleteButton && !field.ShowSelectButton && !field.ShowEditButton) {
                    return SR.GetString(SR.DCFEditor_Node_Insert);
                }
                return SR.GetString(SR.DCFEditor_Node_Command); 
            }
 
            public override TemplateField GetTemplateField(DataBoundControl dataBoundControl) { 
                TemplateField field = base.GetTemplateField(dataBoundControl);
 
                field.ItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_READONLY));
                field.EditItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_EDIT));
                if (dataBoundControl is DetailsView) {
                    field.InsertItemTemplate = GetTemplate(dataBoundControl, GetTemplateContent(MODE_INSERT)); 
                }
 
                return field; 
            }
 
            private string GetTemplateContent(int editMode) {
                StringBuilder sb = new StringBuilder();
                CommandField field = (CommandField)RuntimeField;
                Type controlType = typeof(System.Web.UI.WebControls.Button); 
                int buttonNameStartIndex = 1;
 
                if (field.ButtonType == ButtonType.Link) { 
                    controlType = typeof(System.Web.UI.WebControls.LinkButton);
                } 
                else if (field.ButtonType == ButtonType.Image) {
                    controlType = typeof(System.Web.UI.WebControls.ImageButton);
                }
 
                switch(editMode) {
                    case MODE_EDIT: 
                        if (field.ShowEditButton) { 
                            string updateImageButton = field.ButtonType == ButtonType.Image ? field.UpdateImageUrl : null;
                            sb.Append(BuildButtonString(controlType, field.UpdateText, "Update", updateImageButton, true, MODE_EDIT, ref buttonNameStartIndex)); 
                            buttonNameStartIndex++;
                            if (field.ShowCancelButton) {
                                sb.Append("&nbsp;");
                                string cancelImageButton = field.ButtonType == ButtonType.Image ? field.CancelImageUrl : null; 
                                sb.Append(BuildButtonString(controlType, field.CancelText, "Cancel", cancelImageButton, false, MODE_EDIT, ref buttonNameStartIndex));
                                buttonNameStartIndex++; 
                            } 
                        }
                        break; 
                    case MODE_READONLY:
                        bool isFirstButton = true;
                        if (field.ShowEditButton) {
                            string editImageButton = field.ButtonType == ButtonType.Image ? field.EditImageUrl : null; 
                            sb.Append(BuildButtonString(controlType, field.EditText, "Edit", editImageButton, false, MODE_READONLY, ref buttonNameStartIndex));
                            buttonNameStartIndex++; 
                            isFirstButton = false; 
                        }
                        if (field.ShowInsertButton) { 
                            if (!isFirstButton) {
                                sb.Append("&nbsp;");
                            }
                            string newImageButton = field.ButtonType == ButtonType.Image ? field.NewImageUrl : null; 
                            sb.Append(BuildButtonString(controlType, field.NewText, "New", newImageButton, false, MODE_READONLY, ref buttonNameStartIndex));
                            buttonNameStartIndex++; 
                        } 
                        if (field.ShowSelectButton) {
                            if (!isFirstButton) { 
                                sb.Append("&nbsp;");
                            }
                            string selectImageButton = field.ButtonType == ButtonType.Image ? field.SelectImageUrl : null;
                            sb.Append(BuildButtonString(controlType, field.SelectText, "Select", selectImageButton, false, MODE_READONLY, ref buttonNameStartIndex)); 
                            buttonNameStartIndex++;
                        } 
                        if (field.ShowDeleteButton) { 
                            if (!isFirstButton) {
                                sb.Append("&nbsp;"); 
                            }
                            string deleteImageButton = field.ButtonType == ButtonType.Image ? field.DeleteImageUrl : null;
                            sb.Append(BuildButtonString(controlType, field.DeleteText, "Delete", deleteImageButton, false, MODE_READONLY, ref buttonNameStartIndex));
                            buttonNameStartIndex++; 
                        }
                        break; 
                    case MODE_INSERT: 
                        if (field.ShowInsertButton) {
                            string insertImageButton = field.ButtonType == ButtonType.Image ? field.InsertImageUrl : null; 
                            sb.Append(BuildButtonString(controlType, field.InsertText, "Insert", insertImageButton, true, MODE_INSERT, ref buttonNameStartIndex));
                            buttonNameStartIndex++;
                            if (field.ShowCancelButton) {
                                sb.Append("&nbsp;"); 
                                string cancelImageButton = field.ButtonType == ButtonType.Image ? field.CancelImageUrl : null;
                                sb.Append(BuildButtonString(controlType, field.CancelText, "Cancel", cancelImageButton, false, MODE_INSERT, ref buttonNameStartIndex)); 
                                buttonNameStartIndex++; 
                            }
                        } 
                        break;
                }
                return sb.ToString();
            } 

            public void UpdateImageIndex() { 
                CommandField runtimeField = (CommandField)RuntimeField; 

                if (runtimeField.ShowEditButton && !runtimeField.ShowDeleteButton && !runtimeField.ShowSelectButton && !runtimeField.ShowInsertButton) { 
                    ImageIndex = ILI_EDITBUTTON;
                }
                else if (runtimeField.ShowDeleteButton && !runtimeField.ShowEditButton && !runtimeField.ShowSelectButton && !runtimeField.ShowInsertButton) {
                    ImageIndex = ILI_DELETEBUTTON; 
                }
                else if (runtimeField.ShowSelectButton && !runtimeField.ShowDeleteButton && !runtimeField.ShowEditButton && !runtimeField.ShowInsertButton) { 
                    ImageIndex = ILI_SELECTBUTTON; 
                }
                else if (runtimeField.ShowInsertButton && !runtimeField.ShowDeleteButton && !runtimeField.ShowSelectButton && !runtimeField.ShowEditButton) { 
                    ImageIndex = ILI_INSERTBUTTON;
                }
                else {
                    ImageIndex = ILI_COMMAND; 
                }
            } 
        } 

        private class TreeViewWithEnter : TreeView { 
            protected override bool IsInputKey(Keys keyCode) {
                if (keyCode == Keys.Enter) {
                    return true;
                } 
                return base.IsInputKey(keyCode);
            } 
        } 

        private class ListViewWithEnter : ListView { 
            protected override bool IsInputKey(Keys keyCode) {
                if (keyCode == Keys.Enter) {
                    return true;
                } 
                return base.IsInputKey(keyCode);
            } 
        } 

        /// <include file='doc\DataControlFieldsEditor.uex' path='docs/doc[@for="DataControlFieldsEditor.CustomFieldItem"]/*' /> 
        /// <devdoc>
        ///   Represents a field of an unknown/custom type.
        /// </devdoc>
        private class CustomFieldItem : FieldItem { 

            public CustomFieldItem(DataControlFieldsEditor fieldsEditor, DataControlField runtimeField) : base(fieldsEditor, runtimeField, DataControlFieldsEditor.ILI_CUSTOM) { 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
