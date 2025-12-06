//------------------------------------------------------------------------------ 
// <copyright file="AccessDataSourceWizardForm.cs" company="Microsoft">
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
    /// The AccessDataSource configuration wizard. This guides the user through
    /// the connection chooser panel and the command chooser panel.
    /// </devdoc> 
    internal class AccessDataSourceWizardForm : SqlDataSourceWizardForm {
 
        /// <devdoc> 
        /// Creates a new AccessDataSourceWizardForm.
        /// </devdoc> 
        public AccessDataSourceWizardForm(IServiceProvider serviceProvider, AccessDataSourceDesigner accessDataSourceDesigner, IDataEnvironment dataEnvironment) :
            base(serviceProvider, accessDataSourceDesigner, dataEnvironment) {
            Glyph = new Bitmap(typeof(AccessDataSourceWizardForm), "datasourcewizard.bmp");
        } 

        protected override string HelpTopic { 
            get { 
                return "net.Asp.AccessDataSource.ConfigureDataSource";
            } 
        }

        /// <devdoc>
        /// Creates the appropriate connection panel for the wizard. 
        /// </devdoc>
        protected override SqlDataSourceConnectionPanel CreateConnectionPanel() { 
            Debug.Assert(SqlDataSourceDesigner is AccessDataSourceDesigner); 

            AccessDataSourceDesigner accessDataSourceDesigner = (AccessDataSourceDesigner)SqlDataSourceDesigner; 
            AccessDataSource accessDataSource = (AccessDataSource)accessDataSourceDesigner.Component;

            // Set up the connection panel
            return new AccessDataSourceConnectionChooserPanel(accessDataSourceDesigner, accessDataSource); 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="AccessDataSourceWizardForm.cs" company="Microsoft">
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
    /// The AccessDataSource configuration wizard. This guides the user through
    /// the connection chooser panel and the command chooser panel.
    /// </devdoc> 
    internal class AccessDataSourceWizardForm : SqlDataSourceWizardForm {
 
        /// <devdoc> 
        /// Creates a new AccessDataSourceWizardForm.
        /// </devdoc> 
        public AccessDataSourceWizardForm(IServiceProvider serviceProvider, AccessDataSourceDesigner accessDataSourceDesigner, IDataEnvironment dataEnvironment) :
            base(serviceProvider, accessDataSourceDesigner, dataEnvironment) {
            Glyph = new Bitmap(typeof(AccessDataSourceWizardForm), "datasourcewizard.bmp");
        } 

        protected override string HelpTopic { 
            get { 
                return "net.Asp.AccessDataSource.ConfigureDataSource";
            } 
        }

        /// <devdoc>
        /// Creates the appropriate connection panel for the wizard. 
        /// </devdoc>
        protected override SqlDataSourceConnectionPanel CreateConnectionPanel() { 
            Debug.Assert(SqlDataSourceDesigner is AccessDataSourceDesigner); 

            AccessDataSourceDesigner accessDataSourceDesigner = (AccessDataSourceDesigner)SqlDataSourceDesigner; 
            AccessDataSource accessDataSource = (AccessDataSource)accessDataSourceDesigner.Component;

            // Set up the connection panel
            return new AccessDataSourceConnectionChooserPanel(accessDataSourceDesigner, accessDataSource); 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
