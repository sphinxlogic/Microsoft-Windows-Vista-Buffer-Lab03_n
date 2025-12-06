//------------------------------------------------------------------------------ 
// <copyright file="DataListDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Design;
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.IO;
    using System.Text; 
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       Provides a designer for the <see cref='System.Web.UI.WebControls.DataList'/> 
    ///       control.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)]
    public class DataListDesigner : BaseDataListDesigner { 
 
        internal static TraceSwitch DataListDesignerSwitch =
            new TraceSwitch("DATALISTDESIGNER", "Enable DataList designer general purpose traces."); 

        private const string templateFieldString = "{0}: <asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{2}Label\"/><br />";
        private const string breakString = "<br />";
        private const int HeaderFooterTemplates = 1; 
        private const int ItemTemplates = 0;
        private const int SeparatorTemplate = 2; 
 
        private static string[] HeaderFooterTemplateNames = new string[] { "HeaderTemplate", "FooterTemplate" };
        private const int IDX_HEADER_TEMPLATE = 0; 
        private const int IDX_FOOTER_TEMPLATE = 1;

        private static string[] ItemTemplateNames = new String[] { "ItemTemplate", "AlternatingItemTemplate", "SelectedItemTemplate", "EditItemTemplate" };
        private const int IDX_ITEM_TEMPLATE = 0; 
        private const int IDX_ALTITEM_TEMPLATE = 1;
        private const int IDX_SELITEM_TEMPLATE = 2; 
        private const int IDX_EDITITEM_TEMPLATE = 3; 

        private static string[] SeparatorTemplateNames = new String[] { "SeparatorTemplate" }; 
        private const int IDX_SEPARATOR_TEMPLATE = 0;

#pragma warning disable 618
        private TemplateEditingVerb[] templateVerbs; 
#pragma warning restore 618
        private bool templateVerbsDirty; 
 
        private static DesignerAutoFormatCollection _autoFormats;
 

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.DataListDesigner"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.WebControls.DataListDesigner'/>.
        ///    </para> 
        /// </devdoc> 
        public DataListDesigner() {
            templateVerbsDirty = true; 
        }

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.AllowResize"]/*' />
        public override bool AllowResize { 
            get {
                // When templates are not defined, we render a read-only fixed 
                // size block. Once templates are defined or are being edited the control should allow 
                // resizing.
                return TemplatesExist || InTemplateModeInternal; 
            }
        }

        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.BDL_SCHEMES, 
                        delegate(DataRow schemeData) { return new DataListAutoFormat(schemeData); });
                } 
                return _autoFormats;
            }
        }
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.TemplatesExist"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets a value
        ///       indicating whether templates associated to the designer currently exist. 
        ///    </para>
        /// </devdoc>
        protected bool TemplatesExist {
            get { 
                DataList dataList = (DataList)ViewControl;
                ITemplate itemTemplate = dataList.ItemTemplate; 
                string templateText = null; 
                if (itemTemplate != null) {
                    templateText = GetTextFromTemplate(itemTemplate); 
                    return templateText != null && templateText.Length > 0;
                }
                return false;
            } 
        }
 
        private void CreateDefaultTemplate() { 
            string newTemplateText = String.Empty;
            StringBuilder sb = new StringBuilder(); 
            DataList dataList = (DataList)Component;

            IDataSourceViewSchema schema = GetDataSourceSchema();
            IDataSourceFieldSchema[] fieldSchemas = null; 

            if (schema != null) { 
                fieldSchemas = schema.GetFields(); 
            }
 
            if (fieldSchemas != null && fieldSchemas.Length > 0) {
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
 
                    sb.Append(String.Format(CultureInfo.InvariantCulture, templateFieldString, fieldName, DesignTimeDataBinding.CreateEvalExpression(fieldName, String.Empty), fieldDerivedIDString));
                    sb.Append(Environment.NewLine); 
                    if (fieldSchema.PrimaryKey && dataList.DataKeyField.Length == 0) {
                        dataList.DataKeyField = fieldName;
                    }
                } 
                sb.Append(breakString);
                sb.Append(Environment.NewLine); 
                newTemplateText = sb.ToString(); 
            }
 
            if (newTemplateText != null && newTemplateText.Length > 0) {
                try {
                    // if the schema has a field whose name is not a valid id, this may fail.
                    // It should fail silently. 
                    dataList.ItemTemplate = GetTemplateFromText(newTemplateText, dataList.ItemTemplate);
                } 
                catch { 
                }
            } 
        }

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.CreateTemplateEditingFrame"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected override ITemplateEditingFrame CreateTemplateEditingFrame(TemplateEditingVerb verb) {
            ITemplateEditingService teService = (ITemplateEditingService)GetService(typeof(ITemplateEditingService)); 
            Debug.Assert(teService != null, "How did we get this far without an ITemplateEditingService"); 
            DataList dataList = (DataList)ViewControl;
 
            string[] templateNames = null;
            Style[] templateStyles = null;

            switch (verb.Index) { 
                case HeaderFooterTemplates:
                    templateNames = HeaderFooterTemplateNames; 
                    templateStyles = new Style[] { dataList.HeaderStyle, dataList.FooterStyle }; 
                    break;
                case ItemTemplates: 
                    templateNames = ItemTemplateNames;
                    templateStyles = new Style[] { dataList.ItemStyle, dataList.AlternatingItemStyle, dataList.SelectedItemStyle, dataList.EditItemStyle };
                    break;
                case SeparatorTemplate: 
                    templateNames = SeparatorTemplateNames;
                    templateStyles = new Style[] { dataList.SeparatorStyle }; 
                    break; 
                default:
                    Debug.Fail("Unknown Index value on TemplateEditingVerb"); 
                    break;
            }

            ITemplateEditingFrame editingFrame = 
                teService.CreateFrame(this, verb.Text, templateNames, dataList.ControlStyle, templateStyles);
            return editingFrame; 
        } 

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Disposes of the resources (other than memory) used by the
        ///    <see cref='System.Web.UI.Design.WebControls.DataListDesigner'/>. 
        ///    </para>
        /// </devdoc> 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                DisposeTemplateVerbs(); 
            }

            base.Dispose(disposing);
        } 

        private void DisposeTemplateVerbs() { 
            if (templateVerbs != null) { 
                for (int i = 0; i < templateVerbs.Length; i++) {
                    templateVerbs[i].Dispose(); 
                }

                templateVerbs = null;
                templateVerbsDirty = true; 
            }
        } 
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetCachedTemplateEditingVerbs"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected override TemplateEditingVerb[] GetCachedTemplateEditingVerbs() {
            if (templateVerbsDirty == true) {
                DisposeTemplateVerbs();
 
                templateVerbs = new TemplateEditingVerb[3];
                templateVerbs[0] = new TemplateEditingVerb(SR.GetString(SR.DataList_ItemTemplates), ItemTemplates, this); 
                templateVerbs[1] = new TemplateEditingVerb(SR.GetString(SR.DataList_HeaderFooterTemplates), HeaderFooterTemplates, this); 
                templateVerbs[2] = new TemplateEditingVerb(SR.GetString(SR.DataList_SeparatorTemplate), SeparatorTemplate, this);
 
                templateVerbsDirty = false;
            }

            return templateVerbs; 
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

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetDesignTimeHtml"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the HTML to be used for the design-time representation
        ///       of the control. 
        ///    </para>
        /// </devdoc> 
        public override string GetDesignTimeHtml() { 
            bool hasATemplate = this.TemplatesExist;
            string designTimeHTML = null; 


            if (hasATemplate) {
                DataList dataList = (DataList)ViewControl; 

                bool dummyDataSource = false; 
                IEnumerable designTimeDataSource; 
                DesignerDataSourceView view = DesignerView;
                if (view == null) { 
                    designTimeDataSource = GetDesignTimeDataSource(5, out dummyDataSource);
                }
                else {
                    try { 
                        designTimeDataSource = view.GetDesignTimeData(5, out dummyDataSource);
                    } 
                    catch (Exception ex) { 
                        if (Component.Site != null) {
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)Component.Site.GetService(typeof(IComponentDesignerDebugService)); 
                            if (debugService != null) {
                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.GetDesignTimeData", ex.Message));
                            }
                        } 
                        designTimeDataSource = null;
                    } 
                } 

                bool dataKeyFieldChanged = false; 
                string oldDataKeyField = null;
                bool dataSourceIDChanged = false;
                string oldDataSourceID = null;
 
                try {
                    dataList.DataSource = designTimeDataSource; 
                    oldDataKeyField = dataList.DataKeyField; 
                    if (oldDataKeyField.Length != 0) {
                        dataKeyFieldChanged = true; 
                        dataList.DataKeyField = String.Empty;
                    }

                    oldDataSourceID = dataList.DataSourceID; 
                    dataList.DataSourceID = String.Empty;
                    dataSourceIDChanged = true; 
 
                    dataList.DataBind();
                    designTimeHTML = base.GetDesignTimeHtml(); 
                }
                catch (Exception e) {
                    designTimeHTML = GetErrorDesignTimeHtml(e);
                } 
                finally {
                    dataList.DataSource = null; 
                    if (dataKeyFieldChanged) { 
                        dataList.DataKeyField = oldDataKeyField;
                    } 
                    if (dataSourceIDChanged) {
                        dataList.DataSourceID = oldDataSourceID;
                    }
                } 
            }
            else { 
                designTimeHTML = GetEmptyDesignTimeHtml(); 
            }
            return designTimeHTML; 
        }

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetEmptyDesignTimeHtml"]/*' />
        /// <devdoc> 
        /// </devdoc>
        protected override string GetEmptyDesignTimeHtml() { 
            string text; 

            if (CanEnterTemplateMode) { 
                text = SR.GetString(SR.DataList_NoTemplatesInst);
            }
            else {
                text = SR.GetString(SR.DataList_NoTemplatesInst2); 
            }
            return CreatePlaceHolderDesignTimeHtml(text); 
        } 

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetErrorDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        protected override string GetErrorDesignTimeHtml(Exception e) {
            Debug.Fail(e.ToString()); 
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRendering));
        } 
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetTemplateContainerDataItemProperty"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the template's container's data item property.
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public override string GetTemplateContainerDataItemProperty(string templateName) { 
            return "DataItem"; 
        }
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetTemplateContent"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the template's content. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override string GetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, out bool allowEditing) {
            allowEditing = true; 

            DataList dataList = (DataList)Component;
            ITemplate template = null;
            string templateContent = String.Empty; 

            switch (editingFrame.Verb.Index) { 
                case HeaderFooterTemplates: 
                    if (templateName.Equals(HeaderFooterTemplateNames[IDX_HEADER_TEMPLATE])) {
                        template = dataList.HeaderTemplate; 
                    }
                    else if (templateName.Equals(HeaderFooterTemplateNames[IDX_FOOTER_TEMPLATE])) {
                        template = dataList.FooterTemplate;
                    } 
                    else {
                        Debug.Fail("Unknown template name passed to GetTemplateContent"); 
                    } 
                    break;
                case ItemTemplates: 
                    if (templateName.Equals(ItemTemplateNames[IDX_ITEM_TEMPLATE])) {
                        template = dataList.ItemTemplate;
                    }
                    else if (templateName.Equals(ItemTemplateNames[IDX_ALTITEM_TEMPLATE])) { 
                        template = dataList.AlternatingItemTemplate;
                    } 
                    else if (templateName.Equals(ItemTemplateNames[IDX_SELITEM_TEMPLATE])) { 
                        template = dataList.SelectedItemTemplate;
                    } 
                    else if (templateName.Equals(ItemTemplateNames[IDX_EDITITEM_TEMPLATE])) {
                        template = dataList.EditItemTemplate;
                    }
                    else { 
                        Debug.Fail("Unknown template name passed to GetTemplateContent");
                    } 
                    break; 
                case SeparatorTemplate:
                    Debug.Assert(templateName.Equals(SeparatorTemplateNames[IDX_SEPARATOR_TEMPLATE]), 
                                 "Unknown template name passed to GetTemplateContent");
                    template = dataList.SeparatorTemplate;
                    break;
                default: 
                    Debug.Fail("Unknown Index value on ITemplateEditingFrame");
                    break; 
            } 

            if (template != null) { 
                templateContent = GetTextFromTemplate(template);
            }

            return templateContent; 
        }
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.Initialize"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes the designer with the <see cref='System.Web.UI.WebControls.DataList'/> control that this instance
        ///       of the designer is associated with.
        ///    </para>
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(DataList)); 
 
            base.Initialize(component);
        } 

        /// <summary>
        /// Raised when the data source that this control is bound to has
        /// new schema. Designers can override this to perform additional 
        /// actions required when new schema is available.
        /// </summary> 
        protected override void OnSchemaRefreshed() { 
            if (InTemplateModeInternal) {
                // We ignore the SchemaRefreshed event if we are in template 
                // editing mode since the designer won't reflect the changes.
                return;
            }
 
            InvokeTransactedChange(Component, new TransactedChangeCallback(RefreshSchemaCallback), null, SR.GetString(SR.DataList_RefreshSchemaTransaction));
        } 
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.OnTemplateEditingVerbsChanged"]/*' />
        protected override void OnTemplateEditingVerbsChanged() { 
            templateVerbsDirty = true;
        }

        /// <devdoc> 
        /// Transacted change callback for refresh schema
        /// </devdoc> 
        private bool RefreshSchemaCallback(object context) { 
            DataList dataList = (DataList)Component;
            bool templatesEmpty = (dataList.ItemTemplate == null && 
                                  dataList.EditItemTemplate == null &&
                                  dataList.AlternatingItemTemplate == null &&
                                  dataList.SelectedItemTemplate == null);
            IDataSourceViewSchema schema = GetDataSourceSchema(); 

            if (DataSourceID.Length > 0 && schema != null) { 
                if (templatesEmpty || (!templatesEmpty && DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site, 
                                            SR.GetString(SR.DataList_RegenerateTemplates),
                                            SR.GetString(SR.DataList_ClearTemplatesCaption), 
                                            MessageBoxButtons.YesNo))) {
                    dataList.ItemTemplate = null;
                    dataList.EditItemTemplate = null;
                    dataList.AlternatingItemTemplate = null; 
                    dataList.SelectedItemTemplate = null;
                    dataList.DataKeyField = String.Empty; 
                    CreateDefaultTemplate(); 
                    UpdateDesignTimeHtml();
                } 
            }
            else {
                if (templatesEmpty || (!templatesEmpty && DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site,
                                            SR.GetString(SR.DataList_ClearTemplates), 
                                            SR.GetString(SR.DataList_ClearTemplatesCaption),
                                            MessageBoxButtons.YesNo))) { 
                    dataList.ItemTemplate = null; 
                    dataList.EditItemTemplate = null;
                    dataList.AlternatingItemTemplate = null; 
                    dataList.SelectedItemTemplate = null;
                    dataList.DataKeyField = String.Empty;
                    UpdateDesignTimeHtml();
                } 

            } 
            return true; 
        }
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.SetTemplateContent"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Sets the template's content. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override void SetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, string templateContent) {
            ITemplate newTemplate = null; 
            DataList dataList = (DataList)Component;

            if ((templateContent != null) && (templateContent.Length != 0)) {
                ITemplate currentTemplate = null; 

                // first get the current template so we can use it if we fail to parse the 
                // new text into a template 

                switch (editingFrame.Verb.Index) { 
                    case HeaderFooterTemplates:
                        if (templateName.Equals(HeaderFooterTemplateNames[IDX_HEADER_TEMPLATE])) {
                            currentTemplate = dataList.HeaderTemplate;
                        } 
                        else if (templateName.Equals(HeaderFooterTemplateNames[IDX_FOOTER_TEMPLATE])) {
                            currentTemplate = dataList.FooterTemplate; 
                        } 
                        break;
                    case ItemTemplates: 
                        if (templateName.Equals(ItemTemplateNames[IDX_ITEM_TEMPLATE])) {
                            currentTemplate = dataList.ItemTemplate;
                        }
                        else if (templateName.Equals(ItemTemplateNames[IDX_ALTITEM_TEMPLATE])) { 
                            currentTemplate = dataList.AlternatingItemTemplate;
                        } 
                        else if (templateName.Equals(ItemTemplateNames[IDX_SELITEM_TEMPLATE])) { 
                            currentTemplate = dataList.SelectedItemTemplate;
                        } 
                        else if (templateName.Equals(ItemTemplateNames[IDX_EDITITEM_TEMPLATE])) {
                            currentTemplate = dataList.EditItemTemplate;
                        }
                        break; 
                    case SeparatorTemplate:
                        currentTemplate = dataList.SeparatorTemplate; 
                        break; 
                }
 
                // this will parse out a new template, and if it fails, it will
                // return currentTemplate itself
                newTemplate = GetTemplateFromText(templateContent, currentTemplate);
            } 

            // Set the new template into the control. Note this may be null, if the 
            // template content was empty, i.e., the user cleared out everything in the UI. 

            switch (editingFrame.Verb.Index) { 
                case HeaderFooterTemplates:
                    if (templateName.Equals(HeaderFooterTemplateNames[IDX_HEADER_TEMPLATE])) {
                        dataList.HeaderTemplate = newTemplate;
                    } 
                    else if (templateName.Equals(HeaderFooterTemplateNames[IDX_FOOTER_TEMPLATE])) {
                        dataList.FooterTemplate = newTemplate; 
                    } 
                    else {
                        Debug.Fail("Unknown template name passed to SetTemplateContent"); 
                    }
                    break;
                case ItemTemplates:
                    if (templateName.Equals(ItemTemplateNames[IDX_ITEM_TEMPLATE])) { 
                        dataList.ItemTemplate = newTemplate;
                    } 
                    else if (templateName.Equals(ItemTemplateNames[IDX_ALTITEM_TEMPLATE])) { 
                        dataList.AlternatingItemTemplate = newTemplate;
                    } 
                    else if (templateName.Equals(ItemTemplateNames[IDX_SELITEM_TEMPLATE])) {
                        dataList.SelectedItemTemplate = newTemplate;
                    }
                    else if (templateName.Equals(ItemTemplateNames[IDX_EDITITEM_TEMPLATE])) { 
                        dataList.EditItemTemplate = newTemplate;
                    } 
                    else { 
                        Debug.Fail("Unknown template name passed to SetTemplateContent");
                    } 
                    break;
                case SeparatorTemplate:
                    Debug.Assert(templateName.Equals(SeparatorTemplateNames[IDX_SEPARATOR_TEMPLATE]),
                                 "Unknown template name passed to SetTemplateContent"); 
                    dataList.SeparatorTemplate = newTemplate;
                    break; 
                default: 
                    Debug.Fail("Unknown Index value on ITemplateEditingFrame");
                    break; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataListDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Design;
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.IO;
    using System.Text; 
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       Provides a designer for the <see cref='System.Web.UI.WebControls.DataList'/> 
    ///       control.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)]
    public class DataListDesigner : BaseDataListDesigner { 
 
        internal static TraceSwitch DataListDesignerSwitch =
            new TraceSwitch("DATALISTDESIGNER", "Enable DataList designer general purpose traces."); 

        private const string templateFieldString = "{0}: <asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{2}Label\"/><br />";
        private const string breakString = "<br />";
        private const int HeaderFooterTemplates = 1; 
        private const int ItemTemplates = 0;
        private const int SeparatorTemplate = 2; 
 
        private static string[] HeaderFooterTemplateNames = new string[] { "HeaderTemplate", "FooterTemplate" };
        private const int IDX_HEADER_TEMPLATE = 0; 
        private const int IDX_FOOTER_TEMPLATE = 1;

        private static string[] ItemTemplateNames = new String[] { "ItemTemplate", "AlternatingItemTemplate", "SelectedItemTemplate", "EditItemTemplate" };
        private const int IDX_ITEM_TEMPLATE = 0; 
        private const int IDX_ALTITEM_TEMPLATE = 1;
        private const int IDX_SELITEM_TEMPLATE = 2; 
        private const int IDX_EDITITEM_TEMPLATE = 3; 

        private static string[] SeparatorTemplateNames = new String[] { "SeparatorTemplate" }; 
        private const int IDX_SEPARATOR_TEMPLATE = 0;

#pragma warning disable 618
        private TemplateEditingVerb[] templateVerbs; 
#pragma warning restore 618
        private bool templateVerbsDirty; 
 
        private static DesignerAutoFormatCollection _autoFormats;
 

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.DataListDesigner"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.WebControls.DataListDesigner'/>.
        ///    </para> 
        /// </devdoc> 
        public DataListDesigner() {
            templateVerbsDirty = true; 
        }

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.AllowResize"]/*' />
        public override bool AllowResize { 
            get {
                // When templates are not defined, we render a read-only fixed 
                // size block. Once templates are defined or are being edited the control should allow 
                // resizing.
                return TemplatesExist || InTemplateModeInternal; 
            }
        }

        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.BDL_SCHEMES, 
                        delegate(DataRow schemeData) { return new DataListAutoFormat(schemeData); });
                } 
                return _autoFormats;
            }
        }
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.TemplatesExist"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets a value
        ///       indicating whether templates associated to the designer currently exist. 
        ///    </para>
        /// </devdoc>
        protected bool TemplatesExist {
            get { 
                DataList dataList = (DataList)ViewControl;
                ITemplate itemTemplate = dataList.ItemTemplate; 
                string templateText = null; 
                if (itemTemplate != null) {
                    templateText = GetTextFromTemplate(itemTemplate); 
                    return templateText != null && templateText.Length > 0;
                }
                return false;
            } 
        }
 
        private void CreateDefaultTemplate() { 
            string newTemplateText = String.Empty;
            StringBuilder sb = new StringBuilder(); 
            DataList dataList = (DataList)Component;

            IDataSourceViewSchema schema = GetDataSourceSchema();
            IDataSourceFieldSchema[] fieldSchemas = null; 

            if (schema != null) { 
                fieldSchemas = schema.GetFields(); 
            }
 
            if (fieldSchemas != null && fieldSchemas.Length > 0) {
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
 
                    sb.Append(String.Format(CultureInfo.InvariantCulture, templateFieldString, fieldName, DesignTimeDataBinding.CreateEvalExpression(fieldName, String.Empty), fieldDerivedIDString));
                    sb.Append(Environment.NewLine); 
                    if (fieldSchema.PrimaryKey && dataList.DataKeyField.Length == 0) {
                        dataList.DataKeyField = fieldName;
                    }
                } 
                sb.Append(breakString);
                sb.Append(Environment.NewLine); 
                newTemplateText = sb.ToString(); 
            }
 
            if (newTemplateText != null && newTemplateText.Length > 0) {
                try {
                    // if the schema has a field whose name is not a valid id, this may fail.
                    // It should fail silently. 
                    dataList.ItemTemplate = GetTemplateFromText(newTemplateText, dataList.ItemTemplate);
                } 
                catch { 
                }
            } 
        }

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.CreateTemplateEditingFrame"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected override ITemplateEditingFrame CreateTemplateEditingFrame(TemplateEditingVerb verb) {
            ITemplateEditingService teService = (ITemplateEditingService)GetService(typeof(ITemplateEditingService)); 
            Debug.Assert(teService != null, "How did we get this far without an ITemplateEditingService"); 
            DataList dataList = (DataList)ViewControl;
 
            string[] templateNames = null;
            Style[] templateStyles = null;

            switch (verb.Index) { 
                case HeaderFooterTemplates:
                    templateNames = HeaderFooterTemplateNames; 
                    templateStyles = new Style[] { dataList.HeaderStyle, dataList.FooterStyle }; 
                    break;
                case ItemTemplates: 
                    templateNames = ItemTemplateNames;
                    templateStyles = new Style[] { dataList.ItemStyle, dataList.AlternatingItemStyle, dataList.SelectedItemStyle, dataList.EditItemStyle };
                    break;
                case SeparatorTemplate: 
                    templateNames = SeparatorTemplateNames;
                    templateStyles = new Style[] { dataList.SeparatorStyle }; 
                    break; 
                default:
                    Debug.Fail("Unknown Index value on TemplateEditingVerb"); 
                    break;
            }

            ITemplateEditingFrame editingFrame = 
                teService.CreateFrame(this, verb.Text, templateNames, dataList.ControlStyle, templateStyles);
            return editingFrame; 
        } 

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Disposes of the resources (other than memory) used by the
        ///    <see cref='System.Web.UI.Design.WebControls.DataListDesigner'/>. 
        ///    </para>
        /// </devdoc> 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                DisposeTemplateVerbs(); 
            }

            base.Dispose(disposing);
        } 

        private void DisposeTemplateVerbs() { 
            if (templateVerbs != null) { 
                for (int i = 0; i < templateVerbs.Length; i++) {
                    templateVerbs[i].Dispose(); 
                }

                templateVerbs = null;
                templateVerbsDirty = true; 
            }
        } 
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetCachedTemplateEditingVerbs"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected override TemplateEditingVerb[] GetCachedTemplateEditingVerbs() {
            if (templateVerbsDirty == true) {
                DisposeTemplateVerbs();
 
                templateVerbs = new TemplateEditingVerb[3];
                templateVerbs[0] = new TemplateEditingVerb(SR.GetString(SR.DataList_ItemTemplates), ItemTemplates, this); 
                templateVerbs[1] = new TemplateEditingVerb(SR.GetString(SR.DataList_HeaderFooterTemplates), HeaderFooterTemplates, this); 
                templateVerbs[2] = new TemplateEditingVerb(SR.GetString(SR.DataList_SeparatorTemplate), SeparatorTemplate, this);
 
                templateVerbsDirty = false;
            }

            return templateVerbs; 
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

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetDesignTimeHtml"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the HTML to be used for the design-time representation
        ///       of the control. 
        ///    </para>
        /// </devdoc> 
        public override string GetDesignTimeHtml() { 
            bool hasATemplate = this.TemplatesExist;
            string designTimeHTML = null; 


            if (hasATemplate) {
                DataList dataList = (DataList)ViewControl; 

                bool dummyDataSource = false; 
                IEnumerable designTimeDataSource; 
                DesignerDataSourceView view = DesignerView;
                if (view == null) { 
                    designTimeDataSource = GetDesignTimeDataSource(5, out dummyDataSource);
                }
                else {
                    try { 
                        designTimeDataSource = view.GetDesignTimeData(5, out dummyDataSource);
                    } 
                    catch (Exception ex) { 
                        if (Component.Site != null) {
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)Component.Site.GetService(typeof(IComponentDesignerDebugService)); 
                            if (debugService != null) {
                                debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.GetDesignTimeData", ex.Message));
                            }
                        } 
                        designTimeDataSource = null;
                    } 
                } 

                bool dataKeyFieldChanged = false; 
                string oldDataKeyField = null;
                bool dataSourceIDChanged = false;
                string oldDataSourceID = null;
 
                try {
                    dataList.DataSource = designTimeDataSource; 
                    oldDataKeyField = dataList.DataKeyField; 
                    if (oldDataKeyField.Length != 0) {
                        dataKeyFieldChanged = true; 
                        dataList.DataKeyField = String.Empty;
                    }

                    oldDataSourceID = dataList.DataSourceID; 
                    dataList.DataSourceID = String.Empty;
                    dataSourceIDChanged = true; 
 
                    dataList.DataBind();
                    designTimeHTML = base.GetDesignTimeHtml(); 
                }
                catch (Exception e) {
                    designTimeHTML = GetErrorDesignTimeHtml(e);
                } 
                finally {
                    dataList.DataSource = null; 
                    if (dataKeyFieldChanged) { 
                        dataList.DataKeyField = oldDataKeyField;
                    } 
                    if (dataSourceIDChanged) {
                        dataList.DataSourceID = oldDataSourceID;
                    }
                } 
            }
            else { 
                designTimeHTML = GetEmptyDesignTimeHtml(); 
            }
            return designTimeHTML; 
        }

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetEmptyDesignTimeHtml"]/*' />
        /// <devdoc> 
        /// </devdoc>
        protected override string GetEmptyDesignTimeHtml() { 
            string text; 

            if (CanEnterTemplateMode) { 
                text = SR.GetString(SR.DataList_NoTemplatesInst);
            }
            else {
                text = SR.GetString(SR.DataList_NoTemplatesInst2); 
            }
            return CreatePlaceHolderDesignTimeHtml(text); 
        } 

        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetErrorDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        protected override string GetErrorDesignTimeHtml(Exception e) {
            Debug.Fail(e.ToString()); 
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRendering));
        } 
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetTemplateContainerDataItemProperty"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the template's container's data item property.
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public override string GetTemplateContainerDataItemProperty(string templateName) { 
            return "DataItem"; 
        }
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.GetTemplateContent"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the template's content. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override string GetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, out bool allowEditing) {
            allowEditing = true; 

            DataList dataList = (DataList)Component;
            ITemplate template = null;
            string templateContent = String.Empty; 

            switch (editingFrame.Verb.Index) { 
                case HeaderFooterTemplates: 
                    if (templateName.Equals(HeaderFooterTemplateNames[IDX_HEADER_TEMPLATE])) {
                        template = dataList.HeaderTemplate; 
                    }
                    else if (templateName.Equals(HeaderFooterTemplateNames[IDX_FOOTER_TEMPLATE])) {
                        template = dataList.FooterTemplate;
                    } 
                    else {
                        Debug.Fail("Unknown template name passed to GetTemplateContent"); 
                    } 
                    break;
                case ItemTemplates: 
                    if (templateName.Equals(ItemTemplateNames[IDX_ITEM_TEMPLATE])) {
                        template = dataList.ItemTemplate;
                    }
                    else if (templateName.Equals(ItemTemplateNames[IDX_ALTITEM_TEMPLATE])) { 
                        template = dataList.AlternatingItemTemplate;
                    } 
                    else if (templateName.Equals(ItemTemplateNames[IDX_SELITEM_TEMPLATE])) { 
                        template = dataList.SelectedItemTemplate;
                    } 
                    else if (templateName.Equals(ItemTemplateNames[IDX_EDITITEM_TEMPLATE])) {
                        template = dataList.EditItemTemplate;
                    }
                    else { 
                        Debug.Fail("Unknown template name passed to GetTemplateContent");
                    } 
                    break; 
                case SeparatorTemplate:
                    Debug.Assert(templateName.Equals(SeparatorTemplateNames[IDX_SEPARATOR_TEMPLATE]), 
                                 "Unknown template name passed to GetTemplateContent");
                    template = dataList.SeparatorTemplate;
                    break;
                default: 
                    Debug.Fail("Unknown Index value on ITemplateEditingFrame");
                    break; 
            } 

            if (template != null) { 
                templateContent = GetTextFromTemplate(template);
            }

            return templateContent; 
        }
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.Initialize"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes the designer with the <see cref='System.Web.UI.WebControls.DataList'/> control that this instance
        ///       of the designer is associated with.
        ///    </para>
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(DataList)); 
 
            base.Initialize(component);
        } 

        /// <summary>
        /// Raised when the data source that this control is bound to has
        /// new schema. Designers can override this to perform additional 
        /// actions required when new schema is available.
        /// </summary> 
        protected override void OnSchemaRefreshed() { 
            if (InTemplateModeInternal) {
                // We ignore the SchemaRefreshed event if we are in template 
                // editing mode since the designer won't reflect the changes.
                return;
            }
 
            InvokeTransactedChange(Component, new TransactedChangeCallback(RefreshSchemaCallback), null, SR.GetString(SR.DataList_RefreshSchemaTransaction));
        } 
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.OnTemplateEditingVerbsChanged"]/*' />
        protected override void OnTemplateEditingVerbsChanged() { 
            templateVerbsDirty = true;
        }

        /// <devdoc> 
        /// Transacted change callback for refresh schema
        /// </devdoc> 
        private bool RefreshSchemaCallback(object context) { 
            DataList dataList = (DataList)Component;
            bool templatesEmpty = (dataList.ItemTemplate == null && 
                                  dataList.EditItemTemplate == null &&
                                  dataList.AlternatingItemTemplate == null &&
                                  dataList.SelectedItemTemplate == null);
            IDataSourceViewSchema schema = GetDataSourceSchema(); 

            if (DataSourceID.Length > 0 && schema != null) { 
                if (templatesEmpty || (!templatesEmpty && DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site, 
                                            SR.GetString(SR.DataList_RegenerateTemplates),
                                            SR.GetString(SR.DataList_ClearTemplatesCaption), 
                                            MessageBoxButtons.YesNo))) {
                    dataList.ItemTemplate = null;
                    dataList.EditItemTemplate = null;
                    dataList.AlternatingItemTemplate = null; 
                    dataList.SelectedItemTemplate = null;
                    dataList.DataKeyField = String.Empty; 
                    CreateDefaultTemplate(); 
                    UpdateDesignTimeHtml();
                } 
            }
            else {
                if (templatesEmpty || (!templatesEmpty && DialogResult.Yes == UIServiceHelper.ShowMessage(Component.Site,
                                            SR.GetString(SR.DataList_ClearTemplates), 
                                            SR.GetString(SR.DataList_ClearTemplatesCaption),
                                            MessageBoxButtons.YesNo))) { 
                    dataList.ItemTemplate = null; 
                    dataList.EditItemTemplate = null;
                    dataList.AlternatingItemTemplate = null; 
                    dataList.SelectedItemTemplate = null;
                    dataList.DataKeyField = String.Empty;
                    UpdateDesignTimeHtml();
                } 

            } 
            return true; 
        }
 
        /// <include file='doc\DataListDesigner.uex' path='docs/doc[@for="DataListDesigner.SetTemplateContent"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Sets the template's content. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override void SetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, string templateContent) {
            ITemplate newTemplate = null; 
            DataList dataList = (DataList)Component;

            if ((templateContent != null) && (templateContent.Length != 0)) {
                ITemplate currentTemplate = null; 

                // first get the current template so we can use it if we fail to parse the 
                // new text into a template 

                switch (editingFrame.Verb.Index) { 
                    case HeaderFooterTemplates:
                        if (templateName.Equals(HeaderFooterTemplateNames[IDX_HEADER_TEMPLATE])) {
                            currentTemplate = dataList.HeaderTemplate;
                        } 
                        else if (templateName.Equals(HeaderFooterTemplateNames[IDX_FOOTER_TEMPLATE])) {
                            currentTemplate = dataList.FooterTemplate; 
                        } 
                        break;
                    case ItemTemplates: 
                        if (templateName.Equals(ItemTemplateNames[IDX_ITEM_TEMPLATE])) {
                            currentTemplate = dataList.ItemTemplate;
                        }
                        else if (templateName.Equals(ItemTemplateNames[IDX_ALTITEM_TEMPLATE])) { 
                            currentTemplate = dataList.AlternatingItemTemplate;
                        } 
                        else if (templateName.Equals(ItemTemplateNames[IDX_SELITEM_TEMPLATE])) { 
                            currentTemplate = dataList.SelectedItemTemplate;
                        } 
                        else if (templateName.Equals(ItemTemplateNames[IDX_EDITITEM_TEMPLATE])) {
                            currentTemplate = dataList.EditItemTemplate;
                        }
                        break; 
                    case SeparatorTemplate:
                        currentTemplate = dataList.SeparatorTemplate; 
                        break; 
                }
 
                // this will parse out a new template, and if it fails, it will
                // return currentTemplate itself
                newTemplate = GetTemplateFromText(templateContent, currentTemplate);
            } 

            // Set the new template into the control. Note this may be null, if the 
            // template content was empty, i.e., the user cleared out everything in the UI. 

            switch (editingFrame.Verb.Index) { 
                case HeaderFooterTemplates:
                    if (templateName.Equals(HeaderFooterTemplateNames[IDX_HEADER_TEMPLATE])) {
                        dataList.HeaderTemplate = newTemplate;
                    } 
                    else if (templateName.Equals(HeaderFooterTemplateNames[IDX_FOOTER_TEMPLATE])) {
                        dataList.FooterTemplate = newTemplate; 
                    } 
                    else {
                        Debug.Fail("Unknown template name passed to SetTemplateContent"); 
                    }
                    break;
                case ItemTemplates:
                    if (templateName.Equals(ItemTemplateNames[IDX_ITEM_TEMPLATE])) { 
                        dataList.ItemTemplate = newTemplate;
                    } 
                    else if (templateName.Equals(ItemTemplateNames[IDX_ALTITEM_TEMPLATE])) { 
                        dataList.AlternatingItemTemplate = newTemplate;
                    } 
                    else if (templateName.Equals(ItemTemplateNames[IDX_SELITEM_TEMPLATE])) {
                        dataList.SelectedItemTemplate = newTemplate;
                    }
                    else if (templateName.Equals(ItemTemplateNames[IDX_EDITITEM_TEMPLATE])) { 
                        dataList.EditItemTemplate = newTemplate;
                    } 
                    else { 
                        Debug.Fail("Unknown template name passed to SetTemplateContent");
                    } 
                    break;
                case SeparatorTemplate:
                    Debug.Assert(templateName.Equals(SeparatorTemplateNames[IDX_SEPARATOR_TEMPLATE]),
                                 "Unknown template name passed to SetTemplateContent"); 
                    dataList.SeparatorTemplate = newTemplate;
                    break; 
                default: 
                    Debug.Fail("Unknown Index value on ITemplateEditingFrame");
                    break; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
