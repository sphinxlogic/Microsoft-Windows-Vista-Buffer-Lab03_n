//------------------------------------------------------------------------------ 
// <copyright file="GridViewDesigner.cs" company="Microsoft">
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
 
    using GridView = System.Web.UI.WebControls.GridView; 
    using GridViewRow = System.Web.UI.WebControls.GridViewRow;
    using GridViewRowEventHandler = System.Web.UI.WebControls.GridViewRowEventHandler; 
    using Table = System.Web.UI.WebControls.Table;
    using TableRow = System.Web.UI.WebControls.TableRow;

    /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner"]/*' /> 
    /// <summary>
    /// GridViewDesigner is the designer associated with a 
    /// GridView. 
    /// </summary>
    public class GridViewDesigner : DataBoundControlDesigner { 

        private static DesignerAutoFormatCollection _autoFormats;

        private static string[] _columnTemplateNames = new string[] { 
            "ItemTemplate",
            "AlternatingItemTemplate", 
            "EditItemTemplate", 
            "HeaderTemplate",
            "FooterTemplate" 
        };
        private static bool[] _columnTemplateSupportsDataBinding = new bool[] {
            true,
            true, 
            true,
            false, 
            false 
        };
        private const int IDX_COLUMN_HEADER_TEMPLATE = 3; 
        private const int IDX_COLUMN_ITEM_TEMPLATE = 0;
        private const int IDX_COLUMN_ALTITEM_TEMPLATE = 1;
        private const int IDX_COLUMN_EDITITEM_TEMPLATE = 2;
        private const int IDX_COLUMN_FOOTER_TEMPLATE = 4; 
        private const int BASE_INDEX = 1000;
 
        private static string[] _controlTemplateNames = new string[] { 
            "EmptyDataTemplate",
            "PagerTemplate" 
        };
        private static bool[] _controlTemplateSupportsDataBinding = new bool[] {
            true,
            true 
        };
        private const int IDX_CONTROL_EMPTY_DATA_TEMPLATE = 0; 
        private const int IDX_CONTROL_PAGER_TEMPLATE = 1; 
        private GridViewActionList _actionLists;
 
        private int _regionCount;

        private bool _currentEditState;
        private bool _currentDeleteState; 
        private bool _currentSelectState;
 
        private enum ManipulationMode { 
            Edit,
            Delete, 
            Select
        }

        internal bool _ignoreSchemaRefreshedEvent; 

        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.ActionLists"]/*' /> 
        /// <summary> 
        /// Adds designer actions to the ActionLists collection.
        /// </summary> 
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 

                if (_actionLists == null) { 
                    _actionLists = new GridViewActionList(this); 
                }
                bool inTemplateMode = InTemplateMode; 
                int selectedFieldIndex = SelectedFieldIndex;
                UpdateFieldsCurrentState();

                _actionLists.AllowRemoveField = (((GridView)Component).Columns.Count > 0 && 
                                            selectedFieldIndex >= 0 &&
                                            !inTemplateMode); 
                _actionLists.AllowMoveLeft = (((GridView)Component).Columns.Count > 0 && 
                                              selectedFieldIndex > 0 &&
                                              !inTemplateMode); 
                _actionLists.AllowMoveRight = (((GridView)Component).Columns.Count > 0 &&
                                               selectedFieldIndex >= 0 &&
                                               ((GridView)Component).Columns.Count > selectedFieldIndex + 1 &&
                                               !inTemplateMode); 

                // in the future, these will also look at the DataSourceDesigner to figure out 
                // if they should be enabled 
                DesignerDataSourceView view = DesignerView;
                _actionLists.AllowPaging = !inTemplateMode && view != null; 
                _actionLists.AllowSorting = !inTemplateMode && (view != null && view.CanSort);
                _actionLists.AllowEditing = !inTemplateMode && (view != null && view.CanUpdate);
                _actionLists.AllowDeleting = !inTemplateMode && (view != null && view.CanDelete);
                _actionLists.AllowSelection = !inTemplateMode && view != null; 

                actionLists.Add(_actionLists); 
                return actionLists; 
            }
        } 

        public override DesignerAutoFormatCollection AutoFormats {
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.GRIDVIEW_SCHEMES,
                        delegate(DataRow schemeData) { return new GridViewAutoFormat(schemeData); }); 
                } 
                return _autoFormats;
            } 
        }

        /// <summary>
        /// Called by the action list to enable deleting on the GridView 
        /// </summary>
        internal bool EnableDeleting { 
            get { 
                return _currentDeleteState;
            } 
            set {
                Cursor originalCursor = Cursor.Current;
                try {
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableDeletingCallback), value, SR.GetString(SR.GridView_EnableDeletingTransaction));
                } 
                finally { 
                    Cursor.Current = originalCursor;
                } 
            }
        }

        /// <summary> 
        /// Called by the action list to enable editing on the GridView
        /// </summary> 
        internal bool EnableEditing { 
            get {
                return _currentEditState; 
            }
            set {
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableEditingCallback), value, SR.GetString(SR.GridView_EnableEditingTransaction)); 
                } 
                finally {
                    Cursor.Current = originalCursor; 
                }
            }
        }
 
        /// <summary>
        /// Called by the action list to enable paging on the GridView 
        /// </summary> 
        internal bool EnablePaging {
            get { 
                return ((GridView)Component).AllowPaging;
            }
            set {
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnablePagingCallback), value, SR.GetString(SR.GridView_EnablePagingTransaction)); 
                }
                finally { 
                    Cursor.Current = originalCursor;
                }
            }
        } 

        /// <summary> 
        /// Called by the action list to enable selection on the GridView 
        /// </summary>
        internal bool EnableSelection { 
            get {
                return _currentSelectState;
            }
            set { 
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableSelectionCallback), value, SR.GetString(SR.GridView_EnableSelectionTransaction));
                } 
                finally {
                    Cursor.Current = originalCursor;
                }
            } 
        }
 
        /// <summary> 
        /// Called by the action list to enable sorting on the GridView
        /// </summary> 
        internal bool EnableSorting {
            get {
                return ((GridView)Component).AllowSorting;
            } 
            set {
                Cursor originalCursor = Cursor.Current; 
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableSortingCallback), value, SR.GetString(SR.GridView_EnableSortingTransaction)); 
                }
                finally {
                    Cursor.Current = originalCursor;
                } 
            }
        } 
 
        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.SampleRowCount"]/*' />
        /// <summary> 
        /// Used to determine the number of sample rows the control wishes to show.
        /// </summary>
        protected override int SampleRowCount{
            get { 
                int sampleRows = 5;
                GridView sg = ((GridView)Component); 
                if (sg.AllowPaging && sg.PageSize != 0) { 
                    sampleRows = Math.Min(sg.PageSize, 100) + 1;
                } 
                return sampleRows;
            }
        }
 
        /// <summary>
        /// The index of the currently selected clickable field region 
        /// </summary> 
        private int SelectedFieldIndex {
            get { 
                object selectedFieldIndex = DesignerState["SelectedFieldIndex"];
                int columnFieldCount = ((GridView)Component).Columns.Count;
                if (selectedFieldIndex == null ||
                    columnFieldCount == 0 || 
                    (int)selectedFieldIndex < 0 ||
                    (int)selectedFieldIndex >= columnFieldCount) { 
                    return -1; 
                }
                return (int)selectedFieldIndex; 
            }
            set {
                DesignerState["SelectedFieldIndex"] = value;
            } 
        }
 
        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.TemplateGroups"]/*' /> 
        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;

                DataControlFieldCollection columns = ((GridView)Component).Columns;
                int columnCount = columns.Count; 

                if (columnCount > 0) { 
                    for (int k = 0; k < columnCount; k++) { 
                        TemplateField templateField = columns[k] as TemplateField;
                        if (templateField != null) { 
                            string headerText = columns[k].HeaderText;
                            string caption = SR.GetString(SR.GridView_Field, k.ToString(NumberFormatInfo.InvariantInfo));

                            if ((headerText != null) && (headerText.Length != 0)) { 
                                caption = caption + " - " + headerText;
                            } 
 
                            TemplateGroup group = new TemplateGroup(caption);
 
                            for (int i = 0; i < _columnTemplateNames.Length; i++) {
                                string templateName = _columnTemplateNames[i];
                                TemplateDefinition templateDefinition = new TemplateDefinition(this, templateName, columns[k], templateName, GetTemplateStyle(i + BASE_INDEX, templateField));
                                templateDefinition.SupportsDataBinding = _columnTemplateSupportsDataBinding[i]; 
                                group.AddTemplateDefinition(templateDefinition);
                            } 
 
                            groups.Add(group);
                        } 
                    }
                }

                for (int i = 0; i < _controlTemplateNames.Length; i++) { 
                    string templateName = _controlTemplateNames[i];
 
                    TemplateGroup group = new TemplateGroup(_controlTemplateNames[i]); 
                    TemplateDefinition templateDefinition = new TemplateDefinition(this, templateName, Component, templateName, GetTemplateStyle(i, null));
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
            DataControlFieldCollection fields = ((GridView)Component).Columns;

            Debug.Assert(schema != null, "Did not expect null schema in AddKeysAndBoundFields"); 
            if (schema != null) {
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields(); 
                if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                    ArrayList keys = new ArrayList();
                    foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) { 
                        if (!((GridView)Component).IsBindableType(fieldSchema.DataType)) {
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
                    ((GridView)Component).AutoGenerateColumns = false;
 
                    int keyCount = keys.Count;
                    if (keyCount > 0) {
                        string[] dataKeys = new string[keyCount];
                        keys.CopyTo(dataKeys, 0); 
                        ((GridView)Component).DataKeyNames = dataKeys;
                    } 
                } 
            }
        } 

        /// <summary>
        /// Called by the action list to add a new field on the GridView
        /// </summary> 
        internal void AddNewField() {
            Cursor originalCursor = Cursor.Current; 
            try { 
                Cursor.Current = Cursors.WaitCursor;
                // Ignore schema refreshed events so when the dialogs are dismissed, we won't overwrite the new fields 
                // with gen'd fields from the new schema.
                _ignoreSchemaRefreshedEvent = true;
                InvokeTransactedChange(Component, new TransactedChangeCallback(AddNewFieldChangeCallback), null, SR.GetString(SR.GridView_AddNewFieldTransaction));
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

        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.DataBind"]/*' /> 
        /// <summary>
        /// Raised when the control is databound. 
        /// </summary> 
        protected override void DataBind(BaseDataBoundControl dataBoundControl) {
            GridView gridView = (GridView)dataBoundControl; 
            gridView.RowDataBound += new GridViewRowEventHandler(this.OnRowDataBound);
            try {
                base.DataBind(dataBoundControl);
            } 
            finally {
                gridView.RowDataBound -= new GridViewRowEventHandler(this.OnRowDataBound); 
            } 
        }
 
        /// <summary>
        /// Called by the action list to edit fields on the GridView
        /// </summary>
        internal void EditFields() { 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 
                // Ignore schema refreshed events so when the dialogs are dismissed, we won't overwrite the new fields
                // with gen'd fields from the new schema. 
                _ignoreSchemaRefreshedEvent = true;
                InvokeTransactedChange(Component, new TransactedChangeCallback(EditFieldsChangeCallback), null, SR.GetString(SR.GridView_EditFieldsTransaction));
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
        /// Transacted change callback to enable or disable paging.
        /// </devdoc> 
        private bool EnablePagingCallback(object context) {
            bool currentPageState = ((GridView)Component).AllowPaging; 
            bool setEnabled = !currentPageState; 
            if (context is bool) {
                setEnabled = (bool)context; 
            }

            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AllowPaging"];
            propDesc.SetValue(Component, setEnabled); 

            return true; 
        } 

        /// <devdoc> 
        /// Transacted change callback to enable or disable sorting.
        /// </devdoc>
        private bool EnableSortingCallback(object context) {
            bool currentSortState = ((GridView)Component).AllowSorting; 
            bool setEnabled = !currentSortState;
            if (context is bool) { 
                setEnabled = (bool)context; 
            }
 
            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AllowSorting"];
            propDesc.SetValue(Component, setEnabled);
            return true;
        } 

        /// <devdoc> 
        /// Transacted change callback to enable or disable selection. 
        /// </devdoc>
        private bool EnableSelectionCallback(object context) { 
            bool setEnabled = !_currentEditState;
            if (context is bool) {
                setEnabled = (bool)context;
            } 

            SaveManipulationSetting(ManipulationMode.Select, setEnabled); 
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

        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.GetDesignTimeHtml"]/*' />
        /// <summary> 
        /// Provides the design-time HTML used to display the control
        /// at design-time. 
        /// </summary> 
        /// <returns>
        /// The HTML used to render the control at design-time. 
        /// </returns>
        public override string GetDesignTimeHtml() {
            GridView gridView = (GridView)ViewControl;
 
            IDataSourceDesigner dcd = DataSourceDesigner;
 
            _regionCount = 0; 

            bool hasSchema = false; 
            IDataSourceViewSchema schema = GetDataSourceSchema();
            if (schema != null) {
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                    hasSchema = true;
                } 
            } 
            if (!hasSchema) {
                gridView.DataKeyNames = null; 
            }

            if (gridView.Columns.Count == 0) {
                gridView.AutoGenerateColumns = true; 
            }
 
            // refresh the TypeDescriptor so that PreFilterProperties gets called 
            TypeDescriptor.Refresh(this.Component);
 
            string html = base.GetDesignTimeHtml();
            return html;
        }
 
        /// <internalonly/>
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            string html = GetDesignTimeHtml(); 

            GridView gridView = (GridView)ViewControl; 

            int columnsCount = gridView.Columns.Count;
            GridViewRow headerRow = gridView.HeaderRow;
            GridViewRow footerRow = gridView.FooterRow; 

            int selectedFieldIndex = SelectedFieldIndex; 
 
            // Use three outer loops because it's more efficient to loop through rows first, then columns.
            // All cells are clickable, while just the header cell is selectable. 
            if (headerRow != null) {
                for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++) {
                    string rowIdentifier = SR.GetString(SR.GridView_Field, columnIndex.ToString(NumberFormatInfo.InvariantInfo));
                    string headerText = gridView.Columns[columnIndex].HeaderText; 
                    if (headerText.Length == 0) {
                        rowIdentifier += " - " + headerText; 
                    } 

                    DesignerRegion selectRegion = new DesignerRegion(this, rowIdentifier, true); 
                    selectRegion.UserData = columnIndex;
                    if (columnIndex == selectedFieldIndex) {
                        selectRegion.Highlight = true;
                    } 
                    regions.Add(selectRegion);
                } 
            } 

            // Removing for now because having the whole control clickable prevents the control from ever getting focus. 
            // We should re-enable this when Venus can pass the click event to the control to give it focus.
            for (int rowIndex = 0; rowIndex < gridView.Rows.Count; rowIndex++) {
                GridViewRow row = gridView.Rows[rowIndex];
                for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++) { 
                    DesignerRegion clickRegion = new DesignerRegion(this, columnIndex.ToString(NumberFormatInfo.InvariantInfo), false);
                    clickRegion.UserData = -1; 
                    if (columnIndex == selectedFieldIndex) { 
                        clickRegion.Highlight = true;
                    } 
                    regions.Add(clickRegion);
                }
            }
 
            if (footerRow != null) {
                for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++) { 
                    DesignerRegion clickRegion = new DesignerRegion(this, columnIndex.ToString(NumberFormatInfo.InvariantInfo), false); 
                    clickRegion.UserData = -1;
                    if (columnIndex == selectedFieldIndex) { 
                        clickRegion.Highlight = true;
                    }
                    regions.Add(clickRegion);
                } 
            }
 
            return html; 
        }
 
        /// <internalonly/>
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            return String.Empty; // for now, no editable region support.
        } 

        private Style GetTemplateStyle(int templateIndex, TemplateField templateField) { 
            Style style = new Style(); 
            style.CopyFrom(((GridView)ViewControl).ControlStyle);
 
            if (templateIndex > BASE_INDEX) {
                Debug.Assert(templateField != null);
            }
 
            switch (templateIndex) {
                case IDX_CONTROL_EMPTY_DATA_TEMPLATE: 
                    style.CopyFrom(((GridView)ViewControl).EmptyDataRowStyle); 
                    break;
                case IDX_CONTROL_PAGER_TEMPLATE: 
                    style.CopyFrom(((GridView)ViewControl).PagerStyle);
                    break;
                case IDX_COLUMN_HEADER_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((GridView)ViewControl).HeaderStyle); 
                    style.CopyFrom(templateField.HeaderStyle);
                    break; 
                case IDX_COLUMN_FOOTER_TEMPLATE + BASE_INDEX: 
                    style.CopyFrom(((GridView)ViewControl).FooterStyle);
                    style.CopyFrom(templateField.FooterStyle); 
                    break;
                case IDX_COLUMN_ITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((GridView)ViewControl).RowStyle);
                    style.CopyFrom(templateField.ItemStyle); 
                    break;
                case IDX_COLUMN_ALTITEM_TEMPLATE + BASE_INDEX: 
                    style.CopyFrom(((GridView)ViewControl).RowStyle); 
                    style.CopyFrom(((GridView)ViewControl).AlternatingRowStyle);
                    style.CopyFrom(templateField.ItemStyle); 
                    break;
                case IDX_COLUMN_EDITITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((GridView)ViewControl).RowStyle);
                    style.CopyFrom(((GridView)ViewControl).EditRowStyle); 
                    style.CopyFrom(templateField.ItemStyle);
                    break; 
            } 

            return style; 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(GridView)); 
            base.Initialize(component);
 
            if (View != null) { 
                View.SetFlags(ViewFlags.TemplateEditing, true);
            } 
        }


        /// <summary> 
        /// Called by the action list to move a field left on the GridView
        /// </summary> 
        internal void MoveLeft() { 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor;
                InvokeTransactedChange(Component, new TransactedChangeCallback(MoveLeftCallback), null, SR.GetString(SR.GridView_MoveLeftTransaction));
                UpdateDesignTimeHtml();
            } 
            finally {
                Cursor.Current = originalCursor; 
            } 

        } 

        /// <devdoc>
        /// Transacted change callback to move a field left.
        /// </devdoc> 
        private bool MoveLeftCallback(object context) {
            DataControlFieldCollection fields = ((GridView)Component).Columns; 
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
 
        /// <summary>
        /// Called by the action list to move a field right on the GridView
        /// </summary>
        internal void MoveRight() { 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(MoveRightCallback), null, SR.GetString(SR.GridView_MoveRightTransaction));
                UpdateDesignTimeHtml(); 
            }
            finally {
                Cursor.Current = originalCursor;
            } 
        }
 
        /// <devdoc> 
        /// Transacted change callback to move a field right.
        /// </devdoc> 
        private bool MoveRightCallback(object context) {
            DataControlFieldCollection fields = ((GridView)Component).Columns;
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

        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.OnClick"]/*' /> 
        protected override void OnClick(DesignerRegionMouseEventArgs e) { 
            if (e.Region != null) {
                SelectedFieldIndex = (int)e.Region.UserData; 
                UpdateDesignTimeHtml();
            }
        }
 
        /// <devdoc>
        ///   Adds the designer region attributes on all cells when GetDesignTimeHtml is called. 
        /// </devdoc> 
        private void OnRowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e) {
            GridViewRow row = e.Row; 

            if (row.RowType == DataControlRowType.DataRow ||
                row.RowType == DataControlRowType.Header ||
                row.RowType == DataControlRowType.Footer) { 
                int columnsCount = ((GridView)sender).Columns.Count;
                int autoGenButtonRows = 0; 
 
                if (((GridView)sender).AutoGenerateDeleteButton || ((GridView)sender).AutoGenerateEditButton || ((GridView)sender).AutoGenerateSelectButton) {
                    autoGenButtonRows = 1; 
                }

                for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++) {
                    System.Web.UI.WebControls.TableCell cell = row.Cells[columnIndex + autoGenButtonRows]; 
                    cell.Attributes[DesignerRegion.DesignerRegionAttributeName] = _regionCount.ToString(NumberFormatInfo.InvariantInfo);
                    _regionCount++; 
                } 
            }
 
            return;
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
                InvokeTransactedChange(Component, new TransactedChangeCallback(SchemaRefreshedCallback), null, SR.GetString(SR.GridView_SchemaRefreshedTransaction));
                UpdateDesignTimeHtml();
            } 
            finally {
                Cursor.Current = originalCursor; 
            } 
        }
 
        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.PreFilterProperties"]/*' />
        /// <summary>
        /// Overridden by the designer to shadow various runtime properties
        /// with corresponding properties that it implements. 
        /// </summary>
        /// <param name="properties"> 
        /// The properties to be filtered. 
        /// </param>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);

            // remove the browsable attribute of Columns in template mode so you can't bring up
            // the dialog and delete the column that's in template mode 
            if (InTemplateMode) {
                PropertyDescriptor fieldsProp = (PropertyDescriptor)properties["Columns"]; 
                properties["Columns"] = 
                    TypeDescriptor.CreateProperty(fieldsProp.ComponentType, fieldsProp, BrowsableAttribute.No);
            } 
        }

        /// <summary>
        /// Called by the action list to remove a field on the GridView 
        /// </summary>
        internal void RemoveField() { 
            Cursor originalCursor = Cursor.Current; 
            try {
                Cursor.Current = Cursors.WaitCursor; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(RemoveFieldCallback), null, SR.GetString(SR.GridView_RemoveFieldTransaction));
                UpdateDesignTimeHtml();
            }
            finally { 
                Cursor.Current = originalCursor;
            } 
        } 

        /// <devdoc> 
        /// Transacted change callback to remove a field.
        /// </devdoc>
        private bool RemoveFieldCallback(object context) {
            int selectedFieldIndex = SelectedFieldIndex; 
            if (selectedFieldIndex >= 0) {
                ((GridView)Component).Columns.RemoveAt(selectedFieldIndex); 
                if (selectedFieldIndex == ((GridView)Component).Columns.Count) { 
                    SelectedFieldIndex--;
                    UpdateDesignTimeHtml(); 
                }
                return true;
            }
            return false; 
        }
 
        /// <devdoc> 
        /// Saves the changed states of the checkboxes and alters the settings of the control to match.
        /// </devdoc> 
        private void SaveManipulationSetting(ManipulationMode mode, bool newState) {
            DataControlFieldCollection fields = ((GridView)Component).Columns;
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
                    case ManipulationMode.Select: 
                        commandField.ShowSelectButton = newState;
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
                case ManipulationMode.Select:
                    commandField.ShowSelectButton = newState; 
                    break;
                } 
                fields.Insert(0, commandField); 
            }
 

            PropertyDescriptor propDesc;
            if (!newState) {
                GridView gridView = ((GridView)Component); 
                switch (mode) {
                    case ManipulationMode.Edit: 
                        propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AutoGenerateEditButton"]; 
                        propDesc.SetValue(Component, newState);
                        break; 
                    case ManipulationMode.Delete:
                        propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AutoGenerateDeleteButton"];
                        propDesc.SetValue(Component, newState);
                        break; 
                    case ManipulationMode.Select:
                        propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AutoGenerateSelectButton"]; 
                        propDesc.SetValue(Component, newState); 
                        break;
                } 
            }
        }

        /// <devdoc> 
        /// Transacted change callback for SchemaRefreshed event.
        /// </devdoc> 
        private bool SchemaRefreshedCallback(object context) { 
            IDataSourceViewSchema schema = GetDataSourceSchema();
            if (DataSourceID.Length > 0 && schema != null) { 
                if (((GridView)Component).Columns.Count > 0 || ((GridView)Component).DataKeyNames.Length > 0) {
                    // warn that we're going to obliterate the fields you have
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site,
                                                SR.GetString(SR.DataBoundControl_SchemaRefreshedWarning, SR.GetString(SR.DataBoundControl_GridView), SR.GetString(SR.DataBoundControl_Column)), 
                                                SR.GetString(SR.DataBoundControl_SchemaRefreshedCaption, ((GridView)Component).ID),
                                                MessageBoxButtons.YesNo)) { 
                        ((GridView)Component).DataKeyNames = new string[0]; 
                        ((GridView)Component).Columns.Clear();
                        SelectedFieldIndex = -1;   //  we don't want a design-time html update yet. 
                        AddKeysAndBoundFields(schema);
                    }
                }
                else { 
                    // just ask if we should generate new ones, since you don't have any now
                    AddKeysAndBoundFields(schema); 
                } 
            }
            else {  // either the DataSourceID property was set to "" or we don't have schema 
                if (((GridView)Component).Columns.Count > 0 || ((GridView)Component).DataKeyNames.Length > 0) {
                    // ask if we can clear your fields/keys
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site,
                                                                       SR.GetString(SR.DataBoundControl_SchemaRefreshedWarningNoDataSource, SR.GetString(SR.DataBoundControl_GridView), SR.GetString(SR.DataBoundControl_Column)), 
                                                                       SR.GetString(SR.DataBoundControl_SchemaRefreshedCaption, ((GridView)Component).ID),
                                                                       MessageBoxButtons.YesNo)) { 
                        ((GridView)Component).DataKeyNames = new string[0]; 
                        ((GridView)Component).Columns.Clear();
                        SelectedFieldIndex = -1;   // we don't want a design-time update yet. 
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

        /// <devdoc> 
        /// Gets the curent states of the fields collection.
        /// </devdoc> 
        private void UpdateFieldsCurrentState() { 
            _currentSelectState = ((GridView)Component).AutoGenerateSelectButton;
            _currentEditState = ((GridView)Component).AutoGenerateEditButton; 
            _currentDeleteState = ((GridView)Component).AutoGenerateDeleteButton;

            foreach (DataControlField field in ((GridView)Component).Columns) {
                CommandField commandField = field as CommandField; 
                if (commandField != null) {
                    if (commandField.ShowSelectButton) { 
                        _currentSelectState = true; 
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
// <copyright file="GridViewDesigner.cs" company="Microsoft">
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
 
    using GridView = System.Web.UI.WebControls.GridView; 
    using GridViewRow = System.Web.UI.WebControls.GridViewRow;
    using GridViewRowEventHandler = System.Web.UI.WebControls.GridViewRowEventHandler; 
    using Table = System.Web.UI.WebControls.Table;
    using TableRow = System.Web.UI.WebControls.TableRow;

    /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner"]/*' /> 
    /// <summary>
    /// GridViewDesigner is the designer associated with a 
    /// GridView. 
    /// </summary>
    public class GridViewDesigner : DataBoundControlDesigner { 

        private static DesignerAutoFormatCollection _autoFormats;

        private static string[] _columnTemplateNames = new string[] { 
            "ItemTemplate",
            "AlternatingItemTemplate", 
            "EditItemTemplate", 
            "HeaderTemplate",
            "FooterTemplate" 
        };
        private static bool[] _columnTemplateSupportsDataBinding = new bool[] {
            true,
            true, 
            true,
            false, 
            false 
        };
        private const int IDX_COLUMN_HEADER_TEMPLATE = 3; 
        private const int IDX_COLUMN_ITEM_TEMPLATE = 0;
        private const int IDX_COLUMN_ALTITEM_TEMPLATE = 1;
        private const int IDX_COLUMN_EDITITEM_TEMPLATE = 2;
        private const int IDX_COLUMN_FOOTER_TEMPLATE = 4; 
        private const int BASE_INDEX = 1000;
 
        private static string[] _controlTemplateNames = new string[] { 
            "EmptyDataTemplate",
            "PagerTemplate" 
        };
        private static bool[] _controlTemplateSupportsDataBinding = new bool[] {
            true,
            true 
        };
        private const int IDX_CONTROL_EMPTY_DATA_TEMPLATE = 0; 
        private const int IDX_CONTROL_PAGER_TEMPLATE = 1; 
        private GridViewActionList _actionLists;
 
        private int _regionCount;

        private bool _currentEditState;
        private bool _currentDeleteState; 
        private bool _currentSelectState;
 
        private enum ManipulationMode { 
            Edit,
            Delete, 
            Select
        }

        internal bool _ignoreSchemaRefreshedEvent; 

        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.ActionLists"]/*' /> 
        /// <summary> 
        /// Adds designer actions to the ActionLists collection.
        /// </summary> 
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 

                if (_actionLists == null) { 
                    _actionLists = new GridViewActionList(this); 
                }
                bool inTemplateMode = InTemplateMode; 
                int selectedFieldIndex = SelectedFieldIndex;
                UpdateFieldsCurrentState();

                _actionLists.AllowRemoveField = (((GridView)Component).Columns.Count > 0 && 
                                            selectedFieldIndex >= 0 &&
                                            !inTemplateMode); 
                _actionLists.AllowMoveLeft = (((GridView)Component).Columns.Count > 0 && 
                                              selectedFieldIndex > 0 &&
                                              !inTemplateMode); 
                _actionLists.AllowMoveRight = (((GridView)Component).Columns.Count > 0 &&
                                               selectedFieldIndex >= 0 &&
                                               ((GridView)Component).Columns.Count > selectedFieldIndex + 1 &&
                                               !inTemplateMode); 

                // in the future, these will also look at the DataSourceDesigner to figure out 
                // if they should be enabled 
                DesignerDataSourceView view = DesignerView;
                _actionLists.AllowPaging = !inTemplateMode && view != null; 
                _actionLists.AllowSorting = !inTemplateMode && (view != null && view.CanSort);
                _actionLists.AllowEditing = !inTemplateMode && (view != null && view.CanUpdate);
                _actionLists.AllowDeleting = !inTemplateMode && (view != null && view.CanDelete);
                _actionLists.AllowSelection = !inTemplateMode && view != null; 

                actionLists.Add(_actionLists); 
                return actionLists; 
            }
        } 

        public override DesignerAutoFormatCollection AutoFormats {
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.GRIDVIEW_SCHEMES,
                        delegate(DataRow schemeData) { return new GridViewAutoFormat(schemeData); }); 
                } 
                return _autoFormats;
            } 
        }

        /// <summary>
        /// Called by the action list to enable deleting on the GridView 
        /// </summary>
        internal bool EnableDeleting { 
            get { 
                return _currentDeleteState;
            } 
            set {
                Cursor originalCursor = Cursor.Current;
                try {
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableDeletingCallback), value, SR.GetString(SR.GridView_EnableDeletingTransaction));
                } 
                finally { 
                    Cursor.Current = originalCursor;
                } 
            }
        }

        /// <summary> 
        /// Called by the action list to enable editing on the GridView
        /// </summary> 
        internal bool EnableEditing { 
            get {
                return _currentEditState; 
            }
            set {
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableEditingCallback), value, SR.GetString(SR.GridView_EnableEditingTransaction)); 
                } 
                finally {
                    Cursor.Current = originalCursor; 
                }
            }
        }
 
        /// <summary>
        /// Called by the action list to enable paging on the GridView 
        /// </summary> 
        internal bool EnablePaging {
            get { 
                return ((GridView)Component).AllowPaging;
            }
            set {
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnablePagingCallback), value, SR.GetString(SR.GridView_EnablePagingTransaction)); 
                }
                finally { 
                    Cursor.Current = originalCursor;
                }
            }
        } 

        /// <summary> 
        /// Called by the action list to enable selection on the GridView 
        /// </summary>
        internal bool EnableSelection { 
            get {
                return _currentSelectState;
            }
            set { 
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor; 
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableSelectionCallback), value, SR.GetString(SR.GridView_EnableSelectionTransaction));
                } 
                finally {
                    Cursor.Current = originalCursor;
                }
            } 
        }
 
        /// <summary> 
        /// Called by the action list to enable sorting on the GridView
        /// </summary> 
        internal bool EnableSorting {
            get {
                return ((GridView)Component).AllowSorting;
            } 
            set {
                Cursor originalCursor = Cursor.Current; 
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnableSortingCallback), value, SR.GetString(SR.GridView_EnableSortingTransaction)); 
                }
                finally {
                    Cursor.Current = originalCursor;
                } 
            }
        } 
 
        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.SampleRowCount"]/*' />
        /// <summary> 
        /// Used to determine the number of sample rows the control wishes to show.
        /// </summary>
        protected override int SampleRowCount{
            get { 
                int sampleRows = 5;
                GridView sg = ((GridView)Component); 
                if (sg.AllowPaging && sg.PageSize != 0) { 
                    sampleRows = Math.Min(sg.PageSize, 100) + 1;
                } 
                return sampleRows;
            }
        }
 
        /// <summary>
        /// The index of the currently selected clickable field region 
        /// </summary> 
        private int SelectedFieldIndex {
            get { 
                object selectedFieldIndex = DesignerState["SelectedFieldIndex"];
                int columnFieldCount = ((GridView)Component).Columns.Count;
                if (selectedFieldIndex == null ||
                    columnFieldCount == 0 || 
                    (int)selectedFieldIndex < 0 ||
                    (int)selectedFieldIndex >= columnFieldCount) { 
                    return -1; 
                }
                return (int)selectedFieldIndex; 
            }
            set {
                DesignerState["SelectedFieldIndex"] = value;
            } 
        }
 
        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.TemplateGroups"]/*' /> 
        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;

                DataControlFieldCollection columns = ((GridView)Component).Columns;
                int columnCount = columns.Count; 

                if (columnCount > 0) { 
                    for (int k = 0; k < columnCount; k++) { 
                        TemplateField templateField = columns[k] as TemplateField;
                        if (templateField != null) { 
                            string headerText = columns[k].HeaderText;
                            string caption = SR.GetString(SR.GridView_Field, k.ToString(NumberFormatInfo.InvariantInfo));

                            if ((headerText != null) && (headerText.Length != 0)) { 
                                caption = caption + " - " + headerText;
                            } 
 
                            TemplateGroup group = new TemplateGroup(caption);
 
                            for (int i = 0; i < _columnTemplateNames.Length; i++) {
                                string templateName = _columnTemplateNames[i];
                                TemplateDefinition templateDefinition = new TemplateDefinition(this, templateName, columns[k], templateName, GetTemplateStyle(i + BASE_INDEX, templateField));
                                templateDefinition.SupportsDataBinding = _columnTemplateSupportsDataBinding[i]; 
                                group.AddTemplateDefinition(templateDefinition);
                            } 
 
                            groups.Add(group);
                        } 
                    }
                }

                for (int i = 0; i < _controlTemplateNames.Length; i++) { 
                    string templateName = _controlTemplateNames[i];
 
                    TemplateGroup group = new TemplateGroup(_controlTemplateNames[i]); 
                    TemplateDefinition templateDefinition = new TemplateDefinition(this, templateName, Component, templateName, GetTemplateStyle(i, null));
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
            DataControlFieldCollection fields = ((GridView)Component).Columns;

            Debug.Assert(schema != null, "Did not expect null schema in AddKeysAndBoundFields"); 
            if (schema != null) {
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields(); 
                if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                    ArrayList keys = new ArrayList();
                    foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) { 
                        if (!((GridView)Component).IsBindableType(fieldSchema.DataType)) {
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
                    ((GridView)Component).AutoGenerateColumns = false;
 
                    int keyCount = keys.Count;
                    if (keyCount > 0) {
                        string[] dataKeys = new string[keyCount];
                        keys.CopyTo(dataKeys, 0); 
                        ((GridView)Component).DataKeyNames = dataKeys;
                    } 
                } 
            }
        } 

        /// <summary>
        /// Called by the action list to add a new field on the GridView
        /// </summary> 
        internal void AddNewField() {
            Cursor originalCursor = Cursor.Current; 
            try { 
                Cursor.Current = Cursors.WaitCursor;
                // Ignore schema refreshed events so when the dialogs are dismissed, we won't overwrite the new fields 
                // with gen'd fields from the new schema.
                _ignoreSchemaRefreshedEvent = true;
                InvokeTransactedChange(Component, new TransactedChangeCallback(AddNewFieldChangeCallback), null, SR.GetString(SR.GridView_AddNewFieldTransaction));
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

        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.DataBind"]/*' /> 
        /// <summary>
        /// Raised when the control is databound. 
        /// </summary> 
        protected override void DataBind(BaseDataBoundControl dataBoundControl) {
            GridView gridView = (GridView)dataBoundControl; 
            gridView.RowDataBound += new GridViewRowEventHandler(this.OnRowDataBound);
            try {
                base.DataBind(dataBoundControl);
            } 
            finally {
                gridView.RowDataBound -= new GridViewRowEventHandler(this.OnRowDataBound); 
            } 
        }
 
        /// <summary>
        /// Called by the action list to edit fields on the GridView
        /// </summary>
        internal void EditFields() { 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 
                // Ignore schema refreshed events so when the dialogs are dismissed, we won't overwrite the new fields
                // with gen'd fields from the new schema. 
                _ignoreSchemaRefreshedEvent = true;
                InvokeTransactedChange(Component, new TransactedChangeCallback(EditFieldsChangeCallback), null, SR.GetString(SR.GridView_EditFieldsTransaction));
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
        /// Transacted change callback to enable or disable paging.
        /// </devdoc> 
        private bool EnablePagingCallback(object context) {
            bool currentPageState = ((GridView)Component).AllowPaging; 
            bool setEnabled = !currentPageState; 
            if (context is bool) {
                setEnabled = (bool)context; 
            }

            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AllowPaging"];
            propDesc.SetValue(Component, setEnabled); 

            return true; 
        } 

        /// <devdoc> 
        /// Transacted change callback to enable or disable sorting.
        /// </devdoc>
        private bool EnableSortingCallback(object context) {
            bool currentSortState = ((GridView)Component).AllowSorting; 
            bool setEnabled = !currentSortState;
            if (context is bool) { 
                setEnabled = (bool)context; 
            }
 
            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AllowSorting"];
            propDesc.SetValue(Component, setEnabled);
            return true;
        } 

        /// <devdoc> 
        /// Transacted change callback to enable or disable selection. 
        /// </devdoc>
        private bool EnableSelectionCallback(object context) { 
            bool setEnabled = !_currentEditState;
            if (context is bool) {
                setEnabled = (bool)context;
            } 

            SaveManipulationSetting(ManipulationMode.Select, setEnabled); 
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

        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.GetDesignTimeHtml"]/*' />
        /// <summary> 
        /// Provides the design-time HTML used to display the control
        /// at design-time. 
        /// </summary> 
        /// <returns>
        /// The HTML used to render the control at design-time. 
        /// </returns>
        public override string GetDesignTimeHtml() {
            GridView gridView = (GridView)ViewControl;
 
            IDataSourceDesigner dcd = DataSourceDesigner;
 
            _regionCount = 0; 

            bool hasSchema = false; 
            IDataSourceViewSchema schema = GetDataSourceSchema();
            if (schema != null) {
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                    hasSchema = true;
                } 
            } 
            if (!hasSchema) {
                gridView.DataKeyNames = null; 
            }

            if (gridView.Columns.Count == 0) {
                gridView.AutoGenerateColumns = true; 
            }
 
            // refresh the TypeDescriptor so that PreFilterProperties gets called 
            TypeDescriptor.Refresh(this.Component);
 
            string html = base.GetDesignTimeHtml();
            return html;
        }
 
        /// <internalonly/>
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            string html = GetDesignTimeHtml(); 

            GridView gridView = (GridView)ViewControl; 

            int columnsCount = gridView.Columns.Count;
            GridViewRow headerRow = gridView.HeaderRow;
            GridViewRow footerRow = gridView.FooterRow; 

            int selectedFieldIndex = SelectedFieldIndex; 
 
            // Use three outer loops because it's more efficient to loop through rows first, then columns.
            // All cells are clickable, while just the header cell is selectable. 
            if (headerRow != null) {
                for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++) {
                    string rowIdentifier = SR.GetString(SR.GridView_Field, columnIndex.ToString(NumberFormatInfo.InvariantInfo));
                    string headerText = gridView.Columns[columnIndex].HeaderText; 
                    if (headerText.Length == 0) {
                        rowIdentifier += " - " + headerText; 
                    } 

                    DesignerRegion selectRegion = new DesignerRegion(this, rowIdentifier, true); 
                    selectRegion.UserData = columnIndex;
                    if (columnIndex == selectedFieldIndex) {
                        selectRegion.Highlight = true;
                    } 
                    regions.Add(selectRegion);
                } 
            } 

            // Removing for now because having the whole control clickable prevents the control from ever getting focus. 
            // We should re-enable this when Venus can pass the click event to the control to give it focus.
            for (int rowIndex = 0; rowIndex < gridView.Rows.Count; rowIndex++) {
                GridViewRow row = gridView.Rows[rowIndex];
                for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++) { 
                    DesignerRegion clickRegion = new DesignerRegion(this, columnIndex.ToString(NumberFormatInfo.InvariantInfo), false);
                    clickRegion.UserData = -1; 
                    if (columnIndex == selectedFieldIndex) { 
                        clickRegion.Highlight = true;
                    } 
                    regions.Add(clickRegion);
                }
            }
 
            if (footerRow != null) {
                for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++) { 
                    DesignerRegion clickRegion = new DesignerRegion(this, columnIndex.ToString(NumberFormatInfo.InvariantInfo), false); 
                    clickRegion.UserData = -1;
                    if (columnIndex == selectedFieldIndex) { 
                        clickRegion.Highlight = true;
                    }
                    regions.Add(clickRegion);
                } 
            }
 
            return html; 
        }
 
        /// <internalonly/>
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            return String.Empty; // for now, no editable region support.
        } 

        private Style GetTemplateStyle(int templateIndex, TemplateField templateField) { 
            Style style = new Style(); 
            style.CopyFrom(((GridView)ViewControl).ControlStyle);
 
            if (templateIndex > BASE_INDEX) {
                Debug.Assert(templateField != null);
            }
 
            switch (templateIndex) {
                case IDX_CONTROL_EMPTY_DATA_TEMPLATE: 
                    style.CopyFrom(((GridView)ViewControl).EmptyDataRowStyle); 
                    break;
                case IDX_CONTROL_PAGER_TEMPLATE: 
                    style.CopyFrom(((GridView)ViewControl).PagerStyle);
                    break;
                case IDX_COLUMN_HEADER_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((GridView)ViewControl).HeaderStyle); 
                    style.CopyFrom(templateField.HeaderStyle);
                    break; 
                case IDX_COLUMN_FOOTER_TEMPLATE + BASE_INDEX: 
                    style.CopyFrom(((GridView)ViewControl).FooterStyle);
                    style.CopyFrom(templateField.FooterStyle); 
                    break;
                case IDX_COLUMN_ITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((GridView)ViewControl).RowStyle);
                    style.CopyFrom(templateField.ItemStyle); 
                    break;
                case IDX_COLUMN_ALTITEM_TEMPLATE + BASE_INDEX: 
                    style.CopyFrom(((GridView)ViewControl).RowStyle); 
                    style.CopyFrom(((GridView)ViewControl).AlternatingRowStyle);
                    style.CopyFrom(templateField.ItemStyle); 
                    break;
                case IDX_COLUMN_EDITITEM_TEMPLATE + BASE_INDEX:
                    style.CopyFrom(((GridView)ViewControl).RowStyle);
                    style.CopyFrom(((GridView)ViewControl).EditRowStyle); 
                    style.CopyFrom(templateField.ItemStyle);
                    break; 
            } 

            return style; 
        }

        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(GridView)); 
            base.Initialize(component);
 
            if (View != null) { 
                View.SetFlags(ViewFlags.TemplateEditing, true);
            } 
        }


        /// <summary> 
        /// Called by the action list to move a field left on the GridView
        /// </summary> 
        internal void MoveLeft() { 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor;
                InvokeTransactedChange(Component, new TransactedChangeCallback(MoveLeftCallback), null, SR.GetString(SR.GridView_MoveLeftTransaction));
                UpdateDesignTimeHtml();
            } 
            finally {
                Cursor.Current = originalCursor; 
            } 

        } 

        /// <devdoc>
        /// Transacted change callback to move a field left.
        /// </devdoc> 
        private bool MoveLeftCallback(object context) {
            DataControlFieldCollection fields = ((GridView)Component).Columns; 
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
 
        /// <summary>
        /// Called by the action list to move a field right on the GridView
        /// </summary>
        internal void MoveRight() { 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(MoveRightCallback), null, SR.GetString(SR.GridView_MoveRightTransaction));
                UpdateDesignTimeHtml(); 
            }
            finally {
                Cursor.Current = originalCursor;
            } 
        }
 
        /// <devdoc> 
        /// Transacted change callback to move a field right.
        /// </devdoc> 
        private bool MoveRightCallback(object context) {
            DataControlFieldCollection fields = ((GridView)Component).Columns;
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

        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.OnClick"]/*' /> 
        protected override void OnClick(DesignerRegionMouseEventArgs e) { 
            if (e.Region != null) {
                SelectedFieldIndex = (int)e.Region.UserData; 
                UpdateDesignTimeHtml();
            }
        }
 
        /// <devdoc>
        ///   Adds the designer region attributes on all cells when GetDesignTimeHtml is called. 
        /// </devdoc> 
        private void OnRowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e) {
            GridViewRow row = e.Row; 

            if (row.RowType == DataControlRowType.DataRow ||
                row.RowType == DataControlRowType.Header ||
                row.RowType == DataControlRowType.Footer) { 
                int columnsCount = ((GridView)sender).Columns.Count;
                int autoGenButtonRows = 0; 
 
                if (((GridView)sender).AutoGenerateDeleteButton || ((GridView)sender).AutoGenerateEditButton || ((GridView)sender).AutoGenerateSelectButton) {
                    autoGenButtonRows = 1; 
                }

                for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++) {
                    System.Web.UI.WebControls.TableCell cell = row.Cells[columnIndex + autoGenButtonRows]; 
                    cell.Attributes[DesignerRegion.DesignerRegionAttributeName] = _regionCount.ToString(NumberFormatInfo.InvariantInfo);
                    _regionCount++; 
                } 
            }
 
            return;
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
                InvokeTransactedChange(Component, new TransactedChangeCallback(SchemaRefreshedCallback), null, SR.GetString(SR.GridView_SchemaRefreshedTransaction));
                UpdateDesignTimeHtml();
            } 
            finally {
                Cursor.Current = originalCursor; 
            } 
        }
 
        /// <include file='doc\GridViewDesigner.uex' path='docs/doc[@for="GridViewDesigner.PreFilterProperties"]/*' />
        /// <summary>
        /// Overridden by the designer to shadow various runtime properties
        /// with corresponding properties that it implements. 
        /// </summary>
        /// <param name="properties"> 
        /// The properties to be filtered. 
        /// </param>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);

            // remove the browsable attribute of Columns in template mode so you can't bring up
            // the dialog and delete the column that's in template mode 
            if (InTemplateMode) {
                PropertyDescriptor fieldsProp = (PropertyDescriptor)properties["Columns"]; 
                properties["Columns"] = 
                    TypeDescriptor.CreateProperty(fieldsProp.ComponentType, fieldsProp, BrowsableAttribute.No);
            } 
        }

        /// <summary>
        /// Called by the action list to remove a field on the GridView 
        /// </summary>
        internal void RemoveField() { 
            Cursor originalCursor = Cursor.Current; 
            try {
                Cursor.Current = Cursors.WaitCursor; 
                InvokeTransactedChange(Component, new TransactedChangeCallback(RemoveFieldCallback), null, SR.GetString(SR.GridView_RemoveFieldTransaction));
                UpdateDesignTimeHtml();
            }
            finally { 
                Cursor.Current = originalCursor;
            } 
        } 

        /// <devdoc> 
        /// Transacted change callback to remove a field.
        /// </devdoc>
        private bool RemoveFieldCallback(object context) {
            int selectedFieldIndex = SelectedFieldIndex; 
            if (selectedFieldIndex >= 0) {
                ((GridView)Component).Columns.RemoveAt(selectedFieldIndex); 
                if (selectedFieldIndex == ((GridView)Component).Columns.Count) { 
                    SelectedFieldIndex--;
                    UpdateDesignTimeHtml(); 
                }
                return true;
            }
            return false; 
        }
 
        /// <devdoc> 
        /// Saves the changed states of the checkboxes and alters the settings of the control to match.
        /// </devdoc> 
        private void SaveManipulationSetting(ManipulationMode mode, bool newState) {
            DataControlFieldCollection fields = ((GridView)Component).Columns;
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
                    case ManipulationMode.Select: 
                        commandField.ShowSelectButton = newState;
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
                case ManipulationMode.Select:
                    commandField.ShowSelectButton = newState; 
                    break;
                } 
                fields.Insert(0, commandField); 
            }
 

            PropertyDescriptor propDesc;
            if (!newState) {
                GridView gridView = ((GridView)Component); 
                switch (mode) {
                    case ManipulationMode.Edit: 
                        propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AutoGenerateEditButton"]; 
                        propDesc.SetValue(Component, newState);
                        break; 
                    case ManipulationMode.Delete:
                        propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AutoGenerateDeleteButton"];
                        propDesc.SetValue(Component, newState);
                        break; 
                    case ManipulationMode.Select:
                        propDesc = TypeDescriptor.GetProperties(typeof(GridView))["AutoGenerateSelectButton"]; 
                        propDesc.SetValue(Component, newState); 
                        break;
                } 
            }
        }

        /// <devdoc> 
        /// Transacted change callback for SchemaRefreshed event.
        /// </devdoc> 
        private bool SchemaRefreshedCallback(object context) { 
            IDataSourceViewSchema schema = GetDataSourceSchema();
            if (DataSourceID.Length > 0 && schema != null) { 
                if (((GridView)Component).Columns.Count > 0 || ((GridView)Component).DataKeyNames.Length > 0) {
                    // warn that we're going to obliterate the fields you have
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site,
                                                SR.GetString(SR.DataBoundControl_SchemaRefreshedWarning, SR.GetString(SR.DataBoundControl_GridView), SR.GetString(SR.DataBoundControl_Column)), 
                                                SR.GetString(SR.DataBoundControl_SchemaRefreshedCaption, ((GridView)Component).ID),
                                                MessageBoxButtons.YesNo)) { 
                        ((GridView)Component).DataKeyNames = new string[0]; 
                        ((GridView)Component).Columns.Clear();
                        SelectedFieldIndex = -1;   //  we don't want a design-time html update yet. 
                        AddKeysAndBoundFields(schema);
                    }
                }
                else { 
                    // just ask if we should generate new ones, since you don't have any now
                    AddKeysAndBoundFields(schema); 
                } 
            }
            else {  // either the DataSourceID property was set to "" or we don't have schema 
                if (((GridView)Component).Columns.Count > 0 || ((GridView)Component).DataKeyNames.Length > 0) {
                    // ask if we can clear your fields/keys
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site,
                                                                       SR.GetString(SR.DataBoundControl_SchemaRefreshedWarningNoDataSource, SR.GetString(SR.DataBoundControl_GridView), SR.GetString(SR.DataBoundControl_Column)), 
                                                                       SR.GetString(SR.DataBoundControl_SchemaRefreshedCaption, ((GridView)Component).ID),
                                                                       MessageBoxButtons.YesNo)) { 
                        ((GridView)Component).DataKeyNames = new string[0]; 
                        ((GridView)Component).Columns.Clear();
                        SelectedFieldIndex = -1;   // we don't want a design-time update yet. 
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

        /// <devdoc> 
        /// Gets the curent states of the fields collection.
        /// </devdoc> 
        private void UpdateFieldsCurrentState() { 
            _currentSelectState = ((GridView)Component).AutoGenerateSelectButton;
            _currentEditState = ((GridView)Component).AutoGenerateEditButton; 
            _currentDeleteState = ((GridView)Component).AutoGenerateDeleteButton;

            foreach (DataControlField field in ((GridView)Component).Columns) {
                CommandField commandField = field as CommandField; 
                if (commandField != null) {
                    if (commandField.ShowSelectButton) { 
                        _currentSelectState = true; 
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
