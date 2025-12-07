//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceMethodEditor.cs" company="Microsoft">
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
    using System.Reflection; 
    using System.Text; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    /// <devdoc>
    /// UserControl for choosing a method for an ObjectDataSource. 
    /// </devdoc>
    internal sealed class ObjectDataSourceMethodEditor : UserControl { 
 
        private static readonly object EventMethodChanged = new object();
 
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.Label _methodLabel;
        private AutoSizeComboBox _methodComboBox;
        private System.Windows.Forms.Label _signatureLabel; 
        private System.Windows.Forms.TextBox _signatureTextBox;
 
 
        /// <devdoc>
        /// Creates a new ObjectDataSourceMethodEditor. 
        /// </devdoc>
        public ObjectDataSourceMethodEditor() {
            InitializeComponent();
            InitializeUI(); 
        }
 
 
        /// <devdoc>
        /// The MethodInfo of the selected method, if any. 
        /// </devdoc>
        public MethodInfo MethodInfo {
            get {
                MethodItem item = _methodComboBox.SelectedItem as MethodItem; 
                if (item == null) {
                    return null; 
                } 
                return item.MethodInfo;
            } 
        }

        /// <devdoc>
        /// The type of the DataObject this method requires, if any. For a 
        /// DataObject to be needed, the method must take exactly one parameter
        /// that is a non-primitive (e.g. a Customer object, but not a string). 
        /// </devdoc> 
        public Type DataObjectType {
            get { 
                MethodInfo mi = MethodInfo;
                if (mi == null) {
                    return null;
                } 
                ParameterInfo[] parameters = mi.GetParameters();
                if (parameters.Length != 1) { 
                    return null; 
                }
                Type paramType = parameters[0].ParameterType; 
                if (IsPrimitiveType(paramType)) {
                    return null;
                }
                return paramType; 
            }
        } 
 
        /// <devdoc>
        /// Notifies listeners that the selected method has changed. 
        /// This is used by the method chooser panel to update the UI to
        /// reflect a newly chosen Select method.
        /// </devdoc>
        public event EventHandler MethodChanged { 
            add {
                Events.AddHandler(EventMethodChanged, value); 
            } 
            remove {
                Events.RemoveHandler(EventMethodChanged, value); 
            }
        }

        /// <devdoc> 
        /// Used by GetMethodSignature to pretty-format method signatures.
        /// </devdoc> 
        private static void AppendGenericArguments(Type[] args, StringBuilder sb) { 
            if (args.Length > 0) {
                sb.Append("<"); 
                for (int i = 0; i < args.Length; i++) {
                    AppendTypeName(args[i], false, sb);
                    if (i + 1 < args.Length) {
                        sb.Append(", "); 
                    }
                } 
                sb.Append(">"); 
            }
        } 

        /// <devdoc>
        /// Used by GetMethodSignature to pretty-format method signatures.
        /// </devdoc> 
        internal static void AppendTypeName(Type t, bool topLevelFullName, StringBuilder sb) {
            string name = (topLevelFullName ? t.FullName : t.Name); 
            if (t.IsGenericType) { 
                // Generic types, e.g. List<T, List<int>>[,,][]
                int indexOfTick = name.IndexOf("`", StringComparison.Ordinal); 
                if (indexOfTick == -1) {
                    // There are some bizarre inconsistencies in how the CLR shows type names, and
                    // for some generic types such as the type List<T>+Enumerator<T> you get back
                    // names such as List`1+Enumerator[T], with t.Name being just "Enumerator" 
                    // (without the backtick!).
                    indexOfTick = name.Length; 
                } 
                // Get base outer type name, e.g. List
                sb.Append(name.Substring(0, indexOfTick)); 

                // Get generic arguments, e.g. <T, List<int>>
                AppendGenericArguments(t.GetGenericArguments(), sb);
 
                if (indexOfTick < name.Length) {
                    // Get suffix, e.g. for an array [,,][] 
                    indexOfTick++; 
                    while ((indexOfTick < name.Length) && Char.IsNumber(name, indexOfTick)) {
                        indexOfTick++; 
                    }
                    sb.Append(name.Substring(indexOfTick));
                }
            } 
            else {
                // Non-generic types, e.g. string[][,,] 
                sb.Append(name); 
            }
        } 

        /// <devdoc>
        /// Determines whether a given method is applicable to the specified
        /// data source operation. Returns true if it is. 
        /// </devdoc>
        private bool FilterMethod(MethodInfo methodInfo, DataObjectMethodType methodType) { 
            if (methodType == DataObjectMethodType.Select) { 
                // The requirements for select methods are special since we
                // require certain return types 
                if (methodInfo.ReturnType == typeof(void)) {
                    // Must return a non-void type
                    return false;
                } 
            }
            else { 
                // The requirements for insert/update/delete methods are all the same 
                ParameterInfo[] parameters = methodInfo.GetParameters();
                Debug.Assert(parameters != null, "MethodInfo.GetParameters should not return null"); 
                if ((parameters == null) || (parameters.Length == 0)) {
                    // Must take at least one parameter
                    return false;
                } 
            }
 
            return true; 
        }
 
        /// <devdoc>
        /// Gets a string representing the signature of a method. This takes into
        /// account generic method arguments as well as generic types for the parameters
        /// and return values. 
        /// </devdoc>
        internal static string GetMethodSignature(MethodInfo mi) { 
            if (mi == null) { 
                return String.Empty;
            } 
            StringBuilder methodBuilder = new StringBuilder(128);
            methodBuilder.Append(mi.Name);
            AppendGenericArguments(mi.GetGenericArguments(), methodBuilder);
            methodBuilder.Append("("); 
            ParameterInfo[] parameters = mi.GetParameters();
            foreach (ParameterInfo pi in parameters) { 
                AppendTypeName(pi.ParameterType, false, methodBuilder); 
                methodBuilder.Append(" " + pi.Name);
                if (pi.Position + 1 < parameters.Length) { 
                    methodBuilder.Append(", ");
                }
            }
            methodBuilder.Append(")"); 
            if (mi.ReturnType != typeof(void)) {
                StringBuilder returnTypeBuilder = new StringBuilder(); 
                AppendTypeName(mi.ReturnType, false, returnTypeBuilder); 
                return SR.GetString(SR.ObjectDataSourceMethodEditor_SignatureFormat, methodBuilder, returnTypeBuilder);
            } 
            else {
                return methodBuilder.ToString();
            }
        } 

        #region Designer generated code 
        private void InitializeComponent() { 
            this._helpLabel = new System.Windows.Forms.Label();
            this._methodLabel = new System.Windows.Forms.Label(); 
            this._signatureLabel = new System.Windows.Forms.Label();
            this._methodComboBox = new AutoSizeComboBox();
            this._signatureTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout(); 
            //
            // _helpLabel 
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._helpLabel.Location = new System.Drawing.Point(12, 12);
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(487, 80);
            this._helpLabel.TabIndex = 10; 
            //
            // _methodLabel 
            // 
            this._methodLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._methodLabel.Location = new System.Drawing.Point(12, 98);
            this._methodLabel.Name = "_methodLabel";
            this._methodLabel.Size = new System.Drawing.Size(487, 16);
            this._methodLabel.TabIndex = 20; 
            //
            // _methodComboBox 
            // 
            this._methodComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._methodComboBox.Location = new System.Drawing.Point(12, 116); 
            this._methodComboBox.Name = "_methodComboBox";
            this._methodComboBox.Size = new System.Drawing.Size(300, 21);
            this._methodComboBox.Sorted = true;
            this._methodComboBox.TabIndex = 30; 
            this._methodComboBox.SelectedIndexChanged += new System.EventHandler(this.OnMethodComboBoxSelectedIndexChanged);
            // 
            // _signatureLabel 
            //
            this._signatureLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._signatureLabel.Location = new System.Drawing.Point(12, 145);
            this._signatureLabel.Name = "_signatureLabel";
            this._signatureLabel.Size = new System.Drawing.Size(487, 16); 
            this._signatureLabel.TabIndex = 40;
            // 
            // _signatureTextBox 
            //
            this._signatureTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._signatureTextBox.BackColor = System.Drawing.SystemColors.Control;
            this._signatureTextBox.Location = new System.Drawing.Point(12, 163);
            this._signatureTextBox.Multiline = true; 
            this._signatureTextBox.Name = "_signatureTextBox";
            this._signatureTextBox.ReadOnly = true; 
            this._signatureTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; 
            this._signatureTextBox.Size = new System.Drawing.Size(487, 48);
            this._signatureTextBox.TabIndex = 50; 
            this._signatureTextBox.Text = "";
            //
            // ObjectDataSourceMethodEditor
            // 
            this.Controls.Add(this._signatureTextBox);
            this.Controls.Add(this._methodComboBox); 
            this.Controls.Add(this._signatureLabel); 
            this.Controls.Add(this._methodLabel);
            this.Controls.Add(this._helpLabel); 
            this.Name = "ObjectDataSourceMethodEditor";
            this.Size = new System.Drawing.Size(511, 220);
            this.ResumeLayout(false);
        } 
        #endregion
 
        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() {
            _methodLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_MethodLabel);
            _signatureLabel.Text = SR.GetString(SR.ObjectDataSource_General_MethodSignatureLabel); 
        }
 
        private static bool IsPrimitiveType(Type t) { 
            Debug.Assert(t != null);
            Type underlyingType = Nullable.GetUnderlyingType(t); 
            if (underlyingType != null) {
                t = underlyingType;
            }
            return t.IsPrimitive || (t == typeof(string)) || (t == typeof(DateTime)) || (t == typeof(decimal)) || (t == typeof(object)); 
        }
 
        private void OnMethodChanged(EventArgs e) { 
            EventHandler handler = Events[EventMethodChanged] as EventHandler;
            if (handler != null) { 
                handler(this, e);
            }
        }
 
        private void OnMethodComboBoxSelectedIndexChanged(object sender, System.EventArgs e) {
            OnMethodChanged(EventArgs.Empty); 
 
            _signatureTextBox.Text = GetMethodSignature(MethodInfo);
        } 

        public void SetMethodInformation(MethodInfo[] methods, string selectedMethodName, ParameterCollection selectedParameters, DataObjectMethodType methodType, Type dataObjectType) {
            // Populate method list and auto-select current method
            try { 
                _signatureTextBox.Text = String.Empty;
                switch (methodType) { 
                    case DataObjectMethodType.Delete: 
                        _helpLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_DeleteHelpLabel);
                        break; 
                    case DataObjectMethodType.Insert:
                        _helpLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_InsertHelpLabel);
                        break;
                    case DataObjectMethodType.Select: 
                        _helpLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_SelectHelpLabel);
                        break; 
                    case DataObjectMethodType.Update: 
                        _helpLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_UpdateHelpLabel);
                        break; 
                    default:
                        Debug.Fail("Unexpected DataObjectMethodType: " + methodType);
                        break;
                } 
                _methodComboBox.BeginUpdate();
                _methodComboBox.Items.Clear(); 
                MethodItem selectedItem = null; 
                bool filteredByAttribute = false;
 
                foreach (MethodInfo methodInfo in methods) {
                    if (!FilterMethod(methodInfo, methodType)) {
                        continue;
                    } 
                    // Check if the method is decorated with the DataObjectMethodAttribute
 
                    // Indicates whether this method should be added to the combobox 
                    bool addMethod = false;
 
                    DataObjectMethodAttribute attr = Attribute.GetCustomAttribute(methodInfo, typeof(DataObjectMethodAttribute), true) as DataObjectMethodAttribute;
                    if ((attr != null) && (attr.MethodType == methodType)) {
                        if (!filteredByAttribute) {
                            // If this is the first time we see a decorated method of the right type, 
                            // clear out all the existing methods since they were all not filtered.
                            _methodComboBox.Items.Clear(); 
                        } 
                        filteredByAttribute = true;
                        addMethod = true; 
                    }
                    else {
                        // Method is not decorated, only add it if we are not filtered
                        if (!filteredByAttribute) { 
                            addMethod = true;
                        } 
                    } 

                    // Add this method to the list, and possibly auto-select it if it matches the signature 
                    bool isMatch = ObjectDataSourceDesigner.IsMatchingMethod(methodInfo, selectedMethodName, selectedParameters, dataObjectType);
                    if (addMethod || isMatch) {
                        MethodItem item = new MethodItem(methodInfo);
                        _methodComboBox.Items.Add(item); 

                        if (isMatch) { 
                            // If it's a solid match, auto-select it 
                            selectedItem = item;
                        } 
                        else {
                            // If it's not a solid match, but there is no method set for this
                            // operation, and it is the default method, auto-select it
                            if ((attr != null) && (attr.MethodType == methodType) && attr.IsDefault && (selectedMethodName.Length == 0)) { 
                                selectedItem = item;
                            } 
                        } 
                    }
                } 

                // Add an empty item so that the user can unselect a method
                // This has to be added as the last step since otherwise it might get cleared out by the method filter
                if (methodType != DataObjectMethodType.Select) { 
                    _methodComboBox.Items.Insert(0, new MethodItem(null));
                } 
                _methodComboBox.InvalidateDropDownWidth(); 

                _methodComboBox.SelectedItem = selectedItem; 
                //
            }
            finally {
                _methodComboBox.EndUpdate(); 
            }
        } 
 

        /// <devdoc> 
        /// Represents a method a user can select.
        /// </devdoc>
        private sealed class MethodItem {
            private MethodInfo _methodInfo; 

            public MethodItem(MethodInfo methodInfo) { 
                _methodInfo = methodInfo; 
            }
 
            public MethodInfo MethodInfo {
                get {
                    return _methodInfo;
                } 
            }
 
            public override string ToString() { 
                if (_methodInfo == null) {
                    return SR.GetString(SR.ObjectDataSourceMethodEditor_NoMethod); 
                }
                else {
                    return GetMethodSignature(_methodInfo);
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceMethodEditor.cs" company="Microsoft">
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
    using System.Reflection; 
    using System.Text; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    /// <devdoc>
    /// UserControl for choosing a method for an ObjectDataSource. 
    /// </devdoc>
    internal sealed class ObjectDataSourceMethodEditor : UserControl { 
 
        private static readonly object EventMethodChanged = new object();
 
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.Label _methodLabel;
        private AutoSizeComboBox _methodComboBox;
        private System.Windows.Forms.Label _signatureLabel; 
        private System.Windows.Forms.TextBox _signatureTextBox;
 
 
        /// <devdoc>
        /// Creates a new ObjectDataSourceMethodEditor. 
        /// </devdoc>
        public ObjectDataSourceMethodEditor() {
            InitializeComponent();
            InitializeUI(); 
        }
 
 
        /// <devdoc>
        /// The MethodInfo of the selected method, if any. 
        /// </devdoc>
        public MethodInfo MethodInfo {
            get {
                MethodItem item = _methodComboBox.SelectedItem as MethodItem; 
                if (item == null) {
                    return null; 
                } 
                return item.MethodInfo;
            } 
        }

        /// <devdoc>
        /// The type of the DataObject this method requires, if any. For a 
        /// DataObject to be needed, the method must take exactly one parameter
        /// that is a non-primitive (e.g. a Customer object, but not a string). 
        /// </devdoc> 
        public Type DataObjectType {
            get { 
                MethodInfo mi = MethodInfo;
                if (mi == null) {
                    return null;
                } 
                ParameterInfo[] parameters = mi.GetParameters();
                if (parameters.Length != 1) { 
                    return null; 
                }
                Type paramType = parameters[0].ParameterType; 
                if (IsPrimitiveType(paramType)) {
                    return null;
                }
                return paramType; 
            }
        } 
 
        /// <devdoc>
        /// Notifies listeners that the selected method has changed. 
        /// This is used by the method chooser panel to update the UI to
        /// reflect a newly chosen Select method.
        /// </devdoc>
        public event EventHandler MethodChanged { 
            add {
                Events.AddHandler(EventMethodChanged, value); 
            } 
            remove {
                Events.RemoveHandler(EventMethodChanged, value); 
            }
        }

        /// <devdoc> 
        /// Used by GetMethodSignature to pretty-format method signatures.
        /// </devdoc> 
        private static void AppendGenericArguments(Type[] args, StringBuilder sb) { 
            if (args.Length > 0) {
                sb.Append("<"); 
                for (int i = 0; i < args.Length; i++) {
                    AppendTypeName(args[i], false, sb);
                    if (i + 1 < args.Length) {
                        sb.Append(", "); 
                    }
                } 
                sb.Append(">"); 
            }
        } 

        /// <devdoc>
        /// Used by GetMethodSignature to pretty-format method signatures.
        /// </devdoc> 
        internal static void AppendTypeName(Type t, bool topLevelFullName, StringBuilder sb) {
            string name = (topLevelFullName ? t.FullName : t.Name); 
            if (t.IsGenericType) { 
                // Generic types, e.g. List<T, List<int>>[,,][]
                int indexOfTick = name.IndexOf("`", StringComparison.Ordinal); 
                if (indexOfTick == -1) {
                    // There are some bizarre inconsistencies in how the CLR shows type names, and
                    // for some generic types such as the type List<T>+Enumerator<T> you get back
                    // names such as List`1+Enumerator[T], with t.Name being just "Enumerator" 
                    // (without the backtick!).
                    indexOfTick = name.Length; 
                } 
                // Get base outer type name, e.g. List
                sb.Append(name.Substring(0, indexOfTick)); 

                // Get generic arguments, e.g. <T, List<int>>
                AppendGenericArguments(t.GetGenericArguments(), sb);
 
                if (indexOfTick < name.Length) {
                    // Get suffix, e.g. for an array [,,][] 
                    indexOfTick++; 
                    while ((indexOfTick < name.Length) && Char.IsNumber(name, indexOfTick)) {
                        indexOfTick++; 
                    }
                    sb.Append(name.Substring(indexOfTick));
                }
            } 
            else {
                // Non-generic types, e.g. string[][,,] 
                sb.Append(name); 
            }
        } 

        /// <devdoc>
        /// Determines whether a given method is applicable to the specified
        /// data source operation. Returns true if it is. 
        /// </devdoc>
        private bool FilterMethod(MethodInfo methodInfo, DataObjectMethodType methodType) { 
            if (methodType == DataObjectMethodType.Select) { 
                // The requirements for select methods are special since we
                // require certain return types 
                if (methodInfo.ReturnType == typeof(void)) {
                    // Must return a non-void type
                    return false;
                } 
            }
            else { 
                // The requirements for insert/update/delete methods are all the same 
                ParameterInfo[] parameters = methodInfo.GetParameters();
                Debug.Assert(parameters != null, "MethodInfo.GetParameters should not return null"); 
                if ((parameters == null) || (parameters.Length == 0)) {
                    // Must take at least one parameter
                    return false;
                } 
            }
 
            return true; 
        }
 
        /// <devdoc>
        /// Gets a string representing the signature of a method. This takes into
        /// account generic method arguments as well as generic types for the parameters
        /// and return values. 
        /// </devdoc>
        internal static string GetMethodSignature(MethodInfo mi) { 
            if (mi == null) { 
                return String.Empty;
            } 
            StringBuilder methodBuilder = new StringBuilder(128);
            methodBuilder.Append(mi.Name);
            AppendGenericArguments(mi.GetGenericArguments(), methodBuilder);
            methodBuilder.Append("("); 
            ParameterInfo[] parameters = mi.GetParameters();
            foreach (ParameterInfo pi in parameters) { 
                AppendTypeName(pi.ParameterType, false, methodBuilder); 
                methodBuilder.Append(" " + pi.Name);
                if (pi.Position + 1 < parameters.Length) { 
                    methodBuilder.Append(", ");
                }
            }
            methodBuilder.Append(")"); 
            if (mi.ReturnType != typeof(void)) {
                StringBuilder returnTypeBuilder = new StringBuilder(); 
                AppendTypeName(mi.ReturnType, false, returnTypeBuilder); 
                return SR.GetString(SR.ObjectDataSourceMethodEditor_SignatureFormat, methodBuilder, returnTypeBuilder);
            } 
            else {
                return methodBuilder.ToString();
            }
        } 

        #region Designer generated code 
        private void InitializeComponent() { 
            this._helpLabel = new System.Windows.Forms.Label();
            this._methodLabel = new System.Windows.Forms.Label(); 
            this._signatureLabel = new System.Windows.Forms.Label();
            this._methodComboBox = new AutoSizeComboBox();
            this._signatureTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout(); 
            //
            // _helpLabel 
            // 
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._helpLabel.Location = new System.Drawing.Point(12, 12);
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(487, 80);
            this._helpLabel.TabIndex = 10; 
            //
            // _methodLabel 
            // 
            this._methodLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._methodLabel.Location = new System.Drawing.Point(12, 98);
            this._methodLabel.Name = "_methodLabel";
            this._methodLabel.Size = new System.Drawing.Size(487, 16);
            this._methodLabel.TabIndex = 20; 
            //
            // _methodComboBox 
            // 
            this._methodComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._methodComboBox.Location = new System.Drawing.Point(12, 116); 
            this._methodComboBox.Name = "_methodComboBox";
            this._methodComboBox.Size = new System.Drawing.Size(300, 21);
            this._methodComboBox.Sorted = true;
            this._methodComboBox.TabIndex = 30; 
            this._methodComboBox.SelectedIndexChanged += new System.EventHandler(this.OnMethodComboBoxSelectedIndexChanged);
            // 
            // _signatureLabel 
            //
            this._signatureLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._signatureLabel.Location = new System.Drawing.Point(12, 145);
            this._signatureLabel.Name = "_signatureLabel";
            this._signatureLabel.Size = new System.Drawing.Size(487, 16); 
            this._signatureLabel.TabIndex = 40;
            // 
            // _signatureTextBox 
            //
            this._signatureTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._signatureTextBox.BackColor = System.Drawing.SystemColors.Control;
            this._signatureTextBox.Location = new System.Drawing.Point(12, 163);
            this._signatureTextBox.Multiline = true; 
            this._signatureTextBox.Name = "_signatureTextBox";
            this._signatureTextBox.ReadOnly = true; 
            this._signatureTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; 
            this._signatureTextBox.Size = new System.Drawing.Size(487, 48);
            this._signatureTextBox.TabIndex = 50; 
            this._signatureTextBox.Text = "";
            //
            // ObjectDataSourceMethodEditor
            // 
            this.Controls.Add(this._signatureTextBox);
            this.Controls.Add(this._methodComboBox); 
            this.Controls.Add(this._signatureLabel); 
            this.Controls.Add(this._methodLabel);
            this.Controls.Add(this._helpLabel); 
            this.Name = "ObjectDataSourceMethodEditor";
            this.Size = new System.Drawing.Size(511, 220);
            this.ResumeLayout(false);
        } 
        #endregion
 
        /// <devdoc> 
        /// Called after InitializeComponent to perform additional actions that
        /// are not supported by the designer. 
        /// </devdoc>
        private void InitializeUI() {
            _methodLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_MethodLabel);
            _signatureLabel.Text = SR.GetString(SR.ObjectDataSource_General_MethodSignatureLabel); 
        }
 
        private static bool IsPrimitiveType(Type t) { 
            Debug.Assert(t != null);
            Type underlyingType = Nullable.GetUnderlyingType(t); 
            if (underlyingType != null) {
                t = underlyingType;
            }
            return t.IsPrimitive || (t == typeof(string)) || (t == typeof(DateTime)) || (t == typeof(decimal)) || (t == typeof(object)); 
        }
 
        private void OnMethodChanged(EventArgs e) { 
            EventHandler handler = Events[EventMethodChanged] as EventHandler;
            if (handler != null) { 
                handler(this, e);
            }
        }
 
        private void OnMethodComboBoxSelectedIndexChanged(object sender, System.EventArgs e) {
            OnMethodChanged(EventArgs.Empty); 
 
            _signatureTextBox.Text = GetMethodSignature(MethodInfo);
        } 

        public void SetMethodInformation(MethodInfo[] methods, string selectedMethodName, ParameterCollection selectedParameters, DataObjectMethodType methodType, Type dataObjectType) {
            // Populate method list and auto-select current method
            try { 
                _signatureTextBox.Text = String.Empty;
                switch (methodType) { 
                    case DataObjectMethodType.Delete: 
                        _helpLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_DeleteHelpLabel);
                        break; 
                    case DataObjectMethodType.Insert:
                        _helpLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_InsertHelpLabel);
                        break;
                    case DataObjectMethodType.Select: 
                        _helpLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_SelectHelpLabel);
                        break; 
                    case DataObjectMethodType.Update: 
                        _helpLabel.Text = SR.GetString(SR.ObjectDataSourceMethodEditor_UpdateHelpLabel);
                        break; 
                    default:
                        Debug.Fail("Unexpected DataObjectMethodType: " + methodType);
                        break;
                } 
                _methodComboBox.BeginUpdate();
                _methodComboBox.Items.Clear(); 
                MethodItem selectedItem = null; 
                bool filteredByAttribute = false;
 
                foreach (MethodInfo methodInfo in methods) {
                    if (!FilterMethod(methodInfo, methodType)) {
                        continue;
                    } 
                    // Check if the method is decorated with the DataObjectMethodAttribute
 
                    // Indicates whether this method should be added to the combobox 
                    bool addMethod = false;
 
                    DataObjectMethodAttribute attr = Attribute.GetCustomAttribute(methodInfo, typeof(DataObjectMethodAttribute), true) as DataObjectMethodAttribute;
                    if ((attr != null) && (attr.MethodType == methodType)) {
                        if (!filteredByAttribute) {
                            // If this is the first time we see a decorated method of the right type, 
                            // clear out all the existing methods since they were all not filtered.
                            _methodComboBox.Items.Clear(); 
                        } 
                        filteredByAttribute = true;
                        addMethod = true; 
                    }
                    else {
                        // Method is not decorated, only add it if we are not filtered
                        if (!filteredByAttribute) { 
                            addMethod = true;
                        } 
                    } 

                    // Add this method to the list, and possibly auto-select it if it matches the signature 
                    bool isMatch = ObjectDataSourceDesigner.IsMatchingMethod(methodInfo, selectedMethodName, selectedParameters, dataObjectType);
                    if (addMethod || isMatch) {
                        MethodItem item = new MethodItem(methodInfo);
                        _methodComboBox.Items.Add(item); 

                        if (isMatch) { 
                            // If it's a solid match, auto-select it 
                            selectedItem = item;
                        } 
                        else {
                            // If it's not a solid match, but there is no method set for this
                            // operation, and it is the default method, auto-select it
                            if ((attr != null) && (attr.MethodType == methodType) && attr.IsDefault && (selectedMethodName.Length == 0)) { 
                                selectedItem = item;
                            } 
                        } 
                    }
                } 

                // Add an empty item so that the user can unselect a method
                // This has to be added as the last step since otherwise it might get cleared out by the method filter
                if (methodType != DataObjectMethodType.Select) { 
                    _methodComboBox.Items.Insert(0, new MethodItem(null));
                } 
                _methodComboBox.InvalidateDropDownWidth(); 

                _methodComboBox.SelectedItem = selectedItem; 
                //
            }
            finally {
                _methodComboBox.EndUpdate(); 
            }
        } 
 

        /// <devdoc> 
        /// Represents a method a user can select.
        /// </devdoc>
        private sealed class MethodItem {
            private MethodInfo _methodInfo; 

            public MethodItem(MethodInfo methodInfo) { 
                _methodInfo = methodInfo; 
            }
 
            public MethodInfo MethodInfo {
                get {
                    return _methodInfo;
                } 
            }
 
            public override string ToString() { 
                if (_methodInfo == null) {
                    return SR.GetString(SR.ObjectDataSourceMethodEditor_NoMethod); 
                }
                else {
                    return GetMethodSignature(_methodInfo);
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
