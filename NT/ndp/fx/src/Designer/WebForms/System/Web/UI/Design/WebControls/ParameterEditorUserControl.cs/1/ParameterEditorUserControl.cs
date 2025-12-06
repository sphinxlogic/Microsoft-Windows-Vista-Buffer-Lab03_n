//------------------------------------------------------------------------------ 
// <copyright file="ParameterEditorUserControl.cs" company="Microsoft">
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
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Collections.Generic;

    /// <devdoc> 
    /// A reusable UserControl for editing ParameterCollection objects.
    /// Enables a user to add/remove/reorder parameters and change their types. 
    /// </devdoc> 
    public class ParameterEditorUserControl : System.Windows.Forms.UserControl {
 
        private static readonly object EventParametersChanged = new object();

        private System.Windows.Forms.Label _parametersLabel;
        private System.Windows.Forms.ListView _parametersListView; 
        private AutoSizeComboBox _parameterTypeComboBox;
        private System.Windows.Forms.ColumnHeader _nameColumnHeader; 
        private System.Windows.Forms.ColumnHeader _valueColumnHeader; 
        private System.Windows.Forms.Button _moveUpButton;
        private System.Windows.Forms.Button _moveDownButton; 
        private System.Windows.Forms.Button _deleteParameterButton;
        private System.Windows.Forms.Button _addParameterButton;
        private System.Windows.Forms.Panel _addButtonPanel;
        private System.Windows.Forms.Label _sourceLabel; 
        private System.Windows.Forms.Panel _editorPanel;
 
        private ListDictionary _parameterTypes; 
        private IServiceProvider _serviceProvider;
        private ParameterEditor _parameterEditor; 
        private bool _inAdvancedMode;
        private int _ignoreParameterChangesCount;

        private AdvancedParameterEditor _advancedParameterEditor; 
        private ControlParameterEditor _controlParameterEditor;
        private CookieParameterEditor _cookieParameterEditor; 
        private FormParameterEditor _formParameterEditor; 
        private QueryStringParameterEditor _queryStringParameterEditor;
        private SessionParameterEditor _sessionParameterEditor; 
        private StaticParameterEditor _staticParameterEditor;
        private ProfileParameterEditor _profileParameterEditor;

        private System.Web.UI.Control _control; 

 
        /// <devdoc> 
        /// Creates a new ParameterEditorUserControl.
        /// </devdoc> 
        public ParameterEditorUserControl(IServiceProvider serviceProvider) : this(serviceProvider, null) {
        }

        internal ParameterEditorUserControl(IServiceProvider serviceProvider, System.Web.UI.Control control) { 
            _serviceProvider = serviceProvider;
            _control = control; 
 
            InitializeComponent();
            InitializeUI(); 
            InitializeParameterEditors();

            // Add types to drop down list
            CreateParameterList(); 
            foreach (DictionaryEntry de in _parameterTypes) {
                _parameterTypeComboBox.Items.Add(de.Value); 
            } 
            _parameterTypeComboBox.InvalidateDropDownWidth();
            // Refresh the UI 
            UpdateUI(false);
        }

        /// <devdoc> 
        /// Returns true if all the parameters in the editor are fully configured.
        /// For example if one of the parameters is a SessionParameter but its 
        /// SessionField property is not set, then it is not considered to be 
        /// configured.
        /// </devdoc> 
        public bool ParametersConfigured {
            get {
                foreach (ParameterListViewItem item in _parametersListView.Items) {
                    // VSWhidbey 412396 fixes the null items 
                    Debug.Assert(item != null, "A ListViewItem returned from the Items collection should not be null.");
                    if (item != null && !item.IsConfigured) { 
                        return false; 
                    }
                } 
                return true;
            }
        }
 
        /// <devdoc>
        /// Notifies listeners when the state of a Parameter has changed. For 
        /// example when a user changes a property of a Parameter this event 
        /// is raised.
        /// </devdoc> 
        public event EventHandler ParametersChanged {
            add {
                Events.AddHandler(EventParametersChanged, value);
            } 
            remove {
                Events.RemoveHandler(EventParametersChanged, value); 
            } 
        }
 
        /// <devdoc>
        /// Creates the internal list of parameter types.
        /// </devdoc>
        private void CreateParameterList() { 
            _parameterTypes = new ListDictionary();
            _parameterTypes.Add(typeof(Parameter), "None"); 
            _parameterTypes.Add(typeof(CookieParameter), "Cookie"); 
            _parameterTypes.Add(typeof(ControlParameter), "Control");
            _parameterTypes.Add(typeof(FormParameter), "Form"); 
            _parameterTypes.Add(typeof(ProfileParameter), "Profile");
            _parameterTypes.Add(typeof(QueryStringParameter), "QueryString");
            _parameterTypes.Add(typeof(SessionParameter), "Session");
        } 

        #region Component Designer generated code 
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent() {
            this._addButtonPanel = new System.Windows.Forms.Panel();
            this._addParameterButton = new System.Windows.Forms.Button(); 
            this._parametersLabel = new System.Windows.Forms.Label();
            this._sourceLabel = new System.Windows.Forms.Label(); 
            this._parametersListView = new System.Windows.Forms.ListView(); 
            this._nameColumnHeader = new System.Windows.Forms.ColumnHeader("");
            this._valueColumnHeader = new System.Windows.Forms.ColumnHeader(""); 
            this._parameterTypeComboBox = new AutoSizeComboBox();
            this._moveUpButton = new System.Windows.Forms.Button();
            this._moveDownButton = new System.Windows.Forms.Button();
            this._deleteParameterButton = new System.Windows.Forms.Button(); 
            this._editorPanel = new System.Windows.Forms.Panel();
            this._addButtonPanel.SuspendLayout(); 
            this.SuspendLayout(); 
            //
            // _parametersLabel 
            //
            this._parametersLabel.Location = new System.Drawing.Point(0, 0);
            this._parametersLabel.Name = "_parametersLabel";
            this._parametersLabel.Size = new System.Drawing.Size(252, 16); 
            this._parametersLabel.TabIndex = 10;
            // 
            // _parametersListView 
            //
            this._parametersListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)));
            this._parametersListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._nameColumnHeader,
            this._valueColumnHeader}); 
            this._parametersListView.FullRowSelect = true;
            this._parametersListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable; 
            this._parametersListView.HideSelection = false; 
            this._parametersListView.LabelEdit = true;
            this._parametersListView.Location = new System.Drawing.Point(0, 18); 
            this._parametersListView.MultiSelect = false;
            this._parametersListView.Name = "_parametersListView";
            this._parametersListView.Size = new System.Drawing.Size(252, 234);
            this._parametersListView.TabIndex = 20; 
            this._parametersListView.View = System.Windows.Forms.View.Details;
            this._parametersListView.SelectedIndexChanged += new System.EventHandler(this.OnParametersListViewSelectedIndexChanged); 
            this._parametersListView.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.OnParametersListViewAfterLabelEdit); 
            //
            // _nameColumnHeader 
            //
            this._nameColumnHeader.Width = 85;
            //
            // _valueColumnHeader 
            //
            this._valueColumnHeader.Width = 134; 
            // 
            // _addButtonPanel
            // 
            this._addButtonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._addButtonPanel.Controls.Add(this._addParameterButton);
            this._addButtonPanel.Location = new System.Drawing.Point(0, 258);
            this._addButtonPanel.Name = "_addButtonPanel"; 
            this._addButtonPanel.Size = new System.Drawing.Size(252, 23);
            this._addButtonPanel.TabIndex = 30; 
            // 
            // _addParameterButton
            // 
            this._addParameterButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._addParameterButton.AutoSize = true;
            this._addParameterButton.Location = new System.Drawing.Point(124, 0);
            this._addParameterButton.Name = "_addParameterButton"; 
            this._addParameterButton.Size = new System.Drawing.Size(128, 23);
            this._addParameterButton.TabIndex = 10; 
            this._addParameterButton.Click += new System.EventHandler(this.OnAddParameterButtonClick); 
            //
            // _moveUpButton 
            //
            this._moveUpButton.Location = new System.Drawing.Point(258, 18);
            this._moveUpButton.Name = "_moveUpButton";
            this._moveUpButton.Size = new System.Drawing.Size(26, 23); 
            this._moveUpButton.TabIndex = 40;
            this._moveUpButton.Click += new System.EventHandler(this.OnMoveUpButtonClick); 
            // 
            // _moveDownButton
            // 
            this._moveDownButton.Location = new System.Drawing.Point(258, 42);
            this._moveDownButton.Name = "_moveDownButton";
            this._moveDownButton.Size = new System.Drawing.Size(26, 23);
            this._moveDownButton.TabIndex = 50; 
            this._moveDownButton.Click += new System.EventHandler(this.OnMoveDownButtonClick);
            // 
            // _deleteParameterButton 
            //
            this._deleteParameterButton.Location = new System.Drawing.Point(258, 71); 
            this._deleteParameterButton.Name = "_deleteParameterButton";
            this._deleteParameterButton.Size = new System.Drawing.Size(26, 23);
            this._deleteParameterButton.TabIndex = 60;
            this._deleteParameterButton.Click += new System.EventHandler(this.OnDeleteParameterButtonClick); 
            //
            // _sourceLabel 
            // 
            this._sourceLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sourceLabel.Location = new System.Drawing.Point(292, 0);
            this._sourceLabel.Name = "_sourceLabel";
            this._sourceLabel.Size = new System.Drawing.Size(300, 16);
            this._sourceLabel.TabIndex = 70; 
            //
            // _parameterTypeComboBox 
            // 
            this._parameterTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._parameterTypeComboBox.Location = new System.Drawing.Point(292, 18); 
            this._parameterTypeComboBox.Name = "_parameterTypeComboBox";
            this._parameterTypeComboBox.Size = new System.Drawing.Size(163, 21);
            this._parameterTypeComboBox.TabIndex = 80;
            this._parameterTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnParameterTypeComboBoxSelectedIndexChanged); 
            //
            // _editorPanel 
            // 
            this._editorPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._editorPanel.Location = new System.Drawing.Point(292, 47);
            this._editorPanel.Name = "_editorPanel";
            this._editorPanel.Size = new System.Drawing.Size(308, 235); 
            this._editorPanel.TabIndex = 90;
            // 
            // ParameterEditorUserControl 
            //
            this.Controls.Add(this._editorPanel); 
            this.Controls.Add(this._addButtonPanel);
            this.Controls.Add(this._deleteParameterButton);
            this.Controls.Add(this._moveDownButton);
            this.Controls.Add(this._moveUpButton); 
            this.Controls.Add(this._parameterTypeComboBox);
            this.Controls.Add(this._parametersListView); 
            this.Controls.Add(this._sourceLabel); 
            this.Controls.Add(this._parametersLabel);
            this.MinimumSize = new System.Drawing.Size(460, 126); 
            this.Name = "ParameterEditorUserControl";
            this.Size = new System.Drawing.Size(600, 280);
            this._addButtonPanel.ResumeLayout(false);
            this._addButtonPanel.PerformLayout(); 
            this.ResumeLayout(false);
        } 
        #endregion 

        /// <devdoc> 
        /// Initializes the individual parameter editors and parents them to
        /// the form.
        /// </devdoc>
        private void InitializeParameterEditors() { 
            _advancedParameterEditor = new AdvancedParameterEditor(_serviceProvider, _control);
            _advancedParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _advancedParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _advancedParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_advancedParameterEditor); 

            _staticParameterEditor = new StaticParameterEditor(_serviceProvider);
            _staticParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode);
            _staticParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _staticParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_staticParameterEditor); 
 
            _controlParameterEditor = new ControlParameterEditor(_serviceProvider, _control);
            _controlParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _controlParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged);
            _controlParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_controlParameterEditor);
 
            _formParameterEditor = new FormParameterEditor(_serviceProvider);
            _formParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _formParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _formParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_formParameterEditor); 

            _queryStringParameterEditor = new QueryStringParameterEditor(_serviceProvider);
            _queryStringParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode);
            _queryStringParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _queryStringParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_queryStringParameterEditor); 
 
            _cookieParameterEditor = new CookieParameterEditor(_serviceProvider);
            _cookieParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _cookieParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged);
            _cookieParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_cookieParameterEditor);
 
            _sessionParameterEditor = new SessionParameterEditor(_serviceProvider);
            _sessionParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _sessionParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _sessionParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_sessionParameterEditor); 

            _profileParameterEditor = new ProfileParameterEditor(_serviceProvider);
            _profileParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode);
            _profileParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _profileParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_profileParameterEditor); 
        } 

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer.
        /// </devdoc>
        private void InitializeUI() { 
            _parametersLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParametersLabel);
            _nameColumnHeader.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterNameColumnHeader); 
            _valueColumnHeader.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterValueColumnHeader); 
            _addParameterButton.Text = SR.GetString(SR.ParameterEditorUserControl_AddButton);
            _sourceLabel.Text = SR.GetString(SR.ParameterEditorUserControl_SourceLabel); 

            Icon moveUpIcon = new Icon(typeof(ParameterEditorUserControl), "SortUp.ico");
            Bitmap moveUpBitmap = moveUpIcon.ToBitmap();
            moveUpBitmap.MakeTransparent(); 
            _moveUpButton.Image = moveUpBitmap;
 
            Icon moveDownIcon = new Icon(typeof(ParameterEditorUserControl), "SortDown.ico"); 
            Bitmap moveDownBitmap = moveDownIcon.ToBitmap();
            moveDownBitmap.MakeTransparent(); 
            _moveDownButton.Image = moveDownBitmap;

            Icon deleteIcon = new Icon(typeof(ParameterEditorUserControl), "Delete.ico");
            Bitmap deleteBitmap = deleteIcon.ToBitmap(); 
            deleteBitmap.MakeTransparent();
            _deleteParameterButton.Image = deleteBitmap; 
 
            // Accessibility names
            _moveUpButton.AccessibleName = SR.GetString(SR.ParameterEditorUserControl_MoveParameterUp); 
            _moveDownButton.AccessibleName = SR.GetString(SR.ParameterEditorUserControl_MoveParameterDown);
            _deleteParameterButton.AccessibleName = SR.GetString(SR.ParameterEditorUserControl_DeleteParameter);
        }
 
        /// <devdoc>
        /// Adds a new parameter with a given name. 
        /// </devdoc> 
        private void AddParameter(Parameter parameter) {
            try { 
                IgnoreParameterChanges(true);
                ParameterListViewItem item = new ParameterListViewItem(parameter);

                _parametersListView.BeginUpdate(); 
                try {
                    _parametersListView.Items.Add(item); 
                    // Automatically select new item 
                    item.Selected = true;
                    item.Focused = true; 
                    item.EnsureVisible();
                    _parametersListView.Focus();
                }
                finally { 
                    _parametersListView.EndUpdate();
                } 
 
                item.Refresh();
 
                // Allow user to edit parameter name immediately
                item.BeginEdit();

            } 
            finally {
                IgnoreParameterChanges(false); 
            } 
            OnParametersChanged(this, EventArgs.Empty);
        } 

        /// <devdoc>
        /// Adds an array of existing parameters.
        /// </devdoc> 
        public void AddParameters(Parameter[] parameters) {
            try { 
                IgnoreParameterChanges(true); 
                _parametersListView.BeginUpdate();
                ArrayList items = new ArrayList(); 
                try {
                    foreach (Parameter parameter in parameters) {
                        ParameterListViewItem item = new ParameterListViewItem(parameter);
                        _parametersListView.Items.Add(item); 
                        items.Add(item);
                    } 
                    // Automatically select first item 
                    if (_parametersListView.Items.Count > 0) {
                        _parametersListView.Items[0].Selected = true; 
                        _parametersListView.Items[0].Focused = true;
                        _parametersListView.Items[0].EnsureVisible();
                    }
                    _parametersListView.Focus(); 
                }
                finally { 
                    _parametersListView.EndUpdate(); 
                }
 
                foreach (ParameterListViewItem item in items) {
                    item.Refresh();
                }
            } 
            finally {
                IgnoreParameterChanges(false); 
            } 
            OnParametersChanged(this, EventArgs.Empty);
        } 

        /// <devdoc>
        /// Removes all parameters.
        /// </devdoc> 
        public void ClearParameters() {
            try { 
                IgnoreParameterChanges(true); 
                _parametersListView.Items.Clear();
 
                UpdateUI(false);
            }
            finally {
                IgnoreParameterChanges(false); 
            }
            OnParametersChanged(this, EventArgs.Empty); 
        } 

        internal static string GetControlDefaultValuePropertyName(string controlID, IServiceProvider serviceProvider, System.Web.UI.Control control) { 
            System.Web.UI.Control foundControl = ControlHelper.FindControl(serviceProvider, control, controlID);

            if (foundControl != null) {
                return GetDefaultValuePropertyName(foundControl); 
            }
 
            return String.Empty; 
        }
 
        /// <devdoc>
        /// Get the default property of a control, or return null if there is no default property.
        /// </devdoc>
        private static string GetDefaultValuePropertyName(System.Web.UI.Control control) { 
            ControlValuePropertyAttribute controlValueProp = (ControlValuePropertyAttribute)TypeDescriptor.GetAttributes(control)[typeof(ControlValuePropertyAttribute)];
            if ((controlValueProp != null) && !String.IsNullOrEmpty(controlValueProp.Name)) { 
                return controlValueProp.Name; 
            }
            else { 
                return String.Empty;
            }
        }
 
        /// <devdoc>
        /// Gets an expression indicating how the parameter gets its value. 
        /// </devdoc> 
        internal static string GetParameterExpression(IServiceProvider serviceProvider, Parameter p, System.Web.UI.Control control, out bool isHelperText) {
            Debug.Assert(p != null); 

            if (p.GetType() == typeof(ControlParameter)) {
                ControlParameter cp = (ControlParameter)p;
                if (cp.ControlID.Length == 0) { 
                    isHelperText = true;
                    return SR.GetString(SR.ParameterEditorUserControl_ControlParameterExpressionUnknown); 
                } 
                else {
                    string propertyName = cp.PropertyName; 
                    if (propertyName.Length == 0) {
                        propertyName = GetControlDefaultValuePropertyName(cp.ControlID, serviceProvider, control);
                    }
 
                    if (propertyName.Length > 0) {
                        isHelperText = false; 
                        return cp.ControlID + "." + propertyName; 
                    }
                    else { 
                        isHelperText = true;
                        return SR.GetString(SR.ParameterEditorUserControl_ControlParameterExpressionUnknown);
                    }
                } 
            }
            else if (p.GetType() == typeof(FormParameter)) { 
                FormParameter rp = (FormParameter)p; 
                if (rp.FormField.Length > 0) {
                    isHelperText = false; 
                    return String.Format(CultureInfo.InvariantCulture, "Request.Form(\"{0}\")", rp.FormField);
                }
                else {
                    isHelperText = true; 
                    return SR.GetString(SR.ParameterEditorUserControl_FormParameterExpressionUnknown);
                } 
            } 
            else if (p.GetType() == typeof(QueryStringParameter)) {
                QueryStringParameter qsp = (QueryStringParameter)p; 
                if (qsp.QueryStringField.Length > 0) {
                    isHelperText = false;
                    return String.Format(CultureInfo.InvariantCulture, "Request.QueryString(\"{0}\")", qsp.QueryStringField);
                } 
                else {
                    isHelperText = true; 
                    return SR.GetString(SR.ParameterEditorUserControl_QueryStringParameterExpressionUnknown); 
                }
            } 
            else if (p.GetType() == typeof(CookieParameter)) {
                CookieParameter cp = (CookieParameter)p;
                if (cp.CookieName.Length > 0) {
                    isHelperText = false; 
                    return String.Format(CultureInfo.InvariantCulture, "Request.Cookies(\"{0}\").Value", cp.CookieName);
                } 
                else { 
                    isHelperText = true;
                    return SR.GetString(SR.ParameterEditorUserControl_CookieParameterExpressionUnknown); 
                }
            }
            else if (p.GetType() == typeof(SessionParameter)) {
                SessionParameter sp = (SessionParameter)p; 
                if (sp.SessionField.Length > 0) {
                    isHelperText = false; 
                    return String.Format(CultureInfo.InvariantCulture, "Session(\"{0}\")", sp.SessionField); 
                }
                else { 
                    isHelperText = true;
                    return SR.GetString(SR.ParameterEditorUserControl_SessionParameterExpressionUnknown);
                }
            } 
            else if (p.GetType() == typeof(ProfileParameter)) {
                ProfileParameter pp = (ProfileParameter)p; 
                if (pp.PropertyName.Length > 0) { 
                    isHelperText = false;
                    return String.Format(CultureInfo.InvariantCulture, "Profile(\"{0}\")", pp.PropertyName); 
                }
                else {
                    isHelperText = true;
                    return SR.GetString(SR.ParameterEditorUserControl_ProfileParameterExpressionUnknown); 
                }
            } 
            else if (p.GetType() == typeof(Parameter)) { 
                Parameter sp = (Parameter)p;
                if (sp.DefaultValue == null) { 
                    isHelperText = false;
                    return String.Empty;
                }
                else { 
                    isHelperText = false;
                    return sp.DefaultValue; 
                } 
            }
            // Parameter is of a custom type, so we just show the type name 
            isHelperText = true;
            return p.GetType().Name;
        }
 
        /// <devdoc>
        /// Gets all parameters that have values (i.e. are not unassigned). 
        /// </devdoc> 
        public Parameter[] GetParameters() {
            ArrayList parameters = new ArrayList(); 
            foreach (ParameterListViewItem item in _parametersListView.Items) {
                if (item.Parameter != null) {
                    parameters.Add(item.Parameter);
                } 
            }
            return (Parameter[])parameters.ToArray(typeof(Parameter)); 
        } 

        private void IgnoreParameterChanges(bool ignoreChanges) { 
            _ignoreParameterChangesCount += (ignoreChanges ? 1 : -1);

            if (_ignoreParameterChangesCount == 0) {
                UpdateUI(false); 
            }
        } 
 
        private void OnAddParameterButtonClick(object sender, System.EventArgs e) {
            // 
            AddParameter(new Parameter("newparameter"));
        }

        private void OnDeleteParameterButtonClick(object sender, System.EventArgs e) { 
            try {
                IgnoreParameterChanges(true); 
 
                if (_parametersListView.SelectedItems.Count == 0) {
                    return; 
                }
                int index = _parametersListView.SelectedIndices[0];

                _parametersListView.BeginUpdate(); 
                try {
                    _parametersListView.Items.RemoveAt(index); 
 
                    if (index < _parametersListView.Items.Count) {
                        _parametersListView.Items[index].Selected = true; 
                        _parametersListView.Items[index].Focused = true;
                        _parametersListView.Items[index].EnsureVisible();
                        _parametersListView.Focus();
                    } 
                    else if (_parametersListView.Items.Count > 0) {
                        index = _parametersListView.Items.Count - 1; 
                        _parametersListView.Items[index].Selected = true; 
                        _parametersListView.Items[index].Focused = true;
                        _parametersListView.Items[index].EnsureVisible(); 
                        _parametersListView.Focus();
                    }
                }
                finally { 
                    _parametersListView.EndUpdate();
                } 
 
                UpdateUI(false);
            } 
            finally {
                IgnoreParameterChanges(false);
            }
            OnParametersChanged(this, EventArgs.Empty); 
        }
 
        private void OnMoveDownButtonClick(object sender, System.EventArgs e) { 
            try {
                IgnoreParameterChanges(true); 

                if (_parametersListView.SelectedItems.Count == 0) {
                    return;
                } 

                int index = _parametersListView.SelectedIndices[0]; 
                if (index == _parametersListView.Items.Count - 1) { 
                    return;
                } 

                _parametersListView.BeginUpdate();
                try {
                    // Swap item with the one below it 
                    ListViewItem itemMove = _parametersListView.Items[index];
                    itemMove.Remove(); 
                    _parametersListView.Items.Insert(index + 1, itemMove); 

                    itemMove.Selected = true; 
                    itemMove.Focused = true;
                    itemMove.EnsureVisible();
                    _parametersListView.Focus();
                } 
                finally {
                    _parametersListView.EndUpdate(); 
                } 
            }
            finally { 
                IgnoreParameterChanges(false);
            }
            OnParametersChanged(this, EventArgs.Empty);
        } 

        private void OnMoveUpButtonClick(object sender, System.EventArgs e) { 
            try { 
                IgnoreParameterChanges(true);
 
                if (_parametersListView.SelectedItems.Count == 0) {
                    return;
                }
 
                int index = _parametersListView.SelectedIndices[0];
                if (index == 0) { 
                    return; 
                }
 
                _parametersListView.BeginUpdate();
                try {
                    // Swap item with the one above it
                    ListViewItem itemMove = _parametersListView.Items[index]; 
                    itemMove.Remove();
                    _parametersListView.Items.Insert(index - 1, itemMove); 
 
                    itemMove.Selected = true;
                    itemMove.Focused = true; 
                    itemMove.EnsureVisible();
                    _parametersListView.Focus();
                }
                finally { 
                    _parametersListView.EndUpdate();
                } 
            } 
            finally {
                IgnoreParameterChanges(false); 
            }
            OnParametersChanged(this, EventArgs.Empty);
        }
 
        /// <devdoc>
        /// This method is called whenever the state of a parameter in the editor 
        /// changes. Calling this method also raises the ParametersChanged event 
        /// so that external listeners can be notified as well.
        /// </devdoc> 
        protected virtual void OnParametersChanged(object sender, EventArgs e) {
            if (_ignoreParameterChangesCount > 0) {
                return;
            } 
            EventHandler handler = Events[EventParametersChanged] as EventHandler;
            if (handler != null) { 
                handler(this, EventArgs.Empty); 
            }
        } 

        /// <devdoc>
        /// After Label Edit event handler for Parameters List View.
        /// </devdoc> 
        private void OnParametersListViewAfterLabelEdit(object sender, LabelEditEventArgs e) {
            if ((e.Label == null) || (e.Label.Trim().Length == 0)) { 
                e.CancelEdit = true; 
                return;
            } 
            ParameterListViewItem item = (ParameterListViewItem)_parametersListView.Items[e.Item];
            item.ParameterName = e.Label;
            UpdateUI(false);
        } 

        private void OnParametersListViewSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateUI(false); 
        }
 
        private void OnParameterTypeComboBoxSelectedIndexChanged(object sender, System.EventArgs e) {
            try {
                IgnoreParameterChanges(true);
 
                if (_parametersListView.SelectedItems.Count == 0) {
                    return; 
                } 

                ParameterListViewItem item = (ParameterListViewItem)_parametersListView.SelectedItems[0]; 

                string parameterTypeName = (string)_parameterTypeComboBox.SelectedItem;
                // Search for type name in list to determine the actual Type
                Type parameterType = null; 
                foreach (DictionaryEntry de in _parameterTypes) {
                    if ((string)de.Value == parameterTypeName) { 
                        parameterType = (Type)de.Key; 
                    }
                } 
                // If the type has changed, create a new instance of the type
                if ((parameterType != null) && ((item.Parameter == null) || (item.Parameter.GetType() != parameterType))) {
                    item.Parameter = (Parameter)Activator.CreateInstance(parameterType);
                    item.Refresh(); 
                }
                // Update editor 
                SetActiveEditParameterItem(item, false); 
            }
            finally { 
                IgnoreParameterChanges(false);
            }
            OnParametersChanged(this, EventArgs.Empty);
        } 

        private void SetActiveEditParameterItem(ParameterListViewItem parameterItem, bool allowFocusChange) { 
            if (parameterItem == null) { 
                // Destroy the current editor, if there is one
                if (_parameterEditor != null) { 
                    _parameterEditor.Visible = false;
                    _parameterEditor = null;
                }
            } 
            else {
                // Figure out the appropriate parameter editor for the new parameter 
                ParameterEditor newParameterEditor = null; 
                if (_inAdvancedMode) {
                    newParameterEditor = _advancedParameterEditor; 
                }
                else {
                    Debug.Assert(parameterItem.Parameter != null);
                    if (parameterItem.Parameter != null) { 
                        if (parameterItem.Parameter.GetType() == typeof(Parameter)) {
                            newParameterEditor = _staticParameterEditor; 
                        } 
                        else {
                            if (parameterItem.Parameter.GetType() == typeof(ControlParameter)) { 
                                newParameterEditor = _controlParameterEditor;
                            }
                            else {
                                if (parameterItem.Parameter.GetType() == typeof(FormParameter)) { 
                                    newParameterEditor = _formParameterEditor;
                                } 
                                else { 
                                    if (parameterItem.Parameter.GetType() == typeof(QueryStringParameter)) {
                                        newParameterEditor = _queryStringParameterEditor; 
                                    }
                                    else {
                                        if (parameterItem.Parameter.GetType() == typeof(CookieParameter)) {
                                            newParameterEditor = _cookieParameterEditor; 
                                        }
                                        else { 
                                            if (parameterItem.Parameter.GetType() == typeof(SessionParameter)) { 
                                                newParameterEditor = _sessionParameterEditor;
                                            } 
                                            else {
                                                if (parameterItem.Parameter.GetType() == typeof(ProfileParameter)) {
                                                    newParameterEditor = _profileParameterEditor;
                                                } 
                                                else {
                                                    // Unknown parameter... Do nothing? Perhaps have an UnknownParameterEditor without toggle 
                                                } 
                                            }
                                        } 
                                    }
                                }
                            }
                        } 
                    }
                } 
 
                // If the new editor is of a different type, swap editors
                if (_parameterEditor != newParameterEditor) { 
                    if (_parameterEditor != null) {
                        _parameterEditor.Visible = false;
                    }
                    _parameterEditor = newParameterEditor; 
                }
 
                // Initialize the new editor 
                if (_parameterEditor != null) {
                    _parameterEditor.InitializeParameter(parameterItem); 
                    _parameterEditor.Visible = true;
                    if (allowFocusChange) {
                        _parameterEditor.SetDefaultFocus();
                    } 
                }
            } 
        } 

        /// <devdoc> 
        /// Controls whether changes can be made to the parameter collection.
        /// This only disables the up/down/delete/add buttons - users can still edit the properties of existing parameters.
        /// </devdoc>
        public void SetAllowCollectionChanges(bool allowChanges) { 
            _moveUpButton.Visible = allowChanges;
            _moveDownButton.Visible = allowChanges; 
            _deleteParameterButton.Visible = allowChanges; 
            _addParameterButton.Visible = allowChanges;
        } 

        private void ToggleAdvancedMode(object sender, EventArgs e) {
            _inAdvancedMode = !_inAdvancedMode;
            UpdateUI(true); 
        }
 
        /// <devdoc> 
        /// Updates the UI to reflect the enabled state of buttons and the selected parameter type.
        /// </devdoc> 
        private void UpdateUI(bool allowFocusChange) {
            if (_parametersListView.SelectedItems.Count > 0) {
                ParameterListViewItem item = (ParameterListViewItem)_parametersListView.SelectedItems[0];
 
                _deleteParameterButton.Enabled = true;
                _moveUpButton.Enabled = (_parametersListView.SelectedIndices[0] > 0); 
                _moveDownButton.Enabled = (_parametersListView.SelectedIndices[0] < _parametersListView.Items.Count - 1); 
                _sourceLabel.Enabled = true;
                _parameterTypeComboBox.Enabled = true; 
                _editorPanel.Enabled = true;
                // Select the proper type in the drop down listbox
                if (item.Parameter == null) {
                    _parameterTypeComboBox.SelectedIndex = -1; 
                }
                else { 
                    Type t = item.Parameter.GetType(); 
                    object typeName = _parameterTypes[t];
 
                    if (typeName != null) {
                        _parameterTypeComboBox.SelectedItem = typeName;
                    }
                    else { 
                        _parameterTypeComboBox.SelectedIndex = -1;
                    } 
                } 
                SetActiveEditParameterItem(item, allowFocusChange);
            } 
            else {
                _deleteParameterButton.Enabled = false;
                _moveUpButton.Enabled = false;
                _moveDownButton.Enabled = false; 
                _sourceLabel.Enabled = false;
                _parameterTypeComboBox.Enabled = false; 
                _parameterTypeComboBox.SelectedIndex = -1; 

                _editorPanel.Enabled = false; 
                SetActiveEditParameterItem(null, false);
            }
        }
 
        internal sealed class ControlItem {
            private string _controlID; 
            private string _propertyName; 

            public ControlItem(string controlID, string propertyName) { 
                _controlID = controlID;
                _propertyName = propertyName;
            }
 
            public string ControlID {
                get { 
                    return _controlID; 
                }
            } 

            public string PropertyName {
                get {
                    return _propertyName; 
                }
            } 
 
            private static bool IsValidComponent(IComponent component) {
                System.Web.UI.Control control = component as System.Web.UI.Control; 
                if (control == null) {
                    return false;
                }
                if (String.IsNullOrEmpty(control.ID)) { 
                    return false;
                } 
                return true; 
            }
 
            /// <devdoc>
            /// Returns a list of all ControlItems representing the controls in the container.
            /// </devdoc>
            public static ControlItem[] GetControlItems(IDesignerHost host, System.Web.UI.Control control) { 

                IList<IComponent> allComponents = ControlHelper.GetAllComponents(control, new ControlHelper.IsValidComponentDelegate(IsValidComponent)); 
 
                List<ControlItem> items = new List<ControlItem>();
                foreach (System.Web.UI.Control c in allComponents) { 
                    string defaultPropertyName = GetDefaultValuePropertyName(c);
                    if (!String.IsNullOrEmpty(defaultPropertyName)) {
                        items.Add(new ControlItem(c.ID, defaultPropertyName));
                    } 
                }
 
                return items.ToArray(); 
            }
 
            public override string ToString() {
                return _controlID;
            }
        } 

        /// <devdoc> 
        /// A ListView item that represents a parameter object. 
        /// </devdoc>
        private class ParameterListViewItem : ListViewItem { 
            private Parameter _parameter;
            private bool _isConfigured;

            /// <devdoc> 
            /// Creates a new ParameterListViewItem with a given parameter.
            /// </devdoc> 
            public ParameterListViewItem(Parameter parameter) { 
                Debug.Assert(parameter != null);
                _parameter = parameter; 
                _isConfigured = true;
            }

            public bool IsConfigured { 
                get {
                    return _isConfigured; 
                } 
            }
 
            /// <devdoc>
            /// The name of the parameter.
            /// </devdoc>
            public string ParameterName { 
                get {
                    return _parameter.Name; 
                } 
                set {
                    _parameter.Name = value; 
                }
            }

            /// <devdoc> 
            /// The type of the parameter.
            /// </devdoc> 
            public TypeCode ParameterType { 
                get {
                    return _parameter.Type; 
                }
                set {
                    _parameter.Type = value;
                } 
            }
 
            /// <devdoc> 
            /// The parameter associated with this ListViewItem.
            /// </devdoc> 
            public Parameter Parameter {
                get {
                    return _parameter;
                } 
                set {
                    string defaultValue = _parameter.DefaultValue; 
                    ParameterDirection direction = _parameter.Direction; 
                    string name = _parameter.Name;
                    bool treatEmptyStringsAsNull = _parameter.ConvertEmptyStringToNull; 
                    int size = _parameter.Size;
                    TypeCode type = _parameter.Type;

                    _parameter = value; 

                    Debug.Assert(_parameter != null); 
                    _parameter.DefaultValue = defaultValue; 
                    _parameter.Direction = direction;
                    _parameter.Name = name; 
                    _parameter.ConvertEmptyStringToNull = treatEmptyStringsAsNull;
                    _parameter.Size = size;
                    _parameter.Type = type;
                } 
            }
 
            /// <devdoc> 
            /// Refreshes the properties of the ListViewItem.
            /// </devdoc> 
            public void Refresh() {
                SubItems.Clear();

                Text = ParameterName; 
                UseItemStyleForSubItems = false;
 
                bool isHelperText; 

                ListView listView = ListView; 
                IServiceProvider serviceProvider = null;
                System.Web.UI.Control control = null;
                if (listView != null) {
                    ParameterEditorUserControl parameterEditor = (ParameterEditorUserControl)listView.Parent; 
                    serviceProvider = parameterEditor._serviceProvider;
                    control = parameterEditor._control; 
                } 
                string parameterExpression = ParameterEditorUserControl.GetParameterExpression(serviceProvider, _parameter, control, out isHelperText);
                // If we get back helper text instead of an expression, that means 
                // the parameter is not fully configured. Typically this would cause
                // the containing UI to disable the user from progressing until
                // they fully configure all their parameters.
                _isConfigured = !isHelperText; 
                ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
                subItem.Text = parameterExpression; 
                if (isHelperText) { 
                    subItem.ForeColor = SystemColors.GrayText;
                } 
                SubItems.Add(subItem);
            }
        }
 
        /// <devdoc>
        /// An ISite for use in the property grid. This enables editors the property grid to 
        /// access the component's container. 
        /// </devdoc>
        private class PropertyGridSite : ISite { 
            private IServiceProvider _sp;
            private IComponent _comp;
            private bool _inGetService = false;
 
            public PropertyGridSite(IServiceProvider sp, IComponent comp) {
                _sp = sp; 
                _comp = comp; 
            }
 
            public IComponent Component {
                get {
                    return _comp;
                } 
            }
 
            public IContainer Container { 
                get {
                    return null; 
                }
            }

            public bool DesignMode { 
                get {
                    return false; 
                } 
            }
 
            public string Name {
                get {
                    return null;
                } 
                set {
                } 
            } 

            public object GetService(Type t) { 
                if ((!_inGetService) && (_sp != null)) {
                    try {
                        _inGetService = true;
                        return _sp.GetService(t); 
                    }
                    finally { 
                        _inGetService = false; 
                    }
                } 
                return null;
            }
        }
 
        /// <devdoc>
        /// An abstract base class for all parameter editors, including the advanced 
        /// property grid view, as well as the simple property editors. 
        /// </devdoc>
        private abstract class ParameterEditor : System.Windows.Forms.Panel { 
            private static readonly object EventParameterChanged = new object();
            private static readonly object EventRequestModeChange = new object();

            private IServiceProvider _serviceProvider; 
            private ParameterListViewItem _parameterItem;
 
            protected ParameterEditor(IServiceProvider serviceProvider) { 
                _serviceProvider = serviceProvider;
            } 

            protected ParameterListViewItem ParameterItem {
                get {
                    return _parameterItem; 
                }
            } 
 
            protected IServiceProvider ServiceProvider {
                get { 
                    return _serviceProvider;
                }
            }
 
            public event EventHandler ParameterChanged {
                add { 
                    Events.AddHandler(EventParameterChanged, value); 
                }
                remove { 
                    Events.RemoveHandler(EventParameterChanged, value);
                }
            }
 
            public event EventHandler RequestModeChange {
                add { 
                    Events.AddHandler(EventRequestModeChange, value); 
                }
                remove { 
                    Events.RemoveHandler(EventRequestModeChange, value);
                }
            }
 
            public virtual void InitializeParameter(ParameterListViewItem parameterItem) {
                _parameterItem = parameterItem; 
            } 

            /// <devdoc> 
            /// This method is called whenever the state of a parameter changes.
            /// It also raises the ParameterChanged event to notify external listeners.
            /// </devdoc>
            protected void OnParameterChanged() { 
                ParameterItem.Refresh();
 
                EventHandler handler = Events[EventParameterChanged] as EventHandler; 
                if (handler != null) {
                    handler(this, EventArgs.Empty); 
                }
            }

            protected void OnRequestModeChange() { 
                EventHandler handler = Events[EventRequestModeChange] as EventHandler;
                if (handler != null) { 
                    handler(this, EventArgs.Empty); 
                }
            } 

            public virtual void SetDefaultFocus() {
            }
        } 

        private sealed class AdvancedParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _advancedlabel; 
            private System.Windows.Forms.PropertyGrid _parameterPropertyGrid;
            private System.Windows.Forms.LinkLabel _hideAdvancedLinkLabel; 
            private System.Web.UI.Control _control;

            public AdvancedParameterEditor(IServiceProvider serviceProvider, System.Web.UI.Control control) : base(serviceProvider) {
                _control = control; 

                SuspendLayout(); 
                Size = new Size(400, 400); 

                _advancedlabel = new System.Windows.Forms.Label(); 
                _parameterPropertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider);
                _hideAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();

                _advancedlabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _advancedlabel.Location = new System.Drawing.Point(0, 0);
                _advancedlabel.Size = new System.Drawing.Size(400, 16); 
                _advancedlabel.TabIndex = 10; 
                _advancedlabel.Text = SR.GetString(SR.ParameterEditorUserControl_AdvancedProperties);
 
                _parameterPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _parameterPropertyGrid.CommandsVisibleIfAvailable = true;
                _parameterPropertyGrid.LargeButtons = false;
                _parameterPropertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar; 
                _parameterPropertyGrid.Location = new System.Drawing.Point(0, 18);
                _parameterPropertyGrid.PropertySort = PropertySort.Alphabetical; 
                _parameterPropertyGrid.Site = new PropertyGridSite(ServiceProvider, _parameterPropertyGrid); 
                _parameterPropertyGrid.Size = new System.Drawing.Size(400, 356);
                _parameterPropertyGrid.TabIndex = 20; 
                _parameterPropertyGrid.ToolbarVisible = false;
                _parameterPropertyGrid.ViewBackColor = System.Drawing.SystemColors.Window;
                _parameterPropertyGrid.ViewForeColor = System.Drawing.SystemColors.WindowText;
                _parameterPropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.OnParameterPropertyGridPropertyValueChanged); 

                _hideAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _hideAdvancedLinkLabel.Location = new System.Drawing.Point(0, 384); 
                _hideAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16);
                _hideAdvancedLinkLabel.TabIndex = 30; 
                _hideAdvancedLinkLabel.TabStop = true;
                _hideAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_HideAdvancedPropertiesLabel);
                _hideAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _hideAdvancedLinkLabel.Text.Length));
                _hideAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnHideAdvancedLinkLabelLinkClicked); 

                Controls.Add(_advancedlabel); 
                Controls.Add(_parameterPropertyGrid); 
                Controls.Add(_hideAdvancedLinkLabel);
 
                Dock = DockStyle.Fill;
                ResumeLayout();
            }
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 
 
                _parameterPropertyGrid.SelectedObject = ParameterItem.Parameter;
            } 

            private void OnHideAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange();
            } 

            private void OnParameterPropertyGridPropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e) { 
                // If the parameter is a control parameter, and its ControlID property was changed, set the default property name 
                if (e.ChangedItem.PropertyDescriptor.Name == "ControlID") {
                    ControlParameter controlParameter = ParameterItem.Parameter as ControlParameter; 
                    // Only change the PropertyName property if it is not already set, and if the ControlID property really changed
                    if ((controlParameter != null) && (controlParameter.PropertyName.Length == 0) && (controlParameter.ControlID != (string)e.OldValue)) {
                        // Get the ControlValuePropertyAttribute to determine the default property name
                        controlParameter.PropertyName = GetControlDefaultValuePropertyName(controlParameter.ControlID, ServiceProvider, _control); 
                    }
                } 
                OnParameterChanged(); 
            }
 
            public override void SetDefaultFocus() {
                _parameterPropertyGrid.Focus();
            }
        } 

        private sealed class ControlParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _controlIDLabel; 
            private AutoSizeComboBox _controlIDComboBox;
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel;
            private System.Web.UI.Control _control;
 
            public ControlParameterEditor(IServiceProvider serviceProvider, System.Web.UI.Control control) : base(serviceProvider) {
                _control = control; 
 
                SuspendLayout();
                Size = new Size(400, 400); 

                _controlIDLabel = new System.Windows.Forms.Label();
                _controlIDComboBox = new AutoSizeComboBox();
                _defaultValueLabel = new System.Windows.Forms.Label(); 
                _defaultValueTextBox = new System.Windows.Forms.TextBox();
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel(); 
 
                _controlIDLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _controlIDLabel.Location = new System.Drawing.Point(0, 0); 
                _controlIDLabel.Size = new System.Drawing.Size(400, 16);
                _controlIDLabel.TabIndex = 10;
                _controlIDLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ControlParameterControlID);
 
                _controlIDComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _controlIDComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
                _controlIDComboBox.Location = new System.Drawing.Point(0, 18); 
                _controlIDComboBox.Size = new System.Drawing.Size(400, 21);
                _controlIDComboBox.Sorted = true; 
                _controlIDComboBox.TabIndex = 20;
                _controlIDComboBox.SelectedIndexChanged += new System.EventHandler(this.OnControlIDComboBoxSelectedIndexChanged);

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 45);
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16); 
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 63);
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 87);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true;
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length)); 
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked);
 
                Controls.Add(_controlIDLabel); 
                Controls.Add(_controlIDComboBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel);

                Dock = DockStyle.Fill; 
                ResumeLayout();
            } 
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is ControlParameter);

                string initialControlID = ((ControlParameter)ParameterItem.Parameter).ControlID; 
                string initialPropertyName = ((ControlParameter)ParameterItem.Parameter).PropertyName;
 
                _controlIDComboBox.Items.Clear(); 

                // Populate the ControlID dropdown with all controls that have a default property 
                ControlItem selectedItem = null;
                if (ServiceProvider != null) {
                    IDesignerHost designerHost = (IDesignerHost)ServiceProvider.GetService(typeof(IDesignerHost));
                    if (designerHost != null) { 
                        ControlItem[] controlItems = ControlItem.GetControlItems(designerHost, _control);
                        foreach (ControlItem controlItem in controlItems) { 
                            _controlIDComboBox.Items.Add(controlItem); 
                            if ((controlItem.ControlID == initialControlID) && (controlItem.PropertyName == initialPropertyName)) {
                                selectedItem = controlItem; 
                            }
                        }
                    }
                } 

                // Add a custom entry if the current control is not already in the list 
                if (selectedItem == null) { 
                    if (initialControlID.Length > 0) {
                        // If the control already selected is not in the standard values list, add it 
                        ControlItem customItem = new ControlItem(initialControlID, initialPropertyName);
                        _controlIDComboBox.Items.Insert(0, customItem);
                        selectedItem = customItem;
                    } 
                }
 
                _controlIDComboBox.InvalidateDropDownWidth(); 

                _controlIDComboBox.SelectedItem = selectedItem; 

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue;
            }
 
            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange(); 
            } 

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) {
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged();
                } 
            }
 
            private void OnControlIDComboBoxSelectedIndexChanged(object s, System.EventArgs e) { 
                ControlItem controlItem = _controlIDComboBox.SelectedItem as ControlItem;
 
                ControlParameter controlParameter = (ControlParameter)ParameterItem.Parameter;

                if (controlItem == null) {
                    controlParameter.ControlID = String.Empty; 
                    controlParameter.PropertyName = String.Empty;
                } 
                else { 
                    controlParameter.ControlID = controlItem.ControlID;
                    controlParameter.PropertyName = controlItem.PropertyName; 
                }

                OnParameterChanged();
            } 

            public override void SetDefaultFocus() { 
                _controlIDComboBox.Focus(); 
            }
        } 

        private sealed class CookieParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _cookieNameLabel;
            private System.Windows.Forms.TextBox _cookieNameTextBox; 
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox; 
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel; 

            public CookieParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400);

                _cookieNameLabel = new System.Windows.Forms.Label(); 
                _cookieNameTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label(); 
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();
 
                _cookieNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _cookieNameLabel.Location = new System.Drawing.Point(0, 0);
                _cookieNameLabel.Size = new System.Drawing.Size(400, 16);
                _cookieNameLabel.TabIndex = 10; 
                _cookieNameLabel.Text = SR.GetString(SR.ParameterEditorUserControl_CookieParameterCookieName);
 
                _cookieNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _cookieNameTextBox.Location = new System.Drawing.Point(0, 18);
                _cookieNameTextBox.Size = new System.Drawing.Size(400, 20); 
                _cookieNameTextBox.TabIndex = 20;
                _cookieNameTextBox.TextChanged += new System.EventHandler(this.OnCookieNameTextBoxTextChanged);

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44);
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16); 
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62);
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true;
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length)); 
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked);
 
                Controls.Add(_cookieNameLabel); 
                Controls.Add(_cookieNameTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel);

                Dock = DockStyle.Fill; 
                ResumeLayout();
            } 
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is CookieParameter);

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue; 
                _cookieNameTextBox.Text = ((CookieParameter)ParameterItem.Parameter).CookieName;
            } 
 
            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange(); 
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) { 
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged(); 
                } 
            }
 
            private void OnCookieNameTextBoxTextChanged(object s, System.EventArgs e) {
                if (((CookieParameter)ParameterItem.Parameter).CookieName != _cookieNameTextBox.Text) {
                    ((CookieParameter)ParameterItem.Parameter).CookieName = _cookieNameTextBox.Text;
                    OnParameterChanged(); 
                }
            } 
 
            public override void SetDefaultFocus() {
                _cookieNameTextBox.Focus(); 
            }
        }

        private sealed class FormParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _formFieldLabel;
            private System.Windows.Forms.TextBox _formFieldTextBox; 
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel; 

            public FormParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) {
                SuspendLayout();
                Size = new Size(400, 400); 

                _formFieldLabel = new System.Windows.Forms.Label(); 
                _formFieldTextBox = new System.Windows.Forms.TextBox(); 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();

                _formFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _formFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _formFieldLabel.Size = new System.Drawing.Size(400, 16);
                _formFieldLabel.TabIndex = 10; 
                _formFieldLabel.Text = SR.GetString(SR.ParameterEditorUserControl_FormParameterFormField); 

                _formFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _formFieldTextBox.Location = new System.Drawing.Point(0, 18);
                _formFieldTextBox.Size = new System.Drawing.Size(400, 20);
                _formFieldTextBox.TabIndex = 20;
                _formFieldTextBox.TextChanged += new System.EventHandler(this.OnFormFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44); 
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62); 
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16);
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true; 
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length)); 
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked); 

                Controls.Add(_formFieldLabel); 
                Controls.Add(_formFieldTextBox);
                Controls.Add(_defaultValueLabel);
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel); 

                Dock = DockStyle.Fill; 
                ResumeLayout(); 
            }
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem);

                Debug.Assert(parameterItem.Parameter is FormParameter); 

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue; 
                _formFieldTextBox.Text = ((FormParameter)ParameterItem.Parameter).FormField; 
            }
 
            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange();
            }
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) { 
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text; 
                    OnParameterChanged();
                } 
            }

            private void OnFormFieldTextBoxTextChanged(object s, System.EventArgs e) {
                if (((FormParameter)ParameterItem.Parameter).FormField != _formFieldTextBox.Text) { 
                    ((FormParameter)ParameterItem.Parameter).FormField = _formFieldTextBox.Text;
                    OnParameterChanged(); 
                } 
            }
 
            public override void SetDefaultFocus() {
                _formFieldTextBox.Focus();
            }
        } 

        private sealed class SessionParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _sessionFieldLabel; 
            private System.Windows.Forms.TextBox _sessionFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel;

            public SessionParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400); 
 
                _sessionFieldLabel = new System.Windows.Forms.Label();
                _sessionFieldTextBox = new System.Windows.Forms.TextBox(); 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox();
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();
 
                _sessionFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _sessionFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _sessionFieldLabel.Size = new System.Drawing.Size(400, 16); 
                _sessionFieldLabel.TabIndex = 10;
                _sessionFieldLabel.Text = SR.GetString(SR.ParameterEditorUserControl_SessionParameterSessionField); 

                _sessionFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _sessionFieldTextBox.Location = new System.Drawing.Point(0, 18);
                _sessionFieldTextBox.Size = new System.Drawing.Size(400, 20); 
                _sessionFieldTextBox.TabIndex = 20;
                _sessionFieldTextBox.TextChanged += new System.EventHandler(this.OnSessionFieldTextBoxTextChanged); 
 
                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44); 
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16);
                _defaultValueLabel.TabIndex = 30;
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62); 
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20); 
                _defaultValueTextBox.TabIndex = 40;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true; 
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties); 
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length));
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked); 

                Controls.Add(_sessionFieldLabel);
                Controls.Add(_sessionFieldTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel); 
 
                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }

            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is SessionParameter); 
 
                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue;
                _sessionFieldTextBox.Text = ((SessionParameter)ParameterItem.Parameter).SessionField; 
            }

            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange(); 
            }
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) {
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text; 
                    OnParameterChanged();
                }
            }
 
            private void OnSessionFieldTextBoxTextChanged(object s, System.EventArgs e) {
                if (((SessionParameter)ParameterItem.Parameter).SessionField != _sessionFieldTextBox.Text) { 
                    ((SessionParameter)ParameterItem.Parameter).SessionField = _sessionFieldTextBox.Text; 
                    OnParameterChanged();
                } 
            }

            public override void SetDefaultFocus() {
                _sessionFieldTextBox.Focus(); 
            }
        } 
 
        private sealed class StaticParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel;

            public StaticParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400); 
 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 0); 
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16);
                _defaultValueLabel.TabIndex = 10; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 18);
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 20;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 42); 
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16);
                _showAdvancedLinkLabel.TabIndex = 30; 
                _showAdvancedLinkLabel.TabStop = true;
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length));
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked); 

                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox); 
                Controls.Add(_showAdvancedLinkLabel);
 
                Dock = DockStyle.Fill;
                ResumeLayout();
            }
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 
 
                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue;
            } 

            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange();
            } 

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) { 
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged(); 
                }
            }

            public override void SetDefaultFocus() { 
                _defaultValueTextBox.Focus();
            } 
        } 

        private sealed class QueryStringParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _queryStringFieldLabel;
            private System.Windows.Forms.TextBox _queryStringFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox; 
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel;
 
            public QueryStringParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400); 

                _queryStringFieldLabel = new System.Windows.Forms.Label();
                _queryStringFieldTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label(); 
                _defaultValueTextBox = new System.Windows.Forms.TextBox();
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel(); 
 
                _queryStringFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _queryStringFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _queryStringFieldLabel.Size = new System.Drawing.Size(400, 16);
                _queryStringFieldLabel.TabIndex = 10;
                _queryStringFieldLabel.Text = SR.GetString(SR.ParameterEditorUserControl_QueryStringParameterQueryStringField);
 
                _queryStringFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _queryStringFieldTextBox.Location = new System.Drawing.Point(0, 18); 
                _queryStringFieldTextBox.Size = new System.Drawing.Size(400, 20); 
                _queryStringFieldTextBox.TabIndex = 20;
                _queryStringFieldTextBox.TextChanged += new System.EventHandler(this.OnQueryStringFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44);
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16); 
                _defaultValueLabel.TabIndex = 30;
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue); 
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62); 
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86); 
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true; 
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length));
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked);
 
                Controls.Add(_queryStringFieldLabel);
                Controls.Add(_queryStringFieldTextBox); 
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel); 

                Dock = DockStyle.Fill;
                ResumeLayout();
            } 

            public override void InitializeParameter(ParameterListViewItem parameterItem) { 
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is QueryStringParameter); 

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue;
                _queryStringFieldTextBox.Text = ((QueryStringParameter)ParameterItem.Parameter).QueryStringField;
            } 

            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) { 
                OnRequestModeChange(); 
            }
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) {
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged(); 
                }
            } 
 
            private void OnQueryStringFieldTextBoxTextChanged(object s, System.EventArgs e) {
                if (((QueryStringParameter)ParameterItem.Parameter).QueryStringField != _queryStringFieldTextBox.Text) { 
                    ((QueryStringParameter)ParameterItem.Parameter).QueryStringField = _queryStringFieldTextBox.Text;
                    OnParameterChanged();
                }
            } 

            public override void SetDefaultFocus() { 
                _queryStringFieldTextBox.Focus(); 
            }
        } 

        private sealed class ProfileParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _propertyNameLabel;
            private System.Windows.Forms.TextBox _propertyNameTextBox; 
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox; 
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel; 

            public ProfileParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400);

                _propertyNameLabel = new System.Windows.Forms.Label(); 
                _propertyNameTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label(); 
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();
 
                _propertyNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _propertyNameLabel.Location = new System.Drawing.Point(0, 0);
                _propertyNameLabel.Size = new System.Drawing.Size(400, 16);
                _propertyNameLabel.TabIndex = 10; 
                _propertyNameLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ProfilePropertyName);
 
                _propertyNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _propertyNameTextBox.Location = new System.Drawing.Point(0, 18);
                _propertyNameTextBox.Size = new System.Drawing.Size(400, 20); 
                _propertyNameTextBox.TabIndex = 20;
                _propertyNameTextBox.TextChanged += new System.EventHandler(this.OnPropertyNameTextBoxTextChanged);

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44);
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16); 
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62);
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true;
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length)); 
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked);
 
                Controls.Add(_propertyNameLabel); 
                Controls.Add(_propertyNameTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel);

                Dock = DockStyle.Fill; 
                ResumeLayout();
            } 
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is ProfileParameter);

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue; 
                _propertyNameTextBox.Text = ((ProfileParameter)ParameterItem.Parameter).PropertyName;
            } 
 
            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange(); 
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) { 
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged(); 
                } 
            }
 
            private void OnPropertyNameTextBoxTextChanged(object s, System.EventArgs e) {
                if (((ProfileParameter)ParameterItem.Parameter).PropertyName != _propertyNameTextBox.Text) {
                    ((ProfileParameter)ParameterItem.Parameter).PropertyName = _propertyNameTextBox.Text;
                    OnParameterChanged(); 
                }
            } 
 
            public override void SetDefaultFocus() {
                _propertyNameTextBox.Focus(); 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ParameterEditorUserControl.cs" company="Microsoft">
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
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Collections.Generic;

    /// <devdoc> 
    /// A reusable UserControl for editing ParameterCollection objects.
    /// Enables a user to add/remove/reorder parameters and change their types. 
    /// </devdoc> 
    public class ParameterEditorUserControl : System.Windows.Forms.UserControl {
 
        private static readonly object EventParametersChanged = new object();

        private System.Windows.Forms.Label _parametersLabel;
        private System.Windows.Forms.ListView _parametersListView; 
        private AutoSizeComboBox _parameterTypeComboBox;
        private System.Windows.Forms.ColumnHeader _nameColumnHeader; 
        private System.Windows.Forms.ColumnHeader _valueColumnHeader; 
        private System.Windows.Forms.Button _moveUpButton;
        private System.Windows.Forms.Button _moveDownButton; 
        private System.Windows.Forms.Button _deleteParameterButton;
        private System.Windows.Forms.Button _addParameterButton;
        private System.Windows.Forms.Panel _addButtonPanel;
        private System.Windows.Forms.Label _sourceLabel; 
        private System.Windows.Forms.Panel _editorPanel;
 
        private ListDictionary _parameterTypes; 
        private IServiceProvider _serviceProvider;
        private ParameterEditor _parameterEditor; 
        private bool _inAdvancedMode;
        private int _ignoreParameterChangesCount;

        private AdvancedParameterEditor _advancedParameterEditor; 
        private ControlParameterEditor _controlParameterEditor;
        private CookieParameterEditor _cookieParameterEditor; 
        private FormParameterEditor _formParameterEditor; 
        private QueryStringParameterEditor _queryStringParameterEditor;
        private SessionParameterEditor _sessionParameterEditor; 
        private StaticParameterEditor _staticParameterEditor;
        private ProfileParameterEditor _profileParameterEditor;

        private System.Web.UI.Control _control; 

 
        /// <devdoc> 
        /// Creates a new ParameterEditorUserControl.
        /// </devdoc> 
        public ParameterEditorUserControl(IServiceProvider serviceProvider) : this(serviceProvider, null) {
        }

        internal ParameterEditorUserControl(IServiceProvider serviceProvider, System.Web.UI.Control control) { 
            _serviceProvider = serviceProvider;
            _control = control; 
 
            InitializeComponent();
            InitializeUI(); 
            InitializeParameterEditors();

            // Add types to drop down list
            CreateParameterList(); 
            foreach (DictionaryEntry de in _parameterTypes) {
                _parameterTypeComboBox.Items.Add(de.Value); 
            } 
            _parameterTypeComboBox.InvalidateDropDownWidth();
            // Refresh the UI 
            UpdateUI(false);
        }

        /// <devdoc> 
        /// Returns true if all the parameters in the editor are fully configured.
        /// For example if one of the parameters is a SessionParameter but its 
        /// SessionField property is not set, then it is not considered to be 
        /// configured.
        /// </devdoc> 
        public bool ParametersConfigured {
            get {
                foreach (ParameterListViewItem item in _parametersListView.Items) {
                    // VSWhidbey 412396 fixes the null items 
                    Debug.Assert(item != null, "A ListViewItem returned from the Items collection should not be null.");
                    if (item != null && !item.IsConfigured) { 
                        return false; 
                    }
                } 
                return true;
            }
        }
 
        /// <devdoc>
        /// Notifies listeners when the state of a Parameter has changed. For 
        /// example when a user changes a property of a Parameter this event 
        /// is raised.
        /// </devdoc> 
        public event EventHandler ParametersChanged {
            add {
                Events.AddHandler(EventParametersChanged, value);
            } 
            remove {
                Events.RemoveHandler(EventParametersChanged, value); 
            } 
        }
 
        /// <devdoc>
        /// Creates the internal list of parameter types.
        /// </devdoc>
        private void CreateParameterList() { 
            _parameterTypes = new ListDictionary();
            _parameterTypes.Add(typeof(Parameter), "None"); 
            _parameterTypes.Add(typeof(CookieParameter), "Cookie"); 
            _parameterTypes.Add(typeof(ControlParameter), "Control");
            _parameterTypes.Add(typeof(FormParameter), "Form"); 
            _parameterTypes.Add(typeof(ProfileParameter), "Profile");
            _parameterTypes.Add(typeof(QueryStringParameter), "QueryString");
            _parameterTypes.Add(typeof(SessionParameter), "Session");
        } 

        #region Component Designer generated code 
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent() {
            this._addButtonPanel = new System.Windows.Forms.Panel();
            this._addParameterButton = new System.Windows.Forms.Button(); 
            this._parametersLabel = new System.Windows.Forms.Label();
            this._sourceLabel = new System.Windows.Forms.Label(); 
            this._parametersListView = new System.Windows.Forms.ListView(); 
            this._nameColumnHeader = new System.Windows.Forms.ColumnHeader("");
            this._valueColumnHeader = new System.Windows.Forms.ColumnHeader(""); 
            this._parameterTypeComboBox = new AutoSizeComboBox();
            this._moveUpButton = new System.Windows.Forms.Button();
            this._moveDownButton = new System.Windows.Forms.Button();
            this._deleteParameterButton = new System.Windows.Forms.Button(); 
            this._editorPanel = new System.Windows.Forms.Panel();
            this._addButtonPanel.SuspendLayout(); 
            this.SuspendLayout(); 
            //
            // _parametersLabel 
            //
            this._parametersLabel.Location = new System.Drawing.Point(0, 0);
            this._parametersLabel.Name = "_parametersLabel";
            this._parametersLabel.Size = new System.Drawing.Size(252, 16); 
            this._parametersLabel.TabIndex = 10;
            // 
            // _parametersListView 
            //
            this._parametersListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                        | System.Windows.Forms.AnchorStyles.Left)));
            this._parametersListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._nameColumnHeader,
            this._valueColumnHeader}); 
            this._parametersListView.FullRowSelect = true;
            this._parametersListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable; 
            this._parametersListView.HideSelection = false; 
            this._parametersListView.LabelEdit = true;
            this._parametersListView.Location = new System.Drawing.Point(0, 18); 
            this._parametersListView.MultiSelect = false;
            this._parametersListView.Name = "_parametersListView";
            this._parametersListView.Size = new System.Drawing.Size(252, 234);
            this._parametersListView.TabIndex = 20; 
            this._parametersListView.View = System.Windows.Forms.View.Details;
            this._parametersListView.SelectedIndexChanged += new System.EventHandler(this.OnParametersListViewSelectedIndexChanged); 
            this._parametersListView.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.OnParametersListViewAfterLabelEdit); 
            //
            // _nameColumnHeader 
            //
            this._nameColumnHeader.Width = 85;
            //
            // _valueColumnHeader 
            //
            this._valueColumnHeader.Width = 134; 
            // 
            // _addButtonPanel
            // 
            this._addButtonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._addButtonPanel.Controls.Add(this._addParameterButton);
            this._addButtonPanel.Location = new System.Drawing.Point(0, 258);
            this._addButtonPanel.Name = "_addButtonPanel"; 
            this._addButtonPanel.Size = new System.Drawing.Size(252, 23);
            this._addButtonPanel.TabIndex = 30; 
            // 
            // _addParameterButton
            // 
            this._addParameterButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._addParameterButton.AutoSize = true;
            this._addParameterButton.Location = new System.Drawing.Point(124, 0);
            this._addParameterButton.Name = "_addParameterButton"; 
            this._addParameterButton.Size = new System.Drawing.Size(128, 23);
            this._addParameterButton.TabIndex = 10; 
            this._addParameterButton.Click += new System.EventHandler(this.OnAddParameterButtonClick); 
            //
            // _moveUpButton 
            //
            this._moveUpButton.Location = new System.Drawing.Point(258, 18);
            this._moveUpButton.Name = "_moveUpButton";
            this._moveUpButton.Size = new System.Drawing.Size(26, 23); 
            this._moveUpButton.TabIndex = 40;
            this._moveUpButton.Click += new System.EventHandler(this.OnMoveUpButtonClick); 
            // 
            // _moveDownButton
            // 
            this._moveDownButton.Location = new System.Drawing.Point(258, 42);
            this._moveDownButton.Name = "_moveDownButton";
            this._moveDownButton.Size = new System.Drawing.Size(26, 23);
            this._moveDownButton.TabIndex = 50; 
            this._moveDownButton.Click += new System.EventHandler(this.OnMoveDownButtonClick);
            // 
            // _deleteParameterButton 
            //
            this._deleteParameterButton.Location = new System.Drawing.Point(258, 71); 
            this._deleteParameterButton.Name = "_deleteParameterButton";
            this._deleteParameterButton.Size = new System.Drawing.Size(26, 23);
            this._deleteParameterButton.TabIndex = 60;
            this._deleteParameterButton.Click += new System.EventHandler(this.OnDeleteParameterButtonClick); 
            //
            // _sourceLabel 
            // 
            this._sourceLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._sourceLabel.Location = new System.Drawing.Point(292, 0);
            this._sourceLabel.Name = "_sourceLabel";
            this._sourceLabel.Size = new System.Drawing.Size(300, 16);
            this._sourceLabel.TabIndex = 70; 
            //
            // _parameterTypeComboBox 
            // 
            this._parameterTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._parameterTypeComboBox.Location = new System.Drawing.Point(292, 18); 
            this._parameterTypeComboBox.Name = "_parameterTypeComboBox";
            this._parameterTypeComboBox.Size = new System.Drawing.Size(163, 21);
            this._parameterTypeComboBox.TabIndex = 80;
            this._parameterTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnParameterTypeComboBoxSelectedIndexChanged); 
            //
            // _editorPanel 
            // 
            this._editorPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._editorPanel.Location = new System.Drawing.Point(292, 47);
            this._editorPanel.Name = "_editorPanel";
            this._editorPanel.Size = new System.Drawing.Size(308, 235); 
            this._editorPanel.TabIndex = 90;
            // 
            // ParameterEditorUserControl 
            //
            this.Controls.Add(this._editorPanel); 
            this.Controls.Add(this._addButtonPanel);
            this.Controls.Add(this._deleteParameterButton);
            this.Controls.Add(this._moveDownButton);
            this.Controls.Add(this._moveUpButton); 
            this.Controls.Add(this._parameterTypeComboBox);
            this.Controls.Add(this._parametersListView); 
            this.Controls.Add(this._sourceLabel); 
            this.Controls.Add(this._parametersLabel);
            this.MinimumSize = new System.Drawing.Size(460, 126); 
            this.Name = "ParameterEditorUserControl";
            this.Size = new System.Drawing.Size(600, 280);
            this._addButtonPanel.ResumeLayout(false);
            this._addButtonPanel.PerformLayout(); 
            this.ResumeLayout(false);
        } 
        #endregion 

        /// <devdoc> 
        /// Initializes the individual parameter editors and parents them to
        /// the form.
        /// </devdoc>
        private void InitializeParameterEditors() { 
            _advancedParameterEditor = new AdvancedParameterEditor(_serviceProvider, _control);
            _advancedParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _advancedParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _advancedParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_advancedParameterEditor); 

            _staticParameterEditor = new StaticParameterEditor(_serviceProvider);
            _staticParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode);
            _staticParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _staticParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_staticParameterEditor); 
 
            _controlParameterEditor = new ControlParameterEditor(_serviceProvider, _control);
            _controlParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _controlParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged);
            _controlParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_controlParameterEditor);
 
            _formParameterEditor = new FormParameterEditor(_serviceProvider);
            _formParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _formParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _formParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_formParameterEditor); 

            _queryStringParameterEditor = new QueryStringParameterEditor(_serviceProvider);
            _queryStringParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode);
            _queryStringParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _queryStringParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_queryStringParameterEditor); 
 
            _cookieParameterEditor = new CookieParameterEditor(_serviceProvider);
            _cookieParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _cookieParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged);
            _cookieParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_cookieParameterEditor);
 
            _sessionParameterEditor = new SessionParameterEditor(_serviceProvider);
            _sessionParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode); 
            _sessionParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _sessionParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_sessionParameterEditor); 

            _profileParameterEditor = new ProfileParameterEditor(_serviceProvider);
            _profileParameterEditor.RequestModeChange += new EventHandler(ToggleAdvancedMode);
            _profileParameterEditor.ParameterChanged += new EventHandler(OnParametersChanged); 
            _profileParameterEditor.Visible = false;
            _editorPanel.Controls.Add(_profileParameterEditor); 
        } 

        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer.
        /// </devdoc>
        private void InitializeUI() { 
            _parametersLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParametersLabel);
            _nameColumnHeader.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterNameColumnHeader); 
            _valueColumnHeader.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterValueColumnHeader); 
            _addParameterButton.Text = SR.GetString(SR.ParameterEditorUserControl_AddButton);
            _sourceLabel.Text = SR.GetString(SR.ParameterEditorUserControl_SourceLabel); 

            Icon moveUpIcon = new Icon(typeof(ParameterEditorUserControl), "SortUp.ico");
            Bitmap moveUpBitmap = moveUpIcon.ToBitmap();
            moveUpBitmap.MakeTransparent(); 
            _moveUpButton.Image = moveUpBitmap;
 
            Icon moveDownIcon = new Icon(typeof(ParameterEditorUserControl), "SortDown.ico"); 
            Bitmap moveDownBitmap = moveDownIcon.ToBitmap();
            moveDownBitmap.MakeTransparent(); 
            _moveDownButton.Image = moveDownBitmap;

            Icon deleteIcon = new Icon(typeof(ParameterEditorUserControl), "Delete.ico");
            Bitmap deleteBitmap = deleteIcon.ToBitmap(); 
            deleteBitmap.MakeTransparent();
            _deleteParameterButton.Image = deleteBitmap; 
 
            // Accessibility names
            _moveUpButton.AccessibleName = SR.GetString(SR.ParameterEditorUserControl_MoveParameterUp); 
            _moveDownButton.AccessibleName = SR.GetString(SR.ParameterEditorUserControl_MoveParameterDown);
            _deleteParameterButton.AccessibleName = SR.GetString(SR.ParameterEditorUserControl_DeleteParameter);
        }
 
        /// <devdoc>
        /// Adds a new parameter with a given name. 
        /// </devdoc> 
        private void AddParameter(Parameter parameter) {
            try { 
                IgnoreParameterChanges(true);
                ParameterListViewItem item = new ParameterListViewItem(parameter);

                _parametersListView.BeginUpdate(); 
                try {
                    _parametersListView.Items.Add(item); 
                    // Automatically select new item 
                    item.Selected = true;
                    item.Focused = true; 
                    item.EnsureVisible();
                    _parametersListView.Focus();
                }
                finally { 
                    _parametersListView.EndUpdate();
                } 
 
                item.Refresh();
 
                // Allow user to edit parameter name immediately
                item.BeginEdit();

            } 
            finally {
                IgnoreParameterChanges(false); 
            } 
            OnParametersChanged(this, EventArgs.Empty);
        } 

        /// <devdoc>
        /// Adds an array of existing parameters.
        /// </devdoc> 
        public void AddParameters(Parameter[] parameters) {
            try { 
                IgnoreParameterChanges(true); 
                _parametersListView.BeginUpdate();
                ArrayList items = new ArrayList(); 
                try {
                    foreach (Parameter parameter in parameters) {
                        ParameterListViewItem item = new ParameterListViewItem(parameter);
                        _parametersListView.Items.Add(item); 
                        items.Add(item);
                    } 
                    // Automatically select first item 
                    if (_parametersListView.Items.Count > 0) {
                        _parametersListView.Items[0].Selected = true; 
                        _parametersListView.Items[0].Focused = true;
                        _parametersListView.Items[0].EnsureVisible();
                    }
                    _parametersListView.Focus(); 
                }
                finally { 
                    _parametersListView.EndUpdate(); 
                }
 
                foreach (ParameterListViewItem item in items) {
                    item.Refresh();
                }
            } 
            finally {
                IgnoreParameterChanges(false); 
            } 
            OnParametersChanged(this, EventArgs.Empty);
        } 

        /// <devdoc>
        /// Removes all parameters.
        /// </devdoc> 
        public void ClearParameters() {
            try { 
                IgnoreParameterChanges(true); 
                _parametersListView.Items.Clear();
 
                UpdateUI(false);
            }
            finally {
                IgnoreParameterChanges(false); 
            }
            OnParametersChanged(this, EventArgs.Empty); 
        } 

        internal static string GetControlDefaultValuePropertyName(string controlID, IServiceProvider serviceProvider, System.Web.UI.Control control) { 
            System.Web.UI.Control foundControl = ControlHelper.FindControl(serviceProvider, control, controlID);

            if (foundControl != null) {
                return GetDefaultValuePropertyName(foundControl); 
            }
 
            return String.Empty; 
        }
 
        /// <devdoc>
        /// Get the default property of a control, or return null if there is no default property.
        /// </devdoc>
        private static string GetDefaultValuePropertyName(System.Web.UI.Control control) { 
            ControlValuePropertyAttribute controlValueProp = (ControlValuePropertyAttribute)TypeDescriptor.GetAttributes(control)[typeof(ControlValuePropertyAttribute)];
            if ((controlValueProp != null) && !String.IsNullOrEmpty(controlValueProp.Name)) { 
                return controlValueProp.Name; 
            }
            else { 
                return String.Empty;
            }
        }
 
        /// <devdoc>
        /// Gets an expression indicating how the parameter gets its value. 
        /// </devdoc> 
        internal static string GetParameterExpression(IServiceProvider serviceProvider, Parameter p, System.Web.UI.Control control, out bool isHelperText) {
            Debug.Assert(p != null); 

            if (p.GetType() == typeof(ControlParameter)) {
                ControlParameter cp = (ControlParameter)p;
                if (cp.ControlID.Length == 0) { 
                    isHelperText = true;
                    return SR.GetString(SR.ParameterEditorUserControl_ControlParameterExpressionUnknown); 
                } 
                else {
                    string propertyName = cp.PropertyName; 
                    if (propertyName.Length == 0) {
                        propertyName = GetControlDefaultValuePropertyName(cp.ControlID, serviceProvider, control);
                    }
 
                    if (propertyName.Length > 0) {
                        isHelperText = false; 
                        return cp.ControlID + "." + propertyName; 
                    }
                    else { 
                        isHelperText = true;
                        return SR.GetString(SR.ParameterEditorUserControl_ControlParameterExpressionUnknown);
                    }
                } 
            }
            else if (p.GetType() == typeof(FormParameter)) { 
                FormParameter rp = (FormParameter)p; 
                if (rp.FormField.Length > 0) {
                    isHelperText = false; 
                    return String.Format(CultureInfo.InvariantCulture, "Request.Form(\"{0}\")", rp.FormField);
                }
                else {
                    isHelperText = true; 
                    return SR.GetString(SR.ParameterEditorUserControl_FormParameterExpressionUnknown);
                } 
            } 
            else if (p.GetType() == typeof(QueryStringParameter)) {
                QueryStringParameter qsp = (QueryStringParameter)p; 
                if (qsp.QueryStringField.Length > 0) {
                    isHelperText = false;
                    return String.Format(CultureInfo.InvariantCulture, "Request.QueryString(\"{0}\")", qsp.QueryStringField);
                } 
                else {
                    isHelperText = true; 
                    return SR.GetString(SR.ParameterEditorUserControl_QueryStringParameterExpressionUnknown); 
                }
            } 
            else if (p.GetType() == typeof(CookieParameter)) {
                CookieParameter cp = (CookieParameter)p;
                if (cp.CookieName.Length > 0) {
                    isHelperText = false; 
                    return String.Format(CultureInfo.InvariantCulture, "Request.Cookies(\"{0}\").Value", cp.CookieName);
                } 
                else { 
                    isHelperText = true;
                    return SR.GetString(SR.ParameterEditorUserControl_CookieParameterExpressionUnknown); 
                }
            }
            else if (p.GetType() == typeof(SessionParameter)) {
                SessionParameter sp = (SessionParameter)p; 
                if (sp.SessionField.Length > 0) {
                    isHelperText = false; 
                    return String.Format(CultureInfo.InvariantCulture, "Session(\"{0}\")", sp.SessionField); 
                }
                else { 
                    isHelperText = true;
                    return SR.GetString(SR.ParameterEditorUserControl_SessionParameterExpressionUnknown);
                }
            } 
            else if (p.GetType() == typeof(ProfileParameter)) {
                ProfileParameter pp = (ProfileParameter)p; 
                if (pp.PropertyName.Length > 0) { 
                    isHelperText = false;
                    return String.Format(CultureInfo.InvariantCulture, "Profile(\"{0}\")", pp.PropertyName); 
                }
                else {
                    isHelperText = true;
                    return SR.GetString(SR.ParameterEditorUserControl_ProfileParameterExpressionUnknown); 
                }
            } 
            else if (p.GetType() == typeof(Parameter)) { 
                Parameter sp = (Parameter)p;
                if (sp.DefaultValue == null) { 
                    isHelperText = false;
                    return String.Empty;
                }
                else { 
                    isHelperText = false;
                    return sp.DefaultValue; 
                } 
            }
            // Parameter is of a custom type, so we just show the type name 
            isHelperText = true;
            return p.GetType().Name;
        }
 
        /// <devdoc>
        /// Gets all parameters that have values (i.e. are not unassigned). 
        /// </devdoc> 
        public Parameter[] GetParameters() {
            ArrayList parameters = new ArrayList(); 
            foreach (ParameterListViewItem item in _parametersListView.Items) {
                if (item.Parameter != null) {
                    parameters.Add(item.Parameter);
                } 
            }
            return (Parameter[])parameters.ToArray(typeof(Parameter)); 
        } 

        private void IgnoreParameterChanges(bool ignoreChanges) { 
            _ignoreParameterChangesCount += (ignoreChanges ? 1 : -1);

            if (_ignoreParameterChangesCount == 0) {
                UpdateUI(false); 
            }
        } 
 
        private void OnAddParameterButtonClick(object sender, System.EventArgs e) {
            // 
            AddParameter(new Parameter("newparameter"));
        }

        private void OnDeleteParameterButtonClick(object sender, System.EventArgs e) { 
            try {
                IgnoreParameterChanges(true); 
 
                if (_parametersListView.SelectedItems.Count == 0) {
                    return; 
                }
                int index = _parametersListView.SelectedIndices[0];

                _parametersListView.BeginUpdate(); 
                try {
                    _parametersListView.Items.RemoveAt(index); 
 
                    if (index < _parametersListView.Items.Count) {
                        _parametersListView.Items[index].Selected = true; 
                        _parametersListView.Items[index].Focused = true;
                        _parametersListView.Items[index].EnsureVisible();
                        _parametersListView.Focus();
                    } 
                    else if (_parametersListView.Items.Count > 0) {
                        index = _parametersListView.Items.Count - 1; 
                        _parametersListView.Items[index].Selected = true; 
                        _parametersListView.Items[index].Focused = true;
                        _parametersListView.Items[index].EnsureVisible(); 
                        _parametersListView.Focus();
                    }
                }
                finally { 
                    _parametersListView.EndUpdate();
                } 
 
                UpdateUI(false);
            } 
            finally {
                IgnoreParameterChanges(false);
            }
            OnParametersChanged(this, EventArgs.Empty); 
        }
 
        private void OnMoveDownButtonClick(object sender, System.EventArgs e) { 
            try {
                IgnoreParameterChanges(true); 

                if (_parametersListView.SelectedItems.Count == 0) {
                    return;
                } 

                int index = _parametersListView.SelectedIndices[0]; 
                if (index == _parametersListView.Items.Count - 1) { 
                    return;
                } 

                _parametersListView.BeginUpdate();
                try {
                    // Swap item with the one below it 
                    ListViewItem itemMove = _parametersListView.Items[index];
                    itemMove.Remove(); 
                    _parametersListView.Items.Insert(index + 1, itemMove); 

                    itemMove.Selected = true; 
                    itemMove.Focused = true;
                    itemMove.EnsureVisible();
                    _parametersListView.Focus();
                } 
                finally {
                    _parametersListView.EndUpdate(); 
                } 
            }
            finally { 
                IgnoreParameterChanges(false);
            }
            OnParametersChanged(this, EventArgs.Empty);
        } 

        private void OnMoveUpButtonClick(object sender, System.EventArgs e) { 
            try { 
                IgnoreParameterChanges(true);
 
                if (_parametersListView.SelectedItems.Count == 0) {
                    return;
                }
 
                int index = _parametersListView.SelectedIndices[0];
                if (index == 0) { 
                    return; 
                }
 
                _parametersListView.BeginUpdate();
                try {
                    // Swap item with the one above it
                    ListViewItem itemMove = _parametersListView.Items[index]; 
                    itemMove.Remove();
                    _parametersListView.Items.Insert(index - 1, itemMove); 
 
                    itemMove.Selected = true;
                    itemMove.Focused = true; 
                    itemMove.EnsureVisible();
                    _parametersListView.Focus();
                }
                finally { 
                    _parametersListView.EndUpdate();
                } 
            } 
            finally {
                IgnoreParameterChanges(false); 
            }
            OnParametersChanged(this, EventArgs.Empty);
        }
 
        /// <devdoc>
        /// This method is called whenever the state of a parameter in the editor 
        /// changes. Calling this method also raises the ParametersChanged event 
        /// so that external listeners can be notified as well.
        /// </devdoc> 
        protected virtual void OnParametersChanged(object sender, EventArgs e) {
            if (_ignoreParameterChangesCount > 0) {
                return;
            } 
            EventHandler handler = Events[EventParametersChanged] as EventHandler;
            if (handler != null) { 
                handler(this, EventArgs.Empty); 
            }
        } 

        /// <devdoc>
        /// After Label Edit event handler for Parameters List View.
        /// </devdoc> 
        private void OnParametersListViewAfterLabelEdit(object sender, LabelEditEventArgs e) {
            if ((e.Label == null) || (e.Label.Trim().Length == 0)) { 
                e.CancelEdit = true; 
                return;
            } 
            ParameterListViewItem item = (ParameterListViewItem)_parametersListView.Items[e.Item];
            item.ParameterName = e.Label;
            UpdateUI(false);
        } 

        private void OnParametersListViewSelectedIndexChanged(object sender, System.EventArgs e) { 
            UpdateUI(false); 
        }
 
        private void OnParameterTypeComboBoxSelectedIndexChanged(object sender, System.EventArgs e) {
            try {
                IgnoreParameterChanges(true);
 
                if (_parametersListView.SelectedItems.Count == 0) {
                    return; 
                } 

                ParameterListViewItem item = (ParameterListViewItem)_parametersListView.SelectedItems[0]; 

                string parameterTypeName = (string)_parameterTypeComboBox.SelectedItem;
                // Search for type name in list to determine the actual Type
                Type parameterType = null; 
                foreach (DictionaryEntry de in _parameterTypes) {
                    if ((string)de.Value == parameterTypeName) { 
                        parameterType = (Type)de.Key; 
                    }
                } 
                // If the type has changed, create a new instance of the type
                if ((parameterType != null) && ((item.Parameter == null) || (item.Parameter.GetType() != parameterType))) {
                    item.Parameter = (Parameter)Activator.CreateInstance(parameterType);
                    item.Refresh(); 
                }
                // Update editor 
                SetActiveEditParameterItem(item, false); 
            }
            finally { 
                IgnoreParameterChanges(false);
            }
            OnParametersChanged(this, EventArgs.Empty);
        } 

        private void SetActiveEditParameterItem(ParameterListViewItem parameterItem, bool allowFocusChange) { 
            if (parameterItem == null) { 
                // Destroy the current editor, if there is one
                if (_parameterEditor != null) { 
                    _parameterEditor.Visible = false;
                    _parameterEditor = null;
                }
            } 
            else {
                // Figure out the appropriate parameter editor for the new parameter 
                ParameterEditor newParameterEditor = null; 
                if (_inAdvancedMode) {
                    newParameterEditor = _advancedParameterEditor; 
                }
                else {
                    Debug.Assert(parameterItem.Parameter != null);
                    if (parameterItem.Parameter != null) { 
                        if (parameterItem.Parameter.GetType() == typeof(Parameter)) {
                            newParameterEditor = _staticParameterEditor; 
                        } 
                        else {
                            if (parameterItem.Parameter.GetType() == typeof(ControlParameter)) { 
                                newParameterEditor = _controlParameterEditor;
                            }
                            else {
                                if (parameterItem.Parameter.GetType() == typeof(FormParameter)) { 
                                    newParameterEditor = _formParameterEditor;
                                } 
                                else { 
                                    if (parameterItem.Parameter.GetType() == typeof(QueryStringParameter)) {
                                        newParameterEditor = _queryStringParameterEditor; 
                                    }
                                    else {
                                        if (parameterItem.Parameter.GetType() == typeof(CookieParameter)) {
                                            newParameterEditor = _cookieParameterEditor; 
                                        }
                                        else { 
                                            if (parameterItem.Parameter.GetType() == typeof(SessionParameter)) { 
                                                newParameterEditor = _sessionParameterEditor;
                                            } 
                                            else {
                                                if (parameterItem.Parameter.GetType() == typeof(ProfileParameter)) {
                                                    newParameterEditor = _profileParameterEditor;
                                                } 
                                                else {
                                                    // Unknown parameter... Do nothing? Perhaps have an UnknownParameterEditor without toggle 
                                                } 
                                            }
                                        } 
                                    }
                                }
                            }
                        } 
                    }
                } 
 
                // If the new editor is of a different type, swap editors
                if (_parameterEditor != newParameterEditor) { 
                    if (_parameterEditor != null) {
                        _parameterEditor.Visible = false;
                    }
                    _parameterEditor = newParameterEditor; 
                }
 
                // Initialize the new editor 
                if (_parameterEditor != null) {
                    _parameterEditor.InitializeParameter(parameterItem); 
                    _parameterEditor.Visible = true;
                    if (allowFocusChange) {
                        _parameterEditor.SetDefaultFocus();
                    } 
                }
            } 
        } 

        /// <devdoc> 
        /// Controls whether changes can be made to the parameter collection.
        /// This only disables the up/down/delete/add buttons - users can still edit the properties of existing parameters.
        /// </devdoc>
        public void SetAllowCollectionChanges(bool allowChanges) { 
            _moveUpButton.Visible = allowChanges;
            _moveDownButton.Visible = allowChanges; 
            _deleteParameterButton.Visible = allowChanges; 
            _addParameterButton.Visible = allowChanges;
        } 

        private void ToggleAdvancedMode(object sender, EventArgs e) {
            _inAdvancedMode = !_inAdvancedMode;
            UpdateUI(true); 
        }
 
        /// <devdoc> 
        /// Updates the UI to reflect the enabled state of buttons and the selected parameter type.
        /// </devdoc> 
        private void UpdateUI(bool allowFocusChange) {
            if (_parametersListView.SelectedItems.Count > 0) {
                ParameterListViewItem item = (ParameterListViewItem)_parametersListView.SelectedItems[0];
 
                _deleteParameterButton.Enabled = true;
                _moveUpButton.Enabled = (_parametersListView.SelectedIndices[0] > 0); 
                _moveDownButton.Enabled = (_parametersListView.SelectedIndices[0] < _parametersListView.Items.Count - 1); 
                _sourceLabel.Enabled = true;
                _parameterTypeComboBox.Enabled = true; 
                _editorPanel.Enabled = true;
                // Select the proper type in the drop down listbox
                if (item.Parameter == null) {
                    _parameterTypeComboBox.SelectedIndex = -1; 
                }
                else { 
                    Type t = item.Parameter.GetType(); 
                    object typeName = _parameterTypes[t];
 
                    if (typeName != null) {
                        _parameterTypeComboBox.SelectedItem = typeName;
                    }
                    else { 
                        _parameterTypeComboBox.SelectedIndex = -1;
                    } 
                } 
                SetActiveEditParameterItem(item, allowFocusChange);
            } 
            else {
                _deleteParameterButton.Enabled = false;
                _moveUpButton.Enabled = false;
                _moveDownButton.Enabled = false; 
                _sourceLabel.Enabled = false;
                _parameterTypeComboBox.Enabled = false; 
                _parameterTypeComboBox.SelectedIndex = -1; 

                _editorPanel.Enabled = false; 
                SetActiveEditParameterItem(null, false);
            }
        }
 
        internal sealed class ControlItem {
            private string _controlID; 
            private string _propertyName; 

            public ControlItem(string controlID, string propertyName) { 
                _controlID = controlID;
                _propertyName = propertyName;
            }
 
            public string ControlID {
                get { 
                    return _controlID; 
                }
            } 

            public string PropertyName {
                get {
                    return _propertyName; 
                }
            } 
 
            private static bool IsValidComponent(IComponent component) {
                System.Web.UI.Control control = component as System.Web.UI.Control; 
                if (control == null) {
                    return false;
                }
                if (String.IsNullOrEmpty(control.ID)) { 
                    return false;
                } 
                return true; 
            }
 
            /// <devdoc>
            /// Returns a list of all ControlItems representing the controls in the container.
            /// </devdoc>
            public static ControlItem[] GetControlItems(IDesignerHost host, System.Web.UI.Control control) { 

                IList<IComponent> allComponents = ControlHelper.GetAllComponents(control, new ControlHelper.IsValidComponentDelegate(IsValidComponent)); 
 
                List<ControlItem> items = new List<ControlItem>();
                foreach (System.Web.UI.Control c in allComponents) { 
                    string defaultPropertyName = GetDefaultValuePropertyName(c);
                    if (!String.IsNullOrEmpty(defaultPropertyName)) {
                        items.Add(new ControlItem(c.ID, defaultPropertyName));
                    } 
                }
 
                return items.ToArray(); 
            }
 
            public override string ToString() {
                return _controlID;
            }
        } 

        /// <devdoc> 
        /// A ListView item that represents a parameter object. 
        /// </devdoc>
        private class ParameterListViewItem : ListViewItem { 
            private Parameter _parameter;
            private bool _isConfigured;

            /// <devdoc> 
            /// Creates a new ParameterListViewItem with a given parameter.
            /// </devdoc> 
            public ParameterListViewItem(Parameter parameter) { 
                Debug.Assert(parameter != null);
                _parameter = parameter; 
                _isConfigured = true;
            }

            public bool IsConfigured { 
                get {
                    return _isConfigured; 
                } 
            }
 
            /// <devdoc>
            /// The name of the parameter.
            /// </devdoc>
            public string ParameterName { 
                get {
                    return _parameter.Name; 
                } 
                set {
                    _parameter.Name = value; 
                }
            }

            /// <devdoc> 
            /// The type of the parameter.
            /// </devdoc> 
            public TypeCode ParameterType { 
                get {
                    return _parameter.Type; 
                }
                set {
                    _parameter.Type = value;
                } 
            }
 
            /// <devdoc> 
            /// The parameter associated with this ListViewItem.
            /// </devdoc> 
            public Parameter Parameter {
                get {
                    return _parameter;
                } 
                set {
                    string defaultValue = _parameter.DefaultValue; 
                    ParameterDirection direction = _parameter.Direction; 
                    string name = _parameter.Name;
                    bool treatEmptyStringsAsNull = _parameter.ConvertEmptyStringToNull; 
                    int size = _parameter.Size;
                    TypeCode type = _parameter.Type;

                    _parameter = value; 

                    Debug.Assert(_parameter != null); 
                    _parameter.DefaultValue = defaultValue; 
                    _parameter.Direction = direction;
                    _parameter.Name = name; 
                    _parameter.ConvertEmptyStringToNull = treatEmptyStringsAsNull;
                    _parameter.Size = size;
                    _parameter.Type = type;
                } 
            }
 
            /// <devdoc> 
            /// Refreshes the properties of the ListViewItem.
            /// </devdoc> 
            public void Refresh() {
                SubItems.Clear();

                Text = ParameterName; 
                UseItemStyleForSubItems = false;
 
                bool isHelperText; 

                ListView listView = ListView; 
                IServiceProvider serviceProvider = null;
                System.Web.UI.Control control = null;
                if (listView != null) {
                    ParameterEditorUserControl parameterEditor = (ParameterEditorUserControl)listView.Parent; 
                    serviceProvider = parameterEditor._serviceProvider;
                    control = parameterEditor._control; 
                } 
                string parameterExpression = ParameterEditorUserControl.GetParameterExpression(serviceProvider, _parameter, control, out isHelperText);
                // If we get back helper text instead of an expression, that means 
                // the parameter is not fully configured. Typically this would cause
                // the containing UI to disable the user from progressing until
                // they fully configure all their parameters.
                _isConfigured = !isHelperText; 
                ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
                subItem.Text = parameterExpression; 
                if (isHelperText) { 
                    subItem.ForeColor = SystemColors.GrayText;
                } 
                SubItems.Add(subItem);
            }
        }
 
        /// <devdoc>
        /// An ISite for use in the property grid. This enables editors the property grid to 
        /// access the component's container. 
        /// </devdoc>
        private class PropertyGridSite : ISite { 
            private IServiceProvider _sp;
            private IComponent _comp;
            private bool _inGetService = false;
 
            public PropertyGridSite(IServiceProvider sp, IComponent comp) {
                _sp = sp; 
                _comp = comp; 
            }
 
            public IComponent Component {
                get {
                    return _comp;
                } 
            }
 
            public IContainer Container { 
                get {
                    return null; 
                }
            }

            public bool DesignMode { 
                get {
                    return false; 
                } 
            }
 
            public string Name {
                get {
                    return null;
                } 
                set {
                } 
            } 

            public object GetService(Type t) { 
                if ((!_inGetService) && (_sp != null)) {
                    try {
                        _inGetService = true;
                        return _sp.GetService(t); 
                    }
                    finally { 
                        _inGetService = false; 
                    }
                } 
                return null;
            }
        }
 
        /// <devdoc>
        /// An abstract base class for all parameter editors, including the advanced 
        /// property grid view, as well as the simple property editors. 
        /// </devdoc>
        private abstract class ParameterEditor : System.Windows.Forms.Panel { 
            private static readonly object EventParameterChanged = new object();
            private static readonly object EventRequestModeChange = new object();

            private IServiceProvider _serviceProvider; 
            private ParameterListViewItem _parameterItem;
 
            protected ParameterEditor(IServiceProvider serviceProvider) { 
                _serviceProvider = serviceProvider;
            } 

            protected ParameterListViewItem ParameterItem {
                get {
                    return _parameterItem; 
                }
            } 
 
            protected IServiceProvider ServiceProvider {
                get { 
                    return _serviceProvider;
                }
            }
 
            public event EventHandler ParameterChanged {
                add { 
                    Events.AddHandler(EventParameterChanged, value); 
                }
                remove { 
                    Events.RemoveHandler(EventParameterChanged, value);
                }
            }
 
            public event EventHandler RequestModeChange {
                add { 
                    Events.AddHandler(EventRequestModeChange, value); 
                }
                remove { 
                    Events.RemoveHandler(EventRequestModeChange, value);
                }
            }
 
            public virtual void InitializeParameter(ParameterListViewItem parameterItem) {
                _parameterItem = parameterItem; 
            } 

            /// <devdoc> 
            /// This method is called whenever the state of a parameter changes.
            /// It also raises the ParameterChanged event to notify external listeners.
            /// </devdoc>
            protected void OnParameterChanged() { 
                ParameterItem.Refresh();
 
                EventHandler handler = Events[EventParameterChanged] as EventHandler; 
                if (handler != null) {
                    handler(this, EventArgs.Empty); 
                }
            }

            protected void OnRequestModeChange() { 
                EventHandler handler = Events[EventRequestModeChange] as EventHandler;
                if (handler != null) { 
                    handler(this, EventArgs.Empty); 
                }
            } 

            public virtual void SetDefaultFocus() {
            }
        } 

        private sealed class AdvancedParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _advancedlabel; 
            private System.Windows.Forms.PropertyGrid _parameterPropertyGrid;
            private System.Windows.Forms.LinkLabel _hideAdvancedLinkLabel; 
            private System.Web.UI.Control _control;

            public AdvancedParameterEditor(IServiceProvider serviceProvider, System.Web.UI.Control control) : base(serviceProvider) {
                _control = control; 

                SuspendLayout(); 
                Size = new Size(400, 400); 

                _advancedlabel = new System.Windows.Forms.Label(); 
                _parameterPropertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider);
                _hideAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();

                _advancedlabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _advancedlabel.Location = new System.Drawing.Point(0, 0);
                _advancedlabel.Size = new System.Drawing.Size(400, 16); 
                _advancedlabel.TabIndex = 10; 
                _advancedlabel.Text = SR.GetString(SR.ParameterEditorUserControl_AdvancedProperties);
 
                _parameterPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _parameterPropertyGrid.CommandsVisibleIfAvailable = true;
                _parameterPropertyGrid.LargeButtons = false;
                _parameterPropertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar; 
                _parameterPropertyGrid.Location = new System.Drawing.Point(0, 18);
                _parameterPropertyGrid.PropertySort = PropertySort.Alphabetical; 
                _parameterPropertyGrid.Site = new PropertyGridSite(ServiceProvider, _parameterPropertyGrid); 
                _parameterPropertyGrid.Size = new System.Drawing.Size(400, 356);
                _parameterPropertyGrid.TabIndex = 20; 
                _parameterPropertyGrid.ToolbarVisible = false;
                _parameterPropertyGrid.ViewBackColor = System.Drawing.SystemColors.Window;
                _parameterPropertyGrid.ViewForeColor = System.Drawing.SystemColors.WindowText;
                _parameterPropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.OnParameterPropertyGridPropertyValueChanged); 

                _hideAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _hideAdvancedLinkLabel.Location = new System.Drawing.Point(0, 384); 
                _hideAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16);
                _hideAdvancedLinkLabel.TabIndex = 30; 
                _hideAdvancedLinkLabel.TabStop = true;
                _hideAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_HideAdvancedPropertiesLabel);
                _hideAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _hideAdvancedLinkLabel.Text.Length));
                _hideAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnHideAdvancedLinkLabelLinkClicked); 

                Controls.Add(_advancedlabel); 
                Controls.Add(_parameterPropertyGrid); 
                Controls.Add(_hideAdvancedLinkLabel);
 
                Dock = DockStyle.Fill;
                ResumeLayout();
            }
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 
 
                _parameterPropertyGrid.SelectedObject = ParameterItem.Parameter;
            } 

            private void OnHideAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange();
            } 

            private void OnParameterPropertyGridPropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e) { 
                // If the parameter is a control parameter, and its ControlID property was changed, set the default property name 
                if (e.ChangedItem.PropertyDescriptor.Name == "ControlID") {
                    ControlParameter controlParameter = ParameterItem.Parameter as ControlParameter; 
                    // Only change the PropertyName property if it is not already set, and if the ControlID property really changed
                    if ((controlParameter != null) && (controlParameter.PropertyName.Length == 0) && (controlParameter.ControlID != (string)e.OldValue)) {
                        // Get the ControlValuePropertyAttribute to determine the default property name
                        controlParameter.PropertyName = GetControlDefaultValuePropertyName(controlParameter.ControlID, ServiceProvider, _control); 
                    }
                } 
                OnParameterChanged(); 
            }
 
            public override void SetDefaultFocus() {
                _parameterPropertyGrid.Focus();
            }
        } 

        private sealed class ControlParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _controlIDLabel; 
            private AutoSizeComboBox _controlIDComboBox;
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel;
            private System.Web.UI.Control _control;
 
            public ControlParameterEditor(IServiceProvider serviceProvider, System.Web.UI.Control control) : base(serviceProvider) {
                _control = control; 
 
                SuspendLayout();
                Size = new Size(400, 400); 

                _controlIDLabel = new System.Windows.Forms.Label();
                _controlIDComboBox = new AutoSizeComboBox();
                _defaultValueLabel = new System.Windows.Forms.Label(); 
                _defaultValueTextBox = new System.Windows.Forms.TextBox();
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel(); 
 
                _controlIDLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _controlIDLabel.Location = new System.Drawing.Point(0, 0); 
                _controlIDLabel.Size = new System.Drawing.Size(400, 16);
                _controlIDLabel.TabIndex = 10;
                _controlIDLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ControlParameterControlID);
 
                _controlIDComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _controlIDComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; 
                _controlIDComboBox.Location = new System.Drawing.Point(0, 18); 
                _controlIDComboBox.Size = new System.Drawing.Size(400, 21);
                _controlIDComboBox.Sorted = true; 
                _controlIDComboBox.TabIndex = 20;
                _controlIDComboBox.SelectedIndexChanged += new System.EventHandler(this.OnControlIDComboBoxSelectedIndexChanged);

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 45);
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16); 
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 63);
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 87);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true;
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length)); 
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked);
 
                Controls.Add(_controlIDLabel); 
                Controls.Add(_controlIDComboBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel);

                Dock = DockStyle.Fill; 
                ResumeLayout();
            } 
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is ControlParameter);

                string initialControlID = ((ControlParameter)ParameterItem.Parameter).ControlID; 
                string initialPropertyName = ((ControlParameter)ParameterItem.Parameter).PropertyName;
 
                _controlIDComboBox.Items.Clear(); 

                // Populate the ControlID dropdown with all controls that have a default property 
                ControlItem selectedItem = null;
                if (ServiceProvider != null) {
                    IDesignerHost designerHost = (IDesignerHost)ServiceProvider.GetService(typeof(IDesignerHost));
                    if (designerHost != null) { 
                        ControlItem[] controlItems = ControlItem.GetControlItems(designerHost, _control);
                        foreach (ControlItem controlItem in controlItems) { 
                            _controlIDComboBox.Items.Add(controlItem); 
                            if ((controlItem.ControlID == initialControlID) && (controlItem.PropertyName == initialPropertyName)) {
                                selectedItem = controlItem; 
                            }
                        }
                    }
                } 

                // Add a custom entry if the current control is not already in the list 
                if (selectedItem == null) { 
                    if (initialControlID.Length > 0) {
                        // If the control already selected is not in the standard values list, add it 
                        ControlItem customItem = new ControlItem(initialControlID, initialPropertyName);
                        _controlIDComboBox.Items.Insert(0, customItem);
                        selectedItem = customItem;
                    } 
                }
 
                _controlIDComboBox.InvalidateDropDownWidth(); 

                _controlIDComboBox.SelectedItem = selectedItem; 

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue;
            }
 
            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange(); 
            } 

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) {
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged();
                } 
            }
 
            private void OnControlIDComboBoxSelectedIndexChanged(object s, System.EventArgs e) { 
                ControlItem controlItem = _controlIDComboBox.SelectedItem as ControlItem;
 
                ControlParameter controlParameter = (ControlParameter)ParameterItem.Parameter;

                if (controlItem == null) {
                    controlParameter.ControlID = String.Empty; 
                    controlParameter.PropertyName = String.Empty;
                } 
                else { 
                    controlParameter.ControlID = controlItem.ControlID;
                    controlParameter.PropertyName = controlItem.PropertyName; 
                }

                OnParameterChanged();
            } 

            public override void SetDefaultFocus() { 
                _controlIDComboBox.Focus(); 
            }
        } 

        private sealed class CookieParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _cookieNameLabel;
            private System.Windows.Forms.TextBox _cookieNameTextBox; 
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox; 
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel; 

            public CookieParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400);

                _cookieNameLabel = new System.Windows.Forms.Label(); 
                _cookieNameTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label(); 
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();
 
                _cookieNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _cookieNameLabel.Location = new System.Drawing.Point(0, 0);
                _cookieNameLabel.Size = new System.Drawing.Size(400, 16);
                _cookieNameLabel.TabIndex = 10; 
                _cookieNameLabel.Text = SR.GetString(SR.ParameterEditorUserControl_CookieParameterCookieName);
 
                _cookieNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _cookieNameTextBox.Location = new System.Drawing.Point(0, 18);
                _cookieNameTextBox.Size = new System.Drawing.Size(400, 20); 
                _cookieNameTextBox.TabIndex = 20;
                _cookieNameTextBox.TextChanged += new System.EventHandler(this.OnCookieNameTextBoxTextChanged);

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44);
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16); 
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62);
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true;
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length)); 
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked);
 
                Controls.Add(_cookieNameLabel); 
                Controls.Add(_cookieNameTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel);

                Dock = DockStyle.Fill; 
                ResumeLayout();
            } 
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is CookieParameter);

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue; 
                _cookieNameTextBox.Text = ((CookieParameter)ParameterItem.Parameter).CookieName;
            } 
 
            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange(); 
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) { 
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged(); 
                } 
            }
 
            private void OnCookieNameTextBoxTextChanged(object s, System.EventArgs e) {
                if (((CookieParameter)ParameterItem.Parameter).CookieName != _cookieNameTextBox.Text) {
                    ((CookieParameter)ParameterItem.Parameter).CookieName = _cookieNameTextBox.Text;
                    OnParameterChanged(); 
                }
            } 
 
            public override void SetDefaultFocus() {
                _cookieNameTextBox.Focus(); 
            }
        }

        private sealed class FormParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _formFieldLabel;
            private System.Windows.Forms.TextBox _formFieldTextBox; 
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel; 

            public FormParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) {
                SuspendLayout();
                Size = new Size(400, 400); 

                _formFieldLabel = new System.Windows.Forms.Label(); 
                _formFieldTextBox = new System.Windows.Forms.TextBox(); 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();

                _formFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _formFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _formFieldLabel.Size = new System.Drawing.Size(400, 16);
                _formFieldLabel.TabIndex = 10; 
                _formFieldLabel.Text = SR.GetString(SR.ParameterEditorUserControl_FormParameterFormField); 

                _formFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _formFieldTextBox.Location = new System.Drawing.Point(0, 18);
                _formFieldTextBox.Size = new System.Drawing.Size(400, 20);
                _formFieldTextBox.TabIndex = 20;
                _formFieldTextBox.TextChanged += new System.EventHandler(this.OnFormFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44); 
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16);
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62); 
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16);
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true; 
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length)); 
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked); 

                Controls.Add(_formFieldLabel); 
                Controls.Add(_formFieldTextBox);
                Controls.Add(_defaultValueLabel);
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel); 

                Dock = DockStyle.Fill; 
                ResumeLayout(); 
            }
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem);

                Debug.Assert(parameterItem.Parameter is FormParameter); 

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue; 
                _formFieldTextBox.Text = ((FormParameter)ParameterItem.Parameter).FormField; 
            }
 
            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange();
            }
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) { 
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text; 
                    OnParameterChanged();
                } 
            }

            private void OnFormFieldTextBoxTextChanged(object s, System.EventArgs e) {
                if (((FormParameter)ParameterItem.Parameter).FormField != _formFieldTextBox.Text) { 
                    ((FormParameter)ParameterItem.Parameter).FormField = _formFieldTextBox.Text;
                    OnParameterChanged(); 
                } 
            }
 
            public override void SetDefaultFocus() {
                _formFieldTextBox.Focus();
            }
        } 

        private sealed class SessionParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _sessionFieldLabel; 
            private System.Windows.Forms.TextBox _sessionFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel;

            public SessionParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400); 
 
                _sessionFieldLabel = new System.Windows.Forms.Label();
                _sessionFieldTextBox = new System.Windows.Forms.TextBox(); 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox();
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();
 
                _sessionFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _sessionFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _sessionFieldLabel.Size = new System.Drawing.Size(400, 16); 
                _sessionFieldLabel.TabIndex = 10;
                _sessionFieldLabel.Text = SR.GetString(SR.ParameterEditorUserControl_SessionParameterSessionField); 

                _sessionFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _sessionFieldTextBox.Location = new System.Drawing.Point(0, 18);
                _sessionFieldTextBox.Size = new System.Drawing.Size(400, 20); 
                _sessionFieldTextBox.TabIndex = 20;
                _sessionFieldTextBox.TextChanged += new System.EventHandler(this.OnSessionFieldTextBoxTextChanged); 
 
                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44); 
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16);
                _defaultValueLabel.TabIndex = 30;
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62); 
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20); 
                _defaultValueTextBox.TabIndex = 40;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true; 
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties); 
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length));
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked); 

                Controls.Add(_sessionFieldLabel);
                Controls.Add(_sessionFieldTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel); 
 
                Dock = DockStyle.Fill;
                ResumeLayout(); 
            }

            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is SessionParameter); 
 
                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue;
                _sessionFieldTextBox.Text = ((SessionParameter)ParameterItem.Parameter).SessionField; 
            }

            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange(); 
            }
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) {
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text; 
                    OnParameterChanged();
                }
            }
 
            private void OnSessionFieldTextBoxTextChanged(object s, System.EventArgs e) {
                if (((SessionParameter)ParameterItem.Parameter).SessionField != _sessionFieldTextBox.Text) { 
                    ((SessionParameter)ParameterItem.Parameter).SessionField = _sessionFieldTextBox.Text; 
                    OnParameterChanged();
                } 
            }

            public override void SetDefaultFocus() {
                _sessionFieldTextBox.Focus(); 
            }
        } 
 
        private sealed class StaticParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _defaultValueLabel; 
            private System.Windows.Forms.TextBox _defaultValueTextBox;
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel;

            public StaticParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400); 
 
                _defaultValueLabel = new System.Windows.Forms.Label();
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 0); 
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16);
                _defaultValueLabel.TabIndex = 10; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue); 

                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 18);
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 20;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged); 

                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 42); 
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16);
                _showAdvancedLinkLabel.TabIndex = 30; 
                _showAdvancedLinkLabel.TabStop = true;
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length));
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked); 

                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox); 
                Controls.Add(_showAdvancedLinkLabel);
 
                Dock = DockStyle.Fill;
                ResumeLayout();
            }
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 
 
                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue;
            } 

            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange();
            } 

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) { 
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) { 
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged(); 
                }
            }

            public override void SetDefaultFocus() { 
                _defaultValueTextBox.Focus();
            } 
        } 

        private sealed class QueryStringParameterEditor : ParameterEditor { 
            private System.Windows.Forms.Label _queryStringFieldLabel;
            private System.Windows.Forms.TextBox _queryStringFieldTextBox;
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox; 
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel;
 
            public QueryStringParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400); 

                _queryStringFieldLabel = new System.Windows.Forms.Label();
                _queryStringFieldTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label(); 
                _defaultValueTextBox = new System.Windows.Forms.TextBox();
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel(); 
 
                _queryStringFieldLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _queryStringFieldLabel.Location = new System.Drawing.Point(0, 0); 
                _queryStringFieldLabel.Size = new System.Drawing.Size(400, 16);
                _queryStringFieldLabel.TabIndex = 10;
                _queryStringFieldLabel.Text = SR.GetString(SR.ParameterEditorUserControl_QueryStringParameterQueryStringField);
 
                _queryStringFieldTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _queryStringFieldTextBox.Location = new System.Drawing.Point(0, 18); 
                _queryStringFieldTextBox.Size = new System.Drawing.Size(400, 20); 
                _queryStringFieldTextBox.TabIndex = 20;
                _queryStringFieldTextBox.TextChanged += new System.EventHandler(this.OnQueryStringFieldTextBoxTextChanged); 

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44);
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16); 
                _defaultValueLabel.TabIndex = 30;
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue); 
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62); 
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40;
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86); 
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true; 
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length));
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked);
 
                Controls.Add(_queryStringFieldLabel);
                Controls.Add(_queryStringFieldTextBox); 
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel); 

                Dock = DockStyle.Fill;
                ResumeLayout();
            } 

            public override void InitializeParameter(ParameterListViewItem parameterItem) { 
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is QueryStringParameter); 

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue;
                _queryStringFieldTextBox.Text = ((QueryStringParameter)ParameterItem.Parameter).QueryStringField;
            } 

            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) { 
                OnRequestModeChange(); 
            }
 
            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) {
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged(); 
                }
            } 
 
            private void OnQueryStringFieldTextBoxTextChanged(object s, System.EventArgs e) {
                if (((QueryStringParameter)ParameterItem.Parameter).QueryStringField != _queryStringFieldTextBox.Text) { 
                    ((QueryStringParameter)ParameterItem.Parameter).QueryStringField = _queryStringFieldTextBox.Text;
                    OnParameterChanged();
                }
            } 

            public override void SetDefaultFocus() { 
                _queryStringFieldTextBox.Focus(); 
            }
        } 

        private sealed class ProfileParameterEditor : ParameterEditor {
            private System.Windows.Forms.Label _propertyNameLabel;
            private System.Windows.Forms.TextBox _propertyNameTextBox; 
            private System.Windows.Forms.Label _defaultValueLabel;
            private System.Windows.Forms.TextBox _defaultValueTextBox; 
            private System.Windows.Forms.LinkLabel _showAdvancedLinkLabel; 

            public ProfileParameterEditor(IServiceProvider serviceProvider) : base(serviceProvider) { 
                SuspendLayout();
                Size = new Size(400, 400);

                _propertyNameLabel = new System.Windows.Forms.Label(); 
                _propertyNameTextBox = new System.Windows.Forms.TextBox();
                _defaultValueLabel = new System.Windows.Forms.Label(); 
                _defaultValueTextBox = new System.Windows.Forms.TextBox(); 
                _showAdvancedLinkLabel = new System.Windows.Forms.LinkLabel();
 
                _propertyNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _propertyNameLabel.Location = new System.Drawing.Point(0, 0);
                _propertyNameLabel.Size = new System.Drawing.Size(400, 16);
                _propertyNameLabel.TabIndex = 10; 
                _propertyNameLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ProfilePropertyName);
 
                _propertyNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _propertyNameTextBox.Location = new System.Drawing.Point(0, 18);
                _propertyNameTextBox.Size = new System.Drawing.Size(400, 20); 
                _propertyNameTextBox.TabIndex = 20;
                _propertyNameTextBox.TextChanged += new System.EventHandler(this.OnPropertyNameTextBoxTextChanged);

                _defaultValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _defaultValueLabel.Location = new System.Drawing.Point(0, 44);
                _defaultValueLabel.Size = new System.Drawing.Size(400, 16); 
                _defaultValueLabel.TabIndex = 30; 
                _defaultValueLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ParameterDefaultValue);
 
                _defaultValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
                _defaultValueTextBox.Location = new System.Drawing.Point(0, 62);
                _defaultValueTextBox.Size = new System.Drawing.Size(400, 20);
                _defaultValueTextBox.TabIndex = 40; 
                _defaultValueTextBox.TextChanged += new System.EventHandler(this.OnDefaultValueTextBoxTextChanged);
 
                _showAdvancedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); 
                _showAdvancedLinkLabel.Location = new System.Drawing.Point(0, 86);
                _showAdvancedLinkLabel.Size = new System.Drawing.Size(400, 16); 
                _showAdvancedLinkLabel.TabIndex = 50;
                _showAdvancedLinkLabel.TabStop = true;
                _showAdvancedLinkLabel.Text = SR.GetString(SR.ParameterEditorUserControl_ShowAdvancedProperties);
                _showAdvancedLinkLabel.Links.Add(new System.Windows.Forms.LinkLabel.Link(0, _showAdvancedLinkLabel.Text.Length)); 
                _showAdvancedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnShowAdvancedLinkLabelLinkClicked);
 
                Controls.Add(_propertyNameLabel); 
                Controls.Add(_propertyNameTextBox);
                Controls.Add(_defaultValueLabel); 
                Controls.Add(_defaultValueTextBox);
                Controls.Add(_showAdvancedLinkLabel);

                Dock = DockStyle.Fill; 
                ResumeLayout();
            } 
 
            public override void InitializeParameter(ParameterListViewItem parameterItem) {
                base.InitializeParameter(parameterItem); 

                Debug.Assert(parameterItem.Parameter is ProfileParameter);

                _defaultValueTextBox.Text = ParameterItem.Parameter.DefaultValue; 
                _propertyNameTextBox.Text = ((ProfileParameter)ParameterItem.Parameter).PropertyName;
            } 
 
            private void OnShowAdvancedLinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
                OnRequestModeChange(); 
            }

            private void OnDefaultValueTextBoxTextChanged(object s, System.EventArgs e) {
                if (ParameterItem.Parameter.DefaultValue != _defaultValueTextBox.Text) { 
                    ParameterItem.Parameter.DefaultValue = _defaultValueTextBox.Text;
                    OnParameterChanged(); 
                } 
            }
 
            private void OnPropertyNameTextBoxTextChanged(object s, System.EventArgs e) {
                if (((ProfileParameter)ParameterItem.Parameter).PropertyName != _propertyNameTextBox.Text) {
                    ((ProfileParameter)ParameterItem.Parameter).PropertyName = _propertyNameTextBox.Text;
                    OnParameterChanged(); 
                }
            } 
 
            public override void SetDefaultFocus() {
                _propertyNameTextBox.Focus(); 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
