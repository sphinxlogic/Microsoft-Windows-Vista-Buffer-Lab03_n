//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceChooseMethodsPanel.cs" company="Microsoft">
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
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <devdoc>
    /// Wizard panel for choosing methods for an ObjectDataSource.
    /// </devdoc> 
    internal sealed class ObjectDataSourceChooseMethodsPanel : WizardPanel {
        private System.Windows.Forms.TabControl _methodsTabControl; 
        private System.Windows.Forms.TabPage _selectTabPage; 
        private System.Windows.Forms.TabPage _updateTabPage;
        private System.Windows.Forms.TabPage _insertTabPage; 
        private System.Windows.Forms.TabPage _deleteTabPage;
        private ObjectDataSourceMethodEditor _updateObjectDataSourceMethodEditor;
        private ObjectDataSourceMethodEditor _selectObjectDataSourceMethodEditor;
        private ObjectDataSourceMethodEditor _insertObjectDataSourceMethodEditor; 
        private ObjectDataSourceMethodEditor _deleteObjectDataSourceMethodEditor;
 
        private ObjectDataSource _objectDataSource; 
        private ObjectDataSourceDesigner _objectDataSourceDesigner;
 

        /// <devdoc>
        /// Creates a new ObjectDataSourceChooseMethodsPanel.
        /// </devdoc> 
        public ObjectDataSourceChooseMethodsPanel(ObjectDataSourceDesigner objectDataSourceDesigner) {
            Debug.Assert(objectDataSourceDesigner != null); 
            _objectDataSourceDesigner = objectDataSourceDesigner; 
            InitializeComponent();
            InitializeUI(); 

            _objectDataSource = (ObjectDataSource)_objectDataSourceDesigner.Component;
        }
 
        private Type DeleteMethodDataObjectType {
            get { 
                return _deleteObjectDataSourceMethodEditor.DataObjectType; 
            }
        } 

        private MethodInfo DeleteMethodInfo {
            get {
                return _deleteObjectDataSourceMethodEditor.MethodInfo; 
            }
        } 
 
        private Type InsertMethodDataObjectType {
            get { 
                return _insertObjectDataSourceMethodEditor.DataObjectType;
            }
        }
 
        private MethodInfo InsertMethodInfo {
            get { 
                return _insertObjectDataSourceMethodEditor.MethodInfo; 
            }
        } 

        private MethodInfo SelectMethodInfo {
            get {
                return _selectObjectDataSourceMethodEditor.MethodInfo; 
            }
        } 
 
        private Type UpdateMethodDataObjectType {
            get { 
                return _updateObjectDataSourceMethodEditor.DataObjectType;
            }
        }
 
        private MethodInfo UpdateMethodInfo {
            get { 
                return _updateObjectDataSourceMethodEditor.MethodInfo; 
            }
        } 


        #region Designer generated code
        private void InitializeComponent() { 
            this._methodsTabControl = new System.Windows.Forms.TabControl();
            this._selectTabPage = new System.Windows.Forms.TabPage(); 
            this._selectObjectDataSourceMethodEditor = new ObjectDataSourceMethodEditor(); 
            this._updateTabPage = new System.Windows.Forms.TabPage();
            this._updateObjectDataSourceMethodEditor = new ObjectDataSourceMethodEditor(); 
            this._insertTabPage = new System.Windows.Forms.TabPage();
            this._insertObjectDataSourceMethodEditor = new ObjectDataSourceMethodEditor();
            this._deleteTabPage = new System.Windows.Forms.TabPage();
            this._deleteObjectDataSourceMethodEditor = new ObjectDataSourceMethodEditor(); 
            this._methodsTabControl.SuspendLayout();
            this._selectTabPage.SuspendLayout(); 
            this._updateTabPage.SuspendLayout(); 
            this._insertTabPage.SuspendLayout();
            this._deleteTabPage.SuspendLayout(); 
            this.SuspendLayout();
            //
            // _methodsTabControl
            // 
            this._methodsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._methodsTabControl.Controls.Add(this._selectTabPage);
            this._methodsTabControl.Controls.Add(this._updateTabPage); 
            this._methodsTabControl.Controls.Add(this._insertTabPage);
            this._methodsTabControl.Controls.Add(this._deleteTabPage);
            this._methodsTabControl.Location = new System.Drawing.Point(0, 0);
            this._methodsTabControl.Name = "_methodsTabControl"; 
            this._methodsTabControl.SelectedIndex = 0;
            this._methodsTabControl.ShowToolTips = true; 
            this._methodsTabControl.Size = new System.Drawing.Size(544, 274); 
            this._methodsTabControl.TabIndex = 0;
            // 
            // _selectTabPage
            //
            this._selectTabPage.Controls.Add(this._selectObjectDataSourceMethodEditor);
            this._selectTabPage.Location = new System.Drawing.Point(4, 22); 
            this._selectTabPage.Name = "_selectTabPage";
            this._selectTabPage.Size = new System.Drawing.Size(536, 248); 
            this._selectTabPage.TabIndex = 10; 
            this._selectTabPage.Text = "SELECT";
            // 
            // _selectObjectDataSourceMethodEditor
            //
            this._selectObjectDataSourceMethodEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._selectObjectDataSourceMethodEditor.Location = new System.Drawing.Point(0, 0); 
            this._selectObjectDataSourceMethodEditor.Name = "_selectObjectDataSourceMethodEditor";
            this._selectObjectDataSourceMethodEditor.TabIndex = 0; 
            this._selectObjectDataSourceMethodEditor.MethodChanged += new System.EventHandler(this.OnSelectMethodChanged); 
            //
            // _updateTabPage 
            //
            this._updateTabPage.Controls.Add(this._updateObjectDataSourceMethodEditor);
            this._updateTabPage.Location = new System.Drawing.Point(4, 22);
            this._updateTabPage.Name = "_updateTabPage"; 
            this._updateTabPage.Size = new System.Drawing.Size(536, 248);
            this._updateTabPage.TabIndex = 20; 
            this._updateTabPage.Text = "UPDATE"; 
            //
            // _updateObjectDataSourceMethodEditor 
            //
            this._updateObjectDataSourceMethodEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._updateObjectDataSourceMethodEditor.Location = new System.Drawing.Point(0, 0);
            this._updateObjectDataSourceMethodEditor.Name = "_updateObjectDataSourceMethodEditor"; 
            this._updateObjectDataSourceMethodEditor.TabIndex = 0;
            // 
            // _insertTabPage 
            //
            this._insertTabPage.Controls.Add(this._insertObjectDataSourceMethodEditor); 
            this._insertTabPage.Location = new System.Drawing.Point(4, 22);
            this._insertTabPage.Name = "_insertTabPage";
            this._insertTabPage.Size = new System.Drawing.Size(536, 248);
            this._insertTabPage.TabIndex = 30; 
            this._insertTabPage.Text = "INSERT";
            // 
            // _insertObjectDataSourceMethodEditor 
            //
            this._insertObjectDataSourceMethodEditor.Dock = System.Windows.Forms.DockStyle.Fill; 
            this._insertObjectDataSourceMethodEditor.Location = new System.Drawing.Point(0, 0);
            this._insertObjectDataSourceMethodEditor.Name = "_insertObjectDataSourceMethodEditor";
            this._insertObjectDataSourceMethodEditor.TabIndex = 0;
            // 
            // _deleteTabPage
            // 
            this._deleteTabPage.Controls.Add(this._deleteObjectDataSourceMethodEditor); 
            this._deleteTabPage.Location = new System.Drawing.Point(4, 22);
            this._deleteTabPage.Name = "_deleteTabPage"; 
            this._deleteTabPage.Size = new System.Drawing.Size(536, 248);
            this._deleteTabPage.TabIndex = 40;
            this._deleteTabPage.Text = "DELETE";
            // 
            // _deleteObjectDataSourceMethodEditor
            // 
            this._deleteObjectDataSourceMethodEditor.Dock = System.Windows.Forms.DockStyle.Fill; 
            this._deleteObjectDataSourceMethodEditor.Location = new System.Drawing.Point(0, 0);
            this._deleteObjectDataSourceMethodEditor.Name = "_deleteObjectDataSourceMethodEditor"; 
            this._deleteObjectDataSourceMethodEditor.TabIndex = 0;
            //
            // ObjectDataSourceChooseMethodsPanel
            // 
            this.Controls.Add(this._methodsTabControl);
            this.Name = "ObjectDataSourceChooseMethodsPanel"; 
            this.Size = new System.Drawing.Size(544, 274); 
            this._methodsTabControl.ResumeLayout(false);
            this._selectTabPage.ResumeLayout(false); 
            this._updateTabPage.ResumeLayout(false);
            this._insertTabPage.ResumeLayout(false);
            this._deleteTabPage.ResumeLayout(false);
            this.ResumeLayout(false); 
        }
        #endregion 
 
        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that 
        /// are not supported by the designer.
        /// </devdoc>
        private void InitializeUI() {
            Caption = SR.GetString(SR.ObjectDataSourceChooseMethodsPanel_PanelCaption); 
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
            string methodName;
            MethodInfo methodInfo; 

            methodInfo = DeleteMethodInfo;
            methodName = (methodInfo == null ? String.Empty : methodInfo.Name);
            if (_objectDataSource.DeleteMethod != methodName) { 
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["DeleteMethod"];
                propDesc.ResetValue(_objectDataSource); 
                propDesc.SetValue(_objectDataSource, methodName); 
            }
 
            methodInfo = InsertMethodInfo;
            methodName = (methodInfo == null ? String.Empty : methodInfo.Name);
            if (_objectDataSource.InsertMethod != methodName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["InsertMethod"]; 
                propDesc.ResetValue(_objectDataSource);
                propDesc.SetValue(_objectDataSource, methodName); 
            } 

            methodInfo = SelectMethodInfo; 
            Debug.Assert(methodInfo != null, "SelectMethodInfo should not be null in OnComplete");
            methodName = (methodInfo == null ? String.Empty : methodInfo.Name);
            if (_objectDataSource.SelectMethod != methodName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["SelectMethod"]; 
                propDesc.ResetValue(_objectDataSource);
                propDesc.SetValue(_objectDataSource, methodName); 
            } 

            methodInfo = UpdateMethodInfo; 
            methodName = (methodInfo == null ? String.Empty : methodInfo.Name);
            if (_objectDataSource.UpdateMethod != methodName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["UpdateMethod"];
                propDesc.ResetValue(_objectDataSource); 
                propDesc.SetValue(_objectDataSource, methodName);
            } 
 

            // We clear out all the SELECT parameters because if there are any, 
            // we give the user an opportunity to configure them in the next
            // panel. The UPDATE/INSERT/DELETE parameters are auto-merged.
            _objectDataSource.SelectParameters.Clear();
 
            methodInfo = SelectMethodInfo;
 
            // Get a list of the fields so we can filter out parameters 
            // that already include the original_ prefix. This happens with
            // DataComponents generated by VS. 
            IDataSourceFieldSchema[] fieldSchemas = null;
            try {
                IDataSourceSchema schema = new TypeSchema(methodInfo.ReturnType);
                if (schema != null) { 
                    IDataSourceViewSchema[] viewSchemas = schema.GetViews();
                    if (viewSchemas != null && viewSchemas.Length > 0) { 
                        fieldSchemas = viewSchemas[0].GetFields(); 
                    }
                } 
            }
            catch (Exception ex) {
                Debug.Fail("Failed to get schema:\r\n" + ex.ToString());
            } 

            ObjectDataSourceDesigner.MergeParameters(_objectDataSource.DeleteParameters, DeleteMethodInfo, DeleteMethodDataObjectType); 
            ObjectDataSourceDesigner.MergeParameters(_objectDataSource.InsertParameters, InsertMethodInfo, InsertMethodDataObjectType); 
            ObjectDataSourceDesigner.MergeParameters(_objectDataSource.UpdateParameters, UpdateMethodInfo, UpdateMethodDataObjectType);
 

            // Set the DataObjectTypeName property if it is required
            string dataObjectTypeName = String.Empty;
            if (DeleteMethodDataObjectType != null) { 
                dataObjectTypeName = DeleteMethodDataObjectType.FullName;
            } 
            else { 
                if (InsertMethodDataObjectType != null) {
                    dataObjectTypeName = InsertMethodDataObjectType.FullName; 
                }
                else {
                    if (UpdateMethodDataObjectType != null) {
                        dataObjectTypeName = UpdateMethodDataObjectType.FullName; 
                    }
                } 
            } 
            if (_objectDataSource.DataObjectTypeName != dataObjectTypeName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["DataObjectTypeName"]; 
                propDesc.ResetValue(_objectDataSource);
                propDesc.SetValue(_objectDataSource, dataObjectTypeName);
            }
 

            // Retrieve schema 
            if (methodInfo != null) { 
                _objectDataSourceDesigner.RefreshSchema(methodInfo.ReflectedType, methodInfo.Name, methodInfo.ReturnType, true);
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        public override bool OnNext() {
            // If more than one method requires a DataObject, but they require 
            // different types of DataObjects, we can't continue since the 
            // runtime can't support this.
            System.Collections.Generic.List<Type> dataObjectTypes = new System.Collections.Generic.List<Type>(); 
            Type deleteObjectType = DeleteMethodDataObjectType;
            if (deleteObjectType != null) {
                dataObjectTypes.Add(deleteObjectType);
            } 
            Type insertObjectType = InsertMethodDataObjectType;
            if (insertObjectType != null) { 
                dataObjectTypes.Add(insertObjectType); 
            }
            Type updateObjectType = UpdateMethodDataObjectType; 
            if (updateObjectType != null) {
                dataObjectTypes.Add(updateObjectType);
            }
 
            // DataObject types have been found, make sure they are all the same
            if (dataObjectTypes.Count > 1) { 
                // Compare item #0 to items #1..#N 
                Type dataObjectType = dataObjectTypes[0];
                for (int i = 1; i < dataObjectTypes.Count; i++) { 
                    if (dataObjectType != dataObjectTypes[i]) {
                        UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.ObjectDataSourceChooseMethodsPanel_IncompatibleDataObjectTypes));
                        return false;
                    } 
                }
            } 
 

            MethodInfo methodInfo = SelectMethodInfo; 
            if (methodInfo == null) {
                Debug.Fail("Next button should have been disabled if a select method was not specified");
                return false;
            } 
            // Select method is specified, determine if it has any parameters
            ParameterInfo[] parameters = methodInfo.GetParameters(); 
            bool hasParams = (parameters.Length > 0); 
            if (!hasParams) {
                // Select method has no parameters, wizard can complete 
                return true;
            }

            // Select method has parameters, proceed to parameters wizard panel 

            ObjectDataSourceConfigureParametersPanel parametersPanel = NextPanel as ObjectDataSourceConfigureParametersPanel; 
            if (parametersPanel == null) { 
                // If the panel does not yet exist, create it and initialize it with the current settings
                parametersPanel = ((ObjectDataSourceWizardForm)ParentWizard).GetParametersPanel(); 
                NextPanel = parametersPanel;
                parametersPanel.InitializeParameters(_objectDataSource.SelectParameters);
            }
            // 
            parametersPanel.SetMethod(SelectMethodInfo);
            return true; 
        } 

        /// <devdoc> 
        /// </devdoc>
        public override void OnPrevious() {
        }
 
        private void OnSelectMethodChanged(object sender, EventArgs e) {
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
 
        private static MethodInfo[] GetMethods(Type type) {
            MethodInfo[] methods = type.GetMethods(ObjectDataSourceDesigner.MethodFilter);

            System.Collections.Generic.List<MethodInfo> filteredMethods = new System.Collections.Generic.List<MethodInfo>(); 
            foreach (MethodInfo method in methods) {
                // Ignore methods declared on System.Object 
                // Ignore get_ and set_ etc. methods 
                // Ignore abstract methods
                if ((method.GetBaseDefinition().DeclaringType != typeof(object)) & 
                    (!method.IsSpecialName) &&
                    (!method.IsAbstract)) {
                    filteredMethods.Add(method);
                } 
            }
 
            return filteredMethods.ToArray(); 
        }
 
        public void SetType(Type type) {
            // Initialize UI with new type
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor;
 
                MethodInfo[] methods = GetMethods(type); 

                _methodsTabControl.SelectedIndex = 0; 

                Type dataObjectType = ObjectDataSourceDesigner.GetType(ServiceProvider, _objectDataSource.DataObjectTypeName, true);

                _selectObjectDataSourceMethodEditor.SetMethodInformation(methods, _objectDataSource.SelectMethod, _objectDataSource.SelectParameters, DataObjectMethodType.Select, dataObjectType); 
                _insertObjectDataSourceMethodEditor.SetMethodInformation(methods, _objectDataSource.InsertMethod, _objectDataSource.InsertParameters, DataObjectMethodType.Insert, dataObjectType);
                _updateObjectDataSourceMethodEditor.SetMethodInformation(methods, _objectDataSource.UpdateMethod, _objectDataSource.UpdateParameters, DataObjectMethodType.Update, dataObjectType); 
                _deleteObjectDataSourceMethodEditor.SetMethodInformation(methods, _objectDataSource.DeleteMethod, _objectDataSource.DeleteParameters, DataObjectMethodType.Delete, dataObjectType); 
            }
            finally { 
                Cursor.Current = originalCursor;
            }

            UpdateEnabledState(); 
        }
 
        private void UpdateEnabledState() { 
            Debug.Assert(ParentWizard != null, "Panel must be parented to update UI state");
            MethodInfo methodInfo = SelectMethodInfo; 
            if (methodInfo != null) {
                // Select method is specified, determine if it has any parameters
                ParameterInfo[] parameters = methodInfo.GetParameters();
                bool hasParams = (parameters.Length > 0); 
                ParentWizard.NextButton.Enabled = hasParams;
                ParentWizard.FinishButton.Enabled = !hasParams; 
            } 
            else {
                // No Select method is specified 
                ParentWizard.NextButton.Enabled = false;
                ParentWizard.FinishButton.Enabled = false;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceChooseMethodsPanel.cs" company="Microsoft">
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
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <devdoc>
    /// Wizard panel for choosing methods for an ObjectDataSource.
    /// </devdoc> 
    internal sealed class ObjectDataSourceChooseMethodsPanel : WizardPanel {
        private System.Windows.Forms.TabControl _methodsTabControl; 
        private System.Windows.Forms.TabPage _selectTabPage; 
        private System.Windows.Forms.TabPage _updateTabPage;
        private System.Windows.Forms.TabPage _insertTabPage; 
        private System.Windows.Forms.TabPage _deleteTabPage;
        private ObjectDataSourceMethodEditor _updateObjectDataSourceMethodEditor;
        private ObjectDataSourceMethodEditor _selectObjectDataSourceMethodEditor;
        private ObjectDataSourceMethodEditor _insertObjectDataSourceMethodEditor; 
        private ObjectDataSourceMethodEditor _deleteObjectDataSourceMethodEditor;
 
        private ObjectDataSource _objectDataSource; 
        private ObjectDataSourceDesigner _objectDataSourceDesigner;
 

        /// <devdoc>
        /// Creates a new ObjectDataSourceChooseMethodsPanel.
        /// </devdoc> 
        public ObjectDataSourceChooseMethodsPanel(ObjectDataSourceDesigner objectDataSourceDesigner) {
            Debug.Assert(objectDataSourceDesigner != null); 
            _objectDataSourceDesigner = objectDataSourceDesigner; 
            InitializeComponent();
            InitializeUI(); 

            _objectDataSource = (ObjectDataSource)_objectDataSourceDesigner.Component;
        }
 
        private Type DeleteMethodDataObjectType {
            get { 
                return _deleteObjectDataSourceMethodEditor.DataObjectType; 
            }
        } 

        private MethodInfo DeleteMethodInfo {
            get {
                return _deleteObjectDataSourceMethodEditor.MethodInfo; 
            }
        } 
 
        private Type InsertMethodDataObjectType {
            get { 
                return _insertObjectDataSourceMethodEditor.DataObjectType;
            }
        }
 
        private MethodInfo InsertMethodInfo {
            get { 
                return _insertObjectDataSourceMethodEditor.MethodInfo; 
            }
        } 

        private MethodInfo SelectMethodInfo {
            get {
                return _selectObjectDataSourceMethodEditor.MethodInfo; 
            }
        } 
 
        private Type UpdateMethodDataObjectType {
            get { 
                return _updateObjectDataSourceMethodEditor.DataObjectType;
            }
        }
 
        private MethodInfo UpdateMethodInfo {
            get { 
                return _updateObjectDataSourceMethodEditor.MethodInfo; 
            }
        } 


        #region Designer generated code
        private void InitializeComponent() { 
            this._methodsTabControl = new System.Windows.Forms.TabControl();
            this._selectTabPage = new System.Windows.Forms.TabPage(); 
            this._selectObjectDataSourceMethodEditor = new ObjectDataSourceMethodEditor(); 
            this._updateTabPage = new System.Windows.Forms.TabPage();
            this._updateObjectDataSourceMethodEditor = new ObjectDataSourceMethodEditor(); 
            this._insertTabPage = new System.Windows.Forms.TabPage();
            this._insertObjectDataSourceMethodEditor = new ObjectDataSourceMethodEditor();
            this._deleteTabPage = new System.Windows.Forms.TabPage();
            this._deleteObjectDataSourceMethodEditor = new ObjectDataSourceMethodEditor(); 
            this._methodsTabControl.SuspendLayout();
            this._selectTabPage.SuspendLayout(); 
            this._updateTabPage.SuspendLayout(); 
            this._insertTabPage.SuspendLayout();
            this._deleteTabPage.SuspendLayout(); 
            this.SuspendLayout();
            //
            // _methodsTabControl
            // 
            this._methodsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left) 
                        | System.Windows.Forms.AnchorStyles.Right))); 
            this._methodsTabControl.Controls.Add(this._selectTabPage);
            this._methodsTabControl.Controls.Add(this._updateTabPage); 
            this._methodsTabControl.Controls.Add(this._insertTabPage);
            this._methodsTabControl.Controls.Add(this._deleteTabPage);
            this._methodsTabControl.Location = new System.Drawing.Point(0, 0);
            this._methodsTabControl.Name = "_methodsTabControl"; 
            this._methodsTabControl.SelectedIndex = 0;
            this._methodsTabControl.ShowToolTips = true; 
            this._methodsTabControl.Size = new System.Drawing.Size(544, 274); 
            this._methodsTabControl.TabIndex = 0;
            // 
            // _selectTabPage
            //
            this._selectTabPage.Controls.Add(this._selectObjectDataSourceMethodEditor);
            this._selectTabPage.Location = new System.Drawing.Point(4, 22); 
            this._selectTabPage.Name = "_selectTabPage";
            this._selectTabPage.Size = new System.Drawing.Size(536, 248); 
            this._selectTabPage.TabIndex = 10; 
            this._selectTabPage.Text = "SELECT";
            // 
            // _selectObjectDataSourceMethodEditor
            //
            this._selectObjectDataSourceMethodEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._selectObjectDataSourceMethodEditor.Location = new System.Drawing.Point(0, 0); 
            this._selectObjectDataSourceMethodEditor.Name = "_selectObjectDataSourceMethodEditor";
            this._selectObjectDataSourceMethodEditor.TabIndex = 0; 
            this._selectObjectDataSourceMethodEditor.MethodChanged += new System.EventHandler(this.OnSelectMethodChanged); 
            //
            // _updateTabPage 
            //
            this._updateTabPage.Controls.Add(this._updateObjectDataSourceMethodEditor);
            this._updateTabPage.Location = new System.Drawing.Point(4, 22);
            this._updateTabPage.Name = "_updateTabPage"; 
            this._updateTabPage.Size = new System.Drawing.Size(536, 248);
            this._updateTabPage.TabIndex = 20; 
            this._updateTabPage.Text = "UPDATE"; 
            //
            // _updateObjectDataSourceMethodEditor 
            //
            this._updateObjectDataSourceMethodEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._updateObjectDataSourceMethodEditor.Location = new System.Drawing.Point(0, 0);
            this._updateObjectDataSourceMethodEditor.Name = "_updateObjectDataSourceMethodEditor"; 
            this._updateObjectDataSourceMethodEditor.TabIndex = 0;
            // 
            // _insertTabPage 
            //
            this._insertTabPage.Controls.Add(this._insertObjectDataSourceMethodEditor); 
            this._insertTabPage.Location = new System.Drawing.Point(4, 22);
            this._insertTabPage.Name = "_insertTabPage";
            this._insertTabPage.Size = new System.Drawing.Size(536, 248);
            this._insertTabPage.TabIndex = 30; 
            this._insertTabPage.Text = "INSERT";
            // 
            // _insertObjectDataSourceMethodEditor 
            //
            this._insertObjectDataSourceMethodEditor.Dock = System.Windows.Forms.DockStyle.Fill; 
            this._insertObjectDataSourceMethodEditor.Location = new System.Drawing.Point(0, 0);
            this._insertObjectDataSourceMethodEditor.Name = "_insertObjectDataSourceMethodEditor";
            this._insertObjectDataSourceMethodEditor.TabIndex = 0;
            // 
            // _deleteTabPage
            // 
            this._deleteTabPage.Controls.Add(this._deleteObjectDataSourceMethodEditor); 
            this._deleteTabPage.Location = new System.Drawing.Point(4, 22);
            this._deleteTabPage.Name = "_deleteTabPage"; 
            this._deleteTabPage.Size = new System.Drawing.Size(536, 248);
            this._deleteTabPage.TabIndex = 40;
            this._deleteTabPage.Text = "DELETE";
            // 
            // _deleteObjectDataSourceMethodEditor
            // 
            this._deleteObjectDataSourceMethodEditor.Dock = System.Windows.Forms.DockStyle.Fill; 
            this._deleteObjectDataSourceMethodEditor.Location = new System.Drawing.Point(0, 0);
            this._deleteObjectDataSourceMethodEditor.Name = "_deleteObjectDataSourceMethodEditor"; 
            this._deleteObjectDataSourceMethodEditor.TabIndex = 0;
            //
            // ObjectDataSourceChooseMethodsPanel
            // 
            this.Controls.Add(this._methodsTabControl);
            this.Name = "ObjectDataSourceChooseMethodsPanel"; 
            this.Size = new System.Drawing.Size(544, 274); 
            this._methodsTabControl.ResumeLayout(false);
            this._selectTabPage.ResumeLayout(false); 
            this._updateTabPage.ResumeLayout(false);
            this._insertTabPage.ResumeLayout(false);
            this._deleteTabPage.ResumeLayout(false);
            this.ResumeLayout(false); 
        }
        #endregion 
 
        /// <devdoc>
        /// Called after InitializeComponent to perform additional actions that 
        /// are not supported by the designer.
        /// </devdoc>
        private void InitializeUI() {
            Caption = SR.GetString(SR.ObjectDataSourceChooseMethodsPanel_PanelCaption); 
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
            string methodName;
            MethodInfo methodInfo; 

            methodInfo = DeleteMethodInfo;
            methodName = (methodInfo == null ? String.Empty : methodInfo.Name);
            if (_objectDataSource.DeleteMethod != methodName) { 
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["DeleteMethod"];
                propDesc.ResetValue(_objectDataSource); 
                propDesc.SetValue(_objectDataSource, methodName); 
            }
 
            methodInfo = InsertMethodInfo;
            methodName = (methodInfo == null ? String.Empty : methodInfo.Name);
            if (_objectDataSource.InsertMethod != methodName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["InsertMethod"]; 
                propDesc.ResetValue(_objectDataSource);
                propDesc.SetValue(_objectDataSource, methodName); 
            } 

            methodInfo = SelectMethodInfo; 
            Debug.Assert(methodInfo != null, "SelectMethodInfo should not be null in OnComplete");
            methodName = (methodInfo == null ? String.Empty : methodInfo.Name);
            if (_objectDataSource.SelectMethod != methodName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["SelectMethod"]; 
                propDesc.ResetValue(_objectDataSource);
                propDesc.SetValue(_objectDataSource, methodName); 
            } 

            methodInfo = UpdateMethodInfo; 
            methodName = (methodInfo == null ? String.Empty : methodInfo.Name);
            if (_objectDataSource.UpdateMethod != methodName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["UpdateMethod"];
                propDesc.ResetValue(_objectDataSource); 
                propDesc.SetValue(_objectDataSource, methodName);
            } 
 

            // We clear out all the SELECT parameters because if there are any, 
            // we give the user an opportunity to configure them in the next
            // panel. The UPDATE/INSERT/DELETE parameters are auto-merged.
            _objectDataSource.SelectParameters.Clear();
 
            methodInfo = SelectMethodInfo;
 
            // Get a list of the fields so we can filter out parameters 
            // that already include the original_ prefix. This happens with
            // DataComponents generated by VS. 
            IDataSourceFieldSchema[] fieldSchemas = null;
            try {
                IDataSourceSchema schema = new TypeSchema(methodInfo.ReturnType);
                if (schema != null) { 
                    IDataSourceViewSchema[] viewSchemas = schema.GetViews();
                    if (viewSchemas != null && viewSchemas.Length > 0) { 
                        fieldSchemas = viewSchemas[0].GetFields(); 
                    }
                } 
            }
            catch (Exception ex) {
                Debug.Fail("Failed to get schema:\r\n" + ex.ToString());
            } 

            ObjectDataSourceDesigner.MergeParameters(_objectDataSource.DeleteParameters, DeleteMethodInfo, DeleteMethodDataObjectType); 
            ObjectDataSourceDesigner.MergeParameters(_objectDataSource.InsertParameters, InsertMethodInfo, InsertMethodDataObjectType); 
            ObjectDataSourceDesigner.MergeParameters(_objectDataSource.UpdateParameters, UpdateMethodInfo, UpdateMethodDataObjectType);
 

            // Set the DataObjectTypeName property if it is required
            string dataObjectTypeName = String.Empty;
            if (DeleteMethodDataObjectType != null) { 
                dataObjectTypeName = DeleteMethodDataObjectType.FullName;
            } 
            else { 
                if (InsertMethodDataObjectType != null) {
                    dataObjectTypeName = InsertMethodDataObjectType.FullName; 
                }
                else {
                    if (UpdateMethodDataObjectType != null) {
                        dataObjectTypeName = UpdateMethodDataObjectType.FullName; 
                    }
                } 
            } 
            if (_objectDataSource.DataObjectTypeName != dataObjectTypeName) {
                propDesc = TypeDescriptor.GetProperties(_objectDataSource)["DataObjectTypeName"]; 
                propDesc.ResetValue(_objectDataSource);
                propDesc.SetValue(_objectDataSource, dataObjectTypeName);
            }
 

            // Retrieve schema 
            if (methodInfo != null) { 
                _objectDataSourceDesigner.RefreshSchema(methodInfo.ReflectedType, methodInfo.Name, methodInfo.ReturnType, true);
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        public override bool OnNext() {
            // If more than one method requires a DataObject, but they require 
            // different types of DataObjects, we can't continue since the 
            // runtime can't support this.
            System.Collections.Generic.List<Type> dataObjectTypes = new System.Collections.Generic.List<Type>(); 
            Type deleteObjectType = DeleteMethodDataObjectType;
            if (deleteObjectType != null) {
                dataObjectTypes.Add(deleteObjectType);
            } 
            Type insertObjectType = InsertMethodDataObjectType;
            if (insertObjectType != null) { 
                dataObjectTypes.Add(insertObjectType); 
            }
            Type updateObjectType = UpdateMethodDataObjectType; 
            if (updateObjectType != null) {
                dataObjectTypes.Add(updateObjectType);
            }
 
            // DataObject types have been found, make sure they are all the same
            if (dataObjectTypes.Count > 1) { 
                // Compare item #0 to items #1..#N 
                Type dataObjectType = dataObjectTypes[0];
                for (int i = 1; i < dataObjectTypes.Count; i++) { 
                    if (dataObjectType != dataObjectTypes[i]) {
                        UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.ObjectDataSourceChooseMethodsPanel_IncompatibleDataObjectTypes));
                        return false;
                    } 
                }
            } 
 

            MethodInfo methodInfo = SelectMethodInfo; 
            if (methodInfo == null) {
                Debug.Fail("Next button should have been disabled if a select method was not specified");
                return false;
            } 
            // Select method is specified, determine if it has any parameters
            ParameterInfo[] parameters = methodInfo.GetParameters(); 
            bool hasParams = (parameters.Length > 0); 
            if (!hasParams) {
                // Select method has no parameters, wizard can complete 
                return true;
            }

            // Select method has parameters, proceed to parameters wizard panel 

            ObjectDataSourceConfigureParametersPanel parametersPanel = NextPanel as ObjectDataSourceConfigureParametersPanel; 
            if (parametersPanel == null) { 
                // If the panel does not yet exist, create it and initialize it with the current settings
                parametersPanel = ((ObjectDataSourceWizardForm)ParentWizard).GetParametersPanel(); 
                NextPanel = parametersPanel;
                parametersPanel.InitializeParameters(_objectDataSource.SelectParameters);
            }
            // 
            parametersPanel.SetMethod(SelectMethodInfo);
            return true; 
        } 

        /// <devdoc> 
        /// </devdoc>
        public override void OnPrevious() {
        }
 
        private void OnSelectMethodChanged(object sender, EventArgs e) {
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
 
        private static MethodInfo[] GetMethods(Type type) {
            MethodInfo[] methods = type.GetMethods(ObjectDataSourceDesigner.MethodFilter);

            System.Collections.Generic.List<MethodInfo> filteredMethods = new System.Collections.Generic.List<MethodInfo>(); 
            foreach (MethodInfo method in methods) {
                // Ignore methods declared on System.Object 
                // Ignore get_ and set_ etc. methods 
                // Ignore abstract methods
                if ((method.GetBaseDefinition().DeclaringType != typeof(object)) & 
                    (!method.IsSpecialName) &&
                    (!method.IsAbstract)) {
                    filteredMethods.Add(method);
                } 
            }
 
            return filteredMethods.ToArray(); 
        }
 
        public void SetType(Type type) {
            // Initialize UI with new type
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor;
 
                MethodInfo[] methods = GetMethods(type); 

                _methodsTabControl.SelectedIndex = 0; 

                Type dataObjectType = ObjectDataSourceDesigner.GetType(ServiceProvider, _objectDataSource.DataObjectTypeName, true);

                _selectObjectDataSourceMethodEditor.SetMethodInformation(methods, _objectDataSource.SelectMethod, _objectDataSource.SelectParameters, DataObjectMethodType.Select, dataObjectType); 
                _insertObjectDataSourceMethodEditor.SetMethodInformation(methods, _objectDataSource.InsertMethod, _objectDataSource.InsertParameters, DataObjectMethodType.Insert, dataObjectType);
                _updateObjectDataSourceMethodEditor.SetMethodInformation(methods, _objectDataSource.UpdateMethod, _objectDataSource.UpdateParameters, DataObjectMethodType.Update, dataObjectType); 
                _deleteObjectDataSourceMethodEditor.SetMethodInformation(methods, _objectDataSource.DeleteMethod, _objectDataSource.DeleteParameters, DataObjectMethodType.Delete, dataObjectType); 
            }
            finally { 
                Cursor.Current = originalCursor;
            }

            UpdateEnabledState(); 
        }
 
        private void UpdateEnabledState() { 
            Debug.Assert(ParentWizard != null, "Panel must be parented to update UI state");
            MethodInfo methodInfo = SelectMethodInfo; 
            if (methodInfo != null) {
                // Select method is specified, determine if it has any parameters
                ParameterInfo[] parameters = methodInfo.GetParameters();
                bool hasParams = (parameters.Length > 0); 
                ParentWizard.NextButton.Enabled = hasParams;
                ParentWizard.FinishButton.Enabled = !hasParams; 
            } 
            else {
                // No Select method is specified 
                ParentWizard.NextButton.Enabled = false;
                ParentWizard.FinishButton.Enabled = false;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
