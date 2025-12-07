//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
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
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms; 

    using WebTreeView = System.Web.UI.WebControls.TreeView; 


    /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner"]/*' />
    /// <devdoc> 
    /// The designer for a TreeView control.
    /// </devdoc> 
    public class TreeViewDesigner : HierarchicalDataBoundControlDesigner { 

        private WebTreeView _treeView; 

        private bool _usingSampleData;
        private bool _emptyDataBinding;
 
        private static DesignerAutoFormatCollection _autoFormats;
 
        private const string emptyDesignTimeHtml = 
            @"
                <table cellpadding=4 cellspacing=0 style=""font-family:Tahoma;font-size:8pt;color:buttontext;background-color:buttonface""> 
                  <tr><td><span style=""font-weight:bold"">TreeView</span> - {0}</td></tr>
                  <tr><td>{1}</td></tr>
                </table>
             "; 

        private const string errorDesignTimeHtml = 
            @" 
                <table cellpadding=4 cellspacing=0 style=""font-family:Tahoma;font-size:8pt;color:buttontext;background-color:buttonface;border: solid 1px;border-top-color:buttonhighlight;border-left-color:buttonhighlight;border-bottom-color:buttonshadow;border-right-color:buttonshadow"">
                  <tr><td><span style=""font-weight:bold"">TreeView</span> - {0}</td></tr> 
                  <tr><td>{1}</td></tr>
                </table>
             ";
 
        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new TreeViewDesignerActionList(this));

                return actionLists;
            } 
        }
 
        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.TREEVIEW_SCHEMES,
                        delegate(DataRow schemeData) { return new BaseAutoFormat(schemeData); });
                }
                return _autoFormats; 
            }
        } 
 
        protected override bool UsePreviewControl {
            get { 
                return true;
            }
        }
 
        /// <devdoc>
        ///     Creates and shows a new line image generator. 
        /// </devdoc> 
        protected void CreateLineImages() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(CreateLineImagesCallBack), null, SR.GetString(SR.TreeViewDesigner_CreateLineImagesTransactionDescription)); 
        }

        private bool CreateLineImagesCallBack(object context) {
            TreeViewImageGenerator generator = new TreeViewImageGenerator(_treeView); 
            return (UIServiceHelper.ShowDialog(Component.Site, generator) == DialogResult.OK);
        } 
 
        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.DataBind"]/*' />
        protected override void DataBind(BaseDataBoundControl dataBoundControl) { 
            WebTreeView treeView = (WebTreeView)dataBoundControl;
            _usingSampleData = false;
            _emptyDataBinding = false;
            if ((treeView.DataSourceID != null && treeView.DataSourceID.Length > 0) || 
                treeView.DataSource != null ||
                treeView.Nodes.Count == 0) { 
                treeView.Nodes.Clear(); 
                base.DataBind(treeView);
            } 

            if (_usingSampleData) {
                treeView.ExpandAll();
            } 
            else {
                ExpandToDepth(treeView.Nodes, treeView.ExpandDepth); 
                if (treeView.Nodes.Count == 0) { 
                    _emptyDataBinding = true;
                } 
            }
        }

        protected void EditBindings() { 
            IServiceProvider site = _treeView.Site;
            TreeViewBindingsEditorForm dialog = new TreeViewBindingsEditorForm(site, _treeView, this); 
            UIServiceHelper.ShowDialog(site, dialog); 
        }
 
        /// <devdoc>
        ///     Creates and shows a new node collection editor.
        /// </devdoc>
        protected void EditNodes() { 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)["Nodes"];
            Debug.Assert(descriptor != null, "Expected to find Nodes property on TreeView"); 
            InvokeTransactedChange(Component, new TransactedChangeCallback(EditNodesChangeCallback), null, SR.GetString(SR.TreeViewDesigner_EditNodesTransactionDescription), descriptor); 
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the Edit Nodes dialog.
        /// </devdoc>
        private bool EditNodesChangeCallback(object context) { 
            IServiceProvider site = _treeView.Site;
            TreeNodeCollectionEditorDialog dialog = new TreeNodeCollectionEditorDialog(_treeView, this); 
            DialogResult result = UIServiceHelper.ShowDialog(site, dialog); 
            return (result == DialogResult.OK);
        } 

        private void ExpandToDepth(System.Web.UI.WebControls.TreeNodeCollection nodes, int depth) {
            foreach (System.Web.UI.WebControls.TreeNode node in nodes) {
                if ((node.Expanded != false) && ((depth == -1) || (node.Depth < depth))) { 
                    node.Expanded = true;
                    ExpandToDepth(node.ChildNodes, depth); 
                } 
            }
        } 

        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetDesignTimeHierarchicalDataSource"]/*' />
        protected override IHierarchicalEnumerable GetSampleDataSource() {
            _usingSampleData = true; 
            ((WebTreeView)ViewControl).AutoGenerateDataBindings = true;
 
            return base.GetSampleDataSource(); 
        }
 
        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetDesignTimeHtml"]/*' />
        public override string GetDesignTimeHtml() {
            // Save the HTML temporarily
            string designTimeHtml = base.GetDesignTimeHtml(); 

            if (_emptyDataBinding) { 
                designTimeHtml = GetEmptyDataBindingDesignTimeHtml(); 
            }
 
            return designTimeHtml;
        }

        private string GetEmptyDataBindingDesignTimeHtml() { 
            string name = _treeView.Site.Name;
            return String.Format(CultureInfo.CurrentUICulture, emptyDesignTimeHtml, name, SR.GetString(SR.TreeViewDesigner_EmptyDataBinding)); 
        } 

        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetEmptyDesignTimeHtml"]/*' /> 
        protected override string GetEmptyDesignTimeHtml() {
            string name = _treeView.Site.Name;
            return String.Format(CultureInfo.CurrentUICulture, emptyDesignTimeHtml, name, SR.GetString(SR.TreeViewDesigner_Empty));
        } 

        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) { 
            string name = _treeView.Site.Name;
            return String.Format(CultureInfo.CurrentUICulture, errorDesignTimeHtml, name, SR.GetString(SR.TreeViewDesigner_Error, e.Message)); 
        }

        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(WebTreeView));
            base.Initialize(component); 
            _treeView = (WebTreeView)component; 
        }
 
        internal void InvokeTreeNodeCollectionEditor() {
            EditNodes();
        }
 
        internal void InvokeTreeViewBindingsEditor() {
            EditBindings(); 
        } 

        private class TreeViewDesignerActionList : DesignerActionList { 
            private TreeViewDesigner _parent;

            public TreeViewDesignerActionList(TreeViewDesigner parent) : base (parent.Component) {
                _parent = parent; 
            }
 
            public override bool AutoShow { 
                get {
                    return true; 
                }
                set {
                }
            } 

            public bool ShowLines { 
                get { 
                    return ((WebTreeView)Component).ShowLines;
                } 
                set {
                    PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(WebTreeView))["ShowLines"];
                    propDesc.SetValue(Component, value);
                    TypeDescriptor.Refresh(Component); 
                }
            } 
 
            public void CreateLineImages() {
                _parent.CreateLineImages(); 
            }

            public void EditBindings() {
                _parent.EditBindings(); 
            }
 
            public void EditNodes() { 
                _parent.EditNodes();
            } 

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                string actionGroup = SR.GetString(SR.TreeViewDesigner_DataActionGroup); 

                if (String.IsNullOrEmpty(_parent.DataSourceID)) { 
                    items.Add(new DesignerActionMethodItem(this, 
                        "EditNodes",
                        SR.GetString(SR.TreeViewDesigner_EditNodes), 
                        actionGroup,
                        SR.GetString(SR.TreeViewDesigner_EditNodesDescription),
                        true));
                } 
                else {
                    items.Add(new DesignerActionMethodItem(this, 
                        "EditBindings", 
                        SR.GetString(SR.TreeViewDesigner_EditBindings),
                        actionGroup, 
                        SR.GetString(SR.TreeViewDesigner_EditBindingsDescription),
                        true));
                }
 
                if (ShowLines) {
                    items.Add(new DesignerActionMethodItem(this, 
                        "CreateLineImages", 
                        SR.GetString(SR.TreeViewDesigner_CreateLineImages),
                        actionGroup, 
                        SR.GetString(SR.TreeViewDesigner_CreateLineImagesDescription),
                        true));
                }
 
                items.Add(new DesignerActionPropertyItem("ShowLines",
                    SR.GetString(SR.TreeViewDesigner_ShowLines), 
                    "Actions", 
                    SR.GetString(SR.TreeViewDesigner_ShowLinesDescription)));
 
                return items;
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
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms; 

    using WebTreeView = System.Web.UI.WebControls.TreeView; 


    /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner"]/*' />
    /// <devdoc> 
    /// The designer for a TreeView control.
    /// </devdoc> 
    public class TreeViewDesigner : HierarchicalDataBoundControlDesigner { 

        private WebTreeView _treeView; 

        private bool _usingSampleData;
        private bool _emptyDataBinding;
 
        private static DesignerAutoFormatCollection _autoFormats;
 
        private const string emptyDesignTimeHtml = 
            @"
                <table cellpadding=4 cellspacing=0 style=""font-family:Tahoma;font-size:8pt;color:buttontext;background-color:buttonface""> 
                  <tr><td><span style=""font-weight:bold"">TreeView</span> - {0}</td></tr>
                  <tr><td>{1}</td></tr>
                </table>
             "; 

        private const string errorDesignTimeHtml = 
            @" 
                <table cellpadding=4 cellspacing=0 style=""font-family:Tahoma;font-size:8pt;color:buttontext;background-color:buttonface;border: solid 1px;border-top-color:buttonhighlight;border-left-color:buttonhighlight;border-bottom-color:buttonshadow;border-right-color:buttonshadow"">
                  <tr><td><span style=""font-weight:bold"">TreeView</span> - {0}</td></tr> 
                  <tr><td>{1}</td></tr>
                </table>
             ";
 
        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new TreeViewDesignerActionList(this));

                return actionLists;
            } 
        }
 
        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.TREEVIEW_SCHEMES,
                        delegate(DataRow schemeData) { return new BaseAutoFormat(schemeData); });
                }
                return _autoFormats; 
            }
        } 
 
        protected override bool UsePreviewControl {
            get { 
                return true;
            }
        }
 
        /// <devdoc>
        ///     Creates and shows a new line image generator. 
        /// </devdoc> 
        protected void CreateLineImages() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(CreateLineImagesCallBack), null, SR.GetString(SR.TreeViewDesigner_CreateLineImagesTransactionDescription)); 
        }

        private bool CreateLineImagesCallBack(object context) {
            TreeViewImageGenerator generator = new TreeViewImageGenerator(_treeView); 
            return (UIServiceHelper.ShowDialog(Component.Site, generator) == DialogResult.OK);
        } 
 
        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.DataBind"]/*' />
        protected override void DataBind(BaseDataBoundControl dataBoundControl) { 
            WebTreeView treeView = (WebTreeView)dataBoundControl;
            _usingSampleData = false;
            _emptyDataBinding = false;
            if ((treeView.DataSourceID != null && treeView.DataSourceID.Length > 0) || 
                treeView.DataSource != null ||
                treeView.Nodes.Count == 0) { 
                treeView.Nodes.Clear(); 
                base.DataBind(treeView);
            } 

            if (_usingSampleData) {
                treeView.ExpandAll();
            } 
            else {
                ExpandToDepth(treeView.Nodes, treeView.ExpandDepth); 
                if (treeView.Nodes.Count == 0) { 
                    _emptyDataBinding = true;
                } 
            }
        }

        protected void EditBindings() { 
            IServiceProvider site = _treeView.Site;
            TreeViewBindingsEditorForm dialog = new TreeViewBindingsEditorForm(site, _treeView, this); 
            UIServiceHelper.ShowDialog(site, dialog); 
        }
 
        /// <devdoc>
        ///     Creates and shows a new node collection editor.
        /// </devdoc>
        protected void EditNodes() { 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)["Nodes"];
            Debug.Assert(descriptor != null, "Expected to find Nodes property on TreeView"); 
            InvokeTransactedChange(Component, new TransactedChangeCallback(EditNodesChangeCallback), null, SR.GetString(SR.TreeViewDesigner_EditNodesTransactionDescription), descriptor); 
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the Edit Nodes dialog.
        /// </devdoc>
        private bool EditNodesChangeCallback(object context) { 
            IServiceProvider site = _treeView.Site;
            TreeNodeCollectionEditorDialog dialog = new TreeNodeCollectionEditorDialog(_treeView, this); 
            DialogResult result = UIServiceHelper.ShowDialog(site, dialog); 
            return (result == DialogResult.OK);
        } 

        private void ExpandToDepth(System.Web.UI.WebControls.TreeNodeCollection nodes, int depth) {
            foreach (System.Web.UI.WebControls.TreeNode node in nodes) {
                if ((node.Expanded != false) && ((depth == -1) || (node.Depth < depth))) { 
                    node.Expanded = true;
                    ExpandToDepth(node.ChildNodes, depth); 
                } 
            }
        } 

        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetDesignTimeHierarchicalDataSource"]/*' />
        protected override IHierarchicalEnumerable GetSampleDataSource() {
            _usingSampleData = true; 
            ((WebTreeView)ViewControl).AutoGenerateDataBindings = true;
 
            return base.GetSampleDataSource(); 
        }
 
        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetDesignTimeHtml"]/*' />
        public override string GetDesignTimeHtml() {
            // Save the HTML temporarily
            string designTimeHtml = base.GetDesignTimeHtml(); 

            if (_emptyDataBinding) { 
                designTimeHtml = GetEmptyDataBindingDesignTimeHtml(); 
            }
 
            return designTimeHtml;
        }

        private string GetEmptyDataBindingDesignTimeHtml() { 
            string name = _treeView.Site.Name;
            return String.Format(CultureInfo.CurrentUICulture, emptyDesignTimeHtml, name, SR.GetString(SR.TreeViewDesigner_EmptyDataBinding)); 
        } 

        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetEmptyDesignTimeHtml"]/*' /> 
        protected override string GetEmptyDesignTimeHtml() {
            string name = _treeView.Site.Name;
            return String.Format(CultureInfo.CurrentUICulture, emptyDesignTimeHtml, name, SR.GetString(SR.TreeViewDesigner_Empty));
        } 

        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) { 
            string name = _treeView.Site.Name;
            return String.Format(CultureInfo.CurrentUICulture, errorDesignTimeHtml, name, SR.GetString(SR.TreeViewDesigner_Error, e.Message)); 
        }

        /// <include file='doc\TreeViewDesigner.uex' path='docs/doc[@for="TreeViewDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(WebTreeView));
            base.Initialize(component); 
            _treeView = (WebTreeView)component; 
        }
 
        internal void InvokeTreeNodeCollectionEditor() {
            EditNodes();
        }
 
        internal void InvokeTreeViewBindingsEditor() {
            EditBindings(); 
        } 

        private class TreeViewDesignerActionList : DesignerActionList { 
            private TreeViewDesigner _parent;

            public TreeViewDesignerActionList(TreeViewDesigner parent) : base (parent.Component) {
                _parent = parent; 
            }
 
            public override bool AutoShow { 
                get {
                    return true; 
                }
                set {
                }
            } 

            public bool ShowLines { 
                get { 
                    return ((WebTreeView)Component).ShowLines;
                } 
                set {
                    PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(WebTreeView))["ShowLines"];
                    propDesc.SetValue(Component, value);
                    TypeDescriptor.Refresh(Component); 
                }
            } 
 
            public void CreateLineImages() {
                _parent.CreateLineImages(); 
            }

            public void EditBindings() {
                _parent.EditBindings(); 
            }
 
            public void EditNodes() { 
                _parent.EditNodes();
            } 

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                string actionGroup = SR.GetString(SR.TreeViewDesigner_DataActionGroup); 

                if (String.IsNullOrEmpty(_parent.DataSourceID)) { 
                    items.Add(new DesignerActionMethodItem(this, 
                        "EditNodes",
                        SR.GetString(SR.TreeViewDesigner_EditNodes), 
                        actionGroup,
                        SR.GetString(SR.TreeViewDesigner_EditNodesDescription),
                        true));
                } 
                else {
                    items.Add(new DesignerActionMethodItem(this, 
                        "EditBindings", 
                        SR.GetString(SR.TreeViewDesigner_EditBindings),
                        actionGroup, 
                        SR.GetString(SR.TreeViewDesigner_EditBindingsDescription),
                        true));
                }
 
                if (ShowLines) {
                    items.Add(new DesignerActionMethodItem(this, 
                        "CreateLineImages", 
                        SR.GetString(SR.TreeViewDesigner_CreateLineImages),
                        actionGroup, 
                        SR.GetString(SR.TreeViewDesigner_CreateLineImagesDescription),
                        true));
                }
 
                items.Add(new DesignerActionPropertyItem("ShowLines",
                    SR.GetString(SR.TreeViewDesigner_ShowLines), 
                    "Actions", 
                    SR.GetString(SR.TreeViewDesigner_ShowLinesDescription)));
 
                return items;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
