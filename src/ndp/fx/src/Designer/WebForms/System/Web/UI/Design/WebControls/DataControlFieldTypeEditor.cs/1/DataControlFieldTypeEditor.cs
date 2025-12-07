//------------------------------------------------------------------------------ 
// <copyright file="DataControlFieldTypeEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics;
    using System.Drawing.Design;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
 
    /// <devdoc>
    /// The editor used for property grid field collection edits. 
    /// </devdoc>
    public class DataControlFieldTypeEditor : UITypeEditor {

 
        /// <devdoc>
        /// Launches the editor for DataControlFields. 
        /// </devdoc> 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            DataBoundControl dataBoundControl = context.Instance as DataBoundControl; 

            Debug.Assert(dataBoundControl != null, "Only DataBoundControls should be used with DataControlFieldTypeEditor");
            if (dataBoundControl != null) {
 
                IDesignerHost designerHost = (IDesignerHost)provider.GetService(typeof(IDesignerHost));
                Debug.Assert(designerHost != null, "Did not get DesignerHost service."); 
                DataBoundControlDesigner designer = (DataBoundControlDesigner)designerHost.GetDesigner(dataBoundControl); 

                IComponentChangeService changeService = (IComponentChangeService)provider.GetService(typeof(IComponentChangeService)); 

                DataControlFieldsEditor form = new DataControlFieldsEditor(designer);

                DialogResult result = UIServiceHelper.ShowDialog(provider, form); 
                if (result == DialogResult.OK) {
                    if (changeService != null) { 
                        changeService.OnComponentChanged(dataBoundControl, null, null, null); 
                    }
                } 

                return value;
            }
            return null; 
        }
 
        /// <devdoc> 
        /// Gets the editing style of the Edit method.
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataControlFieldTypeEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics;
    using System.Drawing.Design;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
 
    /// <devdoc>
    /// The editor used for property grid field collection edits. 
    /// </devdoc>
    public class DataControlFieldTypeEditor : UITypeEditor {

 
        /// <devdoc>
        /// Launches the editor for DataControlFields. 
        /// </devdoc> 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            DataBoundControl dataBoundControl = context.Instance as DataBoundControl; 

            Debug.Assert(dataBoundControl != null, "Only DataBoundControls should be used with DataControlFieldTypeEditor");
            if (dataBoundControl != null) {
 
                IDesignerHost designerHost = (IDesignerHost)provider.GetService(typeof(IDesignerHost));
                Debug.Assert(designerHost != null, "Did not get DesignerHost service."); 
                DataBoundControlDesigner designer = (DataBoundControlDesigner)designerHost.GetDesigner(dataBoundControl); 

                IComponentChangeService changeService = (IComponentChangeService)provider.GetService(typeof(IComponentChangeService)); 

                DataControlFieldsEditor form = new DataControlFieldsEditor(designer);

                DialogResult result = UIServiceHelper.ShowDialog(provider, form); 
                if (result == DialogResult.OK) {
                    if (changeService != null) { 
                        changeService.OnComponentChanged(dataBoundControl, null, null, null); 
                    }
                } 

                return value;
            }
            return null; 
        }
 
        /// <devdoc> 
        /// Gets the editing style of the Edit method.
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
