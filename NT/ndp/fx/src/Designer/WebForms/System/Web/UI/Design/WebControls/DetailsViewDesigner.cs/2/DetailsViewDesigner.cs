//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.IO;
    using System.Reflection; 
    using System.Text;
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
 
    using Table = System.Web.UI.WebControls.Table; 
    using TableRow = System.Web.UI.WebControls.TableRow;
 
    /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner"]/*' />
    /// <summary>
    /// DetailsViewDesigner is the designer associated with a
    /// DetailsView. 
    /// </summary>
    public class DetailsViewDesigner : DataBoundControlDesigner { 
 
        private static DesignerAutoFormatCollection _autoFormats;
 
        private static string[] _rowTemplateNames = new string[] {
            "ItemTemplate",
            "AlternatingItemTemplate",
            "EditItemTemplate", 
            "InsertItemTemplate",
            "HeaderTemplate", 
        }; 
        private static bool[] _rowTemplateSupportsDataBinding = new bool[] {
            true, 
            true,
            true,
            true,
            false 
        };
        private const int IDX_ROW_HEADER_TEMPLATE = 4; 
        private const int IDX_ROW_ITEM_TEMPLATE = 0; 
        private const int IDX_ROW_ALTITEM_TEMPLATE = 1;
        private const int IDX_ROW_EDITITEM_TEMPLATE = 2; 
        private const int IDX_ROW_INSERTITEM_TEMPLATE = 3;
        private const int BASE_INDEX = 1000;

        private static string[] _controlTemplateNames = new string[] { 
            "FooterTemplate",
            "HeaderTemplate", 
            "EmptyDataTemplate", 
            "PagerTemplate"
        }; 
        private static bool[] _controlTemplateSupportsDataBinding = new bool[] {
            true,
            true,
            true, 
            true
        }; 
        private const int IDX_CONTROL_HEADER_TEMPLATE = 1; 
        private const int IDX_CONTROL_FOOTER_TEMPLATE = 0;
        private const int IDX_CONTROL_EMPTY_DATA_TEMPLATE = 2; 
        private const int IDX_CONTROL_PAGER_TEMPLATE = 3;

        private DetailsViewActionList _actionLists;
 
        private bool _currentEditState;
        private bool _currentDeleteState; 
        private bool _currentInsertState; 

        private enum ManipulationMode { 
            Edit,
            Delete,
            Insert
        } 

        internal bool _ignoreSchemaRefreshedEvent; 
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.ActionLists"]/*' />
        /// <summary> 
        /// Adds designer actions to the ActionLists collection.
        /// </summary>
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
 
                if (_actionLists == null) {
                    _actionLists = new DetailsViewActionList(this); 
                }
                bool inTemplateMode = InTemplateMode;
                int selectedFieldIndex = SelectedFieldIndex;
                UpdateFieldsCurrentState(); 

                _actionLists.AllowRemoveField = (((DetailsView)Component).Fields.Count > 0 && 
                                            selectedFieldIndex >= 0 && 
                                            !inTemplateMode);
                _actionLists.AllowMoveUp = (((DetailsView)Component).Fields.Count > 0 && 
                                              selectedFieldIndex > 0 &&
                                              !inTemplateMode);
                _actionLists.AllowMoveDown = (((DetailsView)Component).Fields.Count > 0 &&
                                               selectedFieldIndex >= 0 && 
                                               ((DetailsView)Component).Fields.Count > selectedFieldIndex + 1 &&
                                               !inTemplateMode); 
 
                // in the future, these will also look at the DataSourceDesigner to figure out
                // if they should be enabled 
                DesignerDataSourceView view = DesignerView;
                _actionLists.AllowPaging = !inTemplateMode && view != null;
                _actionLists.AllowInserting = !inTemplateMode && (view != null && view.CanInsert);
                _actionLists.AllowEditing = !inTemplateMode && (view != null && view.CanUpdate); 
                _actionLists.AllowDeleting = !inTemplateMode && (view != null && view.CanDelete);
 
                actionLists.Add(_actionLists); 
                return actionLists;
            } 
        }

        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.DETAILSVIEW_SCHEMES, 
                        delegate(DataRow schemeData) { return new DetailsViewAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            }
        }

        /// <summary> 
        /// Called by the action list to enable deleting on the DetailsView
        /// </summary> 
        internal bool EnableDeleting { 
            get {
                return _currentDeleteState; 
            }
            set {
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableDeletingCallback), value, SR.GetString(SR.DetailsView_EnableDeletingTransaction)); 
                } 
                finally {
                    Cursor.Current = originalCursor; 
                }
            }
        }
 
        /// <summary>
        /// Called by the action list to enable editing on the DetailsView 
        /// </summary> 
        internal bool EnableEditing {
            get { 
                return _currentEditState;
            }
            set {
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableEditingCallback), value, SR.GetString(SR.DetailsView_EnableEditingTransaction)); 
                }
                finally { 
                    Cursor.Current = originalCursor;
                }
            }
        } 

        /// <summary> 
        /// Called by the action list to enable sorting on the DetailsView 
        /// </summary>
        internal bool EnableInserting { 
            get {
                return _currentInsertState;
            }
            set { 
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableInsertingCallback), value, SR.GetString(SR.DetailsView_EnableInsertingTransaction));
                } 
                finally {
                    Cursor.Current = originalCursor;
                }
            } 
        }
 
        /// <summary> 
        /// Called by the action list to enable paging on the DetailsView
        /// </summary> 
        internal bool EnablePaging {
            get {
                return ((DetailsView)Component).AllowPaging;
            } 
            set {
                Cursor originalCursor = Cursor.Current; 
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnablePagingCallback), value, SR.GetString(SR.DetailsView_EnablePagingTransaction)); 
                }
                finally {
                    Cursor.Current = originalCursor;
                } 
            }
        } 
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.SampleRowCount"]/*' />
        /// <summary> 
        /// Used to determine the number of sample rows the control wishes to show.
        /// </summary>
        protected override int SampleRowCount{
            get { 
                return 2;   //one to show, one for paging
            } 
        } 

        /// <summary> 
        ///   The index of the currently selected clickable field region
        /// </summary>
        private int SelectedFieldIndex {
            get { 
                object selectedFieldIndex = DesignerState["SelectedFieldIndex"];
                int fieldCount = ((DetailsView)Component).Fields.Count; 
                if (selectedFieldIndex == null || 
                    fieldCount == 0 ||
                    (int)selectedFieldIndex < 0 || 
                    (int)selectedFieldIndex >= fieldCount) {
                    return -1;
                }
                return (int)selectedFieldIndex; 
            }
            set { 
                DesignerState["SelectedFieldIndex"] = value; 
            }
        } 

        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
 
                DataControlFieldCollection rows = ((DetailsView)Component).Fields;
                int rowCount = rows.Count; 

                if (rowCount > 0) {
                    for (int k = 0; k < rowCount; k++) {
                        TemplateField templateField = rows[k] as TemplateField; 
                        if (templateField != null) {
                            string headerText = rows[k].HeaderText; 
                            string caption = SR.GetString(SR.DetailsView_Field, k.ToString(NumberFormatInfo.InvariantInfo)); 

                            if ((headerText != null) && (headerText.Length != 0)) { 
                                caption = caption + " - " + headerText;
                            }

                            TemplateGroup group = new TemplateGroup(caption); 
                            for (int i = 0; i < _rowTemplateNames.Length; i++) {
                                string templateName = _rowTemplateNames[i]; 
                                TemplateDefinition templateDefinition = new TemplateDefinition(this, templateName, rows[k], templateName, GetTemplateStyle(i + BASE_INDEX, templateField)); 
                                templateDefinition.SupportsDataBinding = _rowTemplateSupportsDataBinding[i];
                                group.AddTemplateDefinition(templateDefinition); 
                            }

                            groups.Add(group);
                        } 
                    }
                } 
 
                for (int i = 0; i < _controlTemplateNames.Length; i++) {
                    string templateName = _controlTemplateNames[i]; 

                    TemplateGroup group = new TemplateGroup(_controlTemplateNames[i], GetTemplateStyle(i, null));
                    TemplateDefinition templateDefinition = new TemplateDefinition(this, templateName, Component, templateName);
                    templateDefinition.SupportsDataBinding = _controlTemplateSupportsDataBinding[i]; 
                    group.AddTemplateDefinition(templateDefinition);
 
                    groups.Add(group); 
                }
 
                return groups;
            }
        }
 
        protected override bool UsePreviewControl {
            get { 
                return true; 
            }
        } 

        /// <summary>
        /// Adds bound fields and data keys using schema after the DataSourceID property is set.
        /// </summary> 
        private void AddKeysAndBoundFields(IDataSourceViewSchema schema) {
            DataControlFieldCollection fields = ((DetailsView)Component).Fields; 
 
            Debug.Assert(schema != null, "Did not expect null schema in AddKeysAndBoundFields");
            if (schema != null) { 
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                if (fieldSchemas != null && fieldSchemas.Length > 0) {
                    ArrayList keys = new ArrayList();
                    foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) { 
                        if (!((DetailsView)Component).IsBindableType(fieldSchema.DataType)) {
                            continue; 
                        } 
                        BoundField boundField;
                        if (fieldSchema.DataType == typeof(bool) || 
                            fieldSchema.DataType == typeof(bool?)) {
                            boundField = new CheckBoxField();
                        }
                        else { 
                            boundField = new BoundField();
                        } 
 
                        string fieldName = fieldSchema.Name;
 
                        if (fieldSchema.PrimaryKey) {
                            keys.Add(fieldName);
                        }
 
                        boundField.DataField = fieldName;
                        boundField.HeaderText = fieldName; 
                        boundField.SortExpression = fieldName; 
                        boundField.ReadOnly = fieldSchema.PrimaryKey || fieldSchema.IsReadOnly;
                        boundField.InsertVisible = !fieldSchema.Identity; 
                        fields.Add(boundField);
                    }
                    ((DetailsView)Component).AutoGenerateRows = false;
 
                    int keyCount = keys.Count;
                    if (keyCount > 0) { 
                        string[] dataKeys = new string[keyCount]; 
                        keys.CopyTo(dataKeys, 0);
                        ((DetailsView)Component).DataKeyNames = dataKeys; 
                    }
                }
            }
        } 

        /// <summary> 
        /// Called by the action list to add a new field on the DetailsView 
        /// </summary>
        internal void AddNewField() { 
            Cursor originalCursor = Cursor.Current;
            try {
                Cursor.Current = Cursors.WaitCursor;
                // Ignore schema refreshed events so when the dialogs are dismissed, we won't overwrite the new fields 
                // with gen'd fields from the new schema.
                _ignoreSchemaRefreshedEvent = true; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(AddNewFieldChangeCallback), null, SR.GetString(SR.DetailsView_AddNewFieldTransaction)); 
                _ignoreSchemaRefreshedEvent = false;
                UpdateDesignTimeHtml(); 
            }
            finally {
                Cursor.Current = originalCursor;
            } 
        }
 
        /// <devdoc> 
        /// Transacted change callback to invoke the Add New Field dialog.
        /// </devdoc> 
        private bool AddNewFieldChangeCallback(object context) {
            // We need to suppress changed events so other data bound controls listening to the data source control's
            // changed events won't see each schema change and update while there's a modal dialog up.
            // This control won't hear it anyways though because it's ignoring schema refreshed events 
            if (DataSourceDesigner != null) {
                DataSourceDesigner.SuppressDataSourceEvents(); 
            } 
            AddDataControlFieldDialog dlg = new AddDataControlFieldDialog(this);
            DialogResult result = UIServiceHelper.ShowDialog(Component.Site, dlg); 
            if (DataSourceDesigner != null) {
                DataSourceDesigner.ResumeDataSourceEvents();
            }
            return (result == DialogResult.OK); 
        }
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.DataBind"]/*' /> 
        /// <summary>
        /// Raised when the control is databound. 
        /// </summary>
        protected override void DataBind(BaseDataBoundControl dataBoundControl) {
            base.DataBind(dataBoundControl);
 
            DetailsView detailsView = (DetailsView)dataBoundControl;
            Table table = detailsView.Controls[0] as Table; 
            int autoGeneratedRows = 0; 
            int headerRows = 1;
            int footerRows = 1; 
            int pagerRows = 0;
            if (detailsView.AllowPaging) {
                if (detailsView.PagerSettings.Position == PagerPosition.TopAndBottom) {
                    pagerRows = 2; 
                }
                else { 
                    pagerRows = 1; 
                }
            } 
            if (detailsView.AutoGenerateRows) {
                int autoGeneratedCommandRows = 0;
                if (detailsView.AutoGenerateInsertButton ||
                    detailsView.AutoGenerateDeleteButton || 
                    detailsView.AutoGenerateEditButton) {
                    autoGeneratedCommandRows = 1; 
                } 

                int tableDataRows = table.Rows.Count; 

                autoGeneratedRows = tableDataRows
                    - detailsView.Fields.Count
                    - autoGeneratedCommandRows 
                    - headerRows
                    - footerRows 
                    - pagerRows; 

                Debug.Assert(autoGeneratedRows >= 0, "autoGeneratedRows < 0"); 
            }

            SetRegionAttributes(autoGeneratedRows);
        } 

        /// <summary> 
        /// Called by the action list to edit fields on the DetailsView 
        /// </summary>
        internal void EditFields() { 
            Cursor originalCursor = Cursor.Current;
            try {
                Cursor.Current = Cursors.WaitCursor;
                // Ignore schema refreshed events so when the dialogs are dismissed, we won't overwrite the new fields 
                // with gen'd fields from the new schema.
                _ignoreSchemaRefreshedEvent = true; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(EditFieldsChangeCallback), null, SR.GetString(SR.DetailsView_EditFieldsTransaction)); 
                _ignoreSchemaRefreshedEvent = false;
                UpdateDesignTimeHtml(); 
            }
            finally {
                Cursor.Current = originalCursor;
            } 
        }
 
        /// <devdoc> 
        /// Transacted change callback to invoke the Edit Fields dialog.
        /// </devdoc> 
        private bool EditFieldsChangeCallback(object context) {
            // We need to suppress changed events so other data bound controls listening to the data source control's
            // changed events won't see each schema change and update while there's a modal dialog up.
            // This control won't hear it anyways though because it's ignoring schema refreshed events 
            if (DataSourceDesigner != null) {
                DataSourceDesigner.SuppressDataSourceEvents(); 
            } 
            DataControlFieldsEditor dlg = new DataControlFieldsEditor(this);
            DialogResult result = UIServiceHelper.ShowDialog(Component.Site, dlg); 
            if (DataSourceDesigner != null) {
                DataSourceDesigner.ResumeDataSourceEvents();
            }
            return (result == DialogResult.OK); 
        }
 
        /// <devdoc> 
        /// Transacted change callback to enable or disable deleting.
        /// </devdoc> 
        private bool EnableDeletingCallback(object context) {
            bool setEnabled = !_currentDeleteState;
            if (context is bool) {
                setEnabled = (bool)context; 
            }
 
            SaveManipulationSetting(ManipulationMode.Delete, setEnabled); 
            return true;
        } 

        /// <devdoc>
        /// Transacted change callback to enable or disable editing.
        /// </devdoc> 
        private bool EnableEditingCallback(object context) {
            bool setEnabled = !_currentEditState; 
            if (context is bool) { 
                setEnabled = (bool)context;
            } 

            SaveManipulationSetting(ManipulationMode.Edit, setEnabled);
            return true;
        } 

        /// <devdoc> 
        /// Transacted change callback to enable or disable inserting. 
        /// </devdoc>
        private bool EnableInsertingCallback(object context) { 
            bool setEnabled = !_currentInsertState;
            if (context is bool) {
                setEnabled = (bool)context;
            } 

            SaveManipulationSetting(ManipulationMode.Insert, setEnabled); 
            return true; 
        }
 
        /// <devdoc>
        /// Transacted change callback to enable or disable paging.
        /// </devdoc>
        private bool EnablePagingCallback(object context) { 
            bool currentPageState = ((DetailsView)Component).AllowPaging;
            bool setEnabled = !currentPageState; 
            if (context is bool) { 
                setEnabled = (bool)context;
            } 

            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(DetailsView))["AllowPaging"];
            propDesc.SetValue(Component, setEnabled);
            return true; 
        }
 
        /// <devdoc> 
        /// Attempts to get schema from the data source. If the schema cannot
        /// be retrieved, or there is no schema, then null is returned. 
        /// </devdoc>
        private IDataSourceViewSchema GetDataSourceSchema() {
            DesignerDataSourceView view = DesignerView;
            if (view != null) { 
                try {
                    return view.Schema; 
                } 
                catch (Exception ex) {
                    IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)Component.Site.GetService(typeof(IComponentDesignerDebugService)); 
                    if (debugService != null) {
                        debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message));
                    }
                } 
            }
            return null; 
        } 

        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.GetDesignTimeHtml"]/*' /> 
        /// <summary>
        /// Provides the design-time HTML used to display the control
        /// at design-time.
        /// </summary> 
        /// <returns>
        /// The HTML used to render the control at design-time. 
        /// </returns> 
        public override string GetDesignTimeHtml() {
            DetailsView detailsView = (DetailsView)ViewControl; 

            if (detailsView.Fields.Count == 0) {
                detailsView.AutoGenerateRows = true;
            } 

            bool hasSchema = false; 
            IDataSourceViewSchema schema = GetDataSourceSchema(); 
            if (schema != null) {
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields(); 
                if (fieldSchemas != null && fieldSchemas.Length > 0) {
                    hasSchema = true;
                }
            } 

            if (!hasSchema) { 
                detailsView.DataKeyNames = new string[0]; 
            }
 
            // refresh the TypeDescriptor so that PreFilterProperties gets called
            TypeDescriptor.Refresh(this.Component);

            return base.GetDesignTimeHtml(); 
        }
 
        /// <internalonly/> 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            string html = GetDesignTimeHtml(); 
            DetailsView detailsView = (DetailsView)ViewControl;

            int detailsViewRowCount = detailsView.Rows.Count;
            int selectedFieldIndex = SelectedFieldIndex; 

            // Create the regions. The whole row is clickable, the header is selectable, like GridView. 
            DetailsViewRowCollection rows = detailsView.Rows; 
            for (int i = 0; i < detailsView.Fields.Count; i++) {
                string rowIdentifier = SR.GetString(SR.DetailsView_Field, i.ToString(NumberFormatInfo.InvariantInfo)); 
                string headerText = detailsView.Fields[i].HeaderText;
                if (headerText.Length == 0) {
                    rowIdentifier += " - " + headerText;
                } 

                if (i < detailsViewRowCount) { 
                    DetailsViewRow row = rows[i]; 
                    for (int j = 0; j < row.Cells.Count; j++) {
                        TableCell cell = row.Cells[j]; 

                        if (j == 0) {
                            DesignerRegion selectRegion = new DesignerRegion(this, rowIdentifier, true);
                            selectRegion.UserData = i; 
                            if (i == selectedFieldIndex) {
                                selectRegion.Highlight = true; 
                            } 
                            regions.Add(selectRegion);
                        } 
                        else {
                            DesignerRegion clickRegion = new DesignerRegion(this, i.ToString(NumberFormatInfo.InvariantInfo), false);
                            clickRegion.UserData = -1;
                            if (i == selectedFieldIndex) { 
                                clickRegion.Highlight = true;
                            } 
                            regions.Add(clickRegion); 
                        }
                    } 
                }
            }

            return html; 
        }
 
        private Style GetTemplateStyle(int templateIndex, TemplateField templateField) { 
            Style style = new Style();
            style.CopyFrom(((DetailsView)ViewControl).ControlStyle); 

            if (templateIndex > BASE_INDEX) {
                Debug.Assert(templateField != null);
            } 

            switch (templateIndex) { 
                case IDX_CONTROL_HEADER_TEMPLATE: 
                    style.CopyFrom(((DetailsView)ViewControl).HeaderStyle);
                    break; 
                case IDX_CONTROL_FOOTER_TEMPLATE:
                    style.CopyFrom(((DetailsView)ViewControl).FooterStyle);
                    break;
                case IDX_CONTROL_EMPTY_DATA_TEMPLATE: 
                    style.CopyFrom(((DetailsView)ViewControl).EmptyDataRowStyle);
                    break; 
                case IDX_CONTROL_PAGER_TEMPLATE: 
                    style.CopyFrom(((DetailsView)ViewControl).PagerStyle);
                    break; 
                case IDX_ROW_HEADER_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((DetailsView)ViewControl).HeaderStyle);
                    style.CopyFrom(templateField.HeaderStyle);
                    break; 
                case IDX_ROW_ITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((DetailsView)ViewControl).RowStyle); 
                    style.CopyFrom(templateField.ItemStyle); 
                    break;
                case IDX_ROW_ALTITEM_TEMPLATE + BASE_INDEX: 
                    style.CopyFrom(((DetailsView)ViewControl).RowStyle);
                    style.CopyFrom(((DetailsView)ViewControl).AlternatingRowStyle);
                    style.CopyFrom(templateField.ItemStyle);
                    break; 
                case IDX_ROW_EDITITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((DetailsView)ViewControl).RowStyle); 
                    style.CopyFrom(((DetailsView)ViewControl).EditRowStyle); 
                    style.CopyFrom(templateField.ItemStyle);
                    break; 
                case IDX_ROW_INSERTITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((DetailsView)ViewControl).RowStyle);
                    style.CopyFrom(((DetailsView)ViewControl).InsertRowStyle);
                    style.CopyFrom(templateField.ItemStyle); 
                    break;
            } 
 
            return style;
        } 

        /// <internalonly/>
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            return String.Empty; // for now, no editable region support. 
        }
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(DetailsView)); 
            base.Initialize(component);
 
            if (View != null) {
                View.SetFlags(ViewFlags.TemplateEditing, true);
            }
        } 

 
        /// <summary> 
        /// Called by the action list to move a field down on the DetailsView
        /// </summary> 
        internal void MoveDown() {
            Cursor originalCursor = Cursor.Current;
            try {
                Cursor.Current = Cursors.WaitCursor; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(MoveDownCallback), null, SR.GetString(SR.DetailsView_MoveDownTransaction));
                UpdateDesignTimeHtml(); 
            } 
            finally {
                Cursor.Current = originalCursor; 
            }
        }

        /// <devdoc> 
        /// Transacted change callback to move a field down.
        /// </devdoc> 
        private bool MoveDownCallback(object context) { 
            DataControlFieldCollection fields = ((DetailsView)Component).Fields;
            int selectedFieldIndex = SelectedFieldIndex; 
            if (selectedFieldIndex >= 0 && fields.Count > (selectedFieldIndex + 1)) {
                DataControlField field = fields[selectedFieldIndex];
                fields.RemoveAt(selectedFieldIndex);
                fields.Insert(selectedFieldIndex + 1, field); 
                SelectedFieldIndex++;
                UpdateDesignTimeHtml(); 
                return true; 
            }
            return false; 
        }

        /// <summary>
        /// Called by the action list to move a field up on the DetailsView 
        /// </summary>
        internal void MoveUp() { 
            Cursor originalCursor = Cursor.Current; 
            try {
                Cursor.Current = Cursors.WaitCursor; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(MoveUpCallback), null, SR.GetString(SR.DetailsView_MoveUpTransaction));
                UpdateDesignTimeHtml();
            }
            finally { 
                Cursor.Current = originalCursor;
            } 
        } 

        /// <devdoc> 
        /// Transacted change callback to move a field up.
        /// </devdoc>
        private bool MoveUpCallback(object context) {
            DataControlFieldCollection fields = ((DetailsView)Component).Fields; 
            int selectedFieldIndex = SelectedFieldIndex;
            if (selectedFieldIndex > 0) { 
                DataControlField field = fields[selectedFieldIndex]; 
                fields.RemoveAt(selectedFieldIndex);
                fields.Insert(selectedFieldIndex - 1, field); 
                SelectedFieldIndex--;
                UpdateDesignTimeHtml();
                return true;
            } 
            return false;
        } 
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.OnClick"]/*' />
        protected override void OnClick(DesignerRegionMouseEventArgs e) { 
            if (e.Region != null) {
                SelectedFieldIndex = (int)e.Region.UserData;
                UpdateDesignTimeHtml();
            } 
        }
 
        /// <devdoc> 
        /// Override to execute custom UI-less poststeps to choosing a data source
        /// </devdoc> 
        protected override void OnSchemaRefreshed() {
            if (InTemplateMode) {
                // We ignore the SchemaRefreshed event if we are in template
                // editing mode since the designer won't reflect the changes. 
                return;
            } 
 
            if (_ignoreSchemaRefreshedEvent) {
                return; 
            }

            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor;
                InvokeTransactedChange(Component, new TransactedChangeCallback(SchemaRefreshedCallback), null, SR.GetString(SR.DataControls_SchemaRefreshedTransaction)); 
                UpdateDesignTimeHtml(); 
            }
            finally { 
                Cursor.Current = originalCursor;
            }
        }
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.PreFilterProperties"]/*' />
        /// <summary> 
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements.
        /// </summary> 
        /// <param name="properties">
        /// The properties to be filtered.
        /// </param>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);
 
            // remove the browsable attribute of Fields in template mode so you can't bring up 
            // the dialog and delete the column that's in template mode
            if (InTemplateMode) { 
                PropertyDescriptor fieldsProp = (PropertyDescriptor)properties["Fields"];
                properties["Fields"] =
                    TypeDescriptor.CreateProperty(fieldsProp.ComponentType, fieldsProp, BrowsableAttribute.No);
            } 

        } 
 
        /// <devdoc>
        /// Saves the changed states of the checkboxes and alters the settings of the control to match. 
        /// </devdoc>
        private void SaveManipulationSetting(ManipulationMode mode, bool newState) {
            DataControlFieldCollection fields = ((DetailsView)Component).Fields;
            bool changedCommandField = false; 
            ArrayList fieldsToRemove = new ArrayList();
 
            foreach (DataControlField field in fields) { 
                CommandField commandField = field as CommandField;
                if (commandField != null) { 
                    switch (mode) {
                        case ManipulationMode.Edit:
                            commandField.ShowEditButton = newState;
                            break; 
                        case ManipulationMode.Delete:
                            commandField.ShowDeleteButton = newState; 
                            break; 
                        case ManipulationMode.Insert:
                            commandField.ShowInsertButton = newState; 
                            break;
                    }

                    // remove the field if it's empty 
                    if (!newState &&
                        !commandField.ShowEditButton && 
                        !commandField.ShowDeleteButton && 
                        !commandField.ShowInsertButton &&
                        !commandField.ShowSelectButton) { 
                        fieldsToRemove.Add(commandField);
                    }

                    changedCommandField = true; 
                }
            } 
 
            // remove outside the loop so we're not altering the collection we're enumerating over.
            foreach (object o in fieldsToRemove) { 
                fields.Remove((DataControlField)o);
            }

            if (!changedCommandField && newState) { 
                CommandField commandField = new CommandField();
                switch (mode) { 
                    case ManipulationMode.Edit: 
                        commandField.ShowEditButton = newState;
                        break; 
                    case ManipulationMode.Delete:
                        commandField.ShowDeleteButton = newState;
                        break;
                    case ManipulationMode.Insert: 
                        commandField.ShowInsertButton = newState;
                        break; 
                } 
                fields.Add(commandField);
            } 

            PropertyDescriptor propDesc;
            if (!newState) {
                DetailsView detailsView = ((DetailsView)Component); 
                switch (mode) {
                    case ManipulationMode.Edit: 
                        propDesc = TypeDescriptor.GetProperties(typeof(DetailsView))["AutoGenerateEditButton"]; 
                        propDesc.SetValue(Component, newState);
                        break; 
                    case ManipulationMode.Delete:
                        propDesc = TypeDescriptor.GetProperties(typeof(DetailsView))["AutoGenerateDeleteButton"];
                        propDesc.SetValue(Component, newState);
                        break; 
                    case ManipulationMode.Insert:
                        propDesc = TypeDescriptor.GetProperties(typeof(DetailsView))["AutoGenerateInsertButton"]; 
                        propDesc.SetValue(Component, newState); 
                        break;
                } 
            }
        }

        /// <devdoc> 
        /// Transacted change callback to remove a field.
        /// </devdoc> 
        private bool RemoveCallback(object context) { 
            int selectedFieldIndex = SelectedFieldIndex;
            if (selectedFieldIndex >= 0) { 
                ((DetailsView)Component).Fields.RemoveAt(selectedFieldIndex);
                if (selectedFieldIndex == ((DetailsView)Component).Fields.Count) {
                    SelectedFieldIndex--;
                    UpdateDesignTimeHtml(); 
                }
                return true; 
            } 
            return false;
        } 

        /// <summary>
        /// Called by the action list to remove a field on the DetailsView
        /// </summary> 
        internal void RemoveField() {
            Cursor originalCursor = Cursor.Current; 
            try { 
                Cursor.Current = Cursors.WaitCursor;
                InvokeTransactedChange(Component, new TransactedChangeCallback(RemoveCallback), null, SR.GetString(SR.DetailsView_RemoveFieldTransaction)); 
                UpdateDesignTimeHtml();
            }
            finally {
                Cursor.Current = originalCursor; 
            }
        } 
 
        /// <devdoc>
        /// Transacted change callback for the SchemaRefreshed event. 
        /// </devdoc>
        private bool SchemaRefreshedCallback(object context) {
            IDataSourceViewSchema schema = GetDataSourceSchema();
            if (DataSourceID.Length > 0 && schema != null) { 
                if (((DetailsView)Component).Fields.Count > 0 || ((DetailsView)Component).DataKeyNames.Length > 0) {
                    // warn that we're going to obliterate the fields you have 
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site, 
                                                SR.GetString(SR.DataBoundControl_SchemaRefreshedWarning, SR.GetString(SR.DataBoundControl_DetailsView), SR.GetString(SR.DataBoundControl_Row)),
                                                SR.GetString(SR.DataBoundControl_SchemaRefreshedCaption, ((DetailsView)Component).ID), 
                                                MessageBoxButtons.YesNo)) {
                        ((DetailsView)Component).DataKeyNames = new string[0];
                        ((DetailsView)Component).Fields.Clear();
                        SelectedFieldIndex = -1;   // we don't want a design time html update yet. 
                        AddKeysAndBoundFields(schema);
                    } 
                } 
                else {
                    // just ask if we should generate new ones, since you don't have any now 
                    AddKeysAndBoundFields(schema);
                }
            }
            else {  // either the DataSourceID property was set to "" or we don't have schema 
                if (((DetailsView)Component).Fields.Count > 0 || ((DetailsView)Component).DataKeyNames.Length > 0) {
                    // ask if we can clear your fields/keys 
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site, 
                                                                       SR.GetString(SR.DataBoundControl_SchemaRefreshedWarningNoDataSource, SR.GetString(SR.DataBoundControl_DetailsView), SR.GetString(SR.DataBoundControl_Row)),
                                                                       SR.GetString(SR.DataBoundControl_SchemaRefreshedCaption, ((DetailsView)Component).ID), 
                                                                       MessageBoxButtons.YesNo)) {
                        ((DetailsView)Component).DataKeyNames = new string[0];
                        ((DetailsView)Component).Fields.Clear();
                        SelectedFieldIndex = -1;   // we don't want a design time html update yet. 
                    }
                } 
            } 
            return true;
        } 

        /// <internalonly/>
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
            Debug.Fail("No editable region support!"); 
            return;
        } 
 
        private void SetRegionAttributes(int autoGeneratedRows) {
            int regionCount = 0; 

            DetailsView previewControl = (DetailsView)ViewControl;
            Table table = previewControl.Controls[0] as Table;
            if (table != null) { 
                int topPagerRows = 0;
                if (previewControl.AllowPaging && previewControl.PagerSettings.Position != PagerPosition.Bottom) { 
                    topPagerRows = 1; 
                }
                int topRows = autoGeneratedRows + 1 + topPagerRows;    // always one header row 
                TableRowCollection rows = table.Rows;
                for (int i = topRows; (i < previewControl.Fields.Count + topRows) && (i < rows.Count); i++) {
                    TableRow row = rows[i];
 
                    foreach (TableCell cell in row.Cells) {
                        cell.Attributes[DesignerRegion.DesignerRegionAttributeName] = regionCount.ToString(NumberFormatInfo.InvariantInfo); 
                        regionCount++; 
                    }
                } 
            }
        }

        /// <devdoc> 
        /// Gets the curent states of the fields collection.
        /// </devdoc> 
        private void UpdateFieldsCurrentState() { 
            _currentInsertState = ((DetailsView)Component).AutoGenerateInsertButton;
            _currentEditState = ((DetailsView)Component).AutoGenerateEditButton; 
            _currentDeleteState = ((DetailsView)Component).AutoGenerateDeleteButton;

            foreach (DataControlField field in ((DetailsView)Component).Fields) {
                CommandField commandField = field as CommandField; 
                if (commandField != null) {
                    if (commandField.ShowInsertButton) { 
                        _currentInsertState = true; 
                    }
                    if (commandField.ShowEditButton) { 
                        _currentEditState = true;
                    }
                    if (commandField.ShowDeleteButton) {
                        _currentDeleteState = true; 
                    }
                } 
            } 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.IO;
    using System.Reflection; 
    using System.Text;
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
 
    using Table = System.Web.UI.WebControls.Table; 
    using TableRow = System.Web.UI.WebControls.TableRow;
 
    /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner"]/*' />
    /// <summary>
    /// DetailsViewDesigner is the designer associated with a
    /// DetailsView. 
    /// </summary>
    public class DetailsViewDesigner : DataBoundControlDesigner { 
 
        private static DesignerAutoFormatCollection _autoFormats;
 
        private static string[] _rowTemplateNames = new string[] {
            "ItemTemplate",
            "AlternatingItemTemplate",
            "EditItemTemplate", 
            "InsertItemTemplate",
            "HeaderTemplate", 
        }; 
        private static bool[] _rowTemplateSupportsDataBinding = new bool[] {
            true, 
            true,
            true,
            true,
            false 
        };
        private const int IDX_ROW_HEADER_TEMPLATE = 4; 
        private const int IDX_ROW_ITEM_TEMPLATE = 0; 
        private const int IDX_ROW_ALTITEM_TEMPLATE = 1;
        private const int IDX_ROW_EDITITEM_TEMPLATE = 2; 
        private const int IDX_ROW_INSERTITEM_TEMPLATE = 3;
        private const int BASE_INDEX = 1000;

        private static string[] _controlTemplateNames = new string[] { 
            "FooterTemplate",
            "HeaderTemplate", 
            "EmptyDataTemplate", 
            "PagerTemplate"
        }; 
        private static bool[] _controlTemplateSupportsDataBinding = new bool[] {
            true,
            true,
            true, 
            true
        }; 
        private const int IDX_CONTROL_HEADER_TEMPLATE = 1; 
        private const int IDX_CONTROL_FOOTER_TEMPLATE = 0;
        private const int IDX_CONTROL_EMPTY_DATA_TEMPLATE = 2; 
        private const int IDX_CONTROL_PAGER_TEMPLATE = 3;

        private DetailsViewActionList _actionLists;
 
        private bool _currentEditState;
        private bool _currentDeleteState; 
        private bool _currentInsertState; 

        private enum ManipulationMode { 
            Edit,
            Delete,
            Insert
        } 

        internal bool _ignoreSchemaRefreshedEvent; 
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.ActionLists"]/*' />
        /// <summary> 
        /// Adds designer actions to the ActionLists collection.
        /// </summary>
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
 
                if (_actionLists == null) {
                    _actionLists = new DetailsViewActionList(this); 
                }
                bool inTemplateMode = InTemplateMode;
                int selectedFieldIndex = SelectedFieldIndex;
                UpdateFieldsCurrentState(); 

                _actionLists.AllowRemoveField = (((DetailsView)Component).Fields.Count > 0 && 
                                            selectedFieldIndex >= 0 && 
                                            !inTemplateMode);
                _actionLists.AllowMoveUp = (((DetailsView)Component).Fields.Count > 0 && 
                                              selectedFieldIndex > 0 &&
                                              !inTemplateMode);
                _actionLists.AllowMoveDown = (((DetailsView)Component).Fields.Count > 0 &&
                                               selectedFieldIndex >= 0 && 
                                               ((DetailsView)Component).Fields.Count > selectedFieldIndex + 1 &&
                                               !inTemplateMode); 
 
                // in the future, these will also look at the DataSourceDesigner to figure out
                // if they should be enabled 
                DesignerDataSourceView view = DesignerView;
                _actionLists.AllowPaging = !inTemplateMode && view != null;
                _actionLists.AllowInserting = !inTemplateMode && (view != null && view.CanInsert);
                _actionLists.AllowEditing = !inTemplateMode && (view != null && view.CanUpdate); 
                _actionLists.AllowDeleting = !inTemplateMode && (view != null && view.CanDelete);
 
                actionLists.Add(_actionLists); 
                return actionLists;
            } 
        }

        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.DETAILSVIEW_SCHEMES, 
                        delegate(DataRow schemeData) { return new DetailsViewAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            }
        }

        /// <summary> 
        /// Called by the action list to enable deleting on the DetailsView
        /// </summary> 
        internal bool EnableDeleting { 
            get {
                return _currentDeleteState; 
            }
            set {
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableDeletingCallback), value, SR.GetString(SR.DetailsView_EnableDeletingTransaction)); 
                } 
                finally {
                    Cursor.Current = originalCursor; 
                }
            }
        }
 
        /// <summary>
        /// Called by the action list to enable editing on the DetailsView 
        /// </summary> 
        internal bool EnableEditing {
            get { 
                return _currentEditState;
            }
            set {
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableEditingCallback), value, SR.GetString(SR.DetailsView_EnableEditingTransaction)); 
                }
                finally { 
                    Cursor.Current = originalCursor;
                }
            }
        } 

        /// <summary> 
        /// Called by the action list to enable sorting on the DetailsView 
        /// </summary>
        internal bool EnableInserting { 
            get {
                return _currentInsertState;
            }
            set { 
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableInsertingCallback), value, SR.GetString(SR.DetailsView_EnableInsertingTransaction));
                } 
                finally {
                    Cursor.Current = originalCursor;
                }
            } 
        }
 
        /// <summary> 
        /// Called by the action list to enable paging on the DetailsView
        /// </summary> 
        internal bool EnablePaging {
            get {
                return ((DetailsView)Component).AllowPaging;
            } 
            set {
                Cursor originalCursor = Cursor.Current; 
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnablePagingCallback), value, SR.GetString(SR.DetailsView_EnablePagingTransaction)); 
                }
                finally {
                    Cursor.Current = originalCursor;
                } 
            }
        } 
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.SampleRowCount"]/*' />
        /// <summary> 
        /// Used to determine the number of sample rows the control wishes to show.
        /// </summary>
        protected override int SampleRowCount{
            get { 
                return 2;   //one to show, one for paging
            } 
        } 

        /// <summary> 
        ///   The index of the currently selected clickable field region
        /// </summary>
        private int SelectedFieldIndex {
            get { 
                object selectedFieldIndex = DesignerState["SelectedFieldIndex"];
                int fieldCount = ((DetailsView)Component).Fields.Count; 
                if (selectedFieldIndex == null || 
                    fieldCount == 0 ||
                    (int)selectedFieldIndex < 0 || 
                    (int)selectedFieldIndex >= fieldCount) {
                    return -1;
                }
                return (int)selectedFieldIndex; 
            }
            set { 
                DesignerState["SelectedFieldIndex"] = value; 
            }
        } 

        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
 
                DataControlFieldCollection rows = ((DetailsView)Component).Fields;
                int rowCount = rows.Count; 

                if (rowCount > 0) {
                    for (int k = 0; k < rowCount; k++) {
                        TemplateField templateField = rows[k] as TemplateField; 
                        if (templateField != null) {
                            string headerText = rows[k].HeaderText; 
                            string caption = SR.GetString(SR.DetailsView_Field, k.ToString(NumberFormatInfo.InvariantInfo)); 

                            if ((headerText != null) && (headerText.Length != 0)) { 
                                caption = caption + " - " + headerText;
                            }

                            TemplateGroup group = new TemplateGroup(caption); 
                            for (int i = 0; i < _rowTemplateNames.Length; i++) {
                                string templateName = _rowTemplateNames[i]; 
                                TemplateDefinition templateDefinition = new TemplateDefinition(this, templateName, rows[k], templateName, GetTemplateStyle(i + BASE_INDEX, templateField)); 
                                templateDefinition.SupportsDataBinding = _rowTemplateSupportsDataBinding[i];
                                group.AddTemplateDefinition(templateDefinition); 
                            }

                            groups.Add(group);
                        } 
                    }
                } 
 
                for (int i = 0; i < _controlTemplateNames.Length; i++) {
                    string templateName = _controlTemplateNames[i]; 

                    TemplateGroup group = new TemplateGroup(_controlTemplateNames[i], GetTemplateStyle(i, null));
                    TemplateDefinition templateDefinition = new TemplateDefinition(this, templateName, Component, templateName);
                    templateDefinition.SupportsDataBinding = _controlTemplateSupportsDataBinding[i]; 
                    group.AddTemplateDefinition(templateDefinition);
 
                    groups.Add(group); 
                }
 
                return groups;
            }
        }
 
        protected override bool UsePreviewControl {
            get { 
                return true; 
            }
        } 

        /// <summary>
        /// Adds bound fields and data keys using schema after the DataSourceID property is set.
        /// </summary> 
        private void AddKeysAndBoundFields(IDataSourceViewSchema schema) {
            DataControlFieldCollection fields = ((DetailsView)Component).Fields; 
 
            Debug.Assert(schema != null, "Did not expect null schema in AddKeysAndBoundFields");
            if (schema != null) { 
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                if (fieldSchemas != null && fieldSchemas.Length > 0) {
                    ArrayList keys = new ArrayList();
                    foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) { 
                        if (!((DetailsView)Component).IsBindableType(fieldSchema.DataType)) {
                            continue; 
                        } 
                        BoundField boundField;
                        if (fieldSchema.DataType == typeof(bool) || 
                            fieldSchema.DataType == typeof(bool?)) {
                            boundField = new CheckBoxField();
                        }
                        else { 
                            boundField = new BoundField();
                        } 
 
                        string fieldName = fieldSchema.Name;
 
                        if (fieldSchema.PrimaryKey) {
                            keys.Add(fieldName);
                        }
 
                        boundField.DataField = fieldName;
                        boundField.HeaderText = fieldName; 
                        boundField.SortExpression = fieldName; 
                        boundField.ReadOnly = fieldSchema.PrimaryKey || fieldSchema.IsReadOnly;
                        boundField.InsertVisible = !fieldSchema.Identity; 
                        fields.Add(boundField);
                    }
                    ((DetailsView)Component).AutoGenerateRows = false;
 
                    int keyCount = keys.Count;
                    if (keyCount > 0) { 
                        string[] dataKeys = new string[keyCount]; 
                        keys.CopyTo(dataKeys, 0);
                        ((DetailsView)Component).DataKeyNames = dataKeys; 
                    }
                }
            }
        } 

        /// <summary> 
        /// Called by the action list to add a new field on the DetailsView 
        /// </summary>
        internal void AddNewField() { 
            Cursor originalCursor = Cursor.Current;
            try {
                Cursor.Current = Cursors.WaitCursor;
                // Ignore schema refreshed events so when the dialogs are dismissed, we won't overwrite the new fields 
                // with gen'd fields from the new schema.
                _ignoreSchemaRefreshedEvent = true; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(AddNewFieldChangeCallback), null, SR.GetString(SR.DetailsView_AddNewFieldTransaction)); 
                _ignoreSchemaRefreshedEvent = false;
                UpdateDesignTimeHtml(); 
            }
            finally {
                Cursor.Current = originalCursor;
            } 
        }
 
        /// <devdoc> 
        /// Transacted change callback to invoke the Add New Field dialog.
        /// </devdoc> 
        private bool AddNewFieldChangeCallback(object context) {
            // We need to suppress changed events so other data bound controls listening to the data source control's
            // changed events won't see each schema change and update while there's a modal dialog up.
            // This control won't hear it anyways though because it's ignoring schema refreshed events 
            if (DataSourceDesigner != null) {
                DataSourceDesigner.SuppressDataSourceEvents(); 
            } 
            AddDataControlFieldDialog dlg = new AddDataControlFieldDialog(this);
            DialogResult result = UIServiceHelper.ShowDialog(Component.Site, dlg); 
            if (DataSourceDesigner != null) {
                DataSourceDesigner.ResumeDataSourceEvents();
            }
            return (result == DialogResult.OK); 
        }
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.DataBind"]/*' /> 
        /// <summary>
        /// Raised when the control is databound. 
        /// </summary>
        protected override void DataBind(BaseDataBoundControl dataBoundControl) {
            base.DataBind(dataBoundControl);
 
            DetailsView detailsView = (DetailsView)dataBoundControl;
            Table table = detailsView.Controls[0] as Table; 
            int autoGeneratedRows = 0; 
            int headerRows = 1;
            int footerRows = 1; 
            int pagerRows = 0;
            if (detailsView.AllowPaging) {
                if (detailsView.PagerSettings.Position == PagerPosition.TopAndBottom) {
                    pagerRows = 2; 
                }
                else { 
                    pagerRows = 1; 
                }
            } 
            if (detailsView.AutoGenerateRows) {
                int autoGeneratedCommandRows = 0;
                if (detailsView.AutoGenerateInsertButton ||
                    detailsView.AutoGenerateDeleteButton || 
                    detailsView.AutoGenerateEditButton) {
                    autoGeneratedCommandRows = 1; 
                } 

                int tableDataRows = table.Rows.Count; 

                autoGeneratedRows = tableDataRows
                    - detailsView.Fields.Count
                    - autoGeneratedCommandRows 
                    - headerRows
                    - footerRows 
                    - pagerRows; 

                Debug.Assert(autoGeneratedRows >= 0, "autoGeneratedRows < 0"); 
            }

            SetRegionAttributes(autoGeneratedRows);
        } 

        /// <summary> 
        /// Called by the action list to edit fields on the DetailsView 
        /// </summary>
        internal void EditFields() { 
            Cursor originalCursor = Cursor.Current;
            try {
                Cursor.Current = Cursors.WaitCursor;
                // Ignore schema refreshed events so when the dialogs are dismissed, we won't overwrite the new fields 
                // with gen'd fields from the new schema.
                _ignoreSchemaRefreshedEvent = true; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(EditFieldsChangeCallback), null, SR.GetString(SR.DetailsView_EditFieldsTransaction)); 
                _ignoreSchemaRefreshedEvent = false;
                UpdateDesignTimeHtml(); 
            }
            finally {
                Cursor.Current = originalCursor;
            } 
        }
 
        /// <devdoc> 
        /// Transacted change callback to invoke the Edit Fields dialog.
        /// </devdoc> 
        private bool EditFieldsChangeCallback(object context) {
            // We need to suppress changed events so other data bound controls listening to the data source control's
            // changed events won't see each schema change and update while there's a modal dialog up.
            // This control won't hear it anyways though because it's ignoring schema refreshed events 
            if (DataSourceDesigner != null) {
                DataSourceDesigner.SuppressDataSourceEvents(); 
            } 
            DataControlFieldsEditor dlg = new DataControlFieldsEditor(this);
            DialogResult result = UIServiceHelper.ShowDialog(Component.Site, dlg); 
            if (DataSourceDesigner != null) {
                DataSourceDesigner.ResumeDataSourceEvents();
            }
            return (result == DialogResult.OK); 
        }
 
        /// <devdoc> 
        /// Transacted change callback to enable or disable deleting.
        /// </devdoc> 
        private bool EnableDeletingCallback(object context) {
            bool setEnabled = !_currentDeleteState;
            if (context is bool) {
                setEnabled = (bool)context; 
            }
 
            SaveManipulationSetting(ManipulationMode.Delete, setEnabled); 
            return true;
        } 

        /// <devdoc>
        /// Transacted change callback to enable or disable editing.
        /// </devdoc> 
        private bool EnableEditingCallback(object context) {
            bool setEnabled = !_currentEditState; 
            if (context is bool) { 
                setEnabled = (bool)context;
            } 

            SaveManipulationSetting(ManipulationMode.Edit, setEnabled);
            return true;
        } 

        /// <devdoc> 
        /// Transacted change callback to enable or disable inserting. 
        /// </devdoc>
        private bool EnableInsertingCallback(object context) { 
            bool setEnabled = !_currentInsertState;
            if (context is bool) {
                setEnabled = (bool)context;
            } 

            SaveManipulationSetting(ManipulationMode.Insert, setEnabled); 
            return true; 
        }
 
        /// <devdoc>
        /// Transacted change callback to enable or disable paging.
        /// </devdoc>
        private bool EnablePagingCallback(object context) { 
            bool currentPageState = ((DetailsView)Component).AllowPaging;
            bool setEnabled = !currentPageState; 
            if (context is bool) { 
                setEnabled = (bool)context;
            } 

            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(DetailsView))["AllowPaging"];
            propDesc.SetValue(Component, setEnabled);
            return true; 
        }
 
        /// <devdoc> 
        /// Attempts to get schema from the data source. If the schema cannot
        /// be retrieved, or there is no schema, then null is returned. 
        /// </devdoc>
        private IDataSourceViewSchema GetDataSourceSchema() {
            DesignerDataSourceView view = DesignerView;
            if (view != null) { 
                try {
                    return view.Schema; 
                } 
                catch (Exception ex) {
                    IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)Component.Site.GetService(typeof(IComponentDesignerDebugService)); 
                    if (debugService != null) {
                        debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.Schema", ex.Message));
                    }
                } 
            }
            return null; 
        } 

        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.GetDesignTimeHtml"]/*' /> 
        /// <summary>
        /// Provides the design-time HTML used to display the control
        /// at design-time.
        /// </summary> 
        /// <returns>
        /// The HTML used to render the control at design-time. 
        /// </returns> 
        public override string GetDesignTimeHtml() {
            DetailsView detailsView = (DetailsView)ViewControl; 

            if (detailsView.Fields.Count == 0) {
                detailsView.AutoGenerateRows = true;
            } 

            bool hasSchema = false; 
            IDataSourceViewSchema schema = GetDataSourceSchema(); 
            if (schema != null) {
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields(); 
                if (fieldSchemas != null && fieldSchemas.Length > 0) {
                    hasSchema = true;
                }
            } 

            if (!hasSchema) { 
                detailsView.DataKeyNames = new string[0]; 
            }
 
            // refresh the TypeDescriptor so that PreFilterProperties gets called
            TypeDescriptor.Refresh(this.Component);

            return base.GetDesignTimeHtml(); 
        }
 
        /// <internalonly/> 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            string html = GetDesignTimeHtml(); 
            DetailsView detailsView = (DetailsView)ViewControl;

            int detailsViewRowCount = detailsView.Rows.Count;
            int selectedFieldIndex = SelectedFieldIndex; 

            // Create the regions. The whole row is clickable, the header is selectable, like GridView. 
            DetailsViewRowCollection rows = detailsView.Rows; 
            for (int i = 0; i < detailsView.Fields.Count; i++) {
                string rowIdentifier = SR.GetString(SR.DetailsView_Field, i.ToString(NumberFormatInfo.InvariantInfo)); 
                string headerText = detailsView.Fields[i].HeaderText;
                if (headerText.Length == 0) {
                    rowIdentifier += " - " + headerText;
                } 

                if (i < detailsViewRowCount) { 
                    DetailsViewRow row = rows[i]; 
                    for (int j = 0; j < row.Cells.Count; j++) {
                        TableCell cell = row.Cells[j]; 

                        if (j == 0) {
                            DesignerRegion selectRegion = new DesignerRegion(this, rowIdentifier, true);
                            selectRegion.UserData = i; 
                            if (i == selectedFieldIndex) {
                                selectRegion.Highlight = true; 
                            } 
                            regions.Add(selectRegion);
                        } 
                        else {
                            DesignerRegion clickRegion = new DesignerRegion(this, i.ToString(NumberFormatInfo.InvariantInfo), false);
                            clickRegion.UserData = -1;
                            if (i == selectedFieldIndex) { 
                                clickRegion.Highlight = true;
                            } 
                            regions.Add(clickRegion); 
                        }
                    } 
                }
            }

            return html; 
        }
 
        private Style GetTemplateStyle(int templateIndex, TemplateField templateField) { 
            Style style = new Style();
            style.CopyFrom(((DetailsView)ViewControl).ControlStyle); 

            if (templateIndex > BASE_INDEX) {
                Debug.Assert(templateField != null);
            } 

            switch (templateIndex) { 
                case IDX_CONTROL_HEADER_TEMPLATE: 
                    style.CopyFrom(((DetailsView)ViewControl).HeaderStyle);
                    break; 
                case IDX_CONTROL_FOOTER_TEMPLATE:
                    style.CopyFrom(((DetailsView)ViewControl).FooterStyle);
                    break;
                case IDX_CONTROL_EMPTY_DATA_TEMPLATE: 
                    style.CopyFrom(((DetailsView)ViewControl).EmptyDataRowStyle);
                    break; 
                case IDX_CONTROL_PAGER_TEMPLATE: 
                    style.CopyFrom(((DetailsView)ViewControl).PagerStyle);
                    break; 
                case IDX_ROW_HEADER_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((DetailsView)ViewControl).HeaderStyle);
                    style.CopyFrom(templateField.HeaderStyle);
                    break; 
                case IDX_ROW_ITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((DetailsView)ViewControl).RowStyle); 
                    style.CopyFrom(templateField.ItemStyle); 
                    break;
                case IDX_ROW_ALTITEM_TEMPLATE + BASE_INDEX: 
                    style.CopyFrom(((DetailsView)ViewControl).RowStyle);
                    style.CopyFrom(((DetailsView)ViewControl).AlternatingRowStyle);
                    style.CopyFrom(templateField.ItemStyle);
                    break; 
                case IDX_ROW_EDITITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((DetailsView)ViewControl).RowStyle); 
                    style.CopyFrom(((DetailsView)ViewControl).EditRowStyle); 
                    style.CopyFrom(templateField.ItemStyle);
                    break; 
                case IDX_ROW_INSERTITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((DetailsView)ViewControl).RowStyle);
                    style.CopyFrom(((DetailsView)ViewControl).InsertRowStyle);
                    style.CopyFrom(templateField.ItemStyle); 
                    break;
            } 
 
            return style;
        } 

        /// <internalonly/>
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            return String.Empty; // for now, no editable region support. 
        }
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(DetailsView)); 
            base.Initialize(component);
 
            if (View != null) {
                View.SetFlags(ViewFlags.TemplateEditing, true);
            }
        } 

 
        /// <summary> 
        /// Called by the action list to move a field down on the DetailsView
        /// </summary> 
        internal void MoveDown() {
            Cursor originalCursor = Cursor.Current;
            try {
                Cursor.Current = Cursors.WaitCursor; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(MoveDownCallback), null, SR.GetString(SR.DetailsView_MoveDownTransaction));
                UpdateDesignTimeHtml(); 
            } 
            finally {
                Cursor.Current = originalCursor; 
            }
        }

        /// <devdoc> 
        /// Transacted change callback to move a field down.
        /// </devdoc> 
        private bool MoveDownCallback(object context) { 
            DataControlFieldCollection fields = ((DetailsView)Component).Fields;
            int selectedFieldIndex = SelectedFieldIndex; 
            if (selectedFieldIndex >= 0 && fields.Count > (selectedFieldIndex + 1)) {
                DataControlField field = fields[selectedFieldIndex];
                fields.RemoveAt(selectedFieldIndex);
                fields.Insert(selectedFieldIndex + 1, field); 
                SelectedFieldIndex++;
                UpdateDesignTimeHtml(); 
                return true; 
            }
            return false; 
        }

        /// <summary>
        /// Called by the action list to move a field up on the DetailsView 
        /// </summary>
        internal void MoveUp() { 
            Cursor originalCursor = Cursor.Current; 
            try {
                Cursor.Current = Cursors.WaitCursor; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(MoveUpCallback), null, SR.GetString(SR.DetailsView_MoveUpTransaction));
                UpdateDesignTimeHtml();
            }
            finally { 
                Cursor.Current = originalCursor;
            } 
        } 

        /// <devdoc> 
        /// Transacted change callback to move a field up.
        /// </devdoc>
        private bool MoveUpCallback(object context) {
            DataControlFieldCollection fields = ((DetailsView)Component).Fields; 
            int selectedFieldIndex = SelectedFieldIndex;
            if (selectedFieldIndex > 0) { 
                DataControlField field = fields[selectedFieldIndex]; 
                fields.RemoveAt(selectedFieldIndex);
                fields.Insert(selectedFieldIndex - 1, field); 
                SelectedFieldIndex--;
                UpdateDesignTimeHtml();
                return true;
            } 
            return false;
        } 
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.OnClick"]/*' />
        protected override void OnClick(DesignerRegionMouseEventArgs e) { 
            if (e.Region != null) {
                SelectedFieldIndex = (int)e.Region.UserData;
                UpdateDesignTimeHtml();
            } 
        }
 
        /// <devdoc> 
        /// Override to execute custom UI-less poststeps to choosing a data source
        /// </devdoc> 
        protected override void OnSchemaRefreshed() {
            if (InTemplateMode) {
                // We ignore the SchemaRefreshed event if we are in template
                // editing mode since the designer won't reflect the changes. 
                return;
            } 
 
            if (_ignoreSchemaRefreshedEvent) {
                return; 
            }

            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor;
                InvokeTransactedChange(Component, new TransactedChangeCallback(SchemaRefreshedCallback), null, SR.GetString(SR.DataControls_SchemaRefreshedTransaction)); 
                UpdateDesignTimeHtml(); 
            }
            finally { 
                Cursor.Current = originalCursor;
            }
        }
 
        /// <include file='doc\DetailsViewDesigner.uex' path='docs/doc[@for="DetailsViewDesigner.PreFilterProperties"]/*' />
        /// <summary> 
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements.
        /// </summary> 
        /// <param name="properties">
        /// The properties to be filtered.
        /// </param>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);
 
            // remove the browsable attribute of Fields in template mode so you can't bring up 
            // the dialog and delete the column that's in template mode
            if (InTemplateMode) { 
                PropertyDescriptor fieldsProp = (PropertyDescriptor)properties["Fields"];
                properties["Fields"] =
                    TypeDescriptor.CreateProperty(fieldsProp.ComponentType, fieldsProp, BrowsableAttribute.No);
            } 

        } 
 
        /// <devdoc>
        /// Saves the changed states of the checkboxes and alters the settings of the control to match. 
        /// </devdoc>
        private void SaveManipulationSetting(ManipulationMode mode, bool newState) {
            DataControlFieldCollection fields = ((DetailsView)Component).Fields;
            bool changedCommandField = false; 
            ArrayList fieldsToRemove = new ArrayList();
 
            foreach (DataControlField field in fields) { 
                CommandField commandField = field as CommandField;
                if (commandField != null) { 
                    switch (mode) {
                        case ManipulationMode.Edit:
                            commandField.ShowEditButton = newState;
                            break; 
                        case ManipulationMode.Delete:
                            commandField.ShowDeleteButton = newState; 
                            break; 
                        case ManipulationMode.Insert:
                            commandField.ShowInsertButton = newState; 
                            break;
                    }

                    // remove the field if it's empty 
                    if (!newState &&
                        !commandField.ShowEditButton && 
                        !commandField.ShowDeleteButton && 
                        !commandField.ShowInsertButton &&
                        !commandField.ShowSelectButton) { 
                        fieldsToRemove.Add(commandField);
                    }

                    changedCommandField = true; 
                }
            } 
 
            // remove outside the loop so we're not altering the collection we're enumerating over.
            foreach (object o in fieldsToRemove) { 
                fields.Remove((DataControlField)o);
            }

            if (!changedCommandField && newState) { 
                CommandField commandField = new CommandField();
                switch (mode) { 
                    case ManipulationMode.Edit: 
                        commandField.ShowEditButton = newState;
                        break; 
                    case ManipulationMode.Delete:
                        commandField.ShowDeleteButton = newState;
                        break;
                    case ManipulationMode.Insert: 
                        commandField.ShowInsertButton = newState;
                        break; 
                } 
                fields.Add(commandField);
            } 

            PropertyDescriptor propDesc;
            if (!newState) {
                DetailsView detailsView = ((DetailsView)Component); 
                switch (mode) {
                    case ManipulationMode.Edit: 
                        propDesc = TypeDescriptor.GetProperties(typeof(DetailsView))["AutoGenerateEditButton"]; 
                        propDesc.SetValue(Component, newState);
                        break; 
                    case ManipulationMode.Delete:
                        propDesc = TypeDescriptor.GetProperties(typeof(DetailsView))["AutoGenerateDeleteButton"];
                        propDesc.SetValue(Component, newState);
                        break; 
                    case ManipulationMode.Insert:
                        propDesc = TypeDescriptor.GetProperties(typeof(DetailsView))["AutoGenerateInsertButton"]; 
                        propDesc.SetValue(Component, newState); 
                        break;
                } 
            }
        }

        /// <devdoc> 
        /// Transacted change callback to remove a field.
        /// </devdoc> 
        private bool RemoveCallback(object context) { 
            int selectedFieldIndex = SelectedFieldIndex;
            if (selectedFieldIndex >= 0) { 
                ((DetailsView)Component).Fields.RemoveAt(selectedFieldIndex);
                if (selectedFieldIndex == ((DetailsView)Component).Fields.Count) {
                    SelectedFieldIndex--;
                    UpdateDesignTimeHtml(); 
                }
                return true; 
            } 
            return false;
        } 

        /// <summary>
        /// Called by the action list to remove a field on the DetailsView
        /// </summary> 
        internal void RemoveField() {
            Cursor originalCursor = Cursor.Current; 
            try { 
                Cursor.Current = Cursors.WaitCursor;
                InvokeTransactedChange(Component, new TransactedChangeCallback(RemoveCallback), null, SR.GetString(SR.DetailsView_RemoveFieldTransaction)); 
                UpdateDesignTimeHtml();
            }
            finally {
                Cursor.Current = originalCursor; 
            }
        } 
 
        /// <devdoc>
        /// Transacted change callback for the SchemaRefreshed event. 
        /// </devdoc>
        private bool SchemaRefreshedCallback(object context) {
            IDataSourceViewSchema schema = GetDataSourceSchema();
            if (DataSourceID.Length > 0 && schema != null) { 
                if (((DetailsView)Component).Fields.Count > 0 || ((DetailsView)Component).DataKeyNames.Length > 0) {
                    // warn that we're going to obliterate the fields you have 
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site, 
                                                SR.GetString(SR.DataBoundControl_SchemaRefreshedWarning, SR.GetString(SR.DataBoundControl_DetailsView), SR.GetString(SR.DataBoundControl_Row)),
                                                SR.GetString(SR.DataBoundControl_SchemaRefreshedCaption, ((DetailsView)Component).ID), 
                                                MessageBoxButtons.YesNo)) {
                        ((DetailsView)Component).DataKeyNames = new string[0];
                        ((DetailsView)Component).Fields.Clear();
                        SelectedFieldIndex = -1;   // we don't want a design time html update yet. 
                        AddKeysAndBoundFields(schema);
                    } 
                } 
                else {
                    // just ask if we should generate new ones, since you don't have any now 
                    AddKeysAndBoundFields(schema);
                }
            }
            else {  // either the DataSourceID property was set to "" or we don't have schema 
                if (((DetailsView)Component).Fields.Count > 0 || ((DetailsView)Component).DataKeyNames.Length > 0) {
                    // ask if we can clear your fields/keys 
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site, 
                                                                       SR.GetString(SR.DataBoundControl_SchemaRefreshedWarningNoDataSource, SR.GetString(SR.DataBoundControl_DetailsView), SR.GetString(SR.DataBoundControl_Row)),
                                                                       SR.GetString(SR.DataBoundControl_SchemaRefreshedCaption, ((DetailsView)Component).ID), 
                                                                       MessageBoxButtons.YesNo)) {
                        ((DetailsView)Component).DataKeyNames = new string[0];
                        ((DetailsView)Component).Fields.Clear();
                        SelectedFieldIndex = -1;   // we don't want a design time html update yet. 
                    }
                } 
            } 
            return true;
        } 

        /// <internalonly/>
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
            Debug.Fail("No editable region support!"); 
            return;
        } 
 
        private void SetRegionAttributes(int autoGeneratedRows) {
            int regionCount = 0; 

            DetailsView previewControl = (DetailsView)ViewControl;
            Table table = previewControl.Controls[0] as Table;
            if (table != null) { 
                int topPagerRows = 0;
                if (previewControl.AllowPaging && previewControl.PagerSettings.Position != PagerPosition.Bottom) { 
                    topPagerRows = 1; 
                }
                int topRows = autoGeneratedRows + 1 + topPagerRows;    // always one header row 
                TableRowCollection rows = table.Rows;
                for (int i = topRows; (i < previewControl.Fields.Count + topRows) && (i < rows.Count); i++) {
                    TableRow row = rows[i];
 
                    foreach (TableCell cell in row.Cells) {
                        cell.Attributes[DesignerRegion.DesignerRegionAttributeName] = regionCount.ToString(NumberFormatInfo.InvariantInfo); 
                        regionCount++; 
                    }
                } 
            }
        }

        /// <devdoc> 
        /// Gets the curent states of the fields collection.
        /// </devdoc> 
        private void UpdateFieldsCurrentState() { 
            _currentInsertState = ((DetailsView)Component).AutoGenerateInsertButton;
            _currentEditState = ((DetailsView)Component).AutoGenerateEditButton; 
            _currentDeleteState = ((DetailsView)Component).AutoGenerateDeleteButton;

            foreach (DataControlField field in ((DetailsView)Component).Fields) {
                CommandField commandField = field as CommandField; 
                if (commandField != null) {
                    if (commandField.ShowInsertButton) { 
                        _currentInsertState = true; 
                    }
                    if (commandField.ShowEditButton) { 
                        _currentEditState = true;
                    }
                    if (commandField.ShowDeleteButton) {
                        _currentDeleteState = true; 
                    }
                } 
            } 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
