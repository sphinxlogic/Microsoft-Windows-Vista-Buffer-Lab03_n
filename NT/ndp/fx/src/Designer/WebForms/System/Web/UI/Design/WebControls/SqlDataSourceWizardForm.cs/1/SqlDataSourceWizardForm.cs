//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceWizardForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
 
    /// <devdoc>
    /// The SqlDataSource configuration wizard. This guides the user through
    /// the connection string chooser panel and the command chooser panel.
    /// </devdoc> 
    internal class SqlDataSourceWizardForm : WizardForm {
        private SqlDataSourceConnectionPanel _connectionPanel; 
        private SqlDataSource _sqlDataSource; 
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
        private IDataEnvironment _dataEnvironment; 
        private DesignerDataConnection _designerDataConnection;

        private SqlDataSourceSaveConfiguredConnectionPanel _saveConfiguredConnectionPanel;
        private SqlDataSourceConfigureParametersPanel _configureParametersPanel; 
        private SqlDataSourceConfigureSelectPanel _configureSelectPanel;
        private SqlDataSourceCustomCommandPanel _customCommandPanel; 
        private SqlDataSourceSummaryPanel _summaryPanel; 

 
        /// <devdoc>
        /// Creates a new SqlDataSourceWizardForm.
        /// </devdoc>
        public SqlDataSourceWizardForm(IServiceProvider serviceProvider, SqlDataSourceDesigner sqlDataSourceDesigner, IDataEnvironment dataEnvironment) : base(serviceProvider) { 
            //Icon = new Icon(typeof(LibraryBuilderForm), "LibraryBuilder.ico");
            Glyph = new Bitmap(typeof(SqlDataSourceWizardForm), "datasourcewizard.bmp"); 
 
            Debug.Assert(dataEnvironment != null);
            _dataEnvironment = dataEnvironment; 

            _sqlDataSource = (SqlDataSource)sqlDataSourceDesigner.Component;
            _sqlDataSourceDesigner = sqlDataSourceDesigner;
            Debug.Assert(_sqlDataSource != null); 

 
            Text = SR.GetString(SR.ConfigureDataSource_Title, _sqlDataSource.ID); 

            // Set up the connection panel 
            _connectionPanel = CreateConnectionPanel();

            // Adds the panel to the wizard
            SetPanels(new WizardPanel[] { 
                _connectionPanel,
                }); 
 
            // Create and register all child panels
            _saveConfiguredConnectionPanel = new SqlDataSourceSaveConfiguredConnectionPanel(_sqlDataSourceDesigner, _dataEnvironment); 
            RegisterPanel(_saveConfiguredConnectionPanel);
            _configureParametersPanel = new SqlDataSourceConfigureParametersPanel(_sqlDataSourceDesigner);
            RegisterPanel(_configureParametersPanel);
            _configureSelectPanel = new SqlDataSourceConfigureSelectPanel(_sqlDataSourceDesigner); 
            RegisterPanel(_configureSelectPanel);
            _customCommandPanel = new SqlDataSourceCustomCommandPanel(_sqlDataSourceDesigner); 
            RegisterPanel(_customCommandPanel); 
            _summaryPanel = new SqlDataSourceSummaryPanel(_sqlDataSourceDesigner);
            RegisterPanel(_summaryPanel); 
        }


        /// <devdoc> 
        /// Gets the current data connection associated with this wizard.
        /// </devdoc> 
        internal DesignerDataConnection DesignerDataConnection { 
            get {
                return _designerDataConnection; 
            }
        }

        /// <devdoc> 
        /// Gets the DataEnvironment service.
        /// </devdoc> 
        internal IDataEnvironment DataEnvironment { 
            get {
                return _dataEnvironment; 
            }
        }

        protected override string HelpTopic { 
            get {
                return "net.Asp.SqlDataSource.ConfigureDataSource"; 
            } 
        }
 
        /// <devdoc>
        /// Gets the SqlDatasourceDesigner associated with this wizard
        /// </devdoc>
        internal SqlDataSourceDesigner SqlDataSourceDesigner { 
            get {
                return _sqlDataSourceDesigner; 
            } 
        }
 

        /// <devdoc>
        /// Creates the appropriate connection panel for the wizard.
        /// </devdoc> 
        protected virtual SqlDataSourceConnectionPanel CreateConnectionPanel() {
            // Set up the connection panel 
            return new SqlDataSourceDataConnectionChooserPanel(SqlDataSourceDesigner, DataEnvironment); 
        }
 
        internal SqlDataSourceConfigureParametersPanel GetConfigureParametersPanel() {
            _configureParametersPanel.ResetUI();
            return _configureParametersPanel;
        } 

        internal SqlDataSourceConfigureSelectPanel GetConfigureSelectPanel() { 
            _configureSelectPanel.ResetUI(); 
            return _configureSelectPanel;
        } 

        internal SqlDataSourceCustomCommandPanel GetCustomCommandPanel() {
            _customCommandPanel.ResetUI();
            return _customCommandPanel; 
        }
 
        internal SqlDataSourceSaveConfiguredConnectionPanel GetSaveConfiguredConnectionPanel() { 
            _saveConfiguredConnectionPanel.ResetUI();
            return _saveConfiguredConnectionPanel; 
        }

        internal SqlDataSourceSummaryPanel GetSummaryPanel() {
            _summaryPanel.ResetUI(); 
            return _summaryPanel;
        } 
 
        /// <devdoc>
        /// Called when a panel is about to change. 
        /// </devdoc>
        protected override void OnPanelChanging(WizardPanelChangingEventArgs e) {
            base.OnPanelChanging(e);
 
            // If the panel that was just shown was the connection panel, we
            // keep track of what the connection is so that other panels can 
            // use it. 
            if (e.CurrentPanel == _connectionPanel) {
                _designerDataConnection = _connectionPanel.DataConnection; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceWizardForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
 
    /// <devdoc>
    /// The SqlDataSource configuration wizard. This guides the user through
    /// the connection string chooser panel and the command chooser panel.
    /// </devdoc> 
    internal class SqlDataSourceWizardForm : WizardForm {
        private SqlDataSourceConnectionPanel _connectionPanel; 
        private SqlDataSource _sqlDataSource; 
        private SqlDataSourceDesigner _sqlDataSourceDesigner;
        private IDataEnvironment _dataEnvironment; 
        private DesignerDataConnection _designerDataConnection;

        private SqlDataSourceSaveConfiguredConnectionPanel _saveConfiguredConnectionPanel;
        private SqlDataSourceConfigureParametersPanel _configureParametersPanel; 
        private SqlDataSourceConfigureSelectPanel _configureSelectPanel;
        private SqlDataSourceCustomCommandPanel _customCommandPanel; 
        private SqlDataSourceSummaryPanel _summaryPanel; 

 
        /// <devdoc>
        /// Creates a new SqlDataSourceWizardForm.
        /// </devdoc>
        public SqlDataSourceWizardForm(IServiceProvider serviceProvider, SqlDataSourceDesigner sqlDataSourceDesigner, IDataEnvironment dataEnvironment) : base(serviceProvider) { 
            //Icon = new Icon(typeof(LibraryBuilderForm), "LibraryBuilder.ico");
            Glyph = new Bitmap(typeof(SqlDataSourceWizardForm), "datasourcewizard.bmp"); 
 
            Debug.Assert(dataEnvironment != null);
            _dataEnvironment = dataEnvironment; 

            _sqlDataSource = (SqlDataSource)sqlDataSourceDesigner.Component;
            _sqlDataSourceDesigner = sqlDataSourceDesigner;
            Debug.Assert(_sqlDataSource != null); 

 
            Text = SR.GetString(SR.ConfigureDataSource_Title, _sqlDataSource.ID); 

            // Set up the connection panel 
            _connectionPanel = CreateConnectionPanel();

            // Adds the panel to the wizard
            SetPanels(new WizardPanel[] { 
                _connectionPanel,
                }); 
 
            // Create and register all child panels
            _saveConfiguredConnectionPanel = new SqlDataSourceSaveConfiguredConnectionPanel(_sqlDataSourceDesigner, _dataEnvironment); 
            RegisterPanel(_saveConfiguredConnectionPanel);
            _configureParametersPanel = new SqlDataSourceConfigureParametersPanel(_sqlDataSourceDesigner);
            RegisterPanel(_configureParametersPanel);
            _configureSelectPanel = new SqlDataSourceConfigureSelectPanel(_sqlDataSourceDesigner); 
            RegisterPanel(_configureSelectPanel);
            _customCommandPanel = new SqlDataSourceCustomCommandPanel(_sqlDataSourceDesigner); 
            RegisterPanel(_customCommandPanel); 
            _summaryPanel = new SqlDataSourceSummaryPanel(_sqlDataSourceDesigner);
            RegisterPanel(_summaryPanel); 
        }


        /// <devdoc> 
        /// Gets the current data connection associated with this wizard.
        /// </devdoc> 
        internal DesignerDataConnection DesignerDataConnection { 
            get {
                return _designerDataConnection; 
            }
        }

        /// <devdoc> 
        /// Gets the DataEnvironment service.
        /// </devdoc> 
        internal IDataEnvironment DataEnvironment { 
            get {
                return _dataEnvironment; 
            }
        }

        protected override string HelpTopic { 
            get {
                return "net.Asp.SqlDataSource.ConfigureDataSource"; 
            } 
        }
 
        /// <devdoc>
        /// Gets the SqlDatasourceDesigner associated with this wizard
        /// </devdoc>
        internal SqlDataSourceDesigner SqlDataSourceDesigner { 
            get {
                return _sqlDataSourceDesigner; 
            } 
        }
 

        /// <devdoc>
        /// Creates the appropriate connection panel for the wizard.
        /// </devdoc> 
        protected virtual SqlDataSourceConnectionPanel CreateConnectionPanel() {
            // Set up the connection panel 
            return new SqlDataSourceDataConnectionChooserPanel(SqlDataSourceDesigner, DataEnvironment); 
        }
 
        internal SqlDataSourceConfigureParametersPanel GetConfigureParametersPanel() {
            _configureParametersPanel.ResetUI();
            return _configureParametersPanel;
        } 

        internal SqlDataSourceConfigureSelectPanel GetConfigureSelectPanel() { 
            _configureSelectPanel.ResetUI(); 
            return _configureSelectPanel;
        } 

        internal SqlDataSourceCustomCommandPanel GetCustomCommandPanel() {
            _customCommandPanel.ResetUI();
            return _customCommandPanel; 
        }
 
        internal SqlDataSourceSaveConfiguredConnectionPanel GetSaveConfiguredConnectionPanel() { 
            _saveConfiguredConnectionPanel.ResetUI();
            return _saveConfiguredConnectionPanel; 
        }

        internal SqlDataSourceSummaryPanel GetSummaryPanel() {
            _summaryPanel.ResetUI(); 
            return _summaryPanel;
        } 
 
        /// <devdoc>
        /// Called when a panel is about to change. 
        /// </devdoc>
        protected override void OnPanelChanging(WizardPanelChangingEventArgs e) {
            base.OnPanelChanging(e);
 
            // If the panel that was just shown was the connection panel, we
            // keep track of what the connection is so that other panels can 
            // use it. 
            if (e.CurrentPanel == _connectionPanel) {
                _designerDataConnection = _connectionPanel.DataConnection; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
