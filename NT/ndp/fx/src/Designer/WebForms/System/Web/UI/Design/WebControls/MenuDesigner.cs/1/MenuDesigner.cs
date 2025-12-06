//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
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
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Text; 
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    using WebMenu = System.Web.UI.WebControls.Menu;

    /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner"]/*' /> 
    /// <devdoc>
    /// The designer for a Menu control. 
    /// </devdoc> 
    public class MenuDesigner : HierarchicalDataBoundControlDesigner, IDataBindingSchemaProvider {
 
        private WebMenu _menu;

        private TemplateGroupCollection _templateGroups;
 
        private static DesignerAutoFormatCollection _autoFormats;
        private ViewType _currentView; 
 
        private const string _getDesignTimeStaticHtml = "GetDesignTimeStaticHtml";
        private const string _getDesignTimeDynamicHtml = "GetDesignTimeDynamicHtml"; 

        private const string emptyDesignTimeHtml =
            @"
                <table cellpadding=4 cellspacing=0 style=""font-family:Tahoma;font-size:8pt;color:buttontext;background-color:buttonface""> 
                  <tr><td><span style=""font-weight:bold"">Menu</span> - {0}</td></tr>
                  <tr><td>{1}</td></tr> 
                </table> 
             ";
 
        private const string errorDesignTimeHtml =
            @"
                <table cellpadding=4 cellspacing=0 style=""font-family:Tahoma;font-size:8pt;color:buttontext;background-color:buttonface;border: solid 1px;border-top-color:buttonhighlight;border-left-color:buttonhighlight;border-bottom-color:buttonshadow;border-right-color:buttonshadow"">
                  <tr><td><span style=""font-weight:bold"">Menu</span> - {0}</td></tr> 
                  <tr><td>{1}</td></tr>
                </table> 
             "; 

        private const int _maxDesignDepth = 10; 

        private static readonly string[] _templateNames = new string[] {
            "StaticItemTemplate",
            "DynamicItemTemplate", 
        };
 
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new MenuDesignerActionList(this)); 

                return actionLists; 
            } 
        }
 
        public override DesignerAutoFormatCollection AutoFormats {
            get {
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.MENU_SCHEMES, 
                        delegate(DataRow schemeData) { return new MenuAutoFormat(schemeData); });
                } 
                return _autoFormats; 
            }
        } 

        private void ConvertToDynamicTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToDynamicTemplateChangeCallback), null, SR.GetString(SR.MenuDesigner_ConvertToDynamicTemplate));
        } 

        /// <devdoc> 
        /// Transacted change callback to invoke the ConvertToDynamicTemplate operation. 
        ///
        /// Creates a template that looks identical to the current rendering of the control and tells the control 
        /// to use this template.  Allows a page developer to customize the control using its style properties, then
        /// convert to a template and modify the template for further customization.  The template contains the
        /// necessary databinding expressions.
        /// </devdoc> 
        private bool ConvertToDynamicTemplateChangeCallback(object context) {
            string templateText = null; 
 
            string formatString = _menu.DynamicItemFormatString;
            if (formatString != null && formatString.Length != 0) { 
                templateText = "<%# Eval(\"Text\", \"" + formatString + "\") %>";
            }
            else {
                templateText = "<%# Eval(\"Text\") %>"; 
            }
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            // Create template 
            if (host != null) {
                _menu.DynamicItemTemplate = ControlParser.ParseTemplate(host, templateText);
            }
 
            return true;
        } 
 
        private void ConvertToStaticTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToStaticTemplateChangeCallback), null, SR.GetString(SR.MenuDesigner_ConvertToStaticTemplate)); 
        }

        /// <devdoc>
        /// Transacted change callback to invoke the ConvertToStaticTemplate operation. 
        ///
        /// Creates a template that looks identical to the current rendering of the control and tells the control 
        /// to use this template.  Allows a page developer to customize the control using its style properties, then 
        /// convert to a template and modify the template for further customization.  The template contains the
        /// necessary databinding expressions. 
        /// </devdoc>
        private bool ConvertToStaticTemplateChangeCallback(object context) {
            string templateText = null;
 
            string formatString = _menu.StaticItemFormatString;
            if (formatString != null && formatString.Length != 0) { 
                templateText = "<%# Eval(\"Text\", \"" + formatString + "\") %>"; 
            }
            else { 
                templateText = "<%# Eval(\"Text\") %>";
            }

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            // Create template 
            if (host != null) { 
                _menu.StaticItemTemplate = ControlParser.ParseTemplate(host, templateText);
            } 

            return true;
        }
 
        private void ResetDynamicTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetDynamicTemplateChangeCallback), null, SR.GetString(SR.MenuDesigner_ResetDynamicTemplate)); 
        } 

        /// <devdoc> 
        /// Transacted change callback to invoke the Reset operation.
        ///
        /// Removes the user template from the control, causing it to use the default template.
        /// </devdoc> 
        private bool ResetDynamicTemplateChangeCallback(object context) {
            _menu.Controls.Clear(); 
            _menu.DynamicItemTemplate = null; 
            return true;
        } 

        private void ResetStaticTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetStaticTemplateChangeCallback), null, SR.GetString(SR.MenuDesigner_ResetStaticTemplate));
        } 

        /// <devdoc> 
        /// Transacted change callback to invoke the Reset operation. 
        ///
        /// Removes the user template from the control, causing it to use the default template. 
        /// </devdoc>
        private bool ResetStaticTemplateChangeCallback(object context) {
            _menu.Controls.Clear();
            _menu.StaticItemTemplate = null; 
            return true;
        } 
 
        private bool DynamicTemplated {
            get { 
                return _menu.DynamicItemTemplate != null;
            }
        }
 
        private bool StaticTemplated {
            get { 
                return _menu.StaticItemTemplate != null; 
            }
        } 

        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
                if (_templateGroups == null) { 
                    _templateGroups = new TemplateGroupCollection();
 
                    TemplateGroup templateGroup = new TemplateGroup("Item Templates", ((WebControl)ViewControl).ControlStyle);
                    TemplateDefinition staticTemplateDefinition = new TemplateDefinition(
                        this,
                        _templateNames[0], 
                        _menu,
                        _templateNames[0], 
                        ((WebMenu)ViewControl).StaticMenuStyle); 
                    staticTemplateDefinition.SupportsDataBinding = true;
                    templateGroup.AddTemplateDefinition(staticTemplateDefinition); 

                    TemplateDefinition dynamicTemplateDefinition = new TemplateDefinition(
                        this,
                        _templateNames[1], 
                        _menu,
                        _templateNames[1], 
                        ((WebMenu)ViewControl).DynamicMenuStyle); 
                    dynamicTemplateDefinition.SupportsDataBinding = true;
                    templateGroup.AddTemplateDefinition(dynamicTemplateDefinition); 
                    _templateGroups.Add(templateGroup);

                }
 
                groups.AddRange(_templateGroups);
 
                return groups; 
            }
        } 

        protected override bool UsePreviewControl {
            get {
                return true; 
            }
        } 
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.DataBind"]/*' />
        protected override void DataBind(BaseDataBoundControl dataBoundControl) { 
            WebMenu menu = (WebMenu)dataBoundControl;
            if ((menu.DataSourceID != null && menu.DataSourceID.Length > 0) ||
                menu.DataSource != null ||
                menu.Items.Count == 0) { 
                menu.Items.Clear();
                base.DataBind(menu); 
            } 
        }
 
        private void EditBindings() {
            IServiceProvider site = _menu.Site;
            MenuBindingsEditorForm dialog = new MenuBindingsEditorForm(site, _menu, this);
            UIServiceHelper.ShowDialog(site, dialog); 
        }
 
        /// <devdoc> 
        ///     Creates and shows a new node collection editor.
        /// </devdoc> 
        private void EditMenuItems() {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)["Items"];
            Debug.Assert(descriptor != null, "Expected to find Items property on Menu");
            InvokeTransactedChange(Component, new TransactedChangeCallback(EditMenuItemsChangeCallback), null, SR.GetString(SR.MenuDesigner_EditNodesTransactionDescription), descriptor); 
        }
 
        /// <devdoc> 
        /// Transacted change callback to invoke the Edit Nodes dialog.
        /// </devdoc> 
        private bool EditMenuItemsChangeCallback(object context) {
            IServiceProvider site = _menu.Site;
            MenuItemCollectionEditorDialog dialog = new MenuItemCollectionEditorDialog(_menu, this);
            DialogResult result = UIServiceHelper.ShowDialog(site, dialog); 
            return (result == DialogResult.OK);
        } 
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.GetDesignTimeHtml"]/*' />
        public override string GetDesignTimeHtml() { 
            try {
                WebMenu menu = (WebMenu)ViewControl;
                ListDictionary stateDictionary = new ListDictionary();
                stateDictionary.Add("DesignTimeTextWriterType", typeof(DesignTimeHtmlTextWriter)); 
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(stateDictionary);
                int oldDepth = menu.MaximumDynamicDisplayLevels; 
                if (oldDepth > _maxDesignDepth) { 
                    menu.MaximumDynamicDisplayLevels = _maxDesignDepth;
                } 
                DataBind((BaseDataBoundControl)ViewControl);
                IDictionary state = ((IControlDesignerAccessor)ViewControl).GetDesignModeState();
                switch (_currentView) {
                    case ViewType.Dynamic: 
                        return (string)state[_getDesignTimeDynamicHtml];
                    case ViewType.Static: 
                        return (string)state[_getDesignTimeStaticHtml]; 
                }
                if (oldDepth > _maxDesignDepth) { 
                    menu.MaximumDynamicDisplayLevels = oldDepth;
                }
                return base.GetDesignTimeHtml();
            } 
            catch (Exception e) {
                return GetErrorDesignTimeHtml(e); 
            } 
        }
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.GetEmptyDesignTimeHtml"]/*' />
        protected override string GetEmptyDesignTimeHtml() {
            string name = _menu.Site.Name;
            return String.Format(CultureInfo.CurrentUICulture, emptyDesignTimeHtml, name, SR.GetString(SR.MenuDesigner_Empty)); 
        }
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) {
            string name = _menu.Site.Name; 
            return String.Format(CultureInfo.CurrentUICulture, errorDesignTimeHtml, name, SR.GetString(SR.MenuDesigner_Error, e.Message));
        }

        protected override IHierarchicalEnumerable GetSampleDataSource() { 
            return new MenuSampleData(_menu, 0, String.Empty);
        } 
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(WebMenu));
            base.Initialize(component);
            _menu = (WebMenu)component;
 
            SetViewFlags(ViewFlags.TemplateEditing, true);
        } 
 
        internal void InvokeMenuBindingsEditor() {
            EditBindings(); 
        }

        internal void InvokeMenuItemCollectionEditor() {
            EditMenuItems(); 
        }
 
        #region IDataBindingSchemaProvider implementation 
        bool IDataBindingSchemaProvider.CanRefreshSchema {
            get { 
                return CanRefreshSchema;
            }
        }
 
        protected bool CanRefreshSchema {
            get { 
                return false; 
            }
        } 

        IDataSourceViewSchema IDataBindingSchemaProvider.Schema {
            get {
                return Schema; 
            }
        } 
 
        protected IDataSourceViewSchema Schema {
            get { 
                return new MenuItemSchema();
            }
        }
 
        protected void RefreshSchema(bool preferSilent) {
        } 
 
        void IDataBindingSchemaProvider.RefreshSchema(bool preferSilent) {
            RefreshSchema(preferSilent); 
        }
        #endregion

        private enum ViewType { 
            Static = 0,
            Dynamic = 1, 
        } 

        private class MenuDesignerActionList : DesignerActionList { 
            private MenuDesigner _parent;

            public MenuDesignerActionList(MenuDesigner parent) : base(parent.Component) {
                _parent = parent; 
            }
 
            public override bool AutoShow { 
                get {
                    return true; 
                }
                set {
                }
            } 

            [TypeConverter(typeof(MenuViewTypeConverter))] 
            public string View { 
                get {
                    if (_parent._currentView == ViewType.Static) { 
                        return SR.GetString(SR.Menu_StaticView);
                    }
                    else if (_parent._currentView == ViewType.Dynamic) {
                        return SR.GetString(SR.Menu_DynamicView); 
                    }
                    else { 
                        Debug.Fail("Unexpected view value"); 
                    }
 
                    return String.Empty;
                }
                set {
                    if (String.Compare(value, SR.GetString(SR.Menu_StaticView), StringComparison.Ordinal) == 0) { 
                        _parent._currentView = ViewType.Static;
                    } 
                    else if (String.Compare(value, SR.GetString(SR.Menu_DynamicView), StringComparison.Ordinal) == 0) { 
                        _parent._currentView = ViewType.Dynamic;
                    } 
                    else {
                        Debug.Fail("Unexpected view value");
                    }
 
                    // Update the property grid, since the visible properties may have changed if
                    // the view changed between a templated and non-templated view. 
                    TypeDescriptor.Refresh(_parent.Component); 

                    _parent.UpdateDesignTimeHtml(); 
                }

            }
 
            public void ConvertToDynamicTemplate() {
                _parent.ConvertToDynamicTemplate(); 
            } 

            public void ResetDynamicTemplate() { 
                _parent.ResetDynamicTemplate();
            }

            public void ConvertToStaticTemplate() { 
                _parent.ConvertToStaticTemplate();
            } 
 
            public void ResetStaticTemplate() {
                _parent.ResetStaticTemplate(); 
            }

            public void EditBindings() {
                _parent.EditBindings(); 
            }
 
            public void EditMenuItems() { 
                _parent.EditMenuItems();
            } 

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                string actionGroup = SR.GetString(SR.MenuDesigner_DataActionGroup); 

                items.Add(new DesignerActionPropertyItem( 
                    "View", 
                    SR.GetString(SR.WebControls_Views),
                    actionGroup, 
                    SR.GetString(SR.MenuDesigner_ViewsDescription)));

                if (String.IsNullOrEmpty(_parent.DataSourceID)) {
                    items.Add(new DesignerActionMethodItem(this, 
                        "EditMenuItems",
                        SR.GetString(SR.MenuDesigner_EditMenuItems), 
                        actionGroup, 
                        SR.GetString(SR.MenuDesigner_EditMenuItemsDescription),
                        true)); 
                }
                else {
                    items.Add(new DesignerActionMethodItem(this,
                        "EditBindings", 
                        SR.GetString(SR.MenuDesigner_EditBindings),
                        actionGroup, 
                        SR.GetString(SR.MenuDesigner_EditBindingsDescription), 
                        true));
                } 
                if (_parent.DynamicTemplated) {
                    items.Add(new DesignerActionMethodItem(this,
                        "ResetDynamicTemplate",
                        SR.GetString(SR.MenuDesigner_ResetDynamicTemplate), 
                        actionGroup,
                        SR.GetString(SR.MenuDesigner_ResetDynamicTemplateDescription), 
                        true)); 
                }
                else { 
                    items.Add(new DesignerActionMethodItem(this,
                        "ConvertToDynamicTemplate",
                        SR.GetString(SR.MenuDesigner_ConvertToDynamicTemplate),
                        actionGroup, 
                        SR.GetString(SR.MenuDesigner_ConvertToDynamicTemplateDescription),
                        true)); 
                } 
                if (_parent.StaticTemplated) {
                    items.Add(new DesignerActionMethodItem(this, 
                        "ResetStaticTemplate",
                        SR.GetString(SR.MenuDesigner_ResetStaticTemplate),
                        actionGroup,
                        SR.GetString(SR.MenuDesigner_ResetStaticTemplateDescription), 
                        true));
                } 
                else { 
                    items.Add(new DesignerActionMethodItem(this,
                        "ConvertToStaticTemplate", 
                        SR.GetString(SR.MenuDesigner_ConvertToStaticTemplate),
                        actionGroup,
                        SR.GetString(SR.MenuDesigner_ConvertToStaticTemplateDescription),
                        true)); 
                }
 
                return items; 
            }
 
            private class MenuViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    string[] names = new string[2];
 
                    names[0] = SR.GetString(SR.Menu_StaticView);
                    names[1] = SR.GetString(SR.Menu_DynamicView); 
 
                    return new StandardValuesCollection(names);
                } 

                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
                    return true;
                } 

                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
                    return true; 
                }
 
            }
        }

        private class MenuSampleData : IHierarchicalEnumerable { 
            private ArrayList _list;
            private WebMenu _menu; 
 
            public MenuSampleData(WebMenu menu, int depth, string path) {
                _list = new ArrayList(); 
                _menu = menu;
                int maxDepth = _menu.StaticDisplayLevels + _menu.MaximumDynamicDisplayLevels;
                if ((maxDepth < _menu.StaticDisplayLevels) || (maxDepth < _menu.MaximumDynamicDisplayLevels)) {
                    maxDepth = int.MaxValue; 
                }
                if (depth == 0) { 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path, false)); 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path));
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path, false)); 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path, false));
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path, false));
                }
                else if (depth <= _menu.StaticDisplayLevels && depth < _maxDesignDepth) { 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleParent, depth), depth, path));
                } 
                else if (depth < maxDepth && depth < _maxDesignDepth) { 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleLeaf, 1), depth, path));
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleLeaf, 2), depth, path)); 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleLeaf, 3), depth, path));
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleLeaf, 4), depth, path));
                }
            } 

            public IEnumerator GetEnumerator() { 
                return _list.GetEnumerator(); 
            }
 
            public IHierarchyData GetHierarchyData(object enumeratedItem) {
                return (IHierarchyData)enumeratedItem;
            }
        } 

        private class MenuSampleDataNode : IHierarchyData { 
            private string _text; 
            private int _depth;
            private string _path; 
            private WebMenu _menu;
            private bool _hasChildren;

            public MenuSampleDataNode(WebMenu menu, string text, int depth, string path) : this(menu, text, depth, path, true) { } 

            public MenuSampleDataNode(WebMenu menu, string text, int depth, string path, bool hasChildren) { 
                _text = text; 
                _depth = depth;
                _path = path + '\\' + text; 
                _menu = menu;
                _hasChildren = hasChildren;
            }
 
            public bool HasChildren {
                get { 
                    if (!_hasChildren) { 
                        return false;
                    } 
                    int maxDepth = _menu.StaticDisplayLevels + _menu.MaximumDynamicDisplayLevels;
                    if ((maxDepth < _menu.StaticDisplayLevels) || (maxDepth < _menu.MaximumDynamicDisplayLevels)) {
                        maxDepth = int.MaxValue;
                    } 
                    if (_depth < maxDepth && _depth < _maxDesignDepth) {
                        return true; 
                    } 
                    return false;
                } 
            }

            public string Path {
                get { 
                    return _path;
                } 
            } 

            public object Item { 
                get {
                    return this;
                }
            } 

            public string Type { 
                get { 
                    return "SampleData";
                } 
            }

            public override string ToString() {
                return _text; 
            }
 
            public IHierarchicalEnumerable GetChildren() { 
                return new MenuSampleData(_menu, _depth + 1, _path);
            } 

            public IHierarchyData GetParent() {
                return null;
            } 
        }
 
        private class MenuItemSchema : IDataSourceViewSchema { 

            private static IDataSourceFieldSchema[] _fieldSchema; 

            static MenuItemSchema() {
                PropertyDescriptorCollection menuProperties = TypeDescriptor.GetProperties(typeof(System.Web.UI.WebControls.MenuItem));
                _fieldSchema = new IDataSourceFieldSchema[] { 
                    new TypeFieldSchema(menuProperties["DataPath"]),
                    new TypeFieldSchema(menuProperties["Depth"]), 
                    new TypeFieldSchema(menuProperties["Enabled"]), 
                    new TypeFieldSchema(menuProperties["ImageUrl"]),
                    new TypeFieldSchema(menuProperties["NavigateUrl"]), 
                    new TypeFieldSchema(menuProperties["PopOutImageUrl"]),
                    new TypeFieldSchema(menuProperties["Selectable"]),
                    new TypeFieldSchema(menuProperties["Selected"]),
                    new TypeFieldSchema(menuProperties["SeparatorImageUrl"]), 
                    new TypeFieldSchema(menuProperties["Target"]),
                    new TypeFieldSchema(menuProperties["Text"]), 
                    new TypeFieldSchema(menuProperties["ToolTip"]), 
                    new TypeFieldSchema(menuProperties["Value"]),
                    new TypeFieldSchema(menuProperties["ValuePath"]) 
                    };
            }

            public MenuItemSchema() { } 

            string IDataSourceViewSchema.Name { 
                get { 
                    return "MenuItem";
                } 
            }

            IDataSourceViewSchema[] IDataSourceViewSchema.GetChildren() {
                return new IDataSourceViewSchema[] { }; 
            }
 
            IDataSourceFieldSchema[] IDataSourceViewSchema.GetFields() { 
                return _fieldSchema;
            } 
        }
    }
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
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
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Text; 
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    using WebMenu = System.Web.UI.WebControls.Menu;

    /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner"]/*' /> 
    /// <devdoc>
    /// The designer for a Menu control. 
    /// </devdoc> 
    public class MenuDesigner : HierarchicalDataBoundControlDesigner, IDataBindingSchemaProvider {
 
        private WebMenu _menu;

        private TemplateGroupCollection _templateGroups;
 
        private static DesignerAutoFormatCollection _autoFormats;
        private ViewType _currentView; 
 
        private const string _getDesignTimeStaticHtml = "GetDesignTimeStaticHtml";
        private const string _getDesignTimeDynamicHtml = "GetDesignTimeDynamicHtml"; 

        private const string emptyDesignTimeHtml =
            @"
                <table cellpadding=4 cellspacing=0 style=""font-family:Tahoma;font-size:8pt;color:buttontext;background-color:buttonface""> 
                  <tr><td><span style=""font-weight:bold"">Menu</span> - {0}</td></tr>
                  <tr><td>{1}</td></tr> 
                </table> 
             ";
 
        private const string errorDesignTimeHtml =
            @"
                <table cellpadding=4 cellspacing=0 style=""font-family:Tahoma;font-size:8pt;color:buttontext;background-color:buttonface;border: solid 1px;border-top-color:buttonhighlight;border-left-color:buttonhighlight;border-bottom-color:buttonshadow;border-right-color:buttonshadow"">
                  <tr><td><span style=""font-weight:bold"">Menu</span> - {0}</td></tr> 
                  <tr><td>{1}</td></tr>
                </table> 
             "; 

        private const int _maxDesignDepth = 10; 

        private static readonly string[] _templateNames = new string[] {
            "StaticItemTemplate",
            "DynamicItemTemplate", 
        };
 
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new MenuDesignerActionList(this)); 

                return actionLists; 
            } 
        }
 
        public override DesignerAutoFormatCollection AutoFormats {
            get {
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.MENU_SCHEMES, 
                        delegate(DataRow schemeData) { return new MenuAutoFormat(schemeData); });
                } 
                return _autoFormats; 
            }
        } 

        private void ConvertToDynamicTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToDynamicTemplateChangeCallback), null, SR.GetString(SR.MenuDesigner_ConvertToDynamicTemplate));
        } 

        /// <devdoc> 
        /// Transacted change callback to invoke the ConvertToDynamicTemplate operation. 
        ///
        /// Creates a template that looks identical to the current rendering of the control and tells the control 
        /// to use this template.  Allows a page developer to customize the control using its style properties, then
        /// convert to a template and modify the template for further customization.  The template contains the
        /// necessary databinding expressions.
        /// </devdoc> 
        private bool ConvertToDynamicTemplateChangeCallback(object context) {
            string templateText = null; 
 
            string formatString = _menu.DynamicItemFormatString;
            if (formatString != null && formatString.Length != 0) { 
                templateText = "<%# Eval(\"Text\", \"" + formatString + "\") %>";
            }
            else {
                templateText = "<%# Eval(\"Text\") %>"; 
            }
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            // Create template 
            if (host != null) {
                _menu.DynamicItemTemplate = ControlParser.ParseTemplate(host, templateText);
            }
 
            return true;
        } 
 
        private void ConvertToStaticTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToStaticTemplateChangeCallback), null, SR.GetString(SR.MenuDesigner_ConvertToStaticTemplate)); 
        }

        /// <devdoc>
        /// Transacted change callback to invoke the ConvertToStaticTemplate operation. 
        ///
        /// Creates a template that looks identical to the current rendering of the control and tells the control 
        /// to use this template.  Allows a page developer to customize the control using its style properties, then 
        /// convert to a template and modify the template for further customization.  The template contains the
        /// necessary databinding expressions. 
        /// </devdoc>
        private bool ConvertToStaticTemplateChangeCallback(object context) {
            string templateText = null;
 
            string formatString = _menu.StaticItemFormatString;
            if (formatString != null && formatString.Length != 0) { 
                templateText = "<%# Eval(\"Text\", \"" + formatString + "\") %>"; 
            }
            else { 
                templateText = "<%# Eval(\"Text\") %>";
            }

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            // Create template 
            if (host != null) { 
                _menu.StaticItemTemplate = ControlParser.ParseTemplate(host, templateText);
            } 

            return true;
        }
 
        private void ResetDynamicTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetDynamicTemplateChangeCallback), null, SR.GetString(SR.MenuDesigner_ResetDynamicTemplate)); 
        } 

        /// <devdoc> 
        /// Transacted change callback to invoke the Reset operation.
        ///
        /// Removes the user template from the control, causing it to use the default template.
        /// </devdoc> 
        private bool ResetDynamicTemplateChangeCallback(object context) {
            _menu.Controls.Clear(); 
            _menu.DynamicItemTemplate = null; 
            return true;
        } 

        private void ResetStaticTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetStaticTemplateChangeCallback), null, SR.GetString(SR.MenuDesigner_ResetStaticTemplate));
        } 

        /// <devdoc> 
        /// Transacted change callback to invoke the Reset operation. 
        ///
        /// Removes the user template from the control, causing it to use the default template. 
        /// </devdoc>
        private bool ResetStaticTemplateChangeCallback(object context) {
            _menu.Controls.Clear();
            _menu.StaticItemTemplate = null; 
            return true;
        } 
 
        private bool DynamicTemplated {
            get { 
                return _menu.DynamicItemTemplate != null;
            }
        }
 
        private bool StaticTemplated {
            get { 
                return _menu.StaticItemTemplate != null; 
            }
        } 

        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
                if (_templateGroups == null) { 
                    _templateGroups = new TemplateGroupCollection();
 
                    TemplateGroup templateGroup = new TemplateGroup("Item Templates", ((WebControl)ViewControl).ControlStyle);
                    TemplateDefinition staticTemplateDefinition = new TemplateDefinition(
                        this,
                        _templateNames[0], 
                        _menu,
                        _templateNames[0], 
                        ((WebMenu)ViewControl).StaticMenuStyle); 
                    staticTemplateDefinition.SupportsDataBinding = true;
                    templateGroup.AddTemplateDefinition(staticTemplateDefinition); 

                    TemplateDefinition dynamicTemplateDefinition = new TemplateDefinition(
                        this,
                        _templateNames[1], 
                        _menu,
                        _templateNames[1], 
                        ((WebMenu)ViewControl).DynamicMenuStyle); 
                    dynamicTemplateDefinition.SupportsDataBinding = true;
                    templateGroup.AddTemplateDefinition(dynamicTemplateDefinition); 
                    _templateGroups.Add(templateGroup);

                }
 
                groups.AddRange(_templateGroups);
 
                return groups; 
            }
        } 

        protected override bool UsePreviewControl {
            get {
                return true; 
            }
        } 
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.DataBind"]/*' />
        protected override void DataBind(BaseDataBoundControl dataBoundControl) { 
            WebMenu menu = (WebMenu)dataBoundControl;
            if ((menu.DataSourceID != null && menu.DataSourceID.Length > 0) ||
                menu.DataSource != null ||
                menu.Items.Count == 0) { 
                menu.Items.Clear();
                base.DataBind(menu); 
            } 
        }
 
        private void EditBindings() {
            IServiceProvider site = _menu.Site;
            MenuBindingsEditorForm dialog = new MenuBindingsEditorForm(site, _menu, this);
            UIServiceHelper.ShowDialog(site, dialog); 
        }
 
        /// <devdoc> 
        ///     Creates and shows a new node collection editor.
        /// </devdoc> 
        private void EditMenuItems() {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)["Items"];
            Debug.Assert(descriptor != null, "Expected to find Items property on Menu");
            InvokeTransactedChange(Component, new TransactedChangeCallback(EditMenuItemsChangeCallback), null, SR.GetString(SR.MenuDesigner_EditNodesTransactionDescription), descriptor); 
        }
 
        /// <devdoc> 
        /// Transacted change callback to invoke the Edit Nodes dialog.
        /// </devdoc> 
        private bool EditMenuItemsChangeCallback(object context) {
            IServiceProvider site = _menu.Site;
            MenuItemCollectionEditorDialog dialog = new MenuItemCollectionEditorDialog(_menu, this);
            DialogResult result = UIServiceHelper.ShowDialog(site, dialog); 
            return (result == DialogResult.OK);
        } 
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.GetDesignTimeHtml"]/*' />
        public override string GetDesignTimeHtml() { 
            try {
                WebMenu menu = (WebMenu)ViewControl;
                ListDictionary stateDictionary = new ListDictionary();
                stateDictionary.Add("DesignTimeTextWriterType", typeof(DesignTimeHtmlTextWriter)); 
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(stateDictionary);
                int oldDepth = menu.MaximumDynamicDisplayLevels; 
                if (oldDepth > _maxDesignDepth) { 
                    menu.MaximumDynamicDisplayLevels = _maxDesignDepth;
                } 
                DataBind((BaseDataBoundControl)ViewControl);
                IDictionary state = ((IControlDesignerAccessor)ViewControl).GetDesignModeState();
                switch (_currentView) {
                    case ViewType.Dynamic: 
                        return (string)state[_getDesignTimeDynamicHtml];
                    case ViewType.Static: 
                        return (string)state[_getDesignTimeStaticHtml]; 
                }
                if (oldDepth > _maxDesignDepth) { 
                    menu.MaximumDynamicDisplayLevels = oldDepth;
                }
                return base.GetDesignTimeHtml();
            } 
            catch (Exception e) {
                return GetErrorDesignTimeHtml(e); 
            } 
        }
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.GetEmptyDesignTimeHtml"]/*' />
        protected override string GetEmptyDesignTimeHtml() {
            string name = _menu.Site.Name;
            return String.Format(CultureInfo.CurrentUICulture, emptyDesignTimeHtml, name, SR.GetString(SR.MenuDesigner_Empty)); 
        }
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) {
            string name = _menu.Site.Name; 
            return String.Format(CultureInfo.CurrentUICulture, errorDesignTimeHtml, name, SR.GetString(SR.MenuDesigner_Error, e.Message));
        }

        protected override IHierarchicalEnumerable GetSampleDataSource() { 
            return new MenuSampleData(_menu, 0, String.Empty);
        } 
 
        /// <include file='doc\MenuDesigner.uex' path='docs/doc[@for="MenuDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(WebMenu));
            base.Initialize(component);
            _menu = (WebMenu)component;
 
            SetViewFlags(ViewFlags.TemplateEditing, true);
        } 
 
        internal void InvokeMenuBindingsEditor() {
            EditBindings(); 
        }

        internal void InvokeMenuItemCollectionEditor() {
            EditMenuItems(); 
        }
 
        #region IDataBindingSchemaProvider implementation 
        bool IDataBindingSchemaProvider.CanRefreshSchema {
            get { 
                return CanRefreshSchema;
            }
        }
 
        protected bool CanRefreshSchema {
            get { 
                return false; 
            }
        } 

        IDataSourceViewSchema IDataBindingSchemaProvider.Schema {
            get {
                return Schema; 
            }
        } 
 
        protected IDataSourceViewSchema Schema {
            get { 
                return new MenuItemSchema();
            }
        }
 
        protected void RefreshSchema(bool preferSilent) {
        } 
 
        void IDataBindingSchemaProvider.RefreshSchema(bool preferSilent) {
            RefreshSchema(preferSilent); 
        }
        #endregion

        private enum ViewType { 
            Static = 0,
            Dynamic = 1, 
        } 

        private class MenuDesignerActionList : DesignerActionList { 
            private MenuDesigner _parent;

            public MenuDesignerActionList(MenuDesigner parent) : base(parent.Component) {
                _parent = parent; 
            }
 
            public override bool AutoShow { 
                get {
                    return true; 
                }
                set {
                }
            } 

            [TypeConverter(typeof(MenuViewTypeConverter))] 
            public string View { 
                get {
                    if (_parent._currentView == ViewType.Static) { 
                        return SR.GetString(SR.Menu_StaticView);
                    }
                    else if (_parent._currentView == ViewType.Dynamic) {
                        return SR.GetString(SR.Menu_DynamicView); 
                    }
                    else { 
                        Debug.Fail("Unexpected view value"); 
                    }
 
                    return String.Empty;
                }
                set {
                    if (String.Compare(value, SR.GetString(SR.Menu_StaticView), StringComparison.Ordinal) == 0) { 
                        _parent._currentView = ViewType.Static;
                    } 
                    else if (String.Compare(value, SR.GetString(SR.Menu_DynamicView), StringComparison.Ordinal) == 0) { 
                        _parent._currentView = ViewType.Dynamic;
                    } 
                    else {
                        Debug.Fail("Unexpected view value");
                    }
 
                    // Update the property grid, since the visible properties may have changed if
                    // the view changed between a templated and non-templated view. 
                    TypeDescriptor.Refresh(_parent.Component); 

                    _parent.UpdateDesignTimeHtml(); 
                }

            }
 
            public void ConvertToDynamicTemplate() {
                _parent.ConvertToDynamicTemplate(); 
            } 

            public void ResetDynamicTemplate() { 
                _parent.ResetDynamicTemplate();
            }

            public void ConvertToStaticTemplate() { 
                _parent.ConvertToStaticTemplate();
            } 
 
            public void ResetStaticTemplate() {
                _parent.ResetStaticTemplate(); 
            }

            public void EditBindings() {
                _parent.EditBindings(); 
            }
 
            public void EditMenuItems() { 
                _parent.EditMenuItems();
            } 

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                string actionGroup = SR.GetString(SR.MenuDesigner_DataActionGroup); 

                items.Add(new DesignerActionPropertyItem( 
                    "View", 
                    SR.GetString(SR.WebControls_Views),
                    actionGroup, 
                    SR.GetString(SR.MenuDesigner_ViewsDescription)));

                if (String.IsNullOrEmpty(_parent.DataSourceID)) {
                    items.Add(new DesignerActionMethodItem(this, 
                        "EditMenuItems",
                        SR.GetString(SR.MenuDesigner_EditMenuItems), 
                        actionGroup, 
                        SR.GetString(SR.MenuDesigner_EditMenuItemsDescription),
                        true)); 
                }
                else {
                    items.Add(new DesignerActionMethodItem(this,
                        "EditBindings", 
                        SR.GetString(SR.MenuDesigner_EditBindings),
                        actionGroup, 
                        SR.GetString(SR.MenuDesigner_EditBindingsDescription), 
                        true));
                } 
                if (_parent.DynamicTemplated) {
                    items.Add(new DesignerActionMethodItem(this,
                        "ResetDynamicTemplate",
                        SR.GetString(SR.MenuDesigner_ResetDynamicTemplate), 
                        actionGroup,
                        SR.GetString(SR.MenuDesigner_ResetDynamicTemplateDescription), 
                        true)); 
                }
                else { 
                    items.Add(new DesignerActionMethodItem(this,
                        "ConvertToDynamicTemplate",
                        SR.GetString(SR.MenuDesigner_ConvertToDynamicTemplate),
                        actionGroup, 
                        SR.GetString(SR.MenuDesigner_ConvertToDynamicTemplateDescription),
                        true)); 
                } 
                if (_parent.StaticTemplated) {
                    items.Add(new DesignerActionMethodItem(this, 
                        "ResetStaticTemplate",
                        SR.GetString(SR.MenuDesigner_ResetStaticTemplate),
                        actionGroup,
                        SR.GetString(SR.MenuDesigner_ResetStaticTemplateDescription), 
                        true));
                } 
                else { 
                    items.Add(new DesignerActionMethodItem(this,
                        "ConvertToStaticTemplate", 
                        SR.GetString(SR.MenuDesigner_ConvertToStaticTemplate),
                        actionGroup,
                        SR.GetString(SR.MenuDesigner_ConvertToStaticTemplateDescription),
                        true)); 
                }
 
                return items; 
            }
 
            private class MenuViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    string[] names = new string[2];
 
                    names[0] = SR.GetString(SR.Menu_StaticView);
                    names[1] = SR.GetString(SR.Menu_DynamicView); 
 
                    return new StandardValuesCollection(names);
                } 

                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
                    return true;
                } 

                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
                    return true; 
                }
 
            }
        }

        private class MenuSampleData : IHierarchicalEnumerable { 
            private ArrayList _list;
            private WebMenu _menu; 
 
            public MenuSampleData(WebMenu menu, int depth, string path) {
                _list = new ArrayList(); 
                _menu = menu;
                int maxDepth = _menu.StaticDisplayLevels + _menu.MaximumDynamicDisplayLevels;
                if ((maxDepth < _menu.StaticDisplayLevels) || (maxDepth < _menu.MaximumDynamicDisplayLevels)) {
                    maxDepth = int.MaxValue; 
                }
                if (depth == 0) { 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path, false)); 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path));
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path, false)); 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path, false));
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleRoot), depth, path, false));
                }
                else if (depth <= _menu.StaticDisplayLevels && depth < _maxDesignDepth) { 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleParent, depth), depth, path));
                } 
                else if (depth < maxDepth && depth < _maxDesignDepth) { 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleLeaf, 1), depth, path));
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleLeaf, 2), depth, path)); 
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleLeaf, 3), depth, path));
                    _list.Add(new MenuSampleDataNode(_menu, SR.GetString(SR.HierarchicalDataBoundControlDesigner_SampleLeaf, 4), depth, path));
                }
            } 

            public IEnumerator GetEnumerator() { 
                return _list.GetEnumerator(); 
            }
 
            public IHierarchyData GetHierarchyData(object enumeratedItem) {
                return (IHierarchyData)enumeratedItem;
            }
        } 

        private class MenuSampleDataNode : IHierarchyData { 
            private string _text; 
            private int _depth;
            private string _path; 
            private WebMenu _menu;
            private bool _hasChildren;

            public MenuSampleDataNode(WebMenu menu, string text, int depth, string path) : this(menu, text, depth, path, true) { } 

            public MenuSampleDataNode(WebMenu menu, string text, int depth, string path, bool hasChildren) { 
                _text = text; 
                _depth = depth;
                _path = path + '\\' + text; 
                _menu = menu;
                _hasChildren = hasChildren;
            }
 
            public bool HasChildren {
                get { 
                    if (!_hasChildren) { 
                        return false;
                    } 
                    int maxDepth = _menu.StaticDisplayLevels + _menu.MaximumDynamicDisplayLevels;
                    if ((maxDepth < _menu.StaticDisplayLevels) || (maxDepth < _menu.MaximumDynamicDisplayLevels)) {
                        maxDepth = int.MaxValue;
                    } 
                    if (_depth < maxDepth && _depth < _maxDesignDepth) {
                        return true; 
                    } 
                    return false;
                } 
            }

            public string Path {
                get { 
                    return _path;
                } 
            } 

            public object Item { 
                get {
                    return this;
                }
            } 

            public string Type { 
                get { 
                    return "SampleData";
                } 
            }

            public override string ToString() {
                return _text; 
            }
 
            public IHierarchicalEnumerable GetChildren() { 
                return new MenuSampleData(_menu, _depth + 1, _path);
            } 

            public IHierarchyData GetParent() {
                return null;
            } 
        }
 
        private class MenuItemSchema : IDataSourceViewSchema { 

            private static IDataSourceFieldSchema[] _fieldSchema; 

            static MenuItemSchema() {
                PropertyDescriptorCollection menuProperties = TypeDescriptor.GetProperties(typeof(System.Web.UI.WebControls.MenuItem));
                _fieldSchema = new IDataSourceFieldSchema[] { 
                    new TypeFieldSchema(menuProperties["DataPath"]),
                    new TypeFieldSchema(menuProperties["Depth"]), 
                    new TypeFieldSchema(menuProperties["Enabled"]), 
                    new TypeFieldSchema(menuProperties["ImageUrl"]),
                    new TypeFieldSchema(menuProperties["NavigateUrl"]), 
                    new TypeFieldSchema(menuProperties["PopOutImageUrl"]),
                    new TypeFieldSchema(menuProperties["Selectable"]),
                    new TypeFieldSchema(menuProperties["Selected"]),
                    new TypeFieldSchema(menuProperties["SeparatorImageUrl"]), 
                    new TypeFieldSchema(menuProperties["Target"]),
                    new TypeFieldSchema(menuProperties["Text"]), 
                    new TypeFieldSchema(menuProperties["ToolTip"]), 
                    new TypeFieldSchema(menuProperties["Value"]),
                    new TypeFieldSchema(menuProperties["ValuePath"]) 
                    };
            }

            public MenuItemSchema() { } 

            string IDataSourceViewSchema.Name { 
                get { 
                    return "MenuItem";
                } 
            }

            IDataSourceViewSchema[] IDataSourceViewSchema.GetChildren() {
                return new IDataSourceViewSchema[] { }; 
            }
 
            IDataSourceFieldSchema[] IDataSourceViewSchema.GetFields() { 
                return _fieldSchema;
            } 
        }
    }
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
