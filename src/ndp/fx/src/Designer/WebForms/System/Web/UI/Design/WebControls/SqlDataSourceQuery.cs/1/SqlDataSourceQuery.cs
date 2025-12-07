//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Diagnostics; 
    using System.Web.UI.WebControls;

    /// <devdoc>
    /// Represents a single query of a SqlDataSource (Select/Insert/Update/Delete). 
    /// </devdoc>
    internal sealed class SqlDataSourceQuery { 
 
        private string _command;
        private SqlDataSourceCommandType _commandType; 
        private ICollection _parameters;


        /// <devdoc> 
        /// </devdoc>
        public SqlDataSourceQuery(string command, SqlDataSourceCommandType commandType, ICollection parameters) { 
            Debug.Assert(command != null); 
            Debug.Assert(parameters != null);
            _command = command; 
            _commandType = commandType;
            _parameters = parameters;
        }
 

        public string Command { 
            get { 
                return _command;
            } 
        }

        public SqlDataSourceCommandType CommandType {
            get { 
                return _commandType;
            } 
        } 

        public ICollection Parameters { 
            get {
                return _parameters;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Diagnostics; 
    using System.Web.UI.WebControls;

    /// <devdoc>
    /// Represents a single query of a SqlDataSource (Select/Insert/Update/Delete). 
    /// </devdoc>
    internal sealed class SqlDataSourceQuery { 
 
        private string _command;
        private SqlDataSourceCommandType _commandType; 
        private ICollection _parameters;


        /// <devdoc> 
        /// </devdoc>
        public SqlDataSourceQuery(string command, SqlDataSourceCommandType commandType, ICollection parameters) { 
            Debug.Assert(command != null); 
            Debug.Assert(parameters != null);
            _command = command; 
            _commandType = commandType;
            _parameters = parameters;
        }
 

        public string Command { 
            get { 
                return _command;
            } 
        }

        public SqlDataSourceCommandType CommandType {
            get { 
                return _commandType;
            } 
        } 

        public ICollection Parameters { 
            get {
                return _parameters;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
