//------------------------------------------------------------------------------ 
// <copyright file="DataBindingsDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.Text; 
    using System.Text.RegularExpressions;
    using System.Web.UI; 
    using System.Web.UI.Design.Util;
    //
    using System.Web.UI.Design.WebControls;
    using System.Windows.Forms; 

    using WebUIControl = System.Web.UI.Control; 
 
    internal sealed class DataBindingsDialog : DesignerForm {
 
        private static readonly Attribute[] BrowsablePropertiesFilter =
            new Attribute[] { BrowsableAttribute.Yes, ReadOnlyAttribute.No};
        private static readonly Attribute[] BindablePropertiesFilter =
            new Attribute[] { BindableAttribute.Yes, ReadOnlyAttribute.No}; 

        private const int UnboundImageIndex = 0; 
        private const int BoundImageIndex = 1; 
        private const int TwoWayBoundImageIndex = 2;
 
        private const int UnboundItemIndex = 0;

        private System.Windows.Forms.Label _instructionLabel;
        private System.Windows.Forms.Label _bindablePropsLabels; 
        private System.Windows.Forms.TreeView _bindablePropsTree;
        private System.Windows.Forms.CheckBox _allPropsCheckBox; 
        private Label _bindingLabel; 
        private System.Windows.Forms.RadioButton _fieldBindingRadio;
        private System.Windows.Forms.Label _fieldLabel; 
        private System.Windows.Forms.ComboBox _fieldCombo;
        private System.Windows.Forms.Label _formatLabel;
        private System.Windows.Forms.Label _sampleLabel;
        private System.Windows.Forms.TextBox _sampleTextBox; 
        private System.Windows.Forms.RadioButton _exprBindingRadio;
        private System.Windows.Forms.TextBox _exprTextBox; 
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.ComboBox _formatCombo; 
        private System.Windows.Forms.Label _exprLabel;
        private System.Windows.Forms.LinkLabel _refreshSchemaLink;
        private System.Windows.Forms.CheckBox _twoWayBindingCheckBox;
        private System.Windows.Forms.Panel _fieldBindingPanel; 
        private System.Windows.Forms.Panel _customBindingPanel;
        private System.Windows.Forms.Panel _bindingOptionsPanel; 
 
        private string _controlID;
        private IDictionary _bindings; 
        private bool _bindingsDirty;

        private bool _fieldsAvailable;
 
        private BindablePropertyNode _currentNode;
        private DesignTimeDataBinding _currentDataBinding; 
        private bool _currentDataBindingDirty; 

        private bool _internalChange; 
        private bool _formatDirty;
        private object _formatSampleObject;

        public DataBindingsDialog(IServiceProvider serviceProvider, WebUIControl control) : base(serviceProvider) { 
            Debug.Assert(control != null);
            Debug.Assert(control.Site != null); 
 
            _controlID = control.ID;
 
            // Setup the user interface
            InitializeComponent();
            InitializeUserInterface();
        } 

        private WebUIControl Control { 
            get { 
                IServiceProvider serviceProvider = ServiceProvider;
                Debug.Assert(serviceProvider != null); 
                if (serviceProvider != null) {
                    IContainer container = null;
                    ISite nestedSite = serviceProvider as ISite;
                    IContainer nestedContainer = null; 
                    if (nestedSite != null) {
                        nestedContainer = nestedSite.Container; 
                    } 

                    if (nestedContainer != null && nestedContainer is NestedContainer) { 
                        container = nestedContainer;
                    }
                    else {
                        container = ((IContainer)serviceProvider.GetService(typeof(IContainer))); 
                    }
                    Debug.Assert(container != null); 
                    if (container != null) { 
                        WebUIControl control = container.Components[_controlID] as WebUIControl;
                        Debug.Assert(control != null); 
                        return control;
                    }
                }
                Debug.Fail("Could not get Control"); 
                return null;
            } 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.DataBinding.BindingsDialog";
            }
        } 

        private bool ContainingTemplateIsBindable(ControlDesigner designer) { 
            bool bindable = false; 
            IControlDesignerView view = designer.View;
            if (view != null) { 
                TemplatedEditableDesignerRegion editableRegion = view.ContainingRegion as TemplatedEditableDesignerRegion;
                if (editableRegion != null) {
                    TemplateDefinition templateDefinition = editableRegion.TemplateDefinition;
                    PropertyDescriptor prop = TypeDescriptor.GetProperties(templateDefinition.TemplatedObject)[templateDefinition.TemplatePropertyName]; 
                    if (prop != null) {
                        TemplateContainerAttribute containerAttr = prop.Attributes[typeof(TemplateContainerAttribute)] as TemplateContainerAttribute; 
                        if (containerAttr != null && (containerAttr.BindingDirection == BindingDirection.TwoWay)) { 
                            bindable = true;
                        } 
                    }
                }
            }
            return bindable; 
        }
 
        private void ExtractFields(IDataSourceViewSchema schema, ArrayList fields) { 
            if (schema != null) {
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields(); 
                if (fieldSchemas != null) {
                    for (int i = 0; i < fieldSchemas.Length; i++) {
                        fields.Add(new FieldItem(fieldSchemas[i].Name, fieldSchemas[i].DataType));
                    } 
                }
            } 
        } 

        private void ExtractFields(IDataSourceProvider dataSourceProvider, ArrayList fields) { 
            Debug.Assert(dataSourceProvider != null);

            IEnumerable dataSource = dataSourceProvider.GetResolvedSelectedDataSource();
            if (dataSource != null) { 
                PropertyDescriptorCollection props = DesignTimeData.GetDataFields(dataSource);
                if ((props != null) && (props.Count != 0)) { 
                    foreach (PropertyDescriptor propDesc in props) { 
                        fields.Add(new FieldItem(propDesc.Name, propDesc.PropertyType));
                    } 
                }
            }
        }
 
        private IDesigner GetNamingContainerDesigner(ControlDesigner designer) {
            IControlDesignerView view = designer.View; 
            return (view != null) ? view.NamingContainerDesigner : null; 
        }
 
        #region Windows Form Designer generated code
        private void InitializeComponent() {
            this._instructionLabel = new System.Windows.Forms.Label();
            this._bindablePropsLabels = new System.Windows.Forms.Label(); 
            this._bindablePropsTree = new System.Windows.Forms.TreeView();
            this._allPropsCheckBox = new System.Windows.Forms.CheckBox(); 
            this._bindingLabel = new Label(); 
            this._fieldBindingRadio = new System.Windows.Forms.RadioButton();
            this._fieldLabel = new System.Windows.Forms.Label(); 
            this._fieldCombo = new System.Windows.Forms.ComboBox();
            this._formatLabel = new System.Windows.Forms.Label();
            this._formatCombo = new System.Windows.Forms.ComboBox();
            this._sampleLabel = new System.Windows.Forms.Label(); 
            this._sampleTextBox = new System.Windows.Forms.TextBox();
            this._exprBindingRadio = new System.Windows.Forms.RadioButton(); 
            this._exprTextBox = new System.Windows.Forms.TextBox(); 
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button(); 
            this._refreshSchemaLink = new System.Windows.Forms.LinkLabel();
            this._exprLabel = new System.Windows.Forms.Label();
            this._twoWayBindingCheckBox = new System.Windows.Forms.CheckBox();
            this._fieldBindingPanel = new System.Windows.Forms.Panel(); 
            this._customBindingPanel = new System.Windows.Forms.Panel();
            this._bindingOptionsPanel = new System.Windows.Forms.Panel(); 
            this.SuspendLayout(); 
            //
            // _instructionLabel 
            //
            this._instructionLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._instructionLabel.Location = new System.Drawing.Point(12, 12);
            this._instructionLabel.Name = "_instructionLabel"; 
            this._instructionLabel.Size = new System.Drawing.Size(508, 30);
            this._instructionLabel.TabIndex = 0; 
            // 
            // _bindablePropsLabels
            // 
            this._bindablePropsLabels.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._bindablePropsLabels.Location = new System.Drawing.Point(12, 52);
            this._bindablePropsLabels.Name = "_bindablePropsLabels";
            this._bindablePropsLabels.Size = new System.Drawing.Size(184, 16); 
            this._bindablePropsLabels.TabIndex = 1;
            // 
            // _bindablePropsTree 
            //
            this._bindablePropsTree.HideSelection = false; 
            this._bindablePropsTree.ImageIndex = -1;
            this._bindablePropsTree.Location = new System.Drawing.Point(12, 72);
            this._bindablePropsTree.Name = "_bindablePropsTree";
            this._bindablePropsTree.SelectedImageIndex = -1; 
            this._bindablePropsTree.ShowLines = false;
            this._bindablePropsTree.ShowPlusMinus = false; 
            this._bindablePropsTree.ShowRootLines = false; 
            this._bindablePropsTree.Size = new System.Drawing.Size(184, 112);
            this._bindablePropsTree.TabIndex = 2; 
            this._bindablePropsTree.Sorted = true;
            this._bindablePropsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OnBindablePropsTreeAfterSelect);
            //
            // _allPropsCheckBox 
            //
            this._allPropsCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._allPropsCheckBox.Location = new System.Drawing.Point(12, 190); 
            this._allPropsCheckBox.Name = "_allPropsCheckBox";
            this._allPropsCheckBox.Size = new System.Drawing.Size(184, 40); 
            this._allPropsCheckBox.TabIndex = 3;
            this._allPropsCheckBox.Visible = true;
            this._allPropsCheckBox.CheckedChanged += new System.EventHandler(this.OnShowAllCheckedChanged);
            this._allPropsCheckBox.TextAlign = ContentAlignment.TopLeft; 
            this._allPropsCheckBox.CheckAlign = ContentAlignment.TopLeft;
            // 
            // _bindingLabel 
            //
            this._bindingLabel.Location = new System.Drawing.Point(210, 52); 
            this._bindingLabel.Name = "_bindingGroupLabel";
            this._bindingLabel.Size = new System.Drawing.Size(306, 16);
            this._bindingLabel.TabIndex = 4;
            // 
            // _fieldLabel
            // 
            this._fieldLabel.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._fieldLabel.Location = new System.Drawing.Point(0, 4);
            this._fieldLabel.Name = "_fieldLabel"; 
            this._fieldLabel.Size = new System.Drawing.Size(104, 16);
            this._fieldLabel.TabIndex = 100;
            //
            // _fieldCombo 
            //
            this._fieldCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._fieldCombo.Location = new System.Drawing.Point(118, 0); 
            this._fieldCombo.Name = "_fieldCombo";
            this._fieldCombo.Size = new System.Drawing.Size(164, 21); 
            this._fieldCombo.TabIndex = 101;
            this._fieldCombo.SelectedIndexChanged += new System.EventHandler(this.OnFieldComboSelectedIndexChanged);
            //
            // _formatLabel 
            //
            this._formatLabel.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._formatLabel.Location = new System.Drawing.Point(0, 32); 
            this._formatLabel.Name = "_formatLabel";
            this._formatLabel.Size = new System.Drawing.Size(114, 16); 
            this._formatLabel.TabIndex = 102;
            //
            // _formatCombo
            // 
            this._formatCombo.Location = new System.Drawing.Point(118, 28);
            this._formatCombo.Name = "_formatCombo"; 
            this._formatCombo.Size = new System.Drawing.Size(164, 21); 
            this._formatCombo.TabIndex = 103;
            this._formatCombo.LostFocus += new System.EventHandler(this.OnFormatComboLostFocus); 
            this._formatCombo.TextChanged += new System.EventHandler(this.OnFormatComboTextChanged);
            this._formatCombo.SelectedIndexChanged += new System.EventHandler(this.OnFormatComboSelectedIndexChanged);
            //
            // _sampleLabel 
            //
            this._sampleLabel.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._sampleLabel.Location = new System.Drawing.Point(0, 60); 
            this._sampleLabel.Name = "_sampleLabel";
            this._sampleLabel.Size = new System.Drawing.Size(114, 16); 
            this._sampleLabel.TabIndex = 104;
            //
            // _sampleTextBox
            // 
            this._sampleTextBox.Location = new System.Drawing.Point(118, 56);
            this._sampleTextBox.Name = "_sampleTextBox"; 
            this._sampleTextBox.ReadOnly = true; 
            this._sampleTextBox.Size = new System.Drawing.Size(164, 20);
            this._sampleTextBox.TabIndex = 105; 
            //
            // _exprTextBox
            //
            this._exprTextBox.Location = new System.Drawing.Point(0, 18); 
            this._exprTextBox.Name = "_exprTextBox";
            this._exprTextBox.Size = new System.Drawing.Size(282, 20); 
            this._exprTextBox.TabIndex = 201; 
            this._exprTextBox.TextChanged += new System.EventHandler(this.OnExprTextBoxTextChanged);
            // 
            // _okButton
            //
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(360, 279); 
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 7; 
            this._okButton.Click += new System.EventHandler(this.OnOKButtonClick); 
            //
            // _cancelButton 
            //
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(441, 279); 
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 8; 
            // 
            // _refreshSchemaLink
            // 
            this._refreshSchemaLink.Visible = false;
            this._refreshSchemaLink.Location = new System.Drawing.Point(12, 283);
            this._refreshSchemaLink.Name = "_refreshSchemaLink";
            this._refreshSchemaLink.Size = new System.Drawing.Size(197, 16); 
            this._refreshSchemaLink.TabIndex = 6;
            this._refreshSchemaLink.TabStop = true; 
            this._refreshSchemaLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnRefreshSchemaLinkLinkClicked); 
            //
            // _exprLabel 
            //
            this._exprLabel.Location = new System.Drawing.Point(0, 0);
            this._exprLabel.Name = "_exprLabel";
            this._exprLabel.Size = new System.Drawing.Size(290, 16); 
            this._exprLabel.TabIndex = 200;
            // 
            // _twoWayBindingCheckBox 
            //
            this._twoWayBindingCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._twoWayBindingCheckBox.Location = new System.Drawing.Point(118, 83);
            this._twoWayBindingCheckBox.Name = "_twoWayBindingCheckBox";
            this._twoWayBindingCheckBox.Size = new System.Drawing.Size(168, 30);
            this._twoWayBindingCheckBox.TabIndex = 106; 
            this._twoWayBindingCheckBox.Enabled = true;
            this._twoWayBindingCheckBox.CheckedChanged += new System.EventHandler(this.OnTwoWayBindingChecked); 
            this._twoWayBindingCheckBox.TextAlign = ContentAlignment.TopLeft; 
            this._twoWayBindingCheckBox.CheckAlign = ContentAlignment.TopLeft;
            // 
            // _fieldBindingRadio
            //
            this._fieldBindingRadio.Checked = true;
            this._fieldBindingRadio.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._fieldBindingRadio.Location = new System.Drawing.Point(0, 0);
            this._fieldBindingRadio.Name = "_fieldBindingRadio"; 
            this._fieldBindingRadio.Size = new System.Drawing.Size(302, 18); 
            this._fieldBindingRadio.TabIndex = 0;
            this._fieldBindingRadio.TabStop = true; 
            this._fieldBindingRadio.CheckedChanged += new System.EventHandler(this.OnFieldBindingRadioCheckedChanged);
            //
            // _exprBindingRadio
            // 
            this._exprBindingRadio.Location = new System.Drawing.Point(0, 127);
            this._exprBindingRadio.Name = "_exprBindingRadio"; 
            this._exprBindingRadio.Size = new System.Drawing.Size(302, 18); 
            this._exprBindingRadio.TabIndex = 2;
            this._exprBindingRadio.CheckedChanged += new System.EventHandler(this.OnExprBindingRadioCheckedChanged); 
            //
            // _fieldBindingPanel
            //
            this._fieldBindingPanel.TabIndex = 1; 
            this._fieldBindingPanel.Name = "_fieldBindingPanel";
            this._fieldBindingPanel.Location = new System.Drawing.Point(16, 20); 
            this._fieldBindingPanel.Size = new System.Drawing.Size(286, 105); 
            this._fieldBindingPanel.Controls.Add(this._fieldLabel);
            this._fieldBindingPanel.Controls.Add(this._fieldCombo); 
            this._fieldBindingPanel.Controls.Add(this._formatLabel);
            this._fieldBindingPanel.Controls.Add(this._formatCombo);
            this._fieldBindingPanel.Controls.Add(this._sampleLabel);
            this._fieldBindingPanel.Controls.Add(this._sampleTextBox); 
            this._fieldBindingPanel.Controls.Add(this._twoWayBindingCheckBox);
            // 
            // _customBindingPanel 
            //
            this._customBindingPanel.TabIndex = 3; 
            this._customBindingPanel.Name = "_customBindingPanel";
            this._customBindingPanel.Location = new System.Drawing.Point(16, 148);
            this._customBindingPanel.Size = new System.Drawing.Size(286, 54);
            this._customBindingPanel.Controls.Add(this._exprLabel); 
            this._customBindingPanel.Controls.Add(this._exprTextBox);
            // 
            // DataBindingsDialog 
            //
            this._bindingOptionsPanel.TabIndex = 5; 
            this._bindingOptionsPanel.Name = "_bindingOptionsPanel";
            this._bindingOptionsPanel.Location = new System.Drawing.Point(214, 76);
            this._bindingOptionsPanel.Size = new System.Drawing.Size(302, 200);
            this._bindingOptionsPanel.Controls.Add(this._fieldBindingRadio); 
            this._bindingOptionsPanel.Controls.Add(this._exprBindingRadio);
            this._bindingOptionsPanel.Controls.Add(this._fieldBindingPanel); 
            this._bindingOptionsPanel.Controls.Add(this._customBindingPanel); 

            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(524, 314);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          this._refreshSchemaLink, 
                                                                          this._cancelButton,
                                                                          this._okButton, 
                                                                          this._bindingLabel, 
                                                                          this._allPropsCheckBox,
                                                                          this._bindablePropsTree, 
                                                                          this._bindablePropsLabels,
                                                                          this._instructionLabel,
                                                                          this._bindingOptionsPanel});
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; 
            this.Name = "DataBindingsDialog";
 
            InitializeForm(); 

            this.ResumeLayout(false); 

        }
        #endregion
 
        private void InitializeUserInterface() {
            Text = SR.GetString(SR.DBDlg_Text, Control.Site.Name); 
 
            _instructionLabel.Text = SR.GetString(SR.DBDlg_Inst);
            _bindablePropsLabels.Text = SR.GetString(SR.DBDlg_BindableProps); 
            _allPropsCheckBox.Text = SR.GetString(SR.DBDlg_ShowAll);
            _fieldBindingRadio.Text = SR.GetString(SR.DBDlg_FieldBinding);
            _fieldLabel.Text = SR.GetString(SR.DBDlg_Field);
            _formatLabel.Text = SR.GetString(SR.DBDlg_Format); 
            _sampleLabel.Text = SR.GetString(SR.DBDlg_Sample);
            _exprBindingRadio.Text = SR.GetString(SR.DBDlg_CustomBinding); 
            _okButton.Text = SR.GetString(SR.DBDlg_OK); 
            _cancelButton.Text = SR.GetString(SR.DBDlg_Cancel);
            _refreshSchemaLink.Text = SR.GetString(SR.DBDlg_RefreshSchema); 
            _exprLabel.Text = SR.GetString(SR.DBDlg_Expr);
            _twoWayBindingCheckBox.Text = SR.GetString(SR.DBDlg_TwoWay);

            ImageList imageList = new ImageList(); 
            imageList.TransparentColor = Color.Magenta;
            imageList.ColorDepth = ColorDepth.Depth32Bit; 
            imageList.Images.AddStrip(new Bitmap(typeof(DataBindingsDialog), "BindableProperties.bmp")); 
            _bindablePropsTree.ImageList = imageList;
 
            bool showBindingCheckBox = false;
            IDesignerHost designerHost = (IDesignerHost)Control.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null);
            if (designerHost != null) { 
                ControlDesigner designer = designerHost.GetDesigner(Control) as ControlDesigner;
                if (designer != null) { 
                    showBindingCheckBox = ContainingTemplateIsBindable(designer); 
                }
            } 
            _twoWayBindingCheckBox.Visible = showBindingCheckBox;
        }

        private void LoadBindableProperties(bool showAll) { 
            string previouslySelectedProp = String.Empty;
            if (_bindablePropsTree.SelectedNode != null) { 
                previouslySelectedProp = _bindablePropsTree.SelectedNode.Text; 
            }
            _bindablePropsTree.Nodes.Clear(); 

            PropertyDescriptorCollection propDescs = TypeDescriptor.GetProperties(Control.GetType(), BindablePropertiesFilter);
            if (showAll) {
                PropertyDescriptorCollection browsablePropDescs = TypeDescriptor.GetProperties(Control.GetType(), BrowsablePropertiesFilter); 
                if (browsablePropDescs != null && browsablePropDescs.Count > 0) {
                    int bindableCount = propDescs.Count; 
                    int browsableCount = browsablePropDescs.Count; 
                    PropertyDescriptor[] allProps = new PropertyDescriptor[bindableCount + browsableCount];
                    propDescs.CopyTo(allProps, 0); 

                    int currentPropIndex = bindableCount;
                    // Merge collections, ignoring repeats.  PropertyDescriptorCollection.Add isn't implemented.
                    foreach (PropertyDescriptor pd in browsablePropDescs) { 
                        if (!propDescs.Contains(pd) && !String.Equals(pd.Name, "id", StringComparison.OrdinalIgnoreCase)) {
                            allProps[currentPropIndex++] = pd; 
                        } 
                    }
                    PropertyDescriptor[] allPropsFinalCount = new PropertyDescriptor[currentPropIndex]; 
                    Array.Copy(allProps, allPropsFinalCount, currentPropIndex);
                    propDescs = new PropertyDescriptorCollection(allPropsFinalCount);
                }
            } 

            string defaultPropName = null; 
            ControlValuePropertyAttribute controlValuePropertyAttr = TypeDescriptor.GetAttributes(Control)[typeof(ControlValuePropertyAttribute)] as ControlValuePropertyAttribute; 
            if (controlValuePropertyAttr != null) {
                defaultPropName = controlValuePropertyAttr.Name; 
            }
            else {
                PropertyDescriptor defaultProperty = TypeDescriptor.GetDefaultProperty(Control);
                if (defaultProperty != null) { 
                    defaultPropName = defaultProperty.Name;
                } 
            } 

            TreeNodeCollection nodes = _bindablePropsTree.Nodes; 
            TreeNode defaultNode = null;
            TreeNode selectedNode = null;

            _bindablePropsTree.BeginUpdate(); 

            foreach (PropertyDescriptor pd in propDescs) { 
                bool bound = (_bindings[pd.Name] != null); 
                BindingMode bindingMode = BindingMode.NotSet;
                if (bound) { 
                    if (((DesignTimeDataBinding)_bindings[pd.Name]).IsTwoWayBound) {
                        bindingMode = BindingMode.TwoWay;
                    }
                    else { 
                        bindingMode = BindingMode.OneWay;
                    } 
                } 

                TreeNode node = new BindablePropertyNode(pd, bindingMode); 

                if (pd.Name.Equals(defaultPropName)) {
                    defaultNode = node;
                } 
                if (pd.Name.Equals(previouslySelectedProp)) {
                    selectedNode = node; 
                } 

                nodes.Add(node); 
            }

            _bindablePropsTree.EndUpdate();
 
            if (selectedNode == null && defaultNode == null && nodes.Count != 0) {
                int nodeCount = nodes.Count; 
                for (int i = 0; i < nodeCount; i++) { 
                    BindablePropertyNode node = (BindablePropertyNode)nodes[i];
                    if (node.IsBound) { 
                        selectedNode = node;
                        break;
                    }
                } 
                if (selectedNode == null) {
                    selectedNode = nodes[0]; 
                } 
            }
            if (selectedNode != null) { 
                _bindablePropsTree.SelectedNode = selectedNode;
            }
            else if (defaultNode != null) {
                _bindablePropsTree.SelectedNode = defaultNode; 
            }
 
 
            UpdateUIState();
        } 

        /// <devdoc>
        /// </devdoc>
        private void LoadCurrentDataBinding() { 
            Debug.Assert(_currentDataBindingDirty == false, "Must save pending changes first.");
 
            _internalChange = true; 
            try {
                // first initialize the UI state 
                _fieldBindingRadio.Checked = _fieldsAvailable;
                _bindingLabel.Text = String.Empty;
                _fieldCombo.SelectedIndex = -1;
                _formatCombo.Text = String.Empty; 
                _sampleTextBox.Text = String.Empty;
                _exprBindingRadio.Checked = !_fieldsAvailable; 
                _exprTextBox.Text = String.Empty; 
                _twoWayBindingCheckBox.Checked = false;
 
                _formatDirty = false;

                if (_currentNode != null) {
                    // load the current selected property 
                    _bindingLabel.Text = SR.GetString(SR.DBDlg_BindingGroup, _currentNode.PropertyDescriptor.Name);
 
                    _twoWayBindingCheckBox.Checked = _currentNode.TwoWayBoundByDefault && _twoWayBindingCheckBox.Visible; 
                    if (_currentDataBinding != null) {
                        // load the databinding if there is one associated with the property 
                        bool useExpression = true;

                        if (_fieldsAvailable && (_currentDataBinding.IsCustom == false)) {
                            string field = _currentDataBinding.Field; 
                            string format = _currentDataBinding.Format;
 
                            Debug.Assert(_fieldCombo.Items.Count > 1); 

                            // strip off surrounding square brackets, if they exist 
                            field = field.TrimStart(new char[] {'['});
                            field = field.TrimEnd(new char[] {']'});

                            // this finds the field in a case-insensitive manner 
                            int fieldIndex = _fieldCombo.FindStringExact(field, 1);
 
                            if (fieldIndex != -1) { 
                                useExpression = false;
 
                                _fieldCombo.SelectedIndex = fieldIndex;

                                UpdateFormatItems();
 
                                bool knownFormat = false;
                                foreach (FormatItem item in _formatCombo.Items) { 
                                    if (item.Format.Equals(format)) { 
                                        knownFormat = true;
                                        _formatCombo.SelectedItem = item; 
                                    }
                                }
                                if (knownFormat == false) {
                                    _formatCombo.Text = format; 
                                }
 
                                UpdateFormatSample(); 
                                if (_currentNode.BindingMode == BindingMode.TwoWay) {
                                    _twoWayBindingCheckBox.Checked = true; 
                                }
                                else if (_currentNode.BindingMode == BindingMode.OneWay) {
                                    _twoWayBindingCheckBox.Checked = false;
                                } 
                            }
                        } 
 
                        if (useExpression) {
                            // either it was a custom expression or we're falling back 
                            // on this because of an error such as an unknown field.

                            _exprBindingRadio.Checked = true;
                            _exprTextBox.Text = _currentDataBinding.Expression; 
                        }
                        else { 
                            UpdateExpression(); 
                        }
                    } 
                }
            }
            finally {
                _internalChange = false; 

                UpdateUIState(); 
            } 
        }
 
        /// <devdoc>
        /// </devdoc>
        private void LoadDataBindings() {
            // Load the current bindings 
            _bindings = new Hashtable();
 
            DataBindingCollection currentDataBindings = ((IDataBindingsAccessor)Control).DataBindings; 
            foreach (DataBinding binding in currentDataBindings) {
                _bindings[binding.PropertyName] = new DesignTimeDataBinding(binding); 
            }
        }

        /// <devdoc> 
        /// </devdoc>
        private void LoadFields() { 
            // This can get called multiple times, for example whenever refresh schema is clicked 
            // Therefore clear the combobox first
            _fieldCombo.Items.Clear(); 

            ArrayList fields = new ArrayList();

            // Add the (Unbound) field item 
            fields.Add(new FieldItem());
 
            IDesigner containerControlDesigner = null; 

            IDesignerHost designerHost = (IDesignerHost)Control.Site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);
            if (designerHost != null) {
                ControlDesigner designer = designerHost.GetDesigner(Control) as ControlDesigner;
                if (designer != null) { 
                    containerControlDesigner = GetNamingContainerDesigner(designer);
                } 
            } 

            if (containerControlDesigner != null) { 
                IDataBindingSchemaProvider schemaProvider = containerControlDesigner as IDataBindingSchemaProvider;
                if (schemaProvider != null) {
                    if (schemaProvider.CanRefreshSchema) {
                        _refreshSchemaLink.Visible = true; 
                    }
 
                    IDataSourceViewSchema schema = null; 
                    try {
                        schema = schemaProvider.Schema; 
                    }
                    catch (Exception ex) {
                        IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)ServiceProvider.GetService(typeof(IComponentDesignerDebugService));
                        if (debugService != null) { 
                            debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message));
                        } 
                    } 

                    ExtractFields(schema, fields); 
                }
                else if (containerControlDesigner is IDataSourceProvider) {
                    ExtractFields((IDataSourceProvider)containerControlDesigner, fields);
                } 
            }
 
            _fieldCombo.Items.AddRange(fields.ToArray()); 
            _fieldsAvailable = (fields.Count > 1);
        } 

        private void OnBindablePropsTreeAfterSelect(object sender, TreeViewEventArgs e) {
            if (_currentDataBindingDirty) {
                SaveCurrentDataBinding(); 
            }
 
            _currentDataBinding = null; 
            _currentNode = (BindablePropertyNode)_bindablePropsTree.SelectedNode;
 
            if (_currentNode != null) {
                _currentDataBinding = (DesignTimeDataBinding)_bindings[_currentNode.PropertyDescriptor.Name];
            }
 
            LoadCurrentDataBinding();
        } 
 
        private void OnExprBindingRadioCheckedChanged(object sender, System.EventArgs e) {
            if (_internalChange) { 
                return;
            }

            _currentDataBindingDirty = true; 
            UpdateUIState();
        } 
 
        private void OnExprTextBoxTextChanged(object sender, System.EventArgs e) {
            if (_internalChange) { 
                return;
            }

            _currentDataBindingDirty = true; 
        }
 
        private void OnFieldBindingRadioCheckedChanged(object sender, System.EventArgs e) { 
            if (_internalChange) {
                return; 
            }

            _currentDataBindingDirty = true;
            if (_fieldBindingRadio.Checked) { 
                UpdateExpression();
            } 
            UpdateUIState(); 
        }
 
        private void OnFieldComboSelectedIndexChanged(object sender, EventArgs e) {
            if (_internalChange) {
                return;
            } 

            _currentDataBindingDirty = true; 
            UpdateFormatItems(); 
            UpdateExpression();
            UpdateUIState(); 
        }

        private void OnFormatComboLostFocus(object sender, EventArgs e) {
            if (_formatDirty) { 
                _formatDirty = false;
 
                UpdateFormatSample(); 
                UpdateExpression();
            } 
        }

        private void OnFormatComboTextChanged(object sender, EventArgs e) {
            if (_internalChange) { 
                return;
            } 
 
            _formatDirty = true;
        } 

        private void OnFormatComboSelectedIndexChanged(object sender, EventArgs e) {
            if (_internalChange) {
                return; 
            }
 
            _formatDirty = true; 
            UpdateFormatSample();
            UpdateExpression(); 
        }

        protected override void OnInitialActivated(EventArgs e) {
            base.OnInitialActivated(e); 

            LoadDataBindings(); 
            LoadFields(); 
            LoadBindableProperties(false);
        } 

        private void OnOKButtonClick(object sender, EventArgs e) {
            if (_currentDataBindingDirty) {
                SaveCurrentDataBinding(); 
            }
 
            if (_bindingsDirty) { 
                SaveDataBindings();
            } 

            DialogResult = DialogResult.OK;
            Close();
        } 

        private void OnRefreshSchemaLinkLinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { 
            if (_currentDataBindingDirty) { 
                SaveCurrentDataBinding();
            } 

            IDesigner containerControlDesigner = null;

            IDesignerHost designerHost = (IDesignerHost)Control.Site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);
            if (designerHost != null) { 
                ControlDesigner designer = designerHost.GetDesigner(Control) as ControlDesigner; 
                if (designer != null) {
                    containerControlDesigner = GetNamingContainerDesigner(designer); 
                }
            }

            if (containerControlDesigner != null) { 
                IDataBindingSchemaProvider schemaProvider = containerControlDesigner as IDataBindingSchemaProvider;
                if (schemaProvider != null) { 
                    schemaProvider.RefreshSchema(false); 
                }
            } 

            LoadFields();
            if (_currentNode != null) {
                _currentDataBinding = (DesignTimeDataBinding)_bindings[_currentNode.PropertyDescriptor.Name]; 
            }
            LoadCurrentDataBinding(); 
        } 

        private void OnShowAllCheckedChanged(object sender, EventArgs e) { 
            LoadBindableProperties(_allPropsCheckBox.Checked);
        }

        private void OnTwoWayBindingChecked(object sender, EventArgs e) { 
            if (_internalChange) {
                return; 
            } 

            _currentDataBindingDirty = true; 
            UpdateExpression();
            UpdateUIState();
        }
 
        private void SaveCurrentDataBinding() {
            Debug.Assert(_currentDataBindingDirty); 
            Debug.Assert(_currentNode != null); 

            DesignTimeDataBinding binding = null; 
            if (_fieldBindingRadio.Checked) {
                if (_fieldCombo.SelectedIndex > DataBindingsDialog.UnboundItemIndex) {
                    string fieldName = _fieldCombo.Text;
                    string format = SaveFormat(); 
                    binding = new DesignTimeDataBinding(_currentNode.PropertyDescriptor, fieldName, format, _twoWayBindingCheckBox.Checked);
                } 
            } 
            else {
                string expression = _exprTextBox.Text.Trim(); 
                if (expression.Length != 0) {
                    binding = new DesignTimeDataBinding(_currentNode.PropertyDescriptor, expression);
                }
            } 

            if (binding == null) { 
                _currentNode.BindingMode = BindingMode.NotSet; 
                _bindings.Remove(_currentNode.PropertyDescriptor.Name);
            } 
            else {
                if (_fieldBindingRadio.Checked) {
                    if (_twoWayBindingCheckBox.Checked && _twoWayBindingCheckBox.Visible) {
                        _currentNode.BindingMode =  BindingMode.TwoWay; 
                    }
                    else { 
                        _currentNode.BindingMode =  BindingMode.OneWay; 
                    }
                } 
                else {
                    if (binding.IsTwoWayBound) {
                        _currentNode.BindingMode =  BindingMode.TwoWay;
                    } 
                    else {
                        _currentNode.BindingMode =  BindingMode.OneWay; 
                    } 
                }
                _bindings[_currentNode.PropertyDescriptor.Name] = binding; 
            }

            _currentDataBindingDirty = false;
            _bindingsDirty = true; 
        }
 
        private void SaveDataBindings() { 
            Debug.Assert(_bindingsDirty == true);
 
            DataBindingCollection dbc = ((IDataBindingsAccessor)Control).DataBindings;
            ExpressionBindingCollection ebc = ((IExpressionsAccessor)Control).Expressions;

            dbc.Clear(); 
            foreach (DesignTimeDataBinding binding in _bindings.Values) {
                dbc.Add(binding.RuntimeDataBinding); 
                ebc.Remove(binding.RuntimeDataBinding.PropertyName); 
            }
 
            _bindingsDirty = false;
        }

        /// <devdoc> 
        /// </devdoc>
        private string SaveFormat() { 
            string formatText = String.Empty; 

            FormatItem selectedFormat = _formatCombo.SelectedItem as FormatItem; 
            if (selectedFormat != null) {
                formatText = selectedFormat.Format;
            }
            else { 
                // if all whitespace, then disregard se format
                // otherwise whitespace is significant 
 
                formatText = _formatCombo.Text;
 
                string trimmedText = formatText.Trim();
                if (trimmedText.Length == 0) {
                    formatText = trimmedText;
                } 
            }
 
            return formatText; 
        }
 
        private void UpdateExpression() {
            Debug.Assert(_exprBindingRadio.Checked == false);

            string expression = String.Empty; 

            if (_fieldCombo.SelectedIndex > DataBindingsDialog.UnboundItemIndex) { 
                string fieldName = _fieldCombo.Text; 
                string format = SaveFormat();
 
                if (_twoWayBindingCheckBox.Checked) {
                    expression = DesignTimeDataBinding.CreateBindExpression(fieldName, format);
                }
                else { 
                    expression = DesignTimeDataBinding.CreateEvalExpression(fieldName, format);
 
                } 
            }
 
            _exprTextBox.Text = expression;
        }

        private void UpdateFormatItems() { 
            FormatItem[] items = FormatItem.DefaultFormats;
 
            _formatSampleObject = null; 
            _formatCombo.SelectedIndex = -1;
            _formatCombo.Text = String.Empty; 

            FieldItem selectedItem = (FieldItem)_fieldCombo.SelectedItem;
            if ((selectedItem != null) && (selectedItem.Type != null)) {
                TypeCode typeCode = Type.GetTypeCode(selectedItem.Type); 

                switch (typeCode) { 
                    case TypeCode.Decimal: 
                    case TypeCode.Double:
                    case TypeCode.Single: 
                        items = FormatItem.DecimalFormats;
                        _formatSampleObject = 1;
                        break;
                    case TypeCode.Byte: 
                    case TypeCode.SByte:
                    case TypeCode.Int16: 
                    case TypeCode.Int32: 
                    case TypeCode.Int64:
                    case TypeCode.UInt16: 
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        items = FormatItem.NumericFormats;
                        _formatSampleObject = 1; 
                        break;
                    case TypeCode.String: 
                        _formatSampleObject = "abc"; 
                        break;
                    case TypeCode.DateTime: 
                        items = FormatItem.DateTimeFormats;
                        _formatSampleObject = DateTime.Today;
                        break;
                    case TypeCode.Boolean: 
                    case TypeCode.Char:
                    case TypeCode.DBNull: 
                    case TypeCode.Object: 
                    default:
                        break; 
                }
            }

            _formatCombo.Items.Clear(); 
            _formatCombo.Items.AddRange(items);
        } 
 
        private void UpdateFormatSample() {
            string sampleValue = String.Empty; 

            if (_formatSampleObject != null) {
                string format = SaveFormat();
 
                if (format.Length != 0) {
                    try { 
                        sampleValue = String.Format(CultureInfo.CurrentCulture, format, _formatSampleObject); 
                    }
                    catch { 
                        sampleValue = SR.GetString(SR.DBDlg_InvalidFormat);
                    }
                }
            } 

            _sampleTextBox.Text = sampleValue; 
        } 

        private void UpdateUIState() { 
            if (_currentNode == null) {
                _fieldBindingRadio.Enabled = false;
                _fieldCombo.Enabled = false;
                _formatCombo.Enabled = false; 
                _sampleTextBox.Enabled = false;
                _fieldLabel.Enabled = false; 
                _formatLabel.Enabled = false; 
                _sampleLabel.Enabled = false;
                _twoWayBindingCheckBox.Enabled = false; 

                _exprBindingRadio.Enabled = false;
                _exprTextBox.Enabled = false;
            } 
            else {
                _fieldBindingRadio.Enabled = _fieldsAvailable; 
                _exprBindingRadio.Enabled = true; 

                bool fieldBinding = _fieldBindingRadio.Checked; 
                bool fieldSelected = fieldBinding && (_fieldCombo.SelectedIndex > UnboundItemIndex);
                bool formattable = fieldSelected &&
                                   (_currentNode.PropertyDescriptor.PropertyType == typeof(string));
 
                _fieldCombo.Enabled = fieldBinding;
                _fieldLabel.Enabled = fieldBinding; 
                _formatCombo.Enabled = formattable; 
                _formatLabel.Enabled = formattable;
                _sampleTextBox.Enabled = formattable; 
                _sampleLabel.Enabled = formattable;
                _twoWayBindingCheckBox.Enabled = fieldSelected;
                _exprTextBox.Enabled = !fieldBinding;
            } 
        }
 
 
        /// <summary>
        /// </summary> 
        private sealed class BindablePropertyNode : TreeNode {

            private PropertyDescriptor _propDesc;
            private BindingMode _bindingMode; 
            private bool _twoWayBoundByDefault;
            private bool _twoWayBoundByDefaultValid; 
 
            public BindablePropertyNode(PropertyDescriptor propDesc, BindingMode bindingMode) {
                _propDesc = propDesc; 
                _bindingMode = bindingMode;

                Text = propDesc.Name;
 
                int imageIndex = UnboundImageIndex;
                if (bindingMode == BindingMode.OneWay) { 
                    imageIndex = BoundImageIndex; 
                }
                else if (bindingMode == BindingMode.TwoWay) { 
                    imageIndex = TwoWayBoundImageIndex;
                }
                ImageIndex = SelectedImageIndex = imageIndex;
            } 

            public BindingMode BindingMode { 
                get { 
                    return _bindingMode;
                } 
                set {
                    _bindingMode = value;
                    int imageIndex = UnboundImageIndex;
                    if (_bindingMode == BindingMode.OneWay) { 
                        imageIndex = BoundImageIndex;
                    } 
                    else if (_bindingMode == BindingMode.TwoWay) { 
                        imageIndex = TwoWayBoundImageIndex;
                    } 
                    ImageIndex = SelectedImageIndex = imageIndex;
                }
            }
 
            public bool IsBound {
                get { 
                    return (_bindingMode == BindingMode.OneWay) || (_bindingMode == BindingMode.TwoWay); 
                }
            } 

            public bool TwoWayBoundByDefault {
                get {
                    if (!_twoWayBoundByDefaultValid) { 
                        BindableAttribute bindable = _propDesc.Attributes[typeof(BindableAttribute)] as BindableAttribute;
                        if (bindable != null) { 
                            _twoWayBoundByDefault = (bindable.Direction == BindingDirection.TwoWay); 
                        }
                        _twoWayBoundByDefaultValid = true; 
                    }
                    return _twoWayBoundByDefault;
                }
            } 

            public PropertyDescriptor PropertyDescriptor { 
                get { 
                    return _propDesc;
                } 
            }
        }

        enum BindingMode { 
            NotSet = 0,
            OneWay = 1, 
            TwoWay = 2 
        }
 

        /// <summary>
        /// </summary>
        private sealed class FieldItem { 

            private string _name; 
            private Type _type; 

            public FieldItem() : this(SR.GetString(SR.DBDlg_Unbound), null) { 
            }

            public FieldItem(string name, Type type) {
                _name = name; 
                _type = type;
            } 
 
            public Type Type {
                get { 
                    return _type;
                }
            }
 
            public override string ToString() {
                return _name; 
            } 
        }
 

        /// <devdoc>
        /// </devdoc>
        private class FormatItem { 
            private static readonly FormatItem nullFormat = new FormatItem(SR.GetString(SR.DBDlg_Fmt_None), String.Empty);
 
            private static readonly FormatItem generalFormat = new FormatItem(SR.GetString(SR.DBDlg_Fmt_General), "{0}"); 

            private static readonly FormatItem dtShortTime = new FormatItem(SR.GetString(SR.DBDlg_Fmt_ShortTime), "{0:t}"); 
            private static readonly FormatItem dtLongTime = new FormatItem(SR.GetString(SR.DBDlg_Fmt_LongTime), "{0:T}");
            private static readonly FormatItem dtShortDate = new FormatItem(SR.GetString(SR.DBDlg_Fmt_ShortDate), "{0:d}");
            private static readonly FormatItem dtLongDate = new FormatItem(SR.GetString(SR.DBDlg_Fmt_LongDate), "{0:D}");
            private static readonly FormatItem dtDateTime = new FormatItem(SR.GetString(SR.DBDlg_Fmt_DateTime), "{0:g}"); 
            private static readonly FormatItem dtFullDate = new FormatItem(SR.GetString(SR.DBDlg_Fmt_FullDate), "{0:G}");
 
            private static readonly FormatItem numNumber = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Numeric), "{0:N}"); 
            private static readonly FormatItem numDecimal = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Decimal), "{0:D}");
            private static readonly FormatItem numFixed = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Fixed), "{0:F}"); 
            private static readonly FormatItem numCurrency = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Currency), "{0:C}");
            private static readonly FormatItem numScientific = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Scientific), "{0:E}");
            private static readonly FormatItem numHex = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Hexadecimal), "0x{0:X}");
 
            public static readonly FormatItem[] DefaultFormats = new FormatItem[] {
                FormatItem.nullFormat, 
                FormatItem.generalFormat 
            };
 
            public static readonly FormatItem[] DateTimeFormats = new FormatItem[] {
                FormatItem.nullFormat,
                FormatItem.generalFormat,
                FormatItem.dtShortTime, 
                FormatItem.dtLongTime,
                FormatItem.dtShortDate, 
                FormatItem.dtLongDate, 
                FormatItem.dtDateTime,
                FormatItem.dtFullDate 
            };

            public static readonly FormatItem[] NumericFormats = new FormatItem[] {
                FormatItem.nullFormat, 
                FormatItem.generalFormat,
                FormatItem.numNumber, 
                FormatItem.numDecimal, 
                FormatItem.numFixed,
                FormatItem.numCurrency, 
                FormatItem.numScientific,
                FormatItem.numHex
            };
 
            public static readonly FormatItem[] DecimalFormats = new FormatItem[] {
                FormatItem.nullFormat, 
                FormatItem.generalFormat, 
                FormatItem.numNumber,
                FormatItem.numDecimal, 
                FormatItem.numFixed,
                FormatItem.numCurrency,
                FormatItem.numScientific
            }; 

            private readonly string _displayText; 
            private readonly string _format; 

            private FormatItem(string displayText, string format) { 
                _displayText = String.Format(CultureInfo.CurrentCulture, displayText, format);
                _format = format;
            }
 
            public string Format {
                get { 
                    return _format; 
                }
            } 

            public override string ToString() {
                return _displayText;
            } 
        }
 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataBindingsDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.Text; 
    using System.Text.RegularExpressions;
    using System.Web.UI; 
    using System.Web.UI.Design.Util;
    //
    using System.Web.UI.Design.WebControls;
    using System.Windows.Forms; 

    using WebUIControl = System.Web.UI.Control; 
 
    internal sealed class DataBindingsDialog : DesignerForm {
 
        private static readonly Attribute[] BrowsablePropertiesFilter =
            new Attribute[] { BrowsableAttribute.Yes, ReadOnlyAttribute.No};
        private static readonly Attribute[] BindablePropertiesFilter =
            new Attribute[] { BindableAttribute.Yes, ReadOnlyAttribute.No}; 

        private const int UnboundImageIndex = 0; 
        private const int BoundImageIndex = 1; 
        private const int TwoWayBoundImageIndex = 2;
 
        private const int UnboundItemIndex = 0;

        private System.Windows.Forms.Label _instructionLabel;
        private System.Windows.Forms.Label _bindablePropsLabels; 
        private System.Windows.Forms.TreeView _bindablePropsTree;
        private System.Windows.Forms.CheckBox _allPropsCheckBox; 
        private Label _bindingLabel; 
        private System.Windows.Forms.RadioButton _fieldBindingRadio;
        private System.Windows.Forms.Label _fieldLabel; 
        private System.Windows.Forms.ComboBox _fieldCombo;
        private System.Windows.Forms.Label _formatLabel;
        private System.Windows.Forms.Label _sampleLabel;
        private System.Windows.Forms.TextBox _sampleTextBox; 
        private System.Windows.Forms.RadioButton _exprBindingRadio;
        private System.Windows.Forms.TextBox _exprTextBox; 
        private System.Windows.Forms.Button _okButton; 
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.ComboBox _formatCombo; 
        private System.Windows.Forms.Label _exprLabel;
        private System.Windows.Forms.LinkLabel _refreshSchemaLink;
        private System.Windows.Forms.CheckBox _twoWayBindingCheckBox;
        private System.Windows.Forms.Panel _fieldBindingPanel; 
        private System.Windows.Forms.Panel _customBindingPanel;
        private System.Windows.Forms.Panel _bindingOptionsPanel; 
 
        private string _controlID;
        private IDictionary _bindings; 
        private bool _bindingsDirty;

        private bool _fieldsAvailable;
 
        private BindablePropertyNode _currentNode;
        private DesignTimeDataBinding _currentDataBinding; 
        private bool _currentDataBindingDirty; 

        private bool _internalChange; 
        private bool _formatDirty;
        private object _formatSampleObject;

        public DataBindingsDialog(IServiceProvider serviceProvider, WebUIControl control) : base(serviceProvider) { 
            Debug.Assert(control != null);
            Debug.Assert(control.Site != null); 
 
            _controlID = control.ID;
 
            // Setup the user interface
            InitializeComponent();
            InitializeUserInterface();
        } 

        private WebUIControl Control { 
            get { 
                IServiceProvider serviceProvider = ServiceProvider;
                Debug.Assert(serviceProvider != null); 
                if (serviceProvider != null) {
                    IContainer container = null;
                    ISite nestedSite = serviceProvider as ISite;
                    IContainer nestedContainer = null; 
                    if (nestedSite != null) {
                        nestedContainer = nestedSite.Container; 
                    } 

                    if (nestedContainer != null && nestedContainer is NestedContainer) { 
                        container = nestedContainer;
                    }
                    else {
                        container = ((IContainer)serviceProvider.GetService(typeof(IContainer))); 
                    }
                    Debug.Assert(container != null); 
                    if (container != null) { 
                        WebUIControl control = container.Components[_controlID] as WebUIControl;
                        Debug.Assert(control != null); 
                        return control;
                    }
                }
                Debug.Fail("Could not get Control"); 
                return null;
            } 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.DataBinding.BindingsDialog";
            }
        } 

        private bool ContainingTemplateIsBindable(ControlDesigner designer) { 
            bool bindable = false; 
            IControlDesignerView view = designer.View;
            if (view != null) { 
                TemplatedEditableDesignerRegion editableRegion = view.ContainingRegion as TemplatedEditableDesignerRegion;
                if (editableRegion != null) {
                    TemplateDefinition templateDefinition = editableRegion.TemplateDefinition;
                    PropertyDescriptor prop = TypeDescriptor.GetProperties(templateDefinition.TemplatedObject)[templateDefinition.TemplatePropertyName]; 
                    if (prop != null) {
                        TemplateContainerAttribute containerAttr = prop.Attributes[typeof(TemplateContainerAttribute)] as TemplateContainerAttribute; 
                        if (containerAttr != null && (containerAttr.BindingDirection == BindingDirection.TwoWay)) { 
                            bindable = true;
                        } 
                    }
                }
            }
            return bindable; 
        }
 
        private void ExtractFields(IDataSourceViewSchema schema, ArrayList fields) { 
            if (schema != null) {
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields(); 
                if (fieldSchemas != null) {
                    for (int i = 0; i < fieldSchemas.Length; i++) {
                        fields.Add(new FieldItem(fieldSchemas[i].Name, fieldSchemas[i].DataType));
                    } 
                }
            } 
        } 

        private void ExtractFields(IDataSourceProvider dataSourceProvider, ArrayList fields) { 
            Debug.Assert(dataSourceProvider != null);

            IEnumerable dataSource = dataSourceProvider.GetResolvedSelectedDataSource();
            if (dataSource != null) { 
                PropertyDescriptorCollection props = DesignTimeData.GetDataFields(dataSource);
                if ((props != null) && (props.Count != 0)) { 
                    foreach (PropertyDescriptor propDesc in props) { 
                        fields.Add(new FieldItem(propDesc.Name, propDesc.PropertyType));
                    } 
                }
            }
        }
 
        private IDesigner GetNamingContainerDesigner(ControlDesigner designer) {
            IControlDesignerView view = designer.View; 
            return (view != null) ? view.NamingContainerDesigner : null; 
        }
 
        #region Windows Form Designer generated code
        private void InitializeComponent() {
            this._instructionLabel = new System.Windows.Forms.Label();
            this._bindablePropsLabels = new System.Windows.Forms.Label(); 
            this._bindablePropsTree = new System.Windows.Forms.TreeView();
            this._allPropsCheckBox = new System.Windows.Forms.CheckBox(); 
            this._bindingLabel = new Label(); 
            this._fieldBindingRadio = new System.Windows.Forms.RadioButton();
            this._fieldLabel = new System.Windows.Forms.Label(); 
            this._fieldCombo = new System.Windows.Forms.ComboBox();
            this._formatLabel = new System.Windows.Forms.Label();
            this._formatCombo = new System.Windows.Forms.ComboBox();
            this._sampleLabel = new System.Windows.Forms.Label(); 
            this._sampleTextBox = new System.Windows.Forms.TextBox();
            this._exprBindingRadio = new System.Windows.Forms.RadioButton(); 
            this._exprTextBox = new System.Windows.Forms.TextBox(); 
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button(); 
            this._refreshSchemaLink = new System.Windows.Forms.LinkLabel();
            this._exprLabel = new System.Windows.Forms.Label();
            this._twoWayBindingCheckBox = new System.Windows.Forms.CheckBox();
            this._fieldBindingPanel = new System.Windows.Forms.Panel(); 
            this._customBindingPanel = new System.Windows.Forms.Panel();
            this._bindingOptionsPanel = new System.Windows.Forms.Panel(); 
            this.SuspendLayout(); 
            //
            // _instructionLabel 
            //
            this._instructionLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._instructionLabel.Location = new System.Drawing.Point(12, 12);
            this._instructionLabel.Name = "_instructionLabel"; 
            this._instructionLabel.Size = new System.Drawing.Size(508, 30);
            this._instructionLabel.TabIndex = 0; 
            // 
            // _bindablePropsLabels
            // 
            this._bindablePropsLabels.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._bindablePropsLabels.Location = new System.Drawing.Point(12, 52);
            this._bindablePropsLabels.Name = "_bindablePropsLabels";
            this._bindablePropsLabels.Size = new System.Drawing.Size(184, 16); 
            this._bindablePropsLabels.TabIndex = 1;
            // 
            // _bindablePropsTree 
            //
            this._bindablePropsTree.HideSelection = false; 
            this._bindablePropsTree.ImageIndex = -1;
            this._bindablePropsTree.Location = new System.Drawing.Point(12, 72);
            this._bindablePropsTree.Name = "_bindablePropsTree";
            this._bindablePropsTree.SelectedImageIndex = -1; 
            this._bindablePropsTree.ShowLines = false;
            this._bindablePropsTree.ShowPlusMinus = false; 
            this._bindablePropsTree.ShowRootLines = false; 
            this._bindablePropsTree.Size = new System.Drawing.Size(184, 112);
            this._bindablePropsTree.TabIndex = 2; 
            this._bindablePropsTree.Sorted = true;
            this._bindablePropsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OnBindablePropsTreeAfterSelect);
            //
            // _allPropsCheckBox 
            //
            this._allPropsCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._allPropsCheckBox.Location = new System.Drawing.Point(12, 190); 
            this._allPropsCheckBox.Name = "_allPropsCheckBox";
            this._allPropsCheckBox.Size = new System.Drawing.Size(184, 40); 
            this._allPropsCheckBox.TabIndex = 3;
            this._allPropsCheckBox.Visible = true;
            this._allPropsCheckBox.CheckedChanged += new System.EventHandler(this.OnShowAllCheckedChanged);
            this._allPropsCheckBox.TextAlign = ContentAlignment.TopLeft; 
            this._allPropsCheckBox.CheckAlign = ContentAlignment.TopLeft;
            // 
            // _bindingLabel 
            //
            this._bindingLabel.Location = new System.Drawing.Point(210, 52); 
            this._bindingLabel.Name = "_bindingGroupLabel";
            this._bindingLabel.Size = new System.Drawing.Size(306, 16);
            this._bindingLabel.TabIndex = 4;
            // 
            // _fieldLabel
            // 
            this._fieldLabel.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._fieldLabel.Location = new System.Drawing.Point(0, 4);
            this._fieldLabel.Name = "_fieldLabel"; 
            this._fieldLabel.Size = new System.Drawing.Size(104, 16);
            this._fieldLabel.TabIndex = 100;
            //
            // _fieldCombo 
            //
            this._fieldCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
            this._fieldCombo.Location = new System.Drawing.Point(118, 0); 
            this._fieldCombo.Name = "_fieldCombo";
            this._fieldCombo.Size = new System.Drawing.Size(164, 21); 
            this._fieldCombo.TabIndex = 101;
            this._fieldCombo.SelectedIndexChanged += new System.EventHandler(this.OnFieldComboSelectedIndexChanged);
            //
            // _formatLabel 
            //
            this._formatLabel.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._formatLabel.Location = new System.Drawing.Point(0, 32); 
            this._formatLabel.Name = "_formatLabel";
            this._formatLabel.Size = new System.Drawing.Size(114, 16); 
            this._formatLabel.TabIndex = 102;
            //
            // _formatCombo
            // 
            this._formatCombo.Location = new System.Drawing.Point(118, 28);
            this._formatCombo.Name = "_formatCombo"; 
            this._formatCombo.Size = new System.Drawing.Size(164, 21); 
            this._formatCombo.TabIndex = 103;
            this._formatCombo.LostFocus += new System.EventHandler(this.OnFormatComboLostFocus); 
            this._formatCombo.TextChanged += new System.EventHandler(this.OnFormatComboTextChanged);
            this._formatCombo.SelectedIndexChanged += new System.EventHandler(this.OnFormatComboSelectedIndexChanged);
            //
            // _sampleLabel 
            //
            this._sampleLabel.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._sampleLabel.Location = new System.Drawing.Point(0, 60); 
            this._sampleLabel.Name = "_sampleLabel";
            this._sampleLabel.Size = new System.Drawing.Size(114, 16); 
            this._sampleLabel.TabIndex = 104;
            //
            // _sampleTextBox
            // 
            this._sampleTextBox.Location = new System.Drawing.Point(118, 56);
            this._sampleTextBox.Name = "_sampleTextBox"; 
            this._sampleTextBox.ReadOnly = true; 
            this._sampleTextBox.Size = new System.Drawing.Size(164, 20);
            this._sampleTextBox.TabIndex = 105; 
            //
            // _exprTextBox
            //
            this._exprTextBox.Location = new System.Drawing.Point(0, 18); 
            this._exprTextBox.Name = "_exprTextBox";
            this._exprTextBox.Size = new System.Drawing.Size(282, 20); 
            this._exprTextBox.TabIndex = 201; 
            this._exprTextBox.TextChanged += new System.EventHandler(this.OnExprTextBoxTextChanged);
            // 
            // _okButton
            //
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(360, 279); 
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 7; 
            this._okButton.Click += new System.EventHandler(this.OnOKButtonClick); 
            //
            // _cancelButton 
            //
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(441, 279); 
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 8; 
            // 
            // _refreshSchemaLink
            // 
            this._refreshSchemaLink.Visible = false;
            this._refreshSchemaLink.Location = new System.Drawing.Point(12, 283);
            this._refreshSchemaLink.Name = "_refreshSchemaLink";
            this._refreshSchemaLink.Size = new System.Drawing.Size(197, 16); 
            this._refreshSchemaLink.TabIndex = 6;
            this._refreshSchemaLink.TabStop = true; 
            this._refreshSchemaLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnRefreshSchemaLinkLinkClicked); 
            //
            // _exprLabel 
            //
            this._exprLabel.Location = new System.Drawing.Point(0, 0);
            this._exprLabel.Name = "_exprLabel";
            this._exprLabel.Size = new System.Drawing.Size(290, 16); 
            this._exprLabel.TabIndex = 200;
            // 
            // _twoWayBindingCheckBox 
            //
            this._twoWayBindingCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._twoWayBindingCheckBox.Location = new System.Drawing.Point(118, 83);
            this._twoWayBindingCheckBox.Name = "_twoWayBindingCheckBox";
            this._twoWayBindingCheckBox.Size = new System.Drawing.Size(168, 30);
            this._twoWayBindingCheckBox.TabIndex = 106; 
            this._twoWayBindingCheckBox.Enabled = true;
            this._twoWayBindingCheckBox.CheckedChanged += new System.EventHandler(this.OnTwoWayBindingChecked); 
            this._twoWayBindingCheckBox.TextAlign = ContentAlignment.TopLeft; 
            this._twoWayBindingCheckBox.CheckAlign = ContentAlignment.TopLeft;
            // 
            // _fieldBindingRadio
            //
            this._fieldBindingRadio.Checked = true;
            this._fieldBindingRadio.FlatStyle = System.Windows.Forms.FlatStyle.System; 
            this._fieldBindingRadio.Location = new System.Drawing.Point(0, 0);
            this._fieldBindingRadio.Name = "_fieldBindingRadio"; 
            this._fieldBindingRadio.Size = new System.Drawing.Size(302, 18); 
            this._fieldBindingRadio.TabIndex = 0;
            this._fieldBindingRadio.TabStop = true; 
            this._fieldBindingRadio.CheckedChanged += new System.EventHandler(this.OnFieldBindingRadioCheckedChanged);
            //
            // _exprBindingRadio
            // 
            this._exprBindingRadio.Location = new System.Drawing.Point(0, 127);
            this._exprBindingRadio.Name = "_exprBindingRadio"; 
            this._exprBindingRadio.Size = new System.Drawing.Size(302, 18); 
            this._exprBindingRadio.TabIndex = 2;
            this._exprBindingRadio.CheckedChanged += new System.EventHandler(this.OnExprBindingRadioCheckedChanged); 
            //
            // _fieldBindingPanel
            //
            this._fieldBindingPanel.TabIndex = 1; 
            this._fieldBindingPanel.Name = "_fieldBindingPanel";
            this._fieldBindingPanel.Location = new System.Drawing.Point(16, 20); 
            this._fieldBindingPanel.Size = new System.Drawing.Size(286, 105); 
            this._fieldBindingPanel.Controls.Add(this._fieldLabel);
            this._fieldBindingPanel.Controls.Add(this._fieldCombo); 
            this._fieldBindingPanel.Controls.Add(this._formatLabel);
            this._fieldBindingPanel.Controls.Add(this._formatCombo);
            this._fieldBindingPanel.Controls.Add(this._sampleLabel);
            this._fieldBindingPanel.Controls.Add(this._sampleTextBox); 
            this._fieldBindingPanel.Controls.Add(this._twoWayBindingCheckBox);
            // 
            // _customBindingPanel 
            //
            this._customBindingPanel.TabIndex = 3; 
            this._customBindingPanel.Name = "_customBindingPanel";
            this._customBindingPanel.Location = new System.Drawing.Point(16, 148);
            this._customBindingPanel.Size = new System.Drawing.Size(286, 54);
            this._customBindingPanel.Controls.Add(this._exprLabel); 
            this._customBindingPanel.Controls.Add(this._exprTextBox);
            // 
            // DataBindingsDialog 
            //
            this._bindingOptionsPanel.TabIndex = 5; 
            this._bindingOptionsPanel.Name = "_bindingOptionsPanel";
            this._bindingOptionsPanel.Location = new System.Drawing.Point(214, 76);
            this._bindingOptionsPanel.Size = new System.Drawing.Size(302, 200);
            this._bindingOptionsPanel.Controls.Add(this._fieldBindingRadio); 
            this._bindingOptionsPanel.Controls.Add(this._exprBindingRadio);
            this._bindingOptionsPanel.Controls.Add(this._fieldBindingPanel); 
            this._bindingOptionsPanel.Controls.Add(this._customBindingPanel); 

            this.AcceptButton = this._okButton; 
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(524, 314);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          this._refreshSchemaLink, 
                                                                          this._cancelButton,
                                                                          this._okButton, 
                                                                          this._bindingLabel, 
                                                                          this._allPropsCheckBox,
                                                                          this._bindablePropsTree, 
                                                                          this._bindablePropsLabels,
                                                                          this._instructionLabel,
                                                                          this._bindingOptionsPanel});
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; 
            this.Name = "DataBindingsDialog";
 
            InitializeForm(); 

            this.ResumeLayout(false); 

        }
        #endregion
 
        private void InitializeUserInterface() {
            Text = SR.GetString(SR.DBDlg_Text, Control.Site.Name); 
 
            _instructionLabel.Text = SR.GetString(SR.DBDlg_Inst);
            _bindablePropsLabels.Text = SR.GetString(SR.DBDlg_BindableProps); 
            _allPropsCheckBox.Text = SR.GetString(SR.DBDlg_ShowAll);
            _fieldBindingRadio.Text = SR.GetString(SR.DBDlg_FieldBinding);
            _fieldLabel.Text = SR.GetString(SR.DBDlg_Field);
            _formatLabel.Text = SR.GetString(SR.DBDlg_Format); 
            _sampleLabel.Text = SR.GetString(SR.DBDlg_Sample);
            _exprBindingRadio.Text = SR.GetString(SR.DBDlg_CustomBinding); 
            _okButton.Text = SR.GetString(SR.DBDlg_OK); 
            _cancelButton.Text = SR.GetString(SR.DBDlg_Cancel);
            _refreshSchemaLink.Text = SR.GetString(SR.DBDlg_RefreshSchema); 
            _exprLabel.Text = SR.GetString(SR.DBDlg_Expr);
            _twoWayBindingCheckBox.Text = SR.GetString(SR.DBDlg_TwoWay);

            ImageList imageList = new ImageList(); 
            imageList.TransparentColor = Color.Magenta;
            imageList.ColorDepth = ColorDepth.Depth32Bit; 
            imageList.Images.AddStrip(new Bitmap(typeof(DataBindingsDialog), "BindableProperties.bmp")); 
            _bindablePropsTree.ImageList = imageList;
 
            bool showBindingCheckBox = false;
            IDesignerHost designerHost = (IDesignerHost)Control.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null);
            if (designerHost != null) { 
                ControlDesigner designer = designerHost.GetDesigner(Control) as ControlDesigner;
                if (designer != null) { 
                    showBindingCheckBox = ContainingTemplateIsBindable(designer); 
                }
            } 
            _twoWayBindingCheckBox.Visible = showBindingCheckBox;
        }

        private void LoadBindableProperties(bool showAll) { 
            string previouslySelectedProp = String.Empty;
            if (_bindablePropsTree.SelectedNode != null) { 
                previouslySelectedProp = _bindablePropsTree.SelectedNode.Text; 
            }
            _bindablePropsTree.Nodes.Clear(); 

            PropertyDescriptorCollection propDescs = TypeDescriptor.GetProperties(Control.GetType(), BindablePropertiesFilter);
            if (showAll) {
                PropertyDescriptorCollection browsablePropDescs = TypeDescriptor.GetProperties(Control.GetType(), BrowsablePropertiesFilter); 
                if (browsablePropDescs != null && browsablePropDescs.Count > 0) {
                    int bindableCount = propDescs.Count; 
                    int browsableCount = browsablePropDescs.Count; 
                    PropertyDescriptor[] allProps = new PropertyDescriptor[bindableCount + browsableCount];
                    propDescs.CopyTo(allProps, 0); 

                    int currentPropIndex = bindableCount;
                    // Merge collections, ignoring repeats.  PropertyDescriptorCollection.Add isn't implemented.
                    foreach (PropertyDescriptor pd in browsablePropDescs) { 
                        if (!propDescs.Contains(pd) && !String.Equals(pd.Name, "id", StringComparison.OrdinalIgnoreCase)) {
                            allProps[currentPropIndex++] = pd; 
                        } 
                    }
                    PropertyDescriptor[] allPropsFinalCount = new PropertyDescriptor[currentPropIndex]; 
                    Array.Copy(allProps, allPropsFinalCount, currentPropIndex);
                    propDescs = new PropertyDescriptorCollection(allPropsFinalCount);
                }
            } 

            string defaultPropName = null; 
            ControlValuePropertyAttribute controlValuePropertyAttr = TypeDescriptor.GetAttributes(Control)[typeof(ControlValuePropertyAttribute)] as ControlValuePropertyAttribute; 
            if (controlValuePropertyAttr != null) {
                defaultPropName = controlValuePropertyAttr.Name; 
            }
            else {
                PropertyDescriptor defaultProperty = TypeDescriptor.GetDefaultProperty(Control);
                if (defaultProperty != null) { 
                    defaultPropName = defaultProperty.Name;
                } 
            } 

            TreeNodeCollection nodes = _bindablePropsTree.Nodes; 
            TreeNode defaultNode = null;
            TreeNode selectedNode = null;

            _bindablePropsTree.BeginUpdate(); 

            foreach (PropertyDescriptor pd in propDescs) { 
                bool bound = (_bindings[pd.Name] != null); 
                BindingMode bindingMode = BindingMode.NotSet;
                if (bound) { 
                    if (((DesignTimeDataBinding)_bindings[pd.Name]).IsTwoWayBound) {
                        bindingMode = BindingMode.TwoWay;
                    }
                    else { 
                        bindingMode = BindingMode.OneWay;
                    } 
                } 

                TreeNode node = new BindablePropertyNode(pd, bindingMode); 

                if (pd.Name.Equals(defaultPropName)) {
                    defaultNode = node;
                } 
                if (pd.Name.Equals(previouslySelectedProp)) {
                    selectedNode = node; 
                } 

                nodes.Add(node); 
            }

            _bindablePropsTree.EndUpdate();
 
            if (selectedNode == null && defaultNode == null && nodes.Count != 0) {
                int nodeCount = nodes.Count; 
                for (int i = 0; i < nodeCount; i++) { 
                    BindablePropertyNode node = (BindablePropertyNode)nodes[i];
                    if (node.IsBound) { 
                        selectedNode = node;
                        break;
                    }
                } 
                if (selectedNode == null) {
                    selectedNode = nodes[0]; 
                } 
            }
            if (selectedNode != null) { 
                _bindablePropsTree.SelectedNode = selectedNode;
            }
            else if (defaultNode != null) {
                _bindablePropsTree.SelectedNode = defaultNode; 
            }
 
 
            UpdateUIState();
        } 

        /// <devdoc>
        /// </devdoc>
        private void LoadCurrentDataBinding() { 
            Debug.Assert(_currentDataBindingDirty == false, "Must save pending changes first.");
 
            _internalChange = true; 
            try {
                // first initialize the UI state 
                _fieldBindingRadio.Checked = _fieldsAvailable;
                _bindingLabel.Text = String.Empty;
                _fieldCombo.SelectedIndex = -1;
                _formatCombo.Text = String.Empty; 
                _sampleTextBox.Text = String.Empty;
                _exprBindingRadio.Checked = !_fieldsAvailable; 
                _exprTextBox.Text = String.Empty; 
                _twoWayBindingCheckBox.Checked = false;
 
                _formatDirty = false;

                if (_currentNode != null) {
                    // load the current selected property 
                    _bindingLabel.Text = SR.GetString(SR.DBDlg_BindingGroup, _currentNode.PropertyDescriptor.Name);
 
                    _twoWayBindingCheckBox.Checked = _currentNode.TwoWayBoundByDefault && _twoWayBindingCheckBox.Visible; 
                    if (_currentDataBinding != null) {
                        // load the databinding if there is one associated with the property 
                        bool useExpression = true;

                        if (_fieldsAvailable && (_currentDataBinding.IsCustom == false)) {
                            string field = _currentDataBinding.Field; 
                            string format = _currentDataBinding.Format;
 
                            Debug.Assert(_fieldCombo.Items.Count > 1); 

                            // strip off surrounding square brackets, if they exist 
                            field = field.TrimStart(new char[] {'['});
                            field = field.TrimEnd(new char[] {']'});

                            // this finds the field in a case-insensitive manner 
                            int fieldIndex = _fieldCombo.FindStringExact(field, 1);
 
                            if (fieldIndex != -1) { 
                                useExpression = false;
 
                                _fieldCombo.SelectedIndex = fieldIndex;

                                UpdateFormatItems();
 
                                bool knownFormat = false;
                                foreach (FormatItem item in _formatCombo.Items) { 
                                    if (item.Format.Equals(format)) { 
                                        knownFormat = true;
                                        _formatCombo.SelectedItem = item; 
                                    }
                                }
                                if (knownFormat == false) {
                                    _formatCombo.Text = format; 
                                }
 
                                UpdateFormatSample(); 
                                if (_currentNode.BindingMode == BindingMode.TwoWay) {
                                    _twoWayBindingCheckBox.Checked = true; 
                                }
                                else if (_currentNode.BindingMode == BindingMode.OneWay) {
                                    _twoWayBindingCheckBox.Checked = false;
                                } 
                            }
                        } 
 
                        if (useExpression) {
                            // either it was a custom expression or we're falling back 
                            // on this because of an error such as an unknown field.

                            _exprBindingRadio.Checked = true;
                            _exprTextBox.Text = _currentDataBinding.Expression; 
                        }
                        else { 
                            UpdateExpression(); 
                        }
                    } 
                }
            }
            finally {
                _internalChange = false; 

                UpdateUIState(); 
            } 
        }
 
        /// <devdoc>
        /// </devdoc>
        private void LoadDataBindings() {
            // Load the current bindings 
            _bindings = new Hashtable();
 
            DataBindingCollection currentDataBindings = ((IDataBindingsAccessor)Control).DataBindings; 
            foreach (DataBinding binding in currentDataBindings) {
                _bindings[binding.PropertyName] = new DesignTimeDataBinding(binding); 
            }
        }

        /// <devdoc> 
        /// </devdoc>
        private void LoadFields() { 
            // This can get called multiple times, for example whenever refresh schema is clicked 
            // Therefore clear the combobox first
            _fieldCombo.Items.Clear(); 

            ArrayList fields = new ArrayList();

            // Add the (Unbound) field item 
            fields.Add(new FieldItem());
 
            IDesigner containerControlDesigner = null; 

            IDesignerHost designerHost = (IDesignerHost)Control.Site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);
            if (designerHost != null) {
                ControlDesigner designer = designerHost.GetDesigner(Control) as ControlDesigner;
                if (designer != null) { 
                    containerControlDesigner = GetNamingContainerDesigner(designer);
                } 
            } 

            if (containerControlDesigner != null) { 
                IDataBindingSchemaProvider schemaProvider = containerControlDesigner as IDataBindingSchemaProvider;
                if (schemaProvider != null) {
                    if (schemaProvider.CanRefreshSchema) {
                        _refreshSchemaLink.Visible = true; 
                    }
 
                    IDataSourceViewSchema schema = null; 
                    try {
                        schema = schemaProvider.Schema; 
                    }
                    catch (Exception ex) {
                        IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)ServiceProvider.GetService(typeof(IComponentDesignerDebugService));
                        if (debugService != null) { 
                            debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message));
                        } 
                    } 

                    ExtractFields(schema, fields); 
                }
                else if (containerControlDesigner is IDataSourceProvider) {
                    ExtractFields((IDataSourceProvider)containerControlDesigner, fields);
                } 
            }
 
            _fieldCombo.Items.AddRange(fields.ToArray()); 
            _fieldsAvailable = (fields.Count > 1);
        } 

        private void OnBindablePropsTreeAfterSelect(object sender, TreeViewEventArgs e) {
            if (_currentDataBindingDirty) {
                SaveCurrentDataBinding(); 
            }
 
            _currentDataBinding = null; 
            _currentNode = (BindablePropertyNode)_bindablePropsTree.SelectedNode;
 
            if (_currentNode != null) {
                _currentDataBinding = (DesignTimeDataBinding)_bindings[_currentNode.PropertyDescriptor.Name];
            }
 
            LoadCurrentDataBinding();
        } 
 
        private void OnExprBindingRadioCheckedChanged(object sender, System.EventArgs e) {
            if (_internalChange) { 
                return;
            }

            _currentDataBindingDirty = true; 
            UpdateUIState();
        } 
 
        private void OnExprTextBoxTextChanged(object sender, System.EventArgs e) {
            if (_internalChange) { 
                return;
            }

            _currentDataBindingDirty = true; 
        }
 
        private void OnFieldBindingRadioCheckedChanged(object sender, System.EventArgs e) { 
            if (_internalChange) {
                return; 
            }

            _currentDataBindingDirty = true;
            if (_fieldBindingRadio.Checked) { 
                UpdateExpression();
            } 
            UpdateUIState(); 
        }
 
        private void OnFieldComboSelectedIndexChanged(object sender, EventArgs e) {
            if (_internalChange) {
                return;
            } 

            _currentDataBindingDirty = true; 
            UpdateFormatItems(); 
            UpdateExpression();
            UpdateUIState(); 
        }

        private void OnFormatComboLostFocus(object sender, EventArgs e) {
            if (_formatDirty) { 
                _formatDirty = false;
 
                UpdateFormatSample(); 
                UpdateExpression();
            } 
        }

        private void OnFormatComboTextChanged(object sender, EventArgs e) {
            if (_internalChange) { 
                return;
            } 
 
            _formatDirty = true;
        } 

        private void OnFormatComboSelectedIndexChanged(object sender, EventArgs e) {
            if (_internalChange) {
                return; 
            }
 
            _formatDirty = true; 
            UpdateFormatSample();
            UpdateExpression(); 
        }

        protected override void OnInitialActivated(EventArgs e) {
            base.OnInitialActivated(e); 

            LoadDataBindings(); 
            LoadFields(); 
            LoadBindableProperties(false);
        } 

        private void OnOKButtonClick(object sender, EventArgs e) {
            if (_currentDataBindingDirty) {
                SaveCurrentDataBinding(); 
            }
 
            if (_bindingsDirty) { 
                SaveDataBindings();
            } 

            DialogResult = DialogResult.OK;
            Close();
        } 

        private void OnRefreshSchemaLinkLinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { 
            if (_currentDataBindingDirty) { 
                SaveCurrentDataBinding();
            } 

            IDesigner containerControlDesigner = null;

            IDesignerHost designerHost = (IDesignerHost)Control.Site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);
            if (designerHost != null) { 
                ControlDesigner designer = designerHost.GetDesigner(Control) as ControlDesigner; 
                if (designer != null) {
                    containerControlDesigner = GetNamingContainerDesigner(designer); 
                }
            }

            if (containerControlDesigner != null) { 
                IDataBindingSchemaProvider schemaProvider = containerControlDesigner as IDataBindingSchemaProvider;
                if (schemaProvider != null) { 
                    schemaProvider.RefreshSchema(false); 
                }
            } 

            LoadFields();
            if (_currentNode != null) {
                _currentDataBinding = (DesignTimeDataBinding)_bindings[_currentNode.PropertyDescriptor.Name]; 
            }
            LoadCurrentDataBinding(); 
        } 

        private void OnShowAllCheckedChanged(object sender, EventArgs e) { 
            LoadBindableProperties(_allPropsCheckBox.Checked);
        }

        private void OnTwoWayBindingChecked(object sender, EventArgs e) { 
            if (_internalChange) {
                return; 
            } 

            _currentDataBindingDirty = true; 
            UpdateExpression();
            UpdateUIState();
        }
 
        private void SaveCurrentDataBinding() {
            Debug.Assert(_currentDataBindingDirty); 
            Debug.Assert(_currentNode != null); 

            DesignTimeDataBinding binding = null; 
            if (_fieldBindingRadio.Checked) {
                if (_fieldCombo.SelectedIndex > DataBindingsDialog.UnboundItemIndex) {
                    string fieldName = _fieldCombo.Text;
                    string format = SaveFormat(); 
                    binding = new DesignTimeDataBinding(_currentNode.PropertyDescriptor, fieldName, format, _twoWayBindingCheckBox.Checked);
                } 
            } 
            else {
                string expression = _exprTextBox.Text.Trim(); 
                if (expression.Length != 0) {
                    binding = new DesignTimeDataBinding(_currentNode.PropertyDescriptor, expression);
                }
            } 

            if (binding == null) { 
                _currentNode.BindingMode = BindingMode.NotSet; 
                _bindings.Remove(_currentNode.PropertyDescriptor.Name);
            } 
            else {
                if (_fieldBindingRadio.Checked) {
                    if (_twoWayBindingCheckBox.Checked && _twoWayBindingCheckBox.Visible) {
                        _currentNode.BindingMode =  BindingMode.TwoWay; 
                    }
                    else { 
                        _currentNode.BindingMode =  BindingMode.OneWay; 
                    }
                } 
                else {
                    if (binding.IsTwoWayBound) {
                        _currentNode.BindingMode =  BindingMode.TwoWay;
                    } 
                    else {
                        _currentNode.BindingMode =  BindingMode.OneWay; 
                    } 
                }
                _bindings[_currentNode.PropertyDescriptor.Name] = binding; 
            }

            _currentDataBindingDirty = false;
            _bindingsDirty = true; 
        }
 
        private void SaveDataBindings() { 
            Debug.Assert(_bindingsDirty == true);
 
            DataBindingCollection dbc = ((IDataBindingsAccessor)Control).DataBindings;
            ExpressionBindingCollection ebc = ((IExpressionsAccessor)Control).Expressions;

            dbc.Clear(); 
            foreach (DesignTimeDataBinding binding in _bindings.Values) {
                dbc.Add(binding.RuntimeDataBinding); 
                ebc.Remove(binding.RuntimeDataBinding.PropertyName); 
            }
 
            _bindingsDirty = false;
        }

        /// <devdoc> 
        /// </devdoc>
        private string SaveFormat() { 
            string formatText = String.Empty; 

            FormatItem selectedFormat = _formatCombo.SelectedItem as FormatItem; 
            if (selectedFormat != null) {
                formatText = selectedFormat.Format;
            }
            else { 
                // if all whitespace, then disregard se format
                // otherwise whitespace is significant 
 
                formatText = _formatCombo.Text;
 
                string trimmedText = formatText.Trim();
                if (trimmedText.Length == 0) {
                    formatText = trimmedText;
                } 
            }
 
            return formatText; 
        }
 
        private void UpdateExpression() {
            Debug.Assert(_exprBindingRadio.Checked == false);

            string expression = String.Empty; 

            if (_fieldCombo.SelectedIndex > DataBindingsDialog.UnboundItemIndex) { 
                string fieldName = _fieldCombo.Text; 
                string format = SaveFormat();
 
                if (_twoWayBindingCheckBox.Checked) {
                    expression = DesignTimeDataBinding.CreateBindExpression(fieldName, format);
                }
                else { 
                    expression = DesignTimeDataBinding.CreateEvalExpression(fieldName, format);
 
                } 
            }
 
            _exprTextBox.Text = expression;
        }

        private void UpdateFormatItems() { 
            FormatItem[] items = FormatItem.DefaultFormats;
 
            _formatSampleObject = null; 
            _formatCombo.SelectedIndex = -1;
            _formatCombo.Text = String.Empty; 

            FieldItem selectedItem = (FieldItem)_fieldCombo.SelectedItem;
            if ((selectedItem != null) && (selectedItem.Type != null)) {
                TypeCode typeCode = Type.GetTypeCode(selectedItem.Type); 

                switch (typeCode) { 
                    case TypeCode.Decimal: 
                    case TypeCode.Double:
                    case TypeCode.Single: 
                        items = FormatItem.DecimalFormats;
                        _formatSampleObject = 1;
                        break;
                    case TypeCode.Byte: 
                    case TypeCode.SByte:
                    case TypeCode.Int16: 
                    case TypeCode.Int32: 
                    case TypeCode.Int64:
                    case TypeCode.UInt16: 
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        items = FormatItem.NumericFormats;
                        _formatSampleObject = 1; 
                        break;
                    case TypeCode.String: 
                        _formatSampleObject = "abc"; 
                        break;
                    case TypeCode.DateTime: 
                        items = FormatItem.DateTimeFormats;
                        _formatSampleObject = DateTime.Today;
                        break;
                    case TypeCode.Boolean: 
                    case TypeCode.Char:
                    case TypeCode.DBNull: 
                    case TypeCode.Object: 
                    default:
                        break; 
                }
            }

            _formatCombo.Items.Clear(); 
            _formatCombo.Items.AddRange(items);
        } 
 
        private void UpdateFormatSample() {
            string sampleValue = String.Empty; 

            if (_formatSampleObject != null) {
                string format = SaveFormat();
 
                if (format.Length != 0) {
                    try { 
                        sampleValue = String.Format(CultureInfo.CurrentCulture, format, _formatSampleObject); 
                    }
                    catch { 
                        sampleValue = SR.GetString(SR.DBDlg_InvalidFormat);
                    }
                }
            } 

            _sampleTextBox.Text = sampleValue; 
        } 

        private void UpdateUIState() { 
            if (_currentNode == null) {
                _fieldBindingRadio.Enabled = false;
                _fieldCombo.Enabled = false;
                _formatCombo.Enabled = false; 
                _sampleTextBox.Enabled = false;
                _fieldLabel.Enabled = false; 
                _formatLabel.Enabled = false; 
                _sampleLabel.Enabled = false;
                _twoWayBindingCheckBox.Enabled = false; 

                _exprBindingRadio.Enabled = false;
                _exprTextBox.Enabled = false;
            } 
            else {
                _fieldBindingRadio.Enabled = _fieldsAvailable; 
                _exprBindingRadio.Enabled = true; 

                bool fieldBinding = _fieldBindingRadio.Checked; 
                bool fieldSelected = fieldBinding && (_fieldCombo.SelectedIndex > UnboundItemIndex);
                bool formattable = fieldSelected &&
                                   (_currentNode.PropertyDescriptor.PropertyType == typeof(string));
 
                _fieldCombo.Enabled = fieldBinding;
                _fieldLabel.Enabled = fieldBinding; 
                _formatCombo.Enabled = formattable; 
                _formatLabel.Enabled = formattable;
                _sampleTextBox.Enabled = formattable; 
                _sampleLabel.Enabled = formattable;
                _twoWayBindingCheckBox.Enabled = fieldSelected;
                _exprTextBox.Enabled = !fieldBinding;
            } 
        }
 
 
        /// <summary>
        /// </summary> 
        private sealed class BindablePropertyNode : TreeNode {

            private PropertyDescriptor _propDesc;
            private BindingMode _bindingMode; 
            private bool _twoWayBoundByDefault;
            private bool _twoWayBoundByDefaultValid; 
 
            public BindablePropertyNode(PropertyDescriptor propDesc, BindingMode bindingMode) {
                _propDesc = propDesc; 
                _bindingMode = bindingMode;

                Text = propDesc.Name;
 
                int imageIndex = UnboundImageIndex;
                if (bindingMode == BindingMode.OneWay) { 
                    imageIndex = BoundImageIndex; 
                }
                else if (bindingMode == BindingMode.TwoWay) { 
                    imageIndex = TwoWayBoundImageIndex;
                }
                ImageIndex = SelectedImageIndex = imageIndex;
            } 

            public BindingMode BindingMode { 
                get { 
                    return _bindingMode;
                } 
                set {
                    _bindingMode = value;
                    int imageIndex = UnboundImageIndex;
                    if (_bindingMode == BindingMode.OneWay) { 
                        imageIndex = BoundImageIndex;
                    } 
                    else if (_bindingMode == BindingMode.TwoWay) { 
                        imageIndex = TwoWayBoundImageIndex;
                    } 
                    ImageIndex = SelectedImageIndex = imageIndex;
                }
            }
 
            public bool IsBound {
                get { 
                    return (_bindingMode == BindingMode.OneWay) || (_bindingMode == BindingMode.TwoWay); 
                }
            } 

            public bool TwoWayBoundByDefault {
                get {
                    if (!_twoWayBoundByDefaultValid) { 
                        BindableAttribute bindable = _propDesc.Attributes[typeof(BindableAttribute)] as BindableAttribute;
                        if (bindable != null) { 
                            _twoWayBoundByDefault = (bindable.Direction == BindingDirection.TwoWay); 
                        }
                        _twoWayBoundByDefaultValid = true; 
                    }
                    return _twoWayBoundByDefault;
                }
            } 

            public PropertyDescriptor PropertyDescriptor { 
                get { 
                    return _propDesc;
                } 
            }
        }

        enum BindingMode { 
            NotSet = 0,
            OneWay = 1, 
            TwoWay = 2 
        }
 

        /// <summary>
        /// </summary>
        private sealed class FieldItem { 

            private string _name; 
            private Type _type; 

            public FieldItem() : this(SR.GetString(SR.DBDlg_Unbound), null) { 
            }

            public FieldItem(string name, Type type) {
                _name = name; 
                _type = type;
            } 
 
            public Type Type {
                get { 
                    return _type;
                }
            }
 
            public override string ToString() {
                return _name; 
            } 
        }
 

        /// <devdoc>
        /// </devdoc>
        private class FormatItem { 
            private static readonly FormatItem nullFormat = new FormatItem(SR.GetString(SR.DBDlg_Fmt_None), String.Empty);
 
            private static readonly FormatItem generalFormat = new FormatItem(SR.GetString(SR.DBDlg_Fmt_General), "{0}"); 

            private static readonly FormatItem dtShortTime = new FormatItem(SR.GetString(SR.DBDlg_Fmt_ShortTime), "{0:t}"); 
            private static readonly FormatItem dtLongTime = new FormatItem(SR.GetString(SR.DBDlg_Fmt_LongTime), "{0:T}");
            private static readonly FormatItem dtShortDate = new FormatItem(SR.GetString(SR.DBDlg_Fmt_ShortDate), "{0:d}");
            private static readonly FormatItem dtLongDate = new FormatItem(SR.GetString(SR.DBDlg_Fmt_LongDate), "{0:D}");
            private static readonly FormatItem dtDateTime = new FormatItem(SR.GetString(SR.DBDlg_Fmt_DateTime), "{0:g}"); 
            private static readonly FormatItem dtFullDate = new FormatItem(SR.GetString(SR.DBDlg_Fmt_FullDate), "{0:G}");
 
            private static readonly FormatItem numNumber = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Numeric), "{0:N}"); 
            private static readonly FormatItem numDecimal = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Decimal), "{0:D}");
            private static readonly FormatItem numFixed = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Fixed), "{0:F}"); 
            private static readonly FormatItem numCurrency = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Currency), "{0:C}");
            private static readonly FormatItem numScientific = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Scientific), "{0:E}");
            private static readonly FormatItem numHex = new FormatItem(SR.GetString(SR.DBDlg_Fmt_Hexadecimal), "0x{0:X}");
 
            public static readonly FormatItem[] DefaultFormats = new FormatItem[] {
                FormatItem.nullFormat, 
                FormatItem.generalFormat 
            };
 
            public static readonly FormatItem[] DateTimeFormats = new FormatItem[] {
                FormatItem.nullFormat,
                FormatItem.generalFormat,
                FormatItem.dtShortTime, 
                FormatItem.dtLongTime,
                FormatItem.dtShortDate, 
                FormatItem.dtLongDate, 
                FormatItem.dtDateTime,
                FormatItem.dtFullDate 
            };

            public static readonly FormatItem[] NumericFormats = new FormatItem[] {
                FormatItem.nullFormat, 
                FormatItem.generalFormat,
                FormatItem.numNumber, 
                FormatItem.numDecimal, 
                FormatItem.numFixed,
                FormatItem.numCurrency, 
                FormatItem.numScientific,
                FormatItem.numHex
            };
 
            public static readonly FormatItem[] DecimalFormats = new FormatItem[] {
                FormatItem.nullFormat, 
                FormatItem.generalFormat, 
                FormatItem.numNumber,
                FormatItem.numDecimal, 
                FormatItem.numFixed,
                FormatItem.numCurrency,
                FormatItem.numScientific
            }; 

            private readonly string _displayText; 
            private readonly string _format; 

            private FormatItem(string displayText, string format) { 
                _displayText = String.Format(CultureInfo.CurrentCulture, displayText, format);
                _format = format;
            }
 
            public string Format {
                get { 
                    return _format; 
                }
            } 

            public override string ToString() {
                return _displayText;
            } 
        }
 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
