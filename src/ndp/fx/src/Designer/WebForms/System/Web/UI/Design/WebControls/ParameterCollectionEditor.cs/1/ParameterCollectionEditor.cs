//------------------------------------------------------------------------------ 
// <copyright file="ParameterCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Drawing.Design;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <devdoc> 
    /// The editor for ParameterCollection objects. 
    /// </devdoc>
    public class ParameterCollectionEditor : UITypeEditor { 

        /// <devdoc>
        /// Launches the editor for ParameterCollection objects.
        /// </devdoc> 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            ParameterCollection parameters = value as ParameterCollection; 
            if (parameters == null) { 
                throw new ArgumentException(SR.GetString(SR.ParameterCollectionEditor_InvalidParameters), "value");
            } 

            System.Web.UI.Control control = context.Instance as System.Web.UI.Control;
            System.Web.UI.Design.ControlDesigner controlDesigner = null;
            if (control != null) { 
                if (control.Site != null) {
                    IDesignerHost designerHost = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost)); 
                    if (designerHost != null) { 
                        controlDesigner = designerHost.GetDesigner(control) as ControlDesigner;
                    } 
                }
            }

            ParameterCollectionEditorForm form = new ParameterCollectionEditorForm(provider, parameters, controlDesigner); 
            DialogResult result = form.ShowDialog();
            if (result == DialogResult.OK) { 
                if (context != null) { 
                    context.OnComponentChanged();
                } 
            }

            return value;
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
// <copyright file="ParameterCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Drawing.Design;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    /// <devdoc> 
    /// The editor for ParameterCollection objects. 
    /// </devdoc>
    public class ParameterCollectionEditor : UITypeEditor { 

        /// <devdoc>
        /// Launches the editor for ParameterCollection objects.
        /// </devdoc> 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            ParameterCollection parameters = value as ParameterCollection; 
            if (parameters == null) { 
                throw new ArgumentException(SR.GetString(SR.ParameterCollectionEditor_InvalidParameters), "value");
            } 

            System.Web.UI.Control control = context.Instance as System.Web.UI.Control;
            System.Web.UI.Design.ControlDesigner controlDesigner = null;
            if (control != null) { 
                if (control.Site != null) {
                    IDesignerHost designerHost = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost)); 
                    if (designerHost != null) { 
                        controlDesigner = designerHost.GetDesigner(control) as ControlDesigner;
                    } 
                }
            }

            ParameterCollectionEditorForm form = new ParameterCollectionEditorForm(provider, parameters, controlDesigner); 
            DialogResult result = form.ShowDialog();
            if (result == DialogResult.OK) { 
                if (context != null) { 
                    context.OnComponentChanged();
                } 
            }

            return value;
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
