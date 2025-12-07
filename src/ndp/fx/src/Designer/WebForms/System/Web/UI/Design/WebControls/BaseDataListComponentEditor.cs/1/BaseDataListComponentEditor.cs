//------------------------------------------------------------------------------ 
// <copyright file="BaseDataListComponentEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Design; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Web.UI.Design.WebControls.ListControls; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design; 

    /// <include file='doc\BaseDataListComponentEditor.uex' path='docs/doc[@for="BaseDataListComponentEditor"]/*' /> 
    /// <devdoc>
    ///    <para>
    ///       Provides the
    ///       base component editor for Web Forms DataGrid and DataList controls. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public abstract class BaseDataListComponentEditor : WindowsFormsComponentEditor {
 
        private int initialPage;

        /// <include file='doc\BaseDataListComponentEditor.uex' path='docs/doc[@for="BaseDataListComponentEditor.BaseDataListComponentEditor"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.WebControls.BaseDataListComponentEditor'/>. 
        ///    </para> 
        /// </devdoc>
        public BaseDataListComponentEditor(int initialPage) { 
            this.initialPage = initialPage;
        }

        /// <include file='doc\BaseDataListComponentEditor.uex' path='docs/doc[@for="BaseDataListComponentEditor.EditComponent"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Edits a component. 
        ///    </para>
        /// </devdoc> 
        public override bool EditComponent(ITypeDescriptorContext context, object obj, IWin32Window parent) {
            bool result = false;
            bool inTemplateMode = false;
 
            Debug.Assert(obj is IComponent, "Expected obj to be an IComponent");
            IComponent comp = (IComponent)obj; 
            ISite compSite = comp.Site; 

            if (compSite != null) { 
                IDesignerHost designerHost = (IDesignerHost)compSite.GetService(typeof(IDesignerHost));

                IDesigner compDesigner = designerHost.GetDesigner(comp);
                Debug.Assert(compDesigner is TemplatedControlDesigner, 
                             "Expected BaseDataList to have a TemplatedControlDesigner");
 
                TemplatedControlDesigner tplDesigner = (TemplatedControlDesigner)compDesigner; 
                inTemplateMode = tplDesigner.InTemplateModeInternal;
            } 

            if (inTemplateMode == false) {
                Type[] pageControlTypes = GetComponentEditorPages();
 
                if ((pageControlTypes != null) && (pageControlTypes.Length != 0)) {
                    ComponentEditorForm form = new ComponentEditorForm(obj, 
                                                                       pageControlTypes); 

                    // Set RightToLeft mode based on resource file 
                    string rtlText = SR.GetString(SR.RTL);
                    if (!String.Equals(rtlText, "RTL_False", StringComparison.Ordinal)) {
                        form.RightToLeft = RightToLeft.Yes;
                        form.RightToLeftLayout = true; 
                    }
 
 
                    if (form.ShowForm(parent, GetInitialComponentEditorPageIndex()) == DialogResult.OK)
                        result = true; 
                }
            }
            else {
                RTLAwareMessageBox.Show(null, SR.GetString(SR.BDL_TemplateModePropBuilder), 
                                SR.GetString(SR.BDL_PropertyBuilder),
                                MessageBoxButtons.OK, MessageBoxIcon.Information, 
                                MessageBoxDefaultButton.Button1, 0); 
            }
            return result; 
        }

        /// <include file='doc\BaseDataListComponentEditor.uex' path='docs/doc[@for="BaseDataListComponentEditor.GetInitialComponentEditorPageIndex"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the index of the initial component editor page. 
        ///    </para> 
        /// </devdoc>
        protected override int GetInitialComponentEditorPageIndex() { 
            return initialPage;
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BaseDataListComponentEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Design; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Web.UI.Design.WebControls.ListControls; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design; 

    /// <include file='doc\BaseDataListComponentEditor.uex' path='docs/doc[@for="BaseDataListComponentEditor"]/*' /> 
    /// <devdoc>
    ///    <para>
    ///       Provides the
    ///       base component editor for Web Forms DataGrid and DataList controls. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public abstract class BaseDataListComponentEditor : WindowsFormsComponentEditor {
 
        private int initialPage;

        /// <include file='doc\BaseDataListComponentEditor.uex' path='docs/doc[@for="BaseDataListComponentEditor.BaseDataListComponentEditor"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Initializes a new instance of <see cref='System.Web.UI.Design.WebControls.BaseDataListComponentEditor'/>. 
        ///    </para> 
        /// </devdoc>
        public BaseDataListComponentEditor(int initialPage) { 
            this.initialPage = initialPage;
        }

        /// <include file='doc\BaseDataListComponentEditor.uex' path='docs/doc[@for="BaseDataListComponentEditor.EditComponent"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Edits a component. 
        ///    </para>
        /// </devdoc> 
        public override bool EditComponent(ITypeDescriptorContext context, object obj, IWin32Window parent) {
            bool result = false;
            bool inTemplateMode = false;
 
            Debug.Assert(obj is IComponent, "Expected obj to be an IComponent");
            IComponent comp = (IComponent)obj; 
            ISite compSite = comp.Site; 

            if (compSite != null) { 
                IDesignerHost designerHost = (IDesignerHost)compSite.GetService(typeof(IDesignerHost));

                IDesigner compDesigner = designerHost.GetDesigner(comp);
                Debug.Assert(compDesigner is TemplatedControlDesigner, 
                             "Expected BaseDataList to have a TemplatedControlDesigner");
 
                TemplatedControlDesigner tplDesigner = (TemplatedControlDesigner)compDesigner; 
                inTemplateMode = tplDesigner.InTemplateModeInternal;
            } 

            if (inTemplateMode == false) {
                Type[] pageControlTypes = GetComponentEditorPages();
 
                if ((pageControlTypes != null) && (pageControlTypes.Length != 0)) {
                    ComponentEditorForm form = new ComponentEditorForm(obj, 
                                                                       pageControlTypes); 

                    // Set RightToLeft mode based on resource file 
                    string rtlText = SR.GetString(SR.RTL);
                    if (!String.Equals(rtlText, "RTL_False", StringComparison.Ordinal)) {
                        form.RightToLeft = RightToLeft.Yes;
                        form.RightToLeftLayout = true; 
                    }
 
 
                    if (form.ShowForm(parent, GetInitialComponentEditorPageIndex()) == DialogResult.OK)
                        result = true; 
                }
            }
            else {
                RTLAwareMessageBox.Show(null, SR.GetString(SR.BDL_TemplateModePropBuilder), 
                                SR.GetString(SR.BDL_PropertyBuilder),
                                MessageBoxButtons.OK, MessageBoxIcon.Information, 
                                MessageBoxDefaultButton.Button1, 0); 
            }
            return result; 
        }

        /// <include file='doc\BaseDataListComponentEditor.uex' path='docs/doc[@for="BaseDataListComponentEditor.GetInitialComponentEditorPageIndex"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the index of the initial component editor page. 
        ///    </para> 
        /// </devdoc>
        protected override int GetInitialComponentEditorPageIndex() { 
            return initialPage;
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
