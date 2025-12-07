//------------------------------------------------------------------------------ 
// <copyright file="ObjectSelectorEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System.Design; 
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System; 
    using System.Windows.Forms;
    using System.Drawing; 
    using System.Windows.Forms.PropertyGridInternal; 
    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel; 

    using Microsoft.Win32;
    using System.Drawing.Design;
 

    /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor"]/*' /> 
    /// <internalonly/> 
    /// <devdoc>
    /// </devdoc> 
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")]
    public abstract class ObjectSelectorEditor : UITypeEditor {
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.subObjectSelector"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public bool SubObjectSelector = false; 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.prevValue"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected object prevValue = null;
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.currValue"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        protected object currValue = null;
        private Selector selector = null; 

        //
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.ObjectSelectorEditor"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public ObjectSelectorEditor() { 
        }
 
        //
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.ObjectSelectorEditor1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public ObjectSelectorEditor(bool subObjectSelector) { 
            this.SubObjectSelector = subObjectSelector; 
        }
 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.EditValue"]/*' />
        /// <devdoc>
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            if (null != provider) { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
                if (edSvc != null) {
                    if (null == selector) { 
                        selector = new Selector(this);
                    }

                    prevValue = value; 
                    currValue = value;
                    FillTreeWithData(selector, context, provider); 
                    selector.Start(edSvc, value); 
                    edSvc.DropDownControl(selector);
                    selector.Stop(); 
                    if (prevValue != currValue) {
                        value = currValue;
                    }
                } 
            }
 
            return value; 
        }
 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.GetEditStyle"]/*' />
        /// <devdoc>
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.DropDown; 
        } 

        // 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.EqualsToValue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public bool EqualsToValue(object value) {
            if (value == currValue) 
                return true; 
            else
                return false; 
        }

        //
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.FillTreeWithData"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        protected virtual void FillTreeWithData(Selector selector, ITypeDescriptorContext context, IServiceProvider provider) {
            selector.Clear(); 
        }

        //
        // override this method to add validation code for new value 
        //
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.SetValue"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public virtual void SetValue(object value) {
            this.currValue = value;
        }
 
        //
        // 
        // 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc>
        public class Selector : System.Windows.Forms.TreeView {
 
            //
            private ObjectSelectorEditor editor = null; 
            private IWindowsFormsEditorService edSvc = null; 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.clickSeen"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool clickSeen = false;
 
            //
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.Selector"]/*' /> 
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public Selector(ObjectSelectorEditor editor) {
                CreateHandle();
                this.editor = editor;
 
                this.BorderStyle = BorderStyle.None;
                this.FullRowSelect = !editor.SubObjectSelector; 
                this.Scrollable = true; 
                this.CheckBoxes = false;
                this.ShowPlusMinus = editor.SubObjectSelector; 
                this.ShowLines = editor.SubObjectSelector;
                this.ShowRootLines = editor.SubObjectSelector;

                AfterSelect += new TreeViewEventHandler(this.OnAfterSelect); 
            }
 
            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.AddNode"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public SelectorNode AddNode(string label, object value, SelectorNode parent) {
                SelectorNode newNode = new SelectorNode(label, value); 

                if (parent != null) { 
                    parent.Nodes.Add(newNode); 
                }
                else { 
                    Nodes.Add(newNode);
                }
                return newNode;
            } 

            private bool ChooseSelectedNodeIfEqual() { 
                if (editor != null && edSvc != null) { 
                      editor.SetValue(((SelectorNode)SelectedNode).value);
                      if (editor.EqualsToValue(((SelectorNode)SelectedNode).value)) { 
                          edSvc.CloseDropDown();
                          return true;
                      }
                } 
                return false;
            } 
 

            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.Clear"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public void Clear() {
                clickSeen = false; 
                Nodes.Clear(); 
            }
 
            //
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.OnAfterSelect"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            protected void OnAfterSelect(object sender, TreeViewEventArgs e) { 
                if (clickSeen) { 
                    ChooseSelectedNodeIfEqual();
                    clickSeen = false; 
                }
            }

            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.OnKeyDown"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            protected override void OnKeyDown(KeyEventArgs e) { 
                Keys key = e.KeyCode;
                switch (key) {
                    case Keys.Return:
                        if (ChooseSelectedNodeIfEqual()) { 
                            e.Handled = true;
                        } 
                        break; 

                    case Keys.Escape: 
                        editor.SetValue(editor.prevValue);
                        e.Handled = true;
                        edSvc.CloseDropDown();
                        break; 
                }
                base.OnKeyDown(e); 
            } 

            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.OnKeyPress"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            protected override void OnKeyPress(KeyPressEventArgs e) { 
                switch (e.KeyChar) {
                    case '\r':  // Enter key 
                        e.Handled = true; 
                        break;
                } 
                base.OnKeyPress(e);
            }

            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.OnNodeMouseClick"]/*' /> 
            /// <internalonly/>
            /// <devdoc> 
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            protected override void OnNodeMouseClick(TreeNodeMouseClickEventArgs e) { 
                // we won't get an OnAfterSelect if it's already selected, so use this instead
                if (e.Node == SelectedNode) {
                    ChooseSelectedNodeIfEqual();
                } 
                base.OnNodeMouseClick(e);
            } 
 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.SetSelection"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool SetSelection(object value, System.Windows.Forms.TreeNodeCollection nodes) {
                TreeNode[] treeNodes; 

                if (nodes == null) { 
                    treeNodes = new TreeNode[this.Nodes.Count]; 
                    this.Nodes.CopyTo(treeNodes, 0);
                } 
                else {
                    treeNodes = new TreeNode[nodes.Count];
                    nodes.CopyTo(treeNodes, 0);
                } 

                int len = treeNodes.Length; 
                if (len == 0) return false; 

                for (int i=0; i<len; i++) { 
                    if (((SelectorNode)treeNodes[i]).value == value) {
                        SelectedNode = treeNodes[i];
                        return true;
                    } 
                    if ((treeNodes[i].Nodes != null) && (treeNodes[i].Nodes.Count != 0)) {
                        treeNodes[i].Expand(); 
                        if (SetSelection(value, treeNodes[i].Nodes)) { 
                            return true;
                        } 
                        treeNodes[i].Collapse();
                    }
                }
                return false; 
            }
 
            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.Start"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public void Start(IWindowsFormsEditorService edSvc, object value) {
                this.edSvc = edSvc; 
                this.clickSeen = false;
                SetSelection(value, Nodes); 
            } 

            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.Stop"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public void Stop() {
                this.edSvc = null; 
            } 

            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.WndProc"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
            protected override void WndProc(ref Message m) { 
                switch (m.Msg) { 
                    case NativeMethods.WM_GETDLGCODE:
                        m.Result = (IntPtr)((long)m.Result | NativeMethods.DLGC_WANTALLKEYS); 
                        return;
                    case NativeMethods.WM_MOUSEMOVE:
                        if (clickSeen) {
                            clickSeen = false; 
                        }
                        break; 
                    case NativeMethods.WM_REFLECT + NativeMethods.WM_NOTIFY: 
                        NativeMethods.NMTREEVIEW nmtv = (NativeMethods.NMTREEVIEW)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.NMTREEVIEW));
                        if (nmtv.nmhdr.code == NativeMethods.NM_CLICK) { 
                            clickSeen = true;
                        }
                        break;
                } 
                base.WndProc(ref m);
            } 
        } 

        // 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.SelectorNode"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc> 
        /// Suppressed because although the type implements ISerializable --its on the base class and this class
        /// is not modifying the stream to include its local information.  Therefore, we should not publicly advertise this as 
        /// Serializable unless explicitly required. 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
        public class SelectorNode : System.Windows.Forms.TreeNode { 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.SelectorNode.value"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public object value = null;
 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.SelectorNode.SelectorNode"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public SelectorNode(string label, object value) : base (label) {
                this.value = value;
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ObjectSelectorEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System.Design; 
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System; 
    using System.Windows.Forms;
    using System.Drawing; 
    using System.Windows.Forms.PropertyGridInternal; 
    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel; 

    using Microsoft.Win32;
    using System.Drawing.Design;
 

    /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor"]/*' /> 
    /// <internalonly/> 
    /// <devdoc>
    /// </devdoc> 
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")]
    public abstract class ObjectSelectorEditor : UITypeEditor {
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.subObjectSelector"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public bool SubObjectSelector = false; 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.prevValue"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected object prevValue = null;
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.currValue"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        protected object currValue = null;
        private Selector selector = null; 

        //
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.ObjectSelectorEditor"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public ObjectSelectorEditor() { 
        }
 
        //
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.ObjectSelectorEditor1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public ObjectSelectorEditor(bool subObjectSelector) { 
            this.SubObjectSelector = subObjectSelector; 
        }
 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.EditValue"]/*' />
        /// <devdoc>
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            if (null != provider) { 
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
                if (edSvc != null) {
                    if (null == selector) { 
                        selector = new Selector(this);
                    }

                    prevValue = value; 
                    currValue = value;
                    FillTreeWithData(selector, context, provider); 
                    selector.Start(edSvc, value); 
                    edSvc.DropDownControl(selector);
                    selector.Stop(); 
                    if (prevValue != currValue) {
                        value = currValue;
                    }
                } 
            }
 
            return value; 
        }
 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.GetEditStyle"]/*' />
        /// <devdoc>
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.DropDown; 
        } 

        // 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.EqualsToValue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public bool EqualsToValue(object value) {
            if (value == currValue) 
                return true; 
            else
                return false; 
        }

        //
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.FillTreeWithData"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        protected virtual void FillTreeWithData(Selector selector, ITypeDescriptorContext context, IServiceProvider provider) {
            selector.Clear(); 
        }

        //
        // override this method to add validation code for new value 
        //
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.SetValue"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public virtual void SetValue(object value) {
            this.currValue = value;
        }
 
        //
        // 
        // 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc>
        public class Selector : System.Windows.Forms.TreeView {
 
            //
            private ObjectSelectorEditor editor = null; 
            private IWindowsFormsEditorService edSvc = null; 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.clickSeen"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool clickSeen = false;
 
            //
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.Selector"]/*' /> 
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public Selector(ObjectSelectorEditor editor) {
                CreateHandle();
                this.editor = editor;
 
                this.BorderStyle = BorderStyle.None;
                this.FullRowSelect = !editor.SubObjectSelector; 
                this.Scrollable = true; 
                this.CheckBoxes = false;
                this.ShowPlusMinus = editor.SubObjectSelector; 
                this.ShowLines = editor.SubObjectSelector;
                this.ShowRootLines = editor.SubObjectSelector;

                AfterSelect += new TreeViewEventHandler(this.OnAfterSelect); 
            }
 
            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.AddNode"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public SelectorNode AddNode(string label, object value, SelectorNode parent) {
                SelectorNode newNode = new SelectorNode(label, value); 

                if (parent != null) { 
                    parent.Nodes.Add(newNode); 
                }
                else { 
                    Nodes.Add(newNode);
                }
                return newNode;
            } 

            private bool ChooseSelectedNodeIfEqual() { 
                if (editor != null && edSvc != null) { 
                      editor.SetValue(((SelectorNode)SelectedNode).value);
                      if (editor.EqualsToValue(((SelectorNode)SelectedNode).value)) { 
                          edSvc.CloseDropDown();
                          return true;
                      }
                } 
                return false;
            } 
 

            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.Clear"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public void Clear() {
                clickSeen = false; 
                Nodes.Clear(); 
            }
 
            //
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.OnAfterSelect"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            protected void OnAfterSelect(object sender, TreeViewEventArgs e) { 
                if (clickSeen) { 
                    ChooseSelectedNodeIfEqual();
                    clickSeen = false; 
                }
            }

            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.OnKeyDown"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            protected override void OnKeyDown(KeyEventArgs e) { 
                Keys key = e.KeyCode;
                switch (key) {
                    case Keys.Return:
                        if (ChooseSelectedNodeIfEqual()) { 
                            e.Handled = true;
                        } 
                        break; 

                    case Keys.Escape: 
                        editor.SetValue(editor.prevValue);
                        e.Handled = true;
                        edSvc.CloseDropDown();
                        break; 
                }
                base.OnKeyDown(e); 
            } 

            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.OnKeyPress"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            protected override void OnKeyPress(KeyPressEventArgs e) { 
                switch (e.KeyChar) {
                    case '\r':  // Enter key 
                        e.Handled = true; 
                        break;
                } 
                base.OnKeyPress(e);
            }

            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.OnNodeMouseClick"]/*' /> 
            /// <internalonly/>
            /// <devdoc> 
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            protected override void OnNodeMouseClick(TreeNodeMouseClickEventArgs e) { 
                // we won't get an OnAfterSelect if it's already selected, so use this instead
                if (e.Node == SelectedNode) {
                    ChooseSelectedNodeIfEqual();
                } 
                base.OnNodeMouseClick(e);
            } 
 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.SetSelection"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool SetSelection(object value, System.Windows.Forms.TreeNodeCollection nodes) {
                TreeNode[] treeNodes; 

                if (nodes == null) { 
                    treeNodes = new TreeNode[this.Nodes.Count]; 
                    this.Nodes.CopyTo(treeNodes, 0);
                } 
                else {
                    treeNodes = new TreeNode[nodes.Count];
                    nodes.CopyTo(treeNodes, 0);
                } 

                int len = treeNodes.Length; 
                if (len == 0) return false; 

                for (int i=0; i<len; i++) { 
                    if (((SelectorNode)treeNodes[i]).value == value) {
                        SelectedNode = treeNodes[i];
                        return true;
                    } 
                    if ((treeNodes[i].Nodes != null) && (treeNodes[i].Nodes.Count != 0)) {
                        treeNodes[i].Expand(); 
                        if (SetSelection(value, treeNodes[i].Nodes)) { 
                            return true;
                        } 
                        treeNodes[i].Collapse();
                    }
                }
                return false; 
            }
 
            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.Start"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public void Start(IWindowsFormsEditorService edSvc, object value) {
                this.edSvc = edSvc; 
                this.clickSeen = false;
                SetSelection(value, Nodes); 
            } 

            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.Stop"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public void Stop() {
                this.edSvc = null; 
            } 

            // 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.Selector.WndProc"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
            protected override void WndProc(ref Message m) { 
                switch (m.Msg) { 
                    case NativeMethods.WM_GETDLGCODE:
                        m.Result = (IntPtr)((long)m.Result | NativeMethods.DLGC_WANTALLKEYS); 
                        return;
                    case NativeMethods.WM_MOUSEMOVE:
                        if (clickSeen) {
                            clickSeen = false; 
                        }
                        break; 
                    case NativeMethods.WM_REFLECT + NativeMethods.WM_NOTIFY: 
                        NativeMethods.NMTREEVIEW nmtv = (NativeMethods.NMTREEVIEW)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.NMTREEVIEW));
                        if (nmtv.nmhdr.code == NativeMethods.NM_CLICK) { 
                            clickSeen = true;
                        }
                        break;
                } 
                base.WndProc(ref m);
            } 
        } 

        // 
        /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.SelectorNode"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc> 
        /// Suppressed because although the type implements ISerializable --its on the base class and this class
        /// is not modifying the stream to include its local information.  Therefore, we should not publicly advertise this as 
        /// Serializable unless explicitly required. 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
        public class SelectorNode : System.Windows.Forms.TreeNode { 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.SelectorNode.value"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public object value = null;
 
            /// <include file='doc\ObjectSelectorEditor.uex' path='docs/doc[@for="ObjectSelectorEditor.SelectorNode.SelectorNode"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public SelectorNode(string label, object value) : base (label) {
                this.value = value;
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
