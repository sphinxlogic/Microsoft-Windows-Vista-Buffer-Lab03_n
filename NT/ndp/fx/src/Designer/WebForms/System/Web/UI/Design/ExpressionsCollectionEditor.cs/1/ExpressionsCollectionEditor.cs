//------------------------------------------------------------------------------ 
// <copyright file="ExpressionsCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 
 
    using Control = System.Web.UI.Control;
 
    /// <include file='doc\ExpressionsCollectionEditor.uex' path='docs/doc[@for="ExpressionsCollectionEditor"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides editing functions for data binding collections. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ExpressionsCollectionEditor : UITypeEditor {
 
        /// <include file='doc\ExpressionsCollectionEditor.uex' path='docs/doc[@for="ExpressionsCollectionEditor.EditValue"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Edits a data binding within the design time 
        ///       data binding collection.
        ///    </para> 
        /// </devdoc> 
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
            Debug.Assert(context.Instance is Control, "Expected control"); 
            Control c = (Control)context.Instance;

            IServiceProvider  site = c.Site;
            if (site == null) { 
                if (c.Page != null) {
                    site = c.Page.Site; 
                } 
                if (site == null) {
                    site = provider; 
                }
            }
            if (site == null) {
                // 
                return value;
            } 
 
            IDesignerHost designerHost =
                (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Must always have access to IDesignerHost service");

            DesignerTransaction transaction = designerHost.CreateTransaction("(Expressions)");
 
            try {
                IComponentChangeService changeService = 
                    (IComponentChangeService)site.GetService(typeof(IComponentChangeService)); 

                if (changeService != null) { 
                    try {
                        changeService.OnComponentChanging(c, null);
                    }
                    catch (CheckoutException ce) { 
                        if (ce == CheckoutException.Canceled)
                            return value; 
                        throw ce; 
                    }
                } 

                DialogResult result = DialogResult.Cancel;
                try {
                    ExpressionBindingsDialog ebDialog = new ExpressionBindingsDialog(site, c); 
                    IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                    result = edSvc.ShowDialog(ebDialog); 
                } 
                finally {
                    if ((result == DialogResult.OK) && (changeService != null)) { 
                        try {
                            changeService.OnComponentChanged(c, null, null, null);
                        }
                        catch { 
                        }
                    } 
                } 
            }
            finally { 
                transaction.Commit();
            }

            return value; 
        }
 
        /// <include file='doc\ExpressionsCollectionEditor.uex' path='docs/doc[@for="ExpressionsCollectionEditor.GetEditStyle"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets the edit stytle for use by the editor.
        ///    </para>
        /// </devdoc>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ExpressionsCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 
 
    using Control = System.Web.UI.Control;
 
    /// <include file='doc\ExpressionsCollectionEditor.uex' path='docs/doc[@for="ExpressionsCollectionEditor"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides editing functions for data binding collections. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ExpressionsCollectionEditor : UITypeEditor {
 
        /// <include file='doc\ExpressionsCollectionEditor.uex' path='docs/doc[@for="ExpressionsCollectionEditor.EditValue"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Edits a data binding within the design time 
        ///       data binding collection.
        ///    </para> 
        /// </devdoc> 
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
            Debug.Assert(context.Instance is Control, "Expected control"); 
            Control c = (Control)context.Instance;

            IServiceProvider  site = c.Site;
            if (site == null) { 
                if (c.Page != null) {
                    site = c.Page.Site; 
                } 
                if (site == null) {
                    site = provider; 
                }
            }
            if (site == null) {
                // 
                return value;
            } 
 
            IDesignerHost designerHost =
                (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Must always have access to IDesignerHost service");

            DesignerTransaction transaction = designerHost.CreateTransaction("(Expressions)");
 
            try {
                IComponentChangeService changeService = 
                    (IComponentChangeService)site.GetService(typeof(IComponentChangeService)); 

                if (changeService != null) { 
                    try {
                        changeService.OnComponentChanging(c, null);
                    }
                    catch (CheckoutException ce) { 
                        if (ce == CheckoutException.Canceled)
                            return value; 
                        throw ce; 
                    }
                } 

                DialogResult result = DialogResult.Cancel;
                try {
                    ExpressionBindingsDialog ebDialog = new ExpressionBindingsDialog(site, c); 
                    IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                    result = edSvc.ShowDialog(ebDialog); 
                } 
                finally {
                    if ((result == DialogResult.OK) && (changeService != null)) { 
                        try {
                            changeService.OnComponentChanged(c, null, null, null);
                        }
                        catch { 
                        }
                    } 
                } 
            }
            finally { 
                transaction.Commit();
            }

            return value; 
        }
 
        /// <include file='doc\ExpressionsCollectionEditor.uex' path='docs/doc[@for="ExpressionsCollectionEditor.GetEditStyle"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets the edit stytle for use by the editor.
        ///    </para>
        /// </devdoc>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
