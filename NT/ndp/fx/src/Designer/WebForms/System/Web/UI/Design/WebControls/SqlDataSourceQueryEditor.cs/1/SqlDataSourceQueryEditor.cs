//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceQueryEditor.cs" company="Microsoft">
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
    /// The editor for SqlDataSource queries.
    /// </devdoc>
    internal sealed class SqlDataSourceQueryEditor : UITypeEditor {
 
        /// <devdoc>
        /// Transacted change callback to invoke the Edit Query dialog. 
        /// </devdoc> 
        private bool EditQueryChangeCallback(object context) {
            SqlDataSource sqlDataSource = (SqlDataSource)((Pair)context).First; 
            DataSourceOperation operation = (DataSourceOperation)((Pair)context).Second;
            IServiceProvider serviceProvider = sqlDataSource.Site;

            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Did not get DesignerHost service.");
            SqlDataSourceDesigner designer = (SqlDataSourceDesigner)designerHost.GetDesigner(sqlDataSource); 
 
            ParameterCollection parameterCollection = null;
            string command = String.Empty; 
            SqlDataSourceCommandType commandType = SqlDataSourceCommandType.Text;
            switch (operation) {
                case DataSourceOperation.Delete:
                    parameterCollection = sqlDataSource.DeleteParameters; 
                    command = sqlDataSource.DeleteCommand;
                    commandType = sqlDataSource.DeleteCommandType; 
                    break; 
                case DataSourceOperation.Insert:
                    parameterCollection = sqlDataSource.InsertParameters; 
                    command = sqlDataSource.InsertCommand;
                    commandType = sqlDataSource.InsertCommandType;
                    break;
                case DataSourceOperation.Select: 
                    parameterCollection = sqlDataSource.SelectParameters;
                    command = sqlDataSource.SelectCommand; 
                    commandType = sqlDataSource.SelectCommandType; 
                    break;
                case DataSourceOperation.Update: 
                    parameterCollection = sqlDataSource.UpdateParameters;
                    command = sqlDataSource.UpdateCommand;
                    commandType = sqlDataSource.UpdateCommandType;
                    break; 
            }
 
            SqlDataSourceQueryEditorForm form = new SqlDataSourceQueryEditorForm(serviceProvider, designer, 
                sqlDataSource.ProviderName,
                designer.ConnectionString, 
                operation,
                commandType, command,
                parameterCollection);
 
            DialogResult result = UIServiceHelper.ShowDialog(serviceProvider, form);
            if (result == DialogResult.OK) { 
                // We use the property descriptors to reset the values to 
                // make sure we clear out any databindings or expressions that
                // may be set. 
                PropertyDescriptor propDesc = null;

                switch (operation) {
                    case DataSourceOperation.Delete: 
                        propDesc = TypeDescriptor.GetProperties(sqlDataSource)["DeleteCommand"];
                        break; 
                    case DataSourceOperation.Insert: 
                        propDesc = TypeDescriptor.GetProperties(sqlDataSource)["InsertCommand"];
                        break; 
                    case DataSourceOperation.Select:
                        propDesc = TypeDescriptor.GetProperties(sqlDataSource)["SelectCommand"];
                        break;
                    case DataSourceOperation.Update: 
                        propDesc = TypeDescriptor.GetProperties(sqlDataSource)["UpdateCommand"];
                        break; 
                } 

                if (propDesc != null) { 
                    propDesc.ResetValue(sqlDataSource);
                    propDesc.SetValue(sqlDataSource, form.Command);
                }
                else { 
                    Debug.Fail("Unexpected DataSourceOperation: " + operation);
                } 
 
                return true;
            } 
            else {
                return false;
            }
        } 

        /// <devdoc> 
        /// Launches the editor for SqlDataSource queries. 
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            ControlDesigner.InvokeTransactedChange(
                (IComponent)context.Instance,
                new TransactedChangeCallback(EditQueryChangeCallback),
                new Pair(context.Instance, value), 
                SR.GetString(SR.SqlDataSourceDesigner_EditQueryTransactionDescription));
 
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
// <copyright file="SqlDataSourceQueryEditor.cs" company="Microsoft">
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
    /// The editor for SqlDataSource queries.
    /// </devdoc>
    internal sealed class SqlDataSourceQueryEditor : UITypeEditor {
 
        /// <devdoc>
        /// Transacted change callback to invoke the Edit Query dialog. 
        /// </devdoc> 
        private bool EditQueryChangeCallback(object context) {
            SqlDataSource sqlDataSource = (SqlDataSource)((Pair)context).First; 
            DataSourceOperation operation = (DataSourceOperation)((Pair)context).Second;
            IServiceProvider serviceProvider = sqlDataSource.Site;

            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Did not get DesignerHost service.");
            SqlDataSourceDesigner designer = (SqlDataSourceDesigner)designerHost.GetDesigner(sqlDataSource); 
 
            ParameterCollection parameterCollection = null;
            string command = String.Empty; 
            SqlDataSourceCommandType commandType = SqlDataSourceCommandType.Text;
            switch (operation) {
                case DataSourceOperation.Delete:
                    parameterCollection = sqlDataSource.DeleteParameters; 
                    command = sqlDataSource.DeleteCommand;
                    commandType = sqlDataSource.DeleteCommandType; 
                    break; 
                case DataSourceOperation.Insert:
                    parameterCollection = sqlDataSource.InsertParameters; 
                    command = sqlDataSource.InsertCommand;
                    commandType = sqlDataSource.InsertCommandType;
                    break;
                case DataSourceOperation.Select: 
                    parameterCollection = sqlDataSource.SelectParameters;
                    command = sqlDataSource.SelectCommand; 
                    commandType = sqlDataSource.SelectCommandType; 
                    break;
                case DataSourceOperation.Update: 
                    parameterCollection = sqlDataSource.UpdateParameters;
                    command = sqlDataSource.UpdateCommand;
                    commandType = sqlDataSource.UpdateCommandType;
                    break; 
            }
 
            SqlDataSourceQueryEditorForm form = new SqlDataSourceQueryEditorForm(serviceProvider, designer, 
                sqlDataSource.ProviderName,
                designer.ConnectionString, 
                operation,
                commandType, command,
                parameterCollection);
 
            DialogResult result = UIServiceHelper.ShowDialog(serviceProvider, form);
            if (result == DialogResult.OK) { 
                // We use the property descriptors to reset the values to 
                // make sure we clear out any databindings or expressions that
                // may be set. 
                PropertyDescriptor propDesc = null;

                switch (operation) {
                    case DataSourceOperation.Delete: 
                        propDesc = TypeDescriptor.GetProperties(sqlDataSource)["DeleteCommand"];
                        break; 
                    case DataSourceOperation.Insert: 
                        propDesc = TypeDescriptor.GetProperties(sqlDataSource)["InsertCommand"];
                        break; 
                    case DataSourceOperation.Select:
                        propDesc = TypeDescriptor.GetProperties(sqlDataSource)["SelectCommand"];
                        break;
                    case DataSourceOperation.Update: 
                        propDesc = TypeDescriptor.GetProperties(sqlDataSource)["UpdateCommand"];
                        break; 
                } 

                if (propDesc != null) { 
                    propDesc.ResetValue(sqlDataSource);
                    propDesc.SetValue(sqlDataSource, form.Command);
                }
                else { 
                    Debug.Fail("Unexpected DataSourceOperation: " + operation);
                } 
 
                return true;
            } 
            else {
                return false;
            }
        } 

        /// <devdoc> 
        /// Launches the editor for SqlDataSource queries. 
        /// </devdoc>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            ControlDesigner.InvokeTransactedChange(
                (IComponent)context.Instance,
                new TransactedChangeCallback(EditQueryChangeCallback),
                new Pair(context.Instance, value), 
                SR.GetString(SR.SqlDataSourceDesigner_EditQueryTransactionDescription));
 
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
