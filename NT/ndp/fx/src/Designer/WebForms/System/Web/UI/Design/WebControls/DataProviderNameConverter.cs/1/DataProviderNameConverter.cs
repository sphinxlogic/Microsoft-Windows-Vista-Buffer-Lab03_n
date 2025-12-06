//------------------------------------------------------------------------------ 
// <copyright file="DataProviderNameConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
    using System.Collections; 
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization; 
    using System.Web.UI.WebControls;
 
    /// <devdoc> 
    /// Creates a user-selectable list of ADO.net provider names.
    /// The providers are factories to create System.Data objects. 
    /// </devdoc>
    public class DataProviderNameConverter : StringConverter {

        /// <devdoc> 
        /// Returns a list of the user-friendly provider names.
        /// </devdoc> 
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            DataTable providerTable = DbProviderFactories.GetFactoryClasses();
            DataRowCollection rows = providerTable.Rows; 
            string[] providerNames = new string[rows.Count];
            for (int i = 0; i < rows.Count; i++) {
                providerNames[i] = (string)rows[i]["InvariantName"];
            } 
            return new StandardValuesCollection(providerNames);
        } 
 
        /// <devdoc>
        /// </devdoc> 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return false;
        }
 
        /// <devdoc>
        /// </devdoc> 
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
            return true;
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataProviderNameConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel;
    using System.Collections; 
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization; 
    using System.Web.UI.WebControls;
 
    /// <devdoc> 
    /// Creates a user-selectable list of ADO.net provider names.
    /// The providers are factories to create System.Data objects. 
    /// </devdoc>
    public class DataProviderNameConverter : StringConverter {

        /// <devdoc> 
        /// Returns a list of the user-friendly provider names.
        /// </devdoc> 
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            DataTable providerTable = DbProviderFactories.GetFactoryClasses();
            DataRowCollection rows = providerTable.Rows; 
            string[] providerNames = new string[rows.Count];
            for (int i = 0; i < rows.Count; i++) {
                providerNames[i] = (string)rows[i]["InvariantName"];
            } 
            return new StandardValuesCollection(providerNames);
        } 
 
        /// <devdoc>
        /// </devdoc> 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return false;
        }
 
        /// <devdoc>
        /// </devdoc> 
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
            return true;
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
