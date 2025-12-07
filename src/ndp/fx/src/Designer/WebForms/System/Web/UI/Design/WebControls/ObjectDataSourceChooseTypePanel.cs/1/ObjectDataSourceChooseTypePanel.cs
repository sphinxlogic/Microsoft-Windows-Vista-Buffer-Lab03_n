//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceChooseTypePanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.Text; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    /// <devdoc>
    /// Wizard panel for choosing a type for an ObjectDataSource. 
    /// </devdoc>
    internal sealed class ObjectDataSourceChooseTypePanel : WizardPanel { 
        private const string CompareAllValuesFormatString = "original_{0}"; 

        private System.Windows.Forms.TextBox _typeNameTextBox; 
        private System.Windows.Forms.CheckBox _filterCheckBox;
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.Label _nameLabel;
        private System.Windows.Forms.Label _exampleLabel; 
        private AutoSizeComboBox _typeNameComboBox;
 
        private ObjectDataSource _objectDataSource; 
        private ObjectDataSourceDesigner _objectDataSourceDesigner;
        private Type _previousSelectedType; 
        private bool _discoveryServiceMode;

        private System.Collections.Generic.List<TypeItem> _typeItems;
 
        /// <devdoc>
        /// Creates a new ObjectDataSourceChooseTypePanel. 
        /// </devdoc> 
        public ObjectDataSourceChooseTypePanel(ObjectDataSourceDesigner objectDataSourceDesigner) {
            Debug.Assert(objectDataSourceDesigner != null); 
            _objectDataSourceDesigner = objectDataSourceDesigner;
            _objectDataSource = (ObjectDataSource)_objectDataSourceDesigner.Component;
            Debug.Assert(_objectDataSource != null);
 
            InitializeComponent();
            InitializeUI(); 
 

            ITypeDiscoveryService typeDiscoveryService = null; 
            if (_objectDataSource.Site != null) {
                typeDiscoveryService = (ITypeDiscoveryService)_objectDataSource.Site.GetService(typeof(ITypeDiscoveryService));
            }
 
            _discoveryServiceMode = (typeDiscoveryService != null);
 
            // Only show one type name editor depending on the availability of the service 
            if (_discoveryServiceMode) {
                _typeNameTextBox.Visible = false; 
                _exampleLabel.Visible = false;

                // Populate list of available types
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor; 
 
                    ICollection types = System.Windows.Forms.Design.DesignerUtils.FilterGenericTypes(
                        typeDiscoveryService.GetTypes(typeof(object), true /* excludeGlobalTypes */)); 

                    _typeNameComboBox.BeginUpdate();
                    if (types != null) {
                        StringCollection hiddenTypeNames = new StringCollection(); 
                        hiddenTypeNames.Add("My.MyApplication");
                        hiddenTypeNames.Add("My.MyComputer"); 
                        hiddenTypeNames.Add("My.MyProject"); 
                        hiddenTypeNames.Add("My.MyUser");
 
                        _typeItems = new System.Collections.Generic.List<TypeItem>(types.Count);

                        bool foundDataObject = false;
 
                        // Find all the types and check whether each one is filtered or not
                        foreach (Type t in types) { 
                            if (!t.IsEnum && !t.IsInterface) { 
                                // Check if the type is decorated with the DataObjectAttribute
                                object[] attrs = t.GetCustomAttributes(typeof(DataObjectAttribute), true); 
                                if ((attrs.Length > 0) && (((DataObjectAttribute)attrs[0]).IsDataObject)) {
                                    _typeItems.Add(new TypeItem(t, true));
                                    foundDataObject = true;
                                } 
                                else {
                                    // Type is not decorated, only add it if it's not a hidden type 
                                    if (!hiddenTypeNames.Contains(t.FullName)) { 
                                        _typeItems.Add(new TypeItem(t, false));
                                    } 
                                }
                            }
                        }
 
                        // Get user filter preference from state service and show list of available types
                        object filterState = _objectDataSourceDesigner.ShowOnlyDataComponentsState; 
                        if (filterState == null) { 
                            // If there is no previous user preference, filter
                            // automatically if there is a data component 
                            _filterCheckBox.Checked = foundDataObject;
                        }
                        else {
                            // If there is a previous user preference, just use it 
                            _filterCheckBox.Checked = (bool)filterState;
                        } 
                        UpdateTypeList(); 
                    }
                } 
                finally {
                    _typeNameComboBox.EndUpdate();
                    Cursor.Current = originalCursor;
                } 
            }
            else { 
                _typeNameComboBox.Visible = false; 
                _filterCheckBox.Visible = false;
            } 

            // Initialize the UI to reflect the current type
            TypeName = _objectDataSource.TypeName;
        } 

        private string TypeName { 
            get { 
                TypeItem currentItem = SelectedTypeItem;
                if (currentItem != null) { 
                    return currentItem.TypeName;
                }
                else {
                    return String.Empty; 
                }
            } 
            set { 
                if (_discoveryServiceMode) {
                    // Search for item in the list 
                    foreach (TypeItem item in _typeNameComboBox.Items) {
                        if (String.Compare(item.TypeName, value, StringComparison.OrdinalIgnoreCase) == 0) {
                            _typeNameComboBox.SelectedItem = item;
                            break; 
                        }
                    } 
                    // If the selected type is not in the list, add custom TypeItem 
                    if ((_typeNameComboBox.SelectedItem == null) && (value.Length > 0)) {
                        TypeItem customItem = new TypeItem(value, true); 
                        _typeItems.Add(customItem);
                        UpdateTypeList();
                        _typeNameComboBox.SelectedItem = customItem;
                    } 
                }
                else { 
                    _typeNameTextBox.Text = value; 
                }
            } 
        }

        private TypeItem SelectedTypeItem {
            get { 
                if (_discoveryServiceMode) {
                    return _typeNameComboBox.SelectedItem as TypeItem; 
                } 
                else {
                    return new TypeItem(_typeNameTextBox.Text, false); 
                }
            }
        }
 

        #region Designer generated code 
        private void InitializeComponent() { 
            this._helpLabel = new System.Windows.Forms.Label();
            this._nameLabel = new System.Windows.Forms.Label(); 
            this._exampleLabel = new System.Windows.Forms.Label();
            this._typeNameTextBox = new System.Windows.Forms.TextBox();
            this._typeNameComboBox = new AutoSizeComboBox();
            this._filterCheckBox = new System.Windows.Forms.CheckBox(); 
            this.SuspendLayout();
 
            // 
            // _helpLabel
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(0, 0);
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(544, 60); 
            this._helpLabel.TabIndex = 10;
 
            // 
            // _nameLabel
            // 
            this._nameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._nameLabel.Location = new System.Drawing.Point(0, 68);
            this._nameLabel.Name = "_nameLabel";
            this._nameLabel.Size = new System.Drawing.Size(544, 16); 
            this._nameLabel.TabIndex = 20;
 
            // 
            // _typeNameTextBox
            // 
            this._typeNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._typeNameTextBox.Location = new System.Drawing.Point(0, 86);
            this._typeNameTextBox.Name = "_typeNameTextBox";
            this._typeNameTextBox.Size = new System.Drawing.Size(300, 20); 
            this._typeNameTextBox.TabIndex = 30;
            this._typeNameTextBox.TextChanged += new System.EventHandler(this.OnTypeNameTextBoxTextChanged); 
 
            //
            // _typeNameComboBox 
            //
            this._typeNameComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._typeNameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._typeNameComboBox.Location = new System.Drawing.Point(0, 86); 
            this._typeNameComboBox.Name = "_typeNameComboBox";
            this._typeNameComboBox.Size = new System.Drawing.Size(300, 21); 
            this._typeNameComboBox.Sorted = true; 
            this._typeNameComboBox.TabIndex = 30;
            this._typeNameComboBox.SelectedIndexChanged += new System.EventHandler(this.OnTypeNameComboBoxSelectedIndexChanged); 

            //
            // _filterCheckBox
            // 
            this._filterCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this._filterCheckBox.Location = new System.Drawing.Point(306, 86); 
            this._filterCheckBox.Name = "_filterCheckBox"; 
            this._filterCheckBox.Size = new System.Drawing.Size(200, 18);
            this._filterCheckBox.TabIndex = 50; 
            this._filterCheckBox.CheckedChanged += new System.EventHandler(this.OnFilterCheckBoxCheckedChanged);

            //
            // _exampleLabel 
            //
            this._exampleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
            this._exampleLabel.ForeColor = System.Drawing.SystemColors.GrayText; 
            this._exampleLabel.Location = new System.Drawing.Point(0, 122);
            this._exampleLabel.Name = "_exampleLabel"; 
            this._exampleLabel.Size = new System.Drawing.Size(544, 16);
            this._exampleLabel.TabIndex = 60;

            // 
            // ObjectDataSourceChooseTypePanel
            // 
            this.Controls.Add(this._filterCheckBox); 
            this.Controls.Add(this._typeNameComboBox);
            this.Controls.Add(this._typeNameTextBox); 
            this.Controls.Add(this._exampleLabel);
            this.Controls.Add(this._nameLabel);
            this.Controls.Add(this._helpLabel);
            this.Name = "ObjectDataSourceChooseTypePanel"; 
            this.Size = new System.Drawing.Size(544, 274);
            this.ResumeLayout(false); 
            this.PerformLayout(); 
        }
        #endregion 

        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() { 
            Caption = SR.GetString(SR.ObjectDataSourceChooseTypePanel_PanelCaption); 

            _helpLabel.Text = SR.GetString(SR.ObjectDataSourceChooseTypePanel_HelpLabel); 
            _nameLabel.Text = SR.GetString(SR.ObjectDataSourceChooseTypePanel_NameLabel);
            _exampleLabel.Text = SR.GetString(SR.ObjectDataSourceChooseTypePanel_ExampleLabel);
            _filterCheckBox.Text = SR.GetString(SR.ObjectDataSourceChooseTypePanel_FilterCheckBox);
        } 

        /// <devdoc> 
        /// Called when the user click Finish on the wizard. 
        /// </devdoc>
        protected internal override void OnComplete() { 
            // We use the property descriptors to reset and set values to
            // make sure we clear out any databindings or expressions that
            // may be set. However, we only set properties if they have
            // changed in order to try to preserve any previous settings. 

            PropertyDescriptor propDesc; 
 
            if (_objectDataSource.TypeName != TypeName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["TypeName"]; 
                propDesc.SetValue(_objectDataSource, TypeName);
            }

            // If the user selected a DataSet (DataComponent) then we need to 
            // adjust the OldValuesParameterFormatString to be compatible with it.
            // If the item is "Filtered" that means it is a DataComponent 
            if (SelectedTypeItem != null && SelectedTypeItem.Filtered) { 
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["OldValuesParameterFormatString"];
                propDesc.SetValue(_objectDataSource, CompareAllValuesFormatString); 
            }

            // Store user's preference for data component filtering
            _objectDataSourceDesigner.ShowOnlyDataComponentsState = _filterCheckBox.Checked; 
        }
 
        /// <devdoc> 
        /// </devdoc>
        private void OnFilterCheckBoxCheckedChanged(object sender, System.EventArgs e) { 
            UpdateTypeList();
        }

        /// <devdoc> 
        /// </devdoc>
        public override bool OnNext() { 
            // Check if the type is accessible 
            TypeItem selectedItem = SelectedTypeItem;
            Debug.Assert(selectedItem != null, "Selected item should not be null"); 

            Type selectedType = selectedItem.Type;
            if (selectedType == null) {
                // If we don't have a Type object, we have to try to resolve the type 
                ITypeResolutionService typeResolver = (ITypeResolutionService)ServiceProvider.GetService(typeof(ITypeResolutionService));
                Debug.Assert(typeResolver != null, "ITypeResolutionService must be present for this wizard to run."); 
                if (typeResolver == null) { 
                    return false;
                } 

                try {
                    selectedType = typeResolver.GetType(selectedItem.TypeName, true, true);
                } 
                catch (Exception ex) {
                    UIServiceHelper.ShowError(ServiceProvider, ex, SR.GetString(SR.ObjectDataSourceDesigner_CannotGetType, selectedItem.TypeName)); 
                    return false; 
                }
            } 

            if (selectedType == null) {
                Debug.Fail("Could not load selected type");
                return false; 
            }
 
            if (selectedType != _previousSelectedType) { 
                // If the type has changed (or if this is the first time the
                // user hit next), set the type for the Method Chooser panel. 
                ObjectDataSourceChooseMethodsPanel methodsPanel = NextPanel as ObjectDataSourceChooseMethodsPanel;
                Debug.Assert(methodsPanel != null, "Choose Method panel should not be null");
                methodsPanel.SetType(selectedType);
                _previousSelectedType = selectedType; 
            }
            return true; 
        } 

        /// <devdoc> 
        /// </devdoc>
        public override void OnPrevious() {
        }
 
        /// <devdoc>
        /// </devdoc> 
        private void OnTypeNameComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateEnabledState();
        } 

        /// <devdoc>
        /// </devdoc>
        private void OnTypeNameTextBoxTextChanged(object sender, System.EventArgs e) { 
            UpdateEnabledState();
        } 
 
        /// <devdoc>
        /// </devdoc> 
        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);

            if (Visible) { 
                UpdateEnabledState();
            } 
        } 

        private void UpdateEnabledState() { 
            if (ParentWizard != null) {
                ParentWizard.FinishButton.Enabled = false;
                if (_discoveryServiceMode) {
                    ParentWizard.NextButton.Enabled = (_typeNameComboBox.SelectedItem != null); 
                }
                else { 
                    ParentWizard.NextButton.Enabled = (_typeNameTextBox.Text.Length > 0); 
                }
            } 
        }

        /// <devdoc>
        /// Updates the type combobox with a list of available types, taking 
        /// into consideration the user's preference for filtering data components.
        /// </devdoc> 
        private void UpdateTypeList() { 
            object oldSelection = _typeNameComboBox.SelectedItem;
            try { 
                _typeNameComboBox.BeginUpdate();

                _typeNameComboBox.Items.Clear();
                bool filter = _filterCheckBox.Checked; 
                foreach (TypeItem item in _typeItems) {
                    if (filter) { 
                        if (item.Filtered) { 
                            _typeNameComboBox.Items.Add(item);
                        } 
                    }
                    else {
                        _typeNameComboBox.Items.Add(item);
                    } 
                }
            } 
            finally { 
                _typeNameComboBox.EndUpdate();
            } 

            // Attempt to restore selection (this may fail if the item is now filtered out)
            _typeNameComboBox.SelectedItem = oldSelection;
            UpdateEnabledState(); 
            _typeNameComboBox.InvalidateDropDownWidth();
        } 
 

        /// <devdoc> 
        /// Represents a type a user can select.
        /// </devdoc>
        private sealed class TypeItem {
            private string _prettyTypeName; 
            private string _typeName;
            private Type _type; 
            private bool _filtered; 

            public TypeItem(string typeName, bool filtered) { 
                _typeName = typeName;
                _prettyTypeName = _typeName;
                _type = null;
                _filtered = filtered; 
            }
 
            public TypeItem(Type type, bool filtered) { 
                StringBuilder sb = new StringBuilder(64);
                ObjectDataSourceMethodEditor.AppendTypeName(type, true, sb); 
                _prettyTypeName = sb.ToString();
                _typeName = type.FullName;
                _type = type;
                _filtered = filtered; 
            }
 
            public bool Filtered { 
                get {
                    return _filtered; 
                }
            }

            public string TypeName { 
                get {
                    return _typeName; 
                } 
            }
 
            public Type Type {
                get {
                    return _type;
                } 
            }
 
            public override string ToString() { 
                return _prettyTypeName;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceChooseTypePanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.Text; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    /// <devdoc>
    /// Wizard panel for choosing a type for an ObjectDataSource. 
    /// </devdoc>
    internal sealed class ObjectDataSourceChooseTypePanel : WizardPanel { 
        private const string CompareAllValuesFormatString = "original_{0}"; 

        private System.Windows.Forms.TextBox _typeNameTextBox; 
        private System.Windows.Forms.CheckBox _filterCheckBox;
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.Label _nameLabel;
        private System.Windows.Forms.Label _exampleLabel; 
        private AutoSizeComboBox _typeNameComboBox;
 
        private ObjectDataSource _objectDataSource; 
        private ObjectDataSourceDesigner _objectDataSourceDesigner;
        private Type _previousSelectedType; 
        private bool _discoveryServiceMode;

        private System.Collections.Generic.List<TypeItem> _typeItems;
 
        /// <devdoc>
        /// Creates a new ObjectDataSourceChooseTypePanel. 
        /// </devdoc> 
        public ObjectDataSourceChooseTypePanel(ObjectDataSourceDesigner objectDataSourceDesigner) {
            Debug.Assert(objectDataSourceDesigner != null); 
            _objectDataSourceDesigner = objectDataSourceDesigner;
            _objectDataSource = (ObjectDataSource)_objectDataSourceDesigner.Component;
            Debug.Assert(_objectDataSource != null);
 
            InitializeComponent();
            InitializeUI(); 
 

            ITypeDiscoveryService typeDiscoveryService = null; 
            if (_objectDataSource.Site != null) {
                typeDiscoveryService = (ITypeDiscoveryService)_objectDataSource.Site.GetService(typeof(ITypeDiscoveryService));
            }
 
            _discoveryServiceMode = (typeDiscoveryService != null);
 
            // Only show one type name editor depending on the availability of the service 
            if (_discoveryServiceMode) {
                _typeNameTextBox.Visible = false; 
                _exampleLabel.Visible = false;

                // Populate list of available types
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor; 
 
                    ICollection types = System.Windows.Forms.Design.DesignerUtils.FilterGenericTypes(
                        typeDiscoveryService.GetTypes(typeof(object), true /* excludeGlobalTypes */)); 

                    _typeNameComboBox.BeginUpdate();
                    if (types != null) {
                        StringCollection hiddenTypeNames = new StringCollection(); 
                        hiddenTypeNames.Add("My.MyApplication");
                        hiddenTypeNames.Add("My.MyComputer"); 
                        hiddenTypeNames.Add("My.MyProject"); 
                        hiddenTypeNames.Add("My.MyUser");
 
                        _typeItems = new System.Collections.Generic.List<TypeItem>(types.Count);

                        bool foundDataObject = false;
 
                        // Find all the types and check whether each one is filtered or not
                        foreach (Type t in types) { 
                            if (!t.IsEnum && !t.IsInterface) { 
                                // Check if the type is decorated with the DataObjectAttribute
                                object[] attrs = t.GetCustomAttributes(typeof(DataObjectAttribute), true); 
                                if ((attrs.Length > 0) && (((DataObjectAttribute)attrs[0]).IsDataObject)) {
                                    _typeItems.Add(new TypeItem(t, true));
                                    foundDataObject = true;
                                } 
                                else {
                                    // Type is not decorated, only add it if it's not a hidden type 
                                    if (!hiddenTypeNames.Contains(t.FullName)) { 
                                        _typeItems.Add(new TypeItem(t, false));
                                    } 
                                }
                            }
                        }
 
                        // Get user filter preference from state service and show list of available types
                        object filterState = _objectDataSourceDesigner.ShowOnlyDataComponentsState; 
                        if (filterState == null) { 
                            // If there is no previous user preference, filter
                            // automatically if there is a data component 
                            _filterCheckBox.Checked = foundDataObject;
                        }
                        else {
                            // If there is a previous user preference, just use it 
                            _filterCheckBox.Checked = (bool)filterState;
                        } 
                        UpdateTypeList(); 
                    }
                } 
                finally {
                    _typeNameComboBox.EndUpdate();
                    Cursor.Current = originalCursor;
                } 
            }
            else { 
                _typeNameComboBox.Visible = false; 
                _filterCheckBox.Visible = false;
            } 

            // Initialize the UI to reflect the current type
            TypeName = _objectDataSource.TypeName;
        } 

        private string TypeName { 
            get { 
                TypeItem currentItem = SelectedTypeItem;
                if (currentItem != null) { 
                    return currentItem.TypeName;
                }
                else {
                    return String.Empty; 
                }
            } 
            set { 
                if (_discoveryServiceMode) {
                    // Search for item in the list 
                    foreach (TypeItem item in _typeNameComboBox.Items) {
                        if (String.Compare(item.TypeName, value, StringComparison.OrdinalIgnoreCase) == 0) {
                            _typeNameComboBox.SelectedItem = item;
                            break; 
                        }
                    } 
                    // If the selected type is not in the list, add custom TypeItem 
                    if ((_typeNameComboBox.SelectedItem == null) && (value.Length > 0)) {
                        TypeItem customItem = new TypeItem(value, true); 
                        _typeItems.Add(customItem);
                        UpdateTypeList();
                        _typeNameComboBox.SelectedItem = customItem;
                    } 
                }
                else { 
                    _typeNameTextBox.Text = value; 
                }
            } 
        }

        private TypeItem SelectedTypeItem {
            get { 
                if (_discoveryServiceMode) {
                    return _typeNameComboBox.SelectedItem as TypeItem; 
                } 
                else {
                    return new TypeItem(_typeNameTextBox.Text, false); 
                }
            }
        }
 

        #region Designer generated code 
        private void InitializeComponent() { 
            this._helpLabel = new System.Windows.Forms.Label();
            this._nameLabel = new System.Windows.Forms.Label(); 
            this._exampleLabel = new System.Windows.Forms.Label();
            this._typeNameTextBox = new System.Windows.Forms.TextBox();
            this._typeNameComboBox = new AutoSizeComboBox();
            this._filterCheckBox = new System.Windows.Forms.CheckBox(); 
            this.SuspendLayout();
 
            // 
            // _helpLabel
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.Location = new System.Drawing.Point(0, 0);
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(544, 60); 
            this._helpLabel.TabIndex = 10;
 
            // 
            // _nameLabel
            // 
            this._nameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._nameLabel.Location = new System.Drawing.Point(0, 68);
            this._nameLabel.Name = "_nameLabel";
            this._nameLabel.Size = new System.Drawing.Size(544, 16); 
            this._nameLabel.TabIndex = 20;
 
            // 
            // _typeNameTextBox
            // 
            this._typeNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._typeNameTextBox.Location = new System.Drawing.Point(0, 86);
            this._typeNameTextBox.Name = "_typeNameTextBox";
            this._typeNameTextBox.Size = new System.Drawing.Size(300, 20); 
            this._typeNameTextBox.TabIndex = 30;
            this._typeNameTextBox.TextChanged += new System.EventHandler(this.OnTypeNameTextBoxTextChanged); 
 
            //
            // _typeNameComboBox 
            //
            this._typeNameComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._typeNameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._typeNameComboBox.Location = new System.Drawing.Point(0, 86); 
            this._typeNameComboBox.Name = "_typeNameComboBox";
            this._typeNameComboBox.Size = new System.Drawing.Size(300, 21); 
            this._typeNameComboBox.Sorted = true; 
            this._typeNameComboBox.TabIndex = 30;
            this._typeNameComboBox.SelectedIndexChanged += new System.EventHandler(this.OnTypeNameComboBoxSelectedIndexChanged); 

            //
            // _filterCheckBox
            // 
            this._filterCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this._filterCheckBox.Location = new System.Drawing.Point(306, 86); 
            this._filterCheckBox.Name = "_filterCheckBox"; 
            this._filterCheckBox.Size = new System.Drawing.Size(200, 18);
            this._filterCheckBox.TabIndex = 50; 
            this._filterCheckBox.CheckedChanged += new System.EventHandler(this.OnFilterCheckBoxCheckedChanged);

            //
            // _exampleLabel 
            //
            this._exampleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
            this._exampleLabel.ForeColor = System.Drawing.SystemColors.GrayText; 
            this._exampleLabel.Location = new System.Drawing.Point(0, 122);
            this._exampleLabel.Name = "_exampleLabel"; 
            this._exampleLabel.Size = new System.Drawing.Size(544, 16);
            this._exampleLabel.TabIndex = 60;

            // 
            // ObjectDataSourceChooseTypePanel
            // 
            this.Controls.Add(this._filterCheckBox); 
            this.Controls.Add(this._typeNameComboBox);
            this.Controls.Add(this._typeNameTextBox); 
            this.Controls.Add(this._exampleLabel);
            this.Controls.Add(this._nameLabel);
            this.Controls.Add(this._helpLabel);
            this.Name = "ObjectDataSourceChooseTypePanel"; 
            this.Size = new System.Drawing.Size(544, 274);
            this.ResumeLayout(false); 
            this.PerformLayout(); 
        }
        #endregion 

        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() { 
            Caption = SR.GetString(SR.ObjectDataSourceChooseTypePanel_PanelCaption); 

            _helpLabel.Text = SR.GetString(SR.ObjectDataSourceChooseTypePanel_HelpLabel); 
            _nameLabel.Text = SR.GetString(SR.ObjectDataSourceChooseTypePanel_NameLabel);
            _exampleLabel.Text = SR.GetString(SR.ObjectDataSourceChooseTypePanel_ExampleLabel);
            _filterCheckBox.Text = SR.GetString(SR.ObjectDataSourceChooseTypePanel_FilterCheckBox);
        } 

        /// <devdoc> 
        /// Called when the user click Finish on the wizard. 
        /// </devdoc>
        protected internal override void OnComplete() { 
            // We use the property descriptors to reset and set values to
            // make sure we clear out any databindings or expressions that
            // may be set. However, we only set properties if they have
            // changed in order to try to preserve any previous settings. 

            PropertyDescriptor propDesc; 
 
            if (_objectDataSource.TypeName != TypeName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["TypeName"]; 
                propDesc.SetValue(_objectDataSource, TypeName);
            }

            // If the user selected a DataSet (DataComponent) then we need to 
            // adjust the OldValuesParameterFormatString to be compatible with it.
            // If the item is "Filtered" that means it is a DataComponent 
            if (SelectedTypeItem != null && SelectedTypeItem.Filtered) { 
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["OldValuesParameterFormatString"];
                propDesc.SetValue(_objectDataSource, CompareAllValuesFormatString); 
            }

            // Store user's preference for data component filtering
            _objectDataSourceDesigner.ShowOnlyDataComponentsState = _filterCheckBox.Checked; 
        }
 
        /// <devdoc> 
        /// </devdoc>
        private void OnFilterCheckBoxCheckedChanged(object sender, System.EventArgs e) { 
            UpdateTypeList();
        }

        /// <devdoc> 
        /// </devdoc>
        public override bool OnNext() { 
            // Check if the type is accessible 
            TypeItem selectedItem = SelectedTypeItem;
            Debug.Assert(selectedItem != null, "Selected item should not be null"); 

            Type selectedType = selectedItem.Type;
            if (selectedType == null) {
                // If we don't have a Type object, we have to try to resolve the type 
                ITypeResolutionService typeResolver = (ITypeResolutionService)ServiceProvider.GetService(typeof(ITypeResolutionService));
                Debug.Assert(typeResolver != null, "ITypeResolutionService must be present for this wizard to run."); 
                if (typeResolver == null) { 
                    return false;
                } 

                try {
                    selectedType = typeResolver.GetType(selectedItem.TypeName, true, true);
                } 
                catch (Exception ex) {
                    UIServiceHelper.ShowError(ServiceProvider, ex, SR.GetString(SR.ObjectDataSourceDesigner_CannotGetType, selectedItem.TypeName)); 
                    return false; 
                }
            } 

            if (selectedType == null) {
                Debug.Fail("Could not load selected type");
                return false; 
            }
 
            if (selectedType != _previousSelectedType) { 
                // If the type has changed (or if this is the first time the
                // user hit next), set the type for the Method Chooser panel. 
                ObjectDataSourceChooseMethodsPanel methodsPanel = NextPanel as ObjectDataSourceChooseMethodsPanel;
                Debug.Assert(methodsPanel != null, "Choose Method panel should not be null");
                methodsPanel.SetType(selectedType);
                _previousSelectedType = selectedType; 
            }
            return true; 
        } 

        /// <devdoc> 
        /// </devdoc>
        public override void OnPrevious() {
        }
 
        /// <devdoc>
        /// </devdoc> 
        private void OnTypeNameComboBoxSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateEnabledState();
        } 

        /// <devdoc>
        /// </devdoc>
        private void OnTypeNameTextBoxTextChanged(object sender, System.EventArgs e) { 
            UpdateEnabledState();
        } 
 
        /// <devdoc>
        /// </devdoc> 
        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);

            if (Visible) { 
                UpdateEnabledState();
            } 
        } 

        private void UpdateEnabledState() { 
            if (ParentWizard != null) {
                ParentWizard.FinishButton.Enabled = false;
                if (_discoveryServiceMode) {
                    ParentWizard.NextButton.Enabled = (_typeNameComboBox.SelectedItem != null); 
                }
                else { 
                    ParentWizard.NextButton.Enabled = (_typeNameTextBox.Text.Length > 0); 
                }
            } 
        }

        /// <devdoc>
        /// Updates the type combobox with a list of available types, taking 
        /// into consideration the user's preference for filtering data components.
        /// </devdoc> 
        private void UpdateTypeList() { 
            object oldSelection = _typeNameComboBox.SelectedItem;
            try { 
                _typeNameComboBox.BeginUpdate();

                _typeNameComboBox.Items.Clear();
                bool filter = _filterCheckBox.Checked; 
                foreach (TypeItem item in _typeItems) {
                    if (filter) { 
                        if (item.Filtered) { 
                            _typeNameComboBox.Items.Add(item);
                        } 
                    }
                    else {
                        _typeNameComboBox.Items.Add(item);
                    } 
                }
            } 
            finally { 
                _typeNameComboBox.EndUpdate();
            } 

            // Attempt to restore selection (this may fail if the item is now filtered out)
            _typeNameComboBox.SelectedItem = oldSelection;
            UpdateEnabledState(); 
            _typeNameComboBox.InvalidateDropDownWidth();
        } 
 

        /// <devdoc> 
        /// Represents a type a user can select.
        /// </devdoc>
        private sealed class TypeItem {
            private string _prettyTypeName; 
            private string _typeName;
            private Type _type; 
            private bool _filtered; 

            public TypeItem(string typeName, bool filtered) { 
                _typeName = typeName;
                _prettyTypeName = _typeName;
                _type = null;
                _filtered = filtered; 
            }
 
            public TypeItem(Type type, bool filtered) { 
                StringBuilder sb = new StringBuilder(64);
                ObjectDataSourceMethodEditor.AppendTypeName(type, true, sb); 
                _prettyTypeName = sb.ToString();
                _typeName = type.FullName;
                _type = type;
                _filtered = filtered; 
            }
 
            public bool Filtered { 
                get {
                    return _filtered; 
                }
            }

            public string TypeName { 
                get {
                    return _typeName; 
                } 
            }
 
            public Type Type {
                get {
                    return _type;
                } 
            }
 
            public override string ToString() { 
                return _prettyTypeName;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
