//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataConnection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 
    using System.Windows.Forms;

    /// <devdoc>
    /// A data connection represents a single connection to a particular 
    /// database or data source in the design tool or in an application
    /// config file. 
    /// 
    /// A DesignerDataConnection object may also be passed to other APIs
    /// to get access to services such as database schema information or 
    /// the QueryBuilder host dialog.
    /// </devdoc>
    public sealed class DesignerDataConnection {
 
        private string _connectionString;
        private bool _isConfigured; 
        private string _name; 
        private string _providerName;
 
        /// <devdoc>
        /// Creates a new instance of a DesignerDataConnection representing a
        /// database connection stored by a host environment or located in an
        /// application config file. 
        /// This constructor is used to create non-configured connections.
        /// </devdoc> 
        public DesignerDataConnection(string name, string providerName, string connectionString) : this(name, providerName, connectionString, false) { 
        }
 
        /// <devdoc>
        /// Creates a new instance of a DesignerDataConnection representing a
        /// database connection stored by a host environment or located in an
        /// application config file. 
        /// This constructor is used to create both configured and
        /// non-configured connections. 
        /// </devdoc> 
        public DesignerDataConnection(string name, string providerName, string connectionString, bool isConfigured) {
            _name = name; 
            _providerName = providerName;
            _connectionString = connectionString;
            _isConfigured = isConfigured;
        } 

        /// <devdoc> 
        /// The connection string value for the connection. 
        /// </devdoc>
        public string ConnectionString { 
            get {
                return _connectionString;
            }
        } 

        /// <devdoc> 
        /// Returns true if the connection is configured in the 
        /// application-level configuration file (web.config), false
        /// otherwise. 
        /// </devdoc>
        public bool IsConfigured {
            get {
                return _isConfigured; 
            }
        } 
 
        /// <devdoc>
        /// The name associated with this connection in the design tool. Typically 
        /// this is used to represent the connection in user interface.
        /// If this is a configured connection (IsConfigured=true) then this is
        /// the name of the connection defined in the <connectionStrings>
        /// section of the application web.config. 
        /// </devdoc>
        public string Name { 
            get { 
                return _name;
            } 
        }

        /// <devdoc>
        /// The name of the ADO.NET managed provider used to access data from this 
        /// connection.
        /// </devdoc> 
        public string ProviderName { 
            get {
                return _providerName; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataConnection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 
    using System.Windows.Forms;

    /// <devdoc>
    /// A data connection represents a single connection to a particular 
    /// database or data source in the design tool or in an application
    /// config file. 
    /// 
    /// A DesignerDataConnection object may also be passed to other APIs
    /// to get access to services such as database schema information or 
    /// the QueryBuilder host dialog.
    /// </devdoc>
    public sealed class DesignerDataConnection {
 
        private string _connectionString;
        private bool _isConfigured; 
        private string _name; 
        private string _providerName;
 
        /// <devdoc>
        /// Creates a new instance of a DesignerDataConnection representing a
        /// database connection stored by a host environment or located in an
        /// application config file. 
        /// This constructor is used to create non-configured connections.
        /// </devdoc> 
        public DesignerDataConnection(string name, string providerName, string connectionString) : this(name, providerName, connectionString, false) { 
        }
 
        /// <devdoc>
        /// Creates a new instance of a DesignerDataConnection representing a
        /// database connection stored by a host environment or located in an
        /// application config file. 
        /// This constructor is used to create both configured and
        /// non-configured connections. 
        /// </devdoc> 
        public DesignerDataConnection(string name, string providerName, string connectionString, bool isConfigured) {
            _name = name; 
            _providerName = providerName;
            _connectionString = connectionString;
            _isConfigured = isConfigured;
        } 

        /// <devdoc> 
        /// The connection string value for the connection. 
        /// </devdoc>
        public string ConnectionString { 
            get {
                return _connectionString;
            }
        } 

        /// <devdoc> 
        /// Returns true if the connection is configured in the 
        /// application-level configuration file (web.config), false
        /// otherwise. 
        /// </devdoc>
        public bool IsConfigured {
            get {
                return _isConfigured; 
            }
        } 
 
        /// <devdoc>
        /// The name associated with this connection in the design tool. Typically 
        /// this is used to represent the connection in user interface.
        /// If this is a configured connection (IsConfigured=true) then this is
        /// the name of the connection defined in the <connectionStrings>
        /// section of the application web.config. 
        /// </devdoc>
        public string Name { 
            get { 
                return _name;
            } 
        }

        /// <devdoc>
        /// The name of the ADO.NET managed provider used to access data from this 
        /// connection.
        /// </devdoc> 
        public string ProviderName { 
            get {
                return _providerName; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
