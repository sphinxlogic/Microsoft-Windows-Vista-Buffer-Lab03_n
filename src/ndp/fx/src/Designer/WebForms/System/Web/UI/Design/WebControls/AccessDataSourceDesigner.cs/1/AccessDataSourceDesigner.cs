//------------------------------------------------------------------------------ 
// <copyright file="AccessDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Data.Common; 
    using System.ComponentModel.Design.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing.Design;
    using System.IO; 
    using System.Web.UI;
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 
 

    /// <include file='doc\AccessDataSourceDesigner.uex' path='docs/doc[@for="AccessDataSourceDesigner"]/*' /> 
    /// <devdoc>
    /// AccessDataSourceDesigner is the designer associated with an AccessDataSource.
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class AccessDataSourceDesigner : SqlDataSourceDesigner {
 
        /// <devdoc> 
        /// The AccessDataSource associated with this designer.
        /// </devdoc> 
        private AccessDataSource AccessDataSource {
            get {
                return (AccessDataSource)Component;
            } 
        }
 
        /// <include file='doc\AccessDataSourceDesigner.uex' path='docs/doc[@for="AccessDataSourceDesigner.DataFile"]/*' /> 
        /// <devdoc>
        /// Implements the designer's version of the DataFile property. 
        /// This is used to shadow the DataFile property of the
        /// runtime control.
        /// </devdoc>
        public string DataFile { 
            get {
                return AccessDataSource.DataFile; 
            } 
            set {
                if (value != DataFile) { 
                    AccessDataSource.DataFile = value;
                    UpdateDesignTimeHtml();
                    OnDataSourceChanged(EventArgs.Empty);
                } 
            }
        } 
 
        /// <devdoc>
        /// Creates the appropriate wizard for the Configure Data Source task. 
        /// </devdoc>
        internal override SqlDataSourceWizardForm CreateConfigureDataSourceWizardForm(IServiceProvider serviceProvider, IDataEnvironment dataEnvironment) {
            return new AccessDataSourceWizardForm(serviceProvider, this, dataEnvironment);
        } 

        /// <include file='doc\AccessDataSourceDesigner.uex' path='docs/doc[@for="AccessDataSourceDesigner.GetConnectionString"]/*' /> 
        /// <devdoc> 
        /// Gets the data source's connection string. This is overridden to replace
        /// the runtime control's DataFile property with the mapped path so it can 
        /// be used at design time.
        /// </devdoc>
        protected override string GetConnectionString() {
            return GetConnectionString(Component.Site, AccessDataSource); 
        }
 
        /// <devdoc> 
        /// Helper method to map the DataFile property of an AccessDataSource to
        /// a physical path in order to get a design-time enabled connection string. 
        /// </devdoc>
        internal static string GetConnectionString(IServiceProvider serviceProvider, AccessDataSource dataSource) {
            string originalDataFile = dataSource.DataFile;
            string connectionString; 
            try {
                // If filename is missing, abort 
                if (originalDataFile.Length == 0) { 
                    return null;
                } 
                dataSource.DataFile = UrlPath.MapPath(serviceProvider, originalDataFile);

                // Calling the ConnectionString property will automatically
                // build the full connection string using the file path 
                connectionString = dataSource.ConnectionString;
            } 
            finally { 
                dataSource.DataFile = originalDataFile;
            } 
            return connectionString;
        }

        /// <include file='doc\AccessDataSourceDesigner.uex' path='docs/doc[@for="AccessDataSourceDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);

            // Shadow runtime DataFile property
            PropertyDescriptor property = (PropertyDescriptor)properties["DataFile"]; 
            Debug.Assert(property != null);
            properties["DataFile"] = TypeDescriptor.CreateProperty(GetType(), property, new Attribute[0]); 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="AccessDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Data.Common; 
    using System.ComponentModel.Design.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing.Design;
    using System.IO; 
    using System.Web.UI;
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 
 

    /// <include file='doc\AccessDataSourceDesigner.uex' path='docs/doc[@for="AccessDataSourceDesigner"]/*' /> 
    /// <devdoc>
    /// AccessDataSourceDesigner is the designer associated with an AccessDataSource.
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class AccessDataSourceDesigner : SqlDataSourceDesigner {
 
        /// <devdoc> 
        /// The AccessDataSource associated with this designer.
        /// </devdoc> 
        private AccessDataSource AccessDataSource {
            get {
                return (AccessDataSource)Component;
            } 
        }
 
        /// <include file='doc\AccessDataSourceDesigner.uex' path='docs/doc[@for="AccessDataSourceDesigner.DataFile"]/*' /> 
        /// <devdoc>
        /// Implements the designer's version of the DataFile property. 
        /// This is used to shadow the DataFile property of the
        /// runtime control.
        /// </devdoc>
        public string DataFile { 
            get {
                return AccessDataSource.DataFile; 
            } 
            set {
                if (value != DataFile) { 
                    AccessDataSource.DataFile = value;
                    UpdateDesignTimeHtml();
                    OnDataSourceChanged(EventArgs.Empty);
                } 
            }
        } 
 
        /// <devdoc>
        /// Creates the appropriate wizard for the Configure Data Source task. 
        /// </devdoc>
        internal override SqlDataSourceWizardForm CreateConfigureDataSourceWizardForm(IServiceProvider serviceProvider, IDataEnvironment dataEnvironment) {
            return new AccessDataSourceWizardForm(serviceProvider, this, dataEnvironment);
        } 

        /// <include file='doc\AccessDataSourceDesigner.uex' path='docs/doc[@for="AccessDataSourceDesigner.GetConnectionString"]/*' /> 
        /// <devdoc> 
        /// Gets the data source's connection string. This is overridden to replace
        /// the runtime control's DataFile property with the mapped path so it can 
        /// be used at design time.
        /// </devdoc>
        protected override string GetConnectionString() {
            return GetConnectionString(Component.Site, AccessDataSource); 
        }
 
        /// <devdoc> 
        /// Helper method to map the DataFile property of an AccessDataSource to
        /// a physical path in order to get a design-time enabled connection string. 
        /// </devdoc>
        internal static string GetConnectionString(IServiceProvider serviceProvider, AccessDataSource dataSource) {
            string originalDataFile = dataSource.DataFile;
            string connectionString; 
            try {
                // If filename is missing, abort 
                if (originalDataFile.Length == 0) { 
                    return null;
                } 
                dataSource.DataFile = UrlPath.MapPath(serviceProvider, originalDataFile);

                // Calling the ConnectionString property will automatically
                // build the full connection string using the file path 
                connectionString = dataSource.ConnectionString;
            } 
            finally { 
                dataSource.DataFile = originalDataFile;
            } 
            return connectionString;
        }

        /// <include file='doc\AccessDataSourceDesigner.uex' path='docs/doc[@for="AccessDataSourceDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);

            // Shadow runtime DataFile property
            PropertyDescriptor property = (PropertyDescriptor)properties["DataFile"]; 
            Debug.Assert(property != null);
            properties["DataFile"] = TypeDescriptor.CreateProperty(GetType(), property, new Attribute[0]); 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
