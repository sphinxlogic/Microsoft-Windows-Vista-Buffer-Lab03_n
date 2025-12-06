//------------------------------------------------------------------------------ 
// <copyright file="WizardPanelChangingEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Drawing;
    using System.Windows.Forms; 

    /// <devdoc> 
    /// </devdoc> 
    internal class WizardPanelChangingEventArgs : EventArgs {
 
        private WizardPanel _currentPanel;

        /// <devdoc>
        /// </devdoc> 
        public WizardPanelChangingEventArgs(WizardPanel currentPanel) {
            _currentPanel = currentPanel; 
        } 

        /// <devdoc> 
        /// </devdoc>
        public WizardPanel CurrentPanel {
            get {
                return _currentPanel; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WizardPanelChangingEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Drawing;
    using System.Windows.Forms; 

    /// <devdoc> 
    /// </devdoc> 
    internal class WizardPanelChangingEventArgs : EventArgs {
 
        private WizardPanel _currentPanel;

        /// <devdoc>
        /// </devdoc> 
        public WizardPanelChangingEventArgs(WizardPanel currentPanel) {
            _currentPanel = currentPanel; 
        } 

        /// <devdoc> 
        /// </devdoc>
        public WizardPanel CurrentPanel {
            get {
                return _currentPanel; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
