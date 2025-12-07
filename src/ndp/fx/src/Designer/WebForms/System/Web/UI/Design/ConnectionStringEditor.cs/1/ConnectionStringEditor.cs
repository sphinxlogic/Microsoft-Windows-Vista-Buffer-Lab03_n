//------------------------------------------------------------------------------ 
// <copyright file="ConnectionStringEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing.Design; 
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.Design.Util; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design;

    using WebUIControl = System.Web.UI.Control; 

    /// <devdoc> 
    /// Provides an editor for visually picking a connection string. If the IDataEnvironment 
    /// service is available, connection strings will be retrieved from it (either coming from
    /// web.config or from Data Explorer). If the service is not available, a modal dialog is 
    /// shown to allow editing of the connection string. A sample connection string is shown
    /// for certain providers.
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ConnectionStringEditor : UITypeEditor {
 
        private ConnectionStringPicker _connectionStringPicker; 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            Debug.Assert(context.PropertyDescriptor != null, "Did not expect null property descriptor");

            WebUIControl control = context.Instance as WebUIControl;
 
            if (provider != null) {
                IDataEnvironment dataEnvironment = (IDataEnvironment)provider.GetService(typeof(IDataEnvironment)); 
                if (dataEnvironment != null) { 
                    // Show dropdown picker with connections from Data Explorer and web.config
 
                    IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                    if (edSvc != null && context.Instance != null) {
                        if (_connectionStringPicker == null) { 
                            _connectionStringPicker = new ConnectionStringPicker();
                        } 
 
                        // Detect the current connection that is set. This can be either a configured connection
                        // that is using an ExpressionBuilder, or just a regular property set to a string. 
                        string previousConnectionString = (string)value;

                        ExpressionEditor connectionStringExpressionEditor = ExpressionEditor.GetExpressionEditor(typeof(System.Web.Compilation.ConnectionStringsExpressionBuilder), provider);
                        if (connectionStringExpressionEditor != null) { 
                            string connectionStringExpressionPrefix = connectionStringExpressionEditor.ExpressionPrefix;
 
                            DesignerDataConnection currentConnection = GetCurrentConnection(control, context.PropertyDescriptor.Name, previousConnectionString, connectionStringExpressionPrefix); 

                            // Launch the editor 
                            _connectionStringPicker.Start(edSvc, dataEnvironment.Connections, currentConnection);
                            edSvc.DropDownControl(_connectionStringPicker);
                            if (_connectionStringPicker.SelectedItem != null) {
                                DesignerDataConnection connection = _connectionStringPicker.SelectedConnection; 
                                if (connection == null) {
                                    // A null connection means the user clicked "New Connection" 
                                    connection = dataEnvironment.BuildConnection(UIServiceHelper.GetDialogOwnerWindow(provider), null); 
                                }
                                if (connection != null) { 
                                    if (connection.IsConfigured) {
                                        // Use Expressions to set the configured connection
                                        ExpressionBindingCollection expressionBindings = ((IExpressionsAccessor)control).Expressions;
                                        expressionBindings.Add(new ExpressionBinding(context.PropertyDescriptor.Name, context.PropertyDescriptor.PropertyType, connectionStringExpressionPrefix, connection.Name)); 
                                        SetProviderName(context.Instance, connection);
 
                                        // We call OnComponentChanged *without* a MemberDescriptor to make the ControlDesigner 
                                        // realize that we added an expression, even though we didn't necessarily actually
                                        // change the property's value. 
                                        IComponentChangeService changeService = (IComponentChangeService)provider.GetService(typeof(IComponentChangeService));
                                        if (changeService != null) {
                                            changeService.OnComponentChanged(control, null, null, null);
                                        } 
                                    }
                                    else { 
                                        // Regular connection string, just set the property 
                                        value = connection.ConnectionString;
 
                                        SetProviderName(context.Instance, connection);
                                    }
                                }
                            } 
                            _connectionStringPicker.End();
                        } 
                    } 
                    return value;
                } 
            }

            // Show modal connection string editor (just a textbox) when service is not present
            string providerName = GetProviderName(context.Instance); 

            ConnectionStringEditorDialog form = new ConnectionStringEditorDialog(provider, providerName); 
            form.ConnectionString = (string)value; 
            DialogResult result = UIServiceHelper.ShowDialog(provider, form);
            if (result == DialogResult.OK) { 
                value = form.ConnectionString;
            }

            return value; 
        }
 
        private static DesignerDataConnection GetCurrentConnection(WebUIControl control, string propertyName, string connectionString, string expressionPrefix) { 
            DesignerDataConnection connection;
            ExpressionBindingCollection expressionBindings = ((IExpressionsAccessor)control).Expressions; 
            ExpressionBinding binding = expressionBindings[propertyName];
            string connectionStringExpressionSuffix = "." + SqlDataSourceSaveConfiguredConnectionPanel.ConnectionStringExpressionConnectionSuffix.ToLowerInvariant();
            if ((binding != null) &&
                (String.Equals(binding.ExpressionPrefix, expressionPrefix, StringComparison.OrdinalIgnoreCase))) { 
                // Expression bound connection string
                // Test for ".connectionstring" suffix, and remove if present 
                string connectionName; 
                string expressionValue = binding.Expression;
                if (expressionValue.ToLowerInvariant().EndsWith(connectionStringExpressionSuffix, StringComparison.Ordinal)) { 
                    connectionName = expressionValue.Substring(0, expressionValue.Length - connectionStringExpressionSuffix.Length);
                }
                else {
                    connectionName = expressionValue; 
                }
                connection = new DesignerDataConnection(binding.Expression, String.Empty, connectionString, true); 
            } 
            else {
                // Regular connection string, no bindings 
                connection = new DesignerDataConnection(String.Empty, String.Empty, connectionString, false);
            }
            return connection;
        } 

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            if (context != null) { 
                IDataEnvironment dataEnvironment = (IDataEnvironment)context.GetService(typeof(IDataEnvironment));
                if (dataEnvironment != null) { 
                    return UITypeEditorEditStyle.DropDown;
                }
            }
            return UITypeEditorEditStyle.Modal; 
        }
 
        /// <devdoc> 
        /// Gets the provider name for the current instance. For example, in
        /// the SqlDataSourceConnectionStringEditor, this returns the value 
        /// of the ProviderName property.
        /// The default implementation returns no provider name.
        /// </devdoc>
        protected virtual string GetProviderName(object instance) { 
            return String.Empty;
        } 
 
        /// <devdoc>
        /// Sets the provider name on the current instance. For example, in 
        /// the SqlDataSourceConnectionStringEditor, this either sets the
        /// ProviderName property, or adds an ExpressionBinding if the connection
        /// is a configured connection.
        /// The default implementation does nothing. 
        /// </devdoc>
        protected virtual void SetProviderName(object instance, DesignerDataConnection connection) { 
        } 

 
        /// <devdoc>
        /// Picker listbox that displays available connections.
        /// </devdoc>
        private sealed class ConnectionStringPicker : ListBox { 

            private IWindowsFormsEditorService _edSvc; 
            private bool _keyDown = false; 
            private bool _mouseClicked = false;
 
            public ConnectionStringPicker() {
                BorderStyle = BorderStyle.None;
            }
 
            public DesignerDataConnection SelectedConnection {
                get { 
                    DataConnectionItem item = SelectedItem as DataConnectionItem; 
                    if (item != null) {
                        return item.DesignerDataConnection; 
                    }
                    return null;
                }
            } 

            public void End() { 
                Items.Clear(); 
                _edSvc = null;
            } 

            protected override void OnKeyUp(KeyEventArgs e) {
                base.OnKeyUp(e);
 
                _keyDown = true;
                _mouseClicked = false; 
 
                if (e.KeyData == Keys.Return) {
                    _keyDown = false; 
                    _edSvc.CloseDropDown();
                }
            }
 
            protected override void OnMouseDown(MouseEventArgs e) {
                base.OnMouseDown(e); 
                _mouseClicked = true; 
            }
 
            protected override void OnMouseUp(MouseEventArgs e) {
                base.OnMouseUp(e);
                _mouseClicked = false;
            } 

            protected override void OnSelectedIndexChanged(EventArgs e) { 
                base.OnSelectedIndexChanged(e); 

                // selecting an item w/ the keyboard is done via 
                // On_keyDown. we will select an item w/ the mouse,
                // if this was the last thing that the user did
                if (_mouseClicked && !_keyDown) {
                    _mouseClicked = false; 
                    _keyDown = false;
                    _edSvc.CloseDropDown(); 
                } 

                return; 
            }

            public void Start(IWindowsFormsEditorService edSvc, ICollection connections, DesignerDataConnection currentConnection) {
                Debug.Assert(connections != null, "connections should not be null"); 

                _edSvc = edSvc; 
                Items.Clear(); 
                object selectedItem = null;
                foreach (DesignerDataConnection connection in connections) { 
                    DataConnectionItem item = new DataConnectionItem(connection);
                    if ((connection.ConnectionString == currentConnection.ConnectionString) &&
                        (connection.IsConfigured == currentConnection.IsConfigured)) {
                        selectedItem = item; 
                    }
                    Items.Add(item); 
                } 
                Items.Add(new DataConnectionItem());
 
                SelectedItem = selectedItem;
            }

            /// <devdoc> 
            /// Represents a connection a user can select.
            /// </devdoc> 
            private sealed class DataConnectionItem { 
                private DesignerDataConnection _designerDataConnection;
 
                public DataConnectionItem() {
                }

                public DataConnectionItem(DesignerDataConnection designerDataConnection) { 
                    Debug.Assert(designerDataConnection != null);
                    _designerDataConnection = designerDataConnection; 
                } 

                public DesignerDataConnection DesignerDataConnection { 
                    get {
                        return _designerDataConnection;
                    }
                } 

                public override string ToString() { 
                    if (_designerDataConnection == null) { 
                        return SR.GetString(SR.ConnectionStringEditor_NewConnection);
                    } 
                    else {
                        return _designerDataConnection.Name;
                    }
                } 
            }
        } 
 

        /// <devdoc> 
        /// Modal text editor.
        /// </devdoc>
        private sealed class ConnectionStringEditorDialog : DesignerForm {
 
            private System.Windows.Forms.Label _helpLabel;
            private System.Windows.Forms.Button _okButton; 
            private System.Windows.Forms.Button _cancelButton; 
            private System.Windows.Forms.TextBox _connectionStringTextBox;
 
            private NameValueCollection _defaultConnectionStrings;
            private string _providerName;

            public ConnectionStringEditorDialog(IServiceProvider serviceProvider, string providerName) : base(serviceProvider) { 
                InitializeComponent();
                InitializeUI(); 
 
                _providerName = providerName;
            } 

            public string ConnectionString {
                get {
                    return _connectionStringTextBox.Text; 
                }
                set { 
                    if (String.IsNullOrEmpty(value)) { 
                        // If there is no connection string, we show a default one
                        if (String.IsNullOrEmpty(_providerName)) { 
                            _connectionStringTextBox.Text = DefaultConnectionStrings["System.Data.SqlClient"];
                        }
                        else {
                            _connectionStringTextBox.Text = DefaultConnectionStrings[_providerName]; 
                        }
                    } 
                    else { 
                        _connectionStringTextBox.Text = value;
                    } 
                }
            }

            /// <devdoc> 
            /// Name/value pairs of default connection strings for all known ADO.net providers.
            /// </devdoc> 
            private NameValueCollection DefaultConnectionStrings { 
                get {
                    if (_defaultConnectionStrings == null) { 
                        _defaultConnectionStrings = new NameValueCollection();
                        _defaultConnectionStrings.Add("System.Data.SqlClient", "server=(local); trusted_connection=true; database=[database]");
                        _defaultConnectionStrings.Add("System.Data.Odbc", "Driver=[driver]; Server=[server]; Database=[database]; Uid=[username]; Pwd=[password]");
                        _defaultConnectionStrings.Add("System.Data.OleDb", "Provider=[provider]; Data Source=[server]; Initial Catalog=[database]; User Id=[username]; Password=[password]"); 
                        _defaultConnectionStrings.Add("System.Data.OracleClient", "Data Source=Oracle8i; Integrated Security=SSPI");
                    } 
                    return _defaultConnectionStrings; 
                }
            } 

            protected override string HelpTopic {
                get {
                    return "net.Asp.ConnectionStrings.Editor"; 
                }
            } 
 
            #region Windows Form Designer generated code
            private void InitializeComponent() { 
                this._helpLabel = new System.Windows.Forms.Label();
                this._okButton = new System.Windows.Forms.Button();
                this._cancelButton = new System.Windows.Forms.Button();
                this._connectionStringTextBox = new System.Windows.Forms.TextBox(); 
                this.SuspendLayout();
                // 
                // _helpLabel 
                //
                this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
                this._helpLabel.Location = new System.Drawing.Point(12, 12);
                this._helpLabel.Name = "_helpLabel";
                this._helpLabel.Size = new System.Drawing.Size(369, 16); 
                this._helpLabel.TabIndex = 10;
                // 
                // _okButton 
                //
                this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
                this._okButton.Location = new System.Drawing.Point(228, 233);
                this._okButton.Name = "_okButton";
                this._okButton.TabIndex = 30;
                this._okButton.Click += new System.EventHandler(this.OnOkButtonClick); 
                //
                // _cancelButton 
                // 
                this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
                this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
                this._cancelButton.Location = new System.Drawing.Point(310, 233);
                this._cancelButton.Name = "_cancelButton";
                this._cancelButton.TabIndex = 40;
                this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick); 
                //
                // _connectionStringTextBox 
                // 
                this._connectionStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
                this._connectionStringTextBox.Location = new System.Drawing.Point(12, 36);
                this._connectionStringTextBox.Multiline = true;
                this._connectionStringTextBox.Name = "_connectionStringTextBox"; 
                this._connectionStringTextBox.Size = new System.Drawing.Size(369, 190);
                this._connectionStringTextBox.TabIndex = 20; 
                // 
                // Form1
                // 
                this.AcceptButton = this._okButton;
                this.AutoSize = true;
                this.CancelButton = this._cancelButton;
                this.ClientSize = new System.Drawing.Size(392, 266); 
                this.Controls.Add(this._connectionStringTextBox);
                this.Controls.Add(this._cancelButton); 
                this.Controls.Add(this._okButton); 
                this.Controls.Add(this._helpLabel);
                this.MinimumSize = new System.Drawing.Size(400, 300); 
                this.Name = "Form1";
                this.SizeGripStyle = SizeGripStyle.Hide;

                InitializeForm(); 

                this.ResumeLayout(false); 
                this.PerformLayout(); 
            }
            #endregion 

            /// <devdoc>
            /// Called after InitializeComponent to perform additional actions that
            /// are not supported by the designer. 
            /// </devdoc>
            private void InitializeUI() { 
                _helpLabel.Text = SR.GetString(SR.ConnectionStringEditor_HelpLabel); 
                _okButton.Text = SR.GetString(SR.OK);
                _cancelButton.Text = SR.GetString(SR.Cancel); 

                Text = SR.GetString(SR.ConnectionStringEditor_Title);
            }
 
            private void OnCancelButtonClick(object sender, System.EventArgs e) {
                DialogResult = DialogResult.Cancel; 
                Close(); 
            }
 
            private void OnOkButtonClick(object sender, System.EventArgs e) {
                DialogResult = DialogResult.OK;
                Close();
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ConnectionStringEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing.Design; 
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.Design.Util; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design;

    using WebUIControl = System.Web.UI.Control; 

    /// <devdoc> 
    /// Provides an editor for visually picking a connection string. If the IDataEnvironment 
    /// service is available, connection strings will be retrieved from it (either coming from
    /// web.config or from Data Explorer). If the service is not available, a modal dialog is 
    /// shown to allow editing of the connection string. A sample connection string is shown
    /// for certain providers.
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ConnectionStringEditor : UITypeEditor {
 
        private ConnectionStringPicker _connectionStringPicker; 

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            Debug.Assert(context.PropertyDescriptor != null, "Did not expect null property descriptor");

            WebUIControl control = context.Instance as WebUIControl;
 
            if (provider != null) {
                IDataEnvironment dataEnvironment = (IDataEnvironment)provider.GetService(typeof(IDataEnvironment)); 
                if (dataEnvironment != null) { 
                    // Show dropdown picker with connections from Data Explorer and web.config
 
                    IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                    if (edSvc != null && context.Instance != null) {
                        if (_connectionStringPicker == null) { 
                            _connectionStringPicker = new ConnectionStringPicker();
                        } 
 
                        // Detect the current connection that is set. This can be either a configured connection
                        // that is using an ExpressionBuilder, or just a regular property set to a string. 
                        string previousConnectionString = (string)value;

                        ExpressionEditor connectionStringExpressionEditor = ExpressionEditor.GetExpressionEditor(typeof(System.Web.Compilation.ConnectionStringsExpressionBuilder), provider);
                        if (connectionStringExpressionEditor != null) { 
                            string connectionStringExpressionPrefix = connectionStringExpressionEditor.ExpressionPrefix;
 
                            DesignerDataConnection currentConnection = GetCurrentConnection(control, context.PropertyDescriptor.Name, previousConnectionString, connectionStringExpressionPrefix); 

                            // Launch the editor 
                            _connectionStringPicker.Start(edSvc, dataEnvironment.Connections, currentConnection);
                            edSvc.DropDownControl(_connectionStringPicker);
                            if (_connectionStringPicker.SelectedItem != null) {
                                DesignerDataConnection connection = _connectionStringPicker.SelectedConnection; 
                                if (connection == null) {
                                    // A null connection means the user clicked "New Connection" 
                                    connection = dataEnvironment.BuildConnection(UIServiceHelper.GetDialogOwnerWindow(provider), null); 
                                }
                                if (connection != null) { 
                                    if (connection.IsConfigured) {
                                        // Use Expressions to set the configured connection
                                        ExpressionBindingCollection expressionBindings = ((IExpressionsAccessor)control).Expressions;
                                        expressionBindings.Add(new ExpressionBinding(context.PropertyDescriptor.Name, context.PropertyDescriptor.PropertyType, connectionStringExpressionPrefix, connection.Name)); 
                                        SetProviderName(context.Instance, connection);
 
                                        // We call OnComponentChanged *without* a MemberDescriptor to make the ControlDesigner 
                                        // realize that we added an expression, even though we didn't necessarily actually
                                        // change the property's value. 
                                        IComponentChangeService changeService = (IComponentChangeService)provider.GetService(typeof(IComponentChangeService));
                                        if (changeService != null) {
                                            changeService.OnComponentChanged(control, null, null, null);
                                        } 
                                    }
                                    else { 
                                        // Regular connection string, just set the property 
                                        value = connection.ConnectionString;
 
                                        SetProviderName(context.Instance, connection);
                                    }
                                }
                            } 
                            _connectionStringPicker.End();
                        } 
                    } 
                    return value;
                } 
            }

            // Show modal connection string editor (just a textbox) when service is not present
            string providerName = GetProviderName(context.Instance); 

            ConnectionStringEditorDialog form = new ConnectionStringEditorDialog(provider, providerName); 
            form.ConnectionString = (string)value; 
            DialogResult result = UIServiceHelper.ShowDialog(provider, form);
            if (result == DialogResult.OK) { 
                value = form.ConnectionString;
            }

            return value; 
        }
 
        private static DesignerDataConnection GetCurrentConnection(WebUIControl control, string propertyName, string connectionString, string expressionPrefix) { 
            DesignerDataConnection connection;
            ExpressionBindingCollection expressionBindings = ((IExpressionsAccessor)control).Expressions; 
            ExpressionBinding binding = expressionBindings[propertyName];
            string connectionStringExpressionSuffix = "." + SqlDataSourceSaveConfiguredConnectionPanel.ConnectionStringExpressionConnectionSuffix.ToLowerInvariant();
            if ((binding != null) &&
                (String.Equals(binding.ExpressionPrefix, expressionPrefix, StringComparison.OrdinalIgnoreCase))) { 
                // Expression bound connection string
                // Test for ".connectionstring" suffix, and remove if present 
                string connectionName; 
                string expressionValue = binding.Expression;
                if (expressionValue.ToLowerInvariant().EndsWith(connectionStringExpressionSuffix, StringComparison.Ordinal)) { 
                    connectionName = expressionValue.Substring(0, expressionValue.Length - connectionStringExpressionSuffix.Length);
                }
                else {
                    connectionName = expressionValue; 
                }
                connection = new DesignerDataConnection(binding.Expression, String.Empty, connectionString, true); 
            } 
            else {
                // Regular connection string, no bindings 
                connection = new DesignerDataConnection(String.Empty, String.Empty, connectionString, false);
            }
            return connection;
        } 

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            if (context != null) { 
                IDataEnvironment dataEnvironment = (IDataEnvironment)context.GetService(typeof(IDataEnvironment));
                if (dataEnvironment != null) { 
                    return UITypeEditorEditStyle.DropDown;
                }
            }
            return UITypeEditorEditStyle.Modal; 
        }
 
        /// <devdoc> 
        /// Gets the provider name for the current instance. For example, in
        /// the SqlDataSourceConnectionStringEditor, this returns the value 
        /// of the ProviderName property.
        /// The default implementation returns no provider name.
        /// </devdoc>
        protected virtual string GetProviderName(object instance) { 
            return String.Empty;
        } 
 
        /// <devdoc>
        /// Sets the provider name on the current instance. For example, in 
        /// the SqlDataSourceConnectionStringEditor, this either sets the
        /// ProviderName property, or adds an ExpressionBinding if the connection
        /// is a configured connection.
        /// The default implementation does nothing. 
        /// </devdoc>
        protected virtual void SetProviderName(object instance, DesignerDataConnection connection) { 
        } 

 
        /// <devdoc>
        /// Picker listbox that displays available connections.
        /// </devdoc>
        private sealed class ConnectionStringPicker : ListBox { 

            private IWindowsFormsEditorService _edSvc; 
            private bool _keyDown = false; 
            private bool _mouseClicked = false;
 
            public ConnectionStringPicker() {
                BorderStyle = BorderStyle.None;
            }
 
            public DesignerDataConnection SelectedConnection {
                get { 
                    DataConnectionItem item = SelectedItem as DataConnectionItem; 
                    if (item != null) {
                        return item.DesignerDataConnection; 
                    }
                    return null;
                }
            } 

            public void End() { 
                Items.Clear(); 
                _edSvc = null;
            } 

            protected override void OnKeyUp(KeyEventArgs e) {
                base.OnKeyUp(e);
 
                _keyDown = true;
                _mouseClicked = false; 
 
                if (e.KeyData == Keys.Return) {
                    _keyDown = false; 
                    _edSvc.CloseDropDown();
                }
            }
 
            protected override void OnMouseDown(MouseEventArgs e) {
                base.OnMouseDown(e); 
                _mouseClicked = true; 
            }
 
            protected override void OnMouseUp(MouseEventArgs e) {
                base.OnMouseUp(e);
                _mouseClicked = false;
            } 

            protected override void OnSelectedIndexChanged(EventArgs e) { 
                base.OnSelectedIndexChanged(e); 

                // selecting an item w/ the keyboard is done via 
                // On_keyDown. we will select an item w/ the mouse,
                // if this was the last thing that the user did
                if (_mouseClicked && !_keyDown) {
                    _mouseClicked = false; 
                    _keyDown = false;
                    _edSvc.CloseDropDown(); 
                } 

                return; 
            }

            public void Start(IWindowsFormsEditorService edSvc, ICollection connections, DesignerDataConnection currentConnection) {
                Debug.Assert(connections != null, "connections should not be null"); 

                _edSvc = edSvc; 
                Items.Clear(); 
                object selectedItem = null;
                foreach (DesignerDataConnection connection in connections) { 
                    DataConnectionItem item = new DataConnectionItem(connection);
                    if ((connection.ConnectionString == currentConnection.ConnectionString) &&
                        (connection.IsConfigured == currentConnection.IsConfigured)) {
                        selectedItem = item; 
                    }
                    Items.Add(item); 
                } 
                Items.Add(new DataConnectionItem());
 
                SelectedItem = selectedItem;
            }

            /// <devdoc> 
            /// Represents a connection a user can select.
            /// </devdoc> 
            private sealed class DataConnectionItem { 
                private DesignerDataConnection _designerDataConnection;
 
                public DataConnectionItem() {
                }

                public DataConnectionItem(DesignerDataConnection designerDataConnection) { 
                    Debug.Assert(designerDataConnection != null);
                    _designerDataConnection = designerDataConnection; 
                } 

                public DesignerDataConnection DesignerDataConnection { 
                    get {
                        return _designerDataConnection;
                    }
                } 

                public override string ToString() { 
                    if (_designerDataConnection == null) { 
                        return SR.GetString(SR.ConnectionStringEditor_NewConnection);
                    } 
                    else {
                        return _designerDataConnection.Name;
                    }
                } 
            }
        } 
 

        /// <devdoc> 
        /// Modal text editor.
        /// </devdoc>
        private sealed class ConnectionStringEditorDialog : DesignerForm {
 
            private System.Windows.Forms.Label _helpLabel;
            private System.Windows.Forms.Button _okButton; 
            private System.Windows.Forms.Button _cancelButton; 
            private System.Windows.Forms.TextBox _connectionStringTextBox;
 
            private NameValueCollection _defaultConnectionStrings;
            private string _providerName;

            public ConnectionStringEditorDialog(IServiceProvider serviceProvider, string providerName) : base(serviceProvider) { 
                InitializeComponent();
                InitializeUI(); 
 
                _providerName = providerName;
            } 

            public string ConnectionString {
                get {
                    return _connectionStringTextBox.Text; 
                }
                set { 
                    if (String.IsNullOrEmpty(value)) { 
                        // If there is no connection string, we show a default one
                        if (String.IsNullOrEmpty(_providerName)) { 
                            _connectionStringTextBox.Text = DefaultConnectionStrings["System.Data.SqlClient"];
                        }
                        else {
                            _connectionStringTextBox.Text = DefaultConnectionStrings[_providerName]; 
                        }
                    } 
                    else { 
                        _connectionStringTextBox.Text = value;
                    } 
                }
            }

            /// <devdoc> 
            /// Name/value pairs of default connection strings for all known ADO.net providers.
            /// </devdoc> 
            private NameValueCollection DefaultConnectionStrings { 
                get {
                    if (_defaultConnectionStrings == null) { 
                        _defaultConnectionStrings = new NameValueCollection();
                        _defaultConnectionStrings.Add("System.Data.SqlClient", "server=(local); trusted_connection=true; database=[database]");
                        _defaultConnectionStrings.Add("System.Data.Odbc", "Driver=[driver]; Server=[server]; Database=[database]; Uid=[username]; Pwd=[password]");
                        _defaultConnectionStrings.Add("System.Data.OleDb", "Provider=[provider]; Data Source=[server]; Initial Catalog=[database]; User Id=[username]; Password=[password]"); 
                        _defaultConnectionStrings.Add("System.Data.OracleClient", "Data Source=Oracle8i; Integrated Security=SSPI");
                    } 
                    return _defaultConnectionStrings; 
                }
            } 

            protected override string HelpTopic {
                get {
                    return "net.Asp.ConnectionStrings.Editor"; 
                }
            } 
 
            #region Windows Form Designer generated code
            private void InitializeComponent() { 
                this._helpLabel = new System.Windows.Forms.Label();
                this._okButton = new System.Windows.Forms.Button();
                this._cancelButton = new System.Windows.Forms.Button();
                this._connectionStringTextBox = new System.Windows.Forms.TextBox(); 
                this.SuspendLayout();
                // 
                // _helpLabel 
                //
                this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
                this._helpLabel.Location = new System.Drawing.Point(12, 12);
                this._helpLabel.Name = "_helpLabel";
                this._helpLabel.Size = new System.Drawing.Size(369, 16); 
                this._helpLabel.TabIndex = 10;
                // 
                // _okButton 
                //
                this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right))); 
                this._okButton.Location = new System.Drawing.Point(228, 233);
                this._okButton.Name = "_okButton";
                this._okButton.TabIndex = 30;
                this._okButton.Click += new System.EventHandler(this.OnOkButtonClick); 
                //
                // _cancelButton 
                // 
                this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
                this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
                this._cancelButton.Location = new System.Drawing.Point(310, 233);
                this._cancelButton.Name = "_cancelButton";
                this._cancelButton.TabIndex = 40;
                this._cancelButton.Click += new System.EventHandler(this.OnCancelButtonClick); 
                //
                // _connectionStringTextBox 
                // 
                this._connectionStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
                this._connectionStringTextBox.Location = new System.Drawing.Point(12, 36);
                this._connectionStringTextBox.Multiline = true;
                this._connectionStringTextBox.Name = "_connectionStringTextBox"; 
                this._connectionStringTextBox.Size = new System.Drawing.Size(369, 190);
                this._connectionStringTextBox.TabIndex = 20; 
                // 
                // Form1
                // 
                this.AcceptButton = this._okButton;
                this.AutoSize = true;
                this.CancelButton = this._cancelButton;
                this.ClientSize = new System.Drawing.Size(392, 266); 
                this.Controls.Add(this._connectionStringTextBox);
                this.Controls.Add(this._cancelButton); 
                this.Controls.Add(this._okButton); 
                this.Controls.Add(this._helpLabel);
                this.MinimumSize = new System.Drawing.Size(400, 300); 
                this.Name = "Form1";
                this.SizeGripStyle = SizeGripStyle.Hide;

                InitializeForm(); 

                this.ResumeLayout(false); 
                this.PerformLayout(); 
            }
            #endregion 

            /// <devdoc>
            /// Called after InitializeComponent to perform additional actions that
            /// are not supported by the designer. 
            /// </devdoc>
            private void InitializeUI() { 
                _helpLabel.Text = SR.GetString(SR.ConnectionStringEditor_HelpLabel); 
                _okButton.Text = SR.GetString(SR.OK);
                _cancelButton.Text = SR.GetString(SR.Cancel); 

                Text = SR.GetString(SR.ConnectionStringEditor_Title);
            }
 
            private void OnCancelButtonClick(object sender, System.EventArgs e) {
                DialogResult = DialogResult.Cancel; 
                Close(); 
            }
 
            private void OnOkButtonClick(object sender, System.EventArgs e) {
                DialogResult = DialogResult.OK;
                Close();
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
