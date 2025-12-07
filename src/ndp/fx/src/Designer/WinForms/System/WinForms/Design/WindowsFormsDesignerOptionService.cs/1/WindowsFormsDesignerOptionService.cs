//------------------------------------------------------------------------------ 
// <copyright file="WindowsFormsDesignerOptionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.ComponentModel.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization; 

    /// <devdoc> 
    ///     Makes the DesignerOptions queryable through the IDesignerOption service. 
    /// </devdoc>
    public class WindowsFormsDesignerOptionService : DesignerOptionService { 
        private DesignerOptions _options;

        public WindowsFormsDesignerOptionService() {
        } 

        public virtual DesignerOptions CompatibilityOptions { 
            get { 
                if (_options == null) {
                    _options = new DesignerOptions(); 
                }
                return _options;
            }
        } 

        /// <devdoc> 
        ///     This method is called on demand the first time a user asks for child 
        ///     options or properties of an options collection.
        /// </devdoc> 
        protected override void PopulateOptionCollection(DesignerOptionCollection options) {

            if (options.Parent == null) {
                DesignerOptions designerOptions = CompatibilityOptions; 
                if (designerOptions != null) {
                    CreateOptionCollection(options, "DesignerOptions", designerOptions); 
                } 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WindowsFormsDesignerOptionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.ComponentModel.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization; 

    /// <devdoc> 
    ///     Makes the DesignerOptions queryable through the IDesignerOption service. 
    /// </devdoc>
    public class WindowsFormsDesignerOptionService : DesignerOptionService { 
        private DesignerOptions _options;

        public WindowsFormsDesignerOptionService() {
        } 

        public virtual DesignerOptions CompatibilityOptions { 
            get { 
                if (_options == null) {
                    _options = new DesignerOptions(); 
                }
                return _options;
            }
        } 

        /// <devdoc> 
        ///     This method is called on demand the first time a user asks for child 
        ///     options or properties of an options collection.
        /// </devdoc> 
        protected override void PopulateOptionCollection(DesignerOptionCollection options) {

            if (options.Parent == null) {
                DesignerOptions designerOptions = CompatibilityOptions; 
                if (designerOptions != null) {
                    CreateOptionCollection(options, "DesignerOptions", designerOptions); 
                } 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
