//------------------------------------------------------------------------------ 
// <copyright file="ExpressionBindingsDialogcs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Configuration;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.Web.Configuration; 
    using System.Web.UI;
    using System.Web.UI.Design.Util;
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 

    using WebUIControl = System.Web.UI.Control; 
 
    internal sealed class ExpressionBindingsDialog : DesignerForm {
 
        private static readonly Attribute[] BindablePropertiesFilter =
            new Attribute[] { BrowsableAttribute.Yes, ReadOnlyAttribute.No };

        private const int UnboundImageIndex = 0; 
        private const int BoundImageIndex = 1;
        private const int ImplicitBoundImageIndex = 2; 
 
        private System.Windows.Forms.Label _instructionLabel;
        private System.Windows.Forms.Label _bindablePropsLabels; 
        private System.Windows.Forms.TreeView _bindablePropsTree;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private AutoSizeComboBox _expressionBuilderComboBox; 
        private PropertyGrid _expressionBuilderPropertyGrid;
        private Label _expressionBuilderLabel; 
        private Label _propertyGridLabel; 
        private System.Windows.Forms.Panel _propertiesPanel;
        private System.Windows.Forms.Label _generatedHelpLabel; 

        private WebUIControl _control;
        private string _controlID;
        private bool _bindingsDirty; 

        private ExpressionItem _noneItem; 
        private BindablePropertyNode _currentNode; 
        private ExpressionEditor _currentEditor;
        private ExpressionEditorSheet _currentSheet; 

        private IDictionary _expressionEditors;

        private bool _internalChange; 

        // We hold on to all the complex property bindings because they are 
        // not represented in the list of bindable properties. This way we 
        // don't lose them when the user makes any changes.
        private IDictionary _complexBindings; 

        public ExpressionBindingsDialog(IServiceProvider serviceProvider, WebUIControl control)
            : base(serviceProvider) {
            Debug.Assert(control != null); 
            Debug.Assert(control.Site != null);
 
            _control = control; 
            _controlID = control.ID;
 
            // Setup the user interface
            InitializeComponent();
            InitializeUserInterface();
        } 

        private ExpressionItem NoneItem { 
            get { 
                if (_noneItem == null) {
                    _noneItem = new ExpressionItem(SR.GetString(SR.ExpressionBindingsDialog_None)); 
                }
                return _noneItem;
            }
        } 

        private WebUIControl Control { 
            get { 
                return _control;
            } 
        }

        protected override string HelpTopic {
            get { 
                return "net.Asp.Expressions.BindingsDialog";
            } 
        } 

        #region Windows Form Designer generated code 
        private void InitializeComponent() {
            _instructionLabel = new System.Windows.Forms.Label();
            _bindablePropsLabels = new System.Windows.Forms.Label();
            _bindablePropsTree = new System.Windows.Forms.TreeView(); 
            _okButton = new System.Windows.Forms.Button();
            _cancelButton = new System.Windows.Forms.Button(); 
            _expressionBuilderComboBox = new AutoSizeComboBox(); 
            _expressionBuilderPropertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider);
            _expressionBuilderLabel = new Label(); 
            _propertyGridLabel = new Label();
            _propertiesPanel = new System.Windows.Forms.Panel();
            _generatedHelpLabel = new System.Windows.Forms.Label();
            SuspendLayout(); 
            //
            // _instructionLabel 
            // 
            _instructionLabel.Location = new System.Drawing.Point(12, 12);
            _instructionLabel.Name = "_instructionLabel"; 
            _instructionLabel.Size = new System.Drawing.Size(476, 36);
            _instructionLabel.TabIndex = 0;
            //
            // _bindablePropsLabels 
            //
            _bindablePropsLabels.Location = new System.Drawing.Point(12, 52); 
            _bindablePropsLabels.Name = "_bindablePropsLabels"; 
            _bindablePropsLabels.Size = new System.Drawing.Size(196, 16);
            _bindablePropsLabels.TabIndex = 1; 
            //
            // _bindablePropsTree
            //
            _bindablePropsTree.HideSelection = false; 
            _bindablePropsTree.ImageIndex = -1;
            _bindablePropsTree.Location = new System.Drawing.Point(12, 70); 
            _bindablePropsTree.Name = "_bindablePropsTree"; 
            _bindablePropsTree.SelectedImageIndex = -1;
            _bindablePropsTree.Sorted = true; 
            _bindablePropsTree.ShowLines = false;
            _bindablePropsTree.ShowPlusMinus = false;
            _bindablePropsTree.ShowRootLines = false;
            _bindablePropsTree.Size = new System.Drawing.Size(196, 182); 
            _bindablePropsTree.TabIndex = 2;
            _bindablePropsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(OnBindablePropsTreeAfterSelect); 
            // 
            // _okButton
            // 
            _okButton.Location = new System.Drawing.Point(312, 262);
            _okButton.Name = "_okButton";
            _okButton.TabIndex = 16;
            _okButton.Size = new System.Drawing.Size(85, 23); 
            _okButton.Click += new System.EventHandler(OnOKButtonClick);
            // 
            // _cancelButton 
            //
            _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            _cancelButton.Location = new System.Drawing.Point(403, 262);
            _cancelButton.Name = "_cancelButton";
            _cancelButton.Size = new System.Drawing.Size(85, 23);
            _cancelButton.TabIndex = 17; 
            //
            // _expressionBuilderLabel 
            // 
            _expressionBuilderLabel.Location = new System.Drawing.Point(0, 0);
            _expressionBuilderLabel.Name = "_expressionBuilderLabel"; 
            _expressionBuilderLabel.Size = new System.Drawing.Size(268, 16);
            _expressionBuilderLabel.TabIndex = 10;
            //
            // _expressionBuilderComboBox 
            //
            _expressionBuilderComboBox.DropDownStyle = ComboBoxStyle.DropDownList; 
            _expressionBuilderComboBox.Location = new System.Drawing.Point(0, 18); 
            _expressionBuilderComboBox.Name = "_expressionBuilderComboBox";
            _expressionBuilderComboBox.TabIndex = 20; 
            _expressionBuilderComboBox.Size = new Size(268, 21);
            _expressionBuilderComboBox.Sorted = true;
            _expressionBuilderComboBox.SelectedIndexChanged += new EventHandler(OnExpressionBuilderComboBoxSelectedIndexChanged);
            // 
            // _propertyGridLabel
            // 
            _propertyGridLabel.Location = new System.Drawing.Point(0, 43); 
            _propertyGridLabel.Name = "_propertyGridLabel";
            _propertyGridLabel.Size = new System.Drawing.Size(268, 16); 
            _propertyGridLabel.TabIndex = 30;
            //
            // _expressionBuilderPropertyGrid
            // 
            _expressionBuilderPropertyGrid.Location = new System.Drawing.Point(0, 61);
            _expressionBuilderPropertyGrid.Name = "_expressionBuilderPropertyGrid"; 
            _expressionBuilderPropertyGrid.TabIndex = 40; 
            _expressionBuilderPropertyGrid.Size = new Size(268, 139);
            _expressionBuilderPropertyGrid.PropertySort = PropertySort.Alphabetical; 
            _expressionBuilderPropertyGrid.ToolbarVisible = false;
            _expressionBuilderPropertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(OnExpressionBuilderPropertyGridPropertyValueChanged);
            _expressionBuilderPropertyGrid.Site = _control.Site;
            // 
            // _propertiesPanel
            // 
            _propertiesPanel.Location = new System.Drawing.Point(220, 52); 
            _propertiesPanel.Name = "_propertiesPanel";
            _propertiesPanel.Size = new System.Drawing.Size(268, 200); 
            _propertiesPanel.TabIndex = 5;
            _propertiesPanel.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          _expressionBuilderLabel,
                                                                          _expressionBuilderComboBox, 
                                                                          _propertyGridLabel,
                                                                          _expressionBuilderPropertyGrid}); 
            // 
            // _generatedHelpLabel
            // 
            _generatedHelpLabel.Location = new System.Drawing.Point(220, 72);
            _generatedHelpLabel.Name = "_generatedHelpLabel";
            _generatedHelpLabel.Size = new System.Drawing.Size(268, 180);
            _generatedHelpLabel.TabIndex = 5; 
            //
            // ExpressionBindingsDialog 
            // 
            CancelButton = _cancelButton;
            ClientSize = new System.Drawing.Size(500, 297); 
            Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          _cancelButton,
                                                                          _okButton,
                                                                          _propertiesPanel, 
                                                                          _bindablePropsTree,
                                                                          _bindablePropsLabels, 
                                                                          _instructionLabel, 
                                                                          _generatedHelpLabel});
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; 
            Name = "ExpressionBindingsDialog";

            InitializeForm();
 
            ResumeLayout(false);
 
        } 
        #endregion
 
        private void InitializeUserInterface() {
            string controlSiteName = String.Empty;
            if ((Control != null) && (Control.Site != null)) {
                controlSiteName = Control.Site.Name; 
            }
            Text = SR.GetString(SR.ExpressionBindingsDialog_Text, controlSiteName); 
 
            _instructionLabel.Text = SR.GetString(SR.ExpressionBindingsDialog_Inst);
            _bindablePropsLabels.Text = SR.GetString(SR.ExpressionBindingsDialog_BindableProps); 
            _okButton.Text = SR.GetString(SR.ExpressionBindingsDialog_OK);
            _cancelButton.Text = SR.GetString(SR.ExpressionBindingsDialog_Cancel);
            _expressionBuilderLabel.Text = SR.GetString(SR.ExpressionBindingsDialog_ExpressionType);
            _propertyGridLabel.Text = SR.GetString(SR.ExpressionBindingsDialog_Properties); 
            _generatedHelpLabel.Text = SR.GetString(SR.ExpressionBindingsDialog_GeneratedExpression);
 
            ImageList imageList = new ImageList(); 
            imageList.TransparentColor = Color.Fuchsia;
            imageList.ColorDepth = ColorDepth.Depth32Bit; 
            imageList.Images.AddStrip(new Bitmap(typeof(ExpressionBindingsDialog), "ExpressionBindableProperties.bmp"));
            _bindablePropsTree.ImageList = imageList;
        }
 
        private void LoadBindableProperties() {
            PropertyDescriptorCollection propDescs = TypeDescriptor.GetProperties(Control.GetType(), BindablePropertiesFilter); 
 
            string defaultPropName = null;
            PropertyDescriptor defaultProperty = TypeDescriptor.GetDefaultProperty(Control.GetType()); 
            if (defaultProperty != null) {
                defaultPropName = defaultProperty.Name;
            }
 
            TreeNodeCollection nodes = _bindablePropsTree.Nodes;
 
            // Load the current bindings 
            ExpressionBindingCollection currentExpressionBindings = ((IExpressionsAccessor)Control).Expressions;
            Hashtable bindings = new Hashtable(StringComparer.OrdinalIgnoreCase); 
            foreach (ExpressionBinding binding in currentExpressionBindings) {
                bindings[binding.PropertyName] = binding;
            }
 
            TreeNode selectedNode = null;
            foreach (PropertyDescriptor pd in propDescs) { 
                if (String.Compare(pd.Name, "ID", StringComparison.OrdinalIgnoreCase) == 0) { 
                    continue;
                } 

                ExpressionBinding eb = null;;
                if (bindings.Contains(pd.Name)) {
                    eb = (ExpressionBinding)bindings[pd.Name]; 
                    bindings.Remove(pd.Name);
                } 
                TreeNode node = new BindablePropertyNode(pd, eb); 

                if (pd.Name.Equals(defaultPropName, StringComparison.OrdinalIgnoreCase)) { 
                    selectedNode = node;
                }

                nodes.Add(node); 
            }
 
            // Since we already removed all the simple property bindings from 
            // the hash table, all we're left with is the complex property bindings.
            _complexBindings = bindings; 

            if ((selectedNode == null) && (nodes.Count != 0)) {
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
        }

        private void LoadExpressionEditors() {
            _expressionEditors = new HybridDictionary(true); 

 
            // Get the expression editors from config 
            IWebApplication webApp = (IWebApplication)ServiceProvider.GetService(typeof(IWebApplication));
            if (webApp != null) { 

                try {
                    Configuration config = webApp.OpenWebConfiguration(true);
                    if (config != null) { 
                        // Get the compilation config section to get the list of expressionbuilders
                        CompilationSection compilationSection = (CompilationSection)config.GetSection("system.web/compilation"); 
                        ExpressionBuilderCollection builders = compilationSection.ExpressionBuilders; 
                        foreach (ExpressionBuilder expressionBuilder in builders) {
                            string prefix = expressionBuilder.ExpressionPrefix; 
                            ExpressionEditor editor = ExpressionEditor.GetExpressionEditor(prefix, ServiceProvider);
                            if (editor != null) {
                                _expressionEditors[prefix] = editor;
                                _expressionBuilderComboBox.Items.Add(new ExpressionItem(prefix)); 
                            }
                        } 
                    } 
                }
                catch { 
                    // Ignore config exceptions
                }
                _expressionBuilderComboBox.InvalidateDropDownWidth();
            } 

            _expressionBuilderComboBox.Items.Add(NoneItem); 
        } 

        private void OnBindablePropsTreeAfterSelect(object sender, TreeViewEventArgs e) { 
            BindablePropertyNode selectedNode = (BindablePropertyNode)_bindablePropsTree.SelectedNode;
            if (_currentNode != selectedNode) {
                _currentNode = selectedNode;
                if (_currentNode != null && _currentNode.IsBound) { 
                    // If this node bound, load the appropriate editor and editor sheet
                    ExpressionBinding binding = _currentNode.Binding; 
                    Debug.Assert(binding != null, "Binding should be available if IsBound is true"); 
                    if (!_currentNode.IsGenerated) {
                        ExpressionEditor newExpressionEditor = (ExpressionEditor)_expressionEditors[binding.ExpressionPrefix]; 
                        if (newExpressionEditor == null) {
                            UIServiceHelper.ShowMessage(ServiceProvider, SR.GetString(SR.ExpressionBindingsDialog_UndefinedExpressionPrefix, binding.ExpressionPrefix), SR.GetString(SR.ExpressionBindingsDialog_Text, Control.Site.Name), MessageBoxButtons.OK);
                            newExpressionEditor = new GenericExpressionEditor();
                        } 

                        _currentEditor = newExpressionEditor; 
                        _currentSheet = _currentEditor.GetExpressionEditorSheet(binding.Expression, ServiceProvider); 
                        _internalChange = true;
                        try { 
                            foreach (ExpressionItem item in _expressionBuilderComboBox.Items) {
                                if (String.Equals(item.ToString(), binding.ExpressionPrefix, StringComparison.OrdinalIgnoreCase)) {
                                    _expressionBuilderComboBox.SelectedItem = item;
                                } 
                            }
                            _currentNode.IsValid = _currentSheet.IsValid; 
                        } 
                        finally {
                            _internalChange = false; 
                        }
                    }
                }
                else { 
                    // The tree either has no selected node, or the selected node is not bound
                    _expressionBuilderComboBox.SelectedItem = NoneItem; 
                    _currentEditor = null; 
                    _currentSheet = null;
                } 

                _expressionBuilderPropertyGrid.SelectedObject = _currentSheet;
                UpdateUIState();
            } 
        }
 
        private void OnExpressionBuilderComboBoxSelectedIndexChanged(object sender, EventArgs e) { 
            if (_internalChange) {
                return; 
            }

            _currentSheet = null;
 
            if (_expressionBuilderComboBox.SelectedItem != NoneItem) {
                _currentEditor = (ExpressionEditor)_expressionEditors[_expressionBuilderComboBox.SelectedItem.ToString()]; 
 
                if (_currentNode != null) {
                    if (_currentNode.IsBound) { 
                        // If there is an existing binding for this expression type, use it
                        ExpressionBinding binding = _currentNode.Binding;
                        Debug.Assert(binding != null, "Binding should be available if IsBound is true");
                        if (_expressionEditors[binding.ExpressionPrefix] == _currentEditor) { 
                            _currentSheet = _currentEditor.GetExpressionEditorSheet(binding.Expression, ServiceProvider);
                        } 
                    } 

                    // Otherwise, create a new sheet for a new expression 
                    if (_currentSheet == null) {
                        _currentSheet = _currentEditor.GetExpressionEditorSheet(String.Empty, ServiceProvider);
                    }
                    _currentNode.IsValid = _currentSheet.IsValid; 
                }
            } 
 
            SaveCurrentExpressionBinding();
 
            _expressionBuilderPropertyGrid.SelectedObject = _currentSheet;
            UpdateUIState();
        }
 
        private void OnExpressionBuilderPropertyGridPropertyValueChanged(object sender, PropertyValueChangedEventArgs e) {
            SaveCurrentExpressionBinding(); 
            UpdateUIState(); 
        }
 
        protected override void OnInitialActivated(EventArgs e) {
            base.OnInitialActivated(e);

            LoadExpressionEditors(); 
            LoadBindableProperties();
 
            UpdateUIState(); 
        }
 
        private void OnOKButtonClick(object sender, EventArgs e) {
            if (_bindingsDirty) {
                // If the bindings were changed, save the changes
                ExpressionBindingCollection ebc = ((IExpressionsAccessor)Control).Expressions; 
                DataBindingCollection dbc = ((IDataBindingsAccessor)Control).DataBindings;
                ebc.Clear(); 
                foreach (BindablePropertyNode node in _bindablePropsTree.Nodes) { 
                    if (node.IsBound) {
                        ebc.Add(node.Binding); 

                        // If we are adding a new expression binding but there was
                        // already a databinding, then we have to remove that
                        // databinding so that they don't conflict. 
                        if (dbc.Contains(node.Binding.PropertyName)) {
                            dbc.Remove(node.Binding.PropertyName); 
                        } 
                    }
                } 
                // Also save all the complex property bindings back since they
                // are not editable through this UI.
                foreach (ExpressionBinding binding in _complexBindings.Values) {
                    ebc.Add(binding); 
                }
            } 
 
            DialogResult = DialogResult.OK;
            Close(); 
        }

        private void SaveCurrentExpressionBinding() {
            if (_expressionBuilderComboBox.SelectedItem == NoneItem) { 
                // If None is selected, remove the binding
                _currentNode.Binding = null; 
                _currentNode.IsValid = true; 
            }
            else { 
                Debug.Assert(_currentNode != null);

                string expression = _currentSheet.GetExpression();
                PropertyDescriptor propDesc = _currentNode.PropertyDescriptor; 
                string propName = propDesc.Name;
                ExpressionBinding binding = new ExpressionBinding(propName, propDesc.PropertyType, 
                                                                    _expressionBuilderComboBox.SelectedItem.ToString(), expression); 
                _currentNode.Binding = binding;
                _currentNode.IsValid = _currentSheet.IsValid; 
            }

            _bindingsDirty = true;
        } 

        private void UpdateUIState() { 
            if (_currentNode == null) { 
                _expressionBuilderComboBox.Enabled = false;
                _expressionBuilderPropertyGrid.Enabled = false; 
                _propertiesPanel.Visible = true;
                _generatedHelpLabel.Visible = false;
            }
            else { 
                _expressionBuilderComboBox.Enabled = true;
                bool noneItemSelected = (_expressionBuilderComboBox.SelectedItem == NoneItem); 
                _expressionBuilderPropertyGrid.Enabled = !noneItemSelected; 
                _propertyGridLabel.Enabled = !noneItemSelected;
                _propertiesPanel.Visible = !_currentNode.IsGenerated; 
                _generatedHelpLabel.Visible = _currentNode.IsGenerated;
            }

            _okButton.Enabled = true; 
            foreach (BindablePropertyNode node in _bindablePropsTree.Nodes) {
                if (!node.IsValid) { 
                    _okButton.Enabled = false; 
                    break;
                } 
            }
        }

 
        private sealed class ExpressionItem {
            private string _prefix; 
 
            public ExpressionItem(string prefix) {
                _prefix = prefix; 
            }

            public override string ToString() {
                return _prefix; 
            }
        } 
 
        private sealed class BindablePropertyNode : TreeNode {
 
            private PropertyDescriptor _propDesc;
            private ExpressionBinding _binding;
            private bool _isValid;
 
            public BindablePropertyNode(PropertyDescriptor propDesc, ExpressionBinding binding) {
                // NOTE: The generated parameter indicates that the property is implicitly 
                // associated with an expression. Since these generated expressions have the 
                // higheset precedence in terms of property setters, they cannot be overridden
                // at design-time. 
                _binding = binding;
                _propDesc = propDesc;

                // We assume that all properties are valid when we initially load them 
                _isValid = true;
 
                Text = propDesc.Name; 

                ImageIndex = SelectedImageIndex = (IsBound ? (IsGenerated ? ImplicitBoundImageIndex : BoundImageIndex) : UnboundImageIndex); 
            }

            public bool IsBound {
                get { 
                    return (_binding != null);
                } 
            } 

            public bool IsGenerated { 
                get {
                    return (_binding == null ? false : _binding.Generated);
                }
            } 

            public bool IsValid { 
                get { 
                    return _isValid;
                } 
                set {
                    _isValid = value;
                }
            } 

            public ExpressionBinding Binding { 
                get { 
                    return _binding;
                } 
                set {
                    _binding = value;
                    ImageIndex = SelectedImageIndex = (IsBound ? BoundImageIndex : UnboundImageIndex);
                } 
            }
 
            public PropertyDescriptor PropertyDescriptor { 
                get {
                    return _propDesc; 
                }
            }
        }
 
        private sealed class GenericExpressionEditor : ExpressionEditor {
            public override object EvaluateExpression(string expression, object parsedExpressionData, Type propertyType, IServiceProvider serviceProvider) { 
                return String.Empty; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ExpressionBindingsDialogcs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Configuration;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.Web.Configuration; 
    using System.Web.UI;
    using System.Web.UI.Design.Util;
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 

    using WebUIControl = System.Web.UI.Control; 
 
    internal sealed class ExpressionBindingsDialog : DesignerForm {
 
        private static readonly Attribute[] BindablePropertiesFilter =
            new Attribute[] { BrowsableAttribute.Yes, ReadOnlyAttribute.No };

        private const int UnboundImageIndex = 0; 
        private const int BoundImageIndex = 1;
        private const int ImplicitBoundImageIndex = 2; 
 
        private System.Windows.Forms.Label _instructionLabel;
        private System.Windows.Forms.Label _bindablePropsLabels; 
        private System.Windows.Forms.TreeView _bindablePropsTree;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private AutoSizeComboBox _expressionBuilderComboBox; 
        private PropertyGrid _expressionBuilderPropertyGrid;
        private Label _expressionBuilderLabel; 
        private Label _propertyGridLabel; 
        private System.Windows.Forms.Panel _propertiesPanel;
        private System.Windows.Forms.Label _generatedHelpLabel; 

        private WebUIControl _control;
        private string _controlID;
        private bool _bindingsDirty; 

        private ExpressionItem _noneItem; 
        private BindablePropertyNode _currentNode; 
        private ExpressionEditor _currentEditor;
        private ExpressionEditorSheet _currentSheet; 

        private IDictionary _expressionEditors;

        private bool _internalChange; 

        // We hold on to all the complex property bindings because they are 
        // not represented in the list of bindable properties. This way we 
        // don't lose them when the user makes any changes.
        private IDictionary _complexBindings; 

        public ExpressionBindingsDialog(IServiceProvider serviceProvider, WebUIControl control)
            : base(serviceProvider) {
            Debug.Assert(control != null); 
            Debug.Assert(control.Site != null);
 
            _control = control; 
            _controlID = control.ID;
 
            // Setup the user interface
            InitializeComponent();
            InitializeUserInterface();
        } 

        private ExpressionItem NoneItem { 
            get { 
                if (_noneItem == null) {
                    _noneItem = new ExpressionItem(SR.GetString(SR.ExpressionBindingsDialog_None)); 
                }
                return _noneItem;
            }
        } 

        private WebUIControl Control { 
            get { 
                return _control;
            } 
        }

        protected override string HelpTopic {
            get { 
                return "net.Asp.Expressions.BindingsDialog";
            } 
        } 

        #region Windows Form Designer generated code 
        private void InitializeComponent() {
            _instructionLabel = new System.Windows.Forms.Label();
            _bindablePropsLabels = new System.Windows.Forms.Label();
            _bindablePropsTree = new System.Windows.Forms.TreeView(); 
            _okButton = new System.Windows.Forms.Button();
            _cancelButton = new System.Windows.Forms.Button(); 
            _expressionBuilderComboBox = new AutoSizeComboBox(); 
            _expressionBuilderPropertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider);
            _expressionBuilderLabel = new Label(); 
            _propertyGridLabel = new Label();
            _propertiesPanel = new System.Windows.Forms.Panel();
            _generatedHelpLabel = new System.Windows.Forms.Label();
            SuspendLayout(); 
            //
            // _instructionLabel 
            // 
            _instructionLabel.Location = new System.Drawing.Point(12, 12);
            _instructionLabel.Name = "_instructionLabel"; 
            _instructionLabel.Size = new System.Drawing.Size(476, 36);
            _instructionLabel.TabIndex = 0;
            //
            // _bindablePropsLabels 
            //
            _bindablePropsLabels.Location = new System.Drawing.Point(12, 52); 
            _bindablePropsLabels.Name = "_bindablePropsLabels"; 
            _bindablePropsLabels.Size = new System.Drawing.Size(196, 16);
            _bindablePropsLabels.TabIndex = 1; 
            //
            // _bindablePropsTree
            //
            _bindablePropsTree.HideSelection = false; 
            _bindablePropsTree.ImageIndex = -1;
            _bindablePropsTree.Location = new System.Drawing.Point(12, 70); 
            _bindablePropsTree.Name = "_bindablePropsTree"; 
            _bindablePropsTree.SelectedImageIndex = -1;
            _bindablePropsTree.Sorted = true; 
            _bindablePropsTree.ShowLines = false;
            _bindablePropsTree.ShowPlusMinus = false;
            _bindablePropsTree.ShowRootLines = false;
            _bindablePropsTree.Size = new System.Drawing.Size(196, 182); 
            _bindablePropsTree.TabIndex = 2;
            _bindablePropsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(OnBindablePropsTreeAfterSelect); 
            // 
            // _okButton
            // 
            _okButton.Location = new System.Drawing.Point(312, 262);
            _okButton.Name = "_okButton";
            _okButton.TabIndex = 16;
            _okButton.Size = new System.Drawing.Size(85, 23); 
            _okButton.Click += new System.EventHandler(OnOKButtonClick);
            // 
            // _cancelButton 
            //
            _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            _cancelButton.Location = new System.Drawing.Point(403, 262);
            _cancelButton.Name = "_cancelButton";
            _cancelButton.Size = new System.Drawing.Size(85, 23);
            _cancelButton.TabIndex = 17; 
            //
            // _expressionBuilderLabel 
            // 
            _expressionBuilderLabel.Location = new System.Drawing.Point(0, 0);
            _expressionBuilderLabel.Name = "_expressionBuilderLabel"; 
            _expressionBuilderLabel.Size = new System.Drawing.Size(268, 16);
            _expressionBuilderLabel.TabIndex = 10;
            //
            // _expressionBuilderComboBox 
            //
            _expressionBuilderComboBox.DropDownStyle = ComboBoxStyle.DropDownList; 
            _expressionBuilderComboBox.Location = new System.Drawing.Point(0, 18); 
            _expressionBuilderComboBox.Name = "_expressionBuilderComboBox";
            _expressionBuilderComboBox.TabIndex = 20; 
            _expressionBuilderComboBox.Size = new Size(268, 21);
            _expressionBuilderComboBox.Sorted = true;
            _expressionBuilderComboBox.SelectedIndexChanged += new EventHandler(OnExpressionBuilderComboBoxSelectedIndexChanged);
            // 
            // _propertyGridLabel
            // 
            _propertyGridLabel.Location = new System.Drawing.Point(0, 43); 
            _propertyGridLabel.Name = "_propertyGridLabel";
            _propertyGridLabel.Size = new System.Drawing.Size(268, 16); 
            _propertyGridLabel.TabIndex = 30;
            //
            // _expressionBuilderPropertyGrid
            // 
            _expressionBuilderPropertyGrid.Location = new System.Drawing.Point(0, 61);
            _expressionBuilderPropertyGrid.Name = "_expressionBuilderPropertyGrid"; 
            _expressionBuilderPropertyGrid.TabIndex = 40; 
            _expressionBuilderPropertyGrid.Size = new Size(268, 139);
            _expressionBuilderPropertyGrid.PropertySort = PropertySort.Alphabetical; 
            _expressionBuilderPropertyGrid.ToolbarVisible = false;
            _expressionBuilderPropertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(OnExpressionBuilderPropertyGridPropertyValueChanged);
            _expressionBuilderPropertyGrid.Site = _control.Site;
            // 
            // _propertiesPanel
            // 
            _propertiesPanel.Location = new System.Drawing.Point(220, 52); 
            _propertiesPanel.Name = "_propertiesPanel";
            _propertiesPanel.Size = new System.Drawing.Size(268, 200); 
            _propertiesPanel.TabIndex = 5;
            _propertiesPanel.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          _expressionBuilderLabel,
                                                                          _expressionBuilderComboBox, 
                                                                          _propertyGridLabel,
                                                                          _expressionBuilderPropertyGrid}); 
            // 
            // _generatedHelpLabel
            // 
            _generatedHelpLabel.Location = new System.Drawing.Point(220, 72);
            _generatedHelpLabel.Name = "_generatedHelpLabel";
            _generatedHelpLabel.Size = new System.Drawing.Size(268, 180);
            _generatedHelpLabel.TabIndex = 5; 
            //
            // ExpressionBindingsDialog 
            // 
            CancelButton = _cancelButton;
            ClientSize = new System.Drawing.Size(500, 297); 
            Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          _cancelButton,
                                                                          _okButton,
                                                                          _propertiesPanel, 
                                                                          _bindablePropsTree,
                                                                          _bindablePropsLabels, 
                                                                          _instructionLabel, 
                                                                          _generatedHelpLabel});
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; 
            Name = "ExpressionBindingsDialog";

            InitializeForm();
 
            ResumeLayout(false);
 
        } 
        #endregion
 
        private void InitializeUserInterface() {
            string controlSiteName = String.Empty;
            if ((Control != null) && (Control.Site != null)) {
                controlSiteName = Control.Site.Name; 
            }
            Text = SR.GetString(SR.ExpressionBindingsDialog_Text, controlSiteName); 
 
            _instructionLabel.Text = SR.GetString(SR.ExpressionBindingsDialog_Inst);
            _bindablePropsLabels.Text = SR.GetString(SR.ExpressionBindingsDialog_BindableProps); 
            _okButton.Text = SR.GetString(SR.ExpressionBindingsDialog_OK);
            _cancelButton.Text = SR.GetString(SR.ExpressionBindingsDialog_Cancel);
            _expressionBuilderLabel.Text = SR.GetString(SR.ExpressionBindingsDialog_ExpressionType);
            _propertyGridLabel.Text = SR.GetString(SR.ExpressionBindingsDialog_Properties); 
            _generatedHelpLabel.Text = SR.GetString(SR.ExpressionBindingsDialog_GeneratedExpression);
 
            ImageList imageList = new ImageList(); 
            imageList.TransparentColor = Color.Fuchsia;
            imageList.ColorDepth = ColorDepth.Depth32Bit; 
            imageList.Images.AddStrip(new Bitmap(typeof(ExpressionBindingsDialog), "ExpressionBindableProperties.bmp"));
            _bindablePropsTree.ImageList = imageList;
        }
 
        private void LoadBindableProperties() {
            PropertyDescriptorCollection propDescs = TypeDescriptor.GetProperties(Control.GetType(), BindablePropertiesFilter); 
 
            string defaultPropName = null;
            PropertyDescriptor defaultProperty = TypeDescriptor.GetDefaultProperty(Control.GetType()); 
            if (defaultProperty != null) {
                defaultPropName = defaultProperty.Name;
            }
 
            TreeNodeCollection nodes = _bindablePropsTree.Nodes;
 
            // Load the current bindings 
            ExpressionBindingCollection currentExpressionBindings = ((IExpressionsAccessor)Control).Expressions;
            Hashtable bindings = new Hashtable(StringComparer.OrdinalIgnoreCase); 
            foreach (ExpressionBinding binding in currentExpressionBindings) {
                bindings[binding.PropertyName] = binding;
            }
 
            TreeNode selectedNode = null;
            foreach (PropertyDescriptor pd in propDescs) { 
                if (String.Compare(pd.Name, "ID", StringComparison.OrdinalIgnoreCase) == 0) { 
                    continue;
                } 

                ExpressionBinding eb = null;;
                if (bindings.Contains(pd.Name)) {
                    eb = (ExpressionBinding)bindings[pd.Name]; 
                    bindings.Remove(pd.Name);
                } 
                TreeNode node = new BindablePropertyNode(pd, eb); 

                if (pd.Name.Equals(defaultPropName, StringComparison.OrdinalIgnoreCase)) { 
                    selectedNode = node;
                }

                nodes.Add(node); 
            }
 
            // Since we already removed all the simple property bindings from 
            // the hash table, all we're left with is the complex property bindings.
            _complexBindings = bindings; 

            if ((selectedNode == null) && (nodes.Count != 0)) {
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
        }

        private void LoadExpressionEditors() {
            _expressionEditors = new HybridDictionary(true); 

 
            // Get the expression editors from config 
            IWebApplication webApp = (IWebApplication)ServiceProvider.GetService(typeof(IWebApplication));
            if (webApp != null) { 

                try {
                    Configuration config = webApp.OpenWebConfiguration(true);
                    if (config != null) { 
                        // Get the compilation config section to get the list of expressionbuilders
                        CompilationSection compilationSection = (CompilationSection)config.GetSection("system.web/compilation"); 
                        ExpressionBuilderCollection builders = compilationSection.ExpressionBuilders; 
                        foreach (ExpressionBuilder expressionBuilder in builders) {
                            string prefix = expressionBuilder.ExpressionPrefix; 
                            ExpressionEditor editor = ExpressionEditor.GetExpressionEditor(prefix, ServiceProvider);
                            if (editor != null) {
                                _expressionEditors[prefix] = editor;
                                _expressionBuilderComboBox.Items.Add(new ExpressionItem(prefix)); 
                            }
                        } 
                    } 
                }
                catch { 
                    // Ignore config exceptions
                }
                _expressionBuilderComboBox.InvalidateDropDownWidth();
            } 

            _expressionBuilderComboBox.Items.Add(NoneItem); 
        } 

        private void OnBindablePropsTreeAfterSelect(object sender, TreeViewEventArgs e) { 
            BindablePropertyNode selectedNode = (BindablePropertyNode)_bindablePropsTree.SelectedNode;
            if (_currentNode != selectedNode) {
                _currentNode = selectedNode;
                if (_currentNode != null && _currentNode.IsBound) { 
                    // If this node bound, load the appropriate editor and editor sheet
                    ExpressionBinding binding = _currentNode.Binding; 
                    Debug.Assert(binding != null, "Binding should be available if IsBound is true"); 
                    if (!_currentNode.IsGenerated) {
                        ExpressionEditor newExpressionEditor = (ExpressionEditor)_expressionEditors[binding.ExpressionPrefix]; 
                        if (newExpressionEditor == null) {
                            UIServiceHelper.ShowMessage(ServiceProvider, SR.GetString(SR.ExpressionBindingsDialog_UndefinedExpressionPrefix, binding.ExpressionPrefix), SR.GetString(SR.ExpressionBindingsDialog_Text, Control.Site.Name), MessageBoxButtons.OK);
                            newExpressionEditor = new GenericExpressionEditor();
                        } 

                        _currentEditor = newExpressionEditor; 
                        _currentSheet = _currentEditor.GetExpressionEditorSheet(binding.Expression, ServiceProvider); 
                        _internalChange = true;
                        try { 
                            foreach (ExpressionItem item in _expressionBuilderComboBox.Items) {
                                if (String.Equals(item.ToString(), binding.ExpressionPrefix, StringComparison.OrdinalIgnoreCase)) {
                                    _expressionBuilderComboBox.SelectedItem = item;
                                } 
                            }
                            _currentNode.IsValid = _currentSheet.IsValid; 
                        } 
                        finally {
                            _internalChange = false; 
                        }
                    }
                }
                else { 
                    // The tree either has no selected node, or the selected node is not bound
                    _expressionBuilderComboBox.SelectedItem = NoneItem; 
                    _currentEditor = null; 
                    _currentSheet = null;
                } 

                _expressionBuilderPropertyGrid.SelectedObject = _currentSheet;
                UpdateUIState();
            } 
        }
 
        private void OnExpressionBuilderComboBoxSelectedIndexChanged(object sender, EventArgs e) { 
            if (_internalChange) {
                return; 
            }

            _currentSheet = null;
 
            if (_expressionBuilderComboBox.SelectedItem != NoneItem) {
                _currentEditor = (ExpressionEditor)_expressionEditors[_expressionBuilderComboBox.SelectedItem.ToString()]; 
 
                if (_currentNode != null) {
                    if (_currentNode.IsBound) { 
                        // If there is an existing binding for this expression type, use it
                        ExpressionBinding binding = _currentNode.Binding;
                        Debug.Assert(binding != null, "Binding should be available if IsBound is true");
                        if (_expressionEditors[binding.ExpressionPrefix] == _currentEditor) { 
                            _currentSheet = _currentEditor.GetExpressionEditorSheet(binding.Expression, ServiceProvider);
                        } 
                    } 

                    // Otherwise, create a new sheet for a new expression 
                    if (_currentSheet == null) {
                        _currentSheet = _currentEditor.GetExpressionEditorSheet(String.Empty, ServiceProvider);
                    }
                    _currentNode.IsValid = _currentSheet.IsValid; 
                }
            } 
 
            SaveCurrentExpressionBinding();
 
            _expressionBuilderPropertyGrid.SelectedObject = _currentSheet;
            UpdateUIState();
        }
 
        private void OnExpressionBuilderPropertyGridPropertyValueChanged(object sender, PropertyValueChangedEventArgs e) {
            SaveCurrentExpressionBinding(); 
            UpdateUIState(); 
        }
 
        protected override void OnInitialActivated(EventArgs e) {
            base.OnInitialActivated(e);

            LoadExpressionEditors(); 
            LoadBindableProperties();
 
            UpdateUIState(); 
        }
 
        private void OnOKButtonClick(object sender, EventArgs e) {
            if (_bindingsDirty) {
                // If the bindings were changed, save the changes
                ExpressionBindingCollection ebc = ((IExpressionsAccessor)Control).Expressions; 
                DataBindingCollection dbc = ((IDataBindingsAccessor)Control).DataBindings;
                ebc.Clear(); 
                foreach (BindablePropertyNode node in _bindablePropsTree.Nodes) { 
                    if (node.IsBound) {
                        ebc.Add(node.Binding); 

                        // If we are adding a new expression binding but there was
                        // already a databinding, then we have to remove that
                        // databinding so that they don't conflict. 
                        if (dbc.Contains(node.Binding.PropertyName)) {
                            dbc.Remove(node.Binding.PropertyName); 
                        } 
                    }
                } 
                // Also save all the complex property bindings back since they
                // are not editable through this UI.
                foreach (ExpressionBinding binding in _complexBindings.Values) {
                    ebc.Add(binding); 
                }
            } 
 
            DialogResult = DialogResult.OK;
            Close(); 
        }

        private void SaveCurrentExpressionBinding() {
            if (_expressionBuilderComboBox.SelectedItem == NoneItem) { 
                // If None is selected, remove the binding
                _currentNode.Binding = null; 
                _currentNode.IsValid = true; 
            }
            else { 
                Debug.Assert(_currentNode != null);

                string expression = _currentSheet.GetExpression();
                PropertyDescriptor propDesc = _currentNode.PropertyDescriptor; 
                string propName = propDesc.Name;
                ExpressionBinding binding = new ExpressionBinding(propName, propDesc.PropertyType, 
                                                                    _expressionBuilderComboBox.SelectedItem.ToString(), expression); 
                _currentNode.Binding = binding;
                _currentNode.IsValid = _currentSheet.IsValid; 
            }

            _bindingsDirty = true;
        } 

        private void UpdateUIState() { 
            if (_currentNode == null) { 
                _expressionBuilderComboBox.Enabled = false;
                _expressionBuilderPropertyGrid.Enabled = false; 
                _propertiesPanel.Visible = true;
                _generatedHelpLabel.Visible = false;
            }
            else { 
                _expressionBuilderComboBox.Enabled = true;
                bool noneItemSelected = (_expressionBuilderComboBox.SelectedItem == NoneItem); 
                _expressionBuilderPropertyGrid.Enabled = !noneItemSelected; 
                _propertyGridLabel.Enabled = !noneItemSelected;
                _propertiesPanel.Visible = !_currentNode.IsGenerated; 
                _generatedHelpLabel.Visible = _currentNode.IsGenerated;
            }

            _okButton.Enabled = true; 
            foreach (BindablePropertyNode node in _bindablePropsTree.Nodes) {
                if (!node.IsValid) { 
                    _okButton.Enabled = false; 
                    break;
                } 
            }
        }

 
        private sealed class ExpressionItem {
            private string _prefix; 
 
            public ExpressionItem(string prefix) {
                _prefix = prefix; 
            }

            public override string ToString() {
                return _prefix; 
            }
        } 
 
        private sealed class BindablePropertyNode : TreeNode {
 
            private PropertyDescriptor _propDesc;
            private ExpressionBinding _binding;
            private bool _isValid;
 
            public BindablePropertyNode(PropertyDescriptor propDesc, ExpressionBinding binding) {
                // NOTE: The generated parameter indicates that the property is implicitly 
                // associated with an expression. Since these generated expressions have the 
                // higheset precedence in terms of property setters, they cannot be overridden
                // at design-time. 
                _binding = binding;
                _propDesc = propDesc;

                // We assume that all properties are valid when we initially load them 
                _isValid = true;
 
                Text = propDesc.Name; 

                ImageIndex = SelectedImageIndex = (IsBound ? (IsGenerated ? ImplicitBoundImageIndex : BoundImageIndex) : UnboundImageIndex); 
            }

            public bool IsBound {
                get { 
                    return (_binding != null);
                } 
            } 

            public bool IsGenerated { 
                get {
                    return (_binding == null ? false : _binding.Generated);
                }
            } 

            public bool IsValid { 
                get { 
                    return _isValid;
                } 
                set {
                    _isValid = value;
                }
            } 

            public ExpressionBinding Binding { 
                get { 
                    return _binding;
                } 
                set {
                    _binding = value;
                    ImageIndex = SelectedImageIndex = (IsBound ? BoundImageIndex : UnboundImageIndex);
                } 
            }
 
            public PropertyDescriptor PropertyDescriptor { 
                get {
                    return _propDesc; 
                }
            }
        }
 
        private sealed class GenericExpressionEditor : ExpressionEditor {
            public override object EvaluateExpression(string expression, object parsedExpressionData, Type propertyType, IServiceProvider serviceProvider) { 
                return String.Empty; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
