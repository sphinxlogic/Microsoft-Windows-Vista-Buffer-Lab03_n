//------------------------------------------------------------------------------ 
// <copyright file="DataGridDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.IO; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 

    /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner"]/*' />
    /// <devdoc>
    ///    <para> 
    ///       This is the designer class for the <see cref='System.Web.UI.WebControls.DataGrid'/>
    ///       control. 
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)]
    public class DataGridDesigner : BaseDataListDesigner {

        internal static TraceSwitch DataGridDesignerSwitch = 
            new TraceSwitch("DATAGRIDDESIGNER", "Enable DataGrid designer general purpose traces.");
 
        private static string[] ColumnTemplateNames = new string[] { "ItemTemplate", "EditItemTemplate", "HeaderTemplate", "FooterTemplate" }; 
        private const int IDX_HEADER_TEMPLATE = 2;
        private const int IDX_ITEM_TEMPLATE = 0; 
        private const int IDX_EDITITEM_TEMPLATE = 1;
        private const int IDX_FOOTER_TEMPLATE = 3;

#pragma warning disable 618 
        private TemplateEditingVerb[] templateVerbs;
#pragma warning restore 618 
        private bool templateVerbsDirty; 

        private static DesignerAutoFormatCollection _autoFormats; 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.DataGridDesigner"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.WebControls.DataGridDesigner'/>.
        ///    </para> 
        /// </devdoc> 
        public DataGridDesigner() {
            templateVerbsDirty = true; 
        }

        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.BDL_SCHEMES, 
                        delegate(DataRow schemeData) { return new DataGridAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            }
        }

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.CreateTemplateEditingFrame"]/*' /> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        protected override ITemplateEditingFrame CreateTemplateEditingFrame(TemplateEditingVerb verb) { 
            ITemplateEditingService teService = (ITemplateEditingService)GetService(typeof(ITemplateEditingService)); 
            Debug.Assert(teService != null, "How did we get this far without an ITemplateEditingService");
 
            DataGrid grid = (DataGrid)ViewControl;
            Style[] templateStyles = new Style[] { grid.ItemStyle, grid.EditItemStyle, grid.HeaderStyle, grid.FooterStyle };

            ITemplateEditingFrame editingFrame = 
                teService.CreateFrame(this, verb.Text, ColumnTemplateNames, grid.ControlStyle, templateStyles);
            return editingFrame; 
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.Dispose"]/*' /> 
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

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetCachedTemplateEditingVerbs"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        protected override TemplateEditingVerb[] GetCachedTemplateEditingVerbs() { 
            if (templateVerbsDirty == true) {
                DisposeTemplateVerbs(); 
 
                DataGridColumnCollection columns = ((DataGrid)Component).Columns;
                int columnCount = columns.Count; 

                if (columnCount > 0) {
                    int templateColumns  = 0;
                    int i, t; 

                    for (i = 0; i < columnCount; i++) { 
                        if (columns[i] is TemplateColumn) { 
                            templateColumns++;
                        } 
                    }

                    if (templateColumns > 0) {
                        templateVerbs = new TemplateEditingVerb[templateColumns]; 

                        for (i = 0, t = 0; i < columnCount; i++) { 
                            if (columns[i] is TemplateColumn) { 
                                string headerText = columns[i].HeaderText;
                                string caption = "Columns[" + i.ToString(NumberFormatInfo.CurrentInfo) + "]"; 

                                if ((headerText != null) && (headerText.Length != 0)) {
                                    caption = caption + " - " + headerText;
                                } 
                                templateVerbs[t] = new TemplateEditingVerb(caption, i, this);
                                t++; 
                            } 
                        }
                    } 
                }

                templateVerbsDirty = false;
            } 

            return templateVerbs; 
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetDesignTimeHtml"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the HTML to be used for the design time representation
        ///       of the control. 
        ///    </para>
        /// </devdoc> 
        public override string GetDesignTimeHtml() { 
            int sampleRows = 5;
 
            DataGrid dataGrid = (DataGrid)ViewControl;

            // ensure there are enough sample rows to show an entire page, and still
            // have 1 more for a navigation button to be enabled 
            // we also want to ensure we don't have something ridiculously large
            if (dataGrid.AllowPaging && dataGrid.PageSize != 0) { 
                sampleRows = Math.Min(dataGrid.PageSize, 100) + 1; 
            }
 
            bool dummyDataSource = false;
            IEnumerable designTimeDataSource = null;
            bool autoGenColumnsChanged = false;
            bool dataKeyFieldChanged = false; 
            bool dataSourceIDChanged = false;
            DesignerDataSourceView view = DesignerView; 
 
            bool oldAutoGenColumns = dataGrid.AutoGenerateColumns;
            string oldDataKeyField = String.Empty; 
            string oldDataSourceID = String.Empty;

            string designTimeHTML = null;
 
            if (view == null) {
                designTimeDataSource = GetDesignTimeDataSource(sampleRows, out dummyDataSource); 
            } 
            else {
                try { 
                    designTimeDataSource = view.GetDesignTimeData(sampleRows, out dummyDataSource);
                }
                catch (Exception ex) {
                    if (Component.Site != null) { 
                        IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)Component.Site.GetService(typeof(IComponentDesignerDebugService));
                        if (debugService != null) { 
                            debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.GetDesignTimeData", ex.Message)); 
                        }
                    } 
                }
                if (designTimeDataSource == null) {
                    Debug.Assert(false, "DataBoundControlDesigner::GetDesignTimeHtml - DesignTimeDataSource was null.");
                    return GetEmptyDesignTimeHtml(); 
                }
            } 
 
            if ((oldAutoGenColumns == false) && (dataGrid.Columns.Count == 0)) {
                // ensure that AutoGenerateColumns is true when we don't have 
                // a columns collection, so we see atleast something at
                // design time.
                autoGenColumnsChanged = true;
                dataGrid.AutoGenerateColumns = true; 
            }
 
            if (dummyDataSource) { 
                oldDataKeyField = dataGrid.DataKeyField;
                if (oldDataKeyField.Length != 0) { 
                    dataKeyFieldChanged = true;
                    dataGrid.DataKeyField = String.Empty;
                }
            } 

            try { 
                dataGrid.DataSource = designTimeDataSource; 
                oldDataSourceID = dataGrid.DataSourceID;
                dataGrid.DataSourceID = String.Empty; 
                dataSourceIDChanged = true;
                dataGrid.DataBind();
                designTimeHTML = base.GetDesignTimeHtml();
            } 
            catch (Exception e) {
                designTimeHTML = GetErrorDesignTimeHtml(e); 
            } 
            finally {
                // restore settings we changed for rendering purposes 
                dataGrid.DataSource = null;
                if (autoGenColumnsChanged) {
                    dataGrid.AutoGenerateColumns = false;
                } 
                if (dataKeyFieldChanged == true) {
                    dataGrid.DataKeyField = oldDataKeyField; 
                } 
                if (dataSourceIDChanged == true) {
                    dataGrid.DataSourceID = oldDataSourceID; 
                }
            }
            return designTimeHTML;
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetEmptyDesignTimeHtml"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        protected override string GetEmptyDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml(null);
        }

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetErrorDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected override string GetErrorDesignTimeHtml(Exception e) { 
            Debug.Fail(e.ToString());
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRendering)); 
        }

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetTemplateContainerDataItemProperty"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the template's container's data item property. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override string GetTemplateContainerDataItemProperty(string templateName) {
            return "DataItem";
        }
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetTemplateContent"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets the template's content.
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public override string GetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, out bool allowEditing) {
            allowEditing = true; 
            DataGrid dataGrid = (DataGrid)Component;
 
            int columnIndex = editingFrame.Verb.Index; 

            Debug.Assert((columnIndex >= 0) && (columnIndex < dataGrid.Columns.Count), 
                         "Invalid column index in template editing frame.");
            Debug.Assert(dataGrid.Columns[columnIndex] is TemplateColumn,
                         "Template editing frame points to a non-TemplateColumn column.");
 
            TemplateColumn column = (TemplateColumn)dataGrid.Columns[columnIndex];
            ITemplate template = null; 
            string templateContent = String.Empty; 

            if (templateName.Equals(ColumnTemplateNames[IDX_HEADER_TEMPLATE])) { 
                template = column.HeaderTemplate;
            }
            else if (templateName.Equals(ColumnTemplateNames[IDX_ITEM_TEMPLATE])) {
                template = column.ItemTemplate; 
            }
            else if (templateName.Equals(ColumnTemplateNames[IDX_EDITITEM_TEMPLATE])) { 
                template = column.EditItemTemplate; 
            }
            else if (templateName.Equals(ColumnTemplateNames[IDX_FOOTER_TEMPLATE])) { 
                template = column.FooterTemplate;
            }
            else {
                Debug.Fail("Unknown template name passed to GetTemplateContent"); 
            }
 
            if (template != null) { 
                templateContent = GetTextFromTemplate(template);
            } 

            return templateContent;
        }
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetTemplatePropertyParentType"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override Type GetTemplatePropertyParentType(string templateName) { 
            return typeof(TemplateColumn);
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.OnColumnsChanged"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when the columns changed event occurs.
        ///    </para> 
        /// </devdoc> 
        public virtual void OnColumnsChanged() {
            OnTemplateEditingVerbsChanged(); 
        }

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.OnTemplateEditingVerbsChanged"]/*' />
        protected override void OnTemplateEditingVerbsChanged() { 
            templateVerbsDirty = true;
        } 
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///   Initializes the designer with the DataGrid control that this instance
        ///   of the designer is associated with.
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(DataGrid));
 
            base.Initialize(component); 
        }
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.SetTemplateContent"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Sets the content for the specified template and frame. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override void SetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, string templateContent) {
            int columnIndex = editingFrame.Verb.Index; 
            DataGrid dataGrid = (DataGrid)Component;

            Debug.Assert((columnIndex >= 0) && (columnIndex < dataGrid.Columns.Count),
                         "Invalid column index in template editing frame."); 
            Debug.Assert(dataGrid.Columns[columnIndex] is TemplateColumn,
                         "Template editing frame points to a non-TemplateColumn column."); 
 
            TemplateColumn column = (TemplateColumn)dataGrid.Columns[columnIndex];
            ITemplate newTemplate = null; 

            if ((templateContent != null) && (templateContent.Length != 0)) {
                ITemplate currentTemplate = null;
 
                // first get the current template so we can use it if we fail to parse the
                // new text into a template 
 
                if (templateName.Equals(ColumnTemplateNames[IDX_HEADER_TEMPLATE])) {
                    currentTemplate = column.HeaderTemplate; 
                }
                else if (templateName.Equals(ColumnTemplateNames[IDX_ITEM_TEMPLATE])) {
                    currentTemplate = column.ItemTemplate;
                } 
                else if (templateName.Equals(ColumnTemplateNames[IDX_EDITITEM_TEMPLATE])) {
                    currentTemplate = column.EditItemTemplate; 
                } 
                else if (templateName.Equals(ColumnTemplateNames[IDX_FOOTER_TEMPLATE])) {
                    currentTemplate = column.FooterTemplate; 
                }

                // this will parse out a new template, and if it fails, it will
                // return currentTemplate itself 
                newTemplate = GetTemplateFromText(templateContent, currentTemplate);
            } 
 
            // Set the new template into the control. Note this may be null, if the
            // template content was empty, i.e., the user cleared out everything in the UI. 

            if (templateName.Equals(ColumnTemplateNames[IDX_HEADER_TEMPLATE])) {
                column.HeaderTemplate = newTemplate;
            } 
            else if (templateName.Equals(ColumnTemplateNames[IDX_ITEM_TEMPLATE])) {
                column.ItemTemplate = newTemplate; 
            } 
            else if (templateName.Equals(ColumnTemplateNames[IDX_EDITITEM_TEMPLATE])) {
                column.EditItemTemplate = newTemplate; 
            }
            else if (templateName.Equals(ColumnTemplateNames[IDX_FOOTER_TEMPLATE])) {
                column.FooterTemplate = newTemplate;
            } 
            else {
                Debug.Fail("Unknown template name passed to SetTemplateContent"); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.IO; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 

    /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner"]/*' />
    /// <devdoc>
    ///    <para> 
    ///       This is the designer class for the <see cref='System.Web.UI.WebControls.DataGrid'/>
    ///       control. 
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)]
    public class DataGridDesigner : BaseDataListDesigner {

        internal static TraceSwitch DataGridDesignerSwitch = 
            new TraceSwitch("DATAGRIDDESIGNER", "Enable DataGrid designer general purpose traces.");
 
        private static string[] ColumnTemplateNames = new string[] { "ItemTemplate", "EditItemTemplate", "HeaderTemplate", "FooterTemplate" }; 
        private const int IDX_HEADER_TEMPLATE = 2;
        private const int IDX_ITEM_TEMPLATE = 0; 
        private const int IDX_EDITITEM_TEMPLATE = 1;
        private const int IDX_FOOTER_TEMPLATE = 3;

#pragma warning disable 618 
        private TemplateEditingVerb[] templateVerbs;
#pragma warning restore 618 
        private bool templateVerbsDirty; 

        private static DesignerAutoFormatCollection _autoFormats; 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.DataGridDesigner"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.WebControls.DataGridDesigner'/>.
        ///    </para> 
        /// </devdoc> 
        public DataGridDesigner() {
            templateVerbsDirty = true; 
        }

        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.BDL_SCHEMES, 
                        delegate(DataRow schemeData) { return new DataGridAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            }
        }

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.CreateTemplateEditingFrame"]/*' /> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        protected override ITemplateEditingFrame CreateTemplateEditingFrame(TemplateEditingVerb verb) { 
            ITemplateEditingService teService = (ITemplateEditingService)GetService(typeof(ITemplateEditingService)); 
            Debug.Assert(teService != null, "How did we get this far without an ITemplateEditingService");
 
            DataGrid grid = (DataGrid)ViewControl;
            Style[] templateStyles = new Style[] { grid.ItemStyle, grid.EditItemStyle, grid.HeaderStyle, grid.FooterStyle };

            ITemplateEditingFrame editingFrame = 
                teService.CreateFrame(this, verb.Text, ColumnTemplateNames, grid.ControlStyle, templateStyles);
            return editingFrame; 
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.Dispose"]/*' /> 
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

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetCachedTemplateEditingVerbs"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        protected override TemplateEditingVerb[] GetCachedTemplateEditingVerbs() { 
            if (templateVerbsDirty == true) {
                DisposeTemplateVerbs(); 
 
                DataGridColumnCollection columns = ((DataGrid)Component).Columns;
                int columnCount = columns.Count; 

                if (columnCount > 0) {
                    int templateColumns  = 0;
                    int i, t; 

                    for (i = 0; i < columnCount; i++) { 
                        if (columns[i] is TemplateColumn) { 
                            templateColumns++;
                        } 
                    }

                    if (templateColumns > 0) {
                        templateVerbs = new TemplateEditingVerb[templateColumns]; 

                        for (i = 0, t = 0; i < columnCount; i++) { 
                            if (columns[i] is TemplateColumn) { 
                                string headerText = columns[i].HeaderText;
                                string caption = "Columns[" + i.ToString(NumberFormatInfo.CurrentInfo) + "]"; 

                                if ((headerText != null) && (headerText.Length != 0)) {
                                    caption = caption + " - " + headerText;
                                } 
                                templateVerbs[t] = new TemplateEditingVerb(caption, i, this);
                                t++; 
                            } 
                        }
                    } 
                }

                templateVerbsDirty = false;
            } 

            return templateVerbs; 
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetDesignTimeHtml"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the HTML to be used for the design time representation
        ///       of the control. 
        ///    </para>
        /// </devdoc> 
        public override string GetDesignTimeHtml() { 
            int sampleRows = 5;
 
            DataGrid dataGrid = (DataGrid)ViewControl;

            // ensure there are enough sample rows to show an entire page, and still
            // have 1 more for a navigation button to be enabled 
            // we also want to ensure we don't have something ridiculously large
            if (dataGrid.AllowPaging && dataGrid.PageSize != 0) { 
                sampleRows = Math.Min(dataGrid.PageSize, 100) + 1; 
            }
 
            bool dummyDataSource = false;
            IEnumerable designTimeDataSource = null;
            bool autoGenColumnsChanged = false;
            bool dataKeyFieldChanged = false; 
            bool dataSourceIDChanged = false;
            DesignerDataSourceView view = DesignerView; 
 
            bool oldAutoGenColumns = dataGrid.AutoGenerateColumns;
            string oldDataKeyField = String.Empty; 
            string oldDataSourceID = String.Empty;

            string designTimeHTML = null;
 
            if (view == null) {
                designTimeDataSource = GetDesignTimeDataSource(sampleRows, out dummyDataSource); 
            } 
            else {
                try { 
                    designTimeDataSource = view.GetDesignTimeData(sampleRows, out dummyDataSource);
                }
                catch (Exception ex) {
                    if (Component.Site != null) { 
                        IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)Component.Site.GetService(typeof(IComponentDesignerDebugService));
                        if (debugService != null) { 
                            debugService.Fail(SR.GetString(SR.DataSource_DebugService_FailedCall, "DesignerDataSourceView.GetDesignTimeData", ex.Message)); 
                        }
                    } 
                }
                if (designTimeDataSource == null) {
                    Debug.Assert(false, "DataBoundControlDesigner::GetDesignTimeHtml - DesignTimeDataSource was null.");
                    return GetEmptyDesignTimeHtml(); 
                }
            } 
 
            if ((oldAutoGenColumns == false) && (dataGrid.Columns.Count == 0)) {
                // ensure that AutoGenerateColumns is true when we don't have 
                // a columns collection, so we see atleast something at
                // design time.
                autoGenColumnsChanged = true;
                dataGrid.AutoGenerateColumns = true; 
            }
 
            if (dummyDataSource) { 
                oldDataKeyField = dataGrid.DataKeyField;
                if (oldDataKeyField.Length != 0) { 
                    dataKeyFieldChanged = true;
                    dataGrid.DataKeyField = String.Empty;
                }
            } 

            try { 
                dataGrid.DataSource = designTimeDataSource; 
                oldDataSourceID = dataGrid.DataSourceID;
                dataGrid.DataSourceID = String.Empty; 
                dataSourceIDChanged = true;
                dataGrid.DataBind();
                designTimeHTML = base.GetDesignTimeHtml();
            } 
            catch (Exception e) {
                designTimeHTML = GetErrorDesignTimeHtml(e); 
            } 
            finally {
                // restore settings we changed for rendering purposes 
                dataGrid.DataSource = null;
                if (autoGenColumnsChanged) {
                    dataGrid.AutoGenerateColumns = false;
                } 
                if (dataKeyFieldChanged == true) {
                    dataGrid.DataKeyField = oldDataKeyField; 
                } 
                if (dataSourceIDChanged == true) {
                    dataGrid.DataSourceID = oldDataSourceID; 
                }
            }
            return designTimeHTML;
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetEmptyDesignTimeHtml"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        protected override string GetEmptyDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml(null);
        }

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetErrorDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected override string GetErrorDesignTimeHtml(Exception e) { 
            Debug.Fail(e.ToString());
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRendering)); 
        }

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetTemplateContainerDataItemProperty"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the template's container's data item property. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override string GetTemplateContainerDataItemProperty(string templateName) {
            return "DataItem";
        }
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetTemplateContent"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets the template's content.
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public override string GetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, out bool allowEditing) {
            allowEditing = true; 
            DataGrid dataGrid = (DataGrid)Component;
 
            int columnIndex = editingFrame.Verb.Index; 

            Debug.Assert((columnIndex >= 0) && (columnIndex < dataGrid.Columns.Count), 
                         "Invalid column index in template editing frame.");
            Debug.Assert(dataGrid.Columns[columnIndex] is TemplateColumn,
                         "Template editing frame points to a non-TemplateColumn column.");
 
            TemplateColumn column = (TemplateColumn)dataGrid.Columns[columnIndex];
            ITemplate template = null; 
            string templateContent = String.Empty; 

            if (templateName.Equals(ColumnTemplateNames[IDX_HEADER_TEMPLATE])) { 
                template = column.HeaderTemplate;
            }
            else if (templateName.Equals(ColumnTemplateNames[IDX_ITEM_TEMPLATE])) {
                template = column.ItemTemplate; 
            }
            else if (templateName.Equals(ColumnTemplateNames[IDX_EDITITEM_TEMPLATE])) { 
                template = column.EditItemTemplate; 
            }
            else if (templateName.Equals(ColumnTemplateNames[IDX_FOOTER_TEMPLATE])) { 
                template = column.FooterTemplate;
            }
            else {
                Debug.Fail("Unknown template name passed to GetTemplateContent"); 
            }
 
            if (template != null) { 
                templateContent = GetTextFromTemplate(template);
            } 

            return templateContent;
        }
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.GetTemplatePropertyParentType"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override Type GetTemplatePropertyParentType(string templateName) { 
            return typeof(TemplateColumn);
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.OnColumnsChanged"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when the columns changed event occurs.
        ///    </para> 
        /// </devdoc> 
        public virtual void OnColumnsChanged() {
            OnTemplateEditingVerbsChanged(); 
        }

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.OnTemplateEditingVerbsChanged"]/*' />
        protected override void OnTemplateEditingVerbsChanged() { 
            templateVerbsDirty = true;
        } 
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///   Initializes the designer with the DataGrid control that this instance
        ///   of the designer is associated with.
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(DataGrid));
 
            base.Initialize(component); 
        }
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.SetTemplateContent"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Sets the content for the specified template and frame. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override void SetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, string templateContent) {
            int columnIndex = editingFrame.Verb.Index; 
            DataGrid dataGrid = (DataGrid)Component;

            Debug.Assert((columnIndex >= 0) && (columnIndex < dataGrid.Columns.Count),
                         "Invalid column index in template editing frame."); 
            Debug.Assert(dataGrid.Columns[columnIndex] is TemplateColumn,
                         "Template editing frame points to a non-TemplateColumn column."); 
 
            TemplateColumn column = (TemplateColumn)dataGrid.Columns[columnIndex];
            ITemplate newTemplate = null; 

            if ((templateContent != null) && (templateContent.Length != 0)) {
                ITemplate currentTemplate = null;
 
                // first get the current template so we can use it if we fail to parse the
                // new text into a template 
 
                if (templateName.Equals(ColumnTemplateNames[IDX_HEADER_TEMPLATE])) {
                    currentTemplate = column.HeaderTemplate; 
                }
                else if (templateName.Equals(ColumnTemplateNames[IDX_ITEM_TEMPLATE])) {
                    currentTemplate = column.ItemTemplate;
                } 
                else if (templateName.Equals(ColumnTemplateNames[IDX_EDITITEM_TEMPLATE])) {
                    currentTemplate = column.EditItemTemplate; 
                } 
                else if (templateName.Equals(ColumnTemplateNames[IDX_FOOTER_TEMPLATE])) {
                    currentTemplate = column.FooterTemplate; 
                }

                // this will parse out a new template, and if it fails, it will
                // return currentTemplate itself 
                newTemplate = GetTemplateFromText(templateContent, currentTemplate);
            } 
 
            // Set the new template into the control. Note this may be null, if the
            // template content was empty, i.e., the user cleared out everything in the UI. 

            if (templateName.Equals(ColumnTemplateNames[IDX_HEADER_TEMPLATE])) {
                column.HeaderTemplate = newTemplate;
            } 
            else if (templateName.Equals(ColumnTemplateNames[IDX_ITEM_TEMPLATE])) {
                column.ItemTemplate = newTemplate; 
            } 
            else if (templateName.Equals(ColumnTemplateNames[IDX_EDITITEM_TEMPLATE])) {
                column.EditItemTemplate = newTemplate; 
            }
            else if (templateName.Equals(ColumnTemplateNames[IDX_FOOTER_TEMPLATE])) {
                column.FooterTemplate = newTemplate;
            } 
            else {
                Debug.Fail("Unknown template name passed to SetTemplateContent"); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
