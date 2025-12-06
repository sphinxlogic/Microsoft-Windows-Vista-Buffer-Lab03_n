//------------------------------------------------------------------------------ 
// <copyright file="FormViewDesigner.cs" company="Microsoft">
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
    using System.Text; 
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    using Table = System.Web.UI.WebControls.Table; 
    using TableRow = System.Web.UI.WebControls.TableRow; 

    /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner"]/*' /> 
    /// <summary>
    /// FormViewDesigner is the designer associated with a
    /// FormView.
    /// </summary> 
    public class FormViewDesigner : DataBoundControlDesigner {
 
        private static DesignerAutoFormatCollection _autoFormats; 

        private static string[] _controlTemplateNames = new string[] { 
            "ItemTemplate",
            "FooterTemplate",
            "EditItemTemplate",
            "InsertItemTemplate", 
            "HeaderTemplate",
            "EmptyDataTemplate", 
            "PagerTemplate" 
        };
 
        private static bool[] _controlTemplateSupportsDataBinding = new bool[] {
            true,
            true,
            true, 
            true,
            true, 
            true, 
            true
        }; 

        private const int IDX_CONTROL_HEADER_TEMPLATE = 4;
        private const int IDX_CONTROL_ITEM_TEMPLATE = 0;
        private const int IDX_CONTROL_EDITITEM_TEMPLATE = 2; 
        private const int IDX_CONTROL_INSERTITEM_TEMPLATE = 3;
        private const int IDX_CONTROL_FOOTER_TEMPLATE = 1; 
        private const int IDX_CONTROL_EMPTY_DATA_TEMPLATE = 5; 
        private const int IDX_CONTROL_PAGER_TEMPLATE = 6;
 
        private const string itemTemplateFieldString = "{0}: <asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{2}Label\" /><br />";
        private const string keyItemTemplateFieldString = "{0}: <asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{2}Label\" /><br />";
        private const string boolItemTemplateFieldString = "{0}: <asp:CheckBox Checked='<%# {1} %>' runat=\"server\" id=\"{2}CheckBox\" Enabled=\"false\" /><br />";
 
        private const string editItemTemplateFieldString = "{0}: <asp:TextBox Text='<%# {1} %>' runat=\"server\" id=\"{2}TextBox\" /><br />";
        private const string boolEditItemTemplateFieldString = "{0}: <asp:CheckBox Checked='<%# {1} %>' runat=\"server\" id=\"{2}CheckBox\" /><br />"; 
        private const string keyEditItemTemplateFieldString = "{0}: <asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{2}Label1\" /><br />"; 

        private const string insertItemTemplateFieldString = "{0}: <asp:TextBox Text='<%# {1} %>' runat=\"server\" id=\"{2}TextBox\" /><br />"; 
        private const string boolInsertItemTemplateFieldString = "{0}: <asp:CheckBox Checked='<%# {1} %>' runat=\"server\" id=\"{2}CheckBox\" /><br />";

        private const string templateButtonString = "<asp:LinkButton runat=\"server\" Text=\"{3}\" CommandName=\"{0}\" id=\"{1}{0}Button\" CausesValidation=\"{2}\" />";
 
        private const string nonBreakingSpace = "&nbsp;";
 
        private FormViewActionList _actionLists; 

        /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner.ActionLists"]/*' /> 
        /// <summary>
        /// Adds designer actions to the ActionLists collection.
        /// </summary>
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists); 

                if (_actionLists == null) { 
                    _actionLists = new FormViewActionList(this);
                }
                bool inTemplateMode = InTemplateMode;
 
                DesignerDataSourceView view = DesignerView;
                // in the future, these will also look at the DataSourceDesigner to figure out 
                // if they should be enabled 
                _actionLists.AllowPaging = !inTemplateMode && view != null;
 
                actionLists.Add(_actionLists);
                return actionLists;
            }
        } 

        public override DesignerAutoFormatCollection AutoFormats { 
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.FORMVIEW_SCHEMES, 
                        delegate(DataRow schemeData) { return new FormViewAutoFormat(schemeData); });
                }
                return _autoFormats;
            } 
        }
 
        private bool CurrentModeTemplateExists { 
            get {
                ITemplate itemTemplate = null; 
                if (((FormView)ViewControl).CurrentMode == FormViewMode.ReadOnly) {
                    itemTemplate = ((FormView)ViewControl).ItemTemplate;
                }
                if (((FormView)ViewControl).CurrentMode == FormViewMode.Insert) { 
                    itemTemplate = ((FormView)ViewControl).InsertItemTemplate;
                } 
                if (((FormView)ViewControl).CurrentMode == FormViewMode.Edit || (((FormView)ViewControl).CurrentMode == FormViewMode.Insert && itemTemplate == null)) { 
                    itemTemplate = ((FormView)ViewControl).EditItemTemplate;
                } 

                if (itemTemplate != null) {
                    IDesignerHost host = (IDesignerHost)ViewControl.Site.GetService(typeof(IDesignerHost));
                    string templateText = ControlPersister.PersistTemplate(itemTemplate, host); 
                    return templateText != null && templateText.Length > 0;
                } 
                return false; 
            }
        } 

        /// <summary>
        /// Called by the action list to enable paging on the FormView
        /// </summary> 
        internal bool EnablePaging {
            get { 
                return ((FormView)Component).AllowPaging; 
            }
            set { 
                Cursor originalCursor = Cursor.Current;
                try {
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnablePagingCallback), value, SR.GetString(SR.FormView_EnablePagingTransaction)); 
                }
                finally { 
                    Cursor.Current = originalCursor; 
                }
            } 
        }

        /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner.SampleRowCount"]/*' />
        /// <summary> 
        /// Used to determine the number of sample rows the control wishes to show.
        /// </summary> 
        protected override int SampleRowCount{ 
            get {
                return 2;   //one to show, one for paging 
            }
        }

        /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner.TemplateGroups"]/*' /> 
        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups; 

                // don't cache the template groups because the styles might have changed. 
                for (int i = 0; i < _controlTemplateNames.Length; i++) {
                    string templateName = _controlTemplateNames[i];

                    TemplateGroup group = new TemplateGroup(_controlTemplateNames[i], GetTemplateStyle(i)); 
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
        private void AddTemplatesAndKeys(IDataSourceViewSchema schema) { 
            StringBuilder editItemTemplateStringBuilder = new StringBuilder(); 
            StringBuilder itemTemplateStringBuilder = new StringBuilder();
            StringBuilder insertItemTemplateStringBuilder = new StringBuilder(); 
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));

            Debug.Assert(schema != null, "Did not expect null schema in AddTemplatesAndKeys");
            if (schema != null) { 
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                    ArrayList keys = new ArrayList(); 
                    foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) {
                        string fieldName = fieldSchema.Name; 
                        char[] fieldDerivedID = new char[fieldName.Length];

                        for (int i = 0; i < fieldName.Length; i++) {
                            char currentChar = fieldName[i]; 
                            if (Char.IsLetterOrDigit(currentChar) || currentChar == '_') {
                                fieldDerivedID[i] = currentChar; 
                            } 
                            else {
                                fieldDerivedID[i] = '_'; 
                            }
                        }
                        string fieldDerivedIDString = new String(fieldDerivedID);
                        string evalExpression = DesignTimeDataBinding.CreateEvalExpression(fieldName, String.Empty); 
                        string bindExpression = DesignTimeDataBinding.CreateBindExpression(fieldName, String.Empty);
 
                        if (fieldSchema.PrimaryKey || fieldSchema.Identity) { 
                            editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, keyEditItemTemplateFieldString, fieldName, evalExpression, fieldDerivedIDString));
                            itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, keyItemTemplateFieldString, fieldName, evalExpression, fieldDerivedIDString)); 
                            if (!fieldSchema.Identity) {
                                // you can insert a key but not an identity
                                insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, insertItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString));
                            } 
                        }
                        else { 
                            if (fieldSchema.DataType == typeof(bool) || 
                                fieldSchema.DataType == typeof(bool?)) {
                                editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, boolEditItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString)); 
                                itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, boolItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString));
                                insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, boolInsertItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString));
                            }
                            else { 
                                editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, editItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString));
                                itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, itemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString)); 
                                insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, insertItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString)); 
                            }
                        } 
                        editItemTemplateStringBuilder.Append(Environment.NewLine);
                        itemTemplateStringBuilder.Append(Environment.NewLine);
                        insertItemTemplateStringBuilder.Append(Environment.NewLine);
 
                        if (fieldSchema.PrimaryKey) {
                            keys.Add(fieldName); 
                        } 
                    }
                    bool firstLink = true; 
                    if (DesignerView.CanUpdate) {
                        itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.EditCommandName, String.Empty, Boolean.FalseString, SR.GetString(SR.FormView_Edit)));
                        firstLink = false;
                    } 
                    editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.UpdateCommandName, String.Empty, Boolean.TrueString, SR.GetString(SR.FormView_Update)));
                    editItemTemplateStringBuilder.Append(nonBreakingSpace); 
                    editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.CancelCommandName, DataControlCommands.UpdateCommandName, Boolean.FalseString, SR.GetString(SR.FormView_Cancel))); 

                    if (DesignerView.CanDelete) { 
                        if (!firstLink) {
                            itemTemplateStringBuilder.Append(nonBreakingSpace);
                        }
                        itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.DeleteCommandName, String.Empty, Boolean.FalseString, SR.GetString(SR.FormView_Delete))); 
                        firstLink = false;
                    } 
                    if (DesignerView.CanInsert) { 
                        if (!firstLink) {
                            itemTemplateStringBuilder.Append(nonBreakingSpace); 
                        }
                        itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.NewCommandName, String.Empty, Boolean.FalseString, SR.GetString(SR.FormView_New)));
                    }
                    insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.InsertCommandName, String.Empty, Boolean.TrueString, SR.GetString(SR.FormView_Insert))); 
                    insertItemTemplateStringBuilder.Append(nonBreakingSpace);
                    insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.CancelCommandName, DataControlCommands.InsertCommandName, Boolean.FalseString, SR.GetString(SR.FormView_Cancel))); 
 
                    editItemTemplateStringBuilder.Append(Environment.NewLine);
                    itemTemplateStringBuilder.Append(Environment.NewLine); 
                    insertItemTemplateStringBuilder.Append(Environment.NewLine);

                    try {
                        // if a schema field is not a valid identifier, this will fail.  It should fail silently. 
                        ((FormView)Component).EditItemTemplate = ControlParser.ParseTemplate(host, editItemTemplateStringBuilder.ToString());
                        ((FormView)Component).ItemTemplate = ControlParser.ParseTemplate(host, itemTemplateStringBuilder.ToString()); 
                        ((FormView)Component).InsertItemTemplate = ControlParser.ParseTemplate(host, insertItemTemplateStringBuilder.ToString());; 
                    }
                    catch { 
                    }

                    int keyCount = keys.Count;
                    if (keyCount > 0) { 
                        string[] dataKeys = new string[keyCount];
                        keys.CopyTo(dataKeys, 0); 
                        ((FormView)Component).DataKeyNames = dataKeys; 
                    }
                } 
            }
        }

        /// <devdoc> 
        /// Transacted change callback to enable or disable paging.
        /// </devdoc> 
        private bool EnablePagingCallback(object context) { 
            bool currentPageState = ((FormView)Component).AllowPaging;
            bool setEnabled = !currentPageState; 
            if (context is bool) {
                setEnabled = (bool)context;
            }
 
            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(FormView))["AllowPaging"];
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

        /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner.GetDesignTimeHtml"]/*' />
        /// <summary> 
        /// Provides the design-time HTML used to display the control
        /// at design-time. 
        /// </summary> 
        /// <returns>
        /// The HTML used to render the control at design-time. 
        /// </returns>
        public override string GetDesignTimeHtml() {
            FormView dv = (FormView)ViewControl;
            bool oldDataKeyChanged = false; 
            string[] oldDataKeyNames = null;
            string designTimeHTML = null; 
 
            if (CurrentModeTemplateExists) {
                bool hasSchema = false; 
                IDataSourceViewSchema schema = GetDataSourceSchema();
                if (schema != null) {
                    IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                    if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                        hasSchema = true;
                    } 
                } 
                try {
                    if (!hasSchema) { 
                        oldDataKeyNames = dv.DataKeyNames;
                        dv.DataKeyNames = new string[0];
                        oldDataKeyChanged = true;
                    } 

                    // refresh the TypeDescriptor so that PreFilterProperties gets called 
                    TypeDescriptor.Refresh(this.Component); 

                    designTimeHTML = base.GetDesignTimeHtml(); 
                }
                finally {
                    if (oldDataKeyChanged) {
                        dv.DataKeyNames = oldDataKeyNames; 
                    }
                } 
            } 
            else {
                designTimeHTML = GetEmptyDesignTimeHtml(); 
            }
            return designTimeHTML;
        }
 
        /// <devdoc>
        /// </devdoc> 
        protected override string GetEmptyDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.DataList_NoTemplatesInst));  // todo
        } 

        private Style GetTemplateStyle(int templateIndex) {
            Style style = new Style();
            style.CopyFrom(((FormView)ViewControl).ControlStyle); 

            switch (templateIndex) { 
                case IDX_CONTROL_HEADER_TEMPLATE: 
                    style.CopyFrom(((FormView)ViewControl).HeaderStyle);
                    break; 
                case IDX_CONTROL_ITEM_TEMPLATE:
                    style.CopyFrom(((FormView)ViewControl).RowStyle);
                    break;
                case IDX_CONTROL_EDITITEM_TEMPLATE: 
                    style.CopyFrom(((FormView)ViewControl).RowStyle);
                    style.CopyFrom(((FormView)ViewControl).EditRowStyle); 
                    break; 
                case IDX_CONTROL_INSERTITEM_TEMPLATE:
                    style.CopyFrom(((FormView)ViewControl).RowStyle); 
                    style.CopyFrom(((FormView)ViewControl).InsertRowStyle);
                    break;
                case IDX_CONTROL_FOOTER_TEMPLATE:
                    style.CopyFrom(((FormView)ViewControl).FooterStyle); 
                    break;
                case IDX_CONTROL_EMPTY_DATA_TEMPLATE: 
                    style.CopyFrom(((FormView)ViewControl).EmptyDataRowStyle); 
                    break;
                case IDX_CONTROL_PAGER_TEMPLATE: 
                    style.CopyFrom(((FormView)ViewControl).PagerStyle);
                    break;
            }
 
            return style;
        } 
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(FormView)); 
            base.Initialize(component);

            if (View != null) {
                View.SetFlags(ViewFlags.TemplateEditing, true); 
            }
        } 
 
        /// <devdoc>
        /// Override to execute custom actions when the schema is refreshed. 
        /// </devdoc>
        protected override void OnSchemaRefreshed() {
            if (InTemplateMode) {
                // We ignore the SchemaRefreshed event if we are in template 
                // editing mode since the designer won't reflect the changes.
                return; 
            } 

            InvokeTransactedChange(Component, new TransactedChangeCallback(SchemaRefreshedCallback), null, SR.GetString(SR.DataControls_SchemaRefreshedTransaction)); 
        }

        /// <devdoc>
        /// Transacted change callback for the SchemaRefreshed event. 
        /// </devdoc>
        private bool SchemaRefreshedCallback(object context) { 
            IDataSourceViewSchema schema = GetDataSourceSchema(); 
            if (DataSourceID.Length > 0 && schema != null) {
                if (((FormView)Component).DataKeyNames.Length > 0 || ((FormView)Component).ItemTemplate != null || ((FormView)Component).EditItemTemplate != null) { 
                    // warn that we're going to obliterate the fields you have
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site,
                                                SR.GetString(SR.FormView_SchemaRefreshedWarning),
                                                SR.GetString(SR.FormView_SchemaRefreshedCaption, ((FormView)Component).ID), 
                                                MessageBoxButtons.YesNo)) {
                        ((FormView)Component).DataKeyNames = new string[0]; 
                        AddTemplatesAndKeys(schema); 
                    }
                } 
                else {
                    // just ask if we should generate new ones, since you don't have any now
                    AddTemplatesAndKeys(schema);
                } 
            }
            else {  // either the DataSourceID property was set to "" or we don't have schema 
                if (((FormView)Component).DataKeyNames.Length > 0 || ((FormView)Component).ItemTemplate != null || ((FormView)Component).EditItemTemplate != null) { 
                    // ask if we can clear your fields/keys
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site, 
                                                                       SR.GetString(SR.FormView_SchemaRefreshedWarningNoDataSource),
                                                                       SR.GetString(SR.FormView_SchemaRefreshedCaption, ((FormView)Component).ID),
                                                                       MessageBoxButtons.YesNo)) {
                        ((FormView)Component).DataKeyNames = new string[0]; 
                        ((FormView)Component).ItemTemplate = null;
                        ((FormView)Component).InsertItemTemplate = null; 
                        ((FormView)Component).EditItemTemplate = null; 
                    }
                } 
            }
            UpdateDesignTimeHtml();
            return true;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="FormViewDesigner.cs" company="Microsoft">
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
    using System.Text; 
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    using Table = System.Web.UI.WebControls.Table; 
    using TableRow = System.Web.UI.WebControls.TableRow; 

    /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner"]/*' /> 
    /// <summary>
    /// FormViewDesigner is the designer associated with a
    /// FormView.
    /// </summary> 
    public class FormViewDesigner : DataBoundControlDesigner {
 
        private static DesignerAutoFormatCollection _autoFormats; 

        private static string[] _controlTemplateNames = new string[] { 
            "ItemTemplate",
            "FooterTemplate",
            "EditItemTemplate",
            "InsertItemTemplate", 
            "HeaderTemplate",
            "EmptyDataTemplate", 
            "PagerTemplate" 
        };
 
        private static bool[] _controlTemplateSupportsDataBinding = new bool[] {
            true,
            true,
            true, 
            true,
            true, 
            true, 
            true
        }; 

        private const int IDX_CONTROL_HEADER_TEMPLATE = 4;
        private const int IDX_CONTROL_ITEM_TEMPLATE = 0;
        private const int IDX_CONTROL_EDITITEM_TEMPLATE = 2; 
        private const int IDX_CONTROL_INSERTITEM_TEMPLATE = 3;
        private const int IDX_CONTROL_FOOTER_TEMPLATE = 1; 
        private const int IDX_CONTROL_EMPTY_DATA_TEMPLATE = 5; 
        private const int IDX_CONTROL_PAGER_TEMPLATE = 6;
 
        private const string itemTemplateFieldString = "{0}: <asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{2}Label\" /><br />";
        private const string keyItemTemplateFieldString = "{0}: <asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{2}Label\" /><br />";
        private const string boolItemTemplateFieldString = "{0}: <asp:CheckBox Checked='<%# {1} %>' runat=\"server\" id=\"{2}CheckBox\" Enabled=\"false\" /><br />";
 
        private const string editItemTemplateFieldString = "{0}: <asp:TextBox Text='<%# {1} %>' runat=\"server\" id=\"{2}TextBox\" /><br />";
        private const string boolEditItemTemplateFieldString = "{0}: <asp:CheckBox Checked='<%# {1} %>' runat=\"server\" id=\"{2}CheckBox\" /><br />"; 
        private const string keyEditItemTemplateFieldString = "{0}: <asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{2}Label1\" /><br />"; 

        private const string insertItemTemplateFieldString = "{0}: <asp:TextBox Text='<%# {1} %>' runat=\"server\" id=\"{2}TextBox\" /><br />"; 
        private const string boolInsertItemTemplateFieldString = "{0}: <asp:CheckBox Checked='<%# {1} %>' runat=\"server\" id=\"{2}CheckBox\" /><br />";

        private const string templateButtonString = "<asp:LinkButton runat=\"server\" Text=\"{3}\" CommandName=\"{0}\" id=\"{1}{0}Button\" CausesValidation=\"{2}\" />";
 
        private const string nonBreakingSpace = "&nbsp;";
 
        private FormViewActionList _actionLists; 

        /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner.ActionLists"]/*' /> 
        /// <summary>
        /// Adds designer actions to the ActionLists collection.
        /// </summary>
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists); 

                if (_actionLists == null) { 
                    _actionLists = new FormViewActionList(this);
                }
                bool inTemplateMode = InTemplateMode;
 
                DesignerDataSourceView view = DesignerView;
                // in the future, these will also look at the DataSourceDesigner to figure out 
                // if they should be enabled 
                _actionLists.AllowPaging = !inTemplateMode && view != null;
 
                actionLists.Add(_actionLists);
                return actionLists;
            }
        } 

        public override DesignerAutoFormatCollection AutoFormats { 
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.FORMVIEW_SCHEMES, 
                        delegate(DataRow schemeData) { return new FormViewAutoFormat(schemeData); });
                }
                return _autoFormats;
            } 
        }
 
        private bool CurrentModeTemplateExists { 
            get {
                ITemplate itemTemplate = null; 
                if (((FormView)ViewControl).CurrentMode == FormViewMode.ReadOnly) {
                    itemTemplate = ((FormView)ViewControl).ItemTemplate;
                }
                if (((FormView)ViewControl).CurrentMode == FormViewMode.Insert) { 
                    itemTemplate = ((FormView)ViewControl).InsertItemTemplate;
                } 
                if (((FormView)ViewControl).CurrentMode == FormViewMode.Edit || (((FormView)ViewControl).CurrentMode == FormViewMode.Insert && itemTemplate == null)) { 
                    itemTemplate = ((FormView)ViewControl).EditItemTemplate;
                } 

                if (itemTemplate != null) {
                    IDesignerHost host = (IDesignerHost)ViewControl.Site.GetService(typeof(IDesignerHost));
                    string templateText = ControlPersister.PersistTemplate(itemTemplate, host); 
                    return templateText != null && templateText.Length > 0;
                } 
                return false; 
            }
        } 

        /// <summary>
        /// Called by the action list to enable paging on the FormView
        /// </summary> 
        internal bool EnablePaging {
            get { 
                return ((FormView)Component).AllowPaging; 
            }
            set { 
                Cursor originalCursor = Cursor.Current;
                try {
                    Cursor.Current = Cursors.WaitCursor;
                    InvokeTransactedChange(Component, new TransactedChangeCallback(EnablePagingCallback), value, SR.GetString(SR.FormView_EnablePagingTransaction)); 
                }
                finally { 
                    Cursor.Current = originalCursor; 
                }
            } 
        }

        /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner.SampleRowCount"]/*' />
        /// <summary> 
        /// Used to determine the number of sample rows the control wishes to show.
        /// </summary> 
        protected override int SampleRowCount{ 
            get {
                return 2;   //one to show, one for paging 
            }
        }

        /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner.TemplateGroups"]/*' /> 
        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups; 

                // don't cache the template groups because the styles might have changed. 
                for (int i = 0; i < _controlTemplateNames.Length; i++) {
                    string templateName = _controlTemplateNames[i];

                    TemplateGroup group = new TemplateGroup(_controlTemplateNames[i], GetTemplateStyle(i)); 
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
        private void AddTemplatesAndKeys(IDataSourceViewSchema schema) { 
            StringBuilder editItemTemplateStringBuilder = new StringBuilder(); 
            StringBuilder itemTemplateStringBuilder = new StringBuilder();
            StringBuilder insertItemTemplateStringBuilder = new StringBuilder(); 
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));

            Debug.Assert(schema != null, "Did not expect null schema in AddTemplatesAndKeys");
            if (schema != null) { 
                IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                    ArrayList keys = new ArrayList(); 
                    foreach (IDataSourceFieldSchema fieldSchema in fieldSchemas) {
                        string fieldName = fieldSchema.Name; 
                        char[] fieldDerivedID = new char[fieldName.Length];

                        for (int i = 0; i < fieldName.Length; i++) {
                            char currentChar = fieldName[i]; 
                            if (Char.IsLetterOrDigit(currentChar) || currentChar == '_') {
                                fieldDerivedID[i] = currentChar; 
                            } 
                            else {
                                fieldDerivedID[i] = '_'; 
                            }
                        }
                        string fieldDerivedIDString = new String(fieldDerivedID);
                        string evalExpression = DesignTimeDataBinding.CreateEvalExpression(fieldName, String.Empty); 
                        string bindExpression = DesignTimeDataBinding.CreateBindExpression(fieldName, String.Empty);
 
                        if (fieldSchema.PrimaryKey || fieldSchema.Identity) { 
                            editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, keyEditItemTemplateFieldString, fieldName, evalExpression, fieldDerivedIDString));
                            itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, keyItemTemplateFieldString, fieldName, evalExpression, fieldDerivedIDString)); 
                            if (!fieldSchema.Identity) {
                                // you can insert a key but not an identity
                                insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, insertItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString));
                            } 
                        }
                        else { 
                            if (fieldSchema.DataType == typeof(bool) || 
                                fieldSchema.DataType == typeof(bool?)) {
                                editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, boolEditItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString)); 
                                itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, boolItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString));
                                insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, boolInsertItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString));
                            }
                            else { 
                                editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, editItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString));
                                itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, itemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString)); 
                                insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, insertItemTemplateFieldString, fieldName, bindExpression, fieldDerivedIDString)); 
                            }
                        } 
                        editItemTemplateStringBuilder.Append(Environment.NewLine);
                        itemTemplateStringBuilder.Append(Environment.NewLine);
                        insertItemTemplateStringBuilder.Append(Environment.NewLine);
 
                        if (fieldSchema.PrimaryKey) {
                            keys.Add(fieldName); 
                        } 
                    }
                    bool firstLink = true; 
                    if (DesignerView.CanUpdate) {
                        itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.EditCommandName, String.Empty, Boolean.FalseString, SR.GetString(SR.FormView_Edit)));
                        firstLink = false;
                    } 
                    editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.UpdateCommandName, String.Empty, Boolean.TrueString, SR.GetString(SR.FormView_Update)));
                    editItemTemplateStringBuilder.Append(nonBreakingSpace); 
                    editItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.CancelCommandName, DataControlCommands.UpdateCommandName, Boolean.FalseString, SR.GetString(SR.FormView_Cancel))); 

                    if (DesignerView.CanDelete) { 
                        if (!firstLink) {
                            itemTemplateStringBuilder.Append(nonBreakingSpace);
                        }
                        itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.DeleteCommandName, String.Empty, Boolean.FalseString, SR.GetString(SR.FormView_Delete))); 
                        firstLink = false;
                    } 
                    if (DesignerView.CanInsert) { 
                        if (!firstLink) {
                            itemTemplateStringBuilder.Append(nonBreakingSpace); 
                        }
                        itemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.NewCommandName, String.Empty, Boolean.FalseString, SR.GetString(SR.FormView_New)));
                    }
                    insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.InsertCommandName, String.Empty, Boolean.TrueString, SR.GetString(SR.FormView_Insert))); 
                    insertItemTemplateStringBuilder.Append(nonBreakingSpace);
                    insertItemTemplateStringBuilder.Append(String.Format(CultureInfo.InvariantCulture, templateButtonString, DataControlCommands.CancelCommandName, DataControlCommands.InsertCommandName, Boolean.FalseString, SR.GetString(SR.FormView_Cancel))); 
 
                    editItemTemplateStringBuilder.Append(Environment.NewLine);
                    itemTemplateStringBuilder.Append(Environment.NewLine); 
                    insertItemTemplateStringBuilder.Append(Environment.NewLine);

                    try {
                        // if a schema field is not a valid identifier, this will fail.  It should fail silently. 
                        ((FormView)Component).EditItemTemplate = ControlParser.ParseTemplate(host, editItemTemplateStringBuilder.ToString());
                        ((FormView)Component).ItemTemplate = ControlParser.ParseTemplate(host, itemTemplateStringBuilder.ToString()); 
                        ((FormView)Component).InsertItemTemplate = ControlParser.ParseTemplate(host, insertItemTemplateStringBuilder.ToString());; 
                    }
                    catch { 
                    }

                    int keyCount = keys.Count;
                    if (keyCount > 0) { 
                        string[] dataKeys = new string[keyCount];
                        keys.CopyTo(dataKeys, 0); 
                        ((FormView)Component).DataKeyNames = dataKeys; 
                    }
                } 
            }
        }

        /// <devdoc> 
        /// Transacted change callback to enable or disable paging.
        /// </devdoc> 
        private bool EnablePagingCallback(object context) { 
            bool currentPageState = ((FormView)Component).AllowPaging;
            bool setEnabled = !currentPageState; 
            if (context is bool) {
                setEnabled = (bool)context;
            }
 
            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(FormView))["AllowPaging"];
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

        /// <include file='doc\FormViewDesigner.uex' path='docs/doc[@for="FormViewDesigner.GetDesignTimeHtml"]/*' />
        /// <summary> 
        /// Provides the design-time HTML used to display the control
        /// at design-time. 
        /// </summary> 
        /// <returns>
        /// The HTML used to render the control at design-time. 
        /// </returns>
        public override string GetDesignTimeHtml() {
            FormView dv = (FormView)ViewControl;
            bool oldDataKeyChanged = false; 
            string[] oldDataKeyNames = null;
            string designTimeHTML = null; 
 
            if (CurrentModeTemplateExists) {
                bool hasSchema = false; 
                IDataSourceViewSchema schema = GetDataSourceSchema();
                if (schema != null) {
                    IDataSourceFieldSchema[] fieldSchemas = schema.GetFields();
                    if (fieldSchemas != null && fieldSchemas.Length > 0) { 
                        hasSchema = true;
                    } 
                } 
                try {
                    if (!hasSchema) { 
                        oldDataKeyNames = dv.DataKeyNames;
                        dv.DataKeyNames = new string[0];
                        oldDataKeyChanged = true;
                    } 

                    // refresh the TypeDescriptor so that PreFilterProperties gets called 
                    TypeDescriptor.Refresh(this.Component); 

                    designTimeHTML = base.GetDesignTimeHtml(); 
                }
                finally {
                    if (oldDataKeyChanged) {
                        dv.DataKeyNames = oldDataKeyNames; 
                    }
                } 
            } 
            else {
                designTimeHTML = GetEmptyDesignTimeHtml(); 
            }
            return designTimeHTML;
        }
 
        /// <devdoc>
        /// </devdoc> 
        protected override string GetEmptyDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.DataList_NoTemplatesInst));  // todo
        } 

        private Style GetTemplateStyle(int templateIndex) {
            Style style = new Style();
            style.CopyFrom(((FormView)ViewControl).ControlStyle); 

            switch (templateIndex) { 
                case IDX_CONTROL_HEADER_TEMPLATE: 
                    style.CopyFrom(((FormView)ViewControl).HeaderStyle);
                    break; 
                case IDX_CONTROL_ITEM_TEMPLATE:
                    style.CopyFrom(((FormView)ViewControl).RowStyle);
                    break;
                case IDX_CONTROL_EDITITEM_TEMPLATE: 
                    style.CopyFrom(((FormView)ViewControl).RowStyle);
                    style.CopyFrom(((FormView)ViewControl).EditRowStyle); 
                    break; 
                case IDX_CONTROL_INSERTITEM_TEMPLATE:
                    style.CopyFrom(((FormView)ViewControl).RowStyle); 
                    style.CopyFrom(((FormView)ViewControl).InsertRowStyle);
                    break;
                case IDX_CONTROL_FOOTER_TEMPLATE:
                    style.CopyFrom(((FormView)ViewControl).FooterStyle); 
                    break;
                case IDX_CONTROL_EMPTY_DATA_TEMPLATE: 
                    style.CopyFrom(((FormView)ViewControl).EmptyDataRowStyle); 
                    break;
                case IDX_CONTROL_PAGER_TEMPLATE: 
                    style.CopyFrom(((FormView)ViewControl).PagerStyle);
                    break;
            }
 
            return style;
        } 
 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(FormView)); 
            base.Initialize(component);

            if (View != null) {
                View.SetFlags(ViewFlags.TemplateEditing, true); 
            }
        } 
 
        /// <devdoc>
        /// Override to execute custom actions when the schema is refreshed. 
        /// </devdoc>
        protected override void OnSchemaRefreshed() {
            if (InTemplateMode) {
                // We ignore the SchemaRefreshed event if we are in template 
                // editing mode since the designer won't reflect the changes.
                return; 
            } 

            InvokeTransactedChange(Component, new TransactedChangeCallback(SchemaRefreshedCallback), null, SR.GetString(SR.DataControls_SchemaRefreshedTransaction)); 
        }

        /// <devdoc>
        /// Transacted change callback for the SchemaRefreshed event. 
        /// </devdoc>
        private bool SchemaRefreshedCallback(object context) { 
            IDataSourceViewSchema schema = GetDataSourceSchema(); 
            if (DataSourceID.Length > 0 && schema != null) {
                if (((FormView)Component).DataKeyNames.Length > 0 || ((FormView)Component).ItemTemplate != null || ((FormView)Component).EditItemTemplate != null) { 
                    // warn that we're going to obliterate the fields you have
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site,
                                                SR.GetString(SR.FormView_SchemaRefreshedWarning),
                                                SR.GetString(SR.FormView_SchemaRefreshedCaption, ((FormView)Component).ID), 
                                                MessageBoxButtons.YesNo)) {
                        ((FormView)Component).DataKeyNames = new string[0]; 
                        AddTemplatesAndKeys(schema); 
                    }
                } 
                else {
                    // just ask if we should generate new ones, since you don't have any now
                    AddTemplatesAndKeys(schema);
                } 
            }
            else {  // either the DataSourceID property was set to "" or we don't have schema 
                if (((FormView)Component).DataKeyNames.Length > 0 || ((FormView)Component).ItemTemplate != null || ((FormView)Component).EditItemTemplate != null) { 
                    // ask if we can clear your fields/keys
                    if (DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site, 
                                                                       SR.GetString(SR.FormView_SchemaRefreshedWarningNoDataSource),
                                                                       SR.GetString(SR.FormView_SchemaRefreshedCaption, ((FormView)Component).ID),
                                                                       MessageBoxButtons.YesNo)) {
                        ((FormView)Component).DataKeyNames = new string[0]; 
                        ((FormView)Component).ItemTemplate = null;
                        ((FormView)Component).InsertItemTemplate = null; 
                        ((FormView)Component).EditItemTemplate = null; 
                    }
                } 
            }
            UpdateDesignTimeHtml();
            return true;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
