//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConnectionPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Data; 
    using System.Data.Common;
    using System.ComponentModel.Design.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
 
    /// <devdoc>
    /// An interface to set and retrieve connection strings and provider names 
    /// to assist in configuring data sources.
    /// </devdoc>
    internal abstract class SqlDataSourceConnectionPanel : WizardPanel {
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
 
        protected SqlDataSourceConnectionPanel(SqlDataSourceDesigner sqlDataSourceDesigner) { 
            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
        }


        /// <devdoc> 
        /// The data connection selected by the user.
        /// </devdoc> 
        public abstract DesignerDataConnection DataConnection { 
            get;
        } 


        /// <devdoc>
        /// Checks that the selected connection uses a provider that is 
        /// supported in machine.config. An error message will be shown if the
        /// provider cannot be found, and false will be returned. 
        /// </devdoc> 
        protected bool CheckValidProvider() {
            // Detect if the provider is supported in machine.config 
            DesignerDataConnection dataConnection = DataConnection;
            try {
                SqlDataSourceDesigner.GetDbProviderFactory(dataConnection.ProviderName);
                return true; 
            }
            catch (Exception ex) { 
                UIServiceHelper.ShowError(ServiceProvider, ex, SR.GetString(SR.SqlDataSourceConnectionPanel_ProviderNotFound, dataConnection.ProviderName)); 
                return false;
            } 
        }

        /// <devdoc>
        /// Determines which command panel to show based on the state of the 
        /// wizard, and where the user has been in the wizard previously.
        /// Null will be returned if the user cannot proceed. 
        /// </devdoc> 
        internal static WizardPanel CreateCommandPanel(SqlDataSourceWizardForm wizard, DesignerDataConnection dataConnection, WizardPanel nextPanel) {
            IDataEnvironment dataEnvironment = null; 
            IServiceProvider serviceProvider = wizard.SqlDataSourceDesigner.Component.Site;
            if (serviceProvider != null) {
                dataEnvironment = (IDataEnvironment)serviceProvider.GetService(typeof(IDataEnvironment));
            } 

            bool requestingSchemaMode = false; 
 
            // Check if the table/field picker should be available
            // Must have table or view schema information to enable this 
            if (dataEnvironment != null) {
                // Check if there is schema available for the connection
                try {
                    IDesignerDataSchema schema = dataEnvironment.GetConnectionSchema(dataConnection); 
                    if (schema != null) {
                        requestingSchemaMode = schema.SupportsSchemaClass(DesignerDataSchemaClass.Tables); 
                        if (requestingSchemaMode) { 
                            // Force the table list to be refreshed to check for errors
                            schema.GetSchemaItems(DesignerDataSchemaClass.Tables); 
                        }
                        else {
                            // Tables are not supported, so try views
                            requestingSchemaMode = schema.SupportsSchemaClass(DesignerDataSchemaClass.Views); 

                            // Force the view list to be refreshed to check for errors 
                            schema.GetSchemaItems(DesignerDataSchemaClass.Views); 
                        }
                        // 
                    }
                }
                catch (Exception ex) {
                    UIServiceHelper.ShowError( 
                        serviceProvider,
                        ex, 
                        SR.GetString(SR.SqlDataSourceConnectionPanel_CouldNotGetConnectionSchema)); 
                    return null;
                } 
            }

            if (nextPanel == null) {
                // First time user hit "Next", create and populate appropriate panel 

                if (requestingSchemaMode) { 
                    // Schema is available, go to table/field picker 
                    SqlDataSourceConfigureSelectPanel commandPanel = wizard.GetConfigureSelectPanel();
                    return commandPanel; 
                }
                else {
                    // No schema available, go straight to custom mode
                    return CreateCustomCommandPanel(wizard, dataConnection); 
                }
            } 
            else { 
                // User hit back, and is now hitting Next again - try to retain state of next panel
 
                if (requestingSchemaMode) {
                    // If schema is now available, but we are not in schema mode, switch to it
                    if (!(nextPanel is SqlDataSourceConfigureSelectPanel)) {
                        SqlDataSourceConfigureSelectPanel commandPanel = wizard.GetConfigureSelectPanel(); 
                        return commandPanel;
                    } 
                } 
                else {
                    // In case of custom mode, don't touch anything unless we were previously in schema mode 
                    if (!(nextPanel is SqlDataSourceCustomCommandPanel)) {
                        return CreateCustomCommandPanel(wizard, dataConnection);
                    }
                } 
            }
 
            // Nothing has changed, just return the existing NextPanel without any changes. 
            return nextPanel;
        } 

        /// <devdoc>
        /// Creates the custom command panel.
        /// </devdoc> 
        private static WizardPanel CreateCustomCommandPanel(SqlDataSourceWizardForm wizard, DesignerDataConnection dataConnection) {
            // Clone the lists of parameters so we don't touch the originals 
            SqlDataSource sqlDataSource = (SqlDataSource)wizard.SqlDataSourceDesigner.Component; 
            ArrayList selectParameters = new ArrayList();
            ArrayList insertParameters = new ArrayList(); 
            ArrayList updateParameters = new ArrayList();
            ArrayList deleteParameters = new ArrayList();
            wizard.SqlDataSourceDesigner.CopyList(sqlDataSource.SelectParameters, selectParameters);
            wizard.SqlDataSourceDesigner.CopyList(sqlDataSource.InsertParameters, insertParameters); 
            wizard.SqlDataSourceDesigner.CopyList(sqlDataSource.UpdateParameters, updateParameters);
            wizard.SqlDataSourceDesigner.CopyList(sqlDataSource.DeleteParameters, deleteParameters); 
 
            SqlDataSourceCustomCommandPanel commandPanel = wizard.GetCustomCommandPanel();
 
            commandPanel.SetQueries(
                dataConnection,
                new SqlDataSourceQuery(sqlDataSource.SelectCommand, sqlDataSource.SelectCommandType, selectParameters),
                new SqlDataSourceQuery(sqlDataSource.InsertCommand, sqlDataSource.InsertCommandType, insertParameters), 
                new SqlDataSourceQuery(sqlDataSource.UpdateCommand, sqlDataSource.UpdateCommandType, updateParameters),
                new SqlDataSourceQuery(sqlDataSource.DeleteCommand, sqlDataSource.DeleteCommandType, deleteParameters)); 
 
            return commandPanel;
        } 

        /// <devdoc>
        /// Called when the user clicks on the Next button.
        /// </devdoc> 
        public override bool OnNext() {
            // Detect if the provider is supported in machine.config 
            if (!CheckValidProvider()) { 
                return false;
            } 

            WizardPanel nextPanel = CreateCommandPanel((SqlDataSourceWizardForm)ParentWizard, DataConnection, NextPanel);
            if (nextPanel == null) {
                return false; 
            }
            NextPanel = nextPanel; 
 
            return true;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceConnectionPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Data; 
    using System.Data.Common;
    using System.ComponentModel.Design.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
 
    /// <devdoc>
    /// An interface to set and retrieve connection strings and provider names 
    /// to assist in configuring data sources.
    /// </devdoc>
    internal abstract class SqlDataSourceConnectionPanel : WizardPanel {
 
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
 
        protected SqlDataSourceConnectionPanel(SqlDataSourceDesigner sqlDataSourceDesigner) { 
            Debug.Assert(sqlDataSourceDesigner != null);
            _sqlDataSourceDesigner = sqlDataSourceDesigner; 
        }


        /// <devdoc> 
        /// The data connection selected by the user.
        /// </devdoc> 
        public abstract DesignerDataConnection DataConnection { 
            get;
        } 


        /// <devdoc>
        /// Checks that the selected connection uses a provider that is 
        /// supported in machine.config. An error message will be shown if the
        /// provider cannot be found, and false will be returned. 
        /// </devdoc> 
        protected bool CheckValidProvider() {
            // Detect if the provider is supported in machine.config 
            DesignerDataConnection dataConnection = DataConnection;
            try {
                SqlDataSourceDesigner.GetDbProviderFactory(dataConnection.ProviderName);
                return true; 
            }
            catch (Exception ex) { 
                UIServiceHelper.ShowError(ServiceProvider, ex, SR.GetString(SR.SqlDataSourceConnectionPanel_ProviderNotFound, dataConnection.ProviderName)); 
                return false;
            } 
        }

        /// <devdoc>
        /// Determines which command panel to show based on the state of the 
        /// wizard, and where the user has been in the wizard previously.
        /// Null will be returned if the user cannot proceed. 
        /// </devdoc> 
        internal static WizardPanel CreateCommandPanel(SqlDataSourceWizardForm wizard, DesignerDataConnection dataConnection, WizardPanel nextPanel) {
            IDataEnvironment dataEnvironment = null; 
            IServiceProvider serviceProvider = wizard.SqlDataSourceDesigner.Component.Site;
            if (serviceProvider != null) {
                dataEnvironment = (IDataEnvironment)serviceProvider.GetService(typeof(IDataEnvironment));
            } 

            bool requestingSchemaMode = false; 
 
            // Check if the table/field picker should be available
            // Must have table or view schema information to enable this 
            if (dataEnvironment != null) {
                // Check if there is schema available for the connection
                try {
                    IDesignerDataSchema schema = dataEnvironment.GetConnectionSchema(dataConnection); 
                    if (schema != null) {
                        requestingSchemaMode = schema.SupportsSchemaClass(DesignerDataSchemaClass.Tables); 
                        if (requestingSchemaMode) { 
                            // Force the table list to be refreshed to check for errors
                            schema.GetSchemaItems(DesignerDataSchemaClass.Tables); 
                        }
                        else {
                            // Tables are not supported, so try views
                            requestingSchemaMode = schema.SupportsSchemaClass(DesignerDataSchemaClass.Views); 

                            // Force the view list to be refreshed to check for errors 
                            schema.GetSchemaItems(DesignerDataSchemaClass.Views); 
                        }
                        // 
                    }
                }
                catch (Exception ex) {
                    UIServiceHelper.ShowError( 
                        serviceProvider,
                        ex, 
                        SR.GetString(SR.SqlDataSourceConnectionPanel_CouldNotGetConnectionSchema)); 
                    return null;
                } 
            }

            if (nextPanel == null) {
                // First time user hit "Next", create and populate appropriate panel 

                if (requestingSchemaMode) { 
                    // Schema is available, go to table/field picker 
                    SqlDataSourceConfigureSelectPanel commandPanel = wizard.GetConfigureSelectPanel();
                    return commandPanel; 
                }
                else {
                    // No schema available, go straight to custom mode
                    return CreateCustomCommandPanel(wizard, dataConnection); 
                }
            } 
            else { 
                // User hit back, and is now hitting Next again - try to retain state of next panel
 
                if (requestingSchemaMode) {
                    // If schema is now available, but we are not in schema mode, switch to it
                    if (!(nextPanel is SqlDataSourceConfigureSelectPanel)) {
                        SqlDataSourceConfigureSelectPanel commandPanel = wizard.GetConfigureSelectPanel(); 
                        return commandPanel;
                    } 
                } 
                else {
                    // In case of custom mode, don't touch anything unless we were previously in schema mode 
                    if (!(nextPanel is SqlDataSourceCustomCommandPanel)) {
                        return CreateCustomCommandPanel(wizard, dataConnection);
                    }
                } 
            }
 
            // Nothing has changed, just return the existing NextPanel without any changes. 
            return nextPanel;
        } 

        /// <devdoc>
        /// Creates the custom command panel.
        /// </devdoc> 
        private static WizardPanel CreateCustomCommandPanel(SqlDataSourceWizardForm wizard, DesignerDataConnection dataConnection) {
            // Clone the lists of parameters so we don't touch the originals 
            SqlDataSource sqlDataSource = (SqlDataSource)wizard.SqlDataSourceDesigner.Component; 
            ArrayList selectParameters = new ArrayList();
            ArrayList insertParameters = new ArrayList(); 
            ArrayList updateParameters = new ArrayList();
            ArrayList deleteParameters = new ArrayList();
            wizard.SqlDataSourceDesigner.CopyList(sqlDataSource.SelectParameters, selectParameters);
            wizard.SqlDataSourceDesigner.CopyList(sqlDataSource.InsertParameters, insertParameters); 
            wizard.SqlDataSourceDesigner.CopyList(sqlDataSource.UpdateParameters, updateParameters);
            wizard.SqlDataSourceDesigner.CopyList(sqlDataSource.DeleteParameters, deleteParameters); 
 
            SqlDataSourceCustomCommandPanel commandPanel = wizard.GetCustomCommandPanel();
 
            commandPanel.SetQueries(
                dataConnection,
                new SqlDataSourceQuery(sqlDataSource.SelectCommand, sqlDataSource.SelectCommandType, selectParameters),
                new SqlDataSourceQuery(sqlDataSource.InsertCommand, sqlDataSource.InsertCommandType, insertParameters), 
                new SqlDataSourceQuery(sqlDataSource.UpdateCommand, sqlDataSource.UpdateCommandType, updateParameters),
                new SqlDataSourceQuery(sqlDataSource.DeleteCommand, sqlDataSource.DeleteCommandType, deleteParameters)); 
 
            return commandPanel;
        } 

        /// <devdoc>
        /// Called when the user clicks on the Next button.
        /// </devdoc> 
        public override bool OnNext() {
            // Detect if the provider is supported in machine.config 
            if (!CheckValidProvider()) { 
                return false;
            } 

            WizardPanel nextPanel = CreateCommandPanel((SqlDataSourceWizardForm)ParentWizard, DataConnection, NextPanel);
            if (nextPanel == null) {
                return false; 
            }
            NextPanel = nextPanel; 
 
            return true;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
