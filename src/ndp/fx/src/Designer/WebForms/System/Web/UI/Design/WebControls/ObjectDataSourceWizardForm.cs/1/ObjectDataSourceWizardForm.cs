//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceWizardForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Windows.Forms;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls; 

    /// <devdoc> 
    /// The ObjectDataSource configuration wizard. This guides the user through
    /// the type chooser panel and the method chooser panel and helps to
    /// associate parameters.
    /// </devdoc> 
    internal sealed class ObjectDataSourceWizardForm : WizardForm {
 
        private ObjectDataSourceDesigner _objectDataSourceDesigner; 
        private ObjectDataSource _objectDataSource;
        private ObjectDataSourceConfigureParametersPanel _parametersPanel; 

        /// <devdoc>
        /// Creates a new ObjectDataSourceWizardForm.
        /// </devdoc> 
        public ObjectDataSourceWizardForm(IServiceProvider serviceProvider, ObjectDataSourceDesigner objectDataSourceDesigner) : base(serviceProvider) {
            Glyph = new Bitmap(typeof(SqlDataSourceWizardForm), "datasourcewizard.bmp"); 
            //Icon = new Icon(typeof(LibraryBuilderForm), "LibraryBuilder.ico"); 

            _objectDataSourceDesigner = objectDataSourceDesigner; 
            Debug.Assert(_objectDataSourceDesigner != null);

            _objectDataSource = (ObjectDataSource)_objectDataSourceDesigner.Component;
            Debug.Assert(_objectDataSource != null); 

 
            Text = SR.GetString(SR.ConfigureDataSource_Title, _objectDataSource.ID); 

            ObjectDataSourceChooseTypePanel typePanel = new ObjectDataSourceChooseTypePanel(_objectDataSourceDesigner); 
            ObjectDataSourceChooseMethodsPanel methodsPanel = new ObjectDataSourceChooseMethodsPanel(_objectDataSourceDesigner);

            // Adds the panels to the wizard
            SetPanels(new WizardPanel[] { 
                typePanel,
                methodsPanel, 
                }); 

            _parametersPanel = new ObjectDataSourceConfigureParametersPanel(_objectDataSourceDesigner); 
            RegisterPanel(_parametersPanel);
        }

        protected override string HelpTopic { 
            get {
                return "net.Asp.ObjectDataSource.ConfigureDataSource"; 
            } 
        }
 
        internal ObjectDataSourceConfigureParametersPanel GetParametersPanel() {
            _parametersPanel.ResetUI();
            return _parametersPanel;
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceWizardForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Windows.Forms;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls; 

    /// <devdoc> 
    /// The ObjectDataSource configuration wizard. This guides the user through
    /// the type chooser panel and the method chooser panel and helps to
    /// associate parameters.
    /// </devdoc> 
    internal sealed class ObjectDataSourceWizardForm : WizardForm {
 
        private ObjectDataSourceDesigner _objectDataSourceDesigner; 
        private ObjectDataSource _objectDataSource;
        private ObjectDataSourceConfigureParametersPanel _parametersPanel; 

        /// <devdoc>
        /// Creates a new ObjectDataSourceWizardForm.
        /// </devdoc> 
        public ObjectDataSourceWizardForm(IServiceProvider serviceProvider, ObjectDataSourceDesigner objectDataSourceDesigner) : base(serviceProvider) {
            Glyph = new Bitmap(typeof(SqlDataSourceWizardForm), "datasourcewizard.bmp"); 
            //Icon = new Icon(typeof(LibraryBuilderForm), "LibraryBuilder.ico"); 

            _objectDataSourceDesigner = objectDataSourceDesigner; 
            Debug.Assert(_objectDataSourceDesigner != null);

            _objectDataSource = (ObjectDataSource)_objectDataSourceDesigner.Component;
            Debug.Assert(_objectDataSource != null); 

 
            Text = SR.GetString(SR.ConfigureDataSource_Title, _objectDataSource.ID); 

            ObjectDataSourceChooseTypePanel typePanel = new ObjectDataSourceChooseTypePanel(_objectDataSourceDesigner); 
            ObjectDataSourceChooseMethodsPanel methodsPanel = new ObjectDataSourceChooseMethodsPanel(_objectDataSourceDesigner);

            // Adds the panels to the wizard
            SetPanels(new WizardPanel[] { 
                typePanel,
                methodsPanel, 
                }); 

            _parametersPanel = new ObjectDataSourceConfigureParametersPanel(_objectDataSourceDesigner); 
            RegisterPanel(_parametersPanel);
        }

        protected override string HelpTopic { 
            get {
                return "net.Asp.ObjectDataSource.ConfigureDataSource"; 
            } 
        }
 
        internal ObjectDataSourceConfigureParametersPanel GetParametersPanel() {
            _parametersPanel.ResetUI();
            return _parametersPanel;
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
